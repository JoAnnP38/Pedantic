using Pedantic.Chess;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class EvalCacheTests
    {
        [TestMethod]
        public void PawnCacheItemSizeTest()
        {
            Assert.AreEqual(20, EvalCache.PawnCacheItem.Size);
        }

        [TestMethod]
        public void EvalCacheItemSizeTest()
        {
            Assert.AreEqual(11, EvalCache.EvalCacheItem.Size);
        }

        [TestMethod]
        public void CtorTest()
        {
            EvalCache cache = new();
            Assert.AreEqual(1_525_201, cache.EvalCacheSize);
            Assert.AreEqual(209_715, cache.PawnCacheSize);
        }

        [TestMethod]
        public void SaveEvalTest()
        {
            EvalCache cache = new();
            ulong hash = RandomHash();
            cache.SaveEval(hash, 10, Color.Black);

            if (cache.ProbeEvalCache(hash, Color.Black, out var result))
            {
                Assert.AreEqual(result.EvalScore, 10);
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void SavePawnEvalTest()
        {
            EvalCache cache = new();
            ulong pawnHash = RandomHash();
            ulong passedPawns = RandomHash();
            Score pawnScore = (Score)Random.Shared.Next(int.MinValue, int.MaxValue);
            cache.SavePawnEval(pawnHash, passedPawns, pawnScore);

            if (cache.ProbePawnCache(pawnHash, out var result))
            {
                Assert.AreEqual(passedPawns, result.PassedPawns);
                Assert.AreEqual(pawnScore, result.Eval);
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void ResizeTest()
        {
            EvalCache cache = new();
            cache.Resize(4);
            Assert.AreEqual(381_300, cache.EvalCacheSize);
            Assert.AreEqual(52_428, cache.PawnCacheSize);

            cache.Resize(1);
            Assert.AreEqual(381_300, cache.EvalCacheSize);
            Assert.AreEqual(52_428, cache.PawnCacheSize);
        }

        [TestMethod]
        public void ClearTest()
        {
            EvalCache cache = new();
            cache.Clear();
        }

        public static ulong RandomHash()
        {
            byte[] hashBytes = new byte[8];
            Random.Shared.NextBytes(hashBytes);
            return BitConverter.ToUInt64(hashBytes, 0);
        }
    }
}
