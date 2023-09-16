// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-26-2023
// ***********************************************************************
// <copyright file="TtTran.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     A transposition table dedicated to search. 
// </summary>
// ***********************************************************************
using System.Runtime.CompilerServices;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public static class TtTran
    {
        public const int DEFAULT_SIZE_MB = 64;
        public const int MAX_SIZE_MB = 2048;
        public const int ITEM_SIZE = 16;
        public const int MB_SIZE = 1024 * 1024;
        public const int CAPACITY_MULTIPLIER = MB_SIZE / ITEM_SIZE;

        public struct TtTranItem
        {
            private ulong hash;
            private ulong data;

            public TtTranItem(ulong hash, short score, sbyte depth, TtFlag ttFlag, ulong bestMove)
            {
                data = Move.ClearScore(bestMove) |
                       (((ulong)score & 0x0fffful) << 27) |
                       (((ulong)ttFlag & 0x03ul) << 43) |
                       (((byte)depth & 0x0fful) << 45) |
                       (((ulong)generation & 0x07fful) << 53); 

                this.hash = hash ^ data;
            }

            public readonly ulong Hash => hash ^ data;
            public readonly ulong Data => data;
            public readonly ulong BestMove => (ulong)BitOps.BitFieldExtract(data, 0, 27);
            public readonly short Score => (short)BitOps.BitFieldExtract(data, 27, 16);
            public readonly TtFlag Flag => (TtFlag)BitOps.BitFieldExtract(data, 43, 2);
            public readonly sbyte Depth => (sbyte)BitOps.BitFieldExtract(data, 45, 8);
            public ushort Age
            {
#pragma warning disable IDE0251 // Make member 'readonly'
                get => (ushort) BitOps.BitFieldExtract(data, 53, 11);
                set => BitOps.BitFieldSet(data, value, 53, 11);
#pragma warning restore IDE0251 // Make member 'readonly'
            }

            public bool InUse => Age == generation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool IsValid(ulong hash)
            {
                return (this.hash ^ data)  == hash;
            }

            public static void SetValue(ref TtTranItem item, ulong hash, short score, sbyte depth, 
                TtFlag flag, ulong bestMove)
            {
                item.data = Move.ClearScore(bestMove) |
                            (((ulong)score & 0x0fffful) << 27) |
                            (((ulong)flag & 0x03ul) << 43) |
                            (((byte)depth & 0x0fful) << 45) |
                            (((ulong)generation & 0x07fful) << 53);
                item.hash = hash ^ item.data;
            }
        }


        private static TtTranItem[] table;
        private static int capacity;
        private static int used;
        private static uint mask;
        private static ushort generation;

        static TtTran()
        {
            capacity = DEFAULT_SIZE_MB * CAPACITY_MULTIPLIER;
            used = 0;
            table = new TtTranItem[capacity];
            mask = (uint)(capacity - 1);
            generation = 1;
        }

        public static void IncrementVersion() 
        { 
            generation++;
        }

        public static void Add(ulong hash, int depth, int ply, int alpha, int beta, int score, ulong move)
        {
            int index = GetStoreIndex(hash, depth);

            ref TtTranItem item = ref table[index];
            ulong bestMove = move;

            if (item.IsValid(hash))
            {
                bestMove = bestMove == 0 ? item.BestMove : bestMove;
            }

            if (item.Age == 0)
            {
                ++used;
            }

            if (score >= Constants.TABLEBASE_WIN)
            {
                score += ply;
            }
            else if (score <= Constants.TABLEBASE_LOSS)
            {
                score -= ply;
            }

            TtFlag flag = TtFlag.Exact;

            if (score <= alpha)
            {
                flag = TtFlag.UpperBound;
            }
            else if (score >= beta)
            {
                flag = TtFlag.LowerBound;
            }

            sbyte itemDepth = (sbyte)depth;
            TtTranItem.SetValue(ref item, hash, (short)score, itemDepth, flag, bestMove);
        }

        public static void Clear()
        {
            Span<TtTranItem> spn = new(table);
            spn.Clear();
            used = 0;
            generation = 1;
        }

        public static void Resize(int sizeMb)
        {
            sizeMb = Math.Max(Math.Min(sizeMb, 2048), 2);
            if (!BitOps.IsPow2(sizeMb))
            {
                sizeMb = BitOps.GreatestPowerOfTwoLessThan(sizeMb);
            }
            // resizing also clears the hash table. No attempt to rehash.
            capacity = Math.Min(sizeMb, MAX_SIZE_MB) * CAPACITY_MULTIPLIER;
            table = new TtTranItem[capacity];
            mask = (uint)(capacity - 1);
            used = 0;
            generation = 1;
        }

        public static int Capacity => capacity;

        public static int Usage => (int)((used * 1000L) / capacity);

        public static bool TryGetBestMove(ulong hash, out ulong bestMove)
        {
            bestMove = 0ul;
            if (GetLoadIndex(hash, out int index))
            {
                bestMove = table[index].BestMove;
            }

            return bestMove != 0;
            
        }

        public static bool TryGetBestMoveWithFlags(ulong hash, out TtFlag flag, out ulong bestMove)
        {
            bestMove = 0ul;
            flag = TtFlag.UpperBound;

            if (GetLoadIndex(hash, out int index))
            {
                ref TtTranItem item = ref table[index];
                flag = item.Flag;
                bestMove = item.BestMove;
            }

            return bestMove != 0;
        }

        public static bool TryGetScore(ulong hash, int depth, int ply, int alpha, int beta, 
            out bool avoidNmp, out int score, out ulong move)
        {
            score = 0;
            move = 0;
            avoidNmp = false;

            if (GetLoadIndex(hash, out int index))
            {
                ref TtTranItem item = ref table[index];
                move = item.BestMove;

                if (item.Depth < depth)
                {
                    // even if the TT entry is not good enough to return a score,
                    // it may be good enough to determine if NMP should be run
                    if (item.Depth > 0 && item.Flag == TtFlag.UpperBound && 
                        depth - BasicSearch.NMP[depth] - 1 <= item.Depth &&
                        item.Score < beta)
                    {
                        avoidNmp = true;
                    }
                    return false;
                }

                score = item.Score;

                if (score >= Constants.TABLEBASE_WIN)
                {
                    score -= ply;
                }
                else if (score <= Constants.TABLEBASE_LOSS)
                {
                    score += ply;
                }

                if (item.Flag == TtFlag.Exact)
                {
                    return true;
                }

                if (item.Flag == TtFlag.UpperBound && score <= alpha)
                {
                    return true;
                }
                if (item.Flag == TtFlag.LowerBound && score >= beta)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetStoreIndex(ulong hash, int depth)
        {
            int index0 = (int)(hash & mask);
            int index1 = index0 ^ 1;
            ref TtTranItem item0 = ref table[index0];
            ref TtTranItem item1 = ref table[index1];

            if (item0.IsValid(hash))
            {
                return index0;
            }

            if (item1.IsValid(hash))
            {
                return index1;
            }

            if (item0.Age <= item1.Age && item0.Age < generation)
            {
                return index0;
            }
            else if (item1.Age <= item0.Age && item1.Age < generation)
            {
                return index1;
            }

            if (item0.Depth <= item1.Depth && item0.Depth < depth)
            {
                return index0;
            }

            if (item1.Depth <= item0.Depth && item1.Depth < depth)
            {
                return index1;
            }

            return index0;
        }

        private static bool GetLoadIndex(ulong hash, out int index)
        {
            index = (int)(hash & mask);
            if (!table[index].IsValid(hash))
            {
                index ^= 1;

                if (!table[index].IsValid(hash))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
