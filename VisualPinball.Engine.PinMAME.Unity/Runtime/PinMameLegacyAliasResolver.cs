// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using VisualPinball.Engine.Game.Engines;

namespace VisualPinball.Engine.PinMAME
{
	/// <summary>
	/// Resolves symbolic mapping IDs serialized by the retired class-based definitions.
	/// This is a compatibility shim for existing scenes and packages, not a machine-definition source.
	/// </summary>
	internal static class PinMameLegacyAliasResolver
	{
		private static readonly IReadOnlyDictionary<string, int> WpcSwitches = new Dictionary<string, int>(StringComparer.Ordinal) {
			["s_coin_1"] = 1, ["s_coin_2"] = 2, ["s_coin_3"] = 3, ["s_coin_4"] = 4,
			["s_cancel"] = 5, ["s_down"] = 6, ["s_up"] = 7, ["s_enter"] = 8,
			["s_flipper_lower_right"] = 112, ["s_flipper_lower_left"] = 114,
			["s_flipper_upper_right"] = 116, ["s_flipper_upper_left"] = 118,
		};

		private static readonly IReadOnlyDictionary<string, int> BallySwitches = new Dictionary<string, int>(StringComparer.Ordinal) {
			["s_self_test"] = -7, ["s_cpu_diagnose"] = -6, ["s_sound_diagnose"] = -5,
			["s_ball_roll_tilt"] = 2, ["s_start_button"] = 6, ["s_tilt"] = 7, ["s_slam_tilt"] = 16,
			["s_coin_1"] = 11, ["s_coin_2"] = 10, ["s_coin_3"] = 9,
			["s_flipper_lower_right"] = 82, ["s_flipper_lower_left"] = 84,
		};

		private static readonly IReadOnlyDictionary<string, int> System80Switches = new Dictionary<string, int>(StringComparer.Ordinal) {
			["s_self_test"] = 7, ["s_start_button"] = 47, ["s_tilt"] = 57, ["s_slam_tilt"] = -1,
			["s_coin_1"] = 17, ["s_coin_2"] = 27, ["s_coin_3"] = 37,
			["s_left_advance"] = 6, ["s_right_advance"] = 16,
			["s_flipper_lower_left"] = 114, ["s_flipper_lower_right"] = 112,
		};

		private static readonly IReadOnlyDictionary<string, int> SamSwitches = new Dictionary<string, int>(StringComparer.Ordinal) {
			["s_coin_1"] = 65, ["s_coin_2"] = 66, ["s_coin_3"] = 67,
			["s_cancel"] = -3, ["s_down"] = -2, ["s_up"] = -1, ["s_enter"] = 0,
			["s_start_button"] = 16, ["s_slam_tilt"] = -6, ["s_tilt"] = -7,
			["s_flipper_lower_left"] = 84, ["s_flipper_lower_right"] = 82,
		};

		internal static bool TryResolveSwitch(string machineId, string romId, string legacyGameId, string alias, out int address)
		{
			var platform = PlatformFor(machineId, romId, legacyGameId);
			return SwitchesFor(platform).TryGetValue(alias ?? string.Empty, out address);
		}

		internal static bool TryResolveCoil(string machineId, string romId, string legacyGameId, string alias, out int address)
		{
			address = 0;
			var platform = PlatformFor(machineId, romId, legacyGameId);
			if (platform == LegacyPlatform.Wpc && IsTerminator2(machineId, romId, legacyGameId)) {
				if (TryResolveFlipperCoil(alias, 48, 46, 36, 34, out address)) return true;
			}
			if (platform == LegacyPlatform.System80 && IsRock(machineId, romId, legacyGameId)) {
				if (TryResolveFlipperCoil(alias, 48, 46, 47, 45, out address)) return true;
			}
			if (platform == LegacyPlatform.Sam) {
				if (string.Equals(alias, "c_flipper_lower_left", StringComparison.Ordinal)) { address = 15; return true; }
				if (string.Equals(alias, "c_flipper_lower_right", StringComparison.Ordinal)) { address = 16; return true; }
			}
			if (string.Equals(alias, "c_game_on", StringComparison.Ordinal)) {
				if (platform is LegacyPlatform.Wpc or LegacyPlatform.Bally) { address = 19; return true; }
				if (platform == LegacyPlatform.System80) { address = 10; return true; }
			}
			return false;
		}

		internal static bool IsFlipperCoilAlias(string alias)
		{
			return string.Equals(alias, "c_flipper_lower_left", StringComparison.Ordinal)
			       || string.Equals(alias, "c_flipper_lower_right", StringComparison.Ordinal)
			       || string.Equals(alias, "c_flipper_upper_left", StringComparison.Ordinal)
			       || string.Equals(alias, "c_flipper_upper_right", StringComparison.Ordinal);
		}

