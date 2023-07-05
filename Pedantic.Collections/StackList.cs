using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Collections
{
    public ref struct StackList<T> where T : unmanaged
    {
        private int count;
        private Span<T> list;

        public StackList(Span<T> buffer)
        {
            count = 0;
            list = buffer;
        }

        public void Add(T item)
        {
            if (count + 1 >= list.Length)
            {
                throw new IndexOutOfRangeException();
            }
            list[count++] = item;
        }

        public void Clear()
        {
            count = 0;
            list.Clear();
        }

        public int Count => count;

        public T this[int index] => list[index];
    }
}
