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

        // Solution sample size: 12000000, generated on Thu, 26 Oct 2023 15:39:28 GMT
        // Solution K: 0.003077, error: 0.085994, accuracy: 0.4895
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(121, 166),   S(503, 535),   S(551, 583),   S(675, 1016),  S(1628, 1753), S(  0,   0),

            /* piece square values */
            #region piece square values

            /* pawns: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-57,  16),   S(-61,  19),   S(-44,   6),   S(-25, -14),   S(-43,  35),   S( 58,  -2),   S( 64, -31),   S( -6, -37),
            S(-63,  11),   S(-54,   0),   S(-59, -21),   S(-59, -15),   S(-41, -17),   S(-16, -16),   S( 13, -25),   S(-46, -19),
            S(-30,  12),   S(-39,  13),   S(-17, -26),   S(-31, -48),   S(-17, -30),   S( 17, -30),   S( -3, -11),   S(-11, -24),
            S(-24,  71),   S(-30,  37),   S( -1,  15),   S( 15,   0),   S( 26, -22),   S( 22, -23),   S( 30,   0),   S( 63, -17),
            S( 37,  72),   S(-27,  74),   S(-16,  67),   S( 32,  62),   S(112,  79),   S(157,  11),   S(156,   3),   S(142,   9),
            S( 96,  93),   S( 85, 108),   S( 88,  98),   S(111,  53),   S( 75,  68),   S( 97, -10),   S( 60,  41),   S( 90, -14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-96,  34),   S(-103,  47),  S(-63,  18),   S(-69,  45),   S(-40,  -5),   S( 54, -35),   S( 63, -46),   S( -5, -21),
            S(-92,   7),   S(-97,  10),   S(-73, -20),   S(-72, -21),   S(-36, -46),   S(-26, -29),   S( 16, -41),   S(-39, -14),
            S(-68,  27),   S(-48,  16),   S(-37, -12),   S(-31, -44),   S(-27, -29),   S( -6, -25),   S(-10, -23),   S(  4, -23),
            S(-64,  76),   S(-16,   9),   S( 10,  -4),   S( 12,   9),   S(  2, -10),   S(-32,  30),   S( 22,  11),   S( 63,   1),
            S( 67,  34),   S( 71, -13),   S(108,  -9),   S( 89,  36),   S(131, 111),   S( 50, 102),   S( 36, 103),   S( 88,  94),
            S(104,  10),   S( 33,  46),   S(121, -16),   S( 80,  81),   S( 80, 106),   S( 97, 159),   S(143, 115),   S(158, 113),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-31,  12),   S( 13,   1),   S( -4,  -4),   S(-26,  20),   S(-48,  21),   S(-12,   4),   S(-29,   2),   S(-73,  24),
            S(-25,   9),   S(  0, -12),   S(-45, -16),   S(-61, -25),   S(-44, -22),   S(-38, -23),   S(-55, -13),   S(-108,  10),
            S( -2,  15),   S( -2,  14),   S(-18,  -8),   S(-23, -40),   S(-38, -15),   S(-12, -27),   S(-28, -15),   S(-51,  -3),
            S(  0,  71),   S( -9,  61),   S( 14,  36),   S( 23,  20),   S(  7,  -4),   S( -4, -22),   S(  4, -11),   S(  5,   8),
            S( 47,  92),   S(-16, 120),   S(  0, 119),   S( 55,  78),   S(104,  76),   S(112,  14),   S(108,  -9),   S( 54,  42),
            S(154,  69),   S(208,  87),   S(128, 153),   S(130,  96),   S( 59,  72),   S( 54,   3),   S( 55,  24),   S( 14,  33),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-63,   9),   S(  2, -10),   S( 35, -29),   S( 14, -18),   S( -6, -24),   S( 12, -29),   S(  4, -37),   S(-34, -11),
            S(-57,  -2),   S(-19, -13),   S(-33, -26),   S(-39, -36),   S(-18, -47),   S(-39, -46),   S(-30, -44),   S(-76, -14),
            S(-22,  12),   S(-22,   7),   S(  9, -27),   S( -3, -51),   S( -3, -38),   S( -2, -49),   S( -6, -38),   S(-41, -22),
            S( 49,  19),   S( 66, -15),   S( 62, -23),   S( 59,  -3),   S( 18, -23),   S(-14, -18),   S( 13, -23),   S( 37, -12),
            S(215, -49),   S(159, -41),   S(114, -16),   S(120,  25),   S( 55,  70),   S( 36,  47),   S(  2,  33),   S( 83,  44),
            S(170, -18),   S( 70,  30),   S( 71, -22),   S( 42,  53),   S(101,  25),   S( 51,  41),   S( 93,  63),   S(121,  61),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: KK */
            S(-133, -40),  S(-16, -88),   S(-45, -17),   S(-24,   9),   S(-11,  -2),   S( -9,   8),   S(-12, -35),   S(-62, -50),
            S(-38, -60),   S(-52,  26),   S( -7,   0),   S( 13,   4),   S(  9,  12),   S( 14,  27),   S(  4,  18),   S( -8,   7),
            S(-23, -17),   S( -5,  23),   S(  3,  39),   S( 26,  59),   S( 36,  60),   S( 16,  33),   S( 10,  42),   S(  5,  11),
            S( -9,  33),   S( 17,  57),   S( 28,  88),   S( 31,  97),   S( 40, 102),   S( 52,  87),   S( 53,  84),   S( 24,  44),
            S( 10,  49),   S( 19,  57),   S( 46,  62),   S( 52,  87),   S( 19, 109),   S( 45, 113),   S( 19, 114),   S( 60,  55),
            S(-24,  64),   S(  9,  58),   S( 33,  72),   S( 45,  74),   S( 81,  53),   S(160,  42),   S( 37,  87),   S( -4,  72),
            S(-49,  59),   S(-37,  71),   S(-16,  75),   S( 41,  70),   S( 52,  80),   S(100,  15),   S(-14,   7),   S(  6,  22),
            S(-313,  30),  S(-43,  72),   S(-97,  87),   S( 26,  59),   S( 62,  46),   S(-90,  30),   S(  9,  26),   S(-240, -68),

            /* knights: KQ */
            S(-72, -154),  S(-39, -53),   S(-52,  -8),   S(-35, -15),   S(-19, -12),   S(-16, -17),   S(-35, -24),   S(-39, -92),
            S(-61, -47),   S(-62,  18),   S( -2, -34),   S( 16, -22),   S(  1,  -9),   S( 31,   1),   S(-51,  12),   S(-44, -24),
            S(-31, -22),   S( 17, -36),   S( -6,  22),   S( 47,   6),   S( 24,   9),   S( 31, -15),   S( -7,  16),   S(-33,  33),
            S(  5,  -1),   S( 73, -22),   S( 44,  19),   S( 63,  19),   S( 54,  37),   S( 51,  29),   S( 55,   4),   S( -1,   9),
            S( 65, -33),   S( 28,  -9),   S( 78,   9),   S( 86,   5),   S( 95,  13),   S( 52,  19),   S( 53,   8),   S( 18,   5),
            S( 40, -38),   S( 59, -60),   S(121, -55),   S(152, -42),   S( 81,   7),   S( 61,  12),   S( 10,  33),   S(  6,  21),
            S( -8, -15),   S(-12, -43),   S( 11, -32),   S( 73, -12),   S( 14,  17),   S( 42,  18),   S(-10,  28),   S(-30, -12),
            S(-286, -102), S(-20, -41),   S(  2, -27),   S(  0, -14),   S(-16, -31),   S( 19,  28),   S(-28,   3),   S(-141, -59),

            /* knights: QK */
            S(-132, -98),  S(-73,  15),   S(-45,   9),   S(-75,  36),   S(-46, -10),   S(-37, -32),   S(-56, -40),   S(-65, -89),
            S(-23, -53),   S(  0,  16),   S( -4,  -2),   S(  2,   4),   S( -3,   3),   S(-14,  -2),   S(-65,   1),   S(-27, -64),
            S(-21,  31),   S( -7,  -2),   S( 37, -13),   S( 44,  26),   S( 52,   8),   S(-15,  22),   S( 13, -12),   S(-35, -40),
            S( 15,  24),   S( 80,   6),   S( 69,  31),   S( 54,  49),   S( 53,  27),   S( 54,  35),   S( 51,   2),   S( -8,  12),
            S(  3,  29),   S( 50,  22),   S( 86,  18),   S(120,  12),   S( 65,  39),   S( 83,  30),   S( 41,  13),   S( 45, -23),
            S(-59,  38),   S( 34,  24),   S( 42,  45),   S( 62,  13),   S( 94,  -5),   S(148, -23),   S( 78,  -3),   S( 49, -18),
            S(-19,  27),   S(-53,  44),   S( -1,  35),   S( 44,  41),   S( 11,  60),   S( 26,  -1),   S(-21,  -8),   S( 22, -35),
            S(-133, -69),  S(-23,  36),   S(  7,  39),   S( 27,   6),   S(  7,  19),   S(-41, -36),   S(-15, -22),   S(-32, -45),

            /* knights: QQ */
            S(-13, -120),  S(-43, -25),   S(-15, -13),   S(-57,  -6),   S( -8,   4),   S(-46, -14),   S(-45, -63),   S(-53, -72),
            S(-39, -56),   S(-32,  10),   S( -4, -24),   S( 24, -38),   S( -6, -26),   S( 19, -41),   S( -5, -61),   S(-122, -84),
            S( -4, -17),   S( 16, -15),   S( 14, -14),   S( 30,   6),   S( 19,   1),   S(-18,  11),   S(-19,   5),   S(-29, -54),
            S(-11,  36),   S( 40,  12),   S( 87,  -1),   S( 80,   3),   S( 40,   8),   S( 49,   5),   S( 35,   7),   S(-17,   0),
            S( 42,  -5),   S( 37,  -4),   S( 72,   0),   S( 66,   3),   S( 87,  -6),   S( 82,  -3),   S( 62, -18),   S(-21,  22),
            S( 45,  -7),   S( -4,  16),   S( 62,  -5),   S( 96, -22),   S( 68, -10),   S( 60,  17),   S( 40,   6),   S( 23,   8),
            S( -6,  21),   S( -4,   7),   S(-25, -12),   S( 33,  35),   S( 13,  24),   S( -7,  17),   S(-26,  29),   S(-17,  10),
            S(-154, -102), S( -9,  60),   S( -3, -26),   S( -6,  21),   S(-26, -18),   S(  3,  10),   S(-12,  28),   S(-51, -43),

            /* bishops: KK */
            S(  8, -14),   S( 25, -27),   S(  7, -12),   S(-16,  42),   S(  7,  32),   S( 12,  29),   S( 11,   2),   S( 19, -68),
            S( 24,  -6),   S( 19,  14),   S( 20,  18),   S( 14,  42),   S( 25,  31),   S( 27,  30),   S( 52,   5),   S( 25, -52),
            S(  1,  32),   S( 23,  41),   S( 29,  70),   S( 28,  52),   S( 22,  80),   S( 29,  48),   S( 22,  29),   S( 12,  20),
            S(  2,  38),   S(  5,  67),   S( 25,  75),   S( 55,  60),   S( 45,  53),   S( 20,  60),   S( 19,  47),   S( 33, -14),
            S( -6,  63),   S( 29,  31),   S( 12,  56),   S( 55,  48),   S( 34,  63),   S( 51,  46),   S( 30,  38),   S( 15,  55),
            S( 13,  36),   S( 16,  62),   S(-12,  66),   S( -2,  63),   S(  0,  71),   S( 55,  80),   S( 60,  47),   S(  5,  79),
            S(-50,  79),   S(-23,  72),   S( -8,  68),   S(-24,  69),   S(-61,  90),   S(-35,  67),   S(-82,  64),   S(-52,  46),
            S(-115, 110),  S(-88,  94),   S(-76,  98),   S(-112, 114),  S(-57,  85),   S(-131,  89),  S(-16,  39),   S(-84,  55),

            /* bishops: KQ */
            S(-53,  11),   S(-12,  11),   S(-14,   7),   S(-19,   8),   S( -7,   5),   S( 19, -21),   S( 81, -94),   S(  9, -72),
            S(-38,  -9),   S(  2,   8),   S(  8,  10),   S( 13,   3),   S( 37,  -8),   S( 66, -31),   S( 99, -46),   S( 17, -72),
            S( -4,  10),   S(  3,  12),   S( 35,  17),   S( 50,   2),   S( 36,  17),   S( 70,   3),   S( 47, -10),   S( 64, -44),
            S( 37,  15),   S(  8,  27),   S( 24,  17),   S( 73,  -8),   S( 96,  -3),   S( 43,  29),   S( 46,  16),   S( -5,  28),
            S( -1,  26),   S( 67, -14),   S( 72,  -9),   S(132, -54),   S( 98,  -3),   S( 37,  27),   S( 39,   9),   S( 21,   4),
            S(  6,  -7),   S( 70, -15),   S( 86,  -4),   S( -2,  31),   S( 23,  23),   S( 30,  30),   S( 28,  37),   S(-14,  36),
            S(-78, -13),   S(  9,   0),   S( -2,  14),   S(-22,  27),   S(  3,  29),   S(-11,  38),   S(-45,  61),   S(-67,  68),
            S(-17,   4),   S(-29,  -9),   S(  5,  -6),   S( -6,  38),   S(-39,  37),   S(-53,  44),   S(-34,  44),   S( -4,  18),

            /* bishops: QK */
            S( 70, -75),   S(  4, -35),   S( 13,  15),   S(-20,  19),   S( 11,  -9),   S( -8,  -4),   S(-52,  15),   S(-131,  25),
            S( 23, -35),   S( 84, -27),   S( 45,  -4),   S( 39,  12),   S(  5,  14),   S( -9,  12),   S(-34,  16),   S(-18, -17),
            S( 68, -23),   S( 49,   6),   S( 54,  21),   S( 30,  31),   S( 25,  42),   S( 13,  13),   S( 15,  -8),   S(-23,  -4),
            S( -9,  34),   S( 42,  36),   S( 37,  41),   S( 89,   6),   S( 73,   1),   S( 21,  32),   S(-31,  33),   S( -2,  13),
            S( 34,  31),   S( 25,  25),   S( 34,  34),   S( 83,   9),   S( 52,   9),   S( 51,  20),   S( 45,  -5),   S(-40,  49),
            S(-56,  60),   S( 22,  60),   S( 24,  31),   S( 18,  46),   S( 39,  40),   S( 69,  24),   S( 57,  -2),   S( 12,  26),
            S(-41,  75),   S(-16,  51),   S(  3,  44),   S(-24,  58),   S(-79,  65),   S(-11,  37),   S(-15,  13),   S(-92,  16),
            S(-81,  51),   S(-63,  68),   S(-50,  47),   S(-30,  67),   S( 15,  19),   S(-32,  18),   S(-20,  -4),   S(-58,  -7),

            /* bishops: QQ */
            S(-89, -20),   S(-32, -16),   S(  7, -33),   S(-81,   8),   S(-16, -13),   S( -5, -39),   S( 37, -71),   S( 11, -77),
            S(  8, -42),   S( 60, -41),   S( 36, -26),   S( 33, -19),   S( -5,  -7),   S( 55, -49),   S( 11, -36),   S( 42, -86),
            S( 10,  -7),   S( 45, -28),   S( 38,  -9),   S( 23, -14),   S( 37, -10),   S( 42, -18),   S( 38, -47),   S( 11, -29),
            S( 22,   8),   S( 35, -13),   S( 39, -22),   S( 86, -33),   S( 58, -24),   S( 63, -20),   S( 21, -23),   S( -2, -23),
            S( 10,   7),   S( 60, -22),   S( 24, -26),   S( 92, -47),   S( 88, -48),   S( 28, -24),   S( 40, -27),   S(-29,   4),
            S(-36,  -8),   S( 76, -10),   S( 61, -25),   S( 77, -19),   S( 42,   3),   S( -8,  -4),   S(-16,  -3),   S(-22,  -2),
            S(-64, -31),   S( -8,  -8),   S( 30,  -4),   S(-11,   1),   S( 23, -13),   S(-44,   6),   S(-24,  13),   S(-35,   7),
            S(-14, -18),   S( -4, -15),   S( -6,  -3),   S( 14,   9),   S( 19,  18),   S(-29,   9),   S(-24,  16),   S(-21,  23),

            /* rooks: KK */
            S(-27,  98),   S(-17,  87),   S(-11,  77),   S(  0,  54),   S(  9,  35),   S(  5,  62),   S( 21,  54),   S(-19,  38),
            S(-45, 101),   S(-39, 103),   S(-27,  79),   S(-15,  50),   S(-16,  45),   S( 17,  13),   S( 43,  32),   S(-11,  44),
            S(-46, 100),   S(-37, 101),   S(-30,  85),   S(-21,  55),   S(  0,  34),   S(  3,  38),   S( 31,  45),   S(  6,  29),
            S(-43, 129),   S(-25, 124),   S(-14, 100),   S( -9,  82),   S(-16,  66),   S(-18,  96),   S( 40,  85),   S(-15,  76),
            S(-21, 154),   S( -2, 140),   S( 11, 113),   S( 25,  86),   S(  2, 104),   S( 49,  83),   S( 83,  97),   S( 20,  98),
            S(  3, 151),   S( 40, 126),   S( 36, 111),   S( 32,  97),   S( 64,  74),   S(133,  64),   S(121,  82),   S( 77,  90),
            S(-19, 137),   S(-11, 132),   S( 20,  92),   S( 46,  65),   S( 34,  70),   S(114,  55),   S(101,  94),   S(130,  71),
            S( 79, 112),   S( 78, 124),   S( 52, 109),   S( 66,  82),   S( 28, 113),   S( 94, 110),   S(136, 112),   S(152,  86),

            /* rooks: KQ */
            S(-55,  17),   S(-12, -25),   S(-21, -24),   S(-30, -12),   S(-34,  -3),   S(-32,   5),   S(-41,  25),   S(-78,  47),
            S(-77,   9),   S(-11, -34),   S(-47, -19),   S(-37, -18),   S(-41, -14),   S(-29,  -1),   S(-42,  18),   S(-81,  13),
            S(-75,  14),   S(-39, -18),   S(-63,  -6),   S(-23, -31),   S(-62, -11),   S(-52,  -1),   S(-27,  15),   S(-49,   7),
            S(-69,  31),   S(-20,   9),   S(-54,  23),   S(-22,   7),   S(-27,  11),   S(-56,  40),   S( -4,  44),   S(-47,  35),
            S(-35,  46),   S(  9,  22),   S(-13,  19),   S(  2,   6),   S( 13,   5),   S( 26,  11),   S( 44,  30),   S( -4,  46),
            S( 34,  32),   S( 89,   3),   S( 41,   4),   S( 99, -31),   S( 41,   0),   S( 86,   1),   S(108,  14),   S( 68,  31),
            S(-16,  42),   S( 13,  21),   S( 63, -14),   S( 54, -16),   S( 60, -17),   S( 33,  -2),   S( 59,  26),   S( 45,  26),
            S( 31,  60),   S( 40,  54),   S( 78,   6),   S( 68,   6),   S( 70,   4),   S(110,   4),   S( 84,  44),   S(102,  16),

            /* rooks: QK */
            S(-92,  54),   S(-34,  36),   S(-32,  17),   S(-27, -14),   S(-18, -25),   S(-23, -18),   S(-13, -23),   S(-48,   4),
            S(-56,  30),   S(-56,  30),   S(-46,  19),   S(-46,  -4),   S(-68,  -8),   S(-46, -31),   S(-29, -28),   S(-55, -21),
            S(-50,  26),   S(-19,  27),   S(-57,  32),   S(-58,  -3),   S(-44,  -4),   S(-59,  -2),   S(-17, -28),   S(-45, -18),
            S(-25,  41),   S(  5,  26),   S(-34,  35),   S(-34,  22),   S(-46,  11),   S(-57,  24),   S(-28,  18),   S(-47,  15),
            S( 49,  32),   S( 41,  45),   S( 21,  37),   S(-24,  42),   S( -8,  21),   S(-14,  36),   S( 22,  23),   S( 13,   3),
            S( 54,  51),   S(113,  31),   S( 81,  21),   S( -8,  49),   S( 42,  10),   S( 76,  14),   S(108,  10),   S( 31,  37),
            S( 41,  26),   S( 71,  19),   S( 66,   2),   S(-19,  29),   S(-20,  22),   S( 72, -16),   S(-11,  30),   S( 15,  35),
            S(233, -86),   S(147,   1),   S( 58,  35),   S( 15,  31),   S(  4,  39),   S( 57,  22),   S( 41,  45),   S( 60,  37),

            /* rooks: QQ */
            S(-61, -16),   S(-13, -41),   S( -3, -58),   S(  5, -73),   S(  4, -66),   S(-10, -22),   S(-22,  -6),   S(-45,  18),
            S(-22, -51),   S(-29, -23),   S(-55, -25),   S( -2, -75),   S(-20, -64),   S(-21, -57),   S(-30, -28),   S(-58,   9),
            S(-81,  -7),   S( -7, -34),   S(  6, -58),   S(-18, -41),   S(-51, -26),   S(-53, -10),   S(-66,  12),   S(-64,  17),
            S(-57,  -4),   S( 12, -11),   S(-50,   3),   S(-17, -27),   S(-38, -24),   S(-29,   2),   S(-46,  26),   S(-47,  31),
            S( 35,  -4),   S( 71, -10),   S( 20, -21),   S( 69, -27),   S( 63, -34),   S( 66, -16),   S( 31,  22),   S(-17,  29),
            S( 78,  -3),   S( 66, -11),   S( 96, -26),   S( 56, -12),   S( 55, -19),   S( 63, -12),   S( 77,   3),   S( 31,  22),
            S( 40,   4),   S( 74,  -6),   S( 29, -14),   S( 39, -26),   S(101, -69),   S( 87, -54),   S( 30,   5),   S(  9,  27),
            S( 14,  45),   S( -4,  63),   S( -1,  26),   S( -8,  30),   S( 41,  -9),   S( 48,   1),   S( 60,  36),   S( 48,  40),

            /* queens: KK */
            S( 36,  62),   S( 50,  21),   S( 61,  -8),   S( 67,  -5),   S( 78, -59),   S( 58, -92),   S(  0, -58),   S(  8,  23),
            S( 27,  89),   S( 52,  55),   S( 53,  43),   S( 64,  22),   S( 65,  14),   S( 84, -35),   S(102, -79),   S( 40, -11),
            S( 24,  73),   S( 42,  65),   S( 50,  63),   S( 34,  74),   S( 48,  59),   S( 43,  84),   S( 52,  83),   S( 52,  34),
            S( 31,  97),   S( 20, 105),   S( 25, 102),   S( 29, 100),   S( 17, 102),   S( 25, 123),   S( 43, 122),   S( 20, 150),
            S( 28, 121),   S( 29, 124),   S(  3, 137),   S(  3, 141),   S(  4, 165),   S( 10, 205),   S( 16, 234),   S( 28, 207),
            S( 11, 131),   S( 26, 158),   S(-15, 173),   S(-33, 192),   S( 12, 198),   S( 75, 197),   S( 36, 254),   S( 10, 297),
            S(-31, 204),   S(-46, 231),   S(-19, 203),   S(-21, 201),   S(-78, 269),   S( 56, 181),   S( 44, 291),   S(106, 241),
            S( -9, 166),   S( 22, 158),   S( 45, 145),   S( 31, 154),   S( 38, 170),   S(126, 169),   S(185, 152),   S(165, 182),

            /* queens: KQ */
            S( 32,  -3),   S( 33, -45),   S( 26, -74),   S( 41, -97),   S( 38, -45),   S( 18, -75),   S(-35, -37),   S(-64, -17),
            S( 26,  31),   S( 56, -44),   S( 62, -97),   S( 27, -40),   S( 48, -51),   S( 32, -25),   S( 15, -66),   S(-49, -22),
            S( 14,  33),   S( 29, -32),   S( 51, -84),   S( 42, -54),   S( 29, -25),   S( 45,  -3),   S( -5,  28),   S(-19,  24),
            S( 14,  38),   S( 48, -29),   S( -6,  10),   S( 18, -24),   S( 30,   2),   S(  3,  54),   S( -7,  73),   S(-10,  58),
            S(  0,  61),   S( 57,  -6),   S(  9,  47),   S( 32,  -7),   S( 57,   8),   S(-36,  85),   S(  2,  97),   S( -3,  99),
            S( 31,  50),   S(115,  25),   S( 66,  41),   S( 84,   4),   S( 29,  41),   S( 32,  52),   S( 21,  82),   S(-13, 101),
            S( 80,  72),   S( 70,  44),   S( 43,  31),   S( 60,  71),   S( 41,  33),   S(-25, 117),   S( 13, 105),   S( -1,  80),
            S(114,  42),   S(121,  56),   S(100,  82),   S(107,  43),   S( 29,  74),   S( 69,  42),   S( 75,  35),   S( 14,  31),

            /* queens: QK */
            S(-42, -70),   S(-85, -64),   S( -4, -106),  S(  6, -71),   S( 37, -130),  S( -8, -70),   S(-19, -43),   S(-47,  17),
            S(-56, -55),   S(-24, -63),   S( 51, -115),  S( 41, -69),   S( 32, -74),   S( 30, -74),   S( 30, -47),   S( 21, -14),
            S(-36,  -3),   S( -2,  -1),   S( 13,  -9),   S(  1,  10),   S( 13, -29),   S(  0, -32),   S( 30, -41),   S(  4,  13),
            S( -3,   2),   S(-14,  44),   S(-24,  59),   S(  4,  14),   S( -6,  17),   S(  7,  -2),   S( 19, -13),   S( 14,   8),
            S( -9,  34),   S(-12,  53),   S(-21,  81),   S(-40,  61),   S(  4,  26),   S(  6,  31),   S( 10,  27),   S( -8,  20),
            S( -9,  41),   S( 24,  60),   S(-20,  95),   S(-36,  81),   S( 25,  51),   S( 58,  34),   S( 22,  48),   S(  7,  73),
            S(-23,  62),   S(-50,  85),   S(-30,  99),   S(-48, 105),   S(-27,  76),   S(-16,  52),   S( 26,  74),   S(  8, 108),
            S(-20,  10),   S( 30,  59),   S( 31,  47),   S( 23,  51),   S( 35,  37),   S( 24,  58),   S( 74,  52),   S( 91,  68),

            /* queens: QQ */
            S(-50, -53),   S(-77, -46),   S(-41, -50),   S(-26, -38),   S(  5, -57),   S(-19, -47),   S(-45, -32),   S(-24, -24),
            S(-101, -47),  S(-38, -52),   S( 37, -95),   S( 31, -86),   S(  2, -56),   S(-10, -74),   S(-22,  -4),   S(-55, -12),
            S(-10,  15),   S( -6,  28),   S(  5, -44),   S(-11,  -2),   S(-23,  -1),   S( -6, -17),   S(  6,  11),   S(-18,   5),
            S( -6, -16),   S( 25, -41),   S( -3, -33),   S(-23, -18),   S( 21, -53),   S( -5, -41),   S(-29, -25),   S(-33,  43),
            S(-21,  14),   S(  4,  44),   S( 11, -10),   S(  7, -19),   S( 24, -10),   S( -3, -33),   S( 11,   8),   S(  8, -14),
            S( 17,  37),   S( 23,  61),   S( 83,  33),   S( 11,  33),   S(-21,  -7),   S( 16,   4),   S( 26,   8),   S( 19,  17),
            S(114,  60),   S( 33,  30),   S( 30,  42),   S( 22,  38),   S(  8,  33),   S(  8,  14),   S(  9,  42),   S(-10,  32),
            S( 74,   2),   S( 40,  39),   S( 27,  34),   S( 13,   9),   S( 17,  22),   S( 24,  -3),   S( 37,  -1),   S( 36,  10),

            /* kings: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-114, -11),  S(-128,  15),  S(-44, -17),   S(-59, -43),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-63,  10),   S(-56,  13),   S(-27,   1),   S(-52,  -1),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-28,  29),   S(-67,  30),   S(-52,  11),   S(-110,  13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 96,  17),   S( 37,  28),   S( -9,  27),   S(-111,  22),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(235,  -9),   S(274, -29),   S(169,   1),   S( 22,   4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(209, -12),   S(259, -21),   S(272,  -4),   S(171,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(178,  -6),   S(128,  -5),   S(153,  49),   S( 48,  11),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 56,  14),   S( 40,  46),   S( 46,  38),   S(-16, -180),

            /* kings: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-142,   4),  S(-163,  12),  S(-64, -25),   S(-75, -25),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-58,   0),   S(-64,  -1),   S(-54,   9),   S(-71,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-36,  18),   S(-14,   2),   S(-47,   8),   S(-118,  13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(122,   6),   S( 78,  18),   S( 22,  33),   S(-75,  31),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(194,   7),   S(154,  26),   S( 99,  41),   S(-29,  49),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(142,  20),   S(107,  44),   S(103,  46),   S( 13,  54),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(107,  10),   S(165,  18),   S(121,  31),   S( 29,  17),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 67,  -3),   S( 44,   0),   S( 63,  10),   S( 11, -17),

            /* kings: QK */
            S(-61, -92),   S(-44, -81),   S(-88, -58),   S(-167, -21),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-45, -51),   S(-55, -28),   S(-78, -33),   S(-95, -26),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-138,  -5),  S(-62, -14),   S(-67, -11),   S(-92,   1),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-94,  16),   S(-30,  14),   S( 42,  -6),   S( 45,  -4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-25,   7),   S( 36,  21),   S(110,   2),   S(161, -18),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-32,  27),   S(121,  16),   S(129,  11),   S(161,  -6),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  8,   4),   S(157,   5),   S(105,  12),   S(129,  -2),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-26, -20),   S( 43,  26),   S(106,  -3),   S( 40,  -1),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: QQ */
            S(-168,  37),  S(-103, -15),  S(-156,   3),  S(-185,  -9),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-125,   3),  S(-94,  -7),   S(-89, -14),   S(-137,  -6),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-126,  -4),  S(-83, -12),   S(-45, -12),   S(-78,   4),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-22, -22),   S( 35,   4),   S( 86,  -2),   S(112, -10),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  1,   1),   S(148,  -3),   S(176,  -1),   S(216, -21),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 76,  14),   S(140,  19),   S(134,  -6),   S(118,  12),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 15,  11),   S( 50,  61),   S( 44,  28),   S(115,  11),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-38, -137),  S( 22,  80),   S( 44,  26),   S( 28,  10),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            #endregion

            /* mobility weights */
            S(  9,   7),    // knights
            S(  6,   4),    // bishops
            S(  4,   4),    // rooks
            S(  1,   5),    // queens

            /* center control */
            S(  2,   7),    // D0
            S(  2,   6),    // D1

            /* squares attacked near enemy king */
            S( 25,  -3),    // attacks to squares 1 from king
            S( 21,  -1),    // attacks to squares 2 from king
            S(  6,   1),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S( 21,  19),    // friendly pawns 1 from king
            S( 10,  19),    // friendly pawns 2 from king
            S(  6,  15),    // friendly pawns 3 from king

            /* castling right available */
            S( 44, -47),

            /* castling complete */
            S( 15, -18),

            /* king on open file */
            S(-81,  12),

            /* king on half-open file */
            S(-32,  26),

            /* king on open diagonal */
            S(-11,  12),

            /* isolated pawns */
            S( -4, -12),

            /* doubled pawns */
            S(-14, -33),

            /* backward pawns */
            S(  0,   0),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 26, -10),   S(  3,   0),   S( 11,  -6),   S( 20,  11),   S( 27,  36),   S(  0, -44),   S(-25,  56),   S(  8, -50),
            S(  7,  -2),   S( 34,  -1),   S( 11,  27),   S( 20,  47),   S( 49,  -3),   S(  4,  28),   S( 19,  -4),   S( 16, -13),
            S( -8,  22),   S( 19,   7),   S(  5,  61),   S( 25,  84),   S( 41,  32),   S( 34,  38),   S( 32,  10),   S( 12,  24),
            S( 27,  49),   S( 30,  54),   S( 42,  96),   S( 17,  97),   S( 92,  68),   S( 87,  51),   S( 22,  55),   S( 25,  59),
            S( 99,  92),   S(138, 116),   S(107, 176),   S(157, 178),   S(177, 146),   S(157, 146),   S(158,  96),   S( 86,  62),
            S( 91, 221),   S(132, 308),   S(109, 231),   S( 99, 197),   S( 69, 167),   S( 61, 162),   S( 51, 190),   S( 25, 126),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,  23),   S(-17,  32),   S(-32,  37),   S(-56,  81),   S( 24,  -7),   S(-24,  23),   S(-10,  66),   S( 21,  17),
            S( 10,  32),   S( -4,  48),   S(-15,  40),   S( -9,  36),   S(-23,  44),   S(-56,  56),   S(-55,  85),   S( 23,  29),
            S(  5,  15),   S(  2,  27),   S( -8,  30),   S( 11,  34),   S( -6,  26),   S(-42,  42),   S(-66,  80),   S(-26,  49),
            S( 43,  26),   S( 73,  43),   S( 35,  38),   S( 12,  33),   S( 19,  60),   S( 61,  50),   S( 27,  69),   S(-21,  76),
            S( 73,  71),   S(114, 107),   S(105,  77),   S( 50,  57),   S(-35,  40),   S( 94,  50),   S( 63,  97),   S( 20,  58),
            S(266,  83),   S(240, 129),   S(236, 140),   S(227, 138),   S(212, 148),   S(220, 141),   S(267, 146),   S(280, 118),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 41,   9),   S(  8,  19),   S( 20,  25),   S( 15,  52),   S( 92,  37),   S( 30,  17),   S( 13,   4),   S( 45,   9),
            S(  6,  16),   S(  5,  12),   S( 25,  14),   S( 24,  31),   S( 18,  15),   S( -3,   6),   S( 12,   2),   S( 34,  -8),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -6, -16),   S( -5, -12),   S(-25, -14),   S(-24, -31),   S(-18, -15),   S(  3,  -6),   S(-12,  -2),   S(-34,   8),
            S(-41,  -9),   S( -8, -19),   S(-20, -25),   S(-15, -52),   S(-92, -37),   S(-30, -17),   S(-13,  -4),   S(-45,  -9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 38,   2),   S( 38,  18),   S( 55,  25),   S( 53,  26),   S( 32,  36),   S( 32,  23),   S( 21,  18),   S( 66, -10),
            S( -3,   7),   S( 26,  24),   S( 20,  18),   S( 25,  47),   S( 34,  15),   S( 16,  19),   S( 42,   6),   S( 20,   5),
            S( -7,  13),   S( 26,  32),   S( 62,  40),   S( 54,  38),   S( 67,  37),   S( 64,  20),   S( 31,  33),   S( 27,  16),
            S( 54,  78),   S(124,  50),   S(136,  89),   S(165,  74),   S(155,  72),   S( 93,  86),   S(108,  29),   S( 94,  14),
            S( 63, 101),   S(184,  95),   S(219, 151),   S(182, 107),   S(204, 145),   S(161, 125),   S(201,  73),   S( -5,  85),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S( -3,   8),   S( -7,  52),   S( 33, 107),   S( 87, 230),

            /* enemy king outside passed pawn square */
            S( -2, 234),

            /* passed pawn/friendly king distance penalty */
            S( -1, -22),

            /* passed pawn/enemy king distance bonus */
            S(  1,  36),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 75, -47),   S( 48,  11),   S( 45,  24),   S( 62,  42),   S( 57,   9),   S(182, -14),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 14,  -2),   S( 16,  66),   S(  7,  60),   S(  8,  87),   S( 46,  84),   S(167, 107),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-42,  -1),   S( -9, -43),   S(  3, -43),   S(-25, -24),   S( 31, -58),   S(273, -136),  S(  0,   0),    // blocked by rooks
            S(  0,   0),   S(-10,  -1),   S( 33, -16),   S( -3,  27),   S( 11, -48),   S( 17, -209),  S(-40, -207),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(  5,   1),   S( 95,   4),   S( 32, -50),   S(103, -30),   S(238, -16),   S(399,  75),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S(  2,  51),

            /* knight on outpost */
            S(  2,  34),

            /* bishop on outpost */
            S( 12,  33),

            /* bishop pair */
            S( 39, 110),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,   0),   S(  0,  -9),   S( -4,  -3),   S( -7, -48),   S(-16, -14),   S(-29,   6),   S(-28,  -3),   S(  1,  -7),
            S( -5,  -9),   S( -8, -11),   S(-15, -12),   S( -9, -14),   S(-12, -21),   S(-16,  -8),   S(-16,  -8),   S( -4,  -7),
            S( -6, -10),   S(  5, -30),   S( -2, -44),   S( -3, -57),   S(-14, -40),   S(-13, -31),   S(-14, -19),   S(  0,  -8),
            S(  6, -29),   S( 10, -43),   S( -8, -30),   S( -4, -47),   S( -8, -39),   S( -8, -29),   S(  0, -33),   S( -5, -20),
            S( 14, -19),   S(  8, -49),   S( 10, -60),   S( 10, -63),   S( 24, -69),   S( 12, -61),   S(  7, -74),   S( -2, -10),
            S( 72, -41),   S( 93, -102),  S( 87, -116),  S( 78, -138),  S( 95, -162),  S(130, -130),  S(120, -152),  S(114, -127),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 41,   1),

            /* rook on half-open file */
            S( 12,  39),

            /* rook on seventh rank */
            S(  9,  47),

            /* doubled rooks on file */
            S( 26,  22),

            /* queen on open file */
            S(-13,  38),

            /* queen on half-open file */
            S(  5,  39),

            /* pawn push threats */
            S(  0,   0),   S( 35,  35),   S( 39, -19),   S( 40,  26),   S( 37, -21),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 68,  80),   S( 49, 108),   S( 54,  94),   S( 44,  35),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-13,   6),   S( 51,  32),   S( 94,  -7),   S( 26,  17),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 34,  72),   S(  1,  30),   S( 47,  53),   S( 28, 105),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-11,  63),   S( 50,  49),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-29,  14),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats
        };
    }
}
