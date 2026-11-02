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

Los textos se pueden repartir en subcarpetas por tema (`lang/familia/es.json`): el idioma es el nombre del archivo.

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

### Mortalidad (campo `mortalidad` de country.json)

La probabilidad de morir de causas naturales sale de la **tabla de vida oficial** del país (en Chile, del INE). Cada fila es una edad y la probabilidad de morir durante ese año, por sexo:

```json
"mortalidad": [
  { "edad": 0, "femenino": 0.0072, "masculino": 0.00813 },
  { "edad": 5, "femenino": 0.00014, "masculino": 0.00016 },
  { "edad": 110, "femenino": 1, "masculino": 1 }
]
```

- Las edades van de menor a mayor; entre dos filas el núcleo interpola en línea recta y después de la última usa la última.
- Cada semana se tira el dado con la probabilidad semanal equivalente, para todos los personajes (también los padres).
- La salud cambia el riesgo: con salud 40 o más vale la tabla; por debajo sube en línea recta hasta 10 veces con salud 0.
- Si el país no tiene tabla, nadie muere de causas naturales (útil en pruebas).
- La salud se acerca sola a la "salud esperada" de la edad (85 hasta los 40, 40 a los 90), unos 4 puntos al año: el cuerpo se recupera de lo que restan los eventos y envejece aunque no pase nada.
- Al morir el jugador se escribe `diario.muerte`; al morir su madre, padre, pareja o un hijo, `diario.muerte_madre`, `diario.muerte_padre`, `diario.muerte_pareja`, `diario.muerte_hija` o `diario.muerte_hijo`. En todos los casos el tiempo se detiene. Quien muere deja de tener pareja.

### Leyes (campo `leyes` de country.json)

Cada país trae sus propias leyes. Los eventos preguntan por la ley, nunca por el país, así un mismo evento sirve para todos los países:

```json
"leyes": {
  "ley.edad_voto": 18,
  "ley.matrimonio_igualitario": true,
  "ley.aborto": "tres_causales"
}
```

- El id empieza con `ley.`; el valor es sí/no (`true`/`false`), un número (una edad, un monto) o un texto (una modalidad).
- En condiciones y efectos: `ley('ley.edad_voto')` da el valor en el país donde vive el jugador.
- `cambiar_ley('ley.matrimonio_igualitario', true)` cambia la ley del país del jugador durante la partida (solo efectos): por ejemplo, un evento histórico que la aprueba.
- **Todo país debe definir las leyes que usan los eventos.** Si un evento usa `ley('ley.x')` y algún país no la tiene, el validador lo avisa.
- Cada valor se anota con su fuente en el `SOURCES.md` del país.

### Vida previa (campo `vida_previa` de country.json)

En el legado, el jugador puede seguir con un hijo que ya es adulto y que nadie jugó. Para que no le aparezcan los eventos del colegio a los 40 años, el país dice qué marcas se dan por hechas desde cada edad:

```json
"vida_previa": [
  { "edad": 18, "marcas": ["egresado_media", "decidio_futuro"] }
]
```

Al seguir con el hijo, recibe las marcas de todas las filas con `edad` menor o igual a la suya.

### Familia no jugada (campo `familia_no_jugada` de country.json)

Los descendientes del linaje que nadie juega (hijos, nietos y bisnietos de los jugadores) también forman pareja, tienen hijos y se separan, para que el heredero llegue con su propia familia. Todas son probabilidades por año:

```json
"familia_no_jugada": { "pareja_anual": 0.15, "hijo_anual": 0.07, "separacion_anual": 0.03,
                       "edad_pareja_min": 18, "edad_fertil_max": 44, "max_hijos": 3 }
```

- `hijo_anual` vale solo si la pareja es de mujer y hombre (por sexo al nacer), si la mujer tiene `edad_fertil_max` años o menos, y si tiene menos de `max_hijos` hijos vivos.
- Cuando a un hijo del jugador le nace un hijo, el jugador escribe `diario.nace_nieta` o `diario.nace_nieto`.
- Si falta el campo, los parientes no forman familia.

### Diversidad (campo `diversidad` de country.json)

