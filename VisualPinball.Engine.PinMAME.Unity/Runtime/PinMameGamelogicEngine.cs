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
using System.Linq;
using System.Runtime.InteropServices;
using NLog;
using PinMame;
using UnityEngine;
using VisualPinball.Engine.Game.Engines;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.PinMAME
{
	[Serializable]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(AudioSource))]
	[AddComponentMenu("Visual Pinball/Game Logic Engine/PinMAME")]
	public class PinMameGamelogicEngine : MonoBehaviour, IGamelogicEngine
	{
		public string Name { get; } = "PinMAME Gamelogic Engine";

		public const string DmdPrefix = "dmd";
		public const string SegDispPrefix = "display";

		public PinMameGame Game { get => _game; set => _game = value; }

		[HideInInspector]
		public string romId = string.Empty;

		[Tooltip("Disable built-in mechs")]
		public bool DisableMechs = true;

		[Min(0f)]
		[Tooltip("Delay after startup to listen for solenoid events.")]
		public float SolenoidDelay = 0;

		[Tooltip("Disable audio")]
		public bool DisableAudio;

		public GamelogicEngineSwitch[] AvailableSwitches {
			get {
				UpdateCaches();
				return _game?.AvailableSwitches ?? Array.Empty<GamelogicEngineSwitch>();
			}
		}
		public GamelogicEngineCoil[] AvailableCoils {
			get {
				UpdateCaches();
				return _coils.Values.ToArray();
			}
		}
		public GamelogicEngineLamp[] AvailableLamps {
			get {
				UpdateCaches();
				return _lamps.Values.ToArray();
			}
		}

		public GamelogicEngineWire[] AvailableWires => _game?.AvailableWires ?? Array.Empty<GamelogicEngineWire>();

		public event EventHandler<CoilEventArgs> OnCoilChanged;
		public event EventHandler<LampEventArgs> OnLampChanged;
		public event EventHandler<LampsEventArgs> OnLampsChanged;
		public event EventHandler<LampColorEventArgs> OnLampColorChanged;
		public event EventHandler<AvailableDisplays> OnDisplaysAvailable;
		public event EventHandler<DisplayFrameData> OnDisplayFrame;

		[NonSerialized] private Player _player;
		[NonSerialized] private PinMame.PinMame _pinMame;
		[SerializeReference] private PinMameGame _game;

		private Dictionary<string, GamelogicEngineSwitch> _switches = new Dictionary<string, GamelogicEngineSwitch>();
		private Dictionary<int, GamelogicEngineCoil> _coils = new Dictionary<int, GamelogicEngineCoil>();
		private Dictionary<int, GamelogicEngineLamp> _lamps = new Dictionary<int, GamelogicEngineLamp>();

		private bool _isRunning;
		private int _numMechs;
		private Dictionary<int, byte[]> _frameBuffer = new Dictionary<int, byte[]>();
		private Dictionary<int, Dictionary<byte, byte>> _dmdLevels = new Dictionary<int, Dictionary<byte, byte>>();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly Color Tint = new Color(1, 0.18f, 0);

		private readonly Queue<Action> _dispatchQueue = new Queue<Action>();
		private readonly Queue<float[]> _audioQueue = new Queue<float[]>();

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
		private HashSet<int> _mechSwitches = new();

		private void Awake()
		{
			Logger.Info("Project audio sample rate: " +  AudioSettings.outputSampleRate);
		}

		private void Start()
		{
			UpdateCaches();

			_lastAudioFrame = Array.Empty<float>();
			_lastAudioFrameOffset = 0;
		}

		public void OnInit(Player player, TableApi tableApi, BallManager ballManager)
		{
			// turn off all lamps
			foreach (var lamp in _lamps.Values) {
				OnLampChanged?.Invoke(this, new LampEventArgs(lamp.Id, 0));
			}

			Logger.Info($"New PinMAME instance at {(double)AudioSettings.outputSampleRate / 1000}kHz");
			_pinMame = PinMame.PinMame.Instance(PinMameAudioFormat.AudioFormatFloat, AudioSettings.outputSampleRate);

			_pinMame.SetHandleKeyboard(false);
			_pinMame.SetHandleMechanics(DisableMechs ? 0 : 0xFF);

			_pinMame.OnGameStarted += OnGameStarted;
			_pinMame.OnGameEnded += OnGameEnded;
			_pinMame.OnDisplayAvailable += OnDisplayAvailable;
			_pinMame.OnDisplayUpdated += OnDisplayUpdated;

			if (!DisableAudio) {
				_pinMame.OnAudioAvailable += OnAudioAvailable;
				_pinMame.OnAudioUpdated += OnAudioUpdated;
			}

			_pinMame.OnMechAvailable += OnMechAvailable;
			_pinMame.OnMechUpdated += OnMechUpdated;
			_pinMame.OnSolenoidUpdated += OnSolenoidUpdated;
			_player = player;

			_solenoidsEnabled = SolenoidDelay == 0;

			try {
				_pinMame.StartGame(romId);

			} catch (Exception e) {
				Logger.Error(e);
			}
		}

		public void RegisterMech(PinMameMechComponent mechComponent)
		{
			var id = _numMechs++;
			_registeredMechs[id] = mechComponent;
		}

		private void OnGameStarted()
		{
			Logger.Info($"[PinMAME] Game started.");
			_isRunning = true;

			SendInitialSwitches();
			SendMechs();

			_solenoidDelayStart = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}

		private void Update()
		{
			if (_pinMame == null || !_isRunning) {
				return;
			}

			lock (_dispatchQueue) {
				while (_dispatchQueue.Count > 0) {
					_dispatchQueue.Dequeue().Invoke();
				}
			}

			// lamps
			foreach (var changedLamp in _pinMame.GetChangedLamps()) {
				if (_lamps.ContainsKey(changedLamp.Id)) {
					//Logger.Info($"[PinMAME] <= lamp {changedLamp.Id}: {changedLamp.Value}");
					OnLampChanged?.Invoke(this, new LampEventArgs(_lamps[changedLamp.Id].Id, changedLamp.Value));
				}
			}

			// gi
			foreach (var changedGi in _pinMame.GetChangedGIs()) {
				if (_lamps.ContainsKey(changedGi.Id)) {
					//Logger.Info($"[PinMAME] <= gi {changedGi.Id}: {changedGi.Value}");
					OnLampChanged?.Invoke(this, new LampEventArgs(_lamps[changedGi.Id].Id, changedGi.Value, LampSource.GI));
				} else {
					Debug.Log($"No GI {changedGi.Id} found.");
				}
			}
		}

		private void OnDisplayAvailable(int index, int displayCount, PinMameDisplayLayout displayLayout)
		{
			if (displayLayout.IsDmd) {
				lock (_dispatchQueue) {
					_dispatchQueue.Enqueue(() =>
						OnDisplaysAvailable?.Invoke(this, new AvailableDisplays(
							new DisplayConfig($"{DmdPrefix}{index}", displayLayout.Width, displayLayout.Height))));
				}

				_frameBuffer[index] = new byte[displayLayout.Width * displayLayout.Height];
				_dmdLevels[index] = displayLayout.Levels;

			} else {
				lock (_dispatchQueue) {
					_dispatchQueue.Enqueue(() =>
						OnDisplaysAvailable?.Invoke(this, new AvailableDisplays(
							new DisplayConfig($"{SegDispPrefix}{index}", displayLayout.Length, 1))));
				}

				_frameBuffer[index] = new byte[displayLayout.Length * 2];
				Logger.Info($"[PinMAME] Display {SegDispPrefix}{index} is of type {displayLayout.Type} at {displayLayout.Length} wide.");
			}
		}

		private void OnDisplayUpdated(int index, IntPtr framePtr, PinMameDisplayLayout displayLayout)
		{
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
				_dispatchQueue.Enqueue(() => OnDisplayFrame?.Invoke(this,
					new DisplayFrameData($"{DmdPrefix}{index}", GetDisplayFrameFormat(displayLayout), _frameBuffer[index])));
			}
		}

		private void UpdateSegDisp(int index, PinMameDisplayLayout displayLayout, IntPtr framePtr)
		{
			Marshal.Copy(framePtr, _frameBuffer[index], 0, displayLayout.Length * 2);

			lock (_dispatchQueue) {
				//Logger.Info($"[PinMAME] Seg data ({index}): {BitConverter.ToString(_frameBuffer[index])}" );
				_dispatchQueue.Enqueue(() => OnDisplayFrame?.Invoke(this,
					new DisplayFrameData($"{SegDispPrefix}{index}", GetDisplayFrameFormat(displayLayout), _frameBuffer[index])));
			}
		}

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
				var delta = AudioSettings.dspTime - _audioInputStart;
				var queueMs = System.Math.Round(_audioQueue.Count * (double)_audioInfo.SamplesPerFrame / _audioInfo.SampleRate * 1000);
				//Debug.Log($"INPUT: {System.Math.Round(_audioNumSamplesInput / delta)} - {_audioQueue.Count} in queue ({queueMs}ms)");
				_audioInputStart = AudioSettings.dspTime;
				_audioNumSamplesInput = 0;
			}

			float[] frame;
			if (_audioFilterChannels == _audioInfo.Channels) { // n channels -> n channels
				frame = new float[frameSize];
				unsafe {
					var src = (float*)framePtr;
					for (var i = 0; i < frameSize; i++) {
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
				var delta = AudioSettings.dspTime - _audioOutputStart;
				//Debug.Log($"OUTPUT: {System.Math.Round(_audioNumSamplesOutput / delta)}");
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

		private void OnSolenoidUpdated(int internalId, bool isActive)
		{
			if (_coils.ContainsKey(internalId))
			{
				if (!_solenoidsEnabled)
				{
					_solenoidsEnabled = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - _solenoidDelayStart) >= SolenoidDelay;

					if (_solenoidsEnabled)
					{
						Logger.Info($"Solenoids enabled, {SolenoidDelay}ms passed");
					}
				}

				if (_solenoidsEnabled)
				{
					Logger.Info($"[PinMAME] <= coil {_coils[internalId].Id} ({internalId}): {isActive} | {_coils[internalId].Description}");

					lock (_dispatchQueue)
					{
						_dispatchQueue.Enqueue(() =>
							OnCoilChanged?.Invoke(this, new CoilEventArgs(_coils[internalId].Id, isActive)));
					}
				}
				else
				{
					Logger.Info($"[PinMAME] <= solenoids disabled, coil {_coils[internalId].Id} ({internalId}): {isActive} | {_coils[internalId].Description}");
				}
			}
			else
			{
				Logger.Warn($"[PinMAME] <= coil UNMAPPED {internalId}: {isActive}");
			}
		}

		private void OnGameEnded()
		{
			Logger.Info($"[PinMAME] Game ended.");
			_isRunning = false;
		}

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
				if (_switches.ContainsKey(id) && !_mechSwitches.Contains(_switches[id].InternalId)) {
					Logger.Info($"[PinMAME] => sw {id} ({_switches[id].InternalId}): {true} | {_switches[id].Description}");
					_pinMame.SetSwitch(_switches[id].InternalId, true);
				}
			}
		}

		private void SendMechs()
		{
			var max = _pinMame.GetMaxMechs();
			foreach (var (id, mech) in _registeredMechs) {
				if (id > max) {
					Logger.Error($"PinMAME only supports up to {max} custom mechs, ignoring {mech.name}.");
					return;
				}
				var mechConfig = mech.Config(_player.SwitchMapping, _player.CoilMapping);
				_pinMame.SetMech(id, mechConfig);
				foreach (var c in mechConfig.SwitchList) {
					_mechSwitches.Add(c.SwNo);
				}
			}
		}

		private void UpdateCaches()
		{
			if (_game == null) {
				return;
			}
			_lamps.Clear();
			_coils.Clear();
			_switches.Clear();
			foreach (var lamp in _game.AvailableLamps) {
				_lamps[lamp.InternalId] = lamp;
			}
			foreach (var coil in _game.AvailableCoils) {
				_coils[coil.InternalId] = coil;
			}
			foreach (var sw in _game.AvailableSwitches) {
				_switches[sw.Id] = sw;
			}
		}

		private void OnDestroy()
		{
			StopGame();
		}

		public void StopGame()
		{
			if (_pinMame != null) {
				_pinMame.StopGame();
				_pinMame.OnGameStarted -= OnGameStarted;
				_pinMame.OnGameEnded -= OnGameEnded;
				_pinMame.OnDisplayAvailable -= OnDisplayAvailable;
				_pinMame.OnDisplayUpdated -= OnDisplayUpdated;

				if (!DisableAudio)
				{
					_pinMame.OnAudioAvailable -= OnAudioAvailable;
					_pinMame.OnAudioUpdated -= OnAudioUpdated;
				}

				_pinMame.OnMechAvailable -= OnMechAvailable;
				_pinMame.OnMechUpdated -= OnMechUpdated;
				_pinMame.OnSolenoidUpdated -= OnSolenoidUpdated;

			}
			_frameBuffer.Clear();
			_dmdLevels.Clear();
		}

		public void Switch(string id, bool isClosed)
		{
			if (_switches.ContainsKey(id)) {
				if (_mechSwitches.Contains(_switches[id].InternalId)) {
					// mech switches are triggered internally by pinmame.
					return;
				}
				Logger.Info($"[PinMAME] => sw {id} ({_switches[id].InternalId}): {isClosed} | {_switches[id].Description}");
				_pinMame.SetSwitch(_switches[id].InternalId, isClosed);
			} else {
				Logger.Error($"[PinMAME] Unknown switch \"{id}\".");
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
	}
}
