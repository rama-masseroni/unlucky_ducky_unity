# Changelog

Historial generado desde `git log --no-merges`, por lo que no incluye merge commits.
Las entradas estan ordenadas de mas reciente a mas antigua y mantienen el hash corto
del commit para poder rastrear el cambio original.

## 2026-06-11

### Sin commit - Progresion persistente de niveles

- Se agrego guardado local de niveles completados mediante `PlayerPrefs`.
- Solo el primer nivel del catalogo queda desbloqueado en una partida nueva.
- Completar un nivel al tocar la meta desbloquea la siguiente entrada del catalogo, incluso entre mundos.
- El selector mantiene visibles los niveles bloqueados con arte bloqueado y botones desactivados.
- Los sprites tematicos del selector conservan sus colores originales al bloquearse, sin el tinte deshabilitado del `Button`.
- Los niveles 6 a 10 del Mundo 2 ahora comparten la placa amarilla de Construccion y tienen variantes bloqueadas grises.
- Se agrego `Unlucky Ducky/Progress/Reset Local Progress` para pruebas desde el Editor.
- El ultimo nivel catalogado ahora vuelve al menu principal al continuar.
- Se agregaron tests EditMode para desbloqueo lineal, cargas bloqueadas, persistencia idempotente y datos corruptos.

### Sin commit - Paquetes de arte del selector por mundo

- Se agrego `WorldLevelSelectorAssets` para asociar fondos, navegacion y botones de nivel a cada mundo.
- Se configuraron paquetes para Alcantarillas, Construccion, Centro Logistico y Barco/Muelle con el arte existente en `Assets/Sprites/UX`.
- El selector ahora obtiene el paquete desde `LevelDefinition.WorldDefinition` y usa `worldLabel` solo como fallback.
- Los niveles sin variante bloqueada reutilizan su sprite normal con un tinte configurable.
- Se mantuvo la presentacion numerica del prefab como fallback cuando falta arte.
- El menu de pausa ahora usa los sprites disponibles para `Continuar` y `Volver al menu`, sin texto duplicado.
- Se regeneraron los botones del menu de pausa con texto negro opaco y se agregaron variantes para `Opciones`, `Reiniciar nivel` y `Creditos`.
- El boton `Volver al menu` ahora replica el estilo rojo, texto blanco y dimensiones del boton `Salir` del menu principal.
- Los nuevos sprites se asignaron a `Opciones` y `Creditos` del menu principal, y a todos los botones de accion del menu de pausa.
- Los PNG de botones se recortaron al limite exacto del arte visible para evitar que aparezcan aplastados por margenes transparentes.
- Se reparo el `Canvas` corrupto de `Scene_01_03`, restaurando su `GameObject` y las referencias de sus componentes UI.

## 2026-06-09

### Sin commit - UI de niveles basada en prefabs

- Se creo `UI_LevelRoot` con HUD, inventario, pausa, victoria y derrota como subprefabs.
- Se elimino la construccion visual runtime de los managers y paneles de UI de nivel.
- Se migraron `Test_Scene` y los 18 niveles para usar una unica composicion bajo su Canvas.
- Se agrego inyeccion centralizada de `GameStateManager` y `BuildModePlacementController`.
- Se incorporaron prefabs para slots de inventario, overlays e iconos de HUD.
- Se actualizo el bootstrapper, la herramienta de migracion y la cobertura de tests de assets y escenas.

## 2026-06-07

### Sin commit - Camara dinamica de planificacion

- Se agrego un flag `useDynamicPlanningCamera` por `LevelDefinition`.
- Los niveles dinamicos arrancan centrados en el pato con zoom reducido durante `Planning`.
- Se agrego desplazamiento por bordes de pantalla y `WASD`/flechas, bloqueado mientras se arrastran piezas.
- Al ejecutar, la camara anima de vuelta a la vista completa configurada en la escena.
- El inventario se corre hacia adentro solo durante `Planning` en niveles dinamicos.
- Se activo la feature en niveles existentes con camara full `orthographic size >= 7`.

## 2026-06-04

### `5c88751` - Changed sensors and doors to become part of level

- Sensores y puertas dejaron de ser objetos de inventario.
- Se eliminaron las definiciones `Placeable` de sensor, puerta cerrada y puerta abierta.
- Se limpiaron los `InventorySet` afectados para que conserven solo objetos usables.
- Se mantuvieron los prefabs/componentes de sensores y puertas para autorizar niveles con objetos precolocados.
- Se amplio el auto-snap del editor para cubrir sensores y puertas como objetos insertables en el mapa.
- Se agrego cobertura de tests para evitar que sensores/puertas vuelvan a aparecer como placeables de inventario.

### `d4281b1` - Added planning timer, made option click to destroy mandatory game-wide

- Se agrego un limite de tiempo opcional para la fase de planificacion por `LevelDefinition`.
- El contador se descuenta solo durante `Planning` y se detiene al pasar a `Execution`.
- Se agrego derrota por timeout con mensaje especifico en la pantalla de derrota.
- Se mostro el timer en el HUD del nivel con formato de cuenta regresiva.
- Se elimino la opcion por mundo que habilitaba/deshabilitaba el click-to-destroy.
- El pico quedo habilitado globalmente, condicionado solo por inventario y cantidad disponible.

