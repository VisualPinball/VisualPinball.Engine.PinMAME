// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
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

using System;
using System.IO;
using System.Linq;
using NLog;
using PinMame;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.PinMAME
{
	/// <summary>
	/// Where VPE's PinMAME keeps its files on desktop. VPE ships its own PinMAME and doesn't depend
	/// on a Visual PinMAME (VPM) installation: NVRAM, config and high scores live in VPE's data
	/// folder, shared by all VPE apps. ROMs use <see cref="GamelogicPaths.RomFolder"/> when set,
	/// otherwise VPM's ROM folder if available, then "roms" in VPE's data folder. A game's NVRAM,
	/// high scores and config are copied from VPM the first time VPE runs that game.
	/// </summary>
	public static class PinMameFolders
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static readonly string[] SubFolders = { "roms", "nvram", "cfg", "hi", "samples" };

		private static readonly (PinMameFileType Type, string Folder, string Extension)[] VpmImports = {
			(PinMameFileType.Nvram, "nvram", ".nv"),
			(PinMameFileType.HighScore, "hi", ".hi"),
			(PinMameFileType.Config, "cfg", ".cfg"),
		};

		/// <summary>
		/// PinMAME's base folder, holding roms, nvram, cfg, hi and samples. Created on access.
		/// </summary>
		public static string DataFolder
		{
			get {
				var folder = Path.Combine(GamelogicPaths.DataRoot, "PinMAME");
				foreach (var subFolder in SubFolders) {
					Directory.CreateDirectory(Path.Combine(folder, subFolder));
				}
				return folder;
			}
		}

		/// <summary>
		/// Points PinMAME at the configured ROM folder, falling back to VPM's ROM folder when
		/// available, then "roms" in VPE's data folder. Must not be called while a game is running.
		/// </summary>
		public static void ApplyRomFolder(PinMame.PinMame pinMame)
		{
			pinMame.RomPath = GamelogicPaths.RomFolder ?? PinMame.PinMame.FindVpmPath(PinMameFileType.Roms);
			Logger.Info($"[PinMAME] ROM folder: {pinMame.RomPath}; NVRAM: {pinMame.GetPath(PinMameFileType.Nvram)}; config: {pinMame.GetPath(PinMameFileType.Config)}.");
		}

		/// <summary>
		/// Whether a ROM's zip is in one of the given folders. A cheap check that doesn't need
		/// PinMAME, for UI.
		/// </summary>
		public static bool HasRomZip(string romFolder, string romId)
		{
			if (string.IsNullOrWhiteSpace(romFolder) || string.IsNullOrWhiteSpace(romId)) {
				return false;
			}
			return romFolder.Split(';')
				.Select(folder => folder.Trim())
				.Any(folder => folder.Length > 0 && File.Exists(Path.Combine(folder, romId + ".zip")));
		}

		/// <summary>
		/// Copies the game's NVRAM, high scores and config from VPM's folders, for each of them
		/// that VPE doesn't have yet. Existing files are never overwritten, so after the first run
		/// the game's state is VPE's own.
		/// </summary>
		public static void ImportFromVpm(string romId)
		{
			if (string.IsNullOrWhiteSpace(romId)) {
				return;
			}
			var dataFolder = DataFolder;
			foreach (var (type, folder, extension) in VpmImports) {
				var target = Path.Combine(dataFolder, folder, romId + extension);
				if (File.Exists(target)) {
					continue;
				}
				var vpmFolders = PinMame.PinMame.FindVpmPath(type);
				if (vpmFolders == null) {
					continue;
				}
				foreach (var vpmFolder in vpmFolders.Split(';')) {
					var source = Path.Combine(vpmFolder, romId + extension);
					if (!File.Exists(source)) {
						continue;
					}
					try {
						File.Copy(source, target, false);
						Logger.Info($"[PinMAME] Copied {source} from Visual PinMAME to {target}.");
					} catch (Exception e) {
						Logger.Warn(e, "[PinMAME] Could not copy {Source:l} from Visual PinMAME to {Target:l}.", source, target);
					}
					break;
				}
			}
		}
	}
}
