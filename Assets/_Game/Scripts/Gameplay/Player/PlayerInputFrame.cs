using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Ввод игрока за один кадр. *Pressed - нажатие именно в этом кадре.
    /// </summary>
    public readonly struct PlayerInputFrame
    {
        public readonly Vector2 Move;
        public readonly Vector2 Look;
        public readonly bool Sprint;
        public readonly bool InteractPressed;
        public readonly bool FlashlightPressed;

        public PlayerInputFrame(Vector2 move, Vector2 look, bool sprint, bool interactPressed, bool flashlightPressed)
        {
            Move = move;
            Look = look;
            Sprint = sprint;
            InteractPressed = interactPressed;
            FlashlightPressed = flashlightPressed;
        }
    }
}
