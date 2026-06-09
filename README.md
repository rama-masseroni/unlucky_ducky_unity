# Unlucky Ducky - Guia practica del proyecto

Unity repo para el videojuego **Unlucky Ducky**, un puzzle 2D en grilla donde el jugador prepara el nivel en fase de planificacion y luego ejecuta una simulacion automatica.

Este README esta pensado para editar niveles sin tener que releer todo el codigo: objetos importantes, variables editables, tilemaps necesarios y assets que conviene usar.

## Loop de juego

Cada nivel tiene dos fases:

- `Planning`: el jugador coloca objetos disponibles desde el inventario. Los actores no se mueven y los sensores no se activan.
- `Execution`: el jugador presiona `PROBAR NIVEL`; el pato, ratas, bombas, sensores, puertas, peligros y destruccion empiezan a resolver la simulacion.

El jugador gana cuando el pato llega a `Goal_Point`. Pierde si el pato muere por peligro/enemigo/bomba, o si se agota la cuenta regresiva de planificacion cuando el nivel tiene limite.

## Camara dinamica de planificacion

Los niveles grandes pueden ocultar parte del mapa durante `Planning` para que el jugador no vea la ruta completa desde el inicio. Activa `useDynamicPlanningCamera` en el `LevelDefinition` del nivel.

Cuando esta activo:

- La camara guarda la posicion y el `orthographic size` configurados en la escena como vista completa.
- El nivel arranca con zoom al 60% de esa vista, centrado en `Player_Duck`.
- El jugador mueve la camara llevando el mouse a los bordes de pantalla o usando `WASD`/flechas.
- Durante un drag o movimiento de piezas, la camara no se desplaza.
- El inventario se mueve 64 px hacia adentro solo durante `Planning`, para no quedar sobre la franja de scroll.
- Al presionar `PROBAR NIVEL`, el inventario vuelve a su posicion normal y la camara hace zoom out a la vista completa.

Usa este flag solo en niveles donde la vista completa revela demasiada informacion. Los niveles chicos deberian conservar la camara fija.

## Escenas y datos por nivel

Las escenas siguen la convencion:

```text
Assets/Scenes/World N/Scene_NN_MM.unity
```

Cada escena debe tener un `GameStateManager` con un `LevelDefinition`.

Los datos por nivel viven en:

```text
Assets/ScriptableObjects/Level definitions/LevelDefinition_NN_MM.asset
Assets/ScriptableObjects/InventorySets/InventorySet_NN_MM.asset
```

Campos importantes de `LevelDefinition`:

- `levelId`: identificador del nivel. Ejemplo: `Level_04_03`.
- `levelName`: nombre visible/friendly del nivel.
- `nextSceneName`: escena a cargar al continuar despues de ganar.
- `worldDefinition`: mundo al que pertenece.
- `placeableInventorySet`: inventario del nivel.
- `planningTimeLimitSeconds`: cuenta regresiva de planificacion. `0` significa sin limite.
- `useDynamicPlanningCamera`: activa la camara dinamica durante `Planning` para niveles grandes.

Campos importantes de `InventorySet`:

- `entries`: lista de objetos disponibles.
- `definition`: `PlaceableDefinition` del objeto.
- `amount`: cantidad disponible en ese nivel.

Actualmente los objetos de inventario son:

- `Placeable_Rat`
- `Placeable_Bomb`
- `Placeable_Pickaxe`

Sensores y puertas ya no son objetos de inventario. Se colocan directamente en la escena.

## Objetos principales

### Pato

Prefab:

```text
Assets/Prefabs/Player/Player_Duck.prefab
```

Componentes importantes:

- `PlayerDuckController`
- `GridWalkerController`
- `DuckMovementRules`

Variables editables principales:

- `walkSpeed`: velocidad horizontal.
- `fallSpeed`: velocidad de caida.
- `slopeSpeedMultiplier`: multiplicador en pendientes.
- `initialDirection`: direccion inicial. `1` derecha, `-1` izquierda.
- `groundTilemaps`: tilemaps que cuentan como piso.
- `obstacleTilemaps`: tilemaps que cuentan como obstaculo frontal.
- `hazardContactProbeDistance`: distancia de chequeo contra peligros.
- `logBlocksTraveledPerSecond`: activa logs de velocidad del pato.
- `movementLogIntervalSeconds`: intervalo de logs.
- `movementLogReferenceGrid`: grilla usada para convertir unidades a bloques.

