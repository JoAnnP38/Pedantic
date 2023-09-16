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
        public const int MAX_WEIGHTS = 3970;
        public const int ENDGAME_WEIGHTS = 1985;
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
        public const int KING_ON_OPEN_DIAGONAL = 1984;

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

        // Solution sample size: 12000000, generated on Sat, 16 Sep 2023 00:17:02 GMT
        // Solution error: 0.118126, accuracy: 0.5200, seed: 1824655200
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening piece values */
            92, 458, 505, 615, 1482, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -25,  -28,  -19,  -10,  -36,   51,   49,   -2,
             -28,  -26,  -30,  -45,  -24,   -6,   13,  -28,
              -1,   -7,    3,  -13,    1,   30,    1,    9,
              17,   -8,   15,   30,   41,   29,   35,   56,
              18,  -13,   18,   36,   76,  133,  131,   86,
             112,  124,   72,  106,  115,  100,   70,   78,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -40,  -62,  -41,  -57,  -24,   42,   46,   11,
             -31,  -52,  -40,  -51,  -19,  -10,   17,  -12,
             -14,   -9,   -8,  -15,   -8,    1,   -2,   29,
              -3,   13,   15,   24,   16,  -19,   11,   63,
              80,   70,  146,   63,   78,   11,   31,   42,
             101,   85,  106,   89,   95,   87,  190,  146,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
               7,   21,   16,  -23,  -40,   -7,  -20,  -42,
              15,   16,  -22,  -37,  -42,  -16,  -30,  -62,
              36,   19,    2,  -12,  -14,   11,  -10,  -15,
              48,   -3,   19,   40,   24,   11,   14,   19,
              46,    5,   35,   33,   86,  108,   82,   23,
             140,  165,  104,  168,  111,   78,   68,   52,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -30,   -2,   22,   -7,  -11,    7,  -10,   -9,
             -24,  -12,  -23,  -48,  -22,  -22,  -29,  -50,
              -1,  -13,    3,   -5,    2,   15,   -1,  -19,
              77,   55,   46,   49,   19,   -3,   12,   37,
             136,   74,  134,  103,   32,   30,  -18,   34,
             120,   22,  104,   54,  108,   46,  166,   69,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
            -128,  -31,  -57,  -41,  -18,  -15,  -27,  -66,
             -44,  -54,  -17,    1,    2,    8,  -14,  -11,
             -32,  -14,   -2,   13,   23,    4,    3,   -3,
             -14,    5,   13,   12,   17,   40,   41,    9,
               2,   10,   31,   37,    3,   32,    4,   56,
             -31,   15,   34,   44,   67,  158,   27,  -16,
             -15,  -27,    3,   65,   32,   97,   -7,  -17,
            -249,  -36,    5,   56,   44, -106,   19, -174,

            /* knights: KQ */
            -116,  -53,  -56,  -56,  -46,  -36,  -34,  -47,
             -82,  -44,  -17,    4,  -15,    3,  -84,  -44,
             -50,   -9,  -11,   21,   14,   19,  -23,  -22,
             -11,   43,   26,   46,   33,   18,   16,  -19,
              44,    7,   44,   49,   62,   49,   30,   -1,
              29,   38,   87,   77,   41,   41,  -13,  -29,
              13,  -20,    9,   53,   -7,   21,  -29,  -34,
            -253,  -15,  -15,   46,  -68,   -2,  -43, -117,

            /* knights: QK */
             -97,  -72,  -59,  -69,  -44,  -23,  -58,  -94,
             -58,  -33,   -7,   -8,  -15,   -6,  -48,  -25,
             -19,  -14,   18,   36,   50,  -19,   -8,  -64,
              17,   28,   44,   26,   32,   28,   28,   -8,
              18,   48,   55,  101,   66,   60,   28,   53,
               5,    0,   18,   60,   85,  119,   33,   11,
             -24,   -4,   20,   34,    9,   87,   -1,   43,
            -136,  -50,    6,   11,   -3,  -54,  -67,  -42,

            /* knights: QQ */
             -24,  -38,   10,  -67,  -36,  -57,  -56,  -81,
             -48,   -8,  -19,   12,  -31,   -4,  -17, -102,
              21,   -6,   13,   10,   13,  -20,  -25,  -18,
              20,   19,   67,   61,   32,   27,   35,  -45,
              37,    7,   50,   54,   49,   49,   48,   -1,
              55,  -11,   43,   96,   33,   37,   32,  -20,
             -58,   29,  -10,   12,   36,   44,   -7,  -38,
            -148,  -12,  -13,   10,    3,   41,    4,  -87,

            /* bishops: KK */
               0,    9,  -14,  -29,   -9,   -8,   -2,  -12,
              10,    7,   12,   -2,    2,   14,   36,   13,
             -14,   10,   19,   11,    8,   13,   10,   -5,
             -18,  -13,    8,   34,   26,    5,   -1,   24,
             -11,   12,    4,   38,   17,   37,   10,   -2,
              -5,    7,  -13,  -14,    4,   73,   49,  -19,
             -57,  -28,  -22,  -22,  -37,    5,  -72,  -65,
             -73,  -40,   -2,  -23,  -11,  -62,   10,  -61,

            /* bishops: KQ */
             -67,    5,  -38,  -46,    7,  -12,   86,  -16,
             -25,  -19,  -17,   -3,   -4,   27,   52,    4,
             -42,  -18,   17,   16,   16,   36,   39,   21,
              12,   -3,   -8,   54,   58,   34,   -1,  -19,
              -4,   30,   14,   87,   58,    9,   18,    2,
              -7,   72,   85,  -13,   15,    7,   27,    5,
             -54,  -11,  -46,  -16,   21,  -19,  -10,  -49,
             -38,  -20,   18,   31,  -15,    0,  -10,  -90,

            /* bishops: QK */
              -5,   13,   10,  -19,  -15,  -37,  -35, -107,
              35,   44,   22,   20,  -10,  -29,  -38,  -27,
              -3,   37,   34,   28,    0,   -2,   -4,  -16,
              -5,   23,   24,   68,   48,   -7,  -20,  -24,
             -25,   20,   39,   73,   34,   39,   27,  -11,
             -35,  -19,   39,   27,   50,   83,   59,    6,
             -36,  -22,  -17,  -11,  -37,    0,  -13,  -36,
             -49,  -26,   -4,  -22,   27,  -50,   40,  -30,

            /* bishops: QQ */
             -78,  -39,   -3,  -48,  -20,  -30,   19,  -34,
              24,   37,  -41,   20,  -42,   12,  -22,   57,
              -4,   27,   28,    3,   17,   15,   30,   -7,
              25,   42,   20,   51,    0,   57,   11,   15,
              12,   33,    4,   75,   51,  -10,   -1,    6,
             -37,   38,   67,   33,    0,   25,  -21,   -6,
             -21,  -27,   38,  -12,   17,   -8,   20,   11,
             -19,   10,   23,    1,    2,  -15,   14,  -12,

            /* rooks: KK */
             -17,   -7,   -7,    6,   16,   11,   20,  -10,
             -33,  -15,  -17,   -2,   -5,   19,   41,  -12,
             -36,  -35,  -25,  -13,    8,    4,   31,    4,
             -29,  -16,  -10,   -2,  -12,  -27,   32,  -14,
             -13,    6,   12,   29,   11,   50,   73,   27,
               0,   40,   38,   44,   62,  122,  129,   63,
             -17,  -13,    8,   33,   17,   73,  113,  118,
              65,   55,   33,   54,   36,   65,   79,   85,

            /* rooks: KQ */
             -27,   15,   -1,  -11,  -10,   -9,  -19,  -25,
             -42,  -10,  -24,  -16,  -22,  -30,  -31,  -35,
             -33,  -14,  -40,   -9,  -38,  -15,  -11,  -28,
             -49,  -11,  -52,   -8,  -23,  -49,    0,    9,
               1,   51,    8,   -6,   12,   26,   42,   14,
              65,   72,   44,   63,   63,   25,   88,   30,
              -5,   27,   22,   48,   51,   -9,   38,   84,
              14,   19,   33,    3,   12,   41,   48,   91,

            /* rooks: QK */
             -63,  -11,  -15,   -9,   -2,  -11,   19,  -23,
             -31,  -38,  -42,  -24,  -47,  -43,  -15,  -14,
             -13,    9,  -28,  -32,  -57,  -63,  -13,  -18,
              21,   29,    1,  -18,  -28,  -29,   12,    0,
              45,    1,   46,    2,   -5,    1,   17,   19,
              29,   74,   30,   -5,   41,   65,  102,   49,
              19,   35,   51,   14,    2,   50,   28,   30,
             103,   53,   65,   26,   25,   49,   74,   71,

            /* rooks: QQ */
             -26,  -18,   18,   27,    8,   -5,  -11,  -30,
             -21,  -26,  -32,    5,  -23,  -34,  -14,   -8,
             -61,   26,   -9,    0,  -40,  -28,  -30,  -31,
             -27,   56,  -17,  -11,  -34,    1,  -20,    1,
              12,   49,    9,   66,   25,   38,   -2,   29,
              49,   72,   98,   41,   54,   61,   31,   15,
              48,   36,   25,   66,  116,    2,   36,   32,
              71,   30,   17,  -26,  -14,   34,  106,   34,

            /* queens: KK */
              -2,    5,    5,   12,   35,    5,  -65,  -19,
             -19,    7,    5,   13,   12,   37,   24,  -15,
             -21,   -7,    3,  -13,    9,   -8,    7,    4,
              -7,  -16,  -25,  -24,  -23,  -14,   13,  -22,
             -13,   -3,  -37,  -32,  -30,  -17,  -17,  -10,
             -20,   -6,  -36,  -46,  -16,   72,   35,   -3,
             -38,  -45,  -42,  -29,  -40,   52,   33,   91,
             -43,   27,    9,    6,   23,   67,  119,   75,

            /* queens: KQ */
              22,   14,   15,   11,   10,   -6,  -29,  -27,
              27,   21,   33,   18,    9,   -5,  -48,  -58,
               9,    4,   31,    9,    9,   14,   -7,  -12,
              -6,   20,  -25,  -20,   -3,  -22,   -2,  -11,
              13,   30,  -10,  -22,    6,    4,    9,   -3,
              40,   79,   47,    6,    7,   30,   29,   19,
              39,   57,   67,   61,   30,    1,   -3,   28,
              67,  114,   62,   73,    2,   59,   40,   -2,

            /* queens: QK */
             -49,  -76,  -22,  -14,    9,  -22,  -18,  -19,
             -66,  -43,   27,   17,   13,   -8,   21,    3,
             -46,   -4,    2,  -14,    2,  -28,  -10,   -4,
             -17,  -37,  -23,   -3,  -22,   -5,    3,   -1,
              -8,  -13,  -15,  -31,  -23,   11,   -5,   14,
              -8,    3,   -7,  -28,   12,   53,   32,   31,
              -5,    7,   -5,  -17,    5,   13,   41,   43,
              -1,   17,    8,   18,   -2,   78,   98,   62,

            /* queens: QQ */
             -40,  -56,  -11,  -21,   25,   -8,  -44,  -47,
             -53,  -43,   14,   28,  -28,  -14,    7,  -28,
               0,    1,   18,   -9,   -3,  -11,   -2,   -1,
             -33,   32,  -15,   11,   -1,    0,    1,  -18,
              -6,   12,   50,   34,   63,   18,   11,    7,
              40,   36,   98,   54,   17,  -14,   26,   38,
              69,   59,   68,   58,   26,   -5,   -1,  -23,
               9,   79,   10,   56,   43,   26,   34,    9,

            /* kings: KK */
               0,    0,    0,    0,  -36,  -70,    9,    1,
               0,    0,    0,    0,   -1,    8,   29,    7,
               0,    0,    0,    0,   31,    6,   -1,  -44,
               0,    0,    0,    0,  117,   94,   26,  -62,
               0,    0,    0,    0,  146,  216,  135,   35,
               0,    0,    0,    0,  146,  272,  208,  120,
               0,    0,    0,    0,  218,  134,  113,  104,
               0,    0,    0,    0,   91,   70,   68,  -30,

            /* kings: KQ */
               0,    0,    0,    0,  -75,  -92,    7,   -9,
               0,    0,    0,    0,    2,    0,   -3,   -5,
               0,    0,    0,    0,  -11,   27,   -2,  -53,
               0,    0,    0,    0,   65,   55,   16,   -9,
               0,    0,    0,    0,  148,   93,   78,   19,
               0,    0,    0,    0,   84,   44,  109,    9,
               0,    0,    0,    0,  165,  216,   79,   29,
               0,    0,    0,    0,   87,   76,  101,   38,

            /* kings: QK */
             -15,   15,  -29, -110,    0,    0,    0,    0,
              19,    2,  -21,  -32,    0,    0,    0,    0,
             -37,   -8,   -1,  -38,    0,    0,    0,    0,
             -15,   43,   34,   45,    0,    0,    0,    0,
              12,   56,   51,   98,    0,    0,    0,    0,
              37,   87,  117,  121,    0,    0,    0,    0,
              53,  144,  135,  110,    0,    0,    0,    0,
             -18,   76,  130,   61,    0,    0,    0,    0,

            /* kings: QQ */
             -75,   -9,  -54,  -93,    0,    0,    0,    0,
             -21,  -15,    4,  -36,    0,    0,    0,    0,
             -41,    4,   -1,    3,    0,    0,    0,    0,
             -33,   28,   63,   78,    0,    0,    0,    0,
              75,  101,  168,  130,    0,    0,    0,    0,
              67,  166,  139,   83,    0,    0,    0,    0,
             -17,   92,   88,  120,    0,    0,    0,    0,
             -49,   78,   72,   51,    0,    0,    0,    0,

            #endregion

            /* opening mobility weights */

               9, // knights
               5, // bishops
               2, // rooks
               0, // queens

            /* opening squares attacked near enemy king */
              24, // attacks to squares 1 from king
              21, // attacks to squares 2 from king
               7, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
              25, // # friendly pawns 1 from king
              11, // # friendly pawns 2 from king
               7, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -8,

            /* opening backward pawns */
            9,

            /* opening doubled pawns */
            -14,

            /* opening adjacent/connected pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              12,    0,    3,   18,   25,    1,  -19,   -1,
               7,   20,   11,   23,   48,    1,   13,    8,
             -10,   17,    2,   24,   40,   36,   20,   20,
              28,   22,   56,   23,  104,   86,   47,   10,
              83,  122,  186,  183,  173,  208,  189,   97,
             152,  196,  197,  318,  261,  246,   86,  100,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening knight on outpost */
            2,

            /* opening bishop on outpost */
            8,

            /* opening bishop pair */
            43,

            /* opening rook on open file */
            35,

            /* opening rook on half-open file */
            11,

            /* opening rook behind passed pawn */
            -1,

            /* opening doubled rooks on file */
            9,

            /* opening king on open file */
            -74,

            /* opening king on half-open file */
            -33,

            /* opening castling rights available */
            28,

            /* opening castling complete */
            21,

            /* opening center control */
               1, // D0
               4, // D1

            /* opening queen on open file */
            -10,

            /* opening queen on half-open file */
            9,

            /* opening rook on seventh rank */
            17,

            /* opening passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
             -18,  -22,  -29,  -44,    7,  -23,   -8,   23,
             -10,   -6,  -16,  -10,  -35,  -40,  -52,    4,
             -38,  -32,  -36,   -6,  -29,  -45,  -69,  -34,
              -8,   32,    4,   -7,   -3,   35,   37,   -9,
              73,   98,   67,   41,   23,  109,   65,   52,
             149,  147,  161,  166,  143,  159,  157,  172,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -6,   -2,  -10,   -1,  -16,  -21,  -17,    0,
              -9,   -9,  -18,  -13,  -15,  -17,  -16,   -7,
              -9,    3,    1,   -1,  -15,   -7,   -1,   -4,
               4,    8,    6,    4,   -4,   -1,    1,    1,
              20,    8,   15,   11,   32,   -2,   48,   -1,
              46,   62,   52,   47,   70,   94,   32,   90,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening block passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,  // blocked by pawns
               0,   41,   56,   55,   49,   52,   89,    0,  // blocked by knights
               0,   22,   32,   18,    4,   49,   68,    0,  // blocked by bishops
               0,  -41,  -22,  -28,  -28,   14,  150,    0,  // blocked by rooks
               0,    8,   40,  -11,  -13,   -9, -103,    0,  // blocked by queens
               0,   17,   72,   27,   70,  175,  199,    0,  // blocked by kings

            /* opening supported pawn chain */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,
              28,   32,   49,   56,   36,   44,   31,   59,
              -6,   16,   22,   29,   33,   16,   28,   11,
             -18,   29,   60,   50,   63,   66,   30,   37,
              72,  143,  123,  159,  166,   93,   97,   99,
             129,  235,  235,  188,  342,  226,  198,  -11,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening king outside passed pawn square */
            150,

            /* opening passed pawn/friendly king distance penalty */
            1,

            /* opening passed pawn/enemy king distance bonus */
            2,

            /* opening pawn rams */
               0,    0,    0,    0,    0,    0,    0,    0,
              20,    2,   16,   -3,   69,    7,  -14,    3,
              10,    3,   34,    9,   10,  -11,    5,   37,
               0,    0,    0,    0,    0,    0,    0,    0,
             -11,   -8,  -13,  -38,  -18,    1,  -15,  -10,
              -4,    9,  -11,   -5,  -41,   27,    0,  -14,
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening piece threats */
            /* P     N     B     R     Q     K */
               0,   65,   50,   63,   65,    0,  // Pawn threats
               0,  -13,   48,   83,   26,    0,  // Knight threats
               0,   30,   -5,   52,   41,    0,  // Bishop threats
               0,   21,   27,   -5,   55,    0,  // Rook threats
               0,   -3,   -9,  -16,  -25,    0,  // Queen threats
               0,    0,    0,    0,    0,    0,  // King threats

            /* opening pawn push threats */
               0,   31,   35,   37,   39,    0,  // Pawn push threats

            /* opening king on open diagonal */
            -7,


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game piece values */
            147, 484, 542, 930, 1595, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              -5,    1,   -4,  -18,   27,    9,   -4,  -29,
             -12,  -15,  -28,  -18,  -21,  -10,   -8,  -18,
              -5,    0,  -26,  -44,  -22,  -21,    5,  -18,
              45,   25,   16,    2,  -16,  -10,   16,    7,
              42,   80,   60,   64,   62,   -6,   23,    6,
              86,   74,   91,   46,   41,   28,   62,   33,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              10,   30,   13,   54,  -25,  -12,  -14,  -15,
             -13,    5,  -23,  -28,  -40,  -17,  -19,  -16,
              15,    7,   -3,  -26,  -19,   -7,   -4,  -23,
              65,   10,   12,   22,    3,   37,   38,   23,
              24,   21,  -24,   45,   97,   99,  115,   84,
              52,   40,   -3,   64,  107,  170,  134,  135,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              -3,    4,   -9,   24,    9,   17,   17,   22,
              -4,   -5,  -17,  -22,  -11,  -16,   -8,    4,
               3,   11,    0,  -23,   -7,  -14,   -6,   -5,
              56,   59,   45,   22,    4,  -12,    8,   26,
              69,  126,  119,  101,   61,  -11,   28,   37,
              87,  114,  153,   88,   41,   30,   26,   48,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
               2,    9,   -4,    4,  -14,  -16,  -13,  -15,
              -2,    4,  -15,  -23,  -31,  -37,  -25,  -17,
              12,   14,   -6,  -24,  -24,  -38,  -21,  -18,
              27,   10,    8,   15,   -8,  -11,    4,    2,
             -13,   20,  -18,   32,   68,   27,   53,   40,
              28,   63,  -13,   55,   30,   94,   68,  104,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -51,  -91,  -35,  -11,  -22,  -20,  -41,  -82,
             -75,   -3,  -26,  -21,  -10,    6,   -1,  -23,
             -45,  -13,    1,   23,   22,   -2,    7,    4,
               7,   15,   44,   54,   62,   49,   41,   12,
              18,   11,   24,   49,   61,   70,   80,   27,
              25,   18,   22,   24,   19,   -3,   48,   36,
              10,   33,   27,   13,   38,  -11,    5,    7,
              -8,   31,   21,   23,   26,   17,   20, -100,

            /* knights: KQ */
             -82,  -47,    3,   10,   -5,  -16,  -13, -130,
             -29,    5,  -22,  -18,   -5,    7,   38,  -28,
              -6,  -31,    6,   -1,   -6,  -33,   17,    0,
              -2,  -15,   14,    6,   19,   23,   18,   16,
             -37,  -16,    7,    7,    0,    9,    4,    5,
             -30,  -41,  -42,  -25,   -1,   11,   30,   11,
             -47,  -30,  -46,  -10,    6,   13,   18,   -7,
            -140,  -43,  -38,  -16,  -15,   23,   33,  -49,

            /* knights: QK */
             -97,   -5,   22,   26,   -2,  -28,  -37,  -80,
             -62,    2,   -3,    3,   -4,   -3,  -11,  -53,
              -7,  -16,  -17,   13,   -6,    7,  -12,  -24,
               4,   21,   15,   46,   19,   20,    6,   -9,
              19,   13,   23,    0,   18,   18,   14,  -15,
              22,   33,   33,   -4,   -4,  -22,   -6,  -31,
              26,   13,   24,   51,   21,  -13,  -33,  -26,
             -65,   10,   19,   -6,   13,  -22,   -1, -134,

            /* knights: QQ */
             -72,  -10,   -7,   14,   -3,    3,  -69,  -61,
             -18,   24,   -6,  -12,   -1,  -39,   -9,  -61,
             -20,  -15,  -14,   11,    8,   -5,    7,  -56,
              47,    9,   18,   -5,    7,    0,   16,   15,
              16,   25,    4,    8,   15,   -3,  -11,   15,
              -1,    8,   -7,   -3,   -6,   18,    1,   -4,
              50,    9,    6,   42,   23,   11,   20,   25,
             -59,   77,   17,   38,   -6,   40,   43,  -20,

            /* bishops: KK */
             -41,  -42,  -27,   12,    9,    8,  -25,  -63,
             -33,  -20,  -17,    6,    1,    1,  -26,  -70,
              -3,    5,   27,   17,   43,   12,  -10,   -9,
              18,   28,   38,   18,    7,   15,    2,  -42,
              31,   -3,   12,   14,   22,   19,   17,   23,
              11,   23,   22,   24,   31,   29,   18,   55,
              39,   28,   32,   33,   36,   16,   14,   10,
              44,   37,   35,   47,   34,   23,  -13,   16,

            /* bishops: KQ */
              -2,   -2,    8,    9,  -10,   -5,  -85,  -83,
             -23,   -4,   13,    3,   10,  -22,   -7,  -55,
               7,   20,   18,    5,   15,   -6,   -4,  -24,
              12,   16,   11,   -8,   -2,   24,   20,   19,
              25,   -4,   -1,  -47,   12,   33,   14,   17,
             -11,  -18,   -9,   26,   12,   19,   20,   31,
             -35,   -4,   18,   13,    1,   23,   36,   55,
              11,   -2,  -16,    2,   15,   30,   32,   47,

            /* bishops: QK */
             -46,  -33,   -3,   16,   10,   -3,   21,  -23,
             -50,  -13,   10,   15,    9,   11,   -5,  -20,
              -5,    4,   17,   21,   41,    1,  -12,  -19,
              -4,   20,   38,    3,  -10,   30,   22,   11,
              49,   15,   16,   -3,    5,   14,  -10,   25,
              40,   47,   13,   27,   29,   14,  -20,   13,
              40,   27,   33,   37,   30,   20,  -15,  -39,
              24,   24,   32,   42,   -2,   -1,   -3,   15,

            /* bishops: QQ */
              -9,   -7,  -10,   -8,   -6,  -19,  -37,  -67,
             -58,  -24,    7,  -15,   17,  -27,  -21,  -83,
             -13,  -10,   -3,   -5,   -2,  -13,  -38,  -16,
               2,   -9,  -13,  -23,  -10,  -16,  -12,  -19,
               1,  -11,  -14,  -28,  -33,   -1,  -18,   -4,
               9,    9,  -14,   -4,   14,  -10,    5,   -3,
             -21,    1,   -8,   13,   -7,    2,    1,   -1,
             -13,   -1,  -17,   22,   25,    6,   23,   42,

            /* rooks: KK */
              35,   26,   11,  -11,  -27,   -3,    0,  -19,
              28,   22,   12,  -13,  -23,  -39,  -14,   -3,
              27,   36,    7,   -8,  -35,  -21,  -22,  -11,
              58,   44,   25,   10,   -4,   30,   15,   16,
              68,   55,   32,    7,   19,    6,   20,   26,
              68,   43,   31,    7,   -2,    5,    7,   22,
              62,   67,   27,    6,   11,   10,   30,   20,
              49,   61,   41,    7,   39,   50,   69,   43,

            /* rooks: KQ */
              -8,  -31,  -41,  -24,  -29,  -18,   19,    9,
              -8,  -33,  -42,  -45,  -25,  -11,   13,   -2,
             -11,  -25,  -27,  -45,  -24,  -22,   -3,  -16,
               0,  -13,   -5,  -14,  -17,   22,    9,   -1,
               4,  -16,  -14,    1,  -15,    0,   11,   16,
              -6,  -12,  -33,  -37,  -37,   -3,   -6,   19,
              13,    4,  -28,  -26,  -18,    0,   16,   -5,
              41,   27,  -13,    3,    2,   10,   39,   10,

            /* rooks: QK */
              25,    8,    0,  -27,  -42,  -36,  -47,  -10,
              -2,    2,    6,  -27,  -27,  -34,  -28,  -38,
             -11,   -2,   -5,  -21,  -14,  -30,  -44,  -44,
             -13,   -7,   -4,   -4,  -10,   -5,   -1,  -14,
              13,   45,    3,    5,   -3,   -4,   -1,  -24,
              24,   10,   14,    9,  -19,  -11,  -17,    1,
              20,   16,    0,    2,   -1,  -13,   11,    1,
             -61,   15,   -3,   11,    4,   -2,    9,    3,

            /* rooks: QQ */
             -16,  -10,  -53,  -74,  -55,  -20,  -11,   22,
             -30,   -8,  -26,  -60,  -59,  -48,  -37,  -13,
              -1,  -24,  -39,  -57,  -15,  -48,   -6,    3,
               8,  -25,   -8,  -26,  -34,  -23,    0,   12,
               3,   -8,  -11,  -31,  -23,  -11,   24,    7,
              -4,    1,  -21,  -19,  -30,  -12,    5,   21,
               0,   14,    6,  -32,  -56,  -17,    5,    9,
              19,   37,    9,   20,  -11,   -6,   18,   24,

            /* queens: KK */
              -8,  -33,  -45,  -36, -102, -111,  -69,  -25,
              38,   -7,  -14,  -21,  -29,  -80,  -70,  -24,
              -2,    1,   -3,   10,  -14,   19,   11,  -26,
              11,   22,   40,   23,   12,   37,   19,   78,
              24,   18,   41,   33,   55,   88,  112,  107,
              39,   60,   59,   58,   69,   80,  115,  152,
              78,   97,   83,   61,   85,   58,  167,  110,
              65,   38,   37,   50,   62,   81,   97,  117,

            /* queens: KQ */
              -8,  -24,  -66,  -19,  -16,  -56,  -43,    5,
              32,  -12,  -54,  -26,  -21,  -29,  -28,  -32,
               3,  -21,  -65,   -3,    4,   30,   23,  -50,
              40,  -20,   17,  -20,    8,   34,   22,   33,
               5,  -15,    1,   -2,   -2,   19,   27,   86,
              15,   13,   15,   -3,    9,   20,   71,   44,
              57,   41,   19,   19,   20,   62,   78,   47,
              56,   53,   57,   62,   86,   28,   38,   15,

            /* queens: QK */
             -85,  -53,  -92,  -42,  -68,  -34,  -29,   -2,
             -11,  -63,  -77,  -48,  -57,  -45,  -16,   -2,
             -40,  -33,  -19,    9,  -10,   17,   10,   -6,
             -35,   20,   31,  -12,   17,  -14,    8,  -14,
              23,   13,   33,   17,   16,   24,   28,   -1,
              18,   63,   22,   50,   22,   22,   31,   12,
              13,   24,   38,   27,    6,   15,   21,   48,
              21,   34,   38,   32,   25,   30,   31,   56,

            /* queens: QQ */
             -46,  -37,  -23,  -41,  -22,  -15,  -19,   14,
             -30,  -29,  -26,  -39,   20,  -25,  -34,  -20,
             -10,   13,  -14,    6,  -21,  -12,  -24,  -28,
              28,   11,   19,   -9,  -21,  -63,  -48,   16,
              35,   26,  -15,    5,  -10,  -11,   -5,    3,
              30,   61,   68,    2,   -2,    3,   17,   30,
              51,   73,   55,   54,   22,   13,   63,   11,
             -14,   71,   79,   24,   22,   -7,   14,   10,

            /* kings: KK */
               0,    0,    0,    0,  -26,   15,  -17,  -45,
               0,    0,    0,    0,   -7,    1,   -2,   -1,
               0,    0,    0,    0,    8,    8,    4,    4,
               0,    0,    0,    0,    6,   10,   20,   14,
               0,    0,    0,    0,    3,  -12,   12,   10,
               0,    0,    0,    0,   14,  -16,   27,   20,
               0,    0,    0,    0,   -9,   23,   73,   26,
               0,    0,    0,    0,   14,   52,   55, -119,

            /* kings: KQ */
               0,    0,    0,    0,    0,    8,  -35,  -36,
               0,    0,    0,    0,  -14,   -7,    9,   -3,
               0,    0,    0,    0,    9,   -6,    7,   10,
               0,    0,    0,    0,   13,   26,   37,   20,
               0,    0,    0,    0,   13,   39,   47,   48,
               0,    0,    0,    0,   38,   63,   53,   72,
               0,    0,    0,    0,   19,   13,   51,   43,
               0,    0,    0,    0,    7,   23,   -4,   -4,

            /* kings: QK */
             -63,  -64,  -48,  -20,    0,    0,    0,    0,
             -44,  -20,  -27,  -30,    0,    0,    0,    0,
             -12,  -11,  -18,   -6,    0,    0,    0,    0,
               8,   12,   13,    3,    0,    0,    0,    0,
              20,   44,   29,    5,    0,    0,    0,    0,
              46,   47,   30,   12,    0,    0,    0,    0,
              23,   35,   18,   20,    0,    0,    0,    0,
               1,   42,   33,   26,    0,    0,    0,    0,

            /* kings: QQ */
              33,  -11,   -2,  -17,    0,    0,    0,    0,
               5,    1,  -21,  -26,    0,    0,    0,    0,
              -1,  -10,  -11,  -14,    0,    0,    0,    0,
              -2,   13,    9,    2,    0,    0,    0,    0,
               6,   15,    3,   -3,    0,    0,    0,    0,
              36,   34,   -3,   24,    0,    0,    0,    0,
              14,   67,   33,   23,    0,    0,    0,    0,
            -119,   68,   31,   41,    0,    0,    0,    0,

            #endregion

            /* end game mobility weights */

               9, // knights
               5, // bishops
               4, // rooks
               5, // queens

            /* end game squares attacked near enemy king */
              -5, // attacks to squares 1 from king
              -3, // attacks to squares 2 from king
              -1, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
              19, // # friendly pawns 1 from king
              22, // # friendly pawns 2 from king
              17, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -8,

            /* end game backward pawns */
            -1,

            /* end game doubled pawns */
            -38,

            /* end game adjacent/connected pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               1,    0,   -2,   25,   22,  -27,   23,  -29,
              -3,    4,   23,   42,   -4,   21,    0,  -14,
              14,   15,   49,   72,   29,   29,   12,   11,
              70,   53,   85,   74,   78,   38,   50,   44,
              85,  134,  155,  188,  108,  178,   64,   48,
             253,  358,  329,  396,  408,  283,  242,  222,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game knight on outpost */
            26,

            /* end game bishop on outpost */
            24,

            /* end game bishop pair */
            96,

            /* end game rook on open file */
            2,

            /* end game rook on half-open file */
            34,

            /* end game rook behind passed pawn */
            48,

            /* end game doubled rooks on file */
            14,

            /* end game king on open file */
            14,

            /* end game king on half-open file */
            30,

            /* end game castling rights available */
            -27,

            /* end game castling complete */
            -20,

            /* end game center control */
               8, // D0
               6, // D1

            /* end game queen on open file */
            33,

            /* end game queen on half-open file */
            23,

            /* end game rook on seventh rank */
            31,

            /* end game passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              40,   37,   36,   38,    9,   18,   46,   17,
              43,   46,   40,   34,   42,   38,   66,   32,
             -10,    2,    4,    1,   -3,   11,   35,   14,
              20,   35,   28,   19,   43,   34,   37,   40,
              92,   86,   86,   58,   50,   76,   85,   77,
              94,  143,  154,  149,  166,  133,  150,  103,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               5,   -5,   -2,  -42,  -11,   -1,   -8,   -4,
              -7,  -13,  -12,  -15,  -16,   -2,   -7,   -6,
              -9,  -24,  -44,  -54,  -38,  -35,  -30,   -7,
             -26,  -39,  -41,  -53,  -37,  -34,  -36,  -22,
             -27,  -45,  -62,  -61,  -78,  -53,  -91,  -17,
             -40,  -76,  -78,  -99, -144, -110, -122, -128,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game block passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,  // blocked by pawns
               0,  -11,    5,   21,   63,   45,   58,    0,  // blocked by knights
               0,    9,   39,   57,  111,  121,  182,    0,  // blocked by bishops
               0,    9,  -15,  -20,    0,    3,  -25,    0,  // blocked by rooks
               0,  -19,   20,   50,   15, -110, -171,    0,  // blocked by queens
               0,    3,   13,  -61,  -21,   19,  134,    0,  // blocked by kings

            /* end game supported pawn chain */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,
              10,   27,   29,   37,   38,   15,    9,   -1,
               8,   30,   12,   35,   12,   15,   11,    0,
              18,   29,   39,   33,   35,   17,   27,    6,
              57,   36,   98,   72,   80,   82,   33,    5,
              86,   68,  160,   60,  122,  114,   51,  102,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game king outside passed pawn square */
            195,

            /* end game passed pawn/friendly king distance penalty */
            -18,

            /* end game passed pawn/enemy king distance bonus */
            41,

            /* end game pawn rams */
               0,    0,    0,    0,    0,    0,    0,    0,
             -10,   40,   -2,   20,   32,   23,    2,    9,
               2,   13,   13,   37,   27,    1,   -2,   -6,
               0,    0,    0,    0,    0,    0,    0,    0,
             -27,  -14,  -22,  -18,   -5,  -11,  -11,   -6,
             -17,  -17,  -35,  -66,  -38,    9,  -15,    9,
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game piece threats */
            /* P     N     B     R     Q     K */
               0,   75,   95,   72,   -8,    0,  // Pawn threats
               0,    4,   28,    3,   75,    0,  // Knight threats
               0,   62,   38,   33,  130,    0,  // Bishop threats
               0,   56,   54,   62,   87,    0,  // Rook threats
               0,   45,   79,   39,   34,    0,  // Queen threats
               0,    0,    0,    0,    0,    0,  // King threats

            /* end game pawn push threats */
               0,   28,  -21,   25,  -34,    0,  // Pawn push threats

            /* end game king on open diagonal */
            12,
        };
    }
}