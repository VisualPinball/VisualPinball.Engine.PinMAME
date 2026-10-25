# Visual Pinball Engine - PinMAME Gamelogic Engine
*Enables PinMAME to drive VPE*

## Setup

Add this to your Unity project via the package manager. When using a scoped
registry, use `org.visualpinball.engine.pinmame`.

## Usage

This package adds a new gamelogic engine called *PinMAME*. Add the component to a table, select a physical machine and ROM in its inspector, then click **Populate Hardware**. The editor downloads the selected definition from [games.visualpinball.org](https://games.visualpinball.org), caches it under `Library/PinMAME/GameDefinitions`, and fills the table's switch, coil, and lamp managers.

The catalog and cache are editor-only. A player build receives the serialized hardware mappings and machine/ROM identifiers; it does not fetch or contain the source metadata. Display layouts continue to be queried directly from the selected ROM through PinMAME.

In the Player, this package contributes a **PinMAME** settings section with a global **Volume** slider, also exposed in the **Audio** mixer. The default is 100%; zero mutes playback while the ROM continues running. Both surfaces edit the same plugin-owned value. Gain is applied before the first ROM audio frame and can change during playback, independently of ROM volume and NVRAM. Raw PCM published to other integrations is unaffected; a plugin replacing audio playback owns its output gain. The settings adapter applies only to tables using PinMAME, and cannot be disabled independently of the existing gamelogic engine.

The same section turns on **AltSound**: a pack of recorded music, callouts and effects that the ROM's sound commands trigger instead of its own sound, as in Visual PinMAME. A pack is a folder named after the ROM inside a folder named `altsound`, such as `VPinMAME/altsound/<rom>`. The **AltSound folder** setting points at that `altsound` folder; left empty, the plugin looks next to VPinMAME's ROM folder and in VPE's PinMAME folder. When it is on, **AltSound volume** sets the pack's level relative to the ROM volume, and **ROM sound under AltSound** keeps some of the ROM's own sound for packs that don't replace all of it (off by default, like Visual PinMAME). The pack plays through VPE's central mixer on the ROM bus when that is on, and through Unity otherwise. It is mixed by [libaltsound](https://github.com/vpinball/libaltsound) (BSD-3-Clause), built into `VpeAltSound` from `native/AltSound` for Windows, macOS and Linux.

Existing scenes and packages that used the retired class-based definitions remain compatible through a small runtime alias resolver. New and repopulated tables serialize numeric PinMAME IDs and populated flipper-wire mappings on the table. Candidate hardware snapshots stay in the editor scene and are omitted from packaged gamelogic data, so no machine-definition classes or catalog records are shipped with the player.

### ROM and state folders

Desktop builds use the ROM folder configured by the player or host. If neither sets one, PinMAME uses Visual PinMAME's configured ROM folders when available, otherwise `roms` under VPE's data folder. This fallback also applies to standalone table builds.

NVRAM, configuration and high scores stay in VPE's own shared data folder (`%LOCALAPPDATA%\Visual Pinball Engine\PinMAME` on Windows). Existing game state is copied from Visual PinMAME on the first run only; VPE subsequently maintains its own copy.

## Developer Setup

This UPM package gets build and published via CI. If you want to use it locally,
you'll need to open and compile the VS solution first, which will copy the
necessary binaries to the Unity package folder.

## License

[MIT](../LICENSE)
