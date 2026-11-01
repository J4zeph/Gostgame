using System;
using Game.Core.Interaction;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class DoorModelTests
    {
        private const float OpenAngle = -100f;
        private const float Speed = 200f;

        [Test]
        public void NewDoor_IsClosedAndStill()
        {
            var door = new DoorModel(OpenAngle, Speed);

            Assert.IsFalse(door.IsOpen);
            Assert.AreEqual(0f, door.Angle);
            Assert.IsFalse(door.IsMoving);
        }

        [Test]
        public void Toggle_OpensAndRaisesEventOnce()
        {
            var door = new DoorModel(OpenAngle, Speed);
            var events = 0;
            var lastState = false;
            door.StateChanged += open => { events++; lastState = open; };

            door.Toggle();

            Assert.IsTrue(door.IsOpen);
            Assert.IsTrue(door.IsMoving);
            Assert.AreEqual(1, events);
            Assert.IsTrue(lastState);
        }

        [Test]
        public void SetOpen_SameState_DoesNotRaiseEvent()
        {
            var door = new DoorModel(OpenAngle, Speed);
            var events = 0;
            door.StateChanged += _ => events++;

            door.SetOpen(false);

            Assert.AreEqual(0, events);
        }

        [Test]
        public void Tick_MovesAngleTowardTargetAtSpeed_AndStopsExactly()
        {
            var door = new DoorModel(OpenAngle, Speed);
            door.Toggle();

            door.Tick(0.25f);
            Assert.AreEqual(-50f, door.Angle, 1e-3f);

            door.Tick(10f);
            Assert.AreEqual(OpenAngle, door.Angle);
            Assert.IsFalse(door.IsMoving);
        }

        [Test]
        public void Toggle_WhileOpening_TurnsBack()
        {
            var door = new DoorModel(OpenAngle, Speed);
            door.Toggle();
            door.Tick(0.25f);

            door.Toggle();
            door.Tick(0.1f);

            Assert.IsFalse(door.IsOpen);
            Assert.AreEqual(-30f, door.Angle, 1e-3f);
        }

        [Test]
        public void Constructor_NonPositiveSpeed_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DoorModel(OpenAngle, 0f));
        }
    }
}
