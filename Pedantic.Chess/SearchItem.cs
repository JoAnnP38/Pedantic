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
        public short Eval;
        public short[]? Continuation;
        public uint Excluded;

        public SearchItem()
        {
            Move = (uint)Chess.Move.NullMove;
            IsCheckingMove = false;
            IsPromotionThreat = false;
            Eval = Constants.NO_SCORE;
            Continuation = null;
            Excluded = 0;
        }
    }
}
