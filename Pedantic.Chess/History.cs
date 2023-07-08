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
        private readonly short[] history = new short[Constants.MAX_COLORS * Constants.MAX_SQUARES * Constants.MAX_SQUARES];
        private readonly uint[] counters = new uint[Constants.MAX_COLORS * Constants.MAX_SQUARES * Constants.MAX_SQUARES];
        private int cmFrom = Index.NONE;
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
                cmFrom = Move.GetFrom(lastMove);
                cmTo = Move.GetTo(lastMove);
            }
            else
            {
                cmFrom = Index.NONE;
                cmTo = Index.NONE;
            }
        }

        public ulong CounterMove
        {
            get
            {
                if (cmFrom == Index.NONE)
                {
                    return 0ul;
                }
                else
                {
                    return counters[GetIndex(cmFrom, cmTo)];
                }
            }
        }

        public short this[int from, int to] => history[GetIndex(from, to)];

        public short this[Color color, int from, int to] => history[GetIndex(color, from, to)];

        public void Update(Color color, int from, int to, short bonus)
        {
            int i = GetIndex(color, from, to);

            if (Math.Abs(history[i] + bonus) >= Constants.HISTORY_SCORE)
            {
                Rescale();
            }

            history[i] += bonus;
        }

        public void Update(int from, int to, short bonus)
        {
            Update(SideToMove, from, to, bonus);
        }

        public void Update(ulong move, short bonus)
        {
            Update(SideToMove, Move.GetFrom(move), Move.GetTo(move), bonus);
            if (cmFrom != Index.NONE)
            {
                counters[GetIndex(cmFrom, cmTo)] = (uint)Move.ClearScore(move);
            }
        }

        public void Clear()
        {
            Array.Clear(history);
            Array.Clear(counters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetIndex(int from, int to)
        {
            return GetIndex(sideToMove, from, to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(Color color, int from, int to)
        {
            return ((int)color << 12) + (from << 6) + to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rescale()
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
