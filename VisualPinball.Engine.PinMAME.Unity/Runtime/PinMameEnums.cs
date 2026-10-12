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

namespace VisualPinball.Engine.PinMAME
{
	public enum PinMameMechType
	{
		/// <summary>
		/// The motor turns into one direction only, and the solenoid turns it
		/// on and off.
		/// </summary>
		///
		/// <example>Sopranos pole dancers.</example>
		OneSolenoid,

		/// <summary>
		/// Two solenoids, one for enabling the motor, one for setting the
		/// direction.
		/// </summary>
		///
		/// <example>The cannon in Terminator 2.</example>
		OneDirectionalSolenoid,

		/// <summary>
		/// The first solenoid controls clockwise movement and the second
		/// solenoid controls counter-clockwise movement.
		/// </summary>
		///
		/// <example>The soccer ball in World Cup Soccer '94.</example>
		TwoDirectionalSolenoids,

		/// <summary>
		/// Two solenoids that control a stepper motor.
		/// </summary>
		///
		/// <example>Drag strip cars in Corvette.</example>
		TwoStepperSolenoids,

		/// <summary>
		/// Four solenoids that control a stepper motor.
		/// </summary>
		FourStepperSolenoids,
	}

	public enum PinMameRepeat
	{
		/// <summary>
		/// Loops, i.e. starts the motion again from the beginning.
		/// </summary>
		Circle,

		/// <summary>
		/// Reverses the direction, i.e. moves back to the beginning.
		/// </summary>
		Reverse,

		/// <summary>
		/// Automatically stops when the end has reached.
		/// </summary>
		StopAtEnd,
	}

	/// <summary>
	/// Outputs PinMAME reports as physical, PWM-integrated levels. The values are PinMAME's
	/// <c>CORE_MODOUT_ENABLE_PHYSOUT_*</c> bits, i.e. the mask of <c>PinmameSetSolenoidMask(2, mask)</c>.
	/// </summary>
	[System.Flags]
	public enum PinMamePhysicalOutputFlags
	{
		None = 0,

		/// <summary>
		/// Lamp matrix: 0-255 through the driver's bulb model (e.g. #44 on WPC) instead of 0/1.
		/// </summary>
		Lamps = 0x04,

		/// <summary>
		/// GI strings: 0-255 through the driver's bulb model instead of 0-8.
		/// </summary>
		GI = 0x08,

		/// <summary>
		/// Solenoids: 0-255. Flashers get their bulb model (e.g. #89), plain coils PinMAME's
		/// two-state model, which releases 60 ms after the last pulse.
		/// </summary>
		Solenoids = 0x10,
	}
}
