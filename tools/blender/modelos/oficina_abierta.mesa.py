"""Mesa compartida de la oficina abierta (oficina_abierta.mesa): 5,20 m con tres puestos, cada uno con pantalla, teclado y silla."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    o.mesa(0, 0, 5.2, 1.0)
    for i, x in enumerate((-1.7, 0.0, 1.7)):
        o.pantalla(x, 1.05, 0.3, 0.6, 0.36, apoyo=0.75)
        o.teclado(x, 0.75, -0.15)
        o.silla(x, -0.75)
    o.tazon(-1.2, 0.75, 0.0, (0.25, 0.40, 0.60))
    o.tazon(0.55, 0.75, -0.05)
    c.caja('Cuaderno', (1.2, 0.76, -0.1), (0.2, 0.012, 0.28), o._mat('cuaderno_oficina', (0.75, 0.25, 0.22), 0.5), bisel=0.002)
