using System;

namespace Game.Core.Settings
{
    /// <summary>
    /// Допустимые диапазоны пользовательских настроек управления.
    /// </summary>
    public readonly struct ControlSettingsLimits
    {
        public readonly float MinSensitivity;
        public readonly float MaxSensitivity;
        public readonly float MinFieldOfView;
        public readonly float MaxFieldOfView;

        public ControlSettingsLimits(float minSensitivity, float maxSensitivity, float minFieldOfView, float maxFieldOfView)
        {
            if (minSensitivity <= 0f || minSensitivity > maxSensitivity)
            {
                throw new ArgumentException("Sensitivity range must be positive and ordered.");
            }

            if (minFieldOfView <= 0f || minFieldOfView > maxFieldOfView)
            {
                throw new ArgumentException("Field of view range must be positive and ordered.");
            }

            MinSensitivity = minSensitivity;
            MaxSensitivity = maxSensitivity;
            MinFieldOfView = minFieldOfView;
            MaxFieldOfView = maxFieldOfView;
        }
    }
}
