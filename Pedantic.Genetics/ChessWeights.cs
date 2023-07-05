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
        public const int MAX_WEIGHTS = 3164;
        public const int ENDGAME_WEIGHTS = 1582;
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

        // Solution sample size: 12000000, generated on Fri, 30 Jun 2023 18:51:03 GMT
        // Object ID: ddec04a7-edef-4619-88a8-d56da514ffdf - Optimized
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening phase material boundary */
            6850,

            /* opening piece values */
            100, 385, 415, 580, 1365, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -17,  -28,  -25,    4,  -24,   28,   16,   -7,
             -18,  -12,  -22,  -14,  -12,   11,   18,   -5,
             -14,   -4,    1,    6,    9,   30,   18,   -4,
               5,   16,   21,   29,   44,   63,   42,   20,
              34,   51,   93,   89,  105,  156,  144,   52,
             274,  236,  238,  243,  185,  140,   -7,   14,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -46,  -61,  -32,  -43,  -35,   13,   22,  -15,
             -51,  -45,  -23,  -20,  -15,    7,   22,   -2,
             -28,   -3,   -1,    0,   -7,   10,    8,    6,
             -17,   25,   23,   28,   14,   16,   24,    9,
              56,   78,  112,   68,   88,   72,   88,   50,
             121,   74,  118,  162,  192,  198,  123,  136,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -10,   19,   -8,  -22,  -52,  -30,  -50,  -48,
               8,   31,  -22,  -17,  -26,  -10,  -21,  -48,
               9,   21,  -16,   -4,   -3,   17,    3,  -18,
              21,   17,    5,   13,   30,   59,   35,    3,
              78,  102,   77,   78,  140,  123,  110,   -1,
             216,  251,  170,  210,  185,  155,  -14,  -11,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -39,   -6,    8,  -13,  -33,  -17,  -27,  -14,
             -29,  -12,  -16,  -28,  -10,  -23,  -13,  -20,
             -18,   -4,    3,    0,   -6,    2,    5,   -4,
              28,   34,   42,   24,   30,   48,   20,   16,
              95,  101,  109,  107,   84,   47,   63,   51,
             167,  120,  170,  146,  169,  135,  146,  118,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -92,  -14,  -29,  -12,    4,   11,   -5,  -38,
             -38,  -29,   -4,   16,   14,   23,    8,    2,
             -27,   -3,   -8,   30,   37,    7,   16,    0,
              -3,   17,   21,   25,   32,   31,   43,   14,
              21,    2,   34,   51,   11,   41,    3,   58,
              16,   38,   44,   49,   98,  155,   37,   19,
              -2,    9,   23,   72,   57,  108,   38,   49,
            -174,  -37,   -3,   31,   83, -109,   48, -156,

            /* knights: KQ */
            -135,  -42,  -15,  -21,  -17,    0,  -19,  -90,
             -38,  -17,  -14,   24,    7,   26,  -29,  -48,
             -38,    7,   -2,   41,   30,    7,   -2,  -24,
               8,   57,   38,   49,   36,   38,   17,   11,
              45,    4,   48,   74,   42,   55,   14,   60,
              23,   76,  101,  111,   83,   88,   36,   35,
              18,   15,   20,   82,   50,   71,   35,    7,
            -271, -110,  -36,    0,   74,  -86,  -74, -146,

            /* knights: QK */
            -108,  -46,  -36,  -41,    0,  -17,  -40, -111,
             -45,   -8,    0,    6,    4,  -14,   -2,   -9,
             -14,   -8,   16,   34,   49,  -21,   -5,  -23,
              23,   37,   33,   31,   44,   32,   42,    2,
              14,   30,   83,   96,   42,   61,   36,   33,
             -20,   16,   54,   87,  118,  152,   31,   28,
              -3,   37,   56,   80,   34,   74,   71,   38,
            -165, -101,  -16,   37,   44,  -77,   29,  -69,

            /* knights: QQ */
             -36,  -28,    8,  -38,  -10,  -34,  -44, -162,
             -47,   25,   -8,    8,   11,    3,  -30,  -70,
              -5,   10,   15,   28,   27,  -26,   18,  -43,
              11,   -9,   60,   47,   14,   31,   23,   -2,
              32,   13,   41,   77,   60,   43,   26,   19,
              24,   13,   62,  123,  109,   86,   78,   66,
              -6,   37,   42,   12,   46,   62,    3,  -28,
            -254,  -89,   -1,  -67,   70,  -86,  -19,  -93,

            /* bishops: KK */
             -20,   25,  -10,  -17,  -10,    2,   17,  -12,
              27,    0,   23,   -5,    9,   19,   36,   -3,
             -13,   19,   13,   14,    8,   15,    9,    3,
              -1,  -10,    5,   28,   31,  -19,   -8,   24,
             -16,   -3,   -1,   43,    7,   19,  -13,  -21,
              18,   22,    3,    1,   12,   52,   53,   -3,
             -22,  -19,  -17,   10,  -17,  -18,  -79,  -53,
             -43,  -65,  -43,  -37,  -45,  -60,   24,  -25,

            /* bishops: KQ */
             -78,   -4,  -17,  -17,    1,    5,   46,   -6,
             -12,  -11,   14,   -1,   20,   40,   65,   30,
              -3,   16,   17,   29,   17,   45,   26,   47,
               5,   10,   16,   43,   85,   18,   15,   13,
             -20,   21,   46,   73,   58,   26,   18,  -23,
              14,   70,   88,   26,   52,   17,   23,   -4,
              -7,   26,  -32,    4,   23,   11,  -30,  -48,
             -70,  -47,  -36,  -12,  -71,  -13,  -13,  -10,

            /* bishops: QK */
              39,   24,   11,   10,  -16,  -16,  -21, -109,
              28,   50,   35,   35,   -2,   -6,  -29,  -15,
              55,   45,   40,   37,   20,   -3,   -1,  -19,
              12,   48,   28,   66,   49,   -5,  -12,   20,
               6,   20,   62,  104,   35,   44,   11,  -16,
              -3,   27,   40,   34,   48,   70,   34,   32,
              -4,   23,  -14,   -7,   28,    0,  -48,  -52,
             -86,  -67,  -63,  -16,   -9,  -46,   31,  -50,

            /* bishops: QQ */
             -81,  -52,   18,  -60,  -21,   -6,   37,   13,
              45,   23,    7,   16,   -7,   12,    8,   25,
               9,   34,   20,   17,   31,   37,   33,   -1,
              18,   20,   16,   63,   73,   25,   16,   25,
              -9,   26,   12,   95,   60,   16,   16,  -19,
             -46,   81,   56,   48,   51,   36,   19,   -1,
             -45,    0,    0,    7,   21,  -20,  -15,  -25,
             -31,  -40,  -53,  -38,   -5,  -29,   12,  -41,

            /* rooks: KK */
             -20,   -9,  -12,   -4,    4,    1,    5,  -20,
             -31,  -32,  -18,  -15,  -19,   -7,   21,  -20,
             -45,  -42,  -35,  -27,  -15,  -16,   14,  -11,
             -36,  -30,  -27,  -17,  -26,  -41,   10,  -37,
               0,    0,   -5,   27,   -9,    8,   51,   34,
              17,   52,   47,   41,   67,  102,   85,   55,
              11,   13,   11,   55,   22,  102,   88,  122,
              61,   78,   59,   59,   43,   60,  125,  118,

            /* rooks: KQ */
             -21,   20,   -5,  -19,   -8,  -13,   -9,  -40,
             -64,  -18,  -35,  -29,  -20,  -28,    1,  -40,
             -32,  -17,  -65,  -46,  -44,  -44,   -6,  -11,
             -53,  -16,  -35,  -21,  -24,  -52,   -1,  -19,
               8,   46,  -22,   10,    4,   46,   32,   47,
              41,   86,   43,   95,   86,   79,   92,  101,
              33,   34,   51,   93,   72,   87,   94,  105,
              80,   81,   82,   71,   58,   62,   93,   96,

            /* rooks: QK */
             -31,   14,  -23,  -17,   -4,   -7,   13,  -22,
             -21,  -39,  -25,  -21,  -50,  -37,  -19,  -17,
             -24,  -31,  -37,  -44,  -55,  -56,   -6,  -18,
             -16,   14,  -43,  -41,  -55,  -36,  -23,  -28,
              50,   31,   36,   -3,    6,    0,   13,    0,
              84,   99,   82,    5,   82,  114,  100,   53,
              83,   90,   82,   51,   27,  100,   64,   76,
             156,  125,   82,   61,   50,   83,  103,  117,

            /* rooks: QQ */
             -19,   -6,    6,    8,   -5,    3,   -1,  -27,
             -48,  -52,  -33,   -1,   -3,  -14,   -9,  -21,
             -46,  -40,   -9,  -47,  -64,  -30,  -43,  -17,
              -8,  -13,  -37,   -9,  -31,   -5,  -38,  -49,
              17,   76,  -26,   54,   41,    4,   31,  -20,
              50,  105,   91,   82,   94,   80,   85,   63,
              52,   55,   65,  105,  130,  101,   48,   38,
              53,   81,   58,   56,   46,   84,  114,   51,

            /* queens: KK */
              -4,    4,   11,   15,   28,    1,  -53,  -42,
              -8,    8,    8,   15,   16,   35,   22,  -17,
             -10,    2,    4,  -14,   -1,    3,   13,    3,
              -8,  -20,  -20,  -26,  -22,  -18,    5,   -3,
              -8,   -6,  -24,  -23,  -39,  -19,  -14,  -15,
               0,  -11,  -26,  -39,   -4,   53,    8,    8,
             -22,  -45,  -25,  -25,  -56,   15,    2,   96,
             -42,   25,    4,   -1,   10,   70,  114,   87,

            /* queens: KQ */
              -3,    8,    5,   15,   20,   -9,  -41,  -26,
              18,   29,   27,   12,    8,   18,  -18,  -44,
               4,    5,   24,    2,    3,   14,  -12,   -3,
              -6,   13,  -17,   -3,    2,   -8,  -28,  -29,
              -6,  -13,  -40,   15,   -4,   -4,  -18,    4,
              17,   24,  -13,    6,   14,   38,   12,    2,
              33,    9,   -3,   11,  -15,    4,   12,   23,
              35,   69,   32,   32,    5,   34,   53,   40,

            /* queens: QK */
             -42,  -61,   -4,  -14,    1,  -20,  -15,   -4,
             -66,  -43,   14,   18,   -5,   10,   43,   19,
             -52,  -15,  -10,  -19,   -7,  -17,   -8,   14,
             -29,  -38,  -48,  -23,  -31,  -16,    9,    0,
               1,  -33,  -32,  -15,  -42,   -6,  -22,  -28,
               1,  -18,  -19,  -18,    5,   57,   29,   23,
             -10,  -31,  -27,  -42,  -49,  -26,    0,   23,
             -40,   34,   10,    3,  -12,   88,  102,   95,

            /* queens: QQ */
             -21,  -82,    6,  -26,   39,   10,   45,   -7,
             -73,  -21,   10,   20,  -19,   -2,   17,   -2,
              -6,    6,   -7,  -37,   -4,  -10,   -2,    2,
             -22,   12,  -40,  -46,   -2,  -11,   -7,  -20,
              20,  -46,   -6,  -28,    0,   -3,   15,   -5,
              32,    5,   52,   28,   -4,   19,   52,    6,
              35,   12,   17,   34,   17,    1,    2,    9,
              35,   77,   55,   21,   41,   67,   59,   66,

            /* kings: KK */
               2,   58,    7,  -67,  -56,  -59,    0,  -21,
              32,   23,   12,  -32,  -15,   -7,   16,   -6,
               2,   11,    6,  -33,   27,   -6,   -4,  -42,
             -36,   32,  -11,  -20,   87,   55,   -3,  -83,
              15,    7,   21,   11,  119,  147,   84,  -21,
               4,   59,   33,   -1,  112,  104,  124,   61,
              56,   30,    5,   17,  104,   86,   60,   30,
             -51,   51,   10,  -28,  -51,   14,   59,  103,

            /* kings: KQ */
               2,   58,    7,  -67,  -67,  -74,   -3,  -11,
              32,   23,   12,  -32,  -14,   -2,    9,    1,
               2,   11,    6,  -33,   -1,    6,   13,  -45,
             -36,   32,  -11,  -20,   43,   54,  -30,  -33,
              15,    7,   21,   11,   97,   68,   65,    3,
               4,   59,   33,   -1,   77,   70,   69,   21,
              56,   30,    5,   17,   65,   82,   46,   11,
             -51,   51,   10,  -28,  -48,   -3,   82,   40,

            /* kings: QK */
             -14,   31,   -9,  -76,  -13,  -48,   15,   -8,
              28,   15,    5,  -24,  -20,   -9,   22,    0,
             -16,   25,    3,  -34,  -21,  -28,  -14,  -32,
             -11,   48,   44,   46,  -18,  -17,  -31,  -78,
               7,   62,   65,  106,   -4,   12,   11,  -38,
             -17,   97,  102,   88,    1,   18,   47,   -9,
             107,  111,   86,   76,   10,   13,  -15,  -16,
             -50,   53,   63,  -19,  -65,  -26,   27,   38,

            /* kings: QQ */
             -59,   18,  -23,  -64,  -13,  -48,   15,   -8,
              -7,   10,   26,  -18,  -20,   -9,   22,    0,
             -42,   16,   17,    3,  -21,  -28,  -14,  -32,
               2,   67,   24,   34,  -18,  -17,  -31,  -78,
              68,   61,   91,  132,   -4,   12,   11,  -38,
              84,  164,   99,   96,    1,   18,   47,   -9,
              73,   34,   65,   74,   10,   13,  -15,  -16,
             -44,   62,   18,  -22,  -65,  -26,   27,   38,

            #endregion

            /* opening mobility weights */

            10, // knights
            6, // bishops
            3, // rooks
            1, // queens

            /* opening squares attacked near enemy king */
            15, // attacks to squares 1 from king
            17, // attacks to squares 2 from king
            5, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
            22, // # friendly pawns 1 from king
            17, // # friendly pawns 2 from king
            10, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -4,

            /* opening backward pawns */
            -23,

            /* opening doubled pawns */
            -21,

            /* opening adjacent/connected pawns */
            10,

            /* UNUSED (was - opening king adjacent open file) */
            0,

            /* opening knight on outpost */
            -1,

            /* opening bishop on outpost */
            12,

            /* opening bishop pair */
            30,

            /* opening rook on open file */
            30,

            /* opening rook on half-open file */
            14,

            /* opening rook behind passed pawn */
            -4,

            /* opening doubled rooks on file */
            12,

            /* opening king on open file */
            -49,

            /* opening king on half-open file */
            -16,

            /* opening castling rights available */
            22,

            /* opening castling complete */
            19,

            /* opening center control */
            4, // D0
            3, // D1

            /* opening queen on open file */
            -5,

            /* opening queen on half-open file */
            9,

            /* opening rook on seventh rank */
            9,

            /* opening passed pawn */
            0, 0, 4, 7, 38, 53, 44, 0,

            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game phase material boundary */
            1000,

            /* end game piece values */
            100, 325, 315, 585, 1020, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              32,   26,   15,  -13,   28,   15,   -1,  -11,
              25,   13,    3,   -9,    2,    0,   -6,   -5,
              30,   21,   -7,  -29,  -18,   -6,    3,   -4,
              54,   30,   19,  -15,   -9,   -4,   13,    2,
             103,   91,   64,   35,   28,   25,   31,   26,
             144,  145,  137,   79,   79,   45,   78,   47,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              41,   51,   38,   43,    6,    0,   -8,    5,
              34,   34,   12,    7,  -13,   -9,   -9,   -4,
              29,   14,   -2,  -15,  -10,    6,   11,   13,
              37,    9,    0,  -11,   12,   44,   45,   51,
              46,   26,    1,   22,   81,  114,  141,  127,
              68,   50,   33,   41,  169,  204,  227,  192,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              20,    4,    5,   -7,   27,   27,   32,   39,
              16,   -2,   -5,  -21,    3,   12,   17,   29,
              34,   23,   10,  -11,   -4,    5,    9,   18,
              74,   69,   51,   12,    4,   -8,   14,   14,
             129,  148,  138,   81,   17,   17,   28,   33,
             181,  206,  222,  168,   77,   18,   32,   44,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              17,    7,    8,  -24,    1,    3,    6,   22,
               7,    2,    2,   -5,   -9,  -10,  -11,   11,
               9,   13,   -4,  -12,  -11,  -10,    4,   12,
              14,   10,    2,   -7,   -6,    5,    9,   20,
              26,   45,   17,    8,   41,   52,   73,   73,
              51,   86,   40,   44,   91,  141,  153,  158,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -57,  -81,  -48,  -31,  -44,  -42,  -54,  -71,
             -61,  -22,  -26,  -29,  -22,  -18,  -28,  -25,
             -43,  -16,  -13,   11,   10,  -18,   -5,   -3,
             -10,   14,   27,   40,   40,   35,   20,    2,
              -1,    9,   20,   37,   42,   38,   42,   10,
              12,   17,   18,   23,   19,   13,   23,   12,
              -1,   13,   17,   23,   33,  -11,   -4,    2,
             -11,   21,   32,   19,   12,   17,   28,  -76,

            /* knights: KQ */
             -52,  -31,  -44,  -13,  -30,  -39,  -21,  -98,
             -26,  -20,  -16,  -29,  -23,  -20,   -2,  -26,
             -28,  -24,   -9,   -6,   -1,  -31,  -19,   -8,
              -8,   -2,   15,    8,   18,    8,    6,  -10,
             -16,  -11,    6,    9,    3,    5,   -5,  -20,
             -22,  -26,  -29,  -17,   -6,    4,   14,    3,
             -30,  -31,  -32,    1,    1,   10,   20,    1,
             -74,  -27,  -28,   -4,  -17,   38,   22,  -38,

            /* knights: QK */
             -90,  -35,  -36,   -3,  -40,  -55,  -54,  -56,
             -44,    4,  -18,  -13,  -16,  -13,  -43,  -60,
               0,  -10,  -15,   10,    5,   -3,  -18,  -39,
              -5,   11,   22,   29,   12,   16,    3,  -20,
              33,   15,    9,   13,   19,    9,    3,  -20,
              31,   26,   25,   -4,    0,    4,   -9,   -5,
              -1,   24,   11,   33,   13,  -19,  -33,  -32,
             -15,   41,   13,   16,  -13,  -21,    1,  -84,

            /* knights: QQ */
             -50,  -35,  -29,  -20,  -35,  -22,  -53,  -88,
             -17,   -2,  -34,  -26,  -35,  -54,  -50,  -61,
             -16,  -11,  -23,   -8,   -9,  -16,  -27,  -48,
              16,    5,   14,    6,    2,   -5,   -5,  -19,
              -6,    7,   -1,    6,    3,   -9,  -21,   -5,
               6,   13,  -11,  -17,  -10,    1,  -12,   -4,
               6,    3,  -12,   12,    3,    7,   10,    2,
             -33,   51,  -21,   11,  -15,    5,    2,  -87,

            /* bishops: KK */
             -17,  -16,  -28,   -1,   -5,    5,  -17,  -43,
              -9,  -28,  -12,    4,   -5,  -13,  -26,  -17,
               0,   -2,   10,   14,   22,    2,    3,    3,
              -4,   11,   16,    8,   -1,   15,    7,  -18,
              15,   17,    6,    4,   15,    9,   24,   29,
               0,    8,    7,    1,   15,   20,   16,   35,
              18,   19,   13,   12,   13,   10,   10,   -3,
              39,   31,   35,   30,   22,    4,   -6,   -1,

            /* bishops: KQ */
              32,   16,   -4,   -5,  -16,  -17,  -48,  -51,
             -14,    0,   -3,  -11,  -10,  -36,  -23,  -28,
              -3,   -9,   -1,    0,    1,  -10,   -6,  -23,
              -1,   12,   -4,  -26,  -12,   13,    9,   -3,
              12,    4,  -30,  -30,   -9,    8,   20,   12,
              -6,  -20,  -12,   -3,   -6,    4,   16,   23,
             -38,   -7,   -8,    5,  -11,    2,   23,   29,
              -4,    1,    1,   -4,    6,   14,   10,   24,

            /* bishops: QK */
             -33,  -31,   -5,  -11,   -5,   -7,   19,    7,
             -37,  -19,   -4,  -12,   -2,   -1,   -1,  -14,
              -5,    1,    0,    7,   12,    1,  -11,   -1,
              -6,    3,   12,   -7,   -7,    3,    7,  -22,
              25,   25,   -4,   -2,   -3,   -8,    1,   12,
              17,   39,   17,    8,   13,   -3,   -9,    2,
              28,   23,   12,   17,   -3,   19,   -2,   -7,
              30,   19,   20,   12,    7,    6,    3,  -17,

            /* bishops: QQ */
              18,   -6,  -44,  -21,  -39,  -35,  -42,  -50,
             -14,  -28,  -22,  -31,  -23,  -46,  -40,  -53,
             -12,  -10,  -29,  -32,  -30,  -33,  -44,  -22,
               2,  -13,  -25,  -41,  -33,  -33,  -39,  -34,
             -15,   -1,  -42,  -45,  -41,  -26,  -27,   -4,
             -14,  -19,  -26,  -34,  -12,  -38,  -23,  -21,
             -17,  -12,  -32,  -12,  -17,  -11,  -16,  -21,
             -21,   -9,  -15,  -28,    4,  -10,   -3,    2,

            /* rooks: KK */
              23,    9,   15,   -6,  -23,   -3,    5,   -4,
               8,   10,    3,  -12,  -13,  -16,  -16,   -5,
              28,   25,   17,   -2,   -9,    1,   -7,   -3,
              35,   31,   25,   16,   11,   27,   21,   21,
              45,   45,   35,   24,   22,   29,   22,   17,
              50,   42,   41,   26,   20,   29,   34,   34,
              44,   49,   39,   13,   28,   31,   40,   23,
              37,   47,   37,   18,   37,   48,   39,   39,

            /* rooks: KQ */
             -15,  -24,  -27,  -26,  -22,  -13,   -2,   12,
             -11,  -27,  -19,  -31,  -23,  -12,  -19,   -1,
             -11,  -22,   -1,  -27,  -15,  -12,   -1,   -8,
               5,   -7,   -1,   -4,   -5,   18,    8,    0,
              -2,   -2,   13,    9,    1,   10,   15,    5,
              11,    0,    5,   -6,   -9,    9,   14,   14,
               7,   11,   -5,   -5,   -5,   -7,   18,    5,
              20,    8,   -3,   -5,    2,   12,   22,    8,

            /* rooks: QK */
               2,  -11,   -5,  -21,  -29,  -29,  -31,  -10,
             -16,   -5,   -3,  -19,  -13,  -26,  -32,  -41,
               2,   10,    2,  -16,  -16,  -12,  -26,  -37,
               0,   -4,    6,    2,   -1,   -3,   -8,   -9,
              14,   31,   18,   13,    5,   12,    2,   -1,
              21,   19,   19,   17,   -3,   -3,    7,   -1,
               7,   15,   10,   10,   17,   -6,    4,    6,
             -28,   15,   20,    5,   10,   -1,    4,    5,

            /* rooks: QQ */
             -29,  -34,  -45,  -52,  -51,  -27,  -17,   -2,
             -38,  -17,  -24,  -52,  -50,  -44,  -55,  -32,
              -9,  -15,  -18,  -29,  -34,  -31,  -16,  -13,
             -13,   -8,   -9,  -23,  -27,  -17,  -16,   -7,
              -4,  -21,   -4,  -17,  -22,   -9,  -13,   -2,
              -2,  -13,   -5,   -7,  -16,    1,   -2,    1,
              -4,    0,   10,  -11,  -36,  -24,   -6,   -2,
               6,    3,   -3,   -8,  -20,   -9,    5,    4,

            /* queens: KK */
             -12,  -25,  -39,  -36,  -79,  -86,  -61,  -25,
               2,  -10,  -16,  -32,  -37,  -65,  -86,  -31,
              14,    0,    9,    7,    0,    9,    8,  -19,
              13,   41,   28,   34,   23,   43,   30,   42,
              32,   41,   44,   52,   65,   67,   85,   93,
              39,   71,   54,   63,   75,   81,   95,  117,
              58,   85,   73,   64,   97,   72,  133,   80,
              51,   47,   54,   60,   61,   68,   72,   60,

            /* queens: KQ */
              -9,  -40,  -59,  -37,  -47,  -47,  -68,   -6,
              -1,  -21,  -55,  -27,  -29,  -38,  -30,  -63,
               2,   -4,  -47,  -11,  -14,   21,   19,  -12,
              20,  -10,    9,   -6,   -1,   35,   28,   32,
              26,   28,   31,   12,   27,   33,   52,   42,
              25,   22,   33,   29,   39,   35,   39,   48,
              23,   36,   48,   49,   29,   60,   52,   27,
              29,   38,   40,   37,   34,   35,   10,   11,

            /* queens: QK */
             -45,  -56,  -86,  -57,  -74,  -49,  -41,  -47,
              -8,  -60,  -79,  -38,  -31,  -80,  -35,  -18,
             -25,  -10,   12,   15,   -9,  -14,  -16,  -16,
              -5,   22,   36,   10,   17,   10,  -15,   16,
              17,   32,   47,   33,   32,   23,   30,    5,
              -1,   48,   66,   51,   20,   21,   22,   10,
              15,   64,   62,   62,   34,   17,   41,   23,
              30,   26,   45,   30,   23,   47,   36,   22,

            /* queens: QQ */
             -58,  -56,  -28,  -54,  -85,  -56,  -64,  -45,
             -28,  -33,  -27,  -58,  -26,  -49,  -67,  -50,
               3,    0,  -22,    1,  -29,  -17,  -20,  -28,
              -9,    7,   11,   12,  -27,  -18,   -4,   -2,
              10,   44,   19,   23,    5,  -12,   16,    9,
              43,   61,   28,   42,   30,    6,   17,   27,
              39,   57,   51,   53,   39,   39,   24,  -10,
             -16,   41,   41,    6,    5,  -32,   -8,  -28,

            /* kings: KK */
              -1,  -14,  -21,  -21,  -21,  -10,  -26,  -45,
              -3,   -2,   -8,  -10,   -5,   -6,  -13,  -20,
              -6,    6,    4,    7,    8,    2,   -5,  -21,
               0,   18,   22,   20,   17,   13,    8,   -3,
              19,   34,   34,   20,   16,    8,   14,    1,
              29,   55,   45,   39,   19,   11,   35,   13,
              28,   61,   51,   47,   21,   15,   40,   21,
             -27,   37,   51,   46,   27,   28,   47,  -47,

            /* kings: KQ */
              -1,  -14,  -21,  -21,   -5,   -8,  -28,  -33,
              -3,   -2,   -8,  -10,    6,   -7,   -1,  -20,
              -6,    6,    4,    7,   20,    7,    1,  -10,
               0,   18,   22,   20,   31,   31,   38,   11,
              19,   34,   34,   20,   41,   48,   48,   33,
              29,   55,   45,   39,   44,   53,   60,   42,
              28,   61,   51,   47,   26,   28,   42,   28,
             -27,   37,   51,   46,    8,   18,   13,  -33,

            /* kings: QK */
             -34,  -45,  -37,  -22,  -43,  -31,  -30,  -51,
             -41,  -12,  -20,  -17,  -13,  -13,  -15,  -24,
             -28,    1,   -5,    2,    5,   -2,  -11,  -26,
              -5,   17,   20,   16,   18,   11,    9,   -9,
              17,   38,   30,   22,   19,   28,   27,   13,
              31,   44,   43,   36,   29,   46,   62,   27,
               5,   31,   40,   41,   48,   60,   74,   36,
               8,   31,   20,   27,   49,   59,   57,  -35,

            /* kings: QQ */
              53,    6,  -12,  -22,  -43,  -31,  -30,  -51,
               5,   -2,   -9,  -17,  -13,  -13,  -15,  -24,
              -4,    3,    2,    2,    5,   -2,  -11,  -26,
              -3,   15,   17,   20,   18,   11,    9,   -9,
              14,   24,   24,   20,   19,   28,   27,   13,
              22,   36,   21,   27,   29,   46,   62,   27,
               4,   48,   28,   27,   48,   60,   74,   36,
             -53,   47,   28,   20,   49,   59,   57,  -35,

            #endregion

            /* end game mobility weights */

            2, // knights
            5, // bishops
            2, // rooks
            1, // queens

            /* end game squares attacked near enemy king */
            -1, // attacks to squares 1 from king
            -2, // attacks to squares 2 from king
            -1, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            15, // # friendly pawns 1 from king
            20, // # friendly pawns 2 from king
            13, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -7,

            /* end game backward pawns */
            -11,

            /* end game doubled pawns */
            -18,

            /* end game adjacent/connected pawns */
            9,

            /* UNUSED (was - end game king adjacent open file) */
            0,

            /* end game knight on outpost */
            27,

            /* end game bishop on outpost */
            2,

            /* end game bishop pair */
            84,

            /* end game rook on open file */
            2,

            /* end game rook on half-open file */
            16,

            /* end game rook behind passed pawn */
            20,

            /* end game doubled rooks on file */
            6,

            /* end game king on open file */
            -7,

            /* end game king on half-open file */
            26,

            /* end game castling rights available */
            -6,

            /* end game castling complete */
            -8,

            /* end game center control */
            5, // D0
            2, // D1

            /* end game queen on open file */
            27,

            /* end game queen on half-open file */
            16,

            /* end game rook on seventh rank */
            18,

            /* end game passed pawn */
            0, 14, 15, 40, 54, 56, 45, 0,
        };
    }
}