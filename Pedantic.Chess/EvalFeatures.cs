using System.Numerics;
using LiteDB;
using Pedantic.Utilities;
using System.Runtime.CompilerServices;
using Pedantic.Genetics;

namespace Pedantic.Chess
{
    public sealed class EvalFeatures
    {
        // values required to determine phase and for mopup eval
        private readonly short fullMoveCounter;
        private readonly Color sideToMove;
        private readonly short[] material = new short[Constants.MAX_COLORS];
        private readonly sbyte[] kingIndex = new sbyte[Constants.MAX_COLORS];
        private readonly sbyte totalPawns;

        /*
         * Array/Vector of features (one per color)
         * [0]          # pawns
         * [1]          # knights
         * [2]          # bishops
         * [3]          # rooks
         * [4]          # queens
         * [5 - 68]     0-1 pawn on square
         * [69 - 132]   0-1 knight on square
         * [133 - 196]  0-1 bishop on square
         * [197 - 260]  0-1 rook on square
         * [261 - 324]  0-1 queen on square
         * [325 - 388]  0-1 king on square
         * [389]        # mobility
         * [390]        # isolated pawns
         * [391]        # backward pawns
         * [392]        # doubled pawns
         * [393 - 398]  # passed pawns on rank 2 - 7
         * [399 - 404]  # adjacent pawns on rank 2 - 7
         * [405]        # pieces in king proximity d1
         * [406]        # pieces in king proximity d2
         * [407]        # pieces in king proximity d3
         * [408]        # knights on outpost
         * [409]        # bishops on outpost
         * [410]        0-1 bishop pair
         * [411 - 416]  # pawns blocked by pawn rank 2 - 7
         * [417 - 422]  # pawns blocked by knights rank 2 - 7
         * [423 - 428]  # pawns blocked by bishops rank 2 - 7
         * [429 - 434]  # pawns blocked by rooks rank 2 - 7
         * [435 - 440]  # pawns blocked by queens rank 2 - 7
         * [441 - 446]  # pawns blocked by king rank 2 - 7
         * [447]        # pawns blocked from double move
         * [448]        0-1 queen-side pawn majority
         * [449]        0-1 king-side pawn majority
         * [450 - 455]  # closest passed pawns king cannot stop promotion rank 2 - 7
         * [456]        # passed pawns king can stop
         *
         * Items below this are not included in dot-product. This makes the feature vector +1 greater than weight vector.
         * [457]        # game phase boundary
         */
        public const int FEATURE_SIZE = 457;
        public const int MATERIAL = 0;
        public const int PIECE_SQUARE_TABLES = 5;
        public const int MOBILITY = 389;
        public const int ISOLATED_PAWNS = 390;
        public const int BACKWARD_PAWNS = 391;
        public const int DOUBLED_PAWNS = 392;
        public const int PASSED_PAWNS = 393;
        public const int ADJACENT_PAWNS = 399;
        public const int KING_PROXIMITY = 405;
        public const int KNIGHTS_ON_OUTPOST = 408;
        public const int BISHOPS_ON_OUTPOST = 409;
        public const int BISHOP_PAIR = 410;
        public const int BLOCKED_PAWNS = 411;
        public const int BLOCKED_DBL_MOVE_PAWNS = 447;
        public const int QUEEN_SIDE_PAWN_MAJORITY = 448;
        public const int KING_SIDE_PAWN_MAJORITY = 449;
        public const int KING_NOT_IN_CLOSEST_SQUARE = 450;
        public const int KING_IN_PROMOTE_SQUARE = 456;
        public const int GAME_PHASE_BOUNDARY = 457;


        private readonly short[][] features = { new short[FEATURE_SIZE], new short[FEATURE_SIZE] };

