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

using UnityEditor;
using UnityEngine;

namespace VisualPinball.Unity.Editor
{
	[CustomEditor(typeof(PinMameGamelogicEngine))]
	public class PinMameGamelogicEngineInspector : UnityEditor.Editor
	{
		private PinMameGamelogicEngine _gle;

		private void OnEnable()
		{
			_gle = (PinMameGamelogicEngine) target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUI.BeginDisabledGroup(!Application.isPlaying);

			if (GUILayout.Button("Send initial switches")) {
				_gle.SendInitialSwitches();
			}

			EditorGUI.EndDisabledGroup();
		}
	}
}
