﻿// Visual Pinball Engine
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
	public class Rock : System80
	{
		public override string Name => "Rock";
		public override string Id => "rock";
		public override int Year => 1985;
		public override string Manufacturer => "Premier";
		public override int IpdbId => 1978;

		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("rock"),
			new PinMameRom("rockfp", description: "Free Play"),
			new PinMameRom("rockg", language: PinMameRomLanguage.German),
			new PinMameRom("rockgfp", description: "Free Play", language: PinMameRomLanguage.German),
		};

		protected override PinMameIdAlias[] Aliases { get; } = {
			new PinMameIdAlias(45, CoilFlipperUpperRight, AliasType.Coil),
			new PinMameIdAlias(46, CoilFlipperLowerRight, AliasType.Coil),
			new PinMameIdAlias(47, CoilFlipperUpperLeft, AliasType.Coil),
			new PinMameIdAlias(48, CoilFlipperLowerLeft, AliasType.Coil),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new GamelogicEngineSwitch(40) { Description = "#1 Drop Target (Upper)" },
			new GamelogicEngineSwitch(41) { Description = "10 Point (2)", DeviceHint = "^(sw41[a-f]|(Left|Right)\\sSlingshot)$", NumMatches = 8 },
			new GamelogicEngineSwitch(42) { Description = "Right Flipper (Lower)", DeviceHint = "^LowerRightFlipper$" },
			new GamelogicEngineSwitch(43) { Description = "Rollunder" },
			new GamelogicEngineSwitch(44) { Description = "Right Outside Rollover" },
			new GamelogicEngineSwitch(45) { Description = "Right Spinner (with Bracket)" },
			new GamelogicEngineSwitch(46) { Description = "Right Top Rollover" },
			new GamelogicEngineSwitch(50) { Description = "#2 Drop Target (Upper)" },
			new GamelogicEngineSwitch(51) { Description = "#1 Drop Target (Lower)" },
			new GamelogicEngineSwitch(52) { Description = "Spot Target (with Bracket)" },
			new GamelogicEngineSwitch(53) { Description = "#1 Top Rollover" },
			new GamelogicEngineSwitch(54) { Description = "Right Return Rollover" },
			new GamelogicEngineSwitch(55) { Description = "Right Side Rollover" },
			new GamelogicEngineSwitch(57) { Description = "Tilt (with Bracket)" },
			new GamelogicEngineSwitch(60) { Description = "#3 Drop Targer (Upper)" },
			new GamelogicEngineSwitch(61) { Description = "#2 Drop Targer (Lower)" },
			new GamelogicEngineSwitch(62) { Description = "#4 Drop Targer (Lower)" },
			new GamelogicEngineSwitch(63) { Description = "#2 Top Rollover" },
			new GamelogicEngineSwitch(64) { Description = "Left Return Rollover" },
			new GamelogicEngineSwitch(65) { Description = "Left Side Rollover" },
			new GamelogicEngineSwitch(67) { Description = "Outhole (Assembly)", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "drain_switch" },
			new GamelogicEngineSwitch(70) { Description = "#4 Drop Targer (Upper)" },
			new GamelogicEngineSwitch(71) { Description = "#3 Drop Targer (Lower)" },
			new GamelogicEngineSwitch(72) { Description = "#5 Drop Targer (Lower)" },
			new GamelogicEngineSwitch(73) { Description = "#3 Top Rollover" },
			new GamelogicEngineSwitch(74) { Description = "Left Outside Rollover" },
			new GamelogicEngineSwitch(75) { Description = "Left Spinner (with Bracket)" },
			new GamelogicEngineSwitch(76) { Description = "Left Top Rollover" },
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
			new GamelogicEngineLamp(1) { Description = "Lamp 1" },
			new GamelogicEngineLamp(3) { Description = "Shoot Again" },
			new GamelogicEngineLamp(5) { Description = "#1 Drop Target (Upper)" },
			new GamelogicEngineLamp(6) { Description = "#2 Drop Target (Upper)" },
			new GamelogicEngineLamp(7) { Description = "#3 Drop Target (Upper)" },
			new GamelogicEngineLamp(8) { Description = "#4 Drop Target (Upper)" },
			new GamelogicEngineLamp(9) { Description = "Level 1" },
			new GamelogicEngineLamp(10) { Description = "Level 2" },
			new GamelogicEngineLamp(11) { Description = "Level 3" },
			new GamelogicEngineLamp(12) { Description = "Lamp 12" },
			new GamelogicEngineLamp(13) { Description = "Lamp 13", DeviceHint = "^L13[a-b]$", NumMatches = 2  },
			new GamelogicEngineLamp(14) { Description = "#1 Drop Target (Lower)" },
			new GamelogicEngineLamp(15) { Description = "#2 Drop Target (Lower)" },
			new GamelogicEngineLamp(16) { Description = "#3 Drop Target (Lower)" },
			new GamelogicEngineLamp(17) { Description = "#4 Drop Target (Lower)" },
			new GamelogicEngineLamp(18) { Description = "#5 Drop Target (Lower)" },
			new GamelogicEngineLamp(19) { Description = "Left Side Rollover" },
			new GamelogicEngineLamp(20) { Description = "Left Return Rollover" },
			new GamelogicEngineLamp(21) { Description = "Right Return Rollover" },
			new GamelogicEngineLamp(22) { Description = "Right Side Rollover" },
			new GamelogicEngineLamp(23) { Description = "#1 Top Rollover (\"A\")" },
			new GamelogicEngineLamp(24) { Description = "#2 Top Rollover (\"B\")" },
			new GamelogicEngineLamp(25) { Description = "#3 Top Rollover (\"C\")" },
			new GamelogicEngineLamp(26) { Description = "\"R\"" },
			new GamelogicEngineLamp(27) { Description = "\"O\"" },
			new GamelogicEngineLamp(28) { Description = "\"C\"" },
			new GamelogicEngineLamp(29) { Description = "\"K\"" },
			new GamelogicEngineLamp(30) { Description = "\"AND\"" },
			new GamelogicEngineLamp(31) { Description = "\"R\"" },
			new GamelogicEngineLamp(32) { Description = "\"O\"" },
			new GamelogicEngineLamp(33) { Description = "\"L\"" },
			new GamelogicEngineLamp(34) { Description = "\"L\"" },
			new GamelogicEngineLamp(35) { Description = "\"L\"" },
			new GamelogicEngineLamp(36) { Description = "\"I\"" },
			new GamelogicEngineLamp(37) { Description = "\"V\"" },
			new GamelogicEngineLamp(38) { Description = "\"E\"" },
			new GamelogicEngineLamp(39) { Description = "\"S\"" },
			new GamelogicEngineLamp(40) { Description = "\"!\"" },
			new GamelogicEngineLamp(41) { Description = "Double Scoring" },
			new GamelogicEngineLamp(42) { Description = "2X" },
			new GamelogicEngineLamp(43) { Description = "3X" },
			new GamelogicEngineLamp(44) { Description = "4X" },
			new GamelogicEngineLamp(45) { Description = "5X" },
			new GamelogicEngineLamp(46) { Description = "Spot Target" },
			new GamelogicEngineLamp(47) { Description = "Left Outside Rollover / Right Spinner", DeviceHint = "^L47[a-b]$", NumMatches = 2 },
			new GamelogicEngineLamp(51) { Description = "Right Outside Rollover / Left Spinner", DeviceHint = "^L51[a-b]$", NumMatches = 2 },
			new GamelogicEngineLamp("l_auxiliary") { Description = "Auxiliary Lamps", DeviceHint = "^Auxiliary$" },
			new GamelogicEngineLamp("l_lower") { Description = "Lower", DeviceHint = "^Lower$" },
			new GamelogicEngineLamp("l_upper_left_1") { Description = "Upper Left 1", DeviceHint = "^UpperLeft1$" },
			new GamelogicEngineLamp("l_upper_left_2") { Description = "Upper Left 2", DeviceHint = "^UpperLeft2$" },
			new GamelogicEngineLamp("l_upper_right") { Description = "Upper Right", DeviceHint = "^UpperRight$" },
		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {
			new GamelogicEngineCoil(5) { Description = "Five Pos. Bank Reset", DeviceHint = "^5PosBank\\s*" },
			new GamelogicEngineCoil(6) { Description = "Four Pos. Bank Reset", DeviceHint = "^4PosBank\\s*" },
			new GamelogicEngineCoil(8) { Description = "Knocker Assembly" },
			new GamelogicEngineCoil(9) { Description = "Outhole", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "eject_coil" },
			new GamelogicEngineCoil(CoilFlipperUpperRight) { Description = "Upper Right Flipper", DeviceHint = "^UpperRightFlipper$" },
			new GamelogicEngineCoil(CoilFlipperLowerRight) { Description = "Lower Right Flipper", DeviceHint = "^LowerRightFlipper$" },
			new GamelogicEngineCoil(CoilFlipperUpperLeft) { Description = "Upper Left Flipper", DeviceHint = "^UpperLeftFlipper$" },
			new GamelogicEngineCoil(CoilFlipperLowerLeft) { Description = "Lower Left Flipper", DeviceHint = "^LowerLeftFlipper$" },
		};
	}
}
