using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Pedantic.Tuning;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class IncMomentumTests
    {

        [TestMethod]
        public void AddImprovingIncrementPosTest()
        {
            IncMomentum mom = new(1);
            short increment = 1;

            for (int n = 0; n < 8; n++)
            {
                mom.AddImprovingIncrement(increment);
            }

            Assert.AreEqual(4, mom.BestIncrement);
        }

        [TestMethod]
        public void AddImprovingIncrementNegTest()
        {
            IncMomentum mom = new(1);
            short increment = -1;

            for (int n = 0; n < 8; n++)
            {
                mom.AddImprovingIncrement(increment);
            }

            Assert.AreEqual(-4, mom.BestIncrement);
        }

        [TestMethod]
        public void AddImprovingIncrementOppositeSignTest()
        {
            IncMomentum mom = new(1);
            short increment = 1;

            for (int n = 0; n < 8; n++)
            {
                mom.AddImprovingIncrement(increment);
            }

            increment = mom.BestIncrement;
            increment = IncMomentum.NegIncrement(increment);
            mom.AddImprovingIncrement(increment);

            Assert.AreEqual(1, Math.Abs(mom.BestIncrement));
        }

        [TestMethod]
        public void NegIncrementTest()
        {
            IncMomentum mom = new(1);

            short increment = 4;

            Assert.AreEqual(-5, IncMomentum.NegIncrement(increment));

            increment = -4;

            Assert.AreEqual(5, IncMomentum.NegIncrement(increment));
        }
    }
}
