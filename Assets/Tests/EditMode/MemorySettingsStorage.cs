using Game.Gameplay.Settings;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Хранилище настроек в памяти - для тестов вместо PlayerPrefs.
    /// </summary>
    public sealed class MemorySettingsStorage : ISettingsStorage
    {
        public string Json = string.Empty;

        public string Load() => Json;

        public void Save(string json) => Json = json;
    }
}
