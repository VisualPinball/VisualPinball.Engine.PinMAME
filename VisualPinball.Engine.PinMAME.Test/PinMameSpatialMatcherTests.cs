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
		[Test]
		public void SkipsPlacementsOfConflictedAndCabinetDevices()
		{
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.spatial-filters.2026', 'name': 'Spatial Filters' },
				'coverage': { 'status': 'partial', 'completion_score': 50 },
				'inputs': [
					{ 'id': 'switch.matrix-11', 'label': 'Right Flipper', 'binding': { 'group': 'pinmame.input.switch', 'device': 11 },
					  'roles': ['flipper.lower.right.switch'],
					  'spatial': { 'placements': [{ 'id': 'sensor', 'space': 'playfield', 'x': 0.63, 'y': 0.85 }] } },
					{ 'id': 'switch.start', 'label': 'Start Button', 'binding': { 'group': 'pinmame.input.switch', 'device': 13 },
					  'roles': ['cabinet.start'],
					  'spatial': { 'placements': [{ 'id': 'sensor', 'space': 'playfield', 'x': 0.9, 'y': 0.95 }] } },
					{ 'id': 'switch.outlane', 'label': 'Left Outlane', 'binding': { 'group': 'pinmame.input.switch', 'device': 25 },
					  'spatial': { 'placements': [{ 'id': 'sensor', 'space': 'playfield', 'x': 0.1, 'y': 0.7 }] } }
				],
				'outputs': [
					{ 'id': 'gi.string-3', 'label': 'GI 3', 'kind': 'gi', 'binding': { 'group': 'pinmame.output.gi', 'device': 2 },
					  'spatial': { 'status': 'conflicted', 'placements': [{ 'id': 'bulb', 'space': 'playfield', 'x': 0.75, 'y': 0.81 }] } },
					{ 'id': 'flasher.sling', 'label': 'Right Sling Flashlamps', 'kind': 'flasher', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 18 },
					  'spatial': { 'status': 'validated', 'placements': [{ 'id': 'emitter', 'space': 'playfield', 'x': 0.756, 'y': 0.812 }] } }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json);

			Assert.That(definition.SpatialSwitches.Select(item => item.HardwareId), Is.EqualTo(new[] { "25" }));
			Assert.That(definition.SpatialLamps.Select(item => item.HardwareId), Is.EqualTo(new[] { "18" }));
		}

		[Test]
		public void AcceptsEachItemOfAnOffsetRow()
		{
			// the table's row is shifted by less than the row spacing, so each target also lies close
			// to its neighbour's item, but each item clearly belongs to one target
			var targets = Enumerable.Range(0, 4).Select(i => new PinMameSpatialMatchTarget($"t{i}", 0.2f + 0.025f * i, 0.5f)).ToArray();
			var candidates = Enumerable.Range(0, 4).Select(i => new PinMameSpatialMatchCandidate($"c{i}", 0.21f + 0.025f * i, 0.5f)).ToArray();

			var result = PinMameSpatialMatcher.Match(targets, candidates);

			Assert.That(result.Decisions.All(decision => decision.IsAccepted), Is.True);
			Assert.That(result.Decisions.Select(decision => decision.Candidate.Key), Is.EqualTo(new[] { "c0", "c1", "c2", "c3" }));
		}

		[Test]
		public void TellsItemsAtTheSamePositionApartByName()
		{
			var targets = new[] {
				new PinMameSpatialMatchTarget("32", 0.9f, 0.48f, "Gun Mark"),
				new PinMameSpatialMatchTarget("33", 0.9f, 0.48f, "Gun Home"),
			};
			var candidates = new[] {
				new PinMameSpatialMatchCandidate("home", 0.9f, 0.48f, "Gun Home"),
				new PinMameSpatialMatchCandidate("mark", 0.9f, 0.48f, "gun mark"),
			};

			var result = PinMameSpatialMatcher.Match(targets, candidates);

			Assert.That(result.Decisions.Single(decision => decision.Target.Key == "32").Candidate?.Key, Is.EqualTo("mark"));
			Assert.That(result.Decisions.Single(decision => decision.Target.Key == "33").Candidate?.Key, Is.EqualTo("home"));
		}

		[Test]
		public void ATargetMissingFromTheTableDoesNotPushAMatchAway()
		{
			// "eyes" has no item of its own on the table; the nearest one belongs to "drop"
			var targets = new[] {
				new PinMameSpatialMatchTarget("drop", 0.317f, 0.184f),
				new PinMameSpatialMatchTarget("eyes", 0.302f, 0.138f),
			};
			var candidates = new[] {
				new PinMameSpatialMatchCandidate("drop-insert", 0.311f, 0.168f),
				new PinMameSpatialMatchCandidate("gi-bulb", 0.266f, 0.216f),
			};

			var result = PinMameSpatialMatcher.Match(targets, candidates);

			Assert.That(result.Decisions.Single(decision => decision.Target.Key == "drop").Candidate?.Key, Is.EqualTo("drop-insert"));
			Assert.That(result.Decisions.Single(decision => decision.Target.Key == "eyes").IsAccepted, Is.False);
		}
	}
}
