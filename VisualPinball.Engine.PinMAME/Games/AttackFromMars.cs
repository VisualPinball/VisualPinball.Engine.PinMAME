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
using VisualPinball.Engine.VPT.Plunger;
using VisualPinball.Engine.VPT.Trough;

namespace VisualPinball.Engine.PinMAME.Games
{
	[Serializable]
	public class AttackFromMars : Wpc
	{
		public override string Name { get; } = "Attack From Mars";
		public override string Id { get; } = "afm";
		public override int Year { get; } = 1995;
		public override string Manufacturer { get; } = "Bally";
		public override int IpdbId { get; } = 3781;
		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("afm_113", "1.13"),
			new PinMameRom("afm_10", "1.0"),
			new PinMameRom("afm_11", "1.1"),
			new PinMameRom("afm_11u", "1.1", "Ultrapin"),
			new PinMameRom("afm_11pfx", "1.1", "Pinball FX"),
			new PinMameRom("afm_113b", "1.13b"),
			new PinMameRom("afm_f10", "0.10", "FreeWPC"),
			new PinMameRom("afm_f20", "0.20", "FreeWPC"),
			new PinMameRom("afm_f32", "0.32", "FreeWPC"),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new GamelogicEngineSwitch("11") { Description = "Launch Ball", InputActionHint = InputConstants.ActionPlunger },

			new GamelogicEngineSwitch("13") { Description = "Start Button", InputActionHint = InputConstants.ActionStartGame },
			new GamelogicEngineSwitch("14") { Description = "Plumb Bolt Tilt" },

			new GamelogicEngineSwitch("16") { Description = "Left Outline" },
			new GamelogicEngineSwitch("17") { Description = "Right Return Lane" },
			new GamelogicEngineSwitch("18") { Description = "Shooter Lane" },

			new GamelogicEngineSwitch("21") { Description = "Slam Tilt",  InputActionHint = InputConstants.ActionSlamTilt },
			new GamelogicEngineSwitch("22") { Description = "Coin Door Closed", NormallyClosed = true, InputActionHint = InputConstants.ActionCoinDoorOpenClose },

			new GamelogicEngineSwitch("24") { Description = "Always Closed", ConstantHint = SwitchConstantHint.AlwaysClosed},

			new GamelogicEngineSwitch("26") { Description = "Left Return Lane" },
			new GamelogicEngineSwitch("27") { Description = "Right Outlane" },

			new GamelogicEngineSwitch("31") { Description = "Trough Eject (jam)", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "jam"},
			new GamelogicEngineSwitch("32") { Description = "Trough Ball 1", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "1" },
			new GamelogicEngineSwitch("33") { Description = "Trough Ball 2", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "2" },
			new GamelogicEngineSwitch("34") { Description = "Trough Ball 3", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "3" },
			new GamelogicEngineSwitch("35") { Description = "Trough Ball 4", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "4" },
			new GamelogicEngineSwitch("36") { Description = "Left Popper", ConstantHint = SwitchConstantHint.AlwaysOpen },
			new GamelogicEngineSwitch("37") { Description = "Right Popper", ConstantHint = SwitchConstantHint.AlwaysOpen },
			new GamelogicEngineSwitch("38") { Description = "Left Top Lane" },

			new GamelogicEngineSwitch("41") { Description = "MARTI\"A\"N Target" },
			new GamelogicEngineSwitch("42") { Description = "MARTIA\"N\" Target" },
			new GamelogicEngineSwitch("43") { Description = "MAR\"T\"IAN Target" },
			new GamelogicEngineSwitch("44") { Description = "MART\"I\"AN Target" },
			new GamelogicEngineSwitch("45") { Description = "Left Motor Bank" },
			new GamelogicEngineSwitch("46") { Description = "Center Motor Bank" },
			new GamelogicEngineSwitch("47") { Description = "Right Motor Bank" },
			new GamelogicEngineSwitch("48") { Description = "Right Top Lane" },

			new GamelogicEngineSwitch("51") { Description = "Left Slingshot (Kicker)" },
			new GamelogicEngineSwitch("52") { Description = "Right Slingshot (Kicker)" },
			new GamelogicEngineSwitch("53") { Description = "Left Jet Bumper" },
			new GamelogicEngineSwitch("54") { Description = "Bottom Jet Bumper" },
			new GamelogicEngineSwitch("55") { Description = "Right Jet Bumper" },
			new GamelogicEngineSwitch("56") { Description = "\"M\"ARTIAN Target" },
			new GamelogicEngineSwitch("57") { Description = "M\"A\"RTIAN Target" },
			new GamelogicEngineSwitch("58") { Description = "MA\"R\"TIAN Target" },

