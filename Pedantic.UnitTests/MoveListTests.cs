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
            MoveList list = new();
            list.Add(Move.Pack(Piece.Knight, Index.B1, Index.C3, score: 0));
            list.Add(Piece.Bishop, Index.C1, Index.F4, score: -5);
            list.Add(Move.Pack(Piece.Rook, Index.H1, Index.H3, MoveType.Capture, Piece.Pawn, score: Board.CaptureScore(Piece.Pawn, Piece.Rook)));

            ulong move = list.Sort(0);
            Assert.AreEqual((short)30003, Move.GetScore(move));
            
            move = list.Sort(1);
            Assert.AreEqual((short)0, Move.GetScore(move));

            move = list.Sort(2);
            Assert.AreEqual((short)-5, Move.GetScore(move));
        }

        [TestMethod]
        public void NegativeMoveScoreTest()
        {
            ulong move = Move.Pack(Piece.Pawn, Index.E2, Index.E4, MoveType.DblPawnMove, score: -5);
            short score = Move.GetScore(move);
            Assert.AreEqual((short)-5, score);
        }
    }
}