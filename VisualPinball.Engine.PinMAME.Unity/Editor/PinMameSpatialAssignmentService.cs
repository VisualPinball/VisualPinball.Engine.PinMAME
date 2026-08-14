// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;

namespace VisualPinball.Engine.PinMAME.Editor
{
	internal static class PinMameSpatialAssignmentService
	{
		private const double AnchorMaximumDistance = 0.15;

		internal static PinMameSpatialAssignmentResult AssignSwitches(TableComponent table, PinMameGameDefinition definition,
			Action onBeforeMutate = null)
		{
			return Assign(
				table,
				definition.SpatialSwitches,
				table.MappingConfig.Switches,
				table.GetComponentsInChildren<ISwitchDeviceComponent>(true),
				(mapping, placement) => mapping.Source == SwitchSource.Playfield && mapping.Id == placement.HardwareId,
				mapping => mapping.Source == SwitchSource.Playfield,
				mapping => mapping.Device,
				mapping => mapping.DeviceItem,
				(mapping, device, item) => {
					mapping.Device = device;
					mapping.DeviceItem = item;
				},
				device => device.AvailableSwitches,
				mapping => new SwitchMapping {
					Id = mapping.Id,
					IsNormallyClosed = mapping.IsNormallyClosed,
					Description = mapping.Description,
					Source = mapping.Source,
					InputActionMap = mapping.InputActionMap,
					InputAction = mapping.InputAction,
					Constant = mapping.Constant,
					PulseDelay = mapping.PulseDelay,
				},
				_ => true,
				_ => string.Empty,
				_ => string.Empty,
				onBeforeMutate);
		}

		internal static PinMameSpatialAssignmentResult AssignCoils(TableComponent table, PinMameGameDefinition definition,
			Action onBeforeMutate = null)
		{
			return Assign(
				table,
				definition.SpatialCoils,
				table.MappingConfig.Coils,
				table.GetComponentsInChildren<ICoilDeviceComponent>(true),
				(mapping, placement) => mapping.Destination == CoilDestination.Playfield && mapping.Id == placement.HardwareId,
				mapping => mapping.Destination == CoilDestination.Playfield,
				mapping => mapping.Device,
				mapping => mapping.DeviceItem,
				(mapping, device, item) => {
					mapping.Device = device;
					mapping.DeviceItem = item;
				},
				device => device.AvailableCoils,
				mapping => new CoilMapping {
					Id = mapping.Id,
					Description = mapping.Description,
					Destination = mapping.Destination,
				},
				_ => true,
				_ => string.Empty,
				_ => string.Empty,
				onBeforeMutate);
		}

		internal static PinMameSpatialAssignmentResult AssignLamps(TableComponent table, PinMameGameDefinition definition,
			Action onBeforeMutate = null)
		{
			return Assign(
				table,
				definition.SpatialLamps,
				table.MappingConfig.Lamps,
				table.GetComponentsInChildren<ILampDeviceComponent>(true),
				(mapping, placement) => mapping.Id == placement.HardwareId && mapping.Source == placement.Source
					&& mapping.Channel == placement.Channel && mapping.IsCoil == placement.IsCoil,
				_ => true,
				mapping => mapping.Device,
				mapping => mapping.DeviceItem,
				(mapping, device, item) => {
					mapping.Device = device;
					mapping.DeviceItem = item;
				},
				device => device.AvailableLamps,
				mapping => new LampMapping {
					Id = mapping.Id,
					Source = mapping.Source,
					FadingSteps = mapping.FadingSteps,
					IsCoil = mapping.IsCoil,
					Description = mapping.Description,
					Type = mapping.Type,
					Channel = mapping.Channel,
				},
				device => device is not LightGroupComponent,
				placement => placement.Channel.ToString(),
				mapping => mapping.Channel.ToString(),
				onBeforeMutate);
		}

		private static PinMameSpatialAssignmentResult Assign<TMapping, TDevice, TItem>(TableComponent table,
			IReadOnlyList<PinMameSpatialPlacement> placements, List<TMapping> mappings, IEnumerable<TDevice> devices,
			Func<TMapping, PinMameSpatialPlacement, bool> mappingMatchesPlacement, Func<TMapping, bool> isPlayfieldMapping,
			Func<TMapping, TDevice> getDevice, Func<TMapping, string> getDeviceItem,
			Action<TMapping, TDevice, string> setDevice, Func<TDevice, IEnumerable<TItem>> getItems,
			Func<TMapping, TMapping> cloneMapping, Func<TDevice, bool> mayAssign,
			Func<PinMameSpatialPlacement, string> placementPartition, Func<TMapping, string> mappingPartition,
			Action onBeforeMutate)
			where TDevice : class, IMainComponent
			where TItem : IGamelogicEngineDeviceItem
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (placements == null || placements.Count == 0) {
				return PinMameSpatialAssignmentResult.NoSpatialData();
			}
			if (mappings == null || mappings.Count == 0) {
				return PinMameSpatialAssignmentResult.NoMappings();
			}

