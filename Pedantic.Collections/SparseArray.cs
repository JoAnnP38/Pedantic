// ***********************************************************************
// Assembly         : Pedantic.Collections
// Author           : JoAnn D. Peeler
// Created          : 03-15-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="SparseArray.cs" company="Pedantic.Collections">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implements a sparse array type useful for optimizing large 
//     length dot-products.
// </summary>
// ***********************************************************************
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
