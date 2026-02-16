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

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using PinMame;
using UnityEngine;
using UnityEngine.InputSystem;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.PinMAME
{
	[Serializable]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(AudioSource))]
	[AddComponentMenu("Pinball/Gamelogic Engine/PinMAME")]
	public class PinMameGamelogicEngine : MonoBehaviour, IGamelogicEngine, IGamelogicInputThreading
	{
		public string Name { get; } = "PinMAME Gamelogic Engine";
		public GamelogicInputDispatchMode SwitchDispatchMode => GamelogicInputDispatchMode.SimulationThread;

		public const string DmdPrefix = "dmd";
		public const string SegDispPrefix = "display";

		public PinMameGame Game { get => _game; set => _game = value; }

		#region Configuration

		[HideInInspector]
		public string romId = string.Empty;

		[Tooltip("Disable built-in mechs")]
		public bool DisableMechs = true;

		[Min(0f)]
		[Tooltip("Delay after startup to listen for solenoid events.")]
		public float SolenoidDelay;

		[Tooltip("Disable audio")]
		public bool DisableAudio;

		#endregion

		#region IGamelogicEngine

		public GamelogicEngineSwitch[] RequestedSwitches {
			get {
				UpdateCaches();
				return _game?.AvailableSwitches ?? Array.Empty<GamelogicEngineSwitch>();
			}
		}

		public GamelogicEngineCoil[] RequestedCoils {
			get {
				UpdateCaches();
				return _coils.Values.ToArray();
			}
		}

		public GamelogicEngineLamp[] RequestedLamps {
			get {
				UpdateCaches();
				return _lamps.Values.ToArray();
			}
		}

		public GamelogicEngineWire[] AvailableWires => _game?.AvailableWires ?? Array.Empty<GamelogicEngineWire>();

		public event EventHandler<CoilEventArgs> OnCoilChanged;
		public event EventHandler<SwitchEventArgs2> OnSwitchChanged;
		public event EventHandler<LampEventArgs> OnLampChanged;
		public event EventHandler<LampsEventArgs> OnLampsChanged;
		public event EventHandler<RequestedDisplays> OnDisplaysRequested;
		public event EventHandler<string> OnDisplayClear;
		public event EventHandler<DisplayFrameData> OnDisplayUpdateFrame;
		public event EventHandler<EventArgs> OnStarted;

		#endregion

		#region Internals

		[NonSerialized] private Player _player;
		[NonSerialized] private PinMame.PinMame _pinMame;
		[NonSerialized] private BallManager _ballManager;
		[NonSerialized] private PlayfieldComponent _playfieldComponent;

		[NonSerialized] private readonly List<PinMameLampInfo> _changedLamps = new();
		[NonSerialized] private readonly List<PinMameLampInfo> _changedGIs = new();

		[SerializeReference] private PinMameGame _game;

		private Dictionary<string, GamelogicEngineSwitch> _switches = new();
		private Dictionary<int, string> _pinMameIdToSwitchIdMappings = new();
		private Dictionary<string, int> _switchIdToPinMameIdMappings = new();

		private Dictionary<string, GamelogicEngineCoil> _coils = new();
		private Dictionary<int, string> _pinMameIdToCoilIdMapping = new();
		private Dictionary<string, int> _coilIdToPinMameIdMapping = new();

		private Dictionary<string, GamelogicEngineLamp> _lamps = new();
		private Dictionary<int, string> _pinMameIdToLampIdMapping = new();

		private volatile bool _isRunning;
		private int _numMechs;
		private Dictionary<int, byte[]> _frameBuffer = new();
		private Dictionary<int, Dictionary<byte, byte>> _dmdLevels = new();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly Color Tint = new(1, 0.18f, 0);

		private readonly Queue<Action> _dispatchQueue = new();
		private readonly Queue<float[]> _audioQueue = new();

		private int _audioFilterChannels;
		private PinMameAudioInfo _audioInfo;
		private float[] _lastAudioFrame = {};
		private int _lastAudioFrameOffset;
		private const int _maximalQueueSize = 10;

		private double _audioInputStart;
		private double _audioOutputStart;
		private int _audioNumSamplesInput;
		private int _audioNumSamplesOutput;

		public bool _solenoidsEnabled;
		public long _solenoidDelayStart;
		private Dictionary<int, PinMameMechComponent> _registeredMechs = new();
		private Dictionary<int, string> _registeredMechNames = new();
		private HashSet<int> _mechSwitches = new();

		private bool _toggleSpeed = false;
		private Keyboard _keyboard;

		private static readonly SemaphoreSlim PinMameStartStopGate = new SemaphoreSlim(1, 1);
		private int _onInitCalled;
		private int _stopRequested;
		private int _playModeExitHandled;
		// (removed) lamp delta debug tracking
		private const int StopTimeoutMs = 10000;

		#endregion

		#region Lifecycle

		private void Awake()
		{
			Logger.Info("Project audio sample rate: " +  AudioSettings.outputSampleRate);
			_keyboard = Keyboard.current;
		}

		private void Start()
		{
			UpdateCaches();

			_lastAudioFrame = Array.Empty<float>();
			_lastAudioFrameOffset = 0;
		}

		private void Update()
		{
			if (_pinMame == null || !_isRunning) {
				return;
			}

			lock (_dispatchQueue) {
				while (_dispatchQueue.Count > 0) {
					var callback = _dispatchQueue.Dequeue();
					try {
						callback.Invoke();
					}
					catch (Exception e) {
						Logger.Error(e, "[PinMAME] Exception while processing main-thread dispatch callback.");
					}
				}
			}

			// lamps
			_pinMame.GetChangedLamps(_changedLamps);
			foreach (var changedLamp in _changedLamps) {
				if (_pinMameIdToLampIdMapping.ContainsKey(changedLamp.Id)) {
					//Logger.Info($"[PinMAME] <= lamp {changedLamp.Id}: {changedLamp.Value}");
					OnLampChanged?.Invoke(this, new LampEventArgs(_lamps[_pinMameIdToLampIdMapping[changedLamp.Id]].Id, changedLamp.Value));
				}
			}

			// gi
			_pinMame.GetChangedGIs(_changedGIs);
			foreach (var changedGi in _changedGIs) {
				if (_pinMameIdToLampIdMapping.ContainsKey(changedGi.Id)) {
					//Logger.Info($"[PinMAME] <= gi {changedGi.Id}: {changedGi.Value}");
					OnLampChanged?.Invoke(this, new LampEventArgs(_lamps[_pinMameIdToLampIdMapping[changedGi.Id]].Id, changedGi.Value, LampSource.GI));
				} /*else {
					Logger.Info($"No GI {changedGi.Id} found.");
				}*/
			}

			// if (_keyboard != null && _keyboard.cKey.wasPressedThisFrame)
			// {
			// 	OnCoilChanged.Invoke(this, new CoilEventArgs("28", true));
			// 	OnCoilChanged.Invoke(this, new CoilEventArgs("28", false));
			// }
		}

		private void OnDestroy()
		{
			#if UNITY_EDITOR
			StopForPlayModeExit();
			#else
			RequestStopGame("OnDestroy");
			#endif
			if (_pinMame != null) {
				_pinMame.OnGameStarted -= OnGameStarted;
				_pinMame.OnGameEnded -= OnGameEnded;
				_pinMame.OnDisplayAvailable -= OnDisplayRequested;
				_pinMame.OnDisplayUpdated -= OnDisplayUpdated;

				if (!DisableAudio)
				{
					_pinMame.OnAudioAvailable -= OnAudioAvailable;
					_pinMame.OnAudioUpdated -= OnAudioUpdated;
				}

				_pinMame.OnMechAvailable -= OnMechAvailable;
				_pinMame.OnMechUpdated -= OnMechUpdated;
				_pinMame.OnSolenoidUpdated -= OnSolenoidUpdated;
				_pinMame.IsKeyPressed -= IsKeyPressed;
			}
			_frameBuffer.Clear();
			_dmdLevels.Clear();
		}

		private void OnDisable()
		{
			// In some Unity playmode/domain-reload configurations, OnDestroy isn't reliably called.
			// Best-effort shutdown to avoid leaving a game running across sessions.
			#if UNITY_EDITOR
			StopForPlayModeExit();
			#else
			RequestStopGame("OnDisable");
			#endif
		}

		private void OnApplicationQuit()
		{
			#if UNITY_EDITOR
			StopForPlayModeExit();
			#else
			RequestStopGame("OnApplicationQuit");
			#endif
		}

		public void StopForPlayModeExit()
		{
			if (Interlocked.Exchange(ref _playModeExitHandled, 1) != 0) {
				return;
			}

			// Editor-only: called from playmode lifecycle hook.
			// Do not call into native Stop() here (it blocks); the editor hook will stop PinMAME
			// after we unsubscribe managed callbacks.
			Interlocked.Exchange(ref _stopRequested, 1);
			// Stop polling outputs immediately to avoid concurrent calls into pinmame.dll
			// while a stop is in progress (pinmame APIs are not guaranteed thread-safe).
			_isRunning = false;
			Logger.Info($"[PinMAME] StopForPlayModeExit: unsubscribing callbacks ({name}#{GetInstanceID()})");
			try {
				if (_pinMame == null) {
					return;
				}

				_pinMame.OnGameStarted -= OnGameStarted;
				_pinMame.OnGameEnded -= OnGameEnded;
				_pinMame.OnDisplayAvailable -= OnDisplayRequested;
				_pinMame.OnDisplayUpdated -= OnDisplayUpdated;
				_pinMame.OnMechAvailable -= OnMechAvailable;
				_pinMame.OnMechUpdated -= OnMechUpdated;
				_pinMame.OnSolenoidUpdated -= OnSolenoidUpdated;
				_pinMame.IsKeyPressed -= IsKeyPressed;
				_pinMame.OnAudioAvailable -= OnAudioAvailable;
				_pinMame.OnAudioUpdated -= OnAudioUpdated;
			}
			catch { }
		}

		private void RequestStopGame(string reason)
		{
			#if UNITY_EDITOR
			// In the editor, playmode shutdown is handled by StopForPlayModeExit + editor lifecycle hook.
			// Calling native Stop() from here can race and destabilize the editor.
			StopForPlayModeExit();
			return;
			#endif

			try {
				if (Interlocked.Exchange(ref _stopRequested, 1) != 0) {
					return;
				}
				_isRunning = false;

				if (!PinMame.PinMame.IsRunning) {
					return;
				}

			} catch (Exception e) {
				Logger.Warn(e, $"[PinMAME] StopGame ({reason}) failed.");
				return;
			}

			// Avoid racing StartGame on re-entering play mode.
			if (!PinMameStartStopGate.Wait(0)) {
				Logger.Warn($"[PinMAME] StopGame ({reason}) skipped: start/stop already in progress.");
				return;
			}

			// Never block the Unity main thread during playmode exit.
			// PinMAME shutdown is best-effort here; OnInit() and the editor lifecycle hook
			// will recover/ensure shutdown before starting a new ROM.
			Logger.Warn($"[PinMAME] StopGame ({reason}) requested. RunningGame={_pinMame?.RunningGame}");

			try {
				if (_pinMame != null) {
					// Unsubscribe first to keep the native thread from doing work in managed callbacks during shutdown.
					_pinMame.OnGameStarted -= OnGameStarted;
					_pinMame.OnGameEnded -= OnGameEnded;
					_pinMame.OnDisplayAvailable -= OnDisplayRequested;
					_pinMame.OnDisplayUpdated -= OnDisplayUpdated;
					_pinMame.OnMechAvailable -= OnMechAvailable;
					_pinMame.OnMechUpdated -= OnMechUpdated;
					_pinMame.OnSolenoidUpdated -= OnSolenoidUpdated;
					_pinMame.IsKeyPressed -= IsKeyPressed;
					_pinMame.OnAudioAvailable -= OnAudioAvailable;
					_pinMame.OnAudioUpdated -= OnAudioUpdated;
				}
			}
			catch (Exception e) {
				Logger.Warn(e, $"[PinMAME] Unsubscribe during shutdown ({reason}) failed.");
			}

			// PinmameStop may block (joins the emulation thread). Never do that on the Unity main thread.
			System.Threading.Tasks.Task.Run(() => {
				try {
					_pinMame?.StopGame();
				} catch (Exception e) {
					Logger.Warn(e, $"[PinMAME] StopGame ({reason}) failed.");
				} finally {
					PinMameStartStopGate.Release();
				}
			});
		}

		public async Task OnInit(Player player, TableApi tableApi, BallManager ballManager, CancellationToken ct)
		{
			if (Interlocked.Exchange(ref _onInitCalled, 1) != 0) {
				Logger.Warn($"[PinMAME] OnInit called more than once on {name}, ignoring.");
				return;
			}

			string vpmPath = null;
			_ballManager = ballManager;
			_playfieldComponent = GetComponentInChildren<PlayfieldComponent>();

			#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
				vpmPath = Path.Combine(Application.persistentDataPath, "pinmame");
				Directory.CreateDirectory(Path.Combine(vpmPath, "roms"));

				byte[] data = null;

				#if UNITY_IOS
					data = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, $"{romId}.zip"));
				#else
					UnityWebRequest webRequest = new UnityWebRequest(Path.Combine(Application.streamingAssetsPath, $"{romId}.zip"));

					webRequest.downloadHandler = new DownloadHandlerBuffer();
					webRequest.SendWebRequest();

					while (!webRequest.isDone) { };

					data = webRequest.downloadHandler.data;
				#endif

				File.WriteAllBytes(Path.Combine(vpmPath, "roms", $"{romId}.zip"), data);
			#endif

			await PinMameStartStopGate.WaitAsync(ct);
			try {
				Logger.Info($"[PinMAME] OnInit {name}: romId={romId}, sampleRate={AudioSettings.outputSampleRate}");

				#if UNITY_EDITOR
				// With Domain Reload disabled, PinMAME (native + managed singleton) persists across play sessions.
				// Even when RunState reports 0, the native game thread might still exist and need joining.
				// Calling Stop here is a cheap no-op if nothing is running, and a critical cleanup if something is.
				var preStopState = PinMame.PinMame.RunState;
				var preStopSw = Stopwatch.StartNew();
				try {
					PinMame.PinMame.StopRunningGame();
				} catch (Exception e) {
					Logger.Warn(e, "[PinMAME] Pre-start StopRunningGame failed.");
				}
				// Wait for a clean stopped state before starting a new ROM.
				var preStopWaitSw = Stopwatch.StartNew();
				while (PinMame.PinMame.RunState != 0 && preStopWaitSw.ElapsedMilliseconds < StopTimeoutMs) {
					await Task.Delay(10, ct);
				}
				Logger.Debug($"[PinMAME] Editor pre-stop: initialRunState={preStopState}, stopCallMs={preStopSw.ElapsedMilliseconds}, waitMs={preStopWaitSw.ElapsedMilliseconds}, finalRunState={PinMame.PinMame.RunState}");
				if (PinMame.PinMame.RunState != 0) {
					Logger.Error($"[PinMAME] Cannot start ROM; PinMAME did not reach stopped state (RunState={PinMame.PinMame.RunState}).");
					return;
				}

				// Force a fresh managed wrapper each init. This avoids cross-session managed state
				// (callbacks, buffers) bleeding into the next play run when the host keeps assemblies loaded.
				PinMame.PinMame.ResetInstance();
				#endif

				// If we exited play mode recently, a background stop may still be running.
				// Starting a new ROM while the previous stop is cleaning up can lead to a "started"
				// state with no progress/output on the next run.
				var wasRunning = PinMame.PinMame.IsRunning;
				var wasStopping = PinMame.PinMame.IsStopInProgress;
				var swStop = Stopwatch.StartNew();
				while ((PinMame.PinMame.IsStopInProgress || PinMame.PinMame.IsRunning) && swStop.ElapsedMilliseconds < StopTimeoutMs) {
					await Task.Delay(10, ct);
				}
				Logger.Debug($"[PinMAME] Pre-start wait: {swStop.ElapsedMilliseconds}ms (WasRunning={wasRunning}, WasStopping={wasStopping}, IsRunning={PinMame.PinMame.IsRunning}, RunState={PinMame.PinMame.RunState}, StopInProgress={PinMame.PinMame.IsStopInProgress})");

				// ReSharper disable once ExpressionIsAlwaysNull
				_pinMame = PinMame.PinMame.Instance(PinMameAudioFormat.AudioFormatFloat, AudioSettings.outputSampleRate, vpmPath);
				// Re-apply config each init (important when Domain Reload is disabled).
				_pinMame.ApplyConfig();

				// Prevent duplicate subscriptions if OnInit is invoked more than once.
				_pinMame.OnGameStarted -= OnGameStarted;
				_pinMame.OnGameEnded -= OnGameEnded;
				_pinMame.OnDisplayAvailable -= OnDisplayRequested;
				_pinMame.OnDisplayUpdated -= OnDisplayUpdated;
				_pinMame.OnMechAvailable -= OnMechAvailable;
				_pinMame.OnMechUpdated -= OnMechUpdated;
				_pinMame.OnSolenoidUpdated -= OnSolenoidUpdated;
				_pinMame.IsKeyPressed -= IsKeyPressed;
				_pinMame.OnAudioAvailable -= OnAudioAvailable;
				_pinMame.OnAudioUpdated -= OnAudioUpdated;

				// If domain reload is disabled or a previous table didn't shut down cleanly,
				// PinMAME can still be running here.
				if (PinMame.PinMame.IsRunning) {
					Logger.Warn($"[PinMAME] A game is already running (RunningGame={_pinMame.RunningGame}); stopping it before starting a new one.");
					try {
						_pinMame.StopGame();
					}
					catch (Exception e) {
						Logger.Warn(e, "[PinMAME] StopGame failed while recovering from already running state.");
					}

					var sw = Stopwatch.StartNew();
					while (PinMame.PinMame.IsRunning && sw.ElapsedMilliseconds < StopTimeoutMs) {
						await Task.Delay(10, ct);
					}
					if (PinMame.PinMame.IsRunning) {
						Logger.Warn("[PinMAME] Failed to stop running game within timeout; attempting StartGame anyway.");
					}
				}

				_pinMame.SetHandleKeyboard(false);
				_pinMame.SetHandleMechanics(DisableMechs ? 0 : 0xFF);

				_pinMame.OnGameStarted += OnGameStarted;
				_pinMame.OnGameEnded += OnGameEnded;
				_pinMame.OnDisplayAvailable += OnDisplayRequested;
				_pinMame.OnDisplayUpdated += OnDisplayUpdated;

				if (!DisableAudio) {
					_pinMame.OnAudioAvailable += OnAudioAvailable;
					_pinMame.OnAudioUpdated += OnAudioUpdated;
				}

				_pinMame.OnMechAvailable += OnMechAvailable;
				_pinMame.OnMechUpdated += OnMechUpdated;
				_pinMame.OnSolenoidUpdated += OnSolenoidUpdated;
				_pinMame.IsKeyPressed += IsKeyPressed;

				_player = player;
				_solenoidsEnabled = SolenoidDelay == 0;

				try {
					_pinMame.StartGame(romId);
					Logger.Debug($"[PinMAME] StartGame returned. RunState={PinMame.PinMame.RunState}");
				}
				catch (InvalidOperationException e) when (e.Message != null && e.Message.Contains("status=GAME_ALREADY_RUNNING"))
				{
					Logger.Warn(e, "[PinMAME] StartGame reported already running; stopping and retrying once.");
					try {
						_pinMame.StopGame();
						await Task.Delay(100, ct);
						_pinMame.StartGame(romId);
						Logger.Debug($"[PinMAME] StartGame retry returned. RunState={PinMame.PinMame.RunState}");
					}
					catch (Exception retryEx) {
						Logger.Error(retryEx);
					}
				}
				catch (Exception e) {
					Logger.Error(e);
				}

			} finally {
				PinMameStartStopGate.Release();
			}
		}

		public void ToggleSpeed()
		{
			Logger.Info("[PinMAME] Toggle speed.");

			_pinMame.SetHandleKeyboard(true);
			_toggleSpeed = true;
		}

		private void OnGameStarted()
		{
			Logger.Info("[PinMAME] Game started.");
			_isRunning = true;

			_solenoidDelayStart = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			try {
				SendInitialSwitches();
				SendMechs();
			}

			catch(Exception e) {
				Logger.Error($"[PinMAME] OnGameStarted: {e.Message}");
			}

			lock (_dispatchQueue) {
				_dispatchQueue.Enqueue(() => OnStarted?.Invoke(this, EventArgs.Empty));
			}
		}

		private void OnGameEnded()
		{
			// Native can report ended more than once during teardown; keep it idempotent.
			if (_isRunning) {
				Logger.Info("[PinMAME] Game ended.");
			}
			_isRunning = false;
		}

		private void UpdateCaches()
		{
			if (_game == null) {
				return;
			}

			_lamps.Clear();
			_pinMameIdToLampIdMapping.Clear();

			_coils.Clear();
			_pinMameIdToCoilIdMapping.Clear();

			_switches.Clear();
			_pinMameIdToSwitchIdMappings.Clear();
			_switchIdToPinMameIdMappings.Clear();

			// check aliases first (the switches/coils that aren't an integer)
			foreach (var alias in _game.AvailableAliases) {
				switch (alias.AliasType) {
					case AliasType.Switch:
						_pinMameIdToSwitchIdMappings[alias.Id] = alias.Name;
						_switchIdToPinMameIdMappings[alias.Name] = alias.Id;
						break;


					case AliasType.Coil:
						_pinMameIdToCoilIdMapping[alias.Id] = alias.Name;
						_coilIdToPinMameIdMapping[alias.Name] = alias.Id;
						break;

					case AliasType.Lamp:
						_pinMameIdToLampIdMapping[alias.Id] = alias.Name;
						break;
				}
			}

			// retrieve the game's switches
			foreach (var @switch in _game.AvailableSwitches) {
				_switches[@switch.Id] = @switch;

				if (int.TryParse(@switch.Id, out var pinMameId)) {
					_pinMameIdToSwitchIdMappings[pinMameId] = @switch.Id;
					_switchIdToPinMameIdMappings[@switch.Id] = pinMameId;

					// add mappings with prefixed 0.
					if (pinMameId < 10) {
						_switchIdToPinMameIdMappings["0" + @switch.Id] = pinMameId;
						_switchIdToPinMameIdMappings["00" + @switch.Id] = pinMameId;

						_switches["0" + @switch.Id] = @switch;
						_switches["00" + @switch.Id] = @switch;
					}
					if (pinMameId < 100) {
						_switchIdToPinMameIdMappings["0" + @switch.Id] = pinMameId;
						_switches["0" + @switch.Id] = @switch;
					}
				}
			}

			// retrieve the game's coils
			foreach (var coil in _game.AvailableCoils) {
				_coils[coil.Id] = coil;

				if (int.TryParse(coil.Id, out int pinMameId)) {
					_pinMameIdToCoilIdMapping[pinMameId] = coil.Id;
					_coilIdToPinMameIdMapping[coil.Id] = pinMameId;
				}
			}

			// retrieve the game's lamps
			foreach (var lamp in _game.AvailableLamps) {
				_lamps[lamp.Id] = lamp;

				if (int.TryParse(lamp.Id, out int pinMameId)) {
					_pinMameIdToLampIdMapping[pinMameId] = lamp.Id;
				}
			}
		}

		#endregion

		#region Displays

		private void OnDisplayRequested(int index, int displayCount, PinMameDisplayLayout displayLayout)
		{
			if (displayLayout.IsDmd) {
				lock (_dispatchQueue) {
					_dispatchQueue.Enqueue(() =>
						OnDisplaysRequested?.Invoke(this, new RequestedDisplays(
							new DisplayConfig($"{DmdPrefix}{index}", displayLayout.Width, displayLayout.Height))));
				}

				_frameBuffer[index] = new byte[displayLayout.Width * displayLayout.Height];
				_dmdLevels[index] = displayLayout.Levels;

			} else {
				lock (_dispatchQueue) {
					_dispatchQueue.Enqueue(() =>
						OnDisplaysRequested?.Invoke(this, new RequestedDisplays(
							new DisplayConfig($"{SegDispPrefix}{index}", displayLayout.Length, 1))));
				}

				_frameBuffer[index] = new byte[displayLayout.Length * 2];
				Logger.Info($"[PinMAME] Display {SegDispPrefix}{index} is of type {displayLayout.Type} at {displayLayout.Length} wide.");
			}
		}

		private void OnDisplayUpdated(int index, IntPtr framePtr, PinMameDisplayLayout displayLayout)
		{
			if (!_frameBuffer.ContainsKey(index)) {
				Logger.Warn($"[PinMAME] Dropping display frame for unknown index {index} (layout: {displayLayout}).");
				return;
			}
			if (displayLayout.IsDmd) {
				UpdateDmd(index, displayLayout, framePtr);

			} else {
				UpdateSegDisp(index, displayLayout, framePtr);
			}
		}

		private void UpdateDmd(int index, PinMameDisplayLayout displayLayout, IntPtr framePtr)
		{
			unsafe {
				var ptr = (byte*) framePtr;
				for (var y = 0; y < displayLayout.Height; y++) {
					for (var x = 0; x < displayLayout.Width; x++) {
						var pos = y * displayLayout.Width + x;
						if (!_dmdLevels[index].ContainsKey(ptr[pos])) {
							Logger.Error($"Display {index}: Provided levels ({BitConverter.ToString(_dmdLevels[index].Keys.ToArray())}) don't contain level {BitConverter.ToString(new[] {ptr[pos]})}.");
							_dmdLevels[index][ptr[pos]] = 0x4;
						}
						_frameBuffer[index][pos] = _dmdLevels[index][ptr[pos]];
					}
				}
			}

			lock (_dispatchQueue) {
				_dispatchQueue.Enqueue(() => OnDisplayUpdateFrame?.Invoke(this,
					new DisplayFrameData($"{DmdPrefix}{index}", GetDisplayFrameFormat(displayLayout), _frameBuffer[index])));
			}
		}

		private void UpdateSegDisp(int index, PinMameDisplayLayout displayLayout, IntPtr framePtr)
		{
			Marshal.Copy(framePtr, _frameBuffer[index], 0, displayLayout.Length * 2);

			lock (_dispatchQueue) {
				//Logger.Info($"[PinMAME] Seg data ({index}): {BitConverter.ToString(_frameBuffer[index])}" );
				_dispatchQueue.Enqueue(() => OnDisplayUpdateFrame?.Invoke(this,
					new DisplayFrameData($"{SegDispPrefix}{index}", GetDisplayFrameFormat(displayLayout), _frameBuffer[index])));
			}
		}

		public static DisplayFrameFormat GetDisplayFrameFormat(PinMameDisplayLayout layout)
		{
			if (layout.IsDmd) {
				return layout.Depth == 4 ? DisplayFrameFormat.Dmd4 : DisplayFrameFormat.Dmd2;
			}

			switch (layout.Type) {
				case PinMameDisplayType.Seg8:   // 7  segments and comma
				case PinMameDisplayType.Seg7SC: // 7  segments, small, with comma
				case PinMameDisplayType.Seg8D:  // 7  segments and period
				case PinMameDisplayType.Seg7:  // 7  segments
				case PinMameDisplayType.Seg7S: // 7  segments, small
				case PinMameDisplayType.Seg87:  // 7  segments, comma every three
				case PinMameDisplayType.Seg87F: // 7  segments, forced comma every three
				case PinMameDisplayType.Seg10: // 9  segments and comma
				case PinMameDisplayType.Seg9: // 9  segments
				case PinMameDisplayType.Seg98: // 9  segments, comma every three
				case PinMameDisplayType.Seg98F: // 9  segments, forced comma every three
				case PinMameDisplayType.Seg16:  // 16 segments
				case PinMameDisplayType.Seg16R: // 16 segments with comma and period reversed
				case PinMameDisplayType.Seg16N: // 16 segments without commas
				case PinMameDisplayType.Seg16D: // 16 segments with periods only
				case PinMameDisplayType.Seg16S: // 16 segments with split top and bottom line
				case PinMameDisplayType.Seg8H:
				case PinMameDisplayType.Seg7H:
				case PinMameDisplayType.Seg87H:
				case PinMameDisplayType.Seg87FH:
				case PinMameDisplayType.Seg7SH:
				case PinMameDisplayType.Seg7SCH:
				case PinMameDisplayType.Seg7 | PinMameDisplayType.NoDisp:
					return DisplayFrameFormat.Segment;

				case PinMameDisplayType.Video:
					break;

				case PinMameDisplayType.SegAll:
				case PinMameDisplayType.Import:
				case PinMameDisplayType.SegMask:
				case PinMameDisplayType.SegHiBit:
				case PinMameDisplayType.SegRev:
				case PinMameDisplayType.DmdNoAA:
				case PinMameDisplayType.NoDisp:
					throw new ArgumentOutOfRangeException(nameof(layout), layout, null);

				default:
					throw new ArgumentOutOfRangeException(nameof(layout), layout, null);
			}

			throw new NotImplementedException($"Still unsupported segmented display format: {layout}.");
		}

		public void DisplayChanged(DisplayFrameData displayFrameData)
		{
		}

		#endregion

		#region Audio

		private int OnAudioAvailable(PinMameAudioInfo audioInfo)
		{
			Logger.Info("Game audio available: " + audioInfo);

			_audioInfo = audioInfo;
			return _audioInfo.SamplesPerFrame;
		}

		private int OnAudioUpdated(IntPtr framePtr, int frameSize)
		{
			if (_audioFilterChannels == 0) {
				// don't know how many channels yet
				return _audioInfo.SamplesPerFrame;
			}

			_audioNumSamplesInput += frameSize;
			if (_audioNumSamplesInput > 100000) {
				// var delta = AudioSettings.dspTime - _audioInputStart;
				// var queueMs = System.Math.Round(_audioQueue.Count * (double)_audioInfo.SamplesPerFrame / _audioInfo.SampleRate * 1000);
				// Logger.Info($"INPUT: {System.Math.Round(_audioNumSamplesInput / delta)} - {_audioQueue.Count} in queue ({queueMs}ms)");
				_audioInputStart = AudioSettings.dspTime;
				_audioNumSamplesInput = 0;
			}

			float[] frame;
			if (_audioFilterChannels == _audioInfo.Channels) { // n channels -> n channels
				frame = new float[frameSize * 2];
				unsafe {
					var src = (float*)framePtr;
					for (var i = 0; i < frameSize * 2; i++) {
						frame[i] = src[i];
					}
				}
			} else if (_audioFilterChannels > _audioInfo.Channels) { // 1 channel -> 2 channels
				frame = new float[frameSize * 2];
				unsafe {
					var src = (float*)framePtr;
					for (var i = 0; i < frameSize; i++) {
						frame[i * 2] = src[i];
						frame[i * 2 + 1] = frame[i * 2];
					}
				}
			} else { // 2 channels -> 1 channel
				frame = new float[frameSize / 2];
				unsafe {
					var src = (float*)framePtr;
					for (var i = 0; i < frameSize; i += 2) {
						frame[i] = src[i];
					}
				}
			}

			lock (_audioQueue) {
				if (_audioQueue.Count >= _maximalQueueSize) {
					_audioQueue.Clear();
					Logger.Error("Clearing full audio frame queue.");
				}
				_audioQueue.Enqueue(frame);
			}

			return _audioInfo.SamplesPerFrame;
		}

		private void OnAudioFilterRead(float[] data, int channels)
		{
			_audioNumSamplesOutput += data.Length / channels;
			if (_audioNumSamplesOutput > 100000) {
				//var delta = AudioSettings.dspTime - _audioOutputStart;
				//Logger.Info($"OUTPUT: {System.Math.Round(_audioNumSamplesOutput / delta)}");
				_audioOutputStart = AudioSettings.dspTime;
				_audioNumSamplesOutput = 0;
			}

			if (_audioFilterChannels == 0) {
				_audioFilterChannels = channels;
				Logger.Info($"Creating audio on {channels} channels.");
				return;
			}

			if (_audioQueue.Count == 0) {
				if (!DisableAudio) {
					Logger.Info("Filtering audio but nothing to de-queue.");
				}
				return;
			}


			const int size = sizeof(float);
			var dataOffset = 0;
			var lastFrameSize = _lastAudioFrame.Length - _lastAudioFrameOffset;
			if (data.Length >= lastFrameSize) {
				if (lastFrameSize > 0) {
					Buffer.BlockCopy(_lastAudioFrame, _lastAudioFrameOffset * size, data, 0, lastFrameSize * size);
					dataOffset += lastFrameSize;
				}
				_lastAudioFrame = Array.Empty<float>();
				_lastAudioFrameOffset = 0;

				lock (_audioQueue) {
					while (dataOffset < data.Length && _audioQueue.Count > 0) {
						var frame = _audioQueue.Dequeue();
						if (frame.Length <= data.Length - dataOffset) {
							Buffer.BlockCopy(frame, 0, data, dataOffset * size, frame.Length * size);
							dataOffset += frame.Length;

						} else {
							Buffer.BlockCopy(frame, 0, data, dataOffset * size, (data.Length - dataOffset) * size);
							_lastAudioFrame = frame;
							_lastAudioFrameOffset = data.Length - dataOffset;
							dataOffset = data.Length;
						}
					}
				}

			} else {
				Buffer.BlockCopy(_lastAudioFrame, _lastAudioFrameOffset * size, data, 0, data.Length * size);
				_lastAudioFrameOffset += data.Length;
			}
		}

		#endregion

		#region Coils

		public void SetCoil(string n, bool value)
		{
			OnCoilChanged?.Invoke(this, new CoilEventArgs(n, value));
		}

		public bool GetCoil(string id)
		{
			return _player != null && _player.CoilStatuses.ContainsKey(id) && _player.CoilStatuses[id];
		}

		private void OnSolenoidUpdated(int id, bool isActive)
		{
			if (_pinMameIdToCoilIdMapping.ContainsKey(id)) {
				if (!_solenoidsEnabled) {
					_solenoidsEnabled = DateTimeOffset.Now.ToUnixTimeMilliseconds() - _solenoidDelayStart >= SolenoidDelay;

					if (_solenoidsEnabled) {
						Logger.Info($"Solenoids enabled, {SolenoidDelay}ms passed");
					}
				}

				var coil = _coils[_pinMameIdToCoilIdMapping[id]];

				if (_solenoidsEnabled) {
					Logger.Info($"[PinMAME] <= coil {coil.Id} : {isActive} | {coil.Description}");

					lock (_dispatchQueue) {
						_dispatchQueue.Enqueue(() => OnCoilChanged?.Invoke(this, new CoilEventArgs(coil.Id, isActive)));
					}
				}
				else {
					Logger.Info($"[PinMAME] <= solenoids disabled, coil {coil.Id} : {isActive} | {coil.Description}");
				}
			}
			else {
				Logger.Warn($"[PinMAME] <= coil UNMAPPED {id}: {isActive}");
			}
		}

		#endregion

		#region Lamps

		public void SetLamp(string id, float value, bool isCoil = false, LampSource source = LampSource.Lamp)
		{
			OnLampChanged?.Invoke(this, new LampEventArgs(id, value, isCoil, source));
		}

		public LampState GetLamp(string id)
		{
			return _player != null && _player.LampStatuses.ContainsKey(id) ? _player.LampStatuses[id] : LampState.Default;
		}

		#endregion

		#region Switches

		public void SendInitialSwitches()
		{
			var switches = _player.SwitchStatuses;
				Logger.Info("[PinMAME] Sending initial switch statuses...");
			foreach (var id in switches.Keys) {
				var isClosed = switches[id].IsSwitchClosed;
				// skip open switches
				if (!isClosed) {
					continue;
				}
				if (_switches.ContainsKey(id) && !_mechSwitches.Contains(_switchIdToPinMameIdMappings[_switches[id].Id])) {
					Logger.Info($"[PinMAME] => sw {id} ({_switches[id].Id}): {true} | {_switches[id].Description}");

					_pinMame.SetSwitch(_switchIdToPinMameIdMappings[_switches[id].Id], true);
				}
			}
		}

		public void Switch(string id, bool isClosed)
		{
			if (_switches.TryGetValue(id, out var sw)) {
				if (_switchIdToPinMameIdMappings.TryGetValue(sw.Id, out var pinMameId) && _mechSwitches.Contains(pinMameId)) {
					// mech switches are triggered internally by pinmame.
					return;
				}
				if (_pinMame != null && _isRunning) {
					if (Logger.IsDebugEnabled) {
						Logger.Debug($"[PinMAME] => sw {id}: {isClosed} | {sw.Description}");
					}
					if (_switchIdToPinMameIdMappings.TryGetValue(sw.Id, out pinMameId)) {
						_pinMame.SetSwitch(pinMameId, isClosed);
					}
				}
			} else if (id == "s_spawn_ball") {
				if (isClosed) {
					_ballManager.CreateBall(new DebugBallCreator(630f, _playfieldComponent.Height / 2f));
				}
			} else {
				if (Logger.IsErrorEnabled) {
					Logger.Error($"[PinMAME] Unknown switch \"{id}\".");
				}
			}

			OnSwitchChanged?.Invoke(this, new SwitchEventArgs2(id, isClosed));
		}

		public bool GetSwitch(string id)
		{
			return _player != null && _player.SwitchStatuses.ContainsKey(id) && _player.SwitchStatuses[id].IsSwitchEnabled;
		}

		#endregion

		#region Mechs

		public void RegisterMech(PinMameMechComponent mechComponent)
		{
			// PinMAME mech numbers are 1-based.
			var id = ++_numMechs;
			_registeredMechs[id] = mechComponent;
			_registeredMechNames[id] = mechComponent ? mechComponent.name : "<null-mech>";
		}

		private void OnMechAvailable(int mechNo, PinMameMechInfo mechInfo)
		{
			Logger.Info($"[PinMAME] <= mech available: mechNo={mechNo}, mechInfo={mechInfo}");
		}

		private void OnMechUpdated(int mechNo, PinMameMechInfo mechInfo)
		{
			if (_registeredMechs.ContainsKey(mechNo)) {
				lock (_dispatchQueue) {
					_dispatchQueue.Enqueue(() => _registeredMechs[mechNo].UpdateMech(mechInfo));
				}

			} else {
				Logger.Info($"[PinMAME] <= mech updated: mechNo={mechNo}, mechInfo={mechInfo}");
			}
		}

		private void SendMechs()
		{
			var max = _pinMame.GetMaxMechs();
			foreach (var (id, mech) in _registeredMechs) {
				var mechName = _registeredMechNames.TryGetValue(id, out var name) ? name : $"mech#{id}";
				// GetMaxMechs() returns the count of available slots and valid mech numbers are 1..max.
				if (id > max) {
					Logger.Error($"PinMAME supports {max} custom mech slot(s). Cannot register mech id={id} ({mechName}).");
					continue;
				}
				var mechConfig = mech.Config(_player.SwitchMapping, _player.CoilMapping, _switchIdToPinMameIdMappings, _coilIdToPinMameIdMapping);
				_pinMame.SetMech(id, mechConfig);
				foreach (var c in mechConfig.SwitchList) {
					_mechSwitches.Add(c.SwNo);
				}
			}
		}

		#endregion

		private int IsKeyPressed(PinMameKeycode keycode)
		{
			if (keycode == PinMameKeycode.F10) {
				if (_toggleSpeed) {
					_toggleSpeed = false;

					_pinMame.SetHandleKeyboard(false);

					return 1;
				}
			}

			return 0;
		}
	}
}
