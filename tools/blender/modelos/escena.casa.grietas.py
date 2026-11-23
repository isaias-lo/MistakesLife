"""No reparaste la casa después del terremoto (escena.casa.grietas): una grieta en zigzag que baja
por el muro, con ramas finas y el estuco saltado en algunas partes.
"""

import math

import comun as c


def tramo(nombre, a, b, grueso, mat):
    (y0, z0), (y1, z1) = a, b
    largo = math.hypot(y1 - y0, z1 - z0)
    o = c.caja(nombre, (0.026, (y0 + y1) / 2, (z0 + z1) / 2), (0.004, largo + grueso * 0.5, grueso), mat)
    c.girar_x(o, math.degrees(math.atan2(z1 - z0, y1 - y0)))


def armar():
    oscura = c.material("grieta", (0.16, 0.14, 0.13), aspereza=0.9)
    estuco = c.material("estuco_saltado", (0.62, 0.58, 0.52), aspereza=0.95)
    puntos = [(2.3, 0.8), (2.18, 0.86), (2.0, 0.95), (1.86, 0.88), (1.7, 0.82), (1.55, 0.92), (1.4, 1.0), (1.25, 0.94), (1.1, 0.9)]
    for i in range(len(puntos) - 1):
        tramo("Grieta", puntos[i], puntos[i + 1], 0.012 - i * 0.0008, oscura)
    for inicio, fin in (((2.0, 0.95), (1.92, 1.06)), ((1.7, 0.82), (1.62, 0.72)), ((1.4, 1.0), (1.33, 1.1))):
        tramo("Rama de la grieta", inicio, fin, 0.005, oscura)
    for y, z, tam in ((1.98, 0.97, 0.05), (1.42, 0.99, 0.04)):
        c.caja("Estuco saltado", (0.024, y, z), (0.004, tam, tam * 0.8), estuco)
