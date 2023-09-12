using Pedantic.Utilities;

namespace Pedantic.Tuning
{
    public struct IncMomentum
    {
        private readonly short increment;
        private short momentum;
        private static Random rand = Random.Shared;

        public IncMomentum(short increment)
        {
            this.increment = Math.Abs(increment);
            momentum = 0;
        }

        public void Reset()
        {
            momentum = 0;
        }

        public readonly int Direction => Math.Sign(momentum);
        public readonly short BestIncrement
        {
            get
            {
                if (increment == 0)
                {
                    return 0;
                }

                if (momentum == 0)
                {
                    if (rand.NextBoolean())
                    {
                        return (short)-increment;
                    }
                    else
                    {
                        return increment;
                    }
                }
                else
                {
                    int sign = Direction;
                    short magnitude = (short)(increment + (short)Math.Log2(Math.Abs(momentum)));
                    return (short)(sign * magnitude);

                }
            }
        }

        public static short NegIncrement(short increment)
        {
            if (increment == 0)
            {
                return 0;
            }

            int sign = Math.Sign(increment);
            return (short)-(increment + sign);
        }

        public void AddImprovingIncrement(short increment)
        {
            if (this.increment == 0)
            {
                return;
            }

            int sign = Math.Sign(increment);
            if (Direction == sign || momentum == 0)
            {
                if (sign < 0)
                {
                    momentum--;
                }
                else
                {
                    momentum++;
                }
            }
            else
            {
                momentum = 0;
            }
        }

        public void NoImprovement()
        {
            momentum /= 2;
        }

        public static void SetRandomSeed(int? seed)
        {
            rand = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        }
    }
}
