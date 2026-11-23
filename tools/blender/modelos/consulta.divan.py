"""El diván de la consulta (consulta.divan): 1,80 m de cuero verde oscuro con cabecera inclinada y patas de madera."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    cuero = o._mat('cuero_divan', (0.25, 0.36, 0.31), 0.45)
    pata = o._mat('pata_sillon', (0.30, 0.20, 0.13), 0.5)
    c.caja('Base', (0, 0.28, 0), (1.8, 0.2, 0.8), cuero, bisel=0.03, segmentos=3)
    c.caja('Colchon', (0.1, 0.42, 0), (1.6, 0.1, 0.74), cuero, bisel=0.04, segmentos=3)
    cab = c.caja('Cabecera', (-0.78, 0.55, 0), (0.25, 0.12, 0.74), cuero, bisel=0.04, segmentos=3)
    c.girar_z(cab, 25)
    for dx in (-0.8, 0.8):
        for dz in (-0.34, 0.34):
            c.cilindro('Pata', (dx, 0.09, dz), 0.025, 0.18, pata, lados=10)
