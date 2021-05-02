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

// ReSharper disable CheckNamespace

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.PinMAME.Games;
using VisualPinball.Unity;
using VisualPinball.Unity.Editor;

namespace VisualPinball.Engine.PinMAME.Editor
{
	[CustomEditor(typeof(PinMameGamelogicEngine))]
	public class PinMameGamelogicEngineInspector : UnityEditor.Editor
	{
		private bool IsGameSet => _gle.Game != null;

		private PinMameGamelogicEngine _gle;
		private PinMameGame[] _games;
		private string[] _gameNames;
		private string[] _romNames = new string[0];

		private int _selectedGameIndex;
		private int _selectedRomIndex;

		private TableAuthoring _tableAuthoring;

		private PinMameRom Rom => _gle.Game.Roms[_selectedRomIndex];

		private void OnEnable()
		{
			_gle = (PinMameGamelogicEngine) target;
			_games = new PinMameGame[] {
				new MedievalMadness(),
				new Terminator2(),
				new FlashGordon(),
			};
			_gameNames = new[] {"-- none --"}
				.Concat(_games.Select(g => g.Name))
				.ToArray();

			if (_gle != null) {
				_tableAuthoring = _gle.gameObject.GetComponentInParent<TableAuthoring>();
			}

			if (IsGameSet) {
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
			// game dropdown
			//_gle.romId = EditorGUILayout.TextField("ROM ID", _gle.romId);

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

			// info label
			EditorGUILayout.LabelField("ROM ID", _gle.romId);

			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space();
			EditorGUILayout.Separator();

			EditorGUI.BeginDisabledGroup(!IsGameSet || Application.isPlaying);
			if (GUILayout.Button("Populate Hardware")) {
				if (EditorUtility.DisplayDialog("PinMAME", "This will clear all linked switches, coils and lamps and re-populate them. You sure you want to do that?", "Yes", "No")) {
					_tableAuthoring.RepopulateHardware(_gle);
					TableSelector.Instance.TableUpdated();
					SceneView.RepaintAll();
				}
			}
			if (GUILayout.Button("Create Displays")) {
				var sceneDisplays = FindObjectsOfType<DisplayAuthoring>();
				if (sceneDisplays.Length > 0) {
					if (EditorUtility.DisplayDialog("PinMAME", "This will re-position all your displays, if you have any. You sure you want to do that?", "Yes", "No")) {
						CreateDisplays(sceneDisplays);
						SceneView.RepaintAll();
					}
				} else {
					CreateDisplays(sceneDisplays);
					SceneView.RepaintAll();
				}
			}
			EditorGUI.EndDisabledGroup();
		}

		private void CreateDisplays(IEnumerable<DisplayAuthoring> sceneDisplays)
		{
			// retrieve layouts from pinmame
			var pinMame = PinMame.PinMame.Instance();
			var displayLayouts = pinMame.GetAvailableDisplays(_gle.romId);

			// retrieve already existing displays from scene
			var displayGameObjects = new Dictionary<string, DisplayAuthoring>();
			foreach (var displays in sceneDisplays) {
				displayGameObjects[displays.Id] = displays;
			}
			var ta = _gle.GetComponentInParent<TableAuthoring>();
			var tableHeight = 0f;
			var tableWidth = 1f;
			if (ta) {
				tableHeight = ta.Table.GlassHeight * VpxConverter.GlobalScale;
				tableWidth = ta.Table.Width * VpxConverter.GlobalScale;
			}

			// get total height
			var numRows = displayLayouts.Values.Select(l => l.Top).Max() / 2;

			// get total width
			var lines = new Dictionary<int, int> { { 0, 0 }};
			foreach (var layout in displayLayouts.Values) {
				if (layout.IsDmd) {
					continue;
				}
				lines[layout.Top] = lines.ContainsKey(layout.Top)
					? lines[layout.Top] + layout.Length
					: layout.Length;
			}
			var numCols = lines.Values.ToList().Max();
			var totalWidth = 0f;

			foreach (var index in displayLayouts.Keys) {
				var layout = displayLayouts[index];

				var left = layout.Left / 2;
				var top = layout.Top / 2;

				var id = layout.IsDmd
					? $"{PinMameGamelogicEngine.DmdPrefix}{index}"
					: $"{PinMameGamelogicEngine.SegDispPrefix}{index}";

				var go = displayGameObjects.ContainsKey(id)
					? displayGameObjects[id].gameObject
					: new GameObject();

				if (layout.IsDmd) {
					var auth = !displayGameObjects.ContainsKey(id)
						? go.AddComponent<DotMatrixDisplayAuthoring>()
						: go.GetComponent<DotMatrixDisplayAuthoring>();

					auth.Id = id;
					auth.Width = layout.Width;
					auth.Height = layout.Height;

					go.name = "Dot Matrix Display";
					go.transform.localScale = new Vector3(DisplayInspector.GameObjectScale, DisplayInspector.GameObjectScale, DisplayInspector.GameObjectScale);
					go.transform.localPosition = new Vector3(0f, tableHeight, 1.1f);

				} else {
					var auth = !displayGameObjects.ContainsKey(id)
						? go.AddComponent<SegmentDisplayAuthoring>()
						: go.GetComponent<SegmentDisplayAuthoring>();

					auth.Id = id;
					auth.NumChars = layout.Length;
					//auth.SegmentType = auth.ConvertSegmentType(layout.type);

					go.name = $"Segment Display [{index}]";
					go.transform.localScale = new Vector3(DisplayInspector.GameObjectScale, DisplayInspector.GameObjectScale, DisplayInspector.GameObjectScale);
					var mesh = go.GetComponent<MeshFilter>().sharedMesh;
					var charHeight = mesh.bounds.size.y * DisplayInspector.GameObjectScale;
					var charWidth = mesh.bounds.size.x * DisplayInspector.GameObjectScale / layout.Length;
					totalWidth = charWidth * numCols;

					var globalLeft = (tableWidth - totalWidth) / 2;

					go.transform.localPosition = new Vector3(
						globalLeft - tableWidth / 2 + charWidth * left + mesh.bounds.size.x / 2 * DisplayInspector.GameObjectScale,
						tableHeight + (numRows - top) * charHeight,
						1.1f);
				}
			}


			var str = string.Join("\n", displayLayouts.Keys.Select(t => $"{t}: {displayLayouts[t]}"));
			Debug.Log($"OnDisplaysAvailable ({displayLayouts.Count}): displays=\n{str}\n{tableWidth} - {totalWidth}");
		}
	}
}
