using Pedantic.Chess;
using System.Text;
using Index = Pedantic.Chess.Index;


namespace Pedantic.UnitTests
{
    [TestClass]
    public class Evaluation2Tests
    {
        public static readonly EvalCache cache = new();

        [TestInitialize]
        public void Initialize()
        {
            cache.Clear();
        }

        [TestMethod]
        public void StaticCtorTest()
        {
            Assert.IsNotNull(Evaluation.Weights);
            Assert.AreEqual(new Score(-57, -40), Evaluation.MopUpMate[12]);
            Assert.AreEqual(new Score(-51, -40), Evaluation.MopUpMateNBLight[76]);
            Assert.AreEqual(new Score(-97, -100), Evaluation.MopUpMateNBDark[144]);
        }

        [TestMethod]
        public void InitializeEvalInfoTest()
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(Constants.FEN_START_POS);
            Evaluation.InitializeEvalInfo(board, evalInfo);

            Assert.AreEqual(board.Pieces(Color.White, Piece.Pawn), evalInfo[0].Pawns);
            Assert.AreEqual(board.Pieces(Color.Black, Piece.Pawn), evalInfo[1].Pawns);
            Assert.AreEqual(0ul, evalInfo[0].PassedPawns);
            Assert.AreEqual(0ul, evalInfo[1].PassedPawns);
            Assert.AreEqual(16, evalInfo[0].TotalPawns);
            Assert.AreEqual(16, evalInfo[1].TotalPawns);
            Assert.AreEqual(Index.E1, evalInfo[0].KI);
            Assert.AreEqual(Index.E8, evalInfo[1].KI);
            Assert.AreEqual(KingPlacement.KK, evalInfo[0].KP);
            Assert.AreEqual(KingPlacement.KK, evalInfo[1].KP);
            Assert.AreEqual((byte)CastlingRights.WhiteRights, evalInfo[0].CastlingRightsMask);
            Assert.AreEqual((byte)CastlingRights.BlackRights, evalInfo[1].CastlingRightsMask);
        }

