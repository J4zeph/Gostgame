using System;
using UnityEngine;

namespace Game.Core.Settings
{
    /// <summary>
    /// Пользовательские настройки управления: чувствительность мыши, инверсия Y, угол обзора.
    /// Значения всегда в пределах лимитов; Changed - только при реальном изменении.
    /// </summary>
    public sealed class ControlSettings
    {
        public ControlSettingsLimits Limits { get; }
        public float Sensitivity { get; private set; }
        public bool InvertY { get; private set; }
        public float FieldOfView { get; private set; }

        public event Action Changed;

        public ControlSettings(ControlSettingsLimits limits, ControlSettingsData initial)
        {
            Limits = limits;
            Sensitivity = ClampSensitivity(initial.Sensitivity);
            InvertY = initial.InvertY;
            FieldOfView = ClampFieldOfView(initial.FieldOfView);
        }

        public void SetSensitivity(float value)
        {
            Set(ClampSensitivity(value), InvertY, FieldOfView);
        }

        public void SetInvertY(bool value)
        {
            Set(Sensitivity, value, FieldOfView);
        }

        public void SetFieldOfView(float value)
        {
            Set(Sensitivity, InvertY, ClampFieldOfView(value));
        }

        /// <summary>Применяет сохранённые значения (с обрезкой по лимитам), событие - не больше одного.</summary>
        public void Load(ControlSettingsData data)
        {
            Set(ClampSensitivity(data.Sensitivity), data.InvertY, ClampFieldOfView(data.FieldOfView));
        }

        public ControlSettingsData ToData()
        {
            return new ControlSettingsData(Sensitivity, InvertY, FieldOfView);
        }

        /// <summary>Сырое смещение мыши -> смещение с учётом чувствительности и инверсии.</summary>
        public Vector2 ApplyToLook(Vector2 raw)
        {
            var y = InvertY ? -raw.y : raw.y;
            return new Vector2(raw.x * Sensitivity, y * Sensitivity);
        }

        private void Set(float sensitivity, bool invertY, float fieldOfView)
        {
            var changed = !Mathf.Approximately(sensitivity, Sensitivity)
                          || invertY != InvertY
                          || !Mathf.Approximately(fieldOfView, FieldOfView);
            if (!changed)
            {
                return;
            }

            Sensitivity = sensitivity;
            InvertY = invertY;
            FieldOfView = fieldOfView;
            Changed?.Invoke();
        }

        private float ClampSensitivity(float value)
        {
            // NaN из битого файла настроек -> минимум, а не NaN в камере.
            return float.IsNaN(value) ? Limits.MinSensitivity : Mathf.Clamp(value, Limits.MinSensitivity, Limits.MaxSensitivity);
        }

        private float ClampFieldOfView(float value)
        {
            return float.IsNaN(value) ? Limits.MinFieldOfView : Mathf.Clamp(value, Limits.MinFieldOfView, Limits.MaxFieldOfView);
        }
    }
}
