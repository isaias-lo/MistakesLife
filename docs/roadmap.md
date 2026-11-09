# Hoja de ruta

## Fase 1: núcleo en texto (vida de 0 a 25 años en Santiago)

| # | Tarea | Estado |
|---|---|---|
| 1 | Repositorio, estructura de carpetas, reglas para agentes e integración continua | Hecha |
| 2 | Esqueleto del núcleo: ciclo semanal, modelo de personaje y azar reproducible, con pruebas | Hecha |
| 3 | Sistema de semillas v1, con pruebas | Hecha |
| 4 | Motor de eventos que lee paquetes JSON, con su lenguaje de condiciones | Hecha |
| 5a | Núcleo para paquetes de país: país, ciudad, comuna, familia de origen y etapas educativas | Hecha |
| 5b | Paquetes mínimos: Chile, Santiago con Puente Alto, carrera de software, 0 a 25 años | Hecha |
| 6 | Interfaz simple de texto para jugar una vida completa | Hecha |
| 7 | Simulador de vidas para revisar que los resultados se parezcan a la realidad | Hecha |

**Prueba de la fase 1:** ¿vivir por semanas es entretenido y las decisiones pesan?

## Opción C: realismo de Puente Alto y nuevas carreras

| # | Tarea | Estado |
|---|---|---|
| C1 | Núcleo: carreras como paquetes (`career.json`, niveles, semestres, títulos) y software migrado | Hecha |
| C2 | Paquete de contabilidad (contador general, contador auditor) | Hecha |
| C3 | Paquete de psicología | Hecha |
| C4 | Más eventos de vida en Puente Alto | Hecha |
| C5 | Equilibrar con el simulador y datos reales (`probabilidad_anual`) | Hecha |

## Opción B: la vida hasta la vejez y el legado

| # | Tarea | Estado |
|---|---|---|
| B1 | Envejecer y morir: tabla de mortalidad real por país, salud que se recupera y envejece, muerte del jugador y de sus padres | Hecha |
| B2 | Pareja e hijos: personajes nuevos que nacen en el mundo, con eventos de familia en Chile | Hecha |
| B3 | Legado: al morir, seguir jugando con un hijo (semillas heredables, mismo mundo, vida previa) y consola hasta la muerte | Hecha |
| B4a | Los hijos y nietos que nadie juega forman su propia familia (pareja, hijos, separación), y el jugador tiene nietos | Hecha |
| B4b | Eventos de adultez y vejez en Chile: despido y nuevo trabajo, crisis de los 40, jubilación por AFP con la edad legal, PGU, enfermedades con consecuencias (hipertensión, diabetes, cáncer) y soledad en la vejez | Hecha |
| B5a | Diversidad, núcleo: orientación e identidad al nacer según encuestas del país, y pareja según la orientación | Hecha |
| B5b | Diversidad, eventos de Chile: descubrirse, salir del clóset, reacción de la familia, Marcha del Orgullo, discriminación y cambio registral con la ley | Hecha |
| B5c | Diversidad, familias del mismo sexo con adopción (también los hijos no jugados adoptan) | Hecha |
| B6 | Leyes de cada país (`leyes` en country.json, `ley()` y `cambiar_ley()`), con las primeras leyes de Chile | Hecha |

## Decisiones según la personalidad

| # | Tarea | Estado |
|---|---|---|
| D1 | Tendencia en las opciones y `AutoDecider`: el simulador decide según personalidad, hogar e historia | Hecha |

## Economía (todo con datos de cada paquete de país)

| # | Tarea | Estado |
|---|---|---|
| E1 | Dinero e inflación: saldo de cada personaje, inflación generada por regímenes del país, índice de precios y `precio()` | Hecha |
| E2 | Sueldos: sueldo mínimo y típico del país, sueldo por carrera y título, liquidación mensual con AFP, salud e impuesto del país, reajuste con la inflación | Hecha |
| E3 | Declaración anual de impuestos (en Chile, la Operación Renta): tramos anuales, honorarios, devolución o pago y multas | Hecha |
| E4 | Gastos del hogar (arriendo, comida, transporte, hijos) según la comuna, y estrés financiero | Hecha |
| E5 | Jubilación real: cuenta individual de pensión (AFP) con rentabilidad, pensión al jubilarse y pensión del Estado (PGU) con su monto. El sistema de reparto (Argentina) se suma con ese país | Hecha |
| E6a | Deudas: tipos de deuda del país con interés, cuotas y reajuste (CAE y crédito de consumo en Chile), registro de morosos (DICOM) | Hecha |
| E6b | Herencia: bienes y deudas que pasan a los hijos según la ley del país (beneficio de inventario, deudas que se extinguen al morir) | Hecha |
| E6c | Tarjetas de crédito (pago mínimo) y repactar deudas | Hecha |
| E7 | Ciclo económico del país (normal, recesión, crisis): cambia la inflación, golpea los fondos de pensión y trae despidos; `cambiar_ciclo()` para eventos | Hecha |

