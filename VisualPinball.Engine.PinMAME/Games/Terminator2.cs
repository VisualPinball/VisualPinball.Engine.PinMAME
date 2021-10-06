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

namespace VisualPinball.Engine.PinMAME.Games
{
	[Serializable]
	public class Terminator2 : Wpc
	{
		public override string Name => "Terminator 2: Judgment Day";
		public override string Id => "t2";
		public override int Year => 1991;
		public override string Manufacturer => "Williams";
		public override int IpdbId => 2524;
		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("t2_l2", "L-2"),
			new PinMameRom("t2_l3", "L-3"),
			new PinMameRom("t2_l4", "L-4"),
			new PinMameRom("t2_l6", "L-6"),
			new PinMameRom("t2_l8", "L-8"),
			new PinMameRom("t2_l81", "L-81"),
			new PinMameRom("t2_l82", "L-82"),
			new PinMameRom("t2_p2f", "P-2F", "Profanity ROM"),
			new PinMameRom("t2_f19", "0.19", "FreeWPC"),
			new PinMameRom("t2_f20", "0.20", "FreeWPC"),
			new PinMameRom("t2_f32", "0.32", "FreeWPC"),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new GamelogicEngineSwitch("13") { Description = "Start Button", InputActionHint = InputConstants.ActionStartGame },
			new GamelogicEngineSwitch("14") { Description = "Plumb Bob Tilt" },
			new GamelogicEngineSwitch("15") { Description = "Trough Left", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "2" },
			new GamelogicEngineSwitch("16") { Description = "Trough Center", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "3" },
			new GamelogicEngineSwitch("17") { Description = "Trough Right", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "4" },
			new GamelogicEngineSwitch("18") { Description = "Outhole", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "1" },

			//new GamelogicEngineSwitch("21") { Description = "Slam Tilt" },
			new GamelogicEngineSwitch("22") { Description = "Coin Door Closed", NormallyClosed = true, InputActionHint = InputConstants.ActionCoinDoorOpenClose },
			new GamelogicEngineSwitch("23") { Description = "Ticket Dispenser" },
			new GamelogicEngineSwitch("24") { Description = "Test Position, Always Closed", ConstantHint = SwitchConstantHint.AlwaysClosed},
			new GamelogicEngineSwitch("25") { Description = "Left Outlane" },
			new GamelogicEngineSwitch("26") { Description = "Left Return Lane" },
			new GamelogicEngineSwitch("27") { Description = "Right Return Lane" },
			new GamelogicEngineSwitch("28") { Description = "Right Outlane" },

			new GamelogicEngineSwitch("31") { Description = "Gun Loaded" },
			new GamelogicEngineSwitch("32") { Description = "Gun Mark" },
			new GamelogicEngineSwitch("33") { Description = "Gun Home" },
			new GamelogicEngineSwitch("34") { Description = "Grip Trigger", InputActionHint = InputConstants.ActionPlunger },
			new GamelogicEngineSwitch("36") { Description = "Mid Left Stand-up Target" },
			new GamelogicEngineSwitch("37") { Description = "Mid Center Stand-up Target" },
			new GamelogicEngineSwitch("38") { Description = "Mid Right Stand-up Target" },

			new GamelogicEngineSwitch("41") { Description = "Left Jet" },
			new GamelogicEngineSwitch("42") { Description = "Right Jet" },
			new GamelogicEngineSwitch("43") { Description = "Bottom Jet" },
			new GamelogicEngineSwitch("44") { Description = "Left Sling" },
			new GamelogicEngineSwitch("45") { Description = "Right Sling" },
			new GamelogicEngineSwitch("46") { Description = "Top Right Stand-up Target" },
			new GamelogicEngineSwitch("47") { Description = "Mid Right Stand-up Target" },
			new GamelogicEngineSwitch("48") { Description = "Bottom Right Stand-up Target" },

			new GamelogicEngineSwitch("51") { Description = "Left Lock" },
			new GamelogicEngineSwitch("53") { Description = "Low Escape Route" },
			new GamelogicEngineSwitch("54") { Description = "High Escape Route" },
			new GamelogicEngineSwitch("55") { Description = "Top Lock" },
			new GamelogicEngineSwitch("56") { Description = "Top Lane Left" },
			new GamelogicEngineSwitch("57") { Description = "Top Lane Center" },
			new GamelogicEngineSwitch("58") { Description = "Top Lane Right" },

