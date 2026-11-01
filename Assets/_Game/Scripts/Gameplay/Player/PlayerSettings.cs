using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Параметры игрока. Значения по умолчанию - стартовые; настраиваются в ассете _Game/Data/Player/PlayerSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Gostgame/Player Settings", fileName = "PlayerSettings")]
    public sealed class PlayerSettings : ScriptableObject
    {
        [Header("Движение, м/с")]
        [SerializeField, Min(0f)] private float _walkSpeed = 2.2f;
        [SerializeField, Min(0f)] private float _sprintSpeed = 3.6f;

        [Header("Гравитация, м/с²")]
        [SerializeField, Tooltip("Отрицательное - вниз")] private float _gravity = -15f;
        [SerializeField, Tooltip("Скорость прижатия к земле, отрицательная")] private float _groundedVelocity = -2f;

        [Header("Взгляд")]
        [SerializeField, Min(0f), Tooltip("Градусов на единицу смещения мыши")] private float _lookSensitivity = 0.1f;
        [SerializeField, Range(-90f, 0f)] private float _minPitch = -85f;
        [SerializeField, Range(0f, 90f)] private float _maxPitch = 85f;

        [Header("Тело, м")]
        [SerializeField, Min(0.1f)] private float _height = 1.8f;
        [SerializeField, Min(0.05f)] private float _radius = 0.3f;
        [SerializeField, Min(0.1f)] private float _eyeHeight = 1.65f;

        [Header("Взаимодействие, м")]
        [SerializeField, Min(0.1f)] private float _interactDistance = 2f;
        [SerializeField, Min(0f), Tooltip("Если точный луч прицела не попал в интерактивный объект - ищем его в этом радиусе вокруг луча")]
        private float _interactAssistRadius = 0.15f;

        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float Gravity => _gravity;
        public float GroundedVelocity => _groundedVelocity;
        public float LookSensitivity => _lookSensitivity;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
        public float Height => _height;
        public float Radius => _radius;
        public float EyeHeight => _eyeHeight;
        public float InteractDistance => _interactDistance;
        public float InteractAssistRadius => _interactAssistRadius;
    }
}
