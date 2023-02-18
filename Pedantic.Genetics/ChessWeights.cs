using Pedantic.Utilities;
using LiteDB;

namespace Pedantic.Genetics
{
    public class ChessWeights : IChromosome<ChessWeights>
    {
        public const double MUTATION_PROBABILITY = 0.0005d;
        public const int MAX_AGE = 5;
        public const int MAX_WEIGHTS = 940;
        public const int MAX_PARENTS = 2;
        public const int PIECE_WEIGHT_LENGTH = 6;
        public const int PIECE_SQUARE_LENGTH = 384;
        public const int OPENING_PHASE_WEIGHT_OFFSET = 0;
        public const int ENDGAME_PHASE_WEIGHT_OFFSET = 1;
        public const int OPENING_MOBILITY_WEIGHT_OFFSET = 2;
        public const int ENDGAME_MOBILITY_WEIGHT_OFFSET = 3;
        public const int OPENING_KING_ATTACK_WEIGHT_OFFSET = 4;
        public const int ENDGAME_KING_ATTACK_WEIGHT_OFFSET = 7;
        public const int OPENING_DEVELOPMENT_WEIGHT_OFFSET = 10;
        public const int ENDGAME_DEVELOPMENT_WEIGHT_OFFSET = 11;
        public const int OPENING_PIECE_WEIGHT_OFFSET = 12;
        public const int ENDGAME_PIECE_WEIGHT_OFFSET = 18;
        public const int OPENING_PIECE_SQUARE_WEIGHT_OFFSET = 24;
        public const int ENDGAME_PIECE_SQUARE_WEIGHT_OFFSET = 408;
        public const int OPENING_BLOCKED_PAWN_OFFSET = 792;
        public const int ENDGAME_BLOCKED_PAWN_OFFSET = 828;
        public const int OPENING_BLOCKED_PAWN_DBL_MOVE_OFFSET = 864;
        public const int ENDGAME_BLOCKED_PAWN_DBL_MOVE_OFFSET = 865;
        public const int OPENING_PINNED_PIECE_OFFSET = 866;
        public const int ENDGAME_PINNED_PIECE_OFFSET = 881;
        public const int OPENING_ISOLATED_PAWN_OFFSET = 896;
        public const int ENDGAME_ISOLATED_PAWN_OFFSET = 897;
        public const int OPENING_BACKWARD_PAWN_OFFSET = 898;
        public const int ENDGAME_BACKWARD_PAWN_OFFSET = 899;
        public const int OPENING_DOUBLED_PAWN_OFFSET = 900;
        public const int ENDGAME_DOUBLED_PAWN_OFFSET = 901;
        public const int OPENING_KNIGHT_OUTPOST_OFFSET = 902;
        public const int ENDGAME_KNIGHT_OUTPOST_OFFSET = 903;
        public const int OPENING_BISHOP_OUTPOST_OFFSET = 904;
        public const int ENDGAME_BISHOP_OUTPOST_OFFSET = 905;
        public const int OPENING_PASSED_PAWNS_OFFSET = 906;
        public const int ENDGAME_PASSED_PAWNS_OFFSET = 912;
        public const int OPENING_ADJACENT_PAWNS_OFFSET = 918;
        public const int ENDGAME_ADJACENT_PAWNS_OFFSET = 924;
        public const int OPENING_BISHOP_PAIR_OFFSET = 930;
        public const int ENDGAME_BISHOP_PAIR_OFFSET = 931;
        public const int OPENING_PAWN_MAJORITY_OFFSET = 932;
        public const int ENDGAME_PAWN_MAJORITY_OFFSET = 933;
        public const int OPENING_KING_NEAR_PASSED_PAWN_OFFSET = 934;
        public const int ENDGAME_KING_NEAR_PASSED_PAWN_OFFSET = 935;
        public const int OPENING_GUARDED_PASSED_PAWN_OFFSET = 936;
        public const int ENDGAME_GUARDED_PASSED_PAWN_OFFSET = 937;
        public const int OPENING_ATTACK_PASSED_PAWN_OFFSET = 938;
        public const int ENDGAME_ATTACK_PASSED_PAWN_OFFSET = 939;

        public readonly struct WeightGenerator
        {
            private readonly int minValue;
            private readonly int maxValue;
            private readonly int multiplier;

            public WeightGenerator(int minValue, int maxValue, int multiplier = 1)
            {
                Util.Assert(minValue > short.MinValue && minValue <= short.MaxValue);
                Util.Assert(maxValue > short.MinValue && maxValue <= short.MaxValue);
                Util.Assert(minValue * multiplier > short.MinValue && minValue * multiplier <= short.MaxValue);
                Util.Assert(maxValue * multiplier > short.MinValue && maxValue * multiplier <= short.MaxValue);
                Util.Assert(maxValue > minValue);
                this.minValue = minValue;
                this.maxValue = maxValue;
                this.multiplier = multiplier;
            }

            public short Next()
            {
                return (short)(Random.Shared.Next(minValue, maxValue) * multiplier);
            }

            public int MinValue => minValue;
            public int MaxValue => maxValue;
            public int Multiplier => multiplier;
        }

        public readonly struct WtInfo
        {
            private readonly string key;
            private readonly int offsetStart;
            private readonly int offsetEnd;
            private readonly int valueStart;
            private readonly int valueEnd;
            private readonly int multiplier;

            public WtInfo(string key, int offsetStart, int offsetEnd, int valueStart, int valueEnd, int multiplier = 1)
            {
                this.key = key;
                this.offsetStart = offsetStart;
                this.offsetEnd = offsetEnd;
                this.valueStart = valueStart;
                this.valueEnd = valueEnd;
                this.multiplier = multiplier;
            }

            public bool InRange(int offset, out bool isLow)
            {
                isLow = IsLow(offset);
                return offset >= offsetStart && offset < offsetEnd;
            }

            public bool IsLow(int offset)
            {
                return offset < offsetStart;
            }

            public bool IsHigh(int offset)
            {
                return offset >= offsetEnd;
            }

            public short NextRandom()
            {
                return (short)(Random.Shared.Next(valueStart, valueEnd) * multiplier);
            }

            public string Key => key;
            public int OffsetStart => offsetStart;
            public int OffsetEnd => offsetEnd;
            public int ValueStart => valueStart;
            public int ValueEnd => valueEnd;
            public int Multiplier => multiplier;

            public static WtInfo Empty = new();
        }

        [BsonCtor]
        public ChessWeights(ObjectId _id, bool isActive, bool isImmortal, int age, ObjectId[] parentIds, int wins,
            int draws, int losses, short[] weights, DateTime updatedOn, DateTime createdOn)
        {
            Id = _id;
            IsActive = isActive;
            IsImmortal = isImmortal;
            Age = age;
            ParentIds = parentIds;
            Wins = wins;
            Draws = draws;
            Losses = losses;
            Weights = weights;
            UpdatedOn = updatedOn;
        }

