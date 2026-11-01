using UnityEngine;

namespace Game.Network.Session
{
    /// <summary>
    /// Параметры сети. Ассет: _Game/Data/Network/NetworkSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Gostgame/Network Settings", fileName = "NetworkSettings")]
    public sealed class NetworkSettings : ScriptableObject
    {
        [SerializeField, Range(1, 16)] private int _maxPlayers = 4;
        [SerializeField] private ushort _port = 7777;
        [SerializeField, Tooltip("Адрес по умолчанию в меню подключения")] private string _defaultAddress = "127.0.0.1";
        [SerializeField, Tooltip("На каком адресе хост ждёт подключения; 0.0.0.0 - со всех сетевых интерфейсов")]
        private string _listenAddress = "0.0.0.0";

        [SerializeField, Min(0f), Tooltip("Запас к дальности взаимодействия при проверке на хосте, м (позиция клиента на хосте отстаёт)")]
        private float _reachSlack = 0.75f;

        public int MaxPlayers => _maxPlayers;
        public ushort Port => _port;
        public string DefaultAddress => _defaultAddress;
        public string ListenAddress => _listenAddress;
        public float ReachSlack => _reachSlack;
    }
}
