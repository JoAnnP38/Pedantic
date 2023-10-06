using System.Runtime.CompilerServices;
using Pedantic.Chess;

namespace Pedantic.Tuning
{
    public abstract class Tuner
    {
        public const double GOLDEN_RATIO = 1.618033988749894;
        public const double DEFAULT_K = 0.00385;
        public const double TOLERENCE = 1.0e-7;

        protected Tuner(IList<PosRecord> positions, int? seed)
        {
            this.positions = positions;

#if DEBUG
            rand = new Random(1);
#else
            rand = seed.HasValue ? new Random(seed.Value) : new Random();
#endif
        }

        public abstract (double Error, double Accuracy, HceWeights Weights) Train(int maxEpoch, 
            TimeSpan? maxTime, double minError, double precision = TOLERENCE);

        public abstract double SolveK(double a = 0.0, double b = 1.0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static double Sigmoid(double k, double eval)
        {
            return 1.0 / (1.0 + Math.Exp(-k * eval));
        }

        protected double k;
        protected readonly IList<PosRecord> positions;
        protected readonly Random rand;
    }
}
