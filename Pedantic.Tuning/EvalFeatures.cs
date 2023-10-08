// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 03-15-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="EvalFeatures.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Class EvalFeatures is used by the "Texel" tuning method to 
//     represent how the Evaluation function computes its value, but 
//     structured in a manner that make if efficient to recalculate
//     with different weights.
// </summary>
// ***********************************************************************
using System.Numerics;
using System.Runtime.CompilerServices;

using Pedantic.Collections;
using Pedantic.Chess;
using Pedantic.Utilities;

using Index = Pedantic.Chess.Index;

namespace Pedantic.Tuning
{
    public unsafe sealed class EvalFeatures
    {
        // values required to determine phase and for mopup eval
        private readonly Color sideToMove;
        private readonly short phase = 0;

        public const int FEATURE_SIZE = HceWeights.MAX_WEIGHTS;
        public const int MATERIAL = HceWeights.PIECE_VALUES;
        public const int PIECE_SQUARE_TABLES = HceWeights.PIECE_SQUARE_TABLE;
        public const int MOBILITY = HceWeights.PIECE_MOBILITY;
        public const int CENTER_CONTROL = HceWeights.CENTER_CONTROL;

        public const int KING_ATTACK = HceWeights.KING_ATTACK;
        public const int PAWN_SHIELD = HceWeights.PAWN_SHIELD;
        public const int CASTLING_AVAILABLE = HceWeights.CASTLING_AVAILABLE;
        public const int CASTLING_COMPLETE = HceWeights.CASTLING_COMPLETE;
        public const int KING_ON_OPEN_FILE = HceWeights.KING_ON_OPEN_FILE;
        public const int KING_ON_HALF_OPEN_FILE = HceWeights.KING_ON_HALF_OPEN_FILE;
        public const int KING_ON_OPEN_DIAGONAL = HceWeights.KING_ON_OPEN_DIAGONAL;

        public const int ISOLATED_PAWNS = HceWeights.ISOLATED_PAWN;
        public const int DOUBLED_PAWNS = HceWeights.DOUBLED_PAWN;
        public const int BACKWARD_PAWNS = HceWeights.BACKWARD_PAWN;
        public const int ADJACENT_PAWNS = HceWeights.PHALANX_PAWN;
        public const int PASSED_PAWNS = HceWeights.PASSED_PAWN;
        public const int PAWN_RAM = HceWeights.PAWN_RAM;
        public const int SUPPORTED_PAWN = HceWeights.CHAINED_PAWN;

        //public const int PP_CAN_ADVANCE = HceWeights.PP_CAN_ADVANCE;
        public const int KING_OUTSIDE_SQUARE = HceWeights.KING_OUTSIDE_PP_SQUARE;
        public const int PP_FRIENDLY_KING_DIST = HceWeights.PP_FRIENDLY_KING_DISTANCE;
        public const int PP_ENEMY_KING_DIST = HceWeights.PP_ENEMY_KING_DISTANCE;
        public const int BLOCK_PASSED_PAWN = HceWeights.BLOCK_PASSED_PAWN;

        public const int KNIGHTS_ON_OUTPOST = HceWeights.KNIGHT_OUTPOST;
        public const int BISHOPS_ON_OUTPOST = HceWeights.BISHOP_OUTPOST;
        public const int BISHOP_PAIR = HceWeights.BISHOP_PAIR;
        public const int BAD_BISHOP_PAWN = HceWeights.BAD_BISHOP_PAWN;
        public const int ROOK_OPEN_FILE = HceWeights.ROOK_ON_OPEN_FILE;
        public const int ROOK_HALF_OPEN_FILE = HceWeights.ROOK_ON_HALF_OPEN_FILE;
        public const int ROOK_BEHIND_PASSED_PAWN = HceWeights.ROOK_BEHIND_PASSED_PAWN;
        public const int ROOK_ON_7TH_RANK = HceWeights.ROOK_ON_7TH_RANK;
        public const int DOUBLED_ROOKS_ON_FILE = HceWeights.DOUBLED_ROOKS_ON_FILE;
        public const int QUEEN_OPEN_FILE = HceWeights.QUEEN_ON_OPEN_FILE;
        public const int QUEEN_HALF_OPEN_FILE = HceWeights.QUEEN_ON_HALF_OPEN_FILE;

        public const int PIECE_THREAT = HceWeights.PIECE_THREAT;
        public const int PAWN_PUSH_THREAT = HceWeights.PAWN_PUSH_THREAT;

