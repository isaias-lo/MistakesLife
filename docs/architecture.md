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

SQLite dentro del celular, con versión de formato y migraciones. El legado sigue en el mismo mundo. Detalles en el documento técnico.
