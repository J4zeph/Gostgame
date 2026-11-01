namespace Game.Gameplay.Player
{
    /// <summary>
    /// Клавиша, которую игрок может переназначить в меню: идентификатор и подпись для UI.
    /// </summary>
    public readonly struct RebindableBinding
    {
        public readonly string Id;
        public readonly string Label;

        public RebindableBinding(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }
}
