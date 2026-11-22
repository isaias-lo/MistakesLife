"""Ir al cine del mall (salida.mall): la sala oscura con la pantalla encendida al fondo, filas de
butacas de tela roja con apoyabrazos, alfombra y luces bajas en el muro.
"""

import barrio as b

ALFOMBRA = (0.20, 0.16, 0.20)
MURO = (0.16, 0.13, 0.17)
TELA = (0.52, 0.14, 0.18)
PLASTICO = (0.12, 0.12, 0.13)


def butaca(x, y, z):
    b.caja("Asiento", (x, y + 0.42, z), (0.5, 0.12, 0.48), TELA, bisel=0.03)
    b.caja("Respaldo", (x, y + 0.72, z - 0.22), (0.5, 0.6, 0.1), TELA, bisel=0.03)
    b.caja("Apoyabrazos", (x - 0.28, y + 0.58, z - 0.02), (0.06, 0.05, 0.45), PLASTICO)
    b.caja("Base", (x, y + 0.18, z - 0.05), (0.46, 0.36, 0.3), PLASTICO)


def armar():
    b.sala(ALFOMBRA, MURO, alto=3.2)
    luz_pantalla = b.mat("Pantalla", (0.88, 0.90, 0.94), 0.4)
    b.caja("Marco de la pantalla", (0, 1.85, 2.7), (5.8, 2.4, 0.04), (0.05, 0.05, 0.06))
    b.caja("Pantalla", (0, 1.85, 2.67), (5.6, 2.25, 0.03), luz_pantalla)
    # Gradas: cada fila un poco más alta, mirando a la pantalla.
    for fila in range(3):
        z = 1.5 - fila * 1.0
        y = fila * 0.22
        if fila > 0:
            b.caja("Grada", (0, y / 2, z - 0.05), (6.9, y, 1.0), ALFOMBRA)
        for i in range(-3, 4):
            butaca(i * 0.62 - 0.6, y, z)
    # Luces bajas en el muro lateral y el pasillo.
    for z in (-1.5, 0.0, 1.5):
        b.caja("Aplique", (-3.47, 1.4, z), (0.06, 0.12, 0.3), b.mat("Luz cálida", (0.98, 0.80, 0.50), 0.3))
    for z in (-2.0, -1.0, 0.0, 1.0):
        b.caja("Luz del pasillo", (2.35, 0.03, z), (0.08, 0.02, 0.08), b.mat("Luz del pasillo", (0.98, 0.86, 0.55), 0.3))
