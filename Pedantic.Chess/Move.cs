// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Move.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Move class encapsulates a set of methods on chess moves stored in
//     a 64 bit integer (i.e ulong).
// </summary>
// ***********************************************************************
using Pedantic.Utilities;
using System.Runtime.CompilerServices;
using System.Text;

namespace Pedantic.Chess
{
    public static class Move
    {
        public static readonly ulong NullMove = PackMove(0, 0, MoveType.Null);
        public static ulong PackMove(int from, int to, MoveType type = MoveType.Normal, Piece capture = Piece.None, 
            Piece promote = Piece.None, int score = 0)
        {
            Util.Assert(Index.IsValid(from));
            Util.Assert(Index.IsValid(to));
            Util.Assert(score is >= 0 and <= short.MaxValue);
            ulong move = ((ulong)from & 0x3f) |
                         (((ulong)to & 0x3f) << 6) |
                         (((ulong)type & 0x0f) << 12) |
                         (((ulong)capture & 0x0f) << 16) |
                         (((ulong)promote & 0x0f) << 20) |
                         (((ulong)score & 0x0ffff) << 24);

            return move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ClearScore(ulong move)
        {
            return move & 0x0fffffful;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetScore(ulong move, short score)
        {
            return BitOps.BitFieldSet(move, score, 24, 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFrom(ulong move)
        {
            return BitOps.BitFieldExtract(move, 0, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTo(ulong move)
        {
            return BitOps.BitFieldExtract(move, 6, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MoveType GetMoveType(ulong move)
        {
            return (MoveType)BitOps.BitFieldExtract(move, 12, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetCapture(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 16, 4);
            return piece == 0x0f ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetPromote(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 20, 4);
            return piece == 0x0f ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetScore(ulong move)
        {
            return BitOps.BitFieldExtract(move, 24, 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCapture(ulong move)
        {
            return GetCapture(move) != Piece.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPromote(ulong move)
        {
            return GetPromote(move) != Piece.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPawnMove(ulong move)
        {
            return GetMoveType(move) == MoveType.PawnMove;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsQuiet(ulong move)
        {
            return !IsCapture(move) && !IsPromote(move);
        }

        public static void UnpackMove(ulong move, out int from, out int to, out MoveType type, out Piece capture,
            out Piece promote, out int score)
        {
            from = GetFrom(move);
            to = GetTo(move);
            type = GetMoveType(move);
            capture = GetCapture(move);
            promote = GetPromote(move);
            score = GetScore(move);
        }

        public static bool TryParseMove(Board board, string s, out ulong move)
        {
            move = 0;
            if (s.Length < 4)
            {
                throw new ArgumentException(@"Parameter to short to represent a valid move.", nameof(s));
            }

            if (!Index.TryParse(s[..2], out int from))
            {
                throw new ArgumentException(@"Invalid from square in move.", nameof(s));
            }

            if (!Index.TryParse(s[2..4], out int to))
            {
                throw new ArgumentException(@"Invalid to square in move.", nameof(s));
            }

            Piece promote = s.Length > 4 ? Conversion.ParsePiece(s[4]) : Piece.None;

            MoveList moveList = new();
            board.GenerateMoves(moveList);

            for (int n = 0; n < moveList.Count; ++n)
            {
                ulong mv = moveList[n];
                string mvString = Move.ToString(mv);
                if (from == GetFrom(mv) && to == GetTo(mv) && promote == GetPromote(mv))
                {
                    bool legal = board.MakeMove(mv);
                    if (legal)
                    {
                        board.UnmakeMove();
                        move = mv;
                        return true;
                    }
                }
            }

            return false;
        }

        public static string ToString(ulong move)
        {
            int from = GetFrom(move);
            int to = GetTo(move);
            Piece promote = GetPromote(move);
            return $"{Index.ToString(from)}{Index.ToString(to)}{Conversion.PieceToString(promote)}";
        }

        public static string ToLongString(ulong move)
        {
            UnpackMove(move, out int from, out int to, out MoveType type, out Piece capture, out Piece promote,
                out int score);

            return
                $"(From = {Index.ToString(from)}, To = {Index.ToString(to)}, Type = {type}, Capture = {capture}, Promote = {promote}, Score = {score})";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(ulong move1, ulong move2)
        {
            return (int)ClearScore(move1) - (int)ClearScore(move2);
        }

        public static string ToSanString(ulong move, Board board)
        {
            if (board.IsLegalMove(move))
            {
                throw new InvalidOperationException("Invalid move.");
            }

            StringBuilder sb = new();
            Move.UnpackMove(move, out int from, out int to, out MoveType type, out Piece capture, out Piece promote, out int _);
            if (type == MoveType.Castle)
            {
                if (Index.GetFile(to) == 2)
                {
                    sb.Append("O-O-O");
                }
                else
                {
                    sb.Append("O-O");
                }
            }
            else
            {
                Square sq = board.PieceBoard[from];
                if (sq.Piece != Piece.Pawn)
                {
                    sb.Append(Conversion.PieceToString(board.PieceBoard[from].Piece).ToUpper());
                }

                MoveList moveList = new();
                board.GenerateLegalMoves(moveList);

                var ambiguous = moveList.Where(m =>
                    Move.GetTo(m) == to && board.PieceBoard[Move.GetFrom(m)].Piece == sq.Piece &&
                    Move.Compare(move, m) == 0).ToArray();

                if (ambiguous.Length > 0)
                {
                    if (ambiguous.Any(m => Index.GetFile(Move.GetFrom(m)) == Index.GetFile(from)))
                    {
                        sb.Append(ambiguous.Any(m => Index.GetRank(Move.GetFrom(m)) == Index.GetRank(from))
                            ? Index.ToString(from)
                            : Coord.ToRank(Index.GetRank(from)));
                    }
                    else
                    {
                        sb.Append(Coord.ToFile(Index.GetFile(from)));
                    }
                }

                if (IsCapture(move))
                {
                    sb.Append('x');
                }

                sb.Append(Index.ToString(to));

                if (IsPromote(move))
                {
                    sb.Append('=');
                    sb.Append(Conversion.PieceToString(promote).ToUpper());
                }

                board.MakeMove(move);
                if (board.IsChecked())
                {
                    if (board.NoLegalMoves())
                    {
                        sb.Append('#');
                    }
                    else
                    {
                        sb.Append('+');
                    }
                }

                board.UnmakeMove();
            }

            return sb.ToString();
        }
    }
}
