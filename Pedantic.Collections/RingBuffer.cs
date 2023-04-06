// ***********************************************************************
// Assembly         : Pedantic.Collections
// Author           : JoAnn D. Peeler
// Created          : 04-02-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 04-02-2023
// ***********************************************************************
// <copyright file="RingBuffer.cs" company="Pedantic.Collections">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections;
using System.Collections.Generic;

namespace Pedantic.Collections
{
    public class RingBuffer<T> : ICollection<T> where T : unmanaged, IEquatable<T>
    {
        public RingBuffer() : this(4, false)
        {}

        public RingBuffer(int capacity) : this(capacity, false)
        {}

        public RingBuffer(int capacity, bool allowOverflow)  : this(capacity, allowOverflow, EqualityComparer<T>.Default)
        {}

        public RingBuffer(int capacity, bool allowOverflow, IEqualityComparer<T> comparer)
        {
            this.allowOverflow = allowOverflow;
            this.comparer = comparer;
            buffer = new T[capacity];
            head = tail = count = 0;
        }

        public int Count => count;
        public bool IsReadOnly => false;
        public void Add(T item)
        {
            if (head == tail && count > 0)
            {
                if (allowOverflow)
                {
                    AddToBuffer(item, true);
                }
                else
                {
                    throw new InvalidOperationException("Cannot add new item to RingBuffer because it is full.");
                }
            }
        }

        public void Clear()
        {
            Array.Clear(buffer);
            head = tail = count = 0;
        }

        public bool Contains(T item)
        {
            int index = head;
            for (int i = 0; i < count; i++, index = ++index % buffer.Length)
            {
                if (comparer.Equals(buffer[index], item))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int index = head;
            for (int i = 0; i < count; i++, index = ++index % buffer.Length)
            {
                array[arrayIndex++] = buffer[index];
            }
        }

        public bool Remove(T item)
        {
            int index = head;
            bool found = false;
            for (int i = 0; i < count; i++, index = ++index % buffer.Length)
            {
                if (comparer.Equals(buffer[index], item))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            while (index != tail)
            {
                buffer[index] = buffer[(index + 1) % buffer.Length];
                index = ++index % buffer.Length;
            }

            tail = (--tail + buffer.Length) % buffer.Length;

            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int index = head;
            for (int i = 0; i < count; i++, index = ++index % buffer.Length)
            {
                yield return buffer[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected void AddToBuffer(T item, bool overflow)
        {
            if (overflow)
            {
                // make space for new item by discarding oldest
                head = ++head % buffer.Length;
            }
            else
            {
                count++;
            }

            buffer[tail] = item;
            tail = ++tail % buffer.Length;
        }

        protected readonly T[] buffer;
        protected int head, tail, count;
        protected bool allowOverflow;
        protected IEqualityComparer<T> comparer;
    }
}
