using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public struct SearchItem
    {
        public uint Move;
        public bool IsCheckingMove;
        public bool IsPromotionThreat;
        public MovePair KillerMoves;

        public SearchItem()
        {
            Move = 0;
            IsCheckingMove = false;
            IsPromotionThreat = false;
        }
    }
}
