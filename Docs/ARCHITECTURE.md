# Arquitectura de red · Magolimpiadas

Photon Fusion 2 en topología **Player Host**: uno de los jugadores es el anfitrión (host). Su computador corre la simulación con autoridad y además juega; los otros tres son clientes que se conectan a él. Photon Cloud solo sirve para encontrar la sala y, si hace falta, para retransmitir paquetes (relay); no ejecuta lógica del juego.

## Configuración

| Qué | Dónde | Valor |
| --- | --- | --- |
| App ID de Fusion | `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` | `AppIdFusion` |
| Región fija | el mismo archivo, `FixedRegion` | `us` |
| Ajustes de la sesión en línea | `Assets/_Magolimpiadas/Network/Resources/OnlineSessionSettings.asset` | prefab de la sala, escena del juego, escena del menú, intentos para crear sala |
| Prefab de la sala | `Assets/_Magolimpiadas/Network/Prefabs/MageRoster.prefab` | `NetworkObject` + `MageRoster` |

La región es fija porque en Photon las salas existen por región: si cada computador eligiera la región más cercana por su cuenta, podrían quedar en regiones distintas y no encontrar el código.

## Flujo de conexión

1. **Menú principal → Jugar en línea.** `OnlineMenuPanel` valida el nombre del jugador (`PlayerNameRules`: de 1 a 16 letras, números, espacio, `_` o `-`) y el código de sala (`RoomCodeRules`: 4 letras).
2. **Crear sala.** `SessionLauncher.CreateRoom` crea un objeto con `NetworkRunner`, `NetworkSceneManagerDefault` y `NetworkEvents`, y llama a `StartGame` con `GameMode.Host`, `SessionName` = un código de 4 letras al azar y `PlayerCount` = 4. Si el código ya existe (`ShutdownReason.GameIdAlreadyExists`), genera otro (hasta 3 intentos).
3. **El host crea la sala compartida.** Con `Runner.SpawnAsync` aparece el prefab `MageRoster`. Se usa la versión asíncrona porque justo después de `StartGame` el administrador de escenas de Fusion todavía está ocupado y un `Spawn` inmediato falla.
4. **Unirse.** `SessionLauncher.JoinRoom` llama a `StartGame` con `GameMode.Client` y el código escrito. Si falla, `SessionErrorMessages` traduce la razón a un mensaje: la sala no existe (`GameNotFound`), está llena (`GameIsFull`), la partida ya empezó (`GameClosed`) o no hay conexión (los *timeouts*).
5. **Sala de espera.** Cada peer recibe una copia de `MageRoster`. El host agrega o quita jugadores en `PlayerJoined` y `PlayerLeft`. Los clientes no cambian el estado directamente: le piden los cambios al host por RPC y el host los valida.
6. **Empezar.** Solo el host puede empezar (`SessionLauncher.StartMatch`), y solo si todos los jugadores tienen mago y los clientes pulsaron Listo (`MageRoster.CanStartMatch`). El host marca `MatchStarted`, cierra la sala para que nadie más entre (`SessionInfo.IsOpen = false`) y llama a `Runner.LoadScene` con la escena del juego. `NetworkSceneManagerDefault` hace que todos los clientes carguen la misma escena. `MageRoster` sobrevive al cambio de escena porque en `Spawned` llama a `Runner.MakeDontDestroyOnLoad`.
7. **Desconexión.** `NetworkEvents.OnShutdown` avisa a `SessionLauncher`. Si el jugador no salió a propósito (el host cerró la sala o se perdió la conexión), se guarda un mensaje, se vuelve al menú y el panel de juego en línea lo muestra.

## Los magos en el mundo 3D

Cada jugador sigue manejando su mago con el control de siempre (`PlayerMage`) en su propio computador; así el laberinto, la cámara y los niveles funcionan igual que en el modo solo. Lo que viaja por la red es la **pose** de cada mago.

