"""Tu oficina propia (oficina.rincon): vidrio a los dos lados del escritorio y una planta en la esquina."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    o.vidrio((-1.5, 0.6, 0), (0.05, 1.2, 1.8))
    o.vidrio((1.7, 0.6, 0), (0.05, 1.2, 1.8))
    o.planta(-2.1, 1.4)
