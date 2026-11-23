"""Quedaste en el Registro de Deudores de Pensiones de Alimentos (escena.casa.certificado_deudor):
el certificado oficial con su encabezado, la tabla de la deuda y el timbre rojo.
"""

import papeles as p


def certificado():
    pix = p.hoja(300, 220, (0.97, 0.97, 0.95), 8)
    p.rect(pix, 0.04, 0.1, 0.06, 0.3, (0.2, 0.3, 0.5))
    p.renglones(pix, 0.05, 0.1, 0.35, 0.9, p.TINTA, 8, paso=0.025, grueso=0.4)
    p.rect(pix, 0.15, 0.18, 0.2, 0.8, (0.15, 0.15, 0.18))
    p.renglones(pix, 0.24, 0.45, 0.08, 0.92, p.TINTA, 9, paso=0.04, grueso=0.3)
    for f in range(4):
        y = 0.5 + f * 0.06
        p.borde(pix, y, y + 0.06, 0.08, 0.92, (0.5, 0.5, 0.55), 1)
        p.renglones(pix, y + 0.02, y + 0.05, 0.1, 0.5, p.TINTA, 10 + f, paso=0.03, grueso=0.4)
    p.sello(pix, 0.85, 0.72, 0.14, p.ROJO, semilla=8, opaco=0.85)
    p.firma(pix, 0.88, 0.3, 0.3, (0.15, 0.15, 0.2), 8)
    return pix


def armar():
    p.en_mesa("Certificado de deudor", p.mat_hoja("certificado_deudor", certificado()), -0.5, 0.33, 0.21, 0.28, grueso=0.002, giro=-12)
