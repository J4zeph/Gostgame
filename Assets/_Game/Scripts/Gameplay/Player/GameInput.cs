using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Все действия ввода игры, состояние меню и переназначение клавиш.
    /// Наружу (в UI) отдаёт только строки и события, без типов Input System.
    /// </summary>
    public sealed class GameInput : IDisposable
    {
        public const string MoveForwardId = "move-forward";
        public const string MoveBackId = "move-back";
        public const string MoveLeftId = "move-left";
        public const string MoveRightId = "move-right";
        public const string SprintId = "sprint";
        public const string InteractId = "interact";
        public const string FlashlightId = "flashlight";

        private const string MoveUpBinding = "<Keyboard>/w";
        private const string MoveDownBinding = "<Keyboard>/s";
        private const string MoveLeftBinding = "<Keyboard>/a";
        private const string MoveRightBinding = "<Keyboard>/d";
        private const string LookBinding = "<Mouse>/delta";
        private const string SprintBinding = "<Keyboard>/leftShift";
        private const string InteractBinding = "<Keyboard>/e";
        private const string FlashlightBinding = "<Keyboard>/f";
        private const string MenuBinding = "<Keyboard>/escape";
        private const string CaptureCursorBinding = "<Mouse>/leftButton";
        private const string CancelRebindBinding = "<Keyboard>/escape";
        private const string ExcludeMousePosition = "<Mouse>/position";
        private const string ExcludeMouseDelta = "<Mouse>/delta";
        private const float RebindWaitForAnother = 0.1f;

        // Индексы частей составной привязки 2DVector: [0] - сама составная, дальше Up, Down, Left, Right.
        private const int CompositeUp = 1;
        private const int CompositeDown = 2;
        private const int CompositeLeft = 3;
        private const int CompositeRight = 4;

        private readonly InputActionMap _gameplay;
        private readonly InputActionMap _system;
        private readonly InputAction _menu;
        private readonly InputAction _captureCursor;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<RebindableBinding> _bindings = new List<RebindableBinding>();
        private InputActionRebindingExtensions.RebindingOperation _rebind;
        private bool _enabled;

        public InputAction Move { get; }
        public InputAction Look { get; }
        public InputAction Sprint { get; }
        public InputAction Interact { get; }
        public InputAction Flashlight { get; }

        public bool IsMenuOpen { get; private set; }
        public bool IsRebinding => _rebind != null;
        /// <summary>Принимаются ли игровые действия (ходьба, взгляд, кнопки) - выключены, пока открыто меню.</summary>
        public bool IsGameplayInputEnabled => _gameplay.enabled;
        public IReadOnlyList<RebindableBinding> Bindings => _bindings;

        /// <summary>Нажата клавиша меню (Esc). Открывать/закрывать решает тот, кто показывает меню.</summary>
        public event Action MenuToggleRequested;
        public event Action<bool> MenuStateChanged;
        public event Action BindingsChanged;

        public GameInput()
        {
            _gameplay = new InputActionMap("Gameplay");
            Move = _gameplay.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", MoveUpBinding)
                .With("Down", MoveDownBinding)
                .With("Left", MoveLeftBinding)
                .With("Right", MoveRightBinding);
            Look = _gameplay.AddAction("Look", InputActionType.Value, LookBinding);
            Sprint = _gameplay.AddAction("Sprint", InputActionType.Button, SprintBinding);
            Interact = _gameplay.AddAction("Interact", InputActionType.Button, InteractBinding);
            Flashlight = _gameplay.AddAction("Flashlight", InputActionType.Button, FlashlightBinding);

            _system = new InputActionMap("System");
            _menu = _system.AddAction("Menu", InputActionType.Button, MenuBinding);
            _captureCursor = _system.AddAction("CaptureCursor", InputActionType.Button, CaptureCursorBinding);
            _menu.performed += OnMenu;
            _captureCursor.performed += OnCaptureCursor;

            AddRebindable(MoveForwardId, "Вперёд", Move, CompositeUp);
            AddRebindable(MoveBackId, "Назад", Move, CompositeDown);
            AddRebindable(MoveLeftId, "Влево", Move, CompositeLeft);
            AddRebindable(MoveRightId, "Вправо", Move, CompositeRight);
            AddRebindable(SprintId, "Бег", Sprint, 0);
            AddRebindable(InteractId, "Взаимодействие", Interact, 0);
            AddRebindable(FlashlightId, "Фонарик", Flashlight, 0);
        }

        public void Enable()
        {
            _enabled = true;
            _system.Enable();
            if (!IsMenuOpen)
            {
                _gameplay.Enable();
            }
        }

        public void Disable()
        {
            _enabled = false;
            CancelRebind();
            _system.Disable();
            _gameplay.Disable();
        }

        public void SetMenuOpen(bool open)
        {
            if (open == IsMenuOpen)
            {
                return;
            }

            CancelRebind();
            IsMenuOpen = open;
            if (open)
            {
                _gameplay.Disable();
            }
            else if (_enabled)
            {
                _gameplay.Enable();
            }

            // Курсор захватываем, только если есть свой игрок: без него (меню подключения) курсор нужен.
            SetCursorCaptured(!open && LocalPlayer.Current != null);
            MenuStateChanged?.Invoke(open);
        }

        /// <summary>
        /// Подпись клавиши по-английски (E, Left Shift, Left Button) - по физической клавише,
        /// без перевода на текущую раскладку (иначе с русской раскладкой E показывается как «У»).
        /// </summary>
        public string GetBindingDisplay(string id)
        {
            var entry = Find(id);
            if (entry == null)
            {
                return string.Empty;
            }

            var path = entry.Action.bindings[entry.BindingIndex].effectivePath;
            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        /// <summary>
        /// Ждёт нажатия новой клавиши для привязки id. Esc - отмена. onFinished(true) - назначено.
        /// </summary>
        public bool StartRebind(string id, Action<bool> onFinished)
        {
            var entry = Find(id);
            if (entry == null || IsRebinding)
            {
                return false;
            }

            var wasEnabled = entry.Action.enabled;
            entry.Action.Disable();
            _system.Disable();

            _rebind = entry.Action.PerformInteractiveRebinding(entry.BindingIndex)
                .WithControlsExcluding(ExcludeMousePosition)
                .WithControlsExcluding(ExcludeMouseDelta)
                .WithCancelingThrough(CancelRebindBinding)
                .OnMatchWaitForAnother(RebindWaitForAnother)
                .OnComplete(_ => FinishRebind(entry, wasEnabled, true, onFinished))
                .OnCancel(_ => FinishRebind(entry, wasEnabled, false, onFinished))
                .Start();
            return true;
        }

        public void CancelRebind()
        {
            if (_rebind != null)
            {
                _rebind.Cancel();
            }
        }

        /// <summary>Назначает клавишу программно (тесты, будущие пресеты).</summary>
        public void OverrideBinding(string id, string controlPath)
        {
            var entry = Find(id);
            if (entry == null)
            {
                throw new ArgumentException($"Неизвестная привязка {id}", nameof(id));
            }

            entry.Action.ApplyBindingOverride(entry.BindingIndex, controlPath);
            BindingsChanged?.Invoke();
        }

        public void ResetBindings()
        {
            _gameplay.RemoveAllBindingOverrides();
            BindingsChanged?.Invoke();
        }

        /// <summary>
        /// Переназначенные клавиши в JSON. Свой формат «id привязки -> путь клавиши»: встроенный формат Input System
        /// сопоставляет по внутренним ID привязок, а у действий, созданных в коде, они новые при каждом запуске.
        /// </summary>
        public string SaveOverrides()
        {
            var saved = new SavedOverrides();
            foreach (var entry in _entries)
            {
                var path = entry.Action.bindings[entry.BindingIndex].overridePath;
                if (!string.IsNullOrEmpty(path))
                {
                    saved.Items.Add(new SavedOverride { Id = entry.Id, Path = path });
                }
            }

            return saved.Items.Count == 0 ? string.Empty : JsonUtility.ToJson(saved);
        }

        /// <summary>Применяет сохранённые клавиши. Неизвестные id пропускаются. Битый JSON - ArgumentException.</summary>
        public void LoadOverrides(string json)
        {
            _gameplay.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json))
            {
                var saved = JsonUtility.FromJson<SavedOverrides>(json);
                if (saved != null && saved.Items != null)
                {
                    foreach (var item in saved.Items)
                    {
                        var entry = Find(item.Id);
                        if (entry != null && !string.IsNullOrEmpty(item.Path))
                        {
                            entry.Action.ApplyBindingOverride(entry.BindingIndex, item.Path);
                        }
                    }
                }
            }

            BindingsChanged?.Invoke();
        }

        public static void SetCursorCaptured(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        public void Dispose()
        {
            Disable();
            _menu.performed -= OnMenu;
            _captureCursor.performed -= OnCaptureCursor;
            _gameplay.Dispose();
            _system.Dispose();
        }

        private void FinishRebind(Entry entry, bool wasEnabled, bool success, Action<bool> onFinished)
        {
            _rebind.Dispose();
            _rebind = null;

            if (wasEnabled && !IsMenuOpen)
            {
                entry.Action.Enable();
            }

            if (_enabled)
            {
                _system.Enable();
            }

            if (success)
            {
                BindingsChanged?.Invoke();
            }

            onFinished?.Invoke(success);
        }

        private void OnMenu(InputAction.CallbackContext _)
        {
            MenuToggleRequested?.Invoke();
        }

        private void OnCaptureCursor(InputAction.CallbackContext _)
        {
            if (!IsMenuOpen)
            {
                SetCursorCaptured(true);
            }
        }

        private void AddRebindable(string id, string label, InputAction action, int bindingIndex)
        {
            _entries.Add(new Entry(id, action, bindingIndex));
            _bindings.Add(new RebindableBinding(id, label));
        }

        private Entry Find(string id)
        {
            foreach (var entry in _entries)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }

        [Serializable]
        private sealed class SavedOverrides
        {
            public List<SavedOverride> Items = new List<SavedOverride>();
        }

        [Serializable]
        private sealed class SavedOverride
        {
            public string Id;
            public string Path;
        }

        private sealed class Entry
        {
            public readonly string Id;
            public readonly InputAction Action;
            public readonly int BindingIndex;

            public Entry(string id, InputAction action, int bindingIndex)
            {
                Id = id;
                Action = action;
                BindingIndex = bindingIndex;
            }
        }
    }
}
