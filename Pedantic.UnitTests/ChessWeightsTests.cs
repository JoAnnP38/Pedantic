using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using LiteDB;
using Pedantic.Genetics;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class ChessWeightsTests
    {
        [TestMethod]
        public void CreateParagonTest()
        {
            ChessWeights cw = ChessWeights.CreateParagon();
            Assert.IsTrue(cw.Id != ObjectId.Empty);
            Assert.IsTrue(cw.IsActive);
            Assert.IsTrue(cw.IsImmortal);
            Assert.AreEqual(ChessWeights.MAX_WEIGHTS, cw.Weights.Length);
            Assert.AreEqual(cw.CreatedOn, cw.UpdatedOn);
        }

        [TestMethod]
        public void LoadParagonTest()
        {
            Assert.IsTrue(ChessWeights.LoadParagon(out ChessWeights paragon));
            Assert.AreNotEqual(ChessWeights.Empty, paragon);
            Assert.IsTrue(paragon.Id != ObjectId.Empty);
            Assert.IsTrue(paragon.IsActive);
            Assert.IsTrue(paragon.IsImmortal);
            Assert.AreEqual(ChessWeights.MAX_WEIGHTS, paragon.Weights.Length);
        }

    }
}