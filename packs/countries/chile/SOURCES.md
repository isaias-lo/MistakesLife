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

## Estimaciones por revisar

- **Nivel de hogar de Puente Alto (10 a 65):** comuna popular y de clase media baja, con sectores de alta vulnerabilidad como Bajos de Mena. Falta calibrar con CASEN (pobreza por ingresos y multidimensional) en la tarea 7.
- **Solo una comuna:** la fase 1 parte en Puente Alto. Las demás comunas de Santiago se suman después de investigarlas.
