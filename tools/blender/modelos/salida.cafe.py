"""Tomar un café (salida.cafe): el local por dentro, con piso de madera, el mesón con la máquina de
espresso y la vitrina, repisas con tazas, lámparas colgantes y una mesa para dos con sus cafés.
"""

import barrio as b

PISO = (0.56, 0.42, 0.30)
MURO = (0.90, 0.86, 0.78)
VERDE = (0.20, 0.38, 0.30)


def armar():
    b.sala(PISO, MURO, zocalo=(0.35, 0.26, 0.19))
    # Azulejos detrás del mesón.
    b.caja("Azulejos", (-1.2, 1.2, 2.73), (3.6, 1.2, 0.02), (0.93, 0.93, 0.90))
    # Mesón con cubierta de madera y frente verde.
    b.caja("Mesón", (-1.2, 0.5, 2.1), (3.2, 1.0, 0.6), VERDE, bisel=0.01)
    b.caja("Cubierta del mesón", (-1.2, 1.02, 2.1), (3.3, 0.05, 0.66), b.MADERA)
    b.caja("Máquina de espresso", (-1.9, 1.25, 2.2), (0.55, 0.4, 0.4), (0.72, 0.72, 0.74), bisel=0.02)
    b.caja("Vitrina", (-0.4, 1.2, 2.1), (0.9, 0.32, 0.5), b.mat("Vidrio", b.VIDRIO, 0.1))
    for x in (-0.65, -0.4, -0.15):
        b.esfera("Queque", (x, 1.12, 2.1), 0.07, b.mat("Queque", (0.72, 0.48, 0.26), 0.8), escala=(1, 0.6, 1), detalle=1)
    # Repisas con tazas.
    for y in (1.75, 2.15):
        b.caja("Repisa", (-1.2, y, 2.62), (2.4, 0.04, 0.22), b.MADERA)
        for i in range(8):
            b.taza((-2.2 + i * 0.28, y + 0.02, 2.62))
    for x in (-2.0, -0.6):
        b.lampara_colgante((x, 2.0, 1.9))
    # La mesa para dos, detrás de ustedes.
    b.mesa_redonda((1.4, 0, 0.6))
    b.silla((0.95, 0, 0.6), giro=-90)
    b.silla((1.85, 0, 0.6), giro=90)
    b.taza((1.3, 0.74, 0.55))
    b.taza((1.5, 0.74, 0.68))
    b.lampara_colgante((1.4, 2.0, 0.6))
    # Una planta grande en la esquina.
    b.cilindro("Macetero", (2.9, 0.25, 2.3), 0.22, 0.5, (0.85, 0.83, 0.78), lados=16)
    for i, (dx, dy) in enumerate(((0, 0.75), (0.12, 0.95), (-0.1, 1.05))):
        b.esfera("Planta", (2.9 + dx, dy, 2.3), 0.28, b.mat("Planta", b.HOJAS[1], 0.9), detalle=1)
