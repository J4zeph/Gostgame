using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
using Game.Gameplay.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI.Hud
{
    /// <summary>
    /// HUD локального игрока: точка прицела и значок взаимодействия («[E] Открыть»), когда игрок смотрит
    /// на интерактивный объект. Обновляется по событиям, без опроса каждый кадр. Прячется, пока открыто меню.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        private const string RootClass = "hud-root";
        private const string CrosshairClass = "hud-crosshair";
        private const string CrosshairActiveClass = "hud-crosshair--active";
        private const string PromptClass = "hud-prompt";
        private const string PromptKeyClass = "hud-prompt__key";
        private const string PromptActionClass = "hud-prompt__action";

        [SerializeField] private StyleSheet _styleSheet;

        private UIDocument _document;
        private GameInput _input;
        private PlayerInteractor _interactor;
        private VisualElement _crosshair;
        private VisualElement _prompt;
        private Label _key;
        private Label _action;
        private bool _hasPlayer;
        private bool _menuOpen;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            var manager = SettingsManager.Instance;
            if (manager != null)
            {
                _input = manager.Input;
            }
        }

        private void OnEnable()
        {
            Build();
            LocalPlayer.Changed += OnLocalPlayerChanged;
            OnLocalPlayerChanged(LocalPlayer.Current);

            if (_input != null)
            {
                _input.BindingsChanged += Refresh;
                _input.MenuStateChanged += OnMenuStateChanged;
                OnMenuStateChanged(_input.IsMenuOpen);
            }
        }

        private void OnDisable()
        {
            LocalPlayer.Changed -= OnLocalPlayerChanged;
            Attach(null);

            if (_input != null)
            {
                _input.BindingsChanged -= Refresh;
                _input.MenuStateChanged -= OnMenuStateChanged;
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

            root.AddToClassList(RootClass);
            root.pickingMode = PickingMode.Ignore;

            _crosshair = new VisualElement { pickingMode = PickingMode.Ignore };
            _crosshair.AddToClassList(CrosshairClass);
            root.Add(_crosshair);

            _prompt = new VisualElement { pickingMode = PickingMode.Ignore };
            _prompt.AddToClassList(PromptClass);
            _key = new Label { pickingMode = PickingMode.Ignore };
            _key.AddToClassList(PromptKeyClass);
            _action = new Label { pickingMode = PickingMode.Ignore };
            _action.AddToClassList(PromptActionClass);
            _prompt.Add(_key);
            _prompt.Add(_action);
            root.Add(_prompt);

            Refresh();
        }

        private void OnLocalPlayerChanged(PlayerController player)
        {
            _hasPlayer = player != null;
            Attach(_hasPlayer ? player.GetComponent<PlayerInteractor>() : null);
            UpdateVisibility();
        }

        private void Attach(PlayerInteractor interactor)
        {
            if (_interactor != null)
            {
                _interactor.FocusChanged -= OnFocusChanged;
            }

            _interactor = interactor;
            if (_interactor != null)
            {
                _interactor.FocusChanged += OnFocusChanged;
            }

            Refresh();
        }

        private void OnFocusChanged(IInteractable _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_crosshair == null)
            {
                return;
            }

            var focus = _interactor != null ? _interactor.Focus : null;
            var key = _input != null ? _input.GetBindingDisplay(GameInput.InteractId) : string.Empty;
            var state = InteractionPromptState.For(focus, key);

            _crosshair.EnableInClassList(CrosshairActiveClass, state.Visible);
            _prompt.style.display = state.Visible ? DisplayStyle.Flex : DisplayStyle.None;
            _key.text = state.Key;
            _key.style.display = string.IsNullOrEmpty(state.Key) ? DisplayStyle.None : DisplayStyle.Flex;
            _action.text = state.Action;
        }

        private void OnMenuStateChanged(bool menuOpen)
        {
            _menuOpen = menuOpen;
            UpdateVisibility();
        }

        // Прицел нужен только когда есть свой игрок и не открыто меню.
        private void UpdateVisibility()
        {
            var visible = _hasPlayer && !_menuOpen;
            _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
