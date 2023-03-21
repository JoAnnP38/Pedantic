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

    public interface ISparseArray<T>
    {
        public T this[int i] { get; set; }
        public Span<T> Slice(int start, int count);
        public T DotProduct(ISparseArray<T> other);
        public T DotProduct(T[] other);
        public T DotProduct(ReadOnlySpan<T> other);
        public void Add(int index, T item);
        public bool ContainsItem(int index);
        public T[] ToArray();
        public bool RemoveAt(int index);

    }
}