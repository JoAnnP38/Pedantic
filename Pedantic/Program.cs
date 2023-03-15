using Pedantic.Chess;
using System.Diagnostics;
using System.Runtime;
using System.CommandLine;
using System.Text;
using System.Formats.Tar;
using System.Runtime.CompilerServices;
using LiteDB;
using Pedantic.Genetics;
using Pedantic.Utilities;
// ReSharper disable LocalizableElement


namespace Pedantic
{
    public class Program
    {
        public const string PROGRAM_NAME_VER = "Pedantic v0.0.1";
        public const string AUTHOR = "JoAnn D. Peeler";
        public const string PROGRAM_URL = "https://github.com/JoAnnP38/Pedantic";
        public const double CONVERGENCE_TOLERANCE = 0.0000005;

        enum PerftRunType
        {
            Normal,
            Average,
            Details
        }
        static int Main(string[] args)
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
            var searchTypeOption = new Option<SearchType>(
                name: "--search",
                description: "Specifies the search type to use.",
                getDefaultValue: () => SearchType.Pv);
            var errorFileOption = new Option<string?>(
                name: "--error",
                description: "Output errors to specified file.",
                getDefaultValue: () => null);
            var pgnFileOption = new Option<string?>(
                name: "--pgn",
                description: "Specifies a PGN input file.",
                getDefaultValue: () => null);
            var dataFileOption = new Option<string?>(
                name: "--data",
                description: "The name of the labeled data output file.",
                getDefaultValue: () => null);
            var sampleOption = new Option<int>(
                name: "--sample",
                description: "Specify the number of samples to use from learning data.",
                getDefaultValue: () => 10000);
            var iterOption = new Option<int>(
                name: "--iter",
                description: "Specify the maximum number of iterations before a solution is declared.",
                getDefaultValue: () => 200);
            var preserveOption = new Option<bool>(
                name: "--preserve",
                description: "When present intermediary versions of the solution will be saved.",
                getDefaultValue: () => false);
            var immortalOption = new Option<string?>(
                name: "--immortal",
                description: "Designate a new immortal set of weights.",
                getDefaultValue: () => null);
            var displayOption = new Option<string?>(
                name: "--display",
                description: "Display the specific set of weights as C# code.",
                getDefaultValue: () => null);

            var uciCommand = new Command("uci", "Start the pedantic application in UCI mode.")
            {
                searchTypeOption,
                commandFileOption,
                errorFileOption
            };

            var perftCommand = new Command("perft", "Run a standard Perft test.")
            {
                typeOption,
                depthOption,
                fenOption
            };

            var labelCommand = new Command("label", "Pre-process and label PGN data.")
            {
                pgnFileOption,
                dataFileOption
            };

            var learnCommand = new Command("learn", "Optimize evaluation function using training data.")
            {
                dataFileOption,
                sampleOption,
                iterOption,
                preserveOption
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

            uciCommand.SetHandler(async (searchType, inFile, errFile) => await RunUci(searchType, inFile, errFile), searchTypeOption, commandFileOption, errorFileOption);
            perftCommand.SetHandler(RunPerft, typeOption, depthOption, fenOption);
            labelCommand.SetHandler(RunLabel, pgnFileOption, dataFileOption);
            learnCommand.SetHandler(RunLearn, dataFileOption, sampleOption, iterOption, preserveOption);
            weightsCommand.SetHandler(RunWeights, immortalOption, displayOption);

            return rootCommand.InvokeAsync(args).Result;
        }

