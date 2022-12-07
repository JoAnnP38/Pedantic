namespace Pedantic.Chess
{
    public enum Color 
    {
        None = -1,
        White,
        Black
    }

    public enum Piece 
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
    public enum CastlingRights
    {
        None,
        WhiteKingSide = 1,
        WhiteQueenSide = 2,
        BlackKingSide = 4,
        BlackQueenSide = 8,
        All = WhiteKingSide | WhiteQueenSide | BlackKingSide | BlackQueenSide
    }

    public enum MoveType 
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
}