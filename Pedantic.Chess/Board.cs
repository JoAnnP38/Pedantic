// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 03-12-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Board.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implement Board class to represents the current game state and
//     position.
// </summary>
// ***********************************************************************

using System.Diagnostics;
using Pedantic.Collections;
using Pedantic.Utilities;
using System.Runtime.CompilerServices;
using System.Text;

using Index = Pedantic.Chess.Index;

namespace Pedantic.Chess
{
    public sealed partial class Board : ICloneable
    {
        public const ulong WHITE_KS_CLEAR_MASK = (1ul << Index.F1) | (1ul << Index.G1);
        public const ulong WHITE_QS_CLEAR_MASK = (1ul << Index.B1) | (1ul << Index.C1) | (1ul << Index.D1);
        public const ulong BLACK_KS_CLEAR_MASK = (1ul << Index.F8) | (1ul << Index.G8);
        public const ulong BLACK_QS_CLEAR_MASK = (1ul << Index.B8) | (1ul << Index.C8) | (1ul << Index.D8);
        public const ulong ALL_SQUARES_MASK = 0xFFFFFFFFFFFFFFFFul;

        private readonly Square[] board = new Square[Constants.MAX_SQUARES];
        private readonly Array2D<ulong> pieces = new (Constants.MAX_COLORS, Constants.MAX_PIECES, true);
        private readonly ulong[] units = new ulong[Constants.MAX_COLORS];
        private ulong all = 0ul;
        private readonly short[] material = new short[Constants.MAX_COLORS];

        #region Incrementally updated values used by Evaluation

        private readonly short[] opMaterial = new short[Constants.MAX_COLORS];
        private readonly short[] egMaterial = new short[Constants.MAX_COLORS];
        private readonly Array2D<short> opPcSquare = new (Constants.MAX_COLORS, Constants.MAX_KING_PLACEMENTS, true);
        private readonly Array2D<short> egPcSquare = new (Constants.MAX_COLORS, Constants.MAX_KING_PLACEMENTS, true);
        private ulong pawnHash;

        #endregion

        private Color sideToMove = Color.None;
        private CastlingRights castling = CastlingRights.None;
        private int enPassant = Index.NONE;
        private int enPassantValidated = Index.NONE;
        private int halfMoveClock;
        private int fullMoveCounter;
        private ulong hash;
        private readonly bool[] hasCastled = new bool[Constants.MAX_COLORS];

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
                RevVectors[63 - sq] = Vectors[sq];
            }

