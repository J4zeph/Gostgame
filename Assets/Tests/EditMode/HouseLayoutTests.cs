using Game.Editor.Scenes;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Геометрия дома этапа 1: у каждой комнаты дверь, выключатель и лампы, игрок помещается
    /// в точках появления и в шкафу, двери и выключатели не загорожены.
    /// </summary>
    public class HouseLayoutTests
    {
        private const float PlayerRadius = 0.3f;
        private const float PlayerHeight = 1.8f;
        private const float RayHeight = 1f;
        private const float InteractDistance = 2f;
        private const float SwitchCheckDistance = 1f;
        private const float CapsuleLift = 0.05f;

        private GameObject _root;
        private Transform _rooms;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("HouseTestRoot");
            HouseLayout.BuildLayout(_root.transform, default);
            _rooms = _root.transform.Find($"{HouseLayout.LevelName}/{HouseLayout.RoomsName}");
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void House_HasCorridorAndFourRooms()
        {
            Assert.AreEqual(5, HouseLayout.Rooms.Count);
            foreach (var room in HouseLayout.Rooms)
            {
                Assert.IsNotNull(_rooms.Find(room.Name), room.Name);
            }
        }

        [Test]
        public void EveryRoom_HasSwingingDoor_Switch_AndLampInside()
        {
            foreach (var room in HouseLayout.Rooms)
            {
                var node = _rooms.Find(room.Name);
                var panel = node.Find($"{HouseLayout.DoorName}/{HouseLayout.DoorPanelName}");
                Assert.IsNotNull(panel, $"{room.Name}: нет двери");
                Assert.IsFalse(panel.gameObject.isStatic, $"{room.Name}: полотно двери статично и не повернётся");

                var lightSwitch = node.Find(HouseLayout.LightSwitchName);
                Assert.IsNotNull(lightSwitch, $"{room.Name}: нет выключателя");
                Assert.IsNotNull(lightSwitch.GetComponent<Collider>(), $"{room.Name}: в выключатель не попасть лучом");

                var lamp = node.Find(HouseLayout.LampPrefix + 1);
                Assert.IsNotNull(lamp, $"{room.Name}: нет лампы");
                Assert.IsNotNull(lamp.GetComponent<Light>(), $"{room.Name}: у лампы нет Light");
                var p = lamp.position;
                Assert.IsTrue(room.Bounds.Contains(new Vector2(p.x, p.z)), $"{room.Name}: лампа вне комнаты");
            }
        }

        [Test]
        public void Bedroom_HasWardrobeWithDoor()
        {
            var door = _rooms.Find($"{HouseLayout.WardrobeRoom}/{HouseLayout.WardrobeName}/{HouseLayout.DoorName}/{HouseLayout.DoorPanelName}");

            Assert.IsNotNull(door, "Нет дверцы шкафа");
            Assert.IsFalse(door.gameObject.isStatic);
        }

        [Test]
        public void Wardrobe_FitsPlayer()
        {
            Assert.IsTrue(CapsuleIsFree(HouseLayout.WardrobeInnerCenter), "Игрок не помещается в шкаф");
        }

        [Test]
        public void SpawnPoints_AreInYard_AndHaveRoomForPlayer()
        {
            var group = _root.transform.Find(HouseLayout.SpawnPointsName);

            Assert.AreEqual(HouseLayout.SpawnPointCount, group.childCount);
            foreach (Transform point in group)
            {
                var p = point.position;
                Assert.Less(p.z, -PlayerRadius, $"{point.name} не во дворе");
                Assert.Greater(p.z, -HouseLayout.YardDepth + PlayerRadius, $"{point.name} за забором");
                Assert.Less(Mathf.Abs(p.x), HouseLayout.OuterX - PlayerRadius, $"{point.name} за забором");
                Assert.IsTrue(CapsuleIsFree(p), $"{point.name} упирается в геометрию");
            }
        }

        [Test]
        public void FrontDoor_IsFirstObstacleFromEverySpawnPoint()
        {
            var doorway = HouseLayout.FrontDoorwayCenter + Vector3.up * RayHeight;

            foreach (Transform point in _root.transform.Find(HouseLayout.SpawnPointsName))
            {
                AssertFirstHitIsDoor(point.position + Vector3.up * RayHeight, doorway, HouseLayout.Corridor, point.name);
            }
        }

        [Test]
        public void RoomDoors_AreFirstObstacleFromCorridor()
        {
            foreach (var room in HouseLayout.Rooms)
            {
                if (room.Name == HouseLayout.Corridor)
                {
                    continue;
                }

                var doorway = room.DoorwayCenter + Vector3.up * RayHeight;
                var corridorPoint = new Vector3(0f, RayHeight, doorway.z);
                AssertFirstHitIsDoor(corridorPoint, doorway, room.Name, room.Name);
            }
        }

        [Test]
        public void Switches_CanBeReachedFromInsideTheRoom()
        {
            foreach (var room in HouseLayout.Rooms)
            {
                var target = HouseLayout.SwitchPosition(room);
                var origin = target + room.DoorForward * SwitchCheckDistance;

                Assert.IsTrue(Physics.Raycast(origin, -room.DoorForward, out var hit, InteractDistance), room.Name);
                Assert.AreEqual(HouseLayout.LightSwitchName, hit.collider.name, $"{room.Name}: к выключателю мешает {hit.collider.name}");
            }
        }

        private void AssertFirstHitIsDoor(Vector3 origin, Vector3 doorway, string roomName, string label)
        {
            var toDoor = doorway - origin;
            var expectedPanel = _rooms.Find($"{roomName}/{HouseLayout.DoorName}/{HouseLayout.DoorPanelName}");

            Assert.IsTrue(Physics.Raycast(origin, toDoor.normalized, out var hit, toDoor.magnitude + 1f), label);
            Assert.AreSame(expectedPanel, hit.collider.transform, $"От {label} к двери мешает {hit.collider.name}");
        }

        private static bool CapsuleIsFree(Vector3 feet)
        {
            var bottom = feet + Vector3.up * (PlayerRadius + CapsuleLift);
            var top = feet + Vector3.up * (PlayerHeight - PlayerRadius);
            return !Physics.CheckCapsule(bottom, top, PlayerRadius);
        }
    }
}
