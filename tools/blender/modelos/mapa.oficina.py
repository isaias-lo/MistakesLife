"""Tu pega en el mapa (mapa.oficina): un edificio de oficinas de seis pisos con muro cortina de
vidrio, losas marcadas, el hall de entrada y una plazoleta con jardineras al frente.
"""

import barrio as b

LOSA = (0.86, 0.86, 0.84)
VIDRIO = (0.40, 0.52, 0.62)


def armar():
    b.vereda()
    alto_piso = 0.52
    pisos = 6
    ancho, fondo, z0 = 2.3, 1.8, 0.45
    b.caja("Muro cortina", (0, alto_piso * pisos / 2 + 0.06, z0), (ancho - 0.06, alto_piso * pisos, fondo - 0.06),
           b.mat("Muro cortina", VIDRIO, 0.08))
    for i in range(pisos + 1):
        b.caja("Losa", (0, 0.06 + i * alto_piso, z0), (ancho, 0.06, fondo), LOSA)
    frente = z0 - fondo / 2
    for i in range(7):
        x = -ancho / 2 + 0.03 + i * (ancho - 0.06) / 6
        b.caja("Montante", (x, alto_piso * pisos / 2 + 0.06, frente - 0.01), (0.03, alto_piso * pisos, 0.03), (0.30, 0.32, 0.34))
    for x in (-ancho / 2 + 0.03, ancho / 2 - 0.03):
        for z in (z0 - fondo / 2 + 0.3, z0, z0 + fondo / 2 - 0.3):
            b.caja("Montante", (x + (-0.01 if x < 0 else 0.01), alto_piso * pisos / 2 + 0.06, z), (0.03, alto_piso * pisos, 0.03), (0.30, 0.32, 0.34))
    b.caja("Sala de máquinas", (0.4, alto_piso * pisos + 0.2, z0 + 0.3), (0.8, 0.3, 0.6), LOSA)
    b.caja("Hall", (0, 0.32, frente - 0.12), (0.9, 0.5, 0.22), b.mat("Vidrio del hall", (0.48, 0.60, 0.70), 0.08))
    b.caja("Alero del hall", (0, 0.6, frente - 0.2), (1.1, 0.05, 0.45), LOSA)
    # Plazoleta con jardineras.
    b.caja("Plazoleta", (0, 0.05, -1.05), (3.0, 0.02, 0.9), (0.66, 0.64, 0.60))
    for x in (-1.0, 1.0):
        b.caja("Jardinera", (x, 0.1, -1.0), (0.6, 0.1, 0.35), (0.55, 0.54, 0.52))
        b.pasto((x, 0.155, -1.0), (0.54, 0.01, 0.29))
        b.arbol((x, 0.15, -1.0), alto=0.8, semilla=70 + int(x > 0))
    b.banca((0, 0.06, -1.2), largo=0.6, giro=180)
