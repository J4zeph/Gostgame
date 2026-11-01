using System;

namespace Game.Core.Session
{
    /// <summary>
    /// Игровая сессия: создать, подключиться, выйти. Реализация - в Network/, UI знает только этот интерфейс.
    /// </summary>
    public interface ISessionService
    {
        SessionState State { get; }

        /// <summary>Причина последнего отказа или разрыва; пусто, если ошибок не было.</summary>
        string LastError { get; }

        /// <summary>Адрес по умолчанию для поля «адрес хоста».</summary>
        string DefaultAddress { get; }

        event Action StateChanged;

        void StartHost();

        void StartClient(string address);

        void Leave();
    }
}
