using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Xsl;

namespace Pedantic.Chess
{
    public class MtdSearch : BasicSearch
    {
        private PvList pvList = new();

        public MtdSearch(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100) 
            : base(board, time, maxSearchDepth, maxNodes)
        { }

        public override void Search()
        {
            Engine.Color = board.SideToMove;
            Depth = 0;
            ulong? ponderMove = null;
            bool oneLegalMove = board.OneLegalMove(out ulong bestMove);
            evaluation.CalcMaterialAdjustment(board);
            int guess = evaluation.Compute(board);
            ulong[] pv = Array.Empty<ulong>();

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() && (!IsCheckmate(guess, out int mateIn) || Math.Abs(mateIn) * 2 >= Depth))
            {
                time.StartInterval();
                history.Rescale();
                pvList.Clear();
                UpdateTtWithPv(pv, Depth);

                guess = Mtd(guess, Depth, 0, ref pv);

                if (wasAborted)
                { 
                    break;
                }

                ReportSearchResults(guess, ref pv, ref bestMove, ref ponderMove);

                if (Depth == 5 && oneLegalMove)
                {
                    break;
                }
            }

            if (Pondering)
            {
                bool waiting = false;
                while (time.CanSearchDeeper() && !wasAborted)
                {
                    waiting = true;
                    Thread.Sleep(WAIT_TIME);
                }

                if (waiting)
                {
                    ReportSearchResults(guess, out pv, ref bestMove, ref ponderMove);
                }
            }
            Uci.BestMove(bestMove, CanPonder ? ponderMove : null);
        }

        protected int Mtd(int f, int depth, int ply, ref ulong[] pv)
        {
            int guess = f;
            int searchMargin = search_granularity;
            int lowerBound = -Constants.CHECKMATE_SCORE;
            int upperBound = Constants.CHECKMATE_SCORE;
            pv = Array.Empty<ulong>();

            do
            {
                int beta = guess != lowerBound ? guess : guess + 1;
                guess = ZwSearchTt(beta, depth, ply);

                if (pvList.Pv.Length > 0)
                {
                    pv = pvList.Pv.ToArray();
                }

                if (Evaluation.IsCheckmate(guess))
                {
                    searchMargin = 0;
                }

                if (guess < beta)
                {
                    upperBound = guess;
                }
                else
                {
                    lowerBound = guess;
                }

                guess = (lowerBound + upperBound + 1) / 2;
            } while (lowerBound < upperBound - searchMargin && !wasAborted);

            return guess;
        }

        public override int Quiesce(int alpha, int beta, int ply)
        {
            pvList.AdvancePly(ply);

            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board);
            }

            if (IsDraw())
            {
                return Contempt;
            }

            history.SideToMove = board.SideToMove;
            bool inCheck = board.IsChecked();

            if (!inCheck)
            {
                int standPatScore = evaluation.Compute(board);
                if (standPatScore >= beta)
                {
                    return standPatScore;
                }

                alpha = Math.Max(alpha, standPatScore);
            }

            int expandedNodes = 0;
            MoveList moveList = MoveListPool.Get();
            IEnumerable<ulong> moves =
                inCheck ? board.Moves(ply, killerMoves, history, moveList) : board.CaptureMoves(moveList);

            foreach (ulong move in moves)
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;
                int score = -Quiesce(-beta, -alpha, ply + 1);
                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (score > alpha)
                {
                    alpha = score;
                    pvList.Store(ply, move);
                    if (score >= beta)
                    {
                        MoveListPool.Return(moveList);
                        return beta;
                    }
                }
            }

            if (wasAborted)
            {
                MoveListPool.Return(moveList);
                return 0;
            }

            if (expandedNodes == 0)
            {
                if (inCheck)
                {
                    MoveListPool.Return(moveList);
                    return -Constants.CHECKMATE_SCORE + ply;
                }

                if (!board.HasLegalMoves(moveList))
                {
                    MoveListPool.Return(moveList);
                    return Contempt;
                }
            }

            MoveListPool.Return(moveList);
            return alpha;
        }

        protected override int ZwSearch(int beta, int depth, int ply, bool canNull = true)
        {
            pvList.AdvancePly(ply);

            if (IsDraw())
            {
                return Contempt;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board);
            }

            int alpha = Math.Max(beta - 1, -Constants.CHECKMATE_SCORE + ply - 1);
            beta = Math.Min(beta, Constants.CHECKMATE_SCORE - ply);
            if (alpha >= beta)
            {
                return alpha;
            }

            if (depth <= 0)
            {
                return Quiesce(beta - 1, beta, ply);
            }

            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            bool inCheck = board.IsChecked();
            int X = CalcExtension(inCheck);
            bool canReduce = X == 0;
            int eval = evaluation.Compute(board);

            if (canNull && canReduce && depth >= 3 && eval >= beta &&
                board.HasMinorMajorPieces(board.OpponentColor, 600))
            {
                int R = nmp[depth];
                if (board.MakeMove(Move.NullMove))
                {
                    int result = -ZwSearchTt(1 - beta, depth - R - 1, ply + 1, false);
                    board.UnmakeMove();

                    if (wasAborted)
                    {
                        return 0;
                    }

                    if (result >= beta)
                    {
                        return beta;
                    }
                }
            }

            if (canNull && canReduce && depth <= 2 && !Move.IsPawnMove(board.LastMove))
            {
                int threshold = alpha - 300 * depth;
                if (eval < threshold)
                {
                    int score = Quiesce(alpha, beta, ply);
                    if (score < threshold)
                    {
                        return alpha;
                    }
                }
            }

            bool canPrune = canReduce && Math.Abs(beta - 1) < Constants.CHECKMATE_BASE && depth < 8 &&
                            eval + futilityMargin[depth] <= beta - 1;

