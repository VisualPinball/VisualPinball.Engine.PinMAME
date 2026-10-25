// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

// Replaces the dirent emulation libaltsound bundles for Windows, which changes the process's current directory and keeps a
// single static handle, so a nested listing ends the outer one. This one keeps its state per DIR and never touches the
// current directory. Paths are narrow (ANSI), as libaltsound passes them.
#pragma once
#ifdef _WIN32

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

struct dirent {
	unsigned int d_namlen;
	char d_name[MAX_PATH];
};

typedef struct vpe_dir {
	HANDLE handle;
	WIN32_FIND_DATAA data;
	int first;
	struct dirent entry;
} DIR;

DIR* opendir(const char* name);
struct dirent* readdir(DIR* dir);
int closedir(DIR* dir);

#endif
