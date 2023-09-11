// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 03-18-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Evaluation.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Class Evaluation is used to evaluate a static chess position.
// </summary>
// ***********************************************************************
using Pedantic.Genetics;
using Pedantic.Utilities;
using System.Runtime.CompilerServices;
using Pedantic.Collections;
using System.Numerics;
using System.Drawing;

namespace Pedantic.Chess
{
    public sealed class Evaluation
    {
        public const ulong D0_CENTER_CONTROL_MASK = 0x0000001818000000ul;
        public const ulong D1_CENTER_CONTROL_MASK = 0x00003C24243C0000ul;
        public const ulong DARK_SQUARES_MASK = 0xAA55AA55AA55AA55ul;
        public const ulong LITE_SQUARES_MASK = 0x55AA55AA55AA55AAul;

        static Evaluation()
        {
            wt = new EvalWeights(LoadWeights());
        }

        public Evaluation(bool adjustMaterial = true, bool random = false, bool useMopUp = false)
        {
            this.adjustMaterial = adjustMaterial;
            this.random = random;
            this.useMopUp = useMopUp;
        }

        public bool ShowIntermediateResults
        {
            get => showIntermediateResults;
            set => showIntermediateResults = value;
        }

        public short Compute(Board board, int alpha = -Constants.INFINITE_WINDOW, int beta = Constants.INFINITE_WINDOW)
        {
            if (TtEval.TryGetScore(board.Hash, out short score))
            {
                return score;
            }

            ClearScores();
            totalPawns = BitOps.PopCount(board.Pieces(Color.White, Piece.Pawn) | board.Pieces(Color.Black, Piece.Pawn));
            passedPawns = 0;
            kingIndex[0] = BitOps.TzCount(board.Pieces(Color.White, Piece.King));
            kingIndex[1] = BitOps.TzCount(board.Pieces(Color.Black, Piece.King));

            kp[0] = Index.GetKingPlacement(kingIndex[0], kingIndex[1]);
            kp[1] = Index.GetKingPlacement(kingIndex[1], kingIndex[0]);
            currentPhase = GetGamePhase(board, out opWt, out egWt);

            bool isLazy = false;
            score = currentPhase == GamePhase.EndGameMopup ? ComputeMopUp(board) : ComputeNormal(board, alpha, beta, ref isLazy);
            score = board.SideToMove == Color.White ? score : (short)-score;
            if (!isLazy)
            {
                TtEval.Add(board.Hash, score);
            }
            return score;
        }

        // A variation of the Chess 4.5 MopUp evaluation (+14 Elo)
        public short ComputeMopUp(Board board)
        {
            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int c = (int)color;
                Color other = color.Other();
                int o = (int)other;
                egScore[c] += board.EndGameMaterial[c];

                if (color == winning)
                {
                    short[] mate = mopupMate;
                    /*if ((board.Units(other) ^ board.Pieces(other, Piece.King)) == 0 &&
                        BitOps.PopCount(board.Pieces(color, Piece.Bishop)) == 1 &&
                        BitOps.PopCount(board.Pieces(color, Piece.Knight)) == 1 &&
                        (board.Pieces(color, Piece.Rook) | board.Pieces(color, Piece.Queen)) == 0)
                    {
                        int bishopIndex = BitOps.TzCount(board.Pieces(color, Piece.Bishop));
                        if (Index.IsDark(bishopIndex))
                        {
                            mate = mopupMateNBDark;
                        }
                        else
                        {
                            mate = mopupMateNBLight;
                        }
                    }*/
                    short mopup = mate[kingIndex[o]];
                    mopup += (short)((14 - Index.ManhattanDistance(kingIndex[c], kingIndex[o])) * 10);

                    for (ulong bb = board.Pieces(winning, Piece.Knight); bb != 0; bb = BitOps.ResetLsb(bb))
                    {
                        int index = BitOps.TzCount(bb);
                        mopup += (short)((14 - Index.ManhattanDistance(index, kingIndex[o])) * 10);
                    }
                    egScore[c] += mopup;
                }
            }

            return (short)(egScore[0] - egScore[1]);
        }

