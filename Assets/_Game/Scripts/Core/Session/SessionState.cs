namespace Game.Core.Session
{
    public enum SessionState
    {
        /// <summary>Не в игре: показываем меню подключения.</summary>
        Offline,
        /// <summary>Создаём игру или подключаемся.</summary>
        Starting,
        /// <summary>Этот компьютер - хост (сервер + игрок).</summary>
        Host,
        /// <summary>Подключены к чужой игре.</summary>
        Client,
    }
}
