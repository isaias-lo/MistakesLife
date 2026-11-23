"""El afiche de tu lista al centro de alumnos (escena.casa.afiche_lista): cartulina blanca hecha a
mano con plumones, la franja verde de la lista con su número, y cinta de papel arriba.
"""

import comun as c
import papeles as p


def afiche():
    pix = p.hoja(360, 260, (0.97, 0.97, 0.95), 7)
    verde = (0.18, 0.52, 0.33)
    p.rect(pix, 0.06, 0.3, 0.06, 0.94, verde)
    p.numero(pix, "2", 0.09, 0.4, 0.18, (0.97, 0.97, 0.95))
    p.renglones(pix, 0.38, 0.52, 0.1, 0.9, (0.12, 0.12, 0.18), 7, paso=0.07, grueso=0.5, mano=True)
    p.renglones(pix, 0.58, 0.86, 0.14, 0.86, verde, 8, paso=0.06, grueso=0.3, mano=True)
    p.circulo_a_mano(pix, 0.92, 0.5, 0.035, 0.3, verde, grueso=0.3)
    return pix


def armar():
    y, z = 0.62, 0.3
    p.en_muro("Afiche", p.mat_hoja("afiche_lista", afiche(), aspereza=0.8), y, z, 0.36, 0.26, x=0.025, grueso=0.003)
    cinta = c.material("cinta_papel", (0.91, 0.85, 0.66), aspereza=0.7)
    for dz in (-0.1, 0.1):
        o = c.caja("Cinta", (0.03, y + 0.175, z + dz), (0.002, 0.04, 0.06), cinta)
        c.girar_x(o, 20 if dz > 0 else -20)
