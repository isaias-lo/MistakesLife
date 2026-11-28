"""Tu rincón en la oficina abierta (oficina_abierta.rincon): vidrio esmerilado entre las filas y una planta."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    o.vidrio((0, 0.9, 0), (0.05, 1.8, 0.9))
    o.planta(-0.9, 0)
