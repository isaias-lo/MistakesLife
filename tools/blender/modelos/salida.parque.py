"""Salir al parque (salida.parque): pasto, la laguna con su orilla de piedras, un sendero de
tierra, árboles grandes y un mantel de picnic con su canasto.
"""

import barrio as b

SENDERO = (0.72, 0.62, 0.46)


def armar():
    b.suelo_abierto((0.44, 0.57, 0.35))
    orilla = b.cilindro("Orilla", (1.4, 0.02, 1.3), 1.5, 0.06, (0.62, 0.60, 0.55), lados=32)
    orilla.scale = (1.4, 1.0, 1.0)
    agua = b.cilindro("Laguna", (1.4, 0.03, 1.3), 1.38, 0.06, b.mat("Agua", (0.38, 0.55, 0.66), 0.05), lados=32)
    agua.scale = (1.4, 1.0, 1.0)
    b.caja("Sendero", (0, 0.005, -1.1), (7.0, 0.02, 1.3), SENDERO)
    for i, (x, z, alto) in enumerate(((-2.8, 1.8, 3.4), (-1.2, 2.4, 3.0), (-3.0, -0.2, 2.8), (3.1, -0.4, 3.1))):
        b.arbol((x, 0, z), alto=alto, semilla=130 + i)
    # Picnic: mantel a cuadros (dos colores) y canasto.
    b.caja("Mantel", (-1.3, 0.01, 0.6), (1.3, 0.01, 1.0), (0.80, 0.30, 0.28))
    for i in range(3):
        b.caja("Cuadros del mantel", (-1.3, 0.016, 0.3 + i * 0.3), (1.3, 0.005, 0.1), (0.95, 0.93, 0.88))
    b.caja("Canasto", (-1.6, 0.14, 0.75), (0.45, 0.26, 0.3), (0.62, 0.46, 0.28), bisel=0.02)
    b.caja("Tapa del canasto", (-1.6, 0.28, 0.75), (0.47, 0.03, 0.32), (0.55, 0.40, 0.24))
    b.botella((-1.0, 0.015, 0.45), (0.30, 0.45, 0.25))
    b.banca((2.4, 0, -0.3), largo=1.4, giro=-20, escala=3.4)
