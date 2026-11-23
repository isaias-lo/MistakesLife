"""El gimnasio del barrio (mapa.gimnasio): un local de fachada oscura con vidrio de piso a techo,
donde se alcanzan a ver las máquinas, y unos estacionamientos de bicicletas afuera.
"""

import barrio as b

FACHADA = (0.26, 0.27, 0.29)
ACENTO = (0.80, 0.38, 0.22)


def armar():
    b.vereda()
    b.caja("Gimnasio", (0, 0.75, 0.45), (2.8, 1.4, 1.9), FACHADA, bisel=0.01)
    frente = 0.45 - 0.95
    b.caja("Vidrio", (0, 0.6, frente - 0.01), (2.4, 0.95, 0.03), b.mat("Vidrio oscuro", (0.30, 0.38, 0.44), 0.08))
    for x in (-1.2, -0.6, 0, 0.6, 1.2):
        b.caja("Montante", (x, 0.6, frente - 0.03), (0.04, 0.95, 0.03), (0.15, 0.15, 0.16))
    b.caja("Franja", (0, 1.25, frente - 0.02), (2.8, 0.12, 0.04), ACENTO)
    # Máquinas que se ven por el vidrio.
    for x in (-0.9, -0.3, 0.3, 0.9):
        b.caja("Trotadora", (x, 0.2, frente + 0.35), (0.22, 0.14, 0.45), (0.12, 0.12, 0.13))
        b.caja("Manubrio", (x, 0.38, frente + 0.15), (0.2, 0.22, 0.03), (0.12, 0.12, 0.13))
    # Bicicletero y dos bicicletas.
    for x in (0.8, 1.05, 1.3):
        b.cilindro("Bicicletero", (x, 0.15, -1.0), 0.012, 0.18, (0.55, 0.56, 0.58), eje="z", lados=6)
    for x in (0.8, 1.05):
        b.cilindro("Rueda de bici", (x, 0.12, -1.13), 0.09, 0.015, b.FIERRO, eje="x", lados=12)
        b.cilindro("Rueda de bici", (x, 0.12, -0.87), 0.09, 0.015, b.FIERRO, eje="x", lados=12)
        b.caja("Cuadro de bici", (x, 0.17, -1.0), (0.02, 0.03, 0.25), (0.20, 0.35, 0.55))
    b.arbol((-1.25, 0.06, -1.1), alto=0.9, semilla=41)
