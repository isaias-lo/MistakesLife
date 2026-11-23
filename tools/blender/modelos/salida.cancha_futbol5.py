"""Jugar un picado (salida.cancha_futbol5, paquete de Argentina): el pasto sintético de cerca, la
línea del área, el arco con su red, la reja alta, los focos y la pelota.
"""

import barrio as b

SINTETICO = (0.24, 0.52, 0.30)
LINEA = (0.95, 0.95, 0.93)


def armar():
    b.suelo_abierto(SINTETICO)
    b.caja("Línea del área", (0, 0.005, 0.9), (4.0, 0.01, 0.07), LINEA)
    for x in (-2.0, 2.0):
        b.caja("Línea del área", (x, 0.005, 1.8), (0.07, 0.01, 1.8), LINEA)
    b.caja("Línea de fondo", (0, 0.005, 2.6), (7.0, 0.01, 0.07), LINEA)
    # Arco de 3 x 2 con red.
    for x in (-1.5, 1.5):
        b.cilindro("Palo", (x, 1.0, 2.6), 0.05, 2.0, LINEA, lados=10)
    b.cilindro("Travesaño", (0, 2.0, 2.6), 0.05, 3.1, LINEA, eje="x", lados=10)
    red = b.mat("Red", (0.88, 0.88, 0.88), 0.9)
    for i in range(13):
        b.caja("Red", (-1.5 + i * 0.25, 1.0, 2.72), (0.012, 2.0, 0.012), red)
    for j in range(9):
        b.caja("Red", (0, 0.1 + j * 0.24, 2.72), (3.0, 0.012, 0.012), red)
    # Reja alta al fondo, con postes y alambre.
    gris = (0.50, 0.52, 0.55)
    for i in range(8):
        b.cilindro("Poste de la reja", (-3.4 + i * 0.97, 1.5, 2.8), 0.04, 3.0, gris, lados=8)
    for j in range(12):
        b.caja("Alambre", (0, 0.2 + j * 0.25, 2.8), (7.0, 0.01, 0.01), gris)
    for x in (-3.4, 3.3):
        b.cilindro("Torre del foco", (x, 2.2, 2.7), 0.06, 4.4, b.FIERRO, lados=8)
        b.caja("Foco", (x * 0.95, 4.35, 2.6), (0.5, 0.3, 0.15), (0.92, 0.92, 0.85))
    b.esfera("Pelota", (1.4, 0.11, -0.6), 0.11, b.mat("Pelota", (0.95, 0.95, 0.93), 0.5), detalle=2)
    b.caja("Botella de agua", (-2.6, 0.12, -0.3), (0.08, 0.24, 0.08), (0.45, 0.65, 0.80))
