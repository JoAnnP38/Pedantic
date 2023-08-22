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
        [DataRow(Constants.FEN_START_POS, (GamePhase)0, 64, 0)]
        [DataRow("r1bk3r/ppppnp1p/2n4b/3N1q2/2B2p2/3P4/PPPBQ1PP/4RRK1 b - - 9 13", (GamePhase)0, 59, 5)]
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
        [DataRow("r6r/pp4kp/3B1p2/3P2p1/B1P1q1n1/2Q3P1/PP6/5RK1 w - - 0 13", -90)]
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
        [DataRow(Constants.FEN_START_POS, 4925, 4925, 4500, 4500, (short)64)]
        [DataRow("r2n2k1/3P3p/1R4p1/2B5/4p3/2P1P2P/p4rP1/2KR4 w - - 0 40", 2075, 1955, 2055, 1920, (short)29)]
        public void CorrectMaterialTest(string fen, int opWhiteMaterial, int opBlackMaterial, int egWhiteMaterial,
            int egBlackMaterial, short phase)
        {
            Board board = new(fen);
            Assert.AreEqual(opWhiteMaterial, board.OpeningMaterial[(int)Color.White], "1");
            Assert.AreEqual(opBlackMaterial, board.OpeningMaterial[(int)Color.Black], "2");
            Assert.AreEqual(egWhiteMaterial, board.EndGameMaterial[(int)Color.White], "3");
            Assert.AreEqual(egBlackMaterial, board.EndGameMaterial[(int)Color.Black], "4");
            Assert.AreEqual(phase, board.Phase, "5");
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
            ulong[] moves = board.Moves(0, km, h, new SearchStack(board), list2)
                .Select(m => Move.ClearScore(m))
                .ToArray();
            Assert.IsTrue(s1.SetEquals(moves));

            ulong move = Move.Pack(board.SideToMove, Piece.Pawn, Index.H4, Index.H5, MoveType.PawnMove);
            board.MakeMove(move);

            int eval1 = evaluation.Compute(board);

            Assert.IsTrue(-eval1 > eval0);

            board.UnmakeMove();

            move = Move.Pack(board.SideToMove, Piece.Pawn, Index.C3, Index.C4, MoveType.PawnMove);
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
        [DataRow("1k3r2/1p4p1/p3p1Np/3b1p2/1bq5/2P2P2/PP1Q1PBP/1K1R2R1 w - - 5 27", (short)540)]
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

        [TestMethod]
        [DataRow("5rk1/1ppb3p/p1pb4/6q1/3P1p1r/2P1R2P/PP1BQ1P1/5RKN w - - 0 1", "5rkn/pp1bq1p1/2p1r2p/3p1P1R/6Q1/P1PB4/1PPB3P/5RK1 b - - 0 1")]
        [DataRow("1k3r2/1p4p1/p3p1Np/3b1p2/1bq5/2P2P2/PP1Q1PBP/1K1R2R1 w - - 5 27", "1k1r2r1/pp1q1pbp/2p2p2/1BQ5/3B1P2/P3P1nP/1P4P1/1K3R2 b - - 5 27")]
        [DataRow("r1b1kb1r/3q1ppp/pBp1pn2/8/Np3P2/5B2/PPP3PP/R2Q1RK1 w kq - 0 1 ", "r2q1rk1/ppp3pp/5b2/nP3p2/8/PbP1PN2/3Q1PPP/R1B1KB1R b KQ - 0 1")]
        [DataRow("8/k1b5/P4p2/1Pp2p1p/K1P2P1P/8/3B4/8 w - - 0 1", "8/3b4/8/k1p2p1p/1pP2P1P/p4P2/K1B5/8 b - - 0 1")]
        public void WhiteVsBlackTest(string whiteFen, string blackFen)
        {
            Board bdWhite = new(whiteFen);
            Board bdBlack = new(blackFen);

            Evaluation eval = new(false);

            short evalWhite = eval.Compute(bdWhite);
            short evalBlack = eval.Compute(bdBlack);

            Assert.AreEqual(evalWhite, evalBlack);
        }
    }
}