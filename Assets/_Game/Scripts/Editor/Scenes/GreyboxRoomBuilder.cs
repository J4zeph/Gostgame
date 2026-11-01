using System;
using System.Linq;
using Game.Editor.Common;
using Game.Editor.Networking;
using Game.Editor.Prefabs;
using Game.Editor.UiAssets;
using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
using Game.Gameplay.Settings;
using Game.Network.Interaction;
using Game.Network.Session;
using Game.UI.Hud;
using Game.UI.Session;
using Game.UI.Settings;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Editor.Scenes
{
    /// <summary>
    /// Сцена-заготовка для первых тестов: комната с дверным проёмом и дверью, коридор за ней,
    /// четыре точки появления, свет и игрок на первой точке. Повторный запуск пересобирает сцену целиком.
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
            CreateOrUpdatePalette();
            UiAssetsBuilder.Build();
            PlayerPrefabBuilder.Build();
            NetworkAssetsBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSystemsAndUi();

            // Ассеты загружаем после NewScene: смена сцены выгружает неиспользуемые ассеты,
            // и ссылки, полученные раньше, становятся пустыми.
            var palette = LoadPalette();
            var doorSettings = LoadRequired<DoorSettings>(PlayerPrefabBuilder.DoorSettingsPath);
            var networkSettings = LoadRequired<NetworkSettings>(NetworkAssetsBuilder.SettingsPath);

            var root = new GameObject(RootName);
            BuildLayout(root.transform, palette);

            // Дверь - сетевой объект сцены: состояние решает хост.
            var doorObject = root.transform.Find($"{LevelName}/{DoorName}").gameObject;
            doorObject.AddComponent<NetworkObject>();
            var door = doorObject.AddComponent<Door>();
            EditorAssetUtility.SetReference(door, "_settings", doorSettings);
            var networkDoor = doorObject.AddComponent<NetworkDoor>();
            EditorAssetUtility.SetReference(networkDoor, "_door", door);
            EditorAssetUtility.SetReference(networkDoor, "_settings", networkSettings);

            var spawnPoints = root.transform.Find(SpawnPointsName).gameObject.AddComponent<SpawnPointGroup>();
            BuildMenuCamera(root.transform);
            BuildNetworkManager(spawnPoints, networkSettings);

            SaveSceneOrThrow(scene);
            AddSceneToBuildSettings(ScenePath);

            // Хеши сетевых объектов сцены считаются только у сохранённой сцены из Build Settings - отсюда второе сохранение.
            var networkObjects = NetworkObjectHashUtility.RefreshScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            SaveSceneOrThrow(scene);

            Debug.Log($"[GreyboxRoomBuilder] Сцена сохранена: {ScenePath}, сетевых объектов: {networkObjects}");
        }

        /// <summary>
        /// Строит всю геометрию, свет и точки появления под root. Не трогает ассеты и сцены -
        /// поэтому годится для тестов. Компонент двери и игрок добавляются в BuildScene (им нужны ассеты).
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
        }

        private static void SaveSceneOrThrow(UnityEngine.SceneManagement.Scene scene)
        {
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Не удалось сохранить сцену {ScenePath}");
            }
        }

        private const int HudSortingOrder = 0;
        private const int ConnectionMenuSortingOrder = 5;
        private const int MenuSortingOrder = 10;
        private const float MenuCameraHeight = 1.65f;
        private const string MainCameraTag = "MainCamera";

        /// <summary>Камера сцены у точек появления, смотрит на дверь. Работает, пока нет своего игрока.</summary>
        private static void BuildMenuCamera(Transform root)
        {
            var cameraTransform = CreateChild("MenuCamera", root);
            cameraTransform.localPosition = new Vector3(0f, MenuCameraHeight, -RoomDepth / 2f + SpawnRowOffsetFromBackWall);
            cameraTransform.localRotation = Quaternion.identity;
            cameraTransform.gameObject.AddComponent<Camera>();
            cameraTransform.gameObject.AddComponent<AudioListener>();
            cameraTransform.gameObject.AddComponent<FallbackCamera>();
            cameraTransform.gameObject.tag = MainCameraTag;
        }

        /// <summary>
        /// NetworkManager + UnityTransport + наш сервис сессии. Игрок - из нашего списка префабов.
        /// Управление сценами NGO пока выключено: все играют в одной сцене, загруженной до подключения.
        /// </summary>
        private static void BuildNetworkManager(SpawnPointGroup spawnPoints, NetworkSettings settings)
        {
            var playerPrefab = LoadRequired<GameObject>(PlayerPrefabBuilder.PrefabPath);
            var prefabsList = LoadRequired<NetworkPrefabsList>(NetworkAssetsBuilder.PrefabsListPath);

            var go = new GameObject("NetworkManager");
            var manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabsList);
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.EnableSceneManagement = false;
            EditorUtility.SetDirty(manager);

            var session = go.AddComponent<NetworkSessionService>();
            EditorAssetUtility.SetReference(session, "_settings", settings);
            EditorAssetUtility.SetReference(session, "_spawnPoints", spawnPoints);
        }

        /// <summary>Менеджер настроек и ввода, HUD и меню настроек. Вызывать после NewScene (ассеты грузятся здесь).</summary>
        private static void BuildSystemsAndUi()
        {
            var config = LoadRequired<ControlSettingsConfig>(UiAssetsBuilder.ControlSettingsConfigPath);
            var panelSettings = LoadRequired<PanelSettings>(UiAssetsBuilder.PanelSettingsPath);
            var hudStyle = LoadRequired<StyleSheet>(UiAssetsBuilder.HudStylePath);
            var menuStyle = LoadRequired<StyleSheet>(UiAssetsBuilder.SettingsMenuStylePath);

            var manager = new GameObject("SettingsManager").AddComponent<SettingsManager>();
            EditorAssetUtility.SetReference(manager, "_config", config);

            var hud = CreateUiDocument("HUD", panelSettings, HudSortingOrder).AddComponent<HudView>();
            EditorAssetUtility.SetReference(hud, "_styleSheet", hudStyle);

            var menu = CreateUiDocument("SettingsMenu", panelSettings, MenuSortingOrder).AddComponent<SettingsMenuView>();
            EditorAssetUtility.SetReference(menu, "_styleSheet", menuStyle);

            var connection = CreateUiDocument("ConnectionMenu", panelSettings, ConnectionMenuSortingOrder).AddComponent<ConnectionMenuView>();
            EditorAssetUtility.SetReference(connection, "_styleSheet", menuStyle);
        }

        private static GameObject CreateUiDocument(string name, PanelSettings panelSettings, int sortingOrder)
        {
            var go = new GameObject(name);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.sortingOrder = sortingOrder;
            return go;
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
            // Дверь двигается - статичной её делать нельзя (static batching зафиксирует полотно).
            CreateBox(DoorPanelName, hinge, new Vector3(DoorWidth / 2f, panelHeight / 2f, 0f),
                new Vector3(panelWidth, panelHeight, DoorThickness), palette.Door, isStatic: false);
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

        private static Transform CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void CreateBox(string name, Transform parent, Vector3 localCenter, Vector3 size, Material material,
            bool isStatic = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localScale = size;
            go.isStatic = isStatic;

            if (material != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        private const string FloorMaterial = "Greybox_Floor";
        private const string WallMaterial = "Greybox_Wall";
        private const string CeilingMaterial = "Greybox_Ceiling";
        private const string DoorMaterial = "Greybox_Door";

        private static void CreateOrUpdatePalette()
        {
            EditorAssetUtility.EnsureFolder(MaterialsFolder);
            CreateOrUpdateMaterial(FloorMaterial, FloorColor);
            CreateOrUpdateMaterial(WallMaterial, WallColor);
            CreateOrUpdateMaterial(CeilingMaterial, CeilingColor);
            CreateOrUpdateMaterial(DoorMaterial, DoorColor);
        }

        private static GreyboxPalette LoadPalette()
        {
            return new GreyboxPalette(
                LoadRequired<Material>(MaterialPath(FloorMaterial)),
                LoadRequired<Material>(MaterialPath(WallMaterial)),
                LoadRequired<Material>(MaterialPath(CeilingMaterial)),
                LoadRequired<Material>(MaterialPath(DoorMaterial)));
        }

        private static string MaterialPath(string name) => $"{MaterialsFolder}/{name}.mat";

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Не найден ассет {path}");
            }

            return asset;
        }

        private static void CreateOrUpdateMaterial(string name, Color color)
        {
            var path = MaterialPath(name);
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
