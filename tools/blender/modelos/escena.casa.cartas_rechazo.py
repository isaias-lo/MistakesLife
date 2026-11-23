"""Las respuestas de las postulaciones que no resultaron (escena.casa.cartas_rechazo): tres sobres
abiertos con la carta asomada; la de arriba con el timbre rojo de la empresa.
"""

import comun as c
import papeles as p


def carta(semilla, timbre):
    pix = p.hoja(200, 300, p.PAPEL, semilla)
    p.rect(pix, 0.06, 0.14, 0.06, 0.3, (0.25, 0.3, 0.4))
    p.renglones(pix, 0.25, 0.85, 0.08, 0.92, p.TINTA, semilla, paso=0.07, grueso=0.3)
    if timbre:
        p.sello(pix, 0.75, 0.78, 0.09, p.ROJO, semilla=semilla, opaco=0.8)
    return pix


def armar():
    sobre_mat = p.mat_hoja("sobre_rechazo", p.sobre(120, 200, (0.97, 0.97, 0.95), 5, abierto=True))
    for i in range(3):
        x, z = 0.48 + i * 0.02, -0.1 + i * 0.015
        abajo = p.MESA + i * 0.004
        giro = -14 + i * 13
        p.en_mesa("Sobre", sobre_mat, x, z, 0.2, 0.12, abajo=abajo, grueso=0.002, giro=giro)
        cx, _, cz = p.girar_punto((x - 0.04, 0, z + 0.03), (x, z), giro)
        p.en_mesa("Carta", p.mat_hoja("carta_rechazo_%d" % i, carta(40 + i, i == 2)), cx, cz, 0.2, 0.13, abajo=abajo + 0.002, grueso=0.001, giro=giro + 4)
