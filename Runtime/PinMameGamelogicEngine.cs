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
using Logger = NLog.Logger;

namespace VisualPinball.Unity
{
	[Serializable]
	[DisallowMultipleComponent]
	[AddComponentMenu("Visual Pinball/Game Logic Engine/PinMAME")]
	public class PinMameGamelogicEngine : MonoBehaviour, IGamelogicEngine
	{
		public string Name { get; } = "PinMAME Gamelogic Engine";

		public string GameName = "mm_109c";

		public GameObject Dmd;

		public GamelogicEngineSwitch[] AvailableSwitches => _gameMeta.AvailableSwitches;
		public GamelogicEngineCoil[] AvailableCoils => _coils.Values.ToArray();
		public GamelogicEngineLamp[] AvailableLamps => _lamps.Values.ToArray();

		public event EventHandler<CoilEventArgs> OnCoilChanged;

		public event EventHandler<LampEventArgs> OnLampChanged;
		public event EventHandler<LampsEventArgs> OnLampsChanged;
		public event EventHandler<LampColorEventArgs> OnLampColorChanged;

		private PinMame.PinMame _pinMame;
		private Player _player;
		private readonly MedievalMadness _gameMeta;

		private Dictionary<int, GamelogicEngineSwitch> _switches = new Dictionary<int, GamelogicEngineSwitch>();
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

		public PinMameGamelogicEngine()
		{
			_gameMeta = new MedievalMadness();
			foreach (var lamp in _gameMeta.AvailableLamps) {
				if (int.TryParse(lamp.Id, out var id)) {
					_lamps[id] = lamp;

				} else {
					Logger.Error("Cannot parse lamp ID " + lamp.Id);
				}
			}
			foreach (var coil in _gameMeta.AvailableCoils) {
				if (int.TryParse(coil.Id, out var id)) {
					_coils[id] = coil;

				} else {
					Logger.Error("Cannot parse coil ID " + coil.Id);
				}
			}
		}

		public void OnInit(Player player, TableApi tableApi, BallManager ballManager)
		{
			// turn off all lamps
			foreach (var lamp in _lamps.Values) {
				OnLampChanged?.Invoke(this, new LampEventArgs(lamp.Id, 0));
			}

			_pinMame = PinMame.PinMame.Instance();
			_pinMame.StartGame(GameName, showConsole: true);
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
				var id = changedCoils[i];
				var val = changedCoils[i + 1];

				if (_coils.ContainsKey(id)) {
					Logger.Info($"[PinMAME] <= coil {id}: {val}");
					OnCoilChanged?.Invoke(this, new CoilEventArgs(_coils[id].Id, val == 1));
				}
			}

			// lamps
			var changedLamps = _pinMame.GetChangedLamps();
			for (var i = 0; i < changedLamps.Length; i += 2) {
				var id = changedLamps[i];
				var val = changedLamps[i + 1];

				if (_lamps.ContainsKey(id)) {
					//Logger.Info($"[PinMAME] <= lamp {id}: {val}");
					OnLampChanged?.Invoke(this, new LampEventArgs(_lamps[id].Id, val));
				}
			}

			// dmd
			if (Dmd != null && _pinMame.NeedsDmdUpdate()) {
				if (_texture == null) {
					_dmdDimensions = _pinMame.GetDmdDimensions();
					_texture = new Texture2D(_dmdDimensions.Width, _dmdDimensions.Height);
					Dmd.GetComponent<Renderer>().sharedMaterial.mainTexture = _texture;
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

		private void OnDestroy()
		{
			_pinMame?.StopGame();
		}

		public void Switch(string id, bool isClosed)
		{
			if (int.TryParse(id, out var n)) {
				Logger.Info($"[PinMAME] => sw {id}: {isClosed}");
				_pinMame.SetSwitch(n, isClosed);

			} else {
				Logger.Error($"[PinMAME] Cannot parse switch ID {id}.");
			}
		}
	}
}
