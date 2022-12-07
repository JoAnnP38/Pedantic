namespace Pedantic.Chess
{
    public static class Engine
    {
        private static readonly Board board = new();
        private static readonly TimeControl time = new();
        private static int searchThreads = 1;
        private static Task? search = null;

        public static bool IsRunning { get; private set; } = false;
        public static bool UseOwnBook { get; set; } = true;
        public static int SearchThreads
        {
            get => searchThreads;
            set => searchThreads = /*Math.Max(Math.Min(value, Environment.ProcessorCount), 1)*/ 1;
        }
        public static Color SideToMove => board.SideToMove;

        public static void Start()
        {
            Stop();
            IsRunning = true;
        }

        public static void Stop()
        {
            if (search != null)
            {
                time.Stop();
                search.Wait();
                search = null;
            }
        }

        public static void Quit()
        {
            Stop();
            IsRunning = false;
        }

        public static void Go(int maxDepth, int maxTime, long maxNodes)
        {
            Stop();
            time.Go(maxTime);
            // StartSearch(maxDepth, maxNodes);
        }

        public static void Go(int maxTime, int increment, int movesToGo, int maxDepth, long maxNodes)
        {
            Stop();
            time.Go(maxTime, increment, movesToGo);
            // StartSearch(maxDepth, maxNodes);
        }

        public static void ClearHashTable()
        {
            TtEval.Clear();
        }

        public static void ResizeHashTable(int sizeMb)
        {
            TtEval.Resize(sizeMb);
        }

        public static bool SetupPosition(string fen)
        {
            Stop();
            return board.LoadFenPosition(fen);
        }

        public static void MakeMoves(IEnumerable<string> moves)
        {
            foreach (string s in moves)
            {
                if (Move.TryParseMove(board, s, out ulong move))
                {
                    board.MakeMove(move);
                }
                else
                {
                    throw new ArgumentException("Long algebraic move expected. Bad format '{s}'.");
                }
            }
        }
    }
}
