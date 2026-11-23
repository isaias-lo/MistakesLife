"""Te embargaron (escena.casa.sello_embargo): los autoadhesivos del receptor judicial pegados en la
tele, con la franja roja, el número de la causa y la firma.
"""

import comun as c
import papeles as p


def etiqueta(semilla):
    pix = p.hoja(100, 200, (0.97, 0.97, 0.95), semilla)
    p.rect(pix, 0.0, 0.3, 0.0, 1.0, p.ROJO)
    p.renglones(pix, 0.08, 0.22, 0.2, 0.8, (0.98, 0.95, 0.95), semilla, paso=0.12, grueso=0.6)
    p.renglones(pix, 0.4, 0.75, 0.06, 0.94, p.TINTA, semilla + 1, paso=0.12, grueso=0.35)
    p.firma(pix, 0.85, 0.7, 0.35, (0.15, 0.15, 0.25), semilla)
    return pix


def armar():
    for k, (x, y) in enumerate(((-0.35, 0.18), (0.3, -0.2))):
        o = c.caja("Etiqueta de embargo", (x, y, -0.04), (0.2, 0.1, 0.0008), p.mat_hoja("embargo_%d" % k, etiqueta(500 + k)))
        c.uv_plano_frente(o)
        if k == 1:
            c.girar_z(o, 4)
