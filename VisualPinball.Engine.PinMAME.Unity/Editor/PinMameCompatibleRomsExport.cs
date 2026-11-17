// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

using System;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace VisualPinball.Engine.PinMAME.Editor
{
	/// <summary>
	/// Looks up the ROMs compatible with a table's ROM in the machine catalog when the table is
	/// exported, so the player can offer them when the table's ROM isn't there.
	/// </summary>
	[InitializeOnLoad]
	internal static class PinMameCompatibleRomsExport
	{
		// the export waits for it; a cached catalog answers at once
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

		static PinMameCompatibleRomsExport()
		{
			PinMameGamelogicEngine.ResolveCompatibleRoms = Resolve;
		}

		private static PinMameCompatibleRom[] Resolve(PinMameGamelogicEngine engine)
		{
			if (!engine || string.IsNullOrWhiteSpace(engine.machineId) || string.IsNullOrWhiteSpace(engine.romId)) {
				return null;
			}
			try {
				using var cancellation = new CancellationTokenSource(Timeout);
				PinMameGameDefinitionCatalogClient.EnsureCacheRoot();
				var catalog = PinMameGameDefinitionCatalogClient.GetCatalogAsync(false, cancellation.Token)
					.ConfigureAwait(false).GetAwaiter().GetResult().Value;
				var machine = catalog.Machines.FirstOrDefault(entry => string.Equals(entry.Id, engine.machineId, StringComparison.Ordinal));
				if (machine == null || string.IsNullOrWhiteSpace(machine.Detail)) {
					return null;
				}
				var definition = PinMameGameDefinitionCatalogClient.GetDefinitionAsync(machine, false, cancellation.Token)
					.ConfigureAwait(false).GetAwaiter().GetResult();
				var roms = PinMameGameDefinitionConverter.ReadCompatibleRoms(definition.Value, engine.romId);
				Debug.Log($"[PinMAME] Exporting {roms.Length} ROM(s) compatible with {engine.romId}: {string.Join(", ", roms.Select(rom => rom.Id))}.");
				return roms;
			} catch (Exception exception) {
				Debug.LogWarning($"[PinMAME] Could not look up the ROMs compatible with {engine.romId} in the machine catalog; exporting the {engine.CompatibleRoms.Count} saved with the table. {exception.Message}");
				return null;
			}
		}
	}
}
