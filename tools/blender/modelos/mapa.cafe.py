"""El café del barrio (mapa.cafe): un local chico de esquina con toldo verde, una terraza con
mesas redondas y sillas, quitasol y maceteros.
"""

import barrio as b

MURO = (0.88, 0.80, 0.68)
TOLDO = (0.20, 0.42, 0.33)
MESA = (0.30, 0.30, 0.31)


def mesa(x, z):
    b.cilindro("Mesa", (x, 0.27, z), 0.14, 0.02, MESA, lados=14)
    b.cilindro("Pata de mesa", (x, 0.16, z), 0.015, 0.2, MESA, lados=6)
    for dx, dz in ((-0.2, 0), (0.2, 0)):
        b.caja("Silla", (x + dx, 0.16, z + dz), (0.11, 0.02, 0.11), b.MADERA)
        b.caja("Respaldo", (x + dx * 1.25, 0.24, z + dz), (0.02, 0.16, 0.11), b.MADERA)
        for px in (-0.04, 0.04):
            b.caja("Pata de silla", (x + dx + px, 0.1, z + dz), (0.01, 0.1, 0.1), b.FIERRO)


def armar():
    b.vereda()
    b.caja("Café", (0, 0.65, 0.65), (2.4, 1.2, 1.5), MURO, bisel=0.01)
    b.caja("Techo", (0, 1.27, 0.65), (2.45, 0.05, 1.55), (0.42, 0.40, 0.38))
    frente = 0.65 - 0.75
    b.ventana((-0.55, 0.55, frente), 0.9, 0.6, marco=(0.20, 0.22, 0.20))
    b.caja("Puerta", (0.55, 0.45, frente - 0.02), (0.4, 0.75, 0.04), b.mat("Vidrio", b.VIDRIO, 0.15))
    b.caja("Marco de la puerta", (0.55, 0.45, frente - 0.01), (0.46, 0.8, 0.03), (0.20, 0.22, 0.20))
    # Toldo inclinado sobre la vereda.
    b.malla("Toldo", [(-1.2, 0.98, frente), (1.2, 0.98, frente), (1.2, 0.82, frente - 0.55), (-1.2, 0.82, frente - 0.55)],
            [(0, 1, 2, 3)], b.mat("Toldo", TOLDO, 0.8))
    b.caja("Faldón del toldo", (0, 0.79, frente - 0.55), (2.4, 0.07, 0.01), TOLDO)
    # Terraza.
    mesa(-0.75, -0.95)
    mesa(0.15, -0.95)
    b.cilindro("Palo del quitasol", (0.15, 0.4, -0.95), 0.012, 0.6, b.FIERRO, lados=6)
    quitasol = b.malla("Quitasol", [(0.15 - 0.3, 0.62, -0.95 - 0.3), (0.15 + 0.3, 0.62, -0.95 - 0.3), (0.15 + 0.3, 0.62, -0.95 + 0.3),
                                     (0.15 - 0.3, 0.62, -0.95 + 0.3), (0.15, 0.72, -0.95)],
                       [(0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4)], b.mat("Quitasol", (0.92, 0.90, 0.84), 0.8))
    for x in (-1.4, 1.0, 1.4):
        b.maceta((x, 0.06, -0.3))
    b.arbol((1.25, 0.06, -1.1), alto=0.95, semilla=51)
