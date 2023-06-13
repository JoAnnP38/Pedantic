using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Pedantic.Chess;
using Pedantic.Tablebase;

namespace Pedantic.UnitTests
{
#if USE_TB
    [TestClass]
    public class SyzygyTests
    {
        private static bool syzygyInitialized = false;

        [TestInitialize]
        public void TestInitialize()        
        {
            //context = testContext;
            syzygyInitialized = Syzygy.Initialize("e:/tablebases/syzygy/3-4-5");
        }

        [TestCleanup]
        public void ClassCleanup()
        {
            //Syzygy.Uninitialize();
            syzygyInitialized = false;
        }

        [TestMethod]
        public void InitializeTest()
        {
            Assert.IsTrue(syzygyInitialized);
        }

        [TestMethod]
        public void TbLargestTest()
        {
            Assert.AreEqual((uint)5, Syzygy.TbLargest);
        }
    }
#endif
}
