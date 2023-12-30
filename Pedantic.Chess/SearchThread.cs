using Pedantic.Utilities;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public class SearchThread
    {
        public SearchThread(bool isPrimary = false)
        {
            this.isPrimary = isPrimary;
            search = null;
            clock = null;
            history = new(stack);
            listPool = new(() => new MoveList(history), Constants.MAX_PLY);
        }

        public void Search(GameClock clock, Board board, int maxDepth, long maxNodes, CountdownEvent done)
        {
            Uci uci = new(isPrimary, false);
            clock.Uci = uci;
            this.clock = clock;

            search = new(stack, board, clock, cache, history, listPool, TtTran.Default, maxDepth, maxNodes, 
                UciOptions.RandomSearch)
            {
                CanPonder = Engine.IsPondering,
                CollectStats = isPrimary && UciOptions.CollectStatistics,
                Uci = uci
            };

            ThreadPool.QueueUserWorkItem((state) =>
            {
                SearchProc(board, done);
            });
        }

        public void WriteStats(StreamWriter writer)
        {
            if (search != null)
            {
                foreach (var st in search.Stats)
                {
                    writer.WriteLine($"{st.Phase},{st.Depth},{st.NodesVisited}");
                }
            }
        }

        public void Stop()
        {
            clock?.Stop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SearchProc(Board board, CountdownEvent done)
        {
            stack.Initialize(board, history);
            history.Rescale();
            search?.Search();
            //if (isPrimary && (board.HalfMoveClock & 0x0f) == 0)
            //{
            //    GC.Collect(GC.MaxGeneration, GCCollectionMode.Default, false);
            //}
            done.Signal();
        }

        public long TotalNodes => search?.NodesVisited ?? 0;
        public double TotalTime => (search?.Elapsed ?? 0) / 1000.0;
        public bool IsPrimary => isPrimary;
        public EvalCache Cache => cache;
        public History History => history;
        public SearchStack Stack => stack;
        public ObjectPool<MoveList> MoveListPool => listPool;

        private readonly bool isPrimary;
        private BasicSearch? search;
        private GameClock? clock;
        private readonly EvalCache cache = new();
        private readonly History history;
        private readonly SearchStack stack = new();
        private readonly ObjectPool<MoveList> listPool;
    }
}
