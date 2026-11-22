"""Ir a la fonda (salida.fonda, paquete de Chile): de cerca, la ramada de palos y paja con
banderines, el mesón de las empanadas, un barril de chicha, mesas largas y la pista de tierra
donde se baila cueca.
"""

import barrio as b

TIERRA = (0.70, 0.60, 0.45)
PALO = (0.45, 0.34, 0.22)
PAJA = (0.78, 0.68, 0.42)
BANDERAS = ((0.80, 0.16, 0.13), (0.95, 0.95, 0.93), (0.14, 0.24, 0.50))


def armar():
    b.suelo_abierto(TIERRA)
    # Ramada al fondo, a lo ancho.
    for x in (-3.0, -1.0, 1.0, 3.0):
        for z in (1.2, 2.6):
            b.cilindro("Palo", (x, 1.15, z), 0.07, 2.3, PALO, lados=8)
    b.techo_dos_aguas("Techo de paja", (0, 2.3, 1.9), 6.0, 1.4, 0.55, PAJA, alero=0.25)
    b.caja("Mesón de las empanadas", (-1.5, 0.5, 2.1), (2.4, 1.0, 0.6), PALO)
    b.caja("Cubierta del mesón", (-1.5, 1.02, 2.1), (2.5, 0.05, 0.7), b.MADERA)
    for i in range(8):
        b.esfera("Empanada", (-2.4 + i * 0.25, 1.08, 2.05), 0.08, b.mat("Empanada", (0.82, 0.56, 0.28), 0.8), escala=(1.4, 0.45, 0.8), detalle=1)
    b.cilindro("Barril de chicha", (1.2, 0.45, 2.1), 0.32, 0.9, (0.48, 0.32, 0.18), lados=16)
    for y in (0.15, 0.75):
        b.cilindro("Zuncho", (1.2, y, 2.1), 0.33, 0.04, b.FIERRO, lados=16)
    # Banderines en cordeles que cruzan sobre la pista.
    for fila, z in enumerate((0.9, -0.1)):
        b.cilindro("Cordel", (0, 2.2, z), 0.008, 7.0, (0.30, 0.28, 0.24), eje="x", lados=4)
        for i in range(24):
            x = -3.3 + i * 0.29
            color = BANDERAS[(i + fila) % 3]
            b.malla("Banderín", [(x - 0.1, 2.2, z), (x + 0.1, 2.2, z), (x, 1.96, z)], [(0, 1, 2)], b.mat("Banderín %d" % ((i + fila) % 3), color, 0.8))
    for z in (0.9, -0.1):
        b.cilindro("Poste del cordel", (-3.4, 1.1, z), 0.05, 2.2, PALO, lados=6)
    # Mesa larga con bancas a un lado.
    b.caja("Mesa", (2.6, 0.72, 0.0), (0.8, 0.05, 2.2), b.MADERA)
    for dx in (-0.55, 0.55):
        b.caja("Banca", (2.6 + dx, 0.42, 0.0), (0.28, 0.04, 2.2), b.MADERA)
    for dz in (-0.9, 0.9):
        b.caja("Pata", (2.6, 0.36, dz), (0.7, 0.7, 0.06), PALO)
