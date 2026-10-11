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
using System.IO;
using System.Linq;
using System.Threading;
using PinMame;
using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;
using VisualPinball.Unity.Editor;

namespace VisualPinball.Engine.PinMAME.Editor
{
	[CustomEditor(typeof(PinMameGamelogicEngine))]
	public class PinMameGamelogicEngineInspector : UnityEditor.Editor
	{
		private PinMameGamelogicEngine _gle;
		private PinMameCatalogMachine[] _machines = Array.Empty<PinMameCatalogMachine>();
		private PinMameCatalogMachine _selectedMachine;
		private PinMameGameDefinition _definition;
		private string _definitionJson;
		private PinMameRomDefinition[] _roms = Array.Empty<PinMameRomDefinition>();
		private int _selectedGameIndex;
		private int _selectedRomIndex;
		private TableComponent _tableComponent;
		private bool _toggleStartup = true;
		private SerializedProperty _disableMechsProperty;
		private SerializedProperty _solenoidDelayProperty;
		private bool _toggleDebug = true;
		private int _debugSwitchId;
		private SerializedProperty _disableAudioProperty;
		private CancellationTokenSource _loadCancellation;
		private bool _isLoading;
		private string _loadError;
		private string _loadWarning;
		private string _spatialAssignmentMessage;
		private MessageType _spatialAssignmentMessageType = MessageType.Info;
		private string _romStatusKey;
		private bool _romZipFound;

		private void OnEnable()
		{
			_gle = (PinMameGamelogicEngine) target;
			if (_gle != null) {
				_tableComponent = _gle.gameObject.GetComponentInParent<TableComponent>();
			}
			_solenoidDelayProperty = serializedObject.FindProperty(nameof(_gle.SolenoidDelay));
			_disableMechsProperty = serializedObject.FindProperty(nameof(_gle.DisableMechs));
			_disableAudioProperty = serializedObject.FindProperty(nameof(_gle.DisableAudio));
			LoadCatalog(false);
		}

		private void OnDisable()
		{
			_loadCancellation?.Cancel();
			_loadCancellation?.Dispose();
			_loadCancellation = null;
			_isLoading = false;
		}

		private async void LoadCatalog(bool forceRefresh)
		{
			var load = ResetCancellation();
			var cancellationToken = load.Token;
			_isLoading = true;
			_loadError = null;
			_loadWarning = null;
			try {
				var result = await PinMameGameDefinitionCatalogClient.GetCatalogAsync(forceRefresh, cancellationToken);
				EnsureCurrent(load);
				var machines = result.Value.Machines
					.Where(machine => machine.IsSelectable)
					.OrderBy(machine => machine.Name, StringComparer.OrdinalIgnoreCase)
					.ThenByDescending(machine => machine.Year)
					.ToArray();
				_machines = machines;
				_loadWarning = result.Warning;

				var selectedIndex = FindSelectedMachineIndex();
				if (selectedIndex < 0 && !string.IsNullOrWhiteSpace(_gle.romId)) {
					var resolvedMachineId = await PinMameGameDefinitionCatalogClient.ResolveMachineIdAsync(_gle.romId, cancellationToken);
					EnsureCurrent(load);
					selectedIndex = Array.FindIndex(_machines, machine => string.Equals(machine.Id, resolvedMachineId, StringComparison.Ordinal));
				}
				_selectedGameIndex = selectedIndex + 1;
				if (_selectedGameIndex > 0) {
					await SelectMachine(_selectedGameIndex, _gle.romId, forceRefresh, false, load);
				}
			} catch (OperationCanceledException) {
				return;
			} catch (Exception exception) {
				if (ReferenceEquals(_loadCancellation, load)) _loadError = exception.Message;
			} finally {
				CompleteLoad(load);
			}
		}

		private int FindSelectedMachineIndex()
		{
			if (!string.IsNullOrWhiteSpace(_gle.machineId)) {
				var byId = Array.FindIndex(_machines, machine => string.Equals(machine.Id, _gle.machineId, StringComparison.Ordinal));
				if (byId >= 0) {
					return byId;
				}
			}
			if (string.IsNullOrWhiteSpace(_gle.romId)) {
				return -1;
			}
			return Array.FindIndex(_machines, machine => MachineRoms(machine).Contains(_gle.romId, StringComparer.OrdinalIgnoreCase));
		}

		private async void ChangeMachine(int selectedGameIndex)
		{
			var load = ResetCancellation();
			_isLoading = true;
			_loadError = null;
			_loadWarning = null;
			try {
				await SelectMachine(selectedGameIndex, null, false, true, load);
			} catch (OperationCanceledException) {
				return;
			} catch (Exception exception) {
				if (ReferenceEquals(_loadCancellation, load)) _loadError = exception.Message;
			} finally {
				CompleteLoad(load);
			}
		}

