using System;
using Game.Core.Timing;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class CountdownTests
    {
        [Test]
        public void Tick_WhenNotStarted_DoesNotChangeRemaining()
        {
            var countdown = new Countdown(5f);

            countdown.Tick(1f);

            Assert.AreEqual(5f, countdown.Remaining);
            Assert.IsFalse(countdown.IsRunning);
        }

        [Test]
        public void Tick_AfterStart_DecreasesRemaining()
        {
            var countdown = new Countdown(5f);
            countdown.Start();

            countdown.Tick(2f);

            Assert.AreEqual(3f, countdown.Remaining, 1e-5f);
            Assert.IsTrue(countdown.IsRunning);
        }

        [Test]
        public void Tick_PastDuration_FinishesOnceAndClampsToZero()
        {
            var countdown = new Countdown(1f);
            var finishedCount = 0;
            countdown.Finished += () => finishedCount++;
            countdown.Start();

            countdown.Tick(0.6f);
            countdown.Tick(0.6f);
            countdown.Tick(0.6f);

            Assert.AreEqual(0f, countdown.Remaining);
            Assert.IsTrue(countdown.IsFinished);
            Assert.IsFalse(countdown.IsRunning);
            Assert.AreEqual(1, finishedCount);
        }

        [Test]
        public void Constructor_NonPositiveDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Countdown(0f));
        }
    }
}
