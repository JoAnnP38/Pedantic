using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;
using Index = Pedantic.Chess.Index;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class FenTests
    {
        [TestMethod]
        public void CtorStringTest()
        {
            Fen fen = new(Constants.FEN_EMPTY);
            Assert.IsTrue(fen.Squares.Count == 0);
            Assert.AreEqual(Color.White, fen.SideToMove);
            Assert.AreEqual(CastlingRights.None, fen.Castling);
            Assert.AreEqual(Index.NONE, fen.EnPassant);
            Assert.AreEqual(0, fen.HalfMoveClock);
            Assert.AreEqual(0, fen.FullMoveCounter);
        }

        [TestMethod]
        public void CtorBoardTest()
        {
            Board board = new(Constants.FEN_START_POS);
            Fen fen = new(board);

            Assert.AreEqual(Constants.FEN_START_POS, fen.ToString());
        }

        [TestMethod]
        public void ToStringTest()
        {
            Fen fen = new(Constants.FEN_EMPTY);
            Assert.AreEqual(Constants.FEN_EMPTY, fen.ToString());
        }

        [TestMethod]
        public void TryParseTest()
        {
            Assert.IsTrue(Fen.TryParse(Constants.FEN_START_POS, out Fen fen));
            Assert.AreEqual(32, fen.Squares.Count);
            Assert.AreEqual(Color.White, fen.SideToMove);
            Assert.AreEqual(CastlingRights.All, fen.Castling);
            Assert.AreEqual(Index.NONE, fen.EnPassant);
            Assert.AreEqual(0, fen.HalfMoveClock);
            Assert.AreEqual(1, fen.FullMoveCounter);
        }
    }
}