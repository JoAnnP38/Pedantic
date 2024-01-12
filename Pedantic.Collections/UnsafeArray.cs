using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pedantic.Collections
{
    public unsafe class UnsafeArray<T> : IEnumerable<T>, IDisposable where T : unmanaged
    {
        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            internal Enumerator(UnsafeArray<T> array)
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

        public UnsafeArray(int length, bool fill = false)
        {
            this.length = length;
            insertIndex = fill ? length : 0;
            byteCount = (nuint)(length * sizeof(T));
            pArray = (T*)NativeMemory.AllocZeroed(byteCount);
        }

        ~UnsafeArray()
        {
            if (pArray != null)
            {
                NativeMemory.Free(pArray);
                pArray = null;
            }
        }

        public ref T this[int i]
        {
            get
            {
                Debug.Assert(i >= 0 && i < length);
                return ref pArray[i];
            }
        }

        public int Length => length;
        public int Count => insertIndex;

        public void Add(T item)
        {
            Debug.Assert(insertIndex < length);
            pArray[insertIndex++] = item;
        }

        public void Clear()
        {
            NativeMemory.Clear(pArray, byteCount);
        }

        #region IEnumerable Implementation

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

        #endregion

        private readonly int length; 
        private int insertIndex;
        private nuint byteCount;
        private T* pArray;
    }
}
