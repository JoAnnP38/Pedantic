using System.Runtime.CompilerServices;
using System.Text;

using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class HceWeights
    {
        public static readonly Guid HCE_WEIGHTS_VERSION = new("da5e310e-b0dc-4c77-902c-5a46cc81bb73");

        #region Weight Offset Constants

        public const int MAX_WEIGHTS = 1989;
        public const int PIECE_VALUES = 0;
        public const int PIECE_SQUARE_TABLE = 6;
        public const int PIECE_MOBILITY = 1542;
        public const int CENTER_CONTROL = 1546;

        /* king safety/evaluation */
        public const int KING_ATTACK = 1548;
        public const int PAWN_SHIELD = 1551;
        public const int CASTLING_AVAILABLE = 1554;
        public const int CASTLING_COMPLETE = 1555;
        public const int KING_ON_OPEN_FILE = 1556;
        public const int KING_ON_HALF_OPEN_FILE = 1557;
        public const int KING_ON_OPEN_DIAGONAL = 1558;

        /* cached pawn positions */
        public const int ISOLATED_PAWN = 1559;
        public const int DOUBLED_PAWN = 1560;
        public const int BACKWARD_PAWN = 1561;
        public const int PHALANX_PAWN = 1562;
        public const int PASSED_PAWN = 1626;
        public const int PAWN_RAM = 1690;
        public const int CHAINED_PAWN = 1754;

        /* non-cached pawn positions */
        public const int PP_CAN_ADVANCE = 1818;
        public const int KING_OUTSIDE_PP_SQUARE = 1822;
        public const int PP_FRIENDLY_KING_DISTANCE = 1823;
        public const int PP_ENEMY_KING_DISTANCE = 1824;
        public const int BLOCK_PASSED_PAWN = 1825;
        public const int ROOK_BEHIND_PASSED_PAWN = 1873;

        /* piece evaluation */
        public const int KNIGHT_OUTPOST = 1874;
        public const int BISHOP_OUTPOST = 1875;
        public const int BISHOP_PAIR = 1876;
        public const int BAD_BISHOP_PAWN = 1877;
        public const int ROOK_ON_OPEN_FILE = 1941;
        public const int ROOK_ON_HALF_OPEN_FILE = 1942;
        public const int ROOK_ON_7TH_RANK = 1943;
        public const int DOUBLED_ROOKS_ON_FILE = 1944;
        public const int QUEEN_ON_OPEN_FILE = 1945;
        public const int QUEEN_ON_HALF_OPEN_FILE = 1946;

        /* threats */
        public const int PAWN_PUSH_THREAT = 1947;
        public const int PIECE_THREAT = 1953;

        #endregion

        #region Constructors

        public HceWeights(bool resetWeights = false)
        { 
            weights = new Score[MAX_WEIGHTS];

            if (resetWeights)
            {
                // when resetting the weights (for tuning) everything is set
                // to zero except for piece values
                for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
                {
                    short pcValue = CanonicalPieceValues[pc];
                    weights[PIECE_VALUES + pc] = S(pcValue, pcValue);
                }
            }
            else
            {
                Array.Copy(defaultWeights, weights, MAX_WEIGHTS);
            }
        }

        public HceWeights(string weightsPath)
        { 
            weights = new Score[MAX_WEIGHTS];
            Load(weightsPath);
        }

        #endregion

        #region File I/O

        public void Load(string weightsPath)
        {
            try
            {
                using Stream input = File.OpenRead(weightsPath);
                using BinaryReader reader = new(input, Encoding.UTF8);
                byte[] guidBytes = reader.ReadBytes(16);
                Guid fileGuid = new(guidBytes);
                if (fileGuid != HCE_WEIGHTS_VERSION)
                {
                    throw new Exception("Incorrect file version.");
                }
                int length = reader.ReadInt32();
                if (length != MAX_WEIGHTS)
                {
                    throw new Exception("Incorrect file length.");
                }

                for (int n = 0; n < length; n++)
                {
                    weights[n] = (Score)reader.ReadInt32();
                }
            }
            catch (Exception ex)
            {
                throw new FileLoadException("Could not load the Pedantic weights file.", ex);
            }
        }

        public void Save(string weightsPath)
        {
            try
            {
                using Stream output = File.OpenWrite(weightsPath);
                using BinaryWriter writer = new(output, Encoding.UTF8);
                writer.Write(HCE_WEIGHTS_VERSION.ToByteArray());
                writer.Write(MAX_WEIGHTS);
                for (int n = 0; n < MAX_WEIGHTS; n++)
                {
                    writer.Write(weights[n]);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Exception occurred while saving Pedantic weights.", ex);
            }
        }

        #endregion

        #region Accessors

        public Score[] Weights => weights;
        public int Length => weights.Length;

        public Score this[int i]
        {
            get
            {
                return weights[i];
            }
            set
            {
                weights[i] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PieceValue(Piece piece)
        {
            return weights[PIECE_VALUES + (int)piece];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PieceSquareValue(Piece piece, KingPlacement kp, int square)
        {
            int offset = ((((int)piece << 2) + (int)kp) << 6) + square;
            return weights[PIECE_SQUARE_TABLE + offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PieceMobility(Piece piece)
        {
            Util.Assert(piece >= Piece.Knight);
            return weights[PIECE_MOBILITY + (int)piece - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score CenterControl(int dist)
        {
            return weights[CENTER_CONTROL + dist];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score KingAttack(int distance)
        {
            Util.Assert(distance >= 0 && distance < 3);
            return weights[KING_ATTACK + distance];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PawnShield(int distance)
        {
            Util.Assert(distance >= 0 && distance < 3);
            return weights[PAWN_SHIELD + distance];
        }

        public Score CastlingAvailable => weights[CASTLING_AVAILABLE];
        public Score CastlingComplete => weights[CASTLING_COMPLETE];
        public Score KingOnOpenFile => weights[KING_ON_OPEN_FILE];
        public Score KingOnHalfOpenFile => weights[KING_ON_HALF_OPEN_FILE];
        public Score KingOnOpenDiagonal => weights[KING_ON_OPEN_DIAGONAL];
        public Score IsolatedPawn => weights[ISOLATED_PAWN];
        public Score DoubledPawn => weights[DOUBLED_PAWN];
        public Score BackwardPawn => weights[BACKWARD_PAWN];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PhalanxPawns(int square)
        {
            return weights[PHALANX_PAWN + square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PassedPawn(int square)
        {
            return weights[PASSED_PAWN + square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PawnRam(int square)
        {
            return weights[PAWN_RAM + square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score ChainedPawn(int square)
        {
            return weights[CHAINED_PAWN + square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PassedPawnCanAdvance(int rank)
        {
            Util.Assert(rank >= Coord.RANK_4);
            int index = rank - Coord.RANK_4;
            return weights[PP_CAN_ADVANCE + index];
        }

        public Score KingOutsidePasserSquare => weights[KING_OUTSIDE_PP_SQUARE];
        public Score PasserFriendlyKingDistance => weights[PP_FRIENDLY_KING_DISTANCE];
        public Score PasserEnemyKingDistance => weights[PP_ENEMY_KING_DISTANCE];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score BlockedPassedPawn(Piece blocker, int rank)
        {
            int index = (int)blocker * Constants.MAX_COORDS + rank;
            return weights[BLOCK_PASSED_PAWN + index];
        }

        public Score KnightOutpost => weights[KNIGHT_OUTPOST];
        public Score BishopOutpost => weights[BISHOP_OUTPOST];
        public Score BishopPair => weights[BISHOP_PAIR];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score BadBishopPawn(int square)
        {
            return weights[BAD_BISHOP_PAWN + square];
        }

        public Score RookOnOpenFile => weights[ROOK_ON_OPEN_FILE];
        public Score RookOnHalfOpenFile => weights[ROOK_ON_HALF_OPEN_FILE];
        public Score RookBehindPassedPawn => weights[ROOK_BEHIND_PASSED_PAWN];
        public Score RookOn7thRank => weights[ROOK_ON_7TH_RANK];
        public Score DoubleRooksOnFile => weights[DOUBLED_ROOKS_ON_FILE];
        public Score QueenOnOpenFile => weights[QUEEN_ON_OPEN_FILE];
        public Score QueenOnHalfOpenFile => weights[QUEEN_ON_HALF_OPEN_FILE];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PawnPushThreat(Piece defender)
        {
            return weights[PAWN_PUSH_THREAT + (int)defender];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PieceThreat(Piece attacker, Piece defender)
        {
            int index = (int)attacker * Constants.MAX_PIECES + (int)defender;
            return weights[PIECE_THREAT + index];
        }

        #endregion

        private readonly Score[] weights;
        private static Score S(short mg, short eg) => new(mg, eg);

        public static readonly short[] CanonicalPieceValues = { 100, 300, 300, 500, 900, 0 };

        // Solution sample size: 12000000, generated on Sun, 08 Oct 2023 01:28:14 GMT
        // Solution error: 0.117996, accuracy: 0.5221
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(123, 151),   S(500, 450),   S(550, 495),   S(690, 875),   S(1576, 1542), S(  0,   0),

            /* piece square values */
            #region piece square values

            /* pawns: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-62,  14),   S(-57,  19),   S(-40,   2),   S(-18, -17),   S(-47,  40),   S( 56,  -4),   S( 63, -28),   S( -9, -40),
            S(-66,   8),   S(-51,  -1),   S(-60, -29),   S(-68, -17),   S(-43, -24),   S(-21, -25),   S( 13, -25),   S(-52, -19),
            S(-35,  13),   S(-36,  12),   S(-20, -36),   S(-35, -48),   S(-16, -34),   S( 13, -39),   S(  0, -10),   S( -6, -29),
            S(-17,  61),   S(-31,  36),   S( -2,  15),   S( 22,   2),   S( 34, -21),   S( 18, -15),   S( 41,  -1),   S( 56, -14),
            S( 52,  78),   S( -1,  88),   S( -3,  75),   S( 71,  60),   S(119,  64),   S(190,  22),   S(189,  28),   S(181,  12),
            S( 94,  77),   S(106,  75),   S( 88,  77),   S(123,  32),   S( 92,  52),   S(148, -16),   S( 63,  43),   S( 88,  -9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-96,  27),   S(-97,  44),   S(-65,  20),   S(-71,  58),   S(-41,  -9),   S( 52, -34),   S( 66, -39),   S(-12, -18),
            S(-88,   3),   S(-86,   9),   S(-75, -28),   S(-79, -28),   S(-43, -48),   S(-27, -38),   S( 20, -36),   S(-50, -10),
            S(-65,  20),   S(-40,   8),   S(-34, -23),   S(-37, -39),   S(-24, -33),   S( -9, -33),   S( -4, -18),   S(  3, -23),
            S(-51,  67),   S(-12,   7),   S( -1,   0),   S( 16,  16),   S(  4,  -4),   S(-29,  29),   S( 20,  20),   S( 55,   8),
            S(105,  43),   S( 94,   8),   S(131,  -5),   S(104,  45),   S(120,  94),   S( 70, 105),   S( 68, 110),   S(114,  99),
            S(159, -10),   S( 78,  31),   S(113,  -3),   S( 84,  64),   S(108,  90),   S( 67, 131),   S(148,  95),   S(149,  91),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-36,  14),   S(  6,   3),   S( -4,  -4),   S(-39,  27),   S(-59,  24),   S( -4,  -1),   S(-25,   2),   S(-71,  19),
            S(-28,  10),   S(  3, -10),   S(-51, -22),   S(-60, -29),   S(-55, -23),   S(-37, -31),   S(-45, -18),   S(-108,   7),
            S( -1,   9),   S(  2,  10),   S(-21, -18),   S(-35, -36),   S(-34, -20),   S( -7, -35),   S(-25, -17),   S(-48, -10),
            S( 15,  64),   S(-18,  61),   S(  3,  38),   S( 29,  22),   S( 18,  -7),   S(  0, -16),   S(  8,  -7),   S(  0,  10),
            S( 75,  93),   S(  4, 129),   S( 11, 118),   S( 75,  85),   S(127,  57),   S(171,  11),   S(138,  30),   S( 96,  50),
            S(118,  65),   S(187,  77),   S(114, 129),   S(153,  80),   S( 94,  39),   S(114, -10),   S( 63,  15),   S( 44,  14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-52,  -3),   S( -3,  -5),   S( 37, -22),   S( 10,  -7),   S( -6, -11),   S( 13, -34),   S( -1, -32),   S(-25, -22),
            S(-45, -12),   S(-18, -11),   S(-25, -39),   S(-48, -42),   S(-19, -51),   S(-44, -55),   S(-32, -42),   S(-79, -18),
            S(-21,  -2),   S(-20,   0),   S( 12, -46),   S(-10, -49),   S(  3, -49),   S(  1, -62),   S( -9, -33),   S(-35, -28),
            S( 60,   8),   S( 60,  -8),   S( 57, -18),   S( 54,   3),   S( 28, -24),   S( -9, -19),   S( 18, -20),   S( 31,  -9),
            S(227, -22),   S(144,  -5),   S(172, -22),   S(142,  24),   S( 48,  74),   S( 67,  42),   S( 14,  61),   S(111,  51),
            S(161, -22),   S( 60,  39),   S(104, -18),   S( 54,  52),   S(100,  21),   S( 20,  60),   S(127,  43),   S( 98,  59),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: KK */
            S(-109, -50),  S(-16, -83),   S(-43, -11),   S(-22,   7),   S( -3,   1),   S(  0,   2),   S(-14, -15),   S(-59, -58),
            S(-29, -51),   S(-43,  21),   S( -3,  -1),   S( 13,   3),   S( 14,  16),   S( 24,  25),   S(  1,  20),   S( -1,   4),
            S(-20, -15),   S( -4,  21),   S(  5,  33),   S( 26,  57),   S( 36,  55),   S( 14,  30),   S( 11,  41),   S(  7,  34),
            S( -4,  32),   S( 18,  51),   S( 28,  78),   S( 29,  89),   S( 34,  99),   S( 52,  84),   S( 57,  76),   S( 20,  48),
            S( 14,  48),   S( 19,  48),   S( 42,  61),   S( 47,  91),   S( 17, 103),   S( 42, 112),   S( 17, 116),   S( 70,  56),
            S(-33,  69),   S( 21,  52),   S( 35,  63),   S( 45,  65),   S( 86,  51),   S(175,  26),   S( 38,  79),   S( -7,  69),
            S(-20,  40),   S(-28,  62),   S( -3,  66),   S( 48,  67),   S( 38,  73),   S(122,   6),   S( 18,  27),   S(-18,  51),
            S(-312,  57),  S(-65,  70),   S(-51,  73),   S( 54,  52),   S( 57,  49),   S(-120,  54),  S(  4,  46),   S(-205, -72),

            /* knights: KQ */
            S(-94, -118),  S(-41, -56),   S(-56, -10),   S(-53,   7),   S(-28, -18),   S(-21, -24),   S(-38,   7),   S(-22, -129),
            S(-80, -26),   S(-36,   4),   S( -8, -28),   S( 16, -25),   S( -1, -10),   S( 23,  -8),   S(-63,  24),   S(-47, -17),
            S(-41, -16),   S(  0, -32),   S( -3,   5),   S( 27,   2),   S( 26,   2),   S( 27, -33),   S( -5,  14),   S(-16,  10),
            S( -2,   1),   S( 54, -16),   S( 40,  19),   S( 62,  10),   S( 55,  20),   S( 32,  28),   S( 30,  16),   S(-13,  24),
            S( 64, -46),   S( 28, -19),   S( 66,   0),   S( 63,   9),   S( 85,   3),   S( 67,   6),   S( 52,  -2),   S( 20,  -3),
            S( 58, -37),   S( 71, -61),   S(136, -63),   S(128, -44),   S( 62,   4),   S( 66,   8),   S(-10,  37),   S( -6,   6),
            S(  8, -46),   S(-16, -27),   S(  6, -39),   S( 73, -10),   S( 28,  -2),   S( 25,  13),   S(-28,  25),   S(-47,   5),
            S(-293, -118), S(-14, -57),   S(-14, -34),   S( 36,  -7),   S(-47, -29),   S( -6,  37),   S(-35,  33),   S(-127, -53),

            /* knights: QK */
            S(-97, -107),  S(-70,  17),   S(-47,  11),   S(-69,  37),   S(-26, -11),   S(-18, -30),   S(-51, -44),   S(-90, -88),
            S(-40, -66),   S(-29,  11),   S( 15, -13),   S( -3,   2),   S(-10,   3),   S(  3,   0),   S(-59,  -1),   S(-12, -67),
            S(-10,   4),   S(  1, -15),   S( 28, -13),   S( 51,  18),   S( 63,  -2),   S(-16,  16),   S(  6, -17),   S(-50, -34),
            S( 32,  -1),   S( 60,  10),   S( 54,  22),   S( 41,  50),   S( 54,  23),   S( 41,  30),   S( 42,   4),   S( -9,   0),
            S( 18,  26),   S( 60,  13),   S( 73,  25),   S(122,   3),   S( 70,  27),   S( 82,  17),   S( 53,   6),   S( 75, -22),
            S( -5,  40),   S(  8,  36),   S( 38,  36),   S( 76,  -1),   S(101,   1),   S(160, -32),   S( 44,  -6),   S( 23, -23),
            S(-30,  29),   S( -1,  14),   S( 10,  45),   S( 63,  49),   S(-20,  40),   S( 68,  -6),   S( -3, -18),   S( 63, -34),
            S(-151, -86),  S(-36,  22),   S(-29,  35),   S( 22,  -3),   S( -9,  28),   S(-36, -27),   S(-33, -25),   S(-58, -98),

            /* knights: QQ */
            S(  8, -112),  S(-35, -18),   S( 35, -37),   S(-70,   4),   S(-36,  -7),   S(-42,  -5),   S(-42, -77),   S(-66, -87),
            S(-31, -44),   S(-32,  27),   S( 18, -39),   S( 28, -25),   S(-14, -13),   S( 16, -59),   S(  3, -22),   S(-137, -49),
            S( 15, -26),   S( 13, -25),   S( 27, -28),   S( 40,  -6),   S( 34,  -7),   S(-10, -17),   S(-11,  -6),   S(-25, -55),
            S( 22,  42),   S( 28,   9),   S( 80,   2),   S( 82, -12),   S( 55,  -4),   S( 53, -13),   S( 56,  -4),   S(-33,   9),
            S( 50,  -8),   S( 24,  11),   S( 66, -13),   S( 80, -10),   S( 81,  -4),   S( 69, -18),   S( 72, -29),   S(  9,   3),
            S( 81, -21),   S(-39,   3),   S( 46, -23),   S(140, -31),   S( 53, -15),   S( 33,  13),   S( 68, -22),   S(  3, -22),
            S(-21,  29),   S( 23, -11),   S( -9,   2),   S( 15,  29),   S( 50,  15),   S( 44,   5),   S(-14,  10),   S(-61,  15),
            S(-123, -87),  S(  0,  60),   S(  0,   9),   S( -1,  31),   S(-21, -13),   S( 15,  33),   S(  6,  48),   S(-51, -43),

            /* bishops: KK */
            S( 20, -17),   S( 23, -22),   S(  5,  -5),   S(-16,  39),   S(  9,  30),   S(  6,  37),   S( 10,   6),   S(  5, -46),
            S( 23,  -1),   S( 22,   8),   S( 20,  18),   S( 14,  38),   S( 18,  34),   S( 29,  26),   S( 46,   6),   S( 31, -45),
            S(  1,  23),   S( 21,  42),   S( 29,  61),   S( 22,  48),   S( 20,  77),   S( 21,  48),   S( 24,  25),   S(  8,  23),
            S( -6,  45),   S(  3,  64),   S( 20,  71),   S( 49,  57),   S( 39,  48),   S( 19,  54),   S( 12,  38),   S( 37,  -7),
            S( -4,  64),   S( 24,  35),   S( 11,  50),   S( 50,  49),   S( 33,  57),   S( 48,  49),   S( 28,  45),   S(  6,  61),
            S(  1,  51),   S( 16,  59),   S(-12,  62),   S( -6,  59),   S(  5,  67),   S( 78,  65),   S( 54,  51),   S( -6,  90),
            S(-51,  77),   S(-34,  71),   S(-19,  70),   S(-41,  81),   S(-45,  79),   S( -8,  57),   S(-88,  66),   S(-59,  54),
            S(-108,  98),  S(-73,  88),   S(-54,  88),   S(-64,  97),   S(-65,  89),   S(-98,  78),   S(  6,  30),   S(-69,  55),

            /* bishops: KQ */
            S(-49,   1),   S( 33, -18),   S(-24,   6),   S(-19,   0),   S( 21, -16),   S(  8, -20),   S(103, -93),   S( 15, -91),
            S(-18, -24),   S( -5,  -5),   S(  0,   7),   S( 11,   3),   S( 21,  -1),   S( 53, -30),   S( 84, -29),   S( 20, -72),
            S(-25,   2),   S(  9,   5),   S( 31,  11),   S( 31,   3),   S( 31,  14),   S( 58, -10),   S( 61, -16),   S( 48, -36),
            S( 24,  12),   S(  7,  22),   S( 17,   5),   S( 67,  -8),   S( 84,  -5),   S( 46,  24),   S( 25,  19),   S( -5,  18),
            S( 11,  19),   S( 55, -13),   S( 41, -12),   S(128, -59),   S( 88,  -2),   S( 33,  21),   S( 37,  18),   S( 22,   8),
            S(  8, -10),   S(109, -37),   S(126, -26),   S(  5,  17),   S( 40,   5),   S( 36,  13),   S( 29,  24),   S(  4,  36),
            S(-51, -29),   S( 22, -13),   S(-50,  25),   S(-38,  22),   S( -6,  13),   S(-16,  24),   S(-30,  47),   S(-74,  72),
            S(-32,   5),   S(-35,   5),   S( 16,  -4),   S(-18,  23),   S(-16,  21),   S(-24,  36),   S(-29,  37),   S(-53,  32),

            /* bishops: QK */
            S( 39, -63),   S( 35, -36),   S( 24,  -2),   S(-24,  22),   S( -5,  12),   S(-24,  -4),   S(-25,  20),   S(-132,  -2),
            S( 62, -58),   S( 72, -26),   S( 31,   6),   S( 36,  10),   S( -2,  11),   S(-11,  11),   S(-26,  -5),   S( -5, -27),
            S( 16,  -4),   S( 50,   7),   S( 52,  14),   S( 31,  24),   S( 20,  35),   S(  6,   5),   S( 13,  -7),   S(-14, -18),
            S(  3,   3),   S( 36,  22),   S( 34,  39),   S( 83,   4),   S( 60,  -4),   S( 14,  30),   S(-21,  30),   S( -8,  17),
            S(-13,  49),   S( 30,  21),   S( 45,  16),   S( 93,  -3),   S( 51,   7),   S( 55,  13),   S( 49, -13),   S(-15,  31),
            S(-34,  47),   S( -4,  48),   S( 41,  16),   S( 25,  34),   S( 51,  27),   S( 74,  23),   S( 89, -28),   S( 22,   9),
            S(-53,  58),   S(-65,  45),   S(-44,  54),   S(-39,  55),   S(-71,  53),   S( -8,  31),   S(-32,   0),   S(-30, -25),
            S(-58,  34),   S(-61,  38),   S(-31,  42),   S(-51,  57),   S( -2,  15),   S(-36,   5),   S( 25,   8),   S(-27,  20),

            /* bishops: QQ */
            S(-77, -17),   S(-24, -19),   S( 22, -36),   S(-63, -12),   S(  2, -24),   S(-11, -38),   S( 41, -55),   S(-19, -81),
            S( 49, -74),   S( 70, -52),   S( -8, -12),   S( 39, -28),   S(-26,   1),   S( 42, -49),   S( -5, -36),   S( 93, -110),
            S( 16, -24),   S( 44, -28),   S( 48, -24),   S( 22, -21),   S( 41, -18),   S( 21, -26),   S( 55, -59),   S( -1, -27),
            S( 51, -21),   S( 65, -21),   S( 26, -22),   S( 85, -42),   S( 35, -28),   S( 71, -25),   S( 21, -23),   S( 28, -34),
            S( 32, -15),   S( 36, -21),   S( 37, -35),   S( 90, -47),   S( 92, -55),   S( 10, -21),   S( 37, -35),   S(  3,  -6),
            S(-34,  -3),   S( 90, -23),   S( 94, -35),   S( 54, -24),   S( -2,   4),   S( 66, -36),   S( -7,  -6),   S(  0,  -5),
            S(-47, -20),   S(-30, -10),   S( 36, -24),   S( -6,   8),   S( 26, -21),   S(-30,  -7),   S( 33, -16),   S(-26,   6),
            S(-25, -17),   S( -5, -12),   S( -7, -19),   S( -3,   9),   S( 17,  10),   S(-13,  -3),   S(  6,   7),   S(-32,  31),

            /* rooks: KK */
            S(-36,  95),   S(-24,  87),   S(-18,  77),   S( -6,  50),   S(  2,  37),   S( -4,  62),   S( 10,  55),   S(-31,  45),
            S(-52,  91),   S(-34,  84),   S(-28,  75),   S(-19,  52),   S(-17,  39),   S(  9,  21),   S( 35,  33),   S(-20,  47),
            S(-53, 100),   S(-50, 103),   S(-37,  80),   S(-26,  56),   S( -4,  35),   S( -6,  46),   S( 29,  34),   S( -5,  43),
            S(-47, 126),   S(-35, 113),   S(-25,  99),   S(-14,  79),   S(-22,  63),   S(-28,  92),   S( 26,  74),   S(-26,  78),
            S(-29, 140),   S(-11, 124),   S(  0, 107),   S( 24,  77),   S( -2,  93),   S( 40,  78),   S( 73,  78),   S( 20,  87),
            S( -9, 140),   S( 31, 116),   S( 38, 101),   S( 40,  76),   S( 77,  57),   S(144,  57),   S(140,  65),   S( 68,  81),
            S(-24, 127),   S(-23, 126),   S( 11,  92),   S( 36,  65),   S( 23,  70),   S(104,  59),   S(107,  82),   S(145,  60),
            S( 72, 106),   S( 60, 119),   S( 49,  97),   S( 65,  68),   S( 46,  98),   S(106,  98),   S(119, 108),   S(127,  84),

            /* rooks: KQ */
            S(-52,   3),   S( -4, -26),   S(-15, -31),   S(-25, -16),   S(-26, -16),   S(-19, -15),   S(-32,  24),   S(-60,  29),
            S(-71,   3),   S(-17, -39),   S(-32, -32),   S(-32, -27),   S(-30, -20),   S(-39,  -3),   S(-43,  14),   S(-42,   2),
            S(-53,   2),   S(-28, -17),   S(-48, -15),   S(-21, -34),   S(-56,  -9),   S(-30,  -8),   S( -7,   0),   S(-53,   6),
            S(-74,  21),   S(-17,  -6),   S(-68,  12),   S(-10, -11),   S(-24,  -9),   S(-55,  34),   S(-11,  20),   S(-14,  10),
            S(-19,  16),   S( 43,  -9),   S( -7,   4),   S( -6,  16),   S( 44, -17),   S( 30,   6),   S( 37,  18),   S(  1,  32),
            S( 79,  -1),   S( 93, -14),   S( 58, -25),   S( 84, -31),   S( 81, -34),   S( 61,  -1),   S(133, -15),   S( 51,  22),
            S(-17,  25),   S( 23,   8),   S( 58, -29),   S( 50, -21),   S( 73, -25),   S( 30,  -8),   S( 56,  11),   S( 97,  -6),
            S( 25,  44),   S( 46,  22),   S( 50,   0),   S( 50,  -3),   S( 43,  -2),   S( 82,   1),   S( 78,  31),   S(115,   3),

            /* rooks: QK */
            S(-94,  44),   S(-28,  18),   S(-33,  17),   S(-27, -16),   S(-17, -30),   S(-15, -30),   S( -4, -34),   S(-45,   1),
            S(-49,  12),   S(-49,  11),   S(-52,  19),   S(-38, -15),   S(-53, -20),   S(-50, -27),   S(-23, -34),   S(-35, -24),
            S(-44,  18),   S( -8,  15),   S(-34,   8),   S(-48,  -7),   S(-71,   2),   S(-74,  -4),   S(-24, -31),   S(-35, -30),
            S(-11,  15),   S( 39,   2),   S(-35,  27),   S(-38,  15),   S(-56,  16),   S(-54,  18),   S(  7,  -1),   S(-28,   0),
            S( 52,  20),   S( 30,  39),   S( 59,   7),   S(-18,  28),   S(-26,  23),   S( -8,  15),   S( 17,   9),   S(  1,  -6),
            S( 39,  36),   S(102,  16),   S( 46,  28),   S( -7,  23),   S( 62,  -8),   S(100,  -4),   S(112, -11),   S( 46,  10),
            S( 34,  22),   S( 54,  15),   S( 96,  -9),   S(  9,   9),   S(-30,  21),   S( 88, -11),   S( 33,  11),   S( 15,  18),
            S(246, -90),   S(117,   1),   S( 89,   7),   S( 54,  17),   S( 19,  28),   S( 77,   6),   S( 96,  11),   S(101,   2),

            /* rooks: QQ */
            S(-56, -25),   S(-22, -40),   S( 12, -72),   S( 14, -90),   S( -6, -67),   S(-17, -37),   S(-34, -15),   S(-45,   5),
            S(-34, -53),   S(-40, -33),   S(-34, -48),   S(  5, -86),   S(-33, -74),   S(-47, -58),   S(-28, -50),   S(-29, -26),
            S(-88, -10),   S( 26, -54),   S(  2, -63),   S(-16, -67),   S(-41, -32),   S(-53, -47),   S(-51, -11),   S(-55,  -6),
            S(-46,  -7),   S( 42, -41),   S(-32, -22),   S(-18, -37),   S(-52, -39),   S(-31, -25),   S(-54,  -5),   S(-37,   7),
            S( 29, -22),   S( 67, -36),   S(  7, -22),   S(113, -71),   S( 41, -40),   S( 37, -25),   S( -4,   7),   S(  0,   8),
            S( 78, -28),   S(100, -26),   S(121, -44),   S(104, -58),   S( 77, -56),   S( 73, -24),   S( 35,  -5),   S( 17,   7),
            S( 95, -35),   S( 69, -25),   S( 25, -14),   S( 83, -60),   S(161, -94),   S( 79, -61),   S( 52, -25),   S( 17,   0),
            S( 46,   9),   S( 35,  14),   S( 11,   0),   S(  0,   2),   S( 14, -32),   S( 58, -26),   S(105,  -1),   S( 25,  10),

            /* queens: KK */
            S( 38,  46),   S( 47,  18),   S( 57,  -7),   S( 60,   3),   S( 80, -61),   S( 60, -86),   S( -9, -38),   S(  2,  48),
            S( 26,  73),   S( 47,  45),   S( 49,  41),   S( 58,  24),   S( 60,  13),   S( 86, -39),   S( 82, -57),   S( 50, -23),
            S( 23,  57),   S( 38,  58),   S( 43,  69),   S( 31,  68),   S( 49,  52),   S( 37,  84),   S( 49,  78),   S( 48,  25),
            S( 32,  81),   S( 13, 107),   S( 22,  91),   S( 18, 101),   S( 18,  92),   S( 28, 111),   S( 53,  90),   S( 14, 140),
            S( 22,  98),   S( 31, 104),   S(-11, 146),   S( -5, 135),   S(  4, 151),   S( 14, 180),   S( 23, 191),   S( 19, 200),
            S( 13, 117),   S( 21, 154),   S(-14, 161),   S(-32, 176),   S( 11, 174),   S(106, 163),   S( 54, 213),   S( 15, 257),
            S(-16, 173),   S(-36, 204),   S(-23, 191),   S(-13, 165),   S(-54, 216),   S( 88, 146),   S( 16, 282),   S(123, 173),
            S(-29, 170),   S( 40, 127),   S( 22, 138),   S( 10, 158),   S( 33, 155),   S(110, 146),   S(159, 155),   S(129, 170),

            /* queens: KQ */
            S( 21, -41),   S( 16, -29),   S( 29, -102),  S( 19, -58),   S( 21, -39),   S(  7, -71),   S(-33, -33),   S(-28,   9),
            S( 31,  -1),   S( 30, -46),   S( 44, -94),   S( 20, -39),   S( 27, -44),   S( 17, -62),   S(-40, -30),   S(-54, -51),
            S(  8,   0),   S(  6, -24),   S( 39, -89),   S( 18, -32),   S( 27, -26),   S( 25,  20),   S( 11, -12),   S( -6, -60),
            S( -1,  31),   S( 55, -75),   S(-23,  15),   S(-17,  -2),   S( 13,  -4),   S(-13,  34),   S(  1,  30),   S(-16,  50),
            S( 14,   0),   S( 30,  -5),   S( -2,   2),   S( -5,  -5),   S( 43, -31),   S( -7,  37),   S(  4,  42),   S(  2,  69),
            S( 48,   7),   S( 91,  13),   S( 63,  11),   S( 28,   6),   S( 11,  29),   S( 22,  41),   S( 27,  71),   S( 12,  54),
            S( 52,  37),   S( 94,  16),   S( 72,  28),   S( 79,  12),   S( 43,   3),   S( -4,  71),   S(-15,  88),   S( 20,  55),
            S( 79,  44),   S(126,  41),   S( 96,  41),   S( 85,  59),   S( 28,  69),   S( 59,  30),   S( 51,  20),   S(-10,  23),

            /* queens: QK */
            S(-75, -66),   S(-89, -40),   S( -6, -113),  S( -4, -80),   S( 17, -112),  S(  0, -73),   S(-26, -41),   S(-58,  19),
            S(-70, -29),   S(-36, -91),   S( 39, -122),  S( 22, -78),   S( 20, -77),   S( -1, -57),   S( 41, -75),   S(  2, -10),
            S(-46, -37),   S( -1, -26),   S(  5,  -9),   S(-15,  11),   S( 11, -33),   S(-19,  -8),   S( 13, -54),   S( -4,  -2),
            S(-16, -33),   S(-43,  35),   S(-31,  45),   S(-10,  10),   S(-21,  20),   S( -6, -10),   S( 10, -17),   S(-15,  -3),
            S(-11,  30),   S(-13,  15),   S(-44,  71),   S(-39,  50),   S(-32,  36),   S( 38,  -7),   S(  2,  30),   S( 10, -17),
            S( -4,  21),   S( -4,  75),   S(-14,  44),   S(-32,  53),   S( 15,  32),   S( 69,  16),   S( 43,  40),   S( 13,  37),
            S(  5,  -2),   S(  3,  29),   S(-44,  83),   S(-37,  43),   S(-20,  33),   S(  2,  31),   S( 47,  17),   S( 60,  40),
            S(-31,  44),   S( 17,  30),   S( 32,  27),   S( 27,  26),   S(-27,  33),   S( 78,  47),   S( 96,  36),   S( 65,  48),

            /* queens: QQ */
            S(-60, -59),   S(-74, -48),   S(-39, -19),   S(-40, -65),   S(  9, -45),   S(-12, -67),   S(-69, -38),   S(-76, -19),
            S(-79, -32),   S(-51, -59),   S(  7, -76),   S( 22, -92),   S(-37, -26),   S(-25, -77),   S(-19, -48),   S(-63, -37),
            S(-24, -20),   S(-14, -11),   S(  9, -59),   S(-14, -55),   S(-16, -60),   S(-33, -34),   S(-20, -48),   S(-39, -37),
            S(-46, -26),   S( 25, -19),   S(-40,   0),   S( -1, -49),   S( -8, -59),   S( -4, -99),   S(-17, -60),   S(-40,   5),
            S(-30,  10),   S(-18,  17),   S( 46, -38),   S( 38, -40),   S( 68, -42),   S(  5, -39),   S(-16,  -4),   S( -4, -41),
            S( 36,  17),   S( 48,  15),   S( 91,  41),   S( 51, -21),   S(  9, -18),   S(-30,  -6),   S(-12,   2),   S(-10,  55),
            S( 81,  22),   S( 51,  42),   S( 68,  14),   S( 32,  38),   S( 13,  -3),   S(-14, -13),   S(-11,  25),   S(-44,  12),
            S(  9, -32),   S( 69,  40),   S( 18,  29),   S( 29,  16),   S( 16,  -7),   S( 13, -27),   S( 29, -22),   S( 16, -26),

            /* kings: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-107,  -3),  S(-123,  22),  S(-47,  -9),   S(-57, -40),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-59,  11),   S(-48,  11),   S(-26,   5),   S(-51,   5),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-12,  20),   S(-48,  22),   S(-49,  12),   S(-101,  12),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(107,   8),   S( 60,  19),   S( 12,  16),   S(-126,  25),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(231, -18),   S(287, -34),   S(181,  -6),   S( 33,  -1),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(254, -17),   S(297, -26),   S(275,   2),   S(155,   3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(200, -14),   S(147,  -5),   S(151,  49),   S( 67,  13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 60,   2),   S( 63,  29),   S( 66,  38),   S( -7, -133),

            /* kings: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-147,  16),  S(-163,  24),  S(-68, -17),   S(-79, -26),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-55,   1),   S(-57,   2),   S(-61,  18),   S(-81,  10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-48,  19),   S(-19,   3),   S(-37,   8),   S(-114,  18),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(127,  -2),   S( 55,  21),   S(-13,  34),   S(-84,  33),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(229, -10),   S(147,  22),   S( 87,  35),   S(  0,  38),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(176,   6),   S( 97,  41),   S(118,  35),   S( -3,  55),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(177,  -4),   S(218,  -9),   S(102,  25),   S( 33,  22),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 70,  -7),   S( 69,   3),   S( 83, -21),   S( -1, -21),

            /* kings: QK */
            S(-65, -73),   S(-43, -68),   S(-88, -45),   S(-163, -16),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-38, -48),   S(-60, -17),   S(-73, -28),   S(-92, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-98, -16),   S(-68,  -9),   S(-43, -18),   S(-86,   1),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-96,  13),   S(  7,   3),   S( 47,  -5),   S( 38,  -4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-44,  11),   S( 71,  18),   S( 83,   8),   S(161, -18),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-28,  33),   S( 89,  22),   S(157,   2),   S(171, -11),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 45,  -5),   S(149,   1),   S(120,   2),   S(136,  -8),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-30, -18),   S( 40,  21),   S(121,  -2),   S( 48,   1),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: QQ */
            S(-177,  42),  S(-111,  -1),  S(-158,  12),  S(-205,   3),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-123,  10),  S(-114,   9),  S(-90,  -7),   S(-128,  -7),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-131,   4),  S(-73,  -9),   S(-67,  -7),   S(-66,  -2),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-56, -13),   S( 19,   0),   S( 42,   4),   S(102,  -9),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 54, -11),   S(153,  -4),   S(219, -16),   S(216, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 66,  13),   S(177,  16),   S(164, -15),   S(124,   9),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  8,  -6),   S( 39,  63),   S( 71,  15),   S( 94,   5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-21, -128),  S( 26,  50),   S( 34,   8),   S( 61,   8),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            #endregion

            /* mobility weights */
            S(  9,   6),    // knights
            S(  5,   4),    // bishops
            S(  4,   3),    // rooks
            S(  1,   3),    // queens

            /* center control */
            S(  1,   7),    // D0
            S(  2,   4),    // D1

            /* squares attacked near enemy king */
            S( 24,  -4),    // attacks to squares 1 from king
            S( 21,  -2),    // attacks to squares 2 from king
            S(  7,   1),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S( 21,  16),    // friendly pawns 1 from king
            S(  9,  18),    // friendly pawns 2 from king
            S(  7,  13),    // friendly pawns 3 from king

            /* castling right available */
            S( 38, -39),

            /* castling complete */
            S( 19, -17),

            /* king on open file */
            S(-80,  12),

            /* king on half-open file */
            S(-30,  25),

            /* king on open diagonal */
            S( -8,  11),

            /* isolated pawns */
            S( -4, -11),

            /* doubled pawns */
            S(-16, -31),

            /* backward pawns */
            S(  0,   0),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 28,  -9),   S( -1,   0),   S( 10,  -4),   S( 16,  29),   S( 31,  31),   S(  1, -45),   S(-31,  54),   S(  9, -50),
            S( 18,   2),   S( 22,  -9),   S( 18,  26),   S( 20,  43),   S( 56,  -7),   S(  1,  24),   S( 19,   0),   S( 13, -17),
            S(-11,  12),   S( 23,  18),   S(  1,  51),   S( 29,  81),   S( 40,  27),   S( 40,  38),   S( 23,   5),   S( 20,  22),
            S( 43,  67),   S( 13,  51),   S( 71,  88),   S( 14,  84),   S(107,  73),   S(107,  36),   S( 13,  59),   S( 37,  45),
            S( 70, 115),   S(137, 113),   S(173, 174),   S(182, 166),   S(166, 128),   S(169, 178),   S(153,  87),   S( 84,  44),
            S( 80, 190),   S(123, 276),   S(121, 245),   S(124, 238),   S( 84, 191),   S( 53, 151),   S( 44, 195),   S( 24, 146),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  1,  19),   S(-14,  35),   S(-25,  32),   S(-48,  54),   S( 18,   2),   S(-25,  31),   S(-23,  61),   S( 29,  18),
            S( 11,  25),   S( -2,  44),   S(-16,  41),   S( -8,  36),   S(-34,  48),   S(-49,  50),   S(-59,  78),   S( 18,  28),
            S(  9,  21),   S(  2,  35),   S(-16,  42),   S( 16,  31),   S(-12,  30),   S(-35,  47),   S(-64,  76),   S(-16,  48),
            S( 49,  24),   S( 76,  42),   S( 37,  39),   S( 18,  31),   S( 22,  60),   S( 56,  47),   S( 43,  60),   S(  8,  61),
            S( 66,  49),   S(105,  67),   S(107,  50),   S( 33,  41),   S( -7,  33),   S( 86,  30),   S( 40,  61),   S(  7,  40),
            S(264,  65),   S(247, 101),   S(234, 115),   S(244, 105),   S(252, 109),   S(245, 112),   S(294, 117),   S(271,  93),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 48,  13),   S( 19,  27),   S( 21,  28),   S( 24,  53),   S( 94,  29),   S( 30,  23),   S( 26,  18),   S( 61,  11),
            S( 10,  14),   S(  6,  14),   S( 25,  14),   S( 26,  28),   S( 16,  12),   S( -5,   7),   S( 12,   4),   S( 31,  -6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-10, -14),   S( -6, -14),   S(-25, -14),   S(-26, -28),   S(-16, -12),   S(  5,  -7),   S(-12,  -4),   S(-31,   6),
            S(-48, -13),   S(-19, -27),   S(-21, -28),   S(-24, -53),   S(-94, -29),   S(-30, -23),   S(-26, -18),   S(-61, -11),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 35,  -1),   S( 37,  17),   S( 51,  22),   S( 53,  33),   S( 33,  39),   S( 36,  19),   S( 24,  13),   S( 68, -10),
            S( -6,   3),   S( 21,  23),   S( 21,  13),   S( 30,  35),   S( 34,  12),   S( 19,  17),   S( 40,   4),   S( 10,   3),
            S(-20,  14),   S( 30,  26),   S( 64,  36),   S( 50,  36),   S( 66,  35),   S( 72,  14),   S( 29,  30),   S( 29,   7),
            S( 67,  55),   S(152,  29),   S(137,  87),   S(168,  63),   S(184,  62),   S( 90,  81),   S(104,  26),   S(101,   5),
            S(111,  92),   S(241,  59),   S(224, 145),   S(192,  60),   S(263, 145),   S(164, 128),   S(225,  39),   S(-64, 123),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S( -3,   9),   S( -5,  47),   S( 27,  97),   S(103, 149),

            /* enemy king outside passed pawn square */
            S( 12, 183),

            /* passed pawn/friendly king distance penalty */
            S( -2, -21),

            /* passed pawn/enemy king distance bonus */
            S(  2,  31),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 78, -36),   S( 56,   5),   S( 52,  19),   S( 57,  36),   S( 80,  -5),   S(173, -19),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 32,   1),   S( 19,  51),   S( 14,  53),   S( 13,  86),   S( 66,  74),   S(191,  82),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-62,  19),   S(-15, -26),   S(-21, -34),   S(-21, -23),   S( 42, -59),   S(279, -149),  S(  0,   0),    // blocked by rooks
            S(  0,   0),   S( 13, -42),   S( 59, -37),   S(  3,  11),   S(  6, -36),   S( 34, -221),  S(-62, -239),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S( -2,  15),   S( 84,   8),   S( 56, -47),   S( 95, -26),   S(235, -20),   S(372,  54),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S( -1,  46),

            /* knight on outpost */
            S(  5,  31),

            /* bishop on outpost */
            S( 11,  30),

            /* bishop pair */
            S( 40, 105),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -3,   0),   S(  0,  -7),   S( -4,  -5),   S( -5, -50),   S(-14, -13),   S(-29,   5),   S(-23,  -5),   S( -1,  -6),
            S( -5,  -9),   S( -9, -10),   S(-15, -13),   S( -8, -19),   S(-15, -19),   S(-17,  -5),   S(-16,  -7),   S( -2, -10),
            S( -6, -11),   S(  4, -27),   S(  0, -41),   S( -2, -56),   S(-17, -36),   S(-13, -30),   S(-12, -22),   S( -3,  -7),
            S(  3, -26),   S(  7, -37),   S( -2, -34),   S(  0, -49),   S( -8, -37),   S( -9, -31),   S( -6, -33),   S( -4, -22),
            S( 16, -29),   S(  5, -45),   S( 13, -56),   S(  4, -54),   S( 29, -70),   S( -7, -51),   S( 43, -88),   S(-16, -11),
            S( 63, -44),   S(108, -97),   S( 99, -99),   S( 91, -119),  S( 91, -149),  S(112, -112),  S( 76, -145),  S(155, -142),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 38,   2),

            /* rook on half-open file */
            S( 11,  35),

            /* rook on seventh rank */
            S( 14,  35),

            /* doubled rooks on file */
            S( 20,  25),

            /* queen on open file */
            S(-10,  37),

            /* queen on half-open file */
            S(  7,  32),

            /* pawn push threats */
            S(  0,   0),   S( 32,  30),   S( 35, -18),   S( 34,  25),   S( 34, -21),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 68,  71),   S( 50,  89),   S( 68,  80),   S( 58,  17),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-12,  11),   S( 49,  28),   S( 91,   6),   S( 29,  64),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 31,  63),   S( -5,  40),   S( 55,  39),   S( 37, 105),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-16,  63),   S( 56,  73),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-14,  16),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats
        };
    }
}