			new GamelogicEngineSwitch("61") { Description = "Left Ramp Enter" },
			new GamelogicEngineSwitch("62") { Description = "Center Ramp Enter" },
			new GamelogicEngineSwitch("63") { Description = "Right Ramp Enter" },
			new GamelogicEngineSwitch("64") { Description = "Left Ramp Exit" },
			new GamelogicEngineSwitch("65") { Description = "Right Ramp Exit" },
			new GamelogicEngineSwitch("66") { Description = "Motor Bank Down" },
			new GamelogicEngineSwitch("67") { Description = "Motor Bank Up" },

			new GamelogicEngineSwitch("71") { Description = "Right Bank High" },
			new GamelogicEngineSwitch("72") { Description = "Right Bank Low" },
			new GamelogicEngineSwitch("73") { Description = "Left Loop High" },
			new GamelogicEngineSwitch("74") { Description = "Left Loop Low" },
			new GamelogicEngineSwitch("75") { Description = "Left Saucer Target" },
			new GamelogicEngineSwitch("76") { Description = "Right Saucer Target" },
			new GamelogicEngineSwitch("77") { Description = "Drop Targets" },
			new GamelogicEngineSwitch("78") { Description = "Center Trough" },
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
			new GamelogicEngineLamp("11") { Description = "Super Jets" },
			new GamelogicEngineLamp("12") { Description = "Super Jackpot" },
			new GamelogicEngineLamp("13") { Description = "Martian Attack Multi-ball" },
			new GamelogicEngineLamp("14") { Description = "Annihilation" },
			new GamelogicEngineLamp("15") { Description = "Return To Battle (2)" },
			new GamelogicEngineLamp("16") { Description = "Conquer Mars" },
			new GamelogicEngineLamp("17") { Description = "5-Way Combo" },
			new GamelogicEngineLamp("18") { Description = "Drop Target" },

			new GamelogicEngineLamp("21") { Description = "Big-O-Beam 1" },
			new GamelogicEngineLamp("22") { Description = "Big-O-Beam 2" },
			new GamelogicEngineLamp("23") { Description = "Big-O-Beam 3" },
			new GamelogicEngineLamp("24") { Description = "Left Ramp Jackpot" },
			new GamelogicEngineLamp("25") { Description = "Left Ramp Arrow" },
			new GamelogicEngineLamp("26") { Description = "Lock 2" },
			new GamelogicEngineLamp("27") { Description = "Lock 3" },
			new GamelogicEngineLamp("28") { Description = "Center Ramp Jackpot" },

			new GamelogicEngineLamp("31") { Description = "Tractor Beam 1" },
			new GamelogicEngineLamp("32") { Description = "Tractor Beam 2" },
			new GamelogicEngineLamp("33") { Description = "Tractor Beam 3" },
			new GamelogicEngineLamp("34") { Description = "Right Ramp Jackpot" },
			new GamelogicEngineLamp("35") { Description = "Right Ramp Arrow" },
			new GamelogicEngineLamp("36") { Description = "Martian Attack" },
			new GamelogicEngineLamp("37") { Description = "Rule Universe" },
			new GamelogicEngineLamp("38") { Description = "Stroke Of Luck" },

			new GamelogicEngineLamp("41") { Description = "Right Loop Arrow" },
			new GamelogicEngineLamp("42") { Description = "Center Ramp Arrow" },
			new GamelogicEngineLamp("43") { Description = "Left Top Lane" },
			new GamelogicEngineLamp("44") { Description = "Right Top Lane" },
			new GamelogicEngineLamp("45") { Description = "Left Motor Bank" },
			new GamelogicEngineLamp("46") { Description = "Center Motor Bank" },
			new GamelogicEngineLamp("47") { Description = "Right Motor Bank" },
			new GamelogicEngineLamp("48") { Description = "MARTIAN Target" },

			new GamelogicEngineLamp("51") { Description = "Attack Mars" },
			new GamelogicEngineLamp("52") { Description = "D.C., U.S.A." },
			new GamelogicEngineLamp("53") { Description = "London, England" },
			new GamelogicEngineLamp("54") { Description = "Light Lock" },
			new GamelogicEngineLamp("55") { Description = "Lock 1" },
			new GamelogicEngineLamp("56") { Description = "Pisa, Italy" },
			new GamelogicEngineLamp("57") { Description = "Berlin, Germany" },
			new GamelogicEngineLamp("58") { Description = "Paris, France" },

