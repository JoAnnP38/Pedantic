using System.Runtime.Intrinsics.X86;
using Pedantic.Utilities;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class BitOpsTests
    {
        [TestMethod]
        public void GetMaskTest()
        {
            ulong actual = BitOps.GetMask(12);
            Assert.AreEqual(4096ul, actual);
        }

        [TestMethod]
        public void SetBitTest()
        {
            ulong actual = BitOps.SetBit(0, 13);
            Assert.AreEqual(8192ul, actual);

        }

        [TestMethod]
        public void ResetBitTest()
        {
            ulong actual = BitOps.ResetBit(0x0ffful, 10);
            Assert.AreEqual(0x0bfful, actual);
        }

        [TestMethod]
        public void GetBitTest()
        {
            int actual1 = BitOps.GetBit(0x0bfful, 10);
            int actual2 = BitOps.GetBit(0x0bfful, 11);
            Assert.AreEqual(0, actual1);
            Assert.AreEqual(1, actual2);
        }

        [TestMethod]
        public void TzCountTest()
        {
            int actual = BitOps.TzCount(0x478169C2B0ul);
            Assert.AreEqual(4, actual);
        }

        [TestMethod]
        public void LzCountTest()
        {
            int actual = BitOps.LzCount(0x538745698545BFDul);
            Assert.AreEqual(5, actual);
        }

        [TestMethod]
        public void ResetLsbTest()
        {
            ulong actual = BitOps.ResetLsb(0x0ffcul);
            Assert.AreEqual(0x0ff8ul, actual);
        }

        [TestMethod]
        public void AndNotTest()
        {
            ulong actual = BitOps.AndNot(0x0ffful, 0x0800ul);
            Assert.AreEqual(0x07fful, actual);
        }

        [TestMethod]
        public void PopCountTest()
        {
            int actual = BitOps.PopCount(0x0e4930ul);
            Assert.AreEqual(8, actual);
        }

        [TestMethod]
        public void BitFieldExtractTest()
        {
            int actual = BitOps.BitFieldExtract(0x0f00, 10, 4);
            Assert.AreEqual(3, actual);
        }

        [TestMethod]
        public void BitFieldSetTest()
        {
            ulong actual = BitOps.BitFieldSet(0ul, 3, 10, 4);
            Assert.AreEqual(0x0C00ul, actual);
        }

        [TestMethod]
        public void GreatestPowerOfTwoLessThanTest()
        {
            int actual = BitOps.GreatestPowerOfTwoLessThan(2000);
            Assert.AreEqual(1024, actual);

            actual = BitOps.GreatestPowerOfTwoLessThan(128);
            Assert.AreEqual(64, actual);
        }

        [TestMethod]
        public void ParallelBitDepositText()
        {
            ulong expected = Bmi2.X64.ParallelBitDeposit(10107ul, 0x0ff0ff0fful);
            ulong actual = BitOps.ParallelBitDeposit(10107ul, 0x0ff0ff0fful);
            Assert.AreEqual(expected, actual);

        }
    }
}