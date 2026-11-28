"""Tu cubículo de esquina (oficina_cubiculos.rincon): paneles de vidrio arriba y una planta afuera."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    o.vidrio((0, 1.6, 0), (2.2, 0.6, 0.05))
    o.vidrio((-1.1, 1.6, -0.65), (0.05, 0.6, 1.3))
    o.planta(-1.7, -2.15)
