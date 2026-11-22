# Diseño del juego: acuerdos del rediseño (16-11-2026)

Este documento junta todo lo que Isaias y Claude acordaron el 16 de noviembre de 2026 en una conversación larga sobre cómo se juega y cómo se ve MistakesLife. **Isaias lo aprobó** y desde ahora **manda sobre cómo se juega y cómo se ve**. Reemplaza a `docs/rediseno-vida.md` y `docs/diseno-app.md`, que quedan en `docs/archivo/` solo como historia.

- Versión para comentar: https://claude.ai/artifact/8rdETRHaNTCSfMfcjJm1sA
- Bocetos aprobados (lienzo): https://claude.ai/artifact/G5wzsBKMZgw7BebafBjGk8

**La ley del proyecto sigue igual:** todo tiene que sentirse real y nada puede ser inventado. Cada gasto, cada resultado y cada persona nace de una decisión del jugador o de un dato real del país.

**Forma de trabajar:** ningún módulo se programa sin bocetos aprobados por Isaias. Los bocetos se presentan con opciones para que él elija.

## Resumen

- **El tiempo avanza por mes**, con un botón para avanzar un año, y se detiene en lo importante. La fecha, la edad y el curso siempre se ven.
- **Con el tiempo parado el jugador actúa** en sus módulos: estudiar, hablar con alguien, pedir un aumento, ir al médico. Cada acción gasta tardes libres del mes y da una mejora o un empeoramiento al tiro.
- **Nueve grupos** ordenan todo: Tu gente, Estudios y trabajo, Dinero, Dónde vives, Compras, Salud, Celular, Trámites y leyes, Tu historia.
- **Pantalla principal aprobada:** 3D arriba, lo que pasa abajo, cinco barras grandes con ícono propio y porcentaje, botones Un mes y Un año, y el círculo chico de Volver al pasado.
- **Estilo adulto y legible**, nunca infantil. Se toman las cualidades de BitLife (claro, rápido, con sonido en cada acción) pero con la profundidad que a BitLife le falta, y sin copiar su aspecto.
- **Arquitectura C:** una base de datos con una página web para llenar contenido sin programar, que genera paquetes descargables. El juego sigue funcionando sin internet (ver `docs/architecture.md`).

## Cómo avanza el tiempo

**Opción C elegida.** Avanzar por semana era lento y confuso. Avanzar por año, como BitLife, pierde profundidad. La mezcla queda así:

- **"Seguir un mes"** es el botón principal. Avanza hasta el próximo momento importante, pero nunca más de un mes.
- **"Un año"** está al lado, para quien quiere ir rápido. También se detiene en los momentos importantes.
- **Siempre a la vista:** fecha, edad y curso o cargo, para que el jugador nunca se pierda en el tiempo.
- **Con el tiempo parado** el jugador entra a sus módulos y actúa. Las tardes libres de su horario son el presupuesto real de esas acciones: si su semana está llena, puede hacer menos cosas.

El núcleo sigue avanzando de a una semana por dentro; "Un mes" y "Un año" son varias semanas seguidas que se detienen en un momento.

### Mensajes con el tiempo

- Con el tiempo parado, los mensajes son en vivo: la persona contesta en ese minuto o te deja en visto.
- Al avanzar, los mensajes quedan en espera y llegan cuando paras, con su fecha real.
- Los que tienen fecha límite (una entrevista, una cita, una deuda) detienen el tiempo.
- **Avanzar no castiga.** La persona solo se enoja si viste el mensaje y no contestaste.

### Volver al pasado

Es un **botón circular chico** al lado de los botones de avanzar, y **solo aparece si el jugador lo compró**. También sigue apareciendo frente al error que vuelve, como ya existe. Isaias vio 8 formas de ordenar los tres botones y eligió la 1: el círculo chico de Volver al pasado, "Seguir un mes" ancho y "Un año" al lado (boceto M1Ancho, y S1Filas con las barras). Mientras no exista la tienda, el círculo aparece si quedan cargas y un momento al que volver.

