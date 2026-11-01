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

        public const string CameraPivotName = "CameraPivot";
        public const string FlashlightName = "Flashlight";

        private const string MainCameraTag = "MainCamera";
        private const float CameraNearClip = 0.05f;
        private static readonly Vector3 FlashlightOffset = new Vector3(0.2f, -0.15f, 0.2f);

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
