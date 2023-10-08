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

        // Solution sample size: 12000000, generated on Sat, 07 Oct 2023 11:04:29 GMT
        // Solution error: 0.118100, accuracy: 0.5218
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(116, 159),   S(498, 450),   S(546, 496),   S(688, 877),   S(1576, 1543), S(  0,   0),

            /* piece square values */
            #region piece square values

            /* pawns: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-63,  15),   S(-59,  15),   S(-43,  -1),   S(-24, -23),   S(-52,  31),   S( 53,  -8),   S( 60, -31),   S(-11, -37),
            S(-66,   9),   S(-55,  -3),   S(-56, -24),   S(-68, -15),   S(-41, -24),   S(-15, -23),   S(  9, -25),   S(-53, -18),
            S(-33,  11),   S(-37,   8),   S(-12, -35),   S(-33, -48),   S(-11, -37),   S( 22, -38),   S(  1, -13),   S( -5, -28),
            S(-15,  60),   S(-32,  32),   S( -2,  10),   S( 19,  -8),   S( 32, -34),   S( 19, -22),   S( 41,  -4),   S( 58, -14),
            S( 52,  85),   S( -3,  97),   S( -6,  88),   S( 68,  72),   S(119,  67),   S(192,  17),   S(188,  32),   S(178,  20),
            S( 91,  93),   S(104,  90),   S( 80, 100),   S(117,  53),   S( 94,  63),   S(156, -13),   S( 70,  52),   S( 96,   2),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-98,  31),   S(-100,  41),  S(-69,  17),   S(-77,  52),   S(-48, -17),   S( 49, -38),   S( 62, -43),   S(-14, -16),
            S(-89,   5),   S(-89,   7),   S(-71, -23),   S(-78, -28),   S(-41, -47),   S(-21, -36),   S( 16, -38),   S(-51,  -9),
            S(-65,  23),   S(-42,   7),   S(-28, -19),   S(-36, -39),   S(-21, -36),   S(  0, -33),   S( -3, -24),   S(  6, -27),
            S(-52,  72),   S(-15,   6),   S(  0,  -8),   S( 13,   5),   S(  2, -14),   S(-29,  26),   S( 20,  15),   S( 57,   5),
            S(102,  52),   S( 95,  11),   S(137, -15),   S(107,  45),   S(116, 103),   S( 63, 123),   S( 66, 125),   S(114, 108),
            S(167,   6),   S( 93,  36),   S(129,  -5),   S( 93,  75),   S(111, 105),   S( 64, 152),   S(144, 117),   S(148, 105),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-37,  17),   S(  3,   0),   S( -7,  -8),   S(-44,  19),   S(-64,  15),   S( -8,  -4),   S(-28,  -1),   S(-72,  21),
            S(-29,  12),   S( -1, -12),   S(-47, -18),   S(-58, -29),   S(-54, -23),   S(-32, -29),   S(-49, -19),   S(-109,   7),
            S(  0,   9),   S(  2,   6),   S(-13, -16),   S(-33, -35),   S(-30, -22),   S(  0, -34),   S(-24, -20),   S(-47, -11),
            S( 17,  64),   S(-19,  58),   S(  2,  35),   S( 26,  13),   S( 16, -19),   S(  1, -24),   S(  8, -11),   S(  0,  11),
            S( 72, 104),   S(  0, 142),   S(  4, 137),   S( 73,  97),   S(128,  61),   S(175,   6),   S(137,  34),   S( 93,  57),
            S(122,  81),   S(190, 100),   S(120, 150),   S(160,  93),   S(101,  55),   S(124,  -6),   S( 73,  20),   S( 51,  26),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-56,   2),   S( -8,  -5),   S( 34, -27),   S(  4, -16),   S(-12, -20),   S(  9, -37),   S( -5, -34),   S(-26, -22),
            S(-49,  -7),   S(-24, -10),   S(-22, -35),   S(-46, -43),   S(-17, -50),   S(-39, -52),   S(-36, -43),   S(-79, -19),
            S(-25,   5),   S(-24,   1),   S( 18, -41),   S( -9, -47),   S(  6, -52),   S( 10, -62),   S( -9, -38),   S(-32, -31),
            S( 56,  15),   S( 57,  -9),   S( 59, -26),   S( 52,  -9),   S( 26, -35),   S(-11, -22),   S( 18, -24),   S( 34, -12),
            S(226, -14),   S(146,  -2),   S(176, -32),   S(148,  22),   S( 46,  80),   S( 63,  56),   S( 18,  69),   S(115,  55),
            S(178,  -9),   S( 73,  48),   S(108, -14),   S( 62,  61),   S(108,  35),   S( 21,  79),   S(135,  55),   S(110,  64),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: KK */
            S(-109, -44),  S(-17, -79),   S(-44,  -7),   S(-23,   8),   S( -4,   3),   S(  0,   4),   S(-14, -13),   S(-59, -58),
            S(-30, -45),   S(-44,  25),   S( -3,   1),   S( 13,   3),   S( 14,  15),   S( 25,  25),   S(  0,  21),   S( -1,   5),
            S(-21, -11),   S( -3,  22),   S(  8,  30),   S( 25,  56),   S( 36,  53),   S( 16,  28),   S( 12,  41),   S(  6,  36),
            S( -4,  34),   S( 16,  53),   S( 27,  77),   S( 27,  89),   S( 31,  99),   S( 51,  83),   S( 57,  75),   S( 19,  49),
            S( 14,  47),   S( 19,  46),   S( 43,  59),   S( 46,  89),   S( 15, 101),   S( 43, 108),   S( 17, 114),   S( 70,  56),
            S(-32,  66),   S( 23,  49),   S( 37,  58),   S( 46,  60),   S( 86,  47),   S(177,  20),   S( 39,  76),   S( -7,  67),
            S(-20,  40),   S(-29,  62),   S( -2,  63),   S( 48,  63),   S( 38,  69),   S(122,   4),   S( 17,  27),   S(-17,  48),
            S(-311,  57),  S(-65,  71),   S(-52,  75),   S( 53,  52),   S( 56,  49),   S(-120,  53),  S(  4,  47),   S(-206, -73),

            /* knights: KQ */
            S(-94, -110),  S(-43, -50),   S(-62,   1),   S(-56,  11),   S(-30, -15),   S(-23, -20),   S(-39,  10),   S(-22, -128),
            S(-83, -18),   S(-39,  10),   S(-10, -23),   S( 14, -23),   S( -2,  -9),   S( 22,  -5),   S(-66,  27),   S(-49, -14),
            S(-44,  -7),   S( -1, -30),   S( -2,   5),   S( 26,   2),   S( 26,   1),   S( 28, -34),   S( -5,  14),   S(-17,  12),
            S( -5,   5),   S( 51, -11),   S( 39,  21),   S( 60,  11),   S( 51,  20),   S( 31,  27),   S( 30,  16),   S(-14,  25),
            S( 63, -45),   S( 28, -20),   S( 67,  -3),   S( 61,   7),   S( 83,   0),   S( 66,   4),   S( 51,  -4),   S( 19,  -4),
            S( 59, -39),   S( 73, -64),   S(139, -68),   S(128, -48),   S( 62,  -2),   S( 68,   2),   S( -7,  32),   S( -5,   3),
            S(  7, -46),   S(-17, -27),   S(  6, -42),   S( 72, -15),   S( 28,  -6),   S( 24,  10),   S(-29,  24),   S(-47,   4),
            S(-292, -116), S(-14, -55),   S(-13, -33),   S( 35,  -7),   S(-47, -29),   S( -6,  37),   S(-34,  34),   S(-127, -54),

            /* knights: QK */
            S(-97, -106),  S(-71,  18),   S(-48,  15),   S(-72,  40),   S(-28,  -7),   S(-22, -21),   S(-53, -37),   S(-92, -81),
            S(-42, -64),   S(-30,  13),   S( 14, -11),   S( -5,   3),   S(-11,   4),   S(  0,   5),   S(-61,   4),   S(-15, -58),
            S(-12,   8),   S(  1, -13),   S( 28, -14),   S( 50,  18),   S( 62,  -3),   S(-16,  16),   S(  4, -14),   S(-53, -26),
            S( 30,   2),   S( 58,  11),   S( 53,  22),   S( 38,  50),   S( 51,  24),   S( 39,  31),   S( 40,   7),   S(-11,   3),
            S( 18,  25),   S( 59,  11),   S( 73,  22),   S(120,   1),   S( 68,  26),   S( 82,  15),   S( 53,   4),   S( 73, -21),
            S( -3,  35),   S(  9,  32),   S( 40,  30),   S( 76,  -6),   S(101,  -4),   S(163, -38),   S( 44,  -8),   S( 25, -25),
            S(-30,  27),   S( -2,  14),   S( 12,  41),   S( 63,  44),   S(-21,  36),   S( 67,  -8),   S( -3, -18),   S( 63, -35),
            S(-150, -84),  S(-36,  22),   S(-28,  37),   S( 22,  -4),   S(-12,  27),   S(-36, -26),   S(-33, -26),   S(-58, -97),

            /* knights: QQ */
            S(  9, -109),  S(-36, -16),   S( 33, -32),   S(-73,   8),   S(-39,  -4),   S(-45,   1),   S(-44, -73),   S(-66, -84),
            S(-33, -41),   S(-31,  27),   S( 15, -34),   S( 27, -24),   S(-16, -13),   S( 13, -54),   S(  2, -20),   S(-138, -40),
            S( 13, -22),   S( 12, -24),   S( 27, -28),   S( 39,  -6),   S( 33,  -7),   S(-10, -17),   S(-12,  -3),   S(-28, -47),
            S( 19,  44),   S( 27,   9),   S( 79,   2),   S( 79, -12),   S( 52,  -4),   S( 51, -13),   S( 53,   1),   S(-35,  12),
            S( 50,  -9),   S( 23,   9),   S( 66, -16),   S( 79, -12),   S( 80,  -6),   S( 69, -20),   S( 71, -29),   S(  9,   3),
            S( 81, -23),   S(-37,   0),   S( 48, -27),   S(140, -35),   S( 54, -20),   S( 34,  10),   S( 69, -26),   S(  4, -25),
            S(-20,  31),   S( 24, -12),   S( -8,  -1),   S( 14,  27),   S( 50,  13),   S( 44,   3),   S(-15,  10),   S(-60,  16),
            S(-122, -86),  S(  0,  59),   S( -1,   8),   S( -1,  32),   S(-21, -13),   S( 15,  34),   S(  6,  49),   S(-51, -45),

            /* bishops: KK */
            S( 21, -18),   S( 23, -20),   S(  5,  -4),   S(-15,  40),   S( 10,  30),   S(  7,  37),   S( 11,   6),   S(  7, -49),
            S( 23,   1),   S( 23,   8),   S( 21,  17),   S( 15,  37),   S( 18,  33),   S( 31,  23),   S( 47,   5),   S( 32, -45),
            S(  1,  25),   S( 22,  42),   S( 33,  59),   S( 23,  46),   S( 22,  74),   S( 24,  44),   S( 25,  23),   S(  8,  22),
            S( -5,  47),   S(  3,  64),   S( 22,  70),   S( 49,  54),   S( 39,  45),   S( 21,  51),   S( 12,  37),   S( 38,  -7),
            S( -2,  63),   S( 24,  33),   S( 13,  47),   S( 50,  45),   S( 33,  53),   S( 49,  46),   S( 28,  44),   S(  8,  61),
            S(  2,  49),   S( 18,  56),   S( -9,  56),   S( -5,  56),   S(  6,  65),   S( 80,  62),   S( 56,  49),   S( -5,  90),
            S(-50,  75),   S(-32,  66),   S(-17,  66),   S(-41,  79),   S(-44,  77),   S( -7,  56),   S(-86,  63),   S(-57,  51),
            S(-108,  97),  S(-71,  88),   S(-53,  88),   S(-59,  95),   S(-62,  88),   S(-95,  75),   S(  8,  30),   S(-69,  56),

            /* bishops: KQ */
            S(-48,   1),   S( 30, -10),   S(-25,   9),   S(-19,   2),   S( 20, -13),   S(  8, -19),   S(104, -94),   S( 17, -96),
            S(-20, -16),   S( -6,  -3),   S( -1,  10),   S( 10,   5),   S( 21,  -1),   S( 53, -30),   S( 85, -31),   S( 20, -71),
            S(-27,   8),   S(  9,   8),   S( 32,  11),   S( 32,   2),   S( 32,  13),   S( 61, -13),   S( 61, -15),   S( 49, -36),
            S( 23,  16),   S(  5,  25),   S( 18,   4),   S( 65,  -9),   S( 83,  -5),   S( 46,  24),   S( 24,  18),   S( -5,  19),
            S( 11,  22),   S( 55, -12),   S( 42, -14),   S(126, -61),   S( 86,  -1),   S( 33,  20),   S( 36,  18),   S( 24,   6),
            S(  9,  -9),   S(110, -38),   S(127, -29),   S(  4,  15),   S( 38,   7),   S( 37,  11),   S( 29,  24),   S(  4,  37),
            S(-49, -31),   S( 26, -17),   S(-50,  23),   S(-38,  21),   S( -7,  14),   S(-16,  24),   S(-30,  48),   S(-73,  73),
            S(-32,   6),   S(-35,   6),   S( 18,  -4),   S(-17,  23),   S(-14,  21),   S(-24,  37),   S(-27,  39),   S(-55,  38),

            /* bishops: QK */
            S( 41, -66),   S( 35, -37),   S( 24,  -1),   S(-25,  24),   S( -5,  15),   S(-25,  -2),   S(-27,  26),   S(-131,  -2),
            S( 62, -58),   S( 71, -26),   S( 31,   6),   S( 36,   9),   S( -3,  13),   S(-11,  13),   S(-27,  -3),   S( -8, -20),
            S( 15,  -2),   S( 49,   8),   S( 54,  11),   S( 30,  23),   S( 21,  34),   S(  7,   4),   S( 13,  -5),   S(-16, -13),
            S(  4,   2),   S( 36,  21),   S( 33,  39),   S( 82,   3),   S( 58,  -4),   S( 14,  29),   S(-24,  32),   S( -8,  20),
            S(-11,  47),   S( 29,  20),   S( 46,  15),   S( 90,  -4),   S( 50,   6),   S( 54,  12),   S( 49, -13),   S(-15,  32),
            S(-35,  47),   S( -3,  47),   S( 41,  15),   S( 25,  33),   S( 50,  25),   S( 76,  20),   S( 89, -29),   S( 22,  10),
            S(-51,  55),   S(-66,  46),   S(-43,  53),   S(-40,  54),   S(-71,  52),   S( -9,  30),   S(-30,  -3),   S(-29, -27),
            S(-58,  36),   S(-61,  41),   S(-30,  44),   S(-51,  57),   S( -2,  15),   S(-34,   3),   S( 25,   8),   S(-28,  20),

            /* bishops: QQ */
            S(-76, -19),   S(-24, -18),   S( 22, -35),   S(-63, -10),   S(  2, -22),   S(-13, -35),   S( 39, -51),   S(-19, -83),
            S( 50, -74),   S( 70, -52),   S( -7, -14),   S( 38, -28),   S(-28,   3),   S( 40, -46),   S( -6, -33),   S( 92, -106),
            S( 14, -20),   S( 45, -28),   S( 50, -27),   S( 22, -23),   S( 41, -18),   S( 22, -27),   S( 54, -56),   S( -4, -22),
            S( 51, -18),   S( 63, -20),   S( 26, -22),   S( 83, -42),   S( 33, -28),   S( 71, -25),   S( 19, -21),   S( 28, -32),
            S( 34, -15),   S( 35, -21),   S( 37, -35),   S( 87, -47),   S( 90, -56),   S( 10, -23),   S( 36, -36),   S(  3,  -6),
            S(-34,  -2),   S( 92, -25),   S( 95, -37),   S( 54, -25),   S( -3,   4),   S( 66, -38),   S( -7,  -7),   S(  1,  -6),
            S(-47, -22),   S(-27, -14),   S( 35, -25),   S( -6,   7),   S( 26, -22),   S(-30,  -8),   S( 33, -18),   S(-26,   6),
            S(-26, -15),   S( -5, -10),   S( -6, -19),   S( -3,   9),   S( 18,  10),   S(-10,  -3),   S(  6,  10),   S(-33,  36),

            /* rooks: KK */
            S(-37,  99),   S(-24,  91),   S(-20,  79),   S( -7,  51),   S(  1,  37),   S( -5,  64),   S( 10,  59),   S(-31,  50),
            S(-52,  96),   S(-35,  87),   S(-29,  76),   S(-19,  51),   S(-17,  39),   S(  7,  24),   S( 34,  38),   S(-21,  51),
            S(-54, 100),   S(-51, 103),   S(-39,  78),   S(-26,  52),   S( -5,  32),   S( -8,  47),   S( 27,  35),   S( -6,  44),
            S(-46, 122),   S(-34, 109),   S(-24,  93),   S(-14,  74),   S(-22,  58),   S(-29,  89),   S( 26,  72),   S(-24,  74),
            S(-26, 134),   S( -9, 119),   S(  0, 100),   S( 25,  70),   S( -2,  88),   S( 39,  75),   S( 74,  75),   S( 22,  82),
            S( -7, 135),   S( 31, 113),   S( 36,  98),   S( 41,  70),   S( 76,  52),   S(140,  57),   S(137,  66),   S( 70,  76),
            S(-22, 128),   S(-23, 130),   S(  8,  95),   S( 36,  65),   S( 21,  72),   S( 96,  66),   S(103,  89),   S(147,  61),
            S( 78, 100),   S( 64, 117),   S( 50,  95),   S( 71,  61),   S( 49,  94),   S(106,  97),   S(122, 107),   S(133,  80),

            /* rooks: KQ */
            S(-55,  14),   S( -8, -16),   S(-20, -22),   S(-28, -10),   S(-29, -10),   S(-23,  -8),   S(-35,  33),   S(-63,  40),
            S(-75,  13),   S(-20, -32),   S(-36, -27),   S(-34, -24),   S(-32, -18),   S(-43,   3),   S(-45,  21),   S(-45,   9),
            S(-54,   4),   S(-30, -15),   S(-51, -14),   S(-22, -36),   S(-57, -10),   S(-32,  -7),   S(-10,   2),   S(-54,   7),
            S(-71,  17),   S(-15,  -9),   S(-69,   9),   S(-10, -15),   S(-25, -14),   S(-54,  29),   S(-10,  18),   S(-11,   6),
            S(-14,  10),   S( 45, -13),   S( -7,   0),   S( -6,  10),   S( 44, -23),   S( 31,   1),   S( 39,  13),   S(  4,  25),
            S( 83,  -6),   S( 94, -16),   S( 58, -29),   S( 86, -37),   S( 81, -39),   S( 59,  -3),   S(134, -17),   S( 55,  16),
            S( -7,  21),   S( 27,   9),   S( 59, -28),   S( 50, -20),   S( 75, -25),   S( 28,  -5),   S( 58,  14),   S(102,  -7),
            S( 36,  36),   S( 62,  13),   S( 61,  -9),   S( 57, -12),   S( 50,  -9),   S( 86,  -3),   S( 86,  27),   S(124,  -4),

            /* rooks: QK */
            S(-98,  53),   S(-30,  25),   S(-37,  22),   S(-29, -12),   S(-21, -25),   S(-20, -22),   S( -7, -25),   S(-48,  10),
            S(-52,  19),   S(-51,  17),   S(-56,  23),   S(-41, -12),   S(-55, -18),   S(-55, -20),   S(-26, -27),   S(-39, -15),
            S(-44,  17),   S( -9,  16),   S(-38,   9),   S(-49,  -8),   S(-73,   2),   S(-78,  -2),   S(-27, -29),   S(-37, -29),
            S( -8,   9),   S( 41,  -2),   S(-35,  22),   S(-38,  10),   S(-55,  12),   S(-55,  15),   S(  7,  -3),   S(-27,  -3),
            S( 59,  12),   S( 32,  34),   S( 59,   2),   S(-18,  24),   S(-26,  17),   S( -9,  11),   S( 18,   5),   S(  3, -11),
            S( 45,  29),   S(102,  14),   S( 43,  27),   S( -8,  20),   S( 63, -15),   S( 99,  -6),   S(110, -11),   S( 48,   6),
            S( 44,  18),   S( 53,  20),   S( 90,  -2),   S(  7,  12),   S(-32,  23),   S( 85,  -7),   S( 32,  16),   S( 19,  17),
            S(262, -108),  S(124,  -4),   S( 92,   4),   S( 58,  12),   S( 26,  20),   S( 82,   0),   S(104,   6),   S(112,  -6),

            /* rooks: QQ */
            S(-60, -16),   S(-25, -33),   S(  9, -67),   S( 12, -87),   S( -8, -63),   S(-21, -30),   S(-37,  -6),   S(-48,  13),
            S(-35, -48),   S(-42, -27),   S(-37, -44),   S(  4, -86),   S(-34, -73),   S(-49, -55),   S(-29, -44),   S(-31, -18),
            S(-86, -13),   S( 24, -54),   S( -1, -62),   S(-17, -69),   S(-42, -35),   S(-56, -47),   S(-52, -11),   S(-57,  -4),
            S(-42, -14),   S( 45, -46),   S(-33, -24),   S(-18, -41),   S(-52, -44),   S(-29, -30),   S(-53,  -8),   S(-35,   4),
            S( 35, -29),   S( 68, -39),   S(  6, -25),   S(111, -75),   S( 41, -45),   S( 38, -31),   S( -2,   3),   S(  3,   2),
            S( 83, -34),   S( 99, -25),   S(119, -43),   S(102, -61),   S( 75, -59),   S( 71, -26),   S( 34,  -5),   S( 17,   5),
            S(101, -38),   S( 70, -21),   S( 25,  -8),   S( 82, -55),   S(162, -91),   S( 76, -57),   S( 53, -20),   S( 19,   2),
            S( 52,   3),   S( 37,  13),   S( 12,   0),   S(  1,  -1),   S( 16, -36),   S( 63, -30),   S(108,  -3),   S( 31,   6),

            /* queens: KK */
            S( 38,  50),   S( 46,  23),   S( 56,  -4),   S( 60,   6),   S( 80, -57),   S( 59, -81),   S(-11, -29),   S(  2,  51),
            S( 25,  80),   S( 47,  50),   S( 48,  44),   S( 56,  26),   S( 59,  15),   S( 85, -35),   S( 82, -52),   S( 49, -19),
            S( 21,  60),   S( 36,  59),   S( 42,  69),   S( 30,  67),   S( 48,  51),   S( 36,  85),   S( 47,  80),   S( 45,  28),
            S( 32,  81),   S( 12, 107),   S( 21,  88),   S( 16,  98),   S( 16,  89),   S( 27, 109),   S( 51,  90),   S( 14, 139),
            S( 23,  94),   S( 31, 102),   S(-11, 142),   S( -7, 131),   S(  2, 147),   S( 14, 176),   S( 22, 190),   S( 20, 197),
            S( 13, 113),   S( 22, 151),   S(-15, 158),   S(-33, 172),   S( 10, 172),   S(103, 164),   S( 54, 214),   S( 15, 257),
            S(-14, 170),   S(-35, 203),   S(-24, 190),   S(-13, 163),   S(-56, 217),   S( 85, 149),   S( 16, 285),   S(125, 172),
            S(-25, 164),   S( 43, 126),   S( 25, 135),   S( 15, 153),   S( 37, 152),   S(111, 146),   S(161, 155),   S(131, 170),

            /* queens: KQ */
            S( 20, -30),   S( 15, -17),   S( 27, -92),   S( 19, -52),   S( 21, -32),   S(  6, -63),   S(-31, -28),   S(-26,  14),
            S( 31,   7),   S( 29, -36),   S( 43, -87),   S( 19, -35),   S( 27, -41),   S( 18, -62),   S(-38, -29),   S(-51, -51),
            S(  7,   2),   S(  5, -20),   S( 38, -84),   S( 19, -33),   S( 29, -30),   S( 24,  20),   S( 12, -12),   S( -6, -59),
            S( -1,  33),   S( 56, -74),   S(-22,  14),   S(-18,  -2),   S( 13,  -7),   S(-11,  31),   S(  1,  30),   S(-15,  49),
            S( 18,  -8),   S( 32, -10),   S( -1,  -4),   S( -4, -10),   S( 40, -30),   S( -5,  32),   S(  5,  40),   S(  5,  66),
            S( 51,   0),   S( 93,   8),   S( 66,   4),   S( 29,   1),   S( 13,  25),   S( 20,  43),   S( 28,  72),   S( 15,  49),
            S( 58,  28),   S( 97,  12),   S( 73,  23),   S( 79,  10),   S( 43,   3),   S( -4,  75),   S(-14,  93),   S( 24,  54),
            S( 83,  38),   S(128,  36),   S( 98,  37),   S( 87,  55),   S( 31,  68),   S( 62,  30),   S( 55,  23),   S( -6,  21),

            /* queens: QK */
            S(-75, -59),   S(-88, -34),   S( -7, -108),  S( -4, -76),   S( 16, -107),  S( -2, -64),   S(-27, -30),   S(-59,  28),
            S(-69, -26),   S(-36, -87),   S( 38, -119),  S( 22, -76),   S( 19, -74),   S( -2, -51),   S( 40, -67),   S(  2,  -2),
            S(-48, -32),   S( -3, -24),   S(  4, -10),   S(-15,  10),   S( 11, -34),   S(-21,  -4),   S( 12, -49),   S( -5,   1),
            S(-16, -33),   S(-43,  34),   S(-31,  43),   S(-11,   8),   S(-23,  17),   S( -6, -12),   S(  9, -16),   S(-14,  -4),
            S( -8,  26),   S(-13,  13),   S(-46,  71),   S(-42,  51),   S(-34,  34),   S( 38, -12),   S(  3,  27),   S( 12, -21),
            S( -2,  18),   S( -4,  76),   S(-17,  48),   S(-33,  54),   S( 15,  27),   S( 68,  15),   S( 44,  35),   S( 16,  31),
            S(  9,  -5),   S(  2,  37),   S(-46,  89),   S(-40,  48),   S(-20,  32),   S(  2,  33),   S( 48,  16),   S( 63,  35),
            S(-29,  41),   S( 19,  36),   S( 33,  26),   S( 28,  25),   S(-26,  32),   S( 78,  44),   S( 98,  33),   S( 65,  44),

            /* queens: QQ */
            S(-58, -54),   S(-74, -47),   S(-40, -16),   S(-40, -62),   S(  9, -41),   S(-13, -62),   S(-68, -33),   S(-76, -17),
            S(-79, -32),   S(-51, -54),   S(  6, -72),   S( 22, -90),   S(-38, -25),   S(-25, -75),   S(-17, -49),   S(-62, -38),
            S(-26, -18),   S(-15,  -8),   S(  8, -59),   S(-14, -56),   S(-16, -62),   S(-34, -36),   S(-20, -50),   S(-39, -38),
            S(-46, -26),   S( 24, -20),   S(-41,  -1),   S( -3, -50),   S(-10, -60),   S( -4, -100),  S(-16, -61),   S(-38,   1),
            S(-28,   5),   S(-18,  16),   S( 44, -37),   S( 35, -43),   S( 66, -46),   S(  4, -41),   S(-15,  -9),   S( -3, -44),
            S( 38,  15),   S( 48,  18),   S( 93,  43),   S( 50, -22),   S(  8, -19),   S(-31,  -7),   S(-13,   0),   S( -9,  51),
            S( 83,  24),   S( 53,  45),   S( 70,  18),   S( 32,  39),   S( 12,   0),   S(-14, -12),   S( -8,  23),   S(-42,   9),
            S( 10, -35),   S( 71,  43),   S( 17,  28),   S( 29,  16),   S( 15,  -8),   S( 14, -27),   S( 31, -22),   S( 16, -30),

            /* kings: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-107,  -3),  S(-123,  22),  S(-48,  -6),   S(-59, -36),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-57,   9),   S(-46,   9),   S(-25,   6),   S(-52,   7),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -8,  15),   S(-45,  19),   S(-49,  11),   S(-100,  12),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(107,   4),   S( 60,  16),   S( 12,  16),   S(-124,  24),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(226, -21),   S(283, -36),   S(179,  -7),   S( 36,  -2),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(248, -19),   S(295, -29),   S(270,   2),   S(155,   4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(205, -14),   S(150,  -4),   S(149,  51),   S( 67,  14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 65,   2),   S( 69,  31),   S( 68,  38),   S( -6, -137),

            /* kings: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-148,  18),  S(-164,  25),  S(-69, -14),   S(-80, -22),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-55,  -1),   S(-56,   1),   S(-61,  19),   S(-82,  13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-45,  14),   S(-17,   0),   S(-39,   8),   S(-113,  19),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(125,  -6),   S( 54,  19),   S(-17,  34),   S(-83,  33),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(225, -12),   S(143,  21),   S( 83,  36),   S( -1,  39),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(174,   5),   S( 96,  41),   S(114,  38),   S( -3,  58),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(182,  -3),   S(221,  -9),   S(104,  27),   S( 32,  25),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 72,  -7),   S( 75,   3),   S( 85, -20),   S(  4, -19),

            /* kings: QK */
            S(-65, -68),   S(-43, -65),   S(-88, -44),   S(-163, -16),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-38, -46),   S(-60, -14),   S(-71, -30),   S(-89, -23),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-96, -16),   S(-70,  -7),   S(-39, -21),   S(-82,  -4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-95,  13),   S(  4,   4),   S( 47,  -7),   S( 39,  -8),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-44,  12),   S( 64,  19),   S( 80,   6),   S(156, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-26,  34),   S( 86,  25),   S(155,   3),   S(168, -12),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 45,  -2),   S(145,   5),   S(121,   3),   S(137,  -7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-26, -19),   S( 42,  22),   S(127,  -3),   S( 52,   1),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: QQ */
            S(-178,  49),  S(-113,   5),  S(-158,  14),  S(-204,   3),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-122,  14),  S(-116,  14),  S(-87,  -9),   S(-124, -10),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-129,   4),  S(-73,  -8),   S(-61, -11),   S(-60,  -8),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-57, -13),   S( 17,   1),   S( 42,   1),   S(103, -14),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 54, -10),   S(149,  -4),   S(215, -17),   S(212, -23),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 67,  15),   S(173,  17),   S(163, -17),   S(121,  10),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  7,  -5),   S( 40,  64),   S( 74,  15),   S( 96,   7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-22, -133),  S( 26,  51),   S( 35,  10),   S( 65,   9),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

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
            S( 20,  18),    // friendly pawns 1 from king
            S(  8,  20),    // friendly pawns 2 from king
            S(  6,  14),    // friendly pawns 3 from king

            /* castling right available */
            S( 38, -41),

            /* castling complete */
            S( 19, -17),

            /* king on open file */
            S(-81,  15),

            /* king on half-open file */
            S(-31,  28),

            /* king on open diagonal */
            S( -8,  12),

            /* isolated pawns */
            S(  2, -18),

            /* doubled pawns */
            S(-17, -34),

            /* backward pawns */
            S(  0,   0),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 20,   1),   S( -2,   0),   S(  7,   2),   S( 14,  35),   S( 28,  33),   S( -1, -40),   S(-30,  54),   S(  3, -42),
            S( 14,   8),   S( 21,  -6),   S( 15,  31),   S( 19,  44),   S( 55,  -4),   S( -3,  27),   S( 18,   3),   S( 10, -14),
            S(-16,  16),   S( 20,  23),   S( -2,  53),   S( 30,  79),   S( 37,  30),   S( 37,  42),   S( 20,   9),   S( 16,  27),
            S( 38,  72),   S( 11,  53),   S( 68,  90),   S( 13,  84),   S(105,  73),   S(105,  39),   S( 12,  60),   S( 33,  49),
            S( 73, 107),   S(144, 103),   S(180, 166),   S(191, 161),   S(173, 117),   S(169, 166),   S(155,  70),   S( 84,  23),
            S( 79, 198),   S(121, 287),   S(118, 252),   S(121, 239),   S( 83, 197),   S( 54, 157),   S( 46, 210),   S( 26, 164),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  8,  10),   S( -6,  24),   S(-18,  24),   S(-40,  42),   S( 23,  -6),   S(-20,  22),   S(-14,  49),   S( 37,   8),
            S( 14,  20),   S(  3,  36),   S(-12,  36),   S( -3,  28),   S(-31,  41),   S(-47,  45),   S(-52,  68),   S( 22,  22),
            S( 16,  -3),   S(  9,  13),   S(-13,  27),   S( 20,  17),   S(-10,  19),   S(-34,  35),   S(-59,  58),   S(-11,  33),
            S( 49,  24),   S( 78,  42),   S( 35,  45),   S( 16,  37),   S( 20,  66),   S( 55,  51),   S( 46,  60),   S(  6,  67),
            S( 70,  71),   S(110,  89),   S(109,  72),   S( 34,  60),   S( -4,  53),   S( 89,  48),   S( 42,  79),   S(  5,  64),
            S(274,  89),   S(258, 126),   S(239, 142),   S(250, 132),   S(261, 135),   S(253, 140),   S(303, 143),   S(284, 117),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 47,  15),   S( 20,  28),   S( 20,  30),   S( 23,  55),   S( 93,  29),   S( 29,  24),   S( 26,  19),   S( 60,  14),
            S( 10,  14),   S(  6,  14),   S( 24,  14),   S( 26,  27),   S( 16,  11),   S( -5,   7),   S( 12,   4),   S( 30,  -5),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-10, -14),   S( -6, -14),   S(-24, -14),   S(-26, -27),   S(-16, -11),   S(  5,  -7),   S(-12,  -4),   S(-30,   5),
            S(-47, -15),   S(-20, -28),   S(-20, -30),   S(-23, -55),   S(-93, -29),   S(-29, -24),   S(-26, -19),   S(-60, -14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 38,  -5),   S( 42,  11),   S( 58,  14),   S( 60,  26),   S( 41,  31),   S( 43,  10),   S( 31,   6),   S( 72, -14),
            S( -2,  -2),   S( 23,  20),   S( 23,   9),   S( 34,  29),   S( 35,   9),   S( 21,  12),   S( 40,   3),   S( 15,  -3),
            S(-17,  10),   S( 28,  26),   S( 65,  34),   S( 51,  32),   S( 65,  33),   S( 71,  12),   S( 26,  31),   S( 31,   4),
            S( 71,  47),   S(154,  23),   S(140,  81),   S(173,  56),   S(189,  52),   S( 89,  80),   S(106,  21),   S(107,  -4),
            S(106,  77),   S(245,  45),   S(227, 138),   S(201,  37),   S(262, 124),   S(166, 113),   S(229,  23),   S(-63, 104),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* enemy king outside passed pawn square */
            S( 19, 210),

            /* passed pawn/friendly king distance penalty */
            S( -2, -20),

            /* passed pawn/enemy king distance bonus */
            S(  0,  36),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 79, -37),   S( 56,   5),   S( 51,  22),   S( 52,  56),   S( 75,  37),   S(178,  35),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 35,  -2),   S( 20,  51),   S( 13,  57),   S(  8, 108),   S( 62, 116),   S(195, 135),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-62,  23),   S(-17, -23),   S(-23, -28),   S(-28,   2),   S( 36, -12),   S(276, -88),   S(  0,   0),    // blocked by rooks
            S(  0,   0),   S( 15, -39),   S( 58, -32),   S(  0,  21),   S(  0, -10),   S( 28, -174),  S(-48, -186),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S( -1,  13),   S( 84,   6),   S( 52, -54),   S( 91, -15),   S(234,   4),   S(390,  83),   S(  0,   0),    // blocked by kings

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
            S( -2,   0),   S(  1,  -8),   S( -5,  -4),   S( -4, -50),   S(-12, -15),   S(-29,   5),   S(-23,  -5),   S( -1,  -6),
            S( -5, -10),   S( -9, -10),   S(-15, -13),   S(-10, -18),   S(-16, -18),   S(-17,  -5),   S(-16,  -7),   S( -2, -11),
            S( -6, -12),   S(  3, -26),   S(  0, -41),   S( -1, -56),   S(-17, -35),   S(-13, -30),   S(-12, -22),   S( -2,  -7),
            S(  4, -28),   S(  7, -38),   S(  0, -36),   S(  2, -50),   S( -7, -38),   S( -8, -31),   S( -7, -32),   S( -3, -23),
            S( 20, -36),   S(  8, -52),   S( 17, -62),   S(  8, -62),   S( 32, -77),   S( -6, -52),   S( 47, -95),   S(-11, -18),
            S( 65, -56),   S(106, -107),  S(103, -113),  S( 97, -135),  S( 95, -166),  S(111, -122),  S( 78, -153),  S(158, -157),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 36,   7),

            /* rook on half-open file */
            S( 11,  35),

            /* rook on seventh rank */
            S( 17,  30),

            /* doubled rooks on file */
            S( 20,  23),

            /* queen on open file */
            S(-11,  40),

            /* queen on half-open file */
            S(  7,  31),

            /* pawn push threats */
            S(  0,   0),   S( 32,  30),   S( 36, -22),   S( 34,  28),   S( 35, -24),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 67,  72),   S( 49,  91),   S( 74,  60),   S( 58,  10),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-12,  11),   S( 48,  29),   S( 91,   5),   S( 29,  62),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 31,  63),   S( -5,  41),   S( 56,  38),   S( 37, 104),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-17,  66),   S( 56,  72),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-14,  16),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats
        };
    }
}
