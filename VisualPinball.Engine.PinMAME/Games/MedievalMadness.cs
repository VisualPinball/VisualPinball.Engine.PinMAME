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
	public class MedievalMadness : Wpc
	{
		public override string Name { get; } = "Medieval Madness";
		public override string Id { get; } = "mm";
		public override int Year { get; } = 1997;
		public override string Manufacturer { get; } = "Williams";
		public override int IpdbId { get; } = 4032;
		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("mm_05", "0.5"),
			new PinMameRom("mm_10", "1.0"),
			new PinMameRom("mm_10u", "1.0", "Ultrapin"),
			new PinMameRom("mm_109", "1.09"),
			new PinMameRom("mm_109b", "1.09b"),
			new PinMameRom("mm_109c", "1.09c", "Profanity ROM"),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new GamelogicEngineSwitch("11") { Description = "Launch Ball", InputActionHint = InputConstants.ActionPlunger },
			new GamelogicEngineSwitch("12") { Description = "Catapult Target" },
			new GamelogicEngineSwitch("13") { Description = "Start Button", InputActionHint = InputConstants.ActionStartGame },
			new GamelogicEngineSwitch("14") { Description = "Plumb Bolt Tilt" },
			new GamelogicEngineSwitch("15") { Description = "Left Troll Target" },
			new GamelogicEngineSwitch("16") { Description = "Left Outline" },
			new GamelogicEngineSwitch("17") { Description = "Right Return Lane" },
			new GamelogicEngineSwitch("18") { Description = "Shooter Lane" },

			new GamelogicEngineSwitch("21") { Description = "Slam Tilt",  InputActionHint = InputConstants.ActionSlamTilt },
			new GamelogicEngineSwitch("22") { Description = "Coin Door Closed", NormallyClosed = true, InputActionHint = InputConstants.ActionCoinDoorOpenClose },
			new GamelogicEngineSwitch("24") { Description = "Always Closed", ConstantHint = SwitchConstantHint.AlwaysClosed},
			new GamelogicEngineSwitch("25") { Description = "Right Troll Target" },
			new GamelogicEngineSwitch("26") { Description = "Left Return Lane" },
			new GamelogicEngineSwitch("27") { Description = "Right Outlane" },
			new GamelogicEngineSwitch("28") { Description = "Right Eject" },

			new GamelogicEngineSwitch("31") { Description = "Trough Eject (jam)", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "jam"},
			new GamelogicEngineSwitch("32") { Description = "Trough Ball 1", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "1" },
			new GamelogicEngineSwitch("33") { Description = "Trough Ball 2", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "2" },
			new GamelogicEngineSwitch("34") { Description = "Trough Ball 3", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "3" },
			new GamelogicEngineSwitch("35") { Description = "Trough Ball 4", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "4" },
			new GamelogicEngineSwitch("36") { Description = "Left Popper", ConstantHint = SwitchConstantHint.AlwaysOpen },
			new GamelogicEngineSwitch("37") { Description = "Castle Gate", ConstantHint = SwitchConstantHint.AlwaysOpen },
			new GamelogicEngineSwitch("38") { Description = "Catapult" },

			new GamelogicEngineSwitch("41") { Description = "Moat Enter", ConstantHint = SwitchConstantHint.AlwaysOpen },
			new GamelogicEngineSwitch("44") { Description = "Castle Lock" },
			new GamelogicEngineSwitch("45") { Description = "Left Troll (under playfield)" },
			new GamelogicEngineSwitch("46") { Description = "Right Troll (under playfield)" },
			new GamelogicEngineSwitch("47") { Description = "Left Top Lane" },
			new GamelogicEngineSwitch("48") { Description = "Right Top Lane" },

			new GamelogicEngineSwitch("51") { Description = "Left Slingshot" },
			new GamelogicEngineSwitch("52") { Description = "Right Slingshot" },
			new GamelogicEngineSwitch("53") { Description = "Left Jet Bumper" },
			new GamelogicEngineSwitch("54") { Description = "Bottom Jet Bumper" },
			new GamelogicEngineSwitch("55") { Description = "Right Jet Bumper" },
			new GamelogicEngineSwitch("56") { Description = "Draw-Bridge Up", ConstantHint = SwitchConstantHint.AlwaysClosed },
			new GamelogicEngineSwitch("57") { Description = "Draw-Bridge Down" },
			new GamelogicEngineSwitch("58") { Description = "Tower Exit" },

			new GamelogicEngineSwitch("61") { Description = "Left Ramp Enter" },
			new GamelogicEngineSwitch("62") { Description = "Left Ramp Exit" },
			new GamelogicEngineSwitch("63") { Description = "Right Ramp Enter" },
			new GamelogicEngineSwitch("64") { Description = "Right Ramp Exit" },
			new GamelogicEngineSwitch("65") { Description = "Left Loop Low" },
			new GamelogicEngineSwitch("66") { Description = "Left Loop High" },
			new GamelogicEngineSwitch("67") { Description = "Right Loop Low" },
			new GamelogicEngineSwitch("68") { Description = "Right Loop High" },

			new GamelogicEngineSwitch("71") { Description = "Right Bank Top" },
			new GamelogicEngineSwitch("72") { Description = "Right Bank Middle" },
			new GamelogicEngineSwitch("73") { Description = "Right Bank Bottom" },
			new GamelogicEngineSwitch("74") { Description = "Left Troll Up" },
			new GamelogicEngineSwitch("75") { Description = "Right Troll Up" },

			// new GamelogicEngineSwitch("112") { Description = "Lower Right Flipper", InputActionHint = InputConstants.ActionRightFlipper },
			// new GamelogicEngineSwitch("114") { Description = "Lower Left Flipper", InputActionHint = InputConstants.ActionLeftFlipper },
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
			new GamelogicEngineLamp("11") { Description = "Right Bank Top" },
			new GamelogicEngineLamp("12") { Description = "Right Bank Middle" },
			new GamelogicEngineLamp("13") { Description = "Right Bank Bottom" },
			new GamelogicEngineLamp("14") { Description = "Right Ramp Jackpot" },
			new GamelogicEngineLamp("15") { Description = "Save the Damsel! (2)" },
			new GamelogicEngineLamp("16") { Description = "Dragon Death" },
			new GamelogicEngineLamp("17") { Description = "Dragon Snack" },
			new GamelogicEngineLamp("18") { Description = "Dragon Breath" },

			new GamelogicEngineLamp("21") { Description = "Right Loop Jackpot" },
			new GamelogicEngineLamp("22") { Description = "Right Joust Victory" },
			new GamelogicEngineLamp("23") { Description = "Right Clash!" },
			new GamelogicEngineLamp("24") { Description = "Right Charge!" },
			new GamelogicEngineLamp("25") { Description = "Patron of the Peasants" },
			new GamelogicEngineLamp("26") { Description = "Catapult Ace" },
			new GamelogicEngineLamp("27") { Description = "Joust Champion" },
			new GamelogicEngineLamp("28") { Description = "Castle Crusher" },

			new GamelogicEngineLamp("31") { Description = "Trolls!" },
			new GamelogicEngineLamp("32") { Description = "Extra Ball" },
			new GamelogicEngineLamp("33") { Description = "Merlin's Magic" },
			new GamelogicEngineLamp("34") { Description = "Troll Madness" },
			new GamelogicEngineLamp("35") { Description = "Damsel Madness" },
			new GamelogicEngineLamp("36") { Description = "Peasant Madness" },
			new GamelogicEngineLamp("37") { Description = "Catapult Madness" },
			new GamelogicEngineLamp("38") { Description = "Joust Madness" },

			new GamelogicEngineLamp("41") { Description = "Left Loop Jackpot" },
			new GamelogicEngineLamp("42") { Description = "Left Joust Victory" },
			new GamelogicEngineLamp("43") { Description = "Left Clash!" },
			new GamelogicEngineLamp("44") { Description = "Left Charge!" },
			new GamelogicEngineLamp("45") { Description = "Catapult Jackpot" },
			new GamelogicEngineLamp("46") { Description = "Catapult Slam!" },
			new GamelogicEngineLamp("47") { Description = "BAM!" },
			new GamelogicEngineLamp("48") { Description = "WAM!" },

			new GamelogicEngineLamp("51") { Description = "Center Arrow" },
			new GamelogicEngineLamp("52") { Description = "Battle for the Kingdom" },
			new GamelogicEngineLamp("53") { Description = "Master of Trolls" },
			new GamelogicEngineLamp("54") { Description = "Defender of Damsels" },
			new GamelogicEngineLamp("55") { Description = "Left Top Lane" },
			new GamelogicEngineLamp("56") { Description = "Right Top Lane" },
			new GamelogicEngineLamp("57") { Description = "Left Troll Target" },
			new GamelogicEngineLamp("58") { Description = "Right Troll Target" },

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
			new GamelogicEngineCoil("01", 1) { Description = "Auto Plunger", DeviceHint = "Plunger", DeviceItemHint = "c_autofire" },
			new GamelogicEngineCoil("02", 2) { Description = "Trough Eject", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "eject_coil" },
			new GamelogicEngineCoil("03", 3) { Description = "Left Popper" },
			new GamelogicEngineCoil("04", 4) { Description = "Castle" },
			new GamelogicEngineCoil("05", 5) { Description = "Castle Gate (Power)" },
			new GamelogicEngineCoil("06", 6) { Description = "Castle Gate (Hold)" },
			new GamelogicEngineCoil("07", 7) { Description = "Knocker" },
			new GamelogicEngineCoil("08", 8) { Description = "Catapult" },
			new GamelogicEngineCoil("09", 9) { Description = "Right Eject" },
			new GamelogicEngineCoil("10") { Description = "Left Slingshot" },
			new GamelogicEngineCoil("11") { Description = "Right Slingshot" },
			new GamelogicEngineCoil("12") { Description = "Left Jet Bumper" },
			new GamelogicEngineCoil("13") { Description = "Bottom Jet Bumper" },
			new GamelogicEngineCoil("14") { Description = "Right Jet Bumper" },
			new GamelogicEngineCoil("15") { Description = "Tower Diverter (Power)" },
			new GamelogicEngineCoil("16") { Description = "Tower Diverter (Hold)" },
			new GamelogicEngineCoil("17") { Description = "Left Side Low Flashers", IsLamp = true },
			new GamelogicEngineCoil("18") { Description = "Left Ramp Flashers", IsLamp = true },
			new GamelogicEngineCoil("19") { Description = "Left Side High Flashers", IsLamp = true },
			new GamelogicEngineCoil("20") { Description = "Right Side High Flashers", IsLamp = true },
			new GamelogicEngineCoil("21") { Description = "Right Ramp Flashers", IsLamp = true },
			new GamelogicEngineCoil("22") { Description = "Castle Right Side Flashers", IsLamp = true },
			new GamelogicEngineCoil("23") { Description = "Right Side Low Flashers", IsLamp = true },
			new GamelogicEngineCoil("24") { Description = "Moat Flashers", IsLamp = true },
			new GamelogicEngineCoil("25") { Description = "Castle Left Side Flashers", IsLamp = true },
			new GamelogicEngineCoil("26") { Description = "Tower Lock Post" },
			new GamelogicEngineCoil("27") { Description = "Right Gate" },
			new GamelogicEngineCoil("28") { Description = "Left Gate" },
			// new GamelogicEngineCoil("29", 46) { Description = "Lower Right Flipper (power)", PlayfieldItemHint = "^(RightFlipper|RFlipper|FlipperRight|FlipperR)$" },
			// new GamelogicEngineCoil("30") { Description = "Lower Right Flipper (hold)", MainCoilIdOfHoldCoil = "29" },
			// new GamelogicEngineCoil("31", 48) { Description = "Lower Left Flipper (power)", PlayfieldItemHint = "^(LeftFlipper|LFlipper|FlipperLeft|FlipperL)$" },
			// new GamelogicEngineCoil("32") { Description = "Lower Left Flipper (hold)", MainCoilIdOfHoldCoil = "31" },
			new GamelogicEngineCoil("33") { Description = "Left Troll (Power)" },
			new GamelogicEngineCoil("34") { Description = "Left Troll (Hold)" },
			new GamelogicEngineCoil("35") { Description = "Right Troll (Power)" },
			new GamelogicEngineCoil("36") { Description = "Right Troll (Hold)" },
			new GamelogicEngineCoil("37") { Description = "Drawbridge Motor" },
		};
	}
}
