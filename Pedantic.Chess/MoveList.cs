using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Collections;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class MoveList : ValueList<ulong>, IPooledObject<MoveList>
    {
        public MoveList() : base(Constants.AVG_MOVES_PER_PLY * 2)
        { }

        public void Sort(int n, ulong bestmove = 0)
        {
            int largest = -1;
            int score = -1;
            for (int i = n; i < insertIndex; ++i)
            {
                int mvScore = Move.GetScore(array[i]);
                if (mvScore > score)
                {
                    largest = i;
                    score = mvScore;
                }
            }

            if (largest > n)
            {
                (array[n], array[largest]) = (array[largest], array[n]);
            }
        }

        public void Add(int from, int to, MoveType type = MoveType.Normal, Piece capture = Piece.None,
            Piece promote = Piece.None, int score = 0)
        {
            Add(Move.PackMove(from, to, type, capture, promote, score));
        }

        public ReadOnlySpan<ulong> ToSpan() => new ReadOnlySpan<ulong>(array, 0, Count);

        public void UpdateScores(ulong pv, ulong[] killers)
        {
            byte flags = 0;
            for (int n = 0; n < insertIndex && flags < 7; ++n)
            {
                ulong fromto = array[n] & 0x0fff;
                bool isCapture = Move.GetCapture(array[n]) != Piece.None;

                if (fromto == (pv & 0x0fff))
                {
                    flags |= 1;
                    array[n] = BitOps.BitFieldSet(array[n], Constants.PV_SCORE, 24, 16);
                }
                else if (fromto == (killers[0] & 0x0fff) && !isCapture)
                {
                    flags |= 2; 
                    array[n] = BitOps.BitFieldSet(array[n], Constants.KILLER_0_SCORE, 24, 16);
                }
                else if (fromto == (killers[1] & 0x0fff) && !isCapture)
                {
                    flags |= 4;
                    array[n] = BitOps.BitFieldSet(array[n], Constants.KILLER_1_SCORE, 24, 16);
                }
            }
        }
    }
}
