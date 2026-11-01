using System;
using Game.Core.Settings;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Settings
{
    /// <summary>
    /// Пользовательские настройки целиком: управление + назначенные клавиши, загрузка и сохранение.
    /// Битое или неполное сохранение не ломает игру: недостающее берётся по умолчанию.
    /// </summary>
    public sealed class GameSettings
    {
        private readonly ControlSettingsData _defaults;
        private readonly GameInput _input;
        private readonly ISettingsStorage _storage;

        public ControlSettings Controls { get; }

        public GameSettings(ControlSettingsLimits limits, ControlSettingsData defaults, GameInput input, ISettingsStorage storage)
        {
            _defaults = defaults;
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Controls = new ControlSettings(limits, defaults);
        }

        /// <returns>true, если сохранение нашлось и прочиталось.</returns>
        public bool Load()
        {
            var json = _storage.Load();
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            // Заполняем значениями по умолчанию и поверх кладём то, что есть в JSON.
            var saved = new SavedSettings { Controls = _defaults };
            try
            {
                JsonUtility.FromJsonOverwrite(json, saved);
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"[GameSettings] Сохранение настроек повреждено, используются значения по умолчанию: {e.Message}");
                return false;
            }

            Controls.Load(saved.Controls);
            try
            {
                _input.LoadOverrides(saved.BindingOverrides);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameSettings] Не удалось применить назначенные клавиши, сброс на стандартные: {e.Message}");
                _input.ResetBindings();
            }

            return true;
        }

        public void Save()
        {
            var saved = new SavedSettings
            {
                Controls = Controls.ToData(),
                BindingOverrides = _input.SaveOverrides(),
            };
            _storage.Save(JsonUtility.ToJson(saved));
        }

        public void ResetControls()
        {
            Controls.Load(_defaults);
        }
    }
}
