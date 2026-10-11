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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using VisualPinball.Unity.Simulation;
using ColorChannel = VisualPinball.Engine.Math.ColorChannel;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.PinMAME
{
	[Serializable]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(AudioSource))]
	[PackAs("PinMameGamelogicEngine")]
	[AddComponentMenu("Pinball/Gamelogic Engine/PinMAME")]
	public class PinMameGamelogicEngine : MonoBehaviour, IGamelogicEngine, IDisplayFrameFormatPreference, IRomNameProvider, IGamelogicInputThreading, IGamelogicTimeFence, IGamelogicCoilOutputFeed, IGamelogicSharedStateWriter, IGamelogicSharedStateApplier, IGamelogicPerformanceStats, IPackable
	{
		public string Name { get; } = "PinMAME Gamelogic Engine";
		public GamelogicInputDispatchMode SwitchDispatchMode => GamelogicInputDispatchMode.SimulationThread;

		public const string DmdPrefix = "dmd";
		public const string SegDispPrefix = "display";

		#region Configuration

		[HideInInspector]
		public string machineId = string.Empty;

		[HideInInspector]
		public string romId = string.Empty;

		#if UNITY_EDITOR
		[SerializeField, HideInInspector]
		private PinMameSwitchDefinition[] _availableSwitches = Array.Empty<PinMameSwitchDefinition>();
		[SerializeField, HideInInspector]
		private PinMameCoilDefinition[] _availableCoils = Array.Empty<PinMameCoilDefinition>();
		[SerializeField, HideInInspector]
		private PinMameLampDefinition[] _availableLamps = Array.Empty<PinMameLampDefinition>();
		[SerializeField, HideInInspector]
		private bool _availableHardwareInitialized;
		[SerializeField, HideInInspector]
		private GamelogicEngineWire[] _availableWires = Array.Empty<GamelogicEngineWire>();
		[SerializeField, HideInInspector]
		private bool _availableWiresInitialized;
		// The machine the table's mappings were populated for, when it differs from machineId or is
		// known. Empty for tables populated before this was tracked.
		[SerializeField, HideInInspector]
		private string _mappedMachineId;
		#endif

		[SerializeField, HideInInspector]
		private string _legacyGameId;

		string IRomNameProvider.RomName => romId;

		[Tooltip("Disable built-in mechs")]
		public bool DisableMechs = true;

		[Min(0f)]
		[Tooltip("Delay after startup to listen for solenoid events.")]
		public float SolenoidDelay;

		[Tooltip("Disable audio")]
		public bool DisableAudio;

		#endregion

		#region Packaging

		public byte[] Pack()
		{
			var data = new PinMameGamelogicEnginePackable {
				MachineId = machineId,
				GameId = _legacyGameId,
				RomId = romId,
				DisableMechs = DisableMechs,
				SolenoidDelay = SolenoidDelay,
				DisableAudio = DisableAudio
			};
			return PackageApi.Packer.Pack(data);
		}

		public byte[] PackReferences(Transform root, PackagedRefs refs, PackagedFiles files) => Array.Empty<byte>();

		public void Unpack(byte[] bytes)
		{
			var data = PackageApi.Packer.Unpack<PinMameGamelogicEnginePackable>(bytes);
			machineId = data.MachineId ?? string.Empty;
			_legacyGameId = data.GameId;
			romId = data.RomId;
			DisableMechs = data.DisableMechs;
			SolenoidDelay = data.SolenoidDelay;
			DisableAudio = data.DisableAudio;
			#if UNITY_EDITOR
			ClearAvailableHardware();
			UpdateCaches(false);
			if (!Application.isPlaying) {
				UnityEditor.EditorApplication.delayCall += () => {
					if (this) UpdateCaches(false);
				};
			}
			#endif
		}

		public void UnpackReferences(byte[] data, Transform root, PackagedRefs refs, PackagedFiles files) { }

		#endregion

		#region IGamelogicEngine

		public GamelogicEngineSwitch[] RequestedSwitches {
			get {
				#if UNITY_EDITOR
				if (_availableHardwareInitialized && !_runtimeCachesBuilt) return (_availableSwitches ?? Array.Empty<PinMameSwitchDefinition>()).Select(item => item.ToEngineSwitch()).ToArray();
				#endif
				return _requestedSwitches;
			}
		}

		public GamelogicEngineCoil[] RequestedCoils {
			get {
				#if UNITY_EDITOR
				if (_availableHardwareInitialized && !_runtimeCachesBuilt) return (_availableCoils ?? Array.Empty<PinMameCoilDefinition>()).Select(item => item.ToEngineCoil()).ToArray();
				#endif
				return _requestedCoils;
			}
		}

		public GamelogicEngineLamp[] RequestedLamps {
			get {
				#if UNITY_EDITOR
				if (_availableHardwareInitialized && !_runtimeCachesBuilt) return (_availableLamps ?? Array.Empty<PinMameLampDefinition>()).Select(item => item.ToEngineLamp()).ToArray();
				#endif
				return _requestedLamps;
			}
		}

		public GamelogicEngineWire[] AvailableWires {
			get {
				#if UNITY_EDITOR
				if (_availableWiresInitialized) return _availableWires ?? Array.Empty<GamelogicEngineWire>();
				return string.IsNullOrWhiteSpace(_legacyGameId)
					? Array.Empty<GamelogicEngineWire>()
					: PinMameLegacyAliasResolver.WiresFor(machineId, romId, _legacyGameId);
				#else
				return Array.Empty<GamelogicEngineWire>();
				#endif
			}
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Whether the table's mappings belong to the selected machine, so its positions and IDs can
		/// be matched against them.
		/// </summary>
		public bool MappingsMatchMachine => string.IsNullOrEmpty(_mappedMachineId) || string.Equals(_mappedMachineId, machineId, StringComparison.Ordinal);

		/// <summary>
		/// Records that the table's mappings were just populated from the selected machine.
		/// </summary>
		public void MarkMappingsPopulated() => _mappedMachineId = machineId;

		/// <param name="selectedMachineId">The machine to select.</param>
		/// <param name="selectedRomId">The ROM to select.</param>
		/// <param name="resolvedCurrentMachineId">The machine the editor resolved for the current
		/// selection (e.g. from the ROM) when <see cref="machineId"/> isn't set, like in older tables.</param>
		public void SetMachineSelection(string selectedMachineId, string selectedRomId, string resolvedCurrentMachineId = null)
		{
			selectedMachineId ??= string.Empty;
			var currentMachineId = !string.IsNullOrEmpty(machineId) ? machineId : resolvedCurrentMachineId;
			if (string.IsNullOrEmpty(_mappedMachineId) && !string.IsNullOrEmpty(currentMachineId) && !string.Equals(currentMachineId, selectedMachineId, StringComparison.Ordinal)) {
				// the existing mappings were made for the machine selected until now
				_mappedMachineId = currentMachineId;
			}
			machineId = selectedMachineId;
			romId = selectedRomId ?? string.Empty;
			_legacyGameId = null;
			ClearAvailableHardware();
		}

		private void ClearAvailableHardware()
		{
			_runtimeCachesBuilt = false;
			_availableSwitches = Array.Empty<PinMameSwitchDefinition>();
			_availableCoils = Array.Empty<PinMameCoilDefinition>();
			_availableLamps = Array.Empty<PinMameLampDefinition>();
			_availableHardwareInitialized = false;
			_availableWires = Array.Empty<GamelogicEngineWire>();
			_availableWiresInitialized = false;
		}

		public void SetAvailableHardware(IEnumerable<GamelogicEngineSwitch> switches, IEnumerable<GamelogicEngineCoil> coils,
			IEnumerable<GamelogicEngineLamp> lamps, IEnumerable<GamelogicEngineWire> wires)
		{
			_runtimeCachesBuilt = false;
			_availableSwitches = switches?.Select(item => new PinMameSwitchDefinition(item)).ToArray() ?? Array.Empty<PinMameSwitchDefinition>();
			_availableCoils = coils?.Select(item => new PinMameCoilDefinition(item)).ToArray() ?? Array.Empty<PinMameCoilDefinition>();
			_availableLamps = lamps?.Select(item => new PinMameLampDefinition(item)).ToArray() ?? Array.Empty<PinMameLampDefinition>();
			_availableHardwareInitialized = true;
			_availableWires = wires?.Select(wire => new GamelogicEngineWire(wire.SourceId, wire.DestinationId, wire.DestinationType, wire.Description)).ToArray()
				?? Array.Empty<GamelogicEngineWire>();
			_availableWiresInitialized = true;
		}
		#endif

		public event EventHandler<CoilEventArgs> OnCoilChanged;
		public event EventHandler<SwitchEventArgs2> OnSwitchChanged;
		public event EventHandler<LampEventArgs> OnLampChanged;
		public event EventHandler<LampsEventArgs> OnLampsChanged;
		public event EventHandler<RequestedDisplays> OnDisplaysRequested;
		public event EventHandler<string> OnDisplayClear;
		public event EventHandler<DisplayFrameData> OnDisplayUpdateFrame;
		public event EventHandler<EventArgs> OnStarted;
		private int _loggedChangedLampSamples;
		private int _loggedUnmappedLampSamples;
		private const int MaxLoggedChangedLampSamples = 24;
		private const int MaxLoggedUnmappedLampSamples = 24;

		public void SetTimeFence(double timeInSeconds)
		{
			if (_pinMame == null) {
				return;
			}

			try {
				PinMame.PinMame.SetTimeFence(timeInSeconds);
			}
			catch (Exception e) {
				Logger.Warn(e, "[PinMAME] SetTimeFence call failed.");
			}
		}

		public bool TryDequeueCoilEvent(out CoilEventArgs coilEvent)
		{
			if (_simulationCoilDispatchQueue.TryDequeue(out coilEvent)) {
				if (Interlocked.Decrement(ref _simulationCoilDispatchQueueSize) < 0) {
					Interlocked.Exchange(ref _simulationCoilDispatchQueueSize, 0);
				}
				return true;
			}

			return false;
		}

		public bool TryGetPerformanceStats(out GamelogicPerformanceStats stats)
		{
			if (_pinMame == null) {
				stats = default;
				return false;
			}

			stats = new GamelogicPerformanceStats(_isRunning, Volatile.Read(ref _pinMameCallbackRateHz), PinMame.PinMame.RunState);
			return true;
		}

		public void WriteSharedState(ref SimulationState.Snapshot snapshot)
		{
			lock (_outputStateLock) {
				snapshot.CoilCount = CopyCoilStates(ref snapshot);
				snapshot.LampCount = CopyLampStates(ref snapshot);
				snapshot.GICount = CopyGiStates(ref snapshot);
			}
		}

		public void ApplySharedState(in SimulationState.Snapshot snapshot)
		{
			if (_player == null) {
				return;
			}

			_sharedLampPlaybackActive = true;

			for (var i = 0; i < snapshot.LampCount; i++) {
				var lamp = snapshot.LampStates[i];
				if (!_pinMameIdToLampIdMapping.TryGetValue(lamp.Id, out var lampIds)) {
					continue;
				}
				if (_lastAppliedLampStates.TryGetValue(lamp.Id, out var currentValue) && currentValue == lamp.Value) {
					continue;
				}

				_lastAppliedLampStates[lamp.Id] = lamp.Value;
				foreach (var lampId in lampIds) {
					_player.SetLamp(lampId, lamp.Value, LampSource.Lamp);
				}
			}

			for (var i = 0; i < snapshot.GICount; i++) {
				var gi = snapshot.GIStates[i];
				if (!_pinMameIdToGiIdMapping.TryGetValue(gi.Id, out var giIds)) {
					continue;
				}
				if (_lastAppliedGiStates.TryGetValue(gi.Id, out var currentValue) && currentValue == gi.Value) {
					continue;
				}

				_lastAppliedGiStates[gi.Id] = gi.Value;
				foreach (var giId in giIds) {
					_player.SetLamp(giId, gi.Value, LampSource.GI);
				}
			}
		}

		#endregion

		#region Internals

		[NonSerialized] private Player _player;
		[NonSerialized] private PinMame.PinMame _pinMame;
		[NonSerialized] private BallManager _ballManager;
		[NonSerialized] private PlayfieldComponent _playfieldComponent;

		[NonSerialized] private readonly List<PinMameLampInfo> _changedLamps = new();
		[NonSerialized] private readonly List<PinMameLampInfo> _changedGIs = new();

		private Dictionary<string, GamelogicEngineSwitch> _switches = new();
		private volatile GamelogicEngineSwitch[] _requestedSwitches = Array.Empty<GamelogicEngineSwitch>();
		private Dictionary<int, string> _pinMameIdToSwitchIdMappings = new();
		private Dictionary<string, int> _switchIdToPinMameIdMappings = new();

		private Dictionary<string, GamelogicEngineCoil> _coils = new();
		private volatile GamelogicEngineCoil[] _requestedCoils = Array.Empty<GamelogicEngineCoil>();
		// one PinMAME output can drive several mapping rows (e.g. "19" and the "c_game_on" alias)
		private Dictionary<int, List<string>> _pinMameIdToCoilIdMapping = new();
		private Dictionary<string, int> _coilIdToPinMameIdMapping = new();

		private volatile GamelogicEngineLamp[] _requestedLamps = Array.Empty<GamelogicEngineLamp>();
		private Dictionary<int, List<string>> _pinMameIdToLampIdMapping = new();
		private Dictionary<int, List<string>> _pinMameIdToGiIdMapping = new();
		private volatile bool _runtimeCachesBuilt;

		private volatile bool _isRunning;
		private int _numMechs;
		private Dictionary<int, byte[]> _frameBuffer = new();
		private volatile DisplayFrameFormat _requestedDmdFrameFormat = DisplayFrameFormat.Dmd8;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly Color Tint = new(1, 0.18f, 0);
		private static readonly Color UnlitTint = new(Tint.r * 0.18f, Tint.g * 0.18f, Tint.b * 0.18f, 1f);

		private readonly Queue<Action> _dispatchQueue = new();
		private readonly List<Action> _pendingDispatchCallbacks = new();
		private readonly object _displayStateLock = new();
		private readonly object _outputStateLock = new();
		private readonly ConcurrentQueue<CoilEventArgs> _simulationCoilDispatchQueue = new();
		private int _simulationCoilDispatchQueueSize;
		private bool _dispatchQueueWarningIssued;
		private bool _simulationCoilQueueWarningIssued;
		private bool _simulationCoilQueueDropWarningIssued;
		private long _solenoidDelayStartTimestampUsec;
		private const int MaxSimulationCoilDispatchQueueSize = 8192;
		private const int DispatchQueueWarningThreshold = 512;
		private const double PerfSampleWindowSeconds = 0.25;
		private long _pinMamePerfWindowStartTicks = Stopwatch.GetTimestamp();
		private int _pinMameCallbacksInWindow;
		private float _pinMameCallbackRateHz;
		private readonly Dictionary<int, bool> _sharedCoilStates = new();
		// Solenoid callbacks report 0/1 for binary coils, but 0-255 for PWM-integrated
		// (modulated) coils on WPC/SAM ROMs. We can't tell from a single value whether a
		// "1" is "binary on" or "modulated 0.4% duty"; once any coil reports a value above
		// 1 the game is modulating, and every coil value is then normalized by 255.
		private volatile bool _solenoidsModulated;
		private readonly Dictionary<int, float> _sharedLampStates = new();
		private readonly Dictionary<int, float> _sharedGiStates = new();
		private readonly Dictionary<int, float> _lastAppliedLampStates = new();
		private readonly Dictionary<int, float> _lastAppliedGiStates = new();
		private bool _sharedLampPlaybackActive;
		private readonly Queue<float[]> _audioQueue = new();
		private readonly HashSet<string> _loggedUnresolvedMappingIds = new(StringComparer.Ordinal);

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

		private static long TimestampUsec => (Stopwatch.GetTimestamp() * 1_000_000) / Stopwatch.Frequency;

		private static readonly SemaphoreSlim PinMameStartStopGate = new SemaphoreSlim(1, 1);
		private int _onInitCalled;
		private int _stopRequested;
		private int _playModeExitHandled;
		private const int StopTimeoutMs = 10000;

		#endregion

		#region Lifecycle

		private void Awake()
		{
			// SimulationThreadComponent reads RequestedSwitches from Start(). Build these caches in Awake so
			// their availability does not depend on Unity's undefined ordering between component Start calls.
			UpdateCaches(true);
			Logger.Info("Project audio sample rate: " +  AudioSettings.outputSampleRate);
			_keyboard = Keyboard.current;
		}

		private void Start()
		{
			_lastAudioFrame = Array.Empty<float>();
			_lastAudioFrameOffset = 0;
		}

		#if UNITY_EDITOR
		private void OnValidate()
		{
			if (!Application.isPlaying) UpdateCaches(false);
		}
		#endif

		private void Update()
		{
			if (_pinMame == null || !_isRunning) {
				return;
			}

			_pendingDispatchCallbacks.Clear();

			lock (_dispatchQueue) {
				while (_dispatchQueue.Count > 0) {
					_pendingDispatchCallbacks.Add(_dispatchQueue.Dequeue());
				}
			}

			foreach (var callback in _pendingDispatchCallbacks) {
				try {
					callback.Invoke();
				}
				catch (Exception e) {
					Logger.Error(e, "[PinMAME] Exception while processing main-thread dispatch callback.");
				}
			}

			// lamps
			_pinMame.GetChangedLamps(_changedLamps);
			foreach (var changedLamp in _changedLamps) {
				lock (_outputStateLock) {
					_sharedLampStates[changedLamp.Id] = changedLamp.Value;
				}
				if (!_sharedLampPlaybackActive && _pinMameIdToLampIdMapping.TryGetValue(changedLamp.Id, out var vpeLampIds)) {
					foreach (var vpeLampId in vpeLampIds) {
						LogChangedLampSample("lamp", changedLamp.Id, vpeLampId, changedLamp.Value, LampSource.Lamp);
						OnLampChanged?.Invoke(this, new LampEventArgs(vpeLampId, changedLamp.Value));
					}
				} else {
					LogUnmappedLampSample("lamp", changedLamp.Id, changedLamp.Value, LampSource.Lamp);
				}
			}

			// gi
			_pinMame.GetChangedGIs(_changedGIs);
			foreach (var changedGi in _changedGIs) {
				lock (_outputStateLock) {
					_sharedGiStates[changedGi.Id] = changedGi.Value;
				}
				if (!_sharedLampPlaybackActive && _pinMameIdToGiIdMapping.TryGetValue(changedGi.Id, out var vpeGiIds)) {
					foreach (var vpeLampId in vpeGiIds) {
						LogChangedLampSample("gi", changedGi.Id, vpeLampId, changedGi.Value, LampSource.GI);
						OnLampChanged?.Invoke(this, new LampEventArgs(vpeLampId, changedGi.Value, LampSource.GI));
					}
				} else {
					LogUnmappedLampSample("gi", changedGi.Id, changedGi.Value, LampSource.GI);
				}
			}

			// if (_keyboard != null && _keyboard.cKey.wasPressedThisFrame)
			// {
			// 	OnCoilChanged.Invoke(this, new CoilEventArgs("28", true));
			// 	OnCoilChanged.Invoke(this, new CoilEventArgs("28", false));
			// }
		}

		private void EnqueueMainThreadDispatch(Action callback)
		{
			lock (_dispatchQueue) {
				_dispatchQueue.Enqueue(callback);
				if (!_dispatchQueueWarningIssued && _dispatchQueue.Count >= DispatchQueueWarningThreshold) {
					_dispatchQueueWarningIssued = true;
					Logger.Warn($"[PinMAME] Main-thread dispatch queue backlog reached {_dispatchQueue.Count} items.");
				}
			}
		}

		private void LogChangedLampSample(string kind, int pinMameId, string vpeLampId, float value, LampSource source)
		{
			if (_loggedChangedLampSamples >= MaxLoggedChangedLampSamples) {
				return;
			}

			_loggedChangedLampSamples++;
			Logger.Info(
				$"[PinMAME] <= {kind} sample #{_loggedChangedLampSamples}: pinmameId={pinMameId}, " +
				$"vpeId={vpeLampId}, source={source}, value={value}, sharedLampPlaybackActive={_sharedLampPlaybackActive}.");
		}

		private void LogUnmappedLampSample(string kind, int pinMameId, float value, LampSource source)
		{
			if (_loggedUnmappedLampSamples >= MaxLoggedUnmappedLampSamples) {
				return;
			}

			_loggedUnmappedLampSamples++;
			Logger.Warn(
				$"[PinMAME] <= {kind} UNMAPPED sample #{_loggedUnmappedLampSamples}: pinmameId={pinMameId}, " +
				$"source={source}, value={value}, sharedLampPlaybackActive={_sharedLampPlaybackActive}.");
		}

		private int CopyCoilStates(ref SimulationState.Snapshot snapshot)
		{
			var index = 0;
			foreach (var entry in _sharedCoilStates) {
				if (index >= snapshot.CoilStates.Length) {
					break;
				}

				snapshot.CoilStates[index++] = new SimulationState.CoilState {
					Id = entry.Key,
					IsActive = entry.Value ? (byte)1 : (byte)0,
				};
			}

			return index;
		}

		private int CopyLampStates(ref SimulationState.Snapshot snapshot)
		{
			var index = 0;
			foreach (var entry in _sharedLampStates) {
				if (index >= snapshot.LampStates.Length) {
					break;
				}

				snapshot.LampStates[index++] = new SimulationState.LampState {
					Id = entry.Key,
					Value = entry.Value,
				};
			}

			return index;
		}

		private int CopyGiStates(ref SimulationState.Snapshot snapshot)
		{
			var index = 0;
			foreach (var entry in _sharedGiStates) {
				if (index >= snapshot.GIStates.Length) {
					break;
				}

				snapshot.GIStates[index++] = new SimulationState.GIState {
					Id = entry.Key,
					Value = entry.Value,
				};
			}

			return index;
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
			lock (_outputStateLock) {
				_sharedCoilStates.Clear();
				_solenoidsModulated = false;
				_sharedLampStates.Clear();
				_sharedGiStates.Clear();
			}
			_lastAppliedLampStates.Clear();
			_lastAppliedGiStates.Clear();
			_sharedLampPlaybackActive = false;
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
			#if UNITY_EDITOR
			// With domain reload disabled this component survives the play session. Return editor callers to
			// the complete catalog candidate snapshots instead of the runtime MappingConfig projection.
			_runtimeCachesBuilt = false;
			#endif
			if (Interlocked.Exchange(ref _playModeExitHandled, 1) != 0) {
				return;
			}

			// Editor-only: called from playmode lifecycle hook.
			// Do not call into native Stop() here (it blocks); the editor hook will stop PinMAME
			// after we unsubscribe managed callbacks.
			Interlocked.Exchange(ref _stopRequested, 1);
			SetTimeFence(0.0);
			// Stop polling outputs immediately to avoid concurrent calls into pinmame.dll
			// while a stop is in progress (pinmame APIs are not guaranteed thread-safe).
			_isRunning = false;
			Logger.Info($"[PinMAME] StopForPlayModeExit: unsubscribing callbacks ({name}#{UnityObjectId.Get(this)})");
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
				SetTimeFence(0.0);

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
			#else
				vpmPath = PinMameFolders.DataFolder;
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

				#if !((UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR)
				PrepareDesktopFolders();
				#endif

				_pinMame.SetHandleKeyboard(false);
				_pinMame.SetHandleMechanics(DisableMechs ? 0 : 0xFF);
				ApplyDmdModePreference();
				SetTimeFence(0.01);

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

		/// <summary>
		/// Points PinMAME at the ROM folder and brings over Visual PinMAME's state for the game.
		/// </summary>
		private void PrepareDesktopFolders()
		{
			try {
				PinMameFolders.ApplyRomFolder(_pinMame);
			} catch (InvalidOperationException e) {
				Logger.Error(e, "[PinMAME] Could not set the ROM folder.");
			}
			PinMameFolders.ImportFromVpm(romId);

			// Only a hint: PinMAME's audit reports clones whose files are all identical to their
			// parent's as missing, although they run. StartGame reports a ROM that really is missing.
			try {
				if (!_pinMame.GetGame(romId).RomFound) {
					Logger.Warn($"[PinMAME] ROM \"{romId}\" may be missing from {_pinMame.RomPath}. If the game doesn't start, set the ROM folder to where your ROMs are.");
				}
			} catch (InvalidOperationException) {
				// unknown game: StartGame reports it
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

			_solenoidDelayStartTimestampUsec = TimestampUsec;
			_solenoidDelayStart = _solenoidDelayStartTimestampUsec / 1000;

			try {
				SendInitialSwitches();
				SendMechs();
			}

			catch(Exception e) {
				Logger.Error($"[PinMAME] OnGameStarted: {e.Message}");
			}

			EnqueueMainThreadDispatch(() => OnStarted?.Invoke(this, EventArgs.Empty));
		}

		private void OnGameEnded()
		{
			// Native can report ended more than once during teardown; keep it idempotent.
			if (_isRunning) {
				Logger.Info("[PinMAME] Game ended.");
			}
			_isRunning = false;
			while (_simulationCoilDispatchQueue.TryDequeue(out _)) {
				if (Interlocked.Decrement(ref _simulationCoilDispatchQueueSize) < 0) {
					Interlocked.Exchange(ref _simulationCoilDispatchQueueSize, 0);
				}
			}
			lock (_outputStateLock) {
				_sharedCoilStates.Clear();
				_solenoidsModulated = false;
				_sharedLampStates.Clear();
				_sharedGiStates.Clear();
			}
			_lastAppliedLampStates.Clear();
			_lastAppliedGiStates.Clear();
			_sharedLampPlaybackActive = false;
		}

		private void UpdateCaches(bool runtime)
		{
			var requestedLamps = new List<GamelogicEngineLamp>();
			var requestedCoils = new List<GamelogicEngineCoil>();
			var requestedSwitches = new List<GamelogicEngineSwitch>();
			_requestedLamps = Array.Empty<GamelogicEngineLamp>();
			_pinMameIdToLampIdMapping.Clear();
			_pinMameIdToGiIdMapping.Clear();

			_coils.Clear();
			_requestedCoils = Array.Empty<GamelogicEngineCoil>();
			_pinMameIdToCoilIdMapping.Clear();
			_coilIdToPinMameIdMapping.Clear();

			_switches.Clear();
			_requestedSwitches = Array.Empty<GamelogicEngineSwitch>();
			_pinMameIdToSwitchIdMappings.Clear();
			_switchIdToPinMameIdMappings.Clear();

			var mappingConfig = GetComponentInParent<TableComponent>()?.MappingConfig;
			if (mappingConfig == null) {
				_runtimeCachesBuilt = runtime;
				return;
			}

			foreach (var mapping in mappingConfig.Switches) {
				var @switch = new GamelogicEngineSwitch(mapping.Id) {
					Description = mapping.Description,
					NormallyClosed = mapping.IsNormallyClosed,
					InputActionHint = mapping.Source == SwitchSource.InputSystem ? mapping.InputAction : string.Empty,
					InputMapHint = mapping.Source == SwitchSource.InputSystem ? mapping.InputActionMap : string.Empty,
					ConstantHint = mapping.Source != SwitchSource.Constant
						? SwitchConstantHint.None
						: mapping.Constant == SwitchConstant.Open ? SwitchConstantHint.AlwaysOpen : SwitchConstantHint.AlwaysClosed,
				};
				requestedSwitches.Add(@switch);
				_switches[@switch.Id] = @switch;

				var isNumericMappingId = int.TryParse(@switch.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
				if (TryResolveSwitchId(@switch.Id, out var pinMameId)) {
					_pinMameIdToSwitchIdMappings[pinMameId] = @switch.Id;
					_switchIdToPinMameIdMappings[@switch.Id] = pinMameId;

					// add mappings with prefixed 0.
					if (isNumericMappingId && pinMameId is >= 0 and < 10) {
						_switchIdToPinMameIdMappings["0" + @switch.Id] = pinMameId;
						_switchIdToPinMameIdMappings["00" + @switch.Id] = pinMameId;

						_switches["0" + @switch.Id] = @switch;
						_switches["00" + @switch.Id] = @switch;
					}
					if (isNumericMappingId && pinMameId is >= 0 and < 100) {
						_switchIdToPinMameIdMappings["0" + @switch.Id] = pinMameId;
						_switches["0" + @switch.Id] = @switch;
					}
				} else {
					if (runtime) LogUnresolvedMappingId("switch", @switch.Id);
				}
			}

			foreach (var mapping in mappingConfig.Coils) {
				var coil = new GamelogicEngineCoil(mapping.Id) {
					Description = mapping.Description,
					IsLamp = mapping.Destination == CoilDestination.Lamp,
				};
				_coils[coil.Id] = coil;
				requestedCoils.Add(coil);

				if (TryResolveCoilId(coil.Id, out var pinMameId)) {
					AddMappingTarget(_pinMameIdToCoilIdMapping, pinMameId, coil.Id);
					_coilIdToPinMameIdMapping[coil.Id] = pinMameId;
				} else {
					if (runtime) LogUnresolvedMappingId("coil", coil.Id);
				}
			}

			foreach (var mapping in mappingConfig.Lamps.Where(mapping => !mapping.IsCoil)) {
				var lamp = new GamelogicEngineLamp(mapping.Id) {
					Description = mapping.Description,
					Channel = mapping.Channel,
					FadingSteps = mapping.FadingSteps,
					Source = mapping.Source,
					Type = mapping.Type,
				};
				requestedLamps.Add(lamp);

				if (int.TryParse(lamp.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pinMameId)) {
					AddMappingTarget(lamp.Source == LampSource.GI ? _pinMameIdToGiIdMapping : _pinMameIdToLampIdMapping, pinMameId, lamp.Id);
				}
			}

			CollapseNumericSpellings(_pinMameIdToCoilIdMapping);
			CollapseNumericSpellings(_pinMameIdToLampIdMapping);
			CollapseNumericSpellings(_pinMameIdToGiIdMapping);

			_requestedSwitches = requestedSwitches.GroupBy(item => item.Id, StringComparer.Ordinal).Select(group => group.Last()).ToArray();
			_requestedCoils = requestedCoils.GroupBy(item => item.Id, StringComparer.Ordinal).Select(group => group.Last()).ToArray();
			_requestedLamps = requestedLamps.GroupBy(item => new { item.Id, item.Source, item.Channel }).Select(group => group.Last()).ToArray();
			_runtimeCachesBuilt = runtime;
		}

		private static void AddMappingTarget(Dictionary<int, List<string>> map, int pinMameId, string mappingId)
		{
			if (!map.TryGetValue(pinMameId, out var mappingIds)) {
				map[pinMameId] = mappingIds = new List<string>(1);
			}
			if (!mappingIds.Contains(mappingId)) {
				mappingIds.Add(mappingId);
			}
		}

		/// <summary>
		/// Output events go out with each mapping's own ID, since the players key state and wires by
		/// it. When one output has several numeric spellings ("1" and "01"), they're sent once as
		/// PinMAME's number instead: the coil and lamp players register "01" under "1" too, so sending
		/// both would actuate the "01" device twice.
		/// </summary>
		private static void CollapseNumericSpellings(Dictionary<int, List<string>> map)
		{
			foreach (var (pinMameId, mappingIds) in map) {
				var numericCount = mappingIds.Count(IsNumericId);
				if (numericCount < 2) {
					continue;
				}
				mappingIds.RemoveAll(IsNumericId);
				mappingIds.Insert(0, pinMameId.ToString(CultureInfo.InvariantCulture));
			}
		}

		private static bool IsNumericId(string id) => int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

		private bool TryResolveSwitchId(string id, out int pinMameId)
		{
			return int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out pinMameId)
				|| PinMameLegacyAliasResolver.TryResolveSwitch(machineId, romId, _legacyGameId, id, out pinMameId);
		}

		private bool TryResolveCoilId(string id, out int pinMameId)
		{
			return int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out pinMameId)
				|| PinMameLegacyAliasResolver.TryResolveCoil(machineId, romId, _legacyGameId, id, out pinMameId);
		}

		private void LogUnresolvedMappingId(string kind, string id)
		{
			if (_loggedUnresolvedMappingIds.Add($"{kind}:{id}")) {
				if (string.Equals(kind, "coil", StringComparison.Ordinal) && PinMameLegacyAliasResolver.IsFlipperCoilAlias(id)) {
					Logger.Info($"[PinMAME] Legacy flipper coil mapping '{id}' has no direct PinMAME output for machine '{machineId}' and ROM '{romId}'; a table wire may drive it instead.");
				} else {
					Logger.Warn($"[PinMAME] {kind} '{id}' isn't a PinMAME {kind} of machine '{machineId}' (ROM '{romId}'), so PinMAME won't drive it.");
				}
			}
		}

		#endregion

		#region Displays

		public void RequestDisplayFrameFormat(string displayId, DisplayFrameFormat format)
		{
			if (!string.IsNullOrWhiteSpace(displayId) && !displayId.StartsWith(DmdPrefix, StringComparison.OrdinalIgnoreCase)) {
				return;
			}

			if (format != DisplayFrameFormat.Dmd2 && format != DisplayFrameFormat.Dmd4 && format != DisplayFrameFormat.Dmd8) {
				return;
			}

			if (_requestedDmdFrameFormat == format) {
				return;
			}

			_requestedDmdFrameFormat = format;
			ApplyDmdModePreference();
		}

		private void OnDisplayRequested(int index, int displayCount, PinMameDisplayLayout displayLayout)
		{
			if (displayLayout.IsDmd) {
				EnqueueMainThreadDispatch(() =>
					OnDisplaysRequested?.Invoke(this, new RequestedDisplays(
						new DisplayConfig($"{DmdPrefix}{index}", displayLayout.Width, displayLayout.Height, false, Tint, UnlitTint))));

				lock (_displayStateLock) {
					_frameBuffer[index] = new byte[displayLayout.Width * displayLayout.Height];
				}

			} else {
				EnqueueMainThreadDispatch(() =>
					OnDisplaysRequested?.Invoke(this, new RequestedDisplays(
						new DisplayConfig($"{SegDispPrefix}{index}", displayLayout.Length, 1))));

				lock (_displayStateLock) {
					_frameBuffer[index] = new byte[displayLayout.Length * 2];
				}
				Logger.Info($"[PinMAME] Display {SegDispPrefix}{index} is of type {displayLayout.Type} at {displayLayout.Length} wide.");
			}
		}

		private void OnDisplayUpdated(int index, IntPtr framePtr, PinMameDisplayLayout displayLayout)
		{
			MarkPinMameCallbackActivity();

			if (displayLayout.IsDmd) {
				UpdateDmd(index, displayLayout, framePtr);

			} else {
				UpdateSegDisp(index, displayLayout, framePtr);
			}
		}

		private void UpdateDmd(int index, PinMameDisplayLayout displayLayout, IntPtr framePtr)
		{
			if (framePtr == IntPtr.Zero) {
				Logger.Warn($"[PinMAME] Dropping DMD frame for index {index} because frame pointer is null (layout: {displayLayout}).");
				return;
			}

			byte[] frameBuffer;
			lock (_displayStateLock) {
				if (!_frameBuffer.TryGetValue(index, out frameBuffer)) {
					Logger.Warn($"[PinMAME] Dropping DMD frame for unknown index {index} (layout: {displayLayout}).");
					return;
				}
			}

			// Snapshot into a fresh array: the native PinMAME thread can fire UpdateDmd again before
			// the queued dispatch runs on the main thread, so the frame handed to subscribers must not
			// be the reused _frameBuffer (otherwise it can be overwritten / torn mid-read).
			var frame = new byte[frameBuffer.Length];
			Marshal.Copy(framePtr, frame, 0, frame.Length);

			EnqueueMainThreadDispatch(() => OnDisplayUpdateFrame?.Invoke(this,
				new DisplayFrameData($"{DmdPrefix}{index}", GetRequestedDmdDisplayFrameFormat(displayLayout), frame)));
		}

		private void UpdateSegDisp(int index, PinMameDisplayLayout displayLayout, IntPtr framePtr)
		{
			if (framePtr == IntPtr.Zero) {
				Logger.Warn($"[PinMAME] Dropping segmented display frame for index {index} because frame pointer is null (layout: {displayLayout}).");
				return;
			}

			byte[] frameBuffer;
			lock (_displayStateLock) {
				if (!_frameBuffer.TryGetValue(index, out frameBuffer)) {
					Logger.Warn($"[PinMAME] Dropping segmented display frame for unknown index {index} (layout: {displayLayout}).");
					return;
				}
			}

			// Snapshot into a fresh array (see UpdateDmd) so a follow-up native callback can't
			// overwrite a frame already queued for the main thread.
			var frame = new byte[frameBuffer.Length];
			Marshal.Copy(framePtr, frame, 0, displayLayout.Length * 2);

			EnqueueMainThreadDispatch(() => OnDisplayUpdateFrame?.Invoke(this,
				new DisplayFrameData($"{SegDispPrefix}{index}", GetDisplayFrameFormat(displayLayout), frame)));
		}

		public static DisplayFrameFormat GetDisplayFrameFormat(PinMameDisplayLayout layout)
		{
			if (layout.IsDmd) {
				return DisplayFrameFormat.Dmd8;
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

		private DisplayFrameFormat GetRequestedDmdDisplayFrameFormat(PinMameDisplayLayout layout)
		{
			if (_requestedDmdFrameFormat == DisplayFrameFormat.Dmd8) {
				return DisplayFrameFormat.Dmd8;
			}

			return GetRawDmdDisplayFrameFormat(layout);
		}

		private static DisplayFrameFormat GetRawDmdDisplayFrameFormat(PinMameDisplayLayout layout)
		{
			var levels = layout.Levels?.Count ?? 0;
			if ((levels > 0 && levels <= 4) || (layout.Depth > 0 && layout.Depth <= 2)) {
				return DisplayFrameFormat.Dmd2;
			}

			if ((levels > 0 && levels <= 16) || (layout.Depth > 0 && layout.Depth <= 4)) {
				return DisplayFrameFormat.Dmd4;
			}

			return DisplayFrameFormat.Dmd8;
		}

		private void ApplyDmdModePreference()
		{
			if (_pinMame == null) {
				return;
			}

			var rawMode = _requestedDmdFrameFormat == DisplayFrameFormat.Dmd2 || _requestedDmdFrameFormat == DisplayFrameFormat.Dmd4;
			try {
				_pinMame.SetDmdMode(rawMode ? PinMameDmdMode.Raw : PinMameDmdMode.Brightness);
				Logger.Info($"[PinMAME] DMD frame mode set to {(rawMode ? "raw" : "brightness")} for {_requestedDmdFrameFormat} output.");
			} catch (Exception exception) {
				Logger.Warn(exception, "[PinMAME] Could not set DMD frame mode.");
			}
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
			if (int.TryParse(n, out var coilId)) {
				lock (_outputStateLock) {
					_sharedCoilStates[coilId] = value;
				}
			}
			OnCoilChanged?.Invoke(this, new CoilEventArgs(n, value));
		}

		public bool GetCoil(string id)
		{
			return _player != null && _player.CoilStatuses.ContainsKey(id) && _player.CoilStatuses[id];
		}

		private void OnSolenoidUpdated(int id, int value)
		{
			MarkPinMameCallbackActivity();

			// value is 0/1 for binary coils, 0-255 for PWM-integrated (modulated) coils
			if (value > 1) {
				_solenoidsModulated = true;
			}
			var isActive = value > 0;
			// normalized strength for the (main-thread) coil pipeline; the sim-thread path
			// and the shared snapshot stay boolean on/off
			var intensity = _solenoidsModulated ? System.Math.Min(value, 255) / 255f : (isActive ? 1f : 0f);

			if (_pinMameIdToCoilIdMapping.TryGetValue(id, out var coilIds)) {
				if (!_solenoidsEnabled) {
					_solenoidsEnabled = TimestampUsec - _solenoidDelayStartTimestampUsec >= (long)(SolenoidDelay * 1000f);

					if (_solenoidsEnabled) {
						Logger.Info($"Solenoids enabled, {SolenoidDelay}ms passed");
					}
				}

				if (_solenoidsEnabled) {
					lock (_outputStateLock) {
						_sharedCoilStates[id] = isActive;
					}

					foreach (var coilId in coilIds) {
						if (Logger.IsDebugEnabled) {
							Logger.Debug($"[PinMAME] <= coil {coilId} : {value} | {(_coils.TryGetValue(coilId, out var coil) ? coil.Description : string.Empty)}");
						}
						if (ShouldDispatchSimulationCoil(coilId)) {
							_simulationCoilDispatchQueue.Enqueue(new CoilEventArgs(coilId, isActive));
							var simulationCoilQueueSize = Interlocked.Increment(ref _simulationCoilDispatchQueueSize);
							if (!_simulationCoilQueueWarningIssued && simulationCoilQueueSize >= MaxSimulationCoilDispatchQueueSize / 2) {
								_simulationCoilQueueWarningIssued = true;
								Logger.Warn($"[PinMAME] Simulation coil dispatch backlog reached {simulationCoilQueueSize} items.");
							}
							if (simulationCoilQueueSize > MaxSimulationCoilDispatchQueueSize) {
								if (_simulationCoilDispatchQueue.TryDequeue(out _)) {
									if (Interlocked.Decrement(ref _simulationCoilDispatchQueueSize) < 0) {
										Interlocked.Exchange(ref _simulationCoilDispatchQueueSize, 0);
									}
									if (!_simulationCoilQueueDropWarningIssued) {
										_simulationCoilQueueDropWarningIssued = true;
										Logger.Warn($"[PinMAME] Simulation coil dispatch queue exceeded {MaxSimulationCoilDispatchQueueSize} items. Dropping oldest coil callbacks.");
									}
								}
							}
						}

						EnqueueMainThreadDispatch(() => OnCoilChanged?.Invoke(this, new CoilEventArgs(coilId, intensity)));
					}
				}
				else {
					if (Logger.IsDebugEnabled) {
						Logger.Debug($"[PinMAME] <= solenoids disabled, coil {id} : {value}");
					}
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
			if (int.TryParse(id, out var lampId)) {
				lock (_outputStateLock) {
					if (source == LampSource.GI) {
						_sharedGiStates[lampId] = value;
					} else {
						_sharedLampStates[lampId] = value;
					}
				}
			}
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
				if (_switches.TryGetValue(id, out var @switch)
				    && _switchIdToPinMameIdMappings.TryGetValue(@switch.Id, out var pinMameId)
				    && !_mechSwitches.Contains(pinMameId)) {
					Logger.Info($"[PinMAME] => sw {id} ({@switch.Id}): {true} | {@switch.Description}");

					_pinMame.SetSwitch(pinMameId, true);
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
				EnqueueMainThreadDispatch(() => _registeredMechs[mechNo].UpdateMech(mechInfo));

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

		private bool ShouldDispatchSimulationCoil(string coilId)
		{
			return !string.IsNullOrEmpty(coilId)
				&& _player != null
				&& _player.SupportsSimulationThreadCoilDispatch(coilId);
		}

		private void MarkPinMameCallbackActivity()
		{
			Interlocked.Increment(ref _pinMameCallbacksInWindow);

			var nowTicks = Stopwatch.GetTimestamp();
			var startTicks = Volatile.Read(ref _pinMamePerfWindowStartTicks);
			var elapsedSeconds = (double)(nowTicks - startTicks) / Stopwatch.Frequency;
			if (elapsedSeconds < PerfSampleWindowSeconds) {
				return;
			}

			if (Interlocked.CompareExchange(ref _pinMamePerfWindowStartTicks, nowTicks, startTicks) != startTicks) {
				return;
			}

			var callbacks = Interlocked.Exchange(ref _pinMameCallbacksInWindow, 0);
			var rate = elapsedSeconds > 0.0 ? callbacks / elapsedSeconds : 0.0;
			Volatile.Write(ref _pinMameCallbackRateHz, (float)rate);
		}
	}

	[Serializable]
	public struct PinMameGamelogicEnginePackable
	{
		public string MachineId;
		// Kept so packages produced by older VPE versions still deserialize.
		public string GameType;
		public string GameId;
		public string RomId;
		public bool DisableMechs;
		public float SolenoidDelay;
		public bool DisableAudio;
	}

	#if UNITY_EDITOR
	[Serializable]
	public struct PinMameSwitchDefinition
	{
		public string Id;
		public string Description;
		public bool NormallyClosed;
		public bool IsPulseSwitch;
		public string InputActionHint;
		public string InputMapHint;
		public string DeviceHint;
		public string DeviceItemHint;
		public int NumMatches;
		public SwitchConstantHint ConstantHint;

		public PinMameSwitchDefinition(GamelogicEngineSwitch item)
		{
			Id = item.Id;
			Description = item.Description;
			NormallyClosed = item.NormallyClosed;
			IsPulseSwitch = item.IsPulseSwitch;
			InputActionHint = item.InputActionHint;
			InputMapHint = item.InputMapHint;
			DeviceHint = item.DeviceHint;
			DeviceItemHint = item.DeviceItemHint;
			NumMatches = item.NumMatches;
			ConstantHint = item.ConstantHint;
		}

		public GamelogicEngineSwitch ToEngineSwitch() => new(Id) {
			Description = Description,
			NormallyClosed = NormallyClosed,
			IsPulseSwitch = IsPulseSwitch,
			InputActionHint = InputActionHint,
			InputMapHint = InputMapHint,
			DeviceHint = DeviceHint,
			DeviceItemHint = DeviceItemHint,
			NumMatches = NumMatches,
			ConstantHint = ConstantHint,
		};
	}

	[Serializable]
	public struct PinMameCoilDefinition
	{
		public string Id;
		public string Description;
		public string DeviceHint;
		public string DeviceItemHint;
		public int NumMatches;
		public bool IsLamp;
		public bool IsUnused;

		public PinMameCoilDefinition(GamelogicEngineCoil item)
		{
			Id = item.Id;
			Description = item.Description;
			DeviceHint = item.DeviceHint;
			DeviceItemHint = item.DeviceItemHint;
			NumMatches = item.NumMatches;
			IsLamp = item.IsLamp;
			IsUnused = item.IsUnused;
		}

		public GamelogicEngineCoil ToEngineCoil() => new(Id) {
			Description = Description,
			DeviceHint = DeviceHint,
			DeviceItemHint = DeviceItemHint,
			NumMatches = NumMatches,
			IsLamp = IsLamp,
			IsUnused = IsUnused,
		};
	}

	[Serializable]
	public struct PinMameLampDefinition
	{
		public string Id;
		public string Description;
		public ColorChannel Channel;
		public int FadingSteps;
		public LampSource Source;
		public LampType Type;
		public string DeviceHint;
		public string DeviceItemHint;
		public int NumMatches;

		public PinMameLampDefinition(GamelogicEngineLamp item)
		{
			Id = item.Id;
			Description = item.Description;
			Channel = item.Channel;
			FadingSteps = item.FadingSteps;
			Source = item.Source;
			Type = item.Type;
			DeviceHint = item.DeviceHint;
			DeviceItemHint = item.DeviceItemHint;
			NumMatches = item.NumMatches;
		}

		public GamelogicEngineLamp ToEngineLamp() => new(Id) {
			Description = Description,
			Channel = Channel,
			FadingSteps = FadingSteps,
			Source = Source,
			Type = Type,
			DeviceHint = DeviceHint,
			DeviceItemHint = DeviceItemHint,
			NumMatches = NumMatches,
		};
	}
	#endif
}
