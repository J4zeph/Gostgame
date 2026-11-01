using System;

namespace Game.Core.Timing
{
    /// <summary>
    /// Обратный отсчёт без привязки к движку: время подаётся снаружи через Tick.
    /// </summary>
    public sealed class Countdown
    {
        public float Duration { get; }
        public float Remaining { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsFinished => Remaining <= 0f;

        public event Action Finished;

        public Countdown(float duration)
        {
            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be positive.");
            }

            Duration = duration;
            Remaining = duration;
        }

        public void Start()
        {
            Remaining = Duration;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || deltaTime <= 0f)
            {
                return;
            }

            Remaining = Math.Max(0f, Remaining - deltaTime);
            if (Remaining <= 0f)
            {
                IsRunning = false;
                Finished?.Invoke();
            }
        }
    }
}
