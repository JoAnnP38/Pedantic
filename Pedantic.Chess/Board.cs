using System.Drawing;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Transactions;
using Pedantic.Collections;
using Pedantic.Utilities;
using System.Reflection;
using System.Reflection.Metadata;
using static System.Formats.Asn1.AsnWriter;
using System.Text.RegularExpressions;

namespace Pedantic.Chess
{
    public sealed partial class Board : ICloneable
    {
        private readonly Square[] board = new Square[Constants.MAX_SQUARES];
        private readonly BitBoardArray2D pieces = new(Constants.MAX_COLORS, Constants.MAX_PIECES);
        private readonly ulong[] units = new ulong[Constants.MAX_COLORS];
        private ulong all = 0ul;
        private readonly short[] material = new short[Constants.MAX_COLORS];

        #region Incrementally updated values used by Evaluation

        private readonly short[] opMaterial = new short[Constants.MAX_COLORS];
        private readonly short[] egMaterial = new short[Constants.MAX_COLORS];
        private readonly short[] opPcSquare = new short[Constants.MAX_COLORS];
        private readonly short[] egPcSquare = new short[Constants.MAX_COLORS];
        private readonly bool[] castled = new bool[Constants.MAX_COLORS];
        private ulong pawnHash = 0;

        #endregion

        private Color sideToMove = Color.None;
        private CastlingRights castling = CastlingRights.None;
        private int enPassant = Index.None;
        private int enPassantValidated = Index.None;
        private int halfMoveClock = 0;
        private int fullMoveCounter = 0;
        private ulong hash = 0;

        public struct BoardState
        {
            public ulong Move;
            public Color SideToMove;
            public CastlingRights Castling;
            public int EnPassant;
            public int EnPassantValidated;
            public int HalfMoveClock;
            public int FullMoveCounter;
            public ulong Hash;

            public void Restore(Board board)
            {
                board.sideToMove = SideToMove;
                board.castling = Castling;
                board.enPassant = EnPassant;
                board.enPassantValidated = EnPassantValidated;
                board.halfMoveClock = HalfMoveClock;
                board.fullMoveCounter = FullMoveCounter;
                board.hash = Hash;
            }
        }

        private readonly ValueStack<BoardState> gameStack = new(Constants.MAX_GAME_LENGTH);

        #region Constructors

        static Board()
        {
            for (int sq = 0; sq < Constants.MAX_SQUARES; ++sq)
            {
                revVectors[63 - sq] = vectors[sq];
            }

            InitPieceMasks();
            InitPieceMagicTables();
            InitFancyMagic();
        }

        public Board()
        {
            Clear();
        }

        public Board(string fen)
        {
            if (!LoadFenPosition(fen))
            {
                throw new ArgumentException(@"Invalid FEN position specified.", nameof(fen));
            }
        }

        private Board(Board other)
        {
            Array.Copy(other.board, board, board.Length);
            pieces.Copy(other.pieces);
            Array.Copy(other.units, units, units.Length);
            all = other.all;
            sideToMove = other.sideToMove;
            castling = other.castling;
            enPassant = other.enPassant;
            enPassantValidated = other.enPassantValidated;
            halfMoveClock = other.halfMoveClock;
            fullMoveCounter = other.fullMoveCounter;
            hash = other.hash;
            gameStack = new(other.gameStack);
            Array.Copy(other.material, material, material.Length);
            Array.Copy(other.castled, castled, castled.Length);
            Array.Copy(other.opMaterial, opMaterial, opMaterial.Length);
            Array.Copy(other.egMaterial, egMaterial, egMaterial.Length);
            Array.Copy(other.opPcSquare, opPcSquare, opPcSquare.Length);
            Array.Copy(other.egPcSquare, egPcSquare, egPcSquare.Length);
            pawnHash = other.pawnHash;
        }

        #endregion

        public ReadOnlySpan<Square> PieceBoard => board;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Pieces(Color color, Piece piece) => pieces[(int)color, (int)piece];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Units(Color color) => units[(int)color];

        public ulong All => all;

        public Color SideToMove => sideToMove;
        public Color OpponentColor => (Color)((int)sideToMove ^ 1);
        public CastlingRights Castling => castling;
        public int EnPassant => enPassant;
        public int HalfMoveClock => halfMoveClock;
        public int FullMoveCounter => fullMoveCounter;
        public ulong Hash => hash;
        public short Material(Color color) => material[(int)color];
        public short TotalMaterial => (short)(material[0] + material[1]);
        public bool[] Castled => castled;
        public ulong PawnHash => pawnHash;
        public short[] OpeningMaterial => opMaterial;
        public short[] EndGameMaterial => egMaterial;
        public short[] OpeningPieceSquare => opPcSquare;
        public short[] EndGamePieceSquare => egPcSquare;

        public ulong LastMove
        {
            get
            {
                if (gameStack.TryPeek(out BoardState item))
                {
                    return item.Move;
                }

                return 0ul;
            }
        }

        public void Clear()
        {
            Array.Fill(board, Square.Empty);
            pieces.Clear();
            Array.Clear(units);
            all = 0;
            sideToMove = Color.None;
            castling = CastlingRights.None;
            enPassant = Index.None;
            enPassantValidated = Index.None;
            halfMoveClock = 0;
            fullMoveCounter = 0;
            hash = 0;
            gameStack.Clear();
            Array.Clear(material);
            Array.Clear(castled);
            Array.Clear(opMaterial);
            Array.Clear(egMaterial);
            Array.Clear(opPcSquare);
            Array.Clear(egPcSquare);
            pawnHash = 0;
        }