1. **Al cargar la escena del juego**, el host crea un `MageAvatarState` (prefab `MageAvatar`: `NetworkObject` + `NetworkTransform` + `MageAvatarState`) por cada uno de los 4 magos (`MatchAvatarSpawner`). El del mago de un jugador se crea con ese jugador como *input authority*; el de un mago que nadie eligió no tiene dueño y queda como bot del host.
2. **Cada tick, cada peer envía su pose como *input* de Fusion.** `LocalMageInputProvider` escucha `NetworkEvents.OnInput` y llena un `MagePose` (`INetworkInput`) con la posición, el giro, la velocidad de la animación, si está en el suelo, si se ve, si lleva la antorcha y si el minotauro lo puede perseguir.
3. **El host aplica la pose en `FixedUpdateNetwork`.** `GetInput` le entrega la pose de cada jugador; el host mueve el objeto (que `NetworkTransform` replica e interpola) y guarda los datos de animación en propiedades `[Networked]`. Para los bots copia la pose del bot que corre en su propio mundo.
4. **Cada peer convierte los magos de los demás en títeres.** En `LateUpdate`, `MageAvatarState` busca el cuerpo de ese mago en su escena (`MageBody`), le apaga la IA de bot y su `CharacterController` y le aplica la posición interpolada y la animación.

El mago propio nunca es títere: lo mueve el jugador y su pose es la que se envía.

## El minotauro

Hay un solo minotauro y lo controla el host. Es un objeto de red de la escena (`NetworkObject` + `MinotaurSync` en el objeto `Minotauro`; lo agrega el menú *Juegos Arcanos > 5. Preparar la escena del juego para el modo en línea*).

- **En el host**, `Minotaur` patrulla y persigue como siempre, pero ahora puede perseguir a varias presas (`IMinotaurPrey`): el jugador local (`LocalPlayerPrey`) y, en el host, a los jugadores remotos (`RemotePlayerPrey`, registrados en `MinotaurPreyRegistry`). Si no puede llegar a su presa durante 2 segundos, la abandona.
- **`MinotaurSync`** replica la posición y el giro, si está persiguiendo, la velocidad de la animación y los contadores de rugidos y de atrapadas. Su posición no usa `NetworkTransform`: ese componente reescribe la posición también en el host y eso frena la navegación. Los clientes interpolan entre los dos últimos estados (`TryGetSnapshotsBuffers` + `PropertyReader`).
- **En los clientes** la IA y la navegación del minotauro están apagadas; solo muestran lo que manda el host.
- **Cuando atrapa a un jugador remoto**, el host llama al RPC `MageAvatarState.RPC_Catch` (de `StateAuthority` a `InputAuthority`): solo el dueño de ese mago lo recibe, suena el rugido y su `PlayerMage` reaparece en el inicio. Los demás lo dejan de ver porque su pose llega con `Visible = false`.
- El sonido de cercanía (`MinotaurProximitySound`) va aparte para que también suene en los clientes, donde la IA está apagada.

En línea, el tutorial del templo no pausa el tiempo (`Time.timeScale`), porque la pausa del host congelaría al minotauro y a los bots de todos.

## El portal del laberinto y la cinemática de la isla (`IslandArrivalSync`)

Vive en un objeto de red propio de la escena del juego, **Mundo en línea** (`NetworkObject` + `IslandArrivalSync` + `PickupSync`), que agrega el menú *Juegos Arcanos > 5*.

1. Cuando un jugador entra al portal de salida del laberinto (`IslandTrip`), en línea no empieza su cinemática local: llama a `IslandArrivalSync.TryRequest`, que envía `RPC_RequestOpen` al host con el índice de su mago.
2. El host acepta solo la primera petición: guarda `OpenerMageIndex` y pone `Opened = true` (los dos `[Networked]`).
3. En `Render`, cada peer ve `Opened` y llama a `IslandCinematic.TryStartShared`. Si su jugador está ocupado (intro del búho, atrapado por el minotauro o dentro de otro mundo) lo vuelve a intentar en el siguiente frame.
4. La cinemática compartida no hace caminar al jugador hasta el portal: los demás ven un aviso del búho con el nombre de quien encontró la salida, hay un fundido y los cuatro magos salen cada uno de **su** portal de la isla y encienden el pebetero. Como cada mago usa su propio portal en todos los computadores, terminan en el mismo lugar alrededor del caldero.
5. Mientras dura (`IslandCinematic.AnyInProgress`), `MageAvatarState` no aplica la pose de red a los títeres (`MageBody.WorldIsFree`) para no pelear con la cinemática. Al terminar, los cuerpos de otros jugadores (y los bots en los clientes) vuelven a ser títeres; en el host los bots empiezan a seguir a su jugador.

