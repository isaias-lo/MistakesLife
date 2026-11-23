"""El comprobante del crédito de estudios (escena.casa.comprobante_cae): la hoja con la franja azul
del banco, la tabla de cuotas y un clip metálico.
"""

import comun as c
import papeles as p


def comprobante():
    pix = p.hoja(270, 210, (0.95, 0.94, 0.89), 13)
    p.rect(pix, 0.0, 0.12, 0.0, 1.0, (0.2, 0.35, 0.6))
    p.renglones(pix, 0.03, 0.09, 0.06, 0.5, (0.9, 0.92, 0.96), 13, paso=0.03, grueso=0.5)
    p.renglones(pix, 0.18, 0.35, 0.08, 0.92, p.TINTA, 14, paso=0.04, grueso=0.3)
    for f in range(8):
        y = 0.42 + f * 0.06
        p.rect(pix, y, y + 0.003, 0.08, 0.92, (0.65, 0.68, 0.75))
        p.renglones(pix, y + 0.02, y + 0.045, 0.1, 0.4, p.TINTA, 20 + f, paso=0.03, grueso=0.4)
        p.renglones(pix, y + 0.02, y + 0.045, 0.7, 0.9, p.TINTA, 30 + f, paso=0.03, grueso=0.4)
    return pix


def armar():
    x, z = 0.05, -0.32
    p.en_mesa("Comprobante", p.mat_hoja("comprobante_cae", comprobante()), x, z, 0.16, 0.21, grueso=0.002)
    metal = c.material("clip", (0.72, 0.72, 0.75), aspereza=0.25)
    for dz, largo in ((0.0, 0.05), (0.003, 0.04)):
        for dx in (-0.006, 0.006):
            c.cilindro("Clip", (x - 0.05 + dx * (1 - dz * 100), p.MESA + 0.003 + dz, z + 0.08 - 0.025 + largo / 2), 0.0012, largo, metal, eje="z", lados=6)
