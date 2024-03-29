﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public sealed class GameClock : ICloneable
    {
        public GameClock()
        {
            Reset();
        }

        private GameClock(GameClock other)
        {
            t0 = other.t0;
            tN = other.tN;
            timeBudget = other.timeBudget;
            adjustedBudget = other.adjustedBudget;
            timeLimit = other.timeLimit;
            absoluteLimit = other.absoluteLimit;
            difficulty = other.difficulty;
            remaining = other.remaining;
        }

        public bool Infinite { get; set; }
        public static long Now => Stopwatch.GetTimestamp();
        public int Elapsed => Milliseconds(Now - t0);
        public int ElapsedInterval => Milliseconds(Now - tN);
        public int TimeLimit => timeLimit;
        public Uci Uci
        {
            get => uci;
            set => uci = value;
        }

        public void Reset()
        {
            t0 = Now;
            tN = t0;
            timeBudget = 0;
            timeLimit = 0;
            absoluteLimit = 0;
            difficulty = 100;
            Infinite = false;
        }

        public void Stop()
        {
            Infinite = false;
            timeLimit = 0;
            absoluteLimit = 0;
        }

        public void StartInterval() => tN = Now;

        public void Go(int timePerMove, bool ponder = false)
        {
            Reset();
            timeBudget = 0;
            remaining = Math.Min(timePerMove, max_time_remaining);
            timeLimit = remaining - time_margin;
            absoluteLimit = remaining - time_margin;
            Infinite = ponder;
        }

        public void Go(int time, int opponentTime, int increment = 0, int movesToGo = -1, int movesOutOfBook = 10, bool ponder = false)
        {
            Reset();

            if (movesToGo <= 0)
            {
                if (increment <= 0)
                {
                    movesToGo = default_movestogo_sudden_death;
                }
                else
                {
                    movesToGo = ponder ? default_movestogo_ponder : default_movestogo;
                }
            }

            remaining = time;
            timeBudget = (time + movesToGo * increment) / movesToGo;

            int timeImbalance = 0;
            // if opponent is using significant less time (less than 30%) than we are then reduce time budget
            if ((opponentTime - time) * 10 / time >= 3)
            {
                // reduce time budget by 20%
                timeImbalance = (timeBudget * 2) / 10;
            }

            // if opponent is using significantly more time (more than 30%) than we are then increase time budget
            if ((time - opponentTime) * 10 / time >= 3)
            {
                // increase time budget by 20%
                timeImbalance = -(timeBudget * 2) / 10;
            }

            // give a bonus to move time for the first few moves following the conclusion of book moves
            int factor = 10;
            if (movesOutOfBook < 10)
            {
                // increase time budget by 0 - 100%
                factor = 20 - Math.Min(movesOutOfBook, 10);
            }

            // final adjusted time budget
            adjustedBudget = timeBudget - timeImbalance;
            adjustedBudget = (adjustedBudget * factor) / 10;

            // set the final move time limits
            timeLimit = Math.Max(adjustedBudget - time_margin, time_margin); 
            absoluteLimit = Math.Max(Math.Min(adjustedBudget * absolute_limit_factor, remaining / 2) - time_margin, time_margin);
            Infinite = ponder;
            Uci.Debug($"Starting TimeLimit: {timeLimit}, AbsoluteLimit: {absoluteLimit}");
        }

        public void AdjustTime(bool oneLegalMove, bool bestMoveChanged, int changes)
        {
            if (timeBudget == 0)
            {
                // don't adjust the budget set for analysis
                return;
            }

            if (oneLegalMove)
            {
                difficulty = 10;
            }
            else if (bestMoveChanged)
            {
                if (difficulty < 100)
                {
                    difficulty = 100 + changes * 20;

                }
                else
                {
                    difficulty = (difficulty * 80) / 100 + changes * 20;
                }

                difficulty = Math.Min(difficulty, difficulty_max_limit);
            }
            else
            {
                difficulty = (difficulty * 9) / 10;
                difficulty = Math.Max(difficulty, difficulty_min_limit);
            }

            int budget = (adjustedBudget * difficulty) / 100;

            // update time limits
            timeLimit = Math.Max(budget - time_margin, time_margin);
            absoluteLimit = Math.Max(Math.Min(budget * absolute_limit_factor, remaining / 2) - time_margin, time_margin);
            Uci.Debug($"Difficulty: {difficulty}, Adjusted TimeLimit: {timeLimit}, AbsoluteLimit: {absoluteLimit}");
        }

        public bool CanSearchDeeper()
        {
            int elapsed = Elapsed;
            if (Infinite)
            {
                return true;
            }

            if (timeBudget == 0 && elapsed < absoluteLimit)
            {
                return true;
            }

            int estimate = (ElapsedInterval * branch_factor_multiplier) / branch_factor_divisor;

            Uci.Debug($"CanSearchDeeper Elapsed: {elapsed}, Estimate: {estimate}, TimeLimit: {timeLimit}");
            if (elapsed + estimate <= timeLimit)
            {
                return true;
            }

            return false;
        }

        public bool CheckTimeBudget()
        {
            if (!Infinite)
            {
                return Elapsed > absoluteLimit;
            }

            return false;
        }

        private static int Milliseconds(long ticks) => (int)((ticks * 1000L) / Stopwatch.Frequency);

        public GameClock Clone()
        {
            return new(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private const int time_margin = 25;
        private const int branch_factor_multiplier = 30; /* A: 28, B: 30, C: 32 */
        private const int branch_factor_divisor = 16;
        private const int max_time_remaining = int.MaxValue / 3;
        private const int default_movestogo = 30;
        private const int default_movestogo_ponder = 35;
        private const int default_movestogo_sudden_death = 40;
        private const int absolute_limit_factor = 5;
        private const int difficulty_max_limit = 200;
        private const int difficulty_min_limit = 60;

        private long t0;
        private long tN;
        private int timeBudget;         // time per move budget
        private int adjustedBudget;     // adjusted time budget
        private int timeLimit;          // time limit that governs ID
        private int absoluteLimit;      // time limit that represents the absolute limit on search time
        private int difficulty;         // a quantity that reflects difficulty of position
        private int remaining;
        private Uci uci = Uci.Default;
    }
}
