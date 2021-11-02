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
	public class PinMameMechComponent : MonoBehaviour, IMechHandler, ISwitchDeviceComponent, ISerializationCallbackReceiver
	{
		#region Data

		[Tooltip("This defines how the mech is controlled.")]
		public PinMameMechType Type;

		[Tooltip("The first solenoid handled by the mech.")]
		public int Solenoid1;

		[Tooltip("The second solenoid handled by the mech.")]
		public int Solenoid2;

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
		public int Length;

		[Min(0)]
		[Tooltip("The total number of steps from the start to the end position.")]
		public int Steps;

		[Tooltip("Define your switch marks here.")]
		public MechMark[] Marks;

		[Unit("ms")]
		[Min(0)]
		[Tooltip("The amount of time in milliseconds required to reach full speed.")]
		public int Acceleration;

		[Min(0)]
		[Tooltip("Retardation factor. One means same as acceleration, 0.5 half as fast, etc.")]
		public int Retardation;

		#endregion

		#region Access

		public event EventHandler<MechEventArgs> OnMechUpdate;

		public PinMameMechConfig Config(List<SwitchMapping> switchMappings)
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

			var mechConfig = new PinMameMechConfig(
				type,
				Solenoid1,
				Solenoid2,
				Length,
				Steps,
				0,
				Acceleration,
				Retardation
			);

			foreach (var mark in Marks) {
				var switchMapping = switchMappings.FirstOrDefault(sm => sm.Device == this && sm.DeviceItem == mark.SwitchId);
				if (switchMapping == null) {
					Logger.Error($"Switch \"{mark.Description}\" for mech {name} is not mapped in the switch manager, ignoring.");
					continue;
				}
				mechConfig.AddSwitch(new PinMameMechSwitchConfig(switchMapping.InternalId, mark.StepBeginning, mark.StepEnd, mark.Pulse));
			}

			return mechConfig;

		}

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		#endregion

		#region Wiring

		public IEnumerable<GamelogicEngineSwitch> AvailableSwitches => Marks.Select(m => m.Switch);

		public SwitchDefault SwitchDefault => SwitchDefault.NormallyOpen;

		IEnumerable<GamelogicEngineSwitch> IDeviceComponent<GamelogicEngineSwitch>.AvailableDeviceItems => AvailableSwitches;

		#endregion

		#region Runtime

		private PinMameGamelogicEngine _gle;

		private void Awake()
		{
			_gle = GetComponentInParent<PinMameGamelogicEngine>();
			if (_gle) {
				_gle.RegisterMech(this);
			}
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

		#endregion
	}

	[Serializable]
	public class PinMameMechSwitchMark
	{
		public string Description;
		public int SwitchId;
		public PinMameMechSwitchMarkType Type;

		public int StepBeginning;
		public int StepEnd;
		public int StepPulseDuration;
	}

	public enum PinMameMechSwitchMarkType
	{
		Switch, PulseSwitch, PulseSwitchNew
	}
}
