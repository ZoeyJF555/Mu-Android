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

A working **offline** client: everything runs inside the app, with no server,
no internet and no external files. The data is packaged inside the APK and the
save file is written to the device's private storage.

- [x] .NET 9 solution with projects split by responsibility.
- [x] BMD model reader (meshes, bones and actions) with support for encrypted BMD v12.
- [x] Case-insensitive data path resolution (Linux and Android).
- [x] OZJ/OZT texture and OZB world loading, with baked terrain lighting.
- [x] Model rendering with skinning, plus the character assembled from its parts.
- [x] Game logic (`Mu.Core.Game`): character, bots, monsters, NPCs, chat and a
      single `Simulation` that ties them together.
- [x] Offline account and characters saved on the device (`ProfileStore`).
- [x] Scene flow: login, character select, character create and the game itself.
- [x] HUD with health/mana/experience bars, minimap, chat and touch buttons.
- [x] On-screen keyboard and analog joystick, so it plays on a phone.
- [x] All client data bundled inside the APK.
- [ ] LEA decryption for BMD v15.

## Features

- **`MuCore`** — a MonoGame-free library with the client's binary formats *and*
  the game rules, so both can be tested without a GPU.
- **BMD reader** — parses models, meshes, vertices, normals, texture
  coordinates, triangles, bones and animation tracks, including the encrypted
  **v12** payload.
- **World loading** — terrain height, attributes, mapping, lighting and objects,
  baked into a single ground texture for the whole map.
- **Bounds-checked binary IO** — every read validates the available length so a
  corrupt file raises a clear exception instead of reading out of range.
- **`AssetStore`** — resolves client data paths the way the original files
  reference them (Windows-style backslashes, case-insensitive) on Linux and
  Android.
- **Shared game** — a single `MuGame` runs on desktop (DesktopGL) and Android.
- **100% offline** — no networking code at all; the account, the characters and
  their progress live on the device.

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

```bash
# Unit tests for the pure logic in MuCore (no GPU needed)
dotnet test tests/MuCore.Tests/MuCore.Tests.csproj -c Release
```

The suite covers the character rules (levels, experience, damage), the offline
profile store (account, slots, name checks) and the simulation (bots, monsters,
NPCs, chat). It needs no GPU, so it runs on any CI machine.

### Checking the screens without a screen

The game can open a screen, wait a few seconds, save a screenshot and close
itself. It is how the screens are verified in a headless environment:

```bash
# Scene to open: game (default), login, select or create.
MU_DATA=/path/to/Data MU_PROFILE=/tmp/profile MU_SMOKE=1 \
  MU_SMOKE_SCENE=game MU_SMOKE_WORLD=1 \
  xvfb-run -a dotnet src/MuClient/bin/Release/net9.0/MuClient.dll
```

It writes `comprobacion.png` with the last frame. `MU_SMOKE_SCENE` picks the
screen and `MU_SMOKE_WORLD` the starting map.

### What the tests cover

- **`CharacterTests`** — level ups, experience curve, damage with defense,
  healing and progress loading.
- **`ProfileStoreTests`** — first login creates the account, later logins check
  the password, slots and name validation.
- **`SimulationTests`** — spawning, movement, combat, chat and respawning.

## Project layout

```
src/
  MuCore/            Pure logic: formats, IO, world loading and game rules (no MonoGame)
    Assets/          AssetStore: client resource lookup
    Formats/Bmd/     BMD format model and reader
    Formats/Texture/ OZJ/OZT decoding
    Formats/Terrain/ OZB/ATT/MAP terrain and objects
    IO/              Bounds-checked binary reading
    Rendering/       Camera, mesh building, terrain baking (still no MonoGame)
    World/           Loaders that turn the files into ready-to-draw data
    Game/            Character, bots, monsters, NPCs, chat and Simulation
  MuClient.Shared/   Shared MonoGame game
    Rendering/       Text, world and character drawing, shared GPU resources
    Scenes/          GameHost plus login, select, create and game screens
    Ui/              Theme, input, on-screen keyboard and HUD
  MuClient/          Desktop entry point (MonoGame DesktopGL)
  MuClient.Android/  Android entry point (net9.0-android)
tests/
  MuCore.Tests/      xUnit tests for the MuCore logic (no GPU)
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

- **`MuGame`** — the MonoGame loop shared by desktop and Android. It reads the
  touches, scales them to a virtual 1280×720 canvas and hands each frame to the
  current scene.
- **`Scenes/`** — `GameHost` keeps the device, the profile and the asset source,
  and moves from one scene to another. The scenes are `LoginScene`,
  `CharacterSelectScene`, `CharacterCreateScene` and `GameScene`.
- **`GameScene`** — loads the map, places the player, bots, monsters and NPCs,
  and draws the world with the HUD on top.
- **`Rendering/`** — `TextRenderer` draws all text from one texture atlas;
  `ModelCache` shares the meshes and textures of repeated models (one model for
  the ten bots, six for the monsters) so memory stays low on a phone.
- **`Ui/`** — `Theme`, `InputState`, the on-screen `TextInput` keyboard and the
  `Hud` with the bars, minimap, chat and buttons.

### Game logic (`Mu.Core.Game`)

- **`Character`** — the player's numbers: level, experience, health, mana,
  attack and defense. It is the single source of truth for the player's health.
- **`Bot`, `Monster`, `Npc`** — the other inhabitants, each with its own small
  state machine. Bots hunt monsters and chat; monsters fight back and respawn.
- **`Simulation`** — ties the character, the bots, the monsters, the NPCs and the
  chat together and advances them one frame at a time. It has no graphics, so it
  can be tested on its own.
- **`ProfileStore`** — saves the account and the three character slots to a file
  on the device. There is no server: it is the whole of the "online" part.

### Entry points

- `MuClient` (desktop) — `Program.cs` creates and runs `MuGame` via DesktopGL.
- `MuClient.Android` — `MainActivity` hosts `MuGame` in an `AndroidGameActivity`.

## Client data

Game assets (textures, models, maps) are **not** included in the repository.
`Mu.Core.Assets.AssetStore` takes the path to the data directory and resolves
names with backslashes and case-insensitively, the way the original files
reference them.

## Offline and self-contained

The game is designed to need nothing but itself:

- **Desktop** reads the data from a `Data` folder next to the executable, or from
  the path in the `MU_DATA` environment variable.
- **Android** reads the data from **inside the APK**. Point the build at your
  data folder and it is packaged into the app:

  ```bash
  dotnet publish src/MuClient.Android/MuClient.Android.csproj -f net9.0-android -c Release \
    -p:MuDataFolder=/path/to/Data
  ```

  Without `MuDataFolder` the APK still builds, but the game shows a message
  saying the data is missing instead of a black screen.

- **Saving** — the account and the characters are written to the device's private
  storage (the app folder on Android). That is the only file the game writes, and
  it is what lets a character keep its level between sessions.

There is no networking code anywhere in the project.

## Supported formats

| Format | Purpose | State |
| ------ | ------- | ----- |
| BMD v12 | Models and animations | Fully read |
| BMD v15 | Models and animations (LEA) | Pending |
| OZJ / OZT | Textures | Fully read |
| OZB | Terrain height | Fully read |
| ATT | Terrain attributes | Fully read |
| MAP | Terrain texture mapping | Fully read |
| OBJ | Terrain objects | Fully read |

## Roadmap

- [ ] LEA decryption for BMD v15.
- [ ] Player-vs-player and party play between the local bots.
- [ ] Items, inventory and equipment that change the character's numbers.
- [ ] More maps than the nine currently offered in the options panel.

## License

No license file is currently included in this repository.
