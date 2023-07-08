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

using System.Runtime.CompilerServices;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed class KillerMoves
    {
        public struct KillerMove
        {
            public ulong Killer0;
            public ulong Killer1;
        }

        public KillerMoves()
        {
            killers = new KillerMove[Constants.MAX_PLY];
        }

        public void Add(ulong move, int ply)
        {
            ref KillerMove km = ref killers[ply];

            if (MovesEqual(move, km.Killer0))
            {
                return;
            }

            if (MovesEqual(move, km.Killer1))
            {
                (km.Killer0, km.Killer1) = (km.Killer1, km.Killer0);
                return;
            }

            km.Killer1 = km.Killer0;
            km.Killer0 = Move.SetScore(move, Constants.KILLER_SCORE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref KillerMove GetKillers(int ply)
        {
            return ref killers[ply];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Exists(int ply, ulong move)
        {
            ref KillerMove km = ref killers[ply];
            return MovesEqual(km.Killer0, move) || MovesEqual(km.Killer1, move);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MovesEqual(ulong move1, ulong move2)
        {
            return Move.Compare(move1, move2) == 0;
        }

        private readonly KillerMove[] killers;
    }
}
