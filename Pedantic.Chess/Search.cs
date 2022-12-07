using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class Search
    {
        const int CHECK_TC_NODES = 50;

        private Board board;
        private TimeControl time;
        private long maxNodes;
        private short maxSearchDepth;
        private short depth = 0;
        private short score = 0;
        private ulong[] pv = Array.Empty<ulong>();
        private long nodesVisited = 0;
        private ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);

        public Search(Board board, TimeControl time, short maxSearchDepth, long maxNodes = long.MaxValue)
        {
            this.board = board;
            this.time = time;
            this.maxNodes = maxNodes;
            this.maxSearchDepth = maxSearchDepth;
        }

        private bool Aborted => nodesVisited > maxNodes || ((nodesVisited % CHECK_TC_NODES == 0) && time.CheckTimeBudget());

        private (short Score, ulong[] PV) ExecuteTt(short alpha, short beta, short depth)
        {
            if (TtEval.GetScore(board.Hash, depth, alpha, beta, out short score))
            {
                return (score, Array.Empty<ulong>());
            }

            var result = Execute(alpha, beta, depth);

            TtEval.Add(board.Hash, depth, alpha, beta, result.Score, result.PV.Length > 0 ? result.PV[0] : 0ul);

            // TODO: just to make it compile
            return (0, Array.Empty<ulong>());
        }

        private (short Score, ulong[] PV) Execute(short alpha, short beta, short depth)
        {
            if (depth == 0)
            {
                return (Quiesce(alpha, beta), Array.Empty<ulong>());
            }

            nodesVisited++;

            if (Aborted)
            {
                return (0, Array.Empty<ulong>());
            }

            bool inCheck = board.IsChecked();
            int expandedNodes = 0;

            MoveList moveList = moveListPool.Get();
            board.GenerateMoves(moveList);

            for (int n = 0; n < moveList.Count; ++n)
            {
                moveList.Sort(n);
                if (board.MakeMove(moveList[n]))
                {
                    expandedNodes++;
                    // TODO: just to make it compile
                    var result = ExecuteTt((short)-beta, (short)-alpha, (short)(depth - 1));
                }
            }

            moveListPool.Return(moveList);
            return (0, Array.Empty<ulong>());
        }

        private short Quiesce(short alpha, short beta)
        {
            nodesVisited++;

            if (Aborted)
            {
                return 0;
            }

            bool inCheck = board.IsChecked();
            if (!inCheck)
            {
                short standPat = /*Evaluation.Compute(board)*/ 0;
                if (standPat >= beta)
                {
                    return beta;
                }
                if (alpha < standPat)
                {
                    alpha = standPat;
                }
            }

            int expandedNodes = 0;
            MoveList moveList = moveListPool.Get();

            if (inCheck)
            {
                board.GenerateMoves(moveList);
            }
            else
            {
                board.GenerateCaptures(moveList);
            }
            for (int n = 0; n < moveList.Count; ++n)
            {
                moveList.Sort(n);
                if (board.MakeMove(moveList[n]))
                {
                    ++expandedNodes;
                    short score = (short)-Quiesce((short)-beta, (short)-alpha);
                    board.UnmakeMove();
                    if (score >= beta)
                    {
                        return beta;
                    }
                    if (score > alpha)
                    {
                        alpha = score;
                    }
                }
            }

            if (expandedNodes == 0 && inCheck)
            {
                return board.SideToMove == Color.White ? (short)-Constants.CHECKMATE_SCORE : Constants.CHECKMATE_SCORE;
            }

            return alpha;
        }
    }
}
