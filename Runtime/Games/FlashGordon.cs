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

using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.VPT.Trough;

namespace VisualPinball.Unity
{
	public class FlashGordon : IPinMameGame
	{
		public string Name { get; } = "Flash Gordon";
		public int Year { get; } = 1980;
		public string Manufacturer { get; } = "Bally";
		public int IpdbId { get; } = 874;
		public PinMameRom[] Roms { get; } = {
			new PinMameRom("flashgdn"),
			new PinMameRom("flashgdf", language:PinMameRomLanguage.French),
			new PinMameRom("flashgda", description: "Free Play"),
			new PinMameRom("flashgfa", description: "Free Play", language: PinMameRomLanguage.French),
			new PinMameRom("flashgdp", "Rev.1", "Prototype"),
			new PinMameRom("flashgp2", "Rev.2", "Prototype"),
			new PinMameRom("flashgdv", description:"Vocalizer Sound"),
			new PinMameRom("flashgva", description: "	Vocalizer Sound Free Play"),
		};

		public GamelogicEngineSwitch[] AvailableSwitches { get; } = {
			new GamelogicEngineSwitch { Id = "1", Description = "Left Coin Chute", InputActionHint = InputConstants.ActionInsertCoin1 },
			new GamelogicEngineSwitch { Id = "2", Description = "Center Coin Chute", InputActionHint = InputConstants.ActionInsertCoin2 },
			new GamelogicEngineSwitch { Id = "3", Description = "Right Coin Chute", InputActionHint = InputConstants.ActionInsertCoin3 },
			new GamelogicEngineSwitch { Id = "4", Description = "Fourth Coin Chute", InputActionHint = InputConstants.ActionInsertCoin4 },
			new GamelogicEngineSwitch { Id = "5", Description = "Door: Escape", InputActionHint = InputConstants.ActionCoinDoorCancel },
			new GamelogicEngineSwitch { Id = "6", Description = "Door: Down", InputActionHint = InputConstants.ActionCoinDoorDown },
			new GamelogicEngineSwitch { Id = "7", Description = "Door: Up", InputActionHint = InputConstants.ActionCoinDoorUp },
			new GamelogicEngineSwitch { Id = "8", Description = "Door: Enter", InputActionHint = InputConstants.ActionCoinDoorEnter },
			new GamelogicEngineSwitch { Id = "11", Description = "Launch Ball", InputActionHint = InputConstants.ActionPlunger },
			new GamelogicEngineSwitch { Id = "12", Description = "Catapult Target" },
			new GamelogicEngineSwitch { Id = "13", Description = "Start Button", InputActionHint = InputConstants.ActionStartGame },
			new GamelogicEngineSwitch { Id = "14", Description = "Plumb Bolt Tilt" },
			new GamelogicEngineSwitch { Id = "15", Description = "Left Troll Target" },
			new GamelogicEngineSwitch { Id = "16", Description = "Left Outline" },
			new GamelogicEngineSwitch { Id = "17", Description = "Right Return Lane" },
			new GamelogicEngineSwitch { Id = "18", Description = "Shooter Lane" },
			new GamelogicEngineSwitch { Id = "21", Description = "Slam Tilt" },
			new GamelogicEngineSwitch { Id = "22", Description = "Coin Door Closed", InputActionHint = InputConstants.ActionCoinDoorOpenClose },
			new GamelogicEngineSwitch { Id = "24", Description = "Always Closed", ConstantHint = true},
			new GamelogicEngineSwitch { Id = "25", Description = "Right Troll Target" },
			new GamelogicEngineSwitch { Id = "26", Description = "Left Return Lane" },
			new GamelogicEngineSwitch { Id = "27", Description = "Right Outlane" },
			new GamelogicEngineSwitch { Id = "28", Description = "Right Eject" },
			new GamelogicEngineSwitch { Id = "31", Description = "Trough Eject (jam)", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "jam"},
			new GamelogicEngineSwitch { Id = "32", Description = "Trough Ball 1", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "1" },
			new GamelogicEngineSwitch { Id = "33", Description = "Trough Ball 2", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "2" },
			new GamelogicEngineSwitch { Id = "34", Description = "Trough Ball 3", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "3" },
			new GamelogicEngineSwitch { Id = "35", Description = "Trough Ball 4", DeviceHint = "^Trough\\s*\\d?", DeviceItemHint = "4" },
			new GamelogicEngineSwitch { Id = "36", Description = "Left Popper" },
			new GamelogicEngineSwitch { Id = "37", Description = "Castle Gate" },
			new GamelogicEngineSwitch { Id = "38", Description = "Catapult" },
			new GamelogicEngineSwitch { Id = "41", Description = "Moat Enter" },
			new GamelogicEngineSwitch { Id = "44", Description = "Castle Lock" },
			new GamelogicEngineSwitch { Id = "45", Description = "Left Troll (under playfield)" },
			new GamelogicEngineSwitch { Id = "46", Description = "Right Troll (under playfield)" },
			new GamelogicEngineSwitch { Id = "47", Description = "Left Top Lane" },
			new GamelogicEngineSwitch { Id = "48", Description = "Right Top Lane" },
			new GamelogicEngineSwitch { Id = "51", Description = "Left Slingshot" },
			new GamelogicEngineSwitch { Id = "52", Description = "Right Slingshot" },
			new GamelogicEngineSwitch { Id = "53", Description = "Left Jet Bumper" },
			new GamelogicEngineSwitch { Id = "54", Description = "Bottom Jet Bumper" },
			new GamelogicEngineSwitch { Id = "55", Description = "Right Jet Bumper" },
			new GamelogicEngineSwitch { Id = "56", Description = "Draw-Bridge Up" },
			new GamelogicEngineSwitch { Id = "57", Description = "Draw-Bridge Down" },
			new GamelogicEngineSwitch { Id = "58", Description = "Tower Exit" },
			new GamelogicEngineSwitch { Id = "61", Description = "Left Ramp Enter" },
			new GamelogicEngineSwitch { Id = "62", Description = "Left Ramp Exit" },
			new GamelogicEngineSwitch { Id = "63", Description = "Right Ramp Enter" },
			new GamelogicEngineSwitch { Id = "64", Description = "Right Ramp Exit" },
			new GamelogicEngineSwitch { Id = "65", Description = "Left Loop Low" },
			new GamelogicEngineSwitch { Id = "66", Description = "Left Loop High" },
			new GamelogicEngineSwitch { Id = "67", Description = "Right Loop Low" },
			new GamelogicEngineSwitch { Id = "68", Description = "Right Loop High" },
			new GamelogicEngineSwitch { Id = "71", Description = "Right Bank Top" },
			new GamelogicEngineSwitch { Id = "72", Description = "Right Bank Middle" },
			new GamelogicEngineSwitch { Id = "73", Description = "Right Bank Bottom" },
			new GamelogicEngineSwitch { Id = "74", Description = "Left Troll Up" },
			new GamelogicEngineSwitch { Id = "75", Description = "Right Troll Up" },

			new GamelogicEngineSwitch { Id = "112", Description = "Lower Right Flipper", InputActionHint = InputConstants.ActionRightFlipper },
			new GamelogicEngineSwitch { Id = "114", Description = "Lower Left Flipper", InputActionHint = InputConstants.ActionLeftFlipper },
		};

		public GamelogicEngineLamp[] AvailableLamps { get; } = {
			new GamelogicEngineLamp { Id = "11", Description = "Right Bank Top" },
			new GamelogicEngineLamp { Id = "12", Description = "Right Bank Middle" },
			new GamelogicEngineLamp { Id = "13", Description = "Right Bank Bottom" },
			new GamelogicEngineLamp { Id = "14", Description = "Right Ramp Jackpot" },
			new GamelogicEngineLamp { Id = "15", Description = "Save the Damsel! (2)" },
			new GamelogicEngineLamp { Id = "16", Description = "Dragon Death" },
			new GamelogicEngineLamp { Id = "17", Description = "Dragon Snack" },
			new GamelogicEngineLamp { Id = "18", Description = "Dragon Breath" },
			new GamelogicEngineLamp { Id = "21", Description = "Right Loop Jackpot" },
			new GamelogicEngineLamp { Id = "22", Description = "Right Joust Victory" },
			new GamelogicEngineLamp { Id = "23", Description = "Right Clash!" },
			new GamelogicEngineLamp { Id = "24", Description = "Right Charge!" },
			new GamelogicEngineLamp { Id = "25", Description = "Patron of the Peasants" },
			new GamelogicEngineLamp { Id = "26", Description = "Catapult Ace" },
			new GamelogicEngineLamp { Id = "27", Description = "Joust Champion" },
			new GamelogicEngineLamp { Id = "28", Description = "Castle Crusher" },
			new GamelogicEngineLamp { Id = "31", Description = "Trolls!" },
			new GamelogicEngineLamp { Id = "32", Description = "Extra Ball" },
			new GamelogicEngineLamp { Id = "33", Description = "Merlin's Magic" },
			new GamelogicEngineLamp { Id = "34", Description = "Troll Madness" },
			new GamelogicEngineLamp { Id = "35", Description = "Damsel Madness" },
			new GamelogicEngineLamp { Id = "36", Description = "Peasant Madness" },
			new GamelogicEngineLamp { Id = "37", Description = "Catapult Madness" },
			new GamelogicEngineLamp { Id = "38", Description = "Joust Madness" },
			new GamelogicEngineLamp { Id = "41", Description = "Left Loop Jackpot" },
			new GamelogicEngineLamp { Id = "42", Description = "Left Joust Victory" },
			new GamelogicEngineLamp { Id = "43", Description = "Left Clash!" },
			new GamelogicEngineLamp { Id = "44", Description = "Left Charge!" },
			new GamelogicEngineLamp { Id = "45", Description = "Catapult Jackpot" },
			new GamelogicEngineLamp { Id = "46", Description = "Catapult Slam!" },
			new GamelogicEngineLamp { Id = "47", Description = "BAM!" },
			new GamelogicEngineLamp { Id = "48", Description = "WAM!" },
			new GamelogicEngineLamp { Id = "51", Description = "Center Arrow" },
			new GamelogicEngineLamp { Id = "52", Description = "Battle for the Kingdom" },
			new GamelogicEngineLamp { Id = "53", Description = "Master of Trolls" },
			new GamelogicEngineLamp { Id = "54", Description = "Defender of Damsels" },
			new GamelogicEngineLamp { Id = "55", Description = "Left Top Lane" },
			new GamelogicEngineLamp { Id = "56", Description = "Right Top Lane" },
			new GamelogicEngineLamp { Id = "57", Description = "Left Troll Target" },
			new GamelogicEngineLamp { Id = "58", Description = "Right Troll Target" },
			new GamelogicEngineLamp { Id = "61", Description = "Francois d'Grimm" },
			new GamelogicEngineLamp { Id = "62", Description = "King of Payne" },
			new GamelogicEngineLamp { Id = "63", Description = "Earl of Ego" },
			new GamelogicEngineLamp { Id = "64", Description = "Left Ramp Jackpot" },
			new GamelogicEngineLamp { Id = "65", Description = "Revolting Peasants!" },
			new GamelogicEngineLamp { Id = "66", Description = "Ugly Riot!" },
			new GamelogicEngineLamp { Id = "67", Description = "Angry Mob!" },
			new GamelogicEngineLamp { Id = "68", Description = "Rabble Rouser" },
			new GamelogicEngineLamp { Id = "71", Description = "Howard Hurtz" },
			new GamelogicEngineLamp { Id = "72", Description = "Magic Shield" },
			new GamelogicEngineLamp { Id = "73", Description = "Sir Psycho" },
			new GamelogicEngineLamp { Id = "74", Description = "Duke of Bourbon" },
			new GamelogicEngineLamp { Id = "75", Description = "Castle Lock 2" },
			new GamelogicEngineLamp { Id = "76", Description = "Castle Lock 1" },
			new GamelogicEngineLamp { Id = "77", Description = "Super Jackpot" },
			new GamelogicEngineLamp { Id = "78", Description = "Super Jets (2)" },
			new GamelogicEngineLamp { Id = "81", Description = "Right Outlane" },
			new GamelogicEngineLamp { Id = "82", Description = "Right Return" },
			new GamelogicEngineLamp { Id = "83", Description = "Left Return" },
			new GamelogicEngineLamp { Id = "84", Description = "Left Outlane" },
			new GamelogicEngineLamp { Id = "85", Description = "Castle Lock 3" },
			new GamelogicEngineLamp { Id = "86", Description = "Shoot Again" },
			new GamelogicEngineLamp { Id = "87", Description = "Launch Button" },
			new GamelogicEngineLamp { Id = "88", Description = "Start Button" }
		};

		public GamelogicEngineCoil[] AvailableCoils { get; } = {
			new GamelogicEngineCoil { Id = "01", Description = "Out Hole" },
		};
	}
}
