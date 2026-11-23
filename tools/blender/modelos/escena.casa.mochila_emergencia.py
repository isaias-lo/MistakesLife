"""La mochila de emergencia junto a la puerta (escena.casa.mochila_emergencia): naranja, con
franja reflectante y una linterna encima.
"""

import comun as c


def armar():
    naranja = c.material("naranja", (1, 1, 1), aspereza=0.85, textura=c.textura_tela("tela_naranja", (0.92, 0.48, 0.14), semilla=89))
    reflectante = c.material("reflectante", (0.85, 0.88, 0.90), aspereza=0.2)
    negro = c.material("linterna", (0.18, 0.18, 0.20), aspereza=0.4)
    c.uv_cubo(c.caja("Mochila", (0.35, 0.22, 0.65), (0.28, 0.44, 0.38), naranja, bisel=0.06, segmentos=4), escala=0.5)
    c.caja("Franja", (0.495, 0.28, 0.65), (0.01, 0.05, 0.34), reflectante)
    c.cilindro("Linterna", (0.35, 0.47, 0.6), 0.025, 0.16, negro, eje="z", lados=14)
    c.cilindro("Vidrio", (0.35, 0.47, 0.515), 0.03, 0.012, c.material("vidrio_linterna", (0.9, 0.9, 0.8), aspereza=0.1), eje="z", lados=14)