        public bool IsLegalMove(ulong move)
        {
            bool legal = false;
            MoveList moveList = new();
            GenerateMoves(moveList);
            for (int n = 0; n < moveList.Count; n++)
            {
                if (Move.Compare(move, moveList[n]) == 0)
                {
                    if (MakeMove(move))
                    {
                        UnmakeMove();
                        legal = true;
                    }

                    break;
                }
            }

            return legal;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            var result = GetSquares();
            for (int rank = 7; rank >= 0; --rank)
            {
                for (int file = 0; file <= 7; ++file)
                {
                    int square = Index.ToIndex(file, rank);
                    var sq = result[square];
                    string pc = sq.Piece != Piece.None ? Conversion.PieceToString(sq.Piece) : ".";
                    if (sq.Color == Color.White)
                    {
                        pc = pc.ToUpper();
                    }

                    sb.Append($" {pc}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public bool GameDrawnByRepetition()
        {
            int matchCount = 1;
            ReadOnlySpan<BoardState> stackSpan = gameStack.AsSpan();
            for (int n = stackSpan.Length - 1; n >= stackSpan.Length - halfMoveClock && matchCount < 3; n--)
            {
                if (hash == stackSpan[n].Hash)
                {
                    matchCount++;
                }
            }

            return matchCount == 3;
        }

        public ulong[] GameMoves()
        {
            return gameStack.Select(bs => bs.Move).Reverse().ToArray();
        }

        public bool InsufficientMaterialForMate()
        {
            return Material(Color.White) <= 300 && Material(Color.Black) <= 300 &&
                   Pieces(Color.White, Piece.Pawn) == 0ul &&
                   Pieces(Color.Black, Piece.Pawn) == 0ul;
        }

        public bool IsEnPassantValid(Color color)
        {
            if (enPassant == Index.None)
            {
                return false;
            }

            return (PawnDefends(color, enPassant) & Pieces(color, Piece.Pawn)) != 0;
        }

        public bool HasMinorMajorPieces(Color color, int minMaterial)
        {
            int mat = 0;
            for (Piece piece = Piece.Knight; piece <= Piece.Queen; piece++)
            {
                mat += BitOps.PopCount(Pieces(color, piece)) * Evaluation.CanonicalPieceValues[(int)piece];
                if (mat >= minMaterial)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPromotionThreat(ulong move)
        {
            if (move != Move.NullMove)
            {
                int to = Move.GetTo(move);
                Square sq = board[to];
                int normalizedRank = Index.GetRank(Index.NormalizedIndex[(int)sq.Color][to]);
                return sq.Piece == Piece.Pawn && normalizedRank == 6;
            }

            return false;
        }

        public ReadOnlySpan<Square> GetSquares()
        {
            return new ReadOnlySpan<Square>(board);
        }

        public Board Clone()
        {
            return new Board(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #region Make / Unmake Moves

        public bool MakeMove(ulong move)
        {
            PushBoardState(move);

            if (enPassantValidated != Index.None)
            {
                hash = ZobristHash.HashEnPassant(hash, enPassantValidated);
            }

            enPassant = Index.None;
            enPassantValidated = Index.None;
            hash = ZobristHash.HashCastling(hash, castling);

            int from = Move.GetFrom(move);
            int to = Move.GetTo(move);
            MoveType type = Move.GetMoveType(move);
            Square capture = new(OpponentColor, Move.GetCapture(move));
            Square promote = new(sideToMove, Move.GetPromote(move));

            switch (type)
            {
                case MoveType.Normal:
                    UpdatePiece((Color)board[from], (Piece)board[from], from, to);
                    castling &= (CastlingRights)(castleMask[from] & castleMask[to]);
                    halfMoveClock++;
                    break;

                case MoveType.Capture:
                    RemovePiece((Color)capture, (Piece)capture, to);
                    UpdatePiece((Color)board[from], (Piece)board[from], from, to);
                    castling &= (CastlingRights)(castleMask[from] & castleMask[to]);
                    halfMoveClock = 0;
                    break;

                case MoveType.Castle:
                    CastlingRookMove rookMove = LookupRookMove(to);
                    if (IsSquareAttackedByColor(from, OpponentColor) ||
                        IsSquareAttackedByColor(rookMove.KingMoveThrough, OpponentColor))
                    {
                        BoardState state = gameStack.Pop();
                        state.Restore(this);
                        return false;
                    }

                    UpdatePiece(sideToMove, Piece.King, from, to);
                    UpdatePiece(sideToMove, Piece.Rook, rookMove.RookFrom, rookMove.RookTo);
                    castling &= (CastlingRights)(castleMask[from] & castleMask[to]);
                    halfMoveClock++;
                    castled[(int)sideToMove] = true;
                    break;

                case MoveType.EnPassant:
                    int captureOffset = epOffset[(int)sideToMove];
                    RemovePiece((Color)capture, (Piece)capture, to + captureOffset);
                    UpdatePiece(sideToMove, Piece.Pawn, from, to);
                    halfMoveClock = 0;
                    break;

                case MoveType.PawnMove:
                    UpdatePiece(sideToMove, Piece.Pawn, from, to);
                    halfMoveClock = 0;
                    break;

                case MoveType.DblPawnMove:
                    UpdatePiece(sideToMove, Piece.Pawn, from, to);
                    enPassant = to + epOffset[(int)sideToMove];
                    if (IsEnPassantValid(OpponentColor))
                    {
                        enPassantValidated = enPassant;
                        hash = ZobristHash.HashEnPassant(hash, enPassantValidated);
                    }

                    halfMoveClock = 0;
                    break;

                case MoveType.Promote:
                    RemovePiece((Color)board[from], (Piece)board[from], from);
                    AddPiece((Color)promote, (Piece)promote, to);
                    halfMoveClock = 0;
                    break;

                case MoveType.PromoteCapture:
                    RemovePiece((Color)capture, (Piece)capture, to);
                    RemovePiece((Color)board[from], (Piece)board[from], from);
                    AddPiece((Color)promote, (Piece)promote, to);
                    castling &= (CastlingRights)(castleMask[from] & castleMask[to]);
                    halfMoveClock = 0;
                    break;

                case MoveType.Null:
                    // do nothing
                    break;

                default:
                    string strMove = $"{Index.ToString(from)}{Index.ToString(to)}";
                    Util.Fail($"Invalid move encountered in MakeMove: {strMove}.");
                    Util.TraceError($"Invalid move encountered in MakeMove: {strMove}.");
                    break;
            }

            if (sideToMove == Color.Black)
            {
                ++fullMoveCounter;
            }

            hash = ZobristHash.HashCastling(hash, castling);
            hash = ZobristHash.HashActiveColor(hash, sideToMove);
            sideToMove = OpponentColor;
            hash = ZobristHash.HashActiveColor(hash, sideToMove);

            if (IsChecked(sideToMove))
            {
                UnmakeMove();
                return false;
            }

            return true;
        }

        public void UnmakeMove()
        {
            sideToMove = OpponentColor;
            BoardState state = gameStack.Pop();

            int from = Move.GetFrom(state.Move);
            int to = Move.GetTo(state.Move);
            MoveType type = Move.GetMoveType(state.Move);
            Square capture = Square.Create(OpponentColor, Move.GetCapture(state.Move));
            Square promote = Square.Create(sideToMove, Move.GetPromote(state.Move));

            switch (type)
            {
                case MoveType.Normal:
                case MoveType.PawnMove:
                case MoveType.DblPawnMove:
                    UpdatePiece((Color)board[to], (Piece)board[to], to, from);
                    break;

                case MoveType.Capture:
                    UpdatePiece((Color)board[to], (Piece)board[to], to, from);
                    AddPiece((Color)capture, (Piece)capture, to);
                    break;

                case MoveType.Castle:
                    CastlingRookMove rookMove = LookupRookMove(to);
                    UpdatePiece(sideToMove, Piece.Rook, rookMove.RookTo, rookMove.RookFrom);
                    UpdatePiece(sideToMove, Piece.King, to, from);
                    castled[(int)sideToMove] = false;
                    break;

                case MoveType.EnPassant:
                    int captureOffset = epOffset[(int)sideToMove];
                    UpdatePiece(sideToMove, Piece.Pawn, to, from);
                    AddPiece((Color)capture, (Piece)capture, to + captureOffset);
                    break;

                case MoveType.Promote:
                    RemovePiece((Color)promote, (Piece)promote, to);
                    AddPiece(sideToMove, Piece.Pawn, from);
                    break;

                case MoveType.PromoteCapture:
                    RemovePiece((Color)promote, (Piece)promote, to);
                    AddPiece(sideToMove, Piece.Pawn, from);
                    AddPiece((Color)capture, (Piece)capture, to);
                    break;

                case MoveType.Null:
                    // do nothing
                    break;

                default:
                    string strMove = $"{Index.ToString(from)}{Index.ToString(to)}";
                    Util.Fail($"Invalid move encountered in UnmakeMove: {strMove}.");
                    Util.TraceError($"Invalid move encountered in UnmakeMove: {strMove}.");
                    break;
            }

            state.Restore(this);
        }

        private static CastlingRookMove LookupRookMove(int kingTo)
        {
            switch (kingTo)
            {
                case Index.C1:
                    return CastlingRookMoves[0];

                case Index.G1:
                    return CastlingRookMoves[1];

                case Index.C8:
                    return CastlingRookMoves[2];

                case Index.G8:
                    return CastlingRookMoves[3];

                default:
                    Util.Fail($"Invalid castling move with king moving to {kingTo}.");
                    return new CastlingRookMove();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void PushBoardState(ulong move)
        {
            if (gameStack.Count < Constants.MAX_GAME_LENGTH)
            {
                BoardState state;
                state.Castling = castling;
                state.EnPassant = enPassant;
                state.EnPassantValidated = enPassantValidated;
                state.FullMoveCounter = fullMoveCounter;
                state.HalfMoveClock = halfMoveClock;
                state.Hash = hash;
                state.SideToMove = sideToMove;
                state.Move = move;
                gameStack.Push(ref state);
            }
            else
            {
                throw new IndexOutOfRangeException(
                    @$"Maximum game lenth exceeded - Hash {hash} BestMove {Move.ToString(move)}");
            }
        }

        #endregion

        #region Attacks & Checks

        public bool HasLegalMoves(MoveList moveList)
        {
            GenerateMoves(moveList);
            for (int n = 0; n < moveList.Count; n++)
            {
                if (MakeMove(moveList[n]))
                {
                    UnmakeMove();
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsChecked()
        {
            return IsChecked(OpponentColor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsChecked(Color byColor)
        {
            int kingIndex = BitOps.TzCount(Pieces((Color)((int)byColor ^ 1), Piece.King));
            return IsSquareAttackedByColor(kingIndex, byColor);
        }

        public bool IsSquareAttackedByColor(int index, Color color)
        {
            if ((PawnDefends(color, index) & Pieces(color, Piece.Pawn)) != 0)
            {
                return true;
            }

            if ((PieceMoves(Piece.Knight, index) & Pieces(color, Piece.Knight)) != 0)
            {
                return true;
            }

            if ((PieceMoves(Piece.King, index) & Pieces(color, Piece.King)) != 0)
            {
                return true;
            }

            ulong bb = PieceMoves(Piece.Rook, index) &
                       (Pieces(color, Piece.Rook) | Pieces(color, Piece.Queen));

            bb |= PieceMoves(Piece.Bishop, index) &
                  (Pieces(color, Piece.Bishop) | Pieces(color, Piece.Queen));

            for (; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int index2 = BitOps.TzCount(bb);
                if ((between[index2, index] & All) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Move Generation

        public IEnumerable<ulong> Moves(int ply, KillerMoves killerMoves, History history, MoveList moveList)
        {
            if (TtTran.TryGetBestMove(hash, out ulong bestMove))
            {
                int from = Move.GetFrom(bestMove);
                int to = Move.GetTo(bestMove);
                if (board[from].Color == sideToMove && IsValidMove(board[from].Piece, from, to, out bestMove))
                {
                    yield return bestMove;
                }
            }

            moveList.Clear();
            GenerateCaptures(moveList);
            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                if (Move.Compare(moveList[n], bestMove) == 0)
                {
                    continue;
                }
                yield return moveList[n];
            }

            moveList.Clear();
            GeneratePromotions(moveList);
            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                if (Move.Compare(moveList[n], bestMove) != 0)
                {
                    yield return moveList[n];
                }
            }

            foreach (ulong move in killerMoves.GetKillers(ply))
            {
                int from = Move.GetFrom(move);
                int to = Move.GetTo(move);
                if (board[from].Color == sideToMove && IsValidMove(board[from].Piece, from, to, out ulong validMove))
                {
                    yield return validMove;
                }
            }

            moveList.Clear();
            GenerateQuietMoves(moveList, history);
            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                if (Move.Compare(moveList[n], bestMove) == 0 || killerMoves.Exists(ply, moveList[n]))
                {
                    continue;
                }
                yield return moveList[n];
            }
        }

        public IEnumerable<ulong> CaptureMoves(MoveList moveList)
        {
            moveList.Clear();
            GenerateCaptures(moveList);
            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                yield return moveList[n];
            }

            moveList.Clear();
            GeneratePromotions(moveList);
            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                yield return moveList[n];
            }
        }

        public bool IsValidMove(Piece piece, int from, int to, out ulong validMove)
        {
            validMove = 0;
            ulong bbMoves = BitOps.AndNot(GetPieceMoves(piece, from), All);
            for (; bbMoves != 0; bbMoves = BitOps.ResetLsb(bbMoves))
            {
                int validTo = BitOps.TzCount(bbMoves);
                if (validTo == to)
                {
                    validMove = Move.PackMove(from, to);
                    return true;
                }
            }

            return false;
        }

        public bool NoLegalMoves()
        {
            MoveList list = new();
            GenerateLegalMoves(list);
            return list.Count == 0;
        }
        public bool OneLegalMove(out ulong legalMove)
        {
            int legalCount = 0;
            legalMove = 0;
            MoveList moveList = new();
            GenerateMoves(moveList);
            for (int n = 0; n < moveList.Count && legalCount <= 1; n++)
            {
                if (MakeMove(moveList[n]))
                {
                    if (legalCount == 0)
                    {
                        legalMove = moveList[n];
                    }
                    UnmakeMove();
                    legalCount++;
                }
            }

            return legalCount == 1;
        }

        public short GetPieceMobility(Color color)
        {
            short mobility = 0;
            Color other = (Color)((int)color ^ 1);
            ulong pawnDefended = 0ul;

            for (ulong pawns = Pieces(other, Piece.Pawn); pawns != 0ul; pawns = BitOps.ResetLsb(pawns))
            {
                int square = BitOps.TzCount(pawns);
                pawnDefended |= PawnCaptures[(int)other][square];
            }

            ulong excluded = pawnDefended | Units(color);
            for (Piece piece = Piece.Knight; piece <= Piece.King; piece++)
            {
                for (ulong pcLoc = Pieces(color, piece); pcLoc != 0; pcLoc = BitOps.ResetLsb(pcLoc))
                {
                    int from = BitOps.TzCount(pcLoc);
                    mobility += (short)BitOps.PopCount(BitOps.AndNot(GetPieceMoves(piece, from), excluded));
                }
            }

            return mobility;
        }

        public void GenerateMoves(MoveList list, IHistory? history = null)
        {
            IHistory hist = history ?? fakeHistory;
            GenerateEnPassant(list);
            GenerateCastling(list, hist);
            GeneratePawnMoves(list, hist);

            for (Piece piece = Piece.Knight; piece <= Piece.King; ++piece)
            {
                for (ulong bb1 = Pieces(sideToMove, piece); bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int from = BitOps.TzCount(bb1);
                    ulong bb2 = GetPieceMoves(piece, from);

                    for (ulong bb3 = bb2 & Units(OpponentColor); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        list.Add(from, to, MoveType.Capture, (Piece)board[to], score: CaptureScore((Piece)board[to], piece));
                    }
                    for (ulong bb3 = BitOps.AndNot(bb2, All); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        list.Add(from, to, score: hist[from, to]);
                    }
                }
            }
        }

        public void GenerateLegalMoves(MoveList list)
        {
            list.Clear();
            GenerateMoves(list);

            List<ulong> moves = new();
            for (int n = 0; n < list.Count; n++)
            {
                if (MakeMove(list[n]))
                {
                    UnmakeMove();
                    moves.Add(moves[n]);
                }
            }

            list.Clear();
            list.Add(moves);
        }
        public void GenerateQuietMoves(MoveList list, IHistory history)
        {
            GenerateCastling(list, history);
            GenerateQuietPawnMoves(list, history);

            for (Piece piece = Piece.Knight; piece <= Piece.King; ++piece)
            {
                for (ulong bb1 = Pieces(sideToMove, piece); bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int from = BitOps.TzCount(bb1);
                    ulong bb2 = GetPieceMoves(piece, from);

                    for (ulong bb3 = BitOps.AndNot(bb2, All); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        list.Add(from, to, score: history[from, to]);
                    }
                }
            }
        }

        public void GenerateCaptures(MoveList list)
        {
            GenerateEnPassant(list);
            GeneratePawnCaptures(list);

            for (Piece piece = Piece.Knight; piece <= Piece.King; ++piece)
            {
                for (ulong bb1 = Pieces(sideToMove, piece); bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int from = BitOps.TzCount(bb1);
                    ulong bb2 = GetPieceMoves(piece, from);

                    for (ulong bb3 = bb2 & Units(OpponentColor); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        list.Add(from, to, MoveType.Capture, (Piece)board[to], score: CaptureScore((Piece)board[to], piece));
                    }
                }
            }
        }

        public void GeneratePromotions(MoveList list)
        {
            // only concerned about pawns on the 7th rank (or 2nd for black)
            ulong pawnsToPromote = Pieces(sideToMove, Piece.Pawn) &
                                   MaskRanks[Index.NormalizedIndex[(int)sideToMove][Index.A7]];

            ulong bb = sideToMove == Color.White
                ? BitOps.AndNot(pawnsToPromote, All >> 8)
                : BitOps.AndNot(pawnsToPromote, All << 8);

            for (; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int from = BitOps.TzCount(bb);
                int to = pawnPlus[(int)sideToMove, from];
                for (Piece p = Piece.Knight; p <= Piece.Queen; ++p)
                {
                    int score = Constants.PROMOTE_SCORE + Evaluation.CanonicalPieceValues[(int)p];
                    list.Add(from, to, MoveType.Promote, promote: p, score: score);
                }
            }
        }

        public void GenerateEnPassant(MoveList list)
        {
            if (enPassantValidated != Index.None)
            {
                ulong bb = PawnDefends(sideToMove, enPassantValidated) & Pieces(sideToMove, Piece.Pawn);
                for (; bb != 0; bb = BitOps.ResetLsb(bb))
                {
                    int from = BitOps.TzCount(bb);
                    int captIndex = enPassantValidated + epOffset[(int)sideToMove];
                    list.Add(from, enPassantValidated, MoveType.EnPassant, capture: (Piece)board[captIndex], 
                        score: CaptureScore((Piece)board[captIndex], Piece.Pawn));
                }
            }
        }

        public void GenerateCastling(MoveList list, IHistory hist)
        {
            if (sideToMove == Color.White)
            {
                if ((castling & CastlingRights.WhiteKingSide) != 0 && (between[Index.H1, Index.E1] & All) == 0)
                {
                    list.Add(Index.E1, Index.G1, MoveType.Castle, score: hist[Index.E1, Index.G1]);
                }

                if ((castling & CastlingRights.WhiteQueenSide) != 0 && (between[Index.A1, Index.E1] & All) == 0)
                {
                    list.Add(Index.E1, Index.C1, MoveType.Castle, score: hist[Index.E1, Index.C1]);
                }
            }
            else
            {
                if ((castling & CastlingRights.BlackKingSide) != 0 && (between[Index.E8, Index.H8] & All) == 0)
                {
                    list.Add(Index.E8, Index.G8, MoveType.Castle, score: hist[Index.E8, Index.G8]);
                }

                if ((castling & CastlingRights.BlackQueenSide) != 0 && (between[Index.E8, Index.A8] & All) == 0)
                {
                    list.Add(Index.E8, Index.C8, MoveType.Castle, score: hist[Index.E8, Index.C8]);
                }
            }
        }

        public void GenerateQuietPawnMoves(MoveList list, IHistory hist)
        {
            ulong bb1, bb2;
            int from, to;

            ulong pawns = Pieces(sideToMove, Piece.Pawn);

            if (sideToMove == Color.White)
            {
                bb1 = BitOps.AndNot(BitOps.AndNot(pawns, MaskRanks[Index.A7]), All >> 8);
                bb2 = BitOps.AndNot(bb1 & MaskRanks[Index.A2], All >> 16);
            }
            else
            {
                bb1 = BitOps.AndNot(BitOps.AndNot(pawns, MaskRanks[Index.A2]), All << 8);
                bb2 = BitOps.AndNot(bb1 & MaskRanks[Index.A7], All << 16);
            }

            for (; bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
            {
                from = BitOps.TzCount(bb1);
                to = pawnPlus[(int)sideToMove, from];
                AddPawnMove(list, from, to, MoveType.PawnMove, score: hist[from, to]);
            }

            for (; bb2 != 0; bb2 = BitOps.ResetLsb(bb2))
            {
                from = BitOps.TzCount(bb2);
                to = pawnDouble[(int)sideToMove, from];
                list.Add(from, to, MoveType.DblPawnMove, score: hist[from, to]);
            }
        }

        public void GeneratePawnMoves(MoveList list, IHistory hist)
        {
            ulong bb1, bb2, bb3, bb4;
            int from, to;

            ulong pawns = Pieces(sideToMove, Piece.Pawn);

            if (sideToMove == Color.White)
            {
                bb1 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFiles[7]) >> 7);
                bb2 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFiles[0]) >> 9);
                bb3 = BitOps.AndNot(pawns, All >> 8);
                bb4 = BitOps.AndNot(bb3 & MaskRanks[Index.A2], All >> 16);
            }
            else
            {
                bb1 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFiles[7]) << 9);
                bb2 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFiles[0]) << 7);
                bb3 = BitOps.AndNot(pawns, All << 8);
                bb4 = BitOps.AndNot(bb3 & MaskRanks[Index.A7], All << 16);
            }

            for (; bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
            {
                from = BitOps.TzCount(bb1);
                to = PawnLeft(sideToMove, from);
                AddPawnMove(list, from, to, MoveType.Capture, (Piece)board[to], score: CaptureScore(from, to));
            }

            for (; bb2 != 0; bb2 = BitOps.ResetLsb(bb2))
            {
                from = BitOps.TzCount(bb2);
                to = PawnRight(sideToMove, from);
                AddPawnMove(list, from, to, MoveType.Capture, (Piece)board[to], score: CaptureScore(from, to));
            }

            for (; bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
            {
                from = BitOps.TzCount(bb3);
                to = pawnPlus[(int)sideToMove, from];
                AddPawnMove(list, from, to, MoveType.PawnMove, score: hist[from, to]);
            }

            for (; bb4 != 0; bb4 = BitOps.ResetLsb(bb4))
            {
                from = BitOps.TzCount(bb4);
                to = pawnDouble[(int)sideToMove, from];
                list.Add(from, to, MoveType.DblPawnMove, score: hist[from, to]);
            }
        }

        public void GeneratePawnCaptures(MoveList list)
        {
            ulong bb1, bb2;
            int from, to;

            ulong pawns = Pieces(sideToMove, Piece.Pawn);

            if (sideToMove == Color.White)
            {
                bb1 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFiles[7]) >> 7);
                bb2 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFiles[0]) >> 9);
            }
            else
            {
                bb1 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFiles[7]) << 9);
                bb2 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFiles[0]) << 7);
            }

            for (; bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
            {
                from = BitOps.TzCount(bb1);
                to = PawnLeft(sideToMove, from);
                AddPawnMove(list, from, to, MoveType.Capture, (Piece)board[to], score: CaptureScore(from, to));
            }

            for (; bb2 != 0; bb2 = BitOps.ResetLsb(bb2))
            {
                from = BitOps.TzCount(bb2);
                to = PawnRight(sideToMove, from);
                AddPawnMove(list, from, to, MoveType.Capture, (Piece)board[to], score: CaptureScore(from, to));
            }
        }

        public static void AddPawnMove(MoveList list, int from, int to, MoveType flags = MoveType.PawnMove, Piece capture = Piece.None, int score = 0)
        {
            int rank = Index.GetRank(to);
            if (rank == Coord.MinValue || rank == Coord.MaxValue)
            {
                flags = flags == MoveType.Capture ? MoveType.PromoteCapture : MoveType.Promote;
                for (Piece p = Piece.Knight; p <= Piece.Queen; ++p)
                {
                    score = Constants.PROMOTE_SCORE + Evaluation.CanonicalPieceValues[(int)p];
                    list.Add(from, to, flags, capture, p, score);
                }
            }
            else
            {
                list.Add(from, to, flags, capture: capture, score: score);
            }
        }

        public ulong GetPieceMoves(Piece piece, int from)
        {
            switch (piece)
            {
                case Piece.Knight:
                    return PieceMoves(Piece.Knight, from);

                case Piece.Bishop:
                    return GetBishopAttacksFancy(from, All);

                case Piece.Rook:
                    return GetRookAttacksFancy(from, All);

                case Piece.Queen:
                    return GetBishopAttacksFancy(from, All) | GetRookAttacksFancy(from, All);

                case Piece.King:
                    return PieceMoves(Piece.King, from);

                default:
                    return 0ul;
            }
        }

        public static ulong GetBishopAttacks(int from, ulong blockers)
        {
            Ray ray = vectors[from];
            ulong bb = BitOps.AndNot(ray.NorthEast, vectors[BitOps.TzCount(ray.NorthEast & blockers)].NorthEast) |
                       BitOps.AndNot(ray.NorthWest, vectors[BitOps.TzCount(ray.NorthWest & blockers)].NorthWest) |
                       BitOps.AndNot(ray.SouthEast, revVectors[BitOps.LzCount(ray.SouthEast & blockers)].SouthEast) |
                       BitOps.AndNot(ray.SouthWest, revVectors[BitOps.LzCount(ray.SouthWest & blockers)].SouthWest);
            return bb;
        }

        public static ulong GetRookAttacks(int from, ulong blockers)
        {
            Ray ray = vectors[from];
            ulong bb = BitOps.AndNot(ray.North, vectors[BitOps.TzCount(ray.North & blockers)].North) |
                       BitOps.AndNot(ray.East, vectors[BitOps.TzCount(ray.East & blockers)].East) |
                       BitOps.AndNot(ray.South, revVectors[BitOps.LzCount(ray.South & blockers)].South) |
                       BitOps.AndNot(ray.West, revVectors[BitOps.LzCount(ray.West & blockers)].West);

            return bb;
        }

        #endregion

        #region FEN Processing

        public string ToFenString()
        {
            Fen fen = new(this);
            return fen.ToString();
        }

        public bool LoadFenPosition(string fenPosition)
        {
            if (Fen.TryParse(fenPosition, out Fen fen))
            {
                Clear();
                foreach (var pc in fen.Squares)
                {
                    AddPiece(pc.Color, pc.Piece, pc.Square);
                }

                sideToMove = fen.SideToMove;
                hash = ZobristHash.HashActiveColor(hash, sideToMove);

                castling = fen.Castling;
                hash = ZobristHash.HashCastling(hash, castling);

                enPassantValidated = Index.None;
                enPassant = fen.EnPassant;
                if (IsEnPassantValid(sideToMove))
                {
                    enPassantValidated = enPassant;
                    hash = ZobristHash.HashEnPassant(hash, enPassantValidated);
                }

                halfMoveClock = fen.HalfMoveClock;
                fullMoveCounter = fen.FullMoveCounter;
                return true;
            }

            return false;
        }

        #endregion

        #region Incremental Board Updates

        public void AddPiece(Color color, Piece piece, int square)
        {
            if (piece == Piece.Pawn)
            {
                pawnHash = ZobristHash.HashPiece(pawnHash, color, piece, square);
            }
            hash = ZobristHash.HashPiece(hash, color, piece, square);
            board[square] = Square.Create(color, piece);
            pieces[(int)color, (int)piece] = BitOps.SetBit(Pieces(color, piece), square);
            units[(int)color] = BitOps.SetBit(units[(int)color], square);
            all = BitOps.SetBit(all, square);
            material[(int)color] += Evaluation.CanonicalPieceValues[(int)piece];
            opMaterial[(int)color] += Evaluation.OpeningPieceValues[(int)piece];
            egMaterial[(int)color] += Evaluation.EndGamePieceValues[(int)piece];
            opPcSquare[(int)color] +=
                Evaluation.OpeningPieceSquareTable[(int)piece, Index.NormalizedIndex[(int)color][square]];
            egPcSquare[(int)color] +=
                Evaluation.EndGamePieceSquareTable[(int)piece, Index.NormalizedIndex[(int)color][square]];
        }

        public void RemovePiece(Color color, Piece piece, int square)
        {
            if (piece == Piece.Pawn)
            {
                pawnHash = ZobristHash.HashPiece(pawnHash, color, piece, square);
            }
            hash = ZobristHash.HashPiece(hash, color, piece, square);
            board[square] = Square.Empty;
            pieces[(int)color, (int)piece] = BitOps.ResetBit(Pieces(color, piece), square);
            units[(int)color] = BitOps.ResetBit(Units(color), square);
            all = BitOps.ResetBit(all, square);
            material[(int)color] -= Evaluation.CanonicalPieceValues[(int)piece];
            opMaterial[(int)color] -= Evaluation.OpeningPieceValues[(int)piece];
            egMaterial[(int)color] -= Evaluation.EndGamePieceValues[(int)piece];
            opPcSquare[(int)color] -=
                Evaluation.OpeningPieceSquareTable[(int)piece, Index.NormalizedIndex[(int)color][square]];
            egPcSquare[(int)color] -=
                Evaluation.EndGamePieceSquareTable[(int)piece, Index.NormalizedIndex[(int)color][square]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePiece(Color color, Piece piece, int fromSquare, int toSquare)
        {
            RemovePiece(color, piece, fromSquare);
            AddPiece(color, piece, toSquare);
        }

        #endregion

        #region Static Data Used by BestMove Generation

        private class FakeHistory : IHistory
        {
            public int this[int from, int to] => 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CaptureScore(int from, int to)
        {
            return captureScores[(int)board[to].Piece][(int)board[from].Piece];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CaptureScore(Piece captured, Piece attacker)
        {
            return captureScores[(int)captured][(int)attacker];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PawnDefends(Color color, int square)
        {
            return pawnDefends[(int)color][square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong PieceMoves(Piece piece, int square)
        {
            return pieceMoves[(int)piece][square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PawnLeft(Color color, int square)
        {
            return pawnLeft[(int)color][square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PawnRight(Color color, int square)
        {
            return pawnRight[(int)color][square];
        }

        private static readonly FakeHistory fakeHistory = new();

        private static readonly int[] epOffset = { -8, 8 };

        private static readonly int[] castleMask =
        {
            13, 15, 15, 15, 12, 15, 15, 14,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15,
             7, 15, 15, 15,  3, 15, 15, 11
        };

        public readonly struct CastlingRookMove
        {
            public readonly int KingFrom;
            public readonly int KingTo;
            public readonly int KingMoveThrough;
            public readonly int RookFrom;
            public readonly int RookTo;
            public readonly CastlingRights CastlingMask;

            public CastlingRookMove(int kingFrom, int kingTo, int kingMoveThrough, int rookFrom, int rookTo, CastlingRights mask)
            {
                KingFrom = kingFrom;
                KingTo = kingTo;
                KingMoveThrough = kingMoveThrough;
                RookFrom = rookFrom;
                RookTo = rookTo;
                CastlingMask = mask;
            }
        }

        public static readonly CastlingRookMove[] CastlingRookMoves =
        {
            new(Index.E1, Index.C1, Index.D1, Index.A1, Index.D1, CastlingRights.WhiteQueenSide),
            new(Index.E1, Index.G1, Index.F1, Index.H1, Index.F1, CastlingRights.WhiteKingSide),
            new(Index.E8, Index.C8, Index.D8, Index.A8, Index.D8, CastlingRights.BlackQueenSide),
            new(Index.E8, Index.G8, Index.F8, Index.H8, Index.F8, CastlingRights.BlackKingSide)
        };

        private static readonly int[][] captureScores = 
        {
            #region captureScores data
            new [] {  10010,  10003,  10003,  10002,  10001,  10000 },
            new [] {  10030,  10010,  10010,  10006,  10003,  10000 },
            new [] {  10030,  10010,  10010,  10006,  10003,  10000 },
            new [] {  10050,  10016,  10016,  10010,  10005,  10000 },
            new [] {  10090,  10030,  10030,  10018,  10010,  10000 },
            new [] {  11500,  10500,  10500,  10300,  10166,  10010 }
            #endregion
        };

        private static readonly ulong[][] pawnDefends = 
        {
            #region pawnDefends data
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000002ul, 0x0000000000000005ul, 0x000000000000000Aul, 0x0000000000000014ul,
                0x0000000000000028ul, 0x0000000000000050ul, 0x00000000000000A0ul, 0x0000000000000040ul,
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
                0x0028000000000000ul, 0x0050000000000000ul, 0x00A0000000000000ul, 0x0040000000000000ul
            },
            new[]
            {
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
                0x0200000000000000ul, 0x0500000000000000ul, 0x0A00000000000000ul, 0x1400000000000000ul,
                0x2800000000000000ul, 0x5000000000000000ul, 0xA000000000000000ul, 0x4000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            }
            #endregion
        };

        public static readonly ulong[][] PawnCaptures =
        {
            #region pawnCaptures data
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
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
                0x0200000000000000ul, 0x0500000000000000ul, 0x0A00000000000000ul, 0x1400000000000000ul,
                0x2800000000000000ul, 0x5000000000000000ul, 0xA000000000000000ul, 0x4000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            },
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000002ul, 0x0000000000000005ul, 0x000000000000000Aul, 0x0000000000000014ul,
                0x0000000000000028ul, 0x0000000000000050ul, 0x00000000000000A0ul, 0x0000000000000040ul,
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
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            }

            #endregion pawnCaptures data
        };

        private static readonly ulong[][] pieceMoves = 
        {
            #region pieceMoves data
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            new[]
            {
                0x0000000000020400ul, 0x0000000000050800ul, 0x00000000000A1100ul, 0x0000000000142200ul,
                0x0000000000284400ul, 0x0000000000508800ul, 0x0000000000A01000ul, 0x0000000000402000ul,
                0x0000000002040004ul, 0x0000000005080008ul, 0x000000000A110011ul, 0x0000000014220022ul,
                0x0000000028440044ul, 0x0000000050880088ul, 0x00000000A0100010ul, 0x0000000040200020ul,
                0x0000000204000402ul, 0x0000000508000805ul, 0x0000000A1100110Aul, 0x0000001422002214ul,
                0x0000002844004428ul, 0x0000005088008850ul, 0x000000A0100010A0ul, 0x0000004020002040ul,
                0x0000020400040200ul, 0x0000050800080500ul, 0x00000A1100110A00ul, 0x0000142200221400ul,
                0x0000284400442800ul, 0x0000508800885000ul, 0x0000A0100010A000ul, 0x0000402000204000ul,
                0x0002040004020000ul, 0x0005080008050000ul, 0x000A1100110A0000ul, 0x0014220022140000ul,
                0x0028440044280000ul, 0x0050880088500000ul, 0x00A0100010A00000ul, 0x0040200020400000ul,
                0x0204000402000000ul, 0x0508000805000000ul, 0x0A1100110A000000ul, 0x1422002214000000ul,
                0x2844004428000000ul, 0x5088008850000000ul, 0xA0100010A0000000ul, 0x4020002040000000ul,
                0x0400040200000000ul, 0x0800080500000000ul, 0x1100110A00000000ul, 0x2200221400000000ul,
                0x4400442800000000ul, 0x8800885000000000ul, 0x100010A000000000ul, 0x2000204000000000ul,
                0x0004020000000000ul, 0x0008050000000000ul, 0x00110A0000000000ul, 0x0022140000000000ul,
                0x0044280000000000ul, 0x0088500000000000ul, 0x0010A00000000000ul, 0x0020400000000000ul,
            },
            new[]
            {
                0x8040201008040200ul, 0x0080402010080500ul, 0x0000804020110A00ul, 0x0000008041221400ul,
                0x0000000182442800ul, 0x0000010204885000ul, 0x000102040810A000ul, 0x0102040810204000ul,
                0x4020100804020002ul, 0x8040201008050005ul, 0x00804020110A000Aul, 0x0000804122140014ul,
                0x0000018244280028ul, 0x0001020488500050ul, 0x0102040810A000A0ul, 0x0204081020400040ul,
                0x2010080402000204ul, 0x4020100805000508ul, 0x804020110A000A11ul, 0x0080412214001422ul,
                0x0001824428002844ul, 0x0102048850005088ul, 0x02040810A000A010ul, 0x0408102040004020ul,
                0x1008040200020408ul, 0x2010080500050810ul, 0x4020110A000A1120ul, 0x8041221400142241ul,
                0x0182442800284482ul, 0x0204885000508804ul, 0x040810A000A01008ul, 0x0810204000402010ul,
                0x0804020002040810ul, 0x1008050005081020ul, 0x20110A000A112040ul, 0x4122140014224180ul,
                0x8244280028448201ul, 0x0488500050880402ul, 0x0810A000A0100804ul, 0x1020400040201008ul,
                0x0402000204081020ul, 0x0805000508102040ul, 0x110A000A11204080ul, 0x2214001422418000ul,
                0x4428002844820100ul, 0x8850005088040201ul, 0x10A000A010080402ul, 0x2040004020100804ul,
                0x0200020408102040ul, 0x0500050810204080ul, 0x0A000A1120408000ul, 0x1400142241800000ul,
                0x2800284482010000ul, 0x5000508804020100ul, 0xA000A01008040201ul, 0x4000402010080402ul,
                0x0002040810204080ul, 0x0005081020408000ul, 0x000A112040800000ul, 0x0014224180000000ul,
                0x0028448201000000ul, 0x0050880402010000ul, 0x00A0100804020100ul, 0x0040201008040201ul,
            },
            new[]
            {
                0x01010101010101FEul, 0x02020202020202FDul, 0x04040404040404FBul, 0x08080808080808F7ul,
                0x10101010101010EFul, 0x20202020202020DFul, 0x40404040404040BFul, 0x808080808080807Ful,
                0x010101010101FE01ul, 0x020202020202FD02ul, 0x040404040404FB04ul, 0x080808080808F708ul,
                0x101010101010EF10ul, 0x202020202020DF20ul, 0x404040404040BF40ul, 0x8080808080807F80ul,
                0x0101010101FE0101ul, 0x0202020202FD0202ul, 0x0404040404FB0404ul, 0x0808080808F70808ul,
                0x1010101010EF1010ul, 0x2020202020DF2020ul, 0x4040404040BF4040ul, 0x80808080807F8080ul,
                0x01010101FE010101ul, 0x02020202FD020202ul, 0x04040404FB040404ul, 0x08080808F7080808ul,
                0x10101010EF101010ul, 0x20202020DF202020ul, 0x40404040BF404040ul, 0x808080807F808080ul,
                0x010101FE01010101ul, 0x020202FD02020202ul, 0x040404FB04040404ul, 0x080808F708080808ul,
                0x101010EF10101010ul, 0x202020DF20202020ul, 0x404040BF40404040ul, 0x8080807F80808080ul,
                0x0101FE0101010101ul, 0x0202FD0202020202ul, 0x0404FB0404040404ul, 0x0808F70808080808ul,
                0x1010EF1010101010ul, 0x2020DF2020202020ul, 0x4040BF4040404040ul, 0x80807F8080808080ul,
                0x01FE010101010101ul, 0x02FD020202020202ul, 0x04FB040404040404ul, 0x08F7080808080808ul,
                0x10EF101010101010ul, 0x20DF202020202020ul, 0x40BF404040404040ul, 0x807F808080808080ul,
                0xFE01010101010101ul, 0xFD02020202020202ul, 0xFB04040404040404ul, 0xF708080808080808ul,
                0xEF10101010101010ul, 0xDF20202020202020ul, 0xBF40404040404040ul, 0x7F80808080808080ul,
            },
            new[]
            {
                0x81412111090503FEul, 0x02824222120A07FDul, 0x0404844424150EFBul, 0x08080888492A1CF7ul,
                0x10101011925438EFul, 0x2020212224A870DFul, 0x404142444850E0BFul, 0x8182848890A0C07Ful,
                0x412111090503FE03ul, 0x824222120A07FD07ul, 0x04844424150EFB0Eul, 0x080888492A1CF71Cul,
                0x101011925438EF38ul, 0x20212224A870DF70ul, 0x4142444850E0BFE0ul, 0x82848890A0C07FC0ul,
                0x2111090503FE0305ul, 0x4222120A07FD070Aul, 0x844424150EFB0E15ul, 0x0888492A1CF71C2Aul,
                0x1011925438EF3854ul, 0x212224A870DF70A8ul, 0x42444850E0BFE050ul, 0x848890A0C07FC0A0ul,
                0x11090503FE030509ul, 0x22120A07FD070A12ul, 0x4424150EFB0E1524ul, 0x88492A1CF71C2A49ul,
                0x11925438EF385492ul, 0x2224A870DF70A824ul, 0x444850E0BFE05048ul, 0x8890A0C07FC0A090ul,
                0x090503FE03050911ul, 0x120A07FD070A1222ul, 0x24150EFB0E152444ul, 0x492A1CF71C2A4988ul,
                0x925438EF38549211ul, 0x24A870DF70A82422ul, 0x4850E0BFE0504844ul, 0x90A0C07FC0A09088ul,
                0x0503FE0305091121ul, 0x0A07FD070A122242ul, 0x150EFB0E15244484ul, 0x2A1CF71C2A498808ul,
                0x5438EF3854921110ul, 0xA870DF70A8242221ul, 0x50E0BFE050484442ul, 0xA0C07FC0A0908884ul,
                0x03FE030509112141ul, 0x07FD070A12224282ul, 0x0EFB0E1524448404ul, 0x1CF71C2A49880808ul,
                0x38EF385492111010ul, 0x70DF70A824222120ul, 0xE0BFE05048444241ul, 0xC07FC0A090888482ul,
                0xFE03050911214181ul, 0xFD070A1222428202ul, 0xFB0E152444840404ul, 0xF71C2A4988080808ul,
                0xEF38549211101010ul, 0xDF70A82422212020ul, 0xBFE0504844424140ul, 0x7FC0A09088848281ul,
            },
            new[]
            {
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
            }
            #endregion
        };

        private static readonly ulong[,] between = new ulong[,]
        {
            #region between data
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000002ul, 0x0000000000000006ul,
                0x000000000000000Eul, 0x000000000000001Eul, 0x000000000000003Eul, 0x000000000000007Eul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000100ul, 0x0000000000000000ul, 0x0000000000000200ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000040200ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000001010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000008040200ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000101010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000001008040200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000010101010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000201008040200ul, 0x0000000000000000ul,
                0x0001010101010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0040201008040200ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000004ul,
                0x000000000000000Cul, 0x000000000000001Cul, 0x000000000000003Cul, 0x000000000000007Cul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000200ul, 0x0000000000000000ul, 0x0000000000000400ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000080400ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000002020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000010080400ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000202020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000002010080400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020202020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000402010080400ul,
                0x0000000000000000ul, 0x0002020202020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000002ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000008ul, 0x0000000000000018ul, 0x0000000000000038ul, 0x0000000000000078ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000200ul, 0x0000000000000000ul, 0x0000000000000400ul, 0x0000000000000000ul,
                0x0000000000000800ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000100800ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000004040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000020100800ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000404040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000004020100800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040404040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040404040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000006ul, 0x0000000000000004ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000010ul, 0x0000000000000030ul, 0x0000000000000070ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000400ul, 0x0000000000000000ul, 0x0000000000000800ul,
                0x0000000000000000ul, 0x0000000000001000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000020400ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000201000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000040201000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000808080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000080808080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080808080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x000000000000000Eul, 0x000000000000000Cul, 0x0000000000000008ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000020ul, 0x0000000000000060ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000800ul, 0x0000000000000000ul,
                0x0000000000001000ul, 0x0000000000000000ul, 0x0000000000002000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000040800ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000402000ul,
                0x0000000002040800ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000010101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000001010101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000101010101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010101010101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x000000000000001Eul, 0x000000000000001Cul, 0x0000000000000018ul, 0x0000000000000010ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000040ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000001000ul,
                0x0000000000000000ul, 0x0000000000002000ul, 0x0000000000000000ul, 0x0000000000004000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000081000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000004081000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000020202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000204081000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000002020202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000202020202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020202020202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x000000000000003Eul, 0x000000000000003Cul, 0x0000000000000038ul, 0x0000000000000030ul,
                0x0000000000000020ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000002000ul, 0x0000000000000000ul, 0x0000000000004000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000102000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000404000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008102000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000040404000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000408102000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000004040404000ul, 0x0000000000000000ul,
                0x0000020408102000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000404040404000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040404040404000ul, 0x0000000000000000ul,
            },
            {
                0x000000000000007Eul, 0x000000000000007Cul, 0x0000000000000078ul, 0x0000000000000070ul,
                0x0000000000000060ul, 0x0000000000000040ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000004000ul, 0x0000000000000000ul, 0x0000000000008000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000204000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000808000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000010204000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000080808000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000810204000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000008080808000ul,
                0x0000000000000000ul, 0x0000040810204000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000808080808000ul,
                0x0002040810204000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080808080808000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000200ul, 0x0000000000000600ul,
                0x0000000000000E00ul, 0x0000000000001E00ul, 0x0000000000003E00ul, 0x0000000000007E00ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000010000ul, 0x0000000000000000ul, 0x0000000000020000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000001010000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000004020000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000101010000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000804020000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000010101010000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000100804020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0001010101010000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0020100804020000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000400ul,
                0x0000000000000C00ul, 0x0000000000001C00ul, 0x0000000000003C00ul, 0x0000000000007C00ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000020000ul, 0x0000000000000000ul, 0x0000000000040000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000002020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000008040000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000202020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000001008040000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020202020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000201008040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002020202020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0040201008040000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000200ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000800ul, 0x0000000000001800ul, 0x0000000000003800ul, 0x0000000000007800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000020000ul, 0x0000000000000000ul, 0x0000000000040000ul, 0x0000000000000000ul,
                0x0000000000080000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000004040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000010080000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000404040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000002010080000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040404040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000402010080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040404040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000600ul, 0x0000000000000400ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000001000ul, 0x0000000000003000ul, 0x0000000000007000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000040000ul, 0x0000000000000000ul, 0x0000000000080000ul,
                0x0000000000000000ul, 0x0000000000100000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000002040000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000020100000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000808080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000004020100000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000080808080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080808080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000E00ul, 0x0000000000000C00ul, 0x0000000000000800ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000002000ul, 0x0000000000006000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000080000ul, 0x0000000000000000ul,
                0x0000000000100000ul, 0x0000000000000000ul, 0x0000000000200000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000004080000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000010100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000040200000ul,
                0x0000000204080000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000001010100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000101010100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010101010100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000001E00ul, 0x0000000000001C00ul, 0x0000000000001800ul, 0x0000000000001000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000004000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000100000ul,
                0x0000000000000000ul, 0x0000000000200000ul, 0x0000000000000000ul, 0x0000000000400000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008100000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000020200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000408100000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000002020200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000020408100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000202020200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020202020200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000003E00ul, 0x0000000000003C00ul, 0x0000000000003800ul, 0x0000000000003000ul,
                0x0000000000002000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000200000ul, 0x0000000000000000ul, 0x0000000000400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000010200000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000040400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000810200000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000004040400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000040810200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000404040400000ul, 0x0000000000000000ul,
                0x0002040810200000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040404040400000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000007E00ul, 0x0000000000007C00ul, 0x0000000000007800ul, 0x0000000000007000ul,
                0x0000000000006000ul, 0x0000000000004000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000400000ul, 0x0000000000000000ul, 0x0000000000800000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000020400000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000080800000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000001020400000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000008080800000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000081020400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000808080800000ul,
                0x0000000000000000ul, 0x0004081020400000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080808080800000ul,
            },
            {
                0x0000000000000100ul, 0x0000000000000000ul, 0x0000000000000200ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000020000ul, 0x0000000000060000ul,
                0x00000000000E0000ul, 0x00000000001E0000ul, 0x00000000003E0000ul, 0x00000000007E0000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000001000000ul, 0x0000000000000000ul, 0x0000000002000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000101000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000402000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000010101000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000080402000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0001010101000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0010080402000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000200ul, 0x0000000000000000ul, 0x0000000000000400ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000040000ul,
                0x00000000000C0000ul, 0x00000000001C0000ul, 0x00000000003C0000ul, 0x00000000007C0000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000002000000ul, 0x0000000000000000ul, 0x0000000004000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000202000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000804000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020202000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000100804000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002020202000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0020100804000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000200ul, 0x0000000000000000ul, 0x0000000000000400ul, 0x0000000000000000ul,
                0x0000000000000800ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000020000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000080000ul, 0x0000000000180000ul, 0x0000000000380000ul, 0x0000000000780000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000002000000ul, 0x0000000000000000ul, 0x0000000004000000ul, 0x0000000000000000ul,
                0x0000000008000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000404000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000001008000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040404000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000201008000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040404000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0040201008000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000400ul, 0x0000000000000000ul, 0x0000000000000800ul,
                0x0000000000000000ul, 0x0000000000001000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000060000ul, 0x0000000000040000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000100000ul, 0x0000000000300000ul, 0x0000000000700000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000004000000ul, 0x0000000000000000ul, 0x0000000008000000ul,
                0x0000000000000000ul, 0x0000000010000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000204000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000808000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000002010000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000080808000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000402010000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080808000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000800ul, 0x0000000000000000ul,
                0x0000000000001000ul, 0x0000000000000000ul, 0x0000000000002000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x00000000000E0000ul, 0x00000000000C0000ul, 0x0000000000080000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000200000ul, 0x0000000000600000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008000000ul, 0x0000000000000000ul,
                0x0000000010000000ul, 0x0000000000000000ul, 0x0000000020000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000408000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000001010000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000004020000000ul,
                0x0000020408000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000101010000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010101010000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000001000ul,
                0x0000000000000000ul, 0x0000000000002000ul, 0x0000000000000000ul, 0x0000000000004000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x00000000001E0000ul, 0x00000000001C0000ul, 0x0000000000180000ul, 0x0000000000100000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000400000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000010000000ul,
                0x0000000000000000ul, 0x0000000020000000ul, 0x0000000000000000ul, 0x0000000040000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000810000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000002020000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000040810000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000202020000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0002040810000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020202020000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000002000ul, 0x0000000000000000ul, 0x0000000000004000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x00000000003E0000ul, 0x00000000003C0000ul, 0x0000000000380000ul, 0x0000000000300000ul,
                0x0000000000200000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000020000000ul, 0x0000000000000000ul, 0x0000000040000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000001020000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000004040000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000081020000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000404040000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0004081020000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040404040000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000004000ul, 0x0000000000000000ul, 0x0000000000008000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x00000000007E0000ul, 0x00000000007C0000ul, 0x0000000000780000ul, 0x0000000000700000ul,
                0x0000000000600000ul, 0x0000000000400000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000040000000ul, 0x0000000000000000ul, 0x0000000080000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000002040000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000008080000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000102040000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000808080000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0008102040000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080808080000000ul,
            },
            {
                0x0000000000010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000020400ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000010000ul, 0x0000000000000000ul, 0x0000000000020000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000002000000ul, 0x0000000006000000ul,
                0x000000000E000000ul, 0x000000001E000000ul, 0x000000003E000000ul, 0x000000007E000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000100000000ul, 0x0000000000000000ul, 0x0000000200000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000010100000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000040200000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0001010100000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0008040200000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000040800ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000020000ul, 0x0000000000000000ul, 0x0000000000040000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000004000000ul,
                0x000000000C000000ul, 0x000000001C000000ul, 0x000000003C000000ul, 0x000000007C000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000200000000ul, 0x0000000000000000ul, 0x0000000400000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020200000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000080400000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002020200000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0010080400000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000081000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000020000ul, 0x0000000000000000ul, 0x0000000000040000ul, 0x0000000000000000ul,
                0x0000000000080000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000002000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000008000000ul, 0x0000000018000000ul, 0x0000000038000000ul, 0x0000000078000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000200000000ul, 0x0000000000000000ul, 0x0000000400000000ul, 0x0000000000000000ul,
                0x0000000800000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040400000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000100800000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040400000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0020100800000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000040200ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000102000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000040000ul, 0x0000000000000000ul, 0x0000000000080000ul,
                0x0000000000000000ul, 0x0000000000100000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000006000000ul, 0x0000000004000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000010000000ul, 0x0000000030000000ul, 0x0000000070000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000400000000ul, 0x0000000000000000ul, 0x0000000800000000ul,
                0x0000000000000000ul, 0x0000001000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000020400000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000080800000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000201000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080800000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0040201000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000080400ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000204000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000080000ul, 0x0000000000000000ul,
                0x0000000000100000ul, 0x0000000000000000ul, 0x0000000000200000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x000000000E000000ul, 0x000000000C000000ul, 0x0000000008000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000020000000ul, 0x0000000060000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000800000000ul, 0x0000000000000000ul,
                0x0000001000000000ul, 0x0000000000000000ul, 0x0000002000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000040800000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000101000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000402000000000ul,
                0x0002040800000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010101000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000100800ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000100000ul,
                0x0000000000000000ul, 0x0000000000200000ul, 0x0000000000000000ul, 0x0000000000400000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x000000001E000000ul, 0x000000001C000000ul, 0x0000000018000000ul, 0x0000000010000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000040000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000001000000000ul,
                0x0000000000000000ul, 0x0000002000000000ul, 0x0000000000000000ul, 0x0000004000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000081000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000202000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0004081000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020202000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000201000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000404000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000200000ul, 0x0000000000000000ul, 0x0000000000400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x000000003E000000ul, 0x000000003C000000ul, 0x0000000038000000ul, 0x0000000030000000ul,
                0x0000000020000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000002000000000ul, 0x0000000000000000ul, 0x0000004000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000102000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000404000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0008102000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040404000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000402000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000808000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000400000ul, 0x0000000000000000ul, 0x0000000000800000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x000000007E000000ul, 0x000000007C000000ul, 0x0000000078000000ul, 0x0000000070000000ul,
                0x0000000060000000ul, 0x0000000040000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000004000000000ul, 0x0000000000000000ul, 0x0000008000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000204000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000808000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0010204000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080808000000000ul,
            },
            {
                0x0000000001010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000002040800ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000001010000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000002040000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000001000000ul, 0x0000000000000000ul, 0x0000000002000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000200000000ul, 0x0000000600000000ul,
                0x0000000E00000000ul, 0x0000001E00000000ul, 0x0000003E00000000ul, 0x0000007E00000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000010000000000ul, 0x0000000000000000ul, 0x0000020000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0001010000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0004020000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000002020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000004081000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000002020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000004080000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000002000000ul, 0x0000000000000000ul, 0x0000000004000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000400000000ul,
                0x0000000C00000000ul, 0x0000001C00000000ul, 0x0000003C00000000ul, 0x0000007C00000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020000000000ul, 0x0000000000000000ul, 0x0000040000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002020000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0008040000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000004040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008102000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000004040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000008100000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000002000000ul, 0x0000000000000000ul, 0x0000000004000000ul, 0x0000000000000000ul,
                0x0000000008000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000200000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000800000000ul, 0x0000001800000000ul, 0x0000003800000000ul, 0x0000007800000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000020000000000ul, 0x0000000000000000ul, 0x0000040000000000ul, 0x0000000000000000ul,
                0x0000080000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0010080000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000010204000ul,
                0x0000000004020000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000010200000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000004000000ul, 0x0000000000000000ul, 0x0000000008000000ul,
                0x0000000000000000ul, 0x0000000010000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000600000000ul, 0x0000000400000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000001000000000ul, 0x0000003000000000ul, 0x0000007000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000040000000000ul, 0x0000000000000000ul, 0x0000080000000000ul,
                0x0000000000000000ul, 0x0000100000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0002040000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0020100000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000008040200ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000010101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000008040000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000010100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000020400000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000008000000ul, 0x0000000000000000ul,
                0x0000000010000000ul, 0x0000000000000000ul, 0x0000000020000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000E00000000ul, 0x0000000C00000000ul, 0x0000000800000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000002000000000ul, 0x0000006000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000080000000000ul, 0x0000000000000000ul,
                0x0000100000000000ul, 0x0000000000000000ul, 0x0000200000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0004080000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010100000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0040200000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000010080400ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000020202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000010080000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000020200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000010000000ul,
                0x0000000000000000ul, 0x0000000020000000ul, 0x0000000000000000ul, 0x0000000040000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000001E00000000ul, 0x0000001C00000000ul, 0x0000001800000000ul, 0x0000001000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000004000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000100000000000ul,
                0x0000000000000000ul, 0x0000200000000000ul, 0x0000000000000000ul, 0x0000400000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0008100000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020200000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000020100800ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000040404000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000020100000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000040400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000020000000ul, 0x0000000000000000ul, 0x0000000040000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000003E00000000ul, 0x0000003C00000000ul, 0x0000003800000000ul, 0x0000003000000000ul,
                0x0000002000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000200000000000ul, 0x0000000000000000ul, 0x0000400000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0010200000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040400000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000040201000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000080808000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000040200000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000080800000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000040000000ul, 0x0000000000000000ul, 0x0000000080000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000007E00000000ul, 0x0000007C00000000ul, 0x0000007800000000ul, 0x0000007000000000ul,
                0x0000006000000000ul, 0x0000004000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000400000000000ul, 0x0000000000000000ul, 0x0000800000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0020400000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080800000000000ul,
            },
            {
                0x0000000101010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000204081000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000101010000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000204080000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000101000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000204000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000100000000ul, 0x0000000000000000ul, 0x0000000200000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000020000000000ul, 0x0000060000000000ul,
                0x00000E0000000000ul, 0x00001E0000000000ul, 0x00003E0000000000ul, 0x00007E0000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0001000000000000ul, 0x0000000000000000ul, 0x0002000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000202020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000408102000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000202020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000408100000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000202000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000408000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000200000000ul, 0x0000000000000000ul, 0x0000000400000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000040000000000ul,
                0x00000C0000000000ul, 0x00001C0000000000ul, 0x00003C0000000000ul, 0x00007C0000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002000000000000ul, 0x0000000000000000ul, 0x0004000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000404040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000810204000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000404040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000810200000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000404000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000810000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000200000000ul, 0x0000000000000000ul, 0x0000000400000000ul, 0x0000000000000000ul,
                0x0000000800000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000020000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000080000000000ul, 0x0000180000000000ul, 0x0000380000000000ul, 0x0000780000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0002000000000000ul, 0x0000000000000000ul, 0x0004000000000000ul, 0x0000000000000000ul,
                0x0008000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000808080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000808080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000001020400000ul,
                0x0000000402000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000808000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000001020000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000400000000ul, 0x0000000000000000ul, 0x0000000800000000ul,
                0x0000000000000000ul, 0x0000001000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000060000000000ul, 0x0000040000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000100000000000ul, 0x0000300000000000ul, 0x0000700000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0004000000000000ul, 0x0000000000000000ul, 0x0008000000000000ul,
                0x0000000000000000ul, 0x0010000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000001010101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000804020000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000001010100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000804000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000001010000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000002040000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000800000000ul, 0x0000000000000000ul,
                0x0000001000000000ul, 0x0000000000000000ul, 0x0000002000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x00000E0000000000ul, 0x00000C0000000000ul, 0x0000080000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000200000000000ul, 0x0000600000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0008000000000000ul, 0x0000000000000000ul,
                0x0010000000000000ul, 0x0000000000000000ul, 0x0020000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000001008040200ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000002020202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000001008040000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000002020200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000001008000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000002020000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000001000000000ul,
                0x0000000000000000ul, 0x0000002000000000ul, 0x0000000000000000ul, 0x0000004000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x00001E0000000000ul, 0x00001C0000000000ul, 0x0000180000000000ul, 0x0000100000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000400000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0010000000000000ul,
                0x0000000000000000ul, 0x0020000000000000ul, 0x0000000000000000ul, 0x0040000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000002010080400ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000004040404000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000002010080000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000004040400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000002010000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000004040000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000002000000000ul, 0x0000000000000000ul, 0x0000004000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x00003E0000000000ul, 0x00003C0000000000ul, 0x0000380000000000ul, 0x0000300000000000ul,
                0x0000200000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0020000000000000ul, 0x0000000000000000ul, 0x0040000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000004020100800ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000008080808000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000004020100000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000008080800000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000004020000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000008080000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000004000000000ul, 0x0000000000000000ul, 0x0000008000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x00007E0000000000ul, 0x00007C0000000000ul, 0x0000780000000000ul, 0x0000700000000000ul,
                0x0000600000000000ul, 0x0000400000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0040000000000000ul, 0x0000000000000000ul, 0x0080000000000000ul,
            },
            {
                0x0000010101010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000020408102000ul, 0x0000000000000000ul,
                0x0000010101010000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020408100000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000010101000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000020408000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000010100000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000020400000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000010000000000ul, 0x0000000000000000ul, 0x0000020000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0002000000000000ul, 0x0006000000000000ul,
                0x000E000000000000ul, 0x001E000000000000ul, 0x003E000000000000ul, 0x007E000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000020202020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000040810204000ul,
                0x0000000000000000ul, 0x0000020202020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040810200000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020202000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000040810000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020200000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000040800000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000020000000000ul, 0x0000000000000000ul, 0x0000040000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0004000000000000ul,
                0x000C000000000000ul, 0x001C000000000000ul, 0x003C000000000000ul, 0x007C000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040404040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040404040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000081020400000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040404000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000081020000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000040400000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000081000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000020000000000ul, 0x0000000000000000ul, 0x0000040000000000ul, 0x0000000000000000ul,
                0x0000080000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0002000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0008000000000000ul, 0x0018000000000000ul, 0x0038000000000000ul, 0x0078000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000080808080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000080808080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000080808000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000102040000000ul,
                0x0000040200000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000080800000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000102000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000040000000000ul, 0x0000000000000000ul, 0x0000080000000000ul,
                0x0000000000000000ul, 0x0000100000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0006000000000000ul, 0x0004000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0010000000000000ul, 0x0030000000000000ul, 0x0070000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000101010101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000101010100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000080402000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000101010000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000080400000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000101000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000204000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000080000000000ul, 0x0000000000000000ul,
                0x0000100000000000ul, 0x0000000000000000ul, 0x0000200000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x000E000000000000ul, 0x000C000000000000ul, 0x0008000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0020000000000000ul, 0x0060000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000202020202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000100804020000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000202020200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000100804000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000202020000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000100800000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000202000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000100000000000ul,
                0x0000000000000000ul, 0x0000200000000000ul, 0x0000000000000000ul, 0x0000400000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x001E000000000000ul, 0x001C000000000000ul, 0x0018000000000000ul, 0x0010000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0040000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000201008040200ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000404040404000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000201008040000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000404040400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000201008000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000404040000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000201000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000404000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000200000000000ul, 0x0000000000000000ul, 0x0000400000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x003E000000000000ul, 0x003C000000000000ul, 0x0038000000000000ul, 0x0030000000000000ul,
                0x0020000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000402010080400ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000808080808000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000402010080000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000808080800000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000402010000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000808080000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000402000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000808000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000400000000000ul, 0x0000000000000000ul, 0x0000800000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x007E000000000000ul, 0x007C000000000000ul, 0x0078000000000000ul, 0x0070000000000000ul,
                0x0060000000000000ul, 0x0040000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0001010101010100ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0002040810204000ul,
                0x0001010101010000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0002040810200000ul, 0x0000000000000000ul,
                0x0001010101000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002040810000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0001010100000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0002040800000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0001010000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0002040000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0001000000000000ul, 0x0000000000000000ul, 0x0002000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0200000000000000ul, 0x0600000000000000ul,
                0x0E00000000000000ul, 0x1E00000000000000ul, 0x3E00000000000000ul, 0x7E00000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0002020202020200ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002020202020000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0004081020400000ul,
                0x0000000000000000ul, 0x0002020202000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004081020000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002020200000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0004081000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002020000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0004080000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0002000000000000ul, 0x0000000000000000ul, 0x0004000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0400000000000000ul,
                0x0C00000000000000ul, 0x1C00000000000000ul, 0x3C00000000000000ul, 0x7C00000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040404040400ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040404040000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040404000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008102040000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040400000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0008102000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0004040000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0008100000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0002000000000000ul, 0x0000000000000000ul, 0x0004000000000000ul, 0x0000000000000000ul,
                0x0008000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0200000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0800000000000000ul, 0x1800000000000000ul, 0x3800000000000000ul, 0x7800000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080808080800ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080808080000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080808000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080800000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0010204000000000ul,
                0x0004020000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0008080000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0010200000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0004000000000000ul, 0x0000000000000000ul, 0x0008000000000000ul,
                0x0000000000000000ul, 0x0010000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0600000000000000ul, 0x0400000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x1000000000000000ul, 0x3000000000000000ul, 0x7000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010101010101000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010101010100000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010101010000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0008040200000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010101000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0008040000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010100000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0020400000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0008000000000000ul, 0x0000000000000000ul,
                0x0010000000000000ul, 0x0000000000000000ul, 0x0020000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0E00000000000000ul, 0x0C00000000000000ul, 0x0800000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x2000000000000000ul, 0x6000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020202020202000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020202020200000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0010080402000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020202020000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0010080400000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020202000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0010080000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020200000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0010000000000000ul,
                0x0000000000000000ul, 0x0020000000000000ul, 0x0000000000000000ul, 0x0040000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x1E00000000000000ul, 0x1C00000000000000ul, 0x1800000000000000ul, 0x1000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x4000000000000000ul,
            },
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040404040404000ul, 0x0000000000000000ul,
                0x0020100804020000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040404040400000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0020100804000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040404040000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0020100800000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040404000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0020100000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040400000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0020000000000000ul, 0x0000000000000000ul, 0x0040000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x3E00000000000000ul, 0x3C00000000000000ul, 0x3800000000000000ul, 0x3000000000000000ul,
                0x2000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            },
            {
                0x0040201008040200ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080808080808000ul,
                0x0000000000000000ul, 0x0040201008040000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080808080800000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0040201008000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080808080000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0040201000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080808000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0040200000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0080800000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0040000000000000ul, 0x0000000000000000ul, 0x0080000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x7E00000000000000ul, 0x7C00000000000000ul, 0x7800000000000000ul, 0x7000000000000000ul,
                0x6000000000000000ul, 0x4000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            }
            #endregion
        };

        private static readonly Ray[] vectors = new Ray[]
        {
            #region vectors data
            new Ray(0x0101010101010100ul, 0x8040201008040200ul, 0x00000000000000FEul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul),
            new Ray(0x0202020202020200ul, 0x0080402010080400ul, 0x00000000000000FCul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000001ul, 0x0000000000000100ul),
            new Ray(0x0404040404040400ul, 0x0000804020100800ul, 0x00000000000000F8ul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000003ul, 0x0000000000010200ul),
            new Ray(0x0808080808080800ul, 0x0000008040201000ul, 0x00000000000000F0ul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000007ul, 0x0000000001020400ul),
            new Ray(0x1010101010101000ul, 0x0000000080402000ul, 0x00000000000000E0ul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x000000000000000Ful, 0x0000000102040800ul),
            new Ray(0x2020202020202000ul, 0x0000000000804000ul, 0x00000000000000C0ul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x000000000000001Ful, 0x0000010204081000ul),
            new Ray(0x4040404040404000ul, 0x0000000000008000ul, 0x0000000000000080ul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x000000000000003Ful, 0x0001020408102000ul),
            new Ray(0x8080808080808000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x000000000000007Ful, 0x0102040810204000ul),
            new Ray(0x0101010101010000ul, 0x4020100804020000ul, 0x000000000000FE00ul, 0x0000000000000002ul,
                    0x0000000000000001ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul),
            new Ray(0x0202020202020000ul, 0x8040201008040000ul, 0x000000000000FC00ul, 0x0000000000000004ul,
                    0x0000000000000002ul, 0x0000000000000001ul, 0x0000000000000100ul, 0x0000000000010000ul),
            new Ray(0x0404040404040000ul, 0x0080402010080000ul, 0x000000000000F800ul, 0x0000000000000008ul,
                    0x0000000000000004ul, 0x0000000000000002ul, 0x0000000000000300ul, 0x0000000001020000ul),
            new Ray(0x0808080808080000ul, 0x0000804020100000ul, 0x000000000000F000ul, 0x0000000000000010ul,
                    0x0000000000000008ul, 0x0000000000000004ul, 0x0000000000000700ul, 0x0000000102040000ul),
            new Ray(0x1010101010100000ul, 0x0000008040200000ul, 0x000000000000E000ul, 0x0000000000000020ul,
                    0x0000000000000010ul, 0x0000000000000008ul, 0x0000000000000F00ul, 0x0000010204080000ul),
            new Ray(0x2020202020200000ul, 0x0000000080400000ul, 0x000000000000C000ul, 0x0000000000000040ul,
                    0x0000000000000020ul, 0x0000000000000010ul, 0x0000000000001F00ul, 0x0001020408100000ul),
            new Ray(0x4040404040400000ul, 0x0000000000800000ul, 0x0000000000008000ul, 0x0000000000000080ul,
                    0x0000000000000040ul, 0x0000000000000020ul, 0x0000000000003F00ul, 0x0102040810200000ul),
            new Ray(0x8080808080800000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0000000000000080ul, 0x0000000000000040ul, 0x0000000000007F00ul, 0x0204081020400000ul),
            new Ray(0x0101010101000000ul, 0x2010080402000000ul, 0x0000000000FE0000ul, 0x0000000000000204ul,
                    0x0000000000000101ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul),
            new Ray(0x0202020202000000ul, 0x4020100804000000ul, 0x0000000000FC0000ul, 0x0000000000000408ul,
                    0x0000000000000202ul, 0x0000000000000100ul, 0x0000000000010000ul, 0x0000000001000000ul),
            new Ray(0x0404040404000000ul, 0x8040201008000000ul, 0x0000000000F80000ul, 0x0000000000000810ul,
                    0x0000000000000404ul, 0x0000000000000201ul, 0x0000000000030000ul, 0x0000000102000000ul),
            new Ray(0x0808080808000000ul, 0x0080402010000000ul, 0x0000000000F00000ul, 0x0000000000001020ul,
                    0x0000000000000808ul, 0x0000000000000402ul, 0x0000000000070000ul, 0x0000010204000000ul),
            new Ray(0x1010101010000000ul, 0x0000804020000000ul, 0x0000000000E00000ul, 0x0000000000002040ul,
                    0x0000000000001010ul, 0x0000000000000804ul, 0x00000000000F0000ul, 0x0001020408000000ul),
            new Ray(0x2020202020000000ul, 0x0000008040000000ul, 0x0000000000C00000ul, 0x0000000000004080ul,
                    0x0000000000002020ul, 0x0000000000001008ul, 0x00000000001F0000ul, 0x0102040810000000ul),
            new Ray(0x4040404040000000ul, 0x0000000080000000ul, 0x0000000000800000ul, 0x0000000000008000ul,
                    0x0000000000004040ul, 0x0000000000002010ul, 0x00000000003F0000ul, 0x0204081020000000ul),
            new Ray(0x8080808080000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0000000000008080ul, 0x0000000000004020ul, 0x00000000007F0000ul, 0x0408102040000000ul),
            new Ray(0x0101010100000000ul, 0x1008040200000000ul, 0x00000000FE000000ul, 0x0000000000020408ul,
                    0x0000000000010101ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul),
            new Ray(0x0202020200000000ul, 0x2010080400000000ul, 0x00000000FC000000ul, 0x0000000000040810ul,
                    0x0000000000020202ul, 0x0000000000010000ul, 0x0000000001000000ul, 0x0000000100000000ul),
            new Ray(0x0404040400000000ul, 0x4020100800000000ul, 0x00000000F8000000ul, 0x0000000000081020ul,
                    0x0000000000040404ul, 0x0000000000020100ul, 0x0000000003000000ul, 0x0000010200000000ul),
            new Ray(0x0808080800000000ul, 0x8040201000000000ul, 0x00000000F0000000ul, 0x0000000000102040ul,
                    0x0000000000080808ul, 0x0000000000040201ul, 0x0000000007000000ul, 0x0001020400000000ul),
            new Ray(0x1010101000000000ul, 0x0080402000000000ul, 0x00000000E0000000ul, 0x0000000000204080ul,
                    0x0000000000101010ul, 0x0000000000080402ul, 0x000000000F000000ul, 0x0102040800000000ul),
            new Ray(0x2020202000000000ul, 0x0000804000000000ul, 0x00000000C0000000ul, 0x0000000000408000ul,
                    0x0000000000202020ul, 0x0000000000100804ul, 0x000000001F000000ul, 0x0204081000000000ul),
            new Ray(0x4040404000000000ul, 0x0000008000000000ul, 0x0000000080000000ul, 0x0000000000800000ul,
                    0x0000000000404040ul, 0x0000000000201008ul, 0x000000003F000000ul, 0x0408102000000000ul),
            new Ray(0x8080808000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0000000000808080ul, 0x0000000000402010ul, 0x000000007F000000ul, 0x0810204000000000ul),
            new Ray(0x0101010000000000ul, 0x0804020000000000ul, 0x000000FE00000000ul, 0x0000000002040810ul,
                    0x0000000001010101ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul),
            new Ray(0x0202020000000000ul, 0x1008040000000000ul, 0x000000FC00000000ul, 0x0000000004081020ul,
                    0x0000000002020202ul, 0x0000000001000000ul, 0x0000000100000000ul, 0x0000010000000000ul),
            new Ray(0x0404040000000000ul, 0x2010080000000000ul, 0x000000F800000000ul, 0x0000000008102040ul,
                    0x0000000004040404ul, 0x0000000002010000ul, 0x0000000300000000ul, 0x0001020000000000ul),
            new Ray(0x0808080000000000ul, 0x4020100000000000ul, 0x000000F000000000ul, 0x0000000010204080ul,
                    0x0000000008080808ul, 0x0000000004020100ul, 0x0000000700000000ul, 0x0102040000000000ul),
            new Ray(0x1010100000000000ul, 0x8040200000000000ul, 0x000000E000000000ul, 0x0000000020408000ul,
                    0x0000000010101010ul, 0x0000000008040201ul, 0x0000000F00000000ul, 0x0204080000000000ul),
            new Ray(0x2020200000000000ul, 0x0080400000000000ul, 0x000000C000000000ul, 0x0000000040800000ul,
                    0x0000000020202020ul, 0x0000000010080402ul, 0x0000001F00000000ul, 0x0408100000000000ul),
            new Ray(0x4040400000000000ul, 0x0000800000000000ul, 0x0000008000000000ul, 0x0000000080000000ul,
                    0x0000000040404040ul, 0x0000000020100804ul, 0x0000003F00000000ul, 0x0810200000000000ul),
            new Ray(0x8080800000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0000000080808080ul, 0x0000000040201008ul, 0x0000007F00000000ul, 0x1020400000000000ul),
            new Ray(0x0101000000000000ul, 0x0402000000000000ul, 0x0000FE0000000000ul, 0x0000000204081020ul,
                    0x0000000101010101ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul),
            new Ray(0x0202000000000000ul, 0x0804000000000000ul, 0x0000FC0000000000ul, 0x0000000408102040ul,
                    0x0000000202020202ul, 0x0000000100000000ul, 0x0000010000000000ul, 0x0001000000000000ul),
            new Ray(0x0404000000000000ul, 0x1008000000000000ul, 0x0000F80000000000ul, 0x0000000810204080ul,
                    0x0000000404040404ul, 0x0000000201000000ul, 0x0000030000000000ul, 0x0102000000000000ul),
            new Ray(0x0808000000000000ul, 0x2010000000000000ul, 0x0000F00000000000ul, 0x0000001020408000ul,
                    0x0000000808080808ul, 0x0000000402010000ul, 0x0000070000000000ul, 0x0204000000000000ul),
            new Ray(0x1010000000000000ul, 0x4020000000000000ul, 0x0000E00000000000ul, 0x0000002040800000ul,
                    0x0000001010101010ul, 0x0000000804020100ul, 0x00000F0000000000ul, 0x0408000000000000ul),
            new Ray(0x2020000000000000ul, 0x8040000000000000ul, 0x0000C00000000000ul, 0x0000004080000000ul,
                    0x0000002020202020ul, 0x0000001008040201ul, 0x00001F0000000000ul, 0x0810000000000000ul),
            new Ray(0x4040000000000000ul, 0x0080000000000000ul, 0x0000800000000000ul, 0x0000008000000000ul,
                    0x0000004040404040ul, 0x0000002010080402ul, 0x00003F0000000000ul, 0x1020000000000000ul),
            new Ray(0x8080000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0000008080808080ul, 0x0000004020100804ul, 0x00007F0000000000ul, 0x2040000000000000ul),
            new Ray(0x0100000000000000ul, 0x0200000000000000ul, 0x00FE000000000000ul, 0x0000020408102040ul,
                    0x0000010101010101ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul),
            new Ray(0x0200000000000000ul, 0x0400000000000000ul, 0x00FC000000000000ul, 0x0000040810204080ul,
                    0x0000020202020202ul, 0x0000010000000000ul, 0x0001000000000000ul, 0x0100000000000000ul),
            new Ray(0x0400000000000000ul, 0x0800000000000000ul, 0x00F8000000000000ul, 0x0000081020408000ul,
                    0x0000040404040404ul, 0x0000020100000000ul, 0x0003000000000000ul, 0x0200000000000000ul),
            new Ray(0x0800000000000000ul, 0x1000000000000000ul, 0x00F0000000000000ul, 0x0000102040800000ul,
                    0x0000080808080808ul, 0x0000040201000000ul, 0x0007000000000000ul, 0x0400000000000000ul),
            new Ray(0x1000000000000000ul, 0x2000000000000000ul, 0x00E0000000000000ul, 0x0000204080000000ul,
                    0x0000101010101010ul, 0x0000080402010000ul, 0x000F000000000000ul, 0x0800000000000000ul),
            new Ray(0x2000000000000000ul, 0x4000000000000000ul, 0x00C0000000000000ul, 0x0000408000000000ul,
                    0x0000202020202020ul, 0x0000100804020100ul, 0x001F000000000000ul, 0x1000000000000000ul),
            new Ray(0x4000000000000000ul, 0x8000000000000000ul, 0x0080000000000000ul, 0x0000800000000000ul,
                    0x0000404040404040ul, 0x0000201008040201ul, 0x003F000000000000ul, 0x2000000000000000ul),
            new Ray(0x8000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0000808080808080ul, 0x0000402010080402ul, 0x007F000000000000ul, 0x4000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0xFE00000000000000ul, 0x0002040810204080ul,
                    0x0001010101010101ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0xFC00000000000000ul, 0x0004081020408000ul,
                    0x0002020202020202ul, 0x0001000000000000ul, 0x0100000000000000ul, 0x0000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0xF800000000000000ul, 0x0008102040800000ul,
                    0x0004040404040404ul, 0x0002010000000000ul, 0x0300000000000000ul, 0x0000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0xF000000000000000ul, 0x0010204080000000ul,
                    0x0008080808080808ul, 0x0004020100000000ul, 0x0700000000000000ul, 0x0000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0xE000000000000000ul, 0x0020408000000000ul,
                    0x0010101010101010ul, 0x0008040201000000ul, 0x0F00000000000000ul, 0x0000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0xC000000000000000ul, 0x0040800000000000ul,
                    0x0020202020202020ul, 0x0010080402010000ul, 0x1F00000000000000ul, 0x0000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0x8000000000000000ul, 0x0080000000000000ul,
                    0x0040404040404040ul, 0x0020100804020100ul, 0x3F00000000000000ul, 0x0000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0080808080808080ul, 0x0040201008040201ul, 0x7F00000000000000ul, 0x0000000000000000ul),
            new Ray(0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                    0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul)
            #endregion
        };

        private static readonly Ray[] revVectors = new Ray[Constants.MAX_SQUARES + 1];

        public static readonly ulong[] MaskFiles = new ulong[]
        {
            #region MaskFiles data
            0x0101010101010101ul, 0x0202020202020202ul, 0x0404040404040404ul, 0x0808080808080808ul,
            0x1010101010101010ul, 0x2020202020202020ul, 0x4040404040404040ul, 0x8080808080808080ul,
            0x0101010101010101ul, 0x0202020202020202ul, 0x0404040404040404ul, 0x0808080808080808ul,
            0x1010101010101010ul, 0x2020202020202020ul, 0x4040404040404040ul, 0x8080808080808080ul,
            0x0101010101010101ul, 0x0202020202020202ul, 0x0404040404040404ul, 0x0808080808080808ul,
            0x1010101010101010ul, 0x2020202020202020ul, 0x4040404040404040ul, 0x8080808080808080ul,
            0x0101010101010101ul, 0x0202020202020202ul, 0x0404040404040404ul, 0x0808080808080808ul,
            0x1010101010101010ul, 0x2020202020202020ul, 0x4040404040404040ul, 0x8080808080808080ul,
            0x0101010101010101ul, 0x0202020202020202ul, 0x0404040404040404ul, 0x0808080808080808ul,
            0x1010101010101010ul, 0x2020202020202020ul, 0x4040404040404040ul, 0x8080808080808080ul,
            0x0101010101010101ul, 0x0202020202020202ul, 0x0404040404040404ul, 0x0808080808080808ul,
            0x1010101010101010ul, 0x2020202020202020ul, 0x4040404040404040ul, 0x8080808080808080ul,
            0x0101010101010101ul, 0x0202020202020202ul, 0x0404040404040404ul, 0x0808080808080808ul,
            0x1010101010101010ul, 0x2020202020202020ul, 0x4040404040404040ul, 0x8080808080808080ul,
            0x0101010101010101ul, 0x0202020202020202ul, 0x0404040404040404ul, 0x0808080808080808ul,
            0x1010101010101010ul, 0x2020202020202020ul, 0x4040404040404040ul, 0x8080808080808080ul
            #endregion
        };

        public static readonly ulong[] MaskRanks = new ulong[]
        {
            #region MaskRanks data
            0x00000000000000FFul, 0x00000000000000FFul, 0x00000000000000FFul, 0x00000000000000FFul,
            0x00000000000000FFul, 0x00000000000000FFul, 0x00000000000000FFul, 0x00000000000000FFul,
            0x000000000000FF00ul, 0x000000000000FF00ul, 0x000000000000FF00ul, 0x000000000000FF00ul,
            0x000000000000FF00ul, 0x000000000000FF00ul, 0x000000000000FF00ul, 0x000000000000FF00ul,
            0x0000000000FF0000ul, 0x0000000000FF0000ul, 0x0000000000FF0000ul, 0x0000000000FF0000ul,
            0x0000000000FF0000ul, 0x0000000000FF0000ul, 0x0000000000FF0000ul, 0x0000000000FF0000ul,
            0x00000000FF000000ul, 0x00000000FF000000ul, 0x00000000FF000000ul, 0x00000000FF000000ul,
            0x00000000FF000000ul, 0x00000000FF000000ul, 0x00000000FF000000ul, 0x00000000FF000000ul,
            0x000000FF00000000ul, 0x000000FF00000000ul, 0x000000FF00000000ul, 0x000000FF00000000ul,
            0x000000FF00000000ul, 0x000000FF00000000ul, 0x000000FF00000000ul, 0x000000FF00000000ul,
            0x0000FF0000000000ul, 0x0000FF0000000000ul, 0x0000FF0000000000ul, 0x0000FF0000000000ul,
            0x0000FF0000000000ul, 0x0000FF0000000000ul, 0x0000FF0000000000ul, 0x0000FF0000000000ul,
            0x00FF000000000000ul, 0x00FF000000000000ul, 0x00FF000000000000ul, 0x00FF000000000000ul,
            0x00FF000000000000ul, 0x00FF000000000000ul, 0x00FF000000000000ul, 0x00FF000000000000ul,
            0xFF00000000000000ul, 0xFF00000000000000ul, 0xFF00000000000000ul, 0xFF00000000000000ul,
            0xFF00000000000000ul, 0xFF00000000000000ul, 0xFF00000000000000ul, 0xFF00000000000000ul
            #endregion
        };

        private static readonly int[][] pawnLeft = 
        {
            #region pawnLeft data
            new[]
            {
                -1,  8,  9, 10, 11, 12, 13, 14,
                -1, 16, 17, 18, 19, 20, 21, 22,
                -1, 24, 25, 26, 27, 28, 29, 30,
                -1, 32, 33, 34, 35, 36, 37, 38,
                -1, 40, 41, 42, 43, 44, 45, 46,
                -1, 48, 49, 50, 51, 52, 53, 54,
                -1, 56, 57, 58, 59, 60, 61, 62,
                -1, -1, -1, -1, -1, -1, -1, -1
            },
            new[]
            {
                -1, -1, -1, -1, -1, -1, -1, -1,
                -1,  0,  1,  2,  3,  4,  5,  6,
                -1,  8,  9, 10, 11, 12, 13, 14,
                -1, 16, 17, 18, 19, 20, 21, 22,
                -1, 24, 25, 26, 27, 28, 29, 30,
                -1, 32, 33, 34, 35, 36, 37, 38,
                -1, 40, 41, 42, 43, 44, 45, 46,
                -1, 48, 49, 50, 51, 52, 53, 54
            }
            #endregion
        };

        private static readonly int[][] pawnRight = 
        {
            #region pawnRight data
            new[]
            {
                 9, 10, 11, 12, 13, 14, 15, -1,
                17, 18, 19, 20, 21, 22, 23, -1,
                25, 26, 27, 28, 29, 30, 31, -1,
                33, 34, 35, 36, 37, 38, 39, -1,
                41, 42, 43, 44, 45, 46, 47, -1,
                49, 50, 51, 52, 53, 54, 55, -1,
                57, 58, 59, 60, 61, 62, 63, -1,
                -1, -1, -1, -1, -1, -1, -1, -1
            },
            new[]
            {
                -1, -1, -1, -1, -1, -1, -1, -1,
                 1,  2,  3,  4,  5,  6,  7, -1,
                 9, 10, 11, 12, 13, 14, 15, -1,
                17, 18, 19, 20, 21, 22, 23, -1,
                25, 26, 27, 28, 29, 30, 31, -1,
                33, 34, 35, 36, 37, 38, 39, -1,
                41, 42, 43, 44, 45, 46, 47, -1,
                49, 50, 51, 52, 53, 54, 55, -1
            }
            #endregion
        };

        private static readonly int[,] pawnPlus = new int[,]
        {
            #region pawnPlus data
            {
                 8,  9, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                40, 41, 42, 43, 44, 45, 46, 47,
                48, 49, 50, 51, 52, 53, 54, 55,
                56, 57, 58, 59, 60, 61, 62, 63,
                 0,  0,  0,  0,  0,  0,  0,  0
            },
            {
                 0,  0,  0,  0,  0,  0,  0,  0,
                 0,  1,  2,  3,  4,  5,  6,  7,
                 8,  9, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                40, 41, 42, 43, 44, 45, 46, 47,
                48, 49, 50, 51, 52, 53, 54, 55
            }
            #endregion
        };

        private static readonly int[,] pawnDouble = new int[,]
        {
            #region pawnDouble data
            {
                16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                40, 41, 42, 43, 44, 45, 46, 47,
                48, 49, 50, 51, 52, 53, 54, 55,
                56, 57, 58, 59, 60, 61, 62, 63,
                 0,  0,  0,  0,  0,  0,  0,  0,
                 0,  0,  0,  0,  0,  0,  0,  0
            },
            {
                 0,  0,  0,  0,  0,  0,  0,  0,
                 0,  0,  0,  0,  0,  0,  0,  0,
                 0,  1,  2,  3,  4,  5,  6,  7,
                 8,  9, 10, 11, 12, 13, 14, 15,
                16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39,
                40, 41, 42, 43, 44, 45, 46, 47
            }
            #endregion
        };
        #endregion
    }
}