using Game.Core.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class ReachRulesTests
    {
        [Test]
        public void WithinReach_True()
        {
            Assert.IsTrue(ReachRules.IsWithinReach(Vector3.zero, new Vector3(0f, 0f, 1.9f), 2f, 0f));
        }

        [Test]
        public void BeyondReachButWithinSlack_True()
        {
            Assert.IsTrue(ReachRules.IsWithinReach(Vector3.zero, new Vector3(0f, 0f, 2.5f), 2f, 0.75f));
        }

        [Test]
        public void BeyondReachAndSlack_False()
        {
            Assert.IsFalse(ReachRules.IsWithinReach(Vector3.zero, new Vector3(0f, 0f, 3f), 2f, 0.75f));
        }

        [Test]
        public void NegativeSlack_TreatedAsZero()
        {
            Assert.IsTrue(ReachRules.IsWithinReach(Vector3.zero, new Vector3(2f, 0f, 0f), 2f, -5f));
        }
    }
}
