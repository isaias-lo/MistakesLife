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
| U4 | Guardar y continuar la partida en el teléfono (usa el guardado del núcleo) | Pendiente |
| U5a | Casa en 3D estilo maqueta sobre la pantalla de la vida: muebles según el nivel del hogar, cuna/cama/escritorio según la etapa, una figura por persona (`HomeView` en el núcleo), con los padres en casa hasta los 28 sin pareja | Hecha |
| U5b | Barrio de Puente Alto en 3D: botones Casa/Barrio, cuadra con casas con reja (techo y pisos según el nivel del hogar), colegio, almacén, plaza y paradero, y un camino hasta adonde va el personaje esa semana (`DailyPlace` en el núcleo) | Hecha |
| U5c | Lugar de trabajo en 3D según la carrera: botón Trabajo; cada carrera elige en su paquete `lugar_de_trabajo` (oficina abierta, cubículos o consulta); sin título, un local de atención; un título que paga más se ve más cuidado (`WorkplaceView` en el núcleo) | En revisión |
| U6 | Primer APK para probar en Android: armado en la nube con GameCI (`.github/workflows/apk.yml`, se corre a mano) y menú "Armar APK de prueba" | En revisión (faltan los secretos de Unity en GitHub) |

## Lo que falta en el núcleo

Diagnóstico completo (2026-11-03): [Qué le falta al núcleo de MistakesLife](https://claude.ai/artifact/8oPYjZ8g7hjUPXtnSoaSjb).

| # | Tarea | Estado |
|---|---|---|
| N1 | Guardar y cargar la partida: a texto o bytes comprimidos, con versión y migraciones; la partida cargada sigue igual; `g` y `--cargar` en la consola | Hecha |
| N2 | Fotos del mundo para "volver al pasado" | Pendiente |
| N3 | Mercado laboral realista: trabajo informal sin cotizaciones, fin de contratos, dejar de buscar y volver, cuidar a los hijos en casa, meses cotizados (`jugador.meses_cotizados`) y desempleo en el simulador | En revisión |
| N4 | Relaciones con cariño, confianza, respeto y rencor; amigos y colegas | Pendiente |
