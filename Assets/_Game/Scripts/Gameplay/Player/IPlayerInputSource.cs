namespace Game.Gameplay.Player
{
    /// <summary>
    /// Источник ввода игрока: клавиатура и мышь локально, сеть для чужих игроков, заглушка в тестах.
    /// </summary>
    public interface IPlayerInputSource
    {
        PlayerInputFrame ReadFrame();
    }
}
