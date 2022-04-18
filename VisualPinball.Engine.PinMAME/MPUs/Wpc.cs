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
	public abstract class Wpc : PinMameGame
	{
		public override PinMameIdAlias[] AvailableAliases => Aliases.Concat(_aliases).ToArray();
		protected abstract PinMameIdAlias[] Aliases { get; }

		public override GamelogicEngineSwitch[] AvailableSwitches => Concat(_switches, Switches);

		protected override GamelogicEngineCoil[] Coils => Concat(_coils, GameCoils);
		protected abstract GamelogicEngineCoil[] GameCoils { get; }

		protected abstract GamelogicEngineSwitch[] Switches { get; }

		private readonly PinMameIdAlias[] _aliases =
		{
			new PinMameIdAlias(1, SwCoin1, AliasType.Switch),
			new PinMameIdAlias(2, SwCoin2, AliasType.Switch),
			new PinMameIdAlias(3, SwCoin3, AliasType.Switch),
			new PinMameIdAlias(4, SwCoin4, AliasType.Switch),
			new PinMameIdAlias(5, SwCancel, AliasType.Switch),
			new PinMameIdAlias(6, SwDown, AliasType.Switch),
			new PinMameIdAlias(7, SwUp, AliasType.Switch),
			new PinMameIdAlias(8, SwEnter, AliasType.Switch),
			new PinMameIdAlias(112, SwFlipperLowerRight, AliasType.Switch),
			new PinMameIdAlias(114, SwFlipperLowerLeft, AliasType.Switch),
			new PinMameIdAlias(116, SwFlipperUpperRight, AliasType.Switch),
			new PinMameIdAlias(118, SwFlipperUpperLeft, AliasType.Switch),

			new PinMameIdAlias(19, CoilGameOn, AliasType.Coil),
		};

		private readonly GamelogicEngineSwitch[] _switches = {
			new GamelogicEngineSwitch(SwCoin1) { Description = "Coin Button 1", InputActionHint = InputConstants.ActionInsertCoin1, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin2) { Description = "Coin Button 2", InputActionHint = InputConstants.ActionInsertCoin2, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin3) { Description = "Coin Button 3", InputActionHint = InputConstants.ActionInsertCoin3, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin4) { Description = "Coin Button 4", InputActionHint = InputConstants.ActionInsertCoin4, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCancel) { Description = "Cancel", InputActionHint = InputConstants.ActionCoinDoorCancel, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwDown) { Description = "Down", InputActionHint = InputConstants.ActionCoinDoorDown, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwUp) { Description = "Up", InputActionHint = InputConstants.ActionCoinDoorUp, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwEnter) { Description = "Enter", InputActionHint = InputConstants.ActionCoinDoorEnter, InputMapHint = InputConstants.MapCabinetSwitches },

			new GamelogicEngineSwitch(SwFlipperLowerRight) { Description = "Lower Right Flipper", InputActionHint = InputConstants.ActionRightFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperLowerLeft) { Description = "Lower Left Flipper", InputActionHint = InputConstants.ActionLeftFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperUpperRight) { Description = "Upper Right Flipper", InputActionHint = InputConstants.ActionUpperRightFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperUpperLeft) { Description = "Upper Left Flipper", InputActionHint = InputConstants.ActionUpperLeftFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
		};

		private readonly GamelogicEngineCoil[] _coils = {
			new GamelogicEngineCoil(CoilGameOn) { Description = "ROM Started" }
		};
	}
}
