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
	/// A host can still set <see cref="GamelogicPaths.RomFolder"/> itself before loading a table,
	/// like the player app does with its own setting.
	/// </remarks>
	[InitializeOnLoad]
	public static class PinMameEditorRomFolder
	{
		private const string PrefKey = "VisualPinball.PinMAME.RomFolder";

		private static string _vpmRomFolder;

		static PinMameEditorRomFolder()
		{
			Apply();
		}

		/// <summary>
		/// The ROM folder set in the editor, or an empty string to use Visual PinMAME's.
		/// </summary>
		public static string Custom
		{
			get => EditorPrefs.GetString(PrefKey, string.Empty);
			set {
				EditorPrefs.SetString(PrefKey, value?.Trim() ?? string.Empty);
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
			GamelogicPaths.RomFolder = Effective;
		}
	}
}