**Cambio del 21-11-2026 (ver `docs/publicacion.md`, decisión 8):** el círculo es un **ícono de reloj de arena** sin texto. Al tocarlo se abre un cuadro "Volver al pasado" con los momentos a los que se puede volver y las cargas que quedan. Es una compra, con una sola prueba gratis en todo el juego (con un anuncio, en la primera muerte), junto con **Segundo aliento**, que te salva de una muerte con secuelas reales.

## Pantalla principal, barras, íconos y letras

### Pantalla principal (aprobada, boceto S1Filas)

De arriba hacia abajo:

1. **La escena 3D** del lugar donde estás, ocupando menos espacio que hoy.
2. **Lo que pasa:** el momento o la noticia del mes, con letra grande y legible.
3. **Cinco barras grandes** con ícono propio y porcentaje.
4. **Botones:** el círculo chico de Volver al pasado (solo si lo compró), "Seguir un mes" ancho y "Un año" al lado.
5. **Barra de grupos** abajo: Tu gente, Estudios, Dinero, Celular y Más (donde están los otros grupos).

Al entrar al colegio, la universidad o el trabajo, la escena muestra el lugar con su gente.

### La escena sigue lo que te pasa (regla de Isaias, 22-11-2026)

La escena 3D **sigue el momento, la decisión o lo que le está pasando al jugador**: una cita, algo en la pega, un tema en la casa. No sigue el calendario. "Eso es el realismo que buscamos: que el usuario vea y sienta lo que pasa y decide." Como se juega por mes o por año, si la escena solo cambiara con la etapa no se sentiría el cambio. Por eso:

1. **Cuando un momento detiene el tiempo,** la escena va al lugar donde ocurre, con esa persona destacada. La tarea del IVA con tu jefa muestra la oficina; la prueba, el aula con tu profe; la once con tu mamá, la casa.
2. **Al terminar un mes o un año,** la escena queda en el lugar de lo más importante que pasó, junto al resumen: la oficina nueva si te ascendieron, la ceremonia si te titulaste.
3. **Sin nada especial,** se ve el lugar principal de tu etapa (la sala, el aula, la pega). Con un botón chico vuelves a la casa.
4. **Los lugares cambian con tu vida** aunque no los mires seguido: los compañeros nuevos de cada año, el colega que renunció ya no está, el que entró aparece, tu escritorio mejora al ascender.

Todo momento nuevo dice dónde pasa y con quién, para que la escena lo pueda mostrar. Es la forma de la regla de siempre: toda decisión que se pueda ver, se ve en 3D.

### Barras de la vida

| Barra | Ícono propio | Nota |
|---|---|---|
| Felicidad | carita | |
| Salud | corazón con pulso | |
| Inteligencia | ampolleta | |
| Apariencia | estrella | Se hereda de la mamá y el papá (más el azar de la partida). Cambia con deporte, sueño, carrete y salud; sube en la juventud y baja desde los 45. Pesa al buscar pega |
| Energía | rayo | Baja con la pega, el estudio, un horario lleno, los hijos chicos y la enfermedad; sube con tardes libres, dormir temprano y vacaciones. Con los años cuesta más recuperarla. Bajo 30 rindes menos. La batería del celular la refleja |

Colores verde, amarillo y rojo según el nivel. **Reputación** existe como barra aparte. **Fama** queda preparada pero apagada hasta que lleguen MiTubo y Mercado. Los **talentos heredables** quedan para más adelante.

### Íconos de los grupos

- Estilo **A, línea fina**, por defecto.
- En Configuración, "Estilo de íconos" permite cambiar a C (objetos de la vida) o B (color). Cada ícono nuevo se dibuja en los tres estilos.
- Idea abierta: estilos extra como compra cosmética, a decidir cuando se vea la monetización.

### Letras

**Fraunces** para títulos y **Figtree** para el texto. Nada de letra a mano: todo debe leerse fácil y verse adulto.

## Los 9 grupos y actuar con el tiempo parado

