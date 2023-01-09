using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Pedantic.Utilities
{
    public static class BitOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetMask(int index)
        {
            Util.Assert(index >= 0 && index < 64);
            return 1ul << index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetBit(ulong bitBoard, int bitIndex)
        {
            return bitBoard | (1ul << bitIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ResetBit(ulong bitBoard, int bitIndex)
        {
            return bitBoard & ~(1ul << bitIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBit(ulong bitBoard, int bitIndex)
        {
            //return (int)Bmi1.X64.BitFieldExtract(bitBoard, (byte)bitIndex, 1);
            //return (bitBoard & GetMask(bitIndex)) == 0ul ? 0 : 1;
            return (int)((bitBoard >> bitIndex) & 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TzCount(ulong bitBoard)
        {
#if X64
            return (int)Bmi1.X64.TrailingZeroCount(bitBoard);
#else
            return BitOperations.TrailingZeroCount(bitBoard);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LzCount(ulong bitBoard)
        {
#if X64
            return (int)Lzcnt.X64.LeadingZeroCount(bitBoard);
#else
            return BitOperations.LeadingZeroCount(bitBoard);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ResetLsb(ulong bitBoard)
        {
#if X64
            return Bmi1.X64.ResetLowestSetBit(bitBoard);
#else
            return ResetBit(bitBoard, TzCount(bitBoard));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AndNot(ulong bb1, ulong bb2)
        {
            return bb1 & ~bb2; // actually faster than Bmi1.X64.AndNot
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(ulong bitBoard)
        {
#if X64
            return (int)Popcnt.X64.PopCount(bitBoard);
#else
            return BitOperations.PopCount(bitBoard);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BitFieldExtract(ulong bits, byte start, byte length)
        {
            Util.Assert(start < 64);
            Util.Assert(length <= 64 - start);
#if X64
            return (int)Bmi1.X64.BitFieldExtract(bits, start, length);
#else
            return (int)((bits >> start) & ((1ul << length) - 1ul));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BitFieldSet(ulong bits, int value, byte start, byte length)
        {
            Util.Assert(start < 64);
            Util.Assert(length < 64 - start);
            ulong mask = ((1ul << length) - 1) << start;
            return AndNot(bits, mask) | (((ulong)value << start) & mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong MultX(ulong left, ulong right)
        {
#if X64
            ulong low = 0;
            Bmi2.X64.MultiplyNoFlags(left, right, &low);
            return low;
#else
            return left * right;
#endif
        }

        public static int GreatestPowerOfTwoLessThan(int n)
        {
            int v = n;
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++; // next power of 2

            return v >> 1; // previous power of 2
        }

        private static readonly int[] lsb64Table =
        {
            63, 30,  3, 32, 59, 14, 11, 33,
            60, 24, 50,  9, 55, 19, 21, 34,
            61, 29,  2, 53, 51, 23, 41, 18,
            56, 28,  1, 43, 46, 27,  0, 35,
            62, 31, 58,  4,  5, 49, 54,  6,
            15, 52, 12, 40,  7, 42, 45, 16,
            25, 57, 48, 13, 10, 39,  8, 44,
            20, 47, 38, 22, 17, 37, 36, 26
        };

        private static readonly ulong[] bitMask =
        {
            0x0000000000000001ul, 0x0000000000000002ul, 0x0000000000000004ul, 0x0000000000000008ul,
            0x0000000000000010ul, 0x0000000000000020ul, 0x0000000000000040ul, 0x0000000000000080ul,
            0x0000000000000100ul, 0x0000000000000200ul, 0x0000000000000400ul, 0x0000000000000800ul,
            0x0000000000001000ul, 0x0000000000002000ul, 0x0000000000004000ul, 0x0000000000008000ul,
            0x0000000000010000ul, 0x0000000000020000ul, 0x0000000000040000ul, 0x0000000000080000ul,
            0x0000000000100000ul, 0x0000000000200000ul, 0x0000000000400000ul, 0x0000000000800000ul,
            0x0000000001000000ul, 0x0000000002000000ul, 0x0000000004000000ul, 0x0000000008000000ul,
            0x0000000010000000ul, 0x0000000020000000ul, 0x0000000040000000ul, 0x0000000080000000ul,
            0x0000000100000000ul, 0x0000000200000000ul, 0x0000000400000000ul, 0x0000000800000000ul,
            0x0000001000000000ul, 0x0000002000000000ul, 0x0000004000000000ul, 0x0000008000000000ul,
            0x0000010000000000ul, 0x0000020000000000ul, 0x0000040000000000ul, 0x0000080000000000ul,
            0x0000100000000000ul, 0x0000200000000000ul, 0x0000400000000000ul, 0x0000800000000000ul,
            0x0001000000000000ul, 0x0002000000000000ul, 0x0004000000000000ul, 0x0008000000000000ul,
            0x0010000000000000ul, 0x0020000000000000ul, 0x0040000000000000ul, 0x0080000000000000ul,
            0x0100000000000000ul, 0x0200000000000000ul, 0x0400000000000000ul, 0x0800000000000000ul,
            0x1000000000000000ul, 0x2000000000000000ul, 0x4000000000000000ul, 0x8000000000000000ul
        };
    }
}
