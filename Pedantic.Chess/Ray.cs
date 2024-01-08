// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 01-17-2023
// ***********************************************************************
// <copyright file="Ray.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     A structure that holds the ray masks in every direction from a 
//     square on the chess board.
// </summary>
// ***********************************************************************
using System.Runtime.CompilerServices;

namespace Pedantic.Chess
{
    public readonly struct Ray
    {
        [InlineArray(Constants.MAX_DIRECTIONS)]
        private struct DirArray
        {
            public ulong _element0;
        };
        
        private readonly DirArray rays;

        public Ray(ulong north, ulong northEast, ulong east, ulong southEast, ulong south, ulong southWest, ulong west, ulong northWest)
        {
            North = north;
            NorthEast = northEast;
            East = east;
            SouthEast = southEast;
            South = south;
            SouthWest = southWest;
            West = west;
            NorthWest = northWest;
        }

        public ulong North 
        {
            get => rays[(int)Direction.North];
            init => rays[(int)Direction.North] = value;
        }

        public ulong NorthEast
        {
            get => rays[(int)Direction.NorthEast];
            init => rays[(int)Direction.NorthEast] = value;
        }

        public ulong East
        {
            get => rays[(int)Direction.East];
            init => rays[(int)Direction.East] = value;
        }

        public ulong SouthEast
        {
            get => rays[(int)Direction.SouthEast];
            init => rays[(int)Direction.SouthEast] = value;
        }

        public ulong South
        {
            get => rays[(int)Direction.South];
            init => rays[(int)Direction.South] = value;
        }

        public ulong SouthWest
        {
            get => rays[(int)Direction.SouthWest];
            init => rays[(int)Direction.SouthWest] = value;
        }

        public ulong West
        {
            get => rays[(int)Direction.West];
            init => rays[(int)Direction.West] = value;
        }

        public ulong NorthWest
        {
            get => rays[(int)Direction.NorthWest];
            init => rays[(int)Direction.NorthWest] = value;
        }

        public ulong this[Direction index] => rays[(int)index];
    }
}