| Grupo | Qué tiene |
|---|---|
| Tu gente | Familia, amistades, pareja, conocidos, gente interesante |
| Estudios y trabajo | Colegio, universidad, talleres, la pega, minijuegos |
| Dinero | Banco, presupuesto, tarjetas, deudas, ahorro |
| Dónde vives | Arriendo, compra de casa, roomies, mapa de comunas |
| Compras | Tiendas, auto, lujo, lo que tienes |
| Salud | Plan de salud, ficha médica, médicos, salud mental |
| Celular | Correo, Mensajito, Instante, citas, apps |
| Trámites y leyes | Juzgado, Registro Civil, papeles, delitos, cárcel |
| Tu historia | Capítulos del año, diario de decisiones, el error que vuelve |

### Cómo se ve cada grupo

Dos niveles:

1. **Sección compacta**, al estilo BitLife: cara o ícono, nombre y una palabra o barra. Se entiende de un vistazo y la letra es grande.
2. **Ficha profunda** al tocar: lo vivido, lo que puedes hacer, lo que viene. Ahí está la profundidad que a BitLife le falta.

### Acciones inmediatas

Con el tiempo parado, cada grupo ofrece acciones que **dan resultado al tiro** (la barra sube o baja en verde o rojo, con sonido) y **gastan tardes libres** del mes. Ejemplos: estudiar un ramo, hablarle a alguien, pedir un aumento, ir al médico, abrir una cuenta. Las acciones no son cuestionarios para avanzar: son decisiones con consecuencia.

Isaias eligió (17-11-2026) **el contador del mes**: "Te quedan 5 tardes libres", y cada acción gasta una. El número sale del horario real: los bloques vacíos o con tiempo muerto (tele, celular). También eligió que las acciones vivan **dentro de cada grupo** (boceto D3Grupo), con una franja del contador en la pantalla principal (boceto D3Contador) y el resultado al tiro (boceto D3Resultado). Las primeras acciones son estudiar para la prueba, repasar, estudiar con un compañero, juntarte con un amigo, salir con tu pareja, pasar la tarde con tu hijo, hacer deporte y descansar. Pedir un aumento ya está en la pega. Ir al médico llega con Salud (D8), porque necesita la previsión y las listas de espera reales.

## Tu gente y la pareja

### Quién aparece

- **Cercanos arriba** y una sección **"Conocidos" cerrada** debajo.
- **Cada persona tiene vida real:** oficio o profesión, cargo, empresa, lo que le pasa.
- **La relación se ve con barra, cara y palabra.** Con un desconocido la barra está gris y casi vacía. Nada de números.
- **Boceto elegido (18-11-2026): A.** Los cercanos van en tarjetas de papel con su vida en una línea, la barra y la palabra; la familia primero, después amistades, rivales y ex; quien murió al final, sin barra. "Conocidos · N" va plegado abajo. Al tocar a alguien se abre su ficha: su vida, lo que han vivido juntos y lo que pueden hacer.
- **Las pegas de la gente son reales (regla de Isaias, 18-11-2026).** La gente solo trabaja en los trabajos que existen enteros en el juego (hoy software, contabilidad, psicología, vendedor, garzón y maestro); no hay listas de pegas con solo el nombre. Y solo la parte real según el INE tiene una de esas pegas; el resto muestra lo que es real (estudia, jubilado, busca pega). Cuando se agrega una carrera u oficio, aparece gente trabajando en ella.

### Cómo nacen las amistades

