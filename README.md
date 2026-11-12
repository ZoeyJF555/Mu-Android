# Mu Android

A MU Online client written in C# on .NET 9 and MonoGame, sharing the same code
between desktop and Android.

This is an original project: the logic, structure and code are written here from
scratch, using the publicly documented formats of the client data files
(BMD, OZJ, OZB, OZT, ATT, MAP...).

## Status

Early foundation, work in progress:

- [x] .NET 9 solution with projects split by responsibility.
- [x] BMD model reader (meshes, bones and actions) with support for encrypted BMD v12.
- [x] Case-insensitive data path resolution (Linux and Android).
- [x] MonoGame game loop shared between desktop and Android.
- [ ] LEA decryption for BMD v15.
- [ ] OZJ/OZT texture and OZB world loading.
- [ ] Model rendering with skinning.
- [ ] Networking and login.

## Structure

```
src/
  MuCore/            Pure logic: formats, IO and services (no MonoGame)
    Assets/          AssetStore: client resource lookup
    Formats/Bmd/     BMD format model and reader
    IO/              Bounds-checked binary reading
  MuClient.Shared/   Shared MonoGame game (loop, scenes, UI)
  MuClient/          Desktop entry point (MonoGame DesktopGL)
  MuClient.Android/  Android entry point (net9.0-android)
```

## Requirements

- .NET SDK 9.0
- For Android: `dotnet workload install android`, the Android SDK and JDK 17+

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

To produce the APK:

```bash
dotnet publish src/MuClient.Android/MuClient.Android.csproj -f net9.0-android -c Release
```

## Client data

Game assets (textures, models, maps) are not included in the repository.
`Mu.Core.Assets.AssetStore` takes the path to the data directory and resolves
names with backslashes and case-insensitively, the way the original files
reference them.

## Supported formats

| Format | Purpose | State |
| ------ | ------- | ----- |
| BMD v12 | Models and animations | Fully read |
| BMD v15 | Models and animations (LEA) | Pending |