		internal static GamelogicEngineWire[] WiresFor(string machineId, string romId, string legacyGameId)
		{
			if (PlatformFor(machineId, romId, legacyGameId) == LegacyPlatform.Unknown) return Array.Empty<GamelogicEngineWire>();
			return new[] {
				new GamelogicEngineWire("s_flipper_lower_left", "c_flipper_lower_left", DestinationType.Coil, "Lower Left Flipper"),
				new GamelogicEngineWire("s_flipper_lower_right", "c_flipper_lower_right", DestinationType.Coil, "Lower Right Flipper"),
				new GamelogicEngineWire("s_flipper_upper_left", "c_flipper_upper_left", DestinationType.Coil, "Upper Left Flipper"),
				new GamelogicEngineWire("s_flipper_upper_right", "c_flipper_upper_right", DestinationType.Coil, "Upper Right Flipper"),
			};
		}

		private static bool TryResolveFlipperCoil(string alias, int lowerLeft, int lowerRight, int upperLeft, int upperRight, out int address)
		{
			address = 0;
			if (string.Equals(alias, "c_flipper_lower_left", StringComparison.Ordinal)) address = lowerLeft;
			else if (string.Equals(alias, "c_flipper_lower_right", StringComparison.Ordinal)) address = lowerRight;
			else if (string.Equals(alias, "c_flipper_upper_left", StringComparison.Ordinal)) address = upperLeft;
			else if (string.Equals(alias, "c_flipper_upper_right", StringComparison.Ordinal)) address = upperRight;
			return address != 0;
		}

		private static IReadOnlyDictionary<string, int> SwitchesFor(LegacyPlatform platform)
		{
			return platform switch {
				LegacyPlatform.Wpc => WpcSwitches,
				LegacyPlatform.Bally => BallySwitches,
				LegacyPlatform.System80 => System80Switches,
				LegacyPlatform.Sam => SamSwitches,
				_ => EmptySwitches,
			};
		}

		private static LegacyPlatform PlatformFor(string machineId, string romId, string legacyGameId)
		{
			if (IsAny(machineId, legacyGameId, "bally.attack-from-mars.1995", "bally.creature-from-the-black-lagoon.1992", "williams.medieval-madness.1997", "williams.terminator-2-judgment-day.1991", "afm", "cftbl", "mm", "t2")
			    || StartsWithAny(romId, "afm_", "cftbl_", "mm_", "t2_")) return LegacyPlatform.Wpc;
			if (IsAny(machineId, legacyGameId, "bally.centaur.1981", "bally.flash-gordon.1981", "centaur", "fg")
			    || StartsWithAny(romId, "centaur", "flashg")) return LegacyPlatform.Bally;
			if (IsAny(machineId, legacyGameId, "premier.rock.1985", "premier.rock-encore.1986", "rock", "rock_enc")
			    || StartsWithAny(romId, "rock", "clash")) return LegacyPlatform.System80;
			if (IsAny(machineId, legacyGameId, "stern.star-trek.2013", "stern.tron-legacy.2011", "stern.the-walking-dead.2014", "star-trek-stern", "trn", "twd")
			    || StartsWithAny(romId, "st_", "trn_", "twd_")) return LegacyPlatform.Sam;
			return LegacyPlatform.Unknown;
		}

		private static bool IsTerminator2(string machineId, string romId, string legacyGameId)
		{
			return IsAny(machineId, legacyGameId, "williams.terminator-2-judgment-day.1991", "t2") || StartsWithAny(romId, "t2_");
		}

		private static bool IsRock(string machineId, string romId, string legacyGameId)
		{
			return IsAny(machineId, legacyGameId, "premier.rock.1985", "premier.rock-encore.1986", "rock", "rock_enc") || StartsWithAny(romId, "rock", "clash");
		}

		private static bool IsAny(string machineId, string legacyGameId, params string[] identities)
		{
			foreach (var identity in identities) {
				if (string.Equals(machineId, identity, StringComparison.OrdinalIgnoreCase) || string.Equals(legacyGameId, identity, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		private static bool StartsWithAny(string value, params string[] prefixes)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			foreach (var prefix in prefixes) {
				if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		private static readonly IReadOnlyDictionary<string, int> EmptySwitches = new Dictionary<string, int>();

		private enum LegacyPlatform
		{
			Unknown,
			Wpc,
			Bally,
			System80,
			Sam,
		}
	}
}
