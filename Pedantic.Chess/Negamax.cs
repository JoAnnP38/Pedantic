using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pedantic.Collections;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class Negamax
    {
        private const int CHECK_TC_NODES = 50;

        private Board board;
        private TimeControl time;
        private long maxNodes;
        private short maxSearchDepth;
        private long nodesVisited = 0;
        private ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);
        private PV pv = new();
        private History history = new();
        private ulong[][] killers = Mem.Allocate2D<ulong>(Constants.MAX_PLY, 2);

        public Negamax(Board board, TimeControl time, short maxSearchDepth, long maxNodes = long.MaxValue)
        {
            this.board = board;
            this.time = time;
            this.maxSearchDepth = maxSearchDepth;
            this.maxNodes = maxNodes;
            WasAborted = false;
        }

        public PV PV => pv;
        public short Score { get; private set; }
        public bool IsAborted =>
            nodesVisited > maxNodes || ((nodesVisited % CHECK_TC_NODES == 0) && time.CheckTimeBudget());

        public bool IsGameOver => Evaluation.IsCheckmate(Score);
        public bool WasAborted { get; set; } = false;

        public short Search()
        {
            ulong bestmove = 0ul;
            int mateIn = 0;
            bool checkMate = false;

            Mem.Clear(killers);
            history.Clear();
            pv.Clear();

            short score = Evaluation.Compute(board);
            short depth = 0;
            WasAborted = false;
            while (time.CanSearchDeeper() && depth++ < maxSearchDepth && !WasAborted && !checkMate)
            {
                time.StartInterval();
                short alpha = (short)(score - Constants.ALPHA_BETA_WINDOW);
                short beta = (short)(score + Constants.ALPHA_BETA_WINDOW);
                score = Search(alpha, beta, depth);
                if (IsAborted)
                {
                    WasAborted = true;
                    break;
                }

                // TODO: Can it truly fail the opposite way after only expanding one side of search window?
                while (score <= alpha || score >= beta)
                {
                    if (score <= alpha)
                    {
                        alpha = -Constants.INFINITE_WINDOW;
                        score = Search(alpha, beta, depth);
                    }
                    else if (score >= beta)
                    {
                        beta = Constants.INFINITE_WINDOW;
                        score = Search(alpha, beta, depth);
                    }

                    if (IsAborted)
                    {
                        WasAborted = true;
                        break;
                    }
                }

                ExtractRemainingPV();

                if (IsAborted)
                {
                    WasAborted = true;
                    break;
                }

                checkMate = IsCheckMate(score, out mateIn);

                if (!WasAborted)
                {
                    if (!checkMate)
                    {
                        Uci.Info(depth, score, nodesVisited, time.Elapsed, PV);
                    }
                    else
                    {
                        Uci.InfoMate(depth, mateIn, nodesVisited, time.Elapsed, PV);
                    }
                    bestmove = PV.Moves[0][0];
                }
            }

            Uci.BestMove(bestmove);
            return score;
        }

        public short Search(short alpha, short beta, short depthLeft, int ply = 0)
        {
            PV.Length[ply] = ply;

            short originalAlpha = alpha;
            nodesVisited++;

            if (IsAborted)
            {
                WasAborted = true;
                return 0;
            }

            if (TtEval.TryLookup(board.Hash, out TtEval.TtEvalItem ttItem) && ttItem.Depth >= depthLeft)
            {
                if (ttItem.Flag == TtEval.TtFlag.Exact)
                {
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

            if (ply > Constants.MAX_PLY - 1)
            {
                return Evaluation.Compute(board);
            }

            if (ply > 0 && board.Repeat2())
            {
                // the current position is repeated to times in the past
                // return 0 (draw) for three-fold repetition
                return 0;
            }

            if (depthLeft <= 0)
            {
                return Quiesce(alpha, beta, ply);
            }


            bool inCheck = board.IsChecked();
            MoveList moveList = moveListPool.Get();
            history.SideToMove = board.SideToMove;
            board.GenerateMoves(moveList, history);
            moveList.UpdateScores(PV.Moves[0][ply], killers[ply]);

            short value = short.MinValue;
            int expandedNodes = 0;

            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                if (board.MakeMove(moveList[n]))
                {
                    ulong move = moveList[n];
                    string moveString = Move.ToString(move);
                    bool isCapture = Move.GetCapture(move) != Piece.None;
                    expandedNodes++;
                    short d = RemainingDepth(depthLeft, board, move, expandedNodes, inCheck);

                    value = Math.Max(value, Neg(Search(Neg(beta), Neg(alpha), d, ply + 1)));
                    board.UnmakeMove();

                    if (value > alpha)
                    {
                        alpha = value;
                        pv.Merge(ply, move);

                        if (!isCapture)
                        {
                            history.Update(Move.GetFrom(move), Move.GetTo(move), depthLeft);
                        }

                        if (value >= beta)
                        {
                            if (!isCapture)
                            {
                                killers[ply][1] = killers[ply][0];
                                killers[ply][0] = move;
                            }
                            break; // cutoff
                        }
                    }
                }
            }
            moveListPool.Return(moveList);

            if (expandedNodes == 0)
            {
                if (inCheck)
                {
                    return (short)(-Constants.CHECKMATE_SCORE + ply);
                }

                return 0;
            }

            TtEval.Add(board.Hash, depthLeft, originalAlpha, beta, value, pv.Moves[ply][0]);

            return alpha;
        }

        public short Quiesce(short alpha, short beta, int ply)
        {
            PV.Length[ply] = ply;
            nodesVisited++;

            if (IsAborted)
            {
                WasAborted = true;
                return 0;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return Evaluation.Compute(board);
            }

            bool inCheck = board.IsChecked();
            if (!inCheck)
            {
                short standPat = Evaluation.Compute(board);
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

            short score = short.MinValue;
            for (int n = 0; n < moveList.Count; ++n)
            {
                moveList.Sort(n);
                if (board.MakeMove(moveList[n]))
                {
                    ++expandedNodes;
                    score = Neg(Quiesce(Neg(beta), Neg(alpha), ply + 1));
                    board.UnmakeMove();

                    if (score > alpha)
                    {
                        alpha = score;
                        pv.Merge(ply, moveList[n]);

                        if (score >= beta)
                        {
                            break;
                        }
                    }
                }
            }

            moveListPool.Return(moveList);

            if (expandedNodes == 0)
            {
                if (inCheck)
                {
                    return (short)(-Constants.CHECKMATE_SCORE + ply);
                }

                return 0;
            }

            return score;
        }

        private static short RemainingDepth(short depthLeft, Board board, ulong move, int expandedNodes, bool inCheck)
        {
            short d;
            int score = Move.GetScore(move);
            Piece promote = Move.GetPromote(move);
            if (board.IsChecked())
            {
                d = depthLeft;
            }
            // TODO: Implement reductions when PVS or ZWS is implemented (full depth for now)
            /*else if (depthLeft >= 3 && expandedNodes >= 4 && !inCheck && score == 0 && promote == Piece.None)
            {
                d = Dec(depthLeft, 2);
            }*/
            else
            {
                d = Dec(depthLeft);
            }
            return d;
        }

        public void ExtractRemainingPV()
        {
            int takeBackCount = 0;
            for (int n = 0; n < PV.Length[0]; n++)
            {
                board.MakeMove(PV.Moves[0][n]);
                takeBackCount++;
            }
            MoveList moveList = new();
            while (PV.Length[0] < Constants.MAX_PLY && TtEval.TryLookup(board.Hash, out TtEval.TtEvalItem ttItem) && ttItem.Flag == TtEval.TtFlag.Exact)
            {
                board.GenerateMoves(moveList);
                bool legalMove = false;
                for (int n = 0; n < moveList.Count; n++)
                {
                    if (Move.Compare(ttItem.Move, moveList[n]) == 0)
                    {
                        legalMove = true;
                        break;
                    }
                }

                if (!legalMove)
                {
                    break;
                }
                if (!board.MakeMove(ttItem.Move))
                {
                    Uci.Log($"Bad move found int transposition table: {Move.ToString(ttItem.Move)}");
                    board.UnmakeMove();
                    break;
                }
                PV.Moves[0][PV.Length[0]] = ttItem.Move;
                PV.Length[0]++;
                takeBackCount++;
            }

            while (takeBackCount-- > 0)
            {
                board.UnmakeMove();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short Neg(short value) => (short)-value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short Dec(short value) => (short)(value - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short Dec(short value, short dec) => (short)(value - dec);

        private static bool IsCheckMate(short score, out int mateIn)
        {
            mateIn = 0;
            bool checkMate = Math.Abs(score) >= Constants.CHECKMATE_SCORE - Constants.MAX_PLY;
            if (checkMate)
            {
                mateIn = (Constants.CHECKMATE_SCORE - Math.Abs(score)) * Math.Sign(score);
            }

            return checkMate;
        }
    }
}
