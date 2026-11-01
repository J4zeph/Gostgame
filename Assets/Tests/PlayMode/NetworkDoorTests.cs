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
    /// Дверь в сети на настоящем хосте NGO (один процесс): запрос игрока проходит через хост,
    /// хост проверяет дистанцию до двери.
    /// </summary>
    public class NetworkDoorTests
    {
        private const ushort TestPort = 7790;
        private const int FramesToSettle = 5;
        private const float FarDistance = 10f;
        private const float NearDistance = 1f;
        private const uint PlayerTestHash = 0x7E570001;
        private const uint DoorTestHash = 0x7E570002;

        private GameObject _managerObject;
        private NetworkManager _manager;
        private GameObject _playerObject;
        private PlayerSettings _playerSettings;
        private GameObject _doorObject;
        private Door _door;
        private NetworkDoor _networkDoor;
        private DoorSettings _doorSettings;
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
            _doorSettings = ScriptableObject.CreateInstance<DoorSettings>();
            _networkSettings = ScriptableObject.CreateInstance<NetworkSettings>();

            // Игрок хоста: только то, что нужно хосту для проверки дистанции.
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

            _doorObject = new GameObject("NetDoor");
            _doorObject.SetActive(false);
            var doorNetworkObject = _doorObject.AddComponent<NetworkObject>();
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.transform.SetParent(_doorObject.transform, false);
            panel.transform.localPosition = new Vector3(0.6f, 1f, 0f);
            panel.transform.localScale = new Vector3(1.2f, 2f, 0.05f);
            _door = _doorObject.AddComponent<Door>();
            _door.Init(_doorSettings);
            _networkDoor = _doorObject.AddComponent<NetworkDoor>();
            _networkDoor.Init(_door, _networkSettings);
            AssignTestHash(doorNetworkObject, DoorTestHash);
            _doorObject.SetActive(true);
            doorNetworkObject.Spawn();

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
            Object.Destroy(_doorObject);
            Object.Destroy(_playerObject);
            Object.Destroy(_managerObject);
            Object.Destroy(_playerSettings);
            Object.Destroy(_doorSettings);
            Object.Destroy(_networkSettings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawned_DoorWaitsForHost_InsteadOfTogglingItself()
        {
            Assert.IsFalse(_door.HandleInteractLocally);
            yield break;
        }

        [UnityTest]
        public IEnumerator NearPlayer_Interact_HostOpensDoor()
        {
            PlacePlayerInFrontOfDoor(NearDistance);

            _door.Interact(_playerObject);
            yield return Frames(FramesToSettle);

            Assert.IsTrue(_networkDoor.IsOpen, "Хост не открыл дверь по запросу игрока рядом");
            Assert.IsTrue(_door.IsOpen, "Состояние хоста не дошло до двери");

            _door.Interact(_playerObject);
            yield return Frames(FramesToSettle);

            Assert.IsFalse(_networkDoor.IsOpen, "Повторный запрос должен закрыть дверь");
        }

        [UnityTest]
        public IEnumerator FarPlayer_Interact_HostRejects()
        {
            PlacePlayerInFrontOfDoor(FarDistance);

            _door.Interact(_playerObject);
            yield return Frames(FramesToSettle);

            Assert.IsFalse(_networkDoor.IsOpen, "Хост открыл дверь игроку, который до неё не дотягивается");
            Assert.IsFalse(_door.IsOpen);
        }

        private void PlacePlayerInFrontOfDoor(float distance)
        {
            _playerObject.transform.position = new Vector3(0.6f, 0f, -distance);
            Physics.SyncTransforms();
        }

        // Объекты собраны в тесте, а не из ассета, поэтому хеш взять неоткуда - задаём фиксированный,
        // как делают тестовые хелперы NGO. В игре хеши считает NetworkObjectHashUtility.
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
