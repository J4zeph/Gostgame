using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// По запросу игрока бросает луч из точки взгляда и вызывает Interact у первого IInteractable на пути.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _viewPoint;
        [SerializeField] private LayerMask _mask = ~0;

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
        }

        /// <returns>true, если нашёлся объект и с ним провзаимодействовали.</returns>
        public bool TryInteract()
        {
            if (_player == null || _viewPoint == null || _player.Settings == null)
            {
                return false;
            }

            var ray = new Ray(_viewPoint.position, _viewPoint.forward);
            if (!Physics.Raycast(ray, out var hit, _player.Settings.InteractDistance, _mask, QueryTriggerInteraction.Ignore))
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

        private void OnInteractRequested()
        {
            TryInteract();
        }
    }
}
