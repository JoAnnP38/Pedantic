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

        // Solution sample size: 16000000, generated on Sun, 24 Dec 2023 08:35:06 GMT
        // Solution K: 0.003850, error: 0.083772, accuracy: 0.5047
        private static readonly Score[] defaultWeights =
        {
            /* piece values */
            S( 98, 177),   S(428, 551),   S(443, 623),   S(521, 1041),  S(1328, 1857), S(  0,   0),

            /* friendly king piece square values */
            #region friendly king piece square values

            /* pawns: bucket 0 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 88, -108),  S(130, -75),   S( 25, -31),   S(-31,  10),   S(-26, -11),   S(-17,  -9),   S(-33, -10),   S(-40, -23),
            S( 96, -91),   S( 81, -70),   S(  1, -51),   S(-14, -62),   S(-23, -33),   S(-17, -32),   S(-37, -16),   S(-37, -32),
            S( 93, -83),   S( 68, -45),   S( 19, -48),   S( 10, -63),   S(  1, -56),   S(  4, -48),   S(-11, -34),   S(-29, -26),
            S( 58, -21),   S( 48, -19),   S( 32, -30),   S( 30, -58),   S(  9, -32),   S(-19, -25),   S(-13, -25),   S(-33,   2),
            S( 78,  51),   S( -6,  43),   S( 42,  18),   S( 77, -44),   S( 53,   7),   S(-11,   0),   S(-25,  16),   S(-43,  64),
            S( 93,  62),   S( 75, 104),   S( 50, -30),   S( 30,  -9),   S( 32,   2),   S( 31,  35),   S( 25,  19),   S(-17,  64),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 19, -27),   S( 18, -31),   S( 27, -18),   S(-23,   8),   S(-12, -16),   S( 12, -24),   S(-23,   3),   S(-38,  19),
            S(  7, -20),   S(  5, -25),   S( -7, -28),   S(-18, -38),   S(-16, -30),   S( -5, -33),   S(-35,  -4),   S(-49,   4),
            S( 15, -23),   S(  8, -19),   S( 11, -33),   S( 13, -46),   S( -3, -33),   S(  6, -35),   S( -9, -13),   S(-26,   3),
            S( 22,  11),   S( 11, -14),   S( 34, -20),   S( 18, -22),   S( 10, -11),   S( 19, -23),   S(-25,  -1),   S(-29,  28),
            S( 26,  61),   S(-40,  39),   S(-30,  41),   S( -4,  15),   S( 34,  23),   S(  1,  29),   S(-29,  40),   S(-36,  73),
            S( 54,  63),   S( 40,  38),   S(-43,  24),   S( 17,  42),   S(-16,  24),   S(-45,  54),   S( -7,  37),   S(-49,  99),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-27,   6),   S(-27,   7),   S(-10,  -8),   S(-29,   2),   S(-25,  -1),   S( 19, -20),   S( 12, -43),   S( -5, -26),
            S(-29,   3),   S(-43,   6),   S(-35, -21),   S(-25, -33),   S(-16, -26),   S( -7, -17),   S(-19, -21),   S(-27, -18),
            S(-28,   7),   S(-38,   4),   S( -8, -39),   S( -3, -45),   S( -8, -25),   S( 17, -32),   S( -1, -22),   S(-12, -17),
            S(-51,  39),   S(-27,  -2),   S(-18, -13),   S(  3, -21),   S(  2, -12),   S(  9,  -7),   S( -8,   9),   S(-12,  10),
            S(-28,  63),   S(-87,  43),   S(-49,  20),   S(-58,  35),   S(-10,  67),   S(-20,  53),   S(-41,  59),   S(-26,  96),
            S(-82, 104),   S(-94,  83),   S(-131,  53),  S(-94,  49),   S(-59,  78),   S(-53,  86),   S(-33,  62),   S(-47, 103),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-27,  -8),   S(-34,  -1),   S(-30,  -2),   S( -9, -59),   S(-13, -19),   S( 41, -25),   S( 76, -55),   S( 50, -78),
            S(-34, -17),   S(-44, -11),   S(-30, -30),   S(-29, -28),   S(-16, -38),   S(  4, -34),   S( 39, -42),   S( 43, -60),
            S(-30, -11),   S(-18, -23),   S( -7, -39),   S( -9, -50),   S(  5, -55),   S( 23, -48),   S( 26, -36),   S( 47, -54),
            S(-40,  22),   S(-16, -23),   S( -6, -25),   S( 10, -41),   S( 28, -49),   S( 14, -30),   S( 15,  -7),   S( 45,  -9),
            S( -2,  47),   S(-32,  10),   S(  1, -12),   S( 17,  -2),   S( 71,   6),   S( 57,   5),   S( 19,  68),   S( 46,  83),
            S(-34, 113),   S(-34,  60),   S(  0,  -2),   S(-33,   6),   S( 27,   4),   S( 37,  44),   S( 45,  71),   S( 34,  87),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-126,  40),  S(-11,   1),   S(-43,  12),   S( -3,  34),   S(-43,  -2),   S(-39,   8),   S(-50,   1),   S(-50,  -8),
            S(-61,  29),   S( 39,  -5),   S( 28, -24),   S( 52, -49),   S( 19, -36),   S(-45, -11),   S(-10, -23),   S(-22, -27),
            S(  8, -11),   S( 60, -22),   S( 32, -13),   S(-20, -21),   S(-41, -17),   S( -3, -26),   S(-50, -10),   S(-30,  -2),
            S( -3,   9),   S( 10,  18),   S( 81, -13),   S(  5,  -1),   S(  2, -14),   S(-44,  -3),   S(  7, -11),   S( 13,  -4),
            S( 23,  61),   S(  9,  42),   S( 42,  11),   S(-16,  -1),   S(  1,  -1),   S( 13,   6),   S( 10,   6),   S( 41,  34),
            S( 99,  56),   S( 74,  87),   S( 46,  30),   S( 25,  21),   S( 33, -12),   S( 12,  -4),   S(  8,   5),   S(-19,  46),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-108,  58),  S(-61,  40),   S(-34,  20),   S( 15,   7),   S(-39,  22),   S(-10,   6),   S(-16,   9),   S(-34,  26),
            S(-78,  39),   S(-51,  24),   S( 10,  -3),   S(-29,  18),   S(  9, -11),   S(-17, -13),   S( -7,  -5),   S(-45,  23),
            S(-65,  42),   S(-20,  17),   S( 60, -28),   S( 18, -23),   S( 47, -28),   S(-10, -14),   S( 21,  -8),   S(-43,  25),
            S(-35,  46),   S(  5,  15),   S( 53,  -7),   S( 41,   3),   S( -2,  -1),   S(-14,   0),   S( 36,  -7),   S( 23,  21),
            S( 42,  42),   S( 95,   0),   S( 47,  30),   S( 34,  24),   S(-21,  57),   S( 54,   2),   S( 48, -10),   S( 36,  42),
            S( 98,  15),   S(100,   7),   S( 92,  -7),   S( 48,  19),   S( 47,   5),   S( 51,  -5),   S( 12,  35),   S( 64,  36),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-50,  34),   S(-20,  13),   S( -1,   0),   S( -9,   7),   S( -3,  -4),   S(-18,   4),   S(-55,  10),   S(-55,  15),
            S(-46,  22),   S(-17,  -1),   S( -8, -21),   S( 10, -16),   S( 60, -31),   S( 43, -23),   S(-27,   0),   S(-62,  15),
            S(-36,  25),   S(  3,   3),   S( 11, -20),   S(  4, -19),   S( 50, -25),   S( 62, -31),   S( -1,  -9),   S(-33,  10),
            S(-25,  42),   S(-22,   9),   S(  5,  -7),   S( 16, -15),   S( 45,  -8),   S( 51,  -8),   S( 52, -12),   S( 31,   1),
            S(-25,  46),   S(  5,   2),   S(-20,   8),   S( 33,  -5),   S( 83,  26),   S(134,  11),   S( 94, -14),   S( 90,  14),
            S( 66,  15),   S( 29,  -2),   S( 44, -16),   S( 87, -21),   S( 83,   5),   S( 60,  12),   S( 90, -11),   S(118, -11),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-54, -19),   S(-53,  -5),   S(-13, -14),   S(-43,   2),   S( -3, -35),   S( 17, -19),   S(-14, -24),   S(-75,  -1),
            S(-67, -17),   S(-39, -26),   S(-23, -31),   S(-11, -46),   S( 22, -48),   S( 61, -49),   S( 45, -35),   S(-32, -13),
            S(-70,  -4),   S(-51, -16),   S(-24, -25),   S( -6, -39),   S(  3, -38),   S( 35, -32),   S( 39, -39),   S( 11, -25),
            S(-35,   0),   S(-62,  -8),   S(-63,  -9),   S(-16, -23),   S( 33, -34),   S( 28, -20),   S( 42, -17),   S( 57, -33),
            S( -6,  12),   S(-23, -19),   S(-16, -22),   S( 15, -37),   S(  7,  15),   S( 61,  -1),   S( 87,  11),   S(112,  15),
            S( 41, -13),   S( -5, -41),   S( 24, -53),   S( 36, -50),   S( 41, -57),   S( 39,  -4),   S( 31,  66),   S( 81,  36),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-69,  54),   S(-67,  61),   S(-35,  26),   S( -9, -17),   S( -5,  31),   S(-67,  25),   S(-46,   3),   S(-59,   5),
            S(-45,  37),   S(-35,  35),   S(-62,  34),   S(-28,   4),   S(-32,   6),   S(-23, -12),   S(-47, -29),   S(-25, -18),
            S(-68,  52),   S( -3,  45),   S( 12,  37),   S(-46,  30),   S(-21,  -6),   S(-20, -16),   S(-51, -11),   S( -3, -17),
            S(  5,  53),   S( -2,  78),   S( 74,  26),   S(-29,  37),   S(  5,   8),   S(-36,   2),   S(  2,  -5),   S( 12, -10),
            S( 26,  79),   S( 68,  69),   S( 62,  86),   S( 69,  68),   S(  5,  11),   S( 22,  30),   S( -6,   5),   S( 27,  16),
            S( 74,  96),   S( 91, 112),   S( 74, 118),   S( 50,  30),   S( 11,  -6),   S( -7, -38),   S(  6, -38),   S( 17, -14),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-90,  60),   S(-86,  50),   S(-48,  27),   S(  3,  10),   S(-13,  -5),   S(-93,  27),   S(-70,  18),   S(-93,  28),
            S(-98,  49),   S(-15,  24),   S(-64,  39),   S(-32,  29),   S(-88,   9),   S(-70,   5),   S(-104,  14),  S(-81,  24),
            S(-71,  55),   S(-53,  54),   S(-99,  68),   S(-99,  44),   S(-103,  45),  S(-89,  13),   S(-94,   9),   S(-37,  11),
            S(-17,  51),   S( 60,  40),   S( 12,  64),   S( 74,  48),   S(-19,  30),   S(-23,   6),   S( 57,  -7),   S( 73, -13),
            S( 63,  30),   S( 79,  49),   S( 61,  72),   S( 93,  88),   S( 74,  56),   S( 38,  12),   S( 44, -23),   S( 51, -12),
            S( 40,   9),   S( 80,  14),   S(143,  38),   S(114,  62),   S( 30,  44),   S( -2, -40),   S(  5, -30),   S( 21,  -6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-132,  36),  S(-120,  15),  S( 17, -19),   S( -5,  40),   S(-44,  -1),   S(-94,  44),   S(-137,  47),  S(-79,  36),
            S(-98,  11),   S(-57,  -4),   S(-65,   0),   S(-39,   4),   S(-125,  36),  S(-54,  22),   S(-134,  45),  S(-138,  47),
            S(-24,   6),   S(-82,  17),   S(-50,  15),   S(-96,  55),   S(-86,  49),   S(-30,  22),   S(-95,  39),   S(-108,  44),
            S( 25,   7),   S( 19,  -2),   S( 18,   9),   S(  2,  29),   S(  2,  53),   S( 38,  33),   S(  9,  18),   S( 39,   2),
            S( 61, -14),   S( 42,  -9),   S( 44,  16),   S( 71,  56),   S(130,  59),   S(116,  30),   S( 62,  21),   S( 77,  -5),
            S( 54, -46),   S( 16, -50),   S( 21, -17),   S( 57,  45),   S( 22,  45),   S( 78,  -3),   S( 43, -12),   S( 34,  10),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-128,   7),  S(-132,  17),  S( 39, -24),   S(-18,  18),   S( -8,  19),   S(-118,  49),  S(-115,  53),  S(-109,  55),
            S(-93, -14),   S(-73, -22),   S(-15, -33),   S(-51,   1),   S( -7,  -1),   S(-13,   7),   S(-89,  48),   S(-124,  59),
            S(-47, -16),   S(-65,  -6),   S(-75,  15),   S(-56,  13),   S(-87,  32),   S( -2,  21),   S(-39,  27),   S(-62,  33),
            S( 42,   1),   S(-81,  17),   S(-33,   9),   S(-76,  34),   S( 10,  17),   S( 37,  20),   S( 36,  39),   S( 70,  -1),
            S( 54,  13),   S(-18,  11),   S( 17,  21),   S(-16,  12),   S( 33,  60),   S( 63,  42),   S(111,  20),   S(131,  10),
            S( 42, -29),   S( 18, -41),   S( 10,  -9),   S( 14, -48),   S( 41, -14),   S( 33,  72),   S( 78,  76),   S(115,  67),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 11,  55),   S(-21,  39),   S(-12,  25),   S(  5,  24),   S(  1,  15),   S( -7, -33),   S(-26,  -7),   S(-47,  13),
            S(-34,  23),   S( -7,  -3),   S( -5,  40),   S( -7, -43),   S(-11,  31),   S(-29,   4),   S(-57, -21),   S( 19, -56),
            S(-53,  49),   S(  3,  37),   S( 36,  62),   S( 31,  23),   S( -5,  -5),   S(-27, -15),   S(-14, -48),   S(-35, -40),
            S(-17,  46),   S( 13,  77),   S( 62,  79),   S( 44,  43),   S( -9, -27),   S(-20, -16),   S( -7,  23),   S(-20, -25),
            S( 40,  10),   S( 48, 118),   S( 64,  72),   S( 45,  65),   S(  5,  14),   S( -6,   4),   S( 16,  37),   S( 11, -18),
            S( 10,  35),   S( 19, 162),   S( 89, 183),   S( 56,  90),   S( -7, -44),   S( -6, -76),   S( -8, -50),   S( -4, -55),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-43, -13),   S( 13,  24),   S(  0,   5),   S( -4,   2),   S(-22, -61),   S(-26,   1),   S(-47, -28),   S(-61,  16),
            S(  1, -22),   S( -4,   8),   S(-12,   5),   S(  8,  30),   S(-35,  24),   S( 12, -10),   S(-61, -37),   S(-12, -15),
            S( 16,  -8),   S( 30,  -2),   S(-12,  26),   S( 28,  64),   S( 12,  33),   S(-19, -17),   S(-29, -19),   S(-12, -29),
            S(-15,  12),   S( 24,  28),   S( 34,  64),   S( 17,  71),   S( 25,  39),   S( -4, -12),   S(  2, -21),   S( 27, -51),
            S( 52,   8),   S( 70,  80),   S( 79, 103),   S( 94, 143),   S( 72,  82),   S( 33,   9),   S( 25, -59),   S(  8, -53),
            S( 12,  15),   S( 68,  53),   S( 76, 146),   S( 83, 177),   S( 44,  75),   S( 36,   3),   S(  2, -18),   S( 35, -20),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-45, -13),   S(-62, -14),   S(-33, -11),   S(-10, -28),   S( -9, -19),   S(-31,  32),   S(-44,  10),   S( -1,  43),
            S(-40,   0),   S(-24, -18),   S(-30,  -8),   S(-11,   5),   S(-47,  37),   S(-18,  34),   S(-35,  15),   S( -7,  10),
            S( -5, -14),   S(-11, -11),   S(-22,  -7),   S(-21,   2),   S(-33,  44),   S( 14,  28),   S( -1,  22),   S(-21,  13),
            S( 36, -14),   S( 17, -17),   S( 18, -28),   S( 27,  57),   S(-19,  78),   S( -1,  49),   S(-14,   9),   S( 22,  12),
            S( 38, -33),   S( 31, -26),   S( 31,  18),   S( 62,  87),   S( 79, 120),   S( 76,  97),   S( 34,  24),   S( 45, -12),
            S( 42, -42),   S( 35, -32),   S( 27,  46),   S( 65,  65),   S( 55, 182),   S( 34, 113),   S( 50,  55),   S( 18, -20),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-52, -46),   S(-23,  -2),   S( 12, -34),   S(  6,  16),   S( 12,  42),   S(-19,  23),   S(-28,  24),   S( -2,  53),
            S(-26, -46),   S(-16, -30),   S(-41, -21),   S( 12,  11),   S(-25,  19),   S( -9,  29),   S( 12,  46),   S(-10,  33),
            S(  6, -53),   S( -3, -48),   S(-21, -16),   S(-11, -15),   S( 13,  24),   S( 15,  20),   S( 27,  78),   S(  2,  49),
            S( 27, -15),   S(-27,  11),   S(  2,   9),   S( 13,  43),   S(-13,  -5),   S( 41,  45),   S( -3,  89),   S(  6,  19),
            S( 14,  -8),   S( -1, -39),   S( -5, -14),   S(  7,   3),   S( 48,  69),   S(108,  80),   S(  6, 160),   S( 58,  -6),
            S( 14, -46),   S( 16,  17),   S( 13,  17),   S(  8,   2),   S( 36,  89),   S( 44, 165),   S(  3, 154),   S( 33, -31),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: bucket 0 */
            S(-40,  -8),   S(  6,  -3),   S(-29, -12),   S(-42, -10),   S(-38, -17),   S( -6, -41),   S(-54, -53),   S(-90, -32),
            S( -2,  41),   S( 37, -50),   S(-22,   0),   S(  4, -21),   S( -9, -26),   S( 16, -48),   S( -9, -54),   S(-58, -42),
            S(  5,  47),   S( -9,   5),   S( 21, -27),   S( 10,  -4),   S( 44, -18),   S(-18,  -2),   S(  1, -22),   S(-31, -99),
            S( 24, -25),   S( 39,  10),   S( 19,   4),   S( 38,   8),   S( 30, -23),   S(  4,  -4),   S(  2, -25),   S(-16, -22),
            S( 19, -35),   S( 29,  21),   S( 42, -32),   S( 70, -39),   S( 36,  -5),   S( 39,  -2),   S( 18, -18),   S(-39, -23),
            S(  8, -38),   S(  2,  -6),   S( 36, -17),   S( 51, -49),   S( 45, -61),   S( 32, -37),   S( -8, -32),   S( 20, -23),
            S( -4,  -2),   S(-11, -16),   S( 46, -36),   S( 19, -51),   S( 54, -53),   S(-26, -32),   S(-72, -37),   S(-31, -36),
            S(-38, -56),   S(-12, -29),   S(-20, -16),   S( -3, -25),   S(-41, -42),   S(  6,  13),   S(  7,   6),   S( 19,  -3),

            /* knights: bucket 1 */
            S(-78,  15),   S(-46,  67),   S(  2,  46),   S(-44,  57),   S(-20,  39),   S(-51,  45),   S(-35,  32),   S(-29, -21),
            S( 26,   4),   S( -2,  40),   S(  2,  20),   S( -2,  31),   S( -4,  27),   S(-10,  31),   S( 13, -10),   S(-30,  -1),
            S(-40,  47),   S(  6,  13),   S( 17,  10),   S( 22,  30),   S( 33,  21),   S(-18,  29),   S( -2,   2),   S(-30,  15),
            S( 10,  31),   S( 36,  36),   S( 28,  36),   S( 30,  39),   S( 30,  23),   S(  7,  22),   S( 52,  -6),   S(  8,  -6),
            S(-44,  63),   S( 28,   3),   S( 30,  19),   S( 44,  20),   S( 31,  15),   S( 37,  15),   S( 40,  -9),   S( 30,   3),
            S( -4,  29),   S( -1,  31),   S(  5,  19),   S( 33,   3),   S(  4,  14),   S( 25,  27),   S( 16, -26),   S(  1, -17),
            S( 50, -20),   S( 27,  -2),   S(-29,  23),   S( 14,  27),   S( 55, -25),   S( -2,   1),   S(-13,   0),   S(-21, -11),
            S(-86, -79),   S(-12,  14),   S(  6,  24),   S(-18,   1),   S(-25, -13),   S(-42,   1),   S( -1,   3),   S(-60, -47),

            /* knights: bucket 2 */
            S(-60,  -4),   S(-21,  20),   S(-47,  43),   S(-50,  55),   S(-39,  49),   S(-45,  73),   S(-37,  54),   S(-13, -26),
            S(-29,  31),   S(-34,  28),   S(-17,  25),   S(-18,  38),   S(-13,  29),   S( -8,  44),   S(-55,  51),   S(-46,  47),
            S(-33,  39),   S(-15,  30),   S(-17,  43),   S( 14,  33),   S( 10,  33),   S( -9,  29),   S( -9,  40),   S(-40,  46),
            S(-13,  37),   S(-22,  39),   S( -7,  47),   S(  1,  54),   S(-12,  66),   S(-20,  60),   S(-10,  55),   S(-19,  43),
            S( -4,  40),   S(-17,  37),   S(-20,  42),   S(-25,  57),   S(-21,  58),   S(-20,  59),   S( -6,  42),   S(-17,  25),
            S( -7,  35),   S(-18,  41),   S(-42,  45),   S(-26,  45),   S(-41,  38),   S( 16,  37),   S(-55,  48),   S( 24,  -7),
            S(-26,  40),   S(-49,   6),   S(-29,  28),   S(-34,  37),   S(-36,  36),   S( -4,  16),   S(-46,  44),   S(-44,  -2),
            S(-162,   0),  S(-25,   9),   S(-99,  48),   S(-33,   4),   S(  9,   7),   S(-71,   2),   S(-21, -15),   S(-191, -64),

            /* knights: bucket 3 */
            S(-65, -32),   S(-20, -20),   S(-44,   9),   S(-30,   9),   S(-28,  19),   S(-21,  31),   S(  6,  10),   S(-24,  -4),
            S(-33,  12),   S(-43,  11),   S(-23,   9),   S( -3,  25),   S( -5,  26),   S(-23,  20),   S(-15,   5),   S(-32,  78),
            S(-21,  -6),   S(-15,  27),   S(-12,  30),   S( 13,  28),   S( 19,  38),   S(  4,  29),   S( -4,  31),   S(-18,  59),
            S( -8,  15),   S(-18,  45),   S( -4,  55),   S( 10,  53),   S( 17,  64),   S(  7,  59),   S( 15,  50),   S( -2,  24),
            S( -7,  30),   S( -2,  28),   S( 10,  33),   S(  7,  52),   S( -6,  60),   S( 12,  62),   S( 21,  60),   S(  4,  22),
            S(-17,  33),   S( 17,  15),   S( 25,  13),   S( 33,  16),   S( 35,   2),   S( 58,  10),   S( -8,  36),   S(-34,  53),
            S(-28,  37),   S(  8,  13),   S( 19,   2),   S( 47,   5),   S( 39, -12),   S( 52,  -5),   S( 37, -45),   S(  3,  -6),
            S(-137,  46),  S(-47,  44),   S(-68,  29),   S( -9,  33),   S(  1,  30),   S( -8, -18),   S( -7, -21),   S(-78, -40),

            /* knights: bucket 4 */
            S(  0,   9),   S(-44, -14),   S( 12,   6),   S(-10, -12),   S(-24, -10),   S(-40, -47),   S(-37, -87),   S(-29, -59),
            S( 29,  -8),   S(-92,  64),   S( 10,   0),   S( 11, -24),   S( 12, -28),   S(  9, -39),   S( -1, -21),   S(-43, -36),
            S( -4,  32),   S(-35,  29),   S( 45, -25),   S( 49,  -6),   S( 31,  -4),   S( 10,   3),   S(-20, -42),   S(-36, -75),
            S(-20,  62),   S( 44, -22),   S(103, -32),   S( 47,  14),   S( 30, -15),   S(156, -63),   S( -9, -36),   S(  1, -22),
            S( 90,  12),   S( 15,  38),   S( 36,   8),   S( 63,   0),   S( 57,  -1),   S( 18,  -2),   S( 42, -36),   S( 22, -56),
            S( 10,   9),   S(-42,  -8),   S( 97,  -1),   S(  7, -11),   S(  8, -23),   S( 61, -24),   S( 13, -10),   S( -1, -25),
            S(-14,  -5),   S(-23,  -2),   S( 18,  21),   S(  2,  12),   S( 34,  -6),   S( 18, -12),   S(  6,  -9),   S(-17, -21),
            S(-23, -12),   S(  9,   4),   S(-11,  -9),   S(  4,  -9),   S( 11,  16),   S( 13, -18),   S( -7,  -3),   S( -9, -19),

            /* knights: bucket 5 */
            S( 44, -11),   S(-46,  24),   S( 57,  17),   S(  5,  41),   S(  6,  36),   S( 20,  -4),   S(-22,  11),   S(-22, -19),
            S(  0, -27),   S( 46,  33),   S( 54,  14),   S(  4,  30),   S( 61,  13),   S( 13,   5),   S( 25,  25),   S( 23, -12),
            S( 16,  16),   S( 44,  29),   S( 67,  12),   S( 99,   8),   S( 18,  31),   S( 17,  25),   S( 21,  15),   S( 25,  15),
            S( 50,  31),   S( 46,  34),   S( 92,   9),   S( 29,  40),   S( 70,  25),   S( 63,  18),   S( 39,  40),   S( 40,  27),
            S( 24,  48),   S( 69,  10),   S( 64,  26),   S( 77,  23),   S(103,  25),   S( 75,  27),   S( 24,  11),   S( 16,  26),
            S(  4,  23),   S( 27,  38),   S( 49,  22),   S( 36,  40),   S( 65,  28),   S(-12,  47),   S( 62,   8),   S( -2,  19),
            S( 29,  36),   S( 18,  49),   S( 40,  37),   S( 22,  61),   S( 16,  56),   S( 23,  45),   S( 10,  44),   S( 19,   7),
            S(-22, -45),   S(  3,  28),   S(  3,  46),   S( 12,  32),   S( 33,  76),   S( 11,  49),   S(  4,  20),   S(-29, -28),

            /* knights: bucket 6 */
            S( -5, -39),   S(-66,  10),   S( 25,  13),   S( 12,  21),   S( 12,  24),   S( 10,  44),   S(-19,  35),   S( -4,  35),
            S(  8, -33),   S( 74, -10),   S( 68,  -2),   S( 15,  21),   S(-31,  56),   S( 63,  26),   S( 29,  25),   S(  8,   0),
            S(  2, -11),   S( 46,   6),   S( 33,  13),   S(112,  -7),   S( 15,  37),   S(  4,  45),   S( 25,  51),   S( 15,  49),
            S( 33,  17),   S( 66,   6),   S(126,  -3),   S( 96,  12),   S( 79,  21),   S( 95,  16),   S( 23,  71),   S(  9,  60),
            S( 25,  14),   S( 71,   2),   S(101,   4),   S(111,  10),   S(137,  11),   S(153,   1),   S( 49,  32),   S(-23,  71),
            S( 32,  26),   S( 46,   4),   S( 83,  10),   S( 84,  16),   S( 81,  13),   S( 40,  30),   S( 41,  52),   S( 17,  49),
            S(-31,  35),   S( 12,  15),   S(-12,  36),   S( 16,  48),   S(  0,  53),   S( 24,  39),   S( 49,  73),   S(-15,  28),
            S(-40,  18),   S( -4,  22),   S( 47,  26),   S( -2,  30),   S(-10,  26),   S(  7,  54),   S( 25,  55),   S( -6,  -3),

            /* knights: bucket 7 */
            S(-37, -76),   S(-175, -26),  S(-76, -46),   S(-53, -32),   S(-25, -27),   S(-15,   4),   S( -6,  -2),   S(-15,   6),
            S(-26, -66),   S(-33, -49),   S(-24, -41),   S(-36,   1),   S(-47,  18),   S( 45, -23),   S(-12,  27),   S( 10,  29),
            S(-68, -46),   S(-14, -48),   S(  3, -21),   S( 75, -36),   S( 36, -10),   S( 59, -17),   S(  1,  45),   S( 33,  51),
            S(-55, -15),   S( 39, -27),   S( 18, -12),   S( 80, -18),   S( 92,  -6),   S( 50,  -5),   S(  6,  23),   S( 24,  33),
            S(-64, -16),   S( -2, -26),   S( 81, -45),   S(104, -38),   S(155, -39),   S( 67,   6),   S( 97,  -1),   S( 80,  20),
            S( 14, -32),   S( 16, -37),   S( 12, -26),   S( 78, -34),   S(102, -26),   S( 91, -16),   S( 88, -13),   S(  2,  25),
            S(-49, -52),   S(-84,  -4),   S( 12, -27),   S( 44,   7),   S( 42,  14),   S( 29,   4),   S( -7,   6),   S( 28,  21),
            S(-38, -13),   S(-14, -10),   S(-20, -44),   S( 12,  -8),   S( 26, -14),   S( 35,   8),   S( -4, -31),   S(  9,  -1),

            /* knights: bucket 8 */
            S( -9, -34),   S(-13,   3),   S(  3,  15),   S(-13, -13),   S( -9, -24),   S(-12, -52),   S( -1, -24),   S( -3, -24),
            S( -1,  -1),   S( -8, -28),   S( -3,  -5),   S(-18, -12),   S(-34, -35),   S(-31, -38),   S(-12, -43),   S(-25, -56),
            S(  7,  12),   S( -9, -14),   S( 16,  28),   S(  2, -12),   S(  0, -27),   S(-17,  -8),   S(-17, -31),   S( -7, -34),
            S(-17,   2),   S(  7,  25),   S(-16,   4),   S(  3,  24),   S(  2, -13),   S( -7, -20),   S(-10, -44),   S( -5,  -5),
            S( 20,  48),   S( 18,   9),   S(  8,   5),   S( 42,  35),   S( 11,  29),   S( 18,  -9),   S( -1, -12),   S(  6,  24),
            S( 15,  70),   S(-14, -15),   S( 30,  32),   S( 47,  26),   S(  5, -13),   S(  6,  10),   S(  1, -15),   S(-10, -24),
            S( -8,  -2),   S(  0,  27),   S(  4,  12),   S( 18,  17),   S(  7,  -5),   S(  5,  17),   S(  0,   0),   S( -2,   6),
            S( 16,  34),   S( 13,  33),   S(  8,  24),   S(  3,  13),   S( 13,  35),   S(  2,  -6),   S(  1,   4),   S( -1,  -5),

            /* knights: bucket 9 */
            S(-16, -69),   S( -9,   7),   S(-17, -41),   S(  1,  13),   S(-18, -45),   S(-20, -23),   S(-21, -37),   S( -1,   1),
            S(-23, -80),   S( -6,   2),   S(-20, -58),   S(-32, -33),   S(-13, -31),   S( -1, -32),   S( -8, -24),   S(-12, -40),
            S(-10, -49),   S( -8, -13),   S(-17, -12),   S(  4, -16),   S( -7,  -2),   S(-28, -24),   S(-11,   2),   S( -5, -29),
            S( -7, -20),   S(-10,  -3),   S( -5,   7),   S( 33, -10),   S( 51, -15),   S( 22, -15),   S(  1,   5),   S( -4,  -9),
            S(-15, -22),   S(  8,  12),   S( 19, -17),   S( 14,  -1),   S( 30,   4),   S( -4, -16),   S(  0,   2),   S( -5,  -8),
            S( -1,  11),   S( 17,  20),   S( 17,  16),   S( 18, -24),   S( 39,  24),   S( 23,  21),   S( 10,  38),   S(  1,   5),
            S(  2,  11),   S(-16,  -2),   S( 11,  26),   S(  2,  39),   S( 10,  39),   S(  6,   7),   S(  4,  -2),   S(  1,  20),
            S( -3,  -5),   S( 12,  39),   S( 14,  41),   S(  9,   2),   S( -1,  17),   S(  8,  15),   S(  3,  23),   S( -1, -13),

            /* knights: bucket 10 */
            S(-30, -96),   S(-29, -55),   S(-10, -37),   S(-23, -21),   S(-11, -25),   S(-20, -41),   S(-19, -15),   S(  1,   0),
            S(-21, -76),   S(-14, -17),   S(-23, -39),   S(  2, -26),   S(-19, -12),   S( -6, -33),   S(-18, -35),   S( -6, -26),
            S(-23, -54),   S(-37, -65),   S( -7, -43),   S(-14, -41),   S( -8, -28),   S(-38, -20),   S(  6,  -3),   S(  7,  -2),
            S(-31, -28),   S(-12, -32),   S(  1, -34),   S( 14,  -4),   S( 34,  -7),   S( 18, -16),   S(  9,  11),   S(  1,  37),
            S(-18, -40),   S(-21, -37),   S( 15, -19),   S( 50, -32),   S( 41, -12),   S(  5, -13),   S(  0,  -4),   S(  3,  11),
            S( -6, -25),   S(-11, -43),   S(  1, -17),   S( 25,   6),   S( 35,  16),   S(-14,  15),   S( 21,  51),   S(  4,  25),
            S(  7,  24),   S(-17, -36),   S( 12,   8),   S( 20,  39),   S(  6,  54),   S( -9,   6),   S( 21,  50),   S( -3,  -1),
            S( -9, -18),   S( -1,  -2),   S( -2,  -4),   S(  4,  10),   S(  9,  29),   S( 14,  50),   S(  1,  20),   S( -1,  -5),

            /* knights: bucket 11 */
            S( -5, -29),   S(-30, -42),   S(-11, -30),   S(-15, -42),   S(-15, -28),   S(-31, -40),   S(-14, -31),   S( -5, -21),
            S(-19, -41),   S(-14, -36),   S( -6, -50),   S(-43, -12),   S(-12, -15),   S(  0,   6),   S( -5,  25),   S(-11, -12),
            S(-11, -40),   S(-35, -61),   S(-70, -35),   S(-16, -16),   S(-21,   1),   S(-29,  -5),   S(-12,   9),   S( -8,  -2),
            S(-25, -21),   S( -7, -33),   S(  0, -31),   S( 39,  -7),   S( 28,   8),   S( 56,   6),   S(-21,   0),   S(-14,  31),
            S(  5,   8),   S(-14, -31),   S( 38, -15),   S( 20, -17),   S( 29,   4),   S( 67,  12),   S( 16,  -1),   S( -2,  73),
            S(-25, -16),   S(  4,  -7),   S( -2, -12),   S( 19,  -1),   S( 32,   6),   S( 36,  20),   S( -2,  49),   S( 17,  70),
            S( -2,  10),   S( -9,   4),   S( -7, -26),   S( 11,  -3),   S( 23,  51),   S( 11,  55),   S( -1,  37),   S( 22,  92),
            S( -7,  -8),   S( -6, -28),   S(  7,  21),   S(  1,   1),   S( -3,  14),   S(  9,  57),   S(  4,  30),   S(  7,  39),

            /* knights: bucket 12 */
            S( -2, -14),   S(  0,  -2),   S( -3, -18),   S( -2,   6),   S(  0,  -3),   S( -6, -21),   S(  5,  23),   S( -4, -21),
            S(  0,   2),   S(  0,  -2),   S(  3,  11),   S( -3,  -9),   S( -7,  -1),   S( -7, -38),   S( -3, -30),   S(  1,   4),
            S( -1,   1),   S(  0,  14),   S( -8, -15),   S( -9, -17),   S(-10, -14),   S( -8, -18),   S(  5,   9),   S( -6, -27),
            S(-14, -30),   S( -7, -26),   S(  0, -10),   S( 12,  26),   S( -9, -29),   S(  5,   0),   S(  1,  -3),   S( -2,  -8),
            S( 13,  35),   S(  7,  18),   S( -1,  10),   S( 13,  23),   S(-11, -11),   S(  0, -22),   S( -8, -25),   S( -1,   4),
            S( -1,  21),   S(  4,  20),   S( -2,  52),   S(  6,   1),   S(  4,  -1),   S(  3,  13),   S( -1,  -2),   S(  0,   0),
            S(  2,  27),   S( -3,   2),   S(  9,  14),   S(-13,  28),   S( -5,   1),   S( -4, -14),   S( -1,  -1),   S( -6, -12),
            S(  1,  -4),   S( 14,  65),   S( -1,  11),   S(  4,  11),   S( -1,  -4),   S(  0,  -7),   S( -1,  -5),   S( -1,  -2),

            /* knights: bucket 13 */
            S(-19, -40),   S( -1,   1),   S(  0,  -7),   S( -6, -31),   S( -5, -22),   S( -5,   0),   S( -2,  -2),   S(  1,   4),
            S( -3, -11),   S(  1,  -1),   S( -5, -40),   S( -7, -14),   S(  4,   6),   S(  0, -16),   S(  0,   9),   S(  5,  15),
            S(  3,  19),   S( -4,  -3),   S( -6, -13),   S(  9,  16),   S(-21, -33),   S( -3, -12),   S(  0,  -3),   S( -3,  -7),
            S(-23, -45),   S(  8,  22),   S( 19,  55),   S( -7, -20),   S( -6, -43),   S( 12,  29),   S(  1,  -2),   S( -5, -18),
            S(  6,   7),   S( -1,  -5),   S( 18,  23),   S(-17,  -8),   S(  4,  25),   S( -3, -13),   S(  1,  -5),   S(  3,   6),
            S(  1,  23),   S( 13,  20),   S(  9,  87),   S( -3,  14),   S( 19,  48),   S(  5,  -1),   S(  4,  10),   S( -3,  -3),
            S(  2,   9),   S( 10,  26),   S(  9,  31),   S(  7,  59),   S( 16,  72),   S( -3,  -9),   S(  4,  21),   S( -5,  -1),
            S( -2,  -2),   S(  0,  42),   S(  6,  32),   S( 14,  59),   S(  3,  33),   S(  1,   6),   S(  0,   1),   S(  1,   7),

            /* knights: bucket 14 */
            S( -4, -11),   S(  0, -20),   S( -3, -15),   S(  0,   9),   S( -8, -50),   S( -1,   3),   S(  1,   1),   S( -7, -26),
            S( -1,  -7),   S(  4,  12),   S(-11, -37),   S( -8, -14),   S(  3,  -1),   S( -2, -19),   S(  2,  13),   S( -3, -21),
            S(-15, -50),   S( -6, -28),   S(-19, -13),   S(-12, -44),   S( 11,  10),   S( -6, -13),   S(  3,  11),   S(  6,  12),
            S( -1,   0),   S( -3, -17),   S( -7, -57),   S(  2, -13),   S( -3,  -1),   S(  3,  -3),   S( -2,   5),   S(  3,  37),
            S( -3,  -9),   S( -3, -11),   S(  3,  19),   S(-11, -24),   S(-18, -24),   S(  7,   6),   S(  4,  26),   S(  4,  24),
            S( -3, -14),   S(  4,   5),   S(  6,  10),   S( 17,  -7),   S( 14,  24),   S(-13, -27),   S(  2,  32),   S( -1,  14),
            S( -1,  -5),   S( -2,  -6),   S( 12,  32),   S(  1,  13),   S(-11,  29),   S(  3,  34),   S(  2,  34),   S(  2,  20),
            S( -1,  -3),   S( -1,   0),   S(  0,   5),   S(  3,  50),   S(  4,  12),   S(  6,  30),   S(  1,  41),   S( -1,  -6),

            /* knights: bucket 15 */
            S( -1,  -3),   S( -6, -23),   S(  2,  -7),   S(  0,  -3),   S( -5, -21),   S( -4, -21),   S( -1,  -5),   S( -1,   1),
            S( -2,  -6),   S( -2,  -8),   S( -5, -24),   S( -4, -32),   S(  3,  -7),   S(  1,   2),   S(  2,  16),   S( -1,  -3),
            S( -6, -15),   S( -2, -22),   S(-11, -39),   S(  0,  -3),   S( -6, -12),   S(-10, -23),   S( -8, -28),   S( -5,  -7),
            S( -8, -16),   S( -3, -30),   S( -6, -29),   S( -9, -47),   S(  1, -11),   S(  7,  16),   S(  6,  -1),   S(  4,  21),
            S( -4, -19),   S( -2,  10),   S( -1, -21),   S(  2,  16),   S( 10,  19),   S(  4, -16),   S(-10, -18),   S(  2,  18),
            S( -7,  -5),   S( -5,  -1),   S( -3, -17),   S( -4, -15),   S( -3,  -8),   S( -2, -10),   S( -7,   1),   S( -5,  59),
            S( -4, -11),   S( -4,  -1),   S(  0,  -1),   S( -3,  -8),   S( -3,   9),   S( -6,  16),   S(  4,  10),   S(  3,  30),
            S(  0,  -4),   S( -1, -13),   S( -3, -11),   S(  0,   1),   S( -5,   5),   S(  6,  25),   S(  8,  44),   S( -2,  -5),

            /* bishops: bucket 0 */
            S(  5,  12),   S( -2,  -7),   S( 59,  -8),   S( -8,  33),   S( -6,  11),   S( 14,  -7),   S(-25,  19),   S( 11, -60),
            S( 46, -19),   S( 83,  18),   S( 21,  24),   S( 23,   9),   S(  3,  42),   S( -9,   5),   S(-28,  22),   S( 10, -19),
            S( 62,  -3),   S( 43,  26),   S( 32,  39),   S( 18,  45),   S( 13,  31),   S(-11,  69),   S( 11, -10),   S( 16, -17),
            S( 15,  23),   S( 71,  13),   S( 32,  38),   S( 33,  22),   S( -1,  38),   S( 25,  19),   S(  1,  14),   S(-13,   7),
            S(  4,  29),   S( 28,  27),   S(  7,  51),   S( 36,  23),   S( 35,  13),   S( -1,  24),   S(  6,  21),   S(-35,  50),
            S(-11,  35),   S(  0,  51),   S( 59,  20),   S( -2,  46),   S( -5,  54),   S(  5,  17),   S(  0,  28),   S( -4,  44),
            S(-40,  49),   S(-10,  35),   S(  7,  47),   S( -9,  56),   S(-46,  51),   S( 33,  28),   S( 10,  28),   S(-16,  22),
            S(-49, -34),   S(-17,  68),   S(-23,  42),   S(-27,  47),   S( 24,  54),   S(-23,   5),   S( -3,  54),   S( 26,  45),

            /* bishops: bucket 1 */
            S( 22,  13),   S( 23,   7),   S( -2,  26),   S( -8,  33),   S( -1,  23),   S( -7,  27),   S(-45,  46),   S(-63,  34),
            S( -3,   9),   S( 41,  17),   S( 45,  15),   S( 23,  33),   S(-14,  37),   S( 10,  18),   S(-27,  34),   S(  7,   5),
            S( 40,   3),   S(  6,  25),   S( 38,  44),   S(  8,  41),   S( 12,  44),   S(-16,  56),   S( 16,  18),   S(  4,   3),
            S( 43,  -3),   S( 32,  49),   S( -8,  50),   S( 39,  32),   S( 15,  42),   S( 17,  40),   S(-12,  49),   S( 21,  16),
            S( 51,  30),   S( 10,  38),   S( 37,  27),   S( -8,  36),   S(  6,  33),   S(-23,  60),   S( 26,   1),   S( -9,  47),
            S(-35,  56),   S( 47,  35),   S( 10,  60),   S( 17,  46),   S( 31,  38),   S( -3,  54),   S(-14,  60),   S( 47,   5),
            S(-11,  56),   S( -6,  68),   S( 38,  36),   S(-16,  58),   S( -6,  65),   S(-38,  65),   S(-33,  77),   S(-19,  49),
            S(-10,  69),   S(  0,  51),   S(-20,  58),   S(-12,  54),   S(-15,  60),   S(-16,  49),   S(-19,  73),   S(-68, 109),

            /* bishops: bucket 2 */
            S(  1,  30),   S(-10,  39),   S(-21,  32),   S(-33,  48),   S(-23,  47),   S(-34,  29),   S(-41,  10),   S(-41,  36),
            S(-17,  23),   S(  3,  25),   S( 11,  28),   S(-14,  47),   S(-12,  46),   S(-11,  35),   S( -6,   9),   S(-10,  -3),
            S(-15,  39),   S(-20,  54),   S( -7,  71),   S(-12,  60),   S(-10,  55),   S( -2,  59),   S(-13,  35),   S(-17,  18),
            S(-11,  40),   S(-54,  81),   S(-36,  70),   S(-17,  67),   S(-20,  65),   S(-15,  68),   S( -3,  46),   S(  2,  35),
            S(-22,  59),   S(-26,  57),   S(-43,  67),   S(-53,  67),   S(-41,  62),   S(-25,  70),   S( -9,  36),   S(-33,  48),
            S(-24,  49),   S(-38,  66),   S(-42,  78),   S(-64,  77),   S(-50,  69),   S(-53,  67),   S(-19,  75),   S(-17,  52),
            S(-63,  69),   S(-73,  81),   S(-72,  92),   S(-36,  67),   S(-85,  93),   S(-67,  72),   S(-87,  77),   S(-32,  71),
            S(-62, 114),   S(-100, 100),  S(-68,  94),   S(-79,  84),   S(-80,  79),   S(-93,  89),   S(-34,  72),   S(-77,  74),

            /* bishops: bucket 3 */
            S(-19,  38),   S( -8,  34),   S(  2,  24),   S(-11,  47),   S(-15,  44),   S( 38,  18),   S( 14,  -6),   S( 34, -41),
            S( -7,  36),   S( -1,  39),   S(  4,  31),   S(-10,  65),   S(  1,  50),   S( -9,  57),   S( 36,  29),   S(  7,  11),
            S(  9,  44),   S(-10,  62),   S( -6,  82),   S(  3,  63),   S( -1,  84),   S(  6,  72),   S( 15,  46),   S( 30,  22),
            S(  9,  42),   S(-17,  76),   S(-11,  85),   S( -3,  73),   S( 11,  65),   S(  9,  70),   S(  6,  69),   S(  9,  34),
            S(-11,  70),   S(  4,  45),   S( -3,  61),   S(-14,  73),   S(-10,  67),   S(  0,  69),   S( -1,  58),   S( 14,  60),
            S(-17,  54),   S(  3,  65),   S(  0,  67),   S(-11,  60),   S(-24,  67),   S( 24,  61),   S(  7,  65),   S(-23,  98),
            S(-27,  59),   S(-53,  85),   S( -4,  65),   S(-10,  68),   S(-18,  70),   S(-39,  70),   S(-42,  81),   S(  3,  78),
            S(-31, 106),   S(-11,  69),   S(  4,  64),   S(-33,  90),   S(-28,  77),   S(-55, 102),   S(-28,  76),   S( 47,  37),

            /* bishops: bucket 4 */
            S(-48, -13),   S(-14,  28),   S(-37,  -3),   S(-28,  17),   S(-31,  16),   S(-48,  18),   S( 10,  -2),   S( -8, -13),
            S(-36, -25),   S(  0,  22),   S(-19,  29),   S(-32,  30),   S(-28,  24),   S( 56,  -4),   S(-53,   6),   S( 25,  -9),
            S(-19,  16),   S(-12,  20),   S( 20,  10),   S( 32,   5),   S( 10,  32),   S( 41,  14),   S(-38,   6),   S(-67,  33),
            S(-12,  29),   S(-15,  42),   S( 55,  16),   S( 76,   9),   S( 30,  24),   S( 45,  20),   S( 33,  21),   S(-21,  16),
            S( 43,  26),   S( -4,  27),   S(  4,  41),   S( 49,   6),   S( 14,  17),   S( 15,  43),   S(-41,  14),   S(  6,  25),
            S(  2,  41),   S( 13,  38),   S( 22,  39),   S( 39,  13),   S( -6,  37),   S( -8,  14),   S( 14,  -6),   S(-18,  19),
            S( -9,  43),   S( 42,  22),   S( 25,  29),   S(-16,  46),   S(-17,  42),   S( -4,  27),   S( 17,  -3),   S(-10,   2),
            S(  7,  25),   S(-15,  14),   S( -8,   7),   S(-10,  48),   S( 14,  41),   S(-12,  39),   S( -8,  12),   S( -9,  19),

            /* bishops: bucket 5 */
            S(-15,  25),   S(-13,  35),   S(-60,  57),   S(-22,  48),   S( 11,  34),   S(-25,  24),   S( -2,  35),   S(-35,  38),
            S(  0,  16),   S(-26,  52),   S(-17,  60),   S( 50,  31),   S( 16,  32),   S(  2,  38),   S(  1,  21),   S(-19,  13),
            S( -8,  45),   S(-20,  58),   S( 35,  38),   S(  8,  49),   S( 49,  32),   S( 25,  33),   S(  3,  51),   S( -5,  38),
            S( 47,  37),   S( 30,  54),   S( 54,  33),   S( 43,  33),   S( 60,  35),   S( 42,  45),   S( 17,  46),   S(-22,  41),
            S( 45,  38),   S( 55,  28),   S( 81,  41),   S(117,  14),   S( 97,   1),   S( 71,  28),   S( 59,  23),   S(-39,  56),
            S( 40,  56),   S( 49,  51),   S( 97,  34),   S( 63,  37),   S(  0,  55),   S(  2,  32),   S(-26,  44),   S(  7,  63),
            S( -8,  57),   S(-35,  60),   S( 37,  50),   S( 22,  68),   S( 17,  66),   S(-10,  69),   S(-19,  45),   S(-33,  44),
            S(-19,  53),   S( 28,  57),   S( 49,  38),   S( 23,  71),   S( 10,  54),   S( -5,  68),   S(  5,  67),   S(  2,  42),

            /* bishops: bucket 6 */
            S(-51,  60),   S( 30,  15),   S(-59,  54),   S(-46,  51),   S(-31,  58),   S(-22,  46),   S( 41,  26),   S(-38,  40),
            S( -2,  35),   S( -5,  39),   S( 30,  36),   S(  9,  50),   S( 22,  33),   S( -5,  42),   S(-81,  66),   S(-18,  28),
            S( 21,  20),   S( 22,  29),   S( 78,  29),   S( 51,  31),   S( 78,  15),   S( 51,  31),   S(-22,  75),   S(-43,  61),
            S(-19,  62),   S( 44,  42),   S( 69,  34),   S( 98,  13),   S( 67,  24),   S( 44,  39),   S( -6,  60),   S(  6,  38),
            S(-11,  61),   S( 44,  35),   S( 77,  16),   S( 29,  34),   S(108,  17),   S( 97,  29),   S( 21,  45),   S(-33,  51),
            S(  3,  36),   S( 10,  40),   S( 28,  42),   S( 62,  41),   S( 50,  45),   S( 91,  35),   S( 44,  48),   S(-15,  74),
            S(  6,  35),   S(  6,  47),   S( 28,  49),   S( -3,  66),   S( 35,  58),   S( 40,  53),   S( 20,  49),   S(-36,  69),
            S(-20,  52),   S( 10,  63),   S(-12,  63),   S( -5,  55),   S( -7,  55),   S(  2,  58),   S(  4,  54),   S( -4,  68),

            /* bishops: bucket 7 */
            S(-20,  14),   S(-43,  25),   S(-52,  -2),   S(-34,  19),   S(-33,  24),   S(-47,  26),   S(-57, -20),   S(-34, -19),
            S(-52,   8),   S(-32,   4),   S(-14,  18),   S( 16,  10),   S( -6,  16),   S(-25,   7),   S(-10, -14),   S(-76, -34),
            S(-51,  30),   S( 20,  -1),   S( 21,  20),   S( 47,   4),   S(  6,  13),   S( 22,  10),   S( -7,  17),   S(-56,  23),
            S( -4,  19),   S( 47,  14),   S( 73,   1),   S( 78,   9),   S( 93,   2),   S( 55,   0),   S( 46,  27),   S(-11,  32),
            S(-17,  18),   S(  6,   1),   S( 32,   3),   S( 85, -17),   S( 90,  -3),   S(117,   2),   S( 20,  18),   S( 59,  -5),
            S(-56,   2),   S(-23,  17),   S( 33,   2),   S( 35,   0),   S( 92,  -7),   S( 85,  11),   S( 55,  19),   S(  2,  14),
            S( -8,  -4),   S(  4,   3),   S( 17,  19),   S(  8,  10),   S(  3,   6),   S( 34,  12),   S( 37,  15),   S(  3,  15),
            S(-31,  26),   S(-27,  40),   S(-12,  35),   S(-23,  43),   S(  3,  16),   S( 35,  35),   S(  9,  28),   S( 23,  36),

            /* bishops: bucket 8 */
            S(  0, -52),   S(-13, -86),   S(-43, -10),   S(  2, -11),   S(-20, -28),   S(-26, -10),   S(-12, -38),   S( 11,   9),
            S(-13, -37),   S(-31, -80),   S(  3, -24),   S( -3, -12),   S( 19, -24),   S( 13, -24),   S(-19, -37),   S( 10, -23),
            S(  1, -22),   S( -3, -32),   S( 13, -33),   S( 36, -28),   S( 34, -16),   S( 15, -19),   S( 11, -50),   S(-55, -25),
            S( -3,  22),   S(  0,  -6),   S( 24,   1),   S( 35, -20),   S( 47, -23),   S( 23, -31),   S(  2, -18),   S(  0,  -5),
            S( 15,  32),   S( 14,  12),   S( 51, -17),   S( 77, -22),   S( 40, -53),   S( 42,  -7),   S(  1, -55),   S(  3,  -6),
            S( -4,  22),   S( 28,  17),   S( 20,   3),   S( 12, -18),   S( 19,  -8),   S( 10, -36),   S( 14, -73),   S( -9, -45),
            S(-13, -21),   S( 19,  -6),   S( 20,   3),   S( -2, -20),   S( -2,  -6),   S(  8, -32),   S( -2, -39),   S(-13, -52),
            S( -4, -12),   S( -9, -62),   S(  0, -21),   S( -4, -16),   S(-11, -38),   S(  2, -38),   S( 13,   0),   S(-10, -52),

            /* bishops: bucket 9 */
            S(-12, -61),   S( 10, -64),   S(-27,  -7),   S( -7, -14),   S(-27, -34),   S(-22, -31),   S(-21, -55),   S(  2,   8),
            S(-15, -37),   S(  0, -56),   S( 17, -37),   S( 11, -12),   S(  0, -32),   S( 12, -33),   S( 20, -34),   S(  4, -14),
            S(  0, -16),   S(  4,  -4),   S( 38, -35),   S(  9, -39),   S( 52, -36),   S( 55, -24),   S( -9, -17),   S( -8,  -3),
            S( -5,  16),   S( 29,  -7),   S( 53, -19),   S( 84, -43),   S( 56, -33),   S( 45, -22),   S( 38, -32),   S( -5, -33),
            S( -8,  -6),   S( 13,   2),   S( 31, -17),   S( 58, -23),   S( 64, -53),   S( 47, -32),   S( 12, -34),   S( -5, -21),
            S( -4, -19),   S( 59,   1),   S( -4,  14),   S( 48,  -3),   S( 30, -22),   S( 39, -41),   S( 25, -40),   S( -2, -32),
            S( -8, -32),   S( 11,  -2),   S( -5, -19),   S( 35,   8),   S( 18, -21),   S( 20, -25),   S( 13, -26),   S(-10, -59),
            S(-10, -39),   S(-17, -41),   S(  9, -14),   S(  5, -17),   S(  4, -14),   S(  8, -20),   S(  9, -34),   S( -9, -50),

            /* bishops: bucket 10 */
            S(-21, -68),   S( -2, -44),   S(-81, -28),   S(-11, -33),   S(-33, -22),   S(-13, -44),   S( -4, -56),   S( -8, -56),
            S( 10, -48),   S(-22, -53),   S( 11, -55),   S(-11, -27),   S(-19, -33),   S( 10, -45),   S( -4, -52),   S(-11, -31),
            S(  6, -28),   S( 19, -47),   S( 48, -59),   S( 67, -49),   S( 72, -63),   S( 27, -29),   S( -7, -30),   S(  7, -13),
            S( -5, -45),   S( 27, -35),   S( 64, -38),   S( 76, -67),   S( 70, -45),   S( 47, -31),   S( 11, -11),   S(  8, -12),
            S(  0, -24),   S( 35, -46),   S( 67, -63),   S(132, -63),   S( 86, -42),   S( 56, -27),   S( 42,  -7),   S(-10, -32),
            S( -2, -46),   S( 17, -63),   S( 22, -47),   S( 40, -54),   S( 72, -16),   S( 62,  -9),   S( 50, -34),   S(  1, -43),
            S(-35, -96),   S( 22, -61),   S( 13, -42),   S( 29, -31),   S( 22, -21),   S( 27, -25),   S( 16, -23),   S(  8,  13),
            S( -5, -51),   S(  6, -43),   S( -6, -37),   S( -7, -32),   S(  8, -35),   S( -6, -27),   S( 13,  -1),   S(  4, -16),

            /* bishops: bucket 11 */
            S(-17,  -1),   S(-28, -16),   S(-74, -58),   S(-23, -25),   S(-34, -14),   S(-43, -28),   S(  3, -33),   S(-26, -84),
            S(  1, -31),   S( 10, -42),   S(  6, -28),   S(-21, -42),   S(-17, -17),   S(-21, -28),   S(-54, -80),   S(-21, -50),
            S(  0, -43),   S( 27, -46),   S( 24, -44),   S( 56, -38),   S( 29, -36),   S( 14, -25),   S(-25, -23),   S(-11, -22),
            S( -3, -14),   S( 10, -42),   S( 43, -39),   S( 78, -69),   S( 98, -55),   S( 45, -16),   S( 19, -16),   S( 13,  34),
            S(-16, -45),   S( 11, -41),   S( 33, -49),   S(109, -63),   S( 70, -43),   S( 70, -39),   S( 18,   5),   S( 16,  22),
            S(-15, -57),   S(  7, -54),   S( 43, -55),   S( 29, -37),   S( 51, -33),   S( 38, -15),   S( 19,  15),   S(-26, -18),
            S( -1, -41),   S( 17, -62),   S( -7, -43),   S( 28, -49),   S(  4, -16),   S( 41,  11),   S(  2, -32),   S( 19,  -5),
            S(-11, -62),   S(-15, -52),   S(  1, -46),   S( 12, -27),   S(  7, -28),   S( -6, -55),   S( -5, -37),   S( -8, -39),

            /* bishops: bucket 12 */
            S(-12, -36),   S( -4, -25),   S(-16, -47),   S(  4,  -7),   S( -5,  -7),   S(-25, -56),   S( -4, -22),   S(  0,   6),
            S( -3, -14),   S(-10, -55),   S(-13, -47),   S( -1, -29),   S( -2, -44),   S( -4,  -9),   S(  4,   7),   S(  6,  14),
            S(  1,  -8),   S( -9, -37),   S(  2,   8),   S(-11, -23),   S(  2, -16),   S(  4, -30),   S(  1, -11),   S( -6, -12),
            S( -6, -28),   S(  3,  -7),   S(-11, -36),   S(  7,   2),   S( -6, -64),   S(  1,   0),   S(-10, -39),   S(  2,   2),
            S(  8,  16),   S(  8,   8),   S( 14, -32),   S(  0, -51),   S( 10, -23),   S( -1, -22),   S(  8,   0),   S( -9, -23),
            S(  0,  15),   S(  2,  16),   S(-18, -23),   S( -7, -35),   S( 18, -22),   S( -3, -22),   S( -6, -53),   S( -7,  -6),
            S( -4,   4),   S( -1,  -5),   S( 10,  21),   S( -9, -27),   S(  2, -32),   S(  8, -23),   S( -1, -14),   S( -2, -10),
            S(  3,   4),   S(  3,  20),   S(-10, -43),   S( 15,  43),   S(  0,  -8),   S(  1, -16),   S(-10, -36),   S( -4, -16),

            /* bishops: bucket 13 */
            S( -5, -48),   S(  2, -54),   S(-16, -38),   S( -4, -51),   S( -5, -19),   S(  4, -10),   S( -3, -12),   S( -4, -29),
            S( -3, -24),   S( -5, -46),   S(-12, -67),   S( -1, -26),   S(-11, -45),   S( -5, -22),   S(  4, -11),   S( -4, -26),
            S(-11, -61),   S(  1,  -2),   S(  0, -63),   S( 20, -32),   S( -9, -52),   S(  7, -42),   S(  3, -12),   S(  0,  -6),
            S( -7, -12),   S(  2, -37),   S( 17, -21),   S(  3, -75),   S( 24, -20),   S(  4, -25),   S(  7, -31),   S( -4, -28),
            S( -3,  -9),   S( -8, -14),   S(  6, -39),   S( 28, -17),   S(  1, -50),   S( 10, -42),   S(  2, -54),   S(  4,  -7),
            S(  2, -11),   S( -2, -17),   S(-17,  -8),   S(  8, -16),   S(  6, -11),   S(  9, -24),   S( 15, -36),   S( -8, -46),
            S( -6, -33),   S( -3, -52),   S(  2,  -8),   S( -8, -27),   S( -7, -31),   S(  2,  -9),   S(-11, -58),   S( -1, -17),
            S(-11, -51),   S( -5, -23),   S( -2, -28),   S(  1, -11),   S( -4, -30),   S(-10, -52),   S( -4, -24),   S( -1, -25),

            /* bishops: bucket 14 */
            S(  1, -29),   S(-13, -60),   S( -6, -32),   S(-14, -45),   S(-10, -47),   S( -8, -48),   S(  2, -36),   S( -4, -39),
            S( -2, -26),   S( 10,   6),   S( -2, -47),   S( -9, -42),   S( -9, -79),   S(-18, -90),   S(-14, -71),   S(  4, -14),
            S(  2,  -9),   S( -2, -34),   S(-13, -69),   S( 10, -68),   S( -1, -67),   S( -4, -60),   S(  0, -26),   S(  9,   8),
            S( -1, -19),   S( -2, -29),   S( -3, -45),   S(  7, -63),   S( -1, -89),   S( -9, -47),   S( -2, -24),   S( -2,   7),
            S(-12, -59),   S(  4, -42),   S( -3, -73),   S( 14, -54),   S( 20, -49),   S( -1, -58),   S(  9, -15),   S( -1, -21),
            S( -2, -41),   S( -6, -74),   S(  5, -52),   S(  4, -24),   S(-13, -14),   S(  1,  12),   S(  7, -60),   S( -6, -36),
            S( -6, -52),   S( -2, -68),   S( -8, -56),   S(  1, -24),   S(-12, -39),   S(  0, -14),   S( -9, -36),   S( -6, -20),
            S( -3, -34),   S(-12, -42),   S(  0, -20),   S( -7, -25),   S( -2, -12),   S(  0,  -6),   S(  1,   7),   S(  2, -26),

            /* bishops: bucket 15 */
            S(  8,  23),   S(  8,  34),   S( -6, -18),   S(  1,   2),   S(-13, -46),   S(-12, -32),   S( -7, -33),   S( -7, -27),
            S( -3,  -8),   S( -9, -38),   S( 11,  -7),   S( -5, -32),   S(-18, -43),   S( -6, -51),   S(-16, -60),   S( -4, -14),
            S(-14, -50),   S(  2,   6),   S( -3, -14),   S(  1, -28),   S(  8, -16),   S( -7, -38),   S( -5, -44),   S(  1,  -8),
            S(-12, -41),   S(-14, -60),   S(  6,  -9),   S(-27, -72),   S(  2, -40),   S( -9, -64),   S( 12,  48),   S(  4,  13),
            S(  7,  -2),   S(-12, -50),   S( -3, -66),   S(-21, -68),   S( -2, -62),   S( -8, -18),   S( 19,  24),   S(  5,  -6),
            S( -7, -27),   S( -2, -78),   S(-21, -84),   S(-13, -55),   S( -3, -11),   S(  6,  -7),   S( 31,   3),   S(  3,  -7),
            S( -3, -17),   S( -6, -46),   S( -3, -40),   S( -3, -48),   S(-12, -64),   S(  3,  -7),   S(-17, -52),   S( -6,   6),
            S( -3,  -1),   S( -7, -25),   S( -6, -34),   S( -7, -42),   S( -3, -23),   S(-11, -36),   S( -9, -26),   S(  9,  33),

            /* rooks: bucket 0 */
            S(-33,  14),   S( 22, -31),   S(  7,  -6),   S( 15, -15),   S( 13,   5),   S( 14,  -7),   S( 14,  19),   S( 21,  31),
            S( 30, -50),   S( 45, -17),   S( 12,  12),   S(  7,   7),   S( 23, -18),   S( 30, -16),   S( -4,  13),   S( -8,  16),
            S( 14, -25),   S( 38,  -2),   S( 48,  -5),   S( 14,   4),   S( 20,   2),   S(  8,   4),   S( -2,  10),   S(-31,  14),
            S( 59, -26),   S( 87,  -7),   S( 47,  16),   S( 50,  -5),   S( 34,  -1),   S(  6,  11),   S( -3,  23),   S(-12,  20),
            S(100, -31),   S(112, -29),   S( 80,   1),   S( 32,   4),   S( 70, -20),   S( 39,  -9),   S( 24,  14),   S( -7,  19),
            S( 80, -34),   S(137, -25),   S( 74,  -6),   S( 48, -11),   S( 59, -31),   S(-37,  18),   S( 64,   3),   S( 11,  24),
            S( 82,  -1),   S(105,  -1),   S( 41,  21),   S( 48, -14),   S( 36,   3),   S( -7,  14),   S( -5,  25),   S( 14,  30),
            S( 45,  28),   S( 45,  43),   S( 55,  16),   S( 20,  26),   S( 43,   2),   S(  7,  23),   S(-19,  47),   S(-27,  60),

            /* rooks: bucket 1 */
            S(-68,  45),   S(-49,  39),   S(-47,  21),   S(-46,  10),   S(-18,  -8),   S(-30,  13),   S(-19,   7),   S(-29,  36),
            S(-59,  24),   S(-42,  28),   S( -1,  -4),   S(-19, -15),   S(-39,  11),   S(-39,   8),   S(-41,  11),   S(-51,  36),
            S(  0,  25),   S(-22,  26),   S(-22,  39),   S(-36,  32),   S(-36,  28),   S(  0,   1),   S(-22,  15),   S(-48,  38),
            S(-36,  54),   S(-32,  56),   S(  1,  42),   S( -8,  38),   S(-22,  34),   S(-35,  50),   S(-39,  43),   S(-43,  35),
            S( 40,  40),   S( -3,  56),   S(  9,  42),   S(-26,  51),   S(-20,  48),   S( 16,  29),   S( -3,  41),   S(-34,  41),
            S( 75,  17),   S(  8,  54),   S( 24,  44),   S(-45,  55),   S(  1,  26),   S(  4,  40),   S( -2,  40),   S(-50,  57),
            S( -7,  51),   S( 29,  49),   S( 17,  47),   S(-53,  66),   S(-31,  48),   S( 20,  39),   S(-36,  51),   S(-58,  60),
            S(105,  39),   S( 35,  58),   S(  7,  58),   S(-31,  66),   S( -9,  56),   S( 25,  39),   S( 60,  55),   S( 28,  25),

            /* rooks: bucket 2 */
            S(-82,  85),   S(-47,  50),   S(-50,  41),   S(-61,  44),   S(-67,  37),   S(-68,  46),   S(-52,  32),   S(-47,  35),
            S(-78,  77),   S(-79,  71),   S(-47,  46),   S(-64,  35),   S(-53,  32),   S(-54,  34),   S(-80,  43),   S(-60,  20),
            S(-72,  83),   S(-63,  70),   S(-57,  67),   S(-48,  45),   S(-61,  59),   S(-43,  58),   S(-21,  41),   S(-24,  26),
            S(-62,  86),   S(-56,  93),   S(-56,  83),   S(-31,  61),   S(-45,  67),   S(-20,  61),   S(-50,  75),   S(-29,  49),
            S(-31,  75),   S(-37,  84),   S(-46,  83),   S(-14,  59),   S(-19,  65),   S(-14,  73),   S(-35,  83),   S(-43,  71),
            S(-17,  76),   S(-51,  86),   S(-21,  67),   S(-11,  47),   S(  2,  48),   S( 50,  47),   S( 39,  44),   S(-30,  68),
            S(-49,  77),   S(-82, 107),   S(-50,  86),   S(-26,  59),   S(  8,  56),   S( 56,  37),   S(-33,  80),   S(-37,  74),
            S(-18,  96),   S(-28,  97),   S(-37,  80),   S(-43,  72),   S(-48,  85),   S( -7,  86),   S(-34, 109),   S( 32,  57),

            /* rooks: bucket 3 */
            S(  0, 103),   S(  1,  93),   S(  5,  78),   S( 16,  64),   S(  9,  62),   S( -9,  85),   S(  7,  88),   S(-12,  73),
            S(-21, 105),   S(-14, 101),   S(  2,  83),   S(  4,  72),   S( 18,  68),   S(  8,  67),   S( 43,  32),   S( 39,  -6),
            S(-35, 106),   S(-18,  99),   S( -8,  99),   S( 13,  72),   S( 15,  74),   S( 24,  71),   S( 38,  85),   S( 19,  65),
            S(-17, 110),   S(-11, 112),   S(  7,  95),   S( 33,  77),   S( 22,  86),   S( 15, 109),   S( 54,  88),   S( 10,  90),
            S( -3, 115),   S( 17, 106),   S( 19,  85),   S( 33,  82),   S( 23,  85),   S( 48,  78),   S( 85,  75),   S( 64,  59),
            S(  0, 111),   S( 18, 102),   S(  8,  91),   S( 15,  82),   S( 30,  70),   S( 45,  76),   S( 99,  48),   S(104,  46),
            S(-12, 118),   S( -8, 122),   S(  3, 106),   S( 14,  90),   S( 17,  83),   S( 37,  71),   S( 67,  90),   S(110,  51),
            S(-50, 180),   S( 20, 126),   S( 36,  91),   S( 70,  73),   S( 47,  84),   S( 89,  71),   S(131,  75),   S(111,  71),

            /* rooks: bucket 4 */
            S(-61,  11),   S(-22,   1),   S(-32,  -5),   S(-16,   4),   S(-51,   5),   S( -8,  -8),   S(-12, -15),   S(  7, -10),
            S(-42,  16),   S(-48,   6),   S(-57,  18),   S(-12,   4),   S(  6,  -6),   S( 18, -24),   S( 39, -30),   S( 10, -11),
            S(-25,  20),   S(-10, -10),   S(-28,  27),   S(-16, -13),   S(-20,   2),   S( 37, -24),   S(  8, -22),   S(-25,  -3),
            S(-91,   1),   S(-29,  21),   S(-54,  11),   S(-14,  19),   S( 26,  -8),   S( -2,   9),   S(  4,  -4),   S(-28,  28),
            S(-21,   1),   S( -9,  19),   S(-12,  36),   S( 61,  -8),   S( 46,   8),   S(  9,   2),   S( -9,   0),   S(  9,  16),
            S(-11,  21),   S( 37,  -4),   S( 61, -17),   S( 34,  -1),   S( 25,  18),   S( 36,  12),   S(  9,  34),   S( 13,  32),
            S( 25,  18),   S( 21,  33),   S( 38,  12),   S( 66,   7),   S( 69, -11),   S( 34, -16),   S( 32,  17),   S(-22,  33),
            S( 40, -27),   S( 48,  51),   S( 45,   5),   S( 11,  -7),   S( 20,  -4),   S( 32,  12),   S( 38,  23),   S( 14,  30),

            /* rooks: bucket 5 */
            S(-34,  47),   S(-20,  56),   S(-37,  50),   S(-46,  44),   S(  1,  15),   S(-21,  46),   S( 23,  23),   S( 10,  34),
            S(-55,  41),   S(-16,  51),   S(-76,  67),   S(-62,  53),   S(-72,  55),   S(-36,  45),   S(-10,  32),   S(-41,  35),
            S(-39,  58),   S(-44,  65),   S(-66,  65),   S(-73,  56),   S(-50,  45),   S(-41,  48),   S(-40,  62),   S( 12,  13),
            S(-65,  78),   S(-15,  61),   S(-56,  83),   S(-31,  68),   S(-59,  73),   S( -9,  56),   S(-14,  51),   S( -3,  45),
            S(-19,  73),   S(-20,  70),   S( 45,  45),   S(  7,  71),   S(  3,  67),   S( 42,  59),   S( 62,  55),   S( 14,  55),
            S( 60,  62),   S( 33,  75),   S( 51,  61),   S( 32,  75),   S( 26,  52),   S( 69,  62),   S( 43,  54),   S( 77,  36),
            S( 54,  50),   S( 37,  65),   S( 83,  44),   S( 60,  55),   S( 29,  44),   S( 55,  44),   S(119,  28),   S( 92,  41),
            S( 83,  52),   S( 95,  47),   S( 41,  55),   S( 29,  50),   S( 63,  39),   S( 87,  35),   S( 52,  53),   S( 18,  55),

            /* rooks: bucket 6 */
            S(-27,  34),   S(-14,  43),   S( -8,  33),   S( -5,  27),   S(-44,  46),   S(-42,  56),   S(-18,  63),   S( 15,  38),
            S(-26,  42),   S(-10,  45),   S( -1,  36),   S(-22,  30),   S(-67,  70),   S(-63,  77),   S(-27,  53),   S(  2,  39),
            S(-55,  76),   S(-41,  64),   S(-40,  63),   S(-66,  53),   S(-32,  44),   S(-98,  85),   S(-54,  80),   S(-47,  66),
            S(-61,  84),   S(  8,  69),   S( -6,  66),   S(-30,  56),   S(-38,  67),   S(-36,  65),   S(-77,  86),   S(-32,  66),
            S( -3,  77),   S( 22,  72),   S( 22,  63),   S( 11,  55),   S(-12,  79),   S(  0,  73),   S( 62,  43),   S(  3,  52),
            S( 29,  69),   S( 65,  63),   S( 77,  44),   S( 70,  31),   S( 48,  58),   S( 60,  64),   S( 55,  56),   S( 70,  59),
            S( 52,  67),   S( 69,  58),   S( 78,  34),   S( 86,  26),   S( 98,  31),   S( 75,  42),   S( 99,  34),   S( 28,  59),
            S( 91,  70),   S( 87,  58),   S( 26,  54),   S( 97,  26),   S( 46,  66),   S( 90,  54),   S( 77,  59),   S( 74,  45),

            /* rooks: bucket 7 */
            S(-48,  -2),   S(-16,  -3),   S(-28,   5),   S( -6,  -3),   S( 22, -15),   S( -2,   7),   S(-29,  19),   S( 22,  -4),
            S(-63,  28),   S(-25,  16),   S(-20,  -4),   S(-30,   7),   S( -7,  13),   S(  1,  16),   S(-31,  24),   S(-39,  22),
            S(-89,  63),   S(-77,  41),   S(-19,  22),   S(-16,   3),   S(-23,  16),   S(-40,  25),   S(-46,  28),   S(-33,   8),
            S(-57,  47),   S(-23,  46),   S(  0,  27),   S( 33,  13),   S( 24,   8),   S( 12,  15),   S( 33,   8),   S(  8,   2),
            S(-16,  46),   S(-13,  32),   S( 62,  -2),   S( 50,   2),   S( 78,  -5),   S( 92,   2),   S( 87,   3),   S( 41,  -2),
            S(-16,  49),   S( 29,  33),   S( 85, -17),   S( 77, -17),   S( 90,  -7),   S( 96,   1),   S( 67,  23),   S( 36,   7),
            S(  7,  48),   S( 22,  30),   S( 76,  -6),   S(111, -23),   S(103, -18),   S( 81,   9),   S( 62,  32),   S( 28,   7),
            S( 40,  63),   S(  8,  51),   S( 73,   0),   S( 86, -11),   S( 69,   5),   S( 37,  30),   S( 60,  31),   S( 92,   1),

            /* rooks: bucket 8 */
            S(-76, -14),   S(-33,   4),   S(-11,  12),   S(-44,  -8),   S(-58, -21),   S(-18, -45),   S(-22, -21),   S(-46,   4),
            S( -3, -20),   S( -4,  14),   S( -4,  -1),   S(-15, -23),   S(-27, -29),   S(-23, -23),   S(-13, -26),   S(-17, -57),
            S( 14,  -8),   S( -3,   6),   S(-30,   8),   S( -2, -21),   S(-38, -24),   S(-16, -29),   S( -5,  -6),   S(  5,  -5),
            S(-28, -11),   S(  0,  36),   S(-12,  15),   S(  8,  32),   S( -7,  14),   S(-16, -49),   S(  9,   8),   S( 10,  16),
            S(  8,   1),   S( -2,  25),   S(-12,  49),   S( -3,   8),   S(  8, -10),   S( -6,  -5),   S( 10,   3),   S( -9,  -9),
            S(  6,  22),   S(  6,  22),   S( 24,  19),   S( 28,  -3),   S(  4,   9),   S(  2,   0),   S(  6,  10),   S( -1,   3),
            S(-14, -24),   S(-13,   5),   S( 16,   9),   S( 24, -29),   S( 34, -31),   S( 17, -27),   S( 21, -14),   S( 21, -13),
            S( -4, -121),  S(  5,   1),   S( 15, -11),   S( 11,  -3),   S( 13,   0),   S( -8, -37),   S(  8,  12),   S( -3,  12),

            /* rooks: bucket 9 */
            S(-68, -36),   S(-23, -27),   S(-22, -48),   S(-69, -19),   S(-53, -34),   S( -3, -25),   S(  7, -46),   S(-77, -27),
            S(  0, -35),   S( -1, -27),   S(-46, -33),   S(-56, -30),   S(-29, -37),   S( -1, -36),   S(  2, -30),   S( -9, -40),
            S(-15, -38),   S( -5, -42),   S(-16, -41),   S(-19, -14),   S(-29, -40),   S(  1, -28),   S( -3, -19),   S(-23, -32),
            S( -8, -51),   S( -3, -31),   S( -4, -19),   S(-30,  -3),   S( -9, -27),   S(  3, -16),   S(  2,  -7),   S( -6, -34),
            S( -7, -14),   S(-22,  15),   S( -6,   0),   S( -7,  -9),   S( -5,  -8),   S( 14,   2),   S(  7,  11),   S( 24, -15),
            S( 13,  -5),   S( -3, -18),   S(  8,   4),   S(-11,  -7),   S(  0, -18),   S( 32,  11),   S( 10, -22),   S(  5,  -6),
            S( 39, -11),   S( 54, -27),   S( 20, -17),   S( 37, -19),   S( 13, -61),   S( 29, -51),   S( 46, -30),   S( 83, -26),
            S( 57, -81),   S( 26, -51),   S( 37,   2),   S( 20,  16),   S(  5, -25),   S( 28, -27),   S( 29, -14),   S( 18,  -9),

            /* rooks: bucket 10 */
            S(-102, -79),  S(-35, -67),   S(-32, -63),   S(-45, -55),   S(-48, -62),   S(-43, -48),   S( 11, -44),   S(-31, -47),
            S(-19, -31),   S(  3, -44),   S(-26, -42),   S( -9, -36),   S(-31, -31),   S( -9, -44),   S( 30, -21),   S(  0, -37),
            S(-25, -59),   S(-16, -54),   S(-16, -50),   S(-35, -40),   S(-53, -33),   S(-10, -51),   S( 13, -12),   S( 11, -13),
            S(  0, -34),   S( -6, -41),   S( -4, -42),   S(-27, -26),   S(-29, -38),   S(  4, -30),   S( -6, -12),   S(-18, -21),
            S( -9, -24),   S(  5, -47),   S( -6, -36),   S( -2, -39),   S(-13, -43),   S(  5, -23),   S( 22,   9),   S(  4, -17),
            S( 30, -19),   S( 49,  -8),   S( 29, -26),   S(  5, -41),   S( 10, -22),   S( 18,  -5),   S(  8, -10),   S(  8, -21),
            S( 76, -36),   S( 86, -42),   S( 81, -54),   S( 93, -63),   S( 41, -56),   S( 31, -17),   S( 41, -49),   S( 55, -50),
            S( 69, -18),   S( 26, -31),   S( 46, -12),   S( 19, -48),   S( 22,  -6),   S( 34, -28),   S( 28, -28),   S( 22, -36),

            /* rooks: bucket 11 */
            S(-100, -16),  S(-24, -23),   S(-10, -46),   S(-32, -58),   S(-50, -22),   S(-31, -17),   S(-37,  -8),   S(-66, -11),
            S(-30, -24),   S(-20, -28),   S(  0, -43),   S(-19, -25),   S(-17, -15),   S(-37, -19),   S(-21, -23),   S(-52,  -6),
            S(-20, -19),   S(-10, -25),   S(-18, -17),   S( 31, -22),   S( 23, -34),   S(-23,  -4),   S( -7, -31),   S(-20, -23),
            S(  1,  27),   S( -8,  -8),   S(-12, -10),   S( -5, -27),   S( 16,  -9),   S(-30,  23),   S( -9, -12),   S(-33, -41),
            S(  4,   2),   S( 14, -14),   S( 14,  -3),   S( 30, -24),   S( 12, -26),   S(  2,   4),   S( -2, -14),   S(-16,  -7),
            S(  1,  29),   S( 29,  13),   S( 31, -28),   S( 71,  -6),   S( 37,  -7),   S( 24,  -5),   S(-13,  20),   S(  2,   4),
            S( 63,  23),   S( 46,  -1),   S( 76, -37),   S( 73, -24),   S( 43, -22),   S( 38, -13),   S( 21,  20),   S( 37,   0),
            S( 19,  22),   S( 13,   4),   S( 29, -10),   S( 27, -31),   S( 28, -11),   S( -1, -28),   S( 11,  24),   S( 37,   0),

            /* rooks: bucket 12 */
            S(  1, -30),   S(  5, -15),   S(-23, -54),   S(-15, -49),   S(-13, -46),   S( -4, -35),   S(-28, -73),   S(-33, -32),
            S( 11,  -3),   S( 12,   5),   S( -8, -13),   S(  2,   5),   S(-13, -17),   S( -4, -30),   S( 10,  12),   S(  8,   8),
            S( 21,  22),   S(-11, -37),   S(-21, -44),   S(-21, -53),   S(-15, -30),   S(  6,  -1),   S( -5, -12),   S(-14, -44),
            S( -9,  -2),   S(-12, -37),   S(  4,  -3),   S(  8,  -4),   S( -8, -30),   S( -5, -26),   S( -4, -32),   S( -6, -22),
            S(-22, -33),   S(-16, -12),   S(  7, -14),   S(  2, -26),   S(-17, -56),   S( -1, -46),   S( -8, -46),   S( 17,  24),
            S(-13, -40),   S( -7, -34),   S( 24, -17),   S( 20,  -4),   S(-18, -62),   S( -8, -60),   S(  3, -17),   S( -6, -19),
            S( -5, -20),   S(  6,   0),   S(  5, -41),   S( -1, -28),   S( -3, -35),   S( -1, -40),   S( -2, -35),   S(  6, -12),
            S( -5, -42),   S( -1, -33),   S( -1, -55),   S( -4, -35),   S(  1, -23),   S(-11, -46),   S(-11, -57),   S(  4,   1),

            /* rooks: bucket 13 */
            S( -9, -49),   S(  1,  -6),   S( -1,  -1),   S( -6,  29),   S( 10,  17),   S(-17, -47),   S(  4,  -6),   S(-30, -50),
            S(  6,   2),   S( -9,  -2),   S(-23,  -6),   S(  0,  12),   S(-14,  -6),   S( -4, -22),   S(  5, -17),   S(  4,  -6),
            S(-10, -42),   S(  1, -16),   S(-23, -54),   S( -3, -21),   S( -9,  -7),   S(  4,  -9),   S(-13, -43),   S(  4, -21),
            S(-14, -61),   S(-14, -43),   S(-12, -56),   S(-15, -26),   S(  0, -25),   S( -6, -17),   S(-10, -30),   S( -5, -24),
            S(  1, -23),   S(  2, -42),   S(  9, -27),   S(  8, -24),   S( -6, -38),   S(-11, -74),   S(-10, -49),   S(  2,  -6),
            S( -9,  -6),   S( 10,  14),   S(-11,  -8),   S(  1, -29),   S(  9, -33),   S(  9, -16),   S(  0, -28),   S(  1, -10),
            S(-15, -34),   S( 11,   8),   S(-11, -42),   S( -1, -32),   S( -6, -45),   S(  1, -20),   S(  4, -14),   S(  1, -21),
            S(-25, -143),  S(-18, -78),   S(  1,  -1),   S(  8,  19),   S( -7, -33),   S(-13, -27),   S(-12, -54),   S( 16,  24),

            /* rooks: bucket 14 */
            S(-12, -43),   S(-20, -36),   S(-14, -58),   S(-18, -69),   S( -5,   5),   S(-17, -22),   S( -8, -29),   S(-12, -21),
            S(-28, -52),   S( -5, -28),   S(-27, -39),   S(-17, -13),   S(-11, -35),   S(-12, -19),   S(  1,  -7),   S( -9, -30),
            S(-17, -61),   S(-15, -50),   S( -7, -43),   S(-18, -57),   S(-15, -18),   S(-10, -40),   S( 13,   8),   S( -6, -27),
            S( -5, -49),   S(-14, -52),   S( -1, -41),   S(-26, -62),   S(-18, -52),   S(-11, -51),   S( 10, -25),   S( -8, -31),
            S(  7, -31),   S(  6, -45),   S( -9, -83),   S(-14, -75),   S( -5, -79),   S( 15, -22),   S(  4, -41),   S( 16,   1),
            S( -7, -30),   S( -1, -37),   S(  9, -65),   S( -2, -90),   S( 14, -77),   S(  9, -41),   S( 19, -35),   S( -3, -34),
            S(  2, -11),   S(  1, -49),   S( -7, -72),   S(  3, -80),   S(  2, -70),   S(  2, -27),   S( 18,   6),   S(  6,   0),
            S(-11, -42),   S( -1, -23),   S(-13, -72),   S( -4, -47),   S(-11, -44),   S(  5,   1),   S( -4, -28),   S( -4, -24),

            /* rooks: bucket 15 */
            S( -8, -31),   S( -7, -34),   S( -2, -17),   S(-26, -68),   S( -7, -50),   S( -4, -17),   S( -3, -28),   S( -9, -20),
            S(-10, -27),   S(-20, -69),   S(-11, -45),   S(-13, -40),   S(-32, -58),   S( -4,   1),   S(-12, -15),   S(  6,  23),
            S(-10, -31),   S( -9, -45),   S(-16, -57),   S( 13, -19),   S(  7, -27),   S( -5, -19),   S( -6,  -5),   S( -6, -26),
            S( -6, -38),   S( -1, -35),   S(-17, -65),   S(  3, -21),   S( -3, -27),   S(  5, -11),   S(  2, -23),   S( -8, -10),
            S( -1, -43),   S( -7, -70),   S(  8, -45),   S( -1, -64),   S(  1, -50),   S(  1, -50),   S( 14, -16),   S(-19, -20),
            S( 11,  -2),   S(  6, -31),   S( -6, -76),   S( 12, -67),   S( -8, -82),   S( 18, -51),   S(  8, -31),   S( -8,  -5),
            S( 16,  11),   S(  8, -21),   S(  2, -36),   S( 11, -49),   S( 12, -25),   S( 13, -19),   S( 27,  14),   S(  8,  17),
            S( -8, -29),   S( -4, -46),   S( -5, -44),   S(  5, -35),   S( -2, -56),   S(  0, -53),   S( -3, -25),   S(  4,  15),

            /* queens: bucket 0 */
            S(-50, -48),   S(-13, -46),   S( 58, -119),  S( 39, -54),   S( 39, -38),   S( 39,  -9),   S( 44,  -5),   S( 28, -16),
            S(-46, -16),   S( 31, -42),   S( 38, -51),   S( 42, -43),   S( 37,  -5),   S( 28,  -4),   S( 19,  54),   S( 26,  19),
            S( 20, -17),   S( 26,  32),   S( 11,  33),   S( 17,   7),   S( 12,  -2),   S( 18,  -6),   S( -2,  40),   S( 23,  31),
            S(  4,  35),   S( 36,  25),   S( -7,  50),   S( 14, -10),   S(-11,  25),   S(  8, -19),   S( 39,   3),   S( 20,  32),
            S( 30,  48),   S( 35,  29),   S(  4,  30),   S( 18,  -3),   S(  2,  -3),   S(  1,  -4),   S( 23,   2),   S( 24,   8),
            S( 18,  41),   S( 15,  77),   S( 47,  17),   S( 36,  16),   S( 45,  -8),   S( 42,  -4),   S( 38,  -4),   S( 31, -35),
            S( 40,  34),   S( 50,  42),   S( 17,  32),   S( 57,   3),   S( 31,   3),   S( -8,   7),   S( 44,  29),   S( 35,  16),
            S( 49,  28),   S( 71,  49),   S( 65,  21),   S( 22,  23),   S( 49,  42),   S( 17,   2),   S( 45,  22),   S( 48,  20),

            /* queens: bucket 1 */
            S(-17, -25),   S(-44, -51),   S(-50,  -4),   S(-10, -90),   S(  6, -33),   S(  4, -62),   S( 48, -59),   S(  9,  19),
            S(-17, -35),   S(-10, -50),   S( 10, -23),   S( 12,   8),   S( 12, -11),   S( 24,  -8),   S( 37, -18),   S(  1,  46),
            S(-17,  30),   S(  6,  -2),   S( 21,  19),   S(-11,  35),   S(  5,  14),   S( -2,  23),   S( 28,   1),   S( 20,  26),
            S( 12,  -6),   S(  5,  40),   S(-10,  65),   S( 26,  25),   S(  4,   2),   S( 17,  15),   S( -4,  40),   S( 34,   6),
            S( 20,  27),   S( -4,  69),   S(-22,  68),   S(-33,  60),   S(-25,  56),   S(  8,  27),   S(  5,  30),   S( 12,  11),
            S( 35,  14),   S( 43,  26),   S( 23,  68),   S(-34,  78),   S(-19,  61),   S(-14,  64),   S( 29,  35),   S( 33,  41),
            S( 19,  53),   S( -1,  61),   S(  6,  42),   S(-31,  46),   S(-31,  54),   S( 46,  10),   S( -7,  39),   S(-14,  46),
            S( -8,  38),   S( 28,  51),   S( 38,  11),   S(  5,  26),   S( 39,  60),   S( 19,  29),   S( 26,  31),   S(-31,  30),

            /* queens: bucket 2 */
            S( 38,   0),   S( 27, -16),   S( 18, -17),   S( -8, -18),   S(-27,  11),   S(-11,  -6),   S( -9, -24),   S( -5,  37),
            S( 29,   4),   S( 38, -11),   S( 27, -16),   S( 34, -20),   S( 25, -15),   S( 26, -29),   S( 39, -44),   S( 35, -26),
            S( 24,   2),   S( 25,   9),   S( 24,  26),   S(  8,  37),   S( 18,  42),   S( 14,  64),   S( 14,  25),   S( 31,  14),
            S( 16,  34),   S(  9,  43),   S(  6,  32),   S(  9,  48),   S(-14,  64),   S( 11,  78),   S( 15,  37),   S( 17,  59),
            S( 22,  10),   S( -1,  54),   S(-24,  45),   S(-43,  93),   S(-19,  79),   S( -8,  93),   S(-12, 101),   S(  3,  95),
            S( 16,  11),   S(  4,  71),   S(-23,  92),   S( 11,  52),   S(-35, 101),   S(-37, 134),   S( 12,  77),   S(  4,  86),
            S( -8,  65),   S(-36, 108),   S( -1,  61),   S(  6,  50),   S( 16,  75),   S( 11,  76),   S(-27,  98),   S(-35, 112),
            S(-63, 117),   S( 14,  72),   S( 27,  66),   S( 46,  44),   S( 14,  78),   S(  9,  23),   S( 29,  46),   S(-15,  75),

            /* queens: bucket 3 */
            S( 75, 108),   S( 61, 105),   S( 52, 113),   S( 40,  84),   S( 64,  48),   S( 44,  35),   S(  4,  52),   S( 35,  83),
            S( 67, 128),   S( 58, 126),   S( 42, 117),   S( 45, 100),   S( 49,  93),   S( 64,  62),   S( 70,   8),   S( 30,   1),
            S( 51, 124),   S( 50, 117),   S( 61,  97),   S( 41,  94),   S( 48,  93),   S( 44, 119),   S( 52, 105),   S( 56,  62),
            S( 40, 148),   S( 53, 102),   S( 50,  93),   S( 35,  95),   S( 37,  82),   S( 42, 119),   S( 44, 122),   S( 42, 139),
            S( 62, 127),   S( 43, 113),   S( 27, 111),   S( 15, 114),   S( 20, 119),   S( 22, 146),   S( 20, 177),   S( 49, 165),
            S( 41, 135),   S( 57, 130),   S( 46, 112),   S( 14, 142),   S( 27, 141),   S( 54, 129),   S( 43, 175),   S( 23, 213),
            S( 71, 133),   S( 53, 137),   S( 79,  95),   S( 65, 104),   S( 35, 128),   S( 52, 133),   S( 74, 151),   S(127, 101),
            S( 95,  99),   S( 87, 126),   S( 80, 113),   S( 81,  99),   S( 82, 110),   S(112,  78),   S(138,  96),   S(127,  84),

            /* queens: bucket 4 */
            S(-33, -55),   S(-25, -17),   S(-43, -22),   S( -4,   2),   S( 13, -77),   S( 25, -13),   S(-45,   0),   S(-21,   8),
            S( -1, -16),   S(-28,  18),   S( 21, -34),   S(-49,  16),   S( -5,  -9),   S(  2, -21),   S( -5, -21),   S(-53, -28),
            S(-13, -18),   S(  6,   7),   S( -6,  44),   S( 20,  12),   S( 30,  -6),   S(  3,  -7),   S( -5, -35),   S(-31, -18),
            S( -2,   3),   S(-13,  12),   S( 20,  23),   S(-20,  13),   S( 25, -12),   S( 24,  13),   S(  5, -25),   S(  0,   4),
            S(-24,   2),   S(  4,  29),   S( 47,  27),   S( 42,  13),   S( 16, -13),   S(  8, -25),   S(  7, -25),   S( 13,   8),
            S( -9,   7),   S( 36,  37),   S( 12,  42),   S( 16,  19),   S( 19,  13),   S(  5,  -2),   S( -8, -28),   S(-31, -36),
            S(-40, -56),   S(-12,  13),   S(-31,  16),   S( 17,  31),   S(  5,  19),   S(  4,  17),   S(-19, -48),   S(-26, -35),
            S( -2,  -9),   S( 13,  13),   S( 32,  17),   S( 22,  41),   S(-10,  -2),   S( -6, -11),   S(-12, -24),   S( 12,  -6),

            /* queens: bucket 5 */
            S(-64, -53),   S(-34, -45),   S(-49, -52),   S(-44, -65),   S(-59, -13),   S( 24,   1),   S( 16, -22),   S( -9,  -1),
            S(-33, -23),   S(-73, -19),   S(-66, -14),   S(-49,  -5),   S(-27,  23),   S(-36,   4),   S(-68,  12),   S(-34,  19),
            S(-48, -16),   S(-74,   2),   S(-79,  23),   S(-23,  50),   S(  5,  52),   S(-28,  21),   S(-19, -18),   S(  8,   9),
            S(-62,  17),   S(-22,  26),   S(-28,  76),   S(-25,  53),   S( -9,  54),   S( 20,  34),   S( 20, -11),   S(-26,  46),
            S(-33,  27),   S(-15,  23),   S(-11,  69),   S( 20,  48),   S( 15,  53),   S(-16,  17),   S(-11,   2),   S(-41,   1),
            S(-28,  14),   S( 21,  47),   S(-25,  42),   S( 28,  81),   S( 27,  45),   S(  2,  30),   S(  4,  -7),   S(-22, -16),
            S(-22,  28),   S( 16,  21),   S( -1,  41),   S(  7,  36),   S( 41,  68),   S( -6,  35),   S( 26,  26),   S( -4,   6),
            S( 12,  41),   S( 31,  46),   S( 12,  54),   S(-11,  33),   S( 19,  15),   S( 10,   0),   S(  6,  19),   S(  8,  -4),

            /* queens: bucket 6 */
            S(-14,   5),   S(-52,  28),   S(-47, -30),   S(-64, -34),   S(-71,   0),   S(-64, -19),   S(-41, -30),   S( 10,  12),
            S(-26,  11),   S(-92,   8),   S(-44,  26),   S(-65,  42),   S(-79,  77),   S(-73,  27),   S(-60,   0),   S( -8,  16),
            S(-46,  38),   S(-43,  15),   S(-48,  36),   S(-105, 112),  S(-39,  95),   S(-49,  15),   S(-44,  12),   S(-35,  16),
            S(-20,  29),   S(-31,  41),   S(-33,  71),   S(-29,  74),   S( 24,  30),   S( -1,  64),   S(-10,  73),   S(-12,  36),
            S(-72,  48),   S(-43,  42),   S( 22,  28),   S( 22,  41),   S( 35,  53),   S( 31,  64),   S( 22,  56),   S(  0,  32),
            S(-27,  77),   S(-15,  31),   S( 24,  14),   S( 31,  31),   S( 16,  50),   S( 54,  61),   S(  2,  52),   S(-12,  32),
            S( -4,  52),   S(-23,  35),   S(  7,  34),   S( 39,  38),   S( 52,  63),   S(-15,  37),   S(  6,  11),   S(  3,  33),
            S( -6,  30),   S( 17,  24),   S( 29,  39),   S( 31,  51),   S( 32,  48),   S( 23,  62),   S( 17,  29),   S(-19,  -1),

            /* queens: bucket 7 */
            S(-54,  18),   S(-25,  32),   S(-28,  14),   S(-47,  24),   S(-19,   8),   S(-34,   9),   S(-37,   0),   S(-27, -24),
            S(-74,  13),   S(-72,  24),   S(-66,  39),   S(-19,  24),   S(  5,   3),   S(-54,  73),   S(-78,  62),   S(-26,   3),
            S(-46,  26),   S(-65,  34),   S( 15,  -8),   S(  6,  -5),   S( 31,   3),   S( 20,  23),   S(-57,  -1),   S(-20,  19),
            S(-77,  15),   S(-10,  23),   S(  9,  11),   S( 45, -49),   S( 45, -19),   S( 28,  33),   S(  1,  61),   S(-11,  50),
            S(-46,  25),   S(-22,  19),   S( 17, -19),   S( 72, -41),   S( 60, -35),   S( 75,  -6),   S( 12,  26),   S( 46,  12),
            S(-24,  20),   S(-17,  28),   S( 36, -24),   S( 30,  -9),   S( 33,   1),   S( 67,  20),   S( 46,  21),   S( 42,  21),
            S(-28,  17),   S(-12,  13),   S( 20,  -8),   S( 22,  13),   S( 27,   9),   S( 61,  33),   S( 43,  32),   S( 49,  10),
            S(  5,  24),   S( 11,  27),   S(  2,  19),   S( 22,  33),   S( 48,  21),   S( 28,  33),   S( 39,  34),   S( 41,  14),

            /* queens: bucket 8 */
            S( -9, -11),   S(  7,   9),   S(-23,   0),   S(-16,  -9),   S( -7, -18),   S( -8, -28),   S( -4,  -2),   S( -4,  -6),
            S(-11, -20),   S(-17, -30),   S(  5,   9),   S(-30, -24),   S(-26, -36),   S(-21, -32),   S(-21, -44),   S(-12, -24),
            S(  0,  -5),   S(-32, -31),   S(-26, -37),   S(-38, -49),   S(-14,  -6),   S( -6, -19),   S(-13, -40),   S(-25, -43),
            S(-11,  -6),   S( 15,  30),   S(-11,   5),   S(-32, -39),   S( -9,  -5),   S(-19, -36),   S( -4, -16),   S( -9, -33),
            S(  9,  20),   S(  2,  23),   S(  5,   8),   S(-17, -12),   S(  7, -10),   S(-13,  -9),   S( -4, -15),   S(-11, -11),
            S(  9,  30),   S(  2,  13),   S(-14,  26),   S(-19, -17),   S(-17, -38),   S(-15, -30),   S(-21, -35),   S(  0,   0),
            S( -2,  -1),   S(-18, -20),   S( 22,  36),   S( 11,   8),   S(  8,  -1),   S(-19, -23),   S(-23, -41),   S(-13, -20),
            S(-35, -79),   S( 10,  21),   S( -9, -14),   S( -9, -21),   S( -1,  -6),   S( -9, -23),   S(-12, -37),   S( -6, -20),

            /* queens: bucket 9 */
            S( -4,  -3),   S(-22, -51),   S(-16, -21),   S(-34, -37),   S(-43, -71),   S(-27, -51),   S(-15, -28),   S(-28, -43),
            S( -7, -11),   S(-16, -22),   S( -9,  -7),   S(-35, -45),   S(-25, -33),   S(-15, -32),   S( -9, -21),   S( -6, -11),
            S(  1,  -3),   S( -2,   6),   S(-37, -30),   S(-55, -52),   S(-29, -45),   S(-33, -47),   S( -2, -12),   S( -6,  -8),
            S(-13, -19),   S(-20, -18),   S(-20,   8),   S( -9,  10),   S( 10,  17),   S(-15, -30),   S(-23, -26),   S( -3,  -5),
            S(  9,  10),   S(-23,   1),   S( -4,   9),   S(  9,  35),   S(-11,   0),   S(-13,  -6),   S( 14,  17),   S(-21, -14),
            S(-14, -12),   S(-33, -26),   S( 12,  43),   S( -4,  23),   S(  0,   7),   S( -8, -16),   S(  1,   1),   S( -7, -11),
            S(-22, -32),   S(  1,   6),   S( 16,  55),   S( 11,  44),   S( -2, -20),   S(  4,  -6),   S( -3, -10),   S(-16, -31),
            S( -7, -22),   S(-21, -38),   S(-13, -14),   S( 14,  30),   S(  2,   4),   S(-23, -45),   S(  0,  -3),   S( -6, -17),

            /* queens: bucket 10 */
            S( -2,  12),   S(-21, -46),   S(-17, -30),   S(-47, -53),   S(-21, -32),   S(-12, -19),   S( -5, -22),   S( -1, -10),
            S(-17, -30),   S(-10, -36),   S(-15, -28),   S(-21, -33),   S(-48, -56),   S(-13, -24),   S(  1, -16),   S( -8,  -7),
            S( -7, -15),   S(-33, -49),   S(-33, -45),   S(-50, -50),   S(-70, -56),   S(-14, -15),   S(  5,  13),   S( -6,  -8),
            S( -7,  -4),   S(-19, -43),   S(-41, -68),   S(-21, -20),   S(  1,  18),   S(-39, -18),   S( -6, -12),   S(-13, -14),
            S(-21, -28),   S(-27, -41),   S( -3,   3),   S( -2,  13),   S(  8,   7),   S( 13,  26),   S( 11,  33),   S(-13,  -6),
            S(  0,   6),   S(-25, -39),   S(-31, -30),   S(-33, -24),   S( -2,  16),   S( -2,  34),   S(-10,  -6),   S( -9, -15),
            S(-14, -17),   S(-17, -27),   S(-21, -27),   S(-17, -17),   S(  2,  10),   S(  7,  34),   S(  4,   7),   S( 13,  15),
            S(-12, -22),   S(  1,   4),   S( -8,   2),   S(  7,   5),   S(-19, -28),   S( -2,  -2),   S( -5,  -7),   S(-22, -31),

            /* queens: bucket 11 */
            S(-14, -26),   S( -9, -36),   S(-22, -38),   S( -5, -16),   S(-17, -31),   S(-27, -32),   S(  5,   7),   S( -5, -15),
            S(-13, -31),   S(-16, -41),   S(-64, -72),   S(-27, -30),   S(-24, -30),   S(-19, -29),   S(-14,  -3),   S( 16,  19),
            S(-34, -42),   S(-32, -69),   S(-40, -57),   S(-16, -49),   S(-14, -28),   S(  9,  -1),   S(  1,  10),   S(-20,  -5),
            S(-29, -41),   S(-24, -40),   S(-28, -55),   S(-21, -35),   S(  0, -17),   S(-18,   6),   S( -5,  23),   S(-39, -47),
            S(-19, -31),   S(-12, -34),   S(-24, -39),   S(-13, -30),   S( 13, -13),   S( 28,  29),   S( 13,  26),   S( -8,   5),
            S(-35, -64),   S(-17, -32),   S(-40, -59),   S( -9, -24),   S(-16, -28),   S( 35,  29),   S( 25,  39),   S(-21,  -4),
            S( -6,  -3),   S(-18, -53),   S(-34, -33),   S( -9,  -3),   S(  3, -12),   S( 21,  37),   S( 41,  42),   S(  9,   9),
            S(-12, -12),   S( -7, -26),   S( -4, -11),   S( -1,  -7),   S( -5,  -4),   S( 10,  -7),   S(  6,  12),   S(-12, -40),

            /* queens: bucket 12 */
            S(  5,  10),   S(-11, -19),   S(  1,   4),   S(-13, -15),   S(-17, -27),   S( -3,  -9),   S( -5, -15),   S( -3,  -2),
            S(  2,   7),   S( -4,  -6),   S(  0,   1),   S( -9, -19),   S(-12, -27),   S( -9, -10),   S( -6, -18),   S(-10, -18),
            S( -4,  -9),   S( -5, -12),   S(  0,  -7),   S( -4, -16),   S(-12, -25),   S(-15, -34),   S( -8, -25),   S( -4,  -6),
            S(  2,   0),   S(-10, -12),   S(-10, -20),   S(-24, -43),   S(-13,  -7),   S( -4, -11),   S(-10, -13),   S(  1,  -8),
            S( -3,  -2),   S( -6,  -6),   S( 17,  25),   S(-20, -21),   S(-14, -46),   S(-11, -32),   S(-14, -28),   S(  5,  17),
            S( 12,  24),   S(  6,  11),   S( 25,  34),   S(-13, -19),   S( -2,  -6),   S( -4,  -9),   S( -7, -19),   S( -2,  -4),
            S(  5,  13),   S( 14,  15),   S(  4,  12),   S( 13,  14),   S( 12,  15),   S( -4, -13),   S(  3,   7),   S( -4,   4),
            S(  3,   0),   S(-18, -30),   S( -8,  -1),   S( -6,  -4),   S( -1,   6),   S( 10,  13),   S(  1,   7),   S( -1,  -6),

            /* queens: bucket 13 */
            S(  3,   7),   S( -7, -19),   S(-10, -30),   S( -5, -14),   S( -7, -13),   S(-13, -34),   S(-11, -28),   S( -7, -10),
            S(  0,   0),   S(  0,  -5),   S( -5, -16),   S(-13, -30),   S(-22, -37),   S( -7, -20),   S(-15, -33),   S( -5, -11),
            S(-10, -26),   S( -9, -20),   S( -3, -13),   S(-26, -53),   S( -1, -21),   S(-18, -41),   S(-10, -24),   S( -9, -18),
            S(  1,   7),   S( -7, -19),   S( -7, -32),   S(  6, -22),   S(-12, -21),   S(-13, -35),   S(-12, -28),   S(-10, -27),
            S(  6,   6),   S( -1,   7),   S(  7,  -5),   S(-10, -14),   S( 10,   4),   S( -1, -17),   S(-13, -35),   S( -9, -22),
            S( -4,  -7),   S( -4,  -8),   S( 29,  39),   S( 13,  28),   S( -3,   8),   S( -2, -18),   S(  7,  14),   S( -5, -14),
            S( -5,  -7),   S(  8,  27),   S( 27,  57),   S(  2,  14),   S(  8,  10),   S(  5,   7),   S( -6, -16),   S( 11,  18),
            S(-16, -24),   S(  4,   8),   S( -3,  -1),   S( -7, -11),   S(  2,   6),   S( -1, -13),   S( -4, -13),   S(-16, -26),

            /* queens: bucket 14 */
            S(  6,  19),   S( -5, -20),   S( -1, -15),   S( -8,  -9),   S(  3,   5),   S( -2,  -9),   S( -8, -22),   S(  0,  -8),
            S(-12, -28),   S(  1,   0),   S(-12, -34),   S(-13, -37),   S( -9, -16),   S( -3,  -8),   S(-11, -29),   S( -4,  -8),
            S(  0,  -6),   S( -6, -21),   S(-12, -25),   S(-12, -31),   S( -9, -25),   S(-13, -26),   S(  0,  -3),   S(  1,  -4),
            S( -5, -13),   S( -7, -25),   S(-13, -19),   S(-12, -31),   S( -8, -28),   S(  6,   2),   S(  8,   8),   S( -3, -10),
            S(-10, -16),   S( -1,  -9),   S(-12, -34),   S(-11, -26),   S( -6, -21),   S(  5,  -1),   S(  8,   3),   S( -3, -11),
            S( -5, -14),   S( -3, -15),   S(-14, -24),   S( -7, -18),   S(  9,  26),   S(  1,   7),   S(  8,   9),   S(  3,  -6),
            S(  2,   6),   S( -1, -16),   S( 28,  51),   S( 23,  44),   S( 14,  18),   S( 13,  25),   S( 17,  34),   S(  9,  13),
            S(-15, -33),   S(  1,   3),   S( -6,  -8),   S( 20,  24),   S(  5,   9),   S( 14,  26),   S( -1,  -9),   S(-23, -49),

            /* queens: bucket 15 */
            S( -6, -12),   S(  2,   0),   S( -3,  -7),   S(-10, -24),   S( -5, -10),   S( -7, -17),   S( -5, -15),   S(  1,   7),
            S( -5, -11),   S( -2,  -9),   S( -9, -26),   S( -7, -14),   S( -4, -13),   S(  0,   6),   S(  9,  15),   S(  3,  -3),
            S( -1,  -8),   S( -9, -24),   S( -3, -11),   S(-12, -28),   S(-12, -26),   S(  0,  -6),   S(  3,   2),   S(  2,   6),
            S(  8,  11),   S( -5, -15),   S( -6, -10),   S(  4,  -2),   S(-10, -23),   S( -9, -14),   S(  4,   1),   S(  6,   7),
            S(  1,   1),   S( -4, -17),   S( -4, -13),   S(-14, -38),   S( -7, -29),   S( -3, -12),   S(  8,   5),   S(  3,  -1),
            S( -5, -19),   S( -3,  -5),   S(-13, -25),   S( -1, -14),   S(-11, -30),   S( 23,  31),   S( 13,  26),   S(  1,   2),
            S( -1,  -6),   S(  1,  -5),   S(  1,  -5),   S( -4, -20),   S(  5,   4),   S(  1,  -7),   S( 16,  26),   S(  6,  11),
            S( -5, -11),   S(  3,  -6),   S( -1,  -7),   S(  9,  14),   S(  6,   5),   S(  9,  15),   S(  2,  11),   S( -1,  -1),

            /* kings: bucket 0 */
            S( -7,  76),   S( -1,  91),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  2,  70),   S( 76,  80),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(-34,  45),   S(-73,  44),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 30,  34),   S( 10,  38),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-40,  50),   S(-57,  46),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 34,  38),   S( 30,  32),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 13,  70),   S(-25,  69),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 57,  76),   S(  4,  72),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-39, -28),   S( 40, -31),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-47,  -8),   S(  0,  -1),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( -1, -46),   S(-28, -31),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  6, -19),   S( 16, -26),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -6, -28),   S(-28, -27),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 26, -20),   S( -7, -16),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 27,  -2),   S(-36, -11),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 36,  19),   S(-50,  13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-113, -47),  S( 20, -41),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-58, -38),   S(  6, -32),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(-18, -57),   S( 37, -69),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 76, -62),   S( 72, -60),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 23, -72),   S(-15, -58),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 83, -74),   S( 65, -68),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-13, -42),   S(-83, -64),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 97, -67),   S( -7, -58),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-27, -51),   S( 47, -32),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-66, -78),   S(-12, -20),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(-12, -48),   S( 52, -57),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S( 65, -74),   S( 22, -82),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 87, -69),   S( 44, -77),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 26, -81),   S(-18, -59),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 35, -53),   S(-22, -50),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S( 15, -62),   S(  8, -112),

            #endregion

            /* enemy king piece square values */
            #region enemy king piece square values

            /* pawns: bucket 0 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-28,   4),   S(-45,  36),   S( -6,   9),   S(-25,  37),   S(-13,   5),   S( 22,  10),   S( 37,  -1),   S( 31,  -6),
            S(-20,  -5),   S(-36,  20),   S(-14,   1),   S(-12,  -3),   S(  9,   4),   S( -3,  11),   S( 35,  -2),   S( 15,  18),
            S(  1,  -6),   S( -5,   4),   S( 33, -25),   S(  6, -22),   S( 18, -22),   S( 26,  12),   S( 20,  20),   S( 40,   1),
            S( 21,  10),   S( 32,  23),   S( 68, -17),   S( 41,   2),   S( 30,  16),   S( 15,  58),   S( 37,  47),   S( 75,  15),
            S( 94,  -1),   S(108,  33),   S( 93,  12),   S( 60,  47),   S( 40, 141),   S( 53,  64),   S( 25, 119),   S(135,  32),
            S(-133, -40),  S(-107, -41),  S( 75, -109),  S( 50,  94),   S( 93, 139),   S( 84, 134),   S(181,  39),   S( 65,  79),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 1 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-63,  32),   S(-57,  31),   S(-32,  22),   S(-59,  74),   S(-46,  23),   S( -3,   9),   S(  7,   1),   S( -7,  23),
            S(-57,  18),   S(-45,  15),   S(-43,  12),   S(-30,  15),   S(-16,   3),   S(-22,   6),   S( -4,   1),   S(-19,  15),
            S(-37,  23),   S(-14,  22),   S(-25,  13),   S(  6,  -4),   S( -1,   4),   S( -3,   8),   S( -7,  18),   S( -4,  14),
            S(-26,  46),   S( 23,  23),   S(  3,  30),   S( 22,  36),   S( 21,  20),   S( -4,  32),   S( 19,  30),   S( 42,  30),
            S( 26,  40),   S( 50,   8),   S( 94,  20),   S(100,  14),   S( 41,  46),   S( 32,  40),   S(  5,  59),   S( 66,  58),
            S(233, -41),   S( 15,  14),   S(  4, -48),   S( 45, -53),   S(-28, -24),   S(-90, 112),   S( 44, 140),   S( 40, 126),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 2 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-68,  50),   S(-44,  34),   S(-37,  21),   S(-22,  10),   S(-58,  51),   S(-31,  22),   S( -9,   1),   S(-33,  27),
            S(-63,  35),   S(-38,  26),   S(-47,  16),   S(-43,  22),   S(-47,  22),   S(-40,  11),   S(-19,  -3),   S(-50,  15),
            S(-32,  40),   S(-33,  46),   S(-17,  22),   S(-16,   8),   S(-29,  27),   S(-11,   8),   S(-22,  19),   S(-25,  11),
            S(-15,  69),   S(-19,  60),   S( -3,  41),   S(  8,  33),   S(  0,  38),   S( -8,  31),   S( 11,  25),   S( 32,  12),
            S( -6, 111),   S(-31,  87),   S(-14,  50),   S( 47,   0),   S(112,  17),   S(127,  26),   S(117,   1),   S( 81,  -1),
            S(  0, 192),   S( 69, 107),   S( -7,  78),   S( 24, -37),   S(-23, -82),   S(-30, -61),   S( -9,   0),   S(141, -43),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 3 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-17,  33),   S(-16,  34),   S(-14,  26),   S(-11,  27),   S(-31,  66),   S(  9,  35),   S( 10,  20),   S(-17,   3),
            S(-12,  35),   S(  0,  30),   S(-18,  22),   S(-17,  21),   S( -6,  21),   S(  6,  13),   S(  3,  12),   S(-33,  11),
            S( 10,  31),   S( -7,  51),   S(  3,  20),   S(  0,  -1),   S( 16,  -8),   S( 28,  -1),   S(  6,  12),   S(-12,   5),
            S( 23,  65),   S( -4,  83),   S( 17,  52),   S( 20,  24),   S( 35,   0),   S( 47,  -9),   S( 25,  32),   S( 35,   5),
            S( 27, 113),   S(-21, 136),   S(-27, 146),   S(  6, 112),   S( 43,  60),   S(111,  19),   S(113,  24),   S(115,   9),
            S( 50, 108),   S( 18, 180),   S(-27, 246),   S( 15, 181),   S(-19, 100),   S( 31, -76),   S(-74, -83),   S(-156, -71),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 4 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 58,   3),   S( 19,   3),   S( -5, -10),   S(-23, -10),   S(-17, -16),   S(-30,  -5),   S(-45,  12),   S(-64,  26),
            S( 31,  -8),   S( 13,   8),   S( 44, -27),   S(-13, -13),   S(-46, -17),   S(-18, -18),   S(-78,  14),   S(-54,   9),
            S( 79,   4),   S(115, -19),   S( 50, -15),   S(-20, -19),   S(-59, -14),   S( -6,  -7),   S(-73,  12),   S(-61,  18),
            S(-32, -41),   S( 53, -76),   S( 76, -31),   S( 12,  -6),   S(-24,   8),   S(-25,  31),   S(-14,  22),   S( -2,  16),
            S( 38, -35),   S( -3, -76),   S( 29, -44),   S( 65,  19),   S( 80,  68),   S( 41,  55),   S( 16,  44),   S( 56,  36),
            S( 45, -15),   S( 27, -14),   S( 16, -60),   S(  7,  36),   S( 58,  73),   S( 79, 105),   S( 39,  88),   S( 12,  80),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 5 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-57,  42),   S( -2,  20),   S( -8,  14),   S( 49,  -9),   S( 60, -23),   S( 17, -10),   S(-15,   4),   S(-55,  32),
            S(-48,  20),   S( -1,   8),   S(  8,  -4),   S(  3,   9),   S( -1,  -8),   S( 16, -19),   S(-45,   1),   S(-80,  25),
            S(-17,  24),   S( 36,  22),   S( 47,  19),   S(  8,  32),   S(-15,  18),   S( -1,   2),   S(-20,   9),   S(-62,  28),
            S( 12,  31),   S(  4,  15),   S(  6, -25),   S(-54,   7),   S( 32, -14),   S(  6,  10),   S( 42,   3),   S( 37,  12),
            S( 82,  17),   S( 67, -31),   S( 58, -46),   S( 12, -19),   S( 95, -35),   S( 69,  24),   S( 64,  38),   S( 24,  65),
            S( 93,  28),   S( 84, -15),   S( 43, -67),   S( 57, -47),   S(-20, -51),   S( 69,  32),   S( 83,  87),   S(107,  65),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 6 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-75,  33),   S(-34,   7),   S(-18,   1),   S(  1,   8),   S( -3,  16),   S( 29,  -1),   S( 33,  -3),   S( 20,  10),
            S(-71,  24),   S(-34,  -3),   S(-30,  -5),   S( 35, -16),   S( -1,   7),   S( 22,  -7),   S( 18,  -4),   S( 14,  -4),
            S(-37,  22),   S(-24,  12),   S( -2,   8),   S(  8,   9),   S( 33,  14),   S( 66,  -4),   S( 53,   1),   S( 40,  -7),
            S( -1,  35),   S( -4,  22),   S( 14,   7),   S( 24,   4),   S(-22, -27),   S( 19, -31),   S( 54, -10),   S(114, -28),
            S( 60,  53),   S( 47,  16),   S( 39,  13),   S(  6, -12),   S( 39, -40),   S( 43, -38),   S(103, -36),   S(143, -14),
            S(167,   9),   S(136,  35),   S(136,  -3),   S( 70, -57),   S( 63, -96),   S( 51, -90),   S( 29, -11),   S(135,  -2),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 7 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-60,  12),   S(-52,   2),   S(-30,  -6),   S(-49,   5),   S( 17,  -3),   S( 53, -19),   S( 67, -23),   S( 55,  -9),
            S(-41,   0),   S(-58,   3),   S(-39, -15),   S(-30,  -8),   S( 11, -15),   S( 53, -34),   S( 38, -12),   S( 44, -16),
            S(-31,   5),   S(-52,  11),   S(-34,  -2),   S(-30, -18),   S(  1, -22),   S( 39, -20),   S( 79,  -7),   S( 61,  -8),
            S( -7,  15),   S(-26,  21),   S(-19,  18),   S(-20,  10),   S( 12, -20),   S( 71, -49),   S( 30, -51),   S( 25, -72),
            S( 69,   7),   S(-29,  69),   S(  3,  65),   S( 34,  50),   S( -9,  40),   S(  3, -34),   S(-41, -74),   S(-27, -49),
            S(142,  11),   S(142,  31),   S(123,  59),   S( 79,  73),   S(106,   0),   S( 20, -78),   S( 34, -53),   S( 52, -125),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 8 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 37, -24),   S(  2, -31),   S( 86, -34),   S(-22, -28),   S(-10, -43),   S( 51, -47),   S( 44, -50),   S( 25, -39),
            S(-34, -54),   S(-28, -14),   S(-59, -60),   S(-40, -44),   S(-55, -47),   S(-34, -30),   S( -7, -38),   S(-26, -23),
            S(-43, -50),   S( 37, -55),   S( -5, -39),   S(-29, -58),   S(-27, -30),   S(-21, -27),   S(-95,  -3),   S(-84, -12),
            S(-34,  -5),   S(-17, -11),   S( 33, -32),   S( 17, -17),   S( 32, -12),   S( 26,  10),   S(-20,   8),   S(  7,  -9),
            S( 36,  38),   S(-19, -30),   S( 36,  45),   S( 29,  47),   S( 33,  98),   S( 50,  76),   S( -5,  39),   S(-64,  56),
            S( 20,  41),   S( 17,  41),   S( 41,  65),   S( 19,  30),   S( 47,  70),   S( 39,  96),   S( 26, 102),   S(  5,  35),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 9 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 25, -13),   S( 70, -25),   S( 59, -15),   S(  3,  -5),   S( 20,  -3),   S( 93, -59),   S( 77, -58),   S( -3, -25),
            S(-28, -30),   S(-20, -49),   S(-37, -35),   S(-37, -36),   S(-20, -46),   S( -9, -39),   S(-32, -35),   S(  9, -35),
            S(-82,   3),   S(-31, -40),   S(-32, -51),   S(-57, -39),   S( 10, -46),   S(-24, -38),   S(-45, -35),   S(-64,  -9),
            S(-34,  12),   S(-18, -46),   S( 22, -38),   S( -3, -38),   S( -6, -34),   S(-26, -14),   S(  2, -15),   S(  3, -10),
            S(-39,  17),   S(  7, -33),   S( 39,  -4),   S( 47,   0),   S( 17,   3),   S( 39,  17),   S(-29,  26),   S(-43,  36),
            S(-26,  28),   S( 17,   6),   S( 19,  -1),   S( 46,  17),   S( 49,  50),   S(  4,  38),   S(  3,  28),   S(  4,  58),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 10 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  7, -55),   S(-25, -37),   S( 10, -29),   S(  8, -30),   S( 45, -37),   S(187, -66),   S(114, -44),   S( 93, -60),
            S(-77, -29),   S(-83, -38),   S( 15, -63),   S( -9, -65),   S( -4, -49),   S( 12, -49),   S( 42, -49),   S( 19, -44),
            S(-92, -20),   S(-90, -30),   S(-58, -26),   S(-12, -39),   S(-21, -49),   S(  8, -71),   S( 41, -63),   S( 17, -44),
            S( -9, -27),   S( 11, -28),   S( 15, -23),   S(-13, -48),   S( -4, -55),   S(-37, -35),   S( -5, -42),   S(  8, -28),
            S(  3,   2),   S( 37, -12),   S( 28, -18),   S( 18, -27),   S( 37,   4),   S( 21, -17),   S(-12, -36),   S( 19,  -1),
            S(-20,  -7),   S( 19,   0),   S( 37,  17),   S( 28, -26),   S( 27,   9),   S( 18,  11),   S(-17,  -2),   S(  8,   7),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 11 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-12, -38),   S(-14, -23),   S(-19, -42),   S(  8, -18),   S( 33, -58),   S(125, -51),   S(124, -70),   S(107, -53),
            S(-56, -28),   S(-84, -30),   S(-35, -48),   S(  7, -69),   S(-35, -39),   S(  8, -53),   S( -3, -44),   S( 21, -78),
            S(-57, -21),   S(-71, -26),   S(-24, -38),   S(-20, -43),   S(-43, -46),   S(-20, -35),   S(-37, -58),   S(  7, -52),
            S(-18, -19),   S(-34,  -2),   S( -8,   1),   S( 66, -33),   S(-10, -31),   S(-11, -40),   S(-30, -15),   S( -6, -37),
            S( 11, -31),   S(  7,   5),   S( -3,  37),   S( 41,  48),   S( 52,  50),   S( 39,   6),   S( -3, -16),   S( 12,  -4),
            S(  0,  -3),   S( 27,  24),   S( 37,  35),   S( 46,  58),   S( 29,  31),   S( 43,  43),   S( 11,  20),   S( 12,   6),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -9, -88),   S(-17,  -1),   S(-15, -12),   S( -3,   3),   S(  2, -14),   S(-25, -60),   S( 22, -30),   S(  7, -36),
            S(-18, -61),   S(-14, -12),   S(-41, -55),   S(-49, -48),   S(-61, -44),   S(-41, -14),   S( -6, -43),   S(-20, -38),
            S(-33, -18),   S( 25, -54),   S( -9, -75),   S(-52, -75),   S( -6, -63),   S(-25, -25),   S(-80, -26),   S(-65, -34),
            S(-33,   0),   S(  6, -39),   S(-16, -45),   S( -3, -14),   S( 19,  14),   S(-21,  51),   S(-13,   9),   S(-54,  -2),
            S(  9,  13),   S( -2,  18),   S( -9, -48),   S( 16,  42),   S( 23,  56),   S( 12,  84),   S(-19,  88),   S(-11,  69),
            S( 19,  29),   S( -2,  10),   S( 21,  55),   S( 13,  41),   S(  9,  54),   S( 22,  79),   S(-27, -31),   S(-18,  28),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 13 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-29, -67),   S(-35, -59),   S( -6,  -8),   S( -4, -15),   S(-10, -63),   S( 10, -57),   S(-24, -46),   S( 16, -72),
            S(-61, -54),   S(-35, -81),   S(-57, -58),   S(-19, -55),   S(-27, -63),   S(-69, -39),   S(-13, -59),   S(-23, -53),
            S(-43, -53),   S(-46, -59),   S(-19, -61),   S(-38, -57),   S(-21, -52),   S(  3, -62),   S(-41, -41),   S(-28, -33),
            S(-35,   8),   S(-33, -39),   S( -2, -30),   S(  2,  14),   S( -6,  -2),   S(-29, -10),   S(-10, -27),   S(-57,  11),
            S(-13,  19),   S(  7,  71),   S( -2, -12),   S( 20,  34),   S( 22,  60),   S(  6,  72),   S( 11,  41),   S(-27,  64),
            S(  0,  34),   S( 25,  88),   S(  8,   3),   S( 11, -22),   S( 11,  48),   S( -5,  -1),   S(  4,  52),   S( -3,  67),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 14 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-20, -90),   S(  0, -76),   S( 17, -61),   S( -3,   1),   S(-13, -45),   S(  3, -30),   S( 26, -62),   S(-27, -60),
            S(-51, -64),   S(-52, -76),   S(-39, -77),   S(-33, -81),   S(-26, -60),   S(  3, -49),   S(-52, -48),   S(-25, -47),
            S(-58, -34),   S(-48, -38),   S(-33, -64),   S(-34, -84),   S(-13, -65),   S( -9, -70),   S(-20, -62),   S(-14, -22),
            S(-15,  -7),   S(-32, -37),   S(-10,   0),   S(-35, -66),   S(  0, -40),   S( -1, -12),   S(  3, -16),   S(-18,  14),
            S(-14, -26),   S( -6, -23),   S(  2,  30),   S(  2,  12),   S(  8,   3),   S(  2,   5),   S( 12,  30),   S(  3,  54),
            S(-26,  26),   S( -4,  -3),   S(  9,  22),   S( 15,  39),   S( 11,  31),   S(-15,   8),   S( 16,  50),   S(  5,  55),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawns: bucket 15 */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(-10, -22),   S(-12, -26),   S(  1, -42),   S( -9, -18),   S(-14, -41),   S(-49, -55),   S(-33, -19),   S( -1, -96),
            S(-34, -30),   S(-16, -61),   S(  3, -50),   S(-10, -70),   S(-22, -47),   S(-39, -18),   S(-55, -18),   S(-44, -70),
            S(-40, -37),   S(-50, -50),   S(-22, -39),   S( -2, -62),   S(-36, -45),   S(-39, -58),   S( 20, -22),   S(-27,  -6),
            S(-11,   2),   S(-71,  -6),   S(-18,  23),   S(-21, -27),   S(  4, -15),   S( -6, -47),   S(  7, -31),   S(  0,  -4),
            S( -5,  21),   S( -8,  22),   S( 17,  74),   S( 16,  58),   S( 34,  86),   S(  2,  15),   S( 17,  24),   S(  9,  -2),
            S(-55, -38),   S(-14, -12),   S( -4,  42),   S(  2,  30),   S(  4,  64),   S( 22,  73),   S( -1,  -8),   S( 30,  50),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* knights: bucket 0 */
            S(-75, -53),   S(-14, -24),   S( -6, -12),   S( 11,  12),   S( -1, -41),   S(-19,  14),   S(-11, -22),   S(-76, -67),
            S(  6, -51),   S(-10,   7),   S( -6, -21),   S(  5,  -5),   S( -7,  26),   S( -1,  22),   S(-12, -62),   S(-29, -17),
            S(-13,  -6),   S( 19,   0),   S(  5,  33),   S( 34,  28),   S( -8,  32),   S( 14,  12),   S(-17,  34),   S( -4, -32),
            S(  1,  32),   S( 15,  79),   S( 35,  59),   S( 46,  28),   S( 26,  47),   S( 38,  20),   S( 32,   6),   S( -8,  29),
            S( 47,  54),   S(  0,  86),   S( 61,  57),   S( 31,  52),   S( 85,  28),   S(  2,  40),   S( 16,  28),   S(  5,  16),
            S( 83,  -2),   S(-14,  71),   S(123,  20),   S( 81,  14),   S( 54,  40),   S(-39,  73),   S( 27,  24),   S(-13,  38),
            S( 54, -11),   S(-44, -34),   S( 45,   4),   S( 70,  95),   S( 40,  65),   S(-15,  62),   S( -2,  46),   S(-62,   4),
            S(-113, -173), S(-14, -26),   S( -9,  -6),   S( 37,  53),   S( 18,  59),   S( 54,  34),   S(-25,  10),   S(-13,  -2),

            /* knights: bucket 1 */
            S( 42, -43),   S(-50,  24),   S(-32,  32),   S(-30,  35),   S(-24,  23),   S(-26,  -9),   S(-31,  20),   S( 23,  12),
            S(-31,  15),   S(-60,  62),   S(-25,  43),   S(-15,  36),   S(-17,  28),   S(  8,  31),   S(-20,  19),   S(-27, -34),
            S(-38,  24),   S(-19,  30),   S(-23,  30),   S(-22,  67),   S(-26,  55),   S( -9,  28),   S(-36,  38),   S(  0,   7),
            S(-28,  77),   S( 11,  54),   S(-10,  76),   S( -9,  75),   S( -9,  70),   S(  3,  63),   S(-11,  30),   S(-14,  45),
            S( 33,  28),   S(  2,  34),   S( 27,  81),   S(  9,  66),   S( 32,  50),   S( 20,  55),   S( -8,  59),   S(-21,  77),
            S( 56,  15),   S( 75, -10),   S( 96,  24),   S( 89,  18),   S( 58,  29),   S(-32,  86),   S( 36,  51),   S( -7,  78),
            S( 12,  11),   S( 31,  22),   S( 49,  -9),   S( 16,  47),   S( 23,   9),   S( 16,  42),   S( -2,  79),   S(-47,  73),
            S(-167, -41),  S( 27,  11),   S(-30, -40),   S(-61,   1),   S(  1,   8),   S( 37,  83),   S( -1,  70),   S(-55,  20),

            /* knights: bucket 2 */
            S(-87,  -1),   S(-51,  51),   S(-39,  36),   S(-38,  44),   S(-32,  32),   S(-54,  18),   S(-46,  32),   S(-36,  -9),
            S(-40, -13),   S(-12,  51),   S(-37,  45),   S(-25,  35),   S(-36,  45),   S(-30,  33),   S(-13,  28),   S(-47,   5),
            S(-52,  65),   S(-34,  58),   S(-40,  62),   S(-47,  88),   S(-42,  77),   S(-39,  43),   S(-39,  41),   S(-22,  34),
            S(-42,  87),   S(-32,  85),   S(-27, 103),   S(-40, 104),   S(-47,  96),   S(-12,  82),   S(-16,  69),   S(-12,  52),
            S(-17,  81),   S(-21,  91),   S(-17,  90),   S( -7,  72),   S(-30,  97),   S( -3,  90),   S(-32,  87),   S( 16,  23),
            S(-58,  89),   S(-54, 101),   S(-50, 112),   S( -3,  64),   S( 53,  45),   S( 99,  35),   S( 40,  61),   S(  6,  33),
            S( -3,  61),   S(-57, 104),   S( 11,  73),   S( 29,  38),   S(-16,  46),   S(-30,  27),   S(-51,  68),   S(-13,  46),
            S(-29,  -1),   S(  9,  76),   S(-28,  96),   S( -1,  24),   S(-35,  10),   S(-61, -19),   S( 18, -12),   S(-163, -23),

            /* knights: bucket 3 */
            S(-67,  67),   S(-29,   3),   S(-26,  35),   S(-15,  32),   S( -9,  24),   S(-20,  12),   S(-31,  -2),   S(-33, -20),
            S(-36,  14),   S(-21,  54),   S(-11,  46),   S(-15,  46),   S(-13,  49),   S(  6,  40),   S(  0,  18),   S( 13, -39),
            S(-25,  41),   S(-18,  54),   S(-14,  74),   S(-13,  89),   S(-12,  85),   S(-17,  68),   S( -8,  59),   S(  0,  12),
            S(-22,  67),   S(  7,  80),   S(  2,  98),   S(-11, 110),   S( -7, 119),   S( 16, 106),   S( 10, 106),   S(  2,  79),
            S(-12,  82),   S( -9,  89),   S(  6,  99),   S( 16, 120),   S(  1, 114),   S(  8, 131),   S(-24, 145),   S( 28, 134),
            S(-39,  96),   S(-22,  96),   S( -4, 108),   S( -9, 121),   S( 24, 126),   S( 88, 110),   S( 27, 141),   S( 23, 138),
            S(-39,  90),   S(-54, 110),   S(-47, 122),   S( -9, 117),   S( 12, 108),   S( 46,  94),   S( 14,   4),   S( 86,  29),
            S(-184,  83),  S(-73, 100),   S(-73, 153),   S( 10, 119),   S( 23, 132),   S(-89, 115),   S(-21, -21),   S(-70, -121),

            /* knights: bucket 4 */
            S( -8,  11),   S(-38,  -1),   S(-62,  25),   S(-27,  -5),   S(-51,  10),   S(-44, -14),   S( 28, -32),   S(-23, -11),
            S( 33,   5),   S(-13,  -6),   S( -9,  -3),   S( -1,  10),   S(-19,  10),   S( 39,  -9),   S(  1,  27),   S(-42, -46),
            S(  5,   5),   S(  1,  33),   S( 81,  20),   S(124, -15),   S( 53,  11),   S( 57, -42),   S(  9,  -9),   S(  2, -13),
            S(-13, -38),   S( 61,  -2),   S( 45,   4),   S( 96,  -1),   S( 89, -12),   S( 17,  22),   S(-28,  31),   S(  0,  14),
            S( -7, -54),   S( 41,   4),   S( 69,  -1),   S( 89,  20),   S(106, -15),   S( 14,  12),   S( 21,   2),   S(-17,  25),
            S(  3, -16),   S( -7, -25),   S( 50, -13),   S( 61,  22),   S(  3,  23),   S( -4,  19),   S( -8,  32),   S(-28,  28),
            S(-12, -34),   S(-14, -65),   S( 20,   8),   S( 11,   4),   S( 11,  39),   S(  7,  36),   S( -1,  44),   S(-14,  11),
            S(  4,  -1),   S( -4, -40),   S( -1,  -6),   S(  8,  -8),   S( 17,  33),   S(  6,  33),   S(-11,   9),   S(-33, -22),

            /* knights: bucket 5 */
            S(  7,  -4),   S(-13,  33),   S(  2,  27),   S(-55,  36),   S(-14,  34),   S( 11,  10),   S(-57,  43),   S(-11,  -2),
            S( 35,  39),   S( 53,  15),   S( 27,   8),   S(  0,  15),   S( 27,  13),   S( 16,  19),   S(-10,  32),   S(-51,  31),
            S(-27,  33),   S(  1,  22),   S( 36,  18),   S( 65,  25),   S( 46,  31),   S( 48,   5),   S( 29,  21),   S(-21,  23),
            S( 45,  26),   S( 51,   1),   S( 90,  -4),   S(123, -13),   S( 99,   0),   S( 86,  17),   S( 56,  17),   S(-10,  20),
            S( 67,   0),   S( 78,   8),   S(109, -20),   S(110, -23),   S( 86,  -9),   S( 49,  17),   S( 40,  10),   S( -5,  24),
            S( 11,   5),   S( 67, -21),   S( 16, -40),   S( -1,  -5),   S( 52,  14),   S( 66,  11),   S(-14,  16),   S( 19,  48),
            S( -1,  14),   S(-18,   0),   S( 12, -14),   S(  9,   3),   S( 13, -18),   S( 30,  36),   S( 25,  53),   S(-13,  41),
            S(-39, -22),   S(-14,   4),   S( 10, -22),   S(-10,   6),   S(  5,  10),   S( 12,  41),   S( 19,  78),   S(  1,  10),

            /* knights: bucket 6 */
            S(-17, -35),   S(-25,  22),   S( 12,   4),   S(-41,  41),   S(-42,  33),   S(-21,  28),   S(-25,  32),   S(-45,  -5),
            S( -1, -13),   S( 31,  26),   S( 12,  13),   S( 18,  14),   S( 17,  19),   S(-18,  41),   S(-19,  53),   S(-35,  66),
            S( 13,   3),   S( 68,   6),   S( 82,   2),   S(100,  13),   S( 91,  13),   S( 14,  26),   S( 32,  43),   S( -5,  44),
            S( 67,   9),   S( 79,   8),   S( 98,   4),   S( 93,   8),   S(132, -22),   S(122,   6),   S( 44,  23),   S( -8,  35),
            S( 13,  28),   S( 64,   9),   S(120,   8),   S(181, -28),   S(106, -24),   S(144, -20),   S(157, -23),   S( 65,  22),
            S(  7,  20),   S( 21,  25),   S( 83,  10),   S( 66,   6),   S( 34,  -1),   S( 85, -20),   S( 18,  -4),   S( 30,   3),
            S(-28,  36),   S( 43,  45),   S( 69,  52),   S( -4,  15),   S( 40,  14),   S( 54,   8),   S( 16,   0),   S( 39,  43),
            S( 26,  46),   S( 15,  34),   S( -8,  34),   S( 10,  45),   S( 10,  13),   S( 15,  14),   S( 13,  51),   S(-17, -43),

            /* knights: bucket 7 */
            S(-37, -84),   S(-76,  -9),   S( 25, -13),   S(-45,  31),   S(-17,   4),   S(-34,   5),   S(-11, -16),   S(-22,  -1),
            S( -8, -47),   S(-23,  -8),   S( 18, -10),   S(-42,  24),   S( 16,  21),   S( -3,  32),   S(  5,  31),   S(-19,  10),
            S(-15, -28),   S(-36,  -7),   S( 29, -23),   S( 63,   6),   S( 66,  11),   S( 58,  13),   S(  6,  40),   S(-24,  56),
            S(-12,  -8),   S( 28, -15),   S( 87, -18),   S(120,  -9),   S(109, -11),   S(132,   6),   S( 45,  21),   S( 88,  27),
            S(-39,  45),   S( 31,   8),   S( 57,   0),   S( 82,  -8),   S(145, -10),   S(195, -50),   S(198, -30),   S(  7,  13),
            S(-26,  33),   S( 46,  18),   S( 21,  17),   S(127,   1),   S(137,  10),   S( 84,   0),   S( 53, -23),   S( 28, -53),
            S(-42,  11),   S( 12,  22),   S( -1,  34),   S( 48,  33),   S(109,  17),   S( 23,  12),   S( -4, -38),   S(-15, -26),
            S(-31, -23),   S(-11,  25),   S( 22,  64),   S( 26,  31),   S(  8,  36),   S( 30,  26),   S( 22, -10),   S(  6,  -2),

            /* knights: bucket 8 */
            S(-14, -20),   S(  7,  -2),   S(  0, -16),   S(-18, -42),   S( -4, -25),   S(-10, -37),   S(  5,   1),   S( -5, -25),
            S(-11, -29),   S(-18, -56),   S(  5, -48),   S( -2, -25),   S(  0, -20),   S( 19, -37),   S(  7, -18),   S(  0,  -7),
            S( -3, -35),   S(  1, -22),   S( 23, -62),   S( 15, -33),   S( 26, -37),   S( 32, -35),   S( -3, -29),   S( -6, -40),
            S(  0, -34),   S( -7, -35),   S( 22,   7),   S( 35, -19),   S( 19, -61),   S(  9, -40),   S( -6, -29),   S(-24, -66),
            S(-11, -46),   S(-12, -84),   S( -2, -43),   S( 22, -37),   S(  7, -34),   S( -4, -48),   S( -1, -12),   S(-10, -55),
            S(  2,   9),   S( 12, -23),   S(  3,   2),   S(  8, -33),   S(  2, -34),   S(  8, -28),   S(  3,   3),   S(-11, -38),
            S( -7,  -7),   S(  0, -49),   S(-11, -11),   S( 23,  24),   S(  9,  -4),   S(-12, -35),   S( -7, -24),   S( -5, -12),
            S( -3,   0),   S( -7, -11),   S( -5, -15),   S( -2, -11),   S( -4, -15),   S( -5,  -8),   S( -1,  -7),   S( -3,   2),

            /* knights: bucket 9 */
            S(-34, -116),  S(-17, -44),   S(  5, -27),   S( -1, -41),   S(-12, -43),   S(-25, -30),   S(-18, -40),   S( -4, -33),
            S( -2,   5),   S(-12, -53),   S(-20, -150),  S( -9, -65),   S( -4, -63),   S(  7, -53),   S(  3, -35),   S(-11, -20),
            S( -7, -28),   S(-18, -47),   S(  3, -61),   S( 18, -81),   S( 21, -41),   S(  3, -34),   S(  9, -31),   S(-18, -38),
            S(-19, -52),   S( -9, -66),   S( -5, -82),   S( 16, -71),   S(  3, -60),   S( 36, -29),   S(-21, -36),   S( -3, -23),
            S( -1,  14),   S(  0, -21),   S(  0, -55),   S(-15, -71),   S( -2, -49),   S( 30, -33),   S( -7, -52),   S(  3,   2),
            S(-14, -41),   S(-13, -40),   S(-18, -88),   S( -3, -33),   S( 21, -28),   S(-12, -28),   S(-16, -51),   S(-16, -54),
            S(  4,   5),   S( -7, -26),   S( -1, -26),   S(-19, -47),   S(  1, -16),   S(  7,   4),   S( -6,  -7),   S( -5,  -3),
            S(  1,   3),   S(  0, -14),   S(  4,  -2),   S( -7, -36),   S(  4,   1),   S(  0,   3),   S(  2, -11),   S( -1, -10),

            /* knights: bucket 10 */
            S( -9, -41),   S(-13, -30),   S(-29, -42),   S(-22, -12),   S( -7, -42),   S( 14, -21),   S(-22,  -9),   S( -4, -21),
            S( -5, -24),   S(  6, -23),   S( -8, -51),   S(-20, -51),   S(-11, -61),   S( -5, -82),   S(  1,  -7),   S(-11,  20),
            S( -4, -46),   S(  7, -48),   S( 20, -70),   S( 22, -50),   S( -2, -62),   S( 24, -62),   S(  0, -43),   S( -3, -10),
            S(-19, -58),   S(  3, -31),   S( 22, -61),   S(  8, -68),   S( 28, -49),   S(  0, -44),   S(  3, -12),   S( 11,  10),
            S(-19, -57),   S(  3, -46),   S( 14, -37),   S(  6, -58),   S( 17, -52),   S( -3, -73),   S(-14, -57),   S(  4, -14),
            S(-12, -33),   S( -6, -23),   S(  1, -38),   S( 20, -41),   S(-10, -57),   S(-10, -42),   S( -2, -44),   S( -7, -32),
            S( -3, -18),   S(-12, -32),   S(  0, -32),   S( 20, -34),   S( -6, -44),   S(-13, -47),   S( -7, -20),   S(-10, -26),
            S(  1,   7),   S(  7,  25),   S( -8, -15),   S( -9, -30),   S(-17, -66),   S( -9, -65),   S(  0,   1),   S( -3,  -7),

            /* knights: bucket 11 */
            S(-10, -57),   S(-34, -59),   S( -2, -28),   S(  0, -17),   S(-34, -16),   S( -2,   8),   S(-10,  -4),   S(  0,  13),
            S(-17, -68),   S(-19, -41),   S( 15, -45),   S( 80, -23),   S( 12,   7),   S( 11, -66),   S(-33, -78),   S(  3, -13),
            S(  0, -31),   S(-11, -75),   S( 18, -88),   S( 54, -33),   S( 13, -29),   S( 45, -51),   S( 20, -61),   S( -4, -34),
            S(-30, -83),   S( -1, -45),   S( 30, -64),   S( 72, -34),   S( 68, -38),   S( 11, -44),   S( -4, -30),   S( -2, -22),
            S(-22, -36),   S( 18, -41),   S( -2, -35),   S( 21, -56),   S( 44,  -6),   S( 17, -22),   S(-11, -86),   S( -2,  -8),
            S(  0, -24),   S(-22, -73),   S(  9, -11),   S( 36, -52),   S( 33, -17),   S(  9, -21),   S(  2, -32),   S( -3,  -7),
            S( -6, -22),   S(-13, -12),   S( -9,  -7),   S( -3,  -4),   S(  9,   1),   S( 36, -20),   S( 15,  -1),   S(-13, -22),
            S(  0, -13),   S( -8, -22),   S(-11, -38),   S(  3,   6),   S( -3, -10),   S(  5, -14),   S(  2,   6),   S( -1,  -7),

            /* knights: bucket 12 */
            S(-16, -57),   S( -7, -31),   S( -9, -49),   S(  5,   7),   S( -4, -10),   S(-10, -14),   S( -1,  -5),   S( -3, -13),
            S( -8, -47),   S( -1,  -8),   S( -8, -21),   S( 14,  32),   S(  1, -10),   S( -4, -25),   S( -7, -28),   S(  3,   6),
            S( -8, -40),   S(-17, -63),   S( -6, -31),   S(-12, -110),  S(-10, -49),   S( 11,  14),   S(  7,  -1),   S( -5,  -4),
            S(  2,  -6),   S( -2, -47),   S( -2, -37),   S(  5, -38),   S( -3, -50),   S( -3, -22),   S(  7,   3),   S(  0,   4),
            S(  3,   2),   S(  5, -32),   S(  4, -43),   S(  2, -23),   S( -4,  -4),   S(  3, -12),   S(-14, -38),   S( -7, -32),
            S( -2, -10),   S( -6, -39),   S(  2,   0),   S(  2, -26),   S(-15, -71),   S( -7, -29),   S(  8,  24),   S( -5, -13),
            S(  1,   1),   S(-12, -35),   S(-12, -19),   S( -4,   0),   S( -5,  -6),   S(-14, -43),   S( -8, -24),   S( -2, -18),
            S(  0,  -4),   S(  0,  21),   S( -1,   1),   S(  1,  -5),   S( -2,  -3),   S(  0,  10),   S( -2, -10),   S(  0,   1),

            /* knights: bucket 13 */
            S(  1,   2),   S( -2,  -9),   S( -1, -30),   S( -9, -40),   S( -1, -13),   S(  0,  -8),   S(-12, -39),   S( -1,  -5),
            S( -2, -31),   S( -1, -22),   S( -1,  -5),   S(-16, -62),   S(-10, -45),   S( -3, -23),   S( 21,  45),   S( -7, -29),
            S( -5, -25),   S( -1,  -6),   S(-14, -42),   S(-11, -58),   S( -2, -18),   S( 18,  29),   S(  3,   5),   S( -5, -16),
            S(  1,  -9),   S( -3, -22),   S(-20, -91),   S( -4, -27),   S( 11, -23),   S( 10,   3),   S(  0,  -9),   S(  3,   6),
            S(  1,  12),   S( -5, -36),   S( -5, -66),   S(  1, -45),   S(-14, -46),   S(  2, -30),   S( -5, -28),   S(-11, -34),
            S( -1,  -2),   S(  1,   8),   S(  0,  19),   S(-14, -75),   S(  0, -15),   S( -7, -64),   S( -4,  -9),   S(  7,  28),
            S(  2,   8),   S(  8,  21),   S( -8, -19),   S( -8, -28),   S( -2,  -2),   S(-18, -41),   S(  4,  -1),   S(  0,  -3),
            S(  1,   0),   S(  2,  18),   S(  0,   0),   S( -3,   0),   S( -4,  -6),   S( -1,  -6),   S( -1,  -1),   S(  0,   2),

            /* knights: bucket 14 */
            S(  1,   4),   S( -5, -20),   S( -1, -10),   S( -2, -14),   S( -5, -30),   S(  1,   8),   S(  5,  -1),   S(  1,   1),
            S( -5, -30),   S( -5, -19),   S(  4,  -3),   S( -8, -61),   S( -1, -12),   S(  1,  -8),   S( -8, -37),   S(  5,  41),
            S( -2, -13),   S( -2, -45),   S(  7,  -6),   S( -4, -53),   S(  2,  -2),   S(  7, -17),   S( -5,  -4),   S( -4, -20),
            S( -6, -23),   S( -4, -33),   S(-12, -56),   S( -5,  -8),   S( 10, -15),   S(  0, -35),   S( -1, -15),   S( -6,  14),
            S( -2,   2),   S(-10, -35),   S( -1, -34),   S( -1,   2),   S( -1,  22),   S( -7, -42),   S(-11, -30),   S(  1,  -1),
            S( -1,  -5),   S(  1,  14),   S(  1,  49),   S( -6, -47),   S(-13, -49),   S( -2, -23),   S(  3,  16),   S( -2,  -6),
            S(  0,  -2),   S( -1,   1),   S(  3,  -3),   S( -2,  36),   S(  3,  43),   S( -8, -13),   S(  5,  19),   S( -4, -14),
            S( -1,  -2),   S(  1,  10),   S(  0,  -3),   S(  2,   3),   S(  0,   3),   S(  5,   8),   S( -1,   1),   S(  1,   6),

            /* knights: bucket 15 */
            S( -3, -19),   S( -2,   2),   S( -2,  -4),   S( -7, -23),   S( -9, -40),   S( -2, -25),   S(  0, -37),   S( -4, -17),
            S(  1,   7),   S(  0,  -3),   S(  2, -27),   S( 19,  11),   S( -9,  -3),   S(-13, -66),   S( -3,  -9),   S(  0, -15),
            S(  2,   1),   S( -7, -19),   S( -5, -19),   S( 20, -13),   S(-10, -79),   S( -3, -14),   S(  3, -30),   S(  0,  -4),
            S( -4, -13),   S(  2,   1),   S(  2,  -6),   S(  4, -16),   S( -9, -46),   S( -9, -65),   S( -4, -48),   S(  1,  -8),
            S(  4,   3),   S(  1,   0),   S(  0, -37),   S( -5,  -5),   S(  3,  -4),   S(  1, -13),   S( 15,   7),   S(  7,  17),
            S(  0,  13),   S( -8, -24),   S( -5, -12),   S( -2,  -2),   S(  1,  -2),   S( -5,  -1),   S( -5,  -2),   S(  1,  14),
            S( -5, -18),   S( -3, -11),   S(  0,   3),   S(  0,   5),   S(  4,  48),   S(  9,  36),   S(  5,  23),   S(  5,  13),
            S(  0,   0),   S( -1,   2),   S(  1,   5),   S( -3,  -6),   S(  1,  10),   S( -2,   1),   S(  2,  12),   S(  1,   4),

            /* bishops: bucket 0 */
            S( 47, -21),   S( 17,  32),   S( -5,  13),   S(-19,  -5),   S( -3,   4),   S( -5,  15),   S(104, -66),   S( 39, -15),
            S(  8, -39),   S(  6,  -2),   S(  4,  14),   S( 19,  13),   S( 15,  10),   S( 77, -24),   S( 50,  15),   S( 59, -33),
            S( 38, -50),   S( 24,  -5),   S( 38,  -1),   S( 28,   6),   S( 59,  -7),   S( 50,  33),   S( 63,  12),   S( 23, -14),
            S( 28, -54),   S( 47, -21),   S( 34,   5),   S( 68,  -8),   S( 83,  38),   S( 71,  37),   S( 30,   1),   S( 28, -15),
            S( 51,  -1),   S( 44, -15),   S( 93, -14),   S(101,  -1),   S(144, -21),   S( 49,  21),   S( 59,   1),   S( 12,  10),
            S( 27,  64),   S(125, -16),   S(114,  10),   S( 76, -16),   S( 61,  -6),   S( 29,  27),   S( 42,  24),   S(  7,  17),
            S(-79, -116),  S( 61,  36),   S( 93,  47),   S( 41,  -8),   S( 38,   0),   S( 23,  24),   S( 24,  30),   S( -1,  24),
            S( -1, -21),   S( 12,  -9),   S( 25,  24),   S( 27,   5),   S(  3,  17),   S( -2,  20),   S(-19,  -5),   S( 10, -29),

            /* bishops: bucket 1 */
            S(-40,  -3),   S( 33, -31),   S(-26,  33),   S( 12,  -2),   S( -8,  20),   S(  4,   1),   S( 25,   0),   S( 37, -35),
            S(-20, -23),   S( -4,   0),   S( 16,  -2),   S( -5,  13),   S( 40, -16),   S( 28, -12),   S( 62, -19),   S( 15,  -4),
            S(-24,  21),   S( 29, -20),   S(  9,   4),   S( 26,   6),   S(  6,  14),   S( 55, -22),   S( 24,  -3),   S( 53,  -9),
            S( 36, -15),   S( 32,   2),   S( 34,  -2),   S( 27,   6),   S( 60, -10),   S( 23,   9),   S( 77, -17),   S(  0,  25),
            S( 42,  -8),   S( 70,  -7),   S( 21,   6),   S( 97, -31),   S( 59,  -9),   S(108, -33),   S( 35,  13),   S( 13,  13),
            S( 33, -22),   S( 38,   4),   S( 74,   0),   S( 75, -12),   S(112, -34),   S( 38,  -2),   S( -9,  25),   S(-10,  -2),
            S( 28, -71),   S(  0, -27),   S( 17, -10),   S( 42, -19),   S( -4,  27),   S( -1,   4),   S(  2,  -6),   S(-67,  25),
            S( 10, -56),   S(-28,  -6),   S( -2,  -5),   S(-62,  40),   S(  4,   6),   S( 34,   2),   S( 17,  -8),   S(-34, -17),

            /* bishops: bucket 2 */
            S(  0,  -6),   S(-11,   3),   S(-14,  21),   S(-24,  31),   S( -2,  25),   S(-32,  26),   S( 13, -13),   S( -9, -16),
            S(  5,  -1),   S(  3,   6),   S( -2,   8),   S(  7,  18),   S(-15,  25),   S( 14,   9),   S( -3,  11),   S( 16, -57),
            S( 13,  32),   S( 11,  16),   S( 13,  21),   S( -9,  20),   S( -8,  36),   S( -6,   1),   S( -3,  -1),   S(-22,   8),
            S(  0,  23),   S( 32,  23),   S(  5,  33),   S( 29,  27),   S( -3,  19),   S(-22,  39),   S(-37,  35),   S(-10,  32),
            S(-14,  32),   S( 19,  17),   S( 48,  15),   S( 38,  13),   S( 18,  22),   S( 25,   6),   S( 10,  39),   S( 21,   8),
            S(-13,  27),   S(-17,  27),   S(  7,   5),   S( 64,  -3),   S( 81,  -9),   S( 70,  33),   S( 63,   2),   S( 31, -40),
            S(-17,  20),   S(  1,   4),   S(-25,  33),   S( -5,  14),   S(-78,  10),   S(-31,  -5),   S(-43,  33),   S( -8, -27),
            S(-64, -20),   S( -7,  16),   S( 35,   4),   S(-26,  37),   S(-14,  -1),   S(-59,  -7),   S(-33,  14),   S(-76, -14),

            /* bishops: bucket 3 */
            S( 32,   5),   S( 52, -10),   S(  0,  17),   S(  8,  31),   S( 11,  37),   S(-13,  54),   S( -9,  85),   S(-10,  16),
            S( 31,  18),   S( 16,  36),   S( 28,  26),   S( 16,  36),   S( 20,  38),   S( 23,  42),   S(  9,  36),   S( 31,  -8),
            S(-12,  43),   S( 30,  61),   S( 28,  63),   S( 27,  55),   S( 14,  71),   S( 18,  37),   S( 11,  39),   S( -6,  48),
            S( -8,  38),   S( 16,  57),   S( 33,  72),   S( 44,  69),   S( 31,  53),   S( 14,  46),   S( 22,  14),   S( 27, -12),
            S(  6,  42),   S( 15,  54),   S(  7,  68),   S( 61,  69),   S( 46,  72),   S( 53,  51),   S( 23,  47),   S(-10,  55),
            S( 27,  47),   S( 11,  78),   S( -2,  57),   S( 16,  61),   S( 52,  58),   S( 42,  98),   S( 56,  61),   S( 21, 120),
            S(-17,  77),   S( 19,  55),   S( 11,  50),   S( -5,  62),   S(-28,  85),   S( 35,  81),   S(-52,  73),   S(  0, -34),
            S(-49,  41),   S(-35,  66),   S(-83,  80),   S(-58,  85),   S( -9,  66),   S(-161, 124),  S( 18,  43),   S(-22,  16),

            /* bishops: bucket 4 */
            S(-69,   9),   S(-10,  -5),   S(-55,   2),   S(-24,  -3),   S(-39,  12),   S( -9, -13),   S(-40, -45),   S(-26, -43),
            S(-58,  -4),   S( -3, -21),   S( 17,  -8),   S(-19,  -5),   S(-35,   0),   S( -7, -13),   S(  7, -13),   S(-36, -54),
            S( 29,  20),   S( 24,  -9),   S( 38, -12),   S(  0, -10),   S( 53, -15),   S(-41,   8),   S(  3,  -8),   S(-57, -18),
            S( 13, -20),   S( 49, -17),   S( 51, -18),   S( 60, -20),   S( 36, -13),   S( 62, -37),   S(-36,   0),   S( 20,   7),
            S(  5, -25),   S(  7, -52),   S( 72, -33),   S( 69, -52),   S( 70, -18),   S( 20, -10),   S(-14,  11),   S( -9,  11),
            S(-64, -96),   S(-22, -35),   S( 79, -28),   S( 26,  -9),   S(  4,  -8),   S( 35,  -4),   S(  1,   4),   S( -5,  30),
            S( -2,   5),   S(-18, -42),   S( 11, -24),   S(-11, -29),   S(  2,  -4),   S(  2,  -3),   S(-38,  22),   S( -4,  18),
            S(-12, -48),   S( -2, -10),   S( -8, -27),   S( 22,   1),   S( 33,  -2),   S( 13,  15),   S(-10,  56),   S(  3,   0),

            /* bishops: bucket 5 */
            S(-78,  12),   S(  2, -15),   S(-82,  25),   S(-40,   4),   S(-23,  -1),   S(-41,  10),   S(-26,  -4),   S(-49, -16),
            S(-55,  -5),   S(-37,  10),   S( 50, -29),   S(-28,   1),   S(-34,   1),   S(-47,   7),   S(-11,  -7),   S(  6, -13),
            S(  1,   4),   S(-46,   9),   S( 48, -21),   S(  8,  -3),   S( 32,  -2),   S(-38,   9),   S(-32,   0),   S(-50,   1),
            S(  6,  14),   S( 17,   4),   S( 66, -26),   S(104, -38),   S( 52, -24),   S( 38, -18),   S(-25, -13),   S(-14,   7),
            S( 32, -25),   S( 45, -25),   S( 36, -43),   S( 50, -47),   S( 56, -39),   S( 82, -45),   S( 53, -23),   S(-52, -15),
            S( -5, -17),   S( 19, -28),   S( 19, -28),   S(-35, -15),   S( 23, -27),   S( 50, -25),   S(-11,  -6),   S(-44,  18),
            S(-40, -15),   S(-20, -25),   S(-19, -12),   S(  0,   3),   S(  7,  -7),   S( 12,  -4),   S( -4,   7),   S(-32,  -6),
            S(-32,  -4),   S(-16, -21),   S(-11, -18),   S(-11,  -9),   S(-35,  32),   S(  3,  23),   S(-27,  -2),   S(-25,   8),

            /* bishops: bucket 6 */
            S(-35, -10),   S(-25,  -1),   S(-29,   8),   S(-13,   5),   S(-55,  12),   S(-30,  -3),   S(-77,   6),   S(-86,   8),
            S(  1,  -9),   S(-19, -10),   S(-28,  11),   S(-25,   5),   S(-39,   5),   S(-36,   1),   S(-27,  10),   S(-67,   3),
            S(  8, -10),   S(-23,   9),   S( 41, -15),   S( 18,   2),   S( 40,  -7),   S( 17, -12),   S(-39,   1),   S(-11,   3),
            S( -5,   6),   S( 31, -22),   S( 37, -12),   S(109, -29),   S(132, -39),   S( 55, -18),   S(  6,  -9),   S(-19,  17),
            S(-14,   0),   S(  8, -12),   S( 56, -26),   S(132, -49),   S( 30, -40),   S(-11, -36),   S( 30, -16),   S( 17,  -8),
            S(-36,  22),   S( 49, -23),   S( 17, -17),   S( 23, -18),   S(-33,   4),   S( 26, -34),   S( 13, -19),   S( 14, -30),
            S(-40,   6),   S(-31,  16),   S( 30,  -7),   S(-23,  -3),   S(-46,   8),   S(-31, -11),   S( 16, -29),   S(-29,   0),
            S(-65,   8),   S(-31,   3),   S( -4,   9),   S(-16,  21),   S(-38,  19),   S( 27, -36),   S(-13, -22),   S(  6, -27),

            /* bishops: bucket 7 */
            S( 10, -45),   S(-54, -14),   S(-22, -13),   S( 21,  -2),   S(-34,   1),   S(-39, -10),   S(-29,  -8),   S( -8, -37),
            S( 18, -59),   S( 25, -45),   S(  5, -15),   S( -5, -13),   S(-22,   4),   S( -8, -11),   S(-37, -13),   S(-71,  28),
            S( -7, -26),   S( -5,  -9),   S( 19, -14),   S( 57, -19),   S( 43, -15),   S( 22, -22),   S(-51,  18),   S(-77,  43),
            S(-10, -20),   S(-20, -11),   S( 30, -22),   S(102, -43),   S(111, -27),   S( 53, -20),   S( 66, -36),   S(-37,   1),
            S(  9,  -7),   S( 10, -14),   S( 44, -27),   S( 55, -31),   S(122, -43),   S(101, -28),   S(-75,   6),   S(-46, -18),
            S(-28,  15),   S( 21,   3),   S(  6, -18),   S(-38,  -2),   S( 13,  -9),   S( 52, -11),   S( 78, -24),   S(-99, -100),
            S(-29,  -2),   S(-37,  11),   S(-22,   0),   S(-15,  18),   S( 18, -12),   S( 39, -27),   S( 30, -22),   S( 17, -18),
            S(-48, -22),   S(-29,  -4),   S(-22,  14),   S(-34,  17),   S( -6,  16),   S( 11,  -7),   S( 24, -21),   S(  2, -18),

            /* bishops: bucket 8 */
            S(  9,  90),   S(-27,  13),   S( 18, -35),   S(-42,  51),   S( 13,  35),   S(  3, -13),   S(-25, -54),   S(-16, -45),
            S( -3,  -5),   S( -5,  32),   S( -3,  14),   S( 27,   9),   S( -4, -24),   S(  5,  -6),   S(-30, -64),   S( -8,  -5),
            S(  4,   8),   S( -9, -25),   S( 14,  56),   S( 30, -17),   S( 31,  -8),   S(  5,  -3),   S(-22, -41),   S(-32, -41),
            S( -4,  -2),   S( 16,  59),   S( 11,  25),   S( -2,   6),   S( 11, -10),   S( -4,  -3),   S(-19, -10),   S(  5,  10),
            S( -6,  44),   S(  4,  67),   S( 13,  33),   S(-17,  -9),   S( -5,  15),   S(-14,  -1),   S(  5, -39),   S( -8,   7),
            S(-24, -27),   S( -7,   4),   S( 14,  20),   S( 10,  38),   S( -6,  21),   S( -4,  29),   S(  6,  26),   S(-14,   1),
            S( -7,  11),   S(-13, -48),   S( 30,  53),   S( 10,  56),   S( -2,  23),   S(  4,  35),   S( -7,  60),   S(-15,  18),
            S(  1,  10),   S( -7,  13),   S( -1,  -2),   S(  2,  52),   S(  9,  62),   S(  4,  58),   S(-10,  37),   S( 19, 116),

            /* bishops: bucket 9 */
            S( -4,  26),   S(-41,  24),   S(-50, -16),   S(-63, -22),   S(-33, -21),   S( -3, -22),   S(-26, -28),   S(-15, -36),
            S(-14,   2),   S(-12, -12),   S(-18, -14),   S(-31, -40),   S(-19, -31),   S( -5, -36),   S(-19, -30),   S(-22, -68),
            S( -2, -42),   S( -6, -21),   S( -3, -23),   S(  2,   3),   S( -1, -14),   S( 37, -49),   S(-18, -30),   S(-13, -29),
            S(-36,  20),   S(-34, -15),   S( 18,  19),   S( 19,   4),   S( -6, -18),   S( 12, -41),   S( -6,  -9),   S( 19,   8),
            S(-17,  31),   S(-23,  11),   S( 19,  -3),   S( -6, -15),   S(-20, -14),   S(-16, -16),   S(  5, -16),   S( -4, -14),
            S( -3,  20),   S(-30,   1),   S(-27,  -3),   S(  0,  -7),   S(-24,   4),   S( -5,   3),   S(-12,  13),   S(  2,  11),
            S(-17,  22),   S(-18, -29),   S( -9, -14),   S(-33,  -9),   S(-10,  -8),   S( -7,  22),   S( -3,  18),   S(-14,  49),
            S( -9,  27),   S(-23, -24),   S( -8, -13),   S( -2,  16),   S(-15,  -4),   S(-16,   8),   S(-17,  45),   S( 11,  39),

            /* bishops: bucket 10 */
            S(-29, -34),   S(-14,  -7),   S(-20, -27),   S(-55, -25),   S(-81, -37),   S(-57, -67),   S(-28,  -5),   S(  2,  39),
            S( -9, -10),   S(-12, -35),   S(-22, -26),   S(-33, -33),   S(-45, -60),   S(-39, -44),   S(-15, -39),   S(-10,  33),
            S(-11, -38),   S(-22, -61),   S(-21, -56),   S(  5, -39),   S( -6, -41),   S( -3, -33),   S(-32, -30),   S( -4, -38),
            S(-20, -48),   S(-24, -49),   S(-32, -40),   S(  2, -45),   S( -3, -25),   S( 20,  -8),   S(  8,   5),   S(-37, -16),
            S(-23, -15),   S(-53, -26),   S(  6, -29),   S(-14, -28),   S( -1, -21),   S(-17,   3),   S(-22, -16),   S(-10,  -7),
            S(-26,   8),   S(-14, -12),   S(-34,  -6),   S(-15,   0),   S(-46, -34),   S(-17, -26),   S(-10,  -3),   S(-23,   2),
            S(-10,  27),   S(-21,  13),   S(-21,   3),   S( -6,   4),   S( -4, -22),   S(-21, -37),   S(-19, -49),   S(-12,   9),
            S( -2,  34),   S(-13,  31),   S(-19,  26),   S(-19,   2),   S(-26, -24),   S(-26, -33),   S(-10, -36),   S( -5,   3),

            /* bishops: bucket 11 */
            S(-14, -39),   S(-28, -70),   S(-30,  -4),   S(  3,  26),   S(-30,   4),   S( 26, -28),   S(-20, -32),   S(-37,  38),
            S( -1,   5),   S( 13, -52),   S( -7, -27),   S( 17, -21),   S(-34,   2),   S(-48, -14),   S(-63, -18),   S( -5,  30),
            S( -5, -48),   S( -1, -19),   S(  4, -43),   S(  2, -21),   S(-20, -20),   S(  4,  33),   S( -2, -28),   S(  9,  12),
            S( 18,   9),   S(-22, -52),   S( -6, -11),   S(  6, -52),   S(  5,  -8),   S(  6,  28),   S( 27,  61),   S( -6, -10),
            S(-11,  34),   S(-19,   6),   S(-22, -10),   S(-38,  18),   S(  8, -22),   S( -4,  17),   S(-15,  24),   S(  5,  65),
            S(-25,  23),   S(-31,   7),   S(-33,  29),   S(  0,  17),   S(-38,  24),   S(-30,  -1),   S( -1,  -7),   S(-14, -11),
            S(-27, -16),   S(-14,  71),   S( 12,  42),   S(-12,  40),   S(-18,  20),   S( -9,  11),   S(-21, -62),   S(  6,  27),
            S(  1, 103),   S(-35,  24),   S( -1,  43),   S( -2,  30),   S( 12,  49),   S(  1,  12),   S(-20,  -9),   S(  7,  23),

            /* bishops: bucket 12 */
            S( -8, -21),   S( -4, -32),   S(  6,  15),   S( -4,  29),   S( -1,   9),   S( -1,  -1),   S(  2,   9),   S( -4, -23),
            S( -2, -10),   S( 10,  35),   S( -6, -24),   S( 12,  20),   S( -4,  -8),   S(  3,   4),   S( -7,  -9),   S( -7, -30),
            S(  5,  34),   S( 16,  69),   S( 20,  64),   S( 10,  -9),   S(  9, -10),   S( 20,   5),   S(  5, -14),   S( -3,  14),
            S(  2,  39),   S( 17,  83),   S(  5,  24),   S(  4,  32),   S( 15, -18),   S( 11,   9),   S( -1,   2),   S( -5, -15),
            S( 13,  39),   S( 14,  46),   S( -1,   7),   S(  8,  25),   S( 20,  35),   S( 22,  61),   S(  4,  -6),   S(  5,   3),
            S(  0,   5),   S( -1,   7),   S( -3,  15),   S( 15,  46),   S( 23,  88),   S( 16,  48),   S( -5,  -5),   S(  3,   9),
            S( -2,  -4),   S( -2,  -6),   S( -2, -10),   S(  9,   9),   S( 14,  38),   S( 10,  95),   S( 17,  41),   S( -6,  10),
            S(  0,   8),   S(  0,   5),   S(  2,   8),   S(  0,   4),   S(  2,  13),   S( -2,  14),   S( 13,  97),   S(  9,  58),

            /* bishops: bucket 13 */
            S( -3,  -1),   S( -7,   7),   S(-15, -38),   S( -2,  25),   S(  6,  32),   S(  5,  -1),   S(-13, -36),   S(  0, -13),
            S(-12,  -4),   S(-12, -25),   S( -1,   4),   S(  7,  77),   S(-11,  12),   S(  2,   2),   S(-12, -50),   S( -3, -15),
            S(  9,  51),   S( 14,  91),   S( 11,  13),   S( 20,  23),   S(  0,  -7),   S( 30,  41),   S(  9,  21),   S( -6, -23),
            S( 10, 118),   S( 14,  94),   S( -2,  38),   S(-16, -31),   S( -2,   6),   S(  8,  33),   S(  7,  35),   S(  1,  25),
            S(  8,  85),   S( -7,   8),   S( -5, -26),   S( -5,  -9),   S( -2, -26),   S(  8,  21),   S(  4,  16),   S( -2,  30),
            S(  1,  51),   S(-10,   2),   S( -7,   1),   S( -5,  -9),   S(-10,  41),   S(  0,  -4),   S( -7,   4),   S( 13,  54),
            S(  4,  23),   S( -2,   1),   S( -3,   0),   S(  4,  19),   S(  4,  37),   S( 17,  55),   S(  5,  14),   S( 10,  86),
            S(  3,  21),   S( -4,   0),   S( -7, -17),   S(  3,  27),   S(  0,  19),   S( -4,  -4),   S(  8,  67),   S( 12,  60),

            /* bishops: bucket 14 */
            S( -8, -27),   S( -6,  -5),   S(  6,  14),   S(  0,  41),   S(-14, -50),   S( -3, -11),   S(  2,  20),   S(-10, -23),
            S( -9, -21),   S(-13, -36),   S( -4,  13),   S( -5,  11),   S(  8,  48),   S(  5,  14),   S(  0,  13),   S(  5,  53),
            S(-10, -24),   S(-15,  -5),   S( -8,   7),   S(  6,  27),   S( 18,  45),   S(  9,  61),   S( -1,  62),   S(  2,  68),
            S(  1,  40),   S(  4,  16),   S(  0,  19),   S( -7,   1),   S( -8,  -9),   S(  2,  41),   S( 14,  95),   S(  7,  86),
            S( -5,  25),   S(  2,  21),   S( -4,  16),   S( -6,   3),   S(-16, -35),   S(  1,  34),   S( 10,  24),   S(  7,  87),
            S(  8,  58),   S( 13,  41),   S(  2,  20),   S( 13,   4),   S(  2,  50),   S(  3,   2),   S( -3, -19),   S(  4,  32),
            S( 11,  74),   S(  5,  25),   S( -5,  13),   S(  6,  34),   S(  2,   7),   S(  9,  29),   S( -9, -31),   S(  4,  16),
            S(  9,  55),   S( 16,  61),   S( -3,   4),   S(  3,  20),   S( -4, -11),   S(  0,  10),   S(  6,  11),   S(  0,   5),

            /* bishops: bucket 15 */
            S( -5, -23),   S( -8, -16),   S(-14, -46),   S(-16, -34),   S( -8, -14),   S(  4, -25),   S( -2, -12),   S( -2,  -6),
            S(  8,  30),   S(  5,  -6),   S(  7,   7),   S( -4,  10),   S( 16,   7),   S(-10, -11),   S(  2,   9),   S( -4, -14),
            S( -4, -15),   S(  0,   4),   S(  7,  16),   S(  9,  -2),   S( 10,  25),   S( 17,  34),   S( 17,  46),   S( 10,  74),
            S(  0, -17),   S( -2,   7),   S( 11,  40),   S(-11, -24),   S(  3,  32),   S( 12,  23),   S( 13,  69),   S(  3,  49),
            S( -6,  -6),   S( -2,  15),   S( -9,  19),   S( 10,  34),   S(-12, -17),   S( 27,  53),   S(  1,  26),   S(  1,  17),
            S( -7,  -7),   S( 13,  50),   S(  5,  47),   S(  5,  14),   S( 20,  63),   S(-12,   5),   S( -5, -15),   S( -2,   6),
            S(  8,  43),   S( 10,  53),   S( -5,  47),   S(  8,  42),   S(  4,  29),   S( -1,   3),   S( -5,   0),   S(  2,  17),
            S(  5,  44),   S(  8,  59),   S(  6,  43),   S( 13,  41),   S(  2,   9),   S( -1,  10),   S(  6,  22),   S(  9,  17),

            /* rooks: bucket 0 */
            S( -5,  31),   S( 24,  25),   S(  7,  19),   S(  5,  27),   S(-14,  72),   S( -4,  60),   S(-27,  73),   S(-36,  64),
            S( 11,  18),   S( 20,  30),   S(-13,  32),   S( -4,  46),   S(-11,  70),   S( -4,  66),   S(  3,  39),   S(-33,  79),
            S( 15,  20),   S( 14,  14),   S(-13,  40),   S(  4,  22),   S(-17,  50),   S( -7,  46),   S( -6,  55),   S( -2,  52),
            S(-20,  40),   S( 18,  24),   S(-38,  66),   S( -3,  41),   S( 16,  56),   S(-17,  77),   S(-32,  78),   S(-19,  44),
            S( 34, -13),   S( 30,  46),   S(  8,  50),   S(  7,  54),   S( 33,  47),   S( 15,  82),   S( 43,  51),   S(  1,  85),
            S( 60,   3),   S( 33,  49),   S( 88,  32),   S( 72,  64),   S( 26,  79),   S( 44,  72),   S(  8,  96),   S(-35, 111),
            S( 46,   5),   S( 88,  75),   S( 69,  50),   S( 95,  25),   S( 61,  43),   S( 39,  67),   S( 37,  69),   S(-16,  75),
            S( 10,  30),   S( 32,  41),   S( 36,  43),   S( 47,  26),   S( 60,  49),   S( 83,  47),   S( 53,  55),   S(108, -25),

            /* rooks: bucket 1 */
            S(-60,  74),   S(-30,  42),   S(-19,  39),   S(-52,  66),   S(-52,  70),   S(-49,  66),   S(-53,  87),   S(-78, 100),
            S(-72,  78),   S(-35,  37),   S(-38,  48),   S(-38,  51),   S(-43,  52),   S(-50,  56),   S(-35,  57),   S(-41,  70),
            S(-47,  46),   S(-26,  33),   S(-19,  23),   S(-28,  37),   S(-57,  60),   S(-59,  59),   S(-73,  76),   S(-42,  79),
            S(-80,  69),   S(-16,  27),   S(-16,  35),   S(-58,  67),   S(-68,  66),   S(-79,  85),   S(-55,  86),   S(-66, 104),
            S(-43,  67),   S(-31,  47),   S( -2,  40),   S( -1,  51),   S(-20,  60),   S(-52,  85),   S(-47, 102),   S(-27, 105),
            S( 25,  60),   S( 77,  23),   S( 61,  33),   S( 65,  43),   S( 12,  51),   S( 15,  66),   S( 33,  73),   S( 13,  93),
            S( 36,  49),   S( 24,  43),   S(  9,  35),   S( 19,  52),   S( 69,  28),   S( 12,  49),   S( 29,  78),   S( 51,  87),
            S( 41,  37),   S( 40,  25),   S( 15,   6),   S( -7,  18),   S( 47,  21),   S( 49,  40),   S( 51,  65),   S( 40,  84),

            /* rooks: bucket 2 */
            S(-80, 123),   S(-62, 104),   S(-60,  94),   S(-57,  68),   S(-43,  72),   S(-50,  66),   S(-46,  68),   S(-85, 103),
            S(-66, 108),   S(-65, 106),   S(-76, 100),   S(-67,  83),   S(-70,  87),   S(-60,  62),   S(-28,  54),   S(-62,  82),
            S(-64, 105),   S(-50,  99),   S(-61,  82),   S(-60,  76),   S(-52,  71),   S(-44,  65),   S(-33,  55),   S(-40,  64),
            S(-68, 119),   S(-63, 103),   S(-75,  97),   S(-85,  86),   S(-58,  76),   S(-73,  90),   S(-35,  73),   S(-46,  89),
            S(-35, 121),   S(-52, 120),   S(-53, 111),   S(-51,  83),   S(-56, 104),   S( -6,  79),   S(-17,  85),   S(-42, 113),
            S( -1, 127),   S(  0, 114),   S( 11, 100),   S(-41, 106),   S( 30,  70),   S( 31,  81),   S( 81,  73),   S( 43,  94),
            S( 37,  98),   S( 11, 103),   S( 56,  58),   S( 53,  45),   S(  6,  57),   S( 53,  91),   S(-52, 127),   S( 21, 100),
            S( 75,  67),   S( 23, 100),   S( 32,  81),   S(-33,  85),   S(-53,  66),   S( 26,  54),   S( 28,  82),   S( 25,  99),

            /* rooks: bucket 3 */
            S(-15, 131),   S( -6, 130),   S(-11, 144),   S( -8, 123),   S(  0, 100),   S(  7,  97),   S( 26,  77),   S( -6,  66),
            S( -9, 132),   S(-14, 146),   S(-18, 152),   S( -5, 140),   S( -9, 113),   S( 19,  76),   S( 44,  72),   S( 20,  78),
            S(  8, 117),   S( -5, 144),   S(-11, 139),   S(-10, 130),   S(  6,  94),   S( -3,  99),   S( 34,  74),   S( 25,  71),
            S(-12, 151),   S(-10, 146),   S(-11, 152),   S(-20, 138),   S(-11, 109),   S(-17, 124),   S( 38,  96),   S(  9,  98),
            S(-15, 162),   S(-17, 177),   S(  0, 163),   S( -3, 146),   S(  0, 138),   S(  8, 134),   S( 39, 127),   S( 25, 116),
            S( -1, 171),   S(  8, 168),   S( 18, 173),   S( 10, 164),   S( 65, 118),   S( 93, 111),   S( 72, 135),   S( 45, 122),
            S( 19, 154),   S( 15, 158),   S( 15, 162),   S( 27, 152),   S( 31, 133),   S(114,  95),   S(180, 117),   S(173, 117),
            S(121,  55),   S( 80, 111),   S( 28, 159),   S( 35, 139),   S( 29, 140),   S( 30, 141),   S( 97, 105),   S(150,  83),

            /* rooks: bucket 4 */
            S(-33, -14),   S( 39, -11),   S( 11, -15),   S(  6,  -4),   S(-32,   4),   S(-18,  27),   S(-36,  28),   S(-73,  34),
            S(-24, -13),   S(-63,  21),   S(-15, -16),   S( 15, -35),   S(-30,  -7),   S(-47,  22),   S(-52,  41),   S(-28,  16),
            S(-13,  -5),   S(-42, -10),   S(-32,  -9),   S(-37, -16),   S(-46,   5),   S(-90,  41),   S(-69,  40),   S(-56,   6),
            S( -7, -37),   S( 10,   8),   S( 12, -21),   S( 36, -19),   S( 26,  -4),   S(-12,  15),   S(-41,  13),   S(-42,  22),
            S(  1, -24),   S( 32, -21),   S( 16,   4),   S( 51, -18),   S( 57, -21),   S( 40,  27),   S( 35,  14),   S( -2,  32),
            S(-12, -32),   S( 13,  -4),   S(  9, -16),   S( 37,  15),   S( 16,  15),   S( 29,  33),   S( 44,  34),   S( 23,  37),
            S( 19, -14),   S( 17,  21),   S( 81, -32),   S( 56, -22),   S( 59, -21),   S( 11,   9),   S( 47,   5),   S(  3,  19),
            S( 31,  -7),   S( 51,  10),   S( 77, -21),   S( 34, -17),   S( 59,  -8),   S(  4,  14),   S( 15,  24),   S( 30,   8),

            /* rooks: bucket 5 */
            S(-34,  42),   S( 19,   7),   S(-20,  32),   S( 27,  16),   S(-28,  45),   S(  4,  25),   S(-11,  45),   S(-59,  67),
            S(-32,  16),   S(-26,  12),   S(  3, -12),   S( 15,   2),   S( -8,   6),   S(-31,   6),   S(-39,  42),   S(  4,  40),
            S(-61,  17),   S(-13,   4),   S(-59,   6),   S(-51,  16),   S(-57,  10),   S( -8,  -1),   S(-27,   9),   S(-53,  39),
            S(-40,  31),   S(-25,  29),   S( -5,  -2),   S( 31,   1),   S(-13,  16),   S(  3,  25),   S(-19,  44),   S(-30,  58),
            S( 67,   9),   S( 34,  25),   S( 16,  28),   S( 12,  -4),   S(  9,  15),   S(101,   7),   S( 92,  27),   S( 53,  39),
            S( 29,  32),   S( 13,  23),   S( 13,  19),   S(  7,  -9),   S( 20,  40),   S( 41,  23),   S( 71,  25),   S( 80,  37),
            S( 64,  -5),   S( 56,   0),   S( 14,   5),   S( 45,  28),   S( 52,   8),   S( 61,   1),   S( 28,  28),   S( 12,  36),
            S( 47,  16),   S( 21,  29),   S( 73,   4),   S( 46,  27),   S( 46,  12),   S( 66,   4),   S(-10,  51),   S( 25,  57),

            /* rooks: bucket 6 */
            S(-48,  64),   S(-44,  49),   S(-54,  48),   S(-42,  36),   S(-24,  32),   S(  9,   8),   S(-23,  38),   S(-50,  49),
            S(-45,  47),   S(  7,  14),   S(-47,  30),   S(-26,  26),   S(-24,  16),   S(-39,  19),   S(-36,  22),   S(-10,  15),
            S(-88,  47),   S(-29,  21),   S(-46,   8),   S(-56,  25),   S(-58,  21),   S(-10,   4),   S(-34,   2),   S(-21,   1),
            S(-35,  49),   S(-20,  37),   S(-22,  15),   S( -4,  20),   S( -8,  16),   S( 24,  -1),   S(-41,  37),   S( 23,  22),
            S(  1,  58),   S( 56,  27),   S( 95,   4),   S( 17,  21),   S( -2,  26),   S( 40,  26),   S( 21,  37),   S(123,   2),
            S(132,  21),   S(149,  -8),   S(153, -18),   S( 91,  -6),   S( 24,   0),   S(  6,  22),   S( 45,  17),   S(100,   9),
            S( 43,  22),   S( 81,  -1),   S(108, -28),   S(118, -36),   S( 52,  -6),   S( 62,   5),   S( 64,  10),   S( 89,  -9),
            S( 86,  -7),   S( 37,  22),   S( 51,  14),   S( 81,  -7),   S( 49,  16),   S( 46,  19),   S( 64,  22),   S( 66,  22),

            /* rooks: bucket 7 */
            S(-81,  46),   S(-78,  49),   S(-56,  40),   S(-60,  30),   S(-36,   9),   S(-27,   7),   S(-60,  42),   S(-59,   5),
            S(-98,  50),   S(-24,  25),   S(-41,  24),   S(-62,  32),   S(-52,   8),   S(-43,   6),   S( 18,  12),   S( -3, -16),
            S(-74,  24),   S(-84,  44),   S(-57,  20),   S(-61,  12),   S(-69,  10),   S(-37,  10),   S( 20, -16),   S( -9, -22),
            S(-83,  43),   S(-13,  14),   S(-16,  14),   S( 36, -11),   S( -9,   2),   S( 26,   1),   S( 74,   2),   S( 11,  -3),
            S( -5,  36),   S( 22,  25),   S( 31,  24),   S( 56,  -3),   S(107, -23),   S(101, -19),   S(108,  -5),   S(-30,  12),
            S(  0,  36),   S( 37,  21),   S( 92,  -4),   S(101, -13),   S(104, -15),   S( 73,   3),   S( 25,  33),   S( -3,  -7),
            S( 37,   0),   S( 43,  -3),   S( 70, -12),   S(103, -31),   S(104, -33),   S(104, -12),   S( 91,  14),   S( 48, -19),
            S(-26,  -1),   S( 12,  14),   S( 27,  19),   S( 12,   9),   S( 28,  -3),   S( 62,   2),   S( 48,  34),   S( 11,  21),

            /* rooks: bucket 8 */
            S( 28, -87),   S( 12, -57),   S( 49, -79),   S( 48, -41),   S( -1, -64),   S( -7, -32),   S( 18, -52),   S( 13, -33),
            S( -2, -67),   S( -6, -64),   S( 21, -38),   S(-40, -54),   S(-21, -53),   S(  1, -23),   S( -2, -36),   S(-21,  -8),
            S(  5,  -3),   S( -7, -27),   S( 19, -10),   S(  5,  -9),   S(  1,  13),   S( 30,  16),   S( 15,  23),   S( -2,   9),
            S( -7, -34),   S(  2,  -1),   S(  1, -23),   S( 13, -14),   S(  3,  -6),   S( 36,  -4),   S(  2,  -4),   S(-15, -20),
            S( -4, -25),   S(  5, -24),   S( 35, -29),   S( 27, -15),   S( 12,   2),   S( -2,   2),   S(  1,   8),   S(-15, -12),
            S( 15,   2),   S( 21,   0),   S(  7, -12),   S( -5, -35),   S(-11, -14),   S( -7,  16),   S(  7,   7),   S( 19,  16),
            S( 35,  18),   S( 31,  20),   S( 16, -17),   S( 16,   4),   S(  1, -13),   S(  7, -15),   S(  8,  25),   S(  9,  17),
            S( 10,  24),   S( 21,  -1),   S( 28,   4),   S( 24,   4),   S(-15,   3),   S( 19,  36),   S( 17,  32),   S( 11,  28),

            /* rooks: bucket 9 */
            S( 22, -125),  S(  6, -98),   S( 21, -114),  S( 11, -106),  S( 17, -98),   S( 14, -66),   S(  4, -64),   S(-27, -86),
            S(-23, -85),   S( -5, -88),   S(-20, -93),   S(-34, -87),   S(-23, -84),   S(-12, -58),   S(-15, -52),   S( -7, -57),
            S(-25, -45),   S( -6, -66),   S(-11, -52),   S( 11, -55),   S( 28, -56),   S( 16, -13),   S(-21,   1),   S(  2, -12),
            S( 16, -42),   S(  3, -44),   S( -5, -18),   S(  7,   1),   S( 12, -61),   S( 18, -34),   S(-20, -46),   S( -2, -23),
            S( 43, -44),   S( -1, -55),   S(  9, -53),   S( 12, -42),   S(  0, -46),   S( -1, -43),   S( -7, -56),   S(-27, -35),
            S(  3, -54),   S(-18, -51),   S( 13, -36),   S( -2, -38),   S( 30, -58),   S( -3, -37),   S( -4, -39),   S( -2, -52),
            S(-12, -17),   S(  8, -30),   S(  5, -35),   S( 13, -44),   S( 14, -52),   S(  3, -35),   S(  6, -19),   S( -7, -36),
            S(  0, -26),   S(  7, -25),   S(  1,  -7),   S( 26,   0),   S( 28, -21),   S(  3, -22),   S( 11,  -7),   S( 13,   7),

            /* rooks: bucket 10 */
            S(-39, -82),   S(-43, -71),   S(-16, -97),   S( 38, -118),  S( 21, -108),  S( 14, -118),  S( 37, -116),  S( 12, -103),
            S(-23, -46),   S(-44, -46),   S(-45, -80),   S(-18, -90),   S(-18, -71),   S(-14, -86),   S( -4, -55),   S(-48, -95),
            S(-30, -46),   S(-36, -48),   S(-46, -74),   S( -3, -64),   S(-17, -60),   S(-11, -48),   S(  5, -63),   S(-22, -49),
            S(-32, -51),   S(-30, -68),   S(-12, -61),   S(  1, -59),   S( 18,   9),   S(  6,   4),   S(  1, -71),   S(  5, -74),
            S(-15, -35),   S(-25, -47),   S( 12, -74),   S( 33, -78),   S(  8, -43),   S( -5, -59),   S( 15, -78),   S(  1, -78),
            S(-10, -38),   S(-15, -52),   S( -4, -77),   S(  0, -81),   S( -5, -67),   S( 20, -41),   S( -1, -71),   S(-21, -64),
            S(-36, -38),   S( -8, -53),   S(-10, -68),   S( -2, -67),   S( 28, -55),   S(-16, -49),   S(-25, -69),   S( -5, -54),
            S(-22, -29),   S( -7, -16),   S(-18, -33),   S( 12, -45),   S(  8,  -8),   S(-11, -19),   S(  5, -44),   S(-13, -23),

            /* rooks: bucket 11 */
            S(-21, -73),   S(-33, -43),   S( -9, -49),   S(-33, -72),   S(-31, -24),   S( 58, -91),   S( -3, -50),   S( -1, -87),
            S(-34, -22),   S(-15, -27),   S(-24, -34),   S(-58, -21),   S(-24, -39),   S(  3, -34),   S( -4, -48),   S(  0, -77),
            S(-44,  10),   S(-31,   0),   S(  8,  22),   S( -8, -16),   S(-14, -36),   S( 17,  -5),   S( 20,  11),   S( 10,   6),
            S( -8, -38),   S( -3, -47),   S(-10, -41),   S( 29, -28),   S(  8, -34),   S(  0, -34),   S( 15,   8),   S( -4, -30),
            S( -7, -40),   S( 18, -21),   S( -4, -28),   S( 19, -45),   S( 20, -33),   S( 32, -60),   S( 29, -10),   S(  2, -22),
            S(-23, -42),   S( 11, -35),   S( -5, -19),   S(  4, -45),   S( -6, -39),   S( 44, -49),   S( 32, -22),   S( -8, -44),
            S(-11, -31),   S(-34, -47),   S(  2, -24),   S(-22, -54),   S(  5, -29),   S( 15, -31),   S( 62, -43),   S(  9, -19),
            S( -1,  -6),   S( 28,  -2),   S(  6,  -8),   S( 30, -23),   S( -9, -12),   S( 34, -14),   S( 31, -16),   S( -1,  19),

            /* rooks: bucket 12 */
            S(-28, -93),   S( -1, -15),   S(  4, -32),   S( -2, -47),   S( -4, -60),   S( 10, -41),   S(-13, -60),   S(-17, -39),
            S(  3,  -5),   S( -1, -17),   S(  5,   6),   S( 17,   3),   S(  0, -36),   S( 23, -14),   S(  0, -24),   S(-13, -28),
            S(  4, -15),   S( 16,  44),   S( 10,  -6),   S( 19, -49),   S( 17, -30),   S( 19,  -9),   S(  5,  -3),   S(-16, -51),
            S( -7, -10),   S( 12,  31),   S( 19,  -1),   S(  8,   4),   S(  5,   2),   S(  4,  -3),   S( -3, -14),   S( -7, -21),
            S(  6, -25),   S(  5, -20),   S( 25,  17),   S(  4,  -6),   S(-13, -43),   S( -7, -30),   S(  0, -22),   S(  1,   9),
            S(  8, -16),   S(  9,   5),   S(-10, -65),   S( -6, -70),   S(  8,  -7),   S( 11,   4),   S( -6, -52),   S(  0, -18),
            S(-13, -11),   S( 18,  12),   S(  7, -31),   S( 10, -14),   S( -4, -22),   S( -8, -36),   S(  5,   5),   S( -1,   8),
            S( -3,  -2),   S(  3,   3),   S(  7, -26),   S( 18,   2),   S(  8,  -6),   S( -2, -10),   S(  4,  11),   S(  2,   0),

            /* rooks: bucket 13 */
            S(-17, -62),   S(-22, -63),   S(-22, -60),   S( 17, -33),   S(-32, -106),  S( 15, -38),   S(-15, -49),   S(-30, -60),
            S(-12, -73),   S( -2, -24),   S( -2, -11),   S(  0,  -7),   S( 17,  -9),   S( 27,  -6),   S( 19, -23),   S(  3, -38),
            S( 10, -55),   S( -2, -46),   S(  6,  -9),   S( 15, -10),   S( 37,  -8),   S( 18, -59),   S(  2, -41),   S( -5, -64),
            S( -9, -37),   S(  6, -36),   S( 10, -23),   S( 12, -17),   S(  9, -41),   S(  3, -20),   S(  6,  -5),   S( -9, -15),
            S(  7, -23),   S(-21, -110),  S(  1, -64),   S(  7, -40),   S( 18, -65),   S( -8, -56),   S( -7, -31),   S( -5, -39),
            S( -7, -44),   S( 12, -47),   S(  2, -64),   S(  2, -68),   S(  5, -102),  S( -3, -55),   S(-22, -71),   S( -9, -44),
            S(  0,  -4),   S( 19, -34),   S( -6, -52),   S(  7, -57),   S( -7, -76),   S(  6, -25),   S( -3, -24),   S( -9, -37),
            S(  2, -10),   S( -8, -39),   S(  4, -30),   S(  8, -20),   S( -6, -55),   S(  2, -32),   S(  1, -29),   S( -3,   5),

            /* rooks: bucket 14 */
            S(-14, -37),   S(-42, -64),   S( -6, -57),   S(-18, -97),   S( -6, -58),   S( 18, -32),   S(-25, -113),  S(-16, -57),
            S( 12, -44),   S( 31,   6),   S(  3, -85),   S(  1, -52),   S( -3, -26),   S( -8, -42),   S( -1, -55),   S(  1, -70),
            S( 10, -13),   S( -4, -72),   S(-14, -77),   S( 12, -55),   S( 14,  -7),   S(-14, -67),   S(  7, -55),   S( -8, -89),
            S( -8, -50),   S( -2, -27),   S(  3, -17),   S( -2, -45),   S(-13, -55),   S( -9, -48),   S(  8, -30),   S( -9, -59),
            S(  2, -13),   S( 10,  16),   S( -3, -53),   S( 15, -74),   S(  5, -61),   S(  5, -50),   S(  0, -77),   S( -7, -78),
            S(  4,  -6),   S(  6, -12),   S( -3, -63),   S( 13, -83),   S( -5, -98),   S(  0, -88),   S(  7, -70),   S(-13, -65),
            S( -7, -11),   S( -6, -15),   S( -5, -58),   S(-11, -83),   S(-10, -78),   S( -4, -60),   S(-10, -78),   S(-15, -54),
            S(  0, -40),   S( -4, -21),   S(-13, -69),   S(  2, -68),   S(  1, -51),   S(-11, -85),   S( -7, -67),   S(-12, -27),

            /* rooks: bucket 15 */
            S( -2, -39),   S( -8, -49),   S(-34, -65),   S(-14, -68),   S( -1, -43),   S( -3, -41),   S( 12,   3),   S(-18, -92),
            S(  2,  -8),   S( -3, -31),   S( -1, -36),   S( -5, -51),   S( -6, -38),   S( -1, -27),   S(  4, -10),   S(  8,   7),
            S(  3, -17),   S( -7, -47),   S( -2, -16),   S( -4, -65),   S( -5, -55),   S(  3, -40),   S(  8,  10),   S(  7,  -2),
            S(  0,  -2),   S(  9,  46),   S(  8,  20),   S(-14, -23),   S( -4, -14),   S(-12, -68),   S(  5, -33),   S( -3, -29),
            S( -3, -23),   S( -2, -12),   S( 17,  -4),   S(  4, -19),   S(-10, -63),   S(  8, -53),   S( 19, -18),   S( 13, -15),
            S(  0, -18),   S(  4,  10),   S( -2, -19),   S(  3, -42),   S( -8, -63),   S( 11, -37),   S( 17, -20),   S( -4, -27),
            S(  2, -11),   S(-10, -17),   S(  1,   0),   S( -6, -37),   S( -7, -42),   S( -4, -54),   S(  5, -19),   S(-11, -38),
            S( -4,   3),   S(  3,  21),   S( -6, -29),   S( -7, -25),   S(  0, -15),   S(  0, -58),   S( 16,  -6),   S(-10, -37),

            /* queens: bucket 0 */
            S(-16, -35),   S(-40, -31),   S(-26, -67),   S(  5, -85),   S( 10, -84),   S(-12, -95),   S(-59, -51),   S(-51, -28),
            S(-10,  -7),   S(-13, -32),   S( 12, -95),   S( -7, -43),   S(-12, -35),   S(-13, -42),   S(-21, -70),   S(-44,   0),
            S(-28,  19),   S(-10, -35),   S(  0, -39),   S( -8, -19),   S(-13,  -6),   S(-21,   9),   S(-13, -29),   S(-71, -31),
            S(-46,  71),   S( 19, -57),   S(-39,  29),   S(-28,  36),   S(  7,  22),   S(-11,  10),   S(-67,  31),   S( -6, -89),
            S(-43,  59),   S(-11,  64),   S(-20,  77),   S(-23,  49),   S( -7,  41),   S(-56,  71),   S(-38,  38),   S(-40, -31),
            S(-52,  82),   S( 33,  63),   S(  5,  62),   S(-27,  86),   S(-94,  86),   S(-61,  85),   S(-98,  61),   S(-35, -10),
            S(  0,   0),   S(  0,   0),   S( 25,  48),   S(  0,  55),   S(-34,  47),   S(-100,  97),  S(-100, 110),  S(-108,  65),
            S(  0,   0),   S(  0,   0),   S( 17,  41),   S(-24,  58),   S(-44,  51),   S(-43,  47),   S(-84,  28),   S(-44,   0),

            /* queens: bucket 1 */
            S(-16, -18),   S(  0, -20),   S( -2, -105),  S( 29, -102),  S( 22, -77),   S( 12, -97),   S( 12,  -4),   S(  4,  14),
            S(-28,  20),   S( 35, -47),   S( 37, -67),   S( 18, -29),   S( 28, -41),   S(  2, -41),   S(-21,   4),   S(-24,  -6),
            S( 13,  -9),   S(  8,   3),   S(  3, -24),   S( 13,  14),   S( -6,  21),   S( 26, -15),   S(-17,  51),   S( 25,   1),
            S( 24,  12),   S(  7,  67),   S(-12,  26),   S( 14,  44),   S(  8,  50),   S(-23,  60),   S( 17,  35),   S(-30,  85),
            S( 17,  34),   S(  8,  94),   S( 42,  46),   S( 19,  74),   S( 20,  78),   S( 55,  47),   S(-12,  91),   S(-18,  49),
            S( 67,  50),   S( 86,  78),   S(131,  94),   S( 86,  76),   S( 51,  73),   S(-21, 115),   S( 25,  52),   S(-12,  67),
            S( 72,   6),   S( 27,  49),   S(  0,   0),   S(  0,   0),   S( 45,  83),   S(-33,  90),   S(-31, 122),   S(-41,  93),
            S( 62,  44),   S( 98,  36),   S(  0,   0),   S(  0,   0),   S( 54,  56),   S( 79,  63),   S( 68,  30),   S(-34,  63),

            /* queens: bucket 2 */
            S( 29, -33),   S(  1,  22),   S( 24, -13),   S( 50, -65),   S( 44, -54),   S( 20, -63),   S(  2, -44),   S( 55,  22),
            S( 16,   9),   S( -2,  56),   S( 36,  -3),   S( 37,   5),   S( 48, -21),   S( 25, -28),   S( 22,   4),   S( 36,  23),
            S( 22,  23),   S( 14,  62),   S(  6,  59),   S( 16,  44),   S( 22,  41),   S( 22,  40),   S( 24,  43),   S( 29,  52),
            S( 20,  56),   S( 16,  93),   S( -4,  85),   S( -4, 100),   S( 23,  82),   S(  9,  87),   S( 20,  78),   S( 19, 110),
            S( -5,  78),   S( 18,  62),   S(  2, 127),   S(  9, 115),   S( 24, 122),   S( 37,  92),   S( 52, 138),   S( 44, 105),
            S(-12,  87),   S(-34, 105),   S( 22,  92),   S( 52, 121),   S( 68, 104),   S( 67, 185),   S(141,  61),   S( 18, 145),
            S(-26,  98),   S(-41, 131),   S(-15, 123),   S( 70,  79),   S(  0,   0),   S(  0,   0),   S(-10, 190),   S( 37, 102),
            S(-11,  80),   S(  8,  83),   S( 52,  55),   S( 39,  85),   S(  0,   0),   S(  0,   0),   S( 59, 103),   S( 24, 142),

            /* queens: bucket 3 */
            S(-39,  83),   S(-21,  68),   S(-13,  64),   S(  7,  76),   S( -8,  49),   S(-12,  38),   S( 23, -19),   S(-36,  60),
            S(-45, 104),   S(-23, 102),   S( -7, 100),   S( -2, 100),   S( -6,  95),   S( -1,  67),   S( 20,  44),   S( 53,  -7),
            S(-37,  96),   S(-24, 124),   S(-31, 155),   S(-20, 154),   S(-19, 135),   S(-14, 124),   S(  3, 105),   S( -4,  84),
            S(-24, 107),   S(-46, 153),   S(-35, 169),   S(-26, 182),   S(-28, 183),   S(-25, 158),   S( -3, 140),   S(-13, 130),
            S(-51, 156),   S(-35, 172),   S(-29, 178),   S(-25, 187),   S(-31, 205),   S(-18, 210),   S(-14, 207),   S(-25, 182),
            S(-40, 154),   S(-53, 186),   S(-64, 208),   S(-63, 211),   S(-46, 238),   S(  8, 231),   S(-30, 246),   S(-22, 224),
            S(-95, 195),   S(-86, 219),   S(-104, 259),  S(-94, 239),   S(-87, 255),   S(-16, 188),   S(  0,   0),   S(  0,   0),
            S(-117, 212),  S(-90, 199),   S(-99, 212),   S(-77, 204),   S(-49, 194),   S( -3, 207),   S(  0,   0),   S(  0,   0),

            /* queens: bucket 4 */
            S(-41, -22),   S(-40, -28),   S(-10, -36),   S(-32,   2),   S(-47,   0),   S(-23, -19),   S( -8, -20),   S(  9,  12),
            S(  2,  -7),   S(-34,  23),   S( -6,   7),   S( -2, -14),   S(-59,  21),   S(-10,   1),   S(-19,   0),   S(-19,  -9),
            S(-11,   7),   S( 17, -13),   S( 23, -19),   S( -8,   0),   S( 16, -20),   S(  7,   2),   S(-61, -59),   S(  4, -11),
            S(-12,  -7),   S( 18, -14),   S( 17,   1),   S( -2,   3),   S( 28,  -8),   S(  5,  11),   S(-22, -18),   S(-57,  11),
            S(  0,   0),   S(  0,   0),   S( 33,   7),   S( 33,  38),   S( 48,  15),   S( 13,  15),   S( 30,  18),   S(-22,  -5),
            S(  0,   0),   S(  0,   0),   S( 37,  25),   S( 40,   9),   S(  2,   6),   S( 41,  21),   S( -7,  20),   S(-10, -31),
            S( 38,  31),   S( 36,  36),   S( 77,  30),   S( 74,  52),   S( 54,  10),   S( 39,  14),   S(-33,  13),   S(-39,  10),
            S( 46,  -2),   S( 12,  20),   S( 50,  21),   S( 32,  -1),   S( 10, -12),   S( -2,  -8),   S( -9, -25),   S(  6,  17),

            /* queens: bucket 5 */
            S( 31,   9),   S( 47, -27),   S(-12,  -7),   S(-23,   3),   S(  1,  -7),   S( 40,  20),   S( 18,  -4),   S( 21,  -6),
            S(  2, -28),   S(  0, -21),   S(-16,  -5),   S(-42,  13),   S(  1, -12),   S( -5, -23),   S( 10,  11),   S( 25,   7),
            S( 23,   7),   S( 47, -40),   S( 35, -34),   S(-37,   0),   S( 17,  -5),   S(-16,   9),   S(  1,  -9),   S(  3,   3),
            S( -9, -25),   S( 57,  26),   S( 45,   2),   S( 12,   0),   S( 47, -40),   S( 27, -11),   S( 25,  24),   S(  6,  13),
            S( 72,  30),   S( 66,  18),   S(  0,   0),   S(  0,   0),   S( 36,  25),   S( 56,  15),   S( 30,  38),   S(  2,   9),
            S( 85,   8),   S( 55,  37),   S(  0,   0),   S(  0,   0),   S( 34,   2),   S( 94,  35),   S( 32,  18),   S( 24,  51),
            S(104,  30),   S( 82,   3),   S( 57,  35),   S( 45,  44),   S( 88,  46),   S( 81,  33),   S(105,  27),   S( 44,  18),
            S( 62,  22),   S( 87,  18),   S( 85,  34),   S( 96,  59),   S( 64,   2),   S( 69,  18),   S( 60,  49),   S( 54,  23),

            /* queens: bucket 6 */
            S(  9,  -4),   S(  7, -16),   S(  1,   7),   S(  0,  17),   S( 16,   0),   S(-23, -24),   S(-34,  11),   S(-20,  14),
            S( -5, -17),   S( 19,  -6),   S( 20,  -5),   S( 37, -14),   S( -3, -10),   S( -8, -14),   S(-34,  30),   S(  3,  24),
            S(-36,  42),   S( 22, -20),   S(  1,   0),   S( 32, -39),   S( 30, -20),   S( 15, -31),   S( 60,  17),   S( 41,  34),
            S(  7,  43),   S(-20,  31),   S( 54, -34),   S(109, -36),   S( 56, -29),   S( 74,   3),   S( 70,  21),   S( 87,  16),
            S(  5,  20),   S( 15,  24),   S( 60,   6),   S( 53,  13),   S(  0,   0),   S(  0,   0),   S( 84,  60),   S(122,  36),
            S( 38,  42),   S( 18,  43),   S( 32,  47),   S( 41,  35),   S(  0,   0),   S(  0,   0),   S(133,  68),   S(131,  40),
            S( 20,  19),   S( 52,  21),   S( 71,  20),   S( 86,  19),   S( 67,  51),   S( 90,  62),   S(180,  28),   S(185,  -1),
            S( 43,  -8),   S( 62,   2),   S(102,  12),   S(127,  38),   S(121,  37),   S(133,  28),   S(147,  32),   S(132,  22),

            /* queens: bucket 7 */
            S(-33,   3),   S(-14, -18),   S(-25,  -6),   S(-41,  41),   S( -7, -25),   S(-35,  13),   S(-11,  19),   S(-13,  -7),
            S(-31, -20),   S(-38, -16),   S(-15,  14),   S(-30,  30),   S(-26,   4),   S(-41,  30),   S(-19,  17),   S(-61,  38),
            S(-21, -26),   S(-75,  22),   S(-27,   4),   S( 31, -17),   S( 31, -12),   S( 14, -21),   S(-10,  14),   S( 10,   8),
            S(-62,  -4),   S(  1, -15),   S(  1,   5),   S( 99, -44),   S( 70, -19),   S( 72, -11),   S( 47, -25),   S( 26,  22),
            S(-17,  15),   S(-44,  49),   S( 16,  30),   S( 46,  -4),   S(116, -14),   S( 96,  33),   S(  0,   0),   S(  0,   0),
            S(-37,   7),   S( 13,  20),   S( -8,  19),   S( 15,  12),   S( 90, -13),   S(154,  24),   S(  0,   0),   S(  0,   0),
            S(-38,  18),   S( -5,  -1),   S( 33,   2),   S( 66,   0),   S( 89,  14),   S(102,  16),   S(113,  72),   S( 82,  86),
            S(  1,   9),   S( 33,   1),   S( 37,  -2),   S( 61, -18),   S( 65,  -2),   S( 70,  37),   S( 26,  27),   S( 78,  39),

            /* queens: bucket 8 */
            S( -4,   5),   S( 16,  -8),   S(-12, -30),   S(  4, -10),   S( -9,   3),   S(  2,   0),   S( -7, -23),   S( -4, -19),
            S( -6, -15),   S( -8, -15),   S( 32,  15),   S(-14, -15),   S(  8,  -2),   S(  1,   3),   S(  2,   6),   S( -2,   3),
            S(  0,   0),   S(  0,   0),   S( 25,  20),   S(  5, -32),   S( 13,  -6),   S( 17,   2),   S( -7,  -3),   S( -9,  -7),
            S(  0,   0),   S(  0,   0),   S(  5,   5),   S(-26, -67),   S(-22, -34),   S(  1, -13),   S( 11,  22),   S( -6,  -4),
            S(  5,   1),   S(  9,   7),   S( 24,  16),   S( 24,  -7),   S(  4, -18),   S(  5, -28),   S(  0, -30),   S(  9,   3),
            S(  4, -25),   S( -7, -28),   S( 28,  23),   S(  1, -23),   S( 11,  -4),   S(-19, -60),   S( -7, -26),   S( -9, -18),
            S( -7, -22),   S( -5, -11),   S( 20,   6),   S( 15,   8),   S(  4, -13),   S( -8, -24),   S( 12,  -4),   S( -4,  -8),
            S(  9,   4),   S( 15,  25),   S(  8,   0),   S( 16,  28),   S( 33,  40),   S( -8, -19),   S(  1,   5),   S(-41, -68),

            /* queens: bucket 9 */
            S(  9,   9),   S(-19, -72),   S( -3, -23),   S(  5, -30),   S(-12, -52),   S(  2, -11),   S( -6, -18),   S(-10, -27),
            S( 27,   9),   S(-23, -43),   S( -1, -17),   S(  5, -14),   S(  0, -27),   S( -5, -22),   S(  8, -11),   S( -2,   4),
            S( 11, -22),   S(-15, -50),   S(  0,   0),   S(  0,   0),   S( -5, -14),   S(  9, -25),   S( -5, -25),   S(-10, -17),
            S(  8,  -4),   S(  1, -27),   S(  0,   0),   S(  0,   0),   S(  3, -15),   S( 12, -19),   S( 19,  11),   S( -6,  -1),
            S( 13, -25),   S( -3, -51),   S(-15, -32),   S(-15, -38),   S(-25, -69),   S( 10, -25),   S(  0, -44),   S( -3, -23),
            S(  1, -22),   S(  8, -46),   S( 13, -18),   S(  4, -33),   S( 13, -27),   S(  6, -22),   S(-11, -47),   S( -8, -29),
            S( -4, -30),   S( 13, -40),   S( 23,  -8),   S( -9, -26),   S( 13, -26),   S( 16,   0),   S(-14, -37),   S(  5,  -3),
            S(  1, -19),   S( 29,  18),   S(  1, -17),   S( -2, -27),   S( 20,   5),   S(-19, -52),   S(-19, -43),   S(-16, -47),

            /* queens: bucket 10 */
            S( -4,  -2),   S( -2, -26),   S( 10,  -2),   S( 16,  -5),   S( 15,  -6),   S( 22,  18),   S(-14, -45),   S( -1, -20),
            S(  7,   0),   S(  2, -25),   S( 12, -23),   S(-19, -54),   S( 21,  19),   S( 12, -12),   S( -1, -34),   S(  8,  -6),
            S( -6, -21),   S(  5, -12),   S( 17,  -2),   S(  5, -21),   S(  0,   0),   S(  0,   0),   S(  1, -26),   S(  1, -19),
            S(  5,  -4),   S( -9, -20),   S(  8, -14),   S( -4, -33),   S(  0,   0),   S(  0,   0),   S(-13, -40),   S( 13, -15),
            S( -2, -17),   S(-17, -56),   S(  2, -39),   S(-12, -52),   S(-11, -53),   S(  0, -13),   S( -9, -45),   S( 32,   1),
            S(-24, -56),   S(-12, -41),   S(  0, -37),   S(  0, -41),   S(  5, -19),   S(-14, -45),   S( 30, -14),   S( 20, -32),
            S( -4, -19),   S( -4, -11),   S(  5, -27),   S(  4, -12),   S(  6,  -8),   S( 12,  -1),   S( 16, -19),   S( 15, -23),
            S( -4, -14),   S( -1, -33),   S(  2, -24),   S(  6, -21),   S(  0, -10),   S(  7,  -7),   S( 25,   8),   S( -4, -51),

            /* queens: bucket 11 */
            S(-22, -29),   S( 11, -10),   S(-20, -40),   S(-18, -39),   S(-25, -26),   S( 15,  -3),   S(  8,   9),   S( 23,  17),
            S(-18, -29),   S(  1,   0),   S(-28, -23),   S( 17,   1),   S( -3, -37),   S( 27,  25),   S( 17, -11),   S(-13, -34),
            S(  0,  -2),   S( 17,   6),   S(-27, -36),   S(-29, -47),   S( 21,   0),   S(  6,  -3),   S(  0,   0),   S(  0,   0),
            S( -7, -14),   S(-38, -43),   S(-19, -27),   S(-31, -68),   S(  4, -17),   S(  7,   0),   S(  0,   0),   S(  0,   0),
            S(-14, -14),   S(-17, -24),   S( -6, -19),   S( -5, -32),   S( 13, -20),   S( -5, -31),   S( -8, -14),   S(-11, -18),
            S(-14, -23),   S(  0,   6),   S(-13, -33),   S(  8,  11),   S(-12, -42),   S( 18, -20),   S( 11, -12),   S(  0, -49),
            S(-16,  -9),   S(  3,   7),   S( 12,   7),   S( -4, -22),   S( 22,  15),   S( 27,  13),   S(  6, -11),   S( 26,   9),
            S(-38, -68),   S( 28,  16),   S( 24,  25),   S(  8,   7),   S( 18,  28),   S( -8, -37),   S(  0,   2),   S(  3, -10),

            /* queens: bucket 12 */
            S(  0,   0),   S(  0,   0),   S( 16,  32),   S( -7, -11),   S(  4,   7),   S( -4,   1),   S( 11,  10),   S(  3,   9),
            S(  0,   0),   S(  0,   0),   S( 21,  29),   S( -4, -23),   S(-11, -19),   S( 13,  20),   S( -6, -19),   S(  1,   5),
            S( -2,  -5),   S(  9,  20),   S( -2,  -4),   S(  6, -10),   S( 14,   6),   S( -4, -13),   S(  4,   0),   S( 11,  13),
            S(  6,  -5),   S( 12,  14),   S(  9,   1),   S(  3,  -5),   S( -8, -39),   S(-13, -35),   S(  7,  11),   S( -4, -10),
            S( -6, -21),   S( 10,  21),   S( 13,  14),   S( -7, -43),   S(-10, -19),   S(-20, -50),   S(  0, -11),   S( -6, -10),
            S( -3, -12),   S(  1,  -1),   S(-12, -46),   S(-21, -40),   S(-16, -35),   S( -9, -15),   S( -9, -28),   S( -6, -17),
            S(  6,  12),   S( -1,   2),   S( -8, -23),   S( -3, -10),   S( -4, -17),   S(-22, -49),   S( -9, -15),   S(-10,  -8),
            S(-10, -10),   S( -2,  -3),   S(-13, -28),   S( -6, -11),   S( -4, -12),   S(-24, -61),   S(  0,  -2),   S(-26, -48),

            /* queens: bucket 13 */
            S( -1, -18),   S( -3, -14),   S(  0,   0),   S(  0,   0),   S( -5,  -5),   S(  0, -16),   S(  5,  -4),   S(  0,  -1),
            S( -3, -32),   S( -8, -18),   S(  0,   0),   S(  0,   0),   S(-19, -37),   S( -1, -11),   S(-21, -44),   S( -2,  -9),
            S(  1, -31),   S( -7, -20),   S( -3,  -3),   S( -1,  -7),   S( -4, -20),   S(  0, -12),   S(  2,   3),   S( -6,  -9),
            S( 12,   8),   S(  1, -15),   S( -4, -22),   S(-15, -53),   S(-22, -55),   S( -9, -22),   S( -3, -13),   S(  2, -15),
            S( -3, -21),   S(-17, -49),   S(-11, -35),   S(  1, -21),   S(-30, -76),   S(-13, -35),   S( -4, -26),   S( 18,  47),
            S( -8, -12),   S(-17, -43),   S( -3, -14),   S(-12, -24),   S(  6, -12),   S(-24, -54),   S(-23, -53),   S(-26, -54),
            S( -6, -17),   S( -7, -20),   S(  0, -16),   S( -3, -12),   S( -5, -10),   S(  3, -12),   S(-28, -57),   S( -5, -10),
            S(-22, -39),   S(  0,  -2),   S(  0,  -1),   S( -1,  -4),   S( -5,  -1),   S(-22, -39),   S(  7,   6),   S(-10, -16),

            /* queens: bucket 14 */
            S( 14,  18),   S(  9,   8),   S( 14,   4),   S( -3, -14),   S(  0,   0),   S(  0,   0),   S( -7, -21),   S(  3, -10),
            S( -2, -12),   S( -8, -28),   S( -2, -16),   S(  1,  -9),   S(  0,   0),   S(  0,   0),   S(-14, -27),   S(-19, -44),
            S( -6, -15),   S( -8, -39),   S( -5, -25),   S( -2, -16),   S( -3,  -6),   S( -3, -15),   S( -8, -28),   S(-12, -35),
            S(  0,  -8),   S(  0,  -5),   S( -5, -23),   S(-15, -42),   S( -9, -25),   S(-17, -54),   S(-13, -41),   S( -2,  -7),
            S(-12, -24),   S( -8, -21),   S(-18, -43),   S(-20, -54),   S( -8, -39),   S(-17, -41),   S(-16, -55),   S(-13, -48),
            S(-14, -29),   S( -3, -20),   S(-17, -49),   S(-20, -53),   S( -6,  -9),   S(-18, -33),   S(-12, -19),   S( -9, -26),
            S(-16, -25),   S(-16, -36),   S(-12, -30),   S(-15, -29),   S( -2,  -6),   S( -2, -15),   S( -6, -18),   S( -5, -13),
            S(-13, -36),   S( -2,  -8),   S(-15, -27),   S(-16, -27),   S( -1,  -3),   S( -8, -15),   S(  3,  -1),   S(-11, -27),

            /* queens: bucket 15 */
            S( -3, -12),   S(  1,  -5),   S( -4,  -9),   S(-19, -34),   S(  6,  -6),   S( -5, -15),   S(  0,   0),   S(  0,   0),
            S( -8, -19),   S( -2,  -7),   S( -9, -24),   S(-16, -46),   S( 11,  17),   S(  5,   5),   S(  0,   0),   S(  0,   0),
            S( -1,  -2),   S(  0,  -3),   S(-16, -25),   S(-16, -39),   S(-22, -49),   S( 14,  15),   S(  1,  -5),   S(  2,   3),
            S( -1,   0),   S(-15, -34),   S( -7, -10),   S(-12, -29),   S( 13,  15),   S( 15,  24),   S(  0,   2),   S( -2, -13),
            S(  1,  -1),   S( -7, -23),   S(-19, -53),   S(-17, -47),   S( -9, -30),   S(  0,  -9),   S( -8, -18),   S( -3, -14),
            S(  5,   5),   S(-11, -27),   S(-22, -39),   S(-15, -39),   S(-17, -35),   S(-25, -60),   S( -3,  -5),   S( -9, -16),
            S( -6, -16),   S( -7, -16),   S( -3, -16),   S( -4, -19),   S(-20, -42),   S( -9, -21),   S(-12, -18),   S( -1,  -4),
            S( -9, -21),   S(-14, -31),   S( -7, -29),   S( -9, -11),   S(-15, -20),   S(  3,  -1),   S(  2,   4),   S(-10, -21),

            /* kings: bucket 0 */
            S( -3, -36),   S( 23,   4),   S( 12, -11),   S(-19,  16),   S(-19,  -3),   S( 27, -19),   S(  9,  19),   S( 31, -49),
            S(-13,  17),   S( -8,  17),   S(-13,   4),   S(-35,   7),   S(-24,  23),   S(-22,  21),   S(-19,  56),   S(-21,  47),
            S(  3,  -2),   S( 44, -20),   S(-10,  -1),   S(-19,  -5),   S(-23, -12),   S(-13,  -6),   S(-42,  26),   S(  5,  -4),
            S( -1, -22),   S(-33, -10),   S(-16, -23),   S(-68,  15),   S(-34,  17),   S(-73,  21),   S(-38,  18),   S(-69,  38),
            S(-38, -61),   S( 55, -35),   S(-15, -21),   S( 33, -12),   S(-16, -14),   S(-20,   3),   S(  3,  -4),   S( 12,   3),
            S(  9, -123),  S( 43, -46),   S( 46, -74),   S( 35, -37),   S( 21, -10),   S( 14, -30),   S( 43, -23),   S( -8,  -5),
            S(  0,   0),   S(  0,   0),   S( 15, -39),   S( 43, -22),   S( 33, -23),   S(  2,   0),   S( 17, -18),   S(-12, -14),
            S(  0,   0),   S(  0,   0),   S( -6, -79),   S( 12, -60),   S(  8, -23),   S( -1, -30),   S( 11, -10),   S( 13,  27),

            /* kings: bucket 1 */
            S( 14, -33),   S( 26,  -9),   S( 15, -19),   S( 12,   4),   S(-14,   4),   S( 25,  -7),   S(  8,  35),   S( 30, -11),
            S(  3,  15),   S( 10,  12),   S( 19, -16),   S(-47,  31),   S(-23,  22),   S(-12,  19),   S( 10,  26),   S( -3,  22),
            S(-19, -18),   S(  5, -14),   S(  3, -18),   S(  6, -14),   S(-48,   6),   S(  4, -19),   S( 11,   0),   S( 55, -26),
            S( 47,  -5),   S( 38, -12),   S( -8,  -6),   S( -6,   5),   S(-20,  18),   S(-25,  10),   S(-18,  12),   S(-49,  37),
            S(  3, -22),   S( 11, -32),   S( 17, -39),   S( 50, -34),   S(  0, -16),   S( 29, -24),   S( 11, -10),   S(-10,   4),
            S( 33, -15),   S( 55, -50),   S( 21, -10),   S( 56, -15),   S(  5, -22),   S(  2,  10),   S(-31,   8),   S(  0,   5),
            S(  5, -39),   S(  1,   1),   S(  0,   0),   S(  0,   0),   S(  0,  -1),   S(  7,  18),   S( -9,  48),   S(-14, -23),
            S(-21, -130),  S( -3, -11),   S(  0,   0),   S(  0,   0),   S(  0, -28),   S(  2,  -3),   S( -4, -12),   S(-13, -69),

            /* kings: bucket 2 */
            S( 29, -54),   S(  7,  -2),   S( 16, -27),   S( 31, -13),   S(-14,   5),   S( 36, -20),   S(  4,  36),   S( 34, -14),
            S( 25,  -8),   S(-14,  34),   S(-11,   2),   S(-11,   4),   S(-16,  12),   S(-11,   5),   S(  7,  18),   S( -3,  12),
            S(-40,   1),   S(-11, -13),   S(-10, -17),   S(-17, -19),   S(-12,  -9),   S( -5, -26),   S( 22, -19),   S( 12, -11),
            S(-11,  18),   S( -4,  -1),   S(-22,   2),   S( -4,   8),   S( 12,  -2),   S( -6, -13),   S(  8, -18),   S( 62, -27),
            S(-38,  -4),   S(-20,   2),   S( 33, -29),   S(  9, -24),   S( 51, -38),   S(  3, -45),   S( 51, -41),   S( 14, -34),
            S(-15,   9),   S(  0,   4),   S( 22, -16),   S( 46, -29),   S( 62, -24),   S( 40,   4),   S( 93, -35),   S( 32, -27),
            S(  3, -10),   S( 15,  18),   S(-21,  -7),   S( 11, -15),   S(  0,   0),   S(  0,   0),   S( 24,  26),   S( -2, -56),
            S(-12, -23),   S( -7, -45),   S( 21, -51),   S(  4,  -3),   S(  0,   0),   S(  0,   0),   S(  2,   7),   S(-21, -156),

            /* kings: bucket 3 */
            S(  8, -77),   S( 11, -23),   S( 23, -52),   S( -6, -26),   S( -7, -36),   S( 32, -37),   S(  0,  21),   S( 14, -35),
            S(-13,  15),   S(-30,  27),   S(-27,  -7),   S(-45,   5),   S(-47,  10),   S( -5, -12),   S( -8,  20),   S(-14,  11),
            S( 22, -43),   S(-11, -23),   S( 10, -32),   S(-17, -30),   S( -7, -17),   S( 18, -49),   S( 18, -25),   S( 47, -33),
            S(-66,  16),   S(-103,  30),  S(-93,  16),   S(-113,  21),  S(-54,   3),   S(-83,  -3),   S(-63,  -9),   S(-23, -24),
            S(-15,  -2),   S(-14, -20),   S(-38, -13),   S(-79,  -5),   S(-12, -34),   S( -3, -49),   S( 17, -56),   S( 19, -79),
            S(-35, -26),   S( -3, -11),   S( 19, -46),   S( -5, -30),   S( 42, -48),   S(121, -78),   S(154, -64),   S( 55, -115),
            S(-44, -34),   S( 28, -36),   S( 21, -29),   S(  9, -39),   S(  7, -56),   S( 65, -37),   S(  0,   0),   S(  0,   0),
            S( -6,  -5),   S( -4, -30),   S(  5,  -9),   S( -5, -39),   S(  9, -74),   S( 29,  -8),   S(  0,   0),   S(  0,   0),

            /* kings: bucket 4 */
            S(-35,  -3),   S( 18,  18),   S(  5,  13),   S(-10,  -2),   S(-10,  -1),   S(-12,  18),   S( 29,  20),   S( 42,  -2),
            S(-21,  20),   S( 23,  18),   S(-32,  19),   S(-33,  20),   S( 48, -11),   S( 39, -11),   S( 72,  -2),   S( 15,   4),
            S(-12,  17),   S( 20,  -7),   S(-21,  13),   S(-23,   0),   S( -4,  -3),   S( 25, -22),   S( 13, -15),   S(-17,   9),
            S(-25, -14),   S( 18,   4),   S( 13,   1),   S( 18,   7),   S(-18,   3),   S(-23,  13),   S( 11,   5),   S( 11,  19),
            S(  0,   0),   S(  0,   0),   S(  7, -19),   S(  0,  -7),   S( -8,   5),   S(-17,  -3),   S(-15,   0),   S(-20, -14),
            S(  0,   0),   S(  0,   0),   S(  9,  13),   S( -3,  -5),   S(  1,   0),   S(-36, -20),   S(  6,  -2),   S(  4,  -6),
            S( -2, -22),   S(  2,  28),   S( -2, -23),   S(  7, -15),   S( -5,   8),   S(-13, -10),   S( -1, -17),   S( -3, -17),
            S( -4,  51),   S( -9, -19),   S(  4,  28),   S(  3,  -4),   S(  8,   0),   S(-11, -12),   S(  7,  11),   S(  2,  11),

            /* kings: bucket 5 */
            S( 32,  -4),   S(-22,  30),   S(-43,  25),   S(-18,  19),   S(-19,  18),   S( 39,   2),   S( 67,   2),   S( 10,  30),
            S( 28,  -4),   S( 72, -10),   S( 16,   0),   S( 43,  -5),   S(-13,  12),   S( 44,  -4),   S( 44,  15),   S( 76, -12),
            S( 11,  -4),   S(-29,   6),   S(-31,  -6),   S(-31,  -2),   S( -7,  -1),   S(-67,   7),   S( 29,  -7),   S(  2,   1),
            S(-17, -12),   S( 48, -12),   S( 37, -11),   S( 25,  17),   S( 34,   6),   S( 22,  -1),   S( -7,   9),   S(-26,  20),
            S(-38, -15),   S(-15, -23),   S(  0,   0),   S(  0,   0),   S( -8,  -1),   S(-48,  -4),   S(-42,   7),   S(-30,   1),
            S(-45, -12),   S(-23,   8),   S(  0,   0),   S(  0,   0),   S( -8,  21),   S(-25,  13),   S(-34,  22),   S(  5,   0),
            S(-13,  -3),   S(-21,  18),   S( -6,  16),   S( -4,  -8),   S(-10, -20),   S(-12,  20),   S(  0,  17),   S(-21, -21),
            S(-21, -33),   S(  4,  24),   S(  0,  25),   S( -2,  51),   S(  6,  45),   S(  2,  22),   S( 12,  15),   S(  0,  14),

            /* kings: bucket 6 */
            S( 16,  -9),   S( 41, -10),   S( -6,   3),   S( 44, -10),   S(-31,  25),   S( -5,  24),   S( 17,  38),   S( 28,  15),
            S( 52,  -8),   S( 33,   7),   S( 17,  -2),   S( 31,  -3),   S( 25,   1),   S( 15,   7),   S( 26,  19),   S( 26,   6),
            S(  4,  -3),   S( -8,  -5),   S( -1, -11),   S(-15,  -4),   S(-17,   1),   S(-49,   0),   S(-22,  10),   S(-54,  21),
            S( -3,  13),   S( 26,   6),   S( 13,  -9),   S( 32,   6),   S( 54,   2),   S( 15,  -4),   S(109, -28),   S( 13,  -4),
            S(-12, -12),   S(-19,  -7),   S( -8, -13),   S(  4,  -6),   S(  0,   0),   S(  0,   0),   S(-29, -14),   S(-56,  -8),
            S(-13,  11),   S(-10,  18),   S(-36,  16),   S(-24, -10),   S(  0,   0),   S(  0,   0),   S(-30,  23),   S(-77,  12),
            S(  0, -14),   S(  0,  13),   S(-15,  -4),   S(-17,  -6),   S(  6,  14),   S( -8,   1),   S( -4,  26),   S(-37, -21),
            S(  0,  30),   S( -3,  16),   S(  7,  16),   S( -3,  20),   S( -9,   5),   S(-14,  13),   S(-14,  41),   S(-14, -49),

            /* kings: bucket 7 */
            S( 90, -58),   S( 18, -11),   S( 20, -15),   S( -9,   9),   S(-35,  15),   S(-34,  32),   S(-15,  48),   S(-11,  20),
            S(  6,   0),   S( 30, -11),   S(  7, -15),   S(-19,   1),   S(-13,  14),   S(-23,  17),   S( 14,  19),   S(  2,  21),
            S( -8, -20),   S(-17,  -1),   S( -8, -10),   S(-26,  -2),   S(-31,  -1),   S(-58,  12),   S(-24,  10),   S(-30,  16),
            S(  6,  -2),   S(  2,   5),   S( 14,  -6),   S( 21,  -2),   S(  5,   0),   S( 47, -17),   S( 15,  -6),   S( 40, -22),
            S( -4,  18),   S(-13,   1),   S(-22,   2),   S(-15,  -1),   S( -3, -11),   S( 18, -38),   S(  0,   0),   S(  0,   0),
            S( 21,  -1),   S( 11, -17),   S(  9, -10),   S(-20, -15),   S(  1,  -7),   S( -1,   6),   S(  0,   0),   S(  0,   0),
            S(  1, -13),   S( 37,  -4),   S( 19,  -4),   S(-10, -15),   S( 35, -17),   S( -4,  -5),   S( 11,  -2),   S( -8, -26),
            S( 10,  10),   S(  6,  -1),   S( 26,  -3),   S(  3,   8),   S(  4,  -5),   S(-15, -16),   S(  0,  29),   S( -3, -15),

            /* kings: bucket 8 */
            S(-37, 114),   S(-57,  58),   S(-83,  65),   S(-14,  -4),   S(  6,  -3),   S( 20, -10),   S( 72, -22),   S(-45,  24),
            S(  6,  72),   S( 20,  18),   S(-12,  49),   S(  6,  11),   S( 19,   4),   S( 28,  -3),   S( 18,  28),   S( 20,  29),
            S(  0,   0),   S(  0,   0),   S( 21,  32),   S( 30,  -7),   S( 26,  -1),   S( -5,   1),   S(-10,   0),   S(-15,   2),
            S(  0,   0),   S(  0,   0),   S( 38,  36),   S( 31, -15),   S(  3,  -8),   S( 31,  -1),   S( 23, -15),   S(-12,  15),
            S(  1,   8),   S(  3,   2),   S(  5,   0),   S( 16, -10),   S( 13, -19),   S( 12,  -3),   S(-11,   1),   S(  3, -16),
            S( -6, -14),   S(  2,   4),   S(  2,  12),   S(  2, -25),   S( -3,  -4),   S( -8,  -3),   S(-12,  -9),   S(  1,  -6),
            S( -8, -16),   S( -3, -26),   S( -2, -12),   S( -1, -14),   S( -5, -26),   S( -5,  -6),   S(  2,   8),   S( 18, -37),
            S( -2,   8),   S( -4, -15),   S(  5, -10),   S(  4,   4),   S(  0,   1),   S( -1, -19),   S(  8, -11),   S(  4,  11),

            /* kings: bucket 9 */
            S(-55,  58),   S(-46,  34),   S(-97,  60),   S(-77,  35),   S(-80,  33),   S(-87,  49),   S( 37,  12),   S(  9,  32),
            S(-21,  39),   S( 24,  15),   S( 11,  -1),   S( 21,  11),   S( 46,  16),   S( 12,   5),   S( 39,  29),   S( 20,  19),
            S( -9,  20),   S( -2,  32),   S(  0,   0),   S(  0,   0),   S( 31,  15),   S( -8,   0),   S( 28,   4),   S(  5,   5),
            S( -8, -25),   S(  4, -11),   S(  0,   0),   S(  0,   0),   S(  8,   7),   S( 33,  -7),   S( 21,  13),   S( -7,   1),
            S( -6,  -3),   S( -4,  13),   S(  7,  13),   S(  3, -18),   S(  6, -27),   S(-27,   3),   S(  7,  11),   S(-17,   4),
            S( -8,   5),   S( -6,  10),   S( -4,   9),   S( -7,  -3),   S( -5,  16),   S( -7,  13),   S(-23,  12),   S( -6,   9),
            S(  0,  13),   S(  4, -18),   S(  1,   2),   S( 11,   9),   S(  2,  20),   S( 15,  14),   S( -3, -10),   S(  2,  17),
            S(  0,  24),   S( -5,  -5),   S(  2,  -9),   S(  0, -18),   S( -9, -37),   S(  2, -20),   S( -2, -16),   S( 13,  37),

            /* kings: bucket 10 */
            S( -6,  22),   S(-16,  17),   S(-27,  31),   S(-66,  38),   S(-74,  32),   S(-187,  74),  S(-23,  50),   S(-132, 100),
            S( 17,   7),   S(  6,   7),   S( 24, -23),   S( 33,   8),   S( 57,   4),   S( 59,   3),   S( 52,  25),   S(-43,  40),
            S(  4,  10),   S( 21,   6),   S( 24, -12),   S( 17,   1),   S(  0,   0),   S(  0,   0),   S( 36,  12),   S(-77,  30),
            S(  9,  -1),   S( 25,   3),   S( 25, -11),   S( 25,  -5),   S(  0,   0),   S(  0,   0),   S( 30,   8),   S(  0,  -2),
            S(  4,  -6),   S( 11,  11),   S( -7,  -8),   S( 31,  -8),   S( -6,  -6),   S(  7,   0),   S(  1,  15),   S( -8,   8),
            S(-14,  19),   S(-15,   5),   S(  5,   4),   S(  4,  10),   S(  4,  12),   S( -5,  -6),   S(-25,   4),   S(-19,  25),
            S(  9,  -2),   S(  8,  16),   S(  6, -11),   S(  0,   9),   S( 13,  32),   S(  4,   2),   S(  6, -14),   S( 10,  22),
            S( -5,  13),   S( -5, -37),   S( 10,  16),   S(  2,  12),   S(  1, -18),   S( -4, -17),   S(  1, -27),   S(  9,  45),

            /* kings: bucket 11 */
            S(-46,  40),   S(  6,   5),   S( -5,   8),   S(-39,  11),   S(-36,  -2),   S(-166,  82),  S(-77,  80),   S(-239, 171),
            S( -5, -27),   S( -6,  11),   S( 32, -30),   S( 40,   4),   S( 46,  -2),   S(-28,  55),   S( 32,  20),   S( 56,  35),
            S( 16,  -7),   S(  9,   3),   S( -8, -12),   S( 13,   4),   S( 57, -13),   S( 35,  20),   S(  0,   0),   S(  0,   0),
            S( 14,  28),   S(-15,  -3),   S( 17,  -6),   S( 79, -17),   S( 60, -28),   S( 39,   9),   S(  0,   0),   S(  0,   0),
            S(  5,  -2),   S( 22,  -8),   S( -4,  -8),   S( 16, -10),   S( 31,  -5),   S( 12,  -4),   S(  6,  -9),   S(  5,  12),
            S(  3,  41),   S(-11,   0),   S( 19,  -8),   S(  7,  -9),   S( 12, -26),   S( -5, -18),   S(-13,   0),   S(  2,  -2),
            S( -4,   6),   S( 16, -20),   S(  3,  10),   S( 10,  -8),   S( 19,  -1),   S(  4,   4),   S(  8, -29),   S( -5, -30),
            S(  3,  -4),   S( -1,  -3),   S( -8, -10),   S(  8,  -8),   S(  0,   9),   S( -6, -29),   S(  7,  -5),   S( 13,  43),

            /* kings: bucket 12 */
            S(  0,   0),   S(  0,   0),   S(-11,  55),   S(-13,  22),   S(  0,  41),   S(-11,  -1),   S(  2,   0),   S(  5,  70),
            S(  0,   0),   S(  0,   0),   S( 29, 106),   S( 14,  -1),   S( 15,  21),   S( -3,  -4),   S( 38,  27),   S(-21,  10),
            S(  2,  -6),   S(  1, -52),   S( 18,  30),   S( 23,  26),   S(-13, -19),   S(  4,   5),   S(-19,  -3),   S(-15,  -6),
            S(  3,   2),   S(  6,  17),   S(  4,  -4),   S(  5, -60),   S( 11,  -7),   S(  1, -24),   S(-22,  -3),   S(  2,  20),
            S( 10,  34),   S(  2,  -9),   S( -1,  -7),   S( 12,  -6),   S( -5,  -6),   S(  5,  19),   S(-13,  -4),   S(-12,   0),
            S(  7,  19),   S( -1,   5),   S( -6,  -4),   S( -4,   4),   S( -8, -32),   S(  0,  30),   S( -1,  -9),   S( 13,  34),
            S(  7,  16),   S( -7,  -3),   S( -5,   4),   S(  5,  15),   S(-15, -38),   S(  2, -43),   S( 11,  19),   S( 19,  28),
            S( -5, -12),   S(  5,  -1),   S( -3, -33),   S( -4, -38),   S(  1, -21),   S(  0,  22),   S( -4, -34),   S( -4,  -3),

            /* kings: bucket 13 */
            S(-22,  93),   S( -8,  86),   S(  0,   0),   S(  0,   0),   S( -7,  77),   S(-17,   6),   S(  1,  12),   S(-16,  44),
            S(-35,  -5),   S(  0,  22),   S(  0,   0),   S(  0,   0),   S( 11,   4),   S( -2, -10),   S(  0,  24),   S(-15,  38),
            S( -2,  18),   S( 13,  47),   S(  4, -11),   S(  5, -14),   S(  8,   3),   S(  5,   4),   S( -6,  10),   S(-16,  10),
            S(-14, -38),   S( -8, -15),   S(  4, -12),   S( -1, -47),   S(  6, -24),   S(  9,  -9),   S( -7,  20),   S( -9, -26),
            S( -2,  -5),   S( -4,   3),   S( -1,  15),   S( -5, -30),   S(  1,  -9),   S( -3,   3),   S(-12,  -7),   S( -5,  15),
            S( 11,  41),   S(-11,  -7),   S( -8,  13),   S(  1,  17),   S( -4,  13),   S(-11, -32),   S( -1, -16),   S(  5,  23),
            S(  6,  23),   S(-11,  -7),   S(-14,  -9),   S(  7,  -5),   S(  2, -26),   S(  3, -41),   S( -4, -51),   S( 12,  35),
            S(  3,   8),   S( 10,  29),   S(  8,  22),   S(  0,  -7),   S(  1,  -3),   S(  1,  16),   S( -2, -18),   S(  8,  25),

            /* kings: bucket 14 */
            S( 12,  61),   S(-20,   6),   S( -9,  18),   S( -8,  12),   S(  0,   0),   S(  0,   0),   S(-25,  50),   S(-80,  86),
            S(-20,   5),   S(-15,   2),   S( -6, -14),   S( 15,  -5),   S(  0,   0),   S(  0,   0),   S( 33,  12),   S(-37,  30),
            S( -4,  21),   S(  2, -12),   S( 11, -13),   S(  1,  12),   S(  9, -24),   S(  9,  37),   S( -1,  40),   S(-11,  -3),
            S( 16,  18),   S(  8, -13),   S( -5, -20),   S(  8, -46),   S(-14, -41),   S( 21,  -3),   S( -7,   0),   S( -2,   6),
            S(  1,  17),   S( 12,   9),   S(  1,   9),   S(-10,  -3),   S(-10,  -4),   S(  8,   9),   S(  4,  30),   S( -2,  14),
            S(  5,   8),   S( -7,  16),   S(  0,  -6),   S( -2,  22),   S(-13,   0),   S(  0,  -3),   S(-15, -37),   S( -4,  11),
            S( 10,  29),   S( -2, -15),   S( -3,  33),   S( -4,  30),   S(  3,  15),   S( -5, -39),   S( -4, -53),   S( 10,  64),
            S( -1,   0),   S(  5,  66),   S(  0,  10),   S( -1, -19),   S(  3,  22),   S( -1,   2),   S(-12, -58),   S(  0, -25),

            /* kings: bucket 15 */
            S(-11,  40),   S( -9,   4),   S( -4,  -2),   S(  6,  20),   S(-62,  25),   S(-29,  85),   S(  0,   0),   S(  0,   0),
            S( -6, -21),   S( -4,  -6),   S(  7,   7),   S( 30,  27),   S( 46, -18),   S( 42,  96),   S(  0,   0),   S(  0,   0),
            S(-17, -25),   S( 20,  15),   S(  3, -10),   S( 11,  -3),   S( 35,   3),   S( 34,  36),   S(  7,  -5),   S(-10, -25),
            S( -1,  16),   S( -8,   7),   S( -1,   4),   S( -4, -16),   S( -2, -30),   S(  2,  -7),   S(  1,  52),   S(  3, -10),
            S(  2,  -3),   S( -1,  20),   S( -2, -17),   S( -7, -30),   S( -8, -12),   S( -1,  17),   S( -3,  24),   S( -1,   6),
            S(-14,   2),   S(-19,   9),   S( -4,  -3),   S(  4,  22),   S( -9, -20),   S( -8, -10),   S( -7,   8),   S(-12, -19),
            S(  2,  11),   S(-14, -15),   S(-10,  -2),   S( -7,   5),   S( 12,  27),   S( -9,   2),   S(-14, -39),   S(  9,  26),
            S( -7, -13),   S( -3,   8),   S(  0,   1),   S(  3,   7),   S(  5,  38),   S( -2,   7),   S(  4,  17),   S(  0,  -3),

            #endregion

            /* mobility weights */
            S(  7,   8),    // knights
            S(  5,   4),    // bishops
            S(  3,   3),    // rooks
            S(  1,   4),    // queens

            /* trapped pieces */
            S(-13, -221),   // knights
            S(  3, -130),   // bishops
            S(  3, -99),    // rooks
            S( 18, -92),    // queens

            /* center control */
            S(  2,   7),    // D0
            S(  3,   5),    // D1

            /* squares attacked near enemy king */
            S( 17,  -1),    // attacks to squares 1 from king
            S( 16,   2),    // attacks to squares 2 from king
            S(  6,   2),    // attacks to squares 3 from king

            /* pawn shield/king safety */
            S(  8,  18),    // friendly pawns 1 from king
            S( -1,  19),    // friendly pawns 2 from king
            S(  0,  11),    // friendly pawns 3 from king

            /* castling right available */
            S( 43, -19),

            /* castling complete */
            S(  8,  -9),

            /* king on open file */
            S(-57,  12),

            /* king on half-open file */
            S(-21,  24),

            /* king on open diagonal */
            S(-13,  14),

            /* king attack square open */
            S( -9,  -2),

            /* isolated pawns */
            S(  0, -25),

            /* doubled pawns */
            S(-16, -35),

            /* backward pawns */
            S( 11, -24),

            /* adjacent/phalanx pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 11,  14),   S(  3,   6),   S(  7,   4),   S(  7,  43),   S( 30,  35),   S( -3, -16),   S(-17,  38),   S( -4, -13),
            S(  1,  16),   S( 23,  -2),   S(  6,  38),   S( 16,  42),   S( 44,   6),   S( -6,  35),   S( 26,  -6),   S( -7,  10),
            S(-19,  28),   S( 18,  20),   S( -4,  52),   S( 23,  76),   S( 26,  28),   S( 23,  46),   S( 31,  -5),   S(  5,  39),
            S( -2,  57),   S( 18,  55),   S( 31,  83),   S(  5,  91),   S( 72,  77),   S( 56,  37),   S( 29,  72),   S(  1,  40),
            S( 65,  85),   S(123, 104),   S( 94, 124),   S(129, 153),   S(119,  97),   S(144, 122),   S(202,  92),   S( 93,  34),
            S( 85, 239),   S(125, 313),   S(114, 248),   S(131, 259),   S( 91, 191),   S( 73, 190),   S( 66, 221),   S( 21, 120),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -2,  13),   S(-14,  26),   S(-36,  35),   S(-37,  57),   S( -5,  16),   S(-27,  21),   S(-15,  55),   S( 22,  13),
            S(  9,  27),   S( -9,  42),   S(-24,  39),   S( -7,  26),   S(-24,  38),   S(-54,  51),   S(-53,  63),   S( 16,  24),
            S( -4,  43),   S( -9,  47),   S(-18,  41),   S( 15,  31),   S( -7,  37),   S(-41,  53),   S(-67,  85),   S(-26,  53),
            S( 17,  72),   S( 48,  75),   S( 19,  66),   S( 11,  57),   S( 13,  68),   S( 20,  76),   S( -6,  94),   S(-61, 113),
            S( 20, 111),   S(101, 136),   S( 83, 107),   S( 50,  95),   S(-20,  86),   S( 66,  96),   S( 17, 148),   S(-60, 128),
            S(237, 124),   S(230, 174),   S(249, 178),   S(263, 180),   S(249, 191),   S(222, 196),   S(235, 197),   S(276, 163),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* pawn rams */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 36,  22),   S(  2,  15),   S( 11,  24),   S( -9,  64),   S( 58,  39),   S( 26,   8),   S( -2,   1),   S( 43,  13),
            S(  4,  14),   S(  3,  11),   S( 16,  15),   S( 17,  28),   S( 13,  15),   S( -3,  12),   S(  3,   8),   S( 28,  -3),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( -4, -14),   S( -3, -11),   S(-16, -15),   S(-17, -28),   S(-13, -15),   S(  3, -12),   S( -3,  -8),   S(-28,   3),
            S(-36, -22),   S( -2, -15),   S(-11, -24),   S(  9, -64),   S(-58, -39),   S(-26,  -8),   S(  2,  -1),   S(-43, -13),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* supported pawn chain */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S( 30,   3),   S( 34,  14),   S( 52,  19),   S( 52,  11),   S( 38,  23),   S( 39,  11),   S( 19,   8),   S( 44,  -5),
            S(  2,   8),   S( 20,  21),   S( 21,  18),   S( 26,  36),   S( 26,  18),   S( 13,  16),   S( 29,   6),   S( 17,  -2),
            S( -3,  17),   S( 16,  37),   S( 47,  41),   S( 42,  44),   S( 51,  33),   S( 53,  15),   S( 22,  27),   S( 31,   2),
            S( 33,  70),   S( 91,  47),   S(102,  83),   S(117,  91),   S(132,  74),   S( 81,  70),   S( 74,  41),   S( 68,  14),
            S( 52,  97),   S(157,  97),   S(201, 157),   S(124, 118),   S(200, 177),   S(148, 111),   S(178, 101),   S(-40,  99),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* passed pawn can advance */
            S( -9,  22),   S( -7,  50),   S( 10,  92),   S( 18, 203),

            /* enemy king outside passed pawn square */
            S( -5, 200),

            /* passed pawn/friendly king distance penalty */
            S( -2, -19),

            /* passed pawn/enemy king distance bonus */
            S(  4,  25),

            /* blocked passed pawn */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // blocked by pawns
            S(  0,   0),   S( 86, -51),   S( 41,   7),   S( 42,  15),   S( 48,  40),   S( 50,  12),   S(194, -24),   S(  0,   0),    // blocked by knights
            S(  0,   0),   S( 15,  -5),   S( 23,  51),   S(  7,  44),   S( 13,  74),   S( 38,  88),   S(164,  99),   S(  0,   0),    // blocked by bishops
            S(  0,   0),   S(-38,  -4),   S( -4, -39),   S( -4, -36),   S(-22, -18),   S(  7, -34),   S(209, -87),   S(  0,   0),    // blocked by rooks
            S(  0,   0),   S( 36, -52),   S( 43, -54),   S( -1,   6),   S(  3, -37),   S(  0, -150),  S( 23, -275),  S(  0,   0),    // blocked by queens
            S(  0,   0),   S(-10,  25),   S( 22,  10),   S( 55, -23),   S(-30, -15),   S(205,  18),   S(239, -16),   S(  0,   0),    // blocked by kings

            /* rook behind passed pawn */
            S(  3,  44),

            /* knight on outpost */
            S(  0,  31),

            /* bishop on outpost */
            S( 12,  33),

            /* bishop pair */
            S( 37, 102),

            /* bad bishop pawns */
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),
            S(  0,  -6),   S( -4,  -7),   S( -4, -11),   S( -3, -27),   S(  0, -26),   S(-17,  -3),   S(-23,  -6),   S( -4,  -2),
            S( -4, -11),   S( -8, -11),   S(-12, -16),   S( -7, -20),   S(-11, -26),   S(-14, -12),   S(-12, -10),   S( -4,  -7),
            S( -1, -12),   S(  5, -36),   S( -2, -40),   S( -7, -53),   S(-18, -35),   S(-14, -26),   S(-18, -17),   S( -6,  -7),
            S( 13, -33),   S( 14, -39),   S( -3, -32),   S( -7, -42),   S(-14, -30),   S(-11, -26),   S( -3, -31),   S( -2, -24),
            S( 26, -31),   S( 33, -59),   S( 34, -58),   S(  8, -42),   S(  6, -40),   S(  3, -47),   S(  7, -66),   S(  5, -28),
            S( 46, -29),   S( 64, -74),   S( 88, -92),   S( 41, -85),   S( 45, -83),   S( 84, -84),   S( 81, -117),  S( 56, -76),
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),

            /* rook on open file */
            S( 40,  -1),

            /* rook on half-open file */
            S( 11,  35),

            /* rook on seventh rank */
            S(-10,  43),

            /* doubled rooks on file */
            S( 24,  23),

            /* queen on open file */
            S(-12,  38),

            /* queen on half-open file */
            S(  5,  35),

            /* pawn push threats */
            S(  0,   0),   S( 26,  33),   S( 28, -12),   S( 32,  24),   S( 29,  -9),   S(  0,   0),    // Pawn push threats

            /* piece threats */
            /*  Pawn          Knight         Bishop          Rook          Queen           King */
            S(  0,   0),   S( 64, 104),   S( 55, 106),   S( 70,  74),   S( 53,  33),   S(  0,   0),    // Pawn threats
            S(  0,   0),   S(-11,  10),   S( 50,  39),   S( 87,  10),   S( 40,  27),   S(  0,   0),    // Knight threats
            S(  0,   0),   S( 27,  72),   S(  0,  28),   S( 61,  61),   S( 43,  90),   S(  0,   0),    // Bishop threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S( -9,  50),   S( 59,  54),   S(  0,   0),    // Rook threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(-17,  23),   S(  0,   0),    // Queen threats
            S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),   S(  0,   0),    // King threats

            /* tempo bonus for side to move */
            S( 16,  10),
        };
    }
}
