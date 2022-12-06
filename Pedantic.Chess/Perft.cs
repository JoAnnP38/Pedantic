using System;
using System.Collections.Generic;
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
        private ObjectPool<MoveList> moveListPool = new(Constants.MAX_PLY);

        public struct Counts
        {
            public uint Nodes;
            public uint Captures;
            public uint EnPassants; 
            public uint Castles;
            public uint Checks;
            public uint Checkmates;
            public uint Promotions;

            public static Counts Default
            {
                get
                {
                    Counts counts;
                    counts.Nodes = 0;
                    counts.Captures = 0;
                    counts.EnPassants = 0;
                    counts.Castles = 0;
                    counts.Checks = 0;
                    counts.Checkmates = 0;
                    counts.Promotions = 0;
                    return counts;
                }
            }
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
            for (int n = 0; n < moveList.Count; ++n)
            {
                if (board.MakeMove(moveList[n]))
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
            for (int n = 0; n < moveList.Count; ++n)
            {
                ulong move = moveList[n];
                if (board.MakeMove(move))
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
                for (int n = 0; n < moveList.Count; ++n)
                {
                    if (board.MakeMove(moveList[n]))
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
