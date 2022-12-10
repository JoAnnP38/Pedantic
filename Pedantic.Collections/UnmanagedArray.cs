using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Collections
{
    public sealed unsafe class UnmanagedArray<T> : ICloneable, IDisposable where T : unmanaged
    {
        private bool isDisposed = false;
        private IntPtr memory = IntPtr.Zero;
        private T* pArray = null;
        private int length = 0;

        public UnmanagedArray(int length)
        {
            this.length = length;
            memory = Marshal.AllocHGlobal(MemorySize);
            GC.AddMemoryPressure(MemorySize);
            pArray = (T*)memory;
        }

        private UnmanagedArray(UnmanagedArray<T> other)
        {
            isDisposed = other.isDisposed;
            length = other.length;
            if (!isDisposed)
            {
                memory = Marshal.AllocHGlobal(MemorySize);
                GC.AddMemoryPressure(MemorySize);
                pArray = (T*)memory;
                Copy(other);
            }
        }

        ~UnmanagedArray()
        {
            Dispose();
        }

        public int MemorySize => length * sizeof(T);
        public int Length => length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            for (int n = 0; n < Length; ++n)
            {
                pArray[n] = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(ref T value)
        {
            for (int n = 0; n < Length; ++n)
            {
                pArray[n] = value;
            }
        }

        public void Copy(UnmanagedArray<T> other)
        {
            Debug.Assert(length == other.length);
            for (int n = 0; n < Length; ++n)
            {
                pArray[n] = other.pArray[n];
            }
        }


        public ref T this[int i] => ref pArray[i];


        public Span<ulong> Span => new ((void*)memory, Length);

        public UnmanagedArray<T> Clone()
        {
            return new UnmanagedArray<T>(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            if (memory != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(memory);
                //tell garbage collector memory is gone
                memory = IntPtr.Zero;
                pArray = null;
                GC.RemoveMemoryPressure(MemorySize);
            }
            GC.SuppressFinalize(this);

            isDisposed = true;
        }
    }
}
