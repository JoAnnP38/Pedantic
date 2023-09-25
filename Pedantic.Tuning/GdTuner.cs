using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Pedantic.Chess;
using Pedantic.Genetics;
using Pedantic.Utilities;

namespace Pedantic.Tuning
{
    public class GdTuner : Tuner
    {
        public GdTuner(short[] weights, IList<PosRecord> positions, int? seed)
            : base(positions, seed)
        {
            this.weights = new float[weights.Length];
            CopyWeights(weights, this.weights);
            gradient = new float[weights.Length];
            k = SolveK();
            if ((k > -TOLERENCE && k < TOLERENCE) || (k > 1.0 - TOLERENCE && k < 1.0 + TOLERENCE))
            {
                k = DEFAULT_K;
            }
        }

        public GdTuner(IList<PosRecord> positions, int? seed)
            : base(positions, seed)
        {
            weights = ZeroWeights();
            gradient = new float[weights.Length];
            k = DEFAULT_K;
        }

        public override (double Error, double Accuracy, short[] Weights) Train(int maxEpoch, TimeSpan? maxTime, 
            double minError, double precision = TOLERENCE)
        {
            float[] momentum = new float[weights.Length];
            float[] velocity = new float[weights.Length];

            const double beta1 = 0.9;
            const double beta2 = 0.999;
            DateTime start = DateTime.Now;

            Console.WriteLine($"Data size: {positions.Count}, K: {k:F6}, Start time: {start:h\\:mm\\:ss}");
            double currError = MeanSquaredError(k);
            double bestError = currError + TOLERENCE * 2;
            double accuracy = Accuracy();
            int epoch = 0;

            Console.WriteLine($"Epoch {epoch,5} - \u03B5: {currError:F6}, Accuracy {accuracy:F4}");

            while (epoch < maxEpoch && currError > minError && (bestError - currError) >= TOLERENCE && (maxTime == null || DateTime.Now - start < maxTime))
            {
                ComputeGradient();
                
                for (int n = 0; n < weights.Length; n++)
                {
                    double grad = -k * gradient[n] / positions.Count;
                    momentum[n] = (float)(beta1 * momentum[n] + (1.0 - beta1) * grad);
                    velocity[n] = (float)(beta2 * velocity[n] + (1.0 - beta2) * grad * grad);
                    weights[n] -= (float)(lRate * momentum[n] / (1e-8 + Math.Sqrt(velocity[n])));
                }

                if (++epoch % 100 == 0)
                {
                    bestError = currError;
                    currError = MeanSquaredError(k);
                    accuracy = Accuracy();
                    TimeSpan elapsed = DateTime.Now - start;
                    double epochsPerSec = epoch / elapsed.TotalSeconds;
                    Console.WriteLine($"Epoch {epoch, 5} - \u03B5: {currError:F6}, Accuracy {accuracy:F4}, Epoch/sec {epochsPerSec:F3}, elapsed: {elapsed:d\\.hh\\:mm\\:ss}");
                }
            }

            currError = MeanSquaredError(k);
            accuracy = Accuracy();
            short[] nWeights = new short[weights.Length];
            CopyWeights(weights, nWeights);
            return (currError, accuracy, nWeights);
        }

        public override double SolveK(double a = 0.0, double b = 1.0)
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

        private void ComputeGradient()
        {
            ConcurrentBag<float[]> gradients = new();
            Array.Clear(gradient);

            Parallel.For(0, positions.Count, () => new float[gradient.Length], 
                (j, loop, grad) =>
                {
                    UpdateSingleGradient(grad, positions[j]);
                    return grad;
                },
                    gradients.Add
            );

            foreach (var grad in gradients)
            {
                for (int n = 0; n < grad.Length; n++)
                {
                    gradient[n] += grad[n];
                }
            }
        }

        public void UpdateSingleGradient(float[] grad, PosRecord pos)
        {
            double sig = Sigmoid(k, ComputeEval(pos));
            double res = (pos.Result - sig) * sig * (1.0 - sig);
            double mgBase = res * pos.Features.Phase / Constants.MAX_PHASE;
            double egBase = res - mgBase;

            foreach (var kvp in pos.Features.Coefficients)
            {
                grad[kvp.Key] += (float)(mgBase * kvp.Value);
                grad[kvp.Key + ChessWeights.ENDGAME_WEIGHTS] += (float)(egBase * kvp.Value);
            }
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

        private double Accuracy()
        {
            ConcurrentBag<(int correct, int wrong)> subtotals = new();

            Parallel.For(0, positions.Count, () => (correct: 0, wrong: 0), (j, loop, subtotal) =>
            {
                double normalized = Sigmoid(k, ComputeEval(positions[j]));
                float predicted = normalized switch
                {
                    >= 0.00 and <= 0.33 => 0.0f,
                    > 0.33 and < 0.67 => 0.5f,
                    >= 0.67 and <= 1.00 => 1.0f,
                    _ => -1.0f
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

        private static void CopyWeights(short[] src, float[] dst)
        {
            Util.Assert(src.Length == dst.Length);
            for (int n = 0; n < src.Length; n++)
            {
                dst[n] = src[n];
            }
        }

        private static void CopyWeights(float[] src, short[] dst)
        {
            Util.Assert(src.Length == dst.Length);
            for (int n = 0; n < src.Length; n++)
            { 
                dst[n] = (short)Math.Round(src[n]);
            }
        }

        private static float[] ZeroWeights()
        {
            float[] wts = new float[EvalFeatures.FEATURE_SIZE * 2];
            for (Piece pc = Piece.Pawn; pc <= Piece.Queen; pc++)
            {
                int index = EvalFeatures.MATERIAL + (int)pc;
                wts[index] = pc.Value();
                wts[index + EvalFeatures.FEATURE_SIZE] = pc.Value();
            }
            return wts;
        }

        private double ComputeEval(PosRecord p)
        {
            double opening = 0.0, endgame = 0.0;

            foreach (var kvp in p.Features.Coefficients)
            {
                opening += kvp.Value * weights[kvp.Key];
                endgame += kvp.Value * weights[kvp.Key + ChessWeights.ENDGAME_WEIGHTS];
            }

            double phase = p.Features.Phase;
            return (opening * phase + endgame * (Constants.MAX_PHASE - phase)) / Constants.MAX_PHASE;
        }

        private readonly float[] weights;
        private readonly float[] gradient;
        private readonly double lRate = 1.0;
    }
}
