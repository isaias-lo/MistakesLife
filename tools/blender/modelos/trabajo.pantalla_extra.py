"""Tu segunda pantalla (trabajo.pantalla_extra): monitor con su brazo apoyado en la mesa."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    o.pantalla(0, 0, 0, 0.6, 0.36, apoyo=-0.3)
