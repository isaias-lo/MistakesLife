"""Alfombra de la casa amplia (mueble.alfombra): 2,40 x 1,60 m de lana tejida, con un borde de
otro color, apenas levantada del piso.
"""

import comun as c


def armar():
    lana = c.material("lana", (1, 1, 1), aspereza=1.0, textura=c.textura_tela("tela_alfombra", (0.66, 0.42, 0.36), semilla=41))
    borde = c.material("borde_alfombra", (1, 1, 1), aspereza=1.0, textura=c.textura_tela("tela_borde", (0.40, 0.24, 0.22), semilla=43))
    c.uv_cubo(c.caja("Borde", (0, 0.006, 0), (2.4, 0.012, 1.6), borde, bisel=0.004), escala=0.5)
    c.uv_cubo(c.caja("Centro", (0, 0.009, 0), (2.2, 0.012, 1.4), lana, bisel=0.003), escala=0.5)
