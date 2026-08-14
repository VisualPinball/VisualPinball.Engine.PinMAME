// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

using System;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.Math;

namespace VisualPinball.Engine.PinMAME.Editor
{
	public sealed class PinMameGameDefinition
	{
		public string MachineId { get; }
		public string Name { get; }
		public string CoverageStatus { get; }
		public int CompletionScore { get; }
		public PinMameRomDefinition[] Roms { get; }
		public GamelogicEngineSwitch[] Switches { get; }
		public GamelogicEngineCoil[] Coils { get; }
		public GamelogicEngineLamp[] Lamps { get; }
		public GamelogicEngineWire[] Wires { get; }
		public PinMameSpatialPlacement[] SpatialSwitches { get; }
		public PinMameSpatialPlacement[] SpatialCoils { get; }
		public PinMameSpatialPlacement[] SpatialLamps { get; }
		public string[] Diagnostics { get; }

		public PinMameGameDefinition(string machineId, string name, string coverageStatus, int completionScore,
			PinMameRomDefinition[] roms, GamelogicEngineSwitch[] switches, GamelogicEngineCoil[] coils,
			GamelogicEngineLamp[] lamps, GamelogicEngineWire[] wires, PinMameSpatialPlacement[] spatialSwitches,
			PinMameSpatialPlacement[] spatialCoils, PinMameSpatialPlacement[] spatialLamps, string[] diagnostics)
		{
			MachineId = machineId;
			Name = name;
			CoverageStatus = coverageStatus;
			CompletionScore = completionScore;
			Roms = roms ?? Array.Empty<PinMameRomDefinition>();
			Switches = switches ?? Array.Empty<GamelogicEngineSwitch>();
			Coils = coils ?? Array.Empty<GamelogicEngineCoil>();
			Lamps = lamps ?? Array.Empty<GamelogicEngineLamp>();
			Wires = wires ?? Array.Empty<GamelogicEngineWire>();
			SpatialSwitches = spatialSwitches ?? Array.Empty<PinMameSpatialPlacement>();
			SpatialCoils = spatialCoils ?? Array.Empty<PinMameSpatialPlacement>();
			SpatialLamps = spatialLamps ?? Array.Empty<PinMameSpatialPlacement>();
			Diagnostics = diagnostics ?? Array.Empty<string>();
		}
	}

	public sealed class PinMameSpatialPlacement
	{
		public string HardwareId { get; }
		public string PlacementId { get; }
		public string Label { get; }
		public float X { get; }
		public float Y { get; }
		public LampSource Source { get; }
		public ColorChannel Channel { get; }
		public bool IsCoil { get; }

		public PinMameSpatialPlacement(string hardwareId, string placementId, string label, float x, float y,
			LampSource source = LampSource.Lamp, ColorChannel channel = ColorChannel.Alpha, bool isCoil = false)
		{
			HardwareId = hardwareId;
			PlacementId = placementId;
			Label = label;
			X = x;
			Y = y;
			Source = source;
			Channel = channel;
			IsCoil = isCoil;
		}
	}

	public sealed class PinMameRomDefinition
	{
		public string Id { get; }
		public string Description { get; }

		public PinMameRomDefinition(string id, string description)
		{
			Id = id;
			Description = description;
		}

		public override string ToString() => string.IsNullOrWhiteSpace(Description) ? Id : $"{Id} — {Description}";
	}
}
