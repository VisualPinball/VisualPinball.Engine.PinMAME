// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

namespace VisualPinball.Engine.PinMAME
{
	/// <summary>
	/// How libpinmame encodes output values for one hardware generation. The encoding is fixed per generation, so
	/// every output keeps one meaning from its first value (see pinmame/src/wpc/core.c and vpintf.c):
	/// <list type="bullet">
	/// <item>Lamps are 0/1, except on Pinball 2000 and SAM's modulated LEDs above lamp 80, which report 0..255.</item>
	/// <item>Solenoids report 0..255 where PinMAME forces PWM integration (SAM, Capcom, PinHeck, Pinball 2000), 0/1 elsewhere.</item>
	/// <item>GI strings report levels 0..8 on WPC and 0 or 9 elsewhere.</item>
	/// </list>
	/// </summary>
	public readonly struct PinMameOutputModel
	{
		/// <summary>Hardware generation bits, as in pinmame/src/wpc/gen.h.</summary>
		public const ulong GenAllWpc = 0xff, GenAllS80 = 0x0000600000000, GenSam = 0x0100000000000, GenP2K = 0x8000000000000,
			GenPinHeck = 0x10000000000000, GenCapcom = 0x20000000000000;

		/// <summary>Raw value of a fully on physical output.</summary>
		public const int PhysicalMaximum = 255;

		/// <summary>Highest SAM lamp number that is a plain matrix lamp; the modulated LEDs follow.</summary>
		public const int SamLastMatrixLamp = 80;

		private readonly ulong _generation;

		public PinMameOutputModel(ulong generation)
		{
			_generation = generation;
		}

		/// <summary>Whether the lamp reports physical brightness 0..255 rather than 0/1.</summary>
		public bool IsPhysicalLamp(int lampNo) => (_generation & GenP2K) != 0 || (_generation & GenSam) != 0 && lampNo > SamLastMatrixLamp;

		/// <summary>Whether solenoids report physical strength 0..255 rather than 0/1.</summary>
		public bool PhysicalSolenoids => (_generation & (GenSam | GenCapcom | GenPinHeck | GenP2K)) != 0;

		/// <summary>The GI level of a fully lit string.</summary>
		public int GiMaximum => (_generation & GenAllWpc) != 0 ? 8 : 9;

		/// <summary>Whether segment displays are upright (Gottlieb System 80) rather than slanted.</summary>
		public bool UprightSegments => (_generation & GenAllS80) != 0;
	}
}
