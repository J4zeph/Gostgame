using Game.Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace Game.Network.Player
{
    /// <summary>
    /// Делит экземпляры игрока на «мой» и «чужие». У своего включает ввод, камеру, слух и угол обзора;
    /// у чужого всё это выключено, а движение приходит через NetworkTransform владельца.
    /// В префабе эти компоненты выключены, чтобы чужой игрок ни на кадр не перехватил ввод.
    /// </summary>
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private Camera _camera;
        [SerializeField] private AudioListener _listener;
        [SerializeField] private PlayerCameraFov _fieldOfView;
        [SerializeField] private PlayerBody _body;

        public override void OnNetworkSpawn()
        {
            SetLocal(IsOwner);
        }

        public override void OnNetworkDespawn()
        {
            SetLocal(false);
        }

        private void SetLocal(bool local)
        {
            _player.SetInputSource(local ? _input : null);
            _input.enabled = local;
            _camera.enabled = local;
            _listener.enabled = local;
            _fieldOfView.enabled = local;
            if (_body != null)
            {
                _body.SetFirstPerson(local);
            }
        }
    }
}
