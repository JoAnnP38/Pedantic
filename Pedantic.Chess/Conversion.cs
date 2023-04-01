// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Conversion.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Miscellaneous functions I didn't now where else to put, so they 
//     reside here for now.
// </summary>
// ***********************************************************************
using Pedantic.Utilities;
using System.Text;

namespace Pedantic.Chess
{
    public static class Conversion
    {
        public static Piece ParsePiece(char c)
        {
            return char.ToLower(c) switch
            {
                'p' => Piece.Pawn,
                'n' => Piece.Knight,
                'b' => Piece.Bishop,
                'r' => Piece.Rook,
                'q' => Piece.Queen,
                'k' => Piece.King,
                _ => Piece.None
            };
        }

        public static string PieceToString(Piece piece)
        {
            return piece switch
            {
                Piece.Pawn => "p",
                Piece.Knight => "n",
                Piece.Bishop => "b",
                Piece.Rook => "r",
                Piece.Queen => "q",
                Piece.King => "k",
                Piece.None => string.Empty,
                _ => string.Empty
            };
        }

        public static string BitBoardToString(ulong bitBoard)
        {
            StringBuilder sb = new();
            for (int j = 56; j >= 0; j -= 8)
            {
                for (int i = 0; i < 8; i++)
                {
                    sb.Append(BitOps.GetBit(bitBoard, i + j));
                    sb.Append(' ');
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static Color Other(this Color color)
        {
            return (Color)((int)color ^ 1);
        }
    }
}
