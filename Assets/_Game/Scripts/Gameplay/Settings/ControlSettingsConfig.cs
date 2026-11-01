using Game.Core.Settings;
using UnityEngine;

namespace Game.Gameplay.Settings
{
    /// <summary>
    /// Значения по умолчанию и диапазоны настроек управления. Ассет: _Game/Data/Settings/ControlSettingsConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "Gostgame/Control Settings Config", fileName = "ControlSettingsConfig")]
    public sealed class ControlSettingsConfig : ScriptableObject
    {
        [Header("Чувствительность мыши (множитель к PlayerSettings)")]
        [SerializeField, Min(0.01f)] private float _defaultSensitivity = 1f;
        [SerializeField, Min(0.01f)] private float _minSensitivity = 0.1f;
        [SerializeField, Min(0.01f)] private float _maxSensitivity = 5f;

        [Header("Инверсия")]
        [SerializeField] private bool _defaultInvertY;

        [Header("Угол обзора, градусы (вертикальный)")]
        [SerializeField, Range(1f, 179f)] private float _defaultFieldOfView = 70f;
        [SerializeField, Range(1f, 179f)] private float _minFieldOfView = 55f;
        [SerializeField, Range(1f, 179f)] private float _maxFieldOfView = 100f;

        public ControlSettingsLimits Limits =>
            new ControlSettingsLimits(_minSensitivity, _maxSensitivity, _minFieldOfView, _maxFieldOfView);

        public ControlSettingsData Defaults =>
            new ControlSettingsData(_defaultSensitivity, _defaultInvertY, _defaultFieldOfView);
    }
}
