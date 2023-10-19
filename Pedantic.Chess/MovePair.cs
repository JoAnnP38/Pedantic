using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public struct MovePair
    {
        private uint move1;
        private uint move2;

        public MovePair()
        {
            move1 = 0;
            move2 = 0;
        }

        public readonly ulong Move1 => move1;
        public readonly ulong Move2 => move2;

        public void Add(ulong move)
        {
            if (Move.Compare(move, move2) == 0)
            {
                (move1, move2) = (move2, move1);
            }
            else
            {
                move2 = move1;
                move1 = (uint)move;
            }
        }
    }
}
