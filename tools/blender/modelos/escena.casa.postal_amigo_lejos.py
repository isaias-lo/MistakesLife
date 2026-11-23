"""La postal que te mandó tu amigo que se fue lejos (escena.casa.postal_amigo_lejos): foto de un
paisaje de montaña y lago, con su borde blanco, clavada al muro con una chinche roja.
"""

import comun as c
import fotos as f
import papeles as p


def postal():
    pix = f.foto(140, 200, (0.55, 0.75, 0.92), (0.35, 0.55, 0.65), horizonte=0.6, semilla=71)
    f._elipse(pix, 0.62, 0.3, 0.3, 0.35, (0.45, 0.50, 0.58))
    f._elipse(pix, 0.65, 0.72, 0.35, 0.3, (0.40, 0.46, 0.55))
    f._elipse(pix, 0.38, 0.72, 0.07, 0.08, (0.96, 0.97, 0.98))
    pix[118:, :, :3] = (0.40, 0.55, 0.35)
    f.revelar(pix, 1)
    pix[:6, :, :3] = 0.97
    pix[-6:, :, :3] = 0.97
    pix[:, :6, :3] = 0.97
    pix[:, -6:, :3] = 0.97
    return pix


def armar():
    y, z = 1.25, 0.45
    p.en_muro("Postal", p.mat_hoja("postal", postal(), aspereza=0.4), y, z, 0.14, 0.2, x=0.025, grueso=0.0015, inclinado=-6)
    roja = c.material("chinche", (0.80, 0.18, 0.16), aspereza=0.3)
    c.cilindro("Chinche", (0.032, y + 0.055, z), 0.009, 0.006, roja, eje="x", lados=16)
    c.esfera("Cabeza de la chinche", (0.037, y + 0.055, z), 0.007, roja)
