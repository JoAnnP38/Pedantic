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
using Pedantic.Genetics;
using Pedantic.Tablebase;
using Pedantic.Tuning;

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
                getDefaultValue: () => 250);
            var saveOption = new Option<bool>(
                name: "--save",
                description: "If specified the sample will be saved in file.",
                getDefaultValue: () => false);
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
            var maxTimeOption = new Option<TimeSpan?>(
                name: "--maxtime",
                description: "Maximum time optimization will run.",
                getDefaultValue: () => null);
            var seedOption = new Option<int?>(
                name: "--seed",
                description: "Specify seed for random number generator.",
                getDefaultValue: () => null);

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
                seedOption
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
            learnCommand.SetHandler(RunLearn, dataFileOption, sampleOption, iterOption, saveOption, resetOption, maxTimeOption, seedOption);
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

        private static void RunLearn(string? dataPath, int sampleSize, int maxPass, bool save, bool reset, TimeSpan? maxTime, int? seed)
        {
            if (dataPath == null)
            {
                throw new ArgumentNullException(nameof(dataPath));
            }

            if (!seed.HasValue)
            {
                seed = (int)DateTime.Now.Ticks;
            }

            using var dataFile = new TrainingDataFile(dataPath, seed);

            IList<PosRecord> positions = sampleSize <= 0 ? dataFile.LoadFile() : dataFile.LoadSample(sampleSize, save);

            short[] weights = Array.Empty<short>();
            if (!reset)
            {
                ChessDb rep = new();
                ChessWeights? startingWeight = rep.Weights
                    .Where(w => w.IsActive && w.IsImmortal)
                    .MinBy(w => w.CreatedOn);

                if (startingWeight != null)
                {
                    weights = startingWeight.Weights;
                }
            }

            var tuner = weights.Length > 0 ? new HceTuner(weights, positions, seed) : new HceTuner(positions, seed);
            var (Error, Accuracy, Weights) = tuner.Train(maxPass, maxTime);
            PrintSolution(positions.Count, Error, Accuracy, Weights, seed.Value);
        }

        private static void PrintSolution(short[] weights)
        {
            indentLevel = 2;
            WriteLine($"// Solution sample size: {weights.Length}, generated on {DateTime.Now:R}");
            WriteLine("private static readonly short[] paragonWeights =");
            WriteLine("{");
            indentLevel++;
            PrintSolutionSection(weights, "OPENING WEIGHTS", "opening");
            WriteLine();
            PrintSolutionSection(weights[ChessWeights.ENDGAME_WEIGHTS..], "END GAME WEIGHTS", "end game");
            indentLevel--;            
            WriteLine("};");
        }

        private static void PrintSolution(int sampleSize, double error, double accuracy, short[] weights, int seed)
        {
            indentLevel = 2;
            WriteLine($"// Solution sample size: {sampleSize}, generated on {DateTime.Now:R}");
            WriteLine($"// Solution error: {error:F6}, accuracy: {accuracy:F4}, seed: {seed}");
            WriteLine("private static readonly short[] paragonWeights =");
            WriteLine("{");
            indentLevel++;
            PrintSolutionSection(weights, "OPENING WEIGHTS", "opening");
            WriteLine();
            PrintSolutionSection(weights[ChessWeights.ENDGAME_WEIGHTS..], "END GAME WEIGHTS", "end game");
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
                WriteLine($"{wts[ChessWeights.PIECE_MOBILITY + n],4}, // {pieceNames[n + 1]}");
            }
            WriteLine();
            WriteLine($"/* {section} squares attacked near enemy king */");
            for (int n = 0; n < 3; n++)
            {
                WriteLine($"{wts[ChessWeights.KING_ATTACK + n],4}, // attacks to squares {n + 1} from king");
            }
            WriteLine();
            WriteLine($"/* {section} pawn shield/king safety */");
            for (int n = 0; n < 3; n++)
            {
                WriteLine($"{wts[ChessWeights.PAWN_SHIELD + n],4}, // # friendly pawns {n + 1} from king");
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
                Console.Write($"{wts[ChessWeights.CONNECTED_PAWN + n],4}, ");
            }
            Console.WriteLine();
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
            WriteLine($"{wts[ChessWeights.CENTER_CONTROL],4}, // D0");
            WriteLine($"{wts[ChessWeights.CENTER_CONTROL + 1],4}, // D1");
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
            WriteLine($"/* {section} block passed pawn */");
            for (int n = 0; n < Constants.MAX_COORDS * Constants.MAX_PIECES; n++)
            {
                if (n % 8 == 0)
                {
                    if (n != 0)
                    {
                        Console.WriteLine($" // blocked by {pieceNames[(n / 8) - 1]}");
                    }
                    WriteIndent();
                }
                Console.Write($"{wts[ChessWeights.BLOCK_PASSED_PAWN + n],4}, ");
            }
            Console.WriteLine(" // blocked by kings");
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
                    PrintSolution(wt.Weights);
                }
            }
        }

        private static int indentLevel = 0;
    }
}