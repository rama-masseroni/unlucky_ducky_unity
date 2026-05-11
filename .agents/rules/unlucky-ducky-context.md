---
trigger: always_on
---

##Contexto del juego a desarrollar
Unlucky Ducky es un juego para PC de puzles y estrategia 2D con vista estilo cenital. El objetivo es guiar a un pato de hule a través de distintos mundos, cada uno con sus niveles, donde el jugador deberá colocar objetos que controlarán el flujo del agua, sistemas de ventilación, cintas transportadoras, además de objetos que pueden hacer perder el nivel, como bombas, ratas, rayos láser, etc. Se puede pensar como que estamos creando la máquina de Rude Goldberg que permitirá llevar al pato a buen puerto, con la experiencia deseada de que el jugador se tome su tiempo para pensar dónde pondrá qué objetos, para después ver cómo se desenrolla todo.

## Objetivo del MVP
Validar un juego de puzzles en grilla con simulación en tiempo real:
1. El jugador entra en una fase única de preparación.
2. Coloca piezas limitadas sobre el nivel.
3. Presiona Play.
4. Observa cómo el nivel se resuelve.

### Alcance
- 3 niveles.
- Los primeros 2 funcionan como tutorial.
- Sin necesidad de sprites finales ni arte pulido.
- El foco está en validar el loop: **colocar piezas -> Play -> observar resolución**.

### Supuestos ya definidos
- No depender de físicas complejas.
- Movimiento determinista y fácil de razonar.
- El pato se mueve con lógica de grilla, aunque visualmente pueda desplazarse de forma fluida entre centros de celdas.
- El jugador solo coloca piezas al inicio.
- Luego todo corre en tiempo real.
- Las interacciones se resuelven por reglas simples.

## Enfoque técnico recomendado
- **Grid / Tilemap** para lo estático.
- **Prefabs** para lo interactivo.
- **ScriptableObjects** para datos reutilizables.
- **Simulación determinista propia** como fuente de verdad del gameplay.
- **Composición** mediante componentes e interfaces para no acoplar lógica a una sola pieza.

### Principio rector
Este juego no necesita simular física realista. Necesita simular **reglas claras sobre una grilla**, mostrándolas en tiempo real.

## Sistemas / herramientas a crear

### `LevelGrid`
Capa lógica de la grilla.
- conoce ancho y alto,
- convierte celda <-> mundo,
- registra ocupación,
- consulta si una celda está libre, bloqueada o fuera de rango.

### `LevelDefinition` (`ScriptableObject`)
Un asset por nivel.
Debe contener:
- identidad del nivel,
- tamaño lógico,
- texto tutorial,
- spawn del pato,
- objetos iniciales,
- piezas disponibles,
- piezas obligatorias,
- condiciones de victoria.

### `PlaceableDefinition` (`ScriptableObject`)
Un asset por tipo de pieza.
Debe contener:
- `id`,
- `displayName`,
- `prefab`,
- `size`,
- `rotatable`,
- `inventoryCost`,
- `behaviors`.

### `LevelBuilder`
Lee `LevelDefinition`, construye el escenario e instancia objetos iniciales.

### `PlacementSystem`
Gestiona la fase previa a la simulación.
- preview,
- snap a grilla,
- validación,
- inventario,
- recolocación,
- confirmación de Play.

### `SimulationController`
Controla estados del nivel.
Estados sugeridos:
- `Editing`
- `Ready`
- `Simulating`
- `Won`
- `Lost`

### `DuckMover`
Controlador del protagonista.
- guarda celda actual y dirección,
- mueve de centro de celda a centro de celda,
- resuelve interacciones al entrar a una celda.

### `WinConditionChecker`
Evalúa llegada a meta, muerte del pato y otras condiciones simples.

### `LevelValidator`
Utilitario opcional.
- detecta spawn/meta faltantes,
- piezas fuera de rango,
- inconsistencias en inventario.

## Qué usar para cada tipo de contenido

### Tilemap / Grid
Usar para:
- suelo base,
- paredes,
- layout estructural,
- zonas bloqueadas,
- guías visuales.

### Prefabs
Usar para:
- bomba,
- rata,
- ventilador,
- cinta transportadora,
- grúa,
- patineta,
- meta,
- spawn,
- hazards y piezas colocables.

## Criterios de arquitectura

### 1. Separar datos de comportamiento
- `ScriptableObject` para datos/configuración reusable.
- `MonoBehaviour` para estado y comportamiento runtime.

### 2. Preferir composición sobre herencia excesiva
En vez de una jerarquía rígida, usar componentes reutilizables como:
- `KillDuckOnContact`
- `TimedExplosion`
- `PatrolHorizontal`
- `BreakableSurface`
- `ConveyorRedirector`

### 3. Usar interfaces pequeñas
Ejemplos:
- `IPlaceable`
- `IGridOccupant`
- `IActivatable`
- `ITriggerable`
- `IBreakable`
- `IResettable`
- `IDuckInteraction`

