"""Piezas compartidas por varios modelos de muebles (por ejemplo, las tres camas)."""

import comun as c

ALTO_COLCHON = 0.5  # cara de arriba del colchón, igual que la forma simple de RoomBuilder.cs


def cama(centro_x, ancho, frazada, almohadas, cabecera_madera=True, semilla=3):
    """Cama de largo 1,90 m a lo largo de z, con la cabecera hacia el muro del fondo (+z).

    Base de madera con patas, colchón, sábana doblada a la altura de la almohada y frazada.
    """
    largo = 1.9
    veta = c.textura_madera("veta_cama", claro=(0.60, 0.44, 0.30), oscuro=(0.44, 0.31, 0.20), semilla=semilla)
    madera = c.material("madera_cama", (1, 1, 1), aspereza=0.55, textura=veta)
    colchon = c.material("colchon", (0.93, 0.92, 0.89), aspereza=0.9)
    sabana = c.material("sabana", (0.95, 0.95, 0.93), aspereza=0.9, textura=c.textura_tela("tela_sabana", (0.95, 0.95, 0.93), semilla=semilla))
    tela = c.material("frazada", (1, 1, 1), aspereza=0.95, textura=frazada)

    # Base (somier con marco de madera) y cuatro patas cortas.
    alto_base = 0.22
    base = c.caja("Base", (centro_x, 0.08 + alto_base / 2, 0), (ancho, alto_base, largo), madera, bisel=0.01)
    c.uv_cubo(base, escala=1.6)
    for dx in (-ancho / 2 + 0.05, ancho / 2 - 0.05):
        for dz in (-largo / 2 + 0.05, largo / 2 - 0.05):
            c.caja("Pata", (centro_x + dx, 0.04, dz), (0.05, 0.08, 0.05), madera, bisel=0.005)

    # Cabecera contra el muro.
    if cabecera_madera:
        cab = c.caja("Cabecera", (centro_x, 0.48, largo / 2 + 0.02), (ancho + 0.06, 0.8, 0.05), madera, bisel=0.012, segmentos=3)
        c.uv_cubo(cab, escala=1.6)

    # Colchón.
    alto_colchon = ALTO_COLCHON - 0.08 - alto_base
    c.caja("Colchon", (centro_x, 0.08 + alto_base + alto_colchon / 2, 0), (ancho - 0.02, alto_colchon, largo - 0.03), colchon, bisel=0.04, segmentos=4)

    # Frazada que cubre desde los pies hasta poco antes de las almohadas, cayendo por los lados.
    largo_frazada = largo * 0.72
    z_frazada = -largo / 2 + largo_frazada / 2 - 0.02
    fr = c.caja("Frazada", (centro_x, ALTO_COLCHON - 0.06, z_frazada), (ancho + 0.05, 0.14, largo_frazada), tela, bisel=0.035, segmentos=4)
    c.uv_cubo(fr, escala=0.8)
    # Doblez de la sábana sobre la frazada.
    c.caja("Sabana doblada", (centro_x, ALTO_COLCHON + 0.018, z_frazada + largo_frazada / 2 - 0.08), (ancho + 0.055, 0.03, 0.16), sabana, bisel=0.012, segmentos=3)

    # Almohadas junto a la cabecera.
    funda = c.material("funda", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_funda", (0.94, 0.93, 0.90), semilla=semilla + 5))
    paso = ancho / almohadas
    for i in range(almohadas):
        x = centro_x - ancho / 2 + paso * (i + 0.5)
        alm = c.caja("Almohada", (x, ALTO_COLCHON + 0.05, largo / 2 - 0.22), (min(0.62, paso - 0.08), 0.12, 0.34), funda, bisel=0.05, segmentos=4)
        c.girar_y(alm, 0)