- **No se forman solas.** Solo una amistad de la infancia llega automática.
- Las demás nacen de decisiones del jugador en momentos (por ejemplo, aceptar estudiar con Tomás) o de buscarlas.
- El juego empuja mostrando **gente interesante o influyente** donde de verdad estaría según tu horario: en tu trabajo, tu empresa, tu club.
- **Lo que una persona te puede aportar** se descubre después de la primera conversación.
- **Cómo se hizo la gente interesante (D4b2, Isaias eligió el 20-11-2026 una sección propia arriba y el boceto A, tarjetas que se deslizan):** "Gente que podrías conocer" muestra hasta 3 personas reales de tu horario: alguien con un cargo más alto en tu empresa (solo cargos que existen en una empresa de ese tamaño) o alguien que hace clases en tu carrera y trabaja en ella. Antes de hablarle, lo que te puede aportar está oculto y solo puedes acercarte a conversar (gasta una tarde y te puede no pescar; a la segunda vez se cierra la puerta). Después se ve lo que de verdad puede hacer por ti: un consejo de lo tuyo, recomendarte para un ascenso si manda en tu área, recomendarte en su pega o prestarte plata. Si dejas esa pega o esa carrera, ya no la ves. Quedan para cuando existan: clientes y proveedores de tu empresa propia, y tu club.
- **Cómo se hizo (D4b, aprobado por Isaias el 18-11-2026):** cuando con alguien del curso, la sección o la pega se tienen cariño de ida y vuelta, esa persona te invita a algo real (un cumpleaños, estudiar juntos, un café después de la pega). Si aceptas nace la amistad; si no, queda como conocido. Ya no se forman solas como en R3a, R5c y R6a.

### Acciones

Hay acciones base (hablar, invitar a salir, pedir ayuda) y otras que **nacen de lo vivido** juntos (recomendarlo en tu pega, ser socios, vivir juntos). Todas gastan tardes libres.

### La gente actúa sola

Las personas también escriben, invitan o piden cosas por su cuenta, **sin repetirse**. Los cercanos se simulan mes a mes y los conocidos una vez al año.

**Cómo se hizo (D4c, Isaias eligió "la mezcla" el 19-11-2026):** lo chico llega por Mensajito y lo contestas cuando quieras; sale de la vida real de quien escribe (tu mamá jubilada quiere conocer tu casa, tu papá se quedó sin pega, tu amiga te avisa de una vacante en su empresa y si le dices que sí te recomienda, te invitan a un cumpleaños la semana del cumpleaños) y cada mensaje tiene su tope por persona. Lo grande para el tiempo como momento: que un amigo te pida ser aval de su arriendo (si no paga, te toca a ti). En la ficha de cada persona se puede conversar, invitar a salir, pedir plata prestada, pedir que te recomiende en su pega y hacer las paces; la persona puede decir que no.

### Pareja

- **La iniciativa es del jugador**, con empujón del juego (app de citas, Instante, gente que conoces).
- **Etapas reales:** conocerse, andar, una noche, pololeo, convivir, acuerdo de unión civil o matrimonio, separación o divorcio.
- **Mundo propio de pareja:** citas, planes, viajes, celos, infidelidad, negocio juntos, capítulos propios. La separación conecta con el juzgado.
- **Infecciones de transmisión sexual** (incluido VIH) como consecuencia real, con la recomendación de ir al médico.
- Lo sexual es solo para adultos, por las reglas de Google Play.
- Se toman muchas mecánicas de BitLife como referencia, sin copiar su aspecto.

**Cómo se hizo (D4d1, aprobado por Isaias el 19-11-2026, con "conocidos y gente de tu horario"):** la pareja siempre es alguien real de tu vida. Sale de tus conocidos (tu curso, tu pega, tus amistades) o de gente nueva con nombre que conoces en los lugares reales de tu horario (un carrete, la cancha, la casa de tus amigos, la pega, el campus), que queda como conocido. Desde su ficha la invitas a una cita y la relación avanza por etapas: saliendo, pololeo, vivir juntos, matrimonio o acuerdo de unión civil (con las leyes del país), terminar (si están casados, sigue el juzgado) y la viudez que pidió Isaias (el duelo, el primer año, y en Tu gente sigue como "Fue tu pareja"). La ficha dice la etapa ("Pololeando desde hace 2 años", "Casados"). La otra persona también toma la iniciativa: te invita a salir, te pide pololeo, te propone vivir juntos; y si a esa persona no le gustas, te dice que no. Los hijos, las cuentas de la casa y la herencia de la pareja piden vivir juntos. Se sacó el evento que inventaba una pareja de la nada y la marca de "pololeando" sin persona.