        public ChessWeights(ChessWeights parent1, ChessWeights parent2, short[] weights)
        {
            Id = ObjectId.NewObjectId();
            IsActive = true;
            IsImmortal = false;
            Age = 0;
            Wins = 0;
            Losses = 0;
            Draws = 0;
            ParentIds = new [] { parent1.Id, parent2.Id };
            Weights = weights;
            UpdatedOn = CreatedOn;
        }

        public ChessWeights(short[] weights)
        {
            Id = ObjectId.NewObjectId();
            IsActive = true;
            IsImmortal = false;
            Age = 0;
            Wins = 0;
            Losses = 0;
            Draws = 0;
            ParentIds = Array.Empty<ObjectId>();
            Weights = weights;
            UpdatedOn = CreatedOn;
        }

        private ChessWeights()
        {
            Id = ObjectId.Empty;
            IsActive = false;
            IsImmortal = false;
            Age = 0;
            Wins = 0;
            Losses = 0;
            Draws = 0;
            ParentIds = Array.Empty<ObjectId>();
            Weights = Array.Empty<short>();
            UpdatedOn = CreatedOn;
        }

        public ObjectId Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsImmortal { get; set; }
        public int Age { get; set; }
        public ObjectId[] ParentIds { get; init; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public short[] Weights { get; init; }
        public DateTime UpdatedOn { get; set; }
        public DateTime CreatedOn => Id.CreationTime;

        [BsonIgnore]
        public double Score => GamesPlayed == 0 ? 0.0d : (double)(Wins * 2 + Draws) / (GamesPlayed);

        [BsonIgnore]
        public int GamesPlayed => Wins + Draws + Losses;

        public double PercentChanged(ChessWeights other)
        {
            double totalChange = 0.0d;

            for (int n = 0; n < MAX_WEIGHTS; ++n)
            { 
                totalChange += (double)Math.Abs(other.Weights[n] - Weights[n]) / Math.Abs((wtGens[n].MaxValue - wtGens[n].MinValue) * wtGens[n].Multiplier);
            }

            return (totalChange / MAX_WEIGHTS) * 100.0;
        }

        public static ChessWeights Empty { get; } = new ChessWeights();

        public static (ChessWeights child1, ChessWeights child2) CrossOver(ChessWeights parent1, ChessWeights parent2, bool checkMutation)
        {
            double probability = (parent1.Age + parent2.Age + 1) * MUTATION_PROBABILITY;
            probability = Math.Max(Math.Min(probability, MUTATION_PROBABILITY), 0.05);
            short[] weights1 = new short[MAX_WEIGHTS];
            short[] weights2 = new short[MAX_WEIGHTS];

            for (int n = 0; n < MAX_WEIGHTS; ++n)
            {
                if (rand.NextDouble() <= 0.5d)
                {
                    weights1[n] = Mutate(checkMutation, n, parent1.Weights[n], probability);
                    weights2[n] = Mutate(checkMutation, n, parent2.Weights[n], probability);
                }
                else
                {
                    weights1[n] = Mutate(checkMutation, n, parent2.Weights[n], probability);
                    weights2[n] = Mutate(checkMutation, n, parent1.Weights[n], probability);
                }
            }

            return (new ChessWeights(parent1, parent2, weights1), new ChessWeights(parent1, parent2, weights2));
        }

        public static short Mutate(bool checkMutation, int nWeight, short weight, double probability)
        {
            if (checkMutation && rand.NextDouble() <= probability)
            {
                return wtGens[nWeight].Next();
            }

            return weight;
        }

        public static short NextWeight(int nWeight)
        {
            return wtGens[nWeight].Next();
        }

        public static ChessWeights CreateRandom()
        {
            short[] weights = new short[MAX_WEIGHTS];
            for (int n = 0; n < MAX_WEIGHTS; ++n)
            {
                weights[n] = NextWeight(n);
            }

            return new ChessWeights(weights);
        }

        public static ChessWeights CreateNormal(double[] mean, double[] sigma, bool checkMutation = true)
        {
            short[] weights = new short[MAX_WEIGHTS];
            for (int n = 0; n < MAX_WEIGHTS; ++n)
            {
                short weight = (short)(rand.NextGaussian(mean[n], sigma[n]) + 0.5);
                short scale = (short)(weight / wtGens[n].Multiplier);
                scale = Math.Min(Math.Max(scale, (short)wtGens[n].MinValue), (short)(wtGens[n].MaxValue - 1));
                weight = (short)(scale * wtGens[n].Multiplier);
                weights[n] = Mutate(checkMutation, n, weight, MUTATION_PROBABILITY);
            }

            return new ChessWeights(weights);
        }

        public static ChessWeights CreateParagon()
        {
            ChessWeights paragon = new(paragonWeights)
            {
                IsImmortal = true
            };
            return paragon;
        }

        public static bool LoadParagon(out ChessWeights paragon)
        {
            using var rep = new GeneticsRepository(true);
            ChessWeights? p = rep.Weights.FindOne(w => w.IsActive && w.IsImmortal);
            paragon = p ?? ChessWeights.Empty;
            return p != null;
        }

        public static void CalculateStatistics(GeneticsRepository rep, out double[] mean, out double[] sigma)
        {
            ChessWeights[] pop = rep.Weights
                .Find(w => (w.Wins + w.Draws + w.Losses) > 0)
                .OrderByDescending(w => (double)(w.Wins * 2 + w.Draws) / (w.Wins + w.Draws + w.Losses))
                .Take(128)
                .ToArray();

            if (pop.Length == 128)
            {
                mean = new double[MAX_WEIGHTS];
                sigma = new double[MAX_WEIGHTS];

                for (int n = 0; n < MAX_WEIGHTS; n++)
                {
                    double avg = pop.Average(v => (double)v.Weights[n]);
                    double sumOfSquares = pop.Sum(v => (v.Weights[n] - avg) * (v.Weights[n] - avg));
                    mean[n] = avg;
                    sigma[n] = Math.Sqrt(sumOfSquares / pop.Length - 1);
                }
            }
            else
            {
                mean = Array.Empty<double>();
                sigma = Array.Empty<double>();
            }
        }

        /*
        private static bool TryLookup(int index, out WtInfo wtInfo)
        {
            int length = wtInfos.Length;
            int low = 0;
            int high = length - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (wtInfos[mid].InRange(index, out bool isLow))
                {
                    wtInfo = wtInfos[mid];
                    return true;
                }
                if (isLow)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }
            
            wtInfo = WtInfo.Empty;
            return false;
        }
        */

        private static readonly Random rand = new();

        private static readonly short[] paragonWeights =
        {
            /* OpeningPhaseThruMove */
            10,

            /* EndGamePhaseMaterial */
            3900,

            /* OpeningMobilityWeight */
            3,

            /* EndGameMobilityWeight */
            3,

            /* OpeningKingSafetyWeight */
            8, // attacks to squares adjacent to king
            4, // attacks to squares 2 squares from king
            2, // attacks to squares 3 squares from king

            /* EndGameKingSafetyWeight */
            8, // attacks to squares adjacent to king
            4, // attacks to squares 2 squares from king
            2, // attacks to squares 3 squares from king

            /* OpeningDevelopmentWeight */
            2,

            /* EndGameDevelopmentWeight */
            0,

            /* opening piece values */
            82, 337, 365, 477, 1025, 0,

            /* end game piece values */
            94, 281, 297, 512, 936, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns */
              0,   0,   0,   0,   0,   0,  0,   0,
            -35,  -1, -20, -23, -15,  24, 38, -22,
            -26,  -4,  -4, -10,   3,   3, 33, -12,
            -27,  -2,  -5,  12,  17,   6, 10, -25,
            -14,  13,   6,  21,  23,  12, 17, -23,
             -6,   7,  26,  31,  65,  56, 25, -20,
             98, 134,  61,  95,  68, 126, 34, -11,
              0,   0,   0,   0,   0,   0,  0,   0,

            /* knights */
            -105, -21, -58, -33, -17, -28, -19,  -23,
             -29, -53, -12,  -3,  -1,  18, -14,  -19,
             -23,  -9,  12,  10,  19,  17,  25,  -16,
             -13,   4,  16,  13,  28,  19,  21,   -8,
              -9,  17,  19,  53,  37,  69,  18,   22,
             -47,  60,  37,  65,  84, 129,  73,   44,
             -73, -41,  72,  36,  23,  62,   7,  -17,
            -167, -89, -34, -49,  61, -97, -15, -107,

            /* bishops */
            -33, -3, -14, -21, -13, -12, -39, -21,
              4, 15,  16,   0,   7,  21,  33,   1,
              0, 15,  15,  15,  14,  27,  18,  10,
             -6, 13,  13,  26,  34,  12,  10,   4,
             -4,  5,  19,  50,  37,  37,   7,  -2,
            -16, 37,  43,  40,  35,  50,  37,  -2,
            -26, 16, -18, -13,  30,  59,  18, -47,
            -29,  4, -82, -37, -25, -42,   7,  -8,

            /* rooks */
            -19, -13,   1,  17, 16,  7, -37, -26,
            -44, -16, -20,  -9, -1, 11,  -6, -71,
            -45, -25, -16, -17,  3,  0,  -5, -33,
            -36, -26, -12,  -1,  9, -7,   6, -23,
            -24, -11,   7,  26, 24, 35,  -8, -20,
             -5,  19,  26,  36, 17, 45,  61,  16,
             27,  32,  58,  62, 80, 67,  26,  44,
             32,  42,  32,  51, 63,  9,  31,  43,

            /* queens */
             -1, -18,  -9,  10, -15, -25, -31, -50,
            -35,  -8,  11,   2,   8,  15,  -3,   1,
            -14,   2, -11,  -2,  -5,   2,  14,   5,
             -9, -26,  -9, -10,  -2,  -4,   3,  -3,
            -27, -27, -16, -16,  -1,  17,  -2,   1,
            -13, -17,   7,   8,  29,  56,  47,  57,
            -24, -39,  -5,   1, -16,  57,  28,  54,
            -28,   0,  29,  12,  59,  44,  43,  45,

            /* kings */
            -15,  36,  12, -54,   8, -28,  24,  14,
              1,   7,  -8, -64, -43, -16,   9,   8,
            -14, -14, -22, -46, -44, -30, -15, -27,
            -49,  -1, -27, -39, -46, -44, -33, -51,
            -17, -20, -12, -27, -30, -25, -14, -36,
             -9,  24,   2, -16, -20,   6,  22, -22,
             29,  -1, -20,  -7,  -8,  -4, -38, -29,
            -65,  23,  16, -15, -56, -34,   2,  13,

            #endregion

            /* end game piece square values */

            #region end game piece square values

            /* pawns */
              0,   0,   0,   0,   0,   0,   0,   0,
             13,   8,   8,  10,  13,   0,   2,  -7,
              4,   7,  -6,   1,   0,  -5,  -1,  -8,
             13,   9,  -3,  -7,  -7,  -8,   3,  -1,
             32,  24,  13,   5,  -2,   4,  17,  17,
             94, 100,  85,  67,  56,  53,  82,  84,
            178, 173, 158, 134, 147, 132, 165, 187,
              0,   0,   0,   0,   0,   0,   0,   0,

            /* knights */
            -29, -51, -23, -15, -22, -18, -50, -64,
            -42, -20, -10,  -5,  -2, -20, -23, -44,
            -23,  -3,  -1,  15,  10,  -3, -20, -22,
            -18,  -6,  16,  25,  16,  17,   4, -18,
            -17,   3,  22,  22,  22,  11,   8, -18,
            -24, -20,  10,   9,  -1,  -9, -19, -41,
            -25,  -8, -25,  -2,  -9, -25, -24, -52,
            -58, -38, -13, -28, -31, -27, -63, -99,

            /* bishops */
            -23,  -9, -23,  -5, -9, -16,  -5, -17,
            -14, -18,  -7,  -1,  4,  -9, -15, -27,
            -12,  -3,   8,  10, 13,   3,  -7, -15,
             -6,   3,  13,  19,  7,  10,  -3,  -9,
             -3,   9,  12,   9, 14,  10,   3,   2,
              2,  -8,   0,  -1, -2,   6,   0,   4,
             -8,  -4,   7, -12, -3, -13,  -4, -14,
            -14, -21, -11,  -8, -7,  -9, -17, -24,

            /* rooks */
            -9,  2,  3, -1, -5, -13,   4, -20,
            -6, -6,  0,  2, -9,  -9, -11,  -3,
            -4,  0, -5, -1, -7, -12,  -8, -16,
             3,  5,  8,  4, -5,  -6,  -8, -11,
             4,  3, 13,  1,  2,   1,  -1,   2,
             7,  7,  7,  5,  4,  -3,  -5,  -3,
            11, 13, 13, 11, -3,   3,   8,   3,
            13, 10, 18, 15, 12,  12,   8,   5,

            /* queens */
            -33, -28, -22, -43,  -5, -32, -20, -41,
            -22, -23, -30, -16, -16, -23, -36, -32,
            -16, -27,  15,   6,   9,  17,  10,   5,
            -18,  28,  19,  47,  31,  34,  39,  23,
              3,  22,  24,  45,  57,  40,  57,  36,
            -20,   6,   9,  49,  47,  35,  19,   9,
            -17,  20,  32,  41,  58,  25,  30,   0,
             -9,  22,  22,  27,  27,  19,  10,  20,

            /* kings */
            -53, -34, -21, -11, -28, -14, -24, -43,
            -27, -11,   4,  13,  14,   4,  -5, -17,
            -19,  -3,  11,  21,  23,  16,   7,  -9,
            -18,  -4,  21,  24,  27,  23,   9, -11,
             -8,  22,  24,  27,  26,  33,  26,   3,
             10,  17,  23,  15,  20,  45,  44,  13,
            -12,  17,  14,  17,  17,  38,  23,  11,
            -74, -35, -18, -18, -11,  15,   4, -17,

            #endregion

            #region opening pawn blocked tables

            /* pawn blocked by pawn */
            -5, // rank 2
            -5, // rank 3
            -10, // rank 4
            -10, // rank 5
            -15, // rank 6
            0, // rank 7

            /* pawn blocked by knight */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -40, // rank 7

            /* pawn blocked by bishop */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -40, // rank 7

            /* pawn blocked by rook */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -20, // rank 7

            /* pawn blocked by queen */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -20, // rank 7

            /* pawn blocked by king */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -10, // rank 7

            #endregion

            #region end game pawn blocked tables

            /* pawn blocked by pawn */
            -5, // rank 2
            -5, // rank 3
            -10, // rank 4
            -10, // rank 5
            -15, // rank 6
            0, // rank 7

            /* pawn blocked by knight */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -40, // rank 7

            /* pawn blocked by bishop */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -40, // rank 7

            /* pawn blocked by rook */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -20, // rank 7

            /* pawn blocked by queen */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -20, // rank 7

            /* pawn blocked by king */
            0, // rank 2
            0, // rank 3
            0, // rank 4
            0, // rank 5
            0, // rank 6
            -10, // rank 7

            #endregion

            /* opening pawn double move blocked */
            -5,

            /* end game pawn double move blocked */
            -5,

            #region opening pinned piece tables

            /* opening pinned piece tables */
            /* pins */
            2, // bishop pins pawn
            4, // bishop pins knight
            0, // bishop pins bishop
            8, // bishop pins rook
            12, // bishop pins queen

            2, // rook pins pawn
            4, // rook pins knight
            4, // rook pins bishop
            0, // rook pins rook
            12, // rook pins queen

            2, // queen pins pawn
            4, // queen pins knight
            0, // queen pins bishop
            0, // queen pins rook
            0, // queen pins queen

            #endregion

            #region end game pinned piece tables

            /* end game pinned piece tables */
            /* pins */
            2, // bishop pins pawn
            4, // bishop pins knight
            0, // bishop pins bishop
            8, // bishop pins rook
            12, // bishop pins queen

            2, // rook pins pawn
            4, // rook pins knight
            4, // rook pins bishop
            0, // rook pins rook
            12, // rook pins queen

            2, // queen pins pawn
            4, // queen pins knight
            0, // queen pins bishop
            0, // queen pins rook
            0, // queen pins queen

            #endregion


            /* opening isolated pawns */
            -10,

            /* end game isolated pawns */
            -10,

            /* opening backward pawn */
            -5,

            /* end game backward pawn */
            -5,

            /* opening doubled pawn */
            -5,

            /* end game doubled pawn */
            -5,

            /* opening knight on outpost */
            5,

            /* endgame knight on outpost */
            5,

            /* opening bishop on outpost */
            2,

            /* end game bishop on outpost */
            2,

            /* opening passed pawns table */

            #region opening passed pawns table

            /* passed pawns */
            5, // rank 2
            5, // rank 3
            5, // rank 4
            25, // rank 5
            25, // rank 6
            100, // rank 7

            #endregion

            /* end game passed pawns table */

                #region end game passed pawns table

                /* passed pawns */
                5, // rank 2
                10, // rank 3
                20, // rank 4
                40, // rank 5
                80, // rank 6
                160, // rank 7

                #endregion

            /* opening adjacent pawns table */

            #region opening adjacent pawns table

            /* adjacent pawns */
            2, // rank 2
            2, // rank 3
            5, // rank 4
            5, // rank 5
            10, // rank 6
            20, // rank 7

            #endregion

            /* end game adjacent pawns table */

            #region end game adjacent pawns table

            /* adjacent pawns */
            2, // rank 2
            2, // rank 3
            5, // rank 4
            5, // rank 5
            10, // rank 6
            20, // rank 7

            #endregion

            /* opening bishop pair (vs 2 knights or bishop and knight) */
            20,

            /* end game bishop pair */
            20,

            /* opening pawn majority bonus */
            5,

            /* end game pawn majority bonus */
            10,

            /* opening king near passed pawn (inside its square) */
            0, // not used

            /* endgame king near passed pawn (inside its square) */
            10, // not used

            /* opening guarded passed pawn */
            0, // not used

            /* endgame guarded passed pawn */
            10, // not used

            /* opening closest passed pawn */
            10, // not used

            /* endgame closest passed pawn */
            20 // not used
        };

        private static readonly WeightGenerator[] wtGens =
        {
            #region generator objects
            new(8, 21),
            new(25, 70, 100),
            new(1, 21),
            new(1, 21),
            new(4, 17), new(2, 9), new(1, 5),
            new(4, 17), new(2, 9), new(1, 5),
            new(1, 11), new(0, 5),
            new(80, 121), new(192, 481), new(240, 481), new(360, 841), new(632, 1621), new(0, 1),
            new(80, 121), new(192, 481), new(240, 481), new(360, 841), new(632, 1621), new(0, 1),
            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(-8, 2, 5),   new(0, 3, 5),    new(-5, 3, 5),   new(-10, -1, 5),
            new(-10, -1, 5), new(0, 7, 5),    new(0, 10, 5),   new(-5, 2, 5),
            new(-6, 2, 5),   new(-1, 1, 5),   new(-2, 2, 5),   new(-2, 2, 5),
            new(-2, 2, 5),   new(-2, 2, 5),   new(-1, 9, 5),   new(-3, 2, 5),
            new(-6, 1, 5),   new(0, 2, 5),    new(-1, 2, 5),   new(1, 6, 5),
            new(1, 6, 5),    new(0, 2, 5),    new(0, 3, 5),    new(-6, 1, 5),
            new(-3, 2, 5),   new(0, 4, 5),    new(0, 3, 5),    new(1, 7, 5),
            new(1, 7, 5),    new(0, 4, 5),    new(0, 5, 5),    new(-6, 2, 5),
            new(-1, 3, 5),   new(1, 3, 5),    new(1, 7, 5),    new(3, 8, 5),
            new(3, 17, 5),   new(1, 14, 5),   new(1, 7, 5),    new(-5, 3, 5),
            new(0, 25, 5),   new(1, 33, 5),   new(3, 25, 5),   new(4, 25, 5),
            new(4, 25, 5),   new(3, 31, 5),   new(1, 25, 5),   new(-3, 25, 5),
            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(-25, 0, 5),  new(-10, -3, 5), new(-14, 0, 5),  new(-8, 0, 5),
            new(-7, 0, 5),   new(-7, 0, 5),   new(-10, -3, 5), new(-12, 0, 5),
            new(-10, 0, 5),  new(-13, 1, 5),  new(-3, 1, 5),   new(-1, 2, 5),
            new(0, 2, 5),    new(0, 5, 5),    new(-5, 1, 5),   new(-10, 0, 5),
            new(-7, 0, 5),   new(-2, 2, 5),   new(0, 4, 5),    new(0, 5, 5),
            new(0, 6, 5),    new(0, 5, 5),    new(0, 7, 5),    new(-7, 0, 5),
            new(-7, 1, 5),   new(0, 2, 5),    new(0, 5, 5),    new(1, 6, 5),
            new(1, 8, 5),    new(0, 6, 5),    new(0, 6, 5),    new(-7, 1, 5),
            new(-7, 1, 5),   new(0, 5, 5),    new(0, 6, 5),    new(1, 14, 5),
            new(1, 10, 5),   new(0, 18, 5),   new(0, 5, 5),    new(-7, 6, 5),
            new(-11, 0, 5),  new(0, 15, 5),   new(0, 10, 5),   new(0, 17, 5),
            new(0, 21, 5),   new(0, 32, 5),   new(0, 19, 5),   new(-7, 12, 5),
            new(-18, 0, 5),  new(-10, 1, 5),  new(0, 18, 5),   new(0, 10, 5),
            new(0, 7, 5),    new(0, 16, 5),   new(-5, 3, 5),   new(-10, 0, 5),
            new(-40, 0, 5),  new(-21, 0, 5),  new(-8, 0, 5),   new(-12, 1, 5),
            new(-7, 16, 5),  new(-23, 0, 5),  new(-10, 0, 5),  new(-36, 0, 5),
            new(-8, 0, 5),   new(-2, 1, 5),   new(-5, 0, 5),   new(-5, 0, 5),
            new(-3, 0, 5),   new(-5, 0, 5),   new(-9, 0, 5),   new(-5, 0, 5),
            new(-2, 2, 5),   new(0, 5, 5),    new(0, 5, 5),    new(0, 2, 5),
            new(0, 3, 5),    new(0, 6, 5),    new(0, 9, 5),    new(-2, 1, 5),
            new(-2, 1, 5),   new(0, 5, 5),    new(0, 5, 5),    new(0, 5, 5),
            new(0, 4, 5),    new(0, 7, 5),    new(0, 5, 5),    new(-2, 3, 5),
            new(-2, 1, 5),   new(0, 4, 5),    new(0, 4, 5),    new(1, 7, 5),
            new(1, 9, 5),    new(0, 4, 5),    new(0, 3, 5),    new(-2, 2, 5),
            new(-2, 1, 5),   new(0, 2, 5),    new(0, 6, 5),    new(1, 13, 5),
            new(1, 10, 5),   new(0, 10, 5),   new(0, 3, 5),    new(-2, 1, 5),
            new(-4, 1, 5),   new(0, 10, 5),   new(0, 11, 5),   new(0, 11, 5),
            new(0, 9, 5),    new(0, 13, 5),   new(0, 10, 5),   new(-2, 1, 5),
            new(-6, 0, 5),   new(0, 5, 5),    new(-4, 2, 5),   new(-3, 2, 5),
            new(0, 8, 5),    new(0, 15, 5),   new(0, 5, 5),    new(-11, 0, 5),
            new(-7, 0, 5),   new(-2, 2, 5),   new(-20, 0, 5),  new(-9, 0, 5),
            new(-6, 0, 5),   new(-10, 0, 5),  new(-2, 3, 5),   new(-5, 0, 5),
            new(-5, 2, 5),   new(-3, 2, 5),   new(0, 2, 5),    new(0, 5, 5),
            new(0, 5, 5),    new(0, 3, 5),    new(-9, 2, 5),   new(-6, 2, 5),
            new(-11, 1, 5),  new(-4, 1, 5),   new(-5, 1, 5),   new(-2, 1, 5),
            new(0, 1, 5),    new(0, 4, 5),    new(-1, 1, 5),   new(-17, 1, 5),
            new(-11, 1, 5),  new(-6, 1, 5),   new(-4, 1, 5),   new(-4, 1, 5),
            new(0, 2, 5),    new(0, 1, 5),    new(-1, 1, 5),   new(-8, 1, 5),
            new(-9, 1, 5),   new(-6, 1, 5),   new(-3, 1, 5),   new(0, 1, 5),
            new(0, 3, 5),    new(-2, 1, 5),   new(0, 2, 5),    new(-6, 1, 5),
            new(-6, 1, 5),   new(-3, 1, 5),   new(0, 3, 5),    new(0, 7, 5),
            new(0, 7, 5),    new(0, 9, 5),    new(-2, 1, 5),   new(-5, 1, 5),
            new(-1, 1, 5),   new(0, 6, 5),    new(0, 7, 5),    new(0, 10, 5),
            new(0, 5, 5),    new(0, 12, 5),   new(0, 16, 5),   new(-1, 5, 5),
            new(0, 7, 5),    new(0, 9, 5),    new(0, 15, 5),   new(0, 16, 5),
            new(0, 20, 5),   new(0, 17, 5),   new(0, 7, 5),    new(0, 12, 5),
            new(0, 9, 5),    new(0, 11, 5),   new(0, 9, 5),    new(0, 13, 5),
            new(0, 16, 5),   new(0, 3, 5),    new(0, 8, 5),    new(0, 11, 5),
            new(-5, 1, 5),   new(-4, 1, 5),   new(-2, 1, 5),   new(-1, 3, 5),
            new(-4, 1, 5),   new(-6, 1, 5),   new(-7, 1, 5),   new(-12, 1, 5),
            new(-8, 1, 5),   new(-2, 1, 5),   new(0, 4, 5),    new(0, 1, 5),
            new(0, 3, 5),    new(0, 5, 5),    new(-1, 1, 5),   new(-2, 1, 5),
            new(-3, 1, 5),   new(0, 2, 5),    new(-3, 2, 5),   new(0, 2, 5),
            new(-1, 2, 5),   new(0, 2, 5),    new(0, 4, 5),    new(-2, 2, 5),
            new(-2, 1, 5),   new(-6, 1, 5),   new(-2, 2, 5),   new(-2, 2, 5),
            new(0, 2, 5),    new(-1, 2, 5),   new(0, 2, 5),    new(-1, 1, 5),
            new(-6, 1, 5),   new(-6, 1, 5),   new(-4, 2, 5),   new(-4, 2, 5),
            new(0, 2, 5),    new(0, 5, 5),    new(0, 1, 5),    new(-1, 1, 5),
            new(-3, 1, 5),   new(-4, 1, 5),   new(0, 3, 5),    new(0, 3, 5),
            new(0, 8, 5),    new(0, 14, 5),   new(0, 12, 5),   new(-2, 15, 5),
            new(-6, 1, 5),   new(-9, 1, 5),   new(-1, 1, 5),   new(0, 1, 5),
            new(-4, 1, 5),   new(0, 15, 5),   new(0, 8, 5),    new(-2, 14, 5),
            new(-7, 1, 5),   new(-2, 1, 5),   new(-2, 8, 5),   new(-1, 4, 5),
            new(-1, 15, 5),  new(-2, 12, 5),  new(-2, 11, 5),  new(-5, 12, 5),
            new(-4, 6, 5),   new(4, 10, 5),   new(1, 11, 5),   new(-13, 1, 5),
            new(0, 3, 5),    new(-14, 3, 5),  new(4, 11, 5),   new(2, 6, 5),
            new(-5, 6, 5),   new(-5, 6, 5),   new(-6, 1, 5),   new(-15, 1, 5),
            new(-10, 1, 5),  new(-11, 1, 5),  new(-5, 6, 5),   new(-5, 6, 5),
            new(-12, 0, 5),  new(-12, -1, 5), new(-12, -3, 5), new(-12, -3, 5),
            new(-12, -3, 5), new(-12, -3, 5), new(-12, -2, 5), new(-12, 0, 5),
            new(-12, -3, 5), new(-12, 1, 5),  new(-12, -4, 5), new(-12, -7, 5),
            new(-12, -8, 5), new(-12, -5, 5), new(-12, -5, 5), new(-12, -3, 5),
            new(-12, -2, 5), new(-12, -3, 5), new(-12, -1, 5), new(-12, -4, 5),
            new(-12, -5, 5), new(-12, -4, 5), new(-12, -1, 5), new(-12, -5, 5),
            new(-12, 0, 5),  new(-12, 7, 5),  new(-12, 1, 5),  new(-12, -2, 5),
            new(-12, -3, 5), new(-12, 2, 5),  new(-12, 6, 5),  new(-12, -3, 5),
            new(-12, 8, 5),  new(-12, 1, 5),  new(-12, -3, 5), new(-12, 0, 5),
            new(-12, 0, 5),  new(-12, 1, 5),  new(-12, -7, 5), new(-12, -5, 5),
            new(-16, -5, 5), new(-12, 7, 5),  new(-12, 5, 5),  new(-12, -2, 5),
            new(-13, -8, 5), new(-12, -6, 5), new(-12, 1, 5),  new(-12, 4, 5),

            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(0, 4, 5),    new(0, 3, 5),    new(0, 3, 5),    new(0, 3, 5),
            new(0, 4, 5),    new(0, 1, 5),    new(0, 1, 5),    new(-2, 1, 5),
            new(0, 2, 5),    new(0, 3, 5),    new(-1, 1, 5),   new(0, 1, 5),
            new(0, 1, 5),    new(-1, 1, 5),   new(0, 1, 5),    new(-2, 1, 5),
            new(0, 4, 5),    new(0, 3, 5),    new(-1, 1, 5),   new(-2, 1, 5),
            new(-2, 1, 5),   new(-2, 1, 5),   new(0, 2, 5),    new(0, 1, 5),
            new(0, 9, 5),    new(0, 7, 5),    new(0, 4, 5),    new(0, 2, 5),
            new(0, 1, 5),    new(0, 2, 5),    new(0, 5, 5),    new(0, 5, 5),
            new(0, 24, 5),   new(0, 25, 5),   new(0, 21, 5),   new(0, 17, 5),
            new(0, 14, 5),   new(0, 14, 5),   new(0, 21, 5),   new(0, 21, 5),
            new(0, 44, 5),   new(0, 43, 5),   new(0, 39, 5),   new(0, 33, 5),
            new(0, 36, 5),   new(0, 33, 5),   new(0, 41, 5),   new(0, 46, 5),
            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(-7, 1, 5),   new(-12, 1, 5),  new(-6, 1, 5),   new(-4, 1, 5),
            new(-5, 1, 5),   new(-4, 1, 5),   new(-12, 1, 5),  new(-15, 1, 5),
            new(-10, 1, 5),  new(-5, 1, 5),   new(-2, 1, 5),   new(-1, 1, 5),
            new(0, 1, 5),    new(-5, 1, 5),   new(-6, 1, 5),   new(-11, 1, 5),
            new(-6, 1, 5),   new(-1, 1, 5),   new(0, 1, 5),    new(0, 5, 5),
            new(0, 3, 5),    new(-1, 1, 5),   new(-5, 1, 5),   new(-5, 1, 5),
            new(-4, 1, 5),   new(-1, 1, 5),   new(0, 5, 5),    new(0, 7, 5),
            new(0, 5, 5),    new(0, 5, 5),    new(0, 2, 5),    new(-4, 1, 5),
            new(-4, 1, 5),   new(0, 2, 5),    new(0, 6, 5),    new(0, 6, 5),
            new(0, 6, 5),    new(0, 4, 5),    new(0, 3, 5),    new(-4, 1, 5),
            new(-6, 1, 5),   new(-5, 1, 5),   new(0, 3, 5),    new(0, 3, 5),
            new(0, 1, 5),    new(-2, 1, 5),   new(-5, 1, 5),   new(-10, 1, 5),
            new(-6, 1, 5),   new(-2, 1, 5),   new(-6, 1, 5),   new(0, 1, 5),
            new(-2, 1, 5),   new(-6, 1, 5),   new(-6, 1, 5),   new(-12, 1, 5),
            new(-14, 1, 5),  new(-9, 1, 5),   new(-3, 1, 5),   new(-7, 1, 5),
            new(-7, 1, 5),   new(-6, 1, 5),   new(-15, 1, 5),  new(-24, 1, 5),
            new(-6, 1, 5),   new(-2, 1, 5),   new(-6, 1, 5),   new(-1, 1, 5),
            new(-2, 1, 5),   new(-4, 1, 5),   new(-1, 1, 5),   new(-4, 1, 5),
            new(-3, 1, 5),   new(-4, 1, 5),   new(-2, 1, 5),   new(0, 1, 5),
            new(0, 2, 5),    new(-2, 1, 5),   new(-4, 1, 5),   new(-6, 1, 5),
            new(-3, 1, 5),   new(-1, 1, 5),   new(0, 3, 5),    new(0, 3, 5),
            new(0, 4, 5),    new(0, 2, 5),    new(-2, 1, 5),   new(-4, 1, 5),
            new(-1, 1, 5),   new(0, 2, 5),    new(0, 4, 5),    new(0, 6, 5),
            new(0, 3, 5),    new(0, 3, 5),    new(-1, 1, 5),   new(-2, 1, 5),
            new(-1, 1, 5),   new(0, 3, 5),    new(0, 4, 5),    new(0, 3, 5),
            new(0, 4, 5),    new(0, 3, 5),    new(0, 2, 5),    new(0, 1, 5),
            new(0, 1, 5),    new(-2, 1, 5),   new(0, 1, 5),    new(0, 1, 5),
            new(0, 1, 5),    new(0, 2, 5),    new(0, 1, 5),    new(0, 2, 5),
            new(-2, 1, 5),   new(-1, 1, 5),   new(0, 3, 5),    new(-3, 1, 5),
            new(-1, 1, 5),   new(-3, 1, 5),   new(-1, 1, 5),   new(-3, 1, 5),
            new(-3, 1, 5),   new(-5, 1, 5),   new(-3, 1, 5),   new(-2, 1, 5),
            new(-2, 1, 5),   new(-2, 1, 5),   new(-4, 1, 5),   new(-6, 1, 5),
            new(-2, 1, 5),   new(0, 1, 5),    new(0, 2, 5),    new(0, 1, 5),
            new(-1, 1, 5),   new(-3, 1, 5),   new(0, 2, 5),    new(-5, 1, 5),
            new(-1, 1, 5),   new(-1, 1, 5),   new(0, 1, 5),    new(0, 1, 5),
            new(-2, 1, 5),   new(-2, 1, 5),   new(-3, 1, 5),   new(-1, 1, 5),
            new(-1, 1, 5),   new(0, 1, 5),    new(-1, 1, 5),   new(0, 1, 5),
            new(-2, 1, 5),   new(-3, 1, 5),   new(-2, 1, 5),   new(-4, 1, 5),
            new(0, 2, 5),    new(0, 2, 5),    new(0, 3, 5),    new(0, 2, 5),
            new(-1, 1, 5),   new(-1, 1, 5),   new(-2, 1, 5),   new(-3, 1, 5),
            new(0, 2, 5),    new(0, 2, 5),    new(0, 4, 5),    new(0, 1, 5),
            new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),    new(0, 1, 5),
            new(0, 3, 5),    new(0, 3, 5),    new(0, 3, 5),    new(0, 2, 5),
            new(0, 2, 5),    new(-1, 1, 5),   new(-1, 1, 5),   new(-1, 1, 5),
            new(0, 4, 5),    new(0, 4, 5),    new(0, 4, 5),    new(0, 4, 5),
            new(-1, 1, 5),   new(0, 2, 5),    new(0, 3, 5),    new(0, 2, 5),
            new(0, 4, 5),    new(0, 3, 5),    new(0, 5, 5),    new(0, 5, 5),
            new(0, 4, 5),    new(0, 4, 5),    new(0, 3, 5),    new(0, 2, 5),
            new(-8, 1, 5),   new(-7, 1, 5),   new(-5, 1, 5),   new(-10, 1, 5),
            new(-1, 1, 5),   new(-8, 1, 5),   new(-5, 1, 5),   new(-10, 1, 5),
            new(-5, 1, 5),   new(-6, 1, 5),   new(-7, 1, 5),   new(-4, 1, 5),
            new(-4, 1, 5),   new(-6, 1, 5),   new(-9, 1, 5),   new(-8, 1, 5),
            new(-4, 1, 5),   new(-6, 1, 5),   new(0, 5, 5),    new(0, 2, 5),
            new(0, 3, 5),    new(0, 5, 5),    new(0, 3, 5),    new(0, 2, 5),
            new(-4, 1, 5),   new(0, 8, 5),    new(0, 6, 5),    new(0, 12, 5),
            new(0, 8, 5),    new(0, 9, 5),    new(0, 10, 5),   new(0, 7, 5),
            new(0, 2, 5),    new(0, 6, 5),    new(0, 7, 5),    new(0, 12, 5),
            new(0, 15, 5),   new(0, 11, 5),   new(0, 15, 5),   new(0, 10, 5),
            new(-5, 1, 5),   new(0, 2, 5),    new(0, 3, 5),    new(0, 13, 5),
            new(0, 12, 5),   new(0, 9, 5),    new(0, 6, 5),    new(0, 3, 5),
            new(-4, 1, 5),   new(0, 6, 5),    new(0, 9, 5),    new(0, 11, 5),
            new(0, 15, 5),   new(0, 7, 5),    new(0, 8, 5),    new(0, 1, 5),
            new(-2, 1, 5),   new(0, 6, 5),    new(0, 6, 5),    new(0, 7, 5),
            new(0, 7, 5),    new(0, 6, 5),    new(0, 3, 5),    new(0, 6, 5),
            new(-17, 1, 5),  new(-14, 3, 5),  new(-12, 6, 5),  new(-10, 8, 5),
            new(-10, 8, 5),  new(-12, 6, 5),  new(-14, 3, 5),  new(-17, 1, 5),
            new(-14, 3, 5),  new(-12, 6, 5),  new(-7, 8, 5),   new(-5, 11, 5),
            new(-5, 11, 5),  new(-7, 8, 5),   new(-12, 6, 5),  new(-14, 3, 5),
            new(-12, 6, 5),  new(-7, 8, 5),   new(0, 11, 5),   new(0, 13, 5),
            new(0, 13, 5),   new(0, 11, 5),   new(-7, 8, 5),   new(-12, 6, 5),
            new(-10, 8, 5),  new(-5, 11, 5),  new(0, 13, 5),   new(0, 15, 5),
            new(0, 15, 5),   new(0, 13, 5),   new(-5, 11, 5),  new(-10, 8, 5),
            new(-10, 8, 5),  new(-5, 11, 5),  new(0, 13, 5),   new(0, 15, 5),
            new(0, 15, 5),   new(0, 13, 5),   new(-5, 11, 5),  new(-10, 8, 5),
            new(-12, 6, 5),  new(-7, 8, 5),   new(0, 11, 5),   new(0, 13, 5),
            new(0, 13, 5),   new(0, 12, 5),   new(-7, 12, 5),  new(-12, 6, 5),
            new(-14, 3, 5),  new(-12, 6, 5),  new(-7, 8, 5),   new(-5, 11, 5),
            new(-5, 11, 5),  new(-7, 10, 5),  new(-12, 7, 5),  new(-14, 4, 5),
            new(-18, 1, 5),  new(-14, 3, 5),  new(-12, 6, 5),  new(-10, 8, 5),
            new(-10, 8, 5),  new(-12, 6, 5),  new(-14, 3, 5),  new(-17, 1, 5),

            new(-10, 1), new(-10, 1), new(-20, -4), new(-20, -4), new(-30, -9), new(-5, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-80, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-80, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-40, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-40, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-20, 1),

            new(-10, 1), new(-10, 1), new(-20, -4), new(-20, -4), new(-30, -9), new(-5, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-80, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-80, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-40, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-40, 1),
            new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-5, 1), new(-20, 1),

