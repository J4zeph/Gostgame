using System;
using System.Collections.Generic;
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

        private readonly Dictionary<Collider, IInteractable> _lookup = new Dictionary<Collider, IInteractable>();
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
            IInteractable target = null;
            if (LocalPlayer.Current == _player)
            {
                TryProbe(out hitCollider, out target);
            }

            if (hitCollider == _focusCollider)
            {
                return;
            }

            _focusCollider = hitCollider;
            SetFocus(target);
        }

        /// <returns>true, если нашёлся объект и с ним провзаимодействовали.</returns>
        public bool TryInteract()
        {
            if (!TryProbe(out _, out var target))
            {
                return false;
            }

            target.Interact(_player.gameObject);
            return true;
        }

        /// <summary>
        /// Сначала точный луч из центра прицела; если он не попал в интерактивный объект - сфера радиусом
        /// InteractAssistRadius вдоль того же луча, чтобы не приходилось целиться в мелкий выключатель пиксель в пиксель.
        /// </summary>
        /// <param name="hitCollider">Во что упёрся луч (для смены фокуса); может быть не интерактивным.</param>
        private bool TryProbe(out Collider hitCollider, out IInteractable target)
        {
            hitCollider = null;
            target = null;
            if (_player == null || _viewPoint == null || _player.Settings == null)
            {
                return false;
            }

            var ray = new Ray(_viewPoint.position, _viewPoint.forward);
            var distance = _player.Settings.InteractDistance;
            if (Physics.Raycast(ray, out var hit, distance, _mask, QueryTriggerInteraction.Ignore))
            {
                hitCollider = hit.collider;
                target = Resolve(hitCollider);
                if (target != null)
                {
                    return true;
                }
            }

            var radius = _player.Settings.InteractAssistRadius;
            if (radius > 0f
                && Physics.SphereCast(ray, radius, out var near, distance, _mask, QueryTriggerInteraction.Ignore))
            {
                var nearTarget = Resolve(near.collider);
                if (nearTarget != null)
                {
                    hitCollider = near.collider;
                    target = nearTarget;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Интерактивный объект коллайдера; кэшируется, чтобы не искать компонент каждый кадр.</summary>
        private IInteractable Resolve(Collider collider)
        {
            if (!_lookup.TryGetValue(collider, out var target))
            {
                target = collider.GetComponentInParent<IInteractable>();
                _lookup.Add(collider, target);
            }

            return target;
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
