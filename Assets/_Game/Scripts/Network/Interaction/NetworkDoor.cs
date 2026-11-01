using Game.Gameplay.Interaction;
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
            if (!HostReachCheck.CanReach(NetworkManager, rpcParams.Receive.SenderClientId, _collider, _settings))
            {
                return;
            }

            _isOpen.Value = !_isOpen.Value;
        }

        private void OnOpenChanged(bool previous, bool current)
        {
            _door.SetOpen(current);
        }
    }
}
