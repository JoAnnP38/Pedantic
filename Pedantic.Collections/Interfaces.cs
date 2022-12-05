namespace Pedantic.Collections
{
    public interface IStack<T> : ICollection<T>
    {
        public T Peek();
        public T Pop();
        public void Push(T item);
        public bool TryPeek(out T item);
        public bool TryPop(out T item);
    }
}