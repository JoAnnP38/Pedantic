using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Genetics;
using LiteDB;
using Pedantic.Collections;
using Pedantic.Utilities;
using System.Runtime.CompilerServices;
using static System.Formats.Asn1.AsnWriter;
using System.Drawing;
using System.Reflection.Emit;

namespace Pedantic.Chess
{

    public sealed class Evaluation
    {
        public const ulong QUEEN_SIDE_MASK = 0x0F0F0F0F0F0F0F0Ful;
        public const ulong KING_SIDE_MASK = 0xF0F0F0F0F0F0F0F0ul;

        public enum GamePhase
        {
            Opening,
            MidGame,
            EndGame
        }

        static Evaluation()
        {
            LoadWeights();
        }

        public static bool Debug { get; set; } = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCheckmate(int score)
        {
            int absValue = Math.Abs(score);
            return absValue >= Constants.CHECKMATE_SCORE - Constants.MAX_PLY &&
                   absValue <= Constants.CHECKMATE_SCORE;
        }

        public static void LoadWeights(string? id = null)
        {
            using var rep = new GeneticsRepository();
            if (id == null)
            {
                weights = rep.Weights.FindOne(w => w.IsActive && w.IsImmortal);
            }
            else
            {
                try
                {
                    weights = rep.Weights.FindById(id);
                }
                catch (Exception e)
                {
                    Util.TraceError($"Unexpected exception in Evaluation static ctor: {e.Message}.");
                    weights = ChessWeights.CreateParagon();
                }
            }

            Array.Copy(weights.Weights, ChessWeights.OPENING_PIECE_WEIGHT_OFFSET, openingPieceValues, 0,
                Constants.MAX_PIECES - 1);

            Array.Copy(weights.Weights, ChessWeights.ENDGAME_PIECE_WEIGHT_OFFSET, endGamePieceValues, 0,
                Constants.MAX_PIECES - 1);

            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                for (int rank = 1; rank < 7; rank++)
                {
                    int opIndex = ChessWeights.OPENING_BLOCKED_PAWN_OFFSET + (rank - 1) + (pc * Constants.MAX_PIECES);
                    int egIndex = ChessWeights.ENDGAME_BLOCKED_PAWN_OFFSET + (rank - 1) + (pc * Constants.MAX_PIECES);
                    openingBlockedPawns[pc][rank] = weights.Weights[opIndex];
                    endGameBlockedPawns[pc][rank] = weights.Weights[egIndex];
                }
            }

            Array.Copy(weights.Weights, ChessWeights.OPENING_PASSED_PAWNS_OFFSET, openingPassedPawns, 1, 6);
            Array.Copy(weights.Weights, ChessWeights.ENDGAME_PASSED_PAWNS_OFFSET, endGamePassedPawns, 1, 6);
            Array.Copy(weights.Weights, ChessWeights.OPENING_ADJACENT_PAWNS_OFFSET, openingAdjacentPawns, 1, 6);
            Array.Copy(weights.Weights, ChessWeights.ENDGAME_ADJACENT_PAWNS_OFFSET, endGameAdjacentPawns, 1, 6);

            opKingAttack = new(weights.Weights, ChessWeights.OPENING_KING_ATTACK_WEIGHT_OFFSET, 3);
            egKingAttack = new(weights.Weights, ChessWeights.ENDGAME_KING_ATTACK_WEIGHT_OFFSET, 3);
            opPieceValue = new(openingPieceValues);
            egPieceValue = new(endGamePieceValues);
            openingPst = new(Constants.MAX_PIECES, Constants.MAX_SQUARES, weights.Weights, ChessWeights.OPENING_PIECE_SQUARE_WEIGHT_OFFSET);
            endgamePst = new(Constants.MAX_PIECES, Constants.MAX_SQUARES, weights.Weights, ChessWeights.ENDGAME_PIECE_SQUARE_WEIGHT_OFFSET);
            opPinned = new(3, Constants.MAX_PIECES - 1, weights.Weights, ChessWeights.OPENING_PINNED_PIECE_OFFSET);
            egPinned = new(3, Constants.MAX_PIECES - 1, weights.Weights, ChessWeights.ENDGAME_PINNED_PIECE_OFFSET);
            opPassedPawn = new(openingPassedPawns);
            egPassedPawn = new(endGamePassedPawns);
            opAdjPawn = new(openingAdjacentPawns);
            egAdjPawn = new(endGameAdjacentPawns);
        }

