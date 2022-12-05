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
        public MoveList() : base(64)
        { }

        public void Sort()
        {
            int largest = -1;
            int score = -1;
            for (int i = 0; i < insertIndex; ++i)
            {
                int mvScore = Move.GetScore(array[i]);
                if (mvScore > score)
                {
                    largest = i;
                    score = mvScore;
                }
            }

            if (largest > 0)
            {
                (array[0], array[largest]) = (array[largest], array[0]);
            }
        }

        public void Add(int from, int to, MoveType type = MoveType.Normal, Piece capture = Piece.None,
            Piece promote = Piece.None, int score = 0)
        {
            Add(Move.PackMove(from, to, type, capture, promote, score));
        }
    }
}
