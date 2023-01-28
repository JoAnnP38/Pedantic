using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public class PvList
    {
        private readonly ulong[][] pvTable = Mem.Allocate2D<ulong>(Constants.MAX_PLY, Constants.MAX_PLY);
        private readonly int[] pvLength = new int[Constants.MAX_PLY];

        public void Clear()
        {
            Array.Clear(pvLength);
        }

        public void Store(int ply, ulong move)
        {
            pvTable[ply][ply] = move;
            for (int n = ply + 1; n < pvLength[ply + 1]; n++)
            {
                pvTable[ply][n] = pvTable[ply + 1][n];
            }

            pvLength[ply] = pvLength[ply + 1];
        }

        public void Append(int ply, ulong move)
        {
            pvTable[ply][pvLength[ply]++] = move;
        }

        public void AdvancePly(int ply)
        {
            pvLength[ply] = ply;
        }

        public ReadOnlySpan<ulong> Pv => new(pvTable[0], 0, pvLength[0]);
    }
}
