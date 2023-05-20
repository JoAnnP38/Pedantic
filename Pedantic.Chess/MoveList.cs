﻿// ***********************************************************************
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

namespace Pedantic.Chess
{
    public sealed class MoveList : ValueList<ulong>, IPooledObject<MoveList>
    {
        public MoveList() : base(Constants.AVG_MOVES_PER_PLY * 2)
        { }

        public void Sort(int n)
        {
            int largest = -1;
            int score = -1;
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
        }

        public void Add(int from, int to, MoveType type = MoveType.Normal, Piece capture = Piece.None,
            Piece promote = Piece.None, int score = 0)
        {
            Add(Move.Pack(from, to, type, capture, promote, score));
        }

        public ReadOnlySpan<ulong> ToSpan() => new(array, 0, Count);

        public void UpdateScores(ulong pv, KillerMoves killerMoves, int ply)
        {
            int found = 0;
            ulong[] killers = killerMoves.GetKillers(ply);
            for (int n = 0; n < insertIndex && found < killers.Length + 1; ++n)
            {
                ulong fromto = array[n] & 0x0fff;
                bool isCapture = Move.GetCapture(array[n]) != Piece.None;

                if (fromto == (pv & 0x0fff))
                {
                    array[n] = BitOps.BitFieldSet(array[n], Constants.PV_SCORE, 24, 16);
                    found++;
                }
                else if (!isCapture)
                {
                    for (int m = 0; m < killers.Length; ++m)
                    {
                        if (fromto == (killers[m] & 0x0fff))
                        {
                            array[n] = BitOps.BitFieldSet(array[n], Constants.KILLER_SCORE - found++, 24, 16);
                        }
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
