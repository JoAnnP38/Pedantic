using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.AccessControl;
using LiteDB;

namespace Pedantic.Genetics
{
    public class ChessWeights : IChromosome<ChessWeights>
    {
        public const double MUTATION_PROBABILITY = 0.0002d;
        public const int MAX_AGE = 5;
        public const int MAX_WEIGHTS = 860;
        public const int MAX_PARENTS = 2;
        public const int PIECE_WEIGHT_LENGTH = 6;
        public const int PIECE_SQUARE_LENGTH = 384;
        public const int OPENING_PHASE_WEIGHT_OFFSET = 0;
        public const int ENDGAME_PHASE_WEIGHT_OFFSET = 1;
        public const int OPENING_MOBILITY_WEIGHT_OFFSET = 2;
        public const int ENDGAME_MOBILITY_WEIGHT_OFFSET = 3;
        public const int OPENING_KING_ATTACK_WEIGHT_OFFSET = 4;
        public const int ENDGAME_KING_ATTACK_WEIGHT_OFFSET = 6;
        public const int OPENING_DEVELOPMENT_WEIGHT_OFFSET = 8;
        public const int ENDGAME_DEVELOPMENT_WEIGHT_OFFSET = 9;
        public const int OPENING_PIECE_WEIGHT_OFFSET = 10;
        public const int ENDGAME_PIECE_WEIGHT_OFFSET = 16;
        public const int OPENING_PIECE_SQUARE_WEIGHT_OFFSET = 22;
        public const int ENDGAME_PIECE_SQUARE_WEIGHT_OFFSET = 406;
        public const int BLOCKED_PAWN_OFFSET = 790;
        public const int BLOCKED_PAWN_DBL_MOVE_OFFSET = 826;
        public const int PINNED_PIECE_OFFSET = 827;
        public const int ISOLATED_PAWN_OFFSET = 842;
        public const int BACKWARD_PAWN_OFFSET = 843;
        public const int DOUBLED_PAWN_OFFSET = 844;
        public const int KNIGHT_OUTPOST_OFFSET = 845;
        public const int BISHOP_OUTPOST_OFFSET = 846;
        public const int PASSED_PAWNS_OFFSET = 847;
        public const int ADJACENT_PAWNS_OFFSET = 853;
        public const int BISHOP_PAIR_OFFSET = 859;

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
                totalChange += (double)Math.Abs(other.Weights[n] - Weights[n]) / Weights[n];
            }