En qué proporción nace cada orientación sexual e identidad de género en el país, según encuestas. Son pesos (por cada mil personas):

```json
"diversidad": {
  "orientaciones": { "heterosexual": 910, "homosexual": 30, "bisexual": 50, "asexual": 10 },
  "identidades": { "cis": 970, "trans": 10, "no_binario": 20 }
}
```

- Todo es **oculto**, como la personalidad: el personaje lo va descubriendo con lo que vive.
- `cis` siente el género de su sexo al nacer; `trans` siente el otro; `no_binario`, ninguno de los dos.
- La pareja que forma un personaje es del género que le atrae según su orientación y el género que **siente**. Una persona bisexual tiene pareja de otro género el 88 % de las veces (Pew Research, 2019).
- Los padres del primer jugador son heterosexuales o bisexuales, y las parejas nuevas tienen una orientación que calza con la persona.
- Si falta el campo, todos nacen heterosexuales y con el género de su sexo.

## Legado

Cuando el jugador muere, puede seguir con uno de sus hijos vivos, en el mismo mundo:

- Las semillas con `"heredable": true` pasan al hijo; las demás siguen vivas en el mundo a través de sus testigos.
- El hijo recibe la vida previa de su país y escribe en su diario `diario.muerte_madre` o `diario.muerte_padre`, y luego `diario.legado`.
- Si no quedan hijos vivos, la partida termina.

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
| `jugador.genero` | Género con que vive y figura en su registro: `'femenino'`, `'masculino'` o `'no_binario'`. Al nacer, el de su sexo |
| `jugador.identidad` | Género que siente (oculto). Si es distinto de `jugador.genero`, puede vivir ese camino con los eventos |
| `jugador.orientacion` | `'heterosexual'`, `'homosexual'`, `'bisexual'` o `'asexual'` (oculta) |
| `jugador.apertura`, `.responsabilidad`, `.extraversion`, `.amabilidad`, `.neuroticismo` | Personalidad oculta, 0 a 100 (solo lectura) |
| `jugador.pais`, `jugador.ciudad`, `jugador.comuna` | Ids del lugar donde vive, por ejemplo `'comuna.puente_alto'` |
| `jugador.nivel_hogar` | Nivel socioeconómico del hogar, 0 a 100 |
| `jugador.etapa` | Etapa educativa actual, por ejemplo `'etapa.chile.basica'`, o `''` |
| `jugador.madre`, `jugador.padre` | Otro personaje con los mismos datos (`jugador.madre.edad`), o `nada` si no hay |
| `jugador.pareja` | Su pareja actual (otro personaje: `jugador.pareja.edad`), o `nada`. Usa `jugador.pareja != nada && ...` antes de leer sus datos |
| `jugador.hijos` | Cuántos hijos vivos tiene |
| `jugador.formar_pareja()` | Crea una pareja de edad parecida (±4 años, mínimo 18), del mismo lugar, con nombre del país, del género que le atrae según su orientación (solo efectos). `jugador.formar_pareja('femenino')` la pide de un género. Para hijos biológicos, revisa `jugador.sexo != jugador.pareja.sexo` |
| `jugador.cambiar_genero('género')` | Cambia el género registral (`'femenino'`, `'masculino'` o `'no_binario'`). El sexo al nacer no cambia (solo efectos) |
| `jugador.cambiar_nombre()` | Le da un nombre de pila nuevo del país, acorde a su género (solo efectos) |
| `jugador.terminar_pareja()` | Termina la pareja; la otra persona sigue en el mundo (solo efectos) |
| `jugador.tener_hijo()` | Nace un hijo del jugador y su pareja (si tiene): apellido del padre y luego el de la madre, personalidad mitad heredada y mitad propia. Escribe `diario.nace_hija` o `diario.nace_hijo` (solo efectos) |
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
| `ley('ley.x')` | Valor de una ley en el país del jugador (ver Leyes) |
| `cambiar_ley('ley.x', valor)` | Cambia una ley del país del jugador durante la partida (solo efectos) |

Se irán sumando datos (dinero, colegio, relaciones, empresas) a medida que lleguen esos sistemas.
