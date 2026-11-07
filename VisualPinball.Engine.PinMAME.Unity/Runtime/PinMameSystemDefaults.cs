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
using VisualPinball.Plugins;

namespace VisualPinball.Engine.PinMAME
{
	// What PinMAME takes from Visual PinMAME on this computer while its own settings are empty.
	internal sealed partial class PinMamePluginFactory
	{
		public IReadOnlyList<PluginSystemDefault> FindSystemDefaults(PluginSettingsSnapshot settings, CancellationToken cancellation)
		{
			var found = new List<PluginSystemDefault>();

			// PinMameFolders.ImportFromVpm copies a game's files from here the first time it runs
			var nvram = FirstFolder(Safe(() => PinMame.PinMame.FindVpmPath(PinMameFileType.Nvram)));
			found.Add(new PluginSystemDefault("Saved games", "VPinMAME") {
				Path = nvram ?? "",
				Description = nvram != null
					? "Settings and high scores, copied once per game. VPinMAME's own files stay as they are."
					: "No VPinMAME saved games. Games start with their factory settings.",
			});

			cancellation.ThrowIfCancellationRequested();
			// The folders PinMameAltSound.Find looks in when AltSoundFolder is empty. It looks in all of them for each game, so the
			// first one with packs is shown and the count covers all of them.
			var folders = DefaultAltSoundFolders().Where(Exists).Distinct(StringComparer.OrdinalIgnoreCase)
				.Select(folder => (Folder: folder, Packs: Safe(() => Directory.GetDirectories(folder)) ?? Array.Empty<string>()))
				.ToList();
			var withPacks = folders.Where(folder => folder.Packs.Length > 0).ToList();
			var shown = withPacks.Count > 0 ? withPacks : folders.Take(1).ToList();
			var packs = withPacks.SelectMany(folder => folder.Packs).Select(pack => Path.GetFileName(pack)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
			var on = settings.Boolean("AltSound");
			found.Add(new PluginSystemDefault("AltSound", shown.Count == 0 ? "VPinMAME" : string.Join(" and ", shown.Select(folder => Source(folder.Folder)).Distinct())) {
				Path = shown.Count > 0 ? shown[0].Folder : "",
				FieldId = "AltSoundFolder",
				Description = shown.Count == 0
					? "No AltSound packs found. Only needed for recorded music and callouts."
					: (packs == 1 ? "1 replacement sound pack" : $"{packs} replacement sound packs")
						+ (withPacks.Count > 1 ? ", here and in " + string.Join(" and ", withPacks.Skip(1).Select(folder => folder.Folder)) : "")
						+ (on ? "." : ". AltSound is off: switch it on under Settings › Plugins › PinMAME."),
			});
			return found;
		}

		private static IEnumerable<string> DefaultAltSoundFolders()
		{
			var roms = Safe(() => PinMame.PinMame.FindVpmPath(PinMameFileType.Roms));
			if (!string.IsNullOrEmpty(roms)) {
				foreach (var romFolder in roms.Split(';')) {
					var vpm = Safe(() => Path.GetDirectoryName(romFolder.Trim().TrimEnd('/', '\\')));
					if (!string.IsNullOrEmpty(vpm)) yield return Path.Combine(vpm, "altsound");
				}
			}
			// only the path: reading the defaults never creates folders
			yield return Path.Combine(PinMameFolders.DataFolderPath, "altsound");
		}

		private static string Source(string folder)
			=> folder.StartsWith(PinMameFolders.DataFolderPath, StringComparison.OrdinalIgnoreCase) ? "VPE" : "VPinMAME";

		private static string FirstFolder(string folders)
			=> (folders ?? "").Split(';').Select(folder => folder.Trim()).FirstOrDefault(Exists);

		private static bool Exists(string folder)
		{
			try { return !string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder); }
			catch (Exception error) when (error is ArgumentException || error is IOException || error is UnauthorizedAccessException || error is NotSupportedException) { return false; }
		}

		private static T Safe<T>(Func<T> read)
		{
			try { return read(); }
			catch (Exception error) when (error is ArgumentException || error is IOException || error is UnauthorizedAccessException || error is NotSupportedException || error is DllNotFoundException || error is EntryPointNotFoundException) { return default; }
		}
	}
}
