# Magolimpiadas: guía para continuar el proyecto

Este documento explica cómo está hecho el juego, dónde está cada cosa, qué se ha implementado y qué falta, para quien vaya a terminarlo.

- La especificación del nivel 4 está en `Docs/P5_ArtisticSwimming.md` (con su mapa en `Docs/P5_Mapa_del_nivel.png`).
- El funcionamiento del modo en línea (Photon Fusion 2) está explicado en detalle en `Docs/ARCHITECTURE.md`.

---

## 1. Lo básico

| | |
|---|---|
| Motor | **Unity 6.3 LTS (6000.3.27f1)**, URP, Input System nuevo, TextMeshPro |
| Red | **Photon Fusion 2.1.4**, topología **Host** (Player Host). Está en `Assets/Photon` (se importó con el `.unitypackage` del SDK, no por Package Manager) |
| Paquetes que no pueden faltar | `com.unity.2d.sprite` y `com.unity.2d.tilemap` (los menús del editor cortan sprites con ellos), `com.unity.cloud.gltfast` (importa los modelos `.glb`: arcos, raquetas, mesa de ping pong, antorcha y plataformas rúnicas) y `com.unity.ai.navigation` (la navegación del minotauro). Están en `Packages/manifest.json` |
| Configuración del proyecto | *Allow unsafe code* activado (lo exige Fusion) y *Run In Background* activado (para que el juego siga conectado aunque la ventana no tenga el foco) |
| Escenas que se usan | `Assets/_Magolimpiadas/Escenas/00_Inicio.unity` (menú, elección de mago y sala en línea), `LaberintoMinotauro.unity` (todo el juego) y `99_CeremoniaFinal.unity` (ceremonia de clausura) |
| Build Settings | 0 = 00_Inicio, 1 = LaberintoMinotauro, 2 = 99_CeremoniaFinal |
| Build de Windows | `Builds/Windows/Magolimpiadas.exe` (carpeta fuera de `Assets`; no se sube al repositorio) |
| Idioma del código | **Inglés** en todos los scripts (clases, métodos, variables, namespace `ArcaneGames`). Los textos que ve el jugador, los nombres de objetos de la escena y las carpetas siguen en español |
| Comentarios | El código no tiene comentarios: se quitaron todos (también los `[Tooltip]` y `[Header]`). El equipo los escribe con sus propias palabras |

**Ojo al exportar con `.unitypackage`:** el paquete no lleva los paquetes de Unity (Package Manager). Si se importa en un proyecto nuevo hay que copiar también `Packages/manifest.json` y la carpeta `ProjectSettings`, o el juego no compila, el menú no encuentra las escenas y Fusion no funciona.

**El juego pasa en UNA escena** (`LaberintoMinotauro`), menos la ceremonia final. Los niveles 2D están lejos del mundo 3D y se "entra" a ellos con un fundido y cambiando de cámara:

| Zona | Posición | Cámara |
|---|---|---|
| Mundo 3D (isla, laberintos, podios, piscina) | aprox. x −1200..160 | Main Camera (tercera persona) |
| Templo 2D (nivel 1) | (1000, 1000, 0) | "Camara Mapa 2D" (ortográfica) |
| Coliseo de ping pong 2D (nivel 2) | (2000, 1000, 0) | cámara propia del "Nivel Ping Pong" |
| Piscina de la Luna 2D (nivel 4) | (3000, 1000, 0) | `Cam_P5`, dentro de "Nivel Natación Artística" |

El script que cambia de mundo es `WorldSwitcher` (objeto "Cambio de mundo"): `Enter2D`, `EnterLevel`/`ExitLevel` (ping pong y natación artística), `FinishLevel` (fin del templo).

---

## 2. El recorrido del juego

1. **00_Inicio**: menú principal con **Jugar solo**, **Jugar en línea**, Ajustes y Salir.
   - **Jugar solo**: se elige mago (Mago Elemental, Maga Nebulosa, Druida Verde o Mago del Rayo), se guarda en `GameSession` / PlayerPrefs `selected_wizard`, se reinicia la partida (`NewMatch`) y se carga LaberintoMinotauro. Los otros 3 magos son bots.
   - **Jugar en línea**: el jugador escribe su nombre y **crea una sala** (recibe un código de 4 letras) o **se une** con un código. En la **sala de espera** cada uno elige un mago distinto, los clientes pulsan *¡Listo!* y el anfitrión pulsa *Empezar*; todos cargan LaberintoMinotauro a la vez. Los magos que nadie eligió los maneja el anfitrión como bots. Ver sección 3.
