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
