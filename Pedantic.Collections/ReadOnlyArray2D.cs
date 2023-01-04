using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;


namespace Pedantic.Collections
{
    public sealed class ReadOnlyArray2D<T> where T : unmanaged
    {
        private ReadOnlyArray2D()
        {
            length1 = 0;
            length2 = 0;
            memory = new(Array.Empty<T>());
        }

        public ReadOnlyArray2D(int length1, int length2, T[] array, int start)
        {
            this.length1 = length1;
            this.length2 = length2;
            if ((array.Length - start) < (length1 * length2))
            {
                throw new ArgumentException($"Array parameter is not of sufficient length ({array.Length}).",
                    nameof(array));
            }

            memory = new ReadOnlyMemory<T>(array, start, length1 * length2);
        }

        public bool IsFixedSize => true;
        public bool IsReadOnly => true;
        public int Length1 => length1;
        public int Length2 => length2;
        public int Length => length1 * length2;
        public int Rank => 2;

        public T this[int i, int j] => memory.Span[GetIndex(i, j)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetItem(int i, int j)
        {
            return memory.Span[GetIndex(i, j)];
        }

        public static ReadOnlyArray2D<T> Empty { get; } = new();
    

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetIndex(int i, int j)
        {
            Debug.Assert(i >= 0 && i < length1);
            Debug.Assert(j >= 0 && j < length2);

            return i * length2 + j;
        }

        private readonly int length1;
        private readonly int length2;
        private readonly ReadOnlyMemory<T> memory;
    }
}
