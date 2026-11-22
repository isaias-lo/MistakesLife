# Publicación: lo que necesita el juego para salir en Google Play (borrador para conversar)

Este documento junta **la otra parte del juego**: lo que no es la vida simulada pero hace falta para que Google Play acepte MistakesLife, para que se pueda jugar en varios idiomas, para cobrar sin romper la ley del proyecto y para que las partidas y las compras estén seguras.

**Estado: borrador para conversar.** Nada de esto está decidido. Donde hay que elegir, hay opciones con una recomendación, y la última palabra es de Isaias. No trae código: cuando una parte quede acordada, se programa en su propio PR (serie P del roadmap). `docs/diseno-juego.md` sigue mandando sobre cómo se juega y cómo se ve; las pantallas nuevas (menú inicial, Configuración, tienda) necesitan bocetos aprobados antes de programarse.

**La ley del proyecto también manda aquí:** el jugador siempre debe sentir que todo es real. Por eso la monetización tiene una regla propia: **nunca se vende algo que la vida no podría dar** (plata, notas, salud, sueldo, suerte). Se vende comodidad (sin anuncios), una mecánica declarada (Volver al pasado) y mundos nuevos (países, ciudades, carreras), nunca resultados.

Los datos de Google Play se revisaron el 22-11-2026. Google cambia sus reglas cada año: antes de cada paso hay que volver a mirar la página oficial que se cita.

---

## Índice

