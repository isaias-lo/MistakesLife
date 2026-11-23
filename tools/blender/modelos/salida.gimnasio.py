"""Entrenar en el gimnasio (salida.gimnasio): piso de goma, espejo de muro a muro, dos trotadoras,
una banca con barra y discos, el rack de mancuernas y una colchoneta.
"""

import barrio as b

GOMA = (0.20, 0.21, 0.23)
MURO = (0.84, 0.85, 0.86)
NEGRO = (0.10, 0.10, 0.11)
METAL = (0.62, 0.63, 0.66)


def trotadora(x, z):
    b.caja("Base de la trotadora", (x, 0.12, z), (0.8, 0.2, 1.8), NEGRO, bisel=0.02)
    b.caja("Banda", (x, 0.225, z - 0.05), (0.55, 0.02, 1.5), (0.16, 0.16, 0.17))
    for dx in (-0.36, 0.36):
        b.caja("Brazo", (x + dx, 0.75, z + 0.75), (0.05, 1.1, 0.05), METAL)
    b.caja("Consola", (x, 1.3, z + 0.78), (0.75, 0.3, 0.12), NEGRO, bisel=0.02)
    b.caja("Pantalla", (x, 1.32, z + 0.715), (0.35, 0.16, 0.01), b.mat("Pantalla", (0.35, 0.55, 0.70), 0.3))


def armar():
    b.sala(GOMA, MURO, alto=3.0)
    b.caja("Espejo", (0, 1.35, 2.73), (6.6, 1.9, 0.02), b.mat("Espejo", (0.72, 0.80, 0.86), 0.05))
    b.caja("Franja", (-3.48, 2.4, 0), (0.02, 0.15, 5.5), (0.80, 0.38, 0.22))
    trotadora(-2.4, 1.5)
    trotadora(-1.3, 1.5)
    # Banca plana con la barra en su soporte.
    b.caja("Banca", (1.2, 0.42, 1.3), (0.32, 0.1, 1.2), NEGRO, bisel=0.03)
    for z in (0.8, 1.8):
        b.caja("Pata de la banca", (1.2, 0.2, z), (0.3, 0.38, 0.06), METAL)
    for dx in (-0.45, 0.45):
        b.caja("Soporte", (1.2 + dx, 0.6, 1.85), (0.05, 1.2, 0.05), METAL)
    b.cilindro("Barra", (1.2, 1.05, 1.85), 0.015, 2.0, METAL, eje="x", lados=8)
    for dx in (-0.8, -0.7, 0.7, 0.8):
        b.cilindro("Disco", (1.2 + dx, 1.05, 1.85), 0.22 if abs(dx) < 0.75 else 0.17, 0.05, NEGRO, eje="x", lados=18)
    # Rack de mancuernas en el muro lateral.
    b.caja("Rack", (-3.2, 0.45, -0.6), (0.45, 0.06, 1.8), METAL)
    b.caja("Rack", (-3.2, 0.85, -0.6), (0.45, 0.06, 1.8), METAL)
    for y in (0.5, 0.9):
        for i in range(6):
            z = -1.35 + i * 0.3
            for dx in (-0.12, 0.12):
                b.cilindro("Mancuerna", (-3.2 + dx, y + 0.07, z), 0.065, 0.06, NEGRO, eje="x", lados=8)
            b.cilindro("Mango", (-3.2, y + 0.07, z), 0.018, 0.2, METAL, eje="x", lados=6)
    b.caja("Colchoneta", (2.4, 0.02, -0.4), (0.7, 0.03, 1.7), (0.25, 0.40, 0.55), bisel=0.01)
