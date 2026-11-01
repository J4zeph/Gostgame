using Game.Core.Settings;
using Game.Gameplay.Player;
using Game.Gameplay.Settings;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class GameSettingsTests
    {
        private static readonly ControlSettingsLimits Limits = new ControlSettingsLimits(0.1f, 5f, 55f, 100f);
        private static readonly ControlSettingsData Defaults = new ControlSettingsData(1f, false, 70f);

        private GameInput _input;
        private MemorySettingsStorage _storage;
        private GameSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _input = new GameInput();
            _storage = new MemorySettingsStorage();
            _settings = new GameSettings(Limits, Defaults, _input, _storage);
        }

        [TearDown]
        public void TearDown()
        {
            _input.Dispose();
        }

        [Test]
        public void Load_NothingSaved_KeepsDefaults()
        {
            Assert.IsFalse(_settings.Load());
            Assert.AreEqual(1f, _settings.Controls.Sensitivity);
            Assert.AreEqual(70f, _settings.Controls.FieldOfView);
        }

        [Test]
        public void SaveThenLoad_RestoresControlsAndKeys()
        {
            _settings.Controls.Load(new ControlSettingsData(2.5f, true, 90f));
            _input.OverrideBinding(GameInput.InteractId, "<Keyboard>/q");
            var interactKey = _input.GetBindingDisplay(GameInput.InteractId);
            _settings.Save();

            var otherInput = new GameInput();
            try
            {
                var restored = new GameSettings(Limits, Defaults, otherInput, _storage);

                Assert.IsTrue(restored.Load());
                Assert.AreEqual(2.5f, restored.Controls.Sensitivity, 1e-5f);
                Assert.IsTrue(restored.Controls.InvertY);
                Assert.AreEqual(90f, restored.Controls.FieldOfView, 1e-5f);
                Assert.AreEqual(interactKey, otherInput.GetBindingDisplay(GameInput.InteractId));
            }
            finally
            {
                otherInput.Dispose();
            }
        }

        [Test]
        public void Load_CorruptJson_KeepsDefaults()
        {
            _storage.Json = "{ this is not json";

            Assert.IsFalse(_settings.Load());
            Assert.AreEqual(1f, _settings.Controls.Sensitivity);
        }

        [Test]
        public void Load_MissingControls_UsesDefaultsForThem()
        {
            _storage.Json = "{\"BindingOverrides\":\"\"}";

            Assert.IsTrue(_settings.Load());
            Assert.AreEqual(1f, _settings.Controls.Sensitivity);
            Assert.AreEqual(70f, _settings.Controls.FieldOfView);
        }

        [Test]
        public void Load_OutOfRangeValues_AreClamped()
        {
            _storage.Json = "{\"Controls\":{\"Sensitivity\":999,\"InvertY\":false,\"FieldOfView\":1}}";

            _settings.Load();

            Assert.AreEqual(5f, _settings.Controls.Sensitivity);
            Assert.AreEqual(55f, _settings.Controls.FieldOfView);
        }

        [Test]
        public void ResetControls_RestoresDefaults()
        {
            _settings.Controls.Load(new ControlSettingsData(3f, true, 95f));

            _settings.ResetControls();

            Assert.AreEqual(1f, _settings.Controls.Sensitivity);
            Assert.IsFalse(_settings.Controls.InvertY);
            Assert.AreEqual(70f, _settings.Controls.FieldOfView);
        }
    }
}
