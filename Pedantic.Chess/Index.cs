// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Index.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implement class Index which provides methods to operate on indexes
//     into a 8 x 8 chess board.
// </summary>
// ***********************************************************************
#undef DEBUG
using Pedantic.Utilities;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public static class Index
    {
        public const sbyte MAX_VALUE = 63;
        public const sbyte MIN_VALUE = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(int value)
        {
            return value is >= MIN_VALUE and <= MAX_VALUE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex(int file, int rank)
        {
            Util.Assert(Coord.IsValid(file));
            Util.Assert(Coord.IsValid(rank));
            return (rank << 3) + file;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFile(int index)
        {
            Util.Assert(IsValid(index));
            return index & 0x07;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRank(int index)
        {
            Util.Assert(IsValid(index));
            return index >> 3;
        }

        public static bool TryParse(string s, out int index)
        {
            s = s.Trim().ToLower();
            if (s.Length >= 2 && s[0] >= 'a' && s[0] <= 'h' && s[1] >= '1' && s[1] <= '8')
            {
                index = ToIndex(s[0] - 'a', s[1] - '1');
                return true;
            }

            index = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Flip(int index)
        {
            Util.Assert(IsValid(index));
            return 56 - (index & 0x0f8) + (index & 0x07);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToCoords(int index, out int file, out int rank)
        {
            Util.Assert(IsValid(index));
            rank = index >> 3;
            file = index & 0x07;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int File, int Rank) ToCoords(int index)
        {
            Util.Assert(IsValid(index));
            return (index & 0x07, index >> 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(int index)
        {
            Util.Assert(IsValid(index));
            return algebraicIndices[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBetween(int index, int i1, int i2)
        {
            (int start, int end) = i1 < i2 ? (i1, i2) : (i2, i1);
            return index > start && index < end;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Distance(int index1, int index2)
        {
            (int File, int Rank) coord1 = (GetFile(index1), GetRank(index1));
            (int File, int Rank) coord2 = (GetFile(index2), GetRank(index2));
            return Math.Max(Math.Abs(coord1.File - coord2.File), Math.Abs(coord1.Rank - coord2.Rank));
        }

        public static int CenterDistance(int index)
        {
            ToCoords(index, out int file, out int rank);
            int cFile = (file >> 2) + 3;
            int cRank = (rank >> 2) + 3;
            return Distance(index, ToIndex(cFile, cRank));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ManhattanDistance(int index1, int index2)
        {
            ToCoords(index1, out int file1, out int rank1);
            ToCoords(index2, out int file2, out int rank2);
            return Math.Abs(file1 - file2) + Math.Abs(rank1 - rank2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CenterManhattanDistance(int index)
        {
            ToCoords(index, out int file, out int rank);
            int fileDist = Math.Max(3 - file, file - 4);
            int rankDist = Math.Max(3 - rank, rank - 4);
            return fileDist + rankDist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KingPlacement GetKingPlacement(Color friendlyColor, int friendlyIndex, int opponentIndex)
        {
            return new KingPlacement(friendlyColor, friendlyIndex, opponentIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDark(int index)
        {
            ToCoords(index, out int file, out int rank);
            return ((file + rank) & 0x01) == 0;

        }

        public static bool GetDirection(int from, int to, out Direction direction)
        {
            direction = Direction.None;
            if (from == to)
            {
                return false;
            }

            var fromCoords = ToCoords(from);
            var toCoords = ToCoords(to);
            int fileDiff = toCoords.File - fromCoords.File;
            int rankDiff = toCoords.Rank - fromCoords.Rank;

            if (fileDiff == 0)
            {
                direction = rankDiff > 0 ? Direction.North : Direction.South;
            }
            else if (rankDiff == 0)
            {
                direction = fileDiff > 0 ? Direction.East : Direction.West;
            }
            else if (fileDiff + rankDiff == 0)
            {
                direction = rankDiff > 0 ? Direction.NorthWest : Direction.SouthEast;
            }
            else if (fileDiff - rankDiff == 0)
            {
                direction = rankDiff > 0 ? Direction.NorthEast : Direction.SouthWest;
            }
            return direction != Direction.None;
        }

        private static readonly string[] algebraicIndices =
        {
            "a1", "b1", "c1", "d1", "e1", "f1", "g1", "h1",
            "a2", "b2", "c2", "d2", "e2", "f2", "g2", "h2",
            "a3", "b3", "c3", "d3", "e3", "f3", "g3", "h3",
            "a4", "b4", "c4", "d4", "e4", "f4", "g4", "h4",
            "a5", "b5", "c5", "d5", "e5", "f5", "g5", "h5",
            "a6", "b6", "c6", "d6", "e6", "f6", "g6", "h6",
            "a7", "b7", "c7", "d7", "e7", "f7", "g7", "h7",
            "a8", "b8", "c8", "d8", "e8", "f8", "g8", "h8"
        };

        #region Named Indices

        public const sbyte NONE = -1;
        public const sbyte A1 = 0;
        public const sbyte B1 = 1;
        public const sbyte C1 = 2;
        public const sbyte D1 = 3;
        public const sbyte E1 = 4;
        public const sbyte F1 = 5;
        public const sbyte G1 = 6;
        public const sbyte H1 = 7;
        public const sbyte A2 = 8;
        public const sbyte B2 = 9;
        public const sbyte C2 = 10;
        public const sbyte D2 = 11;
        public const sbyte E2 = 12;
        public const sbyte F2 = 13;
        public const sbyte G2 = 14;
        public const sbyte H2 = 15;
        public const sbyte A3 = 16;
        public const sbyte B3 = 17;
        public const sbyte C3 = 18;
        public const sbyte D3 = 19;
        public const sbyte E3 = 20;
        public const sbyte F3 = 21;
        public const sbyte G3 = 22;
        public const sbyte H3 = 23;
        public const sbyte A4 = 24;
        public const sbyte B4 = 25;
        public const sbyte C4 = 26;
        public const sbyte D4 = 27;
        public const sbyte E4 = 28;
        public const sbyte F4 = 29;
        public const sbyte G4 = 30;
        public const sbyte H4 = 31;
        public const sbyte A5 = 32;
        public const sbyte B5 = 33;
        public const sbyte C5 = 34;
        public const sbyte D5 = 35;
        public const sbyte E5 = 36;
        public const sbyte F5 = 37;
        public const sbyte G5 = 38;
        public const sbyte H5 = 39;
        public const sbyte A6 = 40;
        public const sbyte B6 = 41;
        public const sbyte C6 = 42;
        public const sbyte D6 = 43;
        public const sbyte E6 = 44;
        public const sbyte F6 = 45;
        public const sbyte G6 = 46;
        public const sbyte H6 = 47;
        public const sbyte A7 = 48;
        public const sbyte B7 = 49;
        public const sbyte C7 = 50;
        public const sbyte D7 = 51;
        public const sbyte E7 = 52;
        public const sbyte F7 = 53;
        public const sbyte G7 = 54;
        public const sbyte H7 = 55;
        public const sbyte A8 = 56;
        public const sbyte B8 = 57;
        public const sbyte C8 = 58;
        public const sbyte D8 = 59;
        public const sbyte E8 = 60;
        public const sbyte F8 = 61;
        public const sbyte G8 = 62;
        public const sbyte H8 = 63;

        #endregion

        #region mapping of indices so white oriented artifacts can be used for black

        public static readonly sbyte[][] NormalizedIndex = 
        {
            new sbyte[]
            {
                A1, B1, C1, D1, E1, F1, G1, H1,
                A2, B2, C2, D2, E2, F2, G2, H2,
                A3, B3, C3, D3, E3, F3, G3, H3,
                A4, B4, C4, D4, E4, F4, G4, H4,
                A5, B5, C5, D5, E5, F5, G5, H5,
                A6, B6, C6, D6, E6, F6, G6, H6,
                A7, B7, C7, D7, E7, F7, G7, H7,
                A8, B8, C8, D8, E8, F8, G8, H8
            },
            new sbyte[]
            {
                A8, B8, C8, D8, E8, F8, G8, H8,
                A7, B7, C7, D7, E7, F7, G7, H7,
                A6, B6, C6, D6, E6, F6, G6, H6,
                A5, B5, C5, D5, E5, F5, G5, H5,
                A4, B4, C4, D4, E4, F4, G4, H4,
                A3, B3, C3, D3, E3, F3, G3, H3,
                A2, B2, C2, D2, E2, F2, G2, H2,
                A1, B1, C1, D1, E1, F1, G1, H1
            }
        };

        #endregion
    }
}