			var playfield = table.GetComponentInChildren<PlayfieldComponent>(true);
			if (playfield == null || playfield.Right <= playfield.Left || playfield.Bottom <= playfield.Top) {
				return PinMameSpatialAssignmentResult.InvalidPlayfield();
			}
			var deviceArray = devices.ToArray();
			var assigned = 0;
			var anchored = 0;
			var rejected = 0;
			var unrepresented = 0;
			var details = new List<string>();
			var mutationStarted = false;
			void BeginMutation()
			{
				if (mutationStarted) return;
				mutationStarted = true;
				onBeforeMutate?.Invoke();
			}
			foreach (var partition in placements.GroupBy(placementPartition, StringComparer.Ordinal)) {
				var partitionResult = AssignPartition(partition.ToArray(), mappings, deviceArray,
					mappingMatchesPlacement, isPlayfieldMapping, getDevice, getDeviceItem, setDevice, getItems,
					cloneMapping, mayAssign, playfield,
					mapping => string.Equals(mappingPartition(mapping), partition.Key, StringComparison.Ordinal), BeginMutation);
				assigned += partitionResult.AssignedCount;
				anchored += partitionResult.AnchorCount;
				rejected += partitionResult.RejectedCount;
				unrepresented += partitionResult.UnrepresentedCount;
				details.AddRange(partitionResult.Details);
			}
			return new PinMameSpatialAssignmentResult(placements.Count, assigned, anchored, rejected, unrepresented,
				details.Take(3).ToArray(), null);
		}

		private static PinMameSpatialAssignmentResult AssignPartition<TMapping, TDevice, TItem>(
			IReadOnlyList<PinMameSpatialPlacement> placements, List<TMapping> mappings, IEnumerable<TDevice> devices,
			Func<TMapping, PinMameSpatialPlacement, bool> mappingMatchesPlacement, Func<TMapping, bool> isPlayfieldMapping,
			Func<TMapping, TDevice> getDevice, Func<TMapping, string> getDeviceItem,
			Action<TMapping, TDevice, string> setDevice, Func<TDevice, IEnumerable<TItem>> getItems,
			Func<TMapping, TMapping> cloneMapping, Func<TDevice, bool> mayAssign, PlayfieldComponent playfield,
			Func<TMapping, bool> isInPartition, Action beginMutation)
			where TDevice : class, IMainComponent
			where TItem : IGamelogicEngineDeviceItem
		{
			var indexedPlacements = placements.Select((placement, index) => new IndexedPlacement(placement,
				$"{placement.HardwareId}:{placement.PlacementId}:{index}")).ToArray();
			var allSlots = devices.SelectMany(device => getItems(device).Select(item => CreateSlot(device, item.Id, playfield)))
				.Where(slot => slot != null).ToArray();
			var usedSlotKeys = new HashSet<string>(StringComparer.Ordinal);
			var anchoredTargetKeys = new HashSet<string>(StringComparer.Ordinal);
			var anchors = new List<PinMameSpatialMatchAnchor>();

			foreach (var mapping in mappings.Where(mapping => isPlayfieldMapping(mapping) && isInPartition(mapping))) {
				var device = getDevice(mapping);
				if (!IsAlive(device)) continue;
				var deviceItem = ResolveDeviceItem(getDeviceItem(mapping), getItems(device));
				var coveredDevices = CoveredDevices(device).ToArray();
				var deviceSlots = allSlots.Where(slot => coveredDevices.Any(covered => ReferenceEquals(slot.Device, covered))).ToArray();
				if (device is LightGroupComponent || string.IsNullOrEmpty(deviceItem)) {
					foreach (var slot in deviceSlots) usedSlotKeys.Add(slot.Match.Key);
				} else {
					foreach (var slot in deviceSlots.Where(slot => slot.DeviceItem == deviceItem)) usedSlotKeys.Add(slot.Match.Key);
				}

				var matchingTargets = indexedPlacements
					.Where(item => !anchoredTargetKeys.Contains(item.Key) && mappingMatchesPlacement(mapping, item.Placement))
					.ToArray();
				var scenePoints = AnchorPositions(device, playfield).ToArray();
				if (device is LightGroupComponent && matchingTargets.Length == 1 && scenePoints.Length > 1) {
					var groupPosition = DevicePosition(device, playfield);
					if (groupPosition.HasValue) scenePoints = new[] { groupPosition.Value };
				}
				var anchorPairs = matchingTargets
					.SelectMany(target => scenePoints.Select((point, pointIndex) => new {
						Target = target,
						Point = point,
						PointIndex = pointIndex,
						Distance = Distance(target.Placement.X, target.Placement.Y, point.x, point.y),
					}))
					.OrderBy(pair => pair.Distance)
					.ToArray();
				var usedPoints = new HashSet<int>();
				foreach (var pair in anchorPairs) {
					if (pair.Distance > AnchorMaximumDistance) break;
					if (anchoredTargetKeys.Contains(pair.Target.Key) || !usedPoints.Add(pair.PointIndex)) continue;
					anchoredTargetKeys.Add(pair.Target.Key);
					anchors.Add(new PinMameSpatialMatchAnchor(pair.Target.Placement.X, pair.Target.Placement.Y,
						pair.Point.x, pair.Point.y, pair.Target.Key));
				}
			}

			var targetItems = indexedPlacements
				.Where(item => !anchoredTargetKeys.Contains(item.Key))
				.Where(item => mappings.Any(mapping => isInPartition(mapping) && mappingMatchesPlacement(mapping, item.Placement)))
				.ToArray();
			var unrepresented = indexedPlacements.Length - anchoredTargetKeys.Count - targetItems.Length;
			var candidateSlots = allSlots
				.Where(slot => !usedSlotKeys.Contains(slot.Match.Key) && mayAssign(slot.Device))
				.ToArray();
			var result = PinMameSpatialMatcher.Match(
				targetItems.Select(item => new PinMameSpatialMatchTarget(item.Key, item.Placement.X, item.Placement.Y)).ToArray(),
				candidateSlots.Select(slot => slot.Match).ToArray(),
				anchors);

			var assigned = 0;
			var rejectionReasons = new List<string>();
			foreach (var decision in result.Decisions) {
				var target = targetItems.First(item => item.Key == decision.Target.Key);
				if (!decision.IsAccepted) {
					rejectionReasons.Add($"{target.Placement.Label}: {decision.Reason}");
					continue;
				}
				var slot = candidateSlots.First(item => item.Match.Key == decision.Candidate.Key);
				var mapping = mappings.FirstOrDefault(item => isInPartition(item) && mappingMatchesPlacement(item, target.Placement)
					&& !IsAlive(getDevice(item)) && isPlayfieldMapping(item));
				if (mapping == null) {
					var template = mappings.FirstOrDefault(item => isInPartition(item) && mappingMatchesPlacement(item, target.Placement));
					if (template == null) continue;
					beginMutation();
					mapping = cloneMapping(template);
					mappings.Add(mapping);
				}
				beginMutation();
				setDevice(mapping, slot.Device, slot.DeviceItem);
				assigned++;
			}

			return new PinMameSpatialAssignmentResult(placements.Count, assigned, anchors.Count,
				result.Decisions.Length - assigned, unrepresented, rejectionReasons.Take(3).ToArray(), null);
		}

