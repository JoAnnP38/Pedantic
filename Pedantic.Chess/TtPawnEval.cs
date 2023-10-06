// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="TtPawnEval.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     A secondary transposition table dedicated to the evaluation;
//     however, this one is dedicated purely to pawn structure. 
//     If the pawn structure hasn't changed there is no reason to 
//     recompute its value.
// </summary>
// ***********************************************************************
using Pedantic.Utilities;
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public static class TtPawnEval
    {
        public const int DEFAULT_SIZE_MB = 16;
        public const int MAX_SIZE_MB = 512;
        public const int ITEM_SIZE = 16;
        public const int MB_SIZE = 1024 * 1024;

        public struct TtPawnItem
        {
            private ulong hash;
            private ulong data;

            public TtPawnItem(ulong hash, short[] opScores, short[] egScores)
            {
                ulong op1 = (ushort)opScores[(int)Color.White];
                ulong op2 = (ushort)opScores[(int)Color.Black];
                ulong op3 = (ushort)egScores[(int)Color.White];
                ulong op4 = (ushort)egScores[(int)Color.Black];

                data = op1 | (op2 << 16) | (op3 << 32) | (op4 << 48);
                this.hash = hash ^ data;
            }

            public readonly ulong Hash => hash ^ data;
            public readonly ulong Data => data;

            public readonly short GetOpeningScore(Color color)
            {
                byte start = (byte)(16 * (byte)color);
                return (short)BitOps.BitFieldExtract(data, start, 16);
            }

            public readonly short GetEndGameScore(Color color)
            {
                byte start = (byte)(32 + (16 * (int)color));
                return (short)BitOps.BitFieldExtract(data, start, 16);
            }

            public readonly bool IsValid(ulong hash)
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

            public static void SetValue(ref TtPawnItem item, ulong hash, short[] opScore, short[] egScore)
            {
                ulong op1 = (ushort)opScore[(int)Color.White];
                ulong op2 = (ushort)opScore[(int)Color.Black];
                ulong op3 = (ushort)egScore[(int)Color.White];
                ulong op4 = (ushort)egScore[(int)Color.Black];

                item.data = op1 | (op2 << 16) | (op3 << 32) | (op4 << 48);
                item.hash = hash ^ item.data;
            }
        }

        private static TtPawnItem[] table;
        private static int capacity;
        private static uint mask;

        static TtPawnEval()
        {
            capacity = (DEFAULT_SIZE_MB * MB_SIZE) / ITEM_SIZE;
            table = new TtPawnItem[capacity];
            mask = (uint)(capacity - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public static void Add(ulong hash, short[] opScore, short[] egScore)
        {
            int index = GetIndex(hash);
            ref TtPawnItem item = ref table[index];
            TtPawnItem.SetValue(ref item, hash, opScore, egScore);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear()
        {
            Array.Clear(table);
        }

        public static void Resize(int sizeMb)
        {
            sizeMb = Math.Max(Math.Min(sizeMb, 2048), 2);
            if (!BitOps.IsPow2(sizeMb))
            {
                sizeMb = BitOps.GreatestPowerOfTwoLessThan(sizeMb);
            }
            // resizing also clears the hash table. No attempt to rehash.
            capacity = (Math.Min(sizeMb, MAX_SIZE_MB) * MB_SIZE) / ITEM_SIZE;
            table = new TtPawnItem[capacity];
            mask = (uint)(capacity - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(ulong hash)
        {
            return (int)(hash & mask);
        }
    }
}
