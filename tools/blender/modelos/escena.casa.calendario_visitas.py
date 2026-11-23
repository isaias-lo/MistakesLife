"""Tus hijos viven con su otro papá o mamá (escena.casa.calendario_visitas): el calendario de pared
con la foto del mes arriba y los días de visita encerrados en rojo, colgado de un clavo.
"""

import comun as c
import papeles as p


def calendario():
    pix = p.hoja(320, 260, (0.97, 0.97, 0.95), 9)
    # La foto del mes: un cerro con cielo.
    p.rect(pix, 0.05, 0.27, 0.05, 0.95, (0.62, 0.78, 0.90))
    p.rect(pix, 0.2, 0.27, 0.05, 0.95, (0.42, 0.55, 0.35))
    p.rect(pix, 0.16, 0.2, 0.3, 0.75, (0.48, 0.58, 0.40))
    p.rect(pix, 0.31, 0.35, 0.3, 0.7, (0.25, 0.25, 0.3))
    gris = (0.62, 0.62, 0.66)
    filas, cols = 5, 7
    x0, x1, y0, y1 = 0.05, 0.95, 0.4, 0.96
    for f in range(filas + 1):
        y = y0 + (y1 - y0) * f / filas
        p.rect(pix, y, y + 0.004, x0, x1, gris)
    for k in range(cols + 1):
        x = x0 + (x1 - x0) * k / cols
        p.rect(pix, y0, y1, x, x + 0.004, gris)
    dia = 1
    for f in range(filas):
        for k in range(cols):
            if f == 0 and k < 2 or dia > 30:
                continue
            yy = y0 + (y1 - y0) * f / filas + 0.015
            xx = x0 + (x1 - x0) * k / cols + 0.015
            p.numero(pix, str(dia), yy, xx, 0.028, (0.3, 0.3, 0.34))
            if dia in (6, 7, 20, 21):
                p.circulo_a_mano(pix, yy + 0.04, xx + 0.04, 0.05, 0.06, p.ROJO, grueso=0.18)
            dia += 1
    return pix


def armar():
    y, z = 1.05, 0.0
    p.en_muro("Calendario", p.mat_hoja("calendario", calendario(), aspereza=0.6), y, z, 0.32, 0.26, x=0.025, grueso=0.003)
    metal = c.material("espiral_calendario", (0.7, 0.7, 0.72), aspereza=0.3)
    c.caja("Espiral", (0.031, y + 0.155, z), (0.006, 0.008, 0.24), metal)
    c.cilindro("Clavo", (0.035, y + 0.185, z), 0.003, 0.02, metal, eje="x", lados=8)
    hilo = c.material("colgador", (0.3, 0.3, 0.32), aspereza=0.5)
    for lado in (-1, 1):
        o = c.caja("Colgador", (0.031, y + 0.17, z + lado * 0.012), (0.002, 0.035, 0.002), hilo)
        c.girar_x(o, -lado * 35)
