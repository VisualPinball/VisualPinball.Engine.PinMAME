# Visual Pinball Engine - PinMAME Gamelogic Engine

[![UPM Package](https://img.shields.io/npm/v/org.visualpinball.engine.pinmame?label=org.visualpinball.engine.pinmame&registry_uri=https://registry.visualpinball.org&color=%2333cf57&logo=unity&style=flat)](https://registry.visualpinball.org/-/web/detail/org.visualpinball.engine.pinmame)

*Enables PinMAME to drive VPE*


## Structure

This project contains three root folders:

- `VisualPinball.Engine.PinMAME` wraps the [`pinmame-dotnet`](https://github.com/vpinball/pinmame-dotnet) library and produces the managed Unity plugin.
- `VisualPinball.Engine.PinMAME.Unity` is the Unity UPM package that plugs into VPE and implements the gamelogic engine.
- `VisualPinball.Engine.PinMAME.Test` verifies the editor-side conversion from the public machine-definition schema to VPE hardware mappings.

Machine definitions are not compiled into this package. In the Unity Editor, the PinMAME inspector loads the catalog from `https://games.visualpinball.org`, caches it under the project's `Library/PinMAME/GameDefinitions` directory, and populates the table's serialized hardware mappings. Runtime builds contain those mappings and the selected machine/ROM identifiers, but none of the fetched catalog metadata or networking code. CI converts a pinned revision of the complete curated corpus so incompatible definition changes fail before release.

The published site endpoint is preferred to raw GitHub files because it exposes a stable joined index with all ROM aliases, supports CORS and conditional HTTP caching, and requires only one index request plus one detail request for the selected machine. The GitHub repository remains the canonical source from which the site is built.

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
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/android-arm64-v8a/PinMameDotNet.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/android-arm64-v8a/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/android-arm64-v8a/libpinmame.3.7.0.so.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/ios-arm64/PinMameDotNet.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/ios-arm64/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/ios-arm64/libpinmame.3.7.0.a.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/ios-arm64/libz.a.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/linux-x64/PinMameDotNet.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/linux-x64/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/linux-x64/libpinmame.so.3.7.0.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx/PinMameDotNet.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/osx/libpinmame.3.7.0.dylib.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x64/PinMameDotNet.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x64/VisualPinball.Engine.PinMAME.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x64/pinmame.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x86/PinMame.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x86/PinMameDotNet.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.PinMAME.Unity/Plugins/win-x86/VisualPinball.Engine.PinMAME.dll.meta
```

## License

This plugin is licensed under the [MIT license](LICENSE). However
since we link against PinMAME, the [MAME/BSD-3-Clause](https://github.com/vpinball/pinmame/blob/master/LICENSE)
must be honored as well.
