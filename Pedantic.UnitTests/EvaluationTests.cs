using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class EvaluationTests
    {
        [TestMethod]
        [DataRow("r6r/pp4kp/3B1p2/1B1P2p1/2P1q1n1/2Q3P1/PP6/5RK1 b - - 0 1", 0, 0, 2)]
        [DataRow("8/4pR1P/4p3/4b1Kp/1k2PpP1/4B2P/1p3P2/3Q4 b - - 0 1", 2, 3, 0)]
        public void CalcKingProximityAttacksTest(string fen, int expectedD1, int expectedD2, int expectedD3)
        {
            Board board = new(fen);
            Evaluation.CalcKingProximityAttacks(board, board.SideToMove, out int d1, out int d2, out int d3);
            Assert.AreEqual(expectedD1, d1);
            Assert.AreEqual(expectedD2, d2);
            Assert.AreEqual(expectedD3, d3);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 0, 0, 0, 8)]
        [DataRow("r1bk3r/ppppnp1p/2n4b/3N1q2/2B2p2/3P4/PPPBQ1PP/4RRK1 b - - 0 1", 12, 9, 3, 8)]
        public void CalcDevelopmentParametersTest(string fen, int expectedD, int expectedU, int expectedK, int expectedC)
        {
            Board board = new(fen);
            Evaluation.CalcDevelopmentParameters(board, board.SideToMove, out int d, out int u, out int k, out int c);
            Assert.AreEqual(expectedD, d);
            Assert.AreEqual(expectedU, u);
            Assert.AreEqual(expectedK, k);
            Assert.AreEqual(expectedC, c);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 0, 128, 0)]
        [DataRow("r1bk3r/ppppnp1p/2n4b/3N1q2/2B2p2/3P4/PPPBQ1PP/4RRK1 b - - 9 13", 1, 108, 20)]
        public void GetGamePhaseTest(string fen, Evaluation.GamePhase expectedPhase, int expectedOpWt, int expectedEgWt)
        {
            Board board = new(fen);
            Evaluation.GetGamePhase(board, out Evaluation.GamePhase gamePhase, out int opWt, out int egWt);
            Assert.AreEqual(expectedPhase, gamePhase);
            Assert.AreEqual(expectedOpWt, opWt);
            Assert.AreEqual(expectedEgWt, egWt);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 0)]
        [DataRow("r6r/pp4kp/3B1p2/3P2p1/B1P1q1n1/2Q3P1/PP6/5RK1 w - - 0 13", -68)]
        public void ComputeTest(string fen, int expectedScore)
        {
            Board board = new(fen);
            Evaluation eval = new();
            int score = eval.Compute(board);
            Assert.AreEqual(expectedScore, score);
        }

        [TestMethod]
        public void ComputeTest2()
        {
            Board board = new(Constants.FEN_START_POS);
            Evaluation eval = new();
            int scoreWhite = eval.Compute(board);

            board.LoadFenPosition(@"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1");
            int scoreBlack = eval.Compute(board);

            Assert.AreEqual(Math.Abs(scoreWhite), Math.Abs(scoreBlack));
        }

        [TestMethod]
        public void ComputeTest3()
        {
            Board board = new("    r2n2k1/3P3p/1R4p1/2B5/4p3/2P1P2P/p4rP1/2KR4 w - - 0 40");
            Evaluation eval = new();

            int scoreWhite = eval.Compute(board);
            board.LoadFenPosition("    r2n2k1/3P3p/1R4p1/2B5/4p3/2P1P2P/p4rP1/2KR4 b - - 0 40");
            int scoreBlack = eval.Compute(board);

            Assert.AreEqual(scoreWhite, -scoreBlack);
        }

    }
}