**Cómo se hizo (D4d2, Isaias eligió "insinuada, solo adultos" el 20-11-2026):** la infidelidad siempre es con alguien real y se cuenta sin detalles. Si cedes, queda una semilla con esa persona como testigo, que años después le cuenta todo a tu pareja (el error que vuelve). Tu pareja también puede engañarte con alguien con nombre de su vida, sobre todo si la relación anda mal; tú decides si perdonas o terminas. Los celos nacen de lo que haces (una amistad con química, tu ex que te escribe). Los viajes se hacen desde la ficha de tu pareja, con su costo, y dejan la foto en la casa. Cada cosa tiene su capítulo del año.

## Estudios, trabajo y minijuegos

### Los lugares

Colegio, universidad y trabajo se ven **en 3D con su gente**. Al tocar a alguien se abre su ficha. El diario de decisiones queda escondido, porque el estilo diario de BitLife no le gusta a Isaias.

### Estudiar

- Con el tiempo parado, estudiar es **un texto breve más preguntas reales del ramo**. Se vuelve al plan inicial de preguntas por carrera.
- Cada ramo tiene una **barra de dominio**. Subirla mejora las notas que vienen, no cambia las pasadas. Cada sesión gasta una tarde libre. Hay recuperativas reales.
- **Notas según el país**, en su paquete: Chile de 1 a 7, Argentina de 1 a 10.
- El contenido parte por Lenguaje, Matemática, Historia y Ciencias (programas del Mineduc), más los primeros ramos de las tres carreras.
- **También se puede:** pedir ayudantías, hablar con profesores o el director, copiar, sobornar (con consecuencias), hacer la cimarra, entrar al centro de alumnos (con certificado) y tomar talleres (actuación, música, fútbol) que abren carreras futuras.

**Cómo se hizo (D5a, Isaias eligió el 21-11-2026 "mezcla", "texto breve y 3 preguntas", "por año, con olvido real", "básica primero", "con gente" y la lista A, filas con barra):** en Estudios están tus ramos, cada uno con su barra de dominio, su palabra (te cuesta, vas a medias, vas bien, lo dominas), el promedio y la prueba que viene. Una tarde de estudio es un texto breve del tema que va y 3 preguntas reales del programa del Mineduc: al acertar suena y sube la barra; al fallar se ve por qué y la barra no baja. Se puede estudiar sola, con una compañera real del curso que te quiere (responde lo mismo y se suben el cariño) o con el profe jefe, que te explica lo que fallas. Tú estudias y el personaje rinde: la nota sale del dominio, la inteligencia y la energía. El dominio es por año, parte de lo que llevabas el año anterior y se olvida de a poco si no estudias. La recuperativa es la real: si faltas el día de la prueba, la das la semana siguiente. Si estudias seguido, en la mesa de la casa 3D aparece tu cuaderno de apuntes. Primero de 1º a 8º básico (1.280 preguntas); media, carreras, copiar, cimarra, centro de alumnos y talleres vienen en sus propios PR.

### La pega

- **Misiones mes a mes** para llegar a 100 y ascender.
- Hablar con la jefa o los colegas, capacitaciones, certificaciones, ascensos, y llegar hasta **accionista**.
- Una barra muestra **cómo te ve tu jefa**, con una frase que lo explica.

**Cómo se hizo (D5b, Isaias eligió el 22-11-2026 "mezcla de las dos" y, para el 100, "las dos cosas"):** cada mes te toca una tarea real de tu cargo con su plazo (de 6 a 8 distintas por área, que se turnan) (un viernes del mes, o su fecha real: el IVA el 20, Previred el 13). Cuando vence, es un momento con tu jefa o un colega y eliges cómo hacerla: con calma (te quedas tarde), rápido (si te la puedes sale bien, si no con errores), con ayuda de un colega (sale bien, pero se nota que no fue solo tuyo) o dejarla para la otra semana. En la pantalla del equipo está la barra "cómo te ve tu jefa" con su frase y la agenda del mes. Con 100 tu jefa te ofrece el ascenso si el cargo siguiente ya se puede; antes lo puedes pedir y arriesgarte. En el cargo nuevo hay que volver a mostrarse (la barra parte en 60). Los oficios siguen con sus metas. **Cómo se hizo (D5b2):** casi todas las tareas (45 de 50) se pueden resolver "con lo que aprendiste en la carrera": tres preguntas reales de los temas de tus ramos que tienen que ver con esa tarea (el IVA pregunta del IVA y del F29; el finiquito, del término de contrato). Si aciertas las tres te luces (+4, mejor que con calma); con ninguna sale mal (-3) y queda en tu diario. Así lo que estudiaste sirve en la pega.

