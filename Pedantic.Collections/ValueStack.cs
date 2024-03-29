﻿// ***********************************************************************
// Assembly         : Pedantic.Collections
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-28-2023
// ***********************************************************************
// <copyright file="ValueStack.cs" company="Pedantic.Collections">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implements a specialized stack for simple (i.e. unmanaged,
//     blittable) types.
// </summary>
// ***********************************************************************
using System.Collections;

namespace Pedantic.Collections
{
    public sealed class ValueStack<T> : IStack<T> where T : unmanaged

    {
        private T[] stack;
        private int sp;

        public ValueStack(int capacity = 16)
        {
            stack = new T[capacity];
            sp = 0;
        }

        public ValueStack(ValueStack<T> other)
        {
            stack = new T[other.stack.Length];
            Array.Copy(other.stack, stack, other.sp);
            sp = other.sp;
        }

        public int Count => sp;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            Push(ref item);
        }

        public void Clear()
        {
            sp = 0;
        }

        public bool Contains(T item)
        {
            return Array.FindIndex(stack, 0, sp, e => item.Equals(e)) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(stack, 0, array, arrayIndex, sp);
        }

        public T[] ToArray()
        {
            T[] array = new T[sp];
            CopyTo(array, 0);
            return array;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = sp - 1; i >= 0; --i)
            {
                yield return stack[i];
            }
        }

        public ref T Peek()
        {
            if (sp <= 0)
            {
                throw new InvalidOperationException(@"Cannot peek into an empty stack.");
            }

            return ref stack[sp - 1];
        }

        public T Pop()
        {
            if (sp <= 0)
            {
                throw new InvalidOperationException(@"Cannot pop an empty stack.");
            }

            return stack[--sp];
        }

        public void Push(ref T item)
        {
            if (sp >= stack.Length)
            {
                Array.Resize(ref stack, stack.Length << 1);
            }

            stack[sp++] = item;
        }

        void IStack<T>.Push(T item)
        {
            Push(ref item);
        }

        public bool Remove(T item)
        {
            if (sp > 0 && stack[sp - 1].Equals(item))
            {
                Pop();
                return true;
            }

            return false;
        }

        public bool TryPeek(out T item)
        {
            if (sp > 0)
            {
                item = stack[sp - 1];
                return true;
            }

            item = default;
            return false;
        }

        public bool TryPop(out T item)
        {
            if (sp > 0)
            {
                item = Pop();
                return true;
            }

            item = default;
            return false;
        }

        public int MatchCount(Predicate<T> predicate)
        {
            int matches = 0;
            for (int i = 0; i < sp; ++i)
            {
                if (predicate(stack[i]))
                {
                    ++matches;
                }
            }

            return matches;
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return new ReadOnlySpan<T>(stack, 0, sp);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
