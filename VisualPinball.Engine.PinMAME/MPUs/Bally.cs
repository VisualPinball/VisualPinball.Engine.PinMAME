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

using System.Linq;
using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game.Engines;

namespace VisualPinball.Engine.PinMAME.MPUs
{
	public abstract class Bally : PinMameGame
	{
		public override PinMameIdAlias[] AvailableAliases => Aliases.Concat(_aliases).ToArray();
		protected abstract PinMameIdAlias[] Aliases { get; }

		protected override GamelogicEngineCoil[] Coils => GameCoils.Concat(_coils).ToArray();
		protected abstract GamelogicEngineCoil[] GameCoils { get; }

		public override GamelogicEngineSwitch[] AvailableSwitches => Switches.Concat(_switches).ToArray();
		protected abstract GamelogicEngineSwitch[] Switches { get; }

		private readonly PinMameIdAlias[] _aliases =
		{
			new PinMameIdAlias(-7, SwSelfTest, AliasType.Switch),
			new PinMameIdAlias(-6, SwCpuDiagnose, AliasType.Switch),
			new PinMameIdAlias(-5, SwSoundDiagnose, AliasType.Switch),
			new PinMameIdAlias(2, SwBallRollTilt, AliasType.Switch),
			new PinMameIdAlias(6, SwStartButton, AliasType.Switch),
			new PinMameIdAlias(7, SwTilt, AliasType.Switch),
			new PinMameIdAlias(16, SwSlamTilt, AliasType.Switch),
			new PinMameIdAlias(11, SwCoin1, AliasType.Switch),
			new PinMameIdAlias(10, SwCoin2, AliasType.Switch),
			new PinMameIdAlias(9, SwCoin3, AliasType.Switch),
			new PinMameIdAlias(82, SwFlipperLowerRight, AliasType.Switch),
			new PinMameIdAlias(84, SwFlipperLowerLeft, AliasType.Switch),

			new PinMameIdAlias(19, CoilGameOn, AliasType.Coil),
		};

		private readonly GamelogicEngineSwitch[] _switches = {
			new GamelogicEngineSwitch(SwSelfTest) { Description = "Self Test" },
			new GamelogicEngineSwitch(SwCpuDiagnose) { Description = "CPU Diagnose"},
			new GamelogicEngineSwitch(SwSoundDiagnose) { Description = "Sound Diagnose"},
			new GamelogicEngineSwitch(SwBallRollTilt) { Description = "Ball Roll Tilt"},
			new GamelogicEngineSwitch(SwStartButton) { Description = "Start Button", InputActionHint = InputConstants.ActionStartGame, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwTilt) { Description = "Tilt" },
			new GamelogicEngineSwitch(SwSlamTilt) { Description = "Slam Tilt", InputActionHint = InputConstants.ActionSlamTilt, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin1) { Description = "Coin Button 1", InputActionHint = InputConstants.ActionInsertCoin1, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin2) { Description = "Coin Button 2", InputActionHint = InputConstants.ActionInsertCoin2, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin3) { Description = "Coin Button 3", InputActionHint = InputConstants.ActionInsertCoin3, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperLowerRight) { Description = "Lower Right Flipper", InputActionHint = InputConstants.ActionRightFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperLowerLeft) { Description = "Lower Left Flipper", InputActionHint = InputConstants.ActionLeftFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
		};

		private readonly GamelogicEngineCoil[] _coils = {
			new GamelogicEngineCoil(CoilGameOn) { Description = "ROM Started" }
		};
	}
}
