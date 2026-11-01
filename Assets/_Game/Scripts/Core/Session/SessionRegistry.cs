using System;

namespace Game.Core.Session
{
    /// <summary>
    /// Текущий сервис сессии. Регистрирует его сетевой слой; UI берёт отсюда, не зная о сети.
    /// </summary>
    public static class SessionRegistry
    {
        public static ISessionService Current { get; private set; }

        public static event Action<ISessionService> Changed;

        /// <summary>Первый зарегистрированный остаётся: дубликат (например, из повторно загруженной сцены) игнорируется.</summary>
        public static void Register(ISessionService service)
        {
            if (Current != null || service == null)
            {
                return;
            }

            Current = service;
            Changed?.Invoke(service);
        }

        public static void Unregister(ISessionService service)
        {
            if (!ReferenceEquals(Current, service))
            {
                return;
            }

            Current = null;
            Changed?.Invoke(null);
        }

        // Статика переживает выход из Play Mode при отключённой перезагрузке домена - сбрасываем явно.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Changed = null;
        }
    }
}