### Minijuegos

Deben ser **jueguitos reales**: cortos, visuales, de dedo y bien adornados. Nunca un cuadro con texto. Cada uno es una tarea real del oficio. Ejemplos:

- **Contabilidad:** arrastrar boletas a su lugar y revisar con lupa.
- **Software:** atrapar bugs.
- **Psicología:** leer la cara del paciente. Esta sí puede llevar el texto del caso.

Los bocetos de minijuegos de R6e fueron rechazados por genéricos. Se rehacen con esta regla.

## Dinero, Dónde vives y Compras

### Dinero

- **Sección propia y con vida.** Sus bocetos vienen después.
- **El jugador arma su presupuesto.** Cada cosa que activa entrega algo positivo con una razón real: el auto da tiempo y puntualidad, el plan del celular da contacto, el gimnasio da salud, energía y gente.
- **Productos, en orden:** cuenta, tarjeta, crédito de consumo y ahorro primero. El hipotecario llega con Dónde vives y las inversiones con Mercado. También depósito a plazo y fondo a tasa fija.
- **Puntaje bancario de 1 a 999**, con la forma de cada país (Equifax en Chile, Veraz o BCRA en Argentina: por verificar). Estar en DICOM lo hunde.
- **Tarjetas** clásica, gold, platinum y black. Sus beneficios (millas, cashback, descuentos) se conectan con compras, gimnasio y universidad.
- **Tres bancos por país** con nombres parodia: uno estatal, uno grande y uno digital.
- **Deudas:** el juicio se lleva a fondo en Trámites y leyes, con aviso en Deudas, un correo y "lo que se dice de ti".
- **Pagos automáticos sin avisos** para lo que se repite, con un resumen de gastos.
- Para más adelante: una **mochila**, colección de las cosas de la vida.
- **Seguro de Cesantía (acordado con Isaias el 22-11-2026):** se cobra solo al perder la pega, sin que el jugador haga nada, pero se tiene que notar. El núcleo ya lo paga (N13). Cuando se haga esta sección: el seguro como un plan visible en Dinero con su saldo, la notificación del banco cuando llega cada pago y la cartola con la descripción de cada movimiento. El correo de la AFC trae el documento como adjunto cuando existan los adjuntos (Correo).

### Dónde vives

- Arrendar, comprar con pie y crédito hipotecario, subsidio según el país (DS1 y DS49 en Chile), dividendo y contribuciones.
- **Roomies:** vivir hasta con tres personas y pagar a medias.
- Vender, y arrendar tu casa a otros.
- **Mapa:** cada país tiene 4 lugares (2 de clase baja, 1 media, 1 alta). En Santiago son las comunas **Puente Alto, La Pintana, Maipú y Las Condes**.
- **9 viviendas por país** (3 por clase), distintas en cada país y en 3D.
- **Mudarse se siente:** con datos reales (Casen, precios, delitos) y gente acorde al lugar, sin estereotipos.
- Después: un mapa mundial para la migración.

### Compras

