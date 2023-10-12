using System.Diagnostics;
using System.Globalization;
using System.Text;

using Pedantic.Chess;

namespace Pedantic.Tuning
{
    public class TrainingDataFile : IDisposable
    {
        private readonly string dataPath;
        private readonly StreamReader sr;
        private bool disposedValue;
        private readonly Random random;
        private static readonly float[] validResults = { PosRecord.WDL_LOSS, PosRecord.WDL_DRAW, PosRecord.WDL_WIN };

        public TrainingDataFile(string path, Encoding encoding) 
        { 
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Training data file not found.", path);
            }

            dataPath = path;
            sr = new StreamReader(path, encoding);
            disposedValue = false;

#if DEBUG
            random = new Random(1);
#else
            random = new Random();
#endif
        }

        public TrainingDataFile(string path) : this(path, Encoding.UTF8)
        { }

        // load all position in the file
        public List<PosRecord> LoadFile()
        {
            int lineCount = LineCount();

            if (lineCount <= 1)
            {
                throw new Exception($"Training data file is empty.");
            }

            Console.WriteLine($"Examining data file: \"{dataPath}\"");
            List<PosRecord> records = new(--lineCount); // do not count header
            string? line;
            int currLine = 0;
            Stopwatch clock = new();
            clock.Start();
            long currMs = clock.ElapsedMilliseconds;

            try
            {
                sr.ReadLine(); // skip header row

                while ((line = sr.ReadLine()) != null)
                {
                    ++currLine;
                    int count = records.Count;
                    if (count == AddPosRecord(records, line))
                    {
                        Console.WriteLine($"Unrecognized format found in line {currLine}: {line[..16]}...");
                        continue;
                    }

                    if (clock.ElapsedMilliseconds - currMs > 2000)
                    {
                        Console.Write($"Loading {records.Count} of {lineCount} ({records.Count * 100 / lineCount}%)...\r");
                        currMs = clock.ElapsedMilliseconds;
                    }
                }

                clock.Stop();
                Console.WriteLine($"Loading {records.Count} of {records.Count} (100%)...");
                PrintStatistics(records);
                sr.BaseStream.Seek(0, SeekOrigin.Begin);
                return records;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected exception occurred at line {currLine}: {ex.Message}", ex);
            }
        }

