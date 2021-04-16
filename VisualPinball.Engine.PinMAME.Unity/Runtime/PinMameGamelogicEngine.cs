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

using System;
using System.Collections.Generic;
using System.Linq;
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

		private const string DisplayDmd = "dmd";

		private bool _isRunning;
		private bool _sizeAnnounced;
		private byte[] _frameBuffer;

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

			_pinMame = PinMame.PinMame.Instance(48000, @"D:\Pinball\Visual Pinball\VPinMAME");
			_pinMame.OnGameStarted += GameStarted;
			_pinMame.OnGameEnded += GameEnded;
			_pinMame.OnDisplayUpdate += DisplayUpdated;
			_pinMame.OnSolenoid += SolenoidChanged;
			_player = player;

			try {
				_pinMame.StartGame(romId);

			} catch (Exception e) {
				Logger.Error(e);
			}
		}

		private void GameStarted(object sender, EventArgs e)
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

		private void DisplayUpdated(object sender, EventArgs e, int index, IntPtr framePtr, PinMameDisplayLayout displayLayout)
		{
			if ((displayLayout.type & PinMameDisplayType.DMD) > 0) {
				UpdateDmd(index, displayLayout, framePtr);
			} else {
				UpdateSegDisp(index, displayLayout, framePtr);
			}
		}

		private void UpdateSegDisp(int index, PinMameDisplayLayout displayLayout, IntPtr framePtr)
		{

		}


		private void UpdateDmd(int index, PinMameDisplayLayout displayLayout, IntPtr framePtr)
		{
			if (!_sizeAnnounced) {
				lock (_dispatchQueue) {
					_dispatchQueue.Enqueue(() =>
						OnDisplaysAvailable?.Invoke(this, new AvailableDisplays(
							new DisplayConfig(DisplayDmd, displayLayout.width, displayLayout.height))));
				}

				_sizeAnnounced = true;
				_frameBuffer = new byte[displayLayout.width * displayLayout.height];
			}

			var map = GetMap(displayLayout);
			unsafe {
				var ptr = (byte*) framePtr;
				for (var y = 0; y < displayLayout.height; y++) {
					for (var x = 0; x < displayLayout.width; x++) {
						var pos = y * displayLayout.width + x;
						_frameBuffer[pos] =  map[ptr[pos]];
					}
				}
			}

			lock (_dispatchQueue) {
				_dispatchQueue.Enqueue(() => OnDisplayFrame?.Invoke(this,
					new DisplayFrameData(DisplayDmd, GetDisplayType(displayLayout.type), _frameBuffer)));
			}
		}

		private void SolenoidChanged(object sender, EventArgs e, int internalId, bool isActive)
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

		private void GameEnded(object sender, EventArgs e)
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
			if (_pinMame != null) {
				_pinMame.StopGame();
				_pinMame.OnGameStarted -= GameStarted;
				_pinMame.OnGameEnded -= GameEnded;
				_pinMame.OnDisplayUpdate -= DisplayUpdated;
			}
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


		private Dictionary<byte, byte> GetMap(PinMameDisplayLayout displayLayout)
		{
			if (displayLayout.depth == 2) {
				return DmdMap2Bit;
			}

			return (_pinMame.GetHardwareGen() & (PinMameHardwareGen.SAM | PinMameHardwareGen.SPA)) > 0
				? DmdMapSam
				: DmdMapGts;
		}

		private static DisplayFrameFormat GetDisplayType(PinMameDisplayType dp)
		{
			switch (dp) {
				case PinMameDisplayType.SEG16:
					break;
				case PinMameDisplayType.SEG16R:
					break;
				case PinMameDisplayType.SEG10:
					break;
				case PinMameDisplayType.SEG9:
					break;
				case PinMameDisplayType.SEG8:
					break;
				case PinMameDisplayType.SEG8D:
					break;
				case PinMameDisplayType.SEG7:
					break;
				case PinMameDisplayType.SEG87:
					break;
				case PinMameDisplayType.SEG87F:
					break;
				case PinMameDisplayType.SEG98:
					break;
				case PinMameDisplayType.SEG98F:
					break;
				case PinMameDisplayType.SEG7S:
					break;
				case PinMameDisplayType.SEG7SC:
					break;
				case PinMameDisplayType.SEG16S:
					break;
				case PinMameDisplayType.DMD:
					return DisplayFrameFormat.Dmd2;

				case PinMameDisplayType.VIDEO:
					break;
				case PinMameDisplayType.SEG16N:
					break;
				case PinMameDisplayType.SEG16D:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(dp), dp, null);
			}

			throw new NotImplementedException("only dmd frames supported for now");
		}

		private static readonly Dictionary<byte, byte> DmdMap2Bit = new Dictionary<byte, byte> {
			{ 0x00, 0x0 },
			{ 0x14, 0x0 },
			{ 0x21, 0x1 },
			{ 0x43, 0x2 },
			{ 0x64, 0x3 },
		};

		private static readonly Dictionary<byte, byte> DmdMapSam = new Dictionary<byte, byte> {
			{ 0x00, 0x0 }, { 0x14, 0x1 }, { 0x19, 0x2 }, { 0x1E, 0x3 },
			{ 0x23, 0x4 }, { 0x28, 0x5 }, { 0x2D, 0x6 }, { 0x32, 0x7 },
			{ 0x37, 0x8 }, { 0x3C, 0x9 }, { 0x41, 0xa }, { 0x46, 0xb },
			{ 0x4B, 0xc }, { 0x50, 0xd }, { 0x5A, 0xe }, { 0x64, 0xf }
		};

		private static readonly Dictionary<byte, byte> DmdMapGts = new Dictionary<byte, byte> {
			{ 0x00, 0x0 }, { 0x1E, 0x1 }, { 0x23, 0x2 }, { 0x28, 0x3 },
			{ 0x2D, 0x4 }, { 0x32, 0x5 }, { 0x37, 0x6 }, { 0x3C, 0x7 },
			{ 0x41, 0x8 }, { 0x46, 0x9 }, { 0x4B, 0xa }, { 0x50, 0xb },
			{ 0x55, 0xc }, { 0x5A, 0xd }, { 0x5F, 0xe }, { 0x64, 0xf }
		};

	}
}
