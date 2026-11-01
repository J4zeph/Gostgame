using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Common
{
    /// <summary>
    /// Общие операции с ассетами для editor-инструментов.
    /// </summary>
    public static class EditorAssetUtility
    {
        /// <summary>Создаёт папку (и родителей) через AssetDatabase, чтобы Unity сразу завела .meta.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>
        /// Загружает ScriptableObject по пути или создаёт новый со значениями по умолчанию.
        /// Существующий ассет не перезаписывается - в нём могут быть настроенные значения.
        /// </summary>
        public static T LoadOrCreateScriptableObject<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>Записывает ссылку в сериализованное поле (в том числе приватное с [SerializeField]).</summary>
        public static void SetReference(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.InvalidOperationException($"{target.GetType().Name} не имеет сериализованного поля {fieldName}");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
