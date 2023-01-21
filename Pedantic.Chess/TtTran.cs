﻿using System.Diagnostics.Tracing;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class TtTran
    {
        public enum TtFlag : byte
        {
            Exact,
            UpperBound,
            LowerBound
        }

        public const int DEFAULT_SIZE_MB = 45;
        public const int MAX_SIZE_MB = 2047;
        public const int ITEM_SIZE = 16;
        public const int MB_SIZE = 1024 * 1024;

        public struct TtTranItem
        {
            private ulong hash;
            private ulong data;

            public TtTranItem(ulong hash, short score, byte depth, TtFlag ttFlag, ulong bestMove)
            {
                data = (bestMove & 0x0fffffful) |
                       (((ulong)score & 0x0fffful) << 24) |
                       (((ulong)ttFlag & 0x03ul) << 40) |
                       ((depth & 0x0fful) << 42);

                this.hash = hash ^ data;
            }

            public ulong Hash => hash ^ data;
            public ulong Data => data;
            public ulong BestMove => (ulong)BitOps.BitFieldExtract(data, 0, 24);
            public short Score => (short)BitOps.BitFieldExtract(data, 24, 16);
            public TtFlag Flag => (TtFlag)BitOps.BitFieldExtract(data, 40, 2);
            public byte Depth => (byte)BitOps.BitFieldExtract(data, 42, 8);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsValid(ulong hash)
            {
                return (this.hash ^ data)  == hash;
            }

            public static void SetValue(ref TtTranItem item, ulong hash, short score, byte depth, TtFlag flag,
                ulong bestMove)
            {
                item.data = (Move.ClearScore(bestMove)) |
                            (((ulong)score & 0x0fffful) << 24) |
                            (((ulong)flag & 0x03ul) << 40) |
                            ((depth & 0x0fful) << 42);
                item.hash = hash ^ item.data;
            }
        }


        private static TtTranItem[] table;
        private static int capacity;

        static TtTran()
        {
            capacity = (DEFAULT_SIZE_MB * MB_SIZE) / ITEM_SIZE;
            table = new TtTranItem[capacity];
        }

        public static void Add(ulong hash, int depth, int ply, int alpha, int beta, int score, ulong move)
        {
            int index = GetStoreIndex(hash);
            ref TtTranItem item = ref table[index];
            ulong bestMove = move;

            if (item.IsValid(hash))
            {
                if (item.Depth > depth)
                {
                    return; // don't replace
                }

                bestMove = bestMove == 0 ? item.BestMove : bestMove;
            }

            if (Evaluation.IsCheckmate(score))
            {
                if (score < 0)
                {
                    score -= ply;
                }
                else
                {
                    score += ply;
                }
            }

            byte itemDepth = (byte)Math.Max(0, depth);
            TtFlag flag = TtFlag.Exact;

            if (score <= alpha)
            {
                flag = TtFlag.UpperBound;
                //score = alpha;
            }
            else if (score >= beta)
            {
                flag = TtFlag.LowerBound;
                //score = beta;
            }

            TtTranItem.SetValue(ref item, hash, (short)score, itemDepth, flag, bestMove);
        }

        public static void Clear()
        {
            Array.Clear(table);
        }

        public static void Resize(int sizeMb)
        {
            // resizing also clears the hash table. No attempt to rehash.
            capacity = (Math.Min(sizeMb, MAX_SIZE_MB) * MB_SIZE) / ITEM_SIZE;
            table = new TtTranItem[capacity];
        }

        public static int Capacity => capacity;

        public static bool TryGetBestMove(ulong hash, out ulong bestMove)
        {
            bestMove = 0ul;
            if (GetLoadIndex(hash, out int index))
            {
                bestMove = table[index].BestMove;
            }

            return bestMove != 0;
            
        }

        public static bool TryGetScore(ulong hash, int depth, int ply, int alpha, int beta, out int score)
        {
            return TryGetScore(hash, depth, ply, ref alpha, ref beta, out score, out ulong _);
        }

        public static bool TryGetScore(ulong hash, int depth, int ply, ref int alpha, ref int beta, out int score,
            out ulong move)
        {
            score = 0;
            move = 0;

            if (GetLoadIndex(hash, out int index))
            {
                ref TtTranItem item = ref table[index];

                if (item.Depth <= depth)
                {
                    return false;
                }

                score = item.Score;
                move = item.BestMove;

                if (Evaluation.IsCheckmate(score))
                {
                    score -= Math.Sign(score) * ply;
                }

                if (item.Flag == TtFlag.Exact)
                {
                    return true;
                }

                if (item.Flag == TtFlag.UpperBound /*&& score <= alpha*/)
                {
                    beta = Math.Min(score, beta);
                }

                if (item.Flag == TtFlag.LowerBound /*&& score >= beta*/)
                {
                    alpha = Math.Max(score, alpha);
                }

                return alpha >= beta;
            }

            return false;
        }

        private static int GetStoreIndex(ulong hash)
        {
            int index = (int)(hash % (ulong)capacity);
            if (!table[index].IsValid(hash))
            {
                return index;
            }

            if (!table[index ^ 1].IsValid(hash))
            {
                return index ^ 1;
            }

            return table[index].Depth < table[index ^ 1].Depth ? index : index ^ 1;
        }

        private static bool GetLoadIndex(ulong hash, out int index)
        {
            index = (int)(hash % (ulong)capacity);
            if (table[index].IsValid(hash))
            {
                if (table[index ^ 1].IsValid(hash))
                {
                    index = table[index].Depth > table[index ^ 1].Depth ? index : index ^ 1;
                }

                return true;
            }

            if (table[index ^ 1].IsValid(hash))
            {
                index ^= 1;
                return true;
            }

            return false;
        }
    }
}
