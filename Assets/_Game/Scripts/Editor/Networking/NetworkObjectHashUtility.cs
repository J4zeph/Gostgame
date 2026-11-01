using System;
using System.Reflection;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Networking
{
    /// <summary>
    /// Пересчитывает GlobalObjectIdHash у NetworkObject. По этому хешу клиент сопоставляет сетевые префабы
    /// и объекты сцены; при 0 подключение ломается. NGO считает хеш в редакторе в OnValidate, который
    /// при создании объектов кодом в batchmode не вызывается. Хеш берётся из GlobalObjectId, поэтому префаб
    /// и сцена должны быть уже сохранены (а сцена - в Build Settings).
    /// </summary>
    public static class NetworkObjectHashUtility
    {
        private const BindingFlags InternalInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string ValidateMethodName = "OnValidate";
        private const string HashFieldName = "GlobalObjectIdHash";

        public static void RefreshPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Нет префаба {prefabPath}");
            }

            foreach (var networkObject in prefab.GetComponentsInChildren<NetworkObject>(true))
            {
                Refresh(networkObject, prefabPath);
                EditorUtility.SetDirty(networkObject);
            }

            AssetDatabase.SaveAssets();
        }

        /// <returns>Сколько NetworkObject обработано.</returns>
        public static int RefreshScene(Scene scene)
        {
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var networkObject in root.GetComponentsInChildren<NetworkObject>(true))
                {
                    Refresh(networkObject, scene.path);
                    count++;
                }
            }

            return count;
        }

        private static void Refresh(NetworkObject networkObject, string owner)
        {
            var validate = typeof(NetworkObject).GetMethod(ValidateMethodName, InternalInstance);
            var hashField = typeof(NetworkObject).GetField(HashFieldName, InternalInstance);
            if (validate == null || hashField == null)
            {
                throw new InvalidOperationException(
                    $"В этой версии NGO нет NetworkObject.{ValidateMethodName}/{HashFieldName} - обнови NetworkObjectHashUtility.");
            }

            validate.Invoke(networkObject, null);

            if ((uint)hashField.GetValue(networkObject) == 0)
            {
                throw new InvalidOperationException(
                    $"GlobalObjectIdHash у {networkObject.name} ({owner}) остался 0: ассет или сцена не сохранены, либо сцены нет в Build Settings.");
            }
        }
    }
}
