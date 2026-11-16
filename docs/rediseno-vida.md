# Rediseño: volver al realismo (acordado con Isaias el 2026-11-14)

Este documento manda sobre cómo se **juega** MistakesLife. Nace del análisis profundo del 2026-11-14 (https://claude.ai/artifact/BVsdeKH4Qca6egnybXRR4j) y de los bocetos versión 3 que Isaias aprobó ("me encantó, sigamos"):
https://claude.ai/artifact/G5wzsBKMZgw7BebafBjGk8 (fila de arriba; la fila de abajo es la versión anterior).

**Mientras este rediseño no termine, no se agregan funciones nuevas** (salud, apps nuevas del celular, migración). Pedido de Isaias: "no sigamos avanzando hasta que cambiemos todo".

## 1. Qué estaba mal

Construimos sistema por sistema y lo validamos con el simulador, no con una persona jugando. El resultado se sentía como BitLife con más números:

1. **El jugador no tenía iniciativa.** Solo elegía opciones de eventos que le llegaban. El paso 1 del ciclo semanal ("decisiones") nunca existió.
2. **Eventos genéricos:** sin nombres, sin género, repetidos (7 saludos de cumpleaños iguales al año), en el mes equivocado.
3. **Colegio, universidad y trabajo eran solo números.** No había sala, compañeros ni jefe.
4. **Dinero irreal** (inflación desde el nacimiento, ahorro que explota, deudas sin tope).
5. **Las consecuencias no se sentían:** pasaban, pero el jugador no sabía de dónde venían.

## 2. Por qué triunfan los juegos de vida

Ninguno triunfó por ser realista. Triunfaron porque **el jugador quiere algo** y **cada rato de juego le deja una historia que contar** (BitLife, Los Sims, Stardew Valley, Crusader Kings, Persona, Chinese Parents). El realismo es lo que hace creíble esa historia. inZOI muestra el riesgo contrario: muy bonito y realista, pero lo que haces no pesa, y los jugadores se fueron.

**MistakesLife es un juego sobre los errores y lo que hacen con tu vida.** Eso es lo que nadie más tiene, y tiene que estar a la vista.

## 3. Cómo se juega ahora

### Rutina + momentos

- **Tu horario.** Una vez por etapa (semestre, trabajo nuevo, mudanza) armas tu semana: la mañana la pone el colegio o el trabajo; las tardes y noches las llenas tú (estudiar, plaza con tal amigo, once con tu mamá, deporte, celular). Lo cambias cuando quieras.
- **Tu vida avanza sola** según ese horario. No pide clics.
- **El tiempo se detiene solo cuando algo importa:** una prueba, una entrevista, alguien que te busca, una consecuencia que vuelve, una fecha marcada o algo que la vida te tira. Regla: **un momento detiene el tiempo solo si tiene persona o lugar con nombre y algo en juego.** Si no, queda en el diario.
- **Zoom:** "día a día", "semana" o "hasta lo importante" (lo normal).
- **Tus reglas:** "si hay prueba, estudio el día antes", "si me invitan, pregúntame". Lo ya decidido no se vuelve a preguntar.
- **Iniciativa:** desde el barrio vas a cualquier lugar cuando quieras, sin esperar que te llamen.
- Los ajustes de plata **no** detienen el tiempo: se hacen en la billetera o llegan por correo.

**Meta de ritmo** (a medir con gente jugando):

| Etapa | Minutos por año | Momentos por año |
|---|---|---|
| 0 a 5 años | 1 a 2 | 3 a 5 |
| 6 a 17, colegio | 4 a 6 | 10 a 15 |
| 18 a 30 | 6 a 10 | 15 a 25 |
| 31 a 65 | 3 a 5 | 8 a 12 |
| 66 en adelante | 2 a 3 | 5 a 8 |

Una vida hasta los 85 son unas 5 a 7 horas, en ratos de 5 a 15 minutos.

### Lo que quiero

En cada etapa el personaje desea 2 o 3 cosas que **nacen de su vida** (su gente, sus notas, su casa, su carácter): "que el Tomás me hable de nuevo", "sacarme un azul en mate", "que mi mamá deje el turno de noche". El jugador marca con destacador las que persigue (o ninguna). No son misiones: pueden cumplirse, cambiar o frustrarse. Le dan dirección a la rutina.

### El error al centro

- Cada momento importante tiene una opción tentadora que cuesta después.
- Cuando una semilla despierta, el juego muestra **"Esto viene de acá"**: la foto de ese día (fecha, lugar, personas) y la frase del diario.
- **Volver al pasado** se ofrece ahí mismo, frente al error, no en un menú.
- **Cada año se cierra como un capítulo** del diario con título propio ("El año en que el Tomás dejó de hablarme"), lo que pasó por mes y una foto.

### Tu gente

- Las personas no tienen barras ni números: se entienden por lo que hacen y por lo que han vivido contigo ("lo que han vivido juntos").
- Con los años, una persona puede proponer o recibir **futuros juntos**: ser socios, recomendarla en tu trabajo, arrendar juntos, volver a salir, decirle lo que sientes. Salen de su historia y de lo que cada uno tiene, y cada camino trae sus consecuencias (contrato, cuentas compartidas, juzgado si sale mal).

### Regla de repeticiones

Un contenido sale **una vez por vida**, salvo que haya una razón real para repetirlo (otra persona, otra etapa, algo que cambió). Si se repite, cambia la fecha, la persona o la letra.

## 4. Pantallas (bocetos versión 3)

| # | Pantalla | Objeto de la vida |
|---|---|---|
| 1 | Tu barrio (principal) | Maqueta de madera del barrio con la luz de la hora; credencial escolar, taco calendario, regla de madera con los meses (tu año), nota "lo que quiero", lo que viene, un botón "Seguir hasta el jueves" |
| 2 | Lo que quiero | Página del diario con destacador |
| 3 | Tu horario | Horario del colegio pegado en la pared, con stickers que arrastras |
| 4 | Un momento | El objeto del momento mismo (la hoja de la prueba, con la de la Javiera asomada) y frases que dirías |
| 5 | Un lugar | Tu sala con tus compañeros con nombre |
| 6 | Una persona | Retrato, lo que han vivido juntos, qué le dices |
| 7 | Lo que pueden armar juntos | La misma persona años después, con futuros posibles |
| 8 | El error que vuelve | La escena de hoy con la foto pegada de aquel día y "Volver a ese jueves" |
| 9 | El año se cierra | Página del diario con título, meses y una foto |

Reglas de pantalla, para que se sienta hecho a mano y no "de IA": ver `docs/diseno-app.md`, sección 0.

## 5. Plan por etapas

Cada etapa termina jugable en la consola y, cuando corresponde, en Unity. Como el juego no está publicado, las etapas pueden cambiar el guardado sin miedo (igual se sube la versión y se agrega la migración).

| # | Etapa | Criterio de éxito |
|---|---|---|
| R1 | Bocetos y acuerdo | Isaias aprueba (hecho el 2026-11-14, versión 3) |
| R2 | Núcleo de la vida: calendario real (día, mes y año del juego), horario por etapa con actividades de paquete (qué cuestan y qué mueven), avance "hasta lo importante" con momentos que detienen el tiempo, reglas propias y deseos v1 | En la consola se juega un año de colegio armando el horario una vez y con 10 a 15 detenciones; el simulador arma horarios |
| R3 | El colegio completo: curso con nombres y género, profes, ramos con notas 1,0 a 7,0, pruebas como momentos, recreo, apoderados | Una persona juega 8º básico y recuerda a 3 compañeros por su nombre |
| R4 | Errores que vuelven a la vista: cada semilla guarda su escena de origen; "Esto viene de acá"; volver al pasado frente al error; cierre del año como capítulo | En una vida de prueba, al menos 3 consecuencias muestran de dónde vienen |
| R5 | Universidad, CFT e instituto: inscripción de ramos, secciones, compañeros, práctica, gratuidad y CAE con su costo | Se juega un semestre universitario con compañeros con nombre |
| R6 | El trabajo: jefe, colegas, tareas con plazo, aumento, vacaciones, renuncia; futuros juntos con una persona | Se juega un año de trabajo con decisiones propias |
| R7 | Pasar el contenido de hoy al nuevo modelo y aplicar la regla de repeticiones: cada evento queda como consecuencia con nombre y género, pasa a ser actividad, o se borra | Ningún contenido se repite sin razón en 1000 vidas del simulador |
| R8 | Dinero creíble: inflación desde la partida, gastos con tope y que dependen de decisiones, ahorro con destino (casa, auto, inversión), deudas con tope legal | Montos de una vida de prueba parecidos a los reales de Chile |

En Unity, cada etapa trae su pantalla de la tabla de la sección 4 (la maqueta del barrio va con R2).
