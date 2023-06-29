// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 03-18-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="EvalWeights.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Provides methods to get named weights without affecting the 
//     database mapping in class <c>ChessWeights</c>.
// </summary>
// ***********************************************************************
using Pedantic.Genetics;
using Pedantic.Utilities;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public sealed class EvalWeights
    {
        public EvalWeights(ChessWeights weights)
        {
            wt = ArrayEx.Clone(weights.Weights);
        }

        public short[] Weights => wt;
        public short OpeningPhaseMaterial => wt[ChessWeights.GAME_PHASE_MATERIAL];
        public short EndGamePhaseMaterial => wt[ChessWeights.GAME_PHASE_MATERIAL + ChessWeights.ENDGAME_WEIGHTS];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short OpeningPieceValues(Piece piece)
        {
            int offset = (int)piece;
            return wt[ChessWeights.PIECE_VALUES + offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short EndGamePieceValues(Piece piece)
        {
            int offset = (int)piece;
            return wt[ChessWeights.PIECE_VALUES + ChessWeights.ENDGAME_WEIGHTS + offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short OpeningPieceSquareTable(Piece piece, KingPlacement placement, int square)
        {
            int offset = ((((int)piece << 2) + (int)placement) << 6) + square;
            return wt[ChessWeights.PIECE_SQUARE_TABLE + offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short EndGamePieceSquareTable(Piece piece, KingPlacement placement,int square)
        {
            int offset = ((((int)piece << 2) + (int)placement) << 6) + square;
            return wt[ChessWeights.PIECE_SQUARE_TABLE + ChessWeights.ENDGAME_WEIGHTS + offset];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short OpeningPieceMobility(Piece piece)
        {
            const int start = ChessWeights.PIECE_MOBILITY;
            return wt[start + (int)piece - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short EndGamePieceMobility(Piece piece)
        {
            const int start = ChessWeights.PIECE_MOBILITY + ChessWeights.ENDGAME_WEIGHTS;
            return wt[start + (int)piece - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short OpeningKingAttack(int distance)
        {
            const int start = ChessWeights.KING_ATTACK;
            return wt[start + distance];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short EndGameKingAttack(int distance)
        {
            const int start = ChessWeights.KING_ATTACK + ChessWeights.ENDGAME_WEIGHTS;
            return wt[start + distance];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short OpeningPawnShield(int distance)
        {
            const int start = ChessWeights.PAWN_SHIELD;
            return wt[start + distance];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short EndGamePawnShield(int distance)
        {
            const int start = ChessWeights.PAWN_SHIELD + ChessWeights.ENDGAME_WEIGHTS;
            return wt[start + distance];
        }

        public short OpeningIsolatedPawn => wt[ChessWeights.ISOLATED_PAWN];
        public short EndGameIsolatedPawn => wt[ChessWeights.ISOLATED_PAWN + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningBackwardPawn => wt[ChessWeights.BACKWARD_PAWN];
        public short EndGameBackwardPawn => wt[ChessWeights.BACKWARD_PAWN + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningDoubledPawn => wt[ChessWeights.DOUBLED_PAWN];
        public short EndGameDoubledPawn => wt[ChessWeights.DOUBLED_PAWN + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningConnectedPawn => wt[ChessWeights.CONNECTED_PAWN];
        public short EndGameConnectedPawn => wt[ChessWeights.CONNECTED_PAWN + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningPassedPawn => wt[ChessWeights.PASSED_PAWN];
        public short EndGamePassedPawn => wt[ChessWeights.PASSED_PAWN + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningKnightOutpost => wt[ChessWeights.KNIGHT_OUTPOST];
        public short EndGameKnightOutpost => wt[ChessWeights.KNIGHT_OUTPOST + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningBishopOutpost => wt[ChessWeights.BISHOP_OUTPOST];
        public short EndGameBishopOutpost => wt[ChessWeights.BISHOP_OUTPOST + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningBishopPair => wt[ChessWeights.BISHOP_PAIR];
        public short EndGameBishopPair => wt[ChessWeights.BISHOP_PAIR + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningRookOnOpenFile => wt[ChessWeights.ROOK_ON_OPEN_FILE];
        public short EndGameRookOnOpenFile => wt[ChessWeights.ROOK_ON_OPEN_FILE + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningRookOnHalfOpenFile => wt[ChessWeights.ROOK_ON_HALF_OPEN_FILE];
        public short EndGameRookOnHalfOpenFile => wt[ChessWeights.ROOK_ON_HALF_OPEN_FILE + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningRookBehindPassedPawn => wt[ChessWeights.ROOK_BEHIND_PASSED_PAWN];
        public short EndGameRookBehindPassedPawn => wt[ChessWeights.ROOK_BEHIND_PASSED_PAWN + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningDoubledRooks => wt[ChessWeights.DOUBLED_ROOKS_ON_FILE];
        public short EndGameDoubledRooks => wt[ChessWeights.DOUBLED_ROOKS_ON_FILE + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningKingOnOpenFile => wt[ChessWeights.KING_ON_OPEN_FILE];
        public short EndGameKingOnOpenFile => wt[ChessWeights.KING_ON_OPEN_FILE + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningKingOnHalfOpenFile => wt[ChessWeights.KING_ON_HALF_OPEN_FILE];
        public short EndGameKingOnHalfOpenFile => wt[ChessWeights.KING_ON_HALF_OPEN_FILE + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningCastlingAvailable => wt[ChessWeights.CASTLING_AVAILABLE];
        public short EndGameCastlingAvailable => wt[ChessWeights.CASTLING_AVAILABLE + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningCastlingComplete => wt[ChessWeights.CASTLING_COMPLETE];
        public short EndGameCastlingComplete => wt[ChessWeights.CASTLING_COMPLETE + ChessWeights.ENDGAME_WEIGHTS];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short OpeningCenterControl(int distance)
        {
            return wt[ChessWeights.CENTER_CONTROL + distance];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short EndGameCenterControl(int distance)
        {
            return wt[ChessWeights.CENTER_CONTROL + ChessWeights.ENDGAME_WEIGHTS + distance];
        }

        public short OpeningQueenOnOpenFile => wt[ChessWeights.QUEEN_ON_OPEN_FILE];
        public short EndGameQueenOnOpenFile => wt[ChessWeights.QUEEN_ON_OPEN_FILE + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningQueenOnHalfOpenFile => wt[ChessWeights.QUEEN_ON_HALF_OPEN_FILE];
        public short EndGameQueenOnHalfOpenFile => wt[ChessWeights.QUEEN_ON_HALF_OPEN_FILE + ChessWeights.ENDGAME_WEIGHTS];
        public short OpeningRookOnSeventhRank => wt[ChessWeights.ROOK_ON_7TH_RANK];
        public short EndGameRookOnSeventhRank => wt[ChessWeights.ROOK_ON_7TH_RANK + ChessWeights.ENDGAME_WEIGHTS];

        private readonly short[] wt;
    }
}
