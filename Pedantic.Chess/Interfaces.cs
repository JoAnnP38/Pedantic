namespace Pedantic.Chess
{
    public interface IHistory
    {
        public int this[int from, int to] { get; }
    }

    public interface ICounterMoves
    {
        public ref ulong this[Piece piece, int to] { get; }
    }

    public interface IMoveScorer
    {
        public ulong ScoreQuietMove(Board board, ulong move);
        public ulong ScoreCaptureMove(Board board, ulong move);
        public ulong ScorePromoteMove(Board board, ulong move);
    }

    public interface IHeuritics : IMoveScorer
    {
        public ulong[] Killers { get; }
        public IHistory History { get; } 
        public ICounterMoves CounterMoves { get; }
    }
}