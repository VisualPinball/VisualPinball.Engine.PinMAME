# Visual Pinball Engine - PinMAME Gamelogic Engine
*Enables PinMAME to drive VPE*

## Setup

Add this to your Unity project via the package manager. When using a scoped
registry, use `org.visualpinball.engine.pinmame`.

## Usage

This package adds a new gamelogic engine called *PinMAME*. Add the component to a table, select a physical machine and ROM in its inspector, then click **Populate Hardware**. The editor downloads the selected definition from [games.visualpinball.org](https://games.visualpinball.org), caches it under `Library/PinMAME/GameDefinitions`, and fills the table's switch, coil, and lamp managers.

The catalog and cache are editor-only. A player build receives the serialized hardware mappings and machine/ROM identifiers; it does not fetch or contain the source metadata. Display layouts continue to be queried directly from the selected ROM through PinMAME.

Existing scenes and packages that used the retired class-based definitions remain compatible through a small runtime alias resolver. New and repopulated tables serialize numeric PinMAME IDs and populated flipper-wire mappings on the table. Candidate hardware snapshots stay in the editor scene and are omitted from packaged gamelogic data, so no machine-definition classes or catalog records are shipped with the player.

## Developer Setup

This UPM package gets build and published via CI. If you want to use it locally,
you'll need to open and compile the VS solution first, which will copy the
necessary binaries to the Unity package folder.

## License

[MIT](../LICENSE)
