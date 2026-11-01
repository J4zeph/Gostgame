using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Точки появления игроков - дочерние объекты этого. Порядок детей = порядок выдачи.
    /// </summary>
    public sealed class SpawnPointGroup : MonoBehaviour
    {
        public int Count => transform.childCount;

        public Transform Get(int index)
        {
            return transform.GetChild(index);
        }
    }
}
