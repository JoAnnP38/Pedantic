using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualBasic;
using Pedantic.Genetics;

namespace Pedantic.Chess
{
    public class BasicSearch : SearchBase
    {
        public BasicSearch(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100) 
            : base(board, time, maxSearchDepth, maxNodes)
        { }

        public override SearchResult Search(int alpha, int beta, int depth, int ply, bool canNull = true, bool isPv = true)
        {
            if (IsDraw())
            {
                return new SearchResult(Contempt);
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return new SearchResult(evaluation.Compute(board));
            }

            if (depth <= 0)
            {
                return new SearchResult(QuiesceTt(alpha, beta, ply));
            }

            NodesVisited++;
            int originalAlpha = alpha;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return DefaultResult;
            }

            bool inCheck = board.IsChecked();
            int extension = CalcExtension(inCheck);
            int eval = evaluation.Compute(board);

            if (depth > 2 && canNull && !isPv && !inCheck && eval > beta && 
                board.HasMinorMajorPieces(board.OpponentColor, 600))
            {
                int R = depth > 6 ? 3 : 2;
                if (board.MakeMove(Move.NullMove))
                {
                    SearchResult result = -SearchTt(-beta, -beta + 1, depth - R - 1, ply + 1, false, false);
                    board.UnmakeMove();
                    if (result.Score >= beta)
                    {
                        return new SearchResult(beta);
                    }
                }
            }

            if (!isPv && !inCheck && canNull && depth <= 2)
            {
                int threshold = alpha - futilityMargin[depth];
                if (eval < threshold)
                {
                    int score = QuiesceTt(alpha, beta, ply);
                    if (score < threshold)
                    {
                        return new SearchResult(alpha);
                    }
                }
            }

            bool canPrune = depth <= 2 && !isPv && !inCheck && Math.Abs(alpha) < Constants.CHECKMATE_SCORE &&
                            eval + futilityMargin[depth] <= alpha;
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

                bool checkingMove = board.IsChecked();
                bool interesting = expandedNodes == 1 || extension > 0 || inCheck || checkingMove;
                bool isQuiet = Move.IsQuiet(move);

                if (canPrune && isQuiet && !interesting)
                {
                    board.UnmakeMove();
                    continue;
                }

                int R = 0;
                if (extension == 0 && !isPv && expandedNodes > 3 && !interesting && isQuiet && !killerMoves.Exists(ply, move))
                {
                    R = expandedNodes < 8 ? 1 : 2;
                }

                if (ply > 0 && expandedNodes > 1)
                {
                    SearchResult r = -SearchTt( -alpha - 1, -alpha, depth + extension - R - 1, ply + 1, true, false);

                    if (wasAborted)
                    {
                        board.UnmakeMove();
                        break;
                    }
                    if (r.Score <= alpha)
                    {
                        board.UnmakeMove();
                        continue;
                    }
                }

                SearchResult result = -SearchTt(-beta, -alpha, depth + extension - R - 1, ply + 1, true, isPv);
                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (result.Score > alpha)
                {
                    TtTran.Add(board.Hash, depth, ply, originalAlpha, beta, result.Score, move);
                    pv = MergeMove(result.Pv, move);
                    alpha = result.Score;

                    if (result.Score >= beta)
                    {
                        if (!Move.IsCapture(move))
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

            if (wasAborted)
            {
                return DefaultResult;
            }

            if (expandedNodes == 0)
            {
                return new SearchResult(inCheck ? -Constants.CHECKMATE_SCORE + ply : Contempt, EmptyPv);
            }

            return new SearchResult(alpha, pv);
        }

        private SearchResult SearchTt(int alpha, int beta, int depth, int ply, bool canNull = true, bool isPv = true)
        {
            if (TtTran.TryGetScore(board.Hash, depth, ply, alpha, beta, out int score))
            {
                return new SearchResult(score, EmptyPv);
            }

            SearchResult result = Search(alpha, beta, depth, ply, canNull, isPv);

            TtTran.Add(board.Hash, depth, ply, alpha, beta, result.Score, result.Pv.Length > 0 ? result.Pv[0] : 0ul);
            return result;
        }

    }
}
