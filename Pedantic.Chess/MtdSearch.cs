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
            int guess = evaluation.Compute(board);
            ulong[] pv = Array.Empty<ulong>();

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() && Math.Abs(Result.Score) != Constants.CHECKMATE_SCORE)
            {
                time.StartInterval();
                history.Rescale();
                UpdateTtWithPv(pv, Depth);

                guess = Mtd(guess, Depth, 0);

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
            Uci.BestMove(bestMove, ponderMove);
        }

        protected int Mtd(int f, int depth, int ply)
        {
            int guess = f;
            int searchMargin = search_granularity;
            int lowerBound = -Constants.CHECKMATE_SCORE;
            int upperBound = Constants.CHECKMATE_SCORE;

            do
            {
                int beta = guess != lowerBound ? guess : guess + 1;
                guess = ZwSearch(beta, depth, ply);

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

        private const int search_granularity = 0;
    }
}
