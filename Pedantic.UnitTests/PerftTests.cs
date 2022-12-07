using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pedantic.Chess;
using System;
using System.Diagnostics;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class PerftTests
    {
        public TestContext? TestContext { get; set; }

        [TestMethod]
        [DataRow(0, 1ul)]
        [DataRow(1, 20ul)]
        [DataRow(2, 400ul)]
        [DataRow(3, 8902ul)]
        [DataRow(4, 197281ul)]
        [DataRow(5, 4865609ul)]
        [DataRow(6, 119060324ul)]
        public void ExecuteTest(int depth, ulong expectedNodes)
        {
            Perft perft = new Perft();
            Stopwatch watch = new();
            watch.Start();
            ulong actual = perft.Execute(depth);
            watch.Stop();

            TestContext?.WriteLine($"Elapsed = {watch.Elapsed}");
            Assert.AreEqual(expectedNodes, actual);
        }

        [TestMethod]
        [DataRow("r3k2r/8/8/8/3pPp2/8/8/R3K1RR b KQkq e3 0 1", 6, 485647607ul)]
        [DataRow("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 6, 706045033ul)]
        [DataRow("8/7p/p5pb/4k3/P1pPn3/8/P5PP/1rB2RK1 b - d3 0 28", 6, 38633283ul)]
        [DataRow("8/3K4/2p5/p2b2r1/5k2/8/8/1q6 b - - 1 67", 7, 493407574ul)]
        [DataRow("rnbqkb1r/ppppp1pp/7n/4Pp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3", 6, 244063299ul)]
        [DataRow("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 5, 193690690ul)]
        [DataRow("8/p7/8/1P6/K1k3p1/6P1/7P/8 w - - 0 1", 8, 8103790ul)]
        [DataRow("n1n5/PPPk4/8/8/8/8/4Kppp/5N1N b - - 0 1", 6, 71179139ul)]
        [DataRow("r3k2r/p6p/8/B7/1pp1p3/3b4/P6P/R3K2R w KQkq - 0 1", 6, 77054993ul)]
        [DataRow("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 7, 178633661ul)]
        [DataRow("8/5p2/8/2k3P1/p3K3/8/1P6/8 b - - 0 1", 8, 64451405ul)]
        [DataRow("r3k2r/pb3p2/5npp/n2p4/1p1PPB2/6P1/P2N1PBP/R3K2R w KQkq - 0 1", 5, 29179893ul)]
        public void Execute2Test(string position, int depth, ulong expectedNodes)
        {
            Perft perft = new Perft();
            perft.Initialize(position);
            ulong actual = perft.Execute(depth);
            Assert.AreEqual(expectedNodes, actual);
        }
    }
}