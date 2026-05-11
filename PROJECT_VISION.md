# Project Vision

## Game Concept

**Unlucky Ducky** es un juego de puzzle físico en grilla donde el jugador debe guiar a un pato de hule a través de distintos niveles usando objetos limitados antes de iniciar la simulación.

La fantasía principal es resolver situaciones caóticas y peligrosas colocando elementos del escenario de forma estratégica: plataformas, bombas, minas, ventiladores, enemigos u otros objetos interactivos.

El objetivo del jugador es lograr que el pato llegue a la salida del nivel sin morir, caer fuera del mapa o quedar bloqueado.

El juego se expande en 4 mundos, cada uno con 5 niveles. Cada mundo introduce una nueva mecánica/objeto que se mantiene en los niveles posteriores y que se combina con las mecánicas de los mundos anteriores. De esta manera se busca que el juego sea progresivamente más desafiante y que fomente la experimentación por parte del jugador.

El loop principal del juego es:

1. Observar el nivel.
2. Revisar los objetos disponibles.
3. Colocar objetos en la grilla.
4. Iniciar la simulación.
5. Ver cómo interactúan el pato, los enemigos, los objetos y el terreno.
6. Ganar, perder o reiniciar para probar otra solución.

---

## Target Gameplay

### Movimiento

- El pato se mueve automáticamente durante la simulación.
- El jugador no controla directamente al pato una vez iniciado el nivel.
- El movimiento debe ser predecible, basado en reglas simples.
- Algunos enemigos, como ratas, también pueden moverse automáticamente.
- La dirección inicial de ciertos objetos móviles puede definirse desde el editor o desde los datos del nivel.
- Todos los objetos móviles se mueven a la misma velocidad, salvo que se especifique lo contrario o que haya un tipo de modificador al piso que varíe esa velocidad.

### Interacción con bloques/tilemaps

- El nivel se construye sobre una grilla.
- El escenario puede estar compuesto por tilemaps o bloques individuales.
- Debe distinguirse entre:
  - Terreno sólido.
  - Terreno destruible.
  - Terreno indestructible.
  - Huecos o espacios vacíos.
  - Salidas o puertas.
  - Sensores o activadores.
- Los objetos móviles deben poder detectar colisiones contra bloques relevantes del nivel. Por ejemplo, el objeto pricipal "Pato" debe poder colisionar con un terreno sólido, destruible o indestructible como si estuviera caminando sobre él.
- Los objetos móviles deben poder detectar colisiones contra objetos colocados por el jugador.

### Construcción/destrucción

- Antes de iniciar la simulación, el jugador debe colocar objetos solicitados en espacios válidos de la grilla.
- Los objetos se colocan en el nivel usando un sistema de grilla. El jugador puede ver en tiempo real donde se puede colocar un objeto, y donde no, a medida que los arrastra desde el listado al area de juego.
- El sistema de grilla debe ser capaz de manejar diferentes tamaños y formas de nivel, siempre y cuando los tilemaps tengan el mismo tamaño.
- Cuando un jugador suelta un objeto deseado en un espacio válido de la grilla de juego,el objeto hace una acción de snap hacia la posicion central de la celda deseada.
- Cuando un objeto es exitosamente colocado en la grilla de juego,este debe aparecer en la grilla y consumir un item del inventario.
- Algunos objetos pueden destruir bloques, modificar el recorrido o activar reacciones.
- La destrucción debe estar limitada por reglas claras:
  - Qué tipo de bloque puede destruirse.
  - Qué objeto puede destruirlo.
  - En qué radio o dirección actúa.
  - En qué momento se activa.
- La destrucción puede modificar el recorrido del pato y de los enemigos.

### Enemigos

- Los enemigos funcionan como obstáculos dinámicos.
- Pueden moverse automáticamente.
- Pueden matar al pato si entran en contacto con él.
- También pueden activar sensores u otros elementos del escenario si corresponde.
- Su comportamiento debe ser simple, legible y testeable.

### Condiciones de victoria/derrota

El jugador gana cuando:

- El pato llega correctamente a la salida.
- La salida está habilitada, si el nivel requiere alguna condición previa.

El jugador pierde cuando:

- El pato muere por contacto con un enemigo, mina, fuego u otro peligro.
- El pato cae fuera del nivel.
- El pato queda atrapado sin posibilidad de avanzar.
- Se agota algún límite definido por el nivel, como tiempo o intentos, si aplica.

---

## Architecture Goals

La arquitectura con la que se arrancó el proyecto es una mezcla entre:

- **MonoBehaviour clásico** para objetos presentes en escena.
- **ScriptableObjects** para datos reutilizables.
- **Clases C# puras** para reglas testeables sin depender de Unity.

NO está limitado a los elementos mencionados.

