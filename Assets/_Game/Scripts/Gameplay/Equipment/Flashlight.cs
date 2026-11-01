using System;
using Game.Core.Equipment;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Equipment
{
    /// <summary>
    /// Фонарик игрока: прожектор, который включается и выключается по запросу владельца.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class Flashlight : MonoBehaviour
    {
        [SerializeField] private FlashlightSettings _settings;
        [SerializeField] private PlayerController _owner;

        private Light _light;
        private FlashlightModel _model;

        public bool IsOn => _model.IsOn;

        /// <summary>Запрос переключения, когда фонарик не переключается сам (HandleToggleLocally = false).</summary>
        public event Action ToggleRequested;

        /// <summary>true - Toggle сразу переключает (одиночная игра, тесты); false - только ToggleRequested, состояние приходит через SetOn.</summary>
        public bool HandleToggleLocally { get; set; } = true;

        /// <summary>Для сборки из кода (тесты, editor-скрипты). Вызывать до активации объекта.</summary>
        public void Init(FlashlightSettings settings, PlayerController owner)
        {
            _settings = settings;
            _owner = owner;
        }

        private void Awake()
        {
            _light = GetComponent<Light>();
            _light.type = LightType.Spot;

            var startOn = false;
            if (_settings != null)
            {
                _light.intensity = _settings.Intensity;
                _light.range = _settings.Range;
                _light.spotAngle = _settings.SpotAngle;
                _light.color = _settings.Color;
                startOn = _settings.StartOn;
            }
            else
            {
                Debug.LogError("У фонарика не задан FlashlightSettings", this);
            }

            _model = new FlashlightModel(startOn);
            _model.Switched += ApplyState;
            ApplyState(_model.IsOn);
        }

        private void OnEnable()
        {
            if (_owner != null)
            {
                _owner.FlashlightToggleRequested += Toggle;
            }
        }

        private void OnDisable()
        {
            if (_owner != null)
            {
                _owner.FlashlightToggleRequested -= Toggle;
            }
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.Switched -= ApplyState;
            }
        }

        public void Toggle()
        {
            if (HandleToggleLocally)
            {
                _model.Toggle();
            }
            else
            {
                ToggleRequested?.Invoke();
            }
        }

        public void SetOn(bool on)
        {
            _model.SetOn(on);
        }

        private void ApplyState(bool on)
        {
            _light.enabled = on;
        }
    }
}
