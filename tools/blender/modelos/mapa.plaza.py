"""La plaza del barrio (mapa.plaza): pasto con caminos de maicillo en cruz, una pileta al medio,
árboles, bancas, faroles y los juegos de niños (columpio y resbalín).
"""

import barrio as b

MAICILLO = (0.80, 0.72, 0.58)
ROJO = (0.68, 0.24, 0.20)


def armar():
    b.vereda()
    b.pasto((0, 0.05, 0), (3.0, 0.02, 2.8))
    b.caja("Camino", (0, 0.065, 0), (3.0, 0.02, 0.35), MAICILLO)
    b.caja("Camino", (0, 0.065, 0), (0.35, 0.02, 2.8), MAICILLO)
    b.cilindro("Pileta", (0, 0.1, 0), 0.38, 0.1, (0.78, 0.76, 0.72), lados=20)
    b.cilindro("Agua", (0, 0.13, 0), 0.32, 0.06, b.mat("Agua", (0.42, 0.58, 0.68), 0.1), lados=20)
    for i, (x, z) in enumerate([(-1.05, 0.85), (1.05, 0.95), (-1.1, -0.9), (0.55, -1.0), (1.15, -0.45)]):
        b.arbol((x, 0.06, z), alto=0.9, semilla=10 + i)
    b.banca((-0.6, 0.06, 0.3))
    b.banca((0.6, 0.06, -0.3), giro=180)
    b.banca((0.3, 0.06, 0.7), giro=90)
    for x, z in [(-0.3, -0.3), (0.3, 0.3)]:
        b.farol((x, 0.06, z))
    # Columpio: dos marcos en A y el travesaño.
    for dx in (-0.25, 0.25):
        for dz in (-0.12, 0.12):
            p = b.cilindro("Columpio", (-0.75 + dx, 0.33, -0.55 + dz * 0.5), 0.015, 0.56, ROJO, lados=6)
            p.rotation_euler.y += 0.35 if dz > 0 else -0.35
    b.cilindro("Travesaño", (-0.75, 0.6, -0.55), 0.016, 0.55, ROJO, eje="x", lados=6)
    for dx in (-0.1, 0.1):
        b.caja("Asiento del columpio", (-0.75 + dx, 0.18, -0.55), (0.08, 0.015, 0.05), b.FIERRO)
    # Resbalín.
    b.caja("Escalera del resbalín", (-0.35, 0.25, -1.0), (0.12, 0.4, 0.04), (0.35, 0.45, 0.60))
    rampa = b.caja("Resbalín", (-0.55, 0.25, -1.0), (0.45, 0.02, 0.12), (0.35, 0.45, 0.60))
    rampa.rotation_euler.y += 0.6
