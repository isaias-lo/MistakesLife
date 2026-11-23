"""Defendiste tu tesis (escena.casa.tesis_empastada): el ejemplar empastado en tapa dura azul
marino, con el título y tu nombre en letras doradas y el canto de las hojas a la vista.
"""

import comun as c
import papeles as p


def tapa():
    pix = p.hoja(220, 160, (0.12, 0.17, 0.36), 150, grano=0.04)
    oro = (0.85, 0.70, 0.32)
    p.renglones(pix, 0.18, 0.34, 0.15, 0.85, oro, 150, paso=0.06, grueso=0.4)
    p.rect(pix, 0.42, 0.425, 0.3, 0.7, oro)
    p.renglones(pix, 0.75, 0.88, 0.25, 0.75, oro, 151, paso=0.05, grueso=0.4)
    return pix


def armar():
    x, z = -0.38, -0.2
    azul = c.material("tapa_tesis", (0.12, 0.17, 0.36), aspereza=0.45)
    abajo = p.MESA
    c.caja("Tapa de abajo", (x, abajo + 0.0015, z), (0.16, 0.003, 0.22), azul, bisel=0.001)
    c.caja("Hojas", (x + 0.003, abajo + 0.0125, z), (0.152, 0.019, 0.212), c.material("canto_tesis", (0.95, 0.94, 0.90), aspereza=0.9))
    c.caja("Lomo", (x - 0.079, abajo + 0.0125, z), (0.006, 0.025, 0.22), azul, bisel=0.002)
    c.caja("Tapa de arriba", (x, abajo + 0.0235, z), (0.16, 0.003, 0.22), azul, bisel=0.001)
    p.en_mesa("Letras doradas", p.mat_hoja("tapa_tesis_letras", tapa(), aspereza=0.4), x, z, 0.158, 0.218, abajo=abajo + 0.025, grueso=0.0004)
