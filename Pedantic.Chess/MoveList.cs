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
using Pedantic.Collections;
using Pedantic.Utilities;
using System;

namespace Pedantic.Chess
{
    public sealed class MoveList : ValueList<ulong>, IPooledObject<MoveList>
    {
        public MoveList() : base(Constants.AVG_MOVES_PER_PLY * 2)
        { }

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

        public void Add(int from, int to, MoveType type = MoveType.Normal, Piece capture = Piece.None,
            Piece promote = Piece.None, int score = 0)
        {
            Add(Move.Pack(from, to, type, capture, promote, score));
        }

        public void Add(ulong[] moves, int count)
        {
            for (int n = 0; n < count; n++)
            {
                Add(moves[n]);
            }
        }

        public new bool Remove(ulong move)
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

        public ReadOnlySpan<ulong> ToSpan() => new(array, 0, Count);

        public void UpdateScores(ulong pv, KillerMoves killerMoves, int ply)
        {
            int found = 0;
            ref KillerMoves.KillerMove km = ref killerMoves.GetKillers(ply);

            for (int n = 0; n < insertIndex && found < 3; ++n)
            {
                ulong move = array[n];
                bool isCapture = Move.GetCapture(move) != Piece.None;

                if (Move.Compare(move, pv) == 0)
                {
                    array[n] = Move.SetScore(move, Constants.PV_SCORE);
                    found++;
                }
                else if (!isCapture)
                {
                    if (KillerMoves.MovesEqual(move, km.Killer0))
                    {
                        array[n] = Move.SetScore(move, Constants.KILLER_SCORE - 0);
                        found++;
                    }

                    if (KillerMoves.MovesEqual(move, km.Killer1))
                    {
                        array[n] = Move.SetScore(move, Constants.KILLER_SCORE - 1);
                        found++;
                    }
                }
            }
        }

        public ref ulong LastAdded
        {
            get
            {
                if (insertIndex == 0)
                {
                    throw new InvalidOperationException("Move list is empty.");
                }

                return ref array[insertIndex - 1];
            }
        }
    }
}
