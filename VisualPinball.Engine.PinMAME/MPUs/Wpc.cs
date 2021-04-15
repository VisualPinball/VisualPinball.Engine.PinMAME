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

using System.Linq;
using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game.Engines;

namespace VisualPinball.Engine.PinMAME.MPUs
{
	public abstract class Wpc : PinMameGame
	{
		public override GamelogicEngineSwitch[] AvailableSwitches => Concat(_switches, Switches);

		protected override GamelogicEngineCoil[] Coils => Concat(_coils, GameCoils);
		protected abstract GamelogicEngineCoil[] GameCoils { get; }


		protected abstract GamelogicEngineSwitch[] Switches { get; }

		private readonly GamelogicEngineSwitch[] _switches = {
			new GamelogicEngineSwitch(SwCoin1, 1) {Description = "Coin Button 1", InputActionHint = InputConstants.ActionInsertCoin1, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin2, 2) {Description = "Coin Button 2", InputActionHint = InputConstants.ActionInsertCoin2, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin3, 3) {Description = "Coin Button 3", InputActionHint = InputConstants.ActionInsertCoin3, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin4, 4) {Description = "Coin Button 4", InputActionHint = InputConstants.ActionInsertCoin4, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCancel, 5) {Description = "Cancel", InputActionHint = InputConstants.ActionCoinDoorCancel, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwDown, 6) {Description = "Down", InputActionHint = InputConstants.ActionCoinDoorDown, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwUp, 7) {Description = "Up", InputActionHint = InputConstants.ActionCoinDoorUp, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwEnter, 8) {Description = "Enter", InputActionHint = InputConstants.ActionCoinDoorEnter, InputMapHint = InputConstants.MapCabinetSwitches },

			new GamelogicEngineSwitch(SwFlipperLowerRight, 112) {Description = "Lower Right Flipper", InputActionHint = InputConstants.ActionRightFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperLowerLeft, 114) {Description = "Lower Left Flipper", InputActionHint = InputConstants.ActionLeftFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperUpperRight, 116) {Description = "Upper Right Flipper", InputActionHint = InputConstants.ActionUpperRightFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperUpperLeft, 118) {Description = "Upper Left Flipper", InputActionHint = InputConstants.ActionUpperLeftFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
		};

		private readonly GamelogicEngineCoil[] _coils = {
			new GamelogicEngineCoil(CoilGameOn, 19) { Description = "ROM Started" }
		};
	}
}