        private static async Task RunUci(SearchType searchType, string? inFile, string? errFile)
        {
            TextReader? stdin = null;
            TextWriter? stderr = null;

            if (inFile != null && File.Exists(inFile))
            {
                stdin = Console.In;
                StreamReader inStream = new StreamReader(inFile, Encoding.UTF8);
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
                Console.WriteLine(PROGRAM_NAME_VER);
                Engine.SearchType = searchType;
                Engine.Start();
                while (Engine.IsRunning)
                {
                    string? input = await Task.Run(Console.ReadLine);
                    if (input != null)
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
                    Console.WriteLine($@"id name {PROGRAM_NAME_VER}");
                    Console.WriteLine($@"id author {AUTHOR}");
                    Console.WriteLine(@"option name OwnBook type check default true");
                    Console.WriteLine(@"option name Ponder type check default true");
                    Console.WriteLine($@"option name Hash type spin default {TtTran.DEFAULT_SIZE_MB} min 1 max {TtTran.MAX_SIZE_MB}");
                    Console.WriteLine(@"option name Clear Hash type button");
                    Console.WriteLine($@"option name MaxThreads type spin default 1 min 1 max {Math.Max(Environment.ProcessorCount - 2, 1)}");
                    Console.WriteLine($@"option name UCI_EngineAbout type string default {PROGRAM_NAME_VER} by {AUTHOR}, see {PROGRAM_URL}");
                    Console.WriteLine(@"option name Search_Algorithm type combo default PV var PV var MTD(f) var Minimal");
                    Console.WriteLine(@"option name Evaluation_ID type string default <empty>");
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
                    SetOption(tokens);
                    break;

                case "ucinewgame":
                    Engine.ClearHashTable();
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

        static void SetupPosition(string[] tokens)
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

        static void SetOption(string[] tokens)
        {
            if (tokens[1] == "name")
            {
                switch (tokens[2])
                {
                    case "Hash":
                        if (tokens[3] == "value" && int.TryParse(tokens[4], out int sizeMb))
                        {
                            Engine.ResizeHashTable(sizeMb);
                        }
                        break;

                    case "OwnBook":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool useOwnBook))
                        {
                            Engine.UseOwnBook = useOwnBook;
                        }
                        break;

                    case "Clear":
                        if (tokens[3] == "Hash")
                        {
                            Engine.ClearHashTable();
                        }
                        break;

                    case "MaxThreads":
                        if (tokens[3] == "value" && int.TryParse(tokens[4], out int searchThreads))
                        {
                            Engine.SearchThreads = searchThreads;
                        }
                        break;

                    case "Ponder":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool canPonder))
                        {
                            Engine.CanPonder = canPonder;
                        }

                        break;

                    case "Search_Algorithm":
                        if (tokens[3] == "value" && TryParse(tokens[4], out SearchType searchType))
                        {
                            Engine.SearchType = searchType;
                        }

                        break;
                    case "Evaluation_ID":
                        if (tokens[3] == "value")
                        {
                            Engine.EvaluationId = tokens.Length >= 5 ? tokens[4] : string.Empty;
                        }

                        break;
                }
            }
        }

        static void Go(string[] tokens)
        {
            TryParse(tokens, "depth", out int maxDepth, Constants.MAX_PLY);
            TryParse(tokens, "movetime", out int maxTime, int.MaxValue);
            TryParse(tokens, "nodes", out long maxNodes, long.MaxValue);
            TryParse(tokens, "movestogo", out int movesToGo, 40);
            bool ponder = Array.Exists(tokens, item => item.Equals("ponder"));

            if (Engine.SideToMove == Color.White && TryParse(tokens, "wtime", out int whiteTime))
            {
                TryParse(tokens, "winc", out int whiteIncrement);
                Engine.Go(whiteTime, whiteIncrement, movesToGo, maxDepth, maxNodes, ponder);
            }
            else if (Engine.SideToMove == Color.Black && TryParse(tokens, "btime", out int blackTime))
            {
                TryParse(tokens, "binc", out int blackIncrement);
                Engine.Go(blackTime, blackIncrement, movesToGo, maxDepth, maxNodes, ponder);
            }
            else
            {
                Engine.Go(maxDepth, maxTime, maxNodes, ponder);
            }
        }

        static void Debug(string[] tokens)
        {
            Engine.Debug = tokens[1] == "on";
        }

        static bool TryParse(string s, out SearchType searchType)
        {
            searchType = SearchType.Pv;
            s = s.ToLower();
            if (s == "pv")
            {
                searchType = SearchType.Pv;
                return true;
            }
            if (s == "mtd(f)")
            {
                searchType = SearchType.Mtd;
                return true;
            }
            if (s == "minimal")
            {
                searchType = SearchType.Minimal;
                return true;
            }

            return false;
        }

