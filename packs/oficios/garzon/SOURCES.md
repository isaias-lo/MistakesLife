# Fuentes: garzón

- **Sueldo base.** En Chile el garzón suele tener un sueldo base cercano al mínimo (`sueldo_base` 1) y vive en buena parte de las propinas. Con contrato, el base no puede ser menor que el sueldo mínimo por la jornada que hace.
- **Propina.** La propina no es remuneración: no es imponible, no entra en la liquidación y no cuenta para la indemnización (Dirección del Trabajo, dictamen de 2016; SII, Oficio 2723 de 2022). La Ley 20.729 establece que la cuenta debe sugerir una propina de al menos el 10 %, y que es voluntaria para el cliente. Por eso en el juego la propina llega directo a tu dinero y no a tu liquidación (`pago_variable` "propina").
- **Cuánto son las propinas.** No hay una estadística oficial. `variable_tipico` 0,6 (las propinas de un mes normal, unas seis décimas del sueldo base) es una estimación nuestra por afinar con datos de remuneraciones del sector (Encuesta Suplementaria de Ingresos del INE, rama de alojamiento y servicio de comidas).
- **Turno cortado.** La jornada puede dividirse con un tiempo de colación; los turnos partidos de mediodía y noche son habituales en restoranes.
- **Roturas.** El empleador solo puede descontar lo que permite el art. 58 del Código del Trabajo: las roturas no están entre esos descuentos.
- **Temporada.** Diciembre (cenas de fin de año) y septiembre (Fiestas Patrias) suben; el invierno baja. La forma es conocida; los números (`temporada`) son estimaciones nuestras.
- **Argentina.** Allá se dice mozo. El convenio de gastronomía (UTHGRA) paga varias veces el salario mínimo; `sueldo_base_por_pais` 2,7 es una estimación nuestra por afinar con la escala oficial de UTHGRA-FEHGRA de octubre de 2024 (no encontramos la escala de mozo publicada).
