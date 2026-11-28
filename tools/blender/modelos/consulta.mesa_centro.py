"""La mesa de centro de la consulta (consulta.mesa_centro): madera, con la caja de pañuelos y un vaso de agua."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    m = o.madera('mesa_centro', ((0.58, 0.42, 0.29), (0.46, 0.32, 0.21)), 93)
    c.uv_cubo(c.caja('Cubierta', (0, 0.47, 0), (0.8, 0.04, 0.5), m, bisel=0.006), escala=1.0)
    for dx in (-0.35, 0.35):
        for dz in (-0.2, 0.2):
            c.uv_cubo(c.caja('Pata', (dx, 0.225, dz), (0.04, 0.45, 0.04), m, bisel=0.004), escala=1.0)
    c.caja('Repisa', (0, 0.12, 0), (0.72, 0.02, 0.42), m)
    c.caja('Panuelos', (-0.1, 0.545, 0), (0.24, 0.11, 0.13), o._mat('caja_panuelos', (0.85, 0.88, 0.90), 0.5), bisel=0.005)
    c.caja('Panuelo', (-0.1, 0.61, 0), (0.06, 0.04, 0.05), o._mat('panuelo', (0.98, 0.98, 0.98), 0.9))
    c.cilindro('Vaso de agua', (0.2, 0.54, 0.05), 0.035, 0.1, o._mat('vaso_agua', (0.80, 0.88, 0.92), 0.05), lados=16)
