"""Un cubículo (oficina_cubiculos.cubiculo): paneles de tela gris azulado, escritorio, pantalla y papeles."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    tela = o.tela_panel()
    alu = o._mat('perfil_aluminio', (0.30, 0.32, 0.36), 0.35)
    c.uv_cubo(c.caja('Panel', (0, 0.65, 0.85), (2.2, 1.3, 0.06), tela, bisel=0.01), escala=0.8)
    c.uv_cubo(c.caja('Panel', (-1.1, 0.65, 0.2), (0.06, 1.3, 1.3), tela, bisel=0.01), escala=0.8)
    c.caja('Remate', (0, 1.31, 0.85), (2.22, 0.02, 0.07), alu)
    c.caja('Remate', (-1.1, 1.31, 0.2), (0.07, 0.02, 1.32), alu)
    o.mesa(0, 0.5, 2.0, 0.6)
    o.pantalla(0.4, 1.0, 0.68, 0.56, 0.34, apoyo=0.75)
    o.teclado(0.4, 0.75, 0.4)
    c.caja('Papeles', (-0.5, 0.78, 0.45), (0.3, 0.06, 0.24), o._mat('papeles_oficina', (0.96, 0.96, 0.94), 0.8))
    c.caja('Bandeja', (-0.5, 0.76, 0.45), (0.34, 0.02, 0.28), o._mat('bandeja', (0.15, 0.15, 0.17), 0.4))
