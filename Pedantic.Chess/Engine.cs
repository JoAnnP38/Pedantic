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

using Pedantic.Tablebase;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class Engine
    {
        private static readonly GameClock time = new();
        private static PolyglotEntry[]? bookEntries;
        private static HceWeights? weights;
        private static Color color = Color.White;
        private static readonly SearchThreads threads = new();
        private static readonly string[] benchFens =
        {
            #region bench FENs
			"r3k2r/2pb1ppp/2pp1q2/p7/1nP1B3/1P2P3/P2N1PPP/R2QK2R w KQkq a6 0 14",
			"4rrk1/2p1b1p1/p1p3q1/4p3/2P2n1p/1P1NR2P/PB3PP1/3R1QK1 b - - 2 24",
			"r3qbrk/6p1/2b2pPp/p3pP1Q/PpPpP2P/3P1B2/2PB3K/R5R1 w - - 16 42",
			"6k1/1R3p2/6p1/2Bp3p/3P2q1/P7/1P2rQ1K/5R2 b - - 4 44",
			"8/8/1p2k1p1/3p3p/1p1P1P1P/1P2PK2/8/8 w - - 3 54",
			"7r/2p3k1/1p1p1qp1/1P1Bp3/p1P2r1P/P7/4R3/Q4RK1 w - - 0 36",
			"r1bq1rk1/pp2b1pp/n1pp1n2/3P1p2/2P1p3/2N1P2N/PP2BPPP/R1BQ1RK1 b - - 2 10",
			"3r3k/2r4p/1p1b3q/p4P2/P2Pp3/1B2P3/3BQ1RP/6K1 w - - 3 87",
			"2r4r/1p4k1/1Pnp4/3Qb1pq/8/4BpPp/5P2/2RR1BK1 w - - 0 42",
			"4q1bk/6b1/7p/p1p4p/PNPpP2P/KN4P1/3Q4/4R3 b - - 0 37",
			"2q3r1/1r2pk2/pp3pp1/2pP3p/P1Pb1BbP/1P4Q1/R3NPP1/4R1K1 w - - 2 34",
			"1r2r2k/1b4q1/pp5p/2pPp1p1/P3Pn2/1P1B1Q1P/2R3P1/4BR1K b - - 1 37",
			"r3kbbr/pp1n1p1P/3ppnp1/q5N1/1P1pP3/P1N1B3/2P1QP2/R3KB1R b KQkq b3 0 17",
			"8/6pk/2b1Rp2/3r4/1R1B2PP/P5K1/8/2r5 b - - 16 42",
			"1r4k1/4ppb1/2n1b1qp/pB4p1/1n1BP1P1/7P/2PNQPK1/3RN3 w - - 8 29",
			"8/p2B4/PkP5/4p1pK/4Pb1p/5P2/8/8 w - - 29 68",
			"3r4/ppq1ppkp/4bnp1/2pN4/2P1P3/1P4P1/PQ3PBP/R4K2 b - - 2 20",
			"5rr1/4n2k/4q2P/P1P2n2/3B1p2/4pP2/2N1P3/1RR1K2Q w - - 1 49",
			"1r5k/2pq2p1/3p3p/p1pP4/4QP2/PP1R3P/6PK/8 w - - 1 51",
			"q5k1/5ppp/1r3bn1/1B6/P1N2P2/BQ2P1P1/5K1P/8 b - - 2 34",
			"r1b2k1r/5n2/p4q2/1ppn1Pp1/3pp1p1/NP2P3/P1PPBK2/1RQN2R1 w - - 0 22",
			"r1bqk2r/pppp1ppp/5n2/4b3/4P3/P1N5/1PP2PPP/R1BQKB1R w KQkq - 0 5",
			"r1bqr1k1/pp1p1ppp/2p5/8/3N1Q2/P2BB3/1PP2PPP/R3K2n b Q - 1 12",
			"r1bq2k1/p4r1p/1pp2pp1/3p4/1P1B3Q/P2B1N2/2P3PP/4R1K1 b - - 2 19",
			"r4qk1/6r1/1p4p1/2ppBbN1/1p5Q/P7/2P3PP/5RK1 w - - 2 25",
			"r7/6k1/1p6/2pp1p2/7Q/8/p1P2K1P/8 w - - 0 32",
			"r3k2r/ppp1pp1p/2nqb1pn/3p4/4P3/2PP4/PP1NBPPP/R2QK1NR w KQkq - 1 5",
			"3r1rk1/1pp1pn1p/p1n1q1p1/3p4/Q3P3/2P5/PP1NBPPP/4RRK1 w - - 0 12",
			"5rk1/1pp1pn1p/p3Brp1/8/1n6/5N2/PP3PPP/2R2RK1 w - - 2 20",
			"8/1p2pk1p/p1p1r1p1/3n4/8/5R2/PP3PPP/4R1K1 b - - 3 27",
			"8/4pk2/1p1r2p1/p1p4p/Pn5P/3R4/1P3PP1/4RK2 w - - 1 33",
			"8/5k2/1pnrp1p1/p1p4p/P6P/4R1PK/1P3P2/4R3 b - - 1 38",
			"8/8/1p1kp1p1/p1pr1n1p/P6P/1R4P1/1P3PK1/1R6 b - - 15 45",
			"8/8/1p1k2p1/p1prp2p/P2n3P/6P1/1P1R1PK1/4R3 b - - 5 49",
			"8/8/1p4p1/p1p2k1p/P2npP1P/4K1P1/1P6/3R4 w - - 6 54",
			"8/8/1p4p1/p1p2k1p/P2n1P1P/4K1P1/1P6/6R1 b - - 6 59",
			"8/5k2/1p4p1/p1pK3p/P2n1P1P/6P1/1P6/4R3 b - - 14 63",
			"8/1R6/1p1K1kp1/p6p/P1p2P1P/6P1/1Pn5/8 w - - 0 67",
			"1rb1rn1k/p3q1bp/2p3p1/2p1p3/2P1P2N/PP1RQNP1/1B3P2/4R1K1 b - - 4 23",
			"4rrk1/pp1n1pp1/q5p1/P1pP4/2n3P1/7P/1P3PB1/R1BQ1RK1 w - - 3 22",
			"r2qr1k1/pb1nbppp/1pn1p3/2ppP3/3P4/2PB1NN1/PP3PPP/R1BQR1K1 w - - 4 12",
			"2r2k2/8/4P1R1/1p6/8/P4K1N/7b/2B5 b - - 0 55",
			"6k1/5pp1/8/2bKP2P/2P5/p4PNb/B7/8 b - - 1 44",
			"2rqr1k1/1p3p1p/p2p2p1/P1nPb3/2B1P3/5P2/1PQ2NPP/R1R4K w - - 3 25",
			"r1b2rk1/p1q1ppbp/6p1/2Q5/8/4BP2/PPP3PP/2KR1B1R b - - 2 14",
			"6r1/5k2/p1b1r2p/1pB1p1p1/1Pp3PP/2P1R1K1/2P2P2/3R4 w - - 1 36",
			"rnbqkb1r/pppppppp/5n2/8/2PP4/8/PP2PPPP/RNBQKBNR b KQkq c3 0 2",
			"2rr2k1/1p4bp/p1q1p1p1/4Pp1n/2PB4/1PN3P1/P3Q2P/2RR2K1 w - f6 0 20",
			"3br1k1/p1pn3p/1p3n2/5pNq/2P1p3/1PN3PP/P2Q1PB1/4R1K1 w - - 0 23",
			"2r2b2/5p2/5k2/p1r1pP2/P2pB3/1P3P2/K1P3R1/7R w - - 23 93"
            #endregion
        };

        public static bool Debug { get; set; } = false;
        public static bool IsRunning { get; private set; } = true;
        public static bool IsPondering { get; private set; } = true;
        public static bool Infinite { get; set; } = false;
        public static int MovesOutOfBook { get; set; } = 0;

        public static Board Board { get; } = new();

        public static Color Color
        {
            get => color;
            set => color = value;
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

        public static HceWeights Weights
        {
            get
            {
                if (weights == null)
                {
                    LoadWeights();
                }

                return weights ?? new HceWeights();
            }
        }

        public static int SearchThreads
        {
            get => threads.ThreadCount;
            set => threads.ThreadCount = value;
        }
        public static Color SideToMove => Board.SideToMove;

        public static void Start()
        {
            Stop();
            IsRunning = true;
        }

        public static void Stop()
        {
            threads.WriteStats();
            threads.Stop();
            threads.Wait();
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
            TtTran.Default.Clear();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            threads.ClearEvalCache();
        }

        public static void SetupNewGame()
        {
            Stop();
            ClearHashTable();
            LoadBookEntries();
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

            TtTran.Default.Resize(sizeMb);
            threads.ResizeEvalCache();
        }

        public static bool SetupPosition(string fen)
        {
            try
            {
                Stop();
                bool loaded = Board.LoadFenPosition(fen);
                if (loaded)
                {
                    Uci.Default.Debug(@$"New position: {Board.ToFenString()}");
                }

                if (!loaded)
                {
                    Uci.Default.Log("Engine failed to load position.");
                }

                return loaded;
            }
            catch (Exception e)
            {
                Uci.Default.Log(@$"Engine faulted: {e.Message}");
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

            Uci.Default.Debug($@"New position: {Board.ToFenString()}");
        }

        public static void Wait(bool preserveSearch = false)
        {
            threads.WriteStats();
            threads.Wait();
        }

        private static void WriteStats()
        {
            threads.WriteStats();
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
                if (!UciOptions.OwnBook)
                {
                    bookEntries = null;
                    return;
                }

                if (bookEntries != null)
                {
                    // the book has already been loaded
                    return;
                }

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

        public static void LoadWeights()
        {
            try
            {
                string? exeFullName = Environment.ProcessPath;
                string? dirFullName = Path.GetDirectoryName(exeFullName);
                string? weightsPath = (exeFullName != null && dirFullName != null) ?
                    Path.Combine(dirFullName, "Pedantic.hce") : null;

                if (weightsPath != null && File.Exists(weightsPath))
                {
                    weights = new HceWeights(weightsPath);
                    Evaluation.Weights = weights;
                }
                else
                {
                    weights = new HceWeights();
                    if (weightsPath != null)
                    {
                        weights.Save(weightsPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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

        private static bool ProbeRootTb(Board board, out ulong move, out TbGameResult gameResult)
        {
            move = 0;
            gameResult = TbGameResult.Draw;
            if (UciOptions.SyzygyProbeRoot && Syzygy.IsInitialized && BitOps.PopCount(board.All) <= Syzygy.TbLargest)
            {
                MoveList moveList = new();
                board.GenerateMoves(moveList);

                TbResult result = Syzygy.ProbeRoot(board.Units(Color.White), board.Units(Color.Black), 
                    board.Pieces(Color.White, Piece.King)   | board.Pieces(Color.Black, Piece.King),
                    board.Pieces(Color.White, Piece.Queen)  | board.Pieces(Color.Black, Piece.Queen),
                    board.Pieces(Color.White, Piece.Rook)   | board.Pieces(Color.Black, Piece.Rook),
                    board.Pieces(Color.White, Piece.Bishop) | board.Pieces(Color.Black, Piece.Bishop),
                    board.Pieces(Color.White, Piece.Knight) | board.Pieces(Color.Black, Piece.Knight),
                    board.Pieces(Color.White, Piece.Pawn)   | board.Pieces(Color.Black, Piece.Pawn),
                    (uint)board.HalfMoveClock, (uint)board.Castling, 
                    (uint)(board.EnPassantValidated != Index.NONE ? board.EnPassantValidated : 0), 
                    board.SideToMove == Color.White, null);

                gameResult = result.Wdl;
                int from = (int)result.From;
                int to = (int)result.To;
                uint tbPromotes = result.Promotes;
                Piece promote = (Piece)(5 - tbPromotes);
                promote = promote == Piece.King ? Piece.None : promote;

                for (int n = 0; n < moveList.Count; n++)
                {
                    move = moveList[n];
                    if (Move.GetFrom(move) == from && Move.GetTo(move) == to && Move.GetPromote(move) == promote)
                    {
                        if (board.MakeMove(move))
                        {
                            board.UnmakeMove();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void Bench(int depth)
        {
            long totalNodes = 0;
            double totalTime = 0;

            foreach (string fen in benchFens)
            {
                SetupNewGame();
                SetupPosition(fen);
                Go(depth, int.MaxValue, long.MaxValue, false);
                Wait(true);
                totalNodes += threads.TotalNodes;
                totalTime += threads.TotalTime;
            }
            double nps = totalNodes / totalTime;
            Uci.Default.Log($"depth {depth} time {totalTime:F4} nodes {totalNodes} nps {nps:F4}");
        }

        private static void StartSearch(int maxDepth, long maxNodes)
        {
            string move = GetBookMove();
            if (move != "0000")
            {
                if (Move.TryParseMove(Board, move, out ulong _))
                {
                    Uci.Default.BestMove(move);
                    return;
                }
            }

            if (ProbeRootTb(Board, out ulong mv, out TbGameResult gameResult))
            {
                Board clone = Board.Clone();
                ulong[] pv = new ulong[8];
                int pvInsert = 0;
                for (int ply = 0; ply < pv.Length; ply++)
                {
                    pv[pvInsert++] = mv;
                    clone.MakeMove(mv);
                    if (!ProbeRootTb(clone, out mv, out _))
                    {
                        break;
                    }
                }
                if (pvInsert < pv.Length)
                {
                    Array.Resize(ref pv, pvInsert);
                }
                int score = gameResult switch
                {
                    TbGameResult.Loss       => -Constants.CHECKMATE_SCORE,
                    TbGameResult.BlessedLoss=> -Constants.CHECKMATE_SCORE,
                    TbGameResult.Draw       => 0,
                    TbGameResult.CursedWin  => Constants.CHECKMATE_SCORE,
                    TbGameResult.Win        => Constants.CHECKMATE_SCORE,
                    _ => 0
                };
                Uci.Default.Info(1, 1, score, pvInsert, 0, pv, TtTran.Default.Usage, pvInsert);
                Uci.Default.BestMove(pv[0], null);
                return;
            }

            if (UciOptions.AnalyseMode)
            {
                ClearHashTable();
            }

            ++MovesOutOfBook;
            threads.Search(time, Board, maxDepth, maxNodes);
            IsRunning = true;
        }
    }
}
