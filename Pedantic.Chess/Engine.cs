// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Engine.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implement the functionality of the chess engine.
// </summary>
// ***********************************************************************

using Pedantic.Genetics;
using Pedantic.Utilities;
using System.Diagnostics;

namespace Pedantic.Chess
{
    public static class Engine
    {
        private static readonly GameClock time = new();
        private static int searchThreads = 1;
        private static Thread? searchThread;
        private static PolyglotEntry[]? bookEntries;
        private static Color color = Color.White;
        private static BasicSearch? search = null;

        public static bool Debug { get; set; } = false;
        public static bool IsRunning { get; private set; } = true;
        public static bool IsPondering { get; private set; } = true;
        public static bool Infinite { get; set; } = false;
        public static int MovesOutOfBook { get; set; } = 0;

        public static Board Board { get; } = new();

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
            // Version 0.1 of Pedantic only uses a single thread for search.
            set => searchThreads = Math.Max(value, 1);
        }
        public static Color SideToMove => Board.SideToMove;

        public static void Start()
        {
            Stop();
            IsRunning = true;
        }

        public static void Stop()
        {
            if (searchThread == null)
            {
                return;
            }

            WriteStats();
            time.Stop();
            searchThread.Join();
            searchThread = null;
            search = null;
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

        public static void Go(int maxTime, int opponentTime, int increment, int movesToGo, int maxDepth, long maxNodes, bool ponder = false)
        {
            Stop();
            IsPondering = ponder;
            time.Go(maxTime, opponentTime, increment, movesToGo, MovesOutOfBook, ponder || Infinite);
            StartSearch(maxDepth, maxNodes);
        }

        public static void ClearHashTable()
        {
            TtTran.Clear();
            TtEval.Clear();
            TtPawnEval.Clear();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        }

        public static void SetupNewGame()
        {
            Stop();
            ClearHashTable();
            MovesOutOfBook = 0;
        }

        public static void ResizeHashTable()
        {
            int sizeMb = UciOptions.Hash;
            if (!BitOps.IsPow2(sizeMb))
            {
                sizeMb = BitOps.GreatestPowerOfTwoLessThan(sizeMb);
                UciOptions.Hash = sizeMb;
            }

            TtTran.Resize(sizeMb);
            TtEval.Resize(sizeMb >> 2);
            TtPawnEval.Resize(sizeMb >> 5);
        }

        public static bool SetupPosition(string fen)
        {
            try
            {
                Stop();
                bool loaded = Board.LoadFenPosition(fen);
                if (loaded)
                {
                    Uci.Debug(@$"New position: {Board.ToFenString()}");
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
                if (Move.TryParseMove(Board, s, out ulong move))
                {
                    if (!Board.MakeMove(move))
                    {
                        throw new InvalidOperationException($"Invalid move passed to engine: '{s}'.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Long algebraic move expected. Bad format '{s}'.");
                }
            }

            Uci.Debug($@"New position: {Board.ToFenString()}");
        }

        public static void Wait()
        {
            if (searchThread == null)
            {
                return;
            }

            WriteStats();
            searchThread.Join();
            searchThread = null;
            search = null;
        }

        private static void WriteStats()
        {

            if (UciOptions.CollectStatistics && search != null)
            {
                using var mutex = new Mutex(false, "Pedantic::chess_stats.csv");
                mutex.WaitOne();
                using StreamWriter output = File.AppendText("chess_stats.csv");
                foreach (var st in search.Stats)
                {
                    output.WriteLine($"{st.Phase},{st.Depth},{st.NodesVisited}");
                }

                output.Flush();
                output.Close();
                mutex.ReleaseMutex();
            }
        }

        public static void PonderHit()
        {
            if (!IsRunning)
            {
                return;
            }

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

        public static void LoadEvaluation()
        {
            Evaluation.LoadWeights(UciOptions.EvaluationID);
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
            try
            {
                int first = FindFirstBookMove(hash);
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
                string? dirFullName = Path.GetDirectoryName(exeFullName);
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
            if (!UciOptions.OwnBook)
            {
                return move;
            }

            if (!LookupBookMoves(Board.Hash, out var bookMoves))
            {
                return move;
            }

            int total = 0;
            foreach (PolyglotEntry entry in bookMoves)
            {
                total += entry.Weight;
            }

            int pick = Random.Shared.Next(total + 1);
            foreach (PolyglotEntry entry in bookMoves)
            {
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

                    if (Index.GetFile(from) == 4 && Board.PieceBoard[from].Piece == Piece.King && Board.PieceBoard[to].Piece == Piece.Rook && promote == Piece.None)
                    {
                        foreach (Board.CastlingRookMove rookMove in Board.CastlingRookMoves)
                        {
                            if (rookMove.KingFrom != from || rookMove.RookFrom != to ||
                                (Board.Castling & rookMove.CastlingMask) == 0)
                            {
                                continue;
                            }

                            to = rookMove.KingTo;
                            break;
                        }
                    }
                    move = $@"{Index.ToString(from)}{Index.ToString(to)}{Conversion.PieceToString(promote)}";
                    break;
                }
            }

            return move;
        }

        private static void StartSearch(int maxDepth, long maxNodes)
        {
            string move = GetBookMove();
            if (move != "0000")
            {
                if (Move.TryParseMove(Board, move, out ulong parsedMove))
                {
                    Uci.BestMove(move);
                    return;
                }
            }

            ++MovesOutOfBook;
            search = new(Board, time, maxDepth, maxNodes, UciOptions.RandomSearch)
            {
                CanPonder = UciOptions.Ponder,
                CollectStats = UciOptions.CollectStatistics
            };
            Debugger.Break();
            searchThread = new Thread(() => search.Search())
            {
                Priority = ThreadPriority.Highest
            };
            searchThread.Start();
            IsRunning = true;
        }
    }
}