2. **Cinemática de la isla** (`IslandCinematic`): los 4 magos salen de sus portales y encienden el pebetero.
3. **Laberinto del minotauro** con **intro guiada por el búho** (`LabyrinthIntro` + `OwlGuide`). El minotauro (`Minotaur`) patrulla, persigue al que ve y, si lo atrapa, lo devuelve al inicio; si no puede llegar a su presa durante 2 segundos, la deja. Su sonido de cercanía (`MinotaurProximitySound`) solo suena mientras la maga está dentro del laberinto.
4. **Nivel 1 · Tiro con arco**: cada mago recoge **su arco** y el **PortalNivel1** (en la isla) lleva al **templo 2D**. Los poderes explotan con el efecto de su elemento al chocar.
5. **Fin del templo** (`ElementalDoor` → `PodiumCeremony.TempleEnd`): tabla, **medalla épica**, podio pixel y **ceremonia 3D** en `PodioNivel1` (aura para el ganador, pocos fuegos artificiales y aplauso corto). Desde aquí los magos ya no llevan el arco en la espalda (`PodiumCeremony.StowBows`).
6. **Nivel 2 · Ping pong**: raquetas, mesa (`PingPongTable`) y **coliseo 2D** (`PingPongLevel`). Chispas del color de cada mago en cada devolución y estallido en el golpe de poder.
7. **Nivel 3 · Natación** (carrera): la cortina "BloqueoNivel3" solo deja pasar si se ganó el ping pong. Al entrar al agua empieza la carrera (`SwimmingRace`), luego medalla y ceremonia 3D en `PodioNivel3`.
8. **Nivel 4 · Natación artística** (el último): al terminar la carrera el búho avisa; al volver a meterse a la piscina (`ArtisticSwimmingEntrance`) se entra a la **Piscina de la Luna 2D** (`ArtisticSwimmingLevel`). Juego de ritmo cooperativo de 4 figuras con jurado de 7 búhos. Da la medalla de **natación sincronizada**.
9. **Ceremonia final** (escena `99_CeremoniaFinal`, `FinalCeremony`): en la **Isla de la Ceremonia** (modelo `CeremoniaFinal/Modelos/Isla_Ceremonia.fbx`), el **medallero** de todas las pruebas y los 4 magos en los 4 escalones del estrado con sus medallas (la música de la medalla suena una sola vez), unos pocos fuegos artificiales y "¡Ganaron las Magolimpiadas!", con botones para volver al inicio o salir. No se enciende ninguna antorcha. Los materiales del modelo están en `CeremoniaFinal/Materiales`; si se copian sus texturas (`T_*.png`) a `CeremoniaFinal/Modelos/Isla_Ceremonia.fbm/`, el menú Crear escena las usa.
10. Las medallas ganadas se ven arriba a la derecha en el mundo 3D (`PlayerMedals` + `PodiumCeremony.MedalsPanel`). Los puestos de cada mago en cada prueba se guardan en `GameResults` para el medallero final.

**Sonido:** la música cambia sola según la zona (`GameMusic`: isla, laberinto, templo, ping pong, carrera de natación y ceremonias); la natación artística toca su canción de 151 BPM sincronizada con las notas. Los efectos (saltos, pasos, poderes de cada elemento, golpes, puntos de control, minotauro que se acerca y su rugido, caldero, brazadas, golpes de ping pong, medallas, aplausos y ovación) salen del catálogo `GameAudio`. Los sonidos largos (aplausos, ovación, medalla) se reproducen con `GameSound.Celebrate`: suenan unos segundos con un fundido y se callan (`GameSound.SilenceCelebrations`) en cuanto el juego sigue o el jugador se mueve. El audio de la medalla se recortó para que la melodía suene una sola vez.

**Efectos visuales:** pocos y pequeños. En los niveles 2D se usan efectos de pixel art del paquete "RPG Effect" (estallidos, destellos y anillos del color de cada mago; el de error es pequeño y corto); en el 3D, partículas de Hovl solo en las ceremonias (un aura para el ganador y algunos fuegos artificiales).

**Interfaz:** se acomoda a cualquier proporción de pantalla (16:9, 4:3, 21:9). El HUD del templo mueve el cartel de consejos si no cabe y esconde los paneles de abajo mientras habla el búho; el HUD de la natación artística y las pantallas de medallas y ceremonias siempre muestran el área de 1920×1080 completa.

---

## 3. Modo en línea (Photon Fusion 2, modo Host)

Explicación completa, con el flujo de conexión, quién tiene autoridad y qué va por input, `[Networked]` o RPC: **`Docs/ARCHITECTURE.md`**.

| | |
|---|---|
| App ID | `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` (`AppIdFusion`) |
| Región | fija en `us` (mismo archivo, `FixedRegion`): en Photon las salas existen por región; si cada computador eligiera la suya podrían no encontrarse |
| Jugadores | de 1 a 4 por sala; los puestos vacíos los llenan bots del anfitrión |
| Plan de Photon | gratuito: hasta 20 jugadores conectados a la vez |

