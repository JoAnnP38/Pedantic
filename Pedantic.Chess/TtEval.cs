using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class TtEval
    {
        public enum TtFlag : byte
        {
            Exact,
            LowerBound,
            UpperBound
        }

        public const int DEFAULT_SIZE_MB = 45;
        public const int MAX_SIZE_MB = 2047;
        public const int ITEM_SIZE = 16;
        public const int MB_SIZE = 1024 * 1024;
        public const int HISTORY_DEPTH = 255;

        public struct TtEvalItem
        {
            private ulong hash;
            private ulong data;

            public TtEvalItem(ulong hash, short score, byte depth, TtFlag ttFlag, ulong bestMove)
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

            public static void SetValue(ref TtEvalItem item, ulong hash, short score, byte depth, TtFlag flag,
                ulong bestMove)
            {
                item.data = (Move.ClearScore(bestMove)) |
                            (((ulong)score & 0x0fffful) << 24) |
                            (((ulong)flag & 0x03ul) << 40) |
                            ((depth & 0x0fful) << 42);
                item.hash = hash ^ item.data;
            }
        }


        private static TtEvalItem[] table;
        private static int capacity;

        static TtEval()
        {
            capacity = (DEFAULT_SIZE_MB * MB_SIZE) / ITEM_SIZE;
            table = new TtEvalItem[capacity];
        }

        public static bool TryLookup(ulong hash, out TtEvalItem item)
        {
            int index = GetIndex(hash);
            item = table[index];
            return item.IsValid(hash);
        }

        public static bool FindItem(ulong hash, out int index)
        {
            index = GetIndex(hash);
            return table[index].IsValid(hash);
        }

        public static void Add(ulong hash, short depth, int ply, short alpha, short beta, short score, ulong move)
        {
            int index = GetIndex(hash);
            ref TtEvalItem item = ref table[index];

            if (item.IsValid(hash))
            {
                move = move != 0 ? move : item.BestMove;
            }

            if (Evaluation.IsCheckmate(score))
            {
                if (score < 0)
                {
                    score -= (short)ply;
                }
                else
                {
                    score += (short)ply;
                }
            }

            byte itemDepth = (byte)Math.Max((short)0, depth);
            TtFlag flag = TtFlag.Exact;

            if (score >= beta)
            {
                flag = TtFlag.UpperBound;
                score = beta;
            }
            else if (score <= alpha)
            {
                flag = TtFlag.LowerBound;
                score = alpha;
            }

            TtEvalItem.SetValue(ref item, hash, score, itemDepth, flag, move);
        }

        public static void Clear()
        {
            Array.Clear(table);
        }

        public static void Resize(int sizeMb)
        {
            // resizing also clears the hash table. No attempt to rehash.
            capacity = (Math.Min(sizeMb, MAX_SIZE_MB) * MB_SIZE) / ITEM_SIZE;
            table = new TtEvalItem[capacity];
        }

        public static int Capacity => capacity;

        public static bool TryGetBestMove(ulong hash, out ulong bestMove)
        {
            bestMove = 0ul;
            if (TryLookup(hash, out TtEvalItem item))
            {
                bestMove = item.BestMove != 0ul ? item.BestMove : 0ul;
            }

            return bestMove != 0ul;
        }

        public static bool TryGetScore(ulong hash, short depth, int ply, short alpha, short beta, out short score)
        {
            score = 0;
            int index = GetIndex(hash);

            ref TtEvalItem item = ref table[index];
            if (!item.IsValid(hash) || item.Depth < depth)
            {
                return false;
            }

            score = item.Score;

            if (Evaluation.IsCheckmate(score))
            {
                score -= (short)(Math.Sign(score) * ply);
            }

            if (item.Flag == TtFlag.Exact)
            {
                return true;
            }

            if (item.Flag == TtFlag.LowerBound && score <= alpha)
            {
                return true;
            }

            if (item.Flag == TtFlag.UpperBound && score >= beta)
            {
                return true;
            }

            return false;
        }

        private static int GetIndex(ulong hash)
        {
            return (int)(hash % (ulong)capacity);
        }
    }
}
