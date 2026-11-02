# Paquetes de contenido

Todo lo que hace distinto a un país, una carrera o un evento vive aquí como **datos en JSON**, nunca como código. El núcleo los lee al arrancar.

| Carpeta | Tipo | Ejemplo |
|---|---|---|
| `base/` | Reglas comunes a todo el mundo (estadísticas, rasgos, tipos de relación) | Los cinco grandes rasgos |
| `countries/<país>/` | Ciudades, barrios, moneda, leyes, colegios, trámites, eventos históricos | `countries/chile/` |
| `careers/<carrera>/` | Títulos, áreas, cargos, eventos, dilemas, minijuegos | `careers/software/` |
| `overrides/<carrera>.<país>/` | Lo que una carrera cambia en un país (ajuste local) | Contabilidad en Argentina |
| `extras/<nombre>/` | Apps del teléfono, eventos de temporada, logros | La app Flechazo |

Cada paquete tiene un `pack.json` con su `id`, `type`, `version` y `dependencies`.

## Reglas

- **Identificadores estables:** cada cosa tiene un id único (`carrera.software.area.ciberseguridad`) que nunca cambia, aunque cambie el texto que ve el jugador.
- **Sin textos para el jugador dentro del JSON:** los paquetes usan claves de texto (`opcion.reconocer_error`); las frases van en las tablas de idioma.
- **Los paquetes se combinan:** una carrera no repite datos del país, solo declara lo que ajusta.
- **Todo JSON debe ser válido:** la integración continua lo revisa en cada cambio.

> El formato completo de eventos, semillas y carreras se define en las tareas 4 y 5 de la fase 1. Por ahora solo existen los manifiestos.
