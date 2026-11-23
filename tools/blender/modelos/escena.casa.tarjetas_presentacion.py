"""Gente interesante que conociste (escena.casa.tarjetas_presentacion): un par de tarjetas de
presentación sobre la mesa, cada una con el logo de su empresa, nombre, cargo y datos.
"""

import papeles as p


def tarjeta(fondo, logo, semilla):
    pix = p.hoja(110, 180, fondo, semilla, grano=0.01)
    p.rect(pix, 0.15, 0.4, 0.07, 0.22, logo)
    p.renglones(pix, 0.2, 0.32, 0.3, 0.85, (0.15, 0.15, 0.2), semilla, paso=0.12, grueso=0.6)
    p.renglones(pix, 0.38, 0.44, 0.3, 0.7, (0.45, 0.45, 0.5), semilla + 1, paso=0.06, grueso=0.5)
    p.renglones(pix, 0.65, 0.9, 0.07, 0.7, (0.35, 0.35, 0.4), semilla + 2, paso=0.08, grueso=0.4)
    return pix


def armar():
    p.en_mesa("Tarjeta de presentación", p.mat_hoja("tarjeta_presentacion_1", tarjeta((0.97, 0.95, 0.90), (0.20, 0.45, 0.40), 801), aspereza=0.6), -0.30, 0.10, 0.09, 0.055, grueso=0.0008, giro=18)
    p.en_mesa("Otra tarjeta", p.mat_hoja("tarjeta_presentacion_2", tarjeta((0.84, 0.88, 0.93), (0.75, 0.35, 0.20), 811), aspereza=0.6), -0.26, 0.14, 0.09, 0.055, abajo=p.MESA + 0.0008, grueso=0.0008, giro=-10)
