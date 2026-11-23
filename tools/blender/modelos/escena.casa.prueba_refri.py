"""La prueba con buena nota que dejaste pegada (escena.casa.prueba_refri): la hoja de la prueba
con tu nombre, las respuestas y el 6,8 en rojo arriba, sujeta con un imán azul.
"""

import comun as c
import papeles as p


def prueba():
    pix = p.hoja(240, 170, (0.97, 0.97, 0.95), 81)
    p.borde(pix, 0.04, 0.16, 0.05, 0.6, (0.3, 0.3, 0.35), 1)
    p.renglones(pix, 0.07, 0.14, 0.08, 0.55, p.AZUL_LAPIZ, 81, paso=0.035, grueso=0.4, mano=True)
    p.numero(pix, "6,8", 0.04, 0.68, 0.1, p.ROJO)
    p.circulo_a_mano(pix, 0.09, 0.81, 0.08, 0.17, p.ROJO, grueso=0.1)
    for k in range(5):
        y = 0.22 + k * 0.15
        p.renglones(pix, y, y + 0.04, 0.08, 0.92, p.TINTA, 82 + k, paso=0.03, grueso=0.35)
        p.renglones(pix, y + 0.05, y + 0.11, 0.12, 0.85, p.AZUL_LAPIZ, 90 + k, paso=0.03, grueso=0.3, mano=True)
    return pix


def armar():
    y, z = 1.0, 0.25
    p.en_muro("Prueba", p.mat_hoja("prueba", prueba()), y, z, 0.24, 0.17, x=0.025, grueso=0.0012, inclinado=-4)
    c.cilindro("Iman", (0.033, y + 0.105, z - 0.05), 0.016, 0.01, c.material("iman", (0.22, 0.42, 0.72), aspereza=0.35), eje="x", lados=20)
