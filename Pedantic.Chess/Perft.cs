using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class Perft
    {
        private Board board = new Board(Constants.FEN_START_POS);
        private ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);

        public void Initialize(string fen = Constants.FEN_START_POS)
        {
            board.LoadFenPosition(fen);
        }

        public ulong Execute(int depth)
        {
            if (depth == 0)
            {
                return 1;
            }

            ulong nodes = 0;
            MoveList moveList = moveListPool.Get();
            board.GenerateMoves(moveList);
            for (int n = 0; n < moveList.Count; ++n)
            {
                if (board.MakeMove(moveList[n]))
                {
                    if (depth > 1)
                    {
                        nodes += Execute(depth - 1);
                    }
                    else
                    {
                        nodes++;
                    }
                    board.UnmakeMove();
                }
            }
            moveListPool.Return(moveList);
            return nodes;
        }
    }
}
