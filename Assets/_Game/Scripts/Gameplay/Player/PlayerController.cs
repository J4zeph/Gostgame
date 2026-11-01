using System;
using Game.Core.Player;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Персонаж от первого лица: читает ввод раз в кадр, поворачивает тело и голову, двигает CharacterController,
    /// а нажатия действий раздаёт событиями (фонарик, взаимодействие подписываются сами).
    /// Без источника ввода стоит на месте - так будут вести себя чужие игроки до сетевого слоя.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerSettings _settings;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField, Tooltip("Компонент, реализующий IPlayerInputSource (обычно PlayerInputReader)")]
        private MonoBehaviour _inputSourceBehaviour;

        private CharacterController _controller;
        private IPlayerInputSource _input;
        private LookState _look;
        private float _verticalVelocity;

        public event Action InteractRequested;
        public event Action FlashlightToggleRequested;

        public PlayerSettings Settings => _settings;
        public Transform CameraPivot => _cameraPivot;
        public float Yaw => _look.Yaw;
        public float Pitch => _look.Pitch;

        /// <summary>Для сборки из кода (тесты, editor-скрипты). Вызывать до активации объекта.</summary>
        public void Init(PlayerSettings settings, Transform cameraPivot)
        {
            _settings = settings;
            _cameraPivot = cameraPivot;
        }

        public void SetInputSource(IPlayerInputSource source)
        {
            _input = source;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            // При высоком FPS шаг за кадр меньше порога по умолчанию (1 мм), и контроллер его отбрасывает.
            _controller.minMoveDistance = 0f;
            _look = new LookState(transform.eulerAngles.y);

            if (_input == null && _inputSourceBehaviour != null)
            {
                _input = _inputSourceBehaviour as IPlayerInputSource;
                if (_input == null)
                {
                    Debug.LogError($"{_inputSourceBehaviour.GetType().Name} не реализует IPlayerInputSource", this);
                }
            }
        }

        private void Update()
        {
            if (_input == null || _settings == null)
            {
                return;
            }

            var frame = _input.ReadFrame();
            ApplyLook(frame.Look);
            ApplyMovement(frame.Move, frame.Sprint, Time.deltaTime);

            if (frame.InteractPressed)
            {
                InteractRequested?.Invoke();
            }

            if (frame.FlashlightPressed)
            {
                FlashlightToggleRequested?.Invoke();
            }
        }

        private void ApplyLook(Vector2 delta)
        {
            _look.Apply(delta, _settings.LookSensitivity, _settings.MinPitch, _settings.MaxPitch);
            transform.rotation = Quaternion.Euler(0f, _look.Yaw, 0f);
            if (_cameraPivot != null)
            {
                _cameraPivot.localRotation = Quaternion.Euler(_look.Pitch, 0f, 0f);
            }
        }

        private void ApplyMovement(Vector2 move, bool sprint, float deltaTime)
        {
            var speed = sprint ? _settings.SprintSpeed : _settings.WalkSpeed;
            var horizontal = MovementCalculator.HorizontalVelocity(move, _look.Yaw, speed);
            _verticalVelocity = MovementCalculator.VerticalVelocity(
                _verticalVelocity, _controller.isGrounded, _settings.Gravity, _settings.GroundedVelocity, deltaTime);

            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * deltaTime);
        }
    }
}
