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
        private readonly short[] hhHistory = new short[HISTORY_LEN];
        private readonly MovePair[] counters = new MovePair[HISTORY_LEN];

        public MovePair CounterMoves(ulong lastMove)
        {
            if (lastMove == 0 || lastMove == Move.NullMove)
            {
                return default;
            }
            return counters[GetIndex(Move.GetStm(lastMove), Move.GetPiece(lastMove), Move.GetTo(lastMove))];
        }

        public short this[Color stm, Piece piece, int to]
        {
            get
            {
                return hhHistory[GetIndex(stm, piece, to)];
            }
        }

        public short this[ulong move]
        {
            get
            {
                return hhHistory[GetIndex(Move.GetStm(move), Move.GetPiece(move), Move.GetTo(move))];
            }
        }

        public void UpdateCutoff(ulong move, int ply, ref StackList<uint> quiets, SearchStack searchStack, int depth)
        {
            short bonus = (short)(((depth * depth) >> 1) + (depth << 1) - 1);
            UpdateHistory(ref hhHistory[GetIndex(move)], bonus);

            ulong lastMove = searchStack[ply - 1].Move;
            if (lastMove != Move.NullMove)
            {
                counters[GetIndex(lastMove)].Add((uint)move);
            }

            for (int n = 0; n < quiets.Count; n++)
            {
                ulong quiet = quiets[n];
                UpdateHistory(ref hhHistory[GetIndex(quiet)], (short)-bonus);
            }
        }

        public void Clear()
        {
            Array.Clear(hhHistory);
            Array.Clear(counters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateHistory(ref short hist, short bonus)
        {
            hist += (short)(bonus - hist * Math.Abs(bonus) / Constants.HISTORY_SCORE_MAX);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(Color color, Piece piece, int to)
        {
            return ((int)color * 384) + ((int)piece * 64) + to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(ulong move)
        {
            return ((int)Move.GetStm(move) * 384) + ((int)Move.GetPiece(move) * 64) + Move.GetTo(move);
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