## Arcos y raquetas (`PickupSync`)

Cada arco (`Collectible`) y cada raqueta (`Racket`) pertenece a un mago (`WizardId` / `mageId`).

1. Solo quien maneja ese cuerpo lo puede recoger: el jugador su propio arco y su raqueta, el host los de los bots. Los títeres tienen el `CharacterController` apagado y no disparan triggers.
2. Al recogerlo en su computador, el objeto lanza el evento `CollectedLocally`. `PickupSync` lo escucha y envía `RPC_ReportPickup(tipo, índice del mago)` al host.
3. El host marca el objeto en `BowsCollected` o `RacketsCollected` (`NetworkArray<NetworkBool>`, uno por mago).
4. En `Render`, cada peer compara esos arreglos con lo que ya mostró y, si falta, llama a `CollectRemotely` sobre el cuerpo de ese mago (`MageBody.Find`): el arco vuela a la espalda del títere y la raqueta a su mano, igual que en el computador de su dueño. Esto no vuelve a disparar `CollectedLocally`, así que no rebota.

Al ser estado `[Networked]` y no un evento suelto, un peer que estaba ocupado lo muestra en cuanto puede.

## Las pruebas

Las cuatro pruebas se juegan juntas: cuando cualquier jugador llega a la entrada de una prueba, entran los cuatro al mismo tiempo. Todo vive en un objeto de red de la escena del juego, **Pruebas en línea** (`NetworkObject` + `TrialDirector` + una sincronización por prueba), que agrega el menú *Juegos Arcanos > 5*.

### Entrar a una prueba (`TrialDirector`)

1. La entrada de cada prueba (`WorldEntrance2D` del templo, `PingPongTable`, el trigger de `SwimmingRace` y `ArtisticSwimmingEntrance`) llama a `OnlineTrials.TryRequest`. En el modo solo devuelve `false` y la prueba empieza como siempre; en línea le envía `RPC_RequestStart` al host y no hace nada más.
2. El host acepta la petición solo si no hay otra prueba en curso: guarda `ActiveTrial` y sube el contador `Session` (los dos `[Networked]`).
3. En `Render`, cada peer ve que `Session` cambió y le pide a la sincronización de esa prueba (`TrialSync.TryEnterLocally`) que entre. Si su mundo está ocupado (por ejemplo, en medio de un fundido), lo vuelve a intentar en cada frame durante 20 segundos.
4. Cuando la prueba termina, el host llama a `OnlineTrials.Finished` y `ActiveTrial` vuelve a `None`.

En línea no se puede salir de una prueba con Esc ni cambiar de mago con Tab, porque los demás siguen jugando. Las ayudas de pantalla de esas teclas se ocultan.

Mientras una prueba mueve los cuerpos 3D (carrera, ceremonias o un mundo 2D abierto), `MageAvatarState` deja de aplicar la pose de red a los títeres (`MageBody.WorldIsFree`) para no pelear con la prueba.

### Carrera de natación (`SwimRaceSync`)

El host simula la carrera. Cada jugador pulsa Espacio en su computador y le envía la brazada al host (`RPC_Stroke`); el host suma las brazadas de todos, mueve a los bots y publica la distancia y el tiempo de llegada de cada carril (`NetworkArray<float>`). Los clientes solo dibujan esas distancias. El host decide cuándo termina (todos llegaron o pasaron 6 segundos desde el último jugador) y el orden final sale de sus tiempos, así que el podio es el mismo para todos.