            InitFancyMagic();
            InitPext();
            IsPextSupported = PextSupported();
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
            Array.Copy(other.opMaterial, opMaterial, opMaterial.Length);
            Array.Copy(other.egMaterial, egMaterial, egMaterial.Length);
            opPcSquare.Copy(other.opPcSquare);
            egPcSquare.Copy(other.egPcSquare);
            Array.Copy(other.hasCastled, hasCastled, hasCastled.Length);
            pawnHash = other.pawnHash;
        }

        #endregion

        public ReadOnlySpan<Square> PieceBoard => board;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Pieces(Color color, Piece piece) => pieces[(int)color, (int)piece];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Units(Color color) => units[(int)color];

        public ulong All => all;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong DiagonalSliders(Color color) =>
            pieces[(int)color, (int)Piece.Bishop] | pieces[(int)color, (int)Piece.Queen];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong OrthogonalSliders(Color color) =>
            pieces[(int)color, (int)Piece.Rook] | pieces[(int)color, (int)Piece.Queen];


        public Color SideToMove => sideToMove;
        public Color OpponentColor => sideToMove.Other();
        public CastlingRights Castling => castling;
        public int EnPassant => enPassant;
        public int EnPassantValidated => enPassantValidated;
        public int HalfMoveClock => halfMoveClock;
        public int FullMoveCounter => fullMoveCounter;
        public ulong Hash => hash;
        public bool[] HasCastled => hasCastled;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short Material(Color color) => material[(int)color];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short MaterialNoKing(Color color) => (short)(material[(int)color] - Piece.King.Value());

        public short PieceMaterial(Color color)
        {
            short pieceMaterial = 0;
            for (Piece piece = Piece.Knight; piece < Piece.King; piece++)
            {
                pieceMaterial += (short)(BitOps.PopCount(Pieces(color, piece)) * piece.Value());
            }

            return pieceMaterial;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PieceCount(Color color, Piece piece)
        {
            return BitOps.PopCount(Pieces(color, piece));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MinorPieceCount(Color color)
        {
            return PieceCount(color, Piece.Knight) + PieceCount(color, Piece.Bishop);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MajorPieceCount(Color color)
        {
            return PieceCount(color, Piece.Rook) + PieceCount(color, Piece.Queen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AllPieceCount(Color color)
        {
            return MinorPieceCount(color) + MajorPieceCount(color);
        }

        public short TotalMaterial => (short)(material[0] + material[1]);
        public short TotalMaterialNoKings => (short)(MaterialNoKing(Color.White) + MaterialNoKing(Color.Black));
        public ulong PawnHash => pawnHash;
        public short[] OpeningMaterial => opMaterial;
        public short[] EndGameMaterial => egMaterial;
        public Array2D<short> OpeningPieceSquare => opPcSquare;
        public Array2D<short> EndGamePieceSquare => egPcSquare;

        public ulong LastMove
        {
            get
            {
                if (gameStack.TryPeek(out BoardState item))
                {
                    return item.Move;
                }

                return Move.NullMove;
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
            enPassant = Index.NONE;
            enPassantValidated = Index.NONE;
            halfMoveClock = 0;
            fullMoveCounter = 0;
            hash = 0;
            gameStack.Clear();
            Array.Clear(material);
            Array.Clear(opMaterial);
            Array.Clear(egMaterial);
            opPcSquare.Clear();
            egPcSquare.Clear();
            Array.Fill(hasCastled, false);
            pawnHash = 0;
        }

        public bool IsLegalMove(ulong move)
        {
            bool legal = false;
            IHistory hist = new FakeHistory();
            int from = Move.GetFrom(move);
            MoveType type = Move.GetMoveType(move);
            Piece piece = Move.GetPiece(move);
            if (piece == Piece.None)
            {
                return false;
            }

            MoveList moveList = moveListPool.Get();

            switch (piece, type)
            {
                case (Piece.Pawn, MoveType.EnPassant):
                    GenerateEnPassant(moveList, 1ul << from);
                    break;
                    
                case (Piece.Pawn, not MoveType.EnPassant):
                    GeneratePawnMoves(moveList, hist, 1ul << from);
                    break;

                case (Piece.King, MoveType.Castle):
                    GenerateCastling(moveList, hist);
                    break;

                default:
                    GeneratePieceMoves(moveList, hist, piece, from);
                    break;
            }

            for (int n = 0; n < moveList.Count; n++)
            {
                if (Move.Compare(move, moveList[n]) != 0)
                {
                    continue;
                }

                if (MakeMove(move))
                {
                    UnmakeMove();
                    legal = true;
                }

                break;
            }

            moveListPool.Return(moveList);
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

        public ulong GetPieceLineup(Color color)
        {
            ulong lineup = 0;

            for (Piece piece = Piece.Queen; piece >= Piece.Pawn; piece--)
            {
                int count = BitOps.PopCount(Pieces(color, piece));
                if (count >= 0)
                {
                    for (int n = 0; n < count; ++n)
                    {
                        lineup <<= 3;
                        lineup |= (ulong)(piece + 1);
                    }
                }
            }

            return lineup;
        }

        public unsafe (bool Repeated, bool OverFiftyMoves) PositionRepeated()
        {
            if (halfMoveClock < 4 || gameStack.Count <= 1)
            {
                return (false, false);
            }

            if (halfMoveClock > 99)
            {
                return (false, true);
            }

            var stackSpan = gameStack.AsSpan();
            int max = Math.Max(stackSpan.Length - halfMoveClock, 0);
            int start = stackSpan.Length - 2;

            fixed (BoardState* pStart = &stackSpan[start], pEnd = &stackSpan[max])
            {
                for (BoardState* p = pStart; p >= pEnd; p -= 2)
                {
                    if (hash == p->Hash)
                    {
                        return (true, false);
                    }
                }
            }

            return (false, false);
        }

        public bool GameDrawnByRepetition()
        {
            int matchCount = 1;
            if (halfMoveClock > 99)
            {
                return true;
            }

            if (gameStack.Count > halfMoveClock)
            {
                ReadOnlySpan<BoardState> stackSpan = gameStack.AsSpan();
                if (stackSpan.Length < 4)
                {
                    return false;
                }
                for (int n = stackSpan.Length - 1; n >= stackSpan.Length - halfMoveClock && matchCount < 3; n--)
                {
                    if (hash == stackSpan[n].Hash)
                    {
                        matchCount++;
                    }
                }
            }

            return matchCount == 3;
        }

        public bool IsEnPassantValid(Color color)
        {
            if (enPassant == Index.NONE)
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
                mat += BitOps.PopCount(Pieces(color, piece)) * piece.Value();
                if (mat >= minMaterial)
                {
                    return true;
                }
            }

            return false;
        }

        public int PieceCount(Color color)
        {
            return BitOps.PopCount(Units(color) & ~Pieces(color, Piece.Pawn));
        }

        public GamePhase Phase
        {
            get
            {
                if (TotalMaterialNoKings >= Evaluation.OpeningPhaseMaterial)
                {
                    return GamePhase.Opening;
                }

                if (TotalMaterialNoKings <= Evaluation.EndGamePhaseMaterial)
                {
                    return GamePhase.EndGame;
                }

                return GamePhase.MidGame;
            }
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
            PushBoardState();
            bool result = MakeMoveNs(move);
            if (!result)
            {
                PopBoardState();
            }

            return result;
        }

        public bool MakeMoveNs(ulong move)
        {
            gameStack.Peek().Move = move;

            if (enPassantValidated != Index.NONE)
            {
                hash = ZobristHash.HashEnPassant(hash, enPassantValidated);
            }

            enPassant = Index.NONE;
            enPassantValidated = Index.NONE;
            hash = ZobristHash.HashCastling(hash, castling);
            Color opponent = OpponentColor;

            Move.Unpack(move, out Piece piece, out int from, out int to, out MoveType type, out Piece capture, out Piece promote, out int _);

            switch (type)
            {
                case MoveType.Normal:
                    UpdatePiece(sideToMove, piece, from, to);
                    castling &= (CastlingRights)(castleMask[from] & castleMask[to]);
                    halfMoveClock++;
                    break;

                case MoveType.Capture:
                    RemovePiece(opponent, capture, to);
                    UpdatePiece(sideToMove, piece, from, to);
                    castling &= (CastlingRights)(castleMask[from] & castleMask[to]);
                    halfMoveClock = 0;
                    break;

                case MoveType.Castle:
                    CastlingRookMove rookMove = LookupRookMove(to);
                    if (IsSquareAttackedByColor(from, OpponentColor) ||
                        IsSquareAttackedByColor(rookMove.KingMoveThrough, OpponentColor))
                    {
                        ref BoardState state = ref gameStack.Peek();
                        state.Restore(this);
                        return false;
                    }

                    UpdatePiece(sideToMove, Piece.King, from, to);
                    UpdatePiece(sideToMove, Piece.Rook, rookMove.RookFrom, rookMove.RookTo);
                    castling &= (CastlingRights)(castleMask[from] & castleMask[to]);
                    halfMoveClock++;
                    hasCastled[(int)sideToMove] = true;
                    break;

                case MoveType.EnPassant:
                    int captureOffset = EpOffset(sideToMove);
                    RemovePiece(opponent, capture, to + captureOffset);
                    UpdatePiece(sideToMove, Piece.Pawn, from, to);
                    halfMoveClock = 0;
                    break;

                case MoveType.PawnMove:
                    UpdatePiece(sideToMove, Piece.Pawn, from, to);
                    halfMoveClock = 0;
                    break;

                case MoveType.DblPawnMove:
                    UpdatePiece(sideToMove, Piece.Pawn, from, to);
                    enPassant = to + EpOffset(sideToMove);
                    if (IsEnPassantValid(opponent))
                    {
                        enPassantValidated = enPassant;
                        hash = ZobristHash.HashEnPassant(hash, enPassantValidated);
                    }

                    halfMoveClock = 0;
                    break;

                case MoveType.Promote:
                    RemovePiece(sideToMove, piece, from);
                    AddPiece(sideToMove, promote, to);
                    halfMoveClock = 0;
                    break;

                case MoveType.PromoteCapture:
                    RemovePiece(opponent, capture, to);
                    RemovePiece(sideToMove, piece, from);
                    AddPiece(sideToMove, promote, to);
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
                UnmakeMoveNs();
                return false;
            }

            return true;
        }

        public void UnmakeMove()
        {
            UnmakeMoveNs();
            gameStack.Pop();
        }

        public void UnmakeMoveNs()
        {
            sideToMove = OpponentColor;
            Color opponent = sideToMove.Other();

            BoardState state = gameStack.Peek();
            Move.Unpack(state.Move, out Piece piece, out int from, out int to, out MoveType type, out Piece capture, out Piece promote, out int _);

            switch (type)
            {
                case MoveType.Normal:
                case MoveType.PawnMove:
                case MoveType.DblPawnMove:
                    UpdatePiece(sideToMove, piece, to, from);
                    break;

                case MoveType.Capture:
                    UpdatePiece(sideToMove, piece, to, from);
                    AddPiece(opponent, capture, to);
                    break;

                case MoveType.Castle:
                    CastlingRookMove rookMove = LookupRookMove(to);
                    UpdatePiece(sideToMove, Piece.Rook, rookMove.RookTo, rookMove.RookFrom);
                    UpdatePiece(sideToMove, Piece.King, to, from);
                    hasCastled[(int)sideToMove] = false;
                    break;

                case MoveType.EnPassant:
                    int captureOffset = EpOffset(sideToMove);
                    UpdatePiece(sideToMove, Piece.Pawn, to, from);
                    AddPiece(opponent, capture, to + captureOffset);
                    break;

                case MoveType.Promote:
                    RemovePiece(sideToMove, promote, to);
                    AddPiece(sideToMove, Piece.Pawn, from);
                    break;

                case MoveType.PromoteCapture:
                    RemovePiece(sideToMove, promote, to);
                    AddPiece(sideToMove, Piece.Pawn, from);
                    AddPiece(opponent, capture, to);
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

        public void PushBoardState()
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
                state.Move = Move.NullMove;
                gameStack.Push(ref state);
            }
            else
            {
                throw new IndexOutOfRangeException(
                    @$"Maximum game length exceeded - Hash {hash} BestMove");
            }
        }

        public void PopBoardState()
        {
            gameStack.Pop();
        }

        #endregion

        #region Attacks & Checks

        // SEE where move hasn't been made on the board yet
        public int PreMoveStaticExchangeEval(Color stm, ulong move)
        {
            ulong tempAll = all;
            Span<int> captures = stackalloc int[32];
            captures.Clear();

            int from = Move.GetFrom(move);
            int to = Move.GetTo(move);
            Piece captured = Move.GetCapture(move);
            Piece pc = Move.IsPromote(move) ? Move.GetPromote(move) : Move.GetPiece(move);

            ulong attacksTo = AttacksTo(to);
            captures[0] = captured.Value();

            stm = stm.Other();
            tempAll = BitOps.ResetBit(tempAll, from);

            return SeeImpl(captures, stm, to, pc, attacksTo, tempAll);
        }

        // SEE where move has already been made on the board
        public int PostMoveStaticExchangeEval(Color stm, ulong move)
        {
            Span<int> captures = stackalloc int[32];
            captures.Clear();

            int to = Move.GetTo(move);
            ulong attacksTo = AttacksTo(to);
            Piece pc = Move.GetPiece(move);
            captures[0] = pc.Value();

            stm = stm.Other();
            ulong attacksFrom = 0;
            Piece piece = Piece.Pawn;
            for (; piece <= Piece.King; piece++)
            {
                attacksFrom = Pieces(stm, piece) & attacksTo;
                if (attacksFrom != 0)
                {
                    break;
                }
            }

            if (piece > Piece.King)
            {
                return 0;
            }

            stm = stm.Other();
            ulong tempAll = BitOps.ResetBit(all, BitOps.TzCount(attacksFrom));

            return SeeImpl(captures, stm, to, piece, attacksTo, tempAll);
        }

        private int SeeImpl(Span<int> captures, Color stm, int to, Piece piece, ulong attacks, ulong blockers)
        {
            int cIndex = 1;
            int atkPieceValue = piece.Value();
            ulong dSliders = DiagonalSliders(Color.White) | DiagonalSliders(Color.Black);
            ulong oSliders = OrthogonalSliders(Color.White) | OrthogonalSliders(Color.Black);

            if (piece == Piece.Pawn || piece.IsDiagonalSlider())
            {
                attacks |= GetBishopMoves(to, blockers) & dSliders;
            }
            
            if (piece == Piece.Pawn || piece.IsOrthogonalSlider())
            {
                attacks |= GetRookMoves(to, blockers) & oSliders;
            }

            for (attacks &= blockers; attacks != 0; attacks &= blockers)
            {
                ulong attacksFrom = 0;
                piece = Piece.Pawn;
                for (; piece <= Piece.King; piece++)
                {
                    attacksFrom = Pieces(stm, piece) & attacks;
                    if (attacksFrom != 0)
                    {
                        break;
                    }
                }

                if (piece > Piece.King)
                {
                    break;
                }

                blockers = BitOps.ResetBit(blockers, BitOps.TzCount(attacksFrom));
                if (piece == Piece.Pawn || piece.IsDiagonalSlider())
                {
                    attacks |= GetBishopMoves(to, blockers) & dSliders;
                }

                if (piece == Piece.Pawn || piece.IsOrthogonalSlider())
                {
                    attacks |= GetRookMoves(to, blockers) & oSliders;
                }

                captures[cIndex] = -captures[cIndex - 1] + atkPieceValue;
                atkPieceValue = piece.Value();
                if (captures[cIndex++] - atkPieceValue > 0)
                {
                    break;
                }

                stm = stm.Other();
            }

            for (int n = cIndex - 1; n > 0; n--)
            {
                captures[n - 1] = -Math.Max(-captures[n - 1], captures[n]);
            }

            return captures[0];
        }

        public ulong AttacksTo(int sq)
        {
            ulong attacksTo = (PawnDefends(Color.White, sq) & Pieces(Color.White, Piece.Pawn)) |
                              (PawnDefends(Color.Black, sq) & Pieces(Color.Black, Piece.Pawn));

            attacksTo |= knightMoves[sq] &
                         (Pieces(Color.White, Piece.Knight) | Pieces(Color.Black, Piece.Knight));

            attacksTo |= kingMoves[sq] &
                         (Pieces(Color.White, Piece.King) | Pieces(Color.Black, Piece.King));

            ulong dSliders = DiagonalSliders(Color.White) | DiagonalSliders(Color.Black);
            ulong oSliders = OrthogonalSliders(Color.White) | OrthogonalSliders(Color.Black);

            attacksTo |= GetBishopMoves(sq, all) & dSliders;
            attacksTo |= GetRookMoves(sq, all) & oSliders;

            return attacksTo;
        }

        public ulong AttacksTo(Color byColor, int sq)
        {
            ulong attacksTo = PawnDefends(byColor, sq) & Pieces(byColor, Piece.Pawn);
            attacksTo |= knightMoves[sq] & Pieces(byColor, Piece.Knight);
            attacksTo |= kingMoves[sq] & Pieces(byColor, Piece.King);
            attacksTo |= GetBishopMoves(sq, all) & DiagonalSliders(byColor);
            attacksTo |= GetRookMoves(sq, all) & OrthogonalSliders(byColor);
            return attacksTo;
        }

        public bool HasLegalMoves(MoveList moveList)
        {
            moveList.Clear();
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
        public bool IsDraw()
        {
            return HalfMoveClock >= 100 || GameDrawnByRepetition();
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

            if ((knightMoves[index] & Pieces(color, Piece.Knight)) != 0)
            {
                return true;
            }

            if ((kingMoves[index] & Pieces(color, Piece.King)) != 0)
            {
                return true;
            }

            if ((GetRookMoves(index, all) &
                 (Pieces(color, Piece.Rook) | Pieces(color, Piece.Queen))) != 0)
            {
                return true;
            }

            return (GetBishopMoves(index, all) &
                    (Pieces(color, Piece.Bishop) | Pieces(color, Piece.Queen))) != 0;
        }

        #endregion

        #region Move Generation

        public IEnumerable<ulong> Moves(int ply, KillerMoves killerMoves, History history, MoveList moveList)
        {
            ulong[] bc = badCaptures[ply];
            int bcIndex = 0;

            if (TtTran.TryGetBestMove(hash, out ulong bestMove))
            {
                yield return bestMove;
            }

            moveList.Clear();
            GenerateCaptures(moveList);

            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                ulong move = moveList[n];
                if (Move.Compare(move, bestMove) == 0)
                {
                    continue;
                }

                Piece capture = Move.GetCapture(move);
                Piece piece = Move.GetPiece(move);
                if (bcIndex < 20 && piece.Value() > capture.Value() && PreMoveStaticExchangeEval(SideToMove, move) < 0)
                {
                    bc[bcIndex++] = Move.AdjustScore(move, Constants.BAD_CAPTURE - Constants.CAPTURE_SCORE);
                    continue;
                }
                
                yield return move;
            }

            moveList.Clear();
            GeneratePromotions(moveList, Pieces(sideToMove, Piece.Pawn));
            for (int n = 0; n < moveList.Count; n++)
            {
                moveList.Sort(n);
                if (Move.Compare(moveList[n], bestMove) != 0)
                {
                    yield return moveList[n];
                }
            }

            moveList.Clear();
            GenerateQuietMoves(moveList, history);
            moveList.Remove(bestMove);

            KillerMoves.KillerMove km = killerMoves.GetKillers(ply);
            
            if (moveList.Remove(km.Killer0))
            {
                yield return km.Killer0;
            }

            if (moveList.Remove(km.Killer1))
            {
                yield return km.Killer1;
            }

            if (moveList.Remove(history.CounterMove))
            {
                yield return history.CounterMove;
            }
                
            // now return the bad captures deferred from earlier
            for (int n = 0; n < bcIndex; n++)
            {
                yield return bc[n];
            }

            for (int n = 0; n < moveList.Count; n++)
            {
                yield return moveList.Sort(n);
            }
        }

        public IEnumerable<ulong> QMoves(int ply, int qsPly, MoveList moveList)
        {
            ulong[] bc = badCaptures[ply];
            int bcIndex = 0;

            moveList.Clear();

            if (qsPly < 6)
            {
                GenerateCaptures(moveList);
            }
            else if (Move.IsCapture(LastMove))
            {
                GenerateRecaptures(moveList, Move.GetTo(LastMove));
            }

            for (int n = 0; n < moveList.Count; n++)
            {
                ulong move = moveList.Sort(n);
                Piece piece = Move.GetPiece(move);
                Piece capture = Move.GetCapture(move);
                if (bcIndex < 20 && piece.Value() > capture.Value() && PreMoveStaticExchangeEval(SideToMove, move) < 0)
                {
                    bc[bcIndex++] = Move.AdjustScore(move, Constants.BAD_CAPTURE - Constants.CAPTURE_SCORE);
                    continue;
                }

                yield return move;
            }

            if (qsPly < 2)
            {
                moveList.Clear();
                GeneratePromotions(moveList, Pieces(sideToMove, Piece.Pawn));
                for (int n = 0; n < moveList.Count; n++)
                {
                    yield return moveList.Sort(n);
                }
            }

            for (int n = 0; n < bcIndex; n++)
            {
                yield return bc[n];
            }
        }

        public IEnumerable<ulong> EvasionMoves(MoveList moveList)
        {
            moveList.Clear();
            GenerateEvasions(moveList);

            for (int n = 0; n < moveList.Count; n++)
            {
                yield return moveList.Sort(n);
            }
        }

        public IEnumerable<ulong> EvasionMoves2(int ply, KillerMoves killerMoves, IHistory history, MoveList moveList)
        {
            TtTran.TryGetBestMove(hash, out ulong bestMove);
            moveList.Clear();
            GenerateEvasions(moveList, history);
            moveList.UpdateScores(bestMove, killerMoves, ply);

            for (int n = 0; n < moveList.Count; n++)
            {
                yield return moveList.Sort(n);
            }
        }

        public bool IsValidMove(Square square, int from, int to, out ulong validMove)
        {
            Piece piece = square.Piece;
            validMove = 0;

            if (square.IsEmpty || square.Color != sideToMove || square.Piece == Piece.None)
            {
                return false;
            }

            if (piece == Piece.Pawn)
            {
                ulong bb1, bb2;
                int normalRank = Index.GetRank(Index.NormalizedIndex[(int)sideToMove][to]);
                ulong pawn = (1ul << from) & Pieces(sideToMove, Piece.Pawn);

                if (sideToMove == Color.White)
                {
                    bb1 = BitOps.AndNot(pawn, All >> 8);
                    bb2 = BitOps.AndNot(bb1 & MaskRank(Index.A2), All >> 16);
                }
                else
                {
                    bb1 = BitOps.AndNot(pawn, All << 8);
                    bb2 = BitOps.AndNot(bb1 & MaskRank(Index.A7), All << 16);
                }

                if (bb1 != 0 && to == PawnPlus[(int)sideToMove, from] && normalRank != Coord.MAX_VALUE)
                {
                    validMove = Move.Pack(Piece.Pawn, from, to, MoveType.PawnMove);
                    return true;
                }

                if (bb2 != 0 && to == pawnDouble[(int)sideToMove, from])
                {
                    validMove = Move.Pack(Piece.Pawn, from, to, MoveType.DblPawnMove);
                    return true;
                }
            }
            else
            {
                ulong bb = BitOps.AndNot(GetPieceMoves(piece, from), All) & (1ul << to);
                if (bb != 0)
                {
                    validMove = Move.Pack(piece, from, to);
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

        public bool OneLegalMove(MoveList moveList, out ulong legalMove)
        {
            int legalCount = 0;
            legalMove = 0;
            moveList.Clear();
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
                pawnDefended |= PawnCaptures(other, square);
            }

            ulong excluded = pawnDefended | Units(color);
            for (Piece piece = Piece.Knight; piece <= Piece.Queen; piece++)
            {
                for (ulong pcLoc = Pieces(color, piece); pcLoc != 0; pcLoc = BitOps.ResetLsb(pcLoc))
                {
                    int from = BitOps.TzCount(pcLoc);
                    ulong moves = GetPieceMoves(piece, from);
                    ulong mobMoves = moves & ~excluded;
                    mobility += (short)BitOps.PopCount(mobMoves);
                }
            }

            return mobility;
        }

        public void GetPieceMobility(Color color, Span<short> mobility, Span<short> kingAttacks, Span<short> centerControl)
        {
            Color other = (Color)((int)color ^ 1);
            ulong pawnDefended = 0ul;
            int kingIndex = BitOps.TzCount(Pieces(other, Piece.King));
            mobility.Clear();
            kingAttacks.Clear();
            centerControl.Clear();

            for (ulong pawns = Pieces(other, Piece.Pawn); pawns != 0ul; pawns = BitOps.ResetLsb(pawns))
            {
                int square = BitOps.TzCount(pawns);
                pawnDefended |= PawnCaptures(other, square);
            }

            for (ulong pawns = Pieces(color, Piece.Pawn); pawns != 0ul; pawns = BitOps.ResetLsb(pawns))
            {
                int square = BitOps.TzCount(pawns);
                ulong captures = PawnCaptures(color, square);
                centerControl[0] += (short)BitOps.PopCount(captures & Evaluation.D0_CENTER_CONTROL_MASK);
                centerControl[1] += (short)BitOps.PopCount(captures & Evaluation.D1_CENTER_CONTROL_MASK);
            }

            ulong excluded = pawnDefended | Units(color);
            ulong d0 = Evaluation.KingProximity[0, kingIndex];
            ulong d1 = Evaluation.KingProximity[1, kingIndex];
            ulong d2 = Evaluation.KingProximity[2, kingIndex];
            for (Piece piece = Piece.Knight; piece <= Piece.Queen; piece++)
            {
                for (ulong pcLoc = Pieces(color, piece); pcLoc != 0; pcLoc = BitOps.ResetLsb(pcLoc))
                {
                    int from = BitOps.TzCount(pcLoc);
                    ulong moves = GetPieceMoves(piece, from);
                    ulong mobMoves = moves & ~excluded;
                    ulong atkMoves = moves & ~pawnDefended;
                    mobility[(int)piece] += (short)BitOps.PopCount(mobMoves);
                    kingAttacks[0] += (short)BitOps.PopCount(atkMoves & d0);
                    kingAttacks[1] += (short)BitOps.PopCount(atkMoves & d1);
                    kingAttacks[2] += (short)BitOps.PopCount(atkMoves & d2);
                    centerControl[0] += (short)BitOps.PopCount(moves & Evaluation.D0_CENTER_CONTROL_MASK);
                    centerControl[1] += (short)BitOps.PopCount(moves & Evaluation.D1_CENTER_CONTROL_MASK);
                }
            }
        }

        public void GenerateMoves(MoveList list, IHistory? history = null)
        {
            IHistory hist = history ?? fakeHistory;
            ulong pawns = Pieces(sideToMove, Piece.Pawn);
            GenerateEnPassant(list, pawns);
            GenerateCastling(list, hist);
            GeneratePawnMoves(list, hist, pawns);

            for (Piece piece = Piece.Knight; piece <= Piece.King; ++piece)
            {
                for (ulong bb1 = Pieces(sideToMove, piece); bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int from = BitOps.TzCount(bb1);
                    ulong bb2 = GetPieceMoves(piece, from);

                    for (ulong bb3 = bb2 & Units(OpponentColor); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        Piece capture = board[to].Piece;
                        list.Add(piece, from, to, MoveType.Capture, capture, score: CaptureScore(capture, piece));
                    }
                    for (ulong bb3 = BitOps.AndNot(bb2, All); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        list.Add(piece, from, to, score: hist[piece, to]);
                    }
                }
            }
        }

        public void GeneratePieceMoves(MoveList list, IHistory hist, Piece piece, int from)
        {
            ulong bb2 = GetPieceMoves(piece, from);

            for (ulong bb3 = bb2 & Units(OpponentColor); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
            {
                int to = BitOps.TzCount(bb3);
                Piece capture = board[to].Piece;
                list.Add(piece, from, to, MoveType.Capture, capture, score: CaptureScore(capture, piece));
            }
            for (ulong bb3 = BitOps.AndNot(bb2, All); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
            {
                int to = BitOps.TzCount(bb3);
                list.Add(piece, from, to, score: hist[piece, to]);
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
            GenerateQuietPawnMoves(list, history, Pieces(sideToMove, Piece.Pawn));

            for (Piece piece = Piece.Knight; piece <= Piece.King; ++piece)
            {
                for (ulong bb1 = Pieces(sideToMove, piece); bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int from = BitOps.TzCount(bb1);
                    ulong bb2 = GetPieceMoves(piece, from);

                    for (ulong bb3 = BitOps.AndNot(bb2, All); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        Util.Assert(Index.IsValid(to), $"'to' out of range: {to}");
                        list.Add(piece, from, to, score: history[piece, to]);
                    }
                }
            }
        }

        public void GenerateCaptures(MoveList list)
        {
            ulong pawns = Pieces(sideToMove, Piece.Pawn);
            GenerateEnPassant(list, pawns);
            GeneratePawnCaptures(list, pawns);

            for (Piece piece = Piece.Knight; piece <= Piece.King; ++piece)
            {
                for (ulong bb1 = Pieces(sideToMove, piece); bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int from = BitOps.TzCount(bb1);
                    ulong bb2 = GetPieceMoves(piece, from);

                    for (ulong bb3 = bb2 & Units(OpponentColor); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        Piece capture = board[to].Piece;
                        list.Add(piece, from, to, MoveType.Capture, capture, score: CaptureScore(capture, piece));
                    }
                }
            }
        }

        public void GeneratePromotions(MoveList list, ulong pawns)
        {
            // only concerned about pawns on the 7th rank (or 2nd for black)
            ulong pawnsToPromote = pawns & MaskRank(Index.NormalizedIndex[(int)sideToMove][Index.A7]);

            ulong bb = sideToMove == Color.White
                ? BitOps.AndNot(pawnsToPromote, All >> 8)
                : BitOps.AndNot(pawnsToPromote, All << 8);

            for (; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int from = BitOps.TzCount(bb);
                int to = PawnPlus[(int)sideToMove, from];
                int score = Constants.PROMOTE_SCORE + Piece.Knight.Value();
                list.Add(Piece.Pawn, from, to, MoveType.Promote, promote: Piece.Knight, score: score);
                score = Constants.PROMOTE_SCORE + Piece.Queen.Value();
                list.Add(Piece.Pawn, from, to, MoveType.Promote, promote: Piece.Queen, score: score);
            }
        }

        public void GenerateEnPassant(MoveList list, ulong pawns, ulong validSquares = ALL_SQUARES_MASK)
        {
            if (enPassantValidated != Index.NONE)
            {
                ulong epMask = 1ul << enPassantValidated;
                epMask = sideToMove == Color.White ? epMask >> 8 : epMask << 8;
                if ((epMask & validSquares) != 0)
                {
                    ulong bb = PawnDefends(sideToMove, enPassantValidated) & pawns;
                    for (; bb != 0; bb = BitOps.ResetLsb(bb))
                    {
                        int from = BitOps.TzCount(bb);
                        int captIndex = enPassantValidated + EpOffset(sideToMove);
                        Piece capture = board[captIndex].Piece;
                        list.Add(Piece.Pawn, from, enPassantValidated, MoveType.EnPassant, capture: capture,
                            score: CaptureScore(capture, Piece.Pawn));
                    }
                }
            }
        }

        public void GenerateCastling(MoveList list, IHistory hist)
        {
            if (sideToMove == Color.White)
            {
                if ((castling & CastlingRights.WhiteKingSide) != 0 && (WHITE_KS_CLEAR_MASK & All) == 0)
                {
                    list.Add(Piece.King, Index.E1, Index.G1, MoveType.Castle, score: hist[Piece.King, Index.G1]);
                }

                if ((castling & CastlingRights.WhiteQueenSide) != 0 && (WHITE_QS_CLEAR_MASK & All) == 0)
                {
                    list.Add(Piece.King, Index.E1, Index.C1, MoveType.Castle, score: hist[Piece.King, Index.C1]);
                }
            }
            else
            {
                if ((castling & CastlingRights.BlackKingSide) != 0 && (BLACK_KS_CLEAR_MASK & All) == 0)
                {
                    list.Add(Piece.King, Index.E8, Index.G8, MoveType.Castle, score: hist[Piece.King, Index.G8]);
                }

                if ((castling & CastlingRights.BlackQueenSide) != 0 && (BLACK_QS_CLEAR_MASK & All) == 0)
                {
                    list.Add(Piece.King, Index.E8, Index.C8, MoveType.Castle, score: hist[Piece.King, Index.C8]);
                }
            }
        }

        public void GenerateQuietPawnMoves(MoveList list, IHistory hist, ulong pawns)
        {
            ulong bb1, bb2;
            int from, to;

            if (sideToMove == Color.White)
            {
                bb1 = BitOps.AndNot(BitOps.AndNot(pawns, MaskRank(Index.A7)), All >> 8);
                bb2 = BitOps.AndNot(bb1 & MaskRank(Index.A2), All >> 16);
            }
            else
            {
                bb1 = BitOps.AndNot(BitOps.AndNot(pawns, MaskRank(Index.A2)), All << 8);
                bb2 = BitOps.AndNot(bb1 & MaskRank(Index.A7), All << 16);
            }

            for (; bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
            {
                from = BitOps.TzCount(bb1);
                to = PawnPlus[(int)sideToMove, from];
                AddPawnMove(list, from, to, score: hist[Piece.Pawn, to]);
            }

            for (; bb2 != 0; bb2 = BitOps.ResetLsb(bb2))
            {
                from = BitOps.TzCount(bb2);
                to = pawnDouble[(int)sideToMove, from];
                list.Add(Piece.Pawn, from, to, MoveType.DblPawnMove, score: hist[Piece.Pawn, to]);
            }
        }

        public void GeneratePawnMoves(MoveList list, IHistory hist, ulong pawns)
        {
            ulong bb1, bb2, bb3, bb4;
            int from, to;
            if (sideToMove == Color.White)
            {
                bb1 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFile(7)) >> 7);
                bb2 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFile(0)) >> 9);
                bb3 = BitOps.AndNot(pawns, All >> 8);
                bb4 = BitOps.AndNot(bb3 & MaskRank(Index.A2), All >> 16);
            }
            else
            {
                bb1 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFile(7)) << 9);
                bb2 = pawns & (BitOps.AndNot(Units(OpponentColor), MaskFile(0)) << 7);
                bb3 = BitOps.AndNot(pawns, All << 8);
                bb4 = BitOps.AndNot(bb3 & MaskRank(Index.A7), All << 16);
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
                to = PawnPlus[(int)sideToMove, from];
                AddPawnMove(list, from, to, MoveType.PawnMove, score: hist[Piece.Pawn, to]);
            }

            for (; bb4 != 0; bb4 = BitOps.ResetLsb(bb4))
            {
                from = BitOps.TzCount(bb4);
                to = pawnDouble[(int)sideToMove, from];
                list.Add(Piece.Pawn, from, to, MoveType.DblPawnMove, score: hist[Piece.Pawn, to]);
            }
        }

        public void GeneratePawnMoves(MoveList list, IHistory hist, ulong pawns, ulong validSquares)
        {
            ulong bb1, bb2, bb3, bb4;
            int from, to;
            ulong validEnemies = Units(OpponentColor) & validSquares;

            if (sideToMove == Color.White)
            {
                bb1 = pawns & (BitOps.AndNot(validEnemies, MaskFile(7)) >> 7);
                bb2 = pawns & (BitOps.AndNot(validEnemies, MaskFile(0)) >> 9);
                bb3 = BitOps.AndNot(pawns, All >> 8);
                bb4 = BitOps.AndNot(bb3 & MaskRank(Index.A2), All >> 16);
            }
            else
            {
                bb1 = pawns & (BitOps.AndNot(validEnemies, MaskFile(7)) << 9);
                bb2 = pawns & (BitOps.AndNot(validEnemies, MaskFile(0)) << 7);
                bb3 = BitOps.AndNot(pawns, All << 8);
                bb4 = BitOps.AndNot(bb3 & MaskRank(Index.A7), All << 16);
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
                to = PawnPlus[(int)sideToMove, from];
                if (BitOps.GetBit(validSquares, to) != 0)
                {
                    AddPawnMove(list, from, to, score: hist[Piece.Pawn, to]);
                }
            }

            for (; bb4 != 0; bb4 = BitOps.ResetLsb(bb4))
            {
                from = BitOps.TzCount(bb4);
                to = pawnDouble[(int)sideToMove, from];
                if (BitOps.GetBit(validSquares, to) != 0)
                {
                    list.Add(Piece.Pawn, from, to, MoveType.DblPawnMove, score: hist[Piece.Pawn, to]);
                }
            }
        }

        public void GeneratePawnCaptures(MoveList list, ulong pawns, ulong validSquares = ALL_SQUARES_MASK)
        {
            ulong bb1, bb2;
            int from, to;

            ulong enemies = Units(OpponentColor) & validSquares;
            if (sideToMove == Color.White)
            {
                bb1 = pawns & (BitOps.AndNot(enemies, MaskFile(7)) >> 7);
                bb2 = pawns & (BitOps.AndNot(enemies, MaskFile(0)) >> 9);
            }
            else
            {
                bb1 = pawns & (BitOps.AndNot(enemies, MaskFile(7)) << 9);
                bb2 = pawns & (BitOps.AndNot(enemies, MaskFile(0)) << 7);
            }

            for (; bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
            {
                from = BitOps.TzCount(bb1);
                to = PawnLeft(sideToMove, from);
                AddSearchedPawnMove(list, from, to, MoveType.Capture, (Piece)board[to], score: CaptureScore(from, to));
            }

            for (; bb2 != 0; bb2 = BitOps.ResetLsb(bb2))
            {
                from = BitOps.TzCount(bb2);
                to = PawnRight(sideToMove, from);
                AddSearchedPawnMove(list, from, to, MoveType.Capture, (Piece)board[to], score: CaptureScore(from, to));
            }
        }



        /// <summary>
        /// Checks the mask.
        /// </summary>
        /// <remarks>
        /// Generate a mask that includes the positions of all
        /// checking pieces as well as the squares between a checking slider 
        /// and the attacked king. This mask will be used to only generate
        /// check-evasion moves.
        ///
        /// This method also returns the check count which indicates how many
        /// pieces simultaneously attack the king. If more than one, the only
        /// way to evade the check is to move the king (if legally possible).
        /// </remarks>
        /// <param name="square">The square of the king in check</param>
        /// <param name="checkCount">The check count.</param>
        /// <returns>ulong.</returns>
        private ulong CheckMask(int square, out int checkCount)
        {
            Color opponent = sideToMove.Other();
            ulong checkMask = PawnDefends(opponent, square) & Pieces(opponent, Piece.Pawn);
            checkMask |= knightMoves[square] & Pieces(opponent, Piece.Knight);
            checkCount = BitOps.PopCount(checkMask);


            if (checkCount > 1)
            {
                return 0;
            }

            Ray ray = Vectors[square];

            ulong bbRayBlockers = ray.NorthEast & all;
            if (bbRayBlockers != 0 && checkCount < 2 && ((1ul << BitOps.TzCount(bbRayBlockers)) & DiagonalSliders(opponent)) != 0)
            {
                checkCount++;
                checkMask |= BitOps.AndNot(ray.NorthEast, Vectors[BitOps.TzCount(bbRayBlockers)].NorthEast);

                if (checkCount > 1)
                {
                    return 0;
                }
            }

            bbRayBlockers = ray.NorthWest & all;
            if (bbRayBlockers != 0 && checkCount < 2 && ((1ul << BitOps.TzCount(bbRayBlockers)) & DiagonalSliders(opponent)) != 0)
            {
                checkCount++;
                checkMask |= BitOps.AndNot(ray.NorthWest, Vectors[BitOps.TzCount(bbRayBlockers)].NorthWest);

                if (checkCount > 1)
                {
                    return 0;
                }
            }

            bbRayBlockers = ray.SouthEast & all;
            if (bbRayBlockers != 0 && checkCount < 2 && ((1ul << (63 - BitOps.LzCount(bbRayBlockers))) & DiagonalSliders(opponent)) != 0)
            {
                checkCount++;
                checkMask |= BitOps.AndNot(ray.SouthEast, RevVectors[BitOps.LzCount(bbRayBlockers)].SouthEast);

                if (checkCount > 1)
                {
                    return 0;
                }
            }

            bbRayBlockers = ray.SouthWest & all;
            if (bbRayBlockers != 0 && checkCount < 2 && ((1ul << (63 - BitOps.LzCount(bbRayBlockers))) & DiagonalSliders(opponent)) != 0)
            {
                checkCount++;
                checkMask |= BitOps.AndNot(ray.SouthWest, RevVectors[BitOps.LzCount(bbRayBlockers)].SouthWest);

                if (checkCount > 1)
                {
                    return 0;
                }
            }

            bbRayBlockers = ray.North & all;
            if (bbRayBlockers != 0 && checkCount < 2 && ((1ul << BitOps.TzCount(bbRayBlockers)) & OrthogonalSliders(opponent)) != 0)
            {
                checkCount++;
                checkMask |= BitOps.AndNot(ray.North, Vectors[BitOps.TzCount(bbRayBlockers)].North);

                if (checkCount > 1)
                {
                    return 0;
                }
            }

            bbRayBlockers = ray.East & all;
            if (bbRayBlockers != 0 && checkCount < 2 && ((1ul << BitOps.TzCount(bbRayBlockers)) & OrthogonalSliders(opponent)) != 0)
            {
                checkCount++;
                checkMask |= BitOps.AndNot(ray.East, Vectors[BitOps.TzCount(bbRayBlockers)].East);

                if (checkCount > 1)
                {
                    return 0;
                }
            }

            bbRayBlockers = ray.South & all;
            if (bbRayBlockers != 0 && checkCount < 2 && ((1ul << (63 - BitOps.LzCount(bbRayBlockers))) & OrthogonalSliders(opponent)) != 0)
            {
                checkCount++;
                checkMask |= BitOps.AndNot(ray.South, RevVectors[BitOps.LzCount(bbRayBlockers)].South);

                if (checkCount > 1)
                {
                    return 0;
                }
            }

            bbRayBlockers = ray.West & all;
            if (bbRayBlockers != 0 && checkCount < 2 && ((1ul << (63 - BitOps.LzCount(bbRayBlockers))) & OrthogonalSliders(opponent)) != 0)
            {
                checkCount++;
                checkMask |= BitOps.AndNot(ray.West, RevVectors[BitOps.LzCount(bbRayBlockers)].West);

                if (checkCount > 1)
                {
                    return 0;
                }
            }

            return checkMask;
        }

        public void GenerateEvasions(MoveList moveList, IHistory? history = null)
        {
            Color opponent = sideToMove.Other();
            int kingIndex = BitOps.TzCount(Pieces(sideToMove, Piece.King));
            ulong attacksFrom = CheckMask(kingIndex, out int checkCount);
            IHistory hist = history ?? fakeHistory;

            for (ulong bb = kingMoves[kingIndex] & Units(opponent); bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int to = BitOps.TzCount(bb);
                Piece capture = board[to].Piece;
                moveList.Add(Piece.King, kingIndex, to, MoveType.Capture, capture, score: CaptureScore(capture, Piece.King));
            }

            for (ulong bb = kingMoves[kingIndex] & ~all; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int to = BitOps.TzCount(bb);
                moveList.Add(Piece.King, kingIndex, to, score: hist[Piece.King, to]);
            }

            if (checkCount > 1)
            {
                return;
            }

            ulong validEnemies = Units(opponent) & attacksFrom;
            ulong pawns = Pieces(sideToMove, Piece.Pawn);
            GenerateEnPassant(moveList, pawns, attacksFrom);
            GeneratePawnMoves(moveList, hist, pawns, attacksFrom);

            for (Piece piece = Piece.Knight; piece <= Piece.Queen; ++piece)
            {
                for (ulong bb1 = Pieces(sideToMove, piece); bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int from = BitOps.TzCount(bb1);
                    ulong bb2 = GetPieceMoves(piece, from);

                    for (ulong bb3 = bb2 & validEnemies; bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        Piece capture = board[to].Piece;
                        ulong move = Move.Pack(piece, from, to, MoveType.Capture, capture, score: CaptureScore(capture, piece));

                        if (piece.Value() > capture.Value() && PreMoveStaticExchangeEval(SideToMove, move) < 0)
                        {
                            move = Move.AdjustScore(move, Constants.BAD_CAPTURE - Constants.CAPTURE_SCORE);
                        }

                        moveList.Add(move);
                    }

                    for (ulong bb3 = BitOps.AndNot(bb2, All); bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        if (BitOps.GetBit(attacksFrom, to) != 0)
                        {
                            moveList.Add(piece, from, to, score: hist[piece, to]);
                        }
                    }
                }
            }
        }

        public void GenerateRecaptures(MoveList moveList, int captureSquare)
        {
            Color opponent = sideToMove.Other();
            ulong captureMask = (1ul << captureSquare) & Units(opponent);
            ulong pawns = Pieces(sideToMove, Piece.Pawn);
            GeneratePawnCaptures(moveList, pawns, captureMask);

            for (Piece piece = Piece.Knight; piece <= Piece.King; ++piece)
            {
                for (ulong bb1 = Pieces(sideToMove, piece); bb1 != 0; bb1 = BitOps.ResetLsb(bb1))
                {
                    int from = BitOps.TzCount(bb1);
                    ulong bb2 = GetPieceMoves(piece, from);

                    for (ulong bb3 = bb2 & captureMask; bb3 != 0; bb3 = BitOps.ResetLsb(bb3))
                    {
                        int to = BitOps.TzCount(bb3);
                        Piece capture = board[to].Piece;
                        moveList.Add(piece, from, to, MoveType.Capture, capture, score: CaptureScore(capture, piece));
                    }
                }
            }
        }

        public static void AddPawnMove(MoveList list, int from, int to, MoveType flags = MoveType.PawnMove, Piece capture = Piece.None, int score = 0)
        {
            int rank = Index.GetRank(to);
            if (rank == Coord.MIN_VALUE || rank == Coord.MAX_VALUE)
            {
                flags = flags == MoveType.Capture ? MoveType.PromoteCapture : MoveType.Promote;
                for (Piece p = Piece.Knight; p <= Piece.Queen; ++p)
                {
                    score = flags == MoveType.PromoteCapture ? CaptureScore(capture, Piece.Pawn, p) : Constants.PROMOTE_SCORE + p.Value();
                    list.Add(Piece.Pawn, from, to, flags, capture, p, score);
                }
            }
            else
            {
                list.Add(Piece.Pawn, from, to, flags, capture: capture, score: score);
            }
        }

        public static void AddSearchedPawnMove(MoveList list, int from, int to, MoveType flags = MoveType.PawnMove, Piece capture = Piece.None, int score = 0)
        {
            int rank = Index.GetRank(to);
            if (rank == Coord.MIN_VALUE || rank == Coord.MAX_VALUE)
            {
                flags = flags == MoveType.Capture ? MoveType.PromoteCapture : MoveType.Promote;
                score = flags == MoveType.PromoteCapture ?
                    CaptureScore(capture, Piece.Pawn, Piece.Knight) :
                    Constants.PROMOTE_SCORE + Piece.Knight.Value();
                list.Add(Piece.Pawn, from, to, flags, capture, Piece.Knight, score);

                score = flags == MoveType.PromoteCapture ? 
                    CaptureScore(capture, Piece.Pawn, Piece.Queen) : 
                    Constants.PROMOTE_SCORE + Piece.Queen.Value();
                list.Add(Piece.Pawn, from, to, flags, capture, Piece.Queen, score);
            }
            else
            {
                list.Add(Piece.Pawn, from, to, flags, capture: capture, score: score);
            }
        }

        public static ulong GetPieceMoves(Piece piece, int from, ulong occupied)
        {
            return piece switch
            {
                Piece.Knight => knightMoves[from],
                Piece.Bishop => GetBishopMoves(from, occupied),
                Piece.Rook => GetRookMoves(from, occupied),
                Piece.Queen => GetQueenMoves(from, occupied),
                Piece.King => kingMoves[from],
                _ => 0ul
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetPieceMoves(Piece piece, int from)
        {
            return GetPieceMoves(piece, from, All);
        }

        // traditional diagonal slider move resolution
        public static ulong GetBishopAttacks(int from, ulong blockers)
        {
            Ray ray = Vectors[from];
            ulong bb = BitOps.AndNot(ray.NorthEast, Vectors[BitOps.TzCount(ray.NorthEast & blockers)].NorthEast) |
                       BitOps.AndNot(ray.NorthWest, Vectors[BitOps.TzCount(ray.NorthWest & blockers)].NorthWest) |
                       BitOps.AndNot(ray.SouthEast, RevVectors[BitOps.LzCount(ray.SouthEast & blockers)].SouthEast) |
                       BitOps.AndNot(ray.SouthWest, RevVectors[BitOps.LzCount(ray.SouthWest & blockers)].SouthWest);
            return bb;
        }

        // traditional orthogonal slider move resolution
        public static ulong GetRookAttacks(int from, ulong blockers)
        {
            Ray ray = Vectors[from];
            ulong bb = BitOps.AndNot(ray.North, Vectors[BitOps.TzCount(ray.North & blockers)].North) |
                       BitOps.AndNot(ray.East, Vectors[BitOps.TzCount(ray.East & blockers)].East) |
                       BitOps.AndNot(ray.South, RevVectors[BitOps.LzCount(ray.South & blockers)].South) |
                       BitOps.AndNot(ray.West, RevVectors[BitOps.LzCount(ray.West & blockers)].West);

            return bb;
        }

        public static ulong GetQueenAttacks(int from, ulong blockers)
        {
            Ray ray = Vectors[from];
            ulong bb = BitOps.AndNot(ray.NorthEast, Vectors[BitOps.TzCount(ray.NorthEast & blockers)].NorthEast) |
                       BitOps.AndNot(ray.NorthWest, Vectors[BitOps.TzCount(ray.NorthWest & blockers)].NorthWest) |
                       BitOps.AndNot(ray.SouthEast, RevVectors[BitOps.LzCount(ray.SouthEast & blockers)].SouthEast) |
                       BitOps.AndNot(ray.SouthWest, RevVectors[BitOps.LzCount(ray.SouthWest & blockers)].SouthWest) |
                       BitOps.AndNot(ray.North, Vectors[BitOps.TzCount(ray.North & blockers)].North) |
                       BitOps.AndNot(ray.East, Vectors[BitOps.TzCount(ray.East & blockers)].East) |
                       BitOps.AndNot(ray.South, RevVectors[BitOps.LzCount(ray.South & blockers)].South) |
                       BitOps.AndNot(ray.West, RevVectors[BitOps.LzCount(ray.West & blockers)].West);
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

                enPassantValidated = Index.NONE;
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
#if DEBUG
            if (!board[square].IsEmpty)
            {
                Debugger.Break();
            }
#endif
            if (piece == Piece.Pawn)
            {
                pawnHash = ZobristHash.HashPiece(pawnHash, color, piece, square);
            }
            hash = ZobristHash.HashPiece(hash, color, piece, square);
            board[square] = Square.Create(color, piece);
            pieces[(int)color, (int)piece] = BitOps.SetBit(pieces[(int)color, (int)piece], square);
            units[(int)color] = BitOps.SetBit(units[(int)color], square);
            all = BitOps.SetBit(all, square);
            material[(int)color] += Evaluation.CanonicalPieceValues(piece);
            opMaterial[(int)color] += Evaluation.OpeningPieceValues(piece);
            egMaterial[(int)color] += Evaluation.EndGamePieceValues(piece);
            opPcSquare[(int)color, 0] +=
                Evaluation.OpeningPieceSquareTable(piece, KingPlacement.KK, Index.NormalizedIndex[(int)color][square]);
            egPcSquare[(int)color, 0] +=
                Evaluation.EndGamePieceSquareTable(piece, KingPlacement.KK, Index.NormalizedIndex[(int)color][square]);
            opPcSquare[(int)color, 1] +=
                Evaluation.OpeningPieceSquareTable(piece, KingPlacement.KQ, Index.NormalizedIndex[(int)color][square]);
            egPcSquare[(int)color, 1] +=
                Evaluation.EndGamePieceSquareTable(piece, KingPlacement.KQ, Index.NormalizedIndex[(int)color][square]);
            opPcSquare[(int)color, 2] +=
                Evaluation.OpeningPieceSquareTable(piece, KingPlacement.QK, Index.NormalizedIndex[(int)color][square]);
            egPcSquare[(int)color, 2] +=
                Evaluation.EndGamePieceSquareTable(piece, KingPlacement.QK, Index.NormalizedIndex[(int)color][square]);
            opPcSquare[(int)color, 3] +=
                Evaluation.OpeningPieceSquareTable(piece, KingPlacement.QQ, Index.NormalizedIndex[(int)color][square]);
            egPcSquare[(int)color, 3] +=
                Evaluation.EndGamePieceSquareTable(piece, KingPlacement.QQ, Index.NormalizedIndex[(int)color][square]);
        }

        public void RemovePiece(Color color, Piece piece, int square)
        {
#if DEBUG
            if (board[square].IsEmpty || board[square].Color != color || board[square].Piece != piece)
            {
                Debugger.Break();
            }
#endif
            if (piece == Piece.Pawn)
            {
                pawnHash = ZobristHash.HashPiece(pawnHash, color, piece, square);
            }
            hash = ZobristHash.HashPiece(hash, color, piece, square);
            board[square] = Square.Empty;
            pieces[(int)color, (int)piece] = BitOps.ResetBit(pieces[(int)color, (int)piece], square);
            units[(int)color] = BitOps.ResetBit(Units(color), square);
            all = BitOps.ResetBit(all, square);
            material[(int)color] -= Evaluation.CanonicalPieceValues(piece);
            opMaterial[(int)color] -= Evaluation.OpeningPieceValues(piece);
            egMaterial[(int)color] -= Evaluation.EndGamePieceValues(piece);
            opPcSquare[(int)color, 0] -=
                Evaluation.OpeningPieceSquareTable(piece, KingPlacement.KK, Index.NormalizedIndex[(int)color][square]);
            egPcSquare[(int)color, 0] -=
                Evaluation.EndGamePieceSquareTable(piece, KingPlacement.KK, Index.NormalizedIndex[(int)color][square]);
            opPcSquare[(int)color, 1] -=
                Evaluation.OpeningPieceSquareTable(piece, KingPlacement.KQ, Index.NormalizedIndex[(int)color][square]);
            egPcSquare[(int)color, 1] -=
                Evaluation.EndGamePieceSquareTable(piece, KingPlacement.KQ, Index.NormalizedIndex[(int)color][square]);
            opPcSquare[(int)color, 2] -=
                Evaluation.OpeningPieceSquareTable(piece, KingPlacement.QK, Index.NormalizedIndex[(int)color][square]);
            egPcSquare[(int)color, 2] -=
                Evaluation.EndGamePieceSquareTable(piece, KingPlacement.QK, Index.NormalizedIndex[(int)color][square]);
            opPcSquare[(int)color, 3] -=
                Evaluation.OpeningPieceSquareTable(piece, KingPlacement.QQ, Index.NormalizedIndex[(int)color][square]);
            egPcSquare[(int)color, 3] -=
                Evaluation.EndGamePieceSquareTable(piece, KingPlacement.QQ, Index.NormalizedIndex[(int)color][square]);
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
            public short this[Piece piece, int to] => 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CaptureScore(int from, int to)
        {
            return CaptureScore(board[to].Piece, board[from].Piece);
        }

        public static ulong PawnDefends(Color color, int square)
        {
            return pawnDefends[(int)color, square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PawnCaptures(Color color, int square)
        {
            return pawnCaptures[(int)color, square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PawnLeft(Color color, int square)
        {
            return pawnLeft[(int)color, square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PawnRight(Color color, int square)
        {
            return pawnRight[(int)color, square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CastlingRookMove LookupRookMove(int kingTo)
        {
            return kingTo switch
            {
                Index.C1 => CastlingRookMoves[0],
                Index.G1 => CastlingRookMoves[1],
                Index.C8 => CastlingRookMoves[2],
                Index.G8 => CastlingRookMoves[3],
                _ => throw new ArgumentException("Invalid king target/to square invalid.", nameof(kingTo)),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong MaskFile(int sq)
        {
            Util.Assert(Chess.Index.IsValid(sq));
            return 0x0101010101010101ul << Index.GetFile(sq);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong MaskRank(int sq)
        {
            Util.Assert(Index.IsValid(sq));
            return 0x00000000000000fful << (Index.GetRank(sq) << 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EpOffset(Color color)
        {
            return 8 * (-1 + ((int)color << 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CaptureScore(Piece captured, Piece attacker, Piece promote = Piece.None)
        {
            return Constants.CAPTURE_SCORE + promote.Value() + ((int)captured << 3) + (Constants.MAX_PIECES - (int)attacker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetBishopMoves(int sq, ulong blockers)
        {
            if (IsPextSupported)
            {
                return GetBishopAttacksPext(sq, blockers);
            }

            return GetBishopAttacksFancy(sq, blockers);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetRookMoves(int sq, ulong blockers)
        {
            if (IsPextSupported)
            {
                return GetRookAttacksPext(sq, blockers);
            }

            return GetRookAttacksFancy(sq, blockers);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetQueenMoves(int sq, ulong blockers)
        {
            if (IsPextSupported)
            {
                return GetQueenAttacksPext(sq, blockers);
            }

            return GetQueenAttacksFancy(sq, blockers);
        }

        private static ulong GetBestMove(ulong[] moves, int length, int n)
        {
            int largest = -1;
            int score = -1;
            for (int i = n; i < length; ++i)
            {
                short mvScore = Move.GetScore(moves[i]);
                if (mvScore > score)
                {
                    largest = i;
                    score = mvScore;
                }
            }

            if (largest > n)
            {
                (moves[n],  moves[largest]) = (moves[largest], moves[n]);
            }

            return moves[n];
        }

        private static readonly FakeHistory fakeHistory = new();

        private static readonly UnsafeArray<int> castleMask = new (Constants.MAX_SQUARES)
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
            public readonly ulong ClearMask;

            public CastlingRookMove(int kingFrom, int kingTo, int kingMoveThrough, int rookFrom, int rookTo, CastlingRights mask, ulong clearMask)
            {
                KingFrom = kingFrom;
                KingTo = kingTo;
                KingMoveThrough = kingMoveThrough;
                RookFrom = rookFrom;
                RookTo = rookTo;
                CastlingMask = mask;
                ClearMask = clearMask;
            }
        }

        private readonly ObjectPool<MoveList> moveListPool = new(4);
        private readonly ulong[][] badCaptures = Mem.Allocate2D<ulong>(Constants.MAX_PLY, 20);

        public static readonly CastlingRookMove[] CastlingRookMoves =
        {
            new(Index.E1, Index.C1, Index.D1, Index.A1, Index.D1, CastlingRights.WhiteQueenSide, WHITE_QS_CLEAR_MASK),
            new(Index.E1, Index.G1, Index.F1, Index.H1, Index.F1, CastlingRights.WhiteKingSide, WHITE_KS_CLEAR_MASK),
            new(Index.E8, Index.C8, Index.D8, Index.A8, Index.D8, CastlingRights.BlackQueenSide, BLACK_QS_CLEAR_MASK),
            new(Index.E8, Index.G8, Index.F8, Index.H8, Index.F8, CastlingRights.BlackKingSide, BLACK_KS_CLEAR_MASK)
        };

        private static readonly UnsafeArray2D<ulong> pawnDefends = new (Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region pawnDefends data

            // squares defended by white pawns
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
            0x0028000000000000ul, 0x0050000000000000ul, 0x00A0000000000000ul, 0x0040000000000000ul,

            // squares defended by black pawns
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
            #endregion
        };

        private static readonly UnsafeArray2D<ulong> pawnCaptures = new (Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region pawnCaptures data

            // squares attacked by white pawns
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
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,

            // squares attacked by black pawns
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

            #endregion pawnCaptures data
        };

        private static readonly UnsafeArray<ulong> knightMoves = new(Constants.MAX_SQUARES)
        {
            #region knightMoves data

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
            0x0044280000000000ul, 0x0088500000000000ul, 0x0010A00000000000ul, 0x0020400000000000ul

            #endregion
        };

        private static readonly UnsafeArray<ulong> kingMoves = new(Constants.MAX_SQUARES)
        {
            #region kingMoves data

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
            0x2838000000000000ul, 0x5070000000000000ul, 0xA0E0000000000000ul, 0x40C0000000000000ul

            #endregion
        };

        public static readonly Ray[] Vectors =
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

        public static readonly Ray[] RevVectors = new Ray[Constants.MAX_SQUARES + 1];

        private static readonly UnsafeArray2D<sbyte> pawnLeft = new(Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region pawnLeft data
            -1,  8,  9, 10, 11, 12, 13, 14,
            -1, 16, 17, 18, 19, 20, 21, 22,
            -1, 24, 25, 26, 27, 28, 29, 30,
            -1, 32, 33, 34, 35, 36, 37, 38,
            -1, 40, 41, 42, 43, 44, 45, 46,
            -1, 48, 49, 50, 51, 52, 53, 54,
            -1, 56, 57, 58, 59, 60, 61, 62,
            -1, -1, -1, -1, -1, -1, -1, -1,
            
            -1, -1, -1, -1, -1, -1, -1, -1,
            -1,  0,  1,  2,  3,  4,  5,  6,
            -1,  8,  9, 10, 11, 12, 13, 14,
            -1, 16, 17, 18, 19, 20, 21, 22,
            -1, 24, 25, 26, 27, 28, 29, 30,
            -1, 32, 33, 34, 35, 36, 37, 38,
            -1, 40, 41, 42, 43, 44, 45, 46,
            -1, 48, 49, 50, 51, 52, 53, 54
            #endregion
        };

        private static readonly UnsafeArray2D<sbyte> pawnRight = new(Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region pawnRight data
             9, 10, 11, 12, 13, 14, 15, -1,
            17, 18, 19, 20, 21, 22, 23, -1,
            25, 26, 27, 28, 29, 30, 31, -1,
            33, 34, 35, 36, 37, 38, 39, -1,
            41, 42, 43, 44, 45, 46, 47, -1,
            49, 50, 51, 52, 53, 54, 55, -1,
            57, 58, 59, 60, 61, 62, 63, -1,
            -1, -1, -1, -1, -1, -1, -1, -1,
            
            -1, -1, -1, -1, -1, -1, -1, -1,
             1,  2,  3,  4,  5,  6,  7, -1,
             9, 10, 11, 12, 13, 14, 15, -1,
            17, 18, 19, 20, 21, 22, 23, -1,
            25, 26, 27, 28, 29, 30, 31, -1,
            33, 34, 35, 36, 37, 38, 39, -1,
            41, 42, 43, 44, 45, 46, 47, -1,
            49, 50, 51, 52, 53, 54, 55, -1
            #endregion
        };

        public static readonly UnsafeArray2D<sbyte> PawnPlus = new(Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region pawnPlus data
             8,  9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39,
            40, 41, 42, 43, 44, 45, 46, 47,
            48, 49, 50, 51, 52, 53, 54, 55,
            56, 57, 58, 59, 60, 61, 62, 63,
            -1, -1, -1, -1, -1, -1, -1, -1,
            
            -1, -1, -1, -1, -1, -1, -1, -1,
             0,  1,  2,  3,  4,  5,  6,  7,
             8,  9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39,
            40, 41, 42, 43, 44, 45, 46, 47,
            48, 49, 50, 51, 52, 53, 54, 55
            #endregion
        };

        private static readonly UnsafeArray2D<sbyte> pawnDouble = new(Constants.MAX_COLORS, Constants.MAX_SQUARES)
        {
            #region pawnDouble data
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39,
            40, 41, 42, 43, 44, 45, 46, 47,
            48, 49, 50, 51, 52, 53, 54, 55,
            56, 57, 58, 59, 60, 61, 62, 63,
            -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1,
            
            -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1,
             0,  1,  2,  3,  4,  5,  6,  7,
             8,  9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39,
            40, 41, 42, 43, 44, 45, 46, 47
            #endregion
        };
        #endregion
    }
}