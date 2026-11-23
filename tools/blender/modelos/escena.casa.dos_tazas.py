"""Dos tazas en la mesa, una de cada uno (escena.casa.dos_tazas): tazas de loza con asa."""

import comun as c


def taza(x, z, color, nombre):
    loza = c.material(nombre, color, aspereza=0.25)
    cafe = c.material("cafe", (0.22, 0.13, 0.08), aspereza=0.2)
    c.cilindro("Taza", (x, 0.485, z), 0.04, 0.09, loza, lados=24)
    c.cilindro("Cafe", (x, 0.525, z), 0.036, 0.004, cafe, lados=24)
    asa = c.caja("Asa", (x + 0.05, 0.49, z), (0.02, 0.05, 0.012), loza, bisel=0.006)
    return asa


def armar():
    taza(0.32, -0.18, (0.82, 0.30, 0.25), "tu_taza")
    taza(0.42, -0.12, (0.25, 0.45, 0.75), "taza_pareja")
