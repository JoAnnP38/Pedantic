// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-26-2023
// ***********************************************************************
// <copyright file="Enums.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Global enumerations used by the Pedantic app.
// </summary>
// ***********************************************************************
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public enum Color : sbyte
    {
        None = -1,
        White,
        Black
    }

    public enum Piece : sbyte
    {
        None = -1,
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    [Flags]
    public enum CastlingRights : byte
    {
        None,
        WhiteKingSide = 1,
        WhiteQueenSide = 2,
        BlackKingSide = 4,
        BlackQueenSide = 8,
        WhiteRights = WhiteKingSide | WhiteQueenSide,
        BlackRights = BlackKingSide | BlackQueenSide,
        All = WhiteKingSide | WhiteQueenSide | BlackKingSide | BlackQueenSide
    }

    public enum MoveType : byte
    {
        Normal, 
        Capture, 
        Castle, 
        EnPassant, 
        PawnMove, 
        DblPawnMove, 
        Promote, 
        PromoteCapture,
        Null
    }

    public enum GamePhase : byte
    {
        Opening,
        MidGame,
        EndGame,
        EndGameMopup
    }

    public enum TtFlag : byte
    {
        Exact,
        UpperBound,
        LowerBound
    }

    [Flags]
    public enum KingPlacement : byte
    {
        OpponentKingSide = 0,
        FriendlyKingSide = OpponentKingSide,
        OpponentQueenSide = 1,
        FriendlyQueenSide = 2,

        KK = FriendlyKingSide | OpponentKingSide,
        KQ = FriendlyKingSide | OpponentQueenSide,
        QK = FriendlyQueenSide | OpponentKingSide,
        QQ = FriendlyQueenSide | OpponentQueenSide
    }

    public static class ChessExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Other(this Color color)
        {
            return (Color)((int)color ^ 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDiagonalSlider(this Piece piece)
        {
            return piece == Piece.Bishop || piece == Piece.Queen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOrthogonalSlider(this Piece piece)
        {
            return piece == Piece.Rook || piece == Piece.Queen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Value(this Piece piece)
        {
            return Evaluation.CanonicalPieceValues(piece);
        }
    }
}