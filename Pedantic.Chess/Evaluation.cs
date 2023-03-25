using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Pedantic.Collections;
using Pedantic.Genetics;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class Evaluation
    {
        static Evaluation()
        {
            wt = new (LoadWeights());
        }

        public Evaluation(bool adjustMaterial = true, bool random = false)
        {
            this.adjustMaterial = adjustMaterial;
            this.random = random;
        }

        public short Compute(Board board)
        {
            if (TtEval.TryGetScore(board.Hash, out short score))
            {
                return score;
            }

            ClearScores();

            totalPawns = BitOps.PopCount(board.Pieces(Color.White, Piece.Pawn) | board.Pieces(Color.Black, Piece.Pawn));
            totalMaterial = board.Material(Color.White) + board.Material(Color.Black);
            kingIndex[0] = BitOps.TzCount(board.Pieces(Color.White, Piece.King));
            kingIndex[1] = BitOps.TzCount(board.Pieces(Color.Black, Piece.King));
            GamePhase phase = GetGamePhase(board, out int opWt, out int egWt);

            bool pawnsCalculated = TtPawnEval.TryLookup(board.PawnHash, out TtPawnEval.TtPawnItem item);

            for (Color color = Color.White; color <= Color.Black; color++)
            {
                int c = (int)color;
                ComputeKingAttacks(color, board);
                ComputeMisc(color, board);
                if (pawnsCalculated)
                {
                    opScore[c] += item.GetOpeningScore(color);
                    egScore[c] += item.GetEndGameScore(color);
                }
                else if (totalPawns > 0)
                {
                    ComputePawns(color, board);
                    opScore[c] += opPawnScore[c];
                    egScore[c] += egPawnScore[c];
                }

            opScore[c] += (short)(AdjustMaterial(board.OpeningMaterial[c], adjust[c]) +
                                    board.OpeningPieceSquare[c]);
            egScore[c] += (short)(AdjustMaterial(board.EndGameMaterial[c], adjust[c]) +
                                    board.EndGamePieceSquare[c]);
            }

            if (!pawnsCalculated)
            {
                TtPawnEval.Add(board.PawnHash, opPawnScore, egPawnScore);
            }

            score = (short)((((opScore[0] - opScore[1]) * opWt) >> 7) +
                            (((egScore[0] - egScore[1]) * egWt) >> 7));

            score = board.SideToMove == Color.White ? score : (short)-score;

            if (random)
            {
                score += (short)Random.Shared.Next(-8, 9);
            }
            TtEval.Add(board.Hash, score);
            return score;
        }

        public short AdjustMaterial(short material, int adjustWt)
        {
            return adjustMaterial ? (short)((material * adjustWt) / 10) : material;
        }

        public void ComputeKingAttacks(Color color, Board board)
        {
            short[] mobility = new short[Constants.MAX_PIECES];
            short[] kingAttacks = new short[3];
            int c = (int)color;
            board.GetPieceMobility(color, mobility, kingAttacks);
            for (Piece piece = Piece.Knight; piece <= Piece.Queen; piece++)
            {
                opScore[c] += (short)(mobility[(int)piece] * wt.OpeningPieceMobility(piece));
                egScore[c] += (short)(mobility[(int)piece] * wt.EndGamePieceMobility(piece));
            }

            for (int d = 0; d < 3; d++)
            {
                opScore[c] += (short)(kingAttacks[d] * wt.OpeningKingAttack(d));
                egScore[c] += (short)(kingAttacks[d] * wt.EndGameKingAttack(d));
            }
        }

        public void ComputePawns(Color color, Board board)
        {
            int c = (int)color;
            int o = c ^ 1;
            Color other = (Color)o;
            
            ulong pawns = board.Pieces(color, Piece.Pawn);
            ulong otherPawns = board.Pieces(other, Piece.Pawn);

            for (ulong p = pawns; p != 0; p = BitOps.ResetLsb(p))
            {
                int sq = BitOps.TzCount(p);
                int normalSq = Index.NormalizedIndex[c][sq];

                if ((otherPawns & PassedPawnMasks[c][sq]) == 0)
                {
                    opPawnScore[c] += wt.OpeningPassedPawn;
                    egPawnScore[c] += wt.EndGamePassedPawn;

                    Ray ray = Board.Vectors[sq];
                    ulong bb;
                    if (color == Color.White)
                    {
                        bb = BitOps.AndNot(ray.South, Board.RevVectors[BitOps.LzCount(ray.South & board.All)].South);
                    }
                    else
                    {
                        bb = BitOps.AndNot(ray.North, Board.Vectors[BitOps.TzCount(ray.North & board.All)].North);
                    }
                    if ((bb & board.Pieces(color, Piece.Rook)) != 0)
                    {
                        opScore[c] += wt.OpeningRookBehindPassedPawn;
                        egScore[c] += wt.EndGameRookBehindPassedPawn;
                    }
                }

                if ((pawns & IsolatedPawnMasks[sq]) == 0)
                {
                    opPawnScore[c] += wt.OpeningIsolatedPawn;
                    egPawnScore[c] += wt.EndGameIsolatedPawn;
                }

                if ((pawns & BackwardPawnMasks[c][sq]) == 0)
                {
                    opPawnScore[c] += wt.OpeningBackwardPawn;
                    egPawnScore[c] += wt.EndGameBackwardPawn;
                }

                if ((pawns & AdjacentPawnMasks[sq]) != 0)
                {
                    opPawnScore[c] += wt.OpeningConnectedPawn;
                    egPawnScore[c] += wt.EndGameConnectedPawn;
                }
            }

            for (int file = 0; file < Constants.MAX_COORDS; file++)
            {
                short count = (short)BitOps.PopCount(pawns & Board.MaskFiles[file]);
                if (count > 1)
                {
                    opPawnScore[c] += (short)((count - 1) * wt.OpeningDoubledPawn);
                    egPawnScore[c] += (short)((count - 1) * wt.EndGameDoubledPawn);
                }
            }
        }

        public void ComputeMisc(Color color, Board board)
        {
            int c = (int)color;
            Color other = (Color)(c ^ 1);
            ulong pawns = board.Pieces(color, Piece.Pawn);
            ulong otherPawns = board.Pieces(other, Piece.Pawn);

            if (BitOps.PopCount(board.Pieces(color, Piece.Bishop)) >= 2)
            {
                opScore[c] += wt.OpeningBishopPair;
                egScore[c] += wt.EndGameBishopPair;
            }

            ulong knights = board.Pieces(color, Piece.Knight);
            ulong bishops = board.Pieces(color, Piece.Bishop);

            for (ulong bb = knights | bishops; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                int normalRank = Index.GetRank(Index.NormalizedIndex[c][sq]);
                if (normalRank > 3 && (Board.PawnDefends(color, sq) & pawns) != 0)
                {
                    Piece pc = board.PieceBoard[sq].Piece;
                    if (pc == Piece.Knight)
                    {
                        opScore[c] += wt.OpeningKnightOutpost;
                        egScore[c] += wt.EndGameKnightOutpost;
                    }
                    else
                    {
                        opScore[c] += wt.OpeningBishopOutpost;
                        egScore[c] += wt.EndGameKnightOutpost;
                    }
                }
            }

            int ki = kingIndex[c];
            for (int d = 0; d < 3; d++)
            {
                opScore[c] += (short)(BitOps.PopCount(pawns & KingProximity[d][ki]) * wt.OpeningPawnShield(d));
                egScore[c] += (short)(BitOps.PopCount(pawns & KingProximity[d][ki]) * wt.EndGamePawnShield(d));
            }

            ulong allPawns = pawns | otherPawns;
            ulong rooks = board.Pieces(color, Piece.Rook);

            for (ulong bb = rooks; bb != 0; bb = BitOps.ResetLsb(bb))
            {
                int sq = BitOps.TzCount(bb);
                ulong mask = Board.MaskFiles[sq];
                ulong potentials = mask & rooks;

                if ((mask & allPawns) == 0)
                {
                    opScore[c] += wt.OpeningRookOnOpenFile;
                    egScore[c] += wt.EndGameRookOnOpenFile;

                    
                    if (BitOps.PopCount(potentials) > 1 && IsDoubled(board, potentials))
                    {
                        opScore[c] += wt.OpeningDoubledRooks;
                        egScore[c] += wt.EndGameDoubledRooks;
                    }
                }

                if ((mask & pawns) == 0 && BitOps.PopCount(mask & otherPawns) == 1)
                {
                    opScore[c] += wt.OpeningRookOnHalfOpenFile;
                    egScore[c] += wt.EndGameRookOnHalfOpenFile;

                    if (BitOps.PopCount(potentials) > 1 && IsDoubled(board, potentials))
                    {
                        opScore[c] += wt.OpeningDoubledRooks;
                        egScore[c] += wt.EndGameDoubledRooks;
                    }
                }
            }
        }

        public void CalcMaterialAdjustment(Board board)
        {
            int materialWhite = board.Material(Color.White);
            int materialBlack = board.Material(Color.Black);

            if (materialWhite > 0 && materialBlack > 0 && Math.Abs(materialWhite - materialBlack) >= 200)
            {
                adjust[0] = Math.Max(Math.Min((materialBlack * 10) / materialWhite, 10), 8);
                adjust[1] = Math.Max(Math.Min((materialWhite * 10) / materialBlack, 10), 8);
            }
            else
            {
                adjust[0] = 10;
                adjust[1] = 10;
            }
        }

        public GamePhase GetGamePhase(Board board, out int opWt, out int egWt)
        {
            int totalMaterial = board.Material(Color.White) + board.Material(Color.Black);
            GamePhase phase = GamePhase.Opening;
            opWt = 128;
            egWt = 0;

            if (totalMaterial < wt.EndGamePhaseMaterial)
            {
                phase = totalPawns == 0 ? GamePhase.EndGameMopup : GamePhase.EndGame;
                opWt = 0;
                egWt = 128;
            }
            else if (totalMaterial < wt.OpeningPhaseMaterial && totalMaterial >= wt.EndGamePhaseMaterial)
            {
                phase = GamePhase.MidGame;
                int rngMaterial = wt.OpeningPhaseMaterial - wt.EndGamePhaseMaterial;
                int curMaterial = totalMaterial - wt.EndGamePhaseMaterial;
                opWt = (curMaterial * 128) / rngMaterial;
                egWt = 128 - opWt;
            }

            return phase;
        }

        public void ClearScores()
        {
            Array.Clear(opScore);
            Array.Clear(egScore);
            Array.Clear(opPawnScore);
            Array.Clear(egPawnScore);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCheckmate(int score)
        {
            int absScore = Math.Abs(score);
            return absScore is >= Constants.CHECKMATE_SCORE - Constants.MAX_PLY and <= Constants.CHECKMATE_SCORE;
        }

        public static ChessWeights LoadWeights(string? id = null)
        {
            ChessWeights? w;

            using var rep = new GeneticsRepository();
            w = (string.IsNullOrEmpty(id)
                ? rep.Weights.FindOne(w => w.IsActive && w.IsImmortal)
                : rep.Weights.FindOne(w => w.Id == new ObjectId(id))) ?? ChessWeights.CreateParagon();

            wt = new EvalWeights(w);
            return w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short CanonicalPieceValues(Piece piece)
        {
            return canonicalPieceValues[(int)piece];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short OpeningPieceValues(Piece piece)
        {
            return wt.OpeningPieceValues(piece);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short EndGamePieceValues(Piece piece)
        {
            return wt.EndGamePieceValues(piece);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short OpeningPieceSquareTable(Piece piece, int square)
        {
            return wt.OpeningPieceSquareTable(piece, square);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short EndGamePieceSquareTable(Piece piece, int square)
        {
            return wt.EndGamePieceSquareTable(piece, square);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDoubled(Board bd, ulong piecesOnFile)
        {
            int sq1 = BitOps.TzCount(piecesOnFile);
            piecesOnFile = BitOps.ResetLsb(piecesOnFile);
            int sq2 = BitOps.TzCount(piecesOnFile);
            return ((Board.Between[sq1, sq2] & bd.All) == 0);
        }

        public static short OpeningPhaseMaterial => wt.OpeningPhaseMaterial;
        public static short EndGamePhaseMaterial => wt.EndGamePhaseMaterial;

        public static short[] Weights => wt.Weights;
        public static EvalWeights EvalWts => wt;

        private readonly bool adjustMaterial = default;
        private readonly bool random = false;
        private int totalPawns = default;
        private int totalMaterial = default;
        private readonly int[] adjust = { 10, 10 };
        private readonly int[] kingIndex = new int[2];
        private readonly short[] opScore = { 0, 0 };
        private readonly short[] egScore = { 0, 0 };
        private readonly short[] opPawnScore = { 0, 0 };
        private readonly short[] egPawnScore = { 0, 0 };

        private static EvalWeights wt;
        private static readonly short[] canonicalPieceValues = { 100, 300, 325, 500, 900, 0 };

        public static readonly ulong[][] PassedPawnMasks =
{
            #region PassedPawnMasks data
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0003030303030000ul, 0x0007070707070000ul, 0x000E0E0E0E0E0000ul, 0x001C1C1C1C1C0000ul,
                0x0038383838380000ul, 0x0070707070700000ul, 0x00E0E0E0E0E00000ul, 0x00C0C0C0C0C00000ul,
                0x0003030303000000ul, 0x0007070707000000ul, 0x000E0E0E0E000000ul, 0x001C1C1C1C000000ul,
                0x0038383838000000ul, 0x0070707070000000ul, 0x00E0E0E0E0000000ul, 0x00C0C0C0C0000000ul,
                0x0003030300000000ul, 0x0007070700000000ul, 0x000E0E0E00000000ul, 0x001C1C1C00000000ul,
                0x0038383800000000ul, 0x0070707000000000ul, 0x00E0E0E000000000ul, 0x00C0C0C000000000ul,
                0x0003030000000000ul, 0x0007070000000000ul, 0x000E0E0000000000ul, 0x001C1C0000000000ul,
                0x0038380000000000ul, 0x0070700000000000ul, 0x00E0E00000000000ul, 0x00C0C00000000000ul,
                0x0003000000000000ul, 0x0007000000000000ul, 0x000E000000000000ul, 0x001C000000000000ul,
                0x0038000000000000ul, 0x0070000000000000ul, 0x00E0000000000000ul, 0x00C0000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            },
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000300ul, 0x0000000000000700ul, 0x0000000000000E00ul, 0x0000000000001C00ul,
                0x0000000000003800ul, 0x0000000000007000ul, 0x000000000000E000ul, 0x000000000000C000ul,
                0x0000000000030300ul, 0x0000000000070700ul, 0x00000000000E0E00ul, 0x00000000001C1C00ul,
                0x0000000000383800ul, 0x0000000000707000ul, 0x0000000000E0E000ul, 0x0000000000C0C000ul,
                0x0000000003030300ul, 0x0000000007070700ul, 0x000000000E0E0E00ul, 0x000000001C1C1C00ul,
                0x0000000038383800ul, 0x0000000070707000ul, 0x00000000E0E0E000ul, 0x00000000C0C0C000ul,
                0x0000000303030300ul, 0x0000000707070700ul, 0x0000000E0E0E0E00ul, 0x0000001C1C1C1C00ul,
                0x0000003838383800ul, 0x0000007070707000ul, 0x000000E0E0E0E000ul, 0x000000C0C0C0C000ul,
                0x0000030303030300ul, 0x0000070707070700ul, 0x00000E0E0E0E0E00ul, 0x00001C1C1C1C1C00ul,
                0x0000383838383800ul, 0x0000707070707000ul, 0x0000E0E0E0E0E000ul, 0x0000C0C0C0C0C000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            }
            #endregion PassedPawnMasks data
        };

        public static readonly ulong[] IsolatedPawnMasks =
        {
            #region IsolatedPawnMasks data
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0002020202020200ul, 0x0005050505050500ul, 0x000A0A0A0A0A0A00ul, 0x0014141414141400ul,
            0x0028282828282800ul, 0x0050505050505000ul, 0x00A0A0A0A0A0A000ul, 0x0040404040404000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            #endregion IsolatedPawnMasks data
        };

        public static readonly ulong[][] KingProximity =
        {
            #region kingProximity data
            new[]
            {
                0x0000000000000302ul, 0x0000000000000705ul, 0x0000000000000E0Aul, 0x0000000000001C14ul,
                0x0000000000003828ul, 0x0000000000007050ul, 0x000000000000E0A0ul, 0x000000000000C040ul,
                0x0000000000030203ul, 0x0000000000070507ul, 0x00000000000E0A0Eul, 0x00000000001C141Cul,
                0x0000000000382838ul, 0x0000000000705070ul, 0x0000000000E0A0E0ul, 0x0000000000C040C0ul,
                0x0000000003020300ul, 0x0000000007050700ul, 0x000000000E0A0E00ul, 0x000000001C141C00ul,
                0x0000000038283800ul, 0x0000000070507000ul, 0x00000000E0A0E000ul, 0x00000000C040C000ul,
                0x0000000302030000ul, 0x0000000705070000ul, 0x0000000E0A0E0000ul, 0x0000001C141C0000ul,
                0x0000003828380000ul, 0x0000007050700000ul, 0x000000E0A0E00000ul, 0x000000C040C00000ul,
                0x0000030203000000ul, 0x0000070507000000ul, 0x00000E0A0E000000ul, 0x00001C141C000000ul,
                0x0000382838000000ul, 0x0000705070000000ul, 0x0000E0A0E0000000ul, 0x0000C040C0000000ul,
                0x0003020300000000ul, 0x0007050700000000ul, 0x000E0A0E00000000ul, 0x001C141C00000000ul,
                0x0038283800000000ul, 0x0070507000000000ul, 0x00E0A0E000000000ul, 0x00C040C000000000ul,
                0x0302030000000000ul, 0x0705070000000000ul, 0x0E0A0E0000000000ul, 0x1C141C0000000000ul,
                0x3828380000000000ul, 0x7050700000000000ul, 0xE0A0E00000000000ul, 0xC040C00000000000ul,
                0x0203000000000000ul, 0x0507000000000000ul, 0x0A0E000000000000ul, 0x141C000000000000ul,
                0x2838000000000000ul, 0x5070000000000000ul, 0xA0E0000000000000ul, 0x40C0000000000000ul
            },
            new[]
            {
                0x0000000000070404ul, 0x00000000000F0808ul, 0x00000000001F1111ul, 0x00000000003E2222ul,
                0x00000000007C4444ul, 0x0000000000F88888ul, 0x0000000000F01010ul, 0x0000000000E02020ul,
                0x0000000007040404ul, 0x000000000F080808ul, 0x000000001F111111ul, 0x000000003E222222ul,
                0x000000007C444444ul, 0x00000000F8888888ul, 0x00000000F0101010ul, 0x00000000E0202020ul,
                0x0000000704040407ul, 0x0000000F0808080Ful, 0x0000001F1111111Ful, 0x0000003E2222223Eul,
                0x0000007C4444447Cul, 0x000000F8888888F8ul, 0x000000F0101010F0ul, 0x000000E0202020E0ul,
                0x0000070404040700ul, 0x00000F0808080F00ul, 0x00001F1111111F00ul, 0x00003E2222223E00ul,
                0x00007C4444447C00ul, 0x0000F8888888F800ul, 0x0000F0101010F000ul, 0x0000E0202020E000ul,
                0x0007040404070000ul, 0x000F0808080F0000ul, 0x001F1111111F0000ul, 0x003E2222223E0000ul,
                0x007C4444447C0000ul, 0x00F8888888F80000ul, 0x00F0101010F00000ul, 0x00E0202020E00000ul,
                0x0704040407000000ul, 0x0F0808080F000000ul, 0x1F1111111F000000ul, 0x3E2222223E000000ul,
                0x7C4444447C000000ul, 0xF8888888F8000000ul, 0xF0101010F0000000ul, 0xE0202020E0000000ul,
                0x0404040700000000ul, 0x0808080F00000000ul, 0x1111111F00000000ul, 0x2222223E00000000ul,
                0x4444447C00000000ul, 0x888888F800000000ul, 0x101010F000000000ul, 0x202020E000000000ul,
                0x0404070000000000ul, 0x08080F0000000000ul, 0x11111F0000000000ul, 0x22223E0000000000ul,
                0x44447C0000000000ul, 0x8888F80000000000ul, 0x1010F00000000000ul, 0x2020E00000000000ul
            },
            new[]
            {
                0x000000000F080808ul, 0x000000001F101010ul, 0x000000003F202020ul, 0x000000007F414141ul,
                0x00000000FE828282ul, 0x00000000FC040404ul, 0x00000000F8080808ul, 0x00000000F0101010ul,
                0x0000000F08080808ul, 0x0000001F10101010ul, 0x0000003F20202020ul, 0x0000007F41414141ul,
                0x000000FE82828282ul, 0x000000FC04040404ul, 0x000000F808080808ul, 0x000000F010101010ul,
                0x00000F0808080808ul, 0x00001F1010101010ul, 0x00003F2020202020ul, 0x00007F4141414141ul,
                0x0000FE8282828282ul, 0x0000FC0404040404ul, 0x0000F80808080808ul, 0x0000F01010101010ul,
                0x000F08080808080Ful, 0x001F10101010101Ful, 0x003F20202020203Ful, 0x007F41414141417Ful,
                0x00FE8282828282FEul, 0x00FC0404040404FCul, 0x00F80808080808F8ul, 0x00F01010101010F0ul,
                0x0F08080808080F00ul, 0x1F10101010101F00ul, 0x3F20202020203F00ul, 0x7F41414141417F00ul,
                0xFE8282828282FE00ul, 0xFC0404040404FC00ul, 0xF80808080808F800ul, 0xF01010101010F000ul,
                0x08080808080F0000ul, 0x10101010101F0000ul, 0x20202020203F0000ul, 0x41414141417F0000ul,
                0x8282828282FE0000ul, 0x0404040404FC0000ul, 0x0808080808F80000ul, 0x1010101010F00000ul,
                0x080808080F000000ul, 0x101010101F000000ul, 0x202020203F000000ul, 0x414141417F000000ul,
                0x82828282FE000000ul, 0x04040404FC000000ul, 0x08080808F8000000ul, 0x10101010F0000000ul,
                0x0808080F00000000ul, 0x1010101F00000000ul, 0x2020203F00000000ul, 0x4141417F00000000ul,
                0x828282FE00000000ul, 0x040404FC00000000ul, 0x080808F800000000ul, 0x101010F000000000ul
            }

            #endregion kingProximity data
        };

        public static readonly ulong[][] BackwardPawnMasks =
        {
            #region BackwardPawnMasks data
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000200ul, 0x0000000000000500ul, 0x0000000000000A00ul, 0x0000000000001400ul,
                0x0000000000002800ul, 0x0000000000005000ul, 0x000000000000A000ul, 0x0000000000004000ul,
                0x0000000000020200ul, 0x0000000000050500ul, 0x00000000000A0A00ul, 0x0000000000141400ul,
                0x0000000000282800ul, 0x0000000000505000ul, 0x0000000000A0A000ul, 0x0000000000404000ul,
                0x0000000002020200ul, 0x0000000005050500ul, 0x000000000A0A0A00ul, 0x0000000014141400ul,
                0x0000000028282800ul, 0x0000000050505000ul, 0x00000000A0A0A000ul, 0x0000000040404000ul,
                0x0000000202020200ul, 0x0000000505050500ul, 0x0000000A0A0A0A00ul, 0x0000001414141400ul,
                0x0000002828282800ul, 0x0000005050505000ul, 0x000000A0A0A0A000ul, 0x0000004040404000ul,
                0x0000020202020200ul, 0x0000050505050500ul, 0x00000A0A0A0A0A00ul, 0x0000141414141400ul,
                0x0000282828282800ul, 0x0000505050505000ul, 0x0000A0A0A0A0A000ul, 0x0000404040404000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            },
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0002020202020000ul, 0x0005050505050000ul, 0x000A0A0A0A0A0000ul, 0x0014141414140000ul,
                0x0028282828280000ul, 0x0050505050500000ul, 0x00A0A0A0A0A00000ul, 0x0040404040400000ul,
                0x0002020202000000ul, 0x0005050505000000ul, 0x000A0A0A0A000000ul, 0x0014141414000000ul,
                0x0028282828000000ul, 0x0050505050000000ul, 0x00A0A0A0A0000000ul, 0x0040404040000000ul,
                0x0002020200000000ul, 0x0005050500000000ul, 0x000A0A0A00000000ul, 0x0014141400000000ul,
                0x0028282800000000ul, 0x0050505000000000ul, 0x00A0A0A000000000ul, 0x0040404000000000ul,
                0x0002020000000000ul, 0x0005050000000000ul, 0x000A0A0000000000ul, 0x0014140000000000ul,
                0x0028280000000000ul, 0x0050500000000000ul, 0x00A0A00000000000ul, 0x0040400000000000ul,
                0x0002000000000000ul, 0x0005000000000000ul, 0x000A000000000000ul, 0x0014000000000000ul,
                0x0028000000000000ul, 0x0050000000000000ul, 0x00A0000000000000ul, 0x0040000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            }

            #endregion BackwardPawnMasks data
        };

        public static readonly ulong[] AdjacentPawnMasks =
        {
            #region AdjacentPawnMasks data
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000200ul, 0x0000000000000500ul, 0x0000000000000A00ul, 0x0000000000001400ul,
            0x0000000000002800ul, 0x0000000000005000ul, 0x000000000000A000ul, 0x0000000000004000ul,
            0x0000000000020000ul, 0x0000000000050000ul, 0x00000000000A0000ul, 0x0000000000140000ul,
            0x0000000000280000ul, 0x0000000000500000ul, 0x0000000000A00000ul, 0x0000000000400000ul,
            0x0000000002000000ul, 0x0000000005000000ul, 0x000000000A000000ul, 0x0000000014000000ul,
            0x0000000028000000ul, 0x0000000050000000ul, 0x00000000A0000000ul, 0x0000000040000000ul,
            0x0000000200000000ul, 0x0000000500000000ul, 0x0000000A00000000ul, 0x0000001400000000ul,
            0x0000002800000000ul, 0x0000005000000000ul, 0x000000A000000000ul, 0x0000004000000000ul,
            0x0000020000000000ul, 0x0000050000000000ul, 0x00000A0000000000ul, 0x0000140000000000ul,
            0x0000280000000000ul, 0x0000500000000000ul, 0x0000A00000000000ul, 0x0000400000000000ul,
            0x0002000000000000ul, 0x0005000000000000ul, 0x000A000000000000ul, 0x0014000000000000ul,
            0x0028000000000000ul, 0x0050000000000000ul, 0x00A0000000000000ul, 0x0040000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
            0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            #endregion AdjacentPawnMasks data
        };

        public static readonly int[] PawnOffset = { 8, -8 };

        public static readonly ulong[][] PromoteSquares =
        {
            #region PromoteSquares data
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x7F7F7F7F7F7F7F00ul, 0xFFFFFFFFFFFFFF00ul, 0xFFFFFFFFFFFFFF00ul, 0xFFFFFFFFFFFFFF00ul,
                0xFFFFFFFFFFFFFF00ul, 0xFFFFFFFFFFFFFF00ul, 0xFFFFFFFFFFFFFF00ul, 0xFEFEFEFEFEFEFE00ul,
                0x3F3F3F3F3F3F0000ul, 0x7F7F7F7F7F7F0000ul, 0xFFFFFFFFFFFF0000ul, 0xFFFFFFFFFFFF0000ul,
                0xFFFFFFFFFFFF0000ul, 0xFFFFFFFFFFFF0000ul, 0xFEFEFEFEFEFE0000ul, 0xFCFCFCFCFCFC0000ul,
                0x1F1F1F1F1F000000ul, 0x3F3F3F3F3F000000ul, 0x7F7F7F7F7F000000ul, 0xFFFFFFFFFF000000ul,
                0xFFFFFFFFFF000000ul, 0xFEFEFEFEFE000000ul, 0xFCFCFCFCFC000000ul, 0xF8F8F8F8F8000000ul,
                0x0F0F0F0F00000000ul, 0x1F1F1F1F00000000ul, 0x3F3F3F3F00000000ul, 0x7F7F7F7F00000000ul,
                0xFEFEFEFE00000000ul, 0xFCFCFCFC00000000ul, 0xF8F8F8F800000000ul, 0xF0F0F0F000000000ul,
                0x0707070000000000ul, 0x0F0F0F0000000000ul, 0x1F1F1F0000000000ul, 0x3E3E3E0000000000ul,
                0x7C7C7C0000000000ul, 0xF8F8F80000000000ul, 0xF0F0F00000000000ul, 0xE0E0E00000000000ul,
                0x0303000000000000ul, 0x0707000000000000ul, 0x0E0E000000000000ul, 0x1C1C000000000000ul,
                0x3838000000000000ul, 0x7070000000000000ul, 0xE0E0000000000000ul, 0xC0C0000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            },
            new[]
            {
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000303ul, 0x0000000000000707ul, 0x0000000000000E0Eul, 0x0000000000001C1Cul,
                0x0000000000003838ul, 0x0000000000007070ul, 0x000000000000E0E0ul, 0x000000000000C0C0ul,
                0x0000000000070707ul, 0x00000000000F0F0Ful, 0x00000000001F1F1Ful, 0x00000000003E3E3Eul,
                0x00000000007C7C7Cul, 0x0000000000F8F8F8ul, 0x0000000000F0F0F0ul, 0x0000000000E0E0E0ul,
                0x000000000F0F0F0Ful, 0x000000001F1F1F1Ful, 0x000000003F3F3F3Ful, 0x000000007F7F7F7Ful,
                0x00000000FEFEFEFEul, 0x00000000FCFCFCFCul, 0x00000000F8F8F8F8ul, 0x00000000F0F0F0F0ul,
                0x0000001F1F1F1F1Ful, 0x0000003F3F3F3F3Ful, 0x0000007F7F7F7F7Ful, 0x000000FFFFFFFFFFul,
                0x000000FFFFFFFFFFul, 0x000000FEFEFEFEFEul, 0x000000FCFCFCFCFCul, 0x000000F8F8F8F8F8ul,
                0x00003F3F3F3F3F3Ful, 0x00007F7F7F7F7F7Ful, 0x0000FFFFFFFFFFFFul, 0x0000FFFFFFFFFFFFul,
                0x0000FFFFFFFFFFFFul, 0x0000FFFFFFFFFFFFul, 0x0000FEFEFEFEFEFEul, 0x0000FCFCFCFCFCFCul,
                0x007F7F7F7F7F7F7Ful, 0x00FFFFFFFFFFFFFFul, 0x00FFFFFFFFFFFFFFul, 0x00FFFFFFFFFFFFFFul,
                0x00FFFFFFFFFFFFFFul, 0x00FFFFFFFFFFFFFFul, 0x00FFFFFFFFFFFFFFul, 0x00FEFEFEFEFEFEFEul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul,
                0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul, 0x0000000000000000ul
            }
            #endregion PromoteSquares data
        };
    }
}