La arquitectura debería separar claramente:

- **Gameplay:** movimiento, colisiones, reglas de simulación, victoria y derrota.
- **UI:** inventario, botones, feedback visual, contador, estado del nivel.
- **Datos:** definiciones de bloques, objetos, enemigos y niveles.
- **Escenas:** composición visual y referencias entre objetos principales.

Los sistemas que deberían poder testearse sin depender directamente de la escena son:

- Validación de colocación de objetos.
- Reglas de destrucción.
- Condiciones de victoria y derrota.
- Consumo de inventario.
- Carga de datos de nivel.
- Reglas básicas de movimiento en grilla.
- Evaluación de interacciones entre entidades.

---

## Scene Structure

Cada escena de nivel debería mantener una estructura clara y repetible.

### Objetos raíz esperados

- `LevelRoot`
- `Grid`
- `GameplayManagers`
- `PlayerRoot`
- `EnemyRoot`
- `PlacedObjectsRoot`
- `UIRoot`
- `CameraRoot`

### Tilemaps/Grid

La escena debería tener un objeto `Grid` con tilemaps separados según responsabilidad:

- `GroundTilemap`
- `DestructibleTilemap`
- `IndestructibleTilemap`
- `HazardTilemap`
- `GoalTilemap`
- `SensorTilemap`, si aplica

La separación permite modificar o consultar cada tipo de bloque sin mezclar responsabilidades.

### Managers

Los managers principales esperados son:

- `LevelManager`
- `GameStateManager`
- `GridInteractionManager`
- `InventoryManager`
- `BuildModeManager`
- `DestructionManager`
- `WinLoseManager`
- `UIManager`

Los managers no deberían contener toda la lógica del juego directamente. Siempre que sea posible, deberían delegar reglas a clases o servicios testeables.

### Prefabs importantes

- `Player_Duck`
- `Enemy_Rat`
- `Placeable_Bomb`
- `Placeable_Mine`
- `Placeable_PlatformWood`
- `Placeable_PlatformMetal`
- `Placeable_Fan`
- `Goal_Door`
- `Sensor_Movement`
- `UI_InventorySlot`
- `UI_LevelButton`

### Cámaras/UI

- La cámara principal debería estar separada del gameplay.
- La UI debería vivir dentro de un `Canvas` bajo `UIRoot`.
- La UI mínima debería incluir:
  - Botón de iniciar simulación.
  - Botón de reiniciar nivel.
  - Inventario de objetos disponibles.
  - Indicador de victoria o derrota.
  - Texto de cuenta regresiva, si el nivel la usa.

---

## Data Model

Los datos principales deberían vivir en **ScriptableObjects** para facilitar edición, reutilización y balance.

### Bloques

`BlockDefinition`

Datos sugeridos:

- `id`
- `displayName`
- `blockType`
- `isSolid`
- `isDestructible`
- `destructionResistance`
- `allowedInteractions`
- `tileReference`
- `description`

### Niveles

`LevelDefinition`

Datos sugeridos:

- `id`
- `levelName`
- `sceneName`
- `worldName`
- `gridWidth`
- `gridHeight`
- `availableItems`
- `requiredPlacedItems`
- `winCondition`
- `loseConditions`
- `timeLimit`
- `initialPlayerPosition`
- `initialGoalPosition`
- `initialEnemies`
- `levelDescription`

### Items

`PlaceableItemDefinition`

Datos sugeridos:

- `id`
- `displayName`
- `prefab`
- `icon`
- `itemType`
- `maxAmount`
- `isRequired`
- `placementRules`
- `activationType`
- `description`

### Enemigos

`EnemyDefinition`

Datos sugeridos:

- `id`
- `displayName`
- `prefab`
- `movementType`
- `initialDirection`
- `speed`
- `damageType`
- `canActivateSensors`
- `canBeDestroyed`
- `description`

### Reglas de colocación/destrucción

`PlacementRuleDefinition`

Datos sugeridos:

- `canPlaceOnEmptyCell`
- `canPlaceOnGround`
- `canPlaceOnDestructible`
- `canOverlapOtherObjects`
- `requiresSupportBelow`
- `blockedByHazards`
- `allowedTileTypes`

`DestructionRuleDefinition`

Datos sugeridos:

- `affectedBlockTypes`
- `radius`
- `direction`
- `delay`
- `requiresLineOfSight`
- `destroysEnemies`
- `destroysPlacedObjects`

---

## Core Systems

### LevelManager

Responsable de:

- Cargar la configuración del nivel.
- Inicializar entidades.
- Comunicar el estado inicial a otros sistemas.
- Reiniciar el nivel.
- Coordinar el inicio de la simulación.

### Grid/Tilemap Interaction

Responsable de:

