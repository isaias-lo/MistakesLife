"""Flores en el alféizar de la ventana (escena.casa.flores): un florero de vidrio con tallos y
flores de tres colores.
"""

import math

import comun as c


def armar():
    vidrio = c.material("florero", (0.45, 0.65, 0.80), aspereza=0.1)
    tallo = c.material("tallo", (0.28, 0.48, 0.24), aspereza=0.7)
    colores = [(0.92, 0.42, 0.52), (0.97, 0.83, 0.30), (0.86, 0.52, 0.82)]
    x, z = 0.0, -0.18
    c.cilindro("Florero", (x, 1.27, z), 0.06, 0.12, vidrio, lados=20)
    flores = [(-0.07, 1.47, -0.18), (0.08, 1.5, -0.2), (0.0, 1.56, -0.16)]
    for (fx, fy, fz), color in zip(flores, colores):
        mat = c.material("flor", color, aspereza=0.6)
        largo = fy - 1.3
        t = c.cilindro("Tallo", ((fx + x) / 2, 1.3 + largo / 2, (fz + z) / 2), 0.004, largo, tallo, lados=6)
        c.girar_z(t, math.degrees(math.atan2(-(fx - x), largo)))
        for i in range(6):
            a = math.radians(i * 60)
            c.esfera("Petalo", (fx + 0.03 * math.cos(a), fy, fz + 0.03 * math.sin(a)), 0.025, mat, escala=(1.2, 0.5, 1.2), lados=12)
        c.esfera("Centro", (fx, fy + 0.008, fz), 0.015, c.material("centro_flor", (0.9, 0.7, 0.2), aspereza=0.7), lados=12)
