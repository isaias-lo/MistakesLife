"""Cuaderno de apuntes (escena.casa.cuaderno_apuntes): cuaderno universitario de espiral, abierto,
con hojas de cuadro y apuntes en lápiz pasta azul, y un lápiz grafito encima. Va sobre la mesa,
en el mismo lugar que la forma simple (SceneObjectBuilder.cs).
"""

import math

import numpy as np

import comun as c

MESA = 0.44  # cara de arriba de la mesa


def hoja_cuadro(nombre, con_apuntes, semilla):
    """Hoja de cuadro chico (cuadriculado de 5 mm) con margen rojo y, si se pide, renglones escritos."""
    alto, ancho = 512, 384
    pix = np.ones((alto, ancho, 4))
    pix[..., :3] = (0.96, 0.95, 0.91)
    paso = 12
    pix[::paso, :, :3] = (0.78, 0.84, 0.90)
    pix[:, ::paso, :3] = (0.78, 0.84, 0.90)
    pix[:, 40:42, :3] = (0.85, 0.45, 0.45)
    if con_apuntes:
        rng = np.random.default_rng(semilla)
        for renglon in range(4, 36):
            if rng.random() < 0.2:
                continue
            fin = int(60 + rng.random() * 300)
            y = renglon * paso + 5
            x = 50
            while x < fin:
                palabra = int(8 + rng.random() * 30)
                pix[y:y + 3, x:min(x + palabra, fin), :3] = (0.18, 0.25, 0.55)
                x += palabra + 8
    return c.imagen(nombre, pix[::-1])


def armar():
    tapa = c.material("tapa_cuaderno", (0.22, 0.33, 0.52), aspereza=0.5)
    hoja_izq = c.material("hoja_izquierda", (1, 1, 1), aspereza=0.9, textura=hoja_cuadro("hoja_izq", True, 3))
    hoja_der = c.material("hoja_derecha", (1, 1, 1), aspereza=0.9, textura=hoja_cuadro("hoja_der", True, 5))
    espiral = c.material("espiral", (0.72, 0.72, 0.74), aspereza=0.3)
    madera_lapiz = c.material("lapiz", (0.93, 0.74, 0.22), aspereza=0.5)
    punta = c.material("punta_lapiz", (0.85, 0.70, 0.52), aspereza=0.8)
    goma = c.material("goma_lapiz", (0.85, 0.45, 0.48), aspereza=0.8)

    centro_x, centro_z = -0.05, 0.05
    partes = []

    # Tapa abierta (las dos mitades) y un bloque de hojas en cada lado.
    partes.append(c.caja("Tapa", (centro_x, MESA + 0.002, centro_z), (0.31, 0.004, 0.21), tapa, bisel=0.002))
    izq = c.caja("Hojas izquierdas", (centro_x - 0.075, MESA + 0.007, centro_z), (0.145, 0.006, 0.2), hoja_izq, bisel=0.001)
    der = c.caja("Hojas derechas", (centro_x + 0.075, MESA + 0.007, centro_z), (0.145, 0.006, 0.2), hoja_der, bisel=0.001)
    c.uv_plano_arriba(izq)
    c.uv_plano_arriba(der)
    partes += [izq, der]

    # Espiral metálica por el medio.
    for i in range(18):
        z = centro_z - 0.09 + i * 0.0106
        anillo = c.cilindro("Espiral", (centro_x, MESA + 0.009, z), 0.006, 0.003, espiral, eje="z", lados=8)
        partes.append(anillo)

    for p in partes:
        p.location = c.unity(*_girar((-p.location[0], p.location[2], -p.location[1]), (centro_x, centro_z), -12))
        c.girar_y(p, -12)

    # Lápiz grafito hexagonal, cruzado sobre la hoja derecha.
    cuerpo = c.cilindro("Lapiz", (0.04, MESA + 0.016, 0.08), 0.0045, 0.15, madera_lapiz, eje="x", lados=6)
    c.girar_y(cuerpo, 30)
    largo = 0.075
    ang = math.radians(30)
    punta_pos = (0.04 + largo * math.cos(ang) + 0.008, MESA + 0.016, 0.08 - largo * math.sin(ang) - 0.004)
    p = c.cilindro("Punta del lapiz", punta_pos, 0.003, 0.016, punta, eje="x", lados=6)
    c.girar_y(p, 30)
    goma_pos = (0.04 - largo * math.cos(ang) - 0.006, MESA + 0.016, 0.08 + largo * math.sin(ang) + 0.003)
    g = c.cilindro("Goma del lapiz", goma_pos, 0.0046, 0.012, goma, eje="x", lados=8)
    c.girar_y(g, 30)


def _girar(pos, eje, grados):
    """Gira una posición de Unity alrededor de (eje_x, eje_z) como Quaternion.Euler(0, grados, 0)."""
    x, y, z = pos
    ex, ez = eje
    a = math.radians(grados)
    dx, dz = x - ex, z - ez
    return (ex + dx * math.cos(a) + dz * math.sin(a), y, ez - dx * math.sin(a) + dz * math.cos(a))
