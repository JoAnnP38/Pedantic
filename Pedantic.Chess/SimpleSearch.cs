using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public class SimpleSearch : SearchBase
    {
        public SimpleSearch(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100) 
            : base(board, time, maxSearchDepth, maxNodes)
        { }

        public override SearchResult Search(int alpha, int beta, int depth, int ply, bool canNull = true)
        {
            bool raisedAlpha = false;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return DefaultResult;
            }

            if (board.HalfMoveClock >= 100 || board.GameDrawnByRepetition())
            {
                return DefaultResult;
            }

            if (depth <= 0)
            {
                return new SearchResult(QuiesceTt(alpha, beta, ply), EmptyPv);
            }

            NodesVisited++;

            bool inCheck = board.IsChecked();
            int extension = inCheck ? 1 : 0;
            bool allowNullMove = !canNull || !Evaluation.IsCheckmate(Result.Score) || (ply > Depth / 4);

            if (allowNullMove && depth >= 2 && !inCheck && beta < Constants.INFINITE_WINDOW)
            {
                int R = depth <= 6 ? 1 : 2;
                if (board.MakeMove(Move.NullMove))
                {
                    SearchResult result = -SearchTt(-beta, -beta + 1, depth - R - 1, ply + 1, true);
                    board.UnmakeMove();
                    if (result.Score >= beta)
                    {
                        return new SearchResult(beta, EmptyPv);
                    }
                }
            }

            int expandedNodes = 0;
            history.SideToMove = board.SideToMove;
            MoveList moveList = MoveListPool.Get();
            ulong[] pv = EmptyPv;
            
            foreach (ulong move in board.Moves(ply, killerMoves, history, moveList))
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;
                bool isCapture = Move.IsCapture(move);
                bool isPromote = Move.IsPromote(move);
                bool interesting = expandedNodes == 1 || inCheck || board.IsChecked() || isCapture || isPromote;

                if (ply > 0 && depth >= 2 && raisedAlpha)
                {
                    int R = (interesting || expandedNodes <= 4) ? 0 : 2;
                    //SearchResult r = -SearchTt(-alpha - 1, -alpha, depth - R - 1, ply + 1);
                    int score = -ZwSearchTt(-alpha, depth - R - 1, ply + 1);
                    if (score <= alpha)
                    {
                        board.UnmakeMove();
                        continue;
                    }
                }

                SearchResult result = -SearchTt(-beta, -alpha, depth + extension - 1, ply + 1);
                board.UnmakeMove();

                if (result.Score > alpha)
                {
                    pv = MergeMove(result.Pv, move);
                    TtEval.Add(board.Hash, depth, ply, alpha, beta, result.Score, move);
                    alpha = result.Score;
                    raisedAlpha = true;

                    if (result.Score >= beta)
                    {
                        if (!isCapture && !isPromote)
                        {
                            killerMoves.Add(move, ply);
                            history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                        }
                        alpha = beta;
                        break;
                    }
                }
            }

            MoveListPool.Return(moveList);

            if (expandedNodes == 0)
            {
                if (inCheck)
                {
                    alpha = -Constants.CHECKMATE_SCORE + ply;
                }
                else
                {
                    alpha = 0;
                }
            }

            return new SearchResult(alpha, pv);
        }

        private SearchResult SearchTt(int alpha, int beta, int depth, int ply, bool canNull = true)
        {
            if (TtEval.TryGetScore(board.Hash, depth, ply, alpha, beta, out int score))
            {
                return new SearchResult(score, EmptyPv);
            }

            SearchResult result = Search(alpha, beta, depth, ply, canNull);

            TtEval.Add(board.Hash, depth, ply, alpha, beta, result.Score, result.Pv.Length > 0 ? result.Pv[0] : 0ul);
            return result;
        }
    }
}
