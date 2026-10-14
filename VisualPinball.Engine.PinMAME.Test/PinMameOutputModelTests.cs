// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using NUnit.Framework;

namespace VisualPinball.Engine.PinMAME
{
	[TestFixture]
	public class PinMameOutputModelTests
	{
		// Single generations from pinmame/src/wpc/gen.h.
		private const ulong WpcDmd = 0x04, Wpc95 = 0x80, Whitestar = 0x0004000000000, Gts80 = 0x0000200000000, Gts80B = 0x0000400000000;

		[Test]
		public void SamLampsAreBinaryUpToEightyAndPhysicalAbove()
		{
			var model = new PinMameOutputModel(PinMameOutputModel.GenSam);
			Assert.That(model.IsPhysicalLamp(1), Is.False);
			Assert.That(model.IsPhysicalLamp(80), Is.False);
			Assert.That(model.IsPhysicalLamp(81), Is.True);
			Assert.That(model.IsPhysicalLamp(128), Is.True);
		}

		[Test]
		public void Pinball2000LampsAreAllPhysical()
		{
			var model = new PinMameOutputModel(PinMameOutputModel.GenP2K);
			Assert.That(model.IsPhysicalLamp(1), Is.True);
			Assert.That(model.IsPhysicalLamp(80), Is.True);
		}

		[TestCase(WpcDmd)]
		[TestCase(Wpc95)]
		[TestCase(Whitestar)]
		[TestCase(Gts80)]
		public void OtherLampsAreBinary(ulong generation)
		{
			var model = new PinMameOutputModel(generation);
			Assert.That(model.IsPhysicalLamp(1), Is.False);
			Assert.That(model.IsPhysicalLamp(81), Is.False);
		}

		[TestCase(PinMameOutputModel.GenSam, true)]
		[TestCase(PinMameOutputModel.GenCapcom, true)]
		[TestCase(PinMameOutputModel.GenPinHeck, true)]
		[TestCase(PinMameOutputModel.GenP2K, true)]
		[TestCase(WpcDmd, false)]
		[TestCase(Wpc95, false)]
		[TestCase(Whitestar, false)]
		[TestCase(Gts80, false)]
		public void SolenoidsArePhysicalWherePwmIntegrationIsForced(ulong generation, bool physical)
		{
			Assert.That(new PinMameOutputModel(generation).PhysicalSolenoids, Is.EqualTo(physical));
		}

		[TestCase(WpcDmd, 8)]
		[TestCase(Wpc95, 8)]
		[TestCase(Whitestar, 9)]
		[TestCase(PinMameOutputModel.GenSam, 9)]
		public void GiMaximumFollowsTheGeneration(ulong generation, int maximum)
		{
			Assert.That(new PinMameOutputModel(generation).GiMaximum, Is.EqualTo(maximum));
		}

		[TestCase(Gts80, true)]
		[TestCase(Gts80B, true)]
		[TestCase(WpcDmd, false)]
		[TestCase(Whitestar, false)]
		public void GottliebSystem80SegmentsAreUpright(ulong generation, bool upright)
		{
			Assert.That(new PinMameOutputModel(generation).UprightSegments, Is.EqualTo(upright));
		}
	}
}