            new(-10, 1), new(-10, 1),

            new(0, 5), new(0, 9), new(0, 1), new(0, 17), new(0, 25),
            new(0, 5), new(0, 9), new(0, 9), new(0, 1), new(0, 25),
            new(0, 5), new(0, 9), new(0, 1), new(0, 1), new(0, 1),

            new(0, 5), new(0, 9), new(0, 1), new(0, 17), new(0, 25),
            new(0, 5), new(0, 9), new(0, 9), new(0, 1), new(0, 25),
            new(0, 5), new(0, 9), new(0, 1), new(0, 1), new(0, 1),

            new(-20, 1), new(-20, 1),
            new(-10, 1), new(-10, 1),
            new(-10, 1), new(-10, 1),
            new(0, 11), new(0, 11),
            new(0, 5), new(0, 5),

            new(0, 6), new(0, 6), new(0, 6), new(5, 26), new(5, 26), new(25, 101),
            new(0, 6), new(5, 11), new(10, 21), new(20, 41), new(40, 81), new(80, 161),

            new(0, 5), new(0, 5), new(3, 11), new(3, 11), new(6, 21), new(11, 41),
            new(0, 5), new(0, 5), new(3, 11), new(3, 11), new(6, 21), new(11, 41),

            new(10, 51), new(10, 51),
            new(0, 11), new(5, 21),
            new(0, 1), new(5, 21),
            new(0, 1), new(0, 21),
            new(5, 21), new(10, 41)
            #endregion generator objects
        };

