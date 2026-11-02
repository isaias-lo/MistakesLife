# Reglas para agentes de IA en MistakesLife

MistakesLife es un simulador de vida para celulares, hecho en Unity, que busca ser una "réplica de la vida real": semana a semana, con decisiones cuyas consecuencias aparecen años después. El dueño del proyecto (Isaias) dirige y revisa; los agentes programan. **Isaias tiene poca experiencia en C# y Unity**: todo lo que entregues debe poder entenderse leyendo el resumen del PR.

Fuentes de verdad (léelas antes de decisiones de diseño):
- Diseño: https://claude.ai/artifact/TLyFxfHXuytpZFXHUHY3B9
- Técnico: https://claude.ai/artifact/Rv74yh2pBA6kTHmLLQ7nSL
- Resumen local: `docs/architecture.md` y `docs/roadmap.md`
- Formato de eventos, semillas y lenguaje de condiciones: `docs/content-language.md` (actualízalo si agregas datos al lenguaje)

## Mapa del repositorio

| Carpeta | Qué es |
|---|---|
| `core/MistakesLife.Core/` | Núcleo de simulación en C# puro (`netstandard2.1`, C# 9) |
| `core/MistakesLife.Core.Tests/` | Pruebas del núcleo (xUnit, `net8.0`) |
| `packs/` | Paquetes de contenido en JSON (base, países, carreras, ajustes, extras) |
| `tools/` | Consola de juego, simulador de vidas, validador (usan el núcleo) |
| `unity/` | Proyecto de Unity (solo gráficos e interfaz) |
| `docs/` | Documentos que viven junto al código |

## Reglas de arquitectura (no negociables)

1. **El núcleo no conoce Unity.** Nada de `using UnityEngine` ni paquetes gráficos en `core/`. Solo `netstandard2.1` y C# 9: no uses características de C# 10+ (file-scoped namespaces, `record struct`, `required`, global usings, etc.).
2. **Las reglas de la vida viven en el núcleo; el contenido vive en los paquetes.** Si algo es propio de un país, una carrera o un evento, va en JSON, no en código. El código solo sabe *leer* y *aplicar* paquetes.
3. **Unity solo dibuja.** Ninguna lógica de simulación en `unity/`.
4. **Ciclo semanal fijo:** decisiones → sistemas → personajes → semillas → eventos → consecuencias. Cada sistema es un módulo independiente; agregar uno no obliga a tocar los demás.
5. **Azar reproducible.** Nunca uses `System.Random` suelto, `Guid.NewGuid()` ni `DateTime.Now` dentro de la simulación. Todo el azar sale del generador de la partida (a partir de su semilla), para que una vida se pueda repetir exacta.
6. **Identificadores estables.** Todo contenido tiene un id en minúsculas con puntos (`carrera.software.area.ciberseguridad`). Un id publicado nunca se renombra ni se reutiliza.
7. **Sin textos para el jugador en código ni en paquetes.** Usa claves (`opcion.reconocer_error`); las frases van en tablas de idioma. Idiomas de inicio: español (variantes de Chile y Argentina), inglés, portugués de Brasil.
8. **Guardado versionado.** Si cambias cómo se guarda una partida, sube `CoreInfo.SaveFormatVersion` y agrega su migración.
9. **Primero local, después nube.** El juego funciona completo sin internet.

## Convenciones de código

- Identificadores de C# en inglés (`Character`, `WeekCycle`); comentarios y documentación en español.
- Ids y claves de contenido en español, como en los documentos (`carrera.`, `evento.`, `semilla.`, `dilema.`).
- Un tipo público por archivo; nombre de archivo = nombre del tipo.
- `Nullable` activado y advertencias tratadas como errores.
- Prefiere código simple y legible a código ingenioso.

## Pruebas (obligatorias)

- Todo cambio en `core/` trae sus pruebas en `core/MistakesLife.Core.Tests/`.
- Nombres de pruebas que describan el comportamiento: `Semilla_ConTestigoEnEmpresa_DespiertaEnEntrevista`.
- Para verificar antes de proponer un cambio:

```bash
dotnet test MistakesLife.sln
```

- Todo JSON en `packs/` debe ser válido (la integración continua lo revisa).

## Cómo entregar el trabajo

- Una tarea = una rama = un pull request pequeño, con un criterio de éxito comprobable.
- El resumen del PR va **en español y en lenguaje simple**: qué cambió, por qué, cómo se comprueba y qué debe hacer Isaias (si algo).
- Actualiza `docs/roadmap.md` cuando una tarea avanza.
- No mezcles en un PR cambios que no pidió la tarea.
