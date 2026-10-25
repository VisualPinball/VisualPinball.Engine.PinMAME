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
		private static extern void VpeAltSoundShutdown();

		private static readonly AudioCallback Callback = OnAudio;
		private static PinMameAltSound _current; // the instance the native callback feeds

		/// <summary>The pack's folder, for the status line.</summary>
		public string Folder { get; }

		private readonly PcmAudioStream _stream;
		private readonly float[] _ring = new float[RingFrames * 2];
		private long _written, _read; // frames; the native thread writes, Unity's audio thread reads
		private float _gain = 1;
		private bool _gainSet, _paused;

		private PinMameAltSound(string folder, PcmAudioStream stream)
		{
			Folder = folder;
			_stream = stream;
		}

		/// <summary>
		/// Finds the ROM's pack and starts it, or returns null with the reason. <paramref name="folder"/> is the folder holding a subfolder
		/// per ROM, the ROM's own folder, or a folder with an <c>altsound</c> folder inside; empty looks next to VPinMAME's ROMs and in
		/// VPE's PinMAME folder.
		/// </summary>
		public static PinMameAltSound Start(string folder, string romId, int sampleRate, out string status)
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
			var altSound = new PinMameAltSound(pack, stream);
			_current = altSound;
			int result;
			try {
				result = VpeAltSoundInit(Utf8(basePath), Utf8(Path.GetFileName(pack)), sampleRate, BlockFrames, Callback, IntPtr.Zero, Utf8(PinMameFolders.DataFolder));
			} catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException) {
				result = int.MinValue;
				Logger.Warn(e, "[PinMAME] AltSound is not available on this platform.");
			}
			if (result == 1) {
				status = $"Playing {pack}" + (stream != null ? "." : " through Unity.");
				Logger.Info("[PinMAME] AltSound plays {Pack:l} at {Rate} Hz, {Output:l}.", pack, sampleRate, stream != null ? "through the central mixer" : "through Unity");
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

		/// <summary>The pack's volume, 0..1, from the main thread.</summary>
		public float Gain {
			set {
				value = Mathf.Clamp01(value);
				if (value == Volatile.Read(ref _gain) && _gainSet) return;
				_gainSet = true;
				Volatile.Write(ref _gain, value);
				if (_stream != null) _stream.Gain = value;
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
				var block = new ReadOnlySpan<float>((void*)samples, frames * 2);
				if (altSound._stream != null && altSound._stream.Push(block, frames, 2, sampleRate)) return;
				altSound.Write(block, frames);
			}
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
			long written = Volatile.Read(ref _written), read = Volatile.Read(ref _read);
			if (written - read > RingFrames / 2) read = written - RingFrames / 4; // fell behind: keep the latency bounded
			int frames = (int)System.Math.Min(written - read, data.Length / channels);
			float gain = Volatile.Read(ref _gain);
			for (int i = 0; i < frames; ++i) {
				int slot = (int)((read + i) % RingFrames) * 2;
				float left = _ring[slot] * gain, right = _ring[slot + 1] * gain;
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

		private static byte[] Utf8(string text)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty);
			Array.Resize(ref bytes, bytes.Length + 1);
			return bytes;
		}
	}
}
