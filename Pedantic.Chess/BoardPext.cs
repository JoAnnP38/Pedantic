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
            if (GlobalOptions.DisablePextBitboards)
            {
                return false;
            }

            if (!Bmi2.IsSupported)
            {
                return false;
            }

            Stopwatch sw = new ();
            sw.Start();
            sw.Stop();
            long fancyElapsed = 0;
            long pextElapsed = 0;
            for (int n = 0; n < 5; n++)
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
                if (n > 0)
                {
                    fancyElapsed += sw.ElapsedTicks;
                }

                sw.Restart();
                for (int m = 0; m < 10; m++)
                {
                    foreach ((int sq, ulong blockers) in pextTests)
                    {
                        GetQueenAttacksPext(sq, blockers);
                    }
                }

                sw.Stop();

                if (n > 0)
                {
                    pextElapsed += sw.ElapsedTicks;
                }
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
        private static readonly UnsafeArray<PextEntry> entries = new(Constants.MAX_SQUARES, true);

        private static readonly (int sq, ulong blockers)[] pextTests =
        [
            (56, 0x69EB3C000828EF65ul), (29, 0x000002C424040000ul), (58, 0x042001002080A000ul), (49, 0x0002B001A0284100ul),
            ( 8, 0x40E0080C2430F308ul), (56, 0x6DF96604043AF36Bul), (35, 0x000046E84EAC0048ul), (56, 0x6DE0221C033CC795ul),
            (57, 0x0262A21050244000ul), (27, 0x08080DA019C42442ul), ( 9, 0x7987100B8D00F261ul), (56, 0x01500060E6240501ul),
            ( 9, 0x52E23D10A650CA68ul), (56, 0x51EE490308CC6B60ul), (46, 0x0001400441500000ul), ( 0, 0xC07E095897602461ul),
            (19, 0xB4EB34080A987D62ul), (10, 0x11466094516C850Cul), (30, 0x08C4200748824001ul), (57, 0x4200789228408000ul),
            (26, 0x08AA3180B46A0B00ul), (62, 0x4080000000200001ul), (12, 0x943413C00E10578Eul), (56, 0x416A7104003FC248ul),
            (33, 0x0010006301060000ul), (29, 0x00000501E6120000ul), (59, 0x2C36C611159654A1ul), (19, 0x00000402A2082000ul),
            (41, 0x0001235050000000ul), (47, 0xDDA3E41890265F8Cul), (43, 0x00002CAC8C407100ul), (51, 0x00894A0A49202000ul),
            (62, 0xC0A1F21848218860ul), (26, 0x00B04A8505B06042ul), (26, 0x00100022A4048500ul), (37, 0x400106240E010002ul),
            (10, 0x404040C283010600ul), (19, 0x2860A4002AE86000ul), (20, 0x80211A00F011DA40ul), (35, 0x0018208802D00000ul),
            (16, 0x5860C22B9D557808ul), (19, 0x59C75430A98CDA71ul), (20, 0x4010684B08110060ul), (61, 0x206C38851F9A49A2ul),
            (14, 0x0079D1048C487040ul), (58, 0x14EE8C033816C260ul), (21, 0xB6E42D101126FB69ul), (56, 0x9572B1118806D68Cul),
            (59, 0x68E2570C04D46F49ul), (38, 0x9979D4580825E361ul), (59, 0x68E10422005AF000ul), (60, 0x50622404A088E000ul),
            (12, 0x0586C0350002F388ul), (56, 0x6DFF00901900E744ul), (14, 0x2861A8AA74004302ul), (38, 0x040101F498522144ul),
            (34, 0x0206115C00800002ul), (20, 0x606B907808B06B44ul), (59, 0x0880DA200C6BC910ul), (23, 0x01001011058C0000ul),
            (58, 0x84C129100444B040ul), (50, 0x0004408839418000ul), (56, 0xB5E912143408C961ul), ( 0, 0x00C2220C00D2A195ul)
        ];
    }
}