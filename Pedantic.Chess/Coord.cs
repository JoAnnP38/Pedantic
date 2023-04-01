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
    }
}
