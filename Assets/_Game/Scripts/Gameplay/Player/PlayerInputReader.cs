using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Локальный ввод с клавиатуры и мыши через Input System. Единственное место в Gameplay,
    /// которое знает об Input System. Захватывает курсор; Esc отпускает, клик мышью - снова захватывает.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputSource
    {
        private const string MoveUp = "<Keyboard>/w";
        private const string MoveDown = "<Keyboard>/s";
        private const string MoveLeft = "<Keyboard>/a";
        private const string MoveRight = "<Keyboard>/d";
        private const string LookBinding = "<Mouse>/delta";
        private const string SprintBinding = "<Keyboard>/leftShift";
        private const string InteractBinding = "<Keyboard>/e";
        private const string FlashlightBinding = "<Keyboard>/f";
        private const string ReleaseCursorBinding = "<Keyboard>/escape";
        private const string CaptureCursorBinding = "<Mouse>/leftButton";

        private InputAction _move;
        private InputAction _look;
        private InputAction _sprint;
        private InputAction _interact;
        private InputAction _flashlight;
        private InputAction _releaseCursor;
        private InputAction _captureCursor;

        private void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", MoveUp)
                .With("Down", MoveDown)
                .With("Left", MoveLeft)
                .With("Right", MoveRight);

            _look = new InputAction("Look", InputActionType.Value, LookBinding);
            _sprint = new InputAction("Sprint", InputActionType.Button, SprintBinding);
            _interact = new InputAction("Interact", InputActionType.Button, InteractBinding);
            _flashlight = new InputAction("Flashlight", InputActionType.Button, FlashlightBinding);
            _releaseCursor = new InputAction("ReleaseCursor", InputActionType.Button, ReleaseCursorBinding);
            _captureCursor = new InputAction("CaptureCursor", InputActionType.Button, CaptureCursorBinding);
        }

        private void OnEnable()
        {
            _move.Enable();
            _look.Enable();
            _sprint.Enable();
            _interact.Enable();
            _flashlight.Enable();
            _releaseCursor.Enable();
            _captureCursor.Enable();
            _releaseCursor.performed += OnReleaseCursor;
            _captureCursor.performed += OnCaptureCursor;
            SetCursorCaptured(true);
        }

        private void OnDisable()
        {
            _releaseCursor.performed -= OnReleaseCursor;
            _captureCursor.performed -= OnCaptureCursor;
            _move.Disable();
            _look.Disable();
            _sprint.Disable();
            _interact.Disable();
            _flashlight.Disable();
            _releaseCursor.Disable();
            _captureCursor.Disable();
            SetCursorCaptured(false);
        }

        private void OnDestroy()
        {
            _move.Dispose();
            _look.Dispose();
            _sprint.Dispose();
            _interact.Dispose();
            _flashlight.Dispose();
            _releaseCursor.Dispose();
            _captureCursor.Dispose();
        }

        public PlayerInputFrame ReadFrame()
        {
            // Пока курсор отпущен, мышь не крутит камеру и клики не уходят в игру.
            var captured = Cursor.lockState == CursorLockMode.Locked;
            return new PlayerInputFrame(
                _move.ReadValue<Vector2>(),
                captured ? _look.ReadValue<Vector2>() : Vector2.zero,
                _sprint.IsPressed(),
                _interact.WasPressedThisFrame(),
                _flashlight.WasPressedThisFrame());
        }

        private void OnReleaseCursor(InputAction.CallbackContext _)
        {
            SetCursorCaptured(false);
        }

        private void OnCaptureCursor(InputAction.CallbackContext _)
        {
            SetCursorCaptured(true);
        }

        private static void SetCursorCaptured(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}
