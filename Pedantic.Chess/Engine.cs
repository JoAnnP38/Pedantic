using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class Engine
    {
        public readonly struct Scout
        {
            private readonly TimeControl time;
            private readonly Thread thread;

            public Scout(TimeControl time, Thread thread)
            {
                this.time = time;
                this.thread = thread;
            }

            public TimeControl Time => time;
            public Thread Thread => thread;
        }

        private static readonly Board board = new();
        private static readonly TimeControl time = new();
        private static int searchThreads = 1;
        private static Thread? searchThread = null;
        private static List<Scout> scouts = new();
        private static PolyglotEntry[]? bookEntries = null;
        private static Color color = Color.White;
        private static string evaluationId = string.Empty;

        public static bool Debug { get; set; } = false;
        public static bool IsRunning { get; private set; } = true;
        public static bool IsPondering { get; private set; } = true;
        public static bool CanPonder { get; set; } = true;
        public static bool UseOwnBook { get; set; } = true;
        public static bool Infinite { get; set; } = false;

        public static string EvaluationId
        {
            get => evaluationId;
            set
            {
                Evaluation.LoadWeights(value == string.Empty ? null : value);
                evaluationId = value;
            }
        }
        public static Board Board => board;

        public static Color Color
        {
            get => color;
            set
            {
                if (value != color)
                {
                    TtTran.Clear();
                }

                color = value;
            }
        }
        public static bool RandomSearch { get; set; } = false;
  
        public static PolyglotEntry[] BookEntries
        {
            get
            {
                if (bookEntries == null)
                {
                    LoadBookEntries();
                }

                return bookEntries ?? Array.Empty<PolyglotEntry>();
            }
        }

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

        public static void StopScouts()
        {
            foreach (Scout scout in scouts)
            {
                scout.Time.Stop();
                scout.Thread.Join();
            }

            scouts.Clear();
        }

        public static void Stop(bool force = false)
        {
            if (searchThread != null)
            {
                time.Stop();
                searchThread.Join();
                searchThread = null;
                ClearHashTable();
            }

            StopScouts();
        }

        public static void Quit()
        {
            Stop();
            IsRunning = false;
        }

        public static void Go(int maxDepth, int maxTime, long maxNodes, bool ponder = false)
        {
            Stop();
            IsPondering = ponder;
            time.Go(maxTime, ponder || Infinite);
            StartSearch(maxDepth, maxNodes);
        }

        public static void Go(int maxTime, int increment, int movesToGo, int maxDepth, long maxNodes, bool ponder = false)
        {
            Stop();
            IsPondering = ponder;
            time.Go(maxTime, increment, movesToGo, ponder || Infinite);
            StartSearch(maxDepth, maxNodes);
        }

        public static void ClearHashTable()
        {
            TtTran.Clear();
            TtEval.Clear();
            TtPawnEval.Clear();
        }

        public static void ResizeHashTable(int sizeMb)
        {
            if (!BitOps.IsPow2(sizeMb))
            {
                sizeMb = BitOps.GreatestPowerOfTwoLessThan(sizeMb);
            }

            TtTran.Resize(sizeMb);
            TtEval.Resize(sizeMb >> 1);
            TtPawnEval.Resize(sizeMb >> 2);
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
                    if (!board.MakeMove(move))
                    {
                        throw new InvalidOperationException($"Invalid move passed to engine: '{s}'.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Long algebraic move expected. Bad format '{s}'.");
                }
            }

            if (Debug)
            {
                Uci.Log($@"New position: {board.ToFenString()}");
            }
        }

        public static void Wait()
        {
            if (searchThread != null)
            {
                searchThread.Join();
                searchThread = null;
            }

            StopScouts();
        }
    

        public static void PonderHit()
        {
            if (IsRunning)
            {
                if (IsPondering)
                {
                    IsPondering = false;
                    time.Infinite = false;
                }
                else
                {
                    Stop();
                }
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
            int first = 0;
            try
            {
                first = FindFirstBookMove(hash);
                if (first >= 0 && first < BookEntries.Length - 1 && BookEntries[first].Key == hash)
                {
                    int last = first;
                    while (++last < BookEntries.Length && BookEntries[last].Key == hash)
                    { }

                    bookMoves = new ReadOnlySpan<PolyglotEntry>(BookEntries, first, last - first);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.Message);
                throw;
            }

            bookMoves = new ReadOnlySpan<PolyglotEntry>();
            return false;
        }

        public static void LoadBookEntries()
        {
            try
            {

                string? exeFullName = Environment.ProcessPath;
                string? dirFullName = System.IO.Path.GetDirectoryName(exeFullName);
                string? bookPath = (exeFullName != null && dirFullName != null) ? Path.Combine(dirFullName, "Pedantic.bin") : null;

                if (bookPath != null && File.Exists(bookPath))
                {
                    FileStream fs = new(bookPath, FileMode.Open, FileAccess.Read);
                    using BigEndianBinaryReader reader = new(fs);

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

                        entries.Add(entry);
                    }
                    bookEntries = entries.ToArray();
                }
                else
                {
                    bookEntries = Array.Empty<PolyglotEntry>();
                }
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

                            if (Index.GetFile(from) == 4 && board.PieceBoard[from].Piece == Piece.King && board.PieceBoard[to].Piece == Piece.Rook && promote == Piece.None)
                            {
                                foreach (Board.CastlingRookMove rookMove in Board.CastlingRookMoves)
                                {
                                    if (rookMove.KingFrom == from && rookMove.RookFrom == to && (board.Castling & rookMove.CastlingMask) != 0)
                                    {
                                        to = rookMove.KingTo;
                                        break;
                                    }
                                }
                            }
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

            var search = new BasicSearch(board, time, maxDepth, maxNodes, RandomSearch);
            searchThread = new Thread(search.Search)
            {
                Priority = ThreadPriority.Highest
            };
            IsRunning = true;
            searchThread.Start();
        }
    }
}
