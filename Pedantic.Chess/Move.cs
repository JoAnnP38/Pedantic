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
        public static readonly ulong NullMove = Pack(Piece.None, 0, 0, MoveType.Null);
        public static ulong Pack(Piece piece, int from, int to, MoveType type = MoveType.Normal, 
            Piece capture = Piece.None, Piece promote = Piece.None, int score = 0)
        {
            Util.Assert(Index.IsValid(from));
            Util.Assert(Index.IsValid(to));
            Util.Assert(score is >= -Constants.HISTORY_SCORE and <= short.MaxValue);
            ulong move = ((ulong)piece & 0x07) |
                        (((ulong)from & 0x3f) << 3) |
                        (((ulong)to & 0x3f) << 9) |
                        (((ulong)type & 0x0f) << 15) |
                        (((ulong)capture & 0x07) << 19) |
                        (((ulong)promote & 0x07) << 22) |
                        (((ulong)score & 0x0ffff) << 25);

            return move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ClearScore(ulong move)
        {
            return move & 0x01fffffful;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetScore(ulong move, short score)
        {
            return BitOps.BitFieldSet(move, score, 25, 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AdjustScore(ulong move, short adjustment)
        {
            return SetScore(move, (short)(GetScore(move) + adjustment));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetPiece(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 0, 3);
            return piece == 0x07 ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFrom(ulong move)
        {
            return BitOps.BitFieldExtract(move, 3, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTo(ulong move)
        {
            return BitOps.BitFieldExtract(move, 9, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MoveType GetMoveType(ulong move)
        {
            return (MoveType)BitOps.BitFieldExtract(move, 15, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetCapture(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 19, 3);
            return piece == 0x07 ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetPromote(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 22, 3);
            return piece == 0x07 ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetScore(ulong move)
        {
            return (short)BitOps.BitFieldExtract(move, 25, 16);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBadCapture(ulong move)
        {
            int score = GetScore(move);
            return IsCapture(move) && (score is >= Constants.BAD_CAPTURE and < Constants.KILLER_SCORE);
        }

        public static void Unpack(ulong move, out Piece piece, out int from, out int to, out MoveType type, out Piece capture,
            out Piece promote, out int score)
        {
            piece = GetPiece(move);
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
            try
            {
                if (s.Length < 4)
                {
                    throw new ArgumentException(@"Parameter too short to represent a valid move.", nameof(s));
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
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.ToString());
            }

            return false;
        }

        public static string ToString(ulong move)
        {
            Piece piece = GetPiece(move);
            int from = GetFrom(move);
            int to = GetTo(move);
            Piece promote = GetPromote(move);
            return $"{Index.ToString(from)}{Index.ToString(to)}{Conversion.PieceToString(promote)}";
        }

        public static string ToLongString(ulong move)
        {
            Unpack(move, out Piece piece, out int from, out int to, out MoveType type, out Piece capture, out Piece promote,
                out int score);

            return
                $"(Piece = {piece}, From = {Index.ToString(from)}, To = {Index.ToString(to)}, Type = {type}, Capture = {capture}, Promote = {promote}, Score = {score})";
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
            Move.Unpack(move, out Piece piece, out int from, out int to, out MoveType type, out Piece capture, out Piece promote, out int _);
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
                if (piece != Piece.Pawn)
                {
                    sb.Append(Conversion.PieceToString(piece).ToUpper());
                }

                MoveList moveList = new();
                board.GenerateLegalMoves(moveList);

                var ambiguous = moveList.Where(m =>
                    GetTo(m) == to && GetPiece(m) == piece && Compare(move, m) == 0).ToArray();

                if (ambiguous.Length > 0)
                {
                    if (ambiguous.Any(m => Index.GetFile(GetFrom(m)) == Index.GetFile(from)))
                    {
                        sb.Append(ambiguous.Any(m => Index.GetRank(GetFrom(m)) == Index.GetRank(from))
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
