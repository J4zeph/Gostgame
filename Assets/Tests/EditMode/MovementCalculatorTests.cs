using Game.Core.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class MovementCalculatorTests
    {
        private const float Speed = 2f;
        private const float Gravity = -15f;
        private const float Grounded = -2f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void HorizontalVelocity_ForwardAtZeroYaw_IsPlusZ()
        {
            var v = MovementCalculator.HorizontalVelocity(Vector2.up, 0f, Speed);

            Assert.AreEqual(0f, v.x, Tolerance);
            Assert.AreEqual(0f, v.y, Tolerance);
            Assert.AreEqual(Speed, v.z, Tolerance);
        }

        [Test]
        public void HorizontalVelocity_ForwardAtYaw90_IsPlusX()
        {
            var v = MovementCalculator.HorizontalVelocity(Vector2.up, 90f, Speed);

            Assert.AreEqual(Speed, v.x, Tolerance);
            Assert.AreEqual(0f, v.z, Tolerance);
        }

        [Test]
        public void HorizontalVelocity_Diagonal_IsNotFasterThanStraight()
        {
            var v = MovementCalculator.HorizontalVelocity(new Vector2(1f, 1f), 0f, Speed);

            Assert.AreEqual(Speed, v.magnitude, Tolerance);
        }

        [Test]
        public void HorizontalVelocity_NoInput_IsZero()
        {
            Assert.AreEqual(Vector3.zero, MovementCalculator.HorizontalVelocity(Vector2.zero, 45f, Speed));
        }

        [Test]
        public void VerticalVelocity_InAir_Accelerates()
        {
            var v = MovementCalculator.VerticalVelocity(0f, false, Gravity, Grounded, 0.1f);

            Assert.AreEqual(-1.5f, v, Tolerance);
        }

        [Test]
        public void VerticalVelocity_LandingFast_ResetsToGroundedVelocity()
        {
            var v = MovementCalculator.VerticalVelocity(-20f, true, Gravity, Grounded, 0.1f);

            Assert.AreEqual(Grounded, v, Tolerance);
        }
    }
}
