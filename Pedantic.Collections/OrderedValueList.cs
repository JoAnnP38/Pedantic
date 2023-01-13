using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Collections
{
    public class OrderedValueList<T> : ValueList<T> where T : unmanaged, IComparable<T>
    {
        public OrderedValueList(int capacity = 64)
            : base(capacity)
        {}

        public OrderedValueList(OrderedValueList<T> other)
            : base(other)
        { }

        public new void Add(T item)
        {
            if (insertIndex >= array.Length)
            {
                Array.Resize(ref array, array.Length << 1);
            }

            int insertAt = Array.BinarySearch(array, 0, insertIndex, item);
            if (insertAt < 0)
            {
                insertAt = ~insertAt;
            }

            ShiftArrayUp(insertAt);
            array[insertAt] = item;
        }

        public new void Add(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }

        public new bool Contains(T item)
        {
            return Array.BinarySearch(array, 0, insertIndex, item) >= 0;
        }

        public new bool Remove(T item)
        {
            int index = Array.BinarySearch(array, 0, insertIndex, item);
            if (index >= 0)
            {
                ShiftArrayDown(index);
                return true;
            }

            return false;
        }

        public new void Insert(int index, T item)
        {
            Add(item);
        }

        public new void RemoveAt(int index)
        {
            if (index >= 0 && index < insertIndex)
            {
                ShiftArrayDown(index);
            }
        }

        public new int MatchCount(T match)
        {
            int index = Array.BinarySearch(array, 0, insertIndex, match);
            if (index >= 0)
            {
                int matchCount = 1;
                for (int n = index - 1; n >= 0; --n)
                {
                    if (array[n].CompareTo(match) == 0)
                    {
                        ++matchCount;
                    }
                    else
                    {
                        break;
                    }
                }

                for (int n = index + 1; n < insertIndex; ++n)
                {
                    if (array[n].CompareTo(match) == 0)
                    {
                        ++matchCount;
                    }
                    else
                    {
                        break;
                    }
                }

                return matchCount;
            }

            return 0;
        }

        public new T this[int index]
        {
            get
            {
                if (index < 0 || index >= insertIndex)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), @"Insert index exceeds the current size of the list.");
                }

                return array[index];
            }
        }

        public new OrderedValueList<T> Clone()
        {
            return new OrderedValueList<T>(this);
        }

        protected void ShiftArrayUp(int index)
        {
            if (insertIndex >= array.Length)
            {
                Array.Resize(ref array, array.Length << 1);
            }

            for (int n = insertIndex; n > index; --n)
            {
                array[n] = array[n - 1];
            }

            ++insertIndex;
        }

        protected void ShiftArrayDown(int index)
        {
            for (int n = index; n < insertIndex - 1; n++)
            {
                array[n] = array[n + 1];
            }

            --insertIndex;
        }
    }
}
