"""Los folletos de dónde estudiar que te pasó tu profe jefe (escena.casa.folletos_estudios): tres
dípticos de universidades, institutos y CFT, abiertos en abanico, con foto del campus y sus colores.
"""

import papeles as p

COLORES = [(0.80, 0.28, 0.24), (0.22, 0.42, 0.72), (0.92, 0.70, 0.22)]


def folleto(i):
    pix = p.hoja(280, 200, (0.97, 0.97, 0.96), 120 + i, grano=0.005)
    color = COLORES[i]
    p.rect(pix, 0.0, 0.18, 0.0, 1.0, color)
    p.renglones(pix, 0.05, 0.13, 0.08, 0.7, (0.98, 0.98, 0.98), 120 + i, paso=0.04, grueso=0.6)
    # Foto del campus: cielo, edificio y pasto.
    p.rect(pix, 0.22, 0.52, 0.06, 0.94, (0.66, 0.80, 0.92))
    p.rect(pix, 0.3, 0.46, 0.2, 0.8, (0.78, 0.72, 0.64))
    for k in range(5):
        p.rect(pix, 0.34, 0.39, 0.25 + k * 0.11, 0.3 + k * 0.11, (0.35, 0.45, 0.55))
    p.rect(pix, 0.46, 0.52, 0.06, 0.94, (0.45, 0.62, 0.35))
    p.renglones(pix, 0.58, 0.92, 0.08, 0.92, p.TINTA, 130 + i, paso=0.045, grueso=0.3)
    p.rect(pix, 0.94, 1.0, 0.0, 1.0, color)
    return pix


def armar():
    for i in range(3):
        x, z = -0.1 + i * 0.06, 0.12
        p.en_mesa("Folleto", p.mat_hoja("folleto_%d" % i, folleto(i), aspereza=0.45), x, z, 0.14, 0.2, abajo=p.MESA + i * 0.0015, grueso=0.0015, giro=-20 + i * 20)
