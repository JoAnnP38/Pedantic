using System.Collections;

namespace Pedantic.Chess
{
    public interface IHistory
    {
        public short this[Color stm, Piece piece, int to] { get; }
        public short this[ulong move] { get; }
    }

    public interface IMoveList : IEnumerable<ulong>, IEnumerable
    {
        public ulong this[int index] { get; }
        public void Add(ulong move);
        public void Add(Color stm, Piece piece, int from, int to, MoveType type, Piece capture, Piece promote, int score);
        public void Add(IEnumerable<ulong> moves);
        public ReadOnlySpan<ulong> AsSpan();
        public void Clear();
        public int Count { get; }
        public void ScoredAdd(Color stm, Piece piece, int from, int to, MoveType type, Piece capture, Piece promote);
        public void ScoredAddQuiet(Color stm, Piece piece, int from, int to, MoveType type = MoveType.Normal);
        public void ScoredAddPromote(Color stm, Piece piece, int from, int to, Piece promote);
        public void ScoredAddCapture(Color stm, Piece piece, int from, int to, MoveType type, Piece capture, Piece promote = Piece.None);
        public ulong Sort(int n);
        public bool Remove(ulong move);
    }
}