- Tiendas por tipo, con precios reales, más lujo: yates, aviones, cruceros.
- **Todo lo comprado se usa** y tiene algo que hacer (la diferencia con BitLife), en 3D cuando se pueda.
- Pago al contado, con débito o en cuotas, con beneficios de la tarjeta.
- **Auto:** licencia con examen de señales (preguntas), aprobar o reprobar, auto nuevo, usado o heredado, crédito, permiso de circulación, SOAP, revisión técnica, mantención, bencina, TAG y multas, con seguro.
- **Fiestas** (18+) como lugares del mapa: con alcohol, volver en auto o caminando puede terminar en multa, robo, choque o cárcel.
- **Lo que tienes se gasta:** desgaste, robos según la comuna, embargo del auto o la casa.

## Salud

Isaias aprobó todo lo recomendado, más sus agregados. Cada cosa gasta tiempo y plata.

- **Plan según el país.** Fonasa e Isapre solo en Chile. Cada país trae su sistema en su paquete.
- **Ficha médica** como documento real: diagnósticos, remedios, alergias y **riesgos por hábitos** (sedentarismo, comida, alcohol), con gravedad y alertas.
- **Atención:** Cesfam gratis con espera, o particular pagado. Urgencias.
- **Calidad según la comuna**, con una barra y datos reales (en Las Condes el pronóstico es mejor).
- **Chequeos** con distintos especialistas y precios.
- **Salud mental:** taller con preguntas, psicólogo y psiquiatra.
- **Fertilidad:** examen de semen, congelar óvulos. También adopción y cambio de género, según las leyes de cada país.
- **Enfermedades con aviso** y un sonido triste propio.

### Licencias médicas (S2, se hace dentro de D8)

Isaias lo pidió el 22-11-2026, después de ver la propuesta de S2 (le gustó, pero se hace junto con D8):

- **Licencias que se noten.** Como el tiempo avanza por mes o por año, una licencia de 10 días no se sentiría. Las licencias del juego son largas y notorias: **un mes, dos o tres meses**, por un accidente grave o una enfermedad seria. Nacen siempre de algo que te pasó y de ir al médico, nunca de la nada.
- **Mientras dura, toda la pantalla lo dice.** El menú muestra que estás con licencia, cambia el color de la pantalla principal y cambia la escena 3D de la casa, para que se entienda que tienes algo de salud peligroso. Es lo mismo que pasará con la cárcel: todo cambia mientras dura.
- **Si avanzas un año, queda el registro.** Si el jugador aprieta "Un año", el tiempo solo para en lo importante, pero la licencia y su causa quedan anotadas: qué te pasó, qué accidente o enfermedad fue y qué secuelas te dejó.
- **Enfermedad grave.** Con cáncer u otra enfermedad que no te deja volver a trabajar, existe la salida real de Chile: la **pensión de invalidez**, que decide la comisión médica. Hay que revisar la ley antes de programarla.
- Sigue en pie lo de la propuesta: el subsidio por incapacidad laboral de Fonasa (COMPIN) o de la isapre en la liquidación, que la isapre te la pueda rechazar (llega un correo y puedes reclamar), no ir a la pega mientras dura, que tu jefa lo note, y el papel de la licencia en la casa 3D. A honorarios o sin contrato no hay licencia pagada.

## Celular, correos, noticias y capítulos

### Celular

- **Un celular real:** hora, fecha, batería y todas las apps. La batería refleja la barra de Energía.
- **Orden de las apps:** Instante y citas, luego banco, luego Mercado y MiTubo (con Fama).
- **Tienda de apps propia** del juego para instalar las apps.
- **Instante:** fotos de los momentos tomadas de la escena 3D, que el jugador sube o se suben solas. Los likes vienen de personas con nombre (amigo, pareja, mamá, conocido, famoso) y se puede hablar con quien dio like. Deja huella digital.
- **App de citas** (18+), estilo deslizar con X o like. El perfil muestra lo que muestra una app real (edad, trabajo, comuna, intereses, fotos según Apariencia). Las barras de la otra persona salen grises y se descubren en las citas.

### Correo

- Leer todo, archivar, eliminar y filtros discretos.
- **Adjuntos reales:** contrato, carta de despido, denuncia, certificados.
- Los títulos se reciben **en persona**, con ceremonia y 3D. Por correo solo llega lo que en la vida real llega por correo.

### Noticias de tu mundo

