"""Estás en un taller de la carrera (escena.casa.post_its_proyecto): las tareas del proyecto en
post-its de colores pegados al muro en tres columnas, cada uno escrito a mano.
"""

import papeles as p

COLORES = [(0.98, 0.88, 0.35), (0.55, 0.82, 0.95), (0.95, 0.60, 0.70), (0.65, 0.90, 0.55)]


def nota(i):
    pix = p.hoja(64, 64, COLORES[i % 4], 400 + i, grano=0.02)
    p.renglones(pix, 0.15, 0.8, 0.1, 0.9, (0.15, 0.15, 0.2), 400 + i, paso=0.16, grueso=0.2, mano=True)
    return pix


def armar():
    for i in range(6):
        y = 1.05 + (i // 3) * 0.12
        z = -0.35 + (i % 3) * 0.12
        p.en_muro("Post-it", p.mat_hoja("post_it_%d" % i, nota(i), aspereza=0.8), y, z, 0.076, 0.076, x=0.025, grueso=0.0008, inclinado=-4 + i * 3)
