"""El router de la fibra al lado de la tele, con sus luces verdes (escena.casa.router)."""

import comun as c


def armar():
    blanco = c.material("router", (0.95, 0.95, 0.94), aspereza=0.35)
    gris = c.material("antena", (0.20, 0.20, 0.22), aspereza=0.5)
    verde = c.material("luz_verde", (0.30, 0.90, 0.40), aspereza=0.3)
    verde.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (0.3, 0.9, 0.4, 1)
    verde.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 2.0
    c.caja("Router", (0.78, -0.25, 0.1), (0.24, 0.16, 0.05), blanco, bisel=0.012, segmentos=3)
    for x in (0.7, 0.86):
        c.cilindro("Antena", (x, -0.1, 0.1), 0.008, 0.16, gris, lados=8)
    for i in range(3):
        c.esfera("Luz", (0.72 + i * 0.06, -0.29, 0.074), 0.007, verde, lados=8)
