namespace Pedantic.Chess
{
    public interface IHistory
    {
        public int this[int from, int to] { get; }
    }
}