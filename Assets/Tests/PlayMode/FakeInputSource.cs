using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Ввод для тестов. Move/Look/Sprint держатся, пока их не сменят; нажатия срабатывают на один кадр.
    /// </summary>
    public sealed class FakeInputSource : IPlayerInputSource
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool Sprint;

        private bool _interactPressed;
        private bool _flashlightPressed;

        public void PressInteract() => _interactPressed = true;
        public void PressFlashlight() => _flashlightPressed = true;

        public PlayerInputFrame ReadFrame()
        {
            var frame = new PlayerInputFrame(Move, Look, Sprint, _interactPressed, _flashlightPressed);
            _interactPressed = false;
            _flashlightPressed = false;
            return frame;
        }
    }
}
