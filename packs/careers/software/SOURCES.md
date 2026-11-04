# Fuentes del paquete de ingeniería de software

## Sueldos

- `sueldo_relativo` en `career.json` (cuánto gana un titulado frente al sueldo típico de Chile, $760.000): analista programador 1,4; ingeniería en informática 1,7; ingeniería civil en computación 3,0. Estimado de memoria con el ingreso al cuarto año de titulación de [mifuturo.cl](https://www.mifuturo.cl) (Mineduc). Por verificar.

## Áreas y cargos (estimaciones, a verificar)

- Las áreas, sus requisitos y la escalera de cargos siguen el documento de diseño (sección de la carrera y "Reglas para todas las carreras").
- `sueldo_relativo` de áreas y cargos, `meses_minimos` y `desempeno_minimo` son **estimaciones de diseño**, calibradas con el simulador (`dotnet run --project tools/MistakesLife.Simulator -- 300 45` y `-- 300 65`) para que ascender lleve años y llegar a la cima sea raro. Falta contrastarlos con encuestas de sueldos por cargo en Chile.
- Los eventos de cada área (típicos y dilemas) están inspirados en situaciones reales de la profesión, sin copiar casos con nombre.
