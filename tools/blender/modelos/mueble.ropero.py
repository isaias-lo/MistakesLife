"""Ropero de dos puertas de la pieza arrendada (mueble.ropero): 0,90 x 1,80 x 0,50 m, las puertas
miran a la pieza (-z), con zócalo, cornisa y tiradores metálicos.
"""

import comun as c

ANCHO, ALTO, FONDO = 0.9, 1.8, 0.5


def armar():
    veta = c.textura_madera("veta_ropero", claro=(0.62, 0.47, 0.32), oscuro=(0.47, 0.34, 0.22), semilla=13, vertical=True)
    madera = c.material("madera_ropero", (1, 1, 1), aspereza=0.55, textura=veta)
    oscura = c.material("zocalo_ropero", (0.36, 0.25, 0.16), aspereza=0.6)
    metal = c.material("tirador", (0.75, 0.74, 0.70), aspereza=0.3)

    c.caja("Zocalo", (0, 0.04, 0.01), (ANCHO - 0.02, 0.08, FONDO - 0.04), oscura, bisel=0.004)
    cuerpo = c.caja("Cuerpo", (0, 0.08 + (ALTO - 0.12) / 2, 0), (ANCHO, ALTO - 0.12, FONDO), madera, bisel=0.006)
    c.uv_cubo(cuerpo, escala=1.8)
    c.uv_cubo(c.caja("Cornisa", (0, ALTO - 0.02, -0.005), (ANCHO + 0.03, 0.04, FONDO + 0.02), madera, bisel=0.006), escala=1.8)
    alto_puerta = ALTO - 0.2
    for lado in (-1, 1):
        x = lado * ANCHO / 4
        p = c.caja("Puerta", (x, 0.08 + 0.02 + alto_puerta / 2, -FONDO / 2 - 0.008), (ANCHO / 2 - 0.012, alto_puerta, 0.018), madera, bisel=0.004)
        c.uv_cubo(p, escala=1.8)
        c.cilindro("Tirador", (-lado * 0.035, 1.0, -FONDO / 2 - 0.03), 0.007, 0.16, metal, eje="y", lados=10)
