﻿// ***********************************************************************
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

namespace Pedantic
{
    public readonly struct PosRecord
    {
        public readonly string Fen;
        public readonly byte HasCastled;
        public readonly float Result;

        public PosRecord(string fen, byte hasCastled, float result)
        {
            Fen = fen;
            Result = result;
            HasCastled = hasCastled;
            Board bd = new (fen);
            bd.HasCastled[0] = (HasCastled & 1) != 0;
            bd.HasCastled[1] = (HasCastled & 2) != 0;
            Features = new EvalFeatures(bd);

        }

        public EvalFeatures Features { get; init; }
    }
}
