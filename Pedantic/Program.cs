using Pedantic.Chess;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime;

namespace Pedantic
{
    internal class Program
    {
        enum RunType
        {
            Normal,
            Average,
            Details
        }
        static void Main(string[] args)
        {
            RunType runType = RunType.Normal;
            int trials = 2;
            if (args.Length > 0)
            {
                if (args[0] == "/a")
                {
                    runType = RunType.Average;
                    if (args.Length > 1)
                    {
                        if (!int.TryParse(args[1], out trials))
                        {
                            trials = 2;
                        }

                        trials = Math.Max(trials, 2);
                    }
                }
                else if (args[0] == "/d")
                {
                    runType = RunType.Details;
                    if (args.Length > 1)
                    {
                        if (!int.TryParse(args[1], out trials))
                        {
                            trials = 6;
                        }

                        trials = Math.Max(trials, 5);
                    }
                }
                else if (int.TryParse(args[0], out int result))
                {
                    trials = Math.Max(result, 5);
                }
            }
            RunPerft(runType, trials);
        }

        static void RunPerft(RunType runType, int trials = 2)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            switch (runType)
            {
                case RunType.Normal:
                    RunNormalPerft(trials);
                    break;

                case RunType.Average:
                    RunAveragePerft(trials);
                    break;

                case RunType.Details:
                    RunDetailedPerft(trials);
                    break;
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
        }

        static void RunNormalPerft(int totalDepth)
        {
            Perft perft = new();
            Stopwatch watch = new();

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

        static void RunAveragePerft(int trials)
        {
            Perft perft = new();
            Stopwatch watch = new();

            Console.WriteLine(@"Calculating Perft(6) Average Mnps...");
            double totalSeconds = 0.0D;
            double totalNodes = 0.0D;
            TimeSpan ts = new();
            for (int i = 0; i < trials; ++i)
            {
                watch.Restart();
                totalNodes += perft.Execute(6);
                watch.Stop();

                totalSeconds += watch.Elapsed.TotalSeconds;
                ts += watch.Elapsed;
            }

            TimeSpan avg = ts / (double)trials;
            Console.WriteLine(@"Average Perft(6) Average elapsed: {0}, Mnps: {1,5:N2}", avg, totalNodes / (totalSeconds * 1000000.0D));
        }

        static void RunDetailedPerft(int totalDepth)
        {
            Perft perft = new();
            Stopwatch watch = new();

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