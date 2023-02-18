using System.Diagnostics;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Text;
using Pedantic.Chess;
using Color = Pedantic.Chess.Color;
using Index = Pedantic.Chess.Index;

namespace Pedantic.Client
{
    public partial class GameForm : Form
    {
        delegate void UpdateBoardText(string text);

        public bool IsClosed { get; set; } = false;
        
        public GameForm()
        {
            InitializeComponent();
        }

        public void UpdateBoard(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new UpdateBoardText(UpdateBoard), text);
            }
            else
            {
                rtbChessBoard.Text = text;
                rtbChessBoard.Invalidate();
                rtbChessBoard.Update();
            }
        }

        public void CloseGameForm()
        {
            if (InvokeRequired)
            {
                Invoke(CloseGameForm);
            }
            else
            {
                Close();
            }
        }

        public static void PlayGame(object? obj)
        {
            if (obj is MainForm.TaskArgs args)
            {
                Board board = new(Constants.FEN_START_POS);
                args.GameForm.UpdateBoard(BoardString(board));

                while (!args.Source.IsCancellationRequested && !args.GameForm.IsClosed)
                {
                    Thread.Sleep(100);
                }

                if (!args.GameForm.IsClosed)
                {
                    args.GameForm.CloseGameForm();
                }
            }
        }

        private static string BoardString(Board board)
        {
            StringBuilder sb = new();

            sb.Append(upper_left_corner);
            sb.Append(top_border, 8);
            sb.Append(upper_right_corner);
            sb.AppendLine();

            for (int rank = Coord.MaxValue; rank >= Coord.MinValue; rank--)
            {
                sb.Append((char)(left_border_1 + rank));
                for (int file = Coord.MinValue; file <= Coord.MaxValue; file++)
                {
                    int index = Index.ToIndex(file, rank);
                    sb.Append(SquareChar(index, board.PieceBoard[index]));
                }

                sb.Append(right_border);
                sb.AppendLine();
            }

            sb.Append(lower_left_corner);
            for (int file = Coord.MinValue; file <= Coord.MaxValue; file++)
            {
                sb.Append((char)(bottom_border_a + file));
            }

            sb.Append(lower_right_corner);

            return sb.ToString();
        }

        private static char SquareChar(int index, Square sq)
        {
            if (Index.IsDark(index))
            {
                if (sq.IsEmpty)
                {
                    return '+';
                }

                if (sq.Color == Color.White)
                {
                    return sq.Piece switch
                    {
                        Piece.Pawn => 'P',
                        Piece.Knight => 'N',
                        Piece.Bishop => 'B',
                        Piece.Rook => 'R',
                        Piece.Queen => 'Q',
                        Piece.King => 'K',
                        _ => '+'
                    };
                }
                return sq.Piece switch
                {
                    Piece.Pawn => 'O',
                    Piece.Knight => 'M',
                    Piece.Bishop => 'V',
                    Piece.Rook => 'T',
                    Piece.Queen => 'W',
                    Piece.King => 'L',
                    _ => '+'
                };
            }

            if (sq.IsEmpty)
            {
                return '*';
            }

            if (sq.Color == Color.White)
            {
                return sq.Piece switch
                {
                    Piece.Pawn => 'p',
                    Piece.Knight => 'n',
                    Piece.Bishop => 'b',
                    Piece.Rook => 'r',
                    Piece.Queen => 'q',
                    Piece.King => 'k',
                    _ => '*'
                };
            }

            return sq.Piece switch
            {
                Piece.Pawn => 'o',
                Piece.Knight => 'm',
                Piece.Bishop => 'v',
                Piece.Rook => 't',
                Piece.Queen => 'w',
                Piece.King => 'l',
                _ => '+'
            };
        }

        private const char upper_left_corner = '!';
        private const char upper_right_corner = '#';
        private const char lower_left_corner = '\u002F';
        private const char lower_right_corner = ')';
        private const char top_border = '"';
        private const char right_border = '%';
        private const char left_border_1 = '\u00E0';
        private const char left_border_2 = '\u00E1';
        private const char left_border_3 = '\u00E2';
        private const char left_border_4 = '\u00E3';
        private const char left_border_5 = '\u00E4';
        private const char left_border_6 = '\u00E5';
        private const char left_border_7 = '\u00E6';
        private const char left_border_8 = '\u00E7';
        private const char bottom_border_a = '\u00E8';
        private const char bottom_border_b = '\u00E9';
        private const char bottom_border_c = '\u00EA';
        private const char bottom_border_d = '\u00EB';
        private const char bottom_border_e = '\u00EC';
        private const char bottom_border_f = '\u00ED';
        private const char bottom_border_g = '\u00EE';
        private const char bottom_border_h = '\u00EF';

        private void rtbChessBoard_Enter(object sender, EventArgs e)
        {
            lblCurrentMoveLine.Focus();
        }

        private void GameForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            IsClosed = true;
        }
    }
}