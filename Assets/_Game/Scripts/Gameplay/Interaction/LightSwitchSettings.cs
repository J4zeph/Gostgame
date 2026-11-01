using UnityEngine;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Параметры выключателя и ламп, которыми он управляет. Ассет: _Game/Data/Interaction/LightSwitchSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Gostgame/Light Switch Settings", fileName = "LightSwitchSettings")]
    public sealed class LightSwitchSettings : ScriptableObject
    {
        [SerializeField, Tooltip("Горит ли свет в начале раунда")]
        private bool _startOn;

        [Header("Лампа")]
        [SerializeField, Min(0f)] private float _lampIntensity = 1.5f;
        [SerializeField, Min(0.1f), Tooltip("Дальность света лампы, м")] private float _lampRange = 7f;
        [SerializeField] private Color _lampColor = new Color(1f, 0.9f, 0.75f);

        [Header("Подписи у значка взаимодействия")]
        [SerializeField] private string _turnOnPrompt = "Включить свет";
        [SerializeField] private string _turnOffPrompt = "Выключить свет";

        public bool StartOn => _startOn;
        public float LampIntensity => _lampIntensity;
        public float LampRange => _lampRange;
        public Color LampColor => _lampColor;
        public string TurnOnPrompt => _turnOnPrompt;
        public string TurnOffPrompt => _turnOffPrompt;
    }
}
