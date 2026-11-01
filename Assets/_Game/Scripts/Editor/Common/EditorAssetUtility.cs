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

        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string BaseColorProperty = "_BaseColor";

        /// <summary>Создаёт (если нет) материал URP Lit по пути и задаёт ему цвет.</summary>
        public static Material CreateOrUpdateLitMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find(LitShaderName);
                if (shader == null)
                {
                    throw new System.InvalidOperationException($"Шейдер {LitShaderName} не найден - проект должен быть на URP.");
                }

                EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor(BaseColorProperty, color);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
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

        /// <summary>Записывает массив ссылок в сериализованное поле-массив.</summary>
        public static void SetReferences(Object target, string fieldName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null || !property.isArray)
            {
                throw new System.InvalidOperationException($"{target.GetType().Name} не имеет сериализованного поля-массива {fieldName}");
            }

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
