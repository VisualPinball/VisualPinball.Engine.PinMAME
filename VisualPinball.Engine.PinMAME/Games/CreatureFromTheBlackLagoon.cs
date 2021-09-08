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

using System;
using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.PinMAME.MPUs;
using VisualPinball.Engine.VPT.Trough;

namespace VisualPinball.Engine.PinMAME.Games
{
	[Serializable]
	public class CreatureFromTheBlackLagoon : Wpc
	{
		public override string Name { get; } = "Creature From The Black Lagoon";
		public override string Id { get; } = "cftbl";
		public override int Year { get; } = 1992;
		public override string Manufacturer { get; } = "Bally";
		public override int IpdbId { get; } = 588;
		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("cftbl_l4", "L-4"),
			new PinMameRom("cftbl_p3", "P-3", "Prototype, SP-1"),
			new PinMameRom("cftbl_l2", "L-2"),
			new PinMameRom("cftbl_d2", "D-2", "LED Ghost Fix"),
			new PinMameRom("cftbl_l3", "L-3"),
			new PinMameRom("cftbl_d3", "D-3", "LED Ghost Fix"),
			new PinMameRom("cftbl_d4", "D-4", "LED Ghost Fix"),
			new PinMameRom("cftbl_l4c", "L-4C", "Competition + LED Ghost MOD"),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new GamelogicEngineSwitch("13") { Description = "Start Button", InputActionHint = InputConstants.ActionStartGame },
			new GamelogicEngineSwitch("14") { Description = "Plumb Bolt Tilt" },
			new GamelogicEngineSwitch("15") { Description = "Top Left Rollover" },
			new GamelogicEngineSwitch("16") { Description = "Left Subway" },
			new GamelogicEngineSwitch("17") { Description = "Center Subway" },
			new GamelogicEngineSwitch("18") { Description = "Center Shot" },

			new GamelogicEngineSwitch("21") { Description = "Slam Tilt",  InputActionHint = InputConstants.ActionSlamTilt },
			new GamelogicEngineSwitch("22") { Description = "Coin Door Closed", NormallyClosed = true, InputActionHint = InputConstants.ActionCoinDoorOpenClose },
			new GamelogicEngineSwitch("24") { Description = "Always Closed", ConstantHint = SwitchConstantHint.AlwaysClosed},
			new GamelogicEngineSwitch("25") { Description = "(P)-A-I-D"},
			new GamelogicEngineSwitch("26") { Description = "P-(A)-I-D" },
			new GamelogicEngineSwitch("27") { Description = "P-A-(I)-D" },
			new GamelogicEngineSwitch("28") { Description = "P-A-I-(D)" },

			new GamelogicEngineSwitch("33") { Description = "Bottom Jet" },
			new GamelogicEngineSwitch("34") { Description = "Right Popper" },
			new GamelogicEngineSwitch("35") { Description = "Right Ramp Enter" },
			new GamelogicEngineSwitch("36") { Description = "Left Ramp Enter" },
			new GamelogicEngineSwitch("37") { Description = "Lower Right Popper" },
			new GamelogicEngineSwitch("38") { Description = "Ramp Up/Down" },

			new GamelogicEngineSwitch("41") { Description = "Cola" },
			new GamelogicEngineSwitch("42") { Description = "Hot Dog" },
			new GamelogicEngineSwitch("43") { Description = "Popcorn" },
			new GamelogicEngineSwitch("44") { Description = "Ice Cream" },
			new GamelogicEngineSwitch("45") { Description = "Left Jet" },
			new GamelogicEngineSwitch("46") { Description = "Right Jet" },
			new GamelogicEngineSwitch("47") { Description = "Left Slingshot" },
			new GamelogicEngineSwitch("48") { Description = "Right Slingshot" },

			new GamelogicEngineSwitch("51") { Description = "Left Out Lane" },
			new GamelogicEngineSwitch("52") { Description = "Left Return Lane" },
			new GamelogicEngineSwitch("53") { Description = "Start Combo" },
			new GamelogicEngineSwitch("54") { Description = "Right Out Lane" },
			new GamelogicEngineSwitch("55") { Description = "Outhole", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "drain_switch" },
			new GamelogicEngineSwitch("56") { Description = "Right Trough", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "1" },
			new GamelogicEngineSwitch("57") { Description = "Center Trough", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "2" },
			new GamelogicEngineSwitch("58") { Description = "Left Trough", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "3" },

