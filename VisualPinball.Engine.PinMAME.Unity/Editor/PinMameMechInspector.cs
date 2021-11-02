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

// ReSharper disable AssignmentInConditionalExpression

using UnityEditor;
using UnityEngine;
using VisualPinball.Engine.PinMAME;
using VisualPinball.Engine.VPT.Primitive;

namespace VisualPinball.Unity.Editor
{
	[CustomEditor(typeof(PinMameMechComponent))]
	public class PinMameMechInspector : ItemInspector
	{
		private SerializedProperty _typeProperty;
		private SerializedProperty _repeatProperty;
		private SerializedProperty _linearMovementProperty;
		private SerializedProperty _fastUpdatesProperty;
		private SerializedProperty _resultByLengthProperty;
		private SerializedProperty _lengthProperty;
		private SerializedProperty _stepsProperty;
		private SerializedProperty _marksProperty;
		private SerializedProperty _accelerationProperty;
		private SerializedProperty _retardationProperty;

		protected override MonoBehaviour UndoTarget => target as MonoBehaviour;

		protected override void OnEnable()
		{
			base.OnEnable();

			_typeProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.Type));
			_repeatProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.Repeat));
			_linearMovementProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.LinearMovement));
			_fastUpdatesProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.FastUpdates));
			_resultByLengthProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.ResultByLength));
			_lengthProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.Length));
			_stepsProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.Steps));
			_marksProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.Marks));
			_accelerationProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.Acceleration));
			_retardationProperty = serializedObject.FindProperty(nameof(PinMameMechComponent.Retardation));
		}

		public override void OnInspectorGUI()
		{
			BeginEditing();

			OnPreInspectorGUI();

			PropertyField(_typeProperty);
			PropertyField(_repeatProperty);
			PropertyField(_lengthProperty);
			PropertyField(_stepsProperty);
			PropertyField(_linearMovementProperty);
			PropertyField(_fastUpdatesProperty);
			PropertyField(_resultByLengthProperty);
			PropertyField(_accelerationProperty);
			PropertyField(_retardationProperty);
			PropertyField(_marksProperty, "Switches");

			base.OnInspectorGUI();

			EndEditing();
		}
	}
}
