"""Tienes una causa abierta (escena.casa.carta_tribunal): el sobre del tribunal recién pasado bajo
la puerta, con su timbre azul y la dirección escrita a máquina.
"""

import papeles as p


def sobre():
    pix = p.sobre(170, 260, (0.95, 0.94, 0.90), 14)
    p.sello(pix, 0.22, 0.8, 0.1, (0.20, 0.30, 0.60), semilla=14, opaco=0.75)
    p.rect(pix, 0.06, 0.09, 0.06, 0.42, (0.2, 0.3, 0.55))
    return pix


def armar():
    p.en_mesa("Carta del tribunal", p.mat_hoja("carta_tribunal", sobre()), 0.38, -0.1, 0.26, 0.17, abajo=0.0, grueso=0.003, giro=18)
