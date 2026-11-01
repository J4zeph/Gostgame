using System;
using Game.Core.Interaction;
using UnityEngine;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Дверь на петле: компонент висит на пивоте петли, полотно - дочерний объект с коллайдером.
    /// Update включается только пока дверь поворачивается.
    /// </summary>
    public sealed class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] private DoorSettings _settings;

        private DoorModel _model;
        private Quaternion _closedRotation;

        /// <summary>Аргумент - новое IsOpen. Сетевой слой подпишется сюда, чтобы разослать состояние.</summary>
        public event Action<bool> StateChanged;
        public event Action PromptChanged;

        public string Prompt => _model == null ? string.Empty : _model.IsOpen ? _settings.ClosePrompt : _settings.OpenPrompt;
        public bool IsOpen => _model.IsOpen;
        public float Angle => _model.Angle;
        public float OpenAngle => _model.OpenAngle;

        /// <summary>Для сборки из кода (тесты, editor-скрипты). Вызывать до активации объекта.</summary>
        public void Init(DoorSettings settings)
        {
            _settings = settings;
        }

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError("У двери не задан DoorSettings", this);
                enabled = false;
                return;
            }

            _closedRotation = transform.localRotation;
            _model = new DoorModel(_settings.OpenAngle, _settings.Speed);
            _model.StateChanged += OnModelStateChanged;
            enabled = false;
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.StateChanged -= OnModelStateChanged;
            }
        }

        public void Interact(GameObject interactor)
        {
            _model.Toggle();
        }

        public void SetOpen(bool open)
        {
            _model.SetOpen(open);
        }

        private void Update()
        {
            _model.Tick(Time.deltaTime);
            transform.localRotation = _closedRotation * Quaternion.Euler(0f, _model.Angle, 0f);

            if (!_model.IsMoving)
            {
                enabled = false;
            }
        }

        private void OnModelStateChanged(bool open)
        {
            enabled = true;
            StateChanged?.Invoke(open);
            PromptChanged?.Invoke();
        }
    }
}
