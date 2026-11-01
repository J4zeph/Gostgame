using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.Scenes
{
    /// <summary>
    /// Сцена-заготовка для первых тестов: комната с дверным проёмом и дверью, коридор за ней,
    /// четыре точки появления, свет и временная камера. Повторный запуск пересобирает сцену целиком.
    /// Batchmode: -executeMethod Game.Editor.Scenes.GreyboxRoomBuilder.BuildFromCommandLine
    /// </summary>
    public static class GreyboxRoomBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Greybox_Room.unity";

        public const string RootName = "Greybox";
        public const string LevelName = "Level";
        public const string DoorName = "Door";
        public const string DoorPanelName = "DoorPanel";
        public const string SpawnPointsName = "SpawnPoints";
        public const string SpawnPointPrefix = "SpawnPoint_";

        private const string MaterialsFolder = "Assets/_Game/Art/Materials";
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string BaseColorProperty = "_BaseColor";

        // Размеры в метрах. Комната центрирована в начале координат, дверь в стене +Z.
        public const float RoomWidth = 8f;
        public const float RoomDepth = 8f;
        public const float WallHeight = 3f;
        public const float WallThickness = 0.2f;
        public const float DoorWidth = 1.2f;
        public const float DoorHeight = 2.1f;
        private const float DoorThickness = 0.05f;
        private const float DoorGap = 0.01f;
        private const float CorridorWidth = 3f;
        private const float CorridorLength = 6f;

        public const int SpawnPointCount = 4;
        private const float SpawnRowOffsetFromBackWall = 1.5f;
        private const float SpawnSpacing = 1.5f;

        private const float SunIntensity = 0.3f;
        private static readonly Vector3 SunRotation = new Vector3(50f, -30f, 0f);
        private const float LampIntensity = 1.5f;
        private const float LampRange = 9f;
        private const float LampDropFromCeiling = 0.3f;
        private const float EyeHeight = 1.7f;

        private static readonly Color FloorColor = new Color(0.32f, 0.32f, 0.34f);
        private static readonly Color WallColor = new Color(0.62f, 0.62f, 0.6f);
        private static readonly Color CeilingColor = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color DoorColor = new Color(0.42f, 0.29f, 0.2f);

        /// <summary>Z-координата середины стены с дверью.</summary>
        public static float DoorWallZ => RoomDepth / 2f + WallThickness / 2f;

        [MenuItem("Tools/Gostgame/Build Greybox Room")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void BuildFromCommandLine()
        {
            BuildScene();
        }

        public static void BuildScene()
        {
            var palette = CreateOrUpdatePalette();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject(RootName);
            BuildLayout(root.transform, palette);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Не удалось сохранить сцену {ScenePath}");
            }

            AddSceneToBuildSettings(ScenePath);
            Debug.Log($"[GreyboxRoomBuilder] Сцена сохранена: {ScenePath}");
        }

        /// <summary>
        /// Строит всю геометрию, свет и точки появления под root. Не трогает ассеты и сцены -
        /// поэтому годится для тестов.
        /// </summary>
        public static void BuildLayout(Transform root, GreyboxPalette palette)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var level = CreateChild(LevelName, root);
            BuildRoom(level, palette);
            BuildCorridor(level, palette);
            BuildDoor(level, palette);
            BuildSpawnPoints(root);
            BuildLighting(root);
            BuildPreviewCamera(root);
        }

        private static void BuildRoom(Transform level, GreyboxPalette palette)
        {
            var halfW = RoomWidth / 2f;
            var halfD = RoomDepth / 2f;
            var outerW = RoomWidth + WallThickness * 2f;
            var wallY = WallHeight / 2f;

            CreateBox("Room_Floor", level, new Vector3(0f, -WallThickness / 2f, 0f),
                new Vector3(RoomWidth, WallThickness, RoomDepth), palette.Floor);
            CreateBox("Room_Ceiling", level, new Vector3(0f, WallHeight + WallThickness / 2f, 0f),
                new Vector3(RoomWidth, WallThickness, RoomDepth), palette.Ceiling);

            CreateBox("Room_Wall_West", level, new Vector3(-halfW - WallThickness / 2f, wallY, 0f),
                new Vector3(WallThickness, WallHeight, RoomDepth), palette.Wall);
            CreateBox("Room_Wall_East", level, new Vector3(halfW + WallThickness / 2f, wallY, 0f),
                new Vector3(WallThickness, WallHeight, RoomDepth), palette.Wall);
            CreateBox("Room_Wall_South", level, new Vector3(0f, wallY, -halfD - WallThickness / 2f),
                new Vector3(outerW, WallHeight, WallThickness), palette.Wall);

            // Северная стена: два сегмента по бокам проёма и перемычка над ним.
            var segmentWidth = (outerW - DoorWidth) / 2f;
            var segmentX = DoorWidth / 2f + segmentWidth / 2f;
            CreateBox("Room_Wall_North_Left", level, new Vector3(-segmentX, wallY, DoorWallZ),
                new Vector3(segmentWidth, WallHeight, WallThickness), palette.Wall);
            CreateBox("Room_Wall_North_Right", level, new Vector3(segmentX, wallY, DoorWallZ),
                new Vector3(segmentWidth, WallHeight, WallThickness), palette.Wall);
            var lintelHeight = WallHeight - DoorHeight;
            CreateBox("Room_Wall_North_Lintel", level, new Vector3(0f, DoorHeight + lintelHeight / 2f, DoorWallZ),
                new Vector3(DoorWidth, lintelHeight, WallThickness), palette.Wall);
        }

        private static void BuildCorridor(Transform level, GreyboxPalette palette)
        {
            var startZ = DoorWallZ + WallThickness / 2f;
            var centerZ = startZ + CorridorLength / 2f;
            var wallY = WallHeight / 2f;
            var halfW = CorridorWidth / 2f;

            CreateBox("Corridor_Floor", level, new Vector3(0f, -WallThickness / 2f, centerZ),
                new Vector3(CorridorWidth, WallThickness, CorridorLength), palette.Floor);
            CreateBox("Corridor_Ceiling", level, new Vector3(0f, WallHeight + WallThickness / 2f, centerZ),
                new Vector3(CorridorWidth, WallThickness, CorridorLength), palette.Ceiling);
            CreateBox("Corridor_Wall_West", level, new Vector3(-halfW - WallThickness / 2f, wallY, centerZ),
                new Vector3(WallThickness, WallHeight, CorridorLength), palette.Wall);
            CreateBox("Corridor_Wall_East", level, new Vector3(halfW + WallThickness / 2f, wallY, centerZ),
                new Vector3(WallThickness, WallHeight, CorridorLength), palette.Wall);
            CreateBox("Corridor_Wall_End", level, new Vector3(0f, wallY, startZ + CorridorLength + WallThickness / 2f),
                new Vector3(CorridorWidth + WallThickness * 2f, WallHeight, WallThickness), palette.Wall);
        }

        private static void BuildDoor(Transform level, GreyboxPalette palette)
        {
            // Пивот на петле (левый край проёма): поворот по Y открывает дверь.
            var hinge = CreateChild(DoorName, level);
            hinge.localPosition = new Vector3(-DoorWidth / 2f, 0f, DoorWallZ);

            var panelWidth = DoorWidth - DoorGap * 2f;
            var panelHeight = DoorHeight - DoorGap;
            CreateBox(DoorPanelName, hinge, new Vector3(DoorWidth / 2f, panelHeight / 2f, 0f),
                new Vector3(panelWidth, panelHeight, DoorThickness), palette.Door);
        }

        private static void BuildSpawnPoints(Transform root)
        {
            var group = CreateChild(SpawnPointsName, root);
            var z = -RoomDepth / 2f + SpawnRowOffsetFromBackWall;
            var firstX = -SpawnSpacing * (SpawnPointCount - 1) / 2f;

            for (var i = 0; i < SpawnPointCount; i++)
            {
                var point = CreateChild(SpawnPointPrefix + (i + 1), group);
                point.localPosition = new Vector3(firstX + SpawnSpacing * i, 0f, z);
                point.localRotation = Quaternion.identity;
            }
        }

        private static void BuildLighting(Transform root)
        {
            var sun = CreateChild("Sun", root);
            sun.localRotation = Quaternion.Euler(SunRotation);
            var sunLight = sun.gameObject.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = SunIntensity;
            sunLight.shadows = LightShadows.Soft;

            var lamp = CreateChild("CeilingLamp", root);
            lamp.localPosition = new Vector3(0f, WallHeight - LampDropFromCeiling, 0f);
            var lampLight = lamp.gameObject.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.intensity = LampIntensity;
            lampLight.range = LampRange;
            lampLight.shadows = LightShadows.Soft;
        }

        private static void BuildPreviewCamera(Transform root)
        {
            // Временная камера, пока нет игрока: стоит у точек появления и смотрит на дверь.
            var cameraTransform = CreateChild("PreviewCamera", root);
            cameraTransform.localPosition = new Vector3(0f, EyeHeight, -RoomDepth / 2f + SpawnRowOffsetFromBackWall);
            cameraTransform.localRotation = Quaternion.identity;
            cameraTransform.gameObject.AddComponent<Camera>();
            cameraTransform.gameObject.tag = "MainCamera";
        }

        private static Transform CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void CreateBox(string name, Transform parent, Vector3 localCenter, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localScale = size;
            go.isStatic = true;

            if (material != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        private static GreyboxPalette CreateOrUpdatePalette()
        {
            EnsureFolder(MaterialsFolder);
            return new GreyboxPalette(
                CreateOrUpdateMaterial("Greybox_Floor", FloorColor),
                CreateOrUpdateMaterial("Greybox_Wall", WallColor),
                CreateOrUpdateMaterial("Greybox_Ceiling", CeilingColor),
                CreateOrUpdateMaterial("Greybox_Door", DoorColor));
        }

        private static Material CreateOrUpdateMaterial(string name, Color color)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find(LitShaderName);
                if (shader == null)
                {
                    throw new InvalidOperationException($"Шейдер {LitShaderName} не найден - проект должен быть на URP.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor(BaseColorProperty, color);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Any(s => s.path == scenePath))
            {
                return;
            }

            EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(scenePath, true)).ToArray();
        }
    }
}
