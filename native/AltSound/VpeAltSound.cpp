// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

// C interface to libaltsound for the PinMAME plugin. libaltsound keeps one global instance, renders on miniaudio's own
// realtime-paced thread and hands each mixed block to a callback. Commands arrive on the emulation thread while the host
// starts and stops the instance on its main thread, so every entry point runs under one lock, and commands outside a
// running instance are dropped (libaltsound itself dereferences its processor unchecked). No C++ exception crosses the
// boundary: a pack that makes libaltsound throw fails to start, or loses the one command.

#include "altsound.h"
#include "altsound_processor_base.hpp"

#include <cmath>
#include <mutex>
#include <string>

#if defined(_WIN32)
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#define VPE_ALTSOUND_API extern "C" __declspec(dllexport)
#else
#define VPE_ALTSOUND_API extern "C" __attribute__((visibility("default")))
#endif

typedef void (*VpeAltSoundCallback)(const float* samples, int frames, int sampleRate, int channels, void* userData);

extern std::mutex io_mutex; // libaltsound's lock for its channel table, which its audio thread changes when samples end
extern AltsoundProcessorBase* g_pProcessor;

namespace {
std::mutex gate;
bool running = false;
bool followGameVolume = true; // libaltsound's default; the host turns it off
float gameVolume = 1.0f; // the game's last volume command, kept while the pack doesn't follow it
// The level the pack plays at while it doesn't follow the game: the float just under 1 (inaudibly lower), which no volume
// command produces, so every command shows as a change, a command to full volume included.
const float ownLevel = std::nextafter(1.0f, 0.0f);
VpeAltSoundCallback hostCallback = nullptr;
void* hostUserData = nullptr;

void Forward(const float* samples, size_t frames, uint32_t sampleRate, uint32_t channels, void*)
{
	// The host's callback is set before the engine starts and cleared after it stopped, so no lock is needed here.
	if (hostCallback && samples && frames)
		hostCallback(samples, static_cast<int>(frames), static_cast<int>(sampleRate), static_cast<int>(channels), hostUserData);
}

// libaltsound opens files with narrow paths, which Windows reads in the ANSI code page.
std::string NativePath(const char* utf8)
{
	if (!utf8) return {};
#if defined(_WIN32)
	const int wide = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, utf8, -1, nullptr, 0);
	if (wide <= 0) return {};
	std::wstring w(static_cast<size_t>(wide), L'\0');
	MultiByteToWideChar(CP_UTF8, 0, utf8, -1, w.data(), wide);
	BOOL lossy = FALSE;
	const int narrow = WideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, w.c_str(), -1, nullptr, 0, nullptr, &lossy);
	if (narrow <= 0 || lossy) return {};
	std::string n(static_cast<size_t>(narrow), '\0');
	WideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, w.c_str(), -1, n.data(), narrow, nullptr, nullptr);
	n.resize(static_cast<size_t>(narrow) - 1);
	return n;
#else
	return utf8;
#endif
}
}

VPE_ALTSOUND_API int VpeAltSoundVersion() { return 2; }

