# Mu Android

Cliente de MU Online escrito en C# sobre .NET 9 y MonoGame, con el mismo código
para escritorio y Android.

Proyecto propio: la lógica, la estructura y el código se escriben aquí desde
cero, apoyándose en los formatos públicos de los ficheros de datos del cliente
(BMD, OZJ, OZB, OZT, ATT, MAP...).

## Estado

Base inicial en construcción:

- [x] Solución .NET 9 con proyectos separados por responsabilidad.
- [x] Lector de modelos BMD (mallas, huesos y acciones) con soporte de BMD v12 cifrado.
- [x] Resolución de rutas de datos insensible a mayúsculas (Linux y Android).
- [x] Bucle de juego MonoGame compartido entre escritorio y Android.
- [ ] Cifrado LEA de los BMD v15.
- [ ] Lectura de texturas OZJ/OZT y mundos OZB.
- [ ] Renderizado de modelos con skinning.
- [ ] Red y login.

## Estructura

```
src/
  MuCore/            Lógica pura: formatos, IO y servicios (sin MonoGame)
    Assets/          AssetStore: localización de recursos del cliente
    Formats/Bmd/     Modelo y lector del formato BMD
    IO/              Lectura binaria con comprobación de límites
  MuClient.Shared/   Juego MonoGame compartido (bucle, escenas, UI)
  MuClient/          Entrada de escritorio (MonoGame DesktopGL)
  MuClient.Android/  Entrada Android (net9.0-android)
```

## Requisitos

- .NET SDK 9.0
- Para Android: `dotnet workload install android`, Android SDK y JDK 17+

## Compilar y ejecutar (escritorio)

```bash
dotnet build src/MuClient/MuClient.csproj -c Release
dotnet run --project src/MuClient
```

## Compilar para Android

```bash
dotnet workload install android
dotnet build src/MuClient.Android/MuClient.Android.csproj -c Release
```

Para generar el APK:

```bash
dotnet publish src/MuClient.Android/MuClient.Android.csproj -f net9.0-android -c Release
```

## Datos del cliente

Los recursos del juego (texturas, modelos, mapas) no se incluyen en el
repositorio. `Mu.Core.Assets.AssetStore` recibe la ruta del directorio de datos
y resuelve los nombres con barras invertidas e insensible a mayúsculas, como los
usan los ficheros originales.

## Formatos soportados

| Formato | Uso | Estado |
| ------- | --- | ------ |
| BMD v12 | Modelos y animaciones | Lectura completa |
| BMD v15 | Modelos y animaciones (LEA) | Pendiente |
