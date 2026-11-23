"""La cancha de fútbol 5 (mapa.cancha_futbol5, paquete de Argentina): pasto sintético con sus
líneas blancas, los dos arcos con red, la reja alta alrededor y los focos para jugar de noche.
"""

import math

import barrio as b

SINTETICO = (0.24, 0.52, 0.30)
LINEA = (0.95, 0.95, 0.93)


def armar():
    b.vereda()
    b.caja("Cancha", (0, 0.05, 0), (3.0, 0.02, 2.6), b.mat("Pasto sintético", SINTETICO, 0.9))
    largo, ancho = 2.6, 1.9
    for z in (-ancho / 2, ancho / 2):
        b.caja("Línea", (0, 0.065, z), (largo, 0.01, 0.025), LINEA)
    for x in (-largo / 2, 0, largo / 2):
        b.caja("Línea", (x, 0.065, 0), (0.025, 0.01, ancho), LINEA)
    for i in range(20):
        a = i / 20 * math.tau
        b.caja("Círculo", (math.cos(a) * 0.3, 0.065, math.sin(a) * 0.3), (0.025, 0.01, 0.1), LINEA).rotation_euler.z = a
    for x in (-largo / 2 + 0.3, largo / 2 - 0.3):
        b.caja("Área", (x, 0.065, 0), (0.025, 0.01, 0.9), LINEA)
    for x in (-largo / 2, largo / 2):
        b.caja("Área", ((x + (x - 0.3 * (1 if x > 0 else -1))) / 2, 0.065, 0.45), (0.3, 0.01, 0.025), LINEA)
        b.caja("Área", ((x + (x - 0.3 * (1 if x > 0 else -1))) / 2, 0.065, -0.45), (0.3, 0.01, 0.025), LINEA)
    # Arcos: dos palos, travesaño y red.
    for x, s in ((-largo / 2, -1), (largo / 2, 1)):
        for z in (-0.3, 0.3):
            b.cilindro("Palo", (x, 0.2, z), 0.015, 0.34, LINEA, lados=6)
        b.cilindro("Travesaño", (x, 0.37, 0), 0.015, 0.62, LINEA, eje="z", lados=6)
        b.caja("Red", (x + s * 0.14, 0.2, 0), (0.01, 0.34, 0.6), b.mat("Red", (0.85, 0.85, 0.85), 0.9))
        b.caja("Red de arriba", (x + s * 0.07, 0.37, 0), (0.14, 0.01, 0.6), b.mat("Red", (0.85, 0.85, 0.85), 0.9))
    # Reja alta: postes y barras (la malla se insinúa con barras finas).
    gris = (0.50, 0.52, 0.55)
    for x in (-1.45, 1.45):
        for i in range(6):
            b.cilindro("Poste de la reja", (x, 0.45, -1.25 + i * 0.5), 0.015, 0.8, gris, lados=6)
        b.caja("Barra", (x, 0.84, 0), (0.015, 0.015, 2.5), gris)
    for z in (-1.25, 1.25):
        for i in range(7):
            b.cilindro("Poste de la reja", (-1.45 + i * 0.483, 0.45, z), 0.015, 0.8, gris, lados=6)
        b.caja("Barra", (0, 0.84, z), (2.9, 0.015, 0.015), gris)
    # Focos en las esquinas.
    for x in (-1.45, 1.45):
        for z in (-1.25, 1.25):
            b.cilindro("Torre del foco", (x, 0.75, z), 0.02, 1.4, b.FIERRO, lados=6)
            b.caja("Foco", (x * 0.97, 1.47, z * 0.97), (0.14, 0.08, 0.06), (0.92, 0.92, 0.85))
