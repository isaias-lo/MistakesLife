"""El bar del barrio (mapa.bar): una casona de ladrillo pintada oscura, con ventanas de luz cálida,
puerta de madera, un letrero luminoso sin letras y unas mesas altas afuera. Solo para mayores de 18.
"""

import barrio as b

LADRILLO = (0.42, 0.26, 0.22)
LUZ = (0.98, 0.80, 0.48)


def armar():
    b.vereda()
    b.caja("Bar", (0, 0.8, 0.5), (2.5, 1.5, 1.7), LADRILLO, bisel=0.01)
    b.caja("Cornisa", (0, 1.58, 0.5), (2.6, 0.08, 1.8), (0.25, 0.20, 0.18))
    frente = 0.5 - 0.85
    luz = b.mat("Luz del bar", LUZ, 0.3)
    for x in (-0.8, 0.8):
        b.ventana((x, 0.65, frente), 0.6, 0.55, vidrio=LUZ, marco=(0.16, 0.13, 0.12))
        b.ventana((x, 1.25, frente), 0.4, 0.3, vidrio=(0.55, 0.42, 0.30), marco=(0.16, 0.13, 0.12))
    b.caja("Puerta", (0, 0.45, frente - 0.02), (0.45, 0.8, 0.04), (0.28, 0.18, 0.12))
    b.caja("Farolito", (0, 0.95, frente - 0.06), (0.08, 0.08, 0.08), luz)
    b.caja("Letrero", (0, 1.25, frente - 0.03), (0.7, 0.2, 0.04), (0.12, 0.10, 0.10))
    b.caja("Luz del letrero", (0, 1.25, frente - 0.055), (0.55, 0.05, 0.02), luz)
    # Mesas altas con pisos afuera.
    for x in (-0.85, 0.85):
        b.cilindro("Mesa alta", (x, 0.42, -1.0), 0.11, 0.02, b.MADERA, lados=12)
        b.cilindro("Pata", (x, 0.24, -1.0), 0.015, 0.36, b.FIERRO, lados=6)
        for dx in (-0.17, 0.17):
            b.cilindro("Piso", (x + dx, 0.3, -1.0), 0.05, 0.02, b.MADERA, lados=10)
            b.cilindro("Pata del piso", (x + dx, 0.18, -1.0), 0.01, 0.24, b.FIERRO, lados=6)
    b.farol((1.4, 0.06, -1.3))
