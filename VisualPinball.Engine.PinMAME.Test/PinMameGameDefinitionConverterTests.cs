using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Engine.Math;

namespace VisualPinball.Engine.PinMAME.Editor
{
	[TestFixture]
	public class PinMameGameDefinitionConverterTests
	{
		[Test]
		public void ConvertsTransportBindingsAndDriverOverrides()
		{
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.machine.2026', 'name': 'Test Machine' },
				'coverage': { 'status': 'partial', 'completion_score': 75 },
				'drivers': [
					{ 'id': 'test_1', 'description': 'Test 1' },
					{ 'id': 'test_2', 'description': 'Test 2', 'overrides': [
						{ 'target': 'switch.start', 'binding': { 'group': 'pinmame.input.switch', 'device': 12 } }
					] }
				],
				'catalogDrivers': [
					{ 'id': 'test_3', 'description': 'Catalog-only test ROM' }
				],
				'inputs': [
					{ 'id': 'switch.start', 'label': 'Start', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 11 }, 'roles': ['cabinet.start'], 'pulse': true },
					{ 'id': 'switch.tournament-start', 'label': 'Tournament start', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 17 }, 'roles': ['cabinet.tournament-start'], 'pulse': true },
					{ 'id': 'switch.tournament-start-generic', 'label': 'Tournament Start', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 18 }, 'roles': ['cabinet.start'], 'pulse': true },
					{ 'id': 'switch.pendulum-tilt', 'label': 'Pendulum Tilt', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 19 }, 'roles': ['cabinet.tilt'] },
					{ 'id': 'switch.slam-tilt-generic', 'label': 'Slam Tilt', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 20 }, 'roles': ['cabinet.tilt'] },
					{ 'id': 'switch.ball-roll-tilt', 'label': 'Ball Roll Tilt', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 21 }, 'roles': ['cabinet.tilt'] },
					{ 'id': 'switch.flipper', 'label': 'Left Main Flipper Button', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 13 }, 'roles': ['cabinet.flipper'] },
					{ 'id': 'switch.flipper-virtual', 'label': 'Virtual Left Flipper Compatibility State', 'kind': 'virtual', 'binding': { 'group': 'pinmame.input.switch', 'device': 22 }, 'roles': ['internal.compatibility'] },
					{ 'id': 'switch.flipper-right', 'label': 'Right Flipper Button', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 16 }, 'roles': ['flipper.lower.right.button'] },
					{ 'id': 'switch.flipper-upper', 'label': 'Upper Left Flipper Button', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 15 }, 'roles': ['flipper.upper.left'] },
					{ 'id': 'switch.flipper-mini', 'label': 'Lower mini-playfield left flipper button', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 88 }, 'roles': ['cabinet.flipper'] },
					{ 'id': 'switch.eos', 'label': 'Left Flipper EOS', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 14 }, 'roles': ['flipper.lower.left.button', 'cabinet.flipper'] },
					{ 'id': 'switch.service-wpc', 'label': 'Service Credits / Escape', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 5 }, 'roles': ['cabinet.service'] },
					{ 'id': 'switch.service-sam', 'label': 'Coin-door Back button', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': -3 }, 'roles': ['service.button'] },
					{ 'id': 'switch.grip', 'label': 'Grip Trigger', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 34 }, 'roles': ['cabinet.grip-trigger'] },
					{ 'id': 'switch.fire', 'label': 'Fire', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 64 }, 'roles': ['cabinet.fire'] },
					{ 'id': 'switch.buy-in', 'label': 'Buy-in', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 10 }, 'roles': ['cabinet.buy-in'] },
					{ 'id': 'switch.constant', 'label': 'Interlock', 'kind': 'constant', 'binding': { 'group': 'pinmame.input.switch', 'device': -1 }, 'constant_active': true },
					{ 'id': 'switch.unused', 'label': 'Unused', 'kind': 'switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 99 }, 'availability': 'unused' }
				],
				'outputs': [
					{ 'id': 'flasher.one', 'label': 'Flasher', 'kind': 'flasher', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 1 } },
					{ 'id': 'device.left-flipper', 'label': 'Left Flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 6 } },
					{ 'id': 'coil.flipper-upper-left', 'label': 'Upper Left Flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 7 } },
					{ 'id': 'coil.right', 'label': 'Right Flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 8 }, 'roles': ['flipper.right.coil'] },
					{ 'id': 'coil.lower-mini-playfield-left-flipper', 'label': 'Lower mini-playfield left flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 4 } },
					{ 'id': 'lamp.one.red', 'label': 'Lamp (red)', 'kind': 'rgb_lamp', 'binding': { 'group': 'pinmame.output.lamp', 'device': 2 } },
					{ 'id': 'gi.one', 'label': 'GI without explicit legacy range', 'kind': 'gi', 'binding': { 'group': 'pinmame.output.gi', 'device': 3 } },
					{ 'id': 'gi.explicit', 'label': 'GI with explicit range', 'kind': 'gi', 'binding': { 'group': 'pinmame.output.gi', 'device': 4 }, 'range': { 'minimum': 0, 'maximum': 8, 'steps': 9 } },
					{ 'id': 'lamp.degenerate-range', 'label': 'Degenerate fading range', 'kind': 'lamp', 'binding': { 'group': 'pinmame.output.lamp', 'device': 6 }, 'range': { 'minimum': 0, 'maximum': 0, 'steps': 3 } },
					{ 'id': 'coil.lamp-transport', 'label': 'Lamp-driven coil', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.lamp', 'device': 4 } },
					{ 'id': 'state.virtual', 'label': 'Virtual state bit', 'kind': 'virtual', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 5 } },
					{ 'id': 'lamp.unused', 'label': 'Unused lamp', 'kind': 'lamp', 'binding': { 'group': 'pinmame.output.lamp', 'device': 5 }, 'availability': 'unused' }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json, "test_2");

			Assert.That(definition.MachineId, Is.EqualTo("test.machine.2026"));
			Assert.That(definition.CompletionScore, Is.EqualTo(75));
			Assert.That(definition.Switches.Select(@switch => @switch.Id), Is.EqualTo(new[] { "-3", "-1", "5", "10", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "34", "64", "88" }));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "12").InputActionHint, Is.EqualTo(InputConstants.ActionStartGame));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "17").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "18").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "19").InputActionHint, Is.EqualTo("Tilt"));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "20").InputActionHint, Is.EqualTo(InputConstants.ActionSlamTilt));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "21").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "13").InputActionHint, Is.EqualTo(InputConstants.ActionLeftFlipper));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "14").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "88").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "5").InputActionHint, Is.EqualTo(InputConstants.ActionCoinDoorCancel));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "-3").InputActionHint, Is.EqualTo(InputConstants.ActionCoinDoorBack));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "34").InputActionHint, Is.EqualTo(InputConstants.ActionPlunger));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "64").InputActionHint, Is.EqualTo(InputConstants.ActionFire1));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "10").InputActionHint, Is.EqualTo(InputConstants.ActionFrontBuyIn));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "-1").ConstantHint, Is.EqualTo(SwitchConstantHint.AlwaysClosed));
			Assert.That(definition.Coils.Single(coil => coil.Id == "1").IsLamp, Is.True);
			Assert.That(definition.Coils.Single(coil => coil.Id == "1").DeviceHint, Is.EqualTo("^F1?1$"));
			Assert.That(definition.Coils.Single(coil => coil.Id == "6").DeviceHint, Does.Contain("LeftFlipper"));
			Assert.That(definition.Coils.Single(coil => coil.Id == "6").DeviceHint, Does.Contain("FlipperLeft"));
			Assert.That(definition.Coils, Has.None.Matches<GamelogicEngineCoil>(coil => coil.Id == "5"));
			Assert.That(definition.Wires, Has.Exactly(1).Matches<GamelogicEngineWire>(wire => wire.SourceId == "13" && wire.DestinationId == "6"));
			Assert.That(definition.Wires, Has.Exactly(1).Matches<GamelogicEngineWire>(wire => wire.SourceId == "16" && wire.DestinationId == "8"));
			Assert.That(definition.Wires, Has.Exactly(1).Matches<GamelogicEngineWire>(wire => wire.SourceId == "15" && wire.DestinationId == "7"));
			Assert.That(definition.Wires, Has.None.Matches<GamelogicEngineWire>(wire => wire.SourceId == "88" || wire.DestinationId == "4"));
			Assert.That(definition.Lamps.Single(lamp => lamp.Id == "2").Channel, Is.EqualTo(ColorChannel.Red));
			Assert.That(definition.Lamps.Single(lamp => lamp.Id == "3").Source, Is.EqualTo(LampSource.GI));
			Assert.That(definition.Lamps.Single(lamp => lamp.Id == "3").Type, Is.EqualTo(LampType.SingleFading));
			Assert.That(definition.Lamps.Single(lamp => lamp.Id == "3").FadingSteps, Is.EqualTo(8));
			Assert.That(definition.Lamps.Single(lamp => lamp.Id == "4").Type, Is.EqualTo(LampType.SingleFading));
			Assert.That(definition.Lamps.Single(lamp => lamp.Id == "4").FadingSteps, Is.EqualTo(8));
			Assert.That(definition.Lamps.Single(lamp => lamp.Id == "6").Type, Is.EqualTo(LampType.SingleFading));
			Assert.That(definition.Lamps.Single(lamp => lamp.Id == "6").FadingSteps, Is.EqualTo(2));
			Assert.That(definition.Diagnostics, Has.Exactly(1).Contains("Lamp-driven coil"));

			var catalogOnlyDefinition = PinMameGameDefinitionConverter.Convert(json, "test_3");
			Assert.That(catalogOnlyDefinition.Switches.Single(@switch => @switch.Description == "Start").Id, Is.EqualTo("11"));
			Assert.That(catalogOnlyDefinition.Diagnostics, Has.Exactly(1).Contains("no curated driver override"));
		}

		[Test]
		public void SkipsAmbiguousOrDuplicateDevicesWithDiagnostics()
		{
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.quirks.2026', 'name': 'Test Quirks' },
				'coverage': { 'status': 'partial', 'completion_score': 50 },
				'inputs': [
					{ 'id': 'switch.left.1', 'label': 'Left Flipper Button', 'binding': { 'group': 'pinmame.input.switch', 'device': 1 }, 'roles': ['flipper.lower.left'] },
					{ 'id': 'switch.left.2', 'label': 'Second Left Flipper Button', 'binding': { 'group': 'pinmame.input.switch', 'device': 2 }, 'roles': ['flipper.lower.left'] },
					{ 'id': 'switch.duplicate', 'label': 'Duplicate Switch', 'binding': { 'group': 'pinmame.input.switch', 'device': 1 } }
				],
				'outputs': [
					{ 'id': 'coil.left', 'label': 'Left Flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 1 }, 'roles': ['flipper.lower.left'] },
					{ 'id': 'lamp.valid', 'label': 'Valid Lamp', 'kind': 'lamp', 'binding': { 'group': 'pinmame.output.lamp', 'device': 2 } },
					{ 'id': 'lamp.duplicate', 'label': 'Duplicate Lamp', 'kind': 'lamp', 'binding': { 'group': 'pinmame.output.lamp', 'device': 2 } },
					{ 'id': 'lamp.rgb', 'label': 'Unidentified RGB Lamp', 'kind': 'rgb_lamp', 'binding': { 'group': 'pinmame.output.lamp', 'device': 3 } }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json);

			Assert.That(definition.Switches, Has.Length.EqualTo(2));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "1").InputActionHint, Is.EqualTo(InputConstants.ActionLeftFlipper));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "2").InputActionHint, Is.Empty);
			Assert.That(definition.Lamps, Has.Length.EqualTo(1));
			Assert.That(definition.Wires, Is.Empty);
			Assert.That(definition.Diagnostics, Has.Some.Contains("ambiguous Lower Left Flipper switch roles"));
			Assert.That(definition.Diagnostics, Has.Some.Contains("Duplicate PinMAME switch address 1"));
			Assert.That(definition.Diagnostics, Has.Some.Contains("Duplicate input action"));
			Assert.That(definition.Diagnostics, Has.Some.Contains("Duplicate PinMAME output binding pinmame.output.lamp address 2"));
			Assert.That(definition.Diagnostics, Has.Some.Contains("does not identify its red, green, or blue channel"));
		}

		[Test]
		public void LeavesSharedFlipperInputForManualMapping()
		{
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.shared-flipper.2026', 'name': 'Test Shared Flipper' },
				'coverage': { 'status': 'partial', 'completion_score': 50 },
				'inputs': [
					{ 'id': 'switch.flipper-buttons', 'label': 'Left & Right Flipper Button', 'binding': { 'group': 'pinmame.input.switch', 'device': 21 }, 'roles': ['flipper.lower.left.button', 'flipper.lower.right.button'] }
				],
				'outputs': [
					{ 'id': 'device.flipper-lower-left', 'label': 'Lower left flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 48 } },
					{ 'id': 'device.flipper-lower-right', 'label': 'Lower right flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 46 } }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json);

			Assert.That(definition.Switches.Single(@switch => @switch.Id == "21").InputActionHint, Is.Empty);
			Assert.That(definition.Wires, Is.Empty);
			Assert.That(definition.Diagnostics, Has.Some.Contains("represents multiple flipper buttons"));
			Assert.That(definition.Diagnostics, Has.Some.Contains("direct flipper wires were left for manual mapping"));
		}

		[Test]
		public void MapsGenericCoinRolesByPhysicalSlotLabel()
		{
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.coins.2026', 'name': 'Test Coins' },
				'coverage': { 'status': 'partial', 'completion_score': 50 },
				'inputs': [
					{ 'id': 'switch.coin-4', 'label': '4th Coin Slot', 'binding': { 'group': 'pinmame.input.switch', 'device': 2 }, 'roles': ['cabinet.coin'] },
					{ 'id': 'switch.coin-6', 'label': '6th Coin Slot', 'binding': { 'group': 'pinmame.input.switch', 'device': 3 }, 'roles': ['cabinet.coin'] },
					{ 'id': 'switch.coin-right', 'label': 'Right Coin Slot', 'binding': { 'group': 'pinmame.input.switch', 'device': 4 }, 'roles': ['cabinet.coin'] },
					{ 'id': 'switch.coin-center', 'label': 'Center Coin Slot / DBA', 'binding': { 'group': 'pinmame.input.switch', 'device': 5 }, 'roles': ['cabinet.coin'] },
					{ 'id': 'switch.coin-left', 'label': 'Left Coin Slot', 'binding': { 'group': 'pinmame.input.switch', 'device': 6 }, 'roles': ['cabinet.coin'] },
					{ 'id': 'switch.coin-5', 'label': '5th Coin Slot', 'binding': { 'group': 'pinmame.input.switch', 'device': 7 }, 'roles': ['cabinet.coin'] }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json);

			Assert.That(definition.Switches.Single(@switch => @switch.Id == "2").InputActionHint, Is.EqualTo(InputConstants.ActionInsertCoin4));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "3").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "4").InputActionHint, Is.EqualTo(InputConstants.ActionInsertCoin3));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "5").InputActionHint, Is.EqualTo(InputConstants.ActionInsertCoin2));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "6").InputActionHint, Is.EqualTo(InputConstants.ActionInsertCoin1));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "7").InputActionHint, Is.Empty);
		}

		[Test]
		public void UsesPinMameNumberingForGenericFlipperSwitches()
		{
			// labels and roles of 111-114 swapped, like the published Terminator 2 definition
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.flipper-numbering.2026', 'name': 'Test Flipper Numbering' },
				'coverage': { 'status': 'partial', 'completion_score': 50 },
				'inputs': [
					{ 'id': 'switch.matrix-11', 'label': 'Right Flipper', 'binding': { 'group': 'pinmame.input.switch', 'device': 11 }, 'roles': ['flipper.lower.right.switch'] },
					{ 'id': 'switch.generic-111', 'label': 'Lower Right Flipper Button', 'availability': 'optional', 'binding': { 'group': 'pinmame.input.switch', 'device': 111 }, 'roles': ['flipper.lower.right.button'] },
					{ 'id': 'switch.generic-112', 'label': 'Lower Right Flipper EOS', 'availability': 'optional', 'normally_closed': true, 'binding': { 'group': 'pinmame.input.switch', 'device': 112 }, 'roles': ['internal.flipper.lower.right.eos'] },
					{ 'id': 'switch.generic-113', 'label': 'Lower Left Flipper Button', 'availability': 'optional', 'binding': { 'group': 'pinmame.input.switch', 'device': 113 }, 'roles': ['flipper.lower.left.button'] },
					{ 'id': 'switch.generic-114', 'label': 'Lower Left Flipper EOS', 'availability': 'optional', 'normally_closed': true, 'binding': { 'group': 'pinmame.input.switch', 'device': 114 }, 'roles': ['internal.flipper.lower.left.eos'] }
				],
				'outputs': [
					{ 'id': 'device.flipper-lower-right', 'label': 'Lower right flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 46 } },
					{ 'id': 'device.flipper-lower-left', 'label': 'Lower left flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 48 } }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json);

			Assert.That(definition.Switches.Single(@switch => @switch.Id == "112").InputActionHint, Is.EqualTo(InputConstants.ActionRightFlipper));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "114").InputActionHint, Is.EqualTo(InputConstants.ActionLeftFlipper));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "111").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "113").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "11").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "112").NormallyClosed, Is.False);
			Assert.That(definition.Wires.Select(wire => $"{wire.SourceId}->{wire.DestinationId}"), Is.EquivalentTo(new[] { "112->46", "114->48" }));
			Assert.That(definition.Diagnostics.Count(diagnostic => diagnostic.Contains("PinMAME's numbering")), Is.EqualTo(4));
		}

		[Test]
		public void PrefersGenericFlipperButtonsOverMatrixCopies()
		{
			// pre-Fliptronic WPC lists the matrix flipper switches as buttons too, like the published Bride of Pinbot
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.matrix-flipper-copies.2026', 'name': 'Test Matrix Flipper Copies' },
				'coverage': { 'status': 'author_ready', 'completion_score': 100 },
				'inputs': [
					{ 'id': 'switch.matrix-11', 'label': 'Right Flipper', 'binding': { 'group': 'pinmame.input.switch', 'device': 11 }, 'roles': ['flipper.lower.right.button'] },
					{ 'id': 'switch.matrix-12', 'label': 'Left Flipper', 'binding': { 'group': 'pinmame.input.switch', 'device': 12 }, 'roles': ['flipper.lower.left.button'] },
					{ 'id': 'switch.generic-112', 'label': 'Generic Right Flipper Button State', 'binding': { 'group': 'pinmame.input.switch', 'device': 112 }, 'roles': ['flipper.lower.right.button'] },
					{ 'id': 'switch.generic-114', 'label': 'Generic Left Flipper Button State', 'binding': { 'group': 'pinmame.input.switch', 'device': 114 }, 'roles': ['flipper.lower.left.button'] }
				],
				'outputs': [
					{ 'id': 'device.flipper-lower-right', 'label': 'Lower right flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 46 } },
					{ 'id': 'device.flipper-lower-left', 'label': 'Lower left flipper', 'kind': 'coil', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 48 } }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json);

			Assert.That(definition.Switches.Single(@switch => @switch.Id == "112").InputActionHint, Is.EqualTo(InputConstants.ActionRightFlipper));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "114").InputActionHint, Is.EqualTo(InputConstants.ActionLeftFlipper));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "11").InputActionHint, Is.Empty);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "12").InputActionHint, Is.Empty);
			Assert.That(definition.Wires.Select(wire => $"{wire.SourceId}->{wire.DestinationId}"), Is.EquivalentTo(new[] { "112->46", "114->48" }));
			Assert.That(definition.Diagnostics, Has.None.Contains("not imported"));
		}

		[Test]
		public void PassesNormallyClosedOnlyForTheCoinDoor()
		{
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.normally-closed.2026', 'name': 'Test Normally Closed' },
				'coverage': { 'status': 'partial', 'completion_score': 50 },
				'inputs': [
					{ 'id': 'switch.coin-door', 'label': 'Coin Door Closed', 'normally_closed': true, 'binding': { 'group': 'pinmame.input.switch', 'device': 22 }, 'roles': ['cabinet.coin-door'] },
					{ 'id': 'switch.trough-eject', 'label': 'Trough Eject', 'normally_closed': true, 'binding': { 'group': 'pinmame.input.switch', 'device': 31 } },
					{ 'id': 'switch.slam-tilt', 'label': 'Slam Tilt', 'normally_closed': true, 'binding': { 'group': 'pinmame.input.switch', 'device': 21 }, 'roles': ['cabinet.slam-tilt'] }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json);

			Assert.That(definition.Switches.Single(@switch => @switch.Id == "22").NormallyClosed, Is.True);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "31").NormallyClosed, Is.False);
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "21").NormallyClosed, Is.False);
		}

		[Test]
		public void KeepsVirtualHostDevicesAndDropsVirtualMirrors()
		{
			const string json = @"{
				'format': 'pinmame-machine-definition',
				'schema_version': 2,
				'machine': { 'id': 'test.virtual.2026', 'name': 'Test Virtual' },
				'coverage': { 'status': 'partial', 'completion_score': 50 },
				'inputs': [
					{ 'id': 'switch.flipper-right', 'label': 'Right Flipper Button', 'kind': 'virtual', 'availability': 'used', 'binding': { 'group': 'pinmame.input.switch', 'device': 82 }, 'roles': ['flipper.lower.right.button'] },
					{ 'id': 'switch.host-left', 'label': 'Left Flipper Button Host Input', 'kind': 'virtual', 'availability': 'used', 'binding': { 'group': 'pinmame.input.switch', 'device': 84 } },
					{ 'id': 'switch.compat', 'label': 'Flipper compatibility state', 'kind': 'virtual', 'availability': 'used', 'binding': { 'group': 'pinmame.input.switch', 'device': 85 }, 'roles': ['internal.compatibility'] }
				],
				'outputs': [
					{ 'id': 'device.synthetic-flipper-right', 'label': 'Synthetic Lower Right Flipper', 'kind': 'virtual', 'availability': 'used', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 46 }, 'roles': ['internal.synthetic-flipper', 'flipper.lower.right'] },
					{ 'id': 'device.mirror', 'label': 'LED Transport Mirror Of 37', 'kind': 'virtual', 'availability': 'used', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 41 } },
					{ 'id': 'device.state', 'label': 'WPC State Channel 29', 'kind': 'virtual', 'availability': 'used', 'binding': { 'group': 'pinmame.output.solenoid', 'device': 29 }, 'roles': ['internal.wpc-state'] }
				]
			}";

			var definition = PinMameGameDefinitionConverter.Convert(json);

			Assert.That(definition.Switches.Select(@switch => @switch.Id), Is.EquivalentTo(new[] { "82", "84" }));
			Assert.That(definition.Switches.Single(@switch => @switch.Id == "82").InputActionHint, Is.EqualTo(InputConstants.ActionRightFlipper));
			Assert.That(definition.Coils.Select(coil => coil.Id), Is.EquivalentTo(new[] { "46" }));
			Assert.That(definition.Wires.Select(wire => $"{wire.SourceId}->{wire.DestinationId}"), Is.EquivalentTo(new[] { "82->46" }));
		}

		[Test]
		public void ConvertsEveryCuratedDefinitionWhenCorpusIsAvailable()
		{
			var root = Environment.GetEnvironmentVariable("PINMAME_GAME_DEFS_ROOT");
			if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(Path.Combine(root, "machines"))) {
				Assert.Ignore("Set PINMAME_GAME_DEFS_ROOT to run the cross-repository corpus test.");
			}

			var failures = new List<string>();
			var files = new[] { "author-ready", "partial" }
				.SelectMany(status => Directory.EnumerateFiles(Path.Combine(root, "machines", status), "*.json", SearchOption.AllDirectories));
			foreach (var file in files) {
				var isAuthorReady = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("author-ready", StringComparer.Ordinal);
				var json = File.ReadAllText(file);
				var drivers = (JObject.Parse(json)["drivers"] as JArray)?.OfType<JObject>().Select(driver => driver.Value<string>("id")).Where(id => id != null).ToArray()
					?? Array.Empty<string>();
				foreach (var driver in drivers.DefaultIfEmpty(null)) {
					try {
						var definition = PinMameGameDefinitionConverter.Convert(json, driver);
						var duplicateAction = definition.Switches
							.Where(@switch => !string.IsNullOrEmpty(@switch.InputActionHint))
							.GroupBy(@switch => @switch.InputActionHint, StringComparer.Ordinal)
							.FirstOrDefault(group => group.Count() > 1);
						if (duplicateAction != null) {
							failures.Add($"{Path.GetFileName(file)} [{driver ?? "default"}]: duplicate input action {duplicateAction.Key}");
						}
						if (isAuthorReady) {
							var lossyDiagnostic = definition.Diagnostics.FirstOrDefault(diagnostic =>
								diagnostic.Contains("was not imported", StringComparison.Ordinal)
								|| diagnostic.Contains("left for manual mapping", StringComparison.Ordinal)
								|| diagnostic.Contains("single-channel lamp output", StringComparison.Ordinal)
								|| diagnostic.Contains("VPE has no coil destination", StringComparison.Ordinal));
							if (lossyDiagnostic != null) failures.Add($"{Path.GetFileName(file)} [{driver ?? "default"}]: {lossyDiagnostic}");
						}
					} catch (Exception exception) {
						failures.Add($"{Path.GetFileName(file)} [{driver ?? "default"}]: {exception.Message}");
					}
				}
			}

			Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
		}

		[Test]
		public void ConvertsEverySelectablePublishedDetailWhenSiteDataIsAvailable()
		{
			var root = Environment.GetEnvironmentVariable("PINMAME_GAME_DEFS_ROOT");
			if (string.IsNullOrWhiteSpace(root)) {
				Assert.Ignore("Set PINMAME_GAME_DEFS_ROOT to run the published-detail test.");
			}
			var dataRoot = Path.Combine(root, "site", "public", "data");
			var indexPath = Path.Combine(dataRoot, "index.json");
			Assert.That(File.Exists(indexPath), Is.True, $"The pinned corpus is missing its generated site index: {indexPath}");

			var catalog = PinMameGameDefinitionCatalogClient.ParseCatalog(File.ReadAllText(indexPath));
			var failures = new List<string>();
			var testedDefinitions = 0;
			foreach (var machine in catalog.Machines.Where(entry => entry.IsSelectable)) {
				var expectedDetail = $"data/machines/{machine.Slug}.json";
				if (!string.Equals(machine.Detail, expectedDetail, StringComparison.Ordinal)) {
					failures.Add($"{machine.Id}: unexpected detail path {machine.Detail}");
					continue;
				}
				var detailPath = Path.Combine(dataRoot, "machines", $"{machine.Slug}.json");
				if (!File.Exists(detailPath)) {
					failures.Add($"{machine.Id}: missing {detailPath}");
					continue;
				}

				var json = File.ReadAllText(detailPath);
				foreach (var romId in (machine.Roms ?? Array.Empty<string>()).DefaultIfEmpty(null)) {
					try {
						PinMameGameDefinitionConverter.Convert(json, romId);
					} catch (Exception exception) {
						failures.Add($"{machine.Id} [{romId ?? "default"}]: {exception.Message}");
					}
				}
				testedDefinitions++;
			}

			Assert.That(testedDefinitions, Is.GreaterThan(0));
			Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
		}

		[Test]
		public void CatalogFiltersKnownNonPhysicalEntriesWithoutRejectingFutureKinds()
		{
			const string json = @"{
				'format': 'pinmame-machine-reference-index',
				'version': 2,
				'machines': [
					{ 'slug': 'physical', 'id': 'physical', 'name': 'Physical', 'status': 'partial', 'machineKind': 'physical_pinball', 'detail': 'data/machines/physical.json' },
					{ 'slug': 'diagnostic', 'id': 'diagnostic', 'name': 'Diagnostic', 'status': 'partial', 'machineKind': 'diagnostic_software', 'detail': 'data/machines/diagnostic.json' },
					{ 'slug': 'redemption', 'id': 'redemption', 'name': 'Redemption', 'status': 'partial', 'machineKind': 'redemption_game', 'detail': 'data/machines/redemption.json' },
					{ 'slug': 'conversion', 'id': 'conversion', 'name': 'Conversion', 'status': 'partial', 'machineKind': 'physical_conversion', 'detail': 'data/machines/conversion.json' },
					{ 'slug': 'future', 'id': 'future', 'name': 'Future', 'status': 'partial', 'machineKind': 'new_physical_kind', 'detail': 'data/machines/future.json' }
				]
			}";

			var catalog = PinMameGameDefinitionCatalogClient.ParseCatalog(json);

			Assert.That(catalog.Machines.Single(machine => machine.Id == "physical").IsSelectable, Is.True);
			Assert.That(catalog.Machines.Single(machine => machine.Id == "diagnostic").IsSelectable, Is.False);
			Assert.That(catalog.Machines.Single(machine => machine.Id == "redemption").IsSelectable, Is.False);
			Assert.That(catalog.Machines.Single(machine => machine.Id == "conversion").IsSelectable, Is.False);
			Assert.That(catalog.Machines.Single(machine => machine.Id == "future").IsSelectable, Is.True);
		}

		[Test]
		public void UpgradesVersionOneCatalogWithCompleteDriverList()
		{
			const string json = @"{
				'format': 'pinmame-machine-reference-index',
				'version': 1,
				'machines': [
					{ 'slug': 'physical', 'id': 'physical', 'name': 'Physical', 'status': 'partial', 'rootDrivers': ['root'], 'detail': 'data/machines/physical.json' },
					{ 'slug': 'diagnostic', 'id': 'diagnostic.test', 'name': 'Diagnostic', 'status': 'partial', 'rootDrivers': ['diag'], 'detail': 'data/machines/diagnostic.json' }
				]
			}";

			var catalog = PinMameGameDefinitionCatalogClient.ParseCatalog(json);
			PinMameGameDefinitionCatalogClient.UpgradeLegacyCatalog(catalog, new[] {
				new PinMameCatalogDriver { Id = "root", MachineId = "physical" },
				new PinMameCatalogDriver { Id = "clone", MachineId = "physical" },
			});

			var physical = catalog.Machines.Single(machine => machine.Id == "physical");
			Assert.That(physical.Roms, Is.EqualTo(new[] { "clone", "root" }));
			Assert.That(physical.MachineKind, Is.EqualTo("unknown"));
			Assert.That(physical.IsSelectable, Is.True);
			var diagnostic = catalog.Machines.Single(machine => machine.Id == "diagnostic.test");
			Assert.That(diagnostic.MachineKind, Is.EqualTo("diagnostic_software"));
			Assert.That(diagnostic.IsSelectable, Is.False);
		}

		[Test]
		public void RejectsUnknownFutureCatalogVersions()
		{
			const string json = @"{
				'format': 'pinmame-machine-reference-index',
				'version': 3,
				'machines': []
			}";

			Assert.That(() => PinMameGameDefinitionCatalogClient.ParseCatalog(json), Throws.TypeOf<InvalidDataException>());
		}

		[TestCase("t2_l82", "s_flipper_lower_left", 114)]
		[TestCase("rock", "s_coin_1", 17)]
		[TestCase("trn_174h", "s_enter", 0)]
		[TestCase("centaur", "s_start_button", 6)]
		public void ResolvesLegacySwitchAliases(string romId, string alias, int expectedAddress)
		{
			Assert.That(PinMameLegacyAliasResolver.TryResolveSwitch(null, romId, null, alias, out var address), Is.True);
			Assert.That(address, Is.EqualTo(expectedAddress));
		}

		[TestCase("t2_l82", "c_flipper_lower_left", 48)]
		[TestCase("rock", "c_flipper_upper_right", 45)]
		[TestCase("twd_160h", "c_flipper_lower_right", 16)]
		public void ResolvesLegacyCoilAliases(string romId, string alias, int expectedAddress)
		{
			Assert.That(PinMameLegacyAliasResolver.TryResolveCoil(null, romId, null, alias, out var address), Is.True);
			Assert.That(address, Is.EqualTo(expectedAddress));
		}
	}
}
