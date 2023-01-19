using Pedantic.Chess;
using System.Diagnostics;
using System.Runtime;
using System.CommandLine;
using System.Text;


namespace Pedantic
{
    public class Program
    {
        public const string PROGRAM_NAME_VER = "Pedantic v0.0.1";
        public const string AUTHOR = "JoAnn D. Peeler";
        public const string PROGRAM_URL = "https://github.com/JoAnnP38/Pedantic";

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
            var rootCommand = new RootCommand("The pedantic chess engine.")
            {
                uciCommand,
                perftCommand
            };

            uciCommand.SetHandler(async (searchType, inFile, errFile) => await RunUci(searchType, inFile, errFile), searchTypeOption, commandFileOption, errorFileOption);
            perftCommand.SetHandler((runType, depth, fen) => RunPerft(runType, depth, fen), typeOption, depthOption, fenOption);

            return rootCommand.InvokeAsync(args).Result;
        }

        static async Task RunUci(SearchType searchType, string? inFile, string? errFile)
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
                    Console.WriteLine(@"option name Search Algorithm type combo default PV var PV var MTD(f) var Minimal");
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

                    case "Search":
                        if (tokens[3] == "Algorithm" && tokens[4] == "value" && TryParse(tokens[5], out SearchType searchType))
                        {
                            Engine.SearchType = searchType;
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
    }
}