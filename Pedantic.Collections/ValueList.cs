// ***********************************************************************
// Assembly         : Pedantic.Collections
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 01-17-2023
// ***********************************************************************
// <copyright file="ValueList.cs" company="Pedantic.Collections">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implements a simple, specialized list of unmanaged (i.e. simple,
//     blittable) types. 
// </summary>
// ***********************************************************************
using System.Collections;

namespace Pedantic.Collections
{
    public class ValueList<T> : IList<T>, ICloneable where T : unmanaged
    {
        protected T[] array;
        protected int insertIndex;

        public ValueList(int capacity = 4)
        {
            array = new T[capacity];
            insertIndex = 0;
        }

        protected ValueList(ValueList<T> other)
        {
            array = new T[other.array.Length];
            insertIndex = other.insertIndex;
            Array.Copy(other.array, array, insertIndex);
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

        public void Add(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }

        public void Clear()
        {
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

        public void CopyTo(T[] array, int arrayIndex = 0)
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

        public int Count => insertIndex;

        public int Capacity => array.Length;

        public bool IsReadOnly => false;

        private int FindIndex(Predicate<T> predicate)
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

        public int IndexOf(T item)
        {
            return FindIndex(element => element.Equals(item));
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index >= insertIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(index), @"Insert index exceeds the current size of the list.");
            }

            if (insertIndex >= array.Length)
            {
                Array.Resize(ref array, array.Length * 2);
            }

            Array.Copy(array, index, array, index + 1, insertIndex - index);
            array[index] = item;
            insertIndex++;
        }

        public void RemoveAt(int index)
        {
            if (index >= insertIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(index), @"Insert index exceeds the current size of the list.");
            }

            array[index] = array[--insertIndex];
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= insertIndex)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), @"Insert index exceeds the current size of the list.");
                }

                return array[index];
            }
            set
            {
                if (index < 0 || index >= insertIndex)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), @"Insert index exceeds the current size of the list.");
                }

                array[index] = value;
            }
        }

        public int MatchCount(Predicate<T> match)
        {
            int matchCount = 0;
            for (int i = 0; i < insertIndex; ++i)
            {
                if (match(array[i]))
                {
                    matchCount++;
                }
            }
            return matchCount;
        }

        public int MatchCount(T match)
        {
            int matchCount = 0;
            for (int i = 0; i < insertIndex; ++i)
            {
                if (match.Equals(array[i]))
                {
                    ++matchCount;
                }
            }

            return matchCount;
        }


        public void Rotate(int n = 0)
        {
            // move list up in order and replace tail with head
            if (insertIndex - n >= 2)
            {
                T head = array[n];
                for (int i = n + 1; i < insertIndex; ++i)
                {
                    array[i - 1] = array[i];
                }

                array[insertIndex - 1] = head;
            }

        }

        public void SwapElements(int n1, int n2)
        {
            (array[n1], array[n2]) = (array[n2], array[n1]);
        }

        public ValueList<T> Clone()
        {
            return new ValueList<T>(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}