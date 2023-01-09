using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            killers[ply][0] = move;
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
