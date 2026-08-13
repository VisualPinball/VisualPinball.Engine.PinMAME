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
		public string[] Diagnostics { get; }

		public PinMameGameDefinition(string machineId, string name, string coverageStatus, int completionScore,
			PinMameRomDefinition[] roms, GamelogicEngineSwitch[] switches, GamelogicEngineCoil[] coils,
			GamelogicEngineLamp[] lamps, GamelogicEngineWire[] wires, string[] diagnostics)
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
			Diagnostics = diagnostics ?? Array.Empty<string>();
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
