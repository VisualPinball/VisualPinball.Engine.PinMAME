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
			// the folders PinMameAltSound.Find looks in when AltSoundFolder is empty
			var altSound = DefaultAltSoundFolders().FirstOrDefault(Exists);
			var packs = altSound != null ? Safe(() => Directory.GetDirectories(altSound).Length) : 0;
			var on = settings.Boolean("AltSound");
			found.Add(new PluginSystemDefault("AltSound", altSound != null && altSound.StartsWith(PinMameFolders.DataFolder, StringComparison.OrdinalIgnoreCase) ? "VPE" : "VPinMAME") {
				Path = altSound ?? "",
				FieldId = "AltSoundFolder",
				Description = altSound == null
					? "No AltSound packs found. Only needed for recorded music and callouts."
					: (packs == 1 ? "1 replacement sound pack" : $"{packs} replacement sound packs")
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
			yield return Path.Combine(PinMameFolders.DataFolder, "altsound");
		}

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
