using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Xsl;

namespace Pedantic.Chess
{
    public class MtdSearch : SearchBase
    {
        public MtdSearch(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100) 
            : base(board, time, maxSearchDepth, maxNodes)
        { }

        public override SearchResult Search(int alpha, int beta, int depth, int ply, bool canNull = true, bool isPv = true)
        {
            throw new NotImplementedException();
        }

        public override void Search()
        {
            Engine.Color = board.SideToMove;
            Depth = 0;
            ulong? ponderMove = null;
            bool oneLegalMove = board.OneLegalMove(out ulong bestMove);

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() && Math.Abs(Result.Score) != Constants.CHECKMATE_SCORE)
            {
                time.StartInterval();
                history.Rescale();
                ulong[] pv = Result.Pv;
                UpdateTtWithPv(pv, Depth);

                for (int i = 0; i < 2; i++)
                {
                    Result = Mtd(Result.Score, Depth, 0);

                    if (wasAborted)
                    {
                        break;
                    }

                    if (i == 0 && Result.Pv.Length == 0)
                    {
                        TtEval.Clear();
                        UpdateTtWithPv(pv, Depth);
                    }
                }

                if (wasAborted)
                {
                    break;
                }

                ReportSearchResults(ref bestMove, ref ponderMove);

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
                    ReportSearchResults(ref bestMove, ref ponderMove);
                }
            }
            Uci.BestMove(bestMove, ponderMove);
        }

        protected SearchResult Mtd(int f, int depth, int ply)
        {
            int guess = f;
            int lowerBound = -Constants.CHECKMATE_SCORE;
            int upperBound = Constants.CHECKMATE_SCORE;
            SearchResult result;
            do
            {
                int beta = guess != lowerBound ? guess : guess + 1;
                result = SearchTt(beta - 1, beta, depth, ply);
                guess = result.Score;

                if (guess < beta)
                {
                    upperBound = guess;
                }
                else
                {
                    lowerBound = guess;
                }

                guess = (lowerBound + upperBound + 1) / 2;
            } while (lowerBound < upperBound);

            return result;
        }

        protected SearchResult SearchTt(int alpha, int beta, int depth, int ply, bool canNull = true)
        {
            if (TtTran.TryGetScore(board.Hash, depth, ply, beta - 1, beta, out int score, out ulong move))
            {
                return new SearchResult(score, move);
            }

            SearchResult result = Search(alpha, beta, depth, ply, canNull);

            TtTran.Add(board.Hash, depth, ply, alpha, beta, result.Score, result.Pv.Length > 0 ? result.Pv[0] : 0);
            return result;
        }

        private SearchResult Search(int alpha, int beta, int depth, int ply, bool canNull = true)
        {
            if (IsDraw())
            {
                return new SearchResult(Contempt);
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return new SearchResult(evaluation.Compute(board));
            }

            if (depth <= 0)
            {
                return new SearchResult(QuiesceTt(alpha, beta, ply));
            }

            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return DefaultResult;
            }

            bool inCheck = board.IsChecked();
            bool canReduce = CalcExtension(inCheck) == 0;
            int eval = evaluation.Compute(board);

            if (canNull && depth > 2 && !inCheck && eval > beta &&
                board.HasMinorMajorPieces(board.OpponentColor, 600))
            {
                int reduction = depth > 6 ? 3 : 2;
                if (board.MakeMove(Move.NullMove))
                {
                    SearchResult result = -SearchTt(-beta, -beta + 1, depth - reduction - 1, ply + 1, false);
                    board.UnmakeMove();
                    if (result.Score >= beta)
                    {
                        return new SearchResult(beta);
                    }
                }
            }

            if (canNull && canReduce && !inCheck && depth <= 2)
            {
                int threshold = alpha - futilityMargin[depth];
                if (eval < threshold)
                {
                    int result = QuiesceTt(alpha, beta, ply);
                    if (result < threshold)
                    {
                        return new SearchResult(alpha);
                    }
                }
            }

            ulong[] pv = EmptyPv;
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
                bool interesting = expandedNodes == 1 || !canReduce || inCheck || checkingMove || !isQuiet;

                int reduction = 0;
                if (!interesting && depth > 2 && !killerMoves.Exists(ply, move))
                {
                    reduction = depth > 6 ? 2 : 1;
                }

                int newDepth = depth - reduction - 1;
                SearchResult result = -SearchTt(-beta, -alpha, newDepth, ply + 1);
                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (result.Score >= beta)
                {
                    pv = MergeMove(result.Pv, move);
                    TtTran.Add(board.Hash, depth, ply, alpha, beta, result.Score, move);
                    if (!Move.IsCapture(move))
                    {
                        killerMoves.Add(move, ply);
                        history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                    }

                    MoveListPool.Return(moveList);
                    return new SearchResult(beta, pv);
                }
            }

            MoveListPool.Return(moveList);

            if (wasAborted)
            {
                return DefaultResult;
            }

            if (expandedNodes == 0)
            {
                return new SearchResult(inCheck ? -Constants.CHECKMATE_SCORE + ply : Contempt);
            }

            return new SearchResult(alpha, pv);
        }
    }
}
