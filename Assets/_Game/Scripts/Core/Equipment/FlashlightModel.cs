using System;

namespace Game.Core.Equipment
{
    /// <summary>
    /// Состояние фонарика: включён или нет.
    /// </summary>
    public sealed class FlashlightModel
    {
        public bool IsOn { get; private set; }

        /// <summary>Вызывается только при реальной смене состояния.</summary>
        public event Action<bool> Switched;

        public FlashlightModel(bool startOn = false)
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
