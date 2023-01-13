using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class TtPawnEval
    {
        public const int DEFAULT_SIZE_MB = 5;
        public const int MAX_SIZE_MB = 2047;
        public const int ITEM_SIZE = 16;
        public const int MB_SIZE = 1024 * 1024;

        public struct TtPawnItem
        {
            private ulong hash;
            private ulong data;
            private short[]? opScores = null;
            private short[]? egScores = null;

            public TtPawnItem(ulong hash, short[] opScores, short[] egScores)
            {
                ulong op1 = (ushort)opScores[(int)Color.White];
                ulong op2 = (ushort)opScores[(int)Color.Black];
                ulong op3 = (ushort)egScores[(int)Color.White];
                ulong op4 = (ushort)egScores[(int)Color.Black];

                data = op1 | (op2 << 16) | (op3 << 32) | (op4 << 48);
                this.hash = hash ^ data;
            }

            public ulong Hash => hash ^ data;
            public ulong Data => data;

            public short GetOpeningScore(Color color)
            {
                byte start = (byte)(16 * (byte)color);
                return (short)BitOps.BitFieldExtract(data, start, 16);
            }

            public short GetEndGameScore(Color color)
            {
                byte start = (byte)(32 + (16 * (int)color));
                return (short)BitOps.BitFieldExtract(data, start, 16);
            }

            public bool IsValid(ulong hash)
            {
                return Hash == hash;
            }

            public static void SetValue(ref TtPawnItem item, ulong hash, Span<short> opScores, Span<short> egScores)
            {

                ulong op1 = (ushort)opScores[(int)Color.White];
                ulong op2 = (ushort)opScores[(int)Color.Black];
                ulong op3 = (ushort)egScores[(int)Color.White];
                ulong op4 = (ushort)egScores[(int)Color.Black];

                item.data = op1 | (op2 << 16) | (op3 << 32) | (op4 << 48);
                item.hash = hash ^ item.data;
            }
        }

        private static TtPawnItem[] table;
        private static int capacity;

        static TtPawnEval()
        {
            capacity = (DEFAULT_SIZE_MB * MB_SIZE) / ITEM_SIZE;
            table = new TtPawnItem[capacity];
        }

        public static bool TryLookup(ulong hash, out TtPawnItem item)
        {
            item = table[GetIndex(hash)];
            return item.IsValid(hash);
        }

        public static void Add(ulong hash, Span<short> opScores, Span<short> egScores)
        {
            int index = GetIndex(hash);
            ref TtPawnItem item = ref table[index];
            TtPawnItem.SetValue(ref item, hash, opScores, egScores);
        }

        public static void Clear()
        {
            Array.Clear(table);
        }

        public static void Resize(int sizeMb)
        {
            // resizing also clears the hash table. No attempt to rehash.
            capacity = (Math.Min(sizeMb, MAX_SIZE_MB) * MB_SIZE) / ITEM_SIZE;
            table = new TtPawnItem[capacity];
        }

        private static int GetIndex(ulong hash)
        {
            return (int)(hash % (ulong)capacity);
        }
    }
}
