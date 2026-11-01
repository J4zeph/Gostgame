using System;
using Game.Core.Session;
using Game.Gameplay.Player;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game.Network.Session
{
    /// <summary>
    /// Сессия поверх NGO: создать игру (хост), подключиться по IP, выйти.
    /// Хост одобряет подключения: не больше MaxPlayers, позиция появления - по точкам SpawnPointGroup.
    /// </summary>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class NetworkSessionService : MonoBehaviour, ISessionService
    {
        private const string FullReason = "Сессия заполнена";

        [SerializeField] private NetworkSettings _settings;
        [SerializeField] private SpawnPointGroup _spawnPoints;

        private NetworkManager _network;
        private UnityTransport _transport;

        public SessionState State { get; private set; } = SessionState.Offline;
        public string LastError { get; private set; } = string.Empty;
        public string DefaultAddress => _settings != null ? _settings.DefaultAddress : string.Empty;

        public event Action StateChanged;

        /// <summary>Для сборки из кода (тесты). Вызывать до активации объекта.</summary>
        public void Init(NetworkSettings settings, SpawnPointGroup spawnPoints)
        {
            _settings = settings;
            _spawnPoints = spawnPoints;
        }

        private void Awake()
        {
            _network = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();
        }

        private void OnEnable()
        {
            _network.NetworkConfig.ConnectionApproval = true;
            _network.ConnectionApprovalCallback = Approve;
            _network.OnClientConnectedCallback += OnClientConnected;
            _network.OnClientDisconnectCallback += OnClientDisconnected;
            _network.OnTransportFailure += OnTransportFailure;
            SessionRegistry.Register(this);
        }

        private void OnDisable()
        {
            SessionRegistry.Unregister(this);
            if (_network == null)
            {
                return;
            }

            _network.ConnectionApprovalCallback = null;
            _network.OnClientConnectedCallback -= OnClientConnected;
            _network.OnClientDisconnectCallback -= OnClientDisconnected;
            _network.OnTransportFailure -= OnTransportFailure;
        }

        public void StartHost()
        {
            if (State != SessionState.Offline)
            {
                return;
            }

            SetConnection(_settings.DefaultAddress, _settings.ListenAddress);
            SetState(SessionState.Starting, string.Empty);
            if (_network.StartHost())
            {
                SetState(SessionState.Host, string.Empty);
            }
            else
            {
                SetState(SessionState.Offline, "Не удалось создать игру (порт занят?)");
            }
        }

        public void StartClient(string address)
        {
            if (State != SessionState.Offline)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                SetState(SessionState.Offline, "Введите адрес хоста");
                return;
            }

            SetConnection(address.Trim(), null);
            SetState(SessionState.Starting, string.Empty);
            if (!_network.StartClient())
            {
                SetState(SessionState.Offline, "Не удалось начать подключение (неверный адрес?)");
            }
        }

        public void Leave()
        {
            if (State == SessionState.Offline)
            {
                return;
            }

            _network.Shutdown();
            SetState(SessionState.Offline, string.Empty);
        }

        // Поле ConnectionData вместо SetConnectionData: у того есть перегрузка с типом из Unity Transport,
        // и ради неё сборке понадобилась бы ссылка на Unity.Networking.Transport (её нет в таблице CLAUDE.md).
        private void SetConnection(string address, string listenAddress)
        {
            var data = _transport.ConnectionData;
            data.Address = address;
            data.Port = _settings.Port;
            data.ServerListenAddress = listenAddress;
            _transport.ConnectionData = data;
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var connected = _network.ConnectedClientsIds.Count;
            response.Approved = ConnectionRules.CanAccept(connected, _settings.MaxPlayers);
            response.CreatePlayerObject = response.Approved;
            response.Pending = false;

            if (!response.Approved)
            {
                response.Reason = FullReason;
                return;
            }

            if (_spawnPoints != null && _spawnPoints.Count > 0)
            {
                var spawn = _spawnPoints.Get(ConnectionRules.SpawnIndexFor(connected, _spawnPoints.Count));
                response.Position = spawn.position;
                response.Rotation = spawn.rotation;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!_network.IsServer && clientId == _network.LocalClientId)
            {
                SetState(SessionState.Client, string.Empty);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            // На хосте отключение чужого клиента - не наше событие; на клиенте любое отключение - конец сессии.
            if (_network.IsServer && clientId != _network.LocalClientId)
            {
                return;
            }

            var reason = _network.DisconnectReason;
            SetState(SessionState.Offline, string.IsNullOrEmpty(reason) ? "Соединение потеряно" : reason);
        }

        private void OnTransportFailure()
        {
            SetState(SessionState.Offline, "Ошибка сети");
        }

        private void SetState(SessionState state, string error)
        {
            if (state == State && error == LastError)
            {
                return;
            }

            State = state;
            LastError = error;
            StateChanged?.Invoke();
        }
    }
}
