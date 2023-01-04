using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    [Serializable]
    public readonly struct PolyglotEntry
    {
        private readonly ulong key;
        private readonly ushort move;
        private readonly ushort weight;
        private readonly uint learn;

        public ulong Key
        {
            get => key;
            init => key = value;
        }

        public ushort Move
        {
            get => move;
            init => move = value;
        }

        public ushort Weight
        {
            get => weight;
            init => weight = value;
        }

        public uint Learn
        {
            get => learn;
            init => learn = value;
        }
    }
}
