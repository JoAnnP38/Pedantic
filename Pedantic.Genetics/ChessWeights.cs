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
        public static readonly Guid DEFAULT_IMMORTAL_ID = new Guid("da5e310e-b0dc-4c77-902c-5a46cc81bb73");
        public const int MAX_WEIGHTS = 3292;
        public const int ENDGAME_WEIGHTS = 1646;
        public const int PIECE_WEIGHT_LENGTH = 6;
        public const int PIECE_SQUARE_LENGTH = 1536;
        public const int GAME_PHASE_MATERIAL = 0;
        public const int PIECE_VALUES = 1;
        public const int PIECE_SQUARE_TABLE = 7;
        public const int PIECE_MOBILITY = 1543;
        public const int KING_ATTACK = 1547;
        public const int PAWN_SHIELD = 1550;
        public const int ISOLATED_PAWN = 1553;
        public const int BACKWARD_PAWN = 1554;
        public const int DOUBLED_PAWN = 1555;
        public const int CONNECTED_PAWN = 1556;
        //public const int KING_ADJACENT_OPEN_FILE = 1557; (-Elo)
        public const int UNUSED = 1557;
        public const int KNIGHT_OUTPOST = 1558;
        public const int BISHOP_OUTPOST = 1559;
        public const int BISHOP_PAIR = 1560;
        public const int ROOK_ON_OPEN_FILE = 1561;
        public const int ROOK_ON_HALF_OPEN_FILE = 1562;
        public const int ROOK_BEHIND_PASSED_PAWN = 1563;
        public const int DOUBLED_ROOKS_ON_FILE = 1564;
        public const int KING_ON_OPEN_FILE = 1565;
        public const int KING_ON_HALF_OPEN_FILE = 1566;
        public const int CASTLING_AVAILABLE = 1567;
        public const int CASTLING_COMPLETE = 1568;
        public const int CENTER_CONTROL = 1569;
        public const int QUEEN_ON_OPEN_FILE = 1571;
        public const int QUEEN_ON_HALF_OPEN_FILE = 1572;
        public const int ROOK_ON_7TH_RANK = 1573;
        public const int PASSED_PAWN = 1574;
        public const int BAD_BISHOP_PAWN = 1582;

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

        // Solution sample size: 12000000, generated on Thu, 06 Jul 2023 15:24:07 GMT
        // Object ID: 8ba2b06f-1054-49f7-b5d5-86baa72f94e0 - Optimized
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening phase material boundary */
            6600,

            /* opening piece values */
            110, 400, 455, 610, 1415, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -18,  -26,  -22,   -5,  -27,   31,   18,  -17,
             -17,  -11,  -16,  -10,   -8,    9,   21,   -2,
              -9,   -5,    2,   10,   15,   29,   14,   -8,
               1,    8,   24,   33,   47,   61,   39,   18,
              20,   46,   71,   58,   80,  152,  141,   52,
             267,  227,  233,  234,  179,  138,  -15,   22,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -47,  -57,  -32,  -45,  -39,   19,   19,  -23,
             -45,  -46,  -22,  -18,  -17,    2,   16,    5,
             -24,    0,   -4,    9,   -2,   11,    5,    2,
             -16,   25,   20,   25,   14,   13,   24,    5,
              50,   78,  114,   48,   68,   62,   78,   37,
             122,   54,  130,  169,  201,  204,  136,  149,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -14,   23,   -7,  -29,  -50,  -26,  -44,  -51,
               8,   31,  -18,  -18,  -28,  -11,  -17,  -40,
              12,   26,   -9,    2,    2,   16,    2,  -20,
              23,   17,   11,   17,   25,   56,   37,    7,
              69,   88,   70,   56,  127,  122,  108,    0,
             218,  259,  169,  220,  185,  163,   -4,  -12,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -42,   -7,    0,  -16,  -33,  -19,  -24,  -22,
             -21,  -12,  -12,  -32,   -6,  -22,  -11,  -14,
             -20,   -7,    0,    7,    0,    1,    0,  -17,
              34,   39,   51,   28,   28,   42,   13,   18,
             101,  101,  104,   86,   71,   39,   52,   44,
             185,  123,  188,  147,  170,  134,  170,  145,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -96,  -14,  -28,  -12,    5,    8,   -9,  -39,
             -37,  -26,   -4,   16,   11,   22,    6,    0,
             -28,   -5,   -7,   33,   37,    7,   12,   -3,
              -3,   16,   16,   23,   31,   34,   40,   15,
              17,    3,   32,   50,    8,   38,    5,   54,
              16,   36,   41,   51,   90,  148,   37,   10,
               0,    4,   23,   68,   56,  101,   39,   54,
            -177,  -31,  -11,   24,   83, -115,   57, -162,

            /* knights: KQ */
            -141,  -41,  -12,  -28,  -16,   11,  -20,  -85,
             -38,  -23,   -7,   27,   10,   25,  -31,  -43,
             -36,    3,    0,   42,   28,    6,    1,  -16,
               3,   47,   36,   48,   34,   39,   15,   20,
              44,    3,   50,   74,   51,   58,   14,   63,
              21,   82,  109,  117,   80,   81,   32,   38,
              18,   10,   13,   83,   42,   63,   40,    6,
            -274, -106,  -40,   11,   71,  -81,  -82, -148,

            /* knights: QK */
            -105,  -44,  -32,  -35,   -2,  -23,  -41, -115,
             -44,   -1,   -1,   10,    2,  -10,  -11,  -12,
             -15,  -14,   17,   37,   47,  -20,   -5,  -16,
              23,   38,   27,   30,   42,   35,   35,    4,
              26,   32,   84,   96,   47,   64,   31,   41,
             -21,   18,   49,   86,  114,  155,   32,   33,
               0,   40,   62,   74,   38,   80,   72,   43,
            -161, -106,  -17,   40,   32,  -73,   31,  -64,

            /* knights: QQ */
             -38,  -23,    9,  -43,   -6,  -26,  -40, -175,
             -40,   33,   -8,    7,   13,    5,  -23,  -60,
              -6,    8,   14,   30,   27,  -19,   15,  -40,
              11,   -5,   54,   47,   11,   29,   17,   -1,
              35,   15,   37,   73,   61,   50,   29,   17,
              18,   13,   53,  126,   98,   79,   76,   60,
             -14,   31,   29,    3,   51,   69,    8,  -29,
            -257,  -88,   15,  -79,   72,  -85,  -14, -104,

            /* bishops: KK */
             -17,   29,   -8,  -13,   -7,    7,   14,   -8,
              28,    0,   27,   -1,   12,   20,   34,    6,
              -9,   24,   17,   18,   10,   20,    9,    5,
               0,   -8,   10,   28,   29,  -11,   -2,   23,
             -14,   -2,    1,   44,   14,   21,  -11,  -11,
              16,   15,   -1,    7,   16,   54,   50,   -2,
             -22,  -17,   -8,   12,  -15,  -13,  -78,  -53,
             -51,  -73,  -39,  -37,  -34,  -53,   23,  -26,

            /* bishops: KQ */
             -68,    0,  -12,  -14,    2,    6,   39,   -6,
             -20,   -7,   12,   -4,   26,   39,   63,   33,
               0,   13,   18,   31,   18,   43,   26,   45,
              11,   10,   15,   44,   87,   22,   24,   17,
             -25,   15,   46,   72,   56,   24,   25,  -14,
               9,   64,   87,   35,   57,   20,   24,    8,
             -14,   23,  -23,    0,   17,    5,  -26,  -37,
             -78,  -50,  -18,   -4,  -64,   -6,  -18,  -17,

            /* bishops: QK */
              53,   23,    7,   15,  -18,  -14,  -25, -116,
              27,   55,   38,   25,    1,   -2,  -33,   -9,
              53,   51,   40,   36,   19,   -2,    5,  -21,
               8,   48,   28,   65,   45,    0,  -12,   20,
               5,   25,   59,  100,   30,   43,    6,  -19,
              -7,   30,   44,   28,   57,   76,   39,   27,
               0,   27,   -9,  -12,   25,   11,  -52,  -43,
             -89,  -64,  -59,  -16,    1,  -40,   37,  -48,

            /* bishops: QQ */
             -88,  -48,   20,  -52,  -11,    0,   34,   14,
              45,   21,    6,   20,   -7,   16,    6,   19,
              12,   25,   22,   20,   31,   34,   31,    9,
              28,   24,   21,   59,   77,   28,   12,   23,
               1,   28,   23,   88,   52,   14,   16,  -18,
             -47,   80,   61,   50,   49,   44,   16,   -3,
             -50,   -4,   -5,    8,   18,  -18,   -7,  -26,
             -31,  -39,  -47,  -36,   -1,  -26,   11,  -53,

            /* rooks: KK */
             -18,   -8,   -7,    2,    7,    5,    6,  -21,
             -27,  -26,  -14,  -11,  -17,   -4,   17,  -25,
             -38,  -39,  -34,  -24,  -12,  -11,   16,   -9,
             -37,  -27,  -25,  -17,  -24,  -39,    9,  -33,
               2,    3,   -5,   27,   -3,    7,   50,   33,
              18,   48,   44,   40,   60,   96,   86,   48,
              12,   13,   11,   52,   20,   96,   89,  123,
              60,   81,   57,   51,   40,   67,  121,  120,

            /* rooks: KQ */
             -21,   17,   -4,  -17,  -11,  -11,  -14,  -40,
             -62,  -11,  -34,  -27,  -23,  -26,   -3,  -36,
             -32,  -13,  -65,  -40,  -47,  -38,   -3,   -6,
             -49,  -16,  -33,  -26,  -28,  -50,    3,  -13,
               0,   44,  -19,    9,    4,   46,   42,   43,
              43,   91,   47,   89,   89,   85,   93,  102,
              35,   39,   56,   81,   66,   77,   98,  111,
              73,   85,   79,   77,   63,   67,   94,  108,

            /* rooks: QK */
             -30,   13,  -19,  -15,   -5,  -12,   10,  -19,
             -20,  -37,  -19,  -17,  -48,  -40,  -18,  -18,
             -19,  -27,  -34,  -40,  -48,  -48,  -11,  -19,
             -18,   15,  -38,  -47,  -52,  -38,  -31,  -20,
              48,   32,   35,    3,    8,    2,   15,    2,
              89,  100,   86,    0,   85,  110,   97,   53,
              88,  101,   87,   44,   30,   99,   56,   76,
             180,  125,   82,   53,   56,   85,  107,  113,

            /* rooks: QQ */
             -26,  -10,    3,    8,   -3,    2,    1,  -24,
             -42,  -53,  -33,    7,    4,  -21,  -10,  -16,
             -45,  -41,   -6,  -51,  -65,  -39,  -38,  -17,
              -8,   -3,  -31,  -13,  -28,    1,  -42,  -47,
              24,   78,  -32,   52,   51,   10,   26,  -15,
              52,  120,   97,   79,  101,   81,   84,   66,
              58,   49,   66,  101,  142,  104,   58,   43,
              52,   76,   56,   52,   49,   91,  118,   43,

            /* queens: KK */
              -4,    3,    9,   15,   24,   -3,  -53,  -42,
              -5,    6,    8,   15,   15,   36,   15,  -20,
             -11,   -2,    6,   -9,    3,    0,   12,    4,
              -5,  -16,  -24,  -20,  -21,  -14,    2,    2,
              -9,   -1,  -24,  -22,  -38,  -20,   -8,  -11,
              -1,  -10,  -22,  -38,   -1,   58,    8,   17,
             -27,  -45,  -27,  -14,  -61,   10,   11,  101,
             -40,   18,    4,   -3,   10,   75,  124,   94,

            /* queens: KQ */
               4,    7,    4,   11,   22,  -16,  -38,  -20,
              13,   29,   24,   13,    8,   11,  -18,  -46,
               4,    3,   21,   10,    1,   16,   -8,   -4,
               0,    0,  -11,   -4,    0,   -2,  -25,  -29,
              -3,   -5,  -38,   15,   -4,   -7,   -3,    5,
              18,   26,   -7,   15,    7,   41,   16,    7,
              35,   18,   10,   17,  -11,    9,   13,   25,
              42,   69,   33,   31,    6,   33,   48,   33,

            /* queens: QK */
             -48,  -70,   -1,  -19,    0,  -20,   -8,   -8,
             -65,  -42,    8,   15,   -6,    6,   31,   21,
             -52,  -17,   -7,  -16,  -10,  -22,   -9,   16,
             -27,  -35,  -49,  -22,  -37,  -12,    6,   -1,
               1,  -32,  -35,  -25,  -39,    2,  -21,  -21,
              -4,  -24,  -21,  -21,    4,   48,   25,   32,
              -4,  -28,  -14,  -39,  -58,  -27,    4,   23,
             -44,   33,   13,    6,  -18,   84,  104,   95,

            /* queens: QQ */
             -21,  -90,    3,  -31,   35,    4,   36,  -16,
             -69,  -24,    8,   21,  -17,   -8,   10,   -3,
             -10,    8,   -7,  -24,   -3,   -8,   -8,   -2,
             -28,   14,  -39,  -49,   -6,  -14,  -10,  -12,
              27,  -48,  -10,  -37,    2,   -4,   21,    6,
              26,   13,   57,   34,   -4,    7,   64,    3,
              38,   16,   16,   39,   19,    6,    4,   12,
              34,   72,   56,   24,   42,   57,   53,   65,

            /* kings: KK */
               2,   58,    7,  -67,  -56,  -61,   -5,  -26,
              32,   23,   12,  -32,  -12,   -4,   11,  -11,
               2,   11,    6,  -33,   36,   -1,    0,  -41,
             -36,   32,  -11,  -20,  101,   71,   11,  -81,
              15,    7,   21,   11,  145,  164,  102,    2,
               4,   59,   33,   -1,  140,  132,  149,   73,
              56,   30,    5,   17,  120,  101,   71,   33,
             -51,   51,   10,  -28,  -39,   24,   63,  117,

            /* kings: KQ */
               2,   58,    7,  -67,  -73,  -73,   -5,  -13,
              32,   23,   12,  -32,  -17,   -5,    5,    2,
               2,   11,    6,  -33,    1,   17,   17,  -44,
             -36,   32,  -11,  -20,   50,   55,  -19,  -32,
              15,    7,   21,   11,  116,   86,   74,   15,
               4,   59,   33,   -1,   92,   82,   68,   23,
              56,   30,    5,   17,   84,  107,   57,   21,
             -51,   51,   10,  -28,  -29,    6,   84,   34,

            /* kings: QK */
             -20,   20,  -20,  -77,  -13,  -48,   15,   -8,
              28,    9,   -1,  -26,  -20,   -9,   22,    0,
             -10,   24,    9,  -20,  -21,  -28,  -14,  -32,
              -3,   42,   50,   54,  -18,  -17,  -31,  -78,
              12,   73,   70,  126,   -4,   12,   11,  -38,
              -4,   88,  107,  107,    1,   18,   47,   -9,
             111,  120,   98,   91,   10,   13,  -15,  -16,
             -51,   58,   74,    1,  -65,  -26,   27,   38,

            /* kings: QQ */
             -68,    6,  -29,  -68,  -13,  -48,   15,   -8,
             -14,    3,   20,  -21,  -20,   -9,   22,    0,
             -52,   17,   20,   12,  -21,  -28,  -14,  -32,
               5,   63,   42,   51,  -18,  -17,  -31,  -78,
              74,   83,  104,  155,   -4,   12,   11,  -38,
              95,  180,  124,  103,    1,   18,   47,   -9,
              78,   39,   74,   99,   10,   13,  -15,  -16,
             -43,   72,   26,   -9,  -65,  -26,   27,   38,

            #endregion

            /* opening mobility weights */

            10, // knights
            6, // bishops
            3, // rooks
            1, // queens

            /* opening squares attacked near enemy king */
            16, // attacks to squares 1 from king
            16, // attacks to squares 2 from king
            5, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
            22, // # friendly pawns 1 from king
            17, // # friendly pawns 2 from king
            9, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -2,

            /* opening backward pawns */
            -22,

            /* opening doubled pawns */
            -22,

            /* opening adjacent/connected pawns */
            11,

            /* UNUSED (was opening king adjacent open file) */
            0,

            /* opening knight on outpost */
            -4,

            /* opening bishop on outpost */
            19,

            /* opening bishop pair */
            41,

            /* opening rook on open file */
            31,

            /* opening rook on half-open file */
            13,

            /* opening rook behind passed pawn */
            -2,

            /* opening doubled rooks on file */
            12,

            /* opening king on open file */
            -48,

            /* opening king on half-open file */
            -18,

            /* opening castling rights available */
            23,

            /* opening castling complete */
            14,

            /* opening center control */
            3, // D0
            3, // D1

            /* opening queen on open file */
            -4,

            /* opening queen on half-open file */
            9,

            /* opening rook on seventh rank */
            8,

            /* opening passed pawn */
            0, -6, -18, 2, 38, 85, 55, 0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -4,   -5,   -9,    0,   -7,  -18,  -20,    5,
              -8,   -6,   -9,  -10,  -13,  -11,  -15,  -12,
              -8,   -3,   -7,  -12,  -16,   -8,   -8,   -2,
               3,    1,   -7,  -10,  -12,  -10,   -3,   -5,
              -3,  -18,  -20,  -25,  -22,  -22,  -21,   -5,
              26,   -5,   -9,  -25,  -26,  -13,  -12,   21,
               0,    0,    0,    0,    0,    0,    0,    0,


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game phase material boundary */
            650,

            /* end game piece values */
            100, 300, 325, 560, 965, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              29,   27,   15,  -12,   33,   15,   -1,  -10,
              27,   16,    6,   -3,    3,    4,   -6,   -6,
              32,   31,   -1,  -22,  -14,   -3,   12,   -3,
              61,   36,   16,  -17,   -8,   -3,   15,    6,
              92,   79,   53,   21,   10,    6,   14,    8,
             138,  144,  127,   82,   81,   49,   85,   51,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              45,   48,   37,   38,    5,   -3,   -8,    9,
              36,   34,   14,    9,  -11,   -8,   -6,   -2,
              30,   20,    5,  -14,   -8,    8,   18,   17,
              38,    9,   -1,   -8,   13,   45,   49,   59,
              34,    9,  -22,    8,   70,  108,  130,  115,
              64,   56,   33,   45,  176,  198,  228,  191,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              22,   -2,    7,   -6,   33,   26,   36,   45,
              16,   -1,    2,  -15,    6,   14,   17,   29,
              35,   31,   19,   -8,    2,    6,   12,   19,
              79,   74,   48,   17,    7,   -9,   12,   15,
             114,  138,  128,   61,    2,    4,    7,   23,
             177,  204,  227,  160,   73,   14,   36,   40,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
               7,    1,    5,  -26,    3,    6,    3,   21,
               7,    5,   -2,   -2,  -12,   -7,   -9,    8,
              11,   16,   -2,  -10,   -5,  -10,    4,   15,
              14,    6,   -6,   -5,   -2,    8,   14,   26,
              10,   35,   -2,   -9,   22,   50,   61,   60,
              47,   89,   39,   47,   95,  143,  149,  147,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -51,  -76,  -45,  -30,  -39,  -46,  -50,  -71,
             -56,  -18,  -29,  -30,  -24,  -13,  -24,  -23,
             -43,  -13,  -10,    8,   11,  -16,   -6,   -2,
               1,   13,   28,   39,   37,   32,   18,    3,
               8,    5,   18,   34,   42,   34,   38,   16,
              15,   11,   17,   20,    9,    1,   29,   16,
               4,   21,   11,   21,   30,  -13,    1,    0,
              -3,   27,   27,   12,    9,   21,   39,  -67,

            /* knights: KQ */
             -54,  -27,  -43,  -12,  -28,  -42,  -15,  -97,
             -29,  -17,   -8,  -29,  -25,  -19,    0,  -28,
             -24,  -23,   -9,   -8,   -4,  -29,  -17,   -2,
              -4,   -7,    6,    5,   15,    9,   11,   -5,
             -25,  -11,    4,   -1,    1,    3,    1,  -23,
             -18,  -35,  -33,  -25,   -7,   -3,   16,    2,
             -31,  -33,  -29,   -1,   -9,   -1,   15,    0,
             -53,  -21,  -19,   -3,  -22,   43,   24,  -35,

            /* knights: QK */
             -93,  -27,  -33,    1,  -35,  -51,  -47,  -51,
             -45,   11,  -13,  -14,   -9,   -8,  -37,  -54,
               3,   -5,  -15,   15,    0,   -6,  -17,  -47,
              -6,    3,   19,   25,   10,   12,   -1,  -17,
              32,   12,    6,    9,   22,    6,   -6,  -17,
              38,   31,   27,   -6,   -1,   -8,   -5,   -3,
               0,   28,    9,   30,   14,  -21,  -33,  -37,
              -8,   45,   16,   14,   -7,   -8,   -2,  -90,

            /* knights: QQ */
             -51,  -32,  -26,  -18,  -32,  -18,  -51,  -82,
             -20,  -10,  -39,  -27,  -37,  -55,  -53,  -53,
             -26,   -9,  -28,  -16,  -13,  -19,  -27,  -49,
              14,    0,   12,   -7,    0,   -9,   -6,  -21,
              -1,   -1,   -6,   -2,   -8,  -10,  -31,    0,
               5,   16,  -14,  -22,  -19,  -12,  -19,   -2,
               7,   -4,  -21,   11,    8,   -3,    6,    2,
             -29,   58,  -21,    7,  -14,    2,    1,  -94,

            /* bishops: KK */
             -19,  -24,  -30,    1,   -7,   -4,  -21,  -48,
              -9,  -25,  -12,    5,   -7,  -13,  -33,  -28,
              -3,   -2,   14,   11,   23,   -2,   -7,   -2,
              -5,   12,   19,   19,    3,   16,   12,  -16,
              18,   16,    6,    4,   16,   13,   18,   23,
              -2,    8,    5,    3,   16,   16,   10,   40,
              21,   13,   15,   17,   21,   12,   19,   -3,
              47,   33,   42,   30,   26,   12,  -10,    2,

            /* bishops: KQ */
              44,   21,    2,   -4,  -18,  -15,  -56,  -57,
              -9,   -1,   -1,    0,   -6,  -36,  -27,  -29,
               0,   -2,   -3,   -4,    6,  -10,  -15,  -31,
              -2,   13,    6,  -22,  -13,   16,    2,   -4,
              16,    1,  -32,  -35,   -6,   12,   15,    8,
              -8,  -29,  -19,   -5,    0,    0,   14,   15,
             -40,   -8,   -6,   -1,   -9,    8,   21,   26,
               2,    1,    2,    3,   10,   16,    9,   25,

            /* bishops: QK */
             -41,  -31,   -9,  -12,   -2,   -2,   18,   19,
             -32,  -22,   -3,   -9,    0,   -3,    8,   -9,
              -9,    3,    4,    8,   10,    3,   -9,   -1,
              -9,    6,   14,   -9,   -2,    9,   11,  -19,
              25,   18,   -4,   -5,   -6,  -11,    1,   15,
              18,   32,   14,    7,   17,   -7,  -19,    2,
              27,   22,   13,   18,   -4,   12,    4,  -10,
              27,   24,   25,   15,    7,    3,    0,  -15,

            /* bishops: QQ */
              11,   -7,  -45,  -14,  -37,  -36,  -47,  -47,
             -18,  -22,  -22,  -31,  -26,  -47,  -38,  -58,
             -15,  -15,  -31,  -34,  -31,  -33,  -41,  -21,
              -2,  -18,  -29,  -49,  -31,  -32,  -41,  -30,
             -18,   -8,  -40,  -56,  -43,  -25,  -34,  -14,
             -15,  -26,  -27,  -33,  -14,  -39,  -25,  -30,
             -24,  -13,  -36,  -13,  -17,   -6,  -19,  -27,
             -24,  -14,   -3,  -35,   -6,  -12,  -10,   -7,

            /* rooks: KK */
              26,   13,   14,   -5,  -22,   -4,    5,    4,
              13,   17,    8,   -9,  -13,  -12,  -16,    0,
              34,   31,   22,    1,  -10,    0,   -5,   -1,
              42,   38,   29,   22,   13,   30,   21,   30,
              47,   48,   39,   20,   25,   26,   22,   20,
              56,   44,   43,   27,   17,   27,   25,   30,
              45,   52,   44,   10,   31,   26,   37,   19,
              40,   48,   41,   20,   42,   46,   34,   26,

            /* rooks: KQ */
             -13,  -29,  -28,  -22,  -20,  -15,    3,   13,
              -5,  -25,  -22,  -19,  -18,   -9,  -20,    6,
             -14,  -15,    2,  -20,  -12,   -4,   -3,  -10,
              11,   -7,    7,   -4,   -2,   20,    4,    1,
               3,   -9,   13,    5,    1,    5,   11,    4,
               3,   -8,   -6,  -18,  -18,    6,    8,    4,
               4,    5,   -6,  -13,  -12,  -11,    7,   -7,
              22,    0,   -2,   -7,   -1,   14,   16,   -1,

            /* rooks: QK */
               8,   -7,   -2,  -22,  -31,  -25,  -30,  -11,
             -11,    3,   -1,  -14,  -19,  -25,  -31,  -41,
               2,   11,    5,  -11,  -11,   -5,  -28,  -33,
              -2,    3,   15,    9,    5,    0,   -2,   -7,
              12,   31,   14,   21,    5,    8,   -2,   -8,
               9,    9,   12,   14,  -11,  -13,   -4,  -12,
               4,    8,    3,    4,   19,  -14,    4,   -1,
             -45,   10,   16,    5,    6,   -6,   -4,   -5,

            /* rooks: QQ */
             -25,  -33,  -50,  -55,  -48,  -34,  -15,    0,
             -44,  -24,  -23,  -50,  -62,  -46,  -54,  -36,
              -4,  -16,  -25,  -30,  -30,  -36,  -15,  -14,
             -16,  -14,  -10,  -25,  -27,  -21,  -15,   -4,
             -15,  -32,   -6,  -26,  -37,  -15,  -18,   -4,
             -10,  -28,  -14,  -16,  -34,   -6,  -14,   -4,
             -14,  -14,    6,  -13,  -51,  -37,  -20,  -10,
              -4,   -1,   -4,   -9,  -27,  -17,   -7,    3,

            /* queens: KK */
              -8,  -34,  -46,  -47,  -83,  -92,  -60,  -14,
               0,  -14,  -13,  -39,  -37,  -74,  -92,  -34,
              15,   -3,    9,   12,    0,    2,    7,  -11,
              18,   36,   31,   39,   27,   37,   30,   44,
              43,   40,   50,   56,   68,   76,   94,  104,
              41,   66,   64,   73,   78,   79,  104,  119,
              66,   96,   88,   68,  109,   74,  139,   84,
              60,   49,   55,   62,   66,   62,   70,   58,

            /* queens: KQ */
             -15,  -44,  -63,  -40,  -45,  -51,  -76,   -3,
              -6,  -27,  -61,  -31,  -35,  -44,  -28,  -65,
              -2,   -6,  -57,  -17,  -17,   21,   23,  -14,
              19,  -11,    8,   -8,   -2,   32,   23,   29,
              21,   29,   30,   10,   17,   34,   53,   40,
              26,   12,   32,   26,   29,   43,   34,   51,
              21,   41,   43,   46,   23,   51,   47,   23,
              28,   25,   30,   31,   39,   25,    6,    3,

            /* queens: QK */
             -48,  -57,  -92,  -54,  -89,  -58,  -49,  -57,
             -10,  -59,  -78,  -38,  -37,  -86,  -46,  -22,
             -24,   -7,   16,   10,   -9,  -20,  -14,  -27,
              -2,   31,   41,   13,   17,    3,  -24,   15,
              15,   43,   48,   37,   28,   17,   25,   -8,
             -13,   47,   77,   52,   13,   15,   11,    0,
              15,   66,   64,   55,   45,   16,   48,   21,
              27,   25,   42,   23,   26,   37,   34,   14,

            /* queens: QQ */
             -57,  -55,  -36,  -52,  -91,  -68,  -83,  -56,
             -33,  -34,  -29,  -68,  -27,  -48,  -72,  -53,
               2,  -11,  -34,    2,  -31,  -23,  -24,  -30,
             -20,   -3,    8,   15,  -29,  -21,   -4,  -14,
               2,   43,   10,   20,   -5,  -11,   12,    2,
              45,   60,   22,   36,   26,    4,   13,   20,
              34,   53,   45,   57,   30,   31,   16,  -23,
             -35,   29,   37,   -3,   -2,  -51,  -13,  -38,

            /* kings: KK */
              -1,  -14,  -21,  -21,  -19,   -8,  -24,  -42,
              -3,   -2,   -8,  -10,   -6,   -6,  -10,  -15,
              -6,    6,    4,    7,    5,    3,   -4,  -16,
               0,   18,   22,   20,    9,    8,    7,    6,
              19,   34,   34,   20,    6,   -3,    7,   -1,
              29,   55,   45,   39,    6,    6,   31,   13,
              28,   61,   51,   47,   15,    8,   39,   19,
             -27,   37,   51,   46,   24,   29,   43,  -51,

            /* kings: KQ */
              -1,  -14,  -21,  -21,    2,   -5,  -26,  -29,
              -3,   -2,   -8,  -10,    7,   -2,    1,  -16,
              -6,    6,    4,    7,   23,    9,    5,    1,
               0,   18,   22,   20,   29,   27,   36,   18,
              19,   34,   34,   20,   33,   43,   45,   35,
              29,   55,   45,   39,   40,   49,   53,   44,
              28,   61,   51,   47,   23,   22,   36,   29,
             -27,   37,   51,   46,    2,   12,    2,  -39,

            /* kings: QK */
             -27,  -48,  -41,  -20,  -43,  -31,  -30,  -51,
             -38,  -14,  -22,  -16,  -13,  -13,  -15,  -24,
             -20,   -5,   -7,    3,    5,   -2,  -11,  -26,
              -2,   14,   15,   14,   18,   11,    9,   -9,
              18,   33,   27,   18,   19,   28,   27,   13,
              30,   45,   36,   25,   29,   46,   62,   27,
              -3,   26,   35,   30,   48,   60,   74,   36,
              10,   25,    7,   27,   49,   59,   57,  -35,

            /* kings: QQ */
              68,   12,   -8,  -19,  -43,  -31,  -30,  -51,
              11,    1,  -11,  -12,  -13,  -13,  -15,  -24,
               4,    4,   -1,    1,    5,   -2,  -11,  -26,
               0,   10,   15,   21,   18,   11,    9,   -9,
              11,   22,   23,   11,   19,   28,   27,   13,
              24,   33,   13,   22,   29,   46,   62,   27,
               7,   51,   24,   22,   48,   60,   74,   36,
             -47,   46,   19,   17,   49,   59,   57,  -35,

            #endregion

            /* end game mobility weights */

            3, // knights
            3, // bishops
            2, // rooks
            1, // queens

            /* end game squares attacked near enemy king */
            -3, // attacks to squares 1 from king
            -2, // attacks to squares 2 from king
            0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            15, // # friendly pawns 1 from king
            17, // # friendly pawns 2 from king
            11, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -5,

            /* end game backward pawns */
            -11,

            /* end game doubled pawns */
            -21,

            /* end game adjacent/connected pawns */
            8,

            /* UNUSED (was end game king adjacent open file) */
            0,

            /* end game knight on outpost */
            24,

            /* end game bishop on outpost */
            13,

            /* end game bishop pair */
            81,

            /* end game rook on open file */
            2,

            /* end game rook on half-open file */
            17,

            /* end game rook behind passed pawn */
            19,

            /* end game doubled rooks on file */
            9,

            /* end game king on open file */
            0,

            /* end game king on half-open file */
            29,

            /* end game castling rights available */
            -5,

            /* end game castling complete */
            -7,

            /* end game center control */
            5, // D0
            2, // D1

            /* end game queen on open file */
            25,

            /* end game queen on half-open file */
            15,

            /* end game rook on seventh rank */
            19,

            /* end game passed pawn */
            0, 17, 22, 40, 59, 73, 45, 0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               7,    3,   -5,    1,  -11,   -4,   -7,   -2,
              -5,   -7,   -9,  -28,  -11,   -4,   -4,   -6,
              -9,  -24,  -27,  -33,  -22,  -17,  -16,   -3,
             -24,  -26,  -27,  -40,  -36,  -25,  -25,  -28,
             -27,  -41,  -44,  -50,  -48,  -45,  -44,  -28,
             -29,  -43,  -41,  -46,  -48,  -43,  -46,  -45,
               0,    0,    0,    0,    0,    0,    0,    0,
        };
    }
}