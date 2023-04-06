// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-28-2023
// ***********************************************************************
// <copyright file="TimeControl.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     A class designed to manage the time the engine spends calculating
//     its next move. This class was blatantly ripped off of the 
//     <see href="https://github.com/lithander/MinimalChessEngine">
//         MinimalChess
//     </see> engine.
// </summary>
// ***********************************************************************
using System.Diagnostics;

namespace Pedantic.Chess
{
    public sealed class TimeControl : ICloneable
    {
        private const int time_margin = 50;
        // changed branching factor from 2.5 to 2.125 +27 Elo
        private const int branching_factor_estimate = 17; 
        private const int branching_factor_denominator = 3;
        private const int max_time_remaining = int.MaxValue / 3;

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
        public long Elapsed => Milliseconds(Now - t0);
        public long ElapsedInterval => Milliseconds(Now - tN);

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

        private static long Milliseconds(long ticks)
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
            infinite = false;
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

        public void Go(int timePerMove, bool ponder = false)
        {
            Reset();
            remaining = Math.Min(timePerMove, max_time_remaining);
            infinite = ponder;
        }

        public void Go(int time, int increment, int movesToGo, bool ponder= false)
        {
            Reset();
            remaining = Math.Min(time, max_time_remaining);
            this.increment = increment;
            this.movesToGo = movesToGo;
            infinite = ponder;
        }

        public bool CanSearchDeeper()
        {
            long elapsed = Elapsed;

            if (!Infinite)
            {
                //estimate the branching factor
                long estimate;
                if (movesToGo == 1)
                {
                    estimate = ElapsedInterval;
                }
                else
                {
                    estimate = (ElapsedInterval * branching_factor_estimate) >> branching_factor_denominator;
                }
                long total = elapsed + estimate;

                //no increment... we need to stay within the per-move time budget
                if (increment == 0 && total > TimePerMoveWithMargin)
                    return false;
                //we have already exceeded the average move
                if (elapsed > TimePerMoveWithMargin)
                    return false;
                //shouldn't spend more then the 2x the average on a move (get rid of this???)
                if (total > 2 * TimePerMoveWithMargin)
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
            return new TimeControl(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
