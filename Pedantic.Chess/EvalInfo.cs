using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    public struct EvalInfo
    {
        public ulong PawnAttacks;
        public ulong KingAttacks;
        public ulong AllAttacks;
        public ulong Pawns;
        public ulong EnemyPawns;
        public ulong AllPawns;
        public ulong PassedPawns;
        public KingPlacement KP;
        public short OpScore;
        public short EgScore;
    }
}
