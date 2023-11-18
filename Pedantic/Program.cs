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
using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.CommandLine.Parsing;

using Pedantic.Chess;
using Pedantic.Tablebase;
using Pedantic.Tuning;
using Pedantic.Genetics;

using Score = Pedantic.Chess.Score;

// ReSharper disable LocalizableElement

namespace Pedantic
{
    public static class Program
    {
        public const string APP_NAME = Constants.APP_NAME;
        public const string APP_VERSION = Constants.APP_VERSION;
        public const string APP_NAME_VER = APP_NAME + " " + APP_VERSION;
        public const string AUTHOR = Constants.APP_AUTHOR;
        public const string PROGRAM_URL = "https://github.com/JoAnnP38/Pedantic";
        public const double MINI_CONVERGENCE_TOLERANCE = 0.00000005;
        public const double FULL_CONVERGENCE_TOLERANCE = 0.0000001;
        public const int MAX_CONVERGENCE_FAILURE = 2;
        public const int MINI_BATCH_COUNT = 80;
        public const int MINI_BATCH_MIN_SIZE = 10000;
        public const int MINI_BATCH_MIN_COUNT = 5;

        private enum PerftRunType
        {
            Normal,
            Average,
            Details,
            Divide
        }

        private static int Main(string[] args)
        {
            Console.WriteLine($"{APP_NAME_VER} by {AUTHOR}");
            Console.Write("Fast PEXT available: ");
            if (Board.IsPextSupported)
            {
                Console.WriteLine("Yes");
            }
            else
            {
                Console.WriteLine("No");
            }
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
                getDefaultValue: () => int.MaxValue);
            var sampleOption = new Option<int>(
                name: "--sample",
                description: "Specify the number of samples to use from learning data.",
                getDefaultValue: () => -1);
            var iterOption = new Option<int>(
                name: "--iter",
                description: "Specify the maximum number of iterations before a solution is declared.",
                getDefaultValue: () => 5000);
            var saveOption = new Option<bool>(
                name: "--save",
                description: "If specified the sample will be saved in file.",
                getDefaultValue: () => false);
            var resetOption = new Option<bool>(
                name: "--reset",
                description: "Reset most starting weights to zero before learning begins.",
                getDefaultValue: () => false);
            var statsOption = new Option<bool>(
                name: "--stats",
                description: "Collect search statistics",
                getDefaultValue: () => false);
            var magicOption = new Option<bool>(
                name: "--force_magic",
                description: "Force the use of magic bitboards.",
                getDefaultValue: () => false);
            var maxTimeOption = new Option<TimeSpan?>(
                name: "--maxtime",
                description: "Maximum duration the optimization will run before a solution is declared.",
                getDefaultValue: () => null);
            var errorOption = new Option<double>(
                name: "--error",
                description: "Error threshold for terminating optimization loop.",
                getDefaultValue: () => 0.0);

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
                saveOption,
                resetOption,
                maxTimeOption,
                errorOption
            };

            var weightsCommand = new Command("weights", "Display the default weights used by evaluation.");

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
            learnCommand.SetHandler(RunLearn, dataFileOption, sampleOption, iterOption, saveOption, resetOption, maxTimeOption, errorOption);
            weightsCommand.SetHandler(RunWeights);
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
                Uci.Default.Log(@$"Fatal error occurred in Pedantic: '{e.Message}'.");
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
                    Console.WriteLine($@"option name Hash type spin default {TtTran.DEFAULT_SIZE_MB} min 16 max {TtTran.MAX_SIZE_MB}");
                    Console.WriteLine($@"option name Threads type spin default 1 min 1 max {Math.Max(Environment.ProcessorCount, 1)}");
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
                    Engine.LoadWeights();
                    break;

                case "isready":
                    Console.WriteLine("readyok");
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

                case "bench":
                    Bench(tokens);
                    break;

