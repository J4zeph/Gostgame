namespace Game.Gameplay.Settings
{
    /// <summary>
    /// Где хранятся пользовательские настройки (строка JSON). В игре - PlayerPrefs, в тестах - память.
    /// </summary>
    public interface ISettingsStorage
    {
        /// <returns>Сохранённый JSON или пустая строка, если сохранений нет.</returns>
        string Load();

        void Save(string json);
    }
}
