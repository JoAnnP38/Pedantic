using System.Diagnostics.Tracing;
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
            TtExact,
            TtAlpha,
            TtBeta
        }

        public const int DEFAULT_SIZE_MB = 45;
        public const int MAX_SIZE_MB = 2047;
        public const int ITEM_SIZE = 16;
        public const int MB_SIZE = 1024 * 1024;
        public const int HISTORY_DEPTH = 255;

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

            public bool IsValid(ulong hash)
            {
                return Hash == hash;
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
            int index = GetIndex(hash);
            ref TtTranItem item = ref table[index];

            if (item.IsValid(hash) && item.Depth > depth)
            {
                return; // don't replace
            }

            ulong bestMove = item.BestMove;
            if (!item.IsValid(hash) || move != 0)
            {
                bestMove = move;
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
            TtFlag flag = TtFlag.TtExact;

            if (score <= alpha)
            {
                flag = TtFlag.TtAlpha;
                score = alpha;
            }
            else if (score >= beta)
            {
                flag = TtFlag.TtBeta;
                score = beta;
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

        public static bool FindSlot(ulong hash, out int index)
        {
            index = (int)(hash % (ulong)capacity);
            if (!table[index].IsValid(hash))
            {
                index ^= 1;
            }

            if (!table[index].IsValid(hash))
            {
                return false;
            }

            return true;
        }
        public static bool TryGetBestMove(ulong hash, out ulong bestMove)
        {
            bestMove = 0ul;
            if (FindSlot(hash, out int index))
            {
                bestMove = table[index].BestMove;
                return bestMove != 0;
            }

            return false;
        }

        public static bool TryGetScore(ulong hash, int depth, int ply, int alpha, int beta, out int score)
        {
            score = 0;

            if (!FindSlot(hash, out int index))
            {
                return false;
            }

            ref TtTranItem item = ref table[index];

            if (item.Depth <= depth)
            {
                return false;
            }

            score = item.Score;

            if (Evaluation.IsCheckmate(score))
            {
                score -= Math.Sign(score) * ply;
            }

            if (item.Flag == TtFlag.TtExact)
            {
                return true;
            }

            if (item.Flag == TtFlag.TtAlpha && score <= alpha)
            {
                score = alpha;
                return true;
            }

            if (item.Flag == TtFlag.TtBeta && score >= beta)
            {
                score = beta;
                return true;
            }

            return false;
        }

        private static int GetIndex(ulong hash)
        {
            int index = (int)(hash % (ulong)capacity);
            ref TtTranItem item0 = ref table[index];
            ref TtTranItem item1 = ref table[index ^ 1];

            if (item0.IsValid(hash))
            {
                return index;
            }

            if (item1.IsValid(hash))
            {
                return index ^ 1;
            }

            return (item0.Depth <= item1.Depth) ? index : index ^ 1;
        }
    }
}
