using Pedantic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic
{
    public struct IncMomentum
    {
        private sbyte increment;
        private sbyte momentum;

        public IncMomentum(sbyte increment)
        {
            this.increment = Math.Abs(increment);
            momentum = 0;
        }

        public void Reset()
        {
            momentum = 0;
        }

        public int Direction => Math.Sign(momentum);
        public short BestIncrement
        {
            get
            {
                if (increment == 0)
                {
                    return 0;
                }

                if (momentum == 0)
                {
                    if (Random.Shared.NextBoolean())
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
                    short adjust = (short)(Math.Log(Math.Abs(momentum), 2.0) + 1);
                    return (short)(sign * (increment * adjust));
                }
            }
        }

        public short NegIncrement(short increment)
        {
            if (this.increment == 0)
            {
                return 0;
            }

            if (momentum == 0)
            {
                return (short)(-increment * 2);
            }

            int sign = Math.Sign(BestIncrement) * -1;
            short negIncrement = (short)(Math.Abs(BestIncrement) + this.increment);
            return (short)(negIncrement * sign);
        }

        public void AddImprovingIncrement(sbyte increment)
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
    }
}
