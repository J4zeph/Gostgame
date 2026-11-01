using System;
using Game.Editor.Common;
using Game.Editor.Networking;
using Game.Gameplay.Equipment;
using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
using Game.Network.Equipment;
using Game.Network.Player;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using PlayerSettings = Game.Gameplay.Player.PlayerSettings;

namespace Game.Editor.Prefabs
{
    /// <summary>
    /// Создаёт ассеты настроек (если их нет) и пересобирает префаб игрока.
    /// Настраивать значения - в ассетах _Game/Data, а не в префабе: префаб перезаписывается при каждом запуске.
    /// Batchmode: -executeMethod Game.Editor.Prefabs.PlayerPrefabBuilder.BuildFromCommandLine
    /// </summary>
    public static class PlayerPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/Player.prefab";
        public const string PlayerSettingsPath = "Assets/_Game/Data/Player/PlayerSettings.asset";
        public const string FlashlightSettingsPath = "Assets/_Game/Data/Equipment/FlashlightSettings.asset";
        public const string DoorSettingsPath = "Assets/_Game/Data/Interaction/DoorSettings.asset";

        public const string BodyMaterialPath = "Assets/_Game/Art/Materials/Player_Body.mat";

        public const string CameraPivotName = "CameraPivot";
        public const string FlashlightName = "Flashlight";
        public const string BodyName = "Body";
        public const string VisorName = "Visor";

        private static readonly Color BodyColor = new Color(0.25f, 0.45f, 0.7f);
        // Капсула-примитив Unity: высота 2, радиус 0.5 при масштабе 1.
        private const float CapsulePrimitiveHeight = 2f;
        private const float CapsulePrimitiveDiameter = 1f;
        // «Козырёк» на уровне глаз показывает другим, куда смотрит игрок.
        private static readonly Vector3 VisorSize = new Vector3(0.3f, 0.08f, 0.12f);

        private const string MainCameraTag = "MainCamera";
        private const float CameraNearClip = 0.05f;
        // Снаружи капсулы тела, чтобы её тень не гасила фонарик.
        private static readonly Vector3 FlashlightOffset = new Vector3(0.2f, -0.15f, 0.35f);

