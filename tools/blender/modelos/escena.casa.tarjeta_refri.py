"""La tarjeta de cumpleaños que le hiciste a tu mamá (escena.casa.tarjeta_refri): cartulina amarilla
doblada, dibujada a mano con un corazón y letras de plumón, pegada en la pared con cinta.
"""

import numpy as np

import comun as c
import papeles as p


def tarjeta():
    pix = p.hoja(200, 160, (0.98, 0.86, 0.38), 701, grano=0.03)
    alto, ancho = pix.shape[:2]
    yy, xx = np.indices((alto, ancho))
    u = (xx / ancho - 0.5) * 2.6
    v = -(yy / alto - 0.5) * 2.6 + 0.3
    corazon = (u ** 2 + v ** 2 - 1) ** 3 - u ** 2 * v ** 3 < 0
    pix[corazon, :3] = (0.85, 0.20, 0.25)
    p.renglones(pix, 0.08, 0.18, 0.12, 0.88, (0.25, 0.35, 0.75), 701, paso=0.06, grueso=0.4, mano=True)
    p.renglones(pix, 0.8, 0.92, 0.2, 0.8, (0.20, 0.55, 0.30), 702, paso=0.06, grueso=0.35, mano=True)
    return pix


def armar():
    y, z = 1.2, 0.05
    p.en_muro("Tarjeta para tu mamá", p.mat_hoja("tarjeta_mama", tarjeta(), aspereza=0.85), y, z, 0.2, 0.16, x=0.025, grueso=0.003, inclinado=5)
    c.caja("Cinta", (0.0295, y + 0.1, z), (0.001, 0.03, 0.08), c.material("cinta_papel", (0.91, 0.85, 0.66), aspereza=0.7))
