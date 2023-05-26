using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Transactions;
using Pedantic.Chess;
using Pedantic.Utilities;
using Index = Pedantic.Chess.Index;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public void ParseCommandTest()
        {
            Program.ParseCommand("position fen r6r/pp4kp/3B1p2/3P2p1/B1P1q1n1/2Q3P1/PP6/5RK1 w - - 0 13 moves f1e1");
            Console.WriteLine(Engine.Board.ToString());
        }

        [TestMethod]
        public void GoTest()
        {
            Program.ParseCommand(@"position startpos moves e2e4");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand(@"go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        public void GoBookMoveTest()
        {
            TimeControl time = new();
            time.Reset();
            Engine.UseOwnBook = true;
            Program.ParseCommand(@"position startpos moves e2e4");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand(@"go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        public void GoBookMove2Test()
        {
            Engine.UseOwnBook = true;
            Program.ParseCommand("position startpos moves e2e4 e7e5 g1f3");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go wtime 293797 btime 300000 winc 0 binc 0 movestogo 39");
            Engine.Wait();
        }

        [TestMethod]
        public void GoBookMove3Test()
        {
            Engine.UseOwnBook = true;
            Program.ParseCommand(
                "position fen rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1 moves e7e5 g1f3");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go wtime 293797 btime 300000 winc 0 binc 0 movestogo 39");
            Engine.Wait();
        }

        [TestMethod]
        public void GoBookMove4Test()
        {
            Engine.UseOwnBook = true;
            Program.ParseCommand(
                "position startpos moves e2e4 c7c5 g1f3 b8c6 d2d4 c5d4 f3d4 g8f6 b1c3 e7e5 d4b5 d7d6 c1g5 a7a6 b5a3 b7b5 g5f6 g7f6 c3d5 f6f5 f1d3 c8e6 e1g1 f8g7 d1h5 f5f4 c2c4 b5c4 d3c4");
            Console.WriteLine(Engine.Board.ToString());
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 209886 btime 203893 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void IndexOutOfRangeRecreateTest()
        {
            Program.ParseCommand(
                "position startpos moves e2e4 c7c6 d2d4 d7d5 b1c3 d5e4 c3e4 g8f6 e4f6 g7f6 g1f3 c8g4 f1e2 d8c7 h2h3 g4e6 e1g1 b8a6 e2a6 b7a6 d1e2");
            Program.ParseCommand("go wtime 152586 btime 126096 winc 6000 binc 6000");
            Thread.Sleep(100);
            Engine.Stop();
        }

        [TestMethod]
        public void NotStartingTest()
        {
            Program.ParseCommand("position startpos");
            Program.ParseCommand("go wtime 152586 btime 126096 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void InfiniteLoopTest()
        {
            try
            {
                Engine.ResizeHashTable(128);
                Engine.LoadBookEntries();
                Program.ParseCommand(
                    "position startpos moves e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 b1c3 b8c6 f1c4 e7e6 c1e3 f8e7 c4b3 e8g8 f2f4 c8d7 d4b5 d8b8 a2a4 a7a6 b5a3 c6a5 e3b6 a5b3 c2b3 d7c6 e1g1 f6e4 a3c4 e7d8 d1d4 f7f5 c3e4 f5e4 b6d8 f8d8 c4b6 b8a7 a4a5 a8c8 f4f5 e6e5 d4d1 c8c7 d1g4 c7f7 f5f6 a7b8 a1e1 d8e8 f1f2 d6d5 f6g7 f7g7 g4h4 d5d4 e1e4 c6e4 h4e4 b8d6 b6c4 d6c6 e4c6 b7c6 f2f6 g7g6 f6f5 e5e4 f5e5 e8e5 c4e5 g6e6 e5c4 e4e3 g1f1 e3e2 f1e1 h7h5 h2h3 d4d3");
                Console.WriteLine(Engine.Board.ToFenString());
                Program.ParseCommand("go wtime 43866 btime 36143 winc 6000 binc 6000");
                Engine.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    Console.WriteLine(e.ToString());
                }

                Assert.Fail($"Test case failed: {ae.Message}");
            }
        }

        [TestMethod]
        public void BadBookMoveTest()
        {
            Engine.UseOwnBook = true;
            Engine.LoadBookEntries();
            Program.ParseCommand(
                "position startpos moves d2d4 g8f6 g1f3 e7e6 c2c4 b7b6 g2g3 c8b7 f1g2 f8e7 b1c3 f6e4 c1d2 e7f6 e1g1");
            Console.WriteLine($"FEN: {Engine.Board.ToFenString()}");
            Console.WriteLine($"Hash: 0x{Engine.Board.Hash:X16}ul");
            Program.ParseCommand("go wtime 167364 btime 160872 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        [DataRow("6k1/5ppp/p7/P7/5b2/7P/1r3PP1/3R2K1 w - - 0 1")]
        [DataRow("r1bqkb1r/pppp1ppp/2n2n2/3Q4/2B1P3/8/PB3PPP/RN2K1NR w KQkq - 0 1")]
        public void CheckMateInOneTest(string fen)
        {
            //Engine.Infinite = true;
            Engine.UseOwnBook = true;
            Program.ParseCommand(@$"position fen {fen}");
            Program.ParseCommand("go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        [DataRow("2r2r2/p6p/3R4/4k1p1/2B5/5pP1/P4K1P/3R4 w - - 0 1")]
        [DataRow("1rr3k1/5p2/4p1pQ/pp1n2Np/7P/1P5P/2q2P2/R1B3K1 w - - 0 1")]
        public void CheckMateInTwoTest(string fen)
        {
            //Engine.Infinite = true;
            Engine.UseOwnBook = true;
            Program.ParseCommand(@$"position fen {fen}");
            Program.ParseCommand("go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        [DataRow("r1b2r1k/pp4pp/3p4/3B4/8/1QN3Pn/PP3q1P/R3R2K b - - 0 1")]
        [DataRow("2r3k1/p4p2/3Rp2p/1p2P1pK/8/1P4P1/P3Q2P/1q6 b - - 0 1")]
        [DataRow("8/1n3Np1/1N4Q1/1bkP4/p1p2p2/P1P2R2/3P2PK/B2R4 w - - 0 1")]
        public void CheckMateInThreeTest(string fen)
        {
            //Engine.Infinite = true;
            Engine.UseOwnBook = true;
            Program.ParseCommand(@$"position fen {fen}");
            Program.ParseCommand("go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        [DataRow("rn1k4/ppq3pp/3p1bb1/2pP1Nn1/N1P1Q1B1/1P2R3/P6r/2K1R3 w - - 0 1")]
        public void CheckMateInFourTest(string fen)
        {
            //Engine.Infinite = true;
            Engine.UseOwnBook = true;
            Program.ParseCommand(@$"position fen {fen}");
            Program.ParseCommand("go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        public void ShortenedPVTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand(
                "position startpos moves e2e4 c7c5 g1f3 e7e6 d2d4 c5d4 f3d4 a7a6 f1d3 b8c6 d4c6 d7c6 b1c3 d8c7 c1e3 g8f6 c3a4 c6c5 c2c4 f8d6 d1b3 f6d7 h2h3 e8g8 e1g1 b7b6 f2f4 c8b7");
            Program.ParseCommand("go wtime 120152 btime 150887 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void InfiniteLoop2Test()
        {
            Program.ParseCommand(
                "position startpos moves d2d4 g8f6 c2c4 g7g6 b1c3 f8g7 e2e4 d7d6 f1e2 e8g8 c1g5 a7a6 g1f3 c7c6 h2h3 b7b5 a2a3 b5c4 e4e5 d6e5 d4e5 f6d5 e2c4 c8e6 d1d2 b8d7 c3d5 c6d5");
            Program.ParseCommand("go wtime 129205 btime 145079 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void UnexpectedExceptionTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand(
                "position startpos moves d2d4 g8f6 c2c4 e7e6 g1f3 d7d5 b1c3 f8e7 c1f4 e8g8 e2e3 c7c5 d4c5 e7c5 d1c2 b8c6 a1d1 f6h5");
            Program.ParseCommand("go wtime 172899 btime 73920 winc 6000 binc 6000");
            Engine.Wait();

        }

        [TestMethod]
        public void UnexpectedHugeValue2Test()
        {
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand(
                "position startpos moves e2e4 e7e5 b1c3 g8f6 f1c4 b8c6 d2d3 c6a5 g1e2 c7c6 c4f7 e8f7 c1e3 d7d5 e4d5 c6d5");
            Program.ParseCommand("go wtime 141982 btime 83680 winc 6000 binc 6000");
            Engine.Wait();

        }

        [TestMethod]
        public void UnexpectedException2Test()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand(
                "position startpos moves e2e4 e7e5 g1f3 b8c6 f1c4 g8f6 d2d3 h7h6 e1g1 d7d6 b1c3 f8e7 h2h3 e8g8 a2a3 c8e6 c3d5 c6d4 f3d4 e5d4 d5f6 e7f6 c4e6 f7e6 d1g4 g8f7 f2f4 e6e5 a1b1 d8e8 f4e5 d6e5 c1d2 a8c8 f1f5 g7g6 f5f6 f7f6 d2h6 f8g8 c2c4");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go wtime 28277 btime 11250 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void PonderTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand(
                "position startpos moves e2e4 e7e6 d2d4 d7d5 b1c3 f8b4 e4e5 c7c5 a2a3 b4c3 b2c3 b8c6 d1g4 g7g6 g1f3 d8a5 c1d2 a5a4 f1e2");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go ponder wtime 26610 btime 22770 winc 12 binc 12");
            Thread.Sleep(1000);
            Program.ParseCommand("ponderhit");
            Engine.Wait();
            Program.ParseCommand("isready");
        }

        [TestMethod]
        public void InvalidPVMoveTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves d2d4 d7d6 e2e4 g8f6 b1c3 g7g6 h2h3 f8g7 c1e3 e8g8 f2f4 d6d5 e4e5 f6e4 c3e4 d5e4 f1c4 c7c6 g1e2 a7a5 e2g3 b7b5 c4b3 a5a4 e1g1 a4b3 c2b3 f7f5");
            Console.WriteLine(Engine.Board.ToString());
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 119768 btime 125570 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void IgnoreQueenPromotionTest()
        {
            //Engine.CollectStats = true;
            //Engine.Infinite = true;
            Engine.SearchThreads = 1;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand(
                "position startpos moves d2d4 g8f6 c2c4 e7e6 g1f3 c7c5 d4d5 d7d6 b1c3 e6d5 c4d5 g7g6 c1f4 a7a6 a2a4 f8g7 h2h3 e8g8 e2e3 f6h5 f4h2 g7c3 b2c3 h5f6 d1b3 b7b6 e1c1 f6e4 b3c2 f7f5 f1d3 d8f6 d3e4 f5e4 f3d2 f6f2 h2d6 f8e8 d2c4 f2c2 c1c2 b8d7 d1b1 b6b5 c4a5 b5a4 a5c6 a6a5 c6e7 g8h8 e7c8 a8c8 b1b5 a4a3 h1a1 d7e5 d6c5 e5d3 c5d4 h8g8 b5b7 d3e5 a1a3 c8a8 a3a1 a5a4 d5d6 e5f7 d6d7 e8f8 a1d1 f7d8 b7b6 f8f2 c2c1 a4a3 d4c5 a3a2");
            Console.WriteLine(Engine.Board.ToString());
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 89698 btime 86829 winc 6000 binc 6000");
            Engine.Wait();

            ulong move = Move.Pack(Index.B6, Index.B1);
            //Engine.Infinite = true;
            Engine.Board.MakeMove(move);
            Program.ParseCommand("go wtime 89698 btime 86829 winc 6000 binc 6000");
            Engine.Wait();
            
            move = Move.Pack(Index.A2, Index.A1, MoveType.Promote, promote: Piece.Queen);
            Engine.Board.MakeMove(move);
            Program.ParseCommand("go wtime 89698 btime 86829 winc 6000 binc 6000");

            Engine.Wait();
        }


        [TestMethod]
        public void IndexOutOfRangeTest()
        {
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand(
                "position startpos moves e2e4 e7e6 d2d4 d7d5 b1d2 g8f6 e4e5 f6d7 f1d3 c7c5 c2c3 b8c6 g1e2 c5d4 c3d4 f7f6 e5f6 d7f6 e1g1 f8d6");
            Console.WriteLine(Engine.Board.ToString());
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 179906 btime 179947 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void NoPVDuringPonderTest()
        {
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves e2e4 g7g6 d2d4 d7d6 c2c4 g8f6 b1c3 f8g7 g1f3 e8g8 f1e2 e7e5 d4e5 d6e5 d1d8 f8d8 c1g5 c8g4 c3d5 b8d7 d5c7 a8b8 c7d5 h7h6 g5h4 g6g5 f3g5 h6g5 h4g5 g4e2 e1e2 d8e8 e2f3 b8c8 h1c1 a7a5 g5d2 d7c5 d5f6 g7f6 d2a5 g8g7 h2h3 e8h8 a5b6 c5a4 b6e3 a4b2 a1b1 b2c4 b1b7 c4e3 c1c8 h8c8 f2e3 c8a8 b7b2 a8a3 b2e2 f6e7 e2c2 e7g5 c2e2 g7g6 f3f2 g5f6 g2g4 f6g5 f2f3 g6g7 f3f2 g7f6 f2f3 f6e7 f3f2 e7d7 f2f3 d7c6");
            Program.ParseCommand("go ponder wtime 123099 btime 42017 winc 6000 binc 6000");
            Thread.Sleep(5000);
            Program.ParseCommand("ponderhit");
            Engine.Wait();
        }

        [TestMethod]
        public void InvalidPositionInPonderText()
        {
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves c2c4 g8f6 g1f3 c7c5 b1c3 d7d5 c4d5 f6d5 d2d4 e7e6 g2g3 b8c6 f1g2 c5d4 f3d4 d5c3 b2c3 c6d4 d1d4 d8d4 c3d4 f8b4 c1d2 b4d6 h1g1 e8g8 d2e3 a8b8 e1d2 a7a5 a1b1 f8d8 g2e4 a5a4 g1c1 b7b5 e3g5 f7f6 g5e3 b5b4 c1c6 a4a3 h2h3 h7h6 e4d3 g7g5");
            Program.ParseCommand("go wtime 145759 btime 81533 winc 6000 binc 6000");
            Engine.Wait();

            Engine.Debug = true;
            //Program.ParseCommand("position startpos moves c2c4 g8f6 g1f3 c7c5 b1c3 d7d5 c4d5 f6d5 d2d4 e7e6 g2g3 b8c6 f1g2 c5d4 f3d4 d5c3 b2c3 c6d4 d1d4 d8d4 c3d4 f8b4 c1d2 b4d6 h1g1 e8g8 d2e3 a8b8 e1d2 a7a5 a1b1 f8d8 g2e4 a5a4 g1c1 b7b5 e3g5 f7f6 g5e3 b5b4 c1c6 a4a3 h2h3 h7h6 e4d3 g7g5 d3e4 h3h4");
        }

        [TestMethod]
        public void IncorrectMaterialEvaluationTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("position startpos moves e2e4 d7d5 e4d5 d8d5 b1c3 d5e6 f1e2 e6g6 e2c4 g6g2 d1h5 g2h1 h5f7 e8d7 d2d4 h1g1 e1e2 g1g4 e2f1 g8f6");
            Program.ParseCommand("go depth 1");
            Engine.Wait();
        }

        [TestMethod]
        public void UnexpectedException4Test()
        {
            Program.ParseCommand("position startpos moves d2d4 d7d5 c2c4 d5c4 g1f3 b7b5 b1c3 c8a6 b2b3 b5b4 c3e4 a6b7 e4c5 b7f3 e2f3 e7e5 f1c4 e5d4 c4b5 c7c6 d1e2 d8e7 c5a6 e8d7 a6b8 a8b8 b5c4 e7f6 e2d3 b8e8 e1f1 d7c8 h2h4 h7h6 c1b2 c6c5 c4d5 e8e7 d3c4 c8d8 a2a3 b4a3 b2a3 e7c7 a3b2 c7d7 a1a6 f6f5 d5c6 d7e7 c6e4 f5e5 a6a5 e7d7 e4c6 d7e7 c6e4 e7d7 e4c6 d7c7 c6d5 e5f4 b2c1 f4f5 f1g1 c7d7 d5c6 d7c7 c6d5 f8d6 a5a6 d6e5 d5e4 f5d7 c1a3 e5d6 c4d5 d8e7 d5a8 g7g6 e4d5 d7f5 d5e4 f5d7 e4d5 d7f5 d5e4 f5e5 e4d3 c7d7 g2g3 d7c7 d3b5 h6h5 a8e8 e7f6 e8d8 f6g7 a6d6 c7b7 d6d5 e5f6 d8f6 g8f6 d5c5 f6d7 c5d5 d7f6 d5g5 d4d3 g1g2 h8b8 b5c4 d3d2 a3b2 b7d7 h1d1 b8e8 b2c3 e8e1 d1d2 d7d2 c3d2 e1a1 d2c3 a1d1 g5a5 d1d6 a5a7 g7g8 c4f7 g8h7 f7e6 d6d7 e6d7 f6d7 a7a8 d7f6");
            Program.ParseCommand("go ponder wtime 23172 btime 52317 winc 6000 binc 6000");
            Thread.Sleep(1000);
            Program.ParseCommand("stop");
            Engine.Wait();
        }

        [TestMethod]
        [DataRow("5rk1/1ppb3p/p1pb4/6q1/3P1p1r/2P1R2P/PP1BQ1P1/5RKN w - - 0 1", 9)]
        [DataRow("1k3r2/1p4p1/p3p1Np/3b1p2/1bq5/2P2P2/PP1Q1PBP/1K1R2R1 w - - 5 27", 9)]
        [DataRow("r1b1kb1r/3q1ppp/pBp1pn2/8/Np3P2/5B2/PPP3PP/R2Q1RK1 w kq - 0 1 ", 9)]
        [DataRow("8/k1b5/P4p2/1Pp2p1p/K1P2P1P/8/3B4/8 w - - 0 1", 19)]
        public void IncorrectMoveTest(string fen, int depth)
        {
            try
            {
                Program.ParseCommand($"position fen {fen}");
                Program.ParseCommand($"go depth {depth}");
                Engine.Wait();
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.ToString());
                Assert.Fail("Unexpected exception");
            }
        }

        [TestMethod]
        public void TimeNotUsedTest()
        {
            Engine.MovesOutOfBook = 11;
            Program.ParseCommand("position startpos moves g2g3 h7h5 d2d4 h5h4 g3h4 g8f6 c1g5 d7d5 e2e3 g7g6 g5f6 e7f6 g1f3 c7c6 b1d2 a7a5 f1g2 a5a4 c2c3 a4a3 b2b4 c8e6 a1b1");
            Program.ParseCommand("go wtime 117306 btime 81754 winc 6000 binc 6000");
            Engine.Wait();
        }

        // The test below takes longer than 30s to execute in DEBUG mode.
        /*
        [TestMethod]
        public void InvalidUnderPromotionTest()
        {
            Engine.ClearHashTable();
            Program.ParseCommand("position startpos moves d2d4 d7d5 g1f3 g8f6 c2c4 e7e6 b1c3 c7c6 e2e3 b8d7 f1d3 d5c4 d3c4 b7b5 c4d3 c8b7 e1g1 f8d6 e3e4 e6e5 d4d5 b5b4 d5c6 b7c6 c3d5 e8g8 f1e1 h7h6 d5f6 d7f6 b2b3 d8b6 c1e3 b6b7 f3d2 f8d8 a1c1 a7a5 f2f3 f6h5 g2g3 h5f6 d2c4 d6c7 d1c2 a5a4 e1d1 c6d7 g1g2 a4a3 e3c5 d7e6 c2d2 e6c4 c1c4 c7a5 d2e1 a8b8 h2h4 d8d7 e1e2 b8c8 c4c2 b7b8 d1c1 c8d8 d3a6 d7c7 g3g4 f6h7 a6c4 h7f8 c4d5 d8c8 h4h5 f8e6 d5e6 f7e6 g2g3 c7c6 g3g2 g8h7 c2c4 b8b5 e2c2 a5d8 c5e3 d8b6 e3b6 c6c4 b3c4 b5b6 c4c5 b6d8 c5c6 c8c7 g2g3 d8d4 c2c4 d4d2 c1c2 d2e1 g3g2 e1d1 c2f2 d1d6 f2c2 d6d1 g2f2 d1h1 c4f1 h1h2 f1g2 h2h4 g2g3 h4d8 g3e5 d8d1 e5c7 d1c2 f2g3 b4b3 g4g5 h6g5 a2b3 c2c5 h5h6 h7h6 g3g4 c5g1 c7g3 g1g3 g4g3 a3a2 c6c7 a2a1q c7c8q a1g1 g3h3 g5g4 f3g4 g1e3 h3h2 e3e4 c8h8 h6g6 h8h5 g6f6 h5h4 f6f7 h4h5 g7g6 h5h7 f7e8 h7g8 e8e7 g8g7 e7e8 h2g3 e4e1 g3h3 e1h1 h3g3 h1e1 g3f4 e1d2 f4f3 d2d1 f3f4 d1d2 f4f3 g6g5 g7g6 e8e7 g6g7 e7d6 g7f8 d6d7 f8f7 d7d6 f7f8 d6d7 f8b8 d2d1 f3g3 d1g1 g3f3 g1f1 f3g3 f1d3 g3g2 d3e4 g2g3 e4e3 g3g2 e3e2 g2g3 e2e1 g3g2 e1e2 g2g3 e2e1 g3f3 e1c3 f3e2 c3c2 e2e3 c2c5 e3d3 c5d5 d3e3 d5c5 e3e2 c5d5 b8b4 d7e8 b4c4 d5g2 e2d3 g2g3 d3d2 e8f7 b3b4 g3f2 d2c3 f2g3 c3b2 g3g2 b2a3 g2f3 a3a4 f3f4 a4b5 f4b8 b5a6 b8a8 a6b6 a8b8 b6a5 b8d8 a5b5 d8b8 b5a4 b8f4 c4e2 f7f6 e2g2 f6f7 g2h3 f4d2 h3h7 f7f6 h7h8 f6g6 h8e8 g6f6 e8h8 f6g6 a4a5 d2a2 a5b6 a2f2 b6c7 f2f4 c7d7 f4g4 h8g8 g6h6 g8f8 h6h5 f8h8 h5g6 h8e8 g6h6 b4b5 e6e5 d7d8 g4d4 d8c7 d4c5 c7b7 e5e4 b5b6 e4e3 e8e6 h6g7 b7a6 c5a3 a6b7 a3d3 e6e7 g7g6 e7e6 g6h5 e6h3 h5g6 h3e6 g6h7 e6f7 h7h6 f7e6 h6h5 e6h3 h5g6 h3h8 d3e4 b7a7 e3e2 h8g8 g6h5 g8h8 h5g4 h8c8 g4h4 c8h8 h4g3 h8c3 g3g2 b6b7 e2e1q c3e1 e4e1");
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go depth 16");
            Engine.Wait();
        }
        */

        [TestMethod]
        public void PositionRepeatTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("position startpos moves b1c3 e7e5 e2e4 b8c6 g1f3 g8f6 f1e2 d7d5 e4d5 f6d5 e1g1 d5c3 d2c3 d8d1 f1d1 f8d6 c1e3 c8f5 a1c1 h7h6 f3d2 e8c8 e2f3 c8b8 d2c4 e5e4 c4d6 c7d6 f3e2 f5e6 d1d2 g7g5 c1d1 b8c7 e3d4 c6d4 d2d4 d6d5 g2g3 f7f5 f2f3 h6h5 h2h4 g5h4 g3h4 h8g8 g1f2 d8e8 d1e1 e6f7 f2e3 g8g3 e3f4 g3h3 e1d1 h3h2 e2b5 e8f8 d4d2 h2h3 d1f1 c7d6 b5e2 f7e6 c3c4 f8g8 f3e4 f5e4 c4d5 e4e3 d2d3 g8f8 f4e4 e6f5 f1f5 h3h4 e4e3 f8f5 e2f3 f5e5 e3f2 h4f4 f2g3 f4c4 c2c3 h5h4 g3h3 c4f4 b2b4 e5g5 f3e2 g5d5 d3e3 b7b5 e3e8 d5g5 e8d8 d6c7 d8d3 c7b6 e2d1 a7a5 b4a5 b6a5 d1b3 a5b6 d3e3 b6c5 b3e6 c5b6 e6b3 b6c5 b3e6 c5b6");
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 14966 btime 20197 winc 2000 binc 2000");
            Engine.Wait();
        }

        [TestMethod]
        public void SearchingRepeatedCycleTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("position startpos moves g1f3 c7c5 c2c4 g8f6 b1c3 e7e6 e2e4 b8c6 f1e2 d7d5 e4d5 e6d5 d2d4 c8e6 c4d5 f6d5 c3e4 c5d4 e1g1 d5c7 e2d3 f8e7 c1g5 f7f6 g5f4 e8g8 a1c1 c7d5 f4g3 d5b4 d3b1 f6f5 e4c5 e7c5 c1c5 d8b6 f3d4 f8d8 d4e6 d8d1 f1d1 c6d8 d1d6 b4c6");
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 582209 btime 423666 winc 0 binc 0 movestogo 18");
            Engine.Wait();
        }

        [TestMethod]
        public void RepeatNotDetectedTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("position startpos moves g1f3 d7d5 d2d4 e7e6 c2c4 g8f6 b1c3 c7c6 e2e3 b8d7 f1d3 d5c4 d3c4 b7b5 c4d3 a7a6 a2a4 b5b4 c3e4 c6c5 e4f6 d7f6 e1g1 c5d4 e3d4 f8d6 f3e5 c8b7 c1f4 e8g8 a1c1 d6c7 c1c4 a6a5 d1d2 d8d5 f2f3 c7e5 f4e5 f6d7 f1e1 b7a6 c4c2 d7e5 d3e4 d5a2 d4e5 a8d8 d2e3 a2a4 b2b3 a4a3 c2c1 a3b2 c1c6 a6b5 c6c5 d8b8 e1b1 b2a3 h2h3 f8d8 c5c7 a3a2 b1c1 b5e8");
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 280953 btime 127595 winc 0 binc 0 movestogo 7");
            Engine.Wait();
        }

        [TestMethod]
        public void MopUpNotWorkingTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("position startpos moves b1c3 c7c5 e2e4 b8c6 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6 f1c4 d8b6 d4b3 b6d8 e1g1 g7g6 c3d5 f8g7 d5f6 g7f6 c2c3 h7h5 c1e3 c6e5 c4e2 d8c7 e3d4 c8e6 f2f4 e5d7 d4f2 e6c4 e2c4 c7c4 d1d5 c4d5 e4d5 h5h4 f1d1 d7b6 f2b6 a7b6 b3d4 a8a4 b2b4 h8h5 d4e2 a4a3 d1d3 e8d7 g2g3 h4g3 h2g3 g6g5 g1f2 g5f4 g3f4 f6h8 f2g3 f7f5 g3g2 h5h4 d3e3 b6b5 g2g1 h8f6 e3d3 h4h8 g1f2 h8h2 f2e3 b7b6 a1f1 a3a2 f1f2 h2h1 f2f3 h1e1 d3d2 a2a3 e3f2 e1h1 f2g2 h1h8 f3e3 h8c8 g2f2 f6h4 f2g2 c8g8 g2h2 h4f6 d2c2 g8c8 e3g3 a3a1 c2d2 c8h8 h2g2 h8a8 g3h3 a1b1 h3h5 a8a3 h5h3 b1b3 d2c2 a3a1 h3h5 a1a7 h5h1 a7c7 h1c1 c7c8 g2f2 c8c4 f2g2 b3a3 g2f2 e7e6 d5e6 d7e6 f2e1 d6d5 e1d2 c4e4 c1e1 e6f7 e1h1 d5d4 c3d4 f6d4 c2c7 f7g6 c7c6 d4f6 h1c1 a3a2 c1c2 a2c2 c6c2 e4c4 c2c1 f6b2 c1c2 c4b4 c2c6 g6f7 c6b6 b2d4 e2d4 b4d4 d2e3 d4d5 e3f3 f7e7 f3e3 e7d7 b6g6 d7c7 g6a6 d5d6 a6a1 c7b6 a1b1 d6e6 e3f3 e6c6 f3e3 c6c4 b1d1 b5b4 d1d8 b6b5 e3d3 c4f4 d3c2 f4c4 c2b2 f5f4 d8d3 b5c5 d3f3 c5d5 f3f1 c4d4 b2b3 d5e5 b3a4 e5f5 a4b5 d4e4 b5c5 f5e5 f1f3 e4e3 f3f2 b4b3 f2h2 f4f3 c5c4 e3e2 h2h5 e5e4 h5h4 e4e3 c4b3 f3f2 h4h3 e3d4 h3h4 e2e4 h4h1 e4e1 h1h4 d4e5 h4h5 e5e4 h5h4 e4d5 h4f4 f2f1q f4f1 e1f1 b3c3 d5c5 c3d3 f1f6 d3e4 c5d6 e4d4 f6f3 d4e4 f3a3 e4d4 d6d7 d4e5 a3a4 e5d5 d7c8 d5c5 a4f4 c5d5 f4g4 d5c6 g4e4 c6d5 e4e2 d5d4 c8b7 d4d5 b7a6 d5d4 a6b5 d4d5 e2e3 d5d4 e3e8 d4d5 e8f8 d5d6 f8f5 d6e6 f5f4 e6e5 f4a4 e5d5 b5b6 d5e6 a4f4 e6e5 f4h4 e5d5");
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 14864 btime 135206 winc 6000 binc 6000");
            Engine.Wait();
        }
    }
}