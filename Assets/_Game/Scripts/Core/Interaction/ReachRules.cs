using UnityEngine;

namespace Game.Core.Interaction
{
    /// <summary>
    /// Проверка дотягивается ли игрок до объекта. Хост проверяет так запросы клиентов: клиенту не доверяем.
    /// </summary>
    public static class ReachRules
    {
        /// <param name="slack">Запас на сетевую задержку: позиция игрока на хосте немного отстаёт.</param>
        public static bool IsWithinReach(Vector3 eye, Vector3 closestPointOnTarget, float reach, float slack)
        {
            var limit = reach + Mathf.Max(0f, slack);
            return (closestPointOnTarget - eye).sqrMagnitude <= limit * limit;
        }
    }
}