                default:
                    Uci.Default.Log($@"Unexpected input: '{input}'");
                    return;
            }
        }

        private static void Bench(string[] tokens)
        {
            TryParse(tokens, "depth", out int maxDepth, Constants.MAX_PLY);
            if (maxDepth < Constants.MAX_PLY)
            {
                Engine.Bench(maxDepth);
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
                Uci.Default.Log("'position' parameters missing or not understood. Assuming 'startpos'.");
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
                            sizeMb = Math.Clamp(sizeMb, 16, 2048);
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

                    case "Threads":
                        if (tokens[3] == "value" && int.TryParse(tokens[4], out int searchThreads))
                        {
                            searchThreads = Math.Clamp(searchThreads, 1, Environment.ProcessorCount);
                            Engine.SearchThreads = searchThreads;
                            UciOptions.Threads = Engine.SearchThreads;
                        }
                        break;

                    case "Ponder":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool canPonder))
                        {
                            UciOptions.Ponder = canPonder;
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
                                    Uci.Default.Log($"Ignoring specified SyzygyPath: '{path}'. Path doesn't exist.");
                                }
                                else
                                {
                                    bool result = Syzygy.Initialize(path);
                                    if (!result)
                                    {
                                        Uci.Default.Log($"Could not locate valid Syzygy tablebase files at '{path}'.");
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
            TryParse(tokens, "depth", out int maxDepth, Constants.MAX_PLY - 1);
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
                long total = 0, wins = 0, draws = 0, losses = 0;
                PgnPositionReader posReader = new();

                long count = 0;
                HashSet<ulong> hashes = new();
                Console.WriteLine(@"Hash,Ply,GamePly,FEN,HasCastled,Eval,Result");
                foreach (var p in posReader.Positions(Console.In))
                {
                    if (!hashes.Contains(p.Hash))
                    {
                        if (p.Result == PosRecord.WDL_WIN)
                        {
                            wins++;
                        }
                        else if (p.Result == PosRecord.WDL_LOSS)
                        {
                            losses++;
                        }
                        else if (draws + 1 <= Math.Max(wins, losses) || Random.Shared.NextDouble() > 0.75)
                        {
                            draws++;
                        }
                        else
                        {
                            // skip - don't let draw percentage greatly exceed 33.3%
                            continue;
                        }
                        Console.Error.Write($"{++count}\r");
                        hashes.Add(p.Hash);
                        Console.WriteLine($@"{p.Hash:X16},{p.Ply},{p.GamePly},{p.Fen},{p.HasCastled},{p.Eval},{p.Result:F1}");
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

        private static void RunLearn(string? dataPath, int sampleSize, int maxPass, bool save, bool reset, TimeSpan? maxTime, double minError)
        {
            if (dataPath == null)
            {
                throw new ArgumentNullException(nameof(dataPath));
            }

            using var dataFile = new TrainingDataFile(dataPath);

            IList<PosRecord> positions = sampleSize <= 0 ? dataFile.LoadFile() : dataFile.LoadSample(sampleSize, save);

            var tuner = reset ? new GdTuner(positions) : new GdTuner(Engine.Weights, positions);
            var (Error, Accuracy, Weights, K) = tuner.Train(maxPass, maxTime, minError);
            PrintSolution(positions.Count, Error, Accuracy, Weights, K);
        }

        private static void PrintSolution(HceWeights weights)
        {
            indentLevel = 2;
            WriteLine($"// Solution sample size: {weights.Length}, generated on {DateTime.Now:R}");
            WriteLine("private static readonly Score[] defaultWeights =");
            WriteLine("{");
            indentLevel++;
            PrintSolutionSection(weights);
            indentLevel--;            
            WriteLine("};");
        }

        private static void PrintSolution(int sampleSize, double error, double accuracy, HceWeights weights, double K)
        {
            indentLevel = 2;
            WriteLine($"// Solution sample size: {sampleSize}, generated on {DateTime.Now:R}");
            WriteLine($"// Solution K: {K:F6}, error: {error:F6}, accuracy: {accuracy:F4}");
            WriteLine("private static readonly Score[] defaultWeights =");
            WriteLine("{");
            indentLevel++;
            PrintSolutionSection(weights);
            indentLevel--;            
            WriteLine("};");
        }

        private static void PrintSolutionSection(HceWeights wts)
        {
            void WriteWt(Score s)
            {
                string score = $"S({s.MgScore,3}, {s.EgScore,3}), ";
                Console.Write($"{score,-15}");
            }

            void WriteWtLine(Score s)
            {
                WriteIndent();
                WriteWt(s);
                Console.WriteLine();
            }

            void WriteWtsLine(Score[] scores, int start, int length)
            {
                WriteIndent();
                for (int n = start; n < start + length; n++)
                {
                    WriteWt(scores[n]);
                }
                Console.WriteLine();
            }

            void WriteWts2D(Score[] scores, int start, int width, int length)
            {
                for (int n = 0; n < length; n++)
                {
                    if (n % width == 0)
                    {
                        if (n != 0)
                        {
                            Console.WriteLine();
                        }
                        WriteIndent();
                    }
                    WriteWt(scores[start + n]);
                }
                Console.WriteLine();
            }

            string[] pieceNames = { "pawns", "knights", "bishops", "rooks", "queens", "kings" };
            string[] upperNames = { "Pawn", "Knight", "Bishop", "Rook", "Queen", "King" };
            string[] kpNames = { "KK", "KQ", "QK", "QQ" };
            WriteLine("/* piece values */");
            WriteWtsLine(wts.Weights, HceWeights.PIECE_VALUES, Constants.MAX_PIECES);
            WriteLine();
            WriteLine("/* piece square values */");
            WriteLine("#region piece square values");
            WriteLine();
            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                for (int kp = 0; kp < Constants.MAX_KING_PLACEMENTS; kp++)
                {
                    int index = (pc * Constants.MAX_KING_PLACEMENTS + kp) * Constants.MAX_SQUARES;
                    WriteLine($"/* {pieceNames[pc]}: {kpNames[kp]} */");
                    WriteWts2D(wts.Weights, HceWeights.PIECE_SQUARE_TABLE + index, 8, Constants.MAX_SQUARES);
                    WriteLine();
                }
            }
            WriteLine("#endregion");
            WriteLine();
            WriteLine("/* mobility weights */");
            WriteIndent(); WriteWt(wts[HceWeights.PIECE_MOBILITY + 0]); Console.WriteLine(" // knights");
            WriteIndent(); WriteWt(wts[HceWeights.PIECE_MOBILITY + 1]); Console.WriteLine(" // bishops");
            WriteIndent(); WriteWt(wts[HceWeights.PIECE_MOBILITY + 2]); Console.WriteLine(" // rooks");
            WriteIndent(); WriteWt(wts[HceWeights.PIECE_MOBILITY + 3]); Console.WriteLine(" // queens");
            WriteLine();
            WriteLine("/* center control */");
            WriteIndent(); WriteWt(wts[HceWeights.CENTER_CONTROL + 0]); Console.WriteLine(" // D0");
            WriteIndent(); WriteWt(wts[HceWeights.CENTER_CONTROL + 1]); Console.WriteLine(" // D1");
            WriteLine();
            WriteLine("/* squares attacked near enemy king */");
            WriteIndent(); WriteWt(wts[HceWeights.KING_ATTACK + 0]); Console.WriteLine(" // attacks to squares 1 from king");
            WriteIndent(); WriteWt(wts[HceWeights.KING_ATTACK + 1]); Console.WriteLine(" // attacks to squares 2 from king");
            WriteIndent(); WriteWt(wts[HceWeights.KING_ATTACK + 2]); Console.WriteLine(" // attacks to squares 3 from king");
            WriteLine();
            WriteLine("/* pawn shield/king safety */");
            WriteIndent(); WriteWt(wts[HceWeights.PAWN_SHIELD + 0]); Console.WriteLine(" // friendly pawns 1 from king");
            WriteIndent(); WriteWt(wts[HceWeights.PAWN_SHIELD + 1]); Console.WriteLine(" // friendly pawns 2 from king");
            WriteIndent(); WriteWt(wts[HceWeights.PAWN_SHIELD + 2]); Console.WriteLine(" // friendly pawns 3 from king");
            WriteLine();
            WriteLine("/* castling right available */");
            WriteWtLine(wts[HceWeights.CASTLING_AVAILABLE]);
            WriteLine();
            WriteLine("/* castling complete */");
            WriteWtLine(wts[HceWeights.CASTLING_COMPLETE]);
            WriteLine();
            WriteLine("/* king on open file */");
            WriteWtLine(wts[HceWeights.KING_ON_OPEN_FILE]);
            WriteLine();
            WriteLine("/* king on half-open file */");
            WriteWtLine(wts[HceWeights.KING_ON_HALF_OPEN_FILE]);
            WriteLine();
            WriteLine("/* king on open diagonal */");
            WriteWtLine(wts[HceWeights.KING_ON_OPEN_DIAGONAL]);
            WriteLine();
            WriteLine("/* king attack square open */");
            WriteWtLine(wts[HceWeights.KING_ATTACK_SQUARE_OPEN]);
            WriteLine();
            WriteLine("/* isolated pawns */");
            WriteWtLine(wts[HceWeights.ISOLATED_PAWN]);
            WriteLine();
            WriteLine("/* doubled pawns */");
            WriteWtLine(wts[HceWeights.DOUBLED_PAWN]);
            WriteLine();
            WriteLine("/* backward pawns */");
            WriteWtLine(wts[HceWeights.BACKWARD_PAWN]);
            WriteLine();
            WriteLine("/* adjacent/phalanx pawns */");
            WriteWts2D(wts.Weights, HceWeights.PHALANX_PAWN, 8, Constants.MAX_SQUARES);
            WriteLine();
            WriteLine("/* passed pawn */");
            WriteWts2D(wts.Weights, HceWeights.PASSED_PAWN, 8, Constants.MAX_SQUARES);
            WriteLine();
            WriteLine("/* pawn rams */");
            WriteWts2D(wts.Weights, HceWeights.PAWN_RAM, 8, Constants.MAX_SQUARES);
            WriteLine();
            WriteLine("/* supported pawn chain */");
            WriteWts2D(wts.Weights, HceWeights.CHAINED_PAWN, 8, Constants.MAX_SQUARES);
            WriteLine();
            WriteLine("/* passed pawn can advance */");
            WriteWtsLine(wts.Weights, HceWeights.PP_CAN_ADVANCE, 4);
            WriteLine();
            WriteLine("/* enemy king outside passed pawn square */");
            WriteWtLine(wts[HceWeights.KING_OUTSIDE_PP_SQUARE]);
            WriteLine();
            WriteLine("/* passed pawn/friendly king distance penalty */");
            WriteWtLine(wts[HceWeights.PP_FRIENDLY_KING_DISTANCE]);
            WriteLine();
            WriteLine("/* passed pawn/enemy king distance bonus */");
            WriteWtLine(wts[HceWeights.PP_ENEMY_KING_DISTANCE]);
            WriteLine();
            WriteLine("/* blocked passed pawn */");
            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                WriteIndent();
                for (int n = 0; n < 8; n++)
                {
                    WriteWt(wts[HceWeights.BLOCK_PASSED_PAWN + pc * 8 + n]);
                }
                Console.WriteLine($" // blocked by {pieceNames[pc]}");
            }
            WriteLine();
            WriteLine("/* rook behind passed pawn */");
            WriteWtLine(wts[HceWeights.ROOK_BEHIND_PASSED_PAWN]);
            WriteLine();
            WriteLine("/* knight on outpost */");
            WriteWtLine(wts[HceWeights.KNIGHT_OUTPOST]);
            WriteLine();
            WriteLine("/* bishop on outpost */");
            WriteWtLine(wts[HceWeights.BISHOP_OUTPOST]);
            WriteLine();
            WriteLine("/* bishop pair */");
            WriteWtLine(wts[HceWeights.BISHOP_PAIR]);
            WriteLine();
            WriteLine("/* bad bishop pawns */");
            WriteWts2D(wts.Weights, HceWeights.BAD_BISHOP_PAWN, 8, Constants.MAX_SQUARES);
            WriteLine();
            WriteLine("/* rook on open file */");
            WriteWtLine(wts[HceWeights.ROOK_ON_OPEN_FILE]);
            WriteLine();
            WriteLine("/* rook on half-open file */");
            WriteWtLine(wts[HceWeights.ROOK_ON_HALF_OPEN_FILE]);
            WriteLine();
            WriteLine("/* rook on seventh rank */");
            WriteWtLine(wts[HceWeights.ROOK_ON_7TH_RANK]);
            WriteLine();
            WriteLine("/* doubled rooks on file */");
            WriteWtLine(wts[HceWeights.DOUBLED_ROOKS_ON_FILE]);
            WriteLine();
            WriteLine("/* queen on open file */");
            WriteWtLine(wts[HceWeights.QUEEN_ON_OPEN_FILE]);
            WriteLine();
            WriteLine("/* queen on half-open file */");
            WriteWtLine(wts[HceWeights.QUEEN_ON_HALF_OPEN_FILE]);
            WriteLine();
            WriteLine("/* pawn push threats */");
            WriteIndent();
            for (int n = 0; n < Constants.MAX_PIECES; n++)
            {
                WriteWt(wts[HceWeights.PAWN_PUSH_THREAT + n]);
            }
            Console.WriteLine(" // Pawn push threats");
            WriteLine();
            WriteLine("/* piece threats */");
            WriteLine("/*  Pawn          Knight         Bishop          Rook          Queen           King */");
            for (int pc1 = 0; pc1 < Constants.MAX_PIECES; pc1++)
            {
                WriteIndent();
                for (int pc2 = 0; pc2 < Constants.MAX_PIECES; pc2++)
                {
                    int index = HceWeights.PIECE_THREAT + pc1 * Constants.MAX_PIECES + pc2;
                    WriteWt(wts[index]);
                }
                Console.WriteLine($" // {upperNames[pc1]} threats");
            }
            WriteLine();
            WriteLine("/* tempo bonus for side to move */");
            WriteWtLine(wts[HceWeights.TEMPO_BONUS]);
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

        private static void RunWeights()
        {
            HceWeights weights = Engine.Weights;
            PrintSolution(weights);
        }

        private static int indentLevel = 0;
    }
}