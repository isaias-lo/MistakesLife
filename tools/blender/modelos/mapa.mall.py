"""El mall del barrio (mapa.mall): un edificio grande y bajo de hormigón claro, con la entrada de
vidrio al medio, la franja del cine arriba y el estacionamiento al frente con autos.
"""

import barrio as b

HORMIGON = (0.84, 0.84, 0.82)
FRANJA_CINE = (0.15, 0.22, 0.38)
ASFALTO = (0.36, 0.36, 0.38)
LINEA = (0.90, 0.90, 0.88)


def armar():
    b.vereda()
    b.caja("Mall", (0, 0.8, 0.55), (3.1, 1.5, 1.9), HORMIGON, bisel=0.01)
    b.caja("Cornisa", (0, 1.58, 0.55), (3.15, 0.06, 1.95), (0.72, 0.72, 0.70))
    frente = 0.55 - 0.95
    # Entrada de vidrio, más alta que el resto.
    b.caja("Entrada", (0, 0.85, frente - 0.08), (1.0, 1.6, 0.25), b.mat("Vidrio del mall", (0.42, 0.55, 0.66), 0.1))
    for x in (-0.5, -0.25, 0, 0.25, 0.5):
        b.caja("Montante", (x, 0.85, frente - 0.21), (0.03, 1.6, 0.03), b.FIERRO)
    b.caja("Alero", (0, 0.5, frente - 0.3), (1.3, 0.04, 0.4), (0.70, 0.70, 0.68))
    # La franja del cine (sin letras: el nombre lo dice la pantalla del juego).
    b.caja("Franja del cine", (-0.95, 1.2, frente - 0.02), (0.9, 0.35, 0.04), FRANJA_CINE)
    b.caja("Luz del cine", (-0.95, 1.2, frente - 0.045), (0.75, 0.06, 0.02), b.mat("Luz cálida", (0.98, 0.86, 0.55), 0.3))
    for x in (0.85, 1.25):
        b.ventana((x, 1.15, frente), 0.3, 0.35)
    # Estacionamiento con líneas y autos.
    b.caja("Estacionamiento", (0, 0.05, -1.05), (3.1, 0.02, 0.85), ASFALTO)
    for x in (-1.3, -0.85, -0.4, 0.45, 0.9, 1.35):
        b.caja("Línea", (x, 0.065, -1.05), (0.025, 0.01, 0.6), LINEA)
    b.auto((-1.075, 0.06, -1.05), (0.80, 0.80, 0.82), giro=90)
    b.auto((0.675, 0.06, -1.05), (0.20, 0.30, 0.45), giro=90)
    b.auto((-0.625, 0.06, -1.05), (0.25, 0.25, 0.27), giro=90)
