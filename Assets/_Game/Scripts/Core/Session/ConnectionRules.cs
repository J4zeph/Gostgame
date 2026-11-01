using System;

namespace Game.Core.Session
{
    /// <summary>
    /// Правила подключения к сессии: лимит игроков и выбор точки появления.
    /// </summary>
    public static class ConnectionRules
    {
        /// <param name="connectedPlayers">Сколько игроков уже в сессии (вместе с хостом).</param>
        public static bool CanAccept(int connectedPlayers, int maxPlayers)
        {
            return connectedPlayers >= 0 && connectedPlayers < maxPlayers;
        }

        /// <summary>Точка появления для нового игрока: по кругу, если точек меньше, чем игроков.</summary>
        public static int SpawnIndexFor(int connectedPlayers, int spawnPointCount)
        {
            if (spawnPointCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnPointCount), spawnPointCount, "Need at least one spawn point.");
            }

            return Math.Max(0, connectedPlayers) % spawnPointCount;
        }
    }
}
