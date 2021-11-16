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
using VisualPinball.Engine.Math;
using VisualPinball.Engine.PinMAME.MPUs;

namespace VisualPinball.Engine.PinMAME.Games
{
	[Serializable]
	public class StarTrekEnterprise : Wpc
	{
		public override string Name => "Star Trek - Enterprise Limited Edition";
		public override string Id => "star-trek-stern";
		public override int Year => 2013;
		public override string Manufacturer => "Stern";
		public override int IpdbId => 6046;
		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("st_120", "1.20"),
			new PinMameRom("st_130", "1.30"),
			new PinMameRom("st_140", "1.40"),
			new PinMameRom("st_140h", "1.40h"),
			new PinMameRom("st_141h", "1.41h"),
			new PinMameRom("st_142h", "1.42h"),
			new PinMameRom("st_150", "1.50"),
			new PinMameRom("st_150h", "1.50h"),
			new PinMameRom("st_160", "1.60"),
			new PinMameRom("st_160h", "1.60h"),
			new PinMameRom("st_161", "1.61"),
			new PinMameRom("st_161h", "1.61h"),
		};

		protected override GamelogicEngineSwitch[] Switches { get; } = {
			new GamelogicEngineSwitch("01") { Description = "(Beam) Me Up" },
			new GamelogicEngineSwitch("02") { Description = "Beam (Me) Up" },
			new GamelogicEngineSwitch("04") { Description = "Beam Me (Up)" },
			new GamelogicEngineSwitch("07") { Description = "Right 3-Bank Target (Top)" },
			new GamelogicEngineSwitch("08") { Description = "Right 3-Bank Target (Center)" },
			new GamelogicEngineSwitch("09") { Description = "Right 3-Bank Target (Bottom)" },
			new GamelogicEngineSwitch("10") { Description = "Left Eject" },
			new GamelogicEngineSwitch("11") { Description = "Center Drop Target" },
			new GamelogicEngineSwitch("12") { Description = "Spinner" },
			new GamelogicEngineSwitch("13") { Description = "Warp Ramp Enter" },
			new GamelogicEngineSwitch("14") { Description = "Left Ramp Enter" },
			new GamelogicEngineSwitch("15") { Description = "Tournament Start" },
			new GamelogicEngineSwitch("16") { Description = "Start" },
			new GamelogicEngineSwitch("18") { Description = "Trough #4 (Left)" },
			new GamelogicEngineSwitch("19") { Description = "Trough #3" },
			new GamelogicEngineSwitch("20") { Description = "Trough #2" },
			new GamelogicEngineSwitch("21") { Description = "Trough #1 (Right)" },
			new GamelogicEngineSwitch("22") { Description = "Trough Jam" },
			new GamelogicEngineSwitch("23") { Description = "Shooter Lane" },
			new GamelogicEngineSwitch("24") { Description = "(T)REK" },
			new GamelogicEngineSwitch("25") { Description = "T(R)EK" },
			new GamelogicEngineSwitch("26") { Description = "Left Slingshot" },
			new GamelogicEngineSwitch("27") { Description = "Right Slingshot" },
			new GamelogicEngineSwitch("28") { Description = "TR(E)K" },
			new GamelogicEngineSwitch("29") { Description = "TRE(K)" },
			new GamelogicEngineSwitch("30") { Description = "Left Pop Bumper" },
			new GamelogicEngineSwitch("31") { Description = "Right Pop Bumper" },
			new GamelogicEngineSwitch("32") { Description = "Bottom Pop Bumper" },
			new GamelogicEngineSwitch("33") { Description = "Center Lock (Bottom)" },
			new GamelogicEngineSwitch("34") { Description = "Center Lock (Top)" },
			new GamelogicEngineSwitch("35") { Description = "Left Ramp Exit" },
			new GamelogicEngineSwitch("36") { Description = "Warp Ramp Exit" },
			new GamelogicEngineSwitch("37") { Description = "Right Warp Entrance" },
			new GamelogicEngineSwitch("38") { Description = "Right Ramp Exit" },
			new GamelogicEngineSwitch("39") { Description = "Center 3-Bank Target (Top)" },
			new GamelogicEngineSwitch("40") { Description = "Center 3-Bank Target (Center)" },
			new GamelogicEngineSwitch("41") { Description = "Center 3-Bank Target (Bottom)" },
			new GamelogicEngineSwitch("42") { Description = "Left 2-Bank Target (Top)" },
			new GamelogicEngineSwitch("43") { Description = "Left 2-Bank Target (Bottom)" },
			new GamelogicEngineSwitch("44") { Description = "Behind Upper Flipper" },
			new GamelogicEngineSwitch("45") { Description = "Red Target 1" },
			new GamelogicEngineSwitch("46") { Description = "Red Target 2" },
			new GamelogicEngineSwitch("47") { Description = "Red Target 3" },
			new GamelogicEngineSwitch("48") { Description = "Big Red Target" },
			new GamelogicEngineSwitch("49") { Description = "Red Target 5" },
			new GamelogicEngineSwitch("50") { Description = "Red Target 6" },
			new GamelogicEngineSwitch("51") { Description = "Right Orbit" },
			new GamelogicEngineSwitch("52") { Description = "Left Orbit" },
			new GamelogicEngineSwitch("53") { Description = "Ship Crash" },
		};

