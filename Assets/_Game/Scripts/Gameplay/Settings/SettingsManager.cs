using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Settings
{
    /// <summary>
    /// Явный менеджер пользовательских настроек и ввода: один на игру, живёт между сценами.
    /// Создаёт GameInput и GameSettings, загружает сохранение. Остальные берут его через Instance в Awake.
    /// Порядок выполнения ранний, чтобы Instance был готов до Awake игрока и UI.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class SettingsManager : MonoBehaviour
    {
        [SerializeField] private ControlSettingsConfig _config;

        public static SettingsManager Instance { get; private set; }

        public GameInput Input { get; private set; }
        public GameSettings Settings { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_config == null)
            {
                Debug.LogError("У SettingsManager не задан ControlSettingsConfig", this);
                enabled = false;
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Input = new GameInput();
            Settings = new GameSettings(_config.Limits, _config.Defaults, Input, new PlayerPrefsSettingsStorage());
            Settings.Load();
            Input.Enable();
        }

        private void OnApplicationQuit()
        {
            if (Instance == this)
            {
                Settings.Save();
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Instance = null;
            Input.Dispose();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }
    }
}
