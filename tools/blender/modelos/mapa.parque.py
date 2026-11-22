"""El parque del barrio (mapa.parque): pasto amplio, una laguna, un sendero que la rodea,
árboles grandes y bancas. Para trotar o hacer un picnic.
"""

import math

import barrio as b

SENDERO = (0.78, 0.70, 0.56)


def armar():
    b.vereda()
    b.pasto((0, 0.05, 0), (3.1, 0.02, 2.9), color=(0.44, 0.57, 0.35))
    # Laguna de orilla irregular: una elipse de piedras y el agua encima.
    b.cilindro("Orilla", (0.5, 0.07, 0.3), 0.62, 0.04, (0.62, 0.60, 0.55), lados=24).scale = (1.35, 1.0, 1.0)
    agua = b.cilindro("Laguna", (0.5, 0.08, 0.3), 0.56, 0.04, b.mat("Agua", (0.38, 0.55, 0.66), 0.05), lados=24)
    agua.scale = (1.35, 1.0, 1.0)
    # Sendero que da la vuelta a la laguna, en tramos.
    for i in range(14):
        a = i / 14 * math.tau
        x = 0.5 + math.cos(a) * 1.05
        z = 0.3 + math.sin(a) * 0.82
        tramo = b.caja("Sendero", (x, 0.065, z), (0.42, 0.02, 0.2), SENDERO)
        tramo.rotation_euler.z = a - math.pi / 2
    b.caja("Sendero", (-0.9, 0.065, -0.95), (0.2, 0.02, 1.2), SENDERO)
    for i, (x, z) in enumerate([(-1.15, 1.05), (-1.2, 0.05), (-0.95, -1.15), (1.3, -1.15), (-0.35, 1.2), (1.35, 1.2)]):
        b.arbol((x, 0.06, z), alto=1.15, semilla=20 + i)
    b.banca((-0.55, 0.06, -0.35), giro=60)
    b.banca((1.2, 0.06, -0.65), giro=-30)
    b.farol((-0.65, 0.06, 0.7))
    # Un mantel de picnic en el pasto.
    b.caja("Mantel", (-0.35, 0.065, -1.0), (0.35, 0.01, 0.3), (0.78, 0.30, 0.28))