Al centro de la pantalla y con animación, en dos tamaños:

- **Portada del diario:** solo cuando es una noticia de verdad (hechos del año, de hace dos años).
- **"Lo que se dice de ti":** chismes y lo que comenta tu gente.

### Tu historia

- **Capítulos del año** al centro, impactantes.
- **Diario de decisiones** escondido, para quien quiera revisarlo.
- **El error que vuelve**, con Volver al pasado ahí mismo.

## Trámites y leyes, delitos y cárcel

Es un grupo propio. Se ve como **edificios del mapa** (juzgado, Registro Civil, notaría, comisaría), no como una carpeta.

- **Juzgado:** con abogado de la Corporación de Asistencia Judicial o particular. Deudas, alimentos, divorcio, despidos.
- **Registro Civil:** nacimiento, matrimonio, unión civil, cambio de nombre.
- **Papeles:** impuestos (SII), licencia de conducir, certificados.
- **Notaría y policía.**
- **Migración**, más adelante.

### Delitos propios

- El jugador puede cometer delitos, cada uno con su **minijuego**.
- **La víctima se elige de tu círculo** y el impacto se conecta con todo: robarle el auto al jefe cambia tu pega, tu relación con él y tu reputación.
- Quedan **antecedentes** que pesan después (trabajo, visas, arriendo).

### Cárcel

Un **módulo grande con mundo propio**: reglas, gente y mecánicas propias. Mientras estás preso cambian los módulos que puedes usar y cambia la escena 3D.

## Arquitectura C y el mapa por país

**Decisión definitiva de Isaias**, pensando en 20 a 100 países. El detalle técnico está en `docs/architecture.md`.

1. **Base de datos** con todo el contenido: países, comunas, carreras, eventos, precios, leyes, bancos.
2. **Página web con formularios** para llenar y revisar esos datos **sin programar**.
3. La página **genera paquetes** (los mismos paquetes JSON que ya usa el núcleo).
4. **El juego descarga** los paquetes y después **juega sin internet**.
5. Países nuevos y casas 3D llegan **como descargas**, que también pueden ser expansiones pagadas.

**Nombres propios de cada país (regla de Isaias, 22-11-2026):** lo que en Chile tiene un nombre conocido (Seguro de Cesantía, AFC, Fonasa, isapre, SII, DICOM) se llama así solo en el paquete de Chile. El núcleo usa un nombre general (por ejemplo, "seguro de desempleo") y cada país pone su nombre, su institución y sus reglas en su paquete y en sus textos, para que cambiarlos sea fácil al agregar un país.

Cada país trae la misma forma con datos propios: 4 lugares (2 de clase baja, 1 media, 1 alta) y 9 viviendas en 3D; escala de notas, sistema de salud, puntaje bancario, 3 bancos y leyes; precios, salarios y datos reales con su fuente.

## Lo que sigue valiendo de los documentos anteriores

Todo lo que este documento no cambia sigue igual: el ritmo "rutina + momentos" (un momento detiene el tiempo solo si tiene persona o lugar con nombre y algo en juego), el horario de 14 bloques, lo que quiero, el error que vuelve, los capítulos del año, la regla de no repetir sin razón, y toda decisión que se pueda ver se ve en 3D. Los colores y la escena 3D modificable de `docs/archivo/diseno-app.md` siguen como referencia técnica hasta que se boceteen las pantallas nuevas.

## Para más adelante y próximos pasos

### Para más adelante

- Fama (preparada, apagada), talentos heredables, la mochila de cosas de la vida.
- Mercado e inversiones, MiTubo.
- Migración con mapa mundial.
- Estilos de íconos como compra cosmética (decidir con la monetización).
- Pantalla de inicio y Configuración.

### Próximos pasos

1. **Bocetos antes de programar** cada módulo, con opciones para que Isaias elija: los botones de avanzar ya están elegidos; siguen la ficha de persona, Estudios y Dinero.
2. Recién con bocetos aprobados se programa, un módulo por PR (ver la serie D en `docs/roadmap.md`).
