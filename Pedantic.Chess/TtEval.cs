using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public static class TtEval
    {
        public const int DEFAULT_SIZE_MB = 25;
        public const int MAX_SIZE_MB = 1024;
        public const int ITEM_SIZE = 16;
        public const int MB_SIZE = 1024 * 1024;

        public struct TtEvalItem
        {
            private ulong hash;
            private ulong data;

            public TtEvalItem(ulong hash, short score)
            {
                data = (ulong)score;
                this.hash = hash ^ data;
            }

            public ulong Hash => hash ^ data;
            public short Score => (short)data;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsValid(ulong hash)
            {
                return (this.hash ^ data) == hash;
            }

            public static void SetValue(ref TtEvalItem item, ulong hash, short score)
            {
                item.data = (ulong)score;
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

        public static void Add(ulong hash, short score)
        {
            int index = GetIndex(hash);
            ref TtEvalItem item = ref table[index];
            TtEvalItem.SetValue(ref item, hash, score);

        }

        public static bool TryGetScore(ulong hash, out short score)
        {
            score = 0;
            int index = GetIndex(hash);
            ref TtEvalItem item = ref table[index];
            if (item.IsValid(hash))
            {
                score = item.Score;
                return true;
            }

            return false;
        }

        public static void Clear()
        {
            Array.Clear(table);
        }

        public static void Resize(int sizeMb)
        {
            capacity = (Math.Min(sizeMb, MAX_SIZE_MB) * MB_SIZE) / ITEM_SIZE;
            table = new TtEvalItem[capacity];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(ulong hash)
        {
            return (int)(hash % (ulong)capacity);
        }
    }
}
