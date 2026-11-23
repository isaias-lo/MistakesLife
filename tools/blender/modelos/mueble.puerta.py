"""Puerta de entrada (mueble.puerta), en el muro lateral izquierdo: hoja de 0,90 x 2,00 m de madera
con marco, molduras y manilla. La cara de la hoja mira a la pieza (+x).
"""

import comun as c


def armar():
    veta = c.textura_madera("veta_puerta", claro=(0.44, 0.31, 0.20), oscuro=(0.32, 0.22, 0.14), semilla=19, vertical=True)
    madera = c.material("madera_puerta", (1, 1, 1), aspereza=0.55, textura=veta)
    marco = c.material("marco_puerta", (0.88, 0.86, 0.82), aspereza=0.6)
    metal = c.material("manilla", (0.72, 0.70, 0.64), aspereza=0.25)

    # Marco blanco alrededor.
    c.caja("Marco arriba", (0.02, 2.04, 0), (0.05, 0.08, 1.06), marco, bisel=0.004)
    for z in (-0.49, 0.49):
        c.caja("Marco lado", (0.02, 1.02, z), (0.05, 2.04, 0.08), marco, bisel=0.004)
    hoja = c.caja("Hoja", (0.015, 1.0, 0), (0.04, 2.0, 0.9), madera, bisel=0.004)
    c.uv_cubo(hoja, escala=2.2)
    # Dos tableros con moldura.
    for y, alto in ((1.45, 0.8), (0.55, 0.8)):
        c.uv_cubo(c.caja("Tablero", (0.04, y, 0), (0.012, alto, 0.62), madera, bisel=0.006), escala=2.2)
    # Manilla y chapa.
    c.caja("Roseta", (0.042, 1.0, -0.36), (0.008, 0.16, 0.04), metal, bisel=0.002)
    c.cilindro("Manilla", (0.07, 1.04, -0.33), 0.008, 0.11, metal, eje="z", lados=10)
