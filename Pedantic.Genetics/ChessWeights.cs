// ***********************************************************************
// Assembly         : Pedantic.Genetics
// Author           : JoAnn D. Peeler
// Created          : 03-12-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-28-2023
// ***********************************************************************
// <copyright file="ChessWeights.cs" company="Pedantic.Genetics">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Mapped POCO class for maintaining the weights in the LiteDB 
//     database.
// </summary>
// ***********************************************************************
using Pedantic.Utilities;

namespace Pedantic.Genetics
{
    public sealed class ChessWeights
    {
        public static readonly Guid DEFAULT_IMMORTAL_ID = new("da5e310e-b0dc-4c77-902c-5a46cc81bb73");
        public const int MAX_WEIGHTS = 3968;
        public const int ENDGAME_WEIGHTS = 1984;
        public const int PIECE_WEIGHT_LENGTH = 6;
        public const int PIECE_SQUARE_LENGTH = 1536;
        public const int PIECE_VALUES = 0;
        public const int PIECE_SQUARE_TABLE = 6;
        public const int PIECE_MOBILITY = 1542;
        public const int KING_ATTACK = 1546;
        public const int PAWN_SHIELD = 1549;
        public const int ISOLATED_PAWN = 1552;
        public const int BACKWARD_PAWN = 1553;
        public const int DOUBLED_PAWN = 1554;
        public const int CONNECTED_PAWN = 1555;
        public const int KNIGHT_OUTPOST = 1619;
        public const int BISHOP_OUTPOST = 1620;
        public const int BISHOP_PAIR = 1621;
        public const int ROOK_ON_OPEN_FILE = 1622;
        public const int ROOK_ON_HALF_OPEN_FILE = 1623;
        public const int ROOK_BEHIND_PASSED_PAWN = 1624;
        public const int DOUBLED_ROOKS_ON_FILE = 1625;
        public const int KING_ON_OPEN_FILE = 1626;
        public const int KING_ON_HALF_OPEN_FILE = 1627;
        public const int CASTLING_AVAILABLE = 1628;
        public const int CASTLING_COMPLETE = 1629;
        public const int CENTER_CONTROL = 1630;
        public const int QUEEN_ON_OPEN_FILE = 1632;
        public const int QUEEN_ON_HALF_OPEN_FILE = 1633;
        public const int ROOK_ON_7TH_RANK = 1634;
        public const int PASSED_PAWN = 1635;
        public const int BAD_BISHOP_PAWN = 1699;
        public const int BLOCK_PASSED_PAWN = 1763;
        public const int SUPPORTED_PAWN = 1811;
        public const int KING_OUTSIDE_PP_SQUARE = 1875;
        public const int PP_FRIENDLY_KING_DISTANCE = 1876;
        public const int PP_ENEMY_KING_DISTANCE = 1877;
        public const int PAWN_RAM = 1878;
        public const int PIECE_THREAT = 1942;
        public const int PAWN_PUSH_THREAT = 1978;

        public ChessWeights(Guid _id, bool isActive, bool isImmortal, string description, short[] weights, 
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
            CreatedOn = createdOn;
        }

        public ChessWeights(ChessWeights other)
        {
            Id = Guid.NewGuid();
            IsActive = other.IsActive;
            IsImmortal = other.IsImmortal;
            Description = other.Description;
            Weights = ArrayEx.Clone(other.Weights);
            Fitness = other.Fitness;
            SampleSize = other.SampleSize;
            K = other.K;
            TotalPasses = other.TotalPasses;
            UpdatedOn = DateTime.UtcNow;
            CreatedOn = DateTime.UtcNow;
        }

        public ChessWeights(short[] weights)
        {
            Id = Guid.NewGuid();
            IsActive = true;
            IsImmortal = false;
            Description = "Anonymous";
            Weights = ArrayEx.Clone(weights);
            Fitness = 0;
            SampleSize = 0;
            K = 0;
            TotalPasses = 0;
            UpdatedOn = DateTime.UtcNow;
            CreatedOn = DateTime.UtcNow;
        }