1. [Las decisiones que Isaias tiene que tomar](#1-las-decisiones-que-isaias-tiene-que-tomar)
2. [Lo que Google Play exige](#2-lo-que-google-play-exige)
3. [Idiomas](#3-idiomas)
4. [Privacidad y condiciones de uso](#4-privacidad-y-condiciones-de-uso)
5. [Configuración](#5-configuración)
6. [Menú inicial](#6-menú-inicial)
7. [Monetización: anuncios y compras](#7-monetización-anuncios-y-compras)
8. [Cuenta, guardado en la nube y backend](#8-cuenta-guardado-en-la-nube-y-backend)
9. [Seguridad: trampas, compras y datos](#9-seguridad-trampas-compras-y-datos)
10. [Orden propuesto (serie P)](#10-orden-propuesto-serie-p)

---

## 1. Las decisiones que Isaias tiene que tomar

Lista corta para conversar de a una. Cada una está explicada en su sección.

| # | Pregunta | Opciones | Recomendación |
|---|---|---|---|
| 1 | ¿Cuenta de desarrollador personal o de empresa? | A personal (US$25, exige la prueba de 12 probadores por 14 días) · B organización (necesita empresa y número D-U-N-S, no exige esa prueba) | **A personal**, salvo que ya tengas empresa: es lo más rápido para empezar |
| 2 | ¿Nombre del paquete (id de la app)? | `com.mistakeslife.game` (el de hoy) · otro con tu dominio | Decidirlo **antes** de subir la primera versión: no se puede cambiar nunca |
| 3 | ¿Para qué edad es el juego? | A solo adultos (18+) · B 16+ · C todas las edades | **A 18+**: hay pareja, infidelidad, delitos, alcohol y cárcel; evita las reglas de juegos para niños |
| 4 | ¿Tercer idioma? | portugués de Brasil · francés · otro | **Portugués de Brasil** (ya está en CLAUDE.md; Brasil es un mercado enorme de juegos móviles) |
| 5 | ¿Cómo se manejan los textos en Unity? | A seguir con las tablas del núcleo (`lang/`) · B paquete Localization de Unity | **A**: ya funciona y todo el juego lo usa |
| 6 | ¿Herramienta de anuncios? | A AdMob · B Unity LevelPlay (Unity Ads) · C las dos con mediación | **A AdMob** al inicio; mediación después si hay volumen |
| 7 | ¿Qué anuncios? | intersticial (pantalla completa) · con premio (el jugador elige verlo) · banner | **Solo intersticial al cerrar el año y con premio opcional**, sin banner |
| 8 | ¿Cómo se vende Volver al pasado? | A desbloqueo único · B paquetes de cargas · C suscripción | **A + B**: se desbloquea una vez y se recargan cargas |
| 9 | ¿Argentina gratis o de pago? | gratis al lanzar · expansión pagada | **Gratis al lanzar**: el juego nace con dos países; los siguientes son expansiones |
| 10 | ¿Backend para cuenta y nube? | A Supabase · B Firebase · C Unity Gaming Services · D Play Games (solo guardado) | **A Supabase**: sirve también para la base de datos de la arquitectura C |
| 11 | ¿Cuenta obligatoria? | A nunca (juego sin cuenta, nube opcional) · B obligatoria | **A**: la regla 9 dice "primero local"; la cuenta solo para la nube |
| 12 | ¿Dónde vive la política de privacidad? | GitHub Pages del repo · página propia | **GitHub Pages**: gratis y versionada junto al código |

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
2. **Público objetivo:** recomiendo declarar **18 años o más**. Si el público incluye menores de 13, aplica la política de Familias (anuncios certificados para niños, sin identificador de publicidad, más revisión). No calza con los temas del juego.
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

El juego tiene muchísimo texto (eventos, correos, chats, más de 4.000 preguntas de estudio). Traducir todo de una es caro. Propuesta por capas:

1. **Capa 1:** pantallas, menú, Configuración, tienda, ficha de la tienda.
2. **Capa 2:** eventos, correos y chats de la vida.
3. **Capa 3:** preguntas de estudio (las más largas).

El contenido de un país no se traduce al idioma de otro país solo porque sí: si el inglés llega con un país de habla inglesa, ese país trae su contenido escrito en inglés desde el inicio. Pregunta para conversar: ¿el inglés llega primero para jugar Chile en inglés, o junto con un país de habla inglesa?

Traducción automática más revisión humana es lo más barato; la REGLA DE LEY pide que suene natural en cada idioma, así que al menos la capa 1 y 2 deberían revisarla nativos.

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
4. Que el juego funciona sin cuenta y sin internet; la nube es opcional.
5. Cómo pedir ver, corregir o borrar los datos, y el botón para hacerlo dentro del juego.
6. Que el juego no es para menores de 18.
7. Que la partida (la vida simulada) es ficticia: no se juntan datos reales de salud, dinero ni personas del jugador, aunque el juego hable de esos temas.
8. Cambios a la política y fecha de la última versión.

### 4.3 Datos que juntaría el juego (para el formulario Data safety)

| Dato | ¿Cuándo? | Para qué | ¿Se comparte? |
|---|---|---|---|
| Partida guardada | Solo si activas la nube | Guardado en la nube | No (solo el backend) |
| Id de cuenta (Google Play Games o correo) | Solo si activas la nube | Iniciar sesión | No |
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

**Recomendación 12:** GitHub Pages del mismo repositorio (`docs/legal/` o una rama aparte), con una página por idioma: `privacidad`, `condiciones` y `borrar-cuenta`. Es gratis, tiene historia de cambios y el enlace no cambia. Se enlazan desde Play Console, desde el menú inicial y desde Configuración.

---

## 5. Configuración

Una sola pantalla, limpia, con secciones. Lo que propongo que tenga (los bocetos vienen después, con opciones):

| Sección | Qué tiene |
|---|---|
| **Juego** | Idioma · Estilo de íconos (A línea fina, B color, C objetos; ya acordado) · Confirmar antes de "Un año" · Ver otra vez "Cómo se juega" |
| **Lectura** | Tamaño de letra (normal, grande, muy grande) · Contraste alto. Responde al pedido de que todo se lea fácil |
| **Sonido** | Efectos · Música (si llega) · Vibración (ya existe `Haptics`) |
| **Avisos** | Notificaciones del celular (por ejemplo, "te escribió tu mamá" cuando el juego está cerrado). Apagadas por defecto y pedidas solo cuando tengan sentido; Android 13+ exige pedir permiso |
| **Cuenta y nube** | Iniciar sesión · Respaldar ahora · Última copia en la nube (fecha) · Recuperar de la nube · Cerrar sesión · Borrar mi cuenta |
| **Compras** | Lo que tienes · Restaurar compras (obligatorio en la práctica) |
| **Privacidad** | Preferencias de anuncios (el formulario de consentimiento) · Enviar informes de errores sí/no · Política de privacidad · Condiciones |
| **Ayuda** | Contacto (correo de soporte) · Ayuda real si lo necesitas (líneas de ayuda del país) |
| **Acerca de** | Versión · Créditos · Licencias de letras (Figtree, Fraunces: OFL, ya en `Licencias/`) |

Lo que **no** va en Configuración: nada que cambie las reglas de la vida (dificultad, plata inicial, "modo fácil"), porque rompería la ley del proyecto. Si algún día hay modos, se elige al crear la vida y se declara.

Dónde se guarda: en el celular (`PlayerPrefs` o un archivo propio), aparte de la partida. Si hay nube, también se respalda.

---

## 6. Menú inicial

### 6.1 Lo que ya existe

`StartMenu.cs`: título, lema, "Continuar: Ana Pérez, 34 años", "Nueva vida" (elige entre 3 nacimientos), "Cómo se juega" y el aviso antes de perder una vida en curso. Es una base, pero no tiene diseño aprobado (`docs/diseno-juego.md` lo dejó "para más adelante").

### 6.2 Qué debería tener

1. **Continuar** (la vida en curso, con nombre, edad, ciudad y la fecha del juego).
2. **Nueva vida:** al azar (como hoy) o eligiendo país y ciudad entre los que tienes.
3. **Tu historia familiar:** las vidas pasadas y el legado (quién vino antes, qué dejó). Es lo que hace que el juego se sienta como una familia real y no partidas sueltas.
4. **Tienda** (sin anuncios, Volver al pasado, expansiones).
5. **Configuración.**
6. **Cómo se juega.**

### 6.3 La primera vez que se abre

1. Idioma (si el del celular no existe).
2. **Confirmar la edad** ("Este juego es para mayores de 18") y aceptar condiciones con enlaces.
3. Formulario de consentimiento de anuncios, solo donde la ley lo pide (Europa, Reino Unido, algunos estados de EE.UU.). AdMob lo trae listo (UMP).
4. Directo a "Nueva vida". La nube se ofrece después, no al inicio.

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
4. **Lo que se compra se ve:** el círculo de Volver al pasado, el país nuevo en el mapa.

### 7.2 Anuncios: con qué

| Opción | Lo bueno | Lo malo |
|---|---|---|
| **A. Google AdMob** | La red más grande; paga bien en Latinoamérica; trae el formulario de consentimiento (UMP) y plugin para Unity; se integra bien con Play Console | Junta datos que hay que declarar |
| **B. Unity LevelPlay (Unity Ads)** | Hecha por Unity; mediación incluida; buena en juegos | Unity ha cambiado su producto de anuncios varias veces; más piezas que configurar |
| **C. Mediación (AdMob o LevelPlay con varias redes)** | Más ingresos con mucho volumen | Más complejo; no vale la pena con pocos jugadores |

**Recomendación: A AdMob** al lanzar. Con más de unos 10.000 jugadores al día, probar mediación.

### 7.3 Anuncios: dónde

- **Intersticial (pantalla completa) solo al cerrar el año**, justo después del capítulo del año, como pasar la página de un libro. Como máximo uno cada X minutos de juego (por ejemplo 4) y nunca en los primeros 15 minutos del jugador nuevo.
- **Con premio, siempre opcional:** el jugador decide verlo. Ideas que no rompen la ley: una carga de Volver al pasado gratis una vez por semana real; ver los capítulos de años pasados en el álbum. **Nunca** "ver un anuncio para recibir plata o sanarte".
- **Sin banner:** ensucia la pantalla aprobada y paga poco.
- "Sin anuncios" quita los intersticiales; los con premio siguen disponibles porque son elección del jugador.

### 7.4 Compras: con qué

**Unity IAP** (`com.unity.purchasing`, ya está en `Packages/manifest.json` sin usar). Por dentro usa Google Play Billing, y sirve también si algún día sale en iPhone. Alternativa: el plugin oficial de Google Play Billing, solo Android. **Recomendación: Unity IAP.**

### 7.5 Compras: qué se vende

| Producto | Tipo | Idea de precio | Nota |
|---|---|---|---|
| **Sin anuncios** | Único (para siempre) | US$2,99 a 3,99 | Se restaura en otro celular |
| **Volver al pasado** | Único: desbloquea el círculo y da cargas por generación | US$2,99 a 4,99 | El núcleo ya tiene cargas (`TimeMachine.Charges`, `AddCharges`) y las reglas que limitan (una vez por etapa por generación) |
| **Cargas extra** | Consumible (paquetes de 3, 10) | US$0,99 a 2,99 | Lo que se compra se gasta |
| **Expansión de país** (México, España…) | Único, se descarga | US$1,99 a 3,99 | Llega como paquete descargable (arquitectura C) |
| **Expansión de ciudad** | Único | US$0,99 a 1,99 | Otra ciudad del mismo país |
| **Paquete de carreras** (salud: medicina, enfermería, kinesiología…) | Único | US$1,99 | |
| **Estilos de íconos extra** | Único, cosmético | US$0,99 | Idea abierta de `diseno-juego.md` |
| **Pack de inicio** (sin anuncios + Volver al pasado) | Único, con descuento | US$4,99 a 6,99 | Opcional |

Google pone precios locales por país (pesos chilenos, argentinos). Los precios son ideas para conversar, no estudio de mercado.

**Opciones para Volver al pasado (decisión 8):**
- **A. Desbloqueo único** con N cargas por generación.
- **B. Solo paquetes de cargas.**
- **C. Suscripción mensual** ("MistakesLife+": sin anuncios y cargas cada mes).
- *Recomendación: A + B.* La suscripción en un juego de una persona agrega soporte y reclamos; se puede pensar más adelante.

### 7.6 Cómo llegan las expansiones

Con la arquitectura C, un país nuevo es un paquete (JSON + casas 3D). Opciones de entrega:
- **Play Asset Delivery** (paquetes de recursos dentro de Google Play): no necesita servidor propio, pero cada contenido nuevo es una actualización de la app.
- **Descarga desde el backend** (Supabase Storage o similar) después de comprobar la compra: permite sumar países sin actualizar la app.
- *Recomendación:* el backend, porque es lo que pide la arquitectura C. El paquete descargado se guarda en el celular y se juega sin internet.

### 7.7 Tienda dentro del juego

Una pantalla de la tienda desde el menú inicial y desde Configuración, con lo que tienes marcado. Ningún aviso de "¡compra ya!" en medio de la vida. La única invitación dentro de la vida: frente al "error que vuelve", si no tienes Volver al pasado, el círculo aparece apagado con "Disponible en la tienda" (una vez, sin insistir).

---

## 8. Cuenta, guardado en la nube y backend

### 8.1 Lo que pide el proyecto

- 10.000 a más de 30.000 jugadores con guardado en la nube.
- El juego completo sin internet (regla 9): la nube es un respaldo, no un requisito.
- La arquitectura C necesita una base de datos con página web de formularios que genera paquetes.

### 8.2 Opciones de backend (decisión 10)

| Opción | Qué es | Lo bueno | Lo malo |
|---|---|---|---|
| **A. Supabase** | Base de datos Postgres con cuentas, archivos y funciones | **El mismo servicio sirve para la arquitectura C** (contenido + página de formularios) y para cuentas, partidas y compras; base de datos estándar (se puede mudar); hay conector en este proyecto | No tiene SDK oficial de Unity (se usa su API web, que es simple) |
| **B. Firebase (Google)** | Cuentas, Firestore, Storage, Functions, Crashlytics | Muy probado en móviles; SDK de Unity; Crashlytics para errores | La base de datos no es tabla (peor para la arquitectura C); costos por lectura difíciles de prever |
| **C. Unity Gaming Services** | Cloud Save, Authentication, Economy, Remote Config | Todo dentro de Unity; sencillo para guardar | No sirve para la arquitectura C; atado a Unity |
| **D. Google Play Games, Partidas guardadas** | Guardado gratis en la cuenta de Google | Cero servidor y gratis | Solo Android; no protege compras; no sirve para la arquitectura C |

**Recomendación: A Supabase** para todo (contenido, cuentas, partidas, compras), con el inicio de sesión de **Google Play Games** como forma de entrar, para que el jugador no tenga que escribir correo ni contraseña.

**Cuánto podría costar (a confirmar en su página):** Supabase gratis aguanta las pruebas. Para 30.000 jugadores, el plan Pro (unos US$25 al mes) incluye 100.000 usuarios al mes y 100 GB de archivos. Una partida comprimida pesa del orden de cientos de KB (se mide antes de decidir); 30.000 partidas de 500 KB son unos 15 GB.

### 8.3 Cómo funciona la nube

1. La partida se guarda en el celular, siempre (como hoy, `SaveStore`).
2. Si la nube está activa, se sube **comprimida** (`LifeSession.SaveToBytes()`) al cerrar el año, al salir del juego y con "Respaldar ahora". No cada semana: así el costo es bajo.
3. Cada subida guarda: versión del guardado (`SaveFormatVersion`), fecha real, fecha del juego, nombre y edad del personaje. Se guardan las **3 últimas copias** por si una sale mala.
4. **Conflicto** (jugaste en dos celulares): se muestran las dos ("Ana, 34 años, este celular" y "Ana, 36 años, en la nube") y el jugador elige. Nunca se mezclan.
5. Una partida de una versión nueva del juego en un celular con versión vieja da el error claro que ya existe ("actualiza el juego").

---

## 9. Seguridad: trampas, compras y datos

### 9.1 Qué proteger y qué no

MistakesLife es para un jugador, sin internet. Si alguien edita su propia partida para tener más plata, **solo se hace trampa a sí mismo**: no vale la pena gastar mucho en eso. Lo que sí hay que proteger:

1. **Las compras** (es plata real).
2. **La nube** (que nadie lea o borre la partida de otro).
3. **Lo que algún día sea público** (logros, tablas, legado compartido).

### 9.2 Compras

- **Verificar cada compra en el servidor:** el celular manda el recibo de Google al backend; el backend pregunta a Google (Google Play Developer API) si es real, lo anota y recién ahí se entrega. Así una compra falsa no sirve.
- **Lo comprado vive en la cuenta**, no solo en el celular: "Restaurar compras" lo devuelve en un celular nuevo.
- **Reembolsos:** Google avisa (notificaciones en tiempo real) cuando alguien pide su plata de vuelta; el backend quita lo comprado.
- **Sin internet:** lo comprado se guarda en el celular firmado por el servidor, así funciona sin conexión pero no se puede inventar editando un archivo.
- **Cargas de Volver al pasado:** cada carga comprada se anota en el servidor; la partida no puede tener más cargas que las compradas más las que da el juego.

### 9.3 La app

- **Play Integrity API:** pregunta a Google si la app es la original de la tienda y corre en un celular real. Se usa al comprar y al subir a la nube, no para jugar.
- **IL2CPP** (ya está): hace más difícil leer el código del juego.
- **Partida con firma:** la partida guardada lleva una firma; si alguien la edita, el juego lo nota. Recomendación: no castigar (es su partida), pero **no subirla a lo público** (logros, tablas).
- Nada de llaves secretas dentro de la app: lo secreto vive solo en el backend.

### 9.4 El backend y el repositorio

- Cada jugador solo puede leer y escribir **su** partida (en Supabase, reglas por fila, "RLS").
- Límites de uso por jugador (que nadie suba 1.000 partidas por minuto).
- Copias de seguridad diarias de la base de datos.
- Alerta de costo en el backend, para no tener sorpresas.
- La llave de firma de Android y las claves del backend **solo como secretos de GitHub**, nunca en el repo. Guardar una copia de la llave de subida en un lugar seguro: si se pierde, hay que pedírsela a Google.
- Informes de errores (Crashlytics o Unity Cloud Diagnostics) para saber cuándo algo se rompe en el celular de alguien.

---

## 10. Orden propuesto (serie P)

Cada paso es un PR, después de conversar y (si tiene pantalla) aprobar bocetos.

| # | Tarea | Necesita antes |
|---|---|---|
| P0 | Este documento, conversado y aprobado | — |
| P1 | Armado para la tienda: llave de firma fija como secreto, AAB además del APK, API objetivo 36, `versionCode` automático | Decisión 2 (id de la app) |
| P2 | Textos legales en GitHub Pages: privacidad, condiciones, borrar cuenta (español primero) | Decisiones 3, 10, 12 |
| P3 | Configuración (bocetos y luego pantalla): idioma, letra, sonido, privacidad, acerca de | Bocetos aprobados |
| P4 | Menú inicial nuevo y primera vez (edad, condiciones) | Bocetos aprobados |
| P5 | Idiomas listos para crecer: elegir idioma, informe de claves que faltan, forma de género y plural, idioma de prueba estirado en las capturas | Decisión 5 |
| P6 | Prueba cerrada en Play Console con 12 probadores por 14 días (lo hace Isaias, con una guía paso a paso) | P1, P2 |
| P7 | Backend: cuenta con Google Play Games y guardado en la nube | Decisiones 10, 11 |
| P8 | Compras con Unity IAP y verificación en el servidor; tienda | P7, decisión 8, bocetos de la tienda |
| P9 | Anuncios con AdMob y su consentimiento | Decisiones 6, 7 |
| P10 | Expansiones descargables | D11 (arquitectura C), P8 |
| P11 | Inglés (capa 1) y ficha de la tienda en inglés; después portugués | P5 |

La prueba cerrada (P6) se puede hacer **sin compras ni anuncios**: conviene empezarla apenas estén P1 y P2, para que los 14 días corran mientras se programa lo demás.
