// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

// ReSharper disable StringLiteralTypo

using System;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.PinMAME.MPUs;

namespace VisualPinball.Engine.PinMAME.Games
{
	[Serializable]
	public class FlashGordon : Bally
	{
		public override string Name => "Flash Gordon";
		public override string Id => "fg";
		public override int Year => 1980;
		public override string Manufacturer => "Bally";
		public override int IpdbId => 874;

		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("flashgdn"),
			new PinMameRom("flashgdf", language:PinMameRomLanguage.French),
			new PinMameRom("flashgda", description: "Free Play"),
			new PinMameRom("flashgfa", description: "Free Play", language: PinMameRomLanguage.French),
			new PinMameRom("flashgdp", "Rev.1", "Prototype"),
			new PinMameRom("flashgp2", "Rev.2", "Prototype"),
			new PinMameRom("flashgdv", description:"Vocalizer Sound"),
			new PinMameRom("flashgva", description: "Vocalizer Sound Free Play"),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new GamelogicEngineSwitch("01") { Description = "2 Left & Right R.O. Buttons" },
			new GamelogicEngineSwitch("02") { Description = "3 Shooter Lane R.O. Buttons" },
			new GamelogicEngineSwitch("03") { Description = "Top Single Drop Target" },
			new GamelogicEngineSwitch("04") { Description = "Shooter Lane Rollover" },
			new GamelogicEngineSwitch("05") { Description = "Drop Target 50 Point Reb. (2)" },
			new GamelogicEngineSwitch("06") { Description = "Credit Button" },
			new GamelogicEngineSwitch("07") { Description = "Tilt (3)" },
			new GamelogicEngineSwitch("08") { Description = "Outhole" },
			new GamelogicEngineSwitch("09") { Description = "Coin III (right)" },
			new GamelogicEngineSwitch("10") { Description = "Coin I (left)" },
			new GamelogicEngineSwitch("11") { Description = "Coin II (middle)" },
			new GamelogicEngineSwitch("12") { Description = "Lower Right Side Target" },
			new GamelogicEngineSwitch("13") { Description = "Flip Feed Lane (right)" },
			new GamelogicEngineSwitch("14") { Description = "Flip Feed Lane (left)" },
			new GamelogicEngineSwitch("15") { Description = "Upper Right Side Target" },
			new GamelogicEngineSwitch("16") { Description = "Slam (2)" },
			new GamelogicEngineSwitch("17") { Description = "4 Drop Target A (bottom)" },
			new GamelogicEngineSwitch("18") { Description = "4 Drop Target B" },
			new GamelogicEngineSwitch("19") { Description = "4 Drop Target C" },
			new GamelogicEngineSwitch("20") { Description = "4 Drop Target D (top)" },
			new GamelogicEngineSwitch("21") { Description = "1 Drop Target (top)" },
			new GamelogicEngineSwitch("22") { Description = "2 Drop Target (middle)" },
			new GamelogicEngineSwitch("23") { Description = "3 Drop Target (bottom)" },
			new GamelogicEngineSwitch("24") { Description = "Top Target" },
			new GamelogicEngineSwitch("25") { Description = "1st Inline Drop Target" },
			new GamelogicEngineSwitch("26") { Description = "2nd Inline Drop Target" },
			new GamelogicEngineSwitch("27") { Description = "3rd Inline Drop Target" },
			new GamelogicEngineSwitch("28") { Description = "Inline Back Target" },
			new GamelogicEngineSwitch("29") { Description = "10 Point Rebound (2)" },
			new GamelogicEngineSwitch("30") { Description = "Saucer" },
			new GamelogicEngineSwitch("31") { Description = "Right Outlane" },
			new GamelogicEngineSwitch("32") { Description = "Left Outlane" },
			new GamelogicEngineSwitch("33") { Description = "Right Spinner" },
			new GamelogicEngineSwitch("34") { Description = "Left Spinner" },
			new GamelogicEngineSwitch("35") { Description = "Right Slingshot" },
			new GamelogicEngineSwitch("36") { Description = "Left Slingshot" },
			new GamelogicEngineSwitch("37") { Description = "Top Thumper Bumper" },
			new GamelogicEngineSwitch("39") { Description = "Right Thumper Bumper" },
			new GamelogicEngineSwitch("40") { Description = "Left Thumper Bumper" },
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {

		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {
			new GamelogicEngineCoil("01", 7) { Description = "Out Hole" },
			new GamelogicEngineCoil("02", 6) { Description = "Knocker" },
			new GamelogicEngineCoil("03", 4) { Description = "Saucer Kick Down" },
			new GamelogicEngineCoil("04", 8) { Description = "Saucer Kick Up" },
			new GamelogicEngineCoil("05", 9) { Description = "Single Drop Target Reset" },
			new GamelogicEngineCoil("06", 1) { Description = "Drop Target Reset" },
			new GamelogicEngineCoil("07", 2) { Description = "Drop Target Reset" },
			new GamelogicEngineCoil("08", 3) { Description = "In Line Drop Target" },
			new GamelogicEngineCoil("09", 10) { Description = "Left Bumper" },
			new GamelogicEngineCoil("10", 11) { Description = "Right Bumper" },
			new GamelogicEngineCoil("11", 12) { Description = "Single Drop Target Pull Down" },
			new GamelogicEngineCoil("12", 13) { Description = "Top Bumper" },
			new GamelogicEngineCoil("13", 14) { Description = "Left Slingshot" },
			new GamelogicEngineCoil("14", 15) { Description = "Right Slingshot" },
			new GamelogicEngineCoil("15", 18) { Description = "Coin Lockout Door" },
			new GamelogicEngineCoil("16", 19) { Description = "KI Relay (Flipper enabled)" },
		};
	}
}
