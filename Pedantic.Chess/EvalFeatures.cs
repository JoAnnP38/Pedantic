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
using Pedantic.Collections;
using Pedantic.Utilities;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public sealed class EvalFeatures
    {
        // values required to determine phase and for mopup eval
        private readonly Color sideToMove;
        private readonly short[] material = new short[Constants.MAX_COLORS];
        private readonly sbyte[] kingIndex = new sbyte[Constants.MAX_COLORS];
        private readonly sbyte totalPawns;

        /*
         * Array/Vector of features (one per game phase)
         * [0]              # game phase material (not used in dot-product)
         * [1]              # pawns
         * [2]              # knights
         * [3]              # bishops
         * [4]              # rooks
         * [5]              # queens
         * [6]              # kings
         * [7 - 262]        0-1 pawn on square & king placement
         * [263 - 518]      0-1 knight on square & king placement
         * [519 - 774]      0-1 bishop on square & king placement
         * [775 - 1030]     0-1 rook on square & king placement
         * [1031 - 1286]    0-1 queen on square & king placement
         * [1287 - 1542]    0-1 king on square & king placement
         * [1543]           # knight mobility
         * [1544]           # bishop mobility
         * [1545]           # rook mobility
         * [1546]           # queen mobility
         * [1547 - 1549]    # king attack (d0 - d2)
         * [1550 - 1552]    # pawn shield (d0 - d2)
         * [1553]           # isolated pawns
         * [1554]           # backward pawns
         * [1555]           # doubled pawns
         * [1556]           # connected/adjacent pawns
         * [1557]           # passed pawns
         * [1558]           # knights on outpost
         * [1559]           # bishops on outpost
         * [1560]           0-1 bishop pair
         * [1561]           # rooks on open file
         * [1562]           # rooks on half-open file
         * [1563]           # rooks behind passed pawn
         * [1564]           # doubled rooks on file
         * [1565]           0-1 king on open file
         * [1566]           0-1 king on half-open file
         * [1567]           # of potential castle moves available
         * [1568]           0-1 side has already castled
         * [1569 - 1570]    # center control (d0 - d1)
         * [1571]           # queens on open file
         * [1572]           # queens on half-open file
         * [1573]           # rooks on seventh rank
         */
        public const int FEATURE_SIZE = 1582;
        public const int GAME_PHASE_BOUNDARY = 0;
        public const int MATERIAL = 1;
        public const int PIECE_SQUARE_TABLES = 7;
        public const int MOBILITY = 1543;
        public const int KING_ATTACK = 1547;
        public const int PAWN_SHIELD = 1550;
        public const int ISOLATED_PAWNS = 1553;
        public const int BACKWARD_PAWNS = 1554;
        public const int DOUBLED_PAWNS = 1555;
        public const int ADJACENT_PAWNS = 1556;
        public const int KING_ADJACENT_OPEN_FILE = 1557;
        public const int KNIGHTS_ON_OUTPOST = 1558;
        public const int BISHOPS_ON_OUTPOST = 1559;
        public const int BISHOP_PAIR = 1560;
        public const int ROOK_OPEN_FILE = 1561;
        public const int ROOK_HALF_OPEN_FILE = 1562;
        public const int ROOK_BEHIND_PASSED_PAWN = 1563;
        public const int DOUBLED_ROOKS_ON_FILE = 1564;
        public const int KING_ON_OPEN_FILE = 1565;
        public const int KING_ON_HALF_OPEN_FILE = 1566;
        public const int CASTLING_AVAILABLE = 1567;
        public const int CASTLING_COMPLETE = 1568;
        public const int CENTER_CONTROL = 1569;
        public const int QUEEN_OPEN_FILE = 1571;
        public const int QUEEN_HALF_OPEN_FILE = 1572;
        public const int ROOK_ON_7TH_RANK = 1573;
        public const int PASSED_PAWNS = 1574;

        private readonly SparseArray<short>[] sparse = { new(), new() };
		private readonly short[][] features = { Array.Empty<short>(), Array.Empty<short>() };
		private readonly int[][] indexMap = { Array.Empty<int>(), Array.Empty<int>() };
        private readonly short[][] openingWts = { Array.Empty<short>(), Array.Empty<short>() };
        private readonly short[][] endGameWts = { Array.Empty<short>(), Array.Empty<short>() };

        public EvalFeatures(Board bd)
        {
            Span<short> mobility = stackalloc short[Constants.MAX_PIECES];
            Span<short> kingAttacks = stackalloc short[3];
            Span<short> centerControl = stackalloc short[3];

            totalPawns = (sbyte)BitOps.PopCount(bd.Pieces(Color.White, Piece.Pawn) | bd.Pieces(Color.Black, Piece.Pawn));
            sideToMove = bd.SideToMove;
            kingIndex[0] = (sbyte)BitOps.TzCount(bd.Pieces(Color.White, Piece.King));
            kingIndex[1] = (sbyte)BitOps.TzCount(bd.Pieces(Color.Black, Piece.King));


            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int c = (int)color;
                int o = (int)color.Other();
                KingPlacement kp = Index.GetKingPlacement(kingIndex[c], kingIndex[o]);
                var v = sparse[c];


                for (int index = 0; index < Constants.MAX_SQUARES; index++)
                {
                    Square square = bd.PieceBoard[index];
                    int pstIndex = Index.NormalizedIndex[c][index];
                    if (!square.IsEmpty && square.Color == color)
                    {
                        IncrementPieceCount(v, square.Piece);
                        SetPieceSquare(v, square.Piece, kp, pstIndex);
                    }
                }

                bd.GetPieceMobility(color, mobility, kingAttacks, centerControl);
                for (Piece pc = Piece.Knight; pc <= Piece.Queen; pc++)
                {
                    int p = (int)pc;
                    if (mobility[p] > 0)
                    {
                        SetMobility(v, pc, mobility[p]);
                    }
                }

                for (int d = 0; d < 3; d++)
                {
                    if (kingAttacks[d] > 0)
                    {
                        SetKingAttack(v, d, kingAttacks[d]);
                    }
                }

                for (int d = 0; d < 2; d++)
                {
                    if (centerControl[d] > 0)
                    {
                        SetCenterControl(v, d, centerControl[d]);
                    }
                }

                material[c] = bd.MaterialNoKing(color);
                int ki = kingIndex[c];
                Color other = (Color)(c ^ 1);
                ulong pawns = bd.Pieces(color, Piece.Pawn);
                ulong otherPawns = bd.Pieces(other, Piece.Pawn);
                bd.Pieces(color, Piece.King);

                for (ulong p = pawns; p != 0; p = BitOps.ResetLsb(p))
                {
                    int sq = BitOps.TzCount(p);
                    Ray ray = Board.Vectors[sq];
                    ulong doubledFriends = color == Color.White ? ray.North : ray.South;
                    if ((otherPawns & Evaluation.PassedPawnMasks[c, sq]) == 0 && (pawns & doubledFriends) == 0)
                    {
                        IncrementPassedPawns(v, Index.GetRank(Index.NormalizedIndex[c][sq]));

                        ulong bb;
                        if (color == Color.White)
                        {
                            bb = BitOps.AndNot(ray.South, Board.RevVectors[BitOps.LzCount(ray.South & bd.All)].South);
                        }
                        else
                        {
                            bb = BitOps.AndNot(ray.North, Board.Vectors[BitOps.TzCount(ray.North & bd.All)].North);
                        }
                        if ((bb & bd.Pieces(color, Piece.Rook)) != 0)
                        {
                            IncrementRookBehindPassedPawn(v);
                        }
                    }

                    if ((pawns & Evaluation.IsolatedPawnMasks[sq]) == 0)
                    {
                        IncrementIsolatedPawns(v);
                    }

                    if ((pawns & Evaluation.BackwardPawnMasks[c, sq]) == 0)
                    {
                        IncrementBackwardPawns(v);
                    }

                    if ((pawns & Evaluation.AdjacentPawnMasks[sq]) != 0)
                    {
                        IncrementAdjacentPawns(v);
                    }
                }

                for (int file = 0; file < Constants.MAX_COORDS && pawns != 0; file++)
                {
                    short count = (short)BitOps.PopCount(pawns & Board.MaskFile(file));
                    if ( count > 1)
                    {
                        IncrementDoubledPawns(v, --count);
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

                for (int d = 0; d < 3; d++)
                {
                    short count = (short)BitOps.PopCount(pawns & Evaluation.KingProximity[d, ki]);
                    if (count > 0)
                    {
                        SetPawnShield(v, d, count);
                    }
                }

                ulong allPawns = pawns | otherPawns;
                ulong rooks = bd.Pieces(color, Piece.Rook);

                for (ulong bb = rooks; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    int rank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                    ulong maskFile = Board.MaskFile(sq);
                    ulong maskRank = Board.MaskRank(sq);
                    ulong potentials = maskFile & rooks;
                    int enemyKingRank = Index.GetRank(Index.NormalizedIndex[c][kingIndex[o]]);

                    if (rank == Coord.RANK_7 && ((otherPawns & maskRank) != 0 || enemyKingRank >= Coord.RANK_7))
                    {
                        IncrementRookOnSeventhRank(v);
                    }

                    if ((maskFile & allPawns) == 0)
                    {
                        IncrementRookOnOpenFile(v);

                        if (BitOps.PopCount(potentials) > 1 && Evaluation.IsDoubled(bd, potentials))
                        {
                            IncrementDoubledRook(v);
                        }
                    }

                    if ((maskFile & pawns) == 0 && (maskFile & otherPawns) != 0)
                    {
                        IncrementRookOnHalfOpenFile(v);

                        if (BitOps.PopCount(potentials) > 1 && Evaluation.IsDoubled(bd, potentials))
                        {
                            IncrementDoubledRook(v);
                        }
                    }
                }

                ulong queens = bd.Pieces(color, Piece.Queen);

                for (ulong bb = queens; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    ulong mask = Board.MaskFile(sq);

                    if ((mask & allPawns) == 0)
                    {
                        IncrementQueenOnOpenFile(v);
                    }

                    if ((mask & pawns) == 0 && (mask & otherPawns) != 0)
                    {
                        IncrementQueenOnHalfOpenFile(v);
                    }
                }

                int kingFile = Index.GetFile(ki);
                ulong kingFileMask = Board.MaskFile(kingFile);
                if ((kingFileMask & allPawns) == 0)
                {
                    SetKingOnOpenFile(v);
                }

                if ((kingFileMask & pawns) == 0 && (kingFileMask & otherPawns) != 0)
                {
                    SetKingOnHalfOpenFile(v);
                }

                if (kingFile > Coord.FILE_A && (Board.MaskFile(kingFile - 1) & allPawns) == 0)
                {
                    IncrementKingAdjacentOpenFile(v);
                }

                if (kingFile < Coord.FILE_H && (Board.MaskFile(kingFile + 1) & allPawns) == 0)
                {
                    IncrementKingAdjacentOpenFile(v);
                }

                if (bd.HasCastled[c])
                {
                    SetCastlingComplete(v);
                }
                else
                {
                    ulong mask = (ulong)CastlingRights.WhiteRights << (c << 1);
                    short cntRights = (short)BitOps.PopCount((ulong)bd.Castling & mask);
                    SetCastlingAvailable(v, cntRights);
                }

                int length = sparse[c].Count;
				features[c] = new short[length];
				indexMap[c] = new int[length];
                openingWts[c] = new short[length];
                endGameWts[c] = new short[length];

				int i = 0;
				foreach (var kvp in sparse[c])
				{
					features[c][i] = kvp.Value;
					indexMap[c][i++] = kvp.Key;
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

                MapWeights(opWeights, egWeights);

                for (Color color = Color.White; color <= Color.Black; color++)
                {
                    int c = (int)color;
                    opScore[c] = DotProduct(features[c], openingWts[c]);
                    egScore[c] = DotProduct(features[c], endGameWts[c]);
                }

                GamePhase gamePhase = GetGamePhase(opWeights[GAME_PHASE_BOUNDARY],
                    egWeights[GAME_PHASE_BOUNDARY], out int opWt, out int egWt);

                short score = (short)((((opScore[0] - opScore[1]) * opWt) >> 7 /* / 128 */) +
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

		private void MapWeights(ReadOnlySpan<short> opWeights, ReadOnlySpan<short> egWeights)
		{
			for (int c = 0; c < Constants.MAX_COLORS; c++)
			{
				for (int n = 0; n < indexMap[c].Length; n++)
				{
					openingWts[c][n] = opWeights[indexMap[c][n]];
                    endGameWts[c][n] = egWeights[indexMap[c][n]];
				}
			}
		}
        
        public static short GetOptimizationIncrement(int index)
        {
            return index switch
            {
                GAME_PHASE_BOUNDARY => 50,
                GAME_PHASE_BOUNDARY + FEATURE_SIZE => 50,
                MATERIAL + (int)Piece.King => 0,
                MATERIAL + (int)Piece.King + FEATURE_SIZE => 0,
                >= MATERIAL and < (MATERIAL + (int)Piece.King) => 5,
                >= (MATERIAL + FEATURE_SIZE) and < (MATERIAL + (int)Piece.King + FEATURE_SIZE) => 5,
                _ => 1
            };
        }

        private static short DotProduct(ReadOnlySpan<short> f, ReadOnlySpan<short> weights)
        {
            int results = 0;
            if (f.Length >= Vector<short>.Count)
            {
                int remaining = f.Length % Vector<short>.Count;

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

        private GamePhase GetGamePhase(short openingMaterial, short endGameMaterial, out int opWt, out int egWt)
        {
            GamePhase phase = GamePhase.Opening;
            opWt = 128;
            egWt = 0;
            int totalMaterial = material[0] + material[1];


            if (totalMaterial < endGameMaterial)
            {
                phase = totalPawns == 0 ? GamePhase.EndGameMopup : GamePhase.EndGame;
                opWt = 0;
                egWt = 128;
            }
            else if (totalMaterial < openingMaterial && totalMaterial >= endGameMaterial)
            {
                phase = GamePhase.MidGame;
                int rngMaterial = openingMaterial - endGameMaterial;
                int curMaterial = totalMaterial - endGameMaterial;
                opWt = (curMaterial * 128) / rngMaterial;
                egWt = 128 - opWt;
            }

            return phase;
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
            
            int index = PIECE_SQUARE_TABLES + ((((int)piece << 2) + (int)kp) << 6) + square;
            v[index] = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetMobility(IDictionary<int, short> v, Piece piece, short mobility)
        {
            int p = (int)piece - 1;
            v[MOBILITY + p] = mobility;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetKingAttack(IDictionary<int, short> v, int d, short count)
        {
            v[KING_ATTACK + d] = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetCenterControl(IDictionary<int, short> v, int d, short count)
        {
            v[CENTER_CONTROL + d] = count;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private static void IncrementPassedPawns(IDictionary<int, short> v, int rank)
        {
            if (v.ContainsKey(PASSED_PAWNS + rank))
            {
                v[PASSED_PAWNS + rank]++;
            }
            else
            {
                v.Add(PASSED_PAWNS + rank, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IncrementAdjacentPawns(IDictionary<int, short> v)
        {
            if (v.ContainsKey(ADJACENT_PAWNS))
            {
                v[ADJACENT_PAWNS]++;
            }
            else
            {
                v.Add(ADJACENT_PAWNS, 1);
            }
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
        public static void IncrementKingAdjacentOpenFile(IDictionary<int, short> v)
        {
            if (v.ContainsKey(KING_ADJACENT_OPEN_FILE))
            {
                v[KING_ADJACENT_OPEN_FILE]++;
            }
            else
            {
                v.Add(KING_ADJACENT_OPEN_FILE, 1);
            }
        }

#pragma warning restore CA1854
    }
}
