using Pedantic.Chess;
using System.Diagnostics;
using System.Runtime;

namespace Pedantic
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RunPerft();
        }

        static void RunPerft()
        {
            Perft perft = new();
            Stopwatch watch = new();

            Console.WriteLine(@"Single threaded results:");

            for (int depth = 1; depth < 7; ++depth)
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal; 
                
                watch.Restart();
                ulong actual = perft.Execute(depth);
                watch.Stop();

                Thread.CurrentThread.Priority = ThreadPriority.Normal;
                GCSettings.LatencyMode = GCLatencyMode.Interactive;

                double Mnps = (double)actual / (watch.Elapsed.TotalSeconds * 1000000.0D);
                Console.WriteLine($@"{depth}: Elapsed = {watch.Elapsed}, Mnps: {Mnps,7:N2}, nodes = {actual}");
            }

            Console.WriteLine();
            Console.WriteLine(@"Calculating Perft(6) Average Mnps...");
            double totalSeconds = 0.0D;
            double totalNodes = 0.0D;
            TimeSpan ts = new();
            const int trials = 5;
            for (int i = 0; i < trials; ++i)
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                watch.Restart();
                totalNodes += perft.Execute(6);
                watch.Stop();

                Thread.CurrentThread.Priority = ThreadPriority.Normal;
                GCSettings.LatencyMode = GCLatencyMode.Interactive;

                totalSeconds += watch.Elapsed.TotalSeconds;
                ts += watch.Elapsed;
            }

            TimeSpan avg = ts / (double)trials;
            Console.WriteLine(@"Average Perft(6) Average elapsed: {0}, Mnps: {1,5:N2}", avg, totalNodes / (totalSeconds * 1000000.0D));
        }
    }
}