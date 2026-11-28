"""Tu billetera de cuero sobre la mesa con la tarjeta de débito del banco encima
(escena.casa.billetera_tarjeta): aparece cuando abres tu cuenta. La tarjeta mide como una de
verdad (85,6 x 54 mm), con su chip dorado y la franja de la firma.
"""

import math

import comun as c


def _corrido(cx, cz, dx, dz, grados):
    """El punto (dx, dz) del objeto girado como Quaternion.Euler(0, grados, 0) de Unity, desde su centro."""
    r = math.radians(grados)
    return cx + dx * math.cos(r) + dz * math.sin(r), cz - dx * math.sin(r) + dz * math.cos(r)


def armar():
    cuero = c.material("cuero_billetera", (0.30, 0.18, 0.11), aspereza=0.75)
    costura = c.material("costura_billetera", (0.70, 0.58, 0.40), aspereza=0.8)
    tarjeta = c.material("tarjeta_debito", (0.12, 0.20, 0.36), aspereza=0.3)
    chip = c.material("chip_tarjeta", (0.85, 0.70, 0.35), aspereza=0.25)
    franja = c.material("franja_tarjeta", (0.92, 0.92, 0.90), aspereza=0.4)

    x, z = 0.17, 0.39
    billetera = c.caja("Billetera", (x, 0.449, z), (0.11, 0.018, 0.09), cuero, bisel=0.004)
    c.girar_y(billetera, -8)
    borde = c.caja("Costura", (x, 0.4585, z), (0.1, 0.001, 0.08), costura)
    c.girar_y(borde, -8)
    hoja = c.caja("Tapa", (x, 0.4588, z), (0.096, 0.0012, 0.076), cuero)
    c.girar_y(hoja, -8)

    # La tarjeta, un poco corrida sobre la billetera, como recién dejada.
    tx, tz = x + 0.03, z - 0.01
    cuerpo = c.caja("Tarjeta de débito", (tx, 0.4605, tz), (0.0856, 0.0016, 0.054), tarjeta, bisel=0.0015)
    c.girar_y(cuerpo, 14)
    px, pz = _corrido(tx, tz, -0.02, -0.004, 14)
    pastilla = c.caja("Chip", (px, 0.4616, pz), (0.012, 0.0006, 0.01), chip)
    c.girar_y(pastilla, 14)
    fx, fz = _corrido(tx, tz, 0.005, 0.015, 14)
    firma = c.caja("Franja", (fx, 0.4616, fz), (0.06, 0.0005, 0.008), franja)
    c.girar_y(firma, 14)
