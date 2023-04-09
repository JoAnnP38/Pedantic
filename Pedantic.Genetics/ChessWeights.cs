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
        public const int MAX_WEIGHTS = 826;
        public const int ENDGAME_WEIGHTS = 413;
        public const int PIECE_WEIGHT_LENGTH = 6;
        public const int PIECE_SQUARE_LENGTH = 384;
        public const int GAME_PHASE_MATERIAL = 0;
        public const int PIECE_VALUES = 1;
        public const int PIECE_SQUARE_TABLE = 7;
        public const int PIECE_MOBILITY = 391;
        public const int KING_ATTACK = 395;
        public const int PAWN_SHIELD = 398;
        public const int ISOLATED_PAWN = 401;
        public const int BACKWARD_PAWN = 402;
        public const int DOUBLED_PAWN = 403;
        public const int CONNECTED_PAWN = 404;
        public const int PASSED_PAWN = 405;
        public const int KNIGHT_OUTPOST = 406;
        public const int BISHOP_OUTPOST = 407;
        public const int BISHOP_PAIR = 408;
        public const int ROOK_ON_OPEN_FILE = 409;
        public const int ROOK_ON_HALF_OPEN_FILE = 410;
        public const int ROOK_BEHIND_PASSED_PAWN = 411;
        public const int DOUBLED_ROOKS_ON_FILE = 412;

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

        private static readonly short[] paragonWeights =
        {
            /*------------------- OPENING WEIGHTS -------------------*/

            /* opening phase material boundary */
            7400,

            /* opening piece values */
            85, 330, 355, 430, 1065, 0,

            /* opening piece square values */

            #region opening piece square values */

            /* pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
             -28,  -27,  -23,  -19,  -25,   15,   12,  -25,
             -28,  -16,  -20,  -21,  -12,    6,   21,  -14,
             -23,   -2,    2,    5,    9,   24,   23,  -17,
              -6,   20,   24,   33,   56,   52,   44,   -2,
              32,   46,   73,   86,  102,  105,   71,    7,
             137,  158,  120,  148,  105,  115,    2,  -22,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights */
            -104,  -20,  -43,  -19,    1,   -6,  -17,  -53,
             -44,  -38,   -9,   11,    6,    9,    1,   -2,
             -30,  -10,   -7,   21,   34,    5,   13,  -12,
              -4,    2,   16,   23,   32,   31,   47,    5,
               7,    4,   40,   54,   21,   45,    2,   50,
              -6,   45,   47,   71,  110,  134,   75,   29,
             -30,    5,   46,   63,   54,   96,   27,    0,
            -193,  -76,  -18,  -28,   71,  -86,    7,  -96,

            /* bishops */
             -22,   27,   -8,  -18,  -10,    2,    0,  -19,
              32,    5,   23,   -5,    9,   21,   35,   15,
              -5,   17,   10,   10,   10,   12,    7,    6,
              -7,    4,    4,   42,   38,  -15,   -3,   24,
              -8,   -1,   19,   62,   27,   22,   -8,  -18,
               6,   37,   18,   37,   26,   50,   34,   -2,
             -12,    7,   -9,   -3,   10,   17,  -18,  -33,
             -32,  -28,  -65,  -46,  -46,  -43,   14,   -2,

            /* rooks */
             -20,  -11,   -6,   -2,    5,   -2,   -2,  -26,
             -41,  -28,  -19,  -13,  -22,  -14,    6,  -38,
             -42,  -35,  -30,  -28,  -13,  -15,    6,  -17,
             -42,  -24,  -28,   -5,  -12,  -38,   17,  -37,
              -3,    8,    3,   32,   17,   22,   25,    4,
              18,   35,   51,   52,   63,   81,   85,   50,
              12,   17,   27,   72,   55,   88,   38,   64,
              48,   56,   63,   66,   51,   44,   70,   80,

            /* queens */
             -12,   -2,    5,    6,   17,  -19,  -49,  -23,
             -19,    6,   11,   13,   10,   20,   20,   -1,
             -18,   -4,    9,   -7,    6,    2,    1,   -2,
             -11,  -23,  -13,   -8,   -3,  -11,   -9,  -17,
             -18,   -9,  -12,   -8,  -10,   -9,  -20,  -25,
              -6,  -11,  -12,  -20,    6,   56,   29,    9,
             -11,  -41,  -17,  -25,  -34,   12,   -2,   56,
             -20,   15,   10,    0,   23,   52,   61,   71,

            /* kings */
               7,   59,    9,  -71,  -10,  -48,   17,   -7,
              27,   22,   12,  -43,  -24,   -8,   23,   -2,
               1,   14,    0,  -38,  -26,  -33,  -19,  -30,
             -36,   27,  -17,  -29,  -27,  -20,  -27,  -75,
              11,    2,   14,    1,  -12,    5,    7,  -34,
               3,   51,   24,  -10,  -10,   14,   40,  -10,
              50,   26,    0,   12,    5,    5,  -21,  -22,
             -53,   47,   14,  -26,  -60,  -31,   20,   29,

            #endregion

            /* opening mobility weights */

            8, // knights
            5, // bishops
            3, // rooks
            0, // queens

            /* opening squares attacked near enemy king */
            14, // attacks to squares 1 from king
            14, // attacks to squares 2 from king
            6, // attacks to squares 3 from king

            /* opening pawn shield/king safety */
            30, // # friendly pawns 1 from king
            19, // # friendly pawns 2 from king
            11, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -1,

            /* opening backward pawns */
            -24,

            /* opening doubled pawns */
            -12,

            /* opening adjacent/connected pawns */
            10,

            /* opening passed pawns */
            4,

            /* opening knight on outpost */
            -6,

            /* opening bishop on outpost */
            5,

            /* opening bishop pair */
            22,

            /* opening rook on open file */
            24,

            /* opening rook on half-open file */
            9,

            /* opening rook behind passed pawn */
            4,

            /* opening doubled rooks on file */
            14,

            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game phase material boundary */
            2200,

            /* end game piece values */
            100, 335, 335, 590, 1075, 0,

            /* end game piece square values */

            #region end game piece square values */

            /* pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              17,   15,    7,    5,   21,   17,    1,   -2,
              12,    7,   -1,   -6,   -1,    3,   -7,   -2,
              19,   16,   -5,  -16,   -8,    4,    3,    2,
              45,   37,   25,    5,    7,   17,   22,   20,
              94,   92,   80,   63,   50,   58,   77,   68,
             156,  160,  161,  138,  125,  119,  128,  127,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights */
             -55,  -63,  -43,  -30,  -37,  -36,  -49,  -71,
             -51,  -20,  -28,  -23,  -19,  -22,  -28,  -35,
             -38,  -20,  -12,    8,    5,  -19,  -14,  -18,
             -14,   13,   19,   26,   25,   19,   11,   -3,
               5,    6,   15,   30,   24,   21,   13,   12,
               5,    0,   13,   15,   12,    8,   14,    5,
               2,    8,    5,   31,   21,   11,    4,  -11,
             -63,   10,   12,   13,   -6,    8,   -9,  -84,

            /* bishops */
             -11,  -11,  -24,   -8,  -16,   -6,  -10,  -15,
             -10,  -15,   -2,   -7,   -3,  -14,   -9,  -37,
             -10,    3,    5,    8,   12,    2,   -6,   -3,
              -7,    5,   14,    4,    8,    8,   -1,  -22,
               5,    9,   -4,    9,    8,   10,    6,   13,
               3,   10,    9,    4,    6,    1,   14,    9,
              -1,   10,    6,    9,    2,    5,    2,   -4,
              11,    7,   15,    0,    3,   -1,    2,  -10,

            /* rooks */
             -13,  -18,  -10,  -17,  -24,  -17,  -17,  -17,
             -16,  -17,  -11,  -20,  -22,  -22,  -25,  -18,
              -6,   -3,   -7,   -9,  -17,  -15,  -10,  -19,
               8,   10,   14,    3,   -2,   14,    6,    4,
              26,   28,   31,   21,   20,   23,   24,   17,
              30,   35,   35,   27,   27,   31,   30,   29,
              25,   30,   35,   23,   25,   17,   19,   21,
              18,   24,   22,   17,   21,   23,   20,   17,

            /* queens */
             -19,  -26,  -33,  -31,  -49,  -55,  -45,  -20,
               0,  -21,  -14,  -17,  -27,  -35,  -50,  -38,
              -2,   -3,    4,    1,   -6,    5,    8,  -13,
               2,   23,   20,   29,   19,   23,   19,   22,
              10,   27,   34,   40,   40,   42,   48,   39,
              15,   41,   38,   49,   47,   57,   44,   41,
              16,   36,   42,   41,   45,   37,   41,   29,
              19,   25,   33,   34,   30,   45,   40,   32,

            /* kings */
              -3,  -15,  -21,  -21,  -42,  -32,  -30,  -52,
              -2,   -3,   -7,  -12,  -15,  -16,  -15,  -22,
              -7,    9,    2,    5,    3,   -1,   -9,  -23,
              -3,   18,   22,   18,   15,   16,    9,  -10,
              20,   39,   36,   20,   22,   31,   28,   15,
              29,   57,   42,   40,   28,   46,   62,   28,
              26,   68,   49,   52,   41,   63,   83,   36,
             -30,   26,   42,   37,   40,   56,   60,  -38,

            #endregion

            /* end game mobility weights */

            5, // knights
            6, // bishops
            3, // rooks
            4, // queens

            /* end game squares attacked near enemy king */
            1, // attacks to squares 1 from king
            1, // attacks to squares 2 from king
            0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            12, // # friendly pawns 1 from king
            15, // # friendly pawns 2 from king
            7, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -6,

            /* end game backward pawns */
            -12,

            /* end game doubled pawns */
            -22,

            /* end game adjacent/connected pawns */
            8,

            /* end game passed pawns */
            29,

            /* end game knight on outpost */
            24,

            /* end game bishop on outpost */
            11,

            /* end game bishop pair */
            69,

            /* end game rook on open file */
            8,

            /* end game rook on half-open file */
            14,

            /* end game rook behind passed pawn */
            21,

            /* end game doubled rooks on file */
            9
        };
    }
}