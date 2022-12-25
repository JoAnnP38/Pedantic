using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Utilities;
using static Pedantic.Chess.Perft;

namespace Pedantic.Chess
{
    public sealed class Perft
    {
        private Board board = new Board(Constants.FEN_START_POS);
        private ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY, 10);

        public struct Counts
        {
            public uint Nodes ;
            public uint Captures;
            public uint EnPassants; 
            public uint Castles;
            public uint Checks;
            public uint Checkmates;
            public uint Promotions;

            public Counts()
            {
                Nodes = 0;
                Captures = 0;
                EnPassants = 0;
                Castles = 0;
                Checks = 0;
                Checkmates = 0;
                Promotions = 0;
            }

            public static Counts Default { get; } = new Counts();

            public static Counts operator +(Counts c1, Counts c2)
            {
                Counts counts;
                counts.Nodes = c1.Nodes + c2.Nodes;
                counts.Captures = c1.Captures + c2.Captures;
                counts.EnPassants = c1.EnPassants + c2.EnPassants;
                counts.Castles = c1.Castles + c2.Castles;
                counts.Checks = c1.Checks + c2.Checks;
                counts.Checkmates = c1.Checkmates + c2.Checkmates;
                counts.Promotions = c1.Promotions + c2.Promotions;
                return counts;
            }
        }

        public Perft(string? startingPosition = null)
        {
            board.LoadFenPosition(startingPosition ?? Constants.FEN_START_POS);
        }

        public void Initialize(string fen = Constants.FEN_START_POS)
        {
            board.LoadFenPosition(fen);
        }

        public ulong Execute(int depth)
        {
            if (depth == 0)
            {
                return 1;
            }

            ulong nodes = 0;
            MoveList moveList = moveListPool.Get();
            board.GenerateMoves(moveList);

            ReadOnlySpan<ulong> moves = moveList.ToSpan();
            for (int n = 0; n < moves.Length; ++n)
            {
                if (board.MakeMove(moves[n]))
                {
                    if (depth > 1)
                    {
                        nodes += Execute(depth - 1);
                    }
                    else
                    {
                        nodes++;
                    }

                    board.UnmakeMove();
                }
            }

            moveListPool.Return(moveList);
            return nodes;
        }

        public Counts ExecuteWithDetails(int depth)
        {
            Counts counts = Counts.Default;
            MoveList moveList;

            if (depth == 0)
            {
                return GetCounts();
            }

            moveList = moveListPool.Get();
            board.GenerateMoves(moveList);
            ReadOnlySpan<ulong> moves = moveList.ToSpan();

            for (int n = 0; n < moves.Length; ++n)
            {
                if (board.MakeMove(moves[n]))
                {
                    counts += ExecuteWithDetails(depth - 1);
                    board.UnmakeMove();
                }
            }

            moveListPool.Return(moveList);
            return counts;
        }

        private Counts GetCounts()
        {
            Counts counts = Counts.Default;
            counts.Nodes = 1;
            bool inCheck = board.IsChecked();
            bool mate = false;
            if (inCheck)
            {
                counts.Checks = 1;
                mate = true;
                MoveList moveList = moveListPool.Get();
                board.GenerateMoves(moveList);
                ReadOnlySpan<ulong> moves = moveList.ToSpan();

                for (int n = 0; n < moves.Length; ++n)
                {
                    if (board.MakeMove(moves[n]))
                    {
                        board.UnmakeMove();
                        mate = false;
                        break;
                    }
                }

                moveListPool.Return(moveList);
            }

            if (mate && inCheck)
            {
                counts.Checkmates = 1;
            }

            MoveType type = Move.GetMoveType(board.LastMove);
            switch (type)
            {
                case MoveType.Capture:
                    counts.Captures = 1;
                    break;

                case MoveType.EnPassant:
                    counts.Captures = 1;
                    counts.EnPassants = 1;
                    break;

                case MoveType.Castle:
                    counts.Castles = 1;
                    break;

                case MoveType.Promote:
                    counts.Promotions = 1;
                    break;

                case MoveType.PromoteCapture:
                    counts.Captures = 1;
                    counts.Promotions = 1;
                    break;

            }

            return counts;
        }
    }
}
