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
		public override string Name { get; } = "Flash Gordon";
		public override string Id { get; } = "fg";
		public override int Year { get; } = 1980;
		public override string Manufacturer { get; } = "Bally";
		public override int IpdbId { get; } = 874;

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
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
		};

		protected override GamelogicEngineCoil[] Coils { get; } = {
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
