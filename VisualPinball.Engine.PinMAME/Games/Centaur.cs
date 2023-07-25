// Visual Pinball Engine
// Copyright (C) 2023 freezy and VPE Team
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
	public class Centaur : Bally
	{
		public override string Name => "Centaur";
		public override string Id => "centaur";
		public override int Year => 1981;
		public override string Manufacturer => "Bally";
		public override int IpdbId => 476;

		public override PinMameRom[] Roms { get; } = {
			new("centaur"),
			new("centaura", description: "Free Play"),
			new("centaurb", "Rev. 27", description: "Free Play"),
		};

		protected override PinMameIdAlias[] Aliases { get; } = {
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new(1) { Description = "4th Ball Through", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "4" },
			new(2) { Description = "5th Ball Through", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "5"  },
			new(3) { Description = "Top Right Lane" },
			new(4) { Description = "Top Middle Lane" },
			new(5) { Description = "Top Left Lane" },
			new(6) { Description = "Credit Button" },
			new(7) { Description = "" },
			new(8) { Description = "Outhole", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "drain_switch" },
			new(9) { Description = "Coin III (Right)" },
			new(10) { Description = "Coin I (Left)" },
			new(11) { Description = "Coin II (Middle)" },
			new(12) { Description = "Top Left Lane R.O. Button" },
			new(13) { Description = "" },
			new(14) { Description = "" },
			new(15) { Description = "Tilt" },
			new(16) { Description = "Slam" },
			new(17) { Description = "1st Ball Through", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "1"  },
			new(18) { Description = "Left Side R.O. Button" },
			new(19) { Description = "Orbs Right Lane Target" },
			new(20) { Description = "Inline Back Target" },
			new(21) { Description = "Left & Right Flipper Button" },
			new(22) { Description = "Reset 1-4 Targets" },
			new(23) { Description = "" },
			new(24) { Description = "Top Spot 1-4 Target" },
			new(25) { Description = "Right 4 Drop Target \"4\" (Bottom)" },
			new(26) { Description = "Right 4 Drop Target \"3\"" },
			new(27) { Description = "Right 4 Drop Target \"2\"" },
			new(28) { Description = "Right 4 Drop Target \"1\" (Top)" },
			new(29) { Description = "\"S\" Drop Target" },
			new(30) { Description = "\"B\" Drop Target" },
			new(31) { Description = "\"R\" Drop Target" },
			new(32) { Description = "\"O\" Drop Target" },
			new(33) { Description = "End of Trough" },
			new(34) { Description = "10 Point Rebound (5)" },
			new(35) { Description = "" },
			new(36) { Description = "" },
			new(37) { Description = "Right Slingshot" },
			new(38) { Description = "Left Slingshot" },
			new(39) { Description = "Right Thumper Bumper" },
			new(40) { Description = "Left Thumper Bumper" },
			new(41) { Description = "1st Inline Drop Target" },
			new(42) { Description = "2nd Inline Drop Target" },
			new(43) { Description = "3rd Inline Drop Target" },
			new(44) { Description = "4th Inline Drop Target" },
			new(45) { Description = "Right Outlane" },
			new(46) { Description = "Right Return Lane" },
			new(47) { Description = "Left Return Lane" },
			new(48) { Description = "Left Outlane" },
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
			new(2) { Description = "Middle 1" },
			new(18) { Description = "Middle 2" },
			new(34) { Description = "Middle 3" },
			new(50) { Description = "Middle 4" },
			new(3) { Description = "Middle 5" },
			new(19) { Description = "Middle 6" },
			new(35) { Description = "Middle 7" },
			new(51) { Description = "Middle 8" },
			new(4) { Description = "Middle 9" },
			new(20) { Description = "Middle 10" },
			new(26) { Description = "Middle 20" },
			new(42) { Description = "Middle 40" },
			new(58) { Description = "Middle 60" },
			new(5) { Description = "Middle 2x" },
			new(21) { Description = "Middle 3x" },
			new(37) { Description = "Middle 4x" },
			new(53) { Description = "Middle 5x" },

			new(12) { Description = "Bottom 1" },
			new(28) { Description = "Bottom 2" },
			new(44) { Description = "Bottom 3" },
			new(60) { Description = "Bottom 4" },

			new(17) { Description = "When Lit" },

			new(54) { Description = "5K 1 (left)" },
			new(38) { Description = "5K 2" },
			new(22) { Description = "5K 3" },
			new(6) { Description = "5K 4 (right)" },

			new(9) { Description = "Left Lane 10" },
			new(25) { Description = "Left Lane 20" },
			new(41) { Description = "Left Lane 30" },
			new(57) { Description = "Left Lane 40" },
			new(10) { Description = "Left Lane 50" },

			new(46) { Description = "Left Resets 1-4 Targets Arrow" },

			new(56) { Description = "Center Drop \"O\"" },
			new(40) { Description = "Center Drop \"R\"" },
			new(24) { Description = "Center Drop \"B\"" },
			new(8) { Description = "Center Drop \"S\"" },

			new(14) { Description = "Center SPL" },
			new(30) { Description = "Center 50" },

			new(33) { Description = "Release (Right Lane)" },
			new(49) { Description = "Collect Bonus Arrow" },

			new(15) { Description = "Bonus 2x" },
			new(31) { Description = "Bonus 3x" },
			new(47) { Description = "Bonus 4x" },
			new(63) { Description = "Bonus 5x" },

			new(55) { Description = "Right Drop 10" },
			new(39) { Description = "Right Drop 20" },
			new(23) { Description = "Right Drop 40" },
			new(7) { Description = "Right Drop 80" },

			new(84) { Description = "Queen's Chamber Upper" },
			new(68) { Description = "Queen's Chamber Lower" },

			new(36) { Description = "Release (Top Left)" },
			new(52) { Description = "SPL (Top Left)" },

			new(100) { Description = "Yellow Round Top Left" },
			new(62) { Description = "Green Round Top Right" },

			new(97) { Description = "Rollover 1 (Left)" },
			new(81) { Description = "Rollover 2" },
			new(65) { Description = "Rollover 3 (Right)" },

			new(114) { Description = "Left Thumper Bumper" },
			new(98) { Description = "Right Thumper Bumper" },

			new(59) { Description = "Bottom Left" },
		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {
			new(7) { Description = "Outhole Kicker" },                      // 01
			new(6) { Description = "Knocker" },                             // 02
			new(8) { Description = "Inline Drop Target Reset" },            // 03
			new(9) { Description = "4 Right Drop Target Reset" },           // 04
			new(10) { Description = "Left Thumper Bumper" },                // 05
			new(11) { Description = "Right Thumper Bumper" },               // 06
			new(12) { Description = "Left Slingshot" },                     // 07
			new(13) { Description = "Right Slingshot" },                    // 08
			new(1) { Description = "Orbs Target Reset" },                   // 09
			new(2) { Description = "Right 4 Drop Target \"1\" (top)" },     // 10
			new(3) { Description = "Right 4 Drop Target \"2\"" },           // 11
			new(5) { Description = "Right 4 Drop Target \"3\"" },           // 12
			new(5) { Description = "Right 4 Drop Target \"4\" (bottom)" },  // 13
			new(15) { Description = "Ball Release" },                       // 14
			new(14) { Description = "Ball Kick to Playfield" },             // 15
			new(18) { Description = "Coin Lockout Door" },                  // 16
		};
	}
}