        public short ComputeNormal(Board board, int alpha, int beta, ref bool isLazy)
        {
            opScore[0] = AdjustMaterial(board.OpeningMaterial[0], adjust[0]);
            egScore[0] = AdjustMaterial(board.EndGameMaterial[0], adjust[0]);
            opScore[1] = AdjustMaterial(board.OpeningMaterial[1], adjust[1]);
            egScore[1] = AdjustMaterial(board.EndGameMaterial[1], adjust[1]);

            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int c = (int)color;
                
                for (Piece piece = Piece.Pawn; piece <= Piece.King; piece++)
                {
                    for (ulong bb = board.Pieces(color, piece); bb != 0; bb = BitOps.ResetLsb(bb))
                    {
                        int sq = BitOps.TzCount(bb);
                        opScore[c] += wt.OpeningPieceSquareTable(piece, kp[c], Index.NormalizedIndex[c][sq]);
                        egScore[c] += wt.EndGamePieceSquareTable(piece, kp[c], Index.NormalizedIndex[c][sq]);
                    }
                }
            }

            int score = ((opScore[0] - opScore[1]) * opWt + (egScore[0] - egScore[1]) * egWt) / Constants.MAX_PHASE;

            isLazy = true;
            int evalScore = ((int)board.SideToMove * -2 + 1) * score;
            if (evalScore >= alpha - Constants.LAZY_EVAL_MARGIN && evalScore <= beta + Constants.LAZY_EVAL_MARGIN)
            {
                isLazy = false;
                bool pawnsHashed = TtPawnEval.TryLookup(board.PawnHash, out TtPawnEval.TtPawnItem item);

                ComputeKingAttacks(Color.White, board);
                ComputeKingAttacks(Color.Black, board);

                if (pawnsHashed)
                {
                    opScore[0] += item.GetOpeningScore(Color.White);
                    egScore[0] += item.GetEndGameScore(Color.White);
                    opScore[1] += item.GetOpeningScore(Color.Black);
                    egScore[1] += item.GetEndGameScore(Color.Black);
                    passedPawns = PassedPawnBitboard(board);
                }
                else if (totalPawns > 0)
                {
                    ComputePawns(Color.White, board);
                    ComputePawns(Color.Black, board);
                    opScore[0] += opPawnScore[0];
                    egScore[0] += egPawnScore[0];
                    opScore[1] += opPawnScore[1];
                    egScore[1] += egPawnScore[1];
                }

                if (!pawnsHashed)
                {
                    TtPawnEval.Add(board.PawnHash, opPawnScore, egPawnScore);
                }

                ComputeMisc(Color.White, board);
                ComputeMisc(Color.Black, board);

                score = ((opScore[0] - opScore[1]) * opWt + (egScore[0] - egScore[1]) * egWt) / Constants.MAX_PHASE;

                if (random)
                {
                    score += (short)Random.Shared.Next(-8, 9);
                }
            }

            (bool whiteCanWin, bool blackCanWin) = CanWin(board);
            if ((score > 0 && !whiteCanWin) || (score < 0 && !blackCanWin))
            {
                score >>= 3;
            }
            else if (board.HalfMoveClock > 84)
            {
                score = (short)((score * Math.Min(100 - Math.Min(board.HalfMoveClock, 100), 16)) >> 4);
            }

