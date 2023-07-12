namespace Pedantic.Chess
{
    public interface IHistory
    {
        public short this[Piece piece, int to] { get; }
    }
}