using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Transactions;
using Pedantic.Chess;
using Pedantic.Utilities;
using Index = Pedantic.Chess.Index;
using Pedantic.Tablebase;
using static Pedantic.PgnPositionReader;
using Pedantic.Collections;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class HistoryTests
    {
        private History history = new();
        private SearchStack searchStack = new();

        [TestInitialize]
        public void InitTest()
        {
            history.Clear();
            searchStack.Clear();
        }

        [TestMethod]
        public void CounterMoveTest()
        {
            // set the previous move on the search stack
            searchStack[-1].Move = (uint)Move.Pack(Color.White, Piece.Pawn, Index.D2, Index.D4, MoveType.DblPawnMove);

            StackList<uint> quiets = new(stackalloc uint[8]);
            ulong cutoffMove = Move.Pack(Color.Black, Piece.Pawn, Index.D7, Index.D5, MoveType.DblPawnMove);

            // update Cutoff
            history.UpdateCutoff(cutoffMove, 0, ref quiets, searchStack, 2);

            MovePair counters = history.CounterMoves(searchStack[-1].Move);

            Assert.IsTrue(Move.Compare(counters.Move1, cutoffMove) == 0);
        }

        [TestMethod]
        public void IndexerTest()
        {
            // set the previous move on the search stack
            searchStack[-1].Move = (uint)Move.Pack(Color.White, Piece.Pawn, Index.D2, Index.D4, MoveType.DblPawnMove);

            StackList<uint> quiets = new(stackalloc uint[8]);
            ulong cutoffMove = Move.Pack(Color.Black, Piece.Pawn, Index.D7, Index.D5, MoveType.DblPawnMove);

            // update Cutoff
            history.UpdateCutoff(cutoffMove, 0, ref quiets, searchStack, 2);

            short historyValue = history[Color.Black, Piece.Pawn, Index.D5];
            short bonusValue = CalcBonus(2);
            Assert.AreEqual(bonusValue, historyValue);

            historyValue = history[cutoffMove];
            Assert.AreEqual(bonusValue, historyValue);
        }

        [TestMethod]
        public void UpdateCutoffTest()
        {
            // set the previous move on the search stack
            searchStack[-1].Move = (uint)Move.Pack(Color.White, Piece.Pawn, Index.D2, Index.D4, MoveType.DblPawnMove);

            StackList<uint> quiets = new(stackalloc uint[8]);
            ulong quietMove = Move.Pack(Color.Black, Piece.Pawn, Index.E7, Index.E5, MoveType.DblPawnMove);
            quiets.Add((uint)quietMove);

            ulong cutoffMove = Move.Pack(Color.Black, Piece.Pawn, Index.D7, Index.D5, MoveType.DblPawnMove);

            // update Cutoff
            history.UpdateCutoff(cutoffMove, 0, ref quiets, searchStack, 2);        

            short bonusValue = CalcBonus(2);

            short historyValue = history[quietMove];
            Assert.AreEqual(-bonusValue, historyValue);
        }

        [TestMethod]
        public void ClearTest()
        {
            // set the previous move on the search stack
            searchStack[-1].Move = (uint)Move.Pack(Color.White, Piece.Pawn, Index.D2, Index.D4, MoveType.DblPawnMove);

            StackList<uint> quiets = new(stackalloc uint[8]);
            ulong quietMove = Move.Pack(Color.Black, Piece.Pawn, Index.E7, Index.E5, MoveType.DblPawnMove);
            quiets.Add((uint)quietMove);

            ulong cutoffMove = Move.Pack(Color.Black, Piece.Pawn, Index.D7, Index.D5, MoveType.DblPawnMove);

            // update Cutoff
            history.UpdateCutoff(cutoffMove, 0, ref quiets, searchStack, 2);        

            history.Clear();

            Assert.AreEqual(0, history[quietMove]);
            Assert.AreEqual(0, history[cutoffMove]);
        }

        [TestMethod]
        public void UpdateHistoryTest()
        {
            short historyValue = 0;
            short bonus = CalcBonus(2);

            History.UpdateHistory(ref historyValue, bonus);
            Assert.AreEqual(bonus, historyValue);

            historyValue = 0;
            History.UpdateHistory(ref historyValue, (short)-bonus);
            Assert.AreEqual((short)-bonus, historyValue);

            // test dampening or saturation
            historyValue = Constants.HISTORY_SCORE_MAX / 2;
            History.UpdateHistory(ref historyValue, bonus);

            Assert.AreEqual(Constants.HISTORY_SCORE_MAX / 2 + bonus - bonus / 2, historyValue);

            // test dampening or saturation of negative bonuses
            historyValue = -(Constants.HISTORY_SCORE_MAX / 2);
            bonus = (short)-bonus;
            History.UpdateHistory(ref historyValue, bonus);

            Assert.AreEqual(-Constants.HISTORY_SCORE_MAX / 2 + bonus - bonus / 2, historyValue);
        }

        [TestMethod]
        public void GetIndexTest()
        {
            int index = History.GetIndex(Color.Black, Piece.King, Index.H8);
            Assert.AreEqual(History.HISTORY_LEN - 1, index);

            index = History.GetIndex(Color.White, Piece.Pawn, Index.A1);
            Assert.AreEqual(0, index);
        }

        private short CalcBonus(int depth)
        {
            // copied from History.cs
            return (short)(((depth * depth) >> 1) + (depth << 1) - 1);
        }
    }
}
