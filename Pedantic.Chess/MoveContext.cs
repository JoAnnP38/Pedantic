using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public abstract class MoveContext
    {
        protected MoveContext(Board board, Color color)
        {
            SideToMove = color;
            Opponent = color.Other();
            FriendlyPawns = board.Pieces(color, Piece.Pawn);
            FriendlyKnights = board.Pieces(color, Piece.Knight);
            FriendlyBishops = board.Pieces(color, Piece.Bishop);
            FriendlyRooks = board.Pieces(color, Piece.Rook);
            FriendlyQueens = board.Pieces(color, Piece.Queen);
            FriendlyKing = board.Pieces(color, Piece.King);
            EnemyPawns = board.Pieces(Opponent, Piece.Pawn);
            EnemyKing = board.Pieces(Opponent, Piece.King);
            Friends = board.Units(color);
            Enemies = board.Units(Opponent);
        }

        public Color SideToMove { get; }
        public Color Opponent { get; }
        public ulong FriendlyPawns { get; private set; }
        public ulong FriendlyKnights { get; private set; }
        public ulong FriendlyBishops { get; private set; }
        public ulong FriendlyRooks { get; private set; }
        public ulong FriendlyQueens { get; private set; }
        public ulong FriendlyKing { get; private set; }
        public ulong EnemyPawns { get; private set; }
        public ulong EnemyKing { get; private set; }
        public ulong Friends { get; private set; }
        public ulong Enemies { get; private set; }
        public int PawnStartingRank { get; protected set; }
        public int PawnPromoteRank { get; protected set; }
        public int StartingKingSquare { get; protected set; }
        public CastlingRights KingSideMask { get; protected set; }
        public ulong KingSideClearMask { get; protected set; }
        public int KingSideTo { get; protected set; }
        public CastlingRights QueenSideMask { get; protected set; }
        public ulong QueenSideClearMask { get; protected set; }
        public int QueenSideTo { get; protected set; }
        public int PawnCaptureShiftLeft { get; protected set; }
        public int PawnCaptureShiftRight { get; protected set; }
        public abstract ulong PawnShift(ulong value, int shift);

        public void Update(Board board)
        {
            FriendlyPawns = board.Pieces(SideToMove, Piece.Pawn);
            FriendlyKnights = board.Pieces(SideToMove, Piece.Knight);
            FriendlyBishops = board.Pieces(SideToMove, Piece.Bishop);
            FriendlyRooks = board.Pieces(SideToMove, Piece.Rook);
            FriendlyQueens = board.Pieces(SideToMove, Piece.Queen);
            FriendlyKing = board.Pieces(SideToMove, Piece.King);
            EnemyPawns = board.Pieces(Opponent, Piece.Pawn);
            EnemyKing = board.Pieces(Opponent, Piece.King);
            Friends = board.Units(SideToMove);
            Enemies = board.Units(Opponent);
        }
    }

    public class WhiteMoveContext : MoveContext
    {
        public WhiteMoveContext(Board board) : base(board, Color.White)
        {
            PawnStartingRank = Coord.RANK_2;
            PawnPromoteRank = Coord.RANK_7;
            StartingKingSquare = Index.E1;
            KingSideMask = CastlingRights.WhiteKingSide;
            KingSideClearMask = BitOps.GetMask(Index.F1) | BitOps.GetMask(Index.G1);
            KingSideTo = Index.G1;
            QueenSideMask = CastlingRights.WhiteQueenSide;
            QueenSideClearMask = BitOps.GetMask(Index.B1) | BitOps.GetMask(Index.C1) | BitOps.GetMask(Index.D1);
            QueenSideTo = Index.C1;
            PawnCaptureShiftLeft = 7;
            PawnCaptureShiftRight = 9;
        }

        public override ulong PawnShift(ulong value, int shift)
        {
            return value >> shift;
        }
    }

    public class BlackMoveContext : MoveContext
    {
        public BlackMoveContext(Board board) : base(board, Color.Black)
        {
            PawnStartingRank = Coord.RANK_7;
            PawnPromoteRank = Coord.RANK_2;
            StartingKingSquare = Index.E8;
            KingSideMask = CastlingRights.BlackKingSide;
            KingSideClearMask = BitOps.GetMask(Index.F8) | BitOps.GetMask(Index.G8);
            KingSideTo = Index.G8;
            QueenSideMask = CastlingRights.BlackQueenSide;
            QueenSideClearMask = BitOps.GetMask(Index.B8) | BitOps.GetMask(Index.C8) | BitOps.GetMask(Index.D8);
            QueenSideTo = Index.C8;
            PawnCaptureShiftLeft = 9;
            PawnCaptureShiftRight = 7;
        }
        
        public override ulong PawnShift(ulong value, int shift)
        {
            return value << shift;
        }
    }
}
