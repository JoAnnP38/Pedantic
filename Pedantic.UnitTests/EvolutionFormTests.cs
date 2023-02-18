using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;
using LiteDB;
using Pedantic.Genetics;


namespace Pedantic.UnitTests
{
    [TestClass]
    public class EvolutionFormTests
    {
        public class MatchKey : IEquatable<MatchKey>, IComparable<MatchKey>
        {
            public int Round { get; set; }
            public ObjectId MinPlayer { get; set; }
            public ObjectId MaxPlayer { get; set; }

            public MatchKey(int round, ObjectId player1, ObjectId player2)
            {
                Round = round;
                int compare1 = player1.CompareTo(player2);
                int compare2 = player2.CompareTo(player1);
                MinPlayer = player1.CompareTo(player2) <= 0 ? player1 : player2;
                MaxPlayer = player1.CompareTo(player2) <= 0 ? player2 : player1;
            }

            public bool Equals(MatchKey? other)
            {
                if (other == null)
                {
                    return false;
                }

                return Round == other.Round && MinPlayer.Equals(other.MinPlayer) && MaxPlayer.Equals(other.MaxPlayer);
            }

            public override bool Equals(object? obj)
            {
                if (obj == null)
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                return Equals(obj as MatchKey);
            }

            public int CompareTo(MatchKey? other)
            {
                if (other == null)
                {
                    return 1;
                }
                if (ReferenceEquals(this, other))
                {
                    return 0;
                }

                int result = Round - other.Round;
                if (result == 0)
                {
                    result = MinPlayer.CompareTo(other.MinPlayer);
                    if (result == 0)
                    {
                        return MaxPlayer.CompareTo(other.MaxPlayer);
                    }
                    return result;
                }
                return result;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Round, MinPlayer, MaxPlayer);
            }
        }

        [TestMethod]
        public void MatchKeyTest()
        {

            ObjectId id1 = ObjectId.NewObjectId();
            ObjectId id2 = ObjectId.NewObjectId();

            MatchKey key1 = new(1, id2, id1);
            MatchKey key2 = new(1, id1, id2);

            Assert.AreEqual(key1, key2);

            Dictionary<MatchKey, Match> lookup = new();

            lookup.Add(key1, new Match());

            Assert.IsTrue(lookup.ContainsKey(key1));

            Assert.IsTrue(lookup.ContainsKey(key2));
        }
    }
}