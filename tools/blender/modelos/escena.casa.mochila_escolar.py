"""La mochila del colegio de tus hijos junto a la puerta (escena.casa.mochila_escolar): roja,
chica, con bolsillo amarillo y asa.
"""

import comun as c


def armar():
    roja = c.material("tela_roja", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_roja", (0.80, 0.22, 0.26), semilla=53))
    amarilla = c.material("bolsillo_amarillo", (0.94, 0.78, 0.24), aspereza=0.8)
    negro = c.material("asa_negra", (0.18, 0.18, 0.2), aspereza=0.7)
    x, z = 0.3, 0.2
    c.uv_cubo(c.caja("Cuerpo", (x, 0.16, z), (0.16, 0.32, 0.27), roja, bisel=0.05, segmentos=4), escala=0.5)
    c.caja("Bolsillo", (x + 0.09, 0.11, z), (0.04, 0.14, 0.19), amarilla, bisel=0.015, segmentos=3)
    c.caja("Asa", (x, 0.33, z), (0.03, 0.025, 0.1), negro, bisel=0.008)