        public ChessWeights()
        {
            Id = Guid.NewGuid();
            IsActive = false;
            IsImmortal = false;
            Description = string.Empty;
            Weights = Array.Empty<short>();
            Fitness = default;
            SampleSize = default;
            K = default;
            TotalPasses = default;
            UpdatedOn = DateTime.UtcNow;
            CreatedOn = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsImmortal { get; set; }
        public string Description { get; set; }
        public short[] Weights { get; init; }
        public float Fitness { get; set; }
        public int SampleSize { get; set; }
        public float K { get; set; }
        public short TotalPasses { get; set; }
        public DateTime UpdatedOn { get; set; }

        public DateTime CreatedOn { get; set; }

        public static ChessWeights Empty { get; } = new ChessWeights();

        public static ChessWeights CreateParagon()
        {
            ChessWeights paragon = new(paragonWeights)
            {
                Id = DEFAULT_IMMORTAL_ID,
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
            var rep = new ChessDb();
            ChessWeights? p = rep.Weights.GetAll().FirstOrDefault(w => w.IsActive && w.IsImmortal);
            if (p == null)
            {
                p = CreateParagon();
                rep.Weights.Insert(p);
                rep.Save();
                paragon = p;
            }
            else
            {
                paragon = p;
            }
            return true;
        }

        // Solution sample size: 12000000, generated on Wed, 13 Sep 2023 07:49:56 GMT
        // Solution error: 0.118151, accuracy: 0.5203, seed: -434034628
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening piece values */
            95, 467, 521, 633, 1526, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -23,  -29,  -21,    1,  -33,   48,   43,   -9,
             -26,  -21,  -27,  -49,  -27,   -8,    7,  -31,
              -1,   -3,    1,  -13,    6,   21,    4,    0,
              20,   -8,   17,   33,   41,   31,   38,   49,
              10,  -11,   14,   38,   68,  134,  131,   88,
              75,  108,   93,  114,  104,   99,   76,   59,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -48,  -60,  -41,  -42,  -29,   42,   46,   -7,
             -34,  -45,  -38,  -59,  -25,  -16,   14,  -26,
             -22,   -1,   -8,  -13,   -3,   -5,   -1,   15,
              -8,   19,   19,   25,    5,  -19,   19,   43,
              66,   76,  121,   51,   58,   17,   27,   38,
              76,   50,   81,   81,  137,   99,  179,  155,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              -2,   23,   12,  -15,  -42,  -10,  -27,  -48,
              11,   24,  -20,  -34,  -33,  -18,  -35,  -71,
              34,   30,    1,  -13,  -11,    6,  -12,  -23,
              44,    4,   21,   38,   33,    9,   17,    9,
              47,   11,   31,   46,   79,  110,   78,   20,
              78,  119,  115,  114,  105,  120,   65,   26,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -33,    0,   24,   -6,   -7,    5,   -5,    4,
             -21,   -7,  -16,  -47,  -16,  -22,  -27,  -33,
               7,    0,    8,   -4,   13,    9,   -1,   -7,
              70,   63,   55,   45,   16,   -7,   10,   53,
             152,   98,  149,   72,   28,   -3,   -7,   38,
             113,   85,   79,   78,  113,   34,  145,   89,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
            -120,  -27,  -55,  -33,  -15,  -11,  -26,  -87,
             -42,  -48,  -13,    2,    2,   14,  -12,  -12,
             -30,  -17,   -6,   14,   22,    3,    1,   -2,
             -11,    0,   13,   16,   25,   40,   39,   10,
              -1,    3,   30,   35,    1,   32,    3,   60,
             -32,   16,   27,   36,   74,  148,   24,   -6,
             -21,  -28,   -3,   39,   35,   91,   12,   -7,
            -265,  -10,  -14,   65,   44,  -93,   19, -169,

            /* knights: KQ */
             -63,  -53,  -53,  -59,  -52,  -34,  -42,  -26,
             -81,  -44,  -23,    5,  -15,   -3,  -65,  -54,
             -57,  -11,  -15,   11,   14,   12,  -21,  -17,
             -12,   37,   25,   47,   30,   11,   18,  -18,
              47,    7,   44,   46,   65,   47,   34,   -5,
              50,   49,   82,   87,   55,   52,   -3,    2,
              -1,  -33,  -11,   44,   20,   30,  -24,  -42,
            -290,  -46,  -12,   54,   -6,    9,  -45, -110,

            /* knights: QK */
            -103,  -77,  -53,  -71,  -34,  -17,  -63,  -95,
             -70,  -45,   -3,  -14,  -19,    5,  -55,  -16,
             -20,  -14,   17,   38,   48,  -22,    1,  -61,
              21,   40,   38,   24,   42,   28,   33,  -12,
              13,   35,   54,  113,   55,   64,   37,   52,
               0,   11,   22,   62,   75,   96,   25,    6,
             -24,   17,   40,   64,  -13,   48,    0,   57,
            -141,  -47,   18,   20,  -21,  -53,  -72,  -55,

            /* knights: QQ */
              -9,  -35,    8,  -43,  -38,  -43,  -43,  -31,
             -40,  -29,  -23,    9,  -22,  -16,   -9, -102,
              -7,  -12,   12,   17,   28,  -28,  -24,  -27,
              22,    0,   56,   60,   35,   29,   29,  -35,
              22,    4,   29,   62,   62,   42,   42,    8,
              62,   -4,   57,  103,   44,   21,   49,    5,
               3,    9,  -19,   27,   53,   25,  -10,  -66,
            -182,   11,   11,   17,   18,   52,   25,  -82,

            /* bishops: KK */
              -1,    6,  -19,  -31,  -11,  -10,  -13,   -4,
               8,    2,    5,   -6,    1,   13,   30,    7,
             -18,    4,   12,    5,    1,    6,    5,   -4,
             -16,  -14,    6,   32,   27,    3,    2,   20,
             -15,    9,   -3,   38,   18,   35,    6,   -3,
             -12,    7,   -9,  -16,   -3,   66,   43,  -21,
             -60,  -26,  -19,  -31,  -29,  -11,  -81,  -54,
             -92,  -38,  -35,  -60,  -11,  -54,   32,  -45,

            /* bishops: KQ */
             -50,   12,  -38,  -47,    6,  -15,   50,  -17,
             -25,  -17,  -10,   -1,    1,   36,   58,    4,
             -39,   -7,   23,   10,   12,   32,   40,   32,
              15,   -8,   -2,   45,   57,   31,    0,  -18,
               9,   30,   24,  105,   65,   10,   14,   -8,
             -10,   81,   67,    8,   17,   12,    6,    1,
             -38,    8,   -9,   -7,   -5,  -24,    1,  -39,
             -54,  -60,  -11,  -12,  -40,   -1,   17,  -50,

            /* bishops: QK */
              11,   13,    6,  -25,  -25,  -37,  -42, -124,
              33,   46,   19,   15,  -12,  -27,  -37,  -19,
              -8,   30,   30,   25,    1,    0,   -1,  -18,
             -13,    5,   21,   65,   50,  -11,  -30,  -25,
             -30,   16,   27,   65,   27,   47,   21,   -6,
             -28,  -14,   21,   23,   55,   47,   50,   13,
             -56,  -43,  -31,  -24,  -36,   27,  -24,  -28,
             -57,  -45,  -37,  -52,   -2,  -26,   31,  -29,

            /* bishops: QQ */
             -76,  -43,  -14,  -55,   -3,  -23,   14,   -4,
              36,   35,  -24,   13,  -34,   12,  -14,   56,
              -7,   26,   19,   15,   16,   17,   25,   -6,
              22,   25,   11,   66,   15,   48,    5,   24,
               8,   30,    4,   63,   70,   -7,   -3,   -8,
             -24,   53,   77,   44,   25,   43,    2,   -7,
             -37,  -12,   27,   -7,   25,   -7,   46,  -11,
             -60,    5,    8,   30,   16,   10,   11,  -42,

            /* rooks: KK */
             -20,   -9,   -5,    4,   14,    9,   19,  -13,
             -40,  -19,  -14,   -6,   -1,   14,   36,  -12,
             -36,  -37,  -24,  -15,   10,    2,   35,    5,
             -27,  -21,  -14,    0,   -9,  -22,   29,  -20,
             -14,    8,   13,   28,   10,   43,   79,   30,
               0,   39,   42,   47,   69,  133,  125,   78,
             -33,  -28,    0,   21,   13,   67,   81,  103,
              66,   48,   40,   47,   22,   71,  106,  111,

            /* rooks: KQ */
             -25,   18,    6,   -4,   -7,    2,  -15,  -27,
             -49,    7,  -20,   -8,  -22,  -30,  -13,  -21,
             -24,   -6,  -32,  -15,  -39,  -16,    9,  -27,
             -38,   20,  -39,    7,   -3,  -43,    8,    3,
              26,   60,    0,  -16,   42,   34,   34,    0,
              31,   69,   50,   61,   48,   51,   98,   42,
              20,   25,   13,    9,   35,   12,   46,   77,
              26,   53,   36,   19,   15,   32,   51,   71,

            /* rooks: QK */
             -61,   -6,   -5,   -5,    4,   -3,   18,  -18,
             -35,   -9,  -39,  -23,  -36,  -32,   -5,  -14,
             -10,   10,  -21,  -17,  -46,  -49,   -5,  -16,
              -4,   32,  -14,  -18,  -24,  -24,   27,    2,
              69,   37,   65,    2,  -25,    2,   27,   30,
              31,   90,   41,   13,   52,   89,  106,   64,
              44,   43,   54,   10,    3,   44,   27,   35,
             154,   78,   57,   36,   23,   46,   71,   68,

            /* rooks: QQ */
             -46,  -23,    1,   12,    0,   -9,  -19,  -32,
             -26,  -12,  -30,  -11,  -22,  -34,  -30,  -13,
             -35,   21,   -8,  -15,  -19,  -23,  -23,  -23,
             -19,   41,  -14,  -15,  -36,   -4,  -39,   -9,
              15,   38,   -3,   72,    8,   28,   -5,   18,
              68,   79,   96,   60,   58,   37,   43,   11,
              61,   52,   27,   34,  118,   37,   42,   22,
              39,   63,  -22,    9,    9,   14,   94,   11,

            /* queens: KK */
              -8,   -3,    9,   13,   31,    0,  -62,  -44,
             -16,    5,    4,   11,   16,   35,   28,  -11,
             -17,   -8,    0,  -10,    7,   -4,    6,   -1,
             -11,  -25,  -24,  -22,  -20,   -7,    9,  -20,
             -24,  -10,  -36,  -40,  -25,   -9,   -4,  -10,
             -21,   -4,  -42,  -55,  -12,   66,   31,   -5,
             -38,  -56,  -26,  -41,  -48,   41,   31,   74,
             -43,   12,    9,   -3,   26,   69,  121,   90,

            /* queens: KQ */
              20,   10,   11,    3,    0,    0,  -21,  -16,
              21,   15,   26,   11,   10,   -1,  -51,  -66,
              11,   -3,   15,    7,   14,   17,   -4,  -17,
              -2,   46,  -26,  -10,   10,   -8,   -7,    0,
              17,   24,  -15,  -15,   25,    2,   15,    6,
              50,   72,   50,   19,   30,   25,   45,   18,
              48,   85,   42,   63,   41,   -6,   16,    1,
              80,  104,   82,   61,   19,   57,   59,  -18,

            /* queens: QK */
             -72,  -70,  -18,  -22,    5,  -30,  -31,  -43,
             -60,  -56,   17,   14,    8,  -12,   28,   16,
             -64,  -11,    2,  -11,   13,  -19,    5,    1,
             -17,  -42,  -31,   -9,  -21,   -2,    1,  -11,
             -12,  -10,  -31,  -35,   -9,   22,    6,   14,
             -11,   12,  -19,    0,   12,   65,   39,   39,
               1,    4,  -23,  -23,   10,    7,   54,   45,
             -17,   27,   22,   16,    4,   58,   90,   54,

            /* queens: QQ */
             -40,  -66,  -31,  -24,    7,  -11,  -46,  -50,
             -72,  -32,   -3,   23,  -23,  -16,   -5,  -36,
              -5,  -10,   10,   -8,  -11,  -10,    5,   -2,
             -31,   36,  -18,  -12,    1,   -4,   -9,   -5,
              -8,   -1,   42,   37,   50,   16,   -2,   14,
              29,   50,   88,   60,   -6,  -27,    3,   25,
              73,   82,   85,   34,   40,   -1,   -5,  -42,
              13,   89,   39,   30,    7,    5,   33,   15,

            /* kings: KK */
               0,    0,    0,    0,  -42,  -66,   18,    9,
               0,    0,    0,    0,  -11,    2,   28,   10,
               0,    0,    0,    0,   33,   -8,   -6,  -48,
               0,    0,    0,    0,   98,   84,   41,  -51,
               0,    0,    0,    0,  182,  194,  118,   36,
               0,    0,    0,    0,  180,  252,  212,  145,
               0,    0,    0,    0,  208,  127,  147,   34,
               0,    0,    0,    0,   69,  129,   92,  -47,

            /* kings: KQ */
               0,    0,    0,    0,  -79,  -97,    3,   -4,
               0,    0,    0,    0,   -8,   -3,   -4,  -14,
               0,    0,    0,    0,   -4,    7,    6,  -45,
               0,    0,    0,    0,   85,   50,   18,  -22,
               0,    0,    0,    0,  156,   87,   76,   15,
               0,    0,    0,    0,  117,   89,  108,   13,
               0,    0,    0,    0,  197,  154,   68,   16,
               0,    0,    0,    0,   83,  115,   42,   -5,

            /* kings: QK */
             -18,   17,  -32, -109,    0,    0,    0,    0,
               8,   -7,  -23,  -43,    0,    0,    0,    0,
             -50,  -28,   13,  -32,    0,    0,    0,    0,
             -51,   30,   35,   47,    0,    0,    0,    0,
              -5,   65,   85,  103,    0,    0,    0,    0,
              -8,  100,  152,  123,    0,    0,    0,    0,
              43,  142,   90,  105,    0,    0,    0,    0,
             -46,   49,  106,   64,    0,    0,    0,    0,

            /* kings: QQ */
             -74,   -1,  -50,  -96,    0,    0,    0,    0,
             -16,  -19,   -2,  -38,    0,    0,    0,    0,
             -55,    2,    2,    9,    0,    0,    0,    0,
             -22,   49,   45,   70,    0,    0,    0,    0,
              52,   93,  149,  135,    0,    0,    0,    0,
              34,  149,  176,   99,    0,    0,    0,    0,
              14,   87,   46,  105,    0,    0,    0,    0,
             -63,   80,   35,  126,    0,    0,    0,    0,

            #endregion

            /* opening mobility weights */

              10, // knights
               5, // bishops
               3, // rooks
               0, // queens

            /* opening squares attacked near enemy king */
              25, // attacks to squares 1 from king
              20, // attacks to squares 2 from king
               6, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
              28, // # friendly pawns 1 from king
              13, // # friendly pawns 2 from king
               8, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -8,

            /* opening backward pawns */
            8,

            /* opening doubled pawns */
            -14,

            /* opening adjacent/connected pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              16,   -4,    3,   14,   31,   -2,  -13,   -1,
               7,   16,   11,   23,   47,    5,   13,    6,
              -7,    9,    4,   24,   45,   35,   19,   23,
              16,   33,   44,   31,   90,  101,   24,   31,
              47,  155,  159,  211,  156,  233,  145,   84,
             207,  247,  257,  266,  328,  213,   86,  115,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening knight on outpost */
            3,

            /* opening bishop on outpost */
            8,

            /* opening bishop pair */
            39,

            /* opening rook on open file */
            35,

            /* opening rook on half-open file */
            12,

            /* opening rook behind passed pawn */
            -3,

            /* opening doubled rooks on file */
            9,

            /* opening king on open file */
            -72,

            /* opening king on half-open file */
            -29,

            /* opening castling rights available */
            32,

            /* opening castling complete */
            16,

            /* opening center control */
               1, // D0
               3, // D1

            /* opening queen on open file */
            -4,

            /* opening queen on half-open file */
            11,

            /* opening rook on seventh rank */
            27,

            /* opening passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
             -14,  -24,  -27,  -50,   11,  -12,  -15,   26,
              -6,  -16,  -21,  -13,  -33,  -33,  -46,   14,
             -32,  -40,  -40,   -6,  -35,  -42,  -59,  -34,
             -10,   31,    1,  -13,   -8,   26,   26,  -13,
              86,   93,   62,   40,   11,  110,   66,   48,
             195,  161,  138,  158,  144,  167,  116,  180,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -5,    1,   -6,  -14,  -22,  -20,  -14,    3,
              -7,   -9,  -20,  -11,  -15,  -19,  -15,   -7,
              -7,    1,    6,   -3,  -19,   -7,   -6,    0,
               2,    8,    2,    5,   -5,   -4,    1,    5,
              15,    2,   16,    8,   36,   -4,   46,   -2,
              60,   61,   65,   54,   41,   67,   66,   98,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening block passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,  // blocked by pawns
               0,   50,   58,   55,   51,   65,   84,    0,  // blocked by knights
               0,   33,   30,   16,   10,   51,   97,    0,  // blocked by bishops
               0,  -47,  -10,  -32,  -28,    7,  154,    0,  // blocked by rooks
               0,  -14,   39,   -2,  -10,    6,  -86,    0,  // blocked by queens
               0,   -3,   83,   51,   74,  198,  224,    0,  // blocked by kings

            /* opening supported pawn chain */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,
              26,   29,   51,   56,   36,   43,   36,   59,
              -9,   13,   23,   30,   33,   23,   32,   13,
             -22,   27,   62,   51,   65,   63,   31,   32,
              71,  140,  132,  152,  175,   92,   84,   97,
             118,  226,  294,  187,  327,  192,  224,  -16,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening king outside passed pawn square */
            106,

            /* opening passed pawn/friendly king distance penalty */
            6,

            /* opening passed pawn/enemy king distance bonus */
            -3,

            /* opening pawn rams */
               0,    0,    0,    0,    0,    0,    0,    0,
               8,    1,   18,   13,   30,   16,   12,   -2,
              13,   12,   30,   31,   22,  -17,    5,   21,
               0,    0,    0,    0,    0,    0,    0,    0,
              -9,   -1,  -15,  -18,   -5,   -9,  -17,  -23,
              -9,    8,  -17,   16,  -76,   24,   34,  -22,
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening piece threats */
            /* P     N     B     R     Q     K */
               0,   66,   46,   71,   63,    0,  // Pawn threats
               0,  -12,   48,   86,   36,    0,  // Knight threats
               0,   31,   -2,   54,   35,    0,  // Bishop threats
               0,   21,   28,  -13,   52,    0,  // Rook threats
               0,   -5,   -6,  -14,  -13,    0,  // Queen threats
               0,    0,    0,    0,    0,    0,  // King threats

            /* opening pawn push threats */
               0,   31,   36,   36,   37,    0,  // Pawn push threats


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game piece values */
            147, 480, 526, 910, 1567, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              -8,    3,    0,  -28,   31,   10,    0,  -22,
             -10,  -15,  -28,  -18,  -19,   -5,   -3,  -11,
              -4,   -5,  -24,  -42,  -29,  -12,    4,  -12,
              40,   22,   14,    3,  -20,   -8,   16,   12,
              40,   57,   48,   49,   57,   -1,   39,   15,
              66,   64,   81,   49,   40,   26,   58,   13,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              17,   33,   16,   43,   -9,  -12,  -13,   -7,
              -8,    3,  -19,  -21,  -31,  -10,  -15,   -4,
              24,    6,   -5,  -28,  -22,   -3,   -5,  -14,
              69,    7,    9,   27,    9,   48,   37,   29,
              30,   -6,  -20,   46,  103,  105,  123,   94,
              25,   31,   11,   81,   93,  174,  131,  108,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
               7,    3,    2,   19,   19,   18,   19,   21,
               6,  -10,  -14,  -23,  -15,  -11,   -3,    7,
               9,    5,    0,  -18,   -9,   -7,   -7,   -3,
              62,   55,   49,   31,   -2,   -4,    8,   31,
              69,  105,  111,   87,   63,   -6,   35,   45,
              77,  113,  160,  120,   47,   20,   31,   34,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              11,   12,   -2,   10,   -3,  -14,  -15,  -22,
               2,    3,  -14,  -21,  -27,  -33,  -26,  -26,
              17,   11,   -8,  -19,  -28,  -33,  -18,  -29,
              28,    8,    7,   25,    0,    0,    0,   -2,
             -16,   -1,  -27,   31,   63,   41,   60,   41,
              15,   37,   -3,   63,   26,  107,   64,   74,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -65, -109,  -40,  -17,  -27,  -27,  -38,  -66,
             -77,  -19,  -29,  -26,  -12,   -6,  -15,  -21,
             -49,  -13,    0,   23,   14,   -5,    0,   -4,
              -8,   24,   41,   45,   50,   45,   40,    7,
              17,   17,   27,   53,   62,   73,   80,   16,
              26,   10,   32,   31,   17,    1,   50,   18,
               6,   23,   29,   29,   34,   -7,   -9,    3,
             -22,   22,   22,    7,   16,    7,    8, -106,

            /* knights: KQ */
            -123,  -42,   -8,   11,  -10,  -22,    6, -124,
             -27,   -2,  -15,  -16,   -3,   12,   18,   -7,
             -23,  -31,    7,    4,    1,  -29,   14,    2,
               0,  -10,   23,    7,   17,   26,   15,   14,
             -37,  -13,    7,    5,    6,   15,    6,    5,
             -42,  -53,  -40,  -31,   -3,   10,   30,   -3,
             -44,  -25,  -22,   -4,   -8,   10,   21,   -2,
            -109,  -37,  -32,  -15,  -44,   31,   31,  -77,

            /* knights: QK */
            -102,   16,   10,   27,   -9,  -32,  -33,  -77,
             -37,    8,   -6,    7,    5,   -8,  -11,  -61,
               2,   -9,  -11,   17,   -4,   10,  -26,  -31,
               6,    5,   23,   47,   11,   25,   -4,  -12,
              20,   19,   23,   -6,   21,   17,    7,  -18,
              30,   17,   39,    0,    0,   -5,   -5,  -27,
              20,    2,   17,   39,   28,    1,  -27,  -32,
             -74,    7,   14,  -13,   22,  -28,  -30, -107,

            /* knights: QQ */
            -105,  -18,   -7,   -1,    2,    4,  -75,  -95,
             -19,   28,   -5,   -5,  -11,  -36,   -1,  -42,
             -20,   -2,  -12,   12,    0,    3,    4,  -40,
              45,   25,   18,   -3,    2,    0,   18,   13,
              14,   28,   13,    2,    4,    2,   -2,   13,
               0,   -4,  -20,  -10,   -9,   23,  -15,  -18,
              32,    1,    9,   37,   20,   24,   11,   33,
             -79,   65,   13,   40,  -21,   28,   46,  -56,

            /* bishops: KK */
             -43,  -47,  -23,   10,    1,    6,  -17,  -73,
             -30,  -20,  -11,   12,    3,   -4,  -17,  -67,
              -7,   11,   31,   22,   41,   19,   -4,  -10,
              15,   27,   39,   18,   10,   17,    0,  -48,
              24,    0,   17,   14,   21,   17,   15,   23,
              20,   22,   26,   21,   35,   34,   20,   50,
              43,   25,   33,   39,   27,   26,   22,   11,
              56,   28,   45,   59,   33,   21,  -17,    7,

            /* bishops: KQ */
              -4,   -7,    9,   12,  -15,   -9,  -71,  -77,
             -13,   -2,   11,    4,    2,  -24,  -13,  -68,
               4,   11,   14,   16,   16,    4,   -7,  -34,
              12,   25,    6,   -6,    9,   27,   25,   13,
              20,   -5,   -5,  -54,    6,   35,   15,   19,
              -2,  -29,    7,   17,   14,   24,   34,   29,
             -40,   -4,    6,   15,    7,   26,   36,   58,
              21,    0,   -5,   21,   27,   29,   26,   35,

            /* bishops: QK */
             -54,  -26,   -1,   21,   13,   -6,   23,  -17,
             -43,  -16,   10,   20,   12,   12,   -3,  -30,
               2,   11,   22,   21,   34,    7,  -13,  -21,
              -1,   24,   39,    6,   -5,   33,   26,   17,
              47,   25,   25,    7,    7,   15,   -6,   21,
              33,   45,   25,   31,   27,   34,  -14,    9,
              46,   39,   40,   44,   30,   17,   -3,  -37,
              31,   30,   35,   51,   11,  -11,    1,   20,

            /* bishops: QQ */
              -3,    0,  -10,   -4,  -14,  -25,  -35,  -77,
             -56,  -22,    7,  -10,   11,  -30,  -17,  -79,
              -6,   -7,   -1,   -8,    0,  -14,  -43,  -20,
               0,   -5,   -1,  -31,  -14,   -3,  -12,  -24,
               4,   -7,   -8,  -23,  -40,   -4,  -19,    7,
              -1,    6,   -5,   -7,   12,  -15,    6,    8,
             -15,    1,   -5,   25,   -9,   -1,   -4,    8,
               5,   -5,   -7,    9,   22,    2,   17,   55,

            /* rooks: KK */
              29,   23,   11,   -9,  -23,   -1,    1,  -19,
              30,   21,    7,   -6,  -20,  -35,  -13,   -8,
              28,   32,    8,   -6,  -25,  -18,  -27,  -12,
              56,   47,   30,   14,   -5,   31,   21,   23,
              71,   55,   36,   14,   22,   19,   21,   29,
              70,   45,   28,    7,   -4,   -1,    8,   12,
              66,   65,   26,    6,   13,   10,   34,   15,
              41,   61,   35,   14,   51,   50,   50,   31,

            /* rooks: KQ */
              -5,  -24,  -40,  -25,  -15,  -23,   27,    7,
               7,  -46,  -31,  -32,  -15,    1,    3,    5,
             -11,  -21,  -26,  -37,  -12,  -17,   -8,   -1,
               8,  -18,    1,  -20,  -19,   32,   17,    9,
               2,  -14,    0,   18,  -12,    5,   20,   33,
              10,   -5,  -31,  -28,  -21,   -4,   -3,   21,
               5,    8,  -19,  -10,  -11,   -6,   15,    0,
              40,   17,   -7,    1,    9,   17,   45,   18,

            /* rooks: QK */
              28,    9,   -2,  -16,  -36,  -37,  -46,  -12,
               6,   -7,   11,  -10,  -24,  -39,  -41,  -32,
             -10,    4,   -2,  -22,  -13,  -27,  -43,  -39,
              16,    5,   11,    0,   -3,    2,  -14,  -13,
               9,   29,   -3,   10,   13,    6,   11,  -19,
              25,    9,   15,    6,  -11,  -10,  -19,   -9,
               5,   13,    4,    4,    0,   -9,   10,    6,
             -82,   12,   10,   17,   19,    3,   13,    5,

            /* rooks: QQ */
              -4,  -16,  -41,  -61,  -42,  -23,    5,   24,
             -29,  -20,  -36,  -50,  -60,  -45,  -23,  -10,
             -11,  -25,  -39,  -46,  -26,  -47,   -9,   -6,
               7,  -17,  -11,  -19,  -27,  -17,   11,   20,
               5,    5,   -1,  -29,   -4,   -7,   32,   19,
             -17,   -2,  -18,  -23,  -36,   -5,    3,   23,
             -11,   -5,    2,  -22,  -52,  -33,   -2,   15,
              29,   22,   24,   16,  -14,   -2,   26,   28,

            /* queens: KK */
             -12,  -26,  -66,  -42, -107, -113,  -80,   -4,
               2,  -14,  -24,  -32,  -46,  -80, -101,  -42,
             -16,  -15,   -8,   -6,  -17,   11,   17,  -32,
              15,   24,   24,   10,    7,   23,   22,   57,
              17,   17,   50,   41,   53,   73,   74,   94,
              24,   45,   56,   65,   67,   78,  107,  126,
              63,   91,   55,   60,   82,   63,  145,  103,
              57,   34,   22,   49,   40,   61,   82,   94,

            /* queens: KQ */
             -21,    5,  -64,    9,   10,  -52,  -48,   18,
              34,    2,  -33,  -10,    1,  -28,  -25,  -33,
             -13,    3,  -38,   -3,    1,   23,    2,  -27,
              35,  -45,   24,  -11,   -9,   31,   36,   27,
               2,   14,    9,   -1,  -10,   30,   39,   75,
               4,    7,   12,    3,   14,   36,   50,   47,
              35,   22,   33,   24,    4,   73,   54,   70,
              42,   62,   54,   82,   88,   30,   34,   42,

            /* queens: QK */
             -63,  -51,  -85,  -32,  -82,  -23,  -25,   17,
             -38,  -53,  -50,  -40,  -34,  -34,  -31,   -4,
              -9,  -13,   -2,    8,  -24,    2,  -34,    2,
             -42,   18,   43,    2,   14,  -16,   -6,   -2,
              23,    6,   58,   44,    7,   14,   21,  -18,
               9,   59,   43,   21,   25,   19,   24,  -21,
             -15,   31,   62,   26,    5,   16,   12,   55,
              33,   23,   31,   35,   16,   38,   38,   52,

            /* queens: QQ */
             -45,  -19,  -15,  -58,   11,  -35,  -30,  -23,
              -8,  -49,  -17,  -49,  -10,  -46,  -40,  -33,
             -49,   16,  -34,  -14,  -24,  -41,  -42,  -25,
             -14,    2,    2,   -3,  -35,  -72,  -24,  -22,
              -5,   33,   -3,   -9,   -2,  -23,  -10,  -39,
              36,   34,   51,    1,   11,   10,    7,   30,
              48,   54,   36,   58,    5,  -10,   29,   18,
             -26,   41,   33,   24,    9,   -3,    2,  -15,

            /* kings: KK */
               0,    0,    0,    0,  -21,    9,  -27,  -56,
               0,    0,    0,    0,    5,    5,   -2,   -9,
               0,    0,    0,    0,   13,   22,   13,    3,
               0,    0,    0,    0,   17,   24,   25,   16,
               0,    0,    0,    0,    6,   -1,   22,   14,
               0,    0,    0,    0,   12,   -6,   28,   18,
               0,    0,    0,    0,    8,   19,   59,   24,
               0,    0,    0,    0,   27,   34,   48, -119,

            /* kings: KQ */
               0,    0,    0,    0,   -7,   -2,  -46,  -56,
               0,    0,    0,    0,  -11,  -11,    1,  -15,
               0,    0,    0,    0,    9,    1,    3,    4,
               0,    0,    0,    0,   11,   30,   37,   25,
               0,    0,    0,    0,   10,   42,   51,   53,
               0,    0,    0,    0,   31,   53,   54,   56,
               0,    0,    0,    0,   13,   17,   45,   38,
               0,    0,    0,    0,    9,    6,   10,  -11,

            /* kings: QK */
             -72,  -76,  -53,  -20,    0,    0,    0,    0,
             -49,  -22,  -29,  -23,    0,    0,    0,    0,
             -16,    1,  -22,   -5,    0,    0,    0,    0,
              25,   25,   16,    6,    0,    0,    0,    0,
              35,   47,   29,   11,    0,    0,    0,    0,
              48,   47,   26,   19,    0,    0,    0,    0,
              26,   25,   37,   16,    0,    0,    0,    0,
              19,   40,   28,   28,    0,    0,    0,    0,

            /* kings: QQ */
              28,  -27,  -13,  -18,    0,    0,    0,    0,
              -9,   -4,  -18,  -23,    0,    0,    0,    0,
              -1,   -6,  -10,  -12,    0,    0,    0,    0,
              -3,    8,   17,   10,    0,    0,    0,    0,
              11,   24,   12,    4,    0,    0,    0,    0,
              33,   39,   -4,   29,    0,    0,    0,    0,
               3,   67,   36,   27,    0,    0,    0,    0,
            -117,   58,   27,   21,    0,    0,    0,    0,

            #endregion

            /* end game mobility weights */

               8, // knights
               5, // bishops
               4, // rooks
               5, // queens

            /* end game squares attacked near enemy king */
              -6, // attacks to squares 1 from king
              -2, // attacks to squares 2 from king
               0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
              12, // # friendly pawns 1 from king
              18, // # friendly pawns 2 from king
              14, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -7,

            /* end game backward pawns */
            -2,

            /* end game doubled pawns */
            -39,

            /* end game adjacent/connected pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               5,    1,    2,   17,    5,  -18,   12,  -22,
              -3,    8,   17,   37,    3,   13,    3,  -15,
              15,   23,   53,   70,   33,   23,   19,    4,
              75,   51,   99,   72,   84,   36,   55,   37,
             112,  119,  184,  162,  137,  151,   79,   41,
             203,  335,  325,  370,  330,  233,  284,  252,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game knight on outpost */
            22,

            /* end game bishop on outpost */
            25,

            /* end game bishop pair */
            105,

            /* end game rook on open file */
            2,

            /* end game rook on half-open file */
            31,

            /* end game rook behind passed pawn */
            46,

            /* end game doubled rooks on file */
            18,

            /* end game king on open file */
            9,

            /* end game king on half-open file */
            25,

            /* end game castling rights available */
            -28,

            /* end game castling complete */
            -16,

            /* end game center control */
               6, // D0
               6, // D1

            /* end game queen on open file */
            26,

            /* end game queen on half-open file */
            17,

            /* end game rook on seventh rank */
            31,

            /* end game passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              34,   39,   31,   47,    0,   12,   48,   13,
              37,   50,   40,   34,   42,   30,   57,   23,
             -21,    1,    3,   -1,    3,    8,   31,    6,
              11,   27,   21,    6,   40,   26,   34,   35,
              78,  100,   82,   62,   52,   61,   66,   62,
              95,  149,  145,  134,  162,  115,  150,  113,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               5,   -4,   -7,  -37,   -3,   -1,  -10,   -5,
             -10,  -12,   -9,  -17,  -15,   -4,   -8,   -9,
             -10,  -23,  -47,  -57,  -36,  -35,  -23,   -7,
             -24,  -38,  -40,  -46,  -37,  -35,  -37,  -25,
             -29,  -46,  -63,  -63,  -71,  -51,  -87,  -21,
             -46,  -83,  -80, -108, -134, -102, -129, -124,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game block passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,  // blocked by pawns
               0,  -16,   -1,   16,   59,   36,   64,    0,  // blocked by knights
               0,    1,   45,   57,  103,  116,  166,    0,  // blocked by bishops
               0,   18,  -25,  -18,   11,   14,  -31,    0,  // blocked by rooks
               0,   25,   18,   37,   15, -138, -186,    0,  // blocked by queens
               0,   14,    2,  -65,  -22,    7,  129,    0,  // blocked by kings

            /* end game supported pawn chain */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,
              10,   31,   30,   29,   38,   15,    8,    0,
               7,   31,   12,   34,   14,    8,    8,   -1,
              18,   32,   35,   35,   33,   19,   21,    9,
              60,   40,   98,   75,   70,   84,   41,    4,
              97,   81,  121,   64,  122,  127,   52,  104,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game king outside passed pawn square */
            196,

            /* end game passed pawn/friendly king distance penalty */
            -19,

            /* end game passed pawn/enemy king distance bonus */
            42,

            /* end game pawn rams */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,   23,   29,   62,    1,   -2,    5,    3,
              24,   12,   19,   28,    1,   27,   17,   -9,
               0,    0,    0,    0,    0,    0,    0,    0,
              -1,  -13,  -15,  -44,  -33,    9,    9,   -9,
             -11,  -14,   -7,  -27,  -56,  -19,  -27,   -7,
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game piece threats */
            /* P     N     B     R     Q     K */
               0,   68,   95,   71,   -2,    0,  // Pawn threats
               0,    2,   27,    1,   37,    0,  // Knight threats
               0,   62,   38,   31,  143,    0,  // Bishop threats
               0,   55,   53,   61,   94,    0,  // Rook threats
               0,   47,   73,   36,   15,    0,  // Queen threats
               0,    0,    0,    0,    0,    0,  // King threats

            /* end game pawn push threats */
               0,   32,  -23,   25,  -32,    0,  // Pawn push threats
        };
    }
}