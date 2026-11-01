using Game.Gameplay.Player;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class GameInputTests
    {
        private GameInput _input;

        [SetUp]
        public void SetUp()
        {
            _input = new GameInput();
            _input.Enable();
        }

        [TearDown]
        public void TearDown()
        {
            _input.Dispose();
        }

        [Test]
        public void Bindings_ListAllRebindableActions_WithDisplayNames()
        {
            Assert.AreEqual(7, _input.Bindings.Count);
            foreach (var binding in _input.Bindings)
            {
                Assert.IsNotEmpty(binding.Label, binding.Id);
                Assert.IsNotEmpty(_input.GetBindingDisplay(binding.Id), binding.Id);
            }
        }

        [Test]
        public void BindingDisplay_IsEnglishKeyName_RegardlessOfLayout()
        {
            Assert.AreEqual("E", _input.GetBindingDisplay(GameInput.InteractId));
            Assert.AreEqual("W", _input.GetBindingDisplay(GameInput.MoveForwardId));
            Assert.AreEqual("F", _input.GetBindingDisplay(GameInput.FlashlightId));

            _input.OverrideBinding(GameInput.InteractId, "<Mouse>/rightButton");
            Assert.AreEqual("Right Button", _input.GetBindingDisplay(GameInput.InteractId));
        }

        [Test]
        public void OverrideBinding_ChangesDisplay_AndResetRestoresIt()
        {
            var original = _input.GetBindingDisplay(GameInput.InteractId);
            var changes = 0;
            _input.BindingsChanged += () => changes++;

            _input.OverrideBinding(GameInput.InteractId, "<Keyboard>/q");
            Assert.AreNotEqual(original, _input.GetBindingDisplay(GameInput.InteractId));

            _input.ResetBindings();
            Assert.AreEqual(original, _input.GetBindingDisplay(GameInput.InteractId));
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void OverrideCompositePart_ChangesOnlyThatDirection()
        {
            var back = _input.GetBindingDisplay(GameInput.MoveBackId);

            _input.OverrideBinding(GameInput.MoveForwardId, "<Keyboard>/upArrow");

            Assert.AreNotEqual("W", _input.GetBindingDisplay(GameInput.MoveForwardId));
            Assert.AreEqual(back, _input.GetBindingDisplay(GameInput.MoveBackId));
        }

        [Test]
        public void SaveAndLoadOverrides_RoundTrip()
        {
            _input.OverrideBinding(GameInput.FlashlightId, "<Keyboard>/g");
            var json = _input.SaveOverrides();
            var expected = _input.GetBindingDisplay(GameInput.FlashlightId);

            var other = new GameInput();
            try
            {
                other.LoadOverrides(json);
                Assert.AreEqual(expected, other.GetBindingDisplay(GameInput.FlashlightId));
            }
            finally
            {
                other.Dispose();
            }
        }

        [Test]
        public void SaveOverrides_NothingChanged_IsEmpty()
        {
            Assert.AreEqual(string.Empty, _input.SaveOverrides());
        }

        [Test]
        public void LoadOverrides_UnknownIdIgnored_KnownApplied()
        {
            var original = _input.GetBindingDisplay(GameInput.SprintId);

            _input.LoadOverrides("{\"Items\":[{\"Id\":\"removed-action\",\"Path\":\"<Keyboard>/x\"},{\"Id\":\"sprint\",\"Path\":\"<Keyboard>/leftCtrl\"}]}");

            Assert.AreNotEqual(original, _input.GetBindingDisplay(GameInput.SprintId));
        }

        [Test]
        public void MenuOpen_DisablesGameplayActions_AndCloseRestores()
        {
            Assert.IsTrue(_input.IsGameplayInputEnabled);
            bool? reported = null;
            _input.MenuStateChanged += open => reported = open;

            _input.SetMenuOpen(true);
            Assert.IsFalse(_input.IsGameplayInputEnabled);
            Assert.AreEqual(true, reported);

            _input.SetMenuOpen(false);
            Assert.IsTrue(_input.IsGameplayInputEnabled);
            Assert.AreEqual(false, reported);
        }

        [Test]
        public void UnknownBindingId_ReturnsEmptyAndRebindFails()
        {
            Assert.AreEqual(string.Empty, _input.GetBindingDisplay("nope"));
            Assert.IsFalse(_input.StartRebind("nope", null));
        }
    }
}
