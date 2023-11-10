using Pedantic.Chess;
using Pedantic.Utilities;


namespace Pedantic
{
    public class Labeler
    {
        public const int SEARCH_DEPTH = 7;

        public bool Label(PgnPositionReader.Position pos, out PgnPositionReader.Position labeled)
        {
            Board bd = new(pos.Fen);
            history.Clear();
            cache.Clear();
            clock.Go(int.MaxValue, false);
            labeled = pos;
            search = new(stack, bd, clock, cache, history, listPool, tt, SEARCH_DEPTH)
            {
                CanPonder = false,
                CollectStats = false,
                Uci = uci
            };

            search.Search();
            if (Math.Abs(search.Score) < Constants.TABLEBASE_WIN && search.PV.Length > 0)
            {
                if (!Move.IsCapture(search.PV[0]))
                {
                    short eval = bd.SideToMove == Color.White ? 
                        (short)search.Score : (short)-search.Score;

                    labeled = new(pos, eval);
                    return true;
                }
            }
            return false;
        }

        private BasicSearch? search;
        private readonly GameClock clock = new() { Infinite = true };
        private readonly Uci uci = new(false, false);
        private readonly EvalCache cache = new(4);
        private readonly History history = new();
        private readonly SearchStack stack = new();
        private readonly ObjectPool<MoveList> listPool = new(18);
        private readonly TtTran tt = new(16);
    }
}
