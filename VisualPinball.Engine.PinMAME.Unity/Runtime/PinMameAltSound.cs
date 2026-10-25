// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;
using NLog;
using UnityEngine;
using VisualPinball.Unity;
using Logger = NLog.Logger;

namespace VisualPinball.Engine.PinMAME
{
	/// <summary>
	/// Plays an AltSound pack instead of the ROM's own audio: replacement samples that PinMAME's sound commands trigger. The pack is
	/// a folder named after the ROM, such as <c>altsound/&lt;rom&gt;</c>, and libaltsound (in the bundled VpeAltSound library) mixes it on
	/// its own realtime thread.
	/// </summary>
	/// <remarks>
	/// The mix plays through VPE's central mixer on the ROM bus when it is on, and otherwise through the engine's own Unity filter
	/// (<see cref="Mix"/>). Start and stop are main thread only; <see cref="Command"/> and <see cref="SetHardwareGen"/> come from
	/// the emulation thread. libaltsound is one instance per process, so only one pack plays at a time.
	/// </remarks>
	internal sealed class PinMameAltSound : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private const string Library = "VpeAltSound";
		private const int BlockFrames = 256;
		private const int RingFrames = 16384; // about 340 ms at 48 kHz for Unity playback; a stall drops the oldest half
		private const int RingPrime = 2048; // what Unity playback waits for before it starts: libaltsound delivers blocks in bursts
		// libaltsound mixes at the rate of the packs, which nearly all are 44.1 kHz: its own resampling (miniaudio's engine,
		// which turns its low-pass off) puts images of the treble back into the audible band, about 20 dB under the music.
		private const int PackRate = 44100;
		private const float MaxGain = 2;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void AudioCallback(IntPtr samples, int frames, int sampleRate, int channels, IntPtr userData);
		[DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
		private static extern int VpeAltSoundInit(byte[] basePath, byte[] gameName, int sampleRate, int bufferFrames, AudioCallback callback, IntPtr userData, byte[] logFolder);
		[DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
		private static extern void VpeAltSoundSetHardwareGen(ulong hardwareGen);
		[DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
		private static extern int VpeAltSoundCommand(int board, int command);
		[DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
		private static extern void VpeAltSoundPause(int paused);
		[DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
		private static extern void VpeAltSoundFollowGameVolume(int follow);
		[DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
		private static extern void VpeAltSoundShutdown();

		private static readonly AudioCallback Callback = OnAudio;
		private static PinMameAltSound _current; // the instance the native callback feeds

		/// <summary>The pack's folder, for the status line.</summary>
		public string Folder { get; }

		private readonly PcmAudioStream _stream;
		private readonly int _rate; // what the mix is pushed at: the output rate
		private readonly Resampler _resampler; // null when the output runs at the packs' rate
		private readonly float[] _ring = new float[RingFrames * 2];
		private long _written, _read; // frames; the native thread writes, Unity's audio thread reads
		private bool _priming = true; // Unity's audio thread
		private float _gain = 1, _appliedGain = 1; // set on the main thread, ramped to on the native thread
		private float[] _block = Array.Empty<float>(); // native thread
		private bool? _followGameVolume;
		private bool _paused;

		private PinMameAltSound(string folder, PcmAudioStream stream, int rate)
		{
			Folder = folder;
			_stream = stream;
			_rate = rate;
			_resampler = rate != PackRate ? new Resampler(PackRate, rate) : null;
		}

		/// <summary>
		/// Finds the ROM's pack and starts it, or returns null with the reason. <paramref name="folder"/> is the folder holding a subfolder
		/// per ROM, the ROM's own folder, or a folder with an <c>altsound</c> folder inside; empty looks next to VPinMAME's ROMs and in
		/// VPE's PinMAME folder.
		/// </summary>
		public static PinMameAltSound Start(string folder, string romId, int outputRate, out string status)
		{
			if (_current != null) { status = "Another table's AltSound is playing."; return null; }
			var pack = Find(folder, romId);
			if (pack == null) {
				status = string.IsNullOrWhiteSpace(folder) ? $"No AltSound folder for {romId} found." : $"No AltSound folder for {romId} in {folder}.";
				return null;
			}
			// libaltsound reads <base>/altsound/<rom>, so the pack's parent has to be called "altsound".
			var root = Path.GetDirectoryName(pack);
			var basePath = Path.GetDirectoryName(root);
			if (basePath == null || !string.Equals(Path.GetFileName(root), "altsound", StringComparison.OrdinalIgnoreCase)) {
				status = $"AltSound packs must be in a folder named \"altsound\", {pack} isn't.";
				return null;
			}
			var renderer = PcmAudioRenderer.Current;
			var stream = renderer ? renderer.OpenStream(SoundBuses.Rom) : null;
			var altSound = new PinMameAltSound(pack, stream, outputRate);
			_current = altSound;
			int result;
			try {
				result = VpeAltSoundInit(Utf8(basePath), Utf8(Path.GetFileName(pack)), PackRate, BlockFrames, Callback, IntPtr.Zero, Utf8(PinMameFolders.DataFolder));
			} catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException) {
				result = int.MinValue;
				Logger.Warn(e, "[PinMAME] AltSound is not available on this platform.");
			}
			if (result == 1) {
				status = $"Playing {pack}" + (stream != null ? "." : " through Unity.");
				Logger.Info("[PinMAME] AltSound plays {Pack:l} at {Rate} Hz, {Output:l}.", pack, outputRate, stream != null ? "through the central mixer" : "through Unity");
				return altSound;
			}
			_current = null;
			stream?.Dispose();
			status = result switch {
				int.MinValue => "AltSound is not available on this platform.",
				-1 => $"The path of {pack} can't be read by AltSound. Move it to a folder whose name has only plain characters.",
				_ => $"AltSound couldn't load {pack}. See altsound.log in {PinMameFolders.DataFolder}.",
			};
			Logger.Warn("[PinMAME] {Status:l}", status);
			return null;
		}

		private static string Find(string folder, string romId)
		{
			if (string.IsNullOrWhiteSpace(romId)) return null;
			var candidates = new System.Collections.Generic.List<string>();
			if (!string.IsNullOrWhiteSpace(folder)) {
				folder = folder.Trim().TrimEnd('/', '\\');
				candidates.Add(Path.Combine(folder, romId));                  // the folder with a subfolder per ROM
				if (string.Equals(Path.GetFileName(folder), romId, StringComparison.OrdinalIgnoreCase)) candidates.Add(folder); // the ROM's folder
				candidates.Add(Path.Combine(folder, "altsound", romId));      // VPinMAME's folder
			} else {
				var roms = PinMame.PinMame.FindVpmPath(PinMame.PinMameFileType.Roms);
				if (!string.IsNullOrEmpty(roms)) foreach (var romFolder in roms.Split(';')) {
					var vpm = Path.GetDirectoryName(romFolder.Trim().TrimEnd('/', '\\'));
					if (!string.IsNullOrEmpty(vpm)) candidates.Add(Path.Combine(vpm, "altsound", romId));
				}
				candidates.Add(Path.Combine(PinMameFolders.DataFolder, "altsound", romId));
			}
			foreach (var candidate in candidates) {
				try {
					if (Directory.Exists(candidate)) return Path.GetFullPath(candidate);
				} catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException || e is NotSupportedException) {
					// an unusable configured path is reported as not found
				}
			}
			return null;
		}

		/// <summary>The ROM's hardware generation, which decides how commands are decoded. Any thread.</summary>
		public void SetHardwareGen(long hardwareGen) => VpeAltSoundSetHardwareGen((ulong)hardwareGen);

		/// <summary>A command PinMAME sent to a sound board. Emulation thread.</summary>
		public void Command(int board, int command) => VpeAltSoundCommand(board, command);

		/// <summary>The pack's volume, 0..2, from the main thread. Applied to the mix before it's handed on, so it can amplify.</summary>
		public float Gain {
			set => Volatile.Write(ref _gain, Mathf.Clamp(float.IsNaN(value) ? 0 : value, 0, MaxGain));
		}

		/// <summary>
		/// Whether the game's own volume setting scales the pack too, as in Visual PinMAME. Off, the pack plays at its own level and
		/// VPE's volume controls alone set it. Main thread.
		/// </summary>
		public bool FollowGameVolume {
			set {
				if (_followGameVolume == value) return;
				_followGameVolume = value;
				VpeAltSoundFollowGameVolume(value ? 1 : 0);
			}
		}

		/// <summary>Pauses the pack with the game, so music continues where it was. Main thread.</summary>
		public bool Paused {
			set {
				if (_paused == value) return;
				_paused = value;
				VpeAltSoundPause(value ? 1 : 0);
			}
		}

		[MonoPInvokeCallback(typeof(AudioCallback))]
		private static void OnAudio(IntPtr samples, int frames, int sampleRate, int channels, IntPtr userData)
		{
			var altSound = Volatile.Read(ref _current);
			if (altSound == null || samples == IntPtr.Zero || frames <= 0 || channels != 2) return;
			unsafe {
				var input = new ReadOnlySpan<float>((void*)samples, frames * 2);
				var resampler = altSound._resampler;
				int capacity = resampler != null ? resampler.Capacity(frames) : frames;
				if (altSound._block.Length < capacity * 2) altSound._block = new float[capacity * 2];
				var block = altSound._block;
				int count = resampler != null ? resampler.Process(input, frames, block) : Copy(input, block, frames);
				if (count <= 0) return;
				altSound.ApplyGain(block, count);
				var output = new ReadOnlySpan<float>(block, 0, count * 2);
				if (altSound._stream != null && altSound._stream.Push(output, count, 2, altSound._rate)) return;
				altSound.Write(output, count);
			}
		}

		private static int Copy(ReadOnlySpan<float> input, float[] block, int frames)
		{
			input.Slice(0, frames * 2).CopyTo(block);
			return frames;
		}

		// Ramps from the previous gain over the block, so moving the slider doesn't click.
		private void ApplyGain(float[] block, int frames)
		{
			float target = Volatile.Read(ref _gain), from = _appliedGain;
			if (from == target && target == 1) return;
			for (int i = 0; i < frames; ++i) {
				float gain = from + (target - from) * (i + 1) / frames;
				block[2 * i] *= gain;
				block[2 * i + 1] *= gain;
			}
			_appliedGain = target;
		}

		// Without the mixer, the engine's Unity filter takes the mix from this ring.
		private void Write(ReadOnlySpan<float> block, int frames)
		{
			long written = Volatile.Read(ref _written), read = Volatile.Read(ref _read);
			if (written + frames - read > RingFrames) return; // Unity isn't reading (paused or stalled): drop rather than play late
			for (int i = 0; i < frames; ++i) {
				int slot = (int)((written + i) % RingFrames) * 2;
				_ring[slot] = block[2 * i];
				_ring[slot + 1] = block[2 * i + 1];
			}
			Volatile.Write(ref _written, written + frames);
		}

		/// <summary>Adds the pack to Unity's output when it doesn't play through the mixer. Unity's audio thread.</summary>
		public void Mix(float[] data, int channels)
		{
			if (channels <= 0) return;
			int wanted = data.Length / channels;
			long written = Volatile.Read(ref _written), read = Volatile.Read(ref _read);
			if (written - read > RingFrames / 2) read = written - RingFrames / 4; // fell behind: keep the latency bounded
			if (_priming) {
				if (written - read < RingPrime + wanted) return; // fill up first, so the bursts don't run it dry
				_priming = false;
			}
			int frames = (int)System.Math.Min(written - read, wanted);
			if (frames < wanted) _priming = true; // ran dry: fill up again instead of stuttering
			for (int i = 0; i < frames; ++i) {
				int slot = (int)((read + i) % RingFrames) * 2;
				float left = _ring[slot], right = _ring[slot + 1];
				if (channels == 1) data[i] += (left + right) * .5f;
				else { data[i * channels] += left; data[i * channels + 1] += right; }
			}
			Volatile.Write(ref _read, read + frames);
		}

		/// <summary>Stops the pack. Main thread; the native callback has returned when this does.</summary>
		public void Dispose()
		{
			if (_current != this) return;
			try {
				VpeAltSoundShutdown();
			} catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException) {
				// never started
			}
			Volatile.Write(ref _current, null);
			_stream?.Dispose();
			Logger.Info("[PinMAME] AltSound stopped.");
		}

		/// <summary>
		/// Converts the stereo mix to the output rate with a 64-tap Kaiser-windowed sinc, cut off at 19.5 kHz for the 44.1 kHz packs
		/// (images and aliases about 60 dB down). Native thread only.
		/// </summary>
		private sealed class Resampler
		{
			private const int Taps = 64, Half = Taps / 2, Phases = 256;
			private readonly float[] _table = new float[(Phases + 1) * Taps]; // a row of taps per fractional position
			private readonly double _step; // input frames per output frame
			private float[] _history = new float[4096 * 2]; // interleaved input frames not used up yet
			private int _count = Half - 1; // frames in _history, starting with silence so the first output has its past
			private double _position = Half - 1; // input position of the next output frame, in _history

			public Resampler(int inputRate, int outputRate)
			{
				_step = (double)inputRate / outputRate;
				double cutoff = System.Math.Min(1.0, (double)outputRate / inputRate) * 19500.0 / inputRate; // cycles per input sample
				const double beta = 7.0;
				for (int p = 0; p <= Phases; ++p) {
					double fraction = (double)p / Phases, sum = 0;
					for (int k = 0; k < Taps; ++k) {
						double t = fraction + Half - 1 - k; // distance from the output position to tap k
						double sinc = t == 0 ? 1 : System.Math.Sin(2 * System.Math.PI * cutoff * t) / (2 * System.Math.PI * cutoff * t);
						double edge = t / Half;
						double window = System.Math.Abs(edge) >= 1 ? 0 : BesselI0(beta * System.Math.Sqrt(1 - edge * edge)) / BesselI0(beta);
						double value = 2 * cutoff * sinc * window;
						_table[p * Taps + k] = (float)value;
						sum += value;
					}
					for (int k = 0; k < Taps; ++k) _table[p * Taps + k] = (float)(_table[p * Taps + k] / sum); // unity gain at DC
				}
			}

			/// <summary>The most output frames <see cref="Process"/> can return for this many input frames.</summary>
			public int Capacity(int frames) => (int)System.Math.Ceiling((frames + _count) / _step) + 2;

			/// <summary>Takes interleaved stereo input and writes what it can of the output; returns the output frame count.</summary>
			public int Process(ReadOnlySpan<float> input, int frames, float[] output)
			{
				if ((_count + frames) * 2 > _history.Length) Array.Resize(ref _history, (_count + frames) * 4);
				input.Slice(0, frames * 2).CopyTo(new Span<float>(_history, _count * 2, frames * 2));
				_count += frames;
				int produced = 0;
				while (true) {
					int index = (int)_position;
					if (index + Half >= _count) break; // needs Half input frames past the position
					double phase = (_position - index) * Phases;
					int row = (int)phase;
					float blend = (float)(phase - row);
					int first = row * Taps, second = first + Taps, start = (index - Half + 1) * 2;
					float left = 0, right = 0;
					for (int k = 0; k < Taps; ++k) {
						float tap = _table[first + k] + (_table[second + k] - _table[first + k]) * blend;
						left += _history[start + 2 * k] * tap;
						right += _history[start + 2 * k + 1] * tap;
					}
					output[produced * 2] = left;
					output[produced * 2 + 1] = right;
					++produced;
					_position += _step;
				}
				// keep the Half - 1 frames before the next position, for its past taps
				int drop = (int)_position - (Half - 1);
				if (drop > 0) {
					Array.Copy(_history, drop * 2, _history, 0, (_count - drop) * 2);
					_count -= drop;
					_position -= drop;
				}
				return produced;
			}

			private static double BesselI0(double x)
			{
				double sum = 1, term = 1, half = x / 2;
				for (int k = 1; k < 32; ++k) {
					term *= half / k;
					sum += term * term;
				}
				return sum;
			}
		}

		private static byte[] Utf8(string text)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty);
			Array.Resize(ref bytes, bytes.Length + 1);
			return bytes;
		}
	}
}
