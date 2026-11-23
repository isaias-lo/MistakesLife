"""Las cuentas sin pagar (escena.casa.cuentas): las boletas de la luz, el agua, el gas y el internet
apiladas sobre la mesa; dos ya traen el aviso de corte en rojo.
"""

import papeles as p

EMPRESAS = [(0.92, 0.55, 0.15), (0.20, 0.50, 0.80), (0.30, 0.60, 0.40), (0.50, 0.30, 0.60)]


def boleta(i):
    pix = p.hoja(200, 280, (0.97, 0.97, 0.95), 70 + i)
    color = EMPRESAS[i]
    p.rect(pix, 0.0, 0.14, 0.0, 1.0, color)
    p.renglones(pix, 0.2, 0.42, 0.06, 0.55, p.TINTA, 70 + i, paso=0.05, grueso=0.35)
    p.borde(pix, 0.2, 0.42, 0.6, 0.94, color, 2)
    p.renglones(pix, 0.26, 0.38, 0.64, 0.9, p.TINTA, 80 + i, paso=0.06, grueso=0.4)
    for f in range(5):
        p.renglones(pix, 0.5 + f * 0.07, 0.55 + f * 0.07, 0.06, 0.94, (0.4, 0.4, 0.45), 90 + i * 5 + f, paso=0.05, grueso=0.3)
    if i % 2 == 0:
        p.rect(pix, 0.82, 0.95, 0.5, 0.94, p.ROJO)
        p.renglones(pix, 0.86, 0.92, 0.54, 0.9, (0.98, 0.95, 0.95), 99 + i, paso=0.05, grueso=0.6)
    return pix


def armar():
    for i in range(4):
        x, z = -0.1 + i * 0.07, -0.32 + i * 0.03
        p.en_mesa("Cuenta", p.mat_hoja("cuenta_%d" % i, boleta(i)), x, z, 0.28, 0.2, abajo=p.MESA + i * 0.0012, grueso=0.0012, giro=i * 17)
