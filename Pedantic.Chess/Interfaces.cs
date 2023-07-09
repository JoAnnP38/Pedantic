namespace Pedantic.Chess
{
    public interface IHistory
    {
        public short this[Piece piece, int to] { get; }
    }

    public interface IMoveScorer
    {
        public short QuietScore(Board board, int from, int to);
        public short CaptureScore(Board board, Piece piece, Piece captured, int from, int to);
        public short MoveScore(Piece promote);
    }

    public interface IHeuritics : IMoveScorer
    {
        public ulong[] Killers { get; }
        public IHistory History { get; } 
    }
}