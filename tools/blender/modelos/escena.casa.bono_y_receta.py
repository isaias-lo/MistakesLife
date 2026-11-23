"""Fuiste al médico (escena.casa.bono_y_receta): el bono de atención celeste, la receta escrita
a mano con su timbre, y la caja del remedio.
"""

import comun as c
import papeles as p


def bono():
    pix = p.hoja(120, 216, (0.82, 0.91, 0.96), 2)
    p.rect(pix, 0.06, 0.22, 0.04, 0.5, (0.22, 0.42, 0.62))
    p.renglones(pix, 0.32, 0.9, 0.05, 0.95, (0.2, 0.25, 0.35), 2, paso=0.12, grueso=0.3)
    p.rect(pix, 0.06, 0.22, 0.7, 0.96, (0.95, 0.97, 0.98))
    return pix


def receta():
    pix = p.hoja(200, 150, p.PAPEL, 3)
    p.renglones(pix, 0.05, 0.16, 0.08, 0.6, (0.25, 0.3, 0.45), 3, paso=0.05, grueso=0.35)
    p.rect(pix, 0.2, 0.205, 0.06, 0.94, (0.6, 0.65, 0.75))
    p.renglones(pix, 0.28, 0.62, 0.1, 0.9, p.AZUL_LAPIZ, 4, paso=0.07, grueso=0.25, mano=True)
    p.firma(pix, 0.82, 0.55, 0.4, semilla=3)
    p.sello(pix, 0.8, 0.25, 0.14, (0.25, 0.35, 0.65), semilla=3, opaco=0.6)
    return pix


def caja_remedio():
    pix = p.hoja(64, 128, (0.97, 0.97, 0.97), 5, grano=0.0)
    p.rect(pix, 0.0, 0.3, 0.0, 1.0, (0.25, 0.55, 0.80))
    p.renglones(pix, 0.45, 0.85, 0.08, 0.7, (0.2, 0.2, 0.25), 5, paso=0.16, grueso=0.4)
    return pix


def armar():
    abajo = p.MESA
    p.en_mesa("Bono", p.mat_hoja("bono", bono()), -0.12, 0.2, 0.18, 0.1, abajo=abajo, grueso=0.0012, giro=-10)
    p.en_mesa("Receta", p.mat_hoja("receta", receta()), -0.08, 0.27, 0.12, 0.16, abajo=abajo + 0.0012, grueso=0.0012, giro=14)
    blanco = c.material("caja_remedio", (0.97, 0.97, 0.97), aspereza=0.5)
    c.caja("Caja del remedio", (0.04, abajo + 0.0225, 0.22), (0.1, 0.045, 0.06), blanco, bisel=0.0015)
    p.en_mesa("Cara de la caja", p.mat_hoja("tapa_caja_remedio", caja_remedio(), aspereza=0.5), 0.04, 0.22, 0.098, 0.058, abajo=abajo + 0.045, grueso=0.0006)
