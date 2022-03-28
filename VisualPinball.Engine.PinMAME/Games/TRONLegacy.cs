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
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.PinMAME.MPUs;

namespace VisualPinball.Engine.PinMAME.Games
{
	[Serializable]
	public class TRONLegacy : Sam
	{
		public override string Name => "TRON Legacy - Limited Edition";
		public override string Id => "trn";
		public override int Year => 2011;
		public override string Manufacturer => "Stern";
		public override int IpdbId => 5707;
		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("trn_100h", "1.00h"),
			new PinMameRom("trn_110h", "1.10h"),
			new PinMameRom("trn_110", "1.10"),
			new PinMameRom("trn_120", "1.20"),
			new PinMameRom("trn_130h", "1.30h"),
			new PinMameRom("trn_140",  "1.4"),
			new PinMameRom("trn_140h", "1.40h"),
			new PinMameRom("trn_150", "1.50"),
			new PinMameRom("trn_160", "1.6"),
			new PinMameRom("trn_170", "1.7"),
			new PinMameRom("trn_174", "1.74"),
			new PinMameRom("trn_174h", "1.74h"),
			new PinMameRom("trn_17402", "1.7402"),
			new PinMameRom("trn_1741h", "1.741h"),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new GamelogicEngineSwitch("01") { Description = "TRO(N)" },
			new GamelogicEngineSwitch("02") { Description = "TR(O)N" },
			new GamelogicEngineSwitch("03") { Description = "T(R)ON" },
			new GamelogicEngineSwitch("04") { Description = "(T)RON" },
			new GamelogicEngineSwitch("07") { Description = "(Z)USE" },
			new GamelogicEngineSwitch("08") { Description = "Z(U)SE" },
			new GamelogicEngineSwitch("11") { Description = "Video Game Eject" },
			new GamelogicEngineSwitch("12") { Description = "Zen Rollover" },
			new GamelogicEngineSwitch("13") { Description = "ZUS(E)" },
			new GamelogicEngineSwitch("14") { Description = "C(L)U" },
			new GamelogicEngineSwitch("18") { Description = "Trough #4 Left" },
			new GamelogicEngineSwitch("19") { Description = "Trough #3" },
			new GamelogicEngineSwitch("20") { Description = "Trough #2" },
			new GamelogicEngineSwitch("21") { Description = "Trough #1 Right" },
			new GamelogicEngineSwitch("22") { Description = "Trough Jam" },
			new GamelogicEngineSwitch("23") { Description = "Shooter Lane" },
			new GamelogicEngineSwitch("24") { Description = "Left Outlane" },
			new GamelogicEngineSwitch("25") { Description = "(C)LU" },
			new GamelogicEngineSwitch("26") { Description = "Left Slingshot" },
			new GamelogicEngineSwitch("27") { Description = "Right Slingshot" },
			new GamelogicEngineSwitch("28") { Description = "CL(U)" },
			new GamelogicEngineSwitch("29") { Description = "Right Outlane" },
			new GamelogicEngineSwitch("30") { Description = "Left Bumper" },
			new GamelogicEngineSwitch("31") { Description = "Right Bumper" },
			new GamelogicEngineSwitch("32") { Description = "Bottom Bumper" },
			new GamelogicEngineSwitch("34") { Description = "Right Ramp Exit" },
			new GamelogicEngineSwitch("35") { Description = "Left Ramp Entrance" },
			new GamelogicEngineSwitch("36") { Description = "Right Orbit Spinner" },
			new GamelogicEngineSwitch("37") { Description = "Left Ramp Exit" },
			new GamelogicEngineSwitch("38") { Description = "Right Ramp Entrance" },
			new GamelogicEngineSwitch("39") { Description = "Right Inner Loop" },
			new GamelogicEngineSwitch("41") { Description = "Disc Opto" },
			new GamelogicEngineSwitch("43") { Description = "Left Orbit" },
			new GamelogicEngineSwitch("44") { Description = "Left Spinner" },
			new GamelogicEngineSwitch("46") { Description = "Right Orbit" },
			new GamelogicEngineSwitch("48") { Description = "ZU(S)E" },
			new GamelogicEngineSwitch("49") { Description = "Recognizer 3-Bank (Left)" },
			new GamelogicEngineSwitch("50") { Description = "Recognizer 3-Bank (Center)" },
			new GamelogicEngineSwitch("51") { Description = "Recognizer 3-Bank (Right)" },
			new GamelogicEngineSwitch("52") { Description = "3-Bank Motor (Down)" },
			new GamelogicEngineSwitch("53") { Description = "3-Bank Motor (Up)" },
			new GamelogicEngineSwitch("54") { Description = "Recognizer Motor Pos 1" },
			new GamelogicEngineSwitch("55") { Description = "Recognizer Motor Pos 2" },
			new GamelogicEngineSwitch("56") { Description = "Recognizer Motor Pos 3" }
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {
			new GamelogicEngineLamp("01") { Description = "TRO(N)" },
			new GamelogicEngineLamp("02") { Description = "TR(O)N" },
			new GamelogicEngineLamp("03") { Description = "T(R)ON" },
			new GamelogicEngineLamp("04") { Description = "(T)RON" },
			new GamelogicEngineLamp("05") { Description = "Tron Double Scoring" },
			new GamelogicEngineLamp("06") { Description = "Bumpers" },
			new GamelogicEngineLamp("07") { Description = "Spinners" },
			new GamelogicEngineLamp("08") { Description = "Left Outlane" },
			new GamelogicEngineLamp("09") { Description = "(C)LU" },
			new GamelogicEngineLamp("10") { Description = "Z(U)SE" },
			new GamelogicEngineLamp("11") { Description = "Left Ramp (Light Cycle)" },
			new GamelogicEngineLamp("12") { Description = "(Z)USE" },
			new GamelogicEngineLamp("13") { Description = "Left Orbit (CLU)" },
			new GamelogicEngineLamp("14") { Description = "Left Orbit (Light Cycle)" },
			new GamelogicEngineLamp("15") { Description = "Left Orbit (Disc)" },
			new GamelogicEngineLamp("16") { Description = "Left Loop Arrow" },
			new GamelogicEngineLamp("17") { Description = "Center Portal" },
			new GamelogicEngineLamp("18") { Description = "Center TRON" },
			new GamelogicEngineLamp("19") { Description = "Center Recognizer" },
			new GamelogicEngineLamp("20") { Description = "Center Light Cycle" },
			new GamelogicEngineLamp("21") { Description = "Center Disc" },
			new GamelogicEngineLamp("22") { Description = "Center Quorra" },
			new GamelogicEngineLamp("23") { Description = "Center ZUSE" },
			new GamelogicEngineLamp("24") { Description = "Center CLU" },
			new GamelogicEngineLamp("25") { Description = "Center Gem" },
			new GamelogicEngineLamp("26") { Description = "Shoot Again" },
			new GamelogicEngineLamp("27") { Description = "Center Flynn" },
			new GamelogicEngineLamp("28") { Description = "Eject CLU" },
			new GamelogicEngineLamp("29") { Description = "ZUS(E)" },
			new GamelogicEngineLamp("30") { Description = "C(L)U" },
			new GamelogicEngineLamp("31") { Description = "CL(U)" },
			new GamelogicEngineLamp("32") { Description = "Right Outlane" },
			new GamelogicEngineLamp("33") { Description = "Right Loop Arrow" },
			new GamelogicEngineLamp("34") { Description = "Right Orbit (Disc)" },
			new GamelogicEngineLamp("35") { Description = "Right Orbit (Light Cycle)" },
			new GamelogicEngineLamp("36") { Description = "Right Orbit (CLU)" },
			new GamelogicEngineLamp("37") { Description = "Eject Extra Ball" },
			new GamelogicEngineLamp("38") { Description = "Eject Light Cylce" },
			new GamelogicEngineLamp("39") { Description = "Eject Quorra" },
			new GamelogicEngineLamp("40") { Description = "Eject Portal" },
			new GamelogicEngineLamp("42") { Description = "Right Ramp (Light Cycle)" },
			new GamelogicEngineLamp("43") { Description = "ZU(S)E" },
			new GamelogicEngineLamp("45") { Description = "Flynn's Arcade" },
			new GamelogicEngineLamp("46") { Description = "Left Bumper" },
			new GamelogicEngineLamp("47") { Description = "Right Bumper" },
			new GamelogicEngineLamp("48") { Description = "Bottom Bumper" },
			new GamelogicEngineLamp("49") { Description = "Left Ramp Arrow" },
			new GamelogicEngineLamp("50") { Description = "Left Ramp (Disc)" },
			new GamelogicEngineLamp("51") { Description = "Recognizer Right" },
			new GamelogicEngineLamp("52") { Description = "Recognizer Center" },
			new GamelogicEngineLamp("53") { Description = "Recognizer Left" },
			new GamelogicEngineLamp("54") { Description = "Recognizer 3-Bank" },
			new GamelogicEngineLamp("55") { Description = "Right Ramp (Disc)" },
			new GamelogicEngineLamp("56") { Description = "Right Ramp Arrow" },
			new GamelogicEngineLamp("57") { Description = "Right Inner Loop Arrow" },
			new GamelogicEngineLamp("58") { Description = "Right Inner Loop (Disc)" },
			new GamelogicEngineLamp("59") { Description = "Right Inner Loop (Light Cycle)" },
			new GamelogicEngineLamp("60") { Description = "Advance Quorra" },
			new GamelogicEngineLamp("61") { Description = "Left Inner Loop (CLU)" },
			new GamelogicEngineLamp("62") { Description = "Left Inner Loop (Light Cycle)" },
			new GamelogicEngineLamp("63") { Description = "Left Inner Loop (Disc)" },
			new GamelogicEngineLamp("64") { Description = "Left Inner Loop Arrow" },
			new GamelogicEngineLamp("65") { Description = "Start Button" },
			new GamelogicEngineLamp("66") { Description = "Tournament Start Button" },
		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {
			new GamelogicEngineCoil("01") { Description = "Trough Up-Kicker" },
			new GamelogicEngineCoil("02") { Description = "Auto Launch" },
			new GamelogicEngineCoil("03") { Description = "4-Bank Drop Target" },
			new GamelogicEngineCoil("04") { Description = "Video Game Eject" },
			new GamelogicEngineCoil("05") { Description = "Disc Motor Power" },
			new GamelogicEngineCoil("06") { Description = "Recognizer 3-Bank Motor / Relay" },
			new GamelogicEngineCoil("07") { Description = "Orbit Up / Down Post" },
			new GamelogicEngineCoil("08") { Description = "Shaker Motor (Optional)" },
			new GamelogicEngineCoil("09") { Description = "Left Pop Bumper" },
			new GamelogicEngineCoil("10") { Description = "Right Pop Bumper" },
			new GamelogicEngineCoil("11") { Description = "Bottom Pop Bumper" },
			new GamelogicEngineCoil("12") { Description = "Upper Left Flipper" },
			new GamelogicEngineCoil("13") { Description = "Left Slingshot" },
			new GamelogicEngineCoil("14") { Description = "Right Slingshot" },
			new GamelogicEngineCoil("17") { Description = "Zen Flasher", IsLamp = true },
			new GamelogicEngineCoil("18") { Description = "Flash: Video Game", IsLamp = true },
			new GamelogicEngineCoil("19") { Description = "Flash: Right Domes (X2)", IsLamp = true },
			new GamelogicEngineCoil("20") { Description = "Flash: Bottom Arch (Left)", IsLamp = true },
			new GamelogicEngineCoil("21") { Description = "Flash: Bottom Arch (Right)", IsLamp = true },
			new GamelogicEngineCoil("22") { Description = "Disc Direction Relay" },
			new GamelogicEngineCoil("23") { Description = "Recognizer Relay" },
			new GamelogicEngineCoil("24") { Description = "Optional (e.g. Coin Meter)" },
			new GamelogicEngineCoil("25") { Description = "Flash: Left Domes (X2)", IsLamp = true },
			new GamelogicEngineCoil("26") { Description = "Flash: Disc (Left)", IsLamp = true },
			new GamelogicEngineCoil("27") { Description = "Flash: Disc (Right)", IsLamp = true },
			new GamelogicEngineCoil("28") { Description = "Flash: Backpanel (X2)", IsLamp = true },
			new GamelogicEngineCoil("29") { Description = "Flash: Recognizer", IsLamp = true },
			new GamelogicEngineCoil("30") { Description = "Disc Motor Relay" },
			new GamelogicEngineCoil("31") { Description = "Flash: Red Disc (Left) (X2)", IsLamp = true },
			new GamelogicEngineCoil("32") { Description = "Flash: Red Disc (Right) (X2)", IsLamp = true },
		};
	}
}
