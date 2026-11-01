using System;
using System.Collections.Generic;
using System.IO;
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
    /// Сцена дома (этап 1 GDD): геометрия из HouseLayout, сетевые двери, выключатели и шкаф, двор со стартовой зоной,
    /// NetworkManager, HUD и меню. Повторный запуск пересобирает сцену целиком.
    /// Batchmode: -executeMethod Game.Editor.Scenes.HouseBuilder.BuildFromCommandLine
    /// </summary>
    public static class HouseBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/House.unity";
        public const string RootName = "House";

        public const string LightSwitchSettingsPath = "Assets/_Game/Data/Interaction/LightSwitchSettings.asset";
        public const string WardrobeDoorSettingsPath = "Assets/_Game/Data/Interaction/WardrobeDoorSettings.asset";

        private const string MaterialsFolder = "Assets/_Game/Art/Materials";

        private const string FloorMaterial = "Greybox_Floor";
        private const string WallMaterial = "Greybox_Wall";
        private const string CeilingMaterial = "Greybox_Ceiling";
        private const string DoorMaterial = "Greybox_Door";
        private const string GroundMaterial = "Greybox_Ground";
        private const string FurnitureMaterial = "Greybox_Furniture";
        private const string FixtureMaterial = "Greybox_Fixture";

        private static readonly Color FloorColor = new Color(0.32f, 0.32f, 0.34f);
        private static readonly Color WallColor = new Color(0.62f, 0.62f, 0.6f);
        private static readonly Color CeilingColor = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color DoorColor = new Color(0.42f, 0.29f, 0.2f);
        private static readonly Color GroundColor = new Color(0.2f, 0.26f, 0.18f);
        private static readonly Color FurnitureColor = new Color(0.3f, 0.22f, 0.16f);
        private static readonly Color FixtureColor = new Color(0.85f, 0.85f, 0.8f);

        private const int HudSortingOrder = 0;
        private const int ConnectionMenuSortingOrder = 5;
        private const int MenuSortingOrder = 10;
        private const string MainCameraTag = "MainCamera";

        [MenuItem("Tools/Gostgame/Build House")]
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
            EditorAssetUtility.LoadOrCreateScriptableObject<LightSwitchSettings>(LightSwitchSettingsPath);
            EditorAssetUtility.LoadOrCreateScriptableObject<DoorSettings>(WardrobeDoorSettingsPath);
            EditorAssetUtility.EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildSystemsAndUi();

            // Ассеты загружаем после NewScene: смена сцены выгружает неиспользуемые ассеты,
            // и ссылки, полученные раньше, становятся пустыми.
            var palette = LoadPalette();
            var doorSettings = LoadRequired<DoorSettings>(PlayerPrefabBuilder.DoorSettingsPath);
            var wardrobeDoorSettings = LoadRequired<DoorSettings>(WardrobeDoorSettingsPath);
            var switchSettings = LoadRequired<LightSwitchSettings>(LightSwitchSettingsPath);
            var networkSettings = LoadRequired<NetworkSettings>(NetworkAssetsBuilder.SettingsPath);

            var root = new GameObject(RootName);
            HouseLayout.BuildLayout(root.transform, palette);

            // Двери, выключатели и шкаф - сетевые объекты сцены: состояние решает хост.
            var rooms = root.transform.Find($"{HouseLayout.LevelName}/{HouseLayout.RoomsName}");
            foreach (var room in HouseLayout.Rooms)
            {
                var node = rooms.Find(room.Name);
                AddNetworkDoor(node.Find(HouseLayout.DoorName).gameObject, doorSettings, networkSettings);
                AddNetworkLightSwitch(node, switchSettings, networkSettings);
            }

            var wardrobe = rooms.Find($"{HouseLayout.WardrobeRoom}/{HouseLayout.WardrobeName}");
            AddNetworkDoor(wardrobe.Find(HouseLayout.DoorName).gameObject, wardrobeDoorSettings, networkSettings);

            var spawnPoints = root.transform.Find(HouseLayout.SpawnPointsName).gameObject.AddComponent<SpawnPointGroup>();
            BuildMenuCamera(root.transform);
            BuildNetworkManager(spawnPoints, networkSettings);

            SaveSceneOrThrow(scene);
            SetBuildScenes(ScenePath);

            // Хеши сетевых объектов сцены считаются только у сохранённой сцены из Build Settings - отсюда второе сохранение.
            var networkObjects = NetworkObjectHashUtility.RefreshScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            SaveSceneOrThrow(scene);

            Debug.Log($"[HouseBuilder] Сцена сохранена: {ScenePath}, сетевых объектов: {networkObjects}");
        }

        private static void AddNetworkDoor(GameObject doorObject, DoorSettings settings, NetworkSettings networkSettings)
        {
            doorObject.AddComponent<NetworkObject>();
            var door = doorObject.AddComponent<Door>();
            EditorAssetUtility.SetReference(door, "_settings", settings);
            var networkDoor = doorObject.AddComponent<NetworkDoor>();
            EditorAssetUtility.SetReference(networkDoor, "_door", door);
            EditorAssetUtility.SetReference(networkDoor, "_settings", networkSettings);
        }

        private static void AddNetworkLightSwitch(Transform roomNode, LightSwitchSettings settings, NetworkSettings networkSettings)
        {
            var lamps = new List<Light>();
            foreach (Transform child in roomNode)
            {
                if (child.name.StartsWith(HouseLayout.LampPrefix, StringComparison.Ordinal))
                {
                    lamps.Add(child.GetComponent<Light>());
                }
            }

            var switchObject = roomNode.Find(HouseLayout.LightSwitchName).gameObject;
            switchObject.AddComponent<NetworkObject>();
            var lightSwitch = switchObject.AddComponent<LightSwitch>();
            EditorAssetUtility.SetReference(lightSwitch, "_settings", settings);
            EditorAssetUtility.SetReferences(lightSwitch, "_lamps", lamps.ToArray());
            var networkSwitch = switchObject.AddComponent<NetworkLightSwitch>();
            EditorAssetUtility.SetReference(networkSwitch, "_switch", lightSwitch);
            EditorAssetUtility.SetReference(networkSwitch, "_settings", networkSettings);
        }

        private static void SaveSceneOrThrow(UnityEngine.SceneManagement.Scene scene)
        {
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Не удалось сохранить сцену {ScenePath}");
            }
        }

        /// <summary>Камера сцены во дворе, смотрит на дом. Работает, пока нет своего игрока.</summary>
        private static void BuildMenuCamera(Transform root)
        {
            var cameraTransform = GreyboxGeometry.CreateChild("MenuCamera", root);
            cameraTransform.localPosition = HouseLayout.MenuCameraPosition;
            cameraTransform.localRotation = Quaternion.identity;
            cameraTransform.gameObject.AddComponent<Camera>();
            cameraTransform.gameObject.AddComponent<AudioListener>();
            cameraTransform.gameObject.AddComponent<FallbackCamera>();
            cameraTransform.gameObject.tag = MainCameraTag;
        }

        /// <summary>
        /// NetworkManager + UnityTransport + наш сервис сессии. Игрок - из нашего списка префабов.
        /// Все играют в одной сцене, загруженной до подключения; управление сценами NGO включено ради объектов сцены.
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
            // Нужно для объектов, расставленных в сцене (двери, выключатели): без управления сценами клиент
            // ищет их среди префабов и не находит. Режим синхронизации клиента задаёт NetworkSessionService.
            manager.NetworkConfig.EnableSceneManagement = true;
            EditorUtility.SetDirty(manager);

            var session = go.AddComponent<NetworkSessionService>();
            EditorAssetUtility.SetReference(session, "_settings", settings);
            EditorAssetUtility.SetReference(session, "_spawnPoints", spawnPoints);
        }

        /// <summary>Менеджер настроек и ввода, HUD и меню. Вызывать после NewScene (ассеты грузятся здесь).</summary>
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

        private static void CreateOrUpdatePalette()
        {
            EditorAssetUtility.EnsureFolder(MaterialsFolder);
            CreateOrUpdateMaterial(FloorMaterial, FloorColor);
            CreateOrUpdateMaterial(WallMaterial, WallColor);
            CreateOrUpdateMaterial(CeilingMaterial, CeilingColor);
            CreateOrUpdateMaterial(DoorMaterial, DoorColor);
            CreateOrUpdateMaterial(GroundMaterial, GroundColor);
            CreateOrUpdateMaterial(FurnitureMaterial, FurnitureColor);
            CreateOrUpdateMaterial(FixtureMaterial, FixtureColor);
        }

        private static GreyboxPalette LoadPalette()
        {
            return new GreyboxPalette(
                LoadRequired<Material>(MaterialPath(FloorMaterial)),
                LoadRequired<Material>(MaterialPath(WallMaterial)),
                LoadRequired<Material>(MaterialPath(CeilingMaterial)),
                LoadRequired<Material>(MaterialPath(DoorMaterial)),
                LoadRequired<Material>(MaterialPath(GroundMaterial)),
                LoadRequired<Material>(MaterialPath(FurnitureMaterial)),
                LoadRequired<Material>(MaterialPath(FixtureMaterial)));
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
            EditorAssetUtility.CreateOrUpdateLitMaterial(MaterialPath(name), color);
        }

        /// <summary>Сцена дома - первой в Build Settings; записи об удалённых сценах убираются.</summary>
        private static void SetBuildScenes(string scenePath)
        {
            var others = EditorBuildSettings.scenes
                .Where(s => s.path != scenePath && File.Exists(s.path));
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) }.Concat(others).ToArray();
        }
    }
}
