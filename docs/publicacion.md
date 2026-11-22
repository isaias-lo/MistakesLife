# Publicación: lo que necesita el juego para salir en Google Play

Este documento junta **la otra parte del juego**: lo que no es la vida simulada pero hace falta para que Google Play acepte MistakesLife, para que se pueda jugar en varios idiomas, para cobrar sin romper la ley del proyecto y para que las partidas y las compras estén seguras.

**Estado: las 12 decisiones están tomadas** (Isaias, 21-11-2026, en el hilo "Preparar el juego para Google Play"). La sección 1 dice qué se eligió y el resto del documento ya está escrito con esas elecciones. No trae código: cuando una parte quede acordada, se programa en su propio PR (serie P del roadmap). `docs/diseno-juego.md` sigue mandando sobre cómo se juega y cómo se ve; las pantallas nuevas (menú inicial, Configuración, tienda) necesitan bocetos aprobados antes de programarse.

**La ley del proyecto también manda aquí:** el jugador siempre debe sentir que todo es real. Por eso la monetización tiene una regla propia: **nunca se vende algo que la vida no podría dar** (plata, notas, salud, sueldo, suerte). Se vende comodidad (sin anuncios), mecánicas declaradas (Volver al pasado, Segundo aliento) y mundos nuevos (países, ciudades, carreras), nunca resultados.

**Regla de ley de los nombres:** todo nombre de compra o mecánica suena realista a su situación ("Segundo aliento" para sobrevivir a una muerte, no "Revivir").

Los datos de Google Play se revisaron el 22-11-2026. Google cambia sus reglas cada año: antes de cada paso hay que volver a mirar la página oficial que se cita.

---

## Índice

