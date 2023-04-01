// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="PolyglotEntry.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Structure to hold a book opening position/move from a polyglot
//     format opening book file.
//     <see href="http://hgm.nubati.net/book_format.html"></see>
// </summary>
// ***********************************************************************
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