### Natación artística (`ArtisticSwimmingSync`)

Cada peer juzga solo **su** carril con su propio reloj de audio, que es lo único que se puede juzgar sin retraso.

1. **Inicio de cada figura.** El host anuncia cada figura con un número de jugada (`RPC_StartFigure`); los clientes esperan ese anuncio antes de empezar, así todos van en la misma figura e intento.
2. **Aciertos.** Cada acierto o trampa del jugador local se envía a todos (`RPC_LaneJudged`: jugada, nota, carril, resultado). Los demás lo marcan en su copia del carril remoto. Las notas de un carril remoto esperan 0,45 s más antes de contarse como fallo, para dar tiempo a que llegue el mensaje.
3. **Bots.** Los carriles de los magos que nadie eligió los juega un bot con una semilla fija, así que hacen lo mismo en todos los computadores.
4. **Nota del jurado.** Al terminar la figura, el host envía su precisión (`RPC_FigureResult`) y todos la usan para los búhos y para decidir si se repite la figura. Como el jurado es determinista con la misma precisión, todos ven las mismas notas y la misma medalla.
5. **Final.** El host carga la escena de la ceremonia final para todos con `Runner.LoadScene`.

### Ping pong (`PingPongSync`)

El host simula todo: las bolas, el Guardián, los bots, el marcador y la barra de poder. Cada jugador mueve su propio mago.

- El cliente mueve su paleta localmente y envía su altura unas 20 veces por segundo (`RPC_Paddle`, canal no confiable).
- Al golpear, el cliente busca la bola de su color (o blanca) que tiene cerca y envía `RPC_Hit` con el número de esa bola y su número de lanzamiento. El host la acepta si sigue siendo la misma bola, es del color correcto y todavía no pasó de largo.
- Cada tick el host escribe el estado completo (`PingPongSnapshot`: bolas, paletas, Guardián, marcador, carga, fase, mensaje y resultado) y los clientes lo dibujan. Detectan los cambios (un lanzamiento nuevo, un punto, un golpe) para mostrar los efectos y los sonidos.
- En línea no hay cámara lenta en el golpe de poder: cambiar `Time.timeScale` en el host frenaría el juego de todos.

### Templo 2D (`TempleSync`)

El templo tiene física, enemigos, monedas, puertas y eventos, así que cada computador simula su propio templo y la red mantiene iguales los cuatro.

- **Magos.** Cada mago tiene un dueño: el jugador que lo eligió o, si nadie lo eligió, el host (que lo maneja con su bot). El dueño envía unas 20 veces por segundo sus órdenes (moverse, correr, saltar, atacar, poder, zona, modo arco), su posición, su velocidad y su vida (`RPC_MageState`). Los botones de un solo toque se envían en cuanto se pulsan.
- **Réplicas.** En los demás computadores ese mago lo maneja `RemoteMageControl`, que repite las mismas órdenes (así sus ataques y flechas también aparecen) y corrige poco a poco la posición hacia la del dueño, o lo teletransporta si está a más de 3 unidades. Una réplica no recibe daño ni muere por su cuenta: su vida y su muerte vienen del dueño.
- **Mundo.** Cada vez que algo cambia en un computador, se avisa a los demás con el índice del objeto: enemigo eliminado, moneda recogida (con quién la recogió), evento activado, zona de poder usada, encuentro iniciado, círculo encendido y puerta completada. Los índices salen de ordenar los objetos por su ruta en la jerarquía, que es igual en todos los computadores.
- Los mensajes que vienen de la red no se vuelven a enviar, para que no reboten.

Las RPC de tiempo real (`RPC_MageState`, `RPC_Paddle`, `RPC_Hit`, `RPC_Stroke`, `RPC_LaneJudged` y los eventos del templo) usan `TickAligned = false`. Por defecto Fusion retrasa una RPC hasta que quien la recibe llega al tick de quien la envió; si un cliente se trababa, sus mensajes llegaban hasta un segundo tarde.

