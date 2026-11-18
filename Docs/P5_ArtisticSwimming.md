# P5 · Natación artística: especificación para Claude Code

Este archivo va en `Docs/` del proyecto. Describe la prueba 5 completa. Si algo contradice a `CLAUDE.md`, manda `CLAUDE.md`, con una excepción: los nombres de objetos de escena de P5 de este archivo reemplazan los de la sección 6 de `CLAUDE.md`.

Alcance: P5 es la última en la prioridad (P2, P1, P3, P4, P5). Solo se construye si Hub, P2 y P1 ya están terminadas.

## 1. Resumen

Juego de ritmo cooperativo en 2D, dentro del mural `Mural_P5` del estadio. Los 4 magos nadan en la piscina de la Luna haciendo una rutina de 4 figuras. Cada mago tiene su carril de notas de su color. Cuando una nota cruza la línea de golpe, el jugador pulsa su botón. Si la nota se acierta, el mago hace su movimiento y la figura se ve mejor. Un jurado de 7 búhos da la nota al terminar cada figura.

- **Deporte real:** natación artística por equipos (sincronización).
- **Qué la hace cooperativa:** las notas blancas solo cuentan si los 4 aciertan a la vez, y la nota de cada figura es del equipo, no de cada jugador.
- **Duración:** cada figura dura entre 20 y 30 segundos; la rutina completa, unos 2 minutos. Así cabe en el video de 3 minutos.
- **Cámara:** todo el tiempo en 2D frontal sobre el mural. No hay cambio de cámara dentro de la prueba. El único cambio 3D a 2D es al entrar por el portal, ya resuelto por el sistema de `CLAUDE.md` sección 5.

## 2. Qué se ve en pantalla (2D)

| Zona | Contenido |
| --- | --- |
| Centro | La piscina vista de frente, con la línea del agua. Los 4 magos nadan aquí y forman la figura. |
| Derecha | 4 carriles verticales de notas (`NoteLane_1` a `NoteLane_4`), uno por mago, con su color. Las notas bajan hacia la línea de golpe (`HitLine`). |
| Arriba | Barra de progreso de la rutina, nombre de la figura actual, puntaje del equipo, combo y estrellas. |
| Fondo | Luna, faroles flotantes, nenúfares y una columna griega. |
| Costado | El jurado: 7 búhos con paletas (`JuryBox`). |
| Izquierda, solo en la intro | Una plataforma de salto (`DivingBoard`) de la que los magos se lanzan al agua. |

## 3. Reglas de juego

### Controles

Un solo botón de acción por jugador (`Cast` en `NetworkInputData`). No se necesita movimiento: durante la rutina el movimiento del mago está bloqueado y el mago va a su posición de la figura.

### Tipos de nota

| Tipo | Carril | Qué hay que hacer | Si se acierta | Si se falla |
| --- | --- | --- | --- | --- |
| Color | El del mago | Pulsar al cruzar la línea | Suma puntos y combo | Resta combo |
| Blanca | Los 4 a la vez | Los 4 pulsan dentro de la misma ventana | Suma el doble | Si falla uno, falla la nota para todos |
| Dorada | Un carril | Pulsar al cruzar la línea | Bonus | No pasa nada |
| Trampa | Un carril | **No** pulsar | Nada | Si pulsan, restan puntos y rompen el combo |

Cada nota color o dorada solo la puede pulsar el mago de su carril. Si otro jugador pulsa en un carril ajeno, no cuenta (regla de colores).

### Precisión

Cada pulsación se juzga por qué tan cerca de la línea cruza la nota: `Perfect`, `Good` o `Miss`. Las ventanas de tiempo, los puntos por juicio y los umbrales van en el ScriptableObject `SwimmingSettings`, nunca escritos en el código.

Valores iniciales propuestos para `SwimmingSettings` (el equipo los ajusta jugando):

| Valor | Propuesta |
| --- | --- |
| Ventana Perfect | 0,08 s a cada lado de la línea |
| Ventana Good | 0,16 s a cada lado |
| Puntos Perfect / Good / Miss | 100 / 60 / 0 |
| Puntos de nota trampa pulsada | -80 |
| Aciertos de combo por estrella | 8 |
| Precisión mínima para pasar una figura | 60 % |
| Reintentos máximos por figura | 2 |
| Medalla de oro / plata / bronce (nota promedio del jurado) | 9,0 / 7,5 / 6,0 |

### Combo y estrellas

El combo es del equipo: sube con cada acierto y vuelve a cero cuando alguien falla. Cada 8 aciertos seguidos se gana una estrella, que suma puntos extra pero no cambia si se pasa la figura.

