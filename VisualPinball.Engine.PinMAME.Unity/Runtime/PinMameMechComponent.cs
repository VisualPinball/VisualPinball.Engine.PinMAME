// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using PinMame;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.PinMAME
{
	[AddComponentMenu("Visual Pinball/PinMAME/PinMAME Mech Handler")]
	public class PinMameMechComponent : MonoBehaviour, IMechHandler, ISwitchDeviceComponent, ICoilDeviceComponent, ISerializationCallbackReceiver
	{
		#region Data

		[Tooltip("This defines how the mech is controlled.")]
		public PinMameMechType Type = PinMameMechType.OneDirectionalSolenoid;

		[Tooltip("How the mech behaves when the end of the range of motion is reached.")]
		public PinMameRepeat Repeat;

		[Tooltip("Check if you need to simulate a linear movement, as opposed to non-linear movement.")]
		public bool LinearMovement = true;

		[Tooltip("Check if calculations should be updated at 240Hz, as opposed to 60Hz.")]
		public bool FastUpdates;

		[Tooltip("if you want the result to be based on the length, as opposed to the number of steps.")]
		public bool ResultByLength;

		[Unit("ms")]
		[Min(0)]
		[Tooltip("Amount of time, in milliseconds, that the specified solenoids must be enabled for, to move a single step in the progression from the start to end position. ")]
		public int Length = 200;

		[Min(0)]
		[Tooltip("The total number of steps from the start to the end position.")]
		public int Steps = 200;

		[Tooltip("Define your switch marks here.")]
		public MechMark[] Marks = {};

		[Unit("ms")]
		[Min(0)]
		[Tooltip("The amount of time in milliseconds required to reach full speed.")]
		public int Acceleration;

		[Unit("times acceleration")]
		[Min(0)]
		[Tooltip("Retardation factor. One means same as acceleration, 0.5 half as fast, etc.")]
		public int Retardation;

		#endregion

		#region Access

		[SerializeField] private string _solenoid1;
		[SerializeField] private string _solenoid2;

		public event EventHandler<MechEventArgs> OnMechUpdate;

		public PinMameMechConfig Config(List<SwitchMapping> switchMappings, List<CoilMapping> coilMappings)
		{
			var type = 0u;
			type |= Type switch {
				PinMameMechType.OneSolenoid => (uint)PinMameMechFlag.OneSol,
				PinMameMechType.OneDirectionalSolenoid => (uint)PinMameMechFlag.OneDirSol,
				PinMameMechType.TwoDirectionalSolenoids => (uint)PinMameMechFlag.TwoDirSol,
				PinMameMechType.TwoStepperSolenoids => (uint)PinMameMechFlag.TwoStepSol,
				PinMameMechType.FourStepperSolenoids => (uint)PinMameMechFlag.FourStepSol,
				_ => throw new ArgumentOutOfRangeException()
			};

			type |= Repeat switch {
				PinMameRepeat.Circle => (uint)PinMameMechFlag.Circle,
				PinMameRepeat.Reverse => (uint)PinMameMechFlag.Reverse,
				PinMameRepeat.StopAtEnd => (uint)PinMameMechFlag.StopEnd,
				_ => throw new ArgumentOutOfRangeException()
			};

			type |= LinearMovement ? (uint)PinMameMechFlag.Linear : (uint)PinMameMechFlag.NonLinear;
			type |= FastUpdates ? (uint)PinMameMechFlag.Fast : (uint)PinMameMechFlag.Slow;
			type |= ResultByLength ? (uint)PinMameMechFlag.LengthSw : (uint)PinMameMechFlag.StepSw;

			var coilMapping1 = coilMappings.FirstOrDefault(cm => ReferenceEquals(cm.Device, this) && cm.DeviceItem == _solenoid1);
			var coilMapping2 = coilMappings.FirstOrDefault(cm => ReferenceEquals(cm.Device, this) && cm.DeviceItem == _solenoid2);

			var mechConfig = new PinMameMechConfig(
				type,
				coilMapping1?.InternalId ?? 0,
				coilMapping2?.InternalId ?? 0,
				Length,
				Steps,
				0,
				Acceleration,
				Retardation
			);

			foreach (var mark in Marks) {
				var switchMapping = switchMappings.FirstOrDefault(sm => ReferenceEquals(sm.Device, this) && sm.DeviceItem == mark.SwitchId);
				if (switchMapping == null) {
					Logger.Error($"Switch \"{mark.Name}\" for mech {name} is not mapped in the switch manager, ignoring.");
					continue;
				}

				switch (mark.Type) {
					case MechMarkSwitchType.EnableBetween:
						mechConfig.AddSwitch(new PinMameMechSwitchConfig(switchMapping.InternalId, mark.StepBeginning, mark.StepEnd));
						break;

					case MechMarkSwitchType.AlwaysPulse:
						mechConfig.AddSwitch(new PinMameMechSwitchConfig(switchMapping.InternalId, -mark.PulseFreq, mark.PulseDuration));
						break;

					case MechMarkSwitchType.PulseBetween:
						mechConfig.AddSwitch(new PinMameMechSwitchConfig(switchMapping.InternalId, mark.StepBeginning, mark.StepEnd, mark.PulseFreq));
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			return mechConfig;
		}

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		#endregion

		#region Wiring

		public IEnumerable<GamelogicEngineSwitch> AvailableSwitches => Marks.Select(m => m.Switch);

		public SwitchDefault SwitchDefault => SwitchDefault.NormallyOpen;

		IEnumerable<GamelogicEngineSwitch> IDeviceComponent<GamelogicEngineSwitch>.AvailableDeviceItems => AvailableSwitches;

		IEnumerable<IGamelogicEngineDeviceItem> IDeviceComponent<IGamelogicEngineDeviceItem>.AvailableDeviceItems => AvailableCoils;

		public IEnumerable<IGamelogicEngineDeviceItem> AvailableWireDestinations => AvailableCoils;

		IEnumerable<GamelogicEngineCoil> IDeviceComponent<GamelogicEngineCoil>.AvailableDeviceItems => AvailableCoils;

		public IEnumerable<GamelogicEngineCoil> AvailableCoils {
			get {
				switch (Type)
				{
					case PinMameMechType.OneSolenoid:
						return new[] { new GamelogicEngineCoil(_solenoid1) { Description = "Motor Power" } };
					case PinMameMechType.OneDirectionalSolenoid:
						return new[] {
							new GamelogicEngineCoil(_solenoid1) { Description = "Motor Power" },
							new GamelogicEngineCoil(_solenoid2) { Description = "Motor Direction" },
						};
					case PinMameMechType.TwoDirectionalSolenoids:
						return new[] {
							new GamelogicEngineCoil(_solenoid1) { Description = "Motor Clockwise" },
							new GamelogicEngineCoil(_solenoid2) { Description = "Motor Counter-Clockwise" },
						};

					case PinMameMechType.TwoStepperSolenoids:
						return new[] {
							new GamelogicEngineCoil(_solenoid1) { Description = "Stepper 1" },
							new GamelogicEngineCoil(_solenoid2) { Description = "Stepper 2" },
						};
					case PinMameMechType.FourStepperSolenoids:
						return new[] {
							new GamelogicEngineCoil(_solenoid1) { Description = "First Stepper" },
						};
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		#endregion

		#region Runtime

		private PinMameGamelogicEngine _gle;

		private void Awake()
		{
			_gle = GetComponentInParent<PinMameGamelogicEngine>();
			if (_gle && enabled) {
				_gle.RegisterMech(this);
			}
		}

		private void Start()
		{
			// show disable checkbox in editor
		}

		public void UpdateMech(PinMameMechInfo data)
		{
			OnMechUpdate?.Invoke(this, new MechEventArgs(data.Speed, data.Pos));
		}

		#endregion

		#region ISerializationCallbackReceiver

		public void OnBeforeSerialize()
		{
			#if UNITY_EDITOR

			if (string.IsNullOrEmpty(_solenoid1)) {
				_solenoid1 = GenerateCoilId();
			}
			if (string.IsNullOrEmpty(_solenoid2) || _solenoid1 == _solenoid2) {
				_solenoid2 = GenerateCoilId();
			}


			var switchIds = new HashSet<string>();
			foreach (var mark in Marks) {
				if (!mark.HasId || switchIds.Contains(mark.SwitchId)) {
					mark.GenerateId();
				}
				switchIds.Add(mark.SwitchId);
			}
			#endif
		}

		public void OnAfterDeserialize()
		{
		}

		private string GenerateCoilId() => $"coil_{Guid.NewGuid().ToString()[..8]}";

		#endregion
	}
}