**Qué está sincronizado hoy:**

1. **Conexión y sala de espera** (`SessionLauncher`, `MageRoster`): crear sala con código, unirse, nombres, elección de mago sin repetir, Listo, Empezar, salir y volver al menú con un aviso si el anfitrión cierra la sala. Errores con mensaje: sala inexistente, sala llena, partida ya empezada, sin conexión.
2. **Los 4 magos en el mundo 3D** (`MageAvatarState`, `MagePose`, `MageBody`, `LocalMageInputProvider`, `MatchAvatarSpawner`): cada jugador maneja su mago como siempre y envía su pose en cada tick; los demás lo ven moverse y animarse (caminar, correr, saltar, antorcha, desaparecer al ser atrapado).
3. **El minotauro** (`MinotaurSync`, `RemotePlayerPrey`): uno solo, controlado por el anfitrión; persigue a cualquiera de los 4 y, si atrapa a un jugador remoto, se lo avisa por RPC.
4. **Las cuatro pruebas, todos juntos** (objeto de red **Pruebas en línea** en la escena del juego: `TrialDirector` y una sincronización por prueba). Cada jugador que llega a la entrada de una prueba espera con la **pantalla del búho volando** (`WaitingScreen`) hasta que lleguen todos los jugadores; el búho avisa a los que faltan dónde los esperan. Las pruebas terminadas (ping pong ganado, templo completado) no se vuelven a jugar. En línea no se puede salir con Esc ni cambiar de mago con Tab.
   - **Templo 2D** (`TempleSync`, `RemoteMageControl`): cada jugador maneja su propio mago; los demás ven sus movimientos, saltos, ataques y flechas. Los enemigos eliminados, monedas, eventos, zonas de poder, encuentros, círculos y la puerta final se comparten.
   - **Ping pong** (`PingPongSync`): el anfitrión mueve las bolas, el Guardián y el marcador; cada jugador mueve su paleta y golpea las bolas de su color desde su computador.
   - **Carrera de natación** (`SwimRaceSync`): cada jugador nada pulsando Espacio; el anfitrión suma las brazadas de todos y decide el orden de llegada.
   - **Natación artística** (`ArtisticSwimmingSync`): cada jugador toca su carril; los aciertos de los demás llegan por la red, el anfitrión marca el inicio de cada figura y la nota del jurado, y al final lleva a todos a la ceremonia final.
5. En línea, el tutorial del templo no pausa el tiempo (la pausa del anfitrión congelaría el mundo de todos).
6. **Portal del laberinto y cinemática de la isla** (`IslandArrivalSync`, objeto de red **Mundo en línea**): quien sale del laberinto espera con la pantalla del búho a que salgan todos; luego los cuatro magos salen de sus portales y encienden el pebetero juntos.
7. **Arcos y raquetas** (`PickupSync`): cada uno recoge los suyos (el anfitrión los de los bots) y los demás ven cómo el arco vuela a la espalda de ese mago y la raqueta a su mano.

**Qué NO está sincronizado todavía** (cada jugador lo ve en su propio computador): las ceremonias del podio y la ceremonia final (cada uno ve la suya, con los mismos resultados). Ver sección 11.

**Cómo se juega con amigos:**

1. Hacer el build (*File > Build Profiles > Windows > Build*, en `Builds/Windows`).
2. Comprimir la carpeta completa del build (sin la subcarpeta `magoss_BurstDebugInformation_DoNotShip`) y pasarla. No necesitan Unity ni cuenta de Photon; todos deben tener el mismo build.
3. Si Windows dice "Windows protegió su PC": *Más información* → *Ejecutar de todas formas*. Si pregunta por el firewall: *Permitir*.
4. El anfitrión: *Jugar en línea* → nombre → *Crear sala* → pasa el código. Los demás: *Jugar en línea* → nombre → código → *Unirse*.
5. Cada uno elige mago, los clientes pulsan *¡Listo!* y el anfitrión *Empezar*.

**Para probar solo en un computador:** abrir el `.exe` varias veces (una ventana crea la sala, las otras se unen). Para que no abra a pantalla completa: `.\Magolimpiadas.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720`. El teclado solo controla la ventana que tiene el foco.

---

## 4. Controles

| Dónde | Controles |
|---|---|
| 3D | WASD / flechas moverse, Shift correr, Espacio saltar |
| Templo 2D | A/D moverse, Espacio o W saltar, Shift correr, clic izq. atacar, clic der. poder secundario, Q cambiar modo, E poder de zona, **Tab** cambiar de mago, Esc volver al 3D |
| Ping pong | W/S moverse, Shift correr, clic o Espacio golpear hacia el ratón, Tab cambiar de capitán |
| Natación (carrera) | Espacio (lo más rápido posible) |
| Natación artística | **Espacio, J, clic o botón A del mando**: tocar las notas de tu carril. Tab cambiar de mago. Esc volver al 3D |
| Tutoriales, intros y ceremonias | clic / Espacio para avanzar, Esc para saltar |

