using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Genetics;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class EvolutionTests
    {
        [TestInitialize]
        public void Initialize()
        {
            using var rep = new GeneticsRepository();
            rep.DeleteAll();
        }

        [TestMethod]
        public void StartEvolutionTest()
        {
            Evolution.StartEvolution();
            using var rep = new GeneticsRepository();
            Evolution? ev = rep.Evolutions.FindOne(e => e.State == Evolution.EvolutionState.Evolving);
            Assert.IsNotNull(ev);
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(64, 64)]
        [DataRow(65, 64)]
        public void GetGamesToPlayTest(int maxGames, int expectedCount)
        {
            Evolution.StartEvolution();
            IEnumerable<GameToPlay> gamesToPlay = Evolution.GetGamesToPlay(maxGames);
            int count = gamesToPlay.Count();
            Assert.AreEqual(expectedCount, count);
        }

        [TestMethod]
        public void PostResultTest()
        {
            Evolution.StartEvolution();
            GameToPlay[] gamesToPlay = Evolution.GetGamesToPlay(64).ToArray();
            for (int n = 0; n < gamesToPlay.Length; ++n)
            {
                int result = n % 3;
                GameToPlay gameToPlay = gamesToPlay[n];
                int whiteScore = 0, blackScore = 0;
                switch (result)
                {
                    case 0:
                        whiteScore = 2;
                        break;

                    case 1:
                        blackScore = 2;
                        break;

                    case 2:
                        whiteScore = 1;
                        blackScore = 1;
                        break;
                }

                Evolution.PostResult(gameToPlay, whiteScore, blackScore, string.Empty);
            }

            using var rep = new GeneticsRepository();
            Evolution? e = rep.Evolutions
                .FindOne(e => e.State == Evolution.EvolutionState.Evolving);

            Assert.IsNotNull(e);

            Generation? g = rep.Generations
                .FindOne(g => g.Evolution.Id == e.Id && g.State == Generation.GenerationState.Round1);

            Assert.IsNotNull(g);
        }
    }
}