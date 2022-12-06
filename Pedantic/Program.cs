using Pedantic.Chess;
using System.Diagnostics;
using System.Runtime;
using System.CommandLine;


namespace Pedantic
{
    internal class Program
    {
        private static Perft perft = new();
        private static Stopwatch watch = new();

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
                description: "Specify the perft variant to execute.",
                getDefaultValue: () => PerftRunType.Normal);
            var depthOption = new Option<int>(
                name: "--depth",
                description: "Specifies the maximum depth for the perft test.",
                getDefaultValue: () => 6);
            var uciCommand = new Command("uci", "Start the pedantic application in UCI mode.");
            var perftCommand = new Command("perft", "Run a standard Perft test.")
            {
                typeOption,
                depthOption
            };
            var rootCommand = new RootCommand("The pedantic chess engine.")
            {
                uciCommand,
                perftCommand
            };

            uciCommand.SetHandler(() => Uci());
            perftCommand.SetHandler((runType, depth) => RunPerft(runType, depth), typeOption, depthOption);

            return rootCommand.InvokeAsync(args).Result;
        }

        static void Uci()
        {

        }

        static void RunPerft(PerftRunType runType, int depth)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            switch (runType)
            {
                case PerftRunType.Normal:
                    RunNormalPerft(depth);
                    break;

                case PerftRunType.Details:
                    RunDetailedPerft(depth);
                    break;

                case PerftRunType.Average:
                    RunAveragePerft(depth);
                    break;
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
        }

        static void RunNormalPerft(int totalDepth)
        {
            Console.WriteLine(@"Single threaded results:");

            for (int depth = 1; depth <= totalDepth; ++depth)
            {
                watch.Restart();
                ulong actual = perft.Execute(depth);
                watch.Stop();

                double Mnps = (double)actual / (watch.Elapsed.TotalSeconds * 1000000.0D);
                Console.WriteLine($@"{depth}: Elapsed = {watch.Elapsed}, Mnps: {Mnps,7:N2}, nodes = {actual}");
            }
        }

        static void RunAveragePerft(int totalDepth)
        {
            Console.WriteLine(@"Calculating Perft(6) Average Mnps...");
            const int trials = 5;
            double totalSeconds = 0.0D;
            double totalNodes = 0.0D;
            TimeSpan ts = new();
            for (int i = 0; i < trials; ++i)
            {
                watch.Restart();
                totalNodes += perft.Execute(totalDepth);
                watch.Stop();

                totalSeconds += watch.Elapsed.TotalSeconds;
                ts += watch.Elapsed;
            }

            TimeSpan avg = ts / (double)trials;
            Console.WriteLine(@"Average Perft(6) Average elapsed: {0}, Mnps: {1,5:N2}", avg, totalNodes / (totalSeconds * 1000000.0D));
        }

        static void RunDetailedPerft(int totalDepth)
        {
            Console.WriteLine(@"Running Perft and collecting details...");
            Console.WriteLine(@"+-------+--------+--------------+------------+---------+---------+------------+------------+------------+");
            Console.WriteLine(@"| Depth |  Mnps  |     Nodes    |  Captures  |   E.p.  | Castles |   Checks   | Checkmates | Promotions |");
            Console.WriteLine(@"+-------+--------+--------------+------------+---------+---------+------------+------------+------------+");

            for (int depth = 1; depth <= totalDepth; ++depth)
            {
                watch.Restart();
                Perft.Counts counts = perft.ExecuteWithDetails(depth);
                watch.Stop();
                double Mnps = (double)counts.Nodes / (watch.Elapsed.TotalSeconds * 1000000.0D);
                Console.WriteLine(@$"|{depth,4:N0}   | {Mnps,6:N2} |{counts.Nodes,13:N0} |{counts.Captures,11:N0} |{counts.EnPassants,8:N0} |{counts.Castles,8:N0} |{counts.Checks,11:N0} | {counts.Checkmates,10:N0} |{counts.Promotions,11:N0} |");
                Console.WriteLine(@"+-------+--------+--------------+------------+---------+---------+------------+------------+------------+");
            }
        }
    }
}