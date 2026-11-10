# Fuentes del paquete de Argentina

Primera versión (A2). **Todos los valores son de memoria y están por verificar** con las fuentes oficiales en la tarea A4 (calibración con la EPH del INDEC). Se marcan las simplificaciones.

| Dato | Fuente | Nota |
|---|---|---|
| La Matanza: cerca de 1.838.000 habitantes, el partido más poblado del conurbano | INDEC, Censo 2022, de memoria | Peso de nacimiento en miles. Nivel de hogar de 10 a 60 (más pobreza que el promedio del país). Por verificar |
| Nadie nace en Argentina por azar todavía | Decisión de diseño | `peso_nacimiento: 0` hasta que A3 agregue los eventos de vida (escuela, primer trabajo). Se juega eligiéndola |
| Nombres y apellidos más comunes | Registro Nacional de las Personas (RENAPER), de memoria | Mezcla de nombres de bebés recientes y de la generación de los padres. Un solo apellido (el del padre), como es lo habitual |
| Etapas educativas: jardín maternal (0 a 3), jardín de infantes (sala de 3), salas de 4 y 5 obligatorias, primaria de 6 años y secundaria de 6 años, obligatoria | Ley de Educación Nacional 26.206 y Ley 27.045 (sala de 4 obligatoria) | Así es en la Provincia de Buenos Aires; en otras provincias la primaria dura 7 años |
| Mortalidad: esperanza de vida cerca de 79 años las mujeres y 73 los hombres; mortalidad infantil cerca de 8 por mil | INDEC y Ministerio de Salud (DEIS), de memoria | Simplificación: la tabla de Chile × 1,3 desde los 15 años y × 1,15 en la niñez. Por reemplazar con la tabla de vida del INDEC |
| Voto desde los 16 (obligatorio de 18 a 70) | Ley 26.774 (2012) | `ley.edad_voto` 16. El voto de 16 y 17 es optativo (simplificado) |
| Matrimonio igualitario (2010), adopción por parejas del mismo sexo, identidad de género sin trámite judicial (2012), DNI no binario (2021) | Leyes 26.618 y 26.743, Decreto 476/2021 | Menores de edad pueden cambiar su género con sus representantes: `ley.identidad_genero_edad_min` 0 |
| Interrupción voluntaria del embarazo a pedido hasta la semana 14 | Ley 27.610 (2020) | `ley.aborto` `a_peticion` |
| Jubilación a los 65 los hombres y 60 las mujeres, con 30 años de aportes | Ley 24.241 | |
| Jubilación de reparto (ANSES): PBU (prestación básica universal) + 1,5 % del sueldo promedio de los últimos 10 años por año de aportes; mínima garantizada | Ley 24.241 y movilidad de la ANSES, de memoria | Simplificación: la PC y la PAP juntas como 1,5 % por año. Montos de fines de 2024 (PBU cerca de $112.000, mínima cerca de $245.000, sin bono). Por verificar |
| PUAM: 80 % de la jubilación mínima, desde los 65, para quien no tiene jubilación propia | Ley 27.260 (2016) | `pension_universal` con `completa_hasta: 0` |
| Monotributistas: aportan, pero en el juego todavía no suman años de aportes | Simplificación | Se resuelve en A3 junto al monotributo |
| Recibo de sueldo: jubilación 11 %, ley 19.032 (PAMI) 3 % y obra social 3 %, con tope de la base imponible (cerca de $2.600.000) | Ley 24.241, Ley 19.032, Ley 23.660 | Por verificar el tope |
| Impuesto a las Ganancias (cuarta categoría): se empieza a pagar con sueldos brutos cerca de $1.800.000; tramos de 5 % a 35 % | Ley 27.743 (2024), de memoria | Tramos simplificados sobre el sueldo después de descuentos. No hay declaración anual (lo retiene el empleador). Por verificar |
| Monotributo: cuota fija según la categoría | AFIP/ARCA | Simplificado como 6 % de lo facturado |
| Salario Mínimo, Vital y Móvil cerca de $271.571 (fines de 2024); sueldo típico registrado cerca de $800.000 brutos | Consejo del Salario; INDEC, de memoria | Ajustes cada 3 meses (`reajuste_meses`). Por verificar |
| Inflación alta y persistente (20 % a 50 % la mayor parte de 2007-2017, más de 100 % en 2023) e hiperinflación de 1989-1990 | INDEC, IPC, de memoria | Regímenes `estable` (5 % a 15 %), `alta` (20 % a 50 %, inicial), `muy_alta` (50 % a 120 %) y `hiper` (150 % a 300 %, rara). Recesiones más frecuentes que en Chile |
| Alquiler en La Matanza: cerca de $320.000 al mes por un departamento de 2 ambientes; la habitación ($170.000) y la casa ($480.000) se estimaron con la misma proporción que en Puente Alto | Portales de alquiler (2024), de memoria | `arriendos` de la comuna. Por verificar con avisos reales |
| Contrato de alquiler: 24 meses, un mes de depósito y ajuste cada 3 meses (desde 2023 el contrato es libre y esto es lo más común) | De memoria | `economia.vivienda`. Por verificar |
| Gastos del mes en pesos (comida, servicios, transporte con SUBE, hijos) | Canasta básica del INDEC, de memoria | Estimaciones por calibrar |
| Deudas: préstamo personal y tarjeta de crédito con tasas muy altas (60 % y 80 % al año); figurar en el Veraz | Banco Central (BCRA), de memoria | Tasas nominales: con la inflación, la tasa real es más baja. Prescripción de 5 años (Código Civil y Comercial, art. 2560) |
| Herencia: se responde por las deudas solo con lo heredado | Código Civil y Comercial, art. 2317 | `beneficio_inventario` true. La porción de la pareja es igual a la de cada hijo (`porciones_pareja` 1) |
| Empresas: más micro y pequeñas empresas que en Chile, y más quiebras | Estimación de memoria | Por verificar |
