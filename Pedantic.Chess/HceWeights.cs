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

        // Solution sample size: 16000000, generated on Wed, 17 Jan 2024 18:59:18 GMT
        // Solution K: 0.003850, error: 0.083659, accuracy: 0.5063
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S(102, 177),   S(447, 555),   S(465, 623),   S(559, 1044),  S(1458, 1790), S(  0,   0),

            /* friendly king piece square values */
            #region friendly king piece square values

            /* pawns: bucket 0 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 78, -104),  S(125, -73),   S( 20, -31),   S(-39,  16),   S(-42,   6),   S(-18, -12),   S(-39,  -5),   S(-41, -23),
            S( 90, -88),   S( 80, -72),   S( -5, -47),   S(-24, -59),   S(-24, -30),   S(-19, -31),   S(-40, -20),   S(-35, -39),
            S( 90, -80),   S( 62, -44),   S( 18, -47),   S( 12, -62),   S( -4, -59),   S(  2, -45),   S(-11, -32),   S(-27, -34),
            S( 56, -17),   S( 43, -19),   S( 29, -27),   S( 29, -58),   S( -5, -31),   S(-16, -24),   S(-19, -20),   S(-31,  -5),
            S( 61,  61),   S( 27,  33),   S( 50,   7),   S( 58, -24),   S( 36,  -3),   S( -8,  -6),   S(-25,  18),   S(-33,  55),
            S( 81,  62),   S( 65,  95),   S( 30,  20),   S( 45, -13),   S(-11,  16),   S( 16,  23),   S( 28,  26),   S( 19,  50),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 16, -21),   S( 16, -26),   S( 27, -21),   S(-21,  10),   S(-21, -17),   S( 11, -23),   S(-28,  -1),   S(-41,  23),
            S(  9, -18),   S(  6, -24),   S( -9, -26),   S(-19, -35),   S(-19, -25),   S( -6, -31),   S(-37,  -4),   S(-49,   4),
            S( 12, -20),   S( 10, -15),   S( 14, -34),   S( 16, -47),   S( -6, -29),   S(  8, -38),   S( -6, -14),   S(-25,   0),
            S( 23,  13),   S( 15, -14),   S( 20, -13),   S( 21, -26),   S(  5,  -8),   S( 21, -23),   S(-27,   4),   S(-30,  25),
            S( 15,  65),   S(-33,  37),   S(-12,  28),   S(  7,  11),   S( 39,  25),   S(  1,  26),   S(-12,  40),   S(-21,  70),
            S( 55,  70),   S( 26,  35),   S(-40,  23),   S( -5,  51),   S(  9,  20),   S(-41,  48),   S(-23,  48),   S(-32,  87),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-32,  11),   S(-30,  11),   S(-12,  -5),   S(-28,   9),   S(-27,  -5),   S( 19, -20),   S( 11, -41),   S(-11, -21),
            S(-33,   6),   S(-44,   6),   S(-36, -21),   S(-27, -32),   S(-16, -24),   S( -5, -18),   S(-19, -20),   S(-32, -13),
            S(-32,  10),   S(-38,   4),   S( -7, -39),   S( -2, -43),   S( -7, -23),   S( 19, -30),   S(  1, -23),   S(-15, -14),
            S(-51,  42),   S(-26,  -3),   S(-18, -11),   S(  5, -24),   S(  8, -15),   S(  2,  -2),   S( -6,   7),   S(-17,  14),
            S(-35,  68),   S(-72,  43),   S(-47,  16),   S(-51,  29),   S( -8,  54),   S(-24,  52),   S(-29,  59),   S(-24,  93),
            S(-45,  86),   S(-109,  99),  S(-109,  48),  S(-59,  26),   S(-50,  59),   S(-27,  55),   S(-14,  48),   S(-36,  97),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-31,  -6),   S(-36,   3),   S(-30,  -6),   S( -4, -58),   S(-16, -21),   S( 38, -23),   S( 73, -53),   S( 44, -72),
            S(-36, -18),   S(-46, -10),   S(-31, -30),   S(-26, -32),   S(-16, -39),   S(  4, -33),   S( 38, -43),   S( 40, -57),
            S(-31, -11),   S(-20, -21),   S( -6, -39),   S( -7, -51),   S(  3, -55),   S( 24, -48),   S( 26, -34),   S( 47, -52),
            S(-40,  24),   S(-17, -22),   S( -5, -29),   S( 11, -43),   S( 29, -53),   S( 14, -29),   S( 14,  -5),   S( 45,  -9),
            S( -7,  46),   S(-31,  14),   S(  2, -12),   S( 19, -11),   S( 71,  -2),   S( 58,  -3),   S( 41,  56),   S( 44,  85),
            S(-21, 110),   S(-24,  54),   S( 20, -22),   S(-16,  -6),   S( 36,  -2),   S( 49,  28),   S( 48,  71),   S( 37, 104),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-108,  37),  S(-20,   2),   S(-37,  17),   S(-14,  17),   S(-26, -22),   S(-35,  10),   S(-57,   6),   S(-57,  -3),
            S(-47,  30),   S( 25,  -1),   S( 19, -23),   S( 28, -37),   S(  2, -34),   S(-49, -14),   S(-16, -28),   S(-15, -25),
            S( 22, -15),   S( 45, -13),   S( 13,  -6),   S( 12, -35),   S(-22, -32),   S( -5, -30),   S(-34, -14),   S(-38,  -4),
            S( 16,  14),   S(  1,  27),   S( 59,  -4),   S( 23,  -9),   S( 30, -31),   S(-37,   1),   S( 10, -13),   S( 31, -11),
            S( -1,  57),   S( 13,  51),   S( 15,  16),   S(-14,   5),   S( 22,  10),   S(-10,  16),   S(-24,  -9),   S( 36,  32),
            S( 66,  73),   S( 70,  78),   S( 29,  16),   S( 32,  17),   S( 17, -14),   S(  8, -15),   S( 18,  14),   S(-23,  31),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-103,  61),  S(-69,  43),   S(-30,  21),   S(  1,  -2),   S(-32,  23),   S(-18,   8),   S(-20,   8),   S(-23,  23),
            S(-73,  41),   S(-62,  29),   S(  9,   0),   S(  0,  10),   S( 20, -15),   S( -6, -19),   S(-17,   0),   S(-38,  23),
            S(-49,  41),   S(-23,  17),   S( 66, -28),   S(  9, -20),   S( 45, -27),   S(-24, -11),   S(  2,   4),   S(-24,  18),
            S(-39,  52),   S(  0,  20),   S( 28,   2),   S( 47,  -1),   S(  9,  -3),   S(-39,   7),   S( 35,  -3),   S( 18,  24),
            S( 66,  35),   S(106,   3),   S( 68,  32),   S( 49,  17),   S(-14,  50),   S( 71,   3),   S( 33,   8),   S( 34,  39),
            S( 95,  17),   S( 82,  16),   S( 63,   7),   S( 51,  14),   S( 52,  13),   S( 28,  14),   S( 20,  16),   S( 36,  29),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-46,  33),   S(-11,  12),   S( -3,   2),   S(-23,   9),   S( -3,  -9),   S(-34,  11),   S(-57,  11),   S(-59,  16),
            S(-42,  22),   S( -3,  -6),   S( -6, -23),   S( 17, -18),   S( 53, -27),   S( 27, -20),   S(-38,   2),   S(-73,  19),
            S(-29,  24),   S( 14,  -2),   S( 13, -20),   S( -3, -16),   S( 35, -22),   S( 62, -32),   S(  6, -11),   S(-38,  13),
            S(-18,  42),   S(-39,  20),   S( 27, -17),   S( 25, -20),   S( 33,  -5),   S( 43,  -5),   S( 39,  -7),   S( 37,   0),
            S(-15,  43),   S(  0,   3),   S( -7,   0),   S( 30, -14),   S( 70,  22),   S( 95,  24),   S( 91,  -8),   S( 99,   8),
            S( 75,  12),   S( 42,  -5),   S( 17, -18),   S( 36, -22),   S( 54,  12),   S( 51,  14),   S( 49,  10),   S( 74,  22),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-55, -14),   S(-59,  -2),   S(-20,  -8),   S(-47, -10),   S(-17, -29),   S( 19, -17),   S(-13, -26),   S(-81,   1),
            S(-58, -20),   S(-45, -27),   S(-34, -30),   S(-15, -46),   S( -2, -40),   S( 41, -42),   S( 39, -33),   S(-45, -10),
            S(-67,  -3),   S(-51, -11),   S(-36, -20),   S( -5, -39),   S( 10, -39),   S( 32, -32),   S( 38, -38),   S(  6, -27),
            S(-42,  10),   S(-56,  -5),   S(-61, -11),   S(-20, -20),   S( 10, -29),   S( 30, -19),   S( 31, -11),   S( 59, -32),
            S(-19,   7),   S(-13, -17),   S(-15, -17),   S(  7, -46),   S( 27,   3),   S( 34,   8),   S( 91,  26),   S(100,  21),
            S( 18,  -5),   S(  7, -38),   S( 28, -50),   S(  9, -38),   S( 24, -27),   S( 42,  -5),   S( 52,  59),   S( 86,  59),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-56,  56),   S(-41,  54),   S(-14,  35),   S(-13, -21),   S(  3,  27),   S(-52,  15),   S(-48,  -1),   S(-48,   3),
            S(-45,  46),   S(-35,  31),   S(-34,  34),   S(-25,   4),   S(-30, -13),   S(-27, -21),   S(-23, -23),   S(-28, -13),
            S(-49,  45),   S(-15,  48),   S(-19,  40),   S(-25,  22),   S( -2,  -9),   S(-51, -16),   S(-35, -23),   S( -4, -19),
            S( 10,  57),   S( 15,  75),   S( 74,  37),   S(  6,  30),   S( -2,   5),   S(-17,  -1),   S(  2,   9),   S(  7,  -7),
            S( 32,  62),   S( 56,  59),   S( 44,  90),   S( 50,  64),   S( 18,   8),   S( 14,  19),   S(  7, -12),   S(  4,  18),
            S( 81,  87),   S( 74, 101),   S( 82, 124),   S( 44,  41),   S(  6,  12),   S(  0, -16),   S( -3, -27),   S( 12,   4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-74,  67),   S(-74,  55),   S(-21,  24),   S(  8,  24),   S(  0,   9),   S(-73,  27),   S(-78,  23),   S(-81,  28),
            S(-53,  44),   S(-24,  21),   S(-59,  33),   S( -3,  29),   S(-67,   1),   S(-67,   6),   S(-87,  11),   S(-51,  22),
            S(-55,  51),   S(-40,  52),   S(-53,  53),   S(-85,  36),   S(-68,  39),   S(-71,   5),   S(-29,  -7),   S(-36,  11),
            S( -3,  61),   S( 53,  49),   S( 39,  67),   S( 59,  60),   S(-34,  36),   S(  6,   0),   S( 48,   4),   S( 61,  -7),
            S( 87,  30),   S( 66,  57),   S( 75,  79),   S( 90,  88),   S( 64,  59),   S( 35,  14),   S( 29, -11),   S( 32,  -5),
            S( 29,  -3),   S( 70,  14),   S( 97,  35),   S(104,  79),   S( 35,  51),   S(  0, -39),   S(  2, -45),   S(  0, -24),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-104,  25),  S(-93,  21),   S( -7,  -4),   S( -1,  16),   S(-21,   1),   S(-99,  45),   S(-118,  41),  S(-75,  41),
            S(-86,  14),   S(-35,  -2),   S(-28,  -1),   S(-54,   4),   S(-40,  21),   S(-34,  22),   S(-132,  40),  S(-107,  36),
            S(-13,   7),   S(-34,   8),   S(-22,  12),   S(-77,  52),   S(-84,  50),   S( -7,  21),   S(-85,  36),   S(-86,  43),
            S( 18,  14),   S(  3,  13),   S(  9,  14),   S( -9,  35),   S( 30,  46),   S(-10,  44),   S( 32,  15),   S( 38,   0),
            S( 60,  -3),   S( 20, -21),   S( 34,  14),   S( 53,  61),   S( 93,  72),   S( 81,  34),   S( 40,  25),   S( 75,  -5),
            S( 46, -25),   S(  5, -62),   S( 19, -20),   S( 58,  54),   S( 50,  42),   S( 49,   6),   S( 35,  -4),   S( 39,  24),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-115,   6),  S(-77,   4),   S(  0,   7),   S( -7,   6),   S( -5,  26),   S(-102,  50),  S(-70,  48),   S(-99,  50),
            S(-74, -23),   S(-57, -32),   S(-41, -28),   S(-63,   7),   S(-45,   3),   S(-30,  18),   S(-108,  59),  S(-102,  46),
            S(-20, -20),   S(-30, -19),   S(-36,   8),   S(-23,   8),   S(-57,  23),   S( -8,  31),   S(-54,  39),   S(-75,  37),
            S( 27,  11),   S(-43,   2),   S(-13,   8),   S(-30,  20),   S(  7,  28),   S( 73,  11),   S( 41,  48),   S( 70,  10),
            S( 15,  20),   S(-23,  10),   S(  1,  20),   S( -8,  16),   S( 64,  56),   S( 53,  49),   S(119,  42),   S(139,   9),
            S( 16, -11),   S(  6, -36),   S(  8, -19),   S(  7, -25),   S( 23,   5),   S( 38,  86),   S( 73,  91),   S(104,  59),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -6,  24),   S(-15,  23),   S(-16,   4),   S(  3,   9),   S( -3,   5),   S(-15,  -2),   S(-20, -16),   S(-31,  -5),
            S(-28,   5),   S(  7,  13),   S(  7,  41),   S( -6,  -9),   S(-10,  29),   S(-15,  -5),   S(-36, -33),   S(-10, -63),
            S(-17,  56),   S( -4,  55),   S( 26,  51),   S( 20,  23),   S(  0,  -2),   S(-28, -19),   S(-26, -39),   S(-28, -57),
            S(-18,  44),   S(  2,  59),   S( 53,  75),   S( 33,  52),   S(-12, -25),   S(-12, -25),   S(  7,   2),   S(-33, -46),
            S( 45,  -3),   S( 47, 124),   S( 57,  88),   S( 18,  37),   S(  5,   0),   S(  3,   0),   S(  9,   2),   S( -6, -37),
            S( 25,  22),   S( 28, 166),   S( 99, 181),   S( 45,  72),   S( -6, -12),   S( -4, -42),   S( -8, -49),   S(-12, -79),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-33,  -5),   S( -7,  18),   S( -4,  11),   S( -1,   1),   S( -7, -14),   S(-26, -10),   S(-28, -27),   S(-32,  -3),
            S(  4, -18),   S( -9,  11),   S( -3,  15),   S( 12,  21),   S(-39,  10),   S( -7, -10),   S(-45, -25),   S(-21, -11),
            S( 10,  17),   S( 10,  -6),   S( -3,  31),   S( 13,  65),   S( -9,  28),   S(-19, -23),   S(-15, -33),   S( -3, -42),
            S(  6,  16),   S( 46,  33),   S( 36,  59),   S( 13,  73),   S( 15,  26),   S( 12, -17),   S( 13,  -9),   S( 21, -52),
            S( 20,   4),   S( 64,  76),   S( 75, 106),   S( 84, 138),   S( 44,  57),   S(  9,   8),   S( 10, -54),   S( 11, -59),
            S( 13,   9),   S( 68,  72),   S( 71, 145),   S( 86, 175),   S( 43,  52),   S(  5, -18),   S(  3, -34),   S( 12, -28),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-35, -35),   S(-29, -21),   S( -9, -23),   S( -1,   2),   S( -5,  -6),   S(-35,  25),   S(-28,  16),   S( -1,  36),
            S(-16,   6),   S(-19, -19),   S(-27, -34),   S( -6,  22),   S(-22,  26),   S( -3,  20),   S(-14,  21),   S(-28,   7),
            S( -8, -23),   S( -9, -13),   S(-17,  -7),   S( -5,  25),   S(-10,  34),   S( -1,  19),   S( -5,   3),   S(  2,  19),
            S( 24, -13),   S( 28,  12),   S( 14, -19),   S( 16,  39),   S(  2,  83),   S( 17,  35),   S( -5,  18),   S( 28,  10),
            S( 16, -37),   S( 24,  -7),   S( 33,  -1),   S( 40,  81),   S( 54, 132),   S( 65,  99),   S( 33,  16),   S( 27,   2),
            S( 19, -36),   S( 22, -23),   S( 35,  46),   S( 40,  73),   S( 60, 174),   S( 33,  97),   S( 24,  44),   S( 12, -12),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-45, -49),   S(-32, -27),   S(-13, -46),   S(  0,   1),   S( 12,  31),   S( -3,  47),   S(-12,  13),   S( 12,  48),
            S( -8, -66),   S(-25, -29),   S(-13, -32),   S(  8,   6),   S(-19,   9),   S( 11,  47),   S( 14,  54),   S( -5,  31),
            S( -3, -51),   S( -4, -48),   S(-12, -30),   S( -5,  14),   S( 12,  29),   S( 21,  24),   S( 19,  62),   S(  4,  53),
            S( 16,  -7),   S(-22,  -5),   S( -5,  -1),   S( 18,  30),   S(  3,  30),   S( 33,  39),   S(  8,  87),   S( -2,  20),
            S( -4, -51),   S(-11, -58),   S( -3,  -5),   S(  6, -18),   S( 37,  85),   S( 86,  83),   S( 30, 175),   S( 46,   4),
            S(  7, -33),   S(  3,  -3),   S(  4,   1),   S(  6,  11),   S( 28,  63),   S( 61, 172),   S(  9, 150),   S( 28, -22),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: bucket 0 */
            S(-39, -16),   S(  2,   4),   S(-34,  10),   S(-29, -30),   S(-45, -20),   S(-14, -25),   S(-57, -58),   S(-36, -26),
            S(-38,  46),   S( 26, -45),   S(-40,  10),   S( -2, -21),   S( -9, -26),   S(-17, -10),   S(-33, -27),   S(-71, -30),
            S( -2,  62),   S( -7,  -6),   S( 14, -21),   S(-15,  20),   S( 27,  -7),   S(-32,   5),   S( -5, -33),   S(-42, -48),
            S( 15, -29),   S( 41,   2),   S( 17,  19),   S( 27,  19),   S( 12, -10),   S(  3,  -3),   S( -3, -21),   S( -9,  -9),
            S( 13, -38),   S( 43, -11),   S(  9,  -3),   S( 63, -29),   S( 37, -17),   S( 32,   1),   S( 21, -20),   S(-62, -23),
            S( 19, -21),   S( 12,   3),   S( 22,   0),   S( 51, -35),   S( 41, -75),   S( 23, -16),   S( 13, -31),   S( -8, -17),
            S(  6, -20),   S( 14, -40),   S( 18, -21),   S( 34, -39),   S( 22, -24),   S( -7, -30),   S(-14, -44),   S(-19, -43),
            S(-76, -58),   S( -6,   5),   S( -2, -15),   S(  3, -45),   S(-19, -21),   S( 20,   9),   S( -5,  -4),   S( 13,  -6),

            /* knights: bucket 1 */
            S(-39,  -9),   S(-43,  65),   S( 21,  26),   S(-25,  48),   S(-25,  37),   S(-33,  23),   S(-37,  34),   S(-16, -26),
            S( 33,  -1),   S(  0,  23),   S(  6,  13),   S( -4,  29),   S( -3,  17),   S( -2,  10),   S( 14, -28),   S(-31,   7),
            S(-31,  16),   S(  6,  12),   S( 14,   6),   S( 27,  16),   S( 25,  17),   S(-23,  24),   S( -7,   8),   S(-31,  18),
            S(  1,  27),   S( 51,  26),   S( 28,  39),   S( 27,  27),   S( 20,  20),   S(  7,  22),   S( 25,   4),   S( 10,  12),
            S(  2,  35),   S( 27,  10),   S( 31,  15),   S( 47,   9),   S( 37,  15),   S( 36,   9),   S( 33,   2),   S( 22,   3),
            S( 15,  12),   S( 23,  10),   S( 25,  17),   S( 43,   0),   S( 17,   1),   S( 45,  10),   S( 28,   5),   S( 17, -12),
            S( 28,  -2),   S( 24,  13),   S(-15, -11),   S( 11,  21),   S( 35, -13),   S( 31,  -4),   S(-33,   9),   S(-16, -26),
            S(-99, -77),   S(-23, -20),   S( -5,  18),   S(  8,  12),   S( -9,  -6),   S(-26, -17),   S( -1, -16),   S(-48, -51),

            /* knights: bucket 2 */
            S(-63,  -6),   S( -6,  17),   S(-33,  42),   S(-33,  42),   S(-41,  50),   S(-35,  57),   S(-27,  39),   S(-29,  -8),
            S(-15, -22),   S(-23,   9),   S( -9,  20),   S( -9,  26),   S(  1,  16),   S( -8,  45),   S(-39,  48),   S(-37,  44),
            S(-23,  27),   S(  0,  17),   S( -9,  35),   S( 22,  19),   S( 13,  25),   S(  0,  18),   S(  0,  36),   S(-28,  25),
            S(-10,  39),   S(-27,  43),   S(  7,  38),   S(  5,  49),   S(  1,  51),   S(  0,  42),   S( -1,  48),   S( -4,  34),
            S( 14,  18),   S(-11,  29),   S(-11,  37),   S(-16,  45),   S( -4,  44),   S( -8,  40),   S(  5,  31),   S( -2,  12),
            S(-23,  33),   S( -3,  39),   S(-25,  42),   S(-16,  36),   S(-23,  32),   S(  2,  23),   S(-41,  23),   S( 16,  -3),
            S(-37,  31),   S(-36,  16),   S(-31,  24),   S(-37,  36),   S(-15,  18),   S(  1,  26),   S(-54,  41),   S(-42,  10),
            S(-154,  -4),  S( -6,   2),   S(-72,  35),   S(-26,   3),   S(  0,   3),   S(-56,   6),   S(  2,  -1),   S(-185, -65),

            /* knights: bucket 3 */
            S(-64, -17),   S( -8, -26),   S(-50,  13),   S(-21,  -1),   S(-16,   1),   S(-15,  12),   S( 11, -12),   S(-21, -23),
            S(-29,  11),   S(-46,  13),   S(-25,   7),   S(  1,  14),   S(  4,   9),   S(-17,   8),   S(-16,  -2),   S(-27,  56),
            S(-16, -14),   S( -7,  15),   S(-10,  25),   S( 11,  20),   S( 17,  29),   S(  7,  19),   S(  2,  20),   S(-10,  41),
            S(-17,  19),   S(-10,  32),   S(  1,  50),   S( 12,  39),   S( 17,  50),   S( 18,  46),   S( 16,  38),   S(  8,  23),
            S(  6,  20),   S( -8,  28),   S( 16,  20),   S( 11,  45),   S(  3,  53),   S( 11,  62),   S( 29,  49),   S(  0,  18),
            S( -8,  21),   S( 13,  12),   S( 36,   3),   S( 39,  11),   S( 42,  -4),   S( 58,  -3),   S( -4,  28),   S( -8,  57),
            S(  4,   4),   S( -8,  22),   S( 29,  -6),   S( 31,   5),   S( 48, -16),   S( 40, -14),   S( 44, -52),   S( 29, -17),
            S(-129,  14),  S(-27,  10),   S(-42,  20),   S(-12,  21),   S( 28,  -8),   S(-16,   1),   S(-12, -18),   S(-83, -53),

            /* knights: bucket 4 */
            S(  8,   4),   S(-52, -24),   S( 14,  15),   S(-12, -17),   S(-28, -18),   S(-35, -19),   S(-11, -62),   S(-31, -48),
            S( 34,  22),   S(-30,  30),   S( 10, -13),   S(  7, -11),   S( 21, -28),   S(-11, -35),   S( 11,  -9),   S( -7, -45),
            S(-13,  33),   S(  6,  41),   S( 40, -18),   S( 37,  -1),   S( 13,  -5),   S(-26,   9),   S(-39, -37),   S(-26, -63),
            S(  8,  44),   S( 39, -18),   S( 85, -13),   S( 41,  13),   S( 42,  -3),   S(110, -30),   S( 25, -28),   S(  0, -24),
            S( 50,  24),   S(-21,  37),   S( 54,  24),   S( 48,   0),   S( 52,  18),   S( -9,   4),   S( -2, -31),   S(-11, -22),
            S(  6,  14),   S(-25,  -8),   S( 79,  -1),   S( 21, -13),   S( 22,  -8),   S( 27,  -3),   S( 15,  24),   S(-15, -25),
            S(-10,   3),   S(-12,   7),   S(  9,   4),   S( 13,  23),   S( 13,   0),   S( 11, -20),   S(  3, -19),   S(-15,  -8),
            S(-12,  -4),   S( -1, -11),   S( 11,  17),   S(  0,  -4),   S( -5,  -3),   S( 12,  22),   S( -1,   5),   S( -5, -18),

            /* knights: bucket 5 */
            S(  8, -19),   S(-37,  20),   S( 28,  25),   S( 10,  38),   S( 33,   6),   S( 14,  -6),   S( -5,   7),   S(-25, -32),
            S(  8, -18),   S( 32,  20),   S( 26,  16),   S(  4,  28),   S( 49,  19),   S( 16,  24),   S( 24,  18),   S(-18, -29),
            S( 10,  14),   S( -1,  36),   S( 88,  -2),   S( 93,   3),   S( 18,  30),   S( 23,  17),   S(  4,  13),   S(  7,   3),
            S( 32,  40),   S( 33,  32),   S( 72,  12),   S( 34,  32),   S( 65,  16),   S( 50,  29),   S( 39,  39),   S( 23,  27),
            S( 10,  38),   S( 43,  17),   S( 65,  29),   S( 86,  19),   S( 93,  26),   S( 46,  24),   S( 47,  29),   S( 32,  25),
            S( -6,  28),   S( -2,  47),   S( 31,  15),   S( 28,  38),   S( 44,  30),   S( 20,  37),   S( 21,  13),   S( -4,  22),
            S( 17,  42),   S( -4,  52),   S( 36,  41),   S( 21,  59),   S(  6,  51),   S( 10,  45),   S( 24,  60),   S(  1,   1),
            S( -5,   0),   S(  1,  16),   S( 12,  39),   S( -3,   5),   S( 10,  33),   S(  3,  37),   S( 12,  38),   S(-19, -19),

            /* knights: bucket 6 */
            S( -7, -41),   S(-29,  -1),   S( 30,  25),   S(-37,  35),   S(-25,  39),   S(  9,  37),   S(-12,  10),   S(-14,  15),
            S( -2, -35),   S( 53,   1),   S( 33,   4),   S(-12,  26),   S(-30,  49),   S( 52,  33),   S( 22,  26),   S( -8,  -6),
            S(-18, -15),   S( 12,   4),   S( 41,   7),   S( 77,   4),   S( 30,  27),   S(  2,  32),   S( 32,  44),   S( 10,  40),
            S( 43,   8),   S( 48,  11),   S( 99,   7),   S(107,   3),   S( 74,  15),   S( 68,  25),   S( 26,  57),   S( -9,  56),
            S(  6,  28),   S( 74, -11),   S( 83,  12),   S( 88,  12),   S(114,  14),   S(120,  12),   S( 35,  43),   S( 20,  41),
            S( 16,  26),   S( 23,  11),   S( 71,   7),   S( 65,  27),   S( 66,  35),   S( 42,  24),   S( 28,  32),   S( 39,  36),
            S(-20,   9),   S( -1,  24),   S(-27,  37),   S( 28,  32),   S( 10,  48),   S( 19,  46),   S( 20,  67),   S(-10,  21),
            S(-41, -12),   S( 11,  41),   S( 35,  35),   S(  7,  38),   S( 21,  33),   S( 15,  53),   S( 21,  53),   S( 11,  14),

            /* knights: bucket 7 */
            S(-34, -62),   S(-200, -40),  S(-73, -37),   S(-57, -19),   S(-27, -16),   S(-37, -16),   S(-15,  -7),   S(-19, -16),
            S(-45, -76),   S(-32, -42),   S(-28, -27),   S(-36,   2),   S(-37,   9),   S( 19, -15),   S( -1,  27),   S(  7,  19),
            S(-75, -51),   S(-54, -20),   S(-23,  -8),   S( 54, -31),   S( 26, -12),   S( 39, -14),   S(  2,  53),   S( 52,  50),
            S(-54, -11),   S( 30, -27),   S( 32,  -8),   S( 67, -15),   S( 88, -14),   S( 43,  -2),   S( 30,  16),   S( -7,  35),
            S(-55, -20),   S(-11, -31),   S( 69, -38),   S( 93, -30),   S(127, -23),   S( 82,   8),   S(102,  -1),   S( 77,  10),
            S( -4, -34),   S( 13, -24),   S(  3, -18),   S( 43, -14),   S( 81,  -9),   S( 97, -11),   S( 67, -16),   S( -5,  18),
            S(-32, -42),   S(-63, -21),   S( 19, -22),   S( 40,  17),   S( 42,  19),   S( 50,  -1),   S(-15,  20),   S(  3,   4),
            S(-41, -31),   S( -9, -12),   S(-16, -15),   S( 12,  10),   S( 12,   4),   S( 27,  20),   S( -3, -14),   S( -6,  -5),

            /* knights: bucket 8 */
            S( -1,  -5),   S(-10, -16),   S( -3,  12),   S( -7, -25),   S( -9, -32),   S( -8, -35),   S( -4,  -3),   S( -6, -27),
            S(  2,   6),   S( -5,  -9),   S( -6, -16),   S(-23, -27),   S(-26, -18),   S(-17, -59),   S(-12, -52),   S(-15, -31),
            S(  7,  28),   S(-20, -15),   S( 16,  19),   S( -3, -10),   S( -2, -27),   S(-17,  -5),   S(-11, -26),   S( -6, -30),
            S(-17,   3),   S( -6,   8),   S(  1,   6),   S(  4,  32),   S( 10,  -2),   S(  6,  -8),   S(-11, -40),   S( -2,  -7),
            S( 25,  73),   S(  4,  20),   S( 17,  19),   S( 28,  34),   S(  9,  23),   S( -2, -10),   S(  7,  -4),   S( -7, -10),
            S( 11,  46),   S(  2,   1),   S( 27,  16),   S( 36,   9),   S(  2,  -2),   S(  0,  -8),   S( -4, -23),   S( -5,  -1),
            S(  4,  13),   S(  0,  10),   S(  7,   4),   S( 13,  13),   S(  6,   9),   S(  7,  15),   S(  1,  12),   S( -3,  -3),
            S(  3,   4),   S( 15,  36),   S(  7,  26),   S(  1,  14),   S(  6,  24),   S( -3, -14),   S(  3,  16),   S( -3,  -3),

            /* knights: bucket 9 */
            S(-10, -44),   S(-19, -32),   S(-17, -49),   S( -4,   0),   S(-21, -51),   S(-15, -35),   S( -4, -18),   S( -4, -24),
            S(-11, -36),   S( -9,  -1),   S(-12, -46),   S(-14,  -3),   S( -7,  -6),   S( -3, -21),   S( -2,   6),   S(-14, -46),
            S(  5,  14),   S( -6, -19),   S( -2, -23),   S(  0,  -4),   S(  5,  11),   S(-31, -12),   S(-12,   4),   S( -9,  -6),
            S(-12, -13),   S( -8,  -7),   S( 11,   2),   S( 25,  -2),   S( 31,  10),   S( 10,  22),   S( -7, -24),   S(  2,   5),
            S(  0,  13),   S( 17,  10),   S( 17,  13),   S(  7,   9),   S( 17,   2),   S(  9, -15),   S(  0, -21),   S(  4,  11),
            S(  1,  12),   S(  7,  32),   S(  7,  18),   S(  5, -14),   S( 29,  21),   S( 10,  16),   S(  7,  34),   S( -6, -13),
            S(  1,   5),   S( -4,  19),   S( 19,  41),   S(  3,  33),   S( 15,  49),   S(  1,  -6),   S(  5,  22),   S( -2,   6),
            S( -2,  -7),   S(  6,  20),   S( 13,  35),   S( 13,  49),   S(  9,  13),   S(  3,  17),   S(  3,  15),   S(  1,  -3),

            /* knights: bucket 10 */
            S(-18, -56),   S(-15, -49),   S(-11, -34),   S(-18, -15),   S( -9, -14),   S(-14, -40),   S( -4,   0),   S(  3,   8),
            S( -6, -38),   S( -6, -26),   S( -2,  -3),   S(-18, -28),   S(-24, -36),   S( -4, -37),   S( -9, -12),   S( -6, -29),
            S(-15, -46),   S(-17, -53),   S(-12, -23),   S( -8, -28),   S(  5,  -5),   S( -7, -24),   S( -4,   0),   S( -8, -11),
            S( -8, -15),   S( -5, -29),   S(  4, -35),   S( 21,  -7),   S( 29,  -1),   S( 18,  -9),   S( -1,  18),   S( 10,  39),
            S( -9, -39),   S(-15, -27),   S(  8,   2),   S( 33,   3),   S( 29,   6),   S( 13,  -6),   S( 14,  25),   S( 19,  49),
            S(-11, -27),   S( -5, -11),   S( -6,  -9),   S( 18,  16),   S( 32,  32),   S( 13,  25),   S( 26,  62),   S( 15,  55),
            S(  1,   1),   S(-12, -19),   S(  0,   5),   S( 20,  43),   S( 11,  40),   S( 10,  39),   S(  1,  -8),   S(  8,  27),
            S( -4, -19),   S(  5,  17),   S( -4, -10),   S(  4,  16),   S( 12,  50),   S(  7,  41),   S(  2,  15),   S( -1,   0),

            /* knights: bucket 11 */
            S(  0,   6),   S(-18, -29),   S(-12, -39),   S(-10, -25),   S(-19, -36),   S(-13, -17),   S( -7,  -9),   S( -6, -14),
            S( -8,  -3),   S(-10, -19),   S(-11, -63),   S(-25, -25),   S(-11,   4),   S(-25, -31),   S(-16, -19),   S( -8,  -3),
            S(-13, -42),   S(-21, -31),   S(-30, -31),   S(  5,   0),   S(-16,  -6),   S(-12,   9),   S(  6,   6),   S(  4,  18),
            S(-11, -19),   S(  0, -10),   S(-18,  -1),   S( 20,  26),   S( 18,   2),   S( 11,   4),   S(  5,  34),   S( 11,  35),
            S( -1, -10),   S(-23, -45),   S(  4, -20),   S(  9, -13),   S(  8,   5),   S( 39,  20),   S(  1,  -5),   S( 26,  83),
            S( -7,   0),   S( -5, -15),   S( -1,   5),   S( 36,  25),   S( 16,  11),   S( 42,  36),   S( 10,  30),   S( 10,  44),
            S(  6,  35),   S( -2,  -1),   S(  5,   2),   S( 10,  10),   S( 16,  42),   S( -2,  18),   S(  9,  44),   S( 15,  72),
            S( -3,  -4),   S( -1, -11),   S(  9,  23),   S(  2,   9),   S(  2,  17),   S(  5,  18),   S(  3,   9),   S(  3,  21),

            /* knights: bucket 12 */
            S( -2,  -9),   S(  0,   1),   S( -2, -16),   S( -2,   8),   S( -3,  -8),   S( -2,   0),   S(  3,   9),   S( -2, -11),
            S(  0,   1),   S(  0,   2),   S(  3,  12),   S( -2,  -6),   S(  1,  10),   S( -4, -19),   S( -1,  -8),   S(  2,   7),
            S( -4, -12),   S(  5,   7),   S( -5, -14),   S( -6, -17),   S(  2,   0),   S( -2, -12),   S(  1,  -1),   S( -6, -22),
            S( -5, -12),   S(  0,   1),   S( -5, -19),   S(  3,  29),   S( -4,   1),   S(  2,   1),   S(  1,   3),   S(  1, -10),
            S(  9,  23),   S(  4,   3),   S( -6, -13),   S(  1,   3),   S( -3, -16),   S(  0,  -6),   S( -1,  -6),   S(  0,   3),
            S(  1,  22),   S( -5, -15),   S( -2,  48),   S(  1,  -6),   S(  5,   0),   S( -3, -18),   S(  0,  -6),   S(  0,   4),
            S(  2,  14),   S(-12, -15),   S(  2,   8),   S(  1,  13),   S( -3, -10),   S( -5, -15),   S(  0,  -1),   S( -1,  -2),
            S(  2,   9),   S(  3,  23),   S( -3,  -6),   S(  3,  14),   S( -1,  -1),   S( -2, -10),   S( -2,  -8),   S(  0,   2),

            /* knights: bucket 13 */
            S( -3,  -9),   S(  0,   5),   S( -1,  -3),   S(  0,   1),   S( -7, -18),   S(  1,   3),   S( -2,  -7),   S(  1,   3),
            S( -2, -10),   S(  1,  10),   S(  0, -16),   S( -7, -19),   S( -3, -20),   S( -2, -15),   S(  1,   4),   S(  1,  -4),
            S( -3, -10),   S( -7, -20),   S(  4,  15),   S(  3,  -6),   S(-10, -17),   S( -7, -13),   S( -1,  -7),   S( -4, -17),
            S( -7,  -9),   S(  9,  24),   S(  1,   4),   S( -9, -19),   S(  3,  -1),   S(  8,  19),   S(  1, -10),   S( -3,  -4),
            S(  3,  10),   S( -1,  14),   S(  5,  -7),   S( 10,  32),   S(  3,  17),   S( -3,   0),   S(  3,   6),   S(  2,   7),
            S( -2,   6),   S( 13,  31),   S(  1,  58),   S( -9,  14),   S(  4,  20),   S( -8, -28),   S(  5,  17),   S( -2,  -4),
            S(  2,   7),   S(  3,  25),   S(  9,  27),   S(  6,  43),   S( 12,  56),   S( -4, -12),   S( -2,   5),   S( -4,  -7),
            S(  0,   1),   S(  3,  40),   S(  1,  13),   S(  2,  19),   S(  0,  19),   S(  2,  12),   S(  0,   5),   S(  0,   0),

            /* knights: bucket 14 */
            S( -3, -20),   S( -5, -22),   S( -2,  -1),   S(  0,  11),   S( -7, -31),   S( -1, -11),   S(  0,   0),   S(  1,  -1),
            S(  0,  -6),   S( -2, -11),   S(-15, -54),   S( -5, -31),   S(  2,  -8),   S(  3,   4),   S(  2,   6),   S( -2,  -4),
            S( -3,  -5),   S( -2, -20),   S(-12, -40),   S(  4,  12),   S( -1, -10),   S( -1,  -8),   S(  1,   7),   S(  2,   7),
            S(  1,   9),   S( -4, -18),   S(-12, -52),   S( -6, -19),   S( -1, -24),   S(  6, -11),   S( -1,  -5),   S( -6, -10),
            S( -2,  -2),   S( -2, -10),   S(  3,  12),   S( -6, -23),   S( -9, -19),   S(  5,  -7),   S(  2,  10),   S( -2, -16),
            S( -2,  -9),   S(  4,   7),   S( -6,  -4),   S( 10,   9),   S( 13,  35),   S(  0,  14),   S(  1,  18),   S(  3,  19),
            S(  0,  -1),   S( -2,  -7),   S(  7,  11),   S(  0,   9),   S( -4,  40),   S( -4,  11),   S( -3,  14),   S(  1,  13),
            S(  0,  -2),   S(  2,  12),   S(  0,   4),   S(  7,  37),   S( 10,  31),   S(  4,  27),   S(  0,  21),   S( -1,  -1),

            /* knights: bucket 15 */
            S( -2,  -6),   S(  0, -14),   S(  1,  -9),   S( -6, -14),   S( -2,  -4),   S(  0,  -9),   S(  1,  -1),   S(  0,   4),
            S( -2, -10),   S(  2,   2),   S( -3, -11),   S( -3, -21),   S(  2,   5),   S(  1,   1),   S(  0,  -2),   S( -1,  -3),
            S( -6, -18),   S( -5,  -7),   S( -3, -15),   S(-10, -41),   S( -5, -18),   S(  1,  -1),   S( -2,  -4),   S( -1,  -4),
            S( -8, -20),   S( -4, -24),   S( -4, -31),   S( -1,  -2),   S(  0,  -9),   S(  9,  22),   S(  6,  16),   S( -2,   0),
            S(  0,   2),   S( -2,   2),   S( -2, -19),   S( -5, -23),   S(  5,  -2),   S(  5,  -2),   S( -6,  -6),   S(  0,   6),
            S( -2,   0),   S( -2,  -7),   S( -1, -11),   S(  0,   7),   S( -4, -15),   S( -4,  11),   S(  0,   7),   S(  4,  34),
            S( -3, -13),   S( -1,   1),   S(  0,   0),   S( -2,  -3),   S( -7, -10),   S( -1,  13),   S( -2, -11),   S(  3,  27),
            S(  0,  -4),   S(  0,   3),   S( -3,  -7),   S(  0,  -3),   S( -1,   0),   S( -7, -17),   S(  8,  39),   S(  0,  -1),

            /* bishops: bucket 0 */
            S( 20,  16),   S( 14, -14),   S( 51,   5),   S( -3,  26),   S(-18,   7),   S(  9, -19),   S(  5, -40),   S(  6, -48),
            S( 46, -46),   S( 87,   5),   S( 31,  14),   S( 10,  14),   S( -6,  31),   S( -8,   7),   S(-31,  13),   S(  2, -31),
            S( 35,  15),   S( 44,  17),   S( 35,  30),   S( 10,  53),   S( 13,  33),   S(-16,  51),   S(  0,  -4),   S( 14, -39),
            S( 26,   5),   S( 69,   2),   S( 38,  18),   S( 33,  24),   S( -8,  43),   S( 25,  17),   S(-12,  27),   S( -3,  21),
            S(  9,  19),   S( 37,   7),   S( -7,  49),   S( 47,  14),   S( 36,  19),   S( -3,  31),   S( 13,   8),   S(-47,  34),
            S(-40,  65),   S(-30,  52),   S( 41,  25),   S( 34,  34),   S( 17,  50),   S( -6,  21),   S(-26,  38),   S(-13,  49),
            S(-53,  74),   S(  4,  28),   S(  5,  40),   S(-24,  69),   S(-60,  32),   S( 22,  31),   S( -8,  15),   S(-30,  12),
            S(-32, -17),   S( -2,  45),   S(-11,  42),   S(-11,  35),   S( 14,  48),   S( 26,  39),   S( -9,  53),   S(-19,  39),

            /* bishops: bucket 1 */
            S( 21,  39),   S( -4,  19),   S(  1,  29),   S( -1,  26),   S(  2,  16),   S( -6,  18),   S(-24,  25),   S(-51,   8),
            S( 17, -14),   S( 40,   9),   S( 50,   8),   S( 26,  25),   S( -4,  21),   S( 12,  14),   S(-24,  22),   S( 14,  -1),
            S( 39,   6),   S(  9,  22),   S( 46,  29),   S( 16,  39),   S( 18,  40),   S( -8,  43),   S( 24,  16),   S(  1,  -7),
            S( 46,  10),   S( 24,  36),   S(  7,  37),   S( 32,  35),   S( -5,  46),   S( 19,  29),   S(-16,  48),   S( 19,   8),
            S( 41,  36),   S( 13,  36),   S( 25,  29),   S( -2,  33),   S( 15,  23),   S(-15,  47),   S( 32,   5),   S( -7,  29),
            S(-13,  46),   S( 25,  42),   S( 27,  43),   S( 23,  33),   S(-11,  54),   S( 15,  41),   S(-20,  50),   S( 43,  15),
            S(-21,  55),   S(-22,  49),   S( 12,  45),   S( 10,  60),   S( 20,  53),   S(-43,  54),   S(-11,  58),   S(-35,  57),
            S(  8,  58),   S(-15,  41),   S(-13,  46),   S(-26,  54),   S(  3,  53),   S(-26,  47),   S(-14,  56),   S(-32,  97),

            /* bishops: bucket 2 */
            S( 16,   7),   S(  0,  28),   S( -9,  19),   S(-24,  41),   S(-21,  37),   S(-24,  19),   S(-26,  -8),   S(-48,  36),
            S(-11,  23),   S( 16,  13),   S( 23,  22),   S(  1,  34),   S(  1,  31),   S( 11,  13),   S(  5,  -4),   S(  9, -28),
            S( -6,  27),   S( -2,  33),   S( 12,  55),   S(  2,  55),   S(  3,  50),   S( 12,  46),   S(  7,  26),   S(-16,   8),
            S( 11,  38),   S(-28,  64),   S(-21,  60),   S( -1,  53),   S( -2,  50),   S( -3,  54),   S(  6,  44),   S( 11,  12),
            S( -2,  46),   S( -3,  39),   S(-20,  47),   S(-30,  48),   S(-25,  54),   S( -9,  59),   S( 10,  32),   S(-24,  38),
            S(  3,  41),   S(-28,  47),   S(-26,  66),   S(-43,  58),   S( -4,  43),   S(-26,  61),   S(  0,  60),   S( -5,  45),
            S(-21,  49),   S(-34,  56),   S(-49,  81),   S(-14,  50),   S(-60,  72),   S(-41,  50),   S(-84,  65),   S(-50,  56),
            S(-84,  94),   S(-68,  83),   S(-62,  71),   S(-88,  80),   S(-61,  69),   S(-71,  74),   S(-10,  46),   S(-70,  62),

            /* bishops: bucket 3 */
            S(-14,  29),   S(  7,  19),   S(  6,  15),   S( -4,  36),   S( -5,  31),   S( 43,  -3),   S( 30, -28),   S( 33, -47),
            S( -1,  25),   S(  2,  35),   S( 18,  18),   S( -2,  50),   S(  8,  35),   S(  4,  37),   S( 42,  21),   S( 22,  -4),
            S(  9,  32),   S( -2,  49),   S(  4,  69),   S( 13,  53),   S(  7,  74),   S( 14,  59),   S( 26,  36),   S( 33,   4),
            S( 19,  35),   S( -5,  62),   S( -5,  73),   S(  6,  65),   S( 10,  60),   S( 14,  53),   S( 15,  53),   S( 12,  19),
            S( -9,  56),   S( 15,  34),   S( 12,  41),   S(  2,  64),   S(  0,  56),   S( 19,  51),   S(  9,  41),   S( 16,  56),
            S(  0,  52),   S( -1,  54),   S(  3,  63),   S(  4,  45),   S(-12,  56),   S( 28,  52),   S( 21,  47),   S(-11,  87),
            S(-18,  49),   S(-32,  67),   S( 11,  50),   S(-17,  61),   S(-20,  55),   S(-25,  58),   S(-17,  70),   S( -7,  70),
            S(-55, 103),   S(-19,  60),   S( 25,  48),   S(-23,  73),   S(-47,  84),   S(-47,  89),   S(-14,  56),   S( 65,  21),

            /* bishops: bucket 4 */
            S(-19, -22),   S(-27,  12),   S(-51,  16),   S(-40,  38),   S(-13,  17),   S(-44,  22),   S(-13,  11),   S( -6, -14),
            S(-29,  -7),   S( 26,  10),   S(-12,  33),   S(-21,  22),   S(-19,  17),   S( 63, -10),   S(-12,   9),   S( 16,   0),
            S(-18,  -4),   S(-27,  21),   S( 28,  10),   S(-13,  31),   S( 25,  25),   S( 47,   9),   S(-14,  -5),   S(-50,  17),
            S(-42,  25),   S(  4,  37),   S( 79,  11),   S( 63,  10),   S( 19,  29),   S( 47,  17),   S( 30,  37),   S( -6,   0),
            S( 15,  19),   S(  2,  38),   S( -8,  45),   S( 21,  27),   S( 20,  10),   S(  2,  20),   S(-42,  15),   S( -1,  31),
            S( -5,  30),   S( 18,  19),   S(  9,  33),   S( 26,  18),   S( 25,  31),   S( 16,  15),   S(  4,   0),   S( -4,  14),
            S(-25,  24),   S( 34,  28),   S(  5,  32),   S(  0,  52),   S(  3,  22),   S(  4,  26),   S(  8,   0),   S( -5,  -7),
            S(  6,  25),   S(-16,   3),   S(  0,  22),   S( -4,  26),   S(  0,  29),   S( -3,  15),   S( -3,   4),   S( -5,  21),

            /* bishops: bucket 5 */
            S(-27,   1),   S(-11,  27),   S(-57,  51),   S( -9,  42),   S(-23,  41),   S(-20,  18),   S(-13,  25),   S(-24,  22),
            S(-27,  30),   S(-27,  41),   S(-27,  59),   S( 25,  32),   S(-11,  39),   S(  4,  28),   S(-20,  19),   S(-16,  19),
            S(-15,  45),   S(-10,  57),   S( 41,  38),   S( 24,  34),   S( 33,  38),   S( -2,  32),   S(-29,  49),   S( -4,  12),
            S( 33,  40),   S( 43,  43),   S( 16,  44),   S( 59,  18),   S( 57,  23),   S( 44,  34),   S(-10,  39),   S(  8,  49),
            S( 43,  49),   S( 25,  26),   S( 72,  28),   S(112,  11),   S( 67,  12),   S( 53,  23),   S( 31,  31),   S(-31,  39),
            S( 18,  40),   S( 48,  38),   S( 64,  35),   S( 32,  42),   S( -5,  51),   S( 15,  25),   S( -4,  38),   S( -3,  54),
            S(  2,  45),   S(-30,  45),   S( 26,  45),   S( 28,  57),   S(  7,  62),   S(  9,  64),   S(  2,  42),   S(  1,  22),
            S(-14,  45),   S( 16,  41),   S(  9,  36),   S(  5,  58),   S( 21,  55),   S(  1,  53),   S(  7,  77),   S( -1,  18),

            /* bishops: bucket 6 */
            S(-28,  44),   S( -8,  34),   S(-58,  45),   S(-32,  40),   S(-44,  55),   S(-55,  57),   S(-10,  37),   S(-24,   5),
            S(  7,  27),   S( 12,  22),   S(  0,  38),   S(  0,  45),   S( -2,  38),   S( -7,  32),   S(-99,  58),   S( 10,  27),
            S( 22,  21),   S(  2,  32),   S( 51,  36),   S( 52,  29),   S( 77,  14),   S( 51,  27),   S(  0,  50),   S(-57,  51),
            S( 16,  51),   S( 15,  52),   S( 46,  33),   S( 77,  15),   S( 65,  20),   S( 64,  22),   S( 21,  52),   S(-20,  35),
            S(-19,  58),   S( 30,  35),   S( 63,  18),   S( 59,  21),   S(123,  10),   S( 82,  20),   S( 37,  36),   S(-22,  45),
            S(  3,  36),   S( 15,  29),   S( 37,  36),   S( 33,  46),   S( 40,  41),   S( 63,  38),   S( 39,  32),   S( -8,  49),
            S(-21,  35),   S( -8,  40),   S( 21,  41),   S(  0,  48),   S( 32,  51),   S( 15,  47),   S( 10,  51),   S(-20,  49),
            S(  1,  60),   S( -2,  57),   S(  1,  54),   S(  1,  65),   S( -2,  48),   S( 12,  59),   S(  7,  37),   S(  3,  56),

            /* bishops: bucket 7 */
            S(-26,   7),   S(-19,  24),   S(-48,   3),   S(-38,  17),   S(-40,  14),   S(-70,  31),   S(-68, -26),   S(-46, -12),
            S(-46,  11),   S(-63,  13),   S(-17,  15),   S(  7,  13),   S(-15,  15),   S(-42,  25),   S(-23, -10),   S(-45, -27),
            S(-45,  27),   S( 12,  -2),   S( 25,  19),   S( 37,  14),   S( -9,  26),   S( 14,   6),   S( -2,  22),   S(-16,  13),
            S(-17,  36),   S( 30,  25),   S( 95, -10),   S( 91,  -4),   S(114, -10),   S( 34,  13),   S( 45,  32),   S( -4,  26),
            S(-10,  18),   S(-12,   3),   S( 48,  -6),   S(110, -28),   S( 97,  -4),   S( 93,   2),   S( 18,  28),   S( 38,   4),
            S(-16,  11),   S(-14,  12),   S( 31,  -4),   S( 33,   5),   S( 43,   8),   S( 74,  17),   S( 63,  11),   S(  8,  26),
            S( -2,   8),   S(-11,   7),   S( -3,  18),   S(  8,  19),   S(  6,  11),   S( 30,  11),   S( 32,  17),   S(  0,  30),
            S( -4,  16),   S(-15,  21),   S(-24,  22),   S( -9,  27),   S( 11,  19),   S(  8,  27),   S( 19,  28),   S( 15,  29),

            /* bishops: bucket 8 */
            S( -6, -42),   S(-10, -58),   S(-42, -21),   S(  1,  -7),   S(  3,   7),   S(-20, -20),   S(  7,   7),   S( -4,   4),
            S( -8, -22),   S(-24, -59),   S( -6, -28),   S( -8, -13),   S( 19,  -4),   S( -6, -33),   S( -8, -42),   S(  1, -15),
            S( -1, -14),   S(-11,  -3),   S(  2, -11),   S( 24, -35),   S( 10,  -9),   S( 10, -23),   S(  3, -33),   S(-32, -36),
            S(  2,  25),   S( 10,   2),   S( 18,  10),   S( 17, -19),   S( 21, -17),   S( 13, -30),   S(  1, -17),   S( -1, -21),
            S(  9,  29),   S( 15,  22),   S( 19, -19),   S( 60, -12),   S( 27, -37),   S( 18, -11),   S(  1, -33),   S( -6, -21),
            S( -8,  -3),   S( 12,   1),   S( 14,   9),   S(  5, -26),   S( 22,  -8),   S(  5, -32),   S( -4, -55),   S(-19, -45),
            S( -9, -14),   S( 20,   3),   S( 13,  -4),   S(  2, -16),   S(  4,  -6),   S(  4, -23),   S( -3, -39),   S(-12, -32),
            S( -6, -10),   S( -1, -37),   S(  2, -14),   S( -2, -15),   S(-11, -37),   S(  0, -33),   S( -1, -20),   S( -6, -38),

            /* bishops: bucket 9 */
            S(-16, -58),   S(  7, -37),   S(-26,   1),   S(-10, -14),   S(-19, -31),   S( -7, -39),   S(-14, -40),   S(  7,  11),
            S(-10, -48),   S(-18, -44),   S( -2, -17),   S( 11, -11),   S(-12,  -8),   S( -6, -25),   S( -2, -21),   S( -1,  -9),
            S(  5, -14),   S( 15,  -6),   S( 20, -32),   S( 33, -24),   S( 33, -22),   S( 25, -26),   S(-14, -23),   S(  8,  -7),
            S( -8,   1),   S( 20,   3),   S( 16, -12),   S( 62, -40),   S( 50, -15),   S( 25, -14),   S( 23, -22),   S( -8, -27),
            S(  4,   7),   S( 23,   1),   S( 33,   1),   S( 41,  -7),   S( 42, -45),   S( 32, -21),   S( 10, -13),   S( -6, -13),
            S( -6, -31),   S( 34,   5),   S(  5,  17),   S( 26,   1),   S( 40, -19),   S( 27, -32),   S(  9, -51),   S(-11, -31),
            S( -2, -17),   S( 22,   5),   S(  7, -12),   S( 23, -10),   S( 26,  -2),   S( 13, -30),   S(  2, -23),   S( -4, -33),
            S( -5, -26),   S( -5, -17),   S(  5, -15),   S( -6, -41),   S( -3, -23),   S(  9,   3),   S(  2, -21),   S(-10, -48),

            /* bishops: bucket 10 */
            S(-17, -37),   S(  5, -44),   S(-40, -27),   S(-10, -31),   S(-14, -15),   S(-17, -34),   S( -6, -44),   S(-13, -63),
            S(  3, -25),   S(-14, -43),   S( 14, -37),   S( -9, -30),   S( -5, -29),   S(  9, -28),   S(-14, -67),   S(-12, -34),
            S(  3, -25),   S( 14, -29),   S(  4, -51),   S( 42, -36),   S( 45, -45),   S( 11, -20),   S(-17,  -8),   S( -3,  -7),
            S( -2, -23),   S( 24, -19),   S( 34, -35),   S( 53, -40),   S( 62, -44),   S( 20,  -9),   S(  6,   1),   S( 12,  24),
            S(-11, -19),   S( 20, -31),   S( 46, -46),   S( 84, -43),   S( 48, -22),   S( 41,  -8),   S( 19,  -6),   S( -2, -23),
            S( -1, -23),   S( 14, -54),   S( 18, -41),   S( 29, -34),   S( 43, -12),   S( 43,  11),   S( 23,  -8),   S( -1, -21),
            S(-15, -73),   S(  4, -55),   S(  0, -48),   S( 25, -16),   S(  4, -30),   S( 23,  -5),   S( 18,  17),   S(  6,   2),
            S( -2, -50),   S( -7, -43),   S(  3,  -4),   S(  0, -17),   S( -2, -11),   S(  1, -30),   S(  5,  -2),   S(  5,  10),

            /* bishops: bucket 11 */
            S(-14,   2),   S(-37, -16),   S(-55, -31),   S(-20, -26),   S(-13, -10),   S(-55, -50),   S( -8, -23),   S(-22, -58),
            S( -9, -36),   S( 11, -28),   S( -3,  -9),   S(-18, -29),   S(-20, -30),   S(-17, -44),   S(-15, -51),   S(-24, -47),
            S( -2, -41),   S(  2, -48),   S(  4, -32),   S( 33, -36),   S( 12, -24),   S( 19, -29),   S( -7,   3),   S( -4,  -9),
            S( -9, -34),   S( -3, -34),   S( 34, -42),   S( 41, -58),   S( 68, -45),   S( 38,  -4),   S( 33, -12),   S(  8,  31),
            S( -3, -26),   S( -2, -50),   S( 18, -34),   S( 69, -48),   S( 53, -30),   S( 47, -13),   S( 15,  24),   S( 14,  -1),
            S(-11, -64),   S(  3, -60),   S( 12, -49),   S( 21, -24),   S( 29, -23),   S( 40,  -5),   S( 15,  19),   S(-17, -28),
            S( -8, -44),   S(  4, -60),   S( -2, -36),   S(  6, -44),   S( 10, -17),   S( 30, -14),   S( 11, -29),   S(  6,   5),
            S(-12, -68),   S(-16, -45),   S(  1, -26),   S( 13,  -9),   S( 14, -20),   S(-11, -46),   S(  3, -16),   S( -3, -18),

            /* bishops: bucket 12 */
            S(  0,  -3),   S( -4, -18),   S(-10, -44),   S( -2, -13),   S( -2, -12),   S( -9, -13),   S(  1,  10),   S(  1,   7),
            S( -5, -28),   S(-12, -42),   S( -4, -33),   S( -3, -14),   S( -7, -29),   S(  1,   1),   S(  3,  -1),   S(  0,  -9),
            S( -1, -17),   S(-12, -35),   S( -5, -21),   S( -3, -32),   S( -4, -11),   S(  1, -30),   S(-11, -37),   S( -1,  -1),
            S(  1,  -5),   S(  4, -12),   S( -8, -43),   S(  1, -17),   S(  2, -22),   S(  8,  16),   S( -1, -15),   S( -4, -10),
            S( -2,  -2),   S(  1,  -8),   S(  4, -21),   S( -9, -18),   S(  0, -44),   S( -3, -10),   S(  2, -20),   S( -4,  -9),
            S(-14, -22),   S( 11,  30),   S( -9, -15),   S( -5, -38),   S(  4, -22),   S( -2, -13),   S(  3, -21),   S(  0,  -7),
            S( -2,  -3),   S( -6,   0),   S( -2,  11),   S( -8, -25),   S(  0, -17),   S(  7,  -2),   S( -4, -18),   S( -1,  -6),
            S( -1,  -7),   S( -2,   3),   S( -6, -38),   S(  2,   3),   S(  3,  -8),   S(  0,  -8),   S( -9, -36),   S(  0,   4),

            /* bishops: bucket 13 */
            S( -6, -44),   S( -9, -58),   S( -6, -28),   S( -5, -41),   S( -6, -29),   S( -2,  -5),   S(  0,  -6),   S( -4, -25),
            S( -2, -13),   S( -4, -40),   S( -6, -50),   S( -7, -26),   S( -1, -25),   S( -1, -12),   S(  3, -11),   S(  2, -21),
            S( -6, -36),   S( -2, -15),   S(  7, -25),   S(  1, -60),   S(  0, -61),   S( 12, -28),   S( -3, -25),   S(  7,  27),
            S(  0,   5),   S( -2, -28),   S(  4, -24),   S( -5, -69),   S( 20, -43),   S(  6,  -8),   S(  1, -18),   S( -4, -21),
            S(  0,   2),   S( -9,  -9),   S(  0, -51),   S( 19, -30),   S(  3, -36),   S(  4, -29),   S( -3, -50),   S(  0, -16),
            S( -1, -10),   S( -4, -18),   S( -8, -14),   S( 16,  -7),   S(  5,  -2),   S(  8, -31),   S( 14, -14),   S( -2, -21),
            S( -5, -25),   S( -3, -22),   S(  5,   6),   S( -6,  -7),   S( -3, -27),   S(  5,   1),   S( -9, -53),   S(  1, -10),
            S( -8, -28),   S( -3, -17),   S( -3, -20),   S(  4,  -9),   S(  0,  -5),   S( -5, -31),   S(  3,   1),   S( -4, -34),

            /* bishops: bucket 14 */
            S( -2, -28),   S(-10, -46),   S(-10, -40),   S( -9, -43),   S( -6, -42),   S(  1, -20),   S( -5, -52),   S( -6, -31),
            S( -7, -23),   S(  4,  -2),   S( -1, -37),   S(-17, -65),   S(  0, -30),   S( -6, -65),   S( -9, -43),   S(  1, -20),
            S( -6, -16),   S( -7, -27),   S( -4, -56),   S(  1, -50),   S( -5, -67),   S( -9, -59),   S( -3, -32),   S( -1,  -5),
            S( -2, -17),   S( -4, -28),   S( -1, -21),   S(  1, -47),   S( 13, -71),   S( -3, -60),   S(-13, -56),   S( -6, -14),
            S( -6, -29),   S(  3, -19),   S(  1, -51),   S(  0, -54),   S(  9, -65),   S(  4, -37),   S(  9, -19),   S(  2,  -6),
            S(  1, -20),   S( -1, -33),   S( -4, -42),   S(  0, -32),   S(  4,  -6),   S( -1,   8),   S(  3, -39),   S( -7, -33),
            S( -7, -43),   S(  4, -41),   S( -4, -31),   S(  6,  -5),   S( -9, -24),   S(  3,  -9),   S( -1,  -3),   S( -4, -19),
            S( -4, -29),   S( -4, -25),   S( -1, -18),   S( -3, -25),   S( -4, -24),   S(  1,   8),   S(  5,  22),   S(  0, -11),

            /* bishops: bucket 15 */
            S(  7,  35),   S(  5,  23),   S(-13, -36),   S(  4,  -1),   S( -5, -21),   S(-10, -17),   S( -3, -20),   S( -3, -13),
            S(  4,  20),   S(  4,   1),   S(  5,   4),   S( -1, -26),   S( -9, -28),   S( -3, -15),   S( -9, -34),   S( -2,  -7),
            S( -4, -20),   S( -1,  -8),   S( -4, -32),   S( -6, -14),   S( -7, -62),   S( -8, -26),   S( -5, -28),   S(  3,   1),
            S( -1,  -9),   S(-13, -50),   S(  6,  -1),   S(-12, -60),   S(  2, -30),   S( -5, -42),   S(  3,   3),   S( -1,  -6),
            S(  2, -10),   S( -9, -26),   S( -5, -51),   S(-19, -60),   S(  0, -34),   S( -7, -35),   S(  7, -10),   S( -6, -23),
            S( -7, -31),   S( -6, -61),   S(-13, -54),   S(-11, -53),   S( -2, -24),   S( -2,  -2),   S( 13,   9),   S(  3,   4),
            S( -2, -22),   S(  1, -24),   S( -1, -18),   S( -2, -21),   S( -9, -32),   S(  0, -13),   S( -9, -25),   S(  5,  18),
            S( -4,  -8),   S( -1,  -2),   S( -2, -25),   S( -3, -27),   S( -4, -27),   S(-14, -44),   S(-10, -24),   S(  0,   1),

            /* rooks: bucket 0 */
            S(-29,  16),   S( 13,  -6),   S(  3, -16),   S(  3,  -9),   S( 10,  -8),   S( 13, -22),   S(  8,  16),   S( 16,  19),
            S( 19, -63),   S( 43, -13),   S( 18,  -6),   S( 13, -12),   S( 26, -11),   S( 15, -19),   S(-19,  17),   S(-33,  30),
            S(  7, -19),   S( 30,  16),   S( 39,  -8),   S( 15,  -1),   S( -2,  16),   S(  6,   2),   S(-24,  13),   S(-30,   2),
            S( 34, -24),   S( 67,   2),   S( 57,   7),   S( 52, -12),   S( 20,  -5),   S(  7,   1),   S( -9,  16),   S(-24,  16),
            S( 63, -27),   S( 84,  -8),   S( 72, -13),   S( 41, -13),   S( 44,  -7),   S( 35,  -4),   S(  4,  22),   S( -9,  27),
            S( 79, -45),   S(105, -31),   S( 64,  -1),   S( 29,   4),   S( 52,  -6),   S(-23,  18),   S( 48,  14),   S(-33,  42),
            S( 52,  -7),   S( 74,   2),   S( 17,  13),   S( 13,  14),   S( -6,  16),   S( 10,   4),   S(  0,  28),   S( -3,  36),
            S( 39,  30),   S( 24,  51),   S( 35,  25),   S( 34,  11),   S( 34,   3),   S( 28,  -2),   S( 26,  26),   S( 31,  25),

            /* rooks: bucket 1 */
            S(-73,  42),   S(-47,  27),   S(-50,  10),   S(-44,  -2),   S(-15, -20),   S(-22,  -7),   S(-16,  -2),   S(-29,  32),
            S(-48,  15),   S(-55,  23),   S(-11,  -2),   S(-14, -26),   S(-27,  -2),   S(-34,  -6),   S(-36,  -6),   S(-51,  14),
            S(  7,  10),   S(-24,  41),   S(-10,  17),   S(-38,  27),   S(-39,  25),   S( -3,   5),   S(-19,  10),   S(-42,  25),
            S(-45,  52),   S(-37,  43),   S(  9,  23),   S( -5,  20),   S(-25,  33),   S(-38,  40),   S(-30,  42),   S(-30,  24),
            S( 59,  16),   S( 26,  43),   S( 11,  19),   S(-29,  39),   S(-24,  40),   S( 22,  18),   S(  2,  27),   S(-33,  28),
            S( 65,  13),   S(  5,  44),   S( 22,  30),   S(-23,  29),   S( 14,  17),   S(-18,  43),   S(  8,  29),   S(-41,  45),
            S( -9,  42),   S( 10,  44),   S( 41,  24),   S(-54,  57),   S(-27,  36),   S( 18,  26),   S(-33,  39),   S(-46,  45),
            S( 55,  34),   S( 47,  48),   S( 15,  42),   S(-35,  71),   S( -2,  36),   S( 45,  15),   S( 10,  44),   S( 25,  24),

            /* rooks: bucket 2 */
            S(-70,  67),   S(-34,  31),   S(-40,  25),   S(-54,  29),   S(-61,  28),   S(-58,  33),   S(-39,  10),   S(-35,  20),
            S(-70,  54),   S(-59,  54),   S(-38,  31),   S(-47,  18),   S(-44,  18),   S(-49,  16),   S(-68,  38),   S(-60,  22),
            S(-62,  67),   S(-50,  57),   S(-46,  54),   S(-41,  27),   S(-51,  36),   S(-37,  45),   S(-14,  25),   S(-33,  29),
            S(-60,  70),   S(-48,  74),   S(-35,  65),   S(-32,  53),   S(-42,  50),   S(  3,  41),   S(-37,  69),   S(-12,  35),
            S(-13,  60),   S(-37,  74),   S(-39,  61),   S(-20,  48),   S( 12,  40),   S(  7,  51),   S(-24,  69),   S(-34,  56),
            S(-18,  57),   S(-25,  63),   S(-15,  49),   S( -7,  36),   S( 25,  36),   S( 50,  30),   S( 30,  36),   S(-20,  48),
            S(-47,  62),   S(-54,  83),   S(-30,  60),   S( -1,  47),   S( 10,  37),   S( 31,  31),   S(-52,  79),   S(-16,  56),
            S(  1,  74),   S( 10,  64),   S(-29,  63),   S( -2,  48),   S(-27,  66),   S( -9,  68),   S(-19,  82),   S( 18,  52),

            /* rooks: bucket 3 */
            S(  3,  86),   S(  6,  78),   S( 10,  58),   S( 17,  46),   S( 11,  45),   S( -5,  68),   S( 12,  70),   S( -8,  57),
            S(-27,  97),   S( -6,  79),   S(  9,  64),   S( 15,  55),   S( 22,  51),   S( 20,  52),   S( 45,  22),   S( 31, -31),
            S(-29,  87),   S( -9,  85),   S(  4,  76),   S( 15,  56),   S( 18,  59),   S( 27,  63),   S( 37,  63),   S( 10,  48),
            S(-17,  95),   S(-15,  96),   S( 22,  73),   S( 29,  62),   S( 27,  60),   S(  7,  99),   S( 64,  64),   S( 24,  68),
            S( -1, 101),   S( 30,  85),   S( 18,  67),   S( 36,  64),   S( 38,  64),   S( 51,  65),   S( 93,  56),   S( 60,  46),
            S(  4, 100),   S( 23,  84),   S( 19,  74),   S( 21,  70),   S( 33,  50),   S( 54,  48),   S(101,  41),   S( 97,  29),
            S(-12, 105),   S(  0, 104),   S(  2,  88),   S( 31,  70),   S( 23,  65),   S( 44,  63),   S( 63,  80),   S(132,  30),
            S(-35, 150),   S( 23, 106),   S( 29,  82),   S( 65,  63),   S( 79,  51),   S( 82,  63),   S(141,  59),   S(131,  50),

            /* rooks: bucket 4 */
            S(-77,  31),   S(  1,  -3),   S(-30,   3),   S( -2,   1),   S(-24, -19),   S(  7, -34),   S( -2,  -9),   S( -6, -17),
            S(-25,   1),   S(-39,  10),   S(-44,  18),   S(-26,  11),   S(-17,  -6),   S( -8, -28),   S(  4, -30),   S(-25,  -6),
            S(-12,  16),   S(-34, -12),   S(-15,   5),   S(-17, -19),   S(-30,   2),   S( -1, -24),   S( 21, -16),   S(-59,   4),
            S(-37, -12),   S(  5,   2),   S(-44,  18),   S( 10,   1),   S( 13,  -9),   S( -7,   1),   S( -1,  18),   S(-26,  17),
            S(-28,  -5),   S(-16,  30),   S( -9,   4),   S( 52,   4),   S( 18,   5),   S( -3,   3),   S( 30,  28),   S( 25,  -1),
            S( 18,  10),   S( 15,  15),   S( 56,   5),   S( 40,  -3),   S( 30,   7),   S( 11,  24),   S(  5,  31),   S( 23,  31),
            S( -6,   6),   S( 27,  39),   S( 37,   7),   S( 34,  10),   S( 54,  -8),   S(  7,  -7),   S( 34,  18),   S( 17,  35),
            S( 27, -30),   S( 36,  47),   S( 33,  11),   S( 13,  13),   S( 22,  -3),   S( 20,  10),   S( 19,  18),   S( 15,  30),

            /* rooks: bucket 5 */
            S(-27,  36),   S(-14,  45),   S(-34,  43),   S(-25,  23),   S(-17,  13),   S(-14,  32),   S( 15,  28),   S(-17,  43),
            S(-22,  32),   S(-30,  43),   S(-88,  79),   S(-68,  55),   S(-49,  36),   S(-14,  21),   S( 23,  19),   S(-21,  23),
            S(-14,  40),   S(-66,  64),   S(-85,  70),   S(-80,  56),   S(-59,  32),   S(-33,  40),   S(-18,  40),   S(-10,  29),
            S(-53,  73),   S(-19,  53),   S(-33,  63),   S(-33,  44),   S(-34,  52),   S(-17,  61),   S(-15,  55),   S(-15,  41),
            S(  0,  60),   S( -5,  60),   S( 16,  49),   S( -8,  69),   S( 16,  49),   S( 10,  66),   S( 62,  52),   S( 20,  42),
            S( 53,  61),   S( 43,  60),   S( 51,  54),   S( 19,  68),   S( 59,  38),   S( 68,  40),   S( 48,  52),   S( 48,  44),
            S( 18,  56),   S( 25,  66),   S( 52,  43),   S( 36,  55),   S( 50,  27),   S( 60,  36),   S( 79,  41),   S( 68,  47),
            S( 79,  44),   S( 77,  42),   S( 48,  57),   S( 30,  41),   S( 59,  39),   S( 63,  41),   S( 63,  47),   S( 25,  57),

            /* rooks: bucket 6 */
            S(-35,  34),   S(-18,  39),   S( -1,  21),   S( -4,  16),   S(-34,  28),   S(-48,  51),   S(-26,  59),   S( -9,  48),
            S(-11,  29),   S(-13,  41),   S( -9,  31),   S(-46,  36),   S(-41,  49),   S(-67,  68),   S(-48,  64),   S( 26,  22),
            S(-48,  65),   S(-50,  54),   S(-28,  47),   S(-70,  52),   S(-40,  41),   S(-67,  74),   S(-28,  66),   S(  7,  29),
            S(-43,  75),   S(  6,  62),   S(-19,  64),   S(-32,  56),   S(-26,  50),   S(-20,  59),   S(-69,  82),   S(-20,  54),
            S(  0,  75),   S( 22,  68),   S( 51,  39),   S( 11,  44),   S(-23,  76),   S(  1,  65),   S( 29,  51),   S(  5,  55),
            S( 21,  67),   S( 59,  57),   S( 73,  37),   S( 37,  32),   S( 27,  48),   S( 33,  66),   S( 45,  57),   S( 78,  42),
            S( 49,  61),   S( 69,  48),   S( 92,  22),   S( 82,  14),   S( 82,  32),   S( 52,  49),   S( 71,  48),   S( 42,  53),
            S( 81,  67),   S( 64,  60),   S( 73,  38),   S( 55,  39),   S( 68,  53),   S( 72,  62),   S( 85,  61),   S( 43,  64),

            /* rooks: bucket 7 */
            S(-55,   4),   S(-25,   4),   S(-23, -10),   S( -4, -10),   S(  9, -20),   S(-28,  26),   S(-33,  32),   S( 23,  -9),
            S(-55,  29),   S(-31,  17),   S(-23,   0),   S( -4,  -6),   S(  1,   5),   S( 13,   8),   S(-11,  13),   S(-38,  15),
            S(-90,  59),   S(-52,  30),   S(-33,  25),   S(-20,  -1),   S(-19,  10),   S(-37,  11),   S(-33,   9),   S(  4,   6),
            S(-69,  51),   S( -8,  33),   S( -9,  26),   S(  2,  20),   S( 17,   5),   S( 13,  13),   S( 30,   6),   S( -8,  15),
            S(-25,  50),   S( -7,  32),   S( 47,  -9),   S( 46,  -4),   S( 50,   3),   S( 91,   3),   S( 48,  28),   S( 40,  -9),
            S( -8,  44),   S( 18,  25),   S( 86, -19),   S( 94, -19),   S( 69,  -6),   S( 73,  18),   S( 60,  32),   S( 42,   3),
            S(  3,  43),   S( 41,  21),   S( 60,  -3),   S( 76, -11),   S( 93, -11),   S( 76,   2),   S( 42,  41),   S( 28,  12),
            S( 30,  66),   S(  7,  46),   S( 62,   2),   S(100, -24),   S( 40,   9),   S( 29,  24),   S( 48,  26),   S( 70,  11),

            /* rooks: bucket 8 */
            S(-48, -30),   S(-18,   1),   S( -5,  -3),   S(-24,  -4),   S(-24, -36),   S(-23, -36),   S(-19, -14),   S(-30,  15),
            S(-10,  -6),   S( -8,  -2),   S(-16,  -1),   S(-11, -13),   S(-25, -22),   S(-13, -32),   S(-10, -26),   S(-14, -54),
            S(  2,  -4),   S(  0, -15),   S( -9,  -2),   S(-10,  -1),   S(-29, -41),   S(-14, -33),   S(  1,  18),   S( -4,  -9),
            S(-13, -15),   S(-10,  16),   S(-15,  -8),   S(  4,   1),   S( -2,   5),   S(-16, -12),   S(  4, -10),   S( -8,   7),
            S(-11, -15),   S( -1,  19),   S(-15,  27),   S( -1,   9),   S( -1,  -6),   S(  9,   5),   S(  0,   0),   S( -5, -19),
            S(  3,  18),   S( -6,   8),   S( 18,  32),   S( 13,  -9),   S( -8,  -9),   S(  1,  -3),   S(  2,   2),   S( 10,  43),
            S(-11,   1),   S(-12,  12),   S( 14,   4),   S(  4, -20),   S( 22,   1),   S( 10, -18),   S( 15,  -9),   S(  8,   4),
            S( -7, -101),  S(  5,  -4),   S( 15,   5),   S( -1, -17),   S( -2,  -1),   S(  2, -16),   S(  7,   4),   S(  8,  32),

            /* rooks: bucket 9 */
            S(-53, -33),   S(-11, -40),   S(-41, -40),   S(-53, -25),   S(-36, -16),   S(-19, -19),   S( -9, -35),   S(-55, -27),
            S( 12, -23),   S( -6, -23),   S(-29, -33),   S(-30, -28),   S(-25, -39),   S(  9, -16),   S( -2, -24),   S(-20, -25),
            S( -7, -37),   S(  8, -33),   S(-12, -11),   S(-26, -19),   S(-39, -32),   S(  8, -22),   S(  7,  -5),   S(-10, -24),
            S( -3, -20),   S( -6, -13),   S(-11,  -4),   S(-29,  -9),   S(-11,  -9),   S(  0, -17),   S(  8,  14),   S( -3,  -8),
            S( -4,  -8),   S(-17,   6),   S( -4,   7),   S( -2,  13),   S( 12,  20),   S( 13,  -6),   S( -2,  -3),   S(  2, -19),
            S(  9,   7),   S(-17, -12),   S( -4,  -3),   S(-25,  -5),   S(  5, -18),   S( 21,  -5),   S( 11,  -1),   S(  7,  -6),
            S( 39,   1),   S( 48, -13),   S( 31,  -5),   S( 35,   0),   S( 17, -31),   S( 26, -19),   S( 36, -16),   S( 39,   0),
            S( 47, -62),   S( 29, -34),   S( 21,   3),   S( 21,  26),   S(  8,   0),   S( 19,  -2),   S( 21,   2),   S( 24,   7),

            /* rooks: bucket 10 */
            S(-70, -80),   S(-32, -45),   S(-36, -62),   S(-42, -38),   S(-44, -44),   S(-34, -48),   S( 15, -42),   S(-39, -35),
            S( -7, -25),   S( -7, -25),   S(-14, -42),   S(-35, -23),   S(-20, -28),   S(-25, -22),   S( 21,   1),   S(  9, -16),
            S(-27, -32),   S(-21, -41),   S(-16, -37),   S( -8, -23),   S(-28, -16),   S(-13, -24),   S( 19,  -3),   S( -7, -18),
            S(-10, -16),   S( -4, -26),   S(-22, -22),   S(-13, -19),   S( -2, -17),   S( -9, -25),   S(  6,  12),   S( -6, -23),
            S( -8,  -9),   S( 11, -12),   S(  0, -15),   S(  0, -40),   S(-12, -25),   S(  3,   0),   S( 24,   8),   S(  0,   5),
            S( 30,  -1),   S( 22,  12),   S(  6, -12),   S( 12, -18),   S(-13, -22),   S(  9,  -9),   S( 20,   0),   S(  8,  -5),
            S( 68, -14),   S( 64, -19),   S( 55, -33),   S( 45, -44),   S( 36, -28),   S( 25,  -7),   S( 33, -30),   S( 30, -28),
            S( 45,   4),   S( 11, -14),   S( 29, -18),   S( 18, -27),   S( 28, -20),   S( 22,   5),   S( 23, -20),   S( 13, -19),

            /* rooks: bucket 11 */
            S(-63, -24),   S(-39, -13),   S(-32, -24),   S(-31, -65),   S(-18, -19),   S(-18,  -1),   S(-32, -27),   S(-58, -14),
            S(-30,  -5),   S( -9, -34),   S(-16, -24),   S(-23, -20),   S(-19, -22),   S(-37, -10),   S(-12, -14),   S(-24,  -3),
            S(-15, -21),   S( 14, -23),   S(  4, -16),   S( -6, -26),   S( -6, -21),   S(-24,  -4),   S(-32, -23),   S(-21, -43),
            S(-12,  25),   S(-12, -15),   S(-10,  -4),   S( -4,   4),   S( -9, -19),   S(-10,  17),   S(  6,  -3),   S( -7, -33),
            S(-11,  18),   S( 11, -12),   S( 16,  -4),   S( 17, -22),   S( 17, -14),   S( 21, -13),   S(  9,  -4),   S( -5,  -7),
            S(  6,  37),   S( 20,  17),   S( 14, -12),   S( 42,   0),   S( 32,  13),   S( 32,  -2),   S(-19,  12),   S(  9,  10),
            S( 47,  35),   S( 35,  13),   S( 47, -16),   S( 52, -24),   S( 26,  -5),   S( 30,  10),   S( 26,  37),   S( 40,   0),
            S( 30,  41),   S( 11,  27),   S( 23,   0),   S( 14, -24),   S(  0, -16),   S( 18,   2),   S(  8,  27),   S( 26,  12),

            /* rooks: bucket 12 */
            S(  5, -17),   S( -3, -24),   S(-14, -21),   S( -6, -12),   S(  2,  -7),   S( -1, -31),   S(-18, -47),   S(-19, -30),
            S( 10,   7),   S( -2,  -9),   S(-18, -11),   S( -7, -21),   S( -9, -15),   S( -4,  -8),   S(  2,  -6),   S( -4, -24),
            S(  3,  -7),   S( -7, -21),   S(-14, -30),   S(-11, -31),   S( -6, -28),   S(  5,  -6),   S( -6, -13),   S(  2,  -7),
            S( -9, -12),   S( -6, -21),   S(  0,  -4),   S(  4, -12),   S( -3, -15),   S(-10, -35),   S( -6, -26),   S( -3, -29),
            S(-10, -12),   S(-11, -17),   S(  5, -17),   S(  3,  -1),   S(-10, -32),   S(  4, -14),   S( -4, -18),   S(  0, -12),
            S( -6, -13),   S(  0, -20),   S( 13,   0),   S(  6, -28),   S( -4, -26),   S( -6, -25),   S(  1, -25),   S(  5,  13),
            S( -7, -17),   S(  3, -10),   S( -1, -32),   S( 10,  -5),   S(  3, -24),   S( -7, -47),   S(  0,  -8),   S(  9,   3),
            S( -5, -34),   S(  8,  13),   S( -2, -35),   S( -1, -24),   S( -4, -36),   S(-12, -46),   S( -9, -41),   S(  9,  23),

            /* rooks: bucket 13 */
            S(-14, -40),   S( -1, -21),   S( -9,  -8),   S( -5,  22),   S(  2,  -3),   S(-12, -37),   S(  0, -14),   S(-15, -15),
            S(  0, -15),   S( -3,  -3),   S(-19,   1),   S(-12,  -2),   S(-12, -21),   S( -1,   0),   S(  4,  10),   S( -1, -10),
            S( -9, -30),   S( -9, -30),   S(-12, -30),   S( -5, -23),   S(  3,  22),   S(  2, -12),   S( -4, -10),   S( -1, -21),
            S(-15, -47),   S( -7, -16),   S(-20, -42),   S( -7, -20),   S(  9,   5),   S(-16, -35),   S( -4, -31),   S( -2, -15),
            S(  0, -21),   S( -3, -23),   S(  9,   8),   S( -6, -29),   S(-12, -33),   S( -4, -33),   S( -8, -47),   S(  5,  -2),
            S(-16, -33),   S( -1,  -2),   S( -7, -28),   S( 10, -10),   S(  5, -22),   S(  6,   7),   S(  6,  -7),   S(  3,   0),
            S( -8,  -3),   S(  3,   5),   S(  2,   0),   S( -1,  -9),   S(  3, -25),   S( 14,  26),   S(  6,  -3),   S(  2,   7),
            S(-23, -114),  S(-15, -54),   S(  2,  -3),   S(  0,  -6),   S( -4,  -6),   S( -7, -40),   S( -9, -44),   S(  4,  13),

            /* rooks: bucket 14 */
            S(-13, -18),   S(-17, -22),   S( -1, -12),   S( -8, -30),   S( -6,  15),   S( -9, -15),   S(  9,  -4),   S( -8, -11),
            S(-20, -46),   S(-15, -41),   S(-11, -10),   S(-19, -43),   S(-13, -21),   S( -2,  -7),   S(  6,  21),   S(  7,   5),
            S( -6, -20),   S( -9, -26),   S( -5, -22),   S( -7, -23),   S(-17, -34),   S(-11, -23),   S(  5,  17),   S( -2, -20),
            S(  1,   7),   S(-11, -39),   S( -6, -26),   S( -7, -16),   S( -1, -26),   S( -4, -15),   S( -6, -43),   S( -8,  -8),
            S(  1, -32),   S(  1, -36),   S( -6, -59),   S(-15, -68),   S(  1, -45),   S( -5, -52),   S(  4, -34),   S(  5,   4),
            S( -2, -26),   S( -2, -20),   S(  2, -52),   S(  2, -66),   S(  2, -58),   S(  4, -41),   S(  8, -25),   S(  0, -16),
            S( 10,  10),   S( -1, -29),   S( -1, -48),   S( -4, -66),   S(  1, -62),   S(  7, -13),   S( 10,  -4),   S(  4,  -7),
            S( -7, -26),   S( -1,  -9),   S(-10, -52),   S(  6,  -6),   S( -9, -30),   S( -2,   9),   S(  4,   5),   S( -4, -14),

            /* rooks: bucket 15 */
            S( -6,  -8),   S(-10, -33),   S( -5, -31),   S(-10, -32),   S( -1, -18),   S( -5,  -3),   S(-10, -38),   S(-10, -17),
            S(-12, -12),   S(-14, -36),   S( -1,  -5),   S( -7, -14),   S(-13, -34),   S( -3,  -8),   S(-10, -37),   S(  6,  11),
            S( -9, -32),   S(-11, -40),   S( -6, -34),   S(  2, -20),   S(  3, -21),   S( -6, -25),   S( -1,  -3),   S( -6, -23),
            S( -6, -40),   S( -6, -34),   S(-12, -38),   S( -4, -30),   S(-10, -44),   S( -3, -29),   S( -2, -25),   S( -9,  -7),
            S( -3, -23),   S( -5, -35),   S(  7, -17),   S( -4, -40),   S( -2, -40),   S(  1, -36),   S(  3, -21),   S(  0,  22),
            S(  8,   6),   S(  2,  -5),   S( -1, -51),   S(  2, -56),   S( -6, -57),   S( 11, -29),   S( 12, -15),   S( -4,  -9),
            S( 13,  25),   S( 13,  -6),   S(  7, -27),   S( -3, -60),   S(  0, -35),   S( 18,  19),   S( 14,   6),   S(  1,  -2),
            S(  2,   0),   S( -3, -14),   S(  2, -17),   S(  1, -28),   S( -5, -49),   S(  0, -18),   S(  1, -14),   S(  2,   0),

            /* queens: bucket 0 */
            S(-31, -17),   S(-25, -48),   S( 42, -82),   S( 52, -72),   S( 41, -56),   S( 27, -22),   S( 51,  15),   S( 17,  11),
            S(-26, -21),   S( 24, -64),   S( 35, -32),   S( 29, -15),   S( 31,   6),   S( 25,  12),   S( 14,  49),   S( 38,  26),
            S( 15,  -4),   S( 38,   1),   S( 18,  16),   S( 17,  -2),   S( 20, -24),   S( 14,  -2),   S(  8,  18),   S( 27,  42),
            S( 10,  24),   S( 20,  46),   S(  1,  31),   S( 10,  19),   S( 14,   7),   S( 17,  -2),   S( 17,  10),   S( 19,  30),
            S( 30,  51),   S( 24,  42),   S( 12,  19),   S( 19,  15),   S(-15,   5),   S( -4, -16),   S( 29,  -1),   S( 39,  13),
            S( 22,  67),   S( 23,  54),   S( 13,  28),   S( 22,  -3),   S( 45, -12),   S(  4,  20),   S( 23,  14),   S( 21, -10),
            S( 43,  51),   S( 59,  38),   S( 26,  38),   S( 49,  14),   S( 13,   3),   S( -8, -10),   S( 27,  18),   S( 25,  22),
            S( 44,  40),   S( 29,  45),   S( 42,  31),   S( 42,  35),   S( 50,  39),   S( -6,  -5),   S( 57,  21),   S( 46,  30),

            /* queens: bucket 1 */
            S(-13, -27),   S(-76, -30),   S(-61, -21),   S(-15, -74),   S(  2, -25),   S( -9, -49),   S( 28, -31),   S(  9,  27),
            S(-17, -34),   S(-13, -48),   S( 13, -59),   S(  6,   9),   S(  4,  -4),   S( 17, -18),   S( 27, -28),   S(  8,  23),
            S(-25,  22),   S(  7, -19),   S(  8,   6),   S( -8,  10),   S(  1,   8),   S( -7,  17),   S( 23,  -7),   S( 20,  24),
            S( 16, -18),   S(-15,  33),   S(-12,  28),   S( 16,  29),   S( -2,  23),   S(  8,   8),   S(  9,  -8),   S( 24,  26),
            S( 20,  -2),   S(  6,  14),   S( -7,  51),   S(-19,  40),   S(-15,  36),   S(  5,   5),   S( -6,  14),   S(  8,  43),
            S( 17,  37),   S( 17,  54),   S( 22,  48),   S(-29,  50),   S( -5,  35),   S(-35,  39),   S( 31,  32),   S( 28,  48),
            S(  6,  46),   S( -1,  72),   S( -6,  27),   S(-23,  76),   S(-24,  55),   S( 15,  32),   S( -6,  40),   S(-17,  60),
            S( -5,  18),   S( 13,  28),   S( 29,  40),   S(  4,  22),   S(  4,  23),   S( 14,  24),   S( 21,  30),   S(  2,  31),

            /* queens: bucket 2 */
            S( 14,   9),   S( 19, -39),   S( 11, -30),   S( -5, -26),   S(-27,  11),   S(-23, -22),   S(-34, -17),   S(  6,  16),
            S( 22,  13),   S( 19,  26),   S( 22, -16),   S( 31, -35),   S( 22, -28),   S( 20, -46),   S( 21, -25),   S( 40, -30),
            S( 20,   9),   S( 20,   5),   S( 13,  18),   S(  5,  22),   S(  7,  45),   S( 15,  46),   S( 14,  10),   S( 34,   1),
            S( 15,  28),   S(  3,  42),   S( -3,  33),   S(  6,  40),   S(-13,  61),   S(  1,  80),   S( 20,  20),   S( 12,  60),
            S( 18,   6),   S( -8,  54),   S(-19,  51),   S(-39,  79),   S(-31,  83),   S(-22,  89),   S(-13, 115),   S(  1, 102),
            S( 10,  40),   S(  0,  51),   S(-29,  72),   S( -8,  47),   S(-23,  87),   S(-23, 103),   S(  4,  97),   S( 13,  80),
            S(-24,  75),   S(-38, 100),   S(-20,  66),   S( 16,  55),   S(-21,  85),   S( 25,  48),   S(-27,  59),   S(-20,  96),
            S(-65,  89),   S(  5,  58),   S( 38,  47),   S( 31,  44),   S( 26,  61),   S( 29,  55),   S( 18,  50),   S(-12,  50),

            /* queens: bucket 3 */
            S( 71,  85),   S( 56,  83),   S( 50,  83),   S( 44,  61),   S( 71,  15),   S( 41,  11),   S( 14,  10),   S( 29,  55),
            S( 67, 112),   S( 58, 101),   S( 43,  96),   S( 48,  75),   S( 48,  68),   S( 64,  35),   S( 62,  -3),   S( 22,  29),
            S( 52,  90),   S( 47,  95),   S( 53,  66),   S( 43,  61),   S( 45,  67),   S( 49,  83),   S( 52,  95),   S( 54,  49),
            S( 43, 125),   S( 53,  81),   S( 41,  77),   S( 37,  68),   S( 39,  67),   S( 33, 118),   S( 53,  91),   S( 41, 129),
            S( 50, 105),   S( 45,  98),   S( 33,  88),   S( 26,  77),   S( 23,  93),   S( 13, 119),   S( 28, 161),   S( 48, 144),
            S( 44, 125),   S( 50, 101),   S( 44,  85),   S( 22, 102),   S( 30, 113),   S( 57, 105),   S( 51, 156),   S( 26, 191),
            S( 51, 130),   S( 53, 115),   S( 67,  77),   S( 59,  85),   S( 20, 116),   S( 47, 118),   S( 77, 143),   S(139,  89),
            S( 69, 100),   S( 90, 100),   S( 73,  90),   S( 86,  76),   S( 45, 100),   S( 88,  79),   S(123,  79),   S(116,  80),

            /* queens: bucket 4 */
            S(-13, -25),   S(-14, -12),   S(-24, -12),   S( -9, -15),   S( 12, -23),   S( 38,  -2),   S(-32, -12),   S(-27,   0),
            S(-24, -21),   S(-29, -10),   S(  7, -20),   S(-42,   9),   S( 10, -25),   S( -1, -22),   S( -7, -20),   S(-36,  -8),
            S(  3,   4),   S( 10,   5),   S( -9,  24),   S( -5,  21),   S( 15,  -9),   S( -3, -29),   S( -1, -25),   S(-34, -19),
            S(-21,   3),   S( -2,  13),   S( -1,  25),   S(-13,   0),   S( 10,  -4),   S( 12,   6),   S(  0, -24),   S( -6,   4),
            S(-11,   9),   S( 19,  12),   S( 14,   5),   S( 18,  21),   S( 13,  -5),   S( 19, -17),   S(-16, -24),   S(  0, -14),
            S( -2,  14),   S( 36,  22),   S( 19,  33),   S( 19,  33),   S( 11,   4),   S( -1,  -5),   S(-16, -19),   S(-11,  -3),
            S( -8, -12),   S( -4,  27),   S(  1,  25),   S( 31,  36),   S(  7,   7),   S(-13,  -7),   S(-14, -38),   S(-13, -12),
            S( -3,  -8),   S(  0,   6),   S( 32,  38),   S( 15,  23),   S(-14,  -4),   S( -6,   2),   S(-14, -30),   S( -8, -18),

            /* queens: bucket 5 */
            S(-35, -15),   S(-19, -20),   S(-25, -31),   S(-40, -28),   S(-57, -21),   S( 12,  -6),   S( -4,  -2),   S( -2,   1),
            S(-27, -11),   S(-38, -11),   S(-57, -17),   S(-59,  -1),   S( -3, -17),   S(-38, -17),   S(-44,  -3),   S(-45,  -7),
            S(-34,   2),   S(-62,  -7),   S(-68,  -1),   S(-32,  29),   S( 20,  49),   S( -7,  18),   S(  1,  -6),   S( 12,  20),
            S(-52, -13),   S(-52, -11),   S( -3,  34),   S( -2,  45),   S( 10,  19),   S( -9,  17),   S( -1,  -8),   S( -1,  33),
            S(-33,  -6),   S(-21,   6),   S( -6,  47),   S( -7,  47),   S( 18,  38),   S(  3,   8),   S(  1,   8),   S(-22,  -6),
            S(-21,  18),   S(  5,  34),   S(-11,  35),   S(  4,  40),   S( 39,  35),   S(  0,   6),   S(  1,   1),   S( -7,  -9),
            S( -8,  11),   S( -5,  19),   S( 11,  59),   S( -3,  34),   S(  7,  37),   S( 23,  44),   S( 17,  16),   S(-14,  -4),
            S( 12,  20),   S( 17,  32),   S( 10,  27),   S( 21,  60),   S( 22,  36),   S( 10,  28),   S( -1, -13),   S(-14, -10),

            /* queens: bucket 6 */
            S(-28,  -3),   S(-35, -22),   S(-55, -21),   S(-82, -57),   S(-79, -39),   S(-63, -49),   S(-41, -44),   S(-19,   2),
            S(-56, -12),   S(-36,  -3),   S(-35,   0),   S(-50,   5),   S(-64,  34),   S(-77,  10),   S(-76,  -8),   S( 18,  27),
            S(-36,  13),   S(-20,   9),   S(-34,   9),   S(-82,  70),   S(-35,  49),   S(-36,  11),   S(-40,  -4),   S( 10,  14),
            S(-32,  25),   S(-22,   2),   S(-27,  39),   S(-40,  45),   S(  3,  49),   S( 10,  59),   S(-15,  47),   S( 18,  20),
            S(-48,  40),   S( -2,  33),   S(-27,  45),   S(  5,  23),   S( 29,  53),   S( 61,  52),   S( 28,  45),   S(  2,  27),
            S(-18,  47),   S( -2,  19),   S( 33,  12),   S( 22,  36),   S( 14,  49),   S( 57,  74),   S(-10,  10),   S(-14,  13),
            S( -6,  28),   S(  7,  16),   S( -4,  35),   S(  0,  29),   S( 36,  62),   S( 27,  74),   S( -9,  24),   S(-30,  15),
            S(  1,  29),   S( 18,  39),   S( 20,  35),   S(  7,  36),   S( 42,  67),   S( 32,  52),   S(  8,  34),   S(  4,  11),

            /* queens: bucket 7 */
            S( -9,  -9),   S(-30,  22),   S(-35,  11),   S(-31,  15),   S(-26,   0),   S(-39,   3),   S(-29,  14),   S(-11,   0),
            S(-30,   8),   S(-44,  -1),   S(-23,   1),   S( -4,  11),   S(-10,  13),   S(-39,  43),   S(-55,  58),   S(-20,  -7),
            S(-40,  -6),   S(-61,  35),   S(  0,  -8),   S( -5,   0),   S(  4,  21),   S(-12,  44),   S(-10,  11),   S(-16,   4),
            S(-57,   8),   S( 11,   0),   S(-16,   4),   S( 23, -22),   S( 47, -20),   S( 32,  21),   S(  1,  55),   S(-12,  26),
            S(-28,  32),   S(-53,  26),   S( 16,  -4),   S( 53, -42),   S( 56, -29),   S( 72, -12),   S( 16,  39),   S( 21,  23),
            S(-16,  24),   S(-10,   7),   S(  2,  -8),   S( 19, -13),   S( 39,  14),   S( 79,  15),   S( 51,  24),   S( 34,  34),
            S(  9,   1),   S( 13,  16),   S( 11,  -5),   S(  7,  14),   S( 38,  17),   S( 55,  38),   S( 64,  33),   S( 48,  41),
            S( 13,  20),   S( 23,  28),   S( 22,  23),   S( 27,  27),   S( 42,  32),   S( 25,  38),   S( 27,  26),   S( 43,  43),

            /* queens: bucket 8 */
            S( -6,  -9),   S(  0,  -8),   S(-11,   1),   S(-11,  -9),   S( -4,  -1),   S( -2, -14),   S(-18, -20),   S( -2,   4),
            S( -7,  -1),   S(-11, -15),   S( -6,   2),   S(-19, -10),   S( -9, -18),   S(-19, -29),   S(-20, -48),   S( -3,  -4),
            S( -3,  -2),   S(-11,  -8),   S(-20, -32),   S(-15, -27),   S(-14, -17),   S(-18, -29),   S(-12, -32),   S(-14, -21),
            S( -6,  -3),   S(  7,  16),   S( -4,   1),   S(-21, -29),   S(-23, -42),   S(-15, -25),   S( -1,  -3),   S( -5, -21),
            S( 16,  35),   S(  3,  33),   S(  0,   9),   S( -1,  -2),   S( -5, -14),   S( -5,  -8),   S( -8, -10),   S( -7,  -7),
            S(  7,  21),   S( 10,  24),   S(-22,  -3),   S(  5,  17),   S(-13, -23),   S(-10, -17),   S(  2,   9),   S(  6,  16),
            S( -2,  -5),   S(-16, -15),   S( 13,  29),   S(  8,  10),   S(  0,   0),   S(  2,   6),   S( -4,  -8),   S( -5, -12),
            S(-22, -43),   S( 15,  26),   S(-20, -38),   S( -6, -12),   S(-12, -29),   S( -2, -10),   S( -2, -14),   S( -3,  -6),

            /* queens: bucket 9 */
            S(  2,   5),   S(-16, -38),   S( -2,  -4),   S(-32, -35),   S(-26, -45),   S(-21, -32),   S(-11, -19),   S(-13, -20),
            S( -4,  -9),   S(-11, -21),   S(-26, -34),   S(-11, -16),   S(-28, -36),   S(-19, -32),   S(  0, -10),   S( -3,  -8),
            S(  1,   0),   S( -5,   1),   S(-20,  -4),   S(-24, -30),   S(-24, -38),   S(-15, -21),   S( -8, -12),   S( -2,  -7),
            S(-13, -11),   S(-11,  -9),   S( -1,  25),   S(-13, -16),   S(  0,  -3),   S( -7, -23),   S(-13, -25),   S( -1, -16),
            S(  2,   7),   S(  4,  31),   S(  5,  20),   S(  7,  37),   S(  1,  16),   S(  0,  -3),   S( -4,  -2),   S( -8,  -2),
            S(-20, -25),   S(-21,  -8),   S(  0,  21),   S( 11,  38),   S(-11,  -6),   S( -7,  -3),   S( -8, -10),   S( -6,  -6),
            S( -8, -15),   S(-16, -24),   S( -5,  33),   S( 13,  37),   S( 11,  12),   S(  1, -11),   S(  1,  -1),   S(-14, -29),
            S( -2, -14),   S( -9, -17),   S(  5,  14),   S(  2,  11),   S(  3,  -1),   S( -1,  -1),   S(  7,   9),   S( -4, -10),

            /* queens: bucket 10 */
            S( -2,  -2),   S( -6,  -5),   S(-15, -33),   S(-26, -35),   S(-16, -23),   S( -7,  -8),   S( -4, -14),   S( -9, -20),
            S( -9, -15),   S(-15, -25),   S(-19, -33),   S(-22, -27),   S(-16, -18),   S(-22, -27),   S( -4, -11),   S(-17, -24),
            S( -4,  -9),   S(-16, -26),   S(-23, -35),   S(-25, -31),   S(-20,  -6),   S(-19, -11),   S( -5,  -3),   S( -2,  -3),
            S( -2,  -1),   S( -4, -11),   S(-16, -18),   S(-18, -23),   S( -3,   6),   S(-18,   1),   S( -6,  -7),   S(-16, -18),
            S( -6,  -9),   S( -2, -10),   S(-17, -12),   S(  4,  15),   S(-15, -24),   S( 10,  27),   S( 14,  17),   S(  0,   6),
            S( -5,  -1),   S(-25, -36),   S(-17,  -9),   S(-11,  10),   S( -4,  19),   S(  4,  22),   S(  5,   7),   S( -7, -10),
            S( -8,  -8),   S(-23, -30),   S(  3,   9),   S(-16, -13),   S(  6,  16),   S(  4,  23),   S( -6,  -8),   S( -8, -16),
            S(  2,   3),   S( -8, -11),   S( -1,   1),   S(  4,  10),   S( 14,  23),   S(  4,   7),   S( 11,  22),   S( -3, -10),

            /* queens: bucket 11 */
            S(-10, -22),   S( -7, -20),   S(-23, -31),   S(-14, -32),   S(-14, -15),   S(-10, -12),   S( -2,   4),   S(-14, -26),
            S(-17, -29),   S(-13, -19),   S(-46, -42),   S(-18, -20),   S(-17, -11),   S(-14, -14),   S( -6,  -8),   S( -3,   4),
            S(-19, -23),   S(-24, -51),   S(-12, -38),   S(-26, -48),   S(-19, -31),   S(-12, -21),   S(  4,  22),   S(-16, -13),
            S(-15, -31),   S(-26, -35),   S(-22, -47),   S(-10, -24),   S(-17, -39),   S(-18, -26),   S( 24,  39),   S( -4,  -4),
            S( -7,  -2),   S( -8, -20),   S(-31, -43),   S(  1, -17),   S( -2, -25),   S( 26,  43),   S( 22,  46),   S(  2,  16),
            S(-14, -29),   S( -1,   2),   S(-26, -36),   S(  3,  -5),   S(  3,  -3),   S( 38,  35),   S( 12,  18),   S( -4,   3),
            S( -6,  -3),   S(-16, -28),   S(  1,   1),   S(-16, -18),   S( -2,   2),   S( 20,  26),   S( 37,  48),   S( -1,  -8),
            S(-12, -19),   S( -5, -18),   S(-11, -21),   S(  1,  -7),   S(  4,   5),   S(  0, -14),   S( 18,  18),   S( -9, -36),

            /* queens: bucket 12 */
            S(  7,  13),   S(  0,   1),   S(  2,   4),   S( -6,  -7),   S(-10, -17),   S( -2,  -3),   S(  0,   0),   S( -4,  -6),
            S( -3,  -8),   S( -7, -14),   S(-13, -25),   S( -9, -19),   S( -4,  -9),   S( -6, -10),   S( -2,  -6),   S( -5, -10),
            S( -3,  -6),   S( -5, -11),   S(  1,  -4),   S(-13, -30),   S( -8, -16),   S(-12, -22),   S(-12, -27),   S( -8, -16),
            S(  4,   8),   S( -3,  -2),   S( -3,  -8),   S(-12, -21),   S( -4, -11),   S( -4, -12),   S( -2,  -1),   S( -1,  -9),
            S(  1,   0),   S( 10,  18),   S( 22,  37),   S( -9, -15),   S(-16, -28),   S( -2,  -4),   S(-12, -22),   S( -1,  -2),
            S(  8,  20),   S(  9,  19),   S( 21,  31),   S( -7, -22),   S( -3,  -2),   S(  0,  -1),   S(  3,   6),   S( -4, -12),
            S(  3,   9),   S(  1,   5),   S( 18,  34),   S( 12,  22),   S(  3,   4),   S(  2,   5),   S(  7,  13),   S(  0,   2),
            S( -2,  -7),   S( -8, -19),   S( -9,  -2),   S( -7,  -9),   S(  6,  14),   S(  2,   6),   S(  1,   2),   S( -6,  -5),

            /* queens: bucket 13 */
            S( -2,  -8),   S( -6, -14),   S( -1,  -5),   S( -4,  -6),   S( -4, -10),   S( -2,  -8),   S( -6, -15),   S( -5,  -9),
            S(  5,  11),   S(  5,  12),   S(  3,   2),   S( -6, -16),   S( -9, -19),   S(  0,  -2),   S(  0,  -5),   S(-10, -19),
            S( -4,  -9),   S( -4, -11),   S( -5, -12),   S( -7, -24),   S(-12, -26),   S(-10, -22),   S( -8, -17),   S(-10, -14),
            S( -4,  -7),   S( -3, -12),   S(  2, -11),   S( -4, -24),   S( -3, -11),   S(-12, -32),   S( -5, -15),   S( -4, -15),
            S( -3,  -3),   S(  2,  11),   S(  7,   5),   S( -1,  -8),   S(  3,   0),   S( -6, -18),   S( -6, -16),   S( -8, -16),
            S(  1,  -4),   S(  8,  21),   S( 29,  51),   S( 12,  30),   S( -5,   3),   S( -5, -14),   S(  4,   9),   S( -6, -13),
            S( -2,  -4),   S( 14,  32),   S( 10,  32),   S( 17,  40),   S(  4,   6),   S(  1,  -1),   S( -2,  -7),   S(  8,  18),
            S(-15, -30),   S(  1,  -1),   S( -2,   0),   S( -3,  -1),   S( 12,  22),   S(  4,   7),   S( -6,  -8),   S( -7, -13),

            /* queens: bucket 14 */
            S( -1,  -3),   S(  2,   0),   S( -2,  -8),   S(-10, -18),   S(  3,   6),   S( -2,  -6),   S( -2,  -5),   S( -6, -17),
            S( -4,  -7),   S(  6,  10),   S( -5, -14),   S( -6, -17),   S(-13, -24),   S( -7, -18),   S( -6, -10),   S( -2,  -7),
            S( -3,  -9),   S(-10, -24),   S(-16, -30),   S(-10, -21),   S( -7, -18),   S( -5, -16),   S( -1,  -2),   S( -9, -13),
            S( -7, -12),   S(  5,   5),   S(-12, -22),   S(  4,   6),   S( -6, -27),   S( -7, -14),   S(  7,  15),   S(  0,  -5),
            S(  3,   9),   S(  3,   5),   S(-19, -31),   S( -4, -30),   S( -2,  -6),   S(  7,   4),   S(  5,   4),   S( -5, -15),
            S( -2,  -6),   S(  1,  -1),   S(  6,  14),   S(  1,  -4),   S( 12,  24),   S(  7,  20),   S(  8,  13),   S( -4,  -9),
            S(  3,   9),   S(  6,   7),   S( 16,  32),   S( 18,  30),   S( 18,  33),   S( 17,  36),   S( 15,  25),   S(  0,   5),
            S( -6,  -7),   S(  0,  -1),   S(-10, -16),   S( 10,  11),   S(  4,  13),   S(  5,   8),   S(  1,   7),   S( -9, -20),

            /* queens: bucket 15 */
            S(  0,  -2),   S(  0,  -6),   S( -3,  -7),   S( -2,  -7),   S( -4,  -5),   S( -4,  -6),   S( -9, -21),   S(  2,   1),
            S(  0,  -1),   S( -4, -11),   S( -5, -12),   S( -5, -13),   S(  0,  -3),   S( -4,  -2),   S( 11,  14),   S(  3,   6),
            S(  1,  -2),   S( -4, -14),   S( -4, -11),   S( -7, -18),   S( -8, -24),   S(  2,   2),   S( -3,  -7),   S( -2,  -6),
            S( -5, -11),   S(  2,   1),   S( -6,  -9),   S( -2, -12),   S( -7, -21),   S( -7, -11),   S(  5,   5),   S(  2,   4),
            S( -2,  -4),   S( -2,  -8),   S(-12, -29),   S(-13, -36),   S( -6, -17),   S(  2,  -5),   S( -4, -10),   S(  1,  -1),
            S( -3, -10),   S( -3,  -6),   S( -1, -10),   S( -4, -11),   S( -8, -20),   S( 14,  25),   S(  4,   9),   S(  0,   0),
            S( -3,  -1),   S(  3,   0),   S(  7,   8),   S(  6,   8),   S(  8,  16),   S( 22,  41),   S( 11,  20),   S(  4,   9),
            S(  1,   1),   S( -4, -12),   S(  0,  -2),   S(  8,  11),   S(  9,  15),   S(  4,   1),   S( -3, -10),   S( -3,  -3),

            /* kings: bucket 0 */
            S(  2,  70),   S(  2,  88),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  8,  66),   S( 76,  72),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(-31,  43),   S(-73,  43),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 22,  36),   S(  0,  40),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-37,  39),   S(-53,  40),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 26,  37),   S( 23,  29),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 12,  65),   S(-14,  54),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 49,  71),   S(  3,  65),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-37, -27),   S( 30, -32),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-50, -12),   S( 10,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0, -45),   S(-25, -29),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 23, -21),   S( 17, -24),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-11, -23),   S(-37, -23),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 33, -18),   S( -7, -10),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 26,  -1),   S(-29, -13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 37,  19),   S(-29,   6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-97, -50),   S(  8, -32),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-56, -40),   S( 26, -32),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0, -51),   S( -3, -61),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 76, -62),   S( 52, -53),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 10, -63),   S(-33, -50),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 80, -70),   S( 80, -63),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -3, -44),   S(-95, -62),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 87, -59),   S(  6, -65),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-26, -41),   S( 44, -29),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-39, -85),   S(-10, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 10, -44),   S( 61, -45),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 24, -60),   S( 30, -75),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 53, -58),   S( 39, -62),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 32, -71),   S( -4, -55),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 42, -46),   S(-33, -64),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  4, -60),   S( -2, -107),

            #endregion

            /* enemy king piece square values */
            #region enemy king piece square values

            /* pawns: bucket 0 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-31,   5),   S(-42,  34),   S(-11,  13),   S(-26,  24),   S(  1,   6),   S( 25,  10),   S( 42,  -6),   S( 39, -11),
            S(-22,  -6),   S(-35,  15),   S(-18,   2),   S(-15,   0),   S(  1,   2),   S( -7,  14),   S( 29,   3),   S( 16,  20),
            S( -2,   0),   S( -5,   1),   S( 26, -23),   S(  7, -24),   S( 22, -24),   S( 27,   7),   S( 18,  21),   S( 45,  -8),
            S( 11,  17),   S( 28,  22),   S( 57, -13),   S( 41,  -6),   S( 32,  21),   S( 11,  52),   S( 28,  51),   S( 77,  13),
            S( 81,   2),   S(100,  39),   S( 93,   6),   S( 59,  26),   S( 48, 125),   S( 33,  72),   S( 24, 106),   S(107,  41),
            S(-127, -42),  S(-116, -36),  S( 57, -110),  S( 46,  47),   S(104, 153),   S( 74, 153),   S(144,  51),   S( 60, 100),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-61,  32),   S(-55,  32),   S(-34,  23),   S(-58,  60),   S(-38,  11),   S( -1,   7),   S(  9,   3),   S( -2,  16),
            S(-59,  21),   S(-47,  16),   S(-44,  16),   S(-31,  12),   S(-17,   3),   S(-24,   9),   S( -4,   2),   S(-18,  14),
            S(-39,  27),   S(-17,  25),   S(-22,  11),   S(  5,  -6),   S(  0,   7),   S( -6,  10),   S( -8,  19),   S( -1,  11),
            S(-25,  49),   S( 20,  25),   S( -3,  34),   S( 17,  38),   S( 20,  21),   S( -3,  32),   S( 20,  28),   S( 48,  27),
            S( 12,  38),   S( 75,  10),   S(116,  13),   S(113,  13),   S( 67,  28),   S( 25,  45),   S( -9,  66),   S( 59,  59),
            S(182, -30),   S( 28,  23),   S( 59, -54),   S( 53, -50),   S(-13, -16),   S(-25,  91),   S( 77, 144),   S( 75, 150),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-62,  47),   S(-42,  31),   S(-37,  19),   S(-25,  12),   S(-56,  54),   S(-29,  21),   S( -6,  -1),   S(-27,  22),
            S(-58,  34),   S(-40,  25),   S(-47,  17),   S(-45,  20),   S(-45,  20),   S(-43,  15),   S(-18,  -3),   S(-50,  17),
            S(-29,  38),   S(-34,  44),   S(-14,  17),   S(-20,  12),   S(-22,  21),   S(-11,   9),   S(-21,  16),   S(-23,  10),
            S(-15,  70),   S(-20,  57),   S( -1,  37),   S(  7,  33),   S(  3,  37),   S( -4,  28),   S( 16,  25),   S( 34,   8),
            S(-21, 104),   S(-34,  92),   S(-16,  52),   S( 44,   0),   S(118,  13),   S(110,  30),   S(100,   1),   S( 68,   8),
            S(  5, 187),   S( 91, 111),   S( 22,  74),   S( 12, -24),   S(  5, -90),   S( -8, -79),   S( 18,   6),   S(122, -26),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-12,  27),   S(-13,  28),   S(-12,  25),   S(-13,  30),   S(-30,  66),   S( 12,  32),   S( 13,  17),   S(-14,   0),
            S( -8,  31),   S(  0,  28),   S(-17,  22),   S(-18,  20),   S( -6,  18),   S(  5,  12),   S(  3,  12),   S(-33,  12),
            S( 13,  28),   S( -7,  49),   S(  5,  14),   S(  0,  -6),   S( 21, -13),   S( 27,  -6),   S(  7,   8),   S(-13,   5),
            S( 20,  65),   S( -3,  79),   S( 17,  49),   S( 24,  14),   S( 37,  -2),   S( 48, -11),   S( 27,  33),   S( 38,   4),
            S( 12, 106),   S(-13, 129),   S(-33, 151),   S( -6, 112),   S( 37,  66),   S( 97,  21),   S(104,  26),   S(103,  13),
            S( 71,  87),   S( 36, 186),   S( -8, 247),   S( 16, 188),   S(-12,  99),   S( 11, -75),   S(-88, -82),   S(-137, -83),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 66,  -1),   S( 24,   5),   S(-10, -10),   S(-13, -29),   S( -2, -16),   S(-11,  -9),   S(-19,   4),   S(-60,  28),
            S( 52, -13),   S( 16,  11),   S( 25, -23),   S(-18, -12),   S(-35, -27),   S(-17, -19),   S(-50,   1),   S(-47,   3),
            S( 68,   9),   S( 98,  -6),   S( 49, -18),   S(-19, -18),   S(-63, -13),   S(-18,  -2),   S(-54,  10),   S(-59,  10),
            S(-30, -37),   S( 48, -80),   S( 49, -17),   S(-14,   0),   S(-12,  -2),   S(-19,  30),   S(  2,  23),   S(  0,  12),
            S( 25, -42),   S(-18, -70),   S( 30, -43),   S( 65,  17),   S( 76,  68),   S( 42,  49),   S( 26,  28),   S( -6,  53),
            S( 23, -13),   S( 10, -27),   S( 18, -62),   S( 27,  29),   S( 60,  72),   S( 61, 124),   S( 35, 100),   S( 27,  64),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-52,  38),   S(-22,  27),   S( 17,   6),   S( 59,  -8),   S( 54, -11),   S( 19, -11),   S( -8,   4),   S(-59,  34),
            S(-54,  23),   S(  1,  10),   S( 18,  -3),   S( 11,   3),   S( -9,  -7),   S( -2, -12),   S(-43,   1),   S(-83,  29),
            S(-13,  27),   S( 22,  28),   S( 56,  19),   S(  7,  32),   S(-10,  17),   S( -2,   1),   S(-10,  10),   S(-53,  28),
            S( 18,  32),   S( 25,   9),   S( 10, -26),   S(-12,  -3),   S( 24, -13),   S( 19,   4),   S( 51,   5),   S( 17,  17),
            S( 74,  14),   S( 50, -24),   S( 54, -41),   S( 17, -14),   S( 80, -25),   S( 53,  22),   S( 47,  30),   S( 12,  68),
            S( 82,  36),   S( 50,  -2),   S( 19, -58),   S( 35, -44),   S( -5, -50),   S( 61,  45),   S( 72, 108),   S( 77,  84),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-79,  34),   S(-37,   8),   S( -6,  -5),   S(  0,  -2),   S( 19,  13),   S( 25,   0),   S( 33,  -3),   S(  9,  11),
            S(-75,  23),   S(-32,  -3),   S(-26,  -7),   S( 42, -20),   S( -2,   5),   S( 20,  -7),   S( 18,  -6),   S(  8,  -4),
            S(-42,  24),   S(-27,  15),   S( -9,  10),   S(  3,  11),   S( 30,  17),   S( 56,   1),   S( 57,   1),   S( 20,   0),
            S(-21,  45),   S(  4,  20),   S( 23,  10),   S( 37,  -3),   S( -5, -26),   S( 32, -33),   S( 54, -11),   S( 90, -24),
            S( 41,  49),   S( 33,  17),   S( 38,  19),   S( 29, -12),   S(  7, -30),   S( -3, -20),   S( 88, -31),   S(126, -10),
            S(118,  26),   S(107,  50),   S( 75,  23),   S( 58, -47),   S( 16, -63),   S( 21, -59),   S( 17, -21),   S( 89,  26),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-57,  11),   S(-50,  -1),   S(-10, -17),   S(-55,  26),   S( 23,  -2),   S( 60, -20),   S( 64, -23),   S( 57, -11),
            S(-54,   9),   S(-56,   5),   S(-43, -13),   S(-37,  -3),   S(  3, -16),   S( 52, -35),   S( 35, -10),   S( 53, -18),
            S(-39,  10),   S(-51,  12),   S(-34,  -6),   S(-29, -23),   S( 12, -23),   S( 44, -23),   S( 74,  -5),   S( 63,  -7),
            S( -8,  15),   S(-47,  35),   S(-21,  22),   S( -3,   0),   S( 15, -20),   S( 66, -50),   S( 23, -43),   S( 13, -70),
            S( 53,  -5),   S(-11,  59),   S( 42,  60),   S( 17,  61),   S(  0,  38),   S(  8, -31),   S(-15, -78),   S(  2, -48),
            S(125,  21),   S(112,  45),   S(100,  76),   S( 75,  80),   S( 69,  -4),   S( 25, -58),   S(  5, -54),   S( 32, -130),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 28, -25),   S(  6, -28),   S( 47, -17),   S(-11, -32),   S(-30, -60),   S( 38, -45),   S( 37, -50),   S( 29, -39),
            S(-15, -54),   S(-16, -12),   S(-45, -65),   S(-40, -49),   S(-51, -51),   S(  4, -38),   S(-16, -33),   S(-35, -30),
            S(-35, -56),   S( 27, -46),   S( -1, -38),   S(-35, -48),   S(-39, -32),   S(-30, -31),   S(-43, -21),   S(-77, -14),
            S(  2,   9),   S(-17,  -9),   S( 29,  -7),   S( 16, -14),   S( 13, -20),   S( -2,  11),   S(  0,   4),   S(-20, -10),
            S( 20,  36),   S(  5, -11),   S( 17,  25),   S( 34,  71),   S( 52,  94),   S( 35,  79),   S(  2,  49),   S(-27,  38),
            S( 17,  38),   S(  9,  25),   S( 25,  42),   S( 32,  60),   S( 42,  70),   S( 40, 118),   S( 32,  67),   S(-15,  19),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 21,  -8),   S( 64, -19),   S( 37, -19),   S(  0,  -6),   S(  6, -18),   S( 72, -48),   S( 72, -51),   S( -5, -26),
            S(-12, -34),   S(-40, -41),   S(-16, -43),   S(-25, -47),   S(-16, -50),   S(-26, -40),   S(-15, -34),   S( -1, -33),
            S(-64,  -4),   S(-22, -33),   S( -9, -57),   S(-37, -41),   S(  8, -41),   S(-12, -43),   S(-38, -32),   S(-47, -14),
            S(-31,  20),   S(-42, -35),   S(  9, -47),   S( -9, -28),   S(  3, -43),   S(-15, -16),   S( -7,   0),   S(  9, -13),
            S( -9,  17),   S(  0, -32),   S( 15, -17),   S( 28,  13),   S( 12,  31),   S( 20,  31),   S(  0,  41),   S( -5,  32),
            S(-12,  26),   S( 23,  12),   S( 10, -11),   S( 30,  10),   S( 24,  50),   S(  9,  23),   S(  8,  40),   S(  5,  49),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-21, -47),   S(-22, -36),   S(  4, -27),   S( -9, -46),   S( 39, -29),   S(154, -54),   S( 85, -37),   S( 49, -45),
            S(-58, -34),   S(-59, -43),   S( 20, -60),   S( 14, -56),   S( 20, -53),   S( 21, -47),   S( 37, -49),   S( 16, -41),
            S(-53, -24),   S(-41, -39),   S(-30, -35),   S( 20, -43),   S(-16, -48),   S(  8, -65),   S( -2, -56),   S( 44, -45),
            S( -7, -15),   S( -1, -19),   S(-12, -25),   S( -5, -53),   S(-17, -45),   S(-30, -43),   S( -6, -44),   S( -7, -25),
            S(  5,   1),   S( 27,   8),   S( 24,   7),   S(  1, -34),   S( 25, -10),   S( 20,   8),   S( -7, -24),   S( 18,   0),
            S( -7,   4),   S(  3,  13),   S( 25,   9),   S( 15,  -3),   S( 23,  24),   S(  0,  -1),   S( -9, -26),   S( 19,   9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-29, -33),   S(-10, -23),   S(-14, -38),   S(  5,  -8),   S(  4, -35),   S(107, -35),   S(107, -60),   S( 69, -48),
            S(-55, -32),   S(-56, -49),   S(-45, -53),   S( -7, -53),   S(-13, -46),   S(  0, -53),   S( -3, -38),   S( 20, -71),
            S(-60, -26),   S(-40, -34),   S(-10, -38),   S( 17, -49),   S(-39, -52),   S( 12, -37),   S(-25, -65),   S(  4, -59),
            S(-22, -14),   S(-22,  -8),   S( 31,  -6),   S( 37, -31),   S( -7, -21),   S( -2, -39),   S(-18, -26),   S(-19, -39),
            S( -1, -32),   S(-10,  16),   S( 16,  55),   S( 27,  35),   S( 32,  30),   S( 12,   2),   S( 13,   7),   S(  2,  -2),
            S( 13,   1),   S( 20,  22),   S( 32,  51),   S( 24,  43),   S( 12,  22),   S( 30,  58),   S( 14,  19),   S( 22,  27),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-35, -98),   S( -9,  -1),   S(-16, -11),   S( -2,   2),   S( -4, -25),   S(-22, -61),   S( 24, -17),   S( 10, -28),
            S(-10, -45),   S(-22,  -2),   S(-37, -67),   S(-34, -52),   S(-38, -61),   S(-19, -33),   S(-22, -46),   S(-11, -47),
            S(-25,  -5),   S(  1, -58),   S(-16, -77),   S(-32, -75),   S(-17, -57),   S(-26, -36),   S(-53, -37),   S(-47, -51),
            S(-11,   5),   S( -2,  -7),   S(-15, -36),   S( -4, -23),   S( 13,  29),   S(-10,  54),   S(-16,  23),   S(-36,  -7),
            S( 11,  10),   S(  1,   0),   S(  0,  -9),   S( 11,  42),   S( 18,  59),   S( 18,  58),   S( -4,  70),   S( -3,  46),
            S( 11,   2),   S( -1, -17),   S(  9,  22),   S(  8,  24),   S( 18,  65),   S( 16,  47),   S(-29, -34),   S(-26, -12),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-29, -57),   S(-29, -63),   S(  6,  -4),   S( -4, -20),   S( -2, -34),   S(-10, -47),   S( -4, -55),   S( -9, -55),
            S(-61, -60),   S(-35, -71),   S(-33, -56),   S(  6, -22),   S(-36, -58),   S(-43, -42),   S(-34, -52),   S(-24, -57),
            S(-27, -26),   S(-24, -56),   S(-12, -59),   S(-16, -50),   S(-29, -62),   S( -8, -52),   S(-36, -47),   S(-32, -32),
            S(-16,  22),   S( -7, -23),   S(  0, -28),   S( -4,  -6),   S( -3,   3),   S(-33,   8),   S(-15,  -9),   S(-26,   2),
            S( -8,  15),   S( -1,  12),   S( -2, -22),   S( 14,  19),   S( 22,  48),   S( 14,  56),   S(  2,  57),   S(-18,  43),
            S(  3,  52),   S( 17,  31),   S( -1, -15),   S(  5,   7),   S(  9,  40),   S( -5,   4),   S(-11,  14),   S(-11,  34),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-29, -76),   S(  0, -59),   S( -3, -45),   S( -1,   6),   S(-10, -36),   S( -2, -28),   S(  3, -58),   S(-14, -41),
            S(-27, -64),   S(-74, -73),   S(-21, -64),   S(-34, -100),  S(-24, -51),   S(-11, -43),   S(-30, -36),   S(-21, -46),
            S(-26, -51),   S(-41, -53),   S(-23, -43),   S(-14, -69),   S(-21, -67),   S(-21, -73),   S(-15, -65),   S(-37, -14),
            S(-18,  -2),   S(-27, -34),   S(-12,  -1),   S(-16, -52),   S(  1, -57),   S( -6, -26),   S(  2, -13),   S(-11,  11),
            S( -5, -39),   S(  0,  -9),   S(  1,  16),   S(-14, -32),   S( 12,  12),   S( -1, -11),   S(  8,  25),   S(  7,  44),
            S(-18,   0),   S(  1,   2),   S( 13,  44),   S( 11,  30),   S(  8,  26),   S( -9,  -6),   S( 14,  59),   S( 13,  62),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  1, -16),   S(  8, -31),   S(-19, -39),   S( -8, -19),   S(-11, -32),   S(-31, -42),   S(-23, -40),   S(-19, -95),
            S(-27, -27),   S(-34, -56),   S(-18, -69),   S( -9, -47),   S(-11, -33),   S(-36, -15),   S(-35, -25),   S(-29, -79),
            S(-40, -34),   S(-48, -45),   S(-41, -46),   S( 16, -45),   S(-31, -42),   S(-13, -52),   S(  8, -25),   S(-17, -11),
            S(-37,  -5),   S(-32, -16),   S( -5,  16),   S(-16, -18),   S(  6,   4),   S( -6, -26),   S( -3, -23),   S( -2,  20),
            S( -7,  32),   S(  0,  24),   S( -5,  36),   S(  6,  19),   S( 16,  71),   S(  4,  24),   S( 13,  39),   S(  6,  -2),
            S(-28,  -7),   S(-17, -16),   S( -1,  35),   S( 11,  27),   S( 13,  49),   S(  9,  26),   S( 10,  16),   S( 11,  36),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: bucket 0 */
            S(-75, -39),   S(-31, -27),   S(-19,  -5),   S(-15,  26),   S(-27, -19),   S(-24,  -2),   S(-14, -17),   S(-79, -42),
            S( 15, -38),   S( -3,   0),   S(-22, -12),   S( -1,  -1),   S(-10,  12),   S( -8,  -2),   S(-30, -41),   S(-26, -40),
            S(-18, -12),   S( 16, -10),   S( -2,  20),   S( 36,  35),   S(-13,  28),   S( 10,   8),   S(-23,  37),   S(-24, -25),
            S(  1,  33),   S( 34,  71),   S( 27,  49),   S( 43,  25),   S( 27,  40),   S( 13,  34),   S( 35,   2),   S( -5,  -5),
            S( 47,  60),   S(  0,  70),   S( 49,  80),   S( 37,  48),   S( 74,  34),   S(  7,  34),   S( 10,  13),   S(  5,  13),
            S( 89, -29),   S(-23,  63),   S(117,  31),   S( 60,  40),   S( 48,  50),   S(-31,  66),   S( 37,  -4),   S(-14,  20),
            S( 42, -12),   S(-27, -30),   S( 33,  26),   S( 76,  76),   S( 33,  44),   S(  5,  55),   S(-14,  16),   S(-45,   8),
            S(-109, -146), S( -3,  -2),   S( 10,   8),   S( 20,  49),   S(  5,  42),   S( 25,  38),   S(-33,   5),   S(-11,   0),

            /* knights: bucket 1 */
            S(  9, -12),   S(-62,  12),   S(-28,  17),   S(-52,  39),   S(-27,  30),   S(-21, -19),   S(-36,   0),   S( -4, -22),
            S(-27,  25),   S(-47,  56),   S(-24,  35),   S( -9,  23),   S(-20,  31),   S( -1,  34),   S(-15,  10),   S(-20, -55),
            S(-40,  29),   S( -8,  17),   S(-23,  30),   S( -6,  57),   S(-14,  39),   S( -9,  24),   S(-40,  43),   S(-15,  25),
            S(-18,  72),   S( 20,  52),   S(  2,  57),   S(-11,  71),   S( -3,  64),   S( -8,  70),   S( -7,  42),   S(-26,  46),
            S( 49,   3),   S( 12,  21),   S( 19,  79),   S(  3,  60),   S( 29,  44),   S( 14,  52),   S( -8,  53),   S(-11,  66),
            S( 11,  47),   S( 50,   6),   S( 74,  26),   S( 87,  24),   S( 43,  38),   S(-46,  83),   S( 25,  47),   S( -2,  44),
            S(  6,  -1),   S( 32,   6),   S( 23,  -9),   S( 17,  43),   S( 12,  35),   S( -2,  41),   S(  8,  76),   S(-33,  44),
            S(-170, -50),  S( 19,   5),   S(-31, -53),   S(-17,  11),   S(  0,  20),   S( 41,  56),   S( 20,  44),   S(-84,  13),

            /* knights: bucket 2 */
            S(-64,  12),   S(-43,  35),   S(-25,  13),   S(-25,  29),   S(-23,  23),   S(-52,  16),   S(-32,  18),   S(-21, -28),
            S(-20,  12),   S( -4,  43),   S(-23,  30),   S(-11,  25),   S(-28,  37),   S(-17,  25),   S(  6,  16),   S(-34,   3),
            S(-42,  62),   S(-26,  45),   S(-26,  41),   S(-23,  69),   S(-23,  60),   S(-25,  33),   S(-28,  31),   S(-11,  14),
            S(-18,  61),   S(-11,  67),   S(-19,  90),   S(-24,  94),   S(-32,  86),   S( -3,  68),   S(  3,  54),   S( -5,  42),
            S(-22,  73),   S( -2,  71),   S( -6,  75),   S(  6,  58),   S(-23,  89),   S( 16,  76),   S(-20,  76),   S( 23,  21),
            S(-49,  81),   S(-37,  82),   S(-41, 101),   S( 22,  36),   S( 46,  38),   S(123,  15),   S( 54,  34),   S(  1,  17),
            S( 12,  52),   S(-38,  68),   S( 42,  42),   S( 11,  22),   S(-18,  34),   S( -4,  16),   S( 20,  38),   S(  7,   8),
            S(-50,   8),   S( 30,  76),   S( -8,  78),   S(-14,  -1),   S(-22,   1),   S(-29, -26),   S( 18,   0),   S(-137, -63),

            /* knights: bucket 3 */
            S(-60,  32),   S(-28, -23),   S(-10,   9),   S(-14,  19),   S( -9,  15),   S(-12,   3),   S(-27,  -4),   S(-36, -54),
            S(-24,  -4),   S( -7,  29),   S( -1,  34),   S( -7,  31),   S(-10,  33),   S( 14,  23),   S( 16,  -4),   S( 14, -31),
            S(-22,  34),   S(-18,  52),   S( -5,  55),   S(  1,  74),   S(  3,  66),   S( -9,  56),   S( -3,  42),   S(  6,   1),
            S( -3,  33),   S(  8,  68),   S(  9,  76),   S(  0,  96),   S(  6, 104),   S( 14,  96),   S( 24,  88),   S(  4,  58),
            S(-12,  73),   S(  5,  71),   S( 10,  84),   S( 24, 106),   S(  2, 100),   S( 19, 115),   S(-20, 129),   S( 45, 111),
            S(-23,  70),   S( -6,  80),   S(  5,  85),   S( -5, 107),   S( 27, 108),   S( 96, 107),   S( 36, 117),   S(  6, 125),
            S(-36,  72),   S(-30,  81),   S(-41, 110),   S( 12,  94),   S( 17,  99),   S( 74,  64),   S(-12,   8),   S( 66,  27),
            S(-176,  51),  S(-38, 106),   S(-52, 119),   S( 17,  89),   S( 49, 108),   S(-65,  85),   S(-22, -33),   S(-58, -106),

            /* knights: bucket 4 */
            S(  6,  11),   S(-12, -11),   S(-64,   7),   S(-35, -16),   S(-35,   8),   S(-18, -15),   S( 21, -33),   S(-16, -21),
            S( 15,  23),   S(  5, -15),   S( -1,  15),   S(-11,   9),   S(  7, -12),   S( 22, -41),   S( -1,   3),   S(-40, -16),
            S(  1,  -9),   S( 15,   9),   S( 67,   5),   S( 92,  -8),   S( 42,  13),   S( 60, -37),   S(  6, -13),   S(  8, -35),
            S(-19, -21),   S( 27,   8),   S( 47,  -7),   S( 59,   7),   S( 57,   1),   S( 25,  11),   S(-23,  24),   S( -2,  11),
            S( -6, -50),   S( 16, -16),   S( 55,   7),   S( 45,  40),   S( 75, -12),   S(  7,  21),   S( 34, -10),   S(-30,  34),
            S( -6, -19),   S( -6, -10),   S( 40, -15),   S( 70,  19),   S(  6,  19),   S( -7,  29),   S(-17,  12),   S( 20,   9),
            S(-16, -29),   S(-20, -12),   S(  7,   7),   S( 23,  22),   S( 36,  34),   S( -4,  28),   S(  8,  41),   S(-35, -19),
            S(  4,  13),   S(-11, -34),   S( -4, -19),   S( 14,  21),   S( 15,  17),   S( -3,  20),   S( -3,  15),   S(-16, -13),

            /* knights: bucket 5 */
            S( 13,   8),   S(  6,  10),   S(-34,  26),   S(-20,  13),   S(-14,  19),   S(  5,  13),   S(-18,   1),   S(  7,  11),
            S( 16,  25),   S( 33,  24),   S( 19,   6),   S(-25,  23),   S( 29,  -1),   S(-22,  26),   S(-14,  38),   S(-49,   8),
            S(-24,  33),   S( -9,  18),   S( 50,   9),   S( 51,  20),   S( 36,  24),   S( 15,  18),   S( 10,  21),   S(-44,  13),
            S( 34,  14),   S( 25,  -5),   S( 76,  -1),   S(100, -12),   S( 88,   7),   S( 95,   2),   S( 10,  22),   S( 19,  21),
            S( 49,  15),   S( 31,   9),   S( 91, -25),   S(111, -19),   S( 90, -20),   S( 52,  20),   S( 22,   7),   S( 16,  14),
            S( -1,  -3),   S( 43,  -9),   S(  4, -35),   S(  4,   3),   S( 37,  -4),   S( 60,   5),   S( -1,  23),   S( 30,  37),
            S(  0,  11),   S(-27, -32),   S(  9, -27),   S( -9,  -2),   S( -2, -26),   S( 15,  13),   S(  8,  43),   S( 11,  34),
            S(-29, -47),   S(-25, -52),   S( 14,   8),   S(-14, -12),   S(  8,   1),   S( -1,  37),   S( 20,  43),   S( -7,  14),

            /* knights: bucket 6 */
            S( -8, -22),   S(-48,  16),   S(-18,  -1),   S(-36,  26),   S(-41,  21),   S(-10,  27),   S(-22,  31),   S(-33,  -7),
            S(  3, -14),   S(  0,  35),   S( -2,   8),   S( 27,   9),   S( 22,  25),   S(-24,  41),   S(-17,  45),   S(-40,  48),
            S( -7,  17),   S( 39,  19),   S( 57,   8),   S( 65,  24),   S( 63,  27),   S( -4,  33),   S( 37,  35),   S(-19,  49),
            S( 30,  27),   S( 75,   7),   S( 74,   9),   S(105,   0),   S(107,  -7),   S( 92,  17),   S( 36,  18),   S(-17,  48),
            S(-19,  39),   S( 38,  18),   S(121,   1),   S(124,  -7),   S( 99, -27),   S( 73,   4),   S(144, -19),   S( 34,  35),
            S( 16,  22),   S( 34,  14),   S( 68,  11),   S( 45,  10),   S( 50, -24),   S( 44, -15),   S( 12,  12),   S( 26,  19),
            S( -2,  27),   S( 19,  40),   S( 48,  48),   S(  5,  -2),   S( 30,  16),   S( 33, -12),   S( -2,   6),   S( 15,  45),
            S( 16,  27),   S(  8,  30),   S( 17,  39),   S(  5,  28),   S( 23,   6),   S(  4,  18),   S(  7,  41),   S(-20, -31),

            /* knights: bucket 7 */
            S(-30, -52),   S(-16, -40),   S( 13, -17),   S(-35,  16),   S(-10,  -5),   S(-41,  12),   S(-12,  -7),   S(-16,   7),
            S(-25, -50),   S( -2, -21),   S(-29,   6),   S(-30,  13),   S( 12,  15),   S( 17,  25),   S( -7,  23),   S(-56,  26),
            S(  9, -27),   S(-24,  -5),   S( 33, -21),   S( 46,  18),   S( 88,   4),   S( 64,  16),   S( 30,  28),   S(  6,  40),
            S(-29,  24),   S( 25,   6),   S( 81, -22),   S(116,  -8),   S(116,  -8),   S( 93,  20),   S( 60,  20),   S( 65,  12),
            S( 10,   7),   S(  0,  15),   S( 29,  12),   S( 97,  -7),   S(133,  -6),   S(158, -29),   S(187, -23),   S( 17,   6),
            S(-11,  20),   S( 36,  10),   S( 13,  11),   S( 86,   1),   S(111,   2),   S(105,  -8),   S( 15, -18),   S(  7, -32),
            S(-22,  13),   S(-10,  16),   S(  1,  23),   S( 34,  38),   S( 72,  22),   S( 38,  30),   S(-13, -24),   S(-12, -34),
            S(-32, -36),   S(-10,  13),   S(  6,  31),   S( 13,  25),   S(  9,  24),   S( 22,  18),   S( 11,  -3),   S(  0,  -7),

            /* knights: bucket 8 */
            S( -2,  -2),   S( 13,  14),   S( 14,  23),   S(-15, -51),   S(  0,  -2),   S( -4, -26),   S( 13,  18),   S( -4, -26),
            S( -7, -27),   S( -5, -32),   S( -4, -56),   S( -7, -19),   S(  1,   7),   S(  1, -19),   S( -1, -19),   S( -3, -19),
            S( -9, -48),   S(-10, -36),   S(  2, -58),   S( 17, -24),   S( 10, -40),   S( 29, -21),   S(  5, -30),   S( -1, -25),
            S(-18, -60),   S(-10, -40),   S( 15,   1),   S( 23,  -5),   S( 11, -46),   S(  0, -38),   S(-14, -39),   S(-17, -42),
            S( -6, -26),   S(  2, -33),   S(  2, -26),   S(  9, -47),   S(  9, -28),   S( -5, -41),   S(  4,  -8),   S( -4, -37),
            S(  3,  14),   S( 10, -16),   S(  0, -24),   S(  4, -29),   S(  0, -20),   S(  2, -34),   S( -7, -16),   S( -7, -20),
            S(  1,   5),   S( -2, -40),   S( -8, -24),   S(  7,  10),   S(  4, -10),   S(  0, -18),   S( -4,  -4),   S( -4,  -6),
            S(  1,   0),   S( -2,   0),   S( -6, -10),   S(  4,  -8),   S( -1,   7),   S( -4, -12),   S(  0,   1),   S( -4,  -8),

            /* knights: bucket 9 */
            S(-20, -87),   S( -4,  -5),   S( -6, -33),   S( -3, -38),   S(-16, -20),   S( -7,  -9),   S(  2, -12),   S( -1, -21),
            S( -5,  -5),   S(-16, -52),   S(-13, -101),  S(-16, -66),   S( -9, -46),   S(-11, -64),   S( -7, -43),   S(-13, -24),
            S( -3, -21),   S(-12, -36),   S( -4, -49),   S( 12, -75),   S(  1, -26),   S( 27, -15),   S( -5, -15),   S( -2, -19),
            S(-14, -41),   S( -5, -42),   S( -3, -53),   S( -3, -67),   S(  3, -44),   S( 30, -17),   S( -6, -38),   S(  5, -19),
            S(  6,  23),   S( -4, -25),   S(  2, -43),   S( -1, -55),   S( -3, -57),   S( 10, -27),   S(  0, -22),   S( -3, -26),
            S( -9, -31),   S(-16, -43),   S( -8, -39),   S( 10, -40),   S( 12, -26),   S(  4, -22),   S(  3,  -2),   S( -3,   5),
            S(-10, -28),   S(  2,   6),   S( -7,  -8),   S(-15, -26),   S(  8, -12),   S(  8,  18),   S( -9,   3),   S( -9,  -6),
            S(  3,  11),   S(  3,   3),   S( -2,   2),   S( -1,  -6),   S(-10, -27),   S( -3,  -8),   S(  3,  11),   S(  0,   5),

            /* knights: bucket 10 */
            S(-10, -50),   S( -6,  -7),   S( -9, -16),   S(-12,  -6),   S(-15, -53),   S(  9, -20),   S( -5,   2),   S( -3, -20),
            S( -7, -43),   S(  6, -19),   S( -5, -39),   S( -5, -54),   S(  5, -36),   S(-12, -67),   S( -6,   5),   S( -1,  40),
            S( -4, -21),   S(  3, -18),   S( 10, -39),   S( 22, -57),   S(-14, -62),   S( 13, -38),   S( -2, -26),   S( -3,   3),
            S( -6, -36),   S( -4, -41),   S( 22, -44),   S( 17, -42),   S(  2, -49),   S( -1, -33),   S( -3, -47),   S(  3, -14),
            S(-13, -45),   S(  1, -44),   S(  9, -42),   S( -2, -47),   S(  4, -40),   S( -2, -65),   S( -3, -13),   S(  8, -13),
            S( -3,  -9),   S( -5, -13),   S(  3, -23),   S(  4, -25),   S( -8, -38),   S(-11, -45),   S( -9, -17),   S(-13, -21),
            S(  1,  -1),   S( -1,  -8),   S(  0, -36),   S( 14, -22),   S( -3, -14),   S( -8, -50),   S( -6,  -8),   S(-12, -22),
            S( -1,  -2),   S( -2,  -7),   S( -2,  10),   S( -5, -13),   S( -2, -22),   S( -4, -29),   S(  5,   7),   S(  1,   9),

            /* knights: bucket 11 */
            S( -7, -33),   S(-26, -57),   S( -6, -27),   S(  6,   1),   S(-34, -43),   S( -2,  -2),   S( -7,  -5),   S(  7,  25),
            S(-10, -25),   S(-29, -55),   S( -7, -61),   S( 32, -27),   S( 22,  -5),   S(  2, -46),   S( -9, -25),   S( -7, -21),
            S(-12, -48),   S(-12, -43),   S( 18, -49),   S( 24, -24),   S( 10,  -3),   S( 23, -24),   S(  3, -36),   S( -1, -13),
            S(-11, -30),   S(  9, -31),   S( 18, -46),   S( 46, -29),   S( 54, -29),   S( 14, -37),   S( 15,  -4),   S(  0,  -8),
            S(-16, -20),   S(  3, -60),   S( -4, -34),   S( 23, -35),   S( 38,  -5),   S( 10, -10),   S( -6, -79),   S( -7, -14),
            S( -8, -25),   S( -7, -57),   S(  6,  -2),   S( 27, -24),   S( 26,  -1),   S(  1, -31),   S( -3, -25),   S(  0,   5),
            S( -3, -12),   S( -8,   3),   S(-10, -19),   S(  9,  -2),   S( 15,  -8),   S( 12, -41),   S(  4, -24),   S( -3,  -4),
            S( -3, -21),   S(  1,   2),   S( -5, -11),   S( -3,   9),   S( -4, -11),   S( -1, -12),   S(  5,  16),   S( -1,  -5),

            /* knights: bucket 12 */
            S(-13, -48),   S( -2, -29),   S( -3, -20),   S( -1,   5),   S( -4,  -4),   S( -4,  -8),   S( -3,  -4),   S( -2,  -8),
            S( -5, -33),   S(  0,  -2),   S( -5, -21),   S( -2, -14),   S( -2, -30),   S(  2,  -4),   S(  3,  -1),   S( -1,  -9),
            S( -2, -10),   S( -7, -39),   S( -7, -35),   S( -9, -83),   S( -2, -21),   S(  2,  -3),   S( -4, -17),   S( -6, -14),
            S(  2,   7),   S( -1, -47),   S( -7, -23),   S(  5, -21),   S(  5, -48),   S(  5,  -4),   S(  9,  17),   S(  3,  10),
            S(  0,  -5),   S( -2, -28),   S( -2, -39),   S( -3, -34),   S(  4,  -7),   S( -2, -31),   S( -2, -13),   S( -8, -24),
            S( -3,  -9),   S( -2, -15),   S( -3, -22),   S(  0, -20),   S( -5, -28),   S(-10, -36),   S(  7,   4),   S( -1,  -6),
            S( -4, -14),   S( -2,  -1),   S( -8, -12),   S( -2,   1),   S( -1,   3),   S( -8, -20),   S( -6, -18),   S( -3,  -7),
            S(  0,   1),   S(  4,  25),   S(  1,   1),   S(  0,  -7),   S(  0,   0),   S(  2,  12),   S( -1,  -6),   S(  0,   1),

            /* knights: bucket 13 */
            S( -1,  -4),   S( -3, -16),   S( -3, -23),   S( -3, -21),   S( -3, -16),   S( -3,  -7),   S( -7, -23),   S(  2,   6),
            S( -1,  -7),   S( -1, -10),   S(  2,   3),   S( -7, -34),   S( -4, -30),   S(  2,  -1),   S(  1,   6),   S( -5, -18),
            S(  2,  -1),   S(  3,   3),   S(  2, -17),   S( -5, -39),   S(  2,   0),   S( -4, -14),   S(  6,  -6),   S( -4, -16),
            S( -2,  -5),   S( -1,  -3),   S( -5, -33),   S(  5, -11),   S(  4, -37),   S(  4, -11),   S(  2,  -7),   S( 11,  15),
            S(  0,  17),   S( -2, -37),   S(  1, -40),   S( -1, -45),   S( -9, -41),   S(  2, -27),   S( -8, -39),   S( -3, -19),
            S( -3,  -9),   S(  1,   0),   S( -4,  -2),   S(  4, -29),   S( -7, -26),   S( -6, -46),   S(  2,   0),   S(  0,  -3),
            S(  1,  -1),   S(  2,   2),   S( -7, -12),   S( -5, -18),   S( -2,  -1),   S( -4, -14),   S(  1,   3),   S( -1,  -3),
            S(  2,   7),   S(  0,   7),   S( -1,  -3),   S(  2,   5),   S(  0,   2),   S(  1,   1),   S(  0,   1),   S(  0,   2),

            /* knights: bucket 14 */
            S(  0,  -4),   S( -2,  -9),   S(  5,  12),   S( -3,  -6),   S( -6, -42),   S( -1,  14),   S(  3,  -6),   S( -1,  -5),
            S( -3, -13),   S( -7, -30),   S(  4,  -1),   S(  3, -17),   S(  0, -24),   S(  0,  -1),   S( -5, -19),   S(  4,  39),
            S( -2, -15),   S( -6, -41),   S(  9,   3),   S( -9, -56),   S( -1, -28),   S(  0, -11),   S(  0,  -2),   S(  2,  15),
            S( -2, -11),   S( -5, -35),   S(-15, -59),   S(  0,  -1),   S( 11,  13),   S( -2, -32),   S( -2,  -7),   S(  1,  23),
            S(  5,  14),   S(-15, -48),   S( -9, -45),   S( -2, -16),   S(  2,  25),   S( -7, -31),   S( -5, -10),   S(  3,  -2),
            S( -1,  -6),   S(  3,  19),   S(  1,  40),   S( -2, -15),   S( -1, -13),   S( -2, -12),   S( -1,   8),   S( -4, -15),
            S(  1,   2),   S( -4,  -9),   S(  3,   6),   S(  8,  47),   S(  4,  29),   S( -6, -16),   S(  1,  -3),   S(  2,   0),
            S(  0,   0),   S( -1,  -2),   S( -1,  -4),   S(  1,   6),   S( -1,  -5),   S( -1,   0),   S(  1,   5),   S(  0,   2),

            /* knights: bucket 15 */
            S( -4, -22),   S( -2,  -7),   S(  4,  22),   S( -2,   3),   S( -5, -25),   S( -9, -40),   S( -4, -37),   S( -2, -18),
            S(  0,   4),   S(  2,  -2),   S( -5, -21),   S( 11,  23),   S(  3,  -8),   S( -8, -44),   S( -3, -16),   S(  2,   0),
            S(  0,  -7),   S( -6, -27),   S( -1, -31),   S(  8, -22),   S(-13, -71),   S( -1, -28),   S( -2, -20),   S( -2, -11),
            S( -1, -12),   S( -2,  -1),   S( -8, -35),   S( -5, -24),   S(  1, -47),   S( -3, -36),   S(  2,  -8),   S( -2,  -1),
            S( -2, -13),   S(  9,  20),   S( -4, -35),   S( -5,   0),   S( 15,  11),   S(  1, -10),   S(  6,  -3),   S(  2,   4),
            S(  1,   6),   S( -6, -13),   S( -4, -10),   S(-10, -32),   S( -6, -28),   S( -1,  10),   S(  3,  19),   S(  5,  17),
            S( -2,  -5),   S( -3, -10),   S(  4,  15),   S(  4,  11),   S(  4,  37),   S(  4,  11),   S(  0,   4),   S(  3,   7),
            S(  1,   3),   S( -1,  -7),   S(  0,   2),   S( -2,  -7),   S(  2,  10),   S(  0,   4),   S(  1,   8),   S(  1,   4),

            /* bishops: bucket 0 */
            S( 38, -20),   S(  2,  19),   S(-16,   0),   S(-21,  -4),   S( -4,   5),   S( -2,   8),   S( 83, -61),   S( 25, -10),
            S(-21, -21),   S(  4,  -8),   S( -6,  23),   S( 12,   7),   S(  9,  15),   S( 68, -18),   S( 40,  34),   S( 46, -20),
            S( 19,  11),   S(  8,  15),   S( 25,   1),   S( 15,   9),   S( 44,  -3),   S( 43,  43),   S( 49,  -1),   S( 23,  -5),
            S( 18, -27),   S( 43, -36),   S( 24,  11),   S( 72, -26),   S( 76,  25),   S( 58,  30),   S( 21,   0),   S( 15,  18),
            S( 47,  -1),   S( 42, -16),   S( 90, -18),   S( 94,  -1),   S(124, -23),   S( 19,  25),   S( 43,  10),   S( -3,  12),
            S( 35,  37),   S( 96,   1),   S( 90,   9),   S( 53,  -4),   S( 28,  12),   S( 37,  13),   S( 49,  11),   S(  3,   3),
            S(-62, -96),   S( 63,  31),   S( 87,  59),   S(  7,  -1),   S( 15,  -6),   S( 34,  10),   S( -7,  30),   S(-15,  41),
            S(-20, -41),   S( -4, -19),   S( 12,  -5),   S(-14,  -1),   S(-17,  -4),   S(-17,   6),   S(-16,  -3),   S(-24, -29),

            /* bishops: bucket 1 */
            S(-30, -20),   S(  9, -27),   S(-25,  24),   S( 25, -13),   S(-15,  23),   S(  6,   1),   S( 37, -13),   S( 38, -32),
            S(  2, -39),   S( -7,   0),   S(  8, -10),   S( -8,  10),   S( 38, -15),   S( 16,  -1),   S( 60, -21),   S(  9, -14),
            S(-19,   4),   S( 27, -14),   S(  0,  -5),   S( 28,  -6),   S(  9,  -6),   S( 51, -16),   S( 10,   1),   S( 70, -14),
            S( 30, -12),   S( 40,  -8),   S( 34,  -1),   S( 33,  -8),   S( 61, -14),   S( 15,  10),   S( 76, -20),   S( -2,  25),
            S( 24, -10),   S( 71, -14),   S( 14,  -1),   S(106, -36),   S( 66, -18),   S( 99, -31),   S( 11,   9),   S( 27,   1),
            S( 71, -33),   S( 26,   9),   S( 62,   2),   S( 55, -20),   S( 92, -29),   S(-18,  10),   S(  5,  17),   S(-22,  -3),
            S( -1, -66),   S(  7, -28),   S( -8, -19),   S( 24,  10),   S( 33,  10),   S( -4,  16),   S(  3,  -5),   S(-38,  15),
            S( -5, -33),   S(-21,  -5),   S(  1, -42),   S(-53,  13),   S( -7,   0),   S( 12,   1),   S( 23, -13),   S(-43, -32),

            /* bishops: bucket 2 */
            S( 21, -32),   S( -5, -13),   S(  0,   8),   S(-18,  21),   S( 21,  13),   S(-20,  15),   S( 31, -25),   S( -2, -10),
            S( 19, -18),   S( 17,  -9),   S(  1,   2),   S( 18,   9),   S( -8,  20),   S( 20,   0),   S(  9,  -6),   S( 19, -50),
            S( 34,  10),   S( 22,   8),   S( 15,  15),   S( -3,   8),   S(  3,  22),   S( -3,  -3),   S(  1, -16),   S(-14,   6),
            S(  9,   5),   S( 56,   3),   S( 11,  18),   S( 44,  12),   S( 11,   6),   S( -6,  29),   S(-18,  16),   S(  9,  18),
            S(  9,  11),   S( 19,  12),   S( 63,   6),   S( 33,   4),   S( 28,  10),   S( 23,   9),   S( 19,  25),   S( 40,   2),
            S(-29,  26),   S(  7,  22),   S( -5,   5),   S( 82, -19),   S( 69, -11),   S( 98,   7),   S( 68,   6),   S( 30, -37),
            S(-23,  26),   S(-15,   4),   S(-10,  24),   S(  8,  11),   S(-70,  -6),   S(-41,   4),   S(-37,  21),   S(-15, -29),
            S(-67, -16),   S(-14,  14),   S(  4,  11),   S(-35,  38),   S(-20, -15),   S(-33,   4),   S(-15, -10),   S(-46, -33),

            /* bishops: bucket 3 */
            S( 32,   8),   S( 47, -15),   S(  4,   3),   S(  5,  22),   S( 10,  36),   S( -8,  50),   S( -9,  60),   S(  0,  -3),
            S( 34,   9),   S( 21,  22),   S( 22,  22),   S( 18,  29),   S( 21,  32),   S( 23,  35),   S( 14,  22),   S( 29, -18),
            S( -2,  28),   S( 30,  56),   S( 25,  55),   S( 24,  44),   S( 15,  57),   S( 17,  33),   S(  9,  24),   S(  5,  37),
            S(-14,  29),   S( 13,  45),   S( 35,  61),   S( 45,  54),   S( 43,  36),   S( 18,  37),   S( 20,  15),   S( 34, -15),
            S( 15,  31),   S( 12,  45),   S(  5,  59),   S( 52,  57),   S( 44,  59),   S( 52,  39),   S( 22,  36),   S( -6,  50),
            S( 14,  32),   S( 28,  56),   S(  7,  34),   S(  9,  51),   S( 49,  46),   S( 46,  80),   S( 41,  65),   S( 15, 112),
            S(-21,  71),   S( 11,  49),   S(  7,  37),   S(  2,  53),   S(-13,  70),   S( 27,  75),   S(-46,  41),   S( -1,  -9),
            S(-54,  54),   S(-31,  56),   S(-54,  54),   S(-46,  79),   S( -4,  56),   S(-105,  89),  S(  9,  19),   S( 12,  23),

            /* bishops: bucket 4 */
            S(-37,  -2),   S(-31, -18),   S(-43,   2),   S(-41,   1),   S(-44,   4),   S(-10,  -8),   S(-15, -17),   S(-29, -61),
            S(-25,  15),   S(-17, -10),   S( 51, -27),   S(-25,   8),   S(-44,  20),   S( -8, -24),   S( -7, -22),   S(-31, -33),
            S(  0,  23),   S(-26,   1),   S( 47,  -9),   S( 16,  -9),   S( 27, -13),   S(-18,  -4),   S(-14, -27),   S(-31, -22),
            S( 31,  -6),   S( 48,  -5),   S( 41,  -7),   S( 45, -11),   S( 27,  -9),   S( 57, -27),   S(-44,   6),   S(-10,  -4),
            S( 14,   8),   S( -5, -39),   S( 47, -25),   S( 62, -42),   S( 47, -24),   S( 34, -17),   S(-10,   5),   S(-44, -11),
            S(-52, -69),   S(-23, -43),   S( 31, -22),   S( 30, -14),   S( -2,  -4),   S( 21,   2),   S(-13,   9),   S(-12,  16),
            S( -2, -10),   S( -6, -35),   S(  7, -34),   S(-14, -20),   S(-11,   7),   S( 22,   8),   S(-15,   6),   S(  8,  26),
            S( -7, -22),   S(  3, -27),   S(  0, -14),   S(  4,  -2),   S(  0,  -7),   S( -2,  18),   S( -6,  45),   S(  3,  19),

            /* bishops: bucket 5 */
            S(-68,   0),   S(-13, -17),   S(-62,   5),   S(-54,  12),   S(-11, -12),   S(-44,  -1),   S(-42,   3),   S(-46, -14),
            S(-39,  -9),   S(-32,  -1),   S( 23, -20),   S( -5,  -9),   S(-35,   5),   S( -9,   1),   S( -2, -11),   S(  1, -24),
            S( 11,   9),   S(-32,  -4),   S( 45, -24),   S( 26, -16),   S( 18, -11),   S(-33,   7),   S(  6,  -4),   S( -7,   5),
            S( 29,   1),   S(-14,   2),   S( 62, -31),   S( 81, -33),   S( 15, -14),   S( 55, -18),   S(-45,   7),   S( -6,  -2),
            S( 18, -19),   S( 13, -18),   S( 14, -24),   S(  5, -40),   S( 42, -49),   S( 32, -30),   S( 39, -22),   S(-36,  -8),
            S( -4, -15),   S( -6, -12),   S( 36, -38),   S(-22, -29),   S( -6, -22),   S( 50, -34),   S( -5,  -5),   S(-18,  10),
            S(-33, -29),   S( -7, -24),   S(-20, -20),   S(-10,   9),   S(  7, -20),   S( 10,  -2),   S(  9,   7),   S(-23,  -1),
            S(-18, -19),   S(-25, -34),   S( -2, -26),   S( -2, -17),   S(-25,  22),   S(  1,  16),   S(-31,   3),   S(-11,  19),

            /* bishops: bucket 6 */
            S(-52, -13),   S(-33,  -1),   S(-43,  11),   S(-10,  -2),   S(-45,  11),   S(-18,  -2),   S(-61,  10),   S(-67,   3),
            S(-24, -12),   S(-16, -18),   S(-31,  15),   S(-25,   3),   S(-26,   4),   S(-32,  -3),   S(-31,  11),   S(-47,  -6),
            S( 13, -13),   S(-30,   6),   S( 31, -13),   S(  7,  -1),   S( 24,  -8),   S( 21, -16),   S(-20,  -5),   S(-29,  23),
            S(-25,   0),   S( -5, -16),   S( 24, -10),   S( 92, -25),   S( 74, -24),   S( 51, -20),   S( 19, -18),   S(-13,  12),
            S(-49,   2),   S( -1,  -9),   S( 51, -28),   S(100, -42),   S( 16, -37),   S(  5, -25),   S( 25, -24),   S(-14,  -8),
            S(-35,   9),   S(  1,  -4),   S(  0, -12),   S( 20, -22),   S( -8,  -4),   S( -1, -20),   S( 10,  -3),   S(-12, -16),
            S(-38,   2),   S(-47,  16),   S( 12, -12),   S( -8, -10),   S(-21,  -3),   S( -3, -13),   S( 12, -19),   S(-38,  -1),
            S(-38,   4),   S(-26,  -1),   S(-18,  15),   S( -6,  16),   S( -4,   9),   S( 28, -29),   S(-23,  -5),   S( -4, -23),

            /* bishops: bucket 7 */
            S( -8, -33),   S(-52, -13),   S(-41,  -9),   S(-13,   5),   S(-32,   5),   S(-42,  -6),   S(-66, -10),   S(-46, -25),
            S(  1, -61),   S( 13, -37),   S( 28, -20),   S(-17,  -7),   S(-27,   9),   S(-38,   2),   S(-34, -27),   S(-15,  -4),
            S(-31, -24),   S( -4, -16),   S( 17, -13),   S( 41, -16),   S( 28, -14),   S( 28, -20),   S(-49,  12),   S(-77,  38),
            S( -8, -20),   S(-27,   0),   S(  7, -12),   S( 61, -29),   S(108, -16),   S( 20,  -7),   S( 49, -26),   S(-21,   3),
            S(-17,  -4),   S( 21, -20),   S( 22, -26),   S( 47, -34),   S( 88, -42),   S( 70, -24),   S(-18, -14),   S(-39, -16),
            S(-65,  10),   S( -3,  10),   S( 22, -15),   S(-36,   3),   S(  0,  -4),   S( 59, -23),   S( 30, -17),   S(-60, -77),
            S(-29, -12),   S(-19,   7),   S(-42,  11),   S( 17,  -2),   S(  9,  -6),   S(  8, -20),   S( 14, -18),   S( -6, -15),
            S(-25, -18),   S(-24,   8),   S(-30,  20),   S( -5,  14),   S( -6,  14),   S(  9,  -3),   S( 30, -20),   S(  0,  -4),

            /* bishops: bucket 8 */
            S( 14,  87),   S(-12,   0),   S(  3, -13),   S( -9,  39),   S(  6,  25),   S( -3, -26),   S(-17, -43),   S(-12, -42),
            S( -4,  16),   S(  9,  33),   S(  2,  28),   S( 16,  20),   S(  1,  -2),   S(  8,  -5),   S(-24, -45),   S( -9, -32),
            S( -5, -13),   S( -7, -40),   S( 25,  57),   S( 22,  -6),   S( 21,   1),   S( 15,  -2),   S(-16, -33),   S(-28, -53),
            S( -2,   5),   S( 20,  73),   S(  9,  21),   S( 10,  25),   S( 25,   1),   S( 22,  13),   S(-10,  -1),   S(  7,  -6),
            S(  7,  75),   S( 18,  75),   S(  2,  13),   S(  2,  -7),   S( -8,  24),   S(-20,   4),   S( -2, -35),   S(  2,  27),
            S( -9, -16),   S( -3,  18),   S(  8,  36),   S( 20,  38),   S( 12,  25),   S(  7,  42),   S( -1,  32),   S( -3,  10),
            S(  0,  17),   S(-13, -47),   S( 17,  42),   S(  1,  66),   S( -1,  34),   S( 10,  61),   S(  7,  63),   S(-18,  10),
            S( -2,   4),   S(  3,  13),   S(  2,   7),   S(  4,  37),   S( 15,  60),   S( 13,  60),   S(  4,  58),   S( 20, 101),

            /* bishops: bucket 9 */
            S(  1,  39),   S(-22,  17),   S(-24,   1),   S(-34, -22),   S(-24,  -8),   S( -6,  -7),   S(-10, -12),   S(-10, -23),
            S( -9,  12),   S( -9,   0),   S( -9,   2),   S(-16, -20),   S( -9, -30),   S( -1, -32),   S(-21, -29),   S(-22, -63),
            S( -3, -15),   S( -1,   5),   S(  3, -13),   S(  4,  -1),   S(  4,  -4),   S( -8, -32),   S(  5, -18),   S(-15, -13),
            S( -4,  32),   S(-18,  -8),   S( -1,  15),   S(  6,   9),   S(-11,   0),   S(  0, -25),   S(  5,  -5),   S( -2, -13),
            S( -1,  35),   S(-16,  14),   S( 14,   2),   S(-12, -19),   S( -6,  -7),   S( -8,   0),   S(  0,  -2),   S(-21, -38),
            S( -3,  37),   S(-16,  -2),   S(-13,   5),   S( -5,   2),   S(-25,   5),   S(-13,  18),   S(-10,  19),   S( -5,  19),
            S( -1,  37),   S(-14,   3),   S( -5,   2),   S(-11,   7),   S( -2,  12),   S(  2,  20),   S(  3,  28),   S( -4,  53),
            S(  5,  36),   S( -9, -20),   S( -7,   0),   S( -1,  12),   S( -2,  30),   S(-11,  25),   S( -9,  40),   S( 13,  67),

            /* bishops: bucket 10 */
            S(-13,  -9),   S( -5,  21),   S(-15, -20),   S(-26, -24),   S(-65, -28),   S(-42, -54),   S(-18,  28),   S( -4,  21),
            S(-13, -20),   S( -5, -31),   S(-12, -20),   S(-37, -43),   S(-31, -32),   S(-33, -41),   S(-22, -34),   S( -8,  27),
            S(-13, -48),   S(-17, -49),   S( -9, -33),   S(  1, -18),   S( -6, -29),   S( -3, -28),   S(-18,  -5),   S(  0, -40),
            S(-12, -22),   S(-22, -33),   S(-33, -42),   S(  4, -30),   S( -7, -10),   S( 11,   3),   S(  8,  25),   S(-18,  -8),
            S(-12,  -5),   S(-49,  -9),   S( -3, -27),   S(  2, -21),   S(  7, -25),   S(  0,   3),   S(-22, -11),   S( -4,   9),
            S(-13,  11),   S(-12,   2),   S(-24,   3),   S(-14,   6),   S(-14, -21),   S(-12,  -8),   S(-13,   8),   S(  1,  32),
            S(  0,  33),   S(-22,  13),   S(-15,  10),   S( -6,  16),   S(-12, -10),   S(-24, -37),   S(-11, -13),   S(  5,  45),
            S( -8,  38),   S( -7,  43),   S(  2,  45),   S(-12,   8),   S(-18,   7),   S(-11,  -4),   S(  2,   2),   S( -4,  -4),

            /* bishops: bucket 11 */
            S(  6,   0),   S(-15, -29),   S(-20,   6),   S(-12,  20),   S(-22,   1),   S(  5,  -5),   S(-25, -11),   S(-31,  34),
            S(-12,  -6),   S( 11, -25),   S( -3,  -5),   S(  2, -10),   S(  0,   0),   S(-45, -12),   S(-32,   8),   S(  5,  35),
            S(-10, -42),   S( -7, -14),   S(  3, -27),   S( -6, -19),   S(  5, -12),   S( 20,  35),   S(  0,  -6),   S(  0, -11),
            S(  4,  11),   S( -4, -31),   S(  5, -13),   S(-10, -45),   S(  7,  -5),   S( 22,  46),   S( 19,  58),   S( -7, -27),
            S(-18,   9),   S(-16,  -9),   S(-23,  10),   S(-28,  11),   S( -1, -16),   S( 11,  23),   S( -7,  21),   S(  6,  46),
            S(-10,  25),   S(-16,  -1),   S(-19,  34),   S( -6,  22),   S( -9,  51),   S(-10,  17),   S( -4,  -7),   S( -6,   5),
            S(-14,  21),   S( -5,  87),   S(  9,  50),   S(  9,  54),   S(  3,  27),   S( -8,  -2),   S(-16, -40),   S(-10,  -9),
            S(  9,  98),   S(-14,  37),   S( 10,  74),   S(  4,  51),   S(  9,  45),   S( -2,  16),   S(-11,   5),   S(  4,  11),

            /* bishops: bucket 12 */
            S( -5, -14),   S( -5, -25),   S(  1,  -1),   S(  5,  40),   S(-11, -20),   S( -6,  -7),   S( -2,   0),   S( -1,  -4),
            S(  0,  -3),   S(  6,  24),   S(  2,   7),   S(  3,   4),   S(  3,   2),   S(  8,   2),   S(-14, -22),   S( -3, -17),
            S(  7,  40),   S(  7,  33),   S( 15,  53),   S( 16,  19),   S(  2,   1),   S( -1, -37),   S(  3,  -1),   S( -5,  -2),
            S(  6,  64),   S(  8,  60),   S(  9,  39),   S( 11,  28),   S( 11,  -2),   S(  5,  -4),   S(  2,   7),   S(  2,   4),
            S( 12,  23),   S( 10,  31),   S(  0,   7),   S(  9,  28),   S( 14,  37),   S(  9,  33),   S(  7,   6),   S(  3,  10),
            S(  1,   9),   S( -8, -19),   S( -3,   5),   S( -2,   4),   S( 18,  69),   S( 14,  50),   S( -9, -25),   S( -1,  -9),
            S( -3,  -1),   S(  5,  20),   S(  3,   0),   S(  5,  10),   S(  6,  29),   S( 14,  64),   S( 13,  42),   S( -2,  17),
            S(  1,   7),   S( -1,   4),   S(  0,   1),   S(  0,   3),   S(  3,  12),   S(  4,  24),   S(  7,  62),   S(  7,  41),

            /* bishops: bucket 13 */
            S( -4,  -6),   S( -3,   9),   S( -6, -19),   S( -6,   5),   S( 11,  52),   S( -7, -23),   S(-16, -38),   S( -3, -20),
            S( -5,  10),   S( -7,  -1),   S( -2,   8),   S(  8,  49),   S( -7,   0),   S(  8,  10),   S(  0, -14),   S( -1,  -7),
            S(  2,  30),   S( 19,  81),   S(  4,  21),   S( 15,  29),   S(  1,   4),   S( 16,  39),   S( -3,   1),   S( -8, -17),
            S( 17,  96),   S( 15,  84),   S(  5,  37),   S(-17, -23),   S(  9,  27),   S( -1,   5),   S(  4,  38),   S(  2,  17),
            S(  9,  71),   S(  4,  32),   S(  2,   0),   S(  0,   6),   S(  0,  -9),   S(  1,  35),   S(  7,  24),   S( -1,  23),
            S(  1,  45),   S( -5,   1),   S( -5,   4),   S(  6,  14),   S( -9,  39),   S( -3, -13),   S( -4,   8),   S(  7,  32),
            S(  6,  28),   S( -6, -13),   S( -8, -20),   S( -4,  13),   S( -1,  14),   S(  8,  58),   S(  6,  22),   S(  5,  51),
            S(  1,  -4),   S( -3,  -2),   S( -3,  -5),   S(  1,   9),   S(  3,  25),   S( -1,   1),   S(  9,  53),   S(  8,  43),

            /* bishops: bucket 14 */
            S(-12, -26),   S(  1,  10),   S( 12,  23),   S( -1,  36),   S(-11, -17),   S( -6,  -5),   S( -6, -22),   S( -6, -23),
            S( -2,  -6),   S( -1, -11),   S( -4,  18),   S( -3,  -2),   S(  8,  35),   S(  0,  14),   S( -1,  18),   S(  4,  32),
            S( -2,  -1),   S( -6,  -6),   S( -6, -13),   S( 15,  32),   S( 14,  48),   S(  5,  51),   S(  2,  54),   S(  1,  44),
            S(  3,  29),   S(  5,  12),   S( -6,   8),   S(-11,  13),   S( -3,  -8),   S(  4,  33),   S(  8,  66),   S(  4,  56),
            S(  4,  36),   S(  0,  19),   S( -7,  13),   S( -1,  18),   S( -7, -23),   S(  1,  26),   S(  9,  29),   S(  7,  69),
            S( -1,  19),   S( 12,  41),   S(  0,  27),   S(  3,  23),   S( -1,  30),   S( -7,  -6),   S( -5, -11),   S( 10,  33),
            S( 10,  64),   S(  5,  25),   S(  6,  45),   S(  4,  18),   S(  1,  12),   S( -1,   2),   S( -2, -21),   S(  1,  22),
            S( 10,  63),   S(  6,  50),   S(  1,  12),   S(  2,  16),   S( -3,  -9),   S( -1,  -2),   S(  6,  18),   S(  1,   3),

            /* bishops: bucket 15 */
            S( -3,  -8),   S( -4,  -9),   S( -8, -24),   S( -2, -12),   S(-11,  -8),   S(  1, -18),   S( -5, -33),   S( -3, -17),
            S(  4,  17),   S( -1,  -3),   S(  0,  -6),   S(  5,  14),   S(  8,   7),   S( -1,   1),   S(  0,   6),   S( -3,  -6),
            S(  1,  -8),   S(  1,  -2),   S( -3,   8),   S( 14,  21),   S( 13,  27),   S(  7,  11),   S( 10,  52),   S(  5,  40),
            S(  1,   0),   S( 10,  26),   S( 12,  37),   S(-13, -31),   S( -2,  13),   S(  5,  27),   S(  9,  46),   S(  5,  53),
            S( -6,  -1),   S(  3,   8),   S( -2,  20),   S( 16,  59),   S(  4,  25),   S(  8,  28),   S(  2,  24),   S( -3,   2),
            S(  0,   0),   S(  3,  24),   S( 10,  51),   S(  4,  25),   S( 14,  45),   S(  5,  33),   S(  0,   9),   S(  1,   3),
            S(  3,  23),   S(  4,  19),   S(  2,  50),   S( 14,  47),   S(  7,  45),   S( -3,  -3),   S(  0,  12),   S(  0,   4),
            S(  2,  19),   S(  7,  55),   S(  6,  40),   S(  6,  16),   S(  4,  22),   S(  1,  10),   S(  4,  16),   S(  3,  10),

            /* rooks: bucket 0 */
            S( -5,  24),   S( 19,  12),   S(  0,  10),   S(  3,  19),   S(-19,  62),   S( -5,  41),   S(-35,  75),   S(-44,  59),
            S(  6,   6),   S(  8,  31),   S(-31,  33),   S( -2,  34),   S( -2,  50),   S( -2,  34),   S( -9,  27),   S(-16,  60),
            S( 25, -16),   S(  9,   5),   S(-25,  34),   S(  0,  13),   S(-30,  54),   S(-17,  37),   S(-12,  56),   S(  0,  31),
            S( -4,   8),   S( 39,   8),   S(-41,  40),   S( 16,  10),   S( 12,  37),   S(-21,  51),   S(-17,  64),   S(-10,  40),
            S( 33, -26),   S( 32,  23),   S(  1,  44),   S( 16,  38),   S( 20,  37),   S( 21,  58),   S( 28,  63),   S( 11,  64),
            S( 35,   9),   S( 50,  47),   S( 61,  33),   S( 85,  43),   S( 10,  63),   S( 28,  70),   S( -2,  85),   S(-29,  82),
            S( 35,  23),   S( 60,  68),   S( 89,  44),   S( 51,  23),   S( 63,  32),   S( 19,  57),   S(  2,  72),   S(-20,  73),
            S( 26,  17),   S( 40,  51),   S( 34,  47),   S( 64,  15),   S( 63,  42),   S( 78,  30),   S( 62,  43),   S( 67,  -1),

            /* rooks: bucket 1 */
            S(-54,  65),   S(-21,  23),   S( -7,  27),   S(-36,  43),   S(-36,  53),   S(-37,  49),   S(-44,  75),   S(-75,  90),
            S(-52,  57),   S(-32,  23),   S(-32,  38),   S(-31,  38),   S(-39,  32),   S(-46,  43),   S(-20,  31),   S(-31,  64),
            S(-39,  32),   S(-26,  13),   S(-17,  13),   S(-25,  26),   S(-42,  31),   S(-54,  30),   S(-63,  69),   S(-20,  61),
            S(-56,  48),   S(-26,  27),   S(-25,  35),   S(-35,  28),   S(-47,  40),   S(-65,  63),   S(-29,  49),   S(-67,  84),
            S(-42,  57),   S(-11,  15),   S( 20,  23),   S( 11,  22),   S(-17,  39),   S(-25,  73),   S( -7,  63),   S(-22,  94),
            S( 57,  36),   S( 60,  22),   S( 47,  21),   S( -3,  48),   S( -1,  39),   S(  3,  63),   S( 31,  54),   S( 17,  85),
            S( 28,  52),   S( 41,  11),   S( 25,  24),   S( 15,  32),   S( 35,  27),   S( 16,  43),   S( 27,  72),   S( 45,  79),
            S( 57,  19),   S( 39,  15),   S( 12,  10),   S( -5,   2),   S( 45,  20),   S( 30,  37),   S( 46,  58),   S( 74,  59),

            /* rooks: bucket 2 */
            S(-64, 101),   S(-48,  82),   S(-44,  72),   S(-39,  47),   S(-27,  52),   S(-39,  50),   S(-31,  47),   S(-71,  84),
            S(-53,  88),   S(-55,  82),   S(-56,  72),   S(-56,  60),   S(-58,  65),   S(-52,  49),   S(-21,  33),   S(-45,  61),
            S(-50,  84),   S(-40,  75),   S(-52,  56),   S(-47,  57),   S(-43,  51),   S(-37,  47),   S(-23,  35),   S(-17,  47),
            S(-40,  86),   S(-35,  72),   S(-61,  67),   S(-77,  68),   S(-51,  60),   S(-36,  50),   S(-26,  50),   S(-31,  55),
            S(-29, 104),   S(-45, 101),   S(-22,  82),   S(-42,  66),   S(-44,  76),   S( 11,  48),   S(-18,  71),   S(-18,  85),
            S(  9, 108),   S( 10,  95),   S( 14,  82),   S(-30,  79),   S( 42,  46),   S( 37,  68),   S(101,  36),   S( 60,  78),
            S( 63,  75),   S(  6,  90),   S( 48,  47),   S( 48,  30),   S( 13,  26),   S( 42,  70),   S(-39, 101),   S( 22,  84),
            S( 43,  74),   S( 55,  75),   S( 50,  52),   S(  1,  49),   S(-29,  52),   S( 29,  38),   S( 31,  63),   S( 39,  70),

            /* rooks: bucket 3 */
            S(-10, 114),   S( -3, 108),   S( -8, 127),   S( -2, 106),   S(  6,  81),   S( 11,  76),   S( 27,  65),   S( -2,  46),
            S(  3, 104),   S( -9, 119),   S(-17, 134),   S( -7, 119),   S( -4,  86),   S( 13,  55),   S( 53,  35),   S( 23,  63),
            S( 12,  95),   S( -5, 120),   S(-15, 121),   S( -6, 110),   S(  7,  77),   S(  3,  69),   S( 38,  60),   S( 30,  56),
            S(  0, 124),   S( -4, 135),   S(-15, 131),   S( -8, 114),   S( -6,  91),   S(  3,  88),   S( 34,  79),   S(  2,  77),
            S( -4, 139),   S(-18, 151),   S(  9, 141),   S(  0, 129),   S( -1, 118),   S( 13, 108),   S( 57,  92),   S( 30,  98),
            S( -2, 158),   S( 13, 146),   S( 19, 148),   S( 25, 131),   S( 70,  97),   S( 96,  91),   S( 69, 115),   S( 40,  99),
            S( 12, 138),   S(  2, 144),   S( 22, 138),   S(  7, 133),   S( 24, 115),   S( 99,  79),   S(128, 126),   S(167,  97),
            S(114,  47),   S( 72, 101),   S( 47, 132),   S( 44, 117),   S( 32, 116),   S( 66, 118),   S( 66,  86),   S(129,  73),

            /* rooks: bucket 4 */
            S(-17,  -4),   S( 23,  -4),   S(  3, -20),   S(-23,  -2),   S(-51,  17),   S(-12,  33),   S(-23,   6),   S(-63,  35),
            S(-24, -24),   S(-50,  12),   S( -3, -27),   S(  1, -34),   S(  0,  -9),   S( -4,   2),   S(-34,  27),   S(  2,  19),
            S(-23,  -8),   S(-33, -25),   S(-39, -13),   S(-26, -38),   S(-53,  -7),   S(-56,  11),   S(-18,   1),   S(-60,  10),
            S(-54, -25),   S( 14,   5),   S( 11, -25),   S( 10, -29),   S( 26,  -6),   S(-26,  17),   S(-25,   7),   S(-21,  20),
            S(-17, -14),   S( 27, -18),   S( 24,  10),   S( 53, -17),   S( 56, -10),   S( 48,  24),   S( 16,  11),   S( 20,  22),
            S(-11, -34),   S( 10,   7),   S( 16, -11),   S( 24,  10),   S( 38,  15),   S( 27,  18),   S( 30,  22),   S( 38,  38),
            S(-11,  -9),   S( 38,  22),   S( 50,  -5),   S( 51, -15),   S( 57, -10),   S( -1,  11),   S(  8,   6),   S( 28,  12),
            S( 12,   1),   S( 35,  22),   S( 55, -26),   S( 18,  -9),   S( 47,  -4),   S( 20,   3),   S( 16,  13),   S( 13,  15),

            /* rooks: bucket 5 */
            S(-18,  30),   S( -3,  14),   S(  2,  15),   S( 25,  15),   S(  3,  17),   S( -2,  27),   S(-20,  51),   S(-26,  47),
            S( -5,  -1),   S(-28,  -3),   S(  9, -21),   S( 23,  -7),   S(-13,   0),   S(-17,  -1),   S(-47,  35),   S( -7,  31),
            S(-57,  17),   S(-20,  -5),   S(-14, -17),   S(-44,   3),   S(-53,   2),   S( 11, -26),   S(-56,  17),   S(-39,  18),
            S(-48,  27),   S(-14,  13),   S( 20, -11),   S( 12,   6),   S(  6,   6),   S(-19,  26),   S(-10,  26),   S( -8,  44),
            S( 48,  14),   S( 11,  25),   S( -4,  35),   S(  9,  -1),   S( -9,  24),   S( 83,  -6),   S( 33,  24),   S( 48,  38),
            S( 32,  28),   S( 12,  24),   S(  5,   9),   S( -6, -12),   S( 27,  24),   S( 26,  30),   S( 62,  23),   S( 44,  55),
            S( 28,  12),   S( 37,   7),   S( -3,  10),   S( 40,  12),   S( 46,   0),   S( 35,  -5),   S( 68,   2),   S( 27,  31),
            S( 35,  18),   S( 15,  17),   S( 58,  -1),   S( 10,  23),   S( 63,   1),   S( 34,  15),   S( 30,  35),   S( 41,  47),

            /* rooks: bucket 6 */
            S(-40,  55),   S(-21,  34),   S(-33,  30),   S(-24,  22),   S(  0,  20),   S(  5,  13),   S(  7,  18),   S(-30,  25),
            S(-69,  52),   S(  3,  15),   S(-36,  18),   S(-22,   9),   S(-14,   4),   S(-44,  17),   S(-49,  19),   S(-28,  28),
            S(-80,  38),   S(-34,  18),   S(-23,  -3),   S(-32,   3),   S(-48,  15),   S(-13,   1),   S(-29,  -6),   S(-22,  -6),
            S(-50,  52),   S(-33,  38),   S(-16,  12),   S(  2,  11),   S(-14,  15),   S(-10,  15),   S(-29,  22),   S( -9,  33),
            S(-15,  58),   S( 55,  23),   S( 91,   8),   S( 33,  15),   S( -3,  10),   S( 14,  29),   S( 48,  15),   S( 79,  14),
            S(100,  25),   S( 92,  13),   S(102,  -1),   S( 49,  -1),   S(  8, -11),   S( 23,  46),   S( 44,  11),   S( 74,  24),
            S( 29,  28),   S( 82,   3),   S( 89, -25),   S( 73, -25),   S( 40,  -7),   S( 39,  17),   S( 60,   5),   S( 43,   7),
            S( 62,   5),   S( 10,  34),   S( 18,  11),   S( 70, -13),   S( 54,  12),   S( 54,  26),   S( 66,  22),   S( 52,  22),

            /* rooks: bucket 7 */
            S(-90,  43),   S(-72,  43),   S(-72,  46),   S(-62,  30),   S(-42,   8),   S(-28,  -4),   S(-45,  36),   S(-71,  16),
            S(-78,  42),   S(-34,  25),   S(-61,  30),   S(-78,  32),   S(-55,   0),   S(-18,  -7),   S( -5,  18),   S(-13, -19),
            S(-84,  27),   S(-82,  32),   S(-52,   8),   S(-73,  20),   S(-75,  12),   S(-63,  21),   S( 16,  -8),   S(-25, -12),
            S(-92,  45),   S(-26,  18),   S(-12,   9),   S( 29, -17),   S(-15,   0),   S( 24,  -3),   S( 24,  21),   S( 19, -11),
            S( -4,  30),   S( 13,  31),   S( 46,  16),   S( 58,  -5),   S( 99, -25),   S( 92, -22),   S( 72,  15),   S(-43,   2),
            S( 20,  31),   S( 26,  19),   S(100,   0),   S(100, -24),   S( 91, -13),   S( 48,  15),   S( 27,  32),   S(  0,  -4),
            S(-15,  17),   S( 37,  -2),   S( 57,  -5),   S( 78, -29),   S( 98, -32),   S( 92, -18),   S( 61,  18),   S( 43, -11),
            S(-32,   5),   S(  9,  20),   S( 37,   3),   S( 38,  -9),   S( 36,  -8),   S( 56,   6),   S( 60,  27),   S( 20,  14),

            /* rooks: bucket 8 */
            S(  2, -67),   S( 11, -47),   S( 33, -49),   S( 33, -32),   S(-15, -56),   S(-10, -32),   S(  1, -47),   S(  0, -22),
            S(-15, -67),   S( -6, -37),   S(  8, -22),   S(-24, -65),   S(-10, -56),   S( -7, -29),   S(  1,  -5),   S(-26, -24),
            S(  6,   7),   S( -1, -11),   S( 16,   7),   S( -4,  -5),   S( -3,  15),   S( 14,  20),   S(  8,  30),   S(-16,  -7),
            S( -3, -22),   S( -1, -10),   S(  4, -13),   S( 18,  -1),   S( 11,   3),   S( 33,  23),   S(  3,   2),   S( -8, -21),
            S( -2, -28),   S( 16,  23),   S( 19,  -8),   S( 20,   6),   S(  5,   0),   S(  1, -10),   S( 12,  25),   S(  0,  -1),
            S( -3, -10),   S( 21,  -6),   S( 10, -27),   S( -6, -33),   S(  3,   1),   S(-14,   2),   S( -4,  -8),   S(  0,   7),
            S( 18,  27),   S( 32,  13),   S(  9, -17),   S( 15, -10),   S(  3,  -4),   S( 13,  14),   S(  7,  23),   S(  8,  23),
            S(  0,  27),   S( 20,  13),   S( 12,  -9),   S( 27,  30),   S( -5,   1),   S( 13,  27),   S(  8,  11),   S( 11,  27),

            /* rooks: bucket 9 */
            S(-19, -96),   S( -2, -79),   S( 14, -107),  S( 14, -74),   S( 14, -79),   S( 10, -72),   S( -9, -53),   S( -3, -59),
            S(-36, -70),   S(-11, -66),   S(-11, -67),   S(-15, -63),   S( -5, -74),   S(-10, -23),   S(-16, -49),   S(-12, -38),
            S(-12, -26),   S( -4, -29),   S(  4,  -8),   S( -7, -38),   S( 14, -45),   S(  3,  -3),   S( -3,  -6),   S( -2,  13),
            S( 13, -25),   S(  9,  -9),   S(  2,  -8),   S(  2,  -6),   S( -5, -48),   S( 17, -38),   S( -2, -19),   S(  0, -24),
            S( 24, -37),   S(  0, -26),   S( -1, -56),   S( -1, -24),   S(  0, -49),   S( -5, -27),   S( -2, -44),   S( -6, -35),
            S(  7, -41),   S(-20, -46),   S( -3, -44),   S( 23, -21),   S( 15, -38),   S(  0, -26),   S( -7, -27),   S( -7, -29),
            S( 10,  -5),   S( 13, -20),   S(  8, -43),   S( -5,  -1),   S(  4, -45),   S(  5, -23),   S(  1,  -9),   S(-10, -32),
            S( -7,  -5),   S(  3,  -3),   S(  4,   4),   S( 17,   9),   S( 13, -11),   S( 10,  15),   S( -6,  11),   S( 10,  19),

            /* rooks: bucket 10 */
            S( -4, -79),   S(-37, -60),   S( -9, -96),   S( 24, -93),   S( 15, -95),   S( 18, -103),  S( 21, -90),   S( -7, -77),
            S(-26, -39),   S(-27, -48),   S(-28, -56),   S(-20, -71),   S(-23, -73),   S( -6, -58),   S( -1, -45),   S(-38, -81),
            S( -8, -25),   S(-29, -28),   S(-20, -50),   S(-34, -60),   S( -9, -33),   S(  6, -28),   S(  3, -47),   S(-15, -37),
            S(-12, -28),   S(-29, -50),   S( -7, -56),   S( -8, -19),   S(  4, -14),   S(  2,   3),   S( -2, -58),   S( -1, -53),
            S(  8, -36),   S(  1, -41),   S(  1, -50),   S( -2, -57),   S( 10, -19),   S(  1, -20),   S( 12, -50),   S( -5, -64),
            S( -7, -36),   S(  0, -32),   S(  2, -65),   S(  7, -67),   S(  8, -36),   S( 10, -38),   S(-17, -54),   S( -5, -49),
            S(-27, -29),   S(-13, -45),   S( -9, -62),   S(  0, -57),   S(  7, -29),   S( -9, -27),   S(-19, -47),   S( -5, -42),
            S(-12,  -9),   S( -4,   7),   S(  2,  -8),   S( -7, -19),   S(  5,   6),   S(-21,  -6),   S(  1, -21),   S( -5,   0),

            /* rooks: bucket 11 */
            S(-28, -64),   S(-13, -39),   S(-24, -47),   S( -5, -53),   S(-29, -54),   S( 39, -77),   S(  7, -51),   S(-13, -77),
            S( -7, -19),   S(-16, -16),   S(-39, -33),   S(-47, -33),   S(-13, -35),   S( -2, -20),   S(-14, -39),   S(-22, -67),
            S(-26,   6),   S(-29,   4),   S( -5,  10),   S(-17, -10),   S( -3, -26),   S(  2, -21),   S(  9, -16),   S( -4,  11),
            S(-16, -33),   S( -7, -31),   S( -4, -19),   S( 13, -15),   S( 19,  -9),   S(-13, -42),   S(  8,  15),   S( -7, -26),
            S(  1, -38),   S(  6, -20),   S(  8, -16),   S(  5, -20),   S( 31, -25),   S( 22, -33),   S( 24,  10),   S(-14, -38),
            S( -2, -31),   S( -8, -24),   S( 11, -31),   S(  8, -33),   S(-14, -33),   S( 25, -23),   S( 37,  -8),   S(  6, -26),
            S(-12,  -5),   S(-22, -33),   S( -4, -19),   S( -7, -34),   S(  5, -25),   S( 31, -28),   S( 34,  -2),   S(  6,  -8),
            S(  1,  -5),   S( 24,  19),   S(  0,  13),   S( 17,   2),   S(-15, -12),   S( 19,  -4),   S( 47,  -3),   S(  0,  24),

            /* rooks: bucket 12 */
            S(-22, -84),   S( -5, -15),   S( -5, -43),   S( -6, -50),   S( -5, -44),   S( 11,  -7),   S(-12, -43),   S(-15, -43),
            S(  4,  -6),   S(  1,  -5),   S( 11,  13),   S(  3,  -9),   S(  7, -17),   S( 11,  -7),   S(  9,  12),   S(-12, -37),
            S( -2, -13),   S(  9,  31),   S( 11,  14),   S( 19,   8),   S(  6, -19),   S( 16,  -5),   S(  8,  22),   S( -2, -10),
            S(  6,  18),   S(  3,  12),   S( 10,   8),   S(  9,   3),   S(  9,   4),   S(  4,  -2),   S(  5,   7),   S( -3,  -8),
            S(  9,  -1),   S( 14,  24),   S(  8, -11),   S( -3, -36),   S(  4,   7),   S( -6, -22),   S(  1, -10),   S(  1,  -2),
            S( -1, -21),   S( -1, -12),   S(  5, -29),   S( -9, -37),   S(  7,  -2),   S( -4, -20),   S(  7,  14),   S(  2, -10),
            S(-13, -11),   S(  0,   3),   S( 10,   3),   S( -2, -10),   S( -3, -21),   S( 12,   9),   S(  3,  15),   S(  1,   4),
            S(  4,   7),   S(  6,  13),   S(  5, -16),   S( 10,   0),   S(  0,  -8),   S(  2,   0),   S(  1,   6),   S(  3,  18),

            /* rooks: bucket 13 */
            S( -9, -38),   S(-12, -56),   S(-16, -49),   S( -5, -35),   S(-13, -63),   S(  3, -26),   S(-20, -50),   S(-20, -36),
            S(-10, -40),   S( -5, -32),   S(  1,  -2),   S( -3, -25),   S( 20,  13),   S(  7, -22),   S(  8, -27),   S( -8, -54),
            S( -2, -37),   S( -3, -36),   S( -4, -18),   S( 10,  -9),   S( 13,  -3),   S( 15, -30),   S( 18,   2),   S( -6, -71),
            S(  7,  -9),   S( -4, -21),   S(  1, -23),   S(  8, -15),   S( 12,  -5),   S( -2, -21),   S(  1,  -8),   S(  2,   6),
            S(  6, -13),   S( -1, -72),   S( -5, -62),   S(  2, -31),   S(  3, -59),   S( -6, -42),   S(  0, -11),   S( -4, -19),
            S( -1, -26),   S( -3, -37),   S( -6, -51),   S( -7, -63),   S( -3, -89),   S( -3, -43),   S( -8, -25),   S( -1, -25),
            S(  3, -11),   S( 13,  -6),   S( -8, -40),   S(  5, -16),   S( -4, -52),   S(  5, -18),   S(  0, -13),   S( -1, -14),
            S(  6,   6),   S( -6, -11),   S(  1, -17),   S( 12,  -8),   S( -5, -41),   S(  4,  -5),   S( -1, -14),   S(  4,   4),

            /* rooks: bucket 14 */
            S(  1, -26),   S(-27, -39),   S(-11, -47),   S(-14, -79),   S( -7, -55),   S(  4, -38),   S(-20, -80),   S(-17, -60),
            S(  9, -19),   S(  8, -17),   S(  9, -34),   S( -3, -33),   S( -2, -20),   S( -3, -13),   S(  3, -25),   S(  0, -42),
            S(  4, -12),   S(  3, -18),   S( -1, -39),   S(  2, -32),   S(  5, -14),   S(  1, -11),   S(  8, -32),   S(-18, -85),
            S( -4, -13),   S(  9,  16),   S(  5,   6),   S(  5, -21),   S( -9, -43),   S(  0, -23),   S(  6, -14),   S(-12, -44),
            S(  3, -13),   S(  7,  14),   S( -8, -47),   S(  3, -56),   S(  2, -45),   S( 14, -17),   S(  1, -50),   S( -7, -40),
            S(  1, -14),   S( -1, -11),   S(  4, -34),   S( 10, -68),   S(  2, -62),   S( -5, -63),   S( -5, -64),   S(-11, -44),
            S( -6, -10),   S(  6,   9),   S(-10, -56),   S(-15, -79),   S( -5, -34),   S(  2, -22),   S( -5, -41),   S( -4, -29),
            S( -3, -25),   S( -1, -16),   S( -8, -48),   S(  2, -46),   S(-13, -58),   S(-13, -71),   S(  1, -29),   S(  0,   4),

            /* rooks: bucket 15 */
            S(-18, -57),   S(-11, -50),   S(-32, -53),   S(-16, -66),   S(  0, -31),   S( -6, -31),   S(  2,  -7),   S(-13, -52),
            S( 10,   5),   S( -5, -20),   S( -4, -30),   S( -4, -34),   S( -5, -31),   S(  3,  -5),   S(  9,  11),   S(  2,  -4),
            S( -2, -13),   S( -5, -36),   S(  9,   1),   S(  3, -35),   S(  3, -39),   S( -1, -25),   S(  6,   9),   S(  1,  -8),
            S( -1,  -2),   S( -5,  -7),   S( 14,  27),   S( -7, -10),   S(  0, -14),   S(  0, -19),   S(  7,  -2),   S(  3, -21),
            S( -1, -10),   S(  0, -16),   S(  3, -16),   S( -1, -14),   S(  1,  -3),   S( -2, -38),   S(  7, -17),   S(  1, -27),
            S(  2,   5),   S(  1,   1),   S(  3,  -1),   S(  1, -19),   S( -7, -48),   S( 10, -39),   S( 14,   0),   S( -2, -11),
            S(  2,  -2),   S( -4, -10),   S(  8,  10),   S(  3,  -4),   S( -2,  -5),   S(  3, -21),   S(  4,  -5),   S( -7, -34),
            S(  0,   3),   S(  1,   7),   S(  5,  18),   S(  0,  -7),   S( -2, -19),   S( -6, -47),   S(  8,  -2),   S(-11, -26),

            /* queens: bucket 0 */
            S( -8, -21),   S(-29, -46),   S(-39, -62),   S(  1, -99),   S( -7, -78),   S(  4, -73),   S(-61, -29),   S(-16, -10),
            S(-14, -39),   S( 16, -74),   S( 10, -77),   S(-12, -37),   S(  0, -46),   S( -5, -54),   S(-20, -39),   S(-38,  -4),
            S(-10,  12),   S( -5, -26),   S( 22, -66),   S( -5, -27),   S( -4, -11),   S( -4, -11),   S(-25, -11),   S(-75, -56),
            S(-42,  54),   S( -2,  -9),   S(-27,  29),   S(-18,  27),   S( -3,  19),   S(-16,   5),   S(-37,  -5),   S(-13, -38),
            S(-34,  31),   S(-24,  93),   S(-13,  58),   S(-20,  54),   S( -4,  37),   S(-28,  63),   S(-50,  35),   S(-43,  -1),
            S(-19,  50),   S( 13,  79),   S( 30,  71),   S(-24,  88),   S(-51,  60),   S(-52,  57),   S(-76,  32),   S(-41,  -1),
            S(  0,   0),   S(  0,   0),   S( 27,  38),   S(-17,  39),   S(-37,  41),   S(-67,  81),   S(-85,  81),   S(-98,  43),
            S(  0,   0),   S(  0,   0),   S( 33,  44),   S(  4,  16),   S(-30,  49),   S(-25,  17),   S(-52,  31),   S(-51, -10),

            /* queens: bucket 1 */
            S(  0, -29),   S(  2, -13),   S(  6, -71),   S( 26, -103),  S( 31, -76),   S(  3, -70),   S(  5, -23),   S(-10,  13),
            S(-27,   2),   S( 26, -18),   S( 37, -71),   S( 19, -25),   S( 35, -29),   S(  1, -28),   S(-26,  19),   S(-29,   6),
            S( 32, -21),   S( 15, -16),   S(  6,  -4),   S( 18,  20),   S(-13,  34),   S( 25,   1),   S( -6,  12),   S( 19, -20),
            S( 17,   4),   S( -6,  49),   S(  2,  20),   S( 23,  29),   S(  4,  40),   S(-10,  36),   S( 10,  32),   S(-16,  50),
            S( 25,  20),   S( 29,  59),   S( 28,  64),   S(  6,  60),   S( 24,  73),   S( 54,  21),   S(-22,  74),   S( -5,  75),
            S( 50,  35),   S( 95,  58),   S( 89,  70),   S( 97,  83),   S( 66,  57),   S(  0,  86),   S( 24,  63),   S(-10,  42),
            S( 80,  15),   S( 50,  24),   S(  0,   0),   S(  0,   0),   S(  9,  79),   S(-24,  80),   S(-19,  91),   S(-48,  60),
            S( 78,  39),   S( 72,  40),   S(  0,   0),   S(  0,   0),   S( 49,  50),   S( 63,  58),   S( 84,  37),   S(-18,  45),

            /* queens: bucket 2 */
            S( 25, -16),   S( 27, -23),   S( 31, -23),   S( 48, -61),   S( 48, -67),   S( 26, -50),   S( -8, -39),   S( 24,  18),
            S( 19,   6),   S( 11,  30),   S( 41, -17),   S( 44,  -6),   S( 50, -28),   S( 28, -23),   S( 27,   3),   S( 22,  29),
            S( 33,  31),   S( 24,  37),   S( 16,  59),   S( 20,  33),   S( 30,  28),   S( 21,  40),   S( 31,  21),   S( 25,  51),
            S( 25,  37),   S( 22,  73),   S( 10,  67),   S(  4,  81),   S( 24,  58),   S( 11,  74),   S( 22,  67),   S( 25,  85),
            S( -3,  83),   S( 20,  43),   S(  5,  94),   S( 19,  91),   S( 24, 105),   S( 75,  61),   S( 54, 111),   S( 59,  74),
            S(-27,  94),   S(-26, 101),   S(  8,  98),   S( 66,  84),   S( 54,  94),   S( 91, 125),   S(123,  80),   S( 27, 145),
            S(-15,  94),   S(-25, 104),   S( -7, 118),   S( 67,  69),   S(  0,   0),   S(  0,   0),   S( 11, 128),   S( 33, 101),
            S(  5,  60),   S( 37,  42),   S( 57,  40),   S( 51,  88),   S(  0,   0),   S(  0,   0),   S( 70,  89),   S( 26, 108),

            /* queens: bucket 3 */
            S(-26,  58),   S( -9,  41),   S( -2,  33),   S( 12,  45),   S( -5,  24),   S(  1,  -2),   S(  9, -33),   S(-30,  39),
            S(-39,  70),   S(-12,  67),   S(  2,  64),   S(  6,  67),   S(  6,  61),   S( 11,  34),   S( 39,   0),   S( 50, -38),
            S(-29,  70),   S(-13,  92),   S(-10, 118),   S(-11, 121),   S( -4,  97),   S( -8, 100),   S( 11,  62),   S(  7,  39),
            S(-19,  78),   S(-36, 121),   S(-23, 141),   S(-13, 142),   S(-23, 141),   S(-13, 115),   S(  3, 106),   S( -6,  94),
            S(-26, 115),   S(-20, 134),   S(-25, 139),   S(-24, 162),   S(-16, 168),   S( -4, 184),   S( -9, 164),   S(-15, 144),
            S(-38, 120),   S(-32, 149),   S(-44, 168),   S(-53, 190),   S(-36, 197),   S(  7, 197),   S(-15, 202),   S( -9, 191),
            S(-79, 160),   S(-75, 177),   S(-67, 199),   S(-77, 205),   S(-76, 228),   S( -3, 159),   S(  0,   0),   S(  0,   0),
            S(-116, 195),  S(-73, 159),   S(-55, 151),   S(-54, 161),   S(-33, 172),   S( -8, 166),   S(  0,   0),   S(  0,   0),

            /* queens: bucket 4 */
            S(-36,  -3),   S(-52, -36),   S(-20,   4),   S(-17, -29),   S(-17,  -3),   S( -5,   4),   S(-31, -26),   S( 14,  11),
            S( -5,   2),   S( -4,  12),   S( -9,  -6),   S(-26, -29),   S(-53,   8),   S(-14,  11),   S(-50, -25),   S( -3, -13),
            S( 11,  44),   S( 18, -20),   S( 11, -19),   S(  7, -16),   S( 29, -10),   S(  2,  -8),   S(-33, -27),   S( 22,  17),
            S(  1,   1),   S( 25,   5),   S(  9,  -7),   S(-16, -12),   S( 31, -16),   S( -9,  12),   S(-36, -17),   S(-14,  11),
            S(  0,   0),   S(  0,   0),   S( 24,  12),   S( 49,  22),   S( 12,  19),   S(  8,  16),   S(  3,  -1),   S(  6,  15),
            S(  0,   0),   S(  0,   0),   S( 27,  24),   S( 37,  20),   S( 22,  19),   S( 13,   9),   S(  2,  17),   S(-13,   1),
            S( 28,  23),   S( 21,  25),   S( 76,  44),   S( 69,  52),   S( 44,   7),   S(  2,   4),   S(  2,  11),   S(-20,  16),
            S( 47,  30),   S(  5,  11),   S( 42,  30),   S( 47,  32),   S(  6,   1),   S( -8,   5),   S(-13, -15),   S(  6,  -7),

            /* queens: bucket 5 */
            S( 22,  -3),   S( 17,  -5),   S(  0,  -7),   S(-25,   8),   S(  8, -24),   S( 27,  27),   S(  2,  -3),   S( 11,  -5),
            S( 10,   3),   S(  5, -17),   S( -1, -27),   S(-12,   0),   S( -4,  -4),   S(-32, -35),   S( 16,   1),   S( 12,   8),
            S( 14,  -7),   S( 40,  -7),   S(  9, -25),   S( -7,   2),   S( -3, -18),   S(  6, -10),   S( 11,  15),   S( -6,   2),
            S( 11, -17),   S( 45,  12),   S( 22, -17),   S( 16,  -6),   S( 48, -19),   S( 20,  -5),   S( 16,  25),   S(-17,  24),
            S( 52,  30),   S( 34, -11),   S(  0,   0),   S(  0,   0),   S(  7,  -6),   S( 32,   8),   S( 30,  29),   S( -8,  16),
            S( 46,  27),   S( 46,  33),   S(  0,   0),   S(  0,   0),   S( 32,  16),   S( 63,  19),   S( 30,  11),   S( 35,  27),
            S( 79,  30),   S( 83,  22),   S( 57,  56),   S( 28,  35),   S( 55,  36),   S(101,  50),   S( 65,  38),   S( 29,  16),
            S( 36,  32),   S( 66,  45),   S( 77,  40),   S( 64,  31),   S( 60,  38),   S( 63,  41),   S( 55,  28),   S( 38,  16),

            /* queens: bucket 6 */
            S( 35,  26),   S(-18, -25),   S( -1, -19),   S( 11,  -1),   S( -4,  -5),   S(-27, -13),   S(-14,  -2),   S( -8,   0),
            S( 12,   1),   S( 23,  -5),   S( 32,  -5),   S( 28,  -7),   S( 13, -13),   S( -1,  -8),   S(-37,  12),   S( 11,  21),
            S(-41,  19),   S( 11,   4),   S(  8,  -2),   S( 31, -31),   S( -2,  -5),   S( 15, -17),   S( 50,  19),   S( 52,  44),
            S( -8,  17),   S(-21,  -2),   S( 31, -18),   S( 86,  -5),   S( 33, -35),   S( 36,   7),   S( 81,  23),   S( 99,  39),
            S(  8,  26),   S(  6,  27),   S( 39,  27),   S( 48,  10),   S(  0,   0),   S(  0,   0),   S( 69,  39),   S(115,  89),
            S( 17,  35),   S( 52,  26),   S( 41,  32),   S( 35,  10),   S(  0,   0),   S(  0,   0),   S( 96,  64),   S(119,  45),
            S( 39,  14),   S( 18,   6),   S( 77,  21),   S( 66,  27),   S( 49,  45),   S( 83,  60),   S(138,  42),   S(144,  24),
            S( 15,   5),   S( 49,  13),   S( 65,  16),   S( 94,  44),   S(122,  42),   S(110,  52),   S(122,  53),   S( 97,  28),

            /* queens: bucket 7 */
            S(-25,  -1),   S(-28,   2),   S(-40,  11),   S(-20,   4),   S(-19,   0),   S(-45,  10),   S(-16,  17),   S(-24,  -6),
            S(-22,  -7),   S(-56,   4),   S(-18,  28),   S(-29,  37),   S(-36,  16),   S(-25,  18),   S(-23,  12),   S(-44,   8),
            S(-25, -10),   S(-33, -11),   S(-20,  11),   S( 24,   2),   S( 36, -11),   S(  8,   3),   S( 13,  -7),   S( 33,   4),
            S(-42,   3),   S(  3, -14),   S( 11,  -1),   S( 49, -18),   S( 85, -17),   S( 63,   1),   S( 59,  -8),   S( 28,  22),
            S( -6,  -1),   S(-29,  17),   S(  0,  31),   S( 52,  -3),   S( 79,  -5),   S( 69,  26),   S(  0,   0),   S(  0,   0),
            S(-12,  14),   S(-17,  37),   S(  5,  26),   S(  1,  20),   S( 78,  -9),   S(112,  61),   S(  0,   0),   S(  0,   0),
            S(-45,  36),   S(-17,  -4),   S(  3,  14),   S( 39,  18),   S( 78,  27),   S( 95,  31),   S( 83,  48),   S( 89,  66),
            S( 10, -19),   S( 17,  -4),   S( 29,  24),   S( 49,  -6),   S( 54,  37),   S( 43,  30),   S( 12,  39),   S( 78,  37),

            /* queens: bucket 8 */
            S( -3, -11),   S( 16,   9),   S(  0,  -9),   S(  7,   9),   S( -5, -12),   S( 10,   1),   S( -1, -12),   S(  0,  -5),
            S( -7, -13),   S(  1,   4),   S( 13,   5),   S(  7,  11),   S( 13,   8),   S( -1,  -5),   S( -1,   7),   S( -1,   0),
            S(  0,   0),   S(  0,   0),   S(  3,  -8),   S( -3, -38),   S(  6, -14),   S(  0,  -8),   S( -8, -12),   S(  1,   6),
            S(  0,   0),   S(  0,   0),   S(  3,   1),   S( -7, -26),   S(-13, -38),   S( -6, -22),   S(  9,  22),   S(  6,   5),
            S(  5,   4),   S(  8,  18),   S(  4,  -4),   S(  5, -38),   S(-20, -56),   S( -4, -17),   S(  5,  -4),   S(-11, -15),
            S( 15,   2),   S(  9,  -5),   S( 11,   5),   S( -7, -32),   S( -2, -18),   S( 12,  -7),   S(  0, -12),   S( -8, -19),
            S( -4, -17),   S(  5,  -3),   S( 10,  -3),   S( 19,  22),   S(  6, -10),   S( 10,  18),   S(  2,  -7),   S(  4,  -1),
            S(  6,   5),   S( 10,  13),   S(  7,  -1),   S(  8,  10),   S( 15,  15),   S( -1,  -1),   S(  5,   5),   S(-18, -30),

            /* queens: bucket 9 */
            S( 13,  -3),   S( -2, -25),   S( -1,  -7),   S( 28,  13),   S(  4, -10),   S(  3, -12),   S( -4,  -9),   S( -3, -14),
            S( 15,  10),   S(  3,  -7),   S( -4, -17),   S(  9,   1),   S(-11, -34),   S(  0, -10),   S(  3,  -8),   S(  1,  -4),
            S( -4, -26),   S( -9, -28),   S(  0,   0),   S(  0,   0),   S(  5,  -6),   S( 12, -13),   S( -6, -14),   S(  5,  -5),
            S( 15,   9),   S( -6, -25),   S(  0,   0),   S(  0,   0),   S( -3, -20),   S( 12,  -3),   S( 11,   7),   S( -6,   1),
            S(  6, -13),   S(  7, -10),   S( -4, -15),   S(-17, -30),   S(-19, -60),   S(  4, -16),   S(  1, -27),   S( -6, -29),
            S(  9,   2),   S( -3, -33),   S(  3, -14),   S( -6, -36),   S( -7, -44),   S( -6, -24),   S(-12, -29),   S( -3, -22),
            S(  3,   1),   S(  4, -21),   S( -7, -15),   S( -5, -11),   S( 11,  -8),   S( 10,  -3),   S(  0,   3),   S(  4, -15),
            S( -1, -21),   S( 17,   7),   S(-11, -27),   S( 11,   1),   S(  9,   2),   S( -7, -16),   S( -6, -24),   S(  8,  -7),

            /* queens: bucket 10 */
            S( 11,  18),   S( 17,   2),   S(  0,  -6),   S(  5,  -7),   S(  2, -16),   S(  2,  -1),   S(  3,  -2),   S( -3, -16),
            S(  6,   2),   S(-11, -30),   S(  3, -17),   S(-13, -40),   S(  1,  -7),   S( 14,   7),   S( -1, -20),   S(  3,   1),
            S( -2,  -2),   S(  2,  -3),   S( -2, -24),   S( -3, -23),   S(  0,   0),   S(  0,   0),   S(  5,   1),   S( -3, -11),
            S( -5, -14),   S(  8,   1),   S(  3, -15),   S(  3, -11),   S(  0,   0),   S(  0,   0),   S( -3, -17),   S(  9,  -5),
            S(  8,   7),   S( 12,  -2),   S( -6, -36),   S( 20,  -4),   S( -6, -22),   S( -2,  -8),   S(  4, -12),   S( 16,  -7),
            S( -6, -17),   S( -1, -13),   S( 12,  -2),   S(  0, -14),   S( 11,  -6),   S(  5,  -2),   S( 17,  -2),   S( -3, -30),
            S(  4,  -3),   S( 12,  21),   S(  9,   8),   S( 11,  -8),   S(  7,  -1),   S( 20,   6),   S( 10, -15),   S(  5, -13),
            S(-19, -38),   S( -3, -15),   S(  7, -18),   S(-16, -39),   S( 11,   7),   S( -9, -28),   S(  6,  -8),   S( -2, -30),

            /* queens: bucket 11 */
            S(-12,  -8),   S( -3,  -9),   S( -5, -12),   S( -7, -19),   S(  5,  14),   S( -5,  -6),   S(  6,   1),   S(  6,   4),
            S( -6,  -4),   S( -1,   0),   S(-23, -23),   S(  0,  -9),   S( 29,   2),   S(  4, -10),   S( 18,   7),   S(  4,  -6),
            S(  0,   5),   S( -1,  -9),   S(-28, -24),   S(-13, -38),   S( -1, -27),   S(-15, -34),   S(  0,   0),   S(  0,   0),
            S( -3,  -7),   S(-10,  -8),   S(-14, -15),   S(-13, -47),   S( -9, -36),   S(  0,  -3),   S(  0,   0),   S(  0,   0),
            S( -1,   6),   S(  9,   5),   S( -2, -13),   S(-17, -49),   S( 27,   9),   S( 19,  13),   S( 10,  10),   S( -5, -14),
            S( -4,  -7),   S( -8, -20),   S(-18, -31),   S( -3, -18),   S(  5,  -3),   S(  3, -21),   S(  6,  -1),   S( 16, -12),
            S( -3,  -3),   S(  3,   4),   S(  5,   0),   S( -7, -15),   S( 11,  23),   S( 17,   7),   S(  7,  13),   S( 20,  18),
            S(-20, -60),   S( 10,  15),   S( -5,  -3),   S(  5,   7),   S( 11,  16),   S(  6,  -1),   S( -2,   7),   S( 12,   9),

            /* queens: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  6,  10),   S(  0,  -1),   S(  1,  16),   S( -5, -15),   S(  3,   2),   S( -2,  -3),
            S(  0,   0),   S(  0,   0),   S( 10,  16),   S( -1, -15),   S(  4,   4),   S( -2, -10),   S( -4,  -7),   S(  3,   3),
            S( -3,  -7),   S(  9,  12),   S( -4, -16),   S( -4, -34),   S( 17,  27),   S(  2,   6),   S( -3,  -9),   S(  8,  11),
            S(  2,  -6),   S(  9,   4),   S(  4,   3),   S( -6, -18),   S(-11, -37),   S( -6, -18),   S( -5, -13),   S( -1,  -3),
            S(-12, -25),   S(  5,   8),   S( -1, -17),   S(-13, -41),   S(-10, -25),   S(-13, -42),   S( -9, -26),   S( -2,  -3),
            S(  2,   4),   S( -4,  -8),   S( -9, -27),   S( -6,  -9),   S(-15, -34),   S(-19, -28),   S(-10, -15),   S( -3,  -8),
            S( -6,  -9),   S(  5,  13),   S( -7, -17),   S(  7,  10),   S( -5, -11),   S(-13, -31),   S(  2,   4),   S( -4, -15),
            S(  7,  13),   S(  1,   2),   S(  1,   6),   S( -3,  -4),   S( -8, -15),   S(-13, -30),   S( -2,  -2),   S( -7, -12),

            /* queens: bucket 13 */
            S( -7, -24),   S( -5, -11),   S(  0,   0),   S(  0,   0),   S( -8, -17),   S(  1,  -1),   S( 10,   7),   S(  0,   2),
            S( -2, -15),   S(  3,   3),   S(  0,   0),   S(  0,   0),   S(-10, -18),   S(-13, -30),   S( -7, -15),   S( -1,  -6),
            S( -5, -17),   S(  3,  -5),   S( -2,  -6),   S( -1,  -6),   S(-11, -29),   S( -4, -17),   S( -3,  -4),   S( -2,  -3),
            S( -3,  -9),   S(-12, -34),   S(  2,  -9),   S( -9, -35),   S(  1,  -9),   S( 12,  19),   S( -1, -14),   S( -6, -13),
            S(  6,  -4),   S( -4, -20),   S(-12, -39),   S( -1, -13),   S(-28, -70),   S(-15, -40),   S( -6, -15),   S(  2,   3),
            S( -1,  -7),   S(-16, -39),   S( -6, -17),   S(  5,   2),   S(-10, -30),   S(-22, -44),   S(-17, -40),   S( -9, -24),
            S( -2,  -4),   S( -8, -15),   S(  7,  15),   S( -5, -13),   S( -3,  -9),   S(  3,  -5),   S(-16, -36),   S( -3, -11),
            S( -8, -25),   S( -2,  -6),   S( -8, -16),   S( -1,  -2),   S( -1,  -1),   S( -4, -15),   S( -1,  -5),   S(-14, -28),

            /* queens: bucket 14 */
            S(  0,  -5),   S( 11,   8),   S(  2, -11),   S(  2,  -5),   S(  0,   0),   S(  0,   0),   S(  5,   1),   S(  0, -10),
            S( -1, -13),   S(-15, -37),   S( -5, -16),   S( -1, -13),   S(  0,   0),   S(  0,   0),   S( -2,  -4),   S( -3, -11),
            S( -6, -14),   S( -3, -23),   S( -5, -23),   S( -8, -21),   S( -3,  -6),   S(  4,   6),   S( -6, -18),   S(-13, -35),
            S( -5, -14),   S( -1,  -5),   S( -1, -11),   S(-21, -55),   S( -9, -27),   S(-14, -39),   S(  0, -15),   S(  2,  -1),
            S( -6, -11),   S( -6, -22),   S(-11, -38),   S(-12, -35),   S( -6, -27),   S(-14, -34),   S( -7, -23),   S( -4, -17),
            S( -6, -12),   S( -1, -16),   S(-24, -51),   S(-23, -59),   S( -1,  -7),   S(-10, -23),   S( -6, -16),   S( -9, -19),
            S( -9, -16),   S( -5, -15),   S( -4, -11),   S( -2,  -2),   S( -1,   0),   S(  1,  -7),   S(-11, -28),   S( -1,  -1),
            S(-10, -23),   S(  4,  -6),   S(-13, -23),   S( -8, -18),   S(  0,  -1),   S( -3,  -7),   S( -4, -10),   S( -1,  -5),

            /* queens: bucket 15 */
            S(  2,  -1),   S( -3, -11),   S(  6,   2),   S( -8, -15),   S(  9,   9),   S( -4, -10),   S(  0,   0),   S(  0,   0),
            S( -5, -12),   S(  2,  -2),   S(-10, -22),   S( -4, -16),   S(  1,   0),   S(  5,  13),   S(  0,   0),   S(  0,   0),
            S( -2,  -6),   S(  0,  -3),   S(-14, -27),   S( -8, -23),   S(-13, -30),   S(  3,   0),   S( -2,  -6),   S(  0,  -1),
            S( -1,  -2),   S(-10, -21),   S( -7, -22),   S(-11, -34),   S( -3, -14),   S(  1,  -3),   S( -1,  -4),   S( -3, -14),
            S( -1,  -2),   S(  0,  -3),   S(-12, -28),   S(-19, -43),   S( -6,  -8),   S( -1,  -7),   S(  4,   7),   S( -5, -19),
            S(  1,  -1),   S( -5, -10),   S( -9, -20),   S(-17, -35),   S( -9, -18),   S(-16, -39),   S(  1,   1),   S( -5, -13),
            S( -4,  -8),   S( -1,  -4),   S( -7, -10),   S( -1,  -6),   S(-11, -21),   S(  0,   0),   S(  4,  13),   S( -4, -10),
            S( -8, -15),   S(-16, -38),   S(  0,  -8),   S( -2,  -3),   S(-12, -18),   S( -5, -12),   S(  2,   2),   S( -2,   0),

            /* kings: bucket 0 */
            S( -4, -30),   S( 26,  -5),   S( 11,  -9),   S(-23,   3),   S(-15,   6),   S( 26, -28),   S(  8,  15),   S( 14, -39),
            S(-19,  28),   S( -3,   9),   S(  0,   5),   S(-34,  16),   S(-29,  28),   S(-11,  21),   S( -5,  47),   S(-12,  33),
            S( 12,  -1),   S( 56, -18),   S( -7,   1),   S(-12,  -6),   S(-34,   5),   S(-17,  -4),   S(-33,  19),   S(  1, -17),
            S(-25, -24),   S(  2, -18),   S(-11, -16),   S(-36,  10),   S(-40,  21),   S(-26,   3),   S(-27,  13),   S(-34,  27),
            S(-44, -84),   S( 25, -27),   S( 12, -30),   S(  8,  -6),   S(-18,  -9),   S(-18,   5),   S( -3,  -3),   S(  4,  -5),
            S( -3, -101),  S( 28, -36),   S( 31, -75),   S( 15, -19),   S( 16, -22),   S( 17, -28),   S( 26,  -3),   S(-10, -11),
            S(  0,   0),   S(  0,   0),   S(  5, -32),   S( 18, -35),   S( 12, -28),   S( -1, -15),   S( -5, -26),   S(-12, -10),
            S(  0,   0),   S(  0,   0),   S( -9, -66),   S( 10, -31),   S( 14, -10),   S(  9, -19),   S( 12,  14),   S(  7,  10),

            /* kings: bucket 1 */
            S(  9, -26),   S( 27, -17),   S(  1, -13),   S( 26,  -3),   S(-12,   1),   S( 19, -10),   S(  6,  25),   S( 20, -10),
            S(  1,   5),   S(  6,  21),   S(  8, -10),   S(-36,  27),   S(-20,  19),   S( -4,  13),   S(  3,  30),   S( -8,  22),
            S( -8, -15),   S(  7, -10),   S( 10, -17),   S( 13, -19),   S(-26,  -2),   S( -1, -13),   S( 16,  -2),   S( 36, -14),
            S( 10,  -6),   S( 26, -18),   S( 14,  -9),   S( -6,   7),   S(  4,  11),   S(-29,   5),   S(  9,   3),   S(-34,  28),
            S( -9, -30),   S(  9, -25),   S( 29, -42),   S( 12, -20),   S(  7, -13),   S(  2, -16),   S( 12, -10),   S( -1,   4),
            S( 12, -18),   S( 35, -42),   S( 25, -14),   S( 39, -11),   S( 11, -23),   S( 19,   1),   S( 17,   9),   S( -5,   1),
            S( -4, -43),   S(  9,   4),   S(  0,   0),   S(  0,   0),   S(-12,   4),   S(  4,  20),   S(  7,  34),   S( -8, -36),
            S(-14, -119),  S(-10, -23),   S(  0,   0),   S(  0,   0),   S(  1, -36),   S(  3,  -7),   S(  3,  28),   S( -4, -43),

            /* kings: bucket 2 */
            S( 25, -59),   S(  6,   2),   S(  5, -20),   S( 33, -11),   S(-10,   6),   S( 28, -17),   S(  3,  33),   S( 24, -13),
            S( 11, -10),   S( -7,  31),   S( -7,   4),   S( -6,   7),   S(-15,  10),   S( -7,   4),   S( 15,  15),   S( -7,  15),
            S(-28,  -9),   S(-20,  -2),   S(  3, -16),   S(-10, -18),   S(  1,  -9),   S(  3, -22),   S( 28, -16),   S( 19, -15),
            S(  1,  18),   S( -2,   9),   S(  1,   0),   S( -7,   6),   S( 10,  -1),   S(-10, -13),   S( 24, -22),   S( 51, -20),
            S( -3, -10),   S( 15,  -7),   S(  9, -19),   S(  7, -16),   S( 40, -35),   S(-16, -32),   S( 48, -40),   S(  3, -29),
            S(  6,   3),   S(  3,  -6),   S( 20, -20),   S( 40, -33),   S( 56, -31),   S( 33,   1),   S( 65, -37),   S( 29, -25),
            S( -8,  -9),   S(  7,  25),   S(-10, -10),   S( 20,  -5),   S(  0,   0),   S(  0,   0),   S( 23,  19),   S(-10, -30),
            S( -8, -24),   S(-15, -31),   S( -3, -41),   S(  8,   5),   S(  0,   0),   S(  0,   0),   S( -3,  -9),   S(-19, -147),

            /* kings: bucket 3 */
            S(  0, -68),   S( 10, -15),   S( 18, -45),   S( -2, -23),   S( -8, -33),   S( 29, -35),   S(  3,  15),   S(  5, -28),
            S(-12,  11),   S(-19,  27),   S(-19,  -7),   S(-28,   1),   S(-44,  13),   S(  0, -13),   S(  1,  17),   S(-11,  13),
            S( 17, -41),   S(  9, -18),   S(  4, -28),   S(-22, -24),   S( -3, -12),   S( 24, -42),   S( 42, -30),   S( 50, -30),
            S(-33,  17),   S(-99,  27),   S(-81,  11),   S(-71,  13),   S(-62,  11),   S(-71,  -9),   S(-66,  -5),   S(-52, -14),
            S(-35,   6),   S(-11, -18),   S(-53, -12),   S(-45,  -5),   S(-11, -32),   S(  6, -51),   S( -4, -54),   S(  7, -70),
            S(-27, -27),   S(  6, -19),   S( 16, -40),   S(-35, -24),   S( 34, -41),   S( 78, -72),   S(108, -57),   S( 46, -113),
            S(-42, -21),   S( 28, -17),   S( -4, -33),   S( 16, -35),   S( 18, -33),   S( 42, -48),   S(  0,   0),   S(  0,   0),
            S(-12, -26),   S(  5, -24),   S(  4, -10),   S(  1, -18),   S(  3, -80),   S(  5, -29),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 4 */
            S(-42, -14),   S(  8,  29),   S(-12,  26),   S( -2,   7),   S( -7,  -8),   S(-15,  10),   S(  9,  22),   S( 31, -17),
            S(-40,  25),   S( 26,  20),   S(-13,  14),   S( -7,   3),   S( 41, -12),   S( 30,  -5),   S( 56,  -1),   S( 10,  11),
            S( -5,  18),   S( -4,  -3),   S(-19,   3),   S( -7,  -3),   S(-14,   4),   S( 16, -22),   S(-19,  -7),   S( -6,  -3),
            S(  3, -22),   S(  6,   6),   S( 26,   4),   S(  0,   6),   S( 14,  -6),   S( -5,   9),   S( -4,  13),   S( -2,  14),
            S(  0,   0),   S(  0,   0),   S( 10, -19),   S(  5,   0),   S( -5,  -1),   S(-27,  -3),   S(-19,   0),   S( -9,   6),
            S(  0,   0),   S(  0,   0),   S(  2,   7),   S( 14,   2),   S(  5,  11),   S( -9, -20),   S(  6, -14),   S(  5,   2),
            S( -2, -17),   S(  0,   8),   S(  0, -33),   S( 11,   1),   S(  4,  17),   S(-17,  -3),   S(  9,   3),   S(  0,  -5),
            S( -3,  35),   S(  3,  11),   S( -2,   1),   S(  1,  -1),   S(  5,  -2),   S( -4,  -5),   S(  0, -12),   S(  3,  13),

            /* kings: bucket 5 */
            S( 30, -11),   S(-20,  24),   S(-53,  25),   S(-43,  28),   S(-36,  26),   S(  0,  15),   S( 44,   9),   S( 40,   2),
            S( 12,  -5),   S( 43,   1),   S( 32,  -7),   S( 27,  -2),   S( 13,   0),   S( 33,  -5),   S( 34,  18),   S( 45,  -2),
            S( -4,   0),   S(-26,   8),   S(-17,  -5),   S(-21,  -4),   S( -8,   1),   S(-55,   2),   S( -4,   1),   S(  1,   3),
            S(-11,  -2),   S( 41, -15),   S( 28,  -9),   S( 10,  16),   S( 28,  10),   S(  6,   3),   S(  5,   6),   S( -3,   7),
            S(-21, -14),   S(-14, -29),   S(  0,   0),   S(  0,   0),   S( -7,   0),   S( -8, -12),   S( -6,  -2),   S(-24,  11),
            S(-29,   0),   S(-15,  -1),   S(  0,   0),   S(  0,   0),   S( -2,  18),   S(-25,   9),   S(-27,  10),   S(-14,   6),
            S(-15,  -7),   S( -2,  16),   S(  2,  28),   S( -3,  -7),   S(-11,   2),   S( -3,  12),   S(  7,  18),   S(  3,  10),
            S(-12, -32),   S(  4,  27),   S(  1,  38),   S(  2,  16),   S( -3,  12),   S( -1,  22),   S( -2,   3),   S(  2,  16),

            /* kings: bucket 6 */
            S( 27, -23),   S( 21,  -4),   S(-14,   7),   S( -2,  14),   S(-22,  21),   S(-22,  23),   S( 14,  30),   S( 20,  13),
            S( 47, -17),   S( 22,  16),   S( 22,  -7),   S( 41, -10),   S( 26,  -1),   S(  7,   9),   S( 29,  15),   S( 18,   5),
            S(  5, -11),   S(-21,   7),   S( -2,  -9),   S( -5,  -8),   S( -6,  -4),   S(-51,   3),   S(-12,   6),   S(-45,  19),
            S( -6,  16),   S( 11,   4),   S(  9,  -6),   S( 34,   4),   S( 64,   1),   S( -5,  -1),   S( 78, -22),   S( 11,  -3),
            S(-15,  -8),   S(-18,  -3),   S( -7, -10),   S(  5,  -5),   S(  0,   0),   S(  0,   0),   S(-21, -18),   S(-57, -10),
            S(-19,   5),   S(  8,  10),   S(-19,   4),   S(-10,  -8),   S(  0,   0),   S(  0,   0),   S(-28,  23),   S(-39,  -3),
            S(  3,  -7),   S(  6,  13),   S( -6,   0),   S(-10,   4),   S(  3,  12),   S( -3, -10),   S( -7,   3),   S(-22, -18),
            S( -3,  19),   S( -4,   4),   S( 10,  19),   S(  0,  13),   S( -4,   7),   S( -2,  19),   S(  2,  29),   S( -8,  -3),

            /* kings: bucket 7 */
            S( 52, -32),   S(  0,  -1),   S(-13, -15),   S(-11,  10),   S(-44,  17),   S(-44,  34),   S(-13,  41),   S(-21,  28),
            S( 20,   3),   S( 23, -13),   S( 11, -10),   S(-16,   3),   S(  2,   3),   S(-36,  22),   S(  9,  17),   S( 10,  15),
            S( 17, -18),   S( -2,  -6),   S(-25,  -1),   S(-22,  -6),   S(-30,   1),   S(-49,  11),   S(-15,   8),   S(-56,  19),
            S( -9,  12),   S( 25,  -3),   S( 12,  -5),   S( 38,  -5),   S(  7,   5),   S( 48, -22),   S( 39, -15),   S( 35, -14),
            S( -4,   0),   S( -9,  10),   S(-11,  -8),   S(-14,   1),   S( -2, -15),   S(  9, -24),   S(  0,   0),   S(  0,   0),
            S(-10, -27),   S(  9,  -4),   S( 19, -12),   S( 13,  -7),   S(  7,  -7),   S( 12,   4),   S(  0,   0),   S(  0,   0),
            S(  9,  23),   S( 14, -17),   S( 13,   1),   S(  0, -22),   S( 20, -21),   S( -1,  -2),   S( 10,   3),   S( -3, -21),
            S(  7,   9),   S( -7, -17),   S( 18,  16),   S(  6,  -6),   S(  8,   0),   S(-11, -27),   S(  7,  26),   S(-12, -28),

            /* kings: bucket 8 */
            S(-16, 102),   S(-25,  67),   S(-30,  50),   S(-17,  -1),   S(-14,   3),   S(-12,   1),   S( 33,  -6),   S( -2,  13),
            S( 14,  72),   S( 22,   5),   S(  1,  52),   S( -1,  12),   S(  3,  17),   S(  2,  -1),   S(  7,  22),   S( 27,  29),
            S(  0,   0),   S(  0,   0),   S( 25,  42),   S( 19,   1),   S( 21,  -8),   S( -2,  -8),   S( -5,  10),   S( -1,  -3),
            S(  0,   0),   S(  0,   0),   S( 16,  30),   S( 18, -26),   S(  6,  10),   S( 19,   2),   S( 18,  -2),   S(  3,  16),
            S(  0, -14),   S(  3,   8),   S(  7, -19),   S(  7,  -7),   S( -5, -22),   S(  3, -10),   S(  5,   8),   S(-12, -29),
            S(  3,  16),   S( -5, -10),   S( -2,  -4),   S(  1, -15),   S( -9,  -7),   S( -6,   1),   S( -6,  -1),   S(  3,  -1),
            S( -3, -14),   S( -3, -21),   S(  5,   0),   S( -2, -15),   S(  1, -22),   S(  3, -10),   S(  7,   1),   S(  4, -48),
            S( -5, -13),   S( -8, -35),   S(  2,  -9),   S( -2, -10),   S(  6,  21),   S( -2,  -8),   S(  4,  -7),   S(  5,   1),

            /* kings: bucket 9 */
            S(-34,  53),   S(-39,  34),   S(-55,  51),   S(-64,  31),   S(-68,  38),   S(-51,  32),   S( 50,  11),   S( 14,  33),
            S(-20,  29),   S( 26,  17),   S( -8,   1),   S( 23,  11),   S( 25,  18),   S( 18,   1),   S( 27,  28),   S( 26,   8),
            S(-21,  15),   S( -4,  14),   S(  0,   0),   S(  0,   0),   S( 13,  15),   S(-19,  -3),   S( 15,  -1),   S( -9,  13),
            S( -3, -12),   S( -3,  -9),   S(  0,   0),   S(  0,   0),   S( 11,  18),   S( 28,  -7),   S( -7,   4),   S( -4,  11),
            S( -4,   0),   S( -2,   5),   S(  2,  13),   S(  5, -19),   S(  2, -16),   S( -2,  -4),   S( -7,   9),   S(-10,  -7),
            S(  0,  24),   S( -7,  15),   S( -2,  11),   S( -6,  -5),   S( -4,   8),   S( -3,  21),   S(-18, -10),   S( -3,  36),
            S(  2,   4),   S(  1,  -4),   S(  2,  -6),   S(  1,   7),   S(  7,   6),   S( 19,   4),   S( -4, -17),   S(  1,   9),
            S(  6,  35),   S( -6,  -5),   S(  7,   8),   S( -5, -27),   S( -2, -22),   S(  1,   1),   S(  1, -19),   S(  4,  24),

            /* kings: bucket 10 */
            S( -6,  27),   S(-21,   6),   S(-17,  16),   S(-41,  33),   S(-70,  29),   S(-130,  55),  S(-13,  46),   S(-86,  84),
            S(  8,   5),   S( 22,  15),   S( 11,  -8),   S(  9,  12),   S( 63,   9),   S( 32,   9),   S( 33,  31),   S(-39,  39),
            S( -1,  15),   S( 11,   3),   S( 14,  -7),   S( -4,   3),   S(  0,   0),   S(  0,   0),   S(  8,  13),   S(-47,  20),
            S( 17,   4),   S(  8,  -9),   S( 13, -10),   S( 20,   1),   S(  0,   0),   S(  0,   0),   S(  2,  12),   S( 14,  -3),
            S( -1,  -2),   S( 25,   9),   S(  7,  -3),   S( 10, -24),   S(  3, -23),   S(  4,  13),   S(  4,  12),   S(-18,  13),
            S( -2,  21),   S( -5,  11),   S(-12,  15),   S(  2,   4),   S( -2,  15),   S( -4,  -5),   S( -9,  15),   S( -5,  13),
            S(  1, -23),   S(  1,  -4),   S(  8,  -5),   S( 11,  17),   S( 12,  14),   S( -4,  -6),   S( 10, -15),   S(  0,  20),
            S(  2,  19),   S( 11,  -5),   S(  0, -16),   S(  2,   3),   S(  4,  -1),   S(  0, -16),   S(  0, -22),   S(  6,  35),

            /* kings: bucket 11 */
            S(-34,  42),   S(  3,   5),   S(  0,  -3),   S(-15,   8),   S(-32,   4),   S(-149,  74),  S(-62,  71),   S(-152, 155),
            S( 11, -36),   S(  3,  12),   S(-15, -20),   S(  5,  11),   S( 37,   7),   S(  6,  44),   S( 32,  21),   S( 35,  38),
            S(-10, -15),   S( 17,  -1),   S( -4,  -9),   S( 18,  -2),   S( 56,  -7),   S( 22,  28),   S(  0,   0),   S(  0,   0),
            S(  2,  13),   S(  8,   9),   S( 15,  -6),   S( 42,  -7),   S( 38, -18),   S( 29,   6),   S(  0,   0),   S(  0,   0),
            S(  5,   6),   S(  0,  -8),   S(  6,  -6),   S( 16, -16),   S( 20, -13),   S(  0, -16),   S(  7,  -2),   S(  6,   4),
            S(  9,  26),   S( -4,  -2),   S( 17,  -9),   S(  0,   4),   S(  7, -15),   S(  1,  -9),   S( -7,  10),   S( -6, -13),
            S( 11,   1),   S( 14,   5),   S( 10,  24),   S(  1, -22),   S( 13,  -2),   S(  4,   8),   S(  0, -10),   S( -3, -13),
            S(  4,   6),   S(  4,  -8),   S( -7, -26),   S(  5,   0),   S( -3,  -9),   S( -3, -14),   S(  4, -13),   S(  6,  23),

            /* kings: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  1,  78),   S(-16,  15),   S( -2,  -1),   S(  8,  17),   S( 15,   8),   S( -6,  38),
            S(  0,   0),   S(  0,   0),   S( 23,  91),   S( 10,  -2),   S(  7,  25),   S(  9,  -5),   S( 19,  14),   S( -9,  28),
            S(  0,   2),   S(  0, -44),   S( 17,  26),   S(  8,  22),   S( -2, -16),   S(  2,  11),   S( -3,  -9),   S( -8,  -2),
            S( -2, -11),   S(  4,  16),   S( -5, -17),   S(  5, -38),   S( -9, -20),   S(  6,  -5),   S(-10,   2),   S( -3,  12),
            S(  9,  24),   S(  2,  22),   S(  3,  13),   S(  3, -10),   S( -3,   8),   S( -4,   5),   S(-11,   5),   S(-14, -12),
            S(  3,  14),   S(  5,  23),   S( -2,  -5),   S( -8, -27),   S( -2,  -4),   S( -6,   5),   S(-11, -15),   S(  2,  17),
            S(  4,  15),   S( -5, -15),   S(  1,   9),   S( -2,   3),   S( -6, -26),   S(  0, -10),   S( 12,  30),   S(  1, -17),
            S( -1,   2),   S(  2,  -2),   S(  0, -24),   S(  1, -13),   S( -3, -17),   S(  5,  11),   S(-10, -34),   S( -3, -12),

            /* kings: bucket 13 */
            S( -9,  85),   S( -3,  65),   S(  0,   0),   S(  0,   0),   S(  1,  68),   S(-25,   8),   S( 10,   6),   S( -9,  49),
            S( -3,  23),   S( -8, -11),   S(  0,   0),   S(  0,   0),   S( 16,  -9),   S( -7, -14),   S(-16,  21),   S( -3,  22),
            S( -9,   4),   S(  5,  38),   S( -3, -41),   S(  2,  13),   S(  9,  17),   S( -8,   2),   S(-15,  11),   S( -4,  15),
            S( -8, -21),   S(  2,  11),   S(  0, -13),   S( -1, -34),   S(  3, -37),   S(  2, -18),   S( -5,   5),   S(-14, -19),
            S(  3,  14),   S( -3,  -5),   S(  3,  15),   S( -3, -15),   S( -7,  -7),   S(  2,  12),   S(-10,   3),   S(  3,  23),
            S(  3,  15),   S( -5,   9),   S( -2,  13),   S( -3,   5),   S( -9, -14),   S( -6,   9),   S( -4, -10),   S(  1,  10),
            S(  8,  23),   S(-11, -18),   S(-11, -29),   S(  4,   9),   S( -4, -19),   S(  0, -23),   S( -4, -47),   S(  6,  22),
            S(  1,  -2),   S(  3,  23),   S(  4,  27),   S(  3,  -7),   S(  1,  -5),   S( -8, -10),   S( -2,   0),   S(  8,  24),

            /* kings: bucket 14 */
            S(  1,  57),   S(-13,  10),   S(-14,  -6),   S( -1,  20),   S(  0,   0),   S(  0,   0),   S( -8,  85),   S(-57,  77),
            S(-13,  10),   S( -8,  -5),   S(  4,   0),   S( 14,   3),   S(  0,   0),   S(  0,   0),   S( 18,  22),   S(-22,   6),
            S(  0,  12),   S(  4,  -5),   S(  8,  -2),   S(  8,   7),   S(  4, -28),   S(  5,  30),   S(  7,  41),   S(-25,  -1),
            S(  9,   1),   S( -1, -14),   S( -2, -17),   S(  4, -36),   S(-10, -54),   S(  9,  25),   S(  0,  12),   S(  2,  -4),
            S(  2,  23),   S(  2, -10),   S(-13,   0),   S(-11, -19),   S( -7,  14),   S(  3,  18),   S(  0,  31),   S(  2,  16),
            S( -6,  -8),   S( -6,  14),   S(  0,   7),   S(  0,  23),   S( -5,  -1),   S( -3, -23),   S(-11, -32),   S( -3,   2),
            S(  4,  15),   S( -6, -37),   S(  5,  28),   S(  6,  27),   S( -1,   5),   S( -3, -28),   S(-13, -67),   S(  7,  52),
            S(  0,  13),   S(  5,  51),   S(  4,  17),   S( -4, -15),   S(  6,  42),   S( -3, -20),   S(-11, -48),   S(  1, -14),

            /* kings: bucket 15 */
            S(  7,  43),   S(  1,  -4),   S(  4,   2),   S(-10,  -9),   S(-30,   8),   S(-21,  84),   S(  0,   0),   S(  0,   0),
            S( -2, -24),   S( -9,  -3),   S( -5, -15),   S( 12,  35),   S( 28, -15),   S( 32,  96),   S(  0,   0),   S(  0,   0),
            S(-13,   6),   S( 10,  -8),   S(  0, -17),   S( -7,  -5),   S(  5, -18),   S( 22,  32),   S(  9,   3),   S(-11, -27),
            S( -1,   1),   S( -7,   2),   S(  6,  11),   S( -9, -35),   S(  2, -36),   S(  6,  12),   S(  3,  55),   S( -2, -13),
            S(  9,  28),   S(-12,  23),   S( -3,   6),   S( -9, -35),   S(  1, -11),   S( -6,  10),   S( -5,  -1),   S( -3,   1),
            S(  0,  15),   S(-17, -14),   S(  3,  15),   S(  6,  17),   S( -8, -10),   S( -4,  -6),   S( -3,   5),   S(  4,   9),
            S(  6,  15),   S( -3,  14),   S( -8,  -6),   S(  0,  11),   S(  4,  10),   S(  4,  13),   S( -3,  -6),   S(  1,  12),
            S( -4,  -5),   S(  2,  10),   S( -5,  -6),   S(  3,   3),   S(  1,  23),   S(  7,  35),   S(  0,  -5),   S(  2,  -1),

            #endregion

            /* mobility weights */
            S(  8,   8),    // knights
            S(  5,   4),    // bishops
            S(  3,   3),    // rooks
            S(  1,   5),    // queens

            /* trapped pieces */
            S(-12, -216),   // knights
            S(  4, -131),   // bishops
            S(  2, -93),    // rooks
            S( 10, -51),    // queens

            /* center control */
            S(  2,   8),    // D0
            S(  2,   5),    // D1

            /* squares attacked near enemy king */
            S( 17,  -2),    // attacks to squares 1 from king
            S( 16,   2),    // attacks to squares 2 from king
            S(  5,   3),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S( 10,  16),    // friendly pawns 1 from king
            S(  1,  18),    // friendly pawns 2 from king
            S( -1,  11),    // friendly pawns 3 from king

            /* castling right available */
            S( 42, -19),

            /* castling complete */
            S(  9,  -8),

            /* king on open file */
            S(-55,  12),

            /* king on half-open file */
            S(-20,  24),

            /* king on open diagonal */
            S(-13,  13),

            /* king attack square open */
            S( -9,  -2),

            /* isolated pawns */
            S( -1, -25),

            /* doubled pawns */
            S(-18, -35),

            /* backward pawns */
            S( 11, -25),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 10,  21),   S(  3,   1),   S(  7,   7),   S( 10,  37),   S( 31,  34),   S( -4, -15),   S(-16,  38),   S( -6, -13),
            S( -3,  24),   S( 26,  -5),   S(  4,  42),   S( 20,  39),   S( 41,   7),   S( -1,  35),   S( 18,  -3),   S(  4,   4),
            S(-18,  31),   S( 17,  17),   S( -3,  58),   S( 21,  70),   S( 28,  39),   S( 23,  37),   S( 30,   5),   S(  5,  31),
            S(  7,  49),   S( 12,  63),   S( 29,  82),   S(  9,  97),   S( 67,  60),   S( 59,  50),   S( 17,  64),   S( 18,  42),
            S( 60,  80),   S( 93, 110),   S( 86, 125),   S(123, 139),   S(124, 111),   S(129, 122),   S(147, 103),   S( 72,  45),
            S( 66, 181),   S(109, 262),   S( 97, 213),   S( 92, 194),   S( 62, 142),   S( 42, 119),   S( 38, 138),   S( 16,  85),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -1,  12),   S(-17,  31),   S(-30,  30),   S(-38,  57),   S(  5,   5),   S(-27,  21),   S(-13,  53),   S( 22,  13),
            S( 10,  26),   S( -5,  41),   S(-21,  39),   S(-12,  26),   S(-15,  34),   S(-55,  50),   S(-55,  64),   S( 19,  22),
            S(  3,  30),   S( -1,  35),   S(-12,  34),   S( 20,  25),   S( -5,  30),   S(-45,  52),   S(-58,  76),   S(-26,  49),
            S( 28,  55),   S( 53,  67),   S( 25,  55),   S( 17,  52),   S( 15,  62),   S( 27,  70),   S( -9,  87),   S(-55, 109),
            S( 49, 103),   S(104, 125),   S( 92,  96),   S( 66,  90),   S(-14,  85),   S( 79,  92),   S( 23, 133),   S(-41, 116),
            S(211, 129),   S(188, 175),   S(214, 179),   S(225, 190),   S(219, 204),   S(200, 199),   S(212, 198),   S(227, 167),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 26,  21),   S(  0,  17),   S(  9,  29),   S(-12,  63),   S( 60,  35),   S( 20,   7),   S( -3,   1),   S( 41,  14),
            S(  3,  15),   S(  4,  10),   S( 17,  16),   S( 16,  26),   S( 13,  15),   S( -3,  11),   S(  2,  10),   S( 29,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -3, -15),   S( -4, -10),   S(-17, -16),   S(-16, -26),   S(-13, -15),   S(  3, -11),   S( -2, -10),   S(-29,   4),
            S(-26, -21),   S(  0, -17),   S( -9, -29),   S( 12, -63),   S(-60, -35),   S(-20,  -7),   S(  3,  -1),   S(-41, -14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 30,   4),   S( 34,  16),   S( 53,  16),   S( 51,  14),   S( 39,  22),   S( 40,  11),   S( 20,   8),   S( 45,  -7),
            S(  4,   7),   S( 22,  20),   S( 21,  19),   S( 26,  37),   S( 28,  17),   S( 15,  16),   S( 29,   6),   S( 19,  -2),
            S(  1,  11),   S( 16,  39),   S( 49,  41),   S( 41,  44),   S( 49,  36),   S( 54,  13),   S( 24,  27),   S( 25,   5),
            S( 49,  60),   S( 91,  45),   S(107,  85),   S(130,  85),   S(132,  76),   S( 83,  69),   S( 71,  35),   S( 66,  16),
            S( 74,  68),   S(130,  86),   S(147, 162),   S(152, 130),   S(160, 150),   S(108, 122),   S(190, 101),   S(-39, 114),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S(-12,  21),   S( -4,  49),   S( 15,  92),   S( 47, 195),

            /* enemy king outside passed pawn square */
            S( -1, 193),

            /* passed pawn/friendly king distance penalty */
            S( -2, -19),

            /* passed pawn/enemy king distance bonus */
            S(  4,  27),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 59, -13),   S( 49,   1),   S( 49,  16),   S( 48,  38),   S( 49,  12),   S(167, -10),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 30,  -6),   S( 27,  51),   S( 16,  42),   S( 16,  74),   S( 39,  90),   S(134, 116),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-26, -15),   S(-12, -31),   S( -4, -38),   S(-22, -27),   S(  2, -33),   S(195, -85),   S(  0,   0),    // blocked by rooks
            S(  0,   0),   S(  6, -15),   S( 22, -27),   S( -5,   9),   S(  3, -40),   S( -3, -138),  S(-27, -225),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(  3,  24),   S( 20,   7),   S( 44, -22),   S(-25, -17),   S(206,  16),   S(211, -10),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S(  1,  48),

            /* knight on outpost */
            S(  2,  32),

            /* bishop on outpost */
            S( 12,  34),

            /* bishop pair */
            S( 40,  99),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -1,  -3),   S( -6,  -7),   S( -6, -11),   S( -7, -23),   S(  0, -30),   S(-18,  -3),   S(-22,  -7),   S( -3,  -2),
            S( -5, -10),   S( -7, -11),   S(-11, -17),   S( -6, -20),   S(-13, -24),   S(-15, -13),   S(-12, -10),   S( -3,  -9),
            S( -3, -11),   S(  4, -35),   S( -4, -39),   S( -7, -55),   S(-19, -33),   S(-15, -27),   S(-16, -18),   S( -6,  -6),
            S( 14, -35),   S( 15, -41),   S( -1, -32),   S( -8, -41),   S(-15, -30),   S( -9, -26),   S(  0, -34),   S(  0, -25),
            S( 25, -28),   S( 15, -48),   S( 24, -50),   S( 10, -47),   S( 13, -40),   S(  9, -56),   S(  3, -63),   S( 10, -28),
            S( 32, -24),   S( 82, -82),   S( 73, -86),   S( 62, -96),   S( 54, -90),   S(103, -84),   S( 68, -109),  S( 59, -70),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 40,   1),

            /* rook on half-open file */
            S( 12,  36),

            /* rook on seventh rank */
            S(  3,  40),

            /* doubled rooks on file */
            S( 24,  24),

            /* queen on open file */
            S(-14,  42),

            /* queen on half-open file */
            S(  5,  35),

            /* pawn push threats */
            S(  0,   0),   S( 26,  33),   S( 29, -10),   S( 34,  23),   S( 30,  -9),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 67,  97),   S( 57, 105),   S( 66,  87),   S( 52,  46),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-11,   0),   S( 51,  37),   S( 88,  14),   S( 40,  33),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 28,  73),   S(  1,  23),   S( 63,  60),   S( 43,  92),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-10,  47),   S( 60,  55),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-17,  19),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats

            /* tempo bonus for side to move */
            S( 16,  10),
        };
    }
}
