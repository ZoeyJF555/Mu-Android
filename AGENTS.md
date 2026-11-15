# Mu-Android

Cliente de MU Online en C#/.NET 9 con MonoGame. El mismo código se ejecuta en
escritorio y en Android; todo lo específico de cada plataforma vive en
`src/MuClient/` y `src/MuClient.Android/`, y lo compartido en
`src/MuClient.Shared/`.

## Estructura

- `src/MuCore/` — lectura de formatos y lógica de juego, sin dependencias de
  MonoGame más allá de tipos matemáticos.
  - `Formats/Bmd/` — modelos y animaciones (`BmdReader`, `BmdAnimator`,
    `PlayerAction`).
  - `Formats/Terrain/` — altura, atributos, mapeo y objetos del mapa.
  - `Formats/Texture/` — decodificación OZJ (JPEG), OZT (TGA) y OZB.
  - `World/` — carga de un mundo completo (`WorldLoader`) y de actores
    animados (`SkinnedModelLoader`, `CharacterLoader`).
  - `Rendering/` — construcción de mallas (`TerrainMeshBuilder`,
    `SkinnedMeshBuilder`).
- `src/MuClient.Shared/` — el juego (`MuGame`), la cámara y los dibujantes.
- `tools/AssetFetcher/` — descarga los recursos publicados por el cliente web.

## Compilar y ejecutar

```bash
dotnet build MuAndroid.sln -c Release
MU_DATA=/ruta/al/Data dotnet run --project src/MuClient -c Release
```

La carpeta de datos se indica con la variable de entorno `MU_DATA`
(`DataLocator`). El mundo inicial se elige con `MU_WORLD`; en el juego, RePág y
AvPág recorren los mundos.

## Los datos del cliente no están en el repositorio

`Data/` está en `.gitignore`. Son recursos con copyright de Webzen y del
operador del servidor: el código de este repositorio es original, pero los
modelos, texturas y sonidos no lo son. Consíguelos por tu cuenta y no los
redistribuyas sin permiso.

## Formatos

- **BMD** — modelo con esqueleto (`Bip01`), mallas y animaciones. Todos los
  monstruos, armas, NPC y el cuerpo del jugador lo usan.
- **OZJ / OZT / OZB** — texturas comprimidas (JPEG, TGA, mapa de altura).
- **ATT** — atributos del terreno, una entrada por casilla.
- **MAP** — qué textura de suelo usa cada casilla.
- **OBJ** — objetos colocados sobre el mapa (tipo, posición, giro).

## Convenciones aprendidas

- Las rutas de recursos son las de Windows y sin prefijo: `World1/...`,
  `Object1/...`, `Monster/...`, `Item/...`. `FileAssetSource` las resuelve sin
  distinguir mayúsculas, así que funciona igual en Linux y Android.
- Los objetos del mapa se llaman `Object<tipo+1>.bmd` en todos los mundos menos
  en Lorencia (mundo 1), que usa nombres propios (`Tree01`, `House03`...). Ver
  `ObjectModels`.
- Un monstruo trae su geometría dentro de su propio fichero BMD: no se busca
  por nombre de hueso, se construye con `SkinnedMeshBuilder`.
- El jugador sí se ensambla por partes (`CharacterAssembler`), porque su
  esqueleto vive en `Player/player.bmd` y el equipo son ficheros aparte.
- Las armas y escudos se cuelgan de los huesos `Bip01 R Hand` / `Bip01 L Hand`.

## Recursos del cliente web

`tools/AssetFetcher` descarga los grupos del índice que publica
`https://play.openmu.com.br/assets-index.json`:

```bash
dotnet run --project tools/AssetFetcher -- --list
dotnet run --project tools/AssetFetcher -- --only map:1,map:10 --out Data
```

Por defecto solo baja los formatos que este cliente sabe leer (BMD, OZJ, OZT,
OZB, ATT, MAP, OBJ) y omite GLB, audio e iconos. El índice publica dos árboles
(`Data/` y `game-assets/`) con los mismos nombres dentro; la herramienta quita
el prefijo para juntarlos en un solo árbol.

Comprobado: los ficheros nativos del sitio web son idénticos a los del cliente
de referencia, así que no aportan nada nuevo en estos formatos. Lo que solo
tiene la web son GLB (monstruos, efectos) y audio, que este cliente no lee.

## Empaquetar el juego en un APK

El APK se compila sin datos, pero entonces el juego avisa de que le faltan. Para
que funcione hay que indicar la carpeta de datos:

```bash
dotnet build src/MuClient.Android/MuClient.Android.csproj -c Release \
  -p:MuDataFolder=/ruta/a/Data
```

Los datos se meten como recursos del paquete y se leen con `AndroidAssetSource`,
que no los trata como ficheros del disco sino como entradas del APK. Por eso el
juego recibe el origen de recursos desde fuera (`MuGame(Func<IAssetSource?>)`):
el escritorio usa `DataLocator` y Android usa el paquete.

Dentro de un APK los recursos se leen de uno en uno y sin poder listarlos como
ficheros, así que primero se recorre el árbol y luego se abre cada uno por su
nombre real. `AssetIndex` hace esa traducción entre el nombre que pide el juego
y el que hay de verdad, ignorando mayúsculas, y es lo que comparten
`FileAssetSource` y `AndroidAssetSource`.

El mundo 1 con el personaje y seis monstruos necesita 258 ficheros y 9 MB. El
resto de mundos se pueden añadir pasando una carpeta más completa.

## Texto en pantalla

El proyecto no usa el empaquetador de contenido de MonoGame, así que no hay
`SpriteFont`. `BitmapFont` define las letras como mapas de bits y
`OverlayRenderer` las pinta con una textura de un píxel. Sirve para enseñar los
avisos de fallo, que en un móvil no se pueden ver de otra forma; también se
escriben en la salida de error, que en Android acaba en el registro del sistema.
