"""Cama de dos plazas de la pareja (mueble.cama_doble): 1,60 m de ancho, dos almohadas."""

import comun as c
import muebles


def armar():
    frazada = c.textura_tela("tela_plumon", (0.55, 0.47, 0.60))
    muebles.cama(0.3, 1.6, frazada, almohadas=2, semilla=9)
