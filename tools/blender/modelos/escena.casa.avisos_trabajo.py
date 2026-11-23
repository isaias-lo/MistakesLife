"""Buscas pega en el diario (escena.casa.avisos_trabajo): el diario doblado en la página de avisos
clasificados, con un aviso encerrado con plumón rojo.
"""

import comun as c
import papeles as p


def avisos():
    pix = p.hoja(360, 500, (0.86, 0.85, 0.81), 12, grano=0.03)
    gris = (0.35, 0.35, 0.36)
    p.rect(pix, 0.03, 0.1, 0.04, 0.96, (0.18, 0.18, 0.2))
    for col in range(5):
        x0 = 0.04 + col * 0.186
        y = 0.14
        k = 0
        while y < 0.94:
            alto = 0.08 + ((col * 7 + k * 3) % 5) * 0.025
            p.rect(pix, y, y + 0.018, x0, x0 + 0.17, (0.15, 0.15, 0.16))
            p.renglones(pix, y + 0.03, min(y + alto, 0.95), x0, x0 + 0.17, gris, col * 31 + k, paso=0.022, grueso=0.45)
            y += alto + 0.025
            k += 1
        if col < 4:
            p.rect(pix, 0.14, 0.95, x0 + 0.178, x0 + 0.18, (0.6, 0.6, 0.6))
    p.circulo_a_mano(pix, 0.36, 0.33, 0.11, 0.1, p.ROJO, grueso=0.1)
    return pix


def armar():
    x, z, abajo = -0.3, 0.25, p.MESA
    gris = c.material("canto_diario", (0.84, 0.83, 0.79), aspereza=0.95)
    c.caja("Diario doblado", (x, abajo + 0.005, z), (0.5, 0.01, 0.36), gris, bisel=0.002)
    p.en_mesa("Avisos", p.mat_hoja("avisos", avisos(), aspereza=0.95), x, z, 0.5, 0.36, abajo=abajo + 0.01, grueso=0.001)
    o = c.cilindro("Plumon", (x + 0.18, abajo + 0.018, z - 0.05), 0.007, 0.13, c.material("plumon_rojo", (0.75, 0.15, 0.14), aspereza=0.4), eje="x", lados=12)
    c.girar_y(o, -35)