### 4. Evitar un `LevelManager` gigante
No mezclar en una sola clase:
- construcción del nivel,
- colocación,
- simulación,
- UI,
- tutorial,
- victoria/derrota,
- reset.

### 5. Diseñar pensando en reset
El sistema debe volver al estado inicial de forma confiable.

## Modelado de datos recomendado

### `LevelDefinition`
Para el tamaño del nivel:
- evitar `int[2]`,
- preferir `Vector2Int`, o mejor, un struct serializable con `width` y `height`.

Para posiciones:
- usar `Vector2Int`.

Para direcciones:
- usar `enum`.

Para colecciones editables:
- preferir `List<T>`.

### `PlaceableDefinition`
Debe describir **qué es** la pieza, no concentrar toda su lógica.

## Comportamientos reutilizables
Las reglas especiales no deberían quedar pegadas a una sola pieza si pueden repetirse.

Ejemplo:
La bomba no es la única que puede matar al pato. También podrían hacerlo:
- una rata por contacto,
- un pozo,
- fuego,
- otra trampa futura.

Por eso, capacidades como estas conviene modelarlas como comportamientos reutilizables:
- `KillDuckOnContact`
- `TimedExplosion`
- `PatrolHorizontal`
- `BreakSurfaceOnExplosion`
- `RedirectMovement`
- `PushDuck`
- `BlockMovement`

### Regla mental
Si una lógica puede aparecer en más de un tipo de pieza, probablemente no pertenece a una sola clase concreta. Pertenece a un comportamiento reutilizable.

## Orden de implementación sugerido

### Fase 1
- `LevelGrid`
- movimiento básico del pato entre celdas
- spawn y meta
- reset simple

### Fase 2
- `PlacementSystem`
- inventario simple
- validación de colocación
- botón Play

### Fase 3
- piezas clave:
  - bomba,
  - piso rompible,
  - rata o cinta/ventilador

### Fase 4
- tutoriales básicos
- nivel 3 más abierto

### Fase 5
- utilidades de edición
- validador de niveles
- mejor soporte para escalar a 20 niveles

## Código sugerido — `LevelDefinition`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string levelId;
    [SerializeField] private string displayName;

    [Header("Grid")]
    [SerializeField] private LevelSize size;

    [Header("Tutorial")]
    [SerializeField, TextArea] private string tutorialText;

    [Header("Runtime Setup")]
    [SerializeField] private SpawnData duckSpawn;
    [SerializeField] private List<PlacedObjectData> initialObjects = new();

    [Header("Player Placement")]
    [SerializeField] private List<PlaceableStockData> availablePieces = new();
    [SerializeField] private List<PlaceableRequirementData> mandatoryPieces = new();
}

[Serializable]
public struct LevelSize
{
    public int width;
    public int height;
}

[Serializable]
public class SpawnData
{
    public Vector2Int cell;
    public Direction direction;
}

[Serializable]
public class PlacedObjectData
{
    public PlaceableDefinition definition;
    public Vector2Int cell;
    public Direction direction;
}

[Serializable]
public class PlaceableStockData
{
    public PlaceableDefinition definition;
    public int amount;
}

[Serializable]
public class PlaceableRequirementData
{
    public PlaceableDefinition definition;
    public int requiredAmount;
}

public enum Direction
{
    Up,
    Right,
    Down,
    Left
}
```

## Código sugerido — `PlaceableDefinition`

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Placeable Definition")]
public class PlaceableDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public GameObject prefab;
    public Vector2Int size;
    public bool rotatable;
    public int inventoryCost;
    public List<PlaceableBehaviorDefinition> behaviors;
}
```

## Código sugerido — base de comportamiento reusable

```csharp
using UnityEngine;

public abstract class PlaceableBehaviorDefinition : ScriptableObject
{
    public abstract void Apply(PlaceableContext context);
}

public class PlaceableContext
{
    public GameObject Owner;
    public DuckController Duck;
    public Vector2Int Cell;
}
```

## Código sugerido — interacción reusable con el pato

```csharp
public interface IDuckInteraction
{
    void InteractWithDuck(DuckController duck);
}

using UnityEngine;

public class KillDuckOnContact : MonoBehaviour, IDuckInteraction
{
    public void InteractWithDuck(DuckController duck)
    {
        duck.Die();
    }
}
```

## Código sugerido — explosión temporizada

```csharp
using UnityEngine;

public class TimedExplosion : MonoBehaviour
{
    [SerializeField] private int radius;

    public void Explode()
    {
        // Buscar objetivos en el radio y actuar por interfaces.
    }
}
```

## Interfaces sugeridas

```csharp
public interface IPlaceable {}
public interface IGridOccupant {}
public interface IActivatable {}
public interface ITriggerable {}
public interface IBreakable { void Break(); }
public interface IResettable { void ResetState(); }
```