## 2026-06-03

### `6c90882` - Removed unbreakable tilemap

- Se removio el tilemap separado de bloques no rompibles del prefab base de mapa.
- Se actualizaron escenas de los mundos 1, 2, 3 y 4 para ajustarse a la nueva estructura de tilemaps.
- Se consolido la pintura del mapa alrededor de tilemaps funcionales como paredes, rompibles, hazards y caida.

## 2026-06-01

### `234da2a` - changed death text

- Se ajusto el texto mostrado en la pantalla de derrota.

### `43f1f25` - Necessary screens

- Se agregaron pantallas de victoria y derrota.
- Se amplio el HUD del nivel con controles y estados necesarios para gameplay.
- Se reforzo el flujo de pausa, reinicio y navegacion desde UI.
- Se actualizo el catalogo de niveles y se ajustaron definiciones de nivel.
- Se agregaron o actualizaron tests relacionados con HUD, menu y transiciones de estado.

## 2026-05-31

### `035ef81` - More levels

- Se agregaron nuevos niveles del mundo 4.
- Se crearon `InventorySet` y `LevelDefinition` para niveles del mundo 4.
- Se agrego `World_04` y se actualizo la configuracion de escenas del build.
- Se actualizo la escena de prueba y el inventario de test.

### `3bdf853` - New world

- Se agrego el primer nivel del mundo 4.
- Se incorporo visualizacion del area de explosion de la bomba.
- Se ajusto la logica de colocacion y comportamiento de bomba.
- Se agregaron tests para el area de explosion.

### `3e54051` - World 3 redesign

- Se reorganizaron escenas del mundo 1 al esquema `Scene_01_XX`.
- Se agrego el mundo 3 con cinco niveles.
- Se agregaron definiciones de nivel e inventarios con la convencion `LevelDefinition_XX_YY` e `InventorySet_XX_YY`.
- Se actualizo el catalogo principal y la configuracion de escenas del build.
- Se ajustaron prefabs de pato, rata, bomba, sensores, puertas y hazards.
- Se agregaron sprites de pato, rata y pinchos.
- Se incorporo tooling de editor para bootstrap de niveles y snapping.
- Se agregaron tests para placeables, sensores/puertas, destruccion y movimiento.

## 2026-05-29

### `67f42c2` - Menus

- Se agrego la escena de menu principal.
- Se incorporo `MainLevelCatalog` para listar niveles disponibles.
- Se agregaron controladores de menu, navegacion y seleccion de nivel.
- Se agrego un bootstrapper para construir la UI del menu.
- Se ajusto el menu de pausa.
- Se agregaron tests de menu y se incluyo la escena de menu en build settings.

## 2026-05-27

### `e1b760d` - Sensor and doors, v1

- Se agregaron prefabs de sensor de movimiento, puerta cerrada y puerta abierta.
- Se agregaron `SensorController`, `SensorDoorController` e `ISensorReceiver`.
- Se agregaron assets `Placeable` iniciales para sensor y puertas.
- Se ajusto el panel de inventario para soportar estos objetos.
- Se actualizaron escenas y definiciones de nivel para probar sensores/puertas.
- Se agregaron tests de interaccion de sensores y layout del inventario.

### `c48b659` - Level 4 plus sprites

- Se agrego `Scene_02_04` como nuevo nivel del mundo 2.
- Se agrego `Level_09` con inventario y definicion de nivel.
- Se incorporaron iconos para bomba y pico.
- Se ajustaron assets de `Placeable_Bomb` y `Placeable_Pickaxe`.

## 2026-05-26

### `788ad36` - removed unnecesary tile

- Se ajusto `Scene_02_01` para remover o corregir una pieza de tile innecesaria.

### `a86cc63` - Update Scene_02_03.unity

- Se aplico una correccion puntual en `Scene_02_03`.

### `d570b2e` - Falling blocks

- Se agrego soporte para tilemaps destructibles que caen.
- Se agregaron `FallingDestructibleTilemapLayer`, `FallingTileBlock` y eventos de destruccion por batch.
- Se ajustaron bomba, `LevelManager` y movimiento para convivir con bloques que se desprenden.
- Se actualizaron escenas y el prefab base de mapa para incluir estos tilemaps.
- Se agregaron tests de destruccion, movimiento y manejo de tilemaps.

## 2026-05-25

### `21fcc39` - New world + QoL fixes

- Se separaron escenas por carpetas de mundo.
- Se agrego el mundo 2 con sus primeros niveles.
- Se agregaron nuevos `InventorySet`, `LevelDefinition` y `WorldDefinition`.
- Se incorporo una escena de test.
- Se movio `IBreakable` al espacio core del proyecto.
- Se agrego/ajusto tooling de editor para snapping de actores.
- Se mejoraron reglas de muerte, colocacion, bomba y tests de gameplay.

## 2026-05-24

### `03907e7` - Background

