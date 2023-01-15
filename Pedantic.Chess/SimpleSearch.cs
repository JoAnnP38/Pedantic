namespace Pedantic.Chess
{
    public class SimpleSearch : SearchBase
    {
        public SimpleSearch(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100) 
            : base(board, time, maxSearchDepth, maxNodes)
        { }

        public override SearchResult Search(int alpha, int beta, int depth, int ply)
        {
            if (board.HalfMoveClock >= 100 || board.GameDrawnByRepetition())
            {
                return DefaultResult;
            }

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

            if (ply >= Constants.MAX_PLY - 1)
            {
                return new SearchResult(evaluation.Compute(board), EmptyPv);
            }

            bool inCheck = board.IsChecked();
            int extension = inCheck ? 1 : 0;
            if (Move.GetMoveType(board.LastMove) == MoveType.PawnMove &&
                Index.GetRank(Index.NormalizedIndex[(int)board.OpponentColor][Move.GetTo(board.LastMove)]) == 6)
            {
                // another extension if pawn just moved to 7th rank
                extension++;
            }

            bool allowNullMove = !Evaluation.IsCheckmate(Result.Score) || (ply > Depth / 4);

            if (allowNullMove && depth > 2 && extension == 0 && board.LastMove != Move.NullMove && beta < Constants.INFINITE_WINDOW)
            {
                int R = depth > 6 ? 3 : 2;
                if (board.MakeMove(Move.NullMove))
                {
                    SearchResult result = -SearchTt(-beta, -beta + 1, depth - R - 1, ply + 1);
                    board.UnmakeMove();
                    if (result.Score >= beta)
                    {
                        return new SearchResult(beta, EmptyPv);
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
                bool isCapture = Move.IsCapture(move);
                bool isPromote = Move.IsPromote(move);
                bool interesting = expandedNodes == 1 || inCheck || board.IsChecked() || isPromote;

                if (ply > 0 && depth <= 3 && !interesting && extension == 0)
                {   
                    if (-QuiesceTt(-beta, -alpha, ply) <= alpha)
                    {
                        board.UnmakeMove();
                        continue;
                    }
                }

                if (ply > 0 && depth >= 2 && expandedNodes > 1)
                {
                    int R = (extension > 0 || interesting || expandedNodes < 4) ? 0 : 2;
                    SearchResult r = -SearchTt(-alpha - 1, -alpha, depth - R - 1, ply + 1);
                    if (r.Score <= alpha)
                    {
                        board.UnmakeMove();
                        continue;
                    }
                }

                SearchResult result = -SearchTt(-beta, -alpha, depth + extension - 1, ply + 1);
                board.UnmakeMove();

                if (result.Score > alpha)
                {
                    TtTran.Add(board.Hash, depth, ply, alpha, beta, result.Score, move);
                    pv = MergeMove(result.Pv, move);
                    alpha = result.Score;

                    if (result.Score >= beta)
                    {
                        if (!isCapture && !isPromote)
                        {
                            killerMoves.Add(move, ply);
                            history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                        }

                        MoveListPool.Return(moveList);
                        return new SearchResult(beta, pv);
                    }
                }
            }

            MoveListPool.Return(moveList);
            if (expandedNodes == 0)
            {
                return new SearchResult(inCheck ? -Constants.CHECKMATE_SCORE + ply : 0, EmptyPv);
            }

            return new SearchResult(alpha, pv);
        }

        private SearchResult SearchTt(int alpha, int beta, int depth, int ply)
        {
            if (TtTran.TryGetScore(board.Hash, depth, ply, alpha, beta, out int score))
            {
                return new SearchResult(score, EmptyPv);
            }

            SearchResult result = Search(alpha, beta, depth, ply);

            TtTran.Add(board.Hash, depth, ply, alpha, beta, result.Score, result.Pv.Length > 0 ? result.Pv[0] : 0ul);
            return result;
        }

        private readonly int[] futilityMargin = { 100, 300, 600, 900 };
    }
}
