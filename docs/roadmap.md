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
| D1 | Tendencia en las opciones y `AutoDecider`: el simulador decide según personalidad, hogar e historia | En revisión |

## Economía (todo con datos de cada paquete de país)

| # | Tarea | Estado |
|---|---|---|
| E1 | Dinero e inflación: saldo de cada personaje, inflación generada por regímenes del país, índice de precios y `precio()` | En revisión |
| E2 | Sueldos: sueldo mínimo y típico del país, sueldo por carrera y título, liquidación con AFP, salud e impuestos del país | Pendiente |
| E3 | Gastos del hogar (arriendo, comida, transporte, hijos) según la comuna, y estrés financiero | Pendiente |
| E4 | Deudas: CAE, tarjetas y créditos con interés, registro de morosos y herencia de deudas según la ley | Pendiente |
| E5 | Jubilación real: fondo de pensión (capitalización o reparto según el país) y PGU con su monto | Pendiente |
| E6 | Crisis económicas como eventos que cambian la inflación y el empleo para todos | Pendiente |
