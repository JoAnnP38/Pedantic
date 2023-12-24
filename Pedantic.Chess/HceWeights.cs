using System.Runtime.CompilerServices;
using System.Text;

using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class HceWeights
    {
        public static readonly Guid HCE_WEIGHTS_VERSION = new("3e13519c-760d-4f0d-9648-8441428b3325");

        #region Weight Offset Constants

        public const int MAX_WEIGHTS = 12747;
        public const int PIECE_VALUES = 0;
        public const int FRIENDLY_PIECE_SQUARE_TABLE = 6;
        public const int ENEMY_PIECE_SQUARE_TABLE = 6150;
        public const int PIECE_MOBILITY = 12294;
        public const int TRAPPED_PIECE = 12298;
        public const int CENTER_CONTROL = 12302;

        /* king safety/evaluation */
        public const int KING_ATTACK = 12304;
        public const int PAWN_SHIELD = 12307;
        public const int CASTLING_AVAILABLE = 12310;
        public const int CASTLING_COMPLETE = 12311;
        public const int KING_ON_OPEN_FILE = 12312;
        public const int KING_ON_HALF_OPEN_FILE = 12313;
        public const int KING_ON_OPEN_DIAGONAL = 12314;
        public const int KING_ATTACK_SQUARE_OPEN = 12315;

        /* cached pawn positions */
        public const int ISOLATED_PAWN = 12316;
        public const int DOUBLED_PAWN = 12317;
        public const int BACKWARD_PAWN = 12318;
        public const int PHALANX_PAWN = 12319;
        public const int PASSED_PAWN = 12383;
        public const int PAWN_RAM = 12447;
        public const int CHAINED_PAWN = 12511;

        /* non-cached pawn positions */
        public const int PP_CAN_ADVANCE = 12575;
        public const int KING_OUTSIDE_PP_SQUARE = 12579;
        public const int PP_FRIENDLY_KING_DISTANCE = 12580;
        public const int PP_ENEMY_KING_DISTANCE = 12581;
        public const int BLOCK_PASSED_PAWN = 12582;
        public const int ROOK_BEHIND_PASSED_PAWN = 12630;

        /* piece evaluation */
        public const int KNIGHT_OUTPOST = 12631;
        public const int BISHOP_OUTPOST = 12632;
        public const int BISHOP_PAIR = 12633;
        public const int BAD_BISHOP_PAWN = 12634;
        public const int ROOK_ON_OPEN_FILE = 12698;
        public const int ROOK_ON_HALF_OPEN_FILE = 12699;
        public const int ROOK_ON_7TH_RANK = 12700;
        public const int DOUBLED_ROOKS_ON_FILE = 12701;
        public const int QUEEN_ON_OPEN_FILE = 12702;
        public const int QUEEN_ON_HALF_OPEN_FILE = 12703;

        /* threats */
        public const int PAWN_PUSH_THREAT = 12704;
        public const int PIECE_THREAT = 12710;

        /* misc */
        public const int TEMPO_BONUS = 12746;

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
        public Score FriendlyPieceSquareValue(Piece piece, KingPlacement kp, int square)
        {
            int offset = ((int)piece * Constants.MAX_KING_BUCKETS + kp.Friendly) * Constants.MAX_SQUARES + square;
            return weights[FRIENDLY_PIECE_SQUARE_TABLE + offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Score EnemyPieceSquareValue(Piece piece, KingPlacement kp, int square)
        {
            int offset = ((int)piece * Constants.MAX_KING_BUCKETS + kp.Enemy) * Constants.MAX_SQUARES + square;
            return weights[ENEMY_PIECE_SQUARE_TABLE + offset];
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

        // Solution sample size: 12000000, generated on Sat, 23 Dec 2023 18:20:38 GMT
        // Solution K: 0.003850, error: 0.083692, accuracy: 0.5051
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S( 99, 176),   S(426, 552),   S(443, 623),   S(528, 1036),  S(1338, 1852), S(  0,   0),

            /* friendly king piece square values */
            #region friendly king piece square values

            /* pawns: bucket 0 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 84, -107),  S(132, -81),   S( 19, -30),   S(-35, -13),   S(-40, -11),   S(-24,  -3),   S(-41,  -1),   S(-36, -27),
            S( 98, -98),   S( 83, -75),   S(  0, -54),   S(-14, -72),   S(-26, -31),   S(-25, -28),   S(-38, -14),   S(-41, -33),
            S( 91, -83),   S( 64, -42),   S( 15, -49),   S( 10, -62),   S( -3, -55),   S(  1, -41),   S(-10, -32),   S(-32, -29),
            S( 66, -27),   S( 35, -16),   S( 31, -32),   S( 25, -50),   S(  7, -36),   S(-23, -26),   S(-20, -23),   S(-37,  -2),
            S( 84,  51),   S( 13,  35),   S( 60,  11),   S( 74, -22),   S( 36,   8),   S(-38,  20),   S( -4,  -2),   S(-49,  70),
            S( 74,  58),   S( 55,  66),   S( 59, -19),   S(-10,   0),   S( 49, -17),   S( 30,   6),   S( 14,  25),   S(  7,  55),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 13, -25),   S( 14, -29),   S( 29, -24),   S(-23,  -1),   S(-17, -20),   S( 10, -24),   S(-24,   2),   S(-42,  22),
            S(  8, -20),   S(  2, -26),   S( -8, -27),   S(-22, -36),   S(-13, -36),   S( -7, -31),   S(-33,  -6),   S(-56,   8),
            S( 12, -20),   S(  5, -16),   S( 14, -37),   S(  9, -47),   S( -4, -32),   S(  7, -39),   S( -6, -15),   S(-30,   3),
            S( 24,   9),   S( 13, -14),   S( 31, -21),   S( 18, -17),   S( 10, -12),   S( 19, -23),   S(-37,   9),   S(-44,  34),
            S(-18,  76),   S(-32,  34),   S(-32,  38),   S(  3,  10),   S( 23,  33),   S( -3,  29),   S(-22,  31),   S(-30,  77),
            S( 57,  77),   S( 35,  55),   S(-32,  33),   S(-22,  35),   S(-23,  28),   S(-46,  39),   S( 12,  26),   S(-44, 109),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-30,  11),   S(-28,  10),   S( -9,  -7),   S(-31,   4),   S(-24,  -6),   S( 18, -20),   S(  9, -42),   S( -9, -25),
            S(-30,   6),   S(-46,   8),   S(-32, -23),   S(-27, -33),   S(-16, -27),   S( -7, -18),   S(-20, -21),   S(-30, -17),
            S(-31,   9),   S(-35,   2),   S( -4, -40),   S( -4, -44),   S( -7, -23),   S( 21, -32),   S(  6, -27),   S(-13, -17),
            S(-50,  39),   S(-29,  -2),   S(-20, -12),   S(  2, -22),   S(  5, -14),   S(  6,  -4),   S(-15,  12),   S(-27,  15),
            S(-25,  61),   S(-93,  50),   S(-62,  23),   S(-52,  36),   S(  0,  47),   S(-29,  67),   S(-23,  63),   S(-31, 101),
            S(-68,  97),   S(-110,  96),  S(-137,  58),  S(-30,  16),   S(-52,  66),   S(-39,  60),   S(-47,  59),   S(-55,  98),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-29,  -8),   S(-37,   1),   S(-29,  -2),   S( -5, -73),   S(-14, -22),   S( 42, -26),   S( 76, -58),   S( 49, -79),
            S(-35, -15),   S(-50,  -6),   S(-30, -28),   S(-25, -34),   S(-17, -38),   S(  5, -34),   S( 42, -47),   S( 44, -62),
            S(-34,  -9),   S(-20, -21),   S( -7, -36),   S( -9, -49),   S(  4, -55),   S( 31, -51),   S( 34, -41),   S( 46, -53),
            S(-38,  22),   S(-21, -19),   S( -8, -24),   S( 11, -42),   S( 32, -52),   S( 18, -31),   S( 13, -11),   S( 38,  -8),
            S( -2,  43),   S(-33,   8),   S( -6, -14),   S( 23,   1),   S( 54,  15),   S( 57,   2),   S( 29,  71),   S( 43,  88),
            S(-30, 113),   S(-49,  62),   S( -8,   7),   S(-30,   2),   S( 29,  -3),   S( 31,  50),   S( 25,  92),   S( 39,  83),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-123,  39),  S(-15,  -1),   S(-35,  13),   S( -8,  -5),   S( 10, -12),   S(-18,   6),   S(-45,   0),   S(-44,  -9),
            S(-45,  28),   S( 30,  -5),   S( 26, -28),   S(  3, -30),   S(  5, -34),   S(-51,  -8),   S( -4, -25),   S(-11, -29),
            S(  1, -11),   S( 48, -19),   S( 36, -19),   S(  0, -29),   S(-40, -23),   S( -9, -22),   S(-30,  -8),   S(-38,  -2),
            S(  4,   8),   S(  5,  20),   S( 73, -15),   S( 38, -10),   S(-15, -22),   S(-49,   0),   S( 28, -16),   S( 17,  -8),
            S(  0,  57),   S(-13,  42),   S( 44,   4),   S(  2,   9),   S( 29,   5),   S(  0,   2),   S(-31,   8),   S( 61,  20),
            S( 79,  59),   S( 59,  96),   S( 61,  20),   S( 57,  24),   S( 53,  19),   S( -4, -39),   S( 19,  15),   S( -4,  25),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-100,  58),  S(-48,  31),   S(-28,  22),   S(  4, -21),   S(-51,  28),   S(-27,   9),   S(-10,   3),   S(-39,  31),
            S(-56,  33),   S(-36,  18),   S( 26,  -8),   S( -8,   3),   S( 19, -15),   S(-22, -14),   S( -4,  -7),   S(-62,  28),
            S(-41,  34),   S(-12,  14),   S( 66, -32),   S( 17, -25),   S( 30, -21),   S(-15, -13),   S(  8,  -4),   S(-28,  23),
            S(-24,  47),   S( 25,  13),   S( 48,  -4),   S( 58,  -6),   S(  7,  -2),   S(-46,   8),   S( 23,  -3),   S( 19,  20),
            S( 83,  32),   S( 82,  10),   S( 41,  35),   S( 59,  20),   S(-17,  58),   S( 60,  -2),   S( 10,   5),   S( 32,  44),
            S(116,   9),   S(131, -14),   S( 73,   6),   S( 41,   8),   S( 53,  15),   S( 34,   9),   S( 26,  19),   S( 24,  56),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-47,  32),   S( -9,  10),   S( -7,   1),   S(-33,  15),   S(-30,   2),   S(-28,   6),   S(-58,  10),   S(-67,  20),
            S(-56,  27),   S( -2,  -7),   S(  2, -26),   S( -2, -22),   S( 67, -36),   S( 40, -29),   S(-34,   1),   S(-65,  14),
            S(-50,  30),   S(  5,   1),   S( 20, -24),   S(  7, -21),   S( 45, -26),   S( 68, -36),   S(  0, -12),   S(-33,   9),
            S(-41,  44),   S(-18,  13),   S( 35, -20),   S( 14, -11),   S( 54, -13),   S( 40,  -6),   S( 36,  -6),   S( 35,  -4),
            S(-10,  38),   S(-22,  17),   S(-48,  17),   S( 13,   5),   S( 90,  23),   S(132,   7),   S(102,  -3),   S( 76,  15),
            S( 74,   8),   S( 26,  -5),   S( 17, -34),   S( 62, -40),   S( 67,   8),   S( 68,   7),   S( 78,   1),   S(110,   9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-55, -16),   S(-47,  -6),   S(-17,  -7),   S(-33, -29),   S(-31, -20),   S( 25, -25),   S( -5, -30),   S(-70,  -7),
            S(-66, -20),   S(-48, -23),   S(-37, -25),   S(-10, -46),   S( 30, -55),   S( 62, -50),   S( 49, -41),   S(-41, -14),
            S(-67,  -1),   S(-57,  -7),   S(-29, -27),   S(-14, -32),   S( 13, -42),   S( 44, -37),   S( 51, -46),   S( 18, -32),
            S(-34,   2),   S(-64,  -1),   S(-56,  -9),   S(-24, -23),   S( 33, -34),   S( 25, -20),   S( 50, -24),   S( 57, -36),
            S( -4,   7),   S(-69,  -5),   S(-14, -29),   S( 19, -34),   S( 57,  -6),   S( 69,  -4),   S(108,   7),   S(140,   3),
            S( 25,  -3),   S(-15, -51),   S( 30, -62),   S( 36, -60),   S( 31, -51),   S( 45, -21),   S( 52,  60),   S( 99,  40),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-49,  43),   S(-94,  66),   S(-44,  34),   S( -5,  -8),   S( -8,  16),   S(-18,  10),   S(-77,  15),   S(-57,   6),
            S(-67,  45),   S(-71,  36),   S(-46,  31),   S(-16,  -6),   S(-23,  -6),   S(-21,  -9),   S(-48, -24),   S(-24, -24),
            S(-33,  37),   S(-32,  55),   S(-10,  47),   S(-26,  32),   S( 13,   2),   S(-56,  -8),   S(-59, -19),   S(-11, -16),
            S(-26,  57),   S( 38,  57),   S(120,  24),   S(-34,  29),   S( -7,   9),   S(-12,   2),   S(  1,   2),   S(  9, -10),
            S( 30,  80),   S( 62,  69),   S( 60,  88),   S( 62,  86),   S( 10,  25),   S(  0,   9),   S(  0,  -5),   S(  0,   6),
            S( 77,  95),   S( 85, 106),   S( 87, 104),   S( 46,  37),   S(  8,  35),   S( -3, -21),   S(  5, -41),   S( 32,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-114,  65),  S(-92,  45),   S(-36,  35),   S( 20,  22),   S(-26, -11),   S(-69,  19),   S(-80,  14),   S(-96,  27),
            S(-60,  49),   S(-15,  19),   S(-49,  28),   S(-44,  17),   S(-98,  14),   S(-63,   6),   S(-107,  17),  S(-64,  23),
            S(-49,  44),   S(-53,  53),   S(-56,  49),   S(-79,  43),   S(-111,  48),  S(-89,   6),   S(-84,   6),   S(-46,  13),
            S(-18,  62),   S( 42,  47),   S( 35,  59),   S( 27,  59),   S(-25,  34),   S(-32,   9),   S( 75, -13),   S( 72, -19),
            S( 79,  29),   S( 84,  49),   S( 91,  49),   S(105,  91),   S( 68,  67),   S( 49,  12),   S( 32,   2),   S( 51,  -2),
            S( 23,  -3),   S(110,  -9),   S(144,  28),   S( 99,  77),   S(  9,  49),   S( 16, -29),   S(  0, -34),   S(  9, -21),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-126,  26),  S(-117,  23),  S( 15, -17),   S( -6,  35),   S(-39,  -2),   S(-87,  36),   S(-166,  50),  S(-56,  38),
            S(-85,   6),   S(-65,  -4),   S(-60,   9),   S(-35, -10),   S(-89,  27),   S(-74,  27),   S(-128,  39),  S(-125,  45),
            S(-60,  18),   S(-98,  23),   S(-76,  29),   S(-119,  60),  S(-89,  47),   S(-33,  19),   S(-88,  31),   S(-112,  47),
            S( 21,   8),   S(  9,   6),   S(-25,  20),   S(-26,  48),   S( 35,  40),   S(  6,  38),   S(  5,  17),   S( 46,  -2),
            S( 56,  -9),   S( 38, -14),   S( 41,  20),   S( 63,  68),   S(122,  64),   S(140,  30),   S( 79,  11),   S( 80,  -1),
            S( 53, -46),   S( 14, -48),   S( 24, -22),   S( 66,  46),   S( 35,  34),   S( 71,  15),   S( 41, -16),   S( 42,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-122,   8),  S(-100,   7),  S( 16, -23),   S( -7,   9),   S( 11,  25),   S(-116,  48),  S(-103,  57),  S(-109,  53),
            S(-110, -13),  S(-56, -27),   S(-24, -32),   S(-63,   8),   S(-59,  16),   S( -4,  12),   S(-95,  58),   S(-120,  56),
            S(-19, -19),   S(-34, -14),   S(-49,  23),   S(-21,   6),   S(-75,  35),   S(-25,  27),   S(-31,  34),   S(-100,  43),
            S( 11,  11),   S(-62,  17),   S(  1,   4),   S(-61,  23),   S( -5,  30),   S( 36,  20),   S( 75,  34),   S( 31,  12),
            S( 18,  31),   S(  0,  22),   S(-15,  27),   S(-17,  22),   S( 49,  49),   S( 99,  45),   S(109,  48),   S(173,   0),
            S( 19, -16),   S( 15, -48),   S( 13, -31),   S(-15, -42),   S( 20, -25),   S( 31,  67),   S( 85,  92),   S( 93,  76),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 13,  47),   S( -1,  56),   S(-30,   6),   S( 10,  22),   S(  0,  13),   S(-22, -28),   S(-20, -21),   S(-51,   2),
            S(-36, -11),   S( 23,  22),   S( 24,  53),   S(-12, -33),   S(  2,  49),   S(-11,  13),   S(-49, -37),   S( 16, -64),
            S(-52,  55),   S( 12,  38),   S(  8,  39),   S( 33,   2),   S( -2, -13),   S(-22, -18),   S(-19, -49),   S(-16, -36),
            S( -8,  37),   S( 27,  82),   S( 47,  48),   S( 42,   9),   S( -5, -44),   S(-12, -27),   S( -3,  12),   S(-19, -17),
            S( 23,  10),   S( 30, 124),   S( 70,  82),   S( 26,  58),   S( -1,  40),   S(  9,  12),   S(  6, -14),   S(  3,  -3),
            S( 18,   7),   S( 44, 160),   S( 92, 186),   S( 63, 101),   S( -9, -37),   S(  0, -50),   S( -9, -42),   S( -9, -71),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-37,  26),   S(-16,  24),   S(  6,  25),   S(  0,   6),   S(-24, -39),   S(-32,   0),   S(-42,  -6),   S(-17,   7),
            S( -8, -19),   S( -1,   3),   S( -3,  11),   S( 13,  57),   S(-28,   0),   S(  2,  -6),   S(-52, -20),   S(-43,  -7),
            S( 13,   6),   S( 40, -14),   S(-10,  36),   S( 21,  65),   S( -2,  52),   S(-18, -24),   S(-37, -13),   S(  2, -37),
            S( -4,  14),   S( 22,  31),   S( 27,  43),   S( 30,  81),   S( -6,  34),   S(  2, -11),   S( 18,  -9),   S( 45, -44),
            S( 47,  -4),   S( 76,  75),   S( 81, 123),   S( 97, 136),   S( 71,  74),   S( 33,  16),   S(  6, -68),   S( 23, -39),
            S(  3,  19),   S( 87,  45),   S( 86, 183),   S(104, 185),   S( 57,  81),   S( 29,   8),   S( -8, -50),   S( 29, -11),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-39, -40),   S(-39,   0),   S(-31, -37),   S( -3,   2),   S(-15, -19),   S(-68,  23),   S(-51,  14),   S( -9,  29),
            S(-33,  14),   S( -6,  -4),   S(-55, -35),   S( -3,  28),   S(-35,  29),   S( -8,  17),   S(-23,  16),   S(  2,  12),
            S(  6, -14),   S(-25,  -7),   S(-16,   5),   S(-33,  12),   S( -4,  48),   S(-28,  36),   S( -9,   5),   S(-26,  29),
            S( 27, -10),   S( 28, -11),   S( 19,  -2),   S( 29,  48),   S(-14,  75),   S(  6,  62),   S(  2,   9),   S( 47,  13),
            S( 55, -21),   S( 29, -14),   S( 40,   7),   S( 62,  74),   S( 84, 143),   S( 71, 103),   S( 43,   7),   S( 47, -10),
            S( 37, -26),   S( 36,   8),   S( 37,  49),   S( 60,  70),   S( 64, 163),   S( 55, 122),   S( 34,  67),   S( -6, -26),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-53, -23),   S(-42,  -2),   S( 11, -22),   S(  5,  16),   S(  9,  36),   S(-24,  64),   S(-10,  34),   S(-15,  28),
            S(-29, -69),   S(  5, -25),   S(-20, -20),   S( 24,  21),   S(-28,  23),   S(  1,  54),   S(  1,  64),   S(-11,  41),
            S( -5, -60),   S( -2, -44),   S(-16,  -8),   S(  0,   9),   S( 18,   4),   S( 20,  20),   S(  6,  77),   S(  6,  54),
            S( 36,  -7),   S(-14,  18),   S(-11,  -2),   S(  4,   5),   S( 10,  14),   S( 44,  27),   S( 14, 101),   S(  2,  27),
            S( -4, -47),   S(-13, -61),   S( -3, -17),   S(  6, -45),   S( 45,  91),   S(130,  92),   S(-14, 174),   S( 65,   0),
            S( 26, -14),   S( 13,  22),   S( 15,   3),   S( 16,  24),   S( 42,  84),   S( 49, 187),   S( -6, 152),   S( 36, -29),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: bucket 0 */
            S(-13, -41),   S(  7,  12),   S(-27,   4),   S(-26, -24),   S(-25, -12),   S(-22, -58),   S(-48, -64),   S(-72,  -9),
            S( -3,  32),   S(  9, -33),   S(-24,   8),   S(  4, -16),   S( -5, -37),   S( -6, -11),   S(-20, -33),   S(-41, -41),
            S( 21,  29),   S(  0,  -9),   S( 22, -19),   S( 10,  -1),   S( 47, -17),   S(-18,  -9),   S( -7, -33),   S(-45, -36),
            S( 13, -21),   S( 30,  19),   S( 26,   4),   S( 29,  16),   S( 21, -10),   S( 10,  -2),   S(  9, -29),   S( -4, -25),
            S( 33, -44),   S( 26,  -1),   S( 31, -26),   S( 66, -31),   S( 50, -24),   S( 29,   7),   S( 27, -41),   S(-48, -17),
            S( 17, -32),   S( 23, -17),   S( 33,  -9),   S( 52, -37),   S( 25, -58),   S( 28, -42),   S( 13,  -7),   S(  2,  18),
            S( -5, -41),   S( 22, -37),   S( 39, -18),   S( 36, -65),   S( 28, -19),   S(-16, -20),   S(-58, -34),   S(-13, -36),
            S(-13, -46),   S( -9, -29),   S(-19, -30),   S( 33,  14),   S(-20, -26),   S(  9, -14),   S(  5,   7),   S(-24, -27),

            /* knights: bucket 1 */
            S(-56,  -5),   S(-61,  78),   S( 34,  23),   S(-44,  63),   S(-19,  40),   S(-44,  42),   S(-42,  45),   S( -8, -32),
            S( 28,  -7),   S(-11,  40),   S(  6,  12),   S( -7,  34),   S(  0,  22),   S( -6,  24),   S(-15,  -1),   S(-31,  -5),
            S(-45,  53),   S(  4,  22),   S( 16,   9),   S( 13,  34),   S( 24,  29),   S(-19,  20),   S( -7,  11),   S(-20,  14),
            S(  6,  33),   S( 48,  32),   S( 24,  37),   S( 19,  40),   S( 18,  29),   S( 12,  25),   S( 13,  12),   S( 13,  11),
            S(-20,  29),   S( 21,   5),   S( 26,  25),   S( 53,   0),   S( 30,  16),   S( 35,   7),   S( 21,  10),   S(-13,  49),
            S(  5,   8),   S( -4,  28),   S( 33,   9),   S( 38, -24),   S(  7,  18),   S( 62,   7),   S( 16,  11),   S( -3, -20),
            S( 27, -11),   S( 32,  23),   S(-19,   6),   S( 20,  13),   S( -4,   3),   S( 57, -30),   S(  8,  -2),   S(-52, -12),
            S(-112, -85),  S( -6,  27),   S(-12,  25),   S(-10,   2),   S( 17, -14),   S(-47, -26),   S(-22, -14),   S(-66, -67),

            /* knights: bucket 2 */
            S(-37,   6),   S(-19,  26),   S(-42,  46),   S(-48,  59),   S(-40,  52),   S(-45,  77),   S(-47,  56),   S( -4, -25),
            S(-33,  30),   S(-26,  25),   S(-15,  28),   S(-16,  32),   S( -8,  29),   S(-15,  55),   S(-56,  65),   S(-46,  56),
            S(-28,  25),   S(-12,  28),   S(-13,  43),   S(  9,  33),   S(  6,  40),   S( -7,  24),   S( -8,  43),   S(-20,  38),
            S(-14,  43),   S(-32,  44),   S( -2,  41),   S( -4,  55),   S(-12,  66),   S( -3,  48),   S(-20,  61),   S( -8,  56),
            S( -9,  28),   S(-11,  42),   S(-19,  43),   S(-15,  49),   S(-17,  53),   S(-18,  53),   S( -5,  42),   S(-40,  44),
            S(-32,  30),   S(-11,  44),   S(-35,  42),   S(-21,  42),   S(-19,  24),   S(-13,  46),   S(-47,  38),   S( 41, -16),
            S(-73,  51),   S(-43,  15),   S(-43,  30),   S(-35,  38),   S(-11,  40),   S(-20,  36),   S(-13,  17),   S(-36,   4),
            S(-163,  11),  S(-38,  13),   S(-114,  34),  S(-60,  12),   S(-25,  16),   S(-95,  31),   S(  4,   0),   S(-160, -91),

            /* knights: bucket 3 */
            S(-82,  -4),   S(-13, -21),   S(-47,  18),   S(-31,  22),   S(-20,  20),   S(-16,  25),   S( -6,  18),   S(-46,  13),
            S(-33,  11),   S(-44,  13),   S(-22,  16),   S(  0,  20),   S(  0,  25),   S(-25,  19),   S(-12,   8),   S(-29,  68),
            S(-14,  -8),   S(-13,  25),   S( -9,  36),   S( 14,  26),   S( 22,  38),   S(  5,  26),   S( -4,  32),   S(  0,  50),
            S(-17,  27),   S( -4,  42),   S( -1,  57),   S(  9,  51),   S( 18,  59),   S( 20,  59),   S( 14,  55),   S( 11,  39),
            S(  2,  20),   S(  0,  33),   S( 16,  32),   S( 13,  56),   S(  2,  59),   S(  3,  71),   S( 22,  65),   S( -6,  34),
            S( -2,  13),   S( -1,  32),   S( 12,  23),   S( 40,  12),   S( 32,  14),   S( 45,  14),   S(-24,  42),   S(-30,  59),
            S( -1,   2),   S( -2,  23),   S( 11,   6),   S( 23,  22),   S( 44,   5),   S( 60,  -8),   S(  3, -17),   S( -3,  -5),
            S(-125,  13),  S(-61,  24),   S(-29,  13),   S(  4,  27),   S( 27,  11),   S( 10,  17),   S(-23, -14),   S(-110, -20),

            /* knights: bucket 4 */
            S( -5,   0),   S(-25,   5),   S(  4,  11),   S(-48, -14),   S( -5,  -9),   S( -8, -22),   S( 14, -92),   S(-43, -61),
            S( 72,  15),   S(-54,  30),   S(-17,   2),   S( -1,  -6),   S( 18, -20),   S(-25, -41),   S( -3, -16),   S(-18, -67),
            S(-51,  33),   S( -7,   5),   S( 29, -21),   S( 14,   8),   S( 36, -26),   S(-42,  18),   S(  3, -34),   S(-44, -41),
            S(-11,  46),   S( 24, -15),   S(106, -29),   S( 39,  11),   S( 33,  -6),   S( 98, -16),   S(-11, -19),   S(  9, -20),
            S( 37,  30),   S(  9,  42),   S( 46,  24),   S( 42,   8),   S( 74,  -1),   S( 32,  -5),   S(-12, -24),   S( 33, -34),
            S( 30,  10),   S(-15,   9),   S( 98, -15),   S( 22,  -8),   S( 21, -33),   S( 26,  -5),   S( 18,   6),   S(-32, -45),
            S( 28,  16),   S(-16,  -2),   S( 45,  25),   S( 29,  20),   S(  3,   0),   S( 10,  -3),   S( -4, -32),   S(  8,  19),
            S( -9,   7),   S( -5, -21),   S(  3,  -2),   S(-11, -37),   S( -4,  -4),   S( -5, -18),   S( -3, -19),   S( -5,  -9),

            /* knights: bucket 5 */
            S( 32,   8),   S( -6,  21),   S( 51,  31),   S( 19,  42),   S( 32,  23),   S(  5,  20),   S(  0,  16),   S(-40, -29),
            S( 11, -20),   S( 37,  40),   S( 33,  22),   S( 21,  26),   S(  2,  46),   S( 26,  12),   S( 50,  16),   S(-29, -10),
            S( 45, -12),   S(-10,  54),   S( 85,   7),   S( 91,   9),   S( 20,  38),   S( 24,  22),   S( 45,  13),   S( 31,   9),
            S( 65,  37),   S( 39,  27),   S( 99,   6),   S( 46,  27),   S( 74,  24),   S( 46,  35),   S( 70,  42),   S( 21,  44),
            S( 33,  26),   S( 79,   4),   S( 49,  32),   S( 86,  19),   S(127,  13),   S( 66,  24),   S( 32,  15),   S( 20,  18),
            S( -3,  20),   S( 47,  35),   S( 46,  17),   S( 69,  25),   S( 39,  39),   S( 22,  44),   S( 29,  20),   S(-20,   7),
            S(  9,  28),   S( -9,  44),   S( 30,  23),   S( -1,  58),   S( 29,  35),   S( 21,  49),   S(  0,  40),   S( -1,  10),
            S(-27, -30),   S(  2,  25),   S( 28,  50),   S(  6,  50),   S(  9,  53),   S(  4,  44),   S( 10,  20),   S(-23, -21),

            /* knights: bucket 6 */
            S(-12, -33),   S(-60,  26),   S( 29,  -4),   S( 19,  20),   S( -2,  31),   S( 34,  50),   S(-32,   4),   S( -7,  15),
            S(-12, -47),   S( 58,  -1),   S( 48,   4),   S( 16,  10),   S(  2,  41),   S( 61,  31),   S( 16,  38),   S(-11,  14),
            S(-19,  15),   S( 41,   0),   S( 25,  21),   S( 82,   4),   S( 31,  26),   S( 17,  33),   S( 23,  61),   S( 33,  31),
            S( 53,  10),   S( 93,   4),   S(117,   0),   S(106,   6),   S( 59,  22),   S( 84,  20),   S( 42,  51),   S(-17,  78),
            S( 47,  16),   S( 74,   1),   S( 99,   8),   S( 64,  25),   S(133,   9),   S(139,   5),   S( 48,  39),   S(  1,  65),
            S( -7,  34),   S( 47,   7),   S( 75,   4),   S( 75,  22),   S( 55,  30),   S( 55,  37),   S( 21,  40),   S( 50,  34),
            S(-25,  13),   S(-23,  36),   S(-16,  28),   S( 36,  42),   S( 24,  63),   S( 17,  25),   S(  8,  67),   S(  3,  37),
            S(-55,  -7),   S( -7,  29),   S( 31,  49),   S(-12,  36),   S(  4,  31),   S( 24,  77),   S( 18,  23),   S( 11,  -1),

            /* knights: bucket 7 */
            S(-46, -82),   S(-148, -64),  S(-98, -25),   S(-53, -24),   S(-25, -29),   S(-48,  33),   S(  1, -27),   S(-25, -22),
            S(-63, -76),   S(-52, -42),   S(-46, -20),   S(-58,   3),   S(-30,  10),   S( 73, -39),   S(  9,  29),   S( 21,  16),
            S(-74, -65),   S(-47, -26),   S( 19, -28),   S( 84, -42),   S( 23,  -3),   S( 62, -21),   S( 18,  29),   S( 10,  48),
            S(-68,  -6),   S( 28, -26),   S(  2,  -6),   S( 80, -18),   S(109, -19),   S( 54,  -3),   S( 14,  19),   S( 19,  32),
            S(-35, -23),   S(-22, -16),   S( 75, -46),   S(114, -34),   S(143, -35),   S( 57,  15),   S(116,   0),   S( 77,   9),
            S( 16, -44),   S( 15, -40),   S( 20, -30),   S( 30, -17),   S( 96, -12),   S( 66, -11),   S( 66,  13),   S(-36,  23),
            S(-62, -38),   S(-66, -14),   S(  9, -30),   S( 56,   1),   S( 33,  -3),   S( 62,  -5),   S(-20, -25),   S( 24,  -1),
            S(-64, -29),   S(-12, -39),   S( 17, -15),   S( 19,  -8),   S(  2, -22),   S( 25,  12),   S( -2,   5),   S(-17, -37),

            /* knights: bucket 8 */
            S( -5, -15),   S(-23, -32),   S( -4,   4),   S( -5,   2),   S(-14, -39),   S(-23, -76),   S( -7, -16),   S(-13, -33),
            S(  0,  14),   S(  5,  29),   S(  9,  22),   S(-13, -17),   S(-47, -63),   S(-37, -58),   S( -4, -17),   S(-23, -40),
            S(  2,  -4),   S(-13,  -2),   S(  4,  11),   S(-21, -27),   S(-10, -46),   S(-10, -22),   S( -5, -25),   S(-24, -59),
            S( -5,  31),   S( 26,  33),   S(  8,   1),   S( 12,  23),   S( 18,  -7),   S( -1,   6),   S( -9, -53),   S(  3,   1),
            S( 25,  72),   S(  2, -17),   S( 20,  -1),   S( 33,  24),   S(  8,  37),   S( 31,  -3),   S( -2, -38),   S(-10, -20),
            S( 12,  46),   S(-15,  12),   S( 28,  24),   S( 47,  36),   S(  3,  -2),   S(  1, -16),   S(  0,   3),   S(-24, -44),
            S(  2,  30),   S(  2,  21),   S(  0,   4),   S( 33,  55),   S(  3,  14),   S( -2,  -4),   S(  2,  25),   S(  0,  15),
            S( 10,  15),   S( 14,  33),   S( 16,  32),   S(  2,  25),   S(  4,  14),   S(  3,   5),   S(  6,  24),   S(  3,   8),

            /* knights: bucket 9 */
            S(-10, -56),   S( -7,  12),   S(-24, -52),   S( -4, -19),   S(-31, -72),   S(-15, -21),   S(-14, -19),   S( -1,  -8),
            S(-19, -62),   S(  1,   1),   S( -1, -64),   S(-35, -32),   S( -3, -17),   S( -9, -46),   S(-18, -49),   S(-13, -33),
            S( -7,  10),   S(-15, -24),   S(-28, -33),   S( -1,  -8),   S( 15,  -5),   S(-28, -16),   S(-18, -20),   S( -5,  20),
            S(-21, -22),   S( -8,  -4),   S( 30,   6),   S( 43, -12),   S( 22,   4),   S( 13, -12),   S(  0,  -6),   S( -1,   0),
            S( -5,   8),   S( 37, -14),   S( -1, -20),   S(  9,   8),   S( 27,   3),   S( 11, -10),   S( 16,  -6),   S(  0, -39),
            S(  5,   9),   S( 18,  35),   S( 29,   8),   S( 22,   0),   S( 42,  19),   S( 12,  29),   S(  9,  14),   S( 13,  39),
            S( -4, -37),   S(-22,  13),   S( 16,  30),   S( -7,  27),   S( 22,  89),   S( 21,  15),   S( 13,  42),   S( -2,  11),
            S(-11, -13),   S( 16,  54),   S(  8,  19),   S( 22,  48),   S( 16,  33),   S( 15,  46),   S(  3,  29),   S( -3,  -5),

            /* knights: bucket 10 */
            S(-23, -94),   S(-38, -106),  S(-14, -43),   S(-36, -53),   S(-16, -28),   S( -6, -20),   S(-15,   0),   S( -2,  -9),
            S(-23, -80),   S(-17, -36),   S( -7, -25),   S(-31, -42),   S(-44, -44),   S(-16, -43),   S( -5,  -1),   S( -7, -39),
            S(-16, -53),   S(-29, -88),   S(-38, -38),   S(-13, -33),   S(-23, -31),   S( -8, -20),   S(  5,  14),   S(-16, -50),
            S(-27, -47),   S(-17, -72),   S( 12, -29),   S( 30, -18),   S( 63, -18),   S( 32, -22),   S( 23,  15),   S( -8,  20),
            S(-13, -10),   S( -7, -23),   S( -7,  -8),   S( 49, -35),   S( 64,   1),   S( 17, -10),   S(  5,  20),   S( 10,  10),
            S( -4, -20),   S( -8, -33),   S(  1, -27),   S( 28,   0),   S( 47,  11),   S( 13,  17),   S( 26,  20),   S(  8,  53),
            S( -4,   2),   S( -3,   1),   S(  6,   0),   S( 21,  22),   S(  0,  45),   S(  0,   8),   S( 15,  56),   S( -8, -24),
            S(  1,   4),   S(  9,  32),   S( -1,   1),   S(  5,   6),   S( -1,  28),   S( 19,  53),   S(  1,  22),   S( -1,   9),

            /* knights: bucket 11 */
            S( -2, -14),   S(-17, -49),   S(-16, -48),   S(-12, -15),   S(-19, -17),   S(-34, -69),   S(-12, -34),   S(-10, -27),
            S( -9,  -6),   S(-17, -37),   S( -8, -76),   S(-38, -24),   S(-12,   3),   S(-23,  -9),   S(-17, -33),   S(-16, -28),
            S(-27, -66),   S(-14, -27),   S(-45, -32),   S(-19, -25),   S(-34,   7),   S(-27, -10),   S( -3, -16),   S(  0,  26),
            S(-32, -49),   S( -5, -33),   S(-12, -15),   S( 60,  11),   S(  1,  -9),   S( 48,   4),   S(-11,  25),   S(  2,  18),
            S(  7,   1),   S(-16, -25),   S( 27, -18),   S( 20,  -7),   S(  6,  10),   S( 51,  -9),   S( 29,  32),   S( 25,  98),
            S(-14,  -5),   S( -7,  13),   S(  5, -20),   S( 34,  -8),   S( 10,   6),   S( 53,  32),   S( 22,  33),   S( 14,  59),
            S( -1,   8),   S( -8,   2),   S( -1, -17),   S( -4,  -8),   S( 18,  39),   S(-12,  37),   S( -5,  22),   S( 14,  69),
            S(-16, -31),   S(-13, -55),   S(  2,  -5),   S(  6,  -1),   S(  4,   8),   S(  1,  30),   S(  4,  24),   S(  5,  31),

            /* knights: bucket 12 */
            S( -4, -15),   S( -1,   4),   S( -4, -24),   S( -6, -12),   S(  0,   5),   S(-12, -39),   S(  0,  12),   S(  0,  -2),
            S( -5, -17),   S(  1,   4),   S(  1,   2),   S( -7,   2),   S( -3,   6),   S(-10, -29),   S( -3, -22),   S(  0,   0),
            S(  4,   4),   S( -2,  -1),   S(  0,  -6),   S(  4,  13),   S( -4,   0),   S(  4,   7),   S(  8,  13),   S( -9, -33),
            S( -2, -15),   S( -6, -12),   S(  3, -23),   S(  0,   4),   S(  2,  -8),   S(  6,   2),   S( -8, -19),   S( -5, -25),
            S( 10,  30),   S( -5, -26),   S( -2,  10),   S( 10,  20),   S(-10, -20),   S( -9, -29),   S(-13, -50),   S( -6, -11),
            S(  9,  45),   S(-11, -27),   S( 12,  80),   S( 11,  16),   S( 14,  18),   S(  7,  11),   S(  2,  12),   S( -1,  -4),
            S(  5,  29),   S(-21, -38),   S( 14,  30),   S(  2,  31),   S(-14, -28),   S( -1, -14),   S(  0,  -5),   S( -5, -16),
            S(  7,  17),   S(  3,  46),   S( -4,  11),   S(  5,  18),   S( -2, -12),   S(  1, -10),   S(  2,   6),   S(  0,   0),

            /* knights: bucket 13 */
            S( -6, -15),   S(  2,   0),   S(  2,   6),   S(  5,   9),   S(-10, -25),   S( -4, -19),   S( -1,  -2),   S(  1,  -3),
            S( -3,  -7),   S(  3,  11),   S( -4, -22),   S(-13, -16),   S(  1,   5),   S( -6, -40),   S( -4, -20),   S(  2,  -1),
            S(  5,  20),   S( -5,  -9),   S(  2, -12),   S( 16,  22),   S(-13, -47),   S(  3,  20),   S(-19, -31),   S( -9, -29),
            S( -7, -18),   S(  7,  16),   S( 16,  46),   S( -1,  -7),   S(  2,   0),   S( 15,  16),   S(  2,   3),   S(-15, -30),
            S(  4,   7),   S(  7,  25),   S( 16,  20),   S(-14,   2),   S( -7,  33),   S(  2,  -9),   S(  9,  13),   S( -1,   5),
            S(  5,  22),   S( 10,  12),   S( 11,  78),   S(-32, -16),   S( 16,  23),   S( -1,   6),   S(  4,   0),   S( -3,  -3),
            S(  2,  14),   S( -1,  -1),   S( -1,  26),   S(  1,  28),   S( 11,  55),   S(  5,  29),   S(  6,  28),   S( -4,  -4),
            S(  1,   8),   S(  3,  60),   S( 16,  51),   S(  3,  16),   S( -1,  28),   S(  0,  -3),   S(  0,  10),   S(  0,  -1),

            /* knights: bucket 14 */
            S( -1,   0),   S(  1,   2),   S( -7, -29),   S( -4,  -5),   S( -8, -27),   S( -2,  -6),   S(  3,   6),   S( -2,  -8),
            S(  3,  17),   S(  2,   1),   S(-18, -83),   S( -6,  -4),   S( 10,  15),   S(  0, -19),   S(  2,   6),   S( -5, -31),
            S(-10, -31),   S( -6, -22),   S(-10, -28),   S(  0,  -3),   S(  6, -11),   S(-13,  -4),   S(  3,   3),   S(  1, -10),
            S(  0,   4),   S( -6, -30),   S( -7, -25),   S(  7,  -1),   S( -4, -21),   S(  3,  -4),   S(  3,  -6),   S(  2,  28),
            S(  0,   9),   S( -5, -12),   S( 12,  16),   S(-23, -59),   S(-14, -25),   S(  3,   1),   S(  3,   1),   S(  2,  12),
            S(  0,   3),   S(  6,  22),   S(  1,  -4),   S( 18,  -7),   S( -3, -11),   S(-14, -29),   S( -2,  25),   S(  4,  27),
            S( -2,  -4),   S(  0,   8),   S(  9,  26),   S( -2,   7),   S(  4,  57),   S( -7,  17),   S(  7,  49),   S(  2,  21),
            S( -2, -10),   S( -2,  -1),   S(  0,  -5),   S(  7,  52),   S(  6,  33),   S(  3,  35),   S( -5,  21),   S( -1,   5),

            /* knights: bucket 15 */
            S( -1,  -4),   S(-10, -27),   S(  1,  -2),   S( -2, -14),   S( -1, -11),   S( -5, -16),   S(  3,   6),   S( -2,  -9),
            S( -6, -33),   S(  5,   6),   S( -9, -39),   S(  5,  -2),   S( -8, -34),   S( -1, -27),   S( -3,  -7),   S(  0,   0),
            S( -5, -17),   S( -5, -18),   S( -5, -14),   S( -6, -37),   S(  1, -11),   S(-16, -18),   S( -4,  -8),   S(  1,  11),
            S( -5,  -9),   S( -1, -19),   S(  5, -25),   S( -7, -32),   S( -8, -21),   S(  0,  31),   S(  5,  11),   S(-10, -10),
            S( -7, -29),   S( -3, -12),   S( -6, -46),   S(-24, -41),   S( 23,  29),   S(  5, -16),   S( -3, -14),   S(  4,  21),
            S( -8, -16),   S( -1,  -2),   S( -6, -11),   S(  3,   4),   S( -1, -12),   S(-12, -11),   S( -1,  21),   S(  6,  52),
            S( -3, -11),   S(  0,  13),   S(  2, -10),   S( -5, -10),   S( -4,  -4),   S(-13,  -8),   S( -1,  -5),   S(  1,  25),
            S(  0,  -1),   S(  4,  13),   S( -3, -11),   S(  3,   5),   S(  1,  13),   S(  0,   3),   S(  6,  38),   S( -2,  -8),

            /* bishops: bucket 0 */
            S( -7,  -4),   S( -5, -27),   S( 63, -14),   S( -5,  28),   S( -9,  14),   S( 14,  -4),   S(  3, -13),   S(  6, -25),
            S( 38,  -9),   S( 78,  10),   S( 22,  29),   S( 16,   8),   S( -4,  37),   S( -5,   8),   S(-33,  22),   S( -1,   5),
            S( 74, -13),   S( 52,  17),   S( 31,  44),   S( 10,  60),   S( 11,  39),   S(-21,  65),   S(  4,   0),   S(  0,   1),
            S( 40,  11),   S( 55,  35),   S( 20,  49),   S( 25,  25),   S( -3,  42),   S( 21,  15),   S(-12,  13),   S(-13,  52),
            S(-10,  39),   S( 33,  13),   S(-31,  60),   S( 37,  23),   S( 35,  21),   S(  8,  24),   S( 19,  13),   S(-36,  65),
            S(-56,  65),   S(-30,  74),   S( 29,  26),   S(-17,  37),   S( 21,  61),   S(-28,  32),   S( -9,  26),   S(  1,  41),
            S(-36,  75),   S( 13,  18),   S(  0,  61),   S(  8,  46),   S(-50,  46),   S(  4,  68),   S(-14,  20),   S(-25,  16),
            S(-52, -14),   S( -2,  63),   S(-21,  58),   S(-20,  39),   S(  0,  64),   S( -2,  12),   S( -4,  43),   S( -9,  50),

            /* bishops: bucket 1 */
            S( 22,  32),   S( -1,  16),   S(  4,  21),   S(-25,  46),   S( -6,  17),   S(-11,  32),   S(-28,  38),   S(-58,  39),
            S(  1,  12),   S( 38,  15),   S( 32,  16),   S( 25,  32),   S(-13,  35),   S( 18,  17),   S(-30,  42),   S( -3,  12),
            S( 38,  -4),   S(  3,  34),   S( 41,  37),   S(  9,  48),   S( 15,  43),   S(-17,  54),   S( 25,   8),   S( -5,  16),
            S( 55,  -5),   S( 13,  57),   S( -2,  48),   S( 29,  37),   S( 15,  38),   S(  7,  40),   S(-22,  56),   S( 24,  10),
            S( 59,  29),   S(  1,  33),   S(  1,  40),   S(-15,  49),   S(  4,  32),   S(-29,  60),   S( 23,  14),   S(-22,  47),
            S(-56,  62),   S( 37,  41),   S( 14,  59),   S(  2,  37),   S( -6,  56),   S( 23,  47),   S(-45,  63),   S( 28,  12),
            S(-28,  56),   S(-27,  72),   S( 32,  41),   S(-27,  62),   S( 18,  52),   S(-10,  58),   S(-24,  78),   S(-11,  51),
            S(-23,  56),   S( -7,  53),   S(-18,  50),   S(-11,  41),   S(  6,  65),   S( 19,  53),   S(-36,  91),   S(-38, 120),

            /* bishops: bucket 2 */
            S( -6,  33),   S( -9,  29),   S(-19,  34),   S(-32,  49),   S(-25,  47),   S(-33,  33),   S(-33,   9),   S(-45,  42),
            S(-16,  14),   S(  1,  28),   S( 13,  31),   S( -5,  44),   S(-11,  40),   S(  0,  25),   S( -5,   9),   S( -4, -10),
            S(-12,  43),   S(-13,  47),   S( -5,  74),   S(-12,  63),   S( -9,  58),   S( -6,  55),   S( -7,  37),   S( -8,   8),
            S( -6,  49),   S(-38,  77),   S(-32,  60),   S(-19,  67),   S(-15,  68),   S(-15,  65),   S( -4,  48),   S( 15,  20),
            S(-16,  63),   S(-17,  53),   S(-46,  69),   S(-41,  65),   S(-40,  58),   S(-20,  74),   S( -4,  40),   S(-36,  56),
            S(-12,  52),   S(-41,  71),   S(-26,  70),   S(-65,  74),   S(-57,  68),   S(-41,  76),   S(-16,  69),   S(-29,  66),
            S(-76,  79),   S(-60,  81),   S(-51,  85),   S(-17,  61),   S(-74,  85),   S(-69,  59),   S(-75,  85),   S(-49,  66),
            S(-83, 119),   S(-83,  91),   S(-117, 105),  S(-108,  98),  S(-57,  70),   S(-66,  91),   S(-50,  85),   S(-63,  68),

            /* bishops: bucket 3 */
            S(-19,  38),   S( -2,  29),   S(  4,  26),   S( -8,  46),   S(-13,  46),   S( 41,  14),   S( 27,  -1),   S( 23, -29),
            S( -3,  29),   S( -3,  48),   S( 12,  30),   S( -3,  63),   S(  2,  46),   S( -4,  53),   S( 34,  33),   S( 20,  10),
            S( 12,  46),   S( -8,  65),   S( -5,  88),   S(  8,  65),   S(  3,  87),   S(  4,  74),   S( 20,  48),   S( 37,  16),
            S( 14,  52),   S( -5,  71),   S( -9,  77),   S(-10,  83),   S( 13,  64),   S( 13,  64),   S(  4,  66),   S( 16,  38),
            S(-17,  68),   S(  8,  43),   S( -5,  60),   S( -7,  76),   S( -5,  72),   S(  5,  65),   S(  3,  65),   S( 12,  62),
            S( -8,  53),   S( -7,  68),   S( -2,  72),   S(-22,  64),   S(-10,  61),   S( 24,  63),   S(  8,  65),   S(-11,  85),
            S(-24,  59),   S(-42,  84),   S( 18,  55),   S(-21,  76),   S(-14,  62),   S(-40,  84),   S(-37,  80),   S( -9,  77),
            S( -6, 110),   S( -6,  78),   S( 12,  65),   S(-43,  91),   S(-38,  80),   S(-59, 104),   S( 14,  61),   S( 60,  30),

            /* bishops: bucket 4 */
            S(-48,  14),   S(-24,  17),   S(-22,  12),   S(-34,  23),   S( -8,   2),   S(-31,  12),   S( 11,  21),   S( -6,  26),
            S(-19,  -6),   S( 26,  -8),   S(-23,  38),   S( -7,   7),   S(-18,  16),   S( 64,   5),   S(-59,  12),   S( 33,  18),
            S(-34,  -4),   S(-17,  23),   S( 36,   7),   S(-17,  45),   S( 52,  19),   S( 36,  -1),   S(-28, -16),   S(-57,  28),
            S(-15,  16),   S(-23,  49),   S( 54,  12),   S( 98, -12),   S(-43,  48),   S( 27,  29),   S( 70,  37),   S(-30,  -4),
            S( 14,  16),   S(-18,  44),   S( -2,  34),   S( 71,  12),   S( 25,  17),   S( 13,  30),   S(-42,  22),   S( 36,  33),
            S( 14,  21),   S(  8,  41),   S(  2,  42),   S( 59,   1),   S( 46,  25),   S( -5,  18),   S( -3,   2),   S( 16,  13),
            S(-25,  20),   S( 32,  27),   S( -8,  24),   S(  5,  45),   S(-12,  33),   S( -6,  14),   S( 18,  -2),   S( 11,  10),
            S( -9,  17),   S(-20,  -2),   S(  3,  24),   S(-19,  24),   S( 20,  30),   S( -9,  11),   S(-15, -18),   S(-15,  -1),

            /* bishops: bucket 5 */
            S(-35,  25),   S(-24,  52),   S(-60,  57),   S( -9,  45),   S( -6,  43),   S(-40,  38),   S(-28,  35),   S(-28,  41),
            S(-45,  44),   S(-10,  35),   S( -4,  51),   S( 46,  35),   S( 37,  19),   S( 20,  35),   S(  4,  17),   S(-38,  38),
            S(  2,  41),   S( 21,  48),   S( 40,  41),   S( 44,  29),   S( 49,  39),   S( 31,  28),   S(-13,  43),   S( -3,  18),
            S( 35,  50),   S( 46,  45),   S( 35,  41),   S( 45,  35),   S( 77,  19),   S( 30,  48),   S(-17,  47),   S(  6,  44),
            S( 16,  48),   S( 19,  42),   S( 72,  38),   S(100,  28),   S( 85,  19),   S(104,  11),   S( 55,  32),   S(-50,  51),
            S( 53,  49),   S( 68,  34),   S( 99,  22),   S( 51,  32),   S(-23,  58),   S(-26,  37),   S(-43,  46),   S(-14,  55),
            S( -7,  46),   S( -2,  54),   S( 32,  48),   S( 23,  60),   S( 19,  60),   S(  6,  66),   S(-13,  46),   S(-13,  50),
            S(-21,  69),   S( 31,  47),   S( 33,  38),   S( 15,  59),   S(  7,  44),   S( 11,  63),   S( -5,  77),   S( -8,  15),

            /* bishops: bucket 6 */
            S(-39,  48),   S( 52,   9),   S(-57,  52),   S(-27,  46),   S(-36,  61),   S(-33,  54),   S(-31,  50),   S(-42,   4),
            S(-16,  35),   S(  4,  31),   S(  9,  42),   S(  3,  47),   S( 10,  34),   S( 21,  30),   S(-65,  38),   S(  7,   8),
            S(  8,  25),   S(  2,  36),   S( 61,  38),   S( 45,  31),   S( 74,  16),   S( 42,  34),   S(  2,  60),   S(-21,  47),
            S(-18,  55),   S( 35,  48),   S( 59,  27),   S( 86,  19),   S( 55,  26),   S( 50,  32),   S(-11,  58),   S(-47,  58),
            S(-12,  54),   S( 21,  37),   S(123,   5),   S( 65,  26),   S(106,  20),   S( 39,  41),   S( 43,  42),   S(-43,  52),
            S(-14,  39),   S( 34,  23),   S( 23,  46),   S( 40,  43),   S( 38,  43),   S( 93,  33),   S( 78,  34),   S(-29,  59),
            S(  1,  31),   S( 21,  40),   S( 41,  49),   S(  0,  58),   S( 48,  50),   S( 46,  48),   S( 13,  48),   S(-29,  73),
            S(-12,  65),   S( -5,  61),   S( -1,  58),   S( -6,  57),   S(  4,  54),   S(  3,  54),   S( 18,  67),   S(  5,  66),

            /* bishops: bucket 7 */
            S(-17,  18),   S(-34,  18),   S(-14, -12),   S(-49,  31),   S( -6,   8),   S(-72,  33),   S(-44, -12),   S(-45, -23),
            S(-20,   1),   S(-35,   9),   S(-27,  24),   S(-10,  22),   S(-29,  22),   S(-36,  16),   S( -4, -13),   S(-41, -13),
            S(-25,  20),   S(  0,  15),   S( 40,  13),   S( 32,  25),   S(  8,  20),   S( 19,  15),   S(-12,  27),   S(-61,  43),
            S(-32,  28),   S( 18,  37),   S( 75,  -6),   S(101,  -3),   S(109,  -2),   S( 76, -11),   S( 49,  21),   S(-33,  28),
            S(-27,  32),   S( 22,  -1),   S( 65, -12),   S( 95, -17),   S( 92,  -1),   S(115,   6),   S( 36,  17),   S( 49,  -1),
            S( -6,   7),   S(-29,  13),   S( 56, -11),   S( 57, -11),   S( 25,  11),   S( 72,  12),   S( 90,  10),   S( 19,   2),
            S(-30,  -3),   S(-41,  16),   S(-30,  34),   S( 14,  12),   S(  2,  23),   S( 52,  18),   S( 57,  10),   S(-36,  37),
            S(-17,  10),   S(-45,  34),   S(  1,  39),   S(-33,  30),   S( -3,  10),   S( 11,  42),   S( 34,  34),   S( 29,  30),

            /* bishops: bucket 8 */
            S(  4, -36),   S(-10, -71),   S(-52,  -1),   S( -1, -13),   S(  3, -14),   S(-16, -37),   S(  0, -36),   S( 24,  16),
            S(-26, -40),   S(-23, -86),   S(  2, -15),   S(  0, -23),   S( 37, -14),   S( -2, -24),   S( -6, -42),   S(  0, -17),
            S( -2, -24),   S(  1, -21),   S(-23, -13),   S( 37, -25),   S(  7, -10),   S( 22, -28),   S( -2, -33),   S(-48, -26),
            S( -9,  22),   S( -1,  11),   S( 11,  -5),   S( 37,   0),   S( 43, -14),   S( 19, -10),   S( 19,  -7),   S( 12,  -9),
            S( 11,  44),   S(  8,  15),   S( 45,  -4),   S( 76, -36),   S( 38, -50),   S( 31, -14),   S( -1, -43),   S(  6,   8),
            S(-26, -16),   S( 28,  33),   S( 16,   9),   S(  1, -17),   S( 14,  -8),   S(  8, -19),   S(  9, -55),   S( -1, -27),
            S(-11, -53),   S( 26, -12),   S( 21,  10),   S( 31,   7),   S( -2,  10),   S( 18, -19),   S( -2, -29),   S( -7, -41),
            S( -3, -18),   S(-15, -69),   S(  2, -15),   S( -4,  -3),   S( -1, -19),   S( -1, -25),   S(  2, -29),   S(-10, -44),

            /* bishops: bucket 9 */
            S(-21, -61),   S( 18, -46),   S(-44, -16),   S(-11, -33),   S(-26, -53),   S(-27, -38),   S(-24, -65),   S( -8, -14),
            S(-15, -35),   S( 13, -66),   S( 12, -27),   S(-10, -18),   S( -4, -19),   S(-16, -44),   S( 20, -28),   S( -9, -40),
            S(  6,   4),   S( 14, -10),   S( 65, -46),   S( 32, -46),   S( 55, -32),   S( 35, -12),   S(-12, -14),   S(-19, -31),
            S(-12,   7),   S( 28, -30),   S(  6, -15),   S( 97, -61),   S( 81, -30),   S( 31, -22),   S( 30, -21),   S(-14, -43),
            S(  9, -13),   S( 38, -21),   S( 44, -12),   S( 57, -16),   S( 46, -56),   S( 50, -38),   S( 24, -25),   S(  8, -14),
            S(  8, -23),   S( 49, -19),   S( 10,  22),   S( 48, -16),   S( 44, -33),   S( 41, -52),   S( 23, -41),   S(  4, -39),
            S( -3, -39),   S(  7,  26),   S(  5, -30),   S( 37, -11),   S( 14, -16),   S(  6, -64),   S(  7, -22),   S(-15, -62),
            S(  3, -14),   S( -6, -44),   S(  3, -24),   S(-11, -38),   S( -4, -13),   S(  8, -18),   S( -9, -69),   S(-14, -72),

            /* bishops: bucket 10 */
            S(-35, -73),   S(  1, -50),   S(-83, -42),   S( -5, -53),   S(-12, -16),   S(-27, -43),   S(-19, -61),   S(  0, -56),
            S( 20, -39),   S(-13, -37),   S( 21, -38),   S(-27, -39),   S(-25, -51),   S( -1, -58),   S(-21, -77),   S(-10, -26),
            S(  3, -17),   S( 11, -47),   S( 24, -45),   S( 67, -46),   S( 63, -56),   S( 47, -47),   S(-27, -12),   S(-23, -24),
            S(  7, -45),   S( 23, -44),   S( 80, -53),   S( 73, -53),   S( 88, -54),   S( 41, -39),   S( 15, -16),   S( 22,  21),
            S(-10, -34),   S( 14, -44),   S( 77, -60),   S( 99, -60),   S( 68, -41),   S( 52, -23),   S( 11,  -6),   S( -7, -12),
            S( -8, -48),   S( 40, -82),   S( 53, -50),   S( 39, -48),   S( 69, -19),   S( 46,   6),   S( 34, -26),   S(-17, -49),
            S(-22, -97),   S( 12, -51),   S( 33, -29),   S( 31, -35),   S( 15, -37),   S( 14, -42),   S(-13, -31),   S(  5, -17),
            S( -2, -37),   S( -2, -46),   S( -5, -33),   S( 16, -36),   S( -2, -40),   S(-10, -46),   S( 21,  25),   S(  0, -14),

            /* bishops: bucket 11 */
            S(-22, -26),   S(-32, -45),   S(-46, -46),   S( -9, -36),   S(-24, -10),   S(-84, -51),   S( -5, -27),   S(-25, -85),
            S( -2, -38),   S(-10, -54),   S(  1, -33),   S(-37, -28),   S(-27, -31),   S(-17, -57),   S(-11, -56),   S(-11, -46),
            S(-12, -28),   S( 29, -34),   S( 27, -32),   S( 48, -59),   S( 10, -40),   S( 19, -37),   S(-11, -11),   S(  9, -17),
            S(-15, -25),   S(  6, -32),   S( 87, -48),   S( 86, -72),   S( 92, -46),   S( 60,  -5),   S( 27, -35),   S( 25,  35),
            S( -9, -37),   S(  6, -43),   S(  9, -40),   S( 87, -68),   S( 76, -58),   S( 68, -29),   S(  7,   2),   S( -1, -11),
            S( -2, -61),   S( 18, -61),   S( 26, -59),   S( 21, -27),   S( 53, -46),   S( 50, -21),   S(-15,   1),   S(-18, -12),
            S( -5, -13),   S( 11, -60),   S( -5, -45),   S(  9, -51),   S( -1, -21),   S( 60,   3),   S( 24, -27),   S(  4, -18),
            S(-11, -81),   S(-20, -72),   S(  7, -49),   S( 15, -14),   S( 10,  -8),   S( -9, -34),   S(  2, -49),   S(  1, -28),

            /* bishops: bucket 12 */
            S(-11, -31),   S(-10, -41),   S(-20, -60),   S(  7,  -4),   S( -7, -11),   S(-11, -28),   S( -9, -14),   S(  2,   9),
            S( -5, -25),   S(-17, -66),   S(-17, -64),   S( -2,  -9),   S( -3, -52),   S( -3,   2),   S(  2,   5),   S(  4,  15),
            S( 10,  21),   S(-11, -41),   S( -2, -26),   S(-14, -41),   S(-12, -31),   S(  7, -23),   S(-16, -54),   S( -5, -13),
            S(  7,  16),   S( 15,  27),   S(  8,   8),   S(-10, -22),   S( -5, -17),   S(  9,   2),   S( -4, -37),   S(  2,   8),
            S(  6,   7),   S( 12,  11),   S( 14, -12),   S(  9, -31),   S( 15, -28),   S( -9, -15),   S( -3, -24),   S(  6,  11),
            S(-10,  -4),   S(  5,  23),   S(-10, -14),   S( -4, -15),   S( 13,   5),   S(  5,   4),   S(  2, -28),   S(  2,   9),
            S( -6, -10),   S(-19, -28),   S(  6,   2),   S(-15, -35),   S(  0, -10),   S( -3, -49),   S( -3, -20),   S( -1,  -1),
            S(  0,   6),   S(  1,  14),   S(-14, -30),   S( 11,  22),   S( -4, -39),   S(  6,   0),   S(-18, -41),   S( -4, -15),

            /* bishops: bucket 13 */
            S(-10, -68),   S( -5, -81),   S(-16, -43),   S(  0, -54),   S( -4, -19),   S(  2, -21),   S( -2, -24),   S( -6, -30),
            S(  1, -16),   S( -7, -48),   S( -7, -64),   S( -2, -35),   S( -2, -38),   S(  7, -13),   S(  6,   4),   S(  1, -13),
            S(-10, -59),   S( -5, -26),   S(  2, -44),   S(  2, -70),   S( -9, -74),   S(  6, -14),   S(  4,  -5),   S(  4,   2),
            S(  0,   7),   S( -3, -59),   S(  2, -24),   S( -3, -69),   S( 29, -31),   S( 14, -15),   S( -2, -43),   S( -7, -34),
            S(  0,   3),   S( 16,  -5),   S(  4, -44),   S( 30, -44),   S(  9, -65),   S( -4, -65),   S(  1, -28),   S( -5, -39),
            S( -6, -15),   S(  8,   9),   S(  7,  13),   S( 16,   5),   S(  3,  -3),   S( -9, -56),   S( 18, -22),   S( -3, -42),
            S( -3, -35),   S( -2, -48),   S( 11,   4),   S(  5,   9),   S(-13, -43),   S(  1, -36),   S( -8, -40),   S( -1, -26),
            S( -7, -33),   S(  0,  -1),   S( -3, -23),   S(  6,   4),   S(  0, -34),   S( -3, -32),   S( -2, -22),   S( -9, -49),

            /* bishops: bucket 14 */
            S(  1, -16),   S(-13, -72),   S( -4, -27),   S( -8, -20),   S( -8, -40),   S(  5, -12),   S( -6, -60),   S( -7, -50),
            S( -4, -46),   S(  3,  10),   S(  2, -44),   S(-14, -72),   S(  0, -37),   S(  0, -41),   S(-10, -69),   S(  0, -25),
            S( -8, -38),   S(-11, -47),   S(-24, -86),   S( 12, -42),   S( -7, -57),   S(-13, -75),   S( -3, -55),   S( 10,  16),
            S( -9, -33),   S(-13, -56),   S( 14, -22),   S( -8, -70),   S( 11, -80),   S( -4, -38),   S(  1, -12),   S(  0,   9),
            S(-12, -49),   S(  2, -35),   S(  5, -74),   S( 13, -44),   S( 12, -64),   S(  8, -52),   S( 17, -24),   S( -5, -49),
            S(  8, -14),   S( -3, -72),   S(  8, -58),   S( -3, -38),   S(  1, -22),   S( -2, -23),   S(  6, -33),   S( -1, -35),
            S( -4, -37),   S(  1, -96),   S( -2, -57),   S(  2, -10),   S(-14, -47),   S( 12,  19),   S( -7, -40),   S( -1,  -8),
            S( -7, -45),   S( -5, -11),   S( -4, -38),   S(  1, -33),   S( -6, -34),   S( -2, -32),   S( 13,  32),   S(  2,  -4),

            /* bishops: bucket 15 */
            S(  3,  20),   S( 16,  37),   S(-16, -41),   S(-17, -29),   S( -5, -15),   S(-18, -33),   S( -4, -30),   S( -7, -20),
            S( 10,  45),   S(  4,  -3),   S(  5, -29),   S( -3, -41),   S(-15, -19),   S(  3, -19),   S( -5, -25),   S(-12, -49),
            S( -4, -34),   S(  1, -19),   S(  2, -37),   S(  6, -13),   S( -9, -70),   S( -7, -25),   S(  2, -23),   S(  4,   3),
            S( -9, -40),   S(-15, -64),   S(  4, -13),   S(-13, -60),   S(  0, -62),   S( -8, -67),   S( 11,  19),   S(  0,  -9),
            S(  2,   1),   S( -9, -37),   S(-10, -57),   S(-20, -67),   S( 11, -26),   S( -4, -17),   S( 14, -18),   S(  0, -18),
            S(-10, -28),   S( -8, -73),   S(-19, -72),   S(-16, -37),   S( -1,   3),   S(  8,   7),   S( 20,  -2),   S( -9, -18),
            S(  0, -20),   S( -4, -34),   S(-10, -61),   S( -7, -44),   S(-14, -66),   S( -6, -19),   S(-18, -57),   S(-10,  26),
            S( -1, -18),   S( -6, -23),   S( -8, -28),   S( -1, -21),   S( -6, -36),   S(-15, -39),   S(-11, -44),   S(  2,   2),

            /* rooks: bucket 0 */
            S(-12,  -2),   S( 29,  -9),   S( 15, -31),   S( 12,  -9),   S( 18,  -4),   S( 16, -11),   S(  8,  24),   S( 22,  25),
            S( 20, -53),   S( 52, -35),   S( 16,  15),   S( 23, -14),   S( 26, -12),   S( 24, -25),   S(-25,  20),   S(-13,  24),
            S( 43, -41),   S( 45,   2),   S( 31,   5),   S( 29,  -3),   S( 18,   8),   S( 12,  -2),   S( -5,   5),   S(-34,   2),
            S( 39, -25),   S(105, -32),   S( 59,  13),   S( 53,  -4),   S( 38, -13),   S( 10,   9),   S( 19,  17),   S(-18,  29),
            S( 91, -41),   S(128, -27),   S( 57,   8),   S( 34,  -9),   S( 50,  12),   S(  3,  13),   S( -4,  33),   S(-15,  21),
            S(115, -42),   S(141, -33),   S( 68, -27),   S( 36,   8),   S( 39, -27),   S(-46,  36),   S( 52,  11),   S(  5,  13),
            S( 60,  -3),   S( 95,  -6),   S( 50,  19),   S( 21,  -7),   S( 40,  10),   S( 28,  -4),   S(-43,  50),   S( -8,  40),
            S( 62,  32),   S( 53,  44),   S( 19,  23),   S( 58, -28),   S( 53,  -5),   S( 86,  -1),   S(  0,  43),   S( 13,  35),

            /* rooks: bucket 1 */
            S(-61,  36),   S(-44,  35),   S(-41,   6),   S(-40,   2),   S(-20,  -7),   S(-25,   2),   S(-18,   1),   S(-35,  48),
            S(-52,  25),   S(-71,  34),   S(-14,   8),   S(-18, -21),   S(-42,  10),   S(-14,  -5),   S(-48,   7),   S(-46,  12),
            S( -6,  33),   S(-20,  34),   S(-14,  27),   S(-48,  46),   S(-26,  22),   S(-17,  24),   S(-22,  21),   S(-53,  32),
            S(-41,  59),   S(-22,  39),   S( 14,  36),   S(-16,  36),   S(-55,  47),   S(-33,  49),   S(-30,  46),   S(-35,  40),
            S( 51,  18),   S( 18,  47),   S( 37,  20),   S(-25,  47),   S(  5,  40),   S(  5,  27),   S(-13,  51),   S(-23,  33),
            S( 78,  12),   S( 11,  40),   S( 38,  36),   S(-44,  54),   S( 12,  21),   S(-26,  51),   S( 24,  30),   S(-56,  47),
            S(-13,  53),   S( 21,  48),   S( 52,  26),   S(-60,  74),   S(-17,  50),   S( 44,  22),   S(-40,  49),   S(-43,  60),
            S( 99,  34),   S( 21,  53),   S( -6,  54),   S(-45,  63),   S(  6,  46),   S( 40,  27),   S( 46,  52),   S(-28,  43),

            /* rooks: bucket 2 */
            S(-84,  83),   S(-48,  58),   S(-57,  37),   S(-65,  43),   S(-70,  41),   S(-66,  44),   S(-52,  25),   S(-44,  31),
            S(-75,  68),   S(-69,  68),   S(-44,  44),   S(-63,  43),   S(-51,  34),   S(-49,  19),   S(-83,  47),   S(-66,  42),
            S(-70,  78),   S(-52,  59),   S(-45,  62),   S(-59,  53),   S(-76,  63),   S(-38,  64),   S(-21,  46),   S(-41,  39),
            S(-66,  86),   S(-55,  94),   S(-47,  80),   S(-27,  60),   S(-57,  65),   S(-30,  73),   S(-46,  82),   S(-14,  53),
            S(-37,  81),   S(-33,  82),   S(-48,  80),   S(-27,  63),   S(  9,  61),   S( -8,  63),   S(-26,  69),   S(-50,  72),
            S(-27,  76),   S(-33,  80),   S(-32,  73),   S(-30,  57),   S( 23,  52),   S( 71,  32),   S( 21,  46),   S(-53,  79),
            S(-46,  76),   S(-81, 104),   S(-55,  84),   S(-27,  63),   S(-10,  67),   S( -4,  57),   S(-60,  95),   S(-34,  74),
            S(-17,  95),   S(-39,  94),   S(-28,  77),   S(-32,  71),   S(-66,  93),   S( 22,  77),   S(-54, 111),   S( 29,  64),

            /* rooks: bucket 3 */
            S(  2,  98),   S(  2,  94),   S(  2,  73),   S( 16,  63),   S( 11,  62),   S(-10,  81),   S(  9,  84),   S(-12,  80),
            S(-20, 103),   S( -7,  97),   S(  2,  83),   S(  8,  75),   S( 23,  67),   S( 13,  72),   S( 34,  41),   S( 45, -20),
            S(-28,  99),   S(-19,  96),   S( -2,  92),   S( 16,  77),   S( 20,  70),   S( 34,  73),   S( 38,  85),   S(  8,  62),
            S(-12, 106),   S( -7, 111),   S( 18,  89),   S( 23,  83),   S( 20,  80),   S( 16, 109),   S( 67,  85),   S( 18,  95),
            S( -3, 119),   S( 23, 106),   S(  6,  94),   S( 30,  81),   S( 34,  90),   S( 48,  78),   S( 86,  72),   S( 53,  65),
            S( -4, 123),   S( 23,  98),   S( 11,  92),   S( 22,  83),   S( 23,  82),   S( 60,  61),   S(103,  46),   S( 97,  47),
            S( -9, 119),   S( -3, 124),   S( -2, 105),   S( 20,  91),   S( 21,  90),   S( 29,  91),   S( 74,  85),   S(111,  50),
            S(-59, 179),   S( 25, 119),   S( 13, 104),   S( 55,  81),   S( 57,  77),   S( 73,  77),   S(100,  89),   S(122,  65),

            /* rooks: bucket 4 */
            S(-67,  10),   S( -1, -14),   S(-22, -21),   S(  9,  -9),   S(-28, -18),   S(-31,  -3),   S(  4,  -9),   S(-26,   0),
            S(-53,  32),   S(-29,  11),   S( -4,  -2),   S(  4,   0),   S( 21, -14),   S( 16, -14),   S( 26, -44),   S(-27,  -1),
            S(-34,  30),   S(-20, -21),   S(-13,  14),   S(-32,   5),   S(-32,   1),   S( 35, -32),   S( 16, -37),   S(-32,  10),
            S(-27,  11),   S(-32,  31),   S(-29,  11),   S( -3,  10),   S( 20, -10),   S(-20,   5),   S(-11,  13),   S(-31,  23),
            S( -6,  10),   S(-12,  38),   S(-21,  19),   S( 39,  15),   S( 33,  -9),   S(-22,   6),   S( 34,  -9),   S( 38,   5),
            S(  3,  10),   S( 19,   3),   S( 63, -13),   S( 46,  -2),   S( 44,  -5),   S( 50,   4),   S(  7,  33),   S( -7,  28),
            S( 16,   9),   S(  4,  57),   S( -5,  21),   S( 51,  -4),   S( 71, -16),   S( 15,  -9),   S( 50,  11),   S( -2,  23),
            S( 52, -16),   S( 39,  59),   S( 47,  17),   S( 22,  -1),   S( 14,  -2),   S( 36,  -2),   S( 17,  10),   S( -1,  31),

            /* rooks: bucket 5 */
            S(-32,  50),   S(-42,  69),   S(-35,  36),   S(-62,  52),   S(-11,  19),   S(-15,  31),   S( 30,  22),   S( 11,  26),
            S(-15,  30),   S(-18,  53),   S(-64,  68),   S(-65,  63),   S(-72,  51),   S(-50,  45),   S( 19,   6),   S(-41,  32),
            S(-48,  60),   S(-53,  58),   S(-30,  70),   S(-79,  69),   S(-58,  46),   S(-36,  46),   S(-40,  53),   S(  2,  14),
            S(-44,  67),   S(-20,  59),   S(-80,  74),   S(-69,  75),   S(-52,  57),   S(-29,  73),   S(-47,  63),   S( 18,  49),
            S(  6,  60),   S(-13,  74),   S( 30,  55),   S(  8,  68),   S( -1,  68),   S( 52,  53),   S( 69,  43),   S( 24,  36),
            S( 81,  60),   S( 68,  63),   S( 60,  61),   S( 46,  69),   S( 34,  58),   S( 57,  58),   S( 33,  60),   S( 72,  56),
            S( 30,  64),   S( 69,  50),   S(100,  28),   S( 40,  57),   S( 48,  38),   S( 56,  35),   S(119,  29),   S( 59,  56),
            S( 46,  54),   S( 81,  49),   S( 55,  59),   S( 52,  41),   S( 61,  43),   S( 68,  46),   S( 58,  54),   S( 34,  60),

            /* rooks: bucket 6 */
            S(-32,  35),   S( -3,  35),   S(  6,  15),   S( -5,  19),   S(-47,  41),   S(-36,  49),   S(-12,  58),   S(  3,  50),
            S(-31,  48),   S(-33,  62),   S(-38,  51),   S(-17,  26),   S(-59,  60),   S(-73,  77),   S(-43,  64),   S( -5,  38),
            S(-54,  73),   S(-62,  71),   S(-45,  59),   S(-69,  57),   S(-44,  49),   S(-86,  87),   S(-38,  64),   S(-47,  63),
            S(-59,  85),   S(  1,  75),   S(-13,  71),   S(-30,  58),   S(-44,  62),   S(-21,  71),   S(-61,  83),   S(-28,  67),
            S(-13,  88),   S( 10,  76),   S( 36,  48),   S( 34,  44),   S(-21,  83),   S(  4,  67),   S( 27,  51),   S( -1,  62),
            S( 27,  73),   S( 84,  54),   S( 90,  38),   S( 71,  24),   S( 34,  60),   S( 38,  65),   S( 61,  56),   S( 92,  47),
            S( 71,  64),   S( 79,  55),   S( 78,  32),   S( 98,  15),   S( 91,  35),   S( 53,  59),   S(115,  38),   S( 42,  58),
            S( 96,  70),   S( 67,  62),   S( 86,  37),   S( 54,  41),   S( 74,  52),   S( 82,  54),   S( 82,  63),   S( 74,  55),

            /* rooks: bucket 7 */
            S(-52,  -4),   S(-20,   7),   S(-33,  -8),   S( -6,  -8),   S( 11, -16),   S(-34,  17),   S(-48,  29),   S( 23, -15),
            S(-42,  23),   S(-34,  16),   S(  6, -17),   S(-35,  10),   S(-17,   9),   S( -6,  21),   S(-41,  32),   S(-16,  11),
            S(-86,  61),   S(-104,  52),  S(-36,  23),   S(-21,   9),   S(-20,   9),   S(-32,  23),   S(-35,  -1),   S(-31,  18),
            S(-90,  61),   S( -6,  27),   S(-27,  35),   S( 18,  13),   S( 38,   4),   S(-11,  28),   S( 18,  11),   S( 29,  12),
            S(-21,  48),   S(-39,  45),   S( 35,   7),   S( 60,   5),   S( 80,  -3),   S(108, -10),   S( 82,  15),   S( 53,  -4),
            S(-17,  52),   S(-13,  37),   S( 91, -23),   S(116, -32),   S( 95,  -5),   S( 69,   2),   S( 78,  20),   S(  2,  17),
            S( 12,  47),   S( 48,  29),   S( 82, -15),   S(100, -21),   S(103, -13),   S( 95,   1),   S( 66,  33),   S( 54,  14),
            S( 18,  68),   S( 40,  31),   S( 43,   8),   S( 93, -13),   S( 97,  -9),   S( 41,  26),   S( 57,  22),   S( 92,   1),

            /* rooks: bucket 8 */
            S(-62, -34),   S(-35,   4),   S( -4,  12),   S(-24,  -7),   S(-53, -26),   S(-19, -41),   S(-15, -26),   S(-65,   6),
            S(-13,   3),   S(-14,  -4),   S( -1,   1),   S(-41,  -4),   S(-26, -10),   S( -4, -34),   S(  3,  -2),   S( 10, -12),
            S( 10, -19),   S(-15,  -6),   S(-11,  21),   S(  1,   1),   S(-27, -38),   S(  2, -17),   S(-27, -16),   S( -7, -16),
            S(-23, -12),   S(-15,  57),   S(-11,   1),   S( 10,   6),   S( -5,  11),   S( -5, -25),   S( -6, -12),   S( -6,   0),
            S( -6,  -9),   S(-15,   1),   S(-21,  34),   S(  4,   6),   S( 13,  -4),   S( 10,  -8),   S(-10,   3),   S( -1,   1),
            S(-11,  -5),   S(  3,  25),   S(  6,  19),   S(  2,  -2),   S( 11,  16),   S( -6,  -1),   S( 10,  23),   S( -1,  21),
            S(-11,  -5),   S(-23,  -1),   S(  5,   1),   S( 19, -10),   S( 20, -13),   S( 18, -15),   S(  4,  -4),   S( 29, -14),
            S( 19, -113),  S( 17, -15),   S( 16,   1),   S( 14,  16),   S(-13, -23),   S( 14, -31),   S( 16,   6),   S( 25,  28),

            /* rooks: bucket 9 */
            S(-45, -52),   S(-33, -23),   S(-24, -43),   S(-66, -25),   S(-72, -31),   S(-33, -31),   S(-32, -52),   S(-78, -36),
            S(  9, -32),   S(-16, -46),   S(-30, -18),   S(-47, -19),   S(-50, -38),   S( 28, -24),   S( -7, -24),   S(-13, -35),
            S(-17, -44),   S( 16, -46),   S(-22, -29),   S(-14, -30),   S(-31, -50),   S( -6, -41),   S(-16, -41),   S(  0, -51),
            S(-21, -45),   S(  4, -28),   S( -4,  -3),   S(-22, -12),   S(-14, -52),   S(  0, -22),   S(  7,  -8),   S( 13, -35),
            S( -3,  -9),   S( 10,  -5),   S( -3, -15),   S(-10,   0),   S(-12, -19),   S( 24,   1),   S( 18,   5),   S(  7, -32),
            S( -5,   6),   S(-20,   1),   S(  9,  -1),   S(-13, -22),   S(  8, -15),   S( 30,   5),   S( 32,   3),   S(  5, -12),
            S( 37, -14),   S( 36, -20),   S( 39, -21),   S( 54, -31),   S( 12, -58),   S( 40, -36),   S( 33, -36),   S( 63, -39),
            S( 79, -84),   S( 37, -53),   S( 31, -32),   S( 25,  11),   S(  7, -10),   S( 24, -21),   S( 21, -12),   S(  5, -12),

            /* rooks: bucket 10 */
            S(-79, -84),   S(-34, -53),   S(-37, -71),   S(-51, -48),   S(-37, -41),   S(-44, -64),   S( 14, -47),   S( -6, -55),
            S( -4, -46),   S( -4, -46),   S(-34, -48),   S(-33, -31),   S(-31, -26),   S(-27, -31),   S(  8, -17),   S(  4, -43),
            S(-42, -40),   S(-25, -49),   S(-30, -43),   S(-33, -36),   S(-18, -29),   S(-34, -33),   S(  3, -15),   S(-14, -36),
            S( -8, -32),   S(-25, -38),   S(-34, -34),   S(-33, -50),   S(-28, -41),   S(  9, -21),   S( -4,  -5),   S(-17, -34),
            S( -9, -22),   S(  7, -24),   S( 12, -39),   S(  4, -44),   S(-19, -36),   S(  7,   5),   S( 19,  -6),   S( 15, -19),
            S( 39, -11),   S( 24, -17),   S( 31, -31),   S( 14, -34),   S(  6, -42),   S( 26,  -9),   S( 20, -21),   S(  5, -37),
            S( 90, -51),   S( 94, -48),   S( 66, -57),   S( 73, -62),   S( 45, -52),   S( 39, -35),   S( 47, -53),   S( 50, -56),
            S( 62, -18),   S( 17, -20),   S( 44, -37),   S( 18, -40),   S( 36, -30),   S( 28, -14),   S( 16, -25),   S( 19, -31),

            /* rooks: bucket 11 */
            S(-109, -28),  S(-46, -28),   S(-34, -23),   S(-50, -56),   S(-44, -30),   S(-41,  -8),   S(-56, -16),   S(-78,  -9),
            S(-48, -17),   S(  9, -13),   S(-15, -20),   S(-20, -21),   S(-29, -30),   S(-46,   7),   S( -4,   6),   S(-42, -25),
            S(  5,  -7),   S(-17, -18),   S(-26, -29),   S( 22, -30),   S( -6, -13),   S(-30, -10),   S(-33, -58),   S(  6, -19),
            S(-19,  25),   S( -2,  -5),   S( -6, -24),   S(  3, -24),   S( 15,   8),   S(-13,  13),   S( 24, -14),   S(-12,  -9),
            S( -8,  -7),   S( 10, -21),   S( 26, -10),   S( 27, -34),   S(  8,  -4),   S( 17,  -7),   S(  6, -22),   S(-17, -18),
            S(  1,  31),   S( 41,  35),   S( 22, -19),   S( 72,  -4),   S( 55,  -7),   S( 45,   8),   S( -7,  22),   S( -1,   4),
            S( 40,  27),   S( 34,  10),   S( 85, -31),   S( 83, -49),   S( 39, -24),   S( 44,  -6),   S( 18,  25),   S( 49,  -4),
            S( 45,  20),   S( 20,   4),   S( 29, -20),   S( 27,  -8),   S( 16,  -9),   S( 20, -20),   S( 12,  25),   S( 26,  18),

            /* rooks: bucket 12 */
            S( -2, -31),   S(-10, -54),   S(  8,   8),   S(-11, -11),   S(-10, -71),   S(-12, -48),   S(-16, -54),   S(-31, -45),
            S(  2, -18),   S( -4, -23),   S(-19, -34),   S(  8, -12),   S( -7, -25),   S( -1, -21),   S( -1, -11),   S( -3, -26),
            S( 11, -17),   S(  5, -14),   S(-23, -39),   S(  9, -16),   S(-13, -31),   S( -1,   8),   S( -6, -26),   S(-19, -46),
            S(-17,  -6),   S( -7, -17),   S( -1, -39),   S( 18,  23),   S( -5, -29),   S(-10, -36),   S( -7, -37),   S(  4, -14),
            S( -3, -25),   S(-10, -31),   S(  6, -16),   S( 11,  -3),   S(-19, -59),   S( 16,  12),   S(-12, -49),   S( -7, -23),
            S(  7,  11),   S(-14, -48),   S( 19, -17),   S( 31,  12),   S(-16, -37),   S(  2, -41),   S( -7, -43),   S( -2,   0),
            S( -1, -17),   S(  1, -10),   S( 13, -33),   S( 26,  24),   S( -7, -52),   S(  0, -40),   S(  2, -24),   S(  7,  16),
            S( -9, -44),   S(  2,   1),   S( -4, -44),   S(  9, -27),   S( -9, -44),   S(-14, -58),   S( -2, -48),   S( 11,  18),

            /* rooks: bucket 13 */
            S(-11, -17),   S( -9, -33),   S(  0,   5),   S(-22,  12),   S( -5,  -7),   S(  7, -12),   S( 18,   1),   S(-10, -34),
            S( -5, -18),   S( -3,  -9),   S(-29, -16),   S( -8,  -3),   S(-12,  -6),   S(  2, -10),   S( -1, -10),   S( -3, -24),
            S(-25, -49),   S(  1,  -5),   S(-14, -37),   S( -9, -29),   S(-17, -42),   S(  2, -30),   S( -4, -28),   S(  9, -10),
            S( -9, -49),   S(-11, -59),   S(-17, -35),   S(-13, -14),   S( 13,   4),   S(-12, -16),   S(  1, -31),   S(-16, -45),
            S(  2, -25),   S(-15, -57),   S( -8, -58),   S( -8, -49),   S(  4,  -9),   S( -6, -70),   S(  9, -12),   S(  1,   1),
            S(-13, -20),   S( 11,   7),   S(  2,  -6),   S( -7, -47),   S(  1, -36),   S(  9,   9),   S(  8,  -8),   S(  5,   4),
            S( -4,  -1),   S( -3, -10),   S(  4, -28),   S(  6,  -7),   S(  8, -27),   S(  0, -29),   S( 11,  -1),   S( 15,  23),
            S(-15, -129),  S(-15, -66),   S(  1,  16),   S(  3,   1),   S(-13, -30),   S(-24, -73),   S( -7, -38),   S( -8,  -4),

            /* rooks: bucket 14 */
            S(-22, -16),   S(-19, -43),   S(-10, -44),   S(-12, -32),   S(-12,  -3),   S( -9, -20),   S( 12,  -7),   S( -8,  -9),
            S(-39, -79),   S( -4, -36),   S(  2, -17),   S( -7, -34),   S( -7,  -3),   S( -7, -34),   S( -8, -26),   S( -3,  -3),
            S(-18, -67),   S(-23, -58),   S(-19, -84),   S(-21, -62),   S(-18, -12),   S(-22, -45),   S( 12,  20),   S( -9, -32),
            S( -9, -25),   S( -6, -42),   S( -3, -47),   S(-29, -52),   S(-11, -69),   S( -4, -42),   S(  3, -28),   S(-20, -24),
            S(-10, -30),   S(  7, -51),   S(  0, -82),   S(-16, -75),   S( -6, -91),   S(  3, -31),   S( 13, -31),   S(  9,  -4),
            S( -7, -48),   S(  1, -36),   S( 12, -43),   S( -8, -99),   S(  0, -93),   S( -7, -85),   S( 10, -38),   S(  6,  -7),
            S(  1, -42),   S( -3, -49),   S( -6, -72),   S(  4, -102),  S( -8, -82),   S( 11, -23),   S( 10,  -2),   S(-11, -18),
            S( -8, -46),   S( -4, -12),   S( 15, -45),   S( -7, -34),   S(  2, -10),   S( -5,  -3),   S(  1, -22),   S(  0,  -4),

            /* rooks: bucket 15 */
            S(  2, -20),   S(-19, -54),   S( 14, -20),   S(-32, -88),   S(  1,  -9),   S(-10,  -8),   S(-12, -48),   S( -2, -18),
            S(-10, -37),   S(-21, -71),   S( -6, -54),   S(-22, -62),   S(-35, -74),   S( -9,  -8),   S( -3, -11),   S( -7,   7),
            S(  0, -18),   S( -2, -16),   S( -4, -56),   S( -5, -56),   S(  6, -41),   S( -1, -19),   S(  9,  15),   S(-13, -35),
            S( -8, -52),   S( -6, -40),   S(-17, -45),   S(  7, -31),   S( -4, -31),   S( -2, -53),   S(-11, -77),   S(  0, -14),
            S(  6, -15),   S( -7, -37),   S(  4, -47),   S( -7, -77),   S( -2, -62),   S(  2, -67),   S( 15, -22),   S( -3,  -6),
            S( 11,   6),   S( 12,  -2),   S( -3, -55),   S(  8, -84),   S(-11, -85),   S( 30, -37),   S( 26,   5),   S( -5,  -6),
            S( 13,  18),   S( 16,  11),   S(  8, -20),   S(  7, -71),   S(  6, -39),   S(  9,   4),   S( 22,  -5),   S(  2,  20),
            S( -5, -14),   S( -4, -24),   S( 11, -23),   S( -6, -50),   S( -3, -54),   S(  9, -26),   S(  3,  -5),   S( -8, -31),

            /* queens: bucket 0 */
            S(-79, -41),   S(-32, -61),   S( 51, -83),   S( 46, -69),   S( 42, -46),   S( 30, -11),   S( 51,  -2),   S( 30,   8),
            S(-31, -33),   S( 48, -71),   S( 51, -69),   S( 35, -25),   S( 35,  16),   S( 41,  -8),   S( 24,  22),   S( 38,  40),
            S( 23, -26),   S( 43,   1),   S( 11,  54),   S( 28, -15),   S( 23, -21),   S( 35, -13),   S(  9,  16),   S( 37,  27),
            S( 11,  39),   S( 32,  37),   S(  0,  45),   S(  1,  36),   S( 18,  -1),   S( 13,   2),   S( 43,  -3),   S( 10,  29),
            S( 36,  21),   S( 14,  52),   S(  9,  39),   S( 17,  18),   S(-29,  31),   S(  0, -15),   S( 42,  -9),   S( 45,  -3),
            S( 12,  65),   S(  6,  79),   S( 15,  51),   S( 31,   5),   S( 30,  24),   S( -2,  13),   S( 25,  -2),   S( 36,  -7),
            S( 41,  42),   S( 43,  25),   S(  3,  50),   S( 61,   5),   S( 48,  25),   S( -1,  38),   S( 45,  20),   S( 25,  27),
            S( 54,  26),   S( 48,  24),   S( 44,  30),   S( 30,  24),   S( 28, -14),   S( 18,  12),   S( 54,  29),   S( 53,   4),

            /* queens: bucket 1 */
            S(-31, -21),   S(-19, -52),   S(-65, -38),   S( -7, -78),   S(  6, -24),   S(  6, -68),   S( 49, -24),   S( 15,  36),
            S(-12, -33),   S(-20, -40),   S( 12, -17),   S( 10,  29),   S( 15, -15),   S( 22, -14),   S( 35,  -5),   S(  3,  57),
            S(-22,  29),   S( 22, -24),   S( 28,   0),   S(  3,  17),   S( 21,  -6),   S(  0,  26),   S( 20,  17),   S( 13,  34),
            S( 35, -26),   S( -9,  28),   S(-17,  44),   S( 30,  25),   S( 10,  14),   S(  7,  51),   S(  0,   2),   S( 35,  20),
            S( 21,  36),   S( 28,  24),   S(  5,  43),   S(-34,  47),   S(-10,  63),   S( 17,  12),   S( 18,  24),   S( 13,  47),
            S(  9,  14),   S( 26,  56),   S( 34,  47),   S(-11,  66),   S(  8,   3),   S(-23,  40),   S( -1,  38),   S( 34,  51),
            S(  2,  36),   S(  0,  67),   S(-12,  32),   S(-11,  50),   S(-61,  83),   S( 32,  22),   S(  3,  55),   S(-16,  66),
            S(-13,  26),   S( 42,  82),   S( 22,  26),   S( 42,  23),   S( 23,  55),   S( 37,  18),   S( 27,  29),   S( 38,   7),

            /* queens: bucket 2 */
            S( 18, -16),   S( 32,  -1),   S( 17, -36),   S( -5, -21),   S(-25,  16),   S( -9,  -3),   S(-23, -39),   S(  9,  -7),
            S( 38,  10),   S( 28,  18),   S( 27,  -8),   S( 31, -25),   S( 27, -19),   S( 23, -15),   S( 39, -34),   S( 62, -17),
            S( 25,  10),   S( 25,  16),   S( 33,   5),   S( 13,  26),   S( 15,  41),   S( 14,  62),   S( 10,  44),   S( 20,  50),
            S( 28,  18),   S( 15,  21),   S( -4,  42),   S( 10,  52),   S( -6,  67),   S(  9,  81),   S(  9,  41),   S( 13,  25),
            S( 20,  13),   S( -1,  71),   S(-13,  52),   S(-37,  92),   S(-31,  86),   S(-18, 109),   S( -5,  90),   S(  1,  98),
            S( 11,  27),   S( 13,  59),   S(-25,  56),   S( -8,  71),   S(-25,  89),   S(-52, 125),   S(  5,  89),   S( 16,  79),
            S(-14,  87),   S(-38, 112),   S(-24,  67),   S(  5,  60),   S(-19,  92),   S( 20,  81),   S(-42,  80),   S(-11,  99),
            S(-68, 125),   S( 24,  52),   S( 38,  51),   S( 25,  57),   S( 24,  56),   S( 38,  39),   S(  7,  59),   S(-32,  55),

            /* queens: bucket 3 */
            S( 65, 130),   S( 65, 122),   S( 53, 103),   S( 43,  90),   S( 65,  52),   S( 51,  29),   S( 19,  34),   S( 11,  88),
            S( 80, 132),   S( 57, 137),   S( 44, 122),   S( 46, 106),   S( 49,  94),   S( 63,  64),   S( 76,  10),   S( 33,  50),
            S( 43, 130),   S( 51, 138),   S( 62,  83),   S( 48,  86),   S( 52,  93),   S( 44, 128),   S( 53, 110),   S( 53,  70),
            S( 51, 144),   S( 59,  90),   S( 44,  96),   S( 35,  99),   S( 33, 104),   S( 42, 136),   S( 47, 132),   S( 36, 171),
            S( 55, 134),   S( 53, 113),   S( 42, 113),   S( 18, 112),   S( 15, 124),   S( 12, 152),   S( 34, 170),   S( 42, 177),
            S( 31, 149),   S( 74, 116),   S( 45, 103),   S( 16, 134),   S(  2, 154),   S( 47, 134),   S( 37, 179),   S( 19, 223),
            S( 73, 137),   S( 53, 146),   S( 74,  91),   S( 48, 117),   S( 46, 126),   S( 53, 140),   S( 84, 154),   S(135, 110),
            S(102,  97),   S( 93, 109),   S( 98,  83),   S( 84, 113),   S(105,  73),   S(101,  99),   S(141, 100),   S(107, 123),

            /* queens: bucket 4 */
            S(  1, -63),   S(-13, -41),   S(  2,  -8),   S(  5,  -5),   S( 17, -49),   S( 50, -22),   S(-83,  -2),   S(-51,   8),
            S(-22,   1),   S(-31,   9),   S( 27, -57),   S(-43,  41),   S(  8, -38),   S(-18,  -8),   S(-24, -16),   S(-88, -38),
            S( 12,   8),   S(-22, -11),   S(-18,  37),   S( -3,  20),   S( 21,   5),   S( 44, -21),   S( 17, -22),   S( -8,   0),
            S(-39, -13),   S( 12,  35),   S( 13,  34),   S(  5,   2),   S( 10, -27),   S( 41,   1),   S( 34,   4),   S(-36,  12),
            S(-25,  35),   S(-23,  33),   S( 17,  -4),   S( 10,  -1),   S(  1,  -5),   S( 18,  -9),   S( -2, -12),   S(-29, -26),
            S(  1,  10),   S( 66,  41),   S(-14,  33),   S( -3,   2),   S( 10,   6),   S( 11,   3),   S( -3,  -9),   S(-29, -27),
            S(-24, -25),   S( 14,   0),   S(  5,  39),   S( -5,  17),   S( -1,   7),   S(  4,  15),   S( 27, -31),   S(-28, -26),
            S( -6, -19),   S(  9,  18),   S( 31,  26),   S(  8,  22),   S(  9,   9),   S(-21, -34),   S(-15, -25),   S( -4,  -8),

            /* queens: bucket 5 */
            S(-22, -23),   S(-55, -43),   S(-45, -43),   S(-48, -24),   S(-63, -27),   S(-22,   1),   S( 53,  33),   S(  0,   6),
            S(-54, -32),   S(-34,   5),   S(-55, -17),   S(-60,  16),   S(-11,   5),   S(-29, -11),   S(-28,  19),   S(-25,  10),
            S(-68,  -4),   S(-67,  -7),   S(-71,  34),   S(-47,  39),   S( -3,  40),   S(-27,  31),   S(-18, -21),   S( 12,  -7),
            S(-43, -10),   S(-58,   0),   S(  6,  63),   S(-28,  57),   S(-21,  43),   S(  9,  22),   S( 24,  25),   S(-11,  32),
            S(-50, -15),   S(-11,   6),   S( -3,  84),   S( -6,  57),   S( 23,  30),   S(  8,  24),   S(-20,   9),   S(-22,  21),
            S(-45,  13),   S(  7,  46),   S(-34,  50),   S(  2,  55),   S( 48,  36),   S( 29,  43),   S(-15,  -2),   S( -5,   1),
            S(-11,  14),   S(-20,  43),   S(-25,  48),   S( 25,  38),   S( 39,  68),   S( 30,  60),   S(  4,   7),   S(  7,   7),
            S(-24,  23),   S(  0,  43),   S( 37,  63),   S(  5,  46),   S( 41,  38),   S( 11,  22),   S(  6,   2),   S( -9, -30),

            /* queens: bucket 6 */
            S(-48,  -3),   S(-57,   6),   S(-49,  -9),   S(-37, -26),   S(-77,   9),   S(-70, -14),   S(-57, -30),   S(-19,  -5),
            S(-21,  30),   S(-48,  28),   S(-20,  -4),   S(-40,  30),   S(-75,  49),   S(-86,  24),   S(-43,  -8),   S( -3,  12),
            S(-15,  39),   S(-52,  24),   S(-61,  47),   S(-104,  90),  S(-50,  86),   S(-29,  17),   S(-51,  -8),   S(-17,   2),
            S(-46,  24),   S(-27,  35),   S(-43,  71),   S(-45,  74),   S(  7,  51),   S( 12,  66),   S(  5,  56),   S(  6,  35),
            S(-94,  47),   S(-26,  32),   S(-10,  42),   S( 26,   9),   S( 46,  58),   S( 30,  70),   S(  9,  51),   S( 32,  47),
            S(-27,  71),   S( 14,   8),   S( 20,  15),   S( -1,  35),   S( -4,  59),   S( 72,  59),   S( 19,  51),   S(-17,   8),
            S(-32,  40),   S(-25,  52),   S(  4,  52),   S( 29,  43),   S( 38,  41),   S( 21,  69),   S( -7,   2),   S(-32,  29),
            S( -2,  49),   S(  9,  25),   S( 14,  28),   S( 36,  51),   S( 39,  64),   S(  9,  56),   S(  4,  22),   S( 26,  19),

            /* queens: bucket 7 */
            S(-58,  12),   S(-35,  32),   S(-27,   0),   S(-54,  39),   S( -6, -16),   S(-38,  -6),   S(-29, -23),   S( -6, -35),
            S( -7,  -5),   S(-78,  19),   S(-59,  35),   S(-11,  -1),   S( -8,  15),   S(-33,  30),   S(-71,  70),   S(-67,  -2),
            S(-28,  -9),   S(-80,  40),   S(-20,  13),   S(-11,  -6),   S( 31,   2),   S( -4,  35),   S(-26,  14),   S(-10,   8),
            S(-86,  32),   S(  8,   6),   S( -2, -17),   S( 65, -62),   S( 15,  -1),   S( 10,  48),   S( 10,  36),   S(-24,  42),
            S(-33,  30),   S(-51,  28),   S( -5, -16),   S( 76, -55),   S( 49, -34),   S( 91,   2),   S(  7,  47),   S( 28,   8),
            S(-21,  26),   S(-46,  19),   S( 13, -25),   S( 26, -21),   S( 31,  -8),   S( 98, -20),   S( 47,   5),   S( -5,  48),
            S( -4,  12),   S( 10,  -2),   S( 38, -19),   S(  7,  12),   S( 67,  -1),   S( 67,  35),   S( 12,  22),   S( 18,  32),
            S(  7,  12),   S( 17,   9),   S(  6,  34),   S( 47,  25),   S( 23,  25),   S( 35,  26),   S( 47,  13),   S( 49,  -3),

            /* queens: bucket 8 */
            S( -9,  -1),   S(-10, -17),   S(-27,  -3),   S(-33, -39),   S( -6, -16),   S(-18, -33),   S(-17, -33),   S( -4,  -3),
            S(-26, -37),   S(-16, -22),   S( -4,  13),   S(-41, -43),   S(-27, -28),   S(-23, -39),   S(-12, -33),   S(  1,   2),
            S( -4,  -6),   S(-22, -26),   S(-30, -44),   S(-15, -31),   S( -3,   2),   S( -4,  -7),   S(-16, -38),   S(-17, -22),
            S(-20, -38),   S(  4,  -6),   S(-17,  -6),   S(-12,   7),   S(-18, -24),   S(-17, -34),   S(-11, -19),   S(-13, -28),
            S( 11,  20),   S(-17,  16),   S( 18,  43),   S(-10, -11),   S(  6, -13),   S(-26, -42),   S( -4,  -8),   S(-16, -26),
            S( 16,  30),   S(  0,  20),   S(  5,  28),   S( 10,  17),   S(-14, -34),   S(  2,   3),   S( -5,  -6),   S(  1,   8),
            S(  1,   9),   S( -8,  -3),   S( 14,  18),   S(  3,   5),   S( -1, -23),   S(-23, -36),   S(-22, -40),   S(-16, -33),
            S( -5, -45),   S(  0,  -4),   S(-31, -59),   S(  0,   4),   S(  3,  -2),   S(  9,  10),   S(-18, -43),   S( -8, -13),

            /* queens: bucket 9 */
            S( -7,  -6),   S(-22, -54),   S(-12, -11),   S(-45, -49),   S(-20, -32),   S(-22, -44),   S(-19, -32),   S(-24, -44),
            S(-11, -22),   S(-30, -52),   S(-21, -14),   S(-51, -59),   S(-33, -39),   S(-21, -32),   S(-33, -53),   S(-14, -42),
            S( -6,  -9),   S( -1,  35),   S(-48, -43),   S(-36, -50),   S(-17,  -7),   S(-34, -47),   S(  5,  -3),   S( -2,  -5),
            S(-19,  -9),   S(-10,  -9),   S( -8,  25),   S(-21,  10),   S(  0,  11),   S(-13, -24),   S(-21, -41),   S( -3, -17),
            S(  1,   3),   S(-16,   6),   S(  2,  20),   S( 14,  22),   S(  6,  20),   S( 20,  19),   S(-11, -33),   S(  1,  15),
            S(-15, -37),   S(-22,  24),   S(  9,  44),   S( -6,  19),   S(  0,  18),   S(-19, -16),   S(  2,  -3),   S(-11, -26),
            S( -7,  -3),   S(-26, -21),   S(  9,  37),   S( 13,  34),   S(-15, -15),   S(-11, -40),   S( -2,  -7),   S( -9, -22),
            S(-16, -40),   S( 10,  -7),   S( -6,  -7),   S( 12,  11),   S(-29, -50),   S(-19, -43),   S(  1,  -3),   S(-17, -33),

            /* queens: bucket 10 */
            S( -5,  -4),   S(-20, -39),   S(-26, -52),   S(-34, -59),   S(-10,  -8),   S(-17, -24),   S(-15, -37),   S(  2,   0),
            S(-14, -34),   S( -7, -38),   S(-13, -27),   S(-29, -32),   S(-16,  -9),   S(-42, -66),   S(-19, -27),   S( -9, -15),
            S(-10, -30),   S(-28, -14),   S(-21, -33),   S(-32, -28),   S(-69, -45),   S(-43, -31),   S( -5,  -1),   S(-15, -30),
            S(-22, -25),   S( -8,  -9),   S(-20, -24),   S(-45, -37),   S(-10,  -7),   S(-19,  -2),   S(-10, -15),   S(-20, -41),
            S(  6,  13),   S( -6,   1),   S(-22, -27),   S(-14,  14),   S( -1,   9),   S( 19,  32),   S( 32,  59),   S(-10,  -9),
            S(  3,  19),   S(-43, -59),   S(-10,  -1),   S(-20,   7),   S(-10,   9),   S(-26, -20),   S(-16,  -1),   S(-27, -47),
            S(  2,  12),   S(-20, -41),   S(  2,   3),   S( -2,   1),   S( 18,  33),   S(  0,  12),   S(  5,  11),   S(  2,  -9),
            S(-16, -16),   S(-17, -30),   S(  0,  15),   S(  7,  19),   S(  5,  17),   S( 14,  20),   S( -3, -13),   S( -6, -20),

            /* queens: bucket 11 */
            S(-20, -28),   S(-19, -43),   S(-20, -22),   S( -7, -37),   S(-33, -54),   S(-16, -20),   S( -3,   5),   S(-13, -18),
            S(-15,  -8),   S(-18, -42),   S(-58, -67),   S(-30, -27),   S(-29, -28),   S(-17, -21),   S(  6,  -1),   S( -3,  -7),
            S(-18, -27),   S(-26, -39),   S(-42, -71),   S( -2, -19),   S( -2,  -4),   S(-18, -22),   S(-25,  17),   S(-25, -12),
            S(-24, -37),   S(-31, -47),   S(-30, -46),   S(-23, -25),   S( -8, -22),   S(-29,  -1),   S( 11,  27),   S(  2,   9),
            S(-21, -28),   S(-19, -56),   S(-25, -18),   S(-22, -31),   S(  4, -26),   S( 18,  18),   S(  6,  22),   S(  2,   3),
            S(  4,   4),   S(-28, -33),   S(-30, -41),   S( 16,  -3),   S(  1,  12),   S( 18,  17),   S( 40,  54),   S( -6,   2),
            S(-42, -75),   S(-20, -50),   S(-10, -14),   S(-13, -25),   S( -6, -24),   S( 29,  25),   S( 44,  23),   S( -8, -16),
            S(  2,  -4),   S( -5, -21),   S(  6,  -8),   S(-16, -40),   S( -4, -15),   S(-17, -35),   S(  5,   8),   S( -8, -41),

            /* queens: bucket 12 */
            S(  1,  -2),   S( -3,  -1),   S( -5,  -9),   S(-10, -22),   S( -9, -20),   S(  0, -10),   S(-13, -33),   S(  2,   0),
            S(  2,   3),   S(-15, -32),   S( -6, -25),   S(-15, -27),   S( -7, -17),   S( -9, -18),   S(-13, -21),   S( -8, -16),
            S( -3, -10),   S( -2, -15),   S( 19,  25),   S(-12, -27),   S(-10, -30),   S(-14, -25),   S(-12, -35),   S(-11, -19),
            S(  2,   4),   S(-11, -21),   S( -7,  -7),   S(-22, -44),   S(-11, -11),   S(-12, -35),   S(-13, -19),   S(  0,  -1),
            S(  7,  23),   S( 10,  12),   S( 28,  54),   S(-13,  -8),   S( -8, -25),   S(-20, -53),   S(-14, -30),   S( -6, -12),
            S(  7,  10),   S( -2,  16),   S( 12,  21),   S( -3,  -9),   S( -4,  -6),   S( -5,  -5),   S( -6, -23),   S(  0,  -1),
            S( 10,  25),   S( 13,  21),   S(  3,   6),   S( -4,  -8),   S(  2,   1),   S(  0,  -3),   S(  2,  -1),   S( -3,   1),
            S( -6, -19),   S( -7, -13),   S(-31, -48),   S(-22, -39),   S( 12,  19),   S(  5,   9),   S(  7,  20),   S(-14, -21),

            /* queens: bucket 13 */
            S(-10, -21),   S( -7, -15),   S( -7, -12),   S( -7, -20),   S( -5, -10),   S( -4,  -9),   S(-17, -41),   S( -5,  -7),
            S( -7, -15),   S( -1, -10),   S(-10, -37),   S(-19, -39),   S(-18, -43),   S( -2,  -5),   S(-10, -19),   S(-10, -18),
            S( -6,  -8),   S( -1,  -3),   S( -9, -25),   S(-16, -36),   S(-12, -32),   S(-20, -45),   S(-18, -37),   S( -9, -21),
            S( -1,  -2),   S(  0,   0),   S( -9, -36),   S(  7, -23),   S(-12, -27),   S(-26, -42),   S(-12, -19),   S( -5, -22),
            S( -3,  -7),   S( -1,   5),   S( 11,  15),   S(-22, -27),   S( -6, -21),   S( -4,  -9),   S(-12, -37),   S( -7, -21),
            S( -6, -26),   S(-11, -15),   S( 36,  63),   S( 23,  35),   S( -3,   2),   S(  3,  -2),   S( -1,  -6),   S(  1,  -1),
            S( -2,  -2),   S( 11,  39),   S( 28,  64),   S( 19,  43),   S( 20,  31),   S( -7, -12),   S(  1,  -5),   S( -2,  -3),
            S(-19, -34),   S( 24,  46),   S(-13, -16),   S(-12, -35),   S( 11,  20),   S(-18, -45),   S(  9,  17),   S(-11, -27),

            /* queens: bucket 14 */
            S(  1,   0),   S( -3, -12),   S( -7, -16),   S(-13, -26),   S( -4, -12),   S( -9, -24),   S( -3,  -8),   S(  0,  -2),
            S(-10, -18),   S( -7, -20),   S( -7, -31),   S( -2, -28),   S(-18, -25),   S( -3, -11),   S( -2, -10),   S( -5,  -9),
            S(  5,   4),   S(-13, -31),   S(-22, -37),   S( -5, -24),   S(-13, -33),   S(-10, -27),   S( -5,  -9),   S( -4, -12),
            S(-15, -31),   S(  3,  -5),   S( -1,  -7),   S(-18, -47),   S( -8, -29),   S( -7, -16),   S( -1, -11),   S( -1,  -2),
            S( -1,  -3),   S(  0,  -1),   S(-23, -56),   S( 12,   1),   S(  0, -15),   S( -8, -20),   S( -5, -24),   S(  6,   9),
            S( -9, -24),   S(  0,  -2),   S( -3,  -4),   S( 16,  16),   S( 23,  49),   S( 13,  35),   S(  6,  16),   S( -7, -20),
            S( -2,   4),   S( -7, -26),   S( 23,  41),   S( 22,  37),   S( 16,  28),   S( 24,  40),   S( 15,  20),   S(  5,   4),
            S(  0,  -1),   S(  2,  -1),   S(-11, -23),   S( -3,  -4),   S( 14,  24),   S(  2,   1),   S( -4,  -9),   S(-10, -24),

            /* queens: bucket 15 */
            S(  4,   1),   S(  3,   1),   S( -6, -11),   S( -1,  -7),   S(-11, -17),   S( -1,  -2),   S( -7, -15),   S(  7,  20),
            S( -2,  -8),   S(-14, -33),   S(  3,   0),   S( -9, -22),   S( -3, -13),   S( -1,  10),   S(  7,   3),   S( -9, -24),
            S(  5,  -3),   S(  0,   0),   S( -1,  -6),   S(  0,   2),   S(-13, -36),   S(  6,   3),   S(  1,  -5),   S( -1,  -7),
            S(  2,   3),   S( -1,  -8),   S( -8, -18),   S( -2, -10),   S( -4, -10),   S( -4,  -9),   S( -6, -14),   S(  2,   8),
            S(-16, -31),   S( -4, -20),   S( -2, -10),   S( -9, -27),   S(-18, -44),   S( -4, -14),   S( 10,   5),   S(  3,   2),
            S( -4, -12),   S( -8, -18),   S( -9, -25),   S( -5, -21),   S(-18, -38),   S( 24,  41),   S(  9,  15),   S( -4,  -5),
            S(  1,   2),   S( -2, -17),   S(  4,   4),   S( 16,  29),   S(  7,   2),   S(  8,   4),   S( 10,  10),   S(  6,  10),
            S( -5, -19),   S(  0, -18),   S( -1,   1),   S( 16,  19),   S(  5,   3),   S(  8,   3),   S( -3, -15),   S( -9, -20),

            /* kings: bucket 0 */
            S(-10,  84),   S( -2, 102),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 12,  72),   S( 72,  84),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(-27,  41),   S(-62,  37),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 28,  29),   S(  2,  36),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-42,  44),   S(-59,  45),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 38,  33),   S( 34,  27),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 13,  71),   S(-24,  68),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 56,  75),   S( 10,  68),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-24, -21),   S( 35, -27),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-76,  -1),   S( 12,   1),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  3, -44),   S(-31, -31),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  8, -22),   S( 18, -24),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-16, -26),   S(-33, -27),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 24, -20),   S(  0, -17),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 25,  -1),   S(-43,  -6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 26,  23),   S(-44,  15),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-128, -41),  S( -2, -38),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-15, -48),   S( 35, -41),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( -3, -55),   S( -1, -62),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 80, -63),   S( 53, -59),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  9, -67),   S( -6, -60),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 92, -80),   S( 73, -67),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -1, -49),   S(-120, -60),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 83, -67),   S( 25, -73),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-22, -49),   S( 34, -25),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-48, -86),   S(-33,   2),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(-15, -40),   S( 63, -51),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 52, -68),   S( 34, -87),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 56, -61),   S( 46, -76),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 36, -76),   S(  9, -66),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 52, -54),   S(-26, -68),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 19, -62),   S( -8, -102),

            #endregion

            /* enemy king piece square values */
            #region enemy king piece square values

            /* pawns: bucket 0 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-23,  -3),   S(-47,  38),   S(-14,   9),   S(-28,  32),   S(-15,  11),   S( 20,   7),   S( 35,  -2),   S( 32,  -6),
            S(-15,  -6),   S(-36,  21),   S(-22,   3),   S(-16,   4),   S(  2,   7),   S( -4,   8),   S( 24,   6),   S( 11,  18),
            S(  7,  -9),   S( -2,   3),   S( 18, -16),   S( 12, -28),   S( 17, -20),   S( 15,  18),   S(  8,  27),   S( 44,  -1),
            S( 10,  17),   S( 33,  26),   S( 57,  -9),   S( 42, -10),   S( 23,  23),   S(  4,  69),   S( 37,  52),   S( 82,   8),
            S( 87,  10),   S( 85,  39),   S(101,  10),   S( 74,  14),   S( 36, 137),   S( 55,  78),   S( -2, 121),   S(119,  48),
            S(-151, -44),  S(-125, -25),  S(104, -120),  S( 61,  94),   S(100, 165),   S( 88, 121),   S(141,  37),   S( 72,  96),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-62,  30),   S(-54,  30),   S(-34,  18),   S(-48,  59),   S(-42,  19),   S(  2,   3),   S(  5,   3),   S( -4,  18),
            S(-55,  14),   S(-46,  15),   S(-40,  12),   S(-33,  18),   S(-14,   1),   S(-17,   5),   S( -6,   2),   S(-19,  11),
            S(-38,  24),   S(-17,  20),   S(-23,  12),   S(  5, -10),   S(  0,   7),   S( -8,  10),   S( -6,  15),   S( -1,  10),
            S(-43,  56),   S( 25,  16),   S(  4,  30),   S( 17,  32),   S( 21,  16),   S( -5,  30),   S( 27,  24),   S( 45,  31),
            S(  7,  40),   S( 69,   1),   S(124,  20),   S(102,   9),   S( 68,  41),   S( 54,  34),   S( -7,  56),   S( 66,  62),
            S(216, -33),   S( 43,  18),   S( 36, -55),   S( 21, -54),   S(-14, -25),   S(-105, 128),  S( 57, 119),   S( 25, 154),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-68,  51),   S(-46,  35),   S(-38,  17),   S(-22,  20),   S(-56,  51),   S(-29,  20),   S( -8,  -3),   S(-31,  24),
            S(-65,  37),   S(-40,  26),   S(-50,  19),   S(-42,  23),   S(-44,  20),   S(-38,  11),   S(-16,  -5),   S(-52,  15),
            S(-32,  42),   S(-37,  45),   S(-18,  21),   S(-16,   7),   S(-27,  26),   S(-13,   8),   S(-24,  17),   S(-23,   9),
            S(-20,  70),   S(-21,  63),   S(  5,  36),   S(  7,  34),   S(  2,  35),   S( -8,  27),   S( 23,  20),   S( 41,   8),
            S( -8, 107),   S(-52,  98),   S(-16,  54),   S( 42,   1),   S(130,  13),   S(127,  29),   S( 97,  -3),   S( 84,   5),
            S( -8, 196),   S( 56, 113),   S( 14,  57),   S( 37, -38),   S(  7, -103),  S(-55, -83),   S( -6,  -4),   S(179, -51),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-16,  33),   S(-13,  31),   S(-14,  23),   S(-12,  34),   S(-30,  66),   S(  9,  34),   S( 11,  17),   S(-14,   2),
            S(-11,  34),   S(  2,  29),   S(-16,  20),   S(-19,  26),   S( -4,  19),   S(  4,  15),   S(  4,  11),   S(-32,  11),
            S( 14,  29),   S( -5,  50),   S(  3,  18),   S(  1,  -3),   S( 18, -11),   S( 21,   0),   S(  4,  10),   S( -6,   2),
            S( 16,  69),   S(  1,  80),   S( 23,  46),   S( 19,  18),   S( 35,   0),   S( 42,  -9),   S( 34,  30),   S( 46,   3),
            S( 21, 118),   S(-41, 153),   S(-38, 165),   S(  4, 116),   S( 49,  67),   S(119,  15),   S( 90,  24),   S(120,   8),
            S( 57, 104),   S( 26, 185),   S(-40, 255),   S( 11, 187),   S(-29, 108),   S(-14, -70),   S(-74, -79),   S(-143, -83),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 57,  -2),   S( 32,  -2),   S(  9, -12),   S( -7, -10),   S( -1, -21),   S(-16,  -5),   S(-22,   5),   S(-38,  15),
            S( 29, -10),   S( 12,  13),   S( 16, -22),   S( 22, -20),   S(-48, -24),   S(-10, -18),   S(-50,   1),   S(-50,   9),
            S( 77,   1),   S(103,  -7),   S( 49, -14),   S(-40, -11),   S(-85,   0),   S( -2,  -7),   S(-66,  12),   S(-86,  26),
            S(-16, -43),   S( 70, -83),   S( 43, -24),   S(-33,   9),   S( 17, -13),   S(-13,  31),   S(  6,  14),   S( -6,  21),
            S( 71, -37),   S(-34, -71),   S( 28, -32),   S( 79,  11),   S( 99,  65),   S( 46,  52),   S(-10,  60),   S( 60,  39),
            S( 34, -36),   S( 31,   3),   S( 23, -66),   S( 34,  25),   S( 38,  66),   S(105, 125),   S( 17,  65),   S( -6,  67),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-36,  30),   S(-20,  25),   S( -7,  17),   S( 55,  -9),   S( 78, -28),   S( 14,  -9),   S(-25,   9),   S(-64,  31),
            S(-29,  13),   S(  0,   8),   S( 29,  -7),   S( 21,   0),   S(-14,  -4),   S(  7, -15),   S(-50,   3),   S(-77,  22),
            S( -3,  19),   S( 30,  19),   S( 70,  12),   S( 13,  27),   S(-22,  20),   S(  5,  -3),   S(-26,  12),   S(-58,  26),
            S( 35,  24),   S( 15,  10),   S( 23, -35),   S(-46,  -2),   S( 39, -16),   S(  9,   7),   S( 41,   6),   S(  7,  20),
            S( 65,  21),   S( 54, -29),   S( 40, -44),   S(  0,  -6),   S(109, -36),   S( 54,  24),   S( 44,  38),   S( 29,  64),
            S( 97,  30),   S( 79,  -1),   S( -2, -64),   S( 46, -38),   S( 19, -54),   S(135,  12),   S(113, 114),   S( 98,  59),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-80,  32),   S(-38,   9),   S( -9,  -5),   S( 10, -11),   S( -9,  23),   S( 26,  -2),   S( 23,   0),   S( 16,   5),
            S(-87,  26),   S(-39,   0),   S(-33,  -4),   S( 43, -14),   S( 14,  -1),   S(  7,  -1),   S( 12,  -6),   S(  6,  -5),
            S(-37,  22),   S(-30,  13),   S( -9,   9),   S( 24,  -2),   S( 26,  17),   S( 55,   0),   S( 53,   0),   S( 28,  -4),
            S(-23,  41),   S( 10,  13),   S( 31,   4),   S( 30,  -8),   S(-22, -30),   S( -3, -32),   S( 40, -10),   S( 88, -22),
            S( 61,  48),   S( 53,   8),   S( 47,  14),   S( 30, -13),   S(  3, -25),   S( 17, -29),   S(138, -50),   S(139,  -8),
            S(119,  19),   S(132,  42),   S(115,   9),   S( 45, -39),   S( 37, -77),   S( 30, -72),   S( 22, -21),   S(134,  12),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-52,   4),   S(-50,   1),   S(-23, -14),   S(-45,   5),   S( 17,  -5),   S( 61, -23),   S( 73, -30),   S( 64, -15),
            S(-51,   5),   S(-54,   3),   S(-52, -10),   S(-35,  -9),   S(  8, -17),   S( 45, -31),   S( 46, -16),   S( 40, -13),
            S(-37,   8),   S(-46,   9),   S(-44,  -2),   S(-42, -16),   S(  1, -24),   S( 33, -17),   S( 84, -10),   S( 67,  -9),
            S( -3,  16),   S(-26,  28),   S(-22,  19),   S(-20,   8),   S( 26, -25),   S( 75, -50),   S(  5, -37),   S(  5, -67),
            S( 56,   3),   S(-11,  64),   S( 19,  70),   S(  0,  71),   S(-17,  45),   S( 49, -47),   S( 10, -85),   S(-13, -58),
            S(167,  -1),   S(155,  15),   S(109,  57),   S(107,  68),   S(105,   5),   S( 32, -65),   S( 18, -49),   S( 55, -132),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 18, -21),   S( -5, -18),   S( 63, -36),   S(-19, -36),   S(-31, -46),   S(  0, -34),   S( 65, -53),   S( -4, -38),
            S(-26, -62),   S( -8, -20),   S(-24, -79),   S(-49, -40),   S(-69, -39),   S(-51, -20),   S( 32, -48),   S(-60, -16),
            S(-25, -55),   S( 38, -40),   S( -1, -36),   S( -8, -54),   S(-15, -40),   S(-30, -36),   S(-62,  -8),   S(-67, -14),
            S(-40,  11),   S(-31,  -4),   S( 35, -24),   S( 27, -10),   S( 39, -17),   S( 20,  14),   S(  2,  -3),   S(-25, -16),
            S( 19,  30),   S( -4,  -9),   S( 26,  29),   S( 49,  64),   S( 32,  90),   S( 64,  85),   S( 22,  45),   S(-11,  48),
            S( 20,  46),   S( 19,  34),   S( 66,  89),   S( 22,  24),   S( 56,  94),   S( 44,  96),   S( 22,  83),   S(-11,  22),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 19,  -3),   S( 76, -27),   S( 49,  -2),   S(  9,   7),   S(  3, -13),   S( 74, -51),   S( 44, -53),   S(-26, -33),
            S(-10, -31),   S(-40, -38),   S(  3, -58),   S( -8, -29),   S(-17, -45),   S( 10, -44),   S(-50, -35),   S(-19, -32),
            S(-81,  -6),   S(-46, -30),   S(-27, -60),   S(-75, -29),   S( 30, -49),   S( 26, -52),   S(-29, -42),   S(-60,  -8),
            S(-36,  17),   S(-24, -30),   S( 18, -52),   S( -8, -43),   S(-31, -30),   S(-65, -13),   S(-20,  -7),   S( 26, -17),
            S(-35,  21),   S( -7, -34),   S(  8,  -2),   S( 55,  14),   S( 19,  24),   S( 18,  33),   S(-21,  32),   S(-14,  28),
            S(-28,  16),   S( 23,  10),   S( 29,   5),   S( 28,   2),   S(  6,  -6),   S(  9,  24),   S( -6,  45),   S(-12,  59),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-24, -40),   S(-45, -37),   S( 22, -28),   S(  2, -59),   S( 47, -35),   S(182, -64),   S( 74, -33),   S( 60, -55),
            S(-89, -23),   S(-65, -45),   S( -1, -56),   S(  7, -61),   S(-20, -44),   S(-18, -37),   S( 28, -44),   S( 26, -50),
            S(-100, -14),  S(-79, -35),   S( -6, -42),   S(-16, -42),   S(-18, -62),   S( 18, -73),   S( 19, -58),   S( 21, -40),
            S(  6, -26),   S(  8, -23),   S(-14, -17),   S( 18, -62),   S(  2, -56),   S(  0, -47),   S( 21, -39),   S(  6, -29),
            S( 14,   3),   S( 13, -12),   S( 30, -17),   S( 30, -25),   S( 39,  12),   S( -1, -23),   S( -7, -38),   S( 12,   6),
            S( -7,  12),   S( 19,   4),   S( 33, -10),   S( 25, -35),   S( 33,  38),   S(  3,   3),   S(  4, -19),   S( -3,  -1),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-18, -32),   S(-45, -18),   S(-11, -42),   S(-12, -26),   S(  1, -36),   S(125, -47),   S( 93, -67),   S(110, -59),
            S(-38, -35),   S(-62, -36),   S(-16, -50),   S( -4, -59),   S( -5, -45),   S( 21, -52),   S(-26, -31),   S(  7, -75),
            S(-36, -30),   S(-56, -37),   S(-15, -40),   S(-43, -38),   S(-22, -49),   S( 17, -39),   S(-39, -49),   S( -9, -53),
            S( -9, -16),   S(-66,  -3),   S( 19, -10),   S( 34, -33),   S( 17, -26),   S( 19, -49),   S(-29, -22),   S(-57, -35),
            S( 13, -28),   S( 15,   4),   S(  5,  55),   S( 52,  55),   S( 54,  48),   S( 33,  19),   S( 32, -11),   S( -7,  -2),
            S(-12,  -6),   S( 15,   0),   S( 41,  46),   S( 41,  32),   S( 31,  20),   S( 48,  29),   S( 23,  33),   S( 24,  18),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-29, -76),   S(-17,   4),   S(-13,   7),   S(-11, -27),   S( -6, -28),   S( -3, -64),   S(  8,  -4),   S( -1,  -8),
            S(-38, -51),   S( -4, -11),   S(-36, -69),   S(-66, -104),  S(-46, -64),   S( -5, -21),   S(-14, -44),   S( -3, -48),
            S(-31,  -2),   S( 33, -36),   S(-16, -69),   S(-28, -66),   S(-30, -46),   S(-50, -10),   S(-56, -38),   S(-67, -34),
            S(-17, -21),   S(  3, -12),   S(-14, -55),   S(-10, -19),   S( -9,  26),   S(-24,  63),   S(-30,   8),   S(-53,  -4),
            S( 12, -14),   S(  1,  24),   S( -7, -54),   S( 12,  26),   S( 15,  69),   S( 28,  89),   S(-15,  57),   S(-14,  78),
            S( 20,  32),   S(-10, -51),   S( 17,  24),   S( 11,  48),   S( 14,  71),   S( 21,  70),   S(-32, -16),   S(-24,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-43, -67),   S(-33, -57),   S( -8,  -5),   S(-11, -42),   S(-23, -69),   S(  2, -58),   S(-11, -53),   S(  1, -64),
            S(-70, -62),   S(-43, -71),   S(-36, -63),   S(-12, -30),   S(-22, -70),   S(-41, -42),   S( 11, -49),   S(-30, -34),
            S(-43, -22),   S(-66, -50),   S( -9, -49),   S(-32, -54),   S(-23, -51),   S(-10, -60),   S(-22, -48),   S(-36, -31),
            S(-22,  17),   S( -8, -37),   S( -4, -28),   S( 14,  29),   S(-13, -26),   S(-39,  18),   S(-13,  -2),   S(-32,  25),
            S(  0,  27),   S( -1,  41),   S(-15, -24),   S( 25,  36),   S( 21,  57),   S( 18,  88),   S( -7,  68),   S(-26,  56),
            S(  0,  48),   S( 13,  55),   S(  0,   2),   S(  1, -33),   S( 17,  67),   S(  2,   8),   S( -7,  41),   S( -7,  75),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-18, -77),   S( 13, -73),   S(  1, -46),   S(  3,   5),   S(-14, -43),   S( 17, -29),   S( 20, -48),   S(-13, -65),
            S(-14, -63),   S(-79, -63),   S(-26, -59),   S(-25, -73),   S(-46, -76),   S( -5, -41),   S(-27, -63),   S( -7, -44),
            S(-92, -22),   S(-36, -43),   S(-26, -58),   S(-27, -91),   S(  3, -70),   S(-12, -79),   S(-13, -76),   S(-37,  -4),
            S(-40, -21),   S(-54, -20),   S( -8,   5),   S(-20, -67),   S(-11, -53),   S(-28, -27),   S( 12, -17),   S(-16,  14),
            S(-23, -42),   S( -8, -18),   S( 12,  20),   S(-18, -25),   S( 10,  31),   S(  7, -15),   S(  3,  30),   S(-12,  59),
            S(-13,   7),   S(  0, -15),   S( 14,  38),   S(  9,  49),   S( 13,  20),   S( -3,   9),   S(  6,  56),   S(  5,  70),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-10, -11),   S( -9, -28),   S( -7, -41),   S( -5,  -9),   S(-18, -37),   S(-43, -50),   S(-35, -32),   S( -9, -102),
            S(-48, -34),   S(-35, -66),   S(-17, -38),   S(-30, -88),   S(-30, -30),   S(-40, -39),   S(-52, -16),   S(-23, -75),
            S(-54, -52),   S(-67, -54),   S(-47, -19),   S( 18, -33),   S(-30, -27),   S( -7, -69),   S( 11, -45),   S(-40,  -8),
            S(-12,  -2),   S(-44,  -8),   S(-19,  12),   S(-13, -26),   S( 21,   1),   S(-11, -34),   S(  4, -27),   S(-15,  10),
            S( -9,  -1),   S( -8,  12),   S( -8,  24),   S( 18,  85),   S( 27,  95),   S(  5,  20),   S( 19,  22),   S( 19,   3),
            S(-32,  -4),   S( -6,  23),   S(  0,  63),   S( 11,  58),   S( -1,  26),   S( 26,  96),   S( 18,  33),   S( 10,  47),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: bucket 0 */
            S(-55, -52),   S(-11, -86),   S( 14, -41),   S(-17,  34),   S(-11, -35),   S(-34,  30),   S( -6, -15),   S(-75, -26),
            S(  0, -42),   S(  0,  17),   S(  0, -11),   S(  3,  -8),   S(-15,  17),   S(-24,  37),   S(-18, -58),   S(-24, -19),
            S(-25, -14),   S( 16,   3),   S( -9,  36),   S( 29,  38),   S(-23,  36),   S( 10,  20),   S(-17,  22),   S(-32, -15),
            S( 13,  30),   S( 30,  84),   S( 19,  67),   S( 47,  23),   S( 21,  49),   S( 15,  43),   S( 12,  35),   S(-28,  21),
            S( 41,  66),   S( -2,  78),   S( 42,  78),   S( 22,  61),   S( 76,  47),   S(  2,  42),   S( 13,  30),   S( 14,  34),
            S( 79, -44),   S( -9,  61),   S(144,  20),   S( 90,  15),   S( 50,  37),   S(-34,  93),   S( 37,  14),   S(-19,  25),
            S( 32, -56),   S(-18, -63),   S( 65,  13),   S( 62,  88),   S( 40,  64),   S(  0,  49),   S(  1,  54),   S(-51,  41),
            S(-136, -204), S(-18, -28),   S( 22,  22),   S( 17,  57),   S( -3,  23),   S( 26,  53),   S( -3,   7),   S(-53,   0),

            /* knights: bucket 1 */
            S(-15,   3),   S(-59,  43),   S(-23,  28),   S(-52,  27),   S(-35,  29),   S(-13, -31),   S(-24,  10),   S(-18,  -1),
            S(-39,  37),   S(-75,  89),   S(-18,  41),   S(-13,  26),   S(-15,  25),   S(  6,  23),   S(-42,  37),   S(-29, -13),
            S(-37,  31),   S(-15,  27),   S(-27,  32),   S(-26,  69),   S(-22,  51),   S(-15,  44),   S(-34,  40),   S(-14,   1),
            S(-23,  68),   S( 35,  42),   S( -2,  73),   S( -5,  69),   S(-16,  74),   S(-15,  70),   S( -5,  57),   S(-26,  43),
            S( 54,  21),   S( 11,  33),   S(  9,  92),   S(  9,  61),   S( 30,  64),   S( 21,  57),   S(  0,  55),   S( -4,  67),
            S( 49,  32),   S( 53,  14),   S( 69,  48),   S(100,  30),   S( 23,  57),   S(-46, 103),   S(-18,  80),   S(  2,  57),
            S( 19,  29),   S( 58,   8),   S( 53,  -2),   S( 16,  55),   S(-22,  39),   S(-17,  60),   S( -1,  99),   S(-61,  97),
            S(-155, -21),  S(-19, -30),   S(-38, -44),   S(-15,  47),   S( 21,  22),   S( 27,  65),   S(-32,  34),   S(-42,  17),

            /* knights: bucket 2 */
            S(-63,  52),   S(-50,  52),   S(-33,  24),   S(-31,  31),   S(-35,  36),   S(-57,  25),   S(-33,  31),   S(-38,  -9),
            S(-32,   2),   S(-21,  67),   S(-33,  41),   S(-22,  38),   S(-36,  43),   S(-33,  32),   S( -5,  24),   S(-39,  -8),
            S(-52,  67),   S(-34,  60),   S(-38,  59),   S(-34,  82),   S(-39,  74),   S(-36,  46),   S(-33,  43),   S(-34,  38),
            S(-35,  73),   S(-23,  81),   S(-27, 111),   S(-35, 107),   S(-40,  99),   S(-14,  78),   S(  0,  58),   S(-20,  40),
            S(-24,  97),   S(-19,  87),   S( -8,  85),   S(-13,  76),   S(-34, 107),   S(  6,  91),   S(-25,  84),   S( 25,  22),
            S(-72, 104),   S(-44,  91),   S(-36, 108),   S( -8,  70),   S( 33,  62),   S(125,  29),   S( 33,  75),   S( 20,  32),
            S( 20,  67),   S(-36,  92),   S(  5,  74),   S( 17,  47),   S(-18,  30),   S( 25, -10),   S(  7,  38),   S(-23,  38),
            S(-41,  28),   S( 41,  62),   S(-22, 116),   S( -2,  22),   S( -8,   8),   S(-38, -13),   S( 13,  27),   S(-133, -40),

            /* knights: bucket 3 */
            S(-47,  33),   S(-28,   3),   S(-20,  29),   S(-15,  30),   S(-12,  28),   S(-21,  22),   S(-15,   0),   S( -3, -50),
            S(-35,  26),   S(-16,  50),   S( -7,  43),   S(-11,  46),   S(-13,  47),   S( 13,  39),   S(  5,   6),   S( 11, -29),
            S(-29,  44),   S(-18,  64),   S(-12,  69),   S( -9,  96),   S( -9,  82),   S(-13,  74),   S( -3,  53),   S(-14,  22),
            S(-10,  51),   S(  2,  81),   S(  4,  94),   S( -4, 106),   S( -4, 124),   S(  7, 109),   S( 19,  95),   S( -5,  67),
            S(-10,  82),   S( -8,  93),   S(  4, 100),   S( 17, 119),   S( -1, 119),   S( 24, 125),   S(-21, 141),   S( 36, 138),
            S(-53, 117),   S( -7,  91),   S( 12, 103),   S(-11, 126),   S( 19, 126),   S( 92, 115),   S( 39, 143),   S( 18, 142),
            S(-43,  96),   S(-53, 112),   S(-40, 142),   S( 15, 104),   S( -2, 117),   S( 72,  83),   S( -4,  10),   S(120,  25),
            S(-184,  89),  S(-85, 118),   S(-93, 142),   S( 34, 104),   S( 30, 141),   S(-98, 126),   S(  1,   4),   S(-90, -131),

            /* knights: bucket 4 */
            S(  3,  18),   S(-52,  12),   S(-65,  28),   S(-44, -19),   S(-45,  -2),   S(  6, -25),   S( 19, -29),   S(-32, -26),
            S( 19,  -1),   S(  1,  15),   S(  3,  18),   S( -8,   8),   S(-12,  -9),   S( 51, -25),   S(-16,  11),   S(-62, -25),
            S( 41,  16),   S( 16,  17),   S( 87,  13),   S( 53,  12),   S( 63,   9),   S( 44, -34),   S( 39,  -1),   S( 40, -50),
            S( 22, -49),   S( 60,  -1),   S( 66,  -1),   S( 65,   5),   S( 80,  -4),   S( 38,  13),   S(-33,  20),   S( 24,  -1),
            S(-12, -85),   S( 32, -10),   S( 70,  17),   S( 97,   7),   S( 88,   0),   S( -7,  38),   S( 30, -26),   S(-68,  62),
            S(-11, -30),   S( 23,  25),   S( 67, -19),   S( 79,  19),   S( 13,  14),   S(-26,  28),   S(-30,  19),   S(-14, -17),
            S(-17, -59),   S(-34, -70),   S( 13, -13),   S( 20,  42),   S( 25,  24),   S(  0,  71),   S( -6,  53),   S(-43, -13),
            S( 12,  19),   S( -9, -40),   S(  3, -12),   S(  7,   6),   S( 13,   0),   S( 15,  36),   S( -7,  15),   S(-49, -44),

            /* knights: bucket 5 */
            S(  3,   0),   S( 18,   5),   S(-17,  36),   S(-24,  26),   S( 10,   6),   S( -2,   2),   S(-70,  49),   S(-15,  -4),
            S( 14,  46),   S( 16,  31),   S( 30,   6),   S(-15,  28),   S( 37,   5),   S( 11,  13),   S( 20,  28),   S(-42,  37),
            S(-20,  26),   S( 14,  15),   S( 69,   9),   S( 65,  31),   S( 26,  35),   S( 30,  22),   S(-10,  30),   S(-42,  29),
            S( 61,  16),   S( 19,  13),   S(119, -20),   S(108,  -6),   S( 99,   5),   S(105,  12),   S( 36,  26),   S( 27,  14),
            S( 58,  19),   S(118,  -5),   S( 71,  -7),   S(101, -10),   S(133, -25),   S( 49,  22),   S( 11,   7),   S(-28,  18),
            S(  0,   4),   S( 41,  -9),   S(  7, -44),   S(  6,   6),   S( 30,  17),   S( 46,  21),   S(  6,  18),   S(  0,  14),
            S(-21,  25),   S(-46, -16),   S(-11, -18),   S(  3, -16),   S(  7,  -8),   S(  5,  26),   S( 12,  44),   S( 17,  32),
            S(-23, -26),   S(-15, -20),   S( 17, -14),   S(  2,  16),   S( -1,  23),   S(  6,  34),   S( 10,  50),   S( -1,  16),

            /* knights: bucket 6 */
            S(-22, -46),   S(-16,  -3),   S( -7, -16),   S(-24,  25),   S(-46,  30),   S(-19,  13),   S(-35,  41),   S(-44,   9),
            S( 34, -20),   S( 36,  26),   S( -8,  14),   S( 20,  12),   S( 19,  24),   S(-40,  53),   S(  3,  40),   S(-14,  55),
            S(-49,  26),   S( 51,   9),   S( 67,   2),   S( 99,  15),   S( 67,  20),   S( 17,  29),   S( 33,  30),   S( -5,  41),
            S( 36,  23),   S(101,   8),   S( 92,  10),   S(101,   3),   S(128, -13),   S(128,   8),   S( 68,   9),   S(-37,  44),
            S( 17,  27),   S( 56,  14),   S(136,   0),   S(166, -19),   S(117, -17),   S(125, -16),   S(149, -21),   S( 70,  27),
            S( 39,  10),   S( 37,  22),   S(101,   7),   S( 93,   3),   S( 78, -32),   S( 64, -17),   S( 34,  -9),   S( 21,  30),
            S(-33,  42),   S( 21,  47),   S( 69,  53),   S(  9,  -7),   S( 41,  -2),   S( 46,  15),   S( -5,  18),   S( 23,  26),
            S(  4,  20),   S( 21,  60),   S( 18,  20),   S( -3,  35),   S( 19,  27),   S( -3,   5),   S( 11,  46),   S(-12, -45),

            /* knights: bucket 7 */
            S(-22, -70),   S(-36, -47),   S(  0, -24),   S(-47,  20),   S( 13,  -6),   S(-34,  -3),   S( -9, -13),   S(-33, -17),
            S(  2, -72),   S( 10, -29),   S(-36,  10),   S(-28,  16),   S( 21,  24),   S( 23,  23),   S(-15,  28),   S(-47,  23),
            S( 40, -42),   S(-73,  30),   S( 25, -15),   S( 45,  20),   S( 72,  14),   S( 57,  18),   S( 45,  24),   S( 23,  31),
            S(-67,  25),   S( 30,   2),   S( 89, -10),   S(100,   7),   S(106,  -6),   S(114,  20),   S( 53,  22),   S(101,   5),
            S( 14,  22),   S(  4,  20),   S( 35,  13),   S(109, -11),   S(181, -11),   S(170, -30),   S(183, -35),   S(  8,  -4),
            S( 11,  19),   S( 39,  21),   S(  1,  37),   S( 95,   8),   S(133,   6),   S(115,  -5),   S( 39, -23),   S(  0, -19),
            S(-45, -10),   S(-24,  21),   S( 11,  31),   S( 43,  35),   S( 76,   9),   S( 64,  29),   S(-20, -44),   S(-19, -28),
            S(-33, -50),   S(-25,   5),   S( 55,  49),   S( -6,  35),   S( 19,  32),   S( 30,  22),   S( 17,  -3),   S(-16, -27),

            /* knights: bucket 8 */
            S(  0,   2),   S( 13,  13),   S( -6, -15),   S(-23, -57),   S(-13, -18),   S( -6, -20),   S(  7,   8),   S(-11, -38),
            S(-15, -41),   S( -5, -22),   S(  9, -59),   S(-10, -28),   S( -8,  16),   S(-12, -48),   S(  1, -23),   S( -7, -44),
            S(-11, -53),   S( -4, -30),   S( 26, -50),   S( 33, -23),   S( 41, -66),   S( 60, -26),   S( 13, -33),   S( -7, -31),
            S(-14, -39),   S( -4, -46),   S( 27,  21),   S( 33, -31),   S( 26, -44),   S( 22, -38),   S(-15, -49),   S( -3, -10),
            S( -6, -24),   S(  8, -42),   S(  5, -34),   S(  8, -50),   S( 11, -35),   S(-26, -51),   S(  1, -16),   S(  1, -46),
            S(  6,  16),   S( 19,  -8),   S(  1,   1),   S( 12, -64),   S(  0, -22),   S(  5, -73),   S( -9, -25),   S(-13, -38),
            S( -5, -17),   S(  3, -32),   S( -5, -10),   S(  9,  -2),   S(  0,  25),   S(-19, -49),   S(-11, -10),   S( -6, -30),
            S( -4,  -8),   S( -4,  -6),   S( -7,  -9),   S(  3,  -3),   S(  0,   9),   S( -1,  -8),   S( -4, -16),   S( -1,  -9),

            /* knights: bucket 9 */
            S(-14, -79),   S( -8,  -1),   S( -4, -21),   S(-12, -60),   S( -1, -40),   S(-19, -12),   S(-16, -24),   S(  2,  -9),
            S( -7,  -4),   S(-15, -61),   S( -3, -124),  S(-23, -52),   S(  2, -53),   S( -7, -66),   S(  9,   5),   S(-10, -21),
            S(  0, -35),   S(-20, -76),   S(  9, -68),   S( 25, -85),   S( 15, -40),   S( 11, -20),   S(  3, -21),   S(-13, -28),
            S(-11, -25),   S(-14, -46),   S(-13, -77),   S(  2, -86),   S(  1, -31),   S( 45, -35),   S(-20, -42),   S(  1, -18),
            S( -6, -14),   S(  4, -24),   S( -1, -60),   S(-16, -66),   S( 16, -80),   S( 42, -14),   S( -7, -26),   S( -4, -21),
            S( -6, -37),   S( -8,  -5),   S(  4, -67),   S(  8, -35),   S(  4, -36),   S(-15, -40),   S(  5, -26),   S(-11,   0),
            S( -4, -24),   S( -5,  -3),   S(-25, -39),   S(-14, -24),   S( 16, -13),   S( 14,   9),   S(-17, -14),   S( -5,  -5),
            S( -1,   5),   S(  8,  14),   S(  0,  -5),   S( -6, -37),   S(-17, -35),   S(-11, -46),   S( -1,  11),   S(  0,  17),

            /* knights: bucket 10 */
            S(-13, -59),   S(  4, -17),   S(-13, -21),   S(-12,   3),   S(-11, -50),   S(-10, -27),   S(-30,   5),   S( -8, -39),
            S( -8, -38),   S(-14, -16),   S( -7, -32),   S(  2, -60),   S(-26, -83),   S( -7, -70),   S( -8, -19),   S( -4,  31),
            S( -5, -44),   S(-11, -60),   S( 14, -57),   S( 16, -61),   S(  6, -76),   S( 26, -39),   S( -1, -38),   S( -5,   8),
            S( -2, -27),   S(  0, -53),   S( 44, -62),   S( 28, -65),   S( 20, -60),   S( -1, -36),   S(  0, -29),   S(  5, -28),
            S(-10, -40),   S(-11, -77),   S(  9, -44),   S(  9, -62),   S(  1, -63),   S(  3, -72),   S(  3, -70),   S(-15, -25),
            S(  2, -14),   S(-16, -54),   S( -7, -34),   S( 18, -38),   S(-12, -55),   S(-14, -54),   S( -5, -20),   S(-11, -12),
            S(-11, -31),   S(-12, -16),   S(-15, -56),   S( 31, -38),   S( -4, -20),   S( -2, -53),   S( -8, -11),   S(-23, -54),
            S( -3,   1),   S(  2,  21),   S( -9,  -5),   S(  6,  22),   S(-20, -34),   S( -7, -50),   S( -5,  -9),   S( -5,  -3),

            /* knights: bucket 11 */
            S(-16, -78),   S(-22, -34),   S(-11, -33),   S(  3,  -6),   S(-18, -12),   S( -3, -26),   S(-10, -27),   S(  2,  20),
            S(-13, -62),   S(-19, -37),   S( -7, -65),   S( 38, -33),   S(  4,   0),   S( 10, -46),   S(-15, -51),   S(  4, -43),
            S(-14, -65),   S( -9, -56),   S( 16, -71),   S( 27, -46),   S( 41,  -9),   S( 57, -53),   S( 10, -56),   S( 10, -13),
            S(-34, -61),   S(-18, -45),   S( 20, -63),   S( 87, -37),   S( 74, -48),   S( -8, -21),   S(  0, -48),   S(-13, -22),
            S(-23, -19),   S( 20, -47),   S( 33, -34),   S( 13, -57),   S( 47, -16),   S( 19, -24),   S( -4, -73),   S( -1,  -2),
            S( -4, -19),   S(-18, -84),   S(  6, -11),   S( 38, -36),   S( 21, -23),   S( 10, -18),   S(  0, -31),   S( -8,  -3),
            S( -3, -14),   S(-23, -16),   S( -5,   0),   S(  0, -16),   S( 20,   2),   S( 23, -26),   S(  8, -35),   S( -6,  -4),
            S( -4, -24),   S( -2,   2),   S(-11, -54),   S( -9, -30),   S(-10, -22),   S(  4,  10),   S(  4,   6),   S(  2,   5),

            /* knights: bucket 12 */
            S(-13, -53),   S(-10, -48),   S( -9, -49),   S(  5,  25),   S( -1,  10),   S(-18, -37),   S( -5, -10),   S( -6, -24),
            S( -9, -59),   S( -2, -12),   S(-12, -33),   S(  7,  10),   S( 16,   6),   S( -3, -30),   S(-13, -16),   S( -9, -42),
            S( -7, -28),   S( -7, -35),   S( -4, -27),   S( -5, -79),   S( -7, -30),   S( -7, -33),   S( -2, -14),   S(-11, -11),
            S( -1,  -8),   S( -5, -77),   S(  1, -26),   S( 23, -20),   S( -2, -54),   S( 19, -12),   S(  8,   6),   S(  1,  -2),
            S(  2,   6),   S(  2, -39),   S(-12, -65),   S(-15, -60),   S( -1,   6),   S(-14, -61),   S(-10, -21),   S( -6, -15),
            S( -3, -10),   S( -8, -37),   S( -4, -30),   S( -8, -36),   S( -9, -76),   S(-17, -35),   S(  2,   7),   S( -2,  -9),
            S( -3, -18),   S( -7, -15),   S( -7, -21),   S( -9, -33),   S(  3,   9),   S(-13, -42),   S(  0,   6),   S( -9, -25),
            S(  0,  -1),   S( -1,  24),   S( -1,  -7),   S( -1,  -5),   S( -2,  -7),   S(  8,  35),   S( -2, -13),   S(  0,   7),

            /* knights: bucket 13 */
            S(  0,  -1),   S( -1, -15),   S(  4,   6),   S( -7, -22),   S(  0,   5),   S(  1,  14),   S( -6, -27),   S(  1,   1),
            S(  1,   1),   S( -1, -25),   S( -7, -22),   S(-16, -47),   S(  2, -25),   S( -1, -25),   S(  4,   4),   S(-13, -44),
            S(  3,   4),   S(  1, -16),   S(  5, -39),   S( -6, -52),   S(  4,  -3),   S( 12,  11),   S(  4,   0),   S(-11, -37),
            S(-13, -36),   S( -1,  -5),   S(-15, -93),   S(  5,  -9),   S( 12, -44),   S(  4, -18),   S( 13,  33),   S( 12,  27),
            S(  2,  10),   S(-14, -56),   S(  5, -68),   S( -8, -76),   S(-13, -44),   S(  8, -31),   S( -5, -11),   S( -8, -20),
            S( -7, -35),   S(  5,  13),   S( -9, -39),   S( -7, -57),   S(  0, -19),   S( -2, -29),   S(  0,  -8),   S( -2,  -4),
            S(  4,  17),   S( 15,  33),   S(-14, -37),   S( -7, -39),   S(  4,   7),   S(-10, -17),   S( -3,  -7),   S(  3,  13),
            S(  1,   2),   S(  2,  22),   S(  1,   3),   S( -5,  -8),   S( -7, -28),   S(  1,   2),   S(  1,   6),   S(  0,  -1),

            /* knights: bucket 14 */
            S(  0,  -5),   S( -1, -12),   S( -3, -15),   S(  2,   0),   S( -8, -46),   S(  7,  39),   S(  0, -20),   S(  0,  -5),
            S( -3, -11),   S(-10, -46),   S(  2,  -6),   S( -8, -58),   S( -6, -47),   S( -8, -37),   S( -8, -27),   S(  6,  42),
            S( -4, -18),   S(  1, -53),   S(  3, -17),   S(-11, -68),   S( -7, -43),   S( -2, -64),   S( -3,  -2),   S(  1,   5),
            S( -4, -12),   S(  4, -20),   S( -7, -38),   S(  1, -34),   S(  4, -32),   S(  1, -25),   S( -3, -12),   S(  3,  51),
            S( 10,  14),   S( -7, -36),   S( -3, -50),   S(-20, -43),   S( -6,   1),   S( -7, -36),   S(  5,   1),   S(  0, -10),
            S( -2, -12),   S(  0,   1),   S( -3,  44),   S(-17, -58),   S( -5, -43),   S( -1, -23),   S( -2,   3),   S( -4,  -9),
            S(  1,   1),   S(  1,  12),   S(  6,  23),   S( 10,  52),   S(  1,  28),   S( -9, -19),   S(  2,   6),   S( -5, -16),
            S(  0,   0),   S(  2,  18),   S(  0,  -3),   S(  0,   2),   S(  0,   2),   S(  8,  11),   S(  1,   4),   S(  1,   2),

            /* knights: bucket 15 */
            S( -7, -33),   S( -1,   0),   S(  0,   1),   S( -3,  -9),   S( -4, -24),   S( -9, -47),   S( -6, -62),   S( -2, -29),
            S(  0,   0),   S(  1,  -9),   S( -5, -39),   S( 15,  38),   S(-13, -22),   S(-26, -92),   S( -4, -17),   S(  1, -12),
            S( -2,  -8),   S( -8, -25),   S( 11, -18),   S( 18, -25),   S(-17, -88),   S(  5, -11),   S(  2, -29),   S(  6,   5),
            S(  3,   6),   S(  1, -22),   S(  6, -19),   S( 10,  -7),   S( -7, -45),   S( -8, -40),   S( -3, -27),   S( -4, -12),
            S( -3, -27),   S( -4, -18),   S( -5, -55),   S( -4,  37),   S(  8, -11),   S( -9, -29),   S(  7, -14),   S( -2,  -9),
            S(  1,   9),   S(-11, -29),   S( -2, -12),   S(-12, -13),   S( -2,   2),   S(  0,  -3),   S(  3,  37),   S(  6,  31),
            S(  0,  -9),   S(  3,   3),   S( -2,  -6),   S( -4,   7),   S(  3,  65),   S( -1, -13),   S(  4,  17),   S(  4,  14),
            S(  1,   4),   S( -2, -16),   S(  1,  -1),   S( -3,  -7),   S(  0,  12),   S(  1,  -3),   S(  0,  13),   S(  1,   6),

            /* bishops: bucket 0 */
            S( 39, -22),   S( 17,  33),   S( -7,  -6),   S(-24,   5),   S( -4,   9),   S(-10,  13),   S( 85, -50),   S( 25, -17),
            S( -6, -22),   S( 11, -11),   S( 21,  -7),   S( 13,  18),   S( 10,  19),   S( 61,  -4),   S( 50,  28),   S( 60, -32),
            S( 46, -34),   S( 10,  15),   S( 32,   3),   S( 32,   9),   S( 52,   0),   S( 53,  37),   S( 40,  25),   S( 24,  -4),
            S( 13, -28),   S( 45, -28),   S( 24,  21),   S( 74,  -6),   S( 77,  35),   S( 60,  41),   S( 26,  11),   S( 16,  -1),
            S( 45,  11),   S( 49,  -6),   S( 91, -11),   S(110,   1),   S(123,  -9),   S( 34,  17),   S( 51,  19),   S( -3,  32),
            S( 38,  12),   S(136, -13),   S( 78,  14),   S( 65,  -8),   S( 31,  12),   S( 16,  31),   S( 45,   4),   S(  1,  12),
            S(-90, -129),  S(114,  47),   S( 72,  61),   S( 55,  11),   S(-11,  16),   S( 28,  20),   S( 38,  -5),   S(-11,  27),
            S(  9, -10),   S(  7, -20),   S( 21,  37),   S( 20,  22),   S(-13,  -3),   S(  9,  13),   S( -3,  13),   S(  7, -27),

            /* bishops: bucket 1 */
            S(-21,  -3),   S( 40, -46),   S(-24,  29),   S(  5, -10),   S( -4,  14),   S( -1,   1),   S( 31,  -7),   S( 51, -43),
            S(-12, -20),   S( -3,  -2),   S( 12,  -7),   S(-10,  13),   S( 33,  -4),   S( 17,   1),   S( 60, -22),   S( 24, -24),
            S(-25,  24),   S( 11,  -9),   S( 11,  -2),   S( 19,   2),   S( 11,   6),   S( 52,  -8),   S( 14,   1),   S( 63, -15),
            S( 15, -18),   S( 15,   4),   S( 28,   6),   S( 35,  -7),   S( 54,  -8),   S( 27,  11),   S( 67,  -6),   S(-15,  27),
            S( 32,  -7),   S( 67,  -7),   S( 17,  10),   S( 85, -28),   S( 60, -17),   S( 99, -28),   S( 25,   5),   S( 15,   8),
            S( 61, -33),   S( 48,   4),   S( 97, -12),   S( 97,  -1),   S( 71, -19),   S( 26,   4),   S( -9,  26),   S(  1,   8),
            S( 16, -68),   S(-14, -25),   S( -4, -21),   S(  4,   7),   S( 57, -12),   S(  4,   1),   S(-16,  -4),   S(-60,  42),
            S(  2, -54),   S( 21,  -7),   S(-15, -35),   S(-83,  28),   S(  9,   4),   S( 53,  -4),   S( 27, -31),   S(-31, -40),

            /* bishops: bucket 2 */
            S( -5,  -1),   S( -9,   7),   S( -8,  10),   S(-26,  25),   S(  0,  22),   S(-31,  18),   S(-11,  -3),   S(-18,  -3),
            S( 20,  -3),   S(  6,  11),   S( -5,   7),   S(  6,  17),   S(-15,  26),   S( 21,   6),   S( -1,   5),   S( 10, -30),
            S( 17,  18),   S( 14,   9),   S( 15,  20),   S( -9,  19),   S( -4,  31),   S( -1,   2),   S( -4,  -7),   S(-35,  20),
            S(  2,  14),   S( 28,  21),   S(  6,  33),   S( 38,  19),   S( -5,  18),   S(-14,  35),   S(-37,  29),   S(-11,  31),
            S(  5,  16),   S( 15,  20),   S( 66,   7),   S( 31,  14),   S( 15,  23),   S( 26,  12),   S( 12,  28),   S( 38,   4),
            S(-19,  26),   S(-16,  30),   S(  0,  11),   S( 73,  -8),   S( 51,   6),   S( 54,  37),   S( 72,  14),   S( 24, -36),
            S(-10,  22),   S(-12,   7),   S(-26,  34),   S( 42, -11),   S(-85,  19),   S(-37,  -3),   S(-25,  24),   S(-14, -23),
            S(-46, -21),   S(-20,  14),   S( 17,  12),   S(-14,  51),   S(-21,  11),   S(-77,   8),   S(-15,  -8),   S(-77, -38),

            /* bishops: bucket 3 */
            S( 38,  11),   S( 49,  -6),   S(  2,  14),   S(  5,  36),   S( 15,  39),   S(-11,  54),   S(-12,  67),   S(  3,   7),
            S( 31,  25),   S( 22,  33),   S( 23,  28),   S( 15,  38),   S( 20,  44),   S( 23,  51),   S( 15,  35),   S( 26, -13),
            S(-11,  40),   S( 28,  63),   S( 29,  62),   S( 23,  60),   S( 16,  68),   S( 22,  37),   S( 12,  35),   S( -5,  49),
            S(-14,  35),   S( 12,  55),   S( 33,  83),   S( 56,  59),   S( 33,  51),   S( 15,  50),   S( 23,  19),   S( 22,  -7),
            S( 18,  42),   S( 11,  57),   S( 13,  68),   S( 59,  64),   S( 48,  70),   S( 55,  49),   S( 25,  37),   S(  0,  53),
            S( 15,  57),   S( 25,  70),   S(  3,  49),   S( 29,  59),   S( 44,  68),   S( 48,  99),   S( 38,  80),   S( 14, 131),
            S(-25,  90),   S(  4,  58),   S(  2,  51),   S( -1,  69),   S(-31,  90),   S( 41,  72),   S(-67,  80),   S(  9, -14),
            S(-72,  56),   S(-54,  81),   S(-73,  78),   S(-51,  94),   S( -4,  67),   S(-144, 103),  S( 11,  47),   S(-10,  -4),

            /* bishops: bucket 4 */
            S(-40,  10),   S(-15, -43),   S(-30,  -5),   S(-33,   8),   S(-22,   6),   S(-13,  -3),   S(-23, -48),   S(-50, -52),
            S(-63,  17),   S(-43, -11),   S( 27, -22),   S( -4, -11),   S(-42,  20),   S(  6, -36),   S( -3, -17),   S(-55, -46),
            S( -2,   3),   S(-38,   0),   S( 29,  -9),   S( 23, -22),   S( 33, -13),   S(-21,   9),   S( -3, -30),   S(-37, -43),
            S( 33, -32),   S( 16, -25),   S( 42, -18),   S( 59, -32),   S( 39,  -3),   S( 27,  -9),   S(-23,  -9),   S(-24,   3),
            S( 16, -11),   S(  0, -45),   S( 78, -30),   S( 77, -60),   S( 60,  -7),   S( 25, -13),   S( -6,  11),   S(-18,  -2),
            S(-79, -98),   S(  1, -21),   S( 64, -25),   S( -9, -17),   S( 20, -14),   S( 40,  13),   S( 45,   3),   S(  0,  32),
            S(-10, -12),   S(-15, -70),   S( 32, -21),   S(  0, -30),   S( 20,  -2),   S( 27,  -2),   S(-20,  33),   S(-25,   3),
            S(-14, -61),   S( -1,  13),   S(  1, -28),   S( 15,  14),   S(  5, -11),   S( -7,  10),   S( -6,  59),   S(  2, -11),

            /* bishops: bucket 5 */
            S(-80,   0),   S(  3, -13),   S(-87,  21),   S(-39,   3),   S( 27, -16),   S(-44,   7),   S(-36,   0),   S(-80,  -7),
            S(-34,   8),   S( -4,  -2),   S( 30, -24),   S( 21, -24),   S(-25,   3),   S(-26,   3),   S( -9,  -6),   S(-16,  -3),
            S( 20,   3),   S(-37,  10),   S( 61, -24),   S(  5, -10),   S( 57, -17),   S(-32,   9),   S(-34,  -2),   S(-21, -18),
            S( 14,   9),   S(-17,   3),   S( 79, -26),   S( 94, -39),   S( 34, -16),   S( 73, -16),   S(-25,  -5),   S(-21,  15),
            S( -5,  -1),   S( 35, -26),   S( 12, -35),   S( 28, -42),   S( 46, -33),   S( 77, -42),   S( 59, -24),   S(-74,   3),
            S(-23, -29),   S( 14, -22),   S( 72, -40),   S(-51, -16),   S( -5, -22),   S( 77, -36),   S(-54,   8),   S(-13,  23),
            S(-45,  -1),   S(-13, -23),   S(-16,  -6),   S( -5,  -1),   S( -2, -15),   S( -2, -15),   S( 21,  -6),   S(-50,  -2),
            S(-11,  -8),   S(  3, -19),   S(-21, -12),   S( -4,  -8),   S(-25,  20),   S( 38,  11),   S(-43,   0),   S( -7,  22),

            /* bishops: bucket 6 */
            S(-77,   2),   S(-55,  14),   S(-45,  10),   S( 11,  -7),   S(-31,   5),   S(-32,  -6),   S(-47,  -6),   S(-78,  27),
            S(-42,   6),   S(-27,  -5),   S( -8,  10),   S(-15,   1),   S(-13,   4),   S(-24,  -1),   S(-31,  13),   S(-48,  -7),
            S( -3, -14),   S( 14, -11),   S( 40, -18),   S( 20,  -5),   S( 39, -11),   S( 16,  -9),   S(-20,  -2),   S(-25,  11),
            S(-10,   4),   S( 52, -34),   S( 53, -14),   S(107, -32),   S(137, -45),   S( 63, -24),   S(-14,  -4),   S(-21,   7),
            S(-29,  -1),   S(-10,  -3),   S( 58, -32),   S(103, -47),   S(-13, -27),   S(-16, -23),   S( 41, -28),   S(-21,   4),
            S(-43,  22),   S( -8,  -2),   S( 25, -10),   S(-19,  -7),   S(  9,  -5),   S( 19, -26),   S( 49, -20),   S(  3, -23),
            S(-25,   1),   S(-29,  12),   S( 30, -17),   S(-12, -14),   S(-45,   4),   S(-23, -12),   S(  3, -20),   S(-24,  -7),
            S(-51,  -4),   S(-44,  10),   S( -2,  10),   S(-21,  17),   S( 32,  -1),   S( 34, -36),   S(-16, -16),   S( -8, -23),

            /* bishops: bucket 7 */
            S(-32, -46),   S(  2, -21),   S( -6, -34),   S(  2,  -2),   S(-40,   6),   S(-10, -26),   S(-41, -16),   S(-14, -28),
            S( 35, -63),   S( 24, -40),   S( 19, -27),   S(-22,  -8),   S(-13,   0),   S(-13,  -6),   S(-56,  -5),   S(-54,  23),
            S( 10, -46),   S( 10, -24),   S( 43, -29),   S( 56, -23),   S( 43, -20),   S( 23, -20),   S(-21,   7),   S(-95,  53),
            S( -2,  -8),   S(-16,  -7),   S( 14,  -5),   S( 75, -29),   S(130, -28),   S( 43,  -8),   S( 88, -35),   S(  3,   3),
            S(-15,   7),   S( 12,  -6),   S( 26, -21),   S( 41, -26),   S(129, -55),   S( 95, -22),   S(-48,   7),   S(-30, -21),
            S(-26,  11),   S( 10,  19),   S(  2, -13),   S(  1,  -6),   S( 24, -11),   S( 31,  -7),   S( 24, -25),   S(-69, -101),
            S(-42,  -6),   S(-51,  18),   S(  4,  -3),   S(  9,   9),   S( 45, -19),   S( 26, -18),   S(  9, -34),   S(  1, -16),
            S(-60,  -9),   S(-36,  -3),   S(-21,  17),   S(-39,  12),   S(-20,  24),   S( -5,   0),   S( 22, -33),   S( -1,  -9),

            /* bishops: bucket 8 */
            S( 17,  92),   S(-20,  22),   S( 25, -16),   S(-17,  42),   S( -3,  32),   S(  1,  -2),   S(-14, -37),   S(-12, -22),
            S(  2,  57),   S(  5,  21),   S(-13,  22),   S( 12,  28),   S(-18, -34),   S( 18,   7),   S(-33, -63),   S(  7,  -6),
            S(  2,   2),   S(-17, -34),   S( 21,  49),   S( 21, -13),   S( -4, -15),   S( -6,  -2),   S(-26, -52),   S(-43, -55),
            S( -1,   7),   S( 17,  77),   S( 27,  -2),   S( -2,  29),   S( 25,   7),   S( 16,  -4),   S( -1,   4),   S( 27,  21),
            S(  1,  70),   S(  9,  72),   S( 18,   2),   S( -4,  -7),   S(  3,  29),   S(  0,   6),   S(  1, -17),   S( -8,  -6),
            S(-17, -10),   S(  2,   2),   S(  1,  30),   S( 15,  34),   S(  5,  18),   S( -2,  34),   S(-18,  24),   S(-29, -26),
            S(  0,   0),   S( -9, -44),   S( 19,  39),   S( -7,  48),   S( -3,  24),   S(  5,  47),   S(  5,  68),   S(-18,  37),
            S(  1,  12),   S(-11, -12),   S(  0,   2),   S(  2,  34),   S(  5,  48),   S( 15,  64),   S( -5,  53),   S( 25, 114),

            /* bishops: bucket 9 */
            S( -9,  33),   S(-47,  12),   S(-43,   3),   S(-49,  -3),   S(-37, -23),   S(-20, -18),   S(-24, -21),   S( -6, -38),
            S(-16, -12),   S(-23, -11),   S(-22, -16),   S(-29, -30),   S(-16, -22),   S(  1, -35),   S( -3, -22),   S(-12, -38),
            S(  8, -25),   S(-12, -11),   S(  5, -16),   S( -2, -29),   S(-21, -11),   S(  9, -44),   S(  8, -32),   S(-22, -26),
            S(-10,  28),   S(-15,  -9),   S( 10,  -1),   S(  4,   5),   S(-24, -17),   S( -9, -34),   S(-20, -13),   S( 16, -13),
            S(-25,  25),   S(-24,  20),   S(  0,  -8),   S( -5, -20),   S(  4, -23),   S(-29,  -6),   S(  4,  -6),   S( -9, -26),
            S( -6,   9),   S(-23, -10),   S(-24,  -4),   S(  1,   1),   S(-14, -10),   S(-14,   3),   S( -6,  -7),   S(-19,   3),
            S(-16,  11),   S(-17, -14),   S( -5,  -8),   S(-23, -17),   S( -3,   8),   S( 11,   3),   S( -5,  37),   S(-12,  37),
            S( -5,  46),   S( -8, -37),   S(-18, -33),   S(-10,   5),   S( -4,  13),   S( -5,   9),   S(-16,  16),   S( 14,  61),

            /* bishops: bucket 10 */
            S(-19, -27),   S(-19, -14),   S(-26, -34),   S(-55, -38),   S(-88, -42),   S(-83, -55),   S(-36,  20),   S(  3,  31),
            S( -8, -29),   S( -7, -61),   S(-23, -36),   S(-35, -46),   S(-41, -50),   S(-21, -41),   S(-24, -26),   S( -8,  17),
            S(-12, -44),   S(  6, -79),   S(-26, -57),   S(-20, -26),   S(-21, -46),   S( 12, -39),   S(-13, -18),   S( 15, -23),
            S(-10, -37),   S(-34, -65),   S(-36, -45),   S( -2, -40),   S( 16, -19),   S( 15,  -2),   S(-15,  -8),   S(-22, -24),
            S(-40, -15),   S(-50, -19),   S(  3, -24),   S( 15, -36),   S( 28, -37),   S(-10,  -3),   S(-19, -23),   S( -6,  -7),
            S(-30,  -2),   S(-15,  -4),   S(-36, -10),   S(-37,  -7),   S(-32, -35),   S(-27, -32),   S(-13,  -5),   S( -7,  36),
            S(-15,  29),   S(-19,  -8),   S(-24,  15),   S( -7,  14),   S(-15, -27),   S(-24, -25),   S(-28, -45),   S(  3,  38),
            S(-19,  25),   S(-16,  35),   S(-14,  34),   S(-19, -25),   S(-31, -16),   S(-35, -42),   S(  5, -19),   S( -6, -11),

            /* bishops: bucket 11 */
            S(-22, -38),   S(-38, -76),   S(-29, -15),   S( -2,  11),   S(-19,  -3),   S( -2, -20),   S(-19,  10),   S(-38,  37),
            S(-20, -27),   S(  6, -39),   S( -7,  -8),   S(-19, -10),   S( -7, -17),   S(-43, -15),   S(-47,  -8),   S( -1,  32),
            S( -6, -32),   S( 18, -24),   S(-25, -51),   S( 10, -25),   S( -6, -25),   S( -8,  37),   S(-22, -21),   S(  8,  23),
            S(  3, -16),   S( -1, -27),   S( -1,  -9),   S(  0, -63),   S(  5,   2),   S( 19,  19),   S( 31,  68),   S( -6,   0),
            S(-19,  32),   S(  0, -20),   S(-36, -11),   S(-21,  11),   S( 17, -32),   S(  3,  17),   S(-12,  18),   S( 12,  77),
            S(-26,  18),   S(-16,   9),   S(-14,  13),   S(-29,  14),   S(-19,  30),   S(-20,   3),   S( -6,   8),   S( -1, -23),
            S(-27,  19),   S(-21,  69),   S( 10,  44),   S( -9,  44),   S(-11,  31),   S( -5,  12),   S(-19, -70),   S( 10,  25),
            S(  2, 113),   S(-37,  26),   S(-18,  39),   S( -6,  34),   S( -6,  29),   S( -2,  -1),   S(-31, -19),   S( 10,  22),

            /* bishops: bucket 12 */
            S( -5,  -9),   S(-12, -50),   S( 11,  14),   S(-13,  17),   S( -1,  -5),   S(  3,  15),   S( -3, -18),   S( -7, -24),
            S(  1,   4),   S(  7,  20),   S(  1,   7),   S(  5,   1),   S(-17, -45),   S( 11,  17),   S( -7,  -4),   S( -8, -29),
            S( 11,  46),   S( 21,  73),   S( 23,  64),   S( 16,   0),   S( -4, -25),   S(  9,  11),   S(  5,   1),   S(  3,  18),
            S( -4,  62),   S( 15,  83),   S(  6,  33),   S( 11,  25),   S(  9, -17),   S(  9,   7),   S( -7, -24),   S( -3, -23),
            S( 11,  29),   S( 16,  35),   S(  3,  22),   S( 23,  43),   S( 20,  39),   S( 20,  38),   S( -3,  -9),   S( 13,  24),
            S(  7,  23),   S( -3,   2),   S(  3,  29),   S( 10,  41),   S( 10,  67),   S( 15,  45),   S(-10,  36),   S(  0,  -7),
            S(  2,  13),   S(  6,  22),   S(  6,   2),   S(  5, -16),   S(  5,  30),   S( 18,  88),   S( 12,  42),   S( -1,  26),
            S( -2,   4),   S( -1,   1),   S(  3,  17),   S(  0,   0),   S(  3,   4),   S( 12,  49),   S(  7,  80),   S(  6,  50),

            /* bishops: bucket 13 */
            S(-11, -19),   S(-14, -31),   S(-12, -17),   S( -2,   6),   S(  6,  27),   S( -1, -12),   S(-12, -34),   S(  2, -10),
            S( -6,  -5),   S(-15, -23),   S( -5,  14),   S(  7,  49),   S(  3,  51),   S( -7, -12),   S(-19, -58),   S(  2, -28),
            S(  3,  44),   S( 10,  67),   S(  4,  53),   S(  8,  33),   S(  2,   7),   S(  4,   4),   S( -4,   1),   S(-20, -41),
            S( 18, 121),   S( 16, 106),   S( -9,  17),   S(-15, -12),   S( -6,   8),   S( 14,  24),   S(  9,  14),   S( 11,  38),
            S( 17,  69),   S(  2,  22),   S(  0,  -3),   S(  3,   8),   S( -8, -21),   S(  1,  27),   S(  0,  24),   S(  1,  27),
            S(  1,  35),   S( -9, -21),   S( -8,   3),   S( -4, -20),   S( -9,  35),   S(  4, -11),   S(  2,  16),   S(  7,  39),
            S(  2,  15),   S(-18, -25),   S( -9,  -9),   S(  7,  41),   S( 10,  36),   S( 12,  43),   S( -3,   5),   S(  9,  75),
            S(  3,  20),   S( -6, -15),   S( -8, -11),   S(  6,  13),   S( -4,  14),   S( -3,  -6),   S(  8,  64),   S(  7,  47),

            /* bishops: bucket 14 */
            S(-15, -43),   S( -4,   8),   S(  0,  16),   S( -4,  23),   S( -8,   3),   S( -6,  -9),   S(-13,  -6),   S(  0, -12),
            S( -5, -11),   S(  0,   2),   S(-10,  27),   S( -2,   2),   S( -1,  16),   S( -4,  13),   S(  0,  18),   S( 12,  62),
            S(  4,   8),   S(-11,  15),   S( -9, -10),   S( 24,  49),   S( 15,  22),   S( 11,  75),   S( -4,  52),   S( 15,  56),
            S(  3,  41),   S( -4,   7),   S(-10,  24),   S(-12,   4),   S( -5, -19),   S(-13,   6),   S(  9,  93),   S( 13,  97),
            S( -5,  26),   S( -3,  21),   S( -2,  30),   S(  3, -12),   S( -9, -41),   S( -1,  19),   S(  9,  31),   S(  1,  72),
            S( 10,  81),   S( 12,  48),   S( -3,  28),   S(  4,   9),   S(  4,  77),   S( -8,  -2),   S( -4, -42),   S(  3,  21),
            S(  1,  41),   S( -8,   8),   S( -3,  -1),   S(  5,  33),   S( -5, -17),   S(  3,   7),   S(  3,  -6),   S( -2,  -4),
            S(  5,  39),   S(  8,  46),   S( 11,  38),   S(  3,  14),   S( -4,  -5),   S(  0,  14),   S( 10,  33),   S( -1,   6),

            /* bishops: bucket 15 */
            S( -7, -25),   S( -4, -14),   S(-19, -44),   S( -4,  -9),   S(-15,  19),   S(  6, -12),   S(-12, -46),   S(  3,  -5),
            S(  8,   7),   S( -7, -32),   S(  1,   2),   S( -2,   6),   S( -2,  -1),   S( -5,  -7),   S(  4,  13),   S( -3,  -5),
            S(  1, -10),   S(  1,  -9),   S( -3,  26),   S( 27,  33),   S( 27,  58),   S( 10,   0),   S(  8,  64),   S( 13,  75),
            S( -4, -16),   S( -1,  19),   S( 19,  30),   S( -6, -12),   S(  4,  45),   S( 17,  34),   S( 28,  75),   S(  2,  55),
            S( -7,  -7),   S( -7,  -9),   S( -2,  32),   S( 15,  55),   S(-11,  -1),   S( 20,  30),   S(  1,  32),   S(  0,   3),
            S(  1,  24),   S(  3,  33),   S( 17,  74),   S(  8,  25),   S( 15,  66),   S(-14,  -3),   S( -5, -10),   S(  0,  -1),
            S(  6,  45),   S( 16,  68),   S(  4,  36),   S( 15,  69),   S( -1,  17),   S( -6,  -5),   S(  0,  13),   S( -1,  -3),
            S(  9,  60),   S(  8,  66),   S(  5,  44),   S(  8,  32),   S(  4,  11),   S(  7,  29),   S(  2,  16),   S(  2,  -1),

            /* rooks: bucket 0 */
            S(  1,  31),   S( 27,  16),   S(  8,  25),   S(  3,  37),   S(-10,  61),   S(  2,  50),   S(-26,  82),   S(-29,  54),
            S( 17,  -2),   S(  0,  55),   S(-16,  40),   S(  4,  41),   S(  4,  46),   S(-12,  59),   S( -8,  47),   S(-44,  79),
            S( 41,   0),   S( 18,  18),   S( -8,  23),   S(  7,  22),   S(-29,  62),   S(-28,  51),   S(-21,  60),   S( 12,  49),
            S( -2,  26),   S( 31,  14),   S(-41,  52),   S( 19,  32),   S(  5,  54),   S(-20,  68),   S(-20,  65),   S( -1,  28),
            S( 33, -15),   S( 37,  20),   S( 21,  44),   S( 33,  49),   S( 33,  32),   S( 17,  72),   S( 53,  64),   S( 13,  72),
            S( 25,  25),   S( 67,  42),   S(123,   8),   S(128,  25),   S( 26,  64),   S( 19,  87),   S( 39,  82),   S(-36, 107),
            S( 63,  42),   S( 78,  89),   S(116,  57),   S( 96,  22),   S( 64,  53),   S( 35,  62),   S( 40,  83),   S(-17,  81),
            S( 11,  41),   S( 40,  66),   S( 21,  66),   S( 32,  51),   S( 57,  51),   S( 95,  23),   S( 39,  58),   S( 84, -19),

            /* rooks: bucket 1 */
            S(-62,  77),   S(-31,  37),   S( -9,  37),   S(-51,  64),   S(-49,  65),   S(-44,  67),   S(-60,  96),   S(-82,  96),
            S(-58,  55),   S(-26,  27),   S(-45,  45),   S(-32,  44),   S(-48,  55),   S(-46,  56),   S(-34,  45),   S(-37,  64),
            S(-49,  40),   S( -8,  20),   S(-32,  32),   S(-31,  40),   S(-52,  54),   S(-67,  58),   S(-73,  69),   S(-42,  90),
            S(-65,  67),   S(-26,  35),   S(-23,  38),   S(-77,  80),   S(-47,  59),   S(-83,  86),   S(-53,  80),   S(-70,  89),
            S(-49,  77),   S(-24,  36),   S( 22,  38),   S(-27,  66),   S(-33,  53),   S(-51,  80),   S(-39,  94),   S(-36, 112),
            S( 29,  52),   S( 96,  13),   S( 31,  52),   S( 34,  39),   S( -1,  59),   S( 11,  57),   S( 36,  60),   S(  4,  95),
            S( 65,  40),   S( 19,  33),   S( 40,  29),   S( 29,  61),   S( 28,  31),   S(-10,  54),   S( 27,  79),   S( 42,  92),
            S( 51,  45),   S( 11,  26),   S( 41,  14),   S( 14,  20),   S( 39,  28),   S( 83,  20),   S( 68,  54),   S( 62,  65),

            /* rooks: bucket 2 */
            S(-79, 119),   S(-62, 100),   S(-55,  93),   S(-58,  69),   S(-46,  73),   S(-51,  74),   S(-48,  68),   S(-82,  94),
            S(-80, 106),   S(-69,  99),   S(-75,  95),   S(-77,  89),   S(-77,  89),   S(-61,  56),   S(-27,  48),   S(-71,  81),
            S(-64,  97),   S(-45,  95),   S(-61,  78),   S(-60,  66),   S(-63,  74),   S(-56,  67),   S(-29,  41),   S(-24,  63),
            S(-66, 115),   S(-72, 109),   S(-76,  99),   S(-86,  87),   S(-58,  81),   S(-65,  75),   S(-46,  75),   S(-54,  79),
            S(-46, 120),   S(-62, 126),   S(-52, 107),   S(-43,  81),   S(-67,  95),   S( 11,  62),   S(-50,  98),   S(-39, 107),
            S(  1, 122),   S( -3, 114),   S(  1, 100),   S(-45, 103),   S( 14,  61),   S( 38,  82),   S( 59,  70),   S( 49,  91),
            S( 48,  95),   S(  9, 102),   S( 57,  61),   S( 38,  47),   S(  1,  55),   S( 24,  84),   S(-39, 115),   S(  2, 105),
            S( 64,  78),   S( 33,  98),   S( 23,  79),   S(-45,  88),   S(-48,  60),   S( 34,  43),   S( 15,  88),   S( 44,  82),

            /* rooks: bucket 3 */
            S(-17, 134),   S( -8, 131),   S( -8, 148),   S( -9, 126),   S( -2, 105),   S(  8, 100),   S( 26,  82),   S( -3,  61),
            S(-12, 133),   S(-24, 149),   S(-18, 150),   S(-11, 140),   S(-13, 112),   S( 12,  74),   S( 51,  72),   S( 19,  77),
            S(  4, 119),   S( -4, 146),   S(-18, 139),   S(-18, 129),   S( -2, 103),   S(-12,  95),   S( 28,  84),   S( 29,  79),
            S(-15, 148),   S(-19, 157),   S(-19, 154),   S(-12, 135),   S( -8, 113),   S( -7, 114),   S( 23, 105),   S( -1,  98),
            S(-14, 159),   S(-19, 175),   S( 11, 155),   S(  2, 142),   S( -9, 133),   S( 12, 136),   S( 47, 111),   S( 31, 119),
            S( -5, 166),   S(  1, 169),   S( 18, 167),   S(  8, 161),   S( 64, 114),   S( 90, 116),   S( 70, 137),   S( 47, 129),
            S( 16, 156),   S(  0, 163),   S( 21, 161),   S( 23, 149),   S( 27, 132),   S(113,  98),   S(158, 142),   S(144, 122),
            S(135,  47),   S( 82, 108),   S( 43, 145),   S( 64, 128),   S( 31, 139),   S( 34, 140),   S( 64, 108),   S(138,  81),

            /* rooks: bucket 4 */
            S(-33, -10),   S( 24, -12),   S( -8,  -6),   S( -2,  -7),   S(-68,  35),   S(-26,  32),   S(-16,   4),   S(-68,  29),
            S(-37, -28),   S(-73,  13),   S( 35, -27),   S( -5, -26),   S( -3,  -9),   S(-31,  15),   S(-75,  37),   S( -5,  28),
            S(-38,  -1),   S(-50,   5),   S(-31, -10),   S(-13, -35),   S(-52,  15),   S(-56,   7),   S(-49,  31),   S(-65,   1),
            S( 24, -39),   S( 16, -12),   S( 13, -16),   S(  8, -12),   S( 10,  12),   S(  0,  -5),   S( -8,   9),   S(-42,  34),
            S(  1, -33),   S( 46, -18),   S( 32,  -7),   S( 47, -17),   S( 50,  -3),   S( 22,  28),   S( 44,  25),   S( 18,  29),
            S(  4, -20),   S(  0, -20),   S( 28,   2),   S( 40,   5),   S( 34,  30),   S( 38,  20),   S( 49,   2),   S( 31,  31),
            S( 20, -12),   S( 60,  34),   S( 48,  -4),   S( 71, -43),   S( 87,  -8),   S( 20,  23),   S(  6,  18),   S( 40, -13),
            S( 13,  -2),   S( 61,  13),   S( 61, -19),   S( 36,   2),   S( 30,  -7),   S( 34,   5),   S( -1,  49),   S( 16,   5),

            /* rooks: bucket 5 */
            S(-44,  56),   S(  0,  14),   S( 10,  23),   S( 24,  16),   S(-26,  41),   S(-12,  27),   S(-34,  52),   S(-47,  74),
            S(-19,  15),   S( 15, -16),   S( 17, -20),   S(  7,   2),   S(-32,  27),   S(-21,  16),   S(-61,  44),   S(  7,  30),
            S(-37,  21),   S(  9,  -6),   S(-25,  -7),   S(-42,  15),   S(-28,   5),   S( 20,  -3),   S(-45,   8),   S(-86,  52),
            S(-60,  37),   S(-29,  29),   S(  4,   1),   S(  4,  19),   S(  3,  13),   S( 13,  24),   S( 18,  37),   S(-21,  46),
            S( 70,  17),   S( 54,  21),   S(-32,  42),   S(  0,  13),   S(-11,  32),   S( 98,  -2),   S( 41,  42),   S( 69,  37),
            S( 33,  35),   S(  1,  17),   S( -6,  34),   S(  6, -13),   S( 41,  30),   S( 56,  18),   S( 45,  43),   S( 60,  52),
            S( 38,  15),   S( 76,  -6),   S( 20,   2),   S( 54,  23),   S( 81,  -3),   S( 77, -13),   S( 37,  20),   S( 45,  36),
            S( 52,  14),   S( 49,  22),   S( 51,  16),   S( -1,  53),   S(103,  -1),   S( 61,   5),   S( 15,  53),   S( 14,  56),

            /* rooks: bucket 6 */
            S(-38,  61),   S(-42,  44),   S(-57,  50),   S(-28,  29),   S(  4,  14),   S( -3,  14),   S(-20,  35),   S(-55,  51),
            S(-52,  46),   S(  5,  15),   S(-38,  18),   S( -6,   6),   S(-14,  12),   S( -1,   1),   S(-61,  35),   S(-27,  24),
            S(-79,  43),   S(-49,  31),   S(-21,  -1),   S(-73,  25),   S(-54,  21),   S( -8,   7),   S(-48,   2),   S(-16,   0),
            S(-80,  67),   S( -3,  31),   S(-24,  22),   S( 13,  11),   S( 13,  12),   S( 14,   8),   S(-49,  40),   S( 43,   9),
            S( 12,  54),   S( 50,  30),   S( 92,  10),   S(-12,  37),   S(  9,  10),   S( 31,  34),   S( 48,  23),   S( 90,  21),
            S(111,  21),   S(108,   7),   S(115,  -6),   S( 56,   8),   S( 19,   3),   S( 11,  15),   S( 45,  13),   S( 89,  17),
            S( 41,  21),   S( 79,  -4),   S(130, -31),   S(114, -36),   S( 69,  -8),   S( 74,  -1),   S( 82,  -4),   S( 94,  -8),
            S( 84,  -5),   S( 30,  37),   S( 67,   4),   S( 88, -13),   S( 50,  19),   S( 79,  14),   S( 38,  27),   S( 57,  25),

            /* rooks: bucket 7 */
            S(-82,  33),   S(-84,  43),   S(-55,  43),   S(-61,  35),   S(-45,  12),   S(-31,  10),   S(-35,  26),   S(-79,  21),
            S(-75,  39),   S(-32,  31),   S(-69,  38),   S(-79,  29),   S(-59,  13),   S(-62,  19),   S( 11,  23),   S(  1, -14),
            S(-104,  42),  S(-82,  48),   S(-79,  29),   S(-68,  22),   S(-59,  17),   S(-62,  16),   S( 27, -15),   S(-10, -20),
            S(-89,  46),   S(-17,  19),   S(-29,  14),   S( 16,  -8),   S(-32,  22),   S(-16,  14),   S( 64,  13),   S( 33, -26),
            S( -2,  24),   S( 23,  21),   S( 21,  30),   S( 88, -10),   S(129, -34),   S( 78, -15),   S(104, -13),   S(-29,  14),
            S( 29,  24),   S( 37,  18),   S(104,   0),   S(112, -25),   S(117, -23),   S( 46,  18),   S( 20,  39),   S( -7, -18),
            S( 17,   5),   S( 49,  -6),   S( 65,  -5),   S( 62, -19),   S(107, -36),   S(104, -25),   S( 82,  13),   S( 44,  -9),
            S(-29,   2),   S( 21,  13),   S( 17,  14),   S( 70, -10),   S( 52, -15),   S( 55,   1),   S( 77,  20),   S( 20,  18),

            /* rooks: bucket 8 */
            S( 46, -99),   S( 15, -51),   S( 40, -75),   S( 54, -47),   S( 31, -50),   S(-49, -51),   S( -6, -44),   S( -6, -22),
            S( -8, -79),   S(  2, -32),   S( 10, -56),   S(-12, -54),   S(-18, -45),   S(  0,   9),   S( 19,   0),   S(-13, -40),
            S( -5, -33),   S(  1, -27),   S( 33,   9),   S(  0, -28),   S( -5, -19),   S( 17,  15),   S( 17,  11),   S( -6,   9),
            S( -5, -23),   S( -2,  -9),   S(  2, -31),   S( 26, -19),   S(  2, -27),   S( 42,  11),   S( -5, -36),   S(-13, -12),
            S(-12, -25),   S(  9, -14),   S( 15, -21),   S( 27, -46),   S( 15,   1),   S(  8,   1),   S(  8,   2),   S(-20, -18),
            S( 22,  -4),   S( 27,   6),   S( 24, -28),   S( 10, -55),   S( -8, -19),   S( -9, -12),   S( -4,  -6),   S( -8,  20),
            S(  9,   5),   S( 45,  18),   S( 14, -19),   S( 19, -16),   S(  0, -13),   S( -1,  -2),   S(  9,  23),   S(-10,   6),
            S( 17,  15),   S( 16,  29),   S( 13,   8),   S( 22,  30),   S( 11,  18),   S( 10,  35),   S( -1, -13),   S(  7,  22),

            /* rooks: bucket 9 */
            S( 40, -108),  S( 12, -110),  S(  8, -121),  S( 30, -108),  S( 40, -94),   S(  6, -91),   S( -3, -78),   S(-22, -99),
            S(-24, -84),   S( -5, -78),   S( -8, -89),   S(-18, -89),   S(  2, -92),   S(-26, -58),   S(-30, -77),   S(-11, -52),
            S( -6, -45),   S(-35, -48),   S(  2, -55),   S( -5, -34),   S(  4, -49),   S( -1,  -1),   S(-23, -38),   S(-18, -18),
            S(  9, -46),   S( 19,  -6),   S(  7,   6),   S(  6, -23),   S(  2, -57),   S(  0, -43),   S(-12, -53),   S( 12,  -9),
            S( 15, -46),   S( -6, -44),   S(  7, -77),   S( -3, -29),   S(  6, -56),   S(  5, -48),   S(-16, -38),   S(-16, -51),
            S(  7, -35),   S( -9, -48),   S( -6, -50),   S( -6, -33),   S( 22, -47),   S( 24, -38),   S( -6, -27),   S(  0, -51),
            S( -5, -15),   S( 17, -42),   S(-10, -56),   S( 11, -23),   S(  8, -45),   S(  3, -36),   S( -9, -16),   S(  2, -20),
            S( -5, -19),   S( 12, -17),   S(  7, -13),   S( 19,  -6),   S(  4,  -8),   S( 10, -21),   S(  1,   7),   S( -1,  12),

            /* rooks: bucket 10 */
            S(-15, -78),   S(-51, -76),   S(-16, -92),   S( 37, -101),  S(  8, -113),  S( 17, -123),  S( 45, -111),  S( 30, -111),
            S(-32, -64),   S(-47, -33),   S(-29, -76),   S(-26, -67),   S(-27, -81),   S( 13, -75),   S(  2, -69),   S(-40, -95),
            S(-44, -40),   S(-56, -29),   S(-41, -68),   S(-14, -73),   S(-11, -61),   S( 14, -68),   S( 10, -75),   S(-21, -50),
            S(-16, -54),   S(-14, -69),   S(  5, -66),   S(  1, -46),   S(  3, -28),   S( -1, -20),   S(-17, -91),   S( -3, -82),
            S( 17, -47),   S(-20, -47),   S(  2, -75),   S( 28, -66),   S( 16, -28),   S( 18, -33),   S( 33, -81),   S( 18, -93),
            S( -1, -61),   S(-12, -58),   S( 13, -78),   S( 20, -91),   S( 11, -79),   S(  4, -38),   S( 29, -85),   S(-33, -78),
            S(-39, -48),   S(-41, -59),   S(-21, -81),   S( -1, -66),   S( 12, -45),   S(-28, -59),   S(-27, -69),   S(  0, -51),
            S(-21, -35),   S(-18,  -6),   S(-11, -46),   S( 14, -55),   S(  9, -11),   S(-18, -22),   S(-19, -39),   S(-18, -20),

            /* rooks: bucket 11 */
            S(-14, -95),   S(-16, -38),   S( 10, -53),   S(-19, -57),   S(-24, -41),   S( 57, -101),  S( 10, -52),   S(-37, -74),
            S(-31, -24),   S( -4, -28),   S(-37, -35),   S(-52, -44),   S(-39, -37),   S(  7, -34),   S(  3, -42),   S( -7, -60),
            S(-31, -13),   S(-49,   2),   S(-30,   1),   S(-21, -11),   S( -5, -42),   S( 41, -22),   S(  4, -17),   S(  1,  -3),
            S(-25, -41),   S(  7, -42),   S( -9, -28),   S( 13, -25),   S(  3, -34),   S( -8, -33),   S( 32,  64),   S( -2, -31),
            S(  0, -23),   S(  4, -28),   S(-15, -29),   S( 29, -41),   S( 28, -17),   S( 26, -61),   S( 36,  12),   S(-13, -37),
            S(-10, -35),   S( 19, -11),   S( -1, -33),   S(  0, -52),   S( -1, -36),   S( 40, -46),   S( 45, -20),   S(-25, -37),
            S( -8, -15),   S(-28, -32),   S( -9, -39),   S( -7, -34),   S( -6, -33),   S( 40, -35),   S( 72, -28),   S(-10, -16),
            S(  3,   1),   S( 19,  -4),   S( 20,   7),   S( 27,  -8),   S(  6, -11),   S( 42, -13),   S( 52, -13),   S(-12,  27),

            /* rooks: bucket 12 */
            S(-22, -97),   S(-14, -41),   S(-18, -74),   S(  6, -42),   S(-23, -67),   S( 17, -51),   S(-10, -59),   S(-22, -41),
            S(  2,  -6),   S(  2, -13),   S(  8,  -1),   S( 12, -17),   S( -1, -36),   S( 19, -24),   S( -1,   3),   S(-13, -26),
            S( -3, -22),   S(  8,  24),   S(  9,  -2),   S( 24, -15),   S( 27,  -1),   S( 17,   5),   S( 10,  -7),   S( -4, -28),
            S( -4,   1),   S( 13,   8),   S( 14, -10),   S(  6, -19),   S(  8,  -1),   S(  6,  14),   S(  0, -18),   S( -2,  -1),
            S(  4, -15),   S(  1, -21),   S( 10, -28),   S(  1, -34),   S(  5,   3),   S(  0,   3),   S( -3, -28),   S( -9, -20),
            S(  5,  -8),   S( 10,   1),   S(  8, -52),   S(  3, -20),   S( -1, -26),   S(  6,   1),   S(  8,  24),   S(  8,  -8),
            S(-36, -30),   S( -2,  -6),   S(  8, -19),   S( 12,  -9),   S( -6, -27),   S(  9,  16),   S(  7,  23),   S( -9, -15),
            S(  7, -12),   S(  2,   3),   S(  6, -19),   S(  4, -41),   S(  1,  -8),   S( -3,  -9),   S(  6,  12),   S( 16,  50),

            /* rooks: bucket 13 */
            S(-20, -86),   S(-28, -76),   S(-15, -63),   S( -1, -36),   S(-14, -70),   S(  9, -60),   S(-21, -48),   S(-29, -61),
            S( -7, -56),   S(-12, -76),   S( -1, -21),   S(  8,   9),   S( 16, -17),   S( 19, -18),   S( 16, -22),   S( -5, -65),
            S( 18, -29),   S(  5, -38),   S(  3, -13),   S( 21,   5),   S( 31, -10),   S(  4, -55),   S(  5, -32),   S(-16, -88),
            S(  0, -11),   S( -8, -52),   S(  4, -29),   S( 20,   7),   S( -9, -62),   S(  7,   3),   S( -1, -15),   S( 15,  26),
            S(  2, -38),   S(  4, -69),   S(  2, -61),   S(  1, -32),   S( 19, -75),   S( -2, -53),   S( -8, -41),   S( -2, -29),
            S(  4, -17),   S(  7, -48),   S(  7, -53),   S(  5, -63),   S(  5, -84),   S(  5, -40),   S( -6, -48),   S( 10,   4),
            S( 13,  13),   S( 14, -19),   S( -6, -60),   S( -3, -57),   S( -5, -83),   S(  5, -16),   S( -9, -23),   S(  7,  -1),
            S(  8,  -2),   S(-20, -65),   S( 10, -36),   S(  5, -25),   S(-11, -66),   S(  3, -10),   S(  8,   5),   S(  7,   7),

            /* rooks: bucket 14 */
            S(  2, -41),   S(-32, -45),   S(-14, -65),   S(-23, -91),   S(-18, -85),   S( 11, -22),   S(-19, -80),   S(-15, -57),
            S( 10, -30),   S( 20, -29),   S( 17, -49),   S( -6, -83),   S( -5, -35),   S( -1, -22),   S(  1, -54),   S(  1, -75),
            S( 13, -20),   S( -9, -73),   S( 13, -63),   S(  1, -79),   S(  1, -34),   S(  5, -26),   S( 12, -65),   S(-13, -87),
            S( -6, -49),   S(  6,  -2),   S( -2, -33),   S(-11, -81),   S(-12, -59),   S( -9, -49),   S( 14, -34),   S( -7, -45),
            S( 10,   4),   S( 11,   4),   S(  0, -54),   S( 12, -88),   S(  6, -57),   S(  7, -42),   S(  1, -93),   S(-10, -63),
            S(  8, -24),   S( 11,  -8),   S( 20, -44),   S( 10, -82),   S( -2, -95),   S(  3, -67),   S( 16, -73),   S(-18, -65),
            S( -9, -43),   S( -7, -26),   S(-15, -69),   S(-15, -97),   S( -4, -66),   S(-15, -63),   S( -4, -75),   S(  2, -58),
            S(  0, -39),   S( -5, -18),   S(  1, -39),   S( -6, -78),   S(-15, -80),   S(-10, -64),   S(  2, -40),   S( -7, -29),

            /* rooks: bucket 15 */
            S(-12, -44),   S( -8, -37),   S(-31, -73),   S(-33, -97),   S(-15, -81),   S(  3, -15),   S( -7, -29),   S(-25, -52),
            S( 14,  -1),   S(-12, -46),   S(  4,  -1),   S( -8, -31),   S( -1, -23),   S( -2, -27),   S( -1, -22),   S(  9,   8),
            S(  3, -12),   S( -4, -30),   S(  5, -27),   S( 20, -10),   S(-14, -104),  S( -5, -37),   S( 12,  28),   S(-12, -58),
            S( -6,  -9),   S(  7,  19),   S(  5,   0),   S( -1,  -4),   S(  8,  -7),   S( -3, -32),   S( -9, -57),   S(  7,  -6),
            S(  2,  -3),   S(-17, -24),   S(  7, -24),   S(  6, -42),   S( -4, -44),   S(  3, -57),   S( 24,   2),   S( 12, -35),
            S(  7,  -3),   S(  8,   7),   S(-11, -36),   S( -3, -38),   S( -4, -44),   S(  7, -59),   S( 25,  -4),   S(-10, -31),
            S(  1,  -5),   S( -2,  -3),   S(  5,   8),   S( -5, -27),   S( -7, -39),   S( -2, -34),   S( -7, -42),   S(  0,  -1),
            S(  3,  14),   S(  4,  11),   S( -8, -21),   S(-10, -39),   S(  0, -29),   S( -8, -77),   S( 12, -19),   S(-13, -43),

            /* queens: bucket 0 */
            S(-15,  -3),   S(-32, -64),   S(-20, -74),   S( -1, -82),   S(  5, -70),   S(-11, -89),   S(-41, -33),   S(-34, -12),
            S( -5, -32),   S( 14, -96),   S( 11, -92),   S( -6, -56),   S(-11, -38),   S( -4, -62),   S( -4, -68),   S(-50, -34),
            S(-13,  10),   S(-17, -21),   S(  8, -64),   S( -5, -41),   S(-12,  -8),   S(-15,   5),   S(-10, -28),   S(-68, -62),
            S(-46,  31),   S( 11, -53),   S(-29,  23),   S(-35,  38),   S(-13,  20),   S(-21,  11),   S(-45,   3),   S(-22, -46),
            S(-49,  67),   S( -1,  68),   S(-42, 101),   S( 15,  39),   S(-41,  72),   S(-52,  87),   S(-33,  12),   S(-59,   8),
            S(-44, 100),   S(-15,  47),   S( 24,  65),   S(-19, 106),   S(-71,  90),   S(-34,  64),   S(-110,  90),  S(-25, -20),
            S(  0,   0),   S(  0,   0),   S( 19,  46),   S(-23,  66),   S(-55,  51),   S(-101, 103),  S(-96,  94),   S(-110,  59),
            S(  0,   0),   S(  0,   0),   S( 20,  49),   S( -5,  55),   S(-47,  53),   S(-40,  36),   S(-46,  78),   S(-53, -10),

            /* queens: bucket 1 */
            S(-23, -15),   S( 11, -18),   S( 11, -113),  S( 26, -121),  S( 21, -68),   S( -3, -83),   S(  3, -49),   S(  1,   7),
            S(-26,  17),   S( 28, -47),   S( 31, -50),   S( 21,  -8),   S( 31, -36),   S(  0, -12),   S(-12,  -4),   S(-26, -13),
            S(  5,   7),   S( 14, -18),   S(  5, -12),   S( 14,  17),   S( -9,  17),   S( 17,  -3),   S(-15,  49),   S( 28,  -1),
            S(  9,  13),   S( 25,  46),   S(-14,  46),   S(  4,  31),   S(-16,  77),   S(-32,  64),   S( 10,  28),   S(-13,  73),
            S( 15,  52),   S( 20,  66),   S( 45,  47),   S( 12,  82),   S( 23,  77),   S( 52,  49),   S(-32,  86),   S(  4,  33),
            S( 68,  74),   S( 95,  43),   S( 85,  88),   S( 86,  84),   S( 44,  90),   S( -5, 117),   S( -8,  93),   S(-36,  89),
            S(105, -23),   S(-17,  70),   S(  0,   0),   S(  0,   0),   S( 13,  97),   S(-33,  91),   S(-39, 106),   S(-73,  97),
            S( 91,   6),   S( 72,  30),   S(  0,   0),   S(  0,   0),   S( 61,  46),   S( 57,  66),   S( 61,  48),   S(-54,  58),

            /* queens: bucket 2 */
            S( 28,   2),   S( 19, -15),   S( 29, -22),   S( 47, -61),   S( 45, -70),   S( 21, -50),   S( 13, -59),   S( 46,  26),
            S( 17,  -2),   S( -2,  53),   S( 33, -11),   S( 42,  -7),   S( 47, -28),   S( 26, -39),   S( 34, -26),   S( 10,  70),
            S( 34,  23),   S( 13,  50),   S( -3,  82),   S( 18,  44),   S( 17,  53),   S( 22,  37),   S( 26,  48),   S( 24,  54),
            S( 14,  41),   S(  2, 115),   S(  8,  75),   S( -4, 105),   S( 20,  62),   S( 16,  76),   S( 23,  86),   S( 12, 118),
            S(-15, 102),   S( 14,  56),   S(  0, 120),   S( 14, 110),   S(  4, 153),   S( 75,  69),   S( 51, 131),   S( 43, 115),
            S(-10,  95),   S(-44, 135),   S( -8, 129),   S( 62, 125),   S( 55, 117),   S( 86, 167),   S(133,  82),   S( 23, 155),
            S(-48, 125),   S(-31, 117),   S(-23, 149),   S( 59,  91),   S(  0,   0),   S(  0,   0),   S(-34, 173),   S( 37, 100),
            S( -8,  83),   S( 46,  56),   S( 63,  47),   S( 63,  68),   S(  0,   0),   S(  0,   0),   S( 45,  93),   S( 38, 108),

            /* queens: bucket 3 */
            S(-23,  66),   S(-23,  62),   S(-10,  75),   S(  9,  74),   S( -3,  51),   S(-14,  54),   S(  7,   2),   S(-20,  57),
            S(-51,  95),   S(-16,  88),   S( -5, 102),   S(  1,  97),   S( -3, 100),   S(  4,  64),   S( 25,  36),   S( 32,  18),
            S(-23,  85),   S(-24, 113),   S(-28, 166),   S(-25, 165),   S(-16, 134),   S(-10, 116),   S(  6,  96),   S(  5,  81),
            S(-32, 113),   S(-48, 175),   S(-28, 176),   S(-19, 178),   S(-23, 167),   S(-23, 152),   S(  1, 123),   S( -9, 117),
            S(-34, 148),   S(-39, 175),   S(-38, 179),   S(-26, 198),   S(-19, 197),   S(-10, 209),   S(-25, 217),   S(-19, 185),
            S(-29, 145),   S(-60, 194),   S(-58, 217),   S(-55, 214),   S(-20, 231),   S( 14, 242),   S(-23, 252),   S(-18, 242),
            S(-93, 195),   S(-88, 219),   S(-74, 240),   S(-71, 227),   S(-89, 256),   S(-15, 188),   S(  0,   0),   S(  0,   0),
            S(-140, 232),  S(-63, 194),   S(-73, 200),   S(-79, 208),   S(-36, 205),   S(-13, 191),   S(  0,   0),   S(  0,   0),

            /* queens: bucket 4 */
            S(-53, -10),   S(-54, -24),   S( -3, -18),   S(  1, -17),   S(-53, -10),   S(-44,  -6),   S(-19, -15),   S( 17,  16),
            S( 23,  -2),   S(-27,  17),   S(  1,  -9),   S(-52, -16),   S(-53,  20),   S(-37,  -9),   S(-24, -14),   S(-42, -33),
            S(-20,  23),   S( 25, -11),   S( 38,   7),   S( 20,  -5),   S( 25,  -5),   S( 17,  -1),   S(-61, -49),   S(  2, -20),
            S( -1, -29),   S( 31, -20),   S( -7,  13),   S( 12, -38),   S( 28, -29),   S(-15,  11),   S(-43, -10),   S(-30,   6),
            S(  0,   0),   S(  0,   0),   S( 42,  15),   S( 55,  28),   S( 27,  48),   S( 11,  26),   S( 16, -13),   S(-15, -14),
            S(  0,   0),   S(  0,   0),   S( 57,  58),   S( 42,  42),   S(  7,   0),   S( 23,  37),   S( 10,  20),   S(-26, -37),
            S( 37,  36),   S( 41,  15),   S( 82,  21),   S( 77,  60),   S( 39,   8),   S( 35,   2),   S(-14,   5),   S(-31,  24),
            S( 56,  11),   S( 15,  23),   S( 44,  14),   S( 37,  -8),   S( 22,   3),   S(-19,   2),   S( -6, -22),   S(  4,   8),

            /* queens: bucket 5 */
            S(-12, -20),   S( -5, -44),   S(-30,  14),   S(-14,  -8),   S( 21, -13),   S( 82,  34),   S(  1,   0),   S(-21, -17),
            S(  1, -14),   S(  0, -15),   S(-11, -21),   S(-31,   1),   S( 44, -45),   S(  0, -20),   S(-10, -15),   S(  5,  16),
            S( 26,  13),   S( 78,   0),   S( 12, -35),   S(-16,  18),   S( 44, -17),   S(  4,  20),   S(-21,   7),   S(-25,   1),
            S( 31, -28),   S( 54,  -5),   S( 19, -15),   S( 10,  -6),   S( 51, -19),   S( 57,  31),   S( 32,  27),   S(-36,  -4),
            S( 95,  36),   S( 47,  20),   S(  0,   0),   S(  0,   0),   S( 22,  11),   S( 52,  40),   S( 44,  31),   S( -2,  13),
            S( 43,  17),   S( 61,  43),   S(  0,   0),   S(  0,   0),   S( 44,  29),   S( 95,  40),   S( 71,  25),   S( 11,  36),
            S(121,   2),   S( 76,  23),   S( 58,  40),   S( 41,  50),   S( 72,  47),   S(129,  16),   S( 80,  40),   S( 60,   1),
            S( 43,  16),   S( 74,  30),   S( 80,  26),   S( 86,  45),   S( 74,  19),   S( 82,  33),   S( 43,   5),   S( 48,  11),

            /* queens: bucket 6 */
            S(  6,  15),   S( 10,  -8),   S(  0, -13),   S( -8,  12),   S(-14,  -4),   S(-29, -61),   S(-25,   7),   S(  0,  31),
            S( 25,  -7),   S( 13,  -4),   S( 38, -18),   S( 16, -16),   S(-10,   3),   S(-17, -10),   S( -8,  34),   S( 21,  19),
            S(-33,  34),   S(-30,  32),   S( 21, -12),   S( 67, -51),   S( 41,  -4),   S(  3, -16),   S( 56,  19),   S( 39,  23),
            S(-11,  10),   S( 23, -15),   S( 47,   4),   S( 72,  -4),   S( 19, -20),   S( 12,   8),   S( 96,  24),   S( 84,  32),
            S(-15,  34),   S( 14,  10),   S( 76,  10),   S( 38,  15),   S(  0,   0),   S(  0,   0),   S(125,  61),   S(134,  49),
            S( 17,  21),   S( 70,  23),   S( 18,  34),   S( 54,  36),   S(  0,   0),   S(  0,   0),   S(121,  67),   S(135,  49),
            S(-13,  26),   S( 60,   8),   S( 71,  41),   S( 74,  15),   S( 93,  72),   S(104,  72),   S(174,  20),   S(156,  44),
            S( 48,  10),   S( 72,  20),   S( 68,  20),   S(111,  16),   S(163,  31),   S(163,  21),   S(154,   6),   S(136,  40),

            /* queens: bucket 7 */
            S(-31,   1),   S(-17, -12),   S(-29,  13),   S(-10,   5),   S(-19,  -4),   S(-35,   1),   S(-43,  16),   S(-30, -10),
            S(-35, -27),   S(-69,   8),   S(  8,  11),   S(-47,  40),   S(-42,  16),   S(-23,  14),   S(-23,  16),   S(-38,  19),
            S(  1, -23),   S(-45,   6),   S(-41,  33),   S( 34, -17),   S( 35, -25),   S(  9, -25),   S( 11, -23),   S( 29,  18),
            S(-51,  -1),   S(-20,   9),   S( 20, -14),   S( 75, -36),   S( 59, -17),   S( 92, -35),   S( 46, -16),   S( 17,  27),
            S(-29,   2),   S(-60,  44),   S( 19,  30),   S( 41,  -3),   S(131, -38),   S( 83,  14),   S(  0,   0),   S(  0,   0),
            S(-34,  25),   S(-14,  28),   S( 20,  22),   S(  5,  16),   S( 51,   4),   S(107,  42),   S(  0,   0),   S(  0,   0),
            S(-63,  32),   S(-11,   7),   S( 17,  25),   S( 69, -10),   S( 98,   3),   S( 84,  10),   S(128,  54),   S( 90,  54),
            S( -6,  -4),   S( 23,   4),   S( 68,  -3),   S( 65, -18),   S( 85,   6),   S( 58,  13),   S(  4,  14),   S(116,   9),

            /* queens: bucket 8 */
            S(  3,  -7),   S( 33,  25),   S(-17, -36),   S(  4,  -1),   S( -6, -20),   S( 35,  29),   S(  5, -11),   S( -8, -18),
            S( -7, -17),   S( -3, -17),   S( 24,  13),   S(  8,   0),   S( 18,   8),   S(-22, -42),   S(  0,  19),   S( -5,  -5),
            S(  0,   0),   S(  0,   0),   S( 11, -13),   S( 32,   9),   S(  3, -17),   S( 23,  10),   S( -4,  -2),   S(  1,  -3),
            S(  0,   0),   S(  0,   0),   S( 13,  13),   S(-18, -43),   S( -7, -27),   S( -5, -19),   S(  8,  14),   S(  7,  12),
            S( -8, -10),   S(  2,  -8),   S( 12,  23),   S(-27, -65),   S( 13, -12),   S( -3, -50),   S( -6, -18),   S( -9, -21),
            S( 21,  15),   S( -1, -15),   S( 29,  23),   S(  1, -20),   S( 15, -20),   S( -6, -52),   S( -8, -13),   S( -3,  -5),
            S( -2, -20),   S(  6,  -8),   S( 20,  13),   S( 10,  12),   S(  0, -31),   S( -3,  -3),   S( -4, -40),   S(-21, -32),
            S(  5,   0),   S( -6,  -4),   S( -7, -15),   S( 11,   9),   S( 18,  31),   S( -7,  -6),   S(-10,  -9),   S(-46, -74),

            /* queens: bucket 9 */
            S(  0, -18),   S(-14, -50),   S(  3,  -9),   S( 12, -10),   S(-15, -37),   S(-12, -28),   S(-12, -33),   S(-23, -49),
            S( 25,  11),   S( -1, -25),   S(  4, -13),   S(  0, -15),   S(  8, -19),   S(  1,  -4),   S(  6, -29),   S(-10, -20),
            S( 18,  -1),   S(  8,  -3),   S(  0,   0),   S(  0,   0),   S( 14,   4),   S( 10, -26),   S(  7,   0),   S(  5,  -2),
            S(  8, -25),   S(  7,  -2),   S(  0,   0),   S(  0,   0),   S( 11,  -1),   S( 26,   8),   S(  6,  -7),   S( -8, -14),
            S( 16, -32),   S( -2, -29),   S( -4, -26),   S(-14, -32),   S(-27, -76),   S( 15,  -6),   S( 10, -22),   S( 13, -19),
            S( -7, -38),   S(  0, -67),   S( 18, -13),   S(-15, -42),   S( -2, -44),   S(-12, -57),   S( -9, -42),   S( -8, -31),
            S( -7, -15),   S( 20,   4),   S(-17, -43),   S(-18, -38),   S( 23, -28),   S( 16,   7),   S( -2, -19),   S( -7, -18),
            S( 19,   9),   S(  5, -24),   S(-13, -42),   S(  6,  -7),   S(  7, -17),   S(-14, -49),   S( 17, -16),   S(-17, -52),

            /* queens: bucket 10 */
            S(-11, -19),   S( 24,  -3),   S(  5,  -8),   S( -9, -35),   S( 11, -21),   S(  4, -13),   S(  5, -31),   S( -3, -21),
            S( 15,  18),   S(-11, -38),   S(  8, -19),   S( -3, -31),   S(  7,   4),   S( 21,   4),   S(  3, -23),   S(  2, -11),
            S( -7, -24),   S(  1, -20),   S(  6, -39),   S( 13,  -8),   S(  0,   0),   S(  0,   0),   S(-10, -30),   S(  9,  -8),
            S( -1,   7),   S(-25, -45),   S(  3, -15),   S(  2, -18),   S(  0,   0),   S(  0,   0),   S( 23,  10),   S( 21, -10),
            S(  3, -16),   S(-10, -34),   S( -4, -49),   S( -3, -42),   S(-11, -39),   S(-17, -35),   S(  6, -28),   S( 34,   6),
            S(  1, -18),   S( -8, -34),   S( 14, -13),   S( -5, -38),   S( 14, -12),   S(-10, -55),   S( -5, -46),   S(  7, -41),
            S( 21,  15),   S(-10, -21),   S(  5, -36),   S(  4, -32),   S(  5,   6),   S( 24,  10),   S( 15, -24),   S(  1,  -4),
            S( -8,  -6),   S( -6, -30),   S( -9, -41),   S(-20, -48),   S(-12, -40),   S( -2, -37),   S( 19, -14),   S( 41, -24),

            /* queens: bucket 11 */
            S(-16, -19),   S( -4, -16),   S(  4,  -3),   S(  3, -21),   S( 17,  26),   S( 21,  12),   S(  0,  -2),   S( 29,  28),
            S(  7,   0),   S( -1,   1),   S( 12, -12),   S(-17, -29),   S(  2, -17),   S( -3,  -9),   S( -8, -23),   S( 11, -18),
            S(  4,   8),   S( 20,  16),   S(-38, -35),   S(-11, -39),   S( 20, -15),   S(-10, -24),   S(  0,   0),   S(  0,   0),
            S(-14, -22),   S(-17, -36),   S(-24, -34),   S(-28, -77),   S(-16, -55),   S(  5,  -7),   S(  0,   0),   S(  0,   0),
            S(-28, -26),   S(-12, -25),   S(-11, -28),   S(-10, -24),   S( 13, -23),   S(-13, -20),   S( -9, -14),   S( -9,  -9),
            S(-14, -26),   S(  2,  -3),   S(-16, -25),   S( 17,   5),   S(-14, -43),   S( 12, -26),   S( 35,   3),   S( 15, -25),
            S( -4,  11),   S( 15,  12),   S( 22,  18),   S( -5,   3),   S( 23,   6),   S( 44,  33),   S( 16,  12),   S( 47,  23),
            S(-47, -94),   S( 24,   6),   S( -4, -26),   S(  1,  -1),   S( -2, -14),   S(-21, -38),   S(-20,  -2),   S(-21,  -7),

            /* queens: bucket 12 */
            S(  0,   0),   S(  0,   0),   S( -1,   0),   S(  1,   7),   S(  2,  -1),   S( -7, -16),   S(  9,   2),   S(  3,   8),
            S(  0,   0),   S(  0,   0),   S( 14,   2),   S(  1, -20),   S(-14, -39),   S( 14,  22),   S( -2,  11),   S(  1,   0),
            S(  6,  12),   S(  3,   2),   S(  1,   2),   S( -1, -35),   S( 15,  17),   S(  8,  21),   S( -3, -12),   S(  1,   4),
            S(  6,   0),   S(  1,  -3),   S(  9,  10),   S( 28,  31),   S(-20, -53),   S( -2,  -4),   S( -9, -21),   S( -2,  -9),
            S( -1, -14),   S(  7,   9),   S( 22,  42),   S( -9, -40),   S(-19, -27),   S( -7, -49),   S(-13, -48),   S(  0,   5),
            S(-10, -23),   S( 15,  35),   S(-16, -46),   S( -9, -20),   S(-13, -30),   S(-24, -24),   S(-25, -56),   S(-10, -27),
            S(-15, -20),   S( -8, -22),   S( -7, -19),   S(  6,   1),   S( -4, -30),   S(-16, -33),   S( -6,  -3),   S(-16, -25),
            S(-10, -15),   S(  7,  10),   S( -9,  -2),   S(  6,   6),   S(-11, -10),   S(-10, -29),   S( 10,  11),   S(-13, -24),

            /* queens: bucket 13 */
            S( -9, -34),   S(  3,   3),   S(  0,   0),   S(  0,   0),   S(  0, -17),   S(-11, -31),   S( -6, -16),   S( -3,  -7),
            S( -5, -33),   S(-10, -30),   S(  0,   0),   S(  0,   0),   S( -6, -13),   S( -2, -17),   S(  2,  -4),   S( -7, -23),
            S(-23, -52),   S( -5,  -9),   S( -3, -15),   S(  2, -11),   S(-17, -42),   S( -1,  -4),   S(-15, -23),   S( -5, -12),
            S(  5,   1),   S( -2,  -8),   S(  3, -16),   S(-11, -47),   S( -9, -32),   S(  2,  -6),   S(  6,  -3),   S( -5,  -5),
            S( -1, -30),   S( -3, -16),   S( -5, -29),   S( -1, -22),   S(-28, -72),   S(-15, -45),   S( -8, -21),   S(  5,   6),
            S(-12, -23),   S(-23, -45),   S( -8, -29),   S(-19, -46),   S(-11, -33),   S(-21, -40),   S(-30, -76),   S(-29, -62),
            S( -1, -12),   S(-11, -25),   S(  1,  -2),   S(  3,  -6),   S(  2,  -1),   S( -4, -16),   S(-28, -58),   S(-10, -25),
            S(-11, -26),   S( -1,  -5),   S( -4, -11),   S( 12,  24),   S( -1,   2),   S( -8, -21),   S(  0, -10),   S( -7, -19),

            /* queens: bucket 14 */
            S(  2,  -5),   S(  4,  -3),   S( -2, -35),   S( -2, -10),   S(  0,   0),   S(  0,   0),   S( -5, -18),   S(  2,   6),
            S( -6, -22),   S(  0,  -8),   S(  2, -13),   S( -3, -23),   S(  0,   0),   S(  0,   0),   S( -6, -13),   S( -2,  -1),
            S( -3,  -5),   S( -5, -17),   S(  0, -39),   S(-11, -27),   S( -3,  -7),   S(-14, -34),   S( -5, -18),   S( -4, -19),
            S( -2, -13),   S(  0, -11),   S(-12, -34),   S(-17, -31),   S(  1,  -8),   S(-21, -56),   S( -9, -32),   S(  8,  16),
            S( -4,  -4),   S( -1, -25),   S(-15, -50),   S(-30, -73),   S(-15, -49),   S(-20, -45),   S(-30, -66),   S(-12, -39),
            S(-12, -28),   S(-12, -39),   S(-37, -88),   S(-33, -84),   S( -7, -11),   S(-22, -39),   S(-11, -15),   S(-21, -40),
            S(-13, -23),   S(-12, -31),   S( -3, -13),   S(  5,   1),   S(  3,  -2),   S( -6, -14),   S( -6, -19),   S( -6, -10),
            S(-13, -27),   S(  8,  12),   S(-11, -26),   S(-19, -45),   S(  4,   6),   S(-11, -24),   S(  4,  11),   S( -7, -17),

            /* queens: bucket 15 */
            S( -3,  -8),   S(-12, -26),   S( -3, -11),   S(  5,   9),   S( 13,  13),   S( -6, -14),   S(  0,   0),   S(  0,   0),
            S(-13, -36),   S(  5,   1),   S(-22, -56),   S( -2, -18),   S(  2,  -5),   S(  4,   7),   S(  0,   0),   S(  0,   0),
            S( -7, -18),   S( -3,  -9),   S(-21, -21),   S(-18, -46),   S(-24, -45),   S(  8,   5),   S(  6,   8),   S(  2,   6),
            S( -4,  -9),   S(-11, -23),   S( -1, -11),   S(-18, -44),   S(  7,  14),   S(  5,   9),   S( -9, -38),   S( -8, -26),
            S(-10, -23),   S(  1,  -3),   S(-16, -42),   S(-17, -20),   S(-14, -37),   S(  4,  -1),   S( -6, -18),   S(  5,   7),
            S(  1,  -1),   S( -5,  -5),   S(-19, -44),   S( -6, -16),   S(-16, -40),   S(-19, -44),   S( -9, -24),   S( -4, -15),
            S( -9, -18),   S(-14, -38),   S( -7, -13),   S( -6, -16),   S(-12, -28),   S(-12, -26),   S( -6, -18),   S( -6,  -5),
            S(-13, -45),   S( -4, -13),   S(  3, -13),   S(-12, -25),   S(-17, -26),   S( -7, -15),   S(  3,  10),   S(-15, -24),

            /* kings: bucket 0 */
            S(  4, -53),   S( 27,   6),   S( 30, -30),   S(-21,  20),   S(-17,  -2),   S( 30, -24),   S( 16,  12),   S( 28, -41),
            S(-33,  45),   S( -1,   6),   S( 11,  -7),   S(-44,  23),   S(-33,  31),   S( -8,  13),   S( -8,  53),   S(-16,  32),
            S(  7,  -4),   S( 29, -12),   S(  5,  -8),   S(-17,  -4),   S(-45,   1),   S(-16,  -9),   S(-19,   8),   S( 13,  -4),
            S(-24, -20),   S( 13, -22),   S(-13, -18),   S(-53,   9),   S(-49,  15),   S(-39,  19),   S(-58,  22),   S(-97,  48),
            S(-55, -73),   S( 46, -30),   S(-18, -16),   S(  3,  -9),   S( -8, -13),   S(-28,  -3),   S(-28,  -8),   S(  9,   5),
            S( 24, -96),   S( 37, -54),   S( 63, -85),   S( 11, -26),   S( 32, -27),   S(  7, -19),   S( -6, -11),   S(  9, -15),
            S(  0,   0),   S(  0,   0),   S( 23, -10),   S( 26, -47),   S( 20, -21),   S( -1, -15),   S( 19, -45),   S( -4, -21),
            S(  0,   0),   S(  0,   0),   S( -3, -98),   S( 15, -60),   S( 21,  -4),   S( 16, -23),   S( 18,   7),   S(  9,  22),

            /* kings: bucket 1 */
            S(  1, -20),   S( 29, -17),   S( 15, -19),   S(  3,   3),   S(-15,   1),   S( 18,  -1),   S(  8,  27),   S( 29,  -9),
            S( -2,   2),   S(-12,  30),   S( 10,  -6),   S(-30,  21),   S(-32,  24),   S(-17,  15),   S( -3,  34),   S( -6,  23),
            S( -4, -18),   S(  2,  -5),   S(  1, -21),   S(  6, -13),   S(-30,  -2),   S( -9, -10),   S(  9,  -1),   S( 42, -21),
            S( 45, -23),   S( -6,  -9),   S(  7, -11),   S(-12,   6),   S(-19,  12),   S(-33,  10),   S( -5,  16),   S(-55,  36),
            S(-11, -38),   S( 23, -33),   S( 28, -44),   S( 36, -28),   S(  4, -18),   S( -4, -16),   S( 10,  -6),   S(  3,  -7),
            S( 32, -35),   S( 52, -30),   S( 27,  -8),   S( 44,  -5),   S( 30, -25),   S( 24,   1),   S(-10,  10),   S(-15,   6),
            S(  7, -42),   S( 15,  15),   S(  0,   0),   S(  0,   0),   S(-10,  -7),   S(  9,  29),   S(  4,  19),   S( -4, -18),
            S( -9, -115),  S(-10, -14),   S(  0,   0),   S(  0,   0),   S(  2, -17),   S(  1, -22),   S(  6,  10),   S(  2, -36),

            /* kings: bucket 2 */
            S( 26, -59),   S(  6,  -1),   S( 16, -27),   S( 35, -14),   S(-13,  10),   S( 38, -22),   S(  6,  35),   S( 37, -13),
            S( 19,  -3),   S(-13,  35),   S(  0,   0),   S( -1,   5),   S(-14,  10),   S(-15,   5),   S( 14,  16),   S( -7,  15),
            S(-23, -17),   S(  5, -11),   S(-24, -11),   S(-10, -21),   S( -1,  -8),   S( 10, -29),   S(  0,  -6),   S( 19,  -9),
            S(-46,  32),   S(-16,  13),   S(-12,  10),   S( -4,   4),   S(-14,   3),   S(-21, -12),   S( 16, -21),   S( 64, -26),
            S(-34, -18),   S(  7,  -6),   S( 13, -18),   S( 12, -24),   S( 42, -34),   S( -2, -44),   S( 70, -52),   S(-13, -31),
            S(  4,   1),   S( -6,   6),   S( 28, -23),   S( 69, -32),   S( 71, -19),   S( 38,   8),   S( 63, -28),   S( 24, -18),
            S(-11, -14),   S( 11,  19),   S(-13, -18),   S( 19,  -9),   S(  0,   0),   S(  0,   0),   S( 10,  33),   S( -3, -46),
            S(-11, -12),   S(-15, -45),   S( 12, -42),   S(  6,   8),   S(  0,   0),   S(  0,   0),   S(-10,  -2),   S(-14, -144),

            /* kings: bucket 3 */
            S(  1, -72),   S(  8, -21),   S( 27, -54),   S(-17, -25),   S( -9, -34),   S( 37, -40),   S(  0,  18),   S( 16, -36),
            S(-31,  26),   S(-24,  24),   S(-27,  -2),   S(-29,   1),   S(-54,  13),   S( -9, -12),   S( -8,  19),   S(-21,  19),
            S(  6, -48),   S( 13, -32),   S(-11, -25),   S(-21, -29),   S( -3, -16),   S(  5, -40),   S( 22, -23),   S( 53, -34),
            S(-48,  48),   S(-98,  23),   S(-88,  15),   S(-79,  11),   S(-54,   3),   S(-88,   1),   S(-57, -11),   S(-48, -14),
            S(-22,   9),   S(-52,   1),   S(-42, -23),   S(-52,  -4),   S(-23, -31),   S(-15, -42),   S(-13, -53),   S( 19, -77),
            S(-35, -34),   S( 32, -30),   S(  3, -49),   S(-22, -22),   S( 72, -50),   S(129, -89),   S(157, -69),   S( 74, -126),
            S(-33,  -6),   S( 36, -40),   S(-14, -36),   S( 17, -50),   S(  4, -52),   S( 81, -36),   S(  0,   0),   S(  0,   0),
            S( -9, -30),   S(  0, -28),   S( 15,  -8),   S(  0, -28),   S(  6, -69),   S( 20, -40),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 4 */
            S(  7, -18),   S(-16,  31),   S(-34,  32),   S(  7,   3),   S(-34,   1),   S(  4,   4),   S( 10,  19),   S( 38,  -7),
            S( -8,  20),   S( -8,  25),   S(-13,  12),   S(  3,   8),   S( 55, -13),   S( 53,  -8),   S( 56,   1),   S( 22,  -1),
            S(  8,   8),   S(  2, -14),   S(-37,   0),   S(-24,   6),   S(-23,   6),   S( 25, -29),   S(-40,   6),   S(-19,   6),
            S( -7, -10),   S( -3,  16),   S( 27,   1),   S(  7,   6),   S(-14,  12),   S( 18,   1),   S(-10,  17),   S( 10,   1),
            S(  0,   0),   S(  0,   0),   S( 17,  -6),   S( -1,   3),   S(-13,  -1),   S( -1,  -7),   S(-13,   4),   S( -3,   6),
            S(  0,   0),   S(  0,   0),   S( 15,  11),   S(  3,  -8),   S(  5,  -5),   S(-19,  -4),   S(  6, -20),   S(  8,   0),
            S(  3,  -1),   S(  1,  28),   S(  4, -18),   S(  9, -10),   S(  0,   4),   S(-19,   9),   S(  5, -21),   S( 14,   9),
            S( -5,  32),   S(-11, -27),   S( -4,   8),   S( -4, -16),   S(  3,  -7),   S(-13, -14),   S( -2, -32),   S(  1,   3),

            /* kings: bucket 5 */
            S(-25,   8),   S( -5,  21),   S(-40,  30),   S(-23,  23),   S(-13,  12),   S( 15,   9),   S( 66,   6),   S( 16,  24),
            S( 25,   4),   S( 78, -11),   S( 19,  -4),   S( 34,  -4),   S(  5,   6),   S( 38,  -3),   S( 68,   5),   S( 33,   5),
            S( 24,  -7),   S(-16,   5),   S(  6, -13),   S(-35,  -2),   S( -4,   3),   S(-57,   1),   S( 33,  -8),   S(-20,   8),
            S(  5, -12),   S( 15,  -1),   S(  5,   0),   S( 24,  13),   S( 57,   0),   S( 23,  -1),   S(-10,  10),   S(  1,  11),
            S(-38, -24),   S(-17, -24),   S(  0,   0),   S(  0,   0),   S(-10,   5),   S(-19,  -9),   S( -8,  -2),   S(-59,   8),
            S(-30, -14),   S(-20,  11),   S(  0,   0),   S(  0,   0),   S(-25,   8),   S(-50,  16),   S(-34,  20),   S(  8,   5),
            S(-12,   5),   S(-24,  24),   S( -8,  12),   S( -6,  13),   S(-13,  -7),   S(-14,   0),   S(-14,  25),   S(-12,   3),
            S(-13, -32),   S( -3,   8),   S( -5,  16),   S(  6,  53),   S( 13,  36),   S( -4,   4),   S(  5, -10),   S(  4,  36),

            /* kings: bucket 6 */
            S( 36, -10),   S( 12,   5),   S( -4,   3),   S( 28,   1),   S(-12,  23),   S(  4,  18),   S( 16,  36),   S( 32,   5),
            S( 59, -27),   S( 43,   4),   S( 20,  -2),   S( 43,  -8),   S( 15,   5),   S( 10,   9),   S( 30,  17),   S( 41,   1),
            S(-16,  -6),   S(-20,   4),   S(-33,  -5),   S(  0,  -8),   S( -9,  -3),   S(-53,   2),   S( -4,   4),   S(-51,  22),
            S(-12,  13),   S( 41,   1),   S(-11,   0),   S( 32,   5),   S( 48,   2),   S( 17,  -3),   S( 99, -23),   S( 14,  -9),
            S(-32,   4),   S( -1, -13),   S( -9, -14),   S(-20,  -5),   S(  0,   0),   S(  0,   0),   S(-23, -18),   S(-64, -10),
            S( -4,   3),   S(-24,  22),   S(-31,  10),   S(-18,   6),   S(  0,   0),   S(  0,   0),   S(-29,  20),   S(-56,  13),
            S(  1, -20),   S( -1,  24),   S(-15, -13),   S(-11,  -2),   S(  9,   4),   S(  0,   6),   S( -2,  17),   S(-25, -28),
            S( -6,  22),   S(  9,  13),   S(  1,  13),   S(  6,  33),   S(-14,   8),   S(-10,  27),   S( -6,  52),   S(-24, -32),

            /* kings: bucket 7 */
            S( 84, -52),   S( 13,  -7),   S( 14, -22),   S( 16,  -8),   S(-38,  20),   S(-19,  21),   S(-13,  47),   S(-10,  19),
            S( 21,  -2),   S( 43, -14),   S( 22, -10),   S(-44,  11),   S( -3,   5),   S(-36,  18),   S( 10,  18),   S(  8,  16),
            S( 25, -18),   S(-12,  -8),   S(  3, -15),   S(-10,  -8),   S(-25,   0),   S(-58,  12),   S( -5,   4),   S(-49,  21),
            S(  6,   8),   S( 41, -14),   S(-12,   4),   S( 11,   0),   S(  7,  -2),   S( 25, -10),   S( 15,  -7),   S( 41, -19),
            S(  4,  16),   S(-21,  12),   S(-31,   6),   S( -3,   8),   S( -1, -22),   S( 23, -30),   S(  0,   0),   S(  0,   0),
            S(-17, -25),   S( 14, -17),   S(  4, -15),   S(  0, -20),   S(-31,   3),   S(  2,   0),   S(  0,   0),   S(  0,   0),
            S(  2,   5),   S( 32, -17),   S( 12,   8),   S( -8, -29),   S( 21, -19),   S(-11,  -2),   S( 20,  15),   S(  1, -15),
            S(  5,  25),   S(-10, -15),   S( 10,  -9),   S(  9,  -8),   S(  4, -22),   S( -8,   1),   S( 18,  60),   S(-15, -44),

            /* kings: bucket 8 */
            S(-43, 117),   S(-34,  56),   S(-51,  74),   S( -4,  -2),   S(-31,   8),   S(  9,  11),   S( 68,   0),   S(-53,   9),
            S(  4,  65),   S( 22,  10),   S(-27,  50),   S(-15,  11),   S( 11,   2),   S( 40,  -5),   S( 13,  29),   S( 48,  15),
            S(  0,   0),   S(  0,   0),   S( 36,  41),   S( 14,   4),   S( 47,  -4),   S(-11,  -7),   S(-21,   9),   S( 11,   8),
            S(  0,   0),   S(  0,   0),   S( 25,  23),   S( 30, -15),   S(  3,   4),   S( 21,  -8),   S( 21,  -8),   S( 10,   3),
            S(  3,   4),   S(  2,   9),   S(  6,   6),   S(  7, -21),   S(  2, -21),   S( 11, -10),   S(  3,   3),   S( -3, -24),
            S( -3,   7),   S( -2, -21),   S( -6,  10),   S( -8, -11),   S( -4,  10),   S(  0, -17),   S( -9, -12),   S( -5,  -7),
            S( -2, -12),   S(-10, -26),   S(  3,  -6),   S( -1, -25),   S(-11, -18),   S(  2,  -3),   S( -4,  13),   S(  9, -44),
            S( -3,  -2),   S( -2,  -2),   S(  7, -12),   S( -7,  -7),   S(  4,   1),   S(  3,   2),   S( -3, -26),   S( -5,  -5),

            /* kings: bucket 9 */
            S(-46,  55),   S(-45,  34),   S(-90,  50),   S(-56,  27),   S(-70,  27),   S(-115,  55),  S( 38,  10),   S(-15,  65),
            S(-14,  44),   S( 47,   6),   S(-12,   8),   S( 24,   9),   S( 65,  14),   S( -1,   3),   S( 22,  26),   S( 68,  -1),
            S(-30,  14),   S(  3,  19),   S(  0,   0),   S(  0,   0),   S( 17,  15),   S( 12,  -8),   S( 28,   4),   S( -3,   9),
            S(-21, -16),   S( 14, -18),   S(  0,   0),   S(  0,   0),   S( 26,  14),   S( 23, -14),   S( 10,   4),   S( -7,   2),
            S( -3, -14),   S(  0,   3),   S(  5,   6),   S( 11,  -5),   S( 14, -10),   S(-13,  -7),   S(-15,  13),   S( -5,  -5),
            S(  5,   3),   S( -2,  24),   S(  1,  -3),   S(-17,   2),   S(-12,  14),   S(-28,  19),   S(-22,   2),   S( -3,  17),
            S(  6,  27),   S( -2, -21),   S( 11,  -4),   S(  5,   9),   S( 10,  -7),   S(  3,  26),   S(  4, -15),   S( -2,   7),
            S(  5,  32),   S(-14, -10),   S(  2,   7),   S(  8,   0),   S( -1, -28),   S( 10, -18),   S(  2,  -8),   S( 11,  41),

            /* kings: bucket 10 */
            S(-28,  23),   S( -4,  26),   S(-28,  29),   S(-72,  35),   S(-113,  40),  S(-160,  70),  S(-47,  49),   S(-103,  87),
            S( 28,   8),   S(  0,   6),   S( 31, -18),   S( 16,  13),   S( 77, -14),   S( 56,   4),   S( 25,  32),   S(-33,  46),
            S( -7,   2),   S(  2,  13),   S( 33, -16),   S( -1,  11),   S(  0,   0),   S(  0,   0),   S( 22,  14),   S(-72,  26),
            S( 12,  -2),   S( 21,   2),   S( 39,  -8),   S( 33,  -6),   S(  0,   0),   S(  0,   0),   S( 33,   9),   S( 27,  -1),
            S(  3,  -5),   S( 19,   8),   S( 16,   1),   S( 27, -28),   S(  0,   5),   S( 14,   2),   S( 20,  21),   S(-11,  -5),
            S( -6,  20),   S(-25,  15),   S(-12,   6),   S(  9,   6),   S(  3,   4),   S(-16, -10),   S( -9,  -4),   S(-17,  21),
            S( -4, -16),   S(  3,   5),   S( -2,  -6),   S(  1,  28),   S(  9,  21),   S( 14,   3),   S(  4, -30),   S( -8,  17),
            S(  4,  29),   S( -3, -21),   S(  7,  19),   S(  3,   9),   S( -4, -31),   S(  0,  -7),   S(  3, -20),   S(  9,  50),

            /* kings: bucket 11 */
            S(-19,  41),   S(  5,   5),   S(  2, -12),   S(-25,   7),   S(-14,  -4),   S(-170,  80),  S(-75,  84),   S(-205, 165),
            S( 28, -35),   S(  2,  18),   S( 12, -11),   S( 22,  11),   S( 50,   4),   S( -5,  49),   S( 42,  16),   S( 13,  51),
            S( -2,  -5),   S( 10,  -4),   S(-11, -16),   S( 19,  -2),   S( 29,   0),   S( 39,  20),   S(  0,   0),   S(  0,   0),
            S( -2,  14),   S( -4,   5),   S( 44,  -2),   S( 41, -10),   S( 60, -22),   S( 46,  -2),   S(  0,   0),   S(  0,   0),
            S( 28,  20),   S(-17,   7),   S( 10, -23),   S( 11,  -9),   S(  7,   3),   S( 11, -28),   S( 14,  16),   S(  1,  -5),
            S(  6,  10),   S( -3,   3),   S( 12,   6),   S( 12,   1),   S( -3,  -5),   S(  2,  -2),   S(-15,  -1),   S(  0, -10),
            S(  0,  -3),   S(  6, -23),   S(  9,  -1),   S(  7,  15),   S( 19,  12),   S( -7,   4),   S(  9, -21),   S( -8, -30),
            S(  5,  -1),   S( 13,  17),   S(  0,  -7),   S(  0, -39),   S( -8, -13),   S(  1, -10),   S(  2, -16),   S(  8,  27),

            /* kings: bucket 12 */
            S(  0,   0),   S(  0,   0),   S( -8,  64),   S(-14,  14),   S( 17,  28),   S(-12,  -2),   S(  1,  17),   S(-10,  53),
            S(  0,   0),   S(  0,   0),   S( 13,  91),   S(  6, -12),   S(  2,  26),   S( 18,  -3),   S( 40,  29),   S(-27,   7),
            S(  3,  17),   S(  8, -27),   S( 29,  34),   S( 15,  24),   S( -3, -24),   S(  3,  16),   S(-16, -19),   S( -6,  16),
            S(-10, -31),   S( 12,   9),   S( -3, -50),   S( 10, -15),   S(  8, -22),   S(-11, -11),   S( -7, -13),   S( -1,  19),
            S( 12,  38),   S( -1, -11),   S(  6,  -4),   S( -2,  -1),   S(  0,  -6),   S(  4,  15),   S(-12, -16),   S(-15,   3),
            S(  9,  23),   S( -4,  -7),   S( -2,  -5),   S(  3, -18),   S( -8, -28),   S(  4,  22),   S( -1,  -6),   S(  4,  29),
            S(  9,  27),   S( -7, -17),   S(  2,   3),   S(  2,  -9),   S( -3, -16),   S(-12, -53),   S( 11,  32),   S(  9,  31),
            S( -2,   0),   S(  1, -11),   S(  6, -19),   S( -1, -18),   S( -1, -21),   S( 10,  35),   S( -5, -24),   S( -6, -30),

            /* kings: bucket 13 */
            S(-26, 110),   S(-18,  61),   S(  0,   0),   S(  0,   0),   S(-15,  59),   S(-21,  22),   S( 11,  20),   S(-37,  57),
            S(-26,  13),   S( 10,  29),   S(  0,   0),   S(  0,   0),   S( 11,   1),   S(  1, -17),   S( 13,  15),   S( -7,  35),
            S( -5,  37),   S(  9,  28),   S(  8,   6),   S( -3, -12),   S( 14,  -1),   S( -4,  14),   S( -4,   7),   S(-11,  11),
            S( -5, -19),   S( -3, -11),   S(  4, -23),   S(  5, -63),   S(  3, -21),   S(  7, -21),   S(  5,  18),   S(-12,   1),
            S( -4,   6),   S(  0,  11),   S(  2,   5),   S( -5, -14),   S(  0, -14),   S(  8,  16),   S(-12,  11),   S(-17,  11),
            S(  4,  37),   S( -2,  -3),   S(-13,   0),   S(-11,  -3),   S(  1,  -5),   S(-18, -47),   S(  8, -14),   S(  6,  23),
            S(  1,   2),   S(-11,  28),   S( -6,  14),   S(  8, -10),   S( -1,  -4),   S(  4, -55),   S(  3, -37),   S(  3,  10),
            S(  0,   1),   S(  0,  13),   S( -1,  19),   S( -1, -23),   S( -3, -19),   S( -7,  -9),   S(  4, -13),   S(  6,  22),

            /* kings: bucket 14 */
            S(  1,  43),   S(  1,  13),   S(-19,  13),   S( -3,  13),   S(  0,   0),   S(  0,   0),   S(-19,  63),   S(-90,  93),
            S(-33,   9),   S(-25,  -2),   S(  6, -18),   S( 12,   6),   S(  0,   0),   S(  0,   0),   S( 21,  24),   S(-21,  17),
            S( -1,  18),   S( 16,   4),   S(  8,   4),   S(  5,  -1),   S(  5, -36),   S(  8,  28),   S( 20,  42),   S(-24,  10),
            S( 18,   1),   S( -4, -15),   S(  0,  -3),   S(  4, -34),   S(-12, -52),   S( 14,  16),   S( -6,  -1),   S(  4, -10),
            S(-11, -12),   S( 12, -11),   S( -4, -10),   S(-13,   0),   S( -7,  -7),   S(  2,  22),   S(  2,  -2),   S(  7,  33),
            S(  0,  28),   S(  0,  12),   S(  4,  19),   S( -9,  19),   S(-17,  -5),   S(  4,   2),   S(  0, -10),   S(-15, -13),
            S(  6,   5),   S( -7, -46),   S( -1,  58),   S(  7,  30),   S( -2,   1),   S( -1, -27),   S( -6, -50),   S(  0,  31),
            S( -1,   5),   S(  6,  88),   S( 13,  38),   S(-13, -40),   S( 12,  39),   S( -9, -14),   S(-11, -58),   S(  4,  -5),

            /* kings: bucket 15 */
            S(  9,  58),   S( -3,  15),   S( -8,  11),   S( -6,  14),   S(-21,  12),   S(-47,  81),   S(  0,   0),   S(  0,   0),
            S(-17, -45),   S(-30,  11),   S(-12, -18),   S( 18,  15),   S( 41, -19),   S( 44,  91),   S(  0,   0),   S(  0,   0),
            S(-14,   1),   S( -8,   7),   S(  6, -32),   S(  6,   4),   S(  7,  -1),   S( 25,  29),   S(  9,  -2),   S(-23, -67),
            S(  5,  15),   S( -2,  18),   S(  9,   0),   S( -3, -28),   S(  2, -27),   S( 23, -10),   S( -5,  50),   S( -5,   2),
            S( 15,  38),   S( -3,   7),   S( -5,   8),   S(-14, -50),   S( -3, -18),   S( -1,   9),   S( -3,  22),   S( -2,  17),
            S( -5,  19),   S( -6,  -2),   S(  5,  17),   S( -1,   3),   S( -8, -10),   S(  2,   3),   S( -9,   5),   S(  3,  -2),
            S(  2, -12),   S( -4,   8),   S(-20, -40),   S( -2,  29),   S( 11,  24),   S(  0,  -6),   S( -4, -24),   S(  3,  18),
            S(-11, -41),   S(  5,  36),   S( -5,  -4),   S( 10,  34),   S(  4,  59),   S( -2,   8),   S( -1,   7),   S(  2,  -1),

            #endregion

            /* mobility weights */
            S(  8,   8),    // knights
            S(  5,   4),    // bishops
            S(  3,   3),    // rooks
            S(  2,   3),    // queens

            /* trapped pieces */
            S(-11, -221),   // knights
            S(  3, -136),   // bishops
            S(  2, -98),    // rooks
            S( 15, -93),    // queens

            /* center control */
            S(  1,   7),    // D0
            S(  2,   5),    // D1

            /* squares attacked near enemy king */
            S( 17,  -2),    // attacks to squares 1 from king
            S( 16,   2),    // attacks to squares 2 from king
            S(  5,   2),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S(  7,  20),    // friendly pawns 1 from king
            S( -1,  20),    // friendly pawns 2 from king
            S( -2,  13),    // friendly pawns 3 from king

            /* castling right available */
            S( 44, -21),

            /* castling complete */
            S(  7,  -9),

            /* king on open file */
            S(-54,  11),

            /* king on half-open file */
            S(-20,  23),

            /* king on open diagonal */
            S(-13,  13),

            /* king attack square open */
            S( -9,  -2),

            /* isolated pawns */
            S( -1, -24),

            /* doubled pawns */
            S(-17, -35),

            /* backward pawns */
            S( 11, -24),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 11,  16),   S(  2,   3),   S(  8,   8),   S(  7,  43),   S( 29,  38),   S( -2, -18),   S(-17,  40),   S( -4, -15),
            S(  3,  12),   S( 24,   2),   S(  4,  38),   S( 19,  40),   S( 42,   6),   S( -5,  35),   S( 24,  -5),   S( -3,   7),
            S(-17,  27),   S( 13,  21),   S( -1,  54),   S( 20,  71),   S( 29,  30),   S( 21,  48),   S( 32,  -4),   S(  1,  41),
            S( -9,  66),   S( 30,  48),   S( 13,  90),   S( 15,  88),   S( 59,  70),   S( 64,  46),   S( 23,  49),   S(  9,  55),
            S( 83, 111),   S( 99,  89),   S(103, 156),   S(135, 146),   S(113, 118),   S( 99, 132),   S(124,  66),   S( 84,  81),
            S( 91, 227),   S(138, 323),   S(114, 244),   S(127, 243),   S(101, 214),   S( 65, 174),   S( 67, 212),   S( 40, 140),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,  11),   S(-13,  29),   S(-31,  35),   S(-45,  57),   S( 12,   6),   S(-20,  19),   S(-17,  55),   S( 23,  12),
            S(  9,  26),   S(-15,  46),   S(-20,  38),   S( -8,  26),   S(-20,  38),   S(-57,  50),   S(-50,  62),   S( 21,  21),
            S( -6,  44),   S( -5,  44),   S(-19,  44),   S( 13,  33),   S( -9,  35),   S(-45,  53),   S(-69,  86),   S(-33,  58),
            S( 16,  70),   S( 48,  69),   S( 16,  65),   S( 13,  58),   S(  9,  70),   S( 25,  74),   S(  1,  89),   S(-56, 115),
            S( 32, 111),   S(112, 131),   S( 91, 102),   S( 46,  93),   S(-15,  82),   S( 63,  93),   S( 23, 149),   S(-70, 131),
            S(224, 129),   S(238, 167),   S(258, 176),   S(257, 182),   S(254, 194),   S(240, 196),   S(223, 201),   S(258, 162),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 32,  20),   S( -3,  17),   S(  8,  32),   S( -4,  62),   S( 60,  38),   S( 33,   5),   S(  4,  -5),   S( 45,  16),
            S(  2,  16),   S(  2,  13),   S( 16,  15),   S( 16,  26),   S( 12,  16),   S( -3,  11),   S(  4,   8),   S( 29,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -2, -16),   S( -2, -13),   S(-16, -15),   S(-16, -26),   S(-12, -16),   S(  3, -11),   S( -4,  -8),   S(-29,   4),
            S(-32, -20),   S(  3, -17),   S( -8, -32),   S(  4, -62),   S(-60, -38),   S(-33,  -5),   S( -4,   5),   S(-45, -16),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 32,   3),   S( 35,  13),   S( 51,  18),   S( 51,  14),   S( 38,  23),   S( 40,  11),   S( 19,   9),   S( 44,  -3),
            S(  5,   5),   S( 22,  21),   S( 20,  20),   S( 26,  36),   S( 28,  16),   S( 13,  17),   S( 29,   8),   S( 17,   0),
            S(  0,  11),   S( 15,  38),   S( 46,  43),   S( 44,  42),   S( 47,  37),   S( 55,  15),   S( 24,  27),   S( 29,   6),
            S( 33,  63),   S( 93,  49),   S(106,  81),   S(123,  87),   S(134,  73),   S( 86,  72),   S( 79,  34),   S( 75,  11),
            S( 51,  86),   S(167,  76),   S(179, 151),   S(140, 118),   S(188, 160),   S(150, 136),   S(247,  60),   S(-71, 118),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S( -8,  19),   S( -6,  49),   S(  9,  90),   S(  8, 212),

            /* enemy king outside passed pawn square */
            S( -1, 211),

            /* passed pawn/friendly king distance penalty */
            S( -1, -19),

            /* passed pawn/enemy king distance bonus */
            S(  4,  25),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 71, -47),   S( 42,   4),   S( 45,  13),   S( 48,  41),   S( 55,  13),   S(179, -15),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 17,  -3),   S( 23,  45),   S( 11,  46),   S( 14,  77),   S( 35,  92),   S(158, 103),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-21, -20),   S(-11, -32),   S( -1, -41),   S(-27, -20),   S( 14, -36),   S(216, -91),   S(  0,   0),    // blocked by rooks
            S(  0,   0),   S(  4, -22),   S( 21, -26),   S( -3,   4),   S(  5, -46),   S( -1, -151),  S( 22, -277),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(-12,  15),   S( 24,   8),   S( 40, -17),   S(-36, -15),   S(212,  18),   S(251, -30),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S(  3,  42),

            /* knight on outpost */
            S(  1,  31),

            /* bishop on outpost */
            S( 14,  31),

            /* bishop pair */
            S( 38, 101),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -1,  -3),   S( -5,  -9),   S( -6, -11),   S( -4, -14),   S(  0, -27),   S(-18,  -1),   S(-22,  -7),   S( -3,  -3),
            S( -5, -10),   S( -6, -12),   S(-13, -16),   S( -7, -19),   S(-13, -26),   S(-15, -12),   S(-13, -10),   S( -3,  -7),
            S( -3, -10),   S(  4, -34),   S( -4, -41),   S( -7, -52),   S(-19, -33),   S(-15, -25),   S(-18, -17),   S( -8,  -5),
            S( 15, -34),   S( 13, -39),   S( -5, -28),   S( -8, -40),   S(-16, -28),   S(-10, -26),   S( -2, -33),   S(  1, -26),
            S( 21, -30),   S( 33, -58),   S( 32, -56),   S( 15, -52),   S(  9, -43),   S(  3, -51),   S( 18, -66),   S(  5, -31),
            S( 38, -25),   S( 60, -77),   S( 89, -89),   S( 47, -82),   S( 45, -79),   S( 92, -81),   S( 54, -108),  S( 56, -67),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 41,  -2),

            /* rook on half-open file */
            S( 11,  35),

            /* rook on seventh rank */
            S(-10,  42),

            /* doubled rooks on file */
            S( 24,  23),

            /* queen on open file */
            S(-11,  39),

            /* queen on half-open file */
            S(  5,  36),

            /* pawn push threats */
            S(  0,   0),   S( 27,  33),   S( 29, -12),   S( 31,  25),   S( 29,  -5),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 65,  99),   S( 54, 104),   S( 73,  72),   S( 54,  37),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-11, -14),   S( 50,  40),   S( 90,  10),   S( 40,  34),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 25,  78),   S(  1,  19),   S( 60,  59),   S( 47,  81),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-10,  46),   S( 59,  57),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-16,  23),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats

            /* tempo bonus for side to move */
            S( 16,  10),
        };
    }
}
