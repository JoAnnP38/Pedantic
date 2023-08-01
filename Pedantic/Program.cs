// ***********************************************************************
// Assembly         : Pedantic
// Author           : JoAnn D. Peeler
// Created          : 03-12-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Program.cs" company="Pedantic">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Console application entry point (i.e. definition of Main()).
// </summary>
// ***********************************************************************
using Pedantic.Chess;
using Pedantic.Genetics;
using Pedantic.Utilities;
using Pedantic.Tablebase;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable LocalizableElement

namespace Pedantic
{
    public static class Program
    {
        public const string APP_NAME = Constants.APP_NAME;
        public const string APP_VERSION = Constants.APP_VERSION;
        public const string APP_NAME_VER = APP_NAME + " " + APP_VERSION;
        public const string AUTHOR = "JoAnn D. Peeler";
        public const string PROGRAM_URL = "https://github.com/JoAnnP38/Pedantic";
        public const double MINI_CONVERGENCE_TOLERANCE = 0.00000005;
        public const double FULL_CONVERGENCE_TOLERANCE = 0.0000001;
        public const int MAX_CONVERGENCE_FAILURE = 2;
        public const int MINI_BATCH_COUNT = 40;
        public const int MINI_BATCH_MIN_SIZE = 10000;

        private enum PerftRunType
        {
            Normal,
            Average,
            Details,
            Divide
        }

        private static int Main(string[] args)
        {
            var typeOption = new Option<PerftRunType>(
                name: "--type",
                description: "Specifies the perft variant to execute.",
                getDefaultValue: () => PerftRunType.Normal);
            var depthOption = new Option<int>(
                name: "--depth",
                description: "Specifies the maximum depth for the perft test.",
                getDefaultValue: () => 6);
            var fenOption = new Option<string?>(
                name: "--fen",
                description: "Specifies the starting position if other than the default.",
                getDefaultValue: () => null);
            var commandFileOption = new Option<string?>(
                name: "--input",
                description: "Specify a file read UCI commands from.",
                getDefaultValue: () => null);
            var errorFileOption = new Option<string?>(
                name: "--error",
                description: "Output errors to specified file.",
                getDefaultValue: () => null);
            var randomSearchOption = new Option<bool>(
                name: "--random",
                description: "If specified adds small random amount to positional evaluations.",
                getDefaultValue: () => false);
            var pgnFileOption = new Option<string?>(
                name: "--pgn",
                description: "Specifies a PGN input file.",
                getDefaultValue: () => null);
            var dataFileOption = new Option<string?>(
                name: "--data",
                description: "The name of the labeled data output file.",
                getDefaultValue: () => null);
            var maxPositionsOption = new Option<int>(
                name: "--maxpos",
                description: "Specify the maximum positions to output.",
                getDefaultValue: () => 8000000);
            var sampleOption = new Option<int>(
                name: "--sample",
                description: "Specify the number of samples to use from learning data.",
                getDefaultValue: () => -1);
            var iterOption = new Option<int>(
                name: "--iter",
                description: "Specify the maximum number of iterations before a solution is declared.",
                getDefaultValue: () => 100);
            var preserveOption = new Option<bool>(
                name: "--preserve",
                description: "When present intermediary versions of the solution will be saved.",
                getDefaultValue: () => false);
            var saveOption = new Option<bool>(
                name: "--save",
                description: "If specified the sample will be saved in file.",
                getDefaultValue: () => false);
            var fullOption = new Option<int>(
                name: "--full",
                description: "Specify the number of full batch optimization iterations to complete optimization.",
                getDefaultValue: () => 0);
            var resetOption = new Option<bool>(
                name: "--reset",
                description: "Reset most starting weights to zero before learning begins.",
                getDefaultValue: () => false);
            var immortalOption = new Option<string?>(
                name: "--immortal",
                description: "Designate a new immortal set of weights.",
                getDefaultValue: () => null);
            var displayOption = new Option<string?>(
                name: "--display",
                description: "Display the specific set of weights as C# code.",
                getDefaultValue: () => null);
            var statsOption = new Option<bool>(
                name: "--stats",
                description: "Collect search statistics",
                getDefaultValue: () => false);
            var magicOption = new Option<bool>(
                name: "--force_magic",
                description: "Force the use of magic bitboards.",
                getDefaultValue: () => false);

            var uciCommand = new Command("uci", "Start the pedantic application in UCI mode (default).")
            {
                commandFileOption,
                errorFileOption,
                randomSearchOption,
                statsOption,
                magicOption
            };

            var perftCommand = new Command("perft", "Run a standard Perft test.")
            {
                typeOption,
                depthOption,
                fenOption,
                magicOption
            };

            var labelCommand = new Command("label", "Pre-process and label PGN data.")
            {
                pgnFileOption,
                dataFileOption,
                maxPositionsOption
            };

            var learnCommand = new Command("learn", "Optimize evaluation function using training data.")
            {
                dataFileOption,
                sampleOption,
                iterOption,
                preserveOption,
                saveOption,
                fullOption,
                resetOption
            };

            var weightsCommand = new Command("weights", "Manipulate the weight database.")
            {
                immortalOption,
                displayOption
            };

            var rootCommand = new RootCommand("The pedantic chess engine.")
            {
                uciCommand,
                perftCommand,
                labelCommand,
                learnCommand,
                weightsCommand
            };

            uciCommand.SetHandler(RunUci, commandFileOption, errorFileOption, randomSearchOption, statsOption, magicOption);
            perftCommand.SetHandler(RunPerft, typeOption, depthOption, fenOption, magicOption);
            labelCommand.SetHandler(RunLabel, pgnFileOption, dataFileOption, maxPositionsOption);
            learnCommand.SetHandler(RunLearn, dataFileOption, sampleOption, iterOption, preserveOption, saveOption, fullOption, resetOption);
            weightsCommand.SetHandler(RunWeights, immortalOption, displayOption);
            rootCommand.SetHandler(async () => await RunUci(null, null, false, false, false));

            return rootCommand.InvokeAsync(args).Result;
        }

