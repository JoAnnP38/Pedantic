using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Collections;
using Pedantic.Genetics;

namespace Pedantic.Chess
{
    public class MtdSearchNew : BasicSearch
    {
        public MtdSearchNew(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100) 
            : base(board, time, maxSearchDepth, maxNodes)
        { }

        public override void Search()
        {
            Engine.Color = board.SideToMove;
            Depth = 0;
            ulong? ponderMove = null;
            bool oneLegalMove = board.OneLegalMove(out ulong bestMove);
            ulong[] pv = EmptyPv;
            evaluation.CalcMaterialAdjustment(board);
            int guess = Quiesce(-short.MaxValue, short.MaxValue, 0);

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() &&
                   (!IsCheckmate(guess, out int mateIn) || Math.Abs(mateIn) * 2 >= Depth))
            {
                time.StartInterval();
                history.Rescale();
                UpdateTtWithPv(pv, Depth);

                guess = Mtd(guess, Depth, 0, ref pv);

                if (wasAborted)
                {
                    break;
                }

                ReportSearchResults(guess, out pv, ref bestMove, ref ponderMove);

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
            pv = EmptyPv;

            do
            {
                int beta = guess != lowerBound ? guess : guess + 1;
                guess = ZwSearchTt(beta, depth, ply);
                ExtractPv(ref pv);

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

                guess = (lowerBound + upperBound + 1) >> 1;
            } while (lowerBound < upperBound - searchMargin && !wasAborted);

            return guess;
        }

        protected int ZwQuiesceTt(int beta, int depth, int ply)
        {
            int score;
            if (TtTran.TryLookup(board.Hash, depth, out TtTran.TtTranItem item))
            {
                score = item.Score;
                if (Evaluation.IsCheckmate(score))
                {
                    score -= Math.Sign(score) * ply;
                }

                if (item.Flag == TtTran.TtFlag.LowerBound && score >= beta)
                {
                    return score;
                }

                if (item.Flag == TtTran.TtFlag.UpperBound && score < beta)
                {
                    return score;
                }
            }

            score = ZwQuiesce(beta, depth, ply);

            if (wasAborted)
            {
                return 0;
            }

            TtTran.Add(board.Hash, depth, ply, beta, score, 0ul);
            return score;
        }

        protected int ZwSearchTt(int beta, int depth, int ply)
        {
            int score;
            if (TtTran.TryLookup(board.Hash, depth, out TtTran.TtTranItem item))
            {
                score = item.Score;
                if (Evaluation.IsCheckmate(score))
                {
                    score -= Math.Sign(score) * ply;
                }

                if (item.Flag == TtTran.TtFlag.LowerBound && score >= beta)
                {
                    return score;
                }

                if (item.Flag == TtTran.TtFlag.UpperBound && score < beta)
                {
                    return score;
                }
            }

            score = ZwSearch(beta, depth, ply);

            if (wasAborted)
            {
                return 0;
            }

            TtTran.Add(board.Hash, depth, ply, beta, score, 0ul);
            return score;
        }

        protected int ZwQuiesce(int beta, int depth, int ply)
        {
            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            NodesVisited++;

            if (IsDraw())
            {
                return Contempt;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board);
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
            }

            int expandedNodes = 0;
            int bestScoreSoFar = -short.MaxValue;
            ulong bestMove = 0ul;
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
                int score = -ZwQuiesceTt(-beta + 1, depth - 1,ply + 1);
                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (score > bestScoreSoFar)
                {
                    bestScoreSoFar = score;
                    bestMove = move;

                    if (score >= beta)
                    {
                        break;
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
            TtTran.Add(board.Hash, depth, ply, beta, bestScoreSoFar, bestMove);
            return bestScoreSoFar;
        }

        protected int ZwSearch(int beta, int depth, int ply)
        {
            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            NodesVisited++;

            if (IsDraw())
            {
                return Contempt;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board);
            }

            beta = Math.Min(beta, Constants.CHECKMATE_SCORE - ply);

            if (depth <= 0)
            {
                return ZwQuiesceTt(beta, 0, ply);
            }

            bool inCheck = board.IsChecked();
            int expandedNodes = 0;
            history.SideToMove = board.SideToMove;
            MoveList moveList = MoveListPool.Get();
            int bestScoreSoFar = -short.MaxValue;
            ulong bestMove = 0ul;

            foreach (ulong move in board.Moves(ply, killerMoves, history, moveList))
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;
                int score = -ZwSearchTt(-beta + 1, depth - 1, ply + 1);
                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (score > bestScoreSoFar)
                {
                    bestMove = move;
                    bestScoreSoFar = score;

                    if (score >= beta)
                    {
                        if (!Move.IsCapture(move))
                        {
                            killerMoves.Add(move, ply);
                            history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                        }

                        break;
                    }
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

            TtTran.Add(board.Hash, depth, ply, beta, bestScoreSoFar, bestMove);
            return bestScoreSoFar;
        }

        protected void ExtractPv(ref ulong[] pv)
        {
            Span<ulong> pvExtract = stackalloc ulong[Constants.MAX_PLY];
            int pvInsert = 0;

            Board bd = board.Clone();

            while (TtTran.TryGetBestMove(bd.Hash, out ulong bestMove) && bd.IsLegalMove(bestMove))
            {
                pvExtract[pvInsert++] = bestMove;
                if (pv.Length < pvInsert || pv[pvInsert - 1] != bestMove)
                {
                    pv = EmptyPv;
                }

                bd.MakeMove(bestMove);
            }

            if (pv.Length < pvInsert)
            {
                pv = pvExtract[..pvInsert].ToArray();
            }
        }

        private const int search_granularity = 0;
    }
}