1. [Las decisiones tomadas](#1-las-decisiones-tomadas)
2. [Lo que Google Play exige](#2-lo-que-google-play-exige)
3. [Idiomas](#3-idiomas)
4. [Privacidad y condiciones de uso](#4-privacidad-y-condiciones-de-uso)
5. [Configuración](#5-configuración)
6. [Menú inicial](#6-menú-inicial)
7. [Monetización: anuncios y compras](#7-monetización-anuncios-y-compras)
8. [Cuenta, respaldo y backend](#8-cuenta-respaldo-y-backend)
9. [Seguridad: trampas, compras y datos](#9-seguridad-trampas-compras-y-datos)
10. [Orden de trabajo (serie P)](#10-orden-de-trabajo-serie-p)

---

## 1. Las decisiones tomadas

| # | Pregunta | Elegido |
|---|---|---|
| 1 | ¿Cuenta de desarrollador personal o de empresa? | **Personal ahora.** La SpA se crea cuando el juego gane unos $1 millón al mes por 3 meses seguidos, o antes de empezar a gastar en serio, y la app se **transfiere** a la cuenta de la empresa (Google Play mantiene jugadores, descargas y reseñas). Antes de la primera venta: inicio de actividades en el SII como persona natural, y consultar a un contador |
| 2 | ¿Nombre del paquete (id de la app)? | **`com.<estudio>.<juego>`.** Isaias quiere un estudio de juegos y "MistakesLife" no es el nombre final: **los dos nombres quedan pendientes** y deben estar elegidos (y su marca revisada) antes de la primera subida a Play. El APK de prueba sigue con `com.mistakeslife.game` |
| 3 | ¿Para qué edad es el juego? | **Solo adultos (18+).** Lo sexual sigue siendo solo entre personajes adultos |
| 4 | ¿Idiomas? | **Español, inglés y portugués de Brasil desde el lanzamiento**, que sale con Chile y Argentina. Después vienen Brasil y quizá Estados Unidos como países. Los nombres propios y culturales no se traducen (Puente Alto, Fonasa, PAES) |
| 4b | ¿Quién traduce? | **Claude, por capas** (pantallas, vida, preguntas), con probadores nativos de cada idioma que revisan. Desde P5, **todo contenido nuevo llega en los tres idiomas en el mismo PR**. Nada de traducir en vivo con una API (rompe el juego sin internet) ni idiomas como descarga aparte (pesan menos de 1 MB) |
| 5 | ¿Cómo se manejan los textos en Unity? | **Las tablas del núcleo (`lang/`)**, como hoy. Cuando haya muchos idiomas, cargar solo el elegido |
| 6 | ¿Herramienta de anuncios? | **Google AdMob** al lanzar; mediación cuando haya unos 10.000 jugadores al día |
| 7 | ¿Qué anuncios y dónde? | **Con premio, opcionales, dentro de los momentos**, que compran información o una segunda oportunidad y nunca un resultado. Más **uno de pantalla completa cada 20 minutos de juego**, solo en una pausa natural y nunca en la primera media hora. Sin banner (sección 7.3) |
| 8 | ¿Cómo se vende Volver al pasado? | **Es una compra.** Prueba gratis **una sola vez en todo el juego**, en la primera muerte: un anuncio para **Segundo aliento** (sobrevivir) y otro para Volver al pasado. Después, los dos solo se compran. Volver al pasado es un **ícono de reloj de arena** (sección 7.5) |
| 9 | ¿Argentina gratis o de pago? | **Chile y Argentina gratis.** Las expansiones pagadas empiezan desde el tercer país. "Nueva vida" tiene dos caminos: **Al azar** y **Crear tu vida** (sección 6.2) |
| 10 | ¿Backend? | **Supabase** para el contenido (arquitectura C) y para revisar que las compras sean reales, con el plan gratis mientras alcance |
| 11 | ¿Cuenta obligatoria? ¿Dónde se guardan las vidas? | **Inicio de sesión automático con Google Play Games**, sin pantalla ni contraseña y sin bloquear a quien no lo tenga. El **respaldo de la vida va en "Partidas guardadas" de Google Play Games**, en la cuenta del propio jugador: no cuesta nada aunque haya 100.000 jugadores (sección 8) |
| 12 | ¿Dónde viven la política de privacidad y las condiciones? | **GitHub Pages** del repositorio; se pasa al dominio del estudio cuando exista |

---

## 2. Lo que Google Play exige

### 2.1 La cuenta y la prueba cerrada

- **Cuenta de desarrollador** en Play Console: pago único de US$25 y verificación de identidad (documento y dirección).
- **Cuentas personales creadas después del 13-11-2023** deben hacer una **prueba cerrada con al menos 12 probadores que estén inscritos sin interrupción durante los 14 días anteriores** a pedir el acceso a producción ([ayuda de Google](https://support.google.com/googleplay/android-developer/answer/14151465)). Después Google revisa la solicitud: no se aprueba sola.
- En la práctica hay que **calcular unas 3 semanas**: los 14 días, la solicitud y la revisión. Varios desarrolladores cuentan rechazos por "poco uso": no basta con inscribir a 12 personas, tienen que **jugar de verdad** y conviene pedirles comentarios.
- **Cómo juntar 12 probadores reales:** familia y amigos con Android (cada uno con su cuenta de Google), un grupo de WhatsApp para avisar las versiones nuevas y una pregunta por semana ("¿qué te aburrió?", "¿qué no entendiste?"). Hay servicios que venden probadores; **no los recomiendo**: Google los detecta y además no dan opiniones útiles.
- Las cuentas de **organización** no tienen esa prueba, pero necesitan una empresa con número D-U-N-S (gratis, tarda hasta 30 días).

### 2.2 Requisitos técnicos

| Requisito | Qué es | Cómo estamos |
|---|---|---|
| **API objetivo** | Desde el 31-08-2026, las apps nuevas y sus actualizaciones deben apuntar a **Android 16 (API 36)** ([requisitos de Google](https://developer.android.com/google/play/requirements/target-sdk)). Google lo sube cada año, normalmente en agosto | `AndroidTargetSdkVersion: 0` en `ProjectSettings` (= "el más alto instalado"). Hay que fijarlo en 36 y revisar que el SDK de Android de Unity 6.6 lo traiga |
| **API mínima** | La versión de Android más vieja que acepta el juego | 26 (Android 8). Sirve |
| **Formato AAB** | Google Play solo acepta **Android App Bundle (.aab)**, no APK | Hoy `AndroidBuild.cs` arma APK. Hace falta un segundo armado que haga AAB para la tienda (el APK sigue para probar) |
| **Firma con llave fija** | La app se firma con una **llave de subida** que se guarda para siempre; Google firma la versión final (Play App Signing) | Hoy cada APK se firma con una llave distinta (por eso había que desinstalar la vieja). Se crea una llave una vez y va como secreto de GitHub |
| **64 bits** | Obligatorio | Ya: IL2CPP y ARM64 |
| **Páginas de 16 KB** | Apps para Android 15 o más deben funcionar en celulares con páginas de memoria de 16 KB | Unity 6 lo soporta; hay que confirmarlo con el informe de Play Console al subir |
| **Número de versión** | Cada subida necesita un `versionCode` mayor que el anterior | Hoy fijo en 1. Se puede tomar del número de ejecución del workflow |
| **Biblioteca de cobros** | Las compras deben usar una versión reciente de Google Play Billing | Unity IAP la trae por dentro; usar la versión de Unity IAP que pida Google ese año |

### 2.3 Lo que se llena en Play Console

1. **Clasificación de contenido (IARC):** un cuestionario sobre violencia, sexo, drogas, alcohol, apuestas, lenguaje, compras y si los jugadores se comunican entre sí. MistakesLife tiene: pareja e infidelidad insinuada (solo adultos), delitos y cárcel, alcohol y cigarro como decisiones, compras dentro de la app y anuncios. Lo esperable es una clasificación de **mayores de 16 o 18** según el país. Hay que contestar con la verdad: mentir es causa de baja. Si algún día se agrega casino o apuestas simuladas, cambia la clasificación.
2. **Público objetivo:** **18 años o más** (decisión 3). Si el público incluye menores de 13, aplica la política de Familias (anuncios certificados para niños, sin identificador de publicidad, más revisión). No calza con los temas del juego.
3. **Seguridad de los datos (Data safety):** qué datos se juntan, para qué y con quién se comparten. Ver la tabla de la sección 4.3. Tiene que coincidir con la política de privacidad y con lo que hacen las herramientas (AdMob, por ejemplo, junta el identificador de publicidad).
4. **Anuncios:** declarar que la app tiene anuncios.
5. **Acceso a la app:** si algo necesita cuenta, explicar a Google cómo entrar. Con la recomendación 11 (sin cuenta obligatoria) no hace falta.
6. **Borrar la cuenta:** si existe cuenta, Google exige un **enlace web** donde se pueda pedir borrar la cuenta y sus datos, además del botón dentro del juego.
7. **Ficha de la tienda:** nombre (máx. 30 letras), descripción corta (80) y larga (4.000), ícono 512×512, imagen destacada 1024×500, al menos 2 capturas de celular (recomiendo 6 a 8), categoría **Juegos → Simulación**, correo de contacto, enlace a la política de privacidad. Cada idioma tiene su ficha propia.
8. **Perfil de pagos** (para vender): datos del banco y de impuestos. Google se queda con el 15 % del primer millón de dólares al año (y 30 % sobre eso). En Chile, lo que se gana se declara en el SII: conviene preguntarle a un contador antes de la primera venta.

### 2.4 Otros detalles

- **Nombre y marca:** "MistakesLife" se buscó en la web sin coincidencias, pero la marca no está verificada. Antes de pagar la ficha, buscar en INAPI (Chile) y en la base de marcas de la OMPI.
- **Apps parodia** (Instante, Mensajito, Flechazo, MiTubo): no deben usar logos ni colores de las marcas reales. Ya se hacen originales; mantenerlo.
- **Unity Personal** es gratis mientras los ingresos del año sean menores a lo que fije Unity (hoy US$200.000); en Unity 6 la pantalla de "Made with Unity" es opcional.
- **Verificación de desarrolladores de Android (2026):** Google empezó a exigir que todo desarrollador tenga identidad verificada, incluso para apps fuera de la tienda. Con la cuenta de Play Console verificada ya se cumple; ojo con repartir APK por fuera a probadores en algunos países.

---

## 3. Idiomas

### 3.1 Lo que ya existe (y está bien hecho)

- **Ningún texto del jugador está escrito en el código.** Todo usa claves (`ui.inicio.nueva_vida`) y las frases están en `packs/*/lang/<idioma>.json`. En Unity también: las pantallas piden `presenter.Text("ui....")`. Una revisión rápida encontró un solo texto fijo (`"Error"` en `LifeScreen.cs`).
- **Respaldo:** si una clave no existe en inglés, se muestra en español (`ContentDatabase.Text`).
- **Variantes por país:** `es-CL` y `es-AR` solo traen lo que cambia ("pega", "laburo").
- El validador exige que toda clave exista en español.

Por eso la **recomendación 5 es seguir con las tablas del núcleo** y no usar el paquete Localization de Unity: tendríamos dos sistemas de textos y habría que traducir dos veces.

### 3.2 Lo que falta para que agregar un idioma sea fácil

| Falta | Qué es |
|---|---|
| **Elegir idioma** | Al abrir la primera vez, usar el idioma del celular (`Application.systemLanguage`) si existe; si no, español. Después se cambia en Configuración. Se guarda en el celular, no en la partida |
| **Idioma del jugador vs. idioma del país** | Hoy el país decide la variante (`es` en Chile → `es-CL`). Con inglés: un jugador que juega en inglés una vida en Chile ve el juego en inglés, pero los nombres reales (Puente Alto, Fonasa, "PAES") quedan como en Chile, igual que en la vida real. Hay que definir qué claves se traducen y cuáles son nombres propios |
| **Lista de idiomas listos** | Un idioma aparece en Configuración solo si tiene al menos, por ejemplo, el 95 % de las claves. Así uno a medias no se ve roto |
| **Informe de traducción** | Una herramienta (`tools/`) que diga qué claves faltan en cada idioma y cuántas palabras son. Sirve para pedir traducción o presupuesto |
| **Letras** | Fraunces y Figtree cubren español, inglés y portugués. Si algún día llega un idioma con otro alfabeto (ruso, japonés, árabe), hace falta otra letra de respaldo en TextMeshPro |
| **Espacio** | El inglés suele ser más corto que el español; el portugués y el alemán, más largos. Las pantallas deben aguantar textos 30 % más largos sin cortarse (las capturas automáticas lo pueden revisar con un idioma de prueba con textos estirados) |
| **Género y plural** | El español y el portugués cambian según el género ("bienvenido", "bienvenida") y la cantidad. Hay que acordar una forma única en las claves (por ejemplo `clave.f` y `clave.m`, o `{0}` con variantes) antes de traducir |
| **Números, fechas y plata** | El formato sale del **país de la vida** (pesos chilenos con punto de miles) y los nombres de meses del **idioma del jugador** |
| **Ficha de la tienda** | Cada idioma necesita su descripción y sus capturas |

### 3.3 Cuánto trabajo es traducir

Medido el 22-11-2026: unas **44.000 palabras** de texto de la vida (4.313 claves: pantallas, eventos, correos, chats) y unas **130.000 palabras** de preguntas de estudio. Son unas 175.000 palabras por idioma, y crecen con cada PR.

**Elegido (decisión 4b):** traduce Claude, en los mismos PR, porque entiende el contexto: sabe que `{0}` es un nombre que no se toca, que "pega" es trabajo y que "Puente Alto" no se traduce. Las API comunes de traducción suelen romper esas cosas. Va por capas:

1. **Capa 1:** pantallas, menú, Configuración, tienda.
2. **Capa 2:** eventos, correos y chats de la vida.
3. **Capa 3:** preguntas de estudio (las más largas).

Probadores nativos de inglés y de portugués leen y avisan lo que suena raro. La ficha de la tienda la traduce gratis Play Console, y se revisa a mano. Desde P5, todo contenido nuevo llega en los tres idiomas en el mismo PR, para que no se acumulen miles de textos justo antes de lanzar.

**Se descartó:**
- Traducir en vivo con una API (Google, DeepL): necesita internet, el costo crece con cada jugador y el texto saldría distinto cada vez.
- Idiomas como descarga aparte: todos los textos en español pesan menos de 1 MB comprimidos dentro de un juego de 50 a 100 MB. Cuando exista la arquitectura C, los idiomas igual pueden llegar junto con los paquetes.
- Traductores profesionales: unos US$15.000 a 20.000 por idioma; no hay presupuesto.

---

## 4. Privacidad y condiciones de uso

> Esto es una guía para preparar los textos, no asesoría legal. Antes de salir a producción conviene que un abogado los revise, sobre todo si se vende en varios países.

### 4.1 Leyes que aplican

- **Chile:** Ley 21.719 de protección de datos personales (reemplaza a la 19.628 y entra en vigor por completo el 1-12-2026, con una agencia que fiscaliza y multas).
- **Argentina:** Ley 25.326.
- **Brasil** (cuando llegue el portugués): LGPD.
- **Unión Europea** (si se publica ahí): RGPD, y además el consentimiento de anuncios personalizados.
- **Estados Unidos:** COPPA si hubiera niños (por eso la edad 18+), y leyes de algunos estados como California.

### 4.2 La política de privacidad debe decir

1. Quién es el responsable (nombre o empresa y correo de contacto).
2. Qué datos se juntan, para qué y por cuánto tiempo (tabla 4.3).
3. Con quién se comparten: Google (compras, Play Games), la red de anuncios, el backend, la herramienta de errores.
4. Que el juego funciona sin internet; la sesión de Google Play Games entra sola, pero quien no la tenga juega igual.
5. Cómo pedir ver, corregir o borrar los datos, y el botón para hacerlo dentro del juego.
6. Que el juego no es para menores de 18.
7. Que la partida (la vida simulada) es ficticia: no se juntan datos reales de salud, dinero ni personas del jugador, aunque el juego hable de esos temas.
8. Cambios a la política y fecha de la última versión.

### 4.3 Datos que juntaría el juego (para el formulario Data safety)

| Dato | ¿Cuándo? | Para qué | ¿Se comparte? |
|---|---|---|---|
| Partida guardada | Solo si activas el respaldo | Respaldo de la vida | Queda en "Partidas guardadas" de Google Play Games, en la cuenta del propio jugador |
| Id de jugador de Google Play Games | Al abrir el juego, si tiene Play Games | Recordar lo comprado y la prueba gratis | Con Google y el backend (Supabase) |
| Compras (recibo de Google) | Al comprar | Entregar y proteger lo comprado | Con Google |
| Identificador de publicidad | Si hay anuncios y no compraste "Sin anuncios" | Mostrar anuncios | Con la red de anuncios |
| Informes de errores y uso anónimo | Siempre, salvo que se apague | Arreglar errores y equilibrar | Con la herramienta de errores |

### 4.4 Condiciones de uso deben decir

- Licencia para jugar, no venta del juego.
- Lo comprado (sin anuncios, cargas, expansiones) **no tiene valor en dinero**, no se transfiere y los reembolsos siguen las reglas de Google Play.
- Las cargas de Volver al pasado son un objeto del juego que se gasta al usarlo.
- **Aviso de realismo:** el juego usa datos y leyes reales para sentirse verdadero, pero **no es asesoría** financiera, legal, médica ni de salud mental. Si el jugador vive algo difícil de verdad, un enlace a ayuda real (por ejemplo, la línea *4141 en Chile para prevención del suicidio). Ver si se pone en Configuración y en eventos fuertes.
- Las personas, empresas, colegios y apps del juego son ficticias aunque los lugares sean reales; las apps son parodia.
- Prohibido hacer trampa en lo que afecte a otros (tablas, logros públicos, si existen) y revender cuentas.
- Ley aplicable y cómo contactar.

### 4.5 Dónde se publican

**Elegido (decisión 12):** GitHub Pages del mismo repositorio (`docs/legal/` o una rama aparte), con una página por idioma: `privacidad`, `condiciones` y `borrar-cuenta`. Es gratis, tiene historia de cambios y el enlace no cambia. Se enlazan desde Play Console, desde el menú inicial y desde Configuración.

---

## 5. Configuración

Una sola pantalla, limpia, con secciones. Lo que propongo que tenga (los bocetos vienen después, con opciones):

| Sección | Qué tiene |
|---|---|
| **Juego** | Idioma · Estilo de íconos (A línea fina, B color, C objetos; ya acordado) · Confirmar antes de "Un año" · Ver otra vez "Cómo se juega" |
| **Lectura** | Tamaño de letra (normal, grande, muy grande) · Contraste alto. Responde al pedido de que todo se lea fácil |
| **Sonido** | Efectos · Música (si llega) · Vibración (ya existe `Haptics`) |
| **Avisos** | Notificaciones del celular (por ejemplo, "te escribió tu mamá" cuando el juego está cerrado). Apagadas por defecto y pedidas solo cuando tengan sentido; Android 13+ exige pedir permiso |
| **Cuenta y respaldo** | Tu cuenta de Google Play Games (entra sola) · Respaldar mi vida (sí/no) · Respaldar ahora · Última copia (fecha) · Recuperar una copia · Borrar mis datos |
| **Compras** | Lo que tienes · Restaurar compras (obligatorio en la práctica) |
| **Privacidad** | Preferencias de anuncios (el formulario de consentimiento) · Enviar informes de errores sí/no · Política de privacidad · Condiciones |
| **Ayuda** | Contacto (correo de soporte) · Ayuda real si lo necesitas (líneas de ayuda del país) |
| **Acerca de** | Versión · Créditos · Licencias de letras (Figtree, Fraunces: OFL, ya en `Licencias/`) |

Lo que **no** va en Configuración: nada que cambie las reglas de la vida (dificultad, plata inicial, "modo fácil"), porque rompería la ley del proyecto. Si algún día hay modos, se elige al crear la vida y se declara.

Dónde se guarda: en el celular (`PlayerPrefs` o un archivo propio), aparte de la partida. Si el respaldo está activo, viaja junto con la partida.

---

## 6. Menú inicial

### 6.1 Lo que ya existe

`StartMenu.cs`: título, lema, "Continuar: Ana Pérez, 34 años", "Nueva vida" (elige entre 3 nacimientos), "Cómo se juega" y el aviso antes de perder una vida en curso. Es una base, pero no tiene diseño aprobado (`docs/diseno-juego.md` lo dejó "para más adelante").

### 6.2 Qué debería tener

1. **Continuar** (la vida en curso, con nombre, edad, ciudad y la fecha del juego).
2. **Nueva vida**, con dos caminos (decisión 9):
   - **Al azar:** un toque y estás naciendo, sin preguntas, como en BitLife.
   - **Crear tu vida:** nombre, género, país, ciudad, comuna (que es la clase social: Puente Alto, La Pintana, Maipú, Las Condes) y la cara y el cuerpo del personaje que se ven en 3D. Para que siga siendo real, **la familia se arma acorde**: la mamá y el papá se parecen a lo que elegiste, porque la apariencia se hereda. El cuerpo es el punto de partida: después cambia con los hábitos, el deporte y la edad.
3. **Tu historia familiar:** las vidas pasadas y el legado (quién vino antes, qué dejó). Es lo que hace que el juego se sienta como una familia real y no partidas sueltas.
4. **Tienda** (sin anuncios, Volver al pasado, expansiones).
5. **Configuración.**
6. **Cómo se juega.**

### 6.3 La primera vez que se abre

1. Idioma (si el del celular no existe).
2. **Confirmar la edad** ("Este juego es para mayores de 18") y aceptar condiciones con enlaces.
3. Formulario de consentimiento de anuncios, solo donde la ley lo pide (Europa, Reino Unido, algunos estados de EE.UU.). AdMob lo trae listo (UMP).
4. Directo a "Nueva vida". El respaldo se ofrece después, no al inicio.

### 6.4 Opciones de estilo para bocetear (a elegir)

- **A. La maqueta viva:** la escena 3D del barrio o la casa de la vida en curso detrás, con el menú encima en papel limpio. Si no hay vida, el barrio vacío de día. *Recomendada:* continúa la idea aprobada de "tu vida es una maqueta viva".
- **B. El álbum familiar:** el menú como la tapa de un álbum de fotos con la familia; "Continuar" es la última foto.
- **C. Pantalla limpia tipo BitLife:** fondo de color, botones grandes, nada más. La más rápida y clara, pero con menos personalidad.

---

## 7. Monetización: anuncios y compras

### 7.1 La regla de la monetización

1. **Nunca vender resultados de la vida:** ni plata, ni notas, ni salud, ni suerte, ni "pasar de curso". Todo eso tiene que venir de decisiones del jugador (REGLA DE LEY).
2. **Nunca un anuncio en medio de un momento.** El tiempo detenido, una decisión o un mensaje en vivo son sagrados.
3. **Todo se puede jugar gratis:** la vida completa de Chile y Argentina sin pagar.
4. **Lo que se compra se ve:** el reloj de arena de Volver al pasado, el país nuevo en el mapa.

### 7.2 Anuncios: con qué

| Opción | Lo bueno | Lo malo |
|---|---|---|
| **A. Google AdMob** | La red más grande; paga bien en Latinoamérica; trae el formulario de consentimiento (UMP) y plugin para Unity; se integra bien con Play Console | Junta datos que hay que declarar |
| **B. Unity LevelPlay (Unity Ads)** | Hecha por Unity; mediación incluida; buena en juegos | Unity ha cambiado su producto de anuncios varias veces; más piezas que configurar |
| **C. Mediación (AdMob o LevelPlay con varias redes)** | Más ingresos con mucho volumen | Más complejo; no vale la pena con pocos jugadores |

**Elegido (decisión 6): AdMob** al lanzar. Con más de unos 10.000 jugadores al día, probar mediación.

### 7.3 Anuncios: dónde (decisión 7)

**Con premio, opcionales, dentro de los momentos.** Como en BitLife, en una decisión o un momento importante aparece una opción que se consigue viendo un anuncio. Para que no rompa la ley, el anuncio **compra información o una segunda oportunidad, nunca un resultado**:

- **Pedir consejo:** alguien real de tu vida que sabe del tema (tu mamá, tu amiga contadora, tu profe) te dice qué haría y por qué. El resultado no cambia, solo sabes más.
- **Leer la letra chica:** antes de firmar un crédito, un contrato o un arriendo, ves lo que dice el papel de verdad (la tasa, la multa, lo que pasa si no pagas).
- **Recordar:** en una prueba o una entrevista, repasar lo que estudiaste. Solo ayuda si de verdad estudiaste.
- **La prueba gratis de Segundo aliento y Volver al pasado**, una sola vez en todo el juego (sección 7.5).

**Nunca:** "mira un anuncio y recibe plata, sánate o pasa de curso".

**De pantalla completa, por tiempo y no por año:** como máximo **uno cada 20 minutos de juego**, y solo en una pausa natural (después de un resultado o al cerrar el año), nunca en medio de un momento. Nada de estos anuncios en la primera media hora de un jugador nuevo. Con jugadores reales, AdMob deja medir si la gente se va por los anuncios, y se ajusta a 25 o 30 minutos.

**Sin banner:** ensucia la pantalla principal aprobada y paga poco.

**"Sin anuncios"** quita los de pantalla completa. Los con premio siguen, porque son elección del jugador.

### 7.4 Compras: con qué

**Unity IAP** (`com.unity.purchasing`, ya está en `Packages/manifest.json` sin usar). Por dentro usa Google Play Billing, y sirve también si algún día sale en iPhone. Alternativa: el plugin oficial de Google Play Billing, solo Android. **Recomendación: Unity IAP.**

### 7.5 Compras: qué se vende

| Producto | Tipo | Idea de precio | Nota |
|---|---|---|---|
| **Sin anuncios** | Único (para siempre) | US$2,99 a 3,99 | Se restaura en otro celular |
| **Volver al pasado** | Único: aparece el reloj de arena y da cargas por generación | US$2,99 a 4,99 | El núcleo ya tiene cargas (`TimeMachine.Charges`, `AddCharges`) y las reglas que limitan (una vez por etapa por generación) |
| **Segundo aliento** | Consumible: sobrevivir a una muerte | US$0,99 a 1,99 | Ver abajo |
| **Cargas extra** | Consumible (paquetes de 3, 10) | US$0,99 a 2,99 | Lo que se compra se gasta |
| **Expansión de país** (México, España…) | Único, se descarga | US$1,99 a 3,99 | Llega como paquete descargable (arquitectura C) |
| **Expansión de ciudad** | Único | US$0,99 a 1,99 | Otra ciudad del mismo país |
| **Paquete de carreras** (salud: medicina, enfermería, kinesiología…) | Único | US$1,99 | |
| **Estilos de íconos extra** | Único, cosmético | US$0,99 | Idea abierta de `diseno-juego.md` |
| **Pack de inicio** (sin anuncios + Volver al pasado + un Segundo aliento) | Único, con descuento | US$4,99 a 6,99 | Opcional |

Google pone precios locales por país (pesos chilenos, argentinos). Los precios son ideas para conversar, no estudio de mercado.

**Volver al pasado y Segundo aliento (decisión 8):**

- **Las dos son compras.** Quien compra Volver al pasado tiene el reloj de arena siempre a mano y lo usa en cualquier momento, gastando cargas.
- **Prueba gratis, una sola vez en todo el juego, en la primera muerte del jugador:** ve un anuncio y usa Segundo aliento, o ve otro y usa Volver al pasado. Después de esa prueba, las dos solo se compran. La prueba se anota en la cuenta de Google Play Games para que no se repita al reinstalar (quien no tenga cuenta puede repetirla reinstalando; es un riesgo chico y aceptado).
- **No hay cargas por anuncio** fuera de esa prueba.
- La regla que ya existe se mantiene para todos: cada etapa de la vida se vuelve a vivir una sola vez por generación.

**Volver al pasado en pantalla:** un **ícono de reloj de arena**, sin texto en el botón (reemplaza al "círculo" de `docs/diseno-juego.md`). Al tocarlo se abre un cuadro titulado "Volver al pasado" con los momentos a los que se puede volver ("Al empezar la juventud, 18 años", "Antes de firmar el crédito") y las cargas que quedan.

**Segundo aliento, real y no mágico:** revivir de la nada sería inventado, pero en la vida real hay gente que muere y la reaniman. Por eso **te salvan en el último segundo según cómo moriste**: en un infarto, la reanimación en la ambulancia; en un accidente, la cirugía de urgencia; en una sobredosis, el lavado en la urgencia. Despiertas con **secuelas reales**: días en la UCI, licencia médica, salud baja, la cuenta del hospital según tu previsión, y tu gente que se asustó. Se ve en **3D**: la ambulancia o la camilla, el desfibrilador, la pantalla del pulso que vuelve y el despertar en la UCI. Si mueres de viejo (a los 95, por ejemplo) no hay reanimación posible: ahí solo sirve Volver al pasado. Qué muertes admiten Segundo aliento y con qué secuelas se decide con datos médicos reales al programarlo.

Los dos necesitan bocetos aprobados antes de programarse.

La suscripción mensual se descartó por ahora: en un juego hecho por una persona agrega soporte y reclamos.

### 7.6 Cómo llegan las expansiones

Con la arquitectura C, un país nuevo es un paquete (JSON + casas 3D). Opciones de entrega:
- **Play Asset Delivery** (paquetes de recursos dentro de Google Play): no necesita servidor propio, pero cada contenido nuevo es una actualización de la app.
- **Descarga desde el backend** (Supabase Storage o similar) después de comprobar la compra: permite sumar países sin actualizar la app.
- *Recomendación:* el backend, porque es lo que pide la arquitectura C. El paquete descargado se guarda en el celular y se juega sin internet.

### 7.7 Tienda dentro del juego

Una pantalla de la tienda desde el menú inicial y desde Configuración, con lo que tienes marcado. Ningún aviso de "¡compra ya!" en medio de la vida. La única invitación dentro de la vida: frente al "error que vuelve", si no tienes Volver al pasado, el reloj de arena aparece apagado con "Disponible en la tienda" (una vez, sin insistir).

---

## 8. Cuenta, respaldo y backend

### 8.1 Lo que pide el proyecto

- 10.000 a más de 30.000 jugadores, y el legado de varias generaciones protegido aunque se pierda el celular.
- El juego completo sin internet (regla 9): el respaldo es una ayuda, no un requisito.
- Gastar lo menos posible: el juego todavía no gana plata.
- La arquitectura C necesita una base de datos con página web de formularios que genera paquetes.

### 8.2 Lo elegido (decisiones 10 y 11)

Se miró cómo lo hace BitLife: las vidas quedan solo en el celular, y las compras y los logros van con la cuenta de la tienda. En MistakesLife perder el celular dolería más, porque el legado y la historia familiar son el corazón del juego. Por eso:

| Qué | Dónde | Costo |
|---|---|---|
| **La vida (respaldo)** | **"Partidas guardadas" de Google Play Games**, en la cuenta de Google del propio jugador | $0, aunque haya 100.000 jugadores |
| **La sesión** | **Inicio automático de Google Play Games**: al abrir el juego entra solo, sin pantalla ni contraseña. Quien no tenga Play Games juega igual, sin respaldo | $0 |
| **Lo comprado** | Google Play ("Restaurar compras") y el registro en Supabase, ligado al id de Play Games | $0 en el plan gratis |
| **Revisar que una compra sea real** | Una función de Supabase que le pregunta a Google | $0 en el plan gratis |
| **Logros** | Google Play Games | $0 |
| **Contenido (arquitectura C)** | Base de datos de Supabase | $0 en el plan gratis, unos US$25 al mes si crece mucho |

**Se descartó guardar las vidas en un servidor propio:** costaría unos US$25 al mes con miles de jugadores, y Google Play Games lo hace gratis. Firebase y Unity Gaming Services se descartaron porque no sirven para la arquitectura C.

**¿Cabe el legado en Google Play Games?** Cada partida guardada tiene un límite de unos 3 MB. Medido el 22-11-2026 con el núcleo actual, jugando familias completas en automático, la partida comprimida (con sus fotos de Volver al pasado) pesa:

| Momento | Peso |
|---|---|
| Primera vida, al morir a los 75-80 años | unos 230 KB |
| Segunda generación | 250 a 350 KB |
| Tercera generación | 370 a 440 KB |

Cada generación suma unos 100 KB: el límite llegaría cerca de la **generación 25 o 30**. Para familias tan largas, las vidas muy antiguas se guardan **resumidas** (quién fue, qué dejó, su capítulo, sus semillas que siguen vivas), como un árbol genealógico. Hay que volver a medir cada vez que el guardado crezca (una prueba puede avisar si una vida de tres generaciones pasa de 1 MB).

**Sobre la edad:** ninguna app puede garantizar que el jugador sea adulto. Se usa la clasificación 18+ de la tienda, la pregunta de edad al abrir por primera vez y los controles de Google para que los papás bloqueen compras. Para comprar, el celular ya necesita una cuenta de Google con Play Store: eso lo maneja Google.

### 8.3 Cómo funciona el respaldo

1. La partida se guarda en el celular, siempre (como hoy, `SaveStore`).
2. Si el respaldo está activo, se sube **comprimida** (`LifeSession.SaveToBytes()`) a Partidas guardadas de Google Play Games al cerrar el año, al salir del juego y con "Respaldar ahora".
3. Cada copia lleva la versión del guardado (`SaveFormatVersion`), la fecha real, la fecha del juego y el nombre y la edad del personaje, para mostrarla antes de recuperarla.
4. **Conflicto** (jugaste en dos celulares): se muestran las dos ("Ana, 34 años, este celular" y "Ana, 36 años, en tu cuenta") y el jugador elige. Nunca se mezclan.
5. Una partida de una versión nueva del juego en un celular con versión vieja da el error claro que ya existe ("actualiza el juego").

---

## 9. Seguridad: trampas, compras y datos

### 9.1 Qué proteger y qué no

MistakesLife es para un jugador, sin internet. Si alguien edita su propia partida para tener más plata, **solo se hace trampa a sí mismo**: no vale la pena gastar mucho en eso. Lo que sí hay que proteger:

1. **Las compras** (es plata real).
2. **El respaldo** (que nadie lea o borre la vida de otro; lo cuida Google, porque queda en la cuenta de cada jugador).
3. **Lo que algún día sea público** (logros, tablas, legado compartido).

### 9.2 Compras

- **Verificar cada compra en el servidor:** el celular manda el recibo de Google al backend; el backend pregunta a Google (Google Play Developer API) si es real, lo anota y recién ahí se entrega. Así una compra falsa no sirve.
- **Lo comprado vive en la cuenta** (Google Play y el registro en Supabase ligado al id de Play Games), no solo en el celular: "Restaurar compras" lo devuelve en un celular nuevo.
- **Reembolsos:** Google avisa (notificaciones en tiempo real) cuando alguien pide su plata de vuelta; el backend quita lo comprado.
- **Sin internet:** lo comprado se guarda en el celular firmado por el servidor, así funciona sin conexión pero no se puede inventar editando un archivo.
- **Cargas de Volver al pasado:** cada carga comprada se anota en el servidor; la partida no puede tener más cargas que las compradas más las que da el juego.

### 9.3 La app

- **Play Integrity API:** pregunta a Google si la app es la original de la tienda y corre en un celular real. Se usa al comprar y al usar la prueba gratis, no para jugar.
- **IL2CPP** (ya está): hace más difícil leer el código del juego.
- **Partida con firma:** la partida guardada lleva una firma; si alguien la edita, el juego lo nota. Recomendación: no castigar (es su partida), pero **no subirla a lo público** (logros, tablas).
- Nada de llaves secretas dentro de la app: lo secreto vive solo en el backend.

### 9.4 El backend y el repositorio

- Cada jugador solo puede leer **sus** compras en Supabase (reglas por fila, "RLS"). Las partidas no pasan por nuestro servidor: quedan en la cuenta de Google de cada jugador.
- Límites de uso por jugador (que nadie suba 1.000 partidas por minuto).
- Copias de seguridad diarias de la base de datos.
- Alerta de costo en el backend, para no tener sorpresas.
- La llave de firma de Android y las claves del backend **solo como secretos de GitHub**, nunca en el repo. Guardar una copia de la llave de subida en un lugar seguro: si se pierde, hay que pedírsela a Google.
- Informes de errores (Crashlytics o Unity Cloud Diagnostics) para saber cuándo algo se rompe en el celular de alguien.

---

## 10. Orden de trabajo (serie P)

Cada paso es un PR, después de conversar y (si tiene pantalla) aprobar bocetos.

| # | Tarea | Necesita antes |
|---|---|---|
| P0 | Este documento, conversado y con las 12 decisiones tomadas | — |
| P1 | Armado para la tienda: llave de firma fija como secreto, AAB además del APK, API objetivo 36, `versionCode` automático | Nombre del estudio y del juego (decisión 2) para el id definitivo; lo demás se puede preparar antes |
| P2 | Textos legales en GitHub Pages: privacidad, condiciones, borrar cuenta (español primero) | Decisiones 3, 10, 12 |
| P3 | Configuración (bocetos y luego pantalla): idioma, letra, sonido, privacidad, acerca de | Bocetos aprobados |
| P4 | Menú inicial nuevo (Al azar y Crear tu vida) y primera vez (edad, condiciones) | Bocetos aprobados |
| P5 | Idiomas listos para crecer: elegir idioma, informe de claves que faltan, forma de género y plural, idioma de prueba estirado en las capturas | Decisión 5 |
| P6 | Prueba cerrada en Play Console con 12 probadores por 14 días (lo hace Isaias, con una guía paso a paso) | P1, P2 |
| P7 | Inicio automático de Google Play Games, respaldo en Partidas guardadas y Supabase para lo comprado | — |
| P8 | Compras con Unity IAP y verificación en el servidor; tienda; reloj de arena y Segundo aliento (con su escena 3D) | P7, bocetos de la tienda, del reloj de arena y de Segundo aliento |
| P9 | Anuncios con AdMob y su consentimiento: con premio en los momentos y de pantalla completa cada 20 minutos | Bocetos de las opciones con anuncio |
| P10 | Expansiones descargables | D11 (arquitectura C), P8 |
| P11 | Inglés y portugués, por capas (pantallas, vida, preguntas), con probadores nativos; ficha de la tienda en los tres idiomas | P5 |

La prueba cerrada (P6) se puede hacer **sin compras ni anuncios**: conviene empezarla apenas estén P1 y P2, para que los 14 días corran mientras se programa lo demás.
