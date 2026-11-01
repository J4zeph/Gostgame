using System;
using Game.Core.Interaction;
using UnityEngine;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Выключатель на стене: включает и выключает лампы своей комнаты.
    /// Коллайдер (сама клавиша) - на этом объекте или дочернем.
    /// </summary>
    public sealed class LightSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private LightSwitchSettings _settings;
        [SerializeField] private Light[] _lamps = Array.Empty<Light>();

        private LightSwitchModel _model;

        /// <summary>Аргумент - новое IsOn.</summary>
        public event Action<bool> StateChanged;
        public event Action PromptChanged;

        /// <summary>
        /// Взаимодействие, когда выключатель не обрабатывает его сам (HandleInteractLocally = false):
        /// сетевой слой пересылает запрос хосту, а результат возвращает через SetOn.
        /// </summary>
        public event Action<GameObject> InteractRequested;

        /// <summary>true - Interact сразу переключает свет (одиночная игра, тесты); false - только InteractRequested.</summary>
        public bool HandleInteractLocally { get; set; } = true;

        public string Prompt => _model == null ? string.Empty : _model.IsOn ? _settings.TurnOffPrompt : _settings.TurnOnPrompt;
        public bool IsOn => _model.IsOn;
        public int LampCount => _lamps.Length;

        /// <summary>Для сборки из кода (тесты, editor-скрипты). Вызывать до активации объекта.</summary>
        public void Init(LightSwitchSettings settings, Light[] lamps)
        {
            _settings = settings;
            _lamps = lamps ?? Array.Empty<Light>();
        }

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError("У выключателя не задан LightSwitchSettings", this);
                enabled = false;
                return;
            }

            foreach (var lamp in _lamps)
            {
                if (lamp == null)
                {
                    continue;
                }

                lamp.intensity = _settings.LampIntensity;
                lamp.range = _settings.LampRange;
                lamp.color = _settings.LampColor;
            }

            _model = new LightSwitchModel(_settings.StartOn);
            _model.Switched += OnModelSwitched;
            ApplyLamps(_model.IsOn);
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.Switched -= OnModelSwitched;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (_model == null)
            {
                return;
            }

            if (HandleInteractLocally)
            {
                _model.Toggle();
            }
            else
            {
                InteractRequested?.Invoke(interactor);
            }
        }

        public void SetOn(bool on)
        {
            _model.SetOn(on);
        }

        private void OnModelSwitched(bool on)
        {
            ApplyLamps(on);
            StateChanged?.Invoke(on);
            PromptChanged?.Invoke();
        }

        private void ApplyLamps(bool on)
        {
            foreach (var lamp in _lamps)
            {
                if (lamp != null)
                {
                    lamp.enabled = on;
                }
            }
        }
    }
}
