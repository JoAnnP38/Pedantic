using System.Runtime.CompilerServices;
using System.Text;

using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class HceWeights
    {
        public static readonly Guid HCE_WEIGHTS_VERSION = new("da5e310e-b0dc-4c77-902c-5a46cc81bb73");

        #region Weight Offset Constants

        public const int MAX_WEIGHTS = 1985;
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
        //public const int PP_CAN_ADVANCE = 1817;
        public const int KING_OUTSIDE_PP_SQUARE = 1818;
        public const int PP_FRIENDLY_KING_DISTANCE = 1819;
        public const int PP_ENEMY_KING_DISTANCE = 1820;
        public const int BLOCK_PASSED_PAWN = 1821;
        public const int ROOK_BEHIND_PASSED_PAWN = 1869;

        /* piece evaluation */
        public const int KNIGHT_OUTPOST = 1870;
        public const int BISHOP_OUTPOST = 1871;
        public const int BISHOP_PAIR = 1872;
        public const int BAD_BISHOP_PAWN = 1873;
        public const int ROOK_ON_OPEN_FILE = 1937;
        public const int ROOK_ON_HALF_OPEN_FILE = 1938;
        public const int ROOK_ON_7TH_RANK = 1939;
        public const int DOUBLED_ROOKS_ON_FILE = 1940;
        public const int QUEEN_ON_OPEN_FILE = 1941;
        public const int QUEEN_ON_HALF_OPEN_FILE = 1942;

        /* threats */
        public const int PAWN_PUSH_THREAT = 1943;
        public const int PIECE_THREAT = 1949;

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

#if PP_CAN_ADVANCE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score PassedPawnCanAdvance(int rank)
        {
            Util.Assert(rank >= Coord.RANK_4);
            int index = rank - Coord.RANK_4;
            return weights[PP_CAN_ADVANCE + index];
        }
#endif

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

        // Solution sample size: 12000000, generated on Fri, 06 Oct 2023 09:37:45 GMT
        // Solution error: 0.118106, accuracy: 0.5217
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(122, 153),   S(499, 448),   S(547, 494),   S(689, 875),   S(1577, 1541), S(  0,   0),

            /* piece square values */
            #region piece square values

            /* pawns: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-61,  11),   S(-58,  12),   S(-41,  -4),   S(-20, -28),   S(-52,  29),   S( 55, -11),   S( 61, -34),   S( -8, -41),
            S(-64,   5),   S(-52,  -8),   S(-54, -28),   S(-65, -20),   S(-40, -27),   S(-16, -24),   S( 13, -30),   S(-52, -20),
            S(-33,   9),   S(-36,   4),   S(-14, -35),   S(-33, -50),   S(-14, -36),   S( 18, -37),   S( -1, -15),   S( -6, -29),
            S(-16,  58),   S(-32,  29),   S( -4,  11),   S( 18,  -8),   S( 29, -31),   S( 16, -21),   S( 40,  -6),   S( 56, -14),
            S( 52,  85),   S( -3,  97),   S( -8,  88),   S( 67,  72),   S(117,  68),   S(191,  18),   S(186,  32),   S(178,  21),
            S( 92,  93),   S(104,  91),   S( 80, 101),   S(117,  54),   S( 93,  64),   S(155, -11),   S( 69,  53),   S( 95,   3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-97,  27),   S(-99,  38),   S(-67,  14),   S(-74,  48),   S(-46, -20),   S( 51, -41),   S( 64, -46),   S(-11, -20),
            S(-88,   2),   S(-87,   3),   S(-69, -26),   S(-76, -30),   S(-39, -50),   S(-20, -38),   S( 19, -42),   S(-49, -12),
            S(-65,  21),   S(-41,   4),   S(-29, -20),   S(-36, -41),   S(-23, -35),   S( -3, -32),   S( -4, -26),   S(  5, -28),
            S(-52,  70),   S(-15,   3),   S( -3,  -7),   S( 12,   5),   S( -2, -13),   S(-32,  27),   S( 19,  13),   S( 56,   5),
            S(102,  52),   S( 94,  11),   S(135, -15),   S(105,  45),   S(114, 104),   S( 61, 124),   S( 65, 125),   S(114, 108),
            S(168,   6),   S( 93,  36),   S(130,  -5),   S( 93,  76),   S(111, 106),   S( 64, 152),   S(144, 118),   S(147, 106),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-36,  14),   S(  3,  -2),   S( -6, -10),   S(-43,  16),   S(-62,  12),   S( -6,  -7),   S(-26,  -5),   S(-69,  16),
            S(-28,  10),   S(  1, -15),   S(-46, -20),   S(-57, -31),   S(-52, -25),   S(-31, -30),   S(-45, -24),   S(-107,   4),
            S( -1,   8),   S(  2,   3),   S(-16, -16),   S(-34, -36),   S(-32, -22),   S( -2, -34),   S(-25, -23),   S(-47, -12),
            S( 16,  62),   S(-20,  56),   S( -1,  36),   S( 24,  13),   S( 13, -18),   S( -1, -23),   S(  7, -13),   S(  0,   9),
            S( 71, 104),   S( -1, 142),   S(  2, 137),   S( 71,  97),   S(127,  61),   S(174,   6),   S(137,  33),   S( 94,  57),
            S(122,  82),   S(190, 101),   S(120, 151),   S(159,  93),   S(100,  56),   S(124,  -5),   S( 72,  20),   S( 52,  26),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-55,  -1),   S( -8,  -8),   S( 34, -29),   S(  5, -18),   S( -9, -23),   S( 12, -41),   S( -2, -38),   S(-22, -27),
            S(-48,  -9),   S(-22, -14),   S(-20, -37),   S(-45, -45),   S(-15, -54),   S(-38, -54),   S(-32, -48),   S(-77, -22),
            S(-25,   3),   S(-24,  -2),   S( 16, -41),   S( -9, -48),   S(  5, -52),   S(  7, -62),   S( -9, -41),   S(-32, -33),
            S( 55,  13),   S( 55, -11),   S( 56, -25),   S( 50,  -9),   S( 23, -34),   S(-12, -22),   S( 17, -26),   S( 34, -14),
            S(225, -15),   S(144,  -2),   S(174, -31),   S(146,  22),   S( 45,  80),   S( 63,  55),   S( 18,  68),   S(116,  55),
            S(178,  -8),   S( 73,  49),   S(108, -13),   S( 62,  62),   S(109,  35),   S( 21,  79),   S(135,  55),   S(111,  64),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: KK */
            S(-110, -44),  S(-17, -80),   S(-44,  -7),   S(-24,   8),   S( -4,   3),   S( -1,   4),   S(-15, -13),   S(-59, -58),
            S(-31, -45),   S(-44,  25),   S( -3,   1),   S( 13,   2),   S( 14,  15),   S( 25,  26),   S(  0,  21),   S( -1,   5),
            S(-21, -11),   S( -4,  22),   S(  7,  31),   S( 25,  56),   S( 36,  53),   S( 16,  28),   S( 12,  41),   S(  6,  36),
            S( -5,  34),   S( 16,  53),   S( 27,  77),   S( 27,  89),   S( 31,  98),   S( 51,  83),   S( 56,  75),   S( 19,  49),
            S( 14,  47),   S( 19,  46),   S( 43,  59),   S( 46,  88),   S( 15, 100),   S( 43, 108),   S( 17, 114),   S( 70,  56),
            S(-32,  66),   S( 22,  49),   S( 38,  57),   S( 46,  60),   S( 87,  46),   S(178,  19),   S( 39,  76),   S( -7,  67),
            S(-20,  40),   S(-29,  62),   S( -2,  63),   S( 48,  63),   S( 38,  69),   S(122,   3),   S( 17,  26),   S(-17,  48),
            S(-312,  57),  S(-65,  71),   S(-52,  74),   S( 53,  52),   S( 56,  49),   S(-120,  53),  S(  4,  47),   S(-206, -73),

            /* knights: KQ */
            S(-94, -111),  S(-43, -50),   S(-62,   2),   S(-56,  12),   S(-30, -15),   S(-23, -20),   S(-39,   9),   S(-22, -128),
            S(-83, -17),   S(-40,  10),   S(-11, -22),   S( 14, -23),   S( -2,  -9),   S( 22,  -5),   S(-66,  27),   S(-49, -14),
            S(-44,  -7),   S( -1, -29),   S( -2,   5),   S( 26,   2),   S( 26,   1),   S( 28, -34),   S( -5,  14),   S(-17,  12),
            S( -5,   5),   S( 51, -11),   S( 39,  20),   S( 59,  11),   S( 52,  20),   S( 31,  27),   S( 29,  16),   S(-15,  25),
            S( 62, -45),   S( 27, -20),   S( 66,  -3),   S( 61,   7),   S( 83,   0),   S( 67,   3),   S( 51,  -5),   S( 19,  -4),
            S( 59, -39),   S( 72, -63),   S(139, -68),   S(128, -49),   S( 62,  -2),   S( 69,   2),   S( -7,  32),   S( -5,   3),
            S(  7, -45),   S(-17, -27),   S(  5, -42),   S( 72, -15),   S( 28,  -6),   S( 24,  10),   S(-29,  24),   S(-47,   4),
            S(-293, -116), S(-14, -55),   S(-13, -33),   S( 35,  -7),   S(-47, -29),   S( -6,  37),   S(-34,  34),   S(-127, -54),

            /* knights: QK */
            S(-97, -106),  S(-71,  18),   S(-49,  16),   S(-72,  41),   S(-28,  -7),   S(-22, -20),   S(-54, -37),   S(-92, -81),
            S(-42, -64),   S(-31,  13),   S( 13, -11),   S( -5,   3),   S(-11,   4),   S(  0,   6),   S(-62,   4),   S(-16, -58),
            S(-12,   7),   S(  1, -14),   S( 28, -14),   S( 50,  18),   S( 62,  -3),   S(-16,  16),   S(  4, -14),   S(-53, -26),
            S( 30,   1),   S( 58,  10),   S( 53,  21),   S( 38,  50),   S( 51,  24),   S( 40,  31),   S( 40,   7),   S(-11,   4),
            S( 18,  25),   S( 59,  11),   S( 73,  22),   S(121,   1),   S( 68,  26),   S( 82,  15),   S( 52,   5),   S( 73, -21),
            S( -3,  36),   S(  9,  32),   S( 40,  30),   S( 76,  -6),   S(102,  -4),   S(163, -38),   S( 44,  -7),   S( 24, -24),
            S(-30,  27),   S( -1,  14),   S( 12,  41),   S( 63,  44),   S(-21,  36),   S( 68,  -8),   S( -2, -19),   S( 62, -34),
            S(-150, -84),  S(-37,  22),   S(-28,  37),   S( 22,  -4),   S(-12,  27),   S(-36, -26),   S(-33, -26),   S(-58, -97),

            /* knights: QQ */
            S(  9, -109),  S(-36, -17),   S( 32, -31),   S(-73,   8),   S(-39,  -4),   S(-45,   1),   S(-44, -73),   S(-66, -84),
            S(-33, -41),   S(-31,  27),   S( 15, -34),   S( 27, -24),   S(-15, -13),   S( 13, -54),   S(  2, -20),   S(-138, -40),
            S( 13, -22),   S( 12, -24),   S( 27, -28),   S( 39,  -7),   S( 33,  -7),   S(-10, -17),   S(-12,  -3),   S(-28, -48),
            S( 20,  44),   S( 27,   8),   S( 78,   2),   S( 79, -12),   S( 52,  -4),   S( 51, -13),   S( 53,   0),   S(-35,  12),
            S( 50,  -8),   S( 23,   8),   S( 67, -16),   S( 79, -12),   S( 80,  -6),   S( 69, -21),   S( 71, -29),   S(  9,   3),
            S( 82, -23),   S(-37,   0),   S( 48, -27),   S(141, -36),   S( 54, -20),   S( 35,   9),   S( 69, -26),   S(  4, -25),
            S(-20,  30),   S( 24, -12),   S( -8,  -1),   S( 14,  27),   S( 50,  12),   S( 44,   3),   S(-15,   9),   S(-61,  16),
            S(-122, -86),  S(  0,  58),   S( -1,   8),   S( -1,  32),   S(-21, -13),   S( 15,  34),   S(  6,  49),   S(-51, -45),

            /* bishops: KK */
            S( 21, -18),   S( 24, -20),   S(  5,  -4),   S(-15,  40),   S( 10,  31),   S(  6,  37),   S( 11,   6),   S(  6, -50),
            S( 23,   1),   S( 23,   8),   S( 21,  17),   S( 14,  38),   S( 18,  33),   S( 31,  23),   S( 48,   4),   S( 32, -45),
            S(  1,  25),   S( 22,  42),   S( 32,  59),   S( 23,  46),   S( 22,  75),   S( 24,  44),   S( 25,  23),   S(  8,  22),
            S( -5,  47),   S(  3,  64),   S( 22,  70),   S( 49,  54),   S( 39,  45),   S( 21,  51),   S( 12,  37),   S( 37,  -7),
            S( -3,  64),   S( 24,  33),   S( 13,  47),   S( 50,  45),   S( 33,  53),   S( 49,  47),   S( 28,  44),   S(  7,  61),
            S(  2,  49),   S( 17,  57),   S( -9,  56),   S( -4,  56),   S(  7,  65),   S( 81,  62),   S( 56,  49),   S( -5,  90),
            S(-50,  75),   S(-32,  66),   S(-18,  67),   S(-41,  79),   S(-44,  77),   S( -7,  56),   S(-85,  62),   S(-58,  52),
            S(-108,  97),  S(-72,  89),   S(-53,  88),   S(-60,  95),   S(-62,  88),   S(-95,  76),   S(  8,  30),   S(-69,  56),

            /* bishops: KQ */
            S(-49,   2),   S( 30, -10),   S(-25,   9),   S(-19,   2),   S( 20, -13),   S(  8, -19),   S(104, -94),   S( 17, -96),
            S(-20, -16),   S( -6,  -3),   S( -1,  10),   S(  9,   5),   S( 21,  -1),   S( 53, -30),   S( 85, -31),   S( 20, -71),
            S(-27,   8),   S(  9,   8),   S( 32,  11),   S( 32,   2),   S( 31,  14),   S( 61, -13),   S( 61, -15),   S( 48, -36),
            S( 23,  16),   S(  5,  25),   S( 18,   4),   S( 65,  -9),   S( 83,  -6),   S( 46,  24),   S( 24,  18),   S( -5,  19),
            S( 11,  23),   S( 54, -12),   S( 41, -14),   S(126, -60),   S( 86,  -1),   S( 33,  20),   S( 36,  18),   S( 24,   7),
            S(  9,  -9),   S(110, -38),   S(128, -29),   S(  5,  15),   S( 38,   7),   S( 37,  11),   S( 29,  24),   S(  4,  37),
            S(-50, -31),   S( 26, -17),   S(-50,  23),   S(-38,  20),   S( -7,  14),   S(-16,  24),   S(-30,  48),   S(-73,  73),
            S(-32,   5),   S(-35,   6),   S( 18,  -4),   S(-17,  23),   S(-15,  21),   S(-24,  37),   S(-28,  40),   S(-55,  38),

            /* bishops: QK */
            S( 41, -65),   S( 35, -37),   S( 24,  -1),   S(-25,  24),   S( -6,  15),   S(-25,  -2),   S(-27,  26),   S(-132,  -1),
            S( 62, -58),   S( 72, -26),   S( 31,   5),   S( 36,   9),   S( -3,  13),   S(-11,  13),   S(-27,  -3),   S( -7, -20),
            S( 15,  -2),   S( 49,   8),   S( 54,  11),   S( 30,  23),   S( 21,  34),   S(  8,   4),   S( 13,  -5),   S(-16, -13),
            S(  4,   3),   S( 36,  21),   S( 33,  39),   S( 82,   3),   S( 58,  -4),   S( 14,  29),   S(-24,  33),   S( -8,  21),
            S(-11,  47),   S( 29,  20),   S( 46,  15),   S( 90,  -4),   S( 50,   6),   S( 54,  12),   S( 48, -13),   S(-15,  32),
            S(-35,  47),   S( -3,  47),   S( 41,  15),   S( 25,  33),   S( 51,  25),   S( 77,  20),   S( 90, -30),   S( 22,  10),
            S(-51,  55),   S(-66,  46),   S(-44,  53),   S(-40,  54),   S(-71,  52),   S( -8,  30),   S(-30,  -3),   S(-30, -27),
            S(-58,  36),   S(-60,  41),   S(-30,  44),   S(-50,  57),   S( -1,  15),   S(-34,   4),   S( 24,   8),   S(-28,  19),

            /* bishops: QQ */
            S(-76, -20),   S(-24, -18),   S( 21, -35),   S(-63, -10),   S(  1, -22),   S(-13, -35),   S( 39, -51),   S(-20, -83),
            S( 49, -73),   S( 70, -52),   S( -7, -14),   S( 38, -28),   S(-28,   2),   S( 41, -47),   S( -6, -34),   S( 92, -106),
            S( 14, -20),   S( 44, -28),   S( 50, -27),   S( 22, -23),   S( 41, -18),   S( 22, -27),   S( 53, -56),   S( -4, -22),
            S( 51, -19),   S( 63, -20),   S( 26, -22),   S( 82, -42),   S( 33, -28),   S( 71, -25),   S( 19, -22),   S( 27, -31),
            S( 33, -15),   S( 35, -21),   S( 37, -35),   S( 87, -47),   S( 90, -57),   S( 10, -23),   S( 36, -36),   S(  3,  -5),
            S(-35,  -2),   S( 92, -25),   S( 95, -38),   S( 54, -25),   S( -3,   4),   S( 67, -39),   S( -7,  -7),   S(  0,  -6),
            S(-47, -22),   S(-27, -15),   S( 35, -25),   S( -6,   7),   S( 26, -22),   S(-30,  -8),   S( 33, -19),   S(-27,   5),
            S(-26, -15),   S( -5, -10),   S( -5, -19),   S( -3,   9),   S( 18,  10),   S(-10,  -3),   S(  6,  10),   S(-33,  36),

            /* rooks: KK */
            S(-37,  99),   S(-25,  92),   S(-20,  79),   S( -7,  51),   S(  1,  38),   S( -5,  64),   S(  9,  60),   S(-32,  50),
            S(-52,  96),   S(-35,  87),   S(-29,  76),   S(-19,  51),   S(-17,  39),   S(  7,  25),   S( 34,  38),   S(-21,  51),
            S(-54, 100),   S(-51, 103),   S(-39,  78),   S(-26,  52),   S( -5,  32),   S( -8,  47),   S( 27,  36),   S( -6,  44),
            S(-46, 122),   S(-33, 109),   S(-24,  93),   S(-13,  74),   S(-22,  58),   S(-28,  89),   S( 27,  72),   S(-24,  75),
            S(-26, 134),   S( -9, 119),   S(  1, 100),   S( 26,  69),   S( -1,  87),   S( 40,  74),   S( 74,  75),   S( 22,  82),
            S( -7, 135),   S( 31, 114),   S( 36,  98),   S( 41,  70),   S( 77,  52),   S(140,  57),   S(137,  67),   S( 71,  76),
            S(-22, 128),   S(-23, 131),   S(  8,  95),   S( 36,  65),   S( 22,  72),   S( 97,  66),   S(103,  89),   S(148,  61),
            S( 77, 101),   S( 64, 118),   S( 50,  95),   S( 70,  62),   S( 49,  95),   S(106,  98),   S(121, 108),   S(133,  80),

            /* rooks: KQ */
            S(-56,  14),   S( -8, -16),   S(-21, -22),   S(-29, -10),   S(-29, -10),   S(-23,  -8),   S(-36,  34),   S(-63,  40),
            S(-75,  13),   S(-20, -32),   S(-36, -27),   S(-34, -24),   S(-31, -18),   S(-43,   4),   S(-46,  21),   S(-45,   9),
            S(-54,   4),   S(-30, -15),   S(-51, -14),   S(-22, -36),   S(-57, -11),   S(-32,  -7),   S(-10,   2),   S(-55,   7),
            S(-72,  17),   S(-15,  -9),   S(-69,   9),   S(-10, -16),   S(-24, -14),   S(-54,  29),   S(-10,  18),   S(-12,   6),
            S(-14,  10),   S( 44, -13),   S( -7,   0),   S( -5,   9),   S( 45, -23),   S( 31,   1),   S( 40,  13),   S(  5,  25),
            S( 83,  -6),   S( 94, -16),   S( 58, -28),   S( 86, -38),   S( 81, -39),   S( 59,  -3),   S(134, -17),   S( 55,  16),
            S( -7,  21),   S( 27,  10),   S( 59, -28),   S( 50, -21),   S( 75, -25),   S( 28,  -5),   S( 58,  14),   S(102,  -7),
            S( 36,  37),   S( 61,  14),   S( 61,  -9),   S( 57, -12),   S( 50,  -9),   S( 86,  -3),   S( 86,  27),   S(124,  -3),

            /* rooks: QK */
            S(-97,  54),   S(-30,  25),   S(-37,  22),   S(-29, -12),   S(-20, -25),   S(-20, -22),   S( -8, -24),   S(-48,  11),
            S(-52,  19),   S(-51,  16),   S(-55,  23),   S(-40, -12),   S(-55, -18),   S(-55, -20),   S(-26, -26),   S(-39, -14),
            S(-44,  18),   S( -9,  16),   S(-37,   9),   S(-49,  -8),   S(-73,   1),   S(-78,  -2),   S(-27, -28),   S(-37, -28),
            S( -8,   9),   S( 42,  -3),   S(-34,  21),   S(-38,  10),   S(-55,  12),   S(-54,  15),   S(  7,  -3),   S(-27,  -3),
            S( 59,  12),   S( 33,  33),   S( 60,   2),   S(-18,  23),   S(-25,  17),   S( -8,  10),   S( 18,   5),   S(  3, -11),
            S( 45,  29),   S(102,  14),   S( 43,  27),   S( -7,  19),   S( 63, -15),   S(100,  -6),   S(110, -11),   S( 48,   6),
            S( 45,  18),   S( 53,  20),   S( 90,  -2),   S(  8,  12),   S(-31,  23),   S( 85,  -7),   S( 32,  17),   S( 19,  18),
            S(262, -108),  S(124,  -4),   S( 92,   4),   S( 58,  12),   S( 26,  20),   S( 82,   0),   S(104,   7),   S(112,  -5),

            /* rooks: QQ */
            S(-60, -16),   S(-25, -33),   S(  9, -67),   S( 12, -87),   S( -8, -63),   S(-21, -30),   S(-37,  -6),   S(-48,  13),
            S(-35, -48),   S(-42, -27),   S(-37, -44),   S(  4, -86),   S(-34, -73),   S(-49, -56),   S(-29, -45),   S(-32, -18),
            S(-85, -13),   S( 24, -54),   S( -1, -62),   S(-17, -69),   S(-42, -35),   S(-56, -47),   S(-52, -11),   S(-57,  -3),
            S(-42, -14),   S( 45, -46),   S(-32, -25),   S(-18, -42),   S(-51, -45),   S(-29, -30),   S(-53,  -8),   S(-35,   3),
            S( 36, -29),   S( 68, -40),   S(  6, -26),   S(112, -75),   S( 41, -46),   S( 39, -31),   S( -2,   2),   S(  3,   2),
            S( 83, -34),   S( 99, -25),   S(119, -44),   S(103, -61),   S( 75, -59),   S( 71, -26),   S( 34,  -6),   S( 18,   4),
            S(101, -37),   S( 70, -21),   S( 25,  -7),   S( 82, -55),   S(162, -92),   S( 77, -57),   S( 53, -20),   S( 19,   2),
            S( 52,   3),   S( 37,  13),   S( 12,   0),   S(  1,  -1),   S( 15, -36),   S( 62, -30),   S(108,  -3),   S( 31,   6),

            /* queens: KK */
            S( 37,  51),   S( 46,  23),   S( 56,  -4),   S( 60,   6),   S( 79, -58),   S( 58, -81),   S(-11, -29),   S(  1,  51),
            S( 25,  79),   S( 47,  50),   S( 48,  44),   S( 56,  26),   S( 59,  14),   S( 85, -35),   S( 82, -52),   S( 49, -19),
            S( 21,  59),   S( 36,  59),   S( 42,  69),   S( 30,  66),   S( 48,  51),   S( 36,  85),   S( 47,  80),   S( 45,  28),
            S( 32,  81),   S( 12, 106),   S( 22,  88),   S( 16,  98),   S( 16,  88),   S( 27, 108),   S( 51,  89),   S( 14, 138),
            S( 23,  94),   S( 31, 101),   S(-11, 141),   S( -6, 130),   S(  2, 146),   S( 14, 176),   S( 22, 190),   S( 19, 197),
            S( 13, 113),   S( 21, 151),   S(-15, 157),   S(-33, 171),   S( 10, 171),   S(104, 163),   S( 53, 215),   S( 15, 257),
            S(-14, 170),   S(-35, 203),   S(-24, 190),   S(-13, 163),   S(-55, 217),   S( 85, 149),   S( 17, 284),   S(124, 172),
            S(-25, 164),   S( 43, 126),   S( 25, 135),   S( 15, 153),   S( 37, 151),   S(111, 146),   S(161, 155),   S(131, 169),

            /* queens: KQ */
            S( 19, -30),   S( 15, -17),   S( 26, -92),   S( 19, -53),   S( 21, -32),   S(  6, -64),   S(-32, -28),   S(-27,  13),
            S( 31,   6),   S( 29, -36),   S( 42, -87),   S( 19, -35),   S( 26, -41),   S( 18, -63),   S(-38, -29),   S(-51, -51),
            S(  7,   2),   S(  5, -20),   S( 38, -84),   S( 19, -33),   S( 28, -30),   S( 25,  19),   S( 11, -12),   S( -6, -59),
            S( -1,  33),   S( 56, -75),   S(-22,  14),   S(-18,  -3),   S( 14,  -8),   S(-11,  30),   S(  1,  30),   S(-15,  49),
            S( 18,  -8),   S( 31, -10),   S( -1,  -4),   S( -4, -11),   S( 40, -31),   S( -5,  31),   S(  5,  40),   S(  5,  65),
            S( 51,   0),   S( 93,   7),   S( 66,   3),   S( 29,   0),   S( 13,  24),   S( 20,  43),   S( 28,  71),   S( 15,  49),
            S( 58,  28),   S( 97,  12),   S( 73,  22),   S( 79,   9),   S( 43,   2),   S( -5,  74),   S(-14,  93),   S( 23,  54),
            S( 82,  38),   S(128,  36),   S( 98,  37),   S( 86,  54),   S( 31,  67),   S( 61,  29),   S( 54,  23),   S( -6,  21),

            /* queens: QK */
            S(-75, -59),   S(-88, -34),   S( -7, -108),  S( -5, -76),   S( 16, -107),  S( -2, -64),   S(-27, -30),   S(-59,  28),
            S(-69, -26),   S(-35, -87),   S( 38, -120),  S( 21, -76),   S( 19, -74),   S( -2, -51),   S( 40, -67),   S(  1,  -2),
            S(-48, -32),   S( -2, -24),   S(  4, -10),   S(-15,  10),   S( 10, -33),   S(-20,  -4),   S( 12, -49),   S( -5,   1),
            S(-15, -33),   S(-43,  34),   S(-30,  42),   S(-11,   7),   S(-22,  16),   S( -6, -13),   S(  9, -16),   S(-14,  -4),
            S( -8,  26),   S(-13,  13),   S(-45,  70),   S(-41,  50),   S(-33,  33),   S( 38, -12),   S(  3,  27),   S( 12, -21),
            S( -2,  18),   S( -4,  75),   S(-17,  48),   S(-33,  54),   S( 15,  27),   S( 69,  15),   S( 44,  35),   S( 15,  31),
            S(  8,  -4),   S(  3,  37),   S(-46,  89),   S(-40,  48),   S(-20,  31),   S(  2,  33),   S( 48,  16),   S( 63,  35),
            S(-29,  41),   S( 19,  35),   S( 33,  26),   S( 28,  25),   S(-26,  32),   S( 78,  44),   S( 97,  33),   S( 65,  44),

            /* queens: QQ */
            S(-59, -54),   S(-74, -47),   S(-40, -16),   S(-41, -62),   S(  9, -40),   S(-13, -63),   S(-68, -34),   S(-76, -17),
            S(-79, -33),   S(-51, -55),   S(  6, -73),   S( 21, -91),   S(-38, -25),   S(-26, -75),   S(-17, -49),   S(-62, -38),
            S(-26, -19),   S(-14,  -9),   S(  8, -60),   S(-14, -56),   S(-16, -61),   S(-34, -36),   S(-20, -50),   S(-40, -38),
            S(-46, -27),   S( 24, -21),   S(-41,  -1),   S( -3, -50),   S( -9, -61),   S( -4, -100),  S(-17, -61),   S(-39,   1),
            S(-28,   6),   S(-18,  16),   S( 45, -38),   S( 36, -43),   S( 66, -47),   S(  4, -42),   S(-16,  -9),   S( -3, -45),
            S( 37,  15),   S( 48,  18),   S( 92,  43),   S( 50, -22),   S(  8, -20),   S(-31,  -7),   S(-13,   0),   S( -9,  51),
            S( 83,  24),   S( 53,  45),   S( 70,  18),   S( 32,  39),   S( 12,   0),   S(-14, -12),   S( -7,  23),   S(-42,   9),
            S(  9, -35),   S( 70,  43),   S( 17,  28),   S( 29,  15),   S( 15,  -8),   S( 14, -28),   S( 30, -22),   S( 16, -30),

            /* kings: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-108,  -2),  S(-125,  23),  S(-49,  -6),   S(-59, -35),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-59,  10),   S(-48,  10),   S(-27,   7),   S(-53,   8),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-10,  16),   S(-47,  20),   S(-50,  12),   S(-101,  13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(106,   5),   S( 59,  17),   S( 12,  15),   S(-124,  24),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(225, -21),   S(283, -36),   S(179,  -7),   S( 37,  -3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(248, -19),   S(294, -29),   S(270,   1),   S(155,   3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(204, -15),   S(150,  -5),   S(148,  50),   S( 67,  13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 65,   1),   S( 69,  30),   S( 68,  37),   S( -6, -139),

            /* kings: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-148,  18),  S(-164,  26),  S(-70, -14),   S(-80, -22),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-56,   0),   S(-57,   2),   S(-62,  20),   S(-82,  13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-46,  15),   S(-17,   0),   S(-39,   9),   S(-113,  19),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(124,  -5),   S( 54,  19),   S(-17,  35),   S(-82,  33),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(225, -12),   S(142,  21),   S( 84,  35),   S( -1,  39),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(174,   5),   S( 97,  41),   S(114,  37),   S( -3,  57),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(182,  -3),   S(222, -10),   S(104,  27),   S( 32,  24),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 72,  -8),   S( 75,   2),   S( 85, -21),   S(  4, -20),

            /* kings: QK */
            S(-65, -68),   S(-43, -65),   S(-88, -44),   S(-163, -16),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-38, -46),   S(-60, -14),   S(-72, -29),   S(-90, -22),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-96, -16),   S(-70,  -7),   S(-40, -21),   S(-83,  -3),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-95,  13),   S(  4,   4),   S( 46,  -7),   S( 37,  -8),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-45,  12),   S( 64,  19),   S( 79,   6),   S(156, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-26,  34),   S( 85,  24),   S(155,   2),   S(168, -13),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 45,  -2),   S(145,   4),   S(121,   2),   S(138,  -8),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-26, -19),   S( 42,  21),   S(127,  -4),   S( 52,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: QQ */
            S(-177,  49),  S(-112,   5),  S(-158,  15),  S(-204,   4),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-122,  14),  S(-116,  15),  S(-88,  -8),   S(-125,  -9),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-129,   5),  S(-73,  -8),   S(-62, -11),   S(-61,  -7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-56, -13),   S( 17,   1),   S( 42,   1),   S(103, -13),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 54, -11),   S(149,  -4),   S(215, -17),   S(212, -23),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 66,  14),   S(173,  17),   S(164, -18),   S(122,  10),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  7,  -5),   S( 40,  63),   S( 74,  15),   S( 96,   7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-22, -133),  S( 25,  50),   S( 35,   9),   S( 65,   9),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            #endregion

            /* mobility weights */
            S(  9,   7),    // knights
            S(  5,   4),    // bishops
            S(  4,   2),    // rooks
            S(  1,   3),    // queens

            /* center control */
            S(  1,   9),    // D0
            S(  3,   5),    // D1

            /* squares attacked near enemy king */
            S( 24,  -5),    // attacks to squares 1 from king
            S( 21,  -2),    // attacks to squares 2 from king
            S(  7,   0),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S( 22,  16),    // friendly pawns 1 from king
            S(  9,  19),    // friendly pawns 2 from king
            S(  6,  14),    // friendly pawns 3 from king

            /* castling right available */
            S( 38, -41),

            /* castling complete */
            S( 19, -17),

            /* king on open file */
            S(-81,  15),

            /* king on half-open file */
            S(-32,  28),

            /* king on open diagonal */
            S( -8,  12),

            /* isolated pawns */
            S( -4, -11),

            /* doubled pawns */
            S(-16, -33),

            /* backward pawns */
            S(  0,   0),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 28,  -9),   S( -1,   0),   S( 10,  -3),   S( 16,  30),   S( 30,  30),   S(  1, -44),   S(-30,  54),   S(  9, -50),
            S( 18,   2),   S( 23, -10),   S( 17,  27),   S( 21,  41),   S( 56,  -6),   S(  0,  22),   S( 19,   2),   S( 13, -19),
            S(-11,  11),   S( 23,  19),   S(  0,  51),   S( 29,  80),   S( 39,  28),   S( 40,  37),   S( 23,   6),   S( 20,  21),
            S( 43,  67),   S( 13,  51),   S( 71,  88),   S( 14,  84),   S(107,  71),   S(107,  36),   S( 13,  59),   S( 38,  44),
            S( 72, 110),   S(142, 106),   S(179, 169),   S(191, 165),   S(172, 119),   S(168, 168),   S(153,  74),   S( 83,  26),
            S( 79, 200),   S(122, 289),   S(119, 253),   S(122, 240),   S( 84, 199),   S( 54, 159),   S( 46, 212),   S( 26, 166),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  2,  18),   S(-13,  33),   S(-25,  32),   S(-48,  52),   S( 17,   2),   S(-25,  29),   S(-21,  59),   S( 29,  17),
            S( 11,  25),   S( -1,  44),   S(-16,  41),   S( -9,  35),   S(-33,  46),   S(-49,  49),   S(-58,  77),   S( 19,  27),
            S( 16,   2),   S(  5,  21),   S(-13,  30),   S( 19,  21),   S(-10,  22),   S(-35,  39),   S(-63,  67),   S(-13,  37),
            S( 50,  28),   S( 76,  49),   S( 36,  47),   S( 17,  39),   S( 21,  68),   S( 54,  54),   S( 42,  68),   S(  6,  70),
            S( 70,  72),   S(109,  92),   S(109,  74),   S( 34,  62),   S( -4,  55),   S( 88,  50),   S( 40,  82),   S(  5,  66),
            S(275,  90),   S(257, 127),   S(239, 143),   S(250, 133),   S(260, 137),   S(252, 141),   S(302, 145),   S(284, 118),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 47,  16),   S( 19,  29),   S( 20,  31),   S( 22,  56),   S( 94,  29),   S( 29,  24),   S( 25,  20),   S( 61,  14),
            S( 10,  14),   S(  6,  14),   S( 24,  14),   S( 26,  27),   S( 16,  12),   S( -5,   7),   S( 12,   4),   S( 30,  -5),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-10, -14),   S( -6, -14),   S(-24, -14),   S(-26, -27),   S(-16, -12),   S(  5,  -7),   S(-12,  -4),   S(-30,   5),
            S(-47, -16),   S(-19, -29),   S(-20, -31),   S(-22, -56),   S(-94, -29),   S(-29, -24),   S(-25, -20),   S(-61, -14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 36,  -2),   S( 37,  17),   S( 51,  22),   S( 53,  34),   S( 33,  39),   S( 36,  19),   S( 24,  13),   S( 68, -10),
            S( -6,   2),   S( 21,  23),   S( 21,  13),   S( 30,  34),   S( 34,  11),   S( 19,  16),   S( 40,   4),   S( 10,   3),
            S(-19,  14),   S( 30,  26),   S( 64,  36),   S( 49,  35),   S( 66,  34),   S( 72,  13),   S( 29,  30),   S( 29,   7),
            S( 68,  51),   S(154,  26),   S(139,  85),   S(171,  60),   S(188,  55),   S( 90,  81),   S(105,  23),   S(104,   0),
            S(105,  84),   S(243,  52),   S(226, 144),   S(199,  44),   S(261, 129),   S(165, 118),   S(227,  29),   S(-63, 110),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* enemy king outside passed pawn square */
            S( 18, 208),

            /* passed pawn/friendly king distance penalty */
            S( -2, -21),

            /* passed pawn/enemy king distance bonus */
            S(  0,  36),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 78, -35),   S( 56,   6),   S( 49,  23),   S( 51,  57),   S( 74,  37),   S(178,  35),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 32,   1),   S( 20,  51),   S( 12,  58),   S(  8, 108),   S( 61, 117),   S(195, 135),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-65,  26),   S(-17, -21),   S(-25, -26),   S(-28,   4),   S( 35, -12),   S(276, -88),   S(  0,   0),    // blocked by rooks
            S(  0,   0),   S( 14, -39),   S( 58, -32),   S( -1,  23),   S( -1,  -8),   S( 27, -173),  S(-48, -186),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S( -1,  16),   S( 84,   8),   S( 53, -52),   S( 92, -15),   S(235,   4),   S(391,  83),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S( -1,  43),

            /* knight on outpost */
            S(  4,  32),

            /* bishop on outpost */
            S( 11,  31),

            /* bishop pair */
            S( 40, 104),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -2,   0),   S(  0,  -7),   S( -4,  -5),   S( -3, -51),   S(-13, -14),   S(-29,   5),   S(-23,  -5),   S(  0,  -6),
            S( -5, -10),   S( -9, -10),   S(-15, -14),   S( -9, -19),   S(-15, -19),   S(-17,  -5),   S(-16,  -7),   S( -2, -11),
            S( -6, -12),   S(  3, -26),   S(  0, -41),   S( -1, -56),   S(-17, -35),   S(-14, -30),   S(-12, -22),   S( -2,  -7),
            S(  4, -28),   S(  7, -38),   S( -1, -36),   S(  2, -50),   S( -7, -38),   S( -8, -31),   S( -6, -33),   S( -4, -22),
            S( 20, -36),   S(  8, -53),   S( 16, -62),   S(  7, -62),   S( 32, -77),   S( -6, -52),   S( 47, -95),   S(-12, -18),
            S( 65, -56),   S(106, -107),  S(103, -113),  S( 97, -135),  S( 95, -166),  S(111, -123),  S( 78, -154),  S(158, -157),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 36,   6),

            /* rook on half-open file */
            S( 11,  35),

            /* rook on seventh rank */
            S( 16,  30),

            /* doubled rooks on file */
            S( 20,  23),

            /* queen on open file */
            S(-10,  40),

            /* queen on half-open file */
            S(  7,  30),

            /* pawn push threats */
            S(  0,   0),   S( 32,  30),   S( 36, -22),   S( 34,  28),   S( 35, -24),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 67,  72),   S( 49,  91),   S( 74,  60),   S( 59,  10),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-12,  10),   S( 48,  29),   S( 91,   5),   S( 29,  62),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 31,  63),   S( -5,  41),   S( 56,  38),   S( 37, 104),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-17,  66),   S( 56,  72),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-14,  16),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats
        };
    }
}
