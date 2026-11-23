"""Aportas a la casa de tu familia (escena.casa.sobre_aporte): el sobre café con tu parte del
sueldo, con los billetes asomados y una nota escrita a mano.
"""

import comun as c
import papeles as p


def sobre():
    pix = p.hoja(110, 220, (0.86, 0.74, 0.55), 601, grano=0.04)
    p.renglones(pix, 0.35, 0.6, 0.25, 0.75, (0.2, 0.2, 0.3), 601, paso=0.12, grueso=0.35, mano=True)
    return pix


def billete(color, semilla):
    pix = p.hoja(60, 140, color, semilla, grano=0.05)
    p.rect(pix, 0.1, 0.9, 0.06, 0.3, tuple(v * 0.8 for v in color))
    p.borde(pix, 0.05, 0.95, 0.03, 0.97, tuple(v * 0.75 for v in color), 2)
    return pix


def armar():
    x, z, giro = 0.62, -0.38, 6
    p.en_mesa("Sobre del aporte", p.mat_hoja("sobre_aporte", sobre()), x, z, 0.22, 0.11, grueso=0.004, giro=giro)
    for k, (color, dx) in enumerate((((0.55, 0.70, 0.85), -0.01), ((0.60, 0.78, 0.55), 0.01))):
        bx, _, bz = p.girar_punto((x + dx - 0.04, 0, z + 0.035 + k * 0.006), (x, z), giro)
        p.en_mesa("Billete", p.mat_hoja("billete_%d" % k, billete(color, 610 + k), aspereza=0.6), bx, bz, 0.15, 0.065, abajo=p.MESA + 0.004 + k * 0.0006, grueso=0.0005, giro=giro + 3 - k * 6)
