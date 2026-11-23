"""Aceptaste ser padrino o madrina (escena.casa.foto_ahijado): la foto del bautizo en el muro
lateral, tú con la guagua de blanco en brazos, delante de la pila de la iglesia.
"""

import fotos as f


def imagen():
    pix = f.foto(300, 240, (0.88, 0.85, 0.78), (0.70, 0.62, 0.52), horizonte=0.75, semilla=51)
    pix[20:220, 30:80, :3] = (0.80, 0.76, 0.68)
    pix[20:220, 160:210, :3] = (0.80, 0.76, 0.68)
    f.persona(pix, 0.5, 1.08, 0.9, (0.30, 0.32, 0.40), f.PIELES[1], f.PELOS[0])
    f.guagua(pix, 0.72, 0.52, 0.16, f.PIELES[0])
    return pix


def armar():
    f.marco_lado("foto_ahijado", imagen(), 1.7, 0.45, 0.36, 0.3, madera=(0.82, 0.78, 0.70))