            return (short)score;
        }

        /// <summary>
        /// Determines whether this instance can win the specified board.
        /// </summary>
        /// <remarks>
        /// This function implements an estimate on whether a side can win;
        /// however, it is simple. We can extend this with more detailed
        /// heuristic in a later release.
        /// </remarks>
        /// <param name="board">The board.</param>
        /// <returns>System.ValueTuple&lt;System.Boolean, System.Boolean&gt;.</returns>
        public static (bool WhiteCanWin, bool BlackCanWin) CanWin(Board board)
        {
            bool whiteCanWin = false, blackCanWin = false;
            short materialWhite = board.EndGameMaterial[(int)Color.White];
            short materialBlack = board.EndGameMaterial[(int)Color.Black];
            short pawnValue = EndGamePieceValues(Piece.Pawn);

            if (board.Pieces(Color.White, Piece.Pawn) != 0 ||
                materialWhite - materialBlack >= (pawnValue << 2))
            {
                whiteCanWin = true;
            }

            if (board.Pieces(Color.Black, Piece.Pawn) != 0 ||
                materialBlack - materialWhite >= (pawnValue << 2))
            {
                blackCanWin = true;
            }

            return (whiteCanWin, blackCanWin);
        }

        public short AdjustMaterial(short material, int adjustWt)
        {
            return adjustMaterial ? (short)((material * adjustWt) >> 5) : material;
        }

        public void ComputeKingAttacks(Color color, Board board)
        {
            Span<short> mobility = stackalloc short[Constants.MAX_PIECES];
            Span<short> kingAttacks = stackalloc short[3];
            Span<short> centerControl = stackalloc short[2];

            int c = (int)color;
            int o = (int)color.Other();
            board.GetPieceMobility(color, mobility, kingAttacks, centerControl);
            opScore[c] += (short)(mobility[(int)Piece.Knight] * wt.OpeningPieceMobility(Piece.Knight));
            egScore[c] += (short)(mobility[(int)Piece.Knight] * wt.EndGamePieceMobility(Piece.Knight));

            opScore[c] += (short)(mobility[(int)Piece.Bishop] * wt.OpeningPieceMobility(Piece.Bishop));
            egScore[c] += (short)(mobility[(int)Piece.Bishop] * wt.EndGamePieceMobility(Piece.Bishop));

            opScore[c] += (short)(mobility[(int)Piece.Rook] * wt.OpeningPieceMobility(Piece.Rook));
            egScore[c] += (short)(mobility[(int)Piece.Rook] * wt.EndGamePieceMobility(Piece.Rook));

            opScore[c] += (short)(mobility[(int)Piece.Queen] * wt.OpeningPieceMobility(Piece.Queen));
            egScore[c] += (short)(mobility[(int)Piece.Queen] * wt.EndGamePieceMobility(Piece.Queen));

            opScore[c] += (short)(kingAttacks[0] * wt.OpeningKingAttack(0));
            opScore[c] += (short)(kingAttacks[1] * wt.OpeningKingAttack(1));
            opScore[c] += (short)(kingAttacks[2] * wt.OpeningKingAttack(2));

            egScore[c] += (short)(kingAttacks[0] * wt.EndGameKingAttack(0));
            egScore[c] += (short)(kingAttacks[1] * wt.EndGameKingAttack(1));
            egScore[c] += (short)(kingAttacks[2] * wt.EndGameKingAttack(2));

            opScore[c] += (short)(centerControl[0] * wt.OpeningCenterControl(0));
            opScore[c] += (short)(centerControl[1] * wt.OpeningCenterControl(1));

            egScore[c] += (short)(centerControl[0] * wt.EndGameCenterControl(0));
            egScore[c] += (short)(centerControl[1] * wt.EndGameCenterControl(1));
        }

        public void ComputePawns(Color color, Board board)
        {
            int c = (int)color;
            int o = c ^ 1;
            Color other = (Color)o;

            ulong pawns = board.Pieces(color, Piece.Pawn);
            if (pawns == 0)
            {
                return;
            }

            ulong otherPawns = board.Pieces(other, Piece.Pawn);

            for (ulong p = pawns; p != 0; p = BitOps.ResetLsb(p))
            {
                int sq = BitOps.TzCount(p);
                Ray ray = Board.Vectors[sq];
                ulong doubledFriends = color == Color.White ? ray.North : ray.South;
                int normalSq = Index.NormalizedIndex[c][sq];

                if ((otherPawns & PassedPawnMasks[c, sq]) == 0 && (pawns & doubledFriends) == 0)
                {
                    opPawnScore[c] += wt.OpeningPassedPawn(normalSq);
                    egPawnScore[c] += wt.EndGamePassedPawn(normalSq);
                    passedPawns |= BitOps.GetMask(sq);
                }

                if ((pawns & IsolatedPawnMasks[sq]) == 0)
                {
                    opPawnScore[c] += wt.OpeningIsolatedPawn;
                    egPawnScore[c] += wt.EndGameIsolatedPawn;
                }

                if ((pawns & AdjacentPawnMasks[sq]) != 0)
                {
                    opPawnScore[c] += wt.OpeningConnectedPawn(normalSq);
                    egPawnScore[c] += wt.EndGameConnectedPawn(normalSq);
                }

                if ((pawns & BackwardPawnMasks[c, sq]) == 0)
                {
                    opPawnScore[c] += wt.OpeningBackwardPawn;
                    egPawnScore[c] += wt.EndGameBackwardPawn;
                }
            }

            for (int file = 0; file < Constants.MAX_COORDS; file++)
            {
                short count = (short)BitOps.PopCount(pawns & Board.MaskFile(file));
                if (count > 1)
                {
                    opPawnScore[c] += (short)((count - 1) * wt.OpeningDoubledPawn);
                    egPawnScore[c] += (short)((count - 1) * wt.EndGameDoubledPawn);
                }
            }

            ulong pawnAttacks;
            if (color == Color.White)
            {
                pawnAttacks = ((pawns & ~Board.MaskFile(Index.A1)) << 7) |
                              ((pawns & ~Board.MaskFile(Index.H1)) << 9);
            }
            else
            {
                pawnAttacks = ((pawns & ~Board.MaskFile(Index.H1)) >> 7) |
                              ((pawns & ~Board.MaskFile(Index.A1)) >> 9);
            }

            for (ulong p = pawns & pawnAttacks; p != 0; p = BitOps.ResetLsb(p))
            {
                int sq = BitOps.TzCount(p);
                int normalSq = Index.NormalizedIndex[c][sq];
                opPawnScore[c] += wt.OpeningSupportedPawn(normalSq);
                egPawnScore[c] += wt.EndGameSupportedPawn(normalSq);
            }

            ulong pawnRams = pawns & (color == Color.White ? otherPawns >> 8 : otherPawns << 8);
            for (ulong p = pawnRams; p != 0; p = BitOps.ResetLsb(p))
            {
                int normalSq = Index.NormalizedIndex[c][BitOps.TzCount(p)];
                opPawnScore[c] += wt.OpeningPawnRam(normalSq);
                egPawnScore[c] += wt.EndGamePawnRam(normalSq);
            }
        }

        public void ComputeMisc(Color color, Board board)
        {
            int c = (int)color;
            Color other = (Color)(c ^ 1);
            int o = (int)other;
            ulong pawns = board.Pieces(color, Piece.Pawn);
            ulong otherPawns = board.Pieces(other, Piece.Pawn);

            if (BitOps.PopCount(board.Pieces(color, Piece.Bishop)) >= 2)
            {
                opScore[c] += wt.OpeningBishopPair;
                egScore[c] += wt.EndGameBishopPair;
            }

            ulong knights = board.Pieces(color, Piece.Knight);
            ulong bishops = board.Pieces(color, Piece.Bishop);

            for (ulong bb = knights | bishops; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                if (normalRank > 3 && (Board.PawnDefends(color, sq) & pawns) != 0)
                {
                    Piece pc = board.PieceBoard[sq].Piece;
                    if (pc == Piece.Knight)
                    {
                        opScore[c] += wt.OpeningKnightOutpost;
                        egScore[c] += wt.EndGameKnightOutpost;
                    }
                    else
                    {
                        opScore[c] += wt.OpeningBishopOutpost;
                        egScore[c] += wt.EndGameKnightOutpost;
                    }
                }
            }

            for (ulong bbBishop = bishops; bbBishop != 0; bbBishop = BitOps.ResetLsb(bbBishop))
            { 
                int sq = BitOps.TzCount(bbBishop);
                ulong badPawns = pawns & DARK_SQUARES_MASK;
                if (!Index.IsDark(sq))
                {
                    badPawns = pawns & LITE_SQUARES_MASK;
                }

                for (ulong bbBadPawn = badPawns; bbBadPawn != 0; bbBadPawn = BitOps.ResetLsb(bbBadPawn))
                {
                    int pawnSq = BitOps.TzCount(bbBadPawn);
                    int normalSq = Index.NormalizedIndex[c][pawnSq];
                    opScore[c] += wt.OpeningBadBishopPawn(normalSq);
                    egScore[c] += wt.EndGameBadBishopPawn(normalSq);
                }
            }

            int ki = kingIndex[c];
            opScore[c] += (short)(BitOps.PopCount(pawns & KingProximity[0, ki]) * wt.OpeningPawnShield(0));
            opScore[c] += (short)(BitOps.PopCount(pawns & KingProximity[1, ki]) * wt.OpeningPawnShield(1));
            opScore[c] += (short)(BitOps.PopCount(pawns & KingProximity[2, ki]) * wt.OpeningPawnShield(2));
            egScore[c] += (short)(BitOps.PopCount(pawns & KingProximity[0, ki]) * wt.EndGamePawnShield(0));
            egScore[c] += (short)(BitOps.PopCount(pawns & KingProximity[1, ki]) * wt.EndGamePawnShield(1));
            egScore[c] += (short)(BitOps.PopCount(pawns & KingProximity[2, ki]) * wt.EndGamePawnShield(2));

            // NOTE: passedPawns was initialized during ComputePawns()
            for (ulong p = passedPawns & board.Units(color); p != 0; p = BitOps.ResetLsb(p))
            {
                int sq = BitOps.TzCount(p);
                Ray ray = Board.Vectors[sq];
                ulong bb = color == Color.White ?
                    BitOps.AndNot(ray.South, Board.RevVectors[BitOps.LzCount(ray.South & board.All)].South) :
                    BitOps.AndNot(ray.North, Board.Vectors[BitOps.TzCount(ray.North & board.All)].North);

                if ((bb & board.Pieces(color, Piece.Rook)) != 0)
                {
                    opScore[c] += wt.OpeningRookBehindPassedPawn;
                    egScore[c] += wt.EndGameRookBehindPassedPawn;
                }

                int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                if (normalRank < Coord.RANK_4)
                {
                    continue;
                }

                int promoteSq = Index.NormalizedIndex[c][Index.ToIndex(Index.GetFile(sq), Coord.RANK_8)];
                if (board.PieceCount(other) == 1 && 
                    Index.Distance(sq, promoteSq) < Index.Distance(kingIndex[o], promoteSq) - (other == board.SideToMove ? 1 : 0))
                {
                    opScore[c] += wt.OpeningKingOutsideSquare;
                    egScore[c] += wt.EndGameKingOutsideSquare;
                }

                int blockSq = Board.PawnPlus[c, sq];
                int dist = Index.Distance(blockSq, kingIndex[c]);
                opScore[c] += (short)(dist * wt.OpeningPpFriendlyKingDistance);
                egScore[c] += (short)(dist * wt.EndGamePpFriendlyKingDistance);

                dist = Index.Distance(blockSq, kingIndex[o]);
                opScore[c] += (short)(dist * wt.OpeningPpEnemyKingDistance);
                egScore[c] += (short)(dist * wt.EndGamePpEnemyKingDistance);
            }

            for (ulong p = passedPawns & board.Units(other); p != 0; p = BitOps.ResetLsb(p))
            {
                int sq = BitOps.TzCount(p);
                int blockerSq = Board.PawnPlus[o, sq];
                int normalRank = Index.GetRank(Index.NormalizedIndex[o][sq]);
                Square blocker = board.PieceBoard[blockerSq];
                if (blocker.Color == color && blocker.Piece != Piece.None)
                {
                    opScore[c] += wt.OpeningBlockPassedPawn(normalRank, blocker.Piece);
                    egScore[c] += wt.EndGameBlockPassedPawn(normalRank, blocker.Piece);
                }
            }

            ulong allPawns = pawns | otherPawns;
            ulong rooks = board.Pieces(color, Piece.Rook);

            for (ulong bb = rooks; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                int rank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                int enemyKingRank = Index.GetRank(Index.NormalizedIndex[c][kingIndex[o]]);
                ulong maskFile = Board.MaskFile(sq);
                ulong maskRank = Board.MaskRank(sq);
                ulong potentials = maskFile & rooks;

                if (rank == Coord.RANK_7 && ((otherPawns & maskRank) != 0 || enemyKingRank >= Coord.RANK_7))
                {
                    opScore[c] += wt.OpeningRookOnSeventhRank;
                    egScore[c] += wt.EndGameRookOnSeventhRank;
                }
                
                if ((maskFile & allPawns) == 0)
                {
                    opScore[c] += wt.OpeningRookOnOpenFile;
                    egScore[c] += wt.EndGameRookOnOpenFile;


                    if (BitOps.PopCount(potentials) > 1 && IsDoubled(board, potentials))
                    {
                        opScore[c] += wt.OpeningDoubledRooks;
                        egScore[c] += wt.EndGameDoubledRooks;
                    }
                }

                if ((maskFile & pawns) == 0 && (maskFile & otherPawns) != 0)
                {
                    opScore[c] += wt.OpeningRookOnHalfOpenFile;
                    egScore[c] += wt.EndGameRookOnHalfOpenFile;

                    if (BitOps.PopCount(potentials) > 1 && IsDoubled(board, potentials))
                    {
                        opScore[c] += wt.OpeningDoubledRooks;
                        egScore[c] += wt.EndGameDoubledRooks;
                    }
                }
            }

            ulong queens = board.Pieces(color, Piece.Queen);
            for (ulong bb = queens; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                ulong mask = Board.MaskFile(sq);

                if ((mask & allPawns) == 0)
                {
                    opScore[c] += wt.OpeningQueenOnOpenFile;
                    egScore[c] += wt.EndGameQueenOnOpenFile;
                }

                if ((mask & pawns) == 0 && (mask & otherPawns) != 0)
                {
                    opScore[c] += wt.OpeningQueenOnHalfOpenFile;
                    egScore[c] += wt.EndGameQueenOnHalfOpenFile;
                }
            }

            int kingFile = Index.GetFile(ki);
            if ((Board.MaskFile(kingFile) & allPawns) == 0)
            {
                opScore[c] += wt.OpeningKingOnOpenFile;
                egScore[c] += wt.EndGameKingOnOpenFile;
            }

            if ((Board.MaskFile(kingFile) & pawns) == 0 && (Board.MaskFile(kingFile) & otherPawns) != 0)
            {
                opScore[c] += wt.OpeningKingOnHalfOpenFile;
                egScore[c] += wt.EndGameKingOnHalfOpenFile;
            }

            if (board.HasCastled[(int)color])
            {
                opScore[c] += wt.OpeningCastlingComplete;
                egScore[c] += wt.EndGameCastlingComplete;
            }
            else
            {
                ulong mask = (ulong)CastlingRights.WhiteRights << (c << 1);
                short cntRights = (short)BitOps.PopCount((ulong)board.Castling & mask);
                opScore[c] += (short)(cntRights * wt.OpeningCastlingAvailable);
                egScore[c] += (short)(cntRights * wt.EndGameCastlingAvailable);
            }
        }

        public void CalcMaterialAdjustment(Board board)
        {
            int materialWhite = board.EndGameMaterial[(int)Color.White];
            int materialBlack = board.EndGameMaterial[(int)Color.Black];
            int pawnValue = EndGamePieceValues(Piece.Pawn);

            ulong move = board.LastMove;
            int seeValue = 0;
            if (Move.IsCapture(move))
            {
                seeValue = board.PostMoveStaticExchangeEval(board.SideToMove.Other(), move);
                seeValue = board.SideToMove == Color.White ? seeValue : -seeValue;
            }

            if (materialWhite > 0 && materialBlack > 0 && Math.Abs(materialWhite - materialBlack + seeValue) >= pawnValue * 2)
            {
                adjust[0] = Math.Max(Math.Min((materialBlack * 32) / materialWhite, 32), 31);
                adjust[1] = Math.Max(Math.Min((materialWhite * 32) / materialBlack, 32), 31);
            }
            else
            {
                adjust[0] = 32;
                adjust[1] = 32;
            }
        }

        public GamePhase GetGamePhase(Board board, out int opWt, out int egWt)
        {
            opWt = board.Phase;
            egWt = Constants.MAX_PHASE - board.Phase;
            GamePhase gamePhase = board.GamePhase;

            if (gamePhase == GamePhase.EndGame)
            {
                short materialWhite = board.EndGameMaterial[(int)Color.White];
                short materialBlack = board.EndGameMaterial[(int)Color.Black];
                short pawnValue = EndGamePieceValues(Piece.Pawn);

                if (useMopUp &&
                    (board.Pieces(Color.White, Piece.Pawn) | board.Pieces(Color.Black, Piece.Pawn)) == 0ul && 
                    Math.Abs(materialWhite - materialBlack) >= (pawnValue << 2) &&
                    Math.Min(materialWhite, materialBlack) <= pawnValue * 7)
                {
                    winning = materialWhite > materialBlack ? Color.White : Color.Black;

                    int numKnights = BitOps.PopCount(board.Pieces(winning, Piece.Knight));
                    int numBishops = BitOps.PopCount(board.Pieces(winning, Piece.Bishop));
                    bool case1 = BitOps.PopCount(board.Pieces(winning, Piece.Queen) | board.Pieces(winning, Piece.Rook)) >= 1;
                    bool case2 = (numKnights >= 1 && numBishops >= 1) || numBishops >= 2 || numKnights >= 3;

                    if (case1 || case2)
                    {
                        gamePhase = GamePhase.EndGameMopup;
                    }
                }
            }

            return gamePhase;
        }

        public void ClearScores()
        {
            Array.Clear(opScore);
            Array.Clear(egScore);
            Array.Clear(opPawnScore);
            Array.Clear(egPawnScore);
        }

        public ulong PassedPawnBitboard(Board board)
        {
            ulong passers = 0;
            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int c = (int)color;
                ulong pawns = board.Pieces(color, Piece.Pawn);
                ulong otherPawns = board.Pieces(color.Other(), Piece.Pawn);

                for (ulong p = pawns; p != 0; p = BitOps.ResetLsb(p))
                {
                    int sq = BitOps.TzCount(p);
                    Ray ray = Board.Vectors[sq];
                    ulong doubledFriends = color == Color.White ? ray.North : ray.South;

                    if ((otherPawns & PassedPawnMasks[c, sq]) == 0 && (pawns & doubledFriends) == 0)
                    {
                        passers |= BitOps.GetMask(sq);
                    }
                }
            }
            return passers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCheckmate(int score)
        {
            int absScore = Math.Abs(score);
            return absScore is >= Constants.CHECKMATE_SCORE - Constants.MAX_PLY * 2 and <= Constants.CHECKMATE_SCORE;
        }

        public static ChessWeights LoadWeights(string? id = null)
        {
            var rep = new ChessDb();
            ChessWeights? w = (string.IsNullOrEmpty(id)
                ? rep.Weights.FirstOrDefault(cw => cw.IsActive && cw.IsImmortal)
                : rep.Weights.FirstOrDefault(cw => cw.Id == new Guid(id)));

            if (w == null)
            {
                w = ChessWeights.CreateParagon();
                rep.Weights.Insert(w);
                rep.Save();
            }
            wt = new EvalWeights(w);
            return w;
        }

        public static ChessWeights LoadWeights(Guid? id)
        {
            var rep = new ChessDb();
            ChessWeights? w = id == null
                ? rep.Weights.FirstOrDefault(cw => cw.IsActive && cw.IsImmortal)
                : rep.Weights.FirstOrDefault(cw => cw.Id == id);

            if (w == null)
            {
                w = ChessWeights.CreateParagon();
                rep.Weights.Insert(w);
                rep.Save();
            }
            wt = new EvalWeights(w);
            return w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short CanonicalPieceValues(Piece piece)
        {
            return canonicalPieceValues[(int)piece + 1];
        }

        public static short PiecePhaseValue(Piece piece)
        {
            return piecePhaseValues[(int)piece + 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short OpeningPieceValues(Piece piece)
        {
            return wt.OpeningPieceValues(piece);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short EndGamePieceValues(Piece piece)
        {
            return wt.EndGamePieceValues(piece);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short OpeningPieceSquareTable(Piece piece, KingPlacement placement, int square)
        {
            return wt.OpeningPieceSquareTable(piece, placement, square);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short EndGamePieceSquareTable(Piece piece, KingPlacement placement,int square)
        {
            return wt.EndGamePieceSquareTable(piece, placement,square);
        }

        public static bool IsDoubled(Board bd, ulong piecesOnFile)
        {
            int sq1 = BitOps.TzCount(piecesOnFile);
            piecesOnFile = BitOps.ResetLsb(piecesOnFile);
            int sq2 = BitOps.TzCount(piecesOnFile);
            int file1 = Index.GetFile(sq1);
            int file2 = Index.GetFile(sq2);
            ref Ray ray1 = ref Board.Vectors[sq1];
            ref Ray ray2 = ref Board.Vectors[sq2];

            return file1 == file2 &&
                   (((ray1.South & ray2.North) | (ray1.North & ray2.South)) & bd.All) == 0ul;
        }

        public static int CenterDistance(int sq)
        {
            Index.ToCoords(sq, out int file, out int rank);
            file ^= (file-4) >> 8;
            rank ^= (rank-4) >> 8;
            return (file + rank) & 7;
        }

        private GamePhase currentPhase;
        private int opWt, egWt;
        private Color winning;
        private readonly bool useMopUp;

        public static short[] Weights => wt.Weights;

        private bool showIntermediateResults = false;
        private readonly bool adjustMaterial;
        private readonly bool random;
        private int totalPawns;
        private ulong passedPawns;
        private readonly int[] adjust = { 32, 32 };
        private readonly int[] kingIndex = new int[2];
        private readonly KingPlacement[] kp = new KingPlacement[2];
        private readonly short[] opScore = { 0, 0 };
        private readonly short[] egScore = { 0, 0 };
        private readonly short[] opPawnScore = { 0, 0 };
        private readonly short[] egPawnScore = { 0, 0 };

        private static EvalWeights wt;
        private static readonly UnsafeArray<short> canonicalPieceValues = new (7) 
            { 0, 100, 300, 300, 500, 900, 9900 };

        public static readonly UnsafeArray2D<ulong> PassedPawnMasks = new (Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region PassedPawnMasks data

            // white passed pawn masks
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
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,

            // black passed pawn masks
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

            #endregion PassedPawnMasks data
        };

        public static readonly sbyte[] piecePhaseValues = { 0, 1, 2, 2, 4, 8, 0 };

        public static readonly UnsafeArray<ulong> IsolatedPawnMasks = new (Constants.MAX_SQUARES)
        {
            #region IsolatedPawnMasks data
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
            #endregion IsolatedPawnMasks data
        };

        public static readonly UnsafeArray2D<ulong> KingProximity = new (3, Constants.MAX_SQUARES)
        {
            #region kingProximity data

            // masks for D0
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
            0x2838000000000000ul, 0x5070000000000000ul, 0xA0E0000000000000ul, 0x40C0000000000000ul,

            // masks for D1
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
            0x44447C0000000000ul, 0x8888F80000000000ul, 0x1010F00000000000ul, 0x2020E00000000000ul,

            // masks for D2
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

            #endregion kingProximity data
        };

        public static readonly UnsafeArray2D<ulong> BackwardPawnMasks = new (Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region BackwardPawnMasks data

            // masks for white backward pawns
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
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,

            // masks for black backward pawns
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

            #endregion BackwardPawnMasks data
        };

        public static readonly UnsafeArray<ulong> AdjacentPawnMasks = new (Constants.MAX_SQUARES)
        {
            #region AdjacentPawnMasks data

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

            #endregion AdjacentPawnMasks data
        };

        public static readonly short[] mopupMate =
        {
            140, 120, 100,  80,  80, 100, 120, 140,
            120, 100,  60,  40,  40,  60, 100, 120,
            100,  60,  20,   0,   0,  20,  60, 100,
             80,  40,   0,   0,   0,   0,  40,  80,
             80,  40,   0,   0,   0,   0,  40,  80,
            100,  60,  20,   0,   0,  20,  60, 100,
            120, 100,  60,  40,  40,  60, 100, 120,
            140, 120, 100,  80,  80, 100, 120, 140
        };

        public static readonly short[] mopupMateNBLight =
        {
             40,  40,  60,  80,  80, 100, 120, 140,
             40,  20,  20,  40,  40,  60, 100, 120,
             60,  20,   0,   0,   0,  20,  60, 100,
             80,  40,   0,   0,   0,   0,  40,  80,
             80,  40,   0,   0,   0,   0,  40,  80,
            100,  60,  20,   0,   0,   0,  40,  60,
            120, 100,  60,  40,  40,  20,  20,  40,
            140, 120, 100,  80,  80,  60,  40,  40
        };

        public static readonly short[] mopupMateNBDark =
        {
            140, 120, 100,  80,  80,  60,  40,  40,
            120, 100,  60,  40,  40,  20,  20,  40,
            100,  60,  20,   0,   0,   0,  20,  60,
             80,  40,   0,   0,   0,   0,  40,  80,
             80,  40,   0,   0,   0,   0,  40,  80,
             60,  40,   0,   0,   0,  20,  60, 100,
             40,  20,  20,  40,  40,  60, 100, 120,
             40,  40,  60,  80,  80, 100, 120, 140
        };
    }
}