        public short Compute(Board board)
        {
            if (TtEval.TryGetScore(board.Hash, out short score))
            {
                return score;
            }

            Span<short> opScore = stackalloc short[2];
            opScore.Clear();

            Span<short> egScore = stackalloc short[2];
            egScore.Clear();

            GetGamePhase(board, out GamePhase gamePhase, out int opWt, out int egWt);
            CalculateDevelopment(board, opScore, egScore);
            CalculateKingAttacks(board, opScore, egScore);
            CalculatePawns(board, opScore, egScore);
            CalculateMisc(board, opScore, egScore);

            for (Color color = Color.White; color <= Color.Black; ++color)
            {
                int n = (int)color;
                opScore[n] += (short)(board.OpeningMaterial[n] + board.OpeningPieceSquare[n]);
                egScore[n] += (short)(board.EndGameMaterial[n] + board.EndGamePieceSquare[n]);
                /* put this back in when optimization starts
                short mobility = board.GetPieceMobility(color);
                opScore[n] += (short)(mobility * OpeningMobilityWeight);
                egScore[n] += (short)(mobility * EndGameMobilityWeight);*/
            }

            score = (short)((((opScore[0] - opScore[1]) * opWt) >> 7 /* / 128 */) + 
                            (((egScore[0] - egScore[1]) * egWt) >> 7 /* / 128 */));

            score = board.SideToMove == Color.White ? (short)score : (short)-score;

            TtEval.Add(board.Hash, score);
            return score;
        }

        public short ComputeMobility(Board board)
        {
            Span<short> opScore = stackalloc short[2];
            opScore.Clear();

            Span<short> egScore = stackalloc short[2];
            egScore.Clear();

            GetGamePhase(board, out GamePhase gamePhase, out int opWt, out int egWt);
            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int n = (int)color;
                opScore[n] = (short)(board.GetPieceMobility(color) * OpeningMobilityWeight);
                egScore[n] = (short)(board.GetPieceMobility(color) * EndGameMobilityWeight);
            }

            short score = (short)((((opScore[0] - opScore[1]) * opWt) >> 7 /* / 128 */) +
                                 (((egScore[0] - egScore[1]) * egWt) >> 7 /* / 128 */));

