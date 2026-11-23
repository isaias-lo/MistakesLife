"""Cama de una plaza de la pieza de niño o adolescente (mueble.cama): frazada a rayas."""

import comun as c
import muebles


def armar():
    frazada = c.textura_tela("tela_frazada", (0.30, 0.42, 0.62), rayas=((0.85, 0.82, 0.74), 36))
    muebles.cama(0.0, 0.9, frazada, almohadas=1)
