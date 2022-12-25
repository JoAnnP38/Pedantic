using System.Runtime.CompilerServices;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class Move
    {
        public static ulong PackMove(int from, int to, MoveType type = MoveType.Normal, Piece capture = Piece.None, 
            Piece promote = Piece.None, int score = 0)
        {
            Util.Assert(Index.IsValid(from));
            Util.Assert(Index.IsValid(to));
            Util.Assert(score >= 0 && score <= short.MaxValue);
            ulong move = ((ulong)from & 0x3f) |
                         (((ulong)to & 0x3f) << 6) |
                         (((ulong)type & 0x0f) << 12) |
                         (((ulong)capture & 0x0f) << 16) |
                         (((ulong)promote & 0x0f) << 20) |
                         (((ulong)score & 0x0ffff) << 24);

            return move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFrom(ulong move)
        {
            return BitOps.BitFieldExtract(move, 0, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTo(ulong move)
        {
            return BitOps.BitFieldExtract(move, 6, 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MoveType GetMoveType(ulong move)
        {
            return (MoveType)BitOps.BitFieldExtract(move, 12, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetCapture(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 16, 4);
            return piece == 0x0f ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Piece GetPromote(ulong move)
        {
            int piece = BitOps.BitFieldExtract(move, 20, 4);
            return piece == 0x0f ? Piece.None : (Piece)piece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetScore(ulong move)
        {
            return BitOps.BitFieldExtract(move, 24, 16);
        }

        public static void UnpackMove(ulong move, out int from, out int to, out MoveType type, out Piece capture,
            out Piece promote, out int score)
        {
            from = GetFrom(move);
            to = GetTo(move);
            type = GetMoveType(move);
            capture = GetCapture(move);
            promote = GetPromote(move);
            score = GetScore(move);
        }

        public static bool TryParseMove(Board board, string s, out ulong move)
        {
            move = 0;
            return false;
        }

        public static string ToString(ulong move)
        {
            int from = GetFrom(move);
            int to = GetTo(move);
            Piece promote = GetPromote(move);
            return $"{Index.ToString(from)}{Index.ToString(to)}{Conversion.PieceToString(promote)}";
        }
    }
}