## Quién tiene autoridad

| Objeto | Autoridad de estado | Qué hacen los clientes |
| --- | --- | --- |
| `MageRoster` (sala) | el host | leen el estado replicado y piden cambios por RPC |
| `MageAvatarState` de un jugador | el host, con el jugador como *input authority* | el dueño envía su pose por *input*; los demás la muestran en un títere |
| `MageAvatarState` de un bot | el host | muestran al bot como títere |
| Minotauro (`MinotaurSync`) | el host | muestran la posición interpolada; su IA está apagada |
| Prueba en curso (`TrialDirector`) | el host | piden empezar una prueba por RPC y entran cuando cambia `Session` |
| Carrera de natación (`SwimRaceSync`) | el host | envían sus brazadas y dibujan las distancias |
| Natación artística (`ArtisticSwimmingSync`) | el host decide el inicio de cada figura y la precisión | cada uno juzga su carril y envía sus aciertos |
| Ping pong (`PingPongSync`) | el host | mueven su paleta, piden golpes y dibujan el estado |
| Templo 2D (`TempleSync`) | el dueño de cada mago | simulan su templo, repiten las órdenes de los magos ajenos y comparten los cambios del mundo |
| Llegada a la isla (`IslandArrivalSync`) | el host (acepta solo la primera petición) | piden abrir el portal por RPC y empiezan la cinemática cuando cambia `Opened` |
| Arcos y raquetas (`PickupSync`) | el host guarda qué se recogió | quien maneja el cuerpo lo recoge y avisa; los demás lo muestran sobre el títere |

## Estado replicado (`[Networked]`)

`MageRoster`:

- `Slots` (`NetworkArray<RosterSlot>`, 4 puestos). Cada `RosterSlot` guarda el jugador (`PlayerRef`), el mago elegido (índice), el nombre (`NetworkString<_16>`) y si está listo (`NetworkBool`).
- `HostPlayer`: qué jugador es el anfitrión.
- `MatchStarted`: si la partida ya empezó.

Los cambios se detectan en `Render` con un `ChangeDetector`, y la interfaz (`LobbyPanel`) se actualiza con el evento `Changed`. Al cambiar, cada peer guarda su propio mago en `GameSession.SelectedWizard`, que es lo que usa la escena del juego.

## RPC

| RPC | Dirección | Validación o efecto |
| --- | --- | --- |
| `MageRoster.RPC_SetPlayerName` | cliente → host | el que lo envía tiene puesto y el nombre es válido |
| `MageRoster.RPC_ChooseMage` | cliente → host | el mago existe, nadie más lo tiene y la partida no empezó; quita el Listo |
| `MageRoster.RPC_SetReady` | cliente → host | el que lo envía tiene puesto y un mago válido |
| `MageAvatarState.RPC_Catch` | host → dueño del mago | el minotauro atrapó a ese jugador |
| `TrialDirector.RPC_RequestStart` | cualquiera → host | se acepta solo si no hay otra prueba en curso |
| `SwimRaceSync.RPC_Stroke` | cliente → host | una brazada en ese carril |
| `ArtisticSwimmingSync.RPC_StartFigure` | host → todos | empieza la jugada N |
| `ArtisticSwimmingSync.RPC_FigureResult` | host → todos | precisión de la jugada N |
| `ArtisticSwimmingSync.RPC_LaneJudged` | cualquiera → todos | acierto o trampa de un jugador en su carril |
| `PingPongSync.RPC_Paddle` | cliente → host | altura de la paleta (no confiable) |
| `PingPongSync.RPC_Hit` | cliente → host | golpe a una bola concreta; el host lo valida |
| `TempleSync.RPC_MageState` | dueño → todos | órdenes, posición, velocidad y vida de un mago |
| `TempleSync.RPC_EnemyEliminated`, `RPC_CoinCollected`, `RPC_EventActivated`, `RPC_ZoneUsed`, `RPC_EncounterStarted`, `RPC_CircleLit`, `RPC_DoorCompleted` | cualquiera → todos | un cambio del mundo del templo |
| `IslandArrivalSync.RPC_RequestOpen` | cualquiera → host | se acepta solo la primera vez; marca `Opened` y quién abrió |
| `PickupSync.RPC_ReportPickup` | cualquiera → host | marca el arco o la raqueta de ese mago como recogido |

