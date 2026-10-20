using NUnit.Framework;

namespace VisualPinball.Engine.PinMAME.Test
{
	public class PinMameTimeFenceTests
	{
		[TestCase(0)]
		[TestCase(1800)]
		public void MenuTimeDoesNotAdvanceRom(double applicationTime)
		{
			var clock = new PinMameTimeFence();
			Assert.That(clock.ToEmulationTime(applicationTime), Is.EqualTo(0.01).Within(1e-9));
			Assert.That(clock.ToEmulationTime(applicationTime + 1), Is.EqualTo(1.01).Within(1e-9));
		}

		[Test]
		public void BackwardsClockNeverDisablesFence()
		{
			var clock = new PinMameTimeFence();
			clock.ToEmulationTime(100);
			Assert.That(clock.ToEmulationTime(99), Is.EqualTo(0.01));
		}

		[Test]
		public void ReloadResetsRomClockAndPauseKeepsFenceEnabled()
		{
			var clock = new PinMameTimeFence();
			clock.ToEmulationTime(100);
			Assert.That(clock.ToEmulationTime(105), Is.EqualTo(5.01).Within(1e-9));
			Assert.That(clock.ToEmulationTime(105), Is.EqualTo(5.01).Within(1e-9));
			clock.Reset();
			Assert.That(clock.ToEmulationTime(300), Is.EqualTo(0.01).Within(1e-9));
			Assert.That(clock.ToEmulationTime(300.001), Is.EqualTo(0.011).Within(1e-9));
		}
	}
}
