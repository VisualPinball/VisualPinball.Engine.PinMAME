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

// ReSharper disable CheckNamespace

using PinMame;
using UnityEditor;
using VisualPinball.Unity;

namespace VisualPinball.Engine.PinMAME.Editor
{
	/// <summary>
	/// The ROM folder used when playing tables in the editor. It's a per-machine editor setting,
	/// and without one, Visual PinMAME's ROM folder is used if it's installed, so authors with a
	/// VPM setup don't need to configure anything.
	/// </summary>
	/// <remarks>
	/// It's the host's ROM folder (<see cref="GamelogicPaths.HostRomFolder"/>). A player app running
	/// in the editor can still override it with its own setting for its play session.
	/// </remarks>
	[InitializeOnLoad]
	public static class PinMameEditorRomFolder
	{
		private const string PrefKey = "VisualPinball.PinMAME.RomFolder";

		private static string _vpmRomFolder;

		static PinMameEditorRomFolder()
		{
			Apply();
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		// A player app's ROM folder only applies to its play session; editor actions like Create
		// Displays use the editor's folder again. The domain isn't necessarily reloaded on exit.
		private static void OnPlayModeStateChanged(PlayModeStateChange change)
		{
			if (change == PlayModeStateChange.EnteredEditMode) {
				GamelogicPaths.RomFolderOverride = null;
			}
		}

		/// <summary>
		/// The ROM folder set in the editor, or an empty string to use Visual PinMAME's.
		/// </summary>
		public static string Custom
		{
			get => EditorPrefs.GetString(PrefKey, string.Empty);
			set {
				EditorPrefs.SetString(PrefKey, GamelogicPaths.NormalizeFolderList(value) ?? string.Empty);
				Apply();
			}
		}

		/// <summary>
		/// Visual PinMAME's ROM folder(s), or null if it's not installed. Read when the editor
		/// loads scripts and when the setting changes.
		/// </summary>
		public static string VpmRomFolder => _vpmRomFolder;

		/// <summary>
		/// The ROM folder(s) tables played in the editor use, or null if there's none.
		/// </summary>
		public static string Effective => string.IsNullOrWhiteSpace(Custom) ? VpmRomFolder : Custom;

		private static void Apply()
		{
			_vpmRomFolder = PinMame.PinMame.FindVpmPath(PinMameFileType.Roms);
			GamelogicPaths.HostRomFolder = Effective;
		}
	}
}
