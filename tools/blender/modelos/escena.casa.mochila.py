"""La mochila de estudiante en el suelo (escena.casa.mochila): mochila de tela con bolsillo,
cierre, asa y tirantes, apoyada contra el escritorio.
"""

import comun as c


def armar():
    tela = c.material("tela_mochila", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_mochila", (0.22, 0.40, 0.66), semilla=51))
    oscuro = c.material("tela_oscura", (0.16, 0.26, 0.42), aspereza=0.9)
    cierre = c.material("cierre", (0.12, 0.12, 0.12), aspereza=0.5)
    cuerpo = c.caja("Cuerpo", (0, 0.22, 0), (0.36, 0.44, 0.2), tela, bisel=0.07, segmentos=4)
    c.uv_cubo(cuerpo, escala=0.5)
    c.caja("Bolsillo", (0, 0.15, -0.11), (0.28, 0.2, 0.06), oscuro, bisel=0.03, segmentos=3)
    c.caja("Cierre", (0, 0.24, -0.14), (0.24, 0.008, 0.01), cierre)
    c.caja("Asa", (0, 0.45, 0.03), (0.08, 0.03, 0.02), oscuro, bisel=0.008)
    for dx in (-0.09, 0.09):
        t = c.caja("Tirante", (dx, 0.24, 0.115), (0.05, 0.38, 0.025), oscuro, bisel=0.01)
        c.girar_x(t, 6)
