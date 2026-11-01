using System;
using UnityEngine;

namespace Game.Core.Interaction
{
    /// <summary>
    /// Состояние двери: открыта ли (цель) и текущий угол, который плавно догоняет цель.
    /// IsOpen - то, что синхронизируется по сети; угол каждый клиент досчитывает сам.
    /// </summary>
    public sealed class DoorModel
    {
        public float OpenAngle { get; }
        public float Speed { get; }
        public bool IsOpen { get; private set; }
        public float Angle { get; private set; }
        public float TargetAngle => IsOpen ? OpenAngle : 0f;
        public bool IsMoving => !Mathf.Approximately(Angle, TargetAngle);

        /// <summary>Вызывается при смене цели (открыть/закрыть), аргумент - новое IsOpen.</summary>
        public event Action<bool> StateChanged;

        /// <param name="openAngle">Угол открытия в градусах; знак задаёт сторону.</param>
        /// <param name="speed">Скорость поворота, градусов в секунду.</param>
        public DoorModel(float openAngle, float speed, bool startOpen = false)
        {
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Speed must be positive.");
            }

            OpenAngle = openAngle;
            Speed = speed;
            IsOpen = startOpen;
            Angle = TargetAngle;
        }

        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (open == IsOpen)
            {
                return;
            }

            IsOpen = open;
            StateChanged?.Invoke(open);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Angle = Mathf.MoveTowards(Angle, TargetAngle, Speed * deltaTime);
        }
    }
}
