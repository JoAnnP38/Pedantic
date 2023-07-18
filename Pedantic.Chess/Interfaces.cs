namespace Pedantic.Chess
{
    public interface IHistory
    {
        public int this[Piece piece, int to] { get; }
    }
}