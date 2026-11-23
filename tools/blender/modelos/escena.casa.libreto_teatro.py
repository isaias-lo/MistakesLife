"""Vas al taller de teatro (escena.casa.libreto_teatro): el libreto de la obra en fotocopias
corcheteadas, con los nombres de los personajes y tu parte destacada con amarillo.
"""

import comun as c
import papeles as p


def libreto():
    pix = p.hoja(300, 215, (0.96, 0.96, 0.94), 140, grano=0.03)
    y, k = 0.08, 0
    while y < 0.92:
        p.rect(pix, y, y + 0.012, 0.3, 0.55, p.TINTA)
        alto = 0.05 + (k % 3) * 0.025
        if k % 3 == 1:
            p.rect(pix, y + 0.025, y + 0.02 + alto, 0.08, 0.92, (0.98, 0.88, 0.35))
        p.renglones(pix, y + 0.03, y + 0.02 + alto, 0.1, 0.9, p.TINTA, 140 + k, paso=0.025, grueso=0.4)
        y += alto + 0.05
        k += 1
    return pix


def armar():
    x, z, giro = -0.32, 0.26, 10
    o = c.caja("Hojas", (x, p.MESA + 0.004, z), (0.148, 0.008, 0.208), c.material("canto_libreto", (0.93, 0.93, 0.9), aspereza=0.9))
    c.girar_y(o, giro)
    p.en_mesa("Libreto", p.mat_hoja("libreto", libreto()), x, z, 0.15, 0.21, abajo=p.MESA + 0.008, grueso=0.001, giro=giro)
    metal = c.material("corchete", (0.7, 0.7, 0.73), aspereza=0.3)
    for dz in (-0.07, 0.07):
        cx, _, cz = p.girar_punto((x - 0.065, 0, z + dz), (x, z), giro)
        o = c.caja("Corchete", (cx, p.MESA + 0.0093, cz), (0.0015, 0.0006, 0.012), metal)
        c.girar_y(o, giro)
