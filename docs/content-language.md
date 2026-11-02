# Cómo escribir eventos y semillas

Los eventos y semillas se escriben en JSON dentro de cada paquete:

```
packs/<paquete>/
  pack.json
  country.json    ← solo en paquetes de país
  seeds/*.json    ← lista de tipos de semilla
  events/*.json   ← lista de eventos
  lang/es.json    ← textos que ve el jugador (clave → texto), uno por idioma
```

Al cargar, el juego revisa todo y avisa **todos** los errores juntos, con el id del evento o semilla donde están (condición mal escrita, semilla que no existe, id repetido, campo desconocido…). La integración continua carga los paquetes del repositorio en cada PR.

## Textos (lang/)

Ningún texto para el jugador va dentro de los eventos: se usan **claves** (`evento.primer_dia_colegio`, `opcion.paes.copiar`, `diario.primeros_pasos`) y la frase va en `lang/es.json`:

```json
{
  "evento.primer_dia_colegio": "Primer día de primero básico. Mochila nueva, delantal y un poco de miedo.",
  "opcion.colegio.amigo": "Te sientas al lado de alguien que también está solo."
}
```

El validador exige que **toda clave usada exista en español** (`es`): textos de eventos y opciones, claves de `diario('...')`, nombres de país, ciudad, comuna y etapas, y `diario.<etapa>`. Otros idiomas (`en.json`, `pt-BR.json`) pueden faltar: se muestra el español mientras tanto.

## País (country.json)

```json
{
  "id": "pais.chile",
  "moneda": "CLP",
  "peso_nacimiento": 1,
  "edad_madre_min": 18,
  "edad_madre_max": 42,
  "dos_apellidos": true,
  "nombres": { "femeninos": ["Sofía"], "masculinos": ["Mateo"], "apellidos": ["González", "Muñoz"] },
  "ciudades": [
    { "id": "ciudad.santiago", "peso_nacimiento": 1, "comunas": [
      { "id": "comuna.puente_alto", "peso_nacimiento": 568, "nivel_hogar_min": 15, "nivel_hogar_max": 60 }
    ] }
  ],
  "etapas_educacion": [
    { "id": "etapa.chile.basica", "edad_desde": 6, "edad_hasta": 14, "obligatoria": true }
  ]
}
```

- `peso_nacimiento`: qué tan probable es nacer ahí (sirve la población real, por ejemplo en miles).
- `nivel_hogar_min`/`max`: rango del nivel socioeconómico (0 a 100) de un hogar de esa comuna.
- `etapas_educacion`: en orden, sin solaparse; `edad_hasta` no se incluye.
- Al nacer se eligen país, ciudad y comuna según los pesos, una madre y un padre con nombres del país, y el hijo lleva el primer apellido del padre y luego el de la madre.

## Carrera (career.json)

Cada paquete de carrera tiene un `career.json` con sus **niveles de estudio**. El país no necesita conocer las carreras: un evento con `"opciones_carreras": true` (en Chile, `evento.elegir_futuro`) recibe una opción por cada nivel de cada carrera cargada.

```json
{
  "id": "carrera.software",
  "niveles": [
    { "id": "carrera.software.nivel.analista_programador", "institucion": "instituto", "semestres": 5,
      "condiciones": "jugador.pais == 'pais.chile'" },
    { "id": "carrera.software.nivel.ingenieria_civil_computacion", "institucion": "universidad", "semestres": 11,
      "condiciones": "jugador.pais == 'pais.chile' && jugador.tiene('puede_universidad')" }
  ]
}
```

| Campo | Qué es |
|---|---|
| `id` | Empieza con `carrera.`; es también la clave de su nombre en `lang/` |
| `niveles[].id` | Empieza con el id de la carrera + `.nivel.`. Su texto es la opción ("Analista programador en un instituto…") y `<id>.titulo` el nombre del título |
| `niveles[].institucion` | `cft`, `instituto` o `universidad` |
| `niveles[].semestres` | Duración formal del plan. El núcleo suma un semestre aprobado cada 26 semanas de estudio |
| `niveles[].condiciones` | Quién puede entrar. Opcional |
| `niveles[].tendencia` | Qué tan atractivo es el nivel para quien decide solo (como `tendencia` en las opciones). Opcional |

Lo que pasa mientras se estudia (ramos, práctica, titulación, primer trabajo) va en los eventos del paquete. La titulación la decide un evento con `jugador.titularse()`: así cada carrera pone sus requisitos (por ejemplo, la práctica). La duración real suele ser mayor que la formal: un evento puede restar semestres con `jugador.semestres_aprobados -= 1`.

