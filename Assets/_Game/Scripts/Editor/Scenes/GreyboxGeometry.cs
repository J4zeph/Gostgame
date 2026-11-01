using UnityEngine;

namespace Game.Editor.Scenes
{
    /// <summary>
    /// Примитивы для серой заготовки уровня: пустые узлы и коробки с коллайдером.
    /// </summary>
    public static class GreyboxGeometry
    {
        public static Transform CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <param name="isStatic">Движущиеся части (полотно двери) статичными делать нельзя: static batching их зафиксирует.</param>
        /// <param name="withCollider">false - чисто визуальная деталь (плафон лампы).</param>
        public static GameObject CreateBox(string name, Transform parent, Vector3 localCenter, Vector3 size, Material material,
            bool isStatic = true, bool withCollider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localScale = size;
            go.isStatic = isStatic;

            if (!withCollider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            if (material != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return go;
        }
    }
}
