using Pedantic.Utilities;
using LiteDB;

namespace Pedantic.Genetics
{
    public class ChessWeights
    {
        public const int MAX_WEIGHTS = 826;
        public const int ENDGAME_WEIGHTS = 413;
        public const int PIECE_WEIGHT_LENGTH = 6;
        public const int PIECE_SQUARE_LENGTH = 384;
        public const int GAME_PHASE_MATERIAL = 0;
        public const int PIECE_VALUES = 1;
        public const int PIECE_SQUARE_TABLE = 7;
        public const int PIECE_MOBILITY = 391;
        public const int KING_ATTACK = 395;
        public const int PAWN_SHIELD = 398;
        public const int ISOLATED_PAWN = 401;
        public const int BACKWARD_PAWN = 402;
        public const int DOUBLED_PAWN = 403;
        public const int CONNECTED_PAWN = 404;
        public const int PASSED_PAWN = 405;
        public const int KNIGHT_OUTPOST = 406;
        public const int BISHOP_OUTPOST = 407;
        public const int BISHOP_PAIR = 408;
        public const int ROOK_ON_OPEN_FILE = 409;
        public const int ROOK_ON_HALF_OPEN_FILE = 410;
        public const int ROOK_BEHIND_PASSED_PAWN = 411;
        public const int DOUBLED_ROOKS_ON_FILE = 412;

        [BsonCtor]
        public ChessWeights(ObjectId _id, bool isActive, bool isImmortal, string description, short[] weights, 
            float fitness, int sampleSize, float k, short totalPasses, DateTime updatedOn, DateTime createdOn)
        {
            Id = _id;
            IsActive = isActive;
            IsImmortal = isImmortal;
            Description = description;
            Weights = weights;
            Fitness = fitness;
            SampleSize = sampleSize;
            K = k;
            TotalPasses = totalPasses;
            UpdatedOn = updatedOn;
        }

        public ChessWeights(ChessWeights other)
        {
            Id = ObjectId.NewObjectId();
            IsActive = other.IsActive;
            IsImmortal = other.IsImmortal;
            Description = other.Description;
            Weights = ArrayEx.Clone(other.Weights);
            Fitness = other.Fitness;
            SampleSize = other.SampleSize;
            K = other.K;
            TotalPasses = other.TotalPasses;
            UpdatedOn = DateTime.UtcNow;
        }

        public ChessWeights(short[] weights)
        {
            Id = ObjectId.NewObjectId();
            IsActive = true;
            IsImmortal = false;
            Description = "Anonymous";
            Weights = ArrayEx.Clone(weights);
            Fitness = 0;
            SampleSize = 0;
            K = 0;
            TotalPasses = 0;
            UpdatedOn = CreatedOn;
        }

        private ChessWeights()
        {
            Id = ObjectId.Empty;
            IsActive = false;
            IsImmortal = false;
            Description = string.Empty;
            Weights = Array.Empty<short>();
            Fitness = default;
            SampleSize = default;
            K = default;
            TotalPasses = default;
            UpdatedOn = CreatedOn;
        }

        public ObjectId Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsImmortal { get; set; }
        public string Description { get; set; }
        public short[] Weights { get; init; }
        public float Fitness { get; set; }
        public int SampleSize { get; set; }
        public float K { get; set; }
        public short TotalPasses { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime CreatedOn => Id.CreationTime;

        public static ChessWeights Empty { get; } = new ChessWeights();

        public static ChessWeights CreateParagon()
        {
            ChessWeights paragon = new(paragonWeights)
            {
                IsImmortal = true,
                Fitness = 0,
                SampleSize = 0,
                K = 0,
                TotalPasses = 0,
                UpdatedOn = DateTime.UtcNow
            };
            return paragon;
        }

        public static bool LoadParagon(out ChessWeights paragon)
        {
            using var rep = new GeneticsRepository();
            ChessWeights? p = rep.Weights.FindOne(w => w.IsActive && w.IsImmortal);
            if (p == null)
            {
                p = CreateParagon();
                rep.Weights.Insert(p);
                paragon = p;
            }
            else
            {
                paragon = p;
            }
            return true;
        }

        private static readonly short[] paragonWeights =
        {
            /*----------------------- OPENING WEIGHTS -----------------------*/

            /* OpeningPhaseMaterial */
            7200,

            /* opening piece values */
            80, 335, 365, 475, 1025, 0,

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

            /* OpeningMobilityWeight */
            4, // Knight
            3, // Bishop
            2, // Rook
            1, // Queen

            /* OpeningKingAttackWeight */
            9, // attacks to squares adjacent to king
            4, // attacks to squares 2 squares from king
            1, // attacks to squares 3 squares from king

            /* Opening Pawn Shield/King Safety */
            9, // Pawn adjacent to king
            4, // Pawn 2 squares from king
            1, // Pawn 3 squares from king

            /* opening isolated pawns */
            -10,

            /* opening backward pawn */
            -5,

            /* opening doubled pawn */
            -5,

            /* opening adjacent/connected pawns */
            5,

            /* opening passed pawn */
            20,

            /* opening knight on outpost */
            5,

            /* opening bishop on outpost */
            2,

            /* opening bishop pair */
            20,

            /* opening rook on open file */
            20,

            /* opening rook on half open file */
            10,

            /* opening rook behind passed pawn */
            30,

            /* doubled rooks on file */
            10,

            /*------------------------- END GAME WEIGHTS --------------------*/

            /* EndGamePhaseMaterial */
            3900,

            /* end game piece values */
            95, 280, 295, 510, 935, 0,

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

            /* EndGameMobilityWeight */
            4, // Knight
            3, // Bishop
            2, // Rook
            1, // Queen

            /* EndGameKingAttackWeight */
            9, // attacks to squares adjacent to king
            4, // attacks to squares 2 squares from king
            1, // attacks to squares 3 squares from king

            /* End game Pawn Shield/King Safety */
            9, // Pawn adjacent to king
            4, // Pawn 2 squares from king
            1, // Pawn 3 squares from king

            /* end game isolated pawns */
            -10,

            /* end game backward pawn */
            -5,

            /* end game doubled pawn */
            -5,

            /* end game adjacent/connected pawns */
            5,

            /* end game passed pawn */
            40,

            /* endgame knight on outpost */
            5,

            /* end game bishop on outpost */
            2,

            /* end game bishop pair */
            20,

            /* end game rook on open file */
            20,

            /* end game rook on half open file */
            10,

            /* end game rook behind passed pawn */
            30,

            /* end game doubled rooks on file */
            10
        };
    }
}