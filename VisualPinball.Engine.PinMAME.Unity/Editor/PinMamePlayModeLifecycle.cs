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

		static PinMamePlayModeLifecycle()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.quitting += OnQuitting;
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
		}

		private static void OnBeforeAssemblyReload()
		{
			// Native threads survive a managed domain reload. Join them while their callback
			// delegates are still valid, including ROMs started by editor display discovery.
			RequestStop("Before assembly reload");
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
			} catch {
				return;
			}

			if (_stopping) {
				return;
			}

			_stopping = true;
			try {
				if (runState != 0) {
					Logger.Info($"[PinMAME] Stop requested ({reason}), RunState={runState}");
				}

				// Stop simulation before joining the emulation thread. Include inactive tables,
				// which may have been disabled during a scene or packaged table replacement.
				try {
					var sims = UnityEngine.Object.FindObjectsByType<VisualPinball.Unity.Simulation.SimulationThreadComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
					for (var i = 0; i < sims.Length; i++) {
						sims[i].StopSimulation();
					}
				} catch (Exception e) {
					Logger.Warn(e, "[PinMAME] Could not stop simulation before native shutdown.");
				}

				// Edit-mode components have no runtime subscriptions. Do not set their teardown
				// flags: Unity preserves private fields when hot-reloading scene components.
				if (EditorApplication.isPlaying) {
					try {
						var engines = UnityEngine.Object.FindObjectsByType<VisualPinball.Engine.PinMAME.PinMameGamelogicEngine>(FindObjectsInactive.Include, FindObjectsSortMode.None);
						for (var i = 0; i < engines.Length; i++) {
							engines[i].StopForPlayModeExit();
						}
					} catch (Exception e) {
						Logger.Warn(e, "[PinMAME] Could not detach table callbacks before native shutdown.");
					}
				}

				// A display probe can run without a table component. Release its time fence too.
				PinMame.PinMame.SetTimeFence(0.0);
				// Stop synchronously: deferring this until after reload would call unloaded managed
				// callbacks. Even state 0 can leave a finished native thread that needs joining.
				var sw = Stopwatch.StartNew();
				PinMame.PinMame.StopRunningGame();
				if (runState != 0 || sw.ElapsedMilliseconds > 50) {
					Logger.Info($"[PinMAME] Stop call returned after {sw.ElapsedMilliseconds}ms, RunState={PinMame.PinMame.RunState}");
				}
			} catch (Exception e) {
				Logger.Warn(e, "[PinMAME] Native shutdown failed ({Reason}).", reason);
			} finally {
				_stopping = false;
			}
		}
	}
}

#endif