            return (totalChange / MAX_WEIGHTS) * 100.0;
        }

        public static ChessWeights Empty { get; } = new ChessWeights();

        public static (ChessWeights child1, ChessWeights child2) CrossOver(ChessWeights parent1, ChessWeights parent2, bool checkMutation)
        {
            short[] weights1 = new short[MAX_WEIGHTS];
            short[] weights2 = new short[MAX_WEIGHTS];

            for (int n = 0; n < MAX_WEIGHTS; ++n)
            {
                if (rand.NextDouble() <= 0.5d)
                {
                    weights1[n] = Mutate(checkMutation, n, parent1.Weights[n]);
                    weights2[n] = Mutate(checkMutation, n, parent2.Weights[n]);
                }
                else
                {
                    weights1[n] = Mutate(checkMutation, n, parent2.Weights[n]);
                    weights2[n] = Mutate(checkMutation, n, parent1.Weights[n]);
                }
            }

            return (new ChessWeights(parent1, parent2, weights1), new ChessWeights(parent1, parent2, weights2));
        }

        public static short Mutate(bool checkMutation, int nWeight, short weight)
        {
            if (!checkMutation || rand.NextDouble() <= MUTATION_PROBABILITY)
            {
                return NextWeight(nWeight);
            }

            return weight;
        }

        public static short NextWeight(int nWeight)
        {
            if (nWeight == OPENING_PHASE_WEIGHT_OFFSET)
            {
                return (short)rand.Next(1, 21);
            }

            if (nWeight == ENDGAME_PHASE_WEIGHT_OFFSET)
            {
                return (short)rand.Next(100, 7801);
            }

            if (nWeight == OPENING_MOBILITY_WEIGHT_OFFSET || nWeight == ENDGAME_MOBILITY_WEIGHT_OFFSET)
            {
                return (short)rand.Next(0, 101);
            }

            if (nWeight >= OPENING_KING_ATTACK_WEIGHT_OFFSET && nWeight < OPENING_DEVELOPMENT_WEIGHT_OFFSET)
            {
                return (short)rand.Next(0, 101);
            }

            if (nWeight == OPENING_DEVELOPMENT_WEIGHT_OFFSET || nWeight == ENDGAME_DEVELOPMENT_WEIGHT_OFFSET)
            {
                return (short)rand.Next(0, 201);
            }

            if (nWeight >= OPENING_PIECE_WEIGHT_OFFSET && nWeight < OPENING_PIECE_SQUARE_WEIGHT_OFFSET)
            {
                return (short)rand.Next(10, 1801);
            }

            if (nWeight >= OPENING_PIECE_SQUARE_WEIGHT_OFFSET && nWeight < BLOCKED_PAWN_OFFSET)
            {
                return (short)rand.Next(-300, 301);
            }

            if (nWeight >= BLOCKED_PAWN_OFFSET && nWeight < BLOCKED_PAWN_DBL_MOVE_OFFSET)
            {
                return (short)rand.Next(-100, 1);
            }

            if (nWeight == BLOCKED_PAWN_DBL_MOVE_OFFSET)
            {
                return (short)rand.Next(-100, 1);
            }

            if (nWeight >= PINNED_PIECE_OFFSET && nWeight < ISOLATED_PAWN_OFFSET)
            {
                return (short)rand.Next(-50, 100);
            }

            if (nWeight == ISOLATED_PAWN_OFFSET)
            {
                return (short)rand.Next(-100, 1);
            }

            if (nWeight == BACKWARD_PAWN_OFFSET)
            {
                return (short)rand.Next(-100, 1);
            }

            if (nWeight == DOUBLED_PAWN_OFFSET)
            {
                return (short)rand.Next(-100, 1);
            }

            if (nWeight == KNIGHT_OUTPOST_OFFSET || nWeight == BISHOP_OUTPOST_OFFSET)
            {
                return (short)rand.Next(0, 101);
            }

            if (nWeight >= PASSED_PAWNS_OFFSET && nWeight < BISHOP_PAIR_OFFSET)
            {
                return (short)rand.Next(0, 401);
            }

            if (nWeight == BISHOP_PAIR_OFFSET)
            {
                return (short)rand.Next(0, 101);
            }

            return 0;
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

        private static Random rand = new();

        private static readonly short[] paragonWeights =
        {
            /* OpeningPhaseThruMove */
            10,   
            
            /* EndGamePhaseMaterial */
            3900, 
            
            /* OpeningMobilityWeight */
            10,  
            
            /* EndGameMobilityWeight */
            10,   
            
            /* MidGameKingSafetyWeight */
            8, // attacks to squares adjacent to king
            4, // attacks to squares 2 squares from king

            /* EndGameKingSafetyWeight */
            8, // attacks to squares adjacent to king
            4, // attacks to squares 2 squares from king

            /* MidGameDevelopmentWeight */
            50,

            /* EndGameDevelopmentWeight */
            0,

            /* opening piece values */
            82, 337, 365, 477, 1025, 0, 

            /* end game piece values */
            94, 281, 297, 512,  936, 0,

            /* opening piece square values */
            #region opening piece square values
            /* pawns */
              0,   0,   0,   0,   0,   0,   0,   0,
            -35,  -1, -20, -23, -15,  24,  38, -22,
            -26,  -4,  -4, -10,   3,   3,  33, -12,
            -27,  -2,  -5,  12,  17,   6,  10, -25,
            -14,  13,   6,  21,  23,  12,  17, -23,
             -6,   7,  26,  31,  65,  56,  25, -20,
             98, 134,  61,  95,  68, 126,  34, -11,
              0,   0,   0,   0,   0,   0,   0,   0,

            /* knights */
           -105, -21, -58, -33, -17, -28, -19, -23,
            -29, -53, -12,  -3,  -1,  18, -14, -19,
            -23,  -9,  12,  10,  19,  17,  25, -16,
            -13,   4,  16,  13,  28,  19,  21,  -8,
             -9,  17,  19,  53,  37,  69,  18,  22,
            -47,  60,  37,  65,  84, 129,  73,  44,
            -73, -41,  72,  36,  23,  62,   7, -17,
           -167, -89, -34, -49,  61, -97, -15,-107,

            /* bishops */
            -33,  -3, -14, -21, -13, -12, -39, -21,
              4,  15,  16,   0,   7,  21,  33,   1,
              0,  15,  15,  15,  14,  27,  18,  10,
             -6,  13,  13,  26,  34,  12,  10,   4,
             -4,   5,  19,  50,  37,  37,   7,  -2,
            -16,  37,  43,  40,  35,  50,  37,  -2,
            -26,  16, -18, -13,  30,  59,  18, -47,
            -29,   4, -82, -37, -25, -42,   7,  -8,

            /* rooks */
            -19, -13,   1,  17,  16,   7, -37, -26,
            -44, -16, -20,  -9,  -1,  11,  -6, -71,
            -45, -25, -16, -17,   3,   0,  -5, -33,
            -36, -26, -12,  -1,   9,  -7,   6, -23,
            -24, -11,   7,  26,  24,  35,  -8, -20,
             -5,  19,  26,  36,  17,  45,  61,  16,
             27,  32,  58,  62,  80,  67,  26,  44,
             32,  42,  32,  51,  63,   9,  31,  43,
            
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
             -23,  -9, -23,  -5,  -9, -16,  -5, -17,
             -14, -18,  -7,  -1,   4,  -9, -15, -27,
             -12,  -3,   8,  10,  13,   3,  -7, -15,
              -6,   3,  13,  19,   7,  10,  -3,  -9,
              -3,   9,  12,   9,  14,  10,   3,   2,
               2,  -8,   0,  -1,  -2,   6,   0,   4,
              -8,  -4,   7, -12,  -3, -13,  -4, -14,
             -14, -21, -11,  -8,  -7,  -9, -17, -24,

            /* rooks */
              -9,   2,   3,  -1,  -5, -13,   4, -20,
              -6,  -6,   0,   2,  -9,  -9, -11,  -3,
              -4,   0,  -5,  -1,  -7, -12,  -8, -16,
               3,   5,   8,   4,  -5,  -6,  -8, -11,
               4,   3,  13,   1,   2,   1,  -1,   2,
               7,   7,   7,   5,   4,  -3,  -5,  -3,
              11,  13,  13,  11,  -3,   3,   8,   3,
              13,  10,  18,  15,  12,  12,   8,   5,

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

            /* opening pawn double move blocked */
            -5,

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

            /* isolated pawns */
            -5,

            /* backward pawn */
            -5,

            /* doubled pawn */
            -5,

            /* knight on outpost */
            5,

            /* bishop on outpost */
            2,

            /* passed pawns */
            10, // rank 2
            10, // rank 3
            15, // rank 4
            30, // rank 5
            60, // rank 6
            100, // rank 7

            /* adjacent pawns */
            2, // rank 2
            2, // rank 3
            4, // rank 4
            4, // rank 5
            100, // rank 6
            200, // rank 7

            /* bishop pair (vs 2 knights or bishop and knight) */
            20
        };
    }
}