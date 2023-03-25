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
                commandFileOption,
                errorFileOption,
                randomSearchOption
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
                dataFileOption,
                maxPositionsOption
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

            uciCommand.SetHandler(async (inFile, errFile, random) => await RunUci(inFile, errFile, random), commandFileOption, errorFileOption, randomSearchOption);
            perftCommand.SetHandler(RunPerft, typeOption, depthOption, fenOption);
            labelCommand.SetHandler(RunLabel, pgnFileOption, dataFileOption, maxPositionsOption);
            learnCommand.SetHandler(RunLearn, dataFileOption, sampleOption, iterOption, preserveOption);
            weightsCommand.SetHandler(RunWeights, immortalOption, displayOption);

            return rootCommand.InvokeAsync(args).Result;
        }

        private static async Task RunUci(string? inFile, string? errFile, bool random)
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
                Engine.RandomSearch = random;
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
                    Console.WriteLine(@"option name Evaluation_ID type string default <empty>");
                    Console.WriteLine(@"option name Random_Search type check default false");
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

                    case "Evaluation_ID":
                        if (tokens[3] == "value")
                        {
                            Engine.EvaluationId = tokens.Length >= 5 ? tokens[4] : string.Empty;
                        }

                        break;

                    case "Random_Search":
                        if (tokens[3] == "value" && bool.TryParse(tokens[4], out bool randomSearch))
                        {
                            Engine.RandomSearch = randomSearch;
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

        private static void RunLabel(string? pgnFile, string? dataFile, int maxPositions = 8000000)
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
                Console.WriteLine(@"Hash,Ply,GamePly,FEN,Result");
                foreach (var p in posReader.Positions(Console.In))
                {
                    if (!hashes.Contains(p.Hash))
                    {
                        Console.Error.Write($"{++count}\r");
                        hashes.Add(p.Hash);
                        Console.WriteLine($@"{p.Hash:X16},{p.Ply},{p.GamePly},{p.Fen},{p.Result:F1}");
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
                    short[] weights = ArrayEx.Clone(startingWeight.Weights);
                    double k = SolveKParallel(weights, slices);
                    Console.WriteLine($@"K = {k:F2}");
                    startingWeight.Fitness = (float)EvalErrorParallel(weights, slices, k);
                    ChessWeights guess = new ChessWeights(startingWeight)
                    {
                        IsImmortal = false
                    };
                    ChessWeights optimized = LocalOptimize(guess, weights, slices, k, maxPass,
                        preserve);
                    optimized.Description = "Optimized";
                    optimized.UpdatedOn = DateTime.UtcNow;
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
            DateTime startTime = DateTime.Now;
            bool improved = true;
            double curError = guess.Fitness;
            double refError = MiniEvalErrorParallel(weights, slices, k, 0);
            double bestError = curError + CONVERGENCE_TOLERANCE * 2;
            short[] bestWeights = ArrayEx.Clone(weights);
            int wtLen = weights.Length;
            int passes = 0;
            int failureCount = 0;
            int sampleSize = slices.Sum(s => s.Length);
            using var rep = new GeneticsRepository();

            Console.WriteLine($"Pass stats {passes,3} - \u03B5: {curError:F6}");
            int[] index = new int[wtLen];
            for (int n = 0; n < wtLen; n++)
            {
                index[n] = n;
            }

            while (improved && failureCount < 3 && passes < maxPass)
            {
                improved = false;
                if (curError < bestError)
                {
                    bestError = curError;
                    Array.Copy(weights, bestWeights, bestWeights.Length);
                }

                DateTime wtOptTime = DateTime.Now;
                Random.Shared.Shuffle(index);
                int optAttempts = 0;
                int optHits = 0;
                float effRate = 0;
                refError = MiniEvalErrorParallel(weights, slices, k, passes);
                double errAdjust = curError / refError;

                for (int n = 0; n < wtLen; n++)
                {
                    double completed = ((double)n / wtLen) * 100.0;
                    TimeSpan deltaT = DateTime.Now - wtOptTime;
                    effRate = optAttempts > 0 ? (float)optHits / optAttempts : 0;
                    Console.Write($"Pass stats {completed,3:F0}%- \u03B5: {refError * errAdjust:F6}, \u0394t: {deltaT:mm\\:ss}, eff: {effRate:F3} ({optHits}/{optAttempts})...\r");
                    int i = index[n];
                    short increment = EvalFeatures.GetOptimizationIncrement(i);
                    if (increment > 0)
                    {
                        if (Random.Shared.NextBoolean())
                        {
                            increment = (short)-increment;
                        }
                        short oldValue = weights[i];
                        weights[i] += increment;
                        double error = MiniEvalErrorParallel(weights, slices, k, passes);
                        optAttempts++;
                        bool goodIncrement = error < refError;
                        improved = improved || goodIncrement;

                        if (!goodIncrement)
                        {
                            weights[i] -= (short)(increment * 2);
                            error = MiniEvalErrorParallel(weights, slices, k, passes);
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

                ++passes;
                DateTime now = DateTime.Now;
                TimeSpan totalElapsed = now - startTime;
                TimeSpan avgT = totalElapsed.Divide(passes);
                TimeSpan passT = now - wtOptTime;
                curError = EvalErrorParallel(weights, slices, k);

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
                    Console.WriteLine($"Pass stats {passes,3} - \u03B5: {curError:F6}, Δt: {passT:mm\\:ss}, eff: {effRate:F3}, OID: {intermediate.Id}");
                }
                else
                {
                    Console.WriteLine($"Pass stats {passes,3} - \u03B5: {curError:F6}, Δt: {passT:mm\\:ss}, eff: {effRate:F3}                        ");
                }

                if (curError + CONVERGENCE_TOLERANCE < bestError)
                {
                    failureCount = 0;
                }
                else
                {
                    ++failureCount;
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

        public static PosRecord[] LoadSample(int sampleSize, StreamReader sr)
        {
            int[] selections = GetSampleSelections(sampleSize, sr);
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
                string fen = fields[3];
                float result = float.Parse(fields[4]);
                posRecordList.Add(new PosRecord(fen, result));
            }

            sr.BaseStream.Seek(0L, SeekOrigin.Begin);
            return posRecordList.ToArray();
        }

        private static int[] GetSampleSelections(int sampleSize, StreamReader sr)
        {
            int lineCount = 0;
            while (Console.ReadLine() != null)
            {
                ++lineCount;
            }

            sr.BaseStream.Seek(0L, SeekOrigin.Begin);
            int[] pop = new int[lineCount];
            int i = lineCount - 1;
            while (i >= 0) { pop[i] = i--; }

            int[] sample = new int[sampleSize];

            for (int n = 0; n < sampleSize; ++n)
            {
                int m = Random.Shared.Next(0, lineCount);
                sample[n] = pop[m] + 1;
                lineCount--;
                pop[m] = pop[lineCount];
            }

            Array.Sort(sample);
            return sample;
        }

        public static int UsableProcessorCount
        {
            get
            {
                int processorCount = 1;

                processorCount = Math.Max(Environment.ProcessorCount - 6, 1);
                return processorCount;
            }
        }

        public static List<PosRecord[]> CreateSlices(PosRecord[] records)
        {
            List<PosRecord[]> slices = new List<PosRecord[]>();
            int sliceCount = UsableProcessorCount;
            int sliceLength = records.Length / sliceCount;

            slices.EnsureCapacity(sliceCount);
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

        private static double ErrorSquared(ReadOnlySpan<short> opWeights, ReadOnlySpan<short> egWeights, PosRecord rec, double k)
        {
            int qScore = rec.Features.Compute(opWeights, egWeights);
            qScore = rec.Features.SideToMove == Color.White ? qScore : -qScore;
            double result = rec.Result - Sigmoid(k, qScore);
            return result * result;
        }

        private static double EvalErrorSlice(short[] weights, ReadOnlySpan<PosRecord> slice, double k, double divisor)
        {
            const int vecSize = EvalFeatures.FEATURE_SIZE;
            ReadOnlySpan<short> opWeights = new ReadOnlySpan<short>(weights, 0, vecSize);
            ReadOnlySpan<short> egWeights = new ReadOnlySpan<short>(weights, vecSize, vecSize);

            double result = 0.0;
            foreach (PosRecord rec in slice)
            {
                result += ErrorSquared(opWeights, egWeights, rec, k);
            }

            return divisor * result;
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
            double result = sliceTasks.Sum(t => t.Result);
            Array.ForEach(sliceTasks, t => t.Dispose());
            return result;
        }

        public static double MiniEvalErrorParallel(short[] weights, List<PosRecord[]> slices, double k, int batch)
        {
            int totalLength = slices.Sum(s => s.Length);
            int miniBatchSize = totalLength / 10;
            if (UsableProcessorCount == 1)
            {
                return EvalErrorParallel(weights, slices, k);
            }

            int sliceLen = totalLength / UsableProcessorCount;
            int batchLen = miniBatchSize / UsableProcessorCount;
            int totalBatches = sliceLen / batchLen;
            int start = (batch % totalBatches) * batchLen;
            double divisor = 1.0 / (batchLen * UsableProcessorCount);
            Task<double>[] tasks = new Task<double>[slices.Count];
            for (int n = 0; n < slices.Count; n++)
            {
                PosRecord[] slice = slices[n];
                tasks[n] = Task<double>.Factory.StartNew(() => EvalErrorSlice(weights, slice.AsSpan(start, batchLen), k, divisor));
            }

            Task.WaitAll(tasks);
            double result = tasks.Sum(t => t.Result);
            Array.ForEach(tasks, t => t.Dispose());
            return result;
        }

        private static double SolveKParallel(short[] weights, List<PosRecord[]> slices, double a = 0.0, double b = 2.0)
        {
            const double gr = 1.61803399;
            double k1 = b - (b - a) / gr;
            double k2 = a + (b - a) / gr;

            while (Math.Abs(b - a) > 0.01)
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
            WriteLine($"// Solution fitness: {solution.Fitness:F5} generated on {DateTime.Now:R}");
            WriteLine($"// Object ID: {solution.Id} - {solution.Description}");
            WriteLine("private static readonly short[] weights =");
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
            int centerLength = (60 - sectionTitle.Length) / 2;
            string line = new string('-', centerLength - 3);
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
            WriteLine($"#region {section} piece square values */");
            WriteLine();
            int table = 0;
            for (int n = 0; n < 384; n++)
            {
                if (n % 8 == 0)
                {
                    if (n != 0)
                    {
                        WriteLine();
                    }
                    if (n % 64 == 0)
                    {
                        if (n != 0)
                        {
                            WriteLine();
                        }
                        WriteLine($"/* {pieceNames[table++]} */");
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
            WriteLine($"/* {section} passed pawns */");
            WriteLine($"{wts[ChessWeights.PASSED_PAWN]},");
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
        }

        private static void WriteIndent()
        {
            string indent = new string(' ', indentLevel * 4);
            Console.Write(indent);
        }
        private static void WriteLine(string text = "")
        {
            WriteIndent();
            Console.WriteLine(text);
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

                var otherWts = rep.Weights.Find(w => w.IsActive && w.IsImmortal && w.Id != new ObjectId(immortalWt));
                foreach (var w in otherWts)
                {
                    w.IsActive = false;
                    w.UpdatedOn = DateTime.UtcNow;
                    rep.Weights.Update(w);
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

        private static int indentLevel = 0;
    }
}