			new GamelogicEngineSwitch("61") { Description = "Left Ramp Entry" },
			new GamelogicEngineSwitch("62") { Description = "Left Ramp Made" },
			new GamelogicEngineSwitch("63") { Description = "Right Ramp Entry" },
			new GamelogicEngineSwitch("64") { Description = "Right Ramp Made" },
			new GamelogicEngineSwitch("65") { Description = "Low Chase Loop" },
			new GamelogicEngineSwitch("66") { Description = "High Chase Loop" },

			new GamelogicEngineSwitch("71") { Description = "Target 1 High" },
			new GamelogicEngineSwitch("72") { Description = "Target 2" },
			new GamelogicEngineSwitch("73") { Description = "Target 3" },
			new GamelogicEngineSwitch("74") { Description = "Target 4" },
			new GamelogicEngineSwitch("75") { Description = "Target 5 Low" },
			new GamelogicEngineSwitch("76") { Description = "Ball Popper" },
			new GamelogicEngineSwitch("77") { Description = "Drop Target" },
			new GamelogicEngineSwitch("78") { Description = "Shooter" },
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
			new GamelogicEngineLamp("11") { Description = "Multiplier 2x" },
			new GamelogicEngineLamp("12") { Description = "Multiplier 4x" },
			new GamelogicEngineLamp("13") { Description = "Hold Bonus" },
			new GamelogicEngineLamp("14") { Description = "Multiplier 6x" },
			new GamelogicEngineLamp("15") { Description = "Multiplier 8x" },
			new GamelogicEngineLamp("16") { Description = "Shoot Again" },
			new GamelogicEngineLamp("17") { Description = "Mouth" },

			new GamelogicEngineLamp("21") { Description = "Kickback" },
			new GamelogicEngineLamp("22") { Description = "Special" },
			new GamelogicEngineLamp("23") { Description = "Left Return Lane" },
			new GamelogicEngineLamp("24") { Description = "Right Return Lane" },
			new GamelogicEngineLamp("25") { Description = "Data Base" },
			new GamelogicEngineLamp("26") { Description = "Load Gun" },
			new GamelogicEngineLamp("27") { Description = "Extra Ball" },
			new GamelogicEngineLamp("28") { Description = "Load for Jackpot" },

			new GamelogicEngineLamp("31") { Description = "Target 1 High" },
			new GamelogicEngineLamp("32") { Description = "Target 2" },
			new GamelogicEngineLamp("33") { Description = "Target 3" },
			new GamelogicEngineLamp("34") { Description = "Target 4" },
			new GamelogicEngineLamp("35") { Description = "Target 5 Low" },
			new GamelogicEngineLamp("36") { Description = "Middle Target Bank Left" },
			new GamelogicEngineLamp("37") { Description = "Middle Target Bank Center" },
			new GamelogicEngineLamp("38") { Description = "Middle Target Bank Right" },

			new GamelogicEngineLamp("41") { Description = "Lock Two" },
			new GamelogicEngineLamp("42") { Description = "Data Base" },
			new GamelogicEngineLamp("43") { Description = "10 Million" },
			new GamelogicEngineLamp("44") { Description = "Extra Ball" },
			new GamelogicEngineLamp("45") { Description = "Multi-ball" },
			new GamelogicEngineLamp("46") { Description = "Light Hurry Up" },
			new GamelogicEngineLamp("47") { Description = "Hold Bonus" },
			new GamelogicEngineLamp("48") { Description = "Security Pass" },

			new GamelogicEngineLamp("51") { Description = "Eyes Lower" },
			new GamelogicEngineLamp("52") { Description = "Eyes Upper" },
			new GamelogicEngineLamp("53") { Description = "5,000,000" },
			new GamelogicEngineLamp("54") { Description = "3,000,000" },
			new GamelogicEngineLamp("55") { Description = "1,000,000" },
			new GamelogicEngineLamp("56") { Description = "750,000" },
			new GamelogicEngineLamp("57") { Description = "500,000" },
			new GamelogicEngineLamp("58") { Description = "250,000" },

			new GamelogicEngineLamp("61") { Description = "Left CPU Lit" },
			new GamelogicEngineLamp("62") { Description = "Left Vault Key" },
			new GamelogicEngineLamp("63") { Description = "Left Silent Alarm" },
			new GamelogicEngineLamp("64") { Description = "Left Passcode" },
			new GamelogicEngineLamp("65") { Description = "Left Checkpoint" },
			new GamelogicEngineLamp("66") { Description = "Lock 1" },
			new GamelogicEngineLamp("67") { Description = "Data Base 1" },
			new GamelogicEngineLamp("68") { Description = "Left Ramp" },

