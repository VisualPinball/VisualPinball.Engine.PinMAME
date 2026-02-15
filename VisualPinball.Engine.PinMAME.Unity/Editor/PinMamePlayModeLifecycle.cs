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

#if UNITY_EDITOR

using System;
using System.Diagnostics;
using NLog;
using UnityEditor;
using UnityEngine;
using Logger = NLog.Logger;


// ReSharper disable CheckNamespace

namespace VisualPinball.Engine.PinMAME.Editor
{
	[InitializeOnLoad]
	public static class PinMamePlayModeLifecycle
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static bool _stopping;
		private static Stopwatch _stopwatch;
		private const int StopTimeoutMs = 2000;
		private static bool _warnedTimeout;
		private static System.Threading.Tasks.Task _stopTask;

		// (removed) Domain reload branching; stop is synchronous for determinism.

		static PinMamePlayModeLifecycle()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.quitting += OnQuitting;
		}

		private static void OnQuitting()
		{
			RequestStop("Editor quitting");
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode) {
				RequestStop("Exiting play mode");
			}
		}

		private static void RequestStop(string reason)
		{
			int runState;
			try {
				runState = PinMame.PinMame.RunState;
				if (runState == 0) {
					return;
				}
			} catch {
				return;
			}

			if (_stopping) {
				return;
			}

			_stopping = true;
			_warnedTimeout = false;
			_stopwatch = Stopwatch.StartNew();
			Logger.Info($"[PinMAME] Stop requested ({reason}), RunState={runState}");

			// Stop sim thread(s) first. This reduces the chance of other high-frequency code paths
			// continuing to call into PinMAME while we are stopping it.
			try {
				var sims = UnityEngine.Object.FindObjectsByType<VisualPinball.Unity.Simulation.SimulationThreadComponent>(FindObjectsSortMode.None);
				for (var i = 0; i < sims.Length; i++) {
					sims[i].StopSimulation();
				}
			} catch { }

			// Prefer stopping via the active component(s) so they can unsubscribe callbacks first.
			try {
				var engines = UnityEngine.Object.FindObjectsByType<VisualPinball.Engine.PinMAME.PinMameGamelogicEngine>(FindObjectsSortMode.None);
				for (var i = 0; i < engines.Length; i++) {
					engines[i].StopForPlayModeExit();
				}
			} catch { }

			EditorApplication.update -= Update;
			EditorApplication.update += Update;

			// Stop PinMAME now.
			// With Domain Reload disabled, background stop can overlap the next play session.
			// Now that other hot loops are tamed, prefer a synchronous stop for determinism.
			try {
				var sw = Stopwatch.StartNew();
				PinMame.PinMame.StopRunningGame();
				Logger.Info($"[PinMAME] Stop call returned after {sw.ElapsedMilliseconds}ms");
			} catch (Exception e) {
				Logger.Warn(e, "[PinMAME] StopGame failed while exiting play mode.");
			}
			_stopTask = System.Threading.Tasks.Task.CompletedTask;
		}

		private static void Update()
		{
			int runState;
			try {
				runState = PinMame.PinMame.RunState;
			} catch {
				runState = 0;
			}
			var running = runState != 0;

			var stopDone = _stopTask == null || _stopTask.IsCompleted;
			if (!running && stopDone) {
				EditorApplication.update -= Update;
				_stopping = false;
				_warnedTimeout = false;
				_stopTask = null;
				Logger.Info("[PinMAME] Stopped (editor)");
				return;
			}

			if (!_warnedTimeout && _stopwatch != null && _stopwatch.ElapsedMilliseconds > StopTimeoutMs) {
				_warnedTimeout = true;
				Logger.Warn($"[PinMAME] Still running after stop timeout (editor), RunState={runState}");
			}
		}
	}
}

#endif
