"""Firmaste con una isapre (escena.casa.carpeta_isapre): la carpeta del contrato de salud, de tapa
plástica, parada al fondo de la mesa con las hojas adentro y la etiqueta en el lomo.
"""

import comun as c
import papeles as p


def etiqueta():
    pix = p.hoja(96, 220, (0.97, 0.97, 0.95), 6)
    p.rect(pix, 0.08, 0.3, 0.06, 0.94, (0.20, 0.55, 0.52))
    p.renglones(pix, 0.45, 0.9, 0.08, 0.92, p.TINTA, 6, paso=0.2, grueso=0.25)
    return pix


def armar():
    x, y, z = 0.62, 0.6, 0.3
    plastico = c.material("tapa_isapre", (0.18, 0.55, 0.52), aspereza=0.3)
    c.caja("Tapa de atras", (x + 0.015, y, z), (0.003, 0.3, 0.24), plastico, bisel=0.001)
    c.caja("Tapa de adelante", (x - 0.015, y, z), (0.003, 0.3, 0.24), plastico, bisel=0.001)
    c.caja("Lomo", (x, y - 0.149, z), (0.033, 0.003, 0.24), plastico)
    c.caja("Hojas", (x, y - 0.005, z), (0.024, 0.28, 0.225), c.material("hojas_isapre", (0.95, 0.94, 0.90), aspereza=0.9))
    o = c.caja("Etiqueta", (x - 0.0168, y + 0.06, z), (0.001, 0.06, 0.14), p.mat_hoja("etiqueta_isapre", etiqueta()))
    o.rotation_euler.z += 3.14159
    p.uv_plano_lado(o)
