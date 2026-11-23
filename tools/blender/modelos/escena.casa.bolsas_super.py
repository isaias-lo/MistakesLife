"""Tu compra (escena.casa.bolsas_super): dos bolsas de papel del supermercado en el suelo junto a
la cocina, con una lechuga, tomates y una marraqueta asomando.
"""

import comun as c


def armar():
    papel = c.material("papel_kraft", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_kraft", (0.78, 0.63, 0.44), semilla=71))
    lechuga = c.material("lechuga", (0.44, 0.68, 0.38), aspereza=0.7)
    tomate = c.material("tomate", (0.88, 0.25, 0.20), aspereza=0.35)
    pan = c.material("marraqueta", (0.88, 0.68, 0.40), aspereza=0.8)
    for i in range(2):
        x, z = 0.62 + i * 0.34, -0.12 - i * 0.06
        bolsa = c.caja("Bolsa", (x, 0.2, z), (0.3, 0.4, 0.2), papel, bisel=0.006)
        c.uv_cubo(bolsa, escala=0.5)
        c.girar_y(bolsa, -10 + i * 22)
        c.caja("Doblez", (x, 0.39, z), (0.31, 0.02, 0.21), papel)
    c.esfera("Lechuga", (0.58, 0.42, -0.12), 0.08, lechuga, escala=(1.0, 0.85, 1.0))
    c.esfera("Tomate", (0.69, 0.41, -0.09), 0.045, tomate)
    c.esfera("Tomate", (0.66, 0.405, -0.17), 0.04, tomate)
    for dx in (-0.03, 0.03):
        c.esfera("Marraqueta", (1.0 + dx, 0.43, -0.18), 0.045, pan, escala=(1.0, 0.7, 1.2))
