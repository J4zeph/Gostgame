using System;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// Луч из точки взгляда: у локального игрока каждый кадр определяет, на какой IInteractable он смотрит
    /// (для значка в HUD), и по запросу игрока вызывает у него Interact.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _viewPoint;
        [SerializeField] private LayerMask _mask = ~0;

        private IInteractable _focus;
        private Collider _focusCollider;

        /// <summary>На что сейчас смотрит игрок (null - ни на что). Только для локального игрока.</summary>
        public IInteractable Focus => _focus;

        /// <summary>Сменился объект в фокусе или его подпись.</summary>
        public event Action<IInteractable> FocusChanged;

        /// <summary>Для сборки из кода (тесты, editor-скрипты). Вызывать до активации объекта.</summary>
        public void Init(PlayerController player, Transform viewPoint)
        {
            _player = player;
            _viewPoint = viewPoint;
        }

        private void OnEnable()
        {
            if (_player != null)
            {
                _player.InteractRequested += OnInteractRequested;
            }
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.InteractRequested -= OnInteractRequested;
            }

            _focusCollider = null;
            SetFocus(null);
        }

        private void Update()
        {
            Collider hitCollider = null;
            if (LocalPlayer.Current == _player && TryProbe(out var hit))
            {
                hitCollider = hit.collider;
            }

            // Компонент ищем только когда луч упёрся в другой коллайдер, а не каждый кадр.
            if (hitCollider == _focusCollider)
            {
                return;
            }

            _focusCollider = hitCollider;
            SetFocus(hitCollider != null ? hitCollider.GetComponentInParent<IInteractable>() : null);
        }

        /// <returns>true, если нашёлся объект и с ним провзаимодействовали.</returns>
        public bool TryInteract()
        {
            if (!TryProbe(out var hit))
            {
                return false;
            }

            var target = hit.collider.GetComponentInParent<IInteractable>();
            if (target == null)
            {
                return false;
            }

            target.Interact(_player.gameObject);
            return true;
        }

        private bool TryProbe(out RaycastHit hit)
        {
            if (_player == null || _viewPoint == null || _player.Settings == null)
            {
                hit = default;
                return false;
            }

            var ray = new Ray(_viewPoint.position, _viewPoint.forward);
            return Physics.Raycast(ray, out hit, _player.Settings.InteractDistance, _mask, QueryTriggerInteraction.Ignore);
        }

        private void SetFocus(IInteractable next)
        {
            if (ReferenceEquals(next, _focus))
            {
                return;
            }

            if (_focus != null)
            {
                _focus.PromptChanged -= OnFocusPromptChanged;
            }

            _focus = next;
            if (_focus != null)
            {
                _focus.PromptChanged += OnFocusPromptChanged;
            }

            FocusChanged?.Invoke(_focus);
        }

        private void OnFocusPromptChanged()
        {
            FocusChanged?.Invoke(_focus);
        }

        private void OnInteractRequested()
        {
            TryInteract();
        }
    }
}
