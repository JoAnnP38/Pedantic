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
        public void CreateRandomTest()
        {
            ChessWeights cw = ChessWeights.CreateRandom();
            Assert.IsTrue(cw.Id != ObjectId.Empty);
            Assert.IsTrue(cw.IsActive);
            Assert.IsFalse(cw.IsImmortal);
            Assert.AreEqual(0, cw.ParentIds.Length);
            Assert.AreEqual(0, cw.Wins);
            Assert.AreEqual(0, cw.Losses);
            Assert.AreEqual(0, cw.Draws);
            Assert.AreEqual(ChessWeights.MAX_WEIGHTS, cw.Weights.Length);
            Assert.AreEqual(cw.CreatedOn, cw.UpdatedOn);
        }

        [TestMethod]
        public void CrossOverTest()
        {
            ChessWeights parent1 = ChessWeights.CreateRandom();
            ChessWeights parent2 = ChessWeights.CreateRandom();
            Assert.AreNotSame(parent1, parent2);
           
            var result = ChessWeights.CrossOver(parent1, parent2, true);
            Assert.AreNotSame(result.child1, result.child2);

            Assert.IsTrue(result.child1.Id != ObjectId.Empty);
            Assert.IsTrue(result.child1.IsActive);
            Assert.IsFalse(result.child1.IsImmortal);
            Assert.AreEqual(2, result.child1.ParentIds.Length);
            Assert.AreEqual(parent1.Id, result.child1.ParentIds[0]);
            Assert.AreEqual(parent2.Id, result.child1.ParentIds[1]);
            Assert.AreEqual(0, result.child1.Wins);
            Assert.AreEqual(0, result.child1.Losses);
            Assert.AreEqual(0, result.child1.Draws);
            Assert.AreEqual(ChessWeights.MAX_WEIGHTS, result.child1.Weights.Length);
            Assert.AreEqual(result.child1.CreatedOn, result.child1.UpdatedOn);

            Assert.IsTrue(result.child2.Id != ObjectId.Empty);
            Assert.IsTrue(result.child2.IsActive);
            Assert.IsFalse(result.child2.IsImmortal);
            Assert.AreEqual(2, result.child2.ParentIds.Length);
            Assert.AreEqual(parent1.Id, result.child2.ParentIds[0]);
            Assert.AreEqual(parent2.Id, result.child2.ParentIds[1]);
            Assert.AreEqual(0, result.child2.Wins);
            Assert.AreEqual(0, result.child2.Losses);
            Assert.AreEqual(0, result.child2.Draws);
            Assert.AreEqual(ChessWeights.MAX_WEIGHTS, result.child2.Weights.Length);
            Assert.AreEqual(result.child2.CreatedOn, result.child2.UpdatedOn);
        }

        [TestMethod]
        public void CreateParagonTest()
        {
            ChessWeights cw = ChessWeights.CreateParagon();
            Assert.IsTrue(cw.Id != ObjectId.Empty);
            Assert.IsTrue(cw.IsActive);
            Assert.IsTrue(cw.IsImmortal);
            Assert.AreEqual(0, cw.ParentIds.Length);
            Assert.AreEqual(0, cw.Wins);
            Assert.AreEqual(0, cw.Losses);
            Assert.AreEqual(0, cw.Draws);
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
            Assert.AreEqual(0, paragon.ParentIds.Length);
            Assert.AreEqual(0, paragon.Wins);
            Assert.AreEqual(0, paragon.Losses);
            Assert.AreEqual(0, paragon.Draws);
            Assert.AreEqual(ChessWeights.MAX_WEIGHTS, paragon.Weights.Length);
        }

    }
}