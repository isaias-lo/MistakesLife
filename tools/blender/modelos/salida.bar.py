"""Ir al bar (salida.bar): muro de ladrillo, la barra de madera con sus pisos, repisas iluminadas
con botellas, lámparas de luz cálida y una mesa alta.
"""

import barrio as b

PISO = (0.24, 0.18, 0.15)
LADRILLO = (0.46, 0.27, 0.21)
LUZ = (0.98, 0.78, 0.45)


def armar():
    b.sala(PISO, LADRILLO, zocalo=(0.18, 0.13, 0.11))
    # Repisas con botellas detrás de la barra, con luz cálida por abajo.
    colores = ((0.20, 0.36, 0.22), (0.50, 0.30, 0.12), (0.80, 0.80, 0.78), (0.30, 0.15, 0.12), (0.55, 0.45, 0.20))
    for fila, y in enumerate((1.35, 1.8)):
        b.caja("Repisa", (0, y, 2.6), (4.4, 0.04, 0.25), b.MADERA)
        b.caja("Luz de la repisa", (0, y - 0.03, 2.62), (4.3, 0.015, 0.1), b.mat("Luz del bar", LUZ, 0.3))
        for i in range(14):
            b.botella((-2.05 + i * 0.315, y + 0.02, 2.6), colores[(i + fila * 2) % 5])
    # La barra.
    b.caja("Barra", (0, 0.55, 1.7), (4.6, 1.1, 0.6), (0.32, 0.20, 0.13), bisel=0.01)
    b.caja("Cubierta de la barra", (0, 1.12, 1.65), (4.7, 0.05, 0.72), (0.40, 0.26, 0.16))
    b.caja("Reposapiés", (0, 0.2, 1.35), (4.4, 0.03, 0.03), (0.70, 0.62, 0.40))
    for i in range(6):
        x = -2.0 + i * 0.8
        b.cilindro("Piso de la barra", (x, 0.76, 1.15), 0.18, 0.05, (0.20, 0.14, 0.11), lados=14)
        b.cilindro("Pata del piso", (x, 0.38, 1.15), 0.025, 0.74, b.FIERRO, lados=6)
        b.cilindro("Aro del piso", (x, 0.28, 1.15), 0.14, 0.02, b.FIERRO, lados=12)
    for i in range(4):
        b.botella((-1.5 + i * 1.1, 1.145, 1.7), colores[i], alto=0.24)
    for x in (-1.6, 0, 1.6):
        b.lampara_colgante((x, 2.2, 1.7), color_luz=LUZ)
    # Mesa alta al lado.
    b.cilindro("Mesa alta", (2.5, 1.05, -0.2), 0.35, 0.04, b.MADERA, lados=18)
    b.cilindro("Pata", (2.5, 0.52, -0.2), 0.04, 1.02, b.FIERRO, lados=8)
    b.cilindro("Base", (2.5, 0.02, -0.2), 0.25, 0.03, b.FIERRO, lados=14)
    b.botella((2.45, 1.07, -0.25), colores[1], alto=0.24)