		private async System.Threading.Tasks.Task SelectMachine(int selectedGameIndex, string preferredRomId, bool forceRefresh,
			bool applySelection, CancellationTokenSource load)
		{
			EnsureCurrent(load);
			var cancellationToken = load.Token;
			if (selectedGameIndex < 0 || selectedGameIndex > _machines.Length) {
				throw new ArgumentOutOfRangeException(nameof(selectedGameIndex));
			}

			_selectedGameIndex = selectedGameIndex;
			_selectedRomIndex = 0;
			_definition = null;
			_definitionJson = null;
			_roms = Array.Empty<PinMameRomDefinition>();
			_spatialAssignmentMessage = null;

			if (selectedGameIndex <= 0) {
				_selectedMachine = null;
				if (applySelection) {
					Undo.RecordObject(_gle, "Select PinMAME machine");
					_gle.SetMachineSelection(null, null);
					EditorUtility.SetDirty(_gle);
				}
				return;
			}

			var selectedMachine = _machines[selectedGameIndex - 1];
			_selectedMachine = selectedMachine;

			var result = await PinMameGameDefinitionCatalogClient.GetDefinitionAsync(selectedMachine, forceRefresh, cancellationToken);
			EnsureCurrent(load);
			_definitionJson = result.Value;
			_loadWarning = CombineWarnings(_loadWarning, result.Warning);
			var definition = PinMameGameDefinitionConverter.Convert(_definitionJson, preferredRomId);
			if (!string.Equals(definition.MachineId, selectedMachine.Id, StringComparison.Ordinal)) {
				throw new InvalidDataException($"The catalog entry {selectedMachine.Id} returned definition {definition.MachineId}.");
			}
			_definition = definition;
			var selectedRomId = SetRoms(_definition.Roms, preferredRomId);
			if (applySelection) {
				Undo.RecordObject(_gle, "Select PinMAME machine");
				_gle.SetMachineSelection(selectedMachine.Id, selectedRomId);
				EditorUtility.SetDirty(_gle);
			} else if (!string.IsNullOrWhiteSpace(preferredRomId) && selectedRomId == null) {
				_loadWarning = CombineWarnings(_loadWarning, $"The saved ROM '{preferredRomId}' is not listed for {selectedMachine.Name}; the saved component value was left unchanged.");
			}
		}

		private static string CombineWarnings(string first, string second)
		{
			if (string.IsNullOrWhiteSpace(first)) return second;
			if (string.IsNullOrWhiteSpace(second) || string.Equals(first, second, StringComparison.Ordinal)) return first;
			return $"{first}\n{second}";
		}

		private PinMameGameDefinition ConvertSelectedRom(string selectedRomId)
		{
			if (string.IsNullOrWhiteSpace(_definitionJson)) {
				return null;
			}
			return PinMameGameDefinitionConverter.Convert(_definitionJson, selectedRomId);
		}

		private string SetRoms(PinMameRomDefinition[] roms, string preferredRomId)
		{
			_roms = roms ?? Array.Empty<PinMameRomDefinition>();
			_selectedRomIndex = Array.FindIndex(_roms, rom => string.Equals(rom.Id, preferredRomId, StringComparison.OrdinalIgnoreCase));
			if (_selectedRomIndex < 0) {
				_selectedRomIndex = string.IsNullOrWhiteSpace(preferredRomId) && _roms.Length > 0 ? 0 : -1;
			}
			return _selectedRomIndex >= 0 ? _roms[_selectedRomIndex].Id : null;
		}

		private static string[] MachineRoms(PinMameCatalogMachine machine) => machine.Roms?.Length > 0 ? machine.Roms : machine.RootDrivers ?? Array.Empty<string>();

		private CancellationTokenSource ResetCancellation()
		{
			_loadCancellation?.Cancel();
			_loadCancellation?.Dispose();
			_loadCancellation = new CancellationTokenSource();
			return _loadCancellation;
		}

		private void CompleteLoad(CancellationTokenSource load)
		{
			if (!ReferenceEquals(_loadCancellation, load)) return;
			_isLoading = false;
			if (this && target) Repaint();
		}

