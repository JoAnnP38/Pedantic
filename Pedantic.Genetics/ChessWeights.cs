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
        public const int MAX_WEIGHTS = 836;
        public const int ENDGAME_WEIGHTS = 418;
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
        public const int KING_ON_OPEN_FILE = 413;
        public const int CASTLING_AVAILABLE = 414;
        public const int CASTLING_COMPLETE = 415;
        public const int CENTER_CONTROL = 416;

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
            7200,

            /* opening piece values */
            80, 330, 360, 445, 1085, 0,

            /* opening piece square values */

            #region opening piece square values */

            /* pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
             -27,  -29,  -25,  -15,  -28,   12,    8,  -25,
             -28,  -13,  -24,  -20,  -13,    3,   20,  -15,
             -22,   -3,    2,    5,    9,   24,   20,  -12,
              -5,   20,   25,   34,   56,   58,   48,    2,
              40,   55,   86,   93,  109,  110,   80,    9,
             150,  169,  133,  161,  114,  119,    8,  -16,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights */
            -106,  -17,  -41,  -15,    0,    0,  -16,  -54,
             -41,  -38,   -7,   13,    8,   10,    5,  -11,
             -32,   -9,   -9,   24,   38,    1,   12,  -12,
              -4,    6,   17,   22,   30,   32,   46,    9,
              13,    7,   41,   57,   19,   44,    0,   54,
              -2,   43,   49,   69,  106,  132,   69,   26,
             -24,    9,   41,   68,   53,   93,   29,    8,
            -199,  -74,  -18,  -20,   72,  -88,    8,  -96,

            /* bishops */
             -23,   27,   -9,  -19,   -8,    3,    4,  -20,
              31,    2,   23,   -3,    7,   24,   37,   13,
              -9,   15,   11,   11,    9,   12,    8,    8,
              -4,    2,    2,   39,   37,  -19,   -3,   23,
              -8,   -3,   15,   58,   24,   25,   -6,  -20,
               6,   33,   17,   31,   27,   56,   36,   -1,
             -13,    8,   -9,    1,    4,    7,  -30,  -34,
             -33,  -29,  -63,  -43,  -44,  -42,   15,   -6,

            /* rooks */
             -19,  -10,   -6,   -1,    4,    2,    6,  -24,
             -42,  -31,  -16,  -15,  -23,  -12,    7,  -32,
             -41,  -40,  -31,  -31,  -17,  -19,    6,  -13,
             -40,  -28,  -29,  -12,  -20,  -41,   11,  -41,
              -2,   11,    1,   28,   10,   18,   32,   13,
              21,   42,   52,   51,   68,   87,   87,   60,
              11,   11,   28,   71,   48,   98,   40,   74,
              55,   62,   66,   64,   45,   49,   79,   85,

            /* queens */
             -13,   -3,    5,    8,   22,  -16,  -46,  -24,
             -15,    4,    9,   14,   13,   25,   20,   -1,
             -19,   -6,    5,   -9,    1,   -1,    2,    1,
             -13,  -18,  -17,  -12,  -13,  -21,   -8,  -12,
             -14,   -9,  -19,  -13,  -17,  -19,  -21,  -26,
              -5,   -9,  -15,  -22,    2,   50,   23,    9,
             -15,  -44,  -21,  -23,  -37,    9,    1,   58,
             -19,   26,    8,    0,   25,   55,   62,   76,

            /* kings */
               2,   58,    7,  -67,  -13,  -48,   15,   -8,
              32,   23,   12,  -32,  -20,   -9,   22,    0,
               2,   11,    6,  -33,  -21,  -28,  -14,  -32,
             -36,   32,  -11,  -20,  -18,  -17,  -31,  -78,
              15,    7,   21,   11,   -4,   12,   11,  -38,
               4,   59,   33,   -1,    1,   18,   47,   -9,
              56,   30,    5,   17,   10,   13,  -15,  -16,
             -51,   51,   10,  -28,  -65,  -26,   27,   38,

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
            33, // # friendly pawns 1 from king
            21, // # friendly pawns 2 from king
            11, // # friendly pawns 3 from king

            /* opening isolated pawns */
            -1,

            /* opening backward pawns */
            -24,

            /* opening doubled pawns */
            -16,

            /* opening adjacent/connected pawns */
            10,

            /* opening passed pawns */
            3,

            /* opening knight on outpost */
            -8,

            /* opening bishop on outpost */
            10,

            /* opening bishop pair */
            23,

            /* opening rook on open file */
            26,

            /* opening rook on half-open file */
            9,

            /* opening rook behind passed pawn */
            7,

            /* opening doubled rooks on file */
            16,

            /* opening king on open file */
            -23,

            /* opening castling rights available */
            12,

            /* opening castling complete */
            14,

            /* opening center control */
            2, // D0
            1, // D1

            /*------------------- END GAME WEIGHTS -------------------*/

            /* end game phase material boundary */
            2100,

            /* end game piece values */
            100, 330, 330, 595, 1100, 0,

            /* end game piece square values */

            #region end game piece square values */

            /* pawns */
               0,    0,    0,    0,    0,    0,    0,    0,
              21,   15,    9,   -2,   18,   17,    1,   -3,
              14,    8,    1,   -2,   -2,    3,   -6,   -2,
              20,   15,   -1,  -17,  -11,    5,    3,    2,
              49,   33,   22,    5,    5,   18,   23,   19,
             100,   91,   82,   63,   56,   63,   80,   66,
             155,  154,  161,  131,  129,  118,  129,  122,
               0,    0,    0,    0,    0,    0,    0,    0,

            /* knights */
             -59,  -64,  -37,  -31,  -37,  -36,  -52,  -74,
             -50,  -15,  -29,  -21,  -18,  -23,  -30,  -37,
             -37,  -21,  -14,    5,    5,  -16,  -11,  -15,
             -11,   12,   23,   23,   27,   19,   13,   -2,
               8,    6,   13,   27,   25,   24,   18,   11,
               5,    7,   16,   19,   12,   13,   15,    6,
               4,   13,    5,   30,   26,   16,   12,   -3,
             -59,   16,   15,   18,    1,    7,    1,  -81,

            /* bishops */
             -13,  -13,  -24,  -10,  -18,   -8,  -12,  -23,
             -12,  -17,   -4,   -7,   -5,  -15,  -12,  -36,
              -5,    4,    5,    8,   11,    1,   -7,   -3,
              -3,    1,   13,   -1,    3,    6,    0,  -20,
               5,    8,   -4,    4,    0,    8,    7,   13,
               8,    9,    6,    0,   11,    4,   16,   15,
               3,   15,    2,   10,   -2,    6,   -1,   -3,
              16,    9,   17,    9,    8,    0,   -1,   -9,

            /* rooks */
             -11,  -14,  -12,  -18,  -27,  -17,  -17,  -14,
             -13,  -16,  -14,  -20,  -23,  -28,  -26,  -19,
              -8,   -2,  -10,  -16,  -20,  -17,  -14,  -19,
               7,    8,    9,    0,   -4,    7,    8,    2,
              25,   28,   26,   18,   15,   21,   25,   17,
              33,   37,   32,   27,   22,   28,   30,   26,
              34,   39,   34,   25,   24,   21,   30,   29,
              24,   29,   24,   18,   23,   24,   29,   24,

            /* queens */
             -15,  -27,  -31,  -30,  -54,  -58,  -43,  -23,
               0,  -16,  -18,  -22,  -32,  -41,  -53,  -38,
               6,   -1,    1,    0,   -7,    2,    9,  -12,
               5,   25,   15,   26,   14,   20,   14,   28,
              16,   27,   36,   36,   36,   42,   52,   48,
              19,   41,   42,   46,   49,   55,   47,   50,
              26,   43,   46,   43,   54,   48,   49,   35,
              29,   30,   33,   34,   36,   49,   47,   36,

            /* kings */
              -1,  -14,  -21,  -21,  -43,  -31,  -30,  -51,
              -3,   -2,   -8,  -10,  -13,  -13,  -15,  -24,
              -6,    6,    4,    7,    5,   -2,  -11,  -26,
               0,   18,   22,   20,   18,   11,    9,   -9,
              19,   34,   34,   20,   19,   28,   27,   13,
              29,   55,   45,   39,   29,   46,   62,   27,
              28,   61,   51,   47,   48,   60,   74,   36,
             -27,   37,   51,   46,   49,   59,   57,  -35,

            #endregion

            /* end game mobility weights */

            5, // knights
            6, // bishops
            3, // rooks
            3, // queens

            /* end game squares attacked near enemy king */
            1, // attacks to squares 1 from king
            0, // attacks to squares 2 from king
            0, // attacks to squares 3 from king

            /* end game pawn shield/king safety */
            11, // # friendly pawns 1 from king
            15, // # friendly pawns 2 from king
            9, // # friendly pawns 3 from king

            /* end game isolated pawns */
            -5,

            /* end game backward pawns */
            -13,

            /* end game doubled pawns */
            -18,

            /* end game adjacent/connected pawns */
            10,

            /* end game passed pawns */
            33,

            /* end game knight on outpost */
            23,

            /* end game bishop on outpost */
            9,

            /* end game bishop pair */
            70,

            /* end game rook on open file */
            7,

            /* end game rook on half-open file */
            14,

            /* end game rook behind passed pawn */
            19,

            /* end game doubled rooks on file */
            5,

            /* end game king on open file */
            -18,

            /* end game castling rights available */
            -4,

            /* end game castling complete */
            -4,

            /* end game center control */
            3, // D0
            2 // D1
        };
    }
}