		public override GamelogicEngineLamp[] AvailableLamps { get; } = {

			new GamelogicEngineLamp("84") { Description = "Left Ramp Emblem - R", DeviceHint = "^L1$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("85") { Description = "Left Ramp Emblem - G", DeviceHint = "^L1$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("86") { Description = "Left Ramp Emblem - B", DeviceHint = "^L1$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("81") { Description = "Red Target 2 - R", DeviceHint = "^L2$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("87") { Description = "Red Target 2 - G", DeviceHint = "^L2$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("88") { Description = "Red Target 2 - B", DeviceHint = "^L2$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("83") { Description = "Left Ramp Enterprise Arrow - R", DeviceHint = "^L3$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("82") { Description = "Left Ramp Enterprise Arrow - G", DeviceHint = "^L3$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("89") { Description = "Left Ramp Enterprise Arrow - B", DeviceHint = "^L3$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("92") { Description = "Red Target 3 - R", DeviceHint = "^L4$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("91") { Description = "Red Target 3 - G", DeviceHint = "^L4$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("90") { Description = "Red Target 3 - B", DeviceHint = "^L4$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("93") { Description = "Center Lane Emblem - R", DeviceHint = "^L5$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("94") { Description = "Center Lane Emblem - G", DeviceHint = "^L5$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("95") { Description = "Center Lane Emblem - B", DeviceHint = "^L5$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("96") { Description = "Center Lane Enterprise Arrow - R", DeviceHint = "^L6$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("97") { Description = "Center Lane Enterprise Arrow - G", DeviceHint = "^L6$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("98") { Description = "Center Lane Enterprise Arrow - B", DeviceHint = "^L6$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("99") { Description = "Red Target 4 - R", DeviceHint = "^L7$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("100") { Description = "Red Target 4 - G", DeviceHint = "^L7$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("101") { Description = "Red Target 4 - B", DeviceHint = "^L7$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("102") { Description = "Black Hole Arrow - R", DeviceHint = "^L8$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("103") { Description = "Black Hole Arrow - G", DeviceHint = "^L8$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("104") { Description = "Black Hole Arrow - B", DeviceHint = "^L8$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("113") { Description = "Right Orbit Emblem - R", DeviceHint = "^L9$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("114") { Description = "Right Orbit Emblem - G", DeviceHint = "^L9$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("115") { Description = "Right Orbit Emblem - B", DeviceHint = "^L9$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("116") { Description = "Right Orbit Enterprise Arrow - R", DeviceHint = "^L10$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("117") { Description = "Right Orbit Enterprise Arrow - G", DeviceHint = "^L10$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("118") { Description = "Right Orbit Enterprise Arrow - B", DeviceHint = "^L10$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("119") { Description = "Red Target 6 - R", DeviceHint = "^L11$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("120") { Description = "Red Target 6 - G", DeviceHint = "^L11$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("121") { Description = "Red Target 6 - B", DeviceHint = "^L11$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("122") { Description = "Right Ramp Emblem - R", DeviceHint = "^L12$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("123") { Description = "Right Ramp Emblem - G", DeviceHint = "^L12$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("124") { Description = "Right Ramp Emblem - B", DeviceHint = "^L12$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("125") { Description = "Right Ramp Enterprise Arrow - R", DeviceHint = "^L13$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("126") { Description = "Right Ramp Enterprise Arrow - G", DeviceHint = "^L13$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("127") { Description = "Right Ramp Enterprise Arrow - B", DeviceHint = "^L13$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("128") { Description = "Red Target 5 - R", DeviceHint = "^L14$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("129") { Description = "Red Target 5 - G", DeviceHint = "^L14$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("130") { Description = "Red Target 5 - B", DeviceHint = "^L14$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("131") { Description = "Special - R", DeviceHint = "^L15$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("133") { Description = "Special - G", DeviceHint = "^L15$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("132") { Description = "Special - B", DeviceHint = "^L15$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("134") { Description = "Away Team - R", DeviceHint = "^L16$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("136") { Description = "Away Team - G", DeviceHint = "^L16$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("135") { Description = "Away Team - B", DeviceHint = "^L16$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("146") { Description = "Left Eject Lock - R", DeviceHint = "^L17$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("147") { Description = "Left Eject Lock - G", DeviceHint = "^L17$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("148") { Description = "Left Eject Lock - B", DeviceHint = "^L17$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("149") { Description = "Mission Start - R", DeviceHint = "^L18$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("150") { Description = "Mission Start - G", DeviceHint = "^L18$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("151") { Description = "Mission Start - B", DeviceHint = "^L18$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("152") { Description = "Left 2 Bank - Top - R", DeviceHint = "^L19$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("153") { Description = "Left 2 Bank - Top - G", DeviceHint = "^L19$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("154") { Description = "Left 2 Bank - Top - B", DeviceHint = "^L19$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("155") { Description = "Left Orbit Emblem - R", DeviceHint = "^L20$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("156") { Description = "Left Orbit Emblem - G", DeviceHint = "^L20$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("157") { Description = "Left Orbit Emblem - B", DeviceHint = "^L20$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("158") { Description = "Red Target 1 - R", DeviceHint = "^L21$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("159") { Description = "Red Target 1 - G", DeviceHint = "^L21$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("160") { Description = "Red Target 1 - B", DeviceHint = "^L21$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("161") { Description = "Left Orbit Enterprise Arrow - R", DeviceHint = "^L22$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("162") { Description = "Left Orbit Enterprise Arrow - G", DeviceHint = "^L22$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("163") { Description = "Left Orbit Enterprise Arrow - B", DeviceHint = "^L22$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("164") { Description = "Left 2 Bank (Bottom) - R", DeviceHint = "^L23$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("165") { Description = "Left 2 Bank (Bottom) - G", DeviceHint = "^L23$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("166") { Description = "Left 2 Bank (Bottom) - B", DeviceHint = "^L23$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("167") { Description = "Left Eject Emblem - R", DeviceHint = "^L24$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("168") { Description = "Left Eject Emblem - G", DeviceHint = "^L24$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("169") { Description = "Left Eject Emblem - B", DeviceHint = "^L24$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("170") { Description = "Left Eject Enterprise Arrow - R", DeviceHint = "^L25$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("171") { Description = "Left Eject Enterprise Arrow - G", DeviceHint = "^L25$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("172") { Description = "Left Eject Enterprise Arrow - B", DeviceHint = "^L25$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("179") { Description = "Kickback - R", DeviceHint = "^L26$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("177") { Description = "Kickback - G", DeviceHint = "^L26$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("178") { Description = "Kickback - B", DeviceHint = "^L26$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("182") { Description = "(T)REK - R", DeviceHint = "^L27$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("180") { Description = "(T)REK - G", DeviceHint = "^L27$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("181") { Description = "(T)REK - B", DeviceHint = "^L27$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("185") { Description = "T(R)EK - R", DeviceHint = "^L28$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("183") { Description = "T(R)EK - G", DeviceHint = "^L28$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("184") { Description = "T(R)EK - B", DeviceHint = "^L28$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("186") { Description = "Center 3 Bank (Bottom) - R", DeviceHint = "^L29$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("187") { Description = "Center 3 Bank (Bottom) - G", DeviceHint = "^L29$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("188") { Description = "Center 3 Bank (Bottom) - B", DeviceHint = "^L29$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("189") { Description = "Center 3 Bank (Center) - R", DeviceHint = "^L30$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("190") { Description = "Center 3 Bank (Center) - G", DeviceHint = "^L30$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("191") { Description = "Center 3 Bank (Center) - B", DeviceHint = "^L30$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("192") { Description = "Center 3 Bank (Top) - R", DeviceHint = "^L31$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("193") { Description = "Center 3 Bank (Top) - G", DeviceHint = "^L31$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("194") { Description = "Center 3 Bank (Top) - B", DeviceHint = "^L31$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("195") { Description = "Center Lane Lock - R", DeviceHint = "^L32$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("197") { Description = "Center Lane Lock - G", DeviceHint = "^L32$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("196") { Description = "Center Lane Lock - B", DeviceHint = "^L32$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("198") { Description = "Extra Ball - R", DeviceHint = "^L33$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("200") { Description = "Extra Ball - G", DeviceHint = "^L33$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("199") { Description = "Extra Ball - B", DeviceHint = "^L33$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("214") { Description = "Shoot Again - R", DeviceHint = "^L34$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("215") { Description = "Shoot Again - G", DeviceHint = "^L34$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("216") { Description = "Shoot Again - B", DeviceHint = "^L34$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("211") { Description = "The Captain's Chair - R", DeviceHint = "^L35$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("217") { Description = "The Captain's Chair - G", DeviceHint = "^L35$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("218") { Description = "The Captain's Chair - B", DeviceHint = "^L35$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("213") { Description = "Save the Enterprise - R", DeviceHint = "^L36$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("212") { Description = "Save the Enterprise - G", DeviceHint = "^L36$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("219") { Description = "Save the Enterprise - B", DeviceHint = "^L36$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("222") { Description = "Nero - R", DeviceHint = "^L37$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("221") { Description = "Nero - G", DeviceHint = "^L37$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("220") { Description = "Nero - B", DeviceHint = "^L37$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("223") { Description = "Destroy the Drill - R", DeviceHint = "^L38$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("224") { Description = "Destroy the Drill - G", DeviceHint = "^L38$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("225") { Description = "Destroy the Drill - B", DeviceHint = "^L38$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("226") { Description = "Space Jump - R", DeviceHint = "^L39$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("227") { Description = "Space Jump - G", DeviceHint = "^L39$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("228") { Description = "Space Jump - B", DeviceHint = "^L39$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("229") { Description = "Prime Directive - R", DeviceHint = "^L40$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("230") { Description = "Prime Directive - G", DeviceHint = "^L40$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("231") { Description = "Prime Directive - B", DeviceHint = "^L40$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("232") { Description = "Klingon Battle - R", DeviceHint = "^L41$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("233") { Description = "Klingon Battle - G", DeviceHint = "^L41$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("234") { Description = "Klingon Battle - B", DeviceHint = "^L41$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("235") { Description = "Status 1 (Bottom) - R", DeviceHint = "^L42$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("236") { Description = "Status 1 (Bottom) - G", DeviceHint = "^L42$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("237") { Description = "Status 1 (Bottom) - B", DeviceHint = "^L42$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("238") { Description = "Status 2 - R", DeviceHint = "^L43$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("239") { Description = "Status 2 - G", DeviceHint = "^L43$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("240") { Description = "Status 2 - B", DeviceHint = "^L43$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("241") { Description = "Status 3 - R", DeviceHint = "^L44$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("242") { Description = "Status 3 - G", DeviceHint = "^L44$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("243") { Description = "Status 3 - B", DeviceHint = "^L44$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("244") { Description = "Status 4 - R", DeviceHint = "^L45$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("245") { Description = "Status 4 - G", DeviceHint = "^L45$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("246") { Description = "Status 4 - B", DeviceHint = "^L45$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("247") { Description = "Status 5 - R", DeviceHint = "^L46$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("248") { Description = "Status 5 - G", DeviceHint = "^L46$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("249") { Description = "Status 5 - B", DeviceHint = "^L46$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("250") { Description = "Status 6 - R", DeviceHint = "^L47$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("251") { Description = "Status 6 - G", DeviceHint = "^L47$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("252") { Description = "Status 6 - B", DeviceHint = "^L47$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("253") { Description = "Status 7 - R", DeviceHint = "^L48$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("254") { Description = "Status 7 - G", DeviceHint = "^L48$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("255") { Description = "Status 7 - B", DeviceHint = "^L48$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("256") { Description = "Status 8 (Top) - R", DeviceHint = "^L49$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("257") { Description = "Status 8 (Top) - G", DeviceHint = "^L49$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("258") { Description = "Status 8 (Top) - B", DeviceHint = "^L49$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("276") { Description = "Warp Ramp Red", DeviceHint = "^L50$" },
			new GamelogicEngineLamp("277") { Description = "Enterprise", DeviceHint = "^L51$" },

			new GamelogicEngineLamp("278") { Description = "Warp Ramp Emblem - R", DeviceHint = "^L52$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("279") { Description = "Warp Ramp Emblem - G", DeviceHint = "^L52$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("280") { Description = "Warp Ramp Emblem - B", DeviceHint = "^L52$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("281") { Description = "(Beam) Me Up - B", DeviceHint = "^L53$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("283") { Description = "(Beam) Me Up - B", DeviceHint = "^L53$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("282") { Description = "(Beam) Me Up - B", DeviceHint = "^L53$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("284") { Description = "Beam (Me) Up - R", DeviceHint = "^L54$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("286") { Description = "Beam (Me) Up - G", DeviceHint = "^L54$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("285") { Description = "Beam (Me) Up - B", DeviceHint = "^L54$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("287") { Description = "Beam Me (Up)", DeviceHint = "^L55$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("289") { Description = "Beam Me (Up) - G", DeviceHint = "^L55$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("288") { Description = "Beam Me (Up) - B", DeviceHint = "^L55$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("290") { Description = "Vengeance Saucer", DeviceHint = "^L56$" },
			new GamelogicEngineLamp("291") { Description = "Vengeance Nacelles (X2)", DeviceHint = "^L57$" },

			new GamelogicEngineLamp("308") { Description = "Right 3 Bank (Top) - R", DeviceHint = "^L58$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("309") { Description = "Right 3 Bank (Top) - G", DeviceHint = "^L58$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("310") { Description = "Right 3 Bank (Top) - B", DeviceHint = "^L58$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("311") { Description = "Right 3 Bank (Center) - R", DeviceHint = "^L59$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("312") { Description = "Right 3 Bank (Center) - G", DeviceHint = "^L59$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("313") { Description = "Right 3 Bank (Center) - B", DeviceHint = "^L59$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("314") { Description = "Right 3 Bank (Bottom) - R", DeviceHint = "^L60$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("315") { Description = "Right 3 Bank (Bottom) - G", DeviceHint = "^L60$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("316") { Description = "Right 3 Bank (Bottom) - B", DeviceHint = "^L60$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("317") { Description = "TR(E)K - R", DeviceHint = "^L61$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("319") { Description = "TR(E)K - G", DeviceHint = "^L61$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("318") { Description = "TR(E)K - B", DeviceHint = "^L61$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("320") { Description = "TRE(K) - R", DeviceHint = "^L62$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("322") { Description = "TRE(K) - G", DeviceHint = "^L62$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("321") { Description = "TRE(K) - B", DeviceHint = "^L62$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("300") { Description = "Left Apron (X2) - R", DeviceHint = "^L63$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("302") { Description = "Left Apron (X2) - G", DeviceHint = "^L63$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("301") { Description = "Left Apron (X2) - B", DeviceHint = "^L63$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("303") { Description = "Right Apron (X2) - R", DeviceHint = "^L64$", Type = LampType.RgbMulti, Channel = ColorChannel.Red },
			new GamelogicEngineLamp("305") { Description = "Right Apron (X2) - G", DeviceHint = "^L64$", Type = LampType.RgbMulti, Channel = ColorChannel.Green },
			new GamelogicEngineLamp("304") { Description = "Right Apron (X2) - B", DeviceHint = "^L64$", Type = LampType.RgbMulti, Channel = ColorChannel.Blue },

			new GamelogicEngineLamp("80") { Description = "Start Button", DeviceHint = "^L65$" },
			new GamelogicEngineLamp("78") { Description = "Fire (Red)", DeviceHint = "^L66$" },
			new GamelogicEngineLamp("77") { Description = "Fire (Green)", DeviceHint = "^L67$" },
			new GamelogicEngineLamp("76") { Description = "Fire (Blue)", DeviceHint = "^L68$" },
			new GamelogicEngineLamp("79") { Description = "Tournament Start Button", DeviceHint = "^L69$" },
			new GamelogicEngineLamp("295") { Description = "Warp Chaser 1 (LE)", DeviceHint = "^L70$" },
			new GamelogicEngineLamp("292") { Description = "Warp Chaser 2 (LE)", DeviceHint = "^L71$" },
			new GamelogicEngineLamp("293") { Description = "Warp Chaser 3 (LE)", DeviceHint = "^L72$" },
			new GamelogicEngineLamp("294") { Description = "Warp Chaser 4 (LE)", DeviceHint = "^L73$" },
			new GamelogicEngineLamp("299") { Description = "Warp Chaser 5 (LE)", DeviceHint = "^L74$" },
			new GamelogicEngineLamp("296") { Description = "Warp Chaser 6 (LE)", DeviceHint = "^L75$" },
			new GamelogicEngineLamp("297") { Description = "Warp Chaser 7 (LE)", DeviceHint = "^L76$" },
			new GamelogicEngineLamp("298") { Description = "Warp Chaser 8 (LE)", DeviceHint = "^L77$" },
			new GamelogicEngineLamp("50") { Description = "Cabinet Side Enterprise (X2) (LE)", DeviceHint = "^L78$" },
			new GamelogicEngineLamp("51") { Description = "Cabinet Side Phaser 1 (X2) (Front) (LE)", DeviceHint = "^L79$" },
			new GamelogicEngineLamp("52") { Description = "Cabinet Side Phaser 2 (X2) (LE)", DeviceHint = "^L80$" },
			new GamelogicEngineLamp("53") { Description = "Cabinet Side Phaser 3 (X2) (LE)", DeviceHint = "^L81$" },
			new GamelogicEngineLamp("54") { Description = "Cabinet Side Phaser 4 (X2) (LE)", DeviceHint = "^L82$" },
			new GamelogicEngineLamp("55") { Description = "Cabinet Side Phaser 5 (X2) (LE)", DeviceHint = "^L83$" },
			new GamelogicEngineLamp("56") { Description = "Cabinet Side Phaser 6 (X2) (LE)", DeviceHint = "^L84$" },
			new GamelogicEngineLamp("57") { Description = "Cabinet Side Phaser 7 (X2) (LE)", DeviceHint = "^L85$" },
			new GamelogicEngineLamp("58") { Description = "Cabinet Side Phaser 8 (X2) (LE)", DeviceHint = "^L86$" },
			new GamelogicEngineLamp("59") { Description = "Cabinet Side Phaser 9 (X2) (LE)", DeviceHint = "^L87$" },
			new GamelogicEngineLamp("60") { Description = "Cabinet Side Phaser 10 (X2) (LE)", DeviceHint = "^L88$" },
			new GamelogicEngineLamp("61") { Description = "Cabinet Side Phaser 11 (X2) (LE)", DeviceHint = "^L89$" },
			new GamelogicEngineLamp("62") { Description = "Cabinet Side Phaser 12(X2) (LE)", DeviceHint = "^L90$" },
			new GamelogicEngineLamp("63") { Description = "Cabinet Side Phaser 13 (X2) (LE)", DeviceHint = "^L91$" },
			new GamelogicEngineLamp("64") { Description = "Cabinet Side Phaser 14 (X2) (LE)", DeviceHint = "^L92$" },
			new GamelogicEngineLamp("65") { Description = "Cabinet Side Phaser 15 (X2) (LE)", DeviceHint = "^L93$" },
			new GamelogicEngineLamp("66") { Description = "Cabinet Side Phaser 16 (X2) (LE)", DeviceHint = "^L94$" },
			new GamelogicEngineLamp("67") { Description = "Cabinet Side Phaser 17 (X2) (LE)", DeviceHint = "^L95$" },
			new GamelogicEngineLamp("68") { Description = "Cabinet Side Phaser 18 (X2) (LE)", DeviceHint = "^L96$" },
			new GamelogicEngineLamp("69") { Description = "Cabinet Side Phaser 19 (X2) (LE)", DeviceHint = "^L97$" },
			new GamelogicEngineLamp("70") { Description = "Cabinet Side Phaser 20 (X2) (LE)", DeviceHint = "^L98$" },
			new GamelogicEngineLamp("71") { Description = "Cabinet Side Phaser 21 (X2) (LE)", DeviceHint = "^L99$" },
			new GamelogicEngineLamp("72") { Description = "Cabinet Side Phaser 22 (Back) (X2) (LE)", DeviceHint = "^L100$" },
		};

		protected override GamelogicEngineCoil[] GameCoils { get; } = {

			new GamelogicEngineCoil("01") { Description = "Trough Up-Kicker" },
			new GamelogicEngineCoil("02") { Description = "Auto Launch" },
			new GamelogicEngineCoil("03") { Description = "Magnet" },
			new GamelogicEngineCoil("04") { Description = "Center Drop Target Up" },
			new GamelogicEngineCoil("05") { Description = "Center Drop Target Down" },
			new GamelogicEngineCoil("06") { Description = "Left Eject" },
			new GamelogicEngineCoil("07") { Description = "Vengeance Kick Back" },
			new GamelogicEngineCoil("08") { Description = "Shaker Motor (optional)" },
			new GamelogicEngineCoil("09") { Description = "Left Pop Bumper" },
			new GamelogicEngineCoil("10") { Description = "Right Pop Bumper" },
			new GamelogicEngineCoil("11") { Description = "Bottom Pop Bumper" },
			new GamelogicEngineCoil("12") { Description = "Upper Right Flipper" },
			new GamelogicEngineCoil("13") { Description = "Left Slingshot" },
			new GamelogicEngineCoil("14") { Description = "Right Slingshot" },
			new GamelogicEngineCoil("17") { Description = "Flash: Asteroid (left)", IsLamp = true },
			new GamelogicEngineCoil("18") { Description = "Flash: Asteroid (right)", IsLamp = true },
			new GamelogicEngineCoil("19") { Description = "Flash: Left Ramp (top)", IsLamp = true },
			new GamelogicEngineCoil("20") { Description = "Flash: Right Ramp (top)", IsLamp = true },
			new GamelogicEngineCoil("21") { Description = "Flash: Kick Back", IsLamp = true },
			new GamelogicEngineCoil("22") { Description = "Laser Motor" },
			new GamelogicEngineCoil("23") { Description = "Flash: Ramp (left)", IsLamp = true },
			new GamelogicEngineCoil("25") { Description = "Flash: Pop Bumpers", IsLamp = true },
			new GamelogicEngineCoil("26") { Description = "Flash: Warp Ramp Entrance", IsLamp = true },
			new GamelogicEngineCoil("27") { Description = "Flash: Center Three Banks", IsLamp = true },
			new GamelogicEngineCoil("28") { Description = "Flash: Ramp (right)", IsLamp = true },
			new GamelogicEngineCoil("29") { Description = "Flash: Left Loop", IsLamp = true },
			new GamelogicEngineCoil("30") { Description = "Flash: Upper Right Flipper", IsLamp = true },
			new GamelogicEngineCoil("31") { Description = "Flash: Vengeance Ship", IsLamp = true },
			new GamelogicEngineCoil("32") { Description = "Flash: Bottom Spot (left)", IsLamp = true },
		};
	}
}
