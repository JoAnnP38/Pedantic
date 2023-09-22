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

        // Solution sample size: 12000000, generated on Fri, 22 Sep 2023 03:26:33 GMT
        // Solution error: 0.117983, accuracy: 0.5222, seed: -1299855912
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening piece values */
            116, 508, 556, 695, 1587, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -64,  -59,  -44,  -27,  -53,   52,   60,  -13,
             -67,  -56,  -58,  -70,  -44,  -16,    7,  -55,
             -33,  -36,  -11,  -33,   -8,   25,    3,   -4,
             -15,  -31,    0,   21,   36,   23,   43,   59,
              51,   -4,   -6,   69,  121,  194,  187,  179,
              92,  105,   81,  117,   95,  157,   70,   96,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
            -100,  -99,  -70,  -78,  -49,   47,   62,  -16,
             -90,  -91,  -73,  -81,  -44,  -22,   13,  -52,
             -65,  -41,  -27,  -36,  -18,    2,   -1,    7,
             -52,  -13,    3,   15,    4,  -26,   22,   58,
             102,   93,  137,  107,  117,   64,   66,  115,
             164,   89,  131,   95,  111,   65,  144,  148,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -38,    4,   -7,  -45,  -66,   -9,  -29,  -75,
             -29,   -2,  -49,  -60,  -57,  -32,  -50, -110,
               1,    2,  -12,  -32,  -29,    3,  -21,  -46,
              17,  -18,    3,   28,   18,    4,   10,    1,
              70,   -1,    5,   74,  127,  175,  136,   93,
             123,  190,  120,  160,  100,  122,   71,   49,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -56,   -6,   33,    2,  -14,    7,   -6,  -29,
             -49,  -25,  -25,  -50,  -21,  -40,  -39,  -81,
             -24,  -23,   18,   -8,    8,   12,   -7,  -32,
              57,   59,   60,   54,   28,   -8,   19,   35,
             226,  145,  176,  148,   45,   63,   19,  115,
             177,   71,  109,   64,  110,   22,  136,  109,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
            -113,  -21,  -48,  -28,   -8,   -5,  -19,  -64,
             -34,  -47,   -5,   10,   11,   21,   -3,   -4,
             -24,   -6,    5,   23,   33,   13,    9,    3,
              -6,   15,   26,   25,   31,   50,   54,   17,
              16,   20,   46,   50,   19,   46,   17,   69,
             -31,   26,   45,   54,   96,  181,   40,   -9,
             -13,  -24,    7,   62,   48,  134,   17,  -14,
            -309,  -53,  -30,   72,   73, -110,    4, -207,

            /* knights: KQ */
             -96,  -46,  -63,  -54,  -34,  -27,  -43,  -25,
             -85,  -41,  -12,   14,   -4,   20,  -68,  -51,
             -47,   -3,   -4,   27,   23,   26,   -9,  -20,
              -6,   51,   38,   62,   51,   29,   27,  -16,
              63,   30,   71,   69,   89,   70,   53,   21,
              55,   76,  146,  136,   74,   74,   -4,   -4,
               7,  -14,   10,   80,   33,   33,  -26,  -42,
            -294,  -13,  -11,   39,  -43,   -2,  -32, -122,

            /* knights: QK */
            -101,  -76,  -52,  -74,  -31,  -25,  -57,  -94,
             -44,  -32,   12,   -6,  -13,   -1,  -63,  -17,
             -16,   -3,   25,   48,   61,  -18,    2,  -56,
              28,   56,   53,   38,   51,   39,   39,  -13,
              19,   61,   77,  126,   74,   85,   56,   74,
              -1,   15,   45,   87,  109,  166,   46,   24,
             -24,    4,   21,   73,  -12,   74,   -3,   65,
            -148,  -34,  -23,   29,   -7,  -35,  -34,  -59,

            /* knights: QQ */
               8,  -37,   30,  -73,  -40,  -48,  -47,  -67,
             -36,  -34,   14,   29,  -17,   14,    1, -139,
              10,    9,   24,   38,   31,  -12,  -14,  -30,
              17,   25,   79,   82,   52,   50,   52,  -35,
              50,   24,   71,   87,   86,   72,   74,   13,
              78,  -35,   52,  148,   64,   39,   71,    5,
             -21,   24,   -7,   21,   54,   52,  -12,  -57,
            -121,    0,   -1,    1,  -20,   16,    5,  -50,

            /* bishops: KK */
              19,   20,    2,  -19,    6,    3,    6,    5,
              21,   21,   19,   12,   16,   27,   45,   30,
              -1,   20,   31,   21,   19,   21,   23,    6,
              -6,    1,   21,   49,   39,   19,   10,   35,
              -2,   24,   15,   56,   37,   51,   26,    7,
               4,   20,   -4,    1,   13,   84,   58,   -7,
             -44,  -22,   -7,  -24,  -29,    8,  -79,  -57,
            -100,  -61,  -36,  -36,  -48,  -83,   10,  -70,

            /* bishops: KQ */
             -50,   28,  -28,  -19,   18,    6,  102,   16,
             -21,   -7,   -2,   10,   19,   51,   84,   19,
             -29,    7,   31,   32,   29,   58,   59,   48,
              22,    4,   18,   68,   85,   45,   23,   -6,
              11,   55,   44,  135,   91,   35,   37,   27,
               8,  110,  134,   14,   47,   42,   31,    6,
             -49,   32,  -43,  -29,    3,   -6,  -18,  -61,
             -32,  -34,   18,  -15,  -12,  -21,  -27,  -50,

            /* bishops: QK */
              40,   34,   21,  -25,   -8,  -28,  -30, -134,
              60,   70,   30,   36,   -4,  -14,  -29,  -11,
              14,   47,   51,   28,   19,    6,   10,  -17,
               4,   33,   32,   83,   60,   13,  -25,  -11,
             -10,   30,   47,   98,   54,   56,   50,  -14,
             -30,    0,   46,   34,   57,   79,   91,   21,
             -40,  -57,  -34,  -24,  -60,    3,  -26,  -29,
             -56,  -56,  -24,  -44,    4,  -33,   26,  -28,

            /* bishops: QQ */
             -78,  -24,   19,  -61,    0,  -15,   38,  -19,
              49,   68,   -9,   41,  -28,   38,   -6,   92,
              12,   42,   48,   22,   39,   22,   51,   -4,
              49,   60,   24,   87,   34,   70,   19,   25,
              34,   38,   37,   95,   96,   11,   37,    6,
             -36,   92,   98,   62,    4,   71,   -4,    1,
             -47,  -24,   36,    0,   30,  -19,   36,  -19,
             -27,   -5,   -4,   -1,   20,   -8,    7,  -30,

            /* rooks: KK */
             -33,  -21,  -16,   -3,    5,   -1,   13,  -28,
             -49,  -32,  -25,  -15,  -13,   11,   37,  -18,
             -50,  -47,  -34,  -21,    1,   -4,   30,   -2,
             -43,  -31,  -22,  -10,  -19,  -26,   28,  -21,
             -24,   -8,    2,   26,   -1,   40,   73,   24,
             -10,   28,   35,   38,   73,  140,  132,   68,
             -30,  -29,   -2,   33,   19,   87,  102,  141,
              57,   45,   39,   56,   36,   98,  115,  126,

            /* rooks: KQ */
             -51,   -4,  -16,  -25,  -24,  -18,  -31,  -59,
             -70,  -17,  -31,  -29,  -26,  -37,  -42,  -42,
             -49,  -26,  -45,  -18,  -52,  -27,   -6,  -50,
             -68,  -14,  -65,   -7,  -20,  -50,   -7,   -7,
             -10,   46,   -8,   -8,   43,   32,   41,    7,
              79,   90,   56,   79,   77,   55,  131,   53,
             -19,   17,   53,   47,   73,   13,   50,   96,
              19,   50,   53,   45,   40,   75,   74,  110,

            /* rooks: QK */
             -93,  -27,  -32,  -24,  -17,  -16,   -4,  -45,
             -48,  -48,  -50,  -36,  -51,  -51,  -23,  -35,
             -39,   -5,  -33,  -44,  -67,  -73,  -23,  -32,
              -5,   43,  -33,  -34,  -52,  -51,    9,  -24,
              62,   33,   60,  -15,  -24,  -10,   17,    6,
              44,  100,   40,  -13,   55,   99,  106,   46,
              35,   45,   78,    3,  -35,   77,   33,   12,
             242,  103,   80,   39,   11,   72,   93,   94,

            /* rooks: QQ */
             -56,  -21,   13,   17,   -4,  -18,  -34,  -44,
             -32,  -40,  -33,    8,  -29,  -44,  -26,  -29,
             -80,   27,    3,  -12,  -37,  -51,  -47,  -52,
             -39,   45,  -32,  -15,  -47,  -26,  -49,  -31,
              39,   69,    7,  108,   40,   38,    2,    6,
              79,   92,  117,   93,   69,   67,   31,   18,
              89,   65,   21,   78,  158,   63,   48,   13,
              43,   32,    6,   -8,    9,   55,   97,   13,

            /* queens: KK */
              39,   46,   56,   60,   80,   60,  -11,    3,
              26,   49,   50,   59,   61,   87,   84,   50,
              23,   38,   45,   33,   51,   38,   50,   47,
              33,   15,   24,   20,   20,   31,   54,   16,
              24,   32,   -8,   -4,    5,   17,   24,   21,
              15,   22,  -14,  -31,   10,  105,   56,   18,
             -15,  -33,  -27,  -15,  -56,   80,   19,  126,
             -34,   33,   16,    3,   27,  108,  159,  125,

            /* queens: KQ */
              18,   14,   28,   19,   20,    7,  -29,  -25,
              33,   31,   45,   23,   29,   21,  -37,  -51,
               9,    7,   41,   23,   32,   27,   13,   -5,
               1,   57,  -20,  -14,   16,   -9,    3,  -14,
              19,   32,    1,   -3,   40,   -5,    6,    5,
              52,   92,   62,   25,   11,   19,   28,   16,
              57,   95,   71,   73,   39,  -11,  -17,   22,
              78,  124,   92,   80,   25,   56,   49,  -15,

            /* queens: QK */
             -74,  -87,   -7,   -5,   16,   -2,  -28,  -59,
             -68,  -35,   39,   23,   21,    1,   41,    2,
             -47,   -1,    6,  -12,   14,  -18,   14,   -3,
             -16,  -42,  -28,   -7,  -19,   -3,   11,  -13,
              -8,  -13,  -44,  -38,  -32,   39,    4,   13,
              -2,   -4,  -19,  -31,   11,   68,   40,   18,
               6,   -1,  -53,  -44,  -26,   -4,   48,   61,
             -37,   11,   24,   21,  -34,   75,   92,   61,

            /* queens: QQ */
             -58,  -74,  -39,  -40,   11,  -14,  -68,  -76,
             -78,  -49,    7,   24,  -36,  -23,  -17,  -62,
             -24,  -14,   10,  -11,  -12,  -31,  -18,  -38,
             -46,   25,  -39,    1,   -6,   -2,  -15,  -38,
             -26,  -19,   44,   37,   66,    3,  -17,   -1,
              37,   46,   90,   47,    2,  -33,  -12,   -8,
              82,   50,   67,   26,    7,  -22,  -12,  -44,
               8,   68,   16,   24,   12,   12,   25,   13,

            /* kings: KK */
               0,    0,    0,    0, -107, -124,  -49,  -59,
               0,    0,    0,    0,  -55,  -46,  -25,  -52,
               0,    0,    0,    0,   -6,  -44,  -49, -101,
               0,    0,    0,    0,  112,   62,   12, -126,
               0,    0,    0,    0,  232,  288,  178,   34,
               0,    0,    0,    0,  253,  297,  269,  153,
               0,    0,    0,    0,  206,  150,  147,   65,
               0,    0,    0,    0,   66,   69,   67,   -7,

            /* kings: KQ */
               0,    0,    0,    0, -149, -164,  -71,  -81,
               0,    0,    0,    0,  -53,  -55,  -60,  -83,
               0,    0,    0,    0,  -42,  -15,  -39, -115,
               0,    0,    0,    0,  128,   56,  -17,  -84,
               0,    0,    0,    0,  229,  144,   83,   -3,
               0,    0,    0,    0,  175,   96,  113,   -6,
               0,    0,    0,    0,  182,  219,  101,   31,
               0,    0,    0,    0,   72,   74,   83,    3,

            /* kings: QK */
             -67,  -45,  -90, -164,    0,    0,    0,    0,
             -40,  -60,  -71,  -89,    0,    0,    0,    0,
             -98,  -71,  -39,  -81,    0,    0,    0,    0,
             -98,    3,   48,   42,    0,    0,    0,    0,
             -46,   64,   82,  161,    0,    0,    0,    0,
             -28,   85,  156,  171,    0,    0,    0,    0,
              45,  144,  121,  139,    0,    0,    0,    0,
             -26,   41,  126,   53,    0,    0,    0,    0,

            /* kings: QQ */
            -180, -115, -160, -204,    0,    0,    0,    0,
            -124, -116,  -86, -123,    0,    0,    0,    0,
            -130,  -74,  -60,  -58,    0,    0,    0,    0,
             -60,   16,   43,  107,    0,    0,    0,    0,
              53,  149,  216,  216,    0,    0,    0,    0,
              65,  171,  165,  124,    0,    0,    0,    0,
               6,   38,   74,   96,    0,    0,    0,    0,
             -22,   26,   35,   65,    0,    0,    0,    0,

            #endregion

            /* opening mobility weights */

               9, // knights
               5, // bishops
               3, // rooks
               1, // queens

            /* opening squares attacked near enemy king */
              24, // attacks to squares 1 from king
              21, // attacks to squares 2 from king
               7, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
              20, // # friendly pawns 1 from king
               7, // # friendly pawns 2 from king
               6, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -10,

            /* opening backward pawns */
            15,

            /* opening doubled pawns */
            -18,

            /* opening adjacent/connected pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              17,   -2,    6,   12,   28,   -1,  -30,    2,
              13,   20,   15,   19,   56,   -4,   18,   10,
             -17,   18,   -3,   30,   35,   35,   17,   14,
              33,    9,   66,   10,  101,  103,   11,   30,
              68,  137,  176,  187,  168,  166,  151,   80,
              78,  120,  117,  120,   82,   53,   45,   26,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening knight on outpost */
            -1,

            /* opening bishop on outpost */
            6,

            /* opening bishop pair */
            40,

            /* opening rook on open file */
            35,

            /* opening rook on half-open file */
            12,

            /* opening rook behind passed pawn */
            0,

            /* opening doubled rooks on file */
            9,

            /* opening king on open file */
            -79,

            /* opening king on half-open file */
            -31,

            /* opening castling rights available */
            38,

            /* opening castling complete */
            19,

            /* opening center control */
               0, // D0
               3, // D1

            /* opening queen on open file */
            -10,

            /* opening queen on half-open file */
            7,

            /* opening rook on seventh rank */
            15,

            /* opening passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
               1,  -15,  -28,  -47,   14,  -30,  -24,   30,
              11,   -2,  -17,   -9,  -35,  -55,  -59,   19,
              15,    5,  -16,   18,  -13,  -40,  -64,  -14,
              48,   76,   34,   15,   19,   52,   44,    4,
              69,  111,  109,   34,   -4,   88,   42,    2,
             274,  256,  241,  252,  261,  253,  302,  283,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -2,    1,   -5,   -4,  -12,  -29,  -23,    0,
              -5,   -9,  -15,  -10,  -15,  -17,  -16,   -2,
              -6,    3,    0,   -1,  -18,  -13,  -12,   -2,
               5,    7,    0,    3,   -7,   -9,   -7,   -3,
              21,    8,   17,    7,   32,   -6,   49,  -11,
              65,  107,  103,   97,   94,  112,   78,  161,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening block passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,  // blocked by pawns
               0,   76,   55,   51,   51,   73,  180,    0,  // blocked by knights
               0,   33,   18,   12,    7,   60,  195,    0,  // blocked by bishops
               0,  -63,  -17,  -24,  -30,   32,  271,    0,  // blocked by rooks
               0,   15,   58,   -1,   -2,   25,  -49,    0,  // blocked by queens
               0,   -1,   84,   55,   92,  235,  390,    0,  // blocked by kings

            /* opening supported pawn chain */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,
              39,   45,   61,   63,   45,   47,   36,   74,
               0,   24,   24,   36,   36,   21,   40,   16,
             -16,   26,   64,   51,   64,   68,   22,   31,
              70,  151,  137,  171,  186,   86,  101,  106,
             105,  241,  224,  196,  261,  164,  225,  -64,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening enemy king outside passed pawn square */
            17,

            /* opening passed pawn/friendly king distance penalty */
            -2,

            /* opening passed pawn/enemy king distance bonus */
            0,

            /* opening pawn rams */
               0,    0,    0,    0,    0,    0,    0,    0,
              47,   18,   19,   22,   93,   28,   24,   60,
              10,    5,   25,   26,   16,   -5,   12,   30,
               0,    0,    0,    0,    0,    0,    0,    0,
             -10,   -5,  -25,  -26,  -16,    5,  -12,  -30,
             -47,  -18,  -19,  -22,  -93,  -28,  -24,  -60,
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening piece threats */
            /* P     N     B     R     Q     K */
               0,   67,   49,   76,   59,    0,  // Pawn threats
               0,  -12,   48,   89,   29,    0,  // Knight threats
               0,   31,   -5,   55,   33,    0,  // Bishop threats
               0,   21,   29,  -14,   52,    0,  // Rook threats
               0,    0,   -5,  -10,  -14,    0,  // Queen threats
               0,    0,    0,    0,    0,    0,  // King threats

            /* opening pawn push threats */
               0,   32,   35,   34,   35,    0,  // Pawn push threats

            /* opening king on open diagonal */
            -8,


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game piece values */
            155, 452, 500, 867, 1528, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              13,   11,   -4,  -25,   28,  -11,  -34,  -39,
               6,   -7,  -26,  -17,  -26,  -24,  -28,  -19,
               9,    4,  -37,  -50,  -38,  -39,  -16,  -30,
              57,   29,    9,   -9,  -34,  -23,   -6,  -15,
              85,   97,   88,   72,   67,   18,   32,   21,
              95,   92,  101,   55,   64,  -11,   54,    3,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              29,   37,   15,   48,  -21,  -40,  -46,  -19,
               3,    4,  -24,  -29,  -49,  -37,  -41,  -11,
              21,    4,  -20,  -40,  -36,  -34,  -26,  -28,
              70,    2,   -9,    4,  -15,   25,   13,    4,
              52,   11,  -15,   46,  104,  125,  126,  109,
               8,   38,   -3,   77,  107,  153,  119,  107,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              14,   -3,  -10,   15,   13,   -7,   -4,   18,
              10,  -15,  -19,  -31,  -24,  -31,  -23,    5,
               7,    4,  -17,  -36,  -23,  -35,  -24,  -13,
              62,   55,   34,   11,  -20,  -25,  -14,    9,
             105,  142,  137,   98,   62,    7,   33,   57,
              83,  102,  151,   93,   57,   -4,   21,   27,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,   -9,  -29,  -19,  -23,  -39,  -37,  -24,
              -9,  -13,  -35,  -44,  -52,  -54,  -46,  -21,
               3,   -2,  -41,  -48,  -52,  -63,  -41,  -33,
              12,  -12,  -26,  -10,  -35,  -23,  -27,  -14,
             -15,   -3,  -31,   23,   81,   57,   68,   56,
              -7,   50,  -13,   62,   36,   80,   55,   65,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -41,  -75,    0,   16,    8,   10,  -10,  -57,
             -41,   27,    7,   12,   21,   33,   25,    7,
              -9,   23,   33,   61,   54,   28,   39,   34,
              33,   54,   79,   91,   99,   81,   72,   45,
              48,   48,   61,   91,  101,  107,  113,   54,
              67,   49,   59,   60,   45,   19,   75,   66,
              40,   62,   63,   61,   67,    1,   24,   45,
              58,   69,   69,   47,   44,   50,   47,  -75,

            /* knights: KQ */
            -110,  -48,    2,   13,  -12,  -15,   15, -127,
             -15,   11,  -21,  -17,   -5,    1,   32,  -10,
              -7,  -30,    5,    3,    2,  -33,   14,   12,
               3,  -12,   19,    8,   18,   26,   15,   22,
             -46,  -21,   -4,    5,   -2,    2,   -5,   -6,
             -39,  -65,  -70,  -51,   -5,    0,   30,    1,
             -45,  -29,  -44,  -17,   -7,    7,   22,    1,
            -119,  -55,  -35,   -8,  -30,   36,   34,  -54,

            /* knights: QK */
            -104,   23,   21,   45,   -4,  -19,  -36,  -80,
             -61,   16,   -4,   10,    8,    6,    5,  -57,
              10,  -13,  -11,   21,   -2,   16,  -14,  -27,
               0,   11,   22,   49,   22,   28,    5,    1,
              25,   12,   22,   -1,   24,   14,    3,  -23,
              35,   31,   30,   -9,   -6,  -39,   -9,  -26,
              26,   13,   38,   42,   34,  -10,  -20,  -36,
             -84,   22,   36,   -5,   25,  -26,  -27,  -99,

            /* knights: QQ */
            -108,  -15,  -30,    9,   -2,    3,  -72,  -83,
             -41,   28,  -31,  -20,  -10,  -53,  -19,  -39,
             -23,  -24,  -27,   -6,   -6,  -17,   -4,  -48,
              43,    7,    0,  -15,   -6,  -15,   -1,   11,
              -9,    9,  -17,  -15,   -8,  -21,  -29,    2,
             -24,   -1,  -28,  -38,  -23,    9,  -26,  -26,
              30,  -12,   -2,   25,   10,    1,   10,   16,
             -86,   58,    6,   30,  -15,   33,   48,  -45,

            /* bishops: KK */
             -15,  -16,    3,   46,   37,   42,   11,  -49,
               2,   11,   22,   44,   39,   32,    7,  -45,
              26,   42,   62,   52,   76,   47,   22,   21,
              48,   63,   72,   57,   46,   51,   36,   -9,
              65,   36,   50,   48,   56,   49,   45,   59,
              50,   58,   58,   57,   65,   62,   48,   88,
              75,   65,   66,   76,   74,   54,   62,   50,
              95,   86,   83,   89,   83,   71,   28,   56,

            /* bishops: KQ */
               2,   -9,   13,    5,   -9,  -14,  -91,  -95,
             -15,   -1,   12,    8,    3,  -24,  -27,  -70,
               8,    8,   13,    5,   15,  -10,  -16,  -37,
              15,   22,    4,  -10,   -6,   24,   17,   19,
              22,  -12,  -13,  -61,   -1,   22,   18,    5,
             -10,  -37,  -29,   14,    5,   12,   23,   35,
             -32,  -17,   22,   18,   11,   21,   44,   69,
               5,    6,   -6,   21,   19,   35,   38,   36,

            /* bishops: QK */
             -65,  -35,    4,   27,   18,    1,   27,    0,
             -57,  -22,   12,   15,   15,   14,   -2,  -19,
              -2,    8,   16,   27,   35,    4,   -7,  -15,
               2,   21,   41,    4,   -5,   28,   30,   19,
              47,   22,   17,   -3,    7,   13,  -13,   31,
              47,   47,   16,   32,   25,   20,  -29,    8,
              53,   45,   51,   50,   50,   29,   -3,  -27,
              35,   40,   42,   55,   14,    2,    7,   18,

            /* bishops: QQ */
             -19,  -17,  -32,   -9,  -20,  -34,  -51,  -82,
             -74,  -49,   -9,  -24,    4,  -45,  -33, -107,
             -21,  -27,  -24,  -20,  -17,  -27,  -57,  -24,
             -19,  -21,  -21,  -43,  -29,  -26,  -23,  -32,
             -16,  -21,  -33,  -48,  -56,  -21,  -36,   -7,
              -3,  -24,  -37,  -25,    3,  -38,   -7,   -6,
             -23,  -14,  -24,    5,  -23,  -11,  -18,    4,
             -15,  -11,  -20,    8,    9,   -4,    9,   35,

            /* rooks: KK */
             100,   93,   80,   53,   38,   65,   60,   51,
              97,   89,   77,   52,   39,   24,   38,   52,
             100,  103,   78,   52,   30,   44,   34,   43,
             123,  109,   93,   72,   55,   86,   71,   74,
             134,  118,   98,   67,   85,   71,   74,   82,
             133,  111,   94,   67,   49,   52,   65,   74,
             128,  130,   94,   62,   69,   65,   88,   62,
             104,  120,   93,   61,   94,   96,  108,   81,

            /* rooks: KQ */
              15,  -17,  -26,  -11,  -10,   -8,   34,   42,
              12,  -32,  -30,  -27,  -19,    3,   21,   11,
               2,  -16,  -19,  -39,  -13,   -9,    1,    7,
              15,  -10,    4,  -20,  -18,   27,   17,    6,
               7,  -16,   -4,    5,  -26,   -2,   12,   24,
             -11,  -20,  -34,  -41,  -42,   -6,  -20,   14,
              20,    8,  -32,  -25,  -28,   -5,   14,   -8,
              39,   14,  -13,  -14,   -9,   -2,   30,    0,

            /* rooks: QK */
              54,   26,   22,  -11,  -25,  -25,  -26,   12,
              20,   17,   23,  -12,  -19,  -23,  -28,  -15,
              17,   16,    8,   -9,   -1,   -7,  -32,  -30,
              10,   -2,   22,    8,    9,   11,   -5,   -3,
              11,   33,    0,   20,   12,    7,    4,  -12,
              26,   11,   24,   18,  -17,  -11,  -13,    2,
              18,   20,   -2,   10,   20,  -10,   13,   16,
            -106,   -1,    3,   14,   19,   -3,    6,   -3,

            /* rooks: QQ */
             -15,  -33,  -69,  -87,  -63,  -30,   -5,   15,
             -47,  -26,  -45,  -86,  -74,  -56,  -44,  -17,
             -14,  -54,  -65,  -72,  -36,  -49,  -12,   -4,
             -13,  -46,  -26,  -45,  -48,  -33,   -8,    4,
             -31,  -42,  -30,  -79,  -50,  -34,    0,    1,
             -38,  -29,  -49,  -63,  -63,  -30,  -10,    1,
             -40,  -24,  -12,  -57,  -94,  -58,  -22,    0,
               2,   11,   -4,   -4,  -38,  -33,   -2,    9,

            /* queens: KK */
              56,   30,    4,   15,  -49,  -76,  -25,   56,
              84,   53,   49,   31,   21,  -31,  -50,  -16,
              61,   62,   70,   70,   53,   86,   80,   30,
              83,  105,   89,   97,   87,  106,   87,  139,
              95,  100,  138,  128,  141,  170,  184,  197,
             111,  147,  151,  162,  165,  156,  205,  251,
             165,  196,  183,  156,  206,  141,  280,  167,
             166,  128,  135,  155,  152,  144,  154,  173,

            /* queens: KQ */
             -26,  -13,  -90,  -47,  -26,  -60,  -26,   15,
               8,  -34,  -85,  -35,  -37,  -60,  -28,  -48,
               1,  -20,  -86,  -34,  -30,   19,  -12,  -57,
              32,  -78,   10,   -9,  -11,   29,   29,   51,
             -11,  -14,  -10,  -18,  -35,   28,   39,   69,
              -2,    4,   -1,   -5,   18,   38,   70,   47,
              26,   10,   17,    4,   -2,   71,   90,   51,
              39,   35,   34,   52,   67,   28,   24,   24,

            /* queens: QK */
             -56,  -31, -106,  -70, -103,  -61,  -28,   31,
             -24,  -85, -116,  -72,  -72,  -52,  -64,    0,
             -33,  -22,  -10,   11,  -35,   -5,  -50,   -1,
             -31,   33,   42,    3,   12,  -17,  -20,   -5,
              27,   11,   67,   45,   27,  -18,   20,  -23,
              17,   73,   45,   47,   23,    9,   30,   25,
              -7,   33,   86,   43,   27,   27,   12,   34,
              42,   35,   26,   23,   29,   40,   31,   44,

            /* queens: QQ */
             -53,  -45,  -15,  -58,  -37,  -59,  -31,  -13,
             -30,  -53,  -69,  -89,  -21,  -72,  -48,  -35,
             -18,   -8,  -59,  -54,  -61,  -35,  -50,  -37,
             -25,  -23,   -3,  -54,  -63, -103,  -61,    2,
               3,   13,  -42,  -48,  -50,  -44,  -12,  -44,
              13,   14,   39,  -26,  -24,  -13,   -2,   49,
              22,   42,   12,   32,   -5,  -14,   20,    7,
             -35,   42,   26,   11,  -10,  -29,  -23,  -29,

            /* kings: KK */
               0,    0,    0,    0,   -2,   23,   -7,  -38,
               0,    0,    0,    0,   10,   10,    6,    6,
               0,    0,    0,    0,   16,   20,   12,   12,
               0,    0,    0,    0,    5,   17,   16,   24,
               0,    0,    0,    0,  -21,  -36,   -6,   -2,
               0,    0,    0,    0,  -19,  -29,    2,    4,
               0,    0,    0,    0,  -14,   -5,   50,   13,
               0,    0,    0,    0,    1,   30,   37, -140,

            /* kings: KQ */
               0,    0,    0,    0,   19,   26,  -14,  -24,
               0,    0,    0,    0,    0,    2,   19,   12,
               0,    0,    0,    0,   15,    1,    9,   19,
               0,    0,    0,    0,   -5,   20,   35,   33,
               0,    0,    0,    0,  -12,   21,   36,   39,
               0,    0,    0,    0,    5,   41,   38,   57,
               0,    0,    0,    0,   -3,  -10,   27,   23,
               0,    0,    0,    0,   -8,    2,  -23,  -22,

            /* kings: QK */
             -70,  -66,  -43,  -15,    0,    0,    0,    0,
             -46,  -14,  -29,  -22,    0,    0,    0,    0,
             -16,   -6,  -20,   -2,    0,    0,    0,    0,
              13,    5,   -6,   -7,    0,    0,    0,    0,
              12,   20,    7,  -20,    0,    0,    0,    0,
              34,   25,    3,  -12,    0,    0,    0,    0,
              -4,    4,    2,   -8,    0,    0,    0,    0,
             -21,   20,   -4,    0,    0,    0,    0,    0,

            /* kings: QQ */
              47,    5,   15,    4,    0,    0,    0,    0,
              13,   14,   -9,   -9,    0,    0,    0,    0,
               4,   -7,  -11,   -7,    0,    0,    0,    0,
             -13,    1,    2,  -13,    0,    0,    0,    0,
             -10,   -4,  -17,  -22,    0,    0,    0,    0,
              14,   17,  -18,   10,    0,    0,    0,    0,
              -5,   63,   15,    7,    0,    0,    0,    0,
            -136,   50,    9,    9,    0,    0,    0,    0,

            #endregion

            /* end game mobility weights */

               7, // knights
               4, // bishops
               3, // rooks
               3, // queens

            /* end game squares attacked near enemy king */
              -5, // attacks to squares 1 from king
              -2, // attacks to squares 2 from king
               1, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
              17, // # friendly pawns 1 from king
              19, // # friendly pawns 2 from king
              14, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -8,

            /* end game backward pawns */
            -6,

            /* end game doubled pawns */
            -32,

            /* end game adjacent/connected pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -4,   -1,   -2,   37,   32,  -43,   54,  -47,
               3,   -8,   28,   41,   -5,   24,    2,  -18,
              12,   22,   49,   75,   29,   38,    9,   23,
              70,   54,   88,   86,   73,   37,   60,   47,
             110,  107,  170,  166,  120,  170,   76,   27,
             199,  288,  252,  237,  197,  158,  212,  166,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game knight on outpost */
            26,

            /* end game bishop on outpost */
            26,

            /* end game bishop pair */
            103,

            /* end game rook on open file */
            3,

            /* end game rook on half-open file */
            34,

            /* end game rook behind passed pawn */
            44,

            /* end game doubled rooks on file */
            15,

            /* end game king on open file */
            15,

            /* end game king on half-open file */
            28,

            /* end game castling rights available */
            -42,

            /* end game castling complete */
            -18,

            /* end game center control */
               9, // D0
               5, // D1

            /* end game queen on open file */
            38,

            /* end game queen on half-open file */
            31,

            /* end game rook on seventh rank */
            30,

            /* end game passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              18,   34,   33,   49,    2,   30,   60,   17,
              25,   43,   40,   34,   46,   51,   77,   27,
               3,   21,   32,   22,   23,   41,   68,   38,
              30,   49,   48,   40,   69,   56,   68,   72,
              74,   93,   75,   62,   56,   51,   83,   67,
              92,  129,  144,  135,  139,  143,  147,  120,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -1,   -9,   -7,  -52,  -17,    4,   -6,   -6,
             -10,  -11,  -14,  -19,  -19,   -6,   -8,  -11,
             -12,  -27,  -42,  -56,  -35,  -30,  -23,   -7,
             -28,  -38,  -36,  -50,  -38,  -31,  -32,  -23,
             -36,  -53,  -62,  -62,  -78,  -53,  -96,  -19,
             -58, -108, -114, -136, -167, -124, -155, -159,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game block passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,  // blocked by pawns
               0,  -37,    4,   24,   58,   35,   33,    0,  // blocked by knights
               0,   -1,   50,   58,  109,  116,  134,    0,  // blocked by bishops
               0,   25,  -20,  -23,    8,   -7,  -81,    0,  // blocked by rooks
               0,  -38,  -30,   26,   -6, -170, -182,    0,  // blocked by queens
               0,   16,    8,  -53,  -15,    4,   85,    0,  // blocked by kings

            /* end game supported pawn chain */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,
              -2,   14,   19,   32,   35,   15,    9,  -12,
               0,   23,   13,   33,   12,   16,    4,    1,
              13,   28,   36,   34,   35,   14,   32,    6,
              50,   27,   85,   60,   56,   82,   24,    0,
              84,   52,  144,   43,  130,  118,   29,  111,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game enemy king outside passed pawn square */
            207,

            /* end game passed pawn/friendly king distance penalty */
            -21,

            /* end game passed pawn/enemy king distance bonus */
            36,

            /* end game pawn rams */
               0,    0,    0,    0,    0,    0,    0,    0,
              16,   29,   31,   56,   30,   25,   20,   14,
              14,   14,   14,   27,   12,    8,    4,   -5,
               0,    0,    0,    0,    0,    0,    0,    0,
             -14,  -14,  -14,  -27,  -12,   -8,   -4,    5,
             -16,  -29,  -31,  -56,  -30,  -25,  -20,  -14,
               0,    0,    0,    0,    0,    0,    0,    0,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game piece threats */
            /* P     N     B     R     Q     K */
               0,   73,   92,   60,    9,    0,  // Pawn threats
               0,    6,   28,   -1,   55,    0,  // Knight threats
               0,   62,   37,   32,  160,    0,  // Bishop threats
               0,   55,   54,   65,   91,    0,  // Rook threats
               0,   40,   75,   29,   16,    0,  // Queen threats
               0,    0,    0,    0,    0,    0,  // King threats

            /* end game pawn push threats */
               0,   31,  -22,   27,  -25,    0,  // Pawn push threats

            /* end game king on open diagonal */
            11,
        };
    }
}