### Las figuras

La rutina tiene 4 figuras. Entre figura y figura, los magos nadan a las posiciones de la siguiente (unos 4 segundos).

| Figura | Formación | Dificultad |
| --- | --- | --- |
| 1 · Línea | Los 4 en una fila a la misma altura | Notas lentas, solo de color |
| 2 · Diamante | Uno arriba, uno abajo y dos al centro a los lados | Aparecen las notas blancas |
| 3 · Cuadrado | Dos arriba y dos abajo | Notas más rápidas, dorada y trampa |
| Final · Flor | Los 4 en círculo, cada uno en su color | Todos los tipos, ritmo más alto |

Para el 2D frontal, cada formación son posiciones `x, y` en la piscina. La posición de cada mago es fija según su color (Elemental slot 1, Nebulosa 2, Druida 3, Rayo 4).

### Olas (opcional, segunda fase)

En la figura 3 y en la final pueden pasar olas: la formación se mueve y la ventana de golpe se reduce a la mitad durante 4 tiempos. Es un extra: se construye solo cuando todo lo demás funciona.

### Checkpoint y jurado

- Al terminar cada figura, el equipo recibe una precisión: aciertos ponderados entre notas posibles.
- Los 7 búhos muestran una nota de 0 a 10: la precisión por 10, más una pequeña variación fija por búho (tabla en `SwimmingSettings`, igual en todos los clientes).
- Si la precisión está por debajo del mínimo, el equipo repite solo esa figura. Después de 2 reintentos fallidos, la prueba sigue con la nota baja para no atascarse.
- El final de la prueba usa el promedio de las 4 notas del jurado: oro, plata o bronce. Si la nota está por debajo del bronce, se declara derrota.

## 4. Estados y red

La prueba usa la máquina de estados de `CLAUDE.md`: `WaitingAtPortal → EnteringMural → Intro → Playing → Results → ExitingMural`. El estado `Playing` tiene un subestado propio.

`RoutinePhase`: `FigureCountdown → FigureRunning → FigureJudging → FormationTransition`, que se repite por cada figura.

### Autoridad y sincronización

Principio: **las notas no se envían por la red.** Cada nota se calcula a partir del tick de inicio de la figura y del archivo de la rutina. Así los 4 clientes ven las mismas notas sin gastar ancho de banda.

| Dato | Dónde vive | Quién escribe |
| --- | --- | --- |
| `RoutinePhase`, `CurrentFigureIndex`, `AttemptNumber` | `[Networked]` en `SwimmingTrialFlowController` | Host |
| `FigureStartTick` | `[Networked]` en `SwimmingTrialFlowController` | Host |
| `TeamScore`, `Combo`, `Stars` | `[Networked]` en `SwimmingScoreBoard` | Host |
| Aciertos y fallos por jugador (para el resumen final) | `NetworkArray<int>` de capacidad 4 | Host |
| Notas de la rutina | `RoutineChart` (ScriptableObject, igual en todos) | Nadie, es dato fijo |
| Posiciones de formación | Se calculan de `CurrentFigureIndex` y del slot del mago | Nadie, es local |
| Efectos y sonidos de acierto | RPC del host a todos | Host |

### Juicio de notas

- El host juzga en `FixedUpdateNetwork`, leyendo el `NetworkInputData` de cada jugador: si `Cast` fue pulsado en este tick (`NetworkButtons.WasPressed`), busca la nota más cercana de su carril dentro de la ventana.
- El tiempo de una nota es un tick: `FigureStartTick` más el tiempo de la nota convertido a ticks con `Runner.TickRate`. Nunca se usa `Time.time`.
- Cada cliente muestra el feedback al instante (anticipación local) y el host confirma el resultado. Si el host dice lo contrario, se corrige el juicio mostrado.
- Los clientes dibujan las notas en `Render()`, con un interpolador del tick. Eso evita el parpadeo.

## 5. Clases

Namespace `Magolimpiadas.Trials.ArtisticSwimming`, salvo lo común, que va en `Magolimpiadas.Trials.Common`.

