"""Ganaste el centro de alumnos (escena.casa.acta_centro): el libro de actas de tapa dura verde,
con su etiqueta escrita a mano, y el timbre de madera del centro al lado.
"""

import comun as c
import papeles as p


def etiqueta():
    pix = p.hoja(96, 192, p.PAPEL, 4)
    p.borde(pix, 0.06, 0.94, 0.04, 0.96, (0.25, 0.45, 0.35), 3)
    p.renglones(pix, 0.25, 0.8, 0.12, 0.88, p.AZUL_LAPIZ, 4, paso=0.22, grueso=0.18, mano=True)
    return pix


def armar():
    verde = c.material("tapa_actas", (0.16, 0.36, 0.27), aspereza=0.55)
    hojas = c.material("hojas_actas", (0.93, 0.91, 0.84), aspereza=0.9)
    x, z, abajo = 0.32, 0.2, p.MESA
    c.caja("Tapa de abajo", (x, abajo + 0.0015, z), (0.16, 0.003, 0.22), verde, bisel=0.001)
    c.caja("Hojas", (x + 0.003, abajo + 0.0075, z), (0.15, 0.009, 0.21), hojas)
    c.caja("Tapa de arriba", (x, abajo + 0.0135, z), (0.16, 0.003, 0.22), verde, bisel=0.001)
    c.caja("Lomo", (x - 0.079, abajo + 0.0075, z), (0.006, 0.015, 0.22), verde, bisel=0.002)
    p.en_mesa("Etiqueta", p.mat_hoja("etiqueta_actas", etiqueta()), x + 0.01, z + 0.02, 0.1, 0.05, abajo=abajo + 0.015, grueso=0.001)

    madera = c.material("mango_timbre", (0.38, 0.24, 0.14), aspereza=0.45)
    goma = c.material("goma_timbre", (0.12, 0.12, 0.14), aspereza=0.8)
    tx, tz = 0.45, 0.12
    c.caja("Goma del timbre", (tx, abajo + 0.003, tz), (0.045, 0.006, 0.045), goma, bisel=0.001)
    c.caja("Base del timbre", (tx, abajo + 0.011, tz), (0.048, 0.01, 0.048), madera, bisel=0.002)
    c.cilindro("Mango del timbre", (tx, abajo + 0.032, tz), 0.01, 0.032, madera, lados=14)
    c.esfera("Perilla del timbre", (tx, abajo + 0.053, tz), 0.017, madera, escala=(1, 0.8, 1))
