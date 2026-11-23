"""Juguetes en el suelo (escena.casa.juguetes): una pelota de goma y cubos de madera pintada con
letras talladas, apilados como los deja una guagua.
"""

import comun as c


def armar():
    goma = c.material("goma", (0.92, 0.52, 0.20), aspereza=0.5)
    c.esfera("Pelota", (1.0, 0.12, -0.1), 0.12, goma, lados=28)
    cubos = [((1.45, 0.08, -0.3), (0.30, 0.66, 0.40), 0), ((1.6, 0.08, -0.15), (0.35, 0.50, 0.88), 15), ((1.52, 0.24, -0.22), (0.94, 0.78, 0.25), -10)]
    for i, (pos, color, giro) in enumerate(cubos):
        cubo = c.caja("Cubo", pos, (0.15, 0.15, 0.15), c.material(f"cubo_{i}", color, aspereza=0.6), bisel=0.012, segmentos=2)
        c.girar_y(cubo, giro)
