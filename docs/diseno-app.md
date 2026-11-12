# Diseño de la app (acordado con Isaias el 2026-11-09)

Este documento dice **cómo se ve y cómo se siente** MistakesLife en el celular. Lo acordamos en el hilo "Diseño app" mirando los bocetos de este lienzo:
https://claude.ai/artifact/XBjFnXciYgEPhkNvKRK3Vn
Las filas 1 a 3 son la primera ronda y la fila 4 es la segunda. Donde no coinciden, manda la fila 4.

Antes de programar una pantalla nueva en `unity/`, léelo. Si una pantalla nueva necesita algo que este documento no dice, primero se propone con bocetos y Isaias lo aprueba.

## 1. La idea

**Tu vida es una maqueta viva.** La escena 3D es la protagonista: ocupa toda la pantalla y cambia con la hora, el clima, la etapa de la vida y las decisiones. Cada tema de la vida se abre como **un objeto real de esa vida**, nunca como un menú genérico:

| Tema | Objeto en 3D (se toca para abrir) | Pantalla 2D (su material) |
|---|---|---|
| Tu dinero | La mesa de la cocina a fin de mes, con los gastos del mes como objetos (contrato, bolsas del súper, tarjeta del metro); alcancía en la repisa, carta de cobranza bajo la puerta | La cartola del banco: cada movimiento con la decisión que lo causó |
| Estudios | Libreta y cuadernos en el escritorio, mochila del colegio | Libreta de notas chilena (1,0 a 7,0, rojo bajo 4,0), asistencia y PAES |
| Trabajo / buscar pega | Diario con avisos o notebook | Diario mural de corcho con las postulaciones como fichas (postulaste, entrevista, oferta, no quedaste) |
| Redes | Celular en el velador o en el escritorio | El celular del personaje con sus apps parodia (Instante, Mensajito, Flechazo, Sonar, MiTubo) |
| Juzgado | Carta del tribunal bajo la puerta | Carpeta de cartulina con RIT, tribunal, timbres y la línea del proceso con sus plazos |
| Tu gente | Fotos en la repisa, figuras en la casa | (por diseñar) |

Regla que sigue igual: **lo físico en 3D, lo interno en 2D** (doc de diseño y regla 10 de `CLAUDE.md`).

## 2. Reglas visuales

### Colores

| Nombre | Color | Uso |
|---|---|---|
| Noche (fondo) | `#141B2D` | Fondo de toda la interfaz. A Isaias le gustó: no se cambia |
| Noche 2 | `#1E2740` | Botones y tarjetas sobre el fondo |
| Texto | `#F3F5FA` | Texto principal sobre el fondo |
| Texto suave | `#C3CBDD` | Texto secundario (nunca más oscuro: debe leerse bien) |
| Amarillo del jugador | `#F5C842` | **Solo lo tuyo**: tu personaje en 3D, tu botón principal ("Vivir la semana"), lo que elegiste. Ninguna otra cosa es amarilla |
| Papel | `#F4EFE4` | Tarjetas de eventos, boletas, hojas (con tinta `#1C2236`) |
| Bien / mal | `#4FD1A5` / `#FF8A7A` | Cambios buenos y malos, siempre acompañados de palabras |

Cada tema tiene además **su material**: verde billetera `#1D4A3C`, azul libreta `#24407A`, corcho `#A87A4C`, cartulina `#D9B36E`, celular `#0B0F1A`.

### Letras

- **Títulos:** Fraunces (cálida, como de novela).
- **Texto:** Figtree.
- **Montos, RIT, boletas:** IBM Plex Mono.
- **Cada personaje escribe a mano con su propia letra:** el diario del jugador con Gochi Hand, la mamá con Caveat y el profesor con Patrick Hand. Más adelante la letra del jugador puede ser la de Isaias, convertida en fuente.
- Todas son de Google Fonts con licencia OFL (se pueden incluir en el juego).
- En Unity se usan con **TextMeshPro**, no con el `Text` antiguo, que se ve borroso.
- Los archivos `.ttf` están en `unity/Assets/MistakesLife/Resources/Fuentes` (y sus licencias en `Licencias`). Para cambiar una letra, se reemplaza su archivo con el mismo nombre; `Fonts.cs` dice qué archivo usa cada tipo de texto.
- **Tamaño mínimo 12 px** (en el lienzo de 390 px de ancho) y botones de 44 px de alto como mínimo.

### Tono

