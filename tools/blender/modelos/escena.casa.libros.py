"""Libros de estudio apilados sobre la mesa (escena.casa.libros): tres libros de tapa dura con
sus hojas a la vista, cada uno un poco girado, como quedan cuando se estudia de verdad.
"""

import comun as c

MESA = 0.44


def armar():
    hojas = c.material("hojas", (0.93, 0.91, 0.85), aspereza=0.9)
    tapas = [(0.55, 0.18, 0.18), (0.18, 0.30, 0.50), (0.22, 0.42, 0.28)]
    y = MESA
    for i, color in enumerate(tapas):
        tapa = c.material(f"tapa_{i}", color, aspereza=0.6)
        alto = 0.05 - i * 0.006
        x, z = -0.45, -0.15
        partes = [
            c.caja("Tapa de abajo", (x, y + 0.002, z), (0.36, 0.004, 0.26), tapa, bisel=0.002),
            c.caja("Hojas", (x + 0.004, y + alto / 2, z), (0.345, alto - 0.008, 0.245), hojas),
            c.caja("Tapa de arriba", (x, y + alto - 0.002, z), (0.36, 0.004, 0.26), tapa, bisel=0.002),
            c.caja("Lomo", (x - 0.178, y + alto / 2, z), (0.008, alto, 0.26), tapa, bisel=0.003),
        ]
        for p in partes:
            p.location = c.unity(*_girar(_desde_blender(p.location), (x, z), i * 12))
            c.girar_y(p, i * 12)
        y += alto + 0.002


def _desde_blender(loc):
    return (-loc[0], loc[2], -loc[1])


def _girar(pos, eje, grados):
    import math
    x, y, z = pos
    ex, ez = eje
    a = math.radians(grados)
    dx, dz = x - ex, z - ez
    return (ex + dx * math.cos(a) + dz * math.sin(a), y, ez - dx * math.sin(a) + dz * math.cos(a))
