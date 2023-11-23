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
using Pedantic.Utilities;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public sealed class History : IHistory
    {
        public const int HISTORY_LEN = Constants.MAX_COLORS * Constants.MAX_PIECES * Constants.MAX_SQUARES;
        public const short BONUS_MAX = 640;
        public const short BONUS_COEFF = 80;
        private readonly short[] hhHistory = new short[HISTORY_LEN];
        private readonly uint[] counters = new uint[HISTORY_LEN];
        private readonly short[][] contHist = Mem.Allocate2D<short>(HISTORY_LEN, HISTORY_LEN);
        private readonly SearchStack ss;
        private readonly short[] nullMoveCont;
        private int ply;

        public History(SearchStack stack)
        {
            ss = stack;
            nullMoveCont = GetContinuation(Color.White, Piece.Pawn, Index.A1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong CounterMove(ulong lastMove)
        {
            if (lastMove == 0 || lastMove == Move.NullMove)
            {
                return default;
            }
            return counters[GetIndex(lastMove)];
        }

        public short this[Color stm, Piece piece, int to]
        {
            get
            {
                int index = GetIndex(stm, piece, to);
                int value = hhHistory[index] + ss[ply - 1].Continuation![index];
                return (short)Math.Clamp(value, short.MinValue, short.MaxValue);
            }
        }

        public short this[ulong move]
        {
            get
            {
                int index = GetIndex(move);
                int value = hhHistory[index] + ss[ply - 1].Continuation![index];
                return (short)Math.Clamp(value, short.MinValue, short.MaxValue);
            }
        }

        public short[] NullMoveContinuation => nullMoveCont;

        public short MaxHistory => hhHistory.Max();
        public short MinHistory => hhHistory.Min();
        public short MaxContHistory
        {
            get
            {
                short max = short.MinValue;
                for (int n = 0; n < HISTORY_LEN; n++)
                {
                    max = Math.Max(max, contHist[n].Max());
                }
                return max;
            }
        }

        public short MinContHistory
        {
            get
            {
                short min = short.MaxValue;
                for (int n = 0; n < HISTORY_LEN; n++)
                {
                    min = Math.Min(min, contHist[n].Min());
                }
                return min;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetContext(int ply)
        {
            this.ply = ply;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short[] GetContinuation(Color stm, Piece piece, int to)
        {
            return contHist[GetIndex(stm, piece, to)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short[] GetContinuation(ulong move)
        {
            if (move == Move.NullMove)
            {
                return NullMoveContinuation;
            }
            return contHist[GetIndex(move)];
        }

        public void UpdateCutoff(ulong move, int currentPly, ref StackList<uint> quiets, int depth)
        {
            SetContext(currentPly);
            short bonus = Math.Min(BONUS_MAX, (short)(BONUS_COEFF * (depth - 1)));
            int index = GetIndex(move);
            UpdateHistory(ref hhHistory[index], bonus);
            UpdateHistory(ref ss[ply - 1].Continuation![index], bonus);

            ulong lastMove = ss[ply - 1].Move;
            if (lastMove != Move.NullMove)
            {
                counters[GetIndex(lastMove)] = (uint)move;
            }

            short malus = (short)-bonus;
            for (int n = 0; n < quiets.Count; n++)
            {
                ulong quiet = quiets[n];
                index = GetIndex(quiet);
                UpdateHistory(ref hhHistory[index], malus);
                UpdateHistory(ref ss[ply - 1].Continuation![index], malus);
            }
        }

        public void Clear()
        {
            Array.Clear(hhHistory);
            Array.Clear(counters);
            Mem.Clear(contHist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateHistory(ref short hist, short bonus)
        {
            hist += (short)(bonus - hist * Math.Abs(bonus) / Constants.HISTORY_SCORE_MAX);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(Color color, Piece piece, int to)
        {
            return ((int)color * 384) + ((int)piece * Constants.MAX_SQUARES) + to;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex(ulong move)
        {
            return ((int)Move.GetStm(move) * 384) + ((int)Move.GetPiece(move) * Constants.MAX_SQUARES) + Move.GetTo(move);
        }

        public unsafe void Rescale()
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
