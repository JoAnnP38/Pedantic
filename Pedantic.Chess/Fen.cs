using System.Text;
using System.Text.RegularExpressions;


namespace Pedantic.Chess
{
    public sealed class Fen
    {
        private readonly string fenString;

        private readonly List<(Color Color, Piece Piece, int Square)> squares = new();
        private Color sideToMove = Color.None;
        private CastlingRights castling = CastlingRights.None;
        private int enPassant = Index.None;
        private int halfMoveClock = 0;
        private int fullMoveCounter = 0;

        public Fen(string fenString)
        {
            if (!IsValidFen(fenString))
            {
                throw new ArgumentOutOfRangeException(nameof(fenString), @"Invalid FEN position specified.");
            }

            this.fenString = fenString;
            Parse();
        }

        public Fen(Board board)
        {
            (Color Color, Piece Piece)[] fenSquares = board.GetSquares();
            for (int sq = 0; sq < Constants.MAX_SQUARES; ++sq)
            {
                squares.Add((fenSquares[sq].Color, fenSquares[sq].Piece, sq));
            }

            sideToMove = board.SideToMove;
            castling = board.Castling;
            enPassant = board.EnPassant;
            halfMoveClock = board.HalfMoveClock;
            fullMoveCounter = board.FullMoveCounter;

            StringBuilder sb = new();
            FormatPieces(sb, fenSquares);
            sb.Append(sideToMove == Color.White ? " w" : " b");
            FormatCastling(sb, castling);
            sb.Append(enPassant == Index.None ? " -" : $" {Index.ToString(enPassant)}");
            sb.Append($" {halfMoveClock} {fullMoveCounter}");
            fenString = sb.ToString();
        }

        public ICollection<(Color Color, Piece Piece, int Square)> Squares => squares;
        public Color SideToMove => sideToMove;
        public CastlingRights Castling => castling;
        public int EnPassant => enPassant;
        public int HalfMoveClock => halfMoveClock;
        public int FullMoveCounter => fullMoveCounter;

        public override string ToString()
        {
            return fenString;
        }

        private void Parse()
        {
            string[] fenParts = fenString.Trim().Split(' ');
            ParseSquares(fenParts[0]);
            ParseSideToMove(fenParts[1]);
            ParseCastling(fenParts[2]);
            ParseEnPassant(fenParts[3]);
            halfMoveClock = int.Parse(fenParts[4]);
            fullMoveCounter = int.Parse(fenParts[5]);
        }

        private void ParseSquares(string fenSquares)
        {
            int rank = Coord.MaxValue, file = 0;

            foreach (char ch in fenSquares)
            {
                if (ch == '/')
                {
                    --rank;
                    file = 0;
                }
                else if (char.IsDigit(ch))
                {
                    file += ch - '0';
                }
                else
                {
                    int index = Index.ToIndex(file++, rank);
                    var pc = fenPieceMap[ch];
                    squares.Add((pc.Color, pc.Piece, index));
                }
            }
        }

        private void ParseSideToMove(string fenColor)
        {
            sideToMove = fenColor == "w" ? Color.White : Color.Black;
        }

        private void ParseCastling(string fenCastling)
        {
            castling = CastlingRights.None;
            foreach (char ch in fenCastling)
            {
                switch (ch)
                {
                    case 'K':
                        castling |= CastlingRights.WhiteKingSide;
                        break;

                    case 'Q':
                        castling |= CastlingRights.WhiteQueenSide;
                        break;

                    case 'k':
                        castling |= CastlingRights.BlackKingSide;
                        break;

                    case 'q':
                        castling |= CastlingRights.BlackQueenSide;
                        break;
                }
            }
        }

        private void ParseEnPassant(string fenEnPassant)
        {
            enPassant = fenEnPassant[0] == '-' ? Index.None : Index.ToIndex(fenEnPassant[0] - 'a', fenEnPassant[1] - '1');
        }

        private static void FormatPieces(StringBuilder sb, (Color Color, Piece Piece)[] fenSquares)
        {
            for (int rank = Coord.MaxValue; rank >= 0; --rank)
            {
                int emptyCount = 0;

                for (sbyte file = 0; file <= Coord.MaxValue; ++file)
                {
                    int sq = Index.ToIndex(file, rank);
                    if (fenSquares[sq].Piece == Piece.None)
                    {
                        ++emptyCount;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            sb.Append(emptyCount);
                            emptyCount = 0;
                        }
                        sb.Append(pieceCharMap[(int)fenSquares[sq].Color, (int)fenSquares[sq].Piece]);
                    }
                }

                if (emptyCount > 0)
                {
                    sb.Append(emptyCount);
                }

                if (rank > 0)
                {
                    sb.Append('/');
                }
            }
        }

        private static void FormatCastling(StringBuilder sb, CastlingRights castling)
        {
            sb.Append(' ');
            if (castling == CastlingRights.None)
            {
                sb.Append('-');
            }
            else
            {
                if ((castling & CastlingRights.WhiteKingSide) != 0)
                {
                    sb.Append('K');
                }

                if ((castling & CastlingRights.WhiteQueenSide) != 0)
                {
                    sb.Append('Q');
                }

                if ((castling & CastlingRights.BlackKingSide) != 0)
                {
                    sb.Append('k');
                }

                if ((castling & CastlingRights.BlackQueenSide) != 0)
                {
                    sb.Append('q');
                }
            }
        }

        public static bool IsValidFen(string fenString)
        {
            return Regex.IsMatch(fenString, Constants.REGEX_FEN);
        }

        public static bool TryParse(string fenString, out Fen fen)
        {
            if (IsValidFen(fenString))
            {
                fen = new Fen(fenString);
                return true;
            }

            fen = Empty;
            return false;
        }

        public static readonly Fen Empty = new(Constants.FEN_EMPTY);

        private static readonly SortedDictionary<char, (Color Color, Piece Piece)> fenPieceMap = new()
        {
            { 'P', (Color.White, Piece.Pawn) },
            { 'N', (Color.White, Piece.Knight) },
            { 'B', (Color.White, Piece.Bishop) },
            { 'R', (Color.White, Piece.Rook) },
            { 'Q', (Color.White, Piece.Queen) },
            { 'K', (Color.White, Piece.King) },
            { 'p', (Color.Black, Piece.Pawn) },
            { 'n', (Color.Black, Piece.Knight) },
            { 'b', (Color.Black, Piece.Bishop) },
            { 'r', (Color.Black, Piece.Rook) },
            { 'q', (Color.Black, Piece.Queen) },
            { 'k', (Color.Black, Piece.King) }
        };

        private static readonly char[,] pieceCharMap = new char[Constants.MAX_COLORS, Constants.MAX_PIECES]
        {
            { 'P', 'N', 'B', 'R', 'Q', 'K' },
            { 'p', 'n', 'b', 'r', 'q', 'k' }
        };
    }
}