#if DEBUG
            string fen = board.ToFenString();
#endif
            int expandedNodes = 0;
            history.SideToMove = board.SideToMove;
            MoveList moveList = MoveListPool.Get();

            foreach (ulong move in board.Moves(ply, killerMoves, history, moveList))
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;

                bool checkingMove = board.IsChecked();
                bool isQuiet = Move.IsQuiet(move);
                bool interesting = inCheck || checkingMove || !isQuiet;

                if (canPrune && !interesting && expandedNodes > lmp[depth])
                {
                    board.UnmakeMove();
                    continue;
                }

                int R = 0;
                if (canReduce && !interesting && !killerMoves.Exists(ply, move))
                {
                    R = lmr[Math.Min(depth, 31)][Math.Min(expandedNodes, 63)];
                }

                int score;
                for (;;)
                {
                    score = -ZwSearchTt(-beta + 1, depth + X - R - 1, ply + 1);

                    if (R > 0 && score >= beta)
                    {
                        R = 0;
                    }
                    else
                    {
                        break;
                    }
                }

                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (score >= beta)
                {
                    pvList.Store(ply, move);
                    TtTran.Add(board.Hash, depth, ply, beta - 1, beta, score, move);
                    if (Move.IsQuiet(move))
                    {
                        killerMoves.Add(move, ply);
                        history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                    }

                    MoveListPool.Return(moveList);
                    return beta;
                }
            }

            MoveListPool.Return(moveList);

            if (wasAborted)
            {
                return 0;
            }

            if (expandedNodes == 0)
            {
                return inCheck ? -Constants.CHECKMATE_SCORE + ply : Contempt;
            }

            return beta - 1;
        }
        protected override int ZwSearchTt(int beta, int depth, int ply, bool canNull = true)
        {
            int alpha = beta - 1;
            if (TtTran.TryGetScore(board.Hash, depth, ply, ref alpha, ref beta, out int score, out ulong move))
            {
                return score;
            }

            score = ZwSearch(beta, depth, ply, canNull);

            TtTran.Add(board.Hash, depth, ply, alpha, beta, score, pvList.Pv.Length > ply ? pvList.Pv[ply] : move);
            return score;
        }


        protected void ReportSearchResults(int score, ref ulong[] pv, ref ulong bestMove, ref ulong? ponderMove)
        {
            pv = ExtractPv(pv, Depth);
            if (pv.Length > 0)
            {
                bestMove = pv[0];
                if (pv.Length > 1)
                {
                    ponderMove = pv[1];
                }
                else
                {
                    ponderMove = null;
                }
            }
            else
            {
                pv = EmptyPv;
                if (board.IsLegalMove(bestMove))
                {
                    if (ponderMove != null)
                    {
                        board.MakeMove(bestMove);
                        if (board.IsLegalMove(ponderMove.Value))
                        {
                            pv = MergeMove(MergeMove(pv, ponderMove.Value), bestMove);
                        }
                        else
                        {
                            pv = MergeMove(pv, bestMove);
                        }

                        board.UnmakeMove();
                    }
                }
            }

            if (pv.Length > pvList.Pv.Length)
            {
                for (int n = pvList.Pv.Length; n < pv.Length; n++)
                {
                    pvList.Append(0, pv[n]);
                }
            }

            if (IsCheckmate(score, out int mateIn))
            {
                Uci.InfoMate(Depth, mateIn, NodesVisited, time.Elapsed, pv);
            }
            else
            {
                Uci.Info(Depth, score, NodesVisited, time.Elapsed, pv);
            }
        }

        private const int search_granularity = 5;
    }
}
