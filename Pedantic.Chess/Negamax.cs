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
using Pedantic.Collections;
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
            Engine.Color = board.SideToMove;
            Depth = 0;
            ulong? ponderMove = null;

            if (board.OneLegalMove(out ulong bestMove))
            {
                Uci.BestMove(bestMove);
                return;
            }

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() && !Evaluation.IsCheckmate(Score))
            {

                time.StartInterval();
                history.Rescale();
                UpdateTtWithPV(PV, Depth);

                (Score, PV) = Search(-Constants.INFINITE_WINDOW, Constants.INFINITE_WINDOW, Depth);

                if (wasAborted)
                {
                    break;
                }

                ReportSearchResults(out bestMove, out ponderMove);
            }

            bool waiting = false;
            while (time.CanSearchDeeper() && !wasAborted)
            {
                waiting = true;
                Thread.Sleep(WAIT_TIME);
            }

            if (waiting)
            {
                ReportSearchResults(out bestMove, out ponderMove);
            }
            Uci.BestMove(bestMove, ponderMove);
        }

        public void ReportSearchResults(out ulong bestMove, out ulong? ponderMove)
        {
            PV = ExtractPv(PV, Depth);
            if (IsCheckmate(Score, out int mateIn))
            {
                Uci.InfoMate(Depth, mateIn, nodesVisited, time.Elapsed, PV);
            }
            else
            {
                Uci.Info(Depth, Score, nodesVisited, time.Elapsed, PV);
            }

            bestMove = PV[0];
            if (PV.Length > 1)
            {
                ponderMove = PV[1];
            }
            else
            {
                ponderMove = null;
            }
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

            if (depth <= 0)
            {
                return SearchResult(Quiesce(alpha, beta, ply));
            }

            nodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return defaultResult;
            }

            int extension = 0;
            
            bool inCheck = board.IsChecked();
            if (inCheck)
            {
                extension = 1;
            }

            bool allowNullMove = !Evaluation.IsCheckmate(Score) || ply > (Depth / 4);

            if (allowNullMove && ply > 0 && !inCheck && depth >= 2 && beta < Constants.INFINITE_WINDOW)
            {
                if (board.MakeMove(Move.NullMove))
                {
                    short R = (short)(depth <= 6 ? 2 : 3);
                    (short score, ulong[] _) result = NegSearchTT((short)(beta - 1), beta, (short)(depth - R - 1),
                        ply + 1);
                    board.UnmakeMove();
                    if (result.score >= beta)
                    {
                        return SearchResult(result.score);
                    }
                }
            }

            history.SideToMove = board.SideToMove;
            MoveList moveList = moveListPool.Get();
            int legalMoves = 0;

            foreach (ulong move in board.Moves(ply, killerMoves, history, moveList))
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                legalMoves++;
                bool interesting = legalMoves == 1 || board.IsChecked() || inCheck;

                if (depth <= 4 && !interesting)
                {
                    short eval = Evaluation.Compute(board);
                    if (depth * MAX_GAIN_PER_PLY + eval <= alpha)
                    {
                        board.UnmakeMove();
                        continue;
                    }
                }

                short newDepth = (short)(depth - 1 + extension);

                if (depth >= 2 && legalMoves > 1)
                {
                    short reduction = (short)((interesting || legalMoves <= 4) ? 0 : 2);
                    newDepth -= reduction;
                    if (NegSearchTT(alpha, (short)(alpha + 1), newDepth, ply + 1).Score <= alpha)
                    {
                        board.UnmakeMove();
                        continue;
                    }
                }

                (short score, ulong[] pv) value = NegSearchTT(alpha, beta, (short)(depth + extension - 1), ply + 1);

                board.UnmakeMove();

                if (value.score > alpha)
                {
                    TtEval.Add(board.Hash, depth, ply, alpha, beta, value.score, move);
                    alpha = value.score;
                    pv = MergeMove(value.pv, move);

                    bool isCapture = Move.GetCapture(move) != Piece.None;
                    if (!isCapture)
                    {
                        history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                    }

                    if (value.score >= beta)
                    {
                        if (!isCapture)
                        {
                            killerMoves.Add(move, ply);
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
            foreach (ulong move in board.Moves(ply, killerMoves, history, moveList))
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }
                short score = (short)-ZwSearchTT((short)-beta, (short)(depth - 1), ply + 1);
                board.UnmakeMove();
                if (score > beta)
                {
                    moveListPool.Return(moveList);
                    return beta;
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

            var moves = inCheck ? board.Moves(ply, killerMoves, history, moveList) : board.CaptureMoves(moveList);

            int legalMoves = 0;
            foreach (ulong move in moves)
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }
                legalMoves++;

#if false
                // "delta" pruning https://www.chessprogramming.org/Delta_Pruning
                Piece capture = Move.GetCapture(move);
                Piece promote = Move.GetPromote(move);
                bool isCapture = capture != Piece.None;
                bool isPromote = promote != Piece.None;
                short pieceValue = (short)(isCapture ? Evaluation.CanonicalPieceValues[(int)capture] : 0);
                short promoteValue = (short)(isPromote ? Evaluation.CanonicalPieceValues[(int)promote] : 0);

                short score;
                if (!inCheck && (isPromote || isCapture) &&
                    (board.TotalMaterial - pieceValue + promoteValue > Evaluation.EndGamePhaseMaterial) &&
                    standPat + pieceValue + promoteValue + DELTA_PRUNING_MARGIN < alpha)
                {
                    score = alpha;
                }
                else
                {
                    score = NegQuiesce(alpha, beta, ply + 1);
                }
#else
                short score = NegQuiesce(alpha, beta, ply + 1);
#endif
                board.UnmakeMove();
                
                if (score >= beta)
                {
                    moveListPool.Return(moveList);
                    return beta;
                }

                alpha = Math.Max(alpha, score);
            }

            if (legalMoves == 0)
            {
                if (inCheck)
                {
                    alpha = (short)(-Constants.CHECKMATE_SCORE + ply);
                }
                else if (!board.HasLegalMoves(moveList))
                {
                    alpha = 0;
                }
            }

            moveListPool.Return(moveList);

            return alpha;
        }

        public ulong[] ExtractPv(ulong[] pv, int depth)
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
            bool checkMate = absScore >= Constants.CHECKMATE_BASE;
            if (checkMate)
            {
                mateIn = ((Constants.CHECKMATE_SCORE - absScore + 1) / 2) * Math.Sign(score);
            }

            return checkMate;
        }

        private static readonly ulong[] emptyPV = Array.Empty<ulong>();
        private static readonly (short Score, ulong[] PV) defaultResult = (0, emptyPV);
        private static (short Score, ulong[] PV) SearchResult(short score) => (score, emptyPV);

        private const int CHECK_TC_NODES_MASK = 31;
        private const short DELTA_PRUNING_MARGIN = 200;
        private const int MAX_GAIN_PER_PLY = 100;
        private const int WAIT_TIME = 50;
        
        private readonly Board board;
        private readonly TimeControl time;
        private long maxNodes;
        private short maxSearchDepth;
        private long nodesVisited = 0L;
        private readonly ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);
        private readonly History history = new();
        private readonly KillerMoves killerMoves = new();
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
