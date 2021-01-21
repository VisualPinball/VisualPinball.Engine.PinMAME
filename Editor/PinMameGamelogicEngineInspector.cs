// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
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

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VisualPinball.Unity.Editor
{
	[CustomEditor(typeof(PinMameGamelogicEngine))]
	public class PinMameGamelogicEngineInspector : UnityEditor.Editor
	{
		private PinMameGamelogicEngine _gle;
		private PinMameGame[] _games;
		private string[] _gameNames;
		private string[] _romNames = new string[0];

		private int _selectedGameIndex;
		private int _selectedRomIndex;

		private PinMameRom Rom => _gle.Game.Roms[_selectedRomIndex];

		private void OnEnable()
		{
			_gle = (PinMameGamelogicEngine) target;
			_games = new PinMameGame[] {
				new MedievalMadness(),
				new FlashGordon(),
			};
			_gameNames = new[] {"-- none --"}
				.Concat(_games.Select(g => g.Name))
				.ToArray();

			if (_gle.Game != null) {
				for (var i = 0; i < _games.Length; i++) {
					if (_games[i].Id == _gle.Game.Id) {
						_selectedGameIndex = i + 1;
						break;
					}
				}
				_romNames = _gle.Game.Roms.Select(g => g.ToString()).ToArray();
				if (!string.IsNullOrEmpty(_gle.romId)) {
					for (var i = 0; i < _gle.Game.Roms.Length; i++) {
						if (_gle.Game.Roms[i].Id == _gle.romId) {
							_selectedRomIndex = i;
							break;
						}
					}
				}
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			EditorGUILayout.Separator();

			// info label
			EditorGUILayout.LabelField("Game Name", _gle.romId);

			// game dropdown
			EditorGUI.BeginChangeCheck();
			_selectedGameIndex = EditorGUILayout.Popup("Game", _selectedGameIndex, _gameNames);
			if (EditorGUI.EndChangeCheck()) {
				_selectedRomIndex = 0;
				if (_selectedGameIndex > 0) {
					_gle.Game = _games[_selectedGameIndex - 1];
					_gle.romId = Rom.Id;
					_romNames = _gle.Game.Roms.Select(g => g.ToString()).ToArray();

				} else {
					_gle.Game = null;
					_gle.romId = string.Empty;
					_romNames = new string[0];
				}
			}

			// rom dropdown
			EditorGUI.BeginDisabledGroup(_gle.Game == null);
			EditorGUI.BeginChangeCheck();
			_selectedRomIndex = EditorGUILayout.Popup("ROM", _selectedRomIndex, _romNames);
			if (EditorGUI.EndChangeCheck()) {
				_gle.romId = Rom.Id;
			}
			EditorGUI.EndDisabledGroup();

			// initial switches button
			EditorGUI.BeginDisabledGroup(!Application.isPlaying);
			EditorGUILayout.Space();
			EditorGUILayout.Separator();
			if (GUILayout.Button("Send initial switches")) {
				_gle.SendInitialSwitches();
			}

			EditorGUI.EndDisabledGroup();
		}
	}
}
