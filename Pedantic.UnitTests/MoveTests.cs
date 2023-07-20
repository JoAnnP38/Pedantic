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
            ulong move = Move.Pack(Color.White, Piece.Pawn, Index.E2, Index.E4, MoveType.DblPawnMove, score: -1);
            int moveScore = Move.GetScore(move);
            Assert.AreEqual(-1, moveScore);
        }

        [TestMethod]
        public void AdjustScoreTest()
        {
            ulong move = Move.Pack(Color.White, Piece.Pawn, Index.E4, Index.D5, MoveType.Capture, Piece.Pawn, score: Board.CaptureScore(Piece.Pawn, Piece.Pawn));
            int moveScore = Move.GetScore(move);
            Assert.AreEqual(30006, moveScore);

            move = Move.AdjustScore(move, Constants.BAD_CAPTURE - Constants.CAPTURE_SCORE);
            moveScore = Move.GetScore(move);
            Assert.AreEqual(25006, moveScore);
        }

        [TestMethod]
        public void MoveStmFieldTest()
        {
            ulong move = Move.Pack(Color.None, Piece.Pawn, Index.E2, Index.E4, MoveType.DblPawnMove);
            Color stm = Move.GetStm(move);
            Assert.AreEqual(Color.None, stm);

            move = Move.Pack(Color.White, Piece.Pawn, Index.E2, Index.E4, MoveType.DblPawnMove);
            stm = Move.GetStm(move);
            Assert.AreEqual(Color.White, stm);

            move = Move.Pack(Color.Black, Piece.Pawn, Index.E2, Index.E4, MoveType.DblPawnMove);
            stm = Move.GetStm(move);
            Assert.AreEqual(Color.Black, stm);

            Move.Unpack(move, out stm, out Piece piece, out int from, out int to, out MoveType type, out Piece capture, out Piece promote, out int score);

            Assert.AreEqual(Color.Black, stm);
            Assert.AreEqual(Piece.Pawn, piece);
            Assert.AreEqual(Index.E2, from);
            Assert.AreEqual(Index.E4, to);
            Assert.AreEqual(MoveType.DblPawnMove, type);
            Assert.AreEqual(Piece.None, capture);
            Assert.AreEqual(Piece.None, promote);
            Assert.AreEqual(0, score);

            Move.Unpack(0xfffffffffffffffful, out stm, out piece, out from, out to, out type, out capture, out promote, out score);
            Assert.AreEqual(Color.None, stm);
            Assert.AreEqual(Piece.None, piece);
            Assert.AreEqual(Index.H8, from);
            Assert.AreEqual(Index.H8, to);
            Assert.AreEqual(0x0f, (int)type);
            Assert.AreEqual(Piece.None, capture);
            Assert.AreEqual(Piece.None, promote);
            Assert.AreEqual(-1, score);
        }

    }
}