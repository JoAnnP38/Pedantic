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

using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public sealed class History : IHistory
    {
        private Color sideToMove = Color.White;
        private readonly int[] history = new int[Constants.MAX_COLORS * Constants.MAX_SQUARES * Constants.MAX_SQUARES];

        public Color SideToMove
        {
            get => sideToMove;
            set => sideToMove = value;
        }

        public int this[int from, int to] => history[GetIndex(from, to)];

        public int this[Color color, int from, int to] => history[GetIndex(color, from, to)];

        public void Update(Color color, int from, int to, int value)
        {
            int i = GetIndex(color, from, to);
            history[i] += value;
            if (history[i] >= Constants.HISTORY_SCORE)
            {
                Rescale();
            }
        }

        public void Update(int from, int to, int value)
        {
            Update(SideToMove, from, to, value);
        }

        public void Clear()
        {
            Array.Clear(history);
        }

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
            for (int i = 0; i < history.Length; ++i)
            {
                history[i] >>= 1;
            }
        }
    }
}
