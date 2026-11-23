"""Renunciaste: la caja de cartón con tus cosas de la pega junto a la puerta
(escena.casa.caja_pega), con la taza y la carpeta asomando.
"""

import comun as c


def armar():
    carton = c.material("carton", (0.70, 0.53, 0.34), aspereza=0.9)
    carton_oscuro = c.material("carton_oscuro", (0.60, 0.44, 0.28), aspereza=0.9)
    taza = c.material("taza", (0.95, 0.94, 0.92), aspereza=0.3)
    carpeta = c.material("carpeta", (0.20, 0.35, 0.60), aspereza=0.6)
    x, z = 0.62, 0.3
    c.caja("Fondo", (x, 0.005, z), (0.4, 0.01, 0.3), carton_oscuro)
    for dx in (-0.195, 0.195):
        c.caja("Lado", (x + dx, 0.14, z), (0.01, 0.28, 0.3), carton)
    for dz in (-0.145, 0.145):
        c.caja("Lado", (x, 0.14, z + dz), (0.4, 0.28, 0.01), carton)
    # Solapas abiertas.
    for dz, giro in ((-0.2, -35), (0.2, 35)):
        s = c.caja("Solapa", (x, 0.3, z + dz), (0.38, 0.008, 0.12), carton)
        c.girar_x(s, giro)
    c.cilindro("Taza", (x - 0.07, 0.29, z - 0.04), 0.04, 0.1, taza, lados=16)
    c.caja("Carpeta", (x + 0.08, 0.3, z + 0.04), (0.03, 0.24, 0.22), carpeta, bisel=0.004)
    c.caja("Papeles", (x + 0.03, 0.26, z + 0.02), (0.05, 0.22, 0.2), c.material("papeles", (0.95, 0.94, 0.90), aspereza=0.9))