        /*
        private static readonly WtInfo[] wtInfos =
        {
            new WtInfo("opPhase", 0, 1, 1, 21),
            new WtInfo("egPhase", 1, 2, 25, 70, 100),
            new WtInfo("opMobility", 2, 3, 1, 21),
            new WtInfo("egMobility", 3, 4, 1, 21),
            new WtInfo("opKAttack1", 4, 5, 4, 17),
            new WtInfo("opKAttack2", 5, 6, 2, 9),
            new WtInfo("opKAttack3", 6, 7, 1, 5),
            new WtInfo("egKAttack1", 7, 8, 4, 17),
            new WtInfo("egKAttack2", 8, 9, 2, 9),
            new WtInfo("egKAttack3", 9, 10, 1, 5),
            new WtInfo("opDevelopment", 10, 11, 1, 11),
            new WtInfo("egDevelopment", 11, 12, 0, 5),
            new WtInfo("opPawnVal", 12, 13, 80, 121),
            new WtInfo("opKnightVal", 13, 14, 192, 481),
            new WtInfo("opBishopVal", 14, 15, 240, 481),
            new WtInfo("opRookVal", 15, 16, 360, 841),
            new WtInfo("opQueenVal", 16, 17, 632, 1621),
            new WtInfo("opKingVal", 17, 18, 0, 1),
            new WtInfo("egPawnVal", 18, 19, 80, 121),
            new WtInfo("egKnightVal", 19, 20, 192, 481),
            new WtInfo("egBishopVal", 20, 21, 240, 481),
            new WtInfo("egRookVal", 21, 22, 360, 841),
            new WtInfo("egQueenVal", 22, 23, 632, 1621),
            new WtInfo("egKingVal", 23, 24, 0, 1),
            new WtInfo("opPawnPcSqr", 24, 88, -42, 161),
            new WtInfo("opKnightPcSqr", 88, 152, -200, 155),
            new WtInfo("opBishopPcSqr", 152, 216, -98, 71),
            new WtInfo("opRookPcSqr", 216, 280, -85, 97),
            new WtInfo("opQueenPcSqr", 280, 344, -60, 71),
            new WtInfo("opKingPcSqr", 344, 408, -78, 44),
            new WtInfo("egPawnPcSqr", 408, 472, -10, 225),
            new WtInfo("egKnightPcSqr", 472, 536, -119, 31),
            new WtInfo("egBishopPcSqr", 536, 600, -32, 24),
            new WtInfo("egRookPcSqr", 600, 664, -24, 23),
            new WtInfo("egQueenPcSqr", 664, 728, -49, 71),
            new WtInfo("egKingPcSqr", 728, 792, -89, 55),
            new WtInfo("opBlockedPawn", 792, 828, -25, 1),
            new WtInfo("egBlockedPawn", 828, 864, -25, 1),
            new WtInfo("opBlockedDbl", 864, 865, -25, 1),
            new WtInfo("egBlockedDbl", 865, 866, -25, 1),
            new WtInfo("opPinned", 866, 881, -25, 51),
            new WtInfo("egPinned", 881, 896, -25, 51),
            new WtInfo("opIsolated", 896, 897, -25, 1),
            new WtInfo("egIsolated", 897, 898, -25, 1),
            new WtInfo("opBackward", 898, 899, -25, 1),
            new WtInfo("egBackward", 899, 900, -25, 1),
            new WtInfo("opDoubled", 900, 901, -25, 1),
            new WtInfo("egDoubled", 901, 902, -25, 1),
            new WtInfo("opNOutpost", 902, 903, 1, 26),
            new WtInfo("egNOutpost", 903, 904, 1, 26),
            new WtInfo("opBOutpost", 904, 905, 1, 26),
            new WtInfo("egBOutpost", 905, 906, 1, 26),
            new WtInfo("opPassed2", 906, 907, 1, 11),
            new WtInfo("opPassed3", 907, 908, 1, 11),
            new WtInfo("opPassed4", 908, 909, 1, 11),
            new WtInfo("opPassed5", 909, 910, 5, 51),
            new WtInfo("opPassed6", 910, 911, 5, 51),
            new WtInfo("opPassed7", 911, 912, 50, 301),
            new WtInfo("egPassed2", 912, 913, 1, 11),
            new WtInfo("egPassed3", 913, 914, 5, 21),
            new WtInfo("egPassed4", 914, 915, 10, 41),
            new WtInfo("egPassed5", 915, 916, 20, 81),
            new WtInfo("egPassed6", 916, 917, 40, 161),
            new WtInfo("egPassed7", 917, 918, 80, 321),
            new WtInfo("opAdjacent", 918, 924, 1, 26),
            new WtInfo("egAdjacent", 924, 930, 1, 26),
            new WtInfo("opBPair", 930, 931, 1, 76),
            new WtInfo("egBPair", 931, 932, 1, 76),
            new WtInfo("opPawnMajority", 932, 933, 1, 51),
            new WtInfo("egPawnMajority", 933, 934, 1, 51),
            new WtInfo("opKNearPp", 934, 935, 1, 51),
            new WtInfo("egKNearPp", 935, 936, 1, 51),
            new WtInfo("opGuardPPawn", 936, 937, 1, 51),
            new WtInfo("egGuardPPawn", 937, 938, 1, 51),
            new WtInfo("opClosestPPawn", 938, 939, 1, 51),
            new WtInfo("egClosestPPawn", 939, 940, 1, 51)
        };
        */
    }
}