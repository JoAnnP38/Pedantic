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
        public static readonly ulong NullMove = Pack(Color.None, Piece.None, 0, 0, MoveType.Null);
        public static ulong Pack(Color stm, Piece piece, int from, int to, 
            MoveType type = MoveType.Normal, Piece capture = Piece.None, Piece promote = Piece.None, 
            int score = 0)
        {
            Util.Assert(Index.IsValid(from));
            Util.Assert(Index.IsValid(to));
            ulong move = ((ulong)stm & 0x03) |
                        (((ulong)piece & 0x07) << 2) |
                        (((ulong)from & 0x3f) << 5) |
                        (((ulong)to & 0x3f) << 11) |
                        (((ulong)type & 0x0f) << 17) |
                        (((ulong)capture & 0x07) << 21) |
                        (((ulong)promote & 0x07) << 24) | 
                        /* note the gap here between promote & score*/
                        (((ulong)score) << 32);

            return move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ClearScore(ulong move)
        {
            return move & 0x0fffffffful;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SetScore(ulong move, int score)
        {
            return ClearScore(move) | ((ulong)score << 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AdjustScore(ulong move, int adjustment)
        {
            return SetScore(move, GetScore(move) + adjustment);
        }

        public static Color GetStm(ulong move)
        {
            int color = BitOps.BitFieldExtract(move, 0, 2);
            return color == 0x03 ? Color.None : (Color)color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetPiece(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 2, 3);
            return piece == 0x07 ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFrom(ulong move)
        {
            return BitOps.BitFieldExtract(move, 5, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTo(ulong move)
        {
            return BitOps.BitFieldExtract(move, 11, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFromTo(ulong move)
        {
            return BitOps.BitFieldExtract(move, 5, 12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MoveType GetMoveType(ulong move)
        {
            return (MoveType)BitOps.BitFieldExtract(move, 17, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetCapture(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 21, 3);
            return piece == 0x07 ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetPromote(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 24, 3);
            return piece == 0x07 ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetScore(ulong move)
        {
            return (int)(move >> 32);
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
            return GetPiece(move) == Piece.Pawn;
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

        public static void Unpack(ulong move, out Color stm, out Piece piece, out int from, out int to, out MoveType type, 
            out Piece capture, out Piece promote, out int score)
        {
            stm = GetStm(move);
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
            int from = GetFrom(move);
            int to = GetTo(move);
            Piece promote = GetPromote(move);
            return $"{Index.ToString(from)}{Index.ToString(to)}{Conversion.PieceToString(promote)}";
        }

        public static string ToLongString(ulong move)
        {
            Unpack(move, out Color stm, out Piece piece, out int from, out int to, out MoveType type, 
                out Piece capture, out Piece promote, out int score);

            return
                $"(Stm = {stm}, Piece = {piece}, From = {Index.ToString(from)}, To = {Index.ToString(to)}, Type = {type}, Capture = {capture}, Promote = {promote}, Score = {score})";
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
            Unpack(move, out Color stm, out Piece piece, out int from, out int to, out MoveType type, 
                out Piece capture, out Piece promote, out int _);

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
