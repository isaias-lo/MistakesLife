"""La credencial de la U, del CFT o del instituto (escena.casa.credencial_u): la tarjeta con tu foto
en su funda plástica, con el cordón azul enrollado sobre la mesa.
"""

import comun as c
import papeles as p


def tarjeta():
    pix = p.hoja(90, 140, (0.97, 0.97, 0.96), 2, grano=0.0)
    p.rect(pix, 0.0, 0.2, 0.0, 1.0, (0.20, 0.30, 0.55))
    p.rect(pix, 0.3, 0.85, 0.06, 0.36, (0.55, 0.45, 0.38))
    p.rect(pix, 0.38, 0.6, 0.14, 0.28, (0.80, 0.66, 0.55))
    p.renglones(pix, 0.34, 0.8, 0.44, 0.94, p.TINTA, 2, paso=0.12, grueso=0.35)
    return pix


def armar():
    x, z = -0.15, -0.3
    c.caja("Funda", (x, p.MESA + 0.0015, z), (0.145, 0.003, 0.095), c.material("funda", (0.85, 0.88, 0.90), aspereza=0.15), bisel=0.001)
    p.en_mesa("Credencial", p.mat_hoja("credencial_u", tarjeta(), aspereza=0.4), x, z, 0.135, 0.085, abajo=p.MESA + 0.003, grueso=0.0008)
    azul = c.material("cordon_azul", (0.20, 0.30, 0.55), aspereza=0.8)
    p.cinta_cerrada("Cordon", (x + 0.01, z + 0.12), 0.13, 0.07, 0.015, azul)
    metal = c.material("mosqueton", (0.7, 0.7, 0.72), aspereza=0.3)
    c.caja("Mosqueton", (x + 0.01, p.MESA + 0.003, z + 0.055), (0.012, 0.006, 0.025), metal, bisel=0.002)
