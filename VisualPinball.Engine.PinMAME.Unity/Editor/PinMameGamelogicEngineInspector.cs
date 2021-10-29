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

using System;
using System.Collections.Generic;
using System.Linq;
using PinMame;
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
		private string[] _romNames = Array.Empty<string>();

		private int _selectedGameIndex;
		private int _selectedRomIndex;

		private TableComponent _tableComponent;

		private PinMameRom Rom => _gle.Game.Roms[_selectedRomIndex];

		private bool _toggleStartup = true;

		private SerializedProperty _disableMechsProperty;
		private SerializedProperty _solenoidDelayProperty;

		private bool _toggleDebug = true;

		private SerializedProperty _disableAudioProperty;

		private void OnEnable()
		{
			_gle = (PinMameGamelogicEngine) target;

			_games = new PinMameGame[] {
				new AttackFromMars(),
				new CreatureFromTheBlackLagoon(),
				new MedievalMadness(),
				new Terminator2(),
				new FlashGordon(),
				new StarTrekEnterprise(),
				new Rock(),
			};
			_gameNames = new[] {"-- none --"}
				.Concat(_games.Select(g => g.Name))
				.ToArray();

			if (_gle != null) {
				_tableComponent = _gle.gameObject.GetComponentInParent<TableComponent>();
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

			_solenoidDelayProperty = serializedObject.FindProperty(nameof(_gle.SolenoidDelay));
			_disableMechsProperty = serializedObject.FindProperty(nameof(_gle.DisableMechs));
			_disableAudioProperty = serializedObject.FindProperty(nameof(_gle.DisableAudio));
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
					_romNames = Array.Empty<string>();
				}
			}

			//rom dropdown
			EditorGUI.BeginDisabledGroup(_gle.Game == null);
			EditorGUI.BeginChangeCheck();
			_selectedRomIndex = EditorGUILayout.Popup("ROM", _selectedRomIndex, _romNames);
			if (EditorGUI.EndChangeCheck()) {
				_gle.romId = Rom.Id;
			}

			// info label
			EditorGUILayout.LabelField("ROM ID", _gle.romId);

			EditorGUI.EndDisabledGroup();

			GUILayout.BeginVertical();

			if (_toggleStartup = EditorGUILayout.BeginFoldoutHeaderGroup(_toggleStartup, "Startup"))
			{
				EditorGUI.indentLevel++;

				EditorGUI.BeginChangeCheck();

				EditorGUILayout.PropertyField(_disableMechsProperty, new GUIContent("Disable Mechs"));

				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
				}

				EditorGUI.BeginChangeCheck();

				var cellRect = EditorGUILayout.GetControlRect();

				var labelRect = cellRect;
				labelRect.x += labelRect.width - 20;
				labelRect.width = 20;

				var fieldRect = cellRect;
				fieldRect.width -= 25;

				EditorGUI.PropertyField(fieldRect, _solenoidDelayProperty, new GUIContent("Solenoid Delay"));
				GUI.Label(labelRect, "ms");

				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
				}

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndFoldoutHeaderGroup();

			if (_toggleDebug = EditorGUILayout.BeginFoldoutHeaderGroup(_toggleDebug, "Debug"))
			{
				EditorGUI.indentLevel++;

				EditorGUI.BeginChangeCheck();

				EditorGUILayout.PropertyField(_disableAudioProperty, new GUIContent("Disable Audio"));

				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
				}

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndFoldoutHeaderGroup();

			GUILayout.EndVertical();

			EditorGUILayout.Space();
			EditorGUILayout.Separator();

			//EditorGUI.BeginDisabledGroup(!IsGameSet || Application.isPlaying);
			if (GUILayout.Button("Populate Hardware")) {
				if (EditorUtility.DisplayDialog("PinMAME", "This will clear all linked switches, coils and lamps and re-populate them. You sure you want to do that?", "Yes", "No")) {
					_tableComponent.RepopulateHardware(_gle);
					TableSelector.Instance.TableUpdated();
					SceneView.RepaintAll();
				}
			}
			if (GUILayout.Button("Create Displays")) {
				var sceneDisplays = FindObjectsOfType<DisplayComponent>();
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
			//EditorGUI.EndDisabledGroup();
		}

		private void CreateDisplays(IEnumerable<DisplayComponent> sceneDisplays)
		{
			// retrieve layouts from pinmame
			var pinMame = PinMame.PinMame.Instance(AudioSettings.outputSampleRate);
			var displayLayouts = pinMame.GetAvailableDisplays(_gle.romId);

			// retrieve already existing displays from scene
			var displayGameObjects = new Dictionary<string, DisplayComponent>();
			foreach (var displays in sceneDisplays) {
				displayGameObjects[displays.Id] = displays;
			}
			var ta = _gle.GetComponentInParent<TableComponent>();
			var pa = _gle.GetComponentInChildren<PlayfieldComponent>();
			var tableHeight = 0f;
			var tableWidth = 1f;
			if (ta) {
				tableHeight = pa.GlassHeight * PlayfieldComponent.GlobalScale;
				tableWidth = pa.Width * PlayfieldComponent.GlobalScale;
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
						? go.AddComponent<DotMatrixDisplayComponent>()
						: go.GetComponent<DotMatrixDisplayComponent>();

					auth.Id = id;
					auth.Width = layout.Width;
					auth.Height = layout.Height;

					go.name = "Dot Matrix Display";
					go.transform.localScale = new Vector3(DisplayInspector.GameObjectScale, DisplayInspector.GameObjectScale, DisplayInspector.GameObjectScale);
					go.transform.localPosition = new Vector3(0f, tableHeight, 1.1f);

				} else {
					var auth = !displayGameObjects.ContainsKey(id)
						? go.AddComponent<SegmentDisplayComponent>()
						: go.GetComponent<SegmentDisplayComponent>();

					auth.Id = id;
					auth.NumChars = layout.Length;
					auth.NumSegments = ConvertNumSegments(layout.Type);
					auth.SeparatorType = ConvertSeparatorType(layout.Type);
					auth.SeparatorEveryThreeOnly = ConvertSeparatorEveryThree(layout.Type);
					auth.SegmentTypeName = layout.Type.ToString();

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

				displayGameObjects.Remove(id);
			}
			var str = string.Join("\n", displayLayouts.Keys.Select(t => $"{t}: {displayLayouts[t]}"));
			Debug.Log($"OnDisplaysAvailable ({displayLayouts.Count}): displays=\n{str}\n{tableWidth} - {totalWidth}");

			// remove non-updated game objects.
			foreach (var displayAuthoring in displayGameObjects.Values) {
				DestroyImmediate(displayAuthoring.gameObject);
			}
		}

		private static int ConvertSeparatorType(PinMameDisplayType layoutType)
		{
			switch (layoutType) {

				case PinMameDisplayType.Seg7S:
				case PinMameDisplayType.Seg7:
				case PinMameDisplayType.Seg9:
				case PinMameDisplayType.Seg16N:
				case PinMameDisplayType.Seg7 | PinMameDisplayType.NoDisp:
				case PinMameDisplayType.Seg16S:
					return 0;

				case PinMameDisplayType.Seg16D:
				case PinMameDisplayType.Seg8D:
					return 1;

				case PinMameDisplayType.Seg7SCH:
				case PinMameDisplayType.Seg7H:
				case PinMameDisplayType.Seg7SH:
				case PinMameDisplayType.Seg7SC:
				case PinMameDisplayType.Seg8:
				case PinMameDisplayType.Seg87:
				case PinMameDisplayType.Seg87F:
				case PinMameDisplayType.Seg8H:
				case PinMameDisplayType.Seg87H:
				case PinMameDisplayType.Seg87FH:
				case PinMameDisplayType.Seg98:
				case PinMameDisplayType.Seg98F:
				case PinMameDisplayType.Seg10:
				case PinMameDisplayType.Seg16:
				case PinMameDisplayType.Seg16R:
					return 2;

				default:
					throw new ArgumentOutOfRangeException(nameof(layoutType), layoutType, "Unknown segment display size");
			}
		}

		private static bool ConvertSeparatorEveryThree(PinMameDisplayType layoutType)
		{
			switch (layoutType) {

				case PinMameDisplayType.Seg98F:
				case PinMameDisplayType.Seg98:
				case PinMameDisplayType.Seg87F:
				case PinMameDisplayType.Seg87:
					return true;

				default:
					return false;
			}
		}

		private static int ConvertNumSegments(PinMameDisplayType layoutType)
		{
			switch (layoutType) {

				case PinMameDisplayType.Seg7:
				case PinMameDisplayType.Seg7S:
				case PinMameDisplayType.Seg7SC:
				case PinMameDisplayType.Seg7SCH:
				case PinMameDisplayType.Seg7H:
				case PinMameDisplayType.Seg7SH:
				case PinMameDisplayType.Seg8:
				case PinMameDisplayType.Seg8D:
				case PinMameDisplayType.Seg8H:
				case PinMameDisplayType.Seg87:
				case PinMameDisplayType.Seg87F:
				case PinMameDisplayType.Seg87H:
				case PinMameDisplayType.Seg87FH:
				case PinMameDisplayType.Seg7 | PinMameDisplayType.NoDisp:
					return 7;

				case PinMameDisplayType.Seg9:
				case PinMameDisplayType.Seg10:
				case PinMameDisplayType.Seg98:
				case PinMameDisplayType.Seg98F:
					return 9;

				case PinMameDisplayType.Seg16:
				case PinMameDisplayType.Seg16R:
				case PinMameDisplayType.Seg16N:
				case PinMameDisplayType.Seg16D:
					return 14;

				case PinMameDisplayType.Seg16S:
					return 16;

				default:
					throw new ArgumentOutOfRangeException(nameof(layoutType), layoutType, "Unknown segment display size");
			}
		}
	}
}
