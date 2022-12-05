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
            ulong move = BitOps.BitFieldSet(0ul, from, 0, 6);
            move = BitOps.BitFieldSet(move, to, 6, 6);
            move = BitOps.BitFieldSet(move, (int)type, 12, 4);
            move = BitOps.BitFieldSet(move, (int)capture, 16, 4);
            move = BitOps.BitFieldSet(move, (int)promote, 20, 4);
            move = BitOps.BitFieldSet(move, score, 24, 16);
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
