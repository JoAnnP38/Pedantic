using System.Runtime.CompilerServices;
using System.Text;

using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class HceWeights
    {
        public static readonly Guid HCE_WEIGHTS_VERSION = new("da5e310e-b0dc-4c77-902c-5a46cc81bb73");

        #region Weight Offset Constants

        public const int MAX_WEIGHTS = 1991;
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
        public const int KING_ATTACK_SQUARE_OPEN = 1559;

        /* cached pawn positions */
        public const int ISOLATED_PAWN = 1560;
        public const int DOUBLED_PAWN = 1561;
        public const int BACKWARD_PAWN = 1562;
        public const int PHALANX_PAWN = 1563;
        public const int PASSED_PAWN = 1627;
        public const int PAWN_RAM = 1691;
        public const int CHAINED_PAWN = 1755;

        /* non-cached pawn positions */
        public const int PP_CAN_ADVANCE = 1819;
        public const int KING_OUTSIDE_PP_SQUARE = 1823;
        public const int PP_FRIENDLY_KING_DISTANCE = 1824;
        public const int PP_ENEMY_KING_DISTANCE = 1825;
        public const int BLOCK_PASSED_PAWN = 1826;
        public const int ROOK_BEHIND_PASSED_PAWN = 1874;

        /* piece evaluation */
        public const int KNIGHT_OUTPOST = 1875;
        public const int BISHOP_OUTPOST = 1876;
        public const int BISHOP_PAIR = 1877;
        public const int BAD_BISHOP_PAWN = 1878;
        public const int ROOK_ON_OPEN_FILE = 1942;
        public const int ROOK_ON_HALF_OPEN_FILE = 1943;
        public const int ROOK_ON_7TH_RANK = 1944;
        public const int DOUBLED_ROOKS_ON_FILE = 1945;
        public const int QUEEN_ON_OPEN_FILE = 1946;
        public const int QUEEN_ON_HALF_OPEN_FILE = 1947;

        /* threats */
        public const int PAWN_PUSH_THREAT = 1948;
        public const int PIECE_THREAT = 1954;

        /* misc */
        public const int TEMPO_BONUS = 1990;

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

        // Solution sample size: 12000000, generated on Sat, 18 Nov 2023 10:16:23 GMT
        // Solution K: 0.003850, error: 0.085977, accuracy: 0.4921
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(102, 155),   S(436, 474),   S(482, 509),   S(609, 873),   S(1412, 1536), S(  0,   0),

            /* piece square values */
            #region piece square values

            /* pawns: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-53,  15),   S(-54,  15),   S(-42,   3),   S(-32, -12),   S(-51,  28),   S( 29,  -1),   S( 43, -28),   S(-10, -30),
            S(-56,   9),   S(-50,   1),   S(-54, -19),   S(-54, -12),   S(-40, -16),   S(-14, -18),   S( 11, -23),   S(-41, -17),
            S(-25,   9),   S(-35,  12),   S(-12, -26),   S(-26, -43),   S(-10, -31),   S( 20, -31),   S(  0, -11),   S( -7, -24),
            S(-19,  60),   S(-25,  32),   S(  2,  10),   S( 16,  -1),   S( 27, -22),   S( 25, -22),   S( 31,   1),   S( 60, -18),
            S( 41,  63),   S(-24,  64),   S(-12,  60),   S( 34,  57),   S(111,  69),   S(150,  12),   S(155,   1),   S(141,   9),
            S( 87,  82),   S( 73,  96),   S( 81,  85),   S(102,  48),   S( 71,  61),   S( 98, -11),   S( 65,  35),   S( 89, -14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-87,  30),   S(-93,  40),   S(-59,  14),   S(-72,  40),   S(-50,  -5),   S( 24, -27),   S( 39, -38),   S( -8, -17),
            S(-82,   5),   S(-88,  10),   S(-67, -18),   S(-66, -17),   S(-38, -39),   S(-26, -27),   S( 12, -35),   S(-35, -12),
            S(-58,  21),   S(-42,  14),   S(-31, -13),   S(-26, -38),   S(-21, -27),   S( -2, -24),   S( -6, -20),   S(  6, -21),
            S(-52,  63),   S(-11,   8),   S( 15,  -7),   S( 13,   7),   S(  5, -10),   S(-23,  24),   S( 21,  11),   S( 56,  -1),
            S( 73,  26),   S( 68, -13),   S(105,  -7),   S( 89,  34),   S(122,  98),   S( 51,  91),   S( 44,  88),   S( 88,  84),
            S(106,   4),   S( 51,  36),   S(132, -19),   S( 85,  69),   S( 77,  93),   S( 84, 139),   S(131,  99),   S(143,  99),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-30,  11),   S( -2,   1),   S(-19,  -3),   S(-41,  22),   S(-58,  17),   S(-19,   2),   S(-30,   2),   S(-67,  20),
            S(-24,   7),   S( -9,  -8),   S(-47, -14),   S(-60, -20),   S(-45, -19),   S(-31, -24),   S(-44, -11),   S(-93,   7),
            S( -1,  12),   S( -4,  13),   S(-15,  -9),   S(-21, -35),   S(-31, -16),   S( -5, -27),   S(-20, -14),   S(-42,  -5),
            S(  1,  61),   S( -7,  54),   S( 13,  29),   S( 22,  17),   S(  9,  -7),   S(  3, -22),   S(  7, -10),   S(  9,   3),
            S( 46,  82),   S(-15, 105),   S( -1, 108),   S( 55,  72),   S(101,  66),   S(110,  13),   S(109,  -8),   S( 64,  36),
            S(145,  59),   S(200,  72),   S(116, 132),   S(127,  83),   S( 60,  61),   S( 61,   0),   S( 61,  19),   S( 20,  27),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-57,   6),   S(-12,  -8),   S( 14, -24),   S( -2, -17),   S(-22, -19),   S(  3, -26),   S( -2, -32),   S(-31, -10),
            S(-51,  -3),   S(-24, -10),   S(-35, -23),   S(-40, -30),   S(-22, -40),   S(-32, -44),   S(-23, -38),   S(-65, -14),
            S(-17,   7),   S(-19,   6),   S( 10, -26),   S( -1, -46),   S(  1, -35),   S(  3, -47),   S( -2, -34),   S(-33, -21),
            S( 47,  13),   S( 64, -15),   S( 60, -24),   S( 55,  -4),   S( 20, -23),   S( -5, -19),   S( 15, -20),   S( 35, -13),
            S(205, -45),   S(160, -41),   S(118, -15),   S(125,  21),   S( 57,  61),   S( 38,  41),   S( 13,  27),   S( 87,  39),
            S(180, -24),   S( 82,  22),   S( 84, -23),   S( 51,  44),   S(110,  16),   S( 52,  33),   S( 91,  52),   S(117,  51),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: KK */
            S(-113, -33),  S(-19, -77),   S(-44, -13),   S(-31,   9),   S(-17,  -9),   S(-26,   0),   S(-23, -29),   S(-51, -47),
            S(-35, -48),   S(-45,  25),   S( -5,   2),   S(  9,   6),   S(  5,  10),   S( -3,  26),   S( -2,  12),   S( -8,   4),
            S(-22, -11),   S( -4,  24),   S( -1,  40),   S( 23,  54),   S( 29,  56),   S( 12,  30),   S(  7,  36),   S(  3,  10),
            S( -7,  31),   S( 16,  54),   S( 24,  82),   S( 24,  91),   S( 33,  95),   S( 45,  79),   S( 45,  76),   S( 20,  39),
            S( 10,  45),   S( 17,  53),   S( 39,  60),   S( 43,  82),   S( 15, 101),   S( 39, 105),   S( 18, 105),   S( 53,  49),
            S(-21,  57),   S(  6,  54),   S( 27,  68),   S( 37,  71),   S( 77,  52),   S(143,  42),   S( 39,  80),   S(  0,  64),
            S(-43,  52),   S(-34,  67),   S(-15,  71),   S( 35,  67),   S( 43,  73),   S(107,  13),   S(  0,  10),   S( 11,  21),
            S(-300,  52),  S(-46,  71),   S(-86,  83),   S( 33,  53),   S( 71,  39),   S(-77,  36),   S( 12,  25),   S(-237, -48),

            /* knights: KQ */
            S(-63, -140),  S(-40, -45),   S(-48,  -5),   S(-40,  -9),   S(-26, -11),   S(-35, -13),   S(-42, -21),   S(-32, -91),
            S(-54, -39),   S(-56,  19),   S(  0, -27),   S(  9, -16),   S( -2,  -7),   S( 13,   4),   S(-51,  10),   S(-40, -22),
            S(-26, -17),   S( 17, -27),   S( -7,  24),   S( 41,  10),   S( 21,  11),   S( 26, -11),   S( -5,  13),   S(-28,  29),
            S(  6,   1),   S( 66, -15),   S( 38,  23),   S( 54,  26),   S( 46,  37),   S( 46,  29),   S( 48,   5),   S( -1,   9),
            S( 61, -27),   S( 29,  -3),   S( 69,  15),   S( 76,  14),   S( 84,  16),   S( 46,  21),   S( 47,   9),   S( 20,   4),
            S( 45, -35),   S( 63, -53),   S(129, -51),   S(146, -33),   S( 79,   7),   S( 52,  15),   S(  7,  31),   S(  7,  18),
            S(  0, -14),   S(  6, -36),   S( 32, -31),   S( 83, -12),   S( 17,  16),   S( 36,  20),   S(-12,  28),   S(-31,  -9),
            S(-264, -72),  S(-18, -37),   S(  6, -24),   S(  6,  -9),   S(-12, -25),   S( 21,  28),   S(-31,   6),   S(-152, -52),

            /* knights: QK */
            S(-117, -88),  S(-69,  14),   S(-57,   9),   S(-85,  38),   S(-47,  -7),   S(-36, -27),   S(-53, -33),   S(-63, -86),
            S(-20, -49),   S( -9,  14),   S(-15,   1),   S( -9,   5),   S( -5,   2),   S(-10,  -1),   S(-57,   3),   S(-20, -58),
            S(-17,  26),   S( -6,  -4),   S( 30, -10),   S( 36,  24),   S( 43,  11),   S(-13,  21),   S( 12,  -8),   S(-29, -35),
            S( 15,  22),   S( 73,   5),   S( 60,  29),   S( 45,  46),   S( 45,  30),   S( 47,  35),   S( 47,   6),   S( -5,  11),
            S(  4,  26),   S( 46,  19),   S( 75,  18),   S(103,  14),   S( 58,  40),   S( 74,  32),   S( 38,  15),   S( 41, -18),
            S(-55,  35),   S( 30,  22),   S( 37,  41),   S( 55,  13),   S( 93,  -2),   S(146, -21),   S( 82,  -3),   S( 52, -18),
            S(-22,  27),   S(-50,  43),   S( -3,  35),   S( 42,  37),   S(  8,  55),   S( 39,  -3),   S( -8,  -4),   S( 30, -33),
            S(-139, -64),  S(-28,  34),   S(  7,  36),   S( 33,   4),   S( 13,  18),   S(-38, -29),   S(-14, -20),   S(-34, -48),

            /* knights: QQ */
            S( -7, -120),  S(-42, -22),   S(-28, -10),   S(-66,  -1),   S(-10,   2),   S(-44, -12),   S(-39, -58),   S(-56, -74),
            S(-36, -51),   S(-35,  10),   S(-10, -21),   S( 14, -32),   S( -5, -23),   S( 21, -38),   S(  4, -57),   S(-114, -75),
            S( -1, -16),   S( 17, -15),   S( 13, -12),   S( 27,   7),   S( 18,   2),   S(-13,   9),   S( -9,   3),   S(-18, -51),
            S( -7,  31),   S( 41,   9),   S( 79,   0),   S( 72,   6),   S( 37,  10),   S( 47,   6),   S( 36,   5),   S(-10,   0),
            S( 45,  -8),   S( 39,  -4),   S( 68,   2),   S( 61,   7),   S( 80,  -2),   S( 76,  -2),   S( 61, -17),   S(-12,  17),
            S( 52, -11),   S(  2,  15),   S( 73,  -8),   S(103, -22),   S( 72,  -9),   S( 59,  14),   S( 39,   4),   S( 27,   3),
            S( -6,  19),   S(  0,   7),   S(-18, -10),   S( 38,  30),   S( 14,  23),   S( -3,  17),   S(-26,  28),   S(-16,   7),
            S(-157, -93),  S(-14,  58),   S( -1, -25),   S( -6,  20),   S(-25, -14),   S(  5,   9),   S(-15,  26),   S(-57, -44),

            /* bishops: KK */
            S(  8, -11),   S( 21, -25),   S( -1, -11),   S(-20,  34),   S( -4,  24),   S(-13,  20),   S(-10,  -3),   S( 16, -61),
            S( 20,  -4),   S( 17,  13),   S( 19,  12),   S( 11,  34),   S( 20,  20),   S( 10,  24),   S( 40,  -5),   S( 22, -48),
            S(  2,  28),   S( 21,  37),   S( 27,  62),   S( 24,  44),   S( 16,  69),   S( 25,  36),   S( 21,  20),   S( 10,  15),
            S(  3,  30),   S(  5,  60),   S( 22,  66),   S( 48,  55),   S( 38,  46),   S( 17,  52),   S( 17,  39),   S( 31, -11),
            S( -2,  52),   S( 26,  25),   S( 13,  49),   S( 49,  43),   S( 31,  58),   S( 47,  40),   S( 27,  36),   S( 16,  46),
            S( 12,  30),   S( 16,  53),   S(-11,  57),   S( -1,  56),   S(  3,  62),   S( 53,  72),   S( 57,  42),   S(  9,  70),
            S(-39,  68),   S(-22,  62),   S( -8,  61),   S(-23,  63),   S(-53,  81),   S(-21,  58),   S(-61,  58),   S(-40,  40),
            S(-105,  98),  S(-72,  84),   S(-62,  87),   S(-86,  99),   S(-32,  71),   S(-104,  79),  S( -8,  36),   S(-72,  55),

            /* bishops: KQ */
            S(-45,   7),   S( -9,   7),   S(-20,   3),   S(-24,   4),   S(-21,   4),   S( -9, -19),   S( 53, -84),   S(  9, -66),
            S(-34,  -9),   S(  4,   3),   S(  8,   4),   S(  9,  -2),   S( 29, -14),   S( 43, -28),   S( 80, -46),   S( 16, -65),
            S( -2,   6),   S(  3,  10),   S( 33,  11),   S( 42,   0),   S( 30,  10),   S( 60,  -2),   S( 43, -14),   S( 56, -40),
            S( 33,  13),   S(  8,  25),   S( 20,  16),   S( 65,  -6),   S( 82,  -2),   S( 41,  21),   S( 42,  10),   S(  0,  18),
            S(  6,  20),   S( 60, -10),   S( 66,  -6),   S(118, -44),   S( 88,  -3),   S( 37,  20),   S( 35,   4),   S( 19,  -2),
            S( 15,  -9),   S( 69, -13),   S( 89,  -6),   S(  2,  28),   S( 25,  18),   S( 28,  23),   S( 23,  29),   S(-11,  27),
            S(-64, -10),   S( 18,   0),   S( 10,  11),   S(-15,  24),   S(  3,  26),   S(-12,  35),   S(-42,  53),   S(-66,  61),
            S(-18,   8),   S(-26,  -5),   S( 10,  -6),   S( -5,  32),   S(-39,  35),   S(-53,  43),   S(-37,  43),   S( -7,  16),

            /* bishops: QK */
            S( 71, -72),   S(-11, -29),   S( -7,  15),   S(-36,  19),   S(  4, -11),   S(-14, -10),   S(-45,  11),   S(-118,  24),
            S( 23, -33),   S( 65, -25),   S( 26,  -5),   S( 22,   8),   S(  2,   6),   S( -7,   6),   S(-29,   9),   S(-14, -18),
            S( 62, -22),   S( 43,   2),   S( 46,  13),   S( 23,  22),   S( 17,  36),   S( 13,   9),   S( 17,  -9),   S(-20,  -5),
            S( -5,  25),   S( 38,  28),   S( 33,  32),   S( 78,   4),   S( 63,   2),   S( 17,  29),   S(-26,  31),   S(  3,   9),
            S( 32,  22),   S( 23,  17),   S( 33,  25),   S( 73,   7),   S( 49,   9),   S( 47,  19),   S( 41,  -2),   S(-32,  42),
            S(-50,  48),   S( 22,  48),   S( 20,  24),   S( 17,  38),   S( 37,  35),   S( 70,  20),   S( 57,  -2),   S( 20,  21),
            S(-41,  66),   S(-16,  44),   S(  3,  39),   S(-27,  53),   S(-72,  59),   S( -6,  32),   S( -4,  12),   S(-81,  17),
            S(-89,  50),   S(-69,  64),   S(-54,  46),   S(-29,  61),   S( 23,  14),   S(-30,  18),   S(-18,   0),   S(-60,  -1),

            /* bishops: QQ */
            S(-88, -17),   S(-41, -17),   S( -8, -33),   S(-82,   5),   S(-17, -18),   S(-10, -37),   S( 39, -67),   S( 13, -71),
            S( 13, -42),   S( 45, -42),   S( 19, -26),   S( 24, -23),   S( -7, -12),   S( 52, -47),   S( 12, -36),   S( 45, -81),
            S( 11, -10),   S( 40, -29),   S( 36, -17),   S( 17, -17),   S( 32, -13),   S( 40, -19),   S( 39, -44),   S( 12, -29),
            S( 20,   5),   S( 34, -14),   S( 35, -23),   S( 78, -32),   S( 52, -22),   S( 59, -20),   S( 24, -24),   S(  8, -27),
            S( 17,   0),   S( 55, -21),   S( 24, -24),   S( 87, -43),   S( 87, -44),   S( 31, -25),   S( 42, -29),   S(-26,   0),
            S(-25, -11),   S( 82, -14),   S( 70, -27),   S( 80, -22),   S( 43,  -1),   S( -2,  -9),   S(-13,  -7),   S(-17,  -6),
            S(-58, -27),   S( -3,  -7),   S( 40,  -8),   S( -9,   0),   S( 25, -14),   S(-42,   6),   S(-23,   8),   S(-36,   5),
            S(-13, -15),   S( -3, -13),   S( -5,  -5),   S( 19,   4),   S( 21,  14),   S(-29,   9),   S(-27,  14),   S(-27,  20),

            /* rooks: KK */
            S(-32,  89),   S(-21,  75),   S(-20,  65),   S(-17,  44),   S(-16,  25),   S(-34,  53),   S( -7,  47),   S(-34,  35),
            S(-44,  88),   S(-37,  88),   S(-28,  68),   S(-17,  41),   S(-21,  36),   S( -6,  14),   S( 20,  24),   S(-18,  36),
            S(-45,  89),   S(-36,  89),   S(-32,  77),   S(-23,  49),   S( -7,  32),   S( -6,  35),   S( 20,  36),   S( -2,  24),
            S(-41, 113),   S(-24, 108),   S(-16,  90),   S(-11,  73),   S(-19,  60),   S(-21,  84),   S( 31,  71),   S(-18,  66),
            S(-18, 132),   S( -4, 121),   S(  6, 100),   S( 20,  77),   S( -1,  92),   S( 40,  73),   S( 70,  82),   S( 13,  85),
            S(  3, 131),   S( 33, 109),   S( 28,  99),   S( 25,  87),   S( 63,  63),   S(119,  55),   S(111,  67),   S( 65,  78),
            S(-12, 115),   S( -6, 111),   S( 19,  80),   S( 43,  55),   S( 33,  61),   S(119,  40),   S( 94,  76),   S(115,  59),
            S( 79,  91),   S( 81, 100),   S( 58,  89),   S( 75,  64),   S( 41,  91),   S(102,  86),   S(134,  87),   S(145,  67),

            /* rooks: KQ */
            S(-49,  10),   S( -9, -29),   S(-25, -25),   S(-38, -13),   S(-49,  -6),   S(-60,   5),   S(-53,  20),   S(-74,  32),
            S(-64,   0),   S( -4, -38),   S(-39, -24),   S(-32, -22),   S(-36, -19),   S(-39,  -3),   S(-48,  11),   S(-71,   3),
            S(-64,   8),   S(-29, -22),   S(-51, -10),   S(-18, -30),   S(-53, -15),   S(-47,  -3),   S(-27,   9),   S(-43,   2),
            S(-55,  21),   S(-11,   0),   S(-43,  15),   S(-14,   3),   S(-19,   4),   S(-45,  30),   S( -1,  32),   S(-40,  25),
            S(-22,  32),   S( 17,  11),   S( -5,  11),   S(  7,   1),   S( 18,  -2),   S( 28,   5),   S( 43,  19),   S( -2,  33),
            S( 41,  20),   S( 91,  -7),   S( 49,  -5),   S(106, -36),   S( 48,  -8),   S( 81,  -4),   S(104,   4),   S( 65,  20),
            S( -5,  25),   S( 27,   5),   S( 79, -26),   S( 69, -25),   S( 69, -26),   S( 41, -10),   S( 65,  12),   S( 51,  11),
            S( 39,  41),   S( 50,  34),   S( 93,  -8),   S( 86,  -9),   S( 88, -12),   S(128, -11),   S( 98,  23),   S(113,  -1),

            /* rooks: QK */
            S(-87,  42),   S(-44,  29),   S(-56,  20),   S(-51, -11),   S(-26, -27),   S(-27, -20),   S(-10, -28),   S(-43,  -1),
            S(-50,  19),   S(-58,  22),   S(-53,  15),   S(-43,  -9),   S(-61, -12),   S(-40, -32),   S(-21, -33),   S(-45, -25),
            S(-43,  18),   S(-22,  20),   S(-53,  25),   S(-50,  -8),   S(-42,  -4),   S(-53,  -4),   S(-12, -30),   S(-38, -19),
            S(-21,  31),   S(  7,  16),   S(-26,  25),   S(-24,  12),   S(-40,   8),   S(-50,  19),   S(-20,   9),   S(-35,   7),
            S( 48,  20),   S( 41,  32),   S( 23,  27),   S(-15,  30),   S( -3,  16),   S( -8,  27),   S( 24,  14),   S( 18,  -5),
            S( 55,  37),   S(107,  19),   S( 77,  13),   S( -2,  36),   S( 51,   2),   S( 77,   6),   S(108,   0),   S( 35,  26),
            S( 50,  12),   S( 74,   5),   S( 69,  -6),   S( -5,  17),   S( -9,  14),   S( 88, -25),   S(  3,  17),   S( 22,  21),
            S(247, -94),   S(166, -18),   S( 74,  19),   S( 36,  15),   S( 20,  25),   S( 69,   8),   S( 48,  29),   S( 66,  21),

            /* rooks: QQ */
            S(-55, -26),   S(-23, -43),   S(-28, -51),   S(-19, -67),   S( -3, -68),   S(-12, -29),   S(-16, -19),   S(-35,   3),
            S(-17, -57),   S(-35, -30),   S(-56, -30),   S( -2, -78),   S(-16, -68),   S(-16, -59),   S(-20, -38),   S(-44,  -5),
            S(-75, -13),   S( -6, -41),   S(  7, -60),   S(-14, -45),   S(-44, -31),   S(-44, -16),   S(-53,   2),   S(-51,   5),
            S(-53, -11),   S( 12, -21),   S(-40,  -7),   S( -7, -35),   S(-31, -28),   S(-20,  -7),   S(-36,  14),   S(-36,  17),
            S( 38, -17),   S( 73, -23),   S( 25, -31),   S( 74, -37),   S( 67, -42),   S( 67, -23),   S( 34,   8),   S( -8,  13),
            S( 84, -17),   S( 73, -24),   S(105, -39),   S( 68, -24),   S( 59, -27),   S( 67, -20),   S( 77,  -8),   S( 36,   9),
            S( 41, -10),   S( 81, -22),   S( 40, -26),   S( 53, -37),   S(115, -77),   S( 92, -60),   S( 38,  -9),   S( 20,   8),
            S( 20,  26),   S( -3,  43),   S(  6,  12),   S(  2,  15),   S( 53, -23),   S( 61, -12),   S( 68,  15),   S( 60,  19),

            /* queens: KK */
            S( 23,  50),   S( 33,   9),   S( 37, -13),   S( 37, -13),   S( 49, -75),   S( 19, -98),   S(-29, -72),   S( -5,   7),
            S( 16,  71),   S( 37,  43),   S( 37,  31),   S( 45,  13),   S( 44,   4),   S( 50, -40),   S( 72, -90),   S( 28, -29),
            S( 13,  59),   S( 28,  55),   S( 33,  60),   S( 18,  66),   S( 28,  49),   S( 25,  65),   S( 36,  56),   S( 36,  15),
            S( 20,  78),   S(  9,  90),   S( 12,  91),   S( 13,  94),   S(  3,  89),   S( 12, 102),   S( 30,  95),   S( 11, 116),
            S( 19,  98),   S( 18, 104),   S( -6, 119),   S( -7, 126),   S( -7, 148),   S(  0, 176),   S(  8, 192),   S( 18, 166),
            S(  3, 106),   S( 17, 131),   S(-19, 148),   S(-36, 166),   S(  3, 172),   S( 55, 176),   S( 28, 211),   S(  7, 238),
            S(-29, 167),   S(-42, 194),   S(-21, 173),   S(-22, 173),   S(-75, 238),   S( 44, 162),   S( 23, 251),   S( 87, 187),
            S( -9, 138),   S( 23, 124),   S( 42, 113),   S( 29, 123),   S( 37, 136),   S(110, 133),   S(159, 107),   S(136, 139),

            /* queens: KQ */
            S( 28, -24),   S( 24, -63),   S( 12, -87),   S( 19, -104),  S( 18, -69),   S(-13, -91),   S(-56, -51),   S(-70, -24),
            S( 22,   7),   S( 48, -61),   S( 49, -101),  S( 15, -49),   S( 33, -65),   S(  9, -39),   S(  0, -86),   S(-51, -33),
            S( 10,   8),   S( 20, -41),   S( 39, -82),   S( 27, -53),   S( 16, -34),   S( 30, -17),   S( -8,  -1),   S(-19,   0),
            S( 10,  13),   S( 40, -41),   S(-11,   3),   S(  6, -20),   S( 18,  -3),   S( -2,  33),   S(-11,  47),   S(-10,  25),
            S(  0,  32),   S( 48, -20),   S(  2,  36),   S( 22,  -5),   S( 42,   2),   S(-37,  65),   S( -3,  68),   S( -6,  67),
            S( 31,  21),   S(105,   1),   S( 59,  29),   S( 78, -10),   S( 21,  27),   S( 23,  35),   S( 14,  55),   S(-15,  71),
            S( 71,  37),   S( 58,  17),   S( 38,  13),   S( 54,  52),   S( 39,  12),   S(-27,  93),   S(  8,  76),   S( -2,  52),
            S(102,  10),   S(108,  20),   S( 88,  49),   S(100,  13),   S( 21,  48),   S( 67,  14),   S( 68,  10),   S(  7,  14),

            /* queens: QK */
            S(-46, -78),   S(-93, -67),   S(-26, -118),  S(-11, -84),   S( 23, -141),  S(-15, -80),   S(-23, -54),   S(-49,   6),
            S(-55, -61),   S(-34, -72),   S( 24, -112),  S( 17, -68),   S( 20, -78),   S( 18, -76),   S( 23, -58),   S( 14, -30),
            S(-37, -15),   S(-11, -11),   S( -1, -14),   S(-10,  -2),   S( -4, -24),   S( -9, -28),   S( 21, -43),   S(  1,  -7),
            S( -9, -11),   S(-20,  30),   S(-31,  48),   S( -5,  12),   S(-17,  20),   S( -6,   2),   S( 12, -18),   S( 12, -11),
            S(-12,  13),   S(-16,  40),   S(-24,  66),   S(-45,  54),   S( -7,  31),   S( -1,  26),   S(  6,  16),   S( -9,  -1),
            S(-12,  22),   S( 16,  44),   S(-25,  78),   S(-37,  66),   S( 16,  44),   S( 48,  31),   S( 17,  35),   S( 12,  46),
            S(-24,  47),   S(-49,  70),   S(-31,  81),   S(-51,  91),   S(-35,  72),   S(-17,  46),   S( 16,  52),   S( -7,  87),
            S(-22,  -1),   S( 24,  42),   S( 29,  28),   S( 20,  35),   S( 32,  22),   S( 17,  44),   S( 63,  24),   S( 76,  41),

            /* queens: QQ */
            S(-55, -62),   S(-89, -59),   S(-58, -65),   S(-41, -54),   S( -1, -77),   S(-23, -58),   S(-49, -38),   S(-26, -33),
            S(-100, -53),  S(-47, -66),   S( 14, -107),  S( 15, -92),   S( -3, -67),   S(-11, -80),   S(-22, -11),   S(-53, -18),
            S(-19,   5),   S(-14,  12),   S( -3, -53),   S(-19, -11),   S(-30,  -4),   S(-10, -20),   S(  1,   5),   S(-18,  -4),
            S(-12, -30),   S( 20, -57),   S(-11, -36),   S(-29, -15),   S( 13, -51),   S( -9, -39),   S(-27, -32),   S(-34,  35),
            S(-23,  -2),   S( -3,  31),   S(  9, -18),   S(  5, -19),   S( 19, -10),   S( -4, -38),   S(  6,   0),   S(  4, -24),
            S( 14,  20),   S( 16,  50),   S( 87,  22),   S( 10,  26),   S(-21, -11),   S( 11,  -4),   S( 19,  -3),   S( 15,   4),
            S(101,  36),   S( 26,  15),   S( 27,  35),   S( 21,  31),   S(  8,  25),   S(  3,   3),   S(  4,  29),   S(-15,  22),
            S( 71, -21),   S( 35,  22),   S( 22,  20),   S( 11,   0),   S( 13,   9),   S( 23, -15),   S( 36, -16),   S( 33,  -4),

            /* kings: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-89,  -1),   S(-94,  16),   S(-40,  -4),   S(-71, -31),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-25,   5),   S(-25,  10),   S( -6,  -6),   S(-63,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -2,  23),   S(-40,  24),   S(-33,   9),   S(-110,  10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 95,  13),   S( 36,  26),   S(-13,  26),   S(-119,  21),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(234, -13),   S(254, -28),   S(138,   4),   S(-15,   8),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(227, -18),   S(272, -24),   S(278, -10),   S(161, -10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(192, -14),   S(141, -11),   S(161,  35),   S( 42,   3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 61,   7),   S( 38,  34),   S( 45,  26),   S(-14, -168),

            /* kings: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-120,  10),  S(-131,  14),  S(-65, -12),   S(-94, -15),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-34,  -1),   S(-40,   0),   S(-43,   5),   S(-90,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-22,  17),   S(-13,   5),   S(-40,  10),   S(-129,  15),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(107,   7),   S( 66,  19),   S(  9,  32),   S(-98,  33),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(202,   1),   S(151,  21),   S( 80,  37),   S(-53,  47),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(157,  12),   S(108,  37),   S( 92,  40),   S( -6,  47),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(119,   4),   S(175,   9),   S(123,  20),   S( 19,   9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 76,  -7),   S( 47,  -4),   S( 65,   1),   S( 11, -24),

            /* kings: QK */
            S(-75, -77),   S(-39, -67),   S(-75, -40),   S(-129, -15),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-66, -45),   S(-39, -26),   S(-48, -29),   S(-64, -22),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-145,   1),  S(-50, -10),   S(-47,  -8),   S(-65,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-109,  20),  S(-29,  15),   S( 45,  -6),   S( 51,  -4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-47,  11),   S( 26,  21),   S(107,   1),   S(163, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-52,  26),   S(108,  13),   S(131,   6),   S(166, -10),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-12,  -1),   S(154,  -3),   S(106,   6),   S(132,  -7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-38, -28),   S( 32,  17),   S(110, -12),   S( 41,  -5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: QQ */
            S(-178,  39),  S(-97,  -9),   S(-141,  10),  S(-157,  -5),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-145,   7),  S(-80,  -6),   S(-70, -12),   S(-107,  -6),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-145,   2),  S(-84,  -6),   S(-40,  -8),   S(-70,   5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-48, -15),   S( 13,   8),   S( 65,   1),   S( 99,  -8),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-17,   3),   S(143,  -5),   S(178,  -5),   S(224, -24),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 71,   8),   S(147,  13),   S(150, -10),   S(134,   7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  7,   6),   S( 48,  49),   S( 48,  23),   S(128,   4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-43, -126),  S( 15,  64),   S( 46,  17),   S( 30,   6),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            #endregion

            /* mobility weights */
            S(  8,   5),    // knights
            S(  4,   4),    // bishops
            S(  2,   4),    // rooks
            S(  1,   3),    // queens

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
            S( 37, -24),

            /* castling complete */
            S( 14, -15),

            /* king on open file */
            S(-57,  13),

            /* king on half-open file */
            S(-25,  22),

            /* king on open diagonal */
            S( -4,   8),

            /* king attack square open */
            S(-12,  -1),

            /* isolated pawns */
            S(  1, -16),

            /* doubled pawns */
            S(-12, -31),

            /* backward pawns */
            S(  9, -14),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 16,   5),   S(  2,  -2),   S(  8,   6),   S( 15,  16),   S( 24,  35),   S(  3, -29),   S(-21,  48),   S(  3, -36),
            S(  2,   4),   S( 29,   3),   S(  7,  29),   S( 17,  43),   S( 42,   1),   S(  1,  30),   S( 14,   0),   S( 11,  -6),
            S(-13,  26),   S( 17,   9),   S(  0,  57),   S( 24,  73),   S( 33,  31),   S( 27,  38),   S( 27,  11),   S(  7,  26),
            S( 21,  47),   S( 23,  48),   S( 37,  85),   S( 14,  84),   S( 79,  62),   S( 74,  46),   S( 20,  49),   S( 18,  55),
            S( 96,  80),   S(128,  90),   S( 89, 154),   S(142, 150),   S(166, 116),   S(151, 122),   S(154,  74),   S( 83,  54),
            S( 94, 224),   S(135, 312),   S(113, 239),   S(104, 206),   S( 75, 176),   S( 65, 169),   S( 53, 198),   S( 27, 134),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  1,  13),   S(-11,  19),   S(-25,  26),   S(-44,  63),   S( 21, -10),   S(-22,  15),   S( -9,  49),   S( 20,   8),
            S( 11,  24),   S(  0,  35),   S(-11,  30),   S( -5,  25),   S(-20,  35),   S(-51,  47),   S(-50,  68),   S( 19,  21),
            S(  8,  14),   S(  8,  20),   S( -4,  26),   S( 14,  29),   S( -4,  24),   S(-36,  38),   S(-54,  66),   S(-22,  44),
            S( 41,  24),   S( 69,  35),   S( 33,  34),   S( 16,  29),   S( 21,  53),   S( 56,  43),   S( 31,  56),   S(-18,  68),
            S( 60,  63),   S(107,  92),   S( 98,  64),   S( 45,  46),   S(-22,  31),   S( 83,  42),   S( 54,  83),   S( 11,  49),
            S(249,  68),   S(230, 106),   S(225, 116),   S(218, 114),   S(206, 123),   S(211, 118),   S(257, 121),   S(263,  98),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 39,   9),   S(  7,  17),   S( 17,  25),   S( 16,  50),   S( 78,  37),   S( 29,  18),   S( 18,   3),   S( 44,  10),
            S(  6,  14),   S(  5,  10),   S( 22,  12),   S( 21,  27),   S( 16,  13),   S( -3,   6),   S( 10,   3),   S( 31,  -6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -6, -14),   S( -5, -10),   S(-22, -12),   S(-21, -27),   S(-16, -13),   S(  3,  -6),   S(-10,  -3),   S(-31,   6),
            S(-39,  -9),   S( -7, -17),   S(-17, -25),   S(-16, -50),   S(-78, -37),   S(-29, -18),   S(-18,  -3),   S(-44, -10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 34,   1),   S( 38,  11),   S( 54,  15),   S( 52,  16),   S( 37,  25),   S( 35,  13),   S( 19,   9),   S( 58, -12),
            S(  0,   3),   S( 24,  19),   S( 20,  13),   S( 26,  37),   S( 30,  12),   S( 14,  14),   S( 33,   4),   S( 17,   1),
            S( -5,  10),   S( 22,  28),   S( 55,  34),   S( 50,  31),   S( 58,  32),   S( 55,  17),   S( 25,  30),   S( 27,  10),
            S( 48,  64),   S(109,  41),   S(121,  74),   S(146,  62),   S(137,  60),   S( 83,  74),   S( 96,  24),   S( 87,   7),
            S( 56,  82),   S(184,  70),   S(216, 119),   S(182,  79),   S(200, 115),   S(162,  99),   S(206,  50),   S( -9,  70),
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
            S(  0,   0),   S( 71, -49),   S( 42,   8),   S( 39,  20),   S( 54,  35),   S( 48,  10),   S(186, -19),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 13,  -6),   S( 13,  55),   S(  9,  49),   S(  8,  75),   S( 40,  75),   S(165,  87),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-37,  -2),   S(-10, -37),   S(  1, -38),   S(-24, -20),   S( 27, -50),   S(254, -124),  S(  0,   0),    // blocked by rooks
            S(  0,   0),   S( -6,   0),   S( 34, -22),   S(  0,  18),   S( 11, -48),   S( 18, -190),  S( -4, -204),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(  3,  -1),   S( 81,   1),   S( 10, -39),   S( 75, -23),   S(201, -11),   S(354,  56),   S(  0,   0),    // blocked by kings

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
            S( -4,  -1),   S( -1,  -6),   S( -4,  -4),   S( -4, -45),   S( -9, -16),   S(-24,   5),   S(-22,  -6),   S(  0,  -5),
            S( -5,  -8),   S( -8,  -8),   S(-15, -10),   S(-10, -11),   S(-12, -17),   S(-15,  -6),   S(-14,  -7),   S( -4,  -6),
            S( -5,  -9),   S(  5, -26),   S( -3, -38),   S( -3, -50),   S(-13, -34),   S(-11, -28),   S(-13, -15),   S(  0,  -7),
            S(  6, -25),   S(  9, -36),   S( -7, -25),   S( -4, -40),   S( -7, -33),   S( -9, -23),   S( -2, -26),   S( -5, -17),
            S( 13, -16),   S(  8, -40),   S( 10, -51),   S(  8, -52),   S( 20, -57),   S(  9, -50),   S(  7, -64),   S( -2,  -7),
            S( 59, -33),   S( 85, -88),   S( 75, -98),   S( 68, -118),  S( 88, -141),  S(122, -113),  S(106, -130),  S(102, -110),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 39,  -2),

            /* rook on half-open file */
            S( 12,  34),

            /* rook on seventh rank */
            S(  6,  45),

            /* doubled rooks on file */
            S( 16,  27),

            /* queen on open file */
            S(-12,  35),

            /* queen on half-open file */
            S(  3,  37),

            /* pawn push threats */
            S(  0,   0),   S( 30,  28),   S( 34, -16),   S( 35,  22),   S( 33, -18),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 73,  73),   S( 57,  96),   S( 59,  87),   S( 56,  35),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-13,   7),   S( 50,  34),   S( 92,   4),   S( 42,  17),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 31,  66),   S(  2,  27),   S( 53,  54),   S( 42,  97),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-11,  58),   S( 59,  52),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-23,  17),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats

            /* tempo bonus for side to move */
            S( 18,   7),
        };
    }
}
