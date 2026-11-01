using UnityEngine;

namespace Game.Gameplay.Equipment
{
    /// <summary>
    /// Параметры фонарика. Ассет: _Game/Data/Equipment/FlashlightSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Gostgame/Flashlight Settings", fileName = "FlashlightSettings")]
    public sealed class FlashlightSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _intensity = 3f;
        [SerializeField, Min(0.1f), Tooltip("Дальность, м")] private float _range = 12f;
        [SerializeField, Range(1f, 179f), Tooltip("Угол конуса, градусы")] private float _spotAngle = 55f;
        [SerializeField] private Color _color = new Color(1f, 0.95f, 0.85f);
        [SerializeField] private bool _startOn;

        public float Intensity => _intensity;
        public float Range => _range;
        public float SpotAngle => _spotAngle;
        public Color Color => _color;
        public bool StartOn => _startOn;
    }
}
