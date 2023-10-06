using Pedantic.Chess;

namespace Pedantic.UnitTests
{
    [TestClass]
    public class ScoreTests
    {
        [TestMethod]
        public void Ctor2Test()
        {
            Score score = new(-15, 5);
            Assert.AreEqual(-15, score.MgScore);
            Assert.AreEqual(5, score.EgScore);
        }

        [TestMethod]
        public void CtorTest()
        {
            Score score = new(-15, 5);
            int iScore = score;
            Score score2 = (Score)iScore;
            Assert.AreEqual(score.MgScore, score2.MgScore);
            Assert.AreEqual(score.EgScore, score2.EgScore);
        }

        [TestMethod]
        public void NormalizeScoreTest()
        {
            Score score = new(-15, 5);
            int phase = 50;
            int expected = (-15 * 50 + 5 * 14) / Constants.MAX_PHASE;
            Assert.AreEqual(expected, score.NormalizeScore(phase));
        }

        [TestMethod]
        public void EqualsTest()
        {
            Score s = new(-15, 5);
            Score r = s;
            Score q = new(5, -15);

            Assert.IsTrue(s.Equals(r));
            Assert.IsFalse(s.Equals(q));
        }

        [TestMethod]
        public void CompareToTest()
        {
            Score s = new(5, -15);
            Score r = Score.Zero;
            Score q = new(-15, 5);

            Assert.IsTrue(s.CompareTo(r) < 0);
            Assert.IsTrue(q.CompareTo(s) > 0);
            Assert.IsTrue(r.CompareTo(r) == 0);
        }

        [TestMethod]
        public void ToStringTest()
        {
            Score s = new(5, -15);
            string expected = $"(5, -15)";
            Assert.AreEqual(expected, s.ToString());
        }

        [TestMethod]
        public void GetHashCodeTest()
        {
            Score s = new(5, -15);
            int n = s;
            Assert.AreEqual(n.GetHashCode(), s.GetHashCode());
        }

        [TestMethod]
        public void EqualityOpTest()
        {
            Score s = Score.Zero;
            Score r = new(5, -15);
            Score q = new(5, -15);

            Assert.IsTrue(r == q);
            Assert.IsTrue(r != s);
        }

        [TestMethod]
        public void AdditionSubtractionTest()
        {
            Score s = Score.Zero;
            Score r = new(5, -15);
            Score q = new(-5, 15);

            Score result = r + q;
            Assert.AreEqual(s, result);

            result = s - r;
            Assert.AreEqual(q, result);
        }

        [TestMethod]
        public void MultiOpTest()
        {
            Score s = new(-15, 5);
            Score m = s * 5;

            Assert.AreEqual(-75, m.MgScore);
            Assert.AreEqual(25, m.EgScore);
        }

        [TestMethod]
        public void IncrementAssignmentTest()
        {
            Score lhs = Score.Zero;
            Score rhs = new(5, -10);

            lhs += rhs;

            Assert.AreEqual(lhs, rhs);
        }

        [TestMethod]
        public void DecrementAssignmentTest()
        {
            Score lhs = Score.Zero;
            Score rhs = new(5, -10);

            lhs -= rhs;

            Assert.AreEqual(lhs, -rhs);
        }
    }
}
