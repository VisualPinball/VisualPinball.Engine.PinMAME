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
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.Math;

namespace VisualPinball.Engine.PinMAME.Editor
{
	public static class PinMameGameDefinitionConverter
	{
		private const string ExpectedFormat = "pinmame-machine-definition";
		private static readonly FlipperWireRole[] FlipperWireRoles = {
			new("Lower Left Flipper", new[] { "flipper.lower.left", "flipper.lower.left.button", "flipper.lower-left.button", "flipper.left.button", "cabinet.flipper.left-button" }, "flipper.lower.left"),
			new("Lower Right Flipper", new[] { "flipper.lower.right", "flipper.lower.right.button", "flipper.lower-right.button", "flipper.right.button", "cabinet.flipper.right-button" }, "flipper.lower.right"),
			new("Upper Left Flipper", new[] { "flipper.upper.left", "flipper.upper.left.button" }, "flipper.upper.left"),
			new("Upper Right Flipper", new[] { "flipper.upper.right", "flipper.upper.right.button", "flipper.upper-right.button" }, "flipper.upper.right"),
		};

		// PinMAME's generic lower flipper switches (core.h, column CORE_FLIPPERSWCOL): 111/113 are
		// the end-of-stroke switches, 112/114 the cabinet buttons. The upper channels (115-118) are
		// left to the definition, since machines reuse them (second button optos, other switches).
		private const int FirstCoreFlipperSwitch = 111;
		private static readonly string[] CoreFlipperSwitchRoles = { "flipper.lower.right", "flipper.lower.left" };

		public static PinMameGameDefinition Convert(string json, string selectedRomId = null)
		{
			if (string.IsNullOrWhiteSpace(json)) {
				throw new InvalidDataException("The PinMAME machine definition is empty.");
			}

			var root = JObject.Parse(json);
			if (!string.Equals(root.Value<string>("format"), ExpectedFormat, StringComparison.Ordinal)) {
				throw new InvalidDataException($"Expected {ExpectedFormat}, got {root.Value<string>("format") ?? "no format"}.");
			}

			var schemaVersion = root.Value<int?>("schema_version");
			if (schemaVersion is not (1 or 2)) {
				throw new InvalidDataException($"Unsupported machine-definition schema version {schemaVersion?.ToString() ?? "none"}.");
			}

			var machine = RequiredObject(root, "machine");
			var machineId = RequiredString(machine, "id");
			var name = RequiredString(machine, "name");
			var coverage = RequiredObject(root, "coverage");
			var coverageStatus = RequiredString(coverage, "status");
			if (string.Equals(coverageStatus, "stub", StringComparison.Ordinal)) {
				throw new InvalidDataException($"{name} is a stub and has no hardware definition to populate.");
			}

			var completionScore = coverage.Value<int?>("completion_score") ?? (coverageStatus == "author_ready" ? 100 : 0);
			var sourceDrivers = (root["drivers"] as JArray)?.OfType<JObject>().ToArray() ?? Array.Empty<JObject>();
			var roms = ReadRoms(root, sourceDrivers);
			var defaultRomId = DefaultRomId(root, sourceDrivers, roms);
			var selectedDriver = SelectDriver(sourceDrivers, string.IsNullOrWhiteSpace(selectedRomId) ? defaultRomId : selectedRomId);
			var overrides = ReadOverrides(selectedDriver);
			var diagnostics = new List<string>();
			if (!string.IsNullOrWhiteSpace(selectedRomId) && selectedDriver == null
			    && roms.Any(rom => string.Equals(rom.Id, selectedRomId, StringComparison.OrdinalIgnoreCase))) {
				diagnostics.Add($"ROM {selectedRomId} is listed by PinMAME but has no curated driver override; the machine's base bindings were used.");
			}

			if (string.Equals(selectedDriver?.Value<string>("physical_compatibility"), "different", StringComparison.Ordinal)) {
				diagnostics.Add($"ROM {selectedRomId} is marked as physically different from this machine definition.");
			}

			var spatialSwitches = new List<PinMameSpatialPlacement>();
			var spatialCoils = new List<PinMameSpatialPlacement>();
			var spatialLamps = new List<PinMameSpatialPlacement>();
			var switches = ConvertSwitches(root["inputs"] as JArray, overrides, spatialSwitches, diagnostics);
			var coils = new List<GamelogicEngineCoil>();
			var lamps = new List<GamelogicEngineLamp>();
			ConvertOutputs(root["outputs"] as JArray, overrides, coils, lamps, spatialCoils, spatialLamps, diagnostics);
			var wires = ConvertFlipperWires(root["inputs"] as JArray, root["outputs"] as JArray, overrides, diagnostics);

			return new PinMameGameDefinition(machineId, name, coverageStatus, completionScore, roms, defaultRomId,
				switches.ToArray(), coils.ToArray(), lamps.ToArray(), wires, spatialSwitches.ToArray(),
				spatialCoils.ToArray(), spatialLamps.ToArray(), diagnostics.ToArray());
		}

