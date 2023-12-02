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

        // Solution sample size: 12000000, generated on Sat, 02 Dec 2023 16:29:32 GMT
        // Solution K: 0.003850, error: 0.084357, accuracy: 0.4996
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S( 96, 156),   S(461, 435),   S(472, 494),   S(581, 845),   S(1359, 1577), S(  0,   0),

            /* friendly king piece square values */
            #region friendly king piece square values

            /* pawns: bucket 0 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 91, -102),  S(137, -81),   S( 37, -38),   S( 22,   1),   S(-14,  -1),   S(-18, -14),   S(-34,  -3),   S(-36, -29),
            S( 93, -88),   S( 92, -77),   S( 13, -56),   S(-14, -61),   S(-16, -35),   S(-22, -32),   S(-40, -22),   S(-40, -37),
            S( 93, -78),   S( 82, -52),   S( 27, -44),   S( 14, -58),   S(  5, -60),   S(  9, -45),   S( -9, -32),   S(-28, -30),
            S( 52, -22),   S( 43, -10),   S( 34, -46),   S( 27, -50),   S( 22, -37),   S(-11, -39),   S( -6, -38),   S(-38,  -2),
            S( 82,  39),   S( -2,  41),   S( 46,  -5),   S( 96, -51),   S(  8,  10),   S( 14,  -6),   S(-11,   2),   S(-49,  44),
            S( 49,  49),   S( 81,  27),   S( 58, -27),   S(-13,   3),   S(102, -30),   S( -1,  43),   S( 59, -15),   S(-29,  43),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 24, -27),   S( 25, -31),   S( 42, -29),   S( 20,  -1),   S( -8, -18),   S(  7, -22),   S(-20,  -3),   S(-43,  17),
            S( 18, -21),   S( 17, -28),   S(  5, -33),   S(-14, -38),   S( -4, -37),   S( -1, -30),   S(-27,  -9),   S(-64,   5),
            S( 17, -14),   S( 13, -14),   S( 19, -41),   S( 19, -50),   S(  6, -33),   S( 12, -38),   S( -3, -17),   S(-35,  -2),
            S( 29,   4),   S( 18, -11),   S( 30, -16),   S( 40, -29),   S( 12,  -4),   S( 13, -27),   S(-18,  -7),   S(-45,  26),
            S(-20,  62),   S(-33,  33),   S(-52,  64),   S( -3,  22),   S( 38,  23),   S( 33,  18),   S(-12,  26),   S(-51,  84),
            S( -6,  77),   S( 24,  44),   S(-88,  33),   S( 27,  40),   S(-93,  62),   S(-46,  25),   S( -1,  26),   S(-58, 102),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-30,  10),   S(-25,   9),   S( -3,  -7),   S( -7,  13),   S(-16,   6),   S( 30, -23),   S( 18, -36),   S( -3, -25),
            S(-31,   6),   S(-41,   9),   S(-32, -14),   S(-24, -28),   S(-10, -25),   S(  1, -19),   S( -6, -22),   S(-32, -14),
            S(-31,  11),   S(-34,   7),   S(  0, -39),   S(  1, -42),   S(  0, -19),   S( 32, -28),   S(  6, -19),   S(-17, -11),
            S(-64,  51),   S(-29,   3),   S(-14,  -6),   S(  2, -10),   S( 15, -14),   S( -3,   4),   S(  0,   7),   S(-30,  22),
            S(-35,  55),   S(-81,  46),   S(-77,  40),   S(-50,  39),   S(-16,  74),   S(-29,  54),   S( -2,  32),   S(-46, 107),
            S(-46,  84),   S(-89, 101),   S(-160,  89),  S(-89,  67),   S(-74,  86),   S(-33,  41),   S(-29,  60),   S(-60,  96),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-25,  -7),   S(-36,   3),   S(-28,   0),   S(  8, -32),   S(-10, -15),   S( 50, -24),   S( 91, -54),   S( 57, -77),
            S(-31, -13),   S(-45,  -4),   S(-29, -25),   S(-24, -24),   S(-11, -36),   S(  9, -34),   S( 61, -46),   S( 43, -55),
            S(-25, -11),   S(-18, -15),   S( -6, -36),   S( -7, -47),   S(  9, -55),   S( 37, -50),   S( 34, -29),   S( 43, -48),
            S(-34,  19),   S(-22, -18),   S(  3, -21),   S(  7, -27),   S( 34, -50),   S( 16, -32),   S( 19,  -7),   S( 34,  -6),
            S( -9,  42),   S(-14,  -1),   S( 13, -14),   S( 11,  13),   S( 60,  14),   S( 77,   1),   S( 32,  48),   S( 28,  94),
            S(-40, 113),   S(-27,  79),   S(-16,  14),   S(-34,  22),   S(  2,  31),   S( 57,  30),   S( 48,  51),   S(  8, 106),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-90,  27),   S(-18,   5),   S(-97,  41),   S(-24, -28),   S(  1, -47),   S(-25,   9),   S(-26, -12),   S(-83,  -2),
            S(-79,  34),   S(  2,   8),   S( 60, -34),   S( 43, -39),   S( 16, -30),   S(-59, -10),   S(-12, -21),   S(-54, -19),
            S(-15,  -5),   S( 50, -21),   S( 56, -30),   S(  5, -35),   S(-59,  -9),   S(  7, -38),   S(-64,   0),   S(-47,   2),
            S( -3,   0),   S( 15,  13),   S( 83, -21),   S( 21,   8),   S(-16, -18),   S(-23, -11),   S(  6, -11),   S(-14,   0),
            S(-24,  49),   S(  8,  20),   S( 67, -11),   S(-26,  24),   S( 17,  17),   S(-49,  36),   S(-58, -10),   S( 96,   6),
            S(143,  35),   S( 54,  70),   S( 53,   8),   S( 23,   2),   S( 33,   2),   S(  7, -10),   S( 34,  41),   S(-49,  36),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-98,  56),   S(-47,  33),   S(-20,   9),   S(  6,  -6),   S(-26,  19),   S( -8,   6),   S(-26,   6),   S(-33,  23),
            S(-59,  37),   S(-43,  25),   S( 38,  -4),   S( 30,  -6),   S(  0,  -8),   S(-24, -13),   S(-18,  -4),   S(-58,  25),
            S(-47,  39),   S(-18,  13),   S( 86, -40),   S( 19, -24),   S( 42, -20),   S(-29,  -4),   S(  0,  -2),   S(-31,  19),
            S(-36,  49),   S( 14,  16),   S( 49,  -4),   S( 60,   4),   S(  2,   0),   S(-34,  13),   S( 27,  -3),   S(-13,  30),
            S( 91,  14),   S( 95,   1),   S( 30,  34),   S( 32,  30),   S(-34,  61),   S( 48,  13),   S( -1,  -4),   S( 43,  48),
            S( 92,  16),   S(114,  -2),   S( 95,  -6),   S( 11,  22),   S( 50,  16),   S( 68,  -7),   S( 35,  38),   S( 95,  30),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-38,  28),   S(-25,  16),   S( 15,  -2),   S(-11,   9),   S( 12,  -3),   S(-49,  20),   S(-47,   9),   S(-80,  29),
            S(-39,  21),   S( -5,  -4),   S( 19, -28),   S( 28, -19),   S( 59, -26),   S(  9,  -7),   S(-28,   1),   S(-68,  21),
            S(-43,  27),   S(  7,   0),   S(  1, -14),   S( 10, -23),   S( 55, -22),   S( 58, -25),   S(  0,  -4),   S(-37,  16),
            S(-22,  43),   S(-19,  17),   S( 12,  -3),   S( 13,   0),   S( 50,  -4),   S( 28,   8),   S( 29,   3),   S( 26,   2),
            S(-19,  34),   S(-70,  27),   S(-37,  27),   S( 35,   4),   S( 44,  44),   S(116,  17),   S( 77, -15),   S( 91,  11),
            S( 97,   7),   S( 16,  -2),   S( 49, -11),   S( 77, -19),   S( 88,  15),   S( 46,  13),   S( 60,  28),   S(122,   1),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-71,  -8),   S(-52,   0),   S( -7,  -4),   S(-73, -11),   S( 15, -21),   S(  7,  -6),   S( -9, -14),   S(-63,   2),
            S(-80,  -8),   S(-47, -16),   S(-16, -28),   S( 11, -57),   S( 31, -49),   S( 41, -32),   S( 47, -19),   S(-50,   1),
            S(-79,   3),   S(-50,  -5),   S(-18, -19),   S(  5, -32),   S( 12, -35),   S( 38, -24),   S( 48, -32),   S( 18, -28),
            S(-45,  13),   S(-79,  10),   S(-37,  -9),   S(-33,   0),   S( 20, -19),   S( 35, -15),   S( 57,  -8),   S( 56, -27),
            S(-16,  11),   S(-54,  -3),   S(-18,  -4),   S(-10,  -1),   S( 42,  19),   S( 65,   4),   S(156, -23),   S(149,   1),
            S( 41, -12),   S(-57, -18),   S( 69, -41),   S( 37, -37),   S( 37, -43),   S( 75,  -2),   S( 88,  45),   S(121,  24),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-59,  46),   S(-64,  57),   S(-40,  43),   S( -4,  22),   S(-13,   1),   S(-24,  17),   S(-61,  -3),   S(-30, -14),
            S(-87,  40),   S(-87,  37),   S(-37,  32),   S(-47, -13),   S(-15, -14),   S(-30,  -7),   S(-65, -21),   S(-24,  -9),
            S(-79,  42),   S(-65,  51),   S( -2,  37),   S(-32,  18),   S(-25,   2),   S(-68,   0),   S(-61, -23),   S(-15, -23),
            S(-17,  37),   S(-29,  70),   S(132,   3),   S( 25,  29),   S(-28,  18),   S(-59,   8),   S(-28,  12),   S( 67, -26),
            S( 99,  29),   S( 71,  50),   S( 64,  70),   S( 97,  63),   S( 22,  28),   S( -5,  38),   S( -2, -32),   S(-23,   6),
            S( 95,  64),   S(120,  53),   S(100,  89),   S( 36,  19),   S( -4, -15),   S( -9, -11),   S(-22, -40),   S( 31, -15),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-97,  64),   S(-118,  63),  S(-18,  38),   S(-31,  -9),   S( -8,  -8),   S(-110,  25),  S(-95,  25),   S(-89,  18),
            S(-97,  51),   S(-39,  25),   S(-87,  52),   S( 21,   1),   S(-85,   3),   S(-86,  12),   S(-119,  16),  S(-64,  21),
            S(-87,  49),   S(-66,  46),   S(-100,  52),  S(-109,  28),  S(-141,  57),  S(-124,  18),  S(-72,   3),   S(-74,  16),
            S(-28,  54),   S( 98,  11),   S( 52,  39),   S( 95,  39),   S(-34,  29),   S(-40,  10),   S( 36,   1),   S( 28,  -9),
            S(110,  -4),   S( 96,  21),   S(125,  38),   S(123,  64),   S( 45,  62),   S( 67,  11),   S( 11, -40),   S( 56,  -6),
            S( 56, -19),   S(135, -30),   S(165, -11),   S(130,  28),   S( 35,  32),   S(  2, -37),   S(  7,  -3),   S( 54, -23),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-144,  37),  S(-131,  17),  S(-39, -20),   S(-10,  38),   S(-30,   1),   S(-169,  65),  S(-241,  73),  S(-122,  51),
            S(-100,  10),  S(-59,  -2),   S(-67,  -6),   S(-96,  13),   S(-121,  33),  S(-48,  16),   S(-195,  52),  S(-117,  45),
            S(-80,  13),   S(-76,  12),   S(-49,  20),   S(-116,  55),  S(-90,  49),   S(-83,  35),   S(-163,  42),  S(-86,  41),
            S( 11,   8),   S( -2,  19),   S(-19,  13),   S(-32,  45),   S( 37,  33),   S(-52,  43),   S(  9,   9),   S( 17,   2),
            S( 73, -36),   S( 41, -29),   S( 51,  10),   S(105,  39),   S(161,  45),   S(106,  29),   S( 79,  -1),   S(131, -14),
            S( 54, -63),   S( 28, -74),   S(  8, -32),   S( 82,  26),   S( 77,  18),   S( 53, -12),   S( 68, -15),   S( 28,  -9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-158,  19),  S(-69,   5),   S( 43, -27),   S(-60, -47),   S( 31,  35),   S(-126,  54),  S(-91,  41),   S(-158,  69),
            S(-106, -11),  S(-50, -22),   S(-88, -13),   S(-54,   9),   S(-53,   7),   S(-66,  22),   S(-157,  66),  S(-170,  67),
            S(-82,  -9),   S(-32, -19),   S(-75,  17),   S(-19, -15),   S(-23,   4),   S(-10,  14),   S(-60,  31),   S(-68,  28),
            S(  7,   0),   S(-78,  21),   S(-113,  32),  S(-93,  51),   S(-45,  28),   S( 51,   2),   S( 21,  36),   S( 54,  -9),
            S( 86,  -4),   S(-49,   1),   S(-21,  25),   S( 20,  22),   S( 95,  44),   S( 29,  36),   S(179,  -7),   S(149,  -3),
            S( 46, -34),   S( 13, -72),   S( 22, -23),   S( 14, -49),   S( 59, -40),   S( 56,  45),   S(100,  37),   S(132,  49),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-46,  18),   S(-51,  36),   S(-19,  18),   S(  3,   7),   S(-11, -30),   S(-57,   6),   S( -6, -25),   S(-92,  26),
            S(-58,  64),   S( 10,  10),   S( -8,  25),   S( 14, -27),   S(-17,  48),   S(-48,  16),   S(-61, -20),   S( -4, -42),
            S(-91,  63),   S( -5,  40),   S( 32,  58),   S( 41, -23),   S(-10, -19),   S(-77,  -8),   S(-58, -32),   S(-45, -45),
            S(-22,  44),   S( 38,  59),   S( 68,  49),   S( 34,  23),   S(-21, -20),   S( -7, -38),   S( -9,  15),   S(-27, -15),
            S( 65, -16),   S( 30,  94),   S(109,  61),   S( 57,  59),   S( 26,  23),   S(  6,  -8),   S( 16,  36),   S(-18, -33),
            S(  3, -16),   S( -9, 142),   S(128, 129),   S( 87,  65),   S( -6, -40),   S(  5, -21),   S(-17, -45),   S( 21, -84),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-33,  15),   S(-11,  22),   S( -1,   2),   S( -4,  11),   S(-30, -66),   S( -8,  -2),   S(-58, -16),   S(-86,  19),
            S( -5,  -1),   S(-34,  20),   S(-16,  21),   S( 27,  49),   S(-44,   4),   S( -5,  -8),   S(-82, -14),   S(-21,  -7),
            S(-20,  20),   S(  1,  14),   S(-54,  37),   S(  6,  57),   S(-40,  45),   S(-25, -18),   S(-82,  -1),   S(-15, -26),
            S(  7,  21),   S( 38,  16),   S( 18,  46),   S( 14,  67),   S( 17,   6),   S(-15,  -3),   S(-34, -17),   S( 69, -61),
            S( 19,  -6),   S(111,  35),   S( 91,  77),   S(129, 106),   S(100,  46),   S(  4,  26),   S( 33, -67),   S( 19, -59),
            S( 11,   5),   S( 99,  33),   S( 86, 129),   S(109, 138),   S( 29,  31),   S(  4,   9),   S(-11, -33),   S( 54, -44),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-53, -16),   S(-21,  -9),   S(-27, -47),   S(-16, -26),   S(-12, -10),   S(-65,  27),   S(-41,  24),   S( 22,  36),
            S(-35,   0),   S(-56,   1),   S(-41, -25),   S(-34,   3),   S(-25,  17),   S(-16,  24),   S(-38,  37),   S( -6,  14),
            S(-39,  -3),   S( -3,  -1),   S(-33,  14),   S(-39,  39),   S(-66,  36),   S(-26,  28),   S(-23,  11),   S( -5,  24),
            S( 43,  -3),   S( 22,  -5),   S( 22, -26),   S( 27,  40),   S(-46,  88),   S(-13,  44),   S(-25,  18),   S( 26,  19),
            S( 68, -27),   S( 34, -43),   S( 72,   5),   S( 63,  51),   S(103,  89),   S(106,  68),   S( 38, -10),   S( 69,  -7),
            S( 40, -33),   S( 60,  23),   S( 46,  18),   S( 82,  48),   S( 65, 128),   S( 29,  75),   S( 25,  28),   S( -5, -12),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-46, -24),   S(-38, -26),   S(-15, -35),   S(  1,  25),   S(  8,  47),   S(  2,  29),   S(-45,   4),   S(-10,  52),
            S(-70, -26),   S(-34, -13),   S(-86, -17),   S(  5,  -1),   S(-31,  25),   S( 14,  40),   S( -6,  69),   S(-16,  23),
            S(  1, -31),   S( 11, -26),   S(  0,  -3),   S(-27,  14),   S( 44,  26),   S( 32,  24),   S( 18,  76),   S(-38,  57),
            S( 24,   7),   S(-31,  -7),   S(-23, -19),   S( -7,   0),   S(-33,  21),   S( 62,  40),   S(-20,  92),   S(-16,  16),
            S(  1, -30),   S( -8, -41),   S(  9, -28),   S(  0, -25),   S( 47,  41),   S(121,  48),   S(-10, 122),   S( 20, -11),
            S( 40, -32),   S( 19,  10),   S( 22,  21),   S(  3,  -9),   S( 36,  95),   S( 56, 130),   S(-60, 121),   S( 12, -53),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: bucket 0 */
            S(-29, -33),   S( 47, -23),   S(-37,   8),   S(-69,  34),   S(-12,  -2),   S(-18, -51),   S(-28, -97),   S(-73,   8),
            S(-22,  78),   S(  3, -40),   S(-22,  10),   S( -2,  -4),   S( -5, -17),   S( -5,  -2),   S(-28, -41),   S(-73, -84),
            S( 34,  50),   S(  0,  -9),   S( 25, -26),   S(  2,  12),   S( 46, -20),   S(-12,  -8),   S( 22, -45),   S(-38, -55),
            S( 19,  -2),   S( 47,   5),   S( 49,  -6),   S( 43,   5),   S( 40, -24),   S( 16,   8),   S(-16, -15),   S(-13,  16),
            S(  3, -42),   S( 38,  -2),   S( 34, -36),   S( 65, -27),   S( 43, -25),   S( 35,  -4),   S( 44, -19),   S(-52, -31),
            S(  5, -15),   S( 14,  -6),   S( 50, -18),   S( 67, -74),   S( 77, -90),   S( 72, -54),   S(  4, -61),   S( 37,  25),
            S(-50,  -1),   S( -4, -34),   S( 27, -35),   S( 11, -32),   S( 36, -10),   S(-45,  -7),   S( -3, -73),   S(-13, -16),
            S(-71, -78),   S( -1, -25),   S(-17, -50),   S( 12, -32),   S(-34, -22),   S(-15, -55),   S(-24,  -1),   S( 13,  -4),

            /* knights: bucket 1 */
            S(-61,  -3),   S(-59,  91),   S( 11,  37),   S(-86,  77),   S(-27,  50),   S(-28,  55),   S(-54,  67),   S(-25,  43),
            S( -8, -29),   S( 21,  26),   S( -3,  28),   S(-17,  41),   S(-13,  23),   S(-18,  45),   S(-12,   1),   S(-19, -31),
            S(-25,  31),   S( -4,   5),   S( 15,   9),   S( 19,  29),   S( 25,  23),   S(-22,  38),   S( -8,  24),   S(-15,   7),
            S(  0,  38),   S( 15,  45),   S( 18,  48),   S( 20,  35),   S( 21,  33),   S(  2,  38),   S( 34,  11),   S( -7,  31),
            S(-13,  15),   S( 31,   9),   S( 25,  36),   S( 44,  26),   S( 10,  39),   S( 12,  42),   S( 27,   3),   S( 13,   9),
            S( 28,  -6),   S(  3,  50),   S( -7,  33),   S(  6,  15),   S(-32,  33),   S( 43,   0),   S( 49,  -5),   S( 37, -19),
            S( 45, -20),   S( 20,  32),   S(-48,  26),   S( 38,  21),   S(-13,   7),   S( 23,   2),   S(-59,   9),   S( 25, -33),
            S(-90, -21),   S(-66,  -6),   S(  7,  23),   S( 21,  -6),   S(-26,  -9),   S(-16,  19),   S(-14, -41),   S(-48, -18),

            /* knights: bucket 2 */
            S(-78,  30),   S(-17,  43),   S(-52,  67),   S(-56,  80),   S(-49,  82),   S(-53,  93),   S(-45,  63),   S(-50,  22),
            S(-20,  44),   S(-24,  35),   S( -1,  33),   S(-25,  58),   S(-23,  44),   S( -1,  64),   S(-67,  85),   S(-71,  43),
            S(-21,  44),   S(-12,  49),   S(-25,  72),   S( -4,  55),   S(  7,  49),   S(-10,  44),   S(-10,  50),   S(-14,  57),
            S(-23,  60),   S(-27,  76),   S(-13,  59),   S( -2,  65),   S(-13,  93),   S(-21,  70),   S(-25,  86),   S(-14,  63),
            S(-16,  55),   S(-15,  54),   S(-36,  73),   S(-30,  82),   S(-30,  76),   S(-26,  79),   S(-10,  51),   S(-40,  58),
            S(  1,  31),   S(-42,  75),   S(-46,  73),   S(-33,  62),   S(-75,  76),   S(-19,  59),   S(-81,  56),   S(-18,  33),
            S(-57,  49),   S(-22,  39),   S(-40,  66),   S(-21,  57),   S(  2,  53),   S(  3,  44),   S(-70,  41),   S(-83,  26),
            S(-176,  54),  S(-36,  31),   S(-120,  72),  S(-36,  42),   S(-13,  12),   S(-79,  44),   S(-12, -10),   S(-219, -19),

            /* knights: bucket 3 */
            S(-59,  -6),   S( -6, -10),   S(-34,   3),   S(-34,  35),   S(-14,  38),   S(-21,  55),   S(  3,  23),   S(-20,  25),
            S(-29,  -4),   S(-42,  26),   S( -9,  19),   S(  0,  37),   S( -6,  33),   S( -9,  47),   S( -9,  27),   S(-38,  79),
            S(-14,  22),   S( -6,  36),   S(-13,  51),   S(  6,  44),   S( 17,  57),   S(  7,  43),   S(  1,  41),   S(  0,  66),
            S(-15,  38),   S( -5,  48),   S(  4,  70),   S( 12,  67),   S( 23,  75),   S( 12,  79),   S(  8,  73),   S(  6,  45),
            S( -8,  42),   S(  7,  48),   S(  5,  51),   S(  6,  75),   S( -7,  81),   S( -2,  85),   S( 21,  76),   S(  1,  33),
            S( 17,  23),   S(  1,  37),   S( 27,  33),   S( 32,  39),   S( 32,  12),   S( 55,  21),   S(-20,  55),   S(-20,  60),
            S(-20,  37),   S( 13,  41),   S(  8,  23),   S( 26,  33),   S( 44,   7),   S( 56,  -5),   S(  8, -24),   S( 12, -25),
            S(-129,  56),  S(-18,  46),   S(-37,  35),   S( 13,  31),   S(-11,  32),   S(-29,  19),   S( 29, -48),   S(-110, -22),

            /* knights: bucket 4 */
            S(  2, -31),   S(-33,   6),   S(-13,   1),   S( 43,   8),   S( 24,   5),   S( 30, -26),   S(-14, -102),  S(-44, -66),
            S( 27, -32),   S(-43,  50),   S( 12,  19),   S( 30, -14),   S( 34, -27),   S(-35, -57),   S( 19, -63),   S(-30, -57),
            S(-11,  44),   S(-31,  41),   S( 95, -44),   S( 89, -35),   S( -5, -13),   S( -8, -10),   S(-65, -33),   S(-39, -48),
            S( -6,  45),   S(  5, -10),   S( 55, -21),   S( 19,  20),   S( 16, -12),   S( 61, -33),   S( 11, -29),   S( 10, -28),
            S(125,   3),   S( 39,  16),   S( 94,  -6),   S( 50,   2),   S( 85,   0),   S( 19,  -1),   S( 38, -43),   S( 55, -28),
            S(-43, -39),   S(  6, -17),   S( 41,  -6),   S( 51, -29),   S( -3, -28),   S( 82, -22),   S( 39,  16),   S(-11, -43),
            S(  0,  -7),   S(-36, -15),   S( 19, -24),   S( 27,   6),   S(  0, -10),   S(  7, -11),   S(  1, -22),   S(-19,  -9),
            S(-16,  -8),   S( -1, -10),   S(-15, -44),   S(-21, -53),   S(  2,  15),   S( -8, -43),   S(  2,   4),   S(  9,  -2),

            /* knights: bucket 5 */
            S( -5, -21),   S(-59,  36),   S( 33,  36),   S( -3,  39),   S(-59,  58),   S(-23,  15),   S(-22,   6),   S(-31, -71),
            S(  6, -17),   S( 54,  21),   S( 18,  28),   S( 28,  24),   S( 82,  13),   S( 44,  16),   S( 19,  40),   S( -5, -29),
            S( 55,  21),   S( 16,  37),   S( 75,  24),   S(132,  -2),   S( 50,  25),   S( 12,  43),   S( 49,  21),   S(  6,   7),
            S( 82,  42),   S( 73,  22),   S( 84,   6),   S( 65,  23),   S( 76,  27),   S( 80,  18),   S( 62,  49),   S(-51,  53),
            S( -1,  50),   S( 61,  23),   S( 50,  31),   S(103,  24),   S(157,  10),   S( 99,  22),   S(  9,  47),   S( 20,  27),
            S(-22,  31),   S( 19,  60),   S( 69,  19),   S( 65,  30),   S( 26,  33),   S(-35,  64),   S( 76,  21),   S(-13,  11),
            S( 25,  55),   S(  2,  50),   S( 66,  36),   S( 43,  56),   S( -5,  73),   S( 29,  45),   S( 34,  63),   S(  2,  -8),
            S(-49, -32),   S(-11,  25),   S( 34,  30),   S( 19,  53),   S(  3,  48),   S(-11,  44),   S(-16,  12),   S(-22, -13),

            /* knights: bucket 6 */
            S( -9, -44),   S(-61,  22),   S( 47,  39),   S( 50,  19),   S(-12,  53),   S( 64,  34),   S(-54,  33),   S(  4,  39),
            S(  4, -22),   S( 82,  25),   S( 51,  20),   S(  7,  33),   S( -1,  44),   S( 87,  32),   S( 71,  45),   S( 21,  26),
            S(-22,   5),   S( 20,  19),   S( 52,  23),   S( 67,  14),   S( 33,  40),   S(  9,  43),   S( 43,  52),   S(-13,  69),
            S( 81,  25),   S( 64,  25),   S(110,   5),   S( 97,  21),   S( 85,  26),   S( 85,  28),   S( 67,  55),   S( 29,  57),
            S(-16,  31),   S( 94,  16),   S(103,  17),   S(111,  23),   S(138,  27),   S(189,  -3),   S( 54,  42),   S(  6,  55),
            S(  8,  12),   S(104,   2),   S( 91,  17),   S( 65,  32),   S(136,  20),   S( 38,  41),   S(-11,  63),   S( 52,  29),
            S(-39,  28),   S( -4,  52),   S(-11,  67),   S( 13,  52),   S( 51,  63),   S( 18,  47),   S( 52,  74),   S(-22,   7),
            S(-62,  -2),   S(  5,  29),   S( 21,  47),   S(-21,  38),   S(  2,  44),   S( 21,  70),   S(  8,  32),   S(-25, -34),

            /* knights: bucket 7 */
            S(-43, -41),   S(-190,  -3),  S(-56, -29),   S(-51, -14),   S(-17,   4),   S( 16,  24),   S(-38,   2),   S( 16, -36),
            S( 11, -79),   S(-47, -17),   S(-62,   5),   S(-11,   6),   S(-53,  27),   S( 15,  33),   S( -2,  37),   S(  6,  12),
            S(-51, -54),   S(-14, -34),   S( 14,  -6),   S( 76, -26),   S( 58,  -8),   S( 58,  -8),   S( 14,  48),   S( 18,  57),
            S(-73,  -8),   S( 59, -20),   S( 27,   3),   S( 76,  -1),   S(120,  -1),   S( 73,  -4),   S( 86,  -3),   S(  5,  56),
            S(-39, -16),   S( 26, -28),   S( 67, -18),   S(123, -16),   S(131, -10),   S( 66,  10),   S(165, -17),   S( 43,  23),
            S(-39,   0),   S(  7, -19),   S( 46, -14),   S( 64,  -7),   S(119,   3),   S( 81,  -2),   S( 57,  17),   S(-22,  11),
            S(-43,   5),   S(-75,  15),   S( 57, -13),   S(109,   6),   S( 90,   7),   S( 51,   7),   S( 11, -39),   S( 41,   7),
            S(-76, -41),   S(-23, -20),   S(-18, -40),   S( 26,   6),   S(  9,  -7),   S( 56,  18),   S(  6, -12),   S( 28,  15),

            /* knights: bucket 8 */
            S(-13, -33),   S(-22, -38),   S( -1,  -7),   S( -7,   2),   S(-26, -42),   S(-17, -37),   S(  6,  11),   S(-12, -37),
            S(  9,   9),   S( -6, -32),   S( -9, -41),   S(-15, -42),   S( -7, -33),   S(-30, -90),   S(-27, -95),   S( -6, -39),
            S(  6,  51),   S(-24,  -4),   S( 45,  25),   S( 34, -30),   S( 11, -44),   S(  9,   1),   S(-15, -16),   S(  2, -18),
            S(-13,  48),   S( -3,   1),   S( -5,  -6),   S( 36,  -4),   S( 32, -18),   S( 12, -20),   S(-13, -68),   S(-13, -45),
            S(  6,  70),   S(-21,   4),   S( 29, -17),   S( -1,   7),   S( 22,   4),   S( 38,  12),   S(  6, -41),   S(  5,   8),
            S(-10,  37),   S( -6, -14),   S( 13,  -4),   S( 44, -28),   S( 20, -14),   S( 21,   0),   S(  3,  -3),   S(-11, -39),
            S(-14,  -2),   S(-15,  33),   S(-11, -24),   S( -1,   5),   S(-20, -44),   S( 11,   0),   S(  9,  35),   S( -6, -15),
            S( 13,  31),   S( -8,  -5),   S( 19,  -8),   S( -9,  -8),   S(  1,   4),   S( -7, -25),   S( 11,  48),   S( -1,  -5),

            /* knights: bucket 9 */
            S(-17, -66),   S(-28, -11),   S( -6, -42),   S(-12, -28),   S(-29, -65),   S(-20, -43),   S(-14, -18),   S(-13, -69),
            S(-22, -63),   S(-19, -47),   S(-32, -70),   S(-48, -31),   S( -9, -37),   S( 20, -46),   S( -6, -49),   S(-20, -84),
            S(-18, -40),   S( -8, -44),   S(-20, -24),   S( -7, -33),   S(-12, -41),   S(-28, -31),   S(-22,  -9),   S(-32, -68),
            S(-27, -44),   S(  2,  -5),   S( 24, -27),   S( 25, -40),   S( 43, -20),   S( 30, -27),   S(  4,  -5),   S( -3, -12),
            S(  0, -24),   S( 12,  -9),   S( 63, -46),   S( 39, -24),   S( 12, -25),   S(  8, -44),   S(  5, -37),   S(-19, -52),
            S(  3,   5),   S(  5,   8),   S( 36,   4),   S( 28, -45),   S( 39, -20),   S(  9, -24),   S( 16,  12),   S( -2,  -1),
            S( -4,   7),   S(-21,   1),   S( 31,  28),   S( -3,  17),   S( 20,  42),   S(  1,  -1),   S(  5,   2),   S(  5,  26),
            S(  4,   5),   S( 24,  10),   S( 13,  36),   S(  4,   2),   S( 12, -12),   S( -4,  15),   S(  7,  36),   S( -3, -15),

            /* knights: bucket 10 */
            S(-29, -108),  S(-35, -93),   S(-15, -86),   S(-29, -21),   S(-47, -61),   S(-24, -44),   S(  0,  19),   S( -1, -21),
            S(-13, -60),   S( -7, -71),   S(-21, -59),   S(-19, -38),   S(  6, -23),   S(-11, -69),   S(-14, -45),   S(-10,   0),
            S(-42, -78),   S(-16, -103),  S( -9, -61),   S( 18, -54),   S( -3, -74),   S(-49, -48),   S( -6, -44),   S(  2, -53),
            S(-27, -68),   S(-27, -65),   S( -1, -67),   S( 21, -48),   S(103, -68),   S( 42, -62),   S( -2, -43),   S(-11, -10),
            S(-38, -66),   S(-46, -50),   S( 18, -50),   S(110, -64),   S( 55, -51),   S( 24, -67),   S( 26, -37),   S( -3,  11),
            S(-18, -56),   S(-23, -42),   S(  1, -46),   S( 32, -36),   S( 60, -34),   S( 10, -29),   S(  2,  20),   S( -5,   5),
            S( -7, -29),   S(-17, -63),   S( 10, -21),   S(  1,  -4),   S(  3,  14),   S(-17, -12),   S(  4, -40),   S(-11, -19),
            S( -2, -21),   S(-13, -52),   S( -4, -34),   S( -2, -13),   S( 19,   2),   S( 23,  26),   S(  0,  -2),   S( -8,  -5),

            /* knights: bucket 11 */
            S(-11, -54),   S(-41, -72),   S(-21, -61),   S( -5, -37),   S(-28, -56),   S(-74, -89),   S(-23, -33),   S(-13, -35),
            S(-28, -66),   S(-16,  -9),   S(-12, -103),  S(-37, -23),   S(  4, -15),   S(-41, -45),   S( -5, -29),   S( -7,  -3),
            S(-46, -91),   S(  5, -26),   S(-18, -37),   S(-21, -39),   S( 14,   0),   S(-25,   0),   S(  8,  12),   S(-19,  -6),
            S(-13, -30),   S(-16, -66),   S(-34, -51),   S( 85, -28),   S( 49, -31),   S( 72,   1),   S(-66,   8),   S(  1,  54),
            S(-10, -12),   S(-23, -56),   S( 22, -14),   S( 49, -11),   S( 42, -16),   S( 82, -15),   S( 13,  -5),   S( 11,  86),
            S( -8,  10),   S(  5, -28),   S(  1, -18),   S( 18, -29),   S( 42,  -4),   S( 51,   1),   S(  6,  56),   S( 13,  87),
            S( -4, -17),   S(-25, -18),   S( 17, -19),   S(  6, -17),   S( 16,  33),   S(-13,  42),   S( -1,  33),   S( 20, 138),
            S( -2, -25),   S( -3, -10),   S( 10,  -9),   S(  4,  -4),   S( -3,   9),   S( 23,  62),   S( -4,  16),   S(  6,  59),

            /* knights: bucket 12 */
            S( -4, -20),   S(  9,  33),   S(  2, -11),   S( -6,  -1),   S( -3,  -9),   S( -6, -10),   S( -4,  -2),   S( -6, -42),
            S( -1,   0),   S(  5,  21),   S(  7, -14),   S(-18, -42),   S(  8,  26),   S(-10, -55),   S( -1, -29),   S(  0,  -7),
            S( -6, -14),   S( -4, -17),   S( -7, -42),   S( -3, -25),   S( -6, -53),   S(  4,   6),   S(  6,   8),   S(-13, -50),
            S(-29, -40),   S( -2, -17),   S( -4, -33),   S(  2,  28),   S( -5, -50),   S(  6, -22),   S(  0,   1),   S( -6, -46),
            S(  8,  33),   S(-15, -29),   S(  6, -12),   S(  3, -18),   S(  0, -12),   S(  7, -14),   S(-15, -71),   S(-11, -26),
            S(  5,  89),   S(-36, -42),   S(-30,   7),   S(  3,  -2),   S( -2, -21),   S(-11, -29),   S(  2,   2),   S( -2,  -7),
            S(  8,  77),   S(-39, -30),   S( -7, -28),   S( -2,  15),   S( -3, -13),   S(  4,  -1),   S( -2, -15),   S( -4, -22),
            S(  4,  19),   S(  4,  88),   S( 10,  38),   S(  7,  49),   S( -4, -16),   S(-11, -51),   S( -1,  -2),   S(  1,   4),

            /* knights: bucket 13 */
            S(-19, -19),   S( -1,   5),   S(  0, -14),   S( -5, -27),   S( -9, -27),   S( -6, -31),   S( -4,  -4),   S(  2,   9),
            S( -2,  -4),   S(  4,  26),   S(  1, -18),   S(-13,   7),   S( 13,  23),   S(  0, -47),   S( -6, -12),   S( 11,  31),
            S(  3,  11),   S( -5,  10),   S(-14, -33),   S(  7, -39),   S( -8, -56),   S(-23, -57),   S(  1,  17),   S(  4,   0),
            S(-34, -48),   S(  5,  -4),   S( 14,  -6),   S( -7, -16),   S(-27, -37),   S( 15,   3),   S(  2, -17),   S(-10, -31),
            S( 11,  33),   S(  9,  11),   S( 23, -11),   S(-21, -17),   S( -6,  10),   S(  4, -17),   S(  3,  -2),   S(  4,  17),
            S( 10,  61),   S(  1,  -6),   S(-26,  47),   S(-19,   6),   S( -1,  13),   S( -1,   3),   S( -3,   3),   S(  0,  10),
            S(  4,  -3),   S(  5,  40),   S(-17,  45),   S(  4,  62),   S(  7,  61),   S(  2, -12),   S(  7,  25),   S(-12,  -9),
            S(  2,  17),   S( 15, 186),   S(  0,  46),   S( -1,  35),   S( -1,  54),   S(  7,  34),   S( -1,  -8),   S(  0,  -2),

            /* knights: bucket 14 */
            S(-10, -36),   S(-11, -46),   S( -2,   0),   S(-10, -29),   S(-11, -40),   S( -1, -24),   S(  0, -11),   S( -4, -24),
            S( -1, -20),   S(  2, -14),   S( -3, -54),   S(-14, -78),   S(  4, -24),   S(  0,  -6),   S( -8, -31),   S(  0, -22),
            S(-12, -55),   S(  2,  -8),   S(  0, -26),   S(  0, -20),   S(  3,   1),   S( -5, -14),   S( -4, -14),   S(  5,  30),
            S( -1,  14),   S( -9, -56),   S(-23, -81),   S(  8, -36),   S(  7, -15),   S(  4, -19),   S(  3, -39),   S( -1,  21),
            S( -7, -26),   S(-13, -41),   S(-11, -58),   S(-42, -55),   S(-36, -46),   S( 11, -29),   S( -3,  21),   S( -3,  -8),
            S(  1, -14),   S(  9,   9),   S(  4, -16),   S(  4, -24),   S(  2,  15),   S( -4, -17),   S(  5,  51),   S(  2,  39),
            S( -8, -18),   S( -3,  -6),   S( 11,  16),   S( -8, -54),   S(-11, -11),   S(-20,  27),   S(  1,  62),   S( -4,  -1),
            S(  0,  -4),   S(  4,  12),   S( -1,  17),   S(  7,  66),   S(-12, -44),   S(  0,  27),   S( -3,  83),   S(  0,  -7),

            /* knights: bucket 15 */
            S( -5, -19),   S( -4, -24),   S( -6, -49),   S(  1,   4),   S( -9, -45),   S( -8, -33),   S(  6,   5),   S( -3, -15),
            S(  2,  10),   S( -1, -14),   S( -2, -45),   S(  5,  18),   S( -1,   1),   S( -2,   8),   S( -2,  -6),   S( -7, -35),
            S(-17, -35),   S(  3, -22),   S( -3, -34),   S( -2, -33),   S( -4, -12),   S(-16, -54),   S( -5, -31),   S(-20, -26),
            S( -7, -31),   S( -8, -64),   S( -5, -10),   S(  3,  20),   S(  2,  -8),   S( -1,   9),   S( -5,  14),   S( -9,  -9),
            S(-10, -20),   S(  5,   4),   S( 15,   7),   S(-15, -23),   S(  6, -32),   S( -4, -21),   S(-12, -39),   S( -2, -12),
            S( -1,   4),   S(-14, -48),   S( -8, -32),   S( -8,  -2),   S( -3, -17),   S( -9, -17),   S( -1,  12),   S(  4,  66),
            S(-10, -24),   S( -2,   1),   S( -1, -12),   S( -5, -28),   S(-14,  -7),   S(-10,  11),   S(-15, -32),   S(  9,  70),
            S( -1, -10),   S(  2,  -1),   S(  2,  18),   S(  3,   1),   S(  6,  32),   S(-11, -31),   S( 13,  96),   S( -7, -23),

            /* bishops: bucket 0 */
            S(  3, -11),   S(-66,  42),   S( 55,   8),   S(-14,  15),   S(  9, -24),   S( 15,  -6),   S(-25,   8),   S(-20, -17),
            S( 55, -44),   S( 91,  13),   S( 48,  19),   S( 13,  21),   S( -2,  25),   S(-12,  21),   S(-31,  15),   S(-23,  29),
            S( 55,  13),   S( 59,  -1),   S( 21,  24),   S( 10,  41),   S( 10,  34),   S( 11,  36),   S(  5,   9),   S( 23, -16),
            S( 25,  21),   S( 56,  16),   S( 21,  37),   S( 29,  27),   S( 16,  11),   S( 31,  25),   S( -8,  37),   S(-24,   9),
            S( 26,  22),   S( 21,  11),   S(-12,  48),   S( 21,  33),   S( 26,  23),   S(  7,  27),   S( 21, -13),   S(-35,  45),
            S(-61,  42),   S(-29,  59),   S( 64,   4),   S(-15,  34),   S(  3,  49),   S( 24,  13),   S(-28,  24),   S(-16,  67),
            S(-50,  81),   S(-36,  25),   S(-14,  46),   S(-28,  53),   S(-29,  35),   S(-52,  69),   S( 47, -15),   S(-21,  41),
            S(-39, -35),   S(-80,  76),   S(-71,  52),   S( 46,  39),   S( 60,  27),   S( 11,  17),   S(-14,  33),   S(-11,  46),

            /* bishops: bucket 1 */
            S( 24,  22),   S(-25,  48),   S(-34,  66),   S(-16,  20),   S(-18,  29),   S(-17,  36),   S(-18,  49),   S(-84,  48),
            S(-13,  39),   S( 21,  36),   S( 11,  26),   S( 17,  33),   S(-32,  57),   S(-26,  47),   S(-49,  57),   S( -7,  11),
            S( 38,  21),   S(-12,  48),   S( 15,  50),   S(-12,  65),   S( -2,  57),   S(-17,  65),   S( 17,  11),   S( -8,  23),
            S( 28,  26),   S( -3,  67),   S(-25,  67),   S( 16,  54),   S(  0,  50),   S(  9,  40),   S(-26,  63),   S(  4,  36),
            S( 24,  46),   S(-34,  67),   S( 14,  32),   S(-32,  62),   S( -5,  47),   S(-46,  85),   S( 32,  18),   S(-48,  72),
            S(-57,  70),   S( 42,  43),   S(  0,  60),   S(-26,  63),   S(-21,  79),   S(-28,  68),   S(-45,  72),   S( 34,  32),
            S(-15,  44),   S(  6,  62),   S( 25,  60),   S(-39,  81),   S(-21,  74),   S(-53,  77),   S(-13,  76),   S(-34,  56),
            S(-26,  87),   S(  7,  60),   S(-17,  50),   S(-28,  53),   S(  8,  69),   S(-32,  39),   S(-60,  88),   S(-51,  99),

            /* bishops: bucket 2 */
            S( -1,  49),   S(-20,  59),   S(-26,  49),   S(-45,  73),   S(-15,  55),   S(-44,  50),   S(-51,  46),   S(-61,  60),
            S(-34,  72),   S( -6,  58),   S(  1,  64),   S(-17,  68),   S(-22,  64),   S(-16,  51),   S(-11,  30),   S(-20,  -2),
            S(-29,  68),   S(-24,  77),   S(-13,  94),   S(-25,  88),   S(-22,  79),   S(  5,  63),   S(-18,  53),   S(-12,  35),
            S( -5,  73),   S(-44,  97),   S(-49, 101),   S(-30,  87),   S(-31,  91),   S(-16,  81),   S( -7,  70),   S(  4,  31),
            S(-35,  88),   S(-43,  84),   S(-56,  90),   S(-55,  87),   S(-43,  84),   S(-19,  66),   S( -5,  59),   S(-49,  91),
            S(-37,  65),   S(-28,  83),   S(-55,  99),   S(-101, 105),  S(-87, 104),   S(-85,  97),   S(-31,  96),   S(-10,  74),
            S(-69,  82),   S(-91, 108),   S(-65, 104),   S(-25,  80),   S(-44,  93),   S(-82,  92),   S(-82,  84),   S(-54,  96),
            S(-74, 152),   S(-115, 102),  S(-49,  90),   S(-144, 101),  S(-106, 104),  S(-143, 119),  S(-54,  95),   S(-92,  74),

            /* bishops: bucket 3 */
            S(-43,  73),   S(-11,  40),   S(  4,  41),   S(-19,  70),   S(-18,  62),   S( 35,  39),   S( 21,  11),   S( 29, -22),
            S(  2,  39),   S( -7,  73),   S(  5,  55),   S( -2,  75),   S(  1,  65),   S(-12,  80),   S( 36,  50),   S( -1,  17),
            S(  4,  69),   S(-15,  89),   S( -6, 100),   S(  5,  76),   S( -5, 104),   S( 13,  81),   S( 16,  54),   S( 42,  35),
            S( 19,  55),   S( -3,  94),   S(-15, 105),   S( -6, 100),   S( -8,  97),   S( 17,  73),   S( 15,  86),   S(  7,  38),
            S(-11,  92),   S( -9,  68),   S( -6,  81),   S( -8,  88),   S(  1,  85),   S(-11,  88),   S( 10,  67),   S( 19,  73),
            S(-23,  74),   S(-20,  90),   S(-12,  95),   S(-31,  93),   S(-40,  91),   S(  5,  81),   S(-10,  80),   S( -1, 107),
            S(-55,  95),   S(-60, 108),   S(  6,  75),   S(-29,  94),   S(-21,  92),   S(-37,  92),   S(-30, 107),   S( 10,  91),
            S(-45, 124),   S(-29, 101),   S( 27,  65),   S(-11,  91),   S(-21,  99),   S(-23, 107),   S(-17,  98),   S( 59,  46),

            /* bishops: bucket 4 */
            S(-17,  -3),   S(-54,  26),   S(-66,  33),   S(-56,  36),   S( -7,  22),   S(-56,  21),   S(-16,  22),   S( -3,   6),
            S(-15,  34),   S( -4,  10),   S(-92,  66),   S(-47,  28),   S(-50,  26),   S( 92, -15),   S(-69,  44),   S(  5,   6),
            S(-39,  -2),   S(-54,  26),   S(-37,  28),   S(-13,  38),   S( 24,  35),   S( 28,  14),   S(-46,  -5),   S(-114,  -3),
            S(-18,   7),   S(-22,  67),   S( 85,  -5),   S( 36,  15),   S( 34,  26),   S(103,   6),   S( -9,  46),   S(-34, -31),
            S( 73,  -7),   S( 48,   6),   S(-21,  56),   S( 55,  11),   S(-49,  31),   S(-27,  34),   S(-25,   8),   S(  5,  -3),
            S( 18,  40),   S( 40,  30),   S( -1,  49),   S( 95,   9),   S(-63,  41),   S(-15,  -5),   S( 32,  -5),   S(-56,  20),
            S(-18,  37),   S( 30,  22),   S( 25,  11),   S( -4,  54),   S(-40,  10),   S( 14,  23),   S(-16, -11),   S(-35,  23),
            S( 15,  41),   S(-62,  14),   S(  2,   0),   S( 14,  11),   S(-11,  32),   S(-34,  -7),   S(-16, -23),   S(-36, -22),

            /* bishops: bucket 5 */
            S(-13,  29),   S(-43,  52),   S(-12,  55),   S(-55,  70),   S( 31,  43),   S( 14,  26),   S(-11,  20),   S(-18,  29),
            S( -9,  47),   S( -5,  52),   S(-16,  61),   S( 52,  49),   S( 45,  33),   S(-21,  57),   S( -4,  26),   S(-69,  28),
            S(-12,  52),   S(-10,  65),   S( 77,  42),   S( 47,  40),   S( 51,  43),   S(-28,  47),   S( 10,  39),   S(-27,  35),
            S( 64,  50),   S( 53,  50),   S( 56,  45),   S( 56,  30),   S( 89,  29),   S( 55,  46),   S(-32,  59),   S(-22,  61),
            S( 16,  70),   S( 38,  37),   S( 64,  45),   S( 99,  22),   S(127,   3),   S( 72,  23),   S( 49,  43),   S(-56,  63),
            S( 14,  53),   S( 77,  55),   S( 70,  40),   S( 80,  46),   S( 46,  47),   S(-32,  55),   S(-40,  55),   S(  8,  79),
            S( -2,  44),   S(-48,  72),   S( 10,  58),   S( 36,  60),   S( -8,  71),   S(-26,  69),   S(-51,  55),   S(-27,  48),
            S(-10,  82),   S( 67,  51),   S( 24,  50),   S( 11,  65),   S( 22,  49),   S(  0,  78),   S( 23,  69),   S( -2,   8),

            /* bishops: bucket 6 */
            S(-128,  72),  S( -5,  53),   S(-60,  70),   S(-38,  56),   S(  8,  57),   S(-11,  57),   S(  6,  30),   S( -2,  26),
            S( 52,  18),   S( 26,  37),   S( 44,  43),   S( 23,  57),   S( 48,  42),   S( 46,  29),   S(-75,  68),   S( 36,  21),
            S( 21,  34),   S( 15,  38),   S( 84,  36),   S( 65,  41),   S(103,  11),   S(  3,  59),   S( -3,  62),   S(-23,  62),
            S( -5,  69),   S(101,  48),   S( 68,  44),   S( 96,  22),   S( 40,  40),   S( 41,  42),   S( 13,  63),   S(-56,  74),
            S( -3,  68),   S( 17,  45),   S( 72,  29),   S( 57,  33),   S(106,  34),   S( 87,  44),   S( 15,  54),   S( 21,  39),
            S( 33,  48),   S(  9,  50),   S( 27,  57),   S( 43,  47),   S( 78,  40),   S( 93,  36),   S(118,  43),   S( 10,  65),
            S(-36,  27),   S( 26,  56),   S( 55,  56),   S( 12,  66),   S( 47,  73),   S( 74,  54),   S( 12,  60),   S(-76,  82),
            S( -4,  78),   S( 60,  36),   S( 27,  64),   S(-14,  67),   S(-16,  71),   S(-16,  76),   S( 35,  58),   S(  4,  57),

            /* bishops: bucket 7 */
            S(-23,  11),   S( -7,  28),   S(-24,   2),   S(-19,  24),   S(-34,  33),   S(-39,  28),   S(-82,   2),   S(-75,   4),
            S(-73,  31),   S(-13,  20),   S(-17,  39),   S( 34,  22),   S( 26,  15),   S(-27,  20),   S(-29,  19),   S(-26,  -8),
            S(-39,  28),   S( 35,  11),   S(  8,  38),   S( 63,  12),   S( 28,  26),   S( 35,  10),   S(-21,  42),   S( -4,  36),
            S( 46,  22),   S( 17,  47),   S(150, -10),   S( 79,  21),   S(122,   2),   S( 84,   0),   S( 90,  23),   S(-10,  46),
            S(  4,  33),   S(  5,   7),   S( 64,   9),   S( 52,   3),   S(121,   5),   S(133,   2),   S( 46,  25),   S( 72,  -8),
            S(-38,   8),   S(-30,  31),   S(  6,  22),   S( 14,  25),   S( 78,  14),   S( 54,  34),   S( 22,  49),   S( 50,  16),
            S( -4,   4),   S(  7,  10),   S( 23,  32),   S(  3,  30),   S(  5,  23),   S(124,   4),   S( 50,  37),   S(-54,  57),
            S(  8,  28),   S(-44,  53),   S(-16,  18),   S( 19,  14),   S( 62,  31),   S( -1,  41),   S( 45,  33),   S( 56,  55),

            /* bishops: bucket 8 */
            S(  0, -52),   S( -6, -59),   S(-30,   3),   S(  2,  15),   S(-26,   0),   S(-29, -33),   S(-23, -59),   S( -2,   3),
            S(-20, -38),   S(-34, -115),  S( -6, -12),   S( 13,  -1),   S( 60,  -4),   S( 18, -52),   S( 10, -38),   S(  4, -41),
            S( 17, -25),   S(-15, -16),   S( 11, -17),   S( 46, -30),   S( 22, -15),   S( -3, -32),   S( 37, -28),   S(-48, -21),
            S( -7,  42),   S(  7,  -2),   S( -7, -20),   S( 36, -39),   S( 15, -31),   S( 36, -33),   S( 26, -45),   S(  4, -30),
            S( 24,  54),   S( 14, -19),   S( 61, -35),   S( 60, -36),   S( 71, -40),   S( 44, -28),   S( -2, -35),   S( 23,  23),
            S(-24,  -5),   S( 23,   9),   S( 35, -28),   S( 13, -23),   S( 54, -23),   S( -1, -38),   S( 32, -42),   S( -6, -43),
            S(-11, -20),   S( 16, -14),   S( 28, -19),   S(  8, -44),   S(  1, -10),   S( 12, -28),   S(-12, -45),   S( -9, -54),
            S( -5, -42),   S( 16, -21),   S( -1, -41),   S( -1, -17),   S(  2, -50),   S( 10, -28),   S( 17,   4),   S( -4, -36),

            /* bishops: bucket 9 */
            S(-21, -104),  S(  9, -86),   S(-58, -35),   S( -6, -15),   S(-44, -42),   S(-21, -45),   S(-36, -73),   S(  7, -26),
            S( 10, -54),   S( 14, -59),   S(-11, -44),   S( -1, -18),   S( 12, -23),   S( 16, -57),   S( 26, -53),   S(  3, -66),
            S( 11, -50),   S( 33, -31),   S( 37, -62),   S( 46, -56),   S( 94, -48),   S( 77, -50),   S(  7, -46),   S(-26,   4),
            S(-20,   1),   S( 37, -25),   S(  1, -24),   S(181, -98),   S( 84, -57),   S( 57, -49),   S( 64, -35),   S(-19, -54),
            S( -3, -35),   S( 48, -26),   S( 78, -34),   S( 87, -49),   S( 96, -79),   S( 89, -49),   S( 39, -41),   S(  7, -19),
            S( 10, -47),   S( 79, -31),   S(  4, -23),   S( 87, -44),   S( 66, -56),   S( 45, -71),   S( 22, -65),   S(-19, -87),
            S(-15, -51),   S( 25, -15),   S(  6, -40),   S( 55, -27),   S( 40, -39),   S(  8, -72),   S(  0, -35),   S(-17, -84),
            S(-11, -31),   S(-24, -52),   S( -2, -44),   S(-10, -42),   S( 10, -37),   S(  4, -11),   S(  4, -54),   S(-20, -73),

            /* bishops: bucket 10 */
            S(-39, -73),   S( 12, -69),   S(-84, -55),   S(-17, -59),   S(-16, -22),   S(-16, -62),   S( -4, -84),   S(  6, -83),
            S( 17, -67),   S(-12, -63),   S(-26, -69),   S( -5, -55),   S(-34, -48),   S( 24, -77),   S( -7, -76),   S(-13, -42),
            S( -8, -59),   S( 23, -55),   S( 43, -89),   S( 96, -89),   S(103, -93),   S( 42, -75),   S(-50, -51),   S( -4, -32),
            S(  8, -42),   S( 55, -69),   S(107, -83),   S(136, -94),   S(155, -106),  S( 60, -80),   S( -6, -44),   S( -1, -26),
            S(-16, -48),   S( 43, -74),   S(104, -86),   S(211, -111),  S(122, -85),   S( 73, -75),   S(  3, -27),   S( -1, -32),
            S(-12, -69),   S( 38, -95),   S( 79, -104),  S( 72, -75),   S( 87, -59),   S( 97, -53),   S( 77, -69),   S(-19, -77),
            S(-36, -155),  S( 17, -78),   S( -7, -71),   S( 38, -69),   S( 30, -56),   S( 13, -58),   S( 18, -24),   S( 17, -13),
            S(-14, -111),  S( -7, -79),   S( 18, -49),   S(  3, -37),   S( -3, -72),   S(-24, -60),   S( -3, -31),   S(  5, -28),

            /* bishops: bucket 11 */
            S(-36, -32),   S(-59, -38),   S(-82, -48),   S(  6, -26),   S(-27,  -1),   S(-52, -52),   S( -8, -53),   S(-23, -72),
            S( -8, -72),   S(  2, -47),   S( 16, -38),   S(-17, -34),   S(-22, -67),   S(-31, -34),   S(-47, -97),   S(-20, -15),
            S( 39, -15),   S( 46, -63),   S( 60, -65),   S( 66, -71),   S( 41, -60),   S( 66, -63),   S(-21, -24),   S( -5, -43),
            S( -9, -34),   S( 16, -47),   S( 69, -81),   S(132, -94),   S(159, -69),   S( 38, -18),   S( 18, -13),   S(  9,  24),
            S(-25, -48),   S( 14, -72),   S( 38, -66),   S(128, -96),   S( 95, -55),   S( 96, -51),   S(  6,   2),   S( 14, -40),
            S(-12, -106),  S( 36, -67),   S( 39, -66),   S( 41, -43),   S( 43, -53),   S( 91, -32),   S(  5,   2),   S(-45, -28),
            S( -4, -50),   S( 42, -94),   S(  0, -62),   S( 12, -83),   S(  2, -36),   S( 58, -32),   S(  1, -44),   S(  7, -10),
            S(-11, -100),  S( -8, -77),   S( 13, -51),   S( 15, -26),   S( 17, -66),   S( -6, -70),   S( 12, -48),   S( -7, -36),

            /* bishops: bucket 12 */
            S( -8, -19),   S(  7,  -8),   S(-46, -108),  S( -8, -34),   S(-13, -26),   S(-18,  -7),   S( -4, -16),   S(-16, -45),
            S(-11, -53),   S(-14, -58),   S( -5, -56),   S(-14, -35),   S( 19, -19),   S(-14, -31),   S( -3, -61),   S( -2, -23),
            S(  0,  -8),   S(-21, -73),   S( -7, -24),   S( 12, -30),   S(-15,  -5),   S( 25,  -9),   S(  0,  -9),   S(-15, -10),
            S(  1,  17),   S(  3, -16),   S( -9, -48),   S(-18, -28),   S( -9, -52),   S( 26,  34),   S(-19, -76),   S( -8, -50),
            S(  8,  -4),   S(  0, -11),   S( 10, -39),   S( 19, -61),   S(  5, -59),   S( 18, -15),   S(  1, -32),   S(  1,   9),
            S(-28, -20),   S(  1,  -2),   S(-39, -28),   S( -7, -61),   S( 20,  -8),   S(  8,   9),   S(  8, -55),   S( -8,  14),
            S(  2,  36),   S( -5, -15),   S( -6,  -7),   S(-15, -12),   S(  1, -44),   S( -9, -56),   S(  4,  -6),   S( -3, -15),
            S( -3, -24),   S( -3,   2),   S(-21, -45),   S( 16,  31),   S(  9,  -8),   S(  1, -26),   S(-20, -68),   S( -4,  -6),

            /* bishops: bucket 13 */
            S( -8, -63),   S(-10, -97),   S( -7, -31),   S( -9, -55),   S(-17, -48),   S(-13, -19),   S( 10, -11),   S(-18, -57),
            S(  2, -31),   S(-19, -91),   S( -9, -102),  S( -6, -53),   S(  1, -83),   S(  0, -29),   S(  2, -27),   S( 12, -24),
            S( -8, -76),   S( -4, -34),   S(  7, -78),   S( 25, -88),   S( 32, -52),   S( 14, -69),   S( 11,   3),   S(  4, -12),
            S( -2, -13),   S(  5, -44),   S( 28, -16),   S( 37, -50),   S( 60, -55),   S(  6,  -9),   S(  4, -60),   S(  9,   3),
            S(  1,  -8),   S(-10, -20),   S( 14, -83),   S( 42, -54),   S(  9, -55),   S( 10, -86),   S( 10, -42),   S(  7,  -5),
            S( -2, -32),   S(  7, -48),   S(-37, -41),   S( 42, -14),   S( -1, -27),   S( 15, -36),   S( 34, -43),   S(-22, -92),
            S( -9, -12),   S(  2, -47),   S(  5,  11),   S(-34, -78),   S( -5, -53),   S(  5, -48),   S( -4, -44),   S(  0, -31),
            S(-16, -42),   S( -3, -28),   S( -3,  -9),   S(-12, -40),   S( -8, -32),   S(-13, -72),   S(  1, -59),   S(  0, -40),

            /* bishops: bucket 14 */
            S( -8, -61),   S(-12, -81),   S( -2,   8),   S( -6, -50),   S(-12, -67),   S( -9, -70),   S( -5, -54),   S(-15, -84),
            S(-22, -75),   S( -1,  -8),   S(  9, -61),   S(-11, -87),   S(  6, -79),   S( -9, -80),   S( -3, -56),   S( 11, -20),
            S(  7, -12),   S(  1, -61),   S(-23, -95),   S(  5, -76),   S( 12, -76),   S( -8, -72),   S(  0, -59),   S( 18,  22),
            S( -7, -25),   S( -5, -87),   S( -8, -69),   S(  3, -76),   S( 27, -103),  S(-11, -93),   S(  5, -42),   S( -4,   4),
            S(-18, -55),   S( 17, -47),   S( 13, -75),   S(  6, -72),   S( 25, -105),  S(  6, -73),   S( 24, -20),   S( -2, -51),
            S( -8, -73),   S( 33, -50),   S( 10, -60),   S( 16, -59),   S(-24, -53),   S(-13, -30),   S(  5, -102),  S(-16, -103),
            S( -2, -38),   S( 11, -93),   S(-10, -73),   S(  4, -56),   S(-22, -60),   S(-11, -62),   S( -7, -48),   S( -7, -26),
            S(-10, -81),   S( -6, -47),   S(  4, -40),   S( 11,  -7),   S(-10, -52),   S(  7,   0),   S( 18,  45),   S( -2, -36),

            /* bishops: bucket 15 */
            S( -3,  -8),   S( -1,  -4),   S( -1, -24),   S(  4,  12),   S( -8, -34),   S(-24, -71),   S(  2, -15),   S(-13, -36),
            S( -7, -26),   S( -3, -10),   S( 25,  -9),   S( -1, -48),   S(-18, -41),   S(-14, -35),   S(-10, -48),   S( -7, -37),
            S( -7, -51),   S(-13, -42),   S( -7, -23),   S(  7, -41),   S( 15, -55),   S( -3, -29),   S(-10, -43),   S( -3, -27),
            S(-19, -64),   S(-13, -65),   S(  2, -29),   S(-38, -65),   S( 13, -47),   S( -2, -60),   S( 11,  24),   S( 10,   4),
            S(  6, -16),   S(  2, -19),   S( -4, -78),   S(-27, -65),   S(  9, -41),   S( -9, -14),   S( 15, -25),   S(  1, -28),
            S(  3,   0),   S( -6, -90),   S(-21, -64),   S(-49, -78),   S(-16, -22),   S( 12, -28),   S(  3, -87),   S( -9, -38),
            S( -8, -26),   S(  5, -35),   S(-12, -58),   S( -6, -69),   S(-29, -81),   S(  5, -22),   S(-17, -44),   S(-19,   0),
            S(  3, -20),   S( -5, -38),   S(-10, -48),   S(  1, -24),   S(-18, -85),   S(-16, -42),   S(-31, -16),   S(  6,   4),

            /* rooks: bucket 0 */
            S(-14,   3),   S( 50, -21),   S( 27, -13),   S( 19, -14),   S( 26,  -7),   S( 25, -13),   S( 23,   4),   S( 34,  25),
            S( -9, -39),   S( 31,  -5),   S( 19,  -4),   S( 21,   5),   S( 22, -21),   S( 26, -34),   S( -9,  13),   S( -8,  22),
            S(-12,   0),   S( 40,  -2),   S( 53,  -6),   S( 25,   5),   S( 24,  19),   S(  3,  12),   S( 25,  -5),   S(-42,  29),
            S( 73, -29),   S(113, -26),   S( 53,   5),   S( 55, -10),   S( 59, -23),   S( 23,  17),   S( 19,  21),   S(  1,  22),
            S(126, -53),   S(162, -47),   S( 99, -21),   S( 61, -13),   S(107, -40),   S( 50, -10),   S( 58,  -2),   S( 25,  -1),
            S( 82, -33),   S(129, -50),   S(102, -40),   S( 53,  -6),   S( 39,  -7),   S( 16,   9),   S( 80,   3),   S( 23,  15),
            S( 47,  -1),   S(116, -14),   S( 48,  15),   S( 21,   5),   S(-40,  41),   S( 35,   4),   S(-37,  41),   S( 16,  27),
            S( 47,  16),   S( 53,  25),   S( 37,  32),   S( 47,   3),   S( 62, -14),   S( 32,  33),   S( 26,  13),   S( 35,  28),

            /* rooks: bucket 1 */
            S(-73,  53),   S(-51,  47),   S(-38,  28),   S(-60,  26),   S(-22,   0),   S(-37,  25),   S(-29,  10),   S(-36,  55),
            S(-38,  18),   S(-52,  37),   S(-23,  33),   S(-23,   2),   S(-57,  35),   S(-36,  18),   S(-37,   7),   S(-54,  21),
            S(-62,  58),   S(-24,  28),   S(-39,  58),   S(-47,  51),   S(-42,  51),   S(-33,  39),   S(-17,  27),   S(-65,  35),
            S(-61,  89),   S(-48,  74),   S(-21,  68),   S(  2,  48),   S(-46,  61),   S(-61,  62),   S(-48,  63),   S(-36,  49),
            S( 40,  37),   S( 39,  52),   S(-20,  55),   S(-34,  73),   S(-13,  38),   S(-12,  67),   S(-16,  50),   S(-43,  37),
            S( 66,  34),   S(-10,  68),   S( -2,  55),   S(-63,  72),   S( 16,  51),   S(-18,  34),   S( -2,  46),   S(-46,  63),
            S(-20,  70),   S( -9,  80),   S(  4,  68),   S(-73,  85),   S(-58,  70),   S( 48,  27),   S(-58,  72),   S(-86,  91),
            S( 36,  61),   S( 12,  89),   S(-29,  75),   S(-54,  75),   S(-72,  81),   S( -8,  45),   S( -3,  70),   S( 15,  48),

            /* rooks: bucket 2 */
            S(-94, 123),   S(-59,  90),   S(-54,  67),   S(-80,  81),   S(-87,  73),   S(-87,  85),   S(-72,  62),   S(-55,  64),
            S(-86, 102),   S(-88, 108),   S(-51,  73),   S(-77,  78),   S(-84,  75),   S(-71,  60),   S(-88,  83),   S(-86,  63),
            S(-85, 104),   S(-83, 108),   S(-57,  95),   S(-60,  80),   S(-93,  96),   S(-59,  91),   S(-33,  68),   S(-83,  78),
            S(-86, 128),   S(-57, 119),   S(-48, 103),   S(-34,  96),   S(-48, 100),   S(-21,  90),   S(-63, 116),   S(-42,  94),
            S(-47, 112),   S(-39, 116),   S(-66, 119),   S(-15,  92),   S(-21, 104),   S(-25,  96),   S(-57, 121),   S(-92, 126),
            S(-28, 112),   S(-53, 118),   S(-88, 128),   S(-41,  96),   S( -2,  88),   S(  4,  87),   S( -5,  95),   S(-67, 112),
            S(-97, 135),   S(-87, 138),   S(-64, 119),   S(-40,  94),   S(-15,  91),   S(-22,  93),   S(-56, 117),   S(-44, 108),
            S(-55, 136),   S(-56, 137),   S(-79, 121),   S(-41,  97),   S(-77, 123),   S( 35,  92),   S(-63, 133),   S(  4,  90),

            /* rooks: bucket 3 */
            S( -7, 128),   S( -7, 124),   S(  5,  98),   S(  0, 101),   S( -5,  93),   S(-23, 115),   S( -4, 112),   S(-15,  94),
            S(-25, 126),   S( -8, 112),   S( -8, 119),   S( -4, 111),   S(  0, 100),   S(  6,  94),   S( 30,  61),   S( 15,  27),
            S(-57, 143),   S(-29, 125),   S(-10, 128),   S( 10, 104),   S(  0, 103),   S( 17, 102),   S( 32,  99),   S(-27,  96),
            S(-19, 139),   S(-17, 145),   S( 12, 114),   S( 20, 117),   S( 17, 111),   S( -1, 143),   S( 53, 122),   S( -2, 115),
            S(-13, 152),   S( 20, 130),   S( -1, 124),   S( 33, 106),   S( 28, 110),   S( 59, 101),   S( 87,  95),   S( 11, 106),
            S( 20, 135),   S( 10, 132),   S( -8, 133),   S(  6, 121),   S( 23, 102),   S( 45,  89),   S( 63,  89),   S( 66,  83),
            S(-24, 151),   S(-16, 159),   S( -3, 140),   S( 14, 113),   S(  1, 113),   S( 46,  94),   S( 72, 110),   S( 95,  89),
            S(-69, 208),   S(-27, 172),   S(  2, 141),   S( 45, 112),   S( 43, 100),   S( 75,  93),   S(110, 104),   S( 82,  92),

            /* rooks: bucket 4 */
            S(-91,  18),   S( -6, -25),   S(-39,   0),   S( -2,  -8),   S(-16, -18),   S(-40,  10),   S(-39,  11),   S(  4, -16),
            S(-42,  -2),   S(-61,  16),   S(-30,  -6),   S(  8,   5),   S( 24, -16),   S( 15, -20),   S( 14, -18),   S(-36,  11),
            S( 32,  18),   S(-55,  -2),   S(-25,  14),   S(-28, -12),   S(-33,  11),   S(-18,  -7),   S(  1, -22),   S(-91,  22),
            S(-26,  -5),   S( -4,  -4),   S(-45,  20),   S( -9,  18),   S(  5,   6),   S(  7, -11),   S(  6,   6),   S(-54,  56),
            S(-14,  -1),   S(-59,  36),   S(-50,  10),   S( 65, -12),   S(-23,  16),   S( 17,  -2),   S( 27,   0),   S(-14,  21),
            S( 18,   2),   S( 82, -14),   S( 64, -22),   S( 61,   4),   S(-11,  22),   S( 41,  14),   S( 10,  34),   S( 21,  36),
            S( 17,   4),   S(  7,  29),   S( 20,   9),   S( 48,  -2),   S( 95, -34),   S( 60, -28),   S( 97,   4),   S(-27,  44),
            S( 64, -37),   S( 42,  30),   S( 71, -10),   S( 23,  -2),   S( 14,  -9),   S( 55,  -1),   S(-11,  33),   S( 33,  25),

            /* rooks: bucket 5 */
            S(  5,  46),   S(  1,  62),   S(-41,  65),   S( -5,  30),   S( 19,   9),   S( 19,  38),   S( 38,  29),   S( 13,  51),
            S( -8,  46),   S(  4,  40),   S(-68,  89),   S(-60,  74),   S(-59,  54),   S(-30,  50),   S( 12,  31),   S(-49,  56),
            S(-37,  73),   S(-49,  93),   S(-40,  84),   S(-101,  89),  S(-67,  68),   S( -4,  53),   S(-18,  73),   S( 22,  42),
            S(-21,  85),   S(-32,  86),   S(-69,  94),   S(-69,  93),   S(-33,  71),   S( 29,  62),   S(-14,  75),   S( 53,  36),
            S(-14,  82),   S( 11,  73),   S( 23,  69),   S( 26,  93),   S( 52,  66),   S( 43,  82),   S(-11,  93),   S( 29,  54),
            S( 39,  83),   S(127,  56),   S( 73,  64),   S(  0, 105),   S( 59,  50),   S( 89,  54),   S( 40,  72),   S(130,  43),
            S( 41,  69),   S( 86,  55),   S( 84,  52),   S( 13,  65),   S(117,  30),   S( 60,  40),   S(114,  39),   S(120,  50),
            S(101,  51),   S(109,  51),   S( 26,  69),   S( 35,  70),   S( 82,  44),   S( 88,  40),   S(102,  59),   S(-16,  84),

            /* rooks: bucket 6 */
            S( -9,  49),   S( -6,  61),   S( 28,  31),   S( 21,  25),   S(-37,  61),   S(-19,  60),   S(  6,  59),   S( 15,  58),
            S( -2,  58),   S( 22,  48),   S( 11,  47),   S( -7,  54),   S(-53,  78),   S(-53,  88),   S(-27,  85),   S( 21,  33),
            S(-37,  92),   S(-61,  89),   S(  0,  64),   S(-63,  79),   S(-73,  82),   S(-71,  91),   S(-11,  75),   S(-36,  66),
            S(-61, 112),   S( -8,  93),   S(-16,  89),   S(-13,  77),   S(-35,  81),   S(-44,  85),   S(-88, 108),   S(-11,  59),
            S( 27,  93),   S( 15,  97),   S( 81,  54),   S( 42,  62),   S(-48, 107),   S(  0,  82),   S( 80,  62),   S( 45,  58),
            S( 50,  79),   S( 51,  84),   S( 89,  62),   S( 58,  51),   S(  1,  98),   S( 66,  64),   S( 58,  68),   S( 93,  63),
            S( 62,  79),   S( 67,  73),   S( 88,  48),   S(100,  34),   S(118,  37),   S(101,  49),   S(116,  39),   S( 62,  69),
            S(142,  68),   S(120,  66),   S( 98,  44),   S( 72,  50),   S( 90,  52),   S(131,  59),   S(116,  59),   S( 85,  64),

            /* rooks: bucket 7 */
            S(-64,  26),   S(-38,  33),   S(-23,  19),   S( 16,   1),   S(  4,  10),   S( -4,  24),   S(  3,  25),   S( 28,   9),
            S(-73,  47),   S(-25,  34),   S(-36,  20),   S(-26,  31),   S(  0,  23),   S(  0,  16),   S(-42,  32),   S(  5,  15),
            S(-66,  62),   S(-39,  30),   S(-45,  53),   S(-14,  22),   S(  9,   5),   S(-47,  30),   S(-45,  30),   S( 15, -24),
            S(-74,  76),   S(  2,  59),   S(-13,  61),   S( -3,  43),   S(-17,  46),   S( -2,  32),   S( 20,  39),   S(-26,  30),
            S(-33,  71),   S( 32,  41),   S( 56,  16),   S( 31,  34),   S( 51,  23),   S( 68,  21),   S( 65,  25),   S( 73,   2),
            S(-47,  79),   S( 21,  49),   S( 93,   9),   S(110,   2),   S(116,   0),   S( 83,  16),   S( 55,  37),   S( 72,  12),
            S( 16,  62),   S( 31,  44),   S(107,  10),   S(113,  -1),   S(141, -14),   S( 71,  27),   S( 97,  36),   S(-29,  53),
            S( 78,  54),   S( 86,  43),   S( 70,  20),   S(112,   3),   S( 97,   4),   S( 84,  16),   S( 86,  29),   S(102,   2),

            /* rooks: bucket 8 */
            S(-53, -28),   S(-45,  23),   S(-43,  -3),   S(  4, -35),   S(-32, -26),   S(  6, -38),   S(-51, -39),   S(-85,   7),
            S( -1, -38),   S(-23,  -4),   S(-23, -26),   S(-11, -25),   S(-47, -47),   S(-23, -34),   S( -5, -48),   S(  3, -38),
            S(  9, -26),   S(-14,  11),   S(-25,   1),   S( 14, -35),   S(-12, -27),   S(-19, -35),   S(-47, -16),   S(-18,   5),
            S(-38, -26),   S(-24,  42),   S(-24,  -4),   S(  3,   0),   S( -1,  -6),   S(-20, -17),   S(  1, -15),   S(-14,  -1),
            S(  0,  -3),   S(-14,  36),   S(-20,  34),   S(-16,   9),   S(  9, -16),   S(-11, -27),   S( 14,  -2),   S( -9, -34),
            S(-29, -22),   S(  3,   3),   S( -4,  -3),   S(  8, -26),   S(  3,  -6),   S( 21,  13),   S( 18,  -2),   S(-17,  -5),
            S(  0, -13),   S(-24,   5),   S( 15, -45),   S( 35, -24),   S( 38, -36),   S( 19, -28),   S( 31, -23),   S( 33,  -4),
            S( 43, -109),  S(  3, -25),   S( 11, -15),   S( 16, -37),   S( 11, -14),   S( 11, -10),   S( 18, -13),   S( 17,   8),

            /* rooks: bucket 9 */
            S(-89, -46),   S(-42, -52),   S(-32, -70),   S(-80, -56),   S(-55, -58),   S(-18, -52),   S(-18, -71),   S(-78, -55),
            S( 32, -65),   S(  2, -74),   S( -6, -58),   S(-71, -46),   S(-16, -68),   S( 49, -66),   S( -1, -29),   S(-36, -49),
            S(  0, -87),   S( 14, -70),   S(-12, -42),   S(-12, -45),   S(-39, -50),   S( 34, -61),   S(-26, -46),   S( 12, -58),
            S(-15, -60),   S( -1, -61),   S(  6, -40),   S(-23, -33),   S(-20, -35),   S( 35, -47),   S( 21, -51),   S(  8, -64),
            S(-13, -35),   S(-35, -16),   S(-12, -19),   S( -7, -17),   S( 12, -22),   S( 13, -39),   S(  7, -26),   S(  8, -53),
            S( 25, -31),   S( -3, -37),   S( 33, -28),   S( 11, -42),   S( -2, -32),   S( 13, -38),   S( 53, -59),   S( 26, -53),
            S( 54, -61),   S(100, -95),   S( 68, -74),   S( 80, -49),   S( 37, -71),   S( 71, -71),   S( 93, -82),   S(116, -71),
            S(109, -112),  S( 49, -88),   S( 47, -68),   S( 43,  -8),   S( 37, -53),   S( 41, -65),   S( 36, -56),   S( 34, -38),

            /* rooks: bucket 10 */
            S(-119, -103), S(-61, -108),  S(-33, -117),  S(-40, -103),  S(-54, -88),   S(-39, -98),   S( 29, -109),  S(-58, -73),
            S(-13, -93),   S( -7, -87),   S(-15, -84),   S( -3, -70),   S(-14, -69),   S(-26, -76),   S( 45, -70),   S( 18, -76),
            S(-19, -100),  S(  2, -87),   S(-18, -80),   S(-27, -71),   S(-32, -64),   S(-21, -73),   S( 19, -57),   S( 21, -83),
            S(  4, -86),   S( -4, -81),   S(  2, -97),   S(-56, -62),   S( 18, -68),   S(  3, -53),   S( 20, -74),   S(-15, -82),
            S( -9, -67),   S( 26, -78),   S(  1, -74),   S( 10, -86),   S(-14, -86),   S(  5, -41),   S( 10, -43),   S( 28, -62),
            S( 15, -63),   S( 32, -58),   S( 59, -78),   S( 11, -83),   S(-10, -64),   S( 32, -55),   S( 25, -53),   S( 25, -78),
            S(136, -105),  S(141, -113),  S(152, -123),  S(142, -121),  S( 87, -107),  S( 71, -74),   S( 80, -98),   S( 93, -112),
            S( 87, -67),   S( 40, -64),   S( 93, -102),  S( 50, -91),   S( 29, -74),   S( 44, -60),   S( 31, -82),   S( 46, -95),

            /* rooks: bucket 11 */
            S(-140, -31),  S(-19, -29),   S(-35, -58),   S(-47, -50),   S(-35, -44),   S(-55, -27),   S( -1, -29),   S(-116, -27),
            S(-78,   6),   S(  8, -30),   S( -5, -52),   S(-32, -37),   S(-54, -25),   S( -4, -25),   S( -9, -15),   S(-62, -45),
            S(-36, -32),   S( 17, -41),   S( 10, -34),   S( -7, -63),   S( 21, -39),   S(-12, -39),   S(-47, -48),   S( -4, -26),
            S( -5,  -7),   S(-10, -32),   S(-11, -17),   S(-33, -21),   S( 18, -38),   S(-20,  -7),   S(  7, -22),   S(-29, -54),
            S(  3,  -1),   S( 26, -35),   S( 24, -54),   S( 45, -40),   S( 34, -30),   S( 44, -32),   S( -3,  -6),   S(-13, -41),
            S(-29,   6),   S(  6,  -9),   S( 49, -39),   S( 77, -38),   S( 13, -21),   S( 40, -12),   S(-20,  -2),   S( -2,  -3),
            S( 80, -18),   S( 76, -37),   S(104, -60),   S( 93, -52),   S( 51, -33),   S( 67, -36),   S( 50,  11),   S( 69, -28),
            S( 48,  -2),   S( 41,   1),   S( 58, -26),   S( 42, -34),   S( 30, -17),   S(  4, -47),   S(  9,   4),   S( 38,  -8),

            /* rooks: bucket 12 */
            S(  2, -36),   S( -9, -59),   S( -5,  -3),   S(-23, -64),   S(  0, -56),   S( -8, -30),   S(-35, -72),   S(-16, -29),
            S( 18,  -7),   S( 16, -20),   S(-16, -40),   S( -8, -55),   S(-14,  -7),   S( -6, -58),   S(-16, -74),   S(  2, -58),
            S( 20, -17),   S( -6, -52),   S( -4, -25),   S( 15,  -4),   S(-16, -33),   S(  6,  -4),   S( -5, -32),   S(-12, -63),
            S(-11, -20),   S( -8, -44),   S( -6, -47),   S(  8, -43),   S(-14, -64),   S( -9, -46),   S( -7, -44),   S(-20, -82),
            S(  0, -25),   S( -9, -34),   S( 12, -54),   S( 14, -61),   S(-18, -73),   S( -5, -71),   S( -1, -73),   S(  5,  -5),
            S(-10, -14),   S( 26, -44),   S( 18, -41),   S( 27, -41),   S(-11, -68),   S( -5, -63),   S(  3, -76),   S(  5,  -2),
            S( -2, -34),   S( 24, -47),   S( 13, -22),   S( 22,   6),   S(  9, -52),   S(  0, -44),   S(  4, -36),   S( -3, -54),
            S(-13, -52),   S( 15,   1),   S( -1, -73),   S(  6, -46),   S(  7, -57),   S(-10, -63),   S(  0, -55),   S( 12,  -3),

            /* rooks: bucket 13 */
            S(-17, -70),   S(  1, -25),   S(-12, -24),   S(-27,  37),   S( -1, -25),   S(-27, -63),   S(-19, -18),   S(-22, -52),
            S( -5, -58),   S(-13, -12),   S(-29, -17),   S( -7, -29),   S(-23, -53),   S(-19, -45),   S(-10, -35),   S(  7, -29),
            S(-16, -71),   S( 13, -39),   S(-23, -82),   S(  9, -57),   S(-22, -24),   S(  4, -25),   S(-23, -66),   S( -4, -33),
            S(-29, -74),   S(-21, -65),   S(-25, -45),   S(-20, -61),   S( -3, -41),   S(-24, -61),   S( -1, -72),   S(  3, -32),
            S( -3, -66),   S(-18, -61),   S(  6, -58),   S(-13, -83),   S(-32, -62),   S(  7, -60),   S(  4, -58),   S( 21,  -7),
            S(-21, -46),   S( 15,  -8),   S( 15, -29),   S( 22, -32),   S( 25, -51),   S( 18, -22),   S( 22, -18),   S(  4,  -7),
            S(-10, -35),   S(-22, -23),   S(-16, -49),   S( -2, -34),   S(  4, -45),   S(  8, -35),   S(  5, -50),   S(  6,  -6),
            S(-32, -159),  S(-30, -119),  S(  4,   2),   S( -7, -52),   S(  1, -36),   S(-20, -49),   S( -7, -61),   S( -7, -30),

            /* rooks: bucket 14 */
            S(-25, -42),   S(-63, -86),   S(-17, -57),   S( 11, -41),   S( -7,  14),   S(-14, -45),   S( 11, -29),   S(-35, -44),
            S(-13, -62),   S(-22, -78),   S(-14, -71),   S(-37, -99),   S( -3, -29),   S(-24, -77),   S(  1, -29),   S( 14, -22),
            S(-25, -103),  S(-31, -100),  S(-25, -111),  S(-25, -96),   S(-18, -54),   S(-24, -68),   S( 13,  -7),   S(-11, -59),
            S( -6, -67),   S(-21, -96),   S(-13, -106),  S(-29, -92),   S(-16, -96),   S(  0, -101),  S( -1, -98),   S(-31, -73),
            S(  3, -89),   S(-15, -119),  S( -8, -137),  S(-13, -111),  S(  5, -98),   S( 11, -103),  S(  6, -92),   S( -1, -33),
            S( 22, -73),   S(  1, -79),   S(  7, -109),  S(  5, -135),  S( 29, -127),  S( 16, -117),  S( 21, -75),   S( -4, -76),
            S( 21, -58),   S( -4, -97),   S( -9, -120),  S(-27, -138),  S( 17, -100),  S( 12, -47),   S( 13, -57),   S(  2, -50),
            S( -6, -64),   S(-20, -67),   S( -2, -95),   S(-11, -98),   S( -5, -49),   S(  3, -20),   S(  0, -56),   S(-11, -63),

            /* rooks: bucket 15 */
            S(-10, -29),   S(-20, -89),   S( -7, -47),   S(-47, -119),  S(-14, -49),   S(-12, -22),   S( -5, -37),   S( -2, -59),
            S(-14, -47),   S(-38, -94),   S( -5, -37),   S(-15, -85),   S(-16, -81),   S( -1, -13),   S(-15, -60),   S( 17,  25),
            S(-11, -50),   S( -9, -66),   S(-25, -121),  S( -2, -87),   S( -7, -91),   S( -9, -72),   S(  3, -16),   S( -2, -30),
            S(-16, -84),   S( -7, -90),   S(-15, -87),   S( -3, -65),   S( -3, -85),   S( 18, -41),   S(-13, -72),   S( -4, -33),
            S( 14, -58),   S(-14, -81),   S( 13, -87),   S( -3, -109),  S( 23, -91),   S(  3, -106),  S( 10, -66),   S( -7, -46),
            S( 23, -41),   S( 17, -54),   S(  4, -84),   S( 18, -132),  S(  6, -121),  S( 46, -67),   S( 17, -69),   S(-11, -35),
            S( 23, -16),   S( 20, -35),   S(  9, -75),   S( 21, -88),   S( 18, -80),   S( 32, -17),   S( 19, -39),   S(  8,  14),
            S(  1, -36),   S(-10, -61),   S(  6, -65),   S( 10, -58),   S(  5, -105),  S(  1, -77),   S(  0, -38),   S( -9, -26),

            /* queens: bucket 0 */
            S(-47, -60),   S(-18, -71),   S( 31, -90),   S( 17, -22),   S( 37, -45),   S( 34,  -6),   S(  6,  21),   S(  6, -18),
            S(-28, -30),   S( 39, -58),   S( 36, -58),   S( 43, -50),   S( 12,  -3),   S( 40, -17),   S( 16,   9),   S(  9,  31),
            S( 26, -32),   S( 12,  52),   S( 12,  16),   S(  9,  25),   S(  7,  -1),   S(  9,  17),   S( 37,  32),   S(  2,  41),
            S(  9,  41),   S( 32,  10),   S( 23,  24),   S(  2,  -6),   S( 11,  -1),   S( 24,   9),   S( 43,  -9),   S( 11,  25),
            S( 60,  -3),   S( 32,  25),   S( 26,  -1),   S(  8,   9),   S(  6,  -8),   S(  6, -23),   S( 32, -34),   S( 16, -35),
            S( 21,  30),   S( 59,  15),   S( 22,  30),   S( 23,  12),   S( 51, -22),   S( -2,  19),   S( 35,  -8),   S( 43, -57),
            S( 71,  36),   S( 76,   4),   S( 43,  11),   S( 15,  17),   S( 20,   8),   S(  9,  -4),   S( 60,  29),   S( 35,  13),
            S( 68,  -2),   S( 13,  63),   S( 18,  30),   S( 63, -17),   S( 82, -40),   S( 36, -33),   S( 20, -21),   S( 91,  -5),

            /* queens: bucket 1 */
            S( 16, -31),   S(-47, -21),   S(-47,   0),   S( -4, -66),   S( 31, -14),   S(  7, -28),   S( 37, -39),   S( -9,  49),
            S( 18, -69),   S( 10, -57),   S( 44, -72),   S( 32,  23),   S( 21, -11),   S( 42, -25),   S( 25,  37),   S(  3,  52),
            S(-36,  73),   S( 27,   2),   S( 44, -10),   S( 19,  18),   S( 38,  18),   S( 34,  33),   S( 61,  14),   S( 18,  72),
            S( 35, -13),   S( 14,  24),   S( 19,  48),   S( 39,  33),   S(  9,  34),   S( 46,  26),   S( 30,  34),   S( 37,  18),
            S( 15,  17),   S( -3,  84),   S( -3,  80),   S(-35,  88),   S( 36,  37),   S( 18,  28),   S( 10,  51),   S( 27,  20),
            S( 29,   7),   S( 15,  68),   S( 83,  21),   S(  3,  63),   S( -2,  93),   S( 31,  39),   S(  9,  51),   S( 37,  66),
            S( 22,  31),   S(-13,  76),   S( 23,  87),   S( 18,  64),   S( 33,  59),   S( 31,  24),   S( 16,  83),   S(-35,  97),
            S(  5,  70),   S( 58,  45),   S( 71,  47),   S( 31,  53),   S( 25,  55),   S( 34,  21),   S( 93,  19),   S( 25,  43),

            /* queens: bucket 2 */
            S( 36,  57),   S( 53,  25),   S( 45, -14),   S( 13,   4),   S( -7,  55),   S(-30,  47),   S(  7, -32),   S( -5,  50),
            S( 52,  32),   S( 55,  38),   S( 44,  14),   S( 60,   2),   S( 32,  12),   S( 48,  20),   S( 79, -56),   S( 68,  -3),
            S( 28,  36),   S( 44,  35),   S( 38,  47),   S( 27,  70),   S( 35,  68),   S( 52,  83),   S( 54,  55),   S( 30,  75),
            S( 42,  67),   S( 22,  60),   S( 29,  75),   S( 25,  79),   S(  9,  94),   S( 44,  99),   S( 48,  70),   S( 30,  85),
            S( 44,  22),   S( 34,  82),   S( -6,  70),   S(  4, 105),   S( -4, 132),   S( -3, 111),   S( 21, 114),   S( 30, 111),
            S( 34,  48),   S( 46,  85),   S(  3, 111),   S( 22,  99),   S( 21, 115),   S( 11, 112),   S( 21, 139),   S( 29,  99),
            S( -1,  94),   S(-17, 121),   S(  4,  92),   S( 31, 104),   S( -5, 133),   S( 37, 106),   S( 49,  78),   S(-16, 130),
            S(  4, 129),   S( 39,  74),   S( 44,  66),   S( 41,  89),   S( 30, 102),   S( 57,  73),   S( 39,  60),   S( -3, 111),

            /* queens: bucket 3 */
            S( 75, 167),   S( 80, 126),   S( 60, 144),   S( 49, 127),   S( 70,  98),   S( 54,  70),   S(  2,  83),   S( 23,  96),
            S( 73, 171),   S( 73, 161),   S( 46, 168),   S( 56, 143),   S( 48, 134),   S( 76, 103),   S( 85,  17),   S(  5, 107),
            S( 44, 157),   S( 55, 160),   S( 65, 130),   S( 52, 127),   S( 60, 132),   S( 61, 160),   S( 75, 146),   S( 60,  98),
            S( 54, 170),   S( 60, 132),   S( 58, 147),   S( 46, 134),   S( 44, 132),   S( 61, 163),   S( 69, 162),   S( 39, 184),
            S( 71, 162),   S( 62, 138),   S( 34, 149),   S( 38, 147),   S( 38, 164),   S( 16, 199),   S( 38, 218),   S( 54, 218),
            S( 46, 167),   S( 60, 168),   S( 56, 145),   S( 30, 178),   S( 51, 187),   S( 48, 186),   S( 66, 194),   S( 45, 223),
            S( 57, 189),   S( 62, 179),   S( 67, 158),   S( 84, 150),   S( 57, 167),   S( 65, 172),   S(100, 175),   S(151, 112),
            S(115, 132),   S( 96, 128),   S( 92, 155),   S(107, 123),   S( 88, 141),   S(123, 108),   S(134, 122),   S(135, 116),

            /* queens: bucket 4 */
            S(-52, -75),   S( 12, -20),   S(-29, -47),   S( 30, -14),   S( -8,  -4),   S( 53,   4),   S(-49,  13),   S(-10,   1),
            S(-50, -38),   S(-17, -24),   S( -9, -18),   S(-55,  -3),   S( 23, -15),   S(-35,   5),   S(-65, -31),   S(-77, -17),
            S( -6, -11),   S( 12,  -4),   S( 75,   4),   S(-23,  55),   S( 78,  -1),   S( 33, -14),   S(-85, -23),   S(-34, -22),
            S( 26,   7),   S(  2,  42),   S( 10,  41),   S(-19,  22),   S( 61, -20),   S( -6,  -3),   S( 26,  -5),   S(-40, -15),
            S(-35,  -7),   S(  7,  16),   S( -6,  24),   S( 44,   5),   S( 55,  19),   S( 42, -53),   S( 28,  -8),   S( 41, -35),
            S(  1,  -1),   S( 20,  23),   S(-24,  16),   S( 11,  -1),   S(  7, -24),   S( 19,  -6),   S(-24, -46),   S(  3,  -2),
            S(-16,   8),   S(-25,  13),   S(-14,   2),   S( 15,   6),   S( 21,   9),   S(-22,   9),   S(-52, -90),   S(  8,  -2),
            S(-31, -61),   S( 11,  27),   S( 55,  -1),   S( 39,  53),   S( 11,  27),   S(-32,   3),   S(  5, -12),   S(  0, -35),

            /* queens: bucket 5 */
            S(-42, -46),   S(-60,   0),   S(-32, -77),   S(-16, -38),   S(-34, -39),   S( -7, -28),   S( 23,  11),   S(-26,   8),
            S(-41, -10),   S(-110, -15),  S(-65, -30),   S(-40,  32),   S( 25,  -4),   S(-18,  19),   S(-35,   4),   S(-49,  17),
            S(-29,  16),   S(-23,  19),   S(-87,  29),   S(-34,  30),   S( 19,  72),   S(  3,  54),   S( -6,  18),   S( 37,   9),
            S(-86,   8),   S(-41,  27),   S( -8,  81),   S(-17,  66),   S( 23,  68),   S( 18,  63),   S(-42, -11),   S(-16,  57),
            S(-33,   3),   S(-42,  12),   S( -3,  74),   S(-21,  71),   S( 23,  56),   S(-22,   4),   S( 31,  21),   S(-28, -11),
            S(-45,  32),   S(  0,  48),   S(-35,  70),   S(-17,  63),   S( 38,  56),   S(-14,  41),   S( -7,  -6),   S(-50, -26),
            S( 16,  41),   S(-24,  34),   S(-15,  49),   S( 33,  62),   S( 26,  68),   S( 34,  50),   S( -8, -16),   S( -7,  32),
            S( 30,  25),   S( 14,  26),   S( 25,  87),   S(  1,  68),   S( 24,  35),   S( -7,  17),   S(-16,  -6),   S( 23,  -4),

            /* queens: bucket 6 */
            S(-51,  -3),   S(-59,   1),   S(-38,   3),   S(-61, -25),   S(-78,  31),   S(-21, -30),   S(-17, -32),   S( 10,  42),
            S(-47,  57),   S(-48,  65),   S(-32,  41),   S(-46,  66),   S(-57,  69),   S(-87,  53),   S(-69,  62),   S( 46,  38),
            S(-17,  36),   S(-68,  11),   S(-67,  56),   S(-111, 134),  S(-48,  97),   S(-32,  86),   S(-65,  27),   S( 40,   8),
            S(-22,  54),   S( 13,  41),   S(-31,  82),   S(-27, 101),   S( 38,  69),   S( 63,  66),   S(-13, 131),   S( -9,  49),
            S(-98, 100),   S(-31,  33),   S( 21,  33),   S( 34,  43),   S( 17,  78),   S( 23,  96),   S( -4,  65),   S(-14,  70),
            S(-26,  67),   S( -6,  35),   S( -6,  30),   S( 61,  40),   S( -4,  67),   S( 79,  62),   S( 12,  52),   S(-34,  56),
            S(-16,  63),   S(  7,  60),   S( 16,  53),   S( 16,  63),   S( 36,  53),   S( -7,  84),   S( -5,  31),   S(-29,   4),
            S(-10,  63),   S( 18,  24),   S( 36,  32),   S( 94,  85),   S( 38,  69),   S( 25,  70),   S(  6,  21),   S( 17,  55),

            /* queens: bucket 7 */
            S( -8, -11),   S(-21,   6),   S(-28,  14),   S(-61,  25),   S(  2,  25),   S(-44,   6),   S( -9,  13),   S( -9, -56),
            S(-35,  26),   S(-49,  24),   S(-50,  43),   S(-33,  50),   S(-16,  25),   S(-62,  72),   S(-66,  54),   S(-20, -11),
            S(-49,  19),   S(-61,  55),   S(-14,  21),   S( 19, -17),   S(  7,  41),   S(-10,  51),   S(-51,  33),   S(-44,  10),
            S(-99,  38),   S( -9,  14),   S( 17,  -1),   S( 28, -24),   S( 32,   2),   S(-27,  81),   S( 13,  65),   S(-11,  52),
            S(-31,  44),   S(-57,  28),   S( 11,  -7),   S( 94, -43),   S( 69, -15),   S(135, -22),   S(-21,  64),   S( 16,  26),
            S(-20,  36),   S( -5,  20),   S( 10, -18),   S( 87, -42),   S( 56,   5),   S(128, -31),   S( 61,  -9),   S( -6,  37),
            S(-20,  39),   S( 19,   2),   S( 34, -12),   S( 27,   6),   S( 72,   5),   S(102,  11),   S( 82,  -3),   S( 64,  -1),
            S( 47,  13),   S(  5,  22),   S( 24,  24),   S( 56,   7),   S( 71,  41),   S( 49,  29),   S( 55,  21),   S( 51,   0),

            /* queens: bucket 8 */
            S(-10, -28),   S(-19, -29),   S(-34, -25),   S(-23, -27),   S(-36, -51),   S(-15, -38),   S(-29, -26),   S(-13, -19),
            S(-23, -43),   S(-18, -10),   S( 18,  18),   S(-48, -46),   S(-22, -30),   S(-37, -58),   S( -9, -21),   S(-28, -51),
            S(  1, -18),   S(-17, -25),   S(-21, -54),   S(-38, -56),   S(-11, -11),   S(-23, -28),   S(-12, -31),   S(-45, -89),
            S(-19, -34),   S(  6,  15),   S(-26,  -9),   S(-22, -19),   S(-40, -48),   S(-36, -56),   S( -7, -39),   S(-22, -44),
            S( 14,  31),   S(-16,   3),   S(  1,  18),   S(-19, -26),   S( -1,   1),   S( -9, -18),   S(-23, -40),   S(-17, -39),
            S( 11,  30),   S( 10,  35),   S( -5,  27),   S( 43,  -2),   S(-31, -62),   S(-16, -34),   S(-24, -38),   S( 11,  35),
            S( -9, -11),   S(-27, -42),   S( 16,  27),   S( -3,  -3),   S(-17, -46),   S(-30, -66),   S(-31, -73),   S(-18, -32),
            S(-37, -110),  S( -4, -21),   S(-27, -63),   S(-45, -64),   S(-18, -33),   S(-10, -30),   S(-10, -41),   S(-15, -22),

            /* queens: bucket 9 */
            S(-15, -31),   S(-34, -78),   S(-43, -82),   S(-54, -65),   S(-62, -126),  S(-29, -57),   S(-37, -75),   S(-14, -29),
            S(  3, -20),   S(-31, -69),   S(-39, -46),   S(-51, -57),   S(-42, -93),   S(-14, -28),   S(-22, -52),   S(-18, -49),
            S( -1, -13),   S(-27, -39),   S(-23, -40),   S(-73, -66),   S(-30, -41),   S(-47, -80),   S(-24, -83),   S( -5, -37),
            S(-41, -62),   S(-33, -42),   S(-11,   0),   S(-28, -20),   S( -8, -21),   S(-22, -46),   S(-48, -73),   S(-16, -39),
            S(-10,  -5),   S(-12,   1),   S(-10,   1),   S(-26,  -9),   S(-18, -28),   S(-14,  -2),   S( -4, -30),   S(-50, -26),
            S(-31, -41),   S(-29,   1),   S(  4,  47),   S(-29,  23),   S( -9,  -5),   S(-27, -64),   S(-18, -56),   S(-19, -40),
            S(-34, -72),   S(-13, -26),   S( 11,  12),   S(-27,  15),   S(-24, -29),   S( -9, -60),   S(-22, -80),   S(-40, -92),
            S(-45, -83),   S( -2, -35),   S(-35, -68),   S( -6, -17),   S(-32, -72),   S(-22, -46),   S(-16, -52),   S(-30, -62),

            /* queens: bucket 10 */
            S(-24, -44),   S(-27, -75),   S(-24, -50),   S(-77, -108),  S(-35, -60),   S(-13, -31),   S(-21, -82),   S(-10, -38),
            S(-25, -53),   S(-24, -56),   S(-45, -103),  S(-61, -68),   S(-20, -18),   S(-29, -53),   S( -5, -29),   S(-15, -47),
            S(  0, -34),   S(-46, -56),   S(-51, -93),   S(-26, -50),   S(-78, -61),   S(-55, -51),   S(-16, -19),   S(-13, -25),
            S(-33, -27),   S(-26, -22),   S(-57, -61),   S(-46, -59),   S(-14, -20),   S(-49, -58),   S(-21, -50),   S(-14, -58),
            S(-17, -39),   S(-27, -55),   S(-38, -65),   S(-16, -28),   S(-26, -37),   S( 34,  32),   S( -1,  -1),   S(-27, -20),
            S(  5,  -5),   S(-28, -60),   S(-29, -14),   S(-48, -51),   S(-50, -27),   S( -4,  -5),   S(-27, -32),   S(-34, -70),
            S(-21, -44),   S(-38, -39),   S(-34, -58),   S(-42, -31),   S(-45, -55),   S(-17,  -8),   S(-29, -44),   S(-26, -57),
            S(-24, -29),   S(-12, -39),   S(-19, -11),   S(-15, -47),   S(-26, -43),   S(-27, -57),   S(-19, -23),   S(-27, -62),

            /* queens: bucket 11 */
            S(-13, -12),   S(-33, -52),   S(-37, -34),   S(-36, -70),   S(-48, -62),   S(-45, -76),   S(-14, -26),   S(-20, -47),
            S(-31, -58),   S(-27, -78),   S(-50, -59),   S(-37, -46),   S(-14,  -7),   S(-13, -43),   S(-25, -24),   S(-23, -30),
            S(-20, -38),   S(-49, -110),  S(-40, -71),   S(-46, -97),   S(-24, -60),   S(-22, -57),   S(-24,   3),   S( -9,   0),
            S(-27, -45),   S(-40, -77),   S(-39, -93),   S( -9, -53),   S(-11, -52),   S(-12, -38),   S(-16,  24),   S(-46, -71),
            S(-13, -36),   S(-24, -67),   S(  5, -13),   S(-38, -44),   S(  9, -54),   S( 12, -38),   S( 21,  14),   S(  2,   9),
            S(-18, -67),   S(-43, -52),   S(-36, -58),   S( -8, -31),   S( -6, -39),   S( 40,  -7),   S( 16,  43),   S( -7,   3),
            S(-41, -99),   S(-17, -49),   S(-28, -36),   S(-18, -37),   S(-13, -30),   S( -3, -39),   S( 48,  51),   S( 12,   3),
            S(-26, -18),   S(-33, -87),   S( -1, -16),   S(-18, -60),   S( -2, -23),   S(-14, -41),   S(-25, -44),   S(-17, -67),

            /* queens: bucket 12 */
            S(  0,   2),   S(  4,   8),   S( -4,   1),   S(-20, -40),   S(-13, -29),   S(  2, -11),   S( -5, -21),   S(-11, -18),
            S(  3,   5),   S( -2, -12),   S(-14, -26),   S(-24, -52),   S(-11, -35),   S(-10, -23),   S( -6, -14),   S(-23, -46),
            S( 12,  16),   S( 15,   8),   S( -6,  -8),   S(-12, -58),   S(-35, -65),   S(-19, -17),   S(  8,   4),   S( -7, -15),
            S(  0,  -3),   S(-17, -43),   S( -6, -13),   S(-21, -47),   S(-27, -49),   S(-19, -36),   S( -5, -19),   S(-23, -41),
            S( -8,   3),   S(-13,  -8),   S(-14, -16),   S(-15, -43),   S(-28, -68),   S( -5, -29),   S(-23, -59),   S(-16, -37),
            S(  6,  12),   S(  0,  13),   S( 24,  14),   S(-25, -47),   S( -3,  -8),   S(-13, -38),   S( -1,  -3),   S( -4, -13),
            S( 10,  14),   S(-12, -21),   S(-18, -17),   S(  0, -23),   S(-33, -66),   S(-23, -53),   S( 10,   9),   S(  2,   8),
            S(-17, -41),   S( -1,  -8),   S(-19, -21),   S(-22, -22),   S(-13, -10),   S(  9,   8),   S(-13, -38),   S( 10,  25),

            /* queens: bucket 13 */
            S( -1,  -5),   S(-15, -44),   S(-18, -46),   S(-12, -41),   S(-31, -63),   S(-31, -66),   S(-13, -43),   S( -9, -19),
            S( -5,  -4),   S( -5, -22),   S(-11, -33),   S(-17, -57),   S(-21, -48),   S( -9, -29),   S(-22, -38),   S(-11, -28),
            S(-13, -41),   S(  2,  -9),   S(-24, -67),   S(-37, -64),   S(-25, -66),   S(-34, -70),   S(-16, -38),   S(-11, -28),
            S(  2,  -1),   S(-21, -64),   S(  7, -40),   S( -3, -29),   S(-20, -39),   S(-13, -25),   S(-13, -23),   S( 12,   4),
            S(  4,   7),   S(-19, -17),   S(-18, -57),   S(-17, -31),   S(-12, -33),   S(-17, -44),   S(-17, -55),   S(-12, -16),
            S( -1, -20),   S( 10,   7),   S(  8,  16),   S(  8,   2),   S( -9, -16),   S(-12, -44),   S( -8, -31),   S( -3, -33),
            S( -3,  -4),   S( 18,  32),   S( -1,   5),   S( 20,  33),   S(  2, -15),   S( -5, -22),   S( -4, -27),   S(  2,   7),
            S(-38, -74),   S(  3, -14),   S(-10,  -7),   S(-24, -48),   S( -4,  -9),   S(-12, -37),   S(  0,  -5),   S(-33, -70),

            /* queens: bucket 14 */
            S( -3, -13),   S(  1,  -1),   S( -2,  -9),   S(-20, -44),   S( -5, -22),   S( 11,  22),   S(-17, -54),   S(-16, -49),
            S(-19, -39),   S(  0, -26),   S(-23, -66),   S(-29, -67),   S(-14, -23),   S( -5, -26),   S( -3, -10),   S(-34, -75),
            S(-13, -54),   S(-20, -46),   S(-23, -43),   S(-19, -32),   S(-35, -84),   S(-27, -59),   S(-12, -31),   S(-17, -24),
            S(-17, -38),   S(-10, -29),   S(-26, -58),   S( -9, -52),   S(  1, -36),   S(-10, -49),   S(-14, -50),   S( -2, -15),
            S(  1,  -5),   S(  4,  -8),   S(-36, -74),   S( -8, -46),   S( -7, -60),   S( 20,   2),   S(-13, -56),   S(-14, -44),
            S(-18, -50),   S(-24, -62),   S(-17, -26),   S( -7, -22),   S(-25, -37),   S( -1,  12),   S( -2, -14),   S(  3, -17),
            S(  3,   6),   S(-11, -48),   S(  0, -12),   S( 15,  12),   S( 16,   4),   S( 17,  31),   S( 27,  47),   S( -8, -30),
            S( -8, -10),   S(  9,  17),   S(-22, -36),   S( -7, -25),   S(  5, -15),   S(  2,   2),   S(-18, -49),   S(-17, -48),

            /* queens: bucket 15 */
            S( -6, -29),   S( -4,  -9),   S( -6, -19),   S(-10, -29),   S( -4, -12),   S( -7, -38),   S(-10, -25),   S(  4,  -8),
            S( -7, -21),   S(-11, -32),   S( -5, -21),   S(-22, -41),   S( -4,  -6),   S(  7,  15),   S(-10, -36),   S(-12, -44),
            S( -2, -13),   S(-21, -49),   S( -3, -18),   S(-13, -36),   S(-26, -68),   S( -5, -27),   S(  7,   7),   S( -1,  -6),
            S( 18,  26),   S( -3, -10),   S(-18, -43),   S(-19, -72),   S(-18, -63),   S(-13, -14),   S(  1,  -5),   S(  1,  -7),
            S(-13, -33),   S( -4, -35),   S(-19, -54),   S(-15, -52),   S(-30, -69),   S(-17, -65),   S(  3, -10),   S(-20, -57),
            S(-10, -38),   S(-15, -39),   S(-17, -66),   S(-21, -64),   S(-24, -78),   S(  4, -11),   S(  5,  -1),   S( -4, -33),
            S( 12,  28),   S( -7, -39),   S( -2, -21),   S(  2,   2),   S( -7, -44),   S(  3, -24),   S(  4,  -4),   S( -1,   0),
            S(  3,   6),   S(-11, -40),   S(-23, -49),   S( 19,  20),   S(  1,  -5),   S( 17,  13),   S( -9, -22),   S(-16, -51),

            /* kings: bucket 0 */
            S(-41,  95),   S( -8, 103),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 16,  73),   S( 83,  77),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(-49,  48),   S(-80,  43),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 41,  23),   S( 42,  23),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-42,  46),   S(-70,  50),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 62,  22),   S( 51,  18),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 11,  76),   S(-37,  88),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 67,  72),   S( 10,  76),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-37, -24),   S( 31, -37),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-59,  -3),   S( 17,  -7),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  3, -52),   S(-41, -37),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 26, -35),   S( 24, -35),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0, -46),   S(-15, -43),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 39, -39),   S( 14, -30),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 42, -21),   S(-31, -19),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 43,   1),   S(-21,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-131, -31),  S( -6, -30),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-79, -27),   S( 44, -39),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( -5, -49),   S( 12, -65),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 41, -51),   S( 43, -47),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 36, -68),   S(  5, -54),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 54, -60),   S( 29, -50),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 11, -41),   S(-103, -43),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 57, -51),   S(  5, -50),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-54, -22),   S( 87, -37),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-106, -36),  S( -6,  -5),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(-15, -38),   S( 69, -48),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 73, -68),   S( -3, -67),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 43, -53),   S( 51, -61),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 56, -67),   S(-15, -45),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 54, -42),   S(-66, -34),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 41, -52),   S( -5, -55),

            #endregion

            /* enemy king piece square values */
            #region enemy king piece square values

            /* pawns: bucket 0 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-32,  14),   S(-50,  39),   S(  7,  -1),   S(-10,  44),   S( 13,   0),   S( 27,   6),   S( 45,  -8),   S( 32, -16),
            S(-28,   2),   S(-43,  28),   S(-13,  -1),   S( -4,  -2),   S( 13,   3),   S(  2,  13),   S( 38,   0),   S( 19,  10),
            S( -5,   4),   S(-14,  12),   S( 29, -13),   S( 20, -35),   S( 27, -27),   S( 12,  15),   S( 16,  22),   S( 46,  -7),
            S(  9,  19),   S( 23,  24),   S( 58, -16),   S( 59, -14),   S( 12,  28),   S( -6,  72),   S( 19,  61),   S( 81,  17),
            S(124, -15),   S( 80,  25),   S(113,  12),   S( 17,  61),   S( 40, 143),   S( 17,  83),   S( 21,  82),   S(142,  23),
            S(-116, -41),  S(-130,   4),  S(141, -98),   S( 50,  68),   S(104, 132),   S( 91, 108),   S(234,  15),   S(114,  58),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-62,  33),   S(-45,  30),   S(-26,  22),   S(-48,  42),   S(-27,  10),   S( -1,   5),   S( 12,  -1),   S( -2,  19),
            S(-53,  21),   S(-35,  14),   S(-29,   8),   S(-20,   5),   S(-18,   5),   S(-20,   4),   S( -1,  -2),   S(-16,  13),
            S(-32,  28),   S(-14,  25),   S(-24,  11),   S( 12, -18),   S(  7,  -1),   S(-15,  10),   S(-13,  18),   S(  0,  14),
            S(-10,  47),   S( 25,  19),   S(  7,  24),   S( 16,  37),   S( -1,  18),   S(-18,  38),   S(  5,  27),   S( 43,  26),
            S( 53,  14),   S( 79, -10),   S( 98,  25),   S(149,   5),   S( 63,  45),   S( 33,  48),   S(-27,  64),   S( 69,  45),
            S(278, -70),   S( 27,  42),   S( 40, -44),   S(-12, -30),   S(-10, -19),   S(-93, 118),   S( 33,  96),   S( 85, 133),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-62,  49),   S(-43,  39),   S(-33,  27),   S(-22,   4),   S(-43,  44),   S(-28,  27),   S( -5,   5),   S(-27,  26),
            S(-60,  40),   S(-41,  31),   S(-44,  23),   S(-36,  25),   S(-39,  22),   S(-40,  18),   S(-19,   2),   S(-45,  15),
            S(-29,  42),   S(-38,  46),   S(-17,  24),   S(-15,   9),   S(-24,  27),   S(-22,  16),   S(-26,  24),   S(-19,  10),
            S(-10,  67),   S(-24,  65),   S(-11,  46),   S(  7,  43),   S(-10,  42),   S(-10,  26),   S(  5,  27),   S( 45,   7),
            S(  9,  96),   S(-36,  84),   S( -7,  50),   S( 53,   0),   S(133,  20),   S(129,  20),   S(111,  -2),   S( 88, -11),
            S(-22, 190),   S( 24, 124),   S( -8,  67),   S( 15, -40),   S(-55, -63),   S(-70, -34),   S( 31,  17),   S(184, -55),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-14,  35),   S( -9,  33),   S( -9,  30),   S(-14,  14),   S(-19,  61),   S( 11,  41),   S(  8,  27),   S(-11,   5),
            S(-11,  37),   S(  5,  30),   S(-12,  23),   S(-11,  24),   S( -2,  23),   S(  8,  20),   S( -3,  24),   S(-29,  15),
            S( 10,  36),   S( -9,  56),   S(  4,  24),   S(  4,   0),   S( 17,  -6),   S( 15,   7),   S( -2,  19),   S( -7,   4),
            S( 20,  72),   S(  0,  85),   S(  9,  55),   S( 23,  29),   S( 20,  12),   S( 31,   4),   S( 12,  39),   S( 39,  11),
            S( 42, 101),   S(-32, 141),   S(-43, 169),   S(  5, 120),   S( 44,  70),   S(100,  22),   S(103,  18),   S(116,   1),
            S( 66,  96),   S( -6, 181),   S(-29, 239),   S( 14, 178),   S(-33, 132),   S(-10, -46),   S(-58, -55),   S(-123, -64),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 43,  -1),   S( -3,  11),   S(-16,   0),   S( 33, -22),   S(-46,  -6),   S(-27,  -4),   S(-72,  27),   S(-104,  42),
            S( 24, -11),   S(  2,  17),   S( 43, -35),   S( 14, -21),   S(-26, -32),   S(-19, -16),   S(-73,  12),   S(-103,  20),
            S( 44,  18),   S(123, -15),   S( 20,  -5),   S(-45, -19),   S(-56, -11),   S(-29,   0),   S(-90,  20),   S(-133,  37),
            S(-13, -40),   S( 24, -68),   S( 92, -35),   S(-49,  12),   S(-28,   0),   S(-64,  47),   S( 10,   8),   S(-44,  22),
            S( 74, -47),   S(-42, -49),   S( 17, -20),   S(119,   7),   S( 75,  79),   S( 24,  67),   S( 99,  -4),   S( 11,  54),
            S( 12, -26),   S( 27, -14),   S( 14, -58),   S( 20,  53),   S( 26,  67),   S( 60, 107),   S( 11,  67),   S(-67,  75),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-45,  34),   S(-15,  26),   S(-11,  21),   S( -3,   1),   S( 51, -15),   S(-10,   3),   S(-15,   6),   S(-68,  33),
            S(-47,  21),   S( 17,   3),   S( 16,  -1),   S( 28,  -2),   S(-20,  -4),   S(-14, -10),   S(-59,   7),   S(-94,  29),
            S(-14,  29),   S( 14,  25),   S( 56,  18),   S(  8,  30),   S( -7,  16),   S(-17,   4),   S(-18,   4),   S(-56,  22),
            S( 39,  20),   S( 34,   2),   S(  9, -23),   S(-67,  13),   S( 26, -19),   S( -1,   9),   S( 40,   3),   S(  3,  14),
            S( 70,  14),   S( 98, -53),   S(108, -50),   S(-46,   6),   S( 77, -13),   S( 59,  16),   S( 83,  12),   S( 37,  54),
            S(136,  14),   S(111, -17),   S( 61, -68),   S( 73, -79),   S(-26, -48),   S( 99,  12),   S( 11, 104),   S(113,  54),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-72,  31),   S(-38,   7),   S(-22,   7),   S(  4,  -7),   S(  6,  10),   S( 17,   7),   S( 13,  10),   S(  6,  17),
            S(-66,  24),   S(-32,   0),   S(-14, -12),   S( 55, -24),   S(  1,   3),   S(  0,   2),   S(  9,   1),   S( 11,  -4),
            S(-29,  20),   S(-21,  10),   S(-12,  10),   S( 13,   7),   S( 27,  18),   S( 38,   5),   S( 45,   2),   S( 36,  -5),
            S(  1,  34),   S( 16,  12),   S( 27,   1),   S( 27,   8),   S(-13, -24),   S( 33, -39),   S( 39, -10),   S(125, -32),
            S(116,  22),   S( 63,  -1),   S( 50,  12),   S(  3,  -2),   S( 63, -41),   S(-21, -10),   S(169, -59),   S(159, -14),
            S(189,  -3),   S(151,  24),   S(115,  -7),   S( 97, -69),   S( 61, -81),   S(103, -80),   S( 43, -18),   S(145,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-63,  13),   S(-54,  11),   S(-16,  -8),   S(-30,  30),   S( 40,  -4),   S( 58,  -6),   S( 68, -12),   S( 48,   4),
            S(-44,  10),   S(-80,  22),   S(-37, -10),   S(-30,   0),   S( 21, -15),   S( 56, -30),   S( 45,  -9),   S( 42,  -9),
            S(-52,  16),   S(-62,  20),   S(-14, -10),   S(-28, -15),   S( -9, -16),   S( 28, -14),   S( 51,   5),   S( 61,   3),
            S(  0,  22),   S(-64,  38),   S(-23,  19),   S( 11,   4),   S(  1, -15),   S( 58, -43),   S( 14, -43),   S( 15, -61),
            S( 90, -18),   S( 27,  43),   S( 89,  43),   S( 53,  59),   S( -7,  61),   S( 62, -47),   S( 34, -80),   S( 31, -54),
            S(157,  -8),   S(210,  11),   S(128,  50),   S( 49,  81),   S(118, -10),   S( 14, -48),   S( 30, -24),   S( 55, -109),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 59, -23),   S(-18, -23),   S( 87, -34),   S(-37, -41),   S( -3, -50),   S(  7, -40),   S( 60, -59),   S(-36, -21),
            S(-45, -59),   S(  3, -20),   S(-72, -57),   S(-75, -28),   S(-68, -43),   S(-36, -30),   S(-32, -26),   S(-81, -10),
            S(-83, -37),   S( 22, -43),   S( 13, -45),   S(-89, -44),   S(-57, -24),   S( 20, -39),   S(-59, -15),   S(-106, -13),
            S(-47,  12),   S( -5, -16),   S( 51, -35),   S(-18, -12),   S( 50, -19),   S(  3,   3),   S( 32,  -8),   S(-48,  -3),
            S(  6,  19),   S( 10, -12),   S( 38,  23),   S( 52,  38),   S( 31,  79),   S( 21,  68),   S(  6,  22),   S(-84,  44),
            S( 16,  21),   S( 11,   5),   S( 71,  42),   S( 55,  53),   S( 60,  43),   S( 51,  90),   S( 37,  72),   S(-39,  27),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -1,  -6),   S( 41, -27),   S( 24, -20),   S( 22,  10),   S( -4, -13),   S( 78, -52),   S( 60, -55),   S(-50, -19),
            S(-35, -17),   S( 10, -62),   S(-37, -41),   S(-58, -21),   S(-48, -42),   S(-33, -36),   S(-36, -32),   S(-54, -24),
            S(-105,   6),  S( -7, -41),   S( -6, -56),   S(-78, -28),   S( 22, -42),   S(-23, -46),   S(-64, -31),   S(-97,  -5),
            S(-79,  24),   S(-33, -39),   S( 31, -58),   S( 28, -29),   S( 18, -50),   S(-30, -21),   S(-16, -20),   S( 40, -25),
            S(-19,   6),   S(-22, -42),   S(  9, -16),   S( 84, -16),   S( 20,  25),   S( 57,  15),   S(-54,  24),   S(-41,  23),
            S( -2,  10),   S( 76,  -4),   S( 19, -14),   S( 59, -10),   S( 40,  -8),   S( 37,  33),   S( 21,   2),   S( 18,  31),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-17, -47),   S(-60, -32),   S( -2, -20),   S(  9, -42),   S( 68, -42),   S(189, -65),   S( 92, -40),   S( 80, -51),
            S(-126, -13),  S(-130, -18),  S(-39, -46),   S(  3, -58),   S(-58, -51),   S(-24, -43),   S( 47, -47),   S( -2, -41),
            S(-110, -16),  S(-110, -18),  S(-57, -27),   S( 15, -51),   S( -4, -52),   S( 19, -67),   S(-23, -49),   S( 32, -47),
            S(-12, -34),   S( -2, -30),   S(-17, -31),   S( 29, -50),   S( 38, -62),   S(-18, -55),   S( 19, -51),   S(-10, -31),
            S(-30, -10),   S(-15, -14),   S( 59, -35),   S( 71, -37),   S( 42,  -3),   S( 78, -39),   S(-24, -50),   S( 95, -25),
            S(-50,  -3),   S( 35, -34),   S( 42, -17),   S( 26, -59),   S( 63,  -2),   S( 41, -32),   S(-18, -37),   S(-15, -16),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-86, -14),   S(-79, -16),   S(-18, -33),   S( 26,  28),   S(  9, -22),   S(125, -36),   S(147, -91),   S( 78, -44),
            S(-60, -26),   S(-93, -24),   S(-38, -41),   S( 31, -67),   S( 12, -57),   S(-34, -51),   S(-45, -26),   S( -4, -65),
            S(-87, -23),   S(-77, -27),   S(-11, -36),   S(-25, -54),   S(-86, -26),   S( 18, -46),   S(-60, -44),   S( 26, -57),
            S( 14, -25),   S(-50, -12),   S( 29, -29),   S( 85, -23),   S( 25, -23),   S( 15, -60),   S(-64,  -9),   S(-85, -19),
            S(  5, -35),   S( -5,   5),   S( 16,  48),   S( 66,  31),   S( 82,  21),   S( 42,  14),   S(-13, -53),   S( 37,  -2),
            S(-26,   0),   S( 13,   8),   S( 43,  33),   S( 44,  11),   S( 41,   7),   S( 49,  10),   S( 19,   1),   S( 49,   9),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-51, -106),  S( -3,  -5),   S(-22, -36),   S(-19, -37),   S(  7, -15),   S(-20, -51),   S( -1, -28),   S( 23, -48),
            S(  7, -56),   S(-64,  -3),   S(-88, -71),   S(-75, -79),   S(-26, -42),   S( -7, -15),   S(  6, -45),   S(-29, -27),
            S(-33, -29),   S( 48, -75),   S(-22, -81),   S(-46, -99),   S( -5, -56),   S( 35, -26),   S(-97, -32),   S(-95, -33),
            S(-25,  11),   S( 30, -34),   S(-21, -37),   S(-32,  -4),   S( 11,  43),   S(-25,  49),   S(-44,   1),   S(-69, -16),
            S(  0, -34),   S( -6,  -4),   S(-26, -40),   S(  7,  46),   S( 42,  70),   S( 28,  70),   S(-48,  16),   S(-38,  60),
            S( 18,   2),   S(  7, -49),   S(  8,   5),   S( 18,  54),   S( 10,  75),   S(  9,  59),   S(-73, -57),   S(-49,  25),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-44, -48),   S(-10, -85),   S( -3, -40),   S( -7, -39),   S( -7, -77),   S( 84, -92),   S( 10, -48),   S( 52, -68),
            S(-84, -35),   S(-19, -86),   S(-50, -48),   S( -8, -37),   S(-48, -72),   S(-60, -36),   S(  6, -70),   S(-27, -38),
            S(-56, -19),   S(-28, -42),   S(-18, -46),   S(-34, -46),   S( -6, -67),   S(-25, -37),   S(-43, -37),   S(-60, -13),
            S(-90,  26),   S(-15, -41),   S(  1, -53),   S(-15,  42),   S(-23, -15),   S(-46,   0),   S(-12, -13),   S(-47,   9),
            S(-37, -25),   S( -5,  18),   S(-16, -33),   S( 44,  32),   S( 20,  26),   S(  9,  78),   S( -4,  36),   S(-59,  88),
            S( -9,  15),   S( 19,  21),   S(  1,  -1),   S( 20, -23),   S( 29,  45),   S( -8, -15),   S( -7,  70),   S(-42,  32),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-19, -66),   S( 18, -64),   S( -6, -56),   S(-11, -16),   S(-12, -57),   S( 66, -43),   S( 27, -58),   S( -9, -59),
            S(-71, -39),   S(-95, -51),   S( -1, -56),   S(-30, -100),  S(-12, -46),   S( 19, -51),   S( -9, -59),   S(-36, -46),
            S(-65, -28),   S(-56, -32),   S( -1, -50),   S( 14, -66),   S( 20, -67),   S(-21, -52),   S(-26, -48),   S(-31, -13),
            S(-31, -20),   S(-17, -29),   S(-28, -14),   S(-26, -81),   S(-12, -49),   S(-15, -13),   S( 26,   7),   S(-43,  13),
            S(-19, -19),   S( 14,  -5),   S( -6, -26),   S(-14, -12),   S( 12,  19),   S( 12, -21),   S(  1,   5),   S(-35,  31),
            S(-44,   0),   S( 18, -28),   S(  7,  17),   S( 25,  43),   S( 10,  24),   S(-18, -19),   S( 27,  45),   S( 14,  46),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-13, -28),   S( -6, -54),   S( -5, -49),   S(-11, -14),   S(-17, -35),   S(-48, -49),   S(-23, -22),   S( -5, -122),
            S(-27, -15),   S(-20, -44),   S(-22, -37),   S(-12, -52),   S( 21, -47),   S(-62, -21),   S(-75,   4),   S(-15, -71),
            S(-79, -46),   S( -8, -54),   S(-36, -42),   S( 37, -62),   S(-54, -32),   S(-12, -74),   S( 23, -19),   S(-16,  -3),
            S(-47, -16),   S(-57,   4),   S( 60,  30),   S(-10,  -8),   S( 35,  17),   S(  5, -38),   S( -6,  -2),   S( 14,  17),
            S(-36,  34),   S(-33, -32),   S(  2,  39),   S(  0,  40),   S(  7,  19),   S( 11,   7),   S( 32,  35),   S(  9,  32),
            S(-73,  -3),   S(-61, -36),   S(-23,  33),   S(  0,   1),   S( -4,  18),   S( 19,  46),   S( 28,  23),   S( 18,  33),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: bucket 0 */
            S(-52,   5),   S(-19, -30),   S( 23, -74),   S(-32,  24),   S(-11, -28),   S(-28,  16),   S(-19,   9),   S(-131, -61),
            S(-23, -42),   S(-28,  26),   S(-14,  -8),   S(  4,  -7),   S( -7,  29),   S(  7,  12),   S(-29, -16),   S(-32,  -4),
            S( -5, -54),   S( 18,   5),   S(-12,  53),   S( 21,  40),   S(-22,  39),   S(  3,  22),   S(-17,  31),   S(-34, -16),
            S(-20,  67),   S( 16,  76),   S( 22,  49),   S( 47,  34),   S( 10,  63),   S( 15,  55),   S( 49,   1),   S(-19,  37),
            S( 61,  24),   S( -9,  79),   S( 62,  60),   S( 40,  38),   S( 57,  65),   S( 34,  13),   S(  5,  33),   S( 21,   2),
            S( 80, -23),   S(-14,  64),   S(103,  22),   S( 89,  18),   S( 64,  52),   S(-22,  71),   S( 33,  36),   S( 58,   4),
            S(  3, -43),   S( -7,  -8),   S( 47, -21),   S( 59,  68),   S( 18,  50),   S(  0,  69),   S(-33,  56),   S( -8,   3),
            S(-146, -226), S(-32, -54),   S(  3, -47),   S( 18,  82),   S(-27,  55),   S( 31,  56),   S(-54,  26),   S( -5, -11),

            /* knights: bucket 1 */
            S( 47, -43),   S(-68,  59),   S(-25,  29),   S( -4,  16),   S(-27,  53),   S(-37,   2),   S(-46,  36),   S( 47,   8),
            S(-62,  26),   S(-65,  67),   S(-20,  41),   S(-18,  34),   S(-25,  35),   S(-19,  50),   S(-25,  43),   S(-17,   1),
            S(-55,  55),   S(-18,  35),   S(-35,  47),   S(-30,  75),   S(-41,  62),   S(-20,  45),   S(-46,  69),   S(-55,  60),
            S(-19,  61),   S(  6,  55),   S(-13,  80),   S(-28,  87),   S(-24,  75),   S(-13,  72),   S(-35,  62),   S(-35,  52),
            S( 26,  33),   S(-14,  34),   S(  5,  87),   S(  6,  65),   S( 31,  50),   S(  1,  62),   S( -8,  66),   S(-17,  88),
            S( 45,  46),   S(103, -13),   S( 75,  28),   S( 91,  31),   S(  7,  68),   S(-19,  90),   S( 17,  72),   S(-38,  77),
            S( 21,  37),   S( 21,  10),   S( -1,   1),   S( 70,  53),   S(-35,  32),   S(-43,  80),   S( -6,  90),   S(-56,  81),
            S(-191,   7),  S( 13,  19),   S(  6,  -8),   S(-55,  14),   S(  4,  40),   S( 54,  58),   S( -4,  61),   S(-111,  40),

            /* knights: bucket 2 */
            S(-97,  45),   S(-64,  73),   S(-41,  45),   S(-30,  57),   S(-39,  40),   S(-68,  36),   S(-45,  40),   S(-38, -10),
            S(-35,  36),   S( -7,  51),   S(-58,  66),   S(-29,  52),   S(-42,  70),   S(-52,  51),   S(  3,  24),   S(-38,  15),
            S(-68,  90),   S(-45,  82),   S(-44,  78),   S(-49, 111),   S(-54, 101),   S(-48,  65),   S(-47,  57),   S(-50,  30),
            S(-41, 103),   S(-29, 106),   S(-36, 128),   S(-49, 129),   S(-53, 107),   S(-21,  98),   S(-16,  76),   S(-34,  67),
            S(-31, 104),   S(-33, 109),   S(-18, 104),   S(-16,  85),   S(-33, 110),   S(  1, 102),   S(-43, 105),   S( 10,  36),
            S(-100, 131),  S(-34,  94),   S(-50, 127),   S( -7,  78),   S( 37,  70),   S( 95,  45),   S( 32,  65),   S( -8,  62),
            S(-41,  97),   S(-110, 132),  S(  2,  67),   S( 28,  49),   S( 23,  29),   S(-17,  50),   S(-18,  63),   S(-27,  74),
            S(-67,  13),   S( 31,  94),   S(-45,  89),   S( 28,  30),   S(-16,  46),   S(-106,  -6),  S(100,   6),   S(-122,  44),

            /* knights: bucket 3 */
            S(-80, 103),   S(-36,  21),   S(-30,  58),   S(-12,  41),   S(-20,  42),   S(-18,  25),   S(-29,  38),   S(-46,  21),
            S(-33,  41),   S(-26,  76),   S(-19,  76),   S(-14,  69),   S( -8,  66),   S(-11,  57),   S( -2,  43),   S( 17,   7),
            S(-32,  62),   S(-25,  85),   S(-12,  88),   S( -4, 106),   S(-10, 105),   S(-17,  91),   S(-14,  86),   S(-15,  21),
            S(-15,  74),   S( -4, 107),   S( -4, 124),   S(-12, 131),   S( -9, 137),   S( 10, 119),   S( 13, 126),   S( -6,  87),
            S(-11, 117),   S(-16, 108),   S( 14, 105),   S( 15, 134),   S(  0, 138),   S( 15, 154),   S(-29, 160),   S( 34, 142),
            S(-64, 139),   S(-17, 114),   S(-17, 136),   S(-16, 145),   S( 25, 132),   S( 55, 151),   S( 37, 157),   S(-12, 148),
            S(-47, 111),   S(-62, 117),   S(-44, 136),   S( -7, 124),   S(-15, 141),   S( 53,  97),   S( 49, -30),   S( 55,  30),
            S(-224, 114),  S(-94, 142),   S(-86, 178),   S( 22, 113),   S( 55, 141),   S(-65,  75),   S(-32, -27),   S(-111, -117),

            /* knights: bucket 4 */
            S(-17,  -4),   S( 24, -32),   S(-48,  31),   S(-75,  -3),   S(  1,  11),   S( 36, -57),   S( 23, -12),   S(-30, -35),
            S( 48,  -2),   S( 47,  16),   S( -3,   1),   S( 15,   3),   S(-23,  19),   S( 33, -21),   S(-27, -45),   S(-28, -92),
            S( 16,  24),   S(-23,  23),   S( 60,  27),   S(150, -22),   S(113, -14),   S( 63, -27),   S( 48,  -8),   S( 51, -11),
            S(  8, -30),   S( 43,   6),   S( 58,  -9),   S( 84,   3),   S( 76, -12),   S( 78, -23),   S( -6,  -5),   S(  0, -10),
            S(-86, -62),   S( 16,   6),   S( 24,  19),   S(108, -13),   S(151, -31),   S(  8,  30),   S( -2, -10),   S( 18,  55),
            S(-18, -91),   S(-20, -26),   S( 54,  -1),   S( 71,  28),   S(-13,  28),   S(-13,  10),   S(-32,  28),   S(-25,   3),
            S(-20, -46),   S(-20, -35),   S( 15,  -3),   S( 21,  -4),   S( 25,  25),   S( 13,  38),   S(-34,  34),   S(-37, -47),
            S( 20,  29),   S(-10, -33),   S(  0,  -7),   S( 24,  27),   S( -3, -56),   S( -2,  16),   S( -3,  13),   S(-34, -20),

            /* knights: bucket 5 */
            S( 23, -31),   S( 48,  -5),   S( 17,  38),   S(-50,  27),   S(-28,  39),   S( 36,  11),   S( -4,  15),   S( -5, -12),
            S( 18,  36),   S( 33,  35),   S( 39,  12),   S(  7,  18),   S( 56,   7),   S( 17,  17),   S( 32,  18),   S(-26,  44),
            S( 18,  36),   S( 81,  -1),   S( 47,  33),   S( 41,  35),   S( 83,  19),   S( 20,  26),   S( 30,  17),   S( 19,  23),
            S( 33,  23),   S( 77,  -7),   S( 94,   4),   S(137, -16),   S(123,  -9),   S(105,  11),   S( 61,   8),   S(-12,  43),
            S( 68,   6),   S( 95,   4),   S( 94, -16),   S(118, -12),   S( 64,   5),   S(132,  -5),   S( 26,  21),   S(-42,  36),
            S(-15,  30),   S( 45, -21),   S( -8, -22),   S( 11,   2),   S( 41,  -3),   S(108,  18),   S(-48,  47),   S( 42,  37),
            S(-26,  40),   S(-42,   4),   S( 12,  -5),   S(  0,  11),   S( -5,  -9),   S(  6,  36),   S( 35,  50),   S( 52,  57),
            S( 11, -35),   S(-28,  35),   S( 23,  -4),   S(  3,  19),   S(-26,   1),   S(  3,  38),   S( 11,  45),   S( -7,  -6),

            /* knights: bucket 6 */
            S(  8,  -6),   S(-42,  40),   S(-11,  -1),   S(-33,  43),   S(-34,  26),   S(-37,  41),   S(-41,  35),   S(-44,  -2),
            S( 20, -31),   S( 70,  34),   S( 41,   6),   S( 52,  11),   S( 46,  25),   S(-10,  41),   S( 22,  37),   S(-33,  72),
            S(  0,  22),   S( 40,  37),   S(114, -18),   S( 96,  16),   S( 86,  20),   S( 23,  35),   S( 24,  43),   S( 18,  34),
            S( 93,  16),   S(101,  -1),   S(105,  12),   S(135,   6),   S(210, -36),   S( 50,  37),   S(  2,  23),   S( -6,  54),
            S( 30,  27),   S(116,   1),   S(171,  -4),   S(171, -25),   S( 89, -10),   S( 84,   9),   S(227, -36),   S( 58,  49),
            S( 50,  22),   S( 23,  26),   S(108,   8),   S( 31,  19),   S( 77, -31),   S( 61,  -3),   S( 24,  14),   S( 25,  26),
            S( -6,  42),   S(  6,  47),   S(121,  24),   S( 30,  21),   S( 70,   8),   S( 66,   1),   S( 14,  35),   S( 19,  69),
            S( 23,  45),   S( 11,  50),   S( 27,  38),   S( 11,  49),   S( 30,  15),   S(  4,  -2),   S(-37,  65),   S(-16, -39),

            /* knights: bucket 7 */
            S(-77, -58),   S(-55, -25),   S(  2, -19),   S(-59,  54),   S(  7,  -4),   S(-50,  30),   S(-42, -17),   S( -4,   3),
            S( 61, -67),   S(-11, -22),   S( 21, -11),   S(-36,  24),   S( 21,  23),   S( 39,  18),   S( -8,  20),   S(-46,  18),
            S( 22, -12),   S(  6,  -4),   S( 38, -24),   S( 75,  12),   S(108,  -5),   S(106,  -1),   S( 71,  20),   S(-35,  46),
            S(-36,  39),   S( 25,  15),   S(119,  -5),   S(116,   8),   S(123,  -5),   S(172,   0),   S( 54,  23),   S( 71,  11),
            S( 14,  36),   S(  1,  25),   S( 40,  19),   S(110, -20),   S(222, -36),   S(213, -46),   S(233, -57),   S(-21,  39),
            S(-21,  40),   S( 63,  23),   S( 25,  25),   S(105,   6),   S(152,  -1),   S(134,  -7),   S( 81, -37),   S( 20, -38),
            S(-16,  25),   S( -2,  10),   S( 38,  31),   S( 47,  34),   S( 99,  27),   S( 53,  18),   S( -9, -27),   S(  1, -16),
            S(-56, -31),   S( -5,  35),   S(  3,  54),   S( -1,  41),   S(-14,  62),   S( 30,  18),   S( 26,  -3),   S(  9,  11),

            /* knights: bucket 8 */
            S(-18,   5),   S( 22,  32),   S( 27,  23),   S( -5,  -7),   S(-10,  10),   S(-13, -18),   S( 24,  61),   S( -5, -44),
            S( -7,   2),   S( -8, -34),   S(-21, -103),  S( -7,  -7),   S( -5,   2),   S(  7, -31),   S( -5, -19),   S(-17, -76),
            S(-15, -95),   S(  2, -26),   S( 35, -73),   S( 53, -27),   S( 22, -40),   S( 45, -65),   S( -2, -28),   S(-16, -55),
            S(-18, -82),   S(-26, -91),   S( 26, -11),   S( 76, -24),   S( 40, -48),   S( 24, -52),   S(-15, -43),   S(-27, -40),
            S( -9, -31),   S( 22, -43),   S( 28,  11),   S( 21, -53),   S( 31, -62),   S( 28, -62),   S( 31, -36),   S(-31, -104),
            S(  6,  20),   S(  2, -46),   S( 10,  -9),   S( 13, -56),   S( 25, -60),   S( -8, -74),   S( -5,  18),   S( -8, -23),
            S(-14, -10),   S(  2, -45),   S(-20,   7),   S(  2, -46),   S(-15,   1),   S(-13, -35),   S( -9, -42),   S( -2, -13),
            S(  9,  28),   S( -2,  -3),   S( -8, -21),   S(  3, -20),   S( -2, -13),   S(  2,   2),   S(  4,  17),   S( -9, -31),

            /* knights: bucket 9 */
            S(-53, -203),  S(-30, -29),   S(  9, -52),   S( -5, -46),   S(-37, -53),   S(-18, -33),   S(-19, -40),   S(  2, -12),
            S(-27, -34),   S(-21, -34),   S( -4, -119),  S(-11, -69),   S( 16, -70),   S(-28, -65),   S(  5, -55),   S( -9, -64),
            S(  5, -35),   S( -1, -49),   S(-11, -64),   S( 21, -76),   S( 22, -56),   S(  3, -43),   S( -5, -29),   S(-23, -37),
            S(-33, -25),   S( -5, -70),   S(  0, -87),   S( 28, -76),   S( 13, -65),   S( 66, -59),   S(-11, -35),   S( -4, -92),
            S(  5,  -4),   S(  1, -52),   S(  0, -88),   S(  4, -71),   S( -9, -87),   S( 46, -59),   S( -7, -57),   S(-12, -53),
            S( -5, -43),   S(-26, -58),   S( -5, -88),   S( 24, -57),   S( 40, -29),   S( 16, -45),   S( -7, -61),   S(  6, -20),
            S( -9, -23),   S( 26,  16),   S(-14, -42),   S( -9, -52),   S( -2, -50),   S( 15, -24),   S(  3,  13),   S(  2,  20),
            S(  9,  35),   S( -9,  -7),   S( 15,   7),   S( -4, -53),   S(-18, -42),   S( -3, -44),   S( -6, -18),   S(-11, -39),

            /* knights: bucket 10 */
            S(-12, -75),   S(-39, -49),   S(-51, -44),   S(-34, -26),   S(-33, -76),   S( 25, -55),   S(-42,  -8),   S( -3, -39),
            S(-14, -49),   S(  8, -31),   S(-14, -62),   S(-34, -72),   S(-29, -78),   S( -7, -99),   S(-29, -21),   S(-33,  19),
            S( -4, -82),   S(-25, -84),   S( 39, -91),   S( -6, -73),   S(-15, -70),   S( 32, -78),   S(-30, -60),   S(-18, -33),
            S(-18, -96),   S( -7, -73),   S(  4, -70),   S( 22, -93),   S( 47, -66),   S(-19, -43),   S( 20, -50),   S(  6, -22),
            S(-38, -95),   S( 23, -64),   S( 19, -75),   S(-19, -82),   S(  8, -80),   S( -1, -87),   S( -6, -61),   S(  4, -37),
            S(-21, -71),   S(-12, -69),   S(-16, -78),   S( 31, -53),   S(-11, -65),   S(-12, -93),   S( -2, -54),   S(-29, -71),
            S( -5, -45),   S( -9, -40),   S( -8, -60),   S( 15, -59),   S(  3, -38),   S( -8, -75),   S(-10, -29),   S(-27, -65),
            S( -3,  -2),   S(  3, -16),   S(-15, -27),   S( -8, -47),   S(-14, -68),   S( -9, -60),   S( -6, -32),   S(  5,  28),

            /* knights: bucket 11 */
            S( -6, -71),   S(-32, -85),   S(-14, -42),   S(  9, -28),   S(-54,  -8),   S(  0, -16),   S(-16, -27),   S( 10,  35),
            S(-17, -73),   S(-21, -60),   S(  2, -53),   S( 64, -48),   S( 38, -20),   S( 34, -45),   S( -2, -37),   S( -8, -54),
            S(-24, -69),   S(  4, -71),   S( 72, -81),   S( 61, -41),   S( 14, -49),   S( 52, -54),   S( 20, -24),   S( -3, -96),
            S( -8, -65),   S(  3, -63),   S( 50, -51),   S( 65, -44),   S( 89, -54),   S( -1, -49),   S( 40, -44),   S( -5, -59),
            S(-35, -69),   S( 35, -52),   S( 38, -56),   S( 22, -65),   S( 33, -30),   S( 21, -17),   S( 12, -104),  S(-19, -47),
            S( 11, -25),   S(-39, -67),   S( 24, -21),   S( 59, -48),   S( 32, -37),   S( 15, -45),   S(-20, -43),   S( -1,  -1),
            S( -9, -28),   S(  3, -20),   S(-15, -12),   S( 16,   9),   S( 36, -30),   S( -2, -39),   S(  5, -55),   S(-29, -53),
            S( -5, -44),   S(  1,  -8),   S(-16, -78),   S( -7, -26),   S( -3,  -5),   S(-13, -43),   S(  0,   8),   S(  6,  11),

            /* knights: bucket 12 */
            S(-35, -83),   S( -3, -39),   S(-10, -49),   S( 16,  43),   S(  2,  51),   S(-32, -57),   S(-13, -34),   S(-11, -49),
            S(-16, -108),  S( -2, -31),   S( -7, -39),   S( -6,  23),   S( 35,  22),   S( -2,  -5),   S( -2, -37),   S( -7, -26),
            S(  5,  12),   S(-11, -48),   S(  3,  18),   S(-16, -156),  S( -3, -60),   S( -4, -33),   S( 13, -15),   S(  1,   4),
            S( -5, -28),   S( -2, -108),  S(-41, -133),  S( 29, -34),   S( 14, -48),   S( 19,   1),   S(  8,  -1),   S(  2,  34),
            S(  5,  38),   S( -4, -65),   S(  5, -81),   S( 23, -34),   S( -7,   0),   S( 21, -21),   S(-18, -38),   S( -5, -36),
            S( -5, -10),   S(-10, -32),   S(  1, -69),   S(-13, -102),  S( -7, -44),   S(-19, -46),   S( 15,  48),   S( -8, -31),
            S(  0,  -5),   S( -9, -17),   S( -7, -10),   S(-13,  -9),   S( -3,   4),   S(-26, -70),   S( -4, -33),   S( -4, -34),
            S(  0,   0),   S(  2,  49),   S( -4,  -3),   S(  2,   0),   S( -9, -25),   S( 12,  48),   S( -3,  -7),   S(  1,  12),

            /* knights: bucket 13 */
            S( -2,  -8),   S( -1, -19),   S( -4, -44),   S( -4, -40),   S( -5, -22),   S( 13,  38),   S( -9, -21),   S( -2, -19),
            S(-10, -34),   S( -3, -52),   S( -3, -23),   S(-32, -115),  S( -1, -48),   S( 12, -25),   S(  5,   8),   S(-12, -45),
            S(  5, -22),   S(  9,   9),   S(-31, -113),  S( -1, -57),   S(-11, -52),   S( 19,   1),   S( 13,  11),   S( -9, -22),
            S(  1,  -3),   S(-10,   1),   S(-26, -103),  S(-16, -74),   S( 10, -53),   S(  3, -44),   S( -9, -11),   S(  7, -14),
            S(  4,  32),   S( 10, -85),   S(  5, -83),   S(  3, -49),   S(-30, -98),   S( -7, -81),   S(-21, -50),   S( -3, -18),
            S( -9, -39),   S( 12,  22),   S(-16, -50),   S(-15, -107),  S( -2, -30),   S(-14, -91),   S( -4, -38),   S( 13,  45),
            S(  2,  -3),   S( -4,  -6),   S(-19, -37),   S(-11, -36),   S( -3, -36),   S(  1,   0),   S(-11, -44),   S(  2,  12),
            S(  4,  18),   S(  2,  22),   S(  8,  21),   S( -7,  -9),   S( -9, -24),   S(  1,  -3),   S( -1,   0),   S(  1,  11),

            /* knights: bucket 14 */
            S(  3,   8),   S( -7, -42),   S( -2,  -6),   S( -3, -30),   S(-19, -98),   S(  3,   3),   S(  6,  21),   S(  0,  -1),
            S(  6,   9),   S(-10, -46),   S( 10,   5),   S(-20, -63),   S(  1, -65),   S( -6, -44),   S( -3, -46),   S( -4,  24),
            S( -4, -28),   S(-12, -61),   S( 11, -27),   S(-29, -120),  S(-15, -65),   S( 33,  17),   S( -5, -30),   S(  9,  55),
            S( -8,  -4),   S( -9, -19),   S(-16, -88),   S(-12, -45),   S( -1, -35),   S(  1, -37),   S(-12, -30),   S(  3,  72),
            S(  2,  -1),   S(-26, -70),   S( -2, -63),   S(-16, -37),   S(-10, -16),   S(-48, -47),   S(-13, -26),   S(  0, -34),
            S(  4,  19),   S( -9,  -1),   S(  0,  46),   S( -8, -78),   S(-14, -77),   S(-14, -33),   S(  5,   4),   S(  0,   5),
            S(  0,   0),   S( -5, -13),   S(  4,  13),   S(  9,  29),   S( -6,  19),   S(  3,  22),   S(  5,   4),   S( -9, -13),
            S( -1,   0),   S( -2,   1),   S( -5, -29),   S(  2,   6),   S(  0, -13),   S(  0,   4),   S(  0,   7),   S(  0,   5),

            /* knights: bucket 15 */
            S(-15, -79),   S( 14,  25),   S( -3, -15),   S( 14,  25),   S(-11, -19),   S( -5, -38),   S(-14, -131),  S( -5, -58),
            S(  5,  30),   S( -3, -23),   S(-20, -48),   S( 19,  79),   S(-16,  14),   S(  0, -76),   S(-19, -81),   S(  6, -27),
            S(  1, -10),   S( -7, -42),   S(  2,  -4),   S( 45, -34),   S( -5, -85),   S( -7, -62),   S(-12, -71),   S(  0, -25),
            S( -5, -21),   S( -7, -27),   S(-17, -40),   S( 10, -34),   S(  7, -72),   S(-17, -70),   S(  2, -34),   S( -2,  -6),
            S( -8, -35),   S( -4,  -2),   S( -2, -50),   S( -8,  32),   S( 12,  10),   S(-21, -12),   S(  7, -35),   S(  9,  26),
            S(  5,  16),   S(-10,  -4),   S(-10, -37),   S( -8, -12),   S(-21, -56),   S( -9,   6),   S( -2,  30),   S(  5,  35),
            S( -4, -17),   S( -4, -28),   S(  3,  29),   S(-12, -18),   S( 17,  72),   S( -3,  -2),   S(  2,  12),   S(  3,  15),
            S(  0,   5),   S( -6, -21),   S( -4,  -1),   S( -6, -27),   S( -3,  15),   S(  4,  17),   S(  6,  43),   S(  2,  11),

            /* bishops: bucket 0 */
            S( 23,   3),   S(-10,  37),   S(  7,  10),   S( -1,   5),   S( 23,   3),   S( 11,   8),   S(125, -74),   S( 32, -26),
            S( 36, -73),   S( 25,   2),   S( 18,  -3),   S( 43, -11),   S( 21,  18),   S(102, -27),   S( 55,  33),   S( 97, -37),
            S( 51, -30),   S( 16,  20),   S( 54,  -2),   S( 45,   8),   S( 68,  -3),   S( 50,  41),   S( 77,  11),   S( 41, -25),
            S( 38, -43),   S( 31, -21),   S( 48,  -1),   S( 95, -24),   S( 83,  37),   S( 66,  36),   S( 44, -19),   S( 23,  10),
            S( 25,  10),   S( 67,  -9),   S(128, -30),   S(106,   4),   S(152, -38),   S( 38,   3),   S( 47,  13),   S( 15,  19),
            S( 34,  19),   S(131,  -5),   S(130,   7),   S( 60, -12),   S( 69,   6),   S( 30,  24),   S( 58,  12),   S(-14,  34),
            S(-116, -109), S( 43,  57),   S( 87,  53),   S( 18,  28),   S( 47,  13),   S( 48,   8),   S(-27,  71),   S( -3,  30),
            S(  6,  -2),   S( 12, -18),   S( 13,   7),   S( 36,  19),   S( -8,  -3),   S(-34,  -7),   S( -3,   0),   S(-18, -27),

            /* bishops: bucket 1 */
            S(-51,  -5),   S( 59, -15),   S(-24,  40),   S( 29,  -4),   S( 17,   1),   S( -2,   9),   S( 32,  -2),   S( 39,  -1),
            S(-16, -22),   S( -5,  14),   S( -2,   7),   S( -5,  19),   S( 41,  -9),   S( 35,  -9),   S( 57, -10),   S( -5,  14),
            S(-31,  42),   S( 24,   0),   S( 11,  13),   S( 24,   0),   S(  8,  16),   S( 46,   3),   S( 15,  19),   S( 52,   5),
            S( 25,   0),   S( 34,  -2),   S( 19,  18),   S( 29,  12),   S( 60,   0),   S(  8,  32),   S( 68,  -1),   S( 13,  22),
            S( 36,  -4),   S( 75,  10),   S( 11,  19),   S(111, -38),   S( 39,  13),   S(116, -32),   S( 18,   7),   S( 11,   8),
            S( 38, -23),   S( 54,  24),   S( 50,   8),   S( 63,  16),   S(102, -31),   S( 36,   4),   S( 16,  19),   S( -8,  -1),
            S(  2, -35),   S( 60, -13),   S(-29,   1),   S( 48, -27),   S( 36,  14),   S(  4,   5),   S( 11,   0),   S(-59,  30),
            S( 26, -39),   S(-27,  20),   S(-13,   8),   S(-60,  76),   S( 17,   5),   S(-14,  16),   S( 63, -38),   S( 28, -29),

            /* bishops: bucket 2 */
            S( 20,  15),   S(-19,  30),   S(-13,  38),   S(-44,  55),   S( 15,  24),   S(-32,  38),   S(-11,  16),   S(-12,  -3),
            S(  3,  25),   S(  9,  21),   S(-21,  32),   S(  5,  34),   S(-17,  47),   S(  1,  31),   S( -5,  32),   S(  5,  -8),
            S( 19,  48),   S( 12,  33),   S( 12,  45),   S(-11,  33),   S( -4,  59),   S(-26,  48),   S( -9,  35),   S(-42,  41),
            S(-13,  40),   S( 24,  41),   S(  8,  45),   S( 34,  42),   S(  4,  41),   S(-26,  71),   S(-38,  51),   S(-16,  57),
            S(  1,  40),   S( 26,  32),   S( 55,  29),   S( 31,  32),   S( -6,  53),   S( -2,  44),   S(  1,  60),   S( 26,  37),
            S(-26,  48),   S(-27,  52),   S(-12,  38),   S( 89,   2),   S( 58,  16),   S( 69,  60),   S( 49,  24),   S(  8,  -4),
            S(-16,  48),   S(  4,  16),   S( -7,  36),   S( 54,  31),   S(-103,  11),  S(-58,  11),   S(-38,  52),   S(-54,   0),
            S(-63,  10),   S(-10,  27),   S( 19,  44),   S(  0,  62),   S( 46, -13),   S(-17,  31),   S(-39,  13),   S(-45, -10),

            /* bishops: bucket 3 */
            S( 57,  11),   S( 49,   3),   S(  5,  38),   S( 13,  55),   S( 23,  64),   S( -9,  69),   S(-17, 104),   S( -3,  53),
            S( 28,  47),   S( 26,  45),   S( 22,  40),   S( 17,  65),   S( 26,  55),   S( 26,  61),   S( 13,  57),   S( 33,  31),
            S(  3,  53),   S( 36,  71),   S( 35,  80),   S( 27,  74),   S( 25,  89),   S( 18,  68),   S( 15,  69),   S(-14,  66),
            S(-17,  63),   S( 12,  74),   S( 41,  86),   S( 55,  82),   S( 54,  61),   S( 12,  77),   S( 18,  33),   S( 32,  17),
            S( 15,  56),   S( 31,  64),   S( 15,  90),   S( 58,  84),   S( 39,  93),   S( 62,  69),   S( 21,  66),   S(  0,  76),
            S( 44,  58),   S( 39,  81),   S(  5,  75),   S( 29,  86),   S( 46,  81),   S( 51, 115),   S( 74,  65),   S(  1, 133),
            S( 11, 101),   S( 39,  70),   S(  4,  74),   S( -4,  79),   S(-30, 108),   S( 20,  87),   S(-33,  92),   S(-31,  56),
            S(-19,  43),   S(-34,  94),   S(-92, 107),   S(-77, 125),   S(-12,  76),   S(-166, 154),  S( 26,  48),   S(-33,  37),

            /* bishops: bucket 4 */
            S(-26,  -4),   S(-51,  -1),   S(-109,  29),  S(-26,  13),   S(-19,   1),   S( 16,   2),   S(-20, -29),   S( -6, -62),
            S(-30,   7),   S(-20, -32),   S( 39, -25),   S( -2, -22),   S(-32,   7),   S( 31, -26),   S( 19, -44),   S( -2, -20),
            S(-63,  21),   S(-25,  -3),   S( 59, -24),   S( 27, -13),   S(111, -48),   S(  7,  -6),   S(-34, -12),   S(-51, -62),
            S( 74, -25),   S(100, -51),   S( 71, -23),   S(129, -39),   S( 98, -35),   S(128, -52),   S(-50, -16),   S(-39, -15),
            S( 19, -43),   S(  7, -51),   S( -3, -25),   S( 71, -64),   S(141, -47),   S( 63, -36),   S(  9,  14),   S(  6, -10),
            S(-76, -93),   S(-50,  -8),   S( 75, -55),   S( 44, -14),   S( 27, -20),   S( 28, -15),   S( 25,   9),   S(-20,   6),
            S(-15, -34),   S(  3, -36),   S( 44, -15),   S(-19, -31),   S( 27, -19),   S( 31, -12),   S(-18,  15),   S( 13,  -6),
            S(-23, -81),   S( -4, -31),   S(  2, -58),   S( 39, -22),   S(  8, -12),   S( -9,  -1),   S(-13,  54),   S(  4, -35),

            /* bishops: bucket 5 */
            S(-110,  33),  S(  3,  12),   S(-51,  14),   S(-45,  13),   S(-10,   5),   S(-57,  17),   S(-58,  17),   S(-36,  12),
            S( 14,   4),   S(-42,  17),   S( 35, -12),   S(  5, -11),   S(-17,  17),   S(-36,  14),   S( 11,  -2),   S(-41, -13),
            S( 18,   8),   S( 32,  -5),   S( 63, -16),   S( 18,  -5),   S( 39,  -3),   S(-10,  20),   S(-15,  12),   S(-29,  24),
            S( 34,  10),   S( 47,  -9),   S( 58, -20),   S(110, -20),   S( 61, -16),   S( 77, -10),   S( 12,  -9),   S(-24,  33),
            S( 19, -13),   S( 11, -10),   S( 74, -29),   S( 67, -42),   S( 62, -24),   S( 72, -22),   S( 76, -23),   S(-48,   4),
            S(-13,   1),   S( 21, -23),   S( 68, -26),   S(-56,  -6),   S( 35, -15),   S( 72, -14),   S(-60,  12),   S( 14,   2),
            S(-23,  -3),   S( 16, -12),   S(-55,   7),   S(-42,  15),   S( 10, -27),   S( 18,  -1),   S( 31,  14),   S(-33, -11),
            S(-24, -29),   S(-36,  -6),   S( 18,   4),   S( 31,   6),   S(-26,  27),   S( 21,  27),   S(-38,  -2),   S(  4,  17),

            /* bishops: bucket 6 */
            S(-90,  23),   S(-45,  11),   S(-55,  23),   S(-15,  15),   S( -9,  -2),   S(-17,  10),   S(-50,  27),   S(-126,  41),
            S(-17,   6),   S(-13,   2),   S(-28,  18),   S(-25,  16),   S( -7,   3),   S(-26,  21),   S( 17,   0),   S(-68,  15),
            S( 38,  -8),   S(-28,  11),   S( 37,  -4),   S(  9,  11),   S( 35,   0),   S( 55, -14),   S(-37,  19),   S(-32,  33),
            S(-19,   6),   S( 53, -14),   S( 48,  -8),   S(110, -14),   S(137, -28),   S( 79, -10),   S(-45,  10),   S(-32,  17),
            S(  6,   4),   S( 16,  -5),   S(101, -31),   S(156, -41),   S( 23, -14),   S( 12, -35),   S( 80, -28),   S( 45, -12),
            S(-50,  29),   S( 97, -23),   S( 36,  -9),   S( 14,  -9),   S(-20,  13),   S( 16, -12),   S( 26, -13),   S( 26, -19),
            S(-35,  19),   S(-50,  24),   S( 33,   1),   S(-14,  -4),   S(-44,  16),   S(-39,  -5),   S( 33, -16),   S(-88,  15),
            S(-81,  24),   S(-35,  22),   S( 36,  19),   S(  1,  31),   S( 28,  -3),   S( 44, -36),   S(-22, -12),   S( -4,   6),

            /* bishops: bucket 7 */
            S( 11,  -7),   S(-98,  37),   S(-16,  -1),   S( 25,   8),   S(-15,  11),   S(-45,  12),   S(-56,  24),   S(-27, -30),
            S( 17, -34),   S( 22, -34),   S(  3,   3),   S( 17,  -3),   S( -9,  13),   S(-14,  -8),   S(-41,   7),   S(-66,  50),
            S( 23, -22),   S( 17,  -5),   S( 60, -14),   S( 84, -17),   S( 48,  -9),   S( 43, -12),   S(-14,  11),   S(-73,  61),
            S( 26, -12),   S(  4, -16),   S( 49, -17),   S(105, -32),   S(181, -35),   S(110, -14),   S( 62, -13),   S(-22,  17),
            S( 10,   3),   S( 31,  -8),   S( 50, -21),   S( 52, -23),   S(157, -41),   S(192, -46),   S(-21,   6),   S(-28,  -4),
            S(-31,   4),   S( 31,   7),   S(-15,  12),   S(-19,   8),   S(-22,  11),   S( 75, -12),   S( 59,  -5),   S(-43, -142),
            S(-33,  24),   S(-10,  21),   S(-17,  14),   S( 56,  -8),   S( 20, -10),   S( 51, -16),   S( 10, -17),   S( 10,  -8),
            S(-42,  -8),   S(-26,  13),   S(-11,  17),   S(-24,  22),   S(  9,  14),   S( 16,   3),   S(  9, -11),   S(-24,   3),

            /* bishops: bucket 8 */
            S(  0,  80),   S(-47,  20),   S( 16,  -8),   S(-28,  40),   S(-14,   4),   S( -8, -27),   S(-31, -77),   S(-34, -79),
            S(-12,   0),   S( -6,   9),   S(-29,  20),   S( 20,   6),   S( 12, -16),   S( 14, -13),   S(-54, -81),   S(-10, -90),
            S( -6, -21),   S(-11, -76),   S( 24,  18),   S( 36, -13),   S( 15, -17),   S(-25, -43),   S(-43, -98),   S(-53, -57),
            S(  6,  22),   S( 34,  80),   S( 27, -18),   S( -5, -11),   S( 49, -21),   S( 24,   4),   S(-13, -36),   S(  3, -26),
            S(-10,  69),   S( -8,  48),   S(-15,   7),   S(-15, -43),   S( 13,   2),   S( -6,   7),   S( 18, -22),   S(-16, -20),
            S(-25, -23),   S(-20,   3),   S(  8,  33),   S( 33,  11),   S(  9,  13),   S( 17,  -7),   S(-15,   9),   S( -6,  -2),
            S(-25, -21),   S(-13, -47),   S( 10,  16),   S(-10,  31),   S(-16,  -2),   S(-12, -16),   S(-35,   9),   S(-28,  16),
            S(  6,  29),   S( -4,  -5),   S(-10, -10),   S(  3,   9),   S( 12,  34),   S( -9,  39),   S(-17,  31),   S(  0,  80),

            /* bishops: bucket 9 */
            S(-35,  32),   S(-73, -16),   S(-79, -13),   S(-83, -14),   S(-45,  -8),   S(-35, -36),   S(-18, -29),   S(-26, -71),
            S(-32, -12),   S(-28, -19),   S(-20, -25),   S(-47, -38),   S(-67, -43),   S(-21, -49),   S(-60, -30),   S(-23, -89),
            S(-10, -59),   S(-24, -43),   S(-34, -45),   S(  3, -33),   S(-21, -21),   S(  3, -54),   S(  0, -47),   S( 24, -47),
            S(-48,   5),   S(-59, -35),   S(  8, -31),   S( 29, -16),   S(-35, -43),   S(-11, -38),   S( 11, -29),   S(  1, -23),
            S(-38, -12),   S(-59, -10),   S(  5,  -9),   S(  8, -47),   S(-18, -36),   S(-32, -20),   S(  8, -36),   S(-14, -43),
            S( -7,   4),   S(-23, -24),   S(-44, -28),   S( -5, -18),   S(-59, -18),   S(-28,  -9),   S(  7, -17),   S(-44, -33),
            S(-12,  10),   S(-42,  -9),   S( -1, -35),   S(-47, -31),   S(-25, -12),   S(-19, -20),   S(  3,  10),   S(-16,  35),
            S(-13,  16),   S(-20, -55),   S(-14, -35),   S(  2, -11),   S(-14, -10),   S(-27, -24),   S(-37,   1),   S(-18,  -5),

            /* bishops: bucket 10 */
            S(-44, -44),   S(-24, -31),   S(-26, -49),   S(-94, -35),   S(-126, -52),  S(-102, -84),  S(-75, -29),   S(-30, -20),
            S(-30, -83),   S(-20, -70),   S(-41, -57),   S(-51, -76),   S(-32, -74),   S(-68, -61),   S(-48, -37),   S(-41, -41),
            S(-20, -76),   S(-27, -101),  S(-64, -59),   S(-22, -57),   S( -7, -55),   S( 21, -45),   S(-12, -38),   S(  2, -69),
            S(-36, -59),   S(-22, -62),   S(-35, -69),   S( 13, -55),   S(  1, -49),   S( 53, -53),   S( -4, -22),   S(-46, -31),
            S(-24, -37),   S(-85, -40),   S( 39, -59),   S(-17, -63),   S( 29, -62),   S(-52, -28),   S(-38, -39),   S(-12, -23),
            S(-52,   0),   S( 21, -35),   S(-81, -36),   S(-31, -35),   S(-30, -47),   S(-51, -53),   S(-25, -26),   S(-39, -30),
            S(-23,   6),   S(-32, -30),   S(-22, -21),   S(-23, -26),   S(-20, -52),   S(-36, -46),   S(-30, -58),   S(-19, -33),
            S(-20,  -2),   S(-31,   3),   S( -1,  -3),   S(-37, -38),   S(-34,  -5),   S(-62, -61),   S( -9, -60),   S( -9, -11),

            /* bishops: bucket 11 */
            S(-20, -51),   S(-29, -50),   S(-54, -11),   S( -4,  29),   S(-35,  -6),   S( -7, -29),   S(-28,   5),   S(-54,  53),
            S(-23, -22),   S( -8, -36),   S(  5,  -2),   S( -5, -27),   S(-17,   1),   S(-63, -14),   S(-107, -13),  S( -1,  39),
            S( 15, -40),   S( -8, -53),   S(-38, -28),   S(  6, -36),   S(-45, -36),   S( 42,  13),   S( -3, -14),   S(  0, -22),
            S( 20, -17),   S(-10, -19),   S(-20, -21),   S( 11, -57),   S( 20, -32),   S( 23,  -6),   S( 22,  38),   S(-26, -49),
            S(  6,  40),   S(-30,   3),   S(-45, -23),   S(-41,  14),   S( 46, -30),   S(  7,   9),   S(-31,   7),   S( -1,  57),
            S(-43,  18),   S(-33, -19),   S(-41,  20),   S(  4,  13),   S(-27,  -1),   S(-52,   3),   S(  9, -11),   S(-14, -24),
            S(-47,   3),   S(-64,  38),   S( -7,  15),   S(-25,   1),   S(  3,  -3),   S(-11, -13),   S(-16, -48),   S( -2,  27),
            S(  7,  90),   S(-56,   5),   S(  3,  28),   S( -5,   9),   S(-30,   9),   S( -5,  -1),   S(-18, -16),   S(  3,  14),

            /* bishops: bucket 12 */
            S(-20, -54),   S( -8, -71),   S( 11,  12),   S( -5,  36),   S(-26, -34),   S( 12,  13),   S( -6, -21),   S( -6, -23),
            S( -9, -43),   S(  0,   8),   S( -3,  21),   S( 21,  25),   S(-13, -73),   S( 13,  -9),   S(-25, -60),   S( -4, -22),
            S( 11,  64),   S( 18,  70),   S(  8,  35),   S( 39,   3),   S( -6,  -3),   S( 16,   1),   S( -9, -56),   S( 14,  59),
            S(  5,  61),   S(  4,  86),   S(  8,  43),   S( 18,  25),   S( 14,   5),   S( 16, -14),   S( 21,  25),   S( -3, -22),
            S( 17,  53),   S( 16,  34),   S( 15,  13),   S(  8,  10),   S( 38,  60),   S( 18,  52),   S( 13,  31),   S( -1, -12),
            S(  0,  -8),   S( -3,   4),   S(-16, -49),   S(  3,  23),   S(-10,  41),   S(  4,  21),   S(-12, -12),   S(  7,  23),
            S( -1,  -2),   S( -1,  -4),   S( 13, -16),   S( -2,  -9),   S( 14,  51),   S( 12,  51),   S( -3,  12),   S(  5,  60),
            S(  3,  16),   S( -3,  -3),   S( -2, -31),   S(  3,  19),   S(  5,  14),   S( -1,  -1),   S(  7,  88),   S(  0,  26),

            /* bishops: bucket 13 */
            S(-23, -56),   S(-10,   2),   S(-25, -65),   S(-13,  20),   S( -1,  18),   S( -4, -29),   S(-10, -38),   S( -5, -30),
            S(-16,  37),   S(-16,   6),   S(  4,  21),   S( -3,  62),   S(-16,  -2),   S(  3,   0),   S( 10, -30),   S( -5, -15),
            S( -8,  35),   S( -3,  98),   S(  3,  15),   S( 36,  -1),   S( 16,  14),   S(  8,   2),   S(  1,  -5),   S( -9, -20),
            S( 17, 118),   S( 14,  51),   S( -8,   4),   S(-38, -34),   S(  7, -13),   S(  1,  15),   S(  4,  16),   S( -3, -11),
            S( -2,  79),   S( -5,   6),   S( -1, -51),   S( 18,   3),   S(  4, -42),   S(-11,  26),   S(  4,  -8),   S( -2,  51),
            S(-13,   5),   S(-16, -51),   S( -3,  -4),   S( -9, -43),   S( -2,  21),   S(  7, -22),   S(-14, -35),   S( -9,   2),
            S( -9,  -2),   S(-13, -13),   S( -7, -55),   S( -5, -11),   S( -6,  -2),   S( 28,  46),   S(  8,  -9),   S(  4,  75),
            S( -6, -16),   S( -1, -10),   S(-14, -47),   S(  2, -18),   S( -3,  -9),   S( -5,  -2),   S( 10,  60),   S(  5,  34),

            /* bishops: bucket 14 */
            S(-33, -102),  S(-10, -10),   S(  0,  24),   S(-18,  -7),   S(-11, -17),   S(-14, -64),   S(-18, -35),   S(  0, -43),
            S(-13, -31),   S(  3,  -2),   S(-16,  -2),   S( 16,  48),   S(  7,  61),   S( -5,   5),   S(-33,   5),   S(  2,  48),
            S( -8, -18),   S(-25, -16),   S(-32, -38),   S(  6,  -2),   S(  6,   3),   S(  6,  -3),   S(-16,  49),   S(-14,  20),
            S( -2,  56),   S( -8,  14),   S(-18,   5),   S(-24, -21),   S(-20, -31),   S(-15, -12),   S( -2,  60),   S(  2,  71),
            S(-14,  12),   S( -7,  21),   S(-16, -13),   S( -4, -23),   S(-26, -90),   S(  0,   3),   S(  9,  -8),   S(  3,  89),
            S( -1,  31),   S( 21,  32),   S(-15, -13),   S( -3,   5),   S( -6,  44),   S(-13, -18),   S( -4, -51),   S(  6,  36),
            S(  3,  59),   S(-21, -15),   S(  7,  28),   S( -8,   5),   S(  3,  -9),   S(  8,  13),   S( -6, -42),   S(-14, -24),
            S(  4,  45),   S( 23,  57),   S(  0,  -7),   S(-13, -27),   S( -4, -35),   S( -7, -18),   S( 13,  37),   S(  1,   6),

            /* bishops: bucket 15 */
            S( -7, -33),   S( -9, -40),   S(-18, -28),   S(-13, -43),   S( -5,  48),   S( -1, -18),   S( -6, -44),   S( -7, -45),
            S( 11,  22),   S(-11, -34),   S( -1, -28),   S(-30, -33),   S( 25, -11),   S(-15, -15),   S(  3,  18),   S( -5, -26),
            S( -3,  -7),   S( -6,  -7),   S(  7,  10),   S( 22, -16),   S( 43,  23),   S(  5, -38),   S( 15,  62),   S(  6,  79),
            S(  0, -18),   S( -2,  26),   S( 24,  44),   S(-13, -32),   S(-15,  11),   S( 28, -16),   S( 30,  80),   S( -2,  75),
            S( -2,  11),   S( -8, -39),   S( -7,  27),   S( 24,  33),   S(-15, -15),   S(  5, -12),   S(  1,  12),   S( -6,  10),
            S(-10, -20),   S(  8,  56),   S( 13,  54),   S(  5,   7),   S( 23,  62),   S( -9,  22),   S( -8, -23),   S(  5,  10),
            S(  4,  35),   S( 12,  39),   S( 11,  29),   S( 11,  46),   S( -1,  23),   S( -1,  20),   S(  6,  32),   S(  1,   5),
            S(  0,  27),   S( 14,  45),   S( -6,  24),   S(  8,  23),   S(  1,   6),   S(  7,  52),   S(  9,  39),   S( 10,  14),

            /* rooks: bucket 0 */
            S( -5,  39),   S( 20,  23),   S( -7,  27),   S(  4,  37),   S(-15,  82),   S( -2,  55),   S(-25,  83),   S(-50,  84),
            S(-14,  40),   S( 36,  16),   S(-14,  33),   S(  2,  40),   S( -3,  71),   S( -9,  88),   S(-12,  50),   S(-47,  82),
            S( 26,  10),   S( 33,   2),   S(-23,  38),   S( 24,   0),   S(  1,  34),   S( -6,  31),   S(-21,  62),   S( 17,  50),
            S(-12,  27),   S( 20,  28),   S(-36,  51),   S(  3,  18),   S( -5,  66),   S(  2,  42),   S(-22, 113),   S(  5,  35),
            S( 58, -39),   S( 29,  29),   S( 12,  42),   S( -3,  52),   S( 20,  58),   S(  2,  60),   S(  2,  76),   S( 33,  58),
            S( 18,  14),   S( 83,  42),   S( 50,  59),   S( 90,  38),   S( 16,  75),   S( 20,  80),   S( 36,  83),   S(-15,  85),
            S( 48,  38),   S( 93,  79),   S( 59,  50),   S(114,  -2),   S(102,  33),   S( 59,  46),   S( 63,  54),   S(  4,  61),
            S( 37,  44),   S( 54,  52),   S(  7,  58),   S( 51,  23),   S(104,   8),   S(112,  10),   S( 56,  48),   S(164, -87),

            /* rooks: bucket 1 */
            S(-77,  95),   S(-40,  46),   S(-33,  61),   S(-61,  77),   S(-67,  90),   S(-62,  95),   S(-55, 102),   S(-96, 115),
            S(-79, 100),   S(-15,  24),   S(-48,  56),   S(-56,  65),   S(-64,  72),   S(-84,  93),   S(-52,  87),   S(-44,  92),
            S(-45,  46),   S(-28,  34),   S( -4,  17),   S(-50,  63),   S(-81,  69),   S(-90,  81),   S(-84,  99),   S(-38,  88),
            S(-59,  69),   S(-16,  32),   S(-41,  58),   S(-93,  93),   S(-97,  89),   S(-103, 107),  S(-84,  99),   S(-78, 125),
            S(-120, 120),  S(-43,  62),   S(  6,  48),   S(-26,  54),   S(-74,  82),   S(-59,  97),   S(-42, 109),   S(-49, 146),
            S( 18,  78),   S( 60,  37),   S( 37,  35),   S( 29,  46),   S(-15,  68),   S( 33,  77),   S( 30,  83),   S( 51,  83),
            S( 42,  58),   S( 27,  42),   S( 50,  27),   S(-14,  58),   S( 91,  14),   S( -6,  60),   S( 26,  86),   S( 46,  85),
            S( 33,  65),   S(-10,  67),   S( 22,  27),   S( 15,  39),   S( 19,  50),   S( 87,  40),   S( 15,  90),   S( 23, 105),

            /* rooks: bucket 2 */
            S(-92, 149),   S(-76, 135),   S(-81, 132),   S(-69, 108),   S(-60, 108),   S(-68, 113),   S(-62, 104),   S(-103, 135),
            S(-80, 139),   S(-95, 148),   S(-95, 137),   S(-86, 110),   S(-92, 118),   S(-83, 106),   S(-51,  90),   S(-66, 110),
            S(-64, 131),   S(-40, 117),   S(-73, 116),   S(-87, 110),   S(-74, 109),   S(-59,  95),   S(-55,  87),   S(-41, 109),
            S(-72, 144),   S(-73, 134),   S(-101, 140),  S(-109, 122),  S(-80, 108),   S(-78, 107),   S(-65, 114),   S(-61, 119),
            S(-57, 159),   S(-80, 157),   S(-65, 141),   S(-70, 116),   S(-88, 141),   S(-38, 104),   S(-58, 127),   S(-38, 138),
            S(-30, 160),   S( -4, 136),   S(  2, 124),   S(-60, 137),   S( 13, 103),   S( -9, 138),   S( 35, 118),   S( 22, 132),
            S( 64, 113),   S( 28, 122),   S( 51,  90),   S( 36,  78),   S(  3,  90),   S( 49, 112),   S(-64, 162),   S(  9, 142),
            S( 67, 113),   S( 37, 121),   S( 10, 120),   S(-38, 116),   S(-64, 121),   S( 30,  91),   S( 20, 117),   S( 18, 126),

            /* rooks: bucket 3 */
            S(-21, 165),   S(-15, 159),   S(-25, 180),   S(-11, 153),   S( -5, 138),   S(  1, 134),   S( 16, 120),   S(-15,  98),
            S(-20, 173),   S(-29, 189),   S(-24, 176),   S(-15, 161),   S(-15, 148),   S(  0, 110),   S( 32, 103),   S( 15, 105),
            S( 15, 141),   S( -8, 170),   S(-27, 173),   S(-31, 165),   S(  0, 130),   S(-11, 123),   S(  3, 125),   S( 48,  80),
            S(-19, 171),   S(-10, 172),   S(-26, 187),   S(-32, 174),   S(-27, 141),   S(-17, 154),   S( -1, 142),   S(-11, 131),
            S(-10, 185),   S(-28, 196),   S(  2, 187),   S(-22, 185),   S(-29, 186),   S(-17, 173),   S( 12, 169),   S( 30, 140),
            S(-32, 205),   S( 12, 190),   S( 19, 192),   S(  4, 191),   S( 31, 162),   S( 89, 144),   S( 49, 167),   S( 19, 149),
            S( 27, 175),   S( 17, 180),   S( 23, 178),   S( 32, 174),   S( 49, 157),   S(119, 121),   S(221, 104),   S(116, 148),
            S(156,  55),   S(126, 106),   S( 43, 173),   S( 22, 173),   S( 11, 185),   S( 27, 181),   S( 38, 156),   S(160, 106),

            /* rooks: bucket 4 */
            S(-39,   0),   S( 24, -28),   S( 32, -30),   S(-12,   9),   S(-62,  25),   S(-15,  30),   S(-13,   2),   S(-54,  26),
            S(-17, -20),   S(-52,  11),   S( -8, -10),   S( 32, -48),   S( 36, -29),   S( 26,   2),   S(-15,  -6),   S(  2,  -3),
            S(-56,   7),   S(-27, -30),   S(-46, -16),   S( -2, -14),   S(-130,  36),  S(-88,  33),   S(-34,  19),   S(-75,  21),
            S(-23, -33),   S( 50, -29),   S( 24, -18),   S( 29, -32),   S( 47, -20),   S( 30,  -9),   S( 11,   3),   S(-42,  15),
            S(-40,  -9),   S(  8, -41),   S( 23,   9),   S(128, -35),   S( 92, -23),   S( 23,   3),   S( 48,  18),   S( 16,  18),
            S( 10,   3),   S( 17,  20),   S( 12, -12),   S( 52,  -9),   S( 36,   9),   S( 84, -13),   S( 51,   9),   S( 60,  24),
            S(-18,  -3),   S( 35,   3),   S( 99, -29),   S( 57, -35),   S(114, -31),   S(-28,  16),   S( -9,   2),   S( 36,   6),
            S( -5,   5),   S( 74,  21),   S(118, -47),   S( 45, -28),   S( 27,  -3),   S( 50, -14),   S( 51,   6),   S( 15,  18),

            /* rooks: bucket 5 */
            S(-19,  57),   S(-13,  22),   S(  9,  27),   S( 33,  27),   S( -3,  41),   S(-19,  52),   S(  7,  48),   S(-45,  74),
            S(-12,  21),   S(-17,  28),   S(-12,  11),   S( 28,  13),   S(-27,  37),   S(-38,  24),   S(-23,  37),   S(-10,  63),
            S(-55,  43),   S( -6,  24),   S(-39,   5),   S(-74,  45),   S(-44,  20),   S( 19,   0),   S(-83,  61),   S(-40,  62),
            S(-22,  32),   S(-40,  41),   S( 29,   9),   S( 27,  13),   S( -6,  27),   S(-11,  39),   S( -5,  44),   S(-41,  63),
            S( 79,  34),   S( 66,  19),   S(-19,  43),   S( 92,  -3),   S( 93,  -4),   S(123,  -6),   S( 75,  32),   S( 20,  56),
            S( 22,  57),   S(-17,  50),   S( 13,  27),   S( 35,  31),   S( 40,  31),   S( 53,  38),   S( 32,  60),   S( 42,  64),
            S( 60,  20),   S( 84,   1),   S( 24,  15),   S( 58,  23),   S( 63,   7),   S( 84,   6),   S( 87,  27),   S( 65,  33),
            S( 88,  25),   S( 48,  33),   S( 89,  21),   S( 55,  44),   S(139,   2),   S( 64,  26),   S( 50,  55),   S( 28,  75),

            /* rooks: bucket 6 */
            S(-57,  74),   S(-29,  51),   S(-43,  55),   S( -4,  33),   S( -4,  42),   S( -4,  38),   S( -7,  49),   S(-53,  70),
            S(-35,  57),   S( 45,  21),   S(-39,  32),   S(-27,  25),   S(-14,  27),   S( 24,   6),   S(-85,  54),   S(-37,  48),
            S(-67,  53),   S( 10,  24),   S(-49,  31),   S(-60,  32),   S(-20,  24),   S(-11,  24),   S(-12,  11),   S(-29,  23),
            S(-54,  63),   S( 12,  34),   S( -9,  28),   S(-40,  44),   S(  3,  27),   S( 38,  14),   S(-32,  38),   S( 48,  28),
            S( 46,  44),   S( 74,  31),   S( 61,  34),   S( 40,  22),   S( 19,  32),   S( 43,  35),   S( 44,  32),   S(149,  16),
            S(119,  25),   S(151,   3),   S( 93,   6),   S( 48,  16),   S( 16,  28),   S( 15,  46),   S( 55,  32),   S(121,  22),
            S( 49,  26),   S( 97,  10),   S( 80,  -4),   S(117, -18),   S( 85, -11),   S(109,   1),   S(110,   2),   S(115,  -3),
            S(110,  -1),   S( 24,  44),   S( 62,  24),   S(102,   0),   S( 89,  26),   S( 76,  38),   S(108,  23),   S( 95,  30),

            /* rooks: bucket 7 */
            S(-82,  56),   S(-74,  50),   S(-84,  67),   S(-62,  47),   S(-72,  55),   S(-26,  26),   S(-22,  36),   S(-72,  33),
            S(-48,  40),   S(-55,  36),   S(-58,  42),   S(-75,  31),   S(-57,  25),   S(-22,   5),   S( 46,  11),   S(  9,  -8),
            S(-129,  67),  S(-110,  74),  S(-34,  18),   S(-20,   2),   S(-102,  48),  S(-65,  32),   S( 21,   3),   S(-58,  36),
            S(-123,  58),  S(-51,  37),   S(  2,  14),   S( 20,  -3),   S(  1,   6),   S( 14,  12),   S(105,  -1),   S( 24,  -4),
            S(  5,  32),   S( 35,  29),   S( 37,  27),   S( 48,  14),   S(122, -30),   S( 70,   2),   S(133,  -7),   S(-70,  30),
            S( 66,  19),   S( 39,  20),   S(138, -18),   S( 78,   3),   S( 91,  -5),   S( 73,  29),   S( 41,  54),   S( 31,  15),
            S( 37,   6),   S( 56,  -3),   S( 88, -21),   S(124, -32),   S(107, -18),   S(141, -28),   S(102,  25),   S( 27,  -3),
            S(  7,   0),   S( 13,  26),   S(  7,  24),   S( 35,   6),   S( 18,  17),   S(109,  -2),   S( 40,  33),   S( 44,  10),

            /* rooks: bucket 8 */
            S( 17, -85),   S(  0, -64),   S( 88, -84),   S( 35, -57),   S(-11, -64),   S(-25, -95),   S(-15, -42),   S(  5, -34),
            S(-16, -66),   S( 13, -83),   S( 51, -47),   S(-25, -64),   S(-45, -58),   S(  1, -43),   S( 12, -11),   S(-22, -45),
            S(-15,  -7),   S(-18, -64),   S( 29,  -4),   S( 15,  -5),   S(  0,   6),   S( 12,  13),   S( 38,  -6),   S(-20,   6),
            S(  2, -19),   S( -5, -14),   S(  2, -28),   S( 35, -15),   S( 18, -40),   S( 20,   0),   S( 12, -46),   S(-13, -45),
            S( -2, -41),   S( 25, -31),   S( 17, -64),   S(  9, -34),   S( 10,   2),   S( 11, -19),   S(-20, -19),   S(-24, -30),
            S(-10, -27),   S( 29, -49),   S( 45, -58),   S(  7, -43),   S( -2,  -7),   S(-43, -14),   S(  9,  -1),   S( 23,  17),
            S( 22,  20),   S( 64, -19),   S( 55, -46),   S( 17, -27),   S( 22, -13),   S( 27,  -3),   S(  9,  25),   S( 26,  17),
            S( 45,  -8),   S( 32, -23),   S( 53, -23),   S( 49, -12),   S( 19,  -6),   S( 11,  15),   S( 28,  25),   S( -5,  14),

            /* rooks: bucket 9 */
            S(-19, -114),  S( 63, -137),  S( 45, -140),  S( 15, -106),  S( 42, -106),  S(  4, -116),  S( 24, -99),   S(-37, -105),
            S(-21, -97),   S( -7, -75),   S(-20, -90),   S(-39, -109),  S(-47, -93),   S(  9, -64),   S(-27, -54),   S(-10, -87),
            S(-46, -49),   S(-16, -59),   S(-24, -59),   S(-10, -70),   S( 20, -67),   S(  8, -26),   S(-14, -56),   S(-41, -31),
            S( 39, -61),   S( -1, -72),   S(-32, -97),   S(  0, -14),   S(-13, -102),  S( 14, -53),   S(-10, -40),   S(-10, -26),
            S( 95, -90),   S( 22, -65),   S(-10, -87),   S( -7, -47),   S(-25, -81),   S( 16, -70),   S( -4, -75),   S(-30, -51),
            S( 23, -71),   S( 33, -68),   S( 24, -68),   S( 12, -67),   S( 47, -75),   S( 28, -52),   S( -7, -56),   S( -7, -76),
            S( 32, -60),   S( 29, -57),   S(  6, -68),   S(  2, -37),   S(-10, -67),   S( 22, -78),   S( -3, -36),   S( 18, -49),
            S(-17, -43),   S( 21, -35),   S(  7, -32),   S(  6, -39),   S( 40, -49),   S(-23, -25),   S( 12, -45),   S( 17, -10),

            /* rooks: bucket 10 */
            S(-63, -114),  S(-72, -89),   S(-10, -132),  S( 20, -119),  S( 71, -142),  S( 11, -138),  S( 69, -133),  S( 16, -125),
            S(-35, -87),   S(-43, -60),   S( -8, -112),  S(-36, -99),   S(-36, -96),   S(-22, -111),  S(  2, -106),  S(-32, -113),
            S(-29, -71),   S(-61, -55),   S(-36, -83),   S(-34, -94),   S(-25, -76),   S(  7, -85),   S( 16, -86),   S(-43, -70),
            S(-12, -76),   S(-54, -71),   S(-18, -108),  S( -5, -74),   S(  4, -46),   S( -7, -47),   S(-18, -138),  S( -4, -115),
            S(  6, -77),   S(-36, -69),   S( 10, -104),  S( 20, -124),  S( 29, -91),   S( 17, -85),   S( 51, -112),  S( 25, -125),
            S(-16, -91),   S(-22, -78),   S( -3, -101),  S( 34, -109),  S(-26, -92),   S( 25, -78),   S( 32, -103),  S( -2, -104),
            S(-57, -70),   S(-24, -99),   S( 32, -119),  S(-29, -83),   S( -7, -71),   S(-51, -61),   S(-11, -89),   S( -1, -71),
            S(-22, -64),   S(-37, -55),   S(-24, -53),   S( 10, -76),   S(-24, -44),   S(  6, -48),   S( -7, -64),   S(-33, -48),

            /* rooks: bucket 11 */
            S(-34, -83),   S(-49, -60),   S( 35, -64),   S(-48, -70),   S(-25, -45),   S( 79, -103),  S(-18, -48),   S(-16, -103),
            S(-20, -29),   S(-28, -29),   S(-30, -40),   S(-43, -46),   S(-25, -41),   S(-15, -57),   S(  5, -63),   S( -2, -63),
            S(-12, -17),   S(-27,  -1),   S( -7, -17),   S(  2, -24),   S( -9, -32),   S( 21, -36),   S( 13, -25),   S(  9,  12),
            S(-24, -52),   S( 29, -54),   S(-35, -60),   S( 26, -26),   S( 24, -43),   S(  3, -57),   S(  6,   9),   S(-10, -52),
            S( -7, -38),   S( 59, -45),   S( 32, -50),   S( 26, -58),   S(  6, -49),   S( 66, -65),   S( 25, -26),   S( 23, -53),
            S( -5, -38),   S( 26, -40),   S(-10, -36),   S(-17, -56),   S( -1, -38),   S( 96, -62),   S( 64, -57),   S( 12, -37),
            S(  9, -24),   S(-52, -35),   S(-15, -41),   S( -4, -43),   S(-23, -47),   S( 84, -65),   S( 78, -45),   S(-10, -26),
            S(  3, -31),   S( 18, -19),   S( 11, -16),   S( 13, -13),   S( 13, -29),   S( 37, -31),   S( 57, -33),   S(-18,  12),

            /* rooks: bucket 12 */
            S(-24, -86),   S(  2, -22),   S(-32, -87),   S(-14, -89),   S(  8, -79),   S(  3, -62),   S(-29, -85),   S( -7, -65),
            S( -4, -41),   S( -1, -39),   S( 14,  -6),   S( 12, -13),   S(  2, -47),   S(  5, -56),   S(  3, -24),   S(-20, -62),
            S( -6, -28),   S(  7,  25),   S( 19, -31),   S( 48, -42),   S( 25, -59),   S( 26, -26),   S( 10, -20),   S(-20, -40),
            S(-11, -38),   S(  1,  -1),   S( 34,  18),   S(  3, -41),   S(  8, -32),   S( 20,  11),   S( -4, -49),   S(-13, -57),
            S(  6, -24),   S( 22,  -6),   S(  9, -48),   S(  5, -18),   S( -1, -41),   S(-15, -53),   S( -6, -30),   S(  3, -12),
            S( 15, -75),   S( 13, -26),   S( 11, -69),   S( 13, -79),   S( 11,  -8),   S( -1, -42),   S(-11, -65),   S( -3,  -8),
            S(-41, -51),   S(  3, -40),   S( 12, -52),   S( 19, -34),   S(-22, -43),   S( 10, -11),   S( -3, -25),   S(  3,   5),
            S( -1, -19),   S( -2, -17),   S( 10, -68),   S( 17, -25),   S(  4, -15),   S( -4,  -5),   S( -1, -18),   S( -6,  11),

            /* rooks: bucket 13 */
            S(-27, -73),   S(-37, -59),   S(-12, -60),   S( 10, -64),   S(-23, -115),  S( 33, -69),   S(-20, -47),   S(-45, -52),
            S(-19, -116),  S( -8, -87),   S( -6, -47),   S(  6,  -4),   S( 24, -51),   S( 30, -96),   S(  8, -69),   S(  3, -90),
            S( 30, -73),   S( -1, -85),   S( 22,  -5),   S( 19, -30),   S( 56, -36),   S( 26, -84),   S( 25, -63),   S(  2, -129),
            S(  3, -38),   S(-10, -57),   S( 10, -53),   S( 14, -53),   S( 37, -64),   S(-10, -76),   S( 18,  -4),   S(  8,  13),
            S( -3, -55),   S( 10, -85),   S( 26, -78),   S( 11, -52),   S( 26, -99),   S( 10, -52),   S(  1, -42),   S(-17, -90),
            S(  2, -90),   S( 11, -78),   S( 16, -76),   S( 26, -96),   S( 20, -122),  S(-16, -94),   S(-12, -83),   S( -7, -46),
            S( -4, -49),   S( 43, -61),   S( 22, -59),   S(  9, -64),   S( 21, -79),   S( 10, -35),   S(-12, -62),   S(-16, -47),
            S( -7, -31),   S(-12, -64),   S(  1, -64),   S( 39, -50),   S(-22, -90),   S(  6, -49),   S(  6, -33),   S( -8, -24),

            /* rooks: bucket 14 */
            S(-19, -77),   S(-32, -52),   S( -4, -70),   S(-22, -107),  S(-24, -129),  S( 18, -48),   S(-49, -145),  S(-44, -124),
            S( 19, -104),  S( 32, -77),   S( 10, -124),  S(-18, -109),  S( -5, -55),   S( -1, -49),   S( -5, -126),  S(-11, -112),
            S( 10, -107),  S(-19, -87),   S( 26, -108),  S(-13, -127),  S(  8, -32),   S(-11, -80),   S( 11, -142),  S(-23, -166),
            S(  1, -68),   S( 17, -52),   S( -1, -48),   S(-11, -106),  S(  4, -54),   S( 19, -57),   S(  8, -94),   S(-14, -111),
            S(  8, -78),   S(  9, -50),   S(  1, -102),  S( 31, -95),   S( -1, -112),  S( 27, -84),   S( 22, -94),   S(-13, -87),
            S( 18, -57),   S(  9, -65),   S( 34, -85),   S( 19, -137),  S( 12, -159),  S( 16, -117),  S(  5, -120),  S(-21, -117),
            S(  0, -54),   S(-17, -44),   S( -7, -102),  S(  4, -116),  S( -3, -114),  S( -1, -87),   S( 37, -93),   S( -9, -75),
            S( 15, -68),   S( -9, -58),   S(-11, -101),  S(-16, -129),  S(-14, -116),  S(-15, -139),  S( -7, -120),  S(-23, -85),

            /* rooks: bucket 15 */
            S(  8, -85),   S(-22, -109),  S(-26, -82),   S(-32, -112),  S(-17, -92),   S(  8, -62),   S(-18, -72),   S(-19, -113),
            S( 24, -38),   S(-23, -73),   S( 11, -53),   S( 10, -48),   S( -7, -85),   S( -7, -76),   S( -5, -47),   S( 23,  30),
            S( -6, -58),   S(-14, -58),   S(  3, -75),   S( 13, -66),   S( -8, -93),   S( -3, -47),   S(  0, -14),   S( -8, -39),
            S(  5,  -3),   S( -9,  -5),   S( -3,  -3),   S( -2, -37),   S(  2, -38),   S( -3, -60),   S(  5, -83),   S(  1, -84),
            S(  6,   2),   S(-17, -54),   S(  4, -70),   S( -1, -99),   S( -4, -50),   S( 20, -102),  S( 15, -92),   S( -6, -55),
            S( -2, -18),   S(  6, -33),   S( -2, -38),   S(  3, -61),   S( -1, -90),   S( 21, -116),  S( 21, -66),   S(-12, -71),
            S( -8, -30),   S(-25, -61),   S(  6, -30),   S( -7, -48),   S(  2, -55),   S( 20, -69),   S(  2, -77),   S( -1, -56),
            S(-11, -35),   S(  2,  10),   S( -7, -34),   S(-12, -71),   S(-11, -90),   S( -2, -91),   S( -3, -64),   S(-22, -74),

            /* queens: bucket 0 */
            S( -5, -15),   S(-25, -83),   S(-15, -82),   S(  5, -87),   S(-15, -46),   S(-17, -79),   S(-33, -27),   S(-74, -11),
            S(-19,   1),   S( 17, -140),  S( 18, -92),   S(-30,  -5),   S(  2, -47),   S(-16, -34),   S( -4, -76),   S(-65, -16),
            S(-13,  49),   S(-17,   2),   S(  3, -52),   S(-25,  23),   S(-37,  38),   S( -5, -27),   S(-24, -42),   S(-64,   3),
            S(-47,  61),   S( 21, -67),   S(-43,  25),   S(-43,  66),   S(  5,  24),   S(-15,  -7),   S(-65,  10),   S( 10, -74),
            S(-62,  18),   S(-10,  44),   S(-13,  84),   S(-16,  35),   S(-20,  90),   S(-77,  78),   S(-53,  71),   S(-32, -35),
            S(-45,  78),   S( -4,  96),   S(-13,  98),   S(  3,  54),   S(-81,  63),   S(-30,  33),   S(-89,  88),   S(-42,   8),
            S(  0,   0),   S(  0,   0),   S(-20,  30),   S(-24,  49),   S(-40,  31),   S(-103, 106),  S(-130, 111),  S(-83,  49),
            S(  0,   0),   S(  0,   0),   S( 16,  60),   S( -2,  20),   S(-56,  57),   S(-30,  19),   S(-95,  47),   S(-54,  16),

            /* queens: bucket 1 */
            S(-46, -28),   S( 15, -17),   S( 12, -89),   S( 34, -85),   S( 23, -48),   S( 27, -76),   S(  9, -11),   S( 21,  -3),
            S(-23, -24),   S( 16,  -3),   S( 45, -63),   S( 24, -17),   S( 47, -19),   S(-10,  -3),   S(-18,  75),   S( 16,  17),
            S(  3,  56),   S( 15,  -5),   S( 11,   2),   S( 30,  26),   S( 12,   2),   S( 24,  -1),   S(-26,  51),   S( 32,   2),
            S( 30,  -9),   S( 22,  51),   S(-16,  52),   S( -6,  58),   S(  7,  85),   S(-27,  58),   S(  9,  45),   S(-23, 110),
            S(  9,  59),   S( 26, 100),   S( 34,  89),   S( 35,  64),   S( 23,  78),   S( 67,  44),   S( 11,  68),   S( -5,  85),
            S( 34,  71),   S( 86,  74),   S(122,  87),   S( 89,  62),   S( 96,  46),   S( -5, 108),   S( 14,  92),   S(-15,  91),
            S( 78,  35),   S( 34,  80),   S(  0,   0),   S(  0,   0),   S( 27,  84),   S(-62, 159),   S(-48, 129),   S(-72, 107),
            S(102,  19),   S(115,  36),   S(  0,   0),   S(  0,   0),   S( -9,  47),   S( 63,  74),   S( 83,  54),   S(-50,  79),

            /* queens: bucket 2 */
            S( 18,  32),   S( 21,  10),   S( 29,  17),   S( 56, -43),   S( 64, -57),   S( 43, -59),   S( 41, -32),   S( 95,  20),
            S( 39,  29),   S( 19,  78),   S( 42,  22),   S( 44,  22),   S( 62,   9),   S( 30,  23),   S( 32,  56),   S( 34,  76),
            S( 40,  53),   S( 27,  88),   S( 14, 105),   S( 25,  74),   S( 18,  83),   S( 15,  72),   S(  8,  90),   S( 39,  78),
            S( 21, 120),   S( 26, 146),   S( -7, 140),   S(  5, 134),   S( 24, 102),   S(  1, 126),   S( 13, 110),   S( 31, 138),
            S( -3, 140),   S( 17, 106),   S(  9, 169),   S(  9, 156),   S( -2, 189),   S( 53, 132),   S( 63, 152),   S( 41, 120),
            S( 24,  92),   S( -6, 127),   S(-10, 162),   S( 57, 140),   S( 48, 138),   S( 78, 220),   S(136, 115),   S( 16, 197),
            S(-17, 164),   S(-20, 156),   S(-18, 194),   S( 19, 137),   S(  0,   0),   S(  0,   0),   S(-45, 230),   S( 12, 174),
            S(-22, 133),   S( 39, 133),   S( 58, 101),   S( 92, 123),   S(  0,   0),   S(  0,   0),   S( 66, 125),   S( 44, 152),

            /* queens: bucket 3 */
            S(-33, 126),   S(-28, 137),   S( -3, 100),   S( 11, 116),   S(  0,  77),   S( -3,  57),   S( 21,   6),   S(-20, 139),
            S(-38, 150),   S(-22, 145),   S(  1, 142),   S( -2, 150),   S( 11, 140),   S( -3, 103),   S( 24, 106),   S( 51,  46),
            S(-13, 151),   S(-15, 163),   S(-17, 200),   S(-19, 208),   S(-14, 175),   S(-20, 169),   S(-13, 153),   S(  9, 120),
            S(-22, 165),   S(-35, 215),   S(-29, 210),   S(-18, 231),   S(-24, 226),   S(-31, 202),   S(-14, 174),   S( -4, 167),
            S(-35, 192),   S(-30, 233),   S(-24, 239),   S(-31, 249),   S(-27, 247),   S( -5, 253),   S(-17, 237),   S(-19, 207),
            S(-28, 192),   S(-33, 235),   S(-66, 279),   S(-71, 280),   S(-51, 285),   S( 10, 266),   S(-34, 288),   S(-20, 273),
            S(-64, 224),   S(-81, 276),   S(-78, 296),   S(-100, 300),  S(-117, 340),  S(-19, 251),   S(  0,   0),   S(  0,   0),
            S(-101, 241),  S(-76, 262),   S(-65, 243),   S(-67, 246),   S(-36, 242),   S(-31, 284),   S(  0,   0),   S(  0,   0),

            /* queens: bucket 4 */
            S(-45, -41),   S(-53, -45),   S(-42, -28),   S( 51, -11),   S(-25,  -5),   S(-34, -28),   S(-57, -30),   S(-30, -13),
            S(  5, -15),   S( 24,  49),   S( 65, -33),   S(-16, -43),   S(-109,  45),  S(-16,  -2),   S(-39, -14),   S(-54, -30),
            S( -4,  -6),   S( 18, -32),   S( 69, -42),   S( -4,  -1),   S( 45, -39),   S(  1,  -8),   S(-36, -36),   S(  7,  10),
            S(-17, -17),   S(  1, -15),   S( 14, -16),   S( 32, -41),   S( -3, -15),   S(-11,  28),   S(-69,  21),   S(-101,   7),
            S(  0,   0),   S(  0,   0),   S( 18, -11),   S( 74,  22),   S( 38,   3),   S( 21,  36),   S(-10, -18),   S(-36,  -4),
            S(  0,   0),   S(  0,   0),   S( 46,  42),   S( 52,  19),   S( -2,  21),   S(  7,   3),   S(  3,  52),   S(-53, -20),
            S( 35,  23),   S(  2,  10),   S( 78,   7),   S( 83,  44),   S( 74,  17),   S( 26,   6),   S(-26,  -4),   S(-10,  14),
            S( 27, -17),   S(  9,  28),   S( 43,  18),   S( 68, -14),   S(  4,   0),   S( 20,   7),   S(-10, -11),   S(-11,  -4),

            /* queens: bucket 5 */
            S( 25,   4),   S(-17, -30),   S(-54, -34),   S( -9, -27),   S( -2, -13),   S( 25,  39),   S( 28,  38),   S(  4,   9),
            S( 16, -11),   S( 14, -25),   S(-24,  -8),   S(-22,   6),   S( 10,  -3),   S(-19, -22),   S(-15,   6),   S(-77, -37),
            S( 34, -18),   S( 60, -27),   S( 30, -39),   S(  0, -30),   S(  8, -20),   S(-24,   7),   S(-29,   3),   S(-18,  39),
            S( 15,   1),   S( 71,  19),   S( 71, -29),   S( 15,  12),   S( 72, -25),   S( 55,   2),   S( 50, -11),   S(-27,  19),
            S( 65,   7),   S( 59,   3),   S(  0,   0),   S(  0,   0),   S( 68,   1),   S( 78,  -4),   S( 28,  41),   S( -5,  24),
            S( 93,   7),   S( 66,  32),   S(  0,   0),   S(  0,   0),   S( 53,  26),   S(124,  45),   S( 61,  23),   S( 39,  19),
            S(129,   8),   S( 91, -26),   S( 76,  47),   S( 66,  67),   S( 71,   3),   S(115,   3),   S(138,  -1),   S( 12,  18),
            S( 67,  10),   S(100,  38),   S( 84,  13),   S(107,  25),   S(105,  -5),   S(115,  37),   S(100,  28),   S( 63,  44),

            /* queens: bucket 6 */
            S( 14,  32),   S( 21,  -7),   S( 37, -36),   S(-29,  27),   S(-22,  24),   S( 14, -22),   S(  0,  11),   S( 11,  -4),
            S( 13,   9),   S( 14, -28),   S(  1,   4),   S( 11,   0),   S(  7,  -8),   S( 43, -21),   S( 23,   7),   S( 18,  58),
            S(-27,  48),   S( 13,  18),   S(-11,   6),   S( 41, -21),   S( 32,  -4),   S( 18, -40),   S( 51,  31),   S( 10,  60),
            S(-21,  76),   S(  7,  22),   S( 55,   2),   S( 77, -36),   S( 85, -46),   S( 68,  11),   S(106,   8),   S( 91,   0),
            S( 14,  13),   S( 10,  55),   S(102,  19),   S( 11,  18),   S(  0,   0),   S(  0,   0),   S( 87,  36),   S(117,  27),
            S( 19,  54),   S( 59,  37),   S( 51,  25),   S( 60,   1),   S(  0,   0),   S(  0,   0),   S(144,  65),   S(145,  31),
            S( 38,  19),   S(122, -16),   S(131, -13),   S( 68, -15),   S( 96,  53),   S(132,  44),   S(238, -23),   S(239, -17),
            S( 75,   1),   S( 98,  43),   S(119,   3),   S(140, -14),   S(174,  -5),   S(172,   3),   S(155,  -7),   S(170,  17),

            /* queens: bucket 7 */
            S( 21, -45),   S(-34,  22),   S(-52,  27),   S(-42,  35),   S(-43,  13),   S(-81,  32),   S(-71,  37),   S(-57,   1),
            S(-50, -17),   S(-78,   9),   S(-32,  57),   S(-40,  32),   S(-48,  31),   S(-43,  44),   S( -5,  13),   S(-27,  13),
            S(-25,   9),   S(-59,  10),   S(-18,  20),   S(-15,  33),   S( 24,  -8),   S(  8, -25),   S( 24, -23),   S( 71, -15),
            S(-20, -26),   S( -8,  22),   S( 17, -29),   S( 76, -27),   S( 84, -29),   S(101, -40),   S( 44, -16),   S( 25,  50),
            S( -3,   8),   S(-43,  42),   S(-17,  63),   S( 36,  -3),   S(139, -37),   S(132,  23),   S(  0,   0),   S(  0,   0),
            S(-15,  -5),   S(-22,  18),   S(  6,  19),   S( 32,  -9),   S( 93, -17),   S(168,  40),   S(  0,   0),   S(  0,   0),
            S(-87,  49),   S(-61,  37),   S(  1,  11),   S( 82,  -3),   S( 92,  11),   S(138,   0),   S(171,  51),   S(110,  55),
            S( 12, -41),   S( 30,   2),   S( 50,  -1),   S( 68,   1),   S( 90,  -2),   S( 52,  29),   S( 32,   8),   S(137,   3),

            /* queens: bucket 8 */
            S(-28, -55),   S( 32,   7),   S(-12, -26),   S(-21, -34),   S(-14, -26),   S( -7, -39),   S( -6, -22),   S( -1,  -8),
            S(  1,   2),   S( 12,  -5),   S(  2, -20),   S( 17, -24),   S( 34,  29),   S( 19,  22),   S(-20,  -8),   S(  1,   3),
            S(  0,   0),   S(  0,   0),   S( 20,  -1),   S(  3, -64),   S( 21, -25),   S( 65,  19),   S( -6, -14),   S( 13,   8),
            S(  0,   0),   S(  0,   0),   S( 18,  18),   S(-18, -44),   S(-27, -63),   S( 34, -12),   S(  6,  29),   S(-12, -17),
            S(  4, -21),   S( -4, -31),   S( -8, -27),   S( 12, -33),   S(  9, -64),   S( -2, -46),   S( 13,  -9),   S(-26, -30),
            S( 35, -16),   S(  7, -52),   S( 35,  20),   S(  6, -28),   S( 13, -14),   S( -5, -74),   S(-30, -89),   S( -9, -29),
            S(-13, -42),   S( -4, -36),   S( 12, -32),   S( 42,  35),   S(-21, -64),   S(  0,   4),   S(-31, -64),   S( -4, -14),
            S( 30,  33),   S(-24, -54),   S( -7, -20),   S( 23,  23),   S(  2, -13),   S(-14, -26),   S(-27, -40),   S(-43, -42),

            /* queens: bucket 9 */
            S( 25,  -2),   S( -6, -70),   S(-15, -51),   S( 20, -44),   S(-22, -77),   S( -3, -29),   S(-44, -91),   S(-22, -47),
            S( -1, -26),   S(-19, -56),   S(  2, -30),   S( 23,   5),   S(-21, -84),   S(-14, -65),   S( 16, -36),   S(-16, -30),
            S(-10, -60),   S(  3, -41),   S(  0,   0),   S(  0,   0),   S(-24, -70),   S(  8, -49),   S( 21,  -5),   S(-11, -19),
            S( -5, -62),   S(-18, -51),   S(  0,   0),   S(  0,   0),   S( -6, -49),   S( 14, -40),   S( 11,  -8),   S(-23, -20),
            S( 35,   2),   S(-25, -100),  S( -3, -25),   S(-12, -41),   S( -2, -79),   S( 21, -39),   S( 14, -38),   S( -4, -39),
            S(-44, -90),   S(-20, -88),   S( 17, -43),   S(  5, -66),   S( 19, -41),   S(-28, -99),   S(-17, -79),   S( -9, -56),
            S( 18, -32),   S( -5, -82),   S( -2, -48),   S(  8, -46),   S( 12, -70),   S( 11, -66),   S( -8, -36),   S(-23, -44),
            S(-45, -104),  S(-14, -62),   S( -2, -35),   S(  5, -41),   S(  9, -14),   S(  0, -34),   S( -8, -63),   S(-10, -77),

            /* queens: bucket 10 */
            S( -4, -23),   S(  5, -61),   S( 12, -23),   S( 35, -29),   S( -3, -56),   S( -8, -48),   S(-13, -68),   S(-16, -67),
            S( -9, -23),   S( 16, -25),   S( 13, -36),   S(-41, -102),  S( 23,  19),   S( 18, -21),   S(-33, -106),  S( 20, -20),
            S(-24, -64),   S( -5, -40),   S(  8, -63),   S( -9, -72),   S(  0,   0),   S(  0,   0),   S( 20, -23),   S(-28, -93),
            S(-16, -46),   S(-36, -93),   S( 19, -22),   S(-19, -76),   S(  0,   0),   S(  0,   0),   S( 25, -29),   S( 11, -58),
            S(  3, -34),   S( -1, -34),   S(-25, -118),  S(  0, -97),   S(-40, -104),  S(-14, -57),   S( -4, -65),   S( 18, -63),
            S( -4, -31),   S(-20, -72),   S( -3, -71),   S( -1, -75),   S( 18, -39),   S( 16, -58),   S( 13, -65),   S(  7, -79),
            S(-22, -43),   S(-35, -57),   S(  2, -44),   S(  3, -60),   S(  7, -27),   S( 14, -26),   S(  1, -67),   S( -8, -27),
            S( -2, -66),   S(-15, -89),   S(-11, -94),   S(-24, -72),   S(  5, -20),   S(-22, -78),   S( 20, -28),   S(  8, -66),

            /* queens: bucket 11 */
            S(-45, -41),   S(  7, -56),   S(-26, -66),   S(-24, -55),   S(-23, -28),   S( 13,   2),   S(-29, -38),   S(  0, -20),
            S(-16, -28),   S(-28, -11),   S(-61, -66),   S(-16, -50),   S( 22, -53),   S( -1, -30),   S(  9, -28),   S(-25, -76),
            S(  8, -27),   S( -5,  -1),   S(-45, -59),   S(-38, -82),   S( -8, -57),   S( -8, -62),   S(  0,   0),   S(  0,   0),
            S(-15, -35),   S(-19, -15),   S( -5, -30),   S(-39, -88),   S(  7, -37),   S( -5, -34),   S(  0,   0),   S(  0,   0),
            S(-18, -17),   S(-30, -41),   S(  0, -52),   S( -1, -70),   S( 20, -53),   S( 33, -10),   S( -5, -21),   S( -1, -20),
            S(-26, -43),   S(  2, -11),   S( -2, -53),   S( 15, -29),   S(-28, -51),   S( 24, -46),   S( 21, -32),   S( 33, -64),
            S(-11, -13),   S(  3,   7),   S(  2, -14),   S(-39, -60),   S( 19, -29),   S( 24, -51),   S( 12, -29),   S( -7, -30),
            S(-40, -90),   S(-25, -88),   S(-18, -35),   S(-45, -35),   S( 27,  29),   S(-16, -42),   S(-25, -27),   S( 18,   3),

            /* queens: bucket 12 */
            S(  0,   0),   S(  0,   0),   S( 14,  23),   S(-32, -29),   S( -6, -14),   S( 19,  38),   S(  5,  -9),   S(  6,  14),
            S(  0,   0),   S(  0,   0),   S(  3, -15),   S(-19, -58),   S(-16, -39),   S( 22,  34),   S(-18, -46),   S(  0,  -9),
            S(  9,  11),   S( 21,  38),   S( -7, -31),   S(  6, -26),   S( 14,  -3),   S(  0, -11),   S( 17,   5),   S(  2,   2),
            S(-12, -41),   S(  6, -25),   S( 23,  -9),   S( 19, -10),   S( -6, -44),   S( -3, -32),   S(  0,  -3),   S(  2, -17),
            S(-10, -36),   S( 10,   4),   S(  2, -11),   S( -4, -61),   S( -1, -34),   S(-39, -107),  S(-16, -51),   S(-21, -39),
            S(-11, -37),   S( 19,  48),   S(-18, -78),   S(-22, -36),   S(-19, -75),   S(-18, -21),   S(-43, -76),   S(-16, -45),
            S(  5,  13),   S( -6, -20),   S( -5, -12),   S( 11,  -6),   S( -7, -58),   S(-26, -60),   S(-17, -38),   S(-19, -43),
            S(  2,   5),   S( 25,  40),   S(  3,  23),   S(-15, -30),   S(-13, -35),   S(-31, -81),   S(-24, -48),   S(-37, -93),

            /* queens: bucket 13 */
            S(  3, -61),   S( 11,  -9),   S(  0,   0),   S(  0,   0),   S( 16,  23),   S(  4, -33),   S(  7, -30),   S(-17, -40),
            S( 11, -35),   S( -8, -53),   S(  0,   0),   S(  0,   0),   S(-21, -35),   S(-20, -63),   S(-21, -58),   S(-15, -51),
            S(-11, -43),   S(-21, -57),   S( -1,  -9),   S(-13, -39),   S( -6, -56),   S(  5,  -9),   S( -8,  -6),   S(-12, -35),
            S( 10,   0),   S(-21, -71),   S( -1, -39),   S(-17, -71),   S(  3, -42),   S( -3, -13),   S(  0, -14),   S(-17, -36),
            S(  6, -16),   S(-16, -63),   S(-17, -59),   S( -4, -39),   S(-32, -74),   S( -9, -65),   S(-23, -82),   S(  4,  24),
            S(-19, -34),   S(-22, -66),   S(-19, -51),   S(-20, -43),   S(-20, -78),   S(-39, -110),  S(-33, -96),   S(-23, -54),
            S( -6, -18),   S( -5, -26),   S(-18, -44),   S(  9,  17),   S(-12, -30),   S(-45, -98),   S(-15, -48),   S(  2,  -4),
            S(-10, -30),   S(-13, -34),   S(-14, -33),   S(  8,  25),   S(  9,  11),   S(-32, -64),   S(-19, -39),   S(-28, -73),

            /* queens: bucket 14 */
            S(  7,  -5),   S(  9,  -8),   S(  6, -43),   S( -3, -13),   S(  0,   0),   S(  0,   0),   S( -8, -35),   S( -6, -30),
            S(-16, -50),   S(-24, -72),   S( -8, -50),   S( -5, -47),   S(  0,   0),   S(  0,   0),   S(-19, -41),   S(-38, -97),
            S(-17, -50),   S(-20, -60),   S(-15, -65),   S(-20, -46),   S( -2, -16),   S(  9,  10),   S(-17, -60),   S(-26, -71),
            S(  7,  -6),   S( -7, -30),   S(-19, -60),   S( -7, -46),   S(-22, -72),   S(-20, -59),   S(-18, -87),   S( -6, -48),
            S(-20, -43),   S( -9, -52),   S(-38, -111),  S(-10, -56),   S(-11, -58),   S(-30, -79),   S(-13, -61),   S( -9, -66),
            S(-10, -26),   S( -5, -33),   S(-37, -94),   S(-11, -36),   S(-22, -48),   S(-19, -70),   S(-18, -52),   S( -9, -41),
            S(-20, -36),   S(-16, -56),   S(-17, -34),   S( -8, -31),   S(-11, -30),   S(-11, -39),   S( -8, -48),   S(-17, -41),
            S(-12, -36),   S( -2,  -4),   S(-17, -39),   S(-20, -34),   S( -4, -21),   S(-25, -63),   S(-16, -51),   S(-13, -42),

            /* queens: bucket 15 */
            S(-10, -29),   S(-13, -29),   S(  8, -10),   S(-28, -80),   S(-16, -62),   S(  0,   2),   S(  0,   0),   S(  0,   0),
            S(-14, -45),   S(  6, -14),   S(-12, -43),   S( -7, -47),   S( -5, -43),   S( 20,  27),   S(  0,   0),   S(  0,   0),
            S( -4,  -7),   S( -9, -43),   S(-45, -92),   S(-22, -58),   S( -9, -48),   S( 24,  35),   S(  4, -11),   S( -7, -26),
            S(  3,   8),   S(-23, -50),   S(-24, -63),   S(-22, -80),   S(-10, -39),   S(  0, -13),   S( -1, -17),   S( -7, -33),
            S( -5, -12),   S(-21, -53),   S(-45, -124),  S(-29, -67),   S( -2, -39),   S( -2, -12),   S(  5, -13),   S(  1, -38),
            S( -3, -13),   S(-20, -62),   S(-22, -48),   S(-20, -65),   S(-30, -54),   S(-23, -74),   S(-13, -28),   S( -2, -30),
            S(-13, -38),   S(-14, -23),   S(-21, -62),   S(-14, -33),   S(-30, -71),   S(-12, -39),   S( -1,   2),   S( -5,  -9),
            S(-27, -58),   S(-39, -93),   S(-20, -45),   S(-22, -48),   S(-33, -64),   S(  7,   5),   S(  1,  -8),   S(  7,  11),

            /* kings: bucket 0 */
            S( -1, -15),   S( 32, -14),   S( 35, -22),   S( 38, -25),   S( -3,  -5),   S( 35, -24),   S( 13,  13),   S( 38, -47),
            S(-24,   8),   S( -8,  21),   S(  5,  -8),   S(-58,  22),   S(-34,  25),   S(-18,  15),   S(-10,  53),   S(-29,  46),
            S( 41, -24),   S( 50, -11),   S( -1,  -9),   S(-16,  -4),   S(-28,  -3),   S(-35, -12),   S(-64,  30),   S(-44,  19),
            S(-31, -20),   S(-32,  -3),   S(-15,  -7),   S(-96,  15),   S(-28,  13),   S(-48,   3),   S( 31, -14),   S(-75,  44),
            S(-87, -38),   S( 70, -27),   S(-31, -25),   S( 34, -11),   S(-71,   2),   S( 18,   5),   S( -7,  -5),   S(  9,   2),
            S( 15, -132),  S( 66, -45),   S( 84, -64),   S(  2, -23),   S( 41, -14),   S(  1, -33),   S( 36, -21),   S(-21, -11),
            S(  0,   0),   S(  0,   0),   S( 22, -41),   S( 60, -12),   S( 30, -10),   S(-13, -14),   S(-30, -28),   S(-16, -24),
            S(  0,   0),   S(  0,   0),   S( -6, -86),   S( 24, -56),   S( 21, -21),   S( 10, -29),   S( 22, -22),   S( 10,  13),

            /* kings: bucket 1 */
            S(  3,  -8),   S( 19,  -9),   S( 24, -19),   S( 47, -11),   S(-12,  -3),   S( 27, -12),   S(  0,  37),   S( 24,   4),
            S(-37,  22),   S(-11,  27),   S(  0,  -4),   S(-72,  35),   S(-40,  25),   S(-25,  19),   S(  2,  31),   S( -6,  14),
            S( 15, -28),   S(-45,   2),   S(-12, -16),   S(  0, -10),   S(-35,  -3),   S(  3, -15),   S(  2,  -4),   S( 33, -15),
            S( 54, -18),   S( 48, -21),   S(-18,  -8),   S(  4,   1),   S( -7,   1),   S(-64,  20),   S(-30,  16),   S(-43,  26),
            S( 12, -41),   S( 23, -32),   S( 22, -39),   S( 37, -26),   S( 36, -25),   S(  8, -23),   S( 30, -15),   S( 31,  -4),
            S( 51,  -2),   S( 50, -38),   S( 26,  -9),   S( 63, -12),   S( 53, -20),   S( 24,  -3),   S( -2,  11),   S(-22,   4),
            S(-19, -39),   S( 27,  44),   S(  0,   0),   S(  0,   0),   S( -4,  -8),   S( -9,  14),   S( -9,  37),   S(-35, -19),
            S(-23, -116),  S( -4,  -1),   S(  0,   0),   S(  0,   0),   S(  8,  -8),   S(  0, -17),   S( 12,  25),   S(-20, -94),

            /* kings: bucket 2 */
            S( 47, -67),   S(  6,   1),   S( 35, -37),   S( 52, -13),   S( -3,   0),   S( 45, -21),   S(  3,  35),   S( 37, -19),
            S(-30,  16),   S( -2,  31),   S(-23,  12),   S(-26,  12),   S(-25,  15),   S(-20,   8),   S( 10,  22),   S(-18,  24),
            S( 16, -22),   S(-24,   1),   S(-41,  -1),   S(-11, -14),   S(  4, -12),   S(-12, -25),   S(  7, -14),   S( 22, -15),
            S(-27,  16),   S( -4,   6),   S( 16, -10),   S(-32,   5),   S(-23,   2),   S(-17, -13),   S(  2, -17),   S( 76, -35),
            S(-32,  -6),   S(  5, -14),   S( 32, -30),   S( 21, -23),   S( 52, -37),   S(-19, -36),   S( 31, -30),   S( 31, -39),
            S( -8,  -9),   S( 16,  -4),   S(-32,  -2),   S( 59, -39),   S( 61, -16),   S( 40,   4),   S( 71, -20),   S( 77, -22),
            S( 11,  -9),   S( -1,  14),   S(-23,  -2),   S(-16, -16),   S(  0,   0),   S(  0,   0),   S(  6,  28),   S( -9, -31),
            S(-12,  -5),   S( -7, -30),   S( 21, -36),   S(  3,  -3),   S(  0,   0),   S(  0,   0),   S(  2,   7),   S( -5, -150),

            /* kings: bucket 3 */
            S( 26, -85),   S(  6, -13),   S( 39, -58),   S( 10, -32),   S( -1, -41),   S( 45, -41),   S(  1,  18),   S( 17, -36),
            S(-33,  20),   S(-35,  29),   S(-26, -11),   S(-53,   3),   S(-61,  10),   S(-20, -10),   S( -9,  20),   S(-19,  15),
            S(-48, -23),   S(  3, -27),   S(-20, -28),   S(-36, -23),   S( -7, -18),   S( 12, -49),   S( 30, -33),   S( 56, -38),
            S(-77,  42),   S(-82,  13),   S(-70,   1),   S(-91,  10),   S(-74,   5),   S(-93,  -9),   S(-57,  -8),   S(-58, -22),
            S(-18, -34),   S(-31, -26),   S(-24, -23),   S(-66,  -8),   S(-17, -32),   S(-11, -46),   S( -2, -50),   S( -7, -62),
            S( -9, -20),   S( 39, -46),   S( 18, -48),   S(  2, -39),   S( 20, -43),   S(175, -82),   S(204, -65),   S(120, -121),
            S(-64,  -6),   S( 84, -75),   S(-10, -30),   S(-39, -35),   S(-13, -46),   S( 73, -40),   S(  0,   0),   S(  0,   0),
            S(  0,  -7),   S(-12, -29),   S( 17, -13),   S(-19, -37),   S(  8, -78),   S( 43,  -6),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 4 */
            S(-18,   3),   S( 22,  18),   S(-51,  34),   S( 18,   3),   S(-37,  -1),   S( 13,   9),   S( 56,   2),   S( 57,  -4),
            S(-46,  24),   S( 11,  13),   S(-14,  23),   S(-25,   6),   S( 33,  -5),   S( 29,  -3),   S( 42,   6),   S( 51,  -4),
            S(-16,  11),   S( -1,  -1),   S(-37,  14),   S(-16,   2),   S( -9,   4),   S( 22, -30),   S(-52,   3),   S( 17,  -8),
            S( 11, -15),   S(  6,   5),   S( 81,  -9),   S(  7,  -1),   S( 14,   7),   S( -2,  -2),   S(  9,  20),   S( 11,   4),
            S(  0,   0),   S(  0,   0),   S( 17,  -4),   S( -9, -10),   S(-11,   2),   S( -1,  -3),   S( -2,  -1),   S( -7,  -3),
            S(  0,   0),   S(  0,   0),   S(  9,   9),   S(  1, -22),   S(-20,  -3),   S(-54,  -2),   S( -3, -19),   S(-12,  12),
            S(  8,  26),   S(  7,  40),   S( 21, -38),   S(  4, -16),   S(-17,  24),   S(-29,   6),   S( -1, -11),   S( 20,   0),
            S(-24,  47),   S(-15, -15),   S( -3,  23),   S(-10, -10),   S( -5, -35),   S( -7, -28),   S(-11, -17),   S(  4,  25),

            /* kings: bucket 5 */
            S( 52,  -6),   S( 33,  11),   S(-17,  20),   S(-40,  29),   S(  5,  11),   S( 21,   5),   S( 41,  20),   S( 51,  12),
            S( 23,  -2),   S( 21,   2),   S( 18,  -5),   S( 56,  -7),   S( -1,   6),   S( 42,  -1),   S( 63,  12),   S( 60,  -3),
            S( -4,   0),   S(-85,  19),   S( -2, -10),   S(-34,   3),   S(-16,   2),   S(-42,  -2),   S( 32,  -8),   S(  8,  -5),
            S(  7, -26),   S( 49,  -2),   S(  1,   5),   S( 35,   1),   S( 41,  -1),   S( 18,  -1),   S(-44,  19),   S(  7,   5),
            S(-56, -14),   S( -4, -26),   S(  0,   0),   S(  0,   0),   S( -4,   0),   S(-17,  -7),   S(-10,   0),   S(-74,  15),
            S(-48,  15),   S(-19,  13),   S(  0,   0),   S(  0,   0),   S( -6,  12),   S(-63,  16),   S(-72,  30),   S(-31,  11),
            S(-14, -16),   S(-15,  30),   S(  2,  47),   S( 10, -16),   S(-17,  -3),   S( 15,   2),   S( 17,   6),   S(-34, -11),
            S(-25, -36),   S( -3,  51),   S( -1,  36),   S( -4,  61),   S( 10,  54),   S( -5,  24),   S(  6,  19),   S(  7,  13),

            /* kings: bucket 6 */
            S( 35,  -7),   S( -4,  20),   S(  1,   2),   S( 28,   5),   S(-12,  26),   S( 13,  15),   S( 28,  37),   S( 24,  21),
            S( 63, -19),   S( 44,   3),   S( 11,   4),   S( 56,  -9),   S( 18,   7),   S( 22,   7),   S( 34,  19),   S( 42,   6),
            S(  5,  -9),   S(-42,  21),   S( 13,  -7),   S(  3,  -4),   S(  1,  -2),   S(-40,  -1),   S( 20,  -1),   S(-50,  21),
            S(-31,   6),   S( 42,   1),   S( -7,   1),   S( -4,   8),   S( 44,   4),   S( -2,  -2),   S( 70, -13),   S( 15,  -9),
            S(-15,   2),   S(-28,   5),   S(  1, -19),   S(-22,  11),   S(  0,   0),   S(  0,   0),   S(-12, -16),   S(-66, -10),
            S(-51,  10),   S(  4,   1),   S(-61,  18),   S(-43,   2),   S(  0,   0),   S(  0,   0),   S(-14,  31),   S(-94,  23),
            S(-10, -27),   S(  5,  11),   S(-31,   5),   S(-13,  -6),   S( -4,  16),   S(-10,  17),   S( 24,  31),   S(-57,  -7),
            S( -9,  38),   S( 10,  12),   S( 14,  41),   S(-16,  31),   S(-10,  17),   S( -7,  27),   S(-23,  53),   S(-40, -38),

            /* kings: bucket 7 */
            S( 86, -61),   S( 24, -12),   S(  4, -10),   S( 19,   5),   S(-41,  24),   S(-30,  31),   S(-18,  49),   S(  2,  19),
            S( 50, -13),   S( -7,   7),   S( 32, -19),   S(-18,   3),   S(-10,  11),   S(-23,  17),   S(  9,  22),   S(  1,  31),
            S( 26, -17),   S( -3,  -5),   S(-16,   0),   S( -5,  -4),   S(-26,   4),   S(-64,  15),   S(-20,  10),   S(-18,   5),
            S(-11,   6),   S(  4,  -3),   S(  5,  -4),   S(  9,  -1),   S(  6,  -3),   S( 34, -16),   S( 19,  -8),   S( 20,  -7),
            S(-15,  10),   S(-56,  23),   S(-42,   7),   S(-18,   6),   S(  5,  -8),   S( 47, -35),   S(  0,   0),   S(  0,   0),
            S( -9,   2),   S(-25, -10),   S( 14, -12),   S(-25,   2),   S( -5,  -2),   S( -5,   4),   S(  0,   0),   S(  0,   0),
            S(  9,  20),   S( 19, -16),   S( 17,  -2),   S( 30, -29),   S( 25, -15),   S(-11,  -8),   S( 13,  20),   S(  2, -34),
            S(-10, -17),   S( -9, -11),   S( 14,  -5),   S( 10,  19),   S(  7, -27),   S(-31,  -3),   S( -3,  40),   S(-16,  25),

            /* kings: bucket 8 */
            S(-70, 111),   S(-38,  48),   S(-86,  68),   S(-18, -14),   S(-40,  14),   S( 14,  25),   S( 75,  -3),   S(-78,  71),
            S(  6,  66),   S( 39,  14),   S(-33,  50),   S(  2,   9),   S( 34,  -4),   S(  9,  -2),   S( 16,  24),   S(  4,  36),
            S(  0,   0),   S(  0,   0),   S( 20,  23),   S( 22,   8),   S( 30,  -3),   S(-13,  -1),   S( 28,   6),   S( 18, -15),
            S(  0,   0),   S(  0,   0),   S( 29,   5),   S( 57, -23),   S( 13,   2),   S( 60, -15),   S( 60,  -7),   S( -1, -10),
            S(  3,   6),   S(  6,   9),   S( 25,   6),   S(  5, -15),   S( 11, -12),   S(  8,   6),   S( -6,  11),   S(  9,  -4),
            S( -3,  -9),   S( -6,  -5),   S(-15,  -3),   S( 14,   5),   S(-19, -10),   S(  6,  -3),   S(  8,   7),   S(-23, -35),
            S(  1, -16),   S(-24, -53),   S( -1, -19),   S(  5,   3),   S( -2, -34),   S( 12,  13),   S(  1, -11),   S( 14, -32),
            S( -1,  12),   S( -9, -38),   S(  6, -26),   S( -4, -31),   S(  5,  -3),   S(-13, -29),   S( 11,  -5),   S(  1,   2),

            /* kings: bucket 9 */
            S(-102,  62),  S(-75,  38),   S(-91,  37),   S(-90,  34),   S(-102,  39),  S(-109,  51),  S( 41,  21),   S(  9,  42),
            S( -2,  31),   S( 89,  -6),   S(  7,  14),   S( 28,   9),   S(105,   5),   S( 23,   4),   S( 34,  24),   S(-13,  31),
            S(-33,  25),   S( -3,  26),   S(  0,   0),   S(  0,   0),   S( 26,  11),   S( 26,  -5),   S( 46,   1),   S( -7,  -5),
            S( -1, -14),   S( 19, -10),   S(  0,   0),   S(  0,   0),   S( 37,  -2),   S( 37, -15),   S( 31,   3),   S( -1,  -4),
            S(-11, -13),   S(-13,  14),   S( -1,   5),   S( -1, -19),   S( 33, -24),   S(-10,  -4),   S(-11,   3),   S(-20,   4),
            S( -7,  -2),   S(  2,   8),   S( 19,   3),   S(-17,  12),   S(-17,  17),   S(-21,   8),   S(-64,  21),   S(  7,  22),
            S( -5,  -3),   S( -6, -20),   S( -3,  -8),   S( 13,   1),   S( -9,  25),   S( 31,   3),   S(  1, -17),   S(  2,  -6),
            S( 15,  77),   S(  4,   1),   S( 15,  -8),   S(  0,  -5),   S( -1, -31),   S(  3, -34),   S( 14, -10),   S( 17,  54),

            /* kings: bucket 10 */
            S(-31,  19),   S(-21,  20),   S(-80,  34),   S(-123,  57),  S(-105,  33),  S(-193,  72),  S(-65,  63),   S(-147, 102),
            S( 53,  -6),   S(  9,   6),   S( 25, -14),   S( 59,  -4),   S( 95,  -4),   S( 82,  -8),   S( 33,  18),   S( 15,  25),
            S( -4,  13),   S( 59,   1),   S( 12,   1),   S( -9,   8),   S(  0,   0),   S(  0,   0),   S( 20,  19),   S(-101,  35),
            S( 28, -13),   S(  2,   5),   S( 40, -13),   S( 47, -17),   S(  0,   0),   S(  0,   0),   S( 17,   8),   S( 27, -26),
            S( 20, -13),   S( 21,   7),   S(  6,   1),   S( 14, -26),   S(  6, -14),   S( 28,   1),   S(  3,  11),   S(-21,   1),
            S(-21,  16),   S(-26,   9),   S(-15,  14),   S(  9,  16),   S(-14,  11),   S(-19,   3),   S(  4,   3),   S(-17,  14),
            S(  5, -16),   S(  6,  10),   S( 13,  -9),   S( 22,   8),   S( 29,  -2),   S(  2, -13),   S( 23, -13),   S(  7,   0),
            S(  6,  30),   S(  8, -18),   S( 11,  20),   S(  2,   9),   S(  7,   3),   S( 12,  17),   S(  8, -17),   S(  6,  42),

            /* kings: bucket 11 */
            S(-29,  38),   S(-21,  11),   S(-28,  14),   S(-66,  15),   S(-81,  14),   S(-198,  79),  S(-126,  93),  S(-249, 149),
            S( 28, -25),   S(  3,  10),   S(  6, -30),   S( 51,   5),   S( 22,  -3),   S( 58,  19),   S( 59,   6),   S( 20,  33),
            S(-20,   1),   S( 34, -10),   S( 28, -24),   S( 31, -10),   S( 20, -11),   S( 52,  15),   S(  0,   0),   S(  0,   0),
            S( 11,  29),   S( -3,  -8),   S( 25,  -5),   S(103, -17),   S( 55, -30),   S( 60,  -2),   S(  0,   0),   S(  0,   0),
            S( 23,  -4),   S(  5,  -5),   S( 14, -12),   S( 43, -10),   S( 34, -11),   S( -6, -10),   S( 24,  -5),   S(  4,  18),
            S( -7,  23),   S( -9,   6),   S( 28, -16),   S(  4, -12),   S( 13,  -6),   S(-10,  -2),   S(-31,  -3),   S(  3, -10),
            S( 10,  -1),   S(  3,   8),   S( -1,   8),   S(-14,   8),   S( 24,  -4),   S( 21,   1),   S( 23, -15),   S( 11, -15),
            S( 12,  40),   S(  1, -33),   S(  3, -37),   S(  8, -12),   S( -7,  -4),   S(-14, -26),   S( 24, -17),   S( 14,  49),

            /* kings: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(-11,  37),   S(-35,  37),   S( -5,   4),   S(-30,  -2),   S(  0,  14),   S(-31,  85),
            S(  0,   0),   S(  0,   0),   S( 28,  67),   S( 37, -21),   S( 34,  19),   S(  9,   8),   S( 17,  18),   S(  3,   7),
            S(  7,  -3),   S( 14, -95),   S( 26, -17),   S( 17,  17),   S(-11,  -3),   S(  0, -10),   S(  2, -22),   S(  9,  24),
            S(-12, -37),   S( 14,  39),   S( -6,   4),   S( 19, -35),   S(  9, -21),   S(  5,  -1),   S(-21,  -7),   S(  2,  30),
            S( 28,  79),   S(  4,  15),   S(-11, -13),   S( 14, -27),   S( -2,  -3),   S( -4,   8),   S( -5,  -2),   S(-20,  -5),
            S(  3,   7),   S( -2,  -6),   S( -1,   7),   S(-10, -22),   S(-17, -41),   S( -1,  29),   S(-13, -28),   S( 12,  21),
            S( 10,  47),   S( -3, -15),   S( -7,  24),   S( 14,   8),   S(-14, -16),   S( -7,  -5),   S( 22,  29),   S(  9,  -3),
            S(-12, -27),   S(  4,  -5),   S( -4, -16),   S(  1, -26),   S( -2, -26),   S(  3,  35),   S(-13, -29),   S(  1,   0),

            /* kings: bucket 13 */
            S(-64,  81),   S(-16,  82),   S(  0,   0),   S(  0,   0),   S(-16,  68),   S(-26,  32),   S( 43,   6),   S(-26,  66),
            S(-13,  21),   S( -7,   9),   S(  0,   0),   S(  0,   0),   S( 36, -18),   S( 21, -25),   S( 18,  17),   S( 16,  30),
            S(-13,  31),   S( 29,  39),   S( 15,   0),   S( -2, -50),   S( 43, -15),   S(  1,   4),   S(-26,  13),   S(-13,  17),
            S( -5, -23),   S(-24,  -8),   S(-14, -23),   S( -6, -55),   S( -1, -47),   S(  2, -13),   S(-14,  14),   S(-19, -27),
            S(  1,   9),   S( -8,   3),   S(-10,  19),   S(  0, -34),   S( -3,  -7),   S( -5,  -7),   S(  6,  -1),   S(  0,  17),
            S(  6,  36),   S( -6,  26),   S(-11,  18),   S( -3,  18),   S(-23,  -2),   S(-17, -12),   S(-10, -17),   S(  7,  35),
            S(  4,  15),   S(-16, -25),   S(-13,  -8),   S( 10,  11),   S( -5, -15),   S( -9, -33),   S( -6, -43),   S( 16,  33),
            S( -4, -21),   S( 12,  41),   S(  9,  30),   S( -6, -33),   S(  8,  10),   S( -8,   6),   S(  4, -32),   S( 16,  31),

            /* kings: bucket 14 */
            S(-20,  50),   S( -5,  16),   S(-19,  12),   S(-26,  25),   S(  0,   0),   S(  0,   0),   S( 22,  47),   S(-125, 101),
            S(-27,  13),   S(  4,  -6),   S( 27, -10),   S( 22,  -9),   S(  0,   0),   S(  0,   0),   S( 38,   2),   S(-41,  20),
            S(  1,  35),   S( 45,  -4),   S( 20,  -3),   S(-13,  -8),   S(  5, -61),   S(  3,  38),   S( 17,  23),   S(-22,  19),
            S( 17,  14),   S( -5, -12),   S(-15, -29),   S(  5, -36),   S(-13, -47),   S( 35,  -2),   S( -5,  -3),   S( 19,  13),
            S(-13, -12),   S( 11,  -1),   S(  1,   3),   S(-25,  -7),   S(-23,  -7),   S(-18,  12),   S(-13,  21),   S(  1,  14),
            S(  8,  27),   S( -7,  39),   S( -1,  17),   S(  3,  25),   S( -7,   0),   S( -1, -10),   S( -9, -23),   S( -4,  21),
            S(  9, -11),   S( 10, -14),   S( 18,  22),   S( 14,  25),   S(  2,   1),   S( -3, -36),   S(-19, -60),   S(  3,  69),
            S( -5,   0),   S(  6,  35),   S(-11,   6),   S( -7, -21),   S(  1,  39),   S(  0,  -4),   S( -9, -44),   S( -3, -10),

            /* kings: bucket 15 */
            S(  1,  74),   S(  4,   4),   S(  3,  32),   S( -7,   9),   S(-36,  43),   S(-57,  79),   S(  0,   0),   S(  0,   0),
            S( 12, -23),   S( -2,   7),   S( 12, -14),   S( 43,  21),   S( 55, -31),   S( 44,  70),   S(  0,   0),   S(  0,   0),
            S(-18, -22),   S( 12, -10),   S( 16, -27),   S( -8,  -8),   S( 12, -17),   S( 53,   2),   S( 22, -31),   S(-12, -70),
            S(  4,  49),   S(-11, -14),   S( -7,   8),   S(  4,   0),   S(  8, -23),   S( 22,  -3),   S( -4,  10),   S( -1,  39),
            S( 14,  31),   S(-12,   7),   S( -9,  14),   S( -9, -11),   S( -1, -22),   S(-13,  18),   S(-19,  14),   S(  2,   6),
            S( -4,  17),   S(-25,  -9),   S( -9, -30),   S( -6,   6),   S(  3,   2),   S(-32,  -9),   S(-50, -12),   S( -3, -11),
            S(-10,  10),   S(  2,   4),   S(-19, -11),   S(-19,   0),   S( 26,  11),   S( -6,  -4),   S( -2, -28),   S( -1,  38),
            S(-19, -42),   S(  8,  33),   S( -8, -12),   S( 15,  35),   S(  8,  35),   S(  0,   9),   S( -1, -15),   S(  3,   5),

            #endregion

            /* mobility weights */
            S(  7,   7),    // knights
            S(  5,   3),    // bishops
            S(  3,   3),    // rooks
            S(  2,   1),    // queens

            /* trapped pieces */
            S(-16, -206),   // knights
            S(  2, -122),   // bishops
            S(  2, -92),    // rooks
            S( 20, -96),    // queens

            /* center control */
            S(  2,   6),    // D0
            S(  2,   4),    // D1

            /* squares attacked near enemy king */
            S( 17,  -2),    // attacks to squares 1 from king
            S( 16,   0),    // attacks to squares 2 from king
            S(  5,   1),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S( -3,  21),    // friendly pawns 1 from king
            S( -7,  18),    // friendly pawns 2 from king
            S( -7,  12),    // friendly pawns 3 from king

            /* castling right available */
            S( 43, -27),

            /* castling complete */
            S(  9,  -9),

            /* king on open file */
            S(-53,  12),

            /* king on half-open file */
            S(-19,  23),

            /* king on open diagonal */
            S(-15,  14),

            /* king attack square open */
            S(-10,   0),

            /* isolated pawns */
            S(  0, -16),

            /* doubled pawns */
            S(-10, -37),

            /* backward pawns */
            S( 10, -15),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 10,  12),   S(  5,  -3),   S(  6,   4),   S( 10,  33),   S( 31,  33),   S( -1, -32),   S(-18,  45),   S( -5, -28),
            S(  2,   7),   S( 26,   4),   S(  7,  31),   S( 17,  40),   S( 43,  -1),   S( -2,  31),   S( 17,  -1),   S(  1,   0),
            S(-13,  27),   S( 17,   8),   S( -1,  61),   S( 23,  74),   S( 31,  29),   S( 26,  36),   S( 31,   4),   S(  4,  27),
            S( 26,  45),   S( 20,  50),   S( 39,  82),   S(  9,  89),   S( 83,  55),   S( 76,  32),   S( 27,  49),   S( 15,  47),
            S(127,  62),   S(158,  54),   S( 66, 160),   S(154, 147),   S(206,  93),   S(178, 112),   S(181,  56),   S( 89,  50),
            S(120, 283),   S(172, 404),   S(147, 323),   S(145, 287),   S(118, 269),   S(103, 253),   S( 69, 249),   S( 30, 160),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  3,  11),   S(-11,  17),   S(-23,  23),   S(-38,  62),   S( 17,  -8),   S(-16,   6),   S( -1,  40),   S( 31,   0),
            S( 11,  24),   S( -5,  36),   S(-15,  32),   S( -4,  27),   S(-13,  32),   S(-39,  39),   S(-37,  55),   S( 24,  17),
            S( -8,  22),   S( -8,  27),   S(-15,  30),   S( 14,  26),   S(  0,  17),   S(-36,  35),   S(-56,  62),   S(-19,  36),
            S( 17,  44),   S( 50,  50),   S( 16,  48),   S( 13,  27),   S( 18,  49),   S( 41,  49),   S( 10,  64),   S(-42,  81),
            S( 19,  88),   S( 88, 117),   S( 78,  73),   S( 40,  58),   S(-24,  47),   S( 42,  69),   S(  3, 139),   S(-37,  80),
            S(252,  74),   S(258, 111),   S(292, 119),   S(281, 130),   S(263, 142),   S(246, 137),   S(257, 136),   S(298,  98),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 41,  14),   S(  4,  16),   S( 15,  31),   S(  6,  60),   S( 70,  39),   S( 39,  12),   S( 23,  -9),   S( 49,  16),
            S(  6,  15),   S(  4,  11),   S( 22,  13),   S( 19,  32),   S( 16,  16),   S( -3,   9),   S(  8,   7),   S( 30,  -4),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -6, -15),   S( -4, -11),   S(-22, -13),   S(-19, -32),   S(-16, -16),   S(  3,  -9),   S( -8,  -7),   S(-30,   4),
            S(-41, -14),   S( -4, -16),   S(-15, -31),   S( -6, -60),   S(-70, -39),   S(-39, -12),   S(-23,   9),   S(-49, -16),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 31,   6),   S( 39,  13),   S( 55,  17),   S( 55,  13),   S( 40,  26),   S( 36,  16),   S( 16,  13),   S( 47,  -4),
            S(  1,   5),   S( 24,  21),   S( 21,  17),   S( 23,  44),   S( 29,  15),   S( 13,  17),   S( 32,   6),   S( 21,   0),
            S( -4,  12),   S( 23,  31),   S( 53,  40),   S( 47,  40),   S( 58,  35),   S( 57,  14),   S( 32,  25),   S( 27,  10),
            S( 44,  66),   S(114,  39),   S(124,  74),   S(142,  75),   S(146,  58),   S( 97,  61),   S( 95,  26),   S( 78,  16),
            S( 47,  80),   S(213,  55),   S(286,  82),   S(228,  79),   S(249, 118),   S(223,  63),   S(238,  52),   S(-47, 107),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S( -6,  17),   S( -4,  45),   S( 35,  78),   S(-23, 212),

            /* enemy king outside passed pawn square */
            S(-45, 228),

            /* passed pawn/friendly king distance penalty */
            S( -3, -17),

            /* passed pawn/enemy king distance bonus */
            S(  6,  25),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 74, -55),   S( 48,   3),   S( 45,  13),   S( 54,  36),   S( 44,  16),   S(244, -52),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 10,  -7),   S( 20,  48),   S( 15,  40),   S( 14,  68),   S( 41,  75),   S(201,  64),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-25, -16),   S( -7, -45),   S(  9, -52),   S(-21, -27),   S( 23, -50),   S(238, -100),  S(  0,   0),    // blocked by rooks
            S(  0,   0),   S(  2,  -8),   S( 47, -43),   S( 10,  -3),   S( 15, -56),   S(  8, -171),  S( 57, -260),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(-12,   3),   S( 42,  -1),   S( 44, -21),   S(-27, -21),   S(236,  -6),   S(301, -47),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S(  5,  44),

            /* knight on outpost */
            S(  4,  28),

            /* bishop on outpost */
            S( 13,  33),

            /* bishop pair */
            S( 35,  88),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4,   0),   S( -1,  -5),   S( -2,  -7),   S(-10, -31),   S( -3, -17),   S(-21,   1),   S(-19,  -5),   S( -5,  -1),
            S( -5, -10),   S( -9,  -7),   S(-12, -13),   S( -8, -12),   S(-11, -18),   S(-14,  -7),   S(-13,  -7),   S( -3,  -7),
            S( -6,  -7),   S(  6, -28),   S( -4, -38),   S( -6, -47),   S(-18, -28),   S(-15, -24),   S(-15, -15),   S( -6,  -4),
            S(  9, -26),   S( 11, -37),   S( -3, -28),   S( -8, -38),   S(-13, -29),   S(-12, -21),   S( -1, -28),   S(  3, -22),
            S( 21, -17),   S( 22, -46),   S( 24, -53),   S( 12, -47),   S( 14, -39),   S( 11, -50),   S( 22, -61),   S( 17, -12),
            S( 44, -17),   S( 85, -81),   S( 81, -82),   S( 43, -81),   S( 69, -97),   S(118, -87),   S( 84, -109),  S( 86, -75),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 40,  -2),

            /* rook on half-open file */
            S( 11,  36),

            /* rook on seventh rank */
            S(-27,  50),

            /* doubled rooks on file */
            S( 21,  23),

            /* queen on open file */
            S(-10,  33),

            /* queen on half-open file */
            S(  4,  36),

            /* pawn push threats */
            S(  0,   0),   S( 29,  32),   S( 32, -14),   S( 36,  21),   S( 30, -13),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 71,  83),   S( 57, 103),   S( 62,  85),   S( 56,  44),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-13,   7),   S( 50,  37),   S( 91,   8),   S( 42,  20),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 31,  70),   S(  3,  22),   S( 57,  54),   S( 41, 105),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -8,  56),   S( 57,  60),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-23,  19),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats

            /* tempo bonus for side to move */
            S( 18,   9),
        };
    }
}
