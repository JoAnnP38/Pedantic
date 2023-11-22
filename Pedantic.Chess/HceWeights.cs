using System.Runtime.CompilerServices;
using System.Text;

using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class HceWeights
    {
        public static readonly Guid HCE_WEIGHTS_VERSION = new("da5e310e-b0dc-4c77-902c-5a46cc81bb73");

        #region Weight Offset Constants

        public const int MAX_WEIGHTS = 1995;
        public const int PIECE_VALUES = 0;
        public const int PIECE_SQUARE_TABLE = 6;
        public const int PIECE_MOBILITY = 1542;
        public const int TRAPPED_PIECE = 1546;
        public const int CENTER_CONTROL = 1550;

        /* king safety/evaluation */
        public const int KING_ATTACK = 1552;
        public const int PAWN_SHIELD = 1555;
        public const int CASTLING_AVAILABLE = 1558;
        public const int CASTLING_COMPLETE = 1559;
        public const int KING_ON_OPEN_FILE = 1560;
        public const int KING_ON_HALF_OPEN_FILE = 1561;
        public const int KING_ON_OPEN_DIAGONAL = 1562;
        public const int KING_ATTACK_SQUARE_OPEN = 1563;

        /* cached pawn positions */
        public const int ISOLATED_PAWN = 1564;
        public const int DOUBLED_PAWN = 1565;
        public const int BACKWARD_PAWN = 1566;
        public const int PHALANX_PAWN = 1567;
        public const int PASSED_PAWN = 1631;
        public const int PAWN_RAM = 1695;
        public const int CHAINED_PAWN = 1759;

        /* non-cached pawn positions */
        public const int PP_CAN_ADVANCE = 1823;
        public const int KING_OUTSIDE_PP_SQUARE = 1827;
        public const int PP_FRIENDLY_KING_DISTANCE = 1828;
        public const int PP_ENEMY_KING_DISTANCE = 1829;
        public const int BLOCK_PASSED_PAWN = 1830;
        public const int ROOK_BEHIND_PASSED_PAWN = 1878;

        /* piece evaluation */
        public const int KNIGHT_OUTPOST = 1879;
        public const int BISHOP_OUTPOST = 1880;
        public const int BISHOP_PAIR = 1881;
        public const int BAD_BISHOP_PAWN = 1882;
        public const int ROOK_ON_OPEN_FILE = 1946;
        public const int ROOK_ON_HALF_OPEN_FILE = 1947;
        public const int ROOK_ON_7TH_RANK = 1948;
        public const int DOUBLED_ROOKS_ON_FILE = 1949;
        public const int QUEEN_ON_OPEN_FILE = 1950;
        public const int QUEEN_ON_HALF_OPEN_FILE = 1951;

        /* threats */
        public const int PAWN_PUSH_THREAT = 1952;
        public const int PIECE_THREAT = 1958;

        /* misc */
        public const int TEMPO_BONUS = 1994;

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
        public Score TrappedPiece(Piece piece)
        {
            Util.Assert(piece >= Piece.Knight);
            return weights[TRAPPED_PIECE + (int)piece - 1];
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
        public Score KingAttackSquareOpen => weights[KING_ATTACK_SQUARE_OPEN];
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

        public Score TempoBonus => weights[TEMPO_BONUS];

        #endregion

        private readonly Score[] weights;
        private static Score S(short mg, short eg) => new(mg, eg);

        public static readonly short[] CanonicalPieceValues = { 100, 300, 300, 500, 900, 0 };

        // Solution sample size: 12000000, generated on Tue, 21 Nov 2023 23:25:24 GMT
        // Solution K: 0.003850, error: 0.085940, accuracy: 0.4923
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(102, 155),   S(440, 476),   S(485, 513),   S(612, 873),   S(1415, 1536), S(  0,   0),

            /* piece square values */
            #region piece square values

            /* pawns: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-53,  15),   S(-53,  15),   S(-41,   4),   S(-31, -10),   S(-50,  28),   S( 30,  -2),   S( 44, -28),   S(-10, -31),
            S(-56,   9),   S(-50,   1),   S(-54, -19),   S(-55, -12),   S(-41, -15),   S(-14, -19),   S( 11, -23),   S(-41, -17),
            S(-26,   8),   S(-35,  12),   S(-13, -26),   S(-26, -43),   S(-10, -31),   S( 20, -31),   S(  0, -10),   S( -7, -24),
            S(-19,  60),   S(-25,  33),   S(  2,  10),   S( 15,   0),   S( 27, -22),   S( 25, -22),   S( 31,   1),   S( 59, -18),
            S( 41,  62),   S(-24,  63),   S(-11,  60),   S( 35,  57),   S(111,  68),   S(151,  12),   S(155,   0),   S(141,   9),
            S( 88,  82),   S( 73,  96),   S( 81,  85),   S(102,  47),   S( 72,  60),   S( 97, -11),   S( 65,  35),   S( 88, -14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-87,  29),   S(-92,  40),   S(-59,  15),   S(-71,  40),   S(-49,  -5),   S( 25, -27),   S( 41, -39),   S( -8, -17),
            S(-82,   5),   S(-88,  10),   S(-67, -19),   S(-66, -17),   S(-39, -39),   S(-26, -27),   S( 12, -35),   S(-35, -12),
            S(-58,  21),   S(-42,  15),   S(-31, -13),   S(-26, -38),   S(-21, -27),   S( -3, -24),   S( -6, -20),   S(  6, -21),
            S(-53,  63),   S(-12,   8),   S( 15,  -7),   S( 12,   8),   S(  5, -10),   S(-22,  24),   S( 21,  11),   S( 56,  -1),
            S( 73,  26),   S( 68, -13),   S(106,  -7),   S( 90,  34),   S(122,  98),   S( 51,  91),   S( 43,  87),   S( 89,  84),
            S(106,   4),   S( 50,  37),   S(132, -19),   S( 86,  69),   S( 77,  94),   S( 85, 138),   S(131,  99),   S(145,  98),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-30,  11),   S( -1,   0),   S(-18,  -3),   S(-40,  21),   S(-53,  17),   S(-19,   2),   S(-29,   2),   S(-67,  20),
            S(-24,   7),   S( -9,  -8),   S(-47, -14),   S(-61, -19),   S(-46, -19),   S(-30, -24),   S(-44, -10),   S(-93,   7),
            S( -1,  12),   S( -3,  13),   S(-16,  -9),   S(-21, -35),   S(-31, -15),   S( -6, -27),   S(-20, -14),   S(-43,  -5),
            S(  1,  61),   S( -7,  54),   S( 14,  30),   S( 22,  18),   S(  9,  -6),   S(  3, -22),   S(  7,  -9),   S(  8,   3),
            S( 45,  82),   S(-14, 104),   S( -1, 108),   S( 55,  72),   S(100,  66),   S(110,  13),   S(109,  -9),   S( 64,  36),
            S(145,  59),   S(200,  71),   S(117, 132),   S(127,  83),   S( 60,  61),   S( 61,   0),   S( 60,  19),   S( 20,  27),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-56,   6),   S(-10,  -8),   S( 15, -24),   S( -1, -19),   S(-17, -21),   S(  3, -26),   S( -1, -32),   S(-32, -10),
            S(-51,  -3),   S(-24, -10),   S(-36, -23),   S(-41, -30),   S(-22, -40),   S(-33, -44),   S(-23, -37),   S(-66, -14),
            S(-17,   7),   S(-19,   6),   S( 10, -26),   S( -1, -45),   S(  1, -35),   S(  3, -47),   S( -1, -34),   S(-34, -21),
            S( 47,  13),   S( 64, -15),   S( 60, -23),   S( 55,  -3),   S( 20, -23),   S( -5, -19),   S( 14, -19),   S( 35, -13),
            S(205, -46),   S(159, -42),   S(118, -14),   S(125,  21),   S( 59,  61),   S( 39,  41),   S( 13,  26),   S( 86,  39),
            S(180, -24),   S( 83,  21),   S( 83, -23),   S( 52,  44),   S(110,  16),   S( 52,  33),   S( 92,  51),   S(117,  51),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: KK */
            S(-110, -29),  S(-18, -59),   S(-44, -13),   S(-30,   9),   S(-15,  -8),   S(-26,  -1),   S(-22, -14),   S(-42, -26),
            S(-33, -50),   S(-43,  23),   S( -6,   1),   S(  9,   7),   S(  6,  10),   S( -3,  25),   S(  0,  11),   S( -7,   7),
            S(-22, -13),   S( -4,  22),   S( -1,  38),   S( 23,  54),   S( 28,  56),   S( 11,  28),   S(  7,  35),   S(  4,  13),
            S( -7,  30),   S( 15,  53),   S( 24,  82),   S( 25,  91),   S( 33,  95),   S( 44,  78),   S( 45,  75),   S( 20,  41),
            S(  9,  44),   S( 17,  52),   S( 39,  59),   S( 44,  83),   S( 15, 101),   S( 39, 104),   S( 18, 104),   S( 53,  48),
            S(-21,  56),   S(  6,  53),   S( 27,  68),   S( 37,  71),   S( 77,  51),   S(143,  41),   S( 39,  79),   S( -1,  63),
            S(-44,  50),   S(-35,  66),   S(-16,  70),   S( 34,  66),   S( 42,  72),   S(106,  12),   S( -1,   9),   S( 10,  19),
            S(-302,  52),  S(-47,  69),   S(-88,  83),   S( 32,  52),   S( 69,  38),   S(-78,  34),   S( 12,  24),   S(-240, -48),

            /* knights: KQ */
            S(-61, -136),  S(-36, -32),   S(-47,  -6),   S(-39,  -9),   S(-21, -13),   S(-34, -14),   S(-37,  -9),   S(-20, -83),
            S(-53, -41),   S(-54,  18),   S( -1, -28),   S( 10, -15),   S( -1,  -8),   S( 13,   3),   S(-49,   9),   S(-35, -23),
            S(-27, -19),   S( 16, -28),   S( -7,  22),   S( 41,  10),   S( 20,  11),   S( 25, -13),   S( -6,  12),   S(-25,  27),
            S(  6,   1),   S( 64, -15),   S( 38,  23),   S( 54,  26),   S( 46,  38),   S( 45,  29),   S( 47,   5),   S(  0,  10),
            S( 61, -28),   S( 28,  -4),   S( 69,  15),   S( 76,  14),   S( 84,  16),   S( 46,  21),   S( 46,   8),   S( 19,   2),
            S( 44, -36),   S( 61, -53),   S(129, -52),   S(144, -33),   S( 78,   8),   S( 52,  14),   S(  7,  30),   S(  5,  17),
            S( -2, -15),   S(  4, -37),   S( 29, -31),   S( 82, -12),   S( 16,  16),   S( 34,  20),   S(-13,  27),   S(-32, -10),
            S(-271, -78),  S(-19, -39),   S(  6, -25),   S(  5, -10),   S(-12, -26),   S( 20,  26),   S(-32,   5),   S(-153, -53),

            /* knights: QK */
            S(-95, -79),   S(-61,  20),   S(-57,   8),   S(-83,  37),   S(-44,  -9),   S(-35, -26),   S(-49, -17),   S(-62, -79),
            S(-19, -49),   S( -7,  13),   S(-15,   0),   S( -8,   5),   S( -3,   2),   S(-11,  -2),   S(-57,   2),   S(-20, -59),
            S(-17,  24),   S( -6,  -5),   S( 29, -11),   S( 36,  24),   S( 42,  12),   S(-14,  19),   S( 12, -10),   S(-29, -37),
            S( 15,  22),   S( 73,   4),   S( 59,  29),   S( 44,  46),   S( 45,  31),   S( 46,  35),   S( 46,   5),   S( -3,  11),
            S(  3,  25),   S( 45,  19),   S( 76,  19),   S(103,  14),   S( 58,  41),   S( 74,  31),   S( 38,  15),   S( 41, -20),
            S(-56,  34),   S( 30,  21),   S( 37,  40),   S( 54,  14),   S( 93,  -2),   S(145, -22),   S( 81,  -4),   S( 51, -20),
            S(-22,  25),   S(-50,  42),   S( -4,  34),   S( 41,  37),   S(  7,  55),   S( 38,  -3),   S( -9,  -6),   S( 29, -35),
            S(-140, -64),  S(-28,  33),   S(  7,  35),   S( 33,   4),   S( 13,  18),   S(-39, -30),   S(-14, -20),   S(-35, -48),

            /* knights: QQ */
            S( -1, -114),  S(-32, -19),   S(-28, -12),   S(-64,  -2),   S( -7,   2),   S(-44, -13),   S(-29, -52),   S(-54, -70),
            S(-35, -51),   S(-30,   8),   S(-10, -22),   S( 15, -32),   S( -3, -24),   S( 20, -39),   S(  4, -57),   S(-112, -76),
            S(  0, -17),   S( 16, -16),   S( 11, -13),   S( 27,   7),   S( 17,   2),   S(-14,   9),   S(-10,   2),   S(-20, -52),
            S( -7,  30),   S( 40,   9),   S( 78,   1),   S( 71,   6),   S( 37,  11),   S( 46,   6),   S( 36,   5),   S( -9,   2),
            S( 44,  -9),   S( 38,  -5),   S( 68,   1),   S( 60,   8),   S( 79,  -1),   S( 75,  -2),   S( 60, -17),   S(-14,  16),
            S( 51, -12),   S(  1,  14),   S( 72,  -8),   S(102, -22),   S( 71,  -9),   S( 58,  13),   S( 39,   3),   S( 27,   2),
            S( -6,  17),   S(  0,   5),   S(-20, -11),   S( 38,  29),   S( 13,  22),   S( -4,  16),   S(-27,  27),   S(-16,   6),
            S(-160, -94),  S(-15,  56),   S( -2, -27),   S( -7,  19),   S(-25, -15),   S(  5,   8),   S(-16,  25),   S(-56, -44),

            /* bishops: KK */
            S(  8, -10),   S( 22, -24),   S(  0, -11),   S(-21,  33),   S( -2,  24),   S(-12,  22),   S( -9,  -1),   S( 16, -49),
            S( 20,  -5),   S( 17,  12),   S( 19,  12),   S( 12,  34),   S( 20,  20),   S( 10,  24),   S( 40,  -6),   S( 23, -43),
            S(  3,  28),   S( 21,  36),   S( 27,  61),   S( 25,  44),   S( 17,  69),   S( 25,  36),   S( 21,  21),   S( 10,  16),
            S(  3,  30),   S(  6,  60),   S( 22,  66),   S( 48,  56),   S( 39,  47),   S( 18,  52),   S( 18,  39),   S( 31, -11),
            S( -1,  52),   S( 27,  26),   S( 13,  49),   S( 50,  45),   S( 32,  60),   S( 48,  40),   S( 28,  36),   S( 16,  45),
            S( 12,  29),   S( 16,  53),   S(-10,  57),   S(  0,  57),   S(  3,  63),   S( 53,  72),   S( 57,  41),   S(  9,  69),
            S(-39,  67),   S(-21,  62),   S( -7,  61),   S(-22,  63),   S(-52,  81),   S(-21,  58),   S(-61,  57),   S(-40,  38),
            S(-102,  97),  S(-73,  84),   S(-62,  86),   S(-85,  98),   S(-33,  70),   S(-104,  79),  S( -9,  36),   S(-74,  55),

            /* bishops: KQ */
            S(-45,   8),   S( -7,   6),   S(-19,   4),   S(-24,   4),   S(-20,   3),   S( -8, -18),   S( 55, -83),   S( 14, -61),
            S(-33, -10),   S(  3,   2),   S(  8,   3),   S(  9,  -3),   S( 30, -14),   S( 42, -28),   S( 80, -47),   S( 18, -65),
            S( -2,   6),   S(  3,   9),   S( 32,  10),   S( 43,   0),   S( 30,  10),   S( 60,  -2),   S( 43, -14),   S( 57, -40),
            S( 33,  13),   S(  9,  25),   S( 20,  17),   S( 66,  -5),   S( 82,   0),   S( 41,  22),   S( 43,  10),   S( -1,  18),
            S(  5,  20),   S( 60,  -9),   S( 65,  -6),   S(118, -42),   S( 89,  -1),   S( 37,  20),   S( 36,   4),   S( 19,  -2),
            S( 15, -10),   S( 68, -13),   S( 89,  -5),   S(  3,  29),   S( 26,  19),   S( 29,  23),   S( 24,  29),   S(-11,  27),
            S(-65, -11),   S( 17,   0),   S( 11,  11),   S(-16,  25),   S(  3,  27),   S(-12,  35),   S(-42,  53),   S(-67,  60),
            S(-17,  11),   S(-27,  -5),   S( 10,  -6),   S( -6,  32),   S(-39,  34),   S(-53,  43),   S(-37,  42),   S( -7,  16),

            /* bishops: QK */
            S( 75, -67),   S(-11, -28),   S( -5,  15),   S(-36,  18),   S(  4, -11),   S(-14,  -8),   S(-43,  11),   S(-118,  27),
            S( 25, -33),   S( 65, -26),   S( 26,  -6),   S( 23,   8),   S(  3,   5),   S( -7,   5),   S(-30,   8),   S(-14, -19),
            S( 62, -22),   S( 44,   2),   S( 46,  13),   S( 24,  22),   S( 17,  36),   S( 11,   9),   S( 17,  -9),   S(-19,  -5),
            S( -5,  25),   S( 38,  28),   S( 33,  33),   S( 78,   5),   S( 63,   4),   S( 17,  29),   S(-26,  31),   S(  3,   9),
            S( 32,  22),   S( 24,  18),   S( 34,  26),   S( 72,   9),   S( 49,  11),   S( 47,  20),   S( 41,  -1),   S(-33,  42),
            S(-49,  48),   S( 23,  48),   S( 20,  25),   S( 18,  38),   S( 37,  35),   S( 69,  20),   S( 56,  -2),   S( 20,  21),
            S(-42,  64),   S(-16,  44),   S(  3,  39),   S(-26,  53),   S(-73,  60),   S( -6,  31),   S( -5,  12),   S(-82,  16),
            S(-88,  51),   S(-69,  64),   S(-54,  45),   S(-29,  60),   S( 24,  13),   S(-30,  17),   S(-18,   0),   S(-59,   1),

            /* bishops: QQ */
            S(-86, -11),   S(-39, -17),   S( -5, -33),   S(-81,   4),   S(-16, -19),   S(-10, -37),   S( 39, -67),   S( 15, -70),
            S( 15, -44),   S( 45, -43),   S( 19, -27),   S( 24, -23),   S( -7, -12),   S( 51, -48),   S( 11, -37),   S( 44, -82),
            S( 11, -10),   S( 39, -29),   S( 35, -17),   S( 17, -17),   S( 31, -12),   S( 38, -19),   S( 39, -44),   S( 13, -30),
            S( 20,   5),   S( 34, -13),   S( 35, -22),   S( 78, -31),   S( 52, -20),   S( 58, -19),   S( 25, -23),   S(  8, -27),
            S( 17,   0),   S( 56, -20),   S( 24, -23),   S( 86, -41),   S( 87, -42),   S( 30, -24),   S( 42, -29),   S(-25,  -1),
            S(-26, -12),   S( 81, -14),   S( 70, -26),   S( 80, -22),   S( 42,   0),   S( -3,  -8),   S(-13,  -7),   S(-18,  -6),
            S(-59, -28),   S( -2,  -7),   S( 40,  -9),   S( -8,   0),   S( 25, -13),   S(-43,   6),   S(-22,   8),   S(-37,   4),
            S(-10, -12),   S( -3, -13),   S( -5,  -5),   S( 19,   3),   S( 21,  13),   S(-30,   8),   S(-28,  14),   S(-27,  19),

            /* rooks: KK */
            S(-32,  89),   S(-21,  75),   S(-20,  65),   S(-17,  44),   S(-15,  25),   S(-33,  53),   S( -7,  46),   S(-33,  36),
            S(-44,  88),   S(-36,  87),   S(-27,  68),   S(-17,  41),   S(-20,  36),   S( -6,  14),   S( 21,  23),   S(-18,  35),
            S(-45,  89),   S(-36,  89),   S(-31,  78),   S(-22,  49),   S( -7,  32),   S( -5,  36),   S( 20,  36),   S( -2,  24),
            S(-41, 113),   S(-24, 108),   S(-15,  90),   S(-11,  73),   S(-19,  60),   S(-20,  85),   S( 31,  71),   S(-18,  65),
            S(-18, 132),   S( -3, 121),   S(  6, 100),   S( 20,  77),   S( -1,  92),   S( 40,  73),   S( 70,  81),   S( 13,  85),
            S(  3, 131),   S( 34, 110),   S( 29, 100),   S( 25,  87),   S( 63,  63),   S(119,  55),   S(111,  68),   S( 64,  78),
            S(-12, 115),   S( -6, 111),   S( 19,  80),   S( 43,  55),   S( 33,  60),   S(119,  40),   S( 94,  75),   S(115,  58),
            S( 80,  90),   S( 80, 100),   S( 59,  90),   S( 75,  64),   S( 41,  90),   S(102,  86),   S(134,  87),   S(145,  66),

            /* rooks: KQ */
            S(-49,  10),   S( -9, -29),   S(-25, -24),   S(-38, -13),   S(-48,  -6),   S(-59,   5),   S(-52,  19),   S(-72,  32),
            S(-64,   0),   S( -4, -39),   S(-39, -23),   S(-32, -22),   S(-36, -19),   S(-39,  -2),   S(-47,  10),   S(-70,   3),
            S(-63,   8),   S(-29, -22),   S(-51,  -9),   S(-18, -30),   S(-53, -14),   S(-46,  -2),   S(-27,   9),   S(-43,   3),
            S(-56,  21),   S(-11,   0),   S(-44,  15),   S(-14,   3),   S(-19,   4),   S(-45,  31),   S( -1,  32),   S(-40,  25),
            S(-22,  32),   S( 17,  10),   S( -5,  11),   S(  7,   1),   S( 18,  -2),   S( 28,   5),   S( 44,  19),   S( -1,  33),
            S( 40,  20),   S( 90,  -6),   S( 50,  -4),   S(106, -36),   S( 48,  -8),   S( 81,  -3),   S(104,   4),   S( 65,  21),
            S( -6,  26),   S( 26,   5),   S( 80, -25),   S( 69, -25),   S( 69, -26),   S( 41,  -9),   S( 64,  11),   S( 51,  11),
            S( 40,  40),   S( 49,  33),   S( 92,  -8),   S( 86,  -9),   S( 88, -12),   S(127, -11),   S( 98,  23),   S(114,  -2),

            /* rooks: QK */
            S(-86,  43),   S(-43,  28),   S(-56,  21),   S(-52, -12),   S(-26, -27),   S(-27, -19),   S(-11, -28),   S(-44,  -2),
            S(-50,  19),   S(-58,  21),   S(-53,  16),   S(-43,  -9),   S(-61, -12),   S(-40, -32),   S(-21, -33),   S(-45, -26),
            S(-42,  18),   S(-22,  21),   S(-51,  26),   S(-50,  -8),   S(-42,  -4),   S(-52,  -3),   S(-12, -30),   S(-38, -19),
            S(-20,  30),   S(  7,  16),   S(-26,  25),   S(-24,  12),   S(-40,   8),   S(-51,  19),   S(-21,   9),   S(-35,   7),
            S( 48,  21),   S( 42,  32),   S( 22,  27),   S(-15,  30),   S( -3,  16),   S( -9,  27),   S( 24,  14),   S( 18,  -5),
            S( 54,  38),   S(107,  20),   S( 77,  13),   S( -2,  36),   S( 51,   2),   S( 77,   7),   S(108,   1),   S( 34,  27),
            S( 51,  11),   S( 74,   5),   S( 69,  -5),   S( -5,  17),   S(-10,  14),   S( 88, -25),   S(  1,  17),   S( 21,  21),
            S(247, -95),   S(167, -19),   S( 74,  20),   S( 36,  15),   S( 19,  25),   S( 69,   8),   S( 49,  29),   S( 66,  21),

            /* rooks: QQ */
            S(-54, -26),   S(-22, -44),   S(-28, -51),   S(-19, -67),   S( -3, -68),   S(-12, -28),   S(-17, -18),   S(-36,   4),
            S(-18, -57),   S(-34, -31),   S(-55, -30),   S( -1, -78),   S(-14, -68),   S(-16, -59),   S(-20, -38),   S(-44,  -5),
            S(-72, -14),   S( -5, -41),   S(  8, -60),   S(-13, -45),   S(-44, -30),   S(-43, -15),   S(-53,   3),   S(-51,   6),
            S(-52, -11),   S( 12, -22),   S(-41,  -6),   S( -7, -35),   S(-32, -27),   S(-20,  -6),   S(-35,  14),   S(-36,  17),
            S( 38, -17),   S( 72, -24),   S( 24, -31),   S( 74, -37),   S( 67, -42),   S( 67, -23),   S( 35,   8),   S( -8,  13),
            S( 84, -17),   S( 72, -24),   S(105, -38),   S( 67, -23),   S( 59, -27),   S( 68, -19),   S( 77,  -7),   S( 36,   9),
            S( 40, -10),   S( 81, -23),   S( 40, -26),   S( 52, -37),   S(114, -77),   S( 93, -60),   S( 37,  -9),   S( 21,   8),
            S( 19,  26),   S( -2,  43),   S(  6,  13),   S(  2,  15),   S( 52, -22),   S( 61, -12),   S( 68,  15),   S( 61,  18),

            /* queens: KK */
            S( 22,  49),   S( 34,   8),   S( 37, -12),   S( 37, -14),   S( 50, -75),   S( 19, -98),   S(-29, -73),   S( -6,   7),
            S( 16,  70),   S( 38,  43),   S( 37,  32),   S( 46,  13),   S( 44,   5),   S( 51, -40),   S( 72, -90),   S( 27, -29),
            S( 14,  59),   S( 28,  55),   S( 33,  60),   S( 18,  66),   S( 28,  50),   S( 25,  66),   S( 36,  56),   S( 37,  15),
            S( 20,  78),   S( 10,  90),   S( 12,  91),   S( 13,  95),   S(  4,  90),   S( 12, 103),   S( 30,  95),   S( 11, 115),
            S( 19,  98),   S( 18, 104),   S( -5, 120),   S( -8, 127),   S( -7, 149),   S(  0, 176),   S(  8, 192),   S( 18, 165),
            S(  3, 107),   S( 17, 131),   S(-19, 148),   S(-37, 167),   S(  2, 172),   S( 55, 176),   S( 28, 211),   S(  7, 238),
            S(-29, 166),   S(-42, 193),   S(-21, 174),   S(-23, 173),   S(-76, 238),   S( 43, 163),   S( 22, 251),   S( 88, 186),
            S(-10, 137),   S( 22, 124),   S( 41, 115),   S( 29, 122),   S( 37, 135),   S(110, 134),   S(159, 106),   S(136, 138),

            /* queens: KQ */
            S( 27, -24),   S( 25, -64),   S( 12, -87),   S( 19, -105),  S( 19, -70),   S(-12, -91),   S(-56, -51),   S(-70, -24),
            S( 22,   6),   S( 48, -61),   S( 49, -100),  S( 16, -50),   S( 34, -64),   S( 10, -39),   S(  0, -86),   S(-51, -33),
            S( 10,   8),   S( 20, -41),   S( 39, -81),   S( 28, -52),   S( 16, -34),   S( 31, -16),   S( -8,  -1),   S(-19,   0),
            S(  9,  13),   S( 40, -42),   S(-12,   3),   S(  6, -20),   S( 18,  -3),   S( -2,  34),   S(-10,  47),   S(-10,  25),
            S( -1,  31),   S( 48, -19),   S(  2,  36),   S( 21,  -4),   S( 42,   2),   S(-37,  66),   S( -4,  68),   S( -6,  67),
            S( 29,  22),   S(103,   2),   S( 59,  29),   S( 77, -10),   S( 21,  27),   S( 23,  36),   S( 14,  55),   S(-15,  71),
            S( 70,  37),   S( 57,  16),   S( 37,  13),   S( 54,  52),   S( 39,  12),   S(-27,  93),   S(  8,  75),   S( -3,  52),
            S(103,   9),   S(107,  20),   S( 88,  49),   S( 99,  12),   S( 21,  47),   S( 67,  14),   S( 67,  10),   S(  6,  13),

            /* queens: QK */
            S(-46, -77),   S(-93, -67),   S(-26, -118),  S(-11, -84),   S( 22, -141),  S(-15, -80),   S(-22, -55),   S(-50,   6),
            S(-55, -60),   S(-34, -72),   S( 25, -112),  S( 17, -67),   S( 22, -80),   S( 18, -76),   S( 23, -58),   S( 15, -31),
            S(-36, -15),   S(-10, -11),   S(  0, -13),   S( -9,  -2),   S( -4, -23),   S( -9, -28),   S( 21, -43),   S(  1,  -7),
            S( -9, -11),   S(-20,  30),   S(-31,  49),   S( -5,  12),   S(-16,  20),   S( -5,   2),   S( 12, -18),   S( 11, -12),
            S(-12,  13),   S(-17,  40),   S(-24,  66),   S(-45,  55),   S( -7,  32),   S( -1,  26),   S(  6,  15),   S( -9,  -2),
            S(-13,  23),   S( 16,  44),   S(-24,  79),   S(-37,  67),   S( 15,  45),   S( 46,  32),   S( 16,  35),   S( 11,  46),
            S(-25,  46),   S(-49,  69),   S(-30,  81),   S(-52,  91),   S(-35,  72),   S(-17,  46),   S( 14,  52),   S( -9,  87),
            S(-22,  -2),   S( 23,  42),   S( 30,  28),   S( 19,  34),   S( 32,  22),   S( 17,  44),   S( 62,  23),   S( 76,  40),

            /* queens: QQ */
            S(-56, -62),   S(-89, -59),   S(-58, -65),   S(-41, -55),   S( -2, -77),   S(-23, -58),   S(-49, -38),   S(-26, -33),
            S(-101, -53),  S(-47, -67),   S( 15, -107),  S( 16, -92),   S( -2, -67),   S(-11, -79),   S(-22, -12),   S(-53, -18),
            S(-18,   5),   S(-14,  13),   S( -3, -52),   S(-19, -10),   S(-29,  -3),   S( -9, -20),   S(  1,   5),   S(-18,  -3),
            S(-12, -30),   S( 20, -57),   S(-12, -36),   S(-28, -14),   S( 14, -51),   S( -8, -39),   S(-26, -32),   S(-34,  34),
            S(-23,  -2),   S( -4,  31),   S(  8, -17),   S(  5, -19),   S( 20,  -9),   S( -4, -38),   S(  7,  -1),   S(  4, -25),
            S( 14,  19),   S( 16,  50),   S( 87,  22),   S(  9,  26),   S(-21, -10),   S( 11,  -4),   S( 20,  -2),   S( 15,   4),
            S(100,  35),   S( 26,  14),   S( 27,  35),   S( 20,  31),   S(  7,  25),   S(  3,   4),   S(  3,  29),   S(-15,  21),
            S( 72, -21),   S( 34,  21),   S( 22,  20),   S( 10,  -1),   S( 13,   8),   S( 23, -15),   S( 35, -16),   S( 33,  -5),

            /* kings: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-89,  -1),   S(-95,  16),   S(-41,  -4),   S(-72, -32),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-24,   5),   S(-25,  10),   S( -6,  -6),   S(-64,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -2,  23),   S(-39,  24),   S(-33,   9),   S(-110,  10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 96,  13),   S( 37,  26),   S(-12,  25),   S(-119,  21),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(235, -12),   S(254, -28),   S(139,   4),   S(-15,   8),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(228, -18),   S(273, -24),   S(278, -10),   S(162, -10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(192, -14),   S(141, -11),   S(162,  35),   S( 42,   3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 61,   7),   S( 38,  34),   S( 45,  26),   S(-14, -168),

            /* kings: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-121,  10),  S(-133,  14),  S(-65, -12),   S(-95, -16),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-33,  -1),   S(-40,   0),   S(-43,   5),   S(-90,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-22,  17),   S(-12,   5),   S(-39,  10),   S(-129,  15),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(108,   6),   S( 67,  19),   S( 10,  32),   S(-98,  33),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(203,   0),   S(151,  21),   S( 80,  38),   S(-53,  47),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(157,  12),   S(108,  37),   S( 92,  40),   S( -6,  47),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(119,   4),   S(175,   9),   S(123,  20),   S( 19,   9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 76,  -7),   S( 48,  -4),   S( 65,   1),   S( 11, -23),

            /* kings: QK */
            S(-76, -77),   S(-39, -67),   S(-76, -40),   S(-131, -14),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-67, -45),   S(-39, -26),   S(-48, -29),   S(-64, -22),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-145,   1),  S(-50, -10),   S(-47,  -8),   S(-65,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-110,  20),  S(-28,  15),   S( 46,  -6),   S( 52,  -4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-47,  11),   S( 27,  21),   S(109,   0),   S(163, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-52,  26),   S(109,  13),   S(132,   6),   S(167, -10),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-12,  -1),   S(154,  -3),   S(106,   7),   S(133,  -6),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-38, -29),   S( 32,  17),   S(110, -12),   S( 41,  -4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: QQ */
            S(-179,  39),  S(-98,  -9),   S(-142,  10),  S(-158,  -5),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-145,   7),  S(-80,  -6),   S(-70, -12),   S(-106,  -6),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-144,   2),  S(-84,  -6),   S(-40,  -8),   S(-69,   5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-47, -15),   S( 13,   8),   S( 65,   1),   S(100,  -8),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-18,   3),   S(142,  -5),   S(179,  -5),   S(225, -24),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 71,   8),   S(146,  14),   S(150, -10),   S(134,   7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  7,   6),   S( 48,  49),   S( 48,  23),   S(128,   4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-43, -127),  S( 14,  64),   S( 46,  17),   S( 30,   5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            #endregion

            /* mobility weights */
            S(  7,   5),    // knights
            S(  4,   3),    // bishops
            S(  2,   4),    // rooks
            S(  1,   3),    // queens

            /* trapped pieces */
            S(-13, -217),   // knights
            S(  1, -127),   // bishops
            S(  2, -89),    // rooks
            S( 17, -50),    // queens

            /* center control */
            S(  2,   6),    // D0
            S(  2,   4),    // D1

            /* squares attacked near enemy king */
            S( 21,  -4),    // attacks to squares 1 from king
            S( 19,  -1),    // attacks to squares 2 from king
            S(  6,   1),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S( 16,  12),    // friendly pawns 1 from king
            S(  8,  15),    // friendly pawns 2 from king
            S(  4,  12),    // friendly pawns 3 from king

            /* castling right available */
            S( 37, -26),

            /* castling complete */
            S( 14, -15),

            /* king on open file */
            S(-58,  13),

            /* king on half-open file */
            S(-25,  22),

            /* king on open diagonal */
            S( -4,   8),

            /* king attack square open */
            S(-12,  -1),

            /* isolated pawns */
            S(  1, -16),

            /* doubled pawns */
            S(-13, -30),

            /* backward pawns */
            S(  9, -13),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 15,   4),   S(  2,   0),   S(  7,   4),   S( 14,  26),   S( 24,  37),   S(  1, -29),   S(-20,  49),   S(  3, -36),
            S(  2,   4),   S( 29,   3),   S(  7,  29),   S( 18,  43),   S( 42,   0),   S(  1,  30),   S( 14,   0),   S( 11,  -6),
            S(-13,  26),   S( 17,   8),   S(  0,  57),   S( 24,  73),   S( 33,  31),   S( 26,  38),   S( 27,  10),   S(  7,  26),
            S( 21,  47),   S( 23,  48),   S( 37,  85),   S( 13,  84),   S( 79,  62),   S( 73,  46),   S( 20,  49),   S( 18,  55),
            S( 97,  80),   S(128,  90),   S( 89, 154),   S(143, 151),   S(166, 116),   S(151, 122),   S(154,  73),   S( 84,  53),
            S( 94, 224),   S(135, 312),   S(113, 239),   S(104, 206),   S( 75, 176),   S( 65, 169),   S( 53, 197),   S( 26, 133),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  1,  13),   S(-11,  19),   S(-25,  26),   S(-42,  60),   S( 21, -11),   S(-23,  15),   S( -9,  49),   S( 20,   8),
            S( 10,  24),   S(  0,  35),   S(-11,  30),   S( -4,  24),   S(-20,  35),   S(-51,  47),   S(-50,  68),   S( 19,  21),
            S(  8,  14),   S(  9,  20),   S( -4,  26),   S( 14,  29),   S( -3,  24),   S(-36,  38),   S(-54,  66),   S(-22,  43),
            S( 41,  24),   S( 70,  35),   S( 34,  34),   S( 16,  28),   S( 22,  53),   S( 56,  43),   S( 31,  56),   S(-17,  68),
            S( 61,  63),   S(107,  93),   S( 98,  63),   S( 45,  45),   S(-22,  31),   S( 83,  41),   S( 54,  84),   S( 11,  49),
            S(250,  67),   S(231, 106),   S(226, 115),   S(218, 114),   S(207, 122),   S(211, 118),   S(257, 121),   S(263,  97),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 39,   9),   S(  7,  17),   S( 18,  25),   S( 16,  50),   S( 78,  38),   S( 29,  19),   S( 18,   3),   S( 45,  10),
            S(  6,  14),   S(  4,  10),   S( 22,  12),   S( 21,  27),   S( 16,  13),   S( -3,   6),   S( 11,   3),   S( 31,  -6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -6, -14),   S( -4, -10),   S(-22, -12),   S(-21, -27),   S(-16, -13),   S(  3,  -6),   S(-11,  -3),   S(-31,   6),
            S(-39,  -9),   S( -7, -17),   S(-18, -25),   S(-16, -50),   S(-78, -38),   S(-29, -19),   S(-18,  -3),   S(-45, -10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 34,   1),   S( 38,  11),   S( 54,  15),   S( 52,  16),   S( 37,  26),   S( 34,  13),   S( 19,   9),   S( 58, -12),
            S(  1,   3),   S( 24,  19),   S( 20,  12),   S( 26,  37),   S( 30,  12),   S( 14,  13),   S( 33,   4),   S( 17,   1),
            S( -5,  10),   S( 22,  28),   S( 55,  34),   S( 50,  31),   S( 58,  32),   S( 55,  17),   S( 25,  30),   S( 27,  10),
            S( 47,  64),   S(108,  40),   S(118,  74),   S(143,  62),   S(135,  60),   S( 78,  74),   S( 94,  24),   S( 86,   7),
            S( 56,  81),   S(181,  70),   S(216, 119),   S(183,  79),   S(201, 115),   S(161,  98),   S(203,  49),   S( -8,  69),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S( -2,   6),   S( -7,  46),   S( 27,  93),   S( 60, 208),

            /* enemy king outside passed pawn square */
            S(-23, 196),

            /* passed pawn/friendly king distance penalty */
            S( -2, -19),

            /* passed pawn/enemy king distance bonus */
            S(  2,  30),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 70, -49),   S( 42,   9),   S( 39,  20),   S( 54,  36),   S( 49,   9),   S(185, -27),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 13,  -6),   S( 14,  55),   S(  9,  49),   S(  8,  75),   S( 40,  75),   S(168,  85),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-38,  -2),   S(-10, -37),   S(  1, -38),   S(-23, -21),   S( 27, -50),   S(254, -125),  S(  0,   0),    // blocked by rooks
            S(  0,   0),   S( -6,   0),   S( 34, -22),   S(  0,  18),   S( 12, -48),   S( 19, -192),  S( -3, -206),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(  3,  -1),   S( 81,   1),   S(  9, -39),   S( 74, -23),   S(202, -11),   S(355,  56),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S(  3,  44),

            /* knight on outpost */
            S(  2,  31),

            /* bishop on outpost */
            S( 10,  30),

            /* bishop pair */
            S( 36,  79),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,  -1),   S( -1,  -5),   S( -4,  -5),   S( -2, -35),   S( -8, -14),   S(-24,   5),   S(-22,  -5),   S(  0,  -5),
            S( -5,  -8),   S( -8,  -8),   S(-15, -10),   S(-10, -11),   S(-12, -17),   S(-15,  -6),   S(-14,  -7),   S( -3,  -6),
            S( -5,  -9),   S(  5, -27),   S( -3, -39),   S( -3, -50),   S(-13, -35),   S(-11, -28),   S(-13, -16),   S(  0,  -7),
            S(  6, -25),   S(  9, -37),   S( -6, -25),   S( -4, -40),   S( -7, -34),   S( -9, -23),   S( -2, -27),   S( -5, -17),
            S( 13, -16),   S(  8, -41),   S( 10, -52),   S(  8, -52),   S( 20, -58),   S(  8, -51),   S(  6, -64),   S( -2,  -8),
            S( 59, -33),   S( 84, -87),   S( 75, -98),   S( 68, -118),  S( 88, -141),  S(122, -113),  S(106, -129),  S(103, -110),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 40,  -1),

            /* rook on half-open file */
            S( 12,  34),

            /* rook on seventh rank */
            S(  6,  45),

            /* doubled rooks on file */
            S( 16,  27),

            /* queen on open file */
            S(-12,  36),

            /* queen on half-open file */
            S(  3,  38),

            /* pawn push threats */
            S(  0,   0),   S( 30,  28),   S( 33, -15),   S( 35,  22),   S( 33, -18),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 73,  73),   S( 57,  97),   S( 59,  87),   S( 56,  35),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-13,   7),   S( 50,  34),   S( 93,   4),   S( 42,  17),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 32,  65),   S(  2,  26),   S( 54,  53),   S( 43,  96),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-10,  58),   S( 60,  52),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-23,  17),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats

            /* tempo bonus for side to move */
            S( 18,   7),
        };
    }
}
