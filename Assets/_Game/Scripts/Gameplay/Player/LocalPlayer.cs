using System;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// Игрок, которым управляют с этого компьютера. Регистрирует его PlayerInputReader;
    /// HUD и другие локальные системы подписываются на Changed.
    /// </summary>
    public static class LocalPlayer
    {
        public static PlayerController Current { get; private set; }

        public static event Action<PlayerController> Changed;

        public static void Set(PlayerController player)
        {
            if (Current == player)
            {
                return;
            }

            Current = player;
            Changed?.Invoke(player);
        }

        public static void Clear(PlayerController player)
        {
            if (Current != player)
            {
                return;
            }

            Current = null;
            Changed?.Invoke(null);
        }

        // Статика переживает выход из Play Mode при отключённой перезагрузке домена - сбрасываем явно.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Changed = null;
        }
    }
}