- Nada de números sueltos sin explicación: el dinero se cuenta en **días de trabajo** ("marzo te costó 34 días de trabajo"), y la salud y el ánimo con palabras ("Bien", "Cansada").
- Palabras de Chile en las claves de idioma de Chile ("pega", "cimarra", "DICOM", "RIT").
- Menos etiquetas: los objetos de la escena **reaccionan** (un sobre que late, el celular que se ilumina, una caja nueva) y el jugador toca para descubrir. Como mucho un aviso corto a la vez ("Pasaron algo por debajo de la puerta").

## 3. Pantallas

### Pantalla de la vida (boceto "La casa que guarda recuerdos")
- La escena 3D ocupa toda la pantalla; abajo se oscurece para leer.
- **Arriba:** retrato del personaje con anillo amarillo, nombre y edad, y el día, la hora y el clima ("miércoles 19:40 · llovizna, 9°"). A la derecha está el botón Menú.
- **Opcional arriba:** la tira de la semana (L a D, con lo que pasa cada día: "pega", "pago", "asado", "mamá"). Viene del boceto de la ronda 1.
- **Cambiar de lugar:** Casa, Barrio y Pega a la derecha. Más adelante el barrio será un recorrido.
- **Abajo, en orden:**
  - una línea del diario, con la letra del jugador;
  - Salud, Ánimo y Mente en palabras;
  - la fila "tus cosas": Dinero, Pega, Salir, Celular y Tu gente, con punto rojo si hay algo nuevo;
  - el botón amarillo "Vivir la semana" y, al lado, "Seguir viviendo".
- **La casa guarda recuerdos:** trofeos, fotos, la planta que se secó, la caja de la mamá. Son objetos de escena con condición en `packs/*/scene/` que se van sumando a lo largo de la vida.

### Un momento (evento)
- La cámara 3D se acerca al lugar donde pasa (la oficina, la casa) y aparecen los nombres de quienes miran (los testigos de las semillas).
- La tarjeta es de papel y sube desde abajo:
  - "DÍA CLAVE" si es `dia_clave`, y el sello "ESTO SE RECUERDA" si deja semilla;
  - título y texto;
  - opciones grandes, cada una con una línea de quién lo va a notar;
  - la intuición de "volver al pasado" con letra a mano.

### Tu dinero
Acordado el 2026-11-10 (bocetos en el lienzo, fila 5: "C. La mesa de la cocina" y "A. La cartola del banco"). Reemplaza la idea anterior de los boletos y la mochila.

**Regla de Isaias: nada se inventa.** Todo gasto nace de una decisión del jugador (un contrato que firmó, una compra, una rutina que armó) con el precio real de su país. Si no decide nada, no aparece un cobro: aparece la consecuencia (la despensa vacía, la luz cortada, el aviso de desalojo). Por eso primero van los módulos de gastos (vivienda, supermercado, transporte, cuentas, celular, gastos personales, hijos) y Tu dinero se programa después, mostrando solo lo que existe.

- **Pantalla principal: la mesa de la cocina (3D).** A fin de mes, sobre la mesa aparecen los gastos como objetos: el contrato del arriendo con tu firma, las bolsas del súper, la tarjeta del metro, el celular, un regalo de una salida, el sobre del sueldo. Tu personaje está al lado. Arriba, cuánto te queda.
- Al tocar un objeto sube una hoja: qué es, de qué decisión viene ("Elegiste ir en metro en abril"), lo que costó este mes, tu rutina, cuántos días de sueldo es, y dos botones para cambiar esa decisión.
- **Detalle: la cartola del banco.** Un papel que sale del cajero con cada movimiento por fecha y su motivo ("Contrato que firmaste en marzo"); tocar una línea lleva a la decisión que la causó.
- Las rutinas (decidir una vez y que se repita, por ejemplo la compra de la semana) se ven y se cambian aquí; si el precio sube, la rutina avisa.
- **Por edad:** de 0 a 12 años no hay pantalla, solo se nota en la casa; de 13 a 17, la mesada y la alcancía en tu pieza; en la jubilación, el mes se mide con la pensión.

### Estudios, Buscar pega, Redes y Juzgado
Siguen los bocetos de la fila 3, con las letras y los tamaños de la ronda 2. Cada una comparte la barra de arriba (volver a tu casa, título, quién eres) y el amarillo para lo tuyo. Se programan cuando el núcleo tenga sus datos.

