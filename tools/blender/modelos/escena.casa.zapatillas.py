"""Las zapatillas gastadas al lado de la puerta (escena.casa.zapatillas): un par de zapatillas
blancas con suela gris, puntera redonda y cordones.
"""

import comun as c


def armar():
    tela = c.material("zapatilla", (0.86, 0.86, 0.88), aspereza=0.7)
    suela = c.material("suela", (0.32, 0.32, 0.34), aspereza=0.8)
    cordon = c.material("cordon", (0.95, 0.95, 0.95), aspereza=0.8)
    for z in (-0.62, -0.76):
        c.caja("Suela", (0.32, 0.012, z), (0.28, 0.024, 0.105), suela, bisel=0.02, segmentos=3)
        c.caja("Capellada", (0.30, 0.06, z), (0.24, 0.08, 0.095), tela, bisel=0.02, segmentos=3)
        c.esfera("Puntera", (0.42, 0.04, z), 0.05, tela, escala=(1.0, 0.8, 0.95))
        c.caja("Cordones", (0.34, 0.1, z), (0.1, 0.006, 0.05), cordon)
