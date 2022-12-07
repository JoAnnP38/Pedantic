using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed partial class Board
    {
        #region PEXT Algorithm for Sliding Move Generation

        public readonly struct PextEntry
        {
            private readonly uint offsetRook;
            private readonly uint offsetBishop;
            private readonly ulong maskRook;
            private readonly ulong maskBishop;

            public PextEntry(uint offsetRook, uint offsetBishop, ulong maskRook, ulong maskBishop)
            {
                this.offsetRook = offsetRook;
                this.offsetBishop = offsetBishop;
                this.maskRook = maskRook;
                this.maskBishop = maskBishop;
            }

            public uint OffsetRook => offsetRook;
            public uint OffsetBishop => offsetBishop;
            public ulong MaskRook => maskRook;
            public ulong MaskBishop => maskBishop;
        }

        private static ulong[] pextAttacks = Array.Empty<ulong>();
        private static PextEntry[] pextEntries = new PextEntry[64];

        static ulong RelevantBishopSee(int x, int y)
        {
            // result attacks bitboard
            ulong attacks = 0ul;

            // init ranks & files
            int r, f;

            // mask relevant bishop occupancy bits
            for (r = y + 1, f = x + 1; r <= 6 && f <= 6; r++, f++) attacks |= (1ul << (r * 8 + f));
            for (r = y - 1, f = x + 1; r >= 1 && f <= 6; r--, f++) attacks |= (1ul << (r * 8 + f));
            for (r = y + 1, f = x - 1; r <= 6 && f >= 1; r++, f--) attacks |= (1ul << (r * 8 + f));
            for (r = y - 1, f = x - 1; r >= 1 && f >= 1; r--, f--) attacks |= (1ul << (r * 8 + f));

            // return attack map
            return attacks;
        }

        static ulong RelevantRookSee(int x, int y)
        {
            // result attacks bitboard
            ulong attacks = 0ul;

            // init ranks & files
            int r, f;

            // mask relevant rook occupancy bits
            for (r = y + 1; r <= 6; r++) attacks |= (1ul << (r * 8 + x));
            for (r = y - 1; r >= 1; r--) attacks |= (1ul << (r * 8 + x));
            for (f = x + 1; f <= 6; f++) attacks |= (1ul << (y * 8 + f));
            for (f = x - 1; f >= 1; f--) attacks |= (1ul << (y * 8 + f));

            // return attack map
            return attacks;
        }

        static PextEntry MakePextEntry(int sq, ulong maskRook, ulong maskBishop, List<ulong> target)
        {
            uint offsetRook = (uint)target.Count;
            int cnt = BitOps.PopCount(maskRook);
            for (ulong i = 0; i < (ulong)(1 << cnt); i++)
            {
                ulong occ = Bmi2.X64.ParallelBitDeposit(i, maskRook);
                target.Add(GetRookAttacks(sq, occ));
            }

            uint offsetBishop = (uint)target.Count;
            cnt = BitOps.PopCount(maskBishop);
            for (ulong i = 0; i < (ulong)(1 << cnt); i++)
            {
                ulong occ = Bmi2.X64.ParallelBitDeposit(i, maskBishop);
                target.Add(GetBishopAttacks(sq, occ));
            }

            return new PextEntry(offsetRook, offsetBishop, maskRook, maskBishop);
        }

        static void InitializePext()
        {
            List<ulong> attacks = new List<ulong>();
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int sq = y * 8 + x;
                    pextEntries[sq] = MakePextEntry(sq, RelevantRookSee(x, y), RelevantBishopSee(x, y), attacks);
                }
            }
            pextAttacks = attacks.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetRookAttacksPext(int sq, ulong occupy)
        {
            return pextAttacks[pextEntries[sq].OffsetRook + Bmi2.X64.ParallelBitExtract(occupy, pextEntries[sq].MaskRook)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetBishopAttacksPext(int sq, ulong occupy)
        {
            return pextAttacks[pextEntries[sq].OffsetBishop + Bmi2.X64.ParallelBitExtract(occupy, pextEntries[sq].MaskBishop)];
        }

        #endregion
    }
}
