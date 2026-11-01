using UnityEngine;

namespace Game.Core.Player
{
    /// <summary>
    /// Углы взгляда от первого лица. Yaw - поворот тела по горизонтали (0..360),
    /// Pitch - наклон головы (отрицательный = вверх, как у оси X в Unity), ограничен.
    /// </summary>
    public sealed class LookState
    {
        private const float FullTurn = 360f;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public LookState(float yaw = 0f, float pitch = 0f)
        {
            Yaw = Mathf.Repeat(yaw, FullTurn);
            Pitch = pitch;
        }

        /// <param name="delta">Смещение указателя: x вправо, y вверх.</param>
        /// <param name="sensitivity">Градусов на единицу смещения.</param>
        public void Apply(Vector2 delta, float sensitivity, float minPitch, float maxPitch)
        {
            Yaw = Mathf.Repeat(Yaw + delta.x * sensitivity, FullTurn);
            Pitch = Mathf.Clamp(Pitch - delta.y * sensitivity, minPitch, maxPitch);
        }
    }
}
