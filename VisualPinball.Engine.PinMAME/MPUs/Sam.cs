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
	public abstract class Sam : PinMameGame
	{
		public override PinMameIdAlias[] AvailableAliases => Aliases.Concat(_aliases).ToArray();
		protected abstract PinMameIdAlias[] Aliases { get; }

		public override GamelogicEngineSwitch[] AvailableSwitches => Concat(_switches, Switches);

		protected override GamelogicEngineCoil[] Coils => Concat(_coils, GameCoils);
		protected abstract GamelogicEngineCoil[] GameCoils { get; }

		protected abstract GamelogicEngineSwitch[] Switches { get; }

		private readonly PinMameIdAlias[] _aliases =
		{
			new PinMameIdAlias(65, SwCoin1, AliasType.Switch),
			new PinMameIdAlias(66, SwCoin2, AliasType.Switch),
			new PinMameIdAlias(67, SwCoin3, AliasType.Switch),
			new PinMameIdAlias(-3, SwCancel, AliasType.Switch),
			new PinMameIdAlias(-2, SwDown, AliasType.Switch),
			new PinMameIdAlias(-1, SwUp, AliasType.Switch),
			new PinMameIdAlias(0, SwEnter, AliasType.Switch),
			new PinMameIdAlias(16, SwStartButton, AliasType.Switch),
			new PinMameIdAlias(-6, SwSlamTilt, AliasType.Switch),
			new PinMameIdAlias(-7, SwTilt, AliasType.Switch),
			new PinMameIdAlias(84, SwFlipperLowerLeft, AliasType.Switch),
			new PinMameIdAlias(82, SwFlipperLowerRight, AliasType.Switch),

			new PinMameIdAlias(15, CoilFlipperLowerLeft, AliasType.Coil),
			new PinMameIdAlias(16, CoilFlipperLowerRight, AliasType.Coil),
		};

		private readonly GamelogicEngineSwitch[] _switches = {
			new GamelogicEngineSwitch(SwCoin1) { Description = "Coin Button 1", InputActionHint = InputConstants.ActionInsertCoin1, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin2) { Description = "Coin Button 2", InputActionHint = InputConstants.ActionInsertCoin2, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCoin3) { Description = "Coin Button 3", InputActionHint = InputConstants.ActionInsertCoin3, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwCancel) { Description = "Coin Door Back", InputActionHint = InputConstants.ActionCoinDoorBack, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwDown) { Description = "Coin Door -", InputActionHint = InputConstants.ActionCoinDoorMinus, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwUp) { Description = "Coin Door +", InputActionHint = InputConstants.ActionCoinDoorPlus, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwEnter) { Description = "Coin Door Select", InputActionHint = InputConstants.ActionCoinDoorSelect, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch("15") { Description = "Tourn Start" },
			new GamelogicEngineSwitch(SwStartButton) { Description = "Start", InputActionHint = InputConstants.ActionStartGame, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwSlamTilt) { Description = "Slam Tilt", InputActionHint = InputConstants.ActionSlamTilt, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwTilt) { Description = "Tilt" },
			new GamelogicEngineSwitch(SwFlipperLowerLeft) { Description = "Lower Left Flipper", InputActionHint = InputConstants.ActionLeftFlipper, InputMapHint = InputConstants.MapCabinetSwitches },
			new GamelogicEngineSwitch(SwFlipperLowerRight) { Description = "Lower Right Flipper", InputActionHint = InputConstants.ActionRightFlipper, InputMapHint = InputConstants.MapCabinetSwitches },

			/*

			From manuals:

			new GamelogicEngineSwitch("D1") { Description = "Left Coin Slot" },
			new GamelogicEngineSwitch("D2") { Description = "Center Coin Slot" },
			new GamelogicEngineSwitch("D3") { Description = "Right Coin Slot" },
			new GamelogicEngineSwitch("D4") { Description = "Forth Coin Slot" },
			new GamelogicEngineSwitch("D5") { Description = "Fifth Coin Slot" },
			new GamelogicEngineSwitch("D6") { Description = "Star Rollover (Bottom)" },
			new GamelogicEngineSwitch("D7") { Description = "Fire Bottom" },
			new GamelogicEngineSwitch("D8") { Description = "Star Rollover (Top)" },
			new GamelogicEngineSwitch("D9") { Description = "Left Flipper Button" },
			new GamelogicEngineSwitch("D10") { Description = "Left Flipper EOS" },
			new GamelogicEngineSwitch("D17") { Description = "Tilt Pendulum" },
			new GamelogicEngineSwitch("D18") { Description = "Slam Tilt" },
			new GamelogicEngineSwitch("D19") { Description = "Ticket Notch" },
			new GamelogicEngineSwitch("D21") { Description = "Back (Green)" },
			new GamelogicEngineSwitch("D22") { Description = "Minus (Red)" },
			new GamelogicEngineSwitch("D23") { Description = "Plus (Red)" },
			new GamelogicEngineSwitch("D24") { Description = "Select (Black)" },
			new GamelogicEngineSwitch("D25") { Description = "Dip (1)2345678" },
			new GamelogicEngineSwitch("D26") { Description = "Dip 1(2)345678" },
			new GamelogicEngineSwitch("D27") { Description = "Dip 12(3)45678" },
			new GamelogicEngineSwitch("D28") { Description = "Dip 123(4)5678" },
			new GamelogicEngineSwitch("D29") { Description = "Dip 1234(5)678" },
			new GamelogicEngineSwitch("D30") { Description = "Dip 12345(6)78" },
			new GamelogicEngineSwitch("D31") { Description = "Dip 123456(7)8" },
			new GamelogicEngineSwitch("D32") { Description = "Dip 1234567(8)" },

			*/

		};

		private readonly GamelogicEngineCoil[] _coils = {
			new GamelogicEngineCoil(CoilFlipperLowerLeft) { Description = "Lower Left Flipper", DeviceHint = "^(LeftFlipper|LFlipper|FlipperLeft|FlipperL)$"},
			new GamelogicEngineCoil(CoilFlipperLowerRight) { Description = "Lower Right Flipper", DeviceHint = "^(RightFlipper|RFlipper|FlipperRight|FlipperR)$"},
		};
	}
}
