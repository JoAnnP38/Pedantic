// ***********************************************************************
// Assembly         : Pedantic.Utilities
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="BitOps.cs" company="Pedantic.Utilities">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Bit twiddling utility ops using instrinsics if possible.
// </summary>
// ***********************************************************************
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
            Util.Assert(index is >= 0 and < 64);
            return 1ul << index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetBit(ulong bitBoard, int bitIndex)
        {
            Util.Assert(bitIndex is >= 0 and < 64);
            return bitBoard | (1ul << bitIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ResetBit(ulong bitBoard, int bitIndex)
        {
            Util.Assert(bitIndex is >= 0 and < 64);
            return bitBoard & ~(1ul << bitIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBit(ulong bitBoard, int bitIndex)
        {
            Util.Assert(bitIndex is >= 0 and < 64);
            return (int)((bitBoard >> bitIndex) & 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TzCount(ulong bitBoard)
        {
            if (Bmi1.X64.IsSupported)
            {
                return (int)Bmi1.X64.TrailingZeroCount(bitBoard);
            }
            return BitOperations.TrailingZeroCount(bitBoard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LzCount(ulong bitBoard)
        {
            if (Lzcnt.X64.IsSupported)
            {
                return (int)Lzcnt.X64.LeadingZeroCount(bitBoard);
            }
            return BitOperations.LeadingZeroCount(bitBoard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ResetLsb(ulong bitBoard)
        {
            if (Bmi1.X64.IsSupported)
            {
                return Bmi1.X64.ResetLowestSetBit(bitBoard);
            }
            return ResetBit(bitBoard, TzCount(bitBoard));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AndNot(ulong bb1, ulong bb2)
        {
            return bb1 & ~bb2; // actually faster than Bmi1.X64.AndNot
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(ulong bitBoard)
        {
            if (Popcnt.X64.IsSupported)
            {
                return (int)Popcnt.X64.PopCount(bitBoard);
            }
            return BitOperations.PopCount(bitBoard);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BitFieldExtract(ulong bits, byte start, byte length)
        {
            Util.Assert(start < 64);
            Util.Assert(length <= 64 - start);
            if (Bmi1.X64.IsSupported)
            {
                return (int)Bmi1.X64.BitFieldExtract(bits, start, length);
            }
            return (int)((bits >> start) & ((1ul << length) - 1ul));
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
        public static bool IsPow2(int value)
        {
            return BitOperations.IsPow2(value);
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

        public static ulong ParallelBitDeposit(ulong value, ulong mask)
        {
            if (Bmi2.X64.IsSupported)
            {
                return Bmi2.X64.ParallelBitDeposit(value, mask);
            }
            ulong res = 0;
            for (ulong bb = 1; mask != 0; bb += bb)
            {
                if ((value & bb) != 0)
                    res |= mask & (ulong)(-(long)mask);
                mask &= mask - 1;
            }
            return res;
        }
    }
}
