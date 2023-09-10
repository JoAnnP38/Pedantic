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
        public const int MAX_WEIGHTS = 3756;
        public const int ENDGAME_WEIGHTS = 1878;
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

        // Solution sample size: 12000000, generated on Sat, 09 Sep 2023 18:10:37 GMT
        // Solution error: 0.118752, accuracy: 0.5177, seed: -108001804
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening piece values */
            96, 448, 495, 615, 1491, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -29,  -31,  -28,   -1,  -40,   44,   48,   -6,
             -31,  -33,  -32,  -37,  -30,  -13,    4,  -32,
              -6,   -6,    4,  -12,   12,   23,    9,    4,
               6,   -8,    4,   16,   27,   38,   32,   29,
              -5,   -5,   -7,   44,   33,  140,  147,   69,
              94,  100,   97,  128,   89,  109,   54,   84,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -46,  -60,  -54,  -43,  -41,   40,   55,    9,
             -40,  -61,  -39,  -47,  -21,  -15,   19,  -10,
             -22,   -1,   -7,  -13,    0,   -3,    2,   24,
             -20,   22,   14,   26,   -1,  -15,   16,   35,
              51,   82,  111,   58,   50,   16,   47,   23,
              98,   54,  113,  106,   85,   98,  160,  192,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
               5,   24,    5,  -29,  -48,   -7,  -18,  -39,
              16,   12,  -18,  -28,  -34,  -25,  -38,  -64,
              41,   24,    8,  -13,   -5,    1,   -3,  -15,
              36,   -7,    3,   15,   18,   19,   16,   -4,
              34,    9,   -2,   38,   65,  100,  111,   17,
              89,  142,  127,  152,   87,  113,   52,   54,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -36,    4,   28,    2,   -9,    1,   -1,   -8,
             -31,  -12,  -12,  -36,  -10,  -33,  -30,  -48,
               0,   -6,   23,   -8,   22,    4,   -1,  -14,
              57,   67,   44,   27,   10,   -2,   14,    7,
             127,   90,  132,   82,   -6,   20,   14,   33,
             143,   41,   77,   61,  140,   51,  162,  138,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
            -109,  -19,  -48,  -24,   -2,    0,  -12,  -58,
             -37,  -44,   -3,   14,   12,   25,   -1,    3,
             -23,   -8,    1,   19,   32,    7,    6,    0,
             -10,   11,   13,   14,   27,   34,   36,   17,
              11,   -1,   26,   34,   -3,   19,  -17,   54,
             -25,   44,   44,   62,  103,  157,   49,   -9,
              10,  -11,   25,   73,   40,  116,   17,    7,
            -259,   12,   -5,   78,   50,  -91,   30, -177,

            /* knights: KQ */
            -103,  -46,  -64,  -50,  -37,  -37,  -33,  -29,
             -60,  -33,  -14,   10,  -12,    2,  -69,  -46,
             -48,  -16,   -6,    8,   17,    7,  -28,  -29,
              -2,   26,   28,   45,   26,   17,   10,  -17,
              32,   -5,   29,   38,   61,   56,   18,    8,
              48,   48,  109,  102,   51,   60,    0,    0,
              -5,   -8,   10,   36,   32,   55,  -18,  -56,
            -284,  -23,  -16,   62,  -52,   14,  -41, -114,

            /* knights: QK */
             -83,  -51,  -47,  -79,  -28,  -19,  -61, -131,
             -34,  -30,   10,   -9,  -12,   14,  -59,  -13,
             -23,  -11,   16,   39,   53,  -19,    1,  -62,
              28,   41,   35,   22,   34,   22,   23,  -13,
              10,   30,   56,  109,   49,   32,   12,   58,
              15,   20,   43,   73,  107,  126,   37,   23,
             -23,    8,   38,   86,  -25,   76,   13,   55,
            -193,  -63,  -27,   -1,    3,  -50,  -52,  -70,

            /* knights: QQ */
              -4,  -37,    7,  -72,  -41,  -32,  -48,  -62,
             -35,  -17,  -10,    8,  -22,   -1,   -8, -112,
              -3,  -10,   20,    5,   10,  -28,  -17,  -30,
              13,   24,   52,   50,   37,   24,   39,  -32,
              22,   -5,   27,   55,   42,   42,   28,    4,
              70,   -9,   44,  123,   70,    7,   45,   22,
             -20,   44,   11,    5,   55,   93,  -23,  -32,
            -165,   -5,   15,  -11,    3,   37,   20,  -80,

            /* bishops: KK */
              12,   13,   -5,  -27,    0,    1,    3,   -2,
              15,   12,   11,    1,    8,   18,   40,   22,
              -1,    7,   18,   10,    8,   10,   13,    4,
             -12,   -8,    3,   33,   26,   -7,   -6,   20,
              -7,   -4,   -7,   38,   10,   22,  -12,   -9,
               1,   13,   -5,   -4,    3,   60,   45,   -9,
             -52,  -17,  -16,  -18,  -25,   -6,  -65,  -58,
             -99,  -49,  -24,  -47,  -37,  -67,   -6,  -42,

            /* bishops: KQ */
             -54,   -5,  -34,  -54,    1,   -4,   52,   -8,
             -23,  -13,  -31,   -2,    3,   37,   62,   14,
             -33,   -6,   21,    6,   13,   32,   35,   34,
              10,   -1,   -4,   46,   57,   24,   -1,  -17,
              -2,    8,   -4,   68,   63,   10,    9,    4,
             -16,   70,   88,   -4,   23,   26,   16,   -1,
             -51,   25,  -39,  -32,   -5,  -19,  -34,  -69,
             -27,  -34,   19,  -21,  -32,  -19,   -4,  -45,

            /* bishops: QK */
              10,   12,    6,  -22,  -17,  -27,  -28, -112,
              37,   55,   21,   21,  -14,  -24,  -32,  -23,
              -1,   23,   23,   20,    4,    0,  -12,  -10,
              -5,   13,   15,   57,   38,  -18,  -33,  -19,
             -21,    5,    6,   62,   19,   14,    2,  -29,
             -38,   -5,   42,   16,   39,   51,   62,   15,
             -43,  -52,  -44,  -15,  -47,   -1,  -22,  -22,
             -58,    2,    4,  -27,   23,  -34,   33,  -59,

            /* bishops: QQ */
             -96,  -30,   -9,  -44,  -10,  -24,    4,  -21,
              38,   44,  -24,   19,  -36,   19,  -15,   51,
              12,   27,   23,    1,   22,   13,   26,  -15,
              39,   18,   -1,   44,   17,   37,    4,    5,
              12,   -7,  -10,   46,   35,   -8,    0,   -8,
             -20,   46,   92,   39,    8,   25,   -8,  -13,
             -61,  -18,   30,   29,   71,  -26,   25,  -30,
             -41,   19,  -27,   37,   12,   23,  -12,  -45,

            /* rooks: KK */
             -13,   -1,   -4,    8,   17,   13,   26,   -2,
             -32,  -14,  -11,   -3,   -4,   18,   43,   -6,
             -34,  -41,  -30,  -21,    2,   -2,   26,   10,
             -29,  -25,  -21,  -13,  -16,  -32,   21,  -22,
              -9,    2,    4,   28,   -6,   25,   62,   29,
              15,   46,   52,   50,   79,  131,  139,   86,
              -9,   -9,    6,   50,   19,   88,   99,  123,
              95,   74,   58,   72,   51,   88,  127,  128,

            /* rooks: KQ */
             -22,   22,    0,   -5,  -13,   -2,  -18,  -25,
             -44,    6,  -19,  -23,  -10,  -31,  -35,  -18,
             -20,  -17,  -27,  -26,  -48,  -31,    1,  -39,
             -60,  -14,  -55,  -20,  -27,  -50,  -10,    1,
               3,   33,  -12,    9,   37,   18,   19,    7,
              83,   75,   53,   67,   77,   64,  110,   68,
             -28,   33,   38,    1,   57,   21,   43,   77,
              72,   42,   18,   69,   33,   52,   76,   79,

            /* rooks: QK */
             -63,   -4,  -23,  -13,   -3,   -4,   14,  -21,
             -56,  -34,  -36,  -24,  -51,  -40,  -15,  -12,
             -19,    3,  -30,  -35,  -66,  -63,  -12,  -24,
              17,   27,  -35,  -40,  -55,  -40,   18,   -9,
              61,   17,   40,   -4,  -19,  -10,   15,   15,
              42,   86,   36,   -2,   59,   81,  102,   58,
              34,   64,   65,   14,  -15,   46,   34,   32,
             160,   84,   48,   55,   16,   34,  115,  104,

            /* rooks: QQ */
             -29,   -5,   13,   18,   -2,  -11,  -24,  -36,
             -20,  -20,  -34,   -3,  -32,  -39,  -15,  -17,
             -68,   -2,  -16,  -16,  -51,  -48,  -38,  -45,
             -12,   26,   -8,  -33,  -43,  -32,  -50,   -1,
              20,   -5,    7,   97,    1,   24,    8,   20,
              50,  114,  112,   67,   56,   61,   52,   18,
              85,   43,   22,   51,  111,   48,   43,   25,
              58,   35,    0,   -2,    7,   70,   85,   60,

            /* queens: KK */
               7,   11,   21,   23,   41,   13,  -55,  -35,
              -7,   16,   15,   20,   22,   49,   34,    4,
             -10,   -1,    4,   -7,    7,   -6,    7,    4,
              -5,  -25,  -20,  -28,  -22,  -22,    4,  -32,
              -2,  -18,  -48,  -38,  -49,  -39,  -27,  -22,
             -11,    3,  -39,  -54,   -7,   67,   37,    2,
             -20,  -55,  -36,  -22,  -62,   56,   19,  100,
             -30,   12,   19,    9,   21,   71,   96,   88,

            /* queens: KQ */
              16,    9,   14,    9,   14,    3,  -24,  -26,
              31,   17,   32,   11,   11,    5,  -57,  -47,
              15,   -1,   16,    1,    7,    9,   -6,  -29,
             -10,   29,  -24,  -25,  -17,  -21,  -24,  -14,
               0,   -1,  -17,  -23,    8,  -11,  -10,    8,
              51,   62,   45,    4,   16,    7,   34,    5,
              44,   75,   78,   53,   47,    6,   20,   13,
              73,   81,   80,   74,   46,   54,   34,   -3,

            /* queens: QK */
             -80,  -91,  -18,  -14,   14,   -8,  -25,  -34,
             -60,  -32,   29,   15,   11,   -1,   34,    7,
             -39,  -14,   -1,  -17,    3,  -25,   11,   -4,
             -15,  -37,  -35,  -18,  -32,  -22,    0,   -9,
              -3,  -33,  -39,  -23,  -26,   20,   -5,  -10,
               3,   26,  -21,  -28,  -13,   61,   26,   43,
               4,    1,  -20,  -20,  -24,    2,   58,   60,
             -17,   29,   35,   21,  -21,   79,  102,   52,

            /* queens: QQ */
             -37,  -59,  -26,  -12,   14,  -10,  -31,  -38,
             -50,  -21,   13,   19,  -23,  -20,    8,  -40,
              -6,    0,    8,  -19,  -14,  -24,   -8,  -19,
             -36,   11,  -21,   -2,   -9,  -18,   -4,  -27,
             -12,  -24,   22,   47,   59,   10,  -22,   13,
              47,   53,   99,   47,    8,  -27,    2,   21,
              87,   66,   54,   38,   15,  -11,   18,    5,
              26,   78,   29,   74,   19,   10,   36,   30,

            /* kings: KK */
               0,    0,    0,    0,  -43,  -69,    7,   -2,
               0,    0,    0,    0,  -16,   -1,   17,   -1,
               0,    0,    0,    0,   27,  -15,  -14,  -46,
               0,    0,    0,    0,  101,   81,   39,  -95,
               0,    0,    0,    0,  148,  218,  165,   25,
               0,    0,    0,    0,  241,  290,  265,  141,
               0,    0,    0,    0,  179,  103,  161,   52,
               0,    0,    0,    0,  141,   98,   70,  -18,

            /* kings: KQ */
               0,    0,    0,    0,  -75,  -98,   -3,  -12,
               0,    0,    0,    0,   -1,   -8,    0,  -11,
               0,    0,    0,    0,    2,   28,    2,  -27,
               0,    0,    0,    0,   86,   43,   -9,  -14,
               0,    0,    0,    0,  158,   65,   93,    6,
               0,    0,    0,    0,  131,   84,   94,   26,
               0,    0,    0,    0,  198,  222,   85,   22,
               0,    0,    0,    0,   78,  110,  174,   27,

            /* kings: QK */
             -23,    6,  -35, -111,    0,    0,    0,    0,
              10,   -8,  -24,  -38,    0,    0,    0,    0,
             -53,  -21,    5,  -27,    0,    0,    0,    0,
             -55,   42,   47,   45,    0,    0,    0,    0,
              21,   72,   87,  102,    0,    0,    0,    0,
              18,  111,  129,   95,    0,    0,    0,    0,
              61,  147,   88,  123,    0,    0,    0,    0,
             -21,   53,  133,   24,    0,    0,    0,    0,

            /* kings: QQ */
             -85,  -17,  -58, -107,    0,    0,    0,    0,
             -27,  -28,    2,  -36,    0,    0,    0,    0,
             -48,   -9,    2,    1,    0,    0,    0,    0,
             -14,   37,   52,   66,    0,    0,    0,    0,
              41,  103,  231,  128,    0,    0,    0,    0,
              69,  198,  183,   63,    0,    0,    0,    0,
               7,   90,   54,  101,    0,    0,    0,    0,
             -59,   22,   15,  101,    0,    0,    0,    0,

            #endregion

            /* opening mobility weights */

               8, // knights
               5, // bishops
               2, // rooks
              -1, // queens

            /* opening squares attacked near enemy king */
              24, // attacks to squares 1 from king
              21, // attacks to squares 2 from king
               7, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
              26, // # friendly pawns 1 from king
              11, // # friendly pawns 2 from king
               7, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -7,

            /* opening backward pawns */
            8,

            /* opening doubled pawns */
            -13,

            /* opening adjacent/connected pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              16,   -2,    5,   13,   27,   -3,  -19,   -3,
              13,   18,   14,   14,   49,    9,   14,   12,
              -7,   14,    7,   31,   42,   38,   20,   23,
              31,   41,   55,   43,  112,   98,   41,   44,
              63,  181,  168,  251,  202,  233,  172,  106,
             183,  306,  231,  254,  255,  234,   86,   52,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening knight on outpost */
            10,

            /* opening bishop on outpost */
            17,

            /* opening bishop pair */
            39,

            /* opening rook on open file */
            36,

            /* opening rook on half-open file */
            11,

            /* opening rook behind passed pawn */
            -4,

            /* opening doubled rooks on file */
            8,

            /* opening king on open file */
            -75,

            /* opening king on half-open file */
            -31,

            /* opening castling rights available */
            28,

            /* opening castling complete */
            24,

            /* opening center control */
               3, // D0
               4, // D1

            /* opening queen on open file */
            -10,

            /* opening queen on half-open file */
            9,

            /* opening rook on seventh rank */
            30,

            /* opening passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
             -14,  -18,  -25,  -40,    0,  -13,   -6,   22,
              -5,   -3,  -22,  -17,  -30,  -31,  -37,   15,
             -37,  -40,  -42,   -6,  -37,  -48,  -68,  -38,
               3,   36,   20,   21,   13,   36,   33,   12,
              97,   98,  100,   46,   63,   99,   59,   67,
             169,  160,  141,  150,  186,  158,  168,  159,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -6,   -3,   -9,  -13,  -16,  -22,  -15,    0,
              -6,   -7,  -16,  -11,  -15,  -13,  -15,   -6,
              -8,    7,    0,    0,  -14,   -9,   -2,   -1,
               2,    8,   -2,   -2,    0,   -5,    1,    3,
              20,   12,   21,   14,   30,   13,   56,  -10,
              67,   86,   77,   58,   40,   69,   29,   89,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening block passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,  // blocked by pawns
               0,   60,   67,   62,   62,   74,   88,    0,  // blocked by knights
               0,   46,   41,   22,   17,   67,   95,    0,  // blocked by bishops
               0,  -56,  -19,  -34,  -23,   28,  189,    0,  // blocked by rooks
               0,    5,   46,   17,    8,   24, -118,    0,  // blocked by queens
               0,   11,   52,   50,   82,  202,  211,    0,  // blocked by kings

            /* opening supported pawn chain */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,
              26,   39,   47,   54,   43,   40,   36,   57,
               1,   20,   23,   30,   31,   25,   36,   18,
             -25,   30,   54,   40,   64,   68,   25,   17,
              57,  135,  122,  166,  174,   90,   75,   82,
             139,  252,  322,  212,  358,  212,  183,   -6,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening king outside passed pawn square */
            130,

            /* opening passed pawn/friendly king distance penalty */
            4,

            /* opening passed pawn/enemy king distance bonus */
            1,


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game piece values */
            147, 467, 528, 911, 1559, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              -3,   -1,   -1,  -23,   24,   13,   -8,  -26,
              -7,   -4,  -17,   -6,   -3,   -3,   -1,  -14,
               0,   -3,  -20,  -38,  -25,  -13,    2,  -13,
              25,    8,  -11,  -47,  -28,  -12,    8,    2,
              29,   30,   39,   29,   26,  -13,   20,    5,
              60,   59,   76,   37,   59,   10,   63,   26,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              14,   27,   23,   42,   -9,  -12,  -18,  -16,
               1,   18,  -12,   -5,  -22,  -12,  -13,  -12,
              24,    7,    0,  -23,  -18,    1,   -7,  -19,
              53,   -8,  -18,  -21,   -7,   36,   26,   21,
              22,  -30,  -30,   23,   61,  100,  107,   88,
              20,   17,    2,   67,  116,  165,  145,  120,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
               5,    2,    0,   29,   18,   17,   11,   19,
               5,    7,   -7,   -8,   -1,   -5,    0,    3,
              10,   15,    5,  -16,   -6,   -5,  -10,   -4,
              43,   47,   26,   -7,  -11,  -13,   -5,   19,
              64,   93,  111,   70,   16,   -9,   15,   30,
              76,  102,  151,  102,   60,    6,   38,   49,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              15,    4,   -5,    9,  -17,  -12,  -20,  -17,
              11,   13,  -11,   -6,  -22,  -30,  -21,  -23,
              24,   20,   -8,  -15,  -27,  -32,  -21,  -29,
              17,   -7,  -19,  -15,  -20,  -10,  -13,    1,
             -19,  -23,  -38,   12,   33,   26,   38,   27,
              10,   45,    2,   58,   31,   81,   73,   78,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -67,  -98,  -32,  -20,  -27,  -29,  -33,  -77,
             -69,  -13,  -36,  -37,  -18,  -13,  -14,  -20,
             -40,   -9,   -5,   18,   14,  -11,    1,    3,
               4,   11,   37,   49,   52,   38,   39,   13,
               8,   12,   26,   53,   51,   66,   80,   29,
              35,    6,   21,   14,   -1,   -5,   35,   32,
              -1,   22,   22,   26,   35,  -22,    0,   12,
               5,   14,   29,   12,   27,   11,   13,  -77,

            /* knights: KQ */
             -97,  -42,    0,    9,  -14,  -10,    3, -137,
             -24,    7,  -22,  -24,  -12,    2,   12,  -12,
             -10,  -25,    0,    1,   -2,  -32,   24,   21,
               1,   -5,   15,    2,   21,   24,   21,   20,
             -30,  -17,    1,   14,   -2,    1,   -3,    5,
             -29,  -49,  -45,  -38,    3,    7,   38,    6,
             -29,  -27,  -46,    2,   -3,    7,   11,   11,
            -117,  -31,  -28,  -22,  -18,   33,   38,  -66,

            /* knights: QK */
             -85,  -12,   10,   43,   -8,  -18,  -32,  -60,
             -63,    3,  -16,   -2,   -3,    1,    6,  -58,
              12,  -20,  -16,   13,   -7,    5,  -19,  -21,
               1,   10,   15,   39,   16,   25,    6,    2,
              28,   20,   22,   -5,   11,   27,   11,  -18,
              28,   30,   23,   -4,   -8,  -17,    2,  -23,
              20,    9,   27,   42,   32,    0,  -27,  -24,
             -54,   24,   26,    8,   23,  -26,  -30, -124,

            /* knights: QQ */
             -76,   -8,   -6,   22,    8,    9,  -49,  -81,
             -25,   32,  -10,   -2,   -3,  -30,    5,  -43,
              -5,    4,  -23,   14,    4,   -4,    1,  -32,
              55,   19,   11,    1,    3,   -6,    9,   18,
              17,   20,    3,    1,    6,   -5,  -10,   17,
              -4,    1,  -21,  -13,  -17,   27,    3,  -14,
              48,   -7,    6,   45,   32,    1,   25,   20,
             -72,   71,    5,   47,    2,   31,   70,  -71,

            /* bishops: KK */
             -54,  -54,  -36,    7,   -9,   -4,  -37,  -71,
             -37,  -24,  -16,    0,   -4,  -13,  -38,  -71,
             -13,   15,   28,   12,   38,   12,  -14,  -12,
              16,   29,   41,   17,    6,   20,    5,  -32,
              25,   11,   17,    8,   20,   18,   21,   25,
              13,   20,   16,   19,   30,   37,   14,   44,
              37,   22,   27,   31,   31,   16,   23,   17,
              51,   44,   37,   55,   43,   23,   -2,   19,

            /* bishops: KQ */
              -5,    8,    2,   14,   -8,  -20,  -70,  -75,
             -19,   -4,   22,    2,    3,  -28,  -18,  -73,
              12,   16,    8,    7,   13,   -1,   -6,  -33,
              14,   22,   11,   -7,   -1,   25,   22,   21,
              25,    7,    7,  -31,    3,   28,   20,   13,
              -1,  -17,  -15,   19,   10,   16,   27,   36,
             -26,  -20,   15,   19,    9,   21,   49,   64,
              15,   10,   -1,   22,   26,   28,   33,   24,

            /* bishops: QK */
             -55,  -30,   -3,   16,    7,   -6,   15,  -21,
             -45,  -25,    3,    4,   10,    4,   -9,  -16,
              -2,   19,   22,   19,   36,    3,    0,  -26,
               2,   21,   40,    9,   -2,   36,   30,    9,
              41,   25,   22,    2,    6,   24,    0,   27,
              51,   38,   12,   28,   24,   26,  -23,   -7,
              44,   40,   44,   44,   43,   29,  -10,  -42,
              28,   13,   38,   39,    2,   -4,   -2,   20,

            /* bishops: QQ */
               0,  -10,  -12,  -14,  -20,  -24,  -36,  -68,
             -48,  -28,    0,  -13,   11,  -33,  -22,  -79,
             -11,  -11,   -7,  -12,   -2,  -14,  -29,  -15,
              -8,    6,   -2,  -22,  -17,   -4,  -10,  -10,
              -1,    7,  -10,  -17,  -34,  -11,  -11,    3,
              -3,    6,  -26,   -1,   13,  -14,    2,    2,
              -8,   -7,   -8,    8,  -25,   -2,   -4,   18,
               5,   -6,   -6,   12,   22,    2,   29,   53,

            /* rooks: KK */
              29,   26,   14,   -9,  -24,   -3,   -4,  -19,
              31,   19,    9,  -15,  -22,  -38,  -20,    0,
              31,   40,   14,   -4,  -31,  -17,  -19,  -22,
              59,   46,   32,    9,   -1,   24,   12,   19,
              68,   55,   38,   15,   28,   18,   22,   23,
              70,   48,   33,   15,    3,   12,    8,   15,
              62,   66,   36,   -2,   21,   15,   34,   13,
              40,   59,   34,   10,   44,   47,   51,   25,

            /* rooks: KQ */
              -5,  -30,  -27,  -23,  -19,  -17,   22,   16,
               0,  -47,  -36,  -27,  -22,   -6,   19,   -6,
             -17,  -26,  -38,  -29,  -19,   -9,   -3,    1,
              17,   -9,    0,  -12,  -14,   25,   11,    2,
               4,   -1,   -3,    3,  -16,   12,   24,   31,
              -3,   -3,  -26,  -29,  -34,   -4,   -7,   12,
              30,    8,  -21,   -1,  -16,   -2,   24,    5,
              23,   23,    7,  -18,    1,   12,   38,   20,

            /* rooks: QK */
              29,    8,   11,  -14,  -34,  -35,  -37,   -6,
              23,    6,    8,  -27,  -22,  -28,  -36,  -29,
               2,    7,   -3,  -17,  -11,  -15,  -45,  -40,
              -5,    5,   21,    9,    7,   -2,  -17,  -14,
              15,   41,    9,   15,    8,   10,    1,  -20,
              27,   16,   30,   18,  -11,   -6,  -10,    2,
              18,   13,    4,    9,   13,    5,   11,    6,
             -69,    8,   20,   10,   23,   12,    5,   -6,

            /* rooks: QQ */
             -18,  -25,  -50,  -65,  -52,  -13,    3,   29,
             -35,  -20,  -29,  -63,  -58,  -47,  -34,  -10,
              -6,  -17,  -38,  -49,  -13,  -37,   -3,    7,
              -6,  -16,  -22,  -15,  -34,  -16,   10,    8,
               5,   14,   -6,  -46,   -7,   -3,    8,   17,
              -2,   -7,  -13,  -26,  -34,   -4,    9,   24,
             -13,    5,   15,  -24,  -55,  -30,    3,   19,
              22,   36,   26,   25,  -13,   -9,   24,   18,

            /* queens: KK */
              -5,  -43,  -70,  -40, -118, -116,  -75,  -22,
              17,  -10,  -21,  -41,  -46,  -99,  -86,  -45,
              -5,   -6,   -5,   -8,  -17,   16,    6,  -29,
              15,   31,   12,   27,   13,   44,   32,   79,
              11,   38,   61,   46,   78,  109,  118,  125,
              31,   61,   69,   73,   78,   85,  117,  147,
              58,  105,   86,   53,  106,   66,  177,   82,
              63,   58,   32,   50,   60,   77,   98,  110,

            /* queens: KQ */
             -14,   10,  -52,   -8,  -20,  -52,  -34,   18,
              19,    0,  -53,  -15,  -13,  -39,  -13,  -48,
              -2,   -6,  -50,  -14,   -2,   27,   10,  -22,
              48,  -38,    1,  -10,   10,   26,   59,   32,
               1,   13,    2,    1,   -8,   30,   42,   64,
               2,   33,    3,   16,   22,   47,   66,   60,
              36,   35,   23,   12,    7,   61,   47,   55,
              32,   74,   41,   74,   58,   37,   38,   27,

            /* queens: QK */
             -59,  -28,  -82,  -47,  -76,  -54,  -19,   14,
             -43,  -74,  -73,  -46,  -45,  -47,  -40,    2,
             -54,   -5,   -4,   11,  -10,   -7,  -39,    4,
             -22,   13,   38,    9,   16,   -8,   -8,   -6,
               8,   18,   58,   23,   18,   -4,   20,    5,
               3,   39,   55,   47,   49,   28,   40,   23,
              -5,   42,   67,   36,   37,   41,    3,   42,
              33,   34,   28,   44,   30,   42,   37,   40,

            /* queens: QQ */
             -52,  -51,  -13,  -73,   -4,  -34,  -49,  -11,
             -19,  -62,  -48,  -43,   -5,  -60,  -44,  -46,
              12,    6,  -27,  -16,  -34,  -15,  -43,  -24,
              -7,   17,   -2,  -21,  -16,  -59,  -30,   30,
               3,   48,   -4,  -26,  -30,  -26,    8,  -50,
              29,   33,   61,   19,    5,    5,    5,   61,
              35,   71,   52,   76,   20,    7,   21,   -8,
             -23,   69,   38,   35,   14,   -6,   -1,  -22,

            /* kings: KK */
               0,    0,    0,    0,  -22,   10,  -20,  -48,
               0,    0,    0,    0,    6,    8,    4,   -3,
               0,    0,    0,    0,   14,   22,   16,    2,
               0,    0,    0,    0,   20,   23,   27,   35,
               0,    0,    0,    0,   12,   -8,   16,   18,
               0,    0,    0,    0,    3,   -4,   23,   18,
               0,    0,    0,    0,   16,   28,   55,   25,
               0,    0,    0,    0,   13,   40,   49, -121,

            /* kings: KQ */
               0,    0,    0,    0,  -11,    3,  -40,  -46,
               0,    0,    0,    0,  -14,  -10,    0,  -14,
               0,    0,    0,    0,    9,   -3,    4,  -10,
               0,    0,    0,    0,   15,   34,   46,   29,
               0,    0,    0,    0,   12,   49,   49,   58,
               0,    0,    0,    0,   25,   55,   57,   61,
               0,    0,    0,    0,   11,    6,   44,   39,
               0,    0,    0,    0,    8,    5,  -27,    2,

            /* kings: QK */
             -73,  -78,  -56,  -26,    0,    0,    0,    0,
             -53,  -23,  -32,  -30,    0,    0,    0,    0,
             -22,   -6,  -21,   -7,    0,    0,    0,    0,
              23,   14,   12,    4,    0,    0,    0,    0,
              28,   43,   20,    9,    0,    0,    0,    0,
              39,   40,   29,   22,    0,    0,    0,    0,
              12,   24,   29,   17,    0,    0,    0,    0,
              -4,   34,   10,   33,    0,    0,    0,    0,

            /* kings: QQ */
              23,  -23,  -13,  -18,    0,    0,    0,    0,
              -6,   -5,  -24,  -26,    0,    0,    0,    0,
             -10,  -11,  -13,  -15,    0,    0,    0,    0,
              -8,   11,   12,    6,    0,    0,    0,    0,
               3,   22,   -4,    5,    0,    0,    0,    0,
              21,   28,   -6,   31,    0,    0,    0,    0,
               1,   66,   35,   26,    0,    0,    0,    0,
            -128,   63,   21,   28,    0,    0,    0,    0,

            #endregion

            /* end game mobility weights */

              10, // knights
               5, // bishops
               4, // rooks
               6, // queens

            /* end game squares attacked near enemy king */
              -5, // attacks to squares 1 from king
              -3, // attacks to squares 2 from king
               0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
              12, // # friendly pawns 1 from king
              18, // # friendly pawns 2 from king
              14, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -10,

            /* end game backward pawns */
            2,

            /* end game doubled pawns */
            -41,

            /* end game adjacent/connected pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               2,   -2,    0,   11,    8,  -22,   18,  -24,
               6,  -12,   25,   22,   -2,    9,    5,  -25,
               5,   25,   44,   66,   35,   19,   16,    5,
              71,   54,  104,   79,   84,   31,   57,   41,
              94,  127,  159,  168,   97,  179,   65,   28,
             236,  445,  350,  466,  390,  293,  317,  248,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game knight on outpost */
            30,

            /* end game bishop on outpost */
            27,

            /* end game bishop pair */
            112,

            /* end game rook on open file */
            6,

            /* end game rook on half-open file */
            37,

            /* end game rook behind passed pawn */
            46,

            /* end game doubled rooks on file */
            11,

            /* end game king on open file */
            10,

            /* end game king on half-open file */
            27,

            /* end game castling rights available */
            -32,

            /* end game castling complete */
            -20,

            /* end game center control */
               7, // D0
               5, // D1

            /* end game queen on open file */
            33,

            /* end game queen on half-open file */
            28,

            /* end game rook on seventh rank */
            29,

            /* end game passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              32,   42,   31,   28,   13,   12,   41,   21,
              31,   38,   32,   18,   29,   30,   54,   24,
             -26,   -4,   -3,  -11,   -1,    6,   35,    8,
              22,   38,   37,   44,   52,   30,   42,   35,
              85,  119,   87,   77,   80,   68,   81,   69,
              98,  150,  149,  142,  135,  129,  134,   99,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               4,    2,    1,  -31,   -1,    0,   -8,   -6,
              -6,  -10,  -10,  -12,  -21,   -7,   -6,   -8,
             -10,  -26,  -46,  -57,  -41,  -32,  -28,   -8,
             -26,  -40,  -36,  -47,  -43,  -32,  -34,  -22,
             -36,  -56,  -69,  -70,  -76,  -64,  -96,  -21,
             -58,  -96, -105, -108, -138,  -98, -129, -125,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game block passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,  // blocked by pawns
               0,  -22,    6,   11,   52,   36,   62,    0,  // blocked by knights
               0,    0,   41,   48,  105,  114,  173,    0,  // blocked by bishops
               0,   20,  -25,  -19,    5,   -3,  -49,    0,  // blocked by rooks
               0,  -16,    4,   11,   -5, -159, -165,    0,  // blocked by queens
               0,    2,   11,  -63,  -19,    9,  131,    0,  // blocked by kings

            /* end game supported pawn chain */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,
              14,   20,   32,   46,   35,   23,    6,    3,
              -2,   24,    9,   34,   10,    7,    5,   -2,
              13,   22,   40,   32,   26,   12,   26,   12,
              67,   40,   98,   70,   68,   79,   28,   13,
              85,   65,  123,   64,  113,  111,   63,   93,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game king outside passed pawn square */
            199,

            /* end game passed pawn/friendly king distance penalty */
            -17,

            /* end game passed pawn/enemy king distance bonus */
            40,
        };
    }
}