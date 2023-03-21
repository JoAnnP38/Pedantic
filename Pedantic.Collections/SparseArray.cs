using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Pedantic.Collections
{
    public class SparseArray<T> : SortedList<int, T> where T : unmanaged
    {
        public SparseArray() { }
        public SparseArray(IDictionary<int, T> other) : base(other) { }
        public SparseArray(int capacity) : base(capacity) { }

        public T[] ExpandSlice(int start, int count)
        {
            var array = new T[count];
            for (int i = start, j = 0; i < start + count; i++, j++)
            {
                if (ContainsKey(i))
                {
                    array[j] = this[i];
                }
            }

            return array;
        }

    }
}