		private static GamelogicEngineWire[] ConvertFlipperWires(JArray inputs, JArray outputs, IReadOnlyDictionary<string, JObject> overrides,
			ICollection<string> diagnostics)
		{
			if (inputs == null || outputs == null) {
				return Array.Empty<GamelogicEngineWire>();
			}

			var inputDevices = inputs.OfType<JObject>()
				.Where(IsAvailable)
				.Select(input => new { Device = input, Binding = EffectiveBinding(input, overrides) })
				.Where(item => string.Equals(item.Binding?.Value<string>("group"), "pinmame.input.switch", StringComparison.Ordinal))
				.Where(item => item.Binding?["device"]?.Type == JTokenType.Integer)
				.ToArray();
			var outputDevices = outputs.OfType<JObject>()
				.Where(IsAvailable)
				.Select(output => new { Device = output, Binding = EffectiveBinding(output, overrides) })
				.Where(item => string.Equals(item.Binding?.Value<string>("group"), "pinmame.output.solenoid", StringComparison.Ordinal))
				.Where(item => item.Binding?["device"]?.Type == JTokenType.Integer)
				// synthetic flipper coils (e.g. pre-Fliptronic WPC) are virtual
				.Where(item => item.Device.Value<string>("kind") is "coil" or "virtual")
				.ToArray();

			var wires = new List<GamelogicEngineWire>();
			var ambiguousSourceIds = new HashSet<string>(StringComparer.Ordinal);
			foreach (var role in FlipperWireRoles) {
				var sources = inputDevices
					.Where(item => MatchesFlipperInputRole(item.Device, item.Binding.Value<int>("device"), role))
					.ToArray();
				// PinMAME copies its generic flipper buttons (112/114) into the game's matrix flipper
				// switches, so when both are listed, the generic one is what the host drives.
				var coreSources = sources
					.Where(item => TryGetCoreFlipperSwitch(item.Device, item.Binding.Value<int>("device"), out _, out _))
					.ToArray();
				if (sources.Length > 1 && coreSources.Length == 1) {
					sources = coreSources;
				}
				if (sources.Length > 1) {
					diagnostics.Add($"The definition has ambiguous {role.Description} switch roles; its direct flipper wire was not imported.");
					continue;
				}
				if (sources.Length == 0) {
					continue;
				}
				var destinations = outputDevices
					.Where(item => string.Equals(FlipperRoleForOutput(item.Device), role.DestinationRole, StringComparison.Ordinal))
					.Where(item => !IsHoldFlipperOutput(item.Device))
					.ToArray();
				if (destinations.Length > 1) {
					diagnostics.Add($"The definition has ambiguous {role.Description} coil roles; its direct flipper wire was not imported.");
					continue;
				}
				if (destinations.Length == 0) {
					continue;
				}

				var source = sources[0];
				var destination = destinations[0];
				var sourceId = source.Binding.Value<int>("device").ToString(CultureInfo.InvariantCulture);
				var destinationId = destination.Binding.Value<int>("device").ToString(CultureInfo.InvariantCulture);
				if (ambiguousSourceIds.Contains(sourceId)) {
					continue;
				}
				if (wires.Any(wire => string.Equals(wire.SourceId, sourceId, StringComparison.Ordinal))) {
					wires.RemoveAll(wire => string.Equals(wire.SourceId, sourceId, StringComparison.Ordinal));
					ambiguousSourceIds.Add(sourceId);
					diagnostics.Add($"PinMAME switch {sourceId} represents multiple flipper buttons; its direct flipper wires were left for manual mapping.");
					continue;
				}
				wires.Add(new GamelogicEngineWire(sourceId, destinationId, DestinationType.Coil, role.Description));
			}
			return wires.ToArray();
		}