El pato se mueve solo en `Execution`. En `Planning` no muere por hazards ni se mueve.

### Meta

Prefab:

```text
Assets/Prefabs/Goal/Goal_Point.prefab
```

Componente:

- `GoalPointController`

La meta solo completa el nivel en `Execution`. Al ganar usa `LevelDefinition.nextSceneName`.

### Rata

Prefab:

```text
Assets/Prefabs/Enemies/Enemy_Rat.prefab
```

Componentes importantes:

- `EnemyRatController`
- `GridWalkerController`

Variables editables:

- `killsPlayerOnContact`: si esta activo, mata al pato al tocarlo.
- Variables de `GridWalkerController`: velocidad, direccion inicial, piso y obstaculos.

La rata puede activar sensores porque tambien usa `GridWalkerController`.

### Bomba

Prefab:

```text
Assets/Prefabs/Placeables/Placeable_Bomb.prefab
```

Componente:

- `BombController`

Variables editables:

- `explosionDelaySeconds`: segundos desde que empieza `Execution` hasta explotar.
- `explosionRadiusInCells`: radio en celdas. `1` significa area 3x3.
- `referenceTilemap`: tilemap de referencia para convertir celdas.
- `destructibleTilemaps`: tilemaps que la bomba puede destruir. Si no se setea, busca `DestructibleTilemapLayer`.
- `destructibleObjectMask`: objetos rompibles que puede afectar.
- `killsPlayerInExplosionArea`: si mata al pato dentro del area.
- `playerKillMask`: capas consideradas para matar al jugador.
- `destroyBombAfterExplosion`: si destruye la bomba tras explotar.
- `areaVisualizer`: visual del area de explosion.

La bomba se coloca desde inventario en `Planning` y explota en `Execution`.

### Pico

Asset:

```text
Assets/ScriptableObjects/Items/Placeable_Pickaxe.asset
```

No tiene prefab. Es una herramienta de ejecucion:

- `useMode = ExecutionClickToDestroyTile`
- Si el `InventorySet` del nivel incluye `Pico` con `amount > 0`, durante `Execution` el jugador puede hacer click en un tile destruible para destruirlo.
- Cada tile destruido consume una unidad.
- No hay que seleccionarlo desde el inventario.

### Sensores

Prefab:

```text
Assets/Prefabs/Placeables/Sensor_Movement.prefab
```

Componente:

- `SensorController`

Variables editables:

- `connectionId`: grupo de conexion. Ejemplo: `A`, `B`, `C`.
- `connectedReceivers`: puertas/receptores asignados manualmente.
- `autoDiscoverReceivers`: si esta activo, busca automaticamente receptores con el mismo ID.

Los sensores solo se activan en `Execution`, y solo cuando los pisa un objeto con `GridWalkerController` como el pato o una rata.

### Puertas

Prefabs:

```text
Assets/Prefabs/Placeables/Sensor_Door.prefab
Assets/Prefabs/Placeables/Sensor_Door_Open.prefab
```

Componente:

- `SensorDoorController`

Variables editables:

- `sensorConnectionId`: ID que debe coincidir con el sensor.
- `startsOpen`: si la puerta arranca abierta.
- `blockingCollider`: collider que bloquea cuando la puerta esta cerrada.
- `doorRenderer`: sprite renderer de la puerta.
- `closedSprite`: sprite cerrada.
- `openSprite`: sprite abierta.

Vinculacion:

- Sensor con `connectionId = A` activa todas las puertas con `sensorConnectionId = A`.
- Sensor con `connectionId = B` activa todas las puertas con `sensorConnectionId = B`.
- Se pueden tener muchos sensores y muchas puertas con el mismo ID.
- La puerta hace toggle: si esta cerrada se abre; si esta abierta se cierra.

### Bloque destructible individual

Prefab:

```text
Assets/Prefabs/Destructuble_block.prefab
```

Componente:

- `DestructibleBlock`

Se puede destruir mediante sistemas que llamen a `IBreakable`, como la bomba.

## Auto-snap en el editor

`Assets/Scripts/Editor/LevelActorGridSnapper.cs` hace snap al centro de la celda al mover actores de nivel en el editor.

Actualmente aplica a objetos con:

- `PlayerDuckController`
- `GoalPointController`
- `EnemyRatController`
- `BombController`
- `SensorController`
- `SensorDoorController`
- `DestructibleBlock`
- `PlacedPlaceableInstance`

No aplica a UI, tilemaps ni hijos visuales.

## Tilemaps necesarios

Los niveles deben tener un `Grid` con tilemaps separados por responsabilidad. El bootstrapper/editor crea o espera nombres similares a estos:

```text
Grid
  Walls Tilemap
  Breakable Tilemap
  Hazard Tilemap
```

Tambien puede haber tilemaps extra segun el nivel:

```text
Falling destructible tilemap
Spikes tilemap
Breakable floor grid
Walls grid
```

### Walls Tilemap

Uso:

- Limites y paredes del nivel.
- Bloquea movimiento y colocacion.
- Sirve como referencia visual y fisica para cerrar el espacio jugable.

Componentes esperados:

- `Tilemap`
- `TilemapRenderer`
- `TilemapCollider2D`

Como pintarlo:

- Usar tiles solidos de la paleta principal.
- Pintar el contorno del nivel cerrado.
- No mezclar hazards o bloques destruibles aca.

### Breakable Tilemap

Uso:

- Bloques destruibles por bomba o pico.

Componentes esperados:

- `Tilemap`
- `TilemapRenderer`
- `TilemapCollider2D`
- `DestructibleTilemapLayer`

Como pintarlo:

- Usar tiles visualmente rompibles.
- Si la bomba debe destruirlos, asegurarse de que el tilemap tenga `DestructibleTilemapLayer`.
- Si el pico debe destruirlos, el `LevelManager` debe referenciar el tilemap correcto.

### Hazard Tilemap / Spikes tilemap

Uso:

- Pinchos u otros peligros que matan al pato en `Execution`.

Componentes esperados:

- `Tilemap`
- `TilemapRenderer`
- `TilemapCollider2D`
- `HazardTilemapLayer`

Tile recomendado:

```text
Assets/Free 2D Platform Tileset/Demo/Palettes/Tileset/SpikeTile.asset
```

Regla:

- En `Planning`, el pato no muere al tocar hazards.
- En `Execution`, `PlayerDuckController` consulta `HazardTilemapLayer.ActiveLayers` y mata al pato si sus probes tocan un tile de hazard.

### Falling destructible tilemap

Uso:

- Bloques destruibles que pueden caer cuando pierden soporte.

Componentes esperados:

- `Tilemap`
- `TilemapRenderer`
- `TilemapCollider2D`
- `DestructibleTilemapLayer`
- `FallingDestructibleTilemapLayer`

Variables editables:

- `supportTilemaps`: tilemaps que cuentan como soporte.
- `supportObjectMask`: objetos que cuentan como soporte.
- `gravityScale`: gravedad del bloque al caer.
- `freezeRotation`: evita rotacion durante la caida.
- `fallingBlockPrefab`: prefab usado para convertir tiles a bloques fisicos.
- `runtimeRoot`: contenedor runtime para bloques que caen.
- `supportObjectBoxInset`: ajuste del overlap de soporte.

## Paletas y tiles

Paletas principales:

```text
Assets/Free 2D Platform Tileset/Demo/Palettes/Platform.prefab
Assets/Free 2D Platform Tileset/Demo/Palettes/Platform Sets.prefab
```

Tiles de esa paleta:

```text
Assets/Free 2D Platform Tileset/Demo/Palettes/Tileset/
```

Incluye tiles como:

- `Ground_01` a `Ground_10`
- `Water_01` a `Water_03`
- `Bridge_01` a `Bridge_05`
- `Castle_*`
- `House_*`
- `SpikeTile`

Paleta/tiles legado:

```text
Assets/Tile sets/New Tile Palette.prefab
Assets/Tile sets/House_01.asset
```

Usar la paleta principal de `Free 2D Platform Tileset` para nuevos niveles salvo que el nivel ya este construido con assets legado.

## Como pintar un nivel

1. Abrir la escena del nivel.
2. Verificar que exista `Grid`.
3. Pintar paredes/limites en `Walls Tilemap`.
4. Pintar bloques rompibles en `Breakable Tilemap`.
5. Pintar pinchos/peligros en `Hazard Tilemap` usando `SpikeTile`.
6. Si hay bloques que deben caer, usar un tilemap separado con `FallingDestructibleTilemapLayer`.
7. Arrastrar actores de escena:
   - `Player_Duck`
   - `Goal_Point`
   - `Sensor_Movement`
   - `Sensor_Door` o `Sensor_Door_Open`
