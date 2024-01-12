using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Pedantic.Collections
{
    public unsafe class UnsafeArray2D<T> : IEnumerable<T>, IDisposable where T : unmanaged
    {
        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            internal Enumerator(UnsafeArray2D<T> array)
            {
                pBegin = array.pArray;
                pEnd = array.pArray + array.length;
                pEnumerating = pEnd;
                current = default;
            }

            public T Current => current;

            object IEnumerator.Current => Current;

            public void Dispose()
            { }

            public bool MoveNext()
            {
                if (pEnumerating != pEnd)
                {
                    current = *pEnumerating++;
                    return true;
                }
                current = default;
                return false;
            }

            public void Reset()
            {
                pEnumerating = pBegin;
                current = default;
            }

            private T* pBegin;
            private T* pEnd;
            private T* pEnumerating;
            private T current;
        }

        public UnsafeArray2D(int dim1, int dim2, bool fill = false)
        {
            this.dim1 = dim1;
            this.dim2 = dim2;
            length = dim1 * dim2;
            insertIndex = fill ? length : 0;
            byteCount = (nuint)(length * sizeof(T));
            pArray = (T*)NativeMemory.AllocZeroed(byteCount);
        }

        ~UnsafeArray2D()
        {
            if (pArray != null)
            {
                NativeMemory.Free(pArray);
                pArray = null;
            }
        }

        public void Add(T item)
        {
            Debug.Assert(insertIndex < length);
            pArray[insertIndex++] = item;
        }

        public void Clear()
        {
            NativeMemory.Clear(pArray, byteCount);
        }

        public ref T this[int i, int j]
        {
            get
            {
                Debug.Assert(i >= 0 && i < dim1);
                Debug.Assert(j >= 0 && j < dim2);
                return ref pArray[i * dim2 + j];
            }
        }

        public int GetDimension(int dim)
        {
            return dim switch
            {
                0 => dim1,
                1 => dim2,
                _ => throw new InvalidOperationException("UnsafeArray2D only supports two dimensions [0-1].")
            };
        }

        public int Count => insertIndex;

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (pArray != null)
            {
                NativeMemory.Free(pArray);
                pArray = null;
            }
            GC.SuppressFinalize(this);
        }

        // insertIndex is used by the enumerator/Add methods to support
        // object initialization
        private int insertIndex;
        private readonly int length;
        private readonly int dim1, dim2;
        private readonly nuint byteCount;
        private T* pArray;
    }
}