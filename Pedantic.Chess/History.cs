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
        public const int HISTORY_LEN = Constants.MAX_COLORS * Constants.MAX_PIECES * Constants.MAX_SQUARES;
        private Color sideToMove = Color.White;
        private readonly short[] history = new short[HISTORY_LEN];
        private readonly uint[] counters = new uint[HISTORY_LEN];
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
                cmPiece = Move.GetPiece(lastMove);
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

        public void Update(ulong move, short bonus)
        {
            Update(SideToMove, Move.GetPiece(move), Move.GetTo(move), bonus);
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

        public unsafe void RescaleHistory()
        {
            fixed (short* p = &history[0])
            {
                Span<short> hist = new(p, HISTORY_LEN);
                for (int i = 0; i < HISTORY_LEN; i += 16)
                {
                    hist[i + 0] >>= 1;
                    hist[i + 1] >>= 1;
                    hist[i + 2] >>= 1;
                    hist[i + 3] >>= 1;
                    hist[i + 4] >>= 1;
                    hist[i + 5] >>= 1;
                    hist[i + 6] >>= 1;
                    hist[i + 7] >>= 1;
                    hist[i + 8] >>= 1;
                    hist[i + 9] >>= 1;
                    hist[i + 10] >>= 1;
                    hist[i + 11] >>= 1;
                    hist[i + 12] >>= 1;
                    hist[i + 13] >>= 1;
                    hist[i + 14] >>= 1;
                    hist[i + 15] >>= 1;
                }
            }
        }
    }
}
