"""La fonda en Fiestas Patrias (mapa.fonda, paquete de Chile, solo en septiembre): ramadas de
palos con techo de ramas y paja, banderines rojo, blanco y azul colgando, mesones y la tierra
apisonada donde se baila cueca.
"""

import barrio as b

TIERRA = (0.70, 0.60, 0.45)
PALO = (0.45, 0.34, 0.22)
PAJA = (0.78, 0.68, 0.42)
BANDERAS = ((0.80, 0.16, 0.13), (0.95, 0.95, 0.93), (0.14, 0.24, 0.50))


def ramada(x, z, ancho=0.95, fondo=0.9):
    for dx in (-ancho / 2, ancho / 2):
        for dz in (-fondo / 2, fondo / 2):
            b.cilindro("Palo", (x + dx, 0.55, z + dz), 0.025, 1.0, PALO, lados=6)
    b.techo_dos_aguas("Techo de paja", (x, 1.05, z), ancho, fondo, 0.25, PAJA, alero=0.1)
    b.caja("Mesón", (x, 0.35, z + fondo / 2 - 0.15), (ancho - 0.1, 0.06, 0.25), b.MADERA)
    b.caja("Frente del mesón", (x, 0.2, z + fondo / 2 - 0.27), (ancho - 0.1, 0.3, 0.02), PALO)


def armar():
    b.vereda()
    b.caja("Tierra", (0, 0.05, 0), (3.1, 0.02, 2.9), TIERRA)
    for i, x in enumerate((-1.05, 0, 1.05)):
        ramada(x, 0.75)
    # Banderines en dos cordeles de lado a lado.
    for fila, z in enumerate((-0.2, -0.75)):
        b.cilindro("Cordel", (0, 1.1, z), 0.004, 3.0, (0.30, 0.28, 0.24), eje="x", lados=4)
        for i in range(15):
            x = -1.4 + i * 0.2
            color = BANDERAS[(i + fila) % 3]
            b.malla("Banderín", [(x - 0.06, 1.1, z), (x + 0.06, 1.1, z), (x, 0.97, z)], [(0, 1, 2)], b.mat("Banderín %d" % ((i + fila) % 3), color, 0.8))
    for x in (-1.5, 1.5):
        for z in (-0.2, -0.75):
            b.cilindro("Poste del cordel", (x, 0.58, z), 0.02, 1.06, PALO, lados=6)
    # Mesas largas con bancas.
    for x in (-0.6, 0.6):
        b.caja("Mesa", (x, 0.25, -1.15), (0.8, 0.04, 0.25), b.MADERA)
        for dz in (-0.2, 0.2):
            b.caja("Banca", (x, 0.15, -1.15 + dz), (0.8, 0.03, 0.08), b.MADERA)
        for dx in (-0.35, 0.35):
            b.caja("Pata", (x + dx, 0.13, -1.15), (0.04, 0.22, 0.2), PALO)
