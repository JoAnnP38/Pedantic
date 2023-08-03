using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using Pedantic.Chess;
using Index = Pedantic.Chess.Index;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class MoveListTests
    {
        [TestMethod]
        public void SortTest()
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            MoveList list = new();
#pragma warning restore IDE0028 // Simplify collection initialization
            list.Add(Move.Pack(Color.White, Piece.Knight, Index.B1, Index.C3, score: 0));
            list.Add(Color.White, Piece.Bishop, Index.C1, Index.F4, score: -5);
            list.Add(Move.Pack(Color.White, Piece.Rook, Index.H1, Index.H3, MoveType.Capture, Piece.Pawn, score: Board.CaptureScore(Piece.Pawn, Piece.Rook)));

            ulong move = list.Sort(0);
            Assert.AreEqual(166003, Move.GetScore(move));
            
            move = list.Sort(1);
            Assert.AreEqual(0, Move.GetScore(move));

            move = list.Sort(2);
            Assert.AreEqual(-5, Move.GetScore(move));
        }

        [TestMethod]
        public void NegativeMoveScoreTest()
        {
            ulong move = Move.Pack(Color.White, Piece.Pawn, Index.E2, Index.E4, MoveType.DblPawnMove, score: -5);
            int score = Move.GetScore(move);
            Assert.AreEqual(-5, score);
        }
    }
}