**Convención entre paquetes:** el país marca `puede_universidad` cuando el jugador cumple lo que su país exige para entrar a la universidad (en Chile, un buen puntaje PAES). Las carreras usan esa marca en vez de conocer la PAES.

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
| `enfriamiento_semanas` | Semanas mínimas antes de que el mismo evento se repita | 0 |
| `probabilidad_anual` | Probabilidad real de que ocurra en un año (0 a 1), por ejemplo `0.04` para un robo según ENUSC. El evento no compite por `peso`: cada semana tiene su propia oportunidad. Úsala cuando haya una estadística | — |
| `efectos` | Se aplican apenas ocurre | — |
| `opciones` | Lo que puede elegir el jugador; cada una con `texto`, `condiciones` y `efectos` | — |
| `opciones[].tendencia` | Qué tan atractiva es la opción para quien decide solo (el simulador y, más adelante, los personajes del mundo), como número de 0 en adelante. Ejemplo: `"jugador.amabilidad"` o `"(100 - jugador.responsabilidad) / 3"`. El jugador no la ve. Si falta, vale 50 | 50 |
| `opciones_carreras` | Agrega una opción por cada nivel de carrera, antes de las propias (ver Carrera) | false |

Un evento sin opciones se resuelve solo. Si lo provocó una semilla, la semilla se cierra al terminar el evento.

## El lenguaje de condiciones y efectos

**Condiciones** (deben dar verdadero o falso): `jugador.edad >= 17 && !jugador.tiene('rindio_paes')`

- Comparar: `==  !=  <  <=  >  >=`
- Combinar: `&&` (y), `||` (o), `!` (no), paréntesis
- Calcular: `+  -  *  /`
- Textos entre comillas: `'chile'`; sí/no: `true`/`false` (o `verdadero`/`falso`); vacío: `nada`

**Efectos** (cambian el mundo): `jugador.salud -= 10`, `jugador.felicidad += 5`, `jugador.salud = 50`, `plantar('semilla.x')`, `semilla('semilla.x').cerrar`.

### Lo que existe

| Escribes | Qué es |
|---|---|
| `jugador.edad`, `jugador.edad_semanas` | Edad en años o semanas |
| `jugador.salud`, `jugador.felicidad`, `jugador.inteligencia` | 0 a 100, se pueden cambiar |
| `jugador.vivo`, `jugador.sexo` | `true`/`false`; `'femenino'`/`'masculino'` |
| `jugador.apertura`, `.responsabilidad`, `.extraversion`, `.amabilidad`, `.neuroticismo` | Personalidad oculta, 0 a 100 (solo lectura) |
| `jugador.pais`, `jugador.ciudad`, `jugador.comuna` | Ids del lugar donde vive, por ejemplo `'comuna.puente_alto'` |
| `jugador.nivel_hogar` | Nivel socioeconómico del hogar, 0 a 100 |
| `jugador.etapa` | Etapa educativa actual, por ejemplo `'etapa.chile.basica'`, o `''` |
| `jugador.madre`, `jugador.padre` | Otro personaje con los mismos datos (`jugador.madre.edad`), o `nada` si no hay |
| `jugador.estudia` | Id de la carrera que estudia (`'carrera.software'`), o `nada` |
| `jugador.nivel_estudio`, `jugador.institucion` | Nivel que estudia y dónde (`'cft'`, `'instituto'`, `'universidad'`), o `nada` |
| `jugador.semestres_aprobados` | Semestres aprobados; se puede cambiar mientras estudia (reprobar resta uno) |
| `jugador.semestres_carrera` | Duración formal del nivel que estudia |
| `jugador.titulado` | Si tiene algún título |
| `jugador.titulado_en('carrera.x')` | Si se tituló de esa carrera (o de ese nivel exacto) |
| `jugador.titularse()`, `jugador.desertar()` | Termina los estudios con título, o sin él (solo efectos) |
| `si(condición, a, b)` | Vale `a` si se cumple la condición y `b` si no. Útil en tendencias: `si(jugador.tiene('liceo_tp'), 30, 10)` |
| `estudiar('nivel')` | Empieza a estudiar ese nivel de carrera (solo efectos) |
| `jugador.tiene('x')` | Si tiene la marca `x` |
| `jugador.marcar('x')`, `jugador.desmarcar('x')` | Pone o quita una marca (solo efectos) |
| `mundo.anio`, `mundo.semana`, `mundo.semanas_totales` | Fecha del juego |
| `semilla('id').activa`, `.despierta`, `.fuerza`, `.testigos` | La semilla abierta de ese tipo; `fuerza` se puede cambiar |
| `semilla('id').cerrar` | Cierra la semilla (solo efectos) |
| `plantar('id')` | Planta una semilla nueva (solo efectos). Si el personaje ya tiene abierta una del mismo tipo, no planta otra: la existente gana la `fuerza` del tipo (máximo 100) |
| `diario('clave')` | Escribe en el diario (solo efectos) |

Se irán sumando datos (dinero, colegio, relaciones, empresas) a medida que lleguen esos sistemas.
