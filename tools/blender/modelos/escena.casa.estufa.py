"""La estufa eléctrica prendida al lado del sillón (escena.casa.estufa): estufa de cuarzo con
rejilla, resistencia al rojo y perillas.
"""

import comun as c


def armar():
    blanco = c.material("estufa", (0.85, 0.85, 0.83), aspereza=0.4)
    rojo = c.material("resistencia", (1.0, 0.42, 0.12), aspereza=0.3)
    rojo.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (1.0, 0.35, 0.08, 1)
    rojo.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 3.0
    rejilla = c.material("rejilla", (0.55, 0.55, 0.55), aspereza=0.3)
    negro = c.material("perilla", (0.12, 0.12, 0.12), aspereza=0.5)
    x, z = -0.6, -0.95
    c.caja("Cuerpo", (x, 0.32, z), (0.4, 0.56, 0.18), blanco, bisel=0.02, segmentos=3)
    c.caja("Pie", (x, 0.02, z), (0.32, 0.04, 0.26), blanco, bisel=0.01)
    for i in range(3):
        c.cilindro("Resistencia", (x, 0.2 + i * 0.1, z - 0.07), 0.014, 0.28, rojo, eje="x", lados=10)
    for i in range(7):
        c.cilindro("Rejilla", (x - 0.15 + i * 0.05, 0.3, z - 0.095), 0.003, 0.36, rejilla, lados=6)
    for dx in (-0.08, 0.08):
        c.cilindro("Perilla", (x + dx, 0.55, z - 0.095), 0.018, 0.02, negro, eje="z", lados=14)
