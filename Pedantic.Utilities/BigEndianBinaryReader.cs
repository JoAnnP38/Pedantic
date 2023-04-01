// ***********************************************************************
// Assembly         : Pedantic.Utilities
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="BigEndianBinaryReader.cs" company="Pedantic.Utilities">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Used to deserialize polyglot book file into array of Polyglot 
//     structures. Handles opposite endianess of file.
// </summary>
// ***********************************************************************
using System.Text;

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
