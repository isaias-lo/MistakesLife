# Cómo escribir eventos y semillas

Los eventos y semillas se escriben en JSON dentro de cada paquete:

```
packs/<paquete>/
  pack.json
  seeds/*.json    ← lista de tipos de semilla
  events/*.json   ← lista de eventos
```

Al cargar, el juego revisa todo y avisa **todos** los errores juntos, con el id del evento o semilla donde están (condición mal escrita, semilla que no existe, id repetido, campo desconocido…). La integración continua carga los paquetes del repositorio en cada PR.

## Semilla

```json
{
  "id": "semilla.copio_paes",
  "tipo": "negativa",
  "fuerza": 40,
  "vence_anios": 20,
  "dormir_min_semanas": 0,
  "condiciones": "jugador.tiene('postulando')",
  "heredable": false
}
```

| Campo | Qué es | Por defecto |
|---|---|---|
| `id` | Id estable, empieza con `semilla.` | obligatorio |
| `tipo` | `positiva`, `negativa` o `ambigua` | obligatorio |
| `fuerza` | 0 a 100: qué tan probable es que vuelva | 50 |
| `vence_anios` | Años que puede dormir antes de olvidarse | 20 |
| `dormir_min_semanas` | Semanas mínimas antes de poder despertar | 0 |
| `condiciones` | Qué tiene que pasar para que despierte | siempre |
| `heredable` | Si pasa al hijo cuando el dueño muere | false |

Toda semilla necesita al menos un evento que la use (campo `semilla` del evento); si no, al despertar no pasaría nada y el validador lo avisa.

## Evento

```json
{
  "id": "evento.entrevista_testigo_copia",
  "semilla": "semilla.copio_paes",
  "condiciones": "jugador.edad >= 18",
  "peso": 40,
  "dia_clave": true,
  "unico": false,
  "efectos": [],
  "opciones": [
    { "texto": "opcion.reconocer_error", "efectos": ["jugador.felicidad += 10", "semilla('semilla.copio_paes').cerrar"] },
    { "texto": "opcion.negar", "efectos": ["plantar('semilla.mentira_entrevista')"] }
  ]
}
```

| Campo | Qué es | Por defecto |
|---|---|---|
| `id` | Id estable, empieza con `evento.` | obligatorio |
| `texto` | Clave del texto de la escena | el mismo id |
| `semilla` | Si se pone, el evento **no** pasa al azar: es la escena de esa semilla al despertar | — |
| `condiciones` | Cuándo puede pasar | siempre |
| `peso` | Probabilidad frente a otros eventos posibles | 10 |
| `dia_clave` | Detiene el tiempo | false |
| `unico` | Solo una vez en la vida | false |
| `efectos` | Se aplican apenas ocurre | — |
| `opciones` | Lo que puede elegir el jugador; cada una con `texto`, `condiciones` y `efectos` | — |

Un evento sin opciones se resuelve solo. Si lo provocó una semilla, la semilla se cierra al terminar el evento.

## El lenguaje de condiciones y efectos

**Condiciones** (deben dar verdadero o falso): `jugador.edad >= 17 && !jugador.tiene('rindio_paes')`

- Comparar: `==  !=  <  <=  >  >=`
- Combinar: `&&` (y), `||` (o), `!` (no), paréntesis
- Calcular: `+  -  *  /`
- Textos entre comillas: `'chile'`; sí/no: `true`/`false` (o `verdadero`/`falso`)

**Efectos** (cambian el mundo): `jugador.salud -= 10`, `jugador.felicidad += 5`, `jugador.salud = 50`, `plantar('semilla.x')`, `semilla('semilla.x').cerrar`.

### Lo que existe

| Escribes | Qué es |
|---|---|
| `jugador.edad`, `jugador.edad_semanas` | Edad en años o semanas |
| `jugador.salud`, `jugador.felicidad`, `jugador.inteligencia` | 0 a 100, se pueden cambiar |
| `jugador.vivo`, `jugador.sexo` | `true`/`false`; `'femenino'`/`'masculino'` |
| `jugador.apertura`, `.responsabilidad`, `.extraversion`, `.amabilidad`, `.neuroticismo` | Personalidad oculta, 0 a 100 (solo lectura) |
| `jugador.tiene('x')` | Si tiene la marca `x` |
| `jugador.marcar('x')`, `jugador.desmarcar('x')` | Pone o quita una marca (solo efectos) |
| `mundo.anio`, `mundo.semana`, `mundo.semanas_totales` | Fecha del juego |
| `semilla('id').activa`, `.despierta`, `.fuerza`, `.testigos` | La semilla abierta de ese tipo; `fuerza` se puede cambiar |
| `semilla('id').cerrar` | Cierra la semilla (solo efectos) |
| `plantar('id')` | Planta una semilla nueva (solo efectos) |
| `diario('clave')` | Escribe en el diario (solo efectos) |

Se irán sumando datos (dinero, colegio, relaciones, empresas) a medida que lleguen esos sistemas.