8. Configurar `LevelDefinition`.
9. Configurar `InventorySet` con los objetos disponibles.
10. Probar en `Play`.

## Objetos de inventario

El panel de inventario lee el `PlaceableInventorySet` del `LevelDefinition`.

`PlaceableDefinition` tiene:

- `id`
- `displayName`
- `prefab`
- `icon`
- `useMode`

Modos:

- `DragToPlace`: se arrastra al mapa en `Planning`.
- `ExecutionClickToDestroyTile`: herramienta activa durante `Execution`.

Reglas actuales:

- `Rata` y `Bomba` son `DragToPlace`.
- `Pico` es `ExecutionClickToDestroyTile`.
- Sensores y puertas no van en inventario.

## Colocacion en planificacion

`BuildModePlacementController` controla drag/drop.

Variables importantes:

- `worldCamera`: camara para convertir mouse a mundo.
- `referenceTilemap`: tilemap usado para snap de celdas.
- `blockedTilemaps`: tilemaps que bloquean colocacion.
- `placedObjectsRoot`: padre de objetos colocados.
- `gameStateManager`: estado del nivel.
- `inventoryPanel`: panel de inventario.
- `occupancyMask`: capas que cuentan como ocupadas.
- `occupancyBoxInset`: ajuste para detectar ocupacion de celda.
- `validPreviewColor`: color preview valido.
- `invalidPreviewColor`: color preview invalido.

Solo se puede colocar/mover objetos en `Planning`. Al entrar en `Execution`, el inventario queda bloqueado salvo herramientas de ejecucion como el pico.

## UI y estado del nivel

`GameStateManager` controla:

- fase actual (`Planning` / `Execution`)
- inventario runtime
- cuenta regresiva de planificacion
- reset de escena

`LevelHudPanel` muestra:

- mundo/nivel
- boton de pausa
- boton de reset
- cuenta regresiva si el `LevelDefinition` tiene `planningTimeLimitSeconds > 0`

`PlaceableInventoryPanel` muestra los objetos disponibles del `InventorySet`.

La UI de cada nivel se edita desde prefabs:

```text
Assets/Prefabs/UI/UI_LevelRoot.prefab
```

`UI_LevelRoot` se coloca como hijo del `Canvas` y contiene HUD, inventario, pausa,
victoria y derrota como subprefabs. `LevelUiRoot` resuelve los managers de la
escena e inyecta sus referencias al iniciar; los scripts de UI no construyen su
jerarquia visual durante Play Mode.

Para crear o reparar la composicion en escenas de gameplay:

```text
Unlucky Ducky/UI/Generate Level UI and Migrate Scenes
```

El bootstrapper de niveles agrega automaticamente `UI_LevelRoot` y `EventSystem`
cuando faltan. Los slots de inventario siguen instanciandose en runtime porque
su cantidad y contenido dependen del `PlaceableInventorySet`.

## Validacion rapida

Comandos utiles:

```powershell
dotnet build unlucky_ducky_unity.slnx
dotnet test unlucky_ducky_unity.slnx --no-build
```

Notas:

- `dotnet build` valida compilacion C# generada, no reemplaza probar la escena en Unity.
- `dotnet test` en este entorno puede ser una validacion limitada frente al Test Runner real de Unity.
- Para problemas visuales o de wiring, revisar la escena/prefab en Unity ademas del codigo.

## Gotchas

- Sensores y puertas deben colocarse en escena, no en `InventorySet`.
- Sensor y puerta se conectan por texto exacto: `connectionId` debe coincidir con `sensorConnectionId`.
- Las puertas hacen toggle, no "abrir siempre".
- `explosionRadiusInCells = 1` en la bomba equivale a 3x3.
- El pico consume unidades del inventario durante `Execution`.
- `planningTimeLimitSeconds = 0` desactiva la cuenta regresiva.
- Los hazards matan al pato en `Execution`, no en `Planning`.
- Si una bomba o pico no destruye un tile, revisar que el tilemap tenga `DestructibleTilemapLayer` o que `LevelManager` apunte al tilemap correcto.
- Si un objeto de escena no queda centrado, verificar que tenga uno de los componentes cubiertos por `LevelActorGridSnapper`.
