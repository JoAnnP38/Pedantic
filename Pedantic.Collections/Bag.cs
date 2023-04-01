// ***********************************************************************
// Assembly         : Pedantic.Collections
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 01-17-2023
// ***********************************************************************
// <copyright file="Bag.cs" company="Pedantic.Collections">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Specialized collection for class objects.
// </summary>
// ***********************************************************************
using System.Collections;

namespace Pedantic.Collections
{
    public class Bag<T> : ICollection<T> where T : class
    {
        private T[] array;
        private int insertIndex;

        public Bag(int capacity = 4)
        {
            array = new T[capacity];
            insertIndex = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < insertIndex; ++i)
            {
                yield return array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (insertIndex >= array.Length)
            {
                Array.Resize(ref array, array.Length << 1);
            }

            array[insertIndex++] = item;
        }

        public void Clear()
        {
            Array.Clear(array);
            insertIndex = 0;
        }

        public bool Contains(T item)
        {
            for (int i = 0; i < insertIndex; ++i)
            {
                if (array[i].Equals(item))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(this.array, 0, array, arrayIndex, insertIndex);
        }

        public bool Remove(T item)
        {
            int index = FindIndex(element => element.Equals(item));
            if (index >= 0)
            {
                array[index] = array[--insertIndex];
                return true;
            }

            return false;
        }

        public bool TryTake(out T? item)
        {
            if (insertIndex > 0)
            {
                item = array[--insertIndex];
                return true;
            }

            item = null;
            return false;
        }

        public int Count => insertIndex;

        public bool IsReadOnly => false;

        public int FindIndex(Predicate<T> predicate)
        {
            for (int i = 0; i < insertIndex; ++i)
            {
                if (predicate(array[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
