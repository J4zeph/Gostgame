using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Камера сцены, пока нет локального игрока (меню подключения). Как только игрок появился - выключается.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FallbackCamera : MonoBehaviour
    {
        private Camera _camera;
        private AudioListener _listener;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _listener = GetComponent<AudioListener>();
        }

        private void OnEnable()
        {
            LocalPlayer.Changed += OnLocalPlayerChanged;
            OnLocalPlayerChanged(LocalPlayer.Current);
        }

        private void OnDisable()
        {
            LocalPlayer.Changed -= OnLocalPlayerChanged;
        }

        private void OnLocalPlayerChanged(PlayerController player)
        {
            var active = player == null;
            _camera.enabled = active;
            if (_listener != null)
            {
                _listener.enabled = active;
            }
        }
    }
}
