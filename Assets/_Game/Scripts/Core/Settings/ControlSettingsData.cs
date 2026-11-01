using System;

namespace Game.Core.Settings
{
    /// <summary>
    /// Сохраняемые значения настроек управления (публичные поля - для JsonUtility).
    /// </summary>
    [Serializable]
    public struct ControlSettingsData
    {
        /// <summary>Множитель к базовой чувствительности из PlayerSettings.</summary>
        public float Sensitivity;
        public bool InvertY;
        /// <summary>Вертикальный угол обзора камеры, градусы.</summary>
        public float FieldOfView;

        public ControlSettingsData(float sensitivity, bool invertY, float fieldOfView)
        {
            Sensitivity = sensitivity;
            InvertY = invertY;
            FieldOfView = fieldOfView;
        }
    }
}