| Clase | Tipo | Responsabilidad |
| --- | --- | --- |
| `SwimmingTrialFlowController` | `NetworkBehaviour`, hereda de `TrialFlowController` | Estados de la prueba y de la rutina, figuras, reintentos |
| `RoutineChart` | `ScriptableObject` | Lista de notas por figura: tiempo, carril, tipo |
| `RoutineNote` | struct | Un dato de nota: `TimeSeconds`, `LaneIndex`, `NoteType` |
| `NoteType` | enum | `Color`, `White`, `Gold`, `Trap` |
| `HitJudgement` | enum | `Perfect`, `Good`, `Miss` |
| `RoutineClock` | clase | Convierte `FigureStartTick` y `Runner.Tick` en tiempo de la rutina |
| `NoteJudge` | `NetworkBehaviour` | Solo en el host: compara los pulsos con las notas y decide el juicio |
| `SwimmingScoreBoard` | `NetworkBehaviour` | Puntaje, combo y estrellas en `[Networked]` |
| `NoteLanePresenter` | `MonoBehaviour` | Solo visual: dibuja las notas bajando por un carril |
| `HitFeedbackPresenter` | `MonoBehaviour` | Efecto y sonido por juicio, y animación del mago al acertar |
| `FormationPositioner` | `MonoBehaviour` | Mueve los sprites a los slots de la figura actual |
| `JuryScorePresenter` | `MonoBehaviour` | Muestra las notas de los búhos |
| `SwimmingSettings` | `ScriptableObject` | Ventanas, puntos, umbrales y variación del jurado |
| `RoutineChartValidator` | clase de editor | Revisa que el archivo de rutina esté en orden y no tenga notas superpuestas |

Reutilizar lo ya hecho en `Trials/Common`: máquina de estados, resultados y medallas, y temporizador. No duplicar.

## 6. Objetos de escena (reemplazan a los de CLAUDE.md para P5)

Dentro de `Mural_P5`:

- `MuralSpawn_1` a `MuralSpawn_4`: donde aparecen los magos al entrar.
- `DivingBoard`: plataforma de la intro.
- `Formation_Line_1` a `_4`, `Formation_Diamond_1` a `_4`, `Formation_Square_1` a `_4`, `Formation_Flower_1` a `_4`: posiciones de cada figura.
- `NoteLane_1` a `NoteLane_4`: carriles de notas.
- `HitLine`: línea de golpe, igual para los 4 carriles.
- `JuryBox` con `Owl_1` a `Owl_7`.
- `Cam_P5`: cámara de la sala, la usa `LocalCameraDirector`.

## 7. Qué consigue o hace el equipo (sin IA)

| Pieza | Detalle |
| --- | --- |
| Sprites de los 4 magos nadando | Animaciones `SwimIdle`, `Swim`, `FigurePose`, `Celebrate`, `Stunned` |
| Búhos del jurado | 7 sprites con paletas de nota |
| Iconos de las notas | Color (4), blanca, dorada y trampa |
| Fondo de la piscina | Agua, línea del agua, luna, faroles y nenúfares |
| Música | Una canción sin IA y con licencia libre, con **BPM conocido**. De ella se saca el ritmo de las notas |
| Sonidos | Acierto Perfect y Good, fallo, trampa, ola y aplauso del jurado |
| La rutina | El equipo ajusta a mano `RoutineChart` para que las notas sigan la canción. Claude Code puede generar una rutina inicial a partir del BPM para probar, que el equipo reemplaza |

## 8. Orden de construcción (pasos pequeños)

1. `SwimmingSettings`, `RoutineChart` y los enums. Una rutina de prueba con 2 figuras y notas de color.
2. `RoutineClock` y `NoteLanePresenter`: ver las notas bajar, sincronizadas entre 2 clientes.
3. `NoteJudge`, `SwimmingScoreBoard` y el feedback: acertar, fallar, combo.
4. Notas blancas (los 4 a la vez), la parte más cooperativa.
5. `FormationPositioner` y las transiciones entre figuras.
6. `JuryScorePresenter`, checkpoint y reintentos, y resultado con medalla.
7. Notas dorada y trampa, y la figura final.
8. Olas, solo si sobra tiempo.

Cada paso debe compilar y poder probarse con Multiplayer Play Mode antes del siguiente.

## 9. Pruebas

- Con 2 jugadores en Multiplayer Play Mode, las notas aparecen al mismo tiempo en los dos.
- Una nota blanca falla si solo 3 pulsan; acierta si los 4 pulsan dentro de la ventana.
- Un jugador pulsando en el carril ajeno no suma.
- Si un jugador se desconecta durante la rutina, la prueba sigue y sus notas se cuentan como fallos, sin romperse.
- Con retraso de red simulado, el juicio del host no cambia el resultado de forma visible.
- Al repetir una figura, solo se reinicia esa figura, no la rutina.

## 10. Pregunta abierta para el equipo

- ¿Qué canción y qué BPM? De eso depende el diseño de toda la rutina.
