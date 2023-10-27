// ***********************************************************************
// Assembly         : Pedantic
// Author           : JoAnn D. Peeler
// Created          : 03-12-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-15-2023
// ***********************************************************************
// <copyright file="PosRecord.cs" company="Pedantic">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Value structure used to represent import pieces fields from 
//     training data.
// </summary>
// ***********************************************************************
using Pedantic.Chess;

namespace Pedantic.Tuning
{
    public readonly struct PosRecord
    {
        public const double EVAL_PCT = 0.25;
        public const float WDL_WIN = 1.0f;
        public const float WDL_DRAW  = 0.5f;
        public const float WDL_LOSS = 0.0f;

        public readonly float Progress;
        public readonly short Eval;
        public readonly float Result;

        public PosRecord(int ply, int gamePly, string fen, byte hasCastled, short eval, float result)
        {
            Progress = (float)ply / gamePly;
            Eval = eval;
            Result = result;
            Board bd = new (fen);
            bd.HasCastled[0] = (hasCastled & 1) != 0;
            bd.HasCastled[1] = (hasCastled & 2) != 0;
            Features = new EvalFeatures(bd);
        }

        public double CombinedResult(double k)
        {
            double ratio = EvalRatio();
            return ratio * Tuner.Sigmoid(k, Eval) + (1.0 - ratio) * Result;
        }

        public double EvalRatio() => (1.0 - Progress * Progress) * EVAL_PCT;

        public EvalFeatures Features { get; init; }
    }
}