			new GamelogicEngineSwitch("61") { Description = "Right Ramp Exit" },
			new GamelogicEngineSwitch("62") { Description = "Left Ramp Exit" },
			new GamelogicEngineSwitch("63") { Description = "Center Lane Exit" },
			new GamelogicEngineSwitch("64") { Description = "Upper Ramp" },
			new GamelogicEngineSwitch("65") { Description = "Bowl" },
			new GamelogicEngineSwitch("66") { Description = "Shooter" },
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
			new GamelogicEngineLamp("11") { Description = "(P)-A-I-D" },
			new GamelogicEngineLamp("12") { Description = "P-(A)-I-D" },
			new GamelogicEngineLamp("13") { Description = "P-A-(I)-D" },
			new GamelogicEngineLamp("14") { Description = "P-A-I-(D)" },
			new GamelogicEngineLamp("15") { Description = "Left Jet" },
			new GamelogicEngineLamp("16") { Description = "Right Jet" },
			new GamelogicEngineLamp("17") { Description = "Bottom Jet" },
			new GamelogicEngineLamp("18") { Description = "Admit One" },

			new GamelogicEngineLamp("21") { Description = "(K)-I-S-S" },
			new GamelogicEngineLamp("22") { Description = "K-(I)-S-S" },
			new GamelogicEngineLamp("23") { Description = "K-I-(S)-S" },
			new GamelogicEngineLamp("24") { Description = "K-I-S-(S)" },
			new GamelogicEngineLamp("25") { Description = "10 Million" },
			new GamelogicEngineLamp("26") { Description = "20 Million" },
			new GamelogicEngineLamp("27") { Description = "30 Million" },
			new GamelogicEngineLamp("28") { Description = "Specials" },

			new GamelogicEngineLamp("31") { Description = "Start Mega Menu" },
			new GamelogicEngineLamp("32") { Description = "Playground Award" },
			new GamelogicEngineLamp("33") { Description = "Lite Big Millions" },
			new GamelogicEngineLamp("34") { Description = "Slide" },
			new GamelogicEngineLamp("35") { Description = "Right Search" },
			new GamelogicEngineLamp("36") { Description = "Right Video" },
			new GamelogicEngineLamp("37") { Description = "Right Start Movie" },
			new GamelogicEngineLamp("38") { Description = "Mega Menu" },

			new GamelogicEngineLamp("41") { Description = "Lips" },
			new GamelogicEngineLamp("42") { Description = "Left Search" },
			new GamelogicEngineLamp("43") { Description = "Left Video" },
			new GamelogicEngineLamp("44") { Description = "Left Start Movie" },
			new GamelogicEngineLamp("45") { Description = "Combo Award" },
			new GamelogicEngineLamp("46") { Description = "Parking O.K." },
			new GamelogicEngineLamp("47") { Description = "Move Your Car" },
			new GamelogicEngineLamp("48") { Description = "Extra Ball" },

			new GamelogicEngineLamp("51") { Description = "Snack Bar" },
			new GamelogicEngineLamp("52") { Description = "Center Search" },
			new GamelogicEngineLamp("53") { Description = "Cola" },
			new GamelogicEngineLamp("54") { Description = "Hot Dog" },
			new GamelogicEngineLamp("55") { Description = "Super Jackpot" },
			new GamelogicEngineLamp("56") { Description = "Jackpot" },
			new GamelogicEngineLamp("57") { Description = "Rescue" },
			new GamelogicEngineLamp("58") { Description = "Multiball Restart" },

			new GamelogicEngineLamp("61") { Description = "Free Pass" },
			new GamelogicEngineLamp("62") { Description = "Build Combo" },
			new GamelogicEngineLamp("63") { Description = "Unlimited Millions" },
			new GamelogicEngineLamp("64") { Description = "Creature Feature" },
			new GamelogicEngineLamp("65") { Description = "Extra Ball Countdown" },
			new GamelogicEngineLamp("66") { Description = "Big Millions" },
			new GamelogicEngineLamp("67") { Description = "Movie Madness" },
			new GamelogicEngineLamp("68") { Description = "Snack Attack" },

