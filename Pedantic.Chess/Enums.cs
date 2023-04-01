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
}