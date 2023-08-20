using Pedantic.Chess;
using Pedantic.Utilities;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Pedantic.Tuning
{
    public class HceTuner
    {
        internal const double GOLDEN_RATIO = 1.618033988749894;
        internal const double DEFAULT_K = 0.00385;
        internal const double TOLERENCE = 1.0e-7;
        internal const int MAX_FAILURE = 2;
        internal const double COMPARISON_EPSILON = 5.0e-13;

        public HceTuner(short[] weights, IList<PosRecord> positions)
        { 
            this.weights = weights;
            this.positions = positions;
            k = SolveK();
            k = k == 0.0 || k == 1.0 ? DEFAULT_K : k;
            mb = new(positions.Count);
            momentums = new IncMomentum[weights.Length];

            for (int n = 0; n < weights.Length; n++)
            {
                momentums[n] = new IncMomentum((sbyte)EvalFeatures.GetOptimizationIncrement(n));
            }

#if DEBUG
            rand = new Random(1);
#else
            rand = new Random();
#endif
        }

        public HceTuner(IList<PosRecord> positions)
        { 
            weights = ZeroWeights();
            this.positions = positions;
            k = DEFAULT_K;
            mb = new(positions.Count);
            momentums = new IncMomentum[weights.Length];

            for (int n = 0; n < weights.Length; n++)
            {
                momentums[n] = new IncMomentum((sbyte)EvalFeatures.GetOptimizationIncrement(n));
            }

#if DEBUG
            rand = new Random(1);
#else
            rand = new Random();
#endif
        }

        public (double Error, double Accuracy, short[] Weights) Train(int maxEpoch, TimeSpan? maxTime, double precision = TOLERENCE)
        {
            DateTime start = DateTime.Now;
            Console.WriteLine($"Data size: {positions.Count}, K: {k:F6}, Start time: {start:h\\:mm\\:ss}");
            short[] bestWeights = ArrayEx.Clone(weights);
            int wtLen = weights.Length;
            double currError = MeanSquaredError(k);
            double bestError = currError + precision;
            double accuracy = Accuracy();
            int failures = 0, epoch = 0;
            Console.WriteLine($"\nStarting accuracy: {accuracy:F4}");
            Console.WriteLine($"Mini-batch optimization to begin ({mb.BatchCount} batch count)\n");
            Console.WriteLine($"Epoch {epoch,3} - \u03B5: {currError:F6}");
            int[] wtIndices = GetIndices(wtLen);

            while (failures < MAX_FAILURE && epoch < maxEpoch && (maxTime == null || DateTime.Now - start < maxTime))
            {
                if (currError < bestError)
                {
                    bestError = currError;
                    Array.Copy(weights, bestWeights, bestWeights.Length);
                }
                else
                {
                    Array.Copy(bestWeights, weights, weights.Length);
                }

                rand.Shuffle(wtIndices);
                double refError = MeanSquaredError(k, mb);
                double errAdjust = currError / refError;
                int optAttempts = 0;
                int optHits = 0;
                DateTime wtTrainTime = DateTime.Now;
                double effRate = 0.0;

                for (int n = 0; n < wtLen; n++)
                {
                    double pctComplete = (n * 100.0) / wtLen;
                    TimeSpan deltaT = DateTime.Now - wtTrainTime;
                    effRate = optAttempts > 0 ? (double)optHits / optAttempts : 0.0;
                    Console.Write($"Epoch {pctComplete,3:F0}%- \u03B5: {refError * errAdjust:F6}, \u0394t: {deltaT:h\\:mm\\:ss}, eff: {effRate:F3} ({optHits}/{optAttempts})...\r");
                    int wt = wtIndices[n];
                    short increment = momentums[wt].BestIncrement;

                    if (increment != 0)
                    {
                        short oldValue = weights[wt];
                        weights[wt] += increment;
                        double error = MeanSquaredError(k, mb);
                        optAttempts++;
                        bool goodIncrement = error + COMPARISON_EPSILON < refError;

                        if (!goodIncrement)
                        {
                            increment = momentums[wt].NegIncrement(increment);
                            weights[wt] += increment;
                            error = MeanSquaredError(k, mb);
                            optAttempts++;
                            goodIncrement = error + COMPARISON_EPSILON < refError;
                        }

                        if (goodIncrement)
                        {
                            optHits++;
                            refError = error;
                            momentums[wt].AddImprovingIncrement((sbyte)increment);
                        }
                        else
                        {
                            momentums[wt].NoImprovement();
                            weights[wt] = oldValue;
                        }
                    }
                }

                mb.NextBatch();
                TimeSpan epochT = DateTime.Now - wtTrainTime;
                currError = MeanSquaredError(k);
                if (currError + precision < bestError)
                {
                    failures = 0;
                    epoch++;
                }
                else
                {
                    failures++;

                    if (failures >= MAX_FAILURE && mb.Increment())
                    {
                        failures = 0;
                        rand.Shuffle(positions);
                        accuracy = Accuracy();
                        Console.WriteLine($"Epoch {epoch, 3} - \u03B5: {currError:F6}, Δt: {epochT:h\\:mm\\:ss}, NO IMPROVEMENT                              ");
                        Console.WriteLine($"\nNew accuracy: {accuracy:F4}");
                        Console.WriteLine($"Increasing mini-batch size ({mb.BatchCount} batch count), elapsed: {DateTime.Now - start:d\\.hh\\:mm\\:ss}\n");
                        continue;
                    }
                }

                if (failures == 0)
                {
                    Console.WriteLine($"Epoch {epoch,3} - \u03B5: {currError:F6}, Δt: {epochT:h\\:mm\\:ss}, eff: {effRate:F3}                           ");
                }
                else
                {
                    Console.WriteLine($"Epoch {epoch,3} - \u03B5: {currError:F6}, Δt: {epochT:h\\:mm\\:ss}, NO IMPROVEMENT                              ");
                }
            }

            if (currError < bestError)
            {
                bestError = currError;
            }
            else
            {
                Array.Copy(bestWeights, weights, weights.Length);
            }

            accuracy = Accuracy();
            Console.WriteLine($"Epoch {epoch,3} - \u03B5: {bestError:F6}, accuracy: {accuracy:F4}, FINAL");
            Console.WriteLine($"Total training time: {DateTime.Now - start:d\\.hh\\:mm\\:ss}");
            return (bestError, accuracy, bestWeights);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Sigmoid(double k, double eval)
        {
            return 1.0 / (1.0 + Math.Exp(-k * eval));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private short ComputeEval(PosRecord rec)
        {
            ReadOnlySpan<short> opWeights = new(weights, 0, EvalFeatures.FEATURE_SIZE);
            ReadOnlySpan<short> egWeights = new(weights, EvalFeatures.FEATURE_SIZE, EvalFeatures.FEATURE_SIZE);
            short result = rec.Features.Compute(opWeights, egWeights);
            return rec.Features.SideToMove == Color.White ? result : (short)-result;
        }

        private double MeanSquaredError(double k)
        {
            ConcurrentBag<double> subtotals = new();
            
            Parallel.For(0, positions.Count, () => 0.0, (j, loop, subtotal) =>
            {
                double result = positions[j].Result - Sigmoid(k, ComputeEval(positions[j]));
                subtotal += result * result;
                return subtotal;
            },
                subtotals.Add
            );

            return subtotals.Sum() / positions.Count;
        }

        private double MeanSquaredError(double k, MiniBatch mb)
        {
            ConcurrentBag<double> subtotals = new();
            var (Start, End) = mb.Batch;

            Parallel.For(Start, End, () => 0.0, (j, loop, subtotal) =>
            {
                double result = positions[j].Result - Sigmoid(k, ComputeEval(positions[j]));
                subtotal += result * result;
                return subtotal;
            },
                subtotals.Add
            );

            int count = End - Start;
            return subtotals.Sum() / count;
        }

        private double Accuracy()
        {
            ConcurrentBag<(int correct, int wrong)> subtotals = new();

            Parallel.For(0, positions.Count, () => (correct: 0, wrong: 0), (j, loop, subtotal) =>
            {
                float normalized = (float)Sigmoid(k, ComputeEval(positions[j]));
                float predicted = normalized switch
                {
                    >= 0.00f and <= 0.33f => 0.0f,
                    > 0.33f and < 0.67f => 0.5f,
                    >= 0.67f and <= 1.00f => 1.0f,
                    _ => -1.0f // this will be counted as wrong
                };
                if (predicted == positions[j].Result)
                {
                    subtotal = (subtotal.correct + 1, subtotal.wrong);
                }
                else
                {
                    subtotal = (subtotal.correct, subtotal.wrong + 1);
                }
                return subtotal;
            },
                subtotals.Add
            );

            (int correct, int wrong) total = (0, 0);
            foreach (var (correct, wrong) in subtotals)
            {
                total = (total.correct + correct, total.wrong + wrong);
            }
            return (double)total.correct / (total.correct + total.wrong);
        }

        private double SolveK(double a = 0.0, double b = 1.0)
        {
            double k1 = b - (b - a) / GOLDEN_RATIO;
            double k2 = a + (b - a) / GOLDEN_RATIO;

            while (Math.Abs(b - a) > TOLERENCE)
            {
                double f1 = MeanSquaredError(k1);
                double f2 = MeanSquaredError(k2);

                if (f1 < f2)
                {
                    b = k2;
                }
                else
                {
                    a = k1;
                }
                k1 = b - (b - a) / GOLDEN_RATIO;
                k2 = a + (b - a) / GOLDEN_RATIO;
            }

            return (b + a) / 2.0;
        }
        private static int[] GetIndices(int maxIndex)
        {
            int[] indices = new int[maxIndex];
            for (int n = 0; n < indices.Length; ++n)
            {
                indices[n] = n;
            }
            return indices;
        }

        private static short[] ZeroWeights()
        {
            short[] wts = new short[EvalFeatures.FEATURE_SIZE * 2];
            for (Piece pc = Piece.Pawn; pc <= Piece.Queen; pc++)
            {
                int index = EvalFeatures.MATERIAL + (int)pc;
                wts[index] = (short)pc.Value();
                wts[index + EvalFeatures.FEATURE_SIZE] = (short)pc.Value();
            }
            return wts;
        }

        private readonly double k;
        private readonly short[] weights;
        private readonly IList<PosRecord> positions;
        private readonly MiniBatch mb;
        private readonly IncMomentum[] momentums;
        private readonly Random rand;
    }
}
