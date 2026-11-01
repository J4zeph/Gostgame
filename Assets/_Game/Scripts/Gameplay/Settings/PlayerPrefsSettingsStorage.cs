using UnityEngine;

namespace Game.Gameplay.Settings
{
    /// <summary>
    /// Настройки в PlayerPrefs (на Windows - реестр текущего пользователя).
    /// </summary>
    public sealed class PlayerPrefsSettingsStorage : ISettingsStorage
    {
        private const string Key = "Gostgame.Settings.v1";

        public string Load()
        {
            return PlayerPrefs.GetString(Key, string.Empty);
        }

        public void Save(string json)
        {
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
    }
}
