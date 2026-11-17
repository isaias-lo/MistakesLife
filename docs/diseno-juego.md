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

## Pantalla principal, barras, íconos y letras

### Pantalla principal (aprobada, boceto S1Filas)

De arriba hacia abajo:

1. **La escena 3D** del lugar donde estás, ocupando menos espacio que hoy.
2. **Lo que pasa:** el momento o la noticia del mes, con letra grande y legible.
3. **Cinco barras grandes** con ícono propio y porcentaje.
4. **Botones:** el círculo chico de Volver al pasado (solo si lo compró), "Seguir un mes" ancho y "Un año" al lado.
5. **Barra de grupos** abajo: Tu gente, Estudios, Dinero, Celular y Más (donde están los otros grupos).

Al entrar al colegio, la universidad o el trabajo, la escena muestra el lugar con su gente.

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

### Cómo nacen las amistades

- **No se forman solas.** Solo una amistad de la infancia llega automática.
- Las demás nacen de decisiones del jugador en momentos (por ejemplo, aceptar estudiar con Tomás) o de buscarlas.
- El juego empuja mostrando **gente interesante o influyente** donde de verdad estaría según tu horario: en tu trabajo, tu empresa, tu club.
- **Lo que una persona te puede aportar** se descubre después de la primera conversación.
- Esto cambia lo hecho en R3a, R5c y R6a, donde hoy las amistades se forman solas.

### Acciones

Hay acciones base (hablar, invitar a salir, pedir ayuda) y otras que **nacen de lo vivido** juntos (recomendarlo en tu pega, ser socios, vivir juntos). Todas gastan tardes libres.

### La gente actúa sola

Las personas también escriben, invitan o piden cosas por su cuenta, **sin repetirse**. Los cercanos se simulan mes a mes y los conocidos una vez al año.

### Pareja

- **La iniciativa es del jugador**, con empujón del juego (app de citas, Instante, gente que conoces).
- **Etapas reales:** conocerse, andar, una noche, pololeo, convivir, acuerdo de unión civil o matrimonio, separación o divorcio.
- **Mundo propio de pareja:** citas, planes, viajes, celos, infidelidad, negocio juntos, capítulos propios. La separación conecta con el juzgado.
- **Infecciones de transmisión sexual** (incluido VIH) como consecuencia real, con la recomendación de ir al médico.
- Lo sexual es solo para adultos, por las reglas de Google Play.
- Se toman muchas mecánicas de BitLife como referencia, sin copiar su aspecto.

## Estudios, trabajo y minijuegos

### Los lugares

Colegio, universidad y trabajo se ven **en 3D con su gente**. Al tocar a alguien se abre su ficha. El diario de decisiones queda escondido, porque el estilo diario de BitLife no le gusta a Isaias.

### Estudiar

- Con el tiempo parado, estudiar es **un texto breve más preguntas reales del ramo**. Se vuelve al plan inicial de preguntas por carrera.
- Cada ramo tiene una **barra de dominio**. Subirla mejora las notas que vienen, no cambia las pasadas. Cada sesión gasta una tarde libre. Hay recuperativas reales.
- **Notas según el país**, en su paquete: Chile de 1 a 7, Argentina de 1 a 10.
- El contenido parte por Lenguaje, Matemática, Historia y Ciencias (programas del Mineduc), más los primeros ramos de las tres carreras.
- **También se puede:** pedir ayudantías, hablar con profesores o el director, copiar, sobornar (con consecuencias), hacer la cimarra, entrar al centro de alumnos (con certificado) y tomar talleres (actuación, música, fútbol) que abren carreras futuras.

### La pega

- **Misiones mes a mes** para llegar a 100 y ascender.
- Hablar con la jefa o los colegas, capacitaciones, certificaciones, ascensos, y llegar hasta **accionista**.
- Una barra muestra **cómo te ve tu jefa**, con una frase que lo explica.

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
