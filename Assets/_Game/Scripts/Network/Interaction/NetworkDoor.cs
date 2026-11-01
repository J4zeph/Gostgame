using Game.Core.Interaction;
using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
using Game.Network.Session;
using Unity.Netcode;
using UnityEngine;

namespace Game.Network.Interaction
{
    /// <summary>
    /// Синхронизация двери: открыта ли - NetworkVariable с записью только хостом.
    /// Игрок просит хост переключить дверь; хост проверяет, что игрок до неё дотягивается.
    /// Поворот полотна каждый клиент анимирует сам по Door.SetOpen.
    /// </summary>
    public sealed class NetworkDoor : NetworkBehaviour
    {
        [SerializeField] private Door _door;
        [SerializeField] private NetworkSettings _settings;

        private readonly NetworkVariable<bool> _isOpen = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Collider _collider;

        public bool IsOpen => _isOpen.Value;

        /// <summary>Для сборки из кода (тесты). Вызывать до активации объекта.</summary>
        public void Init(Door door, NetworkSettings settings)
        {
            _door = door;
            _settings = settings;
        }

        private void Awake()
        {
            _collider = GetComponentInChildren<Collider>();
        }

        public override void OnNetworkSpawn()
        {
            _door.HandleInteractLocally = false;
            _door.InteractRequested += OnInteractRequested;
            _isOpen.OnValueChanged += OnOpenChanged;
            _door.SetOpen(_isOpen.Value);
        }

        public override void OnNetworkDespawn()
        {
            _door.InteractRequested -= OnInteractRequested;
            _isOpen.OnValueChanged -= OnOpenChanged;
            _door.HandleInteractLocally = true;
        }

        private void OnInteractRequested(GameObject interactor)
        {
            RequestToggleRpc();
        }

        [Rpc(SendTo.Server)]
        private void RequestToggleRpc(RpcParams rpcParams = default)
        {
            if (!CanReach(rpcParams.Receive.SenderClientId))
            {
                return;
            }

            _isOpen.Value = !_isOpen.Value;
        }

        private bool CanReach(ulong clientId)
        {
            if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            {
                return false;
            }

            var player = client.PlayerObject.GetComponent<PlayerController>();
            if (player == null || player.Settings == null || _collider == null)
            {
                return false;
            }

            var eye = player.CameraPivot != null ? player.CameraPivot.position : player.transform.position;
            var slack = _settings != null ? _settings.ReachSlack : 0f;
            return ReachRules.IsWithinReach(eye, _collider.ClosestPoint(eye), player.Settings.InteractDistance, slack);
        }

        private void OnOpenChanged(bool previous, bool current)
        {
            _door.SetOpen(current);
        }
    }
}