---

## 5. Carpetas

```
Assets/
  _Magolimpiadas/              ← TODO lo nuestro
    Escenas/                   00_Inicio, LaberintoMinotauro y 99_CeremoniaFinal
    Arte/  Fuentes/            UI (FantasyUI, Iconos), fuentes TMP (Fuentes/TMP; Fuentes/Otras)
    Audio/                     Musica/ (una pista por zona), SFX/, Resources/AudiosJuego.asset, GameAudio, GameMusic, Editor/
    Network/                   Prefabs/ (MageRoster, MageAvatar) y Resources/OnlineSessionSettings.asset
    Scripts/                   menú (MainMenuController, CharacterSelectPanel...), GameSession, NewMatch
      Network/                 modo en línea: SessionLauncher, MageRoster, MageAvatarState, MinotaurSync...
      UI/                      paneles del menú en línea: OnlineMenuPanel, LobbyPanel, LobbySlotView
      Sonido/                  SoundSynthesizer, GameSound, CelebrationSounds
      Comun/                   PixelShapes (formas de pixel art dibujadas con código)
      Editor/                  menús del proyecto (MenuSceneBuilder, OnlineMenuBuilder, OnlineGameSceneBuilder, SortProject, PrepareGameEnding)
    Efectos/                   MagicEffects (catálogo de partículas y de efectos 2D), Sprites2D/ (hojas de "RPG Effect"), Resources/EfectosMagicos.asset
    Laberinto/                 mundo 3D: jugador, bots, minotauro, búho, intro, ceremonias, natación, medallas, resultados
      Cortina de runas/        la cortina que venía de Ancient Village
    Isla/                      isla mágica, kit de props (podio, pebetero, barandas...), constructores
    Templo/                    nivel 1 (2D): tiles, personajes (incluye sprites de nadar), enemigos, scripts
    PingPong/                  nivel 2: sprites, raquetas, shader gris, Modelos/ (raqueta, mesa)
    NatacionArtistica/         nivel 4: Scripts/, Editor/, Datos/ (ajustes y rutina), Arte/ (fondo, búhos jueces, formas generadas)
    CeremoniaFinal/            Scripts/, Editor/, Modelos/ (Isla_Ceremonia.fbx), Materiales/, Arte/ (degradado del título), Prefabs/ (los 4 magos)
    Podio/                     sprites de podio, celebración/derrota y medallas (arco, ping pong, natación y sincronizado)
    Magos3D/                   Modelos/ (los 4 FBX + antorcha), Animaciones/ (controladores, Mixamo/), Materiales/, Texturas/, Maga Nebulosoa/
    Minotauro/
    Cielo/                     cielos Cold Night y Glorious Pink
  Photon/                      SDK de Photon Fusion 2 (no tocar; el App ID está en Fusion/Resources)
  Paquetes externos/           Hovl Studio, Reversed Interactive (portales), Eric VFX Studio, portalefectos y mas
  Settings/  TextMesh Pro/     de Unity: no mover
  InputSystem_Actions.inputactions
Builds/                        build de Windows (no se sube al repositorio)
Docs/                          especificación y mapa del nivel 4, ARCHITECTURE.md (modo en línea)
```

Se borraron Ancient Village (menos la cortina de runas) y lo que no se usaba de los paquetes (~650 MB): escenas de demostración, texturas y modelos sueltos, el HDRI y los ajustes HDRP de Reversed Interactive, y las texturas de 6 caras de los cielos (se usan las versiones Equirect).

**Si se mueve una carpeta:** muévela desde Unity (no desde el explorador de Windows) y busca la ruta vieja en los scripts de `Editor/`. Muchos menús cargan assets por ruta, por ejemplo `"Assets/_Magolimpiadas/Templo/..."`.

---

## 6. La jerarquía de la escena LaberintoMinotauro

| Grupo | Qué tiene |
|---|---|
| — Sistema — | luz, Main Camera, volumen, Cambio de mundo, Cielo por zona, Mago elegido, Ceremonia del podio, interfaces, EventSystem |
| — Isla — | fondo, cascadas, puerta, caminos sueltos, Barandas de los caminos |
| — Nivel 1 · Arco — | título "Tiro con arco" |
| — Nivel 2 · Ping pong — | PodioNivel1, punto de inicio del nivel 2, mesa, 4 raquetas, decoración, **Nivel Ping Pong** (coliseo 2D) |
| — Nivel 3 · Natación — | Piscina, "Natación (nivel 3)", **"Natación artística (nivel 4)"** (entrada al nivel 4, mismo agua), PodioNivel3, título |
| Minotauro | `Minotaur` + navegación; además `NetworkObject` y `MinotaurSync` para el modo en línea (en el modo solo no hacen nada) |
| (raíz) | **Nivel Natación Artística** (el nivel 4 en 2D), **Audio del juego** y **Teleport de prueba (a la piscina)** |

Se borraron los dos cubos de prueba que había junto a la entrada del laberinto ("Teleport de prueba (a las raquetas)" y "Entrada Templo 2D (cubo)"). El templo se sigue entrando por `Portales/PortalNivel1` en la isla.

"Nivel Natación Artística" usa los nombres de `Docs/P5_ArtisticSwimming.md`: `Mural_P5` (con `Background`, `MuralSpawn_1`..`4`, `DivingBoard`, `Formaciones/Formation_Line|Diamond|Square|Flower_1..4`, `Nadadores`, `Carriles/NoteLane_1..4` y `HitLine`, `JuryBox/Owl_1..7`, `Efectos`), `Cam_P5` y `HUD Natación`.

---

## 7. Scripts más importantes

**Menú y modo en línea** (`_Magolimpiadas/Scripts`, namespace `ArcaneGames`):

| Script | Para qué |
|---|---|
| `MainMenuController` | cambia entre los paneles: principal, ajustes, elegir mago, en línea y sala de espera |
| `Network/SessionLauncher` | crea el `NetworkRunner`, crea la sala (Host) o se une (Client), empieza la partida, maneja errores y desconexiones |
| `Network/MageRoster`, `RosterSlot` | estado compartido de la sala (jugadores, magos, Listo) y sus RPC |
| `Network/RoomCodeRules`, `PlayerNameRules`, `SessionErrorMessages`, `OnlineSessionSettings` | validaciones, mensajes y ajustes del modo en línea |
| `Network/MageAvatarState`, `MagePose`, `MageBody`, `LocalMageInputProvider`, `MatchAvatarSpawner` | los 4 magos sincronizados en el mundo 3D |
| `Network/MinotaurSync`, `RemotePlayerPrey` | el minotauro controlado por el anfitrión |
| `Network/TrialDirector`, `TrialSync`, `OnlineTrials`, `OnlineTrial`, `OnlineMatch` | entrar todos juntos a una prueba |
| `Network/TempleSync`, `RemoteMageControl`, `MageNetworkState` | templo 2D en red |
| `Network/PingPongSync`, `PingPongSnapshot` | ping pong en red |
| `Network/SwimRaceSync` | carrera de natación en red |
| `Network/ArtisticSwimmingSync` | natación artística en red |
| `UI/OnlineMenuPanel`, `LobbyPanel`, `LobbySlotView` | interfaz de jugar en línea y sala de espera |

**Mundo 3D** (`_Magolimpiadas/Laberinto/Scripts`): `PlayerMage`, `BotMage3D`, `ChosenMage`, **`Minotaur`** (con `IMinotaurPrey`, `LocalPlayerPrey`, `MinotaurPreyRegistry`, `MinotaurProximitySound`), `Collectible`, `TrialGate`, `OwlNotice`, `OwlGuide`, `LabyrinthIntro`, `IslandCinematic`, `ZoneSky`, `ZoneTitle`, `PodiumCeremony`, `PlayerMedals`, `GameResults`, `FixedHead`, `SwimmingRace`, `TestTeleport`.

**Natación artística** (`_Magolimpiadas/NatacionArtistica/Scripts`), equivalencias con el documento:

| Script | En el documento | Para qué |
|---|---|---|
| `ArtisticSwimmingLevel` | SwimmingTrialFlowController | entrar/salir, intro, figuras, jurado, reintentos, medalla y paso a la ceremonia |
| `SwimmingRoutine` | RoutineChart | canción, BPM y las 4 figuras con sus notas (en pulsos) |
| `RoutineNote`, `NoteType`, `NoteJudgment`, `Formation` | RoutineNote, NoteType, HitJudgement | tipos (`TiposNatacion.cs`) |
| `SwimmingSettings` | SwimmingSettings | ventanas, puntos, umbrales, jurado, medallas y acierto de los bots |
| `RoutineClock` | RoutineClock | pulso actual a partir del reloj del audio |
| `NotesJudge` | NoteJudge | juzga pulsaciones, bots, blancas (los 4), doradas y trampas |
| `SwimmingScoreboard` | SwimmingScoreBoard | puntos, combo, estrellas y precisión de la figura |
| `NoteLanes` | NoteLanePresenter | dibuja las notas bajando |
| `SwimmingEffects` | HitFeedbackPresenter | partículas, sonidos y textos de cada juicio |
| `SwimmersFormation` | FormationPositioner | los 4 magos: trampolín, nado, formaciones, flor, olas y saltos |
| `OwlsJury` | JuryScorePresenter | los 7 búhos con sus carteles |
| `SwimmingHud`, `SwimmingMusic`, `ArtisticSwimmingEntrance` | — | interfaz, música y entrada desde la piscina 3D |
| `RoutineGenerator` (Editor) | RoutineChartValidator | genera la rutina de prueba y la valida |

**Ceremonia final** (`_Magolimpiadas/CeremoniaFinal/Scripts`): `FinalCeremony`.

**Efectos y sonido**: `MagicEffects` (partículas: `MagicEffects.Launch(Effect.FireImpact, posición, escala)`; 2D: `MagicEffects.Launch2D(Effect2D.Burst, Color2D.Orange, posición, escala)`), `EffectAnimation2D`, `GameAudio` (catálogo: `GameSound.Play(GameAudio.Sound.Jump)`), `GameMusic` (música por zonas), `GameSound`, `CelebrationSounds` (sonidos largos con fundido) y `SoundSynthesizer` (respaldo hecho con código).

**Templo 2D**: **`TempleHud`** es la interfaz con imágenes (retrato, vida, modo Q Arco/Poder, acción de la tecla E, consejos, monedas, Tab y Esc). Los textos viejos (`MageTextIndicator`, `CoinCounter`, aviso de Esc) siguen en la escena pero apagados.

---

## 8. Menús del editor

⚠️ Algunos **borran y vuelven a crear** objetos. Haz una copia o commit antes de probarlos.

**Barra de arriba → Magolimpiadas:**

| Menú | Qué hace | ¿Borra? |
|---|---|---|
| **Preparar nivel 4 y ceremonia final** | efectos + audio + música por zonas + natación artística + interfaz del templo + escena 99_CeremoniaFinal, y guarda | rehace "Nivel Natación Artística", "HUD Templo" y la escena 99 |
| Audio > Preparar catálogo y música | importación de los audios, catálogo AudiosJuego y "Música del juego" en la escena | no (respeta los volúmenes que ajustes) |
| Templo 2D > Interfaz con imágenes | crea "HUD Templo" y apaga los textos viejos | rehace "HUD Templo" |
| Natación artística > Preparar nivel | sprites, datos y el nivel en (3000, 1000, 0), la entrada desde la piscina | el nivel solo si se confirma |
| Natación artística > Generar rutina de prueba | rutina inicial a partir del BPM | **sí, las notas de la rutina** |
| Natación artística > Validar rutina | revisa carriles, notas encimadas y notas fuera de la figura | no |
| Ceremonia final > Crear escena | usa el modelo Isla_Ceremonia.fbx (materiales y escalones del estrado), copia los magos y arma 99_CeremoniaFinal | **sí, la escena 99** |
| Efectos > Preparar catálogo | busca los prefabs de partículas por nombre y llena el catálogo | no |
| Proyecto > Ordenar carpetas | mueve todo a la estructura de esta guía (desde Unity) | no |
| Isla Mágica, Laberintos, Guía Búho, Templo 2D, Ping Pong, Ceremonias y natación, Tools > Templo > Importar mapa | igual que antes (el de Ping Pong ya no crea el cubo de prueba de las raquetas) | ver cada menú |

**Barra de arriba → Juegos Arcanos:**

| Menú | Qué hace | ¿Borra? |
|---|---|---|
| 2. Crear Escena 00_Inicio | rehace todo el menú (incluye los paneles en línea) | **sí, la escena 00_Inicio** |
| 4. Agregar modo en línea a 00_Inicio | crea los prefabs y ajustes de red, agrega "Jugar en línea" y los paneles en línea y de sala | rehace solo "OnlinePanel" y "LobbyPanel" |
| 5. Preparar la escena del juego para el modo en línea | agrega `NetworkObject` y `MinotaurSync` al minotauro y crea el objeto "Pruebas en línea" con las sincronizaciones de las pruebas | no |

---

## 9. Cómo probar rápido

- **Play directo en LaberintoMinotauro** usa el último mago elegido ("Mago De Prueba" en el objeto "Mago elegido" fuerza uno). Así se juega en modo solo.
- **Modo en línea**: con el build, abrir el `.exe` varias veces (ver sección 3). Dentro del editor se prueba como un jugador más.
- **Nivel 4 sin jugar todo**: en Play, clic derecho sobre el componente `ArtisticSwimmingLevel` (objeto "Nivel Natación Artística") → **Probar: entrar al nivel**.
- **Jugar solo** en la natación artística: casilla en `ArtisticSwimmingLevel`; tu carril también se toca solo (para probar o grabar el video).
- **Ceremonia final**: abrir `99_CeremoniaFinal` y dar Play; si no hay resultados, usa medallas de prueba.
- **Teleport de prueba** (cubo morado en la isla): lleva al borde de la piscina.
- En Play, Unity solo avanza si su ventana está al frente.

---

## 10. Lo que se ha hecho (resumen)

**Para que el juego corra:** se agregaron los paquetes que faltaban (2D Sprite, 2D Tilemap, glTFast) y las escenas en Build Settings.

**Contenido:**
- Nivel 4 completo (natación artística) con su jurado de búhos, rutina, medallas y paso a la ceremonia final.
- Ceremonia final en la Isla de la Ceremonia, con medallero y estrado de 4 escalones.
- Catálogo de audios integrado (música por zonas y efectos) y efectos visuales 2D y 3D.

**Correcciones de juego:**
- Los aplausos, la ovación y la medalla suenan poco y se callan cuando el juego sigue; la melodía de la medalla suena una sola vez.
- El sonido del minotauro solo suena dentro del laberinto.
- Después del nivel 1 los magos ya no llevan el arco.
- Ya no se enciende ninguna antorcha al final.
- El minotauro ya no se queda quieto después de saltar la intro, ni trabado persiguiendo a alguien al que no puede llegar.

**Traspaso visual del compañero (18 de noviembre):** partículas pixel art en el templo (`LevelEvent.Particles(ParticleShape...)`, `LevelEvent.Sparkles`, imágenes en `Templo/Resources/Particulas`); humo al hundirse la puerta elemental; barras de vida del Dragón y del Cristal abajo en el centro (`BossHealthBar`, dentro de "HUD Templo"); consejos del templo en medio de la pantalla y casilla activa sin puntitos; ícono fijo del arco; fuente secundaria Kingthings Petrock (`F_Texto`); ayuda de controles al terminar la intro (`ControlsHelp`, objeto "Ayuda de controles"); estrellas del ganador del color de su mago (`MagicEffects.Tint`); volúmenes de la música igualados; texturas, nubes e islotes en la isla de la ceremonia final; y la explicación del hielo del Mago Elemental. No se trajeron las herramientas de editor "Crear barra en la escena" ni "Medir volumen de la música" (las barras ya vienen hechas en el HUD).

**Interfaz:** los HUD y las pantallas de medallas y ceremonias ya no se enciman en ninguna proporción de pantalla.

**Limpieza:** se ordenó el proyecto y se liberaron ~650 MB. Se quitaron los comentarios, `[Tooltip]` y `[Header]` del código. Se borraron los dos cubos de prueba de la entrada del laberinto.

**Modo en línea:** menú con jugar solo / en línea, sala con código, elección de mago, los 4 magos y el minotauro sincronizados en el mundo 3D, y las cuatro pruebas jugadas por los 4 jugadores a la vez (sección 3). Se probó con 4 copias del build en el mismo equipo: en la carrera y la natación artística los 4 ven los mismos tiempos, aciertos y notas; en el ping pong el mismo marcador; en el templo cada mago se ve a unos 0,1 de distancia de donde está en el computador de su dueño.

---

## 11. Lo que falta

### Multijugador (lo que más pesa en la nota)

La rúbrica pide que todos los jugadores interactúen de forma significativa. Ya caminan juntos, los persigue el mismo minotauro y juegan juntos las cuatro pruebas. Falta:

1. **Ceremonias del podio** y **ceremonia final** en red (hoy cada uno ve la suya con los mismos resultados), y volver al menú cerrando la sala.
2. Decidir qué pasa si un jugador se va a mitad de partida (hoy: si se va el anfitrión, todos vuelven al menú; si se va un cliente, no está tratado; lo lógico es que su mago pase a ser bot).
3. Probar con 4 computadores distintos y redes distintas (hasta ahora se probó con 4 copias en el mismo equipo).

### Penalizaciones del curso por revisar antes de entregar

| Falta | Estado | Qué hacer |
|---|---|---|
| Código en español (-8) | **resuelto**: todos los scripts usan nombres en inglés (117 archivos renombrados; los datos de escenas, prefabs y assets se conservaron) | revisar nombres cortos poco claros (`t`, `p`, `b`...) en los scripts viejos |
| Comentarios de IA (-30) | el código está sin comentarios | el equipo escribe sus propios comentarios (el curso pide comentarios claros y consistentes) |
| Elementos sin usar y `using` sobrantes (-8 / -12) | **revisado**: se quitaron 5 `using` sobrantes, un campo y dos variables sin usar (verificado compilando) | siguen en la escena los textos viejos apagados del templo (`MageTextIndicator`, `CoinCounter`) y el cubo de prueba |
| Valores fijos en el código (-8) | hay muchos números escritos directamente en los scripts viejos | pasarlos a constantes con nombre o a ajustes (`ScriptableObject`) |
| Figuras básicas sin textura (rúbrica) | el fondo del foso ya tiene textura de roca; quedan el cubo "Teleport de prueba (a la piscina)" y los marcadores transparentes de las zonas (quads de color) | quitar el cubo de prueba antes del build final; los marcadores se pueden defender como brillo intencional o texturizar |
| Assets generados por IA (rúbrica: deja en 0 el diseño visual y el seguimiento de instrucciones) | `Magos3D/Maga Nebulosoa/Meshy_AI_Moonspell_Mage_..._texture.png` y `Minotauro/Meshy_AI_Emberhorn_Warlord_..._texture.png`: Meshy es un generador de modelos 3D con IA | **reemplazar** esos modelos y texturas (y confirmar el origen de los otros magos) por assets no generados con IA |
| Laberintos (rúbrica) | el juego empieza en un laberinto | el equipo decidió mantenerlo; poder defender que no es la mecánica central |

