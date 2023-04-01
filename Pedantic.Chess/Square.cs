// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Square.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     A structure representing a square on a chess board and its contents
//     (if any)
// </summary>
// ***********************************************************************
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public readonly struct Square : IEquatable<Square>, IComparable<Square>
    {
        private readonly byte contents;

        public Square()
        {
            contents = 0;
        }

        public Square(Color color, Piece piece)
        {
            if (color == Color.None)
            {
                contents = 0;
            }
            else
            {
                contents = (byte)(1 | (sbyte)color << 1 | (sbyte)piece << 2);
            }
        }

        public bool IsEmpty => contents == 0;
        public Color Color => IsEmpty ? Color.None : (Color)((contents >> 1) & 0x01);
        public Piece Piece => IsEmpty ? Piece.None : (Piece)(contents >> 2);
        public byte Contents => contents;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Square other)
        {
            return contents == other.contents;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Square other)
        {
            return contents - other.contents;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Square square)
            {
                return Equals(square);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return contents.GetHashCode();
        }

        public static bool operator ==(Square sq1, Square sq2) => sq1.Equals(sq2);
        public static bool operator !=(Square sq1, Square sq2) => !sq1.Equals(sq2);

        public static explicit operator Color(Square sq) => sq.Color;
        public static explicit operator Piece(Square sq) => sq.Piece;

        public static Square Create(Color color, Piece piece)
        {
            int c = (int)color + 1;
            int p = (int)piece + 1;
            return lookup[p * 3 + c];

        }

        public static Square Empty = new();
        public static Square WhitePawn = new(Color.White, Piece.Pawn);
        public static Square WhiteKnight = new(Color.White, Piece.Knight);
        public static Square WhiteBishop = new(Color.White, Piece.Bishop);
        public static Square WhiteRook = new(Color.White, Piece.Rook);
        public static Square WhiteQueen = new(Color.White, Piece.Queen);
        public static Square WhiteKing = new(Color.White, Piece.King);
        public static Square BlackPawn = new(Color.Black, Piece.Pawn);
        public static Square BlackKnight = new(Color.Black, Piece.Knight);
        public static Square BlackBishop = new(Color.Black, Piece.Bishop);
        public static Square BlackRook = new(Color.Black, Piece.Rook);
        public static Square BlackQueen = new(Color.Black, Piece.Queen);
        public static Square BlackKing = new(Color.Black, Piece.King);

        public static Square[] lookup =
        {
            Empty,
            Empty,
            Empty,
            Empty,
            WhitePawn,
            BlackPawn,
            Empty,
            WhiteKnight,
            BlackKnight,
            Empty,
            WhiteBishop,
            BlackBishop,
            Empty,
            WhiteRook,
            BlackRook,
            Empty,
            WhiteQueen,
            BlackQueen,
            Empty,
            WhiteKing,
            BlackKing
        };
    }

    public class SquareEqualityComparer : IEqualityComparer<Square>
    {
        public bool Equals(Square x, Square y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Square obj)
        {
            return obj.Contents.GetHashCode();
        }
    }
}
