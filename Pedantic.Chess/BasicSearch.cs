// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 03-12-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="BasicSearch.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Define the <c>BasicSearch</c> class that implement the Principal
//     Variation search.
// </summary>
// ***********************************************************************

using System.Diagnostics;
using System.Text.RegularExpressions;
using Pedantic.Genetics;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class BasicSearch
    {
        public const int CHECK_TC_NODES_MASK = 31;
        internal const int WAIT_TIME = 50;


        public BasicSearch(Board board, TimeControl time, int maxSearchDepth, long maxNodes = long.MaxValue - 100, bool randomSearch = false) 
        {
            this.board = board;
            this.time = time;
            this.maxSearchDepth = maxSearchDepth;
            this.maxNodes = maxNodes;
            Depth = 0;
            PV = Array.Empty<ulong>();
            Score = 0;
            NodesVisited = 0L;
            evaluation = new Evaluation(true, randomSearch, true);
            startDateTime = DateTime.Now;
        }

        public void Search()
        {
            Engine.Color = board.SideToMove;
            Depth = 0;
            long startNodes = 0;
            ulong? ponderMove = null;
            bool oneLegalMove = board.OneLegalMove(out ulong bestMove);
            Eval.CalcMaterialAdjustment(board);
            bool inCheck = board.IsChecked();
            Score = Quiesce(-Constants.INFINITE_WINDOW, Constants.INFINITE_WINDOW, 0, inCheck);

            while (++Depth <= maxSearchDepth && time.CanSearchDeeper() && (!IsCheckmate(Score, out int mateIn) || (Arith.Abs(mateIn) << 1) + (Arith.Abs(mateIn) >> 1) + 2 >= Depth))
            {
                time.StartInterval();
                history.Rescale();
                UpdateTtWithPv(PV, Depth);
                int iAlpha = 0, iBeta = 0, result, alpha, beta;
                seldepth = Depth;

                do
                {
                    alpha = Window[iAlpha] == -Constants.INFINITE_WINDOW ? -Constants.INFINITE_WINDOW : Score - Window[iAlpha];
                    beta = Window[iBeta] == Constants.INFINITE_WINDOW ? Constants.INFINITE_WINDOW : Score + Window[iBeta];
                    result = SearchRoot(alpha, beta, Depth, inCheck);

                    if (wasAborted)
                    {
                        break;
                    }

                    if (result <= alpha)
                    {
                        ++iAlpha;
                    }
                    else if (result >= beta)
                    {
                        ++iBeta;
                    }

                } while (result <= alpha || result >= beta);

                if (wasAborted)
                {
                    break;
                }

                if (CollectStats)
                {
                    stats.Add(new ChessStats()
                    {
                        Phase = board.Phase.ToString(),
                        Depth = Depth,
                        NodesVisited = NodesVisited - startNodes
                    });
                }

                startNodes = NodesVisited;
                Score = result;
                ReportSearchResults(ref bestMove, ref ponderMove);
                if (Depth == 5 && oneLegalMove)
                {
                    break;
                }
            }

            // If program was pondering next move and the search loop was exited for 
            // reasons not due to the client telling us to stop, then sleep until 
            // we get a stop from the client (i.e. Engine will change the Infinite
            // property to false resulting in CanSearchDeeper returning false.)
            if (Pondering)
            {
                bool waiting = false;
                while (time.Infinite && !wasAborted)
                {
                    waiting = true;
                    Thread.Sleep(WAIT_TIME);
                }

                if (waiting)
                {
                    ReportSearchResults(ref bestMove, ref ponderMove);
                }
            }

            if (TryGetCpuLoad(startDateTime, out int cpuLoad))
            {
                Uci.Usage(TtTran.Usage, cpuLoad);
            }

            Uci.BestMove(bestMove, CanPonder ? ponderMove : null);
        }

        public int SearchRoot(int alpha, int beta, int depth, bool inCheck)
        {
            int originalAlpha = alpha;
            depth = Arith.Min(depth, 63);

            if (TtTran.TryGetScore(board.Hash, depth, 0, ref alpha, ref beta, out int score, out ulong bestMove) &&
                board.IsLegalMove(bestMove))
            {
                return score;
            }

            int X = CalcExtension(inCheck);
            if (depth + X <= 0)
            {
                return Quiesce(alpha, beta, 0, inCheck);
            }

            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            int expandedNodes = 0;
            bool raisedAlpha = false;
            history.SideToMove = board.SideToMove;
            MoveList moveList = moveListPool.Get();

            foreach (ulong move in board.Moves(0, killerMoves, history, moveList))
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;
                if (startReporting || (DateTime.Now - startDateTime).TotalMilliseconds >= 1000)
                {
                    startReporting = true;
                    Uci.CurrentMove(depth, move, expandedNodes);
                }

                bool checkingMove = board.IsChecked();
                bool isQuiet = Move.IsQuiet(move);
                bool badCapture = Move.IsCapture(move) && Move.GetScore(move) == Constants.BAD_CAPTURE;
                bool interesting = inCheck || checkingMove || (!isQuiet && !badCapture) || !raisedAlpha;

                int R = 0;
                if (!interesting && !killerMoves.Exists(0, move))
                {
                    R = LMR[Arith.Min(depth, 31)][Arith.Min(expandedNodes - 1, 63)];
                }

                if (X > 0 && R > 0)
                {
                    R--;
                }

                for (;;)
                {
                    if (!raisedAlpha)
                    {
                        score = -Search(-beta, -alpha, depth + X - 1, 1, checkingMove);
                    }
                    else
                    {
                        score = -Search(-alpha - 1, -alpha, Arith.Max(depth + X - R - 1, 0), 1, checkingMove, isPv: false);
                        if (score > alpha)
                        {
                            score = -Search(-beta, -alpha, Arith.Max(depth + X - R - 1, 0), 1, checkingMove, true, R == 0);
                        }
                    }

                    if (R > 0 && score > alpha)
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

                if (score > alpha)
                {
                    raisedAlpha = true;
                    alpha = score;
                    bestMove = move;

                    if (score >= beta)
                    {
                        if (Move.IsQuiet(move))
                        {
                            killerMoves.Add(move, 0);
                            history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                        }

                        break;
                    }
                }
            }

            moveListPool.Return(moveList);

            if (wasAborted)
            {
                return 0;
            }

            if (expandedNodes == 0)
            {
                return inCheck ? -Constants.CHECKMATE_SCORE : 0;
            }

            TtTran.Add(board.Hash, depth, 0, originalAlpha, beta, alpha, bestMove);
            return alpha;
        }

        public int Search(int alpha, int beta, int depth, int ply, bool inCheck, bool canNull = true, bool isPv = true)
        {
            int originalAlpha = alpha;
            depth = Arith.Min(depth, 63);

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board, alpha, beta);
            }

            var repeated = board.PositionRepeated();
            if (repeated.Repeated && !inCheck)
            {
                return Contempt;
            }

            // This is a trick from CPW-Engine which I do not understand
            // but I leave the code here anyways.
            alpha = Arith.Max(alpha, -Constants.CHECKMATE_SCORE + ply - 1);
            beta = Arith.Min(beta, Constants.CHECKMATE_SCORE - ply);

            if (alpha >= beta)
            {
                return alpha;
            }

            if (TtTran.TryGetScore(board.Hash, depth, ply, ref alpha, ref beta, out int score, out ulong _))
            {
                return score;
            }

            int X = CalcExtension(inCheck);
            if (depth + X <= 0)
            {
                return Quiesce(alpha, beta, ply, inCheck);
            }

            NodesVisited++;
            seldepth = Arith.Max(seldepth, ply);

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            int eval = evaluation.Compute(board, alpha, beta);
            bool canPrune = false;

            if (!inCheck && !isPv)
            {
                // static null move pruning
                if (depth <= 2)
                {
                    int evalWithMargin = eval - depth * Constants.STATIC_NULL_MOVE_MARGIN;
                    if (evalWithMargin >= beta)
                    {
                        return evalWithMargin;
                    }
                }

                // null move pruning
                if (canNull && depth >= 3 && eval >= beta && board.PieceCount(board.OpponentColor) > 1)
                {
                    int R = 2 + NMP[depth];
                    if (board.MakeMove(Move.NullMove))
                    {
                        score = -Search(-beta, -beta + 1, Arith.Max(depth - R - 1, 0), ply + 1, false, false, false);
                        board.UnmakeMove();
                        if (wasAborted)
                        {
                            return 0;
                        }

                        if (score >= beta)
                        {
                            TtTran.Add(board.Hash, depth, ply, originalAlpha, beta, score, 0ul);
                            return beta;
                        }
                    }
                }

                // razoring
                if (canNull && depth <= 3)
                {
                    int threshold = alpha - FutilityMargin[depth];
                    if (eval <= threshold)
                    {
                        score = Quiesce(alpha, beta, ply, inCheck);
                        if (score <= alpha)
                        {
                            return score;
                        }
                    }

                    // enable LMP pruning
                    canPrune = true;
                }
            }

            int expandedNodes = 0;
            history.SideToMove = board.SideToMove;
            MoveList moveList = moveListPool.Get();
            ulong bestMove = 0ul;

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
                bool badCapture = Move.IsCapture(move) && Move.GetScore(move) == Constants.BAD_CAPTURE;
                bool interesting = inCheck || checkingMove || (!isQuiet && !badCapture) || expandedNodes == 1;

                if (canPrune && !interesting && expandedNodes > LMP[depth])
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
                    R = LMR[Arith.Min(depth, 31)][Arith.Min(expandedNodes - 1, 63)];
                }

                if (X > 0 && R > 0)
                {
                    R--;
                }

                for (;;)
                {
                    if (expandedNodes == 1)
                    {
                        score = -Search(-beta, -alpha, depth + X - 1, ply + 1, checkingMove, true, isPv);
                    }
                    else
                    {
                        score = -Search(-alpha - 1, -alpha, Arith.Max(depth + X - R - 1, 0), ply + 1, checkingMove, true, false);
                        if (score > alpha)
                        {
                            score = -Search(-beta, -alpha, Arith.Max(depth + X - R - 1, 0), ply + 1, checkingMove, true, R == 0);
                        }
                    }

                    if (R > 0 && score > alpha)
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

                if (score > alpha)
                {
                    alpha = score;
                    bestMove = move;

                    if (score >= beta)
                    {
                        if (Move.IsQuiet(move))
                        {
                            killerMoves.Add(move, ply);
                            history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                        }

                        break;
                    }
                }
            }

            moveListPool.Return(moveList);

            if (wasAborted)
            {
                return 0;
            }

            if (expandedNodes == 0)
            {
                return inCheck ? -Constants.CHECKMATE_SCORE + ply : Contempt;
            }

            TtTran.Add(board.Hash, depth, ply, originalAlpha, beta, alpha, bestMove);
            return alpha;
        }

        public int Quiesce(int alpha, int beta, int ply, bool inCheck, int qsPly = 0)
        {
            NodesVisited++;
            seldepth = Arith.Max(seldepth, ply);

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board, alpha, beta);
            }

            var repeated = board.PositionRepeated();
            if (repeated.Repeated)
            {
                return Contempt;
            }

            history.SideToMove = board.SideToMove;
            if (!inCheck)
            {
                int standPatScore = evaluation.Compute(board, alpha, beta);
                if (standPatScore >= beta)
                {
                    return standPatScore;
                }

                alpha = Arith.Max(alpha, standPatScore);
            }

