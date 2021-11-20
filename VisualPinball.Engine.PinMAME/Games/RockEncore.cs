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

// ReSharper disable StringLiteralTypo

using System;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.PinMAME.MPUs;
using VisualPinball.Engine.Common;

namespace VisualPinball.Engine.PinMAME.Games
{
	[Serializable]
	public class RockEncore : Rock
	{
		public override string Name => "Rock Encore";
		public override string Id => "rock_enc";
		public override int Year => 1986;
		public override int IpdbId => 1979;

		public override PinMameRom[] Roms { get; } = {
			new PinMameRom("rock_enc"),
			new PinMameRom("rock_efp", description: "Free Play"),
			new PinMameRom("rock_eg", language: PinMameRomLanguage.German),
			new PinMameRom("rockegfp", description: "Free Play", language: PinMameRomLanguage.German),
			new PinMameRom("clash", version: "Clash, The (Rock Encore unofficial MOD)"),
		};
	}
}
