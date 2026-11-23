"""Sillón de tres cuerpos tapizado en tela (mueble.sillon), a lo largo de z, con el respaldo
contra el muro (+x) y el asiento mirando a la pieza. Mismo espacio que la forma simple: 0,70 m
de fondo, 1,60 m de largo y 0,60 m de alto.
"""

import comun as c

FONDO, LARGO, ALTO = 0.7, 1.6, 0.6


def armar():
    tapiz = c.material("tapiz", (1, 1, 1), aspereza=0.95, textura=c.textura_tela("tela_sillon", (0.33, 0.43, 0.39), semilla=21))
    patas = c.material("patas_sillon", (0.22, 0.16, 0.11), aspereza=0.5)

    alto_patas = 0.08
    brazo = 0.14
    # Base del asiento.
    base = c.caja("Base", (0, alto_patas + 0.11, 0), (FONDO, 0.22, LARGO), tapiz, bisel=0.03, segmentos=3)
    c.uv_cubo(base, escala=0.6)
    # Respaldo contra el muro.
    resp = c.caja("Respaldo", (FONDO / 2 - 0.09, ALTO / 2 + 0.06, 0), (0.18, ALTO - 0.04, LARGO), tapiz, bisel=0.04, segmentos=3)
    c.uv_cubo(resp, escala=0.6)
    # Brazos.
    for z in (-(LARGO / 2 - brazo / 2), LARGO / 2 - brazo / 2):
        b = c.caja("Brazo", (0, 0.36, z), (FONDO, 0.34, brazo), tapiz, bisel=0.045, segmentos=4)
        c.uv_cubo(b, escala=0.6)
    # Tres cojines de asiento y tres de respaldo.
    ancho_cojin = (LARGO - 2 * brazo) / 3
    for i in range(3):
        z = -LARGO / 2 + brazo + ancho_cojin * (i + 0.5)
        a = c.caja("Cojin del asiento", (-0.06, alto_patas + 0.27, z), (FONDO - 0.2, 0.11, ancho_cojin - 0.01), tapiz, bisel=0.04, segmentos=4)
        c.uv_cubo(a, escala=0.6)
        r = c.caja("Cojin del respaldo", (FONDO / 2 - 0.23, 0.5, z), (0.13, 0.32, ancho_cojin - 0.015), tapiz, bisel=0.05, segmentos=4)
        c.uv_cubo(r, escala=0.6)
    # Patas de madera oscura.
    for x in (-FONDO / 2 + 0.06, FONDO / 2 - 0.06):
        for z in (-LARGO / 2 + 0.06, LARGO / 2 - 0.06):
            c.caja("Pata", (x, alto_patas / 2, z), (0.04, alto_patas, 0.04), patas, bisel=0.006, ahusado=0.75)
