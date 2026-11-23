"""La mecedora de los años de vejez (escena.casa.mecedora): mecedora de madera con asiento de
mimbre, respaldo de listones, apoyabrazos y balancines curvos.
"""

import math

import comun as c


def armar():
    veta = c.textura_madera("veta_mecedora", claro=(0.58, 0.40, 0.25), oscuro=(0.42, 0.28, 0.17), semilla=61, vertical=True)
    madera = c.material("madera_mecedora", (1, 1, 1), aspereza=0.5, textura=veta)
    mimbre = c.material("mimbre", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_mimbre", (0.80, 0.68, 0.48), semilla=63))

    c.uv_cubo(c.caja("Asiento", (0, 0.45, 0), (0.56, 0.05, 0.54), mimbre, bisel=0.012), escala=0.5)
    for dx in (-0.27, 0.27):
        for dz in (-0.22, 0.22):
            c.uv_cubo(c.caja("Pata", (dx, 0.25, dz), (0.04, 0.42, 0.04), madera, bisel=0.005), escala=1.0)
        c.uv_cubo(c.caja("Apoyabrazos", (dx, 0.68, -0.02), (0.05, 0.03, 0.5), madera, bisel=0.008), escala=1.0)
        c.uv_cubo(c.caja("Soporte del brazo", (dx, 0.57, -0.22), (0.03, 0.22, 0.03), madera), escala=1.0)
        # Balancín curvo: tramos cortos que siguen un arco.
        for i in range(9):
            t = -0.4 + i * 0.1
            y = 0.03 + 1.2 * (1 - math.cos(t)) * 0.5
            b = c.caja("Balancin", (dx, y, t), (0.04, 0.035, 0.105), madera)
            c.girar_x(b, -math.degrees(t) * 0.6)
    # Respaldo de listones, echado hacia atrás.
    for i in range(5):
        dx = -0.2 + i * 0.1
        l = c.caja("Liston", (dx, 0.85, 0.3), (0.06, 0.75, 0.025), madera, bisel=0.006)
        c.uv_cubo(l, escala=1.0)
        c.girar_x(l, -10)
    c.uv_cubo(c.caja("Travesano", (0, 1.2, 0.36), (0.6, 0.08, 0.04), madera, bisel=0.01), escala=1.0)
