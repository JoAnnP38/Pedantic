using System.Collections;

namespace Pedantic.Collections
{
    public unsafe class UnsafeArray2D<T> : IEnumerable<T> where T : unmanaged
    {
        public UnsafeArray2D(int dim1, int dim2, bool fill = false)
        {
            this.dim1 = dim1;
            this.dim2 = dim2;
            length = dim1 * dim2;
            insertIndex = fill ? length : 0;
            array = GC.AllocateArray<T>(length, true);
            fixed (T* ptr = array)
            {
                pArray = ptr;
            }
        }

        public void Add(T item)
        {
            if (insertIndex >= length)
            {
                throw new InvalidOperationException("Cannot add more items to UnsafeArray2D than its current size.");
            }
            pArray[insertIndex++] = item;
        }

        public void Clear()
        {
            Array.Clear(array);
        }

        public ref T this[int i, int j] => ref pArray[i * dim2 + j];

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
            return (IEnumerator<T>)array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return array.GetEnumerator();
        }

        // insertIndex is used by the enumerator/Add methods to support
        // object initialization
        private int insertIndex;
        private readonly int length;
        private readonly int dim1, dim2;
        private readonly T[] array;
        private readonly T* pArray;
    }
}