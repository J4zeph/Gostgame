using Game.Gameplay.Interaction;

namespace Game.UI.Hud
{
    /// <summary>
    /// Что показать у значка взаимодействия: видим ли он, клавиша и подпись действия.
    /// </summary>
    public readonly struct InteractionPromptState
    {
        public static readonly InteractionPromptState Hidden = default;

        public readonly bool Visible;
        public readonly string Key;
        public readonly string Action;

        public InteractionPromptState(bool visible, string key, string action)
        {
            Visible = visible;
            Key = key;
            Action = action;
        }

        public static InteractionPromptState For(IInteractable target, string keyLabel)
        {
            if (target == null)
            {
                return Hidden;
            }

            return new InteractionPromptState(true, keyLabel ?? string.Empty, target.Prompt ?? string.Empty);
        }
    }
}