			new GamelogicEngineLamp("71") { Description = "Right CPU Lit" },
			new GamelogicEngineLamp("72") { Description = "Right Vault Key" },
			new GamelogicEngineLamp("73") { Description = "Right Silent Alarm" },
			new GamelogicEngineLamp("74") { Description = "Right Passcode" },
			new GamelogicEngineLamp("75") { Description = "Right Checkpoint" },
			new GamelogicEngineLamp("76") { Description = "Right Bank Top" },
			new GamelogicEngineLamp("77") { Description = "Right Bank Middle" },
			new GamelogicEngineLamp("78") { Description = "Right Bank Bottom" },

			new GamelogicEngineLamp("81") { Description = "Chase Values" },
			new GamelogicEngineLamp("82") { Description = "Right Ramp" },
			new GamelogicEngineLamp("83") { Description = "Hurry Up" },
			new GamelogicEngineLamp("84") { Description = "Start Button" },
			new GamelogicEngineLamp("85") { Description = "Drop Target" },
			new GamelogicEngineLamp("86") { Description = "Top Lane Left" },
			new GamelogicEngineLamp("87") { Description = "Top Lane Center" },
			new GamelogicEngineLamp("88") { Description = "Top Lane Right" },

			new GamelogicEngineLamp("0") { Description = "GI: Top Insert", Source = LampSource.GI },
			new GamelogicEngineLamp("1") { Description = "GI: Bottom Insert", Source = LampSource.GI },
			new GamelogicEngineLamp("2") { Description = "GI: Right Playfield", Source = LampSource.GI },
			new GamelogicEngineLamp("3") { Description = "GI: CPU String", Source = LampSource.GI },
			new GamelogicEngineLamp("4") { Description = "GI: Left Playfield", Source = LampSource.GI },
		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {
			new GamelogicEngineCoil("01") { Description = "Ball Popper" },
			new GamelogicEngineCoil("02") { Description = "Gun Kicker" },
			new GamelogicEngineCoil("03") { Description = "Outhole" },
			new GamelogicEngineCoil("04") { Description = "Trough", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "eject_coil" },
			new GamelogicEngineCoil("05") { Description = "Right Sling" },
			new GamelogicEngineCoil("06") { Description = "Left Sling" },
			new GamelogicEngineCoil("07") { Description = "Knocker" },
			new GamelogicEngineCoil("08") { Description = "Kickback" },
			new GamelogicEngineCoil("09") { Description = "Plunger", DeviceHint = "Plunger" },
			new GamelogicEngineCoil("10") { Description = "Top Lock" },
			new GamelogicEngineCoil("11") { Description = "Gun Motor" },
			new GamelogicEngineCoil("12") { Description = "Knock Down" },
			new GamelogicEngineCoil("13") { Description = "Left Jet" },
			new GamelogicEngineCoil("14") { Description = "Right Jet" },
			new GamelogicEngineCoil("15") { Description = "Bottom Jet" },
			new GamelogicEngineCoil("16") { Description = "Left Lock" },
			new GamelogicEngineCoil("17") { Description = "Hot Dog Flashlamps", IsLamp = true},
			new GamelogicEngineCoil("18") { Description = "Right Sling Flashlamps", IsLamp = true },
			new GamelogicEngineCoil("19") { Description = "Left Sling Flashlamps", IsLamp = true },
			new GamelogicEngineCoil("20") { Description = "Left Lock Flashlamps", IsLamp = true },
			new GamelogicEngineCoil("21") { Description = "Gun Flashlamps", IsLamp = true},
			new GamelogicEngineCoil("22") { Description = "Right Ramp Flashlamps", IsLamp = true },
			new GamelogicEngineCoil("23") { Description = "Left Ramp Flashlamps", IsLamp = true },
			new GamelogicEngineCoil("24") { Description = "Backglass Flashlamp", IsLamp = true },
			new GamelogicEngineCoil("25") { Description = "Targets Flashlamps", IsLamp = true },
			new GamelogicEngineCoil("26") { Description = "Left Popper Flashlamps", IsLamp = true },
			new GamelogicEngineCoil("27") { Description = "Right Popper" },
			new GamelogicEngineCoil("28") { Description = "Flashlamps Drop Target" },

			new GamelogicEngineCoil(CoilFlipperLowerRight, 46) { Description = "Lower Right Flipper", DeviceHint = "^RightFlipper$"},
			new GamelogicEngineCoil(CoilFlipperLowerLeft, 48) { Description = "Lower Left Flipper", DeviceHint = "^LeftFlipper$"},

			new GamelogicEngineCoil(CoilFlipperUpperRight, 34) { IsUnused = true },
			new GamelogicEngineCoil(CoilFlipperUpperLeft, 36) { IsUnused = true },
		};
	}
}
