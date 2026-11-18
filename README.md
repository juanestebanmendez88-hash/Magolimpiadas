# Magolimpiadas

Juego multijugador (1 a 4 jugadores) hecho en Unity con **Photon Fusion 2** en topología **Host** (Player Host). Cuatro magos escapan juntos del laberinto del minotauro y compiten en unas olimpiadas mágicas: tiro con arco en un templo 2D, ping pong contra el Guardián del Olimpo, carrera de natación y natación artística. Al final, la ceremonia de clausura muestra el medallero.

## Requisitos

- **Unity 6000.3.27f1** (Unity 6.3 LTS) con el módulo *Windows Build Support*.
- Photon Fusion 2.1.4 ya viene dentro del proyecto (`Assets/Photon`). El App ID está en `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`.
- Conexión a internet para el modo en línea.

## Abrir el proyecto

1. Clonar el repositorio.
2. En Unity Hub: *Add project from disk* y elegir la carpeta del repositorio (la que tiene `Assets`, `Packages` y `ProjectSettings`).
3. Abrirlo con Unity 6000.3.27f1. La primera vez Unity descarga los paquetes de `Packages/manifest.json` e importa los assets (tarda unos minutos).
4. Abrir la escena `Assets/_Magolimpiadas/Escenas/00_Inicio.unity` y dar *Play*.

## Hacer el ejecutable de Windows

1. *File > Build Profiles > Windows*.
2. Comprobar que las escenas estén en este orden: `00_Inicio`, `LaberintoMinotauro`, `99_CeremoniaFinal`.
3. *Build* y elegir una carpeta (por ejemplo `Builds/Windows`). El ejecutable es `Magolimpiadas.exe`.

## Jugar en línea

1. Todos los jugadores usan el mismo build.
2. El anfitrión: *Jugar en línea* → escribe su nombre → *Crear sala* y comparte el código de 4 letras.
3. Los demás: *Jugar en línea* → nombre → escriben el código → *Unirse*.
4. Cada uno elige un mago distinto; los clientes pulsan *¡Listo!* y el anfitrión *Empezar*. Los magos que nadie eligió los maneja el anfitrión como bots.

Para probar en un solo computador se puede abrir el ejecutable varias veces en ventana:

```
Magolimpiadas.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720
```

## Controles

| Dónde | Controles |
|---|---|
| Mundo 3D | WASD / flechas para moverse, Shift para correr, Espacio para saltar |
| Templo 2D | A/D moverse, Espacio o W saltar, clic izquierdo atacar, clic derecho poder secundario, Q cambiar modo, E poder de zona |
| Ping pong | W/S moverse, clic o Espacio para golpear hacia el ratón |
| Carrera de natación | Espacio lo más rápido posible |
| Natación artística | Espacio, J, clic o botón A del mando para tocar las notas de tu carril |

## Documentación

- `Docs/ARCHITECTURE.md`: flujo de conexión, autoridad, estado replicado y RPC del modo en línea.
- `LEEME_PROYECTO.md`: guía del proyecto (escenas, carpetas, scripts y menús del editor).

## Uso de IA

<!-- Sección para que el equipo explique con sus palabras qué herramientas de IA usó, para qué y cómo verificó el resultado. -->

## Equipo

<!-- Nombres de los integrantes. -->