### Otros pendientes

- **Rutina de la natación artística:** usa `Mus_NatacionArtistica_151BPM` (151 BPM; el primer pulso se midió en 0,296 s y cada figura usa su propia sección de la canción). Las notas son una rutina inicial generada: ajustarlas a mano para que sigan la melodía (Validar rutina para revisarla). Si en algún computador las notas se sienten adelantadas o atrasadas respecto al sonido, mover `audioLatency` en `NatacionArtistica/Datos/AjustesNatacion`.
- **Texturas de la isla de la ceremonia:** el FBX las busca en una carpeta `Isla_Ceremonia.fbm` que no se subió; hoy se ve con los colores de sus materiales.
- **Volúmenes:** cada sonido y cada pista tiene su volumen en `Audio/Resources/AudiosJuego`.
- **Ícono de vida:** el corazón de la interfaz del templo es un pixel art provisional dibujado por código (`Arte/UI/Iconos/Corazon_Pixel.png`); se puede reemplazar por uno del equipo.
- **No hay camino de la arena del ping pong a la piscina**: hoy se llega con el teleport de prueba.
- Templo: faltan colliders en las flechas del pilar.
- En la escena hay 3 efectos de partículas ("Flares", "Crystal effect blue", "Glow") a los que les falta un material o malla.
- Probar una partida completa de principio a fin con cada uno de los 4 magos (modo solo).

### Entregables

1. **Repositorio en GitHub** (todavía no creado; ver sección 12).
2. **README.md** con instrucciones de ejecución y una sección "Uso de IA" escrita por el equipo.
3. **Build de Windows** funcional (se rehace al final).
4. **Video de máximo 3 minutos** con los 4 jugadores interactuando en el mismo entorno (hacen falta 4 personas o 4 computadores).
5. Poder **explicar el flujo de red** frente al profesor: `Docs/ARCHITECTURE.md`.

---

## 12. Repositorio en GitHub (pendiente)

El curso no acepta commits hechos por IA: los commits y el push los hace el equipo desde su propia terminal.

1. Agregar un `.gitignore` de Unity que excluya `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`, `*.csproj`, `*.sln` y `*_BurstDebugInformation_DoNotShip/`.
2. Revisar que ningún archivo pese más de 100 MB (límite de GitHub); si alguno lo pasa, usar Git LFS.
3. Abrir PowerShell **en la carpeta del proyecto** (la que tiene `Assets`, `Packages` y `ProjectSettings`): el proyecto debe quedar en la raíz del repositorio, no en una subcarpeta.
4. Revisar la identidad: `git config user.name` y `git config user.email`.
5. `git init -b main`, `git add .` y `git commit -m "Magolimpiadas: proyecto base con modo en línea"`.
6. `gh repo create Magolimpiadas --public --source . --remote origin --push --description "Juego multijugador de magos con Photon Fusion 2"`.
7. Confirmar que el repositorio es **público** (o compartido con el docente).
8. Desde ahí, commits pequeños con mensajes que digan qué cambió (nada de "cambios" o "fix").

El App ID de Photon queda en el repositorio. No es secreto (va dentro de todo build), pero cualquiera podría usar el cupo gratuito; se puede regenerar en el panel de Photon al terminar el curso.

---

## 13. Reglas para no romper el proyecto

1. No borrar ni recrear objetos que ya están en la escena. Lo nuevo se añade como objeto nuevo.
2. Antes de usar un menú que "rehace", hacer una copia o commit.
3. Después de tocar un script, mirar la consola: no puede haber errores rojos (`error CS`), o Unity no deja dar Play.
4. Mover o renombrar assets **desde Unity**, nunca desde el explorador (se perderían las referencias).
5. Si algo se rompe: volver a la última copia en lugar de arreglarlo encima.
6. Código nuevo: nombres en inglés, sin emojis, sin `using` ni miembros sin usar.
7. En línea no se pausa con `Time.timeScale = 0`: congelaría el mundo de todos los jugadores.