			new GamelogicEngineLamp("61") { Description = "Francois d'Grimm" },
			new GamelogicEngineLamp("62") { Description = "King of Payne" },
			new GamelogicEngineLamp("63") { Description = "Earl of Ego" },
			new GamelogicEngineLamp("64") { Description = "Left Ramp Jackpot" },
			new GamelogicEngineLamp("65") { Description = "Revolting Peasants!" },
			new GamelogicEngineLamp("66") { Description = "Ugly Riot!" },
			new GamelogicEngineLamp("67") { Description = "Angry Mob!" },
			new GamelogicEngineLamp("68") { Description = "Rabble Rouser" },

			new GamelogicEngineLamp("71") { Description = "Howard Hurtz" },
			new GamelogicEngineLamp("72") { Description = "Magic Shield" },
			new GamelogicEngineLamp("73") { Description = "Sir Psycho" },
			new GamelogicEngineLamp("74") { Description = "Duke of Bourbon" },
			new GamelogicEngineLamp("75") { Description = "Castle Lock 2" },
			new GamelogicEngineLamp("76") { Description = "Castle Lock 1" },
			new GamelogicEngineLamp("77") { Description = "Super Jackpot" },
			new GamelogicEngineLamp("78") { Description = "Super Jets (2)" },

			new GamelogicEngineLamp("81") { Description = "Right Outlane" },
			new GamelogicEngineLamp("82") { Description = "Right Return" },
			new GamelogicEngineLamp("83") { Description = "Left Return" },
			new GamelogicEngineLamp("84") { Description = "Left Outlane" },
			new GamelogicEngineLamp("85") { Description = "Castle Lock 3" },
			new GamelogicEngineLamp("86") { Description = "Shoot Again" },
			new GamelogicEngineLamp("87") { Description = "Launch Button" },
			new GamelogicEngineLamp("88") { Description = "Start Button" }
		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {
			new GamelogicEngineCoil("01", 1) { Description = "Auto Plunger", DeviceHint = "Plunger", DeviceItemHint = Plunger.FireCoilId },
			new GamelogicEngineCoil("02", 2) { Description = "Trough Eject", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "eject_coil" },
			new GamelogicEngineCoil("03", 3) { Description = "Left Popper" },
			new GamelogicEngineCoil("04", 4) { Description = "Right Popper" },
			new GamelogicEngineCoil("05", 5) { Description = "Left Alien Low" },
			new GamelogicEngineCoil("06", 6) { Description = "Left Alien High" },
			new GamelogicEngineCoil("07", 7) { Description = "Knocker" },
			new GamelogicEngineCoil("08", 8) { Description = "Right Alien High" },
			new GamelogicEngineCoil("09", 9) { Description = "Left Slingshot" },
			new GamelogicEngineCoil("10") { Description = "Right Slingshot" },
			new GamelogicEngineCoil("11") { Description = "Right Slingshot" },
			new GamelogicEngineCoil("12") { Description = "Left Jet Bumper" },
			new GamelogicEngineCoil("13") { Description = "Bottom Jet Bumper" },
			new GamelogicEngineCoil("14") { Description = "Right Alien Low" },
			new GamelogicEngineCoil("15") { Description = "Saucer Shake" },
			new GamelogicEngineCoil("16") { Description = "Drop Target" },
			new GamelogicEngineCoil("17") { Description = "Right Ramp High Flashers", IsLamp = true },
			new GamelogicEngineCoil("18") { Description = "Right Ramp Low Flasher", IsLamp = true },
			new GamelogicEngineCoil("19") { Description = "Right Side High Flashers", IsLamp = true },
			new GamelogicEngineCoil("20") { Description = "Right Side Low Flasher", IsLamp = true },
			new GamelogicEngineCoil("21") { Description = "Center Arrow Flasher", IsLamp = true },
			new GamelogicEngineCoil("22") { Description = "Jets", IsLamp = true },
			new GamelogicEngineCoil("23") { Description = "Saucer Dome", IsLamp = true },
			new GamelogicEngineCoil("24") { Description = "Motor Bank", IsLamp = true },
			new GamelogicEngineCoil("25") { Description = "Left Ramp Left" },
			new GamelogicEngineCoil("26") { Description = "Left Ramp Right" },
			new GamelogicEngineCoil("27") { Description = "Left Side High" },
			new GamelogicEngineCoil("28") { Description = "Left Side Low" },
			new GamelogicEngineCoil("33") { Description = "Right Gate" },
			new GamelogicEngineCoil("34") { Description = "Left Gate" },
			new GamelogicEngineCoil("35") { Description = "Diverter Power" },
			new GamelogicEngineCoil("36") { Description = "Diverter Hold" },
			new GamelogicEngineCoil("37") { Description = "L.E.D. Clock", IsLamp = true },
			new GamelogicEngineCoil("38") { Description = "L.E.D. Data", IsLamp = true },
			new GamelogicEngineCoil("39") { Description = "Strobe Light", IsLamp = true },
		};
	}
}