## Prototipo en Unity (Unity solo dibuja; todo lo demás es del núcleo)

| # | Tarea | Estado |
|---|---|---|
| U1 | Núcleo listo para Unity: paquetes en un solo archivo (`PackBundle`, para Android) y vista de la partida (`LifePresenter`) con textos `ui.*` | Hecha |
| U2 | Proyecto de Unity en `unity/` (Unity 6 LTS), comando `tools/MistakesLife.UnityExport` que copia el núcleo y los paquetes adentro, y pantalla de prueba | Hecha |
| U3 | La vida en pantalla (2D): personaje con barras, evento con botones, familia, diario, legado y nueva vida (uGUI armado desde código, textos `ui.*`) | Hecha |
| U4 | Guardar y continuar la partida en el teléfono: se guarda sola después de cada paso y al salir; al abrir el juego sigue la misma vida (`SaveStore`, usa `LifeSession.SaveToBytes`/`LoadFromBytes` del núcleo); un guardado dañado se aparta y empieza una vida nueva | Hecha |
| U5a | Casa en 3D estilo maqueta sobre la pantalla de la vida: muebles según el nivel del hogar, cuna/cama/escritorio según la etapa, una figura por persona (`HomeView` en el núcleo), con los padres en casa hasta los 28 sin pareja | Hecha |
| U5b | Barrio de Puente Alto en 3D: botones Casa/Barrio, cuadra con casas con reja (techo y pisos según el nivel del hogar), colegio, almacén, plaza y paradero, y un camino hasta adonde va el personaje esa semana (`DailyPlace` en el núcleo) | Hecha |
| U5c | Lugar de trabajo en 3D según la carrera: botón Trabajo; cada carrera elige en su paquete `lugar_de_trabajo` (oficina abierta, cubículos o consulta); sin título, un local de atención; un título que paga más se ve más cuidado (`WorkplaceView` en el núcleo) | Hecha |
| U6 | Primer APK para probar en Android: armado en la nube con GameCI (`.github/workflows/apk.yml`, se corre a mano) y menú "Armar APK de prueba" | Hecha (secretos listos; falta correrlo por primera vez) |
| U7 | Capturas automáticas de las escenas 3D (pieza, barrio, trabajo) de vidas fijas a varias edades, para revisar el diseño sin abrir Unity: menú "Sacar capturas" y `.github/workflows/capturas.yml` en cada PR que toca `unity/` (necesita los secretos de Unity) | Hecha (secretos funcionando desde el 2026-11-05) |
| U5d | La vista Trabajo muestra el cargo, el área y el desempeño (del núcleo, N5); subir de cargo se nota: segunda pantalla desde el segundo ascenso y rincón propio desde el tercero | Hecha |
| U8a | Primera ronda de correcciones visuales con las capturas: el rincón del cargo alto ya no atraviesa los cubículos (cada lugar arma el suyo), marca amarilla más grande en Trabajo, familia en zigzag en la pieza, y vidas de prueba con carreras distintas más una vitrina de cada lugar en rango 3 | Hecha |
| U8b | Barrio vivo: el jugador (con su tamaño según la edad) y su familia en la calle, el jugador donde va esa semana, vecinos y autos que cambian de una vida a otra, alfiler amarillo sobre tu casa, y sin el árbol cortado en el borde | Hecha |
| U8c | Segunda ronda con capturas: bolita amarilla sobre la cabeza del jugador (se ve aunque un mueble tape el piso), vidrio con marco, puf y colega que ya no se cortan ni atraviesan, peatones lejos de árboles y paradero, familia separada en el antejardín | Hecha |
| U9 | Botón "Volver al pasado" en Unity (usa N2): lista de momentos permitidos con edad y etapa o decisión, cargas que quedan, el diario se rehace y la opción de la otra vez se marca como intuición; en el núcleo `LifePresenter.TimeTravel`/`GoBack` y `EventView.IntuitionOption` | Hecha |
| U8d | Tercera ronda de capturas: bolita del jugador justo sobre el pelo según la estatura, sin círculo de piso en el trabajo, productos del local sin amarillo y sillón de la pieza a un costado | Hecha |
| U10 | Tu gente en pantalla (usa N4): amistades, colegas, rivales y ex vivos (hasta 8, de la más cercana) y, para ellos y la familia, cómo está la relación en una palabra ("muy unidos", "te guarda rencor"), sin números; en el núcleo `LifeSnapshot.People`, `PersonView.Feeling` y `LifePresenter.FeelingKey` | Hecha |
| U11 | La empresa en la vista Trabajo (usa N6): nombre y tamaño antes del cargo; en una micro o pequeña hay menos colegas y en una micro una sola mesa; en una mediana o grande un letrero en el muro (con logo de color en la grande); en el núcleo `WorkplaceView.CompanySizeId` | Hecha |
| U12 | Primera impresión: menú de inicio (continuar la vida en curso, nueva vida con aviso si se pierde la actual, cómo se juega); elegir entre 3 nacimientos posibles (nombre, lugar, hogar y padres, con `LifePresenter.Birth`); el evento como tarjeta ("Momento clave" en color cálido si es `dia_clave`, con `EventView.IsKeyMoment`); "Lo que pasó" después de cada toque con el tiempo que corrió y los cambios en verde o rojo (`LifePresenter.Summarize`, `StepSummary`); tutorial de 5 tarjetas la primera vez; botones y tarjetas con esquinas redondeadas | Hecha |
| U13 | La escena 3D cambia con la vida: objetos definidos en los paquetes (`scene/`, con condición) que aparecen en la casa según lo que vive y decide el jugador (libros, diploma, cuentas impagas, remedios, juguetes, flores, desorden, foto del recuerdo, mecedora...), y el cielo se nubla cuando está triste; en el núcleo `SceneObjectDefinition` y `HomeView.SceneObjects`; vitrina con todos los objetos en las capturas | Hecha |
| U14 | Ritmo del tiempo: dos botones, "Una semana" y "Seguir viviendo" (corre hasta que pase algo, ahora como mucho 1 año en vez de 5: `LifePresenter.MaxYearsPerStep`); "Lo que pasó" destaca lo que se anotó en el diario en ese paso (cumpleaños, logros, pérdidas) | En revisión |
| U15 | Diseño de la app acordado con Isaias: la vida como maqueta viva, 3D a pantalla completa, cada tema como objeto de la vida, letras, colores y personaje (`docs/diseno-app.md`) | Hecha |
| U16 | Base visual: TextMeshPro con las letras acordadas, colores y tamaños mínimos de `docs/diseno-app.md` | Pendiente |
| U17 | 3D modificable: un modelo por id en `Resources/Modelos/` con la forma simple como reserva, pieza/oficina/barrio con anclas editables y materiales compartidos | Pendiente |
| U18 | Pantalla de la vida nueva y personaje estilo juguete de madera que envejece | Pendiente |
| U19 | Tu dinero (el mes en días, la mochila de deudas, tu historia) con sus datos en el núcleo | Pendiente |
| U20 | Luz por etapa, hora y clima, y transición de cada objeto 3D a su pantalla | Pendiente |

