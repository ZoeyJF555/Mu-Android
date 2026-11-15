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
    `SkinnedMeshBuilder`) y la `Camera`.
  - `Game/` — las reglas sin gráficos: `Character`, `Bot`, `Monster`, `Npc`,
    `ChatLog` y `Simulation`, que las une. El perfil offline vive en
    `ProfileStore` (cuenta, contraseña y tres espacios).
- `src/MuClient.Shared/` — el juego compartido: `MuGame` (bucle, toques y lienzo
  virtual), `Scenes/` (una pantalla por fichero: `LoginScene`,
  `CharacterSelectScene`, `CharacterCreateScene`, `GameScene`, unidas por
  `GameHost`), `Ui/` (`Theme`, `InputState`, `TextInput`, `Hud`) y `Rendering/`
  (texto, mundo y personajes, con `ModelCache` compartiendo mallas).
- `tests/MuCore.Tests/` — pruebas de xUnit de la lógica pura, sin GPU.
- `tools/AssetFetcher/` — descarga los recursos publicados por el cliente web.

## Compilar y ejecutar

```bash
dotnet build MuAndroid.sln -c Release
MU_DATA=/ruta/al/Data dotnet run --project src/MuClient -c Release
```

La carpeta de datos se indica con la variable de entorno `MU_DATA`
(`DataLocator`). Dentro del juego, el botón de opciones (arriba a la derecha)
abre la lista de mundos; se empieza en Lorencia.

Para comprobar una pantalla sin pantalla física (útil en CI o por SSH), el modo
de comprobación abre una pantalla, guarda `comprobacion.png` y se cierra solo:

```bash
MU_DATA=/ruta/al/Data MU_PROFILE=/tmp/perfil MU_SMOKE=1 \
  MU_SMOKE_SCENE=game MU_SMOKE_WORLD=1 \
  xvfb-run -a dotnet src/MuClient/bin/Release/net9.0/MuClient.dll
```

`MU_SMOKE_SCENE` admite `game` (por defecto), `login`, `select` y `create`.

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
- La cámara y los actores siguen al cliente original `bhrama-br/muonline-android`:
  distancia 1700 y campo de visión 35 grados (`Camera`), y los personajes se
  apoyan sobre el suelo con `TerrainHeightSampler`, que interpola la altura de
  las cuatro casillas que rodean el punto. Sin eso quedan hundidos, porque el
  terreno de Lorencia no está a cero en el punto de partida (está a 1,65).

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

Los monstruos de la web están **solo en GLB**: su grupo `monsters` no trae ni un
solo BMD. Por eso los monstruos de este cliente salen de otra copia del cliente,
no de la web. Lo que sí publica la web en formato nativo es el terreno y los
objetos de cada mundo, y por eso sirve para completar mapas.

El mundo 19 (Chaos Castle) es el único que la web trae a medias: su
`TerrainHeight.OZB` son 66.620 bytes **todos a cero**, tanto en la web como en la
copia local. No es un fichero que se pueda leer, así que `TerrainHeightReader`
devuelve un suelo llano en vez de fallar y el mundo se carga igual (los demás
ficheros del mundo sí son válidos).

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
`FileAssetSource` y `AndroidAssetSource`. `AssetTree` recorre ese árbol; da por
carpeta solo lo que tiene hijos, porque Android puede responder con una lista
vacía al listar un fichero en vez de dar un error.

Al cambiar la carpeta de datos hay que borrar `obj/` y `bin/` antes de
recompilar. Si no, el empaquetado reutiliza el paquete anterior y el APK sale
con los datos viejos, sin avisar.

El mundo 1 con el personaje y seis monstruos necesita 258 ficheros y 9 MB. Los
46 mundos con sus objetos, personaje y monstruos necesitan 5.194 ficheros y
249 MB.

## Texto en pantalla

El proyecto no usa el empaquetador de contenido de MonoGame, así que no hay
`SpriteFont`. `BitmapFont` define las letras como mapas de bits (solo mayúsculas,
dígitos y signos) y `TextRenderer` las pinta con una textura de un píxel, con
`BitmapFont.Normalize` pasando el texto a mayúsculas. El HUD se dibuja sin prueba
de profundidad para que quede siempre encima del mundo. Los avisos de fallo
también se escriben en la salida de error, que en Android acaba en el registro
del sistema.

## Pruebas

```bash
dotnet test tests/MuCore.Tests/MuCore.Tests.csproj -c Release
```

Cubren la lógica pura (`Character`, `ProfileStore`, `Simulation`) y no necesitan
GPU. En este entorno el SDK está en `~/.dotnet`, así que hay que añadirlo al
`PATH` (`export PATH="$HOME/.dotnet:$PATH"`). Para compilar Android hacen falta
además `ANDROID_HOME` y `JAVA_HOME`; el aviso sobre el JDK es inofensivo.
