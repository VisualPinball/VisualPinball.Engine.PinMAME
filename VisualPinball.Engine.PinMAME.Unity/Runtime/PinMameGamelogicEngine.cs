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

		private bool _sizeAnnounced;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly Color Tint = new Color(1, 0.18f, 0);

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
			_pinMame.StartGame(romId, showConsole: true);
			_player = player;
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

		private void Update()
		{
			if (_pinMame == null || !_pinMame.IsRunning) {
				return;
			}

			// coils
			var changedCoils = _pinMame.GetChangedSolenoids();
			for (var i = 0; i < changedCoils.Length; i += 2) {
				var internalId = changedCoils[i];
				var val = changedCoils[i + 1];

				if (_coils.ContainsKey(internalId)) {
					Logger.Info($"[PinMAME] <= coil {_coils[internalId].Id} ({internalId}): {val} | {_coils[internalId].Description}");
					OnCoilChanged?.Invoke(this, new CoilEventArgs(_coils[internalId].Id, val == 1));

				} else {
					Logger.Warn($"[PinMAME] <= coil UNMAPPED {internalId}: {val}");
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

			// dmd
			if (_pinMame.NeedsDmdUpdate()) {
				if (!_sizeAnnounced) {
					var dim = _pinMame.GetDmdDimensions();
					OnDisplaysAvailable?.Invoke(this, new AvailableDisplays(
						new DisplayConfig(DisplayDmd, DisplayType.Dmd2PinMame, dim.Width, dim.Height)));
					_sizeAnnounced = true;
				}
				OnDisplayFrame?.Invoke(this, new DisplayFrameData(DisplayDmd, _pinMame.GetDmdPixels()));
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
			_pinMame?.StopGame();
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
	}

	// internal readonly struct DisplayKey : IEquatable<DisplayKey>
	// {
	// 	private readonly int _width;
	// 	private readonly int _height;
	// 	private readonly int _bitLength;
	//
	// 	public DisplayKey(int width, int height, int bitLength)
	// 	{
	// 		_width = width;
	// 		_height = height;
	// 		_bitLength = bitLength;
	// 	}
	//
	// 	public override bool Equals(object obj) => obj is DisplayKey other && Equals(other);
	//
	// 	public bool Equals(DisplayKey other)
	// 	{
	// 		return _width == other._width && _height == other._height && _bitLength == other._bitLength;
	// 	}
	//
	// 	public override int GetHashCode() => (_width, _height, _bitLength).GetHashCode();
	// }
}
