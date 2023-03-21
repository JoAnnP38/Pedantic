#undef DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class Index
    {
        public const int MaxValue = 63;
        public const int MinValue = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(int value)
        {
            return value >= MinValue && value <= MaxValue;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDark(int index)
        {
            ToCoords(index, out int file, out int rank);
            return ((file + rank) & 0x01) == 0;

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

        public const int None = -1;
        public const int A1 = 0;
        public const int B1 = 1;
        public const int C1 = 2;
        public const int D1 = 3;
        public const int E1 = 4;
        public const int F1 = 5;
        public const int G1 = 6;
        public const int H1 = 7;
        public const int A2 = 8;
        public const int B2 = 9;
        public const int C2 = 10;
        public const int D2 = 11;
        public const int E2 = 12;
        public const int F2 = 13;
        public const int G2 = 14;
        public const int H2 = 15;
        public const int A3 = 16;
        public const int B3 = 17;
        public const int C3 = 18;
        public const int D3 = 19;
        public const int E3 = 20;
        public const int F3 = 21;
        public const int G3 = 22;
        public const int H3 = 23;
        public const int A4 = 24;
        public const int B4 = 25;
        public const int C4 = 26;
        public const int D4 = 27;
        public const int E4 = 28;
        public const int F4 = 29;
        public const int G4 = 30;
        public const int H4 = 31;
        public const int A5 = 32;
        public const int B5 = 33;
        public const int C5 = 34;
        public const int D5 = 35;
        public const int E5 = 36;
        public const int F5 = 37;
        public const int G5 = 38;
        public const int H5 = 39;
        public const int A6 = 40;
        public const int B6 = 41;
        public const int C6 = 42;
        public const int D6 = 43;
        public const int E6 = 44;
        public const int F6 = 45;
        public const int G6 = 46;
        public const int H6 = 47;
        public const int A7 = 48;
        public const int B7 = 49;
        public const int C7 = 50;
        public const int D7 = 51;
        public const int E7 = 52;
        public const int F7 = 53;
        public const int G7 = 54;
        public const int H7 = 55;
        public const int A8 = 56;
        public const int B8 = 57;
        public const int C8 = 58;
        public const int D8 = 59;
        public const int E8 = 60;
        public const int F8 = 61;
        public const int G8 = 62;
        public const int H8 = 63;

        #endregion

        #region mapping of indices so white oriented artifacts can be used for black

        public static readonly int[][] NormalizedIndex = new int[][]
        {
            new[]
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
            new[]
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
