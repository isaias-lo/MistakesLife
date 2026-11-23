"""La casa desordenada (escena.casa.desorden): ropa tirada en el piso; un chaleco arrugado, unos
jeans y un polerón con una toalla encima.
"""

import comun as c
import papeles as p


def tela(nombre, color, semilla):
    return c.material(nombre, (1, 1, 1), aspereza=0.95, textura=c.textura_tela(nombre, color, semilla=semilla))


def bulto(nombre, centro, radios, mat):
    o = c.esfera(nombre, centro, 1.0, mat, escala=radios, lados=20)
    c.uv_cubo(o, escala=0.3)
    return o


def pieza(nombre, centro, giro, tam, mat, bisel=0.012):
    o = c.caja(nombre, centro, tam, mat, bisel=bisel)
    c.uv_cubo(o, escala=0.3)
    c.girar_y(o, giro)
    return o


def armar():
    # Chaleco arrugado, con las dos mangas saliendo del cuerpo (una estirada, otra doblada).
    chaleco = tela("chaleco", (0.55, 0.30, 0.30), 201)
    cx, cz, g = 0.3, -0.2, 20
    o = bulto("Chaleco", (cx, 0.03, cz), (0.19, 0.03, 0.15), chaleco)
    c.girar_y(o, g)
    mx, _, mz = p.girar_punto((cx + 0.24, 0, cz + 0.08), (cx, cz), g)
    pieza("Manga estirada", (mx, 0.016, mz), g - 15, (0.24, 0.028, 0.075), chaleco)
    mx, _, mz = p.girar_punto((cx - 0.12, 0, cz - 0.15), (cx, cz), g)
    pieza("Manga doblada", (mx, 0.05, mz), g + 70, (0.16, 0.028, 0.07), chaleco)

    # Jeans tirados: la cintura y las dos piernas que se abren desde ella.
    jean = tela("jean", (0.24, 0.30, 0.48), 202)
    jx, jz, g = -0.9, -0.05, -35
    pieza("Cintura del jean", (jx, 0.018, jz), g, (0.34, 0.034, 0.16), jean)
    for lado in (-1, 1):
        px, _, pz = p.girar_punto((jx + lado * 0.1, 0, jz + 0.3), (jx, jz), g + lado * 6)
        pieza("Pierna del jean", (px, 0.014, pz), g + lado * 6, (0.14, 0.026, 0.46), jean)

    # Polerón hecho un bulto, con la capucha, y una toalla doblada encima.
    poleron = tela("poleron", (0.45, 0.45, 0.42), 203)
    o = bulto("Poleron", (1.2, 0.045, 1.2), (0.22, 0.045, 0.17), poleron)
    c.girar_y(o, 60)
    bulto("Capucha", (1.05, 0.04, 1.32), (0.09, 0.04, 0.08), poleron)
    toalla = tela("toalla", (0.85, 0.82, 0.72), 204)
    pieza("Toalla", (1.25, 0.1, 1.17), 40, (0.24, 0.035, 0.15), toalla, bisel=0.015)
