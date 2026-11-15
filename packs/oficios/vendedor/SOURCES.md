# Fuentes: vendedor de tienda

- **Sueldo base y comisión (Chile).** Un estudio del retail (2022) encontró que un vendedor gana en promedio unos $764.000 brutos: cerca de $413.000 fijos y $333.000 variables (El Desconcierto, 2022). En ese momento el sueldo mínimo era $400.000, así que el base es cercano al mínimo (`sueldo_base` 1) y lo variable típico es cerca del 75 % del base (`variable_tipico` 0,75). Avisos de 2024-2025 ofrecen $560.000 de base más comisión.
- **Comisión imponible.** La comisión es remuneración (art. 42 del Código del Trabajo): entra en la liquidación y se cotiza.
- **Domingos.** En el comercio, al menos dos de los días de descanso del mes deben caer en domingo (art. 38 del Código del Trabajo, reforma de la Ley 20.823).
- **Ley de la silla.** El art. 193 del Código del Trabajo obliga a mantener asientos para el personal del comercio.
- **Merma.** El empleador solo puede descontar del sueldo lo que permite el art. 58 del Código del Trabajo: la merma no está entre esos descuentos.
- **Temporada.** Diciembre es el mes más fuerte del comercio; noviembre (Black Friday y Cyber), mayo (Día de la Madre y Cyber) y septiembre (Fiestas Patrias) suben; enero y febrero bajan. La forma es conocida; los números (`temporada`) son estimaciones nuestras por afinar con datos de ventas del comercio (INE, Índice de Actividad del Comercio).
- **Argentina.** El básico de un vendedor B del convenio de Empleados de Comercio (FAECYS) era unos $820.000 en octubre de 2024, con un salario mínimo de $271.571: unas 3 veces (`sueldo_base_por_pais`). Fuentes: Río Negro y El Economista, escalas de octubre de 2024.
- **Escalera de cargos.** Vendedor, encargado de sección, subgerente y jefe de tienda es la escalera habitual de una tienda de cadena; los tiempos (`meses_minimos`) son estimaciones por afinar.
