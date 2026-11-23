"""Cama de una plaza y media de adulto que vive solo (mueble.cama_adulto): cubrecama lisa."""

import comun as c
import muebles


def armar():
    frazada = c.textura_tela("tela_cubrecama", (0.52, 0.44, 0.58))
    muebles.cama(0.3, 0.9, frazada, almohadas=1, semilla=5)
