using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.XPath;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public abstract class SearchBase : ISearch
    {
        protected SearchBase(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100)
        {
            this.board = board;
            this.time = time;
            this.maxSearchDepth = maxSearchDepth;
            this.maxNodes = maxNodes;
            Depth = 0;
            Result = new SearchResult();
            NodesVisited = 0L;
        }

        public int Depth { get; protected set; }
        public SearchResult Result { get; protected set; }
        public long NodesVisited { get; protected set; }
        public bool Pondering { get; set; }

        public bool MustAbort => NodesVisited >= maxNodes ||
                                 ((NodesVisited & CHECK_TC_NODES_MASK) == 0 && time.CheckTimeBudget());

        public virtual void Search()
        {
            Engine.Color = board.SideToMove;
            Depth = 0;
            ulong? ponderMove = null;

            if (board.OneLegalMove(out ulong bestMove))
            {
                Uci.BestMove(bestMove);
                return;
            }

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() && !Evaluation.IsCheckmate(Result.Score))
            {
                time.StartInterval();
                history.Rescale();
                UpdateTtWithPv(Result.Pv, Depth);

                for (int w = 0; w < window.Length; w++)
                {
                    int alpha = Result.Score - window[w];
                    int beta = Result.Score + window[w];

                    Result = Search(alpha, beta, Depth, 0);

                    if (wasAborted || (Result.Score > alpha && Result.Score < beta))
                    {
                        break;
                    }
                }

                if (wasAborted)
                {
                    break;
                }

                ReportSearchResults(out bestMove, out ponderMove);
            }

            if (Pondering)
            {
                bool waiting = false;
                while (time.CanSearchDeeper() && !wasAborted)
                {
                    waiting = true;
                    Thread.Sleep(WAIT_TIME);
                }

                if (waiting)
                {
                    ReportSearchResults(out bestMove, out ponderMove);
                }
            }
            Uci.BestMove(bestMove, ponderMove);
        }

        public abstract SearchResult Search(int alpha, int beta, int depth, int ply, bool canNull = true);

        protected virtual int Quiesce(int alpha, int beta, int ply)
        {
            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board);
            }

            if (board.HalfMoveClock >= 100)
            {
                return 0;
            }

            history.SideToMove = board.SideToMove;
            bool inCheck = board.IsChecked();

            if (!inCheck)
            {
                int standPatScore = evaluation.Compute(board);
                if (standPatScore >= beta)
                {
                    return standPatScore;
                }

                alpha = Math.Max(alpha, standPatScore);
            }

            int expandedNodes = 0;
            MoveList moveList = MoveListPool.Get();
            IEnumerable<ulong> moves =
                inCheck ? board.Moves(ply, killerMoves, history, moveList) : board.CaptureMoves(moveList);

            foreach (ulong move in moves)
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;
                int score = -Quiesce(-beta, -alpha, ply + 1);
                board.UnmakeMove();

                if (score > alpha)
                {
                    alpha = score;

                    if (score >= beta)
                    {
                        MoveListPool.Return(moveList);
                        return beta;
                    }
                }
            }

            if (expandedNodes == 0)
            {
                if (inCheck)
                {
                    MoveListPool.Return(moveList);
                    return -Constants.CHECKMATE_SCORE + ply;
                }

                if (!board.HasLegalMoves(moveList))
                {
                    MoveListPool.Return(moveList);
                    return 0;
                }
            }

            MoveListPool.Return(moveList);
            return alpha;
        }

        protected ulong[] MergeMove(ulong[] pv, ulong move)
        {
            Array.Resize(ref pv, pv.Length + 1);
            Array.Copy(pv, 0, pv, 1, pv.Length - 1);
            pv[0] = move;
            return pv;
        }

        protected void UpdateTtWithPv(ulong[] pv, int depth)
        {
            Board bd = board.Clone();
            for (int n = 0; n < pv.Length; n++)
            {
                ulong move = pv[n];
                TtEval.Add(bd.Hash, (short)depth--, n, -short.MaxValue, short.MaxValue, Result.Score, move);
                bd.MakeMove(move);
            }
        }

        protected void ReportSearchResults(out ulong bestMove, out ulong? ponderMove)
        {
            Result = new SearchResult(Result.Score, ExtractPv(Result.Pv, Depth));
            if (IsCheckmate(Result.Score, out int mateIn))
            {
                Uci.InfoMate(Depth, mateIn, NodesVisited, time.Elapsed, Result.Pv);
            }
            else
            {
                Uci.Info(Depth, Result.Score, NodesVisited, time.Elapsed, Result.Pv);
            }

            bestMove = Result.Pv.Length > 0 ? Result.Pv[0] : 0;
            if (Result.Pv.Length > 1)
            {
                ponderMove = Result.Pv[1];
            }
            else
            {
                ponderMove = null;
            }
        }

        protected ulong[] ExtractPv(ulong[] pv, int depth)
        {
            List<ulong> result = new(pv);

            if (result.Count < depth)
            {
                Board bd = board.Clone();
                foreach (ulong move in pv)
                {
                    if (!bd.IsLegalMove(move))
                    {
                        throw new ArgumentException($"Invalid move in PV '{Move.ToLongString(move)}'.");
                    }

                    bd.MakeMove(move);
                }

                while (result.Count < depth && TtEval.TryGetBestMove(bd.Hash, out ulong bestMove) && bd.IsLegalMove(bestMove))
                {
                    bd.MakeMove(bestMove);
                    result.Add(bestMove);
                }
            }

            return result.ToArray();
        }

        protected static bool IsCheckmate(int score, out int mateIn)
        {
            mateIn = 0;
            int absScore = Math.Abs(score);
            bool checkMate = absScore >= Constants.CHECKMATE_BASE;
            if (checkMate)
            {
                mateIn = ((Constants.CHECKMATE_SCORE - absScore + 1) / 2) * Math.Sign(score);
            }

            return checkMate;
        }

        protected const int CHECK_TC_NODES_MASK = 31;
        protected const short DELTA_PRUNING_MARGIN = 200;
        protected const int MAX_GAIN_PER_PLY = 100;
        protected const int WAIT_TIME = 50;
        protected const long MS_PER_SECOND = 1000L;

        protected Board board;
        protected TimeControl time;
        protected Evaluation evaluation = new();
        protected int maxSearchDepth;
        protected long maxNodes;
        protected History history = new();
        protected KillerMoves killerMoves = new();
        protected bool wasAborted = false;

        protected readonly ObjectPool<MoveList> MoveListPool = new(Constants.MAX_PLY);
        protected static readonly ulong[] EmptyPv = Array.Empty<ulong>();
        protected static readonly SearchResult DefaultResult = new(0, EmptyPv);
        protected static readonly int[] window = { 50, 200, Constants.INFINITE_WINDOW };
    }
}
