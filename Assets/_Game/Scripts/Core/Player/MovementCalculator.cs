using UnityEngine;

namespace Game.Core.Player
{
    /// <summary>
    /// Расчёт скорости персонажа от первого лица. Чистые функции, без состояния.
    /// </summary>
    public static class MovementCalculator
    {
        /// <summary>
        /// Горизонтальная скорость в мировых координатах.
        /// Ввод длиннее 1 (диагональ на клавиатуре) нормализуется, чтобы по диагонали не бегать быстрее.
        /// </summary>
        public static Vector3 HorizontalVelocity(Vector2 moveInput, float yawDegrees, float speed)
        {
            var input = Vector2.ClampMagnitude(moveInput, 1f);
            var local = new Vector3(input.x, 0f, input.y);
            return Quaternion.Euler(0f, yawDegrees, 0f) * local * speed;
        }

        /// <summary>
        /// Вертикальная скорость после шага гравитации.
        /// На земле скорость падения сбрасывается до небольшой прижимающей, чтобы персонаж не копил её.
        /// </summary>
        /// <param name="gravity">Ускорение, отрицательное (вниз).</param>
        /// <param name="groundedVelocity">Скорость прижатия к земле, отрицательная.</param>
        public static float VerticalVelocity(float current, bool grounded, float gravity, float groundedVelocity, float deltaTime)
        {
            if (grounded && current <= groundedVelocity)
            {
                return groundedVelocity;
            }

            return current + gravity * deltaTime;
        }
    }
}