		private void EnsureCurrent(CancellationTokenSource load)
		{
			load.Token.ThrowIfCancellationRequested();
			if (!ReferenceEquals(_loadCancellation, load)) throw new OperationCanceledException(load.Token);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			if (_isLoading) {
				EditorGUILayout.HelpBox("Loading PinMAME machine definitions…", MessageType.Info);
			}
			if (!string.IsNullOrWhiteSpace(_loadWarning)) {
				EditorGUILayout.HelpBox(_loadWarning, MessageType.Warning);
			}
			if (!string.IsNullOrWhiteSpace(_loadError)) {
				EditorGUILayout.HelpBox(_loadError, MessageType.Error);
			}

			EditorGUI.BeginDisabledGroup(_machines.Length == 0 || Application.isPlaying || _isLoading);
			DrawMachinePicker();
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(_roms.Length == 0 || Application.isPlaying || _isLoading);
			DrawRomPicker();
			EditorGUI.EndDisabledGroup();

			if (_selectedMachine != null || !string.IsNullOrWhiteSpace(_gle.machineId) || !string.IsNullOrWhiteSpace(_gle.romId)) {
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.LabelField(SelectedMachineSummary(), EditorStyles.miniLabel);
				EditorGUILayout.LabelField(SelectedDefinitionIds(), EditorStyles.miniLabel);
				EditorGUILayout.EndVertical();
			}
			if (_definition != null) {
				foreach (var diagnostic in _definition.Diagnostics) {
					EditorGUILayout.HelpBox(diagnostic, MessageType.Warning);
				}
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(PinMameGameDefinitionCatalogClient.BaseUrl, EditorStyles.miniLabel);
			if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(64f))) LoadCatalog(true);
			EditorGUILayout.EndHorizontal();

			DrawRomFolder();

			GUILayout.BeginVertical();

