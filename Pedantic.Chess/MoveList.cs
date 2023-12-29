// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="MoveList.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     MoveList class implements a pooled list class that preallocates
//     memory required by the list so this doesn't occur during search.
// </summary>
// ***********************************************************************
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class MoveList : IMoveList, IPooledObject<MoveList>
    {
        public const int CAPACITY = 218;

        [InlineArray(CAPACITY)]
        private struct MoveArray
        {
            public ulong _element0;
        }

        public int Count => insertIndex;

        public ulong this[int index]
        {
            get
            {
                if (index < 0 || index >= insertIndex)
                {
                    throw new IndexOutOfRangeException();
                }
                return array[index];
            }
        }

        public ulong Sort(int n)
        {
            int largest = -1;
            int score = short.MinValue;
            for (int i = n; i < insertIndex; ++i)
            {
                int mvScore = Move.GetScore(array[i]);
                if (mvScore > score)
                {
                    largest = i;
                    score = mvScore;
                }
            }

            if (largest > n)
            {
                (array[n], array[largest]) = (array[largest], array[n]);
            }

            return array[n];
        }

        public void Add(Color stm, Piece piece, int from, int to, MoveType type = MoveType.Normal, 
            Piece capture = Piece.None, Piece promote = Piece.None, int score = 0)
        {
            Add(Move.Pack(stm, piece, from, to, type, capture, promote, score));
        }

        public bool Remove(ulong move)
        {
            for (int n = 0; n < insertIndex; n++)
            {
                if (Move.Compare(array[n], move) == 0)
                {
                    array[n] = array[--insertIndex];
                    return true;
                }
            }
            return false;
        }

        public ReadOnlySpan<ulong> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref array._element0, insertIndex);

        public void Add(ulong move)
        {
            if (insertIndex >= CAPACITY)
            {
                throw new InsufficientMemoryException("Move list is full.");
            }
            array[insertIndex++] = move;
        }

        public void Add(IEnumerable<ulong> moves)
        {
            foreach (ulong move in moves)
            {
                Add(move);
            }
        }

        public void Clear()
        {
            insertIndex = 0;
        }

        public IEnumerator<ulong> GetEnumerator()
        {
            for (int n = 0; n < insertIndex; n++)
            {
                yield return array[n];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private MoveArray array;
        private int insertIndex;
    }
}