// Starts AltSound for a game whose samples are in <basePath>/altsound/<gameName>. Paths are UTF-8; logFolder may be empty.
// The callback receives float blocks on libaltsound's audio thread until VpeAltSoundShutdown returns. Returns 1 on success,
// 0 when the folder doesn't hold a usable pack, -1 when a path can't be represented, -2 when an instance already runs.
VPE_ALTSOUND_API int VpeAltSoundInit(const char* basePath, const char* gameName, int sampleRate, int bufferFrames,
	VpeAltSoundCallback callback, void* userData, const char* logFolder)
{
	std::lock_guard<std::mutex> lock(gate);
	if (running) return -2;
	if (!basePath || !*basePath || !gameName || !*gameName || !callback || sampleRate < 8000 || bufferFrames <= 0) return 0;
	const std::string base = NativePath(basePath), log = NativePath(logFolder);
	if (base.empty() || (logFolder && *logFolder && log.empty())) return -1;
	hostCallback = callback;
	hostUserData = userData;
	try {
		AltSoundSetLogger(log, ALTSOUND_LOG_LEVEL_ERROR, false);
		if (AltSoundInit(base, gameName, static_cast<uint32_t>(sampleRate), 2, static_cast<uint32_t>(bufferFrames))) {
			AltSoundSetAudioCallback(Forward, nullptr);
			// libaltsound always decodes the game's volume commands, so following can be turned on and off at any time
			if (g_pProcessor) g_pProcessor->romControlsVol(true);
			followGameVolume = true;
			gameVolume = 1.0f;
			running = true;
			return 1;
		}
	} catch (...) {}
	try { AltSoundShutdown(); } catch (...) {} // frees what a partial init left behind
	hostCallback = nullptr;
	hostUserData = nullptr;
	return 0;
}

// The ROM's hardware generation (PinMAME's value, which libaltsound shares), so commands are decoded the way that board sends them.
VPE_ALTSOUND_API void VpeAltSoundSetHardwareGen(unsigned long long hardwareGen)
{
	std::lock_guard<std::mutex> lock(gate);
	AltSoundSetHardwareGen(static_cast<ALTSOUND_HARDWARE_GEN>(hardwareGen));
}

// A sound command as PinMAME logs it. Returns 1 when it was handled.
VPE_ALTSOUND_API int VpeAltSoundCommand(int board, int command)
{
	std::lock_guard<std::mutex> lock(gate);
	if (!running) return 0;
	try {
		const bool handled = AltSoundProcessBoardCommand(static_cast<unsigned int>(board), static_cast<unsigned int>(command), 0);
		if (!followGameVolume && g_pProcessor && AltsoundProcessorBase::getGlobalVol() != ownLevel) {
			// a volume command: remember it, and keep the pack at its own level
			gameVolume = AltsoundProcessorBase::getGlobalVol();
			std::lock_guard<std::mutex> streams(io_mutex);
			g_pProcessor->setGlobalVol(ownLevel);
		}
		return handled ? 1 : 0;
	}
	catch (...) { return 0; }
}

// Whether the game's own volume commands scale the pack, as in Visual PinMAME (where the pack's altsound.ini decides). Off
// plays the pack at its own level, so the host's volume controls alone set it; the game's volume is still tracked, so
// turning it back on applies the game's current volume right away.
VPE_ALTSOUND_API void VpeAltSoundFollowGameVolume(int follow)
{
	std::lock_guard<std::mutex> lock(gate);
	if (!running || !g_pProcessor || followGameVolume == (follow != 0)) return;
	std::lock_guard<std::mutex> streams(io_mutex); // setGlobalVol walks the channel table
	try {
		if (follow) g_pProcessor->setGlobalVol(gameVolume);
		else {
			gameVolume = AltsoundProcessorBase::getGlobalVol();
			g_pProcessor->setGlobalVol(ownLevel);
		}
		followGameVolume = follow != 0;
	} catch (...) {}
}

VPE_ALTSOUND_API void VpeAltSoundPause(int paused)
{
	std::lock_guard<std::mutex> lock(gate);
	if (!running) return;
	// AltSoundPause walks the channel table without libaltsound's own lock, which commands and ending samples take
	std::lock_guard<std::mutex> streams(io_mutex);
	try { AltSoundPause(paused != 0); } catch (...) {}
}

// Stops the audio thread and frees the samples. The callback is not called anymore once this returns.
VPE_ALTSOUND_API void VpeAltSoundShutdown()
{
	std::lock_guard<std::mutex> lock(gate);
	if (!running) return;
	running = false;
	try { AltSoundShutdown(); } catch (...) {}
	hostCallback = nullptr;
	hostUserData = nullptr;
}