- Se agrego el prefab base `Map`.
- Se agrego un prefab de fondo.
- Se actualizaron escenas del primer mundo para usar la nueva estructura visual.
- Se ajusto el prefab del pato para la nueva configuracion de escena.

### `f351cec` - First 5 levels

- Se agregaron los primeros cinco niveles jugables.
- Se crearon inventarios y definiciones por nivel.
- Se reorganizaron assets de nivel y mundo.
- Se actualizaron escenas para conectar progresion, objetos e inventario.

## 2026-05-21

### `f9f03d8` - Click to destroy as Pickaxe, spikes

- Se convirtio el click-to-destroy en una herramienta de inventario representada por el pico.
- Se agrego `Placeable_Pickaxe` y su modo de uso de destruccion durante ejecucion.
- Se agregaron pinchos como hazard tilemap.
- Se agrego `HazardTilemapLayer` y deteccion de muerte por hazards.
- Se actualizaron inventarios, escenas, mundo y tests relacionados.

## 2026-05-17

### `73e7713` - Pause, re-drag and return, reset on kill

- Se agrego HUD de nivel.
- Se agrego menu de pausa y flujo de volver/reiniciar.
- Se permitio re-drag de objetos colocados durante planificacion.
- Se agrego reset del nivel cuando el pato muere.
- Se reorganizaron scripts core en un asmdef propio.
- Se ajustaron controladores de inventario, colocacion, bomba, rata y pato.

### `3bc46dd` - Object list, plan & play, restart

- Se agrego meta (`Goal_Point`) y deteccion de victoria.
- Se agrego `GameStateManager` con fases de planificacion y ejecucion.
- Se agrego runtime de inventario para consumir cantidades por nivel.
- Se agregaron controles para iniciar ejecucion y reiniciar nivel.
- Se agregaron `LevelDefinition`, `WorldDefinition` y assets iniciales de nivel/mundo.
- Se agrego una escena de prueba vacia para validar el flujo.

## 2026-05-15

### `8609545` - Rat

- Se agrego el prefab `Enemy_Rat`.
- Se agrego `EnemyRatController` y asmdef de enemigos.
- Se extrajo movimiento compartido a `GridWalkerController`.
- El pato y la rata pasaron a compartir reglas base de movimiento por grilla.
- Se agrego `Placeable_Rat` al inventario.

## 2026-05-13

### `5fb490b` - Object list, drag & drop

- Se agrego el panel UI de inventario de objetos.
- Se agregaron `PlaceableInventorySet`, entradas de inventario y vistas de slot.
- Se agrego `BuildModePlacementController` para arrastrar y soltar objetos en planificacion.
- Se agrego `Placeable_Bomb` como primer item del inventario.
- Se conecto el inventario con `LevelDefinition` y la escena de muestra.

### `534785b` - Added Bomb and destructible properties

- Se agrego prefab de bomba colocable.
- Se agrego `BombController` y calculo de area de explosion.
- Se agregaron componentes de destruccion para objetos y tilemaps.
- Se agrego la interfaz de rompibles y tests del area de explosion.

## 2026-05-10

### `f34597d` - Functional click to destroy + player w/movement

- Se agregaron assets visuales de plataforma abstracta.
- Se implemento click-to-destroy funcional para bloques.
- Se agrego jugador con movimiento.
- Se amplio la escena de muestra con gameplay basico de desplazamiento y destruccion.

## 2026-04-27

### `b4d447b` - cambios

- Se agregaron reglas/contexto de agentes para el proyecto.
- Se agrego skill local de patrones Unity ECS.
- Se creo el prefab de bloque destructible.
- Se agrego `DestructibleBlock` e interfaz `IBreakable`.
- Se incorporaron herramientas iniciales de click-to-destroy.
- Se agregaron tile palettes locales y se reorganizo la escena de muestra.

## 2026-04-24

### `2bf83b8` - Begun scripting types of tiles

- Se iniciaron scripts y assets para definir tipos de tiles y placeables.
- Se agregaron `LevelDefinition`, `PlaceableDefinition` y `LevelManager`.
- Se agrego un script de bloque solido.
- Se importaron recursos de TextMesh Pro.
- Se expandio `SampleScene` para probar tipos de tiles.

## 2026-04-23

### `ccfed98` - Added 2d assets library

- Se importo la libreria `Free 2D Platform Tileset`.
- Se agregaron sprites, tiles, prefabs, paletas, animaciones y escena demo del paquete.
- Se agrego configuracion de Visual Studio y se actualizo la solucion.

## 2026-03-30

### `03b6d2f` - Changed version

- Se ajusto la version/configuracion del proyecto Unity.
- Se actualizaron paquetes, lockfile y project settings relacionados.
- Se agrego una entrada de escena a build settings.

## 2026-03-29

### `a9ecbda` - Initial Commit

- Se creo el proyecto Unity base.
- Se agregaron configuraciones URP 2D, Input System, escenas y project settings.
- Se agregaron `Packages/manifest.json` y `packages-lock.json`.

### `26218c7` - Initial commit

- Se agregaron archivos base de repositorio: `.gitattributes`, `.gitignore` y `README.md`.
