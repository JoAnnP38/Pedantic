using System.Text;

namespace Pedantic.Chess
{
    public sealed class PieceString
    {
        public const int MAX_PIECE_STRING_LENGTH = 16;

        public PieceString()
        {
            Array.Fill(pieces, Piece.None);
        }

        public PieceString(PieceString other)
        {
            Array.Copy(other.pieces, pieces, MAX_PIECE_STRING_LENGTH);
        }

        public PieceString(string str)
            : this()
        {
            for (int n = 0; n < Math.Min(str.Length, MAX_PIECE_STRING_LENGTH); ++n)
            {
                Piece p = Conversion.ParsePiece(str[n]);
                if (p != Piece.None)
                {
                    pieces[n] = p;
                }
                else
                {
                    break;
                }
            }
            Array.Sort(pieces, 0, MAX_PIECE_STRING_LENGTH, pieceComparer);
        }

        public void Clear()
        {
            Array.Fill(pieces, Piece.None);
        }

        public bool AddPiece(Piece piece)
        {
            int n;
            for (n = 0; n < MAX_PIECE_STRING_LENGTH; ++n)
            {
                if (piece > pieces[n])
                {
                    break;
                }
            }

            if (n < MAX_PIECE_STRING_LENGTH)
            {
                for (int m = MAX_PIECE_STRING_LENGTH - 1; m > n; --m)
                {
                    pieces[m] = pieces[m - 1];
                }
                pieces[n] = piece;
                return true;
            }
            return false;
        }

        public bool RemovePiece(Piece piece)
        {
            int index = Array.IndexOf(pieces, piece);
            if (index >= 0)
            {
                for (int n = index; n < MAX_PIECE_STRING_LENGTH - 1; ++n)
                {
                    pieces[n] = pieces[n + 1];
                }
                pieces[MAX_PIECE_STRING_LENGTH - 1] = Piece.None;
                return true;
            }
            return false;
        }

        public Piece this[int index] => pieces[index];

        public bool Equals(PieceString? other)
        {
            return other != null && this == other;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as PieceString);
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            for (int n = 0; n < MAX_PIECE_STRING_LENGTH; ++n)
            {
                hash.Add(pieces[n]);
            }
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Piece p in pieces)
            {
                sb.Append(p.ToChar());
            }
            return sb.ToString();
        }

        public int GetLength()
        {
            int length = 0;
            while (length < MAX_PIECE_STRING_LENGTH && pieces[length] != Piece.None)
            {
                length++;
            }
            return length;
        }


        public static bool operator == (PieceString? ps1, PieceString? ps2)
        {
            if (ps1 == null || ps2 == null)
            {
                return ps1 == null && ps2 == null;
            }

            for (int n = 0; n < MAX_PIECE_STRING_LENGTH; ++n)
            {
                if (ps1.pieces[n] != ps2.pieces[n])
                {
                    return false;
                }
                if (ps1.pieces[n] == Piece.None)
                {
                    break;
                }
            }
            return true;
        }

        public static bool operator != (PieceString? ps1, PieceString? ps2)
        {
            return !(ps1 == ps2);
        }

        private Piece[] pieces = new Piece[MAX_PIECE_STRING_LENGTH];

        private static readonly ReversePieceComparer pieceComparer = new ReversePieceComparer();
    }
}