            return score;
        }

        public void CalculatePawns(Board board, Span<short> opScores, Span<short> egScores)
        {
            if (TtPawnEval.TryLookup(board.PawnHash, out TtPawnEval.TtPawnItem item))
            {
                opScores[0] += item.GetOpeningScore(Color.White);
                opScores[1] += item.GetOpeningScore(Color.Black);
                egScores[0] += item.GetEndGameScore(Color.White);
                egScores[1] += item.GetEndGameScore(Color.Black);
            }
            else
            {
                CalculateHashablePawns(board, opScores, egScores);
            }

            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int n = (int)color;
                ulong pawns = board.Pieces(color, Piece.Pawn);
                if (BitOps.PopCount(pawns) == 0)
                {
                    continue;
                }

                ulong bb1, bb2;
                if (color == Color.White)
                {
                    bb1 = pawns & (board.Units(color) >> 8);
                    bb2 = BitOps.AndNot(pawns, board.All >> 8) & Board.MaskRanks[Index.A2];
                    bb2 &= board.Units(color) >> 16;
                }
                else
                {
                    bb1 = pawns & (board.Units(color) << 8);
                    bb2 = BitOps.AndNot(pawns, board.All << 8) & Board.MaskRanks[Index.A7];
                    bb2 &= board.Units(color) << 16;
                }

                for (; bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int square = BitOps.TzCount(bb1);
                    int normalSq = Index.NormalizedIndex[n][square];
                    int normalRank = Index.GetRank(normalSq);
                    int blockerSquare = square + pawnOffset[n];
                    Piece piece = board.PieceBoard[blockerSquare].Piece;
                    opScores[n] += OpeningBlockedPawnTable[(int)piece][normalRank];
                    egScores[n] += EndGameBlockedPawnTable[(int)piece][normalRank];
                }

                int count = BitOps.PopCount(bb2);
                opScores[n] += (short)(count * OpeningBlockedPawnDoubleMove);
                egScores[n] += (short)(count * EndGameBlockedPawnDoubleMove);
            }
        }

        public void CalculateHashablePawns(Board board, Span<short> opScores, Span<short> egScores)
        {
            Span<short> openingScores = stackalloc short[2];
            openingScores.Clear();

            Span<short> endgameScores = stackalloc short[2];
            openingScores.Clear();

            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int n = (int)color;

                ulong pawns = board.Pieces(color, Piece.Pawn);
                Color other = (Color)(n ^ 1);
                ulong otherPawns = board.Pieces(other, Piece.Pawn);

                if (BitOps.PopCount(pawns & QUEEN_SIDE_MASK) > BitOps.PopCount(otherPawns & QUEEN_SIDE_MASK))
                {
                    openingScores[n] += OpeningPawnMajority;
                    endgameScores[n] += EndGamePawnMajority;
                }

                if (BitOps.PopCount(pawns & KING_SIDE_MASK) > BitOps.PopCount(otherPawns & KING_SIDE_MASK))
                {
                    openingScores[n] += OpeningPawnMajority;
                    endgameScores[n] += EndGamePawnMajority;
                }

                for (ulong p = pawns; p != 0; p = BitOps.ResetLsb(p))
                {
                    int sq = BitOps.TzCount(p);
                    int normalSq = Index.NormalizedIndex[n][sq];
                    int normalRank = Index.GetRank(normalSq);

                    if ((otherPawns & passedPawnMasks[n][sq]) == 0ul)
                    {
                        openingScores[n] += OpeningPassedPawn[normalRank];
                        endgameScores[n] += EndGamePassedPawn[normalRank];
                    }

                    if ((pawns & isolatedPawnMasks[sq]) == 0)
                    {
                        openingScores[n] += OpeningIsolatedPawn;
                        endgameScores[n] += EndGameIsolatedPawn;
                    }

                    if ((pawns & backwardPawnMasks[n][sq]) == 0)
                    {
                        openingScores[n] += OpeningBackwardPawn;
                        endgameScores[n] += EndGameBackwardPawn;
                    }

                    if ((pawns & adjacentPawnMasks[sq]) != 0)
                    {
                        openingScores[n] += OpeningAdjacentPawn[normalRank];
                        endgameScores[n] += EndGameAdjacentPawn[normalRank];
                    }
                }

                for (int file = 0; file <= Constants.MAX_COORDS; file++)
                {
                    if (BitOps.PopCount(pawns & Board.MaskFiles[file]) > 1)
                    {
                        openingScores[n] += OpeningDoubledPawn;
                        endgameScores[n] += EndGameDoubledPawn;

                    }
                }
            }

            TtPawnEval.Add(board.PawnHash, openingScores, endgameScores);
            opScores[0] += openingScores[0];
            opScores[1] += openingScores[1];
            egScores[0] += endgameScores[0];
            egScores[1] += endgameScores[1];
        }
        
        public static void CalculateDevelopment(Board board, Span<short> opScores, Span<short> egScores)
        {
            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int n = (int)color;
                CalcDevelopmentParameters(board, color, out int d, out int u, out int k, out int c);
                opScores[n] += (short)((d - u - (k * c)) * OpeningDevelopmentWeight);
                egScores[n] += (short)((d - u - (k * c)) * EndGameDevelopmentWeight);
            }
        }

        public static void CalculateKingAttacks(Board board, Span<short> opScores, Span<short> egScores)
        {
            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int n = (int)color;
                CalcKingProximityAttacks(board, color, out int d1, out int d2, out int d3);
                opScores[n] +=
                    (short)(d1 * OpeningKingAttack[0] + d2 * OpeningKingAttack[1] + d3 * OpeningKingAttack[2]);
                egScores[n] +=
                    (short)(d1 * EndGameKingAttack[0] + d2 * EndGameKingAttack[1] + d3 * EndGameKingAttack[2]);
            }
        }

        public static void CalculateMisc(Board board, Span<short> opScores, Span<short> egScores)
        {
            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int n = (int)color;

                int count = BitOps.PopCount(board.Pieces(color, Piece.Bishop));
                if (count >= 2)
                {
                    opScores[n] += OpeningBishopPair;
                    egScores[n] += EndGameBishopPair;
                }
            }
        }

        public static void GetGamePhase(Board board, out GamePhase gamePhase, out int opWt, out int egWt)
        {
            if (board.FullMoveCounter <= Evaluation.OpeningPhaseThruTurn)
            {
                gamePhase = GamePhase.Opening;
                opWt = 128;
                egWt = 0;
            }
            else if (board.Material[(int)Color.White] + board.Material[(int)Color.Black] <=
                     Evaluation.EndGamePhaseMaterial)
            {
                gamePhase = GamePhase.EndGame;
                opWt = 0;
                egWt = 128;
            }
            else
            {
                gamePhase = GamePhase.MidGame;
                int totMaterial = Constants.TOTAL_STARTING_MATERIAL - Evaluation.EndGamePhaseMaterial;
                int curMaterial = board.Material[(int)Color.White] + board.Material[(int)Color.Black] -
                                            Evaluation.EndGamePhaseMaterial;
                opWt = (curMaterial * 128) / totMaterial;
                egWt = 128 - opWt;
            }
        }

        public static void CalcDevelopmentParameters(Board board, Color color, out int d, out int u, out int k, out int c)
        {
            Color other = (Color)((int)color ^ 1);
            u = 0;
            k = 0;

            int notMoved = 0;
            ulong bb = board.Pieces(color, Piece.Knight) & homeLocations[(int)color][(int)Piece.Knight];
            notMoved += BitOps.PopCount(bb);

            bb = board.Pieces(color, Piece.Bishop) & homeLocations[(int)color][(int)Piece.Bishop];
            notMoved += BitOps.PopCount(bb);

            d = (Constants.MINOR_PIECE_COUNT - notMoved) * 4;

            bb = board.Pieces(color, Piece.Rook) & homeLocations[(int)color][(int)Piece.Rook];
            notMoved += BitOps.PopCount(bb);

            ulong queenLocation = board.Pieces(color, Piece.Queen);
            if (queenLocation != 0 && (queenLocation & homeLocations[(int)color][(int)Piece.Queen]) == 0)
            {
                u = notMoved * 3;
            }

            if (board.Pieces(other, Piece.Queen) != 0)
            {
                c = 8;
            }
            else
            {
                ulong opposing = board.Pieces(other, Piece.Rook) | board.Pieces(other, Piece.Bishop) |
                                 board.Pieces(other, Piece.Knight);
                c = 4 - (6 - BitOps.PopCount(opposing));
            }

            if (!board.Castled[(int)color])
            {
                var info = castlingRights[(int)color];
                int castling = (int)(board.Castling & info.mask);
                castling >>= info.shift;
                k = castling switch { 2 => 2, 1 => 1, 0 => 3, _ => 0 };
            }
        }

        public static void CalcKingProximityAttacks(Board board, Color color, out int d1, out int d2, out int d3)
        {
            Color other = (Color)((int)color ^ 1);
            int kingSq = BitOps.TzCount(board.Pieces(other, Piece.King));
            d1 = BitOps.PopCount(board.Units(color) & kingProximity[0][kingSq]);
            d2 = BitOps.PopCount(board.Units(color) & kingProximity[1][kingSq]);
            d3 = BitOps.PopCount(board.Units(color) & kingProximity[2][kingSq]);
        }

        private static readonly short[] sign = { 1, -1 };
        public static readonly short[] CanonicalPieceValues = { 100, 300, 300, 500, 900, 0 };
        public static short OpeningPhaseThruTurn => weights.Weights[ChessWeights.OPENING_PHASE_WEIGHT_OFFSET];
        public static short EndGamePhaseMaterial => weights.Weights[ChessWeights.ENDGAME_PHASE_WEIGHT_OFFSET];
        public static short OpeningMobilityWeight => weights.Weights[ChessWeights.OPENING_MOBILITY_WEIGHT_OFFSET];
        public static short EndGameMobilityWeight => weights.Weights[ChessWeights.ENDGAME_MOBILITY_WEIGHT_OFFSET];

        public static ReadOnlySpan<short> OpeningKingAttack => opKingAttack.Span;
        public static ReadOnlySpan<short> EndGameKingAttack => egKingAttack.Span;
        public static short OpeningDevelopmentWeight => weights.Weights[ChessWeights.OPENING_DEVELOPMENT_WEIGHT_OFFSET];
        public static short EndGameDevelopmentWeight => weights.Weights[ChessWeights.ENDGAME_DEVELOPMENT_WEIGHT_OFFSET];

        public static ReadOnlySpan<short> OpeningPieceValues => opPieceValue.Span;
        public static ReadOnlySpan<short> EndGamePieceValues => egPieceValue.Span;
        public static ReadOnlyArray2D<short> OpeningPieceSquareTable => openingPst;
        public static ReadOnlyArray2D<short> EndGamePieceSquareTable => endgamePst;
        public static short[][] OpeningBlockedPawnTable => openingBlockedPawns;
        public static short[][] EndGameBlockedPawnTable => endGameBlockedPawns;
        public static short OpeningBlockedPawnDoubleMove => weights.Weights[ChessWeights.OPENING_BLOCKED_PAWN_DBL_MOVE_OFFSET];
        public static short EndGameBlockedPawnDoubleMove => weights.Weights[ChessWeights.ENDGAME_BLOCKED_PAWN_DBL_MOVE_OFFSET];
        public static ReadOnlyArray2D<short> OpeningPinnedPiece => opPinned;
        public static ReadOnlyArray2D<short> EndGamePinnedPiece => egPinned;
        public static short OpeningIsolatedPawn => weights.Weights[ChessWeights.OPENING_ISOLATED_PAWN_OFFSET];
        public static short EndGameIsolatedPawn => weights.Weights[ChessWeights.ENDGAME_ISOLATED_PAWN_OFFSET];
        public static short OpeningBackwardPawn => weights.Weights[ChessWeights.OPENING_BACKWARD_PAWN_OFFSET];
        public static short EndGameBackwardPawn => weights.Weights[ChessWeights.ENDGAME_BACKWARD_PAWN_OFFSET];
        public static short OpeningDoubledPawn => weights.Weights[ChessWeights.OPENING_DOUBLED_PAWN_OFFSET];
        public static short EndGameDoubledPawn => weights.Weights[ChessWeights.ENDGAME_DOUBLED_PAWN_OFFSET];
        public static short OpeningKnightOutpost => weights.Weights[ChessWeights.OPENING_KNIGHT_OUTPOST_OFFSET];
        public static short EndGameKnightOutpost => weights.Weights[ChessWeights.ENDGAME_KNIGHT_OUTPOST_OFFSET];
        public static short OpeningBishopOutpost => weights.Weights[ChessWeights.OPENING_BISHOP_OUTPOST_OFFSET];
        public static short EndGameBishopOutpost => weights.Weights[ChessWeights.ENDGAME_BISHOP_OUTPOST_OFFSET];
        public static ReadOnlySpan<short> OpeningPassedPawn => opPassedPawn.Span;
        public static ReadOnlySpan<short> EndGamePassedPawn => egPassedPawn.Span;
        public static ReadOnlySpan<short> OpeningAdjacentPawn => opAdjPawn.Span;
        public static ReadOnlySpan<short> EndGameAdjacentPawn => egAdjPawn.Span;
        public static short OpeningBishopPair => weights.Weights[ChessWeights.OPENING_BISHOP_PAIR_OFFSET];
        public static short EndGameBishopPair => weights.Weights[ChessWeights.ENDGAME_BISHOP_PAIR_OFFSET];
        public static short OpeningPawnMajority => weights.Weights[ChessWeights.OPENING_PAWN_MAJORITY_OFFSET];
        public static short EndGamePawnMajority => weights.Weights[ChessWeights.ENDGAME_PAWN_MAJORITY_OFFSET];

        private static ChessWeights weights = ChessWeights.Empty;
        private static ReadOnlyMemory<short> opKingAttack = new(Array.Empty<short>());
        private static ReadOnlyMemory<short> egKingAttack = new(Array.Empty<short>());
        private static ReadOnlyMemory<short> opPieceValue = new(Array.Empty<short>());
        private static ReadOnlyMemory<short> egPieceValue = new(Array.Empty<short>());
        private static ReadOnlyMemory<short> opPassedPawn = new(Array.Empty<short>());
        private static ReadOnlyMemory<short> egPassedPawn = new(Array.Empty<short>());
        private static ReadOnlyMemory<short> opAdjPawn = new(Array.Empty<short>());
        private static ReadOnlyMemory<short> egAdjPawn = new(Array.Empty<short>());

        private static ReadOnlyArray2D<short> openingPst = ReadOnlyArray2D<short>.Empty;
        private static ReadOnlyArray2D<short> endgamePst = ReadOnlyArray2D<short>.Empty;
        private static ReadOnlyArray2D<short> opPinned = ReadOnlyArray2D<short>.Empty;
        private static ReadOnlyArray2D<short> egPinned = ReadOnlyArray2D<short>.Empty;

        private static readonly short[] openingPieceValues = new short[Constants.MAX_PIECES];
        private static readonly short[] endGamePieceValues = new short[Constants.MAX_PIECES];
        private static readonly short[][] openingBlockedPawns = Mem.Allocate2D<short>(Constants.MAX_PIECES, Constants.MAX_COORDS);
        private static readonly short[][] endGameBlockedPawns = Mem.Allocate2D<short>(Constants.MAX_PIECES, Constants.MAX_COORDS);
        private static readonly short[] openingPassedPawns = new short[Constants.MAX_COORDS];
        private static readonly short[] endGamePassedPawns = new short[Constants.MAX_COORDS];
        private static readonly short[] openingAdjacentPawns = new short[Constants.MAX_COORDS];
        private static readonly short[] endGameAdjacentPawns = new short[Constants.MAX_COORDS];


        private static readonly ulong[][] passedPawnMasks =
        {
            #region passedPawnMasks data
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0003030303030000ul, 0x0007070707070000ul, 0x000E0E0E0E0E0000ul, 0x001C1C1C1C1C0000ul,
                0x0038383838380000ul, 0x0070707070700000ul, 0x00E0E0E0E0E00000ul, 0x00C0C0C0C0C00000ul,
                0x0003030303000000ul, 0x0007070707000000ul, 0x000E0E0E0E000000ul, 0x001C1C1C1C000000ul,
                0x0038383838000000ul, 0x0070707070000000ul, 0x00E0E0E0E0000000ul, 0x00C0C0C0C0000000ul,
                0x0003030300000000ul, 0x0007070700000000ul, 0x000E0E0E00000000ul, 0x001C1C1C00000000ul,
                0x0038383800000000ul, 0x0070707000000000ul, 0x00E0E0E000000000ul, 0x00C0C0C000000000ul,
                0x0003030000000000ul, 0x0007070000000000ul, 0x000E0E0000000000ul, 0x001C1C0000000000ul,
                0x0038380000000000ul, 0x0070700000000000ul, 0x00E0E00000000000ul, 0x00C0C00000000000ul,
                0x0003000000000000ul, 0x0007000000000000ul, 0x000E000000000000ul, 0x001C000000000000ul,
                0x0038000000000000ul, 0x0070000000000000ul, 0x00E0000000000000ul, 0x00C0000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            },
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000300ul, 0x0000000000000700ul, 0x0000000000000E00ul, 0x0000000000001C00ul,
                0x0000000000003800ul, 0x0000000000007000ul, 0x000000000000E000ul, 0x000000000000C000ul,
                0x0000000000030300ul, 0x0000000000070700ul, 0x00000000000E0E00ul, 0x00000000001C1C00ul,
                0x0000000000383800ul, 0x0000000000707000ul, 0x0000000000E0E000ul, 0x0000000000C0C000ul,
                0x0000000003030300ul, 0x0000000007070700ul, 0x000000000E0E0E00ul, 0x000000001C1C1C00ul,
                0x0000000038383800ul, 0x0000000070707000ul, 0x00000000E0E0E000ul, 0x00000000C0C0C000ul,
                0x0000000303030300ul, 0x0000000707070700ul, 0x0000000E0E0E0E00ul, 0x0000001C1C1C1C00ul,
                0x0000003838383800ul, 0x0000007070707000ul, 0x000000E0E0E0E000ul, 0x000000C0C0C0C000ul,
                0x0000030303030300ul, 0x0000070707070700ul, 0x00000E0E0E0E0E00ul, 0x00001C1C1C1C1C00ul,
                0x0000383838383800ul, 0x0000707070707000ul, 0x0000E0E0E0E0E000ul, 0x0000C0C0C0C0C000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            }
            #endregion passedPawnMasks data
        };
        private static readonly ulong[] isolatedPawnMasks =
        {
            #region isolatedPawnMasks data
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            #endregion isolatedPawnMasks data
        };
        private static readonly ulong[][] homeLocations =
        {
            #region homeLocations data
            new[]
            {
                0x000000000000FF00ul, 0x0000000000000042ul, 0x0000000000000024ul, 0x0000000000000081ul,
                0x0000000000000008ul, 0x0000000000000010ul
            },
            new[]
            {
                0x0000000000000000ul, 0x4200000000000000ul, 0x2400000000000000ul, 0x8100000000000000ul,
                0x0800000000000000ul, 0x1000000000000000ul
            }
            #endregion homeLocations data
        };
        private static readonly (CastlingRights mask, int shift) [] castlingRights =
        {
            (CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide, 0),
            (CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide, 2)
        };
        private static readonly ulong[][] kingProximity =
        {
            #region kingProximity data
            new[]
            {
                0x0000000000000302ul, 0x0000000000000705ul, 0x0000000000000E0Aul, 0x0000000000001C14ul,
                0x0000000000003828ul, 0x0000000000007050ul, 0x000000000000E0A0ul, 0x000000000000C040ul,
                0x0000000000030203ul, 0x0000000000070507ul, 0x00000000000E0A0Eul, 0x00000000001C141Cul,
                0x0000000000382838ul, 0x0000000000705070ul, 0x0000000000E0A0E0ul, 0x0000000000C040C0ul,
                0x0000000003020300ul, 0x0000000007050700ul, 0x000000000E0A0E00ul, 0x000000001C141C00ul,
                0x0000000038283800ul, 0x0000000070507000ul, 0x00000000E0A0E000ul, 0x00000000C040C000ul,
                0x0000000302030000ul, 0x0000000705070000ul, 0x0000000E0A0E0000ul, 0x0000001C141C0000ul,
                0x0000003828380000ul, 0x0000007050700000ul, 0x000000E0A0E00000ul, 0x000000C040C00000ul,
                0x0000030203000000ul, 0x0000070507000000ul, 0x00000E0A0E000000ul, 0x00001C141C000000ul,
                0x0000382838000000ul, 0x0000705070000000ul, 0x0000E0A0E0000000ul, 0x0000C040C0000000ul,
                0x0003020300000000ul, 0x0007050700000000ul, 0x000E0A0E00000000ul, 0x001C141C00000000ul,
                0x0038283800000000ul, 0x0070507000000000ul, 0x00E0A0E000000000ul, 0x00C040C000000000ul,
                0x0302030000000000ul, 0x0705070000000000ul, 0x0E0A0E0000000000ul, 0x1C141C0000000000ul,
                0x3828380000000000ul, 0x7050700000000000ul, 0xE0A0E00000000000ul, 0xC040C00000000000ul,
                0x0203000000000000ul, 0x0507000000000000ul, 0x0A0E000000000000ul, 0x141C000000000000ul,
                0x2838000000000000ul, 0x5070000000000000ul, 0xA0E0000000000000ul, 0x40C0000000000000ul
            },
            new[]
            {
                0x0000000000070404ul, 0x00000000000F0808ul, 0x00000000001F1111ul, 0x00000000003E2222ul,
                0x00000000007C4444ul, 0x0000000000F88888ul, 0x0000000000F01010ul, 0x0000000000E02020ul,
                0x0000000007040404ul, 0x000000000F080808ul, 0x000000001F111111ul, 0x000000003E222222ul,
                0x000000007C444444ul, 0x00000000F8888888ul, 0x00000000F0101010ul, 0x00000000E0202020ul,
                0x0000000704040407ul, 0x0000000F0808080Ful, 0x0000001F1111111Ful, 0x0000003E2222223Eul,
                0x0000007C4444447Cul, 0x000000F8888888F8ul, 0x000000F0101010F0ul, 0x000000E0202020E0ul,
                0x0000070404040700ul, 0x00000F0808080F00ul, 0x00001F1111111F00ul, 0x00003E2222223E00ul,
                0x00007C4444447C00ul, 0x0000F8888888F800ul, 0x0000F0101010F000ul, 0x0000E0202020E000ul,
                0x0007040404070000ul, 0x000F0808080F0000ul, 0x001F1111111F0000ul, 0x003E2222223E0000ul,
                0x007C4444447C0000ul, 0x00F8888888F80000ul, 0x00F0101010F00000ul, 0x00E0202020E00000ul,
                0x0704040407000000ul, 0x0F0808080F000000ul, 0x1F1111111F000000ul, 0x3E2222223E000000ul,
                0x7C4444447C000000ul, 0xF8888888F8000000ul, 0xF0101010F0000000ul, 0xE0202020E0000000ul,
                0x0404040700000000ul, 0x0808080F00000000ul, 0x1111111F00000000ul, 0x2222223E00000000ul,
                0x4444447C00000000ul, 0x888888F800000000ul, 0x101010F000000000ul, 0x202020E000000000ul,
                0x0404070000000000ul, 0x08080F0000000000ul, 0x11111F0000000000ul, 0x22223E0000000000ul,
                0x44447C0000000000ul, 0x8888F80000000000ul, 0x1010F00000000000ul, 0x2020E00000000000ul
            },
            new[]
            {
                0x000000000F080808ul, 0x000000001F101010ul, 0x000000003F202020ul, 0x000000007F414141ul,
                0x00000000FE828282ul, 0x00000000FC040404ul, 0x00000000F8080808ul, 0x00000000F0101010ul,
                0x0000000F08080808ul, 0x0000001F10101010ul, 0x0000003F20202020ul, 0x0000007F41414141ul,
                0x000000FE82828282ul, 0x000000FC04040404ul, 0x000000F808080808ul, 0x000000F010101010ul,
                0x00000F0808080808ul, 0x00001F1010101010ul, 0x00003F2020202020ul, 0x00007F4141414141ul,
                0x0000FE8282828282ul, 0x0000FC0404040404ul, 0x0000F80808080808ul, 0x0000F01010101010ul,
                0x000F08080808080Ful, 0x001F10101010101Ful, 0x003F20202020203Ful, 0x007F41414141417Ful,
                0x00FE8282828282FEul, 0x00FC0404040404FCul, 0x00F80808080808F8ul, 0x00F01010101010F0ul,
                0x0F08080808080F00ul, 0x1F10101010101F00ul, 0x3F20202020203F00ul, 0x7F41414141417F00ul,
                0xFE8282828282FE00ul, 0xFC0404040404FC00ul, 0xF80808080808F800ul, 0xF01010101010F000ul,
                0x08080808080F0000ul, 0x10101010101F0000ul, 0x20202020203F0000ul, 0x41414141417F0000ul,
                0x8282828282FE0000ul, 0x0404040404FC0000ul, 0x0808080808F80000ul, 0x1010101010F00000ul,
                0x080808080F000000ul, 0x101010101F000000ul, 0x202020203F000000ul, 0x414141417F000000ul,
                0x82828282FE000000ul, 0x04040404FC000000ul, 0x08080808F8000000ul, 0x10101010F0000000ul,
                0x0808080F00000000ul, 0x1010101F00000000ul, 0x2020203F00000000ul, 0x4141417F00000000ul,
                0x828282FE00000000ul, 0x040404FC00000000ul, 0x080808F800000000ul, 0x101010F000000000ul
            }

            #endregion kingProximity data
        };
        private static readonly ulong[][] backwardPawnMasks =
        {
            #region backwardPawnMasks data
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000200ul, 0x0000000000000500ul, 0x0000000000000A00ul, 0x0000000000001400ul,
                0x0000000000002800ul, 0x0000000000005000ul, 0x000000000000A000ul, 0x0000000000004000ul,
                0x0000000000020200ul, 0x0000000000050500ul, 0x00000000000A0A00ul, 0x0000000000141400ul,
                0x0000000000282800ul, 0x0000000000505000ul, 0x0000000000A0A000ul, 0x0000000000404000ul,
                0x0000000002020200ul, 0x0000000005050500ul, 0x000000000A0A0A00ul, 0x0000000014141400ul,
                0x0000000028282800ul, 0x0000000050505000ul, 0x00000000A0A0A000ul, 0x0000000040404000ul,
                0x0000000202020200ul, 0x0000000505050500ul, 0x0000000A0A0A0A00ul, 0x0000001414141400ul,
                0x0000002828282800ul, 0x0000005050505000ul, 0x000000A0A0A0A000ul, 0x0000004040404000ul,
                0x0000020202020200ul, 0x0000050505050500ul, 0x00000A0A0A0A0A00ul, 0x0000141414141400ul,
                0x0000282828282800ul, 0x0000505050505000ul, 0x0000A0A0A0A0A000ul, 0x0000404040404000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            },
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0002020202020000ul, 0x0005050505050000ul, 0x000A0A0A0A0A0000ul, 0x0014141414140000ul,
                0x0028282828280000ul, 0x0050505050500000ul, 0x00A0A0A0A0A00000ul, 0x0040404040400000ul,
                0x0002020202000000ul, 0x0005050505000000ul, 0x000A0A0A0A000000ul, 0x0014141414000000ul,
                0x0028282828000000ul, 0x0050505050000000ul, 0x00A0A0A0A0000000ul, 0x0040404040000000ul,
                0x0002020200000000ul, 0x0005050500000000ul, 0x000A0A0A00000000ul, 0x0014141400000000ul,
                0x0028282800000000ul, 0x0050505000000000ul, 0x00A0A0A000000000ul, 0x0040404000000000ul,
                0x0002020000000000ul, 0x0005050000000000ul, 0x000A0A0000000000ul, 0x0014140000000000ul,
                0x0028280000000000ul, 0x0050500000000000ul, 0x00A0A00000000000ul, 0x0040400000000000ul,
                0x0002000000000000ul, 0x0005000000000000ul, 0x000A000000000000ul, 0x0014000000000000ul,
                0x0028000000000000ul, 0x0050000000000000ul, 0x00A0000000000000ul, 0x0040000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            }

            #endregion backwardPawnMasks data
        };

        private static readonly ulong[] adjacentPawnMasks =
        {
            #region adjacentPawnMasks data
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000200ul, 0x0000000000000500ul, 0x0000000000000A00ul, 0x0000000000001400ul,
            0x0000000000002800ul, 0x0000000000005000ul, 0x000000000000A000ul, 0x0000000000004000ul,
            0x0000000000020000ul, 0x0000000000050000ul, 0x00000000000A0000ul, 0x0000000000140000ul,
            0x0000000000280000ul, 0x0000000000500000ul, 0x0000000000A00000ul, 0x0000000000400000ul,
            0x0000000002000000ul, 0x0000000005000000ul, 0x000000000A000000ul, 0x0000000014000000ul,
            0x0000000028000000ul, 0x0000000050000000ul, 0x00000000A0000000ul, 0x0000000040000000ul,
            0x0000000200000000ul, 0x0000000500000000ul, 0x0000000A00000000ul, 0x0000001400000000ul,
            0x0000002800000000ul, 0x0000005000000000ul, 0x000000A000000000ul, 0x0000004000000000ul,
            0x0000020000000000ul, 0x0000050000000000ul, 0x00000A0000000000ul, 0x0000140000000000ul,
            0x0000280000000000ul, 0x0000500000000000ul, 0x0000A00000000000ul, 0x0000400000000000ul,
            0x0002000000000000ul, 0x0005000000000000ul, 0x000A000000000000ul, 0x0014000000000000ul,
            0x0028000000000000ul, 0x0050000000000000ul, 0x00A0000000000000ul, 0x0040000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            #endregion adjacentPawnMasks data
        };

        private static readonly short[] defaultScores = { 0, 0 };
        private static readonly int[] pawnOffset = { 8, -8 };
    }
}
