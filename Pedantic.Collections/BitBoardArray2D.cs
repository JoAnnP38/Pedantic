// ***********************************************************************
// Assembly         : Pedantic.Collections
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-28-2023
// ***********************************************************************
// <copyright file="BitBoardArray2D.cs" company="Pedantic.Collections">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     A specialized, two-dimensional array of bitboards (i.e. ulong).
// </summary>
// ***********************************************************************
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pedantic.Collections
{
    public sealed unsafe class BitBoardArray2D : ICloneable, IDisposable
    {
        private bool isDisposed;
        private nint memory;
        private ulong* pArray;
        private readonly int length1;
        private readonly int length2;

        public BitBoardArray2D(int length1, int length2)
        {
            this.length1 = length1;
            this.length2 = length2;
            memory = Marshal.AllocHGlobal(MemorySize);
            GC.AddMemoryPressure(MemorySize);
            pArray = (ulong*)memory;
            Clear();
        }

        ~BitBoardArray2D()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            if (memory != nint.Zero)
            {
                Marshal.FreeHGlobal(memory);
                //tell garbage collector memory is gone
                memory = nint.Zero;
                pArray = null;
                GC.RemoveMemoryPressure(MemorySize);
            }
            GC.SuppressFinalize(this);

            isDisposed = true;
        }

        public int MemorySize => length1 * length2 * sizeof(ulong);
        public static bool IsFixedSize => true;
        public static bool IsReadOnly => false;
        public int Length1 => length1;
        public int Length2 => length2;
        public int Length => length1 * length2;
        public static int Rank => 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            for (int n = 0; n < Length; ++n)
            {
                pArray[n] = 0ul;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(ulong bb)
        {
            for (int n = 0; n < Length; ++n)
            {
                pArray[n] = bb;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int i, int j)
        {
            return i * length2 + j;
        }

        public ref ulong this[int i, int j] => ref pArray[i * length2 + j];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetItem(int i, int j)
        {
            return pArray[GetIndex(i, j)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetItem(int i, int j, ulong value)
        {
            pArray[GetIndex(i, j)] = value;
        }

        public Span<ulong> Span => new((void*)memory, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Copy(BitBoardArray2D other)
        {
            Debug.Assert(length1 == other.length1 && length2 == other.length2);
            for (int n = 0; n < Length; ++n)
            {
                pArray[n] = other.pArray[n];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBoardArray2D Clone()
        {
            BitBoardArray2D clone = new(length1, length2);
            clone.Copy(this);
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
