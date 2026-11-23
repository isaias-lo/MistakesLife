"""Diste la PAES (escena.casa.resultados_paes): la hoja de resultados impresa pegada al muro, con
la tabla de puntajes por prueba y el mejor puntaje destacado.
"""

import comun as c
import papeles as p


def resultados():
    pix = p.hoja(260, 190, (0.97, 0.97, 0.95), 91)
    p.rect(pix, 0.03, 0.1, 0.06, 0.94, (0.25, 0.30, 0.45))
    p.renglones(pix, 0.14, 0.24, 0.08, 0.7, p.TINTA, 91, paso=0.04, grueso=0.35)
    puntajes = ["652", "718", "590", "684"]
    for k, puntaje in enumerate(puntajes):
        y = 0.32 + k * 0.12
        p.borde(pix, y, y + 0.12, 0.06, 0.94, (0.55, 0.58, 0.65), 1)
        p.renglones(pix, y + 0.04, y + 0.08, 0.1, 0.5, p.TINTA, 92 + k, paso=0.03, grueso=0.5)
        if k == 1:
            p.rect(pix, y + 0.01, y + 0.11, 0.62, 0.92, (0.98, 0.90, 0.45))
        p.numero(pix, puntaje, y + 0.025, 0.66, 0.07, (0.15, 0.18, 0.3))
    p.renglones(pix, 0.84, 0.95, 0.08, 0.92, (0.45, 0.45, 0.5), 99, paso=0.03, grueso=0.35)
    return pix


def armar():
    y, z = 1.35, 0.02
    p.en_muro("Resultados de la PAES", p.mat_hoja("resultados_paes", resultados()), y, z, 0.26, 0.19, x=0.025, grueso=0.0012, inclinado=3)
    cinta = c.material("cinta_scotch", (0.92, 0.90, 0.82), aspereza=0.3)
    for dz in (-0.08, 0.08):
        c.caja("Cinta", (0.0275, y + 0.125, z + dz), (0.001, 0.025, 0.045), cinta)
