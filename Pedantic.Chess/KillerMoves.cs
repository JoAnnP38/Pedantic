// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="KillerMoves.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Implement KillMoves collection that stores moves that resulted in 
//     a beta cut-off at a particular ply. Those moves can be tried again
//     in the next search branch over.
// </summary>
// ***********************************************************************
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class KillerMoves
    {
        public KillerMoves(int killersPerPly = 4)
        {
            killers = Mem.Allocate2D<ulong>(Constants.MAX_PLY, killersPerPly);
        }

        public void Add(ulong move, int ply)
        {
            int foundAt = 0;
            for (; foundAt < killers[ply].Length - 1; ++foundAt)
            {
                if (Move.Compare(killers[ply][foundAt], move) == 0)
                {
                    break; // move already in killers
                }
            }

            for (int n = foundAt; n > 0; --n)
            {
                killers[ply][n] = killers[ply][n - 1];
            }

            killers[ply][0] = Move.SetScore(move, Constants.KILLER_SCORE);
        }

        public ulong[] GetKillers(int ply)
        {
            return killers[ply];
        }

        public bool Exists(int ply, ulong move)
        {
            return Array.Exists(killers[ply], (item) => (item & 0x0fff) == (move & 0x0fff));
        }

        private readonly ulong[][] killers;
    }
}
