# Cómo escribir eventos y semillas

Los eventos y semillas se escriben en JSON dentro de cada paquete:

```
packs/<paquete>/
  pack.json
  country.json    ← solo en paquetes de país
  seeds/*.json    ← lista de tipos de semilla
  events/*.json   ← lista de eventos
  news/*.json     ← noticias del mundo (temblores, temporales, crisis, elecciones)
  chapters/*.json ← títulos que las líneas del diario le dan al año (capítulos)
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

### Variantes por país (`es-CL`, `es-AR`)

Un mismo texto se dice distinto en cada país ("pega" en Chile, "laburo" en Argentina). El texto general va en `es` con palabras que se entiendan en todos lados, y el paquete del país lo cambia en su variante:

- El país dice qué variante usa en `country.json`: `"variantes_idioma": { "es": "es-CL" }`.
- El paquete del país trae `lang/variante/es-CL.json` (el idioma es el nombre del archivo) con **solo** los textos que cambian.
- Una variante solo puede cambiar textos que ya existen en `es`: si trae una clave nueva, el validador avisa.
- El juego elige solo la variante del país donde vive el jugador (`LifeSession.Text`); lo que la variante no tenga sale de `es`.

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
- En el país mismo, `peso_nacimiento` (1 si falta) elige en qué país se nace. Con 0 nadie nace ahí por azar: solo se juega eligiéndolo (`new LifeSession(contenido, semilla, "pais.argentina")` o `--pais pais.argentina` en el simulador). Sirve mientras un país todavía no tiene sus eventos de vida.
- `nivel_hogar_min`/`max`: rango del nivel socioeconómico (0 a 100) de un hogar de esa comuna.
- `etapas_educacion`: en orden, sin solaparse; `edad_hasta` no se incluye.
- `inicio_ano_escolar`: día-mes en que empiezan las clases (`"04-03"` en Chile). Ese día, cada año, quien va al colegio arma su horario del año nuevo. Opcional.
- `fin_ano_escolar`: día-mes en que terminan las clases (`"13-12"`); desde ahí hasta `inicio_ano_escolar` son las vacaciones de verano. `vacaciones_invierno`: `{ "desde": "23-06", "hasta": "04-07" }` (los dos días incluidos). Con eso, `jugador.en_clases` dice si esa semana hay clases. Opcionales.
- `curso`: cómo es un curso del colegio: `alumnos` (cuántos, contándote), `con_nombre` (con cuántos tienes trato: tienen nombre y aparecen en los momentos), y probabilidades de 0 a 1: `cambio_profe_jefe` (de un año al otro, 0.3 si falta), `siguen_al_cambiar_etapa` (de básica a media, 0.4), `cambios_por_ano` (alguien se va y llega alguien nuevo, 0.3) y `profes_mujeres` (0.7). En `etapas_educacion`, `"curso": true` marca las etapas en que se va a un curso (en Chile, básica y media). Esas etapas pueden tener `ramos` (`[{ "id": "ramo.chile.ingles", "desde": 5 }]`: en qué cursos de la etapa se tiene, de 1 en adelante; el nombre va en el idioma del país; `"prueba_escrita": false` si sus notas no vienen de pruebas que se estudian, como Educación Física, y entonces no se anuncia como momento) y `promocion` (formas de pasar de curso; basta una: `{ "reprobados": 1, "promedio_minimo": 4.5 }`; sin ninguna siempre se pasa).
- `escala_notas`: `{ "minima": 1.0, "maxima": 7.0, "aprueba": 4.0, "decimales": 1, "notas_por_ano": 10 }` (notas por ramo en un año). Sin ella no hay notas. Cómo se llama el curso va en el idioma del país: `"curso.etapa.chile.basica": "{0}º básico {1}"` (el número y la letra).
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

### Economía (campo `economia` de country.json)

Cada país trae cómo funciona su dinero. El juego tiene su propio calendario, así que la inflación no se copia de años reales: **la genera el juego** con los datos del país, y sigue avanzando generación tras generación.

```json
"economia": {
  "inflacion": {
    "regimen_inicial": "economia.inflacion.estable",
    "regimenes": [
      { "id": "economia.inflacion.estable", "anual_min": 0.02, "anual_max": 0.045, "variacion_mensual": 0.003,
        "cambio_anual": { "economia.inflacion.alta": 0.08 } },
      { "id": "economia.inflacion.alta", "anual_min": 0.06, "anual_max": 0.13, "variacion_mensual": 0.006,
        "cambio_anual": { "economia.inflacion.estable": 0.55 } }
    ]
  }
}
```

- Un **régimen** es un "clima" de la economía: estable, alta, hiperinflación… Su id empieza con `economia.inflacion.`.
- `anual_min` y `anual_max`: rango de la inflación anual (0.03 = 3 %). Al entrar al régimen sale un valor al azar en ese rango, y cada mes se mueve hasta `variacion_mensual` hacia arriba o abajo, sin salirse del rango.
- `cambio_anual`: probabilidad al año de pasar a otro régimen. 0.08 es, en promedio, un episodio cada 12 años; 0.55 es que dura cerca de un año y medio. A un régimen que no aparece en ningún `cambio_anual` solo se llega con un evento (`cambiar_inflacion`).
- El año tiene 12 meses (de 4 o 5 semanas). Al empezar cada mes suben los precios y se tiran los dados de cambio de régimen.
- Cuando el país del jugador cambia de régimen, se escribe en su diario `diario.<id del régimen>` (por ejemplo `diario.economia.inflacion.alta`). Ese texto es obligatorio.
- **Montos en los paquetes:** se escriben en dinero de cuando empieza el mundo, y `precio(monto)` los lleva a precios de hoy. Así un evento escrito una vez sirve en cualquier generación.
- Si falta el campo, los precios nunca cambian (útil en pruebas).

#### Sueldos y liquidación (`economia.sueldos` y `economia.liquidacion`)

```json
"sueldos": { "minimo": 539000, "tipico": 760000, "relativo_sin_titulo": 0.85, "reajuste_meses": 12 },
"liquidacion": {
  "dependiente": [
    { "id": "descuento.chile.afp", "tasa": 0.1115, "tope": 3500000, "rebaja_impuesto": true }
  ],
  "honorarios": [ { "id": "descuento.chile.retencion_honorarios", "tasa": 0.1525 } ],
  "impuesto_mensual": [ { "desde": 0, "tasa": 0 }, { "desde": 931500, "tasa": 0.04 } ]
}
```

- Todos los montos son **mensuales, brutos y en dinero del inicio del mundo**. El núcleo los lleva a precios de hoy con el índice de precios (como hacen la UF y la UTM en Chile).
- `minimo` y `tipico` (la mediana): sueldos de referencia del país. `relativo_sin_titulo`: cuánto gana alguien sin título frente al típico.
- `reajuste_meses`: cada cuánto se reajustan los sueldos según la inflación. Entre reajustes el sueldo pierde poder de compra (con inflación alta se nota mucho, como en Argentina).
- `dependiente` y `honorarios`: descuentos de la liquidación según el tipo de trabajo. Cada uno con `id` (empieza con `descuento.`, y es la clave de su nombre en `lang/`), `tasa`, `tope` opcional (el tope imponible) y `rebaja_impuesto` (si se resta antes de calcular el impuesto).
- `impuesto_mensual`: tramos del impuesto que se descuenta cada mes a quien tiene contrato. Cada parte del sueldo paga la tasa de su tramo. Su nombre en la liquidación es el texto `descuento.impuesto_mensual`.
- Al empezar cada mes, quien tiene trabajo recibe el **líquido** (bruto menos descuentos) en su dinero, y queda guardada su liquidación.
- Si un evento usa `sueldo_minimo()`, `sueldo_tipico()` o `sueldo_titulo()`, todo país debe tener `sueldos`.

#### Gastos del mes (`economia.gastos`)

```json
"gastos": {
  "edad_desde": 18,
  "felicidad_en_rojo": 1,
  "deuda_por_deficit": "deuda.chile.linea_credito",
  "cupo_deficit": 1500000,
  "categorias": [
    { "id": "gasto.chile.cuentas", "monto": "100000", "condiciones": "jugador.tiene('vive_independiente') && !jugador.cuentas_incluidas", "compartido": true },
    { "id": "gasto.chile.aporte_casa", "monto": "jugador.sueldo_liquido * 0.1", "a_precios_de_hoy": true, "condiciones": "jugador.tiene('aporta_decimo')" }
  ]
}
```

- Al empezar cada mes, después del sueldo, el jugador paga cada gasto cuyas `condiciones` se cumplan (desde `edad_desde`; antes los pagan sus padres). Por ahora solo el jugador lleva sus cuentas.
- `monto` es una **expresión** del lenguaje que da el monto mensual en dinero del inicio del mundo: un número (`"100000"`), una cuenta (`"160000 + 80000 * jugador.hijos_menores"`) o una función como `arriendo('vivienda.pieza')`. El arriendo y la comida **no** son gastos: salen del contrato que firmó el jugador (ver Vivienda) y de la compra que eligió (ver Supermercado).
- `compartido`: se divide en dos si tiene pareja.
- `a_precios_de_hoy` (opcional): el `monto` ya viene en dinero de hoy y no se reajusta otra vez (por ejemplo `"causa('causa.chile.alimentos_demandado').monto"`, que ya se reajusta solo).
- `ingreso` (opcional): es plata que el jugador **recibe** cada mes en vez de pagarla (la pensión de alimentos que le paga el otro papá). Suma al dinero, queda en la cartola del mes con monto negativo (Tu dinero la muestra como plata que entró) y no cuenta en `jugador.gastos`.
- El `id` empieza con `gasto.` y es la clave de su nombre en `lang/`.
- Cada mes en que los gastos son más que el dinero suma uno a `jugador.meses_en_rojo` y resta `felicidad_en_rojo` (estrés financiero). Al alcanzar de nuevo, vuelve a 0.
- `deuda_por_deficit` (opcional): la deuda de `economia.deudas` que cubre lo que falta, como la línea de crédito del banco. El dinero queda en 0 y lo que faltó pasa a esa deuda, con su interés (diario `diario.gastos.linea_credito` la primera vez). `cupo_deficit` (en dinero del inicio del mundo) es hasta cuánto llega: lo que pasa del cupo no se paga, son cuentas atrasadas. Sin `deuda_por_deficit`, el dinero queda en negativo.

#### Vivienda (`economia.vivienda`)

Nada se cobra si el jugador no lo eligió: el arriendo sale de un **contrato** que firma en un evento (`jugador.arrendar('vivienda.pieza')`), con el precio de su comuna ese día.

```json
"vivienda": { "plazo_meses": 12, "garantia_meses": 1, "reajuste_meses": 6, "tipos": [
  { "id": "vivienda.pieza", "incluye_cuentas": true },
  { "id": "vivienda.departamento" },
  { "id": "vivienda.casa" }
] }
```

y en cada comuna, el arriendo típico de cada tipo (en dinero del inicio del mundo):

```json
{ "id": "comuna.puente_alto", "arriendos": { "vivienda.pieza": 250000, "vivienda.departamento": 420000, "vivienda.casa": 650000 } }
```

- Al firmar deja `garantia_meses` de arriendo de garantía (se devuelve al dejar el arriendo o al cambiarse) y queda con la marca `vive_independiente`.
- Cada mes paga el arriendo del contrato (la mitad si tiene pareja), como primera línea de sus gastos, con el id del tipo (`vivienda.pieza`, que es también su clave en `lang/`).
- El arriendo no cambia entre reajustes: cada `reajuste_meses` sube con la inflación del país.
- El contrato dura `plazo_meses`; en 0 sigue mes a mes hasta que el jugador lo renueve o lo deje (el evento `evento.fin_contrato_arriendo` le pregunta).
- `incluye_cuentas`: el arriendo ya trae luz, agua e internet (`jugador.cuentas_incluidas`), como casi siempre en una pieza.
- Toda comuna del país debe tener el precio de cada tipo, y todo tipo que use un evento debe existir en todo país.

#### Supermercado (`economia.supermercado`)

La comida tampoco es un monto fijo: el jugador **elige su compra** en un evento (`jugador.elegir_compra('compra.basica')`) y cada mes paga esa compra para todos los que comen en su casa.

```json
"supermercado": { "factor_hijo": 1, "compras": [
  { "id": "compra.justa", "costo_persona": 73152 },
  { "id": "compra.basica", "costo_persona": 90384 },
  { "id": "compra.con_antojos", "costo_persona": 107616 }
] }
```

- `costo_persona`: lo que cuesta esa compra al mes para una persona, en dinero del inicio del mundo. Sale de la canasta básica oficial del país (ver `SOURCES.md`).
- Cada mes paga `costo_persona × (1 + pareja + hijos menores × factor_hijo)`, la mitad si tiene pareja, como una línea de sus gastos con el id de la compra (que es también su clave en `lang/`).
- `factor_hijo`: cuánto come un hijo menor comparado con un adulto (1 = lo mismo; en Argentina la canasta se mide por "adulto equivalente" y un niño come menos).
- Solo se cobra mientras vive por su cuenta (`vive_independiente`); en la casa de su familia, la comida va en el aporte a la casa. Si todavía no elige, no se cobra: el evento `evento.armar_compra` le pregunta apenas se va a vivir solo, y `evento.revisar_compra` le pregunta de vez en cuando si sigue igual.
- Todo país debe tener las compras que usan los eventos. El `id` empieza con `compra.`.

#### Servicios que contrata el jugador (`economia.servicios`)

Lo que se paga cada mes porque el jugador lo **contrató** (el plan del celular; después internet, el gimnasio...). Nada se cobra si no lo contrató: lo elige en un evento con `jugador.contratar('servicio.celular.prepago')`, y lo puede cambiar (contratar otra opción del mismo servicio) o cancelar (`jugador.cancelar_servicio('servicio.celular')`).

```json
"servicios": [
  { "id": "servicio.celular", "condiciones": "jugador.tiene_celular || jugador.servicio('servicio.celular') != 'servicio.celular.prepago'", "opciones": [
    { "id": "servicio.celular.prepago", "precio": 5990 },
    { "id": "servicio.celular.plan", "precio": 12990 },
    { "id": "servicio.celular.plan_ilimitado", "precio": 20990 }
  ] }
]
```

- `precio`: lo que cuesta al mes, en dinero del inicio del mundo (con fuente en `SOURCES.md`). Cada mes es una línea de los gastos con el id de la opción, que es también su clave en `lang/`.
- El id del servicio empieza con `servicio.` y el de cada opción, con el id de su servicio (`servicio.celular.plan`).
- `condiciones` (opcional): cuándo se cobra lo contratado. En el celular, el prepago solo se paga si tiene celular (si se lo robaron, no carga), pero un plan sigue llegando en la boleta.
- `compartido` (opcional): se divide en dos si tiene pareja.
- `cantidad` (opcional): una expresión que dice por cuántos se paga cada mes. El jardín se paga por cada hijo chico (`"jugador.hijos_en_casa_entre(0, 3)"`) y el colegio por cada hijo en edad escolar (`"jugador.hijos_en_casa_entre(4, 17)"`). Si da 0, ese mes no se cobra. Si falta, se paga una vez.
- Servicios que ya existen: `servicio.celular` (prepago, plan, plan_ilimitado) `servicio.transporte` (caminando, bus, bus_y_metro: el pasaje por 44 viajes al mes) `servicio.internet` (basico, rapido; compartido) `servicio.cuentas` (cuidando, normal, sin_cuidar: luz, agua y gas; compartido; no se cobra si el arriendo incluye las cuentas) `servicio.jardin_hijos` (en_casa, publico, particular; por cada hijo de 0 a 3 años; compartido) `servicio.colegio_hijos` (gratuito, copago, particular; por cada hijo de 4 a 17 años; compartido) y, solo en Chile, `servicio.personales` (austero, normal, gustos: ropa, salidas y cuidado personal) y `servicio.arancel` (gratuidad, familia, y por institución `pago_*`, `brecha_*` que es lo que no cubre el CAE, y `mitad_*` al pasarse de la duración formal con gratuidad; solo se cobra mientras estudia).
- Todo país debe tener los servicios y opciones que usan los eventos.

#### Declaración anual de impuestos (`economia.declaracion_anual`)

```json
"declaracion_anual": {
  "mes": 4,
  "tramos": [ { "desde": 0, "tasa": 0 }, { "desde": 11178000, "tasa": 0.04 } ],
  "gastos_presuntos_honorarios": 0.3, "tope_gastos_presuntos": 12420000,
  "base_cotizacion_honorarios": 0.8, "tasa_cotizacion_honorarios": 0.17,
  "multa_atraso": 0.1
}
```

- Cada liquidación se suma a lo ganado en el año. Al empezar el `mes` de declarar, se calcula la declaración del año anterior de quien tuvo **honorarios** (quien solo tuvo contrato ya pagó su impuesto cada mes y no declara, como en Chile).
- Renta = sueldos (sin las cotizaciones que rebajan impuesto) + honorarios − gastos presuntos (con su tope) − cotizaciones de los honorarios. El impuesto se calcula con los `tramos` anuales.
- Saldo = lo ya pagado (impuesto mensual y retenciones) − impuesto − cotizaciones de honorarios. Si es positivo hay devolución; si es negativo, hay que pagar.
- El jugador la tiene pendiente (`jugador.declaracion_pendiente`) hasta aceptarla con `jugador.declarar()` en un evento. Los demás personajes declaran solos.
- Hay plazo hasta que empieza el mes siguiente. Si no declaró y debía, paga lo que debía más la `multa_atraso`; si le tocaba devolución, la pierde.
- Textos obligatorios del diario: `diario.impuestos.devolucion`, `diario.impuestos.pago`, `diario.impuestos.multa` y `diario.impuestos.devolucion_perdida`.

#### Jubilación (`economia.jubilacion`)

```json
"jubilacion": {
  "cuenta_individual": {
    "descuento": "descuento.chile.afp", "parte_a_la_cuenta": 0.897, "tasa_honorarios": 0.1,
    "rentabilidad_real_anual": 0.035, "anos_de_pension": 22
  },
  "pension_universal": { "monto": 224004, "completa_hasta": 789139, "sin_pension_desde": 1252602 }
}
```

- Las dos partes son opcionales: cada país arma su sistema con las que tenga.
- `cuenta_individual` (capitalización, como las AFP): cada mes, quien recibe sueldo con contrato ahorra en su fondo la `parte_a_la_cuenta` del `descuento` de su liquidación (el resto es comisión y seguro). Quien trabaja a honorarios ahorra al declarar: `tasa_honorarios` sobre la base de cotización de `declaracion_anual`. El fondo gana `rentabilidad_real_anual` (por sobre la inflación) cada mes.
- El fondo y la pensión se guardan en dinero del inicio del mundo (como la UF): la inflación no los achica.
- Al jubilarse (`jugador.jubilarse()` en un evento), el fondo se convierte en una pensión fija mensual que dura `anos_de_pension` ganando la misma rentabilidad (como una renta vitalicia). Desde el mes siguiente se paga cada mes.
- `pension_universal` (como la PGU): quien la pidió (`jugador.recibir_pension_universal()`) recibe el `monto` completo si su pensión propia llega hasta `completa_hasta`; desde ahí baja en línea recta hasta 0 en `sin_pension_desde`. Montos mensuales en dinero del inicio del mundo.
- El descuento de `cuenta_individual` debe estar en `liquidacion.dependiente`.
- `reparto` (como la ANSES de Argentina), en vez de `cuenta_individual` (un país tiene uno de los dos): el `descuento` no se ahorra en una cuenta. Cada mes con sueldo con contrato suma un mes de aportes y actualiza el sueldo promedio de los últimos `anos_promedio` años (en dinero del inicio del mundo). Al jubilarse, sin `anos_minimos` de aportes no hay pensión propia; con ellos, la pensión es `base_universal` + `porcentaje_por_ano` × años × sueldo promedio, y al menos la `minima`. Quien trabaja a honorarios (monotributo) todavía no suma años.

```json
"reparto": { "descuento": "descuento.argentina.jubilacion", "anos_minimos": 30, "porcentaje_por_ano": 0.015,
  "anos_promedio": 10, "base_universal": 112000, "minima": 245000 },
