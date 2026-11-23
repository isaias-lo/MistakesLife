"""Se cortó la luz: dos velas encendidas en platitos sobre la mesa (escena.casa.velas)."""

import comun as c


def armar():
    cera = c.material("cera", (0.96, 0.94, 0.88), aspereza=0.5)
    plato = c.material("platito", (0.85, 0.85, 0.83), aspereza=0.3)
    llama = c.material("llama", (1.0, 0.72, 0.25), aspereza=0.3)
    llama.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (1.0, 0.65, 0.2, 1)
    llama.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 6.0
    for z in (0.2, 0.36):
        c.cilindro("Platito", (0.63, 0.447, z), 0.05, 0.008, plato, lados=24)
        c.cilindro("Vela", (0.63, 0.51, z), 0.018, 0.12, cera, lados=16)
        c.cilindro("Mecha", (0.63, 0.575, z), 0.0015, 0.012, c.material("mecha", (0.1, 0.1, 0.1), aspereza=0.8), lados=6)
        c.esfera("Llama", (0.63, 0.595, z), 0.01, llama, escala=(1.0, 2.0, 1.0), lados=12)
