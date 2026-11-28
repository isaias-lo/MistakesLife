"""Los archivadores de la oficina de cubículos (trabajo.archivadores): tres metálicos en fila."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    for i in range(3):
        o.archivador(i * 0.55)
