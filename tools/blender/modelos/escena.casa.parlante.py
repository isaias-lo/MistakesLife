"""El parlante grande de las fiestas, en el suelo al lado de la tele (escena.casa.parlante)."""

import comun as c


def armar():
    negro = c.material("parlante", (0.10, 0.10, 0.12), aspereza=0.6)
    cono = c.material("cono", (0.20, 0.20, 0.22), aspereza=0.7)
    luces = c.material("luces", (0.75, 0.30, 0.95), aspereza=0.3)
    luces.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (0.75, 0.30, 0.95, 1)
    luces.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 2.0
    x, z = 1.25, -0.1
    c.caja("Caja", (x, -0.6, z), (0.36, 0.6, 0.3), negro, bisel=0.02, segmentos=3)
    for y, r in ((-0.75, 0.11), (-0.47, 0.07)):
        c.cilindro("Borde", (x, y, z - 0.152), r + 0.015, 0.008, luces, eje="z", lados=28)
        c.cilindro("Cono", (x, y, z - 0.155), r, 0.01, cono, eje="z", lados=28)
        c.cilindro("Centro", (x, y, z - 0.16), r * 0.3, 0.01, negro, eje="z", lados=16)
    c.caja("Manilla", (x, -0.29, z), (0.16, 0.03, 0.05), negro, bisel=0.01)
