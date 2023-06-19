using System.Text;

namespace Pedantic.Chess
{
    public class PieceLineup
    {
        public PieceLineup()
        { }

        public PieceLineup(Board board) : this()
        { 
            for (Color color = Color.White; color <= Color.Black; color++)
            {
                for (Piece piece = Piece.King; piece >= Piece.Pawn; --piece)
                {
                    pieceStrings[(int)color].AddPiece(piece);
                }
            }
        }

        public bool AddPiece(Color color, Piece piece)
        {
            return pieceStrings[(int)color].AddPiece(piece);
        }

        public bool RemovePiece(Color color, Piece piece)
        {
            return pieceStrings[(int)color].RemovePiece(piece);
        }

        public void Clear()
        {
            pieceStrings[0].Clear();
            pieceStrings[1].Clear();
        }

        public PieceString this[Color color] => pieceStrings[(int)color];

        public bool IsMatch(PieceString whitePcStr, PieceString blackPcStr)
        {
            return pieceStrings[0] == whitePcStr && pieceStrings[1] == blackPcStr;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            if (pieceStrings[0].GetLength() > pieceStrings[1].GetLength())
            {
                sb.Append(pieceStrings[0]);
                sb.Append('v');
                sb.Append(pieceStrings[1]);
            }
            else
            {
                sb.Append(pieceStrings[1]);
                sb.Append('v');
                sb.Append(pieceStrings[0]);
            }
            return sb.ToString();
        }

        private PieceString[] pieceStrings = { new PieceString(), new PieceString() };
    }
}
