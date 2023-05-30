using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pedantic.Chess;
using Index = Pedantic.Chess.Index;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class IndexTests
    {
        [TestMethod]
        public void IsValidTest()
        {
            int actual1 = -1;
            int actual2 = 14;

            Assert.IsFalse(Index.IsValid(actual1));
            Assert.IsTrue(Index.IsValid(actual2));
        }

        [TestMethod]
        public void ToIndexTest()
        {
            int actual = Index.ToIndex(3, 3);

            Assert.IsTrue(Index.IsValid(actual));
        }

        [TestMethod]
        public void FlipTest()
        {
            int actual = Index.A1;
            int flipped = Index.Flip(actual);

            Assert.AreEqual(Index.A8, flipped);

            actual = Index.C3;
            flipped = Index.Flip(actual);

            Assert.AreEqual(Index.C6, flipped);
        }

        [TestMethod]
        public void GetFileTest()
        {
            int file = Index.GetFile(Index.E5);
            Assert.AreEqual(4, file);
        }

        [TestMethod]
        public void GetRankTest()
        {
            int rank = Index.GetRank(Index.B3);
            Assert.AreEqual(2, rank);
        }

        [TestMethod]
        public void FileRankTupleToCoordsTest()
        {
            var actual = Index.ToCoords(Index.G7);
            Assert.AreEqual(6, actual.File);
            Assert.AreEqual(6, actual.Rank);
        }


        [TestMethod]
        public void ToStringTest()
        {
            string actual = Index.ToString(Index.B3);
            Assert.AreEqual("b3", actual);
        }

        [TestMethod]
        public void TryParseTest()
        {
            Assert.IsFalse(Index.TryParse("00", out int index1));
            Assert.IsFalse(Index.IsValid(index1));
            Assert.IsTrue(Index.TryParse("c5", out int index2));
            Assert.AreEqual(Index.C5, index2);
        }

        [TestMethod]
        [DataRow(Index.E1, Index.E8, KingPlacement.KK)]
        [DataRow(Index.G1, Index.C8, KingPlacement.KQ)]
        [DataRow(Index.C1, Index.G8, KingPlacement.QK)]
        [DataRow(Index.C1, Index.C8, KingPlacement.QQ)]
        public void GetKingPlacementTest(int friendlyKing, int opponentKing, KingPlacement expected)
        {
            KingPlacement placement = Index.GetKingPlacement(friendlyKing, opponentKing);
            Assert.AreEqual(expected, placement);
        }

        [TestMethod]
        [DataRow(Index.C1, 4)]
        [DataRow(Index.H4, 3)]
        public void CenterManhattanDistanceTest(int index, int expected)
        {
            int actual = Index.CenterManhattanDistance(index);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(Index.B3, Index.H7, 10)]
        [DataRow(Index.G2, Index.C5, 7)]
        public void ManhattanDistanceTest(int index1, int index2, int expected)
        {
            int actual = Index.ManhattanDistance(index1, index2);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(Index.B3, 2)]
        [DataRow(Index.H2, 3)]
        public void CenterDistanceTest(int index, int expected)
        {
            int actual = Index.CenterDistance(index);
            Assert.AreEqual(expected, actual);
        }
    }
}