		private static List<GamelogicEngineSwitch> ConvertSwitches(JArray inputs, IReadOnlyDictionary<string, JObject> overrides,
			ICollection<PinMameSpatialPlacement> spatialSwitches, ICollection<string> diagnostics)
		{
			var switches = new List<GamelogicEngineSwitch>();
			var addresses = new HashSet<int>();
			var coreFlipperButtonIds = new HashSet<string>(StringComparer.Ordinal);
			if (inputs == null) {
				return switches;
			}
			var genericCoinSlots = inputs.OfType<JObject>()
				.Where(IsAvailable)
				.Where(input => (input["roles"] as JArray)?.Values<string>().Contains("cabinet.coin", StringComparer.Ordinal) == true)
				.Select(input => new { Input = input, Binding = EffectiveBinding(input, overrides) })
				.Where(item => string.Equals(item.Binding?.Value<string>("group"), "pinmame.input.switch", StringComparison.Ordinal) && item.Binding?["device"]?.Type == JTokenType.Integer)
				.Select(item => item.Binding.Value<int>("device"))
				.Distinct()
				.OrderBy(address => address)
				.Take(4)
				.Select((address, index) => new { address, slot = index + 1 })
				.ToDictionary(item => item.address, item => item.slot);

			foreach (var input in inputs.OfType<JObject>()) {
				if (!IsAvailable(input)) {
					continue;
				}

				var binding = EffectiveBinding(input, overrides);
				if (!string.Equals(binding?.Value<string>("group"), "pinmame.input.switch", StringComparison.Ordinal)) {
					continue;
				}

				var address = RequiredDevice(binding, input);
				if (!addresses.Add(address)) {
					diagnostics.Add($"Duplicate PinMAME switch address {address}; {input.Value<string>("id") ?? "the later switch"} was not imported.");
					continue;
				}

				var label = RequiredString(input, "label");
				var flipperRoleCount = FlipperWireRoles.Count(role => MatchesFlipperInputRole(input, address, role));
				var inputAction = flipperRoleCount > 1
					? string.Empty
					: InputAction(input, label, address, genericCoinSlots.TryGetValue(address, out var genericCoinSlot) ? genericCoinSlot : 0);
				if (TryGetCoreFlipperSwitch(input, address, out _, out var isCoreEos) && isCoreEos != IsEosByDefinition(input)) {
					diagnostics.Add($"PinMAME switch {address} ({label}) is a flipper {(isCoreEos ? "end-of-stroke switch" : "button")} in PinMAME's numbering; the definition says otherwise, PinMAME's numbering was used.");
				}
				if (flipperRoleCount > 1) {
					diagnostics.Add($"PinMAME switch {address} ({label}) represents multiple flipper buttons; its input action was left for manual mapping.");
				}
				var constantActive = input.Value<bool?>("constant_active");
				var @switch = new GamelogicEngineSwitch(address) {
					Description = label,
					NormallyClosed = IsNormallyClosedForHost(input),
					IsPulseSwitch = input.Value<bool?>("pulse") ?? false,
					InputActionHint = inputAction,
					InputMapHint = string.IsNullOrEmpty(inputAction) ? string.Empty : InputConstants.MapCabinetSwitches,
					ConstantHint = constantActive.HasValue
						? constantActive.Value ? SwitchConstantHint.AlwaysClosed : SwitchConstantHint.AlwaysOpen
						: SwitchConstantHint.None,
				};
				if (TryGetCoreFlipperSwitch(input, address, out _, out var isCoreFlipperEos) && !isCoreFlipperEos) {
					coreFlipperButtonIds.Add(@switch.Id);
				}
				ApplySwitchDeviceHint(@switch, input);
				switches.Add(@switch);
				AddSpatialPlacements(spatialSwitches, input, @switch.Id, label);
			}

			var orderedSwitches = switches.OrderBy(@switch => NumericId(@switch.Id)).ToList();
			var inputActions = new HashSet<string>(StringComparer.Ordinal);
			// generic flipper buttons first: the matrix flipper switches only mirror them
			var actionClaimOrder = orderedSwitches
				.Where(@switch => !string.IsNullOrEmpty(@switch.InputActionHint))
				.OrderBy(@switch => coreFlipperButtonIds.Contains(@switch.Id) ? 0 : 1)
				.ToArray();
			foreach (var @switch in actionClaimOrder) {
				if (inputActions.Add(@switch.InputActionHint)) {
					continue;
				}

				diagnostics.Add($"Duplicate input action {@switch.InputActionHint}; switch {@switch.Id} ({@switch.Description}) was left available for manual mapping.");
				@switch.InputActionHint = string.Empty;
				@switch.InputMapHint = string.Empty;
			}
			return orderedSwitches;
		}

		private static void ConvertOutputs(JArray outputs, IReadOnlyDictionary<string, JObject> overrides,
			ICollection<GamelogicEngineCoil> coils, ICollection<GamelogicEngineLamp> lamps,
			ICollection<PinMameSpatialPlacement> spatialCoils, ICollection<PinMameSpatialPlacement> spatialLamps,
			ICollection<string> diagnostics)
		{
			if (outputs == null) {
				return;
			}

			var addresses = new HashSet<string>(StringComparer.Ordinal);
			foreach (var output in outputs.OfType<JObject>()) {
				if (!IsAvailable(output)) {
					continue;
				}
				var kind = RequiredString(output, "kind");

				var binding = EffectiveBinding(output, overrides);
				var group = binding?.Value<string>("group");
				if (group is not ("pinmame.output.solenoid" or "pinmame.output.lamp" or "pinmame.output.gi")) {
					continue;
				}

				var address = RequiredDevice(binding, output);
				var channel = binding.Value<int?>("channel") ?? -1;
				if (!addresses.Add($"{group}:{address}:{channel}")) {
					diagnostics.Add($"Duplicate PinMAME output binding {group} address {address}, channel {channel}; {output.Value<string>("id") ?? "the later output"} was not imported.");
					continue;
				}

				var label = RequiredString(output, "label");
				if (group == "pinmame.output.solenoid") {
					var coil = new GamelogicEngineCoil(address) {
						Description = label,
						IsLamp = kind is "flasher" or "lamp" or "rgb_lamp" or "gi",
					};
					ApplyCoilDeviceHint(coil, output, kind, address);
					coils.Add(coil);
					if (coil.IsLamp) {
						AddSpatialPlacements(spatialLamps, output, coil.Id, label, LampSource.Lamp, ColorChannel.Alpha, true);
					} else {
						AddSpatialPlacements(spatialCoils, output, coil.Id, label);
					}
					if (kind is "rgb_lamp" or "gi") {
						diagnostics.Add($"{label} uses the solenoid callback as {kind}; VPE currently imports it as a single-channel lamp output.");
					}
					continue;
				}

				if (kind is "coil" or "motor" or "servo" or "magnet" or "relay" or "control_signal") {
					diagnostics.Add($"{label} is a {kind} carried by {group}; VPE has no coil destination driven by lamp callbacks, so it was not imported.");
					continue;
				}

				var isRgbChannel = TryColorChannelFor(output, out var colorChannel);
				if (kind == "rgb_lamp" && !isRgbChannel) {
					diagnostics.Add($"RGB output {output.Value<string>("id") ?? label} does not identify its red, green, or blue channel and was not imported.");
					continue;
				}
				if (!isRgbChannel) colorChannel = ColorChannel.Alpha;
				// VPE needs the raw value at maximum intensity, while the catalog's `steps` is the
				// inclusive count of distinct values. PinMAME GI defaults to native levels 0..8.
				var range = output["range"] as JObject;
				var maximum = range?.Value<int?>("maximum") ?? (group == "pinmame.output.gi" ? 8 : 0);
				var steps = range?.Value<int?>("steps") ?? (group == "pinmame.output.gi" ? 9 : 0);
				var fadingSteps = maximum > 0 ? maximum : steps > 1 ? steps - 1 : 0;
				var lamp = new GamelogicEngineLamp(address) {
					Description = label,
					Channel = colorChannel,
					FadingSteps = fadingSteps,
					Source = group == "pinmame.output.gi" ? LampSource.GI : LampSource.Lamp,
					Type = isRgbChannel ? LampType.RgbMulti : (fadingSteps > 1 ? LampType.SingleFading : LampType.SingleOnOff),
				};
				lamps.Add(lamp);
				AddSpatialPlacements(spatialLamps, output, lamp.Id, label, lamp.Source, lamp.Channel);
			}
		}

		private static void AddSpatialPlacements(ICollection<PinMameSpatialPlacement> destination, JObject device,
			string hardwareId, string label, LampSource source = LampSource.Lamp, ColorChannel channel = ColorChannel.Alpha,
			bool isCoil = false)
		{
			if (device["spatial"]?["placements"] is not JArray placements) {
				return;
			}

			var deviceId = device.Value<string>("id") ?? hardwareId;
			var placementIndex = 0;
			foreach (var placement in placements.OfType<JObject>()) {
				if (!string.Equals(placement.Value<string>("space"), "playfield", StringComparison.Ordinal)) {
					continue;
				}
				var x = placement.Value<float?>("x");
				var y = placement.Value<float?>("y");
				if (!x.HasValue || !y.HasValue || x.Value < 0f || x.Value > 1f || y.Value < 0f || y.Value > 1f) {
					continue;
				}
				var placementId = placement.Value<string>("id");
				if (string.IsNullOrWhiteSpace(placementId)) {
					placementId = $"{deviceId}:{placementIndex}";
				}
				destination.Add(new PinMameSpatialPlacement(hardwareId, placementId, label, x.Value, y.Value, source, channel, isCoil));
				placementIndex++;
			}
		}

		private static PinMameRomDefinition[] ReadRoms(JObject root, IEnumerable<JObject> sourceDrivers)
		{
			var roms = new Dictionary<string, PinMameRomDefinition>(StringComparer.OrdinalIgnoreCase);
			foreach (var driver in sourceDrivers) {
				AddRom(roms, driver);
			}
			if (root["catalogDrivers"] is JArray catalogDrivers) {
				foreach (var driver in catalogDrivers.OfType<JObject>()) {
					AddRom(roms, driver);
				}
			}
			return roms.Values.OrderBy(rom => rom.Id, StringComparer.OrdinalIgnoreCase).ToArray();
		}

		/// <summary>
		/// The parent ROM: the listed driver that isn't a clone, or else the one most clones point to.
		/// The list itself is alphabetical, so its first entry is often a prototype or a homebrew fix.
		/// </summary>
		private static string DefaultRomId(JObject root, IEnumerable<JObject> sourceDrivers, PinMameRomDefinition[] roms)
		{
			var drivers = sourceDrivers
				.Concat((root["catalogDrivers"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
				.ToArray();
			string Listed(string id) => roms.FirstOrDefault(rom => string.Equals(rom.Id, id, StringComparison.OrdinalIgnoreCase))?.Id;

			var parent = drivers
				.Where(driver => string.IsNullOrWhiteSpace(driver.Value<string>("clone_of")))
				.Select(driver => Listed(driver.Value<string>("id")))
				.FirstOrDefault(id => id != null);
			parent ??= drivers
				.Select(driver => Listed(driver.Value<string>("root_driver") ?? driver.Value<string>("clone_of")))
				.Where(id => id != null)
				.GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
				.OrderByDescending(group => group.Count())
				.Select(group => group.Key)
				.FirstOrDefault();
			return parent ?? roms.FirstOrDefault()?.Id;
		}

		private static void AddRom(IDictionary<string, PinMameRomDefinition> roms, JObject driver)
		{
			var id = driver.Value<string>("id");
			if (string.IsNullOrWhiteSpace(id)) {
				return;
			}
			var description = driver.Value<string>("description") ?? string.Empty;
			roms[id] = new PinMameRomDefinition(id, description);
		}

		private static JObject SelectDriver(IEnumerable<JObject> drivers, string selectedRomId)
		{
			var driverList = drivers.ToArray();
			if (!string.IsNullOrWhiteSpace(selectedRomId)) {
				return driverList.FirstOrDefault(driver => string.Equals(driver.Value<string>("id"), selectedRomId, StringComparison.OrdinalIgnoreCase));
			}
			return driverList.FirstOrDefault();
		}

		private static IReadOnlyDictionary<string, JObject> ReadOverrides(JObject driver)
		{
			var overrides = new Dictionary<string, JObject>(StringComparer.Ordinal);
			if (driver?["overrides"] is not JArray overrideArray) {
				return overrides;
			}
			foreach (var item in overrideArray.OfType<JObject>()) {
				var target = item.Value<string>("target");
				if (!string.IsNullOrWhiteSpace(target) && item["binding"] is JObject binding) {
					overrides[target] = binding;
				}
			}
			return overrides;
		}

		private static JObject EffectiveBinding(JObject device, IReadOnlyDictionary<string, JObject> overrides)
		{
			var id = device.Value<string>("id");
			return id != null && overrides.TryGetValue(id, out var binding) ? binding : device["binding"] as JObject;
		}

		private static bool IsAvailable(JObject device)
		{
			if (string.Equals(device.Value<string>("availability"), "unused", StringComparison.Ordinal)) {
				return false;
			}
			if (!string.Equals(device.Value<string>("kind"), "virtual", StringComparison.Ordinal)) {
				return true;
			}
			// "virtual" is independent of availability. A virtual item with a public role is still
			// driven by or drives the host (e.g. a flipper button outside the switch matrix, or a
			// synthetic flipper coil). Without one, virtual inputs are host inputs too, while
			// virtual outputs are mirrors of other outputs or internal state.
			var roles = (device["roles"] as JArray)?.Values<string>().Where(role => role != null).ToArray() ?? Array.Empty<string>();
			if (roles.Any(role => !role.StartsWith("internal.", StringComparison.Ordinal))) {
				return true;
			}
			return roles.Length == 0 && (device["binding"] as JObject)?.Value<string>("group")?.StartsWith("pinmame.input.", StringComparison.Ordinal) == true;
		}

		/// <summary>
		/// The definition's <c>normally_closed</c> is the physical contact (e.g. optos). PinMAME
		/// already inverts those itself (per-game <c>invSw</c>), so passing it on would invert them
		/// twice. Only the coin door switch is closed at rest from the host's point of view.
		/// </summary>
		private static bool IsNormallyClosedForHost(JObject input)
		{
			return HasRole(input, "cabinet.coin-door") && (input.Value<bool?>("normally_closed") ?? false);
		}

		private static bool HasRole(JObject device, params string[] roles)
		{
			var deviceRoles = (device["roles"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>();
			return deviceRoles.Intersect(roles, StringComparer.Ordinal).Any();
		}

		private static int RequiredDevice(JObject binding, JObject device)
		{
			if (binding?["device"]?.Type != JTokenType.Integer) {
				throw new InvalidDataException($"{device.Value<string>("id") ?? "A device"} has no integer PinMAME address.");
			}
			return binding.Value<int>("device");
		}

		private static string InputAction(JObject input, string label, int address, int genericCoinSlot)
		{
			if (TryGetCoreFlipperSwitch(input, address, out var coreFlipperRole, out var isCoreEos)) {
				return isCoreEos ? string.Empty : FlipperAction(coreFlipperRole);
			}
			var roles = input["roles"] as JArray;
			if (roles == null) {
				return string.Empty;
			}
			var values = new HashSet<string>(roles.Values<string>(), StringComparer.Ordinal);
			if (values.Contains("cabinet.start-or-tournament")) return InputConstants.ActionStartGame;
			if (values.Contains("cabinet.start") && !Contains(label, "tournament")) return InputConstants.ActionStartGame;
			if (HasAny(values, "cabinet.launch", "cabinet.shooter")) return InputConstants.ActionPlunger;
			if (HasAny(values, "cabinet.buy-in")) return InputConstants.ActionFrontBuyIn;
			if (HasAny(values, "cabinet.grip-trigger")) return InputConstants.ActionPlunger;
			if (HasAny(values, "cabinet.action", "cabinet.fire", "cabinet.fire-button")) return InputConstants.ActionFire1;
			if (HasAny(values, "cabinet.coin.1", "cabinet.coin.left")) return InputConstants.ActionInsertCoin1;
			if (HasAny(values, "cabinet.coin.2", "cabinet.coin.center", "cabinet.coin.middle")) return InputConstants.ActionInsertCoin2;
			if (HasAny(values, "cabinet.coin.3", "cabinet.coin.right")) return InputConstants.ActionInsertCoin3;
			if (HasAny(values, "cabinet.coin.4", "cabinet.coin.fourth")) return InputConstants.ActionInsertCoin4;
			if (values.Contains("cabinet.coin")) {
				var semanticCoinSlot = CoinSlotFromLabel(label);
				var coinSlot = semanticCoinSlot > 0 ? semanticCoinSlot : semanticCoinSlot < 0 ? 0 : genericCoinSlot;
				switch (coinSlot) {
					case 1: return InputConstants.ActionInsertCoin1;
					case 2: return InputConstants.ActionInsertCoin2;
					case 3: return InputConstants.ActionInsertCoin3;
					case 4: return InputConstants.ActionInsertCoin4;
				}
			}
			if (HasAny(values, "cabinet.coin-door")) return InputConstants.ActionCoinDoorOpenClose;
			if (HasAny(values, "cabinet.slam-tilt")) return InputConstants.ActionSlamTilt;
			if (HasAny(values, "cabinet.tilt")) {
				if (Contains(label, "slam")) return InputConstants.ActionSlamTilt;
				if (Contains(label, "ball roll") || Contains(label, "playfield tilt")) return string.Empty;
				// Keep compatibility with the VisualPinball.Engine preview used by the standalone
				// wrapper build; that release predates InputConstants.ActionTilt.
				return "Tilt";
			}
			if (!IsEosFlipperInput(input)) {
				if (HasAny(values, "flipper.lower.left", "flipper.lower.left.button", "flipper.lower-left.button", "flipper.left.button", "cabinet.flipper.left-button")) return InputConstants.ActionLeftFlipper;
				if (HasAny(values, "flipper.lower.right", "flipper.lower.right.button", "flipper.lower-right.button", "flipper.right.button", "cabinet.flipper.right-button")) return InputConstants.ActionRightFlipper;
				if (HasAny(values, "flipper.upper.left", "flipper.upper.left.button")) return InputConstants.ActionUpperLeftFlipper;
				if (HasAny(values, "flipper.upper.right", "flipper.upper.right.button", "flipper.upper-right.button")) return InputConstants.ActionUpperRightFlipper;
				if (HasAny(values, "cabinet.flipper", "cabinet.flipper.button")) {
					return FlipperAction(FlipperRoleFromIdentity(input.Value<string>("id")) ?? FlipperRoleFromIdentity(label));
				}
			}
			if (HasAny(values, "service.escape", "service.cancel")) return InputConstants.ActionCoinDoorCancel;
			if (HasAny(values, "service.down")) return InputConstants.ActionCoinDoorDown;
			if (HasAny(values, "service.up")) return InputConstants.ActionCoinDoorUp;
			if (HasAny(values, "service.enter")) return InputConstants.ActionCoinDoorEnter;
			if (HasAny(values, "service.back", "cabinet.service.back")) return InputConstants.ActionCoinDoorBack;
			if (HasAny(values, "cabinet.service.minus")) return InputConstants.ActionCoinDoorMinus;
			if (HasAny(values, "cabinet.service.plus")) return InputConstants.ActionCoinDoorPlus;
			if (HasAny(values, "cabinet.service.select")) return InputConstants.ActionCoinDoorSelect;
			if (HasAny(values, "service.advance", "service.adjust")) return InputConstants.ActionCoinDoorAdvance;
			if (HasAny(values, "service.updown")) return InputConstants.ActionCoinDoorUpDown;
			if (HasAny(values, "service.self-test", "service.test", "service.diagnostic")) return InputConstants.ActionSelfTest;
			if (HasAny(values, "cabinet.service", "service.button")) return ServiceActionFromLabel(label);
			return string.Empty;
		}

		private static int CoinSlotFromLabel(string label)
		{
			var identity = NormalizeIdentity(label);
			var words = new HashSet<string>(identity.Split(new[] { ' ', '(', ')', '/', '&' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
			if (words.Contains("left")) return 1;
			if (words.Contains("center") || words.Contains("centre") || words.Contains("middle") || words.Contains("dba")) return 2;
			if (words.Contains("right")) return 3;
			if (words.Contains("fourth") || words.Contains("4th") || words.Contains("4") || words.Contains("iv")) return 4;
			if (words.Contains("fifth") || words.Contains("5th") || words.Contains("5") || words.Contains("v")
			    || words.Contains("sixth") || words.Contains("6th") || words.Contains("6") || words.Contains("vi")) return -1;
			if (words.Contains("first") || words.Contains("1st") || words.Contains("1") || words.Contains("i")) return 1;
			if (words.Contains("second") || words.Contains("2nd") || words.Contains("2") || words.Contains("ii")) return 2;
			if (words.Contains("third") || words.Contains("3rd") || words.Contains("3") || words.Contains("iii")) return 3;
			return 0;
		}

		private static string ServiceActionFromLabel(string label)
		{
			if (Contains(label, "escape") || Contains(label, "cancel")) return InputConstants.ActionCoinDoorCancel;
			if (Contains(label, "down")) return InputConstants.ActionCoinDoorDown;
			if (Contains(label, "up")) return InputConstants.ActionCoinDoorUp;
			if (Contains(label, "enter")) return InputConstants.ActionCoinDoorEnter;
			if (Contains(label, "back")) return InputConstants.ActionCoinDoorBack;
			if (Contains(label, "minus")) return InputConstants.ActionCoinDoorMinus;
			if (Contains(label, "plus")) return InputConstants.ActionCoinDoorPlus;
			if (Contains(label, "select")) return InputConstants.ActionCoinDoorSelect;
			return string.Empty;
		}

		private static bool Contains(string value, string part) => value?.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

		private static bool HasAny(ISet<string> values, params string[] candidates) => candidates.Any(values.Contains);

		private static bool MatchesFlipperInputRole(JObject input, int address, FlipperWireRole role)
		{
			if (TryGetCoreFlipperSwitch(input, address, out var coreFlipperRole, out var isCoreEos)) {
				return !isCoreEos && string.Equals(coreFlipperRole, role.DestinationRole, StringComparison.Ordinal);
			}
			if (IsEosFlipperInput(input)) return false;
			if (HasRole(input, role.SourceRoles)) return true;
			if (!HasRole(input, "cabinet.flipper", "cabinet.flipper.button")) return false;
			var inferredRole = FlipperRoleFromIdentity(input.Value<string>("id")) ?? FlipperRoleFromIdentity(input.Value<string>("label"));
			return string.Equals(inferredRole, role.DestinationRole, StringComparison.Ordinal);
		}

		/// <summary>
		/// Whether the input is one of PinMAME's generic lower flipper switches (111-114), and which
		/// flipper it belongs to. Their meaning is fixed by PinMAME, whatever the definition's
		/// label says. Only inputs the definition describes as flipper switches qualify.
		/// </summary>
		private static bool TryGetCoreFlipperSwitch(JObject input, int address, out string flipperRole, out bool isEos)
		{
			flipperRole = null;
			isEos = false;
			var offset = address - FirstCoreFlipperSwitch;
			if (offset < 0 || offset >= CoreFlipperSwitchRoles.Length * 2) {
				return false;
			}
			var roles = (input["roles"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>();
			if (!roles.Any(role => Contains(role, "flipper")) && !Contains(input.Value<string>("label"), "flipper")) {
				return false;
			}
			flipperRole = CoreFlipperSwitchRoles[offset / 2];
			isEos = offset % 2 == 0;
			return true;
		}

		private static bool IsEosByDefinition(JObject input)
		{
			var roles = (input["roles"] as JArray)?.Values<string>() ?? Enumerable.Empty<string>();
			return IsEosFlipperInput(input) || roles.Any(role => role != null && role.EndsWith(".eos", StringComparison.Ordinal));
		}

		private static bool IsEosFlipperInput(JObject input)
		{
			var identity = NormalizeIdentity($"{input.Value<string>("id")} {input.Value<string>("label")}");
			return identity.Contains("end of stroke") || identity.Contains("eos") || identity.Contains("e o s");
		}

		private static string FlipperAction(string role)
		{
			return role switch {
				"flipper.lower.left" => InputConstants.ActionLeftFlipper,
				"flipper.lower.right" => InputConstants.ActionRightFlipper,
				"flipper.upper.left" => InputConstants.ActionUpperLeftFlipper,
				"flipper.upper.right" => InputConstants.ActionUpperRightFlipper,
				_ => string.Empty,
			};
		}

		private static string FlipperRoleForOutput(JObject output)
		{
			if (HasRole(output, "flipper.left.coil")) return "flipper.lower.left";
			if (HasRole(output, "flipper.right.coil")) return "flipper.lower.right";
			foreach (var role in FlipperWireRoles) {
				if (HasRole(output, role.DestinationRole)) {
					return role.DestinationRole;
				}
			}

			return FlipperRoleFromIdentity(output.Value<string>("id")) ?? FlipperRoleFromIdentity(output.Value<string>("label"));
		}

		private static string FlipperRoleFromIdentity(string value)
		{
			var identity = NormalizeIdentity(value);
			if (!identity.Contains("flipper")) return null;
			var nonCabinetFlipper = new[] { "backbox", "underplayfield", "spinning", "diverter", "relay", "mini", "top", "upf", "unused", "generic" };
			if (nonCabinetFlipper.Any(identity.Contains)) return null;
			var level = identity.Contains("upper") ? "upper" : identity.Contains("lower") ? "lower" : null;
			var side = identity.Contains("left") ? "left" : identity.Contains("right") ? "right" : null;
			if (level == null && side != null) {
				level = "lower";
			}
			return level != null && side != null ? $"flipper.{level}.{side}" : null;
		}

		private static string NormalizeIdentity(string value) => (value ?? string.Empty).ToLowerInvariant().Replace('.', ' ').Replace('-', ' ').Replace('_', ' ');

		private static bool IsHoldFlipperOutput(JObject output)
		{
			var identity = $"{output.Value<string>("id")} {output.Value<string>("label")}";
			return identity.IndexOf("hold", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static void ApplySwitchDeviceHint(GamelogicEngineSwitch @switch, JObject input)
		{
			var label = @switch.Description ?? string.Empty;
			if (HasRole(input, "internal.trough")) {
				@switch.DeviceHint = "^Trough\\s*\\d?$";
				if (string.Equals(label, "Trough Left", StringComparison.OrdinalIgnoreCase)) @switch.DeviceItemHint = "3";
				else if (string.Equals(label, "Trough Center", StringComparison.OrdinalIgnoreCase) || string.Equals(label, "Trough Middle", StringComparison.OrdinalIgnoreCase)) @switch.DeviceItemHint = "2";
				else if (string.Equals(label, "Trough Right", StringComparison.OrdinalIgnoreCase)) @switch.DeviceItemHint = "1";
				return;
			}

			if (label.IndexOf("outhole", StringComparison.OrdinalIgnoreCase) >= 0 || label.IndexOf("drain", StringComparison.OrdinalIgnoreCase) >= 0) {
				@switch.DeviceHint = "^Trough\\s*\\d?$";
				@switch.DeviceItemHint = "drain_switch";
			}
		}

		private static void ApplyCoilDeviceHint(GamelogicEngineCoil coil, JObject output, string kind, int address)
		{
			if (kind == "flasher") {
				coil.DeviceHint = $"^F1?{address}$";
				return;
			}

			var semanticId = output.Value<string>("id") ?? string.Empty;
			if (string.Equals(semanticId, "device.outhole", StringComparison.Ordinal)) {
				coil.DeviceHint = "^Trough\\s*\\d?$";
				coil.DeviceItemHint = "entry_coil";
				return;
			}
			if (string.Equals(semanticId, "device.trough", StringComparison.Ordinal)) {
				coil.DeviceHint = "^Trough\\s*\\d?$";
				coil.DeviceItemHint = "eject_coil";
				return;
			}
			if (string.Equals(semanticId, "device.plunger", StringComparison.Ordinal)) {
				coil.DeviceHint = "^Plunger\\d*$";
				return;
			}

			var flipperRole = FlipperRoleForOutput(output);
			if (flipperRole == null) return;
			var isLeft = flipperRole.EndsWith(".left", StringComparison.Ordinal);
			var isLower = flipperRole.Contains(".lower.");
			coil.DeviceHint = isLeft
				? isLower ? "^((Lower)?LeftFlipper|LFlipper|FlipperLeft|FlipperL)\\d*$" : "^(UpperLeftFlipper|ULFlipper|FlipperUpperLeft)\\d*$"
				: isLower ? "^((Lower)?RightFlipper|RFlipper|FlipperRight|FlipperR)\\d*$" : "^(UpperRightFlipper|URFlipper|FlipperUpperRight)\\d*$";
			coil.DeviceItemHint = IsHoldFlipperOutput(output) ? "hold_coil" : "main_coil";
		}

		private static bool TryColorChannelFor(JObject output, out ColorChannel channel)
		{
			var id = (output.Value<string>("id") ?? string.Empty).ToLowerInvariant();
			var label = (output.Value<string>("label") ?? string.Empty).ToLowerInvariant();
			if (id.EndsWith(".red", StringComparison.Ordinal) || id.EndsWith("-red", StringComparison.Ordinal) || label.Contains("(red)")) channel = ColorChannel.Red;
			else if (id.EndsWith(".green", StringComparison.Ordinal) || id.EndsWith("-green", StringComparison.Ordinal) || label.Contains("(green)")) channel = ColorChannel.Green;
			else if (id.EndsWith(".blue", StringComparison.Ordinal) || id.EndsWith("-blue", StringComparison.Ordinal) || label.Contains("(blue)")) channel = ColorChannel.Blue;
			else {
				channel = ColorChannel.Alpha;
				return false;
			}
			return true;
		}

		private static JObject RequiredObject(JObject parent, string property)
		{
			return parent[property] as JObject ?? throw new InvalidDataException($"The definition has no {property} object.");
		}

		private static string RequiredString(JObject parent, string property)
		{
			var value = parent.Value<string>(property);
			return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidDataException($"The definition has no {property} value.");
		}

		private static int NumericId(string id) => int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : int.MaxValue;

		private sealed class FlipperWireRole
		{
			internal string Description { get; }
			internal string[] SourceRoles { get; }
			internal string DestinationRole { get; }

			internal FlipperWireRole(string description, string[] sourceRoles, string destinationRole)
			{
				Description = description;
				SourceRoles = sourceRoles;
				DestinationRole = destinationRole;
			}
		}
	}
}
