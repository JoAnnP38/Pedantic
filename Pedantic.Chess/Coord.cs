// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Coord.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Class Coord encapsulates some helper functions that make it easier
//     to work with "file" and "rank" coordinates of a chess board.
// </summary>
// ***********************************************************************
using Pedantic.Utilities;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public static class Coord
    {
        public const int MAX_VALUE = 7;
        public const int MIN_VALUE = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(int value)
        {
            return value is >= MIN_VALUE and <= MAX_VALUE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToFile(int value)
        {
            Util.Assert(IsValid(value));
            return new string((char)('a' + value), 1);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToRank(int value)
        {
            Util.Assert(IsValid(value));
            return new string((char)('1' + value), 1);
        }

        #region Named Coords

        public const int NONE = -1;
        public const int RANK_1 = 0;
        public const int RANK_2 = 1;
        public const int RANK_3 = 2;
        public const int RANK_4 = 3;
        public const int RANK_5 = 4;
        public const int RANK_6 = 5;
        public const int RANK_7 = 6;
        public const int RANK_8 = 7;

        public const int FILE_A = 0;
        public const int FILE_B = 1;
        public const int FILE_C = 2;
        public const int FILE_D = 3;
        public const int FILE_E = 4;
        public const int FILE_F = 5;
        public const int FILE_G = 6;
        public const int FILE_H = 7;

        #endregion
    }
}
