﻿using Pedantic.Chess;
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
            Assert.AreEqual(Index.None, board.EnPassant);
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
            Assert.AreEqual(Index.None, board.EnPassant);
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
        [DataRow("rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 0 1", 0x2BF65946A9355D94UL)]
        public void LoadFenPosition2Test(string fenString, ulong hash)
        {
            Board board = new(fenString);
            Console.WriteLine(board.Hash);
            Assert.AreEqual(hash, board.Hash);
        }

        [TestMethod]
        [DataRow(Constants.FEN_START_POS, 0x463b96181691fc9cUL)]
        [DataRow("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", 0x823c9b50fd114196ul)]
        [DataRow("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 1", 0xf092b5b0c1d33ca9ul)]
        [DataRow("rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 0 1", 0x2bf65946a9355d94ul)]
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
            ObjectPool<MoveList> moveListPool = new ObjectPool<MoveList>(Constants.MAX_PLY);
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
        [DataRow("r5rk/2p1Nppp/3p3P/pp2p1P1/4P3/2qnPQK1/8/R6R w - - 1 0", Color.White, 44)]
        [DataRow("1r2k1r1/pbppnp1p/1b3P2/8/Q7/B1PB1q2/P4PPP/3R2K1 w - - 1 0", Color.White, 40)]
        [DataRow("Q7/p1p1q1pk/3p2rp/4n3/3bP3/7b/PP3PPK/R1B2R2 b - - 0 1", Color.Black, 36)]
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
            foreach (ulong move in bd.CaptureMoves(new MoveList()))
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
    }
}