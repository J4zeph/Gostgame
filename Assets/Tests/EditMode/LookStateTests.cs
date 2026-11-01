using Game.Core.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class LookStateTests
    {
        private const float Sensitivity = 0.1f;
        private const float MinPitch = -85f;
        private const float MaxPitch = 85f;

        [Test]
        public void Apply_MouseRight_IncreasesYaw()
        {
            var look = new LookState();

            look.Apply(new Vector2(100f, 0f), Sensitivity, MinPitch, MaxPitch);

            Assert.AreEqual(10f, look.Yaw, 1e-4f);
        }

        [Test]
        public void Apply_MouseUp_LooksUp_WhichIsNegativePitch()
        {
            var look = new LookState();

            look.Apply(new Vector2(0f, 100f), Sensitivity, MinPitch, MaxPitch);

            Assert.AreEqual(-10f, look.Pitch, 1e-4f);
        }

        [Test]
        public void Apply_ClampsPitchAtLimits()
        {
            var look = new LookState();

            look.Apply(new Vector2(0f, 10000f), Sensitivity, MinPitch, MaxPitch);
            Assert.AreEqual(MinPitch, look.Pitch);

            look.Apply(new Vector2(0f, -100000f), Sensitivity, MinPitch, MaxPitch);
            Assert.AreEqual(MaxPitch, look.Pitch);
        }

        [Test]
        public void Apply_WrapsYawInto0To360()
        {
            var look = new LookState(350f);

            look.Apply(new Vector2(200f, 0f), Sensitivity, MinPitch, MaxPitch);
            Assert.AreEqual(10f, look.Yaw, 1e-3f);

            look.Apply(new Vector2(-300f, 0f), Sensitivity, MinPitch, MaxPitch);
            Assert.AreEqual(340f, look.Yaw, 1e-3f);
        }
    }
}
