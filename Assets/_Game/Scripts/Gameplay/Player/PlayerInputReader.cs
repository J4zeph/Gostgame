using Game.Core.Settings;
using Game.Gameplay.Settings;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Локальный ввод игрока: берёт действия из общего GameInput (SettingsManager),
    /// применяет пользовательскую чувствительность и инверсию, регистрирует игрока как локального.
    /// Пока открыто меню, отдаёт пустой кадр. Без SettingsManager создаёт свой GameInput с клавишами по умолчанию.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputSource
    {
        private PlayerController _player;
        private GameInput _input;
        private ControlSettings _controls;
        private bool _ownsInput;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();

            var manager = SettingsManager.Instance;
            if (manager != null && manager.Input != null)
            {
                _input = manager.Input;
                _controls = manager.Settings.Controls;
            }
            else
            {
                _input = new GameInput();
                _input.Enable();
                _ownsInput = true;
            }
        }

        private void OnEnable()
        {
            LocalPlayer.Set(_player);
            if (!_input.IsMenuOpen)
            {
                GameInput.SetCursorCaptured(true);
            }
        }

        private void OnDisable()
        {
            LocalPlayer.Clear(_player);
            GameInput.SetCursorCaptured(false);
        }

        private void OnDestroy()
        {
            if (_ownsInput)
            {
                _input.Dispose();
            }
        }

        public PlayerInputFrame ReadFrame()
        {
            if (_input.IsMenuOpen)
            {
                return default;
            }

            // Пока курсор не захвачен (переключились в другое окно), мышь не крутит камеру и клики не уходят в игру.
            var captured = Cursor.lockState == CursorLockMode.Locked;
            var rawLook = captured ? _input.Look.ReadValue<Vector2>() : Vector2.zero;
            var look = _controls != null ? _controls.ApplyToLook(rawLook) : rawLook;

            return new PlayerInputFrame(
                _input.Move.ReadValue<Vector2>(),
                look,
                _input.Sprint.IsPressed(),
                captured && _input.Interact.WasPressedThisFrame(),
                _input.Flashlight.WasPressedThisFrame());
        }
    }
}
