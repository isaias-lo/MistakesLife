"""Frascos de remedios sobre la mesa (escena.casa.remedios): frascos blancos con tapa roja y etiqueta."""

import comun as c


def armar():
    blanco = c.material("frasco", (0.95, 0.95, 0.94), aspereza=0.35)
    rojo = c.material("tapa_remedio", (0.82, 0.20, 0.18), aspereza=0.4)
    etiqueta = c.material("etiqueta_remedio", (0.40, 0.62, 0.85), aspereza=0.5)
    for i in range(3):
        x, z = 0.45 + i * 0.12, -0.28
        alto = 0.1 - i * 0.012
        c.cilindro("Frasco", (x, 0.44 + alto / 2, z), 0.035, alto, blanco, lados=18)
        c.cilindro("Etiqueta", (x, 0.44 + alto / 2, z), 0.0355, alto * 0.45, etiqueta, lados=18)
        c.cilindro("Tapa", (x, 0.44 + alto + 0.01, z), 0.037, 0.02, rojo, lados=18)
