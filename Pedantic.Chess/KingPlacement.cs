using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public readonly struct KingPlacement
    {
        private readonly byte friendly;
        private readonly byte enemy;

        public KingPlacement(Color friendlyColor, int friendlyIndex, int enemyIndex)
        {
            int c = (int)friendlyColor;
            int o = (int)friendlyColor.Other();

            friendly = IndexToBucket(Index.NormalizedIndex[c][friendlyIndex]);
            enemy = IndexToBucket(Index.NormalizedIndex[o][enemyIndex]);
        }

        public KingPlacement(int intValue)
        {
            friendly = (byte)(intValue & 0x0f);
            enemy = (byte)((intValue >> 4) & 0x0f);
        }

        private KingPlacement(byte friendly, byte enemy)
        {
            this.friendly = friendly;
            this.enemy = enemy;
        }

        public byte Friendly => friendly;
        public byte Enemy => enemy;

        public KingPlacement Flip()
        {
            return new KingPlacement(enemy, friendly);
        }

        public static byte IndexToBucket(int index)
        {
            Util.Assert(Index.IsValid(index));
            var coords = Index.ToCoords(index);
            byte bucket = (byte)((coords.Rank / 2) * 4 + coords.File / 2);
            return bucket;
        }

        public static explicit operator int(KingPlacement kp)
        {
            return (kp.Enemy << 4) + kp.Friendly;
        }
    }
}
