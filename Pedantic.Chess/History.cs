// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="History.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//      Helps supported better move ordering by recording which moves
//      originating "from" a square, to a given square result in a 
//      beta cutoff. Similar to a killerMove, but longer lasting but
//      not as precise.
// </summary>
// ***********************************************************************

using Pedantic.Utilities;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public sealed class History : IHistory
    {
        private Color sideToMove = Color.White;
        private readonly short[] history = new short[Constants.MAX_COLORS * Constants.MAX_PIECES * Constants.MAX_SQUARES];
        private readonly uint[] counters = new uint[Constants.MAX_COLORS * Constants.MAX_PIECES * Constants.MAX_SQUARES];
        private Piece cmPiece = Piece.None;
        private int cmTo = Index.NONE;

        public Color SideToMove
        {
            get => sideToMove;
            set => sideToMove = value;
        }

        public void SetContext(Board board)
        {
            SideToMove = board.SideToMove;
            ulong lastMove = board.LastMove;
            if (lastMove != Move.NullMove)
            {
                cmPiece = board.PieceBoard[Move.GetFrom(lastMove)].Piece;
                cmTo = Move.GetTo(lastMove);
            }
            else
            {
                cmPiece = Piece.None;
                cmTo = Index.NONE;
            }
        }

        public ulong CounterMove
        {
            get
            {
                if (cmPiece == Piece.None)
                {
                    return 0ul;
                }
                else
                {
                    return counters[GetIndex(cmPiece, cmTo)];
                }
            }
        }

        public short this[Piece piece, int to] => history[GetIndex(piece, to)];

        public short this[Color color, Piece piece, int to] => history[GetIndex(color, piece, to)];

        public void Update(Color color, Piece piece, int to, short bonus)
        {
            int i = GetIndex(color, piece, to);

            if (Math.Abs(history[i] + bonus) >= Constants.HISTORY_SCORE)
            {
                RescaleHistory();
            }

            history[i] += bonus;
        }

        public void Update(Board board, ulong move, short bonus)
        {
            Update(SideToMove, board.PieceBoard[Move.GetFrom(move)].Piece, Move.GetTo(move), bonus);
            if (cmPiece != Piece.None)
            {
                counters[GetIndex(cmPiece, cmTo)] = (uint)Move.ClearScore(move);
            }
        }

        public void Clear()
        {
            Array.Clear(history);
            Array.Clear(counters);
        }

        public void Rescale()
        {
            RescaleHistory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetIndex(Piece piece, int to)
        {
            return GetIndex(sideToMove, piece, to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(Color color, Piece piece, int to)
        {
            return ((int)color * 384) + ((int)piece * 64) + to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RescaleHistory()
        {
            for (int i = 0; i < history.Length; i += 8)
            {
                history[i + 0] >>= 1;
                history[i + 1] >>= 1;
                history[i + 2] >>= 1;
                history[i + 3] >>= 1;
                history[i + 4] >>= 1;
                history[i + 5] >>= 1;
                history[i + 6] >>= 1;
                history[i + 7] >>= 1;
            }
        }
    }
}
