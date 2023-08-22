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
        public const int MAX_WEIGHTS = 3402;
        public const int ENDGAME_WEIGHTS = 1701;
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
        //public const int KING_ADJACENT_OPEN_FILE = 1557; (-Elo)
        public const int UNUSED = 1556;
        public const int KNIGHT_OUTPOST = 1557;
        public const int BISHOP_OUTPOST = 1558;
        public const int BISHOP_PAIR = 1559;
        public const int ROOK_ON_OPEN_FILE = 1560;
        public const int ROOK_ON_HALF_OPEN_FILE = 1561;
        public const int ROOK_BEHIND_PASSED_PAWN = 1562;
        public const int DOUBLED_ROOKS_ON_FILE = 1563;
        public const int KING_ON_OPEN_FILE = 1564;
        public const int KING_ON_HALF_OPEN_FILE = 1565;
        public const int CASTLING_AVAILABLE = 1566;
        public const int CASTLING_COMPLETE = 1567;
        public const int CENTER_CONTROL = 1568;
        public const int QUEEN_ON_OPEN_FILE = 1570;
        public const int QUEEN_ON_HALF_OPEN_FILE = 1571;
        public const int ROOK_ON_7TH_RANK = 1572;
        public const int PASSED_PAWN = 1573;
        public const int BAD_BISHOP_PAWN = 1637;

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

        // Solution sample size: 12000000, generated on Mon, 21 Aug 2023 11:14:30 GMT
        // Solution error: 0.117484, accuracy: 0.5192
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening piece values */
            106, 434, 495, 607, 1514, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -26,  -38,  -25,   -9,  -21,   24,   10,  -14,
             -21,  -22,  -13,  -14,  -10,    5,   13,    0,
             -18,  -15,    3,   12,   22,   27,   -1,   -9,
             -13,    3,   25,   23,   45,   56,   19,   12,
             -10,   -1,    9,   38,   56,  127,  115,   50,
              74,  133,  107,   93,   61,   32,  -65,  -31,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -31,  -64,  -43,  -59,  -27,   23,    5,   -7,
             -31,  -44,  -23,  -26,    1,    2,   15,    8,
             -14,    3,    2,   10,    9,   -3,    3,    9,
               7,   25,   52,   24,   16,    7,    0,   -2,
              72,   85,  103,   25,   54,   21,   31,   56,
              16,  -51,   35,   64,  124,  101,  242,  145,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
               7,   22,    1,  -16,  -44,  -29,  -49,  -49,
              17,   28,    0,  -13,  -21,  -10,  -27,  -37,
              18,   11,   -5,    9,   10,   14,    1,  -13,
             -10,   11,   27,   20,   34,   47,   26,    4,
              47,   31,   -2,   43,   76,   95,  112,   33,
             121,  210,  156,  143,   88,   75,   -5,  -36,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -17,    6,   21,  -19,  -24,  -20,  -36,  -28,
             -17,   -8,   -2,  -23,    4,  -18,  -23,  -18,
             -14,   -9,   22,   12,   23,   10,    7,  -18,
              45,   56,   67,   18,   17,   26,   -8,   12,
              59,   88,   39,   19,   -9,   10,   11,   21,
              56,   19,    8,   47,   21,   58,  165,  167,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
            -102,  -12,  -33,  -17,    1,    5,   -5,  -30,
             -41,  -29,  -13,   14,   12,   13,    3,    4,
             -24,   -5,   -7,   32,   39,    5,   15,    2,
              -2,    3,   18,   22,   39,   29,   34,   22,
               8,    1,   37,   40,   -2,   25,  -18,   40,
               1,   31,   47,   55,  100,  143,   14,  -14,
              -1,  -18,   18,   35,   34,   90,   -1,   29,
            -194,  -40,    2,   20,   48,  -89,   25, -121,

            /* knights: KQ */
            -102,  -41,  -24,  -29,  -20,  -17,  -38,  -62,
             -30,  -16,    3,   12,    2,  -13,  -27,  -20,
             -34,   -2,  -10,   32,   24,    6,   -9,  -27,
               0,   18,   26,   61,   31,   42,   12,    3,
              28,    0,   34,   76,   74,   58,   23,   12,
              10,   40,   83,   87,   71,   43,   -5,    5,
             -13,  -13,   -1,   25,   38,   49,   21,  -42,
            -220,   -8,   -9,  -20,   66,    1,   -6, -181,

            /* knights: QK */
             -79,  -44,  -21,  -34,  -33,  -17,  -44, -104,
             -48,  -18,   -4,   -2,    0,  -13,  -14,  -25,
             -33,   -5,   20,   41,   72,  -20,   -1,  -33,
               8,   43,   36,   36,   37,   25,   26,    1,
              -7,   32,   67,  100,   54,   46,   19,   33,
               6,   19,   30,   56,  102,   69,    6,   -3,
              -9,  -33,   34,   32,   50,   12,  -10,   32,
            -103,  -22,   -4,   47,   -2,  -33,  -22, -107,

            /* knights: QQ */
              31,  -26,  -19,  -41,  -18,  -27,  -31,  -98,
              17,   15,    2,   14,   -8,    5,  -10,  -54,
             -14,    1,    6,   25,   30,  -27,    1,    0,
              10,   31,   67,   65,   26,   41,    9,  -17,
               5,    6,   54,   40,   68,   59,   10,   13,
               5,   19,   70,  106,   82,   57,   37,   15,
             -16,   67,   -3,    3,   36,  -10,  -11,  -55,
            -111,   -3,  -33,  -33,  -31,    8,   -6,  -95,

            /* bishops: KK */
             -24,   13,  -12,  -35,  -15,   -1,   -6,   -4,
              12,    2,   14,   -2,    7,   19,   30,   12,
             -13,    7,   15,   13,    4,    9,    6,   -6,
             -10,  -17,   -2,   36,   18,  -13,  -20,   13,
              -6,   -5,   -5,   48,   14,   13,  -15,  -12,
               2,   10,   -5,  -11,   33,   35,   32,  -15,
             -42,  -23,  -15,   -9,  -30,  -27,  -77,  -48,
             -72,  -56,  -28,  -37,  -20,  -87,   -3,  -34,

            /* bishops: KQ */
             -49,  -57,  -34,  -33,    1,   -2,   33,  -47,
              -4,  -15,  -15,   -4,    9,   41,   59,   15,
             -40,  -13,   11,   23,   19,   37,   32,   29,
               7,  -27,    8,   36,   58,   16,    9,   -3,
             -27,    2,   44,   24,   54,   23,   -1,  -18,
              -4,   40,   57,   29,   13,   27,   11,  -20,
             -48,   18,  -11,  -18,    7,  -21,  -65,  -70,
             -31,  -37,    5,   10,  -37,  -25,  -39,  -24,

            /* bishops: QK */
             -25,   27,  -17,   -5,  -34,  -24,  -47,  -64,
              19,   32,   13,   17,   -4,  -12,  -30,  -26,
              -4,   29,   31,   33,    3,   10,   -5,   -7,
              -4,   35,   16,   64,   53,  -23,   -9,  -10,
               5,   15,   35,   60,   33,   22,   -6,   -1,
             -41,   14,    7,   15,   31,   21,   34,   15,
             -42,  -24,  -27,  -15,   -3,  -12,  -24,  -52,
             -63,  -31,  -10,  -41,  -18,  -25,   48,  -46,

            /* bishops: QQ */
             -49,  -68,   19,  -60,  -34,   -9,  -17,    0,
              -8,   12,   -2,   12,  -24,   -1,  -11,   23,
               2,   17,   -8,   25,   17,   12,   14,    3,
               9,   21,   13,   46,   34,   26,   13,  -10,
             -10,   13,   18,   87,   34,    4,    3,  -35,
             -39,   20,   23,   28,    7,   27,    0,  -21,
             -66,   -1,   19,  -16,    5,  -28,  -37,  -21,
             -23,   -9,   40,  -14,   22,   -2,   -3,  -17,

            /* rooks: KK */
             -14,   -1,   -1,    9,   14,   13,   27,   -3,
             -26,  -20,  -13,   -1,   -8,   20,   38,    2,
             -31,  -27,  -23,  -12,   -3,   -2,   29,   11,
             -28,  -24,  -23,   -3,  -23,  -37,   28,  -27,
             -15,   -2,    4,   24,   -4,   18,   72,   18,
              17,   54,   53,   41,   64,  120,  120,   68,
             -16,  -15,    2,   30,   10,   95,   54,  124,
              74,   82,   73,   72,   34,   73,  120,  114,

            /* rooks: KQ */
             -14,   19,    5,   -7,   -6,    3,    4,  -22,
             -53,   -9,  -33,  -26,  -23,  -25,  -25,  -36,
             -53,  -18,  -43,  -12,  -54,  -51,   -1,  -25,
             -65,  -24,  -46,  -29,  -28,  -57,   22,  -34,
              -1,   28,  -24,    2,   11,   11,   31,    9,
              41,   86,   43,   58,   61,   50,   49,   51,
               2,   13,   19,   18,   32,    6,   47,   51,
              69,   39,   23,   51,   53,   66,   74,  105,

            /* rooks: QK */
             -50,   -3,  -17,  -11,  -10,   -2,   27,  -14,
             -23,  -31,  -31,  -25,  -63,  -24,   -6,  -10,
              -6,  -15,  -12,  -23,  -59,  -40,    3,  -16,
               4,   22,  -23,  -23,  -59,  -37,  -19,  -16,
              26,   -2,   27,    8,   -9,   11,   30,   31,
              51,   65,   64,   12,   43,   83,   98,   52,
              29,   34,   26,    2,   -3,   75,   20,   39,
              93,   58,   61,   16,   -6,   37,   80,   50,

            /* rooks: QQ */
             -26,   -9,   -3,    9,    5,   -8,  -18,  -25,
             -71,  -67,  -46,    7,   -8,  -51,  -11,  -59,
             -77,   -4,  -22,  -34,  -50,  -25,  -35,   -6,
             -15,   10,  -59,  -52,  -21,    2,  -65,  -11,
              27,   35,  -16,   37,   16,   13,  -13,  -48,
              87,   77,   78,   35,   11,   43,   70,   18,
              60,   33,   22,   -2,   69,   33,    9,   21,
              17,   42,   76,    7,   58,   31,  100,   36,

            /* queens: KK */
               2,   11,   17,   20,   37,    7,  -64,  -38,
               0,   16,   16,   15,   19,   46,   27,    0,
             -11,    1,    9,  -12,   -2,    1,    6,    1,
              -2,  -21,  -25,  -31,  -15,  -32,   -3,  -25,
             -12,  -12,  -39,  -39,  -34,  -35,  -22,  -30,
             -14,   -8,  -42,  -47,  -13,   50,   26,    1,
             -40,  -66,  -13,   -8,  -67,   41,   -8,  106,
             -22,   11,   -2,   20,   21,   88,  102,   73,

            /* queens: KQ */
              17,    6,   17,   16,   23,   -1,  -60,  -68,
              25,   23,   33,    3,   15,   11,  -47,  -45,
              -8,    4,   10,   -8,    6,   13,  -25,   -8,
              -3,   18,  -34,   -8,    5,  -11,  -41,  -23,
               4,    0,  -36,    9,    7,   10,   11,    1,
              31,   56,   32,   42,   15,   27,    8,   13,
              51,   85,   41,   27,    1,   16,   20,   10,
              77,   89,   61,   71,   39,   12,   64,  -27,

            /* queens: QK */
             -58,  -57,  -22,   -4,    3,  -29,   17,  -13,
            -113,  -33,   33,   20,   19,    1,   23,   21,
             -42,   -8,   -7,  -12,   -1,  -12,  -24,    6,
             -15,  -24,  -69,  -23,  -39,  -13,   26,    9,
             -18,  -33,  -39,  -24,  -35,  -27,    7,   11,
               4,   -8,   -1,  -26,   15,   36,    9,   54,
             -24,  -17,   12,    5,   29,  -14,    9,   43,
             -13,   33,   41,   38,   29,  102,   55,   47,

            /* queens: QQ */
             -67,  -58,  -12,  -25,   27,   12,    1,  -19,
             -52,  -29,   17,   24,  -11,  -25,   -3,  -28,
             -13,   17,   22,    0,  -12,   -7,   -2,    4,
             -23,   21,  -26,  -49,  -10,  -13,   -9,  -18,
               0,  -16,   -5,   18,   23,   -8,    1,   21,
              39,   32,   45,   31,   18,  -17,   12,    4,
              84,  110,  101,   51,   15,   26,   -6,  -30,
              26,   64,   64,   36,   29,   32,   78,   -5,

            /* kings: KK */
               0,    0,    0,    0,  -54,  -70,    8,   -8,
               0,    0,    0,    0,  -10,   -3,   18,   -5,
               0,    0,    0,    0,   40,    6,    1,  -67,
               0,    0,    0,    0,  121,   76,   34,  -59,
               0,    0,    0,    0,  202,  214,  113,   34,
               0,    0,    0,    0,  203,  236,  255,   88,
               0,    0,    0,    0,  162,  169,  124,   93,
               0,    0,    0,    0,   23,   76,  113,   24,

            /* kings: KQ */
               0,    0,    0,    0,  -82,  -90,   12,  -22,
               0,    0,    0,    0,   -8,    1,    3,    4,
               0,    0,    0,    0,   29,   25,   -2,  -51,
               0,    0,    0,    0,  102,   65,   31,   14,
               0,    0,    0,    0,  168,  102,   85,   26,
               0,    0,    0,    0,  113,   94,  101,   38,
               0,    0,    0,    0,  208,  214,  135,   52,
               0,    0,    0,    0,   43,   49,   67,   -1,

            /* kings: QK */
             -33,   17,  -33, -103,    0,    0,    0,    0,
              -3,  -14,  -17,  -45,    0,    0,    0,    0,
             -53,   -5,   -1,  -25,    0,    0,    0,    0,
              -9,   20,   74,   50,    0,    0,    0,    0,
              25,   58,   72,  174,    0,    0,    0,    0,
              21,  105,  106,  130,    0,    0,    0,    0,
              70,  144,   88,  146,    0,    0,    0,    0,
             -11,   59,   96,   42,    0,    0,    0,    0,

            /* kings: QQ */
            -112,  -25,  -60, -100,    0,    0,    0,    0,
             -60,   -7,   -5,  -42,    0,    0,    0,    0,
             -82,  -10,   12,   12,    0,    0,    0,    0,
               5,   62,   45,   54,    0,    0,    0,    0,
              -6,   79,  138,  147,    0,    0,    0,    0,
              63,  109,  102,   69,    0,    0,    0,    0,
             -30,   62,   49,   41,    0,    0,    0,    0,
             -14,   14,   32,  106,    0,    0,    0,    0,

            #endregion

            /* opening mobility weights */

            9, // knights
            5, // bishops
            2, // rooks
            -1, // queens

            /* opening squares attacked near enemy king */
            26, // attacks to squares 1 from king
            21, // attacks to squares 2 from king
            6, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
            23, // # friendly pawns 1 from king
            15, // # friendly pawns 2 from king
            6, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -9,

            /* opening backward pawns */
            -18,

            /* opening doubled pawns */
            -11,

            /* opening adjacent/connected pawns */
            9,

            /* UNUSED (was opening king adjacent open file) */
            0,

            /* opening knight on outpost */
            7,

            /* opening bishop on outpost */
            7,

            /* opening bishop pair */
            35,

            /* opening rook on open file */
            36,

            /* opening rook on half-open file */
            12,

            /* opening rook behind passed pawn */
            -4,

            /* opening doubled rooks on file */
            9,

            /* opening king on open file */
            -67,

            /* opening king on half-open file */
            -33,

            /* opening castling rights available */
            29,

            /* opening castling complete */
            21,

            /* opening center control */
            5, // D0
            3, // D1

            /* opening queen on open file */
            -4,

            /* opening queen on half-open file */
            8,

            /* opening rook on seventh rank */
            28,

            /* opening passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              -7,   -8,   -3,  -73,   -9,    5,   33,   35,
               0,    1,   -7,  -18,  -40,  -44,  -26,   31,
              15,   28,    2,    4,  -14,  -29,  -22,    5,
              65,   72,   41,   41,   24,   39,   38,   43,
             157,  116,  120,   76,   55,   98,   41,   23,
             151,  109,  118,  141,  119,  125,   74,   96,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -7,   -5,  -13,    1,  -17,  -18,  -19,   -4,
              -7,   -6,  -16,   -4,  -15,  -13,  -17,  -17,
              -1,   -1,   -3,   -2,  -14,   -2,   -1,   -3,
               7,    2,   -5,    7,    0,   -3,    9,   -2,
              15,   28,   27,   12,   42,   19,   51,   -4,
              62,   56,   34,   51,   44,   44,   13,   72,
               0,    0,    0,    0,    0,    0,    0,    0,


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game piece values */
            146, 412, 463, 817, 1363, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
               2,    3,   -4,  -42,    9,    6,  -13,  -31,
              -4,   -5,   -8,   -1,   -1,   -3,  -16,  -29,
               5,   11,  -11,  -23,  -25,  -14,    2,  -22,
              36,   19,    8,   -6,  -18,   -7,   10,   -9,
              50,   54,   48,   40,   11,    1,   14,  -29,
             121,  109,   85,   48,   38,    1,   29,   -4,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
               4,   30,   24,   50,   -8,  -17,  -28,  -14,
               5,   13,   13,    9,  -19,  -14,  -25,  -24,
             -11,   -1,   -4,  -12,  -12,    7,    4,    0,
             -10,  -19,  -31,   -1,   16,   59,   55,   56,
             -59,  -46,  -60,   25,   82,  136,  172,  126,
             -26,    4,  -56,    6,  121,  198,  181,  186,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -23,  -36,  -13,   -8,   17,   21,   28,   28,
             -13,  -29,  -12,   -8,    3,    3,    7,   11,
               2,   19,   15,   -4,    2,   -2,   -6,   -8,
              64,   62,   52,   22,    3,   -9,   -1,   -2,
              82,  126,  132,   90,   17,   -2,   -8,  -17,
             130,  152,  168,  104,   56,  -27,  -11,    9,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -23,  -28,  -13,   -7,   -2,   -4,   -8,   10,
             -19,  -13,    1,   -4,  -18,  -17,  -27,  -10,
             -17,    1,   -8,   -9,   -9,  -21,  -13,    1,
             -25,  -21,  -21,    5,   -2,    9,   15,   22,
             -60,  -41,  -40,   20,   54,   67,   90,   72,
              -5,   40,  -34,    1,   92,  103,  133,  131,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -35,  -81,  -30,  -16,  -25,  -29,  -52,  -49,
             -50,   -9,  -18,  -28,  -18,   -8,   -9,  -13,
             -22,   -7,    4,   17,   15,  -13,   -4,    8,
               9,   19,   39,   53,   48,   42,   47,   13,
              22,   19,   22,   52,   59,   60,   71,   43,
              29,   14,    9,   18,    9,    1,   53,   39,
              19,   32,   11,   41,   40,   -5,   21,   10,
              34,   46,   33,   25,   25,   13,   38, -117,

            /* knights: KQ */
             -68,  -34,  -17,    2,  -14,  -11,   12,  -98,
             -32,    9,  -27,  -15,  -15,    5,   10,  -13,
             -20,  -21,   12,    1,   11,  -19,   -5,   21,
              25,    4,   24,   -4,   31,    0,    3,   20,
             -14,    7,   16,   -1,    2,    8,    9,    2,
             -22,  -33,  -35,  -17,  -12,    2,   28,   14,
             -13,  -33,  -39,   -1,   -3,    4,   16,   41,
             -75,  -18,  -25,   12,  -27,   20,   13,  -59,

            /* knights: QK */
             -98,   -9,  -18,   26,    6,  -17,  -15,  -39,
             -34,   32,   -3,    2,    6,    6,  -17,  -21,
              40,   -3,   -8,   12,  -14,    9,  -15,  -30,
              32,   16,   22,   41,   22,   22,    4,   -5,
              29,   22,   15,   10,   21,   11,   10,  -17,
              55,   31,   34,    2,    1,    8,   -2,    1,
              31,   37,   28,   41,   37,  -19,   -3,  -36,
             -79,   47,   42,   29,   14,  -43,   -1, -116,

            /* knights: QQ */
             -78,   -1,  -17,   32,    1,   -3,  -51,  -48,
             -16,   18,  -27,  -16,  -12,  -33,   -9,  -61,
               8,    2,  -18,   -1,   14,    4,   -5,  -23,
              22,   23,   13,   -2,   14,    0,    8,   20,
              23,    5,  -11,   22,   -4,   -7,    8,    5,
              34,   -6,   -6,   -9,  -18,    9,   -4,   26,
              27,   24,   -7,   51,    2,   27,   29,    6,
             -96,   41,  -30,   51,    4,   23,   19,  -31,

            /* bishops: KK */
              -7,  -25,  -37,   26,    8,    5,  -29,  -44,
               4,  -16,  -10,   13,    1,   -8,  -23,  -47,
              13,   17,   32,   29,   45,   15,   -3,    9,
              16,   33,   48,   20,   18,   30,   26,  -21,
              26,   29,   16,    7,   19,   29,   16,   24,
               7,   17,   18,   23,   14,   31,   18,   49,
              42,   23,   30,   23,   29,   24,   17,    6,
              46,   42,   50,   48,   30,   32,   -8,   -2,

            /* bishops: KQ */
              47,   44,    6,    6,  -20,  -21,  -74,  -49,
               9,   21,   22,    3,   -1,  -31,  -29,  -56,
              25,   20,   14,   11,   11,    2,   -3,  -28,
              27,   26,   17,  -18,    8,   35,   15,   20,
              29,    1,  -22,  -28,   -4,   17,   31,   21,
               5,  -23,  -10,   -8,   18,    9,   31,   35,
             -26,  -20,   -7,   -1,   13,   22,   39,   44,
               3,   -8,   -2,    9,   21,   25,   28,   22,

            /* bishops: QK */
             -33,  -27,   12,    9,   14,   -8,   55,   36,
             -29,  -12,    7,    5,    9,   23,   13,    2,
               0,    9,   20,   11,   34,   -3,   -1,   -9,
              14,   18,   23,   -8,   -3,   31,   25,   22,
              36,   20,   20,   10,   -3,   13,    6,    9,
              37,   53,   26,   25,   14,   12,  -19,    4,
              41,   27,   43,   29,   10,    7,  -12,  -11,
              24,   32,   19,   23,    5,   10,  -26,  -13,

            /* bishops: QQ */
              22,   19,  -32,    5,  -11,  -18,  -31,  -22,
             -10,  -12,    6,   -9,   -1,  -24,  -15,  -57,
              14,   -2,    3,  -11,   -1,   -5,  -25,   -7,
              22,   -5,   -8,  -29,  -14,   -6,  -22,  -20,
              13,  -17,  -20,  -37,  -35,   -5,  -14,   -9,
             -17,    0,  -13,  -20,   13,  -26,  -11,  -14,
             -20,  -17,  -21,    1,  -16,   12,   -3,    9,
             -16,   -4,  -18,   -7,    6,   12,   24,   18,

            /* rooks: KK */
              24,   11,    9,  -17,  -29,  -11,   -9,  -30,
              17,   21,    6,  -19,  -23,  -41,  -25,  -15,
              24,   18,    4,  -23,  -34,  -19,  -12,  -30,
              39,   33,   25,   -2,   -2,   19,   11,   13,
              61,   51,   29,    6,   21,   16,    7,   16,
              59,   45,   23,   13,    0,   -1,   10,   18,
              59,   60,   33,    0,   22,   13,   44,    8,
              39,   51,   26,   11,   40,   45,   40,   27,

            /* rooks: KQ */
             -21,  -40,  -38,  -37,  -36,  -23,  -13,   -2,
              -8,  -30,  -28,  -35,  -27,   -7,   11,    5,
              -9,  -29,  -29,  -58,  -16,   -8,  -12,  -14,
               7,  -16,  -10,  -19,  -22,   13,    3,   11,
              -5,    1,  -13,  -17,  -12,    1,   13,   14,
               0,  -14,  -23,  -18,  -32,    9,   22,    9,
              18,   16,  -17,  -16,  -12,    6,   14,    6,
              25,   16,   -3,   -8,    2,    2,   35,   13,

            /* rooks: QK */
              11,  -10,   -1,  -22,  -37,  -35,  -45,  -17,
               0,    3,   10,  -26,  -18,  -40,  -36,  -44,
               0,    3,  -17,  -42,  -16,  -24,  -44,  -49,
              -6,   -6,    0,  -16,   -4,  -21,   -8,  -27,
              13,   31,    8,   -3,   -3,  -10,  -13,  -34,
              21,   13,    6,    8,  -21,  -21,  -16,   -8,
              11,   21,    5,    4,    6,  -23,   10,   -2,
             -51,   14,   17,   13,   16,   -6,   -1,   -2,

            /* rooks: QQ */
             -19,  -30,  -35,  -63,  -50,  -16,   -4,    8,
             -30,   -8,  -13,  -70,  -47,  -42,  -37,  -23,
              -9,  -28,  -33,  -47,  -25,  -49,  -14,  -25,
             -20,  -15,   -6,  -32,  -46,  -23,    6,   -6,
              -8,  -12,   -3,  -30,  -44,   -9,    8,   24,
             -18,  -17,  -20,  -15,  -17,   -8,  -12,   15,
             -17,   -3,   -1,  -18,  -50,  -28,   -5,    9,
              30,   28,   -1,    0,  -36,   -9,   26,   20,

            /* queens: KK */
             -13,  -38,  -63,  -55, -110, -121,  -64,  -31,
               1,  -26,  -40,  -38,  -45,  -92,  -85,  -41,
              -1,  -12,  -13,   -5,  -11,    4,   20,   -1,
               5,   29,   30,   29,    5,   53,   24,   73,
              38,   42,   50,   55,   58,   99,  114,  126,
              30,   67,   76,   75,   89,   94,  104,  156,
              74,  119,   70,   57,  120,   80,  189,   74,
              45,   55,   54,   41,   61,   65,   97,   92,

            /* queens: KQ */
             -16,  -34,  -76,  -57,  -58,  -51,  -30,  -12,
             -13,   -2,  -69,  -22,  -41,  -35,  -30,  -64,
              25,    9,  -50,    1,    8,    5,   38,    9,
              43,    2,   16,  -12,  -10,   23,   52,   33,
              34,   29,   35,  -13,   -4,   27,   32,   60,
              10,   -1,   22,    9,   28,   69,   78,   47,
              29,   43,   36,   43,   11,   56,   71,   52,
              19,   51,   44,   17,   46,   56,   21,   54,

            /* queens: QK */
             -39,  -40,  -75,  -55,  -93,  -68,  -81,  -41,
              10,  -81,  -74,  -31,  -40,  -52,  -17,  -33,
             -36,   -4,  -12,   -4,  -27,  -24,  -12,   -8,
             -33,   16,   50,   16,   22,   -9,  -33,   10,
              34,   37,   49,   26,   33,   10,   32,   -5,
             -15,   48,   69,   48,   -3,   20,   58,    0,
              17,   57,   52,   44,   13,   34,   34,   50,
              32,   35,   39,   13,   -9,   47,   72,   55,

            /* queens: QQ */
             -62,  -53,  -57,  -45,  -76,  -62,  -56,    4,
             -62,  -36,   -6,  -41,   -3,  -27,  -35,  -69,
             -11,   -6,  -13,  -15,   -5,   -7,   -9,  -12,
              -6,  -22,    9,   43,  -32,  -34,  -26,   32,
              29,   54,   58,   21,  -13,   -6,   10,   18,
              75,  104,   37,   30,   27,   16,   46,   23,
              28,   41,   40,   52,   39,    4,   51,   48,
              14,   47,   33,   24,    6,  -20,   21,   -1,

            /* kings: KK */
               0,    0,    0,    0,  -11,   14,  -18,  -43,
               0,    0,    0,    0,   17,   15,    6,   -8,
               0,    0,    0,    0,   24,   24,   13,    5,
               0,    0,    0,    0,   22,   24,   23,    9,
               0,    0,    0,    0,    9,    0,   22,   10,
               0,    0,    0,    0,    4,    0,   10,   17,
               0,    0,    0,    0,    5,    2,   51,   16,
               0,    0,    0,    0,   17,   28,   38,  -81,

            /* kings: KQ */
               0,    0,    0,    0,    8,   -2,  -46,  -45,
               0,    0,    0,    0,    3,   -6,    1,  -23,
               0,    0,    0,    0,   16,    5,    7,   -8,
               0,    0,    0,    0,   18,   28,   28,    1,
               0,    0,    0,    0,   22,   39,   48,   30,
               0,    0,    0,    0,   41,   49,   49,   40,
               0,    0,    0,    0,   -8,    8,   15,   12,
               0,    0,    0,    0,  -10,   -9,  -17,  -48,

            /* kings: QK */
             -43,  -70,  -38,  -10,    0,    0,    0,    0,
             -42,  -13,  -20,  -10,    0,    0,    0,    0,
             -20,    0,   -8,    6,    0,    0,    0,    0,
              -8,   13,    5,   11,    0,    0,    0,    0,
               9,   31,   28,   -3,    0,    0,    0,    0,
              18,   28,   29,   18,    0,    0,    0,    0,
             -20,   -1,   14,    1,    0,    0,    0,    0,
              -7,    1,   -9,    2,    0,    0,    0,    0,

            /* kings: QQ */
              44,  -16,  -22,  -26,    0,    0,    0,    0,
               5,  -17,  -23,  -16,    0,    0,    0,    0,
             -14,  -10,  -10,   -8,    0,    0,    0,    0,
             -28,   -4,   13,    8,    0,    0,    0,    0,
               8,    9,    5,    2,    0,    0,    0,    0,
               9,   33,   15,   20,    0,    0,    0,    0,
              -1,   45,   19,   18,    0,    0,    0,    0,
             -98,   49,   20,    2,    0,    0,    0,    0,

            #endregion

            /* end game mobility weights */

            8, // knights
            5, // bishops
            4, // rooks
            5, // queens

            /* end game squares attacked near enemy king */
            -6, // attacks to squares 1 from king
            -4, // attacks to squares 2 from king
            0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            12, // # friendly pawns 1 from king
            17, // # friendly pawns 2 from king
            12, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -11,

            /* end game backward pawns */
            -7,

            /* end game doubled pawns */
            -37,

            /* end game adjacent/connected pawns */
            6,

            /* UNUSED (was end game king adjacent open file) */
            0,

            /* end game knight on outpost */
            27,

            /* end game bishop on outpost */
            21,

            /* end game bishop pair */
            87,

            /* end game rook on open file */
            2,

            /* end game rook on half-open file */
            26,

            /* end game rook behind passed pawn */
            24,

            /* end game doubled rooks on file */
            12,

            /* end game king on open file */
            5,

            /* end game king on half-open file */
            46,

            /* end game castling rights available */
            -18,

            /* end game castling complete */
            -19,

            /* end game center control */
            7, // D0
            5, // D1

            /* end game queen on open file */
            28,

            /* end game queen on half-open file */
            16,

            /* end game rook on seventh rank */
            30,

            /* end game passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              29,   33,   20,   72,    6,    6,   34,    5,
              32,   32,   10,    5,   21,   21,   52,   11,
              77,   60,   41,   29,   30,   42,   76,   63,
             107,   95,   67,   50,   57,   45,   71,   84,
             148,  160,  125,   77,   92,   75,  100,  110,
             138,  148,  158,  136,  129,  124,  157,  130,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               1,    3,    6,  -23,   -1,    1,   -5,   -4,
              -5,   -9,   -6,  -25,  -13,   -9,   -5,   -3,
             -15,  -24,  -31,  -47,  -28,  -28,  -25,   -7,
             -35,  -40,  -32,  -62,  -47,  -36,  -37,  -33,
             -41,  -80,  -84,  -86, -102,  -77, -122,  -35,
             -62, -116, -113, -134, -168, -129, -164, -138,
               0,    0,    0,    0,    0,    0,    0,    0,
        };
    }
}