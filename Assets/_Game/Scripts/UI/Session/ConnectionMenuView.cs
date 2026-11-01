using Game.Core.Session;
using Game.Gameplay.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI.Session
{
    /// <summary>
    /// Меню подключения: «Создать игру» (хост) или «Подключиться» по адресу. Видно, пока не в сессии.
    /// Работает через ISessionService и ничего не знает о сетевой библиотеке.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ConnectionMenuView : MonoBehaviour
    {
        private const string OverlayClass = "menu-overlay";
        private const string PanelClass = "menu-panel";
        private const string TitleClass = "menu-title";
        private const string SectionClass = "menu-section";
        private const string ButtonsClass = "menu-buttons";
        private const string StatusClass = "menu-hint";
        private const string FooterClass = "menu-footer";

        [SerializeField] private StyleSheet _styleSheet;

        private UIDocument _document;
        private ISessionService _session;
        private VisualElement _overlay;
        private TextField _address;
        private Button _hostButton;
        private Button _joinButton;
        private Label _status;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            Build();
            SessionRegistry.Changed += Attach;
            Attach(SessionRegistry.Current);
        }

        private void OnDisable()
        {
            SessionRegistry.Changed -= Attach;
            Attach(null);
        }

        private void Attach(ISessionService session)
        {
            if (_session != null)
            {
                _session.StateChanged -= Refresh;
            }

            _session = session;
            if (_session != null)
            {
                _session.StateChanged += Refresh;
                if (string.IsNullOrEmpty(_address.value))
                {
                    _address.SetValueWithoutNotify(_session.DefaultAddress);
                }
            }

            Refresh();
        }

        private void Build()
        {
            var root = _document.rootVisualElement;
            root.Clear();
            if (_styleSheet != null)
            {
                root.styleSheets.Add(_styleSheet);
            }

            _overlay = new VisualElement();
            _overlay.AddToClassList(OverlayClass);
            root.Add(_overlay);

            var panel = new VisualElement();
            panel.AddToClassList(PanelClass);
            _overlay.Add(panel);

            panel.Add(MakeLabel("Gostgame", TitleClass));
            panel.Add(MakeLabel("Игра по сети", SectionClass));

            _address = new TextField("Адрес хоста");
            panel.Add(_address);

            _status = MakeLabel(string.Empty, StatusClass);
            panel.Add(_status);

            var buttons = new VisualElement();
            buttons.AddToClassList(ButtonsClass);
            _hostButton = new Button(OnHostClicked) { text = "Создать игру" };
            _joinButton = new Button(OnJoinClicked) { text = "Подключиться" };
            buttons.Add(_hostButton);
            buttons.Add(_joinButton);
            panel.Add(buttons);

            panel.Add(MakeLabel("Хост: «Создать игру». Остальные вводят IP хоста и жмут «Подключиться». На этом же компьютере - 127.0.0.1.", FooterClass));
        }

        private void OnHostClicked()
        {
            if (_session != null)
            {
                _session.StartHost();
            }
        }

        private void OnJoinClicked()
        {
            if (_session != null)
            {
                _session.StartClient(_address.value);
            }
        }

        private void Refresh()
        {
            if (_overlay == null)
            {
                return;
            }

            var state = _session != null ? _session.State : SessionState.Offline;
            var visible = state == SessionState.Offline || state == SessionState.Starting;
            _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            var canStart = _session != null && state == SessionState.Offline;
            _hostButton.SetEnabled(canStart);
            _joinButton.SetEnabled(canStart);
            _address.SetEnabled(canStart);

            _status.text = _session == null ? "Сетевой слой не найден в сцене"
                : state == SessionState.Starting ? "Подключение..."
                : _session.LastError;
            _status.style.display = string.IsNullOrEmpty(_status.text) ? DisplayStyle.None : DisplayStyle.Flex;

            if (visible)
            {
                GameInput.SetCursorCaptured(false);
            }
        }

        private static Label MakeLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }
    }
}
