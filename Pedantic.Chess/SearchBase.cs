using System.Runtime.CompilerServices;
using Pedantic.Collections;
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

        public virtual void Search()
        {
            Engine.Color = board.SideToMove;
            Depth = 0;
            ulong? ponderMove = null;
            bool oneLegalMove = board.OneLegalMove(out ulong bestMove);

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() && Math.Abs(Result.Score) != Constants.CHECKMATE_SCORE)
            {
                time.StartInterval();
                history.Rescale();
                UpdateTtWithPv(Result.Pv, Depth);

                for (int i = 0; i < window.Length; i++)
                {
                    int alpha = Result.Score - window[i];
                    int beta = Result.Score + window[i];

                    Result = Search(alpha, beta, Depth, 0);

                    if (wasAborted)
                    {
                        break;
                    }

                    if (Result.Score > alpha && Result.Score < beta)
                    {
                        break;
                    }
                }

                if (wasAborted)
                {
                    break;
                }

                ReportSearchResults(ref bestMove, ref ponderMove);

                if (Depth == 5 && oneLegalMove)
                {
                    break;
                }
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
                    ReportSearchResults(ref bestMove, ref ponderMove);
                }
            }
            Uci.BestMove(bestMove, ponderMove);
        }

        public virtual void ScoutSearch()
        {
            Depth = 0;

            while (Depth++ < maxSearchDepth && time.CanSearchDeeper() && !Evaluation.IsCheckmate(Result.Score))
            {
                time.StartInterval();
                history.Rescale();
                UpdateTtWithPv(Result.Pv, Depth);

                Result = Search(-Constants.INFINITE_WINDOW, Constants.INFINITE_WINDOW, Depth, 0);

                if (wasAborted)
                {
                    break;
                }

                Result = new SearchResult(Result.Score, ExtractPv(Result.Pv, Depth));
            }
        }

        public abstract SearchResult Search(int alpha, int beta, int depth, int ply, bool canNull = true, bool isPv = true);

        public virtual SearchResult SearchTt(int alpha, int beta, int depth, int ply, bool canNull = true, bool isPv = true)
        {
            int originalAlpha = alpha;

            if (TtTran.TryGetScore(board.Hash, depth, ply, ref alpha, ref beta, out int ttScore, out ulong move))
            {
                return new SearchResult(ttScore, move);
            }

            SearchResult result = Search(alpha, beta, depth, ply);

            TtTran.Add(board.Hash, depth, ply, originalAlpha, beta, result.Score, result.Pv.Length > 0 ? result.Pv[0] : 0);

            return result;
        }

        protected virtual int QuiesceTt(int alpha, int beta, int ply)
        {
            int originalAlpha = alpha;
            if (TtTran.TryGetScore(board.Hash, 0, ply, ref alpha, ref beta, out int score, out ulong _))
            {
                return score;
            }

            score = Quiesce(alpha, beta, ply);

            TtTran.Add(board.Hash, 0, ply, originalAlpha, beta, score, 0ul);
            return score;
        }

        protected virtual int Quiesce(int alpha, int beta, int ply)
        {
            int originalAlpha = alpha;

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

            if (IsDraw())
            {
                return Contempt;
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
                int score = -QuiesceTt(-beta, -alpha, ply + 1);
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
                        MoveListPool.Return(moveList);
                        return beta;
                    }
                }
            }

            if (wasAborted)
            {
                MoveListPool.Return(moveList);
                return 0;
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
                    return Contempt;
                }
            }

            MoveListPool.Return(moveList);
            return alpha;
        }

        protected virtual int ZwSearch(int beta, int depth, int ply, bool canNull = true)
        {
            if (IsDraw())
            {
                return Contempt;
            }

            if (ply >= Constants.MAX_PLY - 1)
            {
                return evaluation.Compute(board);
            }

            int alpha = Math.Max(beta - 1, -Constants.CHECKMATE_SCORE + ply - 1);
            beta = Math.Min(beta, Constants.CHECKMATE_SCORE - ply);
            if (alpha >= beta)
            {
                return alpha;
            }

            if (depth <= 0)
            {
                return QuiesceTt(beta - 1, beta, ply);
            }

            NodesVisited++;

            if (MustAbort || wasAborted)
            {
                wasAborted = true;
                return 0;
            }

            bool inCheck = board.IsChecked();
            int X = CalcExtension(inCheck);
            bool canReduce = X == 0;
            int eval = evaluation.Compute(board);

            if (canNull && canReduce && depth >= 3 && eval >= beta &&
                board.HasMinorMajorPieces(board.OpponentColor, 600))
            {
                int R = nmp[depth];
                if (board.MakeMove(Move.NullMove))
                {
                    int result = -ZwSearch(1 - beta, depth - R - 1, ply + 1, false);
                    board.UnmakeMove();

                    if (wasAborted)
                    {
                        return 0;
                    }

                    if (result >= beta)
                    {
                        return beta;
                    }
                }
            }                                                                                                                                                                                                                  

            if (canNull && canReduce && depth <= 2 && !Move.IsPawnMove(board.LastMove))
            {
                int threshold = alpha - 300 * depth;
                if (eval < threshold)
                {
                    int score = QuiesceTt(alpha, beta, ply);
                    if (score < threshold)
                    {
                        return alpha;
                    }
                }
            }

            bool canPrune = canReduce && Math.Abs(beta - 1) < Constants.CHECKMATE_BASE && depth < 8 &&
                            eval + futilityMargin[depth] <= beta - 1;

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
                bool isQuiet = Move.IsQuiet(move);
                bool interesting = inCheck || checkingMove || !isQuiet;

                if (canPrune && !interesting && expandedNodes > lmp[depth])
                {
                    board.UnmakeMove();
                    continue;
                }

                int R = 0;
                if (canReduce && !interesting && !killerMoves.Exists(ply, move))
                {
                    R = lmr[Math.Min(depth, 31)][Math.Min(expandedNodes, 63)];
                }

                int score;
                for (;;)
                {
                    score = -ZwSearch(-beta + 1, depth + X - R - 1, ply + 1);

                    if (R > 0 && score >= beta)
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

                if (score >= beta)
                {
                    TtTran.Add(board.Hash, depth, ply, beta - 1, beta, score, move);
                    if (!Move.IsCapture(move))
                    {
                        killerMoves.Add(move, ply);
                        history.Update(Move.GetFrom(move), Move.GetTo(move), depth);
                    }

                    MoveListPool.Return(moveList);
                    return beta;
                }
            }

            MoveListPool.Return(moveList);

            if (wasAborted)
            {
                return 0;
            }

            if (expandedNodes == 0)
            {
                return inCheck ? -Constants.CHECKMATE_SCORE + ply : Contempt;
            }

            return beta - 1;
        }

        protected virtual int ZwSearchTt(int beta, int depth, int ply, bool canNull = true)
        {
            if (TtTran.TryGetScore(board.Hash, depth, ply, beta - 1, beta, out int score))
            {
                return score;
            }

            score = ZwSearch(beta, depth, ply, canNull);

            TtTran.Add(board.Hash, depth, ply, beta - 1, beta, score, 0ul);
            return score;
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
                if (!bd.IsLegalMove(move))
                {
                    break;
                }
                TtTran.Add(bd.Hash, (short)depth--, n, -short.MaxValue, short.MaxValue, Result.Score, move);
                bd.MakeMove(move);
            }
        }

        protected void ReportSearchResults(int score, out ulong[] pv, ref ulong bestMove, ref ulong? ponderMove)
        {
            pv = ExtractPv(EmptyPv, Depth);
            if (pv.Length > 0)
            {
                bestMove = pv[0];
                if (pv.Length > 1)
                {
                    ponderMove = pv[1];
                }
                else
                {
                    ponderMove = null;
                }
            }
            else
            {
                pv = EmptyPv;
                if (board.IsLegalMove(bestMove))
                {
                    pv = MergeMove(pv, bestMove);
                    if (ponderMove != null)
                    {
                        board.MakeMove(bestMove);
                        if (board.IsLegalMove(ponderMove.Value))
                        {
                            pv = MergeMove(pv, ponderMove.Value);
                        }

                        board.UnmakeMove();
                    }
                }
            }
            if (IsCheckmate(score, out int mateIn))
            {
                Uci.InfoMate(Depth, mateIn, NodesVisited, time.Elapsed, pv);
            }
            else
            {
                Uci.Info(Depth, score, NodesVisited, time.Elapsed, pv);
            }

        }

        protected void ReportSearchResults(ref ulong bestMove, ref ulong? ponderMove)
        {
            Result = new SearchResult(Result.Score, ExtractPv(Result.Pv, Depth));
            if (Result.Pv.Length > 0)
            {
                bestMove = Result.Pv[0];
                if (Result.Pv.Length > 1)
                {
                    ponderMove = Result.Pv[1];
                }
                else
                {
                    ponderMove = null;
                }
            }
            else if (bestMove != 0)
            {
                ulong[] pv = EmptyPv;
                if (board.IsLegalMove(bestMove))
                {
                    board.MakeMove(bestMove);
                    pv = MergeMove(pv, bestMove);

                    if (ponderMove != null && board.IsLegalMove(ponderMove.Value))
                    {
                        pv = MergeMove(pv, ponderMove.Value);
                    }

                    board.UnmakeMove();
                }

                Result = new SearchResult(Result.Score, pv);
            }

            if (IsCheckmate(Result.Score, out int mateIn))
            {
                Uci.InfoMate(Depth, mateIn, NodesVisited, time.Elapsed, Result.Pv);
            }
            else
            {
                Uci.Info(Depth, Result.Score, NodesVisited, time.Elapsed, Result.Pv);
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
                        break;
                    }

                    bd.MakeMove(move);
                }

                while (result.Count < depth && TtTran.TryGetBestMove(bd.Hash, out ulong bestMove) && bd.IsLegalMove(bestMove))
                {
                    if (!bd.IsLegalMove(bestMove))
                    {
                        break;
                    }

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
            bool checkMate = absScore >= Constants.CHECKMATE_SCORE - Constants.MAX_PLY &&
                             absScore <= Constants.CHECKMATE_SCORE;
            if (checkMate)
            {
                mateIn = ((Constants.CHECKMATE_SCORE - absScore + 1) / 2) * Math.Sign(score);
            }

            return checkMate;
        }

        protected bool IsDraw()
        {
            return board.HalfMoveClock >= 100 || board.GameDrawnByRepetition() || board.InsufficientMaterialForMate();
        }

        protected int CalcExtension(bool inCheck)
        {
            int extension = 0;
            if (inCheck)
            {
                extension++;
            }

            if (board.IsPromotionThreat(board.LastMove))
            {
                extension++;
            }

            if (Move.IsPromote(board.LastMove))
            {
                extension++;
            }

            return extension > 0 ? 1 : 0;
        }

        protected bool IsRecapture(ulong move)
        {
            return Move.IsCapture(board.LastMove) && Move.IsCapture(move) &&
                   Move.GetTo(board.LastMove) == Move.GetTo(move);
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
        protected static readonly int[] window = { 25, 75, Constants.INFINITE_WINDOW };
        protected static readonly int[] futilityMargin = { 0, 100, 150, 200, 250, 300, 400, 500 };
        protected static readonly ulong recaptureMask = 0x0ffc;

        protected static readonly int[][] lmr =
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
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3,
                3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3,
                3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3,
                3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3,
                3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3,
                3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3,
                3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3,
                3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3,
                3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            },
            new []
            {
                1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3,
                3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6
            }
            #endregion lmr data
        };

        protected static readonly int[] lmp = { 3, 5, 10, 15, 20, 25, 30, 35 };

        protected static readonly int[] nmp =
        {
            #region nmp data
            0, 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5,
            5, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10
            #endregion nmp data
        };
    }
}
