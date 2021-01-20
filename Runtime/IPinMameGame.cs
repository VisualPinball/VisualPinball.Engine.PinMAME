using VisualPinball.Engine.Game.Engines;

namespace VisualPinball.Unity
{
	public interface IPinMameGame
	{
		string Name { get; }
		int Year { get; }
		string Manufacturer { get; }
		int IpdbId { get; }

		PinMameRom[] Roms { get; }

		GamelogicEngineSwitch[] AvailableSwitches { get; }
		GamelogicEngineLamp[] AvailableLamps { get; }
		GamelogicEngineCoil[] AvailableCoils { get; }
	}
}
