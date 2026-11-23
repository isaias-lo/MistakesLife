"""Escritorio de la pieza de estudiante (mueble.escritorio): 1,20 x 0,60 m con cubierta a 0,80,
cajonera a un lado y la espalda contra el muro (+z).
"""

import comun as c

ANCHO, ALTO, FONDO = 1.2, 0.8, 0.6


def armar():
    veta = c.textura_madera("veta_escritorio", claro=(0.70, 0.56, 0.40), oscuro=(0.56, 0.43, 0.30), semilla=17)
    madera = c.material("madera_escritorio", (1, 1, 1), aspereza=0.5, textura=veta)
    blanco = c.material("cajonera", (0.90, 0.89, 0.86), aspereza=0.6)
    metal = c.material("tirador", (0.70, 0.70, 0.68), aspereza=0.3)

    c.uv_cubo(c.caja("Cubierta", (0, ALTO - 0.015, 0), (ANCHO, 0.03, FONDO), madera, bisel=0.005), escala=1.4)
    # Cajonera de tres cajones a la derecha (vista desde la pieza).
    ancho_caj = 0.4
    x_caj = -(ANCHO / 2 - ancho_caj / 2 - 0.01)
    alto_caj = ALTO - 0.03
    c.caja("Cajonera", (x_caj, alto_caj / 2, 0.0), (ancho_caj, alto_caj, FONDO - 0.04), blanco, bisel=0.004)
    for i in range(3):
        y = 0.13 + i * 0.25
        c.caja("Frente del cajon", (x_caj, y, -FONDO / 2 + 0.012), (ancho_caj - 0.03, 0.22, 0.012), blanco, bisel=0.003)
        c.caja("Tirador", (x_caj, y + 0.06, -FONDO / 2 + 0.002), (0.12, 0.012, 0.012), metal, bisel=0.002)
    # Lado izquierdo: panel lateral y travesaño trasero.
    c.uv_cubo(c.caja("Lateral", (ANCHO / 2 - 0.015, alto_caj / 2, 0), (0.03, alto_caj, FONDO - 0.04), madera, bisel=0.003), escala=1.4)
    c.uv_cubo(c.caja("Faldon trasero", (0, ALTO - 0.12, FONDO / 2 - 0.03), (ANCHO - 0.04, 0.18, 0.02), madera), escala=1.4)
