using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pedantic.Utilities;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class ArithTests
    {
        [TestMethod]
        public void AbsTest()
        {
            Assert.AreEqual(5, Math.Abs(5));
            Assert.AreEqual(5, Math.Abs(-5));
        }

        [TestMethod]
        public void MaxTest()
        {
            Assert.AreEqual(5, Math.Max(-5, 5));
            Assert.AreEqual(5, Math.Max(5, -5));
        }

        [TestMethod]
        public void MinTest()
        {
            Assert.AreEqual(-5, Math.Min(-5, 5));
            Assert.AreEqual(-5, Math.Min(5, -5));
        }

        [TestMethod]
        public void SignTest()
        {
            Assert.AreEqual(-1, Math.Sign(-5));
            Assert.AreEqual(1, Math.Sign(5));
        }
    }
}