        private readonly Dictionary<int, short> coefficients;
        private readonly SparseArray<short>[] sparse = { new(), new() };

        private readonly static ulong maskFileA = Board.MaskFile(Index.A1);
        private readonly static ulong maskFileH = Board.MaskFile(Index.H1);

        public EvalFeatures(Board bd)
        {
            //SparseArray<short>[] sparse = { new(), new() };
            sideToMove = bd.SideToMove;
            phase = bd.Phase;

            Span<Evaluation2.EvalInfo> evalInfo = stackalloc Evaluation2.EvalInfo[2];
            Evaluation2.InitializeEvalInfo(bd, evalInfo);

            for (Color color = Color.White; color <= Color.Black; color++)
            {
                Color other = color.Other();
                int c = (int)color;
                int o = (int)other;
                var v = sparse[c];
                ulong pawns = evalInfo[c].Pawns;
                ulong otherPawns = evalInfo[o].Pawns;

                // Material + PST
                for (ulong bb = bd.Units(color); bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    int normalSq = Index.NormalizedIndex[c][sq];
                    Piece piece = bd.PieceBoard[sq].Piece;
                    IncrementPieceCount(v, piece);
                    SetPieceSquare(v, piece, evalInfo[c].KP, normalSq);
                }

                // Pawns
                ulong pawnRams = (color == Color.White ? otherPawns >> 8 : otherPawns << 8);
                for (ulong p = pawns; p != 0; p = BitOps.ResetLsb(p))
                {
                    int sq = BitOps.TzCount(p);
                    int normalSq = Index.NormalizedIndex[c][sq];
                    Ray ray = Board.Vectors[sq];
                    ulong friendMask = color == Color.White ? ray.North : ray.South;
                    ulong sqMask = BitOps.GetMask(sq);
                    bool canBeBackward = true;

                    if ((otherPawns & Evaluation2.PassedPawnMasks[c, sq]) == 0 && (pawns & friendMask) == 0)
                    {
                        SetPassedPawns(v, normalSq);
                        evalInfo[c].PassedPawns |= sqMask;
                        canBeBackward = false;
                    }

                    if ((pawns & Evaluation2.IsolatedPawnMasks[sq]) == 0)
                    {
                        IncrementIsolatedPawns(v);
                        canBeBackward = false;
                    }

                    //if (canBeBackward & (pawns & Evaluation2.BackwardPawnMasks[c, sq]) == 0)
                    //{
                    //    IncrementBackwardPawns(v);
                    //}

                    if ((pawns & Evaluation2.AdjacentPawnMasks[sq]) != 0)
                    {
                        SetAdjacentPawns(v, normalSq);
                    }

                    if ((evalInfo[c].PawnAttacks & sqMask) != 0)
                    {
                        SetSupportedPawn(v, normalSq);
                    }

                    if ((pawnRams & sqMask) != 0)
                    {
                        SetPawnRam(v, normalSq);
                    }
                }

                for (int file = 0; file < Constants.MAX_COORDS; file++)
                {
                    int count = BitOps.PopCount(pawns & Board.MaskFile(file));
                    if (count > 1)
                    {
                        IncrementDoubledPawns(v, (short)--count);
                    }
                }

                // Mobility
                for (Piece pc = Piece.Knight; pc <= Piece.Queen; pc++)
                {
                    for (ulong bb = bd.Pieces(color, pc); bb != 0; bb = BitOps.ResetLsb(bb))
                    {
                        int from = BitOps.TzCount(bb);
                        ulong moves = bd.GetPieceMoves(pc, from);
                        evalInfo[c].PieceAttacks |= moves;
                        IncrementMobility(v, pc, (short)BitOps.PopCount(moves & evalInfo[c].MobilityArea));

                        if (evalInfo[c].AttackCount < Evaluation2.MAX_ATTACK_LEN)
                        {
                            evalInfo[c].Attacks[evalInfo[c].AttackCount++] = moves;
                        }
                    }
                }

                // King Safety / Attack
                int enemyKI = evalInfo[o].KI;
                for (int n = 0; n < evalInfo[c].AttackCount; n++)
                {
                    ulong attacks = evalInfo[c].Attacks[n] & ~evalInfo[o].PawnAttacks;
                    IncrementKingAttack(v, 0, (short)BitOps.PopCount(attacks & Evaluation2.KingProximity[0, enemyKI]));
                    IncrementKingAttack(v, 1, (short)BitOps.PopCount(attacks & Evaluation2.KingProximity[1, enemyKI]));
                    IncrementKingAttack(v, 2, (short)BitOps.PopCount(attacks & Evaluation2.KingProximity[2, enemyKI]));
                }

                // Pawn Shield
                int ki = evalInfo[c].KI;
                SetPawnShield(v, 0, (short)BitOps.PopCount(pawns & Evaluation2.KingProximity[0, ki]));
                SetPawnShield(v, 1, (short)BitOps.PopCount(pawns & Evaluation2.KingProximity[1, ki]));
                SetPawnShield(v, 2, (short)BitOps.PopCount(pawns & Evaluation2.KingProximity[2, ki]));

                // Castling
                if (bd.HasCastled[c])
                {
                    SetCastlingComplete(v);
                }
                else
                {
                    ulong castling = evalInfo[c].CastlingRightsMask & (ulong)bd.Castling;
                    SetCastlingAvailable(v, (short)BitOps.PopCount(castling));
                }

                // Pieces - Knights & Bishops
                ulong knights = bd.Pieces(color, Piece.Knight);
                ulong bishops = bd.Pieces(color, Piece.Bishop);

                if (BitOps.PopCount(bishops) >= 2)
                {
                    SetBishopPair(v);
                }

                for (ulong bb = knights | bishops; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                    ulong sqMask = BitOps.GetMask(sq);

                    if (normalRank > Coord.RANK_4 && (evalInfo[c].PawnAttacks & sqMask) != 0)
                    {
                        Piece pc = bd.PieceBoard[sq].Piece;
                        if (pc == Piece.Knight)
                        {
                            IncrementKnightsOnOutpost(v);
                        }
                        else
                        {
                            IncrementBishopsOnOutpost(v);
                        }
                    }

                    if ((bishops & sqMask) != 0)
                    {
                        ulong badPawns = pawns & Evaluation2.DARK_SQUARES_MASK;
                        if (!Index.IsDark(sq))
                        {
                            badPawns = pawns & Evaluation2.LITE_SQUARES_MASK;
                        }

                        for (ulong bbBadPawn = badPawns; bbBadPawn != 0; bbBadPawn = BitOps.ResetLsb(bbBadPawn))
                        {
                            int pawnSq = BitOps.TzCount(bbBadPawn);
                            int normalSq = Index.NormalizedIndex[c][pawnSq];
                            SetBadBishopPawn(v, normalSq);
                        }
                    }
                }

                // Pieces - Rooks
                ulong rooks = bd.Pieces(color, Piece.Rook);
                ulong allPawns = pawns | otherPawns;
                int enemyKingRank = Index.GetRank(Index.NormalizedIndex[c][evalInfo[o].KI]);

                for (ulong bb = rooks; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                    ulong maskFile = Board.MaskFile(sq);
                    ulong maskRank = Board.MaskRank(sq);

                    if (normalRank == Coord.RANK_7 && ((otherPawns & maskRank) != 0 || enemyKingRank >= Coord.RANK_7))
                    {
                        IncrementRookOnSeventhRank(v);
                    }

                    if ((maskFile & allPawns) == 0)
                    {
                        IncrementRookOnOpenFile(v);

                        if (Evaluation2.IsDoubled(bd, sq))
                        {
                            IncrementDoubledRook(v);
                        }
                    }

                    if ((maskFile & pawns) == 0 && (maskFile & otherPawns) != 0)
                    {
                        IncrementRookOnHalfOpenFile(v);

                        if (Evaluation2.IsDoubled(bd, sq))
                        {
                            IncrementDoubledRook(v);
                        }
                    }
                }

                // Pieces - Queen(s)
                ulong queens = bd.Pieces(color, Piece.Queen);

                for (ulong bb = queens; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    ulong maskFile = Board.MaskFile(sq);

                    if ((maskFile & allPawns) == 0)
                    {
                        IncrementQueenOnOpenFile(v);
                    }

                    if ((maskFile & pawns) == 0 && (maskFile & otherPawns) != 0)
                    {
                        IncrementQueenOnHalfOpenFile(v);
                    }
                }

                // Pieces - King
                ulong kingFileMask = Board.MaskFile(evalInfo[c].KI);
                if ((kingFileMask & allPawns) == 0)
                {
                    SetKingOnOpenFile(v);
                }

                if ((kingFileMask & pawns) == 0 && (kingFileMask & otherPawns) != 0)
                {
                    SetKingOnHalfOpenFile(v);
                }

                ulong kingDiagonalMask = Evaluation2.Diagonals[evalInfo[c].KI];
                if (BitOps.PopCount(kingDiagonalMask) > 3 && (kingDiagonalMask & allPawns) == 0)
                {
                    IncrementKingOnOpenDiagonal(v);
                }

                kingDiagonalMask = Evaluation2.Antidiagonals[evalInfo[c].KI];
                if (BitOps.PopCount(kingDiagonalMask) > 3 && (kingDiagonalMask & allPawns) == 0)
                {
                    IncrementKingOnOpenDiagonal(v);
                }

                // Passed Pawns
                for (ulong p = evalInfo[c].PassedPawns; p != 0; p = BitOps.ResetLsb(p))
                {
                    int sq = BitOps.TzCount(p);
                    Ray ray = Board.Vectors[sq];
                    ulong bbDefender = color == Color.White ?
                        ray.South & ~Board.RevVectors[BitOps.LzCount(ray.South & bd.All)].South :
                        ray.North & ~Board.Vectors[BitOps.TzCount(ray.North & bd.All)].North;

                    if ((bbDefender & bd.Pieces(color, Piece.Rook)) != 0)
                    {
                        IncrementRookBehindPassedPawn(v);
                    }
                    
                    int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                    if (normalRank < Coord.RANK_4)
                    {
                        continue;
                    }

                    int promoteSq = Index.NormalizedIndex[c][Index.ToIndex(Index.GetFile(sq), Coord.RANK_8)];
                    if (bd.PieceCount(other) == 1 && 
                        Index.Distance(sq, promoteSq) < Index.Distance(evalInfo[o].KI, promoteSq) - (other == bd.SideToMove ? 1 : 0))
                    {
                        IncrementKingOutsideSquare(v);
                    }

                    int blockSq = Board.PawnPlus[c, sq];
                    int dist = Index.Distance(blockSq, evalInfo[c].KI);
                    IncrementPPFriendlyKingDistance(v, dist);

                    dist = Index.Distance(blockSq, evalInfo[o].KI);
                    IncrementPPEnemyKingDistance(v, dist);
                }

                // Threats
                ulong targets = bd.Units(other) & ~(otherPawns | bd.Pieces(other, Piece.King));
                ulong pushAttacks;

                if (color == Color.White)
                {
                    ulong pawnPushes = (pawns << 8) & ~bd.All;
                    pushAttacks = ((pawnPushes & ~maskFileA) << 7) | ((pawnPushes & ~maskFileH) << 9);
                }
                else
                {
                    ulong pawnPushes = (pawns >> 8) & ~bd.All;
                    pushAttacks = ((pawnPushes & ~maskFileH) >> 7) | ((pawnPushes & ~maskFileA) >> 9);
                }

                for (ulong bb = evalInfo[c].PawnAttacks & targets; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    Piece defender = bd.PieceBoard[sq].Piece;
                    IncrementPieceThreat(v, Piece.Pawn, defender);
                }

                for (ulong bb = pushAttacks & targets; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    Piece defender = bd.PieceBoard[sq].Piece;
                    IncrementPawnPushThreat(v, defender);
                }

                targets &= ~evalInfo[o].PawnAttacks;

                for (Piece attacker = Piece.Knight; attacker <= Piece.Queen; attacker++)
                {
                    for (ulong bb = bd.Pieces(color, attacker); bb != 0; bb = BitOps.ResetLsb(bb))
                    {
                        int from = BitOps.TzCount(bb);
                        ulong bb2 = bd.GetPieceMoves(attacker, from);
                        for (ulong bbAttacks = bb2 & targets; bbAttacks != 0; bbAttacks = BitOps.ResetLsb(bbAttacks))
                        {
                            int to = BitOps.TzCount(bbAttacks);
                            Piece defender = bd.PieceBoard[to].Piece;

                            // TODO: change from canonical values to phased values
                            if (attacker.Value() <= defender.Value())
                            {
                                IncrementPieceThreat(v, attacker, defender);
                            }
                        }
                    }
                }

                // Miscellaneous
                for (ulong bb = evalInfo[c].Pawns; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    ulong attacks = Board.PawnCaptures(color, sq);
                    short d0Count = (short)BitOps.PopCount(attacks & Evaluation2.D0_CENTER_CONTROL_MASK);
                    short d1Count = (short)BitOps.PopCount(attacks & Evaluation2.D1_CENTER_CONTROL_MASK);
                    IncrementCenterControl(v, 0, d0Count);
                    IncrementCenterControl(v, 1, d0Count);
                }

                for (int n = 0; n < evalInfo[c].AttackCount; n++)
                {
                    short d0Count = (short)BitOps.PopCount(evalInfo[c].Attacks[n] & Evaluation2.D0_CENTER_CONTROL_MASK);
                    short d1Count = (short)BitOps.PopCount(evalInfo[c].Attacks[n] & Evaluation2.D1_CENTER_CONTROL_MASK);
                    IncrementCenterControl(v, 0, d0Count);
                    IncrementCenterControl(v, 1, d1Count);
                }
            }

            for (Color color = Color.White; color <= Color.Black; color++)
            {
                Color other = color.Other();
                int c = (int)color;
                int o = (int)other;
                var v = sparse[c];

                for (ulong pp = evalInfo[c].PassedPawns; pp != 0; pp = BitOps.ResetLsb(pp))
                {
                    int sq = BitOps.TzCount(pp);
                    int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                    if (normalRank < Coord.RANK_4)
                    {
                        continue;
                    }

#if PP_CAN_ADVANCE
                    int blockSq = Board.PawnPlus[c, sq];
                    ulong advanceMask = BitOps.GetMask(blockSq);
                    ulong atkMask = evalInfo[o].PawnAttacks | evalInfo[o].KingAttacks | evalInfo[o].PieceAttacks;
                    if ((advanceMask & atkMask) == 0)
                    {
                        IncrementPPCanAdvance(v, normalRank);
                    }
#endif
                }

                ulong blockedPawns = (other == Color.White) ? (evalInfo[o].PassedPawns << 8) : (evalInfo[o].PassedPawns >> 8);
                ulong blockers = blockedPawns & bd.Units(color);
                for (ulong p = blockers; p != 0; p = BitOps.ResetLsb(p))
                {
                    int sq = BitOps.TzCount(p);
                    int normalRank = Index.GetRank(Index.NormalizedIndex[o][sq]);
                    Piece blocker = bd.PieceBoard[sq].Piece;
                    IncrementBlockPassedPawn(v, normalRank - 1, blocker);
                }
            }

            int distinctCount = sparse[0].Select(kvp => kvp.Key)
                .Concat(sparse[1].Select(kvp => kvp.Key))
                .Distinct()
                .Count();
            
            coefficients = new(sparse[0]);
            coefficients.EnsureCapacity(distinctCount);
            foreach (var kvp in sparse[1])
            {
                if (coefficients.ContainsKey(kvp.Key))
                {
                    coefficients[kvp.Key] -= kvp.Value;
                }
                else
                {
                    coefficients.Add(kvp.Key, (short)-kvp.Value);
                }
            }
        }

