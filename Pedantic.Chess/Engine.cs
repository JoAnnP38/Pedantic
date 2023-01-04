using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class Engine
    {
        private static readonly Board board = new();
        private static readonly TimeControl time = new();
        private static int searchThreads = 1;
        private static Task<short>? search = null;
        private static PolyglotEntry[]? bookEntries = null;

        public static bool Debug { get; set; } = false;
        public static bool IsRunning { get; private set; } = true;
        public static bool UseOwnBook { get; set; } = true;
        public static Board Board => board;

        public static PolyglotEntry[] BookEntries
        {
            get
            {
                if (bookEntries == null)
                {
                    LoadBookEntries();
                }

                return bookEntries;
            }
        }

        public static int SearchThreads
        {
            get => searchThreads;
            set => searchThreads = /*Math.Max(Math.Min(value, Environment.ProcessorCount), 1)*/ 1;
        }
        public static Color SideToMove => board.SideToMove;

        public static bool Infinite
        {
            get => time.Infinite;
            set => time.Infinite = value;
        }

        public static void Start()
        {
            Stop();
            IsRunning = true;
        }

        public static void Stop(bool force = false)
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
            StartSearch(maxDepth, maxNodes);
        }

        public static void Go(int maxTime, int increment, int movesToGo, int maxDepth, long maxNodes)
        {
            Stop();
            time.Go(maxTime, increment, movesToGo);
            StartSearch(maxDepth, maxNodes);
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
            try
            {
                Stop();
                bool loaded = board.LoadFenPosition(fen);
                if (loaded && Debug)
                {
                    Uci.Log(@$"New position: {board.ToFenString()}");
                }

                if (!loaded)
                {
                    Uci.Log("Engine failed to load position.");
                }

                return loaded;
            }
            catch (Exception e)
            {
                Uci.Log(@$"Engine faulted: {e.Message}");
                return false;
            }
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

            if (Debug)
            {
                Uci.Log($@"New position: {board.ToFenString()}");
            }
        }

        public static void Wait()
        {
            if (search != null)
            {
                search.Wait();
                search = null;
            }
        }

        private static int FindFirstBookMove(ulong hash)
        {
            int low = 0;
            int high = BookEntries.Length - 1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (BookEntries[mid].Key >= hash)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return low;
        }

        public static bool LookupBookMoves(ulong hash, out ReadOnlySpan<PolyglotEntry> bookMoves)
        {
            int first = FindFirstBookMove(hash);
            if (BookEntries[first].Key == hash)
            {
                int last = first;
                while (BookEntries[++last].Key == hash) {}
                bookMoves = new ReadOnlySpan<PolyglotEntry>(BookEntries, first, last - first);
                return true;
            }

            bookMoves = new ReadOnlySpan<PolyglotEntry>();
            return false;
        }

        public static void LoadBookEntries()
        {
            try
            {
                using MemoryStream ms = new(Resources.Book);
                using BigEndianBinaryReader reader = new(ms);

                List<PolyglotEntry> entries = new();
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    PolyglotEntry entry = new()
                    {
                        Key = reader.ReadUInt64(),
                        Move = reader.ReadUInt16(),
                        Weight = reader.ReadUInt16(),
                        Learn = reader.ReadUInt32()
                    };
                    // TODO: Remove bad entries from book 
                    // TODO: Check (is e8h8 another way to specifiy castling?)
                    entries.Add(entry);
                }
                bookEntries = entries.ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static string GetBookMove()
        {
            string move = "0000";
            if (UseOwnBook)
            {
                if (LookupBookMoves(board.Hash, out ReadOnlySpan<PolyglotEntry> bookMoves))
                {
                    int total = 0;
                    foreach (PolyglotEntry entry in bookMoves)
                    {
                        total += entry.Weight;
                    }

                    int pick = Random.Shared.Next(total + 1);
                    for (int n = 0; n < bookMoves.Length; ++n)
                    {
                        PolyglotEntry entry = bookMoves[n];
                        if (pick > entry.Weight)
                        {
                            pick -= entry.Weight;
                        }
                        else
                        {
                            int toFile = BitOps.BitFieldExtract(entry.Move, 0, 3);
                            int toRank = BitOps.BitFieldExtract(entry.Move, 3, 3);
                            int fromFile = BitOps.BitFieldExtract(entry.Move, 6, 3);
                            int fromRank = BitOps.BitFieldExtract(entry.Move, 9, 3);
                            int pc = BitOps.BitFieldExtract(entry.Move, 12, 3);

                            int from = Index.ToIndex(fromFile, fromRank);
                            int to = Index.ToIndex(toFile, toRank);
                            Piece promote = pc == 0 ? Piece.None : (Piece)(pc + 1);

                            move = $@"{Index.ToString(from)}{Index.ToString(to)}{Conversion.PieceToString(promote)}";
                            break;
                        }
                    }
                }
            }

            return move;
        }

        private static void StartSearch(int maxDepth, long maxNodes)
        {
            string move = GetBookMove();
            if (move != "0000")
            {
                if (Move.TryParseMove(board, move, out ulong parsedMove))
                {
                    Uci.BestMove(move);
                    return;
                }
            }
            Uci.Log($"Search scheduled to take {time.TimePerMoveWithMargin}ms.");
            TtEval.Clear();

            Negamax negamax = new(board, time, (short)maxDepth, maxNodes);
            search = Task.Run(() => negamax.Search());
        }
    }
}
