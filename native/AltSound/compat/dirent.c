// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

#ifdef _WIN32
#include "dirent/dirent.h"

#include <stdlib.h>
#include <string.h>

DIR* opendir(const char* name)
{
	if (!name || !*name) return NULL;
	const size_t length = strlen(name);
	if (length + 3 > MAX_PATH) return NULL;
	char pattern[MAX_PATH];
	memcpy(pattern, name, length);
	size_t end = length;
	if (pattern[end - 1] != '/' && pattern[end - 1] != '\\') pattern[end++] = '\\';
	pattern[end++] = '*';
	pattern[end] = '\0';
	DIR* dir = (DIR*)calloc(1, sizeof(DIR));
	if (!dir) return NULL;
	dir->handle = FindFirstFileA(pattern, &dir->data);
	if (dir->handle == INVALID_HANDLE_VALUE) { free(dir); return NULL; }
	dir->first = 1;
	return dir;
}

struct dirent* readdir(DIR* dir)
{
	if (!dir || dir->handle == INVALID_HANDLE_VALUE) return NULL;
	if (dir->first) dir->first = 0;
	else if (!FindNextFileA(dir->handle, &dir->data)) return NULL;
	strncpy(dir->entry.d_name, dir->data.cFileName, MAX_PATH - 1);
	dir->entry.d_name[MAX_PATH - 1] = '\0';
	dir->entry.d_namlen = (unsigned int)strlen(dir->entry.d_name);
	return &dir->entry;
}

int closedir(DIR* dir)
{
	if (!dir) return -1;
	if (dir->handle != INVALID_HANDLE_VALUE) FindClose(dir->handle);
	free(dir);
	return 0;
}
#endif
