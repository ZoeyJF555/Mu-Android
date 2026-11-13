# Mu Android

A **MU Online** client written in **C#** on **.NET 9** and **MonoGame**, sharing
the same game code between desktop and Android.

This is an original, from-scratch implementation: the logic, structure and code
are written here, using only the publicly documented formats of the client data
files (BMD, OZJ, OZB, OZT, ATT, MAP...).

---

## Table of contents

- [Status](#status)
- [Features](#features)
- [Requirements](#requirements)
- [Build and run (desktop)](#build-and-run-desktop)
- [Build for Android](#build-for-android)
- [Testing](#testing)
- [Project layout](#project-layout)
- [Architecture](#architecture)
- [Client data](#client-data)
- [Supported formats](#supported-formats)
- [Roadmap](#roadmap)
- [License](#license)

---

## Status

Early foundation, work in progress.

- [x] .NET 9 solution with projects split by responsibility.
- [x] BMD model reader (meshes, bones and actions) with support for encrypted BMD v12.
- [x] Case-insensitive data path resolution (Linux and Android).
- [x] MonoGame game loop shared between desktop and Android.
- [ ] LEA decryption for BMD v15.
- [ ] OZJ/OZT texture and OZB world loading.
- [ ] Model rendering with skinning.
- [ ] Networking and login.

## Features

- **`MuCore`** — a MonoGame-free library that parses the client's binary
  formats. It is fully decoupled from rendering so it can be tested and reused.
- **BMD reader** — parses models, meshes, vertices, normals, texture
  coordinates, triangles, bones and animation tracks, including the encrypted
  **v12** payload.
- **Bounds-checked binary IO** — every read validates the available length so a
  corrupt file raises a clear exception instead of reading out of range.
- **`AssetStore`** — resolves client data paths the way the original files
  reference them (Windows-style backslashes, case-insensitive) on Linux and
  Android.
- **Shared game** — a single `MuGame` runs on desktop (DesktopGL) and Android.

## Requirements

- **.NET SDK 9.0**.
- For Android: `dotnet workload install android`, the Android SDK, and JDK 17+.

## Build and run (desktop)

```bash
dotnet build src/MuClient/MuClient.csproj -c Release
dotnet run --project src/MuClient
```

## Build for Android

```bash
dotnet workload install android
dotnet build src/MuClient.Android/MuClient.Android.csproj -c Release
```

To produce an APK:

```bash
dotnet publish src/MuClient.Android/MuClient.Android.csproj -f net9.0-android -c Release
```

## Testing

There is **no automated test project yet**. This section documents how to verify
the current functionality and what a test suite should cover; adding a
`tests/MuCore.Tests` project (xUnit or MSTest) is part of the roadmap.

### Manual verification

```bash
# Build every project in the solution
dotnet build MuAndroid.sln -c Release
```

- Desktop: `dotnet run --project src/MuClient` should open a 1280x720 window
  titled "Mu Android" and close on **Esc**.
- Android: install the produced APK on a device/emulator; the same game loop
  should start in landscape.

### What a unit-test suite should cover

The pure logic in `MuCore` is the natural first target because it has no
MonoGame dependency:

- **`BmdReader`** — parse a known-good BMD v12 fixture and assert the model
  name, mesh/bone/action counts and a few vertex/normal values.
- **`BmdPayloadDecoder`** — round-trip the rolling v12 cipher; assert that an
  unsupported version (v15) throws `NotSupportedException`.
- **`BinaryReaderExtensions`** — `ReadFixedString` NUL termination, and that
  `ReadStruct`/`ReadStructArray`/`EnsureRemaining` throw
  `EndOfStreamException`/`InvalidDataException` on truncated input.
- **`AssetStore`** — case-insensitive resolution, backslash normalisation, and
  `FileNotFoundException` for missing resources.

Because `MuCore` is separate from the game projects, these tests can run on any
CI machine without a GPU.

## Project layout

```
src/
  MuCore/            Pure logic: formats, IO and services (no MonoGame)
    Assets/          AssetStore: client resource lookup
    Formats/Bmd/     BMD format model and reader
    IO/              Bounds-checked binary reading
  MuClient.Shared/   Shared MonoGame game (loop, scenes, UI)
  MuClient/          Desktop entry point (MonoGame DesktopGL)
  MuClient.Android/  Android entry point (net9.0-android)
MuAndroid.sln        Solution tying the projects together
```

## Architecture

The solution is split by responsibility so that format parsing never depends on
the renderer.

### `MuCore` — format parsing (`net9.0`, no MonoGame)

- **`Mu.Core.IO.BinaryReaderExtensions`** — helpers for the binary types the
  client uses. `ReadFixedString` reads a NUL-terminated fixed-length name;
  `ReadStruct`/`ReadStructArray` read disk structs with the on-disk packing;
  `EnsureRemaining` validates the number of bytes left. All reads are
  bounds-checked.
- **`Mu.Core.Assets.AssetStore`** — resolves a resource name to a real file
  inside the data directory. Names are normalised (`\` -> `/`) and matched
  case-insensitively, with a lazily built index and a resolution cache.
- **`Mu.Core.Formats.Bmd`**:
  - `BmdModel`, `BmdMesh`, `BmdBone`, `BmdAction`, ... — the decoded model, ready
    for the renderer.
  - `BmdRecords` — the on-disk `[StructLayout(Pack = 4)]` structures whose sizes
    match the original files byte for byte.
  - `BmdReader` — reads a BMD from disk or memory: checks the `BMD` header,
    decrypts the body when needed, then reads meshes, actions and bones.
  - `BmdPayloadDecoder` — decrypts encrypted bodies. v12 uses a rolling XOR +
    subtract scheme with a 16-byte key; v15 (LEA) is not implemented yet.

### `MuClient.Shared` — the shared game

- `MuGame` — a MonoGame `Game` subclass with the loop shared by desktop and
  Android. Scenes, world, objects and UI will grow here.

### Entry points

- `MuClient` (desktop) — `Program.cs` creates and runs `MuGame` via DesktopGL.
- `MuClient.Android` — `MainActivity` hosts `MuGame` in an `AndroidGameActivity`.

## Client data

Game assets (textures, models, maps) are **not** included in the repository.
`Mu.Core.Assets.AssetStore` takes the path to the data directory and resolves
names with backslashes and case-insensitively, the way the original files
reference them.

## Supported formats

| Format | Purpose | State |
| ------ | ------- | ----- |
| BMD v12 | Models and animations | Fully read |
| BMD v15 | Models and animations (LEA) | Pending |

## Roadmap

- [ ] LEA decryption for BMD v15.
- [ ] OZJ/OZT texture and OZB world loading.
- [ ] Model rendering with skinning.
- [ ] Networking and login.
- [ ] A `MuCore` unit-test project wired into CI.

## License

No license file is currently included in this repository.
