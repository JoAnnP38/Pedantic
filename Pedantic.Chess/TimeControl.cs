using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public sealed class TimeControl
    {
        const int time_margin = 25;
        const int branching_factor_estimate = 3;
        const int max_time_remaining = int.MaxValue / 3;

        private int movesToGo;
        private int increment;
        private int remaining;
        private long t0 = -1;
        private long tN = -1;

        public int TimePerMoveWithMargin => (remaining + (movesToGo - 1) * increment) / movesToGo - time_margin;
        public int TimeRemainingWithMargin => remaining - time_margin;

        private long Now => Stopwatch.GetTimestamp();
        public long Elapsed => MilliSeconds(Now - t0);
        public long ElapsedInterval => MilliSeconds(Now - tN);
        public bool Infinite { get; set; } = false;

        private long MilliSeconds(long ticks)
        {
            return (ticks * 1000L) / Stopwatch.Frequency;
        }

        public void Reset()
        {
            movesToGo = 1;
            increment = 0;
            remaining = max_time_remaining;
            t0 = Now;
            tN = t0;
        }

        public void StartInterval()
        {
            tN = Now;
        }

        public void Stop()
        {
            Infinite = false;
            remaining = 0;
        }

        internal void Go(int timePerMove)
        {
            Reset();
            remaining = Math.Min(timePerMove, max_time_remaining);
        }

        internal void Go(int time, int increment, int movesToGo)
        {
            Reset();
            remaining = Math.Min(time, max_time_remaining);
            this.increment = increment;
            this.movesToGo = movesToGo;
        }

        public bool CanSearchDeeper()
        {
            long elapsed = Elapsed;

            if (!Infinite)
            {
                //estimate the branching factor, if only one move to go we yolo with a low estimate
                int multi = (movesToGo == 1) ? 1 : branching_factor_estimate;
                long estimate = multi * ElapsedInterval;
                long total = elapsed + estimate;

                //no increment... we need to stay within the per-move time budget
                if (increment == 0 && total > TimePerMoveWithMargin)
                    return false;
                //we have already exceeded the average move
                if (elapsed > TimePerMoveWithMargin)
                    return false;
                //shouldn't spend more then the 2x the average on a move
                if (total > (3 * TimePerMoveWithMargin) / 2)
                    return false;
                //can't afford the estimate
                if (total > TimeRemainingWithMargin)
                    return false;
            }

            //all conditions fulfilled
            return true;
        }

        public bool CheckTimeBudget()
        {
            if (!Infinite)
            {
                if (increment == 0)
                {
                    return Elapsed > TimePerMoveWithMargin;
                }

                return Elapsed > TimeRemainingWithMargin;
            }

            return false;
        }
    }
}
