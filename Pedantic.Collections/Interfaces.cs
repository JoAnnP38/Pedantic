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

    public interface IArray<T>
    {
        public int Length { get; }
        public ref T this[int i] { get; }
        public void Clear();
        public void Fill(ref T value);
        public void Copy(IArray<T> array);
    }
}