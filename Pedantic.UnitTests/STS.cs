using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class STS
    {
        [TestMethod]
        [DataRow("1kr5/3n4/q3p2p/p2n2p1/PppB1P2/5BP1/1P2Q2P/3R2K1 w - - 0 1")]
        public void StsPositionTest(string fen)
        {
            //Engine.Infinite = true;
            Engine.SearchType = SearchType.Mtd;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand($"position fen {fen}");
            Program.ParseCommand("go movetime 6000");
            Engine.Wait();
        }
    }
}