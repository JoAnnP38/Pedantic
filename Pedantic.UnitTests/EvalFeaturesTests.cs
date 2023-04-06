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
        [DataRow("8/1p2nkp1/p2b1p2/3p4/r7/2PKBN2/1P3PP1/3R4 b - - 5 39")]
        [DataRow("r5k1/5pp1/2qp3p/1p1n4/3P1p1P/PP1Q1N2/5PP1/R5K1 w - - 3 39")]
        [DataRow("1n6/1P4K1/8/4Pk2/5P2/8/8/8 w - - 3 83")]
        [DataRow("3q2k1/1p3pp1/p1p4p/4b3/PPB1P3/5P1P/4QPK1/8 b - b3 0 28")]
        [DataRow("8/8/5kp1/3K3p/5P1P/3Q2P1/8/4q3 b - - 42 80")]
        [DataRow("8/6R1/4k3/1r4p1/6P1/5PK1/8/8 w - - 15 63")]
        [DataRow("7k/1ppn2pp/p1p1bp2/4p3/4P3/1PP1N3/P4PPP/R5K1 w - - 0 22")]
        [DataRow("r1bqr1k1/p1p2ppp/1bpp1n2/8/3BP3/2PB1Q2/PP1N1PPP/R4RK1 b - - 5 12")]
        [DataRow("4k3/b2q1pp1/4p1nr/3pPnN1/PPr2P1p/2PQ4/3BN1PP/R4R1K w - - 3 23 ")]
        [DataRow("8/8/1k6/1r6/4K3/8/8/8 w - - 2 90")]
        [DataRow("8/8/1k6/1r6/3K4/8/8/8 b - - 3 90")]
        [DataRow("2K5/4r3/5k2/8/8/8/8/8 b - - 9 81")]
        public void ComputeTest(string fen)
        {
            Evaluation.LoadWeights("641a9312a0624206d2bc8ab9");
            Board bd = new Board(fen);
            EvalFeatures features = new EvalFeatures(bd);

            short[] weights = Evaluation.Weights;

            const int vecSize = EvalFeatures.FEATURE_SIZE;
            ReadOnlySpan<short> opWeights = new ReadOnlySpan<short>(weights, 0, vecSize);
            ReadOnlySpan<short> egWeights = new ReadOnlySpan<short>(weights, vecSize, vecSize);

            Evaluation eval = new(false, false);
            short expected = eval.Compute(bd);
            short actual = features.Compute(opWeights, egWeights);
            Assert.AreEqual(expected, actual);
        }
    }
}