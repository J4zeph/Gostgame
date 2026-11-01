using System;
using Game.Editor.Common;
using Game.Editor.Prefabs;
using Game.Network.Session;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Networking
{
    /// <summary>
    /// Ассеты сети: NetworkSettings (если нет) и наш список сетевых префабов с префабом игрока.
    /// NetworkManager в сцене ссылается на этот список, а не на автогенерируемый Assets/DefaultNetworkPrefabs.
    /// Каждый новый сетевой префаб добавлять сюда (CLAUDE.md: все сетевые префабы - в списке NetworkManager).
    /// </summary>
    public static class NetworkAssetsBuilder
    {
        public const string SettingsPath = "Assets/_Game/Data/Network/NetworkSettings.asset";
        public const string PrefabsListPath = "Assets/_Game/Data/Network/NetworkPrefabs.asset";

        [MenuItem("Tools/Gostgame/Build Network Assets")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        /// <summary>Вызывать после PlayerPrefabBuilder.Build - префаб игрока должен существовать.</summary>
        public static void Build()
        {
            EditorAssetUtility.LoadOrCreateScriptableObject<NetworkSettings>(SettingsPath);
            var list = EditorAssetUtility.LoadOrCreateScriptableObject<NetworkPrefabsList>(PrefabsListPath);

            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabBuilder.PrefabPath);
            if (player == null)
            {
                throw new InvalidOperationException($"Нет префаба игрока {PlayerPrefabBuilder.PrefabPath}");
            }

            if (!list.Contains(player))
            {
                list.Add(new NetworkPrefab { Prefab = player });
                EditorUtility.SetDirty(list);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[NetworkAssetsBuilder] Готово: {SettingsPath}, {PrefabsListPath}");
        }
    }
}
