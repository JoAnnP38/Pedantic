using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public sealed class TimeControl : ICloneable
    {
        const int time_margin = 25;
        const int branching_factor_estimate = 3;
        const int max_time_remaining = int.MaxValue / 3;

        private int movesToGo;
        private int increment;
        private int remaining;
        private long t0 = -1;
        private long tN = -1;
        private bool infinite;
        private readonly object lockObject = new();

        public TimeControl()
        {}

        private TimeControl(TimeControl other)
        {
            movesToGo = other.movesToGo;
            increment = other.increment;
            remaining = other.remaining;
            t0 = other.t0;
            tN = other.tN;
            infinite = other.infinite;
        }

        public int TimePerMoveWithMargin => (remaining + (movesToGo - 1) * increment) / movesToGo - time_margin;
        public int TimeRemainingWithMargin => remaining - time_margin;
        private long Now => Stopwatch.GetTimestamp();
        public long Elapsed => MilliSeconds(Now - t0);
        public long ElapsedInterval => MilliSeconds(Now - tN);

        public bool Infinite
        {
            get
            {
                lock (lockObject)
                {
                    return infinite;
                }
            }
            set
            {
                lock (lockObject)
                {
                    infinite = value;
                }
            }
        }

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

        public void Go(int timePerMove)
        {
            Reset();
            remaining = Math.Min(timePerMove, max_time_remaining);
        }

        public void Go(int time, int increment, int movesToGo)
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
                //estimate the branching factor, but even if only 1 move to go don't 
                // use more than half of remaining time
                int multi = (movesToGo == 1) ? /*1*/ 2 : branching_factor_estimate;
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

                return Elapsed > (TimeRemainingWithMargin >> 1);
            }

            return false;
        }

        public TimeControl Clone()
        {
            return new(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
