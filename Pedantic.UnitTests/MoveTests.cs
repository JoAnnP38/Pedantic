using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;
using Index = Pedantic.Chess.Index;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class MoveTests
    {
        [TestMethod]
        public void NegativeScoreTest()
        {
            ulong move = Move.Pack(Piece.Pawn, Index.E2, Index.E4, MoveType.DblPawnMove, score: -1);
            short moveScore = Move.GetScore(move);
            Assert.AreEqual((short)-1, moveScore);
        }

        [TestMethod]
        public void AdjustScoreTest()
        {
            ulong move = Move.Pack(Piece.Pawn, Index.E4, Index.D5, MoveType.Capture, Piece.Pawn, score: Board.CaptureScore(Piece.Pawn, Piece.Pawn));
            short moveScore = Move.GetScore(move);
            Assert.AreEqual((short)30006, moveScore);

            move = Move.AdjustScore(move, Constants.BAD_CAPTURE - Constants.CAPTURE_SCORE);
            moveScore = Move.GetScore(move);
            Assert.AreEqual((short)25006, moveScore);
        }

    }
}