using System.Linq;
using NUnit.Framework;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.Math;

namespace VisualPinball.Engine.PinMAME.Editor
{
	[TestFixture]
	public class PinMameSpatialMatcherTests
	{
		[Test]
		public void ConvertsOnlyValidPlayfieldPlacementsForImportedHardware()
		{
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.spatial.2026', 'name': 'Spatial Test' },
				'coverage': { 'status': 'partial', 'completion_score': 50 },
				'drivers': [{ 'id': 'spatial_1', 'overrides': [
					{ 'target': 'switch.target', 'binding': { 'group': 'pinmame.input.switch', 'device': 12 } }
				] }],
				'inputs': [
					{ 'id': 'switch.target', 'label': 'Target', 'kind': 'switch',
					  'binding': { 'group': 'pinmame.input.switch', 'device': 11 },
					  'spatial': { 'placements': [
						{ 'id': 'sensor', 'space': 'playfield', 'x': 0.25, 'y': 0.75 },
						{ 'id': 'cabinet', 'space': 'cabinet', 'x': 0.1, 'y': 0.2 }
					  ] } }
				],
				'outputs': [
					{ 'id': 'coil.kicker', 'label': 'Kicker', 'kind': 'coil',
					  'binding': { 'group': 'pinmame.output.solenoid', 'device': 1 },
					  'spatial': { 'placements': [{ 'id': 'actuator', 'space': 'playfield', 'x': 0.3, 'y': 0.4 }] } },
					{ 'id': 'flasher.pair', 'label': 'Flasher Pair', 'kind': 'flasher',
					  'binding': { 'group': 'pinmame.output.solenoid', 'device': 2 },
					  'spatial': { 'placements': [
						{ 'id': 'left', 'space': 'playfield', 'x': 0.2, 'y': 0.2 },
						{ 'id': 'right', 'space': 'playfield', 'x': 0.8, 'y': 0.2 }
					  ] } },
					{ 'id': 'gi.one', 'label': 'GI', 'kind': 'gi',
					  'binding': { 'group': 'pinmame.output.gi', 'device': 3 },
					  'spatial': { 'placements': [
						{ 'id': 'valid', 'space': 'playfield', 'x': 0.5, 'y': 0.5 },
						{ 'id': 'invalid', 'space': 'playfield', 'x': 1.2, 'y': 0.5 }
					  ] } },
					{ 'id': 'lamp.rgb-strip-blue', 'label': 'Strip blue', 'kind': 'lamp',
					  'binding': { 'group': 'pinmame.output.lamp', 'device': 4 },
					  'spatial': { 'placements': [{ 'id': 'pixel', 'space': 'playfield', 'x': 0.6, 'y': 0.4 }] } }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json, "spatial_1");

			Assert.That(definition.SpatialSwitches, Has.Length.EqualTo(1));
			Assert.That(definition.SpatialSwitches[0].HardwareId, Is.EqualTo("12"));
			Assert.That(definition.SpatialSwitches[0].X, Is.EqualTo(0.25f));
			Assert.That(definition.SpatialCoils, Has.Length.EqualTo(1));
			Assert.That(definition.SpatialCoils[0].HardwareId, Is.EqualTo("1"));
			Assert.That(definition.SpatialLamps, Has.Length.EqualTo(4));
			Assert.That(definition.SpatialLamps.Count(item => item.HardwareId == "2" && item.IsCoil), Is.EqualTo(2));
			Assert.That(definition.SpatialLamps.Single(item => item.HardwareId == "3").Source, Is.EqualTo(LampSource.GI));
			Assert.That(definition.SpatialLamps.Single(item => item.HardwareId == "3").Channel, Is.EqualTo(ColorChannel.Alpha));
			Assert.That(definition.Lamps.Single(item => item.Id == "4").Channel, Is.EqualTo(ColorChannel.Blue));
			Assert.That(definition.Lamps.Single(item => item.Id == "4").Type, Is.EqualTo(LampType.RgbMulti));
		}

		[Test]
		public void UsesTrustedLayoutRelationInsteadOfACloserWrongItem()
		{
			var targets = new[] { new PinMameSpatialMatchTarget("target", 0.5f, 0.5f) };
			var candidates = new[] {
				new PinMameSpatialMatchCandidate("near-but-wrong", 0.5f, 0.45f),
				new PinMameSpatialMatchCandidate("layout-match", 0.55f, 0.52f),
			};
			var anchors = new[] { new PinMameSpatialMatchAnchor(0f, 0f, 0.05f, 0.02f) };

			var result = PinMameSpatialMatcher.Match(targets, candidates, anchors);

			Assert.That(result.Decisions.Single().IsAccepted, Is.True);
			Assert.That(result.Decisions.Single().Candidate.Key, Is.EqualTo("layout-match"));
		}

		[Test]
		public void RejectsAContestedItemInsteadOfForcingTheSecondMachineTarget()
		{
			var targets = new[] {
				new PinMameSpatialMatchTarget("first", 0.1f, 0.1f),
				new PinMameSpatialMatchTarget("second", 0.13f, 0.1f),
			};
			var candidates = new[] {
				new PinMameSpatialMatchCandidate("shared-best", 0.11f, 0.1f),
				new PinMameSpatialMatchCandidate("weak-second", 0.16f, 0.1f),
			};

			var result = PinMameSpatialMatcher.Match(targets, candidates);

			Assert.That(result.Decisions.Count(decision => decision.IsAccepted), Is.EqualTo(1));
			Assert.That(result.Decisions.Single(decision => !decision.IsAccepted).Reason, Does.Contain("equally plausible"));
		}

		[Test]
		public void RejectsDifferentDeviceItemsAtTheSamePositionAsAmbiguous()
		{
			var targets = new[] { new PinMameSpatialMatchTarget("target", 0.4f, 0.6f) };
			var candidates = new[] {
				new PinMameSpatialMatchCandidate("item-a", 0.4f, 0.6f),
				new PinMameSpatialMatchCandidate("item-b", 0.4f, 0.6f),
			};

			var result = PinMameSpatialMatcher.Match(targets, candidates);

			Assert.That(result.Decisions.Single().IsAccepted, Is.False);
			Assert.That(result.Decisions.Single().Reason, Does.Contain("equally plausible"));
		}

		[Test]
		public void DoesNotDiscountAPairWhenItsOnlyRelationAnchorIsItself()
		{
			var targets = new[] {
				new PinMameSpatialMatchTarget("seed", 0.1f, 0.1f),
				new PinMameSpatialMatchTarget("distant", 0.8f, 0.8f),
			};
			var candidates = new[] {
				new PinMameSpatialMatchCandidate("seed-candidate", 0.12f, 0.1f),
				new PinMameSpatialMatchCandidate("distant-candidate", 0.95f, 0.95f),
			};

			var result = PinMameSpatialMatcher.Match(targets, candidates);

			var decision = result.Decisions.Single(item => item.Target.Key == "seed");
			Assert.That(decision.IsAccepted, Is.True);
			Assert.That(decision.Score, Is.EqualTo(0.02).Within(0.0001));
		}
	}
}
