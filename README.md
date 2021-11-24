# Visual Pinball Engine - PinMAME Gamelogic Engine

[![UPM Package](https://img.shields.io/npm/v/org.visualpinball.engine.pinmame?label=org.visualpinball.engine.pinmame&registry_uri=https://registry.visualpinball.org&color=%2333cf57&logo=unity&style=flat)](https://registry.visualpinball.org/-/web/detail/org.visualpinball.engine.pinmame)

*Enables PinMAME to drive VPE*


## Structure

This project contains two root folders:

- `VisualPinball.Engine.PinMAME` wraps the [`pinmame-dotnet`](https://github.com/vpinball/pinmame-dotnet)
  library and extends it with some meta data. It's also the project that provides
  the Unity plugins.
- `VisualPinball.Engine.PinMAME.Unity` is the Unity UPM package that plugs into
  VPE and implements the gamelogic engine.

### Unity Package

The goal of this repo is to use it within Unity. In order to do that, open the
Package Manager in Unity, and add `org.visualpinball.engine.pinmame` under
*Add package from git URL*.

The Unity package is build and published to our registry on every merge to master.

## Development Setup

In order to import the package locally instead from our registry, clone and
compile it. This will copy the necessary binaries into the Unity folder. Only
then, import the project into Unity.

Since the Unity folder contains `.meta` files of the binaries, but not the
actual binaries, `.meta` files of uncompiled platforms are cleaned up by Unity.
In order to not accidentally commit those files, we recommend to ignore them:

```bash
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/linux-x64/PinMame.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/linux-x64/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/linux-x64/libpinmame.so.3.4.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx-x64/PinMame.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx-x64/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx-x64/libpinmame.3.4.dylib.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx-arm64/PinMame.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx-arm64/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx-arm64/libpinmame.3.4.dylib.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x86/PinMame.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x86/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x86/libpinmame-3.4.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x64/PinMame.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x64/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x64/libpinmame-3.4.dll.meta
```

## License

This plugin is licensed under the [MIT license](LICENSE). However 
since we link against PinMAME, the [MAME/BSD-3-Clause](https://github.com/vpinball/pinmame/blob/master/LICENSE)
must be honored as well. 
