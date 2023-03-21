using Pedantic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
