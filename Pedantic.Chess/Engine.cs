// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="Engine.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implement the functionality of the chess engine.
// </summary>
// ***********************************************************************

using Pedantic.Tablebase;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class Engine
    {
        private static readonly GameClock time = new();
        private static PolyglotEntry[]? bookEntries;
        private static HceWeights? weights;
        private static Color color = Color.White;
        private static readonly SearchThreads threads = new();
        private static readonly string[] benchFens =
        {
            #region bench FENs
			"r3k2r/2pb1ppp/2pp1q2/p7/1nP1B3/1P2P3/P2N1PPP/R2QK2R w KQkq a6 0 14",
			"4rrk1/2p1b1p1/p1p3q1/4p3/2P2n1p/1P1NR2P/PB3PP1/3R1QK1 b - - 2 24",
			"r3qbrk/6p1/2b2pPp/p3pP1Q/PpPpP2P/3P1B2/2PB3K/R5R1 w - - 16 42",
			"6k1/1R3p2/6p1/2Bp3p/3P2q1/P7/1P2rQ1K/5R2 b - - 4 44",
			"8/8/1p2k1p1/3p3p/1p1P1P1P/1P2PK2/8/8 w - - 3 54",
			"7r/2p3k1/1p1p1qp1/1P1Bp3/p1P2r1P/P7/4R3/Q4RK1 w - - 0 36",
			"r1bq1rk1/pp2b1pp/n1pp1n2/3P1p2/2P1p3/2N1P2N/PP2BPPP/R1BQ1RK1 b - - 2 10",
			"3r3k/2r4p/1p1b3q/p4P2/P2Pp3/1B2P3/3BQ1RP/6K1 w - - 3 87",
			"2r4r/1p4k1/1Pnp4/3Qb1pq/8/4BpPp/5P2/2RR1BK1 w - - 0 42",
			"4q1bk/6b1/7p/p1p4p/PNPpP2P/KN4P1/3Q4/4R3 b - - 0 37",
			"2q3r1/1r2pk2/pp3pp1/2pP3p/P1Pb1BbP/1P4Q1/R3NPP1/4R1K1 w - - 2 34",
			"1r2r2k/1b4q1/pp5p/2pPp1p1/P3Pn2/1P1B1Q1P/2R3P1/4BR1K b - - 1 37",
			"r3kbbr/pp1n1p1P/3ppnp1/q5N1/1P1pP3/P1N1B3/2P1QP2/R3KB1R b KQkq b3 0 17",
			"8/6pk/2b1Rp2/3r4/1R1B2PP/P5K1/8/2r5 b - - 16 42",
			"1r4k1/4ppb1/2n1b1qp/pB4p1/1n1BP1P1/7P/2PNQPK1/3RN3 w - - 8 29",
			"8/p2B4/PkP5/4p1pK/4Pb1p/5P2/8/8 w - - 29 68",
			"3r4/ppq1ppkp/4bnp1/2pN4/2P1P3/1P4P1/PQ3PBP/R4K2 b - - 2 20",
			"5rr1/4n2k/4q2P/P1P2n2/3B1p2/4pP2/2N1P3/1RR1K2Q w - - 1 49",
			"1r5k/2pq2p1/3p3p/p1pP4/4QP2/PP1R3P/6PK/8 w - - 1 51",
			"q5k1/5ppp/1r3bn1/1B6/P1N2P2/BQ2P1P1/5K1P/8 b - - 2 34",
			"r1b2k1r/5n2/p4q2/1ppn1Pp1/3pp1p1/NP2P3/P1PPBK2/1RQN2R1 w - - 0 22",
			"r1bqk2r/pppp1ppp/5n2/4b3/4P3/P1N5/1PP2PPP/R1BQKB1R w KQkq - 0 5",
			"r1bqr1k1/pp1p1ppp/2p5/8/3N1Q2/P2BB3/1PP2PPP/R3K2n b Q - 1 12",
			"r1bq2k1/p4r1p/1pp2pp1/3p4/1P1B3Q/P2B1N2/2P3PP/4R1K1 b - - 2 19",
			"r4qk1/6r1/1p4p1/2ppBbN1/1p5Q/P7/2P3PP/5RK1 w - - 2 25",
			"r7/6k1/1p6/2pp1p2/7Q/8/p1P2K1P/8 w - - 0 32",
			"r3k2r/ppp1pp1p/2nqb1pn/3p4/4P3/2PP4/PP1NBPPP/R2QK1NR w KQkq - 1 5",
			"3r1rk1/1pp1pn1p/p1n1q1p1/3p4/Q3P3/2P5/PP1NBPPP/4RRK1 w - - 0 12",
			"5rk1/1pp1pn1p/p3Brp1/8/1n6/5N2/PP3PPP/2R2RK1 w - - 2 20",
			"8/1p2pk1p/p1p1r1p1/3n4/8/5R2/PP3PPP/4R1K1 b - - 3 27",
			"8/4pk2/1p1r2p1/p1p4p/Pn5P/3R4/1P3PP1/4RK2 w - - 1 33",
			"8/5k2/1pnrp1p1/p1p4p/P6P/4R1PK/1P3P2/4R3 b - - 1 38",
			"8/8/1p1kp1p1/p1pr1n1p/P6P/1R4P1/1P3PK1/1R6 b - - 15 45",
			"8/8/1p1k2p1/p1prp2p/P2n3P/6P1/1P1R1PK1/4R3 b - - 5 49",
			"8/8/1p4p1/p1p2k1p/P2npP1P/4K1P1/1P6/3R4 w - - 6 54",
			"8/8/1p4p1/p1p2k1p/P2n1P1P/4K1P1/1P6/6R1 b - - 6 59",
			"8/5k2/1p4p1/p1pK3p/P2n1P1P/6P1/1P6/4R3 b - - 14 63",
			"8/1R6/1p1K1kp1/p6p/P1p2P1P/6P1/1Pn5/8 w - - 0 67",
			"1rb1rn1k/p3q1bp/2p3p1/2p1p3/2P1P2N/PP1RQNP1/1B3P2/4R1K1 b - - 4 23",
			"4rrk1/pp1n1pp1/q5p1/P1pP4/2n3P1/7P/1P3PB1/R1BQ1RK1 w - - 3 22",
			"r2qr1k1/pb1nbppp/1pn1p3/2ppP3/3P4/2PB1NN1/PP3PPP/R1BQR1K1 w - - 4 12",
			"2r2k2/8/4P1R1/1p6/8/P4K1N/7b/2B5 b - - 0 55",
			"6k1/5pp1/8/2bKP2P/2P5/p4PNb/B7/8 b - - 1 44",
			"2rqr1k1/1p3p1p/p2p2p1/P1nPb3/2B1P3/5P2/1PQ2NPP/R1R4K w - - 3 25",
			"r1b2rk1/p1q1ppbp/6p1/2Q5/8/4BP2/PPP3PP/2KR1B1R b - - 2 14",
			"6r1/5k2/p1b1r2p/1pB1p1p1/1Pp3PP/2P1R1K1/2P2P2/3R4 w - - 1 36",
			"rnbqkb1r/pppppppp/5n2/8/2PP4/8/PP2PPPP/RNBQKBNR b KQkq c3 0 2",
			"2rr2k1/1p4bp/p1q1p1p1/4Pp1n/2PB4/1PN3P1/P3Q2P/2RR2K1 w - f6 0 20",
			"3br1k1/p1pn3p/1p3n2/5pNq/2P1p3/1PN3PP/P2Q1PB1/4R1K1 w - - 0 23",
			"2r2b2/5p2/5k2/p1r1pP2/P2pB3/1P3P2/K1P3R1/7R w - - 23 93"
            #endregion
        };

        private static readonly string[] bench2Fens =
        [
            #region bench 2 FENs
            "r2q1rk1/pp1n1ppp/2pbpn2/8/3P4/3B1N2/PPPQ1PPP/R1B2RK1 b - - 7 11",
            "2r1rn1k/pp1n1ppp/2p1bq2/3p4/1P1P4/3BP2P/P1QNNPP1/1R3RK1 w - - 5 17",
            "3r1rk1/1p3pbp/pqn1p1p1/2pn4/2B5/2P1P1BP/PPQN1PP1/R2R2K1 b - - 3 16",
            "r2qk2r/p2bnpb1/2p1p1pp/3pP1B1/3P4/Q1N2N2/PP3PPP/R4RK1 w kq - 0 14",
            "r2qk2r/pp1nbppp/2p2n2/3p4/3P4/2N2PPP/PP3PB1/R1BQ1RK1 b kq d3 0 10",
            "r4rk1/ppqnbppp/4p3/3pN2n/3P1P2/2PQB3/PP1N2PP/R4RK1 b - f3 0 13",
            "r2qnrk1/1p1b2pp/p2b4/P1pPpp2/2P5/2N2P2/1PB1Q1PP/R1B2RK1 b - - 0 16",
            "1rbqr1k1/p4ppp/2n2n2/1p1p4/2pP4/P1P1PP2/2B1N1PP/R1B1QRK1 w - - 4 14",
            "2r2rk1/1p1b1pp1/1n2p2p/p2pP1q1/Pn1P1N2/1P4P1/3Q1PBP/R1B2RK1 w - - 2 20",
            "r4rk1/2qb2bp/3ppn2/ppp2pN1/5N2/2P1P1P1/PPQ1BP1P/R4RK1 b - - 1 18",
            "8/8/1k6/2p3pB/2K2b2/2B5/8/8 b - - 3 48",
            "6R1/7K/8/8/8/5k2/8/r7 w - - 17 75",
            "8/4b3/8/pB3Kp1/P7/1Pk5/8/8 w - - 1 101",
            "8/p7/Pb3k2/4p1N1/4P1K1/8/8/8 b - - 3 51",
            "8/7p/5k2/3p1p2/p2P2P1/4KP2/P7/8 w - - 2 43",
            "8/8/8/1P2k1P1/4Pp1P/3PbK2/8/8 w - - 1 66",
            "8/8/7p/4kPN1/6K1/p7/7P/1B6 w - - 1 62",
            "8/8/pKp5/P1Pk4/8/3b4/7B/8 w - - 58 204",
            "8/7k/8/4KN2/8/n3B2P/5P2/8 w - - 3 59",
            "8/3Q4/5K2/8/8/2k5/8/8 w - - 15 78",
            "2r5/5k2/R7/8/5p2/7P/5P1K/8 b - - 1 72",
            "8/p7/6R1/2k5/r5p1/4K1P1/8/8 w - - 6 48",
            "8/2B5/6p1/3b3p/p2PkP2/P5K1/7P/8 w - - 1 45",
            "8/8/5p1k/1R6/6P1/5P2/r7/5K2 w - - 11 55",
            "8/4kp2/R5p1/8/5P2/6K1/8/7r w - - 6 61",
            "8/4p1b1/8/NP1k2p1/P5P1/1P3P2/8/6K1 w - - 1 58",
            "R7/5kp1/8/p7/r6P/8/5K2/8 b - h3 0 56",
            "8/8/R4p2/P5kp/1r6/8/6K1/8 w - - 0 59",
            "8/3r1b2/5P2/4K3/1k6/8/8/2R5 w - - 45 110",
            "8/2k5/1p2Bp2/4p3/1PP1P1P1/8/3K4/6b1 b - - 90 135",
            "8/1R6/4Bk1p/p7/5K1p/3b1P2/P5r1/8 w - - 4 48",
            "1R6/1P3pk1/1r3p1p/4p3/4P1P1/2K2P2/6P1/8 w - - 31 59",
            "8/3P4/3B4/7p/2k4q/5R2/5PK1/8 b - - 1 67",
            "2R1K3/r7/8/6k1/1r4p1/8/8/6R1 b - - 21 69",
            "8/2R3pp/5pk1/2P1p3/4P3/5P2/2r3PP/5K2 w - - 1 33",
            "b7/5p2/1R5p/3rP1pk/3B4/3K4/7P/8 b - - 2 47",
            "8/1k5p/5Rp1/4B3/5P2/1b4P1/4r2P/2K5 w - - 3 42",
            "8/p3Q3/7k/1r1P4/4p3/4P3/1P6/1K6 w - - 2 41",
            "5B2/1B6/8/Pb1p2pp/1Pk5/5PP1/r7/6K1 w - - 1 75",
            "6k1/2R3p1/1P6/5K2/7P/1r2B1P1/5Pb1/8 w - - 5 42",
            "6k1/5ppp/3p4/2nP4/2p2P2/4BB2/rr2NKPP/3R4 b - - 11 37",
            "8/5p2/2n1r1kp/p1r3p1/5p2/1RP2N1P/5PP1/R4K2 b - - 3 49",
            "rn6/1pB2kp1/2p4p/p2bp3/P1p3PP/2P2P2/4PKB1/3R4 b - - 2 31",
            "r7/r7/2nB1pkp/1pP1p3/1P2K1P1/P2RR2P/8/8 b - - 1 47",
            "3R4/b4rpk/2p5/2P3p1/p1R3P1/Pr5P/5PK1/2B5 b - - 1 39",
            "6k1/p4pp1/1pn1p3/6PP/2P3BK/P5Q1/5q2/8 b - - 5 42",
            "8/p4p1k/1n3Qp1/3q3p/5P1P/8/P7/K1R5 b - h3 0 43",
            "8/4r1k1/1p1Q2p1/2p3q1/2B5/PP2p1P1/4K3/8 b - - 1 62",
            "3b4/3k4/q3p2Q/3p1p2/1P5p/8/6PP/4R2K b - - 0 37",
            "3Q4/5pkp/2p3p1/2p2n2/4NP2/3P3P/2q3PK/8 w - - 6 35",
            "r1bq1rk1/p2pppbp/1pn2np1/2p5/2P5/1P1PPN2/PB2BPPP/RN1Q1RK1 b - - 0 8",
            "r1bq1rk1/1p1nbppp/2p1p3/p2p4/P1PPn3/N4NP1/1PQBPPBP/R4RK1 w - - 2 11",
            "r1bqr3/p4pbk/1pnp1npp/2p1p3/2P1PP2/2NPB1PP/PP1QN1B1/R4RK1 w - - 0 13",
            "r1bq1rk1/pp1pb1pp/2n1pp2/2pnP3/5P2/1PN2NP1/PBPP3P/R2QKB1R w KQ - 0 9",
            "r1bqk2r/ppp2pp1/2np1n1p/4p3/1PBbP3/P1NP1N2/2P2PPP/R1BQK2R w KQkq - 1 8",
            "r1bq1rk1/pp3pb1/2np1np1/2p1p1Bp/4P2P/2PP1NN1/PPB2PP1/R2QK2R w KQ - 2 13",
            "r1b2rk1/pp1nq1pp/2pbp3/3p1p2/2PPnN2/5NP1/PPQ1PPBP/R1B2RK1 w - - 10 11",
            "r1b1r1k1/1p1nqpbp/p1pp2p1/4p2n/2PPP3/2N1BNPP/PP3PB1/2RQR1K1 w - - 0 13",
            "r2qkb1r/5p2/bnn1p1pp/ppppP3/5BPP/3P1NN1/PPPQ1PB1/R4RK1 b kq - 3 15",
            "r1bq1rk1/3nppb1/pp1p1np1/2pP3p/P1P1PP2/2NB1QN1/1P1B2PP/R3K2R b KQ - 1 17",
            "8/8/1Bp3k1/3r1ppp/1Pbp2P1/2bN1P1P/8/3R2K1 b - - 1 43",
            "8/5k2/8/5q2/5p1p/6bP/1P4P1/3Q2BK b - - 1 46",
            "6k1/5pp1/2Rp3p/3Pp3/1P6/2B2K1P/r2N2P1/4r3 w - - 6 33",
            "2r5/p6p/1p4p1/1P3k2/8/3r1P2/PR3KPP/4R3 b - - 1 32",
            "6r1/pbp1kp2/4p2p/2p5/2B3p1/4PP2/PP3KPP/2R5 b - - 5 23",
            "8/5pk1/6p1/p4rPp/2PR3P/1P6/2P1r3/1KR5 b - - 1 38",
            "2N3Q1/8/6pk/3P3p/1b5P/3q3P/5K2/8 b - - 7 59",
            "8/3r1k2/4r1p1/1RRbB2p/5P1P/8/6PK/8 b - - 8 52",
            "8/6pk/1n3r1p/r7/N2P3P/R5P1/8/4R1K1 w - - 1 43",
            "4r1k1/pp3ppp/2p5/8/N7/7P/PP1r2B1/R5K1 w - - 0 26",
            "r1bq1rk1/5ppp/1n3n2/2bPp3/pP6/2NQNP2/BPP3PP/R1B1K2R b KQ - 0 16",
            "r1b1nrk1/1p2qp1p/1n4p1/pN4N1/PbpP1B2/6P1/2Q1PPBP/R4RK1 w - - 0 18",
            "2r2rk1/ppqb2bp/3pp1p1/2n4n/4P3/2N1BN1P/PPB2PP1/1R1Q1RK1 w - - 6 18",
            "2r1rnk1/1b1qppb1/1p1p2pp/7n/1PPNP3/5PPP/1B3QK1/1BRR1N2 b - - 2 25",
            "1r3rk1/pppbq1n1/3p3b/2PPpp1p/1P2P1p1/2NB2P1/P1Q2PP1/3RRNK1 b - - 1 23",
            "r1bqk2r/ppp5/2np1bpp/3Npp2/2P1P2P/3P2P1/PP3PB1/R2QK1NR b KQkq h3 0 10",
            "r1bq1rk1/pppn1ppp/3p2n1/3Pp3/2P1P3/2PBBPP1/P3N1KP/R2Q3R b - - 4 13",
            "r3rnk1/pp3pp1/7n/3p2qP/3Pp2b/1PN1P1Nb/PBQ1BP2/2KR2R1 b - - 1 20",
            "2r1r1k1/2q1bppp/p2pbn2/1p2nN2/4P1P1/2N1Q2P/PPPRB2B/2K4R b - - 1 19",
            "rn1q1rk1/p2b1ppp/2pbp3/3n4/2QP4/3N2P1/1P2PPBP/RNB2RK1 w - - 3 12",
            "3b4/3k4/p1pp4/5q1p/P2Br3/2P3PP/2Q2P2/1R4K1 w - - 2 33",
            "2b1rr2/p1p3k1/1pPb3p/4p1p1/8/P3B1PN/1P1RPP1P/R5K1 w - - 0 24",
            "6k1/1q3p2/4r1pn/P2pP1QP/2p5/4PP2/5BK1/4R3 b - - 0 44",
            "5k2/ppq2rb1/2p3R1/3p1Q2/8/2P1N1P1/PP5P/2K5 w - - 5 29",
            "1rbr2k1/5pbp/4p1p1/2RnN3/p2B4/P5P1/1P2B2P/1K1R4 b - - 5 32",
            "5k2/1bpq2p1/3p1b1p/Qp1P1p2/1P1P3P/3PBBP1/5P2/6K1 w - - 10 52",
            "6r1/R5pk/p6p/1p2Q2P/6P1/P4NK1/1P3P2/1bq5 b - - 22 47",
            "6k1/5p2/4pQp1/1P1p2Pp/3P1P2/r2BbN1P/1q3PK1/8 w - - 5 43",
            "1k6/1pp1q2p/p2p4/2nP1B2/6Pr/P7/1P3Q2/1KR5 b - - 2 37",
            "3r3k/1br2pp1/p2pBn1p/P7/1p1NP3/3R1P2/1PP3PP/3R2K1 b - - 0 27",
            "r2nqrk1/bpp4p/4b3/pP1p4/P1PP3Q/8/1B2BPPP/R4RK1 w - - 0 22",
            "3rq1k1/5bb1/1p4pp/pP1r1p2/P1pNp2P/B1P1P1P1/3RQPK1/3R4 w - - 73 69",
            "r1bq1rk1/ppppnppp/8/4N2Q/B2PR3/8/PPP2PPP/2R3K1 b - - 2 13",
            "q4rk1/n4ppp/nbpp2b1/1p1Pp1B1/1P2P1PN/2P4P/2B1QP2/1N3RK1 w - - 1 20",
            "4bq1k/2r5/3bpr2/1p1pNpQp/pP1P2p1/P1PB2P1/5P1P/2R1R1K1 w - h6 0 34",
            "r2q1rk1/pp3p2/3pbPp1/2pNn2p/2P1P3/3B4/PP4PP/3QRRK1 w - - 1 21",
            "r2qr1k1/1bp2pp1/1p1p3p/p1nN4/2PQ4/1PR1P3/P3BPPP/3R2K1 w - - 1 20",
            "r3r1k1/2p2p2/1pnq1np1/p2p3p/3P1N2/P1P2Q1P/1P3PP1/3RRNK1 b - - 0 19",
            "r4r2/4ppkp/pqppbnp1/8/4P3/2N5/PPPQBPPP/3R1RK1 w - - 0 14",
            "r3k2r/pp1n1ppp/2pn1q2/3p3P/3P1PQ1/2NB4/PP3PP1/2KR3R w kq - 6 16",
            "r7/4b1k1/8/5Rp1/1pK2nBp/2p2P2/p1P5/B7 b - - 5 55",
            "8/1r2k1p1/p4p2/3R1P2/P7/1P1R1P2/1r6/5K2 w - - 1 50",
            "3r4/3N2k1/8/n2P2p1/1R4B1/2r3P1/P4PK1/8 b - - 4 41",
            "8/6pk/4Qp1p/8/4p3/5qPP/5P2/5K2 b - - 9 52",
            "8/5p2/1b2b3/1k1pPp2/3P1K2/4B1P1/1r2NP2/7R w - - 5 43",
            "7r/2p1nk2/2Pp2p1/p7/NpR2PPP/1P6/1P3K2/8 w - - 1 36",
            "6k1/5pp1/4p1np/pQ1r4/5P1P/1P6/6PK/8 w - - 3 46",
            "8/pr6/6k1/1pp3p1/4P3/P1Br1PKP/1P6/2R5 b - - 7 42",
            "r2r2k1/7R/6p1/p5P1/1p6/1P6/P1P5/1K5R b - - 0 36",
            "8/r4pk1/3p3p/1B1Pn3/4P2P/6P1/1R3K2/2r5 b - - 5 56",
            "1r2r1k1/1b3ppp/p1npqn2/4p3/1PP2P1N/4P1P1/1B1Q2BP/3R1RK1 w - - 3 20",
            "2kr1r2/1bq1bp2/1pp3pp/p3n3/P1B1P3/1PN1B2P/2P1Q1P1/R4R1K b - - 0 19",
            "5r2/2qn1pk1/3ppb2/p1p4r/NpPnP3/1P1PB2P/P2Q2B1/1R3R1K b - - 2 34",
            "r1b1k2r/1p2bpp1/p3pn1p/q3P3/3N3B/1PN5/1PP1Q1PP/2KR3R b kq - 0 15",
            "r1r3k1/2qn1pp1/5n1p/pBp5/PpQ1P3/1Nb1BPP1/P5KP/2RR4 w - - 2 29",
            "r3k2r/pbpn1p2/1p1p1qpp/8/2PPpPQN/2P1P3/P1B3PP/R4RK1 w kq - 2 15",
            "2r1k1r1/1b1n1p1p/p2b4/5p1q/PpBPp3/5NPP/1P1BQPK1/2R1R3 b - - 1 23",
            "r2r2k1/pbqnbpp1/1p2p2p/4N3/2P4R/8/PPB1QPPP/2B2RK1 w - - 2 20",
            "r2q1rk1/1b2bppp/p3p3/1p1nN3/3PNQ2/1B6/PP3PPP/3R1RK1 w - - 4 17",
            "1r1q1r1k/n3p1np/1p1p4/pP1Pb3/4Rp1B/5P2/P1NQ2PP/3R1B1K w - - 1 26",
            "r3q1k1/1bpn1pbp/p2p2p1/Qp6/3P4/2PB2BP/PP1N1PP1/5RK1 b - - 1 19",
            "6nk/1pqnbpr1/p2p4/3Pp1P1/PPp1P2P/5QN1/2P2P2/R4RK1 w - - 5 25",
            "2r1r3/1ppq1ppk/2np3p/pQ6/3NPP2/1PP1R3/1P4PP/5RK1 b - - 10 24",
            "3r2k1/p1q1npp1/2b1p2p/3r4/5P2/1P6/P1Q1B1PP/2R1RN1K w - - 1 30",
            "3rr1k1/2q2p1p/p2b2pP/1ppPp1P1/5P2/P6R/1PPQ4/2K1RB2 w - - 5 25",
            "3rb1k1/pp2qpp1/7p/QP1p4/3Pp3/4P2P/P1r2PP1/1R2KN1R w K - 1 22",
            "2rqr1k1/5pbp/p5p1/8/3P4/1Q2nP1P/PP2N1P1/3R1BKR w - - 0 22",
            "4rr2/1p1R1ppk/2p1n2p/p1q1p3/P3P1Q1/2P3NP/1P3PP1/3R2K1 b - - 2 27",
            "r1b2rk1/pp2qppp/8/4n3/8/4B2P/PPQ1BPP1/R4RK1 b - - 1 16",
            "5rk1/3qpp2/r1p3p1/3nPb1p/1p5Q/1B3N1P/PP1B1PP1/2R3K1 b - - 2 25",
            "r3r3/1pp3k1/5pp1/2n1p2p/n3P1N1/2PP1KP1/P1R4P/2BR4 w - h6 0 31",
            "8/2Q4n/6pk/3B1pq1/P1PP1p2/1K3P2/5P2/4q3 b - - 3 48",
            "8/5pk1/4p1p1/p1p1P2p/P1P1NP1b/1n1Q3P/1q1B2PK/8 w - - 12 40",
            "1k1r4/ppp4p/2n2R1Q/2q5/2B5/4N2P/6P1/6K1 b - - 2 32",
            "3r1k2/pp3pp1/2br3p/4p3/2BnP3/P3BP2/PR3KPP/2R5 b - - 7 23",
            "8/3n1pk1/2qbp2p/p1p5/2P3pP/1PNN2P1/P3QP2/6K1 b - - 8 79",
            "8/p3r2k/Pp4q1/1P2nNp1/2R1Q1P1/4P2p/8/6K1 b - - 0 58",
            "8/1bp2nkp/1p4p1/1P1Np1P1/2P1q2P/2Q3R1/5P2/6K1 w - - 5 40",
            "3r1r1k/5pnp/b4RpB/p2pP3/Pp1P4/7P/1P5P/1B1R3K b - - 5 32",
            "2r2b1k/5p1p/2q2p2/3P4/1p4N1/6QP/5PP1/4R1K1 b - - 0 30",
            "3k4/2r3pp/5p2/pPn5/3K2B1/1P5P/6P1/R7 w - - 3 41",
            "3rk3/5R1p/2b3p1/p7/P1B5/1P3P1P/5P2/6K1 w - - 1 34",
            "8/p1r2n1p/2p5/N1P2p1k/3R1P2/P6P/3K4/8 w - - 3 40",
            "8/r5p1/3k1p1p/4p2P/4P3/1R1b1BP1/5P1K/8 b - - 23 62",
            "6k1/p5p1/1p2p2p/1Np1n3/8/PP2P3/5PPP/1b1B2K1 b - - 0 28",
            "4k3/8/8/1P5P/1q2P3/3Q4/5PK1/8 b - - 0 73",
            "5K2/8/4q3/pQ1p4/P3k3/8/1P6/8 b - - 18 57",
            "6k1/1R1b2p1/5p2/1p1r3p/3B4/2P1KP2/6PP/8 b - - 5 34",
            "8/2k5/1p2q3/p7/P1N1B3/1P6/K3R3/8 w - - 25 149",
            "6k1/6b1/8/1p2pPP1/pP1p1p1B/2r5/6K1/7R w - - 4 57",
            "1r4k1/8/3NpRp1/1P2P2p/3K1P2/6P1/7n/8 b - - 2 63",
            "1r6/1Pp5/r3P3/2BPk1p1/8/7K/8/1R6 w - - 1 46",
            "8/6p1/7p/2Np3P/3P2k1/1Rn1P3/4rP2/5K2 b - - 3 91",
            "4r3/p4k1p/2p2p2/1pP3p1/5P2/3NB3/PP2K2P/8 w - - 1 29",
            "8/4p3/1p1k2p1/1P2p1P1/2Pb4/5Nr1/4K3/5R2 w - - 0 47",
            "1k6/5R2/1p6/p2N4/2P1P2p/1P4pP/r5P1/6K1 b - - 2 42",
            "4R3/1k6/1p1r4/5B2/PP1b4/4B2n/4K3/8 b - - 3 56",
            "8/6k1/5R1p/4p3/2p5/2P2N1P/1r2bPP1/6K1 w - - 1 37",
            "2k5/2n4p/P2Qn1p1/5p2/4pP2/8/4KP2/8 b - - 1 52",
            "6k1/5pp1/5n1p/2B2P2/p5P1/P2r4/4K3/R7 b - - 9 47",
            "3r4/1p1r1k1p/p3np2/7p/2R1PP1P/1P1p1KP1/PB1R4/8 w - - 6 37",
            "4r3/2k2p1p/1p1p2p1/p3r3/PnPKP3/1P3B1P/3R2P1/4R3 w - - 11 37",
            "7r/pp2kppp/2r1p3/3n2P1/3P3R/P2K1P2/1P1B2P1/R7 b - - 2 23",
            "4r1k1/pp2bp2/2p2n1p/4n1p1/4P3/PP4PP/4NPB1/2BR2K1 b - - 0 24",
            "r4r2/2bk2p1/1p1p2Pp/3Pp2P/1PP1RpK1/5P2/1B6/7R b - - 0 59",
            "6k1/2r2p2/2P3p1/3qp3/7p/2Q4P/5PP1/2R3K1 b - - 69 80",
            "8/6k1/2pb4/pp4pp/2pPq3/P1P1PNP1/1P3Q2/6K1 b - - 1 39",
            "6k1/8/2p3p1/5q2/8/P1Nn3r/1P6/K2R2Q1 w - - 4 43",
            "3r2k1/r4ppp/Bp6/3p4/1p1P4/4PP2/1Pb2KPP/R1R5 b - - 5 23",
            "r2r2k1/6bp/p3p3/1p1p1p2/5P2/2P3P1/PPNR3P/3R2K1 b - - 1 30",
            "2r1k2r/2q1bp2/pn2p3/6pP/1ppP4/4P3/PPQ1B1P1/1KBR3R w k - 2 23",
            "3q1rk1/1p3rp1/2bp1bQp/p2Np3/P1P5/1P3N1P/4RPP1/3R2K1 b - - 6 22",
            "3r1rk1/p4q2/2bp4/2p1bp1p/2P2Np1/4P1P1/P1B2PP1/1R1QR1K1 w - - 8 27",
            "2rr2k1/p3qppp/1p1bpn2/8/2PR3Q/1P3B2/PB3PPP/3R2K1 b - - 2 19",
            "3qr1k1/1p3r1p/2ppb3/p3b1B1/P1P1P1p1/3R2PP/1P1Q2BK/2R5 b - - 1 27",
            "2kr3r/ppp2ppp/2nqp3/3p4/3P1P2/3Q1NP1/PPP2P1P/2KRR3 b - - 2 13",
            "2q1r1k1/5r2/2p2p1p/1pp1nRp1/3bP3/1P1P2NP/R1P1Q1PB/7K b - - 4 35",
            "r2r2k1/pbq2ppp/1p2pn2/8/1PP5/5NP1/P1Q1RPBP/R5K1 w - - 7 22",
            "2b4r/2k1bp1p/prpppp2/5P2/1q2P3/1N1B2Q1/2P3PP/3R1R1K w - - 1 23",
            "2r4k/1pr2pp1/p2bpn1p/P7/3P4/1qBB1QPP/1PR2P2/R5K1 w - - 3 22",
            "r5k1/1p1n1pp1/p3pnp1/2q5/8/PBNQPP2/1P4PP/3R2K1 b - - 2 23",
            "8/1k4pp/1r3n2/2rp4/8/1P2P1Pq/P4Q1P/R1B1K2R w KQ - 0 28",
            "2b2rk1/1pp1qppp/5n2/4p3/1PP5/4P3/2Q2PPP/B3KB1R w K - 0 15",
            "r1b5/pp3kpp/1bp2q2/8/4Q3/2N2P2/PPP4P/R2R3K w - - 5 23",
            "3bkn2/1qr5/p2p1np1/1p1P4/1P2PQ2/R1N5/P2BB3/3K4 b - - 0 30",
            "3r1bk1/pp1q1bpp/5p2/2p2N2/5BP1/P1Q4P/1PP2P2/2K1R3 w - - 0 24",
            "rr2b1k1/2b2pp1/3pnn1p/1P2p3/p1P1P3/B6P/3N1PP1/RN2RBK1 b - - 4 26",
            "r5k1/1p1qb1pp/p3b3/3pPp2/3B3P/P2B1QP1/1P3P2/4R1K1 w - - 3 24",
            "r5k1/pq3ppp/1p6/2p1r3/8/3Q2P1/PP2PP1P/3RR1K1 w - - 0 20",
            "rq4k1/5ppp/pPp2b2/1n1p4/1Q3P2/2N3P1/1PP4P/4RBK1 w - - 1 26",
            "8/8/p1p5/P7/1KP2rkp/1P2R3/8/8 b - - 2 67",
            "8/4k3/8/1K3p2/2B2P1b/2B5/p1r4P/8 w - - 6 67",
            "8/3PR3/5k2/3r3p/1B6/4K1PP/8/8 b - - 2 60",
            "8/8/4b1kp/1P4p1/P2B1p2/5P1P/3N2P1/6K1 b - - 1 55",
            "6k1/R7/6pp/8/4PK1P/p5P1/r7/8 b - - 1 39",
            "8/R3p3/3nkp2/1b5K/6P1/5B2/5P2/8 b - - 8 49",
            "8/R4p2/4k1p1/8/4P2P/p7/2P2K2/r7 b - - 3 50",
            "8/5p2/8/r6k/3R4/p5Pp/P4K1P/8 w - - 6 66",
            "8/p5pp/1pp2p2/2k1p3/P3P3/1PP2P2/2K3PP/8 w - - 0 31",
            "8/3N1k2/8/8/4KPPR/r7/1b6/8 b - - 14 57",
            "8/8/5p2/6pp/8/6P1/3kbK2/8 w - - 0 69",
            "8/7p/1P1k4/2p2p2/2K2P1P/8/8/8 b - - 0 52",
            "8/8/4K1p1/5p2/5k1r/8/8/8 b - - 1 84",
            "8/n7/8/4B2p/6k1/6P1/5K2/8 b - - 9 95",
            "8/8/4p3/Pk3p2/1P3K1p/7P/8/8 b - - 0 58",
            "8/4k3/8/2n1r3/8/7K/8/8 b - - 7 86",
            "R7/6p1/8/8/4K1k1/7p/8/8 w - - 2 74",
            "8/8/5pp1/2b5/6k1/8/3NK3/8 b - - 3 78",
            "8/2k5/P1P5/1K6/4B3/8/5b2/8 b - - 8 128",
            "8/5p2/3P4/8/7P/6P1/1P2k1PK/8 b - - 0 50",
            "8/8/2B5/1p6/1P3k1p/3b4/5K2/8 b - - 11 62",
            "8/8/3pKP2/3P2k1/8/8/8/1r6 w - - 0 83",
            "8/8/8/6P1/1P1p4/3k4/6K1/4r3 w - - 0 73",
            "8/1k6/6Bp/5K2/P7/7P/3b4/8 b - - 26 61",
            "8/7B/7p/6kP/1pK3n1/8/8/8 w - - 36 89",
            "8/8/B7/3Pk1K1/3n4/5P1P/8/8 w - - 1 51",
            "8/8/4B2p/3Kp3/1k3b2/7P/8/8 b - - 65 83",
            "8/8/4N3/6p1/6P1/5Pk1/4K3/2b5 w - - 76 159",
            "8/8/3k1p2/4p3/3n2K1/4NP2/8/8 b - - 8 76",
            "8/1R6/8/4k3/3b4/8/4KP2/8 w - - 9 51",
            "r1bqr1kb/pp3p1p/2p2npB/3pn3/4P2P/1NN2P2/PPPQB1P1/2KR3R w - - 0 14",
            "r2qr1k1/ppp3pp/2n1b1n1/4Pp2/P2p1P1b/2PB3P/1P1NQ1PB/R3NRK1 w - f6 0 17",
            "1rb1kb1r/2q2ppp/p1np1n2/4p3/P3P3/1pP2B2/NP1BNPPP/R2Q1RK1 w k - 0 14",
            "r1bq1rk1/1pppbppp/p1n2n2/1B2N3/3P4/2P5/PP3PPP/RNBQR1K1 w - - 0 9",
            "r3k2r/2q1b3/pnb1p2p/np1pPp2/2pP3P/1PP1NNB1/P2QBP2/RR4K1 w kq - 0 21",
            "2rq1rk1/1b2nppp/pp2pn2/3pN3/1bPP1P2/1P1B1N2/PB2Q1PP/3R1RK1 b - - 2 15",
            "r1b1kb1r/2qn1ppp/p2pp3/1pn3P1/3NP3/1BN1BP2/PPPQ3P/R3K2R w KQkq - 1 13",
            "r2q1rk1/pbpnbppp/1p3n2/3p4/3P4/2NBPN2/PPQ2PPP/R1B2RK1 w - - 4 10",
            "r1bqkb1r/1p3ppp/p4n2/n2p4/P1pP4/2N1P3/1P1NBPPP/R1BQ1RK1 b kq - 4 10",
            "r2qkb1r/pp1npppb/2p2n1p/8/2BP1N1P/2P3N1/PP3PP1/R1BQK2R b KQkq - 0 10",
            "8/8/2pR1p2/2Pn1p1p/2kP3P/6P1/r3NPK1/8 w - - 10 58",
            "8/pp2r1kp/6b1/5p2/8/1P3P2/P2RNKPP/8 b - - 2 29",
            "8/4k1p1/1pb2pnp/p1p5/P3P3/1P2KP2/1NP3PP/3N4 b - - 0 26",
            "3nk3/pp2p3/6pB/8/2r5/2PR2P1/P3P1KP/8 w - - 3 27",
            "r7/2k5/p1P3p1/RPB1p3/P1K4b/4Pp1P/8/8 w - - 0 41",
            "6k1/5p1p/4pbp1/3p4/6PP/4PN2/R4PK1/5r2 b - - 2 39",
            "1Rn1r1k1/p1P2p1p/6p1/8/8/P5BP/5PPK/8 w - - 5 36",
            "2r1r2k/6p1/7p/8/7P/1P1R2K1/P2R4/8 b - - 1 46",
            "8/2K1Q3/2P3pk/7p/7P/2Pq4/8/8 w - - 1 59",
            "3r4/5p2/4b3/1kb4p/2p2B1P/6P1/2K5/R2N4 b - - 11 49",
            "8/p2b3k/1p1b2p1/1P1B4/P2B2P1/5P2/5K2/8 b - - 4 42",
            "6k1/p7/1pr5/2p2R2/1PPp4/P7/8/1K6 w - - 1 48",
            "5k2/5P2/1p1p2Kp/4pP2/R7/7P/2r5/8 b - - 0 56",
            "8/6k1/8/3B1pp1/8/5P2/2q2BK1/8 w - - 14 60",
            "4k3/1p3n2/8/2R2N2/4P2P/5K2/8/1r6 w - - 11 78",
            "8/8/4k3/5r2/2PQ4/6P1/6KP/8 b - - 0 106",
            "5rk1/2p4p/3p2p1/2pP2b1/2B1P3/8/6K1/8 w - - 0 51",
            "8/5k2/7Q/8/p4PP1/Pb5P/7K/8 b - - 0 45",
            "8/2R3pk/8/1p3P1p/3r4/P6P/6P1/6K1 b - - 1 34",
            "8/3b1k2/r2P1p2/5P2/1RNK4/8/8/8 w - - 3 57",
            "6rk/p4p1n/1p2rNnQ/3pP3/3P2P1/q4P2/3R3P/5RK1 b - - 1 31",
            "2r3k1/5p1p/pBq3p1/Pr1np3/6P1/3Q3P/1PP2P2/2K1R2R w - - 11 32",
            "1r1r3k/2q2ppp/p1b5/P3P3/2P1pQ2/1P6/4B1PP/R2R2K1 w - - 3 24",
            "r2r1qk1/1p3pp1/p3p2p/1b5N/3P2Q1/1P2P2P/6P1/2RR2K1 b - - 6 24",
            "6R1/1p1bq2p/1rp2kp1/p5r1/n6Q/6P1/1P3PBP/R5K1 w - - 2 33",
            "4r1k1/2p2pp1/p2r1q1p/7b/3PP3/2R2NPP/P1Q2P2/4R1K1 w - - 1 26",
            "r7/r3pp1k/3p1q1p/1R2n1pP/P3P1P1/1PR2P2/1Q2B3/1K6 w - - 1 46",
            "r4r1k/p4ppp/1pqp1P2/3Np1b1/4P3/1P2BQ2/PP5P/5R1K b - - 3 22",
            "7k/1pr3p1/p1q1pr1p/2n1p3/2P2NQ1/1P5P/P1R3P1/3R2K1 w - - 0 36",
            "r5k1/1pPb1p2/p2p1qp1/7p/3P4/1PP1rNPP/3Q2P1/3B1RK1 b - - 0 24",
            "8/4qp1k/1p1r2p1/p1n4p/P1R5/4PN1P/5PP1/1Q4K1 w - - 4 45",
            "7k/p4r2/1p1pq3/8/4bPpp/P3B3/1P1RQ1PP/6K1 w - - 6 54",
            "3r2k1/1R3pp1/3B1b2/p1P2Q1p/3p3P/3P2P1/5P1K/2q5 b - - 0 33",
            "kr4n1/pbR4p/q7/8/1QP5/P5P1/1P3P1P/2KB4 w - - 5 32",
            "2q3k1/p4pp1/2p3r1/2Rp4/3Pb3/1P2B1P1/P3QP2/5K2 w - - 9 33",
            "5rk1/Q3nppp/2p1p3/4q3/2P1B3/1P4P1/P6P/5RK1 w - - 1 28",
            "r5r1/1bp1k3/1p1p1p1p/p6P/2P1PPp1/1PB2nP1/P1B2K2/2RR4 w - - 0 24",
            "r1b1r1k1/pp1n1pp1/2p4p/8/3Pp2B/2P1P3/P3BPPP/R4RK1 b - - 1 16",
            "r1b4r/3kn2p/1ppp2p1/p4p2/P3P2P/1N3P2/1PP2P2/2KR1BR1 b - h3 0 16",
            "2r3k1/1b3ppp/2q1p3/8/1Q2pP2/4B3/PP4PP/2R3K1 b - - 0 28",
            "8/5p1k/6p1/7p/5P1P/1q4P1/8/4Q1K1 b - - 38 119",
            "8/4k3/3p4/1P1n1p2/4pP2/QPr5/B7/6K1 w - - 1 62",
            "8/r4p2/PR2pb2/2P2pk1/3P4/2BK1PPp/7P/8 b - - 2 41",
            "8/1p1k3r/p1p2R2/2Bp1P2/7n/PPP4P/2P5/3K4 b - - 0 38",
            "5k2/2Q5/p6p/6p1/8/q6P/P4P2/6K1 w - - 7 40",
            "8/1pp2bp1/p4p2/1k2r2p/8/2P1N2P/5PP1/R5K1 b - - 9 32",
            "6k1/3b4/3p1p2/p3b1p1/4B3/4B1P1/1r3PK1/3R4 w - - 0 30",
            "6k1/1R6/5p1p/8/p7/7P/rP1r2P1/2R3K1 b - - 1 31",
            "8/5p1p/p5p1/1pkb4/3r3P/PP1BR1P1/5P2/4K3 b - - 0 33",
            "8/pp2k3/3p4/2p2pNp/P1P1nP2/1PnR2P1/7P/5K2 b - - 0 51",
            "6k1/6p1/6p1/1p4Kp/pB5P/P7/1Pb5/8 b - - 63 97",
            "8/p3Bkp1/1p2b2p/7P/8/5PP1/4P1K1/8 w - - 1 41",
            "8/R5K1/5B2/P2k2P1/5r2/8/8/8 b - - 2 55",
            "6k1/3R4/7p/4N3/4K1B1/2b5/7P/8 w - - 13 55",
            "5b2/5p2/1p3k2/p4p1P/P1B5/1P4P1/6K1/8 w - - 3 184",
            "8/4r3/8/5p2/3K2k1/2P3P1/4p3/4R3 w - - 10 73",
            "8/5B2/3k4/1p6/pP6/P1bb1K2/8/2B5 w - - 76 94",
            "8/8/7k/8/2RB1p2/1p1r4/1K6/8 w - - 1 116",
            "8/2p1k3/1p6/2pB4/2P4p/1P3K1b/P6P/8 b - - 1 38",
            "8/5p2/4k1p1/7p/2Nn2PP/1p1PKP2/8/8 b - - 5 46",
            "3r1k2/5pp1/2P2b1p/8/1q1p1P2/3B1QBP/5PK1/8 w - - 0 34",
            "6k1/4r3/3p1nP1/pp1P2Q1/3q4/P3R3/8/5NK1 w - - 1 52",
            "2r4k/q5pp/P2R1p2/4p3/2p5/2Q3P1/4PP1P/6K1 b - - 4 43",
            "6kb/3Q4/3p2b1/1p5N/1Bn1p1P1/5B2/4PPK1/1q6 w - - 6 48",
            "r3kb1r/1p3ppp/p2p4/2p2P2/4P3/5R1P/PPP3P1/R1B3K1 b kq - 0 16",
            "3r1rk1/8/1pB2p1p/pP2pP1N/Pb1p2PP/2nP1KB1/8/R7 w - - 24 52",
            "r7/3k4/4p3/3pPp1p/nrpPnB2/5KPP/1P2R3/R2N4 b - - 1 39",
            "1r3k2/r2b1p2/Pbp4p/3p1p2/1P3P2/1N1B2P1/R2K3P/2R5 w - - 8 35",
            "6k1/1ppb1pp1/5n1p/3P4/1P5P/3BN1K1/1Q3PP1/r1r5 b - - 10 32",
            "2r3r1/k2b1p2/4pP1p/3pP3/1R2n1P1/P4B2/4NN1K/1R6 w - - 0 44",
            "8/n2Qnpk1/p3p1pp/2q5/2Np3P/3P2P1/4PPB1/6K1 w - - 2 28",
            "4r1k1/1p3pn1/2p2q2/2P5/5P1p/3Q3P/5RBK/8 b - - 3 43",
            "1R6/p3r1p1/3k1p2/q1p1p3/P3Q3/3PP2P/2P3P1/6K1 b - - 0 35",
            "8/q4p1k/6pp/1p1pP3/1PpP4/2P3P1/2Q1RK2/r7 b - - 0 62",
            "8/p6r/r1bb1pk1/P1p3p1/2PpP3/3P1N1P/1RPB2K1/6R1 w - - 16 52",
            "5k2/2q2pp1/p2pbn1p/4p3/1P2P3/4N1PP/3Q1PBK/8 w - - 0 38",
            "r2r4/1pp1kp2/pb2pNp1/4P1N1/5PKP/8/b5B1/3R3R b - - 0 30",
            "6rk/4R1pp/3P4/p2bB3/8/P2q4/1Q3P1P/K7 b - - 1 38",
            "8/5p1k/3p1Ppp/Pq1Pn3/6B1/4Q2P/r4PK1/4B3 w - - 2 57",
            "6k1/5p2/4p1p1/1q2P2p/p2RQ2P/1p4P1/1P3P1K/2r5 b - - 4 42",
            "3b1rk1/p4qnp/2R5/1Q3p2/8/1p1PN1P1/4PPKP/8 b - - 5 34",
            "2Q5/5b1k/p3Npp1/3p4/3PpP1r/4P1q1/PP4P1/5RK1 w - - 7 33",
            "6k1/pp3pp1/2pq1nr1/3p3p/P2P4/1P2PQ1B/8/4NKN1 w - - 5 44",
            "r3k2N/1b2P3/p4p2/q6p/2pp3P/5P2/PPP1Q3/2K1R3 w q - 0 29",
            "4b2R/4r3/p3q3/Pk1pBp2/2pP1P1Q/1pP1PK2/1P6/8 b - - 4 48",
            "2R5/p3qp1k/1p4pp/8/1P6/5QPB/P2nPPKP/3r4 w - - 5 37",
            "2r3k1/pp5p/3p2p1/4p2n/8/5q2/PPPQ3R/R5K1 b - - 3 30",
            "7r/p1p1kpp1/bpB1p3/3nP1pr/2pPN3/P1P2P2/5KPP/2R4R w - - 7 22",
            "8/4p1k1/3p1pp1/3Pb2p/1q2P2P/r2B1QP1/2P2PK1/5R2 b - - 7 47",
            "3r4/2RP2pk/1p3p2/2bQpq1p/8/5P2/6PP/3R3K w - - 0 39",
            "r1b5/1pq4k/6pp/p1P1bp2/8/1Q6/PP2BBPP/3R3K w - a6 0 45",
            "r7/1b4pk/1P2np1p/4pN2/2p1P2P/3q1PB1/Q5P1/4R1K1 w - - 1 33",
            "r7/r1pn1kpp/1p1ppb2/nP2p3/1B1PP3/2P2NP1/3N1P1P/1R2R1K1 w - - 1 24",
            "r3r2k/pR5p/8/4q3/2p5/P2bP1QP/5PPK/3R4 w - - 2 31",
            "3b3k/5prp/3pb3/2p1p1PQ/2P1P3/3P4/2qBKP2/3N2R1 w - - 5 36",
            "2k1rbbr/1pp3pn/p1p5/4pNNp/4P1P1/2P3KP/PP3P2/R1B1R3 w - - 4 20",
            "2b2b1r/4k1pp/p3pp2/q7/4B1Q1/4B3/PP3PPP/3R2K1 w - - 1 21",
            "4n2k/1p4rp/2p2pr1/8/3B1R1P/P3PQ2/q1BK1P2/8 b - - 0 33",
            "2b1rr2/1kp3pp/pp1b1p2/2p1n3/2P1P3/1P2NPB1/P2RN1PP/3R2K1 b - - 6 21",
            "r1br2k1/pp1nbp1p/2p1p1p1/8/3PN3/2PB1NP1/PPKR1P1P/4R3 b - - 1 18",
            "3r1k2/p4pp1/3b1q1p/1p1P1N1P/2p1QPP1/8/PP4R1/1K6 w - - 2 33",
            "3r4/7k/1p1qb1pp/5p2/2Pp4/pP1B1PP1/P2Q2KP/4R3 b - - 3 37",
            "8/2r2p1k/1q1p2p1/1P1Pn3/R2NPn2/5P2/7P/Q4B1K b - - 0 41",
            "8/6p1/3rbpkp/p1q1p3/1p2P3/1P2NP1P/2P1Q1PK/R7 w - - 17 45",
            "b1q3k1/5p1p/pQ3p2/2nP2P1/P6P/5BB1/r7/2R3K1 b - - 2 33",
            "r3k2r/pp1b1p2/4p2p/2b3pn/7B/2PB1N2/PP3PPP/R4RK1 w kq - 0 16",
            "6k1/5pp1/pr1qp3/Q2p3R/2pP1PP1/n1P2P2/2P1NK2/8 w - - 3 55",
            "r7/5pkp/6p1/3Np1b1/p1p1P3/Pq5P/1PQ1RPP1/6K1 w - - 4 40",
            "3r4/4Q3/2p1R1pk/2P2p2/r2q3p/8/5PPP/4R1K1 b - - 0 34",
            "r6r/p2nkp2/R1p3p1/2b1p1Pp/2n1P2P/3N1PN1/1PPB4/4K2R b K - 2 22",
            "2r5/p7/b7/2p1kpBp/3bp2P/1P2N1P1/P4P2/2R3K1 w - - 4 33",
            "6k1/5p1p/1p2pp2/8/P1q5/6P1/4PPKP/3Q4 w - - 0 25",
            "r5k1/1n4pp/4pp2/p2nN3/N7/4P3/1P3PPP/2R3K1 w - - 0 28",
            "8/pbp4R/2nbk3/8/4p3/3rB3/PP2K2P/2R5 w - - 0 33",
            "2r3k1/p3n1p1/1b2pp2/B6p/3P4/4PN2/4KPPP/R7 b - - 1 25",
            "8/4k3/2prp3/2N3p1/1P2RnK1/2PP4/r7/4R3 b - - 4 45",
            "1r4k1/3n1ppp/4r3/4p3/8/4BN1P/1P3PP1/4R1K1 w - - 0 30",
            "3r3k/n4p1p/5N2/8/1p6/p7/PnP3PP/2K2Q2 b - - 2 33",
            "3r1r2/2p3pk/p7/1p2P3/3P1pp1/1B2B3/PP6/R5K1 w - - 2 30",
            "8/4bpk1/4p2p/1br2p2/1p6/1B5P/PPN2PPK/3R4 b - - 5 37",
            "5rk1/pp1q1pb1/4p2p/3pPnp1/3P4/4BN1P/PP1Q1PP1/2R3K1 w - - 2 19",
            "2r2rk1/3q2p1/1Q2p2p/1pP1P3/3Pp3/1b2B3/6PP/R1R3K1 b - - 2 26",
            "2r2rk1/1p1nbpp1/p1p2n1p/3p1b2/1P6/1NBP1NP1/1P2PPBP/R4RK1 w - - 2 15",
            "r2r3k/1pQ2p1n/5qpp/p1P5/8/PP2P3/5PPP/2R2RK1 w - - 0 25",
            "3r1rk1/1ppnppbp/p1n1b1p1/8/2NP4/1P3NP1/PB2PPBP/R1R3K1 w - - 1 16",
            "q5k1/6pp/1pp1rp1n/p2p3P/Pb1P1P2/1P1QNNP1/2R2PK1/8 b - - 16 34",
            "6k1/q4p2/1n1P2pp/4n3/P2b3B/2p2Q2/1r3NPP/R4R1K w - - 7 31",
            "2r2bk1/5p2/q3pQ1p/p2pP1pP/Pp1N4/nP2PPB1/6P1/3R2K1 b - - 1 33",
            "4r1k1/5q1p/rN1p2p1/PQnP4/4p3/R4pP1/5P1P/1R4K1 b - - 12 48",
            "3qk2r/3nbppp/2B1pn2/1p6/3P4/6P1/Q3PP1P/1NB2RK1 b k - 0 15",
            "6Rn/7P/1p6/8/1k3P2/3K4/8/8 b - - 4 83",
            "6K1/6Q1/8/8/8/k6P/8/8 b - - 10 82",
            "8/5k2/2n3pK/1p2P3/1P4P1/2B5/8/8 b - - 0 66",
            "7k/1R6/7P/5K2/8/6r1/8/8 w - - 19 74",
            "8/1b6/7p/8/K5k1/P4pB1/1P5P/8 b - - 1 42",
            "8/6pp/2Knk3/P7/4p2P/4P3/5P2/8 b - - 0 54",
            "8/p4B2/3b4/1P4p1/P4k2/8/6KP/8 b - - 0 52",
            "8/2k5/P5b1/5R2/6P1/6PK/8/8 b - - 0 60",
            "8/5b2/8/1p3kPp/7P/2B2P2/5K2/8 b - - 30 66",
            "8/R7/4k3/8/7r/p3K3/8/8 w - - 20 88",
            "R7/8/4k3/p3p3/P1P5/2KP3r/8/8 b - - 3 54",
            "8/5k2/1p1n3p/2bN3P/2P2P2/3B4/8/3K4 w - - 89 128",
            "8/2N3kp/6p1/8/R3p3/5n1P/6KP/8 b - - 1 45",
            "8/1pk2b2/3p1p2/PB1P1P1p/3KP2P/8/8/8 b - - 0 54",
            "8/8/3R1pk1/6pp/8/6P1/r4P2/6K1 b - - 19 85",
            "8/R7/5k1N/6p1/P3K1P1/2r5/8/8 w - - 2 67",
            "5k2/5p2/4p3/1r6/RP4p1/2K5/5P2/8 b - - 1 40",
            "8/5p2/2k5/4PK1p/7P/2r1p3/8/3R4 w - - 0 47",
            "8/8/8/2R5/pk1P2p1/6K1/Pr4P1/8 w - - 3 54",
            "3b4/pp5p/4k1p1/4B3/8/P4PP1/1P3PK1/8 w - - 1 33",
            "r1b1kb1r/p2p1ppp/1q2p3/2p1P3/2P1NP2/3Q4/P2B2PP/R4RK1 b kq - 0 16",
            "r2n1rk1/1p2pp1p/p2p2p1/3P1b2/1PPQ4/8/q1N1BPPP/2R2R1K w - - 1 18",
            "r4rk1/4qppp/2p1pn2/p2p4/P1bPPB2/6P1/2Q2PBP/RR4K1 b - - 2 20",
            "1k1r3r/1p4p1/p1n2p2/2Pb1q2/Q3p3/1P2P1Pp/P2RBP1P/2R1B1K1 w - - 0 24",
            "1r4k1/3nbpp1/2p1p2p/1qPp4/1p1P4/4PN1P/rBQ2PP1/R3R1K1 w - - 0 25",
            "r1b1k2r/1p5p/p1nBppp1/q2p3B/8/2PQ4/P1P2PPP/R4RK1 w kq - 0 15",
            "1r3rk1/2q2ppp/p2p1b2/P1nP4/1PB1p3/R4P2/N1PQ2PP/4K2R b K - 2 27",
            "3q2k1/1ppb1r1p/1N1nn3/r7/1p3p2/5P2/PQ2BBPP/R2R2K1 w - - 4 24",
            "7r/3q1pk1/p5r1/3pPbb1/3P2p1/6Q1/RPn1NPPN/3RB1K1 w - - 3 32",
            "3q1rk1/1b2bp1p/1pp2p2/r7/1n1PP3/2NB1R2/4N1PP/1R1Q2K1 w - - 2 19",
            "5rr1/1p5R/pk6/4Pp2/1P1pbP2/R5P1/P1PKB3/8 b - - 0 37",
            "4k1r1/8/p3pR2/Pp1b2B1/1PpP3P/3n4/2B1pP1K/R7 w - - 3 44",
            "7Q/1p2nk2/6q1/p3Bp1p/P7/2P2p1P/2P3P1/6K1 w - - 0 44",
            "4q1k1/5p2/1Q1n2p1/P2p4/1n6/8/6PP/2R3K1 w - - 0 45",
            "2br4/pp4bk/2p1p1p1/P1N1P2p/1P1pR3/7P/B1P2PP1/6K1 b - - 2 31",
            "8/1r1PR3/2k2p2/br4p1/pp4P1/2pKP1B1/P4P2/1R6 w - - 1 44",
            "r5kb/pb5p/3p1p1P/6p1/2P1p3/2P5/2P2PP1/R1B1K2R b KQ - 0 25",
            "7r/3pkppp/bR2pn2/4n3/8/N1P5/P4PPP/R5K1 b - - 0 24",
            "4rk2/3b1Bp1/p4pP1/b1P4n/P2P1P2/2N3pP/N7/1R4K1 b - - 1 36",
            "8/2p4k/2n1p1p1/P3P1Qp/8/q2p1P2/3B2PP/5K2 b - - 3 36",
            "r1b2rk1/pppqbppp/n7/8/P1pNN3/4B3/1PP1QPPP/R2R2K1 b - - 2 12",
            "r2qk2r/np1b1pp1/p3p2p/2bpP3/3N3P/P1PB2P1/3BQP2/R3R1K1 w kq - 0 21",
            "r4rk1/1p3ppb/1qp4p/p1npp3/2P5/PP1P1NPP/R2NPKB1/QR6 w - - 0 18",
            "r1bqk2r/5pbp/p1np4/1p1Np3/P3p3/2P5/1PN2PPP/R2QKB1R b KQkq a3 0 13",
            "r3k2r/pp2b1p1/2p1pp2/3nP1B1/PnBPN1Rp/5P2/1P3P1q/2RQK3 w kq - 0 17",
            "r1b1r1k1/1p3pb1/2p4p/p1B2pqn/2P1P3/1PN3PP/P1Q1RPB1/3R3K b - - 0 22",
            "3rr1k1/pp3pbp/1np1q1p1/2N4n/4P3/2N1BP2/PPP1Q1PP/R4R1K b - - 5 21",
            "r2q1rk1/5pp1/1npNp1bp/p3P3/Pp1P4/1Bn1Q2N/5PPP/R3R1K1 w - - 2 24",
            "2kr1bn1/pp1n2p1/4pqP1/1B1prp2/3N1Np1/2P5/PPK5/R1BQ1R2 b - - 4 20",
            "3qrrk1/4bppp/p1p1bn2/8/PppPP3/4BRP1/1P2QNBP/R5K1 w - - 8 21",
            "r3brk1/1pp1n1bp/3p4/p4pqp/2PPp2N/PPB1P1PB/5P1K/2R1QR2 w - a6 0 20",
            "r1bk3r/ppqnb1p1/2p1pnBp/8/3P4/3Q1NP1/PPP2P1P/R1B1R1K1 b - - 2 13",
            "r1bqkb1r/ppn1p1pp/2n2p2/8/Q2N4/2N3P1/3PPPBP/R1B1K2R b KQkq - 3 11",
            "1rr3k1/1bq1p1bp/4n1p1/NP1p1p2/P2PnP2/B4NPP/6B1/2RRQ1K1 b - - 2 26",
            "2rqr1k1/4bp2/1pnp1np1/4p3/p1P1P3/P1N1BN1b/1P2BP2/R2QK1R1 w Q - 0 21",
            "r1bq1rk1/ppp2ppp/2p2n2/2b1N3/4P3/3P4/PPP2PPP/RNBQ1RK1 b - - 0 7",
            "r1bqk2r/1p2npbp/p5p1/3Pn3/8/8/PPB1NPPP/RNBQ1RK1 w kq - 5 12",
            "1nbqkbr1/5p1p/3p1n2/rNp1p1B1/1p5P/8/PP3PP1/R2QKBNR b KQ h3 0 13",
            "rn1q1k1r/1b3ppp/p2p1n2/2bP1N2/1p6/6P1/PPP2PBP/R1BQR1K1 w - - 0 13",
            "r2qbn1k/pp2n1p1/1bp2r1p/3p3R/3P1PP1/5NNP/P1B5/2BQ1R1K b - - 7 25",
            "3r4/5bk1/1q2pppp/p1p5/1nB1QP2/1P5P/2P3PK/R3B3 w - - 4 38",
            "2r3k1/1q3ppp/4p3/1pr5/p2N4/4P2P/1P1B1PP1/1Q3RK1 b - - 8 30",
            "1r3bk1/2q2ppp/p2p4/1p1B1P1Q/P2p4/7P/1P3PP1/2B1R1K1 b - - 2 27",
            "2r3k1/1q3pb1/4p1p1/4p1Pp/PNbp4/5P2/1P1Q3P/R4BK1 w - - 8 34",
            "R1n2rk1/1q3pb1/6p1/4N3/1p1p1Q2/3P4/1PP2PP1/R5K1 w - - 0 42",
            "4r1k1/5pn1/p4Bpq/1b6/p1p2PPP/2P5/5Q2/R3R1K1 w - - 1 45",
            "1rr3k1/p4p1p/q3p1p1/3pQ3/1P3P2/1RP5/6PP/4R2K b - - 2 26",
            "r2r2k1/2p2p1p/4q3/1Q6/1Np5/P2p2P1/5P1P/R5KR w - - 1 26",
            "2R2bk1/5ppp/4pnn1/p2p4/1q2b3/4N1BP/BP2Q1PK/8 w - - 4 40",
            "rn3rk1/p3bppp/b1p1p3/B2p4/2pP4/1P3NP1/P3PPBP/R3R1K1 b - - 0 14",
            "8/8/2B1k2p/1P2p1pP/3bK1P1/8/8/8 w - - 43 76",
            "8/8/3k1p2/3P4/p4KP1/p1P5/B1b5/8 w - - 4 56",
            "8/3b4/1k1p3p/3Bp3/1KP4P/8/5P2/8 w - - 2 50",
            "6k1/8/6PK/7R/8/6P1/6r1/8 w - - 51 96",
            "8/2K5/5B2/3r4/8/2R5/8/1k6 w - - 57 80",
            "8/3k4/3P4/4Pp2/3B1P2/1p4Kp/6b1/8 b - - 43 114",
            "8/KB6/P5k1/3N3p/5P1P/3n4/8/8 b - - 0 56",
            "5r2/4R3/2k5/2p5/8/1P6/6K1/8 b - - 2 78",
            "8/8/7k/1p5p/1p3N1P/2b2KP1/P7/8 w - - 2 50",
            "8/8/6Bp/7P/1p1k4/r2P4/1K6/8 w - - 10 79",
            "rnbq1rk1/pppp1ppp/1b3n2/8/2PPp3/1Q6/PB1NPPPP/RN2KB1R w KQ - 3 8",
            "rn2k2r/pb1q1ppp/2p1pn2/1p6/PbpPP3/2N2N2/1PQ1BPPP/R1B2RK1 w kq - 6 10",
            "rnb1k2r/4ppbp/p2p1np1/qPpP4/4P3/2N2P2/PP2N1PP/R1BQKB1R w KQkq - 0 9",
            "r2q1rk1/ppp2ppp/1b1p1n2/8/B2nP1b1/2NN4/PPPP1PPP/R1B1QR1K b - - 5 10",
            "r1b2rk1/pp3pbp/1npp1qp1/3Pp2n/4P1P1/1BN4Q/PPPBNP2/R3K2R b KQ - 4 14",
            "r1b2rk1/1p2bppp/1q1ppn2/p2Pn3/1pPNP3/P1N5/4BPPP/1RBQ1RK1 b - - 2 12",
            "r1bqkbnr/p1pp1p1p/1p2p3/3P2p1/2n5/5N2/PP1BPPPP/RN1QKB1R w KQkq - 0 7",
            "rn1qkb1r/pp2nppp/4p3/2PpP3/4b2N/8/PPPNBPPP/R1BQK2R b KQkq - 2 8",
            "r1bqk2r/1pppbppp/p1n2n2/8/B2pP3/5N2/PPP2PPP/RNBQ1RK1 w kq - 0 7",
            "r2qkb1r/ppp2ppp/2n3n1/4P3/2Pp2b1/5NP1/PP1NPPBP/R1BQK2R w KQkq - 3 8",
            "8/3k4/Np3Rp1/pn6/6PP/3K4/6P1/6b1 b - - 1 44",
            "6k1/8/8/p6R/P3Kp2/3B3P/3b2r1/8 w - - 2 51",
            "8/5pkp/1bN1pp2/p7/Pp6/1P2P1PP/5P2/5K2 b - - 2 37",
            "8/p2n1p2/2k3p1/P1p2p1p/2P2P1P/1N1P2P1/1K6/8 w - - 3 47",
            "6k1/1R6/3N3p/r2b3P/5KP1/5P2/8/8 w - - 1 65",
            "8/1k6/2p5/1p1p4/p2P4/K1P2R2/PP1r4/8 w - - 22 53",
            "R7/6k1/5rBp/3K2bP/5p2/5P2/8/8 w - - 47 68",
            "8/p4k2/3R4/2N2p2/2p2P1p/2K4P/4b3/2b5 w - - 2 59",
            "8/2R4p/3p1k2/4p3/4PpB1/8/5KP1/7r w - - 7 62",
            "2r5/k6P/3R2PK/1R6/P7/1P6/8/8 b - - 0 64",
            "5k2/5p2/4b2p/p5pP/P3Q1P1/3B1P2/5K2/3q4 b - - 13 49",
            "3k4/pb1p4/1p4p1/2rN1nPp/4RK2/1P5P/P1PR4/8 w - - 1 30",
            "r7/1p3Bk1/p4n1p/4pb2/8/2P2R2/3Q2P1/7K b - - 0 32",
            "3r2k1/2p3b1/1p1p2pp/3P4/p1P3P1/Pn1BB2P/1P6/4R1K1 b - - 0 28",
            "r2r2k1/2p3pp/pp3p2/8/8/4B3/1RP2PPP/4RK2 b - - 1 23",
            "1n2k3/4b3/1n2r2p/4pNp1/p5P1/P2PP3/1P1NKP2/2R5 b - - 10 39",
            "2Q5/6p1/2P3nk/1q2p2p/3p3P/5PP1/5NK1/8 b - - 6 61",
            "4rbk1/R3pp1p/2N3p1/3PP3/8/4r2P/6P1/5R1K b - - 0 41",
            "8/pp1r1r2/2p3kp/5b2/P3NR2/1P3R2/2P1K2P/8 w - - 3 37",
            "r5k1/r2b1pp1/P1p1p3/8/4P3/2R3P1/5P2/1R3BK1 b - - 0 34",
            "2n2k2/ppq2pp1/3pb2r/6Qp/4P3/1BP5/P1P4P/1K1R1R2 b - - 5 23",
            "6k1/2q2ppp/4rn2/p7/1bN1p3/1P1pP1PP/P2P1PB1/Q4RK1 b - - 4 23",
            "r5k1/4qpp1/3r4/1P5p/2RNbP1P/1P2P1P1/1K1R4/4Q3 b - - 4 49",
            "7r/5k2/1p3q2/p1pp3p/P5rn/1PQ3N1/5PP1/2R1R1K1 w - - 0 29",
            "4r1k1/1p5p/p1b1qpr1/8/P3P3/1P2R3/1B2Q1PP/4R2K b - - 5 33",
            "r4rk1/pp1bppbp/5np1/3P2B1/2nP2P1/2N2B1P/PP3P2/3RK1NR w K - 1 15",
            "5rk1/pp3p1p/2bPq1p1/4b3/4P3/2p2B2/P5PP/3RQRK1 w - - 0 25",
            "8/4k3/bp1r1p2/p1pBq3/P3P2r/1P2RPRp/3Q3K/8 b - - 2 55",
            "1k1r1r2/pp2n3/3pq2R/8/3Q1BPp/1P3P2/P6P/5RK1 b - - 4 34",
            "7k/6p1/4p2p/3bPp1P/1P1R1P1q/8/1rrBQ1P1/3R2K1 b - - 19 55",
            "4rrk1/pbqn2pp/3b1n2/1Ppp4/4p2N/1P2P1P1/PB1NBP1P/R2Q1RK1 w - - 1 15",
            "1r1q1rk1/p3npb1/4p1p1/3nP1Pp/1PbPBP1N/B1p3N1/2P2Q1P/R3K2R w KQ - 5 24",
            "2kr3r/pp1b3p/1qnbp1pP/n2p4/3Pp3/PNP1BNP1/4BP2/1R1Q1K1R w - - 0 18",
            "r4rk1/4qpbp/bpnpp1p1/pN6/1nPPP3/B4NP1/5P1P/R2QRBK1 w - - 4 16",
            "3r1bk1/2qr1p2/2n1bn1p/2p1pNp1/p1N1P1P1/2P4P/PP2QPB1/R1B1R1K1 w - - 2 25",
            "2kr1b1r/1p1npppp/pq3n2/2pP4/5Bb1/2N2N2/PPP1Q1PP/2KR1B1R w - - 7 12",
            "1k5r/1bq1b1r1/ppn2pn1/2pB4/2PpNPP1/3P3p/PPNBQ2P/R4R1K b - - 5 24",
            "rn1qk2r/1b3ppp/p4n2/1p1Pp3/1b4P1/2N3NP/PPP2P2/R1BQKB1R w KQkq - 1 11",
            "r2qnrk1/3p1pb1/b1nPp1pp/1Np5/P3P3/2N1BP2/1P4PP/R2QKB1R b KQ - 2 14",
            "r1b2rk1/ppq1bppp/2n3n1/4P3/2Np4/3P1NP1/PPP1Q1BP/R1B1K2R w KQ - 5 12",
            "r7/5pkp/4b1pb/3pP3/3P3Q/5NPP/2q2PB1/5RK1 w - - 63 69",
            "4r1k1/2b2pp1/1p3n1p/1P5q/4P2P/5PPB/4QB2/3R2K1 w - - 3 34",
            "r3r1k1/p2nb2p/2n3p1/1pp2pP1/3p1p2/PP1P4/1BPN1P1P/R4KR1 w - b6 0 23",
            "2rq1r2/8/3p2pk/3N1b1p/3QN3/5PP1/PP6/3KR3 w - - 5 31",
            "r5k1/p3Rpb1/1n4pp/1Q1q4/1P1p1P2/6P1/5B1P/5BK1 b - - 7 35",
            "1Q3bk1/3q1p1p/p5p1/1b1R4/8/Pp2NN1P/4rPP1/6K1 b - - 4 29",
            "r4rk1/3q1ppp/8/3Pp3/2Q5/8/PP3P1P/K1RR4 b - - 4 25",
            "5r2/1pq1n1bk/7p/p1p1P3/P1Np4/1P1P4/1B4Q1/4R1K1 w - - 1 35",
            "4n2r/r3kppp/pp2p3/n1b1N1P1/P1p2B2/2N1P3/1P3PKP/1R1R4 w - - 1 18",
            "6k1/4nppp/2n5/3p4/2pPNP2/PqP3P1/1r3B1P/2R1R1KB w - - 13 36",
            "6rk/4R1bp/7r/ppp2p2/3p1p2/1P1P1B2/P1P2P1P/5R1K w - - 4 35",
            "4r3/r6p/3p4/2pPbp2/N1P2p1k/1P1KpP1P/P5P1/4R2R w - - 74 90",
            "7k/pp3p2/1npb1q2/8/3PB3/1Q3PP1/PP3BK1/8 w - - 0 36",
            "3r2k1/5pp1/2RP3p/4Q3/1p4P1/3q3P/P4PK1/8 b - - 0 46",
            "1r2n3/4Prkp/p2p2p1/1p1p1p2/5P2/P1PB3P/4R1P1/2K1R3 w - - 1 26",
            "R1b2rk1/6pp/2p1r3/1p2n2B/2p1NB2/2P3R1/7P/6K1 w - - 7 44",
            "8/2rbk3/2pR1p1p/p1P2p2/1rBRpPP1/1P2P3/P4K1P/8 b - - 2 34",
            "5rnk/3r1ppp/pbN5/4P3/PP1p4/6P1/3B1PBP/3R2K1 b - a3 0 30",
            "3r2k1/5p2/6p1/2q5/2pN4/1pBbQP1P/1P4K1/8 w - - 5 48",
            "3q4/pR6/P4rk1/7p/2P2PpP/3pQ3/6PK/8 b - - 0 44",
            "2r3k1/1p5p/3pN1pb/3Pn3/1P2q3/1Q4RP/P1r2BP1/5RK1 b - - 6 29",
            "2r2k2/p1r1p2p/3p1pbP/1P1P2p1/2B3P1/qP3P2/P2Q4/K1R1R3 b - - 5 28",
            "2r4k/4q1p1/4rn1p/1B1p4/8/pPb2P1Q/P1P2B1P/1K1R2R1 w - - 8 32",
            "r2qbrk1/pp5p/6n1/2Pp1p2/PP6/1Q3NP1/5KB1/3R3R b - - 1 25",
            "r4rk1/pbq2p1p/1p3pp1/2P5/4P3/2P2N2/P2Q1PPP/1R2R1K1 w - - 1 17",
            "r3r3/5ppk/2p4p/2Pp1q2/pP1B3b/P2pP2P/3Q2P1/R4RK1 b - - 3 34",
            "1r1qr1k1/p4p2/6p1/2p2b2/2Q1n3/2P5/P1P1NPP1/3RKB1R b K - 0 20",
            "r1b1r1k1/pq4p1/1p1B4/2p1PpnP/2PP3R/2PQ1pK1/8/6R1 b - - 1 27",
            "2rq2k1/pp1b2np/2p2p2/3p1pn1/3P3N/2N4P/PPBQ1PP1/4R2K w - - 12 27",
            "r3r1k1/1pp2pp1/3p2n1/pP4R1/4PPNp/5Q1P/P2q2P1/4R2K w - - 0 29",
            "8/ppk3r1/4b3/3pP3/7R/2PBK3/P7/8 b - - 6 35",
            "8/4n1k1/4r3/2R5/P1P5/3b3R/7K/8 b - - 1 54",
            "4b1k1/6p1/3Rp2p/2r5/8/P5KP/2N3P1/8 w - - 0 51",
            "8/p4pkp/4b3/8/3RP3/r2B1KP1/7P/8 b - - 0 38",
            "8/8/8/1Kp5/4k3/q5p1/1RR5/8 w - - 10 97",
            "1r4k1/5p2/6pp/8/1pR5/pP2K3/P1P3PP/8 b - - 2 40",
            "8/p1k2p2/4p3/8/3p1q2/5P2/P5K1/3R4 w - - 0 47",
            "2r2k2/R4pp1/3Pp2p/1p6/8/6P1/P4PKP/8 w - - 1 33",
            "3K4/5P2/7k/5Q2/q7/7P/8/8 b - - 34 133",
            "R7/3r1kp1/4Np1p/2b2P2/6PP/5K2/8/8 b - - 10 48",
            "5k2/1p3p2/7p/p3PQ2/Pp1P1p2/1P6/4KPq1/8 w - - 2 60",
            "8/4bp2/1p3np1/2k1p2p/r1P1P2P/N1K2PP1/1P4B1/3R4 b - - 4 50",
            "6k1/R5n1/p1rp1b1p/P1p3p1/2Pp2PP/3P1N2/3B1K2/8 b - - 0 39",
            "5r2/3R1pkp/1p4p1/3B4/1P4P1/R3b1PK/8/5r2 b - - 2 40",
            "5r2/pp3pk1/2pr1p2/7p/2B3N1/4P2P/PP2KP2/R7 w - h6 0 26",
            "r5k1/pp3p2/3R1pp1/2r5/8/P3PPR1/1P3P1P/4K3 b - - 0 24",
            "R7/1p1kbpr1/1P1p4/3Pp3/2P3b1/3B2P1/6KP/5R2 b - - 0 35",
            "7k/1pp2Rp1/3pB3/p2P3p/8/1P6/PBK4n/6q1 b - - 11 49",
            "r5k1/1pp3pp/2p5/P7/4P3/2R2P1P/R1Pr2P1/6K1 w - - 1 29",
            "8/8/1Bp4p/2p2bp1/2P1Np1k/1P2bP1P/P1r3P1/R6K w - - 10 39",
            "8/8/6nk/3r1p2/P4r2/1P2QN1P/5K2/8 b - - 10 56",
            "8/1Q6/5qk1/7p/2p3b1/P1P5/1P4P1/6K1 b - - 2 40",
            "8/2br1k2/bp1P1p2/p7/3R2P1/P3P3/5P2/3R2K1 b - - 0 33",
            "r2b4/2pk1p1p/3p2p1/1P2p3/P1R1P3/2N3P1/5P1P/5K2 w - - 7 24",
            "3R1b2/5rk1/1p4p1/nP6/4R3/5p1P/P4PP1/6K1 w - - 0 34",
            "1r4k1/p4p1p/2p3pb/8/3p3P/4P1P1/PP1B1P2/4R1K1 w - - 0 31",
            "r7/5pk1/p2Bp3/4P2p/PP2P1p1/4KbP1/5P1P/2R5 w - - 4 60",
            "2R5/1p1r1k1p/p3bp2/4p1p1/P3P3/4B1KP/1P3PP1/8 b - - 1 28",
            "8/2r3pk/PR3p1p/2pR4/2r5/7P/5PPK/8 b - - 1 42",
            "8/2k1r1p1/1p3p2/p2p1Pn1/P2N2Pp/1PPR3P/5K2/8 b - - 48 109",
            "b2r3r/4k3/3bp3/pB3p2/P3P3/1P1N1P2/K2R4/3R4 w - f6 0 33",
            "2rr1k2/pp3p1p/4p3/2b4N/8/1P4P1/1P2PP1P/R1R3K1 b - - 4 21",
            "5rk1/4p3/R2n3b/3P2pp/P4P2/1P5r/5KN1/2NR4 w - - 0 42",
            "3r2k1/6p1/7p/8/PP3R1P/5QPK/5P2/1q6 w - - 3 45",
            "5n2/6bk/4p1pp/1p3p2/3PpP2/rBQ1P3/6PP/B5K1 b - - 1 39",
            "4kb2/1pr2pn1/nB4p1/PR2P2p/5P2/2P3P1/1PK4P/5R2 b - - 8 33",
            "5k1r/1pR2p1p/1p2pb2/8/4p3/4B1P1/rP2PP1P/1R4K1 w - - 10 23",
            "4r3/5pk1/1p5p/p1b2Bp1/4Pr2/3R1P1P/PP1R1PK1/8 w - - 8 35",
            "1r3k2/p2R1bp1/1p3p2/2p2P2/P4P2/1P1RK2B/5P1P/1r6 b - - 5 33",
            "6k1/2pr1pr1/1p2bQ2/p3P3/6P1/P1PB3P/1PP4K/8 b - - 6 41",
            "r4k1r/pp3pb1/4p1q1/2Pp4/PPbP4/4QNP1/4N2P/2R1K2R b K - 0 26",
            "1k4r1/ppq5/2b1N2r/5PpP/1PPp2B1/P2Pb3/4Q1RP/R6K b - - 2 32",
            "r1b1k3/ppp2p1p/5Br1/q3p3/4p2Q/2P5/P1P1BPPP/2KR3R b q - 0 15",
            "r1r3k1/6bp/b2p2p1/1NqP1p2/P2p2P1/1Q3B2/1P3P1P/R3R1K1 b - g3 0 22",
            "r4rk1/2p1q1pp/2P1b3/1pRpPp1N/pP1P2P1/5P1P/P4QK1/4R3 w - - 4 32",
            "7r/k1br2q1/1pp5/p1p1p1p1/P1P1PpNp/2PP1P1K/2Q3P1/1R1R4 w - - 2 53",
            "4r1k1/2b1qp2/2p2np1/1p5p/3p3P/rR1P1BP1/2QNPPK1/5R2 b - - 1 24",
            "1r1q1rk1/p3ppbp/B5p1/8/1P1Nb3/4P3/P2Q1PPP/3R1RK1 w - - 2 19",
            "5r1k/q4p1p/p2p1Nr1/Pbb1p2N/4P3/5R1P/B2Q2PK/2R5 b - - 6 38",
            "1r4k1/p2bp3/1r1p1p2/q5pP/2pNP1n1/2N5/PPPQ4/KR5R w - - 0 24",
            "1r1q1rk1/1p3pp1/n2p3p/p2P4/3Q4/P3RBPP/1P3P2/R5K1 w - - 0 21",
            "5rk1/1p2bqp1/p7/P1ppPP1r/3B3p/3Q1R2/1P4PP/5RK1 w - - 0 28",
            "2rr1bk1/5p1p/pq3pp1/1p1B4/2P3P1/1P1R3P/P2Q1P2/2R3K1 b - c3 0 28",
            "r6k/pp2b1rp/5p2/4p3/1P1N1pq1/P2Q4/2P2PPP/3RRK2 w - - 2 25",
            "4r1k1/3n1pb1/3p2p1/2pP3n/1p2PP1p/1P1QB2P/qN3NP1/1R4K1 b - - 4 28",
            "4r3/p2q1rnk/p2p1pp1/P2P3p/2P1P2P/B1Q5/P7/K3R1R1 b - - 0 42",
            "4rrk1/1pp2qpp/p1np4/3N4/8/2P2P2/PP1Q2PP/R4RK1 w - - 3 19",
            "r1r4k/5p2/b3pPp1/pq1pB1Pp/5P1P/2P4R/1P3QK1/3R4 b - - 1 36",
            "3r1n1k/pb2q1pp/1p2pp1P/2b5/2B5/P1B1PN2/1PQ2PP1/4K2R b K - 0 18",
            "r2q4/p4pk1/1pp1n1pp/4N3/2P5/2Q1R2P/PP1r1PP1/R5K1 b - - 5 28",
            "r1b1r2k/bp4p1/p1n4p/5pq1/NP1p4/P2N4/1B2P1B1/2RQ1RK1 b - - 3 25",
            "3rr1k1/pp3p2/2n1q2p/3n1bp1/8/P2PB1P1/1P1Q1PBP/2R1NRK1 b - - 2 20",
            "3rk2r/1b1n1p1p/p3pp2/2b5/1p1N2q1/1P1B1N1P/P1PQ2P1/2KR3R b k - 0 22",
            "r1b2r2/1pq2p1k/p2p2pp/P1pPb3/2P1P3/2NB3P/1P1Q1PP1/R4R1K b - - 1 23",
            "r1b2rk1/p3nppp/1p1q4/3pn3/3N4/3B4/PP1QNPPP/R4RK1 w - - 1 14",
            "r3k1r1/pp2ppb1/5n1p/q2p4/3n2PN/2NQ4/PPPB1P2/1K1RR3 b q - 1 19",
            "1rr3k1/p2bppbp/6p1/2qBn3/3N4/5P2/PPP1Q1PP/1KBR3R w - - 1 19",
            "4r2r/1p3pk1/2pbq1p1/p2pNnBb/3P2P1/P4P2/1PPQ3P/4RRK1 w - - 2 25",
            "1r1qr1k1/p2N1pb1/2p3pp/4Pn2/2b5/P4N2/1P3PPP/R1BQR1K1 b - - 0 17",
            "r4rk1/1p3pp1/3p3p/p1p1pqb1/PnP5/2BP2PP/1P3P2/R2QRBK1 w - - 0 21",
            "8/5k2/8/6p1/4K3/6PP/8/8 b - - 0 51",
            "8/1b6/1P6/4k3/2K5/8/8/8 b - - 37 138",
            "8/1k6/4nK2/7P/8/8/8/8 b - - 2 56",
            "8/5k2/8/4KP2/8/4B3/8/8 b - - 12 66",
            "8/8/6K1/6PP/4kp2/8/8/8 b - - 0 74",
            "8/6p1/5k2/7p/5K2/8/5P2/8 w - - 2 51",
            "8/8/8/4K3/8/1b4kp/8/8 w - - 0 89",
            "8/8/8/1pk5/p7/P7/8/2K5 b - - 4 107",
            "4b3/8/8/5k2/7p/8/6K1/8 b - - 3 90",
            "3k4/8/8/4BK2/6P1/8/8/8 w - - 1 82",
            "8/8/3k3p/1Pb4P/8/5K2/8/8 w - - 0 51",
            "8/7p/6p1/8/1K4k1/p7/P6P/8 b - - 1 41",
            "5b2/7k/8/5K1P/8/4B3/8/8 w - - 64 182",
            "8/8/3p3p/4p1p1/2k1P3/4K3/8/8 w - - 2 64",
            "8/8/4k2p/6bP/6P1/8/6K1/8 b - - 57 133",
            "7k/8/6K1/6p1/2B5/7P/7P/8 w - - 19 65",
            "5b2/5P2/5K2/3N4/8/6k1/8/8 w - - 79 100",
            "8/8/8/2K5/5p2/4r3/8/7k b - - 1 129",
            "8/8/8/8/4K3/7b/kp1N4/8 b - - 7 92",
            "1b6/8/P5B1/3K4/8/8/5k2/8 w - - 29 196",
            "6k1/8/6P1/5KP1/8/8/8/8 w - - 4 71",
            "8/8/4k3/8/P3K3/8/6P1/8 b - - 1 65",
            "8/2k5/P7/K7/8/P7/8/8 w - - 3 77",
            "8/8/8/2p5/k7/2K1p3/8/8 w - - 0 65",
            "8/8/8/1p2kPK1/8/8/8/8 b - - 2 68",
            "8/8/7p/8/6k1/4K3/5p2/8 b - - 1 69",
            "8/1p6/3k4/8/8/p2K4/8/8 b - - 0 71",
            "8/4k3/8/8/8/1p1K1p2/8/8 b - - 1 74",
            "8/k7/7K/7P/P7/8/8/8 w - - 1 62",
            "8/5P2/3K4/8/8/7k/P7/8 w - - 0 58",
            "7k/8/6p1/8/8/2n5/3p1K2/8 b - - 1 68",
            "8/2Kn4/8/8/3n4/4k3/8/8 b - - 13 86",
            "6K1/8/8/k7/8/4r3/8/8 b - - 59 129",
            "8/1p6/1N5p/8/3K1k2/8/8/8 w - - 0 58",
            "8/3k4/8/1K6/p4B2/P7/8/8 b - - 58 133",
            "8/1k3p2/5P2/4K3/8/8/P6p/8 b - - 0 46",
            "8/8/8/p7/P1k5/5b2/1K6/8 b - - 7 50",
            "8/5p2/4p3/7K/5k1P/5P2/8/8 w - - 1 48",
            "8/8/4pk2/8/5P1K/8/7B/8 b - - 8 75",
            "8/6k1/8/4p2p/5P2/5P2/3K4/8 b - - 0 56",
            "8/8/8/3K1k2/4p3/8/8/8 w - - 0 76",
            "8/8/8/8/7k/4P3/6K1/8 w - - 0 91",
            "8/7k/8/4P3/6K1/8/8/8 b - - 0 84",
            "8/8/8/1k6/2p5/8/8/2K5 w - - 20 115",
            "4K3/8/4P3/1k6/8/8/8/8 w - - 7 95",
            "8/8/P7/1K5k/8/8/8/8 b - - 0 69",
            "8/8/8/PK6/8/8/8/3k4 w - - 1 77",
            "8/8/4K3/8/5p2/2k5/8/8 b - - 1 119",
            "7k/5p2/8/8/8/5K2/8/8 w - - 4 122",
            "7k/8/8/4p3/8/8/8/1K6 b - - 19 154",
            "4rr2/1bqn1pkp/p5p1/n1pp2P1/7Q/2PPNN1P/1P4B1/q1BR1RK1 w - - 0 24",
            "r4bk1/2qn1p1p/b2p2p1/p2Pp1PN/4P1N1/2P1BQ1P/Pr3P1K/R2q2R1 w - - 0 30",
            "4rr2/1bqn1pkp/p5p1/n1pp2P1/6NQ/2PP1N1P/1P4B1/q1BR1RK1 b - - 1 24",
            "4rr1k/1bqn1p1p/p5p1/n1pp2P1/6NQ/2PP1N1P/1P4B1/q1BR1RK1 w - - 2 25",
            "rnbq1rk1/ppp1ppbp/5np1/8/P2P4/4PNP1/5PBP/qNBQ1RK1 w - - 0 10",
            #endregion
        ];

        public static bool Debug { get; set; } = false;
        public static bool IsRunning { get; private set; } = true;
        public static bool IsPondering { get; private set; } = true;
        public static bool Infinite { get; set; } = false;
        public static int MovesOutOfBook { get; set; } = 0;

        public static Board Board { get; } = new();

        public static Color Color
        {
            get => color;
            set => color = value;
        }
        public static PolyglotEntry[] BookEntries
        {
            get
            {
                if (bookEntries == null)
                {
                    LoadBookEntries();
                }

                return bookEntries ?? Array.Empty<PolyglotEntry>();
            }
        }

        public static HceWeights Weights
        {
            get
            {
                if (weights == null)
                {
                    LoadWeights();
                }

                return weights ?? new HceWeights();
            }
        }

        public static int SearchThreads
        {
            get => threads.ThreadCount;
            set => threads.ThreadCount = value;
        }
        public static Color SideToMove => Board.SideToMove;

        public static void Start()
        {
            Stop();
            IsRunning = true;
        }

        public static void Stop()
        {
            threads.WriteStats();
            threads.Stop();
            threads.Wait();
        }

        public static void Quit()
        {
            Stop();
            IsRunning = false;
        }

        public static void Go(int maxDepth, int maxTime, long maxNodes, bool ponder = false)
        {
            Stop();
            IsPondering = ponder;
            time.Go(maxTime, ponder || Infinite);
            StartSearch(maxDepth, maxNodes);
        }

        public static void Go(int maxTime, int opponentTime, int increment, int movesToGo, int maxDepth, long maxNodes, bool ponder = false)
        {
            Stop();
            IsPondering = ponder;
            time.Go(maxTime, opponentTime, increment, movesToGo, MovesOutOfBook, ponder || Infinite);
            StartSearch(maxDepth, maxNodes);
        }

        public static void ClearHashTable()
        {
            TtTran.Default.Clear();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            threads.ClearEvalCache();
        }

        public static void SetupNewGame()
        {
            Stop();
            ClearHashTable();
            LoadBookEntries();
            MovesOutOfBook = 0;
        }

        public static void ResizeHashTable()
        {
            int sizeMb = UciOptions.Hash;
            if (!BitOps.IsPow2(sizeMb))
            {
                sizeMb = BitOps.GreatestPowerOfTwoLessThan(sizeMb);
                UciOptions.Hash = sizeMb;
            }

            TtTran.Default.Resize(sizeMb);
            threads.ResizeEvalCache();
        }

        public static bool SetupPosition(string fen)
        {
            try
            {
                Stop();
                bool loaded = Board.LoadFenPosition(fen);
                if (loaded)
                {
                    Uci.Default.Debug(@$"New position: {Board.ToFenString()}");
                }

                if (!loaded)
                {
                    Uci.Default.Log("Engine failed to load position.");
                }

                return loaded;
            }
            catch (Exception e)
            {
                Uci.Default.Log(@$"Engine faulted: {e.Message}");
                return false;
            }
        }

        public static void MakeMoves(IEnumerable<string> moves)
        {
            foreach (string s in moves)
            {
                if (Move.TryParseMove(Board, s, out ulong move))
                {
                    if (!Board.MakeMove(move))
                    {
                        throw new InvalidOperationException($"Invalid move passed to engine: '{s}'.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Long algebraic move expected. Bad format '{s}'.");
                }
            }

            Uci.Default.Debug($@"New position: {Board.ToFenString()}");
        }

        public static void Wait(bool preserveSearch = false)
        {
            threads.WriteStats();
            threads.Wait();
        }

        private static void WriteStats()
        {
            threads.WriteStats();
        }

        public static void PonderHit()
        {
            if (!IsRunning)
            {
                return;
            }

            if (IsPondering)
            {
                IsPondering = false;
                time.Infinite = false;
            }
            else
            {
                Stop();
            }
        }

        private static int FindFirstBookMove(ulong hash)
        {
            int low = 0;
            int high = BookEntries.Length - 1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (BookEntries[mid].Key >= hash)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return low;
        }

        public static bool LookupBookMoves(ulong hash, out ReadOnlySpan<PolyglotEntry> bookMoves)
        {
            try
            {
                int first = FindFirstBookMove(hash);
                if (first >= 0 && first < BookEntries.Length - 1 && BookEntries[first].Key == hash)
                {
                    int last = first;
                    while (++last < BookEntries.Length && BookEntries[last].Key == hash)
                    { }

                    bookMoves = new ReadOnlySpan<PolyglotEntry>(BookEntries, first, last - first);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Util.TraceError(ex.Message);
                throw;
            }

            bookMoves = new ReadOnlySpan<PolyglotEntry>();
            return false;
        }

        public static void LoadBookEntries()
        {
            try
            {
                if (!UciOptions.OwnBook)
                {
                    bookEntries = null;
                    return;
                }

                if (bookEntries != null)
                {
                    // the book has already been loaded
                    return;
                }

                string? exeFullName = Environment.ProcessPath;
                string? dirFullName = Path.GetDirectoryName(exeFullName);
                string? bookPath = (exeFullName != null && dirFullName != null) ? Path.Combine(dirFullName, "Pedantic.bin") : null;

                if (bookPath != null && File.Exists(bookPath))
                {
                    FileStream fs = new(bookPath, FileMode.Open, FileAccess.Read);
                    using BigEndianBinaryReader reader = new(fs);

                    List<PolyglotEntry> entries = new();
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        PolyglotEntry entry = new()
                        {
                            Key = reader.ReadUInt64(),
                            Move = reader.ReadUInt16(),
                            Weight = reader.ReadUInt16(),
                            Learn = reader.ReadUInt32()
                        };

                        entries.Add(entry);
                    }
                    bookEntries = entries.ToArray();
                }
                else
                {
                    bookEntries = Array.Empty<PolyglotEntry>();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void LoadWeights()
        {
            try
            {
                string? exeFullName = Environment.ProcessPath;
                string? dirFullName = Path.GetDirectoryName(exeFullName);
                string? weightsPath = (exeFullName != null && dirFullName != null) ?
                    Path.Combine(dirFullName, "Pedantic.hce") : null;

                if (weightsPath != null && File.Exists(weightsPath))
                {
                    weights = new HceWeights(weightsPath);
                    Evaluation.Weights = weights;
                }
                else
                {
                    weights = new HceWeights();
                    if (weightsPath != null)
                    {
                        weights.Save(weightsPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public static string GetBookMove()
        {
            string move = "0000";
            if (!UciOptions.OwnBook)
            {
                return move;
            }

            if (!LookupBookMoves(Board.Hash, out var bookMoves))
            {
                return move;
            }

            int total = 0;
            foreach (PolyglotEntry entry in bookMoves)
            {
                total += entry.Weight;
            }

            int pick = Random.Shared.Next(total + 1);
            foreach (PolyglotEntry entry in bookMoves)
            {
                if (pick > entry.Weight)
                {
                    pick -= entry.Weight;
                }
                else
                {
                    int toFile = BitOps.BitFieldExtract(entry.Move, 0, 3);
                    int toRank = BitOps.BitFieldExtract(entry.Move, 3, 3);
                    int fromFile = BitOps.BitFieldExtract(entry.Move, 6, 3);
                    int fromRank = BitOps.BitFieldExtract(entry.Move, 9, 3);
                    int pc = BitOps.BitFieldExtract(entry.Move, 12, 3);

                    int from = Index.ToIndex(fromFile, fromRank);
                    int to = Index.ToIndex(toFile, toRank);
                    Piece promote = pc == 0 ? Piece.None : (Piece)(pc + 1);

                    if (Index.GetFile(from) == 4 && Board.PieceBoard[from].Piece == Piece.King && Board.PieceBoard[to].Piece == Piece.Rook && promote == Piece.None)
                    {
                        foreach (Board.CastlingRookMove rookMove in Board.CastlingRookMoves)
                        {
                            if (rookMove.KingFrom != from || rookMove.RookFrom != to ||
                                (Board.Castling & rookMove.CastlingMask) == 0)
                            {
                                continue;
                            }

                            to = rookMove.KingTo;
                            break;
                        }
                    }
                    move = $@"{Index.ToString(from)}{Index.ToString(to)}{Conversion.PieceToString(promote)}";
                    break;
                }
            }

            return move;
        }

        private static bool ProbeRootTb(Board board, out ulong move, out TbGameResult gameResult)
        {
            move = 0;
            gameResult = TbGameResult.Draw;
            if (UciOptions.SyzygyProbeRoot && Syzygy.IsInitialized && BitOps.PopCount(board.All) <= Syzygy.TbLargest)
            {
                MoveList moveList = new();
                board.GenerateMoves(moveList);

                TbResult result = Syzygy.ProbeRoot(board.Units(Color.White), board.Units(Color.Black), 
                    board.Pieces(Color.White, Piece.King)   | board.Pieces(Color.Black, Piece.King),
                    board.Pieces(Color.White, Piece.Queen)  | board.Pieces(Color.Black, Piece.Queen),
                    board.Pieces(Color.White, Piece.Rook)   | board.Pieces(Color.Black, Piece.Rook),
                    board.Pieces(Color.White, Piece.Bishop) | board.Pieces(Color.Black, Piece.Bishop),
                    board.Pieces(Color.White, Piece.Knight) | board.Pieces(Color.Black, Piece.Knight),
                    board.Pieces(Color.White, Piece.Pawn)   | board.Pieces(Color.Black, Piece.Pawn),
                    (uint)board.HalfMoveClock, (uint)board.Castling, 
                    (uint)(board.EnPassantValidated != Index.NONE ? board.EnPassantValidated : 0), 
                    board.SideToMove == Color.White, null);

                gameResult = result.Wdl;
                int from = (int)result.From;
                int to = (int)result.To;
                uint tbPromotes = result.Promotes;
                Piece promote = (Piece)(5 - tbPromotes);
                promote = promote == Piece.King ? Piece.None : promote;

                for (int n = 0; n < moveList.Count; n++)
                {
                    move = moveList[n];
                    if (Move.GetFrom(move) == from && Move.GetTo(move) == to && Move.GetPromote(move) == promote)
                    {
                        if (board.MakeMove(move))
                        {
                            board.UnmakeMove();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void Bench(int depth, bool extend)
        {
            long totalNodes = 0;
            double totalTime = 0;

            RunBenchFens(depth, benchFens, ref totalNodes, ref totalTime);

            if (extend)
            {
                RunBenchFens(depth, bench2Fens, ref totalNodes, ref totalTime);
            }
            double nps = totalNodes / totalTime;
            Uci.Default.Log($"depth {depth} time {totalTime:F4} nodes {totalNodes} nps {nps:F4}");
        }

        private static void RunBenchFens(int depth, string[] fens, ref long totalNodes, ref double totalTime)
        {
            foreach (string fen in fens)
            {
                SetupNewGame();
                SetupPosition(fen);
                Go(depth, int.MaxValue, long.MaxValue, false);
                Wait(true);
                totalNodes += threads.TotalNodes;
                totalTime += threads.TotalTime;
            }
        }

        private static void StartSearch(int maxDepth, long maxNodes)
        {
            string move = GetBookMove();
            if (move != "0000")
            {
                if (Move.TryParseMove(Board, move, out ulong _))
                {
                    Uci.Default.BestMove(move);
                    return;
                }
            }

            if (ProbeRootTb(Board, out ulong mv, out TbGameResult gameResult))
            {
                Board clone = Board.Clone();
                ulong[] pv = new ulong[10];
                int pvInsert = 0;
                for (int ply = 0; ply < pv.Length; ply++)
                {
                    pv[pvInsert++] = mv;
                    clone.MakeMove(mv);
                    if (!ProbeRootTb(clone, out mv, out _))
                    {
                        break;
                    }
                }
                if (pvInsert < pv.Length)
                {
                    Array.Resize(ref pv, pvInsert);
                }
                int score = gameResult switch
                {
                    TbGameResult.Loss       => -Constants.CHECKMATE_SCORE,
                    TbGameResult.BlessedLoss=> -Constants.CHECKMATE_SCORE,
                    TbGameResult.Draw       => 0,
                    TbGameResult.CursedWin  => Constants.CHECKMATE_SCORE,
                    TbGameResult.Win        => Constants.CHECKMATE_SCORE,
                    _ => 0
                };
                Uci.Default.Info(1, 1, score, pvInsert, 0, pv, TtTran.Default.Usage, pvInsert);
                Uci.Default.BestMove(pv[0], null);
                return;
            }

            if (UciOptions.AnalyseMode)
            {
                ClearHashTable();
            }

            ++MovesOutOfBook;
            threads.Search(time, Board, maxDepth, maxNodes);
            IsRunning = true;
        }
    }
}
