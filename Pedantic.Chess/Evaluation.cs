using System.Runtime.CompilerServices;

using Pedantic.Collections;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    // TODO: Penalize friendly knight the further it is away from own king if friendly King Safety < enemy King Safety
    // TODO: Penalize trapped pieces (i.e. zero mobility)
    // TODO: Reward pieces defending other pieces
    // TODO: Reduce material or pawn material advantage for winning side if OCB
    // TODO: Penalize each potential square that is open for a slider to attack king
    // TODO: Piece Type bonus for King Zone Attack eval
    public sealed class Evaluation
    {
        public const ulong D0_CENTER_CONTROL_MASK = 0x0000001818000000ul;
        public const ulong D1_CENTER_CONTROL_MASK = 0x00003C24243C0000ul;
        public const ulong DARK_SQUARES_MASK = 0xAA55AA55AA55AA55ul;
        public const ulong LITE_SQUARES_MASK = 0x55AA55AA55AA55AAul;
        public const int LAZY_MARGIN = 500;
        public const int MAX_ATTACK_LEN = 16;
        public const int MOPUP_TABLE_LEN = Constants.MAX_SQUARES * Constants.MAX_KING_BUCKETS;
        private const int mopup_pst_friendly = 0;
        private const int mopup_pst_enemy = 1;

        public enum EgKingPst { Normal, MopUp, MopUpNBLight, MopUpNBDark }

        unsafe public struct EvalInfo
        {
            public ulong Pawns;
            public ulong PassedPawns;
            public ulong PawnAttacks;
            public ulong KingAttacks;
            public ulong PieceAttacks;
            public ulong MobilityArea;
            public sbyte TotalPawns;
            public sbyte KI;
            public KingPlacement KP;
            public short Material;
            public bool CanWin;
            public EgKingPst KingPst;
            public byte CastlingRightsMask;
            public byte AttackCount;
            public fixed ulong Attacks[MAX_ATTACK_LEN];
        }

        static Evaluation()
        {
            wts = new HceWeights();
            InitializeMopUpTables();
        }

        public Evaluation(EvalCache cache, bool random = false, bool useMopup = true)
        {
            this.cache = cache;
            this.random = random;
            this.useMopup = useMopup;
        }

        public short Compute(Board board, int alpha = -Constants.INFINITE_WINDOW, int beta = Constants.INFINITE_WINDOW)
        {
            if (cache.ProbeEvalCache(board.Hash, board.SideToMove, out EvalCache.EvalCacheItem item))
            {
                return item.EvalScore;
            }

            Span<EvalInfo> evalInfo = stackalloc EvalInfo[2];
            InitializeEvalInfo(board, evalInfo);
            gamePhase = GetGamePhase(board, evalInfo);
            bool isLazy = false;
            short score = gamePhase == GamePhase.EndGameMopup ? 
                ComputeMopUp(board, evalInfo) :
                ComputeNormal(board, evalInfo, alpha, beta, ref isLazy);

            if (random)
            {
                score += (short)rand.Next(-8, 9);
            }

            if ((score > 0 && !evalInfo[0].CanWin) || (score < 0 && !evalInfo[1].CanWin))
            {
                score >>= 3;
            }
            else if (board.HalfMoveClock > 84)
            {
                score = (short)((score * Math.Min(100 - Math.Min(board.HalfMoveClock, 100), 16)) >> 4);
            }

            score = StmScore(board.SideToMove, score);

            if (!isLazy)
            {
                cache.SaveEval(board.Hash, score, board.SideToMove);
            }

            return score;
        }

        public short ComputeNormal(Board board, Span<EvalInfo> evalInfo, int alpha, int beta, ref bool isLazy)
        {
            Score score = EvalMaterialAndPst(board, evalInfo, Color.White);
            score -= EvalMaterialAndPst(board, evalInfo, Color.Black);

            int normalScore = score.NormalizeScore(board.Phase);
            int evalScore = StmScore(board.SideToMove, normalScore);

            // if material + PST evaluation is outside alpha/beta window just return 
            if (evalScore < alpha - LAZY_MARGIN || evalScore > beta + LAZY_MARGIN)
            {
                isLazy = true;
                return (short)normalScore;
            }
            
            score += ProbePawnCache(board, evalInfo);
            score += EvalMobility(board, evalInfo, Color.White);
            score -= EvalMobility(board, evalInfo, Color.Black);
            score += EvalKingSafety(board, evalInfo, Color.White);
            score -= EvalKingSafety(board, evalInfo, Color.Black);
            score += EvalPieces(board, evalInfo, Color.White);
            score -= EvalPieces(board, evalInfo, Color.Black);
            score += EvalPassedPawns(board, evalInfo, Color.White);
            score -= EvalPassedPawns(board, evalInfo, Color.Black);
            score += EvalThreats(board, evalInfo, Color.White);
            score -= EvalThreats(board, evalInfo, Color.Black);
            score += EvalMisc(board, evalInfo, Color.White);
            score -= EvalMisc(board, evalInfo, Color.Black);

            return score.NormalizeScore(board.Phase);
        }

        public short ComputeMopUp(Board board, Span<EvalInfo> evalInfo)
        {
            Span<Score> eval = stackalloc Score[Constants.MAX_COLORS];
            eval.Clear();

            for (Color color = Color.White; color <= Color.Black; color++)
            {
                Color other = color.Other();
                int c = (int)color;
                int o = (int)other;
                KingPlacement kp = evalInfo[c].KP;

                eval[c] += board.Material[c];
                Score[][] kingTable = evalInfo[c].KingPst switch
                {
                    EgKingPst.MopUp         => mopUpMate,
                    EgKingPst.MopUpNBLight  => mopUpMateNBLight,
                    EgKingPst.MopUpNBDark   => mopUpMateNBDark,
                    _ => mopUpEmpty
                };

                for (ulong bb = board.Units(color); bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int sq = BitOps.TzCount(bb);
                    int normalSq = Index.NormalizedIndex[c][sq];
                    Piece piece = board.PieceBoard[sq].Piece;

                    if (piece != Piece.King || evalInfo[c].KingPst == EgKingPst.Normal)
                    {
                        eval[c] += wts.FriendlyPieceSquareValue(piece, kp, normalSq);
                        eval[c] += wts.EnemyPieceSquareValue(piece, kp, normalSq);
                    }
                    else
                    {
                        int offset = kp.Friendly * Constants.MAX_SQUARES + normalSq;
                        eval[c] += kingTable[mopup_pst_friendly][offset];

                        offset = kp.Enemy * Constants.MAX_SQUARES + normalSq;
                        eval[c] += kingTable[mopup_pst_enemy][offset];
                    }
                }

                if (evalInfo[c].CanWin)
                {
                    short k2kDist = (short)((14 - Index.ManhattanDistance(evalInfo[c].KI, evalInfo[o].KI)) * 10);
                    eval[c] += new Score(k2kDist, k2kDist);

                    for (ulong bb = board.Pieces(color, Piece.Knight); bb != 0; bb = BitOps.ResetLsb(bb))
                    {
                        int sq = BitOps.TzCount(bb);
                        short n2kDist = (short)((14 - Index.ManhattanDistance(sq, evalInfo[o].KI)) * 10);
                        eval[c] += new Score(n2kDist, n2kDist);
                    }
                }
            }

            Score score = eval[0] - eval[1];
            return score.NormalizeScore(board.Phase);
        }

        public static void InitializeEvalInfo(Board board, Span<EvalInfo> evalInfo)
        {
            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int c = (int)color;

                evalInfo[c].Pawns = board.Pieces(color, Piece.Pawn);
                if (color == Color.White)
                {
                    evalInfo[c].PawnAttacks = ((evalInfo[c].Pawns & ~Board.MaskFile(Index.A1)) << 7) |
                                              ((evalInfo[c].Pawns & ~Board.MaskFile(Index.H1)) << 9);
                    evalInfo[c].CastlingRightsMask = (byte)CastlingRights.WhiteRights;
                }
                else
                {
                    evalInfo[c].PawnAttacks = ((evalInfo[c].Pawns & ~Board.MaskFile(Index.H1)) >> 7) |
                                              ((evalInfo[c].Pawns & ~Board.MaskFile(Index.A1)) >> 9);
                    evalInfo[c].CastlingRightsMask = (byte)CastlingRights.BlackRights;
                }
                evalInfo[c].KI = (sbyte)BitOps.TzCount(board.Pieces(color, Piece.King));
                evalInfo[c].KingAttacks = board.GetPieceMoves(Piece.King, evalInfo[c].KI);
                evalInfo[c].PieceAttacks = 0;
                evalInfo[c].Material = board.Material[c].NormalizeScore(board.Phase);
                evalInfo[c].CanWin = evalInfo[c].Pawns != 0;
                evalInfo[c].KingPst = EgKingPst.Normal;
                evalInfo[c].PassedPawns = 0;
                evalInfo[c].MobilityArea = 0;
                evalInfo[c].AttackCount = 0;
            }
            var (WhiteCanWin, BlackCanWin) = CanWin(board, evalInfo);
            evalInfo[0].TotalPawns = evalInfo[1].TotalPawns = (sbyte)BitOps.PopCount(evalInfo[0].Pawns | evalInfo[1].Pawns);
            evalInfo[0].KP = Index.GetKingPlacement(Color.White, evalInfo[0].KI, evalInfo[1].KI);
            evalInfo[0].CanWin = WhiteCanWin;
            evalInfo[0].MobilityArea = ~(board.Units(Color.White) | evalInfo[1].PawnAttacks);
            evalInfo[1].TotalPawns = evalInfo[0].TotalPawns;
            evalInfo[1].KP = Index.GetKingPlacement(Color.Black, evalInfo[1].KI, evalInfo[0].KI);
            evalInfo[1].CanWin = BlackCanWin;
            evalInfo[1].MobilityArea = ~(board.Units(Color.Black) | evalInfo[0].PawnAttacks);
        }

        public static bool SufficientMatingMaterial(Board board, Span<EvalInfo> evalInfo, Color side)
        {
            int numKnights = BitOps.PopCount(board.Pieces(side, Piece.Knight));
            int numBishops = BitOps.PopCount(board.Pieces(side, Piece.Bishop));
            bool case1 = (board.Pieces(side, Piece.Rook) | board.Pieces(side, Piece.Queen)) != 0;
            bool case2 = (numKnights >= 1 && numBishops >= 1) || numBishops >= 2 || numKnights >= 3;
            int o = (int)side.Other();

            if (case1 || case2)
            {
                evalInfo[o].KingPst = EgKingPst.MopUp;
            }

            if (!case1 && numKnights == 1 && numBishops == 1)
            {
                int sq = BitOps.TzCount(board.Pieces(side, Piece.Bishop));
                evalInfo[o].KingPst = Index.IsDark(sq) ? EgKingPst.MopUpNBDark : EgKingPst.MopUpNBLight;
            }
            return case1 || case2;
        }

        public static (bool WhiteCanWin, bool BlackCanWin) CanWin(Board board, Span<EvalInfo> evalInfo)
        {
            bool whiteCanWin = false, blackCanWin = false;
            int winMargin = wts.PieceValue(Piece.Pawn).NormalizeScore(board.Phase) * 4;

            if (evalInfo[0].Pawns != 0)
            {
                whiteCanWin = true;
            }
            else if (evalInfo[0].Material - evalInfo[1].Material >= winMargin)
            {
                whiteCanWin = SufficientMatingMaterial(board, evalInfo, Color.White);
            }

            if (evalInfo[1].Pawns != 0)
            {
                blackCanWin = true;
                evalInfo[1].KingPst = EgKingPst.Normal;
            }
            else if (evalInfo[1].Material - evalInfo[0].Material >= winMargin)
            {
                blackCanWin = SufficientMatingMaterial(board, evalInfo, Color.Black);
            }

            return (whiteCanWin, blackCanWin);
        }

        public GamePhase GetGamePhase(Board board, Span<EvalInfo> evalInfo)
        {
            GamePhase gamePhase = board.GamePhase;

            if (useMopup && gamePhase == GamePhase.EndGame && evalInfo[0].TotalPawns == 0)
            {
                Color winning = evalInfo[0].CanWin ? Color.White : (evalInfo[1].CanWin ? Color.Black : Color.None);
                if (winning != Color.None)
                {
                    gamePhase = GamePhase.EndGameMopup;
                }
            }
            return gamePhase;
        }

        public static Score EvalMaterialAndPst(Board board, Span<EvalInfo> evalInfo, Color color)
        {
            int c = (int)color;
            Score evalPst = board.Material[c];
            KingPlacement kp = evalInfo[c].KP;

            for (ulong bb = board.Units(color); bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                int normalSq = Index.NormalizedIndex[c][sq];
                Square square = board.PieceBoard[sq];

                evalPst += wts.FriendlyPieceSquareValue(square.Piece, kp, normalSq);
                evalPst += wts.EnemyPieceSquareValue(square.Piece, kp, normalSq);
            }

            return evalPst;
        }

        public static Score EvalPawns(Board _, Span<EvalInfo> evalInfo, Color color)
        {
            Color other = color.Other();
            int c = (int)color;
            int o = (int)other;
            ulong pawns = evalInfo[c].Pawns;
            ulong otherPawns = evalInfo[o].Pawns;
            Score pawnScore = (Score)0;

            if (evalInfo[c].Pawns == 0)
            {
                return (Score)0;
            }

            ulong pawnRams = (color == Color.White ? otherPawns >> 8 : otherPawns << 8);

            for (ulong p = pawns; p != 0; p = BitOps.ResetLsb(p))
            {
                int sq = BitOps.TzCount(p);
                int normalSq = Index.NormalizedIndex[c][sq];
                Ray ray = Board.Vectors[sq];
                ulong friendMask = color == Color.White ? ray.North : ray.South;
                ulong sqMask = BitOps.GetMask(sq);
                bool canBeBackward = true;

                if ((otherPawns & PassedPawnMasks[c, sq]) == 0 && (pawns & friendMask) == 0)
                {
                    pawnScore += wts.PassedPawn(normalSq);
                    evalInfo[c].PassedPawns |= sqMask;
                    canBeBackward = false;
                }

                if ((pawns & IsolatedPawnMasks[sq]) == 0)
                {
                    pawnScore += wts.IsolatedPawn;
                    canBeBackward = false;
                }

                if (canBeBackward && (pawns & BackwardPawnMasks[c, sq]) == 0)
                {
                    pawnScore += wts.BackwardPawn;
                }

                if ((pawns & AdjacentPawnMasks[sq]) != 0)
                {
                    pawnScore += wts.PhalanxPawns(normalSq);
                }

                if ((evalInfo[c].PawnAttacks & sqMask) != 0)
                {
                    pawnScore += wts.ChainedPawn(normalSq);
                }

                if ((pawnRams & sqMask) != 0)
                {
                    pawnScore += wts.PawnRam(normalSq);
                }
            }

            for (int file = 0; file < Constants.MAX_COORDS; file++)
            {
                int count = BitOps.PopCount(pawns & Board.MaskFile(file));
                if (count > 1)
                {
                    pawnScore += --count * wts.DoubledPawn;
                }
            }

            return pawnScore;
        }

        public static Score EvalPassedPawns(Board board, Span<EvalInfo> evalInfo, Color color)
        {
            Score evalPassed = Score.Zero;
            Color other = color.Other();
            int c = (int)color;
            int o = (int)other;

            for (ulong p = evalInfo[c].PassedPawns; p != 0; p = BitOps.ResetLsb(p))
            {
                int sq = BitOps.TzCount(p);
                Ray ray = Board.Vectors[sq];
                ulong bb = color == Color.White ? 
                    ray.South & ~Board.RevVectors[BitOps.LzCount(ray.South & board.All)].South :
                    ray.North & ~Board.Vectors[BitOps.TzCount(ray.North & board.All)].North;

                if ((bb & board.Pieces(color, Piece.Rook)) != 0)
                {
                    evalPassed += wts.RookBehindPassedPawn;
                }

                int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                if (normalRank < Coord.RANK_4)
                {
                    continue;
                }

                int promoteSq = Index.NormalizedIndex[c][Index.ToIndex(Index.GetFile(sq), Coord.RANK_8)];
                if (board.PieceCount(other) == 1 &&
                    Index.Distance(sq, promoteSq) < 
                    Index.Distance(evalInfo[o].KI, promoteSq) - (other == board.SideToMove ? 1 : 0))
                {
                    evalPassed += wts.KingOutsidePasserSquare;
                }

                int blockSq = Board.PawnPlus[c, sq];
                int dist = Index.Distance(blockSq, evalInfo[c].KI);
                evalPassed += dist * wts.PasserFriendlyKingDistance;

                dist = Index.Distance(blockSq, evalInfo[o].KI);
                evalPassed += dist * wts.PasserEnemyKingDistance;

                ulong advanceMask = BitOps.GetMask(blockSq);
                ulong attackMask = evalInfo[o].PawnAttacks | evalInfo[o].KingAttacks | evalInfo[o].PieceAttacks;
                if ((advanceMask & board.All) == 0 && (advanceMask & attackMask) == 0)
                {
                    evalPassed += wts.PassedPawnCanAdvance(normalRank);
                }
            }

            ulong enemyPassedPawns = evalInfo[o].PassedPawns;
            if (other == Color.White)
            {
                enemyPassedPawns <<= 8;
            }
            else
            {
                enemyPassedPawns >>= 8;
            }
            ulong blockers = enemyPassedPawns & board.Units(color);
            for (ulong p = blockers; p != 0; p = BitOps.ResetLsb(p))
            {
                int sq = BitOps.TzCount(p);
                int normalRank = Index.GetRank(Index.NormalizedIndex[o][sq]);
                Piece blocker = board.PieceBoard[sq].Piece;
                evalPassed += wts.BlockedPassedPawn(blocker, normalRank - 1);
            }

            return evalPassed;
        }

        public static Score EvalPieces(Board board, Span<EvalInfo> evalInfo, Color color)
        {
            Color other = color.Other();
            int c = (int)color;
            int o = (int)other;
            Score pieceEval = (Score)0;

            ulong pawns = evalInfo[c].Pawns;
            ulong knights = board.Pieces(color, Piece.Knight);
            ulong bishops = board.Pieces(color, Piece.Bishop);

            if (BitOps.PopCount(bishops) >= 2)
            {
                pieceEval += wts.BishopPair;
            }

            // outposts
            for (ulong bb = knights | bishops; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                ulong sqMask = BitOps.GetMask(sq);

                if (normalRank > Coord.RANK_4 && (evalInfo[c].PawnAttacks & sqMask) != 0)
                {
                    Piece pc = board.PieceBoard[sq].Piece;
                    if (pc == Piece.Knight)
                    {
                        pieceEval += wts.KnightOutpost;
                    }
                    else
                    {
                        pieceEval += wts.BishopOutpost;
                    }
                }

                if ((bishops & sqMask) != 0)
                {
                    // eval bad bishop pawns
                    ulong badPawns = pawns & DARK_SQUARES_MASK;
                    if (!Index.IsDark(sq))
                    {
                        badPawns = pawns & LITE_SQUARES_MASK;
                    }

                    for (ulong bbBadPawn = badPawns; bbBadPawn != 0; bbBadPawn = BitOps.ResetLsb(bbBadPawn))
                    {
                        int pawnSq = BitOps.TzCount(bbBadPawn);
                        int normalSq = Index.NormalizedIndex[c][pawnSq];
                        pieceEval += wts.BadBishopPawn(normalSq);
                    }
                }
            }

            // evaluate rooks
            ulong rooks = board.Pieces(color, Piece.Rook);
            ulong otherPawns = evalInfo[o].Pawns;
            ulong allPawns = pawns | otherPawns;
            int enemyKingRank = Index.GetRank(Index.NormalizedIndex[c][evalInfo[o].KI]);

            for (ulong bb = rooks; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                ulong maskFile = Board.MaskFile(sq);
                ulong maskRank = Board.MaskRank(sq);

                if (normalRank == Coord.RANK_7 && ((otherPawns & maskRank) != 0 || enemyKingRank >= Coord.RANK_7))
                {
                    pieceEval += wts.RookOn7thRank;
                }

                if ((maskFile & allPawns) == 0)
                {
                    pieceEval += wts.RookOnOpenFile;

                    if (IsDoubled(board, sq))
                    {
                        pieceEval += wts.DoubleRooksOnFile;
                    }
                }

                if ((maskFile & pawns) == 0 && (maskFile & otherPawns) != 0)
                {
                    pieceEval += wts.RookOnHalfOpenFile;

                    if (IsDoubled(board, sq))
                    {
                        pieceEval += wts.DoubleRooksOnFile;
                    }
                }
            }

            ulong queens = board.Pieces(color, Piece.Queen);

            for (ulong bb = queens; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                ulong mask = Board.MaskFile(sq);

                if ((mask & allPawns) == 0)
                {
                    pieceEval += wts.QueenOnOpenFile;
                }

                if ((mask & pawns) == 0 && (mask & otherPawns) != 0)
                {
                    pieceEval += wts.QueenOnHalfOpenFile;
                }
            }

            return pieceEval;
        }

        public unsafe static Score EvalKingSafety(Board board, Span<EvalInfo> evalInfo, Color color)
        {
            Score kingSafety = Score.Zero;
            Color other = color.Other();
            int c = (int)color;
            int o = (int)other;

            int enemyKI = evalInfo[o].KI;
            for (int n = 0; n < evalInfo[c].AttackCount; n++)
            {
                ulong attacks = evalInfo[c].Attacks[n] & ~evalInfo[o].PawnAttacks;
                kingSafety += BitOps.PopCount(attacks & KingProximity[0, enemyKI]) * wts.KingAttack(0);
                kingSafety += BitOps.PopCount(attacks & KingProximity[1, enemyKI]) * wts.KingAttack(1);
                kingSafety += BitOps.PopCount(attacks & KingProximity[2, enemyKI]) * wts.KingAttack(2);
            }

            int ki = evalInfo[c].KI;
            ulong pawns = evalInfo[c].Pawns;
            // pawn shield
            kingSafety += BitOps.PopCount(pawns & KingProximity[0, ki]) * wts.PawnShield(0);
            kingSafety += BitOps.PopCount(pawns & KingProximity[1, ki]) * wts.PawnShield(1);
            kingSafety += BitOps.PopCount(pawns & KingProximity[2, ki]) * wts.PawnShield(2);

            if (board.HasCastled[c])
            {
                kingSafety += wts.CastlingComplete;
            }
            else
            {
                ulong castling = evalInfo[c].CastlingRightsMask & (ulong)board.Castling;
                kingSafety += BitOps.PopCount(castling) * wts.CastlingAvailable;
            }

            ulong kingFileMask = Board.MaskFile(evalInfo[c].KI);
            ulong otherPawns = evalInfo[o].Pawns;
            ulong allPawns = pawns | otherPawns;

            if ((kingFileMask & allPawns) == 0)
            {
                kingSafety += wts.KingOnOpenFile;
            }

            if ((kingFileMask & pawns) == 0 && (kingFileMask & otherPawns) != 0)
            {
                kingSafety += wts.KingOnHalfOpenFile;
            }

            ulong kingDiagonalMask = Diagonals[evalInfo[c].KI];
            if (BitOps.PopCount(kingDiagonalMask) > 3 && (kingDiagonalMask & allPawns) == 0)
            {
                kingSafety += wts.KingOnOpenDiagonal;
            }

            kingDiagonalMask = Antidiagonals[evalInfo[c].KI];
            if (BitOps.PopCount(kingDiagonalMask) > 3 && (kingDiagonalMask & allPawns) == 0)
            {
                kingSafety += wts.KingOnOpenDiagonal;
            }

            if (board.Pieces(other, Piece.Queen) != 0 || board.CanBishopAttack(other, evalInfo[c].KI))
            {
                ulong bbAttacks = board.GetPieceMoves(Piece.Bishop, evalInfo[c].KI) & evalInfo[o].MobilityArea;
                kingSafety += BitOps.PopCount(bbAttacks) * wts.KingAttackSquareOpen;
            }

            if (board.OrthogonalSliders(other) != 0)
            {
                ulong bbAttacks = board.GetPieceMoves(Piece.Rook, evalInfo[c].KI) & evalInfo[o].MobilityArea;
                kingSafety += BitOps.PopCount(bbAttacks) * wts.KingAttackSquareOpen;
            }

            // TODO: Add evaluation term for distance of knight to friendly king and 
            // opponent king

            return kingSafety;
        }

        public unsafe static Score EvalMobility(Board board, Span<EvalInfo> evalInfo, Color color)
        {
            int c = (int)color;
            Score mobility = (Score)0;

            for (Piece piece = Piece.Knight; piece <= Piece.Queen; piece++)
            {
                for (ulong pcLoc = board.Pieces(color, piece); pcLoc != 0; pcLoc = BitOps.ResetLsb(pcLoc))
                {
                    int from = BitOps.TzCount(pcLoc);
                    ulong moves = board.GetPieceMoves(piece, from);
                    evalInfo[c].PieceAttacks |= moves;
                    if (evalInfo[c].AttackCount < MAX_ATTACK_LEN)
                    {
                        evalInfo[c].Attacks[evalInfo[c].AttackCount++] = moves;
                    }
                    int moveCount = BitOps.PopCount(moves & evalInfo[c].MobilityArea);
                    if (moveCount > 0)
                    {
                        mobility += moveCount * wts.PieceMobility(piece);
                    }
                    else
                    {
                        mobility += wts.TrappedPiece(piece);
                    }
                }
            }

            return mobility;
        }

        public static Score EvalThreats(Board board, Span<EvalInfo> evalInfo, Color color)
        {
            Color other = color.Other();
            int c = (int)color;
            int o = (int)other;
            ulong pawns = evalInfo[c].Pawns;
            ulong otherPawns = evalInfo[o].Pawns;
            Score evalThreats = Score.Zero;

            ulong targets = board.Units(other) & ~(otherPawns | board.Pieces(other, Piece.King));
            if (targets == 0)
            {
                return evalThreats;
            }

            ulong pushAttacks;
            if (color == Color.White)
            {
                ulong pawnPushes = (pawns << 8) & ~board.All;

                pushAttacks = ((pawnPushes & ~Board.MaskFile(Index.A1)) << 7) |
                              ((pawnPushes & ~Board.MaskFile(Index.H1)) << 9);
            }
            else
            {
                ulong pawnPushes = (pawns >> 8) & ~board.All;

                pushAttacks = ((pawnPushes & ~Board.MaskFile(Index.H1)) >> 7) |
                              ((pawnPushes & ~Board.MaskFile(Index.A1)) >> 9);
            }
        
            for (ulong bb = evalInfo[c].PawnAttacks & targets; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                Piece defender = board.PieceBoard[sq].Piece;
                evalThreats += wts.PieceThreat(Piece.Pawn, defender);
            }

            for (ulong bb = pushAttacks & targets; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                Piece defender = board.PieceBoard[sq].Piece;
                evalThreats += wts.PawnPushThreat(defender);
            }

            targets &= ~evalInfo[o].PawnAttacks;

            if (targets == 0)
            {
                return evalThreats;
            }

            for (Piece attacker = Piece.Knight; attacker <= Piece.Queen; attacker++)
            {
                for (ulong bb = board.Pieces(color, attacker); bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int from = BitOps.TzCount(bb);
                    ulong bb2 = board.GetPieceMoves(attacker, from);
                    for (ulong attacks = bb2 & targets; attacks != 0; attacks = BitOps.ResetLsb(attacks))
                    {
                        int to = BitOps.TzCount(attacks);
                        Piece defender = board.PieceBoard[to].Piece;

                        if (attacker.Value() <= defender.Value())
                        {
                            evalThreats += wts.PieceThreat(attacker, defender);
                        }
                    }
                }
            }

            return evalThreats;
        }

        public unsafe static Score EvalMisc(Board board, Span<EvalInfo> evalInfo, Color color)
        {
            int c = (int)color;
            Score evalMisc = Score.Zero;

            for (ulong bb = evalInfo[c].Pawns; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                ulong attacks = Board.PawnCaptures(color, sq);
                evalMisc += BitOps.PopCount(attacks & D0_CENTER_CONTROL_MASK) * wts.CenterControl(0);
                evalMisc += BitOps.PopCount(attacks & D1_CENTER_CONTROL_MASK) * wts.CenterControl(1);
            }

            for (int n = 0; n < evalInfo[c].AttackCount; n++)
            {
                evalMisc += BitOps.PopCount(evalInfo[c].Attacks[n] & D0_CENTER_CONTROL_MASK) * wts.CenterControl(0);
                evalMisc += BitOps.PopCount(evalInfo[c].Attacks[n] & D1_CENTER_CONTROL_MASK) * wts.CenterControl(1);
            }

            if (color == board.SideToMove)
            {
                evalMisc += wts.TempoBonus;
            }

            // TODO: add knight distance to enemy/friendly king

            return evalMisc;
        }

        public Score ProbePawnCache(Board board, Span<EvalInfo> evalInfo)
        {
            Score pawnScore;
            if (cache.ProbePawnCache(board.PawnHash, out EvalCache.PawnCacheItem item))
            {
                evalInfo[0].PassedPawns = item.PassedPawns & board.Units(Color.White);
                evalInfo[1].PassedPawns = item.PassedPawns & board.Units(Color.Black);
                pawnScore = item.Eval;
            }
            else
            {
                pawnScore = EvalPawns(board, evalInfo, Color.White) - EvalPawns(board, evalInfo, Color.Black);
                ulong passedPawns = evalInfo[0].PassedPawns | evalInfo[1].PassedPawns;
                cache.SavePawnEval(board.PawnHash, passedPawns, pawnScore);
            }

            return pawnScore;
        }

        public static HceWeights Weights
        {
            get
            {
                return wts;
            }
            set
            {
                wts = value;
                InitializeMopUpTables();
            }
        }

        public static void InitializeMopUpTables()
        {
            Mem.Clear(mopUpMate);
            Mem.Clear(mopUpMateNBLight);
            Mem.Clear(mopUpMateNBDark);

            for (int bucket = 0; bucket < Constants.MAX_KING_BUCKETS; bucket++)
            {
                int kpValue = (bucket << 4) | bucket;
                KingPlacement kp = new KingPlacement(kpValue);

                for (int sq = 0; sq < Constants.MAX_SQUARES; sq++)
                {
                    Score friendlyKingScore = wts.FriendlyPieceSquareValue(Piece.King, kp, sq);
                    Score enemyKingScore = wts.EnemyPieceSquareValue(Piece.King, kp, sq);
                    int index = kp.Friendly * Constants.MAX_SQUARES + sq;

                    if (friendlyKingScore != Score.Zero)
                    {
                        mopUpMate[mopup_pst_friendly][index] = new Score(friendlyKingScore.MgScore, (short)(-egMopUpMate[sq] / 2));
                        mopUpMateNBLight[mopup_pst_friendly][index] = new Score(friendlyKingScore.MgScore, (short)(-egMopUpMateNBLight[sq] / 2));
                        mopUpMateNBDark[mopup_pst_friendly][index] = new Score(friendlyKingScore.MgScore, (short)(-egMopUpMateNBDark[sq] / 2));
                    }

                    mopUpMate[mopup_pst_enemy][index] = new Score(enemyKingScore.MgScore, (short)(-egMopUpMate[sq] / 2));
                    mopUpMateNBLight[mopup_pst_enemy][index] = new Score(enemyKingScore.MgScore, (short)(-egMopUpMateNBLight[sq] / 2));
                    mopUpMateNBDark[mopup_pst_enemy][index] = new Score(enemyKingScore.MgScore, (short)(-egMopUpMateNBDark[sq] / 2));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short StmScore(Color stm, int score)
        {
            return (short)(((int)stm * -2 + 1) * score);
        }

        // Check whether piece on square (sq) is doubled with another piece
        // of the same type. Only true if this the piece is *behind* the 
        // other piece.
        public static bool IsDoubled(Board board, int sq)
        {
            Square square = board.PieceBoard[sq];
            Ray ray = Board.Vectors[sq];
            ulong bbPotentials = board.Pieces(square.Color, square.Piece);

            if (square.Color == Color.White)
            {
                ulong bb = bbPotentials & ray.North;
                if (bb != 0)
                {
                    int sq2 = BitOps.TzCount(bb);
                    Ray ray2 = Board.Vectors[sq2];
                    // only indicate double if no other piece is between them
                    return (ray.North & ray2.South & board.All) == 0;
                }
            }
            else
            {
                ulong bb = bbPotentials & ray.South;
                if (bb != 0)
                {
                    int sq2 = BitOps.LzCount(bb);
                    Ray ray2 = Board.Vectors[sq2];
                    // only indicate double if no other piece is between them
                    return (ray.South & ray2.North & board.All) == 0;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short CanonicalPieceValues(Piece piece)
        {
            return canonicalPieceValues[(int)piece + 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short PiecePhaseValue(Piece piece)
        {
            return piecePhaseValues[(int)piece + 1];
        }

        public static Score[][] MopUpMate => mopUpMate;
        public static Score[][] MopUpMateNBLight => mopUpMateNBLight;
        public static Score[][] MopUpMateNBDark => mopUpMateNBDark;

        private readonly EvalCache cache;
        private readonly bool random;
        private readonly bool useMopup;
        private readonly Random rand = new();
        private GamePhase gamePhase;

        private static HceWeights wts;
        private static readonly Score[][] mopUpMate = Mem.Allocate2D<Score>(2, MOPUP_TABLE_LEN);
        private static readonly Score[][] mopUpMateNBLight = Mem.Allocate2D<Score>(2, MOPUP_TABLE_LEN);
        private static readonly Score[][] mopUpMateNBDark = Mem.Allocate2D<Score>(2, MOPUP_TABLE_LEN);
        private static readonly Score[][] mopUpEmpty = Mem.Allocate2D<Score>(1, 1);
        private static readonly short[] canonicalPieceValues = { 0, 100, 300, 300, 500, 900, 9900 };

        public static readonly sbyte[] piecePhaseValues = { 0, 1, 2, 2, 4, 8, 0 };

        public static readonly Array2D<ulong> PassedPawnMasks = new (Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region PassedPawnMasks data

            // white passed pawn masks
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0003030303030000ul, 0x0007070707070000ul, 0x000E0E0E0E0E0000ul, 0x001C1C1C1C1C0000ul,
            0x0038383838380000ul, 0x0070707070700000ul, 0x00E0E0E0E0E00000ul, 0x00C0C0C0C0C00000ul,
            0x0003030303000000ul, 0x0007070707000000ul, 0x000E0E0E0E000000ul, 0x001C1C1C1C000000ul,
            0x0038383838000000ul, 0x0070707070000000ul, 0x00E0E0E0E0000000ul, 0x00C0C0C0C0000000ul,
            0x0003030300000000ul, 0x0007070700000000ul, 0x000E0E0E00000000ul, 0x001C1C1C00000000ul,
            0x0038383800000000ul, 0x0070707000000000ul, 0x00E0E0E000000000ul, 0x00C0C0C000000000ul,
            0x0003030000000000ul, 0x0007070000000000ul, 0x000E0E0000000000ul, 0x001C1C0000000000ul,
            0x0038380000000000ul, 0x0070700000000000ul, 0x00E0E00000000000ul, 0x00C0C00000000000ul,
            0x0003000000000000ul, 0x0007000000000000ul, 0x000E000000000000ul, 0x001C000000000000ul,
            0x0038000000000000ul, 0x0070000000000000ul, 0x00E0000000000000ul, 0x00C0000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,

            // black passed pawn masks
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000300ul, 0x0000000000000700ul, 0x0000000000000E00ul, 0x0000000000001C00ul,
            0x0000000000003800ul, 0x0000000000007000ul, 0x000000000000E000ul, 0x000000000000C000ul,
            0x0000000000030300ul, 0x0000000000070700ul, 0x00000000000E0E00ul, 0x00000000001C1C00ul,
            0x0000000000383800ul, 0x0000000000707000ul, 0x0000000000E0E000ul, 0x0000000000C0C000ul,
            0x0000000003030300ul, 0x0000000007070700ul, 0x000000000E0E0E00ul, 0x000000001C1C1C00ul,
            0x0000000038383800ul, 0x0000000070707000ul, 0x00000000E0E0E000ul, 0x00000000C0C0C000ul,
            0x0000000303030300ul, 0x0000000707070700ul, 0x0000000E0E0E0E00ul, 0x0000001C1C1C1C00ul,
            0x0000003838383800ul, 0x0000007070707000ul, 0x000000E0E0E0E000ul, 0x000000C0C0C0C000ul,
            0x0000030303030300ul, 0x0000070707070700ul, 0x00000E0E0E0E0E00ul, 0x00001C1C1C1C1C00ul,
            0x0000383838383800ul, 0x0000707070707000ul, 0x0000E0E0E0E0E000ul, 0x0000C0C0C0C0C000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul

            #endregion PassedPawnMasks data
        };

        public static readonly ulong[] IsolatedPawnMasks = 
        {
            #region IsolatedPawnMasks data
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            #endregion IsolatedPawnMasks data
        };

        public static readonly Array2D<ulong> KingProximity = new (3, Constants.MAX_SQUARES)
        {
            #region KingProximity data

            // masks for D0
            0x0000000000000302ul, 0x0000000000000705ul, 0x0000000000000E0Aul, 0x0000000000001C14ul,
            0x0000000000003828ul, 0x0000000000007050ul, 0x000000000000E0A0ul, 0x000000000000C040ul,
            0x0000000000030203ul, 0x0000000000070507ul, 0x00000000000E0A0Eul, 0x00000000001C141Cul,
            0x0000000000382838ul, 0x0000000000705070ul, 0x0000000000E0A0E0ul, 0x0000000000C040C0ul,
            0x0000000003020300ul, 0x0000000007050700ul, 0x000000000E0A0E00ul, 0x000000001C141C00ul,
            0x0000000038283800ul, 0x0000000070507000ul, 0x00000000E0A0E000ul, 0x00000000C040C000ul,
            0x0000000302030000ul, 0x0000000705070000ul, 0x0000000E0A0E0000ul, 0x0000001C141C0000ul,
            0x0000003828380000ul, 0x0000007050700000ul, 0x000000E0A0E00000ul, 0x000000C040C00000ul,
            0x0000030203000000ul, 0x0000070507000000ul, 0x00000E0A0E000000ul, 0x00001C141C000000ul,
            0x0000382838000000ul, 0x0000705070000000ul, 0x0000E0A0E0000000ul, 0x0000C040C0000000ul,
            0x0003020300000000ul, 0x0007050700000000ul, 0x000E0A0E00000000ul, 0x001C141C00000000ul,
            0x0038283800000000ul, 0x0070507000000000ul, 0x00E0A0E000000000ul, 0x00C040C000000000ul,
            0x0302030000000000ul, 0x0705070000000000ul, 0x0E0A0E0000000000ul, 0x1C141C0000000000ul,
            0x3828380000000000ul, 0x7050700000000000ul, 0xE0A0E00000000000ul, 0xC040C00000000000ul,
            0x0203000000000000ul, 0x0507000000000000ul, 0x0A0E000000000000ul, 0x141C000000000000ul,
            0x2838000000000000ul, 0x5070000000000000ul, 0xA0E0000000000000ul, 0x40C0000000000000ul,

            // masks for D1
            0x0000000000070404ul, 0x00000000000F0808ul, 0x00000000001F1111ul, 0x00000000003E2222ul,
            0x00000000007C4444ul, 0x0000000000F88888ul, 0x0000000000F01010ul, 0x0000000000E02020ul,
            0x0000000007040404ul, 0x000000000F080808ul, 0x000000001F111111ul, 0x000000003E222222ul,
            0x000000007C444444ul, 0x00000000F8888888ul, 0x00000000F0101010ul, 0x00000000E0202020ul,
            0x0000000704040407ul, 0x0000000F0808080Ful, 0x0000001F1111111Ful, 0x0000003E2222223Eul,
            0x0000007C4444447Cul, 0x000000F8888888F8ul, 0x000000F0101010F0ul, 0x000000E0202020E0ul,
            0x0000070404040700ul, 0x00000F0808080F00ul, 0x00001F1111111F00ul, 0x00003E2222223E00ul,
            0x00007C4444447C00ul, 0x0000F8888888F800ul, 0x0000F0101010F000ul, 0x0000E0202020E000ul,
            0x0007040404070000ul, 0x000F0808080F0000ul, 0x001F1111111F0000ul, 0x003E2222223E0000ul,
            0x007C4444447C0000ul, 0x00F8888888F80000ul, 0x00F0101010F00000ul, 0x00E0202020E00000ul,
            0x0704040407000000ul, 0x0F0808080F000000ul, 0x1F1111111F000000ul, 0x3E2222223E000000ul,
            0x7C4444447C000000ul, 0xF8888888F8000000ul, 0xF0101010F0000000ul, 0xE0202020E0000000ul,
            0x0404040700000000ul, 0x0808080F00000000ul, 0x1111111F00000000ul, 0x2222223E00000000ul,
            0x4444447C00000000ul, 0x888888F800000000ul, 0x101010F000000000ul, 0x202020E000000000ul,
            0x0404070000000000ul, 0x08080F0000000000ul, 0x11111F0000000000ul, 0x22223E0000000000ul,
            0x44447C0000000000ul, 0x8888F80000000000ul, 0x1010F00000000000ul, 0x2020E00000000000ul,

            // masks for D2
            0x000000000F080808ul, 0x000000001F101010ul, 0x000000003F202020ul, 0x000000007F414141ul,
            0x00000000FE828282ul, 0x00000000FC040404ul, 0x00000000F8080808ul, 0x00000000F0101010ul,
            0x0000000F08080808ul, 0x0000001F10101010ul, 0x0000003F20202020ul, 0x0000007F41414141ul,
            0x000000FE82828282ul, 0x000000FC04040404ul, 0x000000F808080808ul, 0x000000F010101010ul,
            0x00000F0808080808ul, 0x00001F1010101010ul, 0x00003F2020202020ul, 0x00007F4141414141ul,
            0x0000FE8282828282ul, 0x0000FC0404040404ul, 0x0000F80808080808ul, 0x0000F01010101010ul,
            0x000F08080808080Ful, 0x001F10101010101Ful, 0x003F20202020203Ful, 0x007F41414141417Ful,
            0x00FE8282828282FEul, 0x00FC0404040404FCul, 0x00F80808080808F8ul, 0x00F01010101010F0ul,
            0x0F08080808080F00ul, 0x1F10101010101F00ul, 0x3F20202020203F00ul, 0x7F41414141417F00ul,
            0xFE8282828282FE00ul, 0xFC0404040404FC00ul, 0xF80808080808F800ul, 0xF01010101010F000ul,
            0x08080808080F0000ul, 0x10101010101F0000ul, 0x20202020203F0000ul, 0x41414141417F0000ul,
            0x8282828282FE0000ul, 0x0404040404FC0000ul, 0x0808080808F80000ul, 0x1010101010F00000ul,
            0x080808080F000000ul, 0x101010101F000000ul, 0x202020203F000000ul, 0x414141417F000000ul,
            0x82828282FE000000ul, 0x04040404FC000000ul, 0x08080808F8000000ul, 0x10101010F0000000ul,
            0x0808080F00000000ul, 0x1010101F00000000ul, 0x2020203F00000000ul, 0x4141417F00000000ul,
            0x828282FE00000000ul, 0x040404FC00000000ul, 0x080808F800000000ul, 0x101010F000000000ul

            #endregion KingProximity data
        };

        public static readonly Array2D<ulong> BackwardPawnMasks = new (Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region BackwardPawnMasks data

            // masks for white backward pawns
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000200ul, 0x0000000000000500ul, 0x0000000000000A00ul, 0x0000000000001400ul,
            0x0000000000002800ul, 0x0000000000005000ul, 0x000000000000A000ul, 0x0000000000004000ul,
            0x0000000000020200ul, 0x0000000000050500ul, 0x00000000000A0A00ul, 0x0000000000141400ul,
            0x0000000000282800ul, 0x0000000000505000ul, 0x0000000000A0A000ul, 0x0000000000404000ul,
            0x0000000002020200ul, 0x0000000005050500ul, 0x000000000A0A0A00ul, 0x0000000014141400ul,
            0x0000000028282800ul, 0x0000000050505000ul, 0x00000000A0A0A000ul, 0x0000000040404000ul,
            0x0000000202020200ul, 0x0000000505050500ul, 0x0000000A0A0A0A00ul, 0x0000001414141400ul,
            0x0000002828282800ul, 0x0000005050505000ul, 0x000000A0A0A0A000ul, 0x0000004040404000ul,
            0x0000020202020200ul, 0x0000050505050500ul, 0x00000A0A0A0A0A00ul, 0x0000141414141400ul,
            0x0000282828282800ul, 0x0000505050505000ul, 0x0000A0A0A0A0A000ul, 0x0000404040404000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,

            // masks for black backward pawns
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0002020202020000ul, 0x0005050505050000ul, 0x000A0A0A0A0A0000ul, 0x0014141414140000ul,
            0x0028282828280000ul, 0x0050505050500000ul, 0x00A0A0A0A0A00000ul, 0x0040404040400000ul,
            0x0002020202000000ul, 0x0005050505000000ul, 0x000A0A0A0A000000ul, 0x0014141414000000ul,
            0x0028282828000000ul, 0x0050505050000000ul, 0x00A0A0A0A0000000ul, 0x0040404040000000ul,
            0x0002020200000000ul, 0x0005050500000000ul, 0x000A0A0A00000000ul, 0x0014141400000000ul,
            0x0028282800000000ul, 0x0050505000000000ul, 0x00A0A0A000000000ul, 0x0040404000000000ul,
            0x0002020000000000ul, 0x0005050000000000ul, 0x000A0A0000000000ul, 0x0014140000000000ul,
            0x0028280000000000ul, 0x0050500000000000ul, 0x00A0A00000000000ul, 0x0040400000000000ul,
            0x0002000000000000ul, 0x0005000000000000ul, 0x000A000000000000ul, 0x0014000000000000ul,
            0x0028000000000000ul, 0x0050000000000000ul, 0x00A0000000000000ul, 0x0040000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul

            #endregion BackwardPawnMasks data
        };

        public static readonly ulong[] AdjacentPawnMasks = 
        {
            #region AdjacentPawnMasks data

            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000200ul, 0x0000000000000500ul, 0x0000000000000A00ul, 0x0000000000001400ul,
            0x0000000000002800ul, 0x0000000000005000ul, 0x000000000000A000ul, 0x0000000000004000ul,
            0x0000000000020000ul, 0x0000000000050000ul, 0x00000000000A0000ul, 0x0000000000140000ul,
            0x0000000000280000ul, 0x0000000000500000ul, 0x0000000000A00000ul, 0x0000000000400000ul,
            0x0000000002000000ul, 0x0000000005000000ul, 0x000000000A000000ul, 0x0000000014000000ul,
            0x0000000028000000ul, 0x0000000050000000ul, 0x00000000A0000000ul, 0x0000000040000000ul,
            0x0000000200000000ul, 0x0000000500000000ul, 0x0000000A00000000ul, 0x0000001400000000ul,
            0x0000002800000000ul, 0x0000005000000000ul, 0x000000A000000000ul, 0x0000004000000000ul,
            0x0000020000000000ul, 0x0000050000000000ul, 0x00000A0000000000ul, 0x0000140000000000ul,
            0x0000280000000000ul, 0x0000500000000000ul, 0x0000A00000000000ul, 0x0000400000000000ul,
            0x0002000000000000ul, 0x0005000000000000ul, 0x000A000000000000ul, 0x0014000000000000ul,
            0x0028000000000000ul, 0x0050000000000000ul, 0x00A0000000000000ul, 0x0040000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul

            #endregion AdjacentPawnMasks data
        };

        public static readonly ulong[] Diagonals =
        {
            #region Diagonals data
            0x8040201008040201ul, 0x0080402010080402ul, 0x0000804020100804ul, 0x0000008040201008ul,
            0x0000000080402010ul, 0x0000000000804020ul, 0x0000000000008040ul, 0x0000000000000080ul,
            0x4020100804020100ul, 0x8040201008040201ul, 0x0080402010080402ul, 0x0000804020100804ul,
            0x0000008040201008ul, 0x0000000080402010ul, 0x0000000000804020ul, 0x0000000000008040ul,
            0x2010080402010000ul, 0x4020100804020100ul, 0x8040201008040201ul, 0x0080402010080402ul,
            0x0000804020100804ul, 0x0000008040201008ul, 0x0000000080402010ul, 0x0000000000804020ul,
            0x1008040201000000ul, 0x2010080402010000ul, 0x4020100804020100ul, 0x8040201008040201ul,
            0x0080402010080402ul, 0x0000804020100804ul, 0x0000008040201008ul, 0x0000000080402010ul,
            0x0804020100000000ul, 0x1008040201000000ul, 0x2010080402010000ul, 0x4020100804020100ul,
            0x8040201008040201ul, 0x0080402010080402ul, 0x0000804020100804ul, 0x0000008040201008ul,
            0x0402010000000000ul, 0x0804020100000000ul, 0x1008040201000000ul, 0x2010080402010000ul,
            0x4020100804020100ul, 0x8040201008040201ul, 0x0080402010080402ul, 0x0000804020100804ul,
            0x0201000000000000ul, 0x0402010000000000ul, 0x0804020100000000ul, 0x1008040201000000ul,
            0x2010080402010000ul, 0x4020100804020100ul, 0x8040201008040201ul, 0x0080402010080402ul,
            0x0100000000000000ul, 0x0201000000000000ul, 0x0402010000000000ul, 0x0804020100000000ul,
            0x1008040201000000ul, 0x2010080402010000ul, 0x4020100804020100ul, 0x8040201008040201ul,
            #endregion Diagonals data
        };

        public static readonly ulong[] Antidiagonals =
        {
            #region Antidiagonals data
            0x0000000000000001ul, 0x0000000000000102ul, 0x0000000000010204ul, 0x0000000001020408ul,
            0x0000000102040810ul, 0x0000010204081020ul, 0x0001020408102040ul, 0x0102040810204080ul,
            0x0000000000000102ul, 0x0000000000010204ul, 0x0000000001020408ul, 0x0000000102040810ul,
            0x0000010204081020ul, 0x0001020408102040ul, 0x0102040810204080ul, 0x0204081020408000ul,
            0x0000000000010204ul, 0x0000000001020408ul, 0x0000000102040810ul, 0x0000010204081020ul,
            0x0001020408102040ul, 0x0102040810204080ul, 0x0204081020408000ul, 0x0408102040800000ul,
            0x0000000001020408ul, 0x0000000102040810ul, 0x0000010204081020ul, 0x0001020408102040ul,
            0x0102040810204080ul, 0x0204081020408000ul, 0x0408102040800000ul, 0x0810204080000000ul,
            0x0000000102040810ul, 0x0000010204081020ul, 0x0001020408102040ul, 0x0102040810204080ul,
            0x0204081020408000ul, 0x0408102040800000ul, 0x0810204080000000ul, 0x1020408000000000ul,
            0x0000010204081020ul, 0x0001020408102040ul, 0x0102040810204080ul, 0x0204081020408000ul,
            0x0408102040800000ul, 0x0810204080000000ul, 0x1020408000000000ul, 0x2040800000000000ul,
            0x0001020408102040ul, 0x0102040810204080ul, 0x0204081020408000ul, 0x0408102040800000ul,
            0x0810204080000000ul, 0x1020408000000000ul, 0x2040800000000000ul, 0x4080000000000000ul,
            0x0102040810204080ul, 0x0204081020408000ul, 0x0408102040800000ul, 0x0810204080000000ul,
            0x1020408000000000ul, 0x2040800000000000ul, 0x4080000000000000ul, 0x8000000000000000ul,
            #endregion Antidiagonals data
        };

        private static readonly short[] egMopUpMate =
        {
            140, 120, 100,  80,  80, 100, 120, 140,
            120, 100,  60,  40,  40,  60, 100, 120,
            100,  60,  20,   0,   0,  20,  60, 100,
             80,  40,   0,   0,   0,   0,  40,  80,
             80,  40,   0,   0,   0,   0,  40,  80,
            100,  60,  20,   0,   0,  20,  60, 100,
            120, 100,  60,  40,  40,  60, 100, 120,
            140, 120, 100,  80,  80, 100, 120, 140
        };

        private static readonly short[] egMopUpMateNBLight =
        {
             40,  40,  60,  80,  80, 100, 120, 140,
             40,  20,  20,  40,  40,  60, 100, 120,
             60,  20,   0,   0,   0,  20,  60, 100,
             80,  40,   0,   0,   0,   0,  40,  80,
             80,  40,   0,   0,   0,   0,  40,  80,
            100,  60,  20,   0,   0,   0,  40,  60,
            120, 100,  60,  40,  40,  20,  20,  40,
            140, 120, 100,  80,  80,  60,  40,  40
        };

        private static readonly short[] egMopUpMateNBDark =
        {
            140, 120, 100,  80,  80,  60,  40,  40,
            120, 100,  60,  40,  40,  20,  20,  40,
            100,  60,  20,   0,   0,   0,  20,  60,
             80,  40,   0,   0,   0,   0,  40,  80,
             80,  40,   0,   0,   0,   0,  40,  80,
             60,  40,   0,   0,   0,  20,  60, 100,
             40,  20,  20,  40,  40,  60, 100, 120,
             40,  40,  60,  80,  80, 100, 120, 140
        };
    }
}
