using Game.UI.Hud;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class InteractionPromptStateTests
    {
        [Test]
        public void NoTarget_IsHidden()
        {
            var state = InteractionPromptState.For(null, "E");

            Assert.IsFalse(state.Visible);
        }

        [Test]
        public void Target_ShowsKeyAndAction()
        {
            var state = InteractionPromptState.For(new FakeInteractable { PromptText = "Открыть" }, "E");

            Assert.IsTrue(state.Visible);
            Assert.AreEqual("E", state.Key);
            Assert.AreEqual("Открыть", state.Action);
        }

        [Test]
        public void NullTexts_BecomeEmpty_NotNull()
        {
            var state = InteractionPromptState.For(new FakeInteractable { PromptText = null }, null);

            Assert.IsTrue(state.Visible);
            Assert.AreEqual(string.Empty, state.Key);
            Assert.AreEqual(string.Empty, state.Action);
        }
    }
}
