using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;
using Index = Pedantic.Chess.Index;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class EvaluationTests
    {
        [TestMethod]
        [DataRow(Constants.FEN_START_POS, (GamePhase)0, 128, 0)]
        [DataRow("r1bk3r/ppppnp1p/2n4b/3N1q2/2B2p2/3P4/PPPBQ1PP/4RRK1 b - - 9 13", (GamePhase)0, 128, 0)]
        public void GetGamePhaseTest(string fen, GamePhase expectedPhase, int expectedOpWt, int expectedEgWt)
        {
            Board board = new(fen);
            Evaluation eval = new();
            GamePhase gamePhase = eval.GetGamePhase(board, out int opWt, out int egWt);
            Assert.AreEqual(expectedPhase, gamePhase);
            Assert.AreEqual(expectedOpWt, opWt);
            Assert.AreEqual(expectedEgWt, egWt);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 0)]
        [DataRow("r6r/pp4kp/3B1p2/3P2p1/B1P1q1n1/2Q3P1/PP6/5RK1 w - - 0 13", -81)]
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
            Board board = new("r2n2k1/3P3p/1R4p1/2B5/4p3/2P1P2P/p4rP1/2KR4 w - - 0 40");
            Evaluation eval = new();

            int scoreWhite = eval.Compute(board);
            board.LoadFenPosition("r2n2k1/3P3p/1R4p1/2B5/4p3/2P1P2P/p4rP1/2KR4 b - - 0 40");
            int scoreBlack = eval.Compute(board);

            Assert.AreEqual(scoreWhite, -scoreBlack);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 4455, 4455, 4300, 4300, 3900, 3900)]
        [DataRow("r2n2k1/3P3p/1R4p1/2B5/4p3/2P1P2P/p4rP1/2KR4 w - - 0 40", 1875, 1755, 1980, 1885, 1800, 1700)]
        public void CorrectMaterialTest(string fen, int opWhiteMaterial, int opBlackMaterial, int egWhiteMaterial,
            int egBlackMaterial, int whiteMaterial, int blackMaterial)
        {
            Board board = new(fen);
            Assert.AreEqual(opWhiteMaterial, board.OpeningMaterial[(int)Color.White], "1");
            Assert.AreEqual(opBlackMaterial, board.OpeningMaterial[(int)Color.Black], "2");
            Assert.AreEqual(egWhiteMaterial, board.EndGameMaterial[(int)Color.White], "3");
            Assert.AreEqual(egBlackMaterial, board.EndGameMaterial[(int)Color.Black], "4");
            Assert.AreEqual(whiteMaterial, board.MaterialNoKing(Color.White), "5");
            Assert.AreEqual(blackMaterial, board.MaterialNoKing(Color.Black), "6");
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, -201, -201, -147, -147)]
        [DataRow("r2n2k1/3P3p/1R4p1/2B5/4p3/2P1P2P/p4rP1/2KR4 w - - 0 40", 264, 401, 91, 123)]
        public void CorrectPcSquareTest(string fen, int opWhite, int opBlack, int egWhite, int egBlack)
        {
            Board board = new(fen);
            Assert.AreEqual(opWhite, board.OpeningPieceSquare[(int)Color.White, 0]);
            Assert.AreEqual(opBlack, board.OpeningPieceSquare[(int)Color.Black, 0]);
            Assert.AreEqual(egWhite, board.EndGamePieceSquare[(int)Color.White, 0]);
            Assert.AreEqual(egBlack, board.EndGamePieceSquare[(int)Color.Black, 0]);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 4, 4)]
        [DataRow("r2n2k1/3P3p/1R4p1/2B5/4p3/2P1P2P/p4rP1/2KR4 w - - 0 40", 25, 20)]
        public void CorrectPieceMobilityTest(string fen, int whiteMobility, int blackMobility)
        {
            Board board = new(fen);
            Assert.AreEqual(whiteMobility, board.GetPieceMobility(Color.White));
            Assert.AreEqual(blackMobility, board.GetPieceMobility(Color.Black));
        }

        [TestMethod]
        [DataRow("5r2/8/8/8/3B3P/2PK4/1k6/7R w - - 94 142")]
        public void PassPawnEvaluationTest(string fen)
        {
            KillerMoves km = new();
            History h = new();
            Board board = new(fen);
            Evaluation evaluation = new();
            int eval0 = evaluation.Compute(board);
            MoveList list1 = new();
            board.GenerateMoves(list1);
            SortedSet<ulong> s1 = new(list1);
            MoveList list2 = new();
            ulong[] moves = board.Moves(0, km, h, list2)
                .Select(m => Move.ClearScore(m))
                .ToArray();
            Assert.IsTrue(s1.SetEquals(moves));

            ulong move = Move.Pack(Index.H4, Index.H5, MoveType.PawnMove);
            board.MakeMove(move);

            int eval1 = evaluation.Compute(board);

            Assert.IsTrue(-eval1 > eval0);

            board.UnmakeMove();

            move = Move.Pack(Index.C3, Index.C4, MoveType.PawnMove);
            board.MakeMove(move);
            int eval2 = evaluation.Compute(board);

            Assert.IsTrue(-eval2 > eval0);
        }

        [TestMethod]
        [DataRow("8/8/8/pk5P/1p5P/4K3/8/8 w - - 0 100")]
        public void PassedPawnEvaluationTest(string fen)
        {
            Board board = new(fen);
            Evaluation eval = new();
            int e = eval.Compute(board);

            Console.WriteLine(@$"eval.Compute(board) : {e}");
            Assert.IsTrue(e < 0);
        }

        [TestMethod]
        [DataRow("1k3r2/1p4p1/p3p1Np/3b1p2/1bq5/2P2P2/PP1Q1PBP/1K1R2R1 w - - 5 27", (short)445)]
        public void UnbalancedPosition(string fen, short expected)
        {
            Board bd = new(fen);
            Evaluation eval = new();
            short actual = eval.Compute(bd);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void IsDoubledTest()
        {
            Board bd = new("rn1k4/ppq3pp/3p1bb1/2pP1Nn1/N1P1Q1B1/1P2R3/P6r/2K1R3 w - - 0 1");
            Assert.IsTrue(Evaluation.IsDoubled(bd, bd.Pieces(Color.White, Piece.Rook) & Board.MaskFile(Index.E1)));
            Assert.IsFalse(Evaluation.IsDoubled(bd, bd.Pieces(Color.Black, Piece.Rook) & Board.MaskFile(Index.H1)));
        }
    }
}