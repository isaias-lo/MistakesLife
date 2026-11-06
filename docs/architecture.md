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
- Avanza el mundo **de a una semana**, siempre en el mismo orden:
  1. Decisiones del jugador
  2. Sistemas (economía, salud, estudios, relaciones, carreras…)
  3. Personajes (cercanos completos, conocidos resumidos, población como estadística)
  4. Semillas (cuáles despiertan, con límite por mes)
  5. Eventos (según condiciones y peso; un "día clave" detiene el tiempo)
  6. Consecuencias (efectos, semillas nuevas, diario)
- Cada sistema es un **módulo independiente** que participa en ese ciclo.
- **Azar reproducible:** cada partida tiene una semilla de azar; con ella se repite exactamente una vida.

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
