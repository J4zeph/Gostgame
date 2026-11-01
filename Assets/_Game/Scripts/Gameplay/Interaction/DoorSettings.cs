using UnityEngine;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Параметры двери. Ассет: _Game/Data/Interaction/DoorSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Gostgame/Door Settings", fileName = "DoorSettings")]
    public sealed class DoorSettings : ScriptableObject
    {
        [SerializeField, Range(-180f, 180f), Tooltip("Угол открытия, градусы. Отрицательный - от петли наружу (в сторону +Z двери)")]
        private float _openAngle = -100f;

        [SerializeField, Min(1f), Tooltip("Скорость поворота, градусов в секунду")]
        private float _speed = 180f;

        public float OpenAngle => _openAngle;
        public float Speed => _speed;
    }
}
