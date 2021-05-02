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
	[AddComponentMenu("Visual Pinball/Game Logic Engine/PinMAME")]
	public class PinMameGamelogicEngine : MonoBehaviour, IGamelogicEngine
	{
		public string Name { get; } = "PinMAME Gamelogic Engine";

		public const string DmdPrefix = "dmd";
		public const string SegDispPrefix = "display";

		public PinMameGame Game {
			get => _game;
			set => _game = value;
		}

		[HideInInspector]
		public string romId = string.Empty;

		public GamelogicEngineSwitch[] AvailableSwitches {
			get {
				UpdateCaches();
				return _game?.AvailableSwitches ?? new GamelogicEngineSwitch[0];
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
		private Dictionary<int, byte[]> _frameBuffer = new Dictionary<int, byte[]>();
		private Dictionary<int, Dictionary<byte, byte>> _dmdLevels = new Dictionary<int, Dictionary<byte, byte>>();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly Color Tint = new Color(1, 0.18f, 0);

		private readonly Queue<Action> _dispatchQueue = new Queue<Action>();

		private void Start()
		{
			UpdateCaches();
		}

		public void OnInit(Player player, TableApi tableApi, BallManager ballManager)
		{
			// turn off all lamps
			foreach (var lamp in _lamps.Values) {
				OnLampChanged?.Invoke(this, new LampEventArgs(lamp.Id, 0));
			}

			_pinMame = PinMame.PinMame.Instance();
			_pinMame.OnGameStarted += GameStarted;
			_pinMame.OnGameEnded += GameEnded;
			_pinMame.OnDisplayUpdated += DisplayUpdated;
			_pinMame.OnSolenoidUpdated += SolenoidUpdated;
			_pinMame.OnDisplayAvailable += OnDisplayAvailable;
			_player = player;

			try {
				//_pinMame.StartGame("fh_906h");
				_pinMame.StartGame(romId);

			} catch (Exception e) {
				Logger.Error(e);
			}
		}

		private void GameStarted()
		{
			Logger.Info($"[PinMAME] Game started.");
			_isRunning = true;
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
			var changedLamps = _pinMame.GetChangedLamps();
			for (var i = 0; i < changedLamps.Length; i += 2) {
				var internalId = changedLamps[i];
				var val = changedLamps[i + 1];

				if (_lamps.ContainsKey(internalId)) {
					//Logger.Info($"[PinMAME] <= lamp {id}: {val}");
					OnLampChanged?.Invoke(this, new LampEventArgs(_lamps[internalId].Id, val));
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

		private void DisplayUpdated(int index, IntPtr framePtr, PinMameDisplayLayout displayLayout)
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
						_frameBuffer[index][pos] = _dmdLevels[index][ptr[pos]];
					}
				}
			}

			lock (_dispatchQueue) {
				_dispatchQueue.Enqueue(() => OnDisplayFrame?.Invoke(this,
					new DisplayFrameData($"{DmdPrefix}{index}", GetDisplayType(displayLayout.Type), _frameBuffer[index])));
			}
		}

		private void UpdateSegDisp(int index, PinMameDisplayLayout displayLayout, IntPtr framePtr)
		{
			Marshal.Copy(framePtr, _frameBuffer[index], 0, displayLayout.Length * 2);

			lock (_dispatchQueue) {
				//Logger.Info($"[PinMAME] Seg data ({index}): {BitConverter.ToString(_frameBuffer[index])}" );
				_dispatchQueue.Enqueue(() => OnDisplayFrame?.Invoke(this,
					new DisplayFrameData($"{SegDispPrefix}{index}", GetDisplayType(displayLayout.Type), _frameBuffer[index])));
			}
		}

		private void SolenoidUpdated(int internalId, bool isActive)
		{
			if (_coils.ContainsKey(internalId)) {
				Logger.Info($"[PinMAME] <= coil {_coils[internalId].Id} ({internalId}): {isActive} | {_coils[internalId].Description}");

				lock (_dispatchQueue) {
					_dispatchQueue.Enqueue(() =>
						OnCoilChanged?.Invoke(this, new CoilEventArgs(_coils[internalId].Id, isActive)));
				}

			} else {
				Logger.Warn($"[PinMAME] <= coil UNMAPPED {internalId}: {isActive}");
			}
		}

		private void GameEnded()
		{
			Logger.Info($"[PinMAME] Game ended.");
			_isRunning = false;
		}

		public void SendInitialSwitches()
		{
			var switches = _player.SwitchStatusesClosed;
			Logger.Info("[PinMAME] Sending initial switch statuses...");
			foreach (var id in switches.Keys) {
				var isClosed = switches[id];
				// skip open switches
				if (!isClosed) {
					continue;
				}
				if (_switches.ContainsKey(id)) {
					Logger.Info($"[PinMAME] => sw {id} ({_switches[id].InternalId}): {true} | {_switches[id].Description}");
					_pinMame.SetSwitch(_switches[id].InternalId, true);
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
				_pinMame.OnGameStarted -= GameStarted;
				_pinMame.OnGameEnded -= GameEnded;
				_pinMame.OnDisplayUpdated -= DisplayUpdated;
				_pinMame.OnSolenoidUpdated -= SolenoidUpdated;
			}
			_frameBuffer.Clear();
			_dmdLevels.Clear();
		}

		public void Switch(string id, bool isClosed)
		{
			if (_switches.ContainsKey(id)) {
				Logger.Info($"[PinMAME] => sw {id} ({_switches[id].InternalId}): {isClosed} | {_switches[id].Description}");
				_pinMame.SetSwitch(_switches[id].InternalId, isClosed);
			} else {
				Logger.Error($"[PinMAME] Unknown switch \"{id}\".");
			}
		}

		public static DisplayFrameFormat GetDisplayType(PinMameDisplayType dp)
		{
			switch (dp) {
				case PinMameDisplayType.Seg8:   // 7  segments and comma
				case PinMameDisplayType.Seg8D:  // 7  segments and period
				case PinMameDisplayType.Seg7:   // 7  segments
				case PinMameDisplayType.Seg87:  // 7  segments, comma every three
				case PinMameDisplayType.Seg87F: // 7  segments, forced comma every three
				case PinMameDisplayType.Seg7S:  // 7  segments, small
				case PinMameDisplayType.Seg7SC: // 7  segments, small, with comma
					return DisplayFrameFormat.Segment7;

				case PinMameDisplayType.Seg10: // 9  segments and comma
				case PinMameDisplayType.Seg9: // 9  segments
				case PinMameDisplayType.Seg98: // 9  segments, comma every three
				case PinMameDisplayType.Seg98F: // 9  segments, forced comma every three
					return DisplayFrameFormat.Segment9;

				case PinMameDisplayType.Seg16:  // 16 segments
				case PinMameDisplayType.Seg16R: // 16 segments with comma and period reversed
				case PinMameDisplayType.Seg16N: // 16 segments without commas
				case PinMameDisplayType.Seg16D: // 16 segments with periods only
				case PinMameDisplayType.Seg16S: // 16 segments with split top and bottom line
					return DisplayFrameFormat.Segment16;

				case PinMameDisplayType.Dmd:
					return DisplayFrameFormat.Dmd2;

				case PinMameDisplayType.Video:
					break;

				case PinMameDisplayType.SegAll:
				case PinMameDisplayType.Import:
				case PinMameDisplayType.SegMask:
				case PinMameDisplayType.SegHiBit:
				case PinMameDisplayType.SegRev:
				case PinMameDisplayType.DmdNoAA:
				case PinMameDisplayType.NoDisp:
				case PinMameDisplayType.Seg8H:
				case PinMameDisplayType.Seg7H:
				case PinMameDisplayType.Seg87H:
				case PinMameDisplayType.Seg87FH:
				case PinMameDisplayType.Seg7SH:
				case PinMameDisplayType.Seg7SCH:
					throw new ArgumentOutOfRangeException(nameof(dp), dp, null);

				default:
					throw new ArgumentOutOfRangeException(nameof(dp), dp, null);
			}

			throw new NotImplementedException($"Still unsupported segmented display format: {dp}.");
		}
	}
}
