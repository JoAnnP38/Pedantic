using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;
using Pedantic.Collections;
using Pedantic.Tuning;

using Score = Pedantic.Chess.Score;
using Newtonsoft.Json.Serialization;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class EvalFeaturesTests
    {
        private static readonly EvalCache cache = new();

        [TestInitialize]
        public void Init()
        {
            cache.Clear();
        }

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
        [DataRow("r3k2r/2pb1ppp/2pp1q2/p7/1nP1B3/1P2P3/P2N1PPP/R2QK2R w KQkq a6 0 14")]
        [DataRow("4rrk1/2p1b1p1/p1p3q1/4p3/2P2n1p/1P1NR2P/PB3PP1/3R1QK1 b - - 2 24")]
        [DataRow("r3qbrk/6p1/2b2pPp/p3pP1Q/PpPpP2P/3P1B2/2PB3K/R5R1 w - - 16 42")]
        [DataRow("6k1/1R3p2/6p1/2Bp3p/3P2q1/P7/1P2rQ1K/5R2 b - - 4 44")]
        [DataRow("8/8/1p2k1p1/3p3p/1p1P1P1P/1P2PK2/8/8 w - - 3 54")]
        [DataRow("7r/2p3k1/1p1p1qp1/1P1Bp3/p1P2r1P/P7/4R3/Q4RK1 w - - 0 36")]
        [DataRow("r1bq1rk1/pp2b1pp/n1pp1n2/3P1p2/2P1p3/2N1P2N/PP2BPPP/R1BQ1RK1 b - - 2 10")]
        [DataRow("3r3k/2r4p/1p1b3q/p4P2/P2Pp3/1B2P3/3BQ1RP/6K1 w - - 3 87")]
        [DataRow("2r4r/1p4k1/1Pnp4/3Qb1pq/8/4BpPp/5P2/2RR1BK1 w - - 0 42")]
        [DataRow("4q1bk/6b1/7p/p1p4p/PNPpP2P/KN4P1/3Q4/4R3 b - - 0 37")]
        [DataRow("2q3r1/1r2pk2/pp3pp1/2pP3p/P1Pb1BbP/1P4Q1/R3NPP1/4R1K1 w - - 2 34")]
        [DataRow("1r2r2k/1b4q1/pp5p/2pPp1p1/P3Pn2/1P1B1Q1P/2R3P1/4BR1K b - - 1 37")]
        [DataRow("r3kbbr/pp1n1p1P/3ppnp1/q5N1/1P1pP3/P1N1B3/2P1QP2/R3KB1R b KQkq b3 0 17")]
        [DataRow("8/6pk/2b1Rp2/3r4/1R1B2PP/P5K1/8/2r5 b - - 16 42")]
        [DataRow("1r4k1/4ppb1/2n1b1qp/pB4p1/1n1BP1P1/7P/2PNQPK1/3RN3 w - - 8 29")]
        [DataRow("8/p2B4/PkP5/4p1pK/4Pb1p/5P2/8/8 w - - 29 68")]
        [DataRow("3r4/ppq1ppkp/4bnp1/2pN4/2P1P3/1P4P1/PQ3PBP/R4K2 b - - 2 20")]
        [DataRow("5rr1/4n2k/4q2P/P1P2n2/3B1p2/4pP2/2N1P3/1RR1K2Q w - - 1 49")]
        [DataRow("1r5k/2pq2p1/3p3p/p1pP4/4QP2/PP1R3P/6PK/8 w - - 1 51")]
        [DataRow("q5k1/5ppp/1r3bn1/1B6/P1N2P2/BQ2P1P1/5K1P/8 b - - 2 34")]
        [DataRow("r1b2k1r/5n2/p4q2/1ppn1Pp1/3pp1p1/NP2P3/P1PPBK2/1RQN2R1 w - - 0 22")]
        [DataRow("r1bqk2r/pppp1ppp/5n2/4b3/4P3/P1N5/1PP2PPP/R1BQKB1R w KQkq - 0 5")]
        [DataRow("r1bqr1k1/pp1p1ppp/2p5/8/3N1Q2/P2BB3/1PP2PPP/R3K2n b Q - 1 12")]
        [DataRow("r1bq2k1/p4r1p/1pp2pp1/3p4/1P1B3Q/P2B1N2/2P3PP/4R1K1 b - - 2 19")]
        public void ComputeTest(string fen)
        {
            Board bd = new(fen);
            EvalFeatures features = new(bd);

            HceWeights weights = Evaluation.Weights;

            Span<Evaluation.EvalInfo> evalInfo = stackalloc Evaluation.EvalInfo[2];
            Evaluation.InitializeEvalInfo(bd, evalInfo);

            Evaluation eval = new(cache, useMopup: false);

            // material + pst
            Score score = Evaluation.EvalMaterialAndPst(bd, evalInfo, Color.White);
            score -= Evaluation.EvalMaterialAndPst(bd, evalInfo, Color.Black);
            short evalScore = Evaluation.StmScore(bd.SideToMove, score.NormalizeScore(bd.Phase));
            short featureScore = features.Compute(weights, 0, HceWeights.PIECE_MOBILITY);
            Assert.AreEqual(evalScore, featureScore);

            // cached pawns
            score = eval.ProbePawnCache(bd, evalInfo);
            evalScore = Evaluation.StmScore(bd.SideToMove, score.NormalizeScore(bd.Phase));
            featureScore = features.Compute(weights, HceWeights.ISOLATED_PAWN, HceWeights.PP_CAN_ADVANCE);
            Assert.AreEqual(evalScore, featureScore);

            // mobility
            score = Evaluation.EvalMobility(bd, evalInfo, Color.White);
            score -= Evaluation.EvalMobility(bd, evalInfo, Color.Black);
            evalScore = Evaluation.StmScore(bd.SideToMove, score.NormalizeScore(bd.Phase));
            featureScore = features.Compute(weights, HceWeights.PIECE_MOBILITY, HceWeights.CENTER_CONTROL);
            Assert.AreEqual(evalScore, featureScore);
            
            // king safety
            score = Evaluation.EvalKingSafety(bd, evalInfo, Color.White);
            score -= Evaluation.EvalKingSafety(bd, evalInfo, Color.Black);
            evalScore = Evaluation.StmScore(bd.SideToMove, score.NormalizeScore(bd.Phase));
            featureScore = features.Compute(weights, HceWeights.KING_ATTACK, HceWeights.ISOLATED_PAWN);
            Assert.AreEqual(evalScore, featureScore);

            // pieces
            score = Evaluation.EvalPieces(bd, evalInfo, Color.White);
            score -= Evaluation.EvalPieces(bd, evalInfo, Color.Black);
            evalScore = Evaluation.StmScore(bd.SideToMove, score.NormalizeScore(bd.Phase));
            featureScore = features.Compute(weights, HceWeights.KNIGHT_OUTPOST, HceWeights.PAWN_PUSH_THREAT);
            Assert.AreEqual(evalScore, featureScore);

            // passed pawns
            score = Evaluation.EvalPassedPawns(bd, evalInfo, Color.White);
            score -= Evaluation.EvalPassedPawns(bd, evalInfo, Color.Black);
            evalScore = Evaluation.StmScore(bd.SideToMove, score.NormalizeScore(bd.Phase));
            featureScore = features.Compute(weights, HceWeights.PP_CAN_ADVANCE, HceWeights.KNIGHT_OUTPOST);
            Assert.AreEqual(evalScore, featureScore);

            // threats
            score = Evaluation.EvalThreats(bd, evalInfo, Color.White);
            score -= Evaluation.EvalThreats(bd, evalInfo, Color.Black);
            evalScore = Evaluation.StmScore(bd.SideToMove, score.NormalizeScore(bd.Phase));
            featureScore = features.Compute(weights, HceWeights.PAWN_PUSH_THREAT, HceWeights.MAX_WEIGHTS);
            Assert.AreEqual(evalScore, featureScore);

            // miscellaneous
            score = Evaluation.EvalMisc(bd, evalInfo, Color.White);
            score -= Evaluation.EvalMisc(bd, evalInfo, Color.Black);
            evalScore = Evaluation.StmScore(bd.SideToMove, score.NormalizeScore(bd.Phase));
            featureScore = features.Compute(weights, HceWeights.CENTER_CONTROL, HceWeights.KING_ATTACK);
            Assert.AreEqual(evalScore, featureScore);

            evalScore = eval.Compute(bd);
            featureScore = features.Compute(weights);
            Assert.AreEqual(evalScore, featureScore);
        }
    }
}