using Game.Gameplay.Interaction;
using Game.Network.Session;
using Unity.Netcode;
using UnityEngine;

namespace Game.Network.Interaction
{
    /// <summary>
    /// Синхронизация выключателя: горит ли свет - NetworkVariable с записью только хостом.
    /// Игрок просит хост переключить свет; хост проверяет, что игрок дотягивается до выключателя.
    /// </summary>
    public sealed class NetworkLightSwitch : NetworkBehaviour
    {
        [SerializeField] private LightSwitch _switch;
        [SerializeField] private NetworkSettings _settings;

        private readonly NetworkVariable<bool> _isOn = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Collider _collider;

        public bool IsOn => _isOn.Value;

        /// <summary>Для сборки из кода (тесты). Вызывать до активации объекта.</summary>
        public void Init(LightSwitch lightSwitch, NetworkSettings settings)
        {
            _switch = lightSwitch;
            _settings = settings;
        }

        private void Awake()
        {
            _collider = GetComponentInChildren<Collider>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Стартовое состояние задают настройки выключателя на хосте.
                _isOn.Value = _switch.IsOn;
            }

            _switch.HandleInteractLocally = false;
            _switch.InteractRequested += OnInteractRequested;
            _isOn.OnValueChanged += OnStateChanged;
            _switch.SetOn(_isOn.Value);
        }

        public override void OnNetworkDespawn()
        {
            _switch.InteractRequested -= OnInteractRequested;
            _isOn.OnValueChanged -= OnStateChanged;
            _switch.HandleInteractLocally = true;
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

            _isOn.Value = !_isOn.Value;
        }

        private void OnStateChanged(bool previous, bool current)
        {
            _switch.SetOn(current);
        }
    }
}