        // load a subset of the data file specified by 'sampleSize' and optionally save subset
        // to its own file
        public List<PosRecord> LoadSample(int sampleSize, bool save)
        {
            if (sampleSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleSize));
            }

            int lineCount = LineCount() - 1;
            if (sampleSize >= lineCount)
            {
                Console.WriteLine("Specified sample size is larger than file. Entire file will be returned.");
                return LoadFile();
            }

            int currLine = 0;
            List<PosRecord> records = new(sampleSize);
            Queue<string> wins = new();
            Queue<string> draws = new();
            Queue<string> losses = new();
            int[] selections = SampleSelections(sampleSize + sampleSize / 7, lineCount);
            StreamWriter? sw = save ? new StreamWriter(OutputName(), false, Encoding.UTF8) : null;
            Stopwatch clock = new();
            clock.Start();
            long currMs = clock.ElapsedMilliseconds;

            try
            {
                string? line = sr.ReadLine(); // read the header
                if (line != null)
                {
                    sw?.WriteLine(line);
                }

                foreach (int sel in selections)
                {
                    while (currLine++ < sel)
                    {
                        sr.ReadLine();
                    }

                    line = sr.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (IsValidResult(line, out float result))
                    {
                        switch (result)
                        {
                            case PosRecord.WDL_WIN:
                                wins.Enqueue(line);
                                break;

                            case PosRecord.WDL_DRAW:
                                draws.Enqueue(line);
                                break;

                            case PosRecord.WDL_LOSS:
                                losses.Enqueue(line);
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unrecognized format found in line {currLine}: {line[..16]}...");
                        continue;
                    }

                    // only add when you can ensure an even distribution of wins, draws and losses
                    // try to maintain WDL ratio of 1:1:1
                    if (wins.Count > 0 && draws.Count > 0 && losses.Count > 0)
                    {
                        if (AddPosRecord(records, wins.Dequeue(), sw) >= sampleSize)
                        {
                            break;
                        }

                        if (AddPosRecord(records, losses.Dequeue(), sw) >= sampleSize)
                        {
                            break;
                        }

                        if (AddPosRecord(records, draws.Dequeue(), sw) >= sampleSize)
                        {
                            break;
                        }
                    }

                    if (clock.ElapsedMilliseconds - currMs > 2000)
                    {
                        Console.Write($"Loading {records.Count} of {sampleSize} ({records.Count * 100 / sampleSize}%)...\r");
                        currMs = clock.ElapsedMilliseconds;
                    }
                }

                // if main loop ends and we have still not collected full sample, then 
                // just add any remaining positions in the queues in as even a manner
                // as possible
                while (records.Count < sampleSize && (wins.Count + losses.Count + draws.Count) > 0)
                {
                    if (wins.Count > 0 && AddPosRecord(records, wins.Dequeue(), sw) >= sampleSize)
                    {
                        break;
                    }

                    if (losses.Count > 0 && AddPosRecord(records, losses.Dequeue(), sw) >= sampleSize)
                    {
                        break;
                    }

                    if (draws.Count > 0 && AddPosRecord(records, draws.Dequeue(), sw) >= sampleSize)
                    {
                        break;
                    }
                }

                clock.Stop();
                Console.WriteLine($"Loading {records.Count} of {records.Count} (100%)...");
                PrintStatistics(records);
                sr.BaseStream.Seek(0, SeekOrigin.Begin);
                return records;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected exception occurred at line {currLine}: {ex.Message}", ex);
            }
            finally
            {
                sw?.Close();
            }
        }

        public int LineCount()
        {
            int lineCount = 0;
            while (sr.ReadLine() != null)
            {
                lineCount++;
            }
            sr.BaseStream.Seek(0, SeekOrigin.Begin);
            return lineCount;
        }

        public int[] SampleSelections(int size, int dataLen)
        {
            int i = dataLen - 1;
            int[] pop = new int[dataLen];
            while (i >= 0) { pop[i] = i--; }

            int[] selections = new int[size];

            for (int n = 0; n < size; n++)
            {
                int m = random.Next(0, dataLen);
                selections[n] = pop[m];
                pop[m] = pop[--dataLen];
            }

            Array.Sort(selections);
            return selections;
        }

        public static string OutputName()
        {
            return $"Pedantic_Sample_{Constants.APP_VERSION}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        }

        public static void PrintStatistics(IEnumerable<PosRecord> positions)
        {
            int totalWins = 0, totalDraws = 0, totalLosses = 0, totalPositions = 0;
            foreach (var pos in positions)
            {
                totalPositions++;
                switch (pos.Result)
                {
                    case PosRecord.WDL_WIN:
                        totalWins++;
                        break;

                    case PosRecord.WDL_DRAW:
                        totalDraws++;
                        break;

                    case PosRecord.WDL_LOSS:
                        totalLosses++;
                        break;
                }
            }

            double minWDL = Math.Min(totalWins, Math.Min(totalDraws, totalLosses));
            double fpWins = totalWins / minWDL;
            double fpDraws = totalDraws / minWDL;
            double fpLosses = totalLosses / minWDL;

            Console.WriteLine($"\nWDL Ratio: {totalWins:#,#} : {totalDraws:#,#} : {totalLosses:#,#} ({fpWins:F3} : {fpDraws:F3} : {fpLosses:F3})\n");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    sr.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static bool IsValidResult(string line, out float result)
        {
            int commaAt = line.LastIndexOf(',') + 1;
            return float.TryParse(line.AsSpan(commaAt), out result) && validResults.Contains(result);
        }

        private static int AddPosRecord(List<PosRecord> records, string line, StreamWriter? sw = null)
        {
            int commaAt = 0;
            for (int n = 0; n < 3; n++)
            {
                commaAt = line.IndexOf(',', commaAt) + 1;
            }
            int nextCommaAt = line.IndexOf(',', commaAt);
            string fen = line[commaAt..nextCommaAt];

            if (!Fen.IsValidFen(fen))
            {
                return records.Count;
            }

            ReadOnlySpan<char> lSpan = line.AsSpan();
            commaAt = nextCommaAt + 1;
            nextCommaAt = line.IndexOf(',', commaAt);

            if (!byte.TryParse(lSpan[commaAt..nextCommaAt], out byte hasCastled) || (hasCastled & ~3) != 0)
            {
                return records.Count;
            }

            commaAt = nextCommaAt + 1;
            if (!float.TryParse(lSpan[commaAt..], out float result) || !validResults.Contains(result))
            {
                return records.Count;
            }

            sw?.WriteLine(line);
            records.Add(new PosRecord(fen, hasCastled, result));
            return records.Count;
        }
    }
}