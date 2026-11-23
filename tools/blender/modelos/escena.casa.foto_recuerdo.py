"""Alguien que ya no está (escena.casa.foto_recuerdo): su retrato en el muro lateral con la cinta
negra cruzada en la esquina, y una vela en su vaso de vidrio abajo.
"""

import comun as c
import fotos as f


def imagen():
    pix = f.foto(300, 240, (0.70, 0.68, 0.64), (0.70, 0.68, 0.64), semilla=61)
    f.persona(pix, 0.5, 1.15, 0.95, (0.30, 0.28, 0.30), f.PIELES[1], f.PELOS[2])
    gris = pix[..., :3].mean(axis=2, keepdims=True)
    pix[..., :3] = gris * 0.7 + pix[..., :3] * 0.3 + (0.04, 0.02, 0.0)
    return pix


def armar():
    f.marco_lado("foto_recuerdo", imagen(), 1.7, 0.0, 0.5, 0.4, madera=(0.22, 0.15, 0.10))
    f.cinta_luto("Cinta negra", (0.065, 1.88, 0.15), (0.003, 0.035, 0.16), giro_x=45)
    vaso = c.material("vaso_vela", (0.85, 0.30, 0.25), aspereza=0.2)
    cera = c.material("cera_recuerdo", (0.96, 0.94, 0.88), aspereza=0.5)
    llama = c.material("llama_recuerdo", (1.0, 0.72, 0.25), aspereza=0.3)
    bsdf = llama.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Emission Color"].default_value = (1.0, 0.65, 0.2, 1)
    bsdf.inputs["Emission Strength"].default_value = 6.0
    c.cilindro("Vaso de la vela", (0.25, 0.075, 0.0), 0.035, 0.15, vaso, lados=20)
    c.cilindro("Cera", (0.25, 0.14, 0.0), 0.032, 0.01, cera, lados=20)
    c.cilindro("Mecha", (0.25, 0.15, 0.0), 0.0015, 0.012, c.material("mecha", (0.1, 0.1, 0.1), aspereza=0.8), lados=6)
    c.esfera("Llama", (0.25, 0.165, 0.0), 0.009, llama, escala=(1.0, 2.0, 1.0), lados=12)