#if DEBUG
            string fen = board.ToFenString();
#endif 

            int expandedNodes = 0;
            MoveList moveList = moveListPool.Get();
            IEnumerable<ulong> moves =
                inCheck ? board.EvasionMoves(history, moveList) : board.QMoves(ply, qsPly, moveList);

            foreach (ulong move in moves)
            {
                if (!board.MakeMove(move))
                {
                    continue;
                }

                expandedNodes++;

                bool checkingMove = board.IsChecked();
                if (!inCheck && !checkingMove && Move.GetScore(move) == Constants.BAD_CAPTURE)
                {
                    board.UnmakeMove();
                    continue;
                }

                int score = -Quiesce(-beta, -alpha, ply + 1, checkingMove, qsPly + 1);
                board.UnmakeMove();

                if (wasAborted)
                {
                    break;
                }

                if (score > alpha)
                {
                    alpha = score;
                    if (score >= beta)
                    {
                        moveListPool.Return(moveList);
                        return beta;
                    }
                }
            }

            if (wasAborted)
            {
                moveListPool.Return(moveList);
                return 0;
            }

            if (expandedNodes == 0)
            {
                if (inCheck)
                {
                    moveListPool.Return(moveList);
                    return -Constants.CHECKMATE_SCORE + ply;
                }

                if (!board.HasLegalMoves(moveList))
                {
                    moveListPool.Return(moveList);
                    return Contempt;
                }
            }

            moveListPool.Return(moveList);
            return alpha;
        }

        private void ReportSearchResults(ref ulong bestMove, ref ulong? ponderMove)
        {
            ulong[] newPv = ExtractPv(Depth);
            if (newPv.Length >= 2 || PV.Length == 0)
            {
                PV = newPv;
            }

            if (PV.Length > 0)
            {
                bestMove = PV[0];
                if (PV.Length > 1)
                {
                    ponderMove = PV[1];
                }
                else
                {
                    ponderMove = null;
                }
            }
            else if (bestMove != 0)
            {
                PV = EmptyPv;
                if (board.IsLegalMove(bestMove))
                {
                    board.MakeMove(bestMove);
                    PV = MergeMove(PV, bestMove);

                    if (ponderMove != null && board.IsLegalMove(ponderMove.Value))
                    {
                        PV = MergeMove(PV, ponderMove.Value);
                    }

                    board.UnmakeMove();
                }
            }

            if (IsCheckmate(Score, out int mateIn))
            {
                Uci.InfoMate(Depth, seldepth, mateIn, NodesVisited, time.Elapsed, PV, TtTran.Usage);
            }
            else
            {
                Uci.Info(Depth, seldepth, Score, NodesVisited, time.Elapsed, PV, TtTran.Usage);
            }
        }

        private ulong[] ExtractPv(int depth)
        {
            int maxDepth = depth + 4;
            MoveList result = moveListPool.Get();
            Board bd = board.Clone();
            int d = 0;

            while (++d < maxDepth && TtTran.TryGetBestMove(bd.Hash, out ulong bestMove))
            {
                if (!bd.IsLegalMove(bestMove))
                {
                    break;
                }

                bd.MakeMove(bestMove);
                result.Add(bestMove);
            }

            ulong[] array = result.ToArray();
            moveListPool.Return(result);
            return array;
        }

        private static ulong[] MergeMove(ulong[] pv, ulong move)
        {
            Array.Resize(ref pv, pv.Length + 1);
            Array.Copy(pv, 0, pv, 1, pv.Length - 1);
            pv[0] = move;
            return pv;
        }

        private void UpdateTtWithPv(ulong[] pv, int depth)
        {
            Board bd = board.Clone();
            for (int n = 0; n < pv.Length; n++)
            {
                ulong move = pv[n];
                if (!bd.IsLegalMove(move))
                {
                    break;
                }
                TtTran.Add(bd.Hash, (short)depth--, n, -short.MaxValue, short.MaxValue, Score, move);
                bd.MakeMove(move);
            }
        }

        private static bool IsCheckmate(int score, out int mateIn)
        {
            mateIn = 0;
            int absScore = Arith.Abs(score);
            bool checkMate = absScore is >= Constants.CHECKMATE_SCORE - Constants.MAX_PLY and <= Constants.CHECKMATE_SCORE;
            if (checkMate)
            {
                mateIn = ((Constants.CHECKMATE_SCORE - absScore + 1) / 2) * Arith.Sign(score);
            }

            return checkMate;
        }

        public bool TryGetCpuLoad(DateTime start, out int cpuLoad)
        {
            cpuLoad = 0;
            if ((DateTime.Now - start).TotalMilliseconds < 1000)
            {
                return false;
            }

            cpuLoad = cpuStats.CpuLoad;
            return true;
        }

        public bool MustAbort => NodesVisited >= maxNodes ||
                         ((NodesVisited & CHECK_TC_NODES_MASK) == 0 && time.CheckTimeBudget());

        public int CalcExtension(bool inCheck)
        {
            int extension = 0;
            if (inCheck)
            {
                extension++;
            }

            if (Move.IsPromote(board.LastMove))
            {
                extension++;
            }

            if (extension > 0)
            {
                ulong move = board.LastMove;
                Piece capture = Move.GetCapture(move);
                int pieceValue = capture != Piece.None ? capture.Value() : 0;
                if (Move.Compare(move, Move.NullMove) == 0 ||
                    board.PostMoveStaticExchangeEval(board.SideToMove.Other(), move) - pieceValue <= 0)
                {
                    return 1;
                }
            }

            return 0;
        }

        public int Contempt
        {
            get
            {
                int contempt = board.TotalMaterial > Evaluation.EndGamePhaseMaterial ? -50 : 0;
                if (board.SideToMove == Engine.Color)
                {
                    return contempt;
                }

                return -contempt;
            }
        }

        public int Depth { get; private set; }
        public ulong[] PV { get; private set; }
        public int Score { get; private set; }
        public long NodesVisited { get; private set; }
        public bool Pondering { get; set; }
        public bool CanPonder { get; set; }
        public bool CollectStats { get; set; } = false;
        public IEnumerable<ChessStats> Stats => stats;

        public Evaluation Eval
        {
            get => evaluation;
            set => evaluation = value;
        }



        private readonly Board board;
        private readonly TimeControl time;
        private Evaluation evaluation;
        private readonly int maxSearchDepth;
        private readonly long maxNodes;
        private readonly History history = new();
        private readonly KillerMoves killerMoves = new();
        private bool wasAborted = false;
        private readonly ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);
        private readonly List<ChessStats> stats = new();
        private readonly CpuStats cpuStats = new();
        private readonly DateTime startDateTime;
        private bool startReporting = false;
        private int seldepth;

        internal static readonly ulong[] EmptyPv = Array.Empty<ulong>();
        internal static readonly int[] Window = { 25, 100, Constants.INFINITE_WINDOW };
        internal static readonly int[] FutilityMargin = { 0, 200, 400, 600, 800 };

        internal static readonly int[][] LMR =
        {
            #region lmr data
            new []
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            new []
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            new []
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            new []
            {
                0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
            },
            new []
            {
                0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2
            },
            new []
            {
                0, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
            },
            new []
            {
                0, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4
            },
            new []
            {
                0, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6
            },
            new []
            {
                0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                0, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                0, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                0, 1, 1, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                0, 1, 1, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4,
                4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7
            },
            new []
            {
                0, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4,
                4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7
            },
            new []
            {
                0, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new []
            {
                0, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new []
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new []
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new []
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new []
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new []
            {
                0, 1, 1, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            }
            #endregion lmr data
        };

        internal static readonly int[] LMP = { 0, 5, 10, 15, 20 };
                                               

        internal static readonly int[] NMP =
        {
            #region nmp data
            0, 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 
            5, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10
            #endregion nmp data
        };

    }
}
