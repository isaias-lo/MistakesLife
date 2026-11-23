"""Las cosas de tu compañero de casa junto a la puerta (escena.casa.cosas_companero): su mochila
verde y sus zapatillas blancas.
"""

import comun as c


def armar():
    verde = c.material("mochila_verde", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_verde", (0.24, 0.50, 0.36), semilla=85))
    oscuro = c.material("verde_oscuro", (0.17, 0.36, 0.26), aspereza=0.9)
    blanco = c.material("zapatilla_blanca", (0.92, 0.92, 0.95), aspereza=0.7)
    suela = c.material("suela_blanca", (0.80, 0.80, 0.78), aspereza=0.8)
    c.uv_cubo(c.caja("Mochila", (0.12, 0.2, -0.62), (0.28, 0.4, 0.2), verde, bisel=0.06, segmentos=4), escala=0.5)
    c.caja("Bolsillo", (0.12, 0.13, -0.73), (0.22, 0.16, 0.05), oscuro, bisel=0.02)
    for z in (-0.58, -0.71):
        c.caja("Suela", (0.5, 0.012, z), (0.26, 0.024, 0.1), suela, bisel=0.01)
        c.caja("Zapatilla", (0.49, 0.055, z), (0.22, 0.07, 0.09), blanco, bisel=0.02, segmentos=3)
        c.esfera("Puntera", (0.59, 0.04, z), 0.045, blanco, escala=(1.0, 0.8, 0.95))
