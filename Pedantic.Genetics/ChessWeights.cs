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

        // Solution sample size: 12000000, generated on Sun, 06 Aug 2023 15:18:00 GMT
        // Object ID: 97723267-fc7e-4ef7-82a1-41b5d7ad488f - Optimized
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening piece values */
            100, 395, 415, 580, 1345, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -25,  -34,  -17,  -10,   -3,   54,   39,   -4,
             -23,  -16,   -6,   -2,    6,   34,   34,   10,
             -14,  -13,   13,   26,   39,   52,   22,    3,
             -15,    7,   31,   43,   69,   80,   42,   17,
             -14,    7,    7,   47,   82,  164,  168,   62,
              73,  112,  111,  102,   65,   11, -120,  -57,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -53,  -63,  -39,  -39,  -17,   48,   37,   -7,
             -53,  -50,  -23,   -8,   11,   35,   42,   14,
             -26,    2,    7,   28,   23,   29,   20,   10,
             -24,   30,   43,   43,   33,   27,   31,    9,
              47,  100,  115,   27,   45,   37,  115,   64,
              46, -113,   82,   90,  172,  112,  228,  182,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              -1,   35,   20,  -17,  -37,   -9,  -35,  -48,
              14,   41,    9,   -1,  -12,   11,  -21,  -32,
               9,   15,    8,   20,   25,   37,   18,   -8,
             -11,   26,   20,   26,   57,   85,   43,   15,
               2,   27,   15,   32,   85,  159,  182,   32,
             181,  254,   98,  117,   51,   50,  -86,  -87,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -15,   20,   29,   -6,  -24,   -6,  -24,  -27,
             -14,   13,   18,  -11,   20,   -8,  -12,   -4,
              -6,    5,   31,   37,   30,   24,   10,  -11,
              35,   75,   77,   32,   65,   52,   19,   16,
             115,  127,  100,   61,   35,   29,   99,   52,
             170,   26,  185,   -1,  144,   55,  346,  286,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
            -105,    0,  -19,    2,   18,   17,    4,  -31,
             -35,  -29,    9,   32,   29,   29,   20,   11,
             -17,    4,    7,   40,   54,   17,   28,    9,
               7,   29,   30,   34,   43,   45,   47,   28,
              26,   16,   52,   64,   17,   50,    6,   61,
              14,   56,   52,   62,  108,  170,   43,    2,
               8,   19,   42,   64,   69,  115,   30,   46,
            -231,  -65,  -19,   22,  127,  -78,   16, -200,

            /* knights: KQ */
            -131,  -51,  -27,    6,   -4,  -10,  -29,  -32,
             -49,  -25,   -9,   32,   17,   26,  -31,  -40,
             -39,    3,   -2,   47,   39,   15,    7,  -15,
               1,   51,   38,   65,   42,   49,   15,   20,
              46,   24,   48,   81,   71,   86,   26,   24,
              32,   79,  135,  160,   84,   57,   37,   25,
              63,   -6,   26,   61,   78,   55,   32,   -3,
            -270,  -49,   19,   67,   51,   22, -142, -189,

            /* knights: QK */
             -87,  -45,  -24,  -32,   -2,    9,  -50, -142,
             -14,   13,    1,   10,   12,    0,   -8,  -16,
             -22,   -9,   23,   63,   70,  -23,    8,  -27,
              28,   65,   38,   44,   61,   30,   35,   -3,
              38,   44,   78,  100,   56,   81,   30,   48,
               5,   38,   64,   81,  125,  149,   21,   -9,
             -45,  -28,   68,   98,   39,   70,   16,   53,
            -133,  -59,    8,  139,   30,   10,  120, -154,

            /* knights: QQ */
              54,    3,   -1,  -68,   -8,  -12,  -31, -278,
             -26,   -8,    9,   31,    7,   21,  -34,  -79,
               1,   19,   25,   21,   39,  -24,   19,   -1,
               9,   11,   87,   65,   24,   48,   34,   -1,
              24,    8,   59,   89,   68,   69,   39,   10,
              23,  -10,   91,  166,  118,   72,  124,   36,
               7,   19,   20,  -29,   13,  -32,  -17, -137,
            -238, -108,  -41, -117,   47,  -25,  -14,  -87,

            /* bishops: KK */
              25,   66,   26,   18,   27,   43,   56,   49,
              65,   42,   69,   36,   53,   55,   81,   58,
              16,   57,   52,   56,   42,   59,   44,   49,
              38,   25,   45,   72,   75,   20,   30,   54,
              20,   39,   42,   92,   52,   57,   19,   18,
              49,   54,   47,   33,   55,   89,   72,   24,
               5,   34,   21,   36,   12,   11,  -49,   -5,
             -15,  -50,  -38,  -11,   -3,  -77,   37,  -20,

            /* bishops: KQ */
             -34,  -10,    4,    2,   38,   38,   80,   19,
             -12,   28,   38,   29,   51,   80,   93,   57,
               6,   20,   41,   49,   53,   84,   57,   63,
              45,   32,   39,   81,  126,   53,   43,   25,
              -5,   50,   65,  121,   93,   58,   39,   18,
              24,   73,  108,   70,   86,   65,   56,   -4,
             -21,   76,   44,   21,   37,    6,   -7,  -60,
             -52,  -40,   23,   25,    3,   53,  -33,  -10,

            /* bishops: QK */
              28,   64,   23,   43,    1,   14,   -8,  -86,
              63,   83,   67,   41,   34,    6,   -4,    6,
              52,   75,   70,   72,   26,   34,   26,    4,
              21,   61,   57,   93,   87,   12,   27,   19,
              23,   41,   69,  130,   62,   97,   25,    6,
               4,   50,   57,   57,   95,  106,  108,   28,
              19,   22,  -29,   11,    0,   29,  -71,   -4,
             -58, -100,  -20,  -33,   44,   -5,  140,  -94,

            /* bishops: QQ */
             -75,  -49,   32,  -44,  -14,   21,   21,   26,
              33,   46,   40,   50,   14,   45,   27,   41,
              33,   45,   42,   48,   53,   54,   48,   33,
              44,   50,   55,  102,   59,   53,   43,   40,
             -11,   45,   43,   94,   92,   27,   39,   10,
             -42,  112,   99,  123,    6,   39,   13,   34,
             -62,  -25,   91,   23,   39,  -15,   34,    5,
             -56,  -33,   36,   58,   48,   21,  -38, -110,

            /* rooks: KK */
             -11,    5,    6,   22,   29,   21,   34,   -4,
             -21,  -17,    0,    9,    4,   22,   43,   -1,
             -36,  -23,  -17,   -3,   10,    8,   45,   18,
             -24,  -23,   -8,   11,   -2,  -14,   35,  -11,
               6,   12,   18,   54,   12,   34,   89,   42,
              30,   66,   72,   71,  101,  154,  129,   76,
              -4,   -1,   23,   56,   32,  132,   88,  119,
              73,   68,   87,  101,   67,   93,  135,  139,

            /* rooks: KQ */
             -16,   24,    7,    6,   12,    4,    3,  -26,
             -49,  -12,  -11,   -3,   -2,  -16,  -37,  -19,
             -43,   -9,  -39,    3,  -36,  -10,   21,   -6,
             -64,    3,  -35,   -3,    9,  -39,    1,  -25,
              -3,   54,   -2,   43,   35,   59,   46,   21,
              57,  129,  105,  166,  113,   96,  142,  112,
              10,   53,   91,  111,   99,   71,   71,   89,
              76,   86,  111,   78,   49,  130,  141,  149,

            /* rooks: QK */
             -56,    3,  -15,   10,    2,    6,   24,  -18,
             -12,   -8,   -4,    0,  -37,  -17,   -1,   -6,
             -10,  -24,  -11,  -25,  -29,  -30,    8,  -21,
               2,   22,  -25,  -21,  -33,  -31,    7,  -18,
              72,   43,   40,   16,    6,   27,    8,   22,
             109,   86,   97,   32,   82,  134,  138,   88,
              72,   63,   91,   -4,   11,  106,   30,   73,
             242,  167,  106,   48,   57,  104,  150,  128,

            /* rooks: QQ */
             -24,  -21,   16,   29,   12,   11,    5,  -19,
             -66,  -57,  -38,   12,    6,  -26,   22,  -22,
             -52,   -6,    3,  -24,  -44,  -31,  -31,  -13,
             -42,    0,   -3,   16,  -15,   15,  -44,  -12,
              59,  111,   -2,   66,   62,   25,   43,   -9,
              99,  174,  214,   94,  160,   89,   99,   67,
             144,  135,   71,   82,  211,  134,   19,   11,
              58,   45,   12,   -2,   46,  112,  151,   36,

            /* queens: KK */
              66,   79,   96,   97,  122,   83,   16,   26,
              67,   84,   92,   95,  100,  126,  113,   66,
              58,   76,   82,   73,   86,   79,   92,   76,
              65,   46,   56,   53,   67,   48,   75,   56,
              54,   62,   44,   51,   41,   44,   38,   46,
              66,   65,   45,   28,   59,  117,   65,   66,
              35,   12,   42,   49,  -11,   66,   40,  160,
              31,   74,   84,   54,   66,  180,  229,  147,

            /* queens: KQ */
              49,   44,   53,   57,   55,   36,   -5,  -17,
              36,   72,   82,   58,   70,   86,   11,  -13,
              32,   56,   72,   54,   68,   54,   20,   24,
              32,   56,   32,   44,   44,   39,    9,    4,
              28,   39,    4,   68,   50,   55,   24,   24,
              71,   98,   89,   72,   59,   49,   54,   13,
             109,  136,  101,   95,   82,   13,   12,   18,
              91,  156,  123,  126,   79,   87,   84,   -1,

            /* queens: QK */
             -25,  -71,   46,   24,   56,   40,   40,   16,
             -49,   -2,   74,   62,   58,   49,   59,   48,
             -17,   34,   35,   22,   43,   11,   27,   46,
              11,    9,    7,   38,    7,   28,   55,   34,
              28,   -6,   12,   24,   27,   16,   12,   20,
              25,   -1,    3,   -6,   49,   66,   76,   48,
              45,    1,    3,  -19,  -21,   11,   25,   87,
             -29,   59,   31,   68,    5,  115,  159,  104,

            /* queens: QQ */
             -25,  -56,   59,  -25,   41,    2,   29,   19,
             -89,   -5,   30,   57,    5,   13,   -3,  -16,
              -1,   11,   31,   -9,    7,   10,   49,   36,
             -35,   -4,  -16,    9,   20,   17,   13,   -5,
               5,  -26,  -12,   18,   52,   18,   -6,   32,
              23,   60,  169,   32,   32,   31,   50,   35,
             111,  174,  147,   74,   91,    9,   26,   -9,
              66,  124,  120,   87,   98,   54,   86,   64,

            /* kings: KK */
               0,    0,    0,    0,  -76,  -87,  -11,  -33,
               0,    0,    0,    0,  -28,  -15,    8,  -22,
               0,    0,    0,    0,   32,   -8,   -9,  -54,
               0,    0,    0,    0,  171,  108,   34,  -73,
               0,    0,    0,    0,  346,  337,  195,   36,
               0,    0,    0,    0,  373,  437,  403,  261,
               0,    0,    0,    0,  362,  315,  267,  170,
               0,    0,    0,    0,  164,  126,  161,  218,

            /* kings: KQ */
               0,    0,    0,    0, -110, -110,  -21,  -44,
               0,    0,    0,    0,  -24,    0,  -16,  -19,
               0,    0,    0,    0,   42,   17,    8,  -46,
               0,    0,    0,    0,  165,  125,   43,   10,
               0,    0,    0,    0,  319,  185,  142,   49,
               0,    0,    0,    0,  312,  214,  155,   97,
               0,    0,    0,    0,  337,  331,  201,   73,
               0,    0,    0,    0,  221,  178,  211,  -38,

            /* kings: QK */
             -45,   -6,  -67, -119,    0,    0,    0,    0,
             -25,  -30,  -15,  -53,    0,    0,    0,    0,
             -49,    1,   -3,  -35,    0,    0,    0,    0,
             -48,   34,   89,   71,    0,    0,    0,    0,
             -31,  110,  129,  199,    0,    0,    0,    0,
              49,  151,  215,  241,    0,    0,    0,    0,
             156,  242,  221,  271,    0,    0,    0,    0,
             -36,  140,  261,  139,    0,    0,    0,    0,

            /* kings: QQ */
            -143,  -52,  -83, -122,    0,    0,    0,    0,
             -82,  -40,  -29,  -53,    0,    0,    0,    0,
            -105,  -31,   10,   14,    0,    0,    0,    0,
             -17,   74,   83,  140,    0,    0,    0,    0,
              50,  212,  246,  321,    0,    0,    0,    0,
             166,  305,  336,  261,    0,    0,    0,    0,
              55,   61,  218,  300,    0,    0,    0,    0,
              25,   61,   94,  134,    0,    0,    0,    0,

            #endregion

            /* opening mobility weights */

            11, // knights
            7, // bishops
            3, // rooks
            1, // queens

            /* opening squares attacked near enemy king */
            21, // attacks to squares 1 from king
            21, // attacks to squares 2 from king
            7, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
            17, // # friendly pawns 1 from king
            12, // # friendly pawns 2 from king
            7, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -7,

            /* opening backward pawns */
            -18,

            /* opening doubled pawns */
            -22,

            /* opening adjacent/connected pawns */
            9,

            /* UNUSED (was opening king adjacent open file) */
            0,

            /* opening knight on outpost */
            -5,

            /* opening bishop on outpost */
            11,

            /* opening bishop pair */
            43,

            /* opening rook on open file */
            37,

            /* opening rook on half-open file */
            14,

            /* opening rook behind passed pawn */
            -7,

            /* opening doubled rooks on file */
            12,

            /* opening king on open file */
            -65,

            /* opening king on half-open file */
            -25,

            /* opening castling rights available */
            32,

            /* opening castling complete */
            22,

            /* opening center control */
            1, // D0
            2, // D1

            /* opening queen on open file */
            -7,

            /* opening queen on half-open file */
            6,

            /* opening rook on seventh rank */
            30,

            /* opening passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              -5,   -8,  -34,  -85,  -10,  -18,   -3,   22,
              -3,    1,  -20,  -27,  -56,  -52,  -65,   17,
              30,   30,   -5,   -9,  -34,  -44,  -45,    1,
              80,   64,   44,   38,    7,   15,   13,   38,
             184,  126,  136,   72,   41,   90,  -27,   32,
             158,  140,  125,  126,  115,   76,   67,   73,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -3,   -1,   -7,   14,  -16,  -23,  -24,    3,
              -6,   -5,  -12,   -5,   -9,  -19,  -14,  -13,
              -7,    6,   -2,   -5,  -17,   -8,   -3,   -1,
              13,   10,   -2,    2,   -7,   -9,    8,    6,
              28,   18,   27,   16,   51,    4,   22,    3,
             112,   87,   87,   90,  102,  128,   82,  162,
               0,    0,    0,    0,    0,    0,    0,    0,


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game piece values */
            95, 330, 370, 605, 1130, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              51,   43,   36,    9,   47,   32,   12,   13,
              48,   34,   30,   21,   25,   23,   13,   14,
              51,   50,   29,    5,    6,   17,   28,   19,
              91,   63,   49,   16,    8,   17,   40,   33,
              97,   85,   90,   64,   63,   37,   32,   25,
             162,  146,  114,   91,   69,   21,   74,   29,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              62,   63,   63,   55,   25,    6,    0,   24,
              54,   48,   39,   30,    2,    3,   -3,   13,
              40,   31,   30,   11,    9,   31,   25,   36,
              51,   11,    3,   17,   28,   76,   70,   88,
              -4,  -41,  -35,   41,  126,  169,  168,  147,
               4,   57,  -32,   26,  139,  174,  169,  163,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              29,    0,   19,   22,   58,   47,   57,   68,
              33,    3,   20,    7,   27,   28,   34,   51,
              51,   50,   46,   17,   21,   22,   22,   34,
             104,   88,   82,   48,   23,    3,   23,   34,
             128,  129,  158,  104,   60,   17,    4,   25,
             140,  146,  203,  129,   74,  -15,    5,   19,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              18,    1,   16,   10,   28,   22,   20,   45,
              22,    7,   16,   17,   -3,   13,    7,   22,
              22,   28,   18,    4,    7,    9,   16,   35,
              23,    5,    6,   21,    1,   28,   34,   49,
             -36,  -27,  -11,   20,   70,   85,   92,   90,
              -5,   60,  -25,   46,   84,  101,   75,   71,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
               6,  -42,    4,   10,    2,    8,  -16,  -23,
             -10,   33,    8,   -4,    9,   30,   26,   20,
              11,   29,   34,   49,   41,   23,   35,   50,
              43,   50,   66,   70,   75,   69,   65,   45,
              55,   46,   51,   58,   72,   71,   89,   62,
              69,   45,   52,   48,   39,   28,   75,   77,
              62,   59,   47,   61,   61,   25,   52,   51,
              96,   87,   83,   67,   42,   73,   56,  -35,

            /* knights: KQ */
             -26,  -25,    7,    9,  -16,   -2,   22,  -88,
               6,   17,   -8,  -16,   -8,   -7,   22,   -6,
              23,  -11,   16,   -4,   10,   -8,    0,   46,
              22,    5,   18,   -1,   17,    9,   13,   15,
              -8,  -20,    3,  -16,   -7,  -10,    3,    5,
             -19,  -26,  -61,  -52,  -10,   15,   20,   18,
             -31,  -22,  -23,   -6,  -28,   13,   38,   21,
              -5,  -16,  -25,    8,  -36,   49,   89,    7,

            /* knights: QK */
             -64,   19,   -1,   24,   -9,  -22,   -8,    6,
             -20,   37,   -4,    2,   -1,    4,  -12,  -13,
              47,    8,   11,   14,    1,   20,   -5,    1,
              27,    5,   23,   28,    8,   24,    8,    9,
              29,   19,   21,    0,   21,    2,    8,    1,
              44,   41,   30,    3,  -13,  -19,    7,   31,
              49,   47,   21,   30,   30,   -4,   -7,  -27,
              34,   79,   40,   21,   16,   -7,  -24, -116,

            /* knights: QQ */
             -63,  -41,  -22,    8,  -12,  -19,  -45,  -18,
              -5,   18,  -48,  -40,  -36,  -66,  -45,  -22,
             -13,  -19,  -30,  -17,  -32,  -12,  -27,  -37,
              26,   -5,  -21,  -27,  -18,  -30,  -14,    1,
               4,   -8,  -45,  -41,  -27,  -29,  -32,   27,
               0,    9,  -47,  -63,  -62,  -12,  -27,  -14,
              37,   13,  -37,   15,  -16,   11,   32,   30,
             -30,   81,   -2,   48,  -19,   21,   62,  -57,

            /* bishops: KK */
              52,   37,   35,   82,   68,   63,   32,    8,
              53,   44,   53,   70,   61,   56,   31,   12,
              80,   80,   94,   90,  106,   73,   59,   63,
              74,   96,  110,   81,   68,   97,   80,   46,
             100,   85,   82,   69,   80,   88,   84,   98,
              77,   82,   82,   87,   94,   99,   83,  115,
              94,   85,   94,   88,   92,   91,   93,   61,
              94,  123,  128,  113,  104,  108,   67,   82,

            /* bishops: KQ */
              84,   77,   53,   57,   22,   25,  -34,  -36,
              64,   42,   43,   36,   28,   -7,   11,   -9,
              69,   56,   61,   46,   50,   31,   31,   12,
              64,   53,   63,   11,   18,   57,   54,   51,
              74,   39,   17,  -12,   33,   52,   61,   59,
              46,   13,   30,   37,   45,   47,   59,   87,
               3,   20,   24,   34,   37,   68,   69,  102,
              50,   56,   45,   45,   58,   52,   78,   81,

            /* bishops: QK */
               1,    1,   43,   33,   53,   39,   77,   70,
              25,   24,   41,   47,   53,   47,   52,   39,
              52,   48,   55,   53,   76,   51,   39,   58,
              60,   60,   64,   33,   28,   74,   59,   32,
              74,   65,   50,   35,   36,   31,   47,   72,
              76,   82,   59,   62,   56,   41,   16,   50,
              73,   64,   88,   64,   56,   59,   53,   24,
              86,  101,   64,   72,   48,   59,   16,   52,

            /* bishops: QQ */
              40,   35,  -10,   26,    4,   -9,  -11,  -21,
              13,    0,    4,   -6,    5,  -15,   -8,  -27,
              27,   11,   13,    0,    2,    2,  -18,   -1,
              31,   16,    8,  -32,   -7,    4,   -7,    1,
              28,    2,  -16,  -25,  -30,   12,   -5,   28,
              32,  -26,  -10,  -10,   32,   -3,    6,   -4,
               6,   11,  -18,   15,   -4,   21,   13,    2,
              17,   12,   11,   -7,   26,   36,   24,   44,

            /* rooks: KK */
             180,  160,  146,  118,  103,  131,  136,  132,
             174,  170,  138,  114,  111,  102,  121,  130,
             168,  164,  140,  115,   95,  107,  107,  105,
             181,  177,  152,  124,  116,  140,  143,  145,
             190,  182,  157,  126,  140,  139,  140,  139,
             193,  172,  153,  135,  115,  114,  143,  150,
             208,  210,  168,  140,  154,  129,  178,  160,
             190,  203,  160,  133,  161,  176,  182,  164,

            /* rooks: KQ */
              87,   58,   59,   55,   57,   70,   92,  114,
              99,   72,   51,   46,   56,   80,  110,   85,
              76,   47,   51,   24,   56,   47,   69,   62,
             100,   74,   81,   43,   45,   85,   89,   96,
              89,   72,   61,   54,   56,   65,   92,  105,
              79,   49,   30,   16,   37,   62,   63,   83,
             111,   94,   56,   48,   57,   60,  105,   94,
             115,   99,   56,   74,   81,   61,  100,   85,

            /* rooks: QK */
             124,   99,   92,   53,   56,   51,   57,   93,
              99,  104,   85,   63,   70,   58,   63,   60,
              88,   98,   72,   60,   49,   57,   40,   46,
              90,   88,   86,   73,   61,   71,   64,   70,
              98,  110,   91,   82,   76,   58,   79,   72,
              95,  105,   78,   81,   60,   35,   62,   62,
             106,  115,   81,  108,  100,   59,  106,   95,
              13,   82,   94,   99,   95,   66,   86,   82,

            /* rooks: QQ */
              56,   51,   12,   -5,    7,   36,   68,   78,
              54,   68,   33,   -7,   -7,   13,   17,   50,
              58,   19,    8,    2,   23,   11,   50,   48,
              55,   37,   29,    2,   18,   21,   56,   64,
              27,    9,   38,    6,    1,   24,   46,   65,
              41,   -6,   -8,   14,  -23,   19,   42,   53,
              34,   48,   43,   29,  -26,   -6,   61,   84,
              79,   82,   76,   73,   33,   29,   65,   87,

            /* queens: KK */
             145,  120,   73,   90,   22,   19,   88,  137,
             162,  133,  105,   94,   78,   44,   40,   97,
             142,  130,  130,  116,  106,  139,  143,  126,
             169,  188,  160,  163,  128,  179,  167,  203,
             204,  185,  186,  167,  185,  218,  267,  273,
             194,  207,  201,  211,  217,  223,  266,  304,
             239,  269,  224,  214,  274,  245,  349,  255,
             226,  211,  196,  219,  239,  189,  209,  227,

            /* queens: KQ */
              30,    6,  -23,   -6,   22,   -4,    3,   49,
              61,   11,  -34,   -8,  -23,  -26,   37,  -34,
              34,   12,  -43,   14,  -19,   64,   86,   55,
             103,   12,   30,   -8,   25,   56,   98,  101,
              80,   61,   57,   -7,   21,   58,  118,  127,
              55,   44,   30,   45,   58,   76,  105,  118,
              67,   55,   62,   47,   67,  123,  159,  121,
              91,   61,   45,   71,   89,   86,   85,  114,

            /* queens: QK */
              25,   50,  -72,  -20,  -42,  -56,   -6,   19,
              74,  -15,  -43,  -16,  -35,  -32,   13,   31,
              25,   31,   50,   38,    8,   23,   21,   24,
              48,   68,   67,   29,   57,   21,   25,   83,
              93,   99,   81,   73,   49,   64,   75,   59,
              70,  132,  127,   97,   38,   59,   85,   55,
              80,  130,  131,  125,   99,  100,   99,   95,
             115,  103,  113,   80,   93,   77,   86,   94,

            /* queens: QQ */
             -95,  -15,  -65,  -20,  -75,  -36,  -24,    4,
             -49,  -46,   -6,  -88,  -13,  -51,  -25,  -12,
              21,   13,  -51,   -3,  -14,  -22,  -24,  -15,
              53,   20,   -9,  -26,  -51,  -48,  -68,   61,
              75,   93,   27,  -11,  -33,  -31,   25,    1,
              50,   79,  -20,   29,   -9,   12,   30,    4,
              60,   15,   15,   47,   23,   49,   63,   47,
             -23,   68,   47,  -25,   18,  -22,   10,   -3,

            /* kings: KK */
               0,    0,    0,    0,   -3,   14,  -18,  -41,
               0,    0,    0,    0,   17,   12,    2,   -3,
               0,    0,    0,    0,   24,   26,   12,    0,
               0,    0,    0,    0,    7,   14,   17,   16,
               0,    0,    0,    0,  -26,  -32,   -3,    3,
               0,    0,    0,    0,  -34,  -43,  -11,  -22,
               0,    0,    0,    0,  -35,  -28,    8,    1,
               0,    0,    0,    0,   -1,   11,   19, -117,

            /* kings: KQ */
               0,    0,    0,    0,   28,   17,  -18,  -19,
               0,    0,    0,    0,   22,    7,   18,   -1,
               0,    0,    0,    0,   27,   19,   15,    5,
               0,    0,    0,    0,   14,   24,   37,   19,
               0,    0,    0,    0,    2,   35,   41,   34,
               0,    0,    0,    0,    8,   34,   41,   32,
               0,    0,    0,    0,  -27,  -24,    7,   14,
               0,    0,    0,    0,  -24,  -18,  -41,  -32,

            /* kings: QK */
             -40,  -57,  -25,   -6,    0,    0,    0,    0,
             -25,   -3,  -21,   -6,    0,    0,    0,    0,
             -13,   -2,   -2,   13,    0,    0,    0,    0,
              -1,   13,    6,   11,    0,    0,    0,    0,
              28,   20,   17,   -5,    0,    0,    0,    0,
              13,   18,    7,   -3,    0,    0,    0,    0,
             -34,  -18,   -3,  -19,    0,    0,    0,    0,
             -13,   -8,  -39,  -17,    0,    0,    0,    0,

            /* kings: QQ */
              65,    7,   -5,   -5,    0,    0,    0,    0,
              17,    4,   -6,   -7,    0,    0,    0,    0,
               6,    2,   -3,    0,    0,    0,    0,    0,
             -10,    1,    6,   -4,    0,    0,    0,    0,
               1,   -5,   -3,  -21,    0,    0,    0,    0,
              -5,    0,  -23,   -5,    0,    0,    0,    0,
              -5,   51,   -3,  -13,    0,    0,    0,    0,
             -88,   41,   12,   -1,    0,    0,    0,    0,

            #endregion

            /* end game mobility weights */

            7, // knights
            1, // bishops
            2, // rooks
            1, // queens

            /* end game squares attacked near enemy king */
            -4, // attacks to squares 1 from king
            -3, // attacks to squares 2 from king
            0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            14, // # friendly pawns 1 from king
            17, // # friendly pawns 2 from king
            12, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -9,

            /* end game backward pawns */
            -10,

            /* end game doubled pawns */
            -25,

            /* end game adjacent/connected pawns */
            9,

            /* UNUSED (was end game king adjacent open file) */
            0,

            /* end game knight on outpost */
            34,

            /* end game bishop on outpost */
            21,

            /* end game bishop pair */
            83,

            /* end game rook on open file */
            -1,

            /* end game rook on half-open file */
            24,

            /* end game rook behind passed pawn */
            22,

            /* end game doubled rooks on file */
            8,

            /* end game king on open file */
            6,

            /* end game king on half-open file */
            38,

            /* end game castling rights available */
            -31,

            /* end game castling complete */
            -14,

            /* end game center control */
            10, // D0
            8, // D1

            /* end game queen on open file */
            32,

            /* end game queen on half-open file */
            27,

            /* end game rook on seventh rank */
            17,

            /* end game passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              27,   38,   43,   72,    7,   16,   46,   15,
              28,   37,   26,   18,   34,   36,   66,   18,
              66,   57,   44,   40,   44,   50,   87,   63,
              89,   92,   68,   55,   68,   60,   82,   77,
             130,  163,  106,   94,   82,   76,  126,  106,
             128,  149,  158,  141,  150,  168,  183,  161,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               0,   -5,   -3,  -43,  -17,    6,   -4,   -4,
              -9,  -11,   -7,  -27,  -18,   -4,   -7,   -6,
             -10,  -29,  -35,  -47,  -33,  -23,  -23,   -8,
             -35,  -41,  -38,  -57,  -42,  -32,  -35,  -35,
             -46,  -67,  -80,  -82, -106,  -74, -108,  -39,
             -88, -137, -133, -165, -204, -162, -185, -169,
               0,    0,    0,    0,    0,    0,    0,    0,
        };
    }
}