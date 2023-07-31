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
        public const int MAX_WEIGHTS = 3404;
        public const int ENDGAME_WEIGHTS = 1702;
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
        public const int BAD_BISHOP_PAWN = 1638;

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

        // Solution sample size: 12000000, generated on Sat, 29 Jul 2023 23:44:04 GMT
        // Object ID: 41a2f863-b760-4598-a1e9-2ebfac7ce255 - Optimized
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening phase material boundary */
            7100,

            /* opening piece values */
            105, 410, 440, 615, 1435, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -23,  -32,  -14,  -15,   -2,   51,   39,   -4,
             -22,  -15,   -6,   -3,    6,   33,   32,    9,
             -11,  -13,   14,   27,   37,   52,   21,    2,
             -10,   12,   35,   44,   68,   79,   42,   20,
              -9,   10,    8,   48,   83,  163,  164,   62,
              78,  121,  105,   95,   67,   12, -121,  -57,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -46,  -62,  -31,  -40,  -20,   40,   34,   -9,
             -51,  -51,  -22,   -7,    7,   29,   39,   12,
             -24,    1,   12,   28,   23,   27,   16,    9,
             -20,   25,   41,   43,   35,   31,   31,    9,
              43,   97,  116,   33,   54,   42,  121,   74,
              42, -112,   76,   84,  160,  107,  228,  195,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              -6,   33,   20,  -20,  -33,   -5,  -31,  -50,
              11,   38,    4,   -6,   -8,   12,  -21,  -34,
               8,   10,    7,   17,   27,   36,   13,  -11,
              -5,   27,   25,   31,   55,   82,   42,   10,
              13,   26,    7,   38,   92,  158,  167,   31,
             191,  245,  101,  119,   64,   47,  -86,  -97,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -18,   16,   25,   -6,  -19,   -5,  -17,  -25,
             -13,   11,   15,  -14,   18,   -6,  -11,   -7,
              -8,    2,   27,   35,   32,   27,    8,   -5,
              30,   74,   78,   31,   63,   46,   22,   16,
             115,  125,  110,   61,   40,   36,  110,   48,
             157,   26,  175,    0,  142,   55,  340,  282,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
            -110,   -4,  -19,   -1,   16,   13,    1,  -35,
             -36,  -28,    7,   28,   27,   28,   18,    8,
             -18,    5,    7,   41,   53,   17,   27,    8,
               8,   29,   33,   37,   46,   48,   50,   28,
              27,   19,   54,   65,   19,   53,    9,   60,
              15,   57,   55,   62,  106,  162,   44,    2,
              12,   20,   47,   65,   67,  102,   27,   39,
            -221,  -59,   -9,   23,  118,  -70,    8, -199,

            /* knights: KQ */
            -133,  -57,  -35,    4,   -9,  -14,  -31,  -44,
             -56,  -24,  -10,   26,    9,   21,  -34,  -41,
             -41,   -3,   -5,   42,   35,    8,    6,  -17,
               0,   49,   32,   63,   38,   46,   12,   19,
              37,   17,   48,   77,   66,   81,   25,   25,
              30,   74,  125,  142,   74,   58,   25,   15,
              54,  -13,   20,   67,   66,   52,   29,   -2,
            -273,  -44,   24,   59,   53,   14, -130, -170,

            /* knights: QK */
             -87,  -49,  -32,  -34,  -10,    1,  -49, -143,
             -15,    9,   -4,    8,    8,   -9,  -16,  -18,
             -21,  -12,   19,   56,   69,  -18,    7,  -29,
              26,   59,   39,   41,   54,   30,   38,   -6,
              34,   42,   79,   95,   53,   74,   33,   45,
               1,   39,   56,   75,  125,  147,   17,  -12,
             -36,  -30,   59,   79,   47,   63,   12,   45,
            -111,  -51,   11,  128,   16,    9,  114, -132,

            /* knights: QQ */
              56,   -1,    3,  -65,   -6,  -19,  -33, -259,
             -23,   -7,    2,   26,    8,   11,  -42,  -84,
              -2,   16,   20,   20,   40,  -24,   19,   -4,
               2,    2,   78,   66,   24,   37,   25,   -2,
              13,    5,   58,   77,   63,   65,   41,    8,
              26,   -3,   90,  160,  103,   71,  116,   36,
               3,   27,    5,  -32,   20,  -31,  -21, -122,
            -220,  -98,  -35, -102,   35,  -21,    5,  -78,

            /* bishops: KK */
              21,   61,   21,   17,   24,   39,   50,   44,
              62,   39,   65,   33,   50,   51,   76,   50,
              16,   57,   51,   56,   42,   57,   41,   47,
              35,   28,   48,   73,   71,   23,   31,   49,
              22,   39,   44,   91,   54,   54,   20,   19,
              50,   53,   49,   37,   56,   87,   67,   27,
               4,   38,   20,   29,    8,   14,  -50,  -11,
             -18,  -53,  -40,  -19,   -8,  -74,   50,  -26,

            /* bishops: KQ */
             -37,  -12,    1,   -4,   33,   33,   64,   13,
             -13,   24,   32,   26,   46,   73,   88,   50,
               5,   18,   41,   44,   49,   78,   54,   57,
              37,   28,   36,   72,  117,   52,   37,   27,
              -3,   50,   57,  116,   89,   47,   38,   13,
              16,   71,  101,   63,   89,   57,   47,   -5,
             -27,   58,   35,   34,   34,   -2,  -10,  -53,
             -60,  -53,   18,   15,   -6,   46,  -40,   -8,

            /* bishops: QK */
              32,   54,   24,   34,   -3,   12,   -6,  -94,
              63,   78,   62,   39,   29,    8,   -1,    2,
              42,   70,   71,   64,   28,   33,   25,    4,
              22,   63,   57,   92,   84,   13,   26,   14,
              24,   41,   72,  123,   61,   86,   22,    1,
               4,   44,   56,   53,   90,  100,  101,   27,
              15,   14,  -21,   -5,    3,   41,  -77,  -15,
             -56,  -92,  -23,  -27,   41,  -17,  127,  -92,

            /* bishops: QQ */
             -64,  -51,   26,  -48,  -17,   16,   20,   15,
              32,   43,   37,   49,    9,   46,   21,   43,
              26,   39,   40,   41,   47,   47,   40,   30,
              31,   51,   51,   93,   62,   48,   35,   42,
             -16,   39,   36,   94,   82,   34,   37,    8,
             -51,  113,   94,  120,   10,   33,   21,   20,
             -65,  -37,   90,   17,   42,  -14,   28,    3,
             -57,  -44,   40,   60,   38,   16,  -29, -103,

            /* rooks: KK */
              -7,    7,    8,   22,   28,   20,   34,   -4,
             -17,  -14,    2,   11,    3,   20,   42,    1,
             -33,  -22,  -13,   -2,    9,    5,   44,   14,
             -15,  -18,   -6,   11,   -3,  -15,   35,  -11,
              11,   13,   20,   53,   11,   36,   87,   42,
              38,   68,   74,   72,   99,  148,  127,   75,
               2,   -3,   17,   60,   28,  119,   90,  116,
              78,   76,   94,  104,   65,   89,  142,  137,

            /* rooks: KQ */
             -13,   22,    9,    8,   12,    7,    9,  -22,
             -46,  -12,  -14,   -9,    1,  -10,  -30,  -16,
             -43,  -10,  -35,    5,  -36,  -11,   20,   -6,
             -59,    4,  -30,    0,    5,  -36,   11,  -17,
              -3,   54,    2,   46,   43,   62,   52,   27,
              63,  124,   98,  163,  115,  101,  139,  104,
               9,   53,   87,  103,   89,   72,   64,   87,
              77,   79,  100,   82,   53,  115,  136,  144,

            /* rooks: QK */
             -48,    6,  -10,    8,    6,    7,   22,  -16,
             -14,   -1,   -2,    1,  -38,  -18,   -4,  -12,
             -16,  -19,   -8,  -20,  -26,  -28,   12,  -24,
               0,   25,  -25,  -13,  -35,  -29,    9,  -18,
              72,   53,   42,   16,   13,   31,    9,   24,
             114,   83,  103,   38,   80,  139,  145,   91,
              78,   56,   84,    4,   21,  100,   37,   60,
             246,  149,   96,   45,   48,   97,  132,  134,

            /* rooks: QQ */
             -16,  -25,   14,   23,   13,    9,   -1,  -19,
             -73,  -66,  -35,   11,    2,  -25,   22,  -24,
             -57,   -6,    9,  -24,  -46,  -28,  -35,  -18,
             -37,    2,   -3,   19,  -20,   25,  -40,  -15,
              63,  105,   -6,   71,   68,   19,   31,  -11,
              88,  170,  208,   82,  152,   96,  102,   65,
             146,  122,   63,   71,  211,  124,   18,    6,
              53,   33,    8,   -7,   50,  103,  147,   32,

            /* queens: KK */
              61,   73,   85,   89,  107,   73,   -2,   21,
              62,   80,   85,   87,   91,  113,  102,   56,
              54,   71,   78,   68,   81,   76,   89,   71,
              64,   46,   54,   57,   64,   50,   72,   56,
              57,   61,   43,   51,   42,   46,   46,   50,
              64,   66,   45,   32,   63,  117,   66,   69,
              34,   20,   39,   50,  -13,   77,   41,  160,
              26,   69,   71,   56,   67,  175,  221,  153,

            /* queens: KQ */
              50,   38,   53,   56,   48,   32,   -8,  -21,
              39,   67,   78,   55,   65,   80,    8,  -24,
              31,   52,   70,   52,   59,   56,   21,   24,
              35,   58,   31,   44,   47,   32,   12,    4,
              34,   35,    9,   68,   55,   48,   24,   27,
              64,  103,   99,   73,   58,   44,   50,   11,
             108,  137,  107,   86,   78,   15,   15,   16,
              98,  161,  124,  117,   77,   83,   71,    1,

            /* queens: QK */
             -28,  -71,   40,   18,   45,   29,   32,   12,
             -55,  -14,   63,   57,   51,   45,   50,   43,
             -21,   32,   33,   23,   38,    9,   26,   46,
               8,    0,    2,   39,    9,   25,   53,   30,
              30,   -7,   13,   25,   29,   22,   12,   24,
              23,   -6,    4,   -8,   58,   61,   85,   45,
              29,   -1,    0,  -13,  -24,    3,   25,   92,
             -25,   58,   36,   59,    4,  119,  162,  112,

            /* queens: QQ */
             -44,  -57,   47,  -27,   44,   -7,   19,    3,
             -95,  -12,   32,   48,    6,   13,    1,  -20,
               2,    9,   31,  -11,   10,   14,   46,   38,
             -32,    8,  -12,    0,   23,   17,    5,    0,
               5,  -23,  -14,   21,   54,   21,   -1,   37,
              33,   74,  164,   32,   36,   36,   59,   31,
             110,  168,  151,   85,   85,   12,   29,   -4,
              66,  110,  129,   89,   84,   63,   82,   71,

            /* kings: KK */
               0,    0,    0,    0,  -75,  -82,  -10,  -33,
               0,    0,    0,    0,  -24,  -12,   11,  -17,
               0,    0,    0,    0,   32,   -2,   -5,  -50,
               0,    0,    0,    0,  170,  104,   36,  -77,
               0,    0,    0,    0,  339,  345,  205,   34,
               0,    0,    0,    0,  368,  434,  401,  260,
               0,    0,    0,    0,  364,  292,  261,  164,
               0,    0,    0,    0,  157,  120,  133,  216,

            /* kings: KQ */
               0,    0,    0,    0, -107, -104,  -22,  -43,
               0,    0,    0,    0,  -22,   -5,  -12,  -18,
               0,    0,    0,    0,   36,   20,    8,  -44,
               0,    0,    0,    0,  166,  113,   32,    7,
               0,    0,    0,    0,  311,  189,  144,   41,
               0,    0,    0,    0,  300,  197,  143,   74,
               0,    0,    0,    0,  322,  313,  193,   64,
               0,    0,    0,    0,  204,  165,  184,  -28,

            /* kings: QK */
             -45,   -5,  -63, -117,    0,    0,    0,    0,
             -18,  -22,  -16,  -55,    0,    0,    0,    0,
             -47,    8,   -1,  -34,    0,    0,    0,    0,
             -43,   40,   81,   73,    0,    0,    0,    0,
             -33,  112,  125,  216,    0,    0,    0,    0,
              46,  143,  211,  241,    0,    0,    0,    0,
             140,  236,  222,  263,    0,    0,    0,    0,
             -27,  119,  242,  126,    0,    0,    0,    0,

            /* kings: QQ */
            -133,  -46,  -84, -110,    0,    0,    0,    0,
             -74,  -36,  -21,  -49,    0,    0,    0,    0,
            -102,  -28,    9,   15,    0,    0,    0,    0,
             -19,   70,   81,  133,    0,    0,    0,    0,
              49,  200,  240,  305,    0,    0,    0,    0,
             156,  292,  315,  257,    0,    0,    0,    0,
              67,   48,  194,  282,    0,    0,    0,    0,
              44,   57,   82,  118,    0,    0,    0,    0,

            #endregion

            /* opening mobility weights */

            11, // knights
            6, // bishops
            3, // rooks
            1, // queens

            /* opening squares attacked near enemy king */
            21, // attacks to squares 1 from king
            19, // attacks to squares 2 from king
            7, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
            20, // # friendly pawns 1 from king
            15, // # friendly pawns 2 from king
            10, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -8,

            /* opening backward pawns */
            -19,

            /* opening doubled pawns */
            -22,

            /* opening adjacent/connected pawns */
            9,

            /* UNUSED (was opening king adjacent open file) */
            0,

            /* opening knight on outpost */
            -2,

            /* opening bishop on outpost */
            14,

            /* opening bishop pair */
            47,

            /* opening rook on open file */
            35,

            /* opening rook on half-open file */
            14,

            /* opening rook behind passed pawn */
            -6,

            /* opening doubled rooks on file */
            12,

            /* opening king on open file */
            -61,

            /* opening king on half-open file */
            -22,

            /* opening castling rights available */
            27,

            /* opening castling complete */
            19,

            /* opening center control */
            1, // D0
            2, // D1

            /* opening queen on open file */
            -6,

            /* opening queen on half-open file */
            9,

            /* opening rook on seventh rank */
            33,

            /* opening passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
               3,   -5,  -24,  -78,   -9,  -16,    0,   24,
               1,   -2,  -14,  -20,  -49,  -55,  -55,   21,
              35,   36,   -1,   -6,  -27,  -35,  -30,    1,
              86,   67,   44,   39,   10,   19,   24,   42,
             190,  129,  136,   75,   38,   97,  -24,   36,
             158,  142,  127,  137,  117,   77,   79,   84,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -2,    0,   -9,   13,  -17,  -22,  -25,    3,
              -5,   -4,  -11,   -6,  -12,  -20,  -15,  -12,
              -7,    7,   -4,   -9,  -19,  -12,   -4,   -1,
              10,    8,   -6,    1,   -8,   -9,    5,    4,
              28,   19,   28,   14,   51,   -2,   24,    4,
             103,   76,   86,   85,   92,  124,   75,  155,
               0,    0,    0,    0,    0,    0,    0,    0,


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game phase material boundary */
            700,

            /* end game piece values */
            100, 350, 380, 625, 1170, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              49,   39,   32,   11,   43,   34,   14,   11,
              46,   31,   27,   20,   20,   22,   13,   13,
              47,   47,   27,    3,    6,   17,   27,   18,
              86,   57,   47,   17,    9,   22,   41,   31,
              95,   80,   89,   66,   64,   50,   46,   29,
             161,  147,  122,   96,   66,   22,   58,   23,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              55,   58,   53,   52,   20,   12,   -1,   25,
              48,   43,   37,   26,    0,    5,   -1,   16,
              37,   31,   27,    9,    9,   28,   26,   36,
              47,   15,    5,   17,   28,   73,   69,   87,
               8,  -30,  -22,   38,  123,  166,  172,  150,
              12,   48,  -25,   36,  145,  178,  181,  172,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              31,    2,   19,   13,   45,   41,   50,   64,
              35,    6,   19,    5,   18,   24,   29,   46,
              50,   50,   43,   15,   19,   20,   26,   32,
             101,   86,   79,   44,   25,   12,   24,   35,
             125,  127,  153,  101,   60,   34,   22,   28,
             146,  166,  202,  133,   63,   -9,   -3,   16,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              17,    4,   21,    5,   21,   18,   14,   43,
              19,    7,   15,   14,   -4,    9,    1,   21,
              23,   28,   17,    5,    6,    7,   15,   30,
              28,    8,   12,   22,    7,   33,   33,   51,
             -19,  -16,   -7,   21,   66,   84,   95,   94,
              11,   63,  -11,   45,   84,  105,   99,   96,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
               8,  -46,   -3,   10,    2,   11,   -8,  -24,
             -19,   28,    7,   -2,    7,   26,   26,   24,
               9,   24,   29,   44,   38,   17,   32,   49,
              41,   45,   58,   67,   69,   65,   62,   48,
              50,   37,   45,   57,   64,   68,   83,   63,
              64,   41,   48,   48,   44,   41,   74,   75,
              56,   58,   43,   60,   59,   36,   49,   52,
              71,   81,   73,   66,   49,   57,   60,  -51,

            /* knights: KQ */
             -34,  -26,    7,    5,  -13,    2,   23,  -85,
               1,   12,  -15,  -18,   -8,   -7,   17,   -9,
              19,  -10,   11,   -7,    8,   -6,   -6,   50,
              18,    4,   19,   -4,   17,    8,    8,   18,
              -7,  -24,    1,  -12,   -6,   -3,    0,    2,
             -17,  -25,  -46,  -37,   -4,   12,   25,   21,
             -25,  -24,  -23,   -6,  -22,   13,   30,   17,
             -25,  -16,  -25,   12,  -32,   45,   75,    2,

            /* knights: QK */
             -65,   18,    5,   18,   -6,  -21,  -11,   -1,
             -20,   34,   -3,    0,    0,    7,   -9,  -15,
              47,   11,    9,   15,    4,    9,   -8,   -1,
              28,    5,   24,   29,   13,   25,    5,    3,
              34,   17,   20,    2,   24,    6,    7,   -2,
              43,   40,   33,    6,   -5,  -10,   11,   36,
              46,   47,   29,   38,   22,   -4,   -8,  -22,
              33,   78,   36,   32,   23,   -4,   -7, -119,

            /* knights: QQ */
             -55,  -37,  -21,   -1,  -15,  -15,  -46,  -35,
               0,   15,  -44,  -37,  -37,  -57,  -41,  -18,
              -4,  -16,  -27,  -17,  -29,  -16,  -31,  -36,
              33,   -4,   -9,  -25,  -19,  -26,  -13,    2,
               7,   -9,  -42,  -32,  -22,  -24,  -27,   28,
              16,    7,  -40,  -49,  -49,  -11,  -20,   -2,
              40,    9,  -32,   10,  -11,    5,   35,   23,
             -36,   75,    0,   39,  -11,   18,   61,  -54,

            /* bishops: KK */
              48,   41,   34,   81,   69,   66,   33,    7,
              53,   41,   52,   71,   59,   54,   32,   22,
              79,   74,   89,   84,  103,   71,   60,   63,
              75,   89,  100,   78,   67,   91,   75,   43,
              95,   79,   76,   65,   72,   86,   76,   96,
              77,   80,   76,   80,   89,   97,   87,  106,
              89,   78,   90,   85,   89,   86,   80,   56,
              88,  113,  120,  110,  100,   96,   64,   76,

            /* bishops: KQ */
              78,   74,   46,   59,   25,   27,  -26,  -34,
              61,   40,   47,   33,   31,   -5,   12,   -2,
              67,   52,   54,   47,   45,   33,   34,   19,
              65,   54,   59,   14,   22,   50,   53,   46,
              72,   37,   19,   -8,   31,   53,   55,   61,
              46,   17,   32,   38,   46,   46,   62,   81,
               5,   23,   22,   31,   38,   66,   65,   92,
              40,   46,   48,   47,   60,   55,   70,   78,

            /* bishops: QK */
              -3,   11,   44,   42,   54,   39,   70,   68,
              25,   30,   45,   48,   55,   48,   43,   44,
              57,   55,   54,   58,   73,   50,   39,   54,
              61,   57,   62,   31,   30,   68,   59,   34,
              74,   64,   47,   37,   34,   37,   48,   74,
              77,   84,   59,   62,   60,   47,   27,   49,
              73,   63,   76,   66,   52,   53,   48,   27,
              82,   87,   66,   68,   53,   63,   25,   40,

            /* bishops: QQ */
              34,   34,  -10,   25,    8,   -5,  -10,  -16,
              18,    4,    8,   -3,    8,  -19,   -2,  -23,
              32,   16,   14,    3,    4,    6,  -12,    5,
              39,   22,   12,  -25,  -11,    6,   -3,    3,
              29,    7,  -11,  -23,  -25,    7,   -4,   31,
              34,  -13,   -6,   -1,   28,   -2,    6,    3,
               3,   10,  -10,   17,   -4,   17,   12,    5,
               9,   11,   14,    0,   33,   35,   20,   34,

            /* rooks: KK */
             171,  156,  140,  115,  100,  127,  136,  128,
             165,  162,  131,  109,  108,   99,  122,  128,
             158,  157,  128,  106,   93,  106,  107,  105,
             169,  167,  144,  119,  112,  134,  141,  142,
             182,  178,  150,  125,  136,  133,  141,  136,
             186,  171,  149,  132,  118,  124,  149,  153,
             202,  209,  169,  139,  153,  143,  184,  169,
             190,  204,  161,  136,  165,  179,  191,  176,

            /* rooks: KQ */
              85,   63,   58,   55,   53,   69,   91,  111,
              96,   72,   51,   49,   55,   71,  104,   82,
              72,   47,   48,   22,   52,   42,   72,   55,
              93,   71,   72,   42,   45,   78,   87,   92,
              88,   76,   60,   51,   53,   66,   94,  104,
              83,   62,   43,   30,   45,   67,   75,   91,
             114,  100,   65,   60,   66,   66,  118,  107,
             122,  110,   69,   79,   86,   79,  115,  100,

            /* rooks: QK */
             119,  102,   89,   57,   53,   51,   62,   96,
             102,  103,   84,   63,   71,   58,   63,   63,
              88,   93,   65,   53,   42,   53,   38,   50,
              90,   88,   81,   67,   59,   68,   63,   70,
             101,  112,   89,   82,   70,   61,   76,   68,
             101,  111,   84,   81,   69,   44,   72,   70,
             112,  125,   90,  102,   99,   68,  110,  108,
              37,  102,  108,  103,  100,   76,  105,   97,

            /* rooks: QQ */
              55,   58,   21,    5,   13,   37,   78,   84,
              53,   69,   33,   -1,   -1,   17,   22,   51,
              62,   26,    9,    1,   22,    8,   52,   51,
              52,   39,   28,    7,   19,   20,   56,   64,
              34,   23,   39,   12,    7,   31,   53,   68,
              56,   14,   16,   26,   -7,   26,   52,   63,
              55,   68,   56,   41,   -2,   12,   69,   90,
              91,   94,   78,   80,   37,   41,   85,   97,

            /* queens: KK */
             136,  117,   74,   85,   34,   13,   94,  130,
             157,  125,  100,   91,   77,   46,   45,   92,
             138,  121,  119,  108,  100,  126,  130,  120,
             149,  167,  143,  138,  114,  166,  160,  194,
             185,  174,  167,  151,  166,  206,  244,  264,
             182,  189,  184,  189,  202,  221,  262,  297,
             225,  243,  212,  197,  263,  229,  342,  259,
             217,  207,  198,  209,  229,  200,  230,  230,

            /* queens: KQ */
              28,   21,  -24,  -11,   29,   -1,   -2,   47,
              63,   20,  -42,  -12,  -15,  -23,   43,  -27,
              47,   20,  -40,   13,   -6,   56,   84,   57,
             102,   13,   29,  -18,   16,   56,   98,   98,
              79,   65,   54,   -5,   18,   54,  115,  128,
              65,   50,   31,   50,   56,   80,  117,  122,
              75,   65,   68,   59,   70,  122,  158,  125,
             100,   79,   54,   85,  102,   95,   97,  110,

            /* queens: QK */
              24,   46,  -66,   -4,  -33,  -44,    0,   33,
              76,   -6,  -30,  -15,  -35,  -40,   21,   48,
              27,   21,   44,   36,   14,   21,   16,   24,
              55,   66,   59,   20,   46,   20,   29,   90,
              92,   96,   73,   60,   42,   51,   80,   59,
              63,  127,  117,   94,   36,   66,   80,   58,
              96,  122,  129,  110,   94,  107,   99,  102,
             111,  114,  110,   89,   91,   85,  103,   97,

            /* queens: QQ */
             -66,   -9,  -45,  -23,  -72,  -36,  -24,    7,
             -26,  -30,    2,  -71,  -19,  -63,  -14,  -11,
              33,   17,  -45,    2,  -18,  -30,  -21,  -18,
              61,   21,   -8,  -21,  -55,  -48,  -56,   57,
              83,   91,   23,  -10,  -26,  -29,   36,   14,
              62,   81,  -11,   35,   -7,   10,   38,   18,
              73,   57,   36,   45,   39,   56,   71,   53,
              -1,   90,   59,   -4,   41,   -7,   29,    0,

            /* kings: KK */
               0,    0,    0,    0,   -9,    3,  -17,  -40,
               0,    0,    0,    0,    9,    5,   -1,   -7,
               0,    0,    0,    0,   21,   17,    7,   -9,
               0,    0,    0,    0,   16,   18,   15,    7,
               0,    0,    0,    0,    1,   -7,    9,    4,
               0,    0,    0,    0,   -4,   -8,   21,   -1,
               0,    0,    0,    0,   -7,   -3,   29,   13,
               0,    0,    0,    0,   10,   19,   30,  -98,

            /* kings: KQ */
               0,    0,    0,    0,   14,    1,  -21,  -24,
               0,    0,    0,    0,   13,    5,   12,   -6,
               0,    0,    0,    0,   24,   14,   10,   -3,
               0,    0,    0,    0,   23,   27,   36,   14,
               0,    0,    0,    0,   25,   45,   47,   35,
               0,    0,    0,    0,   31,   48,   52,   41,
               0,    0,    0,    0,    1,    5,   20,   17,
               0,    0,    0,    0,   -7,   -5,  -23,  -41,

            /* kings: QK */
             -41,  -55,  -36,  -21,    0,    0,    0,    0,
             -30,  -10,  -27,  -15,    0,    0,    0,    0,
             -21,   -5,   -8,    5,    0,    0,    0,    0,
              -8,   11,   10,   10,    0,    0,    0,    0,
              20,   25,   23,    5,    0,    0,    0,    0,
              13,   28,   21,   12,    0,    0,    0,    0,
             -22,    2,   12,    2,    0,    0,    0,    0,
             -19,    2,  -17,   -5,    0,    0,    0,    0,

            /* kings: QQ */
              49,   -2,  -11,  -23,    0,    0,    0,    0,
               4,   -2,  -12,  -14,    0,    0,    0,    0,
              -4,   -4,   -7,   -2,    0,    0,    0,    0,
             -14,    5,    9,    6,    0,    0,    0,    0,
               3,   10,   14,    4,    0,    0,    0,    0,
               4,   24,    3,   13,    0,    0,    0,    0,
              -6,   52,   11,   11,    0,    0,    0,    0,
             -90,   43,   19,    9,    0,    0,    0,    0,

            #endregion

            /* end game mobility weights */

            8, // knights
            3, // bishops
            2, // rooks
            2, // queens

            /* end game squares attacked near enemy king */
            -3, // attacks to squares 1 from king
            -1, // attacks to squares 2 from king
            0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            16, // # friendly pawns 1 from king
            19, // # friendly pawns 2 from king
            11, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -8,

            /* end game backward pawns */
            -13,

            /* end game doubled pawns */
            -27,

            /* end game adjacent/connected pawns */
            10,

            /* UNUSED (was end game king adjacent open file) */
            0,

            /* end game knight on outpost */
            32,

            /* end game bishop on outpost */
            22,

            /* end game bishop pair */
            85,

            /* end game rook on open file */
            4,

            /* end game rook on half-open file */
            25,

            /* end game rook behind passed pawn */
            21,

            /* end game doubled rooks on file */
            9,

            /* end game king on open file */
            -1,

            /* end game king on half-open file */
            33,

            /* end game castling rights available */
            -20,

            /* end game castling complete */
            -11,

            /* end game center control */
            11, // D0
            9, // D1

            /* end game queen on open file */
            31,

            /* end game queen on half-open file */
            22,

            /* end game rook on seventh rank */
            18,

            /* end game passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              20,   35,   34,   57,    3,   14,   44,   14,
              22,   36,   21,    8,   27,   32,   56,   19,
              64,   55,   40,   36,   37,   44,   76,   61,
              90,   95,   67,   56,   66,   57,   78,   78,
             136,  169,  117,   98,   88,   77,  118,  102,
             140,  155,  166,  145,  161,  166,  181,  161,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -1,   -3,   -3,  -31,  -15,    5,   -4,   -6,
              -9,  -14,   -9,  -24,  -17,   -3,   -5,   -7,
             -10,  -27,  -34,  -45,  -31,  -21,  -20,   -8,
             -33,  -38,  -34,  -57,  -41,  -34,  -32,  -34,
             -44,  -64,  -79,  -77,  -99,  -68, -103,  -38,
             -77, -121, -121, -152, -187, -144, -168, -153,
               0,    0,    0,    0,    0,    0,    0,    0,
        };
    }
}