// SPDX-License-Identifier: MIT
using System;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using VisualPinball.Plugins;

namespace VisualPinball.Engine.PinMAME
{
	internal sealed class PinMamePackageIdentityReader : IPackagedGameIdentityReader
	{
		public string ComponentType => "PinMameGamelogicEngine";
		public GameIdentity? Read(ReadOnlyMemory<byte> payload)
		{
			var values = JObject.Parse(Encoding.UTF8.GetString(payload.Span));
			string rom = values.Value<string>("RomId");
			if (string.IsNullOrWhiteSpace(rom)) rom = values.Value<string>("GameId");
			return string.IsNullOrWhiteSpace(rom) ? null : new GameIdentity("pinmame", rom);
		}
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Register() => PackagedGameIdentityReaders.Register(new PinMamePackageIdentityReader());
	}
}
