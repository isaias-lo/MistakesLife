"""El delantal cuadrillé del jardín infantil colgado junto a la puerta (escena.casa.delantal_jardin)."""

import numpy as np

import comun as c
import muebles


def cuadrille():
    pix = np.ones((128, 128, 4))
    cuadro = ((np.indices((128, 128)) // 16).sum(axis=0) % 2) == 0
    pix[cuadro, :3] = (0.45, 0.62, 0.85)
    pix[~cuadro, :3] = (0.96, 0.96, 0.96)
    return c.imagen("cuadrille", pix)


def armar():
    z = 0.95
    muebles.gancho(z)
    tela = c.material("cuadrille", (1, 1, 1), aspereza=0.9, textura=cuadrille())
    d = c.caja("Delantal", (0.045, 1.15, z), (0.01, 0.34, 0.26), tela, bisel=0.006)
    c.uv_cubo(d, escala=0.1)
    for lado in (-1, 1):
        m = c.caja("Manga", (0.045, 1.26, z + lado * 0.16), (0.01, 0.1, 0.08), tela)
        c.uv_cubo(m, escala=0.1)
        c.girar_x(m, lado * 25)
