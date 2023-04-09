using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
            Assert.IsTrue(cw.Id != Guid.Empty);
            Assert.IsTrue(cw.IsActive);
            Assert.IsTrue(cw.IsImmortal);
            Assert.AreEqual(ChessWeights.MAX_WEIGHTS, cw.Weights.Length);
        }

        [TestMethod]
        public void LoadParagonTest()
        {
            Assert.IsTrue(ChessWeights.LoadParagon(out ChessWeights paragon));
            Assert.AreNotEqual(ChessWeights.Empty, paragon);
            Assert.IsTrue(paragon.Id != Guid.Empty);
            Assert.IsTrue(paragon.IsActive);
            Assert.IsTrue(paragon.IsImmortal);
            Assert.AreEqual(ChessWeights.MAX_WEIGHTS, paragon.Weights.Length);
        }

    }
}