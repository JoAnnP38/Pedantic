namespace Pedantic.Chess
{
    public class SearchThread
    {
        public SearchThread(bool isPrimary = false)
        {
            this.isPrimary = isPrimary;
            search = null;
            clock = null;
            thread = null;
        }

        public void Search(GameClock clock, Board board, int maxDepth, long maxNodes, CountdownEvent done, bool prioritize)
        {
            stack.Initialize(board);
            Uci uci = new Uci(isPrimary, false);
            clock.Uci = uci;
            this.clock = clock;
            history.Rescale();

            search = new(stack, board, clock, cache, history, maxDepth, maxNodes, UciOptions.RandomSearch)
            {
                CanPonder = Engine.IsPondering,
                CollectStats = isPrimary && UciOptions.CollectStatistics,
                Uci = uci
            };

            if (prioritize)
            {
                thread = new Thread(() =>
                {
                    search.Search();
                    done.Signal();
                })
                {
                    Priority = ThreadPriority.Highest
                };
                thread.Start();
            }
            else
            {
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    search.Search();
                    done.Signal();
                });
            }
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

        public long TotalNodes => search?.NodesVisited ?? 0;
        public double TotalTime => (search?.Elapsed ?? 0) / 1000.0;
        public bool IsPrimary => isPrimary;
        public EvalCache Cache => cache;
        public History History => history;
        public SearchStack Stack => stack;

        private bool isPrimary;
        private BasicSearch? search;
        private GameClock? clock;
        private Thread? thread;
        private readonly EvalCache cache = new();
        private readonly History history = new();
        private readonly SearchStack stack = new();
    }
}