        static bool TryParse(string[] tokens, string name, out int value, int defaultValue = 0)
        {
            if (int.TryParse(Token(tokens, name), out value))
                return true;
            value = defaultValue;
            return false;
        }

        static bool TryParse(string[] tokens, string name, out long value, long defaultValue = 0)
        {
            if (long.TryParse(Token(tokens, name), out value))
                return true;
            value = defaultValue;
            return false;
        }

        static string? Token(string[] tokens, string name)
        {
            int iParam = Array.IndexOf(tokens, name);
            if (iParam < 0) return null;

            int iValue = iParam + 1;
            return (iValue < tokens.Length) ? tokens[iValue] : null;
        }

        static void RunPerft(PerftRunType runType, int depth, string? fen = null)
        {
            Console.WriteLine($@"{PROGRAM_NAME_VER}");
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
            }
        }

        static void RunNormalPerft(Perft perft, int totalDepth)
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

        static void RunAveragePerft(Perft perft, int totalDepth)
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

        static void RunDetailedPerft(Perft perft, int totalDepth)
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

        private static void RunLabel(string? pgnFile, string? dataFile)
        {
            TextReader? stdin = null;
            TextWriter? stdout = null;

            if (pgnFile != null && File.Exists(pgnFile))
            {
                stdin = Console.In;
                StreamReader inStream = new StreamReader(pgnFile, Encoding.UTF8);
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
                Console.WriteLine(@"Hash,Ply,GamePly,FEN,Score");
                foreach (var p in posReader.Positions(Console.In))
                {
                    if (!hashes.Contains(p.Hash))
                    {
                        Console.Error.Write($"{++count}\r");
                        hashes.Add(p.Hash);
                        Console.WriteLine($@"{p.Hash:X16},{p.Ply},{p.GamePly},{p.Fen},{p.Score:F1}");
                        Console.Out.Flush();
                        if (++total >= 8000000)
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

        private static void RunLearn(string? dataFile, int sampleSize = 10000, int maxPass = 200, bool preserve = false)
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

            if (sampleSize <= 0)
            {
                Console.Error.WriteLine("ERROR: Sample size must be greater than zero.");
                return;
            }

            if (maxPass <= 0)
            {
                Console.Error.WriteLine("ERROR: Iterations must be greater than zero.");
                return;
            }

            DateTime start = DateTime.Now;
            TextReader savedIn = Console.In;
            StreamReader streamReader = new StreamReader(dataFile, Encoding.UTF8);
            Console.SetIn(streamReader);

            try
            {
                Console.WriteLine($@"Sample size: {sampleSize}, Start time: {DateTime.Now:G}");
                var slices = CreateSlices(LoadSample(sampleSize, streamReader));
                using GeneticsRepository rep = new();
                ChessWeights? startingWeight = rep.Weights
                    .Find(w => w.IsActive && w.IsImmortal)
                    .MinBy(w => w.CreatedOn);
                if (startingWeight != null)
                {
                    short[] weights = EvalFeatures.GetCombinedWeights(startingWeight);
                    double k = SolveKParallel(weights, slices);
                    Console.WriteLine($@"K = {k:F2}");
                    startingWeight.Fitness = EvalErrorParallel(weights, slices, k);
                    ChessWeights guess = new ChessWeights(startingWeight)
                    {
                        IsImmortal = false
                    };
                    Console.WriteLine($@"Pass 0, K={k:F2}, Samples={sampleSize}");

                    ChessWeights optimized = LocalOptimize(guess, weights, slices, k, maxPass,
                        preserve);
                    optimized.Description = "Optimized";
                    optimized.UpdatedOn = DateTime.UtcNow;
                    optimized.ParentIds = Array.Empty<ObjectId>();
                    rep.Weights.Insert(optimized);
                    DateTime end = DateTime.Now;
                    Console.WriteLine($@"Optimization complete at: {DateTime.UtcNow:G}, Elapsed: {end - start:g}");
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

        private static ChessWeights LocalOptimize(ChessWeights guess, short[] weights, List<PosRecord[]> slices, double k, int maxPass, bool preserve = false)
        {
            bool improved = true;
            double curError = guess.Fitness;
            double bestError = curError + CONVERGENCE_TOLERANCE * 2;
            int wtLen = weights.Length;
            int passes = 0;

            Console.WriteLine($@"Optimization pass {passes,3}: {curError:F6}");
            using var rep = new GeneticsRepository();
            int[] index = new int[wtLen];
            for (int n = 0; n < wtLen; n++)
            {
                index[n] = n;
            }

            while (improved && curError + CONVERGENCE_TOLERANCE < bestError && passes < maxPass)
            {
                improved = false;
                bestError = curError;
                Random.Shared.Shuffle(index);

                for (int n = 0; n < wtLen; n++)
                {
                    int i = index[n];
                    short increment = EvalFeatures.GetOptimizationIncrement(i);
                    short oldValue = weights[i];
                    weights[i] += increment;
                    double error = EvalErrorParallel(weights, slices, k);
                    bool goodIncrement = error < curError;
                    improved = improved || goodIncrement;

                    if (!goodIncrement)
                    {
                        weights[i] -= (short)(increment * 2);
                        error = EvalErrorParallel(weights, slices, k);
                        goodIncrement = error < curError;
                        improved = improved || goodIncrement;

                        if (!goodIncrement)
                        {
                            weights[i] = oldValue;
                        }
                    }

                    if (goodIncrement)
                    {
                        curError = error;
                    }
                }

                ++passes;
                if (preserve && passes % 10 == 0 && improved)
                {
                    EvalFeatures.UpdateCombinedWeights(guess, weights);
                    ChessWeights intermediate = new(guess)
                    {
                        Description = $"Pass {passes}",
                        Fitness = curError
                    };
                    rep.Weights.Insert(intermediate);
                    Console.WriteLine($@"Optimization pass {passes,3}: {curError:F6}, OID: {intermediate.Id}");
                }
                else
                {
                    Console.WriteLine($@"Optimization pass {passes,3}: {curError:F6}");
                }
            }

            EvalFeatures.UpdateCombinedWeights(guess, weights);
            guess.Description = "Optimized";
            return guess;
        }

        public static PosRecord[] LoadSample(int sampleSize, StreamReader sr)
        {
            SortedSet<int> selections = GetSampleSelections(sampleSize, sr);
            List<PosRecord> posRecordList = new List<PosRecord>(sampleSize);

            int currLine = 0;
            foreach (int selLine in selections)
            {
                while (currLine++ < selLine)
                {
                    if (Console.ReadLine() == null)
                    {
                        sr.BaseStream.Seek(0L, SeekOrigin.Begin);
                        return posRecordList.ToArray();
                    }
                }

                string? str = Console.ReadLine();
                if (str == null)
                {
                    sr.BaseStream.Seek(0L, SeekOrigin.Begin);
                    return posRecordList.ToArray();
                }

                string[] fields = str.Split(',');
                ulong hash = Convert.ToUInt64(fields[0], 16);
                short ply = short.Parse(fields[1]);
                short gamePly = short.Parse(fields[2]);
                short eval = short.Parse(fields[3]);
                string fen = fields[4];
                float result = float.Parse(fields[5]);
                posRecordList.Add(new PosRecord(hash, ply, gamePly, eval, fen, result));
            }

            sr.BaseStream.Seek(0L, SeekOrigin.Begin);
            return posRecordList.ToArray();
        }

        private static SortedSet<int> GetSampleSelections(int sampleSize, StreamReader sr)
        {
            int lineCount = 0;
            while (Console.ReadLine() != null)
            {
                ++lineCount;
            }

            sr.BaseStream.Seek(0L, SeekOrigin.Begin);
            SortedSet<int> sampleSelection = new SortedSet<int>();
            for (int n = 0; n < sampleSize; ++n)
            {
                int num;
                do
                {
                    num = Random.Shared.Next(1, lineCount + 1);
                } 
                while (sampleSelection.Contains(num));

                sampleSelection.Add(num);
            }

            return sampleSelection;
        }

        public static List<PosRecord[]> CreateSlices(PosRecord[] records)
        {
            List<PosRecord[]> slices = new List<PosRecord[]>();
#if DEBUG
            int sliceCount = 1;
            int sliceLength = records.Length;
#else
            int sliceCount = Math.Max(Environment.ProcessorCount - 2, 1);
            int sliceLength = records.Length / sliceCount;
#endif
            for (int s = 0; s < sliceCount; s++)
            {
                int start = s * sliceLength;
                int end = start + sliceLength;
                PosRecord[] slice = s == sliceCount - 1 ? records[start..] : records[start..end];
                slices.Add(slice);
            }

            return slices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Sigmoid(double k, int qScore)
        {
            return 1.0 / (1.0 + Math.Pow(10.0, -k * qScore / 400.0));
        }

        private static double ErrorSquared(ReadOnlySpan<short> opWeights, ReadOnlySpan<short> egWeights, PosRecord rec, double k, double divisor)
        {
            int qScore = rec.Features.Compute(opWeights, egWeights);
            qScore = rec.Features.SideToMove == Color.White ? qScore : -qScore;
            double result = rec.Result - Sigmoid(k, qScore);
            return divisor * result * result;
        }

        private static double EvalErrorSlice(short[] weights, PosRecord[] slice, double k, double divisor)
        {
            const int vecSize = EvalFeatures.FEATURE_SIZE + 1;
            ReadOnlySpan<short> opWeights = new ReadOnlySpan<short>(weights, 0, vecSize);
            ReadOnlySpan<short> egWeights = new ReadOnlySpan<short>(weights, vecSize, vecSize);

            double result = 0.0;
            foreach (PosRecord rec in slice)
            {
                result += ErrorSquared(opWeights, egWeights, rec, k, divisor);
            }

            return result;
        }

        public static double EvalErrorParallel(short[] weights, List<PosRecord[]> slices, double k)
        {
            int totalLength = slices.Sum(s => s.Length);
            double divisor = 1.0 / totalLength;
            Task<double>[] sliceTasks = new Task<double>[slices.Count];
            for (int n = 0; n < slices.Count; n++)
            {
                PosRecord[] slice = slices[n];
                sliceTasks[n] = Task<double>.Factory.StartNew(() => EvalErrorSlice(weights, slice, k, divisor));
            }

            // ReSharper disable once CoVariantArrayConversion
            Task.WaitAll(sliceTasks);
            return sliceTasks.Sum(t => t.Result);
        }

        private static double SolveKParallel(short[] weights, List<PosRecord[]> slices, double a = 0.0, double b = 2.0)
        {
            const double gr = 1.61803399;
            double k1 = b - (b - a) / gr;
            double k2 = a + (b - a) / gr;

            while (Math.Abs(b - a) > 0.01)
            {
                double f_k1 = EvalErrorParallel(weights, slices, k1);
                double f_k2 = EvalErrorParallel(weights, slices, k2);
                if (f_k1 < f_k2)
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
            string[] pieceNames = { "pawns", "knights", "bishops", "rooks", "queens", "kings" };
            short[] wts = solution.Weights;
            Console.WriteLine($"// Solution fitness: {solution.Fitness:F5} generated on {DateTime.Now:R}");
            Console.WriteLine($"Object ID: {solution.Id} - {solution.Description}");
            Console.WriteLine("private static readonly short[] weights =");
            Console.WriteLine("{");
            Console.WriteLine("    /* Opening phase thru move */");
            Console.WriteLine($"    {wts[0]},");
            Console.WriteLine("\n    /* End game phase material */");
            Console.WriteLine($"    {wts[1]},");
            Console.WriteLine("\n    /* Opening mobility weight */");
            Console.WriteLine($"    {wts[2]},");
            Console.WriteLine("\n    /* End game mobility weight */");
            Console.WriteLine($"    {wts[3]},");
            Console.WriteLine("\n    /* Opening king proximity */");
            Console.WriteLine($"    {wts[4]}, {wts[5]}, {wts[6]},");
            Console.WriteLine("\n    /* End game king proximity */");
            Console.WriteLine($"    {wts[7]}, {wts[8]}, {wts[9]},");
            Console.WriteLine("\n    /* Unused weights */");
            Console.WriteLine($"    {wts[10]}, {wts[11]},");
            Console.WriteLine("\n    /* Opening piece values */");
            Console.WriteLine($"    {wts[12]}, {wts[13]}, {wts[14]}, {wts[15]}, {wts[16]}, {wts[17]},");
            Console.WriteLine("\n    /* End game piece values */");
            Console.WriteLine($"    {wts[18]}, {wts[19]}, {wts[20]}, {wts[21]}, {wts[22]}, {wts[23]},");
            Console.WriteLine("\n    /* Opening piece square tables */");
            Console.WriteLine("    #region Opening piece square tables");
            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                Console.WriteLine($"    // {pieceNames[pc]}");
                for (int sq = 0; sq < Constants.MAX_SQUARES; sq++)
                {
                    if (sq % 8 == 0)
                    {
                        if (sq != 0)
                        {
                            Console.WriteLine();
                        }
                        Console.Write("    ");
                    }
                    Console.Write($"{wts[24 + (pc << 6) + sq],4}, ");
                }

                Console.WriteLine();
            }
            Console.WriteLine("    #endregion");
            Console.WriteLine("\n    /* End game piece square tables */");
            Console.WriteLine("    #region End game piece square tables");
            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                Console.WriteLine($"    // {pieceNames[pc]}");
                for (int sq = 0; sq < Constants.MAX_SQUARES; sq++)
                {
                    if (sq % 8 == 0)
                    {
                        if (sq != 0)
                        {
                            Console.WriteLine();
                        }
                        Console.Write("    ");
                    }
                    Console.Write($"{wts[408 + (pc << 6) + sq],4}, ");
                }

                Console.WriteLine();
            }
            Console.WriteLine("    #endregion");
            Console.WriteLine("\n    /* Opening blocked pawns */");
            Console.WriteLine("    #region Opening blocked pawns");
            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                Console.WriteLine($"    // pawns blocked by {pieceNames[pc]}");
                for (int rank = 0; rank < 6; rank++)
                {
                    Console.WriteLine($"    {wts[792 + (pc * 6) + rank]}, // rank {rank + 2}");
                }

                Console.WriteLine();
            }
            Console.WriteLine("    #endregion");
            Console.WriteLine("\n    /* End game blocked pawns */");
            Console.WriteLine("    #region End game blocked pawns");
            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                Console.WriteLine($"    // pawns blocked by {pieceNames[pc]}");
                for (int rank = 0; rank < 6; rank++)
                {
                    Console.WriteLine($"    {wts[828 + (pc * 6) + rank]}, // rank {rank + 2}");
                }

                Console.WriteLine();
            }
            Console.WriteLine("    #endregion");
            Console.WriteLine("\n    /* Opening pawn double move blocked */");
            Console.WriteLine($"    {wts[864]},");
            Console.WriteLine("\n    /* End game pawn double move blocked */");
            Console.WriteLine($"    {wts[865]},");
            Console.WriteLine("\n    /* Opening king *not* in closest promote square */");
            for (int rank = 0; rank < 6; rank++)
            {
                Console.WriteLine($"    {wts[866 + rank]}, // rank {rank + 2}");
            }
            Console.WriteLine("\n    /* End game king *not* in closest promote square */");
            for (int rank = 0; rank < 6; rank++)
            {
                Console.WriteLine($"    {wts[872 + rank]}, // rank {rank + 2}");
            }
            Console.WriteLine("\n    /* Unused */");
            Console.WriteLine("    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,");
            Console.WriteLine("\n    /* Opening isolated pawn */");
            Console.WriteLine($"    {wts[896]},");
            Console.WriteLine("\n    /* End game isolated pawn */");
            Console.WriteLine($"    {wts[897]},");
            Console.WriteLine("\n    /* Opening backward pawn */");
            Console.WriteLine($"    {wts[898]},");
            Console.WriteLine("\n    /* End game backward pawn */");
            Console.WriteLine($"    {wts[899]},");
            Console.WriteLine("\n    /* Opening doubled pawn */");
            Console.WriteLine($"    {wts[900]},");
            Console.WriteLine("\n    /* End game doubled pawn */");
            Console.WriteLine($"    {wts[901]},");
            Console.WriteLine("\n    /* Opening knight on outpost */");
            Console.WriteLine($"    {wts[902]},");
            Console.WriteLine("\n    /* End game knight on outpost */");
            Console.WriteLine($"    {wts[903]},");
            Console.WriteLine("\n    /* Opening bishop on outpost */");
            Console.WriteLine($"    {wts[904]},");
            Console.WriteLine("\n    /* End game bishop on outpost */");
            Console.WriteLine($"    {wts[905]},");
            Console.WriteLine("\n    /* Opening passed pawn */");
            Console.WriteLine("    #region Opening passed pawn");
            for (int rank = 0; rank < 6; rank++)
            {
                Console.WriteLine($"    {wts[906 + rank]}, // rank {rank + 2}");
            }
            Console.WriteLine("    #endregion");
            Console.WriteLine("\n    /* End game passed pawn */");
            Console.WriteLine("    #region End game passed pawn");
            for (int rank = 0; rank < 6; rank++)
            {
                Console.WriteLine($"    {wts[912 + rank]}, // rank {rank + 2}");
            }
            Console.WriteLine("    #endregion");
            Console.WriteLine("\n    /* Opening adjacent pawn */");
            Console.WriteLine("    #region Opening adjacent pawn");
            for (int rank = 0; rank < 6; rank++)
            {
                Console.WriteLine($"    {wts[918 + rank]}, // rank {rank + 2}");
            }
            Console.WriteLine("    #endregion");
            Console.WriteLine("\n    /* End game adjacent pawn */");
            Console.WriteLine("    #region End game adjacent pawn");
            for (int rank = 0; rank < 6; rank++)
            {
                Console.WriteLine($"    {wts[924 + rank]}, // rank {rank + 2}");
            }
            Console.WriteLine("    #endregion");
            Console.WriteLine("\n    /* Opening bishop pair */");
            Console.WriteLine($"    {wts[930]},");
            Console.WriteLine("\n    /* End game bishop pair */");
            Console.WriteLine($"    {wts[931]},");
            Console.WriteLine("\n    /* Opening queen-side pawn majority */");
            Console.WriteLine($"    {wts[932]},");
            Console.WriteLine("\n    /* End game queen-side pawn majority */");
            Console.WriteLine($"    {wts[933]},");
            Console.WriteLine("\n    /* Opening king near passed pawn */");
            Console.WriteLine($"    {wts[934]},");
            Console.WriteLine("\n    /* End game king near passed pawn */");
            Console.WriteLine($"    {wts[935]},");
            Console.WriteLine("\n    /* Opening king-side pawn majority */");
            Console.WriteLine($"    {wts[936]},");
            Console.WriteLine("\n    /* End game king-side pawn majority */");
            Console.WriteLine($"    {wts[937]},");
            Console.WriteLine("\n    /* Unused */");
            Console.WriteLine("    0, 0");
            Console.WriteLine("};");
        }

        private static void RunWeights(string? immortalWt, string? printWt)
        {
            using var rep = new GeneticsRepository();
            if (immortalWt != null)
            {
                ChessWeights? wt = rep.Weights.Find(w => w.Id == new ObjectId(immortalWt)).FirstOrDefault();
                if (wt != null)
                {
                    wt.IsActive = true;
                    wt.IsImmortal = true;
                    wt.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Update(wt);
                }
            }

            if (printWt != null)
            {
                ChessWeights? wt = rep.Weights.Find(w => w.Id == new ObjectId(printWt)).FirstOrDefault();
                if (wt != null)
                {
                    PrintSolution(wt);
                }
            }
        }

        
    }
}