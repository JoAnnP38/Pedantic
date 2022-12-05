using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public static class Coord
    {
        public const int MaxValue = 7;
        public const int MinValue = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(int value)
        {
            return value >= MinValue && value <= MaxValue;
        }
    }
}
