# Fuentes del paquete de Chile

| Dato | Fuente | Nota |
|---|---|---|
| Población de Puente Alto (568.086, usada como peso de nacimiento en miles) | Censo 2024, INE, vía [Emol](https://www.emol.com/noticias/Nacional/2025/03/28/1161836/censo-2024-comunas-mas-menos.html) | Comuna más poblada del país |
| Población extranjera de Puente Alto (29.760, +186 % desde 2017) | [SERMIG, minuta comunal](https://serviciomigraciones.cl/wp-content/uploads/estudios/Minutas-Comuna/RM/Puente-Alto.pdf) | Para futuros eventos de migración |
| Nombres | Registro Civil 2024, vía [BioBioChile](https://www.biobiochile.cl/noticias/servicios/toma-nota/2024/12/06/los-100-nombres-de-ninas-y-ninos-mas-inscritos-el-2024-en-chile-isabella-podria-superar-a-emma.shtml) | Mezclados con nombres comunes de los años 80 y 90 para la generación de los padres (estimación) |
| Apellidos (40 más inscritos 2014-2023) | Registro Civil, vía [CNN Chile](https://www.cnnchile.com/pais/cuales-100-apellidos-mas-inscritos-chile_20240408/) | |
| Edad de la madre | INE, Estadísticas Vitales, vía [La Tercera](https://www.latercera.com/que-pasa/noticia/edad-promedio-en-que-las-mujeres-tienen-su-primer-hijo-en-chile-aumento-casi-tres-anos-en-ultimas-cuatro-decadas/650531/) | Edad promedio al primer hijo: 25 años (2016). Rango 18 a 42 como simplificación |
| Etapas educativas | Sistema educacional chileno (sala cuna, nivel medio, NT1, NT2, básica 1° a 8°, media 1° a 4°); kínder obligatorio por reforma constitucional de 2013 | |
| Gratuidad | Hogares del 60 % de menores ingresos, vía FUAS e institución adscrita ([ChileAtiende](https://www.chileatiende.gob.cl/fichas/43203-gratuidad-en-la-educacion-superior-2018)) | En el juego: `nivel_hogar <= 60` |

| Indicadores de Puente Alto: pobreza por ingresos 13,8 %, multidimensional 22,1 %, hacinamiento 6,9 %, escolaridad 10,4 años | [BCN, reporte comunal](https://www.bcn.cl/siit/reportescomunales/comunas_v.html?anno=2026&idcom=13201) (CASEN 2024, Censo 2024, RSH 2026) | Para calibrar `nivel_hogar`, allegados y subsidios |
| Voto obligatorio desde los 18; multa de 0,5 UTM que cobra el Juzgado de Policía Local | [Meganoticias (2024)](https://www.meganoticias.cl/dato-util/462986-multas-por-no-ir-a-votar-elecciones-municipales-pdp-24-10-2024.html) | Eventos `primera_votacion` y `citacion_policia_local` |
| Línea 4 del Metro llega a Plaza de Puente Alto desde 2005 | [Cooperativa (2005)](https://www.cooperativa.cl/noticias/site/artic/20051130/pags-amp/20051130115251.html) | Evento `viaje_largo` |

| Robo con violencia o intimidación: 3,5 % de los hogares al año | [INE, ENUSC 2024](https://www.ine.gob.cl/sala-de-prensa/prensa/general/noticia/2025/07/07/subsecretar%C3%ADa-de-prevenci%C3%B3n-del-delito-e-ine-presentan-los-resultados-de-la-encuesta-nacional-urbana-de-seguridad-ciudadana-(enusc)-2024) | `robo_celular` usa `probabilidad_anual` 0.04 (estimación por persona) |
| Tabla de mortalidad 0 a 85 años (probabilidad anual de morir por edad y sexo; esperanza de vida 81,4 mujeres y 76,0 hombres) | INE, tablas de vida 2011 (Estadísticas Vitales), publicadas por la Superintendencia de Pensiones; copia en [Human Life-Table Database](https://www.lifetable.de/File/GetDocument/data%5CCHL%5CCHL000020112011CU1.pdf) | Campo `mortalidad` de `country.json` |
| Fecundidad: 1,16 hijos por mujer (2023) y edad promedio de la madre al primer hijo cerca de 29 años | INE, Estadísticas Vitales 2023 | Para calibrar `events/familia.json` (en el simulador: ~1,2 hijos por persona y primer hijo a los ~30) |
| Diversidad: 3 % gay o lesbiana, 4 % bisexual y 3 % trans, no binario o de género fluido; 10 % se siente parte de la comunidad LGBT+ | Ipsos, *LGBT+ Pride 2023*, vía [Alturi](https://alturi.org/news_items/sexual-diversity-10-of-chileans-say-they-feel-part-of-the-lgbt-community-and-65-favor-adoption-by-gay-parents) | Campo `diversidad` de `country.json` |

## Estimaciones por revisar

- **Diversidad (`diversidad` en `country.json`):** de la encuesta Ipsos se dejaron fuera quienes no respondieron, se sumó pansexual a bisexual, se agregó 1 % de asexuales y el 3 % trans/no binario se repartió en 1 % trans y 2 % no binario. Conviene revisar con datos oficiales (el Censo 2024 preguntó por primera vez por orientación e identidad).

- **Leyes (`leyes` en `country.json`), anotadas de memoria y por verificar en [Ley Chile (BCN)](https://www.bcn.cl/leychile):** mayoría de edad 18; voto obligatorio desde los 18 (reforma de 2022); divorcio (Ley 19.947, 2004); matrimonio igualitario con adopción para parejas del mismo sexo (Ley 21.400, 2022); identidad de género desde los 14 años (Ley 21.120, 2018); aborto por tres causales (Ley 21.030, 2017); edad legal de jubilación 65 hombres y 60 mujeres (DL 3.500).

- **Familia no jugada (`familia_no_jugada` en `country.json`):** pareja 0,15 al año, hijo 0,07 al año y separación 0,03 al año, ajustadas con el simulador para que los hijos no jugados lleguen a los 50 años con ~1,2 hijos en promedio, como la fecundidad del INE.
- **Familia (`events/familia.json`):** las probabilidades de conocer pareja (0,3 al año), de conversar sobre hijos (0,4), de lograr el embarazo al buscarlo (0,6), de embarazo no planificado (0,03) y de crisis de pareja (0,06) son estimaciones ajustadas para acercarse a la fecundidad del INE. Falta calibrar con CASEN cuántas personas viven en pareja a cada edad (hoy ~78 % a los 45, probablemente alto).

- **Mortalidad desde los 90 años:** la tabla del INE agrupa "90 y más"; de 90 a 110 se extrapoló siguiendo la curva de las edades anteriores (cada 5 años sube ~1,5 veces). Además la tabla es de 2011: hoy la esperanza de vida en Chile es algo mayor (cerca de 81 años en promedio). Conviene actualizarla con la tabla más reciente del INE.

- **Probabilidades anuales sin estadística exacta:** temblor fuerte que se siente en Santiago (0.3), temporal que inunda la calle (0.25) y un 18 memorable (0.35).

- **Nivel de hogar de Puente Alto (10 a 65):** comuna popular y de clase media baja, con sectores de alta vulnerabilidad como Bajos de Mena. CASEN 2024 da 13,8 % de pobreza por ingresos y 22,1 % multidimensional: hay que llevar esos números a la distribución de `nivel_hogar`.
- **Solo una comuna:** la fase 1 parte en Puente Alto. Las demás comunas de Santiago se suman después de investigarlas.
