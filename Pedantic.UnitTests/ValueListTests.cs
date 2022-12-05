using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pedantic.Collections;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class ValueListTests
    {
        [TestMethod]
        public void CtorInt32Test()
        {
            ValueList<int> intList = new ValueList<int>(512);
            Assert.AreEqual(512, intList.Capacity);
            Assert.AreEqual(0, intList.Count);
        }

        [TestMethod]
        public void AddTest()
        {
            ValueList<int> list = new ValueList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(4, list.Capacity);

            list.Add(4);
            list.Add(5);

            Assert.AreEqual(5, list.Count);
            Assert.AreEqual(8, list.Capacity);
        }

        [TestMethod]
        public void AddMultipleTest()
        {
            ValueList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);

            ValueList<int> list2 = new();
            list2.Add(list);

            Assert.AreEqual(5, list2.Count);
        }

        [TestMethod]
        public void ClearTest()
        {
            ValueList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.AreEqual(3, list.Count);

            list.Clear();

            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void ContainsTest()
        {
            ValueList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.IsFalse(list.Contains(4));
            Assert.IsTrue(list.Contains(2));
        }

        [TestMethod]
        public void CopyToTest()
        {
            ValueList<int> list = new();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            int[] array = new int[list.Count];
            list.CopyTo(array);

            Assert.AreEqual(1, array[0]);
            Assert.AreEqual(2, array[1]);
            Assert.AreEqual(3, array[2]);
        }

        [TestMethod]
        public void RemoveTest()
        {
            ValueList<int> list = new() { 1, 2, 3 };
            list.Remove(2);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(3, list[1]);
        }

        [TestMethod]
        public void IndexOfTest()
        {
            ValueList<int> list = new() { 1, 2, 3 };
            int actual1 = list.IndexOf(4);
            int actual2 = list.IndexOf(2);

            Assert.AreEqual(-1, actual1);
            Assert.AreEqual(1, actual2);
        }

        [TestMethod]
        public void InsertTest()
        {
            ValueList<int> list = new() { 1, 2, 3, 4 };
            list.Insert(2, 7);
            Assert.AreEqual(5, list.Count);
            Assert.AreEqual(7, list[2]);
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            ValueList<int> list = new() { 1, 2, 7, 3, 4 };
            list.RemoveAt(2);

            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(4, list[2]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IndexerOutOfRangeLowTest()
        {
            ValueList<int> list = new() { 1, 2, 3 };
            int item = list[-1];
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IndexerOutOfRangeHighTest()
        {
            ValueList<int> list = new() { 1, 2, 3 };
            int item = list[3];
        }

        [TestMethod]
        public void MatchCountPredicateTest()
        {
            ValueList<int> list = new() { 1, 2, 3, 3, 4 };

            int actual1 = list.MatchCount(element => element.Equals(0));
            int actual2 = list.MatchCount(element => element.Equals(3));

            Assert.AreEqual(0, actual1);
            Assert.AreEqual(2, actual2);
        }

        [TestMethod]
        public void MatchCountTest()
        {
            ValueList<int> list = new() { 1, 2, 3, 3, 4 };

            int actual1 = list.MatchCount(0);
            int actual2 = list.MatchCount(3);

            Assert.AreEqual(0, actual1);
            Assert.AreEqual(2, actual2);
        }

        [TestMethod]
        public void CloneTest()
        {
            ValueList<int> list = new() { 1, 2, 3 };
            ValueList<int> actual = list.Clone();

            Assert.AreNotSame(list, actual);
            Assert.AreEqual(list.Count, actual.Count);
        }
    }
}