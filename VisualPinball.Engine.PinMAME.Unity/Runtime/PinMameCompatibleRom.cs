// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

using System;

namespace VisualPinball.Engine.PinMAME
{
	/// <summary>
	/// How closely a ROM matches the physical machine a table was made for, as the machine catalog
	/// at games.visualpinball.org tells.
	/// </summary>
	public enum PinMameRomCompatibility
	{
		/// <summary>
		/// Another revision of the same game: switches, coils, lamps and displays are wired the same.
		/// </summary>
		Identical = 0,

		/// <summary>
		/// Runs on the same machine, but isn't just a revision of the table's ROM, e.g. a community
		/// fix or alternative firmware.
		/// </summary>
		Compatible = 1,

		/// <summary>
		/// A ROM of the same machine that nobody has confirmed runs it the same way.
		/// </summary>
		Unverified = 2,
	}

	/// <summary>
	/// A ROM that can run a table in place of the one its author chose.
	/// </summary>
	[Serializable]
	public struct PinMameCompatibleRom
	{
		public string Id;
		public string Description;
		public PinMameRomCompatibility Compatibility;

		public PinMameCompatibleRom(string id, string description, PinMameRomCompatibility compatibility)
		{
			Id = id;
			Description = description ?? string.Empty;
			Compatibility = compatibility;
		}

		public override string ToString() => string.IsNullOrWhiteSpace(Description) ? Id : $"{Id} ({Description})";
	}
}
