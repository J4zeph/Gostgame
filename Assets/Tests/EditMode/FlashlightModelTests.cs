using Game.Core.Equipment;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class FlashlightModelTests
    {
        [Test]
        public void Toggle_SwitchesStateAndRaisesEvent()
        {
            var flashlight = new FlashlightModel();
            bool? reported = null;
            flashlight.Switched += on => reported = on;

            flashlight.Toggle();

            Assert.IsTrue(flashlight.IsOn);
            Assert.AreEqual(true, reported);
        }

        [Test]
        public void SetOn_SameState_DoesNotRaiseEvent()
        {
            var flashlight = new FlashlightModel(startOn: true);
            var events = 0;
            flashlight.Switched += _ => events++;

            flashlight.SetOn(true);

            Assert.AreEqual(0, events);
        }
    }
}
