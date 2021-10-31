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
using NLog;
using PinMame;
using UnityEngine;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.PinMAME
{
	public class PinMameMechComponent : MonoBehaviour
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

		[Min(0)]
		[Tooltip("Amount of time, in milliseconds, that the specified solenoids must be enabled for, to move a single step in the progression from the start to end position. ")]
		public int Length;

		[Min(0)]
		[Tooltip("The total number of steps from the start to the end position.")]
		public int Steps;

		[Tooltip("Define your switch marks here.")]
		public PinMameMechSwitchMark[] Marks;

		[Min(0)]
		[Tooltip("The amount of time in milliseconds required to reach full speed.")]
		public float Acceleration;

		[Min(0)]
		[Tooltip("Retardation factor. One means same as acceleration, 0.5 half as fast, etc.")]
		public float Retardation;

		#endregion

		#region Access

		public PinMameMechConfig Config
		{
			get {
				var type = 0u;
				type |= Type switch {
					PinMameMechType.OneSolenoid => (uint)PinMameMechFlag.ONESOL,
					PinMameMechType.OneDirectionalSolenoid => (uint)PinMameMechFlag.ONEDIRSOL,
					PinMameMechType.TwoDirectionalSolenoids => (uint)PinMameMechFlag.TWODIRSOL,
					PinMameMechType.TwoStepperSolenoids => (uint)PinMameMechFlag.TWOSTEPSOL,
					PinMameMechType.FourStepperSolenoids => (uint)PinMameMechFlag.FOURSTEPSOL,
					_ => throw new ArgumentOutOfRangeException()
				};

				type |= Repeat switch {
					PinMameRepeat.Circle => (uint)PinMameMechFlag.CIRCLE,
					PinMameRepeat.Reverse => (uint)PinMameMechFlag.REVERSE,
					PinMameRepeat.StopAtEnd => (uint)PinMameMechFlag.STOPEND,
					_ => throw new ArgumentOutOfRangeException()
				};

				type |= LinearMovement ? (uint)PinMameMechFlag.LINEAR : (uint)PinMameMechFlag.NONLINEAR;
				type |= FastUpdates ? (uint)PinMameMechFlag.FAST : (uint)PinMameMechFlag.SLOW;
				//type |= ResultByLength ? (uint)PinMameMechFlag.LENGTHSW : (uint)PinMameMechFlag.STEPSW;

				var mechConfig = new PinMameMechConfig(
					type,
					Solenoid1,
					Solenoid2,
					Length,
					Steps,
					0
				);

				foreach(var mark in Marks) {
					switch (mark.Type) {
						case PinMameMechSwitchMarkType.Switch:
							mechConfig.AddSwitch(new PinMameMechSwitchConfig(mark.SwitchId, mark.StepBeginning, mark.StepEnd));
							break;
						case PinMameMechSwitchMarkType.PulseSwitch:
							mechConfig.AddSwitch(new PinMameMechSwitchConfig(mark.SwitchId, mark.StepBeginning, mark.StepPulseDuration, true));
							break;
						case PinMameMechSwitchMarkType.PulseSwitchNew:
							// todo in pinmame-dotnet
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				return mechConfig;
			}

		}

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		#endregion

		#region Runtime

		private PinMameGamelogicEngine _gle;

		private void Awake()
		{
			_gle = GetComponentInParent<PinMameGamelogicEngine>();
		}

		private void Start()
		{
			_gle.RegisterMech(this);
		}

		public void OnMechUpdate(PinMameMechInfo data)
		{
			Logger.Info($"[PinMAME] <= mech updated: gameobject={name}, mechInfo={data}");
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
