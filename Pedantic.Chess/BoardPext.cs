using System.Diagnostics;
using Pedantic.Utilities;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Pedantic.Collections;

namespace Pedantic.Chess
{
    public sealed partial class Board
    {
        #region struct PextEntry

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct PextEntry
        {
            public ulong* pAtkRook;
            public ulong* pAtkBish;
            public ulong MaskRook;
            public ulong MaskBish;

            public PextEntry(int offsetRook, int offsetBish, ulong maskRook, ulong maskBish)
            {
                fixed (ulong* ptr = &attacks[offsetRook])
                {
                    pAtkRook = ptr;
                }

                fixed (ulong* ptr = &attacks[offsetBish])
                {
                    pAtkBish = ptr;
                }
                MaskRook = maskRook;
                MaskBish = maskBish;
            }
        }


        #endregion

        private static void InitPext()
        {
            for (int sq = 0; sq < Constants.MAX_SQUARES; sq++)
            {
                Index.ToCoords(sq, out int file, out int rank);
                entries[sq] = CreateEntry(sq, RelevantRookSee(file, rank), RelevantBishopSee(file, rank));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong GetBishopAttacksPext(int square, ulong blockers)
        {
            ref PextEntry entry = ref entries[square];
            return entry.pAtkBish[BitOps.ParallelBitExtract(blockers, entry.MaskBish)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong GetRookAttacksPext(int square, ulong blockers)
        {
            ref PextEntry entry = ref entries[square];
            return entry.pAtkRook[BitOps.ParallelBitExtract(blockers, entry.MaskRook)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong GetQueenAttacksPext(int square, ulong blockers)
        {
            ref PextEntry entry = ref entries[square];
            return entry.pAtkBish[BitOps.ParallelBitExtract(blockers, entry.MaskBish)] |
                   entry.pAtkRook[BitOps.ParallelBitExtract(blockers, entry.MaskRook)];
        }

        public static bool PextSupported()
        {
            if (!Bmi2.IsSupported)
            {
                return false;
            }

            Stopwatch sw = new ();
            sw.Start();
            sw.Stop();
            long fancyElapsed = 0;
            long pextElapsed = 0;
            for (int n = 0; n < 4; n++)
            {
                sw.Restart();
                for (int m = 0; m < 10; m++)
                {
                    foreach ((int sq, ulong blockers) in pextTests)
                    {
                        GetQueenAttacksFancy(sq, blockers);
                    }
                }

                sw.Stop();
                fancyElapsed = sw.ElapsedTicks;

                sw.Restart();
                for (int m = 0; m < 10; m++)
                {
                    foreach ((int sq, ulong blockers) in pextTests)
                    {
                        GetQueenAttacksPext(sq, blockers);
                    }
                }

                sw.Stop();
                pextElapsed = sw.ElapsedTicks;
            }

            return pextElapsed < fancyElapsed;
        }

        private static PextEntry CreateEntry(int sq, ulong maskRook, ulong maskBish)
        {
            int offsetRook = attacks.Count;
            int cnt = BitOps.PopCount(maskRook);
            for (ulong i = 0; i < (1ul << cnt); i++)
            {
                ulong blockers = BitOps.ParallelBitDeposit(i, maskRook);
                attacks.Add(GetRookAttacks(sq, blockers));
            }

            int offsetBish = attacks.Count;
            cnt = BitOps.PopCount(maskBish);
            for (ulong i = 0; i < (1ul << cnt); i++)
            {
                ulong blockers = BitOps.ParallelBitDeposit(i, maskBish);
                attacks.Add(GetBishopAttacks(sq, blockers));
            }

            return new PextEntry(offsetRook, offsetBish, maskRook, maskBish);
        }

        public static readonly bool IsPextSupported;

        private static readonly UnsafeArray<ulong> attacks = new(107648);
        private static readonly UnsafeArray<PextEntry> entries = new(Constants.MAX_SQUARES);

        private static readonly (int sq, ulong blockers)[] pextTests =
        {
            (27, 1213162166818714128), (27, 60275846047117904),
            (20, 8057014715858174825), (23, 132231735533429541),
            (27, 132231735533429541), (10, 11079754638545970259),
            (34, 11079754638545970259), (32, 10484661412159848849),
            (47, 36241071969190246), (39, 1213162166818714128),
            (39, 60275846047117904), (0, 8057014715858174825),
            (5, 8057014715858174825), (46, 132231735533429541),
            (0, 11079754638545970259), (4, 11079754638545970259),
            (0, 10484661412159848849), (7, 10484661412159848849),
            (1, 36241071969190246)
        };
    }
}