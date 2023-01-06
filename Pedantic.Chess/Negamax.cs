using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Pedantic.Genetics;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class Negamax
    {
        public Negamax(Board board, TimeControl time, short maxSearchDepth, long maxNodes = long.MaxValue)
        {
            this.board = board;
            this.time = time;
            this.maxSearchDepth = maxSearchDepth;
            this.maxNodes = maxNodes;
        }

        public short Depth { get; private set; }
        public short Score { get; private set; }
        public ulong[] PV { get; private set; } = emptyPV;
        public bool MustAbort => nodesVisited > maxNodes || ((nodesVisited & CHECK_TC_NODES_MASK) == 0 && time.CheckTimeBudget());

        public void Search()
        {
            Depth = 0;

            if (board.OneLegalMove(out ulong bestMove))
            {
                Uci.BestMove(bestMove);
                return;
            }

            (short score, ulong[] pv) = Search(-Constants.INFINITE_WINDOW, Constants.INFINITE_WINDOW, 0);
            Score = (short)-score;

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() && !Evaluation.IsCheckmate(Score) && !wasAborted)
            {
                time.StartInterval();
                history.Rescale();
                UpdateTtWithPV(PV, Depth);

                short alpha = (short)(Score - Constants.ALPHA_BETA_WINDOW);
                short beta = (short)(Score + Constants.ALPHA_BETA_WINDOW);

                (Score, PV) = Search(alpha, beta, Depth);

                if (wasAborted)
                {
                    break;
                }

                if (Score <= alpha || Score >= beta)
                {
                    (Score, PV) = Search(-Constants.INFINITE_WINDOW, Constants.INFINITE_WINDOW, Depth);
                }

                ReportSearchResults(out bestMove);
            }

            Uci.BestMove(bestMove);
        }

        public void ReportSearchResults(out ulong bestMove)
        {
            PV = CollectPV(PV, Depth);
            if (IsCheckmate(Score, out int mateIn))
            {
                Uci.InfoMate(Depth, mateIn, nodesVisited, time.Elapsed, PV);
            }
            else
            {
                Uci.Info(Depth, Score, nodesVisited, time.Elapsed, PV);
            }

            bestMove = PV[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (short Score, ulong[] PV) NegSearchTT(short alpha, short beta, short depth, int ply)
        {
            var result = SearchTT((short)-beta, (short)-alpha, depth, ply);
            return ((short)-result.Score, result.PV);
        }
        public (short Score, ulong[] PV) SearchTT(short alpha, short beta, short depth, int ply = 0)
        {
            if (TtEval.TryGetScore(board.Hash, depth, ply, alpha, beta, out short score))
            {
                return SearchResult(score);
            }

            var result = Search(alpha, beta, depth, ply);

            TtEval.Add(board.Hash, depth, ply, alpha, beta, result.Score, result.PV.Length > 0 ? result.PV[0] : 0ul);
            return result;
        }

        public (short Score, ulong[] PV) Search(short alpha, short beta, short depth, int ply = 0)
        {
            ulong[] pv = emptyPV;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return defaultResult;
            }

            if (ply > 0)
            {
                alpha = Math.Max(alpha, (short)-Constants.CHECKMATE_SCORE);
                beta = Math.Min(beta, (short)(Constants.CHECKMATE_SCORE - 1));

                if (alpha >= beta)
                {
                    return SearchResult(alpha);
                }
            }

            if (depth <= 0)
            {
                return SearchResult(Quiesce(alpha, beta, ply));
            }

            nodesVisited++;

            int extension = 0;
            
            bool inCheck = board.IsChecked();
            if (inCheck)
            {
                extension = 1;
            }
            short eval = Evaluation.Compute(board);

            if (ply > 0 && !inCheck && depth >= 3 && eval >= beta)
            {
                if (board.MakeMove(Move.NullMove))
                {
                    short R = (short)(depth <= 6 ? 2 : 3);
                    //short score = NegZeroWindowSearch(beta, (short)(depth - 3), ply + 1);
                    short score = (short)-ZwSearchTT((short)(-beta + 1), (short)(depth - R - 1), ply + 1);
                    board.UnmakeMove();
                    if (score >= beta)
                    {
                        return SearchResult(beta);
                    }
                }
            }

            bool futilityPruning = depth < 4 && Math.Abs(alpha) < Constants.CHECKMATE_BASE &&
                                   eval + futilityMargin[depth] <= alpha;

            history.SideToMove = board.SideToMove;
            MoveList moveList = moveListPool.Get();
            board.GenerateMoves(moveList, history);
            TtEval.TryGetBestMove(board.Hash, out ulong bestMove);
            moveList.UpdateScores(bestMove, killers[ply]);

            bool raisedAlpha = false;

            int legalMoves = 0;

            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                ulong move = moveList[n];

                if (!board.MakeMove(move))
                {
                    continue;
                }

                bool isCapture = Move.GetCapture(move) != Piece.None;
                bool isPromote = Move.GetPromote(move) != Piece.None;

                if (futilityPruning && legalMoves > 0 && !isCapture && !isPromote && !inCheck)
                {
                    board.UnmakeMove();
                    continue;
                }

                short newDepth = (short)(depth - 1 + extension);
                short reduction = 0;
                if (!inCheck && depth > 4 && legalMoves > 2 && !isCapture && !isPromote &&
                    !board.IsChecked() &&
                    Move.GetScore(move) < 50 &&
                    (move & 0x0fff) != (killers[ply][0] & 0x0fff) &&
                    (move & 0x0fff) != (killers[ply][1] & 0x0fff) &&
                    (move & 0x0fff) != (bestMove & 0x0fff))
                {
                    reduction = (short)(legalMoves <= 6 ? 1 : 2);
                    newDepth -= reduction;
                }

                (short Score, ulong[] PV) value = (-short.MaxValue, emptyPV);
                for (;;)
                {
                    if (!raisedAlpha)
                    {
                        value = NegSearchTT(alpha, beta, newDepth, ply + 1);
                    }
                    else
                    {
                        if ((short)-ZwSearchTT((short)-alpha, newDepth, ply + 1) > alpha)
                        {
                            value = NegSearchTT(alpha, beta, newDepth, ply + 1);
                        }
                    }

                    if (reduction > 0 && value.Score > alpha)
                    {
                        newDepth += reduction;
                        reduction = 0;
                    }
                    else
                    {
                        break;
                    }
                }

                board.UnmakeMove();
                legalMoves++;


                if (value.Score > alpha)
                {
                    alpha = value.Score;
                    raisedAlpha = true;
                    pv = MergeMove(value.PV, move);

                    if (!isCapture)
                    {
                        history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                    }

                    if (value.Score >= beta)
                    {
                        if (!isCapture)
                        {
                            killers[ply][1] = killers[ply][0];
                            killers[ply][0] = move;
                        }

                        moveListPool.Return(moveList);
                        return (beta, pv);
                    }
                }
                
            }

            moveListPool.Return(moveList);
            if (legalMoves == 0)
            {
                if (inCheck)
                {
                    return SearchResult((short)(-Constants.CHECKMATE_SCORE + ply));
                }

                return defaultResult;
            }

            return (alpha, pv);
        }

        public ulong[] MergeMove(ulong[] pv, ulong move)
        {
            Array.Resize(ref pv, pv.Length + 1);
            Array.Copy(pv, 0, pv, 1, pv.Length - 1);
            pv[0] = move;
            return pv;
        }

        public void UpdateTtWithPV(ulong[] pv, int depth)
        {
            Board bd = board.Clone();
            for (int n = 0; n < pv.Length; n++)
            {
                ulong move = pv[n];
                TtEval.Add(bd.Hash, (short)--depth, n, -short.MaxValue, short.MaxValue, Score, move);
                bd.MakeMove(move);
            }
        }

        public short ZwSearchTT(short beta, short depth, int ply)
        {
            short alpha = (short)(beta - 1);
            if (TtEval.TryGetScore(board.Hash, depth, ply, alpha, beta, out short score))
            {
                return score;
            }

            score = ZeroWindowSearch(beta, depth, ply);

            TtEval.Add(board.Hash, depth, ply, alpha, beta, score, 0ul);
            return score;
        }
        public short ZeroWindowSearch(short beta, short depth, int ply)
        {
            short alpha = (short)(beta - 1);

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;//??
            }

            if (depth <= 0)
            {
                return Quiesce(alpha, beta, ply);
            }

            nodesVisited++;

            MoveList moveList = moveListPool.Get();
            board.GenerateMoves(moveList);
            for (int n = 0; n < moveList.Count; n++)
            {
                if (board.MakeMove(moveList[n]))
                {
                    //short score = NegZeroWindowSearch(beta, (short)(depth - 1), ply + 1);
                    short score = (short)-ZwSearchTT((short)-beta, (short)(depth - 1), ply + 1);
                    board.UnmakeMove();
                    if (score > beta)
                    {
                        moveListPool.Return(moveList);
                        return beta;
                    }
                }
            }

            moveListPool.Return(moveList);
            return alpha;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short NegQuiesce(short alpha, short beta, int ply)
        {
            return (short)-Quiesce((short)-beta, (short)-alpha, ply);
        }

        public short Quiesce(short alpha, short beta, int ply)
        {
            nodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            if (board.HalfMoveClock >= Constants.MAX_PLY_WITHOUT_PAWN_MOVE_OR_CAPTURE)
            {
                return 0;
            }

            short standPat = Evaluation.Compute(board);
            if (ply >= Constants.MAX_PLY - 1)
            {
                return standPat;
            }

            bool inCheck = board.IsChecked();
            if (!inCheck)
            {
                if (standPat >= beta)
                {
                    return beta;
                }
                alpha = Math.Max(alpha, standPat);
            }

            MoveList moveList = moveListPool.Get();

            if (inCheck)
            {
                board.GenerateMoves(moveList);
            }
            else
            {
                board.GenerateCaptures(moveList);
            }

            int legalMoves = 0;
            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                ulong move = moveList[n];

                // "delta" pruning https://www.chessprogramming.org/Delta_Pruning
                Piece capture = Move.GetCapture(move);
                bool isCapture = capture != Piece.None;
                bool isPromote = Move.GetPromote(move) != Piece.None;
                short pieceValue = (short)(capture != Piece.None ? Evaluation.CanonicalPieceValues[(int)capture] : 0);

                if (!inCheck && !isPromote && !isCapture &&
                    (board.TotalMaterial - pieceValue > Evaluation.EndGamePhaseMaterial) &&
                    standPat + pieceValue + DELTA_PRUNING_MARGIN < alpha)
                {
                    continue;
                }

                if (board.MakeMove(move))
                {
                    short score = NegQuiesce(alpha, beta, ply + 1);
                    board.UnmakeMove();
                    
                    if (score >= beta)
                    {
                        moveListPool.Return(moveList);
                        return beta;
                    }

                    alpha = Math.Max(alpha, score);
                    legalMoves++;
                }
            }

            moveListPool.Return(moveList);

            if (inCheck)
            {
                if (legalMoves == 0)
                {
                    return (short)(-Constants.CHECKMATE_SCORE + ply);
                }

                return 0;
            }
            return alpha;
        }

        public ulong[] CollectPV(ulong[] pv, int depth)
        {
            List<ulong> result = new(pv);
            if (result.Count < depth)
            {
                Board bd = board.Clone();
                foreach (ulong move in pv)
                {
                    if (!bd.IsLegalMove(move))
                    {
                        throw new ArgumentException($"Invalid move in PV '{Move.ToString(move)}'.");
                    }

                    bd.MakeMove(move);
                }

                while (result.Count < depth && TtEval.TryGetBestMove(bd.Hash, out ulong bestMove) && bd.IsLegalMove(bestMove))
                {
                    bd.MakeMove(bestMove);
                    result.Add(bestMove);
                }
            }

            return result.ToArray();
        }

        private static bool IsCheckmate(short score, out int mateIn)
        {
            mateIn = 0;
            short absScore = Math.Abs(score);
            bool checkMate = absScore >= Constants.CHECKMATE_SCORE - Constants.MAX_PLY;
            if (checkMate)
            {
                mateIn = (Constants.CHECKMATE_SCORE - absScore) * Math.Sign(score);
            }

            return checkMate;
        }

        private static readonly ulong[] emptyPV = Array.Empty<ulong>();
        private static readonly (short Score, ulong[] PV) defaultResult = (0, emptyPV);
        private static (short Score, ulong[] PV) SearchResult(short score) => (score, emptyPV);

        private const int CHECK_TC_NODES_MASK = 0x03f;
        private const short DELTA_PRUNING_MARGIN = 200;
        
        private readonly Board board;
        private readonly TimeControl time;
        private readonly long maxNodes;
        private short maxSearchDepth;
        private long nodesVisited = 0L;
        private readonly ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);
        private History history = new();
        private readonly ulong[][] killers = Mem.Allocate2D<ulong>(Constants.MAX_PLY, 2);
        private bool wasAborted = false;

        private static readonly short[] futilityMargin =
        {
            0,
            Evaluation.CanonicalPieceValues[(int)Piece.Pawn],
            Evaluation.CanonicalPieceValues[(int)Piece.Knight],
            Evaluation.CanonicalPieceValues[(int)Piece.Rook]
        };
    }
}
