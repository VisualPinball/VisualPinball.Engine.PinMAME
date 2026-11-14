// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

using System;
using UnityEngine;
using VisualPinball.Plugins;
using VisualPinball.Unity;

namespace VisualPinball.Engine.PinMAME
{
	internal sealed class PinMamePackageIdentityReader : IPackagedGameIdentityReader
	{
		public string ComponentType => "PinMameGamelogicEngine";
		public GameIdentity? Read(ReadOnlyMemory<byte> payload)
		{
			// read as the packer wrote it, so the key names are the packer's
			var data = PackageApi.Packer.Unpack<PinMameGamelogicEnginePackable>(payload.ToArray());
			var rom = data.RomId;
			if (string.IsNullOrWhiteSpace(rom)) rom = data.GameId;
			return string.IsNullOrWhiteSpace(rom) ? null : new GameIdentity("pinmame", rom);
		}
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Register() => PackagedGameIdentityReaders.Register(new PinMamePackageIdentityReader());
	}
}