        private static async Task RunUci(string? inFile, string? errFile, bool random, bool stats, bool forceMagic)
        {
            GlobalOptions.DisablePextBitboards = forceMagic;

            TextReader? stdin = null;
            TextWriter? stderr = null;

            if (inFile != null && File.Exists(inFile))
            {
                stdin = Console.In;
                StreamReader inStream = new(inFile, Encoding.UTF8);
                Console.SetIn(inStream);
            }

            if (errFile != null)
            {
                stderr = Console.Error;
                StreamWriter errStream = File.AppendText(errFile);
                Console.SetError(errStream);
            }

            try
            {
                Console.WriteLine(APP_NAME_VER);
                UciOptions.RandomSearch = random;
                UciOptions.CollectStatistics = stats;
                Engine.Start();
                while (Engine.IsRunning)
                {
                    string? input = await Task.Run(Console.ReadLine);
                    if (input != null && !string.IsNullOrWhiteSpace(input))
                    {
                        ParseCommand(input);
                    }
                }
            }
            catch (Exception e)
            {
                Uci.Log(@$"Fatal error occurred in Pedantic: '{e.Message}'.");
                await Console.Error.WriteAsync(Environment.NewLine);
                await Console.Error.WriteLineAsync($@"[{DateTime.Now}]");
                await Console.Error.WriteLineAsync(e.ToString());
            }
            finally
            {
                var input = Console.In;
                var error = Console.Error;

                if (stdin != null)
                {
                    Console.SetIn(stdin);
                    input.Close();
                }

                if (stderr != null)
                {
                    Console.SetError(stderr);
                    error.Close();
                }
            }
        }

        public static void ParseCommand(string input)
        {
            input = input.Trim();
            string[] tokens = input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            switch (tokens[0])
            {
                case "uci":
                    Console.WriteLine($@"id name {APP_NAME_VER}");
                    Console.WriteLine($@"id author {AUTHOR}");
                    Console.WriteLine(@"option name Clear Hash type button");
                    Console.WriteLine(@"option name CollectStats type check default false");
                    Console.WriteLine(@"option name EvaluationID type string default <empty>");
                    Console.WriteLine($@"option name Hash type spin default {TtTran.DEFAULT_SIZE_MB} min 1 max {TtTran.MAX_SIZE_MB}");
                    //Console.WriteLine($@"option name Threads type spin default 1 min 1 max {Math.Max(Environment.ProcessorCount - 2, 1)}");
                    Console.WriteLine(@"option name OwnBook type check default true");
                    Console.WriteLine(@"option name Ponder type check default true");
                    Console.WriteLine(@"option name RandomSearch type check default false");
#if USE_TB
                    Console.WriteLine(@"option name SyzygyPath type string default <empty>");
                    Console.WriteLine(@"option name SyzygyProbeRoot type check default true");
                    Console.WriteLine($@"option name SyzygyProbeDepth type spin default 2 min 0 max {Constants.MAX_PLY - 1}");
#endif
                    Console.WriteLine($@"option name UCI_AnalyseMode type check default false");
                    Console.WriteLine($@"option name UCI_EngineAbout type string default {APP_NAME_VER} by {AUTHOR}, see {PROGRAM_URL}");
                    Console.WriteLine(@"uciok");
                    break;

                case "isready":
                    Engine.LoadBookEntries();
                    Console.WriteLine(@"readyok");
                    break;

                case "position":
                    SetupPosition(tokens);
                    break;

                case "setoption":
                    SetOption(tokens, input);
                    break;

                case "ucinewgame":
                    Engine.SetupNewGame();                    
                    break;

                case "go":
                    Go(tokens);
                    break;

                case "stop":
                    Engine.Stop();
                    break;

                case "quit":
                    Engine.Quit();
                    break;

                case "wait":
                    Engine.Wait();
                    break;

                case "debug":
                    Debug(tokens);
                    break;

                case "ponderhit":
                    Engine.PonderHit();
                    break;

                default:
                    Uci.Log($@"Unexpected input: '{input}'");
                    return;
            }
        }

        private static void SetupPosition(string[] tokens)
        {
            if (tokens[1] == "startpos")
            {
                Engine.SetupPosition(Constants.FEN_START_POS);
            }
            else if (tokens[1] == "fen")
            {
                string fen = string.Join(' ', tokens[2..8]);
                Engine.SetupPosition(fen);
            }
            else
            {
                Uci.Log("'position' parameters missing or not understood. Assuming 'startpos'.");
                Engine.SetupPosition(Constants.FEN_START_POS);
            }

            int firstMove = Array.IndexOf(tokens, "moves") + 1;
            if (firstMove == 0)
            {
                return;
            }

            Engine.MakeMoves(tokens[firstMove..]);
        }

        private static void SetOption(string[] tokens, string line)
        {
            if (tokens[1] == "name")
            {
                switch (tokens[2])
                {
                    case "Hash":
                        if (tokens[3] == "value" && int.TryParse(tokens[4], out int sizeMb))
                        {
                            UciOptions.Hash = sizeMb;
                            Engine.ResizeHashTable();
                        }
                        break;

                    case "OwnBook":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool useOwnBook))
                        {
                            UciOptions.OwnBook = useOwnBook;
                        }
                        break;

                    case "Clear":
                        if (tokens[3] == "Hash")
                        {
                            Engine.ClearHashTable();
                        }
                        break;

                    /*case "MaxThreads":
                        if (tokens[3] == "value" && int.TryParse(tokens[4], out int searchThreads))
                        {
                            Engine.SearchThreads = searchThreads;
                        }
                        break;*/

                    case "Ponder":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool canPonder))
                        {
                            UciOptions.Ponder = canPonder;
                        }

                        break;

                    case "EvaluationID":
                        if (tokens[3] == "value")
                        {
                            bool validGuidFormat = tokens.Length >= 5 && Guid.TryParse(tokens[4], out Guid _);
                            if (!validGuidFormat)
                            {
                                Uci.Debug($"Ignoring illegal GUID specified for evaluation ID: '{(tokens.Length >= 5 ? tokens[4] : string.Empty)}'.");
                            }
                            UciOptions.EvaluationID = validGuidFormat ? Guid.Parse(tokens[4]) : null;
                            Engine.LoadEvaluation();
                        }

                        break;

                    case "RandomSearch":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool randomSearch))
                        {
                            UciOptions.RandomSearch = randomSearch;
                        }

                        break;

                    case "CollectStats":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool collectStats))
                        {
                            UciOptions.CollectStatistics = collectStats;
                        }

                        break;

                    case "UCI_AnalyseMode":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool analyseMode))
                        {
                            UciOptions.AnalyseMode = analyseMode;
                        }
                        break;
#if USE_TB
                    case "SyzygyPath":
                        string valueToken = " value ";
                        int index = line.IndexOf(valueToken);
                        if (index >= 0)
                        {
                            int start = index + valueToken.Length;
                            string path = line[start..].Trim();
                            if (path != "<empty>")
                            {
                                if (!Path.Exists(path))
                                {
                                    Uci.Log($"Ignoring specified SyzygyPath: '{path}'. Path doesn't exist.");
                                }
                                else
                                {
                                    bool result = Syzygy.Initialize(path);
                                    if (!result)
                                    {
                                        Uci.Log($"Could not locate valid Syzygy tablebase files at '{path}'.");
                                    }
                                    else
                                    {
                                        UciOptions.SyzygyPath = path;
                                    }
                                }
                            }
                        }
                        break;

                    case "SyzygyProbeRoot":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool probeRoot))
                        {
                            UciOptions.SyzygyProbeRoot = probeRoot;
                        }
                        break;

                    case "SyzygyProbeDepth":
                        if (tokens[3] == "value" && int.TryParse(tokens[4], out int probeDepth))
                        {
                            UciOptions.SyzygyProbeDepth = Math.Max(Math.Min(probeDepth, Constants.MAX_PLY - 1), 0);
                        }
                        break;
