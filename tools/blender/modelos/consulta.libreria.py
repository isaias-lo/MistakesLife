"""La librería de la consulta (consulta.libreria): 1,80 x 2,00 m de madera, con libros de psicología y adornos."""

import comun as c  # noqa: F401
import oficinas as o
import papeles as p  # noqa: F401


def armar():
    import numpy as np
    m = o.madera('libreria', ((0.55, 0.40, 0.27), (0.44, 0.31, 0.20)), 95)
    c.uv_cubo(c.caja('Fondo', (0, 1.0, 0.18), (1.8, 2.0, 0.02), m), escala=1.4)
    for dx in (-0.89, 0.89):
        c.uv_cubo(c.caja('Lado', (dx, 1.0, 0), (0.03, 2.0, 0.4), m, bisel=0.003), escala=1.4)
    for k in range(6):
        c.uv_cubo(c.caja('Repisa', (0, 0.03 + k * 0.39, 0), (1.78, 0.025, 0.38), m, bisel=0.002), escala=1.4)
    rng = np.random.default_rng(96)
    colores = [(0.55, 0.20, 0.18), (0.20, 0.30, 0.48), (0.25, 0.40, 0.30), (0.80, 0.76, 0.66), (0.30, 0.28, 0.32), (0.62, 0.48, 0.25)]
    for k in range(5):
        y0 = 0.045 + k * 0.39
        x = -0.85
        fin = 0.85 - (0.5 if k % 2 else 0.15)
        while x < fin:
            g = 0.025 + rng.random() * 0.035
            h = 0.2 + rng.random() * 0.1
            c.caja('Libro', (x + g / 2, y0 + h / 2, -0.02), (g, h, 0.24 + rng.random() * 0.06), o._mat('libro_%d' % int(rng.integers(6)), colores[int(rng.integers(6))], 0.6))
            x += g + 0.003
        if k % 2:
            c.esfera('Adorno', (0.6, y0 + 0.08, -0.02), 0.08, o._mat('adorno', (0.85, 0.82, 0.75), 0.3), escala=(1, 1, 1))
