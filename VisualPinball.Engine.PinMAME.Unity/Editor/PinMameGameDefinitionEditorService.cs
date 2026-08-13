// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;

namespace VisualPinball.Engine.PinMAME.Editor
{
	/// <summary>
	/// Editor-only entry point used by table importers that need to populate mappings without opening the inspector.
	/// The raw fetched definition remains transient. Editor builds retain normalized manager candidates for authoring,
	/// while player builds receive only the selected identifiers and populated table mappings.
	/// </summary>
	public static class PinMameGameDefinitionEditorService
	{
		public static PinMameGameDefinition PopulateHardware(PinMameGamelogicEngine gle,
			TableComponent tableComponent, string machineId, string romId, CancellationToken cancellationToken = default)
		{
			ValidateArguments(gle, tableComponent, machineId);
			var loaded = LoadDefinitionAsync(machineId, romId, cancellationToken, false).ConfigureAwait(false).GetAwaiter().GetResult();
			cancellationToken.ThrowIfCancellationRequested();
			if (!gle || !tableComponent) throw new OperationCanceledException("The target table was destroyed while its PinMAME definition was loading.");
			return Apply(gle, tableComponent, machineId, romId, loaded);
		}

		public static PinMameGameDefinition PopulateHardwareFromCache(PinMameGamelogicEngine gle,
			TableComponent tableComponent, string machineId, string romId, CancellationToken cancellationToken = default)
		{
			ValidateArguments(gle, tableComponent, machineId);
			var loaded = LoadDefinitionAsync(machineId, romId, cancellationToken, true).ConfigureAwait(false).GetAwaiter().GetResult();
			cancellationToken.ThrowIfCancellationRequested();
			if (!gle || !tableComponent) throw new OperationCanceledException("The target table was destroyed while its PinMAME definition was loading.");
			return Apply(gle, tableComponent, machineId, romId, loaded);
		}

		public static async Task<PinMameGameDefinition> PopulateHardwareAsync(PinMameGamelogicEngine gle,
			TableComponent tableComponent, string machineId, string romId, CancellationToken cancellationToken = default)
		{
			ValidateArguments(gle, tableComponent, machineId);
			var loaded = await LoadDefinitionAsync(machineId, romId, cancellationToken, false);
			cancellationToken.ThrowIfCancellationRequested();
			if (!gle || !tableComponent) throw new OperationCanceledException("The target table was destroyed while its PinMAME definition was loading.");
			return Apply(gle, tableComponent, machineId, romId, loaded);
		}

		private static async Task<LoadedDefinition> LoadDefinitionAsync(string machineId, string romId,
			CancellationToken cancellationToken, bool cacheOnly)
		{
			var catalogResult = await PinMameGameDefinitionCatalogClient.GetCatalogAsync(false, cancellationToken, cacheOnly).ConfigureAwait(false);
			var machine = catalogResult.Value.Machines.FirstOrDefault(entry => string.Equals(entry.Id, machineId, StringComparison.Ordinal));
			if (machine == null) throw new InvalidDataException($"Machine {machineId} is not present in the published catalog.");
			if (!machine.IsSelectable) throw new InvalidDataException($"Machine {machineId} has no selectable physical definition.");

			var definitionResult = await PinMameGameDefinitionCatalogClient.GetDefinitionAsync(machine, false, cancellationToken, cacheOnly).ConfigureAwait(false);
			var definition = PinMameGameDefinitionConverter.Convert(definitionResult.Value, romId);
			if (!string.Equals(definition.MachineId, machineId, StringComparison.Ordinal)) {
				throw new InvalidDataException($"The catalog entry {machineId} returned definition {definition.MachineId}.");
			}
			if (!string.IsNullOrWhiteSpace(romId) && !definition.Roms.Any(rom => string.Equals(rom.Id, romId, StringComparison.OrdinalIgnoreCase))) {
				throw new InvalidDataException($"ROM {romId} is not assigned to machine {machineId}.");
			}
			var warning = string.Join("\n", new[] { catalogResult.Warning, definitionResult.Warning }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
			return new LoadedDefinition(definition, warning);
		}

		private static PinMameGameDefinition Apply(PinMameGamelogicEngine gle, TableComponent tableComponent,
			string machineId, string romId, LoadedDefinition loaded)
		{
			var definition = loaded.Definition;
			gle.SetMachineSelection(machineId, !string.IsNullOrWhiteSpace(romId) ? romId : definition.Roms.FirstOrDefault()?.Id);
			gle.SetAvailableHardware(definition.Switches, definition.Coils, definition.Lamps, definition.Wires);
			tableComponent.RepopulateHardware(gle);
			EditorUtility.SetDirty(gle);
			EditorUtility.SetDirty(tableComponent);

			if (!string.IsNullOrWhiteSpace(loaded.Warning)) Debug.LogWarning($"[PinMAME] {loaded.Warning}", gle);
			foreach (var diagnostic in definition.Diagnostics) Debug.LogWarning($"[PinMAME] {diagnostic}", gle);
			return definition;
		}

		private static void ValidateArguments(PinMameGamelogicEngine gle, TableComponent tableComponent, string machineId)
		{
			if (!gle) throw new ArgumentNullException(nameof(gle));
			if (!tableComponent) throw new ArgumentNullException(nameof(tableComponent));
			if (string.IsNullOrWhiteSpace(machineId)) throw new ArgumentException("A machine ID is required.", nameof(machineId));
			PinMameGameDefinitionCatalogClient.EnsureCacheRoot();
		}

		private readonly struct LoadedDefinition
		{
			internal PinMameGameDefinition Definition { get; }
			internal string Warning { get; }

			internal LoadedDefinition(PinMameGameDefinition definition, string warning)
			{
				Definition = definition;
				Warning = warning;
			}
		}
	}
}
