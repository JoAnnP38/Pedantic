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

using System.Runtime.CompilerServices;
using Pedantic.Genetics;
using Pedantic.Tablebase;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class BasicSearch
    {
        public const int CHECK_TC_NODES_MASK = 127;
        internal const int WAIT_TIME = 50;
        internal const int ONE_MOVE_MAX_DEPTH = 5;
        internal const int LMR_DEPTH_LIMIT = 47;
        internal const int LMR_MOVE_LIMIT = 63;
        internal const int STATIC_NULL_MOVE_MAX_DEPTH = 6;
        internal const int STATIC_NULL_MOVE_MARGIN = 75; 
        internal const int NMP_MIN_DEPTH = 3;
        internal const int NMP_BASE_REDUCTION = 2;
        internal const int NMP_INC_DIVISOR = 4; 
        internal const int RAZOR_MAX_DEPTH = 3;
        internal const int IID_MIN_DEPTH = 5;
        internal const int LMP_MAX_HISTORY = 32;
        internal const int PRUNING_DEPTH = 3;

        public BasicSearch(Board board, GameClock time, int maxSearchDepth, long maxNodes = long.MaxValue - 100, bool randomSearch = false) 
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
            string location = "0";
            string position = board.ToFenString();
            try
            {
                Engine.Color = board.SideToMove;
                Depth = 0;
                long startNodes = 0;
                ulong? ponderMove = null;
                MoveList moveList = new();
                oneLegalMove = board.OneLegalMove(moveList, out ulong bestMove);
                
#if USE_TB                
                if (ProbeRootTb(moveList, out ulong tbMove))
                {
                    Uci.BestMove(tbMove, null);
                    return;
                }
#endif                

                Eval.CalcMaterialAdjustment(board);
                bool inCheck = board.IsChecked();
                Score = Quiesce(-Constants.INFINITE_WINDOW, Constants.INFINITE_WINDOW, 0, inCheck);
                location = "1";
                while (++Depth <= maxSearchDepth && time.CanSearchDeeper())
                {
                    time.StartInterval();
                    history.Rescale();
                    UpdateTtWithPv(PV, Depth);
                    int alpha = -Constants.INFINITE_WINDOW;
                    int beta = Constants.INFINITE_WINDOW;
                    int iAlpha = 0, iBeta = 0, result;
                    seldepth = 0;
                    location = "2";
                    do
                    {
                        if (Depth > Constants.WINDOW_MIN_DEPTH)
                        {
                            alpha = Window[iAlpha] == Constants.INFINITE_WINDOW
                                ? -Constants.INFINITE_WINDOW
                                : Score - Window[iAlpha];
                            location = "3";
                            beta = Window[iBeta] == Constants.INFINITE_WINDOW
                                ? Constants.INFINITE_WINDOW
                                : Score + Window[iBeta];
                        }
                        location = "4";
                        result = SearchRoot(alpha, beta, Depth, inCheck);
                        location = "5";
                        if (wasAborted)
                        {
                            break;
                        }

                        ulong bm = bestMove;
                        ulong? pm = ponderMove;

                        if (result <= alpha)
                        {
                            ++iAlpha;
                            ReportSearchResults(result, TtFlag.UpperBound);
                        }
                        else if (result >= beta)
                        {
                            ++iBeta;
                            ReportSearchResults(result, TtFlag.LowerBound);
                        }
                    } while (result <= alpha || result >= beta);

                    location = "6";
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

                    location = "7";
                    startNodes = NodesVisited;
                    Score = result;
                    ReportSearchResults(ref bestMove, ref ponderMove);

                    location = "8";
                    if (Depth == ONE_MOVE_MAX_DEPTH && oneLegalMove)
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
                    location = "9";
                    bool waiting = false;
                    while (time.Infinite && !wasAborted)
                    {
                        waiting = true;
                        Thread.Sleep(WAIT_TIME);
                    }

                    location = "10";
                    if (waiting)
                    {
                        ReportSearchResults(ref bestMove, ref ponderMove);
                    }

                    location = "11";
                }

                if (TryGetCpuLoad(startDateTime, out int cpuLoad))
                {
                    Uci.Usage(cpuLoad);
                }

                location = "12";
                Uci.BestMove(bestMove, CanPonder ? ponderMove : null);
                location = "13";
                Uci.Debug("Incrementing hash table version.");
                TtTran.IncrementVersion();
            }
            catch (Exception ex)
            {
                string msg =
                    $"Search: Unexpected exception occurred at location '{location}' and at position '{position}'.";
                Console.Error.WriteLine(msg);
                Console.Error.WriteLine(ex.ToString());
                Uci.Log(msg);
                Util.TraceError(ex.ToString());
                throw;
            }
        }

        public int SearchRoot(int alpha, int beta, int depth, bool inCheck)
        {
            int originalAlpha = alpha;
            depth = Math.Min(depth, Constants.MAX_PLY - 1);
            InitPv(0);
            
            int X = CalcExtension(inCheck);

            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            int expandedNodes = 0;
            bool raisedAlpha = false;
            history.SideToMove = board.SideToMove;
            MoveList moveList = GetMoveList();
            ulong bestMove = 0ul;
            IEnumerable<ulong> moves = board.Moves(0, killerMoves, history, moveList);

            foreach (ulong move in moves)
            {
                if (!board.MakeMoveNs(move))
                {
                    continue;
                }

                expandedNodes++;
                if (startReporting || (DateTime.Now - startDateTime).TotalMilliseconds >= 1000)
                {
                    startReporting = true;
                    Uci.CurrentMove(depth, move, expandedNodes, NodesVisited, TtTran.Usage);
                }

                bool checkingMove = board.IsChecked();
                bool isQuiet = Move.IsQuiet(move);
                bool badCapture = Move.IsCapture(move) && Move.GetScore(move) == Constants.BAD_CAPTURE;
                bool interesting = inCheck || checkingMove || (!isQuiet && !badCapture) || !raisedAlpha;

                int R = 0;
                if (!interesting && !killerMoves.Exists(0, move))
                {
                    R = LMR[Math.Min(depth, LMR_DEPTH_LIMIT)][Math.Min(expandedNodes - 1, LMR_MOVE_LIMIT)];
                }

                if (X > 0 && R > 0)
                {
                    R--;
                }

                int score;
                if (!raisedAlpha)
                {
                    score = -Search(-beta, -alpha, depth + X - 1, 1, checkingMove);
                }
                else
                {
                    score = -Search(-alpha - 1, -alpha, Math.Max(depth + X - R - 1, 0), 1, checkingMove, isPv: false);

                    if (score > alpha && R > 0)
                    {
                        score = -Search(-alpha - 1, -alpha, depth + X - 1, 1, checkingMove, isPv: false);
                    }

                    if (score > alpha)
                    {
                        score = -Search(-beta, -alpha, depth + X - 1, 1, checkingMove);
                    }
                }

                board.UnmakeMoveNs();

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

                    MergePv(0, move);
                }
            }

            ReturnMoveList(moveList);

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
            NodesVisited++;
            depth = Math.Min(depth, Constants.MAX_PLY - 1);
            seldepth = Math.Max(seldepth, ply);
            InitPv(ply);

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board, alpha, beta);
            }

            (bool repeated, _) = board.PositionRepeated();
            if (repeated)
            {
                return Contempt;
            }

            // This is a trick from CPW-Engine which I do not understand
            // but I leave the code here anyways.
            alpha = Math.Max(alpha, -Constants.CHECKMATE_SCORE + ply - 1);
            beta = Math.Min(beta, Constants.CHECKMATE_SCORE - ply);

            if (alpha >= beta)
            {
                return alpha;
            }

            if (TtTran.TryGetScore(board.Hash, depth, ply, ref alpha, ref beta, out int score, out ulong bestMove))
            { 
                return score;
            }
            