#endif
                }
            }
        }

        private static void Go(string[] tokens)
        {
            TryParse(tokens, "depth", out int maxDepth, Constants.MAX_PLY);
            TryParse(tokens, "movetime", out int maxTime, int.MaxValue);
            TryParse(tokens, "nodes", out long maxNodes, long.MaxValue);
            TryParse(tokens, "movestogo", out int movesToGo, -1);
            bool ponder = Array.Exists(tokens, item => item.Equals("ponder"));

            int blackTime;
            if (Engine.SideToMove == Color.White && TryParse(tokens, "wtime", out int whiteTime))
            {
                TryParse(tokens, "winc", out int whiteIncrement);
                TryParse(tokens, "btime", out blackTime, whiteTime);
                Engine.Go(whiteTime, blackTime, whiteIncrement, movesToGo, maxDepth, maxNodes, ponder);
            }
            else if (Engine.SideToMove == Color.Black && TryParse(tokens, "btime", out blackTime))
            {
                TryParse(tokens, "binc", out int blackIncrement);
                TryParse(tokens, "wtime", out whiteTime, blackTime);
                Engine.Go(blackTime, whiteTime, blackIncrement, movesToGo, maxDepth, maxNodes, ponder);
            }
            else
            {
                Engine.Go(maxDepth, maxTime, maxNodes, ponder);
            }
        }

        private static void Debug(string[] tokens)
        {
            Engine.Debug = tokens[1] == "on";
        }

        private static bool TryParse(string[] tokens, string name, out int value, int defaultValue = 0)
        {
            if (int.TryParse(Token(tokens, name), out value))
                return true;
            value = defaultValue;
            return false;
        }

        private static bool TryParse(string[] tokens, string name, out long value, long defaultValue = 0)
        {
            if (long.TryParse(Token(tokens, name), out value))
                return true;
            value = defaultValue;
            return false;
        }

        private static string? Token(string[] tokens, string name)
        {
            int iParam = Array.IndexOf(tokens, name);
            if (iParam < 0) return null;

            int iValue = iParam + 1;
            return (iValue < tokens.Length) ? tokens[iValue] : null;
        }

        private static void RunPerft(PerftRunType runType, int depth, string? fen = null, bool forceMagic = false)
        {
            GlobalOptions.DisablePextBitboards = forceMagic;

            Console.WriteLine($@"{APP_NAME_VER}");
            Perft perft = new(fen);
            switch (runType)
            {
                case PerftRunType.Normal:
                    RunNormalPerft(perft, depth);
                    break;

                case PerftRunType.Details:
                    RunDetailedPerft(perft, depth);
                    break;

                case PerftRunType.Average:
                    RunAveragePerft(perft, depth);
                    break;

                case PerftRunType.Divide:
                    RunDividePerft(perft, fen, depth);
                    break;
            }
        }

        private static void RunDividePerft(Perft perft, string? fen, int depth)
        {
            Console.WriteLine($"Calculating Perft({depth})...");
            ulong nodes = perft.Divide(depth, out Perft.DivideCount[] rootCounts);
            Board board = new(fen ?? Constants.FEN_START_POS);

            foreach (Perft.DivideCount divCnt in rootCounts)
            {
                Console.Write($"{Move.ToString(divCnt.Move)} : {divCnt.Count} : ");
                board.MakeMove(divCnt.Move);
                Console.WriteLine($"\"{board.ToFenString()}\"");
                board.UnmakeMove();
            }

            Console.WriteLine();
            Console.WriteLine($"Total nodes for depth ({depth}) : {nodes}");
        }

        private static void RunNormalPerft(Perft perft, int totalDepth)
        {
            Console.WriteLine(@"Single threaded results:");
            Stopwatch watch = new();

            for (int depth = 1; depth <= totalDepth; ++depth)
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                watch.Restart();
                ulong actual = perft.Execute(depth);
                watch.Stop();

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                double Mnps = (double)actual / (watch.Elapsed.TotalSeconds * 1000000.0D);
                Console.WriteLine($@"{depth}: Elapsed = {watch.Elapsed}, Mnps: {Mnps,6:N2}, nodes = {actual}");
            }
        }

        private static void RunAveragePerft(Perft perft, int totalDepth)
        {
            Console.WriteLine(@$"Calculating Perft({totalDepth}) Average Mnps...");
            Stopwatch watch = new();

            const int trials = 5;
            double totalSeconds = 0.0D;
            double totalNodes = 0.0D;
            TimeSpan ts = new();
            for (int i = 0; i < trials; ++i)
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                watch.Restart();
                totalNodes += perft.Execute(totalDepth);
                watch.Stop();

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                totalSeconds += watch.Elapsed.TotalSeconds;
                ts += watch.Elapsed;
            }

            TimeSpan avg = ts / (double)trials;
            Console.WriteLine(@"Average Perft({2}) Average elapsed: {0}, Mnps: {1,6:N2}", avg, totalNodes / (totalSeconds * 1000000.0D), totalDepth);
        }

        private static void RunDetailedPerft(Perft perft, int totalDepth)
        {
            Stopwatch watch = new();

            Console.WriteLine(@"Running Perft and collecting details...");
            Console.WriteLine(@"+-------+--------+--------------+------------+---------+-----------+------------+------------+------------+");
            Console.WriteLine(@"| Depth |  Mnps  |     Nodes    |  Captures  |   E.p.  |  Castles  |   Checks   | Checkmates | Promotions |");
            Console.WriteLine(@"+-------+--------+--------------+------------+---------+-----------+------------+------------+------------+");

            for (int depth = 1; depth <= totalDepth; ++depth)
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                watch.Restart();
                Perft.Counts counts = perft.ExecuteWithDetails(depth);
                watch.Stop();

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                double Mnps = (double)counts.Nodes / (watch.Elapsed.TotalSeconds * 1000000.0D);
                Console.WriteLine(@$"|{depth,4:N0}   | {Mnps,6:N2} |{counts.Nodes,13:N0} |{counts.Captures,11:N0} |{counts.EnPassants,8:N0} |{counts.Castles,10:N0} |{counts.Checks,11:N0} | {counts.Checkmates,10:N0} |{counts.Promotions,11:N0} |");
                Console.WriteLine(@"+-------+--------+--------------+------------+---------+-----------+------------+------------+------------+");
            }
        }

        private static void RunLabel(string? pgnFile, string? dataFile, int maxPositions = 8000000)
        {
            TextReader? stdin = null;
            TextWriter? stdout = null;

            if (pgnFile != null && File.Exists(pgnFile))
            {
                stdin = Console.In;
                StreamReader inStream = new(pgnFile, Encoding.UTF8);
                Console.SetIn(inStream);
            }

            if (dataFile != null)
            {
                stdout = Console.Out;
                StreamWriter dataStream = File.CreateText(dataFile);
                Console.SetOut(dataStream);
            }

            try
            {
                long total = 0;
                PgnPositionReader posReader = new();

                long count = 0;
                HashSet<ulong> hashes = new();
                Console.WriteLine(@"Hash,Ply,GamePly,FEN,HasCastled,Result");
                foreach (var p in posReader.Positions(Console.In))
                {
                    if (!hashes.Contains(p.Hash))
                    {
                        Console.Error.Write($"{++count}\r");
                        hashes.Add(p.Hash);
                        Console.WriteLine($@"{p.Hash:X16},{p.Ply},{p.GamePly},{p.Fen},{p.HasCastled},{p.Result:F1}");
                        Console.Out.Flush();
                        if (++total >= maxPositions)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"\n{e}");
            }
            finally
            {
                var input = Console.In;
                var output = Console.Out;

                if (stdin != null)
                {
                    input.Close();
                    Console.SetIn(stdin);
                }

                if (stdout != null)
                {
                    output.Close();
                    Console.SetOut(stdout);
                }
            }
        }

        private static void RunLearn(string? dataFile, int sampleSize, int maxPass = 200, bool preserve = false, bool save = false, int full = 0, bool reset = false)
        {
            if (string.IsNullOrEmpty(dataFile))
            {
                Console.Error.WriteLine("ERROR: A data file path must be specified.");
                return;
            }
            if (!File.Exists(dataFile))
            {
                Console.Error.WriteLine("ERROR: The specified data file does not exist.");
                return;
            }

            if (maxPass <= 0)
            {
                Console.Error.WriteLine("ERROR: Iterations must be greater than zero.");
                return;
            }

            DateTime start = DateTime.Now;
            TextReader savedIn = Console.In;
            StreamReader streamReader = new(dataFile, Encoding.UTF8);
            Console.SetIn(streamReader);
            full = Math.Max(Math.Min(full, 10), 0);

            try
            {
                bool[] fixedWeights = new bool[ChessWeights.MAX_WEIGHTS];
                /*
                fixedWeights[ChessWeights.GAME_PHASE_MATERIAL] = true;
                fixedWeights[ChessWeights.GAME_PHASE_MATERIAL + ChessWeights.ENDGAME_WEIGHTS] = true;
                for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
                {
                    fixedWeights[ChessWeights.PIECE_VALUES + pc] = true;
                    fixedWeights[ChessWeights.PIECE_VALUES + ChessWeights.ENDGAME_WEIGHTS + pc] = true;
                }
                */

                List<Memory<PosRecord>> slices;
                PosRecord[] records;
                if (sampleSize == -1)
                {
                    sampleSize = GetDataSize(streamReader);
                    Console.WriteLine($@"Sample size: {sampleSize}, Start time: {DateTime.Now:G}");
                    records = LoadDataFile(streamReader, sampleSize);
                    slices = CreateSlices(records);
                }
                else
                {
                    Console.WriteLine($@"Sample size: {sampleSize}, Start time: {DateTime.Now:G}");
                    string savedSampleName = $"Pedantic_Sample_{sampleSize}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    using StreamWriter? streamWriter = save ? new(savedSampleName, false, Encoding.UTF8) : null;
                    records = LoadSample(ref sampleSize, streamReader, streamWriter);
                    slices = CreateSlices(records);
                    streamWriter?.Close();
                }

                ChessDb rep = new();
                ChessWeights? startingWeight = null;
                
                if (!reset)
                {
                    startingWeight = rep.Weights
                    .Where(w => w.IsActive && w.IsImmortal)
                    .MinBy(w => w.CreatedOn);
                }
                else
                {
                    short[] resetWts = new short[ChessWeights.MAX_WEIGHTS];
                    resetWts[ChessWeights.GAME_PHASE_MATERIAL] = 6900;
                    resetWts[ChessWeights.GAME_PHASE_MATERIAL + ChessWeights.ENDGAME_WEIGHTS] = 1000;
                    resetWts[ChessWeights.PIECE_VALUES + (int)Piece.Pawn] = 100;
                    resetWts[ChessWeights.PIECE_VALUES + (int)Piece.Knight] = 300;
                    resetWts[ChessWeights.PIECE_VALUES + (int)Piece.Bishop] = 325;
                    resetWts[ChessWeights.PIECE_VALUES + (int)Piece.Rook] = 500;
                    resetWts[ChessWeights.PIECE_VALUES + (int)Piece.Queen] = 900;
                    resetWts[ChessWeights.PIECE_VALUES + ChessWeights.ENDGAME_WEIGHTS + (int)Piece.Pawn] = 100;
                    resetWts[ChessWeights.PIECE_VALUES + ChessWeights.ENDGAME_WEIGHTS + (int)Piece.Knight] = 300;
                    resetWts[ChessWeights.PIECE_VALUES + ChessWeights.ENDGAME_WEIGHTS + (int)Piece.Bishop] = 325;
                    resetWts[ChessWeights.PIECE_VALUES + ChessWeights.ENDGAME_WEIGHTS + (int)Piece.Rook] = 500;
                    resetWts[ChessWeights.PIECE_VALUES + ChessWeights.ENDGAME_WEIGHTS + (int)Piece.Queen] = 900;
                    startingWeight = new ChessWeights(resetWts);
                }

                if (startingWeight != null)
                {
                    short[] weights = ArrayEx.Clone(startingWeight.Weights);
                    double k = reset ? 0.00385 : SolveKParallel(weights, slices);
                    Console.WriteLine($@"K = {k:F4}");
                    startingWeight.Fitness = (float)EvalErrorParallel(weights, slices, k);
                    ChessWeights guess = new(startingWeight)
                    {
                        IsImmortal = false
                    };
                    ChessWeights optimized = LocalOptimize(guess, weights, fixedWeights, slices, k, maxPass,
                        preserve, full, records);
                    optimized.Description = "Optimized";
                    optimized.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Insert(optimized);
                    rep.Save();
                    DateTime end = DateTime.Now;
                    Console.WriteLine($@"Optimization complete at: {DateTime.Now:G}, Elapsed: {end - start:g}");
                    PrintSolution(optimized);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\n{ex}");
            }
            finally
            {
                Console.In.Close();
                Console.SetIn(savedIn);
            }
        }

        private static ChessWeights LocalOptimize(ChessWeights guess, short[] weights, bool[] fixedWeights, 
            List<Memory<PosRecord>> slices, double k, int maxPass, bool preserve, int full, PosRecord[] records)
        {
            DateTime startTime = DateTime.Now;
            bool improved = true;
            double curError = guess.Fitness;
            double refError = MiniEvalErrorParallel(weights, slices, k, 0);
            double bestError = curError + MINI_CONVERGENCE_TOLERANCE * 2;
            short[] bestWeights = ArrayEx.Clone(weights);
            int wtLen = weights.Length;
            int passes = 0;
            int batchCounter = 0;
            int failureCount = 0;
            int sampleSize = slices.Sum(s => s.Length);
            var rep = new ChessDb();

            miniBatchCount = MINI_BATCH_COUNT;
            Console.WriteLine($"\nMini-batch optimization to begin ({miniBatchCount} batch count)\n");
            Console.WriteLine($"Pass stats {passes,3} - \u03B5: {curError:F6}");
            int[] index = new int[wtLen];
            for (int n = 0; n < wtLen; n++)
            {
                index[n] = n;
            }

            while (improved && failureCount < MAX_CONVERGENCE_FAILURE && passes < maxPass)
            {
                improved = false;
                if (curError < bestError)
                {
                    bestError = curError;
                    Array.Copy(weights, bestWeights, bestWeights.Length);
                }
                else
                {
                    Array.Copy(bestWeights, weights, weights.Length);
                }

                DateTime wtOptTime = DateTime.Now;
                Random.Shared.Shuffle(index);
                int optAttempts = 0;
                int optHits = 0;
                float effRate = 0;
                refError = MiniEvalErrorParallel(weights, slices, k, batchCounter);
                double errAdjust = curError / refError;

                for (int n = 0; n < wtLen; n++)
                {
                    double completed = ((double)n / wtLen) * 100.0;
                    TimeSpan deltaT = DateTime.Now - wtOptTime;
                    effRate = optAttempts > 0 ? (float)optHits / optAttempts : 0;
                    Console.Write($"Pass stats {completed,3:F0}%- \u03B5: {refError * errAdjust:F6}, \u0394t: {deltaT:h\\:mm\\:ss}, eff: {effRate:F3} ({optHits}/{optAttempts})...\r");
                    int i = index[n];
                    if (fixedWeights[i])
                    {
                        continue;
                    }

                    short increment = EvalFeatures.GetOptimizationIncrement(i);
                    if (increment > 0)
                    {
                        if (Random.Shared.NextBoolean())
                        {
                            increment = (short)-increment;
                        }
                        short oldValue = weights[i];
                        weights[i] += increment;
                        double error = MiniEvalErrorParallel(weights, slices, k, batchCounter);
                        optAttempts++;
                        bool goodIncrement = error < refError;
                        improved = improved || goodIncrement;

                        if (!goodIncrement)
                        {
                            weights[i] -= (short)(increment * 2);
                            error = MiniEvalErrorParallel(weights, slices, k, batchCounter);
                            optAttempts++;
                            goodIncrement = error < refError;
                            improved = improved || goodIncrement;
                        }

                        if (goodIncrement)
                        {
                            optHits++;
                            refError = error;
                        }
                        else
                        {
                            weights[i] = oldValue;
                        }
                    }
                }

                ++batchCounter;
                DateTime now = DateTime.Now;
                TimeSpan passT = now - wtOptTime;
                curError = EvalErrorParallel(weights, slices, k);

                if (curError + MINI_CONVERGENCE_TOLERANCE < bestError)
                {
                    failureCount = 0;
                    ++passes;
                }
                else
                {
                    ++failureCount;

                    if (failureCount >= 2 && (miniBatchCount / 2) > 2)
                    {
                        failureCount = 0;
                        miniBatchCount /= 2;
                        Random.Shared.Shuffle(records);
                        failureCount = 0;
                        Console.WriteLine(
                            $"Pass stats {passes, 3} - \u03B5: {curError:F6}, Δt: {passT:h\\:mm\\:ss}, NO IMPROVEMENT                              ");
                        Console.WriteLine($"\nIncreasing mini-batch size ({miniBatchCount} batch count)\n");
                        continue;
                    }
                }

                if (failureCount == 0)
                {
                    if (preserve && passes % 10 == 0 && improved)
                    {
                        ChessWeights intermediate = new(weights)
                        {
                            Description = $"Pass {passes}",
                            Fitness = (float)curError,
                            K = (float)k,
                            TotalPasses = (short)passes,
                            SampleSize = sampleSize,
                            UpdatedOn = DateTime.UtcNow
                        };
                        rep.Weights.Insert(intermediate);
                        rep.Save();
                        Console.WriteLine(
                            $"Pass stats {passes,3} - \u03B5: {curError:F6}, Δt: {passT:h\\:mm\\:ss}, eff: {effRate:F3}, OID: {intermediate.Id}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Pass stats {passes,3} - \u03B5: {curError:F6}, Δt: {passT:h\\:mm\\:ss}, eff: {effRate:F3}                        ");
                    }
                }
                else
                {
                    Console.WriteLine(
                        $"Pass stats {passes, 3} - \u03B5: {curError:F6}, Δt: {passT:h\\:mm\\:ss}, NO IMPROVEMENT                              ");
                }
            }

            if (full > 0 && passes < maxPass)
            {
                // if we still have passes left to perform, do the rest of the using non-mini-batch optimization
                improved = true;
                failureCount = 0;
                maxPass = passes + full;   // only do "full" more iterations with full batch

                Console.WriteLine($"\nMini-batch optimization complete. Full batch optimization to begin.\n");
                while (improved && failureCount == 0 && passes < maxPass)
                {
                    improved = false;
                    if (curError < bestError)
                    {
                        bestError = curError;
                        Array.Copy(weights, bestWeights, bestWeights.Length);
                    }
                    else
                    {
                        Array.Copy(bestWeights, weights, weights.Length);
                    }

                    DateTime wtOptTime = DateTime.Now;
                    Random.Shared.Shuffle(index);
                    int optAttempts = 0;
                    int optHits = 0;
                    float effRate = 0;
                    refError = EvalErrorParallel(weights, slices, k);
                    
                    for (int n = 0; n < wtLen; n++)
                    {
                        double completed = ((double)n / wtLen) * 100.0;
                        TimeSpan deltaT = DateTime.Now - wtOptTime;
                        effRate = optAttempts > 0 ? (float)optHits / optAttempts : 0;
                        Console.Write($"Pass stats {completed,3:F0}%- \u03B5: {refError:F7}, \u0394t: {deltaT:h\\:mm\\:ss}, eff: {effRate:F3} ({optHits}/{optAttempts})... \r");
                        int i = index[n];
                        if (fixedWeights[i])
                        {
                            continue;
                        }

                        short increment = EvalFeatures.GetOptimizationIncrement(i);
                        if (increment > 0)
                        {
                            if (Random.Shared.NextBoolean())
                            {
                                increment = (short)-increment;
                            }
                            short oldValue = weights[i];
                            weights[i] += increment;
                            double error = EvalErrorParallel(weights, slices, k);
                            optAttempts++;
                            bool goodIncrement = error < refError;
                            improved = improved || goodIncrement;

                            if (!goodIncrement)
                            {
                                weights[i] -= (short)(increment * 2);
                                error = EvalErrorParallel(weights, slices, k);
                                optAttempts++;
                                goodIncrement = error < refError;
                                improved = improved || goodIncrement;
                            }

                            if (goodIncrement)
                            {
                                optHits++;
                                refError = error;
                            }
                            else
                            {
                                weights[i] = oldValue;
                            }
                        }
                    }

                    DateTime now = DateTime.Now;
                    TimeSpan passT = now - wtOptTime;
                    curError = refError;

                    if (curError + FULL_CONVERGENCE_TOLERANCE < bestError)
                    {
                        ++passes;
                    }
                    else
                    {
                        failureCount = 1;
                    }

                    if (failureCount == 0)
                    {
                        if (preserve && passes % 10 == 0 && improved)
                        {
                            ChessWeights intermediate = new (weights)
                            {
                                Description = $"Pass {passes}",
                                Fitness = (float)curError,
                                K = (float)k,
                                TotalPasses = (short)passes,
                                SampleSize = sampleSize,
                                UpdatedOn = DateTime.UtcNow
                            };

                            rep.Weights.Insert(intermediate);
                            rep.Save();
                            Console.WriteLine(
                                $"Pass stats {passes,3} - \u03B5: {curError:F7}, Δt: {passT:h\\:mm\\:ss}, eff: {effRate:F3}, OID: {intermediate.Id}");
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Pass stats {passes,3} - \u03B5: {curError:F7}, Δt: {passT:h\\:mm\\:ss}, eff: {effRate:F3}                        ");
                        }
                    }
                }
            }

            ChessWeights optimized = new(bestWeights)
            {
                Description = "Optimized",
                Fitness = (float)curError,
                K = (float)k,
                TotalPasses = (short)passes,
                SampleSize = sampleSize,
                UpdatedOn = DateTime.UtcNow
            };
            return optimized;
        }

        public static PosRecord[] LoadSample(ref int sampleSize, StreamReader sr, StreamWriter? sw)
        {
            Stopwatch watch = new();
            Console.WriteLine("Loading samples...");
            int[] selections = GetSampleSelections(ref sampleSize, sr);
            PosRecord[] posRecords = new PosRecord[sampleSize];
            int posInsert = 0;
            int currLine = 0;

            try
            {
                string? header = Console.ReadLine();
                if (header != null)
                {
                    sw?.WriteLine(header);
                }
                watch.Start();
                long currMs = watch.ElapsedMilliseconds;
                foreach (int selLine in selections)
                {
                    while (currLine++ < selLine)
                    {
                        if (Console.ReadLine() == null)
                        {
                            break;
                        }
                    }

                    //Console.Write($"Loading line: {currLine}\r");
                    string? str = Console.ReadLine();
                    if (str == null)
                    {
                        break;
                    }
                    sw?.WriteLine(str);
                    string[] fields = str.Split(',');
                    string fen = fields[3];
                    byte hasCastled = byte.Parse(fields[4]);
                    float result = float.Parse(fields[5]);
                    posRecords[posInsert++] = new PosRecord(fen, hasCastled, result);

                    if (watch.ElapsedMilliseconds - currMs > 1000)
                    {
                        currMs = watch.ElapsedMilliseconds;
                        Console.Write($"Loading {posInsert} of {sampleSize} ({posInsert * 100 / sampleSize}%)...\r");
                    }

                    if (posInsert >= sampleSize)
                    {
                        break;
                    }
                }

                Console.WriteLine($"Loading {posInsert} of {sampleSize} ({posInsert * 100 / sampleSize}%)     ");
                sr.BaseStream.Seek(0L, SeekOrigin.Begin);
                if (posInsert < sampleSize)
                {
                    Array.Resize(ref posRecords, posInsert);
                }

                Console.WriteLine("Shuffling samples...");
                Random.Shared.Shuffle(posRecords);
                return posRecords;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected exception occurred at line {currLine}: {ex.Message}", ex);
            }
        }

        public static PosRecord[] LoadDataFile(StreamReader sr, int sampleSize)
        {
            Stopwatch watch = new();
            Console.WriteLine("Loading data file...");
            PosRecord[] posRecords = new PosRecord[sampleSize];
            int posInsert = 0;
            int currLine = 0;

            try
            {
                string? header = Console.ReadLine();
                watch.Start();
                long currMs = watch.ElapsedMilliseconds;

                string? line;

                while ((line = sr.ReadLine()) != null)
                {
                    string[] fields = line.Split(',');
                    string fen = fields[3];
                    byte hasCastled = byte.Parse(fields[4]);
                    float result = float.Parse(fields[5]);
                    posRecords[posInsert++] = new PosRecord(fen, hasCastled, result);

                    if (watch.ElapsedMilliseconds - currMs > 1000)
                    {
                        currMs = watch.ElapsedMilliseconds;
                        Console.Write($"Loading {posInsert} of {sampleSize} ({posInsert * 100 / sampleSize}%)...\r");
                    }
                }

                Console.WriteLine($"Loading {posInsert} of {sampleSize} ({posInsert * 100 / sampleSize}%)     ");
                sr.BaseStream.Seek(0L, SeekOrigin.Begin);
                if (posInsert < sampleSize)
                {
                    Array.Resize(ref posRecords, posInsert);
                }

                Console.WriteLine("Shuffling data...");
                Random.Shared.Shuffle(posRecords);
                return posRecords;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected exception occurred at line {currLine}: {ex.Message}", ex);
            }
        }

        private static int GetDataSize(StreamReader sr)
        {
            int lineCount = 0;
            while (sr.ReadLine() != null)
            {
                ++lineCount;
            }
            sr.BaseStream.Seek(0L, SeekOrigin.Begin);
            return lineCount - 1; // minus 1 to account for header
        }

        private static int[] GetSampleSelections(ref int sampleSize, StreamReader sr)
        {
            int[] sample;
            int lineCount = GetDataSize(sr);


            if (sampleSize > 0 && sampleSize < lineCount)
            {
                int i = lineCount - 1;
                int[] pop = new int[lineCount];
                while (i >= 0) { pop[i] = i--; }

                sample = new int[sampleSize];

                for (int n = 0; n < sampleSize; ++n)
                {
                    int m = Random.Shared.Next(0, lineCount);
                    sample[n] = pop[m];
                    lineCount--;
                    pop[m] = pop[lineCount];
                }

                Array.Sort(sample);
            }
            else
            {
                int i = 0;
                sampleSize = lineCount;
                sample = new int[sampleSize];
                while (i < lineCount)
                {
                    sample[i] = i++;
                }
            }
            return sample;
        }

        public static int UsableProcessorCount
        {
            get
            {
                int processorCount = Math.Max(Environment.ProcessorCount - 4, 1);
                return processorCount;
            }
        }

        public static List<Memory<PosRecord>> CreateSlices(PosRecord[] records)
        {
            List<Memory<PosRecord>> slices = new();
            int sliceCount = UsableProcessorCount;
            int sliceLength = records.Length / sliceCount;

            slices.EnsureCapacity(sliceCount);
            for (int s = 0; s < sliceCount; s++)
            {
                int start = s * sliceLength;
                Memory<PosRecord> slice = s == sliceCount - 1 ? 
                    new Memory<PosRecord>(records, start, records.Length - start) : 
                    new Memory<PosRecord>(records, start, sliceLength);

                slices.Add(slice);
            }

            return slices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Sigmoid(double k, int qScore)
        {
            return 1.0 / (1.0 + Math.Exp(-k * qScore));
        }

        private static double ErrorSquared(ReadOnlySpan<short> opWeights, ReadOnlySpan<short> egWeights, PosRecord rec, double k)
        {
            int qScore = rec.Features.Compute(opWeights, egWeights);
            qScore = rec.Features.SideToMove == Color.White ? qScore : -qScore;
            double result = rec.Result - Sigmoid(k, qScore);
            return result * result;
        }

        private static double EvalErrorSlice(short[] weights, ReadOnlyMemory<PosRecord> slice, double k, double divisor)
        {
            const int vecSize = EvalFeatures.FEATURE_SIZE;
            var opWeights = new ReadOnlySpan<short>(weights, 0, vecSize);
            var egWeights = new ReadOnlySpan<short>(weights, vecSize, vecSize);

            double result = 0.0;
            for (int n = 0; n < slice.Length; n++)
            {
                result += ErrorSquared(opWeights, egWeights, slice.Span[n], k);
            }

            return divisor * result;
        }

        public static double EvalErrorParallel(short[] weights, List<Memory<PosRecord>> slices, double k)
        {
            int totalLength = slices.Sum(s => s.Length);
            double divisor = 1.0 / totalLength;
            Task<double>[] sliceTasks = new Task<double>[slices.Count];
            for (int n = 0; n < slices.Count; n++)
            {
                Memory<PosRecord> slice = slices[n];
                sliceTasks[n] = Task<double>.Factory.StartNew(() => EvalErrorSlice(weights, slice, k, divisor));
            }

            // ReSharper disable once CoVariantArrayConversion
            Task.WaitAll(sliceTasks);
            double result = sliceTasks.Sum(t => t.Result);
            Array.ForEach(sliceTasks, t => t.Dispose());
            return result;
        }

        public static double MiniEvalErrorParallel(short[] weights, List<Memory<PosRecord>> slices, double k, int batch)
        {
            int totalLength = slices.Sum(s => s.Length);
            int miniBatchSize = totalLength / miniBatchCount;
            if (miniBatchSize < MINI_BATCH_MIN_SIZE)
            {
                miniBatchSize = totalLength;
            }
            if (UsableProcessorCount == 1)
            {
                return EvalErrorParallel(weights, slices, k);
            }

            int sliceLen = totalLength / UsableProcessorCount;
            int batchLen = miniBatchSize / UsableProcessorCount;
            int totalBatches = sliceLen / batchLen;
            int start = (batch % totalBatches) * batchLen;
            int end = start + batchLen;
            double divisor = 1.0 / (batchLen * UsableProcessorCount);
            Task<double>[] tasks = new Task<double>[slices.Count];
            for (int n = 0; n < slices.Count; n++)
            {
                Memory<PosRecord> slice = slices[n];
                tasks[n] = Task<double>.Factory.StartNew(() => EvalErrorSlice(weights, slice[start..end], k, divisor));
            }

            Task.WaitAll(tasks);
            double result = tasks.Sum(t => t.Result);
            Array.ForEach(tasks, t => t.Dispose());
            return result;
        }

        private static double SolveKParallel(short[] weights, List<Memory<PosRecord>> slices, double a = 0.0, double b = 10.0)
        {
            const double gr = 1.6180339887; // golden ratio?
            double k1 = b - (b - a) / gr;
            double k2 = a + (b - a) / gr;

            while (Math.Abs(b - a) > 0.000025)
            {
                double f1 = EvalErrorParallel(weights, slices, k1);
                double f2 = EvalErrorParallel(weights, slices, k2);
                if (f1 < f2)
                {
                    b = k2;
                }
                else
                {
                    a = k1;
                }
                k1 = b - (b - a) / gr;
                k2 = a + (b - a) / gr;
            }

            return (b + a) / 2.0;
        }

        private static void PrintSolution(ChessWeights solution)
        {
            short[] wts = solution.Weights;
            indentLevel = 2;
            WriteLine($"// Solution sample size: {solution.SampleSize}, generated on {DateTime.Now:R}");
            WriteLine($"// Object ID: {solution.Id} - {solution.Description}");
            WriteLine("private static readonly short[] paragonWeights =");
            WriteLine("{");
            indentLevel++;
            PrintSolutionSection(wts, "OPENING WEIGHTS", "opening");
            WriteLine();
            PrintSolutionSection(wts[ChessWeights.ENDGAME_WEIGHTS..], "END GAME WEIGHTS", "end game");
            indentLevel--;            
            WriteLine("};");
        }

        private static void PrintSolutionSection(short[] wts, string sectionTitle, string section)
        {
            string[] pieceNames = { "pawns", "knights", "bishops", "rooks", "queens", "kings" };
            string[] kpNames = { "KK", "KQ", "QK", "QQ" };
            int centerLength = (60 - sectionTitle.Length) / 2;
            string line = new('-', centerLength - 3);
            WriteLine($"/*{line} {sectionTitle} {line}*/");
            WriteLine();
            WriteLine($"/* {section} phase material boundary */");
            WriteLine($"{wts[ChessWeights.GAME_PHASE_MATERIAL]},");
            WriteLine();
            WriteLine($"/* {section} piece values */");
            WriteIndent();
            for (int n = 0; n < 6; n++)
            {
                Console.Write($"{wts[ChessWeights.PIECE_VALUES + n]}, ");
            }
            WriteLine();
            WriteLine();
            WriteLine($"/* {section} piece square values */");
            WriteLine();
            WriteLine($"#region {section} piece square values");
            WriteLine();
            int table = 0;
            int kp = 0;
            for (int n = 0; n < ChessWeights.PIECE_SQUARE_LENGTH; n++)
            {
                if (n % 8 == 0)
                {
                    if (n != 0)
                    {
                        WriteLine();
                    }
                    if (n % 64 == 0)
                    {

                        if (n % 256 == 0) // 4 * 64 squares for each piece
                        {
                            table++;
                            kp = 0;
                        }
                        if (n != 0)
                        {
                            WriteLine();
                        }
                        WriteLine($"/* {pieceNames[table - 1]}: {kpNames[kp++]} */");
                    }
                    WriteIndent();
                }
                Console.Write($"{wts[ChessWeights.PIECE_SQUARE_TABLE + n],4}, ");
            }
            WriteLine();
            WriteLine();
            WriteLine("#endregion");
            WriteLine();
            WriteLine($"/* {section} mobility weights */");
            WriteLine();
            for (int n = 0; n < 4; n++)
            {
                WriteLine($"{wts[ChessWeights.PIECE_MOBILITY + n]}, // {pieceNames[n + 1]}");
            }
            WriteLine();
            WriteLine($"/* {section} squares attacked near enemy king */");
            for (int n = 0; n < 3; n++)
            {
                WriteLine($"{wts[ChessWeights.KING_ATTACK + n]}, // attacks to squares {n + 1} from king");
            }
            WriteLine();
            WriteLine($"/* {section} pawn shield/king safety */");
            for (int n = 0; n < 3; n++)
            {
                WriteLine($"{wts[ChessWeights.PAWN_SHIELD + n]}, // # friendly pawns {n + 1} from king");
            }
            WriteLine();
            WriteLine($"/* {section} isolated pawns */");
            WriteLine($"{wts[ChessWeights.ISOLATED_PAWN]},");
            WriteLine();
            WriteLine($"/* {section} backward pawns */");
            WriteLine($"{wts[ChessWeights.BACKWARD_PAWN]},");
            WriteLine();
            WriteLine($"/* {section} doubled pawns */");
            WriteLine($"{wts[ChessWeights.DOUBLED_PAWN]},");
            WriteLine();
            WriteLine($"/* {section} adjacent/connected pawns */");
            WriteLine($"{wts[ChessWeights.CONNECTED_PAWN]},");
            WriteLine();
            WriteLine($"/* UNUSED (was {section} king adjacent open file) */");
            WriteLine($"{wts[ChessWeights.UNUSED]},");
            WriteLine();
            WriteLine($"/* {section} knight on outpost */");
            WriteLine($"{wts[ChessWeights.KNIGHT_OUTPOST]},");
            WriteLine();
            WriteLine($"/* {section} bishop on outpost */");
            WriteLine($"{wts[ChessWeights.BISHOP_OUTPOST]},");
            WriteLine();
            WriteLine($"/* {section} bishop pair */");
            WriteLine($"{wts[ChessWeights.BISHOP_PAIR]},");
            WriteLine();
            WriteLine($"/* {section} rook on open file */");
            WriteLine($"{wts[ChessWeights.ROOK_ON_OPEN_FILE]},");
            WriteLine();
            WriteLine($"/* {section} rook on half-open file */");
            WriteLine($"{wts[ChessWeights.ROOK_ON_HALF_OPEN_FILE]},");
            WriteLine();
            WriteLine($"/* {section} rook behind passed pawn */");
            WriteLine($"{wts[ChessWeights.ROOK_BEHIND_PASSED_PAWN]},");
            WriteLine();
            WriteLine($"/* {section} doubled rooks on file */");
            WriteLine($"{wts[ChessWeights.DOUBLED_ROOKS_ON_FILE]},");
            WriteLine();
            WriteLine($"/* {section} king on open file */");
            WriteLine($"{wts[ChessWeights.KING_ON_OPEN_FILE]},");
            WriteLine();
            WriteLine($"/* {section} king on half-open file */");
            WriteLine($"{wts[ChessWeights.KING_ON_HALF_OPEN_FILE]},");
            WriteLine();
            WriteLine($"/* {section} castling rights available */");
            WriteLine($"{wts[ChessWeights.CASTLING_AVAILABLE]},");
            WriteLine();
            WriteLine($"/* {section} castling complete */");
            WriteLine($"{wts[ChessWeights.CASTLING_COMPLETE]},");
            WriteLine();
            WriteLine($"/* {section} center control */");
            WriteLine($"{wts[ChessWeights.CENTER_CONTROL]}, // D0");
            WriteLine($"{wts[ChessWeights.CENTER_CONTROL + 1]}, // D1");
            WriteLine();
            WriteLine($"/* {section} queen on open file */");
            WriteLine($"{wts[ChessWeights.QUEEN_ON_OPEN_FILE]},");
            WriteLine();
            WriteLine($"/* {section} queen on half-open file */");
            WriteLine($"{wts[ChessWeights.QUEEN_ON_HALF_OPEN_FILE]},");
            WriteLine();
            WriteLine($"/* {section} rook on seventh rank */");
            WriteLine($"{wts[ChessWeights.ROOK_ON_7TH_RANK]},");
            WriteLine();
            WriteLine($"/* {section} passed pawn */");
            for (int n = 0; n < Constants.MAX_SQUARES; n++)
            {
                if (n % 8 == 0)
                {
                    if (n != 0)
                    {
                        Console.WriteLine();
                    }
                    WriteIndent();
                }
                Console.Write($"{wts[ChessWeights.PASSED_PAWN + n],4}, ");
            }
            Console.WriteLine();
            WriteLine();
            WriteLine($"/* {section} bad bishop pawns */");
            for (int n = 0; n < Constants.MAX_SQUARES; n++)
            {
                if (n % 8 == 0)
                {
                    if (n != 0)
                    {
                        Console.WriteLine();
                    }
                    WriteIndent();
                }
                Console.Write($"{wts[ChessWeights.BAD_BISHOP_PAWN + n],4}, ");
            }
            Console.WriteLine();
            WriteLine();
        }

        private static void WriteIndent()
        {
            string indent = new(' ', indentLevel * 4);
            Console.Write(indent);
        }
        private static void WriteLine(string text = "")
        {
            WriteIndent();
            Console.WriteLine(text);
        }

        private static void RunWeights(string? immortalWt, string? printWt)
        {
            var rep = new ChessDb();
            if (immortalWt != null)
            {
                ChessWeights? wt = rep.Weights.FirstOrDefault(w => w.Id == new Guid(immortalWt));
                if (wt != null)
                {
                    wt.IsActive = true;
                    wt.IsImmortal = true;
                    wt.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Update(wt);
                }

                var otherWts = rep.Weights.Where(w => w.IsActive && w.IsImmortal && w.Id != new Guid(immortalWt));
                foreach (var w in otherWts)
                {
                    w.IsActive = false;
                    w.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Update(w);
                }

                rep.Save();
            }

            if (printWt != null)
            {
                ChessWeights? wt = rep.Weights.FirstOrDefault(w => w.Id == new Guid(printWt));
                if (wt != null)
                {
                    PrintSolution(wt);
                }
            }
        }

        private static int indentLevel = 0;
        private static int miniBatchCount = MINI_BATCH_COUNT;
        private static Encoding savedOsEncoding;
    }
}