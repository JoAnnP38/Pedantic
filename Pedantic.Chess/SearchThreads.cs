using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class SearchThreads
    {
        static SearchThreads()
        {
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out _);
        }

        public SearchThreads()
        {
            threads = [new SearchThread(true)];
            done = new CountdownEvent(0);
        }

        public bool IsRunning
        {
            get
            {
                return !done.IsSet;
            }
        }

        public int ThreadCount
        {
            get => threads.Length;
            set
            {
                if (value < 1)
                {
                    throw new InvalidOperationException("Pedantic must have at least one search thread.");
                }
                if (value > maxWorkerThreads)
                {
                    throw new Exception($"Simultaneous search threads cannot exceed thread pool capability ({maxWorkerThreads}).");
                }
            
                Array.Resize(ref threads, value);
                for (int n = 1; n < value; n++)
                {
                    if (threads[n] == null)
                    {
                        threads[n] = new SearchThread();
                    }
                }
            }
        }

        public void Wait()
        {
            // wait (i.e. block) for search to be complete
            if (!done.IsSet)
            {
                done.Wait();
            }
        }

        public void ResizeEvalCache()
        {
            int sizeMb = UciOptions.Hash;
            if (!BitOps.IsPow2(sizeMb))
            {
                sizeMb = BitOps.GreatestPowerOfTwoLessThan(sizeMb);
            }
            sizeMb /= UciOptions.Threads;
            sizeMb >>= 2;
            foreach (var thread in threads)
            {
                thread.Cache.Resize(sizeMb);
            }
        }

        public void ClearEvalCache()
        {
            foreach (var thread in threads)
            {
                thread.Cache.Clear();
                thread.History.Clear();
            }
        }

        public void Search(GameClock clock, Board board, int maxDepth, long maxNodes)
        {
            if (done.IsSet)
            {
                done.Reset(threads.Length);
                for (int n = 1; n < threads.Length; n++)
                {
                    threads[n].Search(clock.Clone(), board.Clone(), maxDepth, maxNodes, done);
                }
                threads[0].Search(clock, board, maxDepth, maxNodes, done);
            }
        }

        public void WriteStats()
        {
            if (!UciOptions.CollectStatistics)
            {
                return;
            }
            using var mutex = new Mutex(false, "Pedantic::chess_stats.csv");
            mutex.WaitOne();
            using StreamWriter output = File.AppendText("chess_stats.csv");
            threads[0].WriteStats(output);
            output.Flush();
            output.Close();
            mutex.ReleaseMutex();
        }

        public void Stop()
        {
            if (!done.IsSet)
            {
                foreach(var thread in threads)
                {
                    thread.Stop();
                }
            }
        }

        public long TotalNodes
        {
            get
            {
                if (!done.IsSet)
                {
                    return 0;
                }
                return threads[0].TotalNodes;
            }
        }

        public double TotalTime
        {
            get
            {
                if (!done.IsSet)
                {
                    return 0.0;
                }
                return threads[0].TotalTime;
            }
        }

        public static int MaxWorkerThreads => maxWorkerThreads;

        private readonly CountdownEvent done;
        private SearchThread[] threads;
        private static readonly int maxWorkerThreads;
    }
}
