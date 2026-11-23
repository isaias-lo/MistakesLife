"""La pelota de fútbol del taller junto a la puerta (escena.casa.pelota_futbol): blanca con
parches negros repartidos.
"""

import math

from mathutils import Vector

import comun as c


def armar():
    blanca = c.material("cuero_blanco", (0.95, 0.95, 0.93), aspereza=0.45)
    negra = c.material("parche_negro", (0.10, 0.10, 0.12), aspereza=0.45)
    x, y, z, r = 0.25, 0.11, 0.35, 0.11
    c.esfera("Pelota", (x, y, z), r, blanca, lados=32)
    # Doce parches como en un icosaedro.
    fi = (1 + 5 ** 0.5) / 2
    puntos = [(0, 1, fi), (0, -1, fi), (0, 1, -fi), (0, -1, -fi), (1, fi, 0), (-1, fi, 0), (1, -fi, 0), (-1, -fi, 0), (fi, 0, 1), (-fi, 0, 1), (fi, 0, -1), (-fi, 0, -1)]
    for px, py, pz in puntos:
        n = math.sqrt(px * px + py * py + pz * pz)
        # Parche aplanado que apenas sobresale, girado para quedar tangente a la pelota.
        parche = c.esfera("Parche", (x + px / n * r * 0.97, y + py / n * r * 0.97, z + pz / n * r * 0.97), r * 0.36, negra, escala=(1, 0.12, 1), lados=12)
        normal = Vector(c.unity(px / n, py / n, pz / n))
        parche.rotation_mode = "QUATERNION"
        parche.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(normal)
