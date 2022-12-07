using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualBasic;
using Pedantic.Collections;

namespace Pedantic.Utilities
{
    public class ObjectPool<T> where T : class, IPooledObject<T>, new()
    {
        private readonly Bag<T> objects;

        public ObjectPool(int capacity, int preallocate = 0)
        {
            objects = new Bag<T>(capacity);

            for (int i = 0; i < preallocate; ++i)
            {
                Return(new T());
            }
        }

        public T Get()
        {
            if (objects.TryTake(out T? item))
            {
                return item ?? new();
            }

            return new();
        }

        public void Return(T item)
        {
            item.Clear();
            objects.Add(item);
        }
    }
}
