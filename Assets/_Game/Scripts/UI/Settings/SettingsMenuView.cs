using System.Collections.Generic;
using Game.Core.Session;
using Game.Gameplay.Player;
using Game.Gameplay.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI.Settings
{
    /// <summary>
    /// Меню настроек по Esc: чувствительность мыши, инверсия Y, угол обзора, переназначение клавиш.
    /// Пока меню открыто, игрок не управляется. Настройки сохраняются при закрытии и после смены клавиши.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsMenuView : MonoBehaviour
    {
        private const string OverlayClass = "menu-overlay";
        private const string PanelClass = "menu-panel";
        private const string TitleClass = "menu-title";
        private const string SectionClass = "menu-section";
        private const string RowClass = "menu-row";
        private const string BindingClass = "menu-binding";
        private const string BindingWaitingClass = "menu-binding--waiting";
        private const string HintClass = "menu-hint";
        private const string ButtonsClass = "menu-buttons";
        private const string FooterClass = "menu-footer";

        [SerializeField] private StyleSheet _styleSheet;

        private readonly List<BindingRow> _bindingRows = new List<BindingRow>();
        private UIDocument _document;
        private SettingsManager _manager;
        private VisualElement _overlay;
        private Slider _sensitivity;
        private Toggle _invertY;
        private Slider _fieldOfView;
        private Label _hint;
        private Button _leaveButton;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _manager = SettingsManager.Instance;
        }

        private void OnEnable()
        {
            if (_manager == null || _manager.Input == null)
            {
                Debug.LogError("SettingsMenuView: нет SettingsManager в сцене", this);
                enabled = false;
                return;
            }

            Build();
            SetVisible(_manager.Input.IsMenuOpen);
            _manager.Input.MenuToggleRequested += Toggle;
            _manager.Input.BindingsChanged += RefreshBindings;
            _manager.Settings.Controls.Changed += RefreshControls;
        }

        private void OnDisable()
        {
            if (_manager == null || _manager.Input == null)
            {
                return;
            }

            _manager.Input.MenuToggleRequested -= Toggle;
            _manager.Input.BindingsChanged -= RefreshBindings;
            _manager.Settings.Controls.Changed -= RefreshControls;
        }

        private void Toggle()
        {
            SetOpen(!_manager.Input.IsMenuOpen);
        }

        private void SetOpen(bool open)
        {
            _manager.Input.SetMenuOpen(open);
            SetVisible(open);
            if (!open)
            {
                _manager.Settings.Save();
            }
        }

        private void SetVisible(bool visible)
        {
            _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                RefreshControls();
                RefreshBindings();
                _hint.style.display = DisplayStyle.None;
                var session = SessionRegistry.Current;
                var inSession = session != null && (session.State == SessionState.Host || session.State == SessionState.Client);
                _leaveButton.style.display = inSession ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void Build()
        {
            var root = _document.rootVisualElement;
            root.Clear();
            if (_styleSheet != null)
            {
                root.styleSheets.Add(_styleSheet);
            }

            var controls = _manager.Settings.Controls;
            var limits = controls.Limits;

            _overlay = new VisualElement();
            _overlay.AddToClassList(OverlayClass);
            root.Add(_overlay);

            var panel = new ScrollView(ScrollViewMode.Vertical);
            panel.AddToClassList(PanelClass);
            _overlay.Add(panel);

            panel.Add(MakeLabel("Настройки", TitleClass));

            panel.Add(MakeLabel("Мышь и камера", SectionClass));
            _sensitivity = new Slider("Чувствительность мыши", limits.MinSensitivity, limits.MaxSensitivity) { showInputField = true };
            _sensitivity.RegisterValueChangedCallback(evt => controls.SetSensitivity(evt.newValue));
            panel.Add(_sensitivity);

            _invertY = new Toggle("Инвертировать ось Y");
            _invertY.RegisterValueChangedCallback(evt => controls.SetInvertY(evt.newValue));
            panel.Add(_invertY);

            _fieldOfView = new Slider("Угол обзора", limits.MinFieldOfView, limits.MaxFieldOfView) { showInputField = true };
            _fieldOfView.RegisterValueChangedCallback(evt => controls.SetFieldOfView(evt.newValue));
            panel.Add(_fieldOfView);

            panel.Add(MakeLabel("Клавиши", SectionClass));
            _bindingRows.Clear();
            foreach (var binding in _manager.Input.Bindings)
            {
                var row = new VisualElement();
                row.AddToClassList(RowClass);
                row.Add(new Label(binding.Label));

                var button = new Button();
                button.AddToClassList(BindingClass);
                var id = binding.Id;
                button.clicked += () => StartRebind(id, button);
                row.Add(button);

                panel.Add(row);
                _bindingRows.Add(new BindingRow(id, button));
            }

            _hint = MakeLabel("Нажмите новую клавишу. Esc - отмена.", HintClass);
            panel.Add(_hint);

            var buttons = new VisualElement();
            buttons.AddToClassList(ButtonsClass);
            buttons.Add(new Button(ResetBindings) { text = "Сбросить клавиши" });
            buttons.Add(new Button(_manager.Settings.ResetControls) { text = "Сбросить мышь и камеру" });
            _leaveButton = new Button(LeaveSession) { text = "Отключиться" };
            buttons.Add(_leaveButton);
            buttons.Add(new Button(() => SetOpen(false)) { text = "Закрыть" });
            panel.Add(buttons);

            panel.Add(MakeLabel("Esc - закрыть меню", FooterClass));
        }

        private void StartRebind(string id, Button button)
        {
            if (_manager.Input.IsRebinding)
            {
                return;
            }

            var started = _manager.Input.StartRebind(id, success =>
            {
                button.RemoveFromClassList(BindingWaitingClass);
                _hint.style.display = DisplayStyle.None;
                RefreshBindings();
                if (success)
                {
                    _manager.Settings.Save();
                }
            });

            if (started)
            {
                button.text = "...";
                button.AddToClassList(BindingWaitingClass);
                _hint.style.display = DisplayStyle.Flex;
            }
        }

        private void LeaveSession()
        {
            SetOpen(false);
            var session = SessionRegistry.Current;
            if (session != null)
            {
                session.Leave();
            }
        }

        private void ResetBindings()
        {
            _manager.Input.ResetBindings();
            _manager.Settings.Save();
        }

        private void RefreshControls()
        {
            var controls = _manager.Settings.Controls;
            _sensitivity.SetValueWithoutNotify(controls.Sensitivity);
            _invertY.SetValueWithoutNotify(controls.InvertY);
            _fieldOfView.SetValueWithoutNotify(controls.FieldOfView);
        }

        private void RefreshBindings()
        {
            foreach (var row in _bindingRows)
            {
                row.Button.text = _manager.Input.GetBindingDisplay(row.Id);
            }
        }

        private static Label MakeLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private readonly struct BindingRow
        {
            public readonly string Id;
            public readonly Button Button;

            public BindingRow(string id, Button button)
            {
                Id = id;
                Button = button;
            }
        }
    }
}
