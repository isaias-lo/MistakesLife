"""Llegaron correos sin leer (escena.casa.celular_avisos): la pantalla del celular se enciende con
los avisos del correo uno bajo otro y el punto rojo del contador.
"""

import comun as c
import papeles as p


def pantalla():
    pix = p.hoja(300, 150, (0.10, 0.14, 0.22), 1, grano=0.0)
    p.numero(pix, "9", 0.08, 0.38, 0.06, (0.95, 0.96, 0.98))
    p.numero(pix, "41", 0.08, 0.5, 0.06, (0.95, 0.96, 0.98))
    for k in range(4):
        y = 0.3 + k * 0.15
        p.rect(pix, y, y + 0.12, 0.06, 0.94, (0.86, 0.90, 0.96))
        p.rect(pix, y + 0.025, y + 0.07, 0.1, 0.24, (0.85, 0.30, 0.25) if k == 0 else (0.30, 0.50, 0.85))
        p.renglones(pix, y + 0.03, y + 0.1, 0.3, 0.9, (0.25, 0.28, 0.35), 5 + k, paso=0.035, grueso=0.5)
    return pix


def armar():
    luz = c.material("pantalla_encendida", (1, 1, 1), aspereza=0.1, textura=p.imagen("pantalla_encendida", pantalla()[::-1, ::-1]))
    bsdf = luz.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Emission Color"].default_value = (0.6, 0.75, 1.0, 1)
    bsdf.inputs["Emission Strength"].default_value = 0.4
    o = c.caja("Pantalla encendida", (0.0, 0.4542, 0.36), (0.068, 0.0008, 0.146), luz)
    c.uv_plano_arriba(o)
    c.girar_y(o, 12)
    rojo = c.material("aviso_rojo", (0.95, 0.30, 0.25), aspereza=0.3)
    x, _, z = p.girar_punto((0.022, 0, 0.42), (0.0, 0.36), 12)
    c.cilindro("Aviso del correo", (x, 0.4552, z), 0.006, 0.001, rojo, lados=16)