#if USE_TB
            if (ProbeTb(depth, ply, alpha, beta, out score))
            {
                ++tbHits;
                return score;
            }
#endif

            int X = CalcExtension(inCheck);
            if (depth + X <= 0)
            {
                return Quiesce(alpha, beta, ply, inCheck);
            }

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            int eval = evaluation.Compute(board, alpha, beta);
            bool canPrune = false;

            if (!inCheck && !isPv)
            {
                // static null move pruning (reverse futility pruning)
                if (depth <= STATIC_NULL_MOVE_MAX_DEPTH && eval >= beta + depth * STATIC_NULL_MOVE_MARGIN)
                {
                    return eval;
                }

                // null move pruning
                if (canNull && depth >= NMP_MIN_DEPTH && eval >= beta && board.PieceCount(board.SideToMove) > 1)
                {
                    int R = NMP[depth];
                    //int R = NmpReduction(depth);
                    if (board.MakeMove(Move.NullMove))
                    {
                        score = -Search(-beta, -beta + 1, Math.Max(depth - R - 1, 0), ply + 1, false, false, false);
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
                if (canNull)
                {
                    if (depth <= RAZOR_MAX_DEPTH && !IsPromotionThreat(board.LastMove))
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
                    }

                    if (depth <= PRUNING_DEPTH)
                    {
                        // enable LMP pruning
                        canPrune = true;
                    }
                }
            }

            // Internal Iterative Deepening (IID)
            // This will make sure there is a bestMove in the transposition table
            // if we find ourselves here and there is none. Should improve worst
            // case scenario of search blowing up.
            if (canNull && isPv && depth >= IID_MIN_DEPTH && bestMove == 0)
            {
                Search(alpha, beta, depth - 2, ply, inCheck);
                if (wasAborted)
                {
                    return 0;
                }

                InitPv(ply);
            }

            int expandedNodes = 0;
            history.SideToMove = board.SideToMove;
            MoveList moveList = GetMoveList();
            bestMove = 0ul;
            IEnumerable<ulong> moves = board.Moves(ply, killerMoves, history, moveList);

#if DEBUG
            if (ply == 0)
            {
                KillerMoves.KillerMove km = killerMoves.GetKillers(ply);
                Util.TraceInfo($"KILLERS: Depth {depth}, Ply {ply}, Move {Move.ToLongString(km.Killer0)}");
                Util.TraceInfo($"KILLERS: Depth {depth}, Ply {ply}, Move {Move.ToLongString(km.Killer1)}");
            }
            string fen = board.ToFenString();
#endif

            foreach (ulong move in moves)
            {
                if (!board.MakeMoveNs(move))
                {
                    continue;
                }

                expandedNodes++;

                bool checkingMove = board.IsChecked();
                bool isQuiet = Move.IsQuiet(move);
                bool isKiller = isQuiet && killerMoves.Exists(ply, move);
                bool badCapture = Move.IsCapture(move) && Move.GetScore(move) == Constants.BAD_CAPTURE /* Move.IsBadCapture(move) */;
                bool interesting = inCheck || checkingMove || (!isQuiet && !badCapture) || isKiller || expandedNodes == 1;

                if (canPrune && !interesting && !IsPromotionThreat(move))
                {
                    // late move pruning
                    if (expandedNodes > LMP[depth])
                    {
                        board.UnmakeMoveNs();
                        continue;
                    }

                    // see-based pruning (bad captures have already been found bad by see)
                    if (badCapture || board.PostMoveStaticExchangeEval(history.SideToMove, move) < 0)
                    {
                        board.UnmakeMoveNs();
                        continue;
                    }
                }

#if DEBUG
                if (ply == 0)
                {
                    Util.TraceInfo($"Depth {depth}, Ply {ply}, Move {Move.ToLongString(move)}");
                }
#endif

                int R = 0;
                if (!interesting)
                {
                    R = LMR[Math.Min(depth, LMR_DEPTH_LIMIT)][Math.Min(expandedNodes - 1, LMR_MOVE_LIMIT)];
                }

                if (X > 0 && R > 0)
                {
                    R--;
                }

                if (expandedNodes == 1)
                {
                    score = -Search(-beta, -alpha, depth + X - 1, ply + 1, checkingMove, true, isPv);
                }
                else
                {
                    score = -Search(-alpha - 1, -alpha, Math.Max(depth + X - R - 1, 0), ply + 1, checkingMove, isPv: false);

                    if (score > alpha && R > 0)
                    {
                        score = -Search(-alpha - 1, -alpha, depth + X - 1, ply + 1, checkingMove, isPv: false);
                    }

                    if (score > alpha)
                    {
                        score = -Search(-beta, -alpha, depth + X - 1, ply + 1, checkingMove);
                    }
                }

                board.UnmakeMoveNs();

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

                    MergePv(ply, move);
                }
            }

            ReturnMoveList(moveList);

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
            seldepth = Math.Max(seldepth, ply);

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board, alpha, beta);
            }

            (bool repeated, _) = board.PositionRepeated();
            if (repeated)
            {
                return Contempt;
            }

            if (!inCheck)
            {
                int standPatScore = evaluation.Compute(board, alpha, beta);
                if (standPatScore >= beta)
                {
                    return standPatScore;
                }

                alpha = Math.Max(alpha, standPatScore);
            }

