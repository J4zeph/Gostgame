using Game.Core.Interaction;
using Game.Gameplay.Player;
using Game.Network.Session;
using Unity.Netcode;
using UnityEngine;

namespace Game.Network.Interaction
{
    /// <summary>
    /// Проверка на хосте: дотягивается ли игрок-отправитель запроса до объекта мира.
    /// Позицию берём у объекта игрока на хосте, клиентским данным не доверяем.
    /// </summary>
    public static class HostReachCheck
    {
        public static bool CanReach(NetworkManager manager, ulong clientId, Collider target, NetworkSettings settings)
        {
            if (manager == null || target == null
                || !manager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            {
                return false;
            }

            var player = client.PlayerObject.GetComponent<PlayerController>();
            if (player == null || player.Settings == null)
            {
                return false;
            }

            var eye = player.CameraPivot != null ? player.CameraPivot.position : player.transform.position;
            var slack = settings != null ? settings.ReachSlack : 0f;
            return ReachRules.IsWithinReach(eye, target.ClosestPoint(eye), player.Settings.InteractDistance, slack);
        }
    }
}