- **Redes, hecho en U21** (`PhoneScreen`): el botón "Celular" en tus cosas (con el número y el punto rojo si hay correos sin leer) abre el celular encima de la vida, en su material `#0B0F1A`: arriba Volver, el título y Cerrar; adentro las apps en filas de tres (el correo con su punto rojo; las otras dicen "Todavía no hay nada aquí" hasta que tengan su capa). La bandeja muestra quién manda, el asunto y hace cuánto, los sin leer en negrita con punto rojo; un correo se abre en papel y queda leído. En la casa, el celular está sobre la mesa y su pantalla se ilumina con un punto rojo mientras haya correos sin leer (`escena.casa.celular` y `escena.casa.celular_avisos`). Capturas `pantalla-celular.png` y `pantalla-correo.png`.
- **U21b:** el celular ahora se dibuja como un celular (marco `#0B0F1A` con cámara, barra de estado con la fecha del juego, señal y batería, fondo de pantalla `#1C2B4D`, íconos de color con su inicial y globo rojo con el número, dock con las primeras cuatro apps y rayita de inicio) y el correo como una app de correo blanca (`#FAFAFC`): "Recibidos", círculo con la inicial de quien manda del color de su tema (la estafa usa el color del banco, porque el correo falso se ve igual al real), asunto, vista previa, "hace cuánto" y punto azul de sin leer. Cerrar va debajo del celular.

## 4. El personaje

- **Estilo "juguete de madera":** cabeza redonda, cara pintada (ojos de punto, mejillas, boca de una línea), pelo como casco y cuerpo de una pieza.
- **Envejece:** crece de niño a adulto, cambia de ropa según la etapa, a los 70 tiene pelo gris, lentes y bastón si corresponde.
- **Ropa amarilla** solo en el jugador.
- **Gestos simples:** contento, cansado, triste, según el ánimo.
- **Hecho en U18c** (`RoomBuilder.Person` y `Face`): la cara mira hacia la cámara de la pieza; la boca sale de `HomeMemberView.Mood` (contenta desde 60 de felicidad, triste bajo 35); lentes en la vejez y bastón desde los 80. Falta el cansancio (según la salud) y el retrato en la barra de arriba.

## 5. Luz, tiempo y etapas

- **Un color de luz por etapa:**
  - infancia cálida y saturada;
  - juventud con contraste y colores de afiche;
  - adultez sobria (la base);
  - vejez en tonos de foto antigua.
- **Cambia solo la luz de la escena:** la interfaz sigue en azul noche.
- **La luz sigue la hora y el clima de la ciudad:** smog y lluvia en invierno, calor en enero.
- **Se mezcla con el ánimo:** si el jugador está mal, todo se ve más gris (ya existe `HomeScene.SetMood`).

## 6. Del 3D al 2D sin cortes

Tocar un objeto lo transforma en su pantalla (menos de un segundo, con sonido):

1. el objeto brilla en amarillo;
2. la cámara se acerca y el fondo se desenfoca;
3. el objeto se abre (de la alcancía caen boletos);
4. se arma la pantalla.

Al cerrar, vuelve al objeto. Lo mismo vale para el celular (Redes), la libreta (Estudios) y la carta (Juzgado). Para las animaciones se recomienda DOTween (gratis).

## 7. El 3D debe ser escalable y fácil de modificar (pedido de Isaias)

Hoy cada objeto y cada lugar (pieza, oficina, barrio) está armado **en código**, con cubos y esferas (`ShapeKit`, `RoomBuilder`, `OfficeBuilder`, `NeighborhoodBuilder`, `SceneObjectBuilder`). Isaias quiere más adelante cambiar el diseño de cada objeto, más vivo y más realista, **sin depender de programar**. Por eso el 3D debe quedar así:

1. **Un modelo por id.**
   - Cada objeto de escena (`escena.casa.diploma`, muebles, personas, edificios del barrio) se busca primero como modelo en `unity/Assets/MistakesLife/Resources/Modelos/<id>.prefab`.
   - Si existe, se usa ese.
   - Si no existe, se dibuja la forma simple de hoy como reserva.
   - Así se reemplaza un objeto arrastrando un modelo nuevo con el mismo nombre, sin tocar código, y nada se rompe si falta uno.
2. **Los lugares como piezas editables.**
   - La pieza, la oficina y el barrio pasan a ser prefabs que se abren y se mueven en el editor de Unity.
   - Cada uno tiene **anclas**: objetos vacíos con nombre, como `ancla.repisa`, `ancla.mesa` o `ancla.puerta`.
   - Los objetos de escena se ubican en su ancla. Para cambiar dónde va algo se mueve el ancla, no un número en el código.
3. **Materiales compartidos.**
   - Los colores y materiales viven en pocos archivos de material (madera, tela, papel, amarillo del jugador).
   - Cambiar uno cambia todo el juego.
   - Más adelante se puede cambiar el sombreado entero, por ejemplo a uno tipo dibujo con contorno (toon), en un solo lugar.
