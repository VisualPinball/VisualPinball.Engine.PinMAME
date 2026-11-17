// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PinMame;
using VisualPinball.Unity;

namespace VisualPinball.Engine.PinMAME
{
	/// <summary>
	/// Finds the ROM a table package needs and the compatible ones it lists, and whether they are
	/// in the ROM folder, without loading the table or PinMAME.
	/// </summary>
	internal static class PinMameRomAvailability
	{
		private const string ComponentType = "PinMameGamelogicEngine";

		private sealed class Cached
		{
			internal long Size;
			internal DateTime Modified;
			internal PackagedRoms Roms;
		}

		private static readonly Dictionary<string, Cached> Cache = new(StringComparer.Ordinal);

		internal sealed class PackagedRoms
		{
			public string Rom { get; }
			public IReadOnlyList<PinMameCompatibleRom> Compatible { get; }

			public PackagedRoms(string rom, IEnumerable<PinMameCompatibleRom> compatible)
			{
				Rom = rom;
				// best match first, keeping the catalog's order within each kind
				Compatible = (compatible ?? Enumerable.Empty<PinMameCompatibleRom>())
					.Where(candidate => IsSafeRomName(candidate.Id) && !string.Equals(candidate.Id, rom, StringComparison.OrdinalIgnoreCase))
					.GroupBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
					.Select(group => group.First())
					.OrderBy(candidate => candidate.Compatibility)
					.ToArray();
			}

			public bool Lists(string rom) => Compatible.Any(candidate => string.Equals(candidate.Id, rom, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// The ';'-separated folders PinMAME looks for ROMs in, as <see cref="PinMameFolders.ApplyRomFolder"/>
		/// sets them: the player's or editor's ROM folder, else VPM's, else "roms" in VPE's data folder.
		/// </summary>
		internal static string RomFolders => GamelogicPaths.RomFolder
			?? PinMame.PinMame.FindVpmPath(PinMameFileType.Roms)
			?? Path.Combine(PinMameFolders.DataFolderPath, "roms");

		/// <summary>
		/// Whether the ROM's zip is in the ROM folder. Clones also need their parent's zip, which
		/// isn't checked: PinMAME reports it when the game doesn't start.
		/// </summary>
		internal static bool IsAvailable(string rom, string romFolders) => IsSafeRomName(rom) && PinMameFolders.HasRomZip(romFolders, rom);

		/// <summary>
		/// Reads the ROM of the package's PinMAME engine and its compatible ROMs. Null when the
		/// package has no PinMAME engine with a ROM, or more than one. Runs on any thread.
		/// </summary>
		internal static PackagedRoms Read(string packagePath, CancellationToken cancellation)
		{
			if (string.IsNullOrWhiteSpace(packagePath)) return null;
			var path = Path.GetFullPath(packagePath);
			var file = new FileInfo(path);
			if (!file.Exists) return null;
			lock (Cache) {
				if (Cache.TryGetValue(path, out var cached) && cached.Size == file.Length && cached.Modified == file.LastWriteTimeUtc) {
					return cached.Roms;
				}
			}

			PackagedRoms found = null;
			var ambiguous = false;
			using (var storage = PackageApi.StorageManager.OpenStorage(path)) {
				var table = storage.GetFolder(PackageApi.TableFolder);
				foreach (var node in PackagedItems.Read(table)) {
					cancellation.ThrowIfCancellationRequested();
					foreach (var component in node.Components) {
						if (!string.Equals(component.Type, ComponentType, StringComparison.Ordinal) || component.Data == null) continue;
						// read as the packer wrote it, so the key names are the packer's
						var data = PackageApi.Packer.Unpack<PinMameGamelogicEnginePackable>(component.Data);
						var rom = string.IsNullOrWhiteSpace(data.RomId) ? data.GameId : data.RomId;
						if (string.IsNullOrWhiteSpace(rom)) continue;
						if (found != null && !string.Equals(found.Rom, rom, StringComparison.OrdinalIgnoreCase)) ambiguous = true;
						found ??= new PackagedRoms(rom.Trim(), data.CompatibleRoms);
					}
				}
			}
			var result = ambiguous ? null : found;
			lock (Cache) Cache[path] = new Cached { Size = file.Length, Modified = file.LastWriteTimeUtc, Roms = result };
			return result;
		}

		// ROM names go into file paths; PinMAME's are lower case letters, digits and underscores
		private static bool IsSafeRomName(string rom) => !string.IsNullOrWhiteSpace(rom)
			&& rom.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-');
	}
}
