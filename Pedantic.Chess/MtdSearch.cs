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
        public MtdSearch(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100) 
            : base(board, time, maxSearchDepth, maxNodes)
        { }

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

                Result = Mtd(Result.Score, Depth, 0);

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
            int searchMargin = search_granularity;
            int lowerBound = -Constants.CHECKMATE_SCORE;
            int upperBound = Constants.CHECKMATE_SCORE;
            SearchResult result;
            do
            {
                int beta = guess != lowerBound ? guess : guess + 1;
                result = Search(beta - 1, beta, depth, ply);
                guess = result.Score;
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
            } while (lowerBound < upperBound - searchMargin);

            return result;
        }

        public override SearchResult Search(int alpha, int beta, int depth, int ply, bool canNull = true, bool isPv = true)
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
            int X = CalcExtension(inCheck);
            bool canReduce = X == 0;
            int eval = evaluation.Compute(board);

            if (canNull && canReduce && depth > 2 && !inCheck && eval > beta &&
                board.HasMinorMajorPieces(board.OpponentColor, 600)) // improve with incremental count of pieces
            {
                int R = depth > 8 ? 3 : 2;
                if (board.MakeMove(Move.NullMove))
                {
                    SearchResult result = -SearchTt(-beta, -beta + 1, depth - R - 1, ply + 1, false, false);
                    board.UnmakeMove();
                    if (result.Score >= beta)
                    {
                        return new SearchResult(beta);
                    }
                }
            }

            if (canNull && canReduce && !inCheck && depth < 3)
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

            bool canPrune = depth <= 2 && !inCheck && Math.Abs(alpha) < Constants.CHECKMATE_BASE &&
                            canReduce && eval + futilityMargin[depth] <= alpha;

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
                bool isRecapture = IsRecapture(move);
                bool interesting = expandedNodes == 1 || !canReduce || inCheck || checkingMove || isRecapture;

                if (canPrune && isQuiet && !interesting)
                {
                    board.UnmakeMove();
                    continue;
                }

                int R = 0;
                if (canReduce && !interesting && isQuiet && !killerMoves.Exists(ply, move))
                {
                    R = depth > 8 ? 2 : 1;
                }

                SearchResult r = -SearchTt(-alpha - 1, -alpha, depth - R - 1, ply + 1, true, false);

                if (wasAborted)
                {
                    board.UnmakeMove();
                    break;
                }

                if (r.Score <= alpha)
                {
                    board.UnmakeMove();
                    continue;
                }

                int extension = X;
                if (isRecapture && extension == 0)
                {
                    extension = 1;
                }

                SearchResult result = -SearchTt(-beta, -alpha, depth + extension - 1, ply + 1, true, isPv);
                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (result.Score > alpha)
                {
                    pv = MergeMove(result.Pv, move);
                    alpha = result.Score;

                    if (result.Score >= beta)
                    {
                        if (!Move.IsCapture(move))
                        {
                            killerMoves.Add(move, ply);
                            history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                        }

                        MoveListPool.Return(moveList);
                        return new SearchResult(beta, pv);
                    }
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

        private const int search_granularity = 16;
    }
}
