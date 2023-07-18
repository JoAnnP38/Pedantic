﻿// ***********************************************************************
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

        // Solution sample size: 12000000, generated on Thu, 13 Jul 2023 23:47:54 GMT
        // Object ID: 156f915e-7d23-4acf-964d-6e6b32c54d46 - Optimized
        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening phase material boundary */
            7000,

            /* opening piece values */
            100, 345, 395, 525, 1215, 0,

            /* opening piece square values */

            #region opening piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -22,  -31,  -22,   -7,  -22,   36,   21,  -15,
             -23,  -15,  -16,  -10,   -4,   11,   20,    1,
             -15,   -8,    2,   14,   18,   30,   14,   -9,
             -10,    3,   22,   33,   53,   61,   37,   16,
              -1,   26,   47,   52,   73,  150,  141,   49,
             245,  223,  225,  225,  167,  112,  -24,   24,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -52,  -65,  -33,  -44,  -30,   21,   23,  -29,
             -48,  -42,  -21,  -15,  -10,    9,   20,    2,
             -23,   -2,   -1,    9,    4,   11,    9,   -4,
             -21,   20,   20,   28,   23,   24,   18,    4,
              48,   74,  110,   23,   45,   56,   66,   31,
             130,   11,  141,  159,  219,  211,  164,  157,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
             -14,   19,   -5,  -29,  -38,  -26,  -42,  -51,
               5,   34,  -17,  -15,  -15,   -9,  -21,  -37,
               7,   18,   -5,    6,   14,   14,    4,  -18,
               7,    8,    8,   18,   33,   59,   32,   10,
              27,   61,   56,   45,  113,  121,  114,   -1,
             225,  256,  160,  230,  192,  169,   -4,   -3,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
             -31,   -9,   10,  -12,  -33,  -10,  -25,  -24,
             -20,   -9,   -7,  -26,    0,  -22,  -16,  -17,
             -13,   -4,    8,    8,    9,   12,   -7,  -15,
              30,   35,   55,   23,   38,   52,   15,   11,
             122,  103,   97,   49,   54,   14,   26,   35,
             221,  136,  205,  150,  192,  128,  237,  191,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -91,  -10,  -24,   -8,    8,   14,   -3,  -37,
             -28,  -29,   -1,   19,   11,   27,   12,    3,
             -25,   -4,   -5,   31,   37,    7,   14,   -1,
               2,   20,   17,   21,   32,   31,   39,   10,
              21,    5,   37,   49,   11,   41,    2,   60,
               9,   31,   42,   49,   91,  146,   39,   11,
              10,    5,   24,   68,   61,  105,   38,   51,
            -173,  -31,   -2,   36,   71, -135,   56, -169,

            /* knights: KQ */
            -143,  -40,   -1,  -24,  -10,    6,  -20,  -55,
             -31,  -12,   -5,   26,    8,   26,  -27,  -42,
             -38,    9,   -4,   48,   29,   11,    0,   -5,
               4,   51,   36,   44,   33,   31,   16,   19,
              46,    8,   48,   74,   49,   46,   16,   58,
               7,   76,  102,  125,   70,   73,   32,   20,
              31,   21,   23,   79,   40,   78,   44,    3,
            -276,  -84,  -29,   18,   62,  -84,  -96, -152,

            /* knights: QK */
             -86,  -39,  -29,  -37,    1,  -21,  -44, -131,
             -42,    5,    1,    6,    5,   -8,  -16,  -11,
             -14,  -11,   18,   34,   46,  -19,   -5,  -20,
              26,   46,   23,   28,   40,   33,   31,    4,
              21,   33,   90,   90,   44,   66,   34,   43,
             -19,   33,   46,   78,  120,  155,   27,   34,
               9,   53,   59,   69,   37,   83,   72,   50,
            -134, -118,  -24,   51,   23,  -75,   38,  -61,

            /* knights: QQ */
             -16,  -21,   25,  -39,  -11,   -6,  -44, -201,
             -23,   45,    3,   10,   18,   -1,  -16,  -65,
              -1,   12,   16,   34,   34,  -24,   14,  -42,
              14,    0,   55,   50,   12,   32,   23,    8,
              38,   20,   51,   63,   62,   55,   33,   -2,
               7,  -11,   44,  121,   71,   86,   77,   78,
               0,   31,   19,  -22,   48,   58,    8,  -27,
            -256,  -92,   -5,  -96,   51,  -88,  -32, -125,

            /* bishops: KK */
             -16,   36,   -3,   -3,   -3,   14,   15,   -4,
              41,    3,   40,    1,   20,   25,   41,    9,
              -5,   32,   21,   24,   12,   24,   11,   12,
              15,    1,   19,   30,   36,  -12,    5,   26,
              -9,    7,   12,   51,   17,   32,   -5,   -1,
              24,   18,   12,   10,   20,   56,   56,    3,
             -10,   -8,   -7,   19,  -13,    2,  -77,  -45,
             -51,  -80,  -36,  -35,  -33,  -82,   29,  -35,

            /* bishops: KQ */
             -58,   -4,  -11,   -5,   13,   12,   46,    9,
             -28,   -4,   14,    3,   28,   38,   65,   43,
              -3,   20,   22,   34,   28,   49,   32,   46,
               7,   18,   21,   43,   90,   33,   31,   19,
             -12,   23,   48,   71,   54,   38,   28,    1,
              13,   65,   97,   29,   52,   25,   35,   18,
             -16,   30,  -18,  -10,   28,    7,  -15,  -40,
             -71,  -64,  -10,    6,  -50,    8,  -39,  -26,

            /* bishops: QK */
              62,   34,    7,   18,    3,   -9,  -21, -129,
              21,   49,   49,   29,   10,  -11,  -30,   -9,
              50,   54,   51,   40,   23,    3,    0,  -22,
              13,   46,   36,   67,   45,   -8,    0,   19,
               7,   34,   54,  101,   29,   45,    6,  -23,
               0,   28,   42,   28,   65,   71,   26,   32,
               8,   13,   -5,   -5,   10,   34,  -64,  -38,
            -109,  -64,  -59,  -20,   21,  -26,   58,  -61,

            /* bishops: QQ */
             -88,  -50,   19,  -51,  -13,   -3,   29,    4,
              44,   18,    6,   27,   -7,   22,    3,   24,
              12,   32,   20,   19,   35,   35,   32,   11,
              20,   22,   25,   65,   71,   33,   16,   24,
               4,   25,   25,   89,   70,   17,   20,   -6,
             -44,   75,   75,   78,   33,   42,   12,    4,
             -65,  -22,  -13,   14,   27,  -25,    2,  -14,
             -26,  -37,  -35,  -15,    8,  -28,   -1,  -80,

            /* rooks: KK */
             -17,   -7,   -8,   -2,    5,    4,    7,  -16,
             -31,  -28,  -14,  -14,  -17,   -6,   15,  -17,
             -43,  -36,  -34,  -26,  -12,  -12,   15,  -12,
             -36,  -27,  -31,  -16,  -25,  -42,    8,  -34,
              -4,    1,   -4,   24,   -6,    8,   40,   31,
              22,   45,   44,   38,   60,   97,   82,   33,
              12,    9,    8,   51,   15,   90,   85,  124,
              55,   74,   56,   46,   39,   72,  128,  115,

            /* rooks: KQ */
             -21,   18,   -3,  -13,   -4,   -9,   -9,  -35,
             -57,   -5,  -31,  -24,  -27,  -26,    5,  -29,
             -30,  -15,  -58,  -29,  -43,  -37,    4,  -10,
             -52,   -6,  -34,  -15,  -26,  -56,   12,  -26,
               8,   51,  -13,   11,    0,   54,   47,   35,
              51,   96,   58,  105,  104,   95,  105,  101,
              32,   43,   64,   97,   68,   72,  102,  113,
              67,   90,   82,   87,   66,   82,  101,  126,

            /* rooks: QK */
             -22,    3,  -18,  -15,    0,   -8,   10,  -24,
             -15,  -48,  -21,  -12,  -56,  -38,   -9,  -25,
             -22,  -23,  -31,  -47,  -50,  -61,  -13,  -19,
             -13,   14,  -38,  -51,  -57,  -28,  -32,  -17,
              43,   35,   31,    4,    9,   30,   10,   -3,
             101,  106,   86,    4,   76,  108,  101,   46,
              98,  106,   92,   50,   23,   94,   47,   74,
             205,  145,   74,   47,   65,   92,  122,  108,

            /* rooks: QQ */
             -26,  -14,   12,    4,   -5,    3,   -3,  -22,
             -47,  -57,  -43,    0,    3,   -9,  -16,  -14,
             -58,  -39,   -4,  -43,  -57,  -33,  -43,  -20,
              -9,    3,  -31,   -4,  -33,    6,  -35,  -49,
              46,   74,  -28,   48,   52,   26,   35,  -22,
              53,  132,  113,   96,  118,   94,   89,   70,
              57,   60,   91,  102,  178,  126,   52,   39,
              54,   68,   49,   49,   73,  111,  123,   30,

            /* queens: KK */
               2,    5,   12,   18,   31,    7,  -49,  -32,
               1,   10,   12,   20,   16,   39,   21,  -12,
             -11,    1,    9,   -9,    3,    2,   15,    8,
              -3,  -19,  -19,  -22,  -19,  -18,   -1,   -1,
              -7,   -1,  -22,  -27,  -32,  -20,  -18,  -14,
               7,  -10,  -25,  -38,   -7,   56,   -1,    9,
             -28,  -43,  -21,  -23,  -65,   10,   27,   97,
             -46,   17,    4,   -4,    9,   86,  135,   96,

            /* queens: KQ */
               0,   10,    3,   12,   14,   -7,  -34,  -16,
              22,   38,   24,   13,   13,   20,  -22,  -44,
               6,    3,   27,   10,    8,   11,   -3,    2,
              -7,   -1,  -22,   -4,    7,   -4,  -25,  -23,
               4,   -8,  -33,   15,    8,   -3,  -12,   10,
              13,   28,   -3,   20,   25,   32,    4,    0,
              46,   33,   25,   25,    0,    9,   23,   25,
              48,   75,   46,   37,    9,   35,   49,   39,

            /* queens: QK */
             -43,  -67,   -1,   -6,    9,  -21,    8,  -12,
             -63,  -48,   11,   14,   -6,   10,   25,   26,
             -47,  -15,  -12,  -18,  -12,  -25,  -13,   14,
             -21,  -38,  -51,  -27,  -35,  -21,    2,    4,
               5,  -38,  -34,  -32,  -30,   -6,  -31,  -20,
               4,  -19,  -30,  -25,   -5,   59,   30,   30,
              -1,  -22,  -20,  -48,  -67,  -34,    2,   30,
             -49,   39,   14,    7,  -24,   80,  116,  101,

            /* queens: QQ */
             -24, -102,   -2,  -28,   43,   -1,   47,  -23,
             -67,  -38,   -1,   20,  -19,  -10,    2,  -14,
             -10,   12,   -9,  -26,   -6,  -12,  -16,  -15,
             -29,   17,  -47,  -57,   -6,   -6,  -16,  -16,
              29,  -48,  -13,  -34,    4,    6,   30,   18,
              10,   31,   69,   44,    0,   -3,   78,    8,
              24,   27,   20,   38,   22,    7,   11,   11,
              35,   74,   56,   41,   46,   66,   38,   58,

            /* kings: KK */
               2,   58,    7,  -67,  -56,  -65,   -9,  -31,
              32,   23,   12,  -32,  -15,   -4,    5,  -15,
               2,   11,    6,  -33,   43,    3,   -1,  -46,
             -36,   32,  -11,  -20,  130,   92,   26,  -53,
              15,    7,   21,   11,  187,  209,  141,   30,
               4,   59,   33,   -1,  201,  205,  205,   92,
              56,   30,    5,   17,  182,  146,  124,   46,
             -51,   51,   10,  -28,   -5,   45,   94,  124,

            /* kings: KQ */
               2,   58,    7,  -67,  -75,  -85,  -16,  -21,
              32,   23,   12,  -32,  -15,   -2,    0,   -3,
               2,   11,    6,  -33,   23,   22,   20,  -42,
             -36,   32,  -11,  -20,   84,   84,   10,  -17,
              15,    7,   21,   11,  164,  122,   76,   39,
               4,   59,   33,   -1,  126,  104,   96,   28,
              56,   30,    5,   17,  132,  147,  108,   25,
             -51,   51,   10,  -28,    3,   32,  120,   16,

            /* kings: QK */
             -24,   11,  -28,  -80,  -13,  -48,   15,   -8,
              13,   -1,   -2,  -30,  -20,   -9,   22,    0,
             -27,   23,   15,  -12,  -21,  -28,  -14,  -32,
              12,   57,   57,   66,  -18,  -17,  -31,  -78,
              16,   93,   95,  143,   -4,   12,   11,  -38,
              -4,  103,  141,  134,    1,   18,   47,   -9,
             129,  141,  126,  129,   10,   13,  -15,  -16,
             -49,   74,  131,   28,  -65,  -26,   27,   38,

            /* kings: QQ */
             -90,  -16,  -48,  -73,  -13,  -48,   15,   -8,
             -33,  -21,   14,  -26,  -20,   -9,   22,    0,
             -60,   15,   27,   13,  -21,  -28,  -14,  -32,
              14,   70,   63,   74,  -18,  -17,  -31,  -78,
              80,  112,  148,  193,   -4,   12,   11,  -38,
             101,  213,  176,  118,    1,   18,   47,   -9,
              86,   56,  106,  133,   10,   13,  -15,  -16,
             -40,   85,   64,   17,  -65,  -26,   27,   38,

            #endregion

            /* opening mobility weights */

            9, // knights
            5, // bishops
            3, // rooks
            1, // queens

            /* opening squares attacked near enemy king */
            15, // attacks to squares 1 from king
            16, // attacks to squares 2 from king
            6, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
            19, // # friendly pawns 1 from king
            15, // # friendly pawns 2 from king
            9, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -4,

            /* opening backward pawns */
            -21,

            /* opening doubled pawns */
            -22,

            /* opening adjacent/connected pawns */
            10,

            /* UNUSED (was opening king adjacent open file) */
            0,

            /* opening knight on outpost */
            -3,

            /* opening bishop on outpost */
            15,

            /* opening bishop pair */
            34,

            /* opening rook on open file */
            29,

            /* opening rook on half-open file */
            13,

            /* opening rook behind passed pawn */
            -4,

            /* opening doubled rooks on file */
            13,

            /* opening king on open file */
            -46,

            /* opening king on half-open file */
            -18,

            /* opening castling rights available */
            17,

            /* opening castling complete */
            17,

            /* opening center control */
            3, // D0
            3, // D1

            /* opening queen on open file */
            -6,

            /* opening queen on half-open file */
            9,

            /* opening rook on seventh rank */
            10,

            /* opening passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              -7,    4,  -16,  -42,  -21,  -13,    3,   19,
              -8,   -3,  -25,  -29,  -43,  -43,  -36,   -8,
              22,   27,   -7,   -7,  -24,  -26,  -13,   -1,
              70,   64,   38,   32,    6,    8,   24,   27,
             144,  121,   97,   72,   60,   73,   44,   41,
              56,   31,   55,   54,   60,   52,   64,   57,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* opening bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              -4,   -4,   -8,    5,   -7,  -19,  -21,    5,
              -5,   -4,  -10,   -9,  -12,  -11,  -11,  -14,
              -7,   -1,   -2,  -10,  -16,   -4,   -6,    2,
               7,    5,   -4,  -10,  -12,   -5,    1,   -6,
               0,  -10,   -3,  -11,   -6,  -17,  -14,   -1,
              37,    0,  -16,  -19,  -35,   -8,  -18,   13,
               0,    0,    0,    0,    0,    0,    0,    0,


            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game phase material boundary */
            600,

            /* end game piece values */
            90, 300, 320, 550, 995, 0,

            /* end game piece square values */

            #region end game piece square values

            /* pawns: KK */
               0,    0,    0,    0,    0,    0,    0,    0,
              27,   24,   19,  -11,   40,   14,   -5,  -13,
              26,   17,    6,    4,   13,    7,   -7,   -7,
              24,   27,    6,  -12,   -2,    1,    7,   -2,
              56,   34,   21,   -2,   -2,   -1,   11,    6,
              66,   56,   42,   27,   25,   13,   19,    7,
             138,  142,  127,   86,   87,   63,   96,   59,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: KQ */
               0,    0,    0,    0,    0,    0,    0,    0,
              42,   46,   40,   42,   10,    4,  -10,   13,
              34,   30,   19,   17,   -2,   -2,   -9,   -1,
              25,   20,    9,   -1,    4,   18,   15,   18,
              35,    3,    6,    3,   20,   46,   46,   58,
               1,  -18,  -39,   10,   78,  108,  129,  115,
              61,   72,   26,   53,  172,  197,  221,  193,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QK */
               0,    0,    0,    0,    0,    0,    0,    0,
              15,   -2,   12,   -1,   38,   31,   32,   47,
              19,   -1,   10,   -3,   11,   17,   16,   30,
              30,   30,   23,    5,   10,   11,   11,   18,
              75,   66,   56,   31,   14,   -3,   12,   17,
              91,  107,  110,   65,    7,    6,    9,   24,
             175,  210,  221,  151,   79,   16,   50,   47,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* pawns: QQ */
               0,    0,    0,    0,    0,    0,    0,    0,
               3,   -2,    7,  -21,   14,   10,    3,   18,
               1,    0,    4,    8,   -6,   -3,  -13,    6,
               8,   14,    0,   -1,    6,   -6,    2,   12,
               6,   -1,   -2,   10,    1,   13,    7,   26,
             -26,   -3,  -14,   -3,   26,   46,   62,   54,
              41,   89,   42,   51,   93,  144,  132,  139,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights: KK */
             -45,  -73,  -43,  -25,  -34,  -31,  -48,  -72,
             -54,  -10,  -21,  -30,  -16,  -17,  -14,  -25,
             -34,   -8,   -9,   10,    9,  -15,   -7,    8,
              -7,   19,   33,   39,   40,   32,   26,   10,
               8,   11,   16,   31,   42,   34,   39,   15,
              21,   13,   16,   22,   12,   -3,   26,   14,
               2,   19,   13,   26,   31,  -16,   -1,   -1,
              22,   39,   31,   24,   11,   39,   46,  -36,

            /* knights: KQ */
             -51,  -27,  -43,  -11,  -32,  -45,    1,  -96,
             -22,  -16,  -18,  -30,  -28,  -25,    8,  -26,
             -24,  -27,   -5,  -11,   -1,  -29,  -17,   -5,
              -2,   -9,    2,   -4,    7,    6,    6,   -9,
             -24,  -15,   -1,   -9,   -7,    5,   -4,  -37,
             -30,  -39,  -44,  -34,   -8,   -8,   12,   -2,
             -35,  -38,  -35,  -11,   -3,    0,   15,    8,
             -16,   -8,  -20,   -3,  -29,   57,   43,  -17,

            /* knights: QK */
             -90,  -22,  -29,   10,  -33,  -45,  -40,  -41,
             -47,   15,  -13,  -15,  -11,   -7,  -36,  -50,
               9,   -4,  -16,    3,   -7,   -2,  -16,  -35,
             -13,    8,   21,   21,    3,   14,   -3,  -16,
              27,   12,   -2,    6,   15,    0,   -4,  -19,
              39,   31,   24,   -8,  -16,  -23,   -5,   -6,
              -2,   25,    9,   26,   12,  -29,  -37,  -42,
              14,   48,   19,   18,  -10,  -10,    7,  -97,

            /* knights: QQ */
             -56,  -30,  -39,  -22,  -29,  -24,  -57,  -75,
             -25,  -27,  -43,  -36,  -46,  -63,  -53,  -45,
             -26,  -23,  -34,  -29,  -24,  -23,  -30,  -51,
               5,   -5,   -3,  -15,   -4,  -23,  -16,  -36,
             -13,  -12,  -24,  -11,  -17,  -25,  -39,   -1,
               6,    6,  -22,  -40,  -22,  -17,  -29,  -13,
              -9,  -10,  -20,   11,   -4,  -14,    3,    4,
             -18,   44,  -29,    3,  -21,    1,   13, -100,

            /* bishops: KK */
              -7,  -18,  -23,    5,   -2,    1,  -22,  -41,
              -4,  -13,   -4,   13,   -2,  -10,  -23,  -24,
              10,    5,   18,   23,   29,    4,   -2,   -2,
              -5,   19,   25,   18,    5,   22,   13,  -16,
              21,   14,   10,    8,   16,   11,   19,   26,
               5,   14,   11,   12,   20,   18,   10,   36,
              22,   16,   21,   16,   27,   19,   25,    8,
              47,   47,   41,   33,   26,   28,   -4,    5,

            /* bishops: KQ */
              44,   22,    4,   -8,  -17,  -17,  -61,  -71,
               5,    2,   -2,    2,   -9,  -34,  -27,  -37,
               6,   -2,    6,   -5,    7,  -17,   -9,  -36,
               6,   13,   11,  -21,  -17,   13,   -1,   -3,
               8,   -5,  -30,  -34,   -3,    5,   15,    4,
             -12,  -27,  -22,    0,   -1,    1,   11,   12,
             -35,  -13,   -3,    5,   -7,   11,   23,   22,
               2,    4,    6,   -1,    8,   23,   18,   36,

            /* bishops: QK */
             -47,  -33,   -4,  -14,  -11,   -6,   22,   34,
             -26,  -17,   -9,  -11,    0,   -5,    7,   -1,
              -8,    0,    0,   12,   11,    7,   -8,    5,
               2,   10,   13,   -8,    0,   13,   10,  -14,
              22,   17,   -9,    1,    2,  -14,   -2,   19,
              18,   27,   11,   14,   11,   -8,  -13,   -4,
              31,   18,   14,   18,    4,   15,    7,  -14,
              37,   31,   31,   12,    3,   12,    4,    0,

            /* bishops: QQ */
               0,  -12,  -43,  -16,  -40,  -41,  -50,  -48,
             -21,  -28,  -23,  -42,  -22,  -53,  -40,  -56,
             -23,  -19,  -36,  -38,  -34,  -39,  -42,  -27,
             -16,  -21,  -31,  -52,  -37,  -32,  -44,  -40,
             -21,  -20,  -50,  -53,  -48,  -29,  -47,  -17,
             -16,  -40,  -33,  -45,  -23,  -47,  -24,  -38,
             -27,  -17,  -46,  -19,  -21,   -8,  -24,  -34,
             -13,  -25,  -14,  -39,  -11,  -14,  -20,  -14,

            /* rooks: KK */
              33,   19,   21,    4,  -13,    6,    9,    6,
              29,   29,   14,   -1,   -1,   -6,   -4,    1,
              43,   36,   24,    7,   -1,    2,    5,    8,
              47,   45,   38,   19,   17,   41,   23,   23,
              53,   55,   41,   23,   28,   35,   29,   20,
              58,   48,   45,   30,   18,   26,   27,   36,
              48,   53,   41,   16,   27,   32,   39,   15,
              45,   52,   45,   25,   46,   44,   40,   31,

            /* rooks: KQ */
             -14,  -23,  -25,  -23,  -17,  -18,    1,   12,
             -11,  -35,  -18,  -29,  -19,  -11,  -23,    6,
              -6,  -21,   -2,  -30,  -13,   -7,   -6,  -13,
              12,  -13,    4,  -12,   -8,   17,    4,    4,
              -5,   -9,    0,    2,   -8,   -1,    1,    7,
              -1,  -19,   -8,  -35,  -25,   -4,   -5,   -2,
              -4,   -4,  -18,  -14,  -16,  -16,    1,   -8,
              14,   -6,  -10,  -18,   -3,    1,    9,  -11,

            /* rooks: QK */
               5,   -1,   -3,  -24,  -31,  -34,  -28,   -5,
              -8,   12,    4,  -23,  -12,  -27,  -35,  -37,
               0,    8,    1,   -2,  -10,    3,  -30,  -31,
               1,   -4,   14,   10,    4,   -4,   -2,  -13,
              14,   24,    8,    6,   -3,   -1,   -6,  -13,
               7,    6,   11,   19,  -20,  -21,  -11,   -9,
              -5,   -4,  -11,   -3,   12,  -22,    6,   -8,
             -52,   -1,   11,    2,    2,  -12,  -12,   -8,

            /* rooks: QQ */
             -29,  -42,  -62,  -59,  -61,  -37,  -23,  -11,
             -46,  -17,  -27,  -54,  -65,  -55,  -58,  -43,
              -9,  -28,  -29,  -36,  -35,  -40,  -26,  -20,
             -27,  -22,  -18,  -35,  -30,  -39,  -17,  -13,
             -29,  -44,  -17,  -40,  -49,  -31,  -31,  -15,
             -20,  -46,  -35,  -27,  -48,  -25,  -18,  -20,
             -26,  -30,  -18,  -36,  -81,  -49,  -29,  -19,
             -13,  -12,  -17,  -26,  -49,  -35,  -21,   -5,

            /* queens: KK */
              -1,  -25,  -29,  -39,  -79,  -90,  -49,   -8,
               8,    0,   -3,  -30,  -23,  -62,  -74,  -37,
              22,   15,   13,   15,   -3,   17,    7,   -7,
              27,   46,   38,   47,   34,   43,   43,   43,
              52,   44,   56,   70,   72,   82,   96,  109,
              47,   81,   70,   79,   81,   79,  113,  128,
              85,  104,   88,   73,  112,   81,  132,   83,
              73,   56,   68,   74,   77,   61,   66,   58,

            /* queens: KQ */
             -18,  -59,  -71,  -49,  -48,  -57,  -86,  -31,
             -11,  -38,  -60,  -44,  -37,  -52,  -37,  -49,
             -14,  -31,  -73,  -19,  -17,    9,   16,  -21,
              18,  -14,   10,  -16,   -6,   24,   27,   32,
              14,   19,   26,    2,   11,   30,   50,   30,
              26,    2,   15,   19,   21,   41,   40,   59,
               6,   23,   32,   32,   22,   44,   42,   27,
              16,   16,   23,   21,   35,   15,   -2,    6,

            /* queens: QK */
             -51,  -71,  -93,  -68, -100,  -64,  -56,  -59,
             -12,  -51,  -78,  -44,  -51,  -80,  -54,  -45,
             -21,   -5,    7,    2,  -14,  -29,   -9,  -18,
             -16,   32,   32,   23,    8,    1,  -22,   -4,
               9,   39,   36,   29,   25,    9,   21,  -24,
             -11,   51,   75,   45,    5,   -2,    0,   -5,
              13,   60,   66,   55,   41,   13,   48,    9,
              28,   19,   42,   13,   19,   23,   27,    1,

            /* queens: QQ */
             -58,  -57,  -37,  -47, -101,  -93, -121,  -67,
             -41,  -37,  -33,  -75,  -37,  -54,  -68,  -85,
             -17,  -21,  -44,   -5,  -51,  -29,  -21,  -38,
             -13,  -24,    2,   -3,  -47,  -37,  -17,  -14,
             -11,   36,   -8,   11,  -21,  -40,    7,  -35,
              45,   43,   13,   12,   14,   -6,   -5,    7,
              22,   34,   28,   41,   12,   30,    0,  -36,
             -45,   12,   17,  -22,  -34,  -61,  -31,  -50,

            /* kings: KK */
              -1,  -14,  -21,  -21,  -16,   -2,  -18,  -38,
              -3,   -2,   -8,  -10,   -1,   -3,   -7,  -14,
              -6,    6,    4,    7,    6,    6,   -1,   -9,
               0,   18,   22,   20,    9,    6,    8,    3,
              19,   34,   34,   20,    2,   -5,    4,   -6,
              29,   55,   45,   39,    1,   -1,   22,   12,
              28,   61,   51,   47,    9,    0,   31,   14,
             -27,   37,   51,   46,   11,   20,   29,  -56,

            /* kings: KQ */
              -1,  -14,  -21,  -21,    7,    2,  -19,  -25,
              -3,   -2,   -8,  -10,    7,   -1,    5,  -12,
              -6,    6,    4,    7,   20,   10,    2,    3,
               0,   18,   22,   20,   24,   28,   35,   12,
              19,   34,   34,   20,   27,   42,   45,   34,
              29,   55,   45,   39,   37,   46,   48,   50,
              28,   61,   51,   47,   15,   17,   30,   21,
             -27,   37,   51,   46,   -6,   12,   -8,  -44,

            /* kings: QK */
             -24,  -47,  -34,  -19,  -43,  -31,  -30,  -51,
             -34,   -9,  -20,  -14,  -13,  -13,  -15,  -24,
             -17,   -1,   -6,    2,    5,   -2,  -11,  -26,
             -11,    9,   14,   12,   18,   11,    9,   -9,
              16,   24,   22,   10,   19,   28,   27,   13,
              28,   39,   27,   23,   29,   46,   62,   27,
             -15,   12,   26,   22,   48,   60,   74,   36,
               9,   10,   -5,   23,   49,   59,   57,  -35,

            /* kings: QQ */
              70,   14,   -2,  -19,  -43,  -31,  -30,  -51,
               9,    1,  -14,  -13,  -13,  -13,  -15,  -24,
               3,    3,   -3,   -2,    5,   -2,  -11,  -26,
              -4,   13,    9,   10,   18,   11,    9,   -9,
              10,   14,   12,    7,   19,   28,   27,   13,
              17,   24,   14,   18,   29,   46,   62,   27,
              -2,   42,   16,   13,   48,   60,   74,   36,
             -47,   33,   10,   18,   49,   59,   57,  -35,

            #endregion

            /* end game mobility weights */

            2, // knights
            3, // bishops
            2, // rooks
            -1, // queens

            /* end game squares attacked near enemy king */
            -2, // attacks to squares 1 from king
            -2, // attacks to squares 2 from king
            0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            15, // # friendly pawns 1 from king
            16, // # friendly pawns 2 from king
            11, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -9,

            /* end game backward pawns */
            -6,

            /* end game doubled pawns */
            -16,

            /* end game adjacent/connected pawns */
            6,

            /* UNUSED (was end game king adjacent open file) */
            0,

            /* end game knight on outpost */
            22,

            /* end game bishop on outpost */
            17,

            /* end game bishop pair */
            76,

            /* end game rook on open file */
            1,

            /* end game rook on half-open file */
            16,

            /* end game rook behind passed pawn */
            16,

            /* end game doubled rooks on file */
            4,

            /* end game king on open file */
            1,

            /* end game king on half-open file */
            27,

            /* end game castling rights available */
            -9,

            /* end game castling complete */
            -7,

            /* end game center control */
            5, // D0
            2, // D1

            /* end game queen on open file */
            27,

            /* end game queen on half-open file */
            19,

            /* end game rook on seventh rank */
            21,

            /* end game passed pawn */
               0,    0,    0,    0,    0,    0,    0,    0,
              17,   22,   30,   28,   19,   19,   44,   12,
              18,   26,   19,   11,   25,   23,   47,   24,
              50,   45,   37,   31,   28,   37,   55,   49,
              71,   73,   57,   44,   49,   54,   69,   65,
             101,  107,   89,   73,   67,   69,   77,   76,
              43,   44,   49,   48,   43,   48,   45,   46,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* end game bad bishop pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
               5,    1,   -4,  -11,  -13,   -5,   -5,    1,
              -7,  -10,   -7,  -20,  -15,   -3,   -7,   -7,
              -6,  -23,  -28,  -36,  -25,  -19,  -18,   -6,
             -29,  -30,  -33,  -39,  -33,  -21,  -21,  -27,
             -30,  -44,  -56,  -53,  -55,  -49,  -62,  -26,
             -36,  -45,  -58,  -69,  -72,  -64,  -67,  -62,
               0,    0,    0,    0,    0,    0,    0,    0,
        };
    }
}