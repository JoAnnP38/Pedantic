using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class Negamax
    {
        private const int CHECK_TC_NODES = 50;

        private Board board;
        private TimeControl time;
        private long maxNodes;
        private short maxSearchDepth;
        private long nodesVisited = 0;
        private ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);
        private PV pv = new();

        public Negamax(Board board, TimeControl time, short maxSearchDepth, long maxNodes = long.MaxValue)
        {
            this.board = board;
            this.time = time;
            this.maxSearchDepth = maxSearchDepth;
            this.maxNodes = maxNodes;
        }

        private bool Aborted =>
            nodesVisited > maxNodes || ((nodesVisited % CHECK_TC_NODES == 0) && time.CheckTimeBudget());

        public short Search(short alpha, short beta, short depthLeft, int ply = 0)
        {
            short originalAlpha = alpha;
            pv.Moves[ply][0] = 0ul;
            ulong bestmove = 0ul;

            if (TtEval.TryLookup(board.Hash, out TtEval.TtEvalItem ttItem) && ttItem.Depth >= depthLeft)
            {
                bestmove = ttItem.Move;
                if (ttItem.Flag == TtEval.TtFlag.Exact)
                {
                    // this result in PV being truncated because we don't
                    // preserve the line the resulted in this move being chosen
                    pv.AddMove(ply, ttItem.Move); 
                    return ttItem.Score;
                }

                if (ttItem.Flag == TtEval.TtFlag.LowerBound)
                {
                    alpha = Math.Max(alpha, ttItem.Score);
                }
                else if (ttItem.Flag == TtEval.TtFlag.UpperBound)
                {
                    beta = Math.Min(beta, ttItem.Score);
                }

                if (alpha >= beta)
                {
                    return ttItem.Score;
                }
            }

            if (depthLeft == 0)
            {
                return Quiesce(alpha, beta, ply);
            }

            nodesVisited++;

            if (Aborted)
            {
                return 0;
            }

            bool inCheck = board.IsChecked();
            if (inCheck)
            {
                depthLeft++;
            }

            MoveList moveList = moveListPool.Get();
            board.GenerateMoves(moveList);
            short value = short.MinValue;
            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                if (board.MakeMove(moveList[n]))
                {
                    value = Math.Max(value, Neg(Search(Neg(beta), Neg(alpha), Dec(depthLeft), ply + 1)));
                    board.UnmakeMove();

                    if (value > alpha)
                    {
                        alpha = value;
                        pv.Merge(ply, moveList[n]);
                    }

                    if (alpha >= beta)
                    {
                        break; // cutoff
                    }
                }
            }

            moveListPool.Return(moveList);
            TtEval.Add(board.Hash, depthLeft, originalAlpha, beta, value, pv.Moves[ply][0]);

            return value;
        }

        public short Quiesce(short alpha, short beta, int ply)
        {
            nodesVisited++;

            if (Aborted)
            {
                return 0;
            }

            if (ply >= Constants.MAX_PLY)
            {
                // return Evaluation.Compute(board);
            }

            bool inCheck = board.IsChecked();
            if (!inCheck)
            {
                short standPat = /* Evaluation.Compute(board)*/ 0;
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
                    short score = Neg(Quiesce(Neg(beta), Neg(alpha), ply + 1));
                    board.UnmakeMove();

                    if (score >= beta)
                    {
                        moveListPool.Return(moveList);
                        return beta;
                    }

                    if (score > alpha)
                    {
                        alpha = score;
                        pv.Merge(ply, moveList[n]);
                    }

                }
            }

            moveListPool.Return(moveList);
            if (expandedNodes == 0 && inCheck)
            {
                return board.SideToMove == Color.White ? Neg(Constants.CHECKMATE_SCORE) : Constants.CHECKMATE_SCORE;
            }

            return alpha;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short Neg(short value) => (short)-value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short Dec(short value) => (short)(value - 1);
    }
}
