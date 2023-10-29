using System.Runtime.CompilerServices;
using System.Text;

using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class HceWeights
    {
        public static readonly Guid HCE_WEIGHTS_VERSION = new("da5e310e-b0dc-4c77-902c-5a46cc81bb73");

        #region Weight Offset Constants

        public const int MAX_WEIGHTS = 1990;
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

        #endregion

        private readonly Score[] weights;
        private static Score S(short mg, short eg) => new(mg, eg);

        public static readonly short[] CanonicalPieceValues = { 100, 300, 300, 500, 900, 0 };

        // Solution sample size: 12000000, generated on Sun, 29 Oct 2023 03:36:18 GMT
        // Solution K: 0.003850, error: 0.086101, accuracy: 0.4914
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(104, 149),   S(434, 473),   S(478, 508),   S(604, 870),   S(1404, 1530), S(  0,   0),

            /* piece square values */
            #region piece square values

            /* pawns: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-50,  12),   S(-53,  13),   S(-39,   1),   S(-28, -16),   S(-50,  26),   S( 31,  -4),   S( 44, -30),   S( -7, -34),
            S(-55,   7),   S(-48,  -2),   S(-52, -21),   S(-53, -15),   S(-40, -17),   S(-14, -19),   S( 14, -27),   S(-40, -18),
            S(-26,   8),   S(-34,   9),   S(-14, -25),   S(-26, -44),   S(-13, -29),   S( 17, -29),   S( -1, -12),   S( -8, -24),
            S(-19,  59),   S(-25,  30),   S(  0,  11),   S( 15,  -1),   S( 25, -20),   S( 22, -21),   S( 30,   0),   S( 57, -18),
            S( 38,  63),   S(-26,  64),   S(-15,  60),   S( 31,  57),   S(106,  68),   S(146,  12),   S(150,   0),   S(136,   9),
            S( 86,  83),   S( 72,  97),   S( 79,  86),   S(101,  48),   S( 70,  62),   S( 96, -10),   S( 64,  37),   S( 88, -13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-84,  26),   S(-91,  37),   S(-57,  12),   S(-70,  37),   S(-48,  -7),   S( 27, -30),   S( 42, -41),   S( -6, -20),
            S(-80,   3),   S(-85,   6),   S(-65, -20),   S(-64, -19),   S(-37, -41),   S(-25, -28),   S( 15, -39),   S(-34, -14),
            S(-58,  20),   S(-41,  12),   S(-32, -13),   S(-26, -39),   S(-22, -26),   S( -4, -24),   S( -7, -22),   S(  6, -22),
            S(-53,  62),   S(-11,   6),   S( 13,  -6),   S( 13,   6),   S(  4,  -9),   S(-24,  24),   S( 21,   9),   S( 55,  -1),
            S( 71,  26),   S( 64, -13),   S(101,  -6),   S( 86,  34),   S(118,  97),   S( 48,  90),   S( 40,  87),   S( 84,  83),
            S(106,   4),   S( 50,  37),   S(132, -18),   S( 85,  69),   S( 76,  94),   S( 84, 139),   S(130, 100),   S(141, 100),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-29,   9),   S( -1,  -1),   S(-18,  -5),   S(-40,  21),   S(-55,  15),   S(-17,  -1),   S(-27,  -1),   S(-63,  17),
            S(-24,   6),   S( -7, -12),   S(-45, -16),   S(-60, -21),   S(-43, -22),   S(-30, -25),   S(-40, -15),   S(-91,   5),
            S( -2,  11),   S( -3,  10),   S(-17,  -9),   S(-21, -36),   S(-32, -15),   S( -7, -27),   S(-20, -16),   S(-42,  -6),
            S(  1,  60),   S( -7,  52),   S( 12,  30),   S( 22,  16),   S(  8,  -6),   S(  0, -21),   S(  7, -11),   S(  8,   3),
            S( 45,  82),   S(-17, 105),   S( -3, 107),   S( 51,  72),   S( 97,  65),   S(107,  13),   S(105, -10),   S( 60,  35),
            S(144,  61),   S(199,  73),   S(115, 133),   S(125,  84),   S( 59,  62),   S( 60,   1),   S( 59,  20),   S( 20,  27),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: QQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-55,   5),   S(-11, -10),   S( 16, -26),   S(  0, -18),   S(-19, -22),   S(  6, -29),   S(  0, -35),   S(-28, -13),
            S(-51,  -5),   S(-22, -13),   S(-34, -25),   S(-40, -32),   S(-20, -43),   S(-31, -45),   S(-19, -43),   S(-63, -17),
            S(-18,   6),   S(-18,   3),   S(  8, -26),   S( -1, -47),   S(  0, -35),   S(  2, -47),   S( -1, -36),   S(-32, -23),
            S( 46,  12),   S( 63, -16),   S( 59, -23),   S( 55,  -5),   S( 19, -23),   S( -7, -20),   S( 15, -22),   S( 35, -15),
            S(204, -46),   S(157, -41),   S(115, -15),   S(122,  21),   S( 54,  59),   S( 38,  39),   S( 11,  24),   S( 85,  37),
            S(180, -23),   S( 81,  23),   S( 83, -22),   S( 51,  45),   S(109,  17),   S( 51,  34),   S( 91,  52),   S(117,  51),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: KK */
            S(-115, -33),  S(-15, -82),   S(-46, -12),   S(-33,   9),   S(-19,  -9),   S(-28,   1),   S(-19, -33),   S(-54, -47),
            S(-35, -49),   S(-47,  25),   S( -7,   2),   S(  9,   5),   S(  5,  10),   S( -5,  27),   S( -4,  13),   S( -9,   5),
            S(-21, -12),   S( -5,  24),   S(  0,  40),   S( 21,  54),   S( 27,  56),   S( 13,  29),   S(  5,  37),   S(  2,   9),
            S( -9,  32),   S( 14,  53),   S( 23,  82),   S( 23,  90),   S( 31,  95),   S( 43,  80),   S( 42,  76),   S( 19,  39),
            S(  8,  45),   S( 16,  53),   S( 38,  59),   S( 43,  82),   S( 14, 101),   S( 37, 105),   S( 15, 106),   S( 50,  51),
            S(-21,  57),   S(  7,  54),   S( 29,  67),   S( 38,  70),   S( 78,  50),   S(143,  42),   S( 38,  81),   S( -1,  65),
            S(-42,  51),   S(-33,  66),   S(-13,  69),   S( 36,  66),   S( 44,  73),   S(110,  12),   S(  2,   9),   S( 11,  21),
            S(-299,  52),  S(-45,  70),   S(-85,  82),   S( 33,  53),   S( 72,  39),   S(-74,  36),   S( 12,  26),   S(-237, -47),

            /* knights: KQ */
            S(-64, -140),  S(-36, -46),   S(-49,  -5),   S(-41,  -9),   S(-26, -12),   S(-36, -13),   S(-39, -23),   S(-32, -91),
            S(-55, -39),   S(-56,  18),   S( -1, -27),   S( 10, -17),   S( -2,  -8),   S( 12,   4),   S(-52,  10),   S(-39, -22),
            S(-26, -18),   S( 16, -27),   S( -6,  23),   S( 40,   9),   S( 19,  11),   S( 27, -13),   S( -6,  13),   S(-27,  28),
            S(  5,   2),   S( 63, -15),   S( 38,  22),   S( 53,  25),   S( 45,  37),   S( 45,  28),   S( 46,   4),   S( -1,   8),
            S( 59, -26),   S( 26,  -2),   S( 69,  15),   S( 76,  13),   S( 83,  15),   S( 47,  19),   S( 46,   8),   S( 19,   4),
            S( 44, -35),   S( 63, -52),   S(131, -50),   S(147, -35),   S( 80,   6),   S( 55,  13),   S(  8,  30),   S(  6,  18),
            S(  1, -14),   S(  6, -36),   S( 35, -29),   S( 84, -12),   S( 18,  16),   S( 42,  17),   S(-12,  27),   S(-30,  -9),
            S(-265, -73),  S(-18, -37),   S(  8, -21),   S(  6,  -9),   S(-12, -25),   S( 21,  26),   S(-31,   6),   S(-151, -53),

            /* knights: QK */
            S(-117, -87),  S(-68,  14),   S(-58,   8),   S(-85,  37),   S(-48,  -7),   S(-36, -27),   S(-50, -34),   S(-62, -86),
            S(-20, -49),   S(-10,  14),   S(-16,   1),   S( -8,   4),   S( -4,   1),   S(-11,  -1),   S(-59,   4),   S(-20, -58),
            S(-18,  26),   S( -6,  -5),   S( 31, -11),   S( 36,  24),   S( 43,  11),   S(-12,  21),   S( 12,  -8),   S(-28, -35),
            S( 16,  21),   S( 72,   4),   S( 60,  28),   S( 45,  44),   S( 44,  29),   S( 46,  35),   S( 46,   5),   S( -6,  12),
            S(  4,  25),   S( 45,  19),   S( 75,  17),   S(104,  13),   S( 58,  40),   S( 73,  31),   S( 36,  17),   S( 40, -18),
            S(-55,  36),   S( 32,  22),   S( 38,  40),   S( 56,  12),   S( 94,  -3),   S(146, -21),   S( 82,  -3),   S( 51, -17),
            S(-20,  26),   S(-49,  42),   S(  0,  33),   S( 45,  37),   S( 11,  55),   S( 41,  -2),   S( -5,  -5),   S( 30, -33),
            S(-139, -64),  S(-28,  35),   S(  7,  36),   S( 33,   4),   S( 14,  19),   S(-37, -28),   S(-15, -20),   S(-33, -47),

            /* knights: QQ */
            S( -6, -121),  S(-40, -23),   S(-29, -10),   S(-67,  -2),   S(-11,   2),   S(-43, -13),   S(-36, -59),   S(-57, -74),
            S(-36, -52),   S(-36,   9),   S(-11, -21),   S( 15, -33),   S( -4, -25),   S( 21, -39),   S(  4, -57),   S(-114, -75),
            S( -1, -17),   S( 17, -15),   S( 14, -13),   S( 27,   6),   S( 18,   1),   S(-12,   9),   S(-10,   2),   S(-18, -51),
            S( -7,  30),   S( 39,   9),   S( 78,   0),   S( 72,   4),   S( 37,   9),   S( 46,   5),   S( 37,   4),   S(-10,   0),
            S( 44,  -7),   S( 38,  -4),   S( 66,   1),   S( 61,   5),   S( 80,  -3),   S( 77,  -3),   S( 59, -18),   S(-13,  17),
            S( 51, -10),   S(  4,  15),   S( 74,  -7),   S(105, -23),   S( 73, -10),   S( 61,  12),   S( 40,   3),   S( 27,   3),
            S( -6,  19),   S(  0,   8),   S(-17,  -9),   S( 39,  30),   S( 16,  22),   S( -1,  15),   S(-25,  27),   S(-16,   7),
            S(-157, -92),  S(-14,  58),   S( -1, -24),   S( -7,  20),   S(-24, -14),   S(  5,   8),   S(-15,  26),   S(-56, -45),

            /* bishops: KK */
            S(  6, -10),   S( 20, -24),   S(  2, -13),   S(-22,  34),   S( -6,  25),   S(-11,  18),   S(-11,  -2),   S( 15, -60),
            S( 19,  -4),   S( 17,  14),   S( 17,  13),   S( 10,  35),   S( 19,  20),   S(  8,  25),   S( 40,  -5),   S( 21, -46),
            S(  1,  29),   S( 19,  37),   S( 26,  62),   S( 24,  44),   S( 16,  69),   S( 24,  37),   S( 18,  22),   S(  8,  16),
            S(  1,  31),   S(  3,  60),   S( 21,  65),   S( 46,  55),   S( 36,  46),   S( 16,  53),   S( 15,  40),   S( 29,  -9),
            S( -3,  54),   S( 25,  26),   S( 11,  49),   S( 47,  43),   S( 29,  58),   S( 44,  41),   S( 25,  37),   S( 13,  48),
            S( 12,  30),   S( 16,  53),   S(-11,  56),   S( -2,  57),   S(  2,  62),   S( 50,  73),   S( 55,  42),   S(  6,  72),
            S(-39,  69),   S(-20,  62),   S( -7,  61),   S(-23,  62),   S(-52,  81),   S(-19,  57),   S(-58,  56),   S(-36,  38),
            S(-106,  99),  S(-74,  85),   S(-63,  87),   S(-86,  99),   S(-34,  72),   S(-104,  79),  S( -9,  36),   S(-75,  55),

            /* bishops: KQ */
            S(-46,   7),   S( -9,   6),   S(-17,   2),   S(-25,   5),   S(-22,   5),   S( -8, -20),   S( 53, -84),   S(  9, -66),
            S(-34,  -9),   S(  4,   3),   S(  7,   4),   S(  9,  -2),   S( 29, -14),   S( 43, -29),   S( 80, -46),   S( 15, -64),
            S( -2,   6),   S(  3,  10),   S( 32,  11),   S( 43,  -1),   S( 30,   9),   S( 59,  -2),   S( 41, -13),   S( 56, -40),
            S( 32,  14),   S(  6,  25),   S( 19,  16),   S( 64,  -7),   S( 81,  -3),   S( 40,  21),   S( 41,  10),   S( -2,  19),
            S(  4,  21),   S( 58,  -9),   S( 64,  -6),   S(116, -44),   S( 87,  -4),   S( 36,  19),   S( 35,   4),   S( 19,  -1),
            S( 11,  -7),   S( 69, -13),   S( 89,  -6),   S(  4,  27),   S( 25,  18),   S( 29,  22),   S( 24,  28),   S(-10,  27),
            S(-65, -10),   S( 19,  -1),   S( 12,  11),   S(-16,  24),   S(  3,  25),   S(-12,  33),   S(-40,  52),   S(-63,  60),
            S(-17,   7),   S(-25,  -6),   S(  9,  -5),   S( -5,  32),   S(-39,  34),   S(-55,  43),   S(-38,  43),   S( -7,  15),

            /* bishops: QK */
            S( 69, -71),   S(-12, -29),   S( -6,  14),   S(-36,  19),   S(  3, -10),   S(-12, -10),   S(-44,  11),   S(-119,  24),
            S( 22, -32),   S( 66, -25),   S( 26,  -5),   S( 23,   7),   S(  2,   6),   S( -7,   6),   S(-28,   8),   S(-14, -18),
            S( 61, -22),   S( 42,   3),   S( 46,  13),   S( 24,  21),   S( 18,  36),   S( 12,   9),   S( 16,  -9),   S(-19,  -5),
            S( -6,  26),   S( 38,  28),   S( 33,  32),   S( 78,   3),   S( 62,   2),   S( 17,  29),   S(-28,  30),   S(  2,  11),
            S( 32,  23),   S( 22,  18),   S( 33,  25),   S( 72,   6),   S( 48,   9),   S( 45,  19),   S( 40,  -1),   S(-34,  43),
            S(-49,  49),   S( 22,  48),   S( 21,  23),   S( 18,  38),   S( 37,  34),   S( 69,  19),   S( 56,  -2),   S( 16,  23),
            S(-38,  65),   S(-15,  43),   S(  3,  38),   S(-25,  52),   S(-70,  58),   S( -5,  32),   S( -2,  11),   S(-79,  16),
            S(-90,  50),   S(-69,  64),   S(-54,  45),   S(-29,  61),   S( 23,  14),   S(-30,  18),   S(-18,   0),   S(-59,  -1),

            /* bishops: QQ */
            S(-88, -18),   S(-41, -17),   S( -7, -33),   S(-82,   6),   S(-18, -17),   S( -8, -39),   S( 40, -68),   S( 12, -70),
            S( 13, -42),   S( 46, -42),   S( 18, -27),   S( 24, -24),   S( -7, -12),   S( 52, -48),   S( 12, -37),   S( 46, -81),
            S( 11, -10),   S( 40, -29),   S( 36, -18),   S( 18, -18),   S( 32, -14),   S( 39, -19),   S( 38, -45),   S( 13, -30),
            S( 19,   5),   S( 32, -14),   S( 35, -23),   S( 78, -34),   S( 51, -23),   S( 59, -21),   S( 23, -24),   S(  6, -27),
            S( 16,   1),   S( 54, -21),   S( 23, -24),   S( 85, -44),   S( 86, -46),   S( 30, -25),   S( 41, -29),   S(-26,   0),
            S(-26, -11),   S( 83, -15),   S( 69, -27),   S( 81, -23),   S( 43,  -2),   S( -3,  -9),   S(-14,  -7),   S(-16,  -6),
            S(-59, -26),   S( -4,  -8),   S( 39,  -8),   S( -9,  -1),   S( 26, -15),   S(-42,   4),   S(-23,   7),   S(-36,   4),
            S(-13, -16),   S( -3, -14),   S( -5,  -5),   S( 18,   3),   S( 21,  13),   S(-30,   8),   S(-27,  14),   S(-27,  19),

            /* rooks: KK */
            S(-31,  90),   S(-22,  76),   S(-21,  66),   S(-17,  45),   S(-16,  27),   S(-32,  52),   S( -8,  48),   S(-32,  36),
            S(-45,  89),   S(-38,  89),   S(-29,  68),   S(-19,  41),   S(-22,  37),   S( -7,  14),   S( 19,  26),   S(-19,  37),
            S(-46,  89),   S(-37,  89),   S(-33,  77),   S(-24,  49),   S( -8,  32),   S( -6,  35),   S( 18,  37),   S( -4,  25),
            S(-42, 112),   S(-25, 108),   S(-17,  89),   S(-12,  72),   S(-20,  60),   S(-21,  84),   S( 29,  72),   S(-20,  66),
            S(-20, 132),   S( -4, 121),   S(  5,  99),   S( 19,  76),   S( -2,  91),   S( 39,  73),   S( 68,  82),   S( 11,  86),
            S(  1, 131),   S( 31, 110),   S( 27,  98),   S( 23,  86),   S( 61,  63),   S(116,  55),   S(108,  69),   S( 62,  79),
            S(-13, 116),   S( -7, 112),   S( 18,  80),   S( 42,  56),   S( 33,  61),   S(116,  43),   S( 95,  77),   S(114,  60),
            S( 77,  92),   S( 78, 101),   S( 56,  90),   S( 72,  65),   S( 39,  92),   S( 99,  88),   S(131,  90),   S(143,  68),

            /* rooks: KQ */
            S(-46,  10),   S( -9, -28),   S(-24, -25),   S(-37, -13),   S(-48,  -6),   S(-57,   4),   S(-52,  20),   S(-72,  32),
            S(-64,   1),   S( -5, -38),   S(-40, -23),   S(-32, -22),   S(-36, -20),   S(-39,  -3),   S(-48,  10),   S(-70,   3),
            S(-63,   7),   S(-30, -22),   S(-52, -10),   S(-18, -31),   S(-53, -16),   S(-46,  -5),   S(-27,   8),   S(-44,   1),
            S(-56,  20),   S(-12,   0),   S(-44,  14),   S(-15,   2),   S(-19,   3),   S(-45,  28),   S( -1,  31),   S(-40,  24),
            S(-24,  32),   S( 15,  11),   S( -7,  11),   S(  6,   1),   S( 18,  -3),   S( 29,   2),   S( 43,  17),   S( -1,  32),
            S( 39,  20),   S( 90,  -6),   S( 46,  -4),   S(105, -38),   S( 48,  -9),   S( 81,  -5),   S(104,   3),   S( 65,  19),
            S( -3,  26),   S( 29,   6),   S( 81, -25),   S( 69, -25),   S( 69, -26),   S( 41, -11),   S( 65,  12),   S( 51,  12),
            S( 40,  41),   S( 51,  34),   S( 94,  -8),   S( 87,  -9),   S( 87, -11),   S(126, -12),   S( 98,  22),   S(112,  -2),

            /* rooks: QK */
            S(-84,  42),   S(-43,  29),   S(-56,  20),   S(-49, -12),   S(-25, -27),   S(-26, -20),   S(-10, -27),   S(-40,  -1),
            S(-51,  20),   S(-58,  22),   S(-54,  15),   S(-43,  -9),   S(-62, -12),   S(-40, -32),   S(-21, -32),   S(-44, -25),
            S(-43,  18),   S(-22,  19),   S(-52,  23),   S(-50,  -9),   S(-42,  -5),   S(-53,  -4),   S(-12, -30),   S(-38, -19),
            S(-20,  30),   S(  7,  16),   S(-26,  23),   S(-23,  10),   S(-40,   7),   S(-50,  18),   S(-20,   9),   S(-35,   7),
            S( 49,  20),   S( 42,  31),   S( 23,  25),   S(-14,  28),   S( -3,  15),   S( -9,  26),   S( 24,  14),   S( 17,  -4),
            S( 54,  37),   S(106,  19),   S( 76,  12),   S( -3,  36),   S( 49,   2),   S( 75,   6),   S(106,   1),   S( 34,  26),
            S( 49,  13),   S( 74,   6),   S( 68,  -6),   S( -5,  17),   S(-10,  14),   S( 88, -24),   S(  4,  18),   S( 23,  22),
            S(246, -94),   S(165, -18),   S( 74,  19),   S( 34,  16),   S( 20,  25),   S( 70,   8),   S( 50,  30),   S( 67,  21),

            /* rooks: QQ */
            S(-52, -26),   S(-22, -43),   S(-27, -51),   S(-16, -68),   S( -2, -68),   S(-10, -29),   S(-15, -18),   S(-33,   3),
            S(-16, -57),   S(-35, -30),   S(-56, -31),   S( -2, -78),   S(-16, -68),   S(-16, -61),   S(-19, -38),   S(-44,  -5),
            S(-75, -13),   S( -7, -41),   S(  6, -60),   S(-14, -46),   S(-45, -31),   S(-44, -17),   S(-52,   1),   S(-51,   5),
            S(-53, -11),   S( 11, -21),   S(-41,  -7),   S( -8, -35),   S(-31, -30),   S(-21,  -8),   S(-36,  12),   S(-35,  16),
            S( 37, -17),   S( 72, -24),   S( 24, -32),   S( 74, -38),   S( 67, -43),   S( 67, -25),   S( 35,   6),   S( -8,  13),
            S( 83, -17),   S( 73, -25),   S(105, -39),   S( 68, -25),   S( 59, -28),   S( 65, -21),   S( 77,  -9),   S( 36,   8),
            S( 41, -10),   S( 82, -21),   S( 40, -25),   S( 52, -36),   S(114, -77),   S( 93, -61),   S( 39,  -9),   S( 21,   8),
            S( 19,  27),   S( -2,  44),   S(  6,  12),   S(  2,  15),   S( 54, -23),   S( 60, -13),   S( 68,  15),   S( 59,  18),

            /* queens: KK */
            S( 23,  49),   S( 32,   9),   S( 37, -15),   S( 41, -18),   S( 50, -78),   S( 18, -99),   S(-29, -73),   S( -7,   8),
            S( 15,  71),   S( 37,  43),   S( 37,  30),   S( 46,  10),   S( 44,   2),   S( 50, -41),   S( 71, -90),   S( 27, -28),
            S( 13,  58),   S( 28,  52),   S( 33,  57),   S( 19,  62),   S( 28,  46),   S( 26,  61),   S( 35,  56),   S( 35,  16),
            S( 20,  76),   S(  8,  89),   S( 12,  88),   S( 13,  89),   S(  2,  87),   S( 11, 100),   S( 28,  95),   S(  9, 117),
            S( 17,  97),   S( 16, 103),   S( -7, 117),   S( -8, 123),   S( -8, 146),   S( -3, 176),   S(  5, 194),   S( 16, 167),
            S(  1, 106),   S( 14, 130),   S(-22, 146),   S(-38, 165),   S(  2, 170),   S( 52, 174),   S( 23, 214),   S(  4, 240),
            S(-32, 169),   S(-47, 196),   S(-25, 175),   S(-28, 175),   S(-80, 239),   S( 40, 161),   S( 19, 252),   S( 85, 189),
            S(-10, 138),   S( 21, 126),   S( 38, 116),   S( 26, 126),   S( 34, 139),   S(107, 135),   S(158, 109),   S(135, 140),

            /* queens: KQ */
            S( 27, -23),   S( 25, -63),   S( 12, -88),   S( 23, -109),  S( 19, -70),   S(-13, -91),   S(-57, -51),   S(-71, -23),
            S( 21,   7),   S( 48, -61),   S( 50, -103),  S( 16, -52),   S( 34, -66),   S(  9, -40),   S(  0, -86),   S(-50, -33),
            S(  9,   8),   S( 21, -43),   S( 39, -83),   S( 29, -56),   S( 16, -36),   S( 32, -21),   S( -9,  -2),   S(-21,   1),
            S( 10,  14),   S( 38, -40),   S(-11,   2),   S(  6, -23),   S( 18,  -6),   S( -2,  31),   S(-11,  46),   S(-11,  25),
            S( -3,  35),   S( 46, -18),   S(  0,  36),   S( 19,  -6),   S( 41,   0),   S(-38,  63),   S( -5,  67),   S( -6,  66),
            S( 27,  23),   S(102,   3),   S( 55,  29),   S( 77, -11),   S( 21,  25),   S( 21,  33),   S( 13,  53),   S(-16,  70),
            S( 68,  39),   S( 57,  19),   S( 36,  13),   S( 51,  52),   S( 34,  13),   S(-31,  93),   S(  6,  77),   S( -5,  54),
            S(101,  11),   S(108,  22),   S( 88,  49),   S(100,  14),   S( 20,  49),   S( 65,  15),   S( 68,  10),   S(  8,  13),

            /* queens: QK */
            S(-46, -77),   S(-93, -66),   S(-26, -118),  S( -9, -86),   S( 23, -141),  S(-15, -80),   S(-23, -53),   S(-49,   6),
            S(-54, -61),   S(-34, -72),   S( 26, -114),  S( 19, -70),   S( 22, -81),   S( 18, -76),   S( 23, -58),   S( 14, -29),
            S(-37, -16),   S(-10, -14),   S(  1, -17),   S( -8,  -4),   S( -3, -26),   S( -7, -32),   S( 21, -43),   S(  0,  -6),
            S( -8, -12),   S(-20,  28),   S(-30,  45),   S( -4,   8),   S(-18,  19),   S( -5,   0),   S( 11, -18),   S( 11,  -9),
            S(-12,  13),   S(-17,  38),   S(-24,  63),   S(-45,  51),   S( -8,  29),   S( -3,  26),   S(  4,  17),   S( -9,   0),
            S(-13,  21),   S( 15,  42),   S(-26,  76),   S(-38,  65),   S( 16,  42),   S( 45,  30),   S( 13,  36),   S(  9,  47),
            S(-26,  47),   S(-51,  70),   S(-34,  82),   S(-54,  90),   S(-36,  71),   S(-19,  45),   S( 14,  54),   S( -7,  87),
            S(-22,  -1),   S( 24,  41),   S( 28,  28),   S( 20,  35),   S( 32,  23),   S( 17,  43),   S( 63,  26),   S( 77,  40),

            /* queens: QQ */
            S(-54, -62),   S(-89, -59),   S(-58, -65),   S(-40, -54),   S( -2, -75),   S(-23, -58),   S(-50, -39),   S(-24, -32),
            S(-99, -52),   S(-47, -67),   S( 16, -109),  S( 17, -94),   S( -1, -67),   S(-12, -80),   S(-22, -11),   S(-54, -18),
            S(-20,   6),   S(-12,  11),   S( -3, -54),   S(-18, -14),   S(-29,  -5),   S( -9, -21),   S(  1,   5),   S(-18,  -3),
            S(-12, -30),   S( 19, -56),   S(-10, -37),   S(-29, -17),   S( 13, -53),   S( -9, -41),   S(-28, -31),   S(-35,  35),
            S(-24,   0),   S( -5,  32),   S(  7, -17),   S(  3, -21),   S( 18, -11),   S( -5, -39),   S(  6,  -1),   S(  5, -27),
            S( 13,  20),   S( 14,  50),   S( 86,  21),   S(  9,  26),   S(-21, -11),   S( 10,  -5),   S( 19,  -4),   S( 16,   3),
            S(100,  36),   S( 26,  15),   S( 27,  34),   S( 20,  31),   S(  6,  26),   S(  2,   4),   S(  3,  30),   S(-17,  23),
            S( 72, -21),   S( 35,  23),   S( 22,  21),   S( 12,   0),   S( 14,   9),   S( 23, -14),   S( 37, -15),   S( 35,  -4),

            /* kings: KK */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-88,  -1),   S(-94,  17),   S(-40,  -3),   S(-72, -30),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-26,   5),   S(-26,  10),   S( -6,  -5),   S(-63,  -3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -4,  24),   S(-41,  24),   S(-33,   9),   S(-110,  10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 93,  13),   S( 35,  26),   S(-14,  25),   S(-119,  21),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(232, -13),   S(252, -29),   S(137,   2),   S(-14,   7),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(226, -19),   S(270, -25),   S(276, -12),   S(161, -12),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(192, -15),   S(141, -12),   S(161,  33),   S( 41,   2),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 61,   6),   S( 38,  33),   S( 44,  25),   S(-14, -170),

            /* kings: KQ */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-119,  11),  S(-131,  15),  S(-63, -11),   S(-94, -14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-34,   0),   S(-40,   1),   S(-42,   6),   S(-89,   1),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-23,  18),   S(-13,   6),   S(-40,  10),   S(-128,  15),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(106,   7),   S( 65,  19),   S(  8,  32),   S(-97,  33),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(202,   1),   S(149,  20),   S( 80,  37),   S(-54,  47),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(156,  12),   S(108,  36),   S( 93,  38),   S( -6,  46),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(119,   3),   S(175,   8),   S(122,  19),   S( 19,   9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 76,  -7),   S( 48,  -5),   S( 66,  -1),   S( 10, -26),

            /* kings: QK */
            S(-75, -76),   S(-38, -66),   S(-73, -40),   S(-128, -15),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-66, -45),   S(-38, -26),   S(-48, -29),   S(-64, -21),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-145,   0),  S(-50, -11),   S(-48,  -8),   S(-66,   1),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-109,  20),  S(-30,  14),   S( 43,  -5),   S( 49,  -3),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-47,  10),   S( 25,  20),   S(105,   1),   S(160, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-52,  25),   S(107,  12),   S(130,   5),   S(165, -10),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-12,  -2),   S(154,  -5),   S(105,   6),   S(132,  -7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-38, -29),   S( 32,  16),   S(110, -13),   S( 41,  -5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: QQ */
            S(-178,  39),  S(-96,  -9),   S(-139,  10),  S(-156,  -5),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-144,   7),  S(-80,  -6),   S(-70, -12),   S(-107,  -5),  S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-145,   2),  S(-83,  -6),   S(-41,  -7),   S(-71,   6),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-47, -15),   S( 13,   8),   S( 64,   2),   S( 98,  -7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-17,   3),   S(142,  -5),   S(177,  -4),   S(224, -24),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 71,   7),   S(146,  13),   S(149, -10),   S(133,   6),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  7,   6),   S( 48,  48),   S( 48,  22),   S(128,   3),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-42, -127),  S( 14,  64),   S( 46,  16),   S( 30,   5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            #endregion

            /* mobility weights */
            S(  8,   5),    // knights
            S(  5,   4),    // bishops
            S(  2,   4),    // rooks
            S(  1,   4),    // queens

            /* center control */
            S(  2,   6),    // D0
            S(  2,   5),    // D1

            /* squares attacked near enemy king */
            S( 21,  -4),    // attacks to squares 1 from king
            S( 18,  -1),    // attacks to squares 2 from king
            S(  6,   1),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S( 16,  11),    // friendly pawns 1 from king
            S(  9,  14),    // friendly pawns 2 from king
            S(  4,  11),    // friendly pawns 3 from king

            /* castling right available */
            S( 38, -24),

            /* castling complete */
            S( 13, -15),

            /* king on open file */
            S(-57,  13),

            /* king on half-open file */
            S(-26,  22),

            /* king on open diagonal */
            S( -4,   8),

            /* king attack square open */
            S(-12,  -1),

            /* isolated pawns */
            S( -4, -10),

            /* doubled pawns */
            S(-12, -29),

            /* backward pawns */
            S(  0,   0),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 23,  -5),   S(  1,  -2),   S( 11,   1),   S( 17,  11),   S( 26,  33),   S(  4, -32),   S(-21,  48),   S(  8, -43),
            S(  5,  -2),   S( 30,   0),   S(  9,  25),   S( 19,  41),   S( 43,  -1),   S(  4,  25),   S( 15,  -1),   S( 13, -11),
            S( -9,  21),   S( 18,   6),   S(  3,  54),   S( 23,  73),   S( 35,  29),   S( 30,  34),   S( 29,   8),   S( 10,  21),
            S( 25,  43),   S( 25,  46),   S( 38,  83),   S( 15,  84),   S( 80,  60),   S( 76,  43),   S( 20,  48),   S( 23,  51),
            S( 96,  81),   S(128,  93),   S( 89, 156),   S(141, 153),   S(165, 119),   S(149, 124),   S(152,  77),   S( 82,  56),
            S( 94, 225),   S(136, 314),   S(114, 240),   S(105, 208),   S( 75, 177),   S( 65, 170),   S( 54, 200),   S( 27, 135),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,  20),   S(-16,  27),   S(-30,  33),   S(-52,  73),   S( 17,  -3),   S(-26,  21),   S(-14,  58),   S( 14,  16),
            S(  9,  28),   S( -3,  41),   S(-13,  34),   S( -8,  30),   S(-21,  39),   S(-53,  51),   S(-55,  76),   S( 17,  26),
            S(  9,  18),   S(  6,  27),   S( -4,  29),   S( 13,  32),   S( -3,  26),   S(-36,  41),   S(-57,  74),   S(-23,  47),
            S( 42,  28),   S( 67,  41),   S( 34,  36),   S( 16,  31),   S( 21,  55),   S( 56,  46),   S( 28,  63),   S(-16,  71),
            S( 62,  65),   S(108,  96),   S( 99,  67),   S( 47,  49),   S(-20,  35),   S( 85,  45),   S( 56,  89),   S( 16,  53),
            S(248,  69),   S(228, 108),   S(223, 118),   S(216, 116),   S(204, 125),   S(208, 120),   S(254, 123),   S(261,  99),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 39,   9),   S(  6,  18),   S( 16,  25),   S( 14,  50),   S( 77,  36),   S( 28,  18),   S( 16,   3),   S( 43,   9),
            S(  6,  14),   S(  4,  10),   S( 22,  12),   S( 21,  27),   S( 16,  13),   S( -2,   6),   S( 10,   3),   S( 30,  -6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -6, -14),   S( -4, -10),   S(-22, -12),   S(-21, -27),   S(-16, -13),   S(  2,  -6),   S(-10,  -3),   S(-30,   6),
            S(-39,  -9),   S( -6, -18),   S(-16, -25),   S(-14, -50),   S(-77, -36),   S(-28, -18),   S(-16,  -3),   S(-43,  -9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 33,   3),   S( 33,  16),   S( 48,  22),   S( 46,  23),   S( 30,  33),   S( 28,  20),   S( 12,  16),   S( 55,  -9),
            S( -3,   7),   S( 22,  22),   S( 18,  16),   S( 22,  41),   S( 29,  14),   S( 12,  17),   S( 33,   5),   S( 13,   6),
            S( -7,  13),   S( 23,  29),   S( 54,  36),   S( 48,  33),   S( 58,  33),   S( 56,  18),   S( 27,  29),   S( 25,  13),
            S( 46,  68),   S(110,  43),   S(121,  77),   S(145,  64),   S(137,  62),   S( 84,  75),   S( 96,  25),   S( 85,  11),
            S( 54,  89),   S(180,  77),   S(216, 124),   S(180,  85),   S(199, 119),   S(161, 103),   S(205,  54),   S(-10,  75),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S( -2,   7),   S( -6,  45),   S( 27,  92),   S( 53, 207),

            /* enemy king outside passed pawn square */
            S(-22, 198),

            /* passed pawn/friendly king distance penalty */
            S( -2, -19),

            /* passed pawn/enemy king distance bonus */
            S(  2,  30),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 71, -47),   S( 43,   8),   S( 39,  21),   S( 53,  36),   S( 48,  10),   S(183, -18),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 12,  -3),   S( 14,  55),   S(  8,  50),   S(  8,  75),   S( 40,  75),   S(162,  87),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-39,   1),   S(-10, -35),   S(  1, -36),   S(-25, -18),   S( 26, -48),   S(251, -123),  S(  0,   0),    // blocked by rooks
            S(  0,   0),   S( -7,   0),   S( 35, -22),   S( -1,  20),   S( 10, -45),   S( 17, -189),  S( -6, -204),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(  3,   1),   S( 82,   1),   S( 11, -39),   S( 76, -23),   S(201, -11),   S(353,  56),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S(  3,  44),

            /* knight on outpost */
            S(  1,  30),

            /* bishop on outpost */
            S( 10,  30),

            /* bishop pair */
            S( 36,  80),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,  -2),   S( -1,  -6),   S( -3,  -5),   S( -2, -48),   S( -9, -17),   S(-24,   4),   S(-22,  -6),   S(  1,  -5),
            S( -5,  -8),   S( -8,  -8),   S(-14, -11),   S( -8, -13),   S(-12, -17),   S(-15,  -6),   S(-14,  -7),   S( -4,  -5),
            S( -5,  -9),   S(  5, -26),   S( -2, -38),   S( -3, -49),   S(-13, -34),   S(-11, -27),   S(-13, -15),   S( -1,  -7),
            S(  5, -25),   S(  9, -36),   S( -7, -24),   S( -5, -39),   S( -7, -33),   S( -9, -23),   S( -2, -27),   S( -6, -16),
            S( 13, -16),   S(  8, -41),   S(  9, -51),   S(  8, -51),   S( 20, -57),   S(  9, -50),   S(  6, -64),   S( -2,  -7),
            S( 58, -33),   S( 84, -88),   S( 75, -98),   S( 68, -117),  S( 87, -140),  S(123, -113),  S(107, -130),  S(101, -109),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 39,  -1),

            /* rook on half-open file */
            S( 12,  33),

            /* rook on seventh rank */
            S(  4,  44),

            /* doubled rooks on file */
            S( 16,  27),

            /* queen on open file */
            S(-11,  34),

            /* queen on half-open file */
            S(  4,  36),

            /* pawn push threats */
            S(  0,   0),   S( 30,  30),   S( 33, -15),   S( 35,  22),   S( 33, -18),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 58,  69),   S( 42,  94),   S( 47,  79),   S( 37,  33),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-12,   7),   S( 45,  29),   S( 81,  -6),   S( 23,  12),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 30,  63),   S(  2,  26),   S( 41,  46),   S( 23,  94),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -9,  53),   S( 41,  47),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-25,  12),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats
        };
    }
}
