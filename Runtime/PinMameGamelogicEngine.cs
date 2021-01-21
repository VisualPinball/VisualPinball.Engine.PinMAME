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
			set {
				_game = value;
				UpdateCaches();
			}
		}

		[HideInInspector]
		public string romId = string.Empty;

		public GameObject dmd;


		public GamelogicEngineSwitch[] AvailableSwitches => _game?.AvailableSwitches ?? new GamelogicEngineSwitch[0];
		public GamelogicEngineCoil[] AvailableCoils => _coils.Values.ToArray();
		public GamelogicEngineLamp[] AvailableLamps => _lamps.Values.ToArray();

		public event EventHandler<CoilEventArgs> OnCoilChanged;

		public event EventHandler<LampEventArgs> OnLampChanged;
		public event EventHandler<LampsEventArgs> OnLampsChanged;
		public event EventHandler<LampColorEventArgs> OnLampColorChanged;

		private PinMame.PinMame _pinMame;
		private Player _player;
		private PinMameGame _game;

		private Dictionary<string, GamelogicEngineSwitch> _switches = new Dictionary<string, GamelogicEngineSwitch>();
		private Dictionary<int, GamelogicEngineCoil> _coils = new Dictionary<int, GamelogicEngineCoil>();
		private Dictionary<int, GamelogicEngineLamp> _lamps = new Dictionary<int, GamelogicEngineLamp>();

		private Texture2D _texture;
		private DmdDimensions _dmdDimensions;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly Color Tint = new Color(1, 0.18f, 0);

		private readonly Dictionary<byte, Color> _map = new Dictionary<byte, Color> {
			{0x0, Color.Lerp(Color.black, Tint, 0)},
			{0x14, Color.Lerp(Color.black, Tint, 0.33f)},
			{0x21, Color.Lerp(Color.black, Tint, 0.66f)},
			{0x43, Color.Lerp(Color.black, Tint, 1f)},
			{0x64, Color.Lerp(Color.black, Tint, 1f)}
		};

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
			var switches = _player.SwitchStatuses;
			Debug.Log("Sending initial switch statuses...");
			foreach (var sw in switches.Keys) {
				if (int.TryParse(sw, out var s)) {
					_pinMame.SetSwitch(s, switches[sw]);
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
			if (dmd != null && _pinMame.NeedsDmdUpdate()) {
				if (_texture == null) {
					_dmdDimensions = _pinMame.GetDmdDimensions();
					_texture = new Texture2D(_dmdDimensions.Width, _dmdDimensions.Height);
					dmd.GetComponent<Renderer>().sharedMaterial.mainTexture = _texture;
				}

				var frame = _pinMame.GetDmdPixels();
				if (frame.Length == _dmdDimensions.Width * _dmdDimensions.Height) {
					for (var y = 0; y < _dmdDimensions.Height; y++) {
						for (var x = 0; x < _dmdDimensions.Width; x++) {
							var pixel = frame[y * _dmdDimensions.Width + x];
							_texture.SetPixel(_dmdDimensions.Width - x, _dmdDimensions.Height - y, _map.ContainsKey(pixel) ? _map[pixel] : Color.magenta);
						}
					}
				}
				_texture.Apply();
			}
		}


		private void UpdateCaches()
		{
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
}