4. **Reglas que no cambian.**
   - Cuándo aparece un objeto lo sigue decidiendo el paquete (`packs/*/scene/*.json`, con condición).
   - Unity solo decide cómo se ve (regla 3 de `CLAUDE.md`).
5. **Herramientas para que Isaias haga o cambie modelos más adelante:**
   - **ProBuilder** (gratis, viene en Unity) para modelar dentro del editor.
   - **Blender** (gratis) para modelos más detallados.
   - Modelos de la Asset Store con cuidado: los paquetes muy usados se reconocen en muchos juegos.
   - Las capturas automáticas (`capturas.yml`) sirven para revisar cada cambio sin abrir Unity.

### Cómo cambiar un objeto hoy (pieza, barrio y trabajos)

- **Cambiar cómo se ve un objeto:** crea un prefab con el nombre de su id en `unity/Assets/MistakesLife/Resources/Modelos/`. Por ejemplo, `mueble.mesa.prefab` o `escena.casa.diploma.prefab`. El juego lo usa en vez de la forma simple. Si lo borras, vuelve la forma simple.
  - Muebles: `mueble.cama`, `mueble.cama_adulto`, `mueble.cama_doble`, `mueble.cuna`, `mueble.escritorio`, `mueble.mesa`, `mueble.sillon`, `mueble.tele`, `mueble.planta`, `mueble.alfombra`, `mueble.ventana`, `mueble.puerta`.
  - Objetos de escena: los ids de `packs/base/scene/casa.json` (`escena.casa.libros`, `escena.casa.diploma`...).
  - Barrio: `barrio.casa_humilde`, `barrio.casa_media`, `barrio.casa_acomodada`, `barrio.colegio`, `barrio.almacen`, `barrio.plaza`, `barrio.paradero`, `barrio.arbol`, `barrio.auto`.
  - Trabajos: `oficina_abierta.mesa`, `oficina_abierta.pizarra`, `oficina_abierta.puf`, `oficina_abierta.rincon`, `oficina_cubiculos.cubiculo`, `oficina_cubiculos.rincon`, `consulta.alfombra`, `consulta.sillon_terapeuta`, `consulta.sillon_paciente`, `consulta.mesa_centro`, `consulta.divan`, `consulta.libreria`, `consulta.titulo`, `oficina.escritorio`, `oficina.rincon`, `local.mostrador`, `local.estantes`. Comunes a todos: `trabajo.ventana`, `trabajo.ventana_grande`, `trabajo.planta`, `trabajo.archivador`, `trabajo.archivadores`, `trabajo.pantalla_extra`, `trabajo.letrero_mediano`, `trabajo.letrero_grande`.
  - El modelo se pone con su centro en el ancla y con la base en el piso (o en el muro, para cuadros y tele).
- **Cambiar dónde va algo:** menú *MistakesLife > Crear anclas de la pieza* (o *del barrio*, o *de los trabajos*, que crea `oficina_abierta`, `oficina_cubiculos`, `consulta`, `oficina` y `local`). En los trabajos también se mueven el jugador (`ancla.jugador`) y los colegas (`ancla.colega_1`...). Crea `Resources/Lugares/pieza.prefab` con un cubito por ancla. Muévelas, guarda, y el juego usa esas posiciones. Los objetos de la mesa (libros, cuentas, remedios...) siguen a `ancla.mesa`.
- **Cambiar un color o material de todo el juego:** abre `Resources/Materiales/` (`madera`, `madera_oscura`, `marco`, `papel`, `alarma`, `pantalla`, `piel`, `jugador`, `marca_jugador`) y cambia el color o el sombreado.

## 8. Orden sugerido para programarlo

1. **Base visual:** TextMeshPro con las letras, colores de la sección 2, tamaños mínimos.
2. **3D modificable** (sección 7): modelo por id con reserva, lugares con anclas y materiales compartidos. Va antes de rehacer pantallas para que todo lo nuevo nazca modificable.
3. **Pantalla de la vida nueva** (sección 3) y el personaje estilo juguete de madera (sección 4).
4. **Módulos de gastos que decide el jugador** (V1 vivienda primero, luego supermercado, transporte, cuentas, celular, personales e hijos) y después **Tu dinero**: la mesa de la cocina con la cartola de detalle.
5. **Luz por etapa, hora y clima** (sección 5) y las transiciones del 3D al 2D (sección 6).
6. **Estudios, Buscar pega, Redes y Juzgado**, cada uno cuando el núcleo tenga sus sistemas.
