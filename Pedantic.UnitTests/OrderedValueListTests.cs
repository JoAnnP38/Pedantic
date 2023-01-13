using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Collections;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class OrderedValueListTests
    {
        [TestMethod]
        public void AddTest()
        {
            OrderedValueList<ulong> list = new();
            list.Add(5ul);
            list.Add(6ul);

            Assert.AreEqual(2, list.Count);
        }

        [TestMethod]
        public void AddMultipleTest()
        {
            OrderedValueList<ulong> list = new();
            ulong[] items = { 6u, 3ul, 12ul, 3ul, 7ul };
            list.Add(items);

            ulong lastItem = 0ul;
            foreach (ulong item in list)
            {
                Assert.IsTrue(item >= lastItem);
                lastItem = item;
            }
        }

        [TestMethod]
        public void ContainsTest()
        {
            OrderedValueList<ulong> list = new();
            ulong[] items = { 6u, 3ul, 12ul, 3ul, 7ul };
            list.Add(items);

            Assert.IsTrue(list.Contains(3ul));
            Assert.IsTrue(list.Contains(12ul));
            Assert.IsTrue(list.Contains(6ul));
        }

        [TestMethod]
        public void RemoveTest()
        {
            OrderedValueList<ulong> list = new();
            ulong[] items = { 6u, 3ul, 12ul, 3ul, 7ul };
            list.Add(items);

            list.Remove(3ul);

            Assert.AreEqual(3ul, list[0]);
            Assert.AreEqual(6ul, list[1]);
            Assert.AreEqual(7ul, list[2]);
            Assert.AreEqual(12ul, list[3]);
            Assert.AreEqual(4, list.Count);
        }

        [TestMethod]
        public void InsertTest()
        {
            OrderedValueList<ulong> list = new();
            ulong[] items = { 6u, 3ul, 12ul, 3ul, 7ul };
            list.Add(items);

            list.Insert(0, 8ul);

            Assert.AreEqual(6, list.Count);
            Assert.AreEqual(8ul, list[4]);
        }
    }
}