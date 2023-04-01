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
namespace Pedantic.Chess
{
    public readonly struct Ray
    {
        private readonly ulong north;
        private readonly ulong northEast;
        private readonly ulong east;
        private readonly ulong southEast;
        private readonly ulong south;
        private readonly ulong southWest;
        private readonly ulong west;
        private readonly ulong northWest;

        public Ray(ulong north, ulong northEast, ulong east, ulong southEast, ulong south, ulong southWest, ulong west, ulong northWest)
        {
            this.north = north;
            this.northEast = northEast;
            this.east = east;
            this.southEast = southEast;
            this.south = south;
            this.southWest = southWest;
            this.west = west;
            this.northWest = northWest;
        }

        public ulong North => north;
        public ulong NorthEast => northEast;
        public ulong East => east;
        public ulong SouthEast => southEast;
        public ulong South => south;
        public ulong SouthWest => southWest;
        public ulong West => west;
        public ulong NorthWest => northWest;
    }
}