- Convertir posiciones del mouse a celdas.
- Consultar si una celda está ocupada.
- Consultar qué tile existe en una celda.
- Validar si una celda es apta para colocar objetos.
- Exponer métodos seguros para modificar tilemaps.

### Inventory/Build Mode

Responsable de:

- Mostrar objetos disponibles.
- Controlar cantidades restantes.
- Permitir seleccionar, arrastrar y colocar objetos.
- Validar reglas de colocación.
- Bloquear edición una vez iniciada la simulación.

### Destruction System

Responsable de:

- Ejecutar destrucciones de bloques.
- Aplicar reglas de radio, dirección o delay.
- Actualizar tilemaps.
- Notificar cambios relevantes a otros sistemas.
- Mantener la lógica desacoplada de los efectos visuales.

### Player Controller

Responsable de:

- Mover automáticamente al pato.
- Detectar colisiones relevantes.
- Reaccionar ante peligros, paredes, huecos y salida.
- Comunicar eventos como muerte, llegada a meta o bloqueo.

### Enemy Controller

Responsable de:

- Mover enemigos según reglas simples.
- Detectar colisiones.
- Interactuar con el pato, sensores u objetos.
- Mantener comportamientos consistentes y predecibles.

### GameStateManager

Responsable de controlar los estados principales del nivel:

- `Planning`
- `Countdown`
- `Simulation`
- `Win`
- `Lose`
- `Paused`

### Save/Load

En una primera versión, el sistema de guardado debería ser mínimo.

Responsable de:

- Guardar progreso de niveles desbloqueados.
- Guardar opciones simples.
- No guardar soluciones complejas todavía.

---

## Testing Strategy

La estrategia de testing debería priorizar lógica desacoplada de Unity.

### Unit Tests

Probar clases C# puras, por ejemplo:

- Validación de colocación.
- Consumo de inventario.
- Reglas de destrucción.
- Evaluación de victoria.
- Evaluación de derrota.
- Conversión de datos de nivel a estado inicial.
- Reglas básicas de movimiento en grilla.

### EditMode Tests

Probar lógica que puede usar tipos de Unity pero no requiere ejecutar una escena completa:

- ScriptableObjects válidos.
- Configuraciones de niveles.
- Definiciones de items.
- Definiciones de bloques.
- Reglas de placement/destruction.
- Métodos de utilidad para grilla.

### PlayMode Tests

Probar interacciones integradas dentro de una escena:

- El pato inicia en la posición correcta.
- El botón de iniciar cambia el estado del juego.
- Un objeto se puede colocar en una celda válida.
- Un objeto no se puede colocar en una celda inválida.
- Una bomba destruye bloques destructibles.
- El pato gana al llegar a la salida.
- El pato pierde al tocar un peligro.

### Lógica desacoplada de Unity

Deberían estar desacoplados de Unity:

- `PlacementValidator`
- `InventoryService`
- `DestructionResolver`
- `WinLoseEvaluator`
- `GridRules`
- `LevelDataValidator`
- `MovementRuleResolver`

Los MonoBehaviours deberían actuar principalmente como adaptadores entre la escena y la lógica del juego.

---

## Naming Conventions

### Idioma de nombres

- Código, carpetas, clases, métodos y assets técnicos en **inglés**.
- Textos visibles para el jugador en **español** o en el idioma definido por la entrega.
- Comentarios en código preferentemente en inglés simple, salvo documentación interna universitaria.

### Clases

Usar nombres claros por responsabilidad:

- `LevelManager`
- `GridInteractionManager`
- `BuildModeManager`
- `InventoryManager`
- `DestructionManager`
- `PlayerController`
- `EnemyController`
- `WinLoseManager`

### ScriptableObjects

Usar sufijo `Definition`:

- `BlockDefinition`
- `LevelDefinition`
- `EnemyDefinition`
- `PlaceableItemDefinition`
- `PlacementRuleDefinition`
- `DestructionRuleDefinition`

### Prefabs

Usar prefijos por tipo:

- `Player_`
- `Enemy_`
- `Placeable_`
- `Hazard_`
- `Goal_`
- `Sensor_`
- `UI_`

Ejemplos:

- `Player_Duck`
- `Enemy_Rat`
- `Placeable_Bomb`
- `Goal_Door`
- `UI_InventorySlot`

### Carpetas

Estructura sugerida:

```text
Assets/
  _Project/
    Art/
    Audio/
    Materials/
    Prefabs/
      Player/
      Enemies/
      Placeables/
      UI/
    Scenes/
    ScriptableObjects/
      Blocks/
      Levels/
      Items/
      Enemies/
      Rules/
    Scripts/
      Core/
      Gameplay/
      Grid/
      Inventory/
      Destruction/
      Player/
      Enemies/
      UI/
      Data/
      Tests/