"pension_universal": { "monto": 196000, "completa_hasta": 0, "sin_pension_desde": 1 }
```

- Con `completa_hasta: 0` y `sin_pension_desde: 1`, la pensión universal es solo para quien no tiene pensión propia (como la PUAM).

#### Ciclo económico (`economia.ciclo`)

```json
"ciclo": {
  "fase_inicial": "economia.ciclo.normal",
  "fases": [
    { "id": "economia.ciclo.normal", "cambio_anual": { "economia.ciclo.recesion": 0.1 } },
    { "id": "economia.ciclo.recesion", "golpe_fondos": -0.08,
      "cambio_anual": { "economia.ciclo.normal": 0.6, "economia.ciclo.crisis": 0.1 } },
    { "id": "economia.ciclo.crisis", "inflacion": "economia.inflacion.alta", "golpe_fondos": -0.2,
      "cambio_anual": { "economia.ciclo.normal": 0.5 } }
  ]
}
```

- Funciona como los regímenes de inflación: cada mes, el país puede pasar a otra fase con las probabilidades de `cambio_anual` (por año). Los eventos pueden provocar una con `cambiar_ciclo('id')`, y leer la fase actual con `mundo.ciclo`.
- Al empezar una fase: el jugador que vive ahí lo lee en su diario (`diario.` + id, texto obligatorio); si tiene `inflacion`, el país pasa a ese régimen; si tiene `golpe_fondos`, los fondos de pensión de todos los que viven ahí cambian de una vez (-0.2 = caen 20 %).
- Los efectos en el empleo los ponen los eventos del país, con condiciones sobre `mundo.ciclo` (por ejemplo, despidos solo en recesión o crisis).
- Con `quiebras` (opcional, por defecto 1), la fase multiplica las quiebras de empresas del país (2 = el doble que en un año normal).
- Con `contratacion` (opcional, por defecto 1), la fase multiplica las llamadas a entrevista de quienes buscan trabajo (0.6 = 40 % menos).
- Opcional: un país sin `ciclo` no tiene crisis.

#### Deudas (`economia.deudas`)

```json
"deudas": {
  "meses_para_moroso": 3,
  "tipos": [
    { "id": "deuda.chile.cae", "tasa_anual": 0.02, "cuotas": 180, "reajustable": true, "entrega_dinero": false, "se_extingue_al_morir": true,
      "condicion_pago": "jugador.estudia == nada" },
    { "id": "deuda.chile.consumo", "tasa_anual": 0.3, "cuotas": 36 }
  ]
}
```

- Un evento endeuda con `jugador.endeudarse('tipo', monto)` (monto en dinero del inicio del mundo). Si ya tiene una deuda de ese tipo, se suma a ella y vuelve a tener todas las `cuotas`.
- `entrega_dinero` (por defecto `true`): el monto llega a su saldo. El crédito universitario paga directo a la institución (`false`).
- Al empezar cada mes, después de los gastos: si es `reajustable`, la deuda sube con la inflación (como lo que está en UF); si no, la inflación la achica. Luego suma su interés (`tasa_anual`; si es reajustable, es por sobre la inflación).
- Si se cumple `condicion_pago` (o si no la tiene), se cobra la cuota fija que paga la deuda en las cuotas que quedan. Sin dinero suficiente, la cuota queda impaga.
- Con `meses_para_moroso` meses seguidos sin pagar alguna cuota, queda en el registro de morosos (`jugador.moroso`); sale al ponerse al día con todas. Con `meses_prescripcion` (opcional) meses seguidos sin pagar, la deuda prescribe y se borra (diario `diario.deudas.prescrita`).
- El `id` empieza con `deuda.` y es la clave de su nombre en `lang/`. Todo país debe tener los tipos que usan los eventos.
- `se_paga_con_el_saldo: true` (línea de crédito, sobregiro): cada mes se lleva todo el dinero que haya, hasta lo que se debe. Si no alcanza al pago mínimo, igual queda atrasada.
- Deudas rotativas (tarjetas): con `pago_minimo` (por ejemplo `0.05`), cada mes se cobra esa parte de lo que se debe, al menos `pago_minimo_monto` (en dinero del inicio del mundo), en vez de cuotas fijas.
- Con `cuotas_repactacion`, una deuda atrasada se puede repactar con `jugador.repactar()`: queda al día en esas cuotas nuevas y, al mes siguiente, sale del registro de morosos. `jugador.puede_repactar` dice si tiene alguna.
- Textos obligatorios del diario: `diario.deudas.pagada`, `diario.deudas.moroso`, `diario.deudas.fuera_registro` y `diario.deudas.prescrita`.

#### Empresas (`economia.empresas`)

```json
"empresas": {
  "misma_empresa": 0.5,
  "tamanos": [
    { "id": "empresa.tamano.micro", "peso": 0.15, "sueldo_relativo": 0.75, "cargo_maximo": 2, "quiebra_anual": 0.1, "crece_anual": 0.04 },
    { "id": "empresa.tamano.grande", "peso": 0.5, "sueldo_relativo": 1.15, "quiebra_anual": 0.015, "achica_anual": 0.02 }
  ]
}
```

- Quien entra a trabajar en su carrera (`jugador.trabajar_en`) llega a una empresa de esa carrera. El tamaño sale al azar según `peso` (la parte de los trabajadores que trabaja en ese tamaño). Con `misma_empresa` de probabilidad llega a una empresa donde ya trabaja otro personaje, del mismo tamaño y carrera; si no, a una nueva, con un nombre de `nombres_empresa` de la carrera.
- Los tamaños van del más chico al más grande. `sueldo_relativo` multiplica el sueldo del cargo. `cargo_maximo` es el cargo más alto que existe en ese tamaño, contado desde el inicial (0 = solo el inicial). Sin él no hay tope. Quien llega con más experiencia entra en el cargo más alto que existe, y no puede ascender más allá.
- Una vez al año, cada empresa donde trabaja alguien puede quebrar (`quiebra_anual`, por la fase del ciclo), o si no crecer al tamaño siguiente (`crece_anual`) o achicarse al anterior (`achica_anual`).
- Si quiebra la empresa del jugador, el tiempo se detiene y `jugador.empresa_quebro` queda verdadero hasta que un evento del país le quite el trabajo (en Chile, `evento.empresa.quiebra`). Si el país no tiene ese evento, lo pierde al año siguiente. Los demás personajes lo pierden de una vez.
- El `id` empieza con `empresa.tamano.` y es la clave de su texto. Textos obligatorios del diario: `diario.empresa.quiebra`, `diario.empresa.crece` y `diario.empresa.achica`.
- Opcional: en un país sin `empresas`, los trabajos no tienen empresa (sin tope de cargo ni sueldo por tamaño).

#### Búsqueda de trabajo (`economia.busqueda_trabajo`)

Ningún trabajo con contrato llega solo: el jugador lo busca. Primero dice qué busca (en su carrera, con `jugador.postular_en('área')`, o `postular_en('cualquier_trabajo')`) y después cómo (`jugador.buscar_trabajo('forma')`). Cada semana manda postulaciones y puede llegar una llamada a entrevista; en la entrevista elige cómo ir (`jugador.dar_entrevista(puntos)`) y, si queda, recibe una oferta que acepta, negocia o rechaza.

```json
"busqueda_trabajo": {
  "formas": [
    { "id": "busqueda.forma.portales", "postulaciones_semana": 10, "entrevista_semanal": 0.08, "exito_extra": -0.08 },
    { "id": "busqueda.forma.contactos", "postulaciones_semana": 1, "entrevista_semanal": 0.08, "exito_extra": 0.2, "necesita_contacto": true }
  ],
  "exito_entrevista": 0.35,
  "factor_por_ano_experiencia": 0.05, "tope_experiencia": 1.5,
  "factor_titulo": 1.2,
  "edad_dificil": 50, "factor_edad": 0.65,
  "semanas_cesantia_larga": 52, "factor_cesantia_larga": 0.7,
  "meses_para_sueldo_tipico": 12,
  "carino_contacto": 50,
  "negociacion": { "mejora": 0.08, "prob_mejora": 0.4, "prob_retiro": 0.1 }
}
```

- **Formas de buscar** (`formas`): cada país trae las suyas (en Chile también la OMIL de la municipalidad). El `id` empieza con `busqueda.forma.` y es la clave de su texto. `entrevista_semanal` es la probabilidad de que lo llamen cada semana; `exito_extra` suma o resta a la probabilidad de quedar después de la entrevista (postular a todo da más llamadas pero peores; una recomendación ayuda mucho). Con `necesita_contacto`, solo se puede si tiene una amistad o un colega de 18 a 70 años que le tenga al menos `carino_contacto` de cariño: esa persona queda como `jugador.contacto`.
- **Lo que cambia la llamada:** cada año cotizado suma `factor_por_ano_experiencia` (hasta `tope_experiencia`), un título multiplica por `factor_titulo`, desde `edad_dificil` años por `factor_edad`, después de `semanas_cesantia_larga` buscando por `factor_cesantia_larga`, y la fase del ciclo por su `contratacion`.
- **La entrevista:** quedar es `exito_entrevista` + `exito_extra` + los puntos de la opción (10 = 10 % más) + un poco por la extraversión, entre 5 % y 95 %. Si no queda, lo anota en el diario (`diario.busqueda.rechazo`).
- **La oferta:** en su área entra como con `trabajar_en` (empresa, cargo y sueldo), con el contrato que pidió en `postular_en` y la `marca_trabajo` de su carrera. Fuera de su carrera entra con contrato: con el sueldo mínimo si no tiene título y cotizó menos de `meses_para_sueldo_tipico` meses, y si no con lo de `sueldo_titulo()`. Al aceptar pierde la marca `buscando_trabajo`, gana `trabajando` y se anota `diario.busqueda.nuevo_trabajo`.
- **Negociar** (`negociacion`): con `prob_mejora` le suben el sueldo de la oferta en `mejora`; con `prob_retiro` la retiran (`diario.busqueda.oferta_retirada`) y sigue buscando; si no, la mantienen igual.
- La búsqueda dura mientras tenga la marca `buscando_trabajo`; al quitarla se borra sola.
- Textos obligatorios: el de cada forma y los cuatro del diario (`diario.busqueda.llamado` se anota cuando lo llaman a entrevista). Opcional: un país sin `busqueda_trabajo` no tiene formas de buscar (nadie consigue trabajo buscando).

#### Vacaciones legales (`economia.vacaciones`, rediseño R6c)

Quien trabaja con contrato gana sus días de vacaciones mes a mes (`sistema.vacaciones`, después de los sueldos) y los toma cuando quiere, con el botón "Tomar tus vacaciones" del equipo (un momento `a_pedido`). Si se va sin tomarlos, se le pagan con `jugador.cobrar_vacaciones()`.

```json
"vacaciones": {
  "tramos": [{ "desde_anos": 0, "dias": 15 }],
  "unidad": "habiles",
  "progresivo_desde_anos": 10, "progresivo_cada_anos": 3, "progresivo_maximo_anos_anteriores": 10,
  "acumula_maximo_anos": 2,
  "se_pagan_al_salir": true,
  "divisor_dia": 30,
  "dias_pagados_por_dia": 1.4
}
```

- `tramos`: días al año según los años con el mismo empleador (Argentina: 14, 21, 28 y 35 desde los 0, 5, 10 y 20 años). El primero parte en 0.
- `unidad`: `habiles` o `corridos` (solo cambia el texto de la pantalla).
- Feriado progresivo (opcional): con `progresivo_desde_anos` años trabajados en total, un día más por cada `progresivo_cada_anos` años nuevos; de empleadores anteriores cuentan hasta `progresivo_maximo_anos_anteriores` (salen de los meses cotizados).
- `acumula_maximo_anos`: cuántos años de días se pueden juntar; lo que pasa de ahí se pierde.
- Al salir se paga cada día ganado como `dias_pagados_por_dia` días de sueldo, y un día de sueldo es el sueldo mensual / `divisor_dia` (Argentina: 25 y 1).
- Opcional: en un país sin `vacaciones`, nadie gana días.

#### Negocios entre socios (`economia.negocios`, rediseño R6d2)

Un negocio chico con una persona de tu vida (`jugador.abrir_negocio(aporte, 'formal' | 'informal')`). Cada mes (`sistema.negocios`, después de los sueldos) a cada socio le toca su parte de la ganancia, o el negocio cierra.

```json
"negocios": { "ganancia_formal": 3.7, "ganancia_informal": 0.92, "variacion": 1.47, "cierre_primer_ano": 0.152, "cierre_anual": 0.137 }
```

- `ganancia_formal` y `ganancia_informal`: lo que gana al mes **cada socio** de un negocio típico, en sueldos mínimos (en Chile, la Encuesta de Microemprendimiento del INE). Se multiplica por lo bien que lo llevan los dos (su responsabilidad, de 0,5 a 1,5), por la economía (como en los oficios), por la suerte del negocio y por una variación chica de cada mes.
- `variacion`: qué tan distinto es un negocio de otro (desviación del logaritmo). La suerte se elige al abrir: la mayoría gana menos que el promedio y unos pocos mucho más (con 1,47, el 77 % queda bajo el promedio, como en la encuesta).
- `cierre_primer_ano` y `cierre_anual`: probabilidad de cerrar en el primer año y en cada año siguiente; llevarlo mal la sube hasta 1,5 veces y llevarlo bien la baja a la mitad.
- `impuesto_formal` (opcional): se descuenta de la ganancia formal si la del país está medida antes de impuestos.
- Si quiebra, se anota `diario.negocio.quebro`; lo puesto se pierde. Si el socio muere, cierra. Textos obligatorios del diario: `diario.negocio.abrio`, `diario.negocio.cerro`, `diario.negocio.quebro`.

#### Herencia (`economia.herencia`)

```json
"herencia": { "beneficio_inventario": true, "porciones_pareja": 2 }
```

- Cuando alguien muere, sus bienes son su dinero y lo ahorrado para la pensión. Se restan sus deudas, salvo las de tipos con `"se_extingue_al_morir": true` (en `economia.deudas`), que se borran.
- Heredan los hijos vivos (una parte cada uno) y la pareja (`porciones_pareja` partes). Sin hijos ni pareja, heredan los padres vivos. Sin nadie, se pierde (lo recibe el Estado).
- Si las deudas son más que los bienes: con `beneficio_inventario`, nadie hereda ni paga; si es `false`, cada heredero paga su parte de lo que falta.
- `posesion_efectiva` (opcional): para recibir lo heredado hay que hacer un trámite. Lo que le toca a cada heredero queda en `jugador.herencia_pendiente` (se reajusta con los precios) hasta que un evento lo entregue con `jugador.recibir_herencia()`. Las deudas que se heredan se cobran igual.
- Si falta `herencia`, lo que deja se pierde. Textos obligatorios del diario: `diario.herencia.recibida` y `diario.herencia.deudas`.

### Vida previa (campo `vida_previa` de country.json)

En el legado, el jugador puede seguir con un hijo que ya es adulto y que nadie jugó. Para que no le aparezcan los eventos del colegio a los 40 años, el país dice qué marcas se dan por hechas desde cada edad:

```json
"vida_previa": [
  { "edad": 18, "marcas": ["egresado_media", "decidio_futuro"] }
]
```

Al seguir con el hijo, recibe las marcas de todas las filas con `edad` menor o igual a la suya. Una fila puede traer `"vivienda": "vivienda.departamento"`: el hijo ya arrienda eso, con un contrato al precio de hoy de su comuna y sin volver a pagar la garantía.

### Familia no jugada (campo `familia_no_jugada` de country.json)

Los descendientes del linaje que nadie juega (hijos, nietos y bisnietos de los jugadores) también forman pareja, tienen hijos y se separan, para que el heredero llegue con su propia familia. Todas son probabilidades por año:

```json
"familia_no_jugada": { "pareja_anual": 0.15, "hijo_anual": 0.07, "separacion_anual": 0.03, "adopcion_anual": 0.03,
                       "edad_pareja_min": 18, "edad_fertil_max": 44, "max_hijos": 3 }
```

- `hijo_anual` vale solo si la pareja es de mujer y hombre (por sexo al nacer), si la mujer tiene `edad_fertil_max` años o menos, y si tiene menos de `max_hijos` hijos vivos.
- `adopcion_anual` vale para parejas del mismo sexo (hasta los 50 años y con menos de `max_hijos` hijos vivos). Pon 0 si la ley del país no permite que adopten.
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

## Volver al pasado

Textos obligatorios del núcleo en `packs/base/lang/es.json`: `diario.intuicion` (al volver al pasado), `diario.intuicion_decision` (al volver antes de una decisión que ya se había tomado) y el nombre de cada etapa de la vida: `etapa_vida.infancia`, `etapa_vida.ninez`, `etapa_vida.adolescencia`, `etapa_vida.juventud`, `etapa_vida.adultez`, `etapa_vida.madurez` y `etapa_vida.vejez`. Las fotos antes de decidir se sacan en los eventos con `"dia_clave": true`.

## Carrera (career.json)

Cada paquete de carrera tiene un `career.json` con sus **niveles de estudio**. El país no necesita conocer las carreras: un evento con `"opciones_carreras": true` (en Chile, `evento.elegir_carrera`, `evento.estudios.nueva_carrera` al cambiarse de carrera y `evento.estudios.volver_a_estudiar` de adulto) recibe una opción por cada nivel de cada carrera cargada.

```json
{
  "id": "carrera.software",
  "lugar_de_trabajo": "lugar_trabajo.oficina_abierta",
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
| `lugar_de_trabajo` | Opcional. Cómo se dibuja donde trabaja quien tiene el título: `lugar_trabajo.oficina_abierta` (mesas compartidas con pantallas), `lugar_trabajo.oficina_cubiculos` (cubículos y archivadores), `lugar_trabajo.consulta` (sillones frente a frente), `lugar_trabajo.tienda` (vitrina, percheros y caja), `lugar_trabajo.restoran` (barra y mesas puestas) o `lugar_trabajo.obra` (andamio, betonera y ladrillos). Sin él, una oficina común. Un tipo nuevo hay que dibujarlo en Unity |
| `marca_trabajo` | Opcional. Marca que recibe quien entra a trabajar en esta carrera al aceptar una oferta (por ejemplo `trabaja_software`), para los eventos de la carrera |
| `nombres_empresa` | Opcional. Claves de texto (empiezan con `empresa.`) para los nombres de las empresas de esta carrera. Cada empresa nueva toma uno que no use otra empresa abierta. Ver `economia.empresas` en country.json |
| `niveles[].id` | Empieza con el id de la carrera + `.nivel.`. Su texto es la opción ("Analista programador en un instituto…") y `<id>.titulo` el nombre del título |
| `niveles[].institucion` | `cft`, `instituto` o `universidad` |
| `niveles[].semestres` | Duración formal del plan. El núcleo suma un semestre aprobado cada 26 semanas de estudio |
| `niveles[].condiciones` | Quién puede entrar. Opcional |
| `niveles[].tendencia` | Qué tan atractivo es el nivel para quien decide solo (como `tendencia` en las opciones). Opcional |
| `niveles[].sueldo_relativo` | Cuánto gana un titulado de este nivel frente al sueldo típico de su país (`2.0` = el doble). Lo usa `sueldo_titulo()`. Si falta, 1 |
| `ramos` | La malla: los ramos de la carrera por semestre (ver abajo). Si una carrera tiene ramos, cada semestre de cada nivel tiene que tener al menos uno (la carga lo revisa) |

### Ramos y sección (campo `ramos` de career.json, rediseño R5a)

```json
"ramos": [
  { "id": "ramo.software.calculo", "desde": 1, "hasta": 4, "instituciones": ["universidad"] },
  { "id": "ramo.software.programacion", "desde": 1, "hasta": 2, "instituciones": ["universidad", "instituto"] },
  { "id": "ramo.software.memoria", "ultimo": true, "instituciones": ["universidad"] }
]
```

| Campo | Qué es |
|---|---|
| `id` | Empieza con `ramo.`; su texto en `lang/` es el nombre del ramo ("Cálculo") |
| `desde`, `hasta` | En qué semestres del plan se cursa. Con `hasta`, se numera como en la malla: Cálculo I, II, III y IV. Sin `hasta`, solo el semestre `desde` |
| `instituciones` | En qué tipo de institución se cursa (`cft`, `instituto`, `universidad`). Vacío: en todas. Así una carrera puede tener mallas distintas por nivel |
| `ultimo` | En vez de `desde`: el último semestre del nivel, sea cual sea su largo (la memoria, el seminario de título) |

Quien trabaja con contrato o informal tiene un **equipo** (`WorkSystem`): un jefe o una jefa y cinco colegas con nombre, que desde el primer día son colegas tuyos (`vincular('colega')`) y lo siguen siendo cuando dejas el trabajo (pueden recomendarte después). A honorarios se trabaja solo. Cada año alguien se va (`diario.pega.se_fue`, nunca tus amigos), a veces llega alguien (`diario.pega.llego_alguien`) y a veces cambia la jefatura (`diario.pega.jefe_nuevo`). Cada mes el cariño sube un punto, a los dos, con quien ya viviste un momento, y quien te quiere y con quien viviste 2 momentos se vuelve amistad (`diario.pega.amistad`). En las condiciones, `jugador.en_la_pega` dice si trabaja con equipo. Los momentos de todos los trabajos están en `packs/base/events/pega.json`.

Quien estudia tiene una **sección** (`UniversitySystem`): diez compañeros con nombre, de 18 años o algo mayores (algunos vienen de trabajar), y un profe o profesora del semestre. Los momentos pueden ser `con` `companero` o `profe`, como en el colegio. Al cerrar cada semestre se pone nota a cada ramo; dependen de lo mismo que en el colegio, cuestan un poco más y de cuánto se preparó (`jugador.estudiar_semestre(n)` en los momentos). Si queda un ramo rojo, el semestre no cuenta y se vuelve a cursar (`diario.estudios.ramo_reprobado`): no hace falta un evento al azar para reprobar. Cada año llega un profe nuevo, y a veces alguien de la sección no sigue (`diario.seccion.se_fue`): su lugar no siempre se llena, así que la sección se achica con los años (nunca baja de seis con nombre).

Lo que pasa mientras se estudia (ramos, práctica, titulación) va en los eventos del paquete. Al titularse, el evento de la carrera pregunta a qué área postular (`jugador.postular_en('área')`); el trabajo llega después, buscándolo (ver `economia.busqueda_trabajo`). La titulación la decide un evento con `jugador.titularse()`: así cada carrera pone sus requisitos (por ejemplo, la práctica). La duración real suele ser mayor que la formal: los ramos rojos ya la alargan (ver "Ramos y sección"), y un evento también puede restar semestres con `jugador.semestres_aprobados -= 1` (botar un ramo).

### Áreas y cargos (campos `areas` y `cargos` de career.json)

Toda carrera tiene **al menos tres áreas** (regla 3 del diseño; el núcleo rechaza el paquete si tiene menos) y una **escalera de cargos** del inicial a la cima. Un área puede tener su propia escalera (en contabilidad, auditoría llega a socio); si no, usa la de la carrera.

```json
"areas": [
  { "id": "carrera.software.area.desarrollo", "sueldo_relativo": 1.0 },
  { "id": "carrera.software.area.ciberseguridad", "sueldo_relativo": 1.2,
    "requisitos": "jugador.titulado_en('carrera.software.nivel.ingenieria_civil_computacion') || jugador.tiene('certificacion_ciberseguridad')" }
],
"cargos": [
  { "id": "carrera.software.cargo.junior" },
  { "id": "carrera.software.cargo.semi_senior", "sueldo_relativo": 1.3, "meses_minimos": 18, "desempeno_minimo": 50 },
  { "id": "carrera.software.cargo.senior", "sueldo_relativo": 1.7, "meses_minimos": 24, "desempeno_minimo": 55,
    "siguientes": ["carrera.software.cargo.lider_tecnico", "carrera.software.cargo.jefe_de_equipo"] },
  { "id": "carrera.software.cargo.cto", "sueldo_relativo": 3.4, "meses_minimos": 48, "desempeno_minimo": 65, "siguientes": [] }
]
```

| Campo | Qué es |
|---|---|
| `areas[].id` | Empieza con el id de la carrera + `.area.`; es también la clave de su nombre en `lang/` |
| `areas[].requisitos` | Condición extra para trabajar en el área (certificación, diplomado, título universitario). Siempre hace falta, además, un título de esa carrera. Opcional |
| `areas[].sueldo_relativo` | Cuánto paga el área frente al sueldo del título. Si falta, 1 |
| `areas[].cargos` | Escalera propia del área. Opcional |
| `cargos[].id` | Empieza con el id de la carrera + `.cargo.`; también es la clave de su nombre. Cada escalera usa ids propios |
| `cargos[].sueldo_relativo` | Cuánto paga el cargo frente al sueldo del título. Si falta, 1. El sueldo de un cargo es `sueldo_titulo()` × área × cargo |
| `cargos[].meses_minimos` | Meses en el cargo anterior que pide el ascenso a este. Si falta, 0 |
| `cargos[].desempeno_minimo` | Desempeño (0 a 100) que pide el ascenso. Si falta, 50 |
| `cargos[].requisitos` | Otros requisitos del ascenso: formación, personalidad (`jugador.extraversion >= 35`), contactos. Opcional |
| `cargos[].siguientes` | A qué cargos se asciende desde este. Si falta, al siguiente de la lista; `[]` es la cima; dos o más dividen la carrera en caminos (técnico o de gestión). A cada cargo se llega por un solo camino |

Reglas del núcleo, iguales para toda carrera:

- **Dónde se empieza:** quien entra a un área sin experiencia, en el primer cargo. Con experiencia (su cargo actual o el último que tuvo en la carrera, aunque haya perdido el trabajo): en la misma área, en el mismo cargo; en otra área, un cargo más abajo (es el costo de cambiarse).
- **Experiencia y desempeño:** cada mes en el cargo suma un mes de experiencia, y el desempeño se acerca 1 punto a lo que rinde su personalidad (35 + la mitad de su responsabilidad). Los eventos lo suben y lo bajan.
- **Ascender:** se puede cuando cumple los meses, el desempeño y los requisitos de algún cargo siguiente. Lo da un evento (`jugador.ascender()`), así el contenido decide la probabilidad, la crisis económica o si la persona lo rechaza.

Los eventos de ascenso comunes a toda carrera (oferta de ascenso, mala racha, bajar de cargo, despido, cima) están en el paquete del país (`events/ascensos.json`); los de cada área, en su paquete de carrera (`events/areas.json`).

### Oficios (bloque `oficio` de career.json, rediseño R6b)

Un **oficio** es una carrera que se trabaja sin título: vendedor de tienda, garzón, maestro de la construcción (`packs/oficios/`). Su id empieza con `oficio.` (no `carrera.`), no tiene `niveles` y cualquiera que cumpla los `requisitos` del área puede entrar (normalmente `jugador.edad >= 18`). Tiene áreas y cargos como cualquier carrera y, además:

```json
"oficio": {
  "sueldo_base": 1.0,
  "sueldo_base_por_pais": { "pais.argentina": 3.0 },
  "pago_variable": "comision",
  "variable_tipico": 0.75,
  "temporada": [0.85, 0.85, 0.95, 0.9, 1.05, 1.0, 0.95, 0.95, 1.05, 0.95, 1.1, 1.45],
  "jefatura": "oficio.vendedor.jefatura"
}
```

| Campo | Qué es |
|---|---|
| `sueldo_base` | El sueldo del primer cargo, en veces el sueldo mínimo del país (en un oficio el título no cuenta). El de cada cargo es este × área × cargo. Si falta, 1 |
| `sueldo_base_por_pais` | El mismo número para un país donde el oficio paga distinto frente a su mínimo (los convenios de Argentina). Opcional |
| `pago_variable` | `comision` o `trato` (entran en la liquidación: son imponibles) o `propina` (la pagan los clientes: no es sueldo, llega directo a su dinero). Opcional |
| `variable_tipico` | Lo variable de un mes en que cumple justo la meta, frente al sueldo (0,75 = tres cuartos del sueldo). Si falta, 0 |
| `temporada` | Cuánto se mueve el trabajo cada mes, de enero a diciembre (diciembre en el comercio, el invierno en la obra). Cambia lo que se gana, no la meta. Opcional |
| `jefatura` | Clave del nombre de la jefatura: sus textos son `<clave>.f` y `<clave>.m` (en la pantalla del equipo: "Jefa de tienda") y `<clave>.momento.f` y `<clave>.momento.m` (en los momentos `con` `jefe`: "tu jefa de tienda, {0},"). Opcional |

Reglas del núcleo (`OccupationSystem`), iguales para todo oficio:

- **La meta del mes:** al empezar cada mes se cierra el mes anterior. Se cumple, en porcentaje, 66 + 0,8 × desempeño (106 % con desempeño 50), más o menos 12 de suerte, por la economía (en una crisis, la mitad de lo que baja la contratación). Un mes de 125 % o más sube el desempeño en 1; uno bajo 85 %, lo baja en 1.
- **Lo variable:** sueldo × `variable_tipico` × cumplimiento × temporada. La comisión y el trato suman al sueldo bruto de la liquidación de ese mes; las propinas llegan a su dinero.
- **Bajo la meta:** el núcleo solo cuenta los meses seguidos bajo la meta (`jugador.meses_bajo_meta`). Los eventos deciden qué pasa (en `packs/base/events/oficios.json`: llamado de atención a los 3 meses, amonestación por escrito 2 meses después y despido 2 meses después de eso), con `jugador.amonestaciones`. Seis meses seguidos cumpliendo dejan atrás una amonestación.
- **Desafío antes de ascender:** un cargo puede pedir en sus `requisitos` una marca de desafío (`jugador.tiene('desafio_vendedor_encargado')`). Un momento la ofrece con `jugador.desafio('desafio_vendedor_encargado')`: si ese mes cierra sobre el 118 % de la meta, gana la marca (`diario.oficio.desafio_logrado`); si no, `diario.oficio.desafio_fallido` y lo puede volver a intentar.
- **Cómo se llega:** cada oficio tiene un evento de aviso ("Se busca vendedor/a") para quien busca trabajo sin título, o con título y más de medio año buscando, que lo hace postular a un área con `jugador.postular_en`.

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

**Dónde va cada evento:** los que le pueden pasar a cualquiera (amigos, pareja, salud, trabajo, ascensos, empresas, jubilación por ley) van en `packs/base/events/`. Los propios de un país (la PAES, la gratuidad, el CAE, un temblor) van en el paquete de ese país, y **solo le pasan a quien vive en ese país**: no hace falta escribir `jugador.pais == ...` en sus condiciones. Los eventos de las carreras le pueden pasar a quien estudia o trabaja en ellas, en cualquier país. Para que un evento común funcione en un país nuevo, ese país debe definir las leyes que el evento usa (`ley(...)`) y marcar los mismos hitos (`decidio_futuro`, `buscando_trabajo`, `trabajando`). Las leyes, deudas, fases del ciclo y regímenes de inflación que nombra un evento de un país solo tienen que existir en ese país; los del paquete base, en todos.

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
| `veces_maximas` | Cuántas veces como máximo puede pasar en la vida (`unico` es lo mismo que 1). No lo uses en eventos que sacan al jugador de un problema (por ejemplo, salir de DICOM) ni en los buenos momentos de toda la vida: la vejez se queda vacía | sin tope |
| `por_persona` | Con `con`: el tope de `veces_maximas` y las `variantes` se cuentan con cada persona. Con tu pareja de hoy el aniversario empieza desde su primer texto, aunque con la anterior ya hayas vivido todas sus formas; y nunca se lee dos veces igual con la misma persona | false |
| `variantes` | Otras claves de texto para contar la escena cuando vuelve a pasar: la 1.ª vez se usa `texto`, la 2.ª la primera variante, y así; al acabarse vuelven a empezar. Las opciones son las mismas, así que cada variante debe calzar con ellas. Convención: `<id>.2`, `<id>.3`… | — |
| `probabilidad_anual` | Probabilidad real de que ocurra en un año (0 a 1), por ejemplo `0.04` para un robo según ENUSC. El evento no compite por `peso`: cada semana tiene su propia oportunidad. Úsala cuando haya una estadística | — |
| `efectos` | Se aplican apenas ocurre | — |
| `opciones` | Lo que puede elegir el jugador; cada una con `texto`, `condiciones` y `efectos` | — |
| `opciones[].tendencia` | Qué tan atractiva es la opción para quien decide solo (el simulador y, más adelante, los personajes del mundo), como número de 0 en adelante. Ejemplo: `"jugador.amabilidad"` o `"(100 - jugador.responsabilidad) / 3"`. El jugador no la ve. Si falta, vale 50 | 50 |
| `opciones_carreras` | Agrega una opción por cada nivel de carrera, antes de las propias (ver Carrera) | false |
| `con` | Con quién pasa: `madre`, `padre`, `pareja`, `hijo`, `amigo`, `colega`, `rival`, `ex`, `companero` (alguien de tu curso: se elige más seguido a quien quieres más), `apoderado` (quien va a las reuniones del colegio: tu mamá, o tu papá si no está; en los textos `{persona}` dice "tu mamá" o "tu papá") o `profe` (tu profe jefe; en los textos `{persona}` dice "la profe Carolina" o "el profe Juan", así que no la pongas al empezar una frase), `jefe` (tu jefe o jefa del trabajo; `{persona}` dice "tu jefa Carolina" o "tu jefe Juan"), `equipo` (un colega de tu equipo de hoy: se elige más seguido a quien se quieren más) `companero_casa` (el amigo que vive contigo y comparte el arriendo) o `socio` (tu socio en el negocio). En un evento de `semilla` también puede ser `testigo` (rediseño R7d): pasa con quien vio lo que hiciste, el testigo vivo más antiguo de la semilla (el amigo al que le prestaste plata, quien te recomendó), y `{persona}` dice su nombre (o "tu mamá", "tu papá"). Si no queda ningún testigo vivo, no pasa. Un evento de semilla respeta, como cualquier otro, su `unico`, sus `veces_maximas` y su `enfriamiento_semanas`. Al ocurrir se elige a una persona de verdad, con nombre (si no hay nadie así, no pasa). En el evento se le habla como `persona` (`persona.carino += 3`, `plantar('semilla.x', persona)`) y en los textos de la escena y de las opciones `{persona}` se cambia por su nombre. En el diario no se usa `{persona}` | — |
| `lugar` | Dónde pasa: clave `lugar.<nombre>` con su texto en `lang/` ("La plaza", "Tu sala"). La pantalla lo muestra junto a la persona | — |
| `actividad` | El momento nace de tu horario: solo pasa si esa actividad está en tu semana. Pide `probabilidad_anual`, que es la probabilidad en un año haciéndola **una vez por semana** (si la haces tres veces, es más probable) | — |
| `regla` | Grupo para las reglas propias del jugador (`regla.prueba`, con su texto en `lang/`: "Cuando hay prueba o tarea"). Cuando el jugador elige "hacer esto siempre", la regla sirve para todos los eventos del grupo. Sin grupo, sirve solo para este evento. Un `dia_clave` y un evento de `semilla` nunca admiten regla: se viven cada vez | — |
| `opciones[].regla` | Qué es la opción para las reglas del grupo (`estudiar`, `copiar`): opciones de eventos distintos con el mismo valor son "lo mismo". Pide `regla` en el evento | su texto |
| `{ramo}`, `{nota}` | En los textos de un evento se cambian por el ramo de la prueba anunciada o devuelta ("Matemática") y su nota ("5,8") | — |
| `{empresa}` | En los textos de la búsqueda de trabajo (entrevista y oferta) se cambia por la empresa que te llamó ("Moda Cordillera"): una de `nombres_empresa` de la carrera u oficio si postulas a un área, o de `economia.busqueda_trabajo.empresas` del país si buscas cualquier trabajo. Si no hay nombres, dice "una empresa". Al aceptar, entras a esa empresa | — |
| `a_pedido` | El momento lo **pide el jugador** con un botón (rediseño R6c): `{ "texto": "accion.pega.pedir_aumento", "por_ano": "0.2 + jugador.extraversion / 400" }`. Nunca pasa solo: aparece como botón mientras se cumplan sus `condiciones` (y `enfriamiento_semanas`, y haya alguien para `con`) y, al apretarlo, pasa ahí mismo. `texto` es la clave del botón y empieza con `accion.`; `por_ano` dice cuántas veces al año lo pediría alguien con esa personalidad, solo para el simulador de vidas. No lleva `actividad`, `fecha`, `probabilidad_anual` ni `semilla`, y necesita `con` o `lugar`. Los de `lugar.trabajo` salen en la pantalla de tu equipo; los de `lugar.dinero` salen en Tu dinero, en "Lo que decides tú" (revisar la compra, el plan, el transporte, repactar una deuda, irte de la casa: R7b, `LifePresenter.MoneyActions()`); los demás con `con` salen como botones en la línea de cada persona de "Tu gente" que calce (futuros juntos, R6d): recomendarla en tu pega, proponerle vivir juntos. En el código: `LifeSession.Requestable()`, `LifeSession.Request(id)`, y con una persona `LifeSession.RequestableWith(personId)` y `LifeSession.Request(id, personId)` | — |
| `fecha` | El momento tiene fecha: `"24-12"` (día-mes) pasa la semana de ese día, y `"cumpleanos"` la semana del cumpleaños de la persona de `con`. Pasa como mucho una vez al año. Con `probabilidad_anual`, esa es la probabilidad de que ese año se viva como momento | — |

Un evento sin opciones se resuelve solo. Si lo provocó una semilla, la semilla se cierra al terminar el evento.

### Momentos (rediseño R2b)

Regla del rediseño (`docs/rediseno-vida.md`): **un momento detiene el tiempo solo si tiene persona o lugar con nombre y algo en juego**. Por eso un evento con `actividad` o `fecha` debe tener `con` o `lugar` (la carga lo revisa). Ejemplo, en `packs/base/events/rutina.json`:

```json
{
  "id": "evento.rutina.fuente_quebrada",
  "actividad": "actividad.ayudar_casa",
  "con": "madre",
  "lugar": "lugar.casa",
  "condiciones": "jugador.edad >= 8 && jugador.edad < 15",
  "probabilidad_anual": 0.6,
  "unico": true,
  "opciones": [
    { "texto": "opcion.rutina.fuente_quebrada.contar", "efectos": ["persona.confianza += 4"] },
    { "texto": "opcion.rutina.fuente_quebrada.esconder", "efectos": ["plantar('semilla.fuente_escondida', persona)"] }
  ]
}
```

Cada semana se prueba, en este orden: lo que despierta una semilla, los momentos con fecha de esta semana, los momentos de tu horario, los eventos con `probabilidad_anual` y, al final, uno al azar por `peso`. Los momentos tienen su propio azar, así que agregar uno no cambia qué otros eventos salen. Regla de repeticiones (R7): un evento no se vuelve a leer igual con la misma persona (o sin persona). Usa `con` para que hable de alguien con nombre, `unico` o `veces_maximas` con `variantes` (y `por_persona` si el tope es con cada persona), para que un cumpleaños no se lea igual cada año. El simulador de vidas cuenta lo que se rompe en la sección "Repeticiones sin razón": el mismo texto con la misma persona o sin persona (lo que pide el jugador con un botón no cuenta).

## Capítulos del año (chapters/)

Cada año de la vida se cierra como un **capítulo del diario** con título propio. El título no se inventa: lo da una línea del diario que de verdad se escribió ese año. Cada archivo de `chapters/` es una lista:

```json
[
  { "diario": "diario.muerte_madre", "titulo": "capitulo.muerte_madre", "peso": 10 },
  { "diario": "diario.amigo_se_va", "titulo": "capitulo.amigo_se_va", "peso": 6 }
]
```

| Campo | Qué es |
|---|---|
| `diario` | La clave de la línea del diario (tiene que existir en `lang/`) |
| `titulo` | La clave del título en `lang/` ("El año en que murió mi mamá"). Puede usar `{persona}`: el nombre de la persona ligada a una semilla plantada ese mismo día; si no hay nadie, esa línea no titula el año |
| `peso` | 1 a 10: cuánto marca el año. 10 es lo que cambia la vida (nacer, morir alguien cercano); 1, algo que apenas se nota |

Reglas:

- Gana la línea de más peso del año; si empatan, la más nueva.
- Un mismo título (su texto) sale **una sola vez por vida**: si ya tituló un año anterior, gana la siguiente línea de ese año.
- Si ninguna línea del año da título, el año se llama por la edad ("El año en que cumplí 9"; el primero, "El año en que nací").
- Pon cada título en el paquete donde vive su línea del diario, y escribe los textos en primera persona y sin palabras de un solo país en `base` (las variantes `es-AR` pueden cambiarlos).

## Noticias (news/)

El diario de noticias cuenta lo que le pasa al **país** (o a una ciudad): un temblor, un temporal, una crisis, unas elecciones, algo que se vuelve viral. Una noticia **no toca al jugador**: no le baja la salud ni le cobra nada. Solo puede cambiar lo del país (el ciclo económico, la inflación, una ley) y queda en el diario de noticias (en Unity, el diario mural). Lo que le pase al jugador lo decide un **evento** que lee la noticia con `noticia_reciente(...)`, y sus opciones dependen de lo que él eligió antes (si armó la mochila de emergencia, si arrienda, si tiene hijos). Así se cumple la regla de que nada se inventa: el mundo pasa, y lo que te pasa a ti es consecuencia de tus decisiones.

Las noticias de un paquete de país solo salen en ese país; las del paquete base, en cualquiera (deben usar solo leyes y datos que todos los países tengan). Cada semana, cada noticia tiene su propia oportunidad según su `probabilidad_anual`, sin quitarle el lugar a los eventos. El mundo recuerda las noticias de los últimos 12 años (así el guardado sigue liviano): una espera o un `noticia_reciente` más largo que eso no sirve.

```json
[
  { "id": "noticia.chile.sistema_frontal", "tema": "clima", "probabilidad_anual": 0.99, "enfriamiento_semanas": 4,
    "ciudades": ["ciudad.santiago"], "condiciones": "mundo.mes >= 5 && mundo.mes <= 8",
    "variantes": ["noticia.chile.sistema_frontal.2"] },
  { "id": "noticia.argentina.devaluacion", "tema": "economia", "probabilidad_anual": 0.15,
    "condiciones": "mundo.regimen_inflacion == 'economia.inflacion.alta'", "efectos": ["cambiar_inflacion('economia.inflacion.muy_alta')"] }
]
```

| Campo | Qué es | Por defecto |
|---|---|---|
| `id` | Id estable, empieza con `noticia.` | obligatorio |
| `texto` | Clave del titular | el mismo id |
| `variantes` | Otros titulares para cuando vuelve a salir en el mismo país (como en los eventos) | — |
| `tema` | `sismo`, `clima`, `economia`, `politica`, `sociedad`, `viral` o `salud`: la pantalla lo usa para el dibujo | obligatorio |
| `probabilidad_anual` | Probabilidad real de que pase en un año (mayor que 0, máximo 1). Ojo: con una condición de temporada (4 meses), la oportunidad semanal es la misma, así que pasa menos en el año; `1` hace que salga la primera semana en que se cumple la condición | obligatorio |
| `condiciones` | Cuándo puede pasar. **Solo puede leer el mundo** (`mundo.mes`, `mundo.ciclo`, `mundo.inflacion`, `ley(...)`, `noticia_reciente(...)`): usar `jugador` es un error | siempre |
| `enfriamiento_semanas` | Semanas mínimas antes de que vuelva a salir en el mismo país | 0 |
| `ciudades` | Si se pone, solo sale para quien vive en esas ciudades (un temporal en Santiago). Si no, es de todo el país | todo el país |
| `efectos` | Solo `cambiar_ciclo`, `cambiar_inflacion` o `cambiar_ley`. Cambiar al jugador es un error | — |

**Un evento que sigue a la noticia:** `"condiciones": "noticia_reciente('noticia.chile.terremoto', 2) && jugador.edad >= 4"` con `"probabilidad_anual": 1` pasa la primera semana después de la noticia. Para que pase un tiempo después (una semana después del temblor, en la tele recomiendan armar la mochila): `noticia_reciente('noticia.chile.temblor', 6) && !noticia_reciente('noticia.chile.temblor', 1)`. Lo rutinario (un temblor más, otras Fiestas Patrias) puede quedar solo en el diario de noticias: el evento personal lleva `veces_maximas` y la noticia sigue saliendo.

## Objetos de la escena (scene/)

Para que la escena 3D cambie con la vida, un paquete puede traer objetos en `scene/*.json` (una lista). Cada objeto aparece mientras su condición se cumpla para el jugador; no lleva texto para el jugador. La pantalla dibuja los ids que conoce (en Unity, `SceneObjectBuilder`) y salta los demás, así un objeto nuevo en un paquete nunca rompe el juego: solo no se ve hasta que se le da forma.

```json
[
  { "id": "escena.casa.libros", "lugar": "casa", "condiciones": "jugador.estudia != nada" },
  { "id": "escena.casa.cuentas", "lugar": "casa", "condiciones": "jugador.moroso || jugador.meses_en_rojo >= 3" }
]
```

| Campo | Qué es |
|---|---|
| `id` | Id estable: `escena.<lugar>.<nombre>` |
| `lugar` | `casa`, `barrio` o `trabajo` |
| `condiciones` | Cuándo se ve (obligatorio). Las decisiones se pueden conectar con marcas: un efecto `jugador.marcar('tiene_perro')` y un objeto con `jugador.tiene('tiene_perro')` |

Los objetos que vienen de las noticias (paquete de Chile): `escena.casa.mochila_emergencia` (armó la mochila), `escena.casa.grietas` (dejó la casa sin reparar después del terremoto), `escena.casa.velas` (temporal de las últimas 2 semanas) y `escena.barrio.escombros` (terremoto de los últimos 3 meses).

Los objetos de la casa del paquete base: libros, mochila, diploma, notebook, avisos de trabajo, cartas de rechazo (desde 2 entrevistas sin quedar), ropa de entrevista (con una entrevista u oferta pendiente), celular (si tiene) y su aviso encendido (`escena.casa.celular_avisos`, con correos sin leer), cuentas impagas, remedios, flores, desorden, juguetes, foto con la pareja, foto del recuerdo y mecedora. Además, Unity nubla el cielo cuando la felicidad baja.

## Celular y correo (apps/ y mail/)

El celular es un objeto de la vida: alguien se lo da (los papás, con `jugador.dar_celular()`), se compra, se pierde o se lo roban (`jugador.perder_celular()`). Adentro tiene apps; las de un paquete van en `apps/*.json` (una lista) y siempre son parodias, nunca una marca real:

```json
[
  { "id": "app.correo", "de_fabrica": true },
  { "id": "app.instante" }
]
```

| Campo | Qué es |
|---|---|
| `id` | Id estable: `app.<nombre>`. Es también la clave del nombre de la app en lang/ |
| `de_fabrica` | Si viene en todo celular nuevo (las demás se instalan con `jugador.instalar_app('app.x')`) |

El **correo** es una cuenta, no está en el aparato: si pierde el celular, el correo sigue. Se crea con `jugador.crear_correo()` y desde ese día le llegan los avisos de su vida. Los correos de un paquete van en `mail/*.json` (una lista):

```json
[
  { "id": "correo.busqueda.rechazo", "remitente": "remitente.seleccion", "tema": "trabajo", "al_anotar": ["diario.busqueda.rechazo"] },
  { "id": "correo.estafa.banco", "remitente": "remitente.banco_falso", "tema": "estafa" }
]
```

| Campo | Qué es |
|---|---|
| `id` | Id estable: `correo.<tema>.<nombre>`. Es la clave del cuerpo del correo; el asunto es `id + ".asunto"` |
| `remitente` | Clave de texto de quien lo manda: `remitente.<nombre>` |
| `tema` | `trabajo`, `estudio`, `banco`, `estado`, `juzgado`, `personal`, `publicidad` o `estafa` (la pantalla le pone su ícono) |
| `al_anotar` | Líneas del diario que lo hacen llegar. Así ningún sistema ni evento necesita saber del correo: cuando algo anota `diario.busqueda.rechazo`, al final de esa semana llega el correo de rechazo a quien tiene cuenta. Si falta, solo llega con `jugador.recibir_correo('id')` |
| `condiciones` | Opcional. Condición sobre quien lo recibe, revisada en el momento en que llegaría (por ejemplo `jugador.tiene_app('app.instante')` para el saludo de cumpleaños de Instante). Si no la cumple, ese correo no llega |
| `cada` | Opcional, en vez de `al_anotar`: `"mes"` (primera semana de cada mes), `"cuatrimestre"` (enero, mayo y septiembre) o `"anio"`. Obliga a poner `condiciones`, porque un correo periódico siempre viene de algo que el personaje tiene (regla "nada se inventa"): la liquidación con `jugador.trabaja && jugador.tipo_trabajo == 'dependiente'`, el estado de cuenta con `jugador.tiene('tarjeta_credito')`, la cartola de la AFP con `jugador.meses_cotizados > 0` |
| `en_mes` | Con `"cada": "anio"`, el mes (1 a 12) en que llega, en su primera semana. Ejemplo: la inscripción de asignaturas en marzo (3) y agosto (8) para quien estudia |

Lo que pasó antes de tener correo nunca llega. Sin cuenta, los correos no llegan (pero el diario sí se escribe). La bandeja guarda los últimos 300 correos. Unity marca un correo como leído con `LifeSession.ReadMail(posición)`.

## Mensajito (chats/)

Mensajito es la app de mensajes del celular. **Nadie inventado escribe**: cada mensaje lo manda una persona real de la vida del jugador (su mamá, su pareja, un amigo que conoció en un evento, un ex), y solo si siente lo que el mensaje pide. Llega por algo que pasó en la vida del jugador (una línea de su diario, como su cumpleaños) o, de vez en cuando, porque esa persona se acordó de él. Los mensajes de un paquete van en `chats/*.json` (una lista):

```json
[
  { "id": "chat.madre.cumpleanos", "de": "madre", "al_anotar": ["diario.cumpleanos"], "carino_minimo": 30,
    "respuestas": [
      { "id": "chat.madre.cumpleanos.resp.te_quiero", "carino": 2 },
      { "id": "chat.madre.cumpleanos.resp.gracias" }
    ],
    "si_no_respondes": { "carino": -3 } },
  { "id": "chat.amigo.juntarse", "de": "amigo", "probabilidad": 0.04, "carino_minimo": 45, "condiciones": "jugador.edad >= 13",
    "respuestas": [ { "id": "chat.amigo.juntarse.resp.dale", "carino": 3 } ] }
]
```

| Campo | Qué es |
|---|---|
| `id` | Id estable: `chat.<quien>.<nombre>`. Es la clave del texto del mensaje en lang/ |
| `de` | Quién lo escribe: `madre`, `padre`, `pareja`, `hijo` (hijos e hijas), `amigo`, `colega`, `rival` o `ex`. Si hay varios (amigos, hijos), escribe cada uno que cumpla lo pedido; por una línea del diario, como mucho los 5 que más quieren al jugador |
| `al_anotar` | Líneas del diario **del jugador** que lo hacen llegar al final de esa semana |
| `probabilidad` | En vez de `al_anotar`: la probabilidad (0 a 1) de que cada persona lo escriba en una semana. Nadie escribe así si espera que le contesten, ni repite el mismo mensaje en medio año, ni manda más de uno por semana |
| `carino_minimo`, `carino_maximo` | Lo que la persona siente por el jugador (0 a 100). Con `carino_maximo` se escriben mensajes de alguien que se enfrió ("hace siglos que no sé de ti") |
| `rencor_minimo`, `rencor_maximo` | Igual con el rencor (el rival que manda una indirecta; el ex dolido no saluda) |
| `edad_minima`, `edad_maxima` | La edad de quien escribe (un hijo en el colegio, un hijo ya adulto). Nadie escribe antes de los 12 |
| `condiciones` | Opcional. Condición sobre el jugador (`jugador.trabaja`, `jugador.edad < 18`) |
| `respuestas` | Lo que el jugador puede contestar, cada una con su `id` (clave del texto) y cuánto cambia en quien escribió: `carino`, `confianza`, `respeto`, `rencor`. Sin respuestas, el mensaje no espera nada (una cadena reenviada) |
| `si_no_respondes` | Lo que cambia si el jugador no contesta en 2 semanas (lo dejó en visto) |

Sin la app no llega nada, y lo que pasó antes de tenerla no llega después. Las conversaciones son una cuenta (como el correo): si pierde el celular, vuelven al tener la app otra vez. Unity las lee con `LifePresenter.Chats()` y `Chat(id)`, y contesta con `ReplyChat(id, respuesta)`.

### Escribir tú primero (escribir/)

El jugador también puede escribirle primero a la gente de su vida ("Nuevo chat"). Lo que puede escribirle a cada uno va en `escribir/*.json` (una lista). **La otra persona contesta según lo que siente por él**: la semana siguiente le llega la primera respuesta cuyo cariño y rencor calcen con lo que esa persona siente; si ninguna calza, lo deja en visto.

```json
[
  { "id": "escribir.madre.como_estas", "para": "madre", "condiciones": "jugador.edad >= 12",
    "respuestas": [
      { "id": "escribir.madre.como_estas.resp.feliz", "carino_minimo": 40, "rencor_maximo": 50, "carino": 2 },
      { "id": "escribir.madre.como_estas.resp.seca", "rencor_maximo": 70, "carino": 1 }
    ] }
]
```

| Campo | Qué es |
|---|---|
| `id` | Id estable: `escribir.<quien>.<nombre>`. Es la clave del texto que manda el jugador |
| `para` | A quién se le puede escribir: los mismos roles que `de` en `chats/` |
| `carino_minimo`, `carino_maximo`, `rencor_minimo`, `rencor_maximo` | Opcionales. Lo que debe sentir esa persona para que el jugador piense en escribirle eso (pedir perdón solo si hay rencor) |
| `edad_minima`, `edad_maxima` | La edad de la otra persona |
| `condiciones` | Opcional. Condición sobre el jugador |
| `respuestas` | Obligatorio, al menos una. En orden: gana la primera cuyo `carino_minimo/maximo` y `rencor_minimo/maximo` calcen con lo que siente; su `id` es la clave del texto de la respuesta y `carino`, `confianza`, `respeto`, `rencor` lo que cambia en esa persona. Si ninguna calza, lo deja en visto (no cambia nada) |

No se puede escribir a quien espera que le contestes, ni dos veces sin respuesta, ni repetir lo mismo en medio año, ni mientras hay un evento por decidir. Unity las lee con `LifePresenter.ChatContacts()` y `Chat(id).Openers`, y escribe con `WriteChat(id, idDelMensaje)`.

## Tu horario (actividades/)

Lo que el jugador puede pegar en su horario (rediseño R2, `docs/rediseno-vida.md`). El horario tiene 14 bloques: la tarde y la noche de cada día, de lunes a domingo. La mañana no se llena: la ponen el colegio, los estudios o el trabajo. Desde los 6 años, cada vez que cambia de etapa (por ejemplo, al entrar a estudiar) y, en el colegio, cada año el día que empiezan las clases (`inicio_ano_escolar` del país), el juego le sugiere un horario y detiene el tiempo para que lo arme. El sugerido mantiene casi todo lo del horario anterior que todavía se puede hacer (70 %) y elige lo demás según su forma de ser. Después la vida sigue sola con él.

Cada archivo es una lista:

```json
{ "id": "actividad.plaza_amigos", "edad_minima": 8, "edad_maxima": 17, "franjas": ["tarde"], "con": "amigo",
  "aporta": { "animo": 2, "salud": 1, "carino": 1 }, "tendencia": "jugador.extraversion" }
```

| Campo | Qué es |
|---|---|
| `id` | `actividad.<nombre>`; es también la clave de su nombre en `lang/` (corto, como en un sticker: "plaza con los amigos") |
| `edad_minima`, `edad_maxima` | Edades en que tiene sentido |
| `condiciones` | Condición extra en el lenguaje de condiciones (por ejemplo `jugador.tiene_celular`) |
| `franjas` | `tarde`, `noche` o las dos (por defecto, las dos) |
| `con` | Con quién se hace: `madre`, `padre`, `pareja`, `hijo`, `amigo`, `colega`, `rival`, `ex`, `companero` o `profe`. Solo se puede elegir si esa persona existe, y el cariño se mueve con ella |
| `aporta` | Cuánto mueve por cada bloque en que está, de -3 a 3: `animo`, `mente`, `salud`, `carino` (este último necesita `con`) |
| `tendencia` | Qué tan probable es que la elija alguien sin el jugador (simulador, horario sugerido); por defecto 50 |

Cómo pesa (`RoutineSystem`):

- **Ánimo:** cada semana se acerca un punto a `50 + 2 × (suma de animo)`, entre 15 y 85. Así el ánimo no se queda pegado en 0 ni en 100: los eventos lo sacuden y el horario lo devuelve a lo que tu vida da.
- **Mente:** cada mes se acerca un punto a `50 + 2 × (suma de mente)`, entre 15 y 90. Estudiar sirve, pero de a poco; dejarlo también se nota.
- **Salud:** cada mes sube o baja un punto si la suma de salud es 4 o más (o -4 o menos). El envejecimiento hace el resto.
- **Cariño:** cada mes, con cada persona de `con`, sube lo que diga `carino` en los dos sentidos (lo que tú sientes y lo que siente esa persona).
- Si una actividad deja de tener sentido (ya no hay amigos, pasó la edad), queda en el horario pero no cuenta hasta que la cambies.

## Lo que quiero (deseos/)

Rediseño R2c. En cada etapa del horario (cada año escolar, la universidad, un trabajo) el personaje quiere hasta 3 cosas que **nacen de su vida**: se eligen entre los deseos cuyas condiciones se cumplen y que todavía no tiene. El jugador marca con destacador los que persigue: el horario sugerido pone más seguido su `actividad`. Cada mes se revisa si se cumplió (ánimo +3, o +6 si lo perseguía, y su línea en el diario). Al cambiar de etapa, lo que perseguía y no pasó queda "perdido" (ánimo -3 y su línea en el diario); lo demás se le pasa sin pena. Lo mismo no se quiere dos etapas seguidas.

```json
{ "id": "deseo.amigo_de_verdad", "edad_minima": 6, "edad_maxima": 17,
  "condiciones": "jugador.amigos == 0", "cumplido": "jugador.amigos >= 1",
  "actividad": "actividad.plaza_amigos", "peso": 30 }
```

| Campo | Qué es |
|---|---|
| `id` | `deseo.<nombre>`; es la clave de su frase en `lang/` ("Tener un amigo de verdad"). También deben existir `<id>.cumplido` y `<id>.perdido`, las líneas del diario |
| `edad_minima`, `edad_maxima` | Edades en que tiene sentido |
| `condiciones` | De qué vida nace (si falta, de cualquiera) |
| `cumplido` | Cuándo se cumple (obligatorio). Si ya se cumple, el deseo no nace |
| `con` | Con quién es (como en los eventos): en las condiciones es `persona` y en la frase `{persona}` |
| `actividad` | La actividad del horario que ayuda: si lo persigue, se sugiere más |
| `peso` | Qué tan probable es frente a otros deseos (10) |
| `repetible` | Si se puede volver a querer en otra etapa después de cumplirlo (false) |

## Juzgados (courts/) y vida civil

Una **causa judicial** es un proceso que dura meses: tiene un RIT (por ejemplo `C-1532-81`), un tribunal y etapas con sus plazos, como la carpeta de cartulina del juzgado. Una causa **nunca empieza sola**: la abre un evento con `abrir_causa('causa.x')` por algo que hizo el jugador (dejó de pagar una deuda, se casó y se separó). Los tipos de causa van en `courts/*.json` del paquete de **un país** (cada país tiene sus tribunales y plazos):

```json
[
  { "id": "causa.chile.cobro_deuda", "tribunal": "tribunal.chile.civil", "prefijo_rit": "C",
    "etapas": [
      { "id": "demanda", "semanas": 6 },
      { "id": "requerimiento", "semanas": 1, "espera": true },
      { "id": "remate", "semanas": 12 }
    ],
    "resultados": ["avenimiento", "pago", "terminada"] }
]
```

| Campo | Qué es |
|---|---|
| `id` | Id estable: `causa.<país>.<nombre>`. Es también la clave de su nombre en lang/ |
| `tribunal` | Clave del nombre del tribunal: `tribunal.<país>.<nombre>` |
| `prefijo_rit` | Letra con que empieza su RIT o rol (`C` en lo civil y familia) |
| `etapas` | En orden; la causa empieza en la primera. `semanas` es su plazo. Si `espera` es false, al cumplirse el plazo pasa sola a la siguiente (o, si es la última, la causa termina como `terminada`). Si es true, se queda ahí hasta que un evento la mueva. El nombre de cada etapa es `<id de la causa>.etapa.<id de la etapa>` |
| `resultados` | Cómo puede terminar. Su texto es `<id de la causa>.resultado.<resultado>`. Si la última etapa no espera, debe estar `terminada` |
| `termina_si_muere_la_otra_parte` | Si la causa se cierra sola cuando muere la otra parte (un divorcio), como `fallecimiento` (que debe estar en `resultados`; su texto queda en el diario) |

**Un evento que sigue a la causa:** `"condiciones": "causa_en('causa.chile.divorcio', 'audiencia_preparatoria')"` con `"probabilidad_anual": 1` pasa la primera semana en que la causa está en esa etapa y ya se cumplió su plazo. Sus opciones mueven la causa (`causa('causa.x').avanzar()`, `.ir_a('sentencia')`) o la cierran (`.cerrar('divorcio')`).

**Vida civil:** `uniones_civiles` en country.json lista las uniones que reconoce la ley del país (en Chile, `union.chile.matrimonio` y `union.chile.auc`). `jugador.unirse('union.x')` une al jugador con su pareja, en ambos sentidos. Si después se separan (`jugador.terminar_pareja()`), **siguen unidos ante la ley** (`jugador.separado` es true) hasta que algo la termine con `jugador.terminar_union()`: la sentencia de divorcio, el aviso en el Registro Civil. Si muere el cónyuge, la unión deja de existir (`jugador.viudo`).

**Hijos después de separarse (cuidado personal):** mientras sus papás son pareja, los hijos viven con los dos. Si se separan, cada hijo menor queda **por acordar** (`jugador.hijos_por_acordar`) hasta que un evento decida: `jugador.quedarse_con_hijos()` (viven con el jugador), `jugador.hijos_con_el_otro()` (con su otro papá o mamá) o `jugador.cuidado_compartido()` (con los dos). `jugador.coprogenitor` es la otra parte (el otro papá vivo del hijo menor más chico de papás separados; si ya no hay papás separados, el otro papá de su hijo menor más chico, para que una decisión pendiente no quede sin persona). La compra del supermercado, la casa de la escena y los gastos por `jugador.hijos_en_casa` solo cuentan a los hijos que viven con el jugador. Si muere el papá con quien vivía un hijo, vuelve a vivir con el otro.

**Montos que se reajustan:** `causa('causa.x').monto` se escribe y se lee en dinero de hoy, pero se guarda en dinero del inicio del mundo: si suben los precios, el monto sube con ellos (como una pensión de alimentos reajustada por el IPC). Un gasto mensual que lo cobra usa `"a_precios_de_hoy": true`.

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
| `jugador.en_clases` | Si va a un curso y esa semana hay clases (no son vacaciones de verano ni de invierno) |
| `jugador.curso` | En qué año de su etapa va (1 a 8 en básica, 1 a 4 en media), o 0 si no va a un curso |
| `jugador.companeros` | Cuántos compañeros con nombre tiene en su curso |
| `jugador.prueba_anunciada` | Si hay una prueba anunciada para la semana que viene (la que se juega como momento). Con ella se puede usar `jugador.preparar_prueba(n)`, de 0 a 3: cada punto sube la nota unas 0,3 |
| `jugador.prueba_devuelta` | Si esta semana le devolvieron la prueba anunciada con su nota |
| `jugador.nota_prueba` | La nota de esa prueba, en la escala de 1 a 7 (en Argentina se lleva a esa escala), o 0 |
| `jugador.prueba_aprobada` | Si esa prueba quedó aprobada |
| `jugador.promedio` | Su promedio general del año que va (en vacaciones, el del año pasado), en la escala de 1 a 7, o 0 |
| `jugador.rojos` | En cuántos ramos va con promedio rojo este año |
| `jugador.repitio` | Cuántos años repitió |
| `jugador.semestres_cursados` | Semestres que cursó en la carrera que estudia, aprobados o no (la gratuidad mira estos) |
| `jugador.en_la_u` | Si estudia en la universidad, el CFT o el instituto y ya tiene sección (sus compañeros con nombre) |
| `jugador.en_la_pega` | Si trabaja (con contrato o informal) y ya tiene su equipo: jefe o jefa y colegas con nombre |
| `jugador.estudiar_semestre(n)` | Suma `n` (de -3 a 3 en total) a cuánto se prepara este semestre: estudiar en grupo suma, la fiesta antes del examen resta. Cada punto mueve la nota de los ramos unas 0,3. Se vuelve a 0 al cerrar el semestre (solo efectos, mientras estudia) |
| `jugador.madre`, `jugador.padre` | Otro personaje con los mismos datos (`jugador.madre.edad`), o `nada` si no hay |
| `jugador.pareja` | Su pareja actual (otro personaje: `jugador.pareja.edad`), o `nada`. Usa `jugador.pareja != nada && ...` antes de leer sus datos |
| `jugador.hijos` | Cuántos hijos vivos tiene (biológicos o adoptados) |
| `jugador.adoptado` | `true` si llegó a su familia por adopción |
| `jugador.formar_pareja()` | Crea una pareja de edad parecida (±4 años, mínimo 18), del mismo lugar, con nombre del país, del género que le atrae según su orientación (solo efectos). `jugador.formar_pareja('femenino')` la pide de un género. Para hijos biológicos, revisa `jugador.sexo != jugador.pareja.sexo` |
| `jugador.adoptar()` | Llega por adopción un niño o niña de 0 a 5 años; su pareja (si tiene) también es su madre o padre. Lleva el primer apellido de quien adopta y luego el de su pareja. Escribe `diario.adopcion_hija` o `diario.adopcion_hijo` (solo efectos) |
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
| `mundo.anio`, `mundo.mes`, `mundo.semana`, `mundo.semanas_totales` | Fecha del juego (mes de 1 a 12) |
| `jugador.dinero` | Su saldo, en la moneda del país donde vive. Se puede cambiar (`jugador.dinero += precio(20000)`) y puede quedar negativo |
| `precio(monto)` | Lleva un monto escrito en dinero del inicio del mundo a precios de hoy, en el país del jugador: `precio(15000)` |
| `mundo.inflacion` | Cuánto subieron los precios en los últimos 12 meses en el país del jugador, en porcentaje (`3.4` es 3,4 %) |
| `mundo.regimen_inflacion` | Régimen de inflación actual del país del jugador, por ejemplo `'economia.inflacion.alta'` |
| `mundo.ciclo`, `cambiar_ciclo('id')` | Fase del ciclo económico del país del jugador (`'economia.ciclo.crisis'`), y provocar otra (solo efectos) |
| `mundo.indice_precios` | Cuántas veces más cuesta todo que al empezar el mundo (1 al principio) |
| `jugador.trabajar(sueldo)`, `jugador.trabajar(sueldo, 'honorarios')`, `jugador.trabajar(sueldo, 'informal')` | Empieza un trabajo con ese sueldo bruto mensual, en dinero del inicio del mundo; sin tipo es `'dependiente'` (con contrato). `'informal'` es sin contrato: recibe todo el sueldo, pero no cotiza ni paga impuesto, y esos meses no suman a su pensión (lagunas). Reemplaza el trabajo anterior (solo efectos). Ejemplo: `jugador.trabajar(sueldo_titulo() * 1.1)` |
| `jugador.hijos_menores` | Cuántos hijos vivos menores de 18 tiene |
| `jugador.hijos_en_casa`, `jugador.hijos_con_otro` | Hijos menores que viven con el jugador (también los de cuidado compartido) o con su otro papá o mamá |
| `jugador.hijos_en_casa_entre(desde, hasta)` | Cuántos hijos menores que viven con el jugador tienen entre esas edades (años cumplidos, las dos incluidas) |
| `jugador.hijos_a_cargo`, `jugador.hijos_por_acordar` | Hijos menores de papás separados que viven solo con el jugador, o a los que aún no se les decide con quién viven |
| `jugador.coprogenitor` | El otro papá o mamá de sus hijos menores: de preferencia el de una pareja anterior (vivo); si no hay, el de su hijo menor más chico (o de su hijo más chico). `nada` si no tiene hijos con otra persona |
| `jugador.quedarse_con_hijos()`, `jugador.hijos_con_el_otro()`, `jugador.cuidado_compartido()` | Deciden con quién viven los hijos menores de papás separados (solo efectos) |
| `jugador.gastos`, `jugador.meses_en_rojo` | Lo que pagó en gastos el último mes, y cuántos meses seguidos terminó con el dinero en negativo |
| `arriendo('vivienda.pieza')` | Arriendo típico de un contrato nuevo de ese tipo en la comuna donde vive, en dinero del inicio del mundo (para comparar: `jugador.sueldo_liquido > precio(arriendo('vivienda.departamento')) * 2.5`) |
| `compra('compra.basica')` | Lo que le tocaría pagar al mes por esa compra, para los de su casa y a medias si tiene pareja, en dinero del inicio del mundo (para comparar: `jugador.sueldo_liquido > precio(compra('compra.con_antojos')) * 5`) |
| `jugador.elegir_compra('compra')` | Desde ahora hace esa compra de comida cada mes (solo efectos) |
| `tarifa('servicio.celular.plan')` | Lo que cuesta al mes esa opción de un servicio, en dinero del inicio del mundo |
| `jugador.contratar('opción')`, `jugador.cancelar_servicio('servicio')` | Contrata esa opción (cambia la que tenía del mismo servicio) o deja de pagar el servicio (solo efectos) |
| `jugador.servicio('servicio.celular')` | La opción que tiene contratada de ese servicio, o `nada` |
| `jugador.compra` | La compra de comida que eligió, o `nada` si todavía no decide |
| `jugador.arrendar('tipo')` | Firma un contrato de ese tipo de vivienda en su comuna: deja la garantía (y recupera la del contrato anterior, si tenía) y vive por su cuenta (solo efectos) |
| `jugador.renovar_arriendo()`, `jugador.dejar_arriendo()` | Renueva el contrato por otro plazo, o lo deja: recupera la garantía y vuelve a vivir con su familia (solo efectos) |
| `jugador.vivienda`, `jugador.arriendo`, `jugador.meses_contrato`, `jugador.cuentas_incluidas` | Tipo de vivienda que arrienda (o `nada`), lo que paga al mes a precios de hoy, los meses que le quedan al contrato y si el arriendo trae las cuentas |
| `jugador.declaracion_pendiente`, `jugador.declaracion_saldo` | Si tiene una declaración de impuestos por aceptar, y su saldo (positivo: devolución; negativo: a pagar) |
| `jugador.declarar()` | Acepta la declaración pendiente: recibe la devolución o paga (solo efectos) |
| `jugador.fondo_pension`, `jugador.pension_estimada` | Lo ahorrado en su cuenta de pensión y la pensión mensual que recibiría si se jubilara hoy, a precios de hoy |
| `jugador.jubilado`, `jugador.pension` | Si ya se jubiló (con `jubilarse()`), y lo que recibió de pensiones el último mes (propia + del Estado) |
| `jugador.pension_universal` | Si pidió la pensión del Estado (PGU) |
| `jugador.jubilarse()` | Convierte su fondo en una pensión mensual para siempre; solo una vez (solo efectos) |
| `jugador.recibir_pension_universal()` | Empieza a recibir la pensión del Estado de su país (solo efectos) |
| `jugador.endeudarse('tipo', monto)` | Pide prestado ese monto (en dinero del inicio del mundo) con un tipo de deuda de su país (solo efectos) |
| `jugador.deuda`, `jugador.deuda_de('tipo')` | Lo que debe hoy en total, o de un tipo de deuda |
| `jugador.repactar()`, `jugador.puede_repactar` | Repacta las deudas atrasadas que se pueden repactar (solo efectos), y si tiene alguna |
| `jugador.cuotas_deudas`, `jugador.moroso` | Lo que pagó en cuotas el último mes, y si está en el registro de morosos |
| `jugador.dejar_trabajo()` | Deja de trabajar y de recibir sueldo (solo efectos) |
| `jugador.trabaja`, `jugador.tipo_trabajo` | Si tiene trabajo; `'dependiente'`, `'honorarios'`, `'informal'` o `nada` |
| `jugador.trabajar_en('área')`, `jugador.trabajar_en('área', 'honorarios')` | Empieza un trabajo en esa área de su carrera, en el cargo que le da su experiencia y con el sueldo de ese cargo. Es un error si no puede (revisa antes `puede_trabajar_en`). Con `jugador.trabajar_en(jugador.area)` cambia de contrato sin perder el cargo (solo efectos) |
| `jugador.puede_trabajar_en('área')` | Si tiene un título de esa carrera (un oficio no lo pide) y cumple los requisitos del área |
| `jugador.area`, `jugador.cargo` | Área y cargo de su trabajo actual (`'carrera.software.cargo.senior'`), o `nada` si no trabaja en su carrera |
| `jugador.cargo_anterior`, `jugador.ultimo_cargo` | El cargo de más abajo en su escalera (`nada` en el inicial), y el cargo actual o el último que tuvo en su carrera |
| `jugador.meses_en_cargo` | Meses de experiencia en el cargo actual |
| `jugador.ultima_area` | El área actual o la última que tuvo (para volver a la misma: `jugador.trabajar_en(jugador.ultima_area)`) |
| `jugador.oficio` | El oficio de su trabajo actual (`'oficio.garzon'`), o `nada` |
| `jugador.meta`, `jugador.meses_bajo_meta`, `jugador.amonestaciones` | En un oficio: cuánto cumplió de la meta el último mes (en %), los meses seguidos bajo la meta y los llamados o amonestaciones que lleva. Los dos últimos se pueden cambiar (`jugador.amonestaciones = 1`, `jugador.meses_bajo_meta = 0`) |
| `jugador.pago_variable` | La comisión, el trato o las propinas del último mes (a precios de hoy) |
| `jugador.nuevo_contrato()` | Un contrato nuevo con el mismo empleador y el mismo equipo (la obra siguiente de la constructora): la antigüedad vuelve a cero (solo efectos) |
| `jugador.desafio('marca')`, `jugador.en_desafio` | Empieza el desafío de este mes en un oficio (solo efectos; ver "Oficios"), y si hay uno en curso |
| `jugador.pedir_aumento(porcentaje)` | Pide un aumento a la persona del evento (su jefe). Sale todo, la mitad o nada (encuesta de PayScale: 39 %, 31 % y 30 %), movido por el desempeño, el respeto del jefe, la economía y lo mucho que pide (más de un 10 % resta). Sube el sueldo base y lo anota en el diario (`diario.pega.aumento_completo`, `_parcial` o `_no`) (solo efectos) |
| `jugador.dias_vacaciones`, `jugador.tomar_vacaciones(días)` | Los días de vacaciones ganados y no tomados (enteros), y tomarlos (solo efectos; tiene que tener esos días) |
| `jugador.pago_vacaciones`, `jugador.cobrar_vacaciones()` | Lo que le pagarían por los días no tomados (de su trabajo o, si ya no trabaja, del último), y recibir ese pago: los días quedan en cero (solo efectos) |
| `jugador.avisar_renuncia(semanas)`, `jugador.renuncio`, `jugador.semanas_para_irse` | Renuncia con aviso: se va en esas semanas (Chile 4, Argentina 2); si ya avisó, y cuántas semanas le quedan. El último día lo hace un evento (`evento.pega.ultimo_dia`) |
| `jugador.abrir_negocio(aporte, forma)`, `jugador.cerrar_negocio()` | Abre un negocio con la persona del evento como socio: pone el aporte (en la moneda de hoy, por ejemplo `precio(sueldo_minimo())`), `'formal'` (una empresa) o `'informal'` (de palabra); y lo cierra de común acuerdo (solo efectos) |
| `jugador.tiene_negocio`, `jugador.negocio_formal`, `jugador.meses_negocio`, `jugador.ganancia_negocio`, `jugador.ganado_negocio`, `persona.es_tu_socio` | Si tiene negocio, si es formal, los meses abierto, su parte del último mes y todo lo que ha recibido (en la moneda de hoy), y si esa persona es su socio |
| `jugador.tiene_equipo` | Si trabaja con su equipo armado (jefe y colegas) |
| `jugador.recomendar()` | Recomienda en su equipo a la persona del evento. La contratan con la probabilidad de una entrevista por contacto del país (`exito_entrevista` + `exito_extra` de la forma con `necesita_contacto`; en Chile 55 %): entra a su equipo y lo anota en el diario (`diario.pega.recomendado_entra` o `_no`) (solo efectos) |
| `persona.en_tu_equipo`, `persona.recomendado` | Si esa persona trabaja en tu equipo, y si entró recomendada por ti |
| `jugador.compartir_arriendo()`, `jugador.dejar_de_compartir()`, `jugador.comparte_arriendo`, `persona.vive_contigo` | La persona del evento se viene a vivir con él y paga su parte del arriendo cada mes (el arriendo se divide entre él, su pareja y quien lo comparte); dejar de compartir; si comparte; y si esa persona es quien vive con él |
| `jugador.meses_en_trabajo` | Meses con el empleador actual (la antigüedad; sigue al ascender en la misma empresa) |
| `jugador.ultimo_sueldo`, `jugador.meses_ultimo_trabajo`, `jugador.tipo_ultimo_trabajo` | El sueldo bruto, la antigüedad y el tipo (`'dependiente'`...) del último trabajo que dejó: sirven para el finiquito o una demanda después de un despido |
| `jugador.herencia_pendiente`, `jugador.recibir_herencia()` | Lo heredado que espera un trámite (a precios de hoy), y el efecto que lo entrega |
| `jugador.desempeno` | Qué tan bien trabaja, 0 a 100. Se puede cambiar en cualquier trabajo (`jugador.desempeno += 5`); en un trabajo de su carrera cuenta para ascender |
| `jugador.puede_ascender`, `jugador.caminos_ascenso` | Si puede ascender ahora, y a cuántos cargos (2 cuando hay que elegir camino) |
| `jugador.puede_ascender_a('cargo')` | Si puede ascender ahora a ese cargo |
| `jugador.ascender()`, `jugador.ascender('cargo')` | Asciende y le suben el sueldo; con dos caminos hay que nombrar el cargo. Es un error si no puede (solo efectos) |
| `jugador.bajar_cargo()` | Vuelve al cargo anterior, con su sueldo (solo efectos) |
| `jugador.en_la_cima` | Si su cargo es el más alto de su escalera |
| `jugador.empresa_tamano`, `jugador.empresa_mayor` | Tamaño de su empresa (`'empresa.tamano.micro'`) y el tamaño siguiente, o `nada` |
| `jugador.tope_de_empresa` | Si está en el cargo más alto que existe en su empresa, aunque su escalera sigue |
| `jugador.empresa_quebro` | Si su empresa quebró y todavía no deja ese trabajo |
| `jugador.cambiar_de_empresa()` | Se va a una empresa más grande de su carrera (o a otra del mismo tamaño si ya es grande), en la misma área y cargo, con el sueldo de allá (solo efectos) |
| `jugador.postular_en('área')`, `postular_en('área', 'honorarios')`, `postular_en('cualquier_trabajo')` | Dice qué trabajo va a buscar: en un área de su carrera (tiene que cumplir sus requisitos) o cualquiera (solo efectos) |
| `jugador.buscar_trabajo('forma')` | Empieza a buscar de esa forma de su país (`'busqueda.forma.portales'`). Si no dijo qué busca, busca cualquier trabajo. Con una forma que necesita contacto, ese conocido queda en `jugador.contacto` (solo efectos) |
| `jugador.puede_buscar_con('forma')` | Si esa forma existe en su país y, si necesita contacto, si tiene a alguien que lo recomiende |
| `jugador.busqueda`, `jugador.busca_en` | La forma con que busca y lo que busca (un área o `'cualquier_trabajo'`), o `nada` |
| `jugador.semanas_buscando`, `.postulaciones`, `.entrevistas`, `.rechazos` | Cómo le va en la búsqueda (0 si no busca) |
| `jugador.entrevista_pendiente`, `jugador.dar_entrevista(puntos)` | Si lo llamaron a una entrevista, y va a ella: los puntos (10 = 10 % más) dicen cómo se preparó (solo efectos) |
| `jugador.tiene_oferta`, `jugador.oferta_area` | Si le ofrecieron un trabajo, y de qué área (`nada` si es fuera de su carrera) |
| `jugador.aceptar_oferta()`, `jugador.negociar_oferta()`, `jugador.rechazar_oferta()` | Acepta (entra a trabajar), pide más sueldo o la rechaza y sigue buscando (solo efectos) |
| `jugador.oferta_negociada`, `jugador.oferta_mejorada` | Si ya pidió más sueldo por esta oferta, y si se lo subieron |
| `jugador.contacto` | Quien lo recomienda (otra persona, vista desde el jugador), o `nada` |
| `jugador.dejar_de_buscar()` | Borra la búsqueda (solo efectos). También se borra sola al quitar la marca `buscando_trabajo` |
| `jugador.meses_cotizados` | Meses en que cotizó para su pensión (con contrato). Los meses sin trabajo o informales no suman |
| `jugador.sueldo`, `jugador.sueldo_liquido` | Sueldo bruto de hoy y líquido de la última liquidación (0 si no trabaja) |
| `sueldo_minimo()`, `sueldo_tipico()` | Sueldos de referencia del país del jugador, en dinero del inicio del mundo (para usar en `trabajar`) |
| `sueldo_titulo()` | Lo que paga el mercado según su mejor título: el típico por el `sueldo_relativo` del nivel (o por `relativo_sin_titulo`), nunca menos que el mínimo |
| `cambiar_inflacion('economia.inflacion.x')` | Pasa el país del jugador a ese régimen (por ejemplo, en una crisis). Todo país debe definir los regímenes que usan los eventos (solo efectos) |
| `semilla('id').activa`, `.despierta`, `.fuerza`, `.testigos` | La semilla abierta de ese tipo; `fuerza` se puede cambiar |
| `semilla('id').testigo` | El primer testigo vivo de la semilla (otra persona, vista desde el jugador: `semilla('id').testigo.rencor`), o `nada` |
| `semilla('id').cerrar` | Cierra la semilla (solo efectos) |
| `plantar('id')` | Planta una semilla nueva (solo efectos). Si el personaje ya tiene abierta una del mismo tipo, no planta otra: la existente gana la `fuerza` del tipo (máximo 100) |
| `plantar('id', persona)` | Igual, con esa persona como testigo: `plantar('semilla.presto_plata_amigo', jugador.amigo)`. Así el evento de la semilla, años después, sabe de quién se trata (`semilla('id').testigo`). La carta del evento lo muestra: bajo la opción dice "Lo va a notar Pedro" y, cuando la semilla despierta, "Te miran: Pedro". Si el evento tiene alguna opción con `plantar`, lleva el sello "Esto se recuerda" |
| `diario('clave')` | Escribe en el diario (solo efectos) |
| `jugador.tiene_celular`, `jugador.tiene_app('app.x')` | Si tiene celular, y si tiene esa app instalada |
| `jugador.dar_celular()`, `jugador.perder_celular()` | Recibe un celular nuevo con las apps de fábrica (reemplaza al que tenía), o lo pierde con sus apps (solo efectos) |
| `jugador.instalar_app('app.x')`, `jugador.desinstalar_app('app.x')` | Instala o borra una app; sin celular es un error (solo efectos) |
| `jugador.tiene_correo`, `jugador.correos`, `jugador.correos_sin_leer` | Si tiene cuenta de correo, cuántos correos tiene y cuántos no ha leído |
| `jugador.mensajes_sin_leer` | Cuántos mensajes de Mensajito no ha visto (ver "Mensajito" más abajo) |
| `jugador.crear_correo()`, `jugador.recibir_correo('correo.x')` | Crea su cuenta de correo, y le hace llegar un correo (sin cuenta, no llega) (solo efectos) |
| `noticia_reciente('noticia.x')`, `noticia_reciente('noticia.x', semanas)` | Si esa noticia salió en el país del jugador (de todo el país o de su ciudad) hace menos de esas semanas (8 si no se dice). Sirve en eventos, semillas y objetos de la escena |
| `abrir_causa('causa.x')`, `abrir_causa('causa.x', persona)` | Abre una causa del país del jugador en su primera etapa, con esa persona como la otra parte (la expareja). Si ya hay una abierta de ese tipo, no abre otra (solo efectos) |
| `causa('causa.x')` | La causa de ese tipo del jugador (la abierta, o la última que terminó). Datos: `existe`, `abierta`, `etapa` (id, o `''`), `semanas_en_etapa`, `lista` (se cumplió el plazo de la etapa), `resultado` (o `nada`), `monto` (se puede cambiar), `rit`, `contraparte` (la otra persona, o `nada`). Acciones: `avanzar()`, `ir_a('etapa')`, `cerrar('resultado')` |
| `causa_en('causa.x', 'etapa')` | Si la causa está abierta, en esa etapa, y ya se cumplió su plazo |
| `causas_abiertas()` | Cuántas causas abiertas tiene el jugador (para la carta del tribunal en la escena) |
| `jugador.union`, `jugador.conyuge` | La unión ante la ley (`'union.chile.matrimonio'`...) y con quién, o `nada` |
| `jugador.separado`, `jugador.viudo` | Unido ante la ley con alguien que ya no es su pareja; su cónyuge murió |
| `jugador.unirse('union.x')`, `jugador.terminar_union()` | Se casa o firma la unión civil con su pareja (la unión debe estar en `uniones_civiles` de su país y no puede estar unido con otra persona); termina la unión ante la ley, para los dos (solo efectos) |
| `ley('ley.x')` | Valor de una ley en el país del jugador (ver Leyes) |
| `cambiar_ley('ley.x', valor)` | Cambia una ley del país del jugador durante la partida (solo efectos) |

### Relaciones

Cada persona siente algo por cada otra: **cariño, confianza, respeto y rencor**, de 0 a 100 y por separado. No es simétrico: lo que tu madre siente por ti no es lo que tú sientes por ella. En el lenguaje se lee en la otra persona, y es lo que **ella siente por quien se llegó a ella**:

| Escribes | Qué es |
|---|---|
| `jugador.madre.carino`, `.confianza`, `.respeto`, `.rencor` | Lo que la madre siente por el jugador (sirve en `padre`, `pareja`, `amigo`, `colega`, `rival`, `ex` y `semilla('id').testigo`). Se pueden cambiar: `jugador.pareja.rencor += 15`. Siempre quedan entre 0 y 100. `jugador.carino` es un error: el jugador no siente por sí mismo |
| `jugador.amigo`, `jugador.colega`, `jugador.rival`, `jugador.ex` | La persona más cercana con ese lazo, o `nada`: el amigo que más cariño le tiene, el colega que más lo respeta, el rival con más rencor, el ex con más cariño |
| `jugador.amigos` | Cuántos amigos vivos tiene |
| `jugador.conocer('amistad')`, `('colega')`, `('rival')` | Llega alguien nuevo con ese lazo, del mismo lugar y de edad parecida (casi la misma en el colegio, hasta 8 años de diferencia de adulto) (solo efectos) |
| `jugador.colega.vincular('amistad')` | Cambia el lazo con esa persona, en ambos sentidos; lo que sienten se mantiene (solo efectos) |
| `jugador.amigo.alejarse()` | Deja de ser amigo (o colega, rival); lo que siente se mantiene (solo efectos) |
| `jugador.amigo.vinculo` | `'amistad'`, `'colega'`, `'rival'`, `'ex'` o `nada` |

Al empezar, sin que haya pasado nada: madres o padres e hijos sienten cariño 75, confianza 65, respeto 55; hermanos 60, 55, 45; la pareja 75, 60, 55; cualquier otra persona 30, 30, 30; nadie siente rencor. Un amigo nuevo empieza con cariño 55, un colega con respeto 50 y un rival con rencor 45. Al terminar una pareja (no al morir), quedan como ex con lo que sentían.

El tiempo también cambia las relaciones, una vez al mes: el rencor baja 1 punto (2 si la persona es amable, con amabilidad 60 o más), y una amistad pierde 1 punto de cariño cada 4 meses (hasta 25) si nada la alimenta. Juntarse, ayudar y pedir perdón son eventos de los paquetes.

Se irán sumando datos (colegio, empresas) a medida que lleguen esos sistemas.
