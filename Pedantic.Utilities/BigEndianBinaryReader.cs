using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Utilities
{
    public class BigEndianBinaryReader : BinaryReader
    {
        public BigEndianBinaryReader(Stream stream)
            : base(stream, Encoding.ASCII)
        { }

        public override ushort ReadUInt16()
        {
            byte[] data = ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data);
        }

        public override uint ReadUInt32()
        {
            byte[] data = ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data);
        }

        public override ulong ReadUInt64()
        {
            byte[] data = ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToUInt64(data);
        }
    }
}
