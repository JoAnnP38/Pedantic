using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Chess
{
    [Serializable]
    public struct PolyglotEntry
    {
        private ulong key;
        private ushort move;
        private ushort weight;
        private uint learn;

        public ulong Key
        {
            get => key;
            set => key = value;
        }

        public ushort Move
        {
            get => move;
            set => move = value;
        }

        public ushort Weight
        {
            get => weight;
            set => weight = value;
        }

        public uint Learn
        {
            get => learn;
            set => learn = value;
        }
    }
}