			new GamelogicEngineLamp("71") { Description = "C" },
			new GamelogicEngineLamp("72") { Description = "R" },
			new GamelogicEngineLamp("73") { Description = "E" },
			new GamelogicEngineLamp("74") { Description = "A" },
			new GamelogicEngineLamp("75") { Description = "T" },
			new GamelogicEngineLamp("76") { Description = "U" },
			new GamelogicEngineLamp("77") { Description = "R" },
			new GamelogicEngineLamp("78") { Description = "E" },

			new GamelogicEngineLamp("81") { Description = "(F)-I-L-M" },
			new GamelogicEngineLamp("82") { Description = "F-(I)-L-M" },
			new GamelogicEngineLamp("83") { Description = "F-I-(L)-M" },
			new GamelogicEngineLamp("84") { Description = "F-I-L-(M)" },
			new GamelogicEngineLamp("85") { Description = "Start Combo" },
			new GamelogicEngineLamp("86") { Description = "Popcorn" },
			new GamelogicEngineLamp("87") { Description = "Ice Cream" },
			new GamelogicEngineLamp("88") { Description = "Start Button" }
		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {
			new GamelogicEngineCoil("01", 1) { Description = "Top Right Popper" },
			new GamelogicEngineCoil("02", 2) { Description = "Left Subway Enter Flasher", IsLamp = true },
			new GamelogicEngineCoil("03", 3) { Description = "Lower Right Popper" },
			new GamelogicEngineCoil("04", 4) { Description = "Trough Eject", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "eject_coil" },
			new GamelogicEngineCoil("05", 5) { Description = "Right Slingshot" },
			new GamelogicEngineCoil("06", 6) { Description = "Left Slingshot" },
			new GamelogicEngineCoil("07", 7) { Description = "Knocker" },
			new GamelogicEngineCoil("08", 8) { Description = "Bottom Right Flasher", IsLamp = true },
			new GamelogicEngineCoil("09", 9) { Description = "Back Flashers", IsLamp = true },
			new GamelogicEngineCoil("10") { Description = "Bowl Flasher", IsLamp = true },
			new GamelogicEngineCoil("11") { Description = "Creature Flasher", IsLamp = true },
			new GamelogicEngineCoil("12") { Description = "Outhole", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "entry_coil" },
			new GamelogicEngineCoil("13") { Description = "Left Jet" },
			new GamelogicEngineCoil("14") { Description = "Right Jet" },
			new GamelogicEngineCoil("15") { Description = "Bottom Jet" },
			new GamelogicEngineCoil("16") { Description = "Right Popper Flasher", IsLamp = true },
			new GamelogicEngineCoil("17") { Description = "Bottom Left Flasher", IsLamp = true },
			new GamelogicEngineCoil("18") { Description = "Right Ramp Flasher", IsLamp = true },
			new GamelogicEngineCoil("19") { Description = "Left Ramp Flasher", IsLamp = true },
			new GamelogicEngineCoil("20") { Description = "Sequential GI #1", IsLamp = true },
			new GamelogicEngineCoil("21") { Description = "Hologram Push Motor (playfield)" },
			new GamelogicEngineCoil("22") { Description = "Center Hole Flasher", IsLamp = true },
			new GamelogicEngineCoil("23") { Description = "Up/Down Ramp (up)" },
			new GamelogicEngineCoil("24") { Description = "Sequential GI #2", IsLamp = true },
			new GamelogicEngineCoil("25") { Description = "Start Move Flashers", IsLamp = true },
			new GamelogicEngineCoil("26") { Description = "Up/Down Ramp (down)" },
			new GamelogicEngineCoil("27") { Description = "Creature Motor (mirror)" },
			new GamelogicEngineCoil("28") { Description = "Hologram Lamp (cabinet)", IsLamp = true  },
		};
	}
}
