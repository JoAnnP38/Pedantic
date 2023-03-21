using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualBasic;
using Pedantic.Genetics;
using Pedantic.Utilities;

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

            alpha = Math.Max(alpha, -Constants.CHECKMATE_SCORE + ply - 1);
            beta = Math.Min(beta, Constants.CHECKMATE_SCORE - ply);

            if (alpha >= beta)
            {
                return new SearchResult(alpha);
            }

            if (depth <= 0)
            {
                return new SearchResult(Quiesce(alpha, beta, ply));
            }

            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return DefaultResult;
            }

            bool inCheck = board.IsChecked();
            int X = CalcExtension(inCheck);
            bool canReduce = X == 0;
            int eval = evaluation.Compute(board);

            if (canNull && canReduce && depth >= 3 && !isPv && eval >= beta && 
                board.HasMinorMajorPieces(board.OpponentColor, 600))
            {
                int R = nmp[depth];
                if (board.MakeMove(Move.NullMove))
                {
                    SearchResult result = -SearchTt(-beta, -beta + 1, Math.Max(depth - R - 1, 0), ply + 1, false, false);
                    board.UnmakeMove();
                    if (wasAborted)
                    {
                        return DefaultResult;
                    }

                    if (result.Score >= beta)
                    {
                        return new SearchResult(beta);
                    }
                }
            }

            if (canNull && canReduce && !isPv && depth <= 2 && !Move.IsPawnMove(board.LastMove))
            {
                int threshold = alpha - 300 * depth;
                if (eval < threshold)
                {
                    int score = Quiesce(alpha, beta, ply);
                    if (score < threshold)
                    {
                        return new SearchResult(alpha);
                    }
                }
            }

            bool canPrune = canReduce && !isPv && Math.Abs(alpha) < Constants.CHECKMATE_BASE && depth < 8 &&
                            eval + futilityMargin[depth] <= alpha;

            ulong[] pv = EmptyPv;
            int expandedNodes = 0;
            history.SideToMove = board.SideToMove;
            MoveList moveList = MoveListPool.Get();

#if DEBUG
            if (ply == 0)
            {
                foreach (ulong move in killerMoves.GetKillers(ply))
                {
                    Util.TraceInfo($"KILLERS: Depth {depth}, Ply {ply}, Move {Move.ToLongString(move)}");
                }
            }
            string fen = board.ToFenString();
#endif

            foreach (ulong move in board.Moves(ply, killerMoves, history, moveList))
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;

                bool checkingMove = board.IsChecked();
                bool isQuiet = Move.IsQuiet(move);
                bool interesting = inCheck || checkingMove || !isQuiet || expandedNodes == 1;

                if (canPrune && !interesting && expandedNodes > lmp[depth])
                {
                    board.UnmakeMove();
                    continue;
                }

#if DEBUG
                if (ply == 0)
                {
                    Util.TraceInfo($"Depth {depth}, Ply {ply}, Move {Move.ToLongString(move)}");
                }
#endif

                int R = 0;
                if (!interesting && !killerMoves.Exists(ply, move))
                {
                    R = lmr[Math.Min(depth, 31)][Math.Min(expandedNodes, 63)];
                }

                if (X > 0 && R > 0)
                {
                    R--;
                }

                SearchResult result;
                for (;;)
                {
                    if (expandedNodes == 1)
                    {
                        result = -SearchTt(-beta, -alpha, depth + X - 1, ply + 1, true, isPv);
                    }
                    else
                    {
                        result = -SearchTt(-alpha - 1, -alpha, Math.Max(depth + X - R - 1, 0), ply + 1, true, false);
                        if (result.Score > alpha)
                        {
                            result = -SearchTt(-beta, -alpha, Math.Max(depth + X - R - 1, 0), ply + 1, true, R == 0);
                        }
                    }

                    if (R > 0 && result.Score > alpha)
                    {
                        R = 0;
                    }
                    else
                    {
                        break;
                    }
                }

                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (result.Score > alpha)
                {
                    pv = MergeMove(result.Pv, move);
                    alpha = result.Score;

                    if (result.Score >= beta)
                    {
                        if (Move.IsQuiet(move))
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
                return new SearchResult(inCheck ? -Constants.CHECKMATE_SCORE + ply : Contempt);
            }

            return new SearchResult(alpha, pv);
        }

        public Evaluation Evaluation
        {
            get => evaluation;
            set => evaluation = value;
        }
    }
}
