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

using Pedantic.Collections;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public sealed class History : IHistory
    {
        public const int HISTORY_LEN = Constants.MAX_COLORS * Constants.MAX_PIECES * Constants.MAX_SQUARES;
        private Color sideToMove = Color.White;
        private readonly short[] hhHistory = new short[HISTORY_LEN];
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

        public int this[Piece piece, int to]
        {
            get
            {
                return hhHistory[GetIndex(piece, to)];
            }
        }

        public void UpdateCutoff(ulong move, ref StackList<ulong> quiets, int depth)
        {
            short bonus = (short)(((depth * depth) >> 1) + (depth << 1) - 1);
            UpdateHistory(ref hhHistory[GetIndex(Move.GetPiece(move), Move.GetTo(move))], bonus);

            if (cmPiece != Piece.None)
            {
                counters[GetIndex(cmPiece, cmTo)] = (uint)Move.ClearScore(move);
            }

            for (int n = 0; n < quiets.Count; n++)
            {
                ulong quiet = quiets[n];
                UpdateHistory(ref hhHistory[GetIndex(Move.GetPiece(quiet), Move.GetTo(quiet))], (short)-bonus);
            }
        }

        public void Clear()
        {
            Array.Clear(hhHistory);
            Array.Clear(counters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateHistory(ref short hist, short bonus)
        {
            hist += (short)(bonus - hist * Math.Abs(bonus) / Constants.HISTORY_SCORE_MAX);
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

        private unsafe void Rescale()
        {
            fixed (short* p = &hhHistory[0])
            {
                for (int n = 0; n < HISTORY_LEN; n += 16)
                {
                    p[n + 0] >>= 1;
                    p[n + 1] >>= 1;
                    p[n + 2] >>= 1;
                    p[n + 3] >>= 1;
                    p[n + 4] >>= 1;
                    p[n + 5] >>= 1;
                    p[n + 6] >>= 1;
                    p[n + 7] >>= 1;
                    p[n + 8] >>= 1;
                    p[n + 9] >>= 1;
                    p[n + 10] >>= 1;
                    p[n + 11] >>= 1;
                    p[n + 12] >>= 1;
                    p[n + 13] >>= 1;
                    p[n + 14] >>= 1;
                    p[n + 15] >>= 1;
                }
            }
        }
    }
}