        public EvalFeatures(Board bd)
        {
            fullMoveCounter = (short)bd.FullMoveCounter;
            totalPawns = (sbyte)BitOps.PopCount(bd.Pieces(Color.White, Piece.Pawn) | bd.Pieces(Color.Black, Piece.Pawn));
            sideToMove = bd.SideToMove;

            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int c = (int)color;
                short[] v = features[c];
                for (int index = 0; index < Constants.MAX_SQUARES; index++)
                {
                    Square square = bd.PieceBoard[index];
                    int pstIndex = Index.NormalizedIndex[c][index];
                    if (!square.IsEmpty && square.Color == color)
                    {
                        IncrementPieceCount(v, square.Piece);
                        SetPieceSquare(v, square.Piece, pstIndex);
                    }
                }

                SetMobility(v, bd.GetPieceMobility(color));

                Evaluation.CalcKingProximityAttacks(bd, color, out int d1, out int d2, out int d3);
                SetKingProximity(v, (short)d1, (short)d2, (short)d3);

                material[c] = bd.Material(color);
                kingIndex[c] = (sbyte)BitOps.TzCount(bd.Pieces(color, Piece.King));
                Color other = (Color)(c ^ 1);
                int o = (int)other;
                ulong pawns = bd.Pieces(color, Piece.Pawn);
                ulong otherPawns = bd.Pieces(other, Piece.Pawn);
                ulong myKing = bd.Pieces(color, Piece.King);

                if (BitOps.PopCount(pawns & Evaluation.QUEEN_SIDE_MASK) >
                    BitOps.PopCount(otherPawns & Evaluation.QUEEN_SIDE_MASK))
                {
                    SetQueenSideMajority(v);
                }

                if (BitOps.PopCount(pawns & Evaluation.KING_SIDE_MASK) >
                    BitOps.PopCount(otherPawns & Evaluation.KING_SIDE_MASK))
                {
                    SetKingSideMajority(v);
                }

                for (ulong p = pawns; p != 0; p = BitOps.ResetLsb(p))
                {
                    int sq = BitOps.TzCount(p);
                    int normalSq = Index.NormalizedIndex[c][sq];
                    int normalRank = Index.GetRank(normalSq);

                    if ((otherPawns & Evaluation.PassedPawnMasks[c][sq]) == 0)
                    {
                        IncrementPassedPawns(v, normalRank);
                    }

                    if ((pawns & Evaluation.IsolatedPawnMasks[sq]) == 0)
                    {
                        IncrementIsolatedPawns(v);
                    }

                    if ((pawns & Evaluation.BackwardPawnMasks[c][sq]) == 0)
                    {
                        IncrementBackwardPawns(v);
                    }

                    if ((pawns & Evaluation.AdjacentPawnMasks[sq]) != 0)
                    {
                        IncrementAdjacentPawns(v, normalRank);
                    }
                }

                for (int file = 0; file < Constants.MAX_COORDS && pawns != 0; file++)
                {
                    if (BitOps.PopCount(pawns & Board.MaskFiles[file]) > 1)
                    {
                        IncrementDoubledPawns(v);
                    }
                }

                ulong bb1, bb2;
                if (color == Color.White)
                {
                    bb1 = pawns & (bd.Units(color) >> 8);
                    bb2 = BitOps.AndNot(pawns, bd.All >> 16) & Board.MaskRanks[Index.A2];
                    bb2 &= bd.Units(color) >> 8;
                }
                else
                {
                    bb1 = pawns & (bd.Units(color) << 8);
                    bb2 = BitOps.AndNot(pawns, bd.All << 16) & Board.MaskRanks[Index.A7];
                    bb2 &= bd.Units(color) << 8;
                }

                for (; bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int sq = BitOps.TzCount(bb1);
                    int normalSq = Index.NormalizedIndex[c][sq];
                    int normalRank = Index.GetRank(normalSq);
                    int blockerSq = sq + Evaluation.PawnOffset[c];

                    Piece piece = bd.PieceBoard[blockerSq].Piece;
                    IncrementBlockedPawns(v, piece, normalRank);
                }

                short count = (short)BitOps.PopCount(bb2);
                if (count > 0)
                {
                    SetBlockedDblMovePawns(v, count);
                }

                int closestDist = Constants.MAX_COORDS;
                for (ulong bb = otherPawns; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    if ((pawns & Evaluation.PassedPawnMasks[o][sq]) == 0)
                    {
                        closestDist = Math.Min(closestDist, Evaluation.CalcPromoteDistance(other, sq));
                        if ((Evaluation.PromoteSquares[o][sq] & myKing) != 0)
                        {
                            IncrementKingInPromoteSquare(v);
                        }
                    }
                }

                if (closestDist < Constants.MAX_COORDS)
                {
                    int normalRank = Coord.MaxValue - closestDist;
                    for (ulong bb = otherPawns; bb != 0; bb = BitOps.ResetLsb(bb))
                    {
                        int sq = BitOps.TzCount(bb);
                        if ((pawns & Evaluation.PassedPawnMasks[o][sq]) == 0 &&
                            Evaluation.CalcPromoteDistance(other, sq) == closestDist &&
                            (myKing & Evaluation.PromoteSquares[o][sq]) == 0)
                        {
                            IncrementKingNotInClosest(v, normalRank);
                        }
                    }
                }

                int bishopCount = BitOps.PopCount(bd.Pieces(color, Piece.Bishop));
                if (bishopCount >= 2)
                {
                    SetBishopPair(v);
                }

                ulong knights = bd.Pieces(color, Piece.Knight);
                for (ulong bb = knights; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                    if (normalRank > 3 && (Board.PawnDefends(color, sq) & pawns) != 0)
                    {
                        IncrementKnightsOnOutpost(v);
                    }
                }

                ulong bishops = bd.Pieces(color, Piece.Bishop);
                for (ulong bb = bishops; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                    if (normalRank > 3 && (Board.PawnDefends(color, sq) & pawns) != 0)
                    {
                        IncrementBishopsOnOutpost(v);
                    }
                }
            }
        }

