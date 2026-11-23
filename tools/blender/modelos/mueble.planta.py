"""Planta de interior (mueble.planta) en macetero de greda: un ficus de hojas grandes, como los
que se compran en un vivero. Mismo espacio que la forma simple (macetero de 0,40 m y follaje
hasta 1,15 m). Las hojas salen de una semilla fija: siempre la misma planta.
"""

import math

import numpy as np

import comun as c


def armar():
    greda = c.material("greda", (0.66, 0.40, 0.27), aspereza=0.85)
    tierra = c.material("tierra", (0.24, 0.17, 0.12), aspereza=1.0)
    tallo = c.material("tallo", (0.36, 0.27, 0.18), aspereza=0.8)
    hojas = [c.material("hoja", (0.20, 0.42, 0.22), aspereza=0.45), c.material("hoja_clara", (0.28, 0.50, 0.26), aspereza=0.45)]

    maceta = c.cilindro("Macetero", (0, 0.2, 0), 0.19, 0.4, greda, lados=24)
    maceta.scale = (1.0, 1.0, 1.0)
    c.cilindro("Borde del macetero", (0, 0.39, 0), 0.205, 0.04, greda, lados=24)
    c.cilindro("Tierra", (0, 0.38, 0), 0.18, 0.02, tierra, lados=24)
    c.cilindro("Tronco", (0, 0.7, 0), 0.018, 0.65, tallo, lados=8)

    rng = np.random.default_rng(31)
    for i in range(46):
        alto = 0.55 + rng.random() * 0.6
        angulo = rng.random() * 360
        radio = 0.06 + (1.15 - alto) * 0.35 + rng.random() * 0.08
        x = radio * math.cos(math.radians(angulo))
        z = radio * math.sin(math.radians(angulo))
        hoja = c.caja("Hoja", (x, alto, z), (0.2, 0.012, 0.09), hojas[i % 2], bisel=0.03, segmentos=3)
        hoja.rotation_euler = (math.radians(rng.random() * 50 - 25), math.radians(rng.random() * 40 - 20), math.radians(-angulo))
