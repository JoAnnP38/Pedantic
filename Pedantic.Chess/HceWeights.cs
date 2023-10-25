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

        // Solution sample size: 12000000, generated on Wed, 25 Oct 2023 10:49:47 GMT
        // Solution K: 0.003872, error: 0.125425, accuracy: 0.4918
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(108, 146),   S(450, 472),   S(492, 516),   S(605, 900),   S(1423, 1594), S(  0,   0),

            /* piece square values */
            #region piece square values

            /* pawns: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-51,  13),   S(-55,  15),   S(-40,   4),   S(-23, -14),   S(-39,  29),   S( 50,  -5),   S( 55, -30),   S( -6, -35),
            S(-55,   8),   S(-49,  -2),   S(-52, -19),   S(-51, -14),   S(-36, -16),   S(-14, -16),   S( 11, -25),   S(-40, -18),
            S(-27,   9),   S(-36,   9),   S(-16, -24),   S(-30, -42),   S(-18, -27),   S( 13, -27),   S( -6, -11),   S(-12, -22),
            S(-25,  63),   S(-29,  30),   S( -2,  11),   S( 10,  -2),   S( 18, -20),   S( 14, -22),   S( 21,   0),   S( 54, -17),
            S( 43,  59),   S(-31,  61),   S(-21,  57),   S( 28,  59),   S(103,  71),   S(146,   9),   S(144,  -5),   S(134,   6),
            S( 85,  80),   S( 71,  95),   S( 77,  82),   S(101,  43),   S( 65,  59),   S( 84, -11),   S( 65,  36),   S( 85, -15),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-85,  27),   S(-93,  39),   S(-55,  13),   S(-60,  37),   S(-34,  -8),   S( 49, -34),   S( 56, -45),   S( -1, -23),
            S(-83,   4),   S(-88,   6),   S(-64, -20),   S(-62, -19),   S(-30, -42),   S(-22, -27),   S( 15, -40),   S(-30, -16),
            S(-61,  21),   S(-45,  12),   S(-35, -12),   S(-28, -40),   S(-26, -26),   S( -3, -24),   S(-10, -23),   S(  7, -24),
            S(-63,  66),   S(-18,   5),   S(  8,  -6),   S(  7,   5),   S( -3, -11),   S(-32,  24),   S( 19,   6),   S( 58,  -3),
            S( 65,  25),   S( 62, -19),   S( 89,  -9),   S( 82,  34),   S(125,  97),   S( 55,  86),   S( 41,  80),   S( 89,  76),
            S(101,   2),   S( 32,  41),   S(134, -23),   S( 80,  69),   S( 67,  91),   S( 89, 133),   S(131,  91),   S(146,  91),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-26,   7),   S( 11,  -3),   S( -5,  -6),   S(-22,  16),   S(-41,  15),   S(-11,   1),   S(-26,  -1),   S(-66,  19),
            S(-21,   4),   S(  0, -14),   S(-39, -16),   S(-54, -22),   S(-37, -22),   S(-33, -22),   S(-48, -14),   S(-97,   8),
            S( -1,  10),   S( -2,   9),   S(-17,  -8),   S(-22, -36),   S(-37, -13),   S(-13, -25),   S(-25, -16),   S(-46,  -3),
            S( -4,  61),   S( -9,  51),   S( 13,  27),   S( 18,  16),   S(  0,  -4),   S( -9, -20),   S(  0, -12),   S(  2,   5),
            S( 50,  76),   S(-24, 102),   S( -9, 103),   S( 51,  70),   S( 98,  67),   S(106,  13),   S(108, -17),   S( 57,  34),
            S(154,  48),   S(212,  60),   S(113, 130),   S(124,  79),   S( 52,  62),   S( 60,   0),   S( 59,  22),   S( 17,  28),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-54,   3),   S(  3, -14),   S( 32, -31),   S( 16, -24),   S( -1, -26),   S( 15, -29),   S(  8, -38),   S(-25, -14),
            S(-48,  -6),   S(-16, -16),   S(-28, -26),   S(-31, -33),   S(-12, -44),   S(-32, -44),   S(-22, -44),   S(-61, -17),
            S(-17,   6),   S(-21,   2),   S(  9, -26),   S( -2, -48),   S( -2, -35),   S(  2, -47),   S( -2, -37),   S(-33, -22),
            S( 43,  12),   S( 58, -19),   S( 55, -26),   S( 54,  -8),   S( 13, -22),   S(-13, -21),   S( 13, -25),   S( 38, -16),
            S(209, -53),   S(163, -51),   S(107, -21),   S(117,  20),   S( 59,  59),   S( 49,  35),   S( 14,  18),   S( 93,  30),
            S(187, -27),   S( 81,  21),   S( 89, -30),   S( 42,  44),   S(110,  12),   S( 51,  28),   S( 91,  48),   S(127,  45),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: KK */
            S(-125, -27),  S(-16, -73),   S(-43, -12),   S(-24,  13),   S(-12,   3),   S(-10,  13),   S(-12, -27),   S(-55, -45),
            S(-36, -49),   S(-51,  28),   S( -7,   2),   S( 10,   8),   S(  6,  15),   S(  9,  27),   S(  3,  18),   S(-10,  11),
            S(-23,  -9),   S( -7,  24),   S(  0,  38),   S( 21,  56),   S( 30,  57),   S( 13,  31),   S(  6,  40),   S(  3,  10),
            S(-12,  35),   S( 12,  56),   S( 24,  82),   S( 26,  91),   S( 37,  94),   S( 45,  81),   S( 46,  79),   S( 20,  41),
            S(  4,  48),   S( 14,  54),   S( 39,  57),   S( 44,  79),   S( 15, 100),   S( 38, 105),   S( 15, 106),   S( 49,  50),
            S(-25,  61),   S(  3,  55),   S( 26,  67),   S( 35,  69),   S( 69,  49),   S(138,  41),   S( 29,  82),   S(-10,  68),
            S(-53,  58),   S(-40,  67),   S(-23,  71),   S( 30,  66),   S( 42,  74),   S( 93,  11),   S(-17,   6),   S(  2,  18),
            S(-315,  57),  S(-52,  73),   S(-111,  87),  S( 20,  54),   S( 54,  41),   S(-100,  33),  S( 10,  19),   S(-268, -51),

            /* knights: KQ */
            S(-65, -151),  S(-33, -45),   S(-43,  -6),   S(-24, -17),   S(-12, -11),   S(-10, -12),   S(-28, -21),   S(-24, -93),
            S(-55, -41),   S(-61,  20),   S(  1, -31),   S( 16, -20),   S(  3,  -8),   S( 33,  -1),   S(-45,  13),   S(-38, -19),
            S(-27, -19),   S( 20, -35),   S( -4,  20),   S( 46,   3),   S( 23,   7),   S( 30, -15),   S( -6,  16),   S(-31,  35),
            S(  4,  -1),   S( 71, -22),   S( 41,  17),   S( 58,  17),   S( 50,  34),   S( 49,  26),   S( 55,   1),   S(  0,   7),
            S( 61, -30),   S( 27, -10),   S( 73,   7),   S( 82,   3),   S( 89,  10),   S( 45,  18),   S( 46,   8),   S( 18,   1),
            S( 42, -40),   S( 58, -59),   S(125, -58),   S(150, -46),   S( 75,   3),   S( 53,  10),   S( 11,  27),   S(  6,  17),
            S(-10, -11),   S(-12, -38),   S( 15, -33),   S( 80, -18),   S(  8,  15),   S( 35,  15),   S(-13,  24),   S(-34, -11),
            S(-285, -76),  S(-21, -39),   S(  1, -27),   S(  3, -15),   S(-22, -33),   S( 20,  22),   S(-38,   0),   S(-162, -57),

            /* knights: QK */
            S(-125, -86),  S(-65,  21),   S(-38,   8),   S(-67,  33),   S(-41, -10),   S(-33, -29),   S(-49, -33),   S(-71, -84),
            S(-14, -50),   S(  5,  14),   S( -4,  -1),   S(  3,   4),   S(  0,   2),   S(-13,  -1),   S(-62,   2),   S(-21, -64),
            S(-18,  31),   S( -5,  -4),   S( 35, -15),   S( 39,  23),   S( 46,   7),   S(-11,  18),   S( 14, -13),   S(-30, -39),
            S( 12,  24),   S( 73,   5),   S( 65,  27),   S( 50,  43),   S( 48,  24),   S( 49,  30),   S( 44,   2),   S( -7,  11),
            S(  0,  26),   S( 43,  21),   S( 79,  15),   S(110,   8),   S( 58,  35),   S( 76,  25),   S( 36,  11),   S( 39, -23),
            S(-72,  39),   S( 31,  20),   S( 41,  36),   S( 52,   9),   S( 85,  -9),   S(142, -28),   S( 81,  -7),   S( 55, -22),
            S(-24,  28),   S(-61,  44),   S( -6,  30),   S( 37,  34),   S(  9,  53),   S( 27,  -3),   S(-23,  -7),   S( 23, -39),
            S(-143, -68),  S(-35,  35),   S(  3,  32),   S( 33,   0),   S(  6,  16),   S(-48, -38),   S(-17, -24),   S(-38, -49),

            /* knights: QQ */
            S(  0, -122),  S(-34, -26),   S( -9, -14),   S(-49,  -7),   S(  3,   1),   S(-37, -13),   S(-29, -63),   S(-61, -76),
            S(-29, -57),   S(-31,   8),   S(  2, -25),   S( 28, -40),   S(  1, -28),   S( 30, -43),   S(  1, -59),   S(-119, -82),
            S(  4, -24),   S( 21, -19),   S( 17, -19),   S( 31,   2),   S( 19,  -2),   S(-11,   7),   S(-11,   1),   S(-18, -56),
            S(-12,  32),   S( 43,   7),   S( 87,  -6),   S( 77,   0),   S( 40,   3),   S( 48,   2),   S( 37,   2),   S( -9,  -5),
            S( 44, -13),   S( 40,  -9),   S( 69,  -3),   S( 59,   2),   S( 83, -10),   S( 80,  -7),   S( 58, -20),   S(-18,  20),
            S( 58, -18),   S( -7,  13),   S( 69, -12),   S( 93, -26),   S( 69, -14),   S( 67,   8),   S( 47,  -3),   S( 30,   2),
            S( -8,  14),   S( -5,   4),   S(-33, -15),   S( 37,  28),   S(  9,  19),   S( -8,  11),   S(-34,  25),   S(-21,   7),
            S(-176, -103), S(-15,  55),   S( -3, -33),   S(-11,  15),   S(-37, -22),   S(  1,   1),   S(-22,  26),   S(-59, -49),

            /* bishops: KK */
            S(  4, -11),   S( 20, -24),   S(  4,  -9),   S(-16,  41),   S(  5,  31),   S(  8,  29),   S(  7,   5),   S( 17, -60),
            S( 18,  -5),   S( 14,  15),   S( 15,  17),   S( 11,  39),   S( 21,  30),   S( 20,  30),   S( 44,   7),   S( 19, -46),
            S( -1,  32),   S( 17,  38),   S( 24,  64),   S( 23,  48),   S( 17,  74),   S( 24,  45),   S( 16,  28),   S(  8,  20),
            S( -2,  36),   S(  2,  62),   S( 21,  69),   S( 46,  56),   S( 38,  50),   S( 17,  55),   S( 17,  44),   S( 28, -13),
            S( -8,  58),   S( 23,  29),   S(  8,  52),   S( 46,  45),   S( 27,  60),   S( 43,  43),   S( 26,  35),   S( 12,  50),
            S( 12,  32),   S( 11,  58),   S(-16,  62),   S( -7,  59),   S( -9,  67),   S( 45,  75),   S( 50,  43),   S(  2,  73),
            S(-50,  75),   S(-23,  66),   S(-10,  63),   S(-24,  63),   S(-60,  84),   S(-36,  62),   S(-71,  58),   S(-54,  44),
            S(-111, 105),  S(-85,  88),   S(-77,  91),   S(-110, 107),  S(-60,  80),   S(-130,  87),  S(-20,  37),   S(-95,  59),

            /* bishops: KQ */
            S(-45,   8),   S(-11,  10),   S(-11,   7),   S(-13,   4),   S( -6,   4),   S( 19, -20),   S( 84, -93),   S( 12, -69),
            S(-36, -10),   S(  3,   6),   S( 10,   7),   S( 14,   1),   S( 36,  -9),   S( 61, -31),   S( 92, -45),   S( 14, -66),
            S(  1,   5),   S(  0,  11),   S( 33,  12),   S( 49,  -1),   S( 32,  13),   S( 67,   0),   S( 42, -11),   S( 61, -45),
            S( 39,  11),   S(  9,  23),   S( 23,  14),   S( 66, -10),   S( 88,  -4),   S( 39,  24),   S( 47,  10),   S( -5,  25),
            S(  0,  20),   S( 65, -17),   S( 70, -13),   S(122, -52),   S( 90,  -5),   S( 34,  21),   S( 38,   5),   S( 21,   0),
            S(  6, -11),   S( 62, -16),   S( 82,  -8),   S( -7,  29),   S( 17,  20),   S( 23,  26),   S( 25,  32),   S(-12,  29),
            S(-81, -13),   S( 15,  -2),   S(  3,  10),   S(-29,  25),   S(  5,  24),   S(-14,  33),   S(-43,  55),   S(-72,  67),
            S(-19,   5),   S(-40,  -8),   S(  6,  -9),   S( -4,  32),   S(-49,  34),   S(-63,  42),   S(-44,  42),   S( -2,  14),

            /* bishops: QK */
            S( 85, -79),   S( -2, -31),   S( 11,  16),   S(-14,  14),   S( 15, -13),   S( -6,  -5),   S(-52,  13),   S(-121,  24),
            S( 17, -33),   S( 78, -25),   S( 40,  -6),   S( 36,   9),   S(  5,  10),   S( -8,   8),   S(-30,  15),   S(-17, -19),
            S( 68, -24),   S( 44,   2),   S( 49,  16),   S( 27,  26),   S( 23,  35),   S( 12,  10),   S( 13, -11),   S(-19,  -6),
            S( -6,  29),   S( 41,  29),   S( 34,  35),   S( 81,   3),   S( 67,   0),   S( 20,  26),   S(-29,  28),   S( -1,   9),
            S( 37,  21),   S( 22,  20),   S( 30,  29),   S( 72,   7),   S( 44,   7),   S( 46,  17),   S( 43,  -9),   S(-38,  43),
            S(-54,  54),   S( 22,  52),   S( 16,  27),   S( 13,  40),   S( 29,  35),   S( 69,  16),   S( 54,  -7),   S( 10,  22),
            S(-37,  68),   S(-10,  44),   S(  4,  37),   S(-27,  53),   S(-85,  62),   S(-14,  33),   S( -8,   9),   S(-97,  19),
            S(-96,  52),   S(-77,  67),   S(-63,  44),   S(-41,  61),   S( 18,  11),   S(-37,  17),   S(-27,  -6),   S(-71,  -6),

            /* bishops: QQ */
            S(-99, -12),   S(-21, -18),   S( 12, -35),   S(-71,   4),   S( -9, -18),   S(  3, -39),   S( 44, -73),   S( 27, -76),
            S( 16, -46),   S( 58, -40),   S( 46, -30),   S( 36, -22),   S(  2, -11),   S( 58, -51),   S( 18, -38),   S( 43, -86),
            S( 14, -10),   S( 48, -32),   S( 38, -13),   S( 26, -18),   S( 37, -15),   S( 46, -22),   S( 40, -49),   S( 19, -33),
            S( 27,   1),   S( 41, -19),   S( 43, -26),   S( 80, -34),   S( 57, -27),   S( 62, -24),   S( 27, -28),   S(  0, -26),
            S( 13,   1),   S( 65, -29),   S( 20, -27),   S( 87, -48),   S( 87, -49),   S( 33, -29),   S( 41, -29),   S(-27,   0),
            S(-37, -10),   S( 82, -17),   S( 72, -32),   S( 82, -24),   S( 46,  -5),   S(-12,  -8),   S( -9,  -9),   S(-17,  -6),
            S(-74, -30),   S(-11, -10),   S( 39,  -9),   S(-11,  -2),   S( 32, -20),   S(-42,   1),   S(-26,   8),   S(-36,   3),
            S(-16, -21),   S( -3, -19),   S( -8,  -6),   S( 19,   2),   S( 22,  10),   S(-37,   6),   S(-29,  10),   S(-24,  18),

            /* rooks: KK */
            S(-28,  86),   S(-19,  76),   S(-13,  67),   S( -4,  47),   S(  4,  31),   S(  0,  56),   S( 13,  50),   S(-24,  35),
            S(-43,  87),   S(-38,  91),   S(-27,  70),   S(-17,  43),   S(-18,  41),   S( 11,  10),   S( 35,  27),   S(-16,  39),
            S(-45,  87),   S(-36,  89),   S(-31,  76),   S(-23,  49),   S( -3,  31),   S( -2,  35),   S( 22,  42),   S( -1,  25),
            S(-44, 114),   S(-26, 111),   S(-16,  89),   S(-12,  73),   S(-18,  59),   S(-23,  89),   S( 32,  76),   S(-20,  68),
            S(-24, 137),   S( -7, 125),   S(  4, 101),   S( 18,  78),   S( -3,  94),   S( 39,  75),   S( 68,  88),   S(  8,  91),
            S( -3, 134),   S( 29, 111),   S( 25, 100),   S( 21,  89),   S( 52,  67),   S(113,  58),   S( 98,  75),   S( 60,  82),
            S(-15, 117),   S( -7, 113),   S( 18,  80),   S( 43,  54),   S( 33,  60),   S(112,  42),   S( 94,  80),   S(116,  60),
            S( 65,  97),   S( 65, 108),   S( 40,  97),   S( 54,  72),   S( 17, 102),   S( 80,  97),   S(123,  97),   S(133,  73),

            /* rooks: KQ */
            S(-51,  12),   S(-11, -28),   S(-19, -26),   S(-26, -16),   S(-31,  -6),   S(-31,   2),   S(-40,  20),   S(-76,  43),
            S(-69,   2),   S( -6, -39),   S(-42, -21),   S(-32, -22),   S(-36, -18),   S(-24,  -7),   S(-37,   9),   S(-80,   8),
            S(-69,   7),   S(-35, -23),   S(-57, -10),   S(-19, -33),   S(-56, -15),   S(-49,  -6),   S(-28,   9),   S(-46,   2),
            S(-61,  21),   S(-17,   2),   S(-49,  18),   S(-19,   2),   S(-22,   6),   S(-53,  33),   S( -6,  38),   S(-48,  28),
            S(-36,  37),   S(  7,  14),   S(-14,  16),   S(  1,   1),   S( 10,   1),   S( 19,   6),   S( 39,  22),   S(-13,  40),
            S( 25,  25),   S( 82,  -5),   S( 33,   1),   S( 95, -36),   S( 29,  -1),   S( 76,  -4),   S( 97,   6),   S( 54,  25),
            S(-11,  28),   S( 19,   9),   S( 71, -23),   S( 61, -25),   S( 61, -24),   S( 33, -10),   S( 57,  15),   S( 39,  17),
            S( 27,  47),   S( 40,  39),   S( 84,  -6),   S( 76,  -6),   S( 79,  -9),   S(119, -12),   S( 83,  28),   S(101,   2),

            /* rooks: QK */
            S(-84,  47),   S(-28,  27),   S(-27,  11),   S(-23, -20),   S(-15, -27),   S(-22, -20),   S(-13, -26),   S(-44,   0),
            S(-47,  19),   S(-47,  18),   S(-40,  11),   S(-38, -11),   S(-60, -13),   S(-40, -33),   S(-24, -32),   S(-50, -25),
            S(-47,  18),   S(-18,  19),   S(-54,  26),   S(-51,  -8),   S(-35,  -9),   S(-53,  -6),   S(-14, -31),   S(-41, -21),
            S(-24,  32),   S(  2,  18),   S(-34,  29),   S(-28,  14),   S(-43,   6),   S(-57,  19),   S(-30,  13),   S(-45,   9),
            S( 46,  21),   S( 37,  34),   S( 14,  31),   S(-23,  34),   S( -8,  17),   S(-19,  30),   S( 18,  16),   S( 10,  -3),
            S( 47,  40),   S(105,  21),   S( 74,  14),   S(-11,  41),   S( 38,   5),   S( 68,   8),   S( 98,   2),   S( 20,  31),
            S( 41,  14),   S( 71,   6),   S( 63,  -5),   S(-17,  21),   S(-18,  16),   S( 77, -25),   S(-11,  20),   S( 17,  23),
            S(250, -104),  S(160, -20),   S( 57,  23),   S( 12,  21),   S(  2,  29),   S( 61,  10),   S( 38,  31),   S( 59,  23),

            /* rooks: QQ */
            S(-53, -20),   S( -8, -47),   S(  1, -60),   S(  7, -75),   S( 10, -69),   S( -5, -26),   S(-14, -15),   S(-38,   9),
            S(-15, -57),   S(-22, -31),   S(-46, -31),   S(  5, -79),   S( -9, -68),   S(-12, -61),   S(-20, -37),   S(-52,   2),
            S(-78, -10),   S( -8, -38),   S( 11, -61),   S(-12, -45),   S(-42, -31),   S(-48, -15),   S(-60,   4),   S(-57,   8),
            S(-61,  -7),   S( 14, -19),   S(-50,  -1),   S(-15, -31),   S(-34, -29),   S(-30,  -3),   S(-45,  17),   S(-42,  20),
            S( 29, -12),   S( 77, -22),   S( 19, -28),   S( 70, -35),   S( 67, -42),   S( 68, -24),   S( 32,  10),   S(-21,  18),
            S( 83, -16),   S( 67, -23),   S(108, -40),   S( 61, -20),   S( 52, -26),   S( 62, -22),   S( 73,  -9),   S( 29,  11),
            S( 44,  -9),   S( 88, -21),   S( 36, -24),   S( 48, -38),   S(113, -79),   S( 92, -64),   S( 31,  -8),   S( 11,  13),
            S( 12,  32),   S(-13,  50),   S( -4,  16),   S(-10,  20),   S( 49, -20),   S( 51, -11),   S( 62,  18),   S( 49,  24),

            /* queens: KK */
            S( 21,  57),   S( 35,  15),   S( 45,  -9),   S( 49,  -7),   S( 60, -56),   S( 44, -91),   S( -6, -68),   S( -4,  15),
            S( 13,  80),   S( 36,  48),   S( 36,  37),   S( 46,  20),   S( 46,  11),   S( 64, -36),   S( 85, -84),   S( 25, -20),
            S( 10,  65),   S( 27,  56),   S( 34,  55),   S( 18,  67),   S( 31,  53),   S( 27,  74),   S( 35,  73),   S( 38,  21),
            S( 16,  87),   S(  6,  94),   S( 11,  92),   S( 14,  92),   S(  2,  94),   S( 11, 108),   S( 29, 105),   S(  7, 130),
            S( 14, 105),   S( 15, 110),   S(-10, 126),   S( -9, 129),   S( -8, 150),   S( -4, 184),   S(  3, 209),   S( 15, 179),
            S( -1, 114),   S( 11, 142),   S(-28, 159),   S(-45, 177),   S( -5, 184),   S( 54, 178),   S( 12, 233),   S( -6, 269),
            S(-42, 183),   S(-56, 208),   S(-31, 186),   S(-32, 183),   S(-91, 249),   S( 34, 167),   S(  5, 287),   S( 70, 228),
            S(-24, 152),   S(  6, 140),   S( 28, 130),   S( 13, 140),   S( 21, 151),   S(103, 147),   S(164, 123),   S(131, 163),

            /* queens: KQ */
            S( 28, -18),   S( 28, -60),   S( 22, -87),   S( 34, -103),  S( 35, -56),   S( 22, -94),   S(-32, -48),   S(-73, -20),
            S( 17,  17),   S( 50, -58),   S( 54, -101),  S( 19, -46),   S( 42, -63),   S( 28, -38),   S( 21, -88),   S(-46, -31),
            S(  8,  18),   S( 23, -44),   S( 44, -91),   S( 37, -62),   S( 22, -34),   S( 39, -17),   S(-10,  12),   S(-21,  11),
            S( 12,  17),   S( 44, -46),   S(-11,  -1),   S( 11, -30),   S( 21,  -3),   S( -4,  42),   S(-12,  56),   S(-14,  40),
            S( -7,  43),   S( 53, -22),   S(  4,  33),   S( 22, -12),   S( 45,   4),   S(-48,  78),   S( -7,  83),   S( -9,  80),
            S( 18,  37),   S(105,   8),   S( 53,  31),   S( 82, -14),   S( 18,  31),   S( 20,  41),   S(  7,  69),   S(-23,  90),
            S( 63,  53),   S( 61,  26),   S( 36,  15),   S( 45,  57),   S( 32,  17),   S(-38, 104),   S(  0,  90),   S(-14,  67),
            S(108,  18),   S(119,  27),   S(100,  58),   S(107,  17),   S( 11,  61),   S( 63,  22),   S( 68,  14),   S(  1,  21),

            /* queens: QK */
            S(-36, -85),   S(-81, -75),   S(  1, -123),  S(  2, -76),   S( 33, -139),  S( -7, -85),   S(-19, -57),   S(-55,  12),
            S(-51, -70),   S(-18, -80),   S( 43, -122),  S( 33, -74),   S( 24, -78),   S( 24, -84),   S( 19, -53),   S( 12, -27),
            S(-37, -11),   S( -6, -12),   S(  5, -16),   S( -3,  -2),   S(  4, -34),   S( -7, -39),   S( 23, -49),   S( -3,  -3),
            S( -6, -13),   S(-18,  33),   S(-29,  47),   S( -3,   6),   S(-14,  11),   S(  1, -10),   S( 14, -26),   S( 11, -12),
            S(-11,  14),   S(-18,  41),   S(-29,  72),   S(-49,  54),   S( -6,  19),   S( -5,  23),   S(  2,  11),   S(-16,   1),
            S(-16,  28),   S( 17,  46),   S(-30,  85),   S(-41,  68),   S( 12,  42),   S( 45,  21),   S(  3,  38),   S( -7,  63),
            S(-30,  50),   S(-63,  74),   S(-40,  83),   S(-64,  96),   S(-46,  70),   S(-32,  42),   S(  0,  69),   S(-23, 104),
            S(-34,   6),   S( 24,  44),   S( 22,  33),   S( 16,  36),   S( 29,  19),   S( 12,  42),   S( 69,  28),   S( 80,  50),

            /* queens: QQ */
            S(-57, -67),   S(-86, -55),   S(-32, -63),   S(-25, -47),   S(  6, -71),   S(-18, -55),   S(-47, -37),   S(-24, -33),
            S(-113, -56),  S(-34, -67),   S( 36, -112),  S( 28, -97),   S(  2, -67),   S(-10, -86),   S(-22,  -6),   S(-57, -14),
            S(-12,  11),   S( -9,  23),   S(  2, -55),   S(-13,  -6),   S(-29,   3),   S( -8, -22),   S(  2,   8),   S(-21,   4),
            S( -2, -32),   S( 25, -61),   S( -3, -45),   S(-31, -17),   S( 20, -59),   S( -9, -44),   S(-32, -31),   S(-40,  45),
            S(-27,   5),   S( -7,  41),   S(  5, -21),   S(  0, -22),   S( 19, -15),   S(-11, -35),   S(  8,   4),   S(  9, -29),
            S(  7,  29),   S(  8,  56),   S( 88,  23),   S(  6,  25),   S(-37,  -6),   S( 11,  -2),   S( 17,   1),   S( 13,   8),
            S(114,  45),   S( 30,  19),   S( 29,  37),   S( 15,  30),   S(  2,  26),   S(  1,   3),   S( -2,  36),   S(-24,  27),
            S( 80, -15),   S( 35,  27),   S( 26,  27),   S(  8,  -2),   S( 15,  13),   S( 23, -16),   S( 36, -16),   S( 36,  -1),

            /* kings: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-105,  -5),  S(-120,  20),  S(-42, -12),   S(-56, -36),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-59,  14),   S(-53,  16),   S(-28,   6),   S(-51,   3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-32,  32),   S(-66,  33),   S(-51,  14),   S(-103,  16),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 80,  19),   S( 28,  29),   S( -9,  27),   S(-98,  20),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(227, -10),   S(280, -35),   S(168,  -4),   S( 23,   2),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(233, -22),   S(295, -35),   S(311, -21),   S(204, -19),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(203, -21),   S(153, -18),   S(179,  34),   S( 66,  -2),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 62,   3),   S( 44,  35),   S( 51,  28),   S( -6, -181),

            /* kings: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-135,   9),  S(-156,  18),  S(-66, -17),   S(-77, -18),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-59,   6),   S(-65,   6),   S(-57,  14),   S(-74,   6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-41,  22),   S(-17,   6),   S(-51,  14),   S(-121,  20),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(105,  10),   S( 68,  20),   S( 16,  33),   S(-82,  34),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(207,   1),   S(160,  20),   S(100,  34),   S(-42,  49),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(166,   8),   S(114,  34),   S(105,  37),   S( 11,  47),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(124,  -1),   S(188,   5),   S(137,  18),   S( 37,   9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 80, -10),   S( 51,  -9),   S( 74,   1),   S( 16, -26),

            /* kings: QK */
            S(-57, -85),   S(-43, -72),   S(-81, -50),   S(-152, -15),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-48, -42),   S(-53, -22),   S(-73, -26),   S(-89, -19),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-140,   4),  S(-64,  -7),   S(-68,  -5),   S(-89,   6),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-98,  20),   S(-39,  17),   S( 34,  -3),   S( 34,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-29,   8),   S( 28,  19),   S(103,   1),   S(155, -18),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-44,  27),   S(127,   7),   S(134,   2),   S(171, -13),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  9,  -1),   S(172,  -8),   S(113,   1),   S(146, -14),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-31, -25),   S( 47,  17),   S(125, -16),   S( 45,  -9),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: QQ */
            S(-168,  38),  S(-109, -10),  S(-158,  10),  S(-182,  -1),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-133,  10),  S(-101,  -1),  S(-94,  -8),   S(-142,   2),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-144,   5),  S(-97,  -4),   S(-60,  -4),   S(-89,  10),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-27, -19),   S( 19,   7),   S( 65,   3),   S( 89,  -5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,   1),   S(162,  -9),   S(188,  -6),   S(233, -26),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 95,   5),   S(164,   7),   S(159, -14),   S(136,   3),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 20,   5),   S( 58,  49),   S( 51,  20),   S(137,  -2),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-39, -134),  S( 20,  69),   S( 51,  15),   S( 31,   3),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            #endregion

            /* mobility weights */
            S(  8,   6),    // knights
            S(  5,   4),    // bishops
            S(  3,   4),    // rooks
            S(  1,   4),    // queens

            /* center control */
            S(  2,   6),    // D0
            S(  2,   5),    // D1

            /* squares attacked near enemy king */
            S( 22,  -3),    // attacks to squares 1 from king
            S( 18,  -1),    // attacks to squares 2 from king
            S(  5,   1),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S( 18,  16),    // friendly pawns 1 from king
            S(  8,  16),    // friendly pawns 2 from king
            S(  4,  13),    // friendly pawns 3 from king

            /* castling right available */
            S( 41, -43),

            /* castling complete */
            S( 15, -19),

            /* king on open file */
            S(-72,  11),

            /* king on half-open file */
            S(-29,  24),

            /* king on open diagonal */
            S(-11,  11),

            /* isolated pawns */
            S( -3, -11),

            /* doubled pawns */
            S(-11, -29),

            /* backward pawns */
            S(  0,   0),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 23,  -8),   S(  3,  -1),   S( 10,  -6),   S( 18,  11),   S( 23,  34),   S(  0, -40),   S(-23,  51),   S(  8, -46),
            S(  4,   1),   S( 33,  -4),   S(  8,  27),   S( 18,  41),   S( 44,  -4),   S(  2,  26),   S( 16,  -4),   S( 15, -11),
            S( -7,  22),   S( 17,   6),   S(  4,  55),   S( 22,  76),   S( 38,  27),   S( 30,  34),   S( 30,   7),   S( 10,  23),
            S( 27,  43),   S( 25,  49),   S( 35,  88),   S( 12,  88),   S( 84,  60),   S( 79,  43),   S( 18,  51),   S( 22,  54),
            S(101,  84),   S(133,  95),   S( 88, 164),   S(149, 159),   S(179, 118),   S(161, 128),   S(160,  79),   S( 84,  56),
            S( 97, 231),   S(139, 321),   S(116, 245),   S(107, 211),   S( 76, 180),   S( 67, 176),   S( 54, 202),   S( 26, 134),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,  22),   S(-15,  28),   S(-29,  34),   S(-52,  76),   S( 23,  -8),   S(-21,  20),   S( -6,  57),   S( 15,  16),
            S(  9,  29),   S( -3,  43),   S(-12,  35),   S( -8,  31),   S(-19,  39),   S(-45,  47),   S(-47,  74),   S( 21,  25),
            S( 18,   9),   S( 13,  21),   S(  4,  23),   S( 20,  27),   S(  6,  19),   S(-27,  33),   S(-47,  67),   S(-15,  40),
            S( 51,  18),   S( 76,  33),   S( 40,  30),   S( 19,  27),   S( 25,  50),   S( 64,  40),   S( 34,  56),   S(-14,  64),
            S( 62,  60),   S(112,  93),   S(102,  63),   S( 44,  40),   S(-38,  29),   S( 76,  39),   S( 54,  85),   S( 13,  46),
            S(256,  60),   S(232, 102),   S(227, 111),   S(211, 112),   S(195, 122),   S(207, 115),   S(258, 117),   S(272,  92),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 41,   7),   S(  6,  16),   S( 17,  22),   S( 19,  48),   S( 87,  33),   S( 35,  15),   S( 18,   1),   S( 46,   7),
            S(  4,  15),   S(  4,  10),   S( 23,  12),   S( 22,  26),   S( 17,  13),   S( -3,   5),   S( 10,   2),   S( 30,  -7),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4, -15),   S( -4, -10),   S(-23, -12),   S(-22, -26),   S(-17, -13),   S(  3,  -5),   S(-10,  -2),   S(-30,   7),
            S(-41,  -7),   S( -6, -16),   S(-17, -22),   S(-19, -48),   S(-87, -33),   S(-35, -15),   S(-18,  -1),   S(-46,  -7),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 34,   3),   S( 34,  16),   S( 50,  20),   S( 46,  21),   S( 27,  31),   S( 28,  21),   S( 16,  17),   S( 58,  -9),
            S( -3,   8),   S( 24,  21),   S( 18,  16),   S( 22,  42),   S( 31,  12),   S( 13,  17),   S( 38,   5),   S( 18,   4),
            S( -5,  12),   S( 23,  28),   S( 55,  36),   S( 48,  33),   S( 59,  32),   S( 56,  18),   S( 27,  29),   S( 24,  14),
            S( 44,  73),   S(106,  45),   S(117,  80),   S(143,  67),   S(133,  65),   S( 81,  75),   S( 94,  27),   S( 82,  13),
            S( 57,  87),   S(188,  71),   S(224, 120),   S(191,  85),   S(208, 120),   S(172, 106),   S(215,  53),   S(-14,  79),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S( -3,   7),   S( -7,  47),   S( 32,  95),   S( 57, 207),

            /* enemy king outside passed pawn square */
            S(-20, 193),

            /* passed pawn/friendly king distance penalty */
            S( -2, -18),

            /* passed pawn/enemy king distance bonus */
            S(  0,  31),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 73, -48),   S( 41,  11),   S( 38,  23),   S( 55,  38),   S( 47,  10),   S(201, -27),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S(  9,  -2),   S( 11,  62),   S(  3,  55),   S(  5,  78),   S( 37,  77),   S(168,  89),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-37,  -1),   S( -6, -40),   S(  6, -38),   S(-24, -18),   S( 26, -50),   S(262, -129),  S(  0,   0),    // blocked by rooks
            S(  0,   0),   S( -7,  -5),   S( 35, -22),   S( -1,  22),   S( 12, -51),   S( 20, -210),  S(  9, -219),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(  2,   4),   S( 92,   2),   S( 20, -42),   S( 92, -26),   S(213, -16),   S(384,  57),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S(  2,  47),

            /* knight on outpost */
            S(  2,  30),

            /* bishop on outpost */
            S( 11,  29),

            /* bishop pair */
            S( 34,  94),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,  -1),   S(  1, -10),   S( -2,  -3),   S( -7, -40),   S(-14, -12),   S(-25,   5),   S(-23,  -3),   S(  2,  -7),
            S( -5,  -8),   S( -7,  -9),   S(-14, -10),   S( -8, -12),   S( -9, -19),   S(-14,  -6),   S(-14,  -7),   S( -4,  -6),
            S( -5, -10),   S(  5, -27),   S( -3, -39),   S( -2, -49),   S(-12, -35),   S(-12, -27),   S(-13, -16),   S(  0,  -7),
            S(  6, -26),   S(  8, -37),   S( -7, -25),   S( -4, -41),   S( -6, -34),   S( -7, -24),   S(  0, -28),   S( -5, -17),
            S( 10, -16),   S(  7, -42),   S(  9, -52),   S( 12, -56),   S( 22, -60),   S( 12, -54),   S(  1, -63),   S( -2,  -8),
            S( 62, -33),   S( 85, -90),   S( 80, -101),  S( 74, -123),  S( 92, -143),  S(125, -116),  S(110, -135),  S( 95, -109),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 34,   2),

            /* rook on half-open file */
            S(  9,  34),

            /* rook on seventh rank */
            S(  0,  45),

            /* doubled rooks on file */
            S( 23,  19),

            /* queen on open file */
            S(-12,  35),

            /* queen on half-open file */
            S(  4,  37),

            /* pawn push threats */
            S(  0,   0),   S( 32,  32),   S( 36, -17),   S( 37,  24),   S( 35, -22),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 63,  72),   S( 45,  99),   S( 48,  87),   S( 42,  31),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-11,   3),   S( 48,  28),   S( 86,  -7),   S( 25,  15),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 32,  66),   S(  2,  25),   S( 41,  50),   S( 26,  98),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -8,  56),   S( 44,  48),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-29,  13),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats
        };
    }
}
