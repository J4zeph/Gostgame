using Game.Core.Interaction;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class LightSwitchModelTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void StartsInGivenState(bool startOn)
        {
            Assert.AreEqual(startOn, new LightSwitchModel(startOn).IsOn);
        }

        [Test]
        public void Toggle_SwitchesAndReportsNewState()
        {
            var model = new LightSwitchModel();
            bool? reported = null;
            model.Switched += on => reported = on;

            model.Toggle();

            Assert.IsTrue(model.IsOn);
            Assert.AreEqual(true, reported);
        }

        [Test]
        public void SetOn_SameState_DoesNotReport()
        {
            var model = new LightSwitchModel(startOn: true);
            var reports = 0;
            model.Switched += _ => reports++;

            model.SetOn(true);

            Assert.AreEqual(0, reports);
        }

        [Test]
        public void SetOn_Off_TurnsLightOff()
        {
            var model = new LightSwitchModel(startOn: true);

            model.SetOn(false);

            Assert.IsFalse(model.IsOn);
        }
    }
}
