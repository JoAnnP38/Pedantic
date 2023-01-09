using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;

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
            Board bd = new(Constants.FEN_START_POS);
            TimeControl time = new();
            time.Reset();
            //Engine.UseOwnBook = false;
            Program.ParseCommand(@"position startpos moves e2e4");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand(@"go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        public void GoBookMove2Test()
        {
            //Engine.UseOwnBook = false;
            Program.ParseCommand("position startpos moves e2e4 e7e5 g1f3");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go wtime 293797 btime 300000 winc 0 binc 0 movestogo 39");
            Engine.Wait();
        }

        [TestMethod]
        public void GoBookMove3Test()
        {
            //Engine.UseOwnBook = false;
            Program.ParseCommand("position fen rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1 moves e7e5 g1f3");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go wtime 293797 btime 300000 winc 0 binc 0 movestogo 39");
            Engine.Wait();
        }

        [TestMethod]
        public void GoBookMove4Test()
        {
            Engine.UseOwnBook = false;
            Program.ParseCommand("position startpos moves e2e4 c7c5 g1f3 b8c6 d2d4 c5d4 f3d4 g8f6 b1c3 e7e5 d4b5 d7d6 c1g5 a7a6 b5a3 b7b5 g5f6 g7f6 c3d5 f6f5 f1d3 c8e6 e1g1 f8g7 d1h5 f5f4 c2c4 b5c4 d3c4");
            Console.WriteLine(Engine.Board.ToString());
            Console.WriteLine(Engine.Board.ToFenString());
            Program.ParseCommand("go wtime 209886 btime 203893 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void IndexOutOfRangeRecreateTest()
        {
            Program.ParseCommand("position startpos moves e2e4 c7c6 d2d4 d7d5 b1c3 d5e4 c3e4 g8f6 e4f6 g7f6 g1f3 c8g4 f1e2 d8c7 h2h3 g4e6 e1g1 b8a6 e2a6 b7a6 d1e2");
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
            Program.ParseCommand("position startpos moves d2d4 g8f6 g1f3 e7e6 c2c4 b7b6 g2g3 c8b7 f1g2 f8e7 b1c3 f6e4 c1d2 e7f6 e1g1");
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
            Engine.Infinite = true;
            Engine.UseOwnBook = false;
            Program.ParseCommand(@$"position fen {fen}");
            Program.ParseCommand("go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        [DataRow("2r2r2/p6p/3R4/4k1p1/2B5/5pP1/P4K1P/3R4 w - - 0 1")]
        [DataRow("1rr3k1/5p2/4p1pQ/pp1n2Np/7P/1P5P/2q2P2/R1B3K1 w - - 0 1")]
        public void CheckMateInTwoTest(string fen)
        {
            Engine.Infinite = true;
            Engine.UseOwnBook = false;
            Program.ParseCommand(@$"position fen {fen}");
            Program.ParseCommand("go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        [DataRow("r1b2r1k/pp4pp/3p4/3B4/8/1QN3Pn/PP3q1P/R3R2K b - - 0 1")]
        [DataRow("2r3k1/p4p2/3Rp2p/1p2P1pK/8/1P4P1/P3Q2P/1q6 b - - 0 1")]
        public void CheckMateInThreeTest(string fen)
        {
            Engine.Infinite = true;
            Engine.UseOwnBook = false;
            Program.ParseCommand(@$"position fen {fen}");
            Program.ParseCommand("go wtime 300000 btime 300000 winc 0 binc 0");
            Engine.Wait();
        }

        [TestMethod]
        public void ShortenedPVTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("position startpos moves e2e4 c7c5 g1f3 e7e6 d2d4 c5d4 f3d4 a7a6 f1d3 b8c6 d4c6 d7c6 b1c3 d8c7 c1e3 g8f6 c3a4 c6c5 c2c4 f8d6 d1b3 f6d7 h2h3 e8g8 e1g1 b7b6 f2f4 c8b7");
            Program.ParseCommand("go wtime 120152 btime 150887 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void InfiniteLoop2Test()
        {
            Program.ParseCommand("position startpos moves d2d4 g8f6 c2c4 g7g6 b1c3 f8g7 e2e4 d7d6 f1e2 e8g8 c1g5 a7a6 g1f3 c7c6 h2h3 b7b5 a2a3 b5c4 e4e5 d6e5 d4e5 f6d5 e2c4 c8e6 d1d2 b8d7 c3d5 c6d5");
            Program.ParseCommand("go wtime 129205 btime 145079 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void UnexpectedExceptionTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves d2d4 g8f6 c2c4 e7e6 g1f3 d7d5 b1c3 f8e7 c1f4 e8g8 e2e3 c7c5 d4c5 e7c5 d1c2 b8c6 a1d1 f6h5");
            Program.ParseCommand("go wtime 1728990 btime 739200 winc 6000 binc 6000");
            Engine.Wait();

        }

        [TestMethod]
        public void UnexpectedHugeValueTest()
        {
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves e2e4 e7e5 g1f3 b8c6 f1b5 g8f6 d2d3 f8d6 c2c3 a7a6 b5a4 e8g8 b1d2 f8e8 f3g5 h7h6");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go wtime 158770 btime 84422 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void UnexpectedHugeValue2Test()
        {
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves e2e4 e7e5 b1c3 g8f6 f1c4 b8c6 d2d3 c6a5 g1e2 c7c6 c4f7 e8f7 c1e3 d7d5 e4d5 c6d5");
            Program.ParseCommand("go wtime 141982 btime 83680 winc 6000 binc 6000");
            Engine.Wait();

        }

        [TestMethod]
        public void UnexpectedHugeValue3Test()
        {
            for (int n = 0; n < 3; n++)
            {
                Program.ParseCommand("setoption name Hash value 128");
                Program.ParseCommand(
                    "position startpos moves e2e4 e7e5 g1f3 b8c6 f1b5 g8f6 d2d3 f8d6 c2c3 a7a6 b5a4 e8g8 b1d2 f8e8 a4b3 d6c5 f3d4 e5d4 e1g1 d7d6");
                Program.ParseCommand("go wtime 131792 btime 69924 winc 6000 binc 6000");
                Engine.Wait();
            }
        }

        [TestMethod]
        public void UnexpectedException2Test()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves e2e4 e7e5 g1f3 b8c6 f1c4 g8f6 d2d3 h7h6 e1g1 d7d6 b1c3 f8e7 h2h3 e8g8 a2a3 c8e6 c3d5 c6d4 f3d4 e5d4 d5f6 e7f6 c4e6 f7e6 d1g4 g8f7 f2f4 e6e5 a1b1 d8e8 f4e5 d6e5 c1d2 a8c8 f1f5 g7g6 f5f6 f7f6 d2h6 f8g8 c2c4");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go wtime 28277 btime 11250 winc 6000 binc 6000");
            Engine.Wait();
        }

        [TestMethod]
        public void UnexpectedException3Test()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves e2e4 e7e6 d2d4 d7d5 b1c3 f8b4 e4e5 c7c5 a2a3 b4c3 b2c3 b8c6 d1g4 g7g6 g1f3 d8a5 c1d2 a5a4 f1e2");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go wtime 86615 btime 227701 winc 12000 binc 12000");
            Engine.Wait();
        }

        [TestMethod]
        public void PonderTest()
        {
            //Engine.Infinite = true;
            Program.ParseCommand("setoption name Hash value 128");
            Program.ParseCommand("position startpos moves e2e4 e7e6 d2d4 d7d5 b1c3 f8b4 e4e5 c7c5 a2a3 b4c3 b2c3 b8c6 d1g4 g7g6 g1f3 d8a5 c1d2 a5a4 f1e2");
            Console.WriteLine(Engine.Board.ToString());
            Program.ParseCommand("go ponder wtime 86615 btime 227701 winc 12000 binc 12000");
            Thread.Sleep(35000);
            Program.ParseCommand("ponderhit");
            Engine.Wait();
            Program.ParseCommand("isready");
        }
    }
}