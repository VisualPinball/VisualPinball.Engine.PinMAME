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
using System.Collections.Generic;
using System.Linq;
using VisualPinball.Engine.Game.Engines;

namespace VisualPinball.Engine.PinMAME
{
	[Serializable]
	public abstract class PinMameGame
	{
		public const string SwCoin1 = "s_coin_1";
		public const string SwCoin2 = "s_coin_2";
		public const string SwCoin3 = "s_coin_3";
		public const string SwCoin4 = "s_coin_4";
		public const string SwCancel = "s_cancel";
		public const string SwDown = "s_down";
		public const string SwUp = "s_up";
		public const string SwEnter = "s_enter";
		public const string SwFlipperLowerRight = "s_flipper_lower_right";
		public const string SwFlipperLowerLeft = "s_flipper_lower_left";
		public const string SwFlipperUpperRight = "s_flipper_upper_right";
		public const string SwFlipperUpperLeft = "s_flipper_upper_left";
		public const string SwSelfTest = "s_self_test";
		public const string SwCpuDiagnose = "s_cpu_diagnose";
		public const string SwSoundDiagnose = "s_sound_diagnose";
		public const string SwTilt = "s_tilt";
		public const string SwBallRollTilt = "s_ball_roll_tilt";
		public const string SwSlamTilt = "s_slam_tilt";
		public const string SwStartButton = "s_start_button";

		public const string CoilGameOn = "c_game_on";
		public const string CoilFlipperLowerRight = "c_flipper_lower_right";
		public const string CoilFlipperLowerLeft = "c_flipper_lower_left";
		public const string CoilFlipperUpperRight = "c_flipper_upper_right";
		public const string CoilFlipperUpperLeft = "c_flipper_upper_left";

		public abstract string Name { get; }
		public abstract string Id { get; }
		public abstract int Year { get; }
		public abstract string Manufacturer { get; }
		public abstract int IpdbId { get; }

		public abstract PinMameRom[] Roms { get; }

		/// <summary>
		/// All available switches for that game
		/// </summary>
		public abstract GamelogicEngineSwitch[] AvailableSwitches { get; }

		/// <summary>
		/// All available coils for that game
		/// </summary>
		public GamelogicEngineCoil[] AvailableCoils => Concat(_coils, Coils);

		/// <summary>
		/// Coils specific to the MPU
		/// </summary>
		protected abstract GamelogicEngineCoil[] Coils { get; }

		public abstract GamelogicEngineLamp[] AvailableLamps { get; }

		/// <summary>
		/// These coils are common to all games.
		/// </summary>
		private readonly GamelogicEngineCoil[] _coils = {
			new GamelogicEngineCoil(CoilFlipperLowerRight, 46) { Description = "Lower Right Flipper", DeviceHint = "^(RightFlipper|RFlipper|FlipperRight|FlipperR)$"},
			new GamelogicEngineCoil(CoilFlipperLowerLeft, 48) { Description = "Lower Left Flipper", DeviceHint = "^(LeftFlipper|LFlipper|FlipperLeft|FlipperL)$"},
			new GamelogicEngineCoil(CoilFlipperUpperRight, 34) { Description = "Upper Right Flipper"},
			new GamelogicEngineCoil(CoilFlipperUpperLeft, 36) { Description = "Upper Left Flipper"},
		};

		protected GamelogicEngineCoil[] Concat(IEnumerable<GamelogicEngineCoil> parent, IEnumerable<GamelogicEngineCoil> children)
		{
			var c = parent.ToDictionary(s => s.InternalId, s => s);
			foreach (var child in children) {
				c[child.InternalId] = child;
			}
			return c.Values.ToArray();
		}

		protected GamelogicEngineSwitch[] Concat(IEnumerable<GamelogicEngineSwitch> parent, IEnumerable<GamelogicEngineSwitch> children)
		{
			var c = parent.ToDictionary(s => s.InternalId, s => s);
			foreach (var child in children) {
				c[child.InternalId] = child;
			}
			return c.Values.ToArray();
		}
	}
}
