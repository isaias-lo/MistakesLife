"""Estás de duelo (escena.casa.vela_recuerdo): un portarretrato de pie sobre la mesa con la foto de
quien murió y la cinta negra en la esquina, y al lado una vela encendida en su platito.
"""

import comun as c
import fotos as f
import papeles as p


def retrato():
    pix = f.foto(160, 120, (0.72, 0.70, 0.66), (0.72, 0.70, 0.66), semilla=91)
    f.persona(pix, 0.5, 1.12, 0.92, (0.35, 0.33, 0.36), f.PIELES[0], f.PELOS[2], pelo_largo=True)
    gris = pix[..., :3].mean(axis=2, keepdims=True)
    pix[..., :3] = gris * 0.6 + pix[..., :3] * 0.4
    return pix


def armar():
    madera = c.material("portarretrato", (0.24, 0.16, 0.10), aspereza=0.45)
    x, y, z = 0.42, p.MESA + 0.08, -0.32
    marco = c.caja("Portarretrato", (x, y, z), (0.12, 0.16, 0.015), madera, bisel=0.003)
    c.girar_x(marco, -10)
    foto = c.caja("Foto", (x, y + 0.002, z - 0.009), (0.09, 0.125, 0.001), f.material_foto("retrato_duelo", retrato()))
    c.uv_plano_frente(foto)
    c.girar_x(foto, -10)
    c.caja("Pata del portarretrato", (x, p.MESA + 0.055, z + 0.04), (0.02, 0.11, 0.004), madera)
    f.cinta_luto("Cinta negra", (x - 0.04, y + 0.06, z - 0.012), (0.06, 0.012, 0.003), giro_z=45)

    cera = c.material("cera_duelo", (0.96, 0.93, 0.84), aspereza=0.5)
    plato = c.material("platito_duelo", (0.85, 0.85, 0.83), aspereza=0.3)
    llama = c.material("llama_duelo", (1.0, 0.72, 0.25), aspereza=0.3)
    bsdf = llama.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Emission Color"].default_value = (1.0, 0.65, 0.2, 1)
    bsdf.inputs["Emission Strength"].default_value = 6.0
    vx, vz = 0.28, -0.3
    c.cilindro("Platito", (vx, p.MESA + 0.004, vz), 0.04, 0.008, plato, lados=24)
    c.cilindro("Vela", (vx, p.MESA + 0.053, vz), 0.017, 0.09, cera, lados=16)
    c.cilindro("Mecha", (vx, p.MESA + 0.104, vz), 0.0015, 0.012, c.material("mecha", (0.1, 0.1, 0.1), aspereza=0.8), lados=6)
    c.esfera("Llama", (vx, p.MESA + 0.122, vz), 0.009, llama, escala=(1.0, 2.0, 1.0), lados=12)