        public short Compute(ReadOnlySpan<short> opWeights, ReadOnlySpan<short> egWeights)
        {
            try
            {
                Span<short> opScore = stackalloc short[2];
                Span<short> egScore = stackalloc short[2];
                opScore.Clear();
                egScore.Clear();

                short score;

                Evaluation.GamePhase gamePhase = GetGamePhase(opWeights[GAME_PHASE_BOUNDARY],
                    egWeights[GAME_PHASE_BOUNDARY], out int opWt, out int egWt);

                if (gamePhase == Evaluation.GamePhase.MopUp)
                {
                    // TODO: Move MopUpTable and Distance Weight to weight vector so they can be optimized
                    for (Color color = Color.White; color <= Color.Black; color++)
                    {
                        int c = (int)color;
                        ReadOnlySpan<short> f = features[c][..5];
                        ReadOnlySpan<short> wts = egWeights[..5];
                        opScore[c] = 0;
                        egScore[c] = DotProduct(f, wts);
                        egScore[c] += Evaluation.MopUpTable[kingIndex[c]];
                    }

                    short d = (short)Index.Distance(kingIndex[0], kingIndex[1]);
                    if (egScore[0] > egScore[1])
                    {
                        egScore[0] -= d;
                    }
                    else
                    {
                        egScore[1] -= d;
                    }

                    score = (short)(egScore[0] - egScore[1]);
                    return sideToMove == Color.White ? score : (short)-score;
                }

                for (Color color = Color.White; color <= Color.Black; color++)
                {
                    int c = (int)color;
                    opScore[c] = DotProduct(features[c], opWeights);
                    egScore[c] = DotProduct(features[c], egWeights);
                }

                score = (short)((((opScore[0] - opScore[1]) * opWt) >> 7 /* / 128 */) +
                                (((egScore[0] - egScore[1]) * egWt) >> 7 /* / 128 */));

                return sideToMove == Color.White ? score : (short)-score;
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.ToString());
                throw new Exception("EvalFeatures.Compute error occurred.", ex);
            }
        }

        public Color SideToMove => sideToMove;

        // TODO: Change native ChessWeight layout so that this mapping isn't required
        public static short[] GetCombinedWeights(ChessWeights wt)
        {
            // allocate array large enough to hold both opening and end game weights (i.e. "Combined")
            short[] weights = new short[(FEATURE_SIZE + 1) * 2];

            // opening weights are first
            for (int pc = (int)Piece.Pawn; pc <= (int)Piece.Queen; pc++)
            {
                weights[MATERIAL + pc] = wt.Weights[ChessWeights.OPENING_PIECE_WEIGHT_OFFSET + pc];
            }

            for (int n = 0; n < ChessWeights.PIECE_SQUARE_LENGTH; n++)
            {
                weights[PIECE_SQUARE_TABLES + n] =
                    wt.Weights[ChessWeights.OPENING_PIECE_SQUARE_WEIGHT_OFFSET + n];
            }

            weights[MOBILITY] = wt.Weights[ChessWeights.OPENING_MOBILITY_WEIGHT_OFFSET];
            weights[ISOLATED_PAWNS] = wt.Weights[ChessWeights.OPENING_ISOLATED_PAWN_OFFSET];
            weights[BACKWARD_PAWNS] = wt.Weights[ChessWeights.OPENING_BACKWARD_PAWN_OFFSET];
            weights[DOUBLED_PAWNS] = wt.Weights[ChessWeights.OPENING_DOUBLED_PAWN_OFFSET];

            for (int n = 0; n < 6; n++)
            {
                weights[PASSED_PAWNS + n] = wt.Weights[ChessWeights.OPENING_PASSED_PAWNS_OFFSET + n];
                weights[ADJACENT_PAWNS + n] = wt.Weights[ChessWeights.OPENING_ADJACENT_PAWNS_OFFSET + n];
                weights[KING_NOT_IN_CLOSEST_SQUARE + n] =
                    wt.Weights[ChessWeights.OPENING_KING_NOT_IN_CLOSEST_PROMOTE_OFFSET + n];
            }

            weights[KING_PROXIMITY] = wt.Weights[ChessWeights.OPENING_KING_ATTACK_WEIGHT_OFFSET];
            weights[KING_PROXIMITY + 1] = wt.Weights[ChessWeights.OPENING_KING_ATTACK_WEIGHT_OFFSET + 1];
            weights[KING_PROXIMITY + 2] = wt.Weights[ChessWeights.OPENING_KING_ATTACK_WEIGHT_OFFSET + 2];

            weights[KNIGHTS_ON_OUTPOST] = wt.Weights[ChessWeights.OPENING_KNIGHT_OUTPOST_OFFSET];
            weights[BISHOPS_ON_OUTPOST] = wt.Weights[ChessWeights.OPENING_BISHOP_OUTPOST_OFFSET];
            weights[BISHOP_PAIR] = wt.Weights[ChessWeights.OPENING_BISHOP_PAIR_OFFSET];

            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                for (int r = 0; r < 6; r++)
                {
                    int offset = pc * 6 + r;
                    weights[BLOCKED_PAWNS + offset] = wt.Weights[ChessWeights.OPENING_BLOCKED_PAWN_OFFSET + offset];
                }
            }

            weights[BLOCKED_DBL_MOVE_PAWNS] = wt.Weights[ChessWeights.OPENING_BLOCKED_PAWN_DBL_MOVE_OFFSET];
            weights[QUEEN_SIDE_PAWN_MAJORITY] = wt.Weights[ChessWeights.OPENING_PAWN_QS_MAJORITY_OFFSET];
            weights[KING_SIDE_PAWN_MAJORITY] = wt.Weights[ChessWeights.OPENING_PAWN_KS_MAJORITY_OFFSET];
            weights[KING_IN_PROMOTE_SQUARE] = wt.Weights[ChessWeights.OPENING_KING_NEAR_PASSED_PAWN_OFFSET];
            weights[GAME_PHASE_BOUNDARY] = wt.Weights[ChessWeights.OPENING_PHASE_WEIGHT_OFFSET];

            // ... followed by the end-game weights
            const int eg = FEATURE_SIZE + 1;

            for (int pc = (int)Piece.Pawn; pc <= (int)Piece.Queen; pc++)
            {
                weights[MATERIAL + pc + eg] = wt.Weights[ChessWeights.ENDGAME_PIECE_WEIGHT_OFFSET + pc];
            }

            for (int n = 0; n < ChessWeights.PIECE_SQUARE_LENGTH; n++)
            {
                weights[PIECE_SQUARE_TABLES + n + eg] =
                    wt.Weights[ChessWeights.ENDGAME_PIECE_SQUARE_WEIGHT_OFFSET + n];
            }

            weights[MOBILITY + eg] = wt.Weights[ChessWeights.ENDGAME_MOBILITY_WEIGHT_OFFSET];
            weights[ISOLATED_PAWNS + eg] = wt.Weights[ChessWeights.ENDGAME_ISOLATED_PAWN_OFFSET];
            weights[BACKWARD_PAWNS + eg] = wt.Weights[ChessWeights.ENDGAME_BACKWARD_PAWN_OFFSET];
            weights[DOUBLED_PAWNS + eg] = wt.Weights[ChessWeights.ENDGAME_DOUBLED_PAWN_OFFSET];

            for (int n = 0; n < 6; n++)
            {
                weights[PASSED_PAWNS + n + eg] = wt.Weights[ChessWeights.ENDGAME_PASSED_PAWNS_OFFSET + n];
                weights[ADJACENT_PAWNS + n + eg] = wt.Weights[ChessWeights.ENDGAME_ADJACENT_PAWNS_OFFSET + n];
                weights[KING_NOT_IN_CLOSEST_SQUARE + n + eg] =
                    wt.Weights[ChessWeights.ENDGAME_KING_NOT_IN_CLOSEST_PROMOTE_OFFSET + n];
            }

            weights[KING_PROXIMITY + eg] = wt.Weights[ChessWeights.ENDGAME_KING_ATTACK_WEIGHT_OFFSET];
            weights[KING_PROXIMITY + 1 + eg] = wt.Weights[ChessWeights.ENDGAME_KING_ATTACK_WEIGHT_OFFSET + 1];
            weights[KING_PROXIMITY + 2 + eg] = wt.Weights[ChessWeights.ENDGAME_KING_ATTACK_WEIGHT_OFFSET + 2];

            weights[KNIGHTS_ON_OUTPOST + eg] = wt.Weights[ChessWeights.ENDGAME_KNIGHT_OUTPOST_OFFSET];
            weights[BISHOPS_ON_OUTPOST + eg] = wt.Weights[ChessWeights.ENDGAME_BISHOP_OUTPOST_OFFSET];
            weights[BISHOP_PAIR + eg] = wt.Weights[ChessWeights.ENDGAME_BISHOP_PAIR_OFFSET];

            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                for (int r = 0; r < 6; r++)
                {
                    int offset = pc * 6 + r;
                    weights[BLOCKED_PAWNS + offset + eg] = wt.Weights[ChessWeights.ENDGAME_BLOCKED_PAWN_OFFSET + offset];
                }
            }

            weights[BLOCKED_DBL_MOVE_PAWNS + eg] = wt.Weights[ChessWeights.ENDGAME_BLOCKED_PAWN_DBL_MOVE_OFFSET];
            weights[QUEEN_SIDE_PAWN_MAJORITY + eg] = wt.Weights[ChessWeights.ENDGAME_PAWN_QS_MAJORITY_OFFSET];
            weights[KING_SIDE_PAWN_MAJORITY + eg] = wt.Weights[ChessWeights.ENDGAME_PAWN_KS_MAJORITY_OFFSET];
            weights[KING_IN_PROMOTE_SQUARE + eg] = wt.Weights[ChessWeights.ENDGAME_KING_NEAR_PASSED_PAWN_OFFSET];
            weights[GAME_PHASE_BOUNDARY + eg] = wt.Weights[ChessWeights.ENDGAME_PHASE_WEIGHT_OFFSET];

            return weights;
        }

        public static void UpdateCombinedWeights(ChessWeights wt, short[] weights)
        {
            // opening weights are first
            for (int pc = (int)Piece.Pawn; pc <= (int)Piece.Queen; pc++)
            {
                wt.Weights[ChessWeights.OPENING_PIECE_WEIGHT_OFFSET + pc] = weights[MATERIAL + pc];
            }

            for (int n = 0; n < ChessWeights.PIECE_SQUARE_LENGTH; n++)
            {
                wt.Weights[ChessWeights.OPENING_PIECE_SQUARE_WEIGHT_OFFSET + n] = weights[PIECE_SQUARE_TABLES + n];
            }

            wt.Weights[ChessWeights.OPENING_MOBILITY_WEIGHT_OFFSET] = weights[MOBILITY];
            wt.Weights[ChessWeights.OPENING_ISOLATED_PAWN_OFFSET] = weights[ISOLATED_PAWNS];
            wt.Weights[ChessWeights.OPENING_BACKWARD_PAWN_OFFSET] = weights[BACKWARD_PAWNS]; 
            wt.Weights[ChessWeights.OPENING_DOUBLED_PAWN_OFFSET] = weights[DOUBLED_PAWNS];

            for (int n = 0; n < 6; n++)
            {
                wt.Weights[ChessWeights.OPENING_PASSED_PAWNS_OFFSET + n] = weights[PASSED_PAWNS + n];
                wt.Weights[ChessWeights.OPENING_ADJACENT_PAWNS_OFFSET + n] = weights[ADJACENT_PAWNS + n];
                wt.Weights[ChessWeights.OPENING_KING_NOT_IN_CLOSEST_PROMOTE_OFFSET + n] = weights[KING_NOT_IN_CLOSEST_SQUARE + n];
            }

            wt.Weights[ChessWeights.OPENING_KING_ATTACK_WEIGHT_OFFSET] = weights[KING_PROXIMITY];
            wt.Weights[ChessWeights.OPENING_KING_ATTACK_WEIGHT_OFFSET + 1] = weights[KING_PROXIMITY + 1];
            wt.Weights[ChessWeights.OPENING_KING_ATTACK_WEIGHT_OFFSET + 2] = weights[KING_PROXIMITY + 2];

            wt.Weights[ChessWeights.OPENING_KNIGHT_OUTPOST_OFFSET] = weights[KNIGHTS_ON_OUTPOST];
            wt.Weights[ChessWeights.OPENING_BISHOP_OUTPOST_OFFSET] = weights[BISHOPS_ON_OUTPOST];
            wt.Weights[ChessWeights.OPENING_BISHOP_PAIR_OFFSET] = weights[BISHOP_PAIR];

            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                for (int r = 0; r < 6; r++)
                {
                    int offset = pc * 6 + r;
                    wt.Weights[ChessWeights.OPENING_BLOCKED_PAWN_OFFSET + offset] = weights[BLOCKED_PAWNS + offset];
                }
            }

            wt.Weights[ChessWeights.OPENING_BLOCKED_PAWN_DBL_MOVE_OFFSET] = weights[BLOCKED_DBL_MOVE_PAWNS];
            wt.Weights[ChessWeights.OPENING_PAWN_QS_MAJORITY_OFFSET] = weights[QUEEN_SIDE_PAWN_MAJORITY];
            wt.Weights[ChessWeights.OPENING_PAWN_KS_MAJORITY_OFFSET] = weights[KING_SIDE_PAWN_MAJORITY];
            wt.Weights[ChessWeights.OPENING_KING_NEAR_PASSED_PAWN_OFFSET] = weights[KING_IN_PROMOTE_SQUARE];
            wt.Weights[ChessWeights.OPENING_PHASE_WEIGHT_OFFSET] = weights[GAME_PHASE_BOUNDARY];

            // ... followed by the end-game weights
            const int eg = FEATURE_SIZE + 1;

            for (int pc = (int)Piece.Pawn; pc <= (int)Piece.Queen; pc++)
            {
                wt.Weights[ChessWeights.ENDGAME_PIECE_WEIGHT_OFFSET + pc] = weights[MATERIAL + pc + eg];
            }

            for (int n = 0; n < ChessWeights.PIECE_SQUARE_LENGTH; n++)
            {
                wt.Weights[ChessWeights.ENDGAME_PIECE_SQUARE_WEIGHT_OFFSET + n] = weights[PIECE_SQUARE_TABLES + n + eg];
            }

            wt.Weights[ChessWeights.ENDGAME_MOBILITY_WEIGHT_OFFSET] = weights[MOBILITY + eg];
            wt.Weights[ChessWeights.ENDGAME_ISOLATED_PAWN_OFFSET] = weights[ISOLATED_PAWNS + eg];
            wt.Weights[ChessWeights.ENDGAME_BACKWARD_PAWN_OFFSET] = weights[BACKWARD_PAWNS + eg];
            wt.Weights[ChessWeights.ENDGAME_DOUBLED_PAWN_OFFSET] = weights[DOUBLED_PAWNS + eg];

            for (int n = 0; n < 6; n++)
            {
                wt.Weights[ChessWeights.ENDGAME_PASSED_PAWNS_OFFSET + n] = weights[PASSED_PAWNS + n + eg];
                wt.Weights[ChessWeights.ENDGAME_ADJACENT_PAWNS_OFFSET + n] = weights[ADJACENT_PAWNS + n + eg];
                wt.Weights[ChessWeights.ENDGAME_KING_NOT_IN_CLOSEST_PROMOTE_OFFSET + n] = weights[KING_NOT_IN_CLOSEST_SQUARE + n + eg];
            }

            wt.Weights[ChessWeights.ENDGAME_KING_ATTACK_WEIGHT_OFFSET] = weights[KING_PROXIMITY + eg];
            wt.Weights[ChessWeights.ENDGAME_KING_ATTACK_WEIGHT_OFFSET + 1] = weights[KING_PROXIMITY + 1 + eg];
            wt.Weights[ChessWeights.ENDGAME_KING_ATTACK_WEIGHT_OFFSET + 2] = weights[KING_PROXIMITY + 2 + eg];

            wt.Weights[ChessWeights.ENDGAME_KNIGHT_OUTPOST_OFFSET] = weights[KNIGHTS_ON_OUTPOST + eg];
            wt.Weights[ChessWeights.ENDGAME_BISHOP_OUTPOST_OFFSET] = weights[BISHOPS_ON_OUTPOST + eg];
            wt.Weights[ChessWeights.ENDGAME_BISHOP_PAIR_OFFSET] = weights[BISHOP_PAIR + eg];

            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                for (int r = 0; r < 6; r++)
                {
                    int offset = pc * 6 + r;
                    wt.Weights[ChessWeights.ENDGAME_BLOCKED_PAWN_OFFSET + offset] = weights[BLOCKED_PAWNS + offset + eg];
                }
            }

            wt.Weights[ChessWeights.ENDGAME_BLOCKED_PAWN_DBL_MOVE_OFFSET] = weights[BLOCKED_DBL_MOVE_PAWNS + eg];
            wt.Weights[ChessWeights.ENDGAME_PAWN_QS_MAJORITY_OFFSET] = weights[QUEEN_SIDE_PAWN_MAJORITY + eg];
            wt.Weights[ChessWeights.ENDGAME_PAWN_KS_MAJORITY_OFFSET] = weights[KING_SIDE_PAWN_MAJORITY + eg];
            wt.Weights[ChessWeights.ENDGAME_KING_NEAR_PASSED_PAWN_OFFSET] = weights[KING_IN_PROMOTE_SQUARE + eg];
            wt.Weights[ChessWeights.ENDGAME_PHASE_WEIGHT_OFFSET] = weights[GAME_PHASE_BOUNDARY + eg];
            wt.UpdatedOn = DateTime.UtcNow;
        }

        public static short GetOptimizationIncrement(int index)
        {
            const int eg = FEATURE_SIZE + 1;
            return (short)(index == GAME_PHASE_BOUNDARY + eg ? 50 : 1);
        }

        private short DotProduct(ReadOnlySpan<short> f, ReadOnlySpan<short> weights)
        {
            int results = 0;
            if (f.Length >= Vector<short>.Count)
            {
                int remaining = FEATURE_SIZE % Vector<short>.Count;

                for (int i = 0; i < f.Length - remaining; i += Vector<short>.Count)
                {
                    var v1 = new Vector<short>(f[i..]);
                    var v2 = new Vector<short>(weights[i..]);
                    results += Vector.Dot(v1, v2);
                }

                for (int i = f.Length - remaining; i < f.Length; i++)
                {
                    results += (short)(f[i] * weights[i]);
                }
            }
            else
            {
                for (int i = 0; i < f.Length; i++)
                {
                    results += (short)(f[i] * weights[i]);
                }
            }

            return (short)results;
        }

        private Evaluation.GamePhase GetGamePhase(short openingPhaseThru, short endGameMaterial, out int opWt, out int egWt)
        {
            Evaluation.GamePhase phase = Evaluation.GamePhase.Opening;
            opWt = 128;
            egWt = 0;

            if (fullMoveCounter > openingPhaseThru)
            {
                if (material[(int)Color.White] + material[(int)Color.Black] <= endGameMaterial)
                {
                    phase = totalPawns == 0 ? Evaluation.GamePhase.MopUp : Evaluation.GamePhase.EndGame;
                    opWt = 0;
                    egWt = 128;
                }
                else
                {
                    phase = Evaluation.GamePhase.MidGame;
                    int totMaterial = Constants.TOTAL_STARTING_MATERIAL - endGameMaterial;
                    int curMaterial = material[(int)Color.White] + material[(int)Color.Black] - endGameMaterial;
                    opWt = (curMaterial * 128) / totMaterial;
                    egWt = 128 - opWt;
                }
            }

            return phase;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementPieceCount(short[] v, Piece piece)
        {
            if (piece != Piece.King)
            {
                int index = MATERIAL + (int)piece;
                v[index]++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetPieceSquare(short[] v, Piece piece, int square)
        {
            int index = PIECE_SQUARE_TABLES + ((int)piece << 6) + square;
            v[index] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetMobility(short[] v, short mobility)
        {
            v[MOBILITY] = mobility;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementIsolatedPawns(short[] v)
        {
            v[ISOLATED_PAWNS]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementBackwardPawns(short[] v)
        {
            v[BACKWARD_PAWNS]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementDoubledPawns(short[] v)
        {
            v[DOUBLED_PAWNS]++;
        }

        private static void IncrementPassedPawns(short[] v, int rank)
        {
            int index = PASSED_PAWNS + rank - 1;
            v[index]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementAdjacentPawns(short[] v, int rank)
        {
            int index = ADJACENT_PAWNS + rank - 1;
            v[index]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetKingProximity(short[] v, short d1, short d2, short d3)
        {
            int index = KING_PROXIMITY;
            v[index] = d1;
            v[++index] = d2;
            v[++index] = d3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementKnightsOnOutpost(short[] v)
        {
            v[KNIGHTS_ON_OUTPOST]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementBishopsOnOutpost(short[] v)
        {
            v[BISHOPS_ON_OUTPOST]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetBishopPair(short[] v)
        {
            v[BISHOP_PAIR] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementBlockedPawns(short[] v, Piece blocker, int rank)
        {
            int index = BLOCKED_PAWNS + ((int)blocker * 6) + rank - 1;
            v[index]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetBlockedDblMovePawns(short[] v, short count)
        {
            v[BLOCKED_DBL_MOVE_PAWNS] = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetQueenSideMajority(short[] v)
        {
            v[QUEEN_SIDE_PAWN_MAJORITY] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetKingSideMajority(short[] v)
        {
            v[KING_SIDE_PAWN_MAJORITY] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementKingNotInClosest(short[] v, int rank)
        {
            int index = KING_NOT_IN_CLOSEST_SQUARE + rank - 1;
            v[index]++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementKingInPromoteSquare(short[] v)
        {
            v[KING_IN_PROMOTE_SQUARE]++;
        }
    }
}
