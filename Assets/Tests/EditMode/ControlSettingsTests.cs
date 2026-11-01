using Game.Core.Settings;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class ControlSettingsTests
    {
        private static readonly ControlSettingsLimits Limits = new ControlSettingsLimits(0.1f, 5f, 55f, 100f);
        private static readonly ControlSettingsData Defaults = new ControlSettingsData(1f, false, 70f);

        [Test]
        public void Constructor_ClampsInitialValues()
        {
            var settings = new ControlSettings(Limits, new ControlSettingsData(50f, true, 10f));

            Assert.AreEqual(5f, settings.Sensitivity);
            Assert.AreEqual(55f, settings.FieldOfView);
            Assert.IsTrue(settings.InvertY);
        }

        [Test]
        public void SetSensitivity_OutOfRange_ClampsAndRaisesOnce()
        {
            var settings = new ControlSettings(Limits, Defaults);
            var events = 0;
            settings.Changed += () => events++;

            settings.SetSensitivity(-3f);

            Assert.AreEqual(0.1f, settings.Sensitivity, 1e-6f);
            Assert.AreEqual(1, events);
        }

        [Test]
        public void SetSameValue_DoesNotRaise()
        {
            var settings = new ControlSettings(Limits, Defaults);
            var events = 0;
            settings.Changed += () => events++;

            settings.SetSensitivity(1f);
            settings.SetInvertY(false);
            settings.SetFieldOfView(70f);

            Assert.AreEqual(0, events);
        }

        [Test]
        public void ApplyToLook_ScalesBySensitivity()
        {
            var settings = new ControlSettings(Limits, new ControlSettingsData(2f, false, 70f));

            Assert.AreEqual(new Vector2(20f, -10f), settings.ApplyToLook(new Vector2(10f, -5f)));
        }

        [Test]
        public void ApplyToLook_InvertY_FlipsOnlyVertical()
        {
            var settings = new ControlSettings(Limits, new ControlSettingsData(1f, true, 70f));

            Assert.AreEqual(new Vector2(10f, 5f), settings.ApplyToLook(new Vector2(10f, -5f)));
        }

        [Test]
        public void Load_NaN_FallsBackToMinimum()
        {
            var settings = new ControlSettings(Limits, Defaults);

            settings.Load(new ControlSettingsData(float.NaN, false, float.NaN));

            Assert.AreEqual(Limits.MinSensitivity, settings.Sensitivity);
            Assert.AreEqual(Limits.MinFieldOfView, settings.FieldOfView);
        }

        [Test]
        public void Load_ManyChanges_RaisesSingleEvent()
        {
            var settings = new ControlSettings(Limits, Defaults);
            var events = 0;
            settings.Changed += () => events++;

            settings.Load(new ControlSettingsData(2f, true, 90f));

            Assert.AreEqual(1, events);
        }
    }
}
