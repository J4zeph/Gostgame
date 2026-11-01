using System;
using Game.Core.Session;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class ConnectionRulesTests
    {
        [TestCase(0, 4, true)]
        [TestCase(3, 4, true)]
        [TestCase(4, 4, false)]
        [TestCase(5, 4, false)]
        public void CanAccept_RespectsMaxPlayers(int connected, int max, bool expected)
        {
            Assert.AreEqual(expected, ConnectionRules.CanAccept(connected, max));
        }

        [TestCase(0, 4, 0)]
        [TestCase(3, 4, 3)]
        [TestCase(5, 4, 1)]
        [TestCase(2, 1, 0)]
        public void SpawnIndexFor_CyclesThroughPoints(int connected, int points, int expected)
        {
            Assert.AreEqual(expected, ConnectionRules.SpawnIndexFor(connected, points));
        }

        [Test]
        public void SpawnIndexFor_NoPoints_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ConnectionRules.SpawnIndexFor(0, 0));
        }
    }
}
