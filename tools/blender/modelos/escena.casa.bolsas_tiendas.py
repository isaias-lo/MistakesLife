"""Bolsas de tiendas a los pies de la cama (escena.casa.bolsas_tiendas): bolsas de papel
plastificado con asas de cordón.
"""

import comun as c


def armar():
    colores = [(0.15, 0.15, 0.18), (0.82, 0.30, 0.44), (0.95, 0.95, 0.94)]
    for i, color in enumerate(colores):
        mat = c.material(f"bolsa_{i}", color, aspereza=0.4)
        x, z = -0.25 + i * 0.25, -1.25
        b = c.caja("Bolsa", (x, 0.17, z), (0.22, 0.34, 0.1), mat, bisel=0.004)
        c.girar_y(b, -15 + i * 15)
        for dx in (-0.04, 0.04):
            a = c.caja("Asa", (x + dx, 0.38, z), (0.006, 0.08, 0.006), c.material("cordon", (0.85, 0.80, 0.70), aspereza=0.8))
            c.girar_z(a, -dx * 400)
