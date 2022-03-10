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

namespace VisualPinball.Engine.PinMAME.MPUs
{
	public abstract class Sam : PinMameGame
	{
		public override GamelogicEngineSwitch[] AvailableSwitches => Concat(_switches, Switches);

		protected override GamelogicEngineCoil[] Coils => Concat(_coils, GameCoils);
		protected abstract GamelogicEngineCoil[] GameCoils { get; }

		protected abstract GamelogicEngineSwitch[] Switches { get; }

		private readonly GamelogicEngineSwitch[] _switches = {
			new GamelogicEngineSwitch(SwCoin1, 65) { Description = "Coin Button 1", InputActionHint = InputConstants.ActionInsertCoin1, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin2, 66) { Description = "Coin Button 2", InputActionHint = InputConstants.ActionInsertCoin2, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin3, 67) { Description = "Coin Button 3", InputActionHint = InputConstants.ActionInsertCoin3, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCancel, -3) { Description = "Cancel", InputActionHint = InputConstants.ActionCoinDoorCancel, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwDown, -2) { Description = "Down", InputActionHint = InputConstants.ActionCoinDoorDown, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwUp, -1) { Description = "Up", InputActionHint = InputConstants.ActionCoinDoorUp, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwEnter, 0) { Description = "Enter", InputActionHint = InputConstants.ActionCoinDoorEnter, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwStartButton, 16) { Description = "Start", InputActionHint = InputConstants.ActionStartGame, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwSlamTilt, -6) { Description = "Slam Tilt", InputActionHint = InputConstants.ActionSlamTilt, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwTilt, -7) { Description = "Tilt" },
			new GamelogicEngineSwitch(SwFlipperLowerRight, 82) { Description = "Lower Right Flipper", InputActionHint = InputConstants.ActionRightFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperLowerLeft, 84) { Description = "Lower Left Flipper", InputActionHint = InputConstants.ActionLeftFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
		};

		private readonly GamelogicEngineCoil[] _coils = {
		};
	}
}