		private static SceneSlot<TDevice> CreateSlot<TDevice>(TDevice device, string deviceItem, PlayfieldComponent playfield)
			where TDevice : class, IMainComponent
		{
			var position = DevicePosition(device, playfield);
			if (position == null) return null;
			var component = device as MonoBehaviour;
			var entityId = UnityObjectId.Get(component ? (UnityEngine.Object)component : device.gameObject);
			var key = $"{entityId}:{deviceItem}";
			return new SceneSlot<TDevice>(device, deviceItem, new PinMameSpatialMatchCandidate(key, position.Value.x, position.Value.y));
		}

		private static Vector2? DevicePosition<TDevice>(TDevice device, PlayfieldComponent playfield)
			where TDevice : class, IMainComponent
		{
			if (device is LightGroupComponent lightGroup) {
				var lights = lightGroup.LightSources.Where(light => light).ToArray();
				if (lights.Length > 0) {
					var sum = Vector3.zero;
					foreach (var light in lights) sum += light.transform.position;
					return Normalize(sum / lights.Length, playfield);
				}
			}
			return Normalize(device.gameObject.transform.position, playfield);
		}

		private static IEnumerable<Vector2> AnchorPositions<TDevice>(TDevice device, PlayfieldComponent playfield)
			where TDevice : class, IMainComponent
		{
			if (device is LightGroupComponent lightGroup) {
				var childPositions = LeafLamps(lightGroup)
					.Select(lamp => Normalize(lamp.gameObject.transform.position, playfield))
					.Where(position => position.HasValue)
					.Select(position => position.Value)
					.ToArray();
				if (childPositions.Length > 0) return childPositions;
			}
			var position = DevicePosition(device, playfield);
			return position.HasValue ? new[] { position.Value } : Array.Empty<Vector2>();
		}

		private static IEnumerable<TDevice> CoveredDevices<TDevice>(TDevice device)
			where TDevice : class, IMainComponent
		{
			yield return device;
			if (device is not LightGroupComponent lightGroup) yield break;
			foreach (var lamp in LeafLamps(lightGroup).OfType<TDevice>()) yield return lamp;
		}

