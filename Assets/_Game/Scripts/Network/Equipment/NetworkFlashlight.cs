using Game.Gameplay.Equipment;
using Unity.Netcode;
using UnityEngine;

namespace Game.Network.Equipment
{
    /// <summary>
    /// Синхронизация фонарика игрока: включён ли - NetworkVariable с записью только хостом.
    /// Переключить может только владелец фонарика (хост проверяет отправителя).
    /// </summary>
    public sealed class NetworkFlashlight : NetworkBehaviour
    {
        [SerializeField] private Flashlight _flashlight;

        private readonly NetworkVariable<bool> _isOn = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _isOn.Value = _flashlight.IsOn;
            }

            _flashlight.HandleToggleLocally = false;
            _flashlight.ToggleRequested += OnToggleRequested;
            _isOn.OnValueChanged += OnStateChanged;
            _flashlight.SetOn(_isOn.Value);
        }

        public override void OnNetworkDespawn()
        {
            _flashlight.ToggleRequested -= OnToggleRequested;
            _isOn.OnValueChanged -= OnStateChanged;
            _flashlight.HandleToggleLocally = true;
        }

        private void OnToggleRequested()
        {
            if (IsOwner)
            {
                RequestToggleRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestToggleRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
            {
                return;
            }

            _isOn.Value = !_isOn.Value;
        }

        private void OnStateChanged(bool previous, bool current)
        {
            _flashlight.SetOn(current);
        }
    }
}