			if (_toggleStartup = EditorGUILayout.BeginFoldoutHeaderGroup(_toggleStartup, "Startup")) {
				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(_disableMechsProperty, new GUIContent("Disable Mechs"));
				if (EditorGUI.EndChangeCheck()) {
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
				if (EditorGUI.EndChangeCheck()) {
					serializedObject.ApplyModifiedProperties();
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			if (_toggleDebug = EditorGUILayout.BeginFoldoutHeaderGroup(_toggleDebug, "Debug")) {
				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(_disableAudioProperty, new GUIContent("Disable Audio"));
				if (EditorGUI.EndChangeCheck()) {
					serializedObject.ApplyModifiedProperties();
				}
				EditorGUILayout.BeginHorizontal();
				_debugSwitchId = EditorGUILayout.IntField("Switch", _debugSwitchId);
				if (GUILayout.Button("ON")) {
					_gle.Switch(_debugSwitchId.ToString(), true);
				}
				if (GUILayout.Button("OFF")) {
					_gle.Switch(_debugSwitchId.ToString(), false);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
			GUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.Separator();
			if (Application.isPlaying) {
				if (GUILayout.Button("Toggle Speed")) {
					_gle.ToggleSpeed();
				}

				EditorGUILayout.Space();
				EditorGUILayout.Separator();
			}

			EditorGUI.BeginDisabledGroup(_definition == null || _selectedRomIndex < 0 || _tableComponent == null || Application.isPlaying || _isLoading);
			if (GUILayout.Button("Populate Hardware")) {
				if (EditorUtility.DisplayDialog("PinMAME", "This will clear all linked switches, coils and lamps and re-populate them from the selected machine definition. Continue?", "Yes", "No")) {
					Undo.RecordObject(_gle, "Populate PinMAME hardware");
					Undo.RecordObject(_tableComponent, "Populate PinMAME hardware");
					_gle.SetMachineSelection(_definition.MachineId, _roms[_selectedRomIndex].Id);
					_gle.SetAvailableHardware(_definition.Switches, _definition.Coils, _definition.Lamps, _definition.Wires);
					_tableComponent.RepopulateHardware(_gle);
					EditorUtility.SetDirty(_gle);
					EditorUtility.SetDirty(_tableComponent);
					TableSelector.Instance.TableUpdated();
					SceneView.RepaintAll();
				}
			}
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Spatial Assignment", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Fills empty playfield mappings. Existing links are preserved as layout anchors; uncertain matches stay empty.", EditorStyles.wordWrappedMiniLabel);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Switches")) ApplySpatialAssignments("switch", PinMameSpatialAssignmentService.AssignSwitches);
			if (GUILayout.Button("Coils")) ApplySpatialAssignments("coil", PinMameSpatialAssignmentService.AssignCoils);
			if (GUILayout.Button("Lamps")) ApplySpatialAssignments("lamp", PinMameSpatialAssignmentService.AssignLamps);
			EditorGUILayout.EndHorizontal();
			if (!string.IsNullOrWhiteSpace(_spatialAssignmentMessage)) {
				EditorGUILayout.HelpBox(_spatialAssignmentMessage, _spatialAssignmentMessageType);
			}
			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_gle.romId));
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
			EditorGUI.EndDisabledGroup();
		}

		private void ApplySpatialAssignments(string hardwareType,
			Func<TableComponent, PinMameGameDefinition, Action, PinMameSpatialAssignmentResult> assign)
		{
			var result = assign(_tableComponent, _definition,
				() => Undo.RecordObject(_tableComponent, $"Assign PinMAME {hardwareType} mappings by position"));
			_spatialAssignmentMessage = result.ToString();
			_spatialAssignmentMessageType = result.Changed ? MessageType.Info : MessageType.Warning;
			if (!result.Changed) return;
			EditorUtility.SetDirty(_tableComponent);
			TableSelector.Instance.TableUpdated();
			SceneView.RepaintAll();
		}

		private void DrawMachinePicker()
		{
			EditorGUILayout.LabelField("Game", EditorStyles.miniLabel);
			var fieldRect = EditorGUILayout.GetControlRect();
			var displayName = _selectedMachine?.Name
				?? (!string.IsNullOrWhiteSpace(_gle.machineId) ? _gle.machineId : _isLoading ? "Loading definitions…" : "Search games…");
			var content = new GUIContent(displayName, EditorGUIUtility.IconContent("Search Icon").image, "Search the published physical-machine catalog.");
			if (!EditorGUI.DropdownButton(fieldRect, content, FocusType.Keyboard, EditorStyles.popup)) return;

			var picker = new PinMameSearchablePicker<PinMameCatalogMachine>(
				"Select PinMAME game",
				"game",
				_machines,
				_selectedGameIndex - 1,
				machine => machine.Name,
				MachineMetadata,
				MachineCoverage,
				machine => $"{machine.Name} {machine.Manufacturer} {machine.Year} {machine.Id} {machine.Status} {machine.CompletionScore}",
				index => {
					_selectedGameIndex = index + 1;
					ChangeMachine(_selectedGameIndex);
				},
				true);
			PopupWindow.Show(fieldRect, picker);
		}

		private void DrawRomPicker()
		{
			EditorGUILayout.LabelField("ROM", EditorStyles.miniLabel);
			var fieldRect = EditorGUILayout.GetControlRect();
			var displayName = _selectedRomIndex >= 0 && _selectedRomIndex < _roms.Length
				? CompactRomLabel(_roms[_selectedRomIndex])
				: !string.IsNullOrWhiteSpace(_gle.romId) ? _gle.romId : "Search ROMs…";
			var content = new GUIContent(displayName, EditorGUIUtility.IconContent("Search Icon").image, "Search ROM revisions for the selected game.");
			if (!EditorGUI.DropdownButton(fieldRect, content, FocusType.Keyboard, EditorStyles.popup)) return;

			var picker = new PinMameSearchablePicker<PinMameRomDefinition>(
				$"Select ROM for {_selectedMachine?.Name}",
				"ROM",
				_roms,
				_selectedRomIndex,
				rom => rom.Id,
				CompactRomDescription,
				null,
				rom => $"{rom.Id} {rom.Description} {CompactRomDescription(rom)}",
				ChangeRom);
			PopupWindow.Show(fieldRect, picker);
		}

		private void ChangeRom(int selectedRomIndex)
		{
			if (selectedRomIndex < 0 || selectedRomIndex >= _roms.Length) return;
			var selectedRomId = _roms[selectedRomIndex].Id;
			try {
				var definition = ConvertSelectedRom(selectedRomId);
				Undo.RecordObject(_gle, "Select PinMAME ROM");
				_definition = definition;
				SetRoms(definition.Roms, selectedRomId);
				_gle.SetMachineSelection(definition.MachineId, selectedRomId);
				_loadError = null;
				_spatialAssignmentMessage = null;
				EditorUtility.SetDirty(_gle);
			} catch (Exception exception) {
				_loadError = exception.Message;
				SetRoms(_definition?.Roms, _gle.romId);
			}
			Repaint();
		}

		private static string MachineMetadata(PinMameCatalogMachine machine)
		{
			return string.Join(" · ", new[] {
				machine.Manufacturer,
				machine.Year?.ToString(),
			}.Where(value => !string.IsNullOrWhiteSpace(value)));
		}

		private static string MachineCoverage(PinMameCatalogMachine machine)
		{
			return string.Equals(machine.Status, "author_ready", StringComparison.Ordinal)
				? "Author-ready"
				: $"{machine.CompletionScore}% complete";
		}

		private string SelectedMachineSummary()
		{
			if (_selectedMachine == null) {
				return _isLoading ? "Resolving saved selection…" : "Saved selection is not available in the current catalog";
			}
			var coverage = _definition == null
				? MachineCoverage(_selectedMachine)
				: string.Equals(_definition.CoverageStatus, "author_ready", StringComparison.Ordinal)
					? "Author-ready"
					: $"{_definition.CompletionScore}% complete";
			return string.Join(" · ", new[] {
				MachineMetadata(_selectedMachine),
				coverage,
			}.Where(value => !string.IsNullOrWhiteSpace(value)));
		}

		private string SelectedDefinitionIds()
		{
			var machineId = string.IsNullOrWhiteSpace(_gle.machineId) ? _selectedMachine?.Id : _gle.machineId;
			return string.Join(" · ", new[] {
				string.IsNullOrWhiteSpace(machineId) ? null : $"Machine: {machineId}",
				string.IsNullOrWhiteSpace(_gle.romId) ? null : $"ROM: {_gle.romId}",
			}.Where(value => !string.IsNullOrWhiteSpace(value)));
		}

		private string CompactRomLabel(PinMameRomDefinition rom)
		{
			var description = CompactRomDescription(rom);
			return string.IsNullOrWhiteSpace(description) ? rom.Id : $"{rom.Id} · {description}";
		}

		private string CompactRomDescription(PinMameRomDefinition rom)
		{
			var description = rom?.Description?.Trim() ?? string.Empty;
			foreach (var title in new[] { _selectedMachine?.Name, _definition?.Name }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase)) {
				if (!description.StartsWith(title, StringComparison.OrdinalIgnoreCase)) continue;
				description = description.Substring(title.Length).TrimStart(' ', ':', '-', '—', '·');
				break;
			}
			if (description.Length >= 2 && description[0] == '(' && description.IndexOf(')') == description.Length - 1) {
				description = description.Substring(1, description.Length - 2).Trim();
			}
			return description;
		}

