using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Utilities
{
    public static class Arith
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Abs(int value)
        {
            int s = value >> 31; // cdq, signed shift, -1 if negative, else 0
            value ^= s;  // ones' complement if negative
            value -= s;  // plus one if negative -> two's complement if negative
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Max(int val1, int val2)
        {
            int diff = val1 - val2;
            int dsgn = diff >> 31;
            return val1 - (diff & dsgn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(int val1, int val2)
        {
            int diff = val1 - val2;
            int dsgn = diff >> 31;
            return val2 + (diff & dsgn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(int value)
        {
            int sign = value >> 31;
            return sign + sign + 1;
        }
    }
}
