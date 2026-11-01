using Game.Editor.Scenes;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class GreyboxRoomBuilderTests
    {
        private const float PlayerRadius = 0.3f;
        private const float PlayerHeight = 1.8f;
        private const float RayHeight = 1f;

        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("GreyboxTestRoot");
            GreyboxRoomBuilder.BuildLayout(_root.transform, default);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void BuildLayout_CreatesDoorWithPanel()
        {
            var door = _root.transform.Find($"{GreyboxRoomBuilder.LevelName}/{GreyboxRoomBuilder.DoorName}");

            Assert.IsNotNull(door, "Нет объекта двери");
            Assert.IsNotNull(door.Find(GreyboxRoomBuilder.DoorPanelName), "У двери нет полотна");
        }

        [Test]
        public void BuildLayout_CreatesSpawnPointsInsideRoom()
        {
            var group = _root.transform.Find(GreyboxRoomBuilder.SpawnPointsName);

            Assert.IsNotNull(group);
            Assert.AreEqual(GreyboxRoomBuilder.SpawnPointCount, group.childCount);
            foreach (Transform point in group)
            {
                var p = point.position;
                Assert.Less(Mathf.Abs(p.x), GreyboxRoomBuilder.RoomWidth / 2f - PlayerRadius, point.name);
                Assert.Less(Mathf.Abs(p.z), GreyboxRoomBuilder.RoomDepth / 2f - PlayerRadius, point.name);
            }
        }

        [Test]
        public void SpawnPoints_HaveRoomForPlayer()
        {
            foreach (Transform point in _root.transform.Find(GreyboxRoomBuilder.SpawnPointsName))
            {
                var bottom = point.position + Vector3.up * (PlayerRadius + 0.05f);
                var top = point.position + Vector3.up * (PlayerHeight - PlayerRadius);

                Assert.IsFalse(Physics.CheckCapsule(bottom, top, PlayerRadius), $"{point.name} упирается в геометрию");
            }
        }

        [Test]
        public void ClosedDoor_IsFirstObstacleFromEverySpawnPointToDoorway()
        {
            var doorway = new Vector3(0f, RayHeight, GreyboxRoomBuilder.DoorWallZ);

            foreach (Transform point in _root.transform.Find(GreyboxRoomBuilder.SpawnPointsName))
            {
                var origin = point.position + Vector3.up * RayHeight;
                var toDoor = doorway - origin;

                Assert.IsTrue(Physics.Raycast(origin, toDoor.normalized, out var hit, toDoor.magnitude + 1f), point.name);
                Assert.AreEqual(GreyboxRoomBuilder.DoorPanelName, hit.collider.name, $"От {point.name} к двери мешает {hit.collider.name}");
            }
        }
    }
}