        [TestMethod]
        [DataRow("8/8/8/2kb4/1n6/8/8/2K5 b - - 0 1", true, Evaluation.EgKingPst.MopUpNBLight)]
        [DataRow("8/8/8/2k5/1n1b4/8/8/2K5 b - - 0 1", true, Evaluation.EgKingPst.MopUpNBDark)]
        [DataRow("8/8/8/2k5/1nn5/8/8/2K5 b - - 0 1", false, Evaluation.EgKingPst.Normal)]
        [DataRow("8/8/8/2k5/1nn5/8/8/2K3n1 b - - 0 1", true, Evaluation.EgKingPst.MopUp)]
        public void SufficientMatingMaterialTest(string fen, bool sufficient, Evaluation.EgKingPst kingPst)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            int c = (int)board.SideToMove;
            int o = (int)board.SideToMove.Other();
            bool isSufficient = Evaluation.SufficientMatingMaterial(board, evalInfo, board.SideToMove);
            Assert.AreEqual(sufficient, isSufficient);
            Assert.AreEqual(kingPst, evalInfo[o].KingPst);
        }

        [TestMethod]
        [DataRow("8/8/8/2kb4/1n6/8/8/2K5 b - - 0 1", false, true)]
        [DataRow("8/8/8/2k5/1n1b4/8/8/2K5 b - - 0 1", false, true)]
        [DataRow("8/8/8/2k5/1nn5/8/8/2K5 b - - 0 1", false, false)]
        [DataRow("8/8/8/2k5/1nn5/8/8/2K3n1 b - - 0 1", false, true)]
        public void CanWinTest(string fen, bool wCanWin, bool bCanWin)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            var (WhiteCanWin, BlackCanWin) = Evaluation.CanWin(board, evalInfo);

            Assert.AreEqual(wCanWin, WhiteCanWin);
            Assert.AreEqual(bCanWin, BlackCanWin);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, GamePhase.Opening, Constants.MAX_PHASE)]
        [DataRow("8/8/8/2k5/1nn5/8/8/2K3n1 b - - 0 1", GamePhase.EndGameMopup, 6)]
        public void GetGamePhaseTest(string fen, GamePhase gamePhase, int phase)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);
            Assert.AreEqual(gamePhase, eval.GetGamePhase(board, evalInfo));
        }

        [TestMethod]
        [DataRow("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 1", 2228239, 2228239)]
        public void EvalMiscTest(string fen, int whiteScore, int blackScore)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);

            Score wScore = Evaluation.EvalMisc(board, evalInfo, Color.White);
            Score bScore = Evaluation.EvalMisc(board, evalInfo, Color.Black);

            Assert.AreEqual(whiteScore, wScore);
            Assert.AreEqual(blackScore, bScore);
        }

        [TestMethod]
        [DataRow("8/8/4K3/3N4/2b5/1k6/8/8 b - - 0 1", 0, 4063263)]
        public void EvalThreatsTest(string fen, int whiteScore, int blackScore)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);

            Score wScore = Evaluation.EvalThreats(board, evalInfo, Color.White);
            Score bScore = Evaluation.EvalThreats(board, evalInfo, Color.Black);

            Assert.AreEqual(whiteScore, wScore);
            Assert.AreEqual(blackScore, bScore);
        }

        [TestMethod]
        [DataRow("8/8/4K3/3N4/2b5/1k6/8/8 b - - 0 1", 3670088, 1572894)]
        public void EvalMobilityTest(string fen, int whiteScore, int blackScore)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);

            Score wScore = Evaluation.EvalMobility(board, evalInfo, Color.White);
            Score bScore = Evaluation.EvalMobility(board, evalInfo, Color.Black);

            Console.WriteLine($"wScore: {wScore}, bScore: {bScore}");

            Assert.AreEqual(whiteScore, wScore);
            Assert.AreEqual(blackScore, bScore);
        }

        [TestMethod]
        [DataRow("1r5k/6np/2p3p1/8/8/P5P1/1P2RP2/5BK1 b - - 0 1", 2555961, 2293789)]
        public void EvalKingSafteyTest(string fen, int whiteScore, int blackScore)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);

            // have to run EvalMobility first so that PieceAttacks are initialized in EvalInfo
            Evaluation.EvalMobility(board, evalInfo, Color.White);
            Evaluation.EvalMobility(board, evalInfo, Color.Black);

            Score wScore = Evaluation.EvalKingSafety(board, evalInfo, Color.White);
            Score bScore = Evaluation.EvalKingSafety(board, evalInfo, Color.Black);

            Console.WriteLine($"wScore: {wScore}, bScore: {bScore}");

            Assert.AreEqual(whiteScore, wScore);
            Assert.AreEqual(blackScore, bScore);
        }

        [TestMethod]
        [DataRow("1r5k/6n1/2p3p1/2p4p/7P/6P1/PP1R1P2/5BK1 b - - 0 1", 1638463, -3407886)]
        public void EvalPawnsTest(string fen, int whiteScore, int blackScore)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);

            // have to run EvalMobility first so that PieceAttacks are initialized in EvalInfo
            Evaluation.EvalMobility(board, evalInfo, Color.White);
            Evaluation.EvalMobility(board, evalInfo, Color.Black);

            Score wScore = Evaluation.EvalPawns(board, evalInfo, Color.White);
            Score bScore = Evaluation.EvalPawns(board, evalInfo, Color.Black);

            Console.WriteLine($"wScore: {wScore}, bScore: {bScore}");

            Assert.AreEqual(whiteScore, wScore);
            Assert.AreEqual(blackScore, bScore);
        }

        [TestMethod]
        [DataRow("3r3k/6n1/6p1/7p/3p3P/3B2P1/PP1R1P2/6K1 w - - 0 1", 7143431, 4456449)]
        public void EvalPassedPawnsTest(string fen, int whiteScore, int blackScore)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);

            // EvalPawns must be called first to set EvalInfo.PassedPawns
            Evaluation.EvalPawns(board, evalInfo, Color.White);
            Evaluation.EvalPawns(board, evalInfo, Color.Black);

            Score wScore = Evaluation.EvalPassedPawns(board, evalInfo, Color.White);
            Score bScore = Evaluation.EvalPassedPawns(board, evalInfo, Color.Black);

            Console.WriteLine($"wScore: {wScore}, bScore: {bScore}");

            Assert.AreEqual(whiteScore, wScore);
            Assert.AreEqual(blackScore, bScore);
        }

        [TestMethod]
        [DataRow("2r5/6k1/q3p3/3p1p2/3Nb3/2P1BB2/rR1P4/1R2KQ2 w - - 0 1", 4980756, 12058634)]
        public void EvalPiecesTest(string fen, int whiteScore, int blackScore)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);

            Score wScore = Evaluation.EvalPieces(board, evalInfo, Color.White);
            Score bScore = Evaluation.EvalPieces(board, evalInfo, Color.Black);

            Console.WriteLine($"wScore: {wScore}, bScore: {bScore}");

            Assert.AreEqual(whiteScore, wScore);
            Assert.AreEqual(blackScore, bScore);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 422188732, 422188732)]
        public void EvalMaterialAndPstTest(string fen, int whiteScore, int blackScore)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);

            Score wScore = Evaluation.EvalMaterialAndPst(board, evalInfo, Color.White);
            Score bScore = Evaluation.EvalMaterialAndPst(board, evalInfo, Color.Black);

            Console.WriteLine($"wScore: {wScore}, bScore: {bScore}");

            Assert.AreEqual(whiteScore, wScore);
            Assert.AreEqual(blackScore, bScore);
        }

        [TestMethod]
        [DataRow("3R4/8/8/4k3/8/3K4/8/8 w - - 0 1", 965)]
        [DataRow("8/8/8/3nk2b/8/3K4/8/8 b - - 0 1", -1225)]
        [DataRow("8/8/8/3nk1b1/8/3K4/8/8 b - - 0 1", -1225)]
        public void ComputeMopUpTest(string fen, int score)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);
            GamePhase gamePhase = eval.GetGamePhase(board, evalInfo);
            Assert.AreEqual(GamePhase.EndGameMopup, gamePhase);

            int mopUpScore = eval.ComputeMopUp(board, evalInfo);
            Assert.AreEqual(score, mopUpScore);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 0)]
        public void ComputeNormalTest(string fen, int score)
        {
            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[Constants.MAX_COLORS];
            Board board = new(fen);
            Evaluation.InitializeEvalInfo(board, evalInfo);
            Evaluation eval = new(cache);
            GamePhase gamePhase = eval.GetGamePhase(board, evalInfo);
            bool isLazy = false;
            int normalScore = eval.ComputeNormal(board, evalInfo, -Constants.INFINITE_WINDOW, Constants.INFINITE_WINDOW, ref isLazy);
            Assert.IsFalse(isLazy);
            Assert.AreEqual(score, normalScore);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 0)]
        public void ComputeTest(string fen, int score)
        {
            Board board = new(fen);
            Evaluation eval = new(cache);
            int computeScore = eval.Compute(board);
            Assert.AreEqual(score, computeScore);
        }

        [TestMethod]
        [DataRow("8/5k2/5p2/7r/8/8/4R3/3K1R2 w - - 0 1", Index.E2, false)]
        [DataRow("8/5k2/5p2/7r/8/8/8/3KRR2 b - - 0 1", Index.E1, false)]
        [DataRow("8/5k2/5p2/7r/8/4R3/4P3/3KR3 w - - 0 1", Index.E1, false)]
        [DataRow("8/5k2/5p2/7r/8/4R3/3P4/3KR3 w - - 0 1", Index.E3, false)]
        [DataRow("8/5k2/5p2/7r/8/4R3/3P4/3KR3 w - - 0 1", Index.E1, true)]
        [DataRow("8/5k2/5pr1/7r/8/4R3/3P4/3KR3 b - - 0 1", Index.G6, false)]
        [DataRow("8/5k2/5pr1/8/8/4R3/3P2r1/3KR3 b - - 0 1", Index.G2, false)]
        [DataRow("8/5k2/5pr1/8/8/4R3/3P2r1/3KR3 b - - 0 1", Index.G6, true)]
        public void IsDoubledTest(string fen, int sq, bool isDoubled)
        {
            Board board = new(fen);
            Assert.AreEqual(isDoubled, Evaluation.IsDoubled(board, sq));
        }

        [TestMethod]
        public void StmScoreTest()
        {
            Assert.AreEqual(-1, Evaluation.StmScore(Color.Black, 1));
            Assert.AreEqual(1, Evaluation.StmScore(Color.White, 1));
        }
    }
}