## Lo que falta en el núcleo

Diagnóstico completo (2026-11-03): [Qué le falta al núcleo de MistakesLife](https://claude.ai/artifact/8oPYjZ8g7hjUPXtnSoaSjb).

| # | Tarea | Estado |
|---|---|---|
| N1 | Guardar y cargar la partida: a texto o bytes comprimidos, con versión y migraciones; la partida cargada sigue igual; `g` y `--cargar` en la consola | Hecha |
| N2 | Volver al pasado: fotos del mundo al empezar cada etapa de la vida y antes de cada día clave, `LifeSession.GoBack`, cargas (`TimeMachine.Charges`, una gratis por partida), cada etapa se vuelve a vivir una vez por generación, intuición en el diario y la opción elegida la otra vez; `v` en la consola; guardado versión 5 | Hecha |
| N3 | Mercado laboral realista: trabajo informal sin cotizaciones, fin de contratos, dejar de buscar y volver, cuidar a los hijos en casa, meses cotizados (`jugador.meses_cotizados`) y desempleo en el simulador | Hecha |
| N4 | Relaciones con cariño, confianza, respeto y rencor (lo que cada persona siente por otra, 0 a 100); amigos, colegas, rivales y ex (`jugador.conocer('amistad')`, `jugador.amigo.carino`); el rencor se olvida y la amistad se enfría con el tiempo; testigos en las semillas (`plantar('id', persona)`); 25 eventos de Chile y guardado versión 3 | Hecha |
| N5 | Áreas y ascensos en las carreras: cada carrera con al menos 3 áreas (software 5, contabilidad 4, psicología 4) con requisitos y sueldo propios, escalera de cargos hasta la cima (con dos caminos en software senior), experiencia y desempeño (`jugador.desempeno`), ascensos, bajar de cargo y despidos; volver a trabajar con la experiencia y cambiar de área cuesta un cargo; un dilema con semilla y un evento típico por área; guardado versión 4. Pendiente para después: minijuegos por área, empresas con tamaño (que el cargo exista), reconocimiento fuera de la empresa | Hecha |
| N6 | Empresas con tamaño y vida: cada trabajo de carrera es en una empresa (micro, pequeña, mediana o grande, según cuánta gente trabaja en cada tamaño en el país) que define el sueldo y el cargo más alto que existe; cada año puede crecer, achicarse o quebrar (más en recesión y crisis); compañeros en la misma empresa; `cambiar_de_empresa()`; 4 eventos de Chile; guardado versión 6 | Hecha |
| N7 | Calibrar con datos reales (primera ronda): la carrera se alarga por ramos botados o reprobados, primer año con dudas (seguir, cambiarse o dejarla), volver a estudiar de adulto (vespertino), jubilarse y seguir trabajando, y dejar de trabajar en la vejez. El simulador mide trabajo, estudio y jubilación solo entre los vivos y muestra cuántos estudian. Comparado con la ENE 2024 y la CASEN 2024 (detalle en `packs/countries/chile/SOURCES.md`). Pendiente: inactividad entre los 30 y los 60 (en el juego trabaja ~83 %, en Chile ~75 %), trabajo informal en la vejez y pensiones | Hecha |
| N8 | Calibrar con datos reales (segunda ronda): cuidar a la mamá o al papá enfermo, dejar de trabajar por salud y volver al recuperarse, quedarse en casa con los hijos más seguido, "pololitos" sin contrato después de jubilar, y los jubilados dejan de buscar trabajo. Ahora trabaja ~80 % a los 35, ~73 % a los 45 y ~70 % a los 55, como en la ENE 2024. Pendiente: pensiones y mortalidad | Hecha |
| N9 | Eventos que no se repiten igual (pedido de Isaias tras jugar el APK): `variantes` (otro texto cada vez que el evento vuelve) y `veces_maximas` (tope en la vida); 33 textos nuevos y más espera en los 15 eventos que más se repetían (de ~330 a ~260 eventos por vida). Pendiente: más contenido nuevo, felicidad que vuelva sola a su nivel (hoy solo la suben los eventos) y las deudas que crecen sin control | En revisión |
| N10 | El rojo ya no es infinito: lo que falta para los gastos lo cubre la línea de crédito del banco (Chile) o el descubierto (Argentina), con interés y hasta un cupo; con plata, la línea se paga sola con el saldo; lo que pasa del cupo son cuentas atrasadas. Con 4 meses en rojo viviendo solo llega el desalojo: volver con la familia, arrendar una pieza (40 % del arriendo, sin cuentas aparte) o irse de allegado; con las cuentas al día se puede volver a un departamento. Antes, a los 26 años había vidas con -59 millones sin explicación; en 100 vidas a los 30, ninguna queda en negativo. Pendiente: pantalla "Tu dinero" (diseño en revisión) y calibrar el ahorro, que sale alto (mediana ~50 millones a los 30) | En revisión |

## Argentina como segundo país

| # | Tarea | Estado |
|---|---|---|
| A1 | Núcleo para varios países: los eventos del paquete de un país solo le pasan a quien vive ahí; los eventos comunes (amigos, pareja, diversidad, salud, trabajo, ascensos, empresas, estudios, jubilación) pasan al paquete base; textos con variante por país (`variantes_idioma` y `lang/variante/es-CL.json`), con palabras generales en `es` y las chilenas en `es-CL`. Para quien vive en Chile no cambia nada (el simulador da lo mismo) | Hecha |
| A2 | País Argentina: Buenos Aires (La Matanza), colegios (jardín, primaria, secundaria), leyes, mortalidad, nombres, inflación alta (con hiperinflación posible), sueldos, recibo de sueldo (jubilación, PAMI, obra social, Ganancias), gastos, deudas y jubilación de reparto (ANSES) con PUAM, nueva en el núcleo. Textos generales de la economía pasan al paquete base. Nadie nace en Argentina por azar hasta A3 (`peso_nacimiento: 0`); se juega eligiéndola (`--pais pais.argentina`). Valores de memoria, por verificar en A4 | En revisión |
| A3 | Eventos y carreras de Argentina: ingreso a la universidad (CBC de la UBA), universidad pública gratis, terciarios, monotributo, trabajo en negro, textos `es-AR` | Pendiente |
| A4 | Calibrar Argentina con la EPH del INDEC | Pendiente |
