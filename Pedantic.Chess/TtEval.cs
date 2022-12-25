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

        public const int DEFAULT_SIZE_MB = 50;
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

            public TtEvalItem(ulong hash, ulong data)
            {
                this.hash = hash;
                this.data = data;
            }

            public ulong Hash => hash ^ data;
            public ulong Data => data;
            public ulong Move => (ulong)BitOps.BitFieldExtract(data, 0, 24);
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
                item.data = (bestMove & 0x0fffffful) |
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

        public static void Add(ulong hash, short depth, short alpha, short beta, short score, ulong move)
        {
            int index = GetIndex(hash);
            byte itemDepth = (byte)Math.Max((short)0, depth);
            TtFlag flag = TtFlag.Exact;

            if (score <= alpha)
            {
                flag = TtFlag.UpperBound;
            }
            else if (score >= beta)
            {
                flag = TtFlag.LowerBound;
            }
            TtEvalItem.SetValue(ref table[index], hash, score, itemDepth, flag, move);
        }

        public static void Clear()
        {
            Array.Clear(table);
        }

        public static void Resize(int sizeMb)
        {
            // resizing also clears the hash table. No attempt to rehash.
            capacity = (sizeMb * MB_SIZE) / ITEM_SIZE;
            table = new TtEvalItem[capacity];
        }

        private static int GetIndex(ulong hash)
        {
            return (int)(hash % (ulong)capacity);
        }

        public static int Capacity => capacity;
    }
}
