# Fuentes del paquete de contabilidad

| Dato | Valor usado | Fuente |
|---|---|---|
| Contador general (técnico de nivel superior) | 5 semestres en CFT, con continuidad a contador auditor | [CFT Santo Tomás, ficha de la carrera](https://www.cftsantotomas.cl/web/wp-content/uploads/sites/7/2025/05/CFT_Contabilidad_general.pdf) |
| Contador auditor: duración formal | 8 semestres lo más común, 10 en algunas universidades | [Rankia: qué hace un contador auditor](https://www.rankia.cl/blog/sii/3410655-que-hace-como-ser-contador-auditor-chile) (datos de Mifuturo) |
| Contador auditor: duración real | 11,8 semestres en universidad, 10,1 en instituto profesional | Misma fuente |
| Empleabilidad al primer año | 85,3% universidad, 84,0% instituto profesional | Misma fuente |
| Ingreso bruto al primer año | ~$1,2 millones (universidad e IP) | Misma fuente |
| Áreas de trabajo | Auditoría, tributación, contabilidad de pymes, sector público (SII, municipios) | Misma fuente |

## Estimaciones (por calibrar)

- La universidad usa 10 semestres formales (tomamos el extremo largo para acercarnos a la duración real). Las reprobaciones agregan semestres.
- Los sueldos todavía no se usan: llegarán con el sistema de economía.

## Sueldos

- `sueldo_relativo` en `career.json` (cuánto gana un titulado frente al sueldo típico de Chile, $760.000): contador general (CFT) 1,0; contador auditor de instituto 1,45; contador auditor universitario 2,0. Estimado de memoria con el ingreso al cuarto año de titulación de [mifuturo.cl](https://www.mifuturo.cl) (Mineduc). Por verificar.

## Áreas y cargos (estimaciones, a verificar)

- Las áreas, sus requisitos y la escalera de cargos siguen el documento de diseño (sección de la carrera y "Reglas para todas las carreras").
- `sueldo_relativo` de áreas y cargos, `meses_minimos` y `desempeno_minimo` son **estimaciones de diseño**, calibradas con el simulador (`dotnet run --project tools/MistakesLife.Simulator -- 300 45` y `-- 300 65`) para que ascender lleve años y llegar a la cima sea raro. Falta contrastarlos con encuestas de sueldos por cargo en Chile.
- Los eventos de cada área (típicos y dilemas) están inspirados en situaciones reales de la profesión, sin copiar casos con nombre.