        public IDictionary<int, short> Coefficients => coefficients;
        public SparseArray<short>[] Sparse => sparse;
        public short Phase => phase;

        public short Compute(HceWeights weights, int start = MATERIAL, int end = FEATURE_SIZE)
        {
            try
            {
                Score computeScore = Score.Zero;
                foreach (var coeff in coefficients)
                {
                    if (coeff.Key >= start && coeff.Key < end)
                    {
                        computeScore += coeff.Value * weights[coeff.Key];
                    }
                }
                int score = computeScore.NormalizeScore(phase);
                return Evaluation2.StmScore(sideToMove, score);
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.ToString());
                throw new Exception("EvalFeatures.Compute error occurred.", ex);
            }
        }

        public Color SideToMove => sideToMove;

        public static short GetOptimizationIncrement(int index)
        {
            return index switch
            {
                MATERIAL + (int)Piece.King => 0,
                MATERIAL + (int)Piece.King + FEATURE_SIZE => 0,
                >= MATERIAL and < (MATERIAL + (int)Piece.King) => 5,
                >= (MATERIAL + FEATURE_SIZE) and < (MATERIAL + (int)Piece.King + FEATURE_SIZE) => 5,
                _ => 1
            };
        }

#pragma warning disable CA1854
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementPieceCount(IDictionary<int, short> v, Piece piece)
        {
            if (piece == Piece.King)
            {
                return;
            }

            int index = MATERIAL + (int)piece;
            if (v.ContainsKey(index))
            {
                v[index]++;
            }
            else
            {
                v.Add(index, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetPieceSquare(IDictionary<int, short> v, Piece piece, KingPlacement kp, int square)
        {
            
            //int index = PIECE_SQUARE_TABLES + ((((int)piece << 2) + (int)kp) << 6) + square;
            int index = PIECE_SQUARE_TABLES + (int)piece * 256 + (int)kp * 64 + square;
            v[index] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementMobility(IDictionary<int, short> v, Piece piece, short mobility)
        {
            int key = MOBILITY + (int)piece - 1;
            if (v.ContainsKey(key))
            {
                v[key] += mobility;
            }
            else
            {
                v.Add(key, mobility);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementKingAttack(IDictionary<int, short> v, int d, short count)
        {
            if (count <= 0)
            {
                return;
            }

            int key = KING_ATTACK + d;
            if (v.ContainsKey(key))
            {
                v[key] += count;
            }
            else
            {
                v.Add(key, count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementCenterControl(IDictionary<int, short> v, int d, short count)
        {
            if (count <= 0)
            {
                return;
            }
            int key = CENTER_CONTROL + d;
            if (v.ContainsKey(key))
            {
                v[key] += count;
            }
            else
            {
                v.Add(key, count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetPawnShield(IDictionary<int, short> v, int d, short count)
        {
            v[PAWN_SHIELD + d] = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementIsolatedPawns(IDictionary<int, short> v)
        {
            if (v.ContainsKey(ISOLATED_PAWNS))
            {
                v[ISOLATED_PAWNS]++;
            }
            else
            {
                v.Add(ISOLATED_PAWNS, 1);
            }
        }

        private static void IncrementBackwardPawns(IDictionary<int, short> v)
        {
            if (v.ContainsKey(BACKWARD_PAWNS))
            {
                v[BACKWARD_PAWNS]++;
            }
            else
            {
                v.Add(BACKWARD_PAWNS, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementDoubledPawns(IDictionary<int, short> v, short count)
        {
            if (v.ContainsKey(DOUBLED_PAWNS))
            {
                v[DOUBLED_PAWNS] += count;
            }
            else
            {
                v.Add(DOUBLED_PAWNS, count);
            }
        }

        private static void SetPassedPawns(IDictionary<int, short> v, int square)
        {
            v[PASSED_PAWNS + square] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetAdjacentPawns(IDictionary<int, short> v, int square)
        {
            v[ADJACENT_PAWNS + square] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementKnightsOnOutpost(IDictionary<int, short> v)
        {
            if (v.ContainsKey(KNIGHTS_ON_OUTPOST))
            {
                v[KNIGHTS_ON_OUTPOST]++;
            }
            else
            {
                v.Add(KNIGHTS_ON_OUTPOST, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementBishopsOnOutpost(IDictionary<int, short> v)
        {
            if (v.ContainsKey(BISHOPS_ON_OUTPOST))
            {
                v[BISHOPS_ON_OUTPOST]++;
            }
            else
            {
                v.Add(BISHOPS_ON_OUTPOST, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetBishopPair(IDictionary<int, short> v)
        {
            v[BISHOP_PAIR] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementRookOnOpenFile(IDictionary<int, short> v)
        {
            if (v.ContainsKey(ROOK_OPEN_FILE))
            {
                v[ROOK_OPEN_FILE]++;
            }
            else
            {
                v.Add(ROOK_OPEN_FILE, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementRookOnHalfOpenFile(IDictionary<int, short> v)
        {
            if (v.ContainsKey(ROOK_HALF_OPEN_FILE))
            {
                v[ROOK_HALF_OPEN_FILE]++;
            }
            else
            {
                v.Add(ROOK_HALF_OPEN_FILE, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementRookBehindPassedPawn(IDictionary<int, short> v)
        {
            if (v.ContainsKey(ROOK_BEHIND_PASSED_PAWN))
            {
                v[ROOK_BEHIND_PASSED_PAWN]++;
            }
            else
            {
                v.Add(ROOK_BEHIND_PASSED_PAWN, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementDoubledRook(IDictionary<int, short> v)
        {
            if (v.ContainsKey(DOUBLED_ROOKS_ON_FILE))
            {
                v[DOUBLED_ROOKS_ON_FILE]++;
            }
            else
            {
                v.Add(DOUBLED_ROOKS_ON_FILE, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetKingOnOpenFile(IDictionary<int, short> v)
        {
            v[KING_ON_OPEN_FILE] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetKingOnHalfOpenFile(IDictionary<int, short> v)
        {
            v[KING_ON_HALF_OPEN_FILE] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetCastlingComplete(IDictionary<int, short> v)
        {
            v[CASTLING_COMPLETE] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetCastlingAvailable(IDictionary<int, short> v, short count)
        {
            v[CASTLING_AVAILABLE] = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementQueenOnOpenFile(IDictionary<int, short> v)
        {
            if (v.ContainsKey(QUEEN_OPEN_FILE))
            {
                v[QUEEN_OPEN_FILE]++;
            }
            else
            {
                v.Add(QUEEN_OPEN_FILE, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementQueenOnHalfOpenFile(IDictionary<int, short> v)
        {
            if (v.ContainsKey(QUEEN_HALF_OPEN_FILE))
            {
                v[QUEEN_HALF_OPEN_FILE]++;
            }
            else
            {
                v.Add(QUEEN_HALF_OPEN_FILE, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementRookOnSeventhRank(IDictionary<int, short> v)
        {
            if (v.ContainsKey(ROOK_ON_7TH_RANK))
            {
                v[ROOK_ON_7TH_RANK]++;
            }
            else
            {
                v.Add(ROOK_ON_7TH_RANK, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBadBishopPawn(IDictionary<int, short> v, int square) 
        {
            v[BAD_BISHOP_PAWN + square] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementBlockPassedPawn(IDictionary<int, short> v, int rank, Piece piece)
        {
            int index = BLOCK_PASSED_PAWN + (int)piece * Constants.MAX_COORDS + rank;
            if (v.ContainsKey(index))
            {
                v[index]++;
            }
            else
            {
                v.Add(index, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSupportedPawn(IDictionary<int, short> v, int square)
        {
            v[SUPPORTED_PAWN + square] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementKingOutsideSquare(IDictionary<int, short> v)
        {
            if (v.ContainsKey(KING_OUTSIDE_SQUARE))
            {
                v[KING_OUTSIDE_SQUARE]++;
            }
            else
            {
                v.Add(KING_OUTSIDE_SQUARE, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementPPFriendlyKingDistance(IDictionary<int, short> v, int dist)
        {
            if (v.ContainsKey(PP_FRIENDLY_KING_DIST))
            {
                v[PP_FRIENDLY_KING_DIST] += (short)dist;
            }
            else
            {
                v.Add(PP_FRIENDLY_KING_DIST, (short)dist);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementPPEnemyKingDistance(IDictionary<int, short> v, int dist)
        {
            if (v.ContainsKey(PP_ENEMY_KING_DIST))
            {
                v[PP_ENEMY_KING_DIST] += (short)dist;
            }
            else
            {
                v.Add(PP_ENEMY_KING_DIST, (short)dist);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetPawnRam(IDictionary<int, short> v, int square)
        {
            v[PAWN_RAM + square] = 1;
        }

        public static void IncrementPieceThreat(IDictionary<int, short> v, Piece attacker, Piece defender, short count = 1)
        {
            int index = PIECE_THREAT + (int)attacker * Constants.MAX_PIECES + (int)defender;
            if (v.ContainsKey(index))
            {
                v[index] += count;
            }
            else
            {
                v[index] = 1;
            }
        }

        public static void IncrementPawnPushThreat(IDictionary<int, short> v, Piece defender)
        {
            int index = PAWN_PUSH_THREAT + (int)defender;
            if (v.ContainsKey(index))
            {
                v[index]++;
            }
            else
            {
                v[index] = 1;
            }
        }

        public static void IncrementKingOnOpenDiagonal(IDictionary<int, short> v)
        {
            if (v.ContainsKey(KING_ON_OPEN_DIAGONAL))
            {
                v[KING_ON_OPEN_DIAGONAL]++;
            }
            else
            {
                v[KING_ON_OPEN_DIAGONAL] = 1;
            }
        }

#if PP_CAN_ADVANCE
        public static void IncrementPPCanAdvance(IDictionary<int, short> v, int rank)
        {
            rank -= Coord.RANK_4;
            int key = PP_CAN_ADVANCE + rank;
            if (v.ContainsKey(key))
            {
                v[key]++;
            }
            else
            {
                v[key] = 1;
            }
        }
#endif

#pragma warning restore CA1854
    }
}