		private void DrawRomFolder()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			var custom = EditorGUILayout.DelayedTextField(new GUIContent("ROM Folder",
				"Where PinMAME looks for ROMs when playing in the editor. Separate several folders with ';'. "
				+ "Leave empty to use Visual PinMAME's ROM folder. Saved on this machine, not in the project."), PinMameEditorRomFolder.Custom);
			if (EditorGUI.EndChangeCheck()) {
				PinMameEditorRomFolder.Custom = custom;
			}
			if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(24f))) {
				var current = PinMameEditorRomFolder.Effective?.Split(';')[0] ?? string.Empty;
				var folder = EditorUtility.OpenFolderPanel("PinMAME ROM Folder", current, string.Empty);
				if (!string.IsNullOrEmpty(folder)) {
					PinMameEditorRomFolder.Custom = folder.Replace('/', Path.DirectorySeparatorChar);
				}
				GUIUtility.ExitGUI();
			}
			EditorGUILayout.EndHorizontal();

			var effective = PinMameEditorRomFolder.Effective;
			if (string.IsNullOrWhiteSpace(PinMameEditorRomFolder.Custom)) {
				EditorGUILayout.LabelField(effective != null
					? $"Using Visual PinMAME's ROM folder: {effective}"
					: "Set the folder with your ROMs. Visual PinMAME's isn't available to fall back to.", EditorStyles.wordWrappedMiniLabel);
			}
			if (effective != null && !string.IsNullOrWhiteSpace(_gle.romId)) {
				var key = effective + "|" + _gle.romId;
				if (key != _romStatusKey) {
					_romStatusKey = key;
					_romZipFound = PinMameFolders.HasRomZip(effective, _gle.romId);
				}
				if (!_romZipFound) {
					EditorGUILayout.HelpBox($"{_gle.romId}.zip isn't in the ROM folder.", MessageType.Warning);
				}
			}
			EditorGUILayout.EndVertical();
		}

		private void CreateDisplays(IEnumerable<DisplayComponent> sceneDisplays)
		{
			// retrieve layouts from pinmame
			var pinMame = PinMame.PinMame.Instance(PinMameAudioFormat.AudioFormatFloat, AudioSettings.outputSampleRate, PinMameFolders.DataFolder);
			if (!PinMame.PinMame.IsRunning) {
				PinMameFolders.ApplyRomFolder(pinMame);
			}
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
				tableHeight = pa.GlassHeight * 0.00053975f; // todo use Physics.ScaleToVpx()
				tableWidth = pa.Width * 0.00053975f;
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
