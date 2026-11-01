using Game.Core.Settings;
using Game.Gameplay.Settings;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Применяет угол обзора из пользовательских настроек к камере игрока.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerCameraFov : MonoBehaviour
    {
        private Camera _camera;
        private ControlSettings _controls;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            var manager = SettingsManager.Instance;
            if (manager != null && manager.Settings != null)
            {
                _controls = manager.Settings.Controls;
            }
        }

        private void OnEnable()
        {
            if (_controls == null)
            {
                return;
            }

            _controls.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            if (_controls != null)
            {
                _controls.Changed -= Apply;
            }
        }

        private void Apply()
        {
            _camera.fieldOfView = _controls.FieldOfView;
        }
    }
}