		private static IEnumerable<ILampDeviceComponent> LeafLamps(LightGroupComponent root)
		{
			var leaves = new List<ILampDeviceComponent>();
			var visited = new HashSet<LightGroupComponent>();
			CollectLeafLamps(root, visited, leaves);
			return leaves.Distinct();
		}

		private static void CollectLeafLamps(LightGroupComponent group, ISet<LightGroupComponent> visited,
			ICollection<ILampDeviceComponent> leaves)
		{
			if (!visited.Add(group)) return;
			foreach (var lamp in group.Lights) {
				if (lamp is LightGroupComponent childGroup) CollectLeafLamps(childGroup, visited, leaves);
				else leaves.Add(lamp);
			}
		}

		private static Vector2? Normalize(Vector3 worldPosition, PlayfieldComponent playfield)
		{
			var vpx = worldPosition.TranslateToVpx(playfield.transform);
			var x = (vpx.x - playfield.Left) / (playfield.Right - playfield.Left);
			var y = (vpx.y - playfield.Top) / (playfield.Bottom - playfield.Top);
			if (float.IsNaN(x) || float.IsNaN(y) || x < -0.05f || x > 1.05f || y < -0.05f || y > 1.05f) return null;
			return new Vector2(x, y);
		}

		private static string ResolveDeviceItem<TItem>(string configuredItem, IEnumerable<TItem> items)
			where TItem : IGamelogicEngineDeviceItem
		{
			var available = items.ToArray();
			if (!string.IsNullOrEmpty(configuredItem)) {
				return available.Any(item => item.Id == configuredItem) ? configuredItem : null;
			}
			return available.Length == 1 ? available[0].Id : null;
		}

		private static bool IsAlive<TDevice>(TDevice device) where TDevice : class
		{
			return device is MonoBehaviour behaviour ? behaviour : device != null;
		}

		private static double Distance(float x1, float y1, float x2, float y2)
		{
			var x = x1 - x2;
			var y = y1 - y2;
			return System.Math.Sqrt(x * x + y * y);
		}

		private sealed class IndexedPlacement
		{
			internal PinMameSpatialPlacement Placement { get; }
			internal string Key { get; }

			internal IndexedPlacement(PinMameSpatialPlacement placement, string key)
			{
				Placement = placement;
				Key = key;
			}
		}

		private sealed class SceneSlot<TDevice>
		{
			internal TDevice Device { get; }
			internal string DeviceItem { get; }
			internal PinMameSpatialMatchCandidate Match { get; }

			internal SceneSlot(TDevice device, string deviceItem, PinMameSpatialMatchCandidate match)
			{
				Device = device;
				DeviceItem = deviceItem;
				Match = match;
			}
		}
	}

	internal sealed class PinMameSpatialAssignmentResult
	{
		private readonly string _message;

		internal int PlacementCount { get; }
		internal int AssignedCount { get; }
		internal int AnchorCount { get; }
		internal int RejectedCount { get; }
		internal int UnrepresentedCount { get; }
		internal string[] Details { get; }

		internal bool Changed => AssignedCount > 0;

		internal PinMameSpatialAssignmentResult(int placementCount, int assignedCount, int anchorCount, int rejectedCount,
			int unrepresentedCount, string[] details, string message)
		{
			PlacementCount = placementCount;
			AssignedCount = assignedCount;
			AnchorCount = anchorCount;
			RejectedCount = rejectedCount;
			UnrepresentedCount = unrepresentedCount;
			Details = details ?? Array.Empty<string>();
			_message = message;
		}

		internal static PinMameSpatialAssignmentResult NoSpatialData()
			=> new(0, 0, 0, 0, 0, Array.Empty<string>(), "This definition has no curated playfield positions for that hardware type.");

		internal static PinMameSpatialAssignmentResult NoMappings()
			=> new(0, 0, 0, 0, 0, Array.Empty<string>(), "Populate Hardware before applying spatial assignments.");

		internal static PinMameSpatialAssignmentResult InvalidPlayfield()
			=> new(0, 0, 0, 0, 0, Array.Empty<string>(), "The table has no valid playfield bounds.");

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(_message)) return _message;
			var parts = new List<string> {
				$"Assigned {AssignedCount} of {PlacementCount} curated positions",
			};
			if (AnchorCount > 0) parts.Add($"preserved {AnchorCount} existing assignments as anchors");
			if (RejectedCount > 0) parts.Add($"left {RejectedCount} uncertain");
			if (UnrepresentedCount > 0) parts.Add($"skipped {UnrepresentedCount} without compatible mappings");
			var summary = string.Join("; ", parts) + ".";
			return Details.Length == 0 ? summary : $"{summary}\n{string.Join("\n", Details)}";
		}
	}
}
