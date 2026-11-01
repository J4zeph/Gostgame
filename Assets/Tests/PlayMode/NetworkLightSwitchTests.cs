using System.Collections;
using System.Reflection;
using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
using Game.Network.Interaction;
using Game.Network.Session;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Выключатель в сети на настоящем хосте NGO (один процесс): запрос игрока проходит через хост,
    /// хост проверяет дистанцию до выключателя, лампы следуют решению хоста.
    /// </summary>
    public class NetworkLightSwitchTests
    {
        private const ushort TestPort = 7791;
        private const int FramesToSettle = 5;
        private const float FarDistance = 10f;
        private const float NearDistance = 1f;
        private const float SwitchHeight = 1.3f;
        private const uint PlayerTestHash = 0x7E570011;
        private const uint SwitchTestHash = 0x7E570012;

        private GameObject _managerObject;
        private NetworkManager _manager;
        private GameObject _playerObject;
        private PlayerSettings _playerSettings;
        private GameObject _switchObject;
        private GameObject _lampObject;
        private Light _lamp;
        private LightSwitch _switch;
        private NetworkLightSwitch _networkSwitch;
        private LightSwitchSettings _switchSettings;
        private NetworkSettings _networkSettings;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _managerObject = new GameObject("TestNetworkManager");
            _manager = _managerObject.AddComponent<NetworkManager>();
            var transport = _managerObject.AddComponent<UnityTransport>();
            var connection = transport.ConnectionData;
            connection.Address = "127.0.0.1";
            connection.Port = TestPort;
            transport.ConnectionData = connection;
            _manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
            };

            Assert.IsTrue(_manager.StartHost(), "Хост не запустился");
            yield return null;

            _playerSettings = ScriptableObject.CreateInstance<PlayerSettings>();
            _switchSettings = ScriptableObject.CreateInstance<LightSwitchSettings>();
            _networkSettings = ScriptableObject.CreateInstance<NetworkSettings>();

            _playerObject = new GameObject("HostPlayer");
            _playerObject.SetActive(false);
            var playerNetworkObject = _playerObject.AddComponent<NetworkObject>();
            _playerObject.AddComponent<CharacterController>();
            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(_playerObject.transform, false);
            pivot.localPosition = Vector3.up * _playerSettings.EyeHeight;
            _playerObject.AddComponent<PlayerController>().Init(_playerSettings, pivot);
            AssignTestHash(playerNetworkObject, PlayerTestHash);
            _playerObject.SetActive(true);
            playerNetworkObject.SpawnAsPlayerObject(_manager.LocalClientId);

            _lampObject = new GameObject("Lamp");
            _lamp = _lampObject.AddComponent<Light>();

            _switchObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _switchObject.name = "NetSwitch";
            _switchObject.SetActive(false);
            _switchObject.transform.position = new Vector3(0f, SwitchHeight, 0f);
            _switchObject.transform.localScale = new Vector3(0.08f, 0.12f, 0.04f);
            var switchNetworkObject = _switchObject.AddComponent<NetworkObject>();
            _switch = _switchObject.AddComponent<LightSwitch>();
            _switch.Init(_switchSettings, new[] { _lamp });
            _networkSwitch = _switchObject.AddComponent<NetworkLightSwitch>();
            _networkSwitch.Init(_switch, _networkSettings);
            AssignTestHash(switchNetworkObject, SwitchTestHash);
            _switchObject.SetActive(true);
            switchNetworkObject.Spawn();

            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_manager != null)
            {
                _manager.Shutdown();
            }

            yield return null;
            Object.Destroy(_switchObject);
            Object.Destroy(_lampObject);
            Object.Destroy(_playerObject);
            Object.Destroy(_managerObject);
            Object.Destroy(_playerSettings);
            Object.Destroy(_switchSettings);
            Object.Destroy(_networkSettings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawned_SwitchWaitsForHost_AndStartsFromSettings()
        {
            Assert.IsFalse(_switch.HandleInteractLocally);
            Assert.AreEqual(_switchSettings.StartOn, _networkSwitch.IsOn);
            Assert.AreEqual(_switchSettings.StartOn, _lamp.enabled);
            yield break;
        }

        [UnityTest]
        public IEnumerator NearPlayer_Interact_HostTurnsLightOnAndOff()
        {
            PlacePlayerInFrontOfSwitch(NearDistance);

            _switch.Interact(_playerObject);
            yield return Frames(FramesToSettle);

            Assert.IsTrue(_networkSwitch.IsOn, "Хост не включил свет по запросу игрока рядом");
            Assert.IsTrue(_lamp.enabled, "Решение хоста не дошло до лампы");

            _switch.Interact(_playerObject);
            yield return Frames(FramesToSettle);

            Assert.IsFalse(_networkSwitch.IsOn, "Повторный запрос должен выключить свет");
            Assert.IsFalse(_lamp.enabled);
        }

        [UnityTest]
        public IEnumerator FarPlayer_Interact_HostRejects()
        {
            PlacePlayerInFrontOfSwitch(FarDistance);

            _switch.Interact(_playerObject);
            yield return Frames(FramesToSettle);

            Assert.IsFalse(_networkSwitch.IsOn, "Хост включил свет игроку, который до выключателя не дотягивается");
            Assert.IsFalse(_lamp.enabled);
        }

        private void PlacePlayerInFrontOfSwitch(float distance)
        {
            _playerObject.transform.position = new Vector3(0f, 0f, -distance);
            Physics.SyncTransforms();
        }

        // Объекты собраны в тесте, а не из ассета, поэтому хеш взять неоткуда - задаём фиксированный.
        private static void AssignTestHash(NetworkObject networkObject, uint hash)
        {
            var field = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "В этой версии NGO нет поля GlobalObjectIdHash");
            field.SetValue(networkObject, hash);
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return null;
            }
        }
    }
}
