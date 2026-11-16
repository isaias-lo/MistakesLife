# Arquitectura

Resumen del documento técnico, para tenerlo junto al código.

## Regla de oro

Las **reglas de la vida** viven solo en el núcleo (`core/`). El **contenido** vive solo en los paquetes (`packs/`). Unity (`unity/`) solo dibuja.

```
packs/ (JSON)  ──lee──▶  core/ (C# puro)  ◀──usa──  unity/ (gráficos)
                              ▲
                              └──usa──  tools/ (consola, simulador, validador)
```

## El núcleo

- C# puro, `netstandard2.1` y C# 9 (lo que Unity soporta). Sin ninguna referencia a Unity.
- Avanza el mundo **de a una semana**, siempre en el mismo orden (en pantalla el jugador avanza un mes o un año, que son varias semanas seguidas que se detienen en un momento):
  1. Decisiones del jugador
  2. Sistemas (economía, salud, estudios, relaciones, carreras…)
  3. Personajes (cercanos completos, conocidos resumidos, población como estadística)
  4. Semillas (cuáles despiertan, con límite por mes)
  5. Eventos (según condiciones y peso; un "día clave" detiene el tiempo)
  6. Consecuencias (efectos, semillas nuevas, diario)
- Cada sistema es un **módulo independiente** que participa en ese ciclo.
- **Azar reproducible:** cada partida tiene una semilla de azar; con ella se repite exactamente una vida.

## Contenido desde una base de datos (arquitectura C, decidida el 16-11-2026)

Decisión definitiva de Isaias, pensando en 20 a 100 países. Lo que no cambia: el núcleo en C# puro, Unity solo dibuja, el azar reproducible, los ids estables y el juego completo sin internet. Lo que cambia es **cómo se escribe el contenido**, no cómo se lee.

```
base de datos ──▶ página web con formularios ──▶ paquetes (JSON, mismo formato de packs/)
                                                     │
                       juego ◀── descarga una vez ───┘  (después juega sin internet)
```

1. **Base de datos** con todo el contenido: países, comunas, carreras, eventos, precios, leyes, bancos.
2. **Página web con formularios** para llenar y revisar esos datos sin programar. Valida lo mismo que hoy valida la carga de paquetes.
3. La página **genera los paquetes** con el formato que ya lee el núcleo (`docs/content-language.md`). Mientras no exista, los paquetes se siguen escribiendo a mano en `packs/`.
4. **El juego descarga** los paquetes que le faltan y los guarda en el celular; después juega sin internet.
5. **Países nuevos y casas 3D llegan como descargas**, que también pueden ser expansiones pagadas.

Cada país es un paquete con la misma forma y datos propios: 4 lugares (2 de clase baja, 1 media, 1 alta), 9 viviendas en 3D, escala de notas, sistema de salud, puntaje bancario, 3 bancos, leyes, precios y salarios con su fuente. Detalle del juego en `docs/diseno-juego.md`.

## Guardado

- El núcleo convierte una partida completa en texto (JSON) o en bytes comprimidos, y de vuelta: `LifeSession.SaveToText()`, `SaveToBytes()`, `LifeSession.Load(content, texto)` y `LoadFromBytes(content, bytes)`. No escribe archivos: Unity (o la consola) decide dónde guardarla.
- Se guarda todo lo que cambia durante la vida: fecha, azar, personajes, semillas, economía, leyes cambiadas, diario y eventos. Lo que viene de los paquetes (eventos, condiciones de semillas, textos) no se guarda: se vuelve a leer del paquete, así un arreglo en un paquete llega también a las partidas ya empezadas.
- Una partida cargada sigue viviendo exactamente igual que si nunca se hubiera guardado (hay pruebas que lo comprueban).
- Cada guardado dice su versión (`CoreInfo.SaveFormatVersion`). Las partidas viejas se convierten con los pasos de `Saving/SaveMigrations.cs`; una partida de una versión más nueva del juego da un error claro que pide actualizar.
- Más adelante: SQLite dentro del celular para consultas rápidas y respaldo en la nube. Detalles en el documento técnico.

## Volver al pasado

- `LifeSession.TimeMachine` saca fotos del mundo (la partida completa de ese momento, comprimida): una al empezar cada etapa de la vida (`LifeStages`: infancia 0, niñez 6, adolescencia 12, juventud 18, adultez 30, madurez 45 y vejez 65 años) y otra antes de cada decisión de día clave (se guardan las 3 más recientes).
- `LifeSession.GoBack(id)` vuelve a una foto: el mundo de la partida pasa a ser el de ese momento y las fotos posteriores se borran. Unity debe pedir siempre `session.World`, nunca guardarlo aparte.
- Reglas para que las decisiones sigan pesando: cada vuelta gasta una carga (`Charges`; una partida nueva trae `TimeMachine.StartingCharges` y las compras suman con `AddCharges`), y cada etapa de la vida se vuelve a vivir una sola vez por generación (`CanGoBackTo`). En el legado, el hijo empieza sin fotos ni etapas usadas, con las mismas cargas.
- El jugador conserva una intuición: `diario.intuicion` en su diario y, si volvió antes de una decisión, `diario.intuicion_decision` y la opción que eligió la otra vez en `TimeMachine.PreviousChoice(pendiente)`, para que la pantalla la marque.
- Las fotos se guardan con la partida (formato 5). El simulador de vidas apaga la máquina (`TimeMachine.Enabled = false`) para ir rápido.
