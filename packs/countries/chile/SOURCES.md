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

## Estimaciones por revisar

- **Mortalidad desde los 90 años:** la tabla del INE agrupa "90 y más"; de 90 a 110 se extrapoló siguiendo la curva de las edades anteriores (cada 5 años sube ~1,5 veces). Además la tabla es de 2011: hoy la esperanza de vida en Chile es algo mayor (cerca de 81 años en promedio). Conviene actualizarla con la tabla más reciente del INE.

- **Probabilidades anuales sin estadística exacta:** temblor fuerte que se siente en Santiago (0.3), temporal que inunda la calle (0.25) y un 18 memorable (0.35).

- **Nivel de hogar de Puente Alto (10 a 65):** comuna popular y de clase media baja, con sectores de alta vulnerabilidad como Bajos de Mena. CASEN 2024 da 13,8 % de pobreza por ingresos y 22,1 % multidimensional: hay que llevar esos números a la distribución de `nivel_hogar`.
- **Solo una comuna:** la fase 1 parte en Puente Alto. Las demás comunas de Santiago se suman después de investigarlas.
