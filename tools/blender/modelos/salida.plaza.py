"""Dar una vuelta por la plaza (salida.plaza): de cerca, el camino de maicillo, la pileta, bancas
de madera, faroles, árboles y los juegos de niños al fondo.
"""

import barrio as b

MAICILLO = (0.80, 0.72, 0.58)
ROJO = (0.68, 0.24, 0.20)


def armar():
    b.suelo_abierto(b.PASTO)
    b.caja("Camino", (0, 0.005, -0.9), (7.0, 0.02, 1.6), MAICILLO)
    b.caja("Camino", (1.6, 0.005, 1.0), (1.2, 0.02, 3.5), MAICILLO)
    b.cilindro("Pileta", (1.6, 0.22, 1.2), 0.9, 0.44, (0.78, 0.76, 0.72), lados=28)
    b.cilindro("Agua", (1.6, 0.43, 1.2), 0.8, 0.06, b.mat("Agua", (0.42, 0.58, 0.68), 0.05), lados=28)
    b.cilindro("Surtidor", (1.6, 0.6, 1.2), 0.12, 0.5, (0.78, 0.76, 0.72), lados=12)
    b.banca((-1.9, 0, 0.25), largo=1.4, escala=3.4)
    b.banca((-0.2, 0, 0.25), largo=1.4, escala=3.4)
    for x in (-2.9, 0.7):
        b.farol((x, 0, 0.2), alto=2.6)
    for i, (x, z, alto) in enumerate(((-2.9, 1.9, 3.0), (-0.6, 2.3, 3.2), (3.0, 2.2, 2.9), (3.1, -0.3, 2.8))):
        b.arbol((x, 0, z), alto=alto, semilla=110 + i)
    # Columpio al fondo.
    for dx in (-0.7, 0.7):
        for dz in (-0.3, 0.3):
            p = b.cilindro("Columpio", (-1.6 + dx, 0.95, 2.1 + dz * 0.6), 0.04, 1.9, ROJO, lados=8)
            p.rotation_euler.x += 0.2 if dz > 0 else -0.2
    b.cilindro("Travesaño", (-1.6, 1.85, 2.1), 0.045, 1.5, ROJO, eje="x", lados=8)
    for dx in (-0.3, 0.3):
        b.cilindro("Cadena", (-1.6 + dx, 1.2, 2.1), 0.008, 1.3, b.FIERRO, lados=4)
        b.caja("Asiento del columpio", (-1.6 + dx, 0.5, 2.1), (0.4, 0.03, 0.16), b.FIERRO)
