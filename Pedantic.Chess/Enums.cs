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
}