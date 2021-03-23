# Visual Pinball Engine - PinMAME Gamelogic Engine
*Enables PinMAME to drive VPE*

## Setup

Add this to your Unity project via the package manager. When using a scoped 
registry, use `org.visualpinball.engine.pinmame`.

## Usage

This package adds a new Gamelogic Engine called *PinMAME*. 
Just add the component to your table and wire up the switches, coils, lights
and DMD.

## Developer Setup

This UPM package gets build and published via CI. If you want to use it locally,
you'll need to open and compile the VS solution first, which will copy the 
necessary binaries to the Unity package folder.

## License

[MIT](../LICENSE)
