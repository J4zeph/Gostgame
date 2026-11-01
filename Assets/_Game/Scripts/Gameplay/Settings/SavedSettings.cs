using System;
using Game.Core.Settings;

namespace Game.Gameplay.Settings
{
    /// <summary>
    /// Формат сохранения настроек (публичные поля - для JsonUtility).
    /// </summary>
    [Serializable]
    public sealed class SavedSettings
    {
        public ControlSettingsData Controls;
        /// <summary>JSON переопределений клавиш из Input System; пусто - клавиши по умолчанию.</summary>
        public string BindingOverrides = string.Empty;
    }
}