#if DEBUG
            string fen = board.ToFenString();
#endif 

            int expandedNodes = 0;
            MoveList moveList = GetMoveList();
            IEnumerable<ulong> moves = inCheck ? 
                board.EvasionMoves(moveList) : 
                board.QMoves(ply, qsPly, moveList);

            foreach (ulong move in moves)
            {
                if (!board.MakeMoveNs(move))
                {
                    continue;
                }

                expandedNodes++;

                bool checkingMove = board.IsChecked();
                if (!inCheck && !checkingMove && Move.GetScore(move) == Constants.BAD_CAPTURE)
                {
                    board.UnmakeMoveNs();
                    continue;
                }

                int score = -Quiesce(-beta, -alpha, ply + 1, checkingMove, qsPly + 1);
                board.UnmakeMoveNs();

                if (wasAborted)
                {
                    break;
                }

                if (score > alpha)
                {
                    alpha = score;
                    if (score >= beta)
                    {
                        ReturnMoveList(moveList);
                        return beta;
                    }
                }
            }

            ReturnMoveList(moveList);

            if (wasAborted)
            {
                return 0;
            }

            return alpha;
        }

#if USE_TB
        private bool ProbeTb(int depth, int ply, int alpha, int beta, out int score)
        {
            score = 0;
            if (Syzygy.IsInitialized && depth >= UciOptions.SyzygyProbeDepth && 
                board.HalfMoveClock == 0 && board.Castling == CastlingRights.None &&
                BitOps.PopCount(board.All) <= Syzygy.TbLargest)
            {
                TbResult result = Syzygy.ProbeWdl(board.Units(Color.White), board.Units(Color.Black), 
                    board.Pieces(Color.White, Piece.King)   | board.Pieces(Color.Black, Piece.King),
                    board.Pieces(Color.White, Piece.Queen)  | board.Pieces(Color.Black, Piece.Queen),
                    board.Pieces(Color.White, Piece.Rook)   | board.Pieces(Color.Black, Piece.Rook),
                    board.Pieces(Color.White, Piece.Bishop) | board.Pieces(Color.Black, Piece.Bishop),
                    board.Pieces(Color.White, Piece.Knight) | board.Pieces(Color.Black, Piece.Knight),
                    board.Pieces(Color.White, Piece.Pawn)   | board.Pieces(Color.Black, Piece.Pawn),
                    0, 0, (uint)(board.EnPassantValidated != Index.NONE ? board.EnPassantValidated : 0), 
                    board.SideToMove == Color.White);

                if (result == TbResult.TbFailure)
                {
                    return false;
                }

                tbHits++;
                TtFlag flag = TtFlag.Exact;
                if (result.Wdl == TbGameResult.Win)
                {
                    score = Constants.CHECKMATE_BASE - (Constants.MAX_PLY + ply);
                    flag = TtFlag.LowerBound;
                }
                else if (result.Wdl == TbGameResult.Loss)
                {
                    score = -Constants.CHECKMATE_BASE + (Constants.MAX_PLY + ply);
                    flag = TtFlag.UpperBound;
                }
                else
                {
                    score = (int)result.Wdl;
                }

                if (flag == TtFlag.Exact || 
                    (flag == TtFlag.UpperBound && score <= alpha) ||
                    (flag == TtFlag.LowerBound && score >= beta))
                {
                    TtTran.Add(board.Hash, Constants.MAX_PLY, ply, alpha, beta, score, 0ul);
                    return true;
                }
            }
            return false;
        }

        private bool ProbeRootTb(MoveList moveList, out ulong move)
        {
            move = 0;
            if (Syzygy.IsInitialized && UciOptions.SyzygyProbeRoot && BitOps.PopCount(board.All) <= Syzygy.TbLargest)
            {
                TbResult result = Syzygy.ProbeRoot(board.Units(Color.White), board.Units(Color.Black), 
                    board.Pieces(Color.White, Piece.King)   | board.Pieces(Color.Black, Piece.King),
                    board.Pieces(Color.White, Piece.Queen)  | board.Pieces(Color.Black, Piece.Queen),
                    board.Pieces(Color.White, Piece.Rook)   | board.Pieces(Color.Black, Piece.Rook),
                    board.Pieces(Color.White, Piece.Bishop) | board.Pieces(Color.Black, Piece.Bishop),
                    board.Pieces(Color.White, Piece.Knight) | board.Pieces(Color.Black, Piece.Knight),
                    board.Pieces(Color.White, Piece.Pawn)   | board.Pieces(Color.Black, Piece.Pawn),
                    (uint)board.HalfMoveClock, (uint)board.Castling, 
                    (uint)(board.EnPassantValidated != Index.NONE ? board.EnPassantValidated : 0), 
                    board.SideToMove == Color.White, null);

                int from = (int)result.From;
                int to = (int)result.To;
                uint tbPromotes = result.Promotes;
                Piece promote = (Piece)(5 - tbPromotes);
                promote = promote == Piece.King ? Piece.None : promote;

                for (int n = 0; n < moveList.Count; n++)
                {
                    move = moveList[n];
                    if (Move.GetFrom(move) == from && Move.GetTo(move) == to && Move.GetPromote(move) == promote)
                    {
                        if (board.MakeMove(move))
                        {
                            board.UnmakeMove();
                            return true;
                        }
                    }
                }
            }
            return false;
        }
