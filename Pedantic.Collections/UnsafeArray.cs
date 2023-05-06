using System.Collections;

namespace Pedantic.Collections
{
    public unsafe class UnsafeArray<T> : IEnumerable<T> where T : unmanaged
    {
        public UnsafeArray(int length, bool fill = false)
        {
            this.length = length;
            insertIndex = fill ? length : 0;
            array = GC.AllocateArray<T>(length, true);
            fixed (T* ptr = array)
            {
                pArray = ptr;
            }
        }

        public ref T this[int i] => ref pArray[i];
        public int Length => length;
        public int Count => insertIndex;

        public void Add(T item)
        {
            if (insertIndex >= length)
            {
                throw new InvalidOperationException("Cannot add more items to UnsafeArray than its current size.");
            }
            pArray[insertIndex++] = item;
        }

        public void Clear()
        {
            Array.Clear(array);
        }

        #region IEnumerable Implementation

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return array.GetEnumerator();
        }

        #endregion

        private readonly int length; 
        private int insertIndex;
        private readonly T[] array;
        private readonly T* pArray;
    }
}
