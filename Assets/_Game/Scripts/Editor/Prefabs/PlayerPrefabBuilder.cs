using System;
using Game.Editor.Common;
using Game.Gameplay.Equipment;
using Game.Gameplay.Interaction;
using Game.Gameplay.Player;
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
                var controller = root.AddComponent<CharacterController>();
                controller.height = playerSettings.Height;
                controller.radius = playerSettings.Radius;
                controller.center = new Vector3(0f, playerSettings.Height / 2f, 0f);

                var pivot = new GameObject(CameraPivotName).transform;
                pivot.SetParent(root.transform, false);
                pivot.localPosition = new Vector3(0f, playerSettings.EyeHeight, 0f);
                var camera = pivot.gameObject.AddComponent<Camera>();
                camera.nearClipPlane = CameraNearClip;
                pivot.gameObject.AddComponent<AudioListener>();
                pivot.gameObject.tag = MainCameraTag;

                var flashlightTransform = new GameObject(FlashlightName).transform;
                flashlightTransform.SetParent(pivot, false);
                flashlightTransform.localPosition = FlashlightOffset;
                var light = flashlightTransform.gameObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.shadows = LightShadows.Soft;

                var reader = root.AddComponent<PlayerInputReader>();
                var player = root.AddComponent<PlayerController>();
                EditorAssetUtility.SetReference(player, "_settings", playerSettings);
                EditorAssetUtility.SetReference(player, "_cameraPivot", pivot);
                EditorAssetUtility.SetReference(player, "_inputSourceBehaviour", reader);

                var interactor = root.AddComponent<PlayerInteractor>();
                EditorAssetUtility.SetReference(interactor, "_player", player);
                EditorAssetUtility.SetReference(interactor, "_viewPoint", pivot);

                var flashlight = flashlightTransform.gameObject.AddComponent<Flashlight>();
                EditorAssetUtility.SetReference(flashlight, "_settings", flashlightSettings);
                EditorAssetUtility.SetReference(flashlight, "_owner", player);

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var success);
                if (!success)
                {
                    throw new InvalidOperationException($"Не удалось сохранить префаб {PrefabPath}");
                }

                Debug.Log($"[PlayerPrefabBuilder] Префаб сохранён: {PrefabPath}");
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
