# Fuentes del paquete de psicología

| Dato | Valor usado | Fuente |
|---|---|---|
| Solo en universidades | El título de psicólogo exige la licenciatura en psicología, que solo dan las universidades | [DFL 2 de 2010 (Mineduc), artículo 63 letra k](https://iura.cl/dfl-2-mineduc-2010/63) |
| Duración | 10 semestres (5 años) | [Meganoticias, datos de Mifuturo (2024)](https://www.meganoticias.cl/dato-util/462683-sueldos-psicologia-recien-egresados-cuanto-ganan-mifuturo-pdp-21-10-2024.html) |
| Ingreso | ~$909 mil bruto al primer año, ~$1,2 millones al cuarto | Misma fuente |
| Empleabilidad y saturación | ~77% al primer año; es la carrera universitaria con más matrícula (~11.500 en 2024) | [En Cancha (2025)](https://www.encancha.cl/enlahora/nacional/2025/04/20/estas-son-las-carreras-con-mas-matriculas-en-2025-sueldos-de-hasta-3-millones-y-97-de-empleabilidad/) (estimación de prensa, por confirmar en Mifuturo) |
| Áreas y práctica clínica | Clínica, educacional, organizacional; prácticas en servicios de salud públicos y privados | [Aceituno, Universidad de Chile (2017)](https://uchile.cl/noticias/133422/la-salud-mental-en-chile-y-la-acreditacion-de-psicologosas-clinicos) |

## Estimaciones (por calibrar)

- La universidad privada sin puntaje alto representa que muchas universidades privadas aceptan puntajes PAES más bajos (con arancel alto).
- El primer trabajo aparece con menos frecuencia que en contabilidad (peso 15 contra 30) para reflejar la menor empleabilidad.
- El trabajo a honorarios en salud es frecuente, pero el porcentaje exacto está por investigar.

## Sueldos

- `sueldo_relativo` en `career.json` (cuánto gana un titulado frente al sueldo típico de Chile, $760.000): psicólogo 1,45 (universidad del CRUCH) y 1,35 (universidad privada). Estimado de memoria con el ingreso al cuarto año de titulación de [mifuturo.cl](https://www.mifuturo.cl) (Mineduc). Por verificar.

## Áreas y cargos (estimaciones, a verificar)

- Las áreas, sus requisitos y la escalera de cargos siguen el documento de diseño (sección de la carrera y "Reglas para todas las carreras").
- `sueldo_relativo` de áreas y cargos, `meses_minimos` y `desempeno_minimo` son **estimaciones de diseño**, calibradas con el simulador (`dotnet run --project tools/MistakesLife.Simulator -- 300 45` y `-- 300 65`) para que ascender lleve años y llegar a la cima sea raro. Falta contrastarlos con encuestas de sueldos por cargo en Chile.
- Los eventos de cada área (típicos y dilemas) están inspirados en situaciones reales de la profesión, sin copiar casos con nombre.
