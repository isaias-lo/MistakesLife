"""Escritorio de la oficina común (oficina.escritorio): 2,00 x 0,90 m con pantalla, teclado, papeles y silla."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    o.mesa(0, 0, 2.0, 0.9)
    o.pantalla(0, 1.05, 0.3, 0.6, 0.36, apoyo=0.75)
    o.teclado(0, 0.75, -0.1)
    o.silla(0, -0.8)
    c.caja('Papeles', (-0.65, 0.765, 0.05), (0.3, 0.03, 0.22), o._mat('papeles_oficina', (0.96, 0.96, 0.94), 0.8))
    o.tazon(0.6, 0.75, 0.1)
