using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;
using Pedantic.Genetics;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class EvalFeaturesTests
    {
        [TestMethod]
        [DataRow("4r3/ppR5/5pk1/3q2p1/3Pp1b1/2Q3P1/PP3P2/4R1K1 w - - 5 36")]
        [DataRow("8/3p1k2/4bppp/pq6/3P3P/R7/P1PQ4/2K5 b - - 2 43")]
        [DataRow("1q2r1k1/3b1ppp/pb1p1n2/1p6/4PB2/PN1B3P/1P2QPP1/2R3K1 b - - 2 23")]
        [DataRow("8/1k6/bp2pq2/p1p5/3PPp2/P1Q2P2/4r3/1KBR4 b - - 3 57")]
        [DataRow("r5k1/6p1/1qb1p1p1/1n1pP1Pp/1BpP3P/p1P1QP2/2P4K/R4N2 w - - 48 85")]
        [DataRow("rnbqkb1r/ppp2ppp/3p1n2/8/3NP3/2N5/PPP2PPP/R1BQKB1R b KQkq - 2 5")]
        [DataRow("8/3k1p2/3n2p1/p1pr2P1/1p3R1P/1P3N1K/P4P2/8 w - - 0 41")]
        [DataRow("r2q1rk1/4ppbp/pnp3p1/1p2Pb2/1Q1PN3/1PN2P2/P5PP/2BRK2R w K - 1 19")]
        [DataRow("rnbq1rk1/pp2ppbp/6p1/2p5/3PP3/2P2N2/P4PPP/1RBQKB1R w K - 2 9")]
        public void ComputeTest(string fen)
        {
            Board bd = new Board(fen);
            EvalFeatures features = new EvalFeatures(bd);

            short[] weights = Evaluation.Weights;

            const int vecSize = EvalFeatures.FEATURE_SIZE;
            ReadOnlySpan<short> opWeights = new ReadOnlySpan<short>(weights, 0, vecSize);
            ReadOnlySpan<short> egWeights = new ReadOnlySpan<short>(weights, vecSize, vecSize);

            Evaluation eval = new(false);
            short expected = eval.Compute(bd);
            short actual = features.Compute(opWeights, egWeights);
            Assert.AreEqual(expected, actual);
        }
    }
}