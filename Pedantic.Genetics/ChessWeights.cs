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
        public const int MAX_WEIGHTS = 3148;
        public const int ENDGAME_WEIGHTS = 1574;
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
        public const int PASSED_PAWN = 1557;
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

        // Solution sample size: 12000000, generated on Wed, 28 Jun 2023 19:29:17 GMT
        // Object ID: 158a147c-43cb-43b9-90f4-3714a2e80f1e - Optimized
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening phase material boundary */
            7000,

            /* opening piece values */
            95, 370, 395, 555, 1315, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -17,  -27,  -22,    2,  -23,   27,   15,  -10,
             -19,  -12,  -21,  -12,  -14,   10,   19,   -8,
             -13,   -4,    1,    6,    8,   30,   15,   -6,
               4,   18,   23,   36,   48,   63,   42,   16,
              39,   58,  105,  104,  117,  157,  142,   54,
             274,  240,  244,  252,  186,  144,   -5,   12,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -51,  -63,  -30,  -45,  -37,   13,   24,  -16,
             -52,  -45,  -25,  -21,  -17,    7,   21,   -1,
             -28,   -6,   -3,   -1,   -6,    9,    3,    4,
             -20,   23,   21,   31,   20,   15,   28,   14,
              58,   84,  115,   74,   97,   78,   88,   55,
             123,   86,  111,  171,  187,  195,  122,  127,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -10,   19,   -7,  -25,  -57,  -32,  -49,  -52,
               4,   31,  -21,  -15,  -29,  -13,  -21,  -51,
               7,   19,  -14,   -4,   -6,   17,    1,  -18,
              18,   22,    8,   18,   29,   55,   36,    7,
              88,  108,   81,   87,  145,  121,  107,    4,
             216,  253,  172,  211,  184,  156,  -15,   -8,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -40,   -6,    1,  -14,  -37,  -17,  -27,  -15,
             -30,  -14,  -14,  -31,  -12,  -21,  -13,  -23,
             -20,   -7,   -1,   -1,   -5,    1,    0,   -6,
              30,   35,   44,   27,   31,   50,   24,   18,
              98,  100,  104,  117,   87,   47,   70,   51,
             159,  119,  168,  147,  166,  130,  137,  112,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -97,  -12,  -29,   -9,    5,   10,   -4,  -40,
             -35,  -29,   -3,   18,   14,   23,   13,    4,
             -24,   -4,   -6,   32,   39,    5,   15,    0,
              -1,   16,   18,   25,   32,   32,   44,   14,
              18,    1,   31,   49,    7,   41,   -1,   62,
              17,   32,   44,   49,   98,  157,   43,   21,
              -6,   11,   26,   70,   59,  107,   39,   48,
            -168,  -29,   -5,   33,   81, -112,   39, -152,

            /* knights: KQ */
            -133,  -44,  -18,  -25,  -15,   -3,  -17,  -92,
             -34,  -18,  -10,   27,   10,   24,  -35,  -45,
             -38,    7,   -4,   39,   25,   10,    0,  -29,
               1,   49,   33,   50,   35,   37,   16,   14,
              44,   10,   42,   73,   42,   54,   14,   60,
              25,   72,   97,  114,   87,   84,   34,   34,
              12,   18,   26,   90,   49,   71,   37,    5,
            -272, -110,  -36,   -2,   74,  -89,  -73, -143,

            /* knights: QK */
            -110,  -43,  -33,  -39,    0,  -16,  -40, -107,
             -47,   -3,   -4,    6,    4,  -16,   -4,   -6,
             -18,   -9,   17,   33,   52,  -19,   -9,  -28,
              22,   35,   31,   29,   43,   32,   43,    1,
              19,   30,   80,   97,   42,   63,   34,   33,
             -18,   16,   54,   90,  120,  153,   29,   26,
              -4,   33,   58,   78,   32,   73,   78,   29,
            -167, -102,  -19,   34,   45,  -77,   29,  -66,

            /* knights: QQ */
             -38,  -26,    4,  -37,  -12,  -31,  -41, -153,
             -52,   20,  -14,    7,   10,    1,  -28,  -67,
              -6,    9,   15,   28,   26,  -27,   18,  -39,
               9,  -12,   58,   45,   17,   32,   23,   -1,
              30,    9,   42,   76,   58,   45,   24,   22,
              21,   17,   68,  121,  110,   90,   77,   62,
               0,   35,   45,   14,   41,   62,   -2,  -25,
            -251,  -87,    0,  -64,   73,  -81,  -17,  -88,

            /* bishops: KK */
             -20,   26,   -9,  -17,   -9,    1,   15,   -8,
              26,   -2,   24,   -4,   11,   21,   35,   -4,
             -12,   17,   13,   13,    6,   15,    8,    5,
              -3,   -8,    4,   28,   26,  -19,   -3,   24,
             -15,   -8,    4,   42,   11,   20,  -14,  -22,
              16,   22,    1,    3,   13,   53,   51,   -3,
             -18,  -17,  -14,   12,  -12,  -19,  -80,  -47,
             -44,  -63,  -42,  -33,  -45,  -54,   24,  -24,

            /* bishops: KQ */
             -76,   -3,  -15,  -15,    0,    7,   46,   -6,
             -14,  -11,   12,    0,   17,   42,   69,   26,
              -2,   14,   17,   24,   17,   44,   24,   47,
               6,    5,   15,   40,   83,   13,   16,    8,
             -19,   22,   42,   73,   57,   25,   14,  -23,
              21,   71,   83,   22,   50,   15,   21,   -6,
              -4,   26,  -35,    3,   22,    7,  -34,  -53,
             -65,  -48,  -42,  -15,  -70,  -14,  -11,  -10,

            /* bishops: QK */
              32,   27,   16,    9,  -18,  -17,  -20, -101,
              33,   49,   37,   34,   -2,   -7,  -29,  -11,
              55,   49,   39,   36,   17,   -5,    0,  -18,
              15,   52,   28,   65,   48,   -6,  -10,   21,
               3,   20,   64,  104,   33,   45,   10,  -16,
              -7,   27,   38,   36,   46,   70,   30,   32,
              -5,   19,  -16,   -9,   26,   -4,  -44,  -56,
             -76,  -64,  -62,  -14,   -7,  -49,   26,  -47,

            /* bishops: QQ */
             -79,  -53,   20,  -54,  -20,   -6,   33,   14,
              47,   25,   10,   15,   -8,   15,   12,   27,
              12,   30,   19,   21,   28,   37,   34,   -1,
              19,   20,   15,   62,   71,   26,   17,   24,
             -12,   25,   17,   94,   57,   19,   12,  -21,
             -48,   82,   58,   47,   49,   32,   21,   -4,
             -37,    3,    4,    6,   24,  -18,  -11,  -28,
             -33,  -32,  -57,  -42,   -2,  -32,   18,  -33,

            /* rooks: KK */
             -21,  -12,  -11,   -3,    3,    3,    7,  -19,
             -29,  -29,  -18,  -16,  -16,   -7,   20,  -21,
             -46,  -41,  -36,  -26,  -13,  -14,   16,  -14,
             -38,  -31,  -30,  -17,  -28,  -45,    9,  -36,
              -3,    1,   -5,   29,  -10,    8,   51,   35,
              19,   51,   45,   43,   65,   99,   84,   58,
               7,   13,   14,   57,   22,  106,   86,  126,
              59,   77,   63,   55,   41,   60,  124,  117,

            /* rooks: KQ */
             -24,   19,   -7,  -18,   -7,  -11,  -10,  -42,
             -62,  -16,  -38,  -27,  -19,  -31,   -2,  -40,
             -35,  -16,  -62,  -46,  -48,  -45,   -9,  -16,
             -55,  -10,  -35,  -20,  -23,  -54,   -2,  -19,
              10,   46,  -22,    9,    0,   44,   37,   45,
              41,   87,   40,   96,   88,   83,   95,   97,
              34,   38,   56,  100,   73,   87,   94,  110,
              75,   84,   77,   69,   55,   60,   91,   97,

            /* rooks: QK */
             -28,   15,  -27,  -14,   -5,   -7,   14,  -24,
             -25,  -40,  -23,  -16,  -53,  -33,  -17,  -18,
             -29,  -35,  -37,  -47,  -55,  -52,   -7,  -18,
             -17,   17,  -41,  -37,  -54,  -32,  -20,  -30,
              51,   34,   37,   -1,    6,    1,   18,    0,
              84,  100,   78,    5,   76,  113,   96,   58,
              80,   91,   81,   54,   32,   98,   67,   75,
             145,  124,   82,   63,   49,   80,   99,  121,

            /* rooks: QQ */
             -19,   -8,    7,    6,   -1,    0,   -3,  -31,
             -49,  -48,  -34,   -3,   -2,   -9,   -3,  -26,
             -51,  -40,  -18,  -50,  -59,  -34,  -46,  -21,
             -10,  -14,  -36,   -9,  -31,   -8,  -35,  -47,
              18,   76,  -23,   56,   42,    5,   27,  -19,
              46,  103,   94,   75,   92,   81,   89,   63,
              51,   49,   59,  109,  124,  102,   47,   35,
              49,   82,   56,   57,   41,   89,  107,   54,

            /* queens: KK */
              -4,    2,   10,   15,   29,   -3,  -53,  -40,
              -7,    6,   10,   16,   15,   34,   23,  -21,
             -11,   -1,    2,  -13,   -1,   -1,   16,    4,
              -7,  -19,  -19,  -25,  -22,  -20,    2,   -5,
              -3,   -8,  -22,  -27,  -39,  -20,  -15,  -18,
              -2,  -11,  -24,  -39,   -2,   51,    8,    7,
             -30,  -46,  -25,  -24,  -55,   15,    2,   92,
             -37,   25,    3,   -1,    8,   71,  109,   88,

            /* queens: KQ */
              -7,    8,    3,   14,   23,  -12,  -45,  -33,
              16,   29,   27,   10,    8,   14,  -23,  -42,
               2,    1,   21,   10,    5,   14,  -13,   -4,
              -4,   16,  -17,   -3,    3,  -11,  -28,  -28,
              -3,   -8,  -40,   15,   -8,   -3,  -22,    8,
              17,   25,  -14,    5,   10,   35,   10,    1,
              33,    3,   -3,    8,  -18,   -3,    8,   21,
              36,   67,   32,   28,    7,   36,   49,   37,

            /* queens: QK */
             -36,  -56,   -5,  -18,    4,  -18,  -14,   -2,
             -61,  -44,   18,   19,   -2,   11,   45,   19,
             -50,  -12,  -10,  -18,   -6,  -21,   -8,   18,
             -27,  -39,  -47,  -17,  -34,  -15,    9,   -1,
              -3,  -31,  -37,  -10,  -38,   -9,  -19,  -30,
              -3,  -17,  -18,  -19,    2,   55,   33,   19,
              -7,  -33,  -23,  -40,  -45,  -26,    0,   24,
             -44,   38,   11,    8,  -10,   85,   98,   89,

            /* queens: QQ */
             -23,  -78,    6,  -24,   43,   11,   37,   -2,
             -77,  -20,   13,   21,  -16,    1,   20,   -3,
              -1,    7,   -7,  -34,   -3,  -10,   -1,    0,
             -21,    8,  -41,  -43,   -3,  -16,   -4,  -22,
              10,  -42,   -4,  -31,    5,   -2,   11,   -7,
              36,    3,   44,   23,   -6,   16,   51,    6,
              37,    9,   20,   30,   14,    4,    1,    9,
              32,   80,   52,   15,   41,   65,   63,   67,

            /* kings: KK */
               2,   58,    7,  -67,  -53,  -58,    2,  -18,
              32,   23,   12,  -32,  -18,   -5,   15,   -6,
               2,   11,    6,  -33,   19,  -11,   -6,  -43,
             -36,   32,  -11,  -20,   80,   53,  -10,  -81,
              15,    7,   21,   11,  105,  134,   76,  -21,
               4,   59,   33,   -1,  100,   88,  120,   52,
              56,   30,    5,   17,   97,   78,   58,   23,
             -51,   51,   10,  -28,  -56,    8,   51,   99,

            /* kings: KQ */
               2,   58,    7,  -67,  -66,  -72,    1,   -8,
              32,   23,   12,  -32,  -16,   -1,   10,    6,
               2,   11,    6,  -33,   -3,    4,   10,  -38,
             -36,   32,  -11,  -20,   36,   47,  -30,  -38,
              15,    7,   21,   11,   88,   66,   61,   -2,
               4,   59,   33,   -1,   69,   63,   66,   21,
              56,   30,    5,   17,   55,   71,   40,    4,
             -51,   51,   10,  -28,  -52,   -9,   77,   44,

            /* kings: QK */
              -8,   33,   -8,  -77,  -13,  -48,   15,   -8,
              31,   16,    3,  -23,  -20,   -9,   22,    0,
             -11,   25,   10,  -32,  -21,  -28,  -14,  -32,
             -13,   44,   40,   46,  -18,  -17,  -31,  -78,
              11,   57,   61,   96,   -4,   12,   11,  -38,
             -18,   95,   91,   80,    1,   18,   47,   -9,
             111,  105,   81,   69,   10,   13,  -15,  -16,
             -49,   50,   56,  -28,  -65,  -26,   27,   38,

            /* kings: QQ */
             -54,   21,  -16,  -60,  -13,  -48,   15,   -8,
             -10,   12,   28,  -21,  -20,   -9,   22,    0,
             -38,   19,   11,    5,  -21,  -28,  -14,  -32,
              -4,   67,   22,   29,  -18,  -17,  -31,  -78,
              61,   55,   80,  121,   -4,   12,   11,  -38,
              80,  158,   88,   85,    1,   18,   47,   -9,
              71,   30,   64,   70,   10,   13,  -15,  -16,
             -52,   63,   16,  -29,  -65,  -26,   27,   38,

            #endregion

            /* opening mobility weights */

            10, // knights
            6, // bishops
            3, // rooks
            1, // queens

            /* opening squares attacked near enemy king */
            15, // attacks to squares 1 from king
            16, // attacks to squares 2 from king
            5, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
            23, // # friendly pawns 1 from king
            18, // # friendly pawns 2 from king
            11, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -1,

            /* opening backward pawns */
            -23,

            /* opening doubled pawns */
            -20,

            /* opening adjacent/connected pawns */
            10,

            /* opening passed pawns */
            8,

            /* opening knight on outpost */
            -3,

            /* opening bishop on outpost */
            14,

            /* opening bishop pair */
            30,

            /* opening rook on open file */
            30,

            /* opening rook on half-open file */
            13,

            /* opening rook behind passed pawn */
            4,

            /* opening doubled rooks on file */
            14,

            /* opening king on open file */
            -48,

            /* opening king on half-open file */
            -19,

            /* opening castling rights available */
            22,

            /* opening castling complete */
            18,

            /* opening center control */
            4, // D0
            3, // D1

            /* opening queen on open file */
            -5,

            /* opening queen on half-open file */
            8,

            /* opening rook on seventh rank */
            9,

            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game phase material boundary */
            1100,

            /* end game piece values */
            100, 325, 315, 580, 1025, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              27,   23,   11,  -12,   25,   12,   -5,  -13,
              19,   12,   -1,   -8,   -1,   -3,   -9,   -8,
              27,   17,   -8,  -28,  -21,   -8,    2,   -6,
              58,   31,   20,   -8,   -6,   -1,   18,    7,
             108,  101,   77,   47,   40,   30,   45,   35,
             156,  155,  145,   89,   86,   52,   84,   57,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              39,   44,   32,   34,    5,    1,  -10,    0,
              28,   27,    8,    6,  -14,   -9,  -10,   -5,
              25,   14,   -3,  -16,  -11,    7,   13,   13,
              37,    9,   -1,   -6,   16,   47,   47,   55,
              52,   33,    9,   33,   91,  126,  151,  133,
              75,   57,   40,   53,  176,  210,  234,  200,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              19,    4,    1,   -7,   27,   21,   30,   35,
              15,   -2,   -5,  -22,   -1,    9,   11,   23,
              32,   26,   13,  -12,   -2,    0,    7,   14,
              78,   74,   56,   18,    7,   -8,   12,   16,
             138,  156,  151,   91,   29,   23,   30,   38,
             191,  216,  231,  173,   84,   26,   43,   51,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              13,    4,    9,  -22,    3,    4,    4,   19,
               2,   -1,   -3,  -10,  -11,  -10,  -12,    8,
              14,    9,   -8,  -14,  -11,  -11,    2,    8,
              21,   13,    6,    0,   -2,    7,    9,   25,
              37,   53,   26,   22,   48,   59,   78,   75,
              58,   91,   47,   47,   94,  151,  154,  164,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -58,  -78,  -48,  -30,  -44,  -41,  -54,  -68,
             -60,  -23,  -27,  -30,  -24,  -15,  -28,  -21,
             -41,  -18,  -13,   13,   11,  -15,   -5,   -6,
             -10,    9,   27,   37,   37,   33,   21,    0,
               0,    4,   18,   30,   39,   34,   38,   16,
               9,   14,   19,   24,   16,   11,   21,   11,
               4,   14,   16,   28,   32,   -4,   -4,    3,
             -13,   23,   31,   20,   10,   15,   31,  -83,

            /* knights: KQ */
             -60,  -38,  -39,  -15,  -35,  -36,  -20,  -96,
             -29,  -20,  -19,  -28,  -25,  -22,    1,  -24,
             -29,  -24,   -6,   -8,   -1,  -31,  -23,   -6,
              -7,   -5,   16,    4,   16,    9,    2,   -8,
             -14,  -14,    4,    4,    4,    7,   -3,  -19,
             -24,  -22,  -23,  -15,   -3,    1,   16,    4,
             -31,  -31,  -31,    0,   -3,    7,   20,   -2,
             -77,  -24,  -25,   -1,  -17,   39,   17,  -40,

            /* knights: QK */
             -86,  -34,  -33,   -7,  -36,  -52,  -56,  -60,
             -44,    4,  -20,   -9,  -16,  -12,  -41,  -64,
              -1,  -13,  -18,    9,    3,   -5,  -16,  -41,
              -4,    6,   22,   27,   11,   11,    5,  -16,
              29,    9,   12,   13,   22,   11,   -1,  -19,
              31,   26,   24,   -5,    2,    3,   -8,   -3,
               4,   26,   15,   31,   13,  -15,  -35,  -27,
             -14,   42,   15,   16,  -12,  -25,   -3,  -78,

            /* knights: QQ */
             -52,  -35,  -27,  -21,  -36,  -19,  -54,  -90,
             -13,   -4,  -36,  -23,  -31,  -56,  -42,  -63,
             -17,   -7,  -23,  -12,   -8,  -17,  -25,  -42,
              17,    4,   13,    2,    3,   -4,   -8,  -16,
              -3,    7,   -1,    9,   -1,   -4,  -22,   -8,
              11,   13,   -8,  -15,   -7,   -1,  -15,    0,
               8,    5,   -8,   14,    5,    6,   10,    0,
             -33,   46,  -15,   11,  -14,    6,    5,  -86,

            /* bishops: KK */
             -17,  -20,  -28,   -3,   -7,    2,  -15,  -41,
             -10,  -27,  -15,    1,   -5,  -11,  -22,  -19,
               0,    2,   12,   11,   25,    0,   -4,    3,
              -6,    7,   17,   10,   -1,   15,    4,  -16,
              16,   16,    4,    6,   12,    9,   19,   23,
               1,   10,    8,    0,   17,   21,   14,   34,
              16,   16,   14,   12,   16,   10,   11,   -5,
              39,   29,   34,   29,   25,    6,   -6,   -7,

            /* bishops: KQ */
              34,   14,   -6,  -10,  -16,  -17,  -48,  -46,
             -13,   -1,   -3,   -9,  -11,  -38,  -22,  -32,
              -5,   -9,    1,    0,    4,  -11,   -6,  -24,
              -3,   13,   -1,  -29,   -8,   12,    7,    0,
               8,    5,  -31,  -33,   -5,    8,   20,    8,
              -7,  -18,  -12,   -5,   -2,    1,   15,   24,
             -37,  -11,   -7,    2,   -8,    4,   19,   27,
               0,    1,    4,    0,    9,   18,    9,   26,

            /* bishops: QK */
             -33,  -26,   -6,  -14,   -7,   -7,   16,    6,
             -37,  -15,    0,  -14,    1,   -4,   -2,  -15,
              -7,    3,    0,    5,   11,    1,  -11,   -2,
              -2,    4,    9,  -10,   -6,    6,    7,  -21,
              21,   24,   -4,   -1,   -9,   -9,   -1,    8,
              23,   36,   12,    6,   13,   -4,   -8,   -5,
              25,   23,   11,   13,    1,   17,   -1,  -10,
              27,   21,   18,   13,    5,    6,    1,  -24,

            /* bishops: QQ */
              12,   -4,  -42,  -18,  -36,  -38,  -38,  -46,
             -10,  -24,  -19,  -31,  -20,  -44,  -33,  -51,
             -15,  -12,  -28,  -32,  -27,  -32,  -41,  -17,
               3,  -10,  -22,  -41,  -32,  -28,  -37,  -34,
             -12,   -2,  -41,  -44,  -40,  -23,  -27,   -4,
             -13,  -14,  -18,  -32,  -12,  -35,  -15,  -21,
             -14,  -11,  -29,   -8,  -16,   -8,  -16,  -22,
             -17,   -9,  -11,  -32,    2,  -12,   -5,   -2,

            /* rooks: KK */
              20,   10,   13,   -7,  -16,   -4,    5,   -8,
               5,    7,    4,  -10,  -17,  -18,  -16,   -7,
              24,   21,   13,   -2,  -11,    0,   -2,   -5,
              33,   30,   23,   18,    7,   26,   19,   18,
              43,   44,   36,   24,   26,   30,   23,   17,
              48,   41,   38,   24,   20,   30,   36,   30,
              45,   50,   40,   15,   29,   28,   44,   25,
              40,   53,   35,   18,   37,   46,   41,   37,

            /* rooks: KQ */
             -15,  -27,  -23,  -21,  -22,  -17,   -6,   10,
             -15,  -25,  -24,  -30,  -23,  -11,  -19,   -4,
             -11,  -20,   -7,  -24,  -17,  -10,    2,   -8,
               4,   -9,    4,   -5,   -7,   19,   10,   -1,
               2,    0,   15,   12,    2,    9,   16,    4,
              11,    2,    7,   -6,   -3,   13,   17,   14,
               9,    9,   -2,   -4,    1,   -2,   18,    7,
              22,    9,   -1,    1,    3,   14,   23,    8,

            /* rooks: QK */
              -1,  -13,   -4,  -21,  -30,  -32,  -32,  -11,
             -16,   -5,   -4,  -20,  -16,  -30,  -33,  -43,
               0,    3,    1,  -16,  -14,   -8,  -30,  -36,
               0,   -2,    8,    3,    0,   -6,  -11,  -14,
              12,   29,   20,   17,    7,   16,   -1,   -8,
              18,   22,   21,   19,    1,   -3,    4,    1,
              10,   20,   13,   14,   23,   -4,    8,    3,
             -22,   15,   22,    9,   12,    0,    1,    2,

            /* rooks: QQ */
             -31,  -36,  -46,  -52,  -47,  -24,  -12,   -5,
             -37,  -19,  -26,  -48,  -49,  -46,  -56,  -36,
              -4,  -17,  -16,  -29,  -34,  -31,  -13,  -16,
             -14,   -8,   -8,  -19,  -29,  -21,  -15,   -2,
              -1,  -18,   -3,  -13,  -20,   -7,  -11,    1,
               3,   -9,   -4,   -3,  -17,    6,    0,    1,
               0,    2,   14,   -6,  -30,  -17,   -4,    9,
               6,    8,    0,   -6,  -20,  -10,   12,    6,

            /* queens: KK */
             -11,  -25,  -41,  -39,  -79,  -89,  -64,  -22,
               4,  -12,  -15,  -26,  -35,  -66,  -80,  -25,
              10,   -1,   10,    9,    1,    7,    8,  -16,
              12,   37,   25,   36,   21,   41,   25,   39,
              27,   37,   44,   52,   61,   66,   83,   87,
              29,   62,   55,   66,   76,   79,   91,  114,
              54,   84,   70,   65,   92,   72,  130,   81,
              48,   45,   53,   58,   62,   65,   74,   62,

            /* queens: KQ */
             -10,  -38,  -62,  -31,  -44,  -47,  -66,  -10,
               3,  -18,  -52,  -27,  -31,  -31,  -28,  -64,
               1,   -1,  -42,   -8,  -11,   22,   18,  -12,
              21,  -10,   10,   -3,   -1,   37,   30,   28,
              27,   23,   32,    9,   27,   33,   49,   40,
              21,   26,   35,   26,   36,   38,   39,   44,
              29,   32,   45,   50,   33,   57,   54,   27,
              31,   41,   42,   41,   41,   34,   12,   12,

            /* queens: QK */
             -43,  -51,  -86,  -56,  -74,  -55,  -41,  -45,
              -7,  -58,  -73,  -35,  -34,  -74,  -35,  -19,
             -21,  -12,   13,   10,  -15,  -13,  -14,  -17,
              -6,   23,   37,   10,   15,   13,  -15,   15,
              21,   30,   45,   27,   34,   24,   25,    2,
              -2,   48,   65,   49,   20,   22,   23,   13,
              17,   62,   62,   59,   36,   20,   37,   25,
              28,   26,   41,   32,   17,   47,   37,   27,

            /* queens: QQ */
             -58,  -55,  -26,  -49,  -79,  -50,  -59,  -41,
             -25,  -35,  -26,  -54,  -25,  -47,  -66,  -51,
               4,    0,  -24,    5,  -26,  -13,  -18,  -30,
             -16,    7,   14,   13,  -26,  -23,  -10,    1,
              14,   42,   24,   29,   12,   -5,   17,   11,
              39,   58,   31,   42,   30,    6,   22,   25,
              42,   53,   54,   57,   39,   35,   28,  -10,
             -12,   41,   47,   14,    6,  -24,    1,  -22,

            /* kings: KK */
              -1,  -14,  -21,  -21,  -24,  -15,  -24,  -47,
              -3,   -2,   -8,  -10,   -9,  -12,  -13,  -20,
              -6,    6,    4,    7,    6,    0,   -7,  -20,
               0,   18,   22,   20,   15,    8,   10,    1,
              19,   34,   34,   20,   17,   11,   17,    6,
              29,   55,   45,   39,   20,   14,   42,   22,
              28,   61,   51,   47,   26,   15,   46,   31,
             -27,   37,   51,   46,   26,   33,   49,  -42,

            /* kings: KQ */
              -1,  -14,  -21,  -21,   -8,   -9,  -26,  -36,
              -3,   -2,   -8,  -10,    0,   -7,   -4,  -17,
              -6,    6,    4,    7,   17,    5,    1,   -7,
               0,   18,   22,   20,   26,   27,   38,   12,
              19,   34,   34,   20,   40,   51,   49,   35,
              29,   55,   45,   39,   43,   55,   64,   49,
              28,   61,   51,   47,   33,   33,   46,   38,
             -27,   37,   51,   46,   15,   24,   15,  -28,

            /* kings: QK */
             -32,  -45,  -40,  -25,  -43,  -31,  -30,  -51,
             -39,  -12,  -22,  -20,  -13,  -13,  -15,  -24,
             -26,    1,   -7,   -2,    5,   -2,  -11,  -26,
              -1,   15,   19,   13,   18,   11,    9,   -9,
              23,   42,   33,   26,   19,   28,   27,   13,
              34,   47,   42,   36,   29,   46,   62,   27,
              13,   34,   42,   40,   48,   60,   74,   36,
              14,   42,   27,   27,   49,   59,   57,  -35,

            /* kings: QQ */
              53,    7,  -14,  -26,  -43,  -31,  -30,  -51,
               6,   -3,  -12,  -16,  -13,  -13,  -15,  -24,
              -4,    3,   -2,   -1,    5,   -2,  -11,  -26,
               3,   15,   17,   19,   18,   11,    9,   -9,
              20,   30,   25,   21,   19,   28,   27,   13,
              31,   42,   20,   29,   29,   46,   62,   27,
              11,   52,   24,   29,   48,   60,   74,   36,
             -47,   58,   31,   20,   49,   59,   57,  -35,

            #endregion

            /* end game mobility weights */

            2, // knights
            5, // bishops
            2, // rooks
            1, // queens

            /* end game squares attacked near enemy king */
            -1, // attacks to squares 1 from king
            -1, // attacks to squares 2 from king
            0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            17, // # friendly pawns 1 from king
            18, // # friendly pawns 2 from king
            11, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -7,

            /* end game backward pawns */
            -13,

            /* end game doubled pawns */
            -17,

            /* end game adjacent/connected pawns */
            9,

            /* end game passed pawns */
            39,

            /* end game knight on outpost */
            26,

            /* end game bishop on outpost */
            4,

            /* end game bishop pair */
            85,

            /* end game rook on open file */
            5,

            /* end game rook on half-open file */
            14,

            /* end game rook behind passed pawn */
            27,

            /* end game doubled rooks on file */
            7,

            /* end game king on open file */
            -1,

            /* end game king on half-open file */
            25,

            /* end game castling rights available */
            -10,

            /* end game castling complete */
            -8,

            /* end game center control */
            5, // D0
            2, // D1

            /* end game queen on open file */
            26,

            /* end game queen on half-open file */
            16,

            /* end game rook on seventh rank */
            17,
        };
    }
}