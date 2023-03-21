using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Genetics;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class EvalWeights
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
        public short OpeningPieceSquareTable(Piece piece, int square)
        {
            int offset = (int)piece * 64 + square;
            return wt[ChessWeights.PIECE_SQUARE_TABLE + offset];
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
        public short EndGamePieceSquareTable(Piece piece, int square)
        {
            int offset = (int)piece * 64 + square;
            return wt[ChessWeights.PIECE_SQUARE_TABLE + ChessWeights.ENDGAME_WEIGHTS + offset];
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

        private readonly short[] wt;
    }
}