Las de la sala son `RpcSources.All` → `RpcTargets.StateAuthority`: cualquiera puede enviarlas, pero solo se ejecutan en el host, que sabe quién la envió por `RpcInfo.Source`.

## Input contra RPC contra `[Networked]`

- **Input** (`MagePose`): lo que cada jugador manda en cada tick; Fusion lo entrega en orden y se pierde si llega tarde, igual que una tecla.
- **`[Networked]`**: el estado que todos deben ver igual (la sala, la animación de cada mago, el minotauro). Solo lo escribe quien tiene la autoridad (el host) y Fusion lo replica.
- **RPC**: eventos puntuales que no son estado, como "te atraparon" o "quiero este mago".

## Archivos

| Carpeta | Archivo | Responsabilidad |
| --- | --- | --- |
| `Scripts/Network` | `SessionLauncher` | crear, unirse, salir, empezar la partida y manejar la desconexión |
| | `MageRoster`, `RosterSlot` | estado compartido de la sala |
| | `RoomCodeRules`, `PlayerNameRules` | generar y validar códigos y nombres |
| | `SessionErrorMessages` | mensajes para el jugador según el error |
| | `OnlineSessionSettings` | valores configurables del modo en línea |
| `Scripts/UI` | `OnlineMenuPanel` | panel para escribir el nombre y crear o unirse |
| | `LobbyPanel`, `LobbySlotView` | sala de espera: puestos, elección de mago, Listo, Empezar, Salir |
| | `MagePose`, `MageBody`, `MageAvatarState`, `MatchAvatarSpawner`, `LocalMageInputProvider` | los 4 magos sincronizados en el mundo 3D |
| | `MinotaurSync`, `RemotePlayerPrey` | el minotauro controlado por el host |
| | `OnlineTrial`, `OnlineTrials`, `OnlineMatch`, `TrialDirector`, `TrialSync` | entrar todos juntos a una prueba |
| | `SwimRaceSync` | carrera de natación |
| | `ArtisticSwimmingSync` | natación artística |
| | `PingPongSync`, `PingPongSnapshot` | ping pong |
| | `TempleSync`, `RemoteMageControl`, `MageNetworkState` | templo 2D |
| | `IslandArrivalSync` | portal del laberinto y cinemática de la isla para todos |
| | `PickupSync`, `PickupKind` | arcos y raquetas recogidos |
| `Laberinto/Scripts` | `Minotaur`, `IMinotaurPrey`, `LocalPlayerPrey`, `MinotaurPreyRegistry`, `MinotaurProximitySound` | el minotauro (también en el modo solo) |
| `Scripts/Editor` | `OnlineMenuBuilder` | menú *Juegos Arcanos > 4. Agregar modo en línea a 00_Inicio*: crea los prefabs, los ajustes y los paneles |
| | `OnlineGameSceneBuilder` | menú *Juegos Arcanos > 5. Preparar la escena del juego para el modo en línea*: agrega los componentes de red al minotauro y crea los objetos *Pruebas en línea* y *Mundo en línea* |

## Cómo probar con varios jugadores

- **Dos o más computadores:** hacer un build de Windows. Uno elige *Crear sala*, los demás escriben el código.
- **Un solo computador:** abrir el build varias veces. Cada ventana sigue funcionando sin foco porque *Run In Background* está activado.

## Pendiente

Ya están sincronizados la sala, los 4 magos en el mundo 3D, el minotauro, el portal del laberinto con la cinemática de la isla, los arcos y raquetas, y las cuatro pruebas. Falta:

- Las ceremonias y el medallero: cada computador las muestra por su cuenta con los mismos resultados.
- Qué pasa si un jugador se va a mitad de partida.
