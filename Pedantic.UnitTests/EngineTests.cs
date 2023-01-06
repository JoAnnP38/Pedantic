using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Pedantic.Chess;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class EngineTests
    {
        [TestMethod]
        public void NoIllegalBookEntries()
        {
            int count = 0;
            for (int n = 0; n < Engine.BookEntries.Length; n++)
            {
                PolyglotEntry entry = Engine.BookEntries[n];

                if (entry.Weight == 0)
                {
                    count++;
                    Console.WriteLine($@"Book entry at ({n}) has weight of zero.");
                    Console.WriteLine($@"Key: 0x{entry.Key:X16}ul, BestMove: 0x{entry.Move:X8}");
                }
            }
            Assert.AreEqual(0, count);
        }

    }
}