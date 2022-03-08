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

using System;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.Math;
using VisualPinball.Engine.PinMAME.MPUs;

namespace VisualPinball.Engine.PinMAME.Games
{
	[Serializable]
	public class TheWalkingDead : Sam
	{
		public override string Name => "The Walking Dead - Limited Edition";
		public override string Id => "twd";
		public override int Year => 2014;
		public override string Manufacturer => "Stern";
		public override int IpdbId => 6156;
		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("twd_105", "1.05"),
			new PinMameRom("twd_111", "1.11"),
			new PinMameRom("twd_111h", "1.11h"),
			new PinMameRom("twd_119",  "1.19"),
			new PinMameRom("twd_119h", "1.19h"),
			new PinMameRom("twd_124",  "1.24"),
			new PinMameRom("twd_124h", "1.24h"),
			new PinMameRom("twd_125", "1.25"),
			new PinMameRom("twd_125h", "1.25h"),
			new PinMameRom("twd_128", "1.28"),
			new PinMameRom("twd_128h", "1.28h"),
			new PinMameRom("twd_141", "1.41"),
			new PinMameRom("twd_141h", "1.41h"),
			new PinMameRom("twd_153", "1.53"),
			new PinMameRom("twd_153h", "1.53h"),
			new PinMameRom("twd_156", "1.56"),
			new PinMameRom("twd_156h", "1.56h"),
			new PinMameRom("twd_160", "1.60.0"),
			new PinMameRom("twd_160h", "1.60.0h"),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
			new GamelogicEngineLamp("1") { Description = "Start Button" },
			new GamelogicEngineLamp("2") { Description = "Tournament Start Button" },
			new GamelogicEngineLamp("3") { Description = "12X Playfield values" },
			new GamelogicEngineLamp("4") { Description = "Shoot Again" },
			new GamelogicEngineLamp("5") { Description = "4 Walkers Killed" },
			new GamelogicEngineLamp("6") { Description = "3 Walkers Killed" },
			new GamelogicEngineLamp("7") { Description = "2 Walkers Killed" },
			new GamelogicEngineLamp("8") { Description = "1 Walker Killed" },
			new GamelogicEngineLamp("9") { Description = "40 Walkers Killed" },
			new GamelogicEngineLamp("10") { Description = "Last Man Standing" },
			new GamelogicEngineLamp("11") { Description = "5 Walkers Killed" },
			new GamelogicEngineLamp("12") { Description = "10 Walkers Killed" },
			new GamelogicEngineLamp("13") { Description = "20 Walkers Killed" },
			new GamelogicEngineLamp("14") { Description = "30 Walkers Killed" },
			new GamelogicEngineLamp("15") { Description = "Hammer Multi-Killed" },
			new GamelogicEngineLamp("16") { Description = "Sword Muiti-Kill" },
			new GamelogicEngineLamp("17") { Description = "Crossbow Muiti-Kill" },
			new GamelogicEngineLamp("18") { Description = "Gun Multi-Kill" },
			new GamelogicEngineLamp("19") { Description = "Knife Multi-Kill" },
			new GamelogicEngineLamp("20") { Description = "Axe Multi-Kill" },
			new GamelogicEngineLamp("21") { Description = "Horde" },
			new GamelogicEngineLamp("22") { Description = "Left Outlane" },
			new GamelogicEngineLamp("23") { Description = "Left Return Lane" },
			new GamelogicEngineLamp("24") { Description = "R. Loop Arrow" },
			new GamelogicEngineLamp("25") { Description = "Blood Bath" },
			new GamelogicEngineLamp("26") { Description = "First Aid" },
			new GamelogicEngineLamp("27") { Description = "Weapons" },
			new GamelogicEngineLamp("28") { Description = "Food" },
			new GamelogicEngineLamp("29") { Description = "L. Ramp Walker Kill" },
			new GamelogicEngineLamp("30") { Description = "L. Loop Walker Kill" },
			new GamelogicEngineLamp("31") { Description = "L. Loop Multi-Kill" },
			new GamelogicEngineLamp("32") { Description = "Barn Mode" },
			new GamelogicEngineLamp("34") { Description = "L. Ramp Multi-Kill" },
			new GamelogicEngineLamp("35") { Description = "CDC Mode" },
			new GamelogicEngineLamp("36") { Description = "L. Ramp ArroW" },
			new GamelogicEngineLamp("37") { Description = "Right Outlane" },
			new GamelogicEngineLamp("38") { Description = "Right Return Lane" },
			new GamelogicEngineLamp("39") { Description = "Extra Ball" },
			new GamelogicEngineLamp("40") { Description = "Welcome To Woodbury" },
			new GamelogicEngineLamp("41") { Description = "R. Ramp Arrow" },
			new GamelogicEngineLamp("42") { Description = "R. Ramp Walker Kill" },
			new GamelogicEngineLamp("43") { Description = "R. Ramp Multi-Kill" },
			new GamelogicEngineLamp("44") { Description = "Arena Mode" },
			new GamelogicEngineLamp("45") { Description = "Well Walker Kill" },
			new GamelogicEngineLamp("46") { Description = "(W)ELL" },
			new GamelogicEngineLamp("47") { Description = "W(E)LL" },
			new GamelogicEngineLamp("48") { Description = "WE(L)L" },
			new GamelogicEngineLamp("49") { Description = "WEL(L)" },
			new GamelogicEngineLamp("50") { Description = "Well Walker" },
			new GamelogicEngineLamp("51") { Description = "R. Loop Walker Kill" },
			new GamelogicEngineLamp("52") { Description = "R. Loop Multi-Kill" },
			new GamelogicEngineLamp("53") { Description = "Tunnel Mode" },
			new GamelogicEngineLamp("54") { Description = "Siege" },
			new GamelogicEngineLamp("55") { Description = "R. Prison Standup" },
			new GamelogicEngineLamp("56") { Description = "L. Prison Standup" },
			new GamelogicEngineLamp("57") { Description = "C. Lane Arrow" },
			new GamelogicEngineLamp("58") { Description = "Riot Mode" },
			new GamelogicEngineLamp("59") { Description = "C. Lane Multi-Kill" },
			new GamelogicEngineLamp("60") { Description = "Top Bumper" },
			new GamelogicEngineLamp("61") { Description = "Right Bumper" },
			new GamelogicEngineLamp("62") { Description = "Left Bumper" },
			new GamelogicEngineLamp("63") { Description = "C. Lane Walker" },
			new GamelogicEngineLamp("64") { Description = "(P)RISON" },
			new GamelogicEngineLamp("65") { Description = "P(R)ISON" },
			new GamelogicEngineLamp("66") { Description = "PR(I)SON" },
			new GamelogicEngineLamp("67") { Description = "PRI(S)ON" },
			new GamelogicEngineLamp("68") { Description = "PRIS(O)N" },
			new GamelogicEngineLamp("69") { Description = "PRISO(N)" },
			new GamelogicEngineLamp("70") { Description = "Crossbow" },
			new GamelogicEngineLamp("71") { Description = "Fish Tank" },
			new GamelogicEngineLamp("72") { Description = "Tower" },
			new GamelogicEngineLamp("73") { Description = "Fish Tank Head #1" },
			new GamelogicEngineLamp("74") { Description = "Fish Tank Head #2" },
			new GamelogicEngineLamp("75") { Description = "Fish Tank Head #3" },
			new GamelogicEngineLamp("76") { Description = "Left Top Lane" },
			new GamelogicEngineLamp("77") { Description = "Right Top Lane" },
			new GamelogicEngineLamp("78") { Description = "Bicycle Girl" },
			new GamelogicEngineLamp("79") { Description = "Star Rollover (Bot.) Red", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("79") { Description = "Star Rollover (Bot.) Grn", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("79") { Description = "Star Rollover (Bot.) Blu", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },
			new GamelogicEngineLamp("80") { Description = "Star Rollover (Top.) Red", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("80") { Description = "Star Rollover (Top.) Grn", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("80") { Description = "Star Rollover (Top.) Blu", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },
			new GamelogicEngineLamp("81") { Description = "Fire Button (Red)", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("81") { Description = "Fire Button (Grn)", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("81") { Description = "Fire Button (Blu)", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },
		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {
			new GamelogicEngineCoil("01") { Description = "Trough Up-Kicker" },
			new GamelogicEngineCoil("02") { Description = "Auto Launch" },
			new GamelogicEngineCoil("03") { Description = "Prison Doors (Power)" },
			new GamelogicEngineCoil("04") { Description = "Prison Doors (Hold)" },
			new GamelogicEngineCoil("05") { Description = "Ramp Magnet Diverter" },
			new GamelogicEngineCoil("06") { Description = "Well Magnet" },
			new GamelogicEngineCoil("07") { Description = "Prison Magnet" },
			new GamelogicEngineCoil("08") { Description = "Shaker Motor (Optional)" },
			new GamelogicEngineCoil("09") { Description = "Left Pop Bumper" },
			new GamelogicEngineCoil("10") { Description = "Right Pop Bumper" },
			new GamelogicEngineCoil("11") { Description = "Top Pop Bumper" },
			new GamelogicEngineCoil("12") { Description = "Left 3-Bank Drop Target" },
			new GamelogicEngineCoil("13") { Description = "Left Slingshot" },
			new GamelogicEngineCoil("14") { Description = "Right Slingshot" },
			new GamelogicEngineCoil("15") { Description = "Left Flipper (50V RED/YEL)" },
			new GamelogicEngineCoil("16") { Description = "Right Flipper (50V RED/YEL)" },
			new GamelogicEngineCoil("19") { Description = "Flash: Well Walker", IsLamp = true },
			new GamelogicEngineCoil("20") { Description = "Flash: Right Spinner", IsLamp = true },
			new GamelogicEngineCoil("21") { Description = "Crossbow Motor" },
			new GamelogicEngineCoil("24") { Description = "Optional (e.g. Coin Meter)" },
			new GamelogicEngineCoil("25") { Description = "Flash: Pop Bumpers", IsLamp = true },
			new GamelogicEngineCoil("26") { Description = "Flash: Prison (Top)", IsLamp = true },
			new GamelogicEngineCoil("27") { Description = "Flash: Prison (Bottom) (X2)", IsLamp = true },
			new GamelogicEngineCoil("28") { Description = "Flash: Left Dome", IsLamp = true },
			new GamelogicEngineCoil("29") { Description = "Flash: Right Dome", IsLamp = true },
			new GamelogicEngineCoil("31") { Description = "Flash: Left Loop", IsLamp = true },
			new GamelogicEngineCoil("32") { Description = "Flash: Center Loop", IsLamp = true },
		};
	}
}
