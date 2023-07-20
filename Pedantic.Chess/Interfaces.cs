namespace Pedantic.Chess
{
    public interface IHistory
    {
        public short this[Color stm, Piece piece, int to] { get; }
        public short this[ulong move] { get; }
    }
}