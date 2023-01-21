namespace Pedantic.Chess
{
    public class MinimalSearch : SearchBase
    {
        public MinimalSearch(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100) 
            : base(board, time, maxSearchDepth, maxNodes)
        { }

        public override SearchResult Search(int alpha, int beta, int depth, int ply, bool canNull = true, bool isPv = true)
        {
            if (depth <= 0)
            {
                return new SearchResult(QuiesceTt(alpha, beta, ply), EmptyPv);
            }

            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return DefaultResult;
            }

            bool isChecked = board.IsChecked();
            bool allowNullMove = !Evaluation.IsCheckmate(Result.Score) || (ply > Depth / 4);

            if (allowNullMove && depth >= 2 && !isChecked && beta < Constants.INFINITE_WINDOW)
            {
                const int R = 2;
                if (board.MakeMove(Move.NullMove))
                {
                    SearchResult result = -SearchTt(-beta, -beta + 1, depth - R - 1, ply + 1);
                    board.UnmakeMove();
                    if (result.Score >= beta)
                    {
                        return new SearchResult(result.Score, EmptyPv);
                    }
                }
            }

            ulong[] pv = EmptyPv;
            int expandedNodes = 0;
            history.SideToMove = board.SideToMove;
            MoveList moveList = MoveListPool.Get();

            foreach (ulong move in board.Moves(ply, killerMoves, history, moveList))
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;
                bool interesting = expandedNodes == 1 || isChecked || board.IsChecked();

                if (ply > 0 && depth <= 4 && !interesting)
                {
                    int futilityMargin = depth * minimal_max_gain_per_ply;
                    if (evaluation.Compute(board) + futilityMargin <= alpha)
                    {
                        board.UnmakeMove();
                        continue;
                    }
                }

                if (ply != 0 && depth >= 2 && expandedNodes > 1)
                {
                    int R = (interesting || expandedNodes < 4) ? 0 : 2;
                    SearchResult r = -SearchTt(-alpha - 1, -alpha, depth - R - 1, ply + 1);
                    if (r.Score <= alpha)
                    {
                        board.UnmakeMove();
                        continue;
                    }
                }

                SearchResult result = -SearchTt(-beta, -alpha, depth - 1, ply + 1);
                board.UnmakeMove();

                if (result.Score > alpha)
                {
                    TtTran.Add(board.Hash, depth, ply, alpha, beta, result.Score, move);
                    pv = MergeMove(result.Pv, move);
                    alpha = result.Score;

                    if (result.Score >= beta)
                    {
                        if (Move.GetCapture(move) == Piece.None)
                        {
                            history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                            killerMoves.Add(move, ply);
                        }

                        MoveListPool.Return(moveList);
                        return new SearchResult(beta, pv);
                    }
                }
            }

            MoveListPool.Return(moveList);
            if (expandedNodes == 0)
            {
                return new SearchResult(isChecked ? -Constants.CHECKMATE_SCORE + ply : 0, EmptyPv);
            }

            return new SearchResult(alpha, pv);
        }

        private const int minimal_max_gain_per_ply = 70;
    }
}
