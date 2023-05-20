using Pedantic.Chess;
using Pedantic.Utilities;
using Constants = Pedantic.Chess.Constants;
using Index = Pedantic.Chess.Index;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class BoardTests
    {
        public TestContext? TestContext { get; set; }

        [TestMethod]
        public void CtorTest()
        {
            Board board = new();
            Assert.AreEqual(Color.None, board.SideToMove);
            Assert.AreEqual(CastlingRights.None, board.Castling);
            Assert.AreEqual(Index.NONE, board.EnPassant);
            Assert.AreEqual(0, board.HalfMoveClock);
            Assert.AreEqual(0, board.FullMoveCounter);
            Assert.AreEqual(0ul, board.Hash);
        }

        [TestMethod]
        public void AddPieceTest()
        {
            Board board = new();
            board.AddPiece(Color.White, Piece.Pawn, Index.A2);

            Assert.AreEqual(Square.WhitePawn, board.PieceBoard[Index.A2]);
            
            ulong bb = board.Pieces(Color.White, Piece.Pawn);
            Assert.AreEqual(Index.A2, BitOps.TzCount(bb));

            bb = board.Units(Color.White);
            Assert.AreEqual(Index.A2, BitOps.TzCount(bb));

            bb = board.All;
            Assert.AreEqual(Index.A2, BitOps.TzCount(bb));

            Assert.IsTrue(board.Hash != 0);
        }

        [TestMethod]
        public void RemovePieceTest()
        {
            Board board = new();
            board.AddPiece(Color.White, Piece.Pawn, Index.E2);
            board.RemovePiece(Color.White, Piece.Pawn, Index.E2);

            Assert.AreEqual(Square.Empty, board.PieceBoard[Index.E2]);

            ulong bb = board.Pieces(Color.White, Piece.Pawn);
            Assert.AreEqual(0ul, bb);

            bb = board.Units(Color.White);
            Assert.AreEqual(0ul, bb);

            bb = board.All;
            Assert.AreEqual(0ul, bb);

            Assert.IsTrue(board.Hash == 0);
        }

        [TestMethod]
        public void UpdatePieceTest()
        {
            Board board = new();
            board.AddPiece(Color.White, Piece.Pawn, Index.D2);
            board.UpdatePiece(Color.White, Piece.Pawn, Index.D2, Index.D4);

            Assert.AreEqual(Square.Empty, board.PieceBoard[Index.D2]);
            Assert.AreEqual(Square.WhitePawn, board.PieceBoard[Index.D4]);

            ulong bb = board.Pieces(Color.White, Piece.Pawn);
            Assert.AreEqual(Index.D4, BitOps.TzCount(bb));

            bb = board.Units(Color.White);
            Assert.AreEqual(Index.D4, BitOps.TzCount(bb));

            bb = board.All;
            Assert.AreEqual(Index.D4, BitOps.TzCount(bb));

            Assert.IsTrue(board.Hash != 0);

        }

        [TestMethod]
        public void LoadFenPositionTest()
        {
            Board board = new();

            Assert.IsTrue(board.LoadFenPosition(Constants.FEN_START_POS));
            Assert.AreEqual(32, BitOps.PopCount(board.All));
            Assert.AreEqual(Color.White, board.SideToMove);
            Assert.AreEqual(CastlingRights.All, board.Castling);
            Assert.AreEqual(Index.NONE, board.EnPassant);
            Assert.AreEqual(0, board.HalfMoveClock);
            Assert.AreEqual(1, board.FullMoveCounter);
            Assert.IsTrue(board.Hash != 0);
        }

        [TestMethod]
        [DataRow("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 0x463b96181691fc9cUL)]
        [DataRow("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", 0x823c9b50fd114196UL)]
        [DataRow("rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 2", 0x0756b94461c50fb0UL)]
        [DataRow("rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 2", 0x662fafb965db29d4UL)]
        [DataRow("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3", 0x22a48b5a8e47ff78UL)]
        [DataRow("rnbq1bnr/ppp1pkpp/8/3pPp2/8/8/PPPPKPPP/RNBQ1BNR w - - 0 4", 0x00fdd303c946bdd9UL)]
        [DataRow("rnbqkbnr/p1pppppp/8/8/PpP4P/8/1P1PPPP1/RNBQKBNR b KQkq c3 0 3", 0x3c8123ea7b067637UL)]
        [DataRow("rnbqkbnr/p1pppppp/8/8/P6P/R1p5/1P1PPPP1/1NBQKBNR b Kkq - 0 4", 0x5c3f9b829b279560UL)]
        [DataRow("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPPKPPP/RNBQ1BNR b kq - 0 3", 0x652a607ca3f242c1UL)]
        public void LoadFenPosition2Test(string fenString, ulong hash)
        {
            Board board = new(fenString);
            Console.WriteLine(board.Hash);
            Assert.AreEqual(hash, board.Hash);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 0x463b96181691fc9cUL)]
        [DataRow("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", 0x823c9b50fd114196ul)]
        public void LoadFenPositionHashTest(string fenString, ulong hash)
        {
            Board board = new(fenString);
            Console.WriteLine(board.Hash);
            ulong diff = board.Hash ^ hash;
            Assert.AreEqual(0ul, diff);
        }

        [TestMethod]
        [DataRow("rnb1k2r/pp3ppp/3bpq2/2p5/2B2P2/1P3Q2/P1PPN1PP/2B2RK1 w kq - 2 11")]
        [DataRow("rnbqkbnr/p1pppppp/8/8/PpP4P/8/1P1PPPP1/RNBQKBNR b KQkq c3 0 3")]
        public void ToFenStringTest(string expectedFen)
        {
            Board bd = new();
            bd.LoadFenPosition(expectedFen);
            Assert.AreEqual(expectedFen, bd.ToFenString());
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS)]
        [DataRow("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR b KQkq - 0 1")]
        public void GenerateMovesTest(string fen)
        {
            Board bd = new(fen);
            MoveList list = new();
            bd.GenerateMoves(list);
            Assert.AreEqual(20, list.Count);
        }

        [TestMethod]
        [DataRow("rnbqkbnr/p1pppppp/8/8/P6P/R1p5/1P1PPPP1/1NBQKBNR b Kkq - 0 4", 23)]
        [DataRow("r2qk2r/pb4pp/1n2Pb2/2B2Q2/p1p5/2P5/2B2PPP/RN2R1K1 w - - 1 0", 47)]
        [DataRow("r2qk2r/pb4pp/1n2Pb2/2B2Q2/p1p5/2P5/2B2PPP/RN2R1K1 b - - 1 0", 42)]
        public void GenerateMovesTest2(string fen, int expectedMoveCount)
        {
            Board bd = new(fen);
            MoveList list = new();
            bd.GenerateMoves(list);
            Assert.AreEqual(expectedMoveCount, list.Count);
        }

        [TestMethod]
        [DataRow("r2qk2r/pb4pp/1n2Pb2/2B2Q2/p1p5/2P5/2B2PPP/RN2R1K1 w - - 1 0")]
        [DataRow("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [DataRow("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1")]
        [DataRow("rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 2")]
        [DataRow("rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 2")]
        [DataRow("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3")]
        [DataRow("rnbq1bnr/ppp1pkpp/8/3pPp2/8/8/PPPPKPPP/RNBQ1BNR w - - 0 4")]
        [DataRow("rnbqkbnr/p1pppppp/8/8/PpP4P/8/1P1PPPP1/RNBQKBNR b KQkq c3 0 3")]
        [DataRow("rnbqkbnr/p1pppppp/8/8/P6P/R1p5/1P1PPPP1/1NBQKBNR b Kkq - 0 4")]
        [DataRow("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPPKPPP/RNBQ1BNR b kq - 0 3")]
        public void MakeMoveTest(string fen)
        {
            Board bd = new(fen);
            ObjectPool<MoveList> moveListPool = new (Constants.MAX_PLY);
            MoveList moveList = moveListPool.Get();
            bd.GenerateMoves(moveList);

            for (int i = 0; i < moveList.Count; ++i)
            {
                if (bd.MakeMove(moveList[i]))
                {
                    bd.UnmakeMove();
                }
                MoveList mvList = moveListPool.Get();
                bd.GenerateMoves(mvList);
                Assert.AreEqual(moveList.Count, mvList.Count, $"BestMove arrays differ after move: {Move.ToString(mvList[i])}");
                for (int n = 0; n < mvList.Count; ++n)
                {
                    Assert.AreEqual(mvList[n], moveList[n], $"BestMove arrays differ after move: {Move.ToString(mvList[i])}");
                }

                moveListPool.Return(mvList);

            }
            moveListPool.Return(moveList);
        }

        [TestMethod]
        [DataRow("r5rk/2p1Nppp/3p3P/pp2p1P1/4P3/2qnPQK1/8/R6R w - - 1 0", Color.White, 33)]
        [DataRow("1r2k1r1/pbppnp1p/1b3P2/8/Q7/B1PB1q2/P4PPP/3R2K1 w - - 1 0", Color.White, 35)]
        [DataRow("Q7/p1p1q1pk/3p2rp/4n3/3bP3/7b/PP3PPK/R1B2R2 b - - 0 1", Color.Black, 29)]
        public void GetPieceMobilityTest(string fen, Color color, int expectedMobility)
        {
            Board bd = new(fen);
            int mobility = bd.GetPieceMobility(color);
            Assert.AreEqual(expectedMobility, mobility);
        }

        [TestMethod]
        public void MoveCapturesTest()
        {
            Board bd = new("7r/P7/1K2k3/8/8/8/7p/1R6 b - - 0 1");
            foreach (ulong move in bd.QMoves(0, 0, new MoveList()))
            {
                Console.WriteLine(Move.ToString(move));
            }
        }

        [TestMethod]
        [DataRow("rnbq1rk1/4p1bp/2p3p1/1p2Pp2/3PpP2/1P2B1NP/PP4P1/R2Q1RK1 w - f6 0 15")]
        [DataRow("7r/P7/1K2k3/8/8/8/7p/1R6 b - - 0 1")]
        public void MovesTest(string fen)
        {
            Board bd = new(fen);
            foreach (ulong move in bd.Moves(0, new KillerMoves(), new History(), new MoveList()))
            {
                Console.WriteLine(Move.ToLongString(move));
            }
        }

        [TestMethod]
        public void GenerateMoves2Test()
        {
            Board bd = new("rnbq1rk1/4p1bp/2p3p1/1p2Pp2/3PpP2/1P2B1NP/PP4P1/R2Q1RK1 w - f6 0 15");
            MoveList moveList = new();
            bd.GenerateMoves(moveList);
            for (int n = 0; n < moveList.Count; n++)
            {
                Console.WriteLine(Move.ToLongString(moveList[n]));
            }
        }

        [TestMethod]
        [DataRow("3k4/1P6/8/2p3qp/8/8/8/K7 w - - 1 14")]
        public void GeneratePromotionsTest(string fen)
        {
            Board bd = new(fen);
            MoveList list = new();
            bd.GeneratePromotions(list, bd.Pieces(bd.SideToMove, Piece.Pawn));

            for (int n = 0; n < list.Count; n++)
            {
                Console.WriteLine($"{n}: {Move.ToLongString(list[n])}");
            }
        }

        [TestMethod]
        [DataRow("4k3/1pp1q1pp/2n5/4p2R/3B1b2/2QP1N2/1P2PP2/4K3 w - - 0 1", Color.White, 100)]
        [DataRow("8/1pp1q1pp/2n2k2/4p2R/3B1b2/2QP1N2/1P2PP2/4K3 w - - 0 1", Color.White, -200)]
        public void SEETest(string fen, Color stm, int expected)
        {
            Board bd = new(fen);
            ulong move = Move.Pack(Index.D4, Index.E5, MoveType.Capture, Piece.Pawn);
            int seeEval = bd.PreMoveStaticExchangeEval(stm, move);
            Assert.AreEqual(expected, seeEval);
        }

        [TestMethod]
        [DataRow("8/1pp1q1pp/2n2k2/4p2R/3B1b2/2QP1N2/1P2PP2/4K1r1 w - - 0 1", false)]
        [DataRow("8/1pp1q1pp/2n2k2/4p2R/3B4/2QP1N2/1P2PP1b/4K1r1 w - - 0 1", false)]
        [DataRow("8/1pp1q1pp/2n2k2/4p2R/3B4/2QP4/1P1NPP1b/4K1rR w - - 0 1", true)]
        public void SEE0Test(string fen, bool safe)
        {
            Board bd = new(fen);
            ulong move = Move.Pack(Index.G6, Index.G1);
            int seeEval = bd.PostMoveStaticExchangeEval(bd.SideToMove.Other(), move);
            if (safe)
            {
                Assert.IsTrue(seeEval <= 0);
            }
            else
            {
                Assert.IsTrue(seeEval > 0);
            }
        }

        [TestMethod]
        [DataRow("8/1pqk2Q1/2p4p/5pp1/5P2/7P/1P5K/2R1r3 b - - 0 1", 6)]
        [DataRow("2kr1b1r/p1p3pp/Bp3n2/4B3/1q6/2N5/PPP2PbP/R2QK2R b KQ - 0 1", 2)]
        [DataRow("r4rk1/pbp2ppp/1pnq1N2/8/QP1pPP2/3P3P/P2K3P/2RB1R2 b - - 0 1", 3)] /* pawn capture */
        [DataRow("5r2/8/8/2B4k/5pP1/8/5P2/6K1 b - g3 0 1", 6)] /* e.p. */
        [DataRow("8/8/5q2/4k3/3p1n2/6PN/6K1/2Q5 w - - 0 1", 9)] /* knight check */
        [DataRow("rnb1kbnr/pp1ppppp/8/q7/2p5/2KP4/PPP1PPPP/RNBQ1BNR w kq - 2 4", 3)] /* king escape */
        public void GenerateEvasionsTest(string fen, int expected)
        {
            Board bd = new (fen);
            MoveList list = new ();
            bd.GenerateEvasions(list);
            int legalMoves = 0;
            for (int n = 0; n < list.Count; n++)
            {
                ulong move = list[n];
                if (!bd.MakeMove(move))
                {
                    continue;
                }

                legalMoves++;
                Util.WriteLine(Move.ToLongString(move));
                bd.UnmakeMove();
            }

            Assert.AreEqual(expected, legalMoves);

        }

        [TestMethod]
        public void GenerateRecapturesTest()
        {
            Board bd = new("Bn2k2r/p5pp/3b2q1/8/4p1n1/8/PP5p/R1BQR2K w - - 0 1");
            bd.MakeMove(Move.Pack(Index.E1, Index.E4, MoveType.Capture, Piece.Pawn));
            MoveList list = new ();
            bd.GenerateRecaptures(list, Index.E4);
            int legalMoves = 0;
            for (int n = 0; n < list.Count; n++)
            {
                ulong move = list[n];
                if (!bd.MakeMove(move))
                {
                    continue;
                }

                legalMoves++;
                Util.WriteLine(Move.ToLongString(move));
                bd.UnmakeMove();
            }

            Assert.AreEqual(1, legalMoves);
        }

#if false
        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 6)]
        [DataRow("2b5/1r6/2kBp1p1/p2pP1P1/2pP4/1pP3K1/1R3P2/8 b - - 0 1", 7)]
        public void GenerateEvasions2Test(string fen, int depth)
        {
            Board bd = new(fen);
            SearchForBadState(bd, depth, bd.IsChecked());
        }

        private void SearchForBadState(Board bd, int depth, bool inCheck)
        {
            if (depth <= 0)
            {
                return;
            }

            MoveList list = new();
            MoveList evasions = new();
            bd.PushBoardState();

            bd.GenerateMoves(list);
            if (inCheck)
            {
                bd.GenerateEvasions(evasions);
            }

            for (int n = 0; n < list.Count; n++)
            {
                ulong move = list[n];
                if (!bd.MakeMoveNs(move))
                {
                    continue;
                }

                SearchForBadState(bd, depth - 1, bd.IsChecked());
                bd.UnmakeMoveNs();

                if (inCheck && evasions.FindIndex(i => Move.Compare(i, move) == 0) == -1)
                {
                    Util.WriteLine($"Depth: {depth}, Bad move: {Move.ToString(move)}, FEN: \"{bd.ToFenString()}\"");
                }
            }

            bd.PopBoardState();
        }
#endif
    }
}