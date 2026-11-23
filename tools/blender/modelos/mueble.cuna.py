"""Cuna de guagua (mueble.cuna): de madera blanca con barrotes, colchón y una mantita, en el lugar
de la cama. Mismo espacio que la forma simple: 1,00 x 0,60 m y 0,70 m de alto.
"""

import comun as c


def armar():
    blanco = c.material("madera_blanca", (0.94, 0.93, 0.90), aspereza=0.5)
    colchon = c.material("colchon_cuna", (0.92, 0.94, 0.96), aspereza=0.9)
    manta = c.material("manta", (1, 1, 1), aspereza=0.95, textura=c.textura_tela("tela_manta", (0.72, 0.82, 0.92), semilla=47))

    cx, cz = 0.0, 0.2
    largo, ancho, alto = 1.0, 0.6, 0.7
    for dx in (-largo / 2, largo / 2):
        for dz in (-ancho / 2, ancho / 2):
            c.caja("Pilar", (cx + dx, alto / 2, cz + dz), (0.045, alto, 0.045), blanco, bisel=0.006)
    for y in (0.3, alto - 0.02):
        for dz in (-ancho / 2, ancho / 2):
            c.caja("Baranda larga", (cx, y, cz + dz), (largo, 0.035, 0.03), blanco, bisel=0.004)
        for dx in (-largo / 2, largo / 2):
            c.caja("Baranda corta", (cx + dx, y, cz), (0.03, 0.035, ancho), blanco, bisel=0.004)
    # Barrotes.
    for i in range(1, 12):
        x = cx - largo / 2 + i * largo / 12
        for dz in (-ancho / 2, ancho / 2):
            c.cilindro("Barrote", (x, 0.5, cz + dz), 0.008, 0.38, blanco, lados=8)
    for i in range(1, 6):
        z = cz - ancho / 2 + i * ancho / 6
        for dx in (-largo / 2, largo / 2):
            c.cilindro("Barrote", (cx + dx, 0.5, z), 0.008, 0.38, blanco, lados=8)
    c.caja("Base", (cx, 0.3, cz), (largo - 0.02, 0.03, ancho - 0.02), blanco)
    c.caja("Colchon", (cx, 0.36, cz), (largo - 0.04, 0.09, ancho - 0.04), colchon, bisel=0.025, segmentos=3)
    c.uv_cubo(c.caja("Mantita", (cx - 0.15, 0.415, cz), (0.5, 0.025, ancho - 0.06), manta, bisel=0.012, segmentos=3), escala=0.5)