        [MenuItem("Tools/Gostgame/Build Player Prefab")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static GameObject Build()
        {
            var playerSettings = EditorAssetUtility.LoadOrCreateScriptableObject<PlayerSettings>(PlayerSettingsPath);
            var flashlightSettings = EditorAssetUtility.LoadOrCreateScriptableObject<FlashlightSettings>(FlashlightSettingsPath);
            EditorAssetUtility.LoadOrCreateScriptableObject<DoorSettings>(DoorSettingsPath);
            var bodyMaterial = EditorAssetUtility.CreateOrUpdateLitMaterial(BodyMaterialPath, BodyColor);
            EditorAssetUtility.EnsureFolder(System.IO.Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));

            var root = new GameObject("Player");
            try
            {
                // Сеть: NetworkObject первым - NetworkBehaviour'ам он нужен на корне.
                root.AddComponent<NetworkObject>();
                var bodySync = root.AddComponent<NetworkTransform>();
                ConfigureOwnerTransform(bodySync);
                bodySync.SyncRotAngleX = false;
                bodySync.SyncRotAngleZ = false;

                var controller = root.AddComponent<CharacterController>();
                controller.height = playerSettings.Height;
                controller.radius = playerSettings.Radius;
                controller.center = new Vector3(0f, playerSettings.Height / 2f, 0f);

                var pivot = new GameObject(CameraPivotName).transform;
                pivot.SetParent(root.transform, false);
                pivot.localPosition = new Vector3(0f, playerSettings.EyeHeight, 0f);

                // Наклон головы владельца виден другим (фонарик светит туда, куда он смотрит).
                var headSync = pivot.gameObject.AddComponent<NetworkTransform>();
                ConfigureOwnerTransform(headSync);
                headSync.InLocalSpace = true;
                headSync.SyncPositionX = false;
                headSync.SyncPositionY = false;
                headSync.SyncPositionZ = false;
                headSync.SyncRotAngleY = false;
                headSync.SyncRotAngleZ = false;

                // Камера, слух, угол обзора и ввод выключены: их включает NetworkPlayer только у своего игрока.
                var camera = pivot.gameObject.AddComponent<Camera>();
                camera.nearClipPlane = CameraNearClip;
                camera.enabled = false;
                var listener = pivot.gameObject.AddComponent<AudioListener>();
                listener.enabled = false;
                var fieldOfView = pivot.gameObject.AddComponent<PlayerCameraFov>();
                fieldOfView.enabled = false;
                pivot.gameObject.tag = MainCameraTag;

                var body = BuildBody(root.transform, pivot, playerSettings, bodyMaterial);

                var flashlightTransform = new GameObject(FlashlightName).transform;
                flashlightTransform.SetParent(pivot, false);
                flashlightTransform.localPosition = FlashlightOffset;
                var light = flashlightTransform.gameObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.shadows = LightShadows.Soft;

                // Сначала контроллер: PlayerInputReader требует его и иначе добавил бы второй.
                var player = root.AddComponent<PlayerController>();
                var reader = root.AddComponent<PlayerInputReader>();
                reader.enabled = false;
                EditorAssetUtility.SetReference(player, "_settings", playerSettings);
                EditorAssetUtility.SetReference(player, "_cameraPivot", pivot);

                var interactor = root.AddComponent<PlayerInteractor>();
                EditorAssetUtility.SetReference(interactor, "_player", player);
                EditorAssetUtility.SetReference(interactor, "_viewPoint", pivot);

                var flashlight = flashlightTransform.gameObject.AddComponent<Flashlight>();
                EditorAssetUtility.SetReference(flashlight, "_settings", flashlightSettings);
                EditorAssetUtility.SetReference(flashlight, "_owner", player);

                var networkPlayer = root.AddComponent<NetworkPlayer>();
                EditorAssetUtility.SetReference(networkPlayer, "_player", player);
                EditorAssetUtility.SetReference(networkPlayer, "_input", reader);
                EditorAssetUtility.SetReference(networkPlayer, "_camera", camera);
                EditorAssetUtility.SetReference(networkPlayer, "_listener", listener);
                EditorAssetUtility.SetReference(networkPlayer, "_fieldOfView", fieldOfView);
                EditorAssetUtility.SetReference(networkPlayer, "_body", body);

                var networkFlashlight = root.AddComponent<NetworkFlashlight>();
                EditorAssetUtility.SetReference(networkFlashlight, "_flashlight", flashlight);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var success);
                if (!success)
                {
                    throw new InvalidOperationException($"Не удалось сохранить префаб {PrefabPath}");
                }

                // Хеш сетевого объекта считается только у сохранённого префаба.
                NetworkObjectHashUtility.RefreshPrefab(PrefabPath);

                Debug.Log($"[PlayerPrefabBuilder] Префаб сохранён: {PrefabPath}");
                return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>Капсула тела (поворачивается с телом) и козырёк (наклоняется с головой). Без коллайдеров: столкновения - у CharacterController.</summary>
        private static PlayerBody BuildBody(Transform root, Transform pivot, PlayerSettings settings, Material material)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = BodyName;
            UnityEngine.Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.transform.SetParent(root, false);
            capsule.transform.localPosition = new Vector3(0f, settings.Height / 2f, 0f);
            var diameter = settings.Radius * 2f / CapsulePrimitiveDiameter;
            capsule.transform.localScale = new Vector3(diameter, settings.Height / CapsulePrimitiveHeight, diameter);

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = VisorName;
            UnityEngine.Object.DestroyImmediate(visor.GetComponent<Collider>());
            visor.transform.SetParent(pivot, false);
            visor.transform.localPosition = new Vector3(0f, 0f, settings.Radius);
            visor.transform.localScale = VisorSize;

            var renderers = new Renderer[] { capsule.GetComponent<MeshRenderer>(), visor.GetComponent<MeshRenderer>() };
            foreach (var bodyRenderer in renderers)
            {
                bodyRenderer.sharedMaterial = material;
            }

            var body = root.gameObject.AddComponent<PlayerBody>();
            EditorAssetUtility.SetReferences(body, "_renderers", renderers);
            return body;
        }

        // Перемещение игрока двигает владелец (решение пользователя, CLAUDE.md). Масштаб не синхронизируем.
        private static void ConfigureOwnerTransform(NetworkTransform sync)
        {
            sync.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            sync.SyncScaleX = false;
            sync.SyncScaleY = false;
            sync.SyncScaleZ = false;
        }
    }
}
