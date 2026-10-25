# Builds VpeAltSound, a C interface to libaltsound (BSD-3-Clause, https://github.com/vpinball/libaltsound) for the PinMAME
# plugin. libaltsound's own API takes std::string, which can't be called from C#, so its sources are compiled into this
# library together with a small C shim. Only miniaudio's null backend is used: the mixed audio goes to the host through a
# callback, not to a device. Fixes on top of the pinned sources: patches/fixes.patch joins the sample paths of folder-based
# (PinSound-style) packs and makes AltSoundInit fail when the processor couldn't load the samples, and compat/dirent
# replaces the Windows directory listing libaltsound bundles, which changes the process's current directory and can't nest
# listings.
#
#   cmake -S native/AltSound -B build/altsound -A x64
#   cmake --build build/altsound --config Release
#
# Pass -DLIBALTSOUND_DIR=<path> to build from a local checkout instead of the pinned commit (the patch is not applied then).
cmake_minimum_required(VERSION 3.25)
project(VpeAltSound C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 99)
set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_C_VISIBILITY_PRESET hidden)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

set(LIBALTSOUND_DIR "" CACHE PATH "Local libaltsound checkout; empty fetches the pinned commit")
if(LIBALTSOUND_DIR)
	set(altsound_SOURCE_DIR "${LIBALTSOUND_DIR}")
else()
	include(FetchContent)
	FetchContent_Declare(altsound
		GIT_REPOSITORY https://github.com/vpinball/libaltsound.git
		GIT_TAG ce11780064a75fe23059fd3b61847ee18d477931
		PATCH_COMMAND git apply --whitespace=nowarn "${CMAKE_CURRENT_SOURCE_DIR}/patches/fixes.patch"
		UPDATE_DISCONNECTED ON)
	FetchContent_GetProperties(altsound)
	if(NOT altsound_POPULATED)
		FetchContent_Populate(altsound) # sources only: libaltsound's own project sets platform variables we don't want
	endif()
endif()

set(A "${altsound_SOURCE_DIR}")
set(ALTSOUND_SOURCES
	${A}/src/altsound_data.cpp
	${A}/src/gsound_csv_parser.cpp
	${A}/src/altsound_ini_processor.cpp
	${A}/src/altsound_logger.cpp
	${A}/src/altsound_processor_base.cpp
	${A}/src/altsound_processor.cpp
	${A}/src/altsound_file_parser.cpp
	${A}/src/altsound_csv_parser.cpp
	${A}/src/gsound_processor.cpp
	${A}/src/altsound.cpp
	${A}/src/miniaudio_private.c
	${A}/src/miniaudio_bass_compat.cpp)
if(WIN32)
	list(APPEND ALTSOUND_SOURCES compat/dirent.c)
endif()

add_library(VpeAltSound SHARED VpeAltSound.cpp ${ALTSOUND_SOURCES})
target_include_directories(VpeAltSound PRIVATE compat ${A}/src ${A}/third-party/include) # compat first: its dirent wins
if(MSVC)
	target_compile_definitions(VpeAltSound PRIVATE _CRT_SECURE_NO_WARNINGS)
	target_compile_options(VpeAltSound PRIVATE /utf-8 /MP)
	set_property(TARGET VpeAltSound PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>") # no VC++ redistributable needed
elseif(UNIX AND NOT APPLE)
	find_package(Threads REQUIRED)
	target_link_libraries(VpeAltSound PRIVATE Threads::Threads ${CMAKE_DL_LIBS} m)
endif()
if(APPLE)
	set_target_properties(VpeAltSound PROPERTIES PREFIX "lib" SUFFIX ".dylib")
endif()
