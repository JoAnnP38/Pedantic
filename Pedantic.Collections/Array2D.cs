using System.Collections;

namespace Pedantic.Collections
{
    public class Array2D<T> : IEnumerable<T> where T : unmanaged
    {
        public Array2D(int dim1, int dim2, bool fill = false)
        {
            this.dim1 = dim1;
            this.dim2 = dim2;
            length = dim1 * dim2;
            insertIndex = fill ? length : 0;
            array = new T[length];
        }

        public void Add(T item)
        {
            if (insertIndex >= length)
            {
                throw new InvalidOperationException("Cannot add more items to Array2D than its current size.");
            }

            array[insertIndex++] = item;
        }

        public void Clear()
        {
            Array.Clear(array);
        }

        public void Copy(Array2D<T> other)
        {
            if (dim1 != other.dim1 || dim2 != other.dim2)
            {
                throw new ArgumentException("Cannot copy an Array2D of different sizes.", nameof(other));
            }

            Array.Copy(other.array, array, array.Length);
        }

        public ref T this[int i, int j] => ref array[i * dim2 + j];

        public int GetDimension(int dim)
        {
            return dim switch
            {
                0 => dim1,
                1 => dim2,
                _ => throw new InvalidOperationException("Array2D only supports two dimensions [0-1].")
            };
        }

        public int Count => insertIndex;

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int insertIndex;
        private readonly int length;
        private readonly int dim1, dim2;
        private readonly T[] array;
    }
}
