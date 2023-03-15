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

            short[] weights = EvalFeatures.GetCombinedWeights(Evaluation.Weights);

            const int vecSize = EvalFeatures.FEATURE_SIZE + 1;
            ReadOnlySpan<short> opWeights = new ReadOnlySpan<short>(weights, 0, vecSize);
            ReadOnlySpan<short> egWeights = new ReadOnlySpan<short>(weights, vecSize, vecSize);

            Evaluation eval = new();
            short expected = eval.Compute(bd);
            short actual = features.Compute(opWeights, egWeights);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void VerifyWeightsInSyncTest()
        {
            short[] weights = EvalFeatures.GetCombinedWeights(Evaluation.Weights);
            const int vecSize = EvalFeatures.FEATURE_SIZE + 1;
            ReadOnlySpan<short> opWeights = new ReadOnlySpan<short>(weights, 0, vecSize);
            ReadOnlySpan<short> egWeights = new ReadOnlySpan<short>(weights, vecSize, vecSize);

            for (int pc = 0; pc < Constants.MAX_PIECES - 1; pc++)
            {
                Assert.AreEqual(Evaluation.OpeningPieceValues[pc], opWeights[EvalFeatures.MATERIAL + pc], $"OP Piece = {pc}");
                Assert.AreEqual(Evaluation.EndGamePieceValues[pc], egWeights[EvalFeatures.MATERIAL + pc], $"EG Piece = {pc}");
            }

            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                for (int sq = 0; sq < Constants.MAX_SQUARES; sq++)
                {
                    Assert.AreEqual(Evaluation.OpeningPieceSquareTable[pc, sq], opWeights[EvalFeatures.PIECE_SQUARE_TABLES + (pc * 64) + sq], $"OP Piece = {pc}, Square = {sq}");
                    Assert.AreEqual(Evaluation.EndGamePieceSquareTable[pc, sq], egWeights[EvalFeatures.PIECE_SQUARE_TABLES + (pc * 64) + sq], $"EG Piece = {pc}, Square = {sq}");
                }
            }

            Assert.AreEqual(Evaluation.OpeningMobilityWeight, opWeights[EvalFeatures.MOBILITY], "OP Mobility");
            Assert.AreEqual(Evaluation.EndGameMobilityWeight, egWeights[EvalFeatures.MOBILITY], "EG Mobility");

            Assert.AreEqual(Evaluation.OpeningIsolatedPawn, opWeights[EvalFeatures.ISOLATED_PAWNS], "OP Isolated Pawn");
            Assert.AreEqual(Evaluation.EndGameIsolatedPawn, egWeights[EvalFeatures.ISOLATED_PAWNS], "EG Isolated Pawn");

            Assert.AreEqual(Evaluation.OpeningBackwardPawn, opWeights[EvalFeatures.BACKWARD_PAWNS], "OP Backward Pawn");
            Assert.AreEqual(Evaluation.EndGameBackwardPawn, egWeights[EvalFeatures.BACKWARD_PAWNS], "EG Backward Pawn");

            Assert.AreEqual(Evaluation.OpeningDoubledPawn, opWeights[EvalFeatures.DOUBLED_PAWNS], "OP Doubled Pawn");
            Assert.AreEqual(Evaluation.EndGameDoubledPawn, egWeights[EvalFeatures.DOUBLED_PAWNS], "EG Doubled Pawn");

            for (int rank = 0; rank < 6; rank++)
            {
                Assert.AreEqual(Evaluation.OpeningPassedPawn[rank + 1], opWeights[EvalFeatures.PASSED_PAWNS + rank], $"OP Passed Pawn, Rank = {rank}");
                Assert.AreEqual(Evaluation.EndGamePassedPawn[rank + 1], egWeights[EvalFeatures.PASSED_PAWNS + rank], $"EG Passed Pawn, Rank = {rank}");

                Assert.AreEqual(Evaluation.OpeningAdjacentPawn[rank + 1], opWeights[EvalFeatures.ADJACENT_PAWNS + rank], $"OP Adjacent Pawn, Rank = {rank}");
                Assert.AreEqual(Evaluation.EndGameAdjacentPawn[rank + 1], egWeights[EvalFeatures.ADJACENT_PAWNS + rank], $"EG Adjacent Pawn, Rank = {rank}");
            }

            Assert.AreEqual(Evaluation.OpeningKingAttack[0], opWeights[EvalFeatures.KING_PROXIMITY], "OP King Proximity, D0");
            Assert.AreEqual(Evaluation.OpeningKingAttack[1], opWeights[EvalFeatures.KING_PROXIMITY + 1], "OP King Proximity, D1");
            Assert.AreEqual(Evaluation.OpeningKingAttack[2], opWeights[EvalFeatures.KING_PROXIMITY + 2], "OP King Proximity, D2");

            Assert.AreEqual(Evaluation.EndGameKingAttack[0], egWeights[EvalFeatures.KING_PROXIMITY], "EG King Proximity, D0");
            Assert.AreEqual(Evaluation.EndGameKingAttack[1], egWeights[EvalFeatures.KING_PROXIMITY + 1], "EG King Proximity, D1");
            Assert.AreEqual(Evaluation.EndGameKingAttack[2], egWeights[EvalFeatures.KING_PROXIMITY + 2], "EG King Proximity, D2");

            Assert.AreEqual(Evaluation.OpeningKnightOutpost, opWeights[EvalFeatures.KNIGHTS_ON_OUTPOST], "OP Knight on Outpost");
            Assert.AreEqual(Evaluation.EndGameKnightOutpost, egWeights[EvalFeatures.KNIGHTS_ON_OUTPOST], "EG Knight on Outpost");

            Assert.AreEqual(Evaluation.OpeningBishopOutpost, opWeights[EvalFeatures.BISHOPS_ON_OUTPOST], "OP Bishop on Outpost");
            Assert.AreEqual(Evaluation.EndGameBishopOutpost, egWeights[EvalFeatures.BISHOPS_ON_OUTPOST], "EG Bishop on Outpost");

            Assert.AreEqual(Evaluation.OpeningBishopPair, opWeights[EvalFeatures.BISHOP_PAIR], "OP Bishop Pair");
            Assert.AreEqual(Evaluation.EndGameBishopPair, egWeights[EvalFeatures.BISHOP_PAIR], "EG Bishop Pair");

            for (int pc = 0; pc < Constants.MAX_PIECES; pc++)
            {
                for (int rank = 0; rank < 6; rank++)
                {
                    Assert.AreEqual(Evaluation.OpeningBlockedPawnTable[pc][rank + 1], opWeights[EvalFeatures.BLOCKED_PAWNS + (pc * 6) + rank], $"OP Blocked Pawn, Piece = {pc}, Rank = {rank}");
                    Assert.AreEqual(Evaluation.EndGameBlockedPawnTable[pc][rank + 1], egWeights[EvalFeatures.BLOCKED_PAWNS + (pc * 6) + rank], $"EG Blocked Pawn, Piece = {pc}, Rank = {rank}");
                }
            }

            Assert.AreEqual(Evaluation.OpeningBlockedPawnDoubleMove, opWeights[EvalFeatures.BLOCKED_DBL_MOVE_PAWNS], "OP Blocked Pawn Double Move");
            Assert.AreEqual(Evaluation.EndGameBlockedPawnDoubleMove, egWeights[EvalFeatures.BLOCKED_DBL_MOVE_PAWNS], "EG Blocked Pawn Double Move");

            Assert.AreEqual(Evaluation.OpeningPawnMajorityQS, opWeights[EvalFeatures.QUEEN_SIDE_PAWN_MAJORITY], "OP Queen Side Majority");
            Assert.AreEqual(Evaluation.EndGamePawnMajorityQS, egWeights[EvalFeatures.QUEEN_SIDE_PAWN_MAJORITY], "EG Queen Side Majority");

            Assert.AreEqual(Evaluation.OpeningPawnMajorityKS, opWeights[EvalFeatures.KING_SIDE_PAWN_MAJORITY], "OP King Side Majority");
            Assert.AreEqual(Evaluation.EndGamePawnMajorityKS, egWeights[EvalFeatures.KING_SIDE_PAWN_MAJORITY], "EG King Side Majority");

            for (int rank = 0; rank < 6; rank++)
            {
                Assert.AreEqual(Evaluation.OpeningKingClosest[rank + 1], opWeights[EvalFeatures.KING_NOT_IN_CLOSEST_SQUARE + rank], $"OP King Not in Closest Square, Rank = {rank}");
                Assert.AreEqual(Evaluation.EndGameKingClosest[rank + 1], egWeights[EvalFeatures.KING_NOT_IN_CLOSEST_SQUARE + rank], $"EG King Not in Closest Square, Rank = {rank}");
            }

            Assert.AreEqual(Evaluation.OpeningKingNearPassedPawn, opWeights[EvalFeatures.KING_IN_PROMOTE_SQUARE], "OP King in Promote Square");
            Assert.AreEqual(Evaluation.EndGameKingNearPassedPawn, egWeights[EvalFeatures.KING_IN_PROMOTE_SQUARE], "EG King in Promote Square");

            Assert.AreEqual(Evaluation.OpeningPhaseThruTurn, opWeights[EvalFeatures.GAME_PHASE_BOUNDARY], "OP Game Phase Boundary");
            Assert.AreEqual(Evaluation.EndGamePhaseMaterial, egWeights[EvalFeatures.GAME_PHASE_BOUNDARY], "EG Game Phase Boundary");
        }
    }
}