using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.Scenes
{
    /// <summary>
    /// Геометрия дома (этап 1 GDD): коридор посередине, по две комнаты с каждой стороны, входная дверь в торце
    /// коридора, перед ней двор со стартовой зоной. У каждой комнаты и коридора - дверь, выключатель и лампы;
    /// в спальне шкаф-укрытие с дверцей. Здесь только объекты и коллайдеры, без игровых компонентов и ассетов -
    /// поэтому годится для EditMode-тестов. Компоненты навешивает HouseBuilder.
    ///
    /// План (вид сверху, +Z - вглубь дома):
    ///   z=10 +---------+----+---------+
    ///        | Bathroom|    | Bedroom |
    ///   z=5  +---------+ Co +---------+
    ///        | Kitchen |    | Living  |
    ///   z=0  +---------+-[]-+---------+  входная дверь
    ///                 двор (старт)
    /// </summary>
    public static class HouseLayout
    {
        public const string LevelName = "Level";
        public const string StructureName = "Structure";
        public const string RoomsName = "Rooms";
        public const string SpawnPointsName = "SpawnPoints";
        public const string SpawnPointPrefix = "SpawnPoint_";
        public const string DoorName = "Door";
        public const string DoorPanelName = "DoorPanel";
        public const string LightSwitchName = "LightSwitch";
        public const string LampPrefix = "Lamp_";
        public const string WardrobeName = "Wardrobe";

        public const string Corridor = "Corridor";
        public const string Kitchen = "Kitchen";
        public const string LivingRoom = "LivingRoom";
        public const string Bathroom = "Bathroom";
        public const string Bedroom = "Bedroom";
        public const string WardrobeRoom = Bedroom;

        // Размеры в метрах.
        public const float WallHeight = 3f;
        public const float WallThickness = 0.2f;
        public const float DoorWidth = 1.2f;
        public const float DoorHeight = 2.1f;
        private const float DoorThickness = 0.05f;
        private const float DoorGap = 0.01f;

        public const float CorridorWidth = 2.5f;
        public const float RoomWidth = 5f;
        public const float RoomDepth = 5f;
        public const float HouseDepth = RoomDepth * 2f;
        public const float YardDepth = 8f;
        private const float FenceHeight = 1.2f;

        /// <summary>X осевой линии внешней боковой стены.</summary>
        public const float OuterX = CorridorWidth / 2f + RoomWidth;
        private const float CorridorHalf = CorridorWidth / 2f;

        public const int SpawnPointCount = 4;
        private const float SpawnSpacing = 1.5f;

        public static readonly Vector3 SwitchSize = new Vector3(0.08f, 0.12f, 0.04f);
        public const float SwitchHeight = 1.3f;
        private const float SwitchOffsetFromDoorway = 0.3f;

        private const float LampDropFromCeiling = 0.3f;
        private static readonly Vector3 ShadeSize = new Vector3(0.35f, 0.08f, 0.35f);
        private const float ShadeAboveLamp = 0.1f;

        public const float WardrobeInnerWidth = 1.1f;
        public const float WardrobeInnerDepth = 0.8f;
        public const float WardrobeInnerHeight = DoorHeight;
        private const float WardrobePanel = 0.05f;
        /// <summary>Сдвиг шкафа от центра стены спальни вглубь дома.</summary>
        private const float WardrobeOffsetAlongWall = 1f;

        private const float MoonIntensity = 0.3f;
        private static readonly Vector3 MoonRotation = new Vector3(50f, -30f, 0f);

        private const float EyeHeight = 1.65f;
        private const float MenuCameraFromFence = 1f;

        /// <summary>Камера меню: у забора двора, смотрит на входную дверь.</summary>
        public static Vector3 MenuCameraPosition => new Vector3(0f, EyeHeight, -YardDepth + MenuCameraFromFence);

        /// <summary>Центр проёма входной двери.</summary>
        public static Vector3 FrontDoorwayCenter => Vector3.zero;

        public static readonly IReadOnlyList<HouseRoom> Rooms = new[]
        {
            new HouseRoom(Corridor, new Rect(-CorridorHalf, 0f, CorridorWidth, HouseDepth), FrontDoorwayCenter, 0f,
                new Vector2(0f, RoomDepth / 2f), new Vector2(0f, RoomDepth * 1.5f)),
            new HouseRoom(Kitchen, new Rect(-OuterX, 0f, RoomWidth, RoomDepth),
                new Vector3(-CorridorHalf, 0f, RoomDepth / 2f), -90f, new Vector2(-OuterX + RoomWidth / 2f, RoomDepth / 2f)),
            new HouseRoom(Bathroom, new Rect(-OuterX, RoomDepth, RoomWidth, RoomDepth),
                new Vector3(-CorridorHalf, 0f, RoomDepth * 1.5f), -90f, new Vector2(-OuterX + RoomWidth / 2f, RoomDepth * 1.5f)),
            new HouseRoom(LivingRoom, new Rect(CorridorHalf, 0f, RoomWidth, RoomDepth),
                new Vector3(CorridorHalf, 0f, RoomDepth / 2f), 90f, new Vector2(OuterX - RoomWidth / 2f, RoomDepth / 2f)),
            new HouseRoom(Bedroom, new Rect(CorridorHalf, RoomDepth, RoomWidth, RoomDepth),
                new Vector3(CorridorHalf, 0f, RoomDepth * 1.5f), 90f, new Vector2(OuterX - RoomWidth / 2f, RoomDepth * 1.5f)),
        };

        /// <summary>Строит дом, двор, точки появления и лунный свет под root.</summary>
        public static void BuildLayout(Transform root, GreyboxPalette palette)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var level = GreyboxGeometry.CreateChild(LevelName, root);
            var structure = GreyboxGeometry.CreateChild(StructureName, level);
            BuildFloors(structure, palette);
            BuildWalls(structure, palette);
            BuildYardFence(structure, palette);

            var rooms = GreyboxGeometry.CreateChild(RoomsName, level);
            foreach (var room in Rooms)
            {
                var node = GreyboxGeometry.CreateChild(room.Name, rooms);
                BuildDoor(node, room.DoorwayCenter, room.DoorRotation, DoorWidth, DoorHeight, palette.Door);
                BuildSwitch(node, room, palette);
                BuildLamps(node, room, palette);
            }

            BuildWardrobe(rooms.Find(WardrobeRoom), palette);
            BuildSpawnPoints(root);
            BuildMoonlight(root);
        }

        public static HouseRoom GetRoom(string name)
        {
            foreach (var room in Rooms)
            {
                if (room.Name == name)
                {
                    return room;
                }
            }

            throw new ArgumentException($"Нет комнаты {name}", nameof(name));
        }

        /// <summary>Центр выключателя комнаты: у проёма со стороны защёлки, на стене со стороны комнаты.</summary>
        public static Vector3 SwitchPosition(HouseRoom room)
        {
            var alongWall = room.DoorRight * (DoorWidth / 2f + SwitchOffsetFromDoorway);
            var offWall = room.DoorForward * (WallThickness / 2f + SwitchSize.z / 2f);
            return room.DoorwayCenter + alongWall + offWall + Vector3.up * SwitchHeight;
        }

        /// <summary>Точка у пола в центре шкафа, мировые координаты относительно root дома.</summary>
        public static Vector3 WardrobeInnerCenter => WardrobeOrigin + WardrobeRotation * new Vector3(0f, 0f, -WardrobeInnerDepth / 2f);

        // Шкаф стоит задней стенкой к внешней восточной стене спальни, открывается на запад (в комнату).
        private static Quaternion WardrobeRotation => Quaternion.Euler(0f, -90f, 0f);

        private static Vector3 WardrobeOrigin
        {
            get
            {
                var bedroom = GetRoom(WardrobeRoom);
                var wallInnerFace = OuterX - WallThickness / 2f;
                return new Vector3(wallInnerFace - WardrobeInnerDepth - WardrobePanel, 0f,
                    bedroom.Bounds.center.y + WardrobeOffsetAlongWall);
            }
        }

        private static void BuildFloors(Transform parent, GreyboxPalette palette)
        {
            var width = OuterX * 2f + WallThickness;
            var t = WallThickness;

            GreyboxGeometry.CreateBox("House_Floor", parent, new Vector3(0f, -t / 2f, HouseDepth / 2f),
                new Vector3(width, t, HouseDepth + t), palette.Floor);
            GreyboxGeometry.CreateBox("House_Ceiling", parent, new Vector3(0f, WallHeight + t / 2f, HouseDepth / 2f),
                new Vector3(width, t, HouseDepth + t), palette.Ceiling);
            GreyboxGeometry.CreateBox("Yard_Ground", parent, new Vector3(0f, -t / 2f, -YardDepth / 2f - t / 2f),
                new Vector3(width, t, YardDepth), palette.Ground);
        }

        private static void BuildWalls(Transform parent, GreyboxPalette palette)
        {
            var t = WallThickness;
            var outer = OuterX + t / 2f;
            var wall = palette.Wall;

            BuildWall("Wall_Front", parent, new Vector2(-outer, 0f), new Vector2(outer, 0f), WallHeight, wall, outer);
            BuildWall("Wall_Back", parent, new Vector2(-outer, HouseDepth), new Vector2(outer, HouseDepth), WallHeight, wall);
            BuildWall("Wall_West", parent, new Vector2(-OuterX, t / 2f), new Vector2(-OuterX, HouseDepth - t / 2f), WallHeight, wall);
            BuildWall("Wall_East", parent, new Vector2(OuterX, t / 2f), new Vector2(OuterX, HouseDepth - t / 2f), WallHeight, wall);

            // Стены коридора: проёмы на середине каждой комнаты, отсчёт от начала стены (t/2).
            var nearDoorway = RoomDepth / 2f - t / 2f;
            var farDoorway = RoomDepth * 1.5f - t / 2f;
            BuildWall("Wall_Corridor_West", parent, new Vector2(-CorridorHalf, t / 2f), new Vector2(-CorridorHalf, HouseDepth - t / 2f),
                WallHeight, wall, nearDoorway, farDoorway);
            BuildWall("Wall_Corridor_East", parent, new Vector2(CorridorHalf, t / 2f), new Vector2(CorridorHalf, HouseDepth - t / 2f),
                WallHeight, wall, nearDoorway, farDoorway);

            BuildWall("Wall_Divider_West", parent, new Vector2(-OuterX + t / 2f, RoomDepth), new Vector2(-CorridorHalf - t / 2f, RoomDepth),
                WallHeight, wall);
            BuildWall("Wall_Divider_East", parent, new Vector2(CorridorHalf + t / 2f, RoomDepth), new Vector2(OuterX - t / 2f, RoomDepth),
                WallHeight, wall);
        }

        private static void BuildYardFence(Transform parent, GreyboxPalette palette)
        {
            var t = WallThickness;
            var outer = OuterX + t / 2f;
            BuildWall("Fence_West", parent, new Vector2(-OuterX, -YardDepth + t / 2f), new Vector2(-OuterX, -t / 2f), FenceHeight, palette.Wall);
            BuildWall("Fence_East", parent, new Vector2(OuterX, -YardDepth + t / 2f), new Vector2(OuterX, -t / 2f), FenceHeight, palette.Wall);
            BuildWall("Fence_South", parent, new Vector2(-outer, -YardDepth), new Vector2(outer, -YardDepth), FenceHeight, palette.Wall);
        }

        /// <summary>
        /// Прямая стена по оси X или Z между точками плана (x, z). Проёмы (ширина DoorWidth, высота DoorHeight)
        /// заданы расстоянием центра от начала стены; над проёмом - перемычка.
        /// </summary>
        private static void BuildWall(string name, Transform parent, Vector2 from, Vector2 to, float height, Material material,
            params float[] doorways)
        {
            var delta = to - from;
            var length = delta.magnitude;
            var direction = delta / length;
            var alongX = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);

            var sorted = (float[])doorways.Clone();
            Array.Sort(sorted);

            var cursor = 0f;
            var piece = 0;
            foreach (var center in sorted)
            {
                AddWallPiece(name, ++piece, parent, from, direction, alongX, cursor, center - DoorWidth / 2f, 0f, height, material);
                AddWallPiece(name, ++piece, parent, from, direction, alongX, center - DoorWidth / 2f, center + DoorWidth / 2f,
                    DoorHeight, height, material);
                cursor = center + DoorWidth / 2f;
            }

            AddWallPiece(name, ++piece, parent, from, direction, alongX, cursor, length, 0f, height, material);
        }

        private static void AddWallPiece(string name, int index, Transform parent, Vector2 from, Vector2 direction, bool alongX,
            float start, float end, float bottom, float top, Material material)
        {
            const float minSize = 0.001f;
            if (end - start < minSize || top - bottom < minSize)
            {
                return;
            }

            var mid = from + direction * ((start + end) / 2f);
            var center = new Vector3(mid.x, (bottom + top) / 2f, mid.y);
            var size = alongX
                ? new Vector3(end - start, top - bottom, WallThickness)
                : new Vector3(WallThickness, top - bottom, end - start);
            GreyboxGeometry.CreateBox($"{name}_{index}", parent, center, size, material);
        }

        /// <summary>
        /// Дверь: пивот на петле (левый край проёма, если смотреть по DoorForward), полотно - дочерний объект.
        /// Открывается в сторону forward поворота (настройка OpenAngle < 0).
        /// </summary>
        private static void BuildDoor(Transform parent, Vector3 doorwayCenter, Quaternion rotation, float width, float height,
            Material material)
        {
            var hinge = GreyboxGeometry.CreateChild(DoorName, parent);
            hinge.localPosition = doorwayCenter - rotation * Vector3.right * (width / 2f);
            hinge.localRotation = rotation;

            var panelWidth = width - DoorGap * 2f;
            var panelHeight = height - DoorGap;
            GreyboxGeometry.CreateBox(DoorPanelName, hinge, new Vector3(width / 2f, panelHeight / 2f, 0f),
                new Vector3(panelWidth, panelHeight, DoorThickness), material, isStatic: false);
        }

        private static void BuildSwitch(Transform parent, HouseRoom room, GreyboxPalette palette)
        {
            var box = GreyboxGeometry.CreateBox(LightSwitchName, parent, SwitchPosition(room), SwitchSize, palette.Fixture,
                isStatic: false);
            box.transform.localRotation = room.DoorRotation;
        }

        private static void BuildLamps(Transform parent, HouseRoom room, GreyboxPalette palette)
        {
            for (var i = 0; i < room.Lamps.Length; i++)
            {
                var lamp = GreyboxGeometry.CreateChild(LampPrefix + (i + 1), parent);
                lamp.localPosition = new Vector3(room.Lamps[i].x, WallHeight - LampDropFromCeiling, room.Lamps[i].y);

                var light = lamp.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.shadows = LightShadows.Soft;

                // Плафон - только визуально: без коллайдера и без тени, иначе закроет свою же лампу.
                var shade = GreyboxGeometry.CreateBox("Shade", lamp, Vector3.up * ShadeAboveLamp, ShadeSize, palette.Fixture,
                    withCollider: false);
                shade.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>Шкаф: локально проём в плоскости z = 0, внутренность в -Z, дверца открывается наружу (+Z).</summary>
        private static void BuildWardrobe(Transform roomNode, GreyboxPalette palette)
        {
            var wardrobe = GreyboxGeometry.CreateChild(WardrobeName, roomNode);
            wardrobe.localPosition = WardrobeOrigin;
            wardrobe.localRotation = WardrobeRotation;

            const float w = WardrobeInnerWidth;
            const float d = WardrobeInnerDepth;
            const float h = WardrobeInnerHeight;
            const float p = WardrobePanel;
            var material = palette.Furniture;

            GreyboxGeometry.CreateBox("Back", wardrobe, new Vector3(0f, (h + p) / 2f, -d - p / 2f), new Vector3(w + p * 2f, h + p, p), material);
            GreyboxGeometry.CreateBox("Side_Left", wardrobe, new Vector3(-w / 2f - p / 2f, (h + p) / 2f, -d / 2f), new Vector3(p, h + p, d), material);
            GreyboxGeometry.CreateBox("Side_Right", wardrobe, new Vector3(w / 2f + p / 2f, (h + p) / 2f, -d / 2f), new Vector3(p, h + p, d), material);
            GreyboxGeometry.CreateBox("Top", wardrobe, new Vector3(0f, h + p / 2f, -d / 2f), new Vector3(w, p, d), material);

            // Дверца чуть впереди проёма, чтобы не задевать боковины при повороте.
            BuildDoor(wardrobe, new Vector3(0f, 0f, DoorThickness / 2f), Quaternion.identity, w, h, palette.Door);
        }

        private static void BuildSpawnPoints(Transform root)
        {
            var group = GreyboxGeometry.CreateChild(SpawnPointsName, root);
            var z = -YardDepth / 2f;
            var firstX = -SpawnSpacing * (SpawnPointCount - 1) / 2f;

            for (var i = 0; i < SpawnPointCount; i++)
            {
                var point = GreyboxGeometry.CreateChild(SpawnPointPrefix + (i + 1), group);
                point.localPosition = new Vector3(firstX + SpawnSpacing * i, 0f, z);
                point.localRotation = Quaternion.identity;
            }
        }

        private static void BuildMoonlight(Transform root)
        {
            var moon = GreyboxGeometry.CreateChild("Moon", root);
            moon.localRotation = Quaternion.Euler(MoonRotation);
            var light = moon.gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = MoonIntensity;
            light.shadows = LightShadows.Soft;
        }
    }
}