#endif

        private void ReportSearchResults(int score, TtFlag flag)
        {
            if (Depth > Constants.WINDOW_MIN_DEPTH)
            {
                Uci.Info(Depth, seldepth, score, NodesVisited, time.Elapsed, PV, TtTran.Usage, tbHits, flag);
            }
        }

        private void ReportSearchResults(ref ulong bestMove, ref ulong? ponderMove)
        {
            bool bestMoveChanged = false;
            ulong oldBestMove = bestMove;
            PV = GetPv();
            PV = ExtractPv(PV);

            if (PV.Length > 0)
            {
                bestMove = PV[0];
                if (Move.Compare(bestMove, oldBestMove) != 0)
                {
                    bestMoveChanged = true;
                }

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

            if (bestMoveChanged)
            {
                ++rootChanges;
            }

            if (Depth > 4)
            {
                time.AdjustTime(oneLegalMove, bestMoveChanged, rootChanges);
            }

            if (IsCheckmate(Score, out int mateIn))
            {
                Uci.InfoMate(Depth, seldepth, mateIn, NodesVisited, time.Elapsed, PV, TtTran.Usage, tbHits);
            }
            else
            {
                Uci.Info(Depth, seldepth, Score, NodesVisited, time.Elapsed, PV, TtTran.Usage, tbHits);
            }
        }

        private ulong[] ExtractPv(ulong[] pv)
        {
            MoveList result = moveListPool.Get();
            Board bd = board.Clone();
            int d = 0;
            positions.Clear();

            for (int n = 0; n < pv.Length; n++)
            {
                if (!bd.MakeMove(pv[n]))
                {
                    throw new InvalidOperationException($"Invalid move in PV: {pv[n]}");
                }

                positions.Add(bd.Hash);
                d++;
            }

            while (d++ < Constants.MAX_PLY && TtTran.TryGetBestMoveWithFlags(bd.Hash, out TtFlag flag, out ulong bestMove))
            {
                if (flag != TtFlag.Exact || !bd.IsLegalMove(bestMove))
                {
                    break;
                }

                bd.MakeMove(bestMove);
                if (positions.Contains(bd.Hash))
                {
                    break;
                }

                positions.Add(bd.Hash);
                result.Add(bestMove);
            }


            ulong[] array = result.ToArray();
            moveListPool.Return(result);
            return AppendPv(ref pv, array);
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
            for (int n = 0; n < pv.Length && depth > 0; n++)
            {
                ulong move = pv[n];
                if (!bd.IsLegalMove(move))
                {
                    break;
                }
                TtTran.Add(bd.Hash, (short)--depth, n, -short.MaxValue, short.MaxValue, Score, move);
                bd.MakeMove(move);
            }
        }

        private static bool IsCheckmate(int score, out int mateIn)
        {
            mateIn = 0;
            int absScore = Math.Abs(score);
            bool checkMate = absScore is >= Constants.CHECKMATE_SCORE - Constants.MAX_PLY * 2 and <= Constants.CHECKMATE_SCORE;
            if (checkMate)
            {
                mateIn = ((Constants.CHECKMATE_SCORE - absScore + 1) / 2) * Math.Sign(score);
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

        public MoveList GetMoveList()
        {
            board.PushBoardState();
            return moveListPool.Get();
        }

        public void ReturnMoveList(MoveList moveList)
        {
            moveListPool.Return(moveList);
            board.PopBoardState();
        }

        public int Contempt
        {
            get
            {
                int contempt = board.TotalMaterialNoKings > Evaluation.EndGamePhaseMaterial ? -50 : 0;
                if (board.SideToMove == Engine.Color)
                {
                    return contempt;
                }

                return -contempt;
            }
        }

        public void InitPv(int ply)
        {
            pvLength[ply] = 0;
        }

        public void MergePv(int ply, ulong move)
        {
            pvLength[ply] = pvLength[ply + 1] + 1;
            pvTable[ply][0] = move;
            Array.Copy(pvTable[ply + 1], 0, pvTable[ply], 1, pvLength[ply + 1]);
        }

        public ulong[] GetPv()
        {
            ulong[] pv = new ulong[pvLength[0]];
            Array.Copy(pvTable[0], pv, pv.Length);
            return pv;
        }

        public ulong[] AppendPv(ref ulong[] pv, ulong[] moves)
        {
            if (moves.Length > 0)
            {
                int append = pv.Length;
                Array.Resize(ref pv, pv.Length + moves.Length);
                Array.Copy(moves, 0, pv, append, moves.Length);
            }

            return pv;
        }

        public bool IsPromotionThreat(ulong move)
        {
            if (Move.IsPawnMove(move))
            {
                int to = Move.GetTo(move);
                int c = (int)board.PieceBoard[to].Color;
                int toRank = Index.GetRank(Index.NormalizedIndex[c][to]);
                return toRank >= Coord.RANK_6;
            }
            return false;
        }

        public int Depth { get; private set; }
        public int Score { get; private set; }
        public ulong[] PV { get; private set; }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NmpReduction(int depth)
        {
            return NMP_BASE_REDUCTION + Math.Max(depth - 3, 0) / NMP_INC_DIVISOR + 1;
        }

        private readonly Board board;
        private readonly GameClock time;
        private Evaluation evaluation;
        private readonly int maxSearchDepth;
        private readonly long maxNodes;
        private readonly History history = new();
        private readonly KillerMoves killerMoves = new();
        private bool wasAborted = false;
        private bool oneLegalMove = false;
        private int rootChanges = 0;
        private readonly HashSet<ulong> positions = new(Constants.MAX_PLY);
        private readonly ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);
        private readonly List<ChessStats> stats = new();
        private readonly CpuStats cpuStats = new();
        private readonly DateTime startDateTime;
        private bool startReporting = false;
        private int seldepth;
        private long tbHits = 0;
        private readonly ulong[][] pvTable = Mem.Allocate2D<ulong>(Constants.MAX_PLY, Constants.MAX_PLY);
        private readonly int[] pvLength = new int[Constants.MAX_PLY];

        internal static readonly ulong[] EmptyPv = Array.Empty<ulong>();
        // Optimized 6/21/2023: 33, 100, 200, 300, INF
        internal static readonly int[] Window = { 33, 100, 200, 300, Constants.INFINITE_WINDOW };
        internal static readonly int[] FutilityMargin = { 0, 200, 400, 600, 800 };

        internal static readonly sbyte[][] LMR =
        {
            #region LMR data
            new sbyte[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            new sbyte[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            new sbyte[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            new sbyte[]
            {
                0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
            },
            new sbyte[]
            {
                0, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2
            },
            new sbyte[]
            {
                0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
            },
            new sbyte[]
            {
                0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4
            },
            new sbyte[]
            {
                0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new sbyte[]
            {
                0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new sbyte[]
            {
                0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4,
                4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4,
                4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4,
                4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4,
                4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5,
                5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
                5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
                5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5,
                5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            },
            new sbyte[]
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9
            },
            new sbyte[]
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9
            },
            new sbyte[]
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9
            },
            new sbyte[]
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9
            },
            new sbyte[]
            {
                0, 1, 1, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 5, 5, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
                8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9
            },
            #endregion LMR data
        };

        internal static readonly sbyte[] LMP = { 0, 6, 12, 18, 24, 30 };
                                               
        internal static readonly sbyte[] NMP =
        {
            #region nmp data
            0, 0, 0, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9, 10, 
            10, 10, 10, 11, 11, 11, 11, 12, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 14, 15, 15, 15, 15, 16, 16, 16, 16, 
            17, 17, 17, 17, 18
            #endregion nmp data
        };

    }
}
