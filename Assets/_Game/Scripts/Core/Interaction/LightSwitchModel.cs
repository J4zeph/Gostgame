using System;

namespace Game.Core.Interaction
{
    /// <summary>
    /// Состояние выключателя комнаты: горят ли его лампы. Включить или выключить может игрок, позже - призрак.
    /// </summary>
    public sealed class LightSwitchModel
    {
        public bool IsOn { get; private set; }

        /// <summary>Вызывается только при реальной смене состояния, аргумент - новое IsOn.</summary>
        public event Action<bool> Switched;

        public LightSwitchModel(bool startOn = false)
        {
            IsOn = startOn;
        }

        public void Toggle()
        {
            SetOn(!IsOn);
        }

        public void SetOn(bool on)
        {
            if (on == IsOn)
            {
                return;
            }

            IsOn = on;
            Switched?.Invoke(on);
        }
    }
}
