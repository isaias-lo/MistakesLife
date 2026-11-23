"""El contrato de arriendo que firmaste (escena.casa.contrato_arriendo): tres hojas corcheteadas con
sus cláusulas, tu firma en lápiz azul y la banderita amarilla de "firme aquí".
"""

import comun as c
import papeles as p


def hoja_contrato(semilla, con_firmas):
    pix = p.hoja(300, 220, (0.97, 0.97, 0.95), semilla)
    if not con_firmas:
        p.rect(pix, 0.05, 0.08, 0.3, 0.7, (0.15, 0.15, 0.18))
    p.renglones(pix, 0.13, 0.7 if con_firmas else 0.95, 0.08, 0.92, p.TINTA, semilla, paso=0.03, grueso=0.35)
    if con_firmas:
        for cx in (0.28, 0.72):
            p.rect(pix, 0.86, 0.864, cx - 0.17, cx + 0.17, p.TINTA)
        p.firma(pix, 0.82, 0.28, 0.28, p.AZUL_LAPIZ, semilla)
        p.firma(pix, 0.82, 0.72, 0.25, (0.1, 0.1, 0.15), semilla + 7)
    return pix


def armar():
    x, z, giro = -0.1, -0.02, 8
    for i in range(3):
        mat = p.mat_hoja("contrato_%d" % i, hoja_contrato(60 + i, i == 0))
        dx, _, dz = p.girar_punto((x + 0.004 * (2 - i), 0, z - 0.004 * (2 - i)), (x, z), 0)
        p.en_mesa("Hoja del contrato", mat, dx, dz, 0.22, 0.3, abajo=p.MESA + i * 0.0012, grueso=0.0012, giro=giro + (2 - i) * 1.5)
    metal = c.material("corchete", (0.7, 0.7, 0.73), aspereza=0.3)
    cx, _, cz = p.girar_punto((x + 0.09, 0, z - 0.13), (x, z), giro)
    o = c.caja("Corchete", (cx, p.MESA + 0.0042, cz), (0.012, 0.001, 0.0015), metal)
    c.girar_y(o, giro + 45)
    fx, _, fz = p.girar_punto((x + 0.12, 0, z + 0.1), (x, z), giro)
    o = c.caja("Banderita firme aqui", (fx, p.MESA + 0.0042, fz), (0.05, 0.0006, 0.014), c.material("banderita", (0.98, 0.84, 0.25), aspereza=0.6))
    c.girar_y(o, giro)
    p.lapiz(0.05, -0.05, -20, color=(0.18, 0.28, 0.60), nombre="Lapiz pasta")
