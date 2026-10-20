namespace VisualPinball.Engine.PinMAME
{
	/// <summary>Maps the application's simulation clock to the current ROM's elapsed time.</summary>
	internal sealed class PinMameTimeFence
	{
		// Zero disables PinMAME's fence; retain the startup fence on the first tick.
		internal const double InitialFenceSeconds = 0.01;
		private double? _origin;

		internal void Reset() => _origin = null;

		internal double ToEmulationTime(double simulationSeconds)
		{
			_origin ??= simulationSeconds;
			return System.Math.Max(InitialFenceSeconds, simulationSeconds - _origin.Value + InitialFenceSeconds);
		}
	}
}
