﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public sealed class PV
    {
        public PV()
        {
            Moves = new ulong[Constants.MAX_PLY][];
            for (int n = 0; n < Constants.MAX_PLY; n++)
            {
                Moves[n] = new ulong[Constants.MAX_PLY];
            }

            Length = new int[Constants.MAX_PLY];
        }

        public void Merge(int ply, ulong move)
        {
            Moves[ply][0] = move;
            Array.Copy(Moves[ply + 1], 0, Moves[ply], 1, Length[ply + 1]);
            Length[ply] = Length[ply + 1];
        }

        public void AddMove(int ply, ulong move)
        {
            Moves[ply][0] = move;
            Array.Fill(Moves[ply], 0ul, 1, Moves[ply].Length - 1);
        }

        public void Clear()
        {
            for (int n = 0; n < Constants.MAX_PLY; n++)
            {
                Array.Clear(Moves[n]);
            }

            Array.Clear(Length);
        }

        public ulong[][] Moves;
        public int[] Length;
    }
}