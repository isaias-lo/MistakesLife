"""La alfombra de la consulta (consulta.alfombra): 3,60 x 2,60 m de lana color arena con un borde
más oscuro, como las que se ven en las consultas de psicología."""

import comun as c


def armar():
    lana = c.material("alfombra_consulta", (1, 1, 1), aspereza=0.95, textura=c.textura_tela("alfombra_consulta", (0.76, 0.64, 0.52), semilla=81))
    borde = c.material("borde_alfombra", (1, 1, 1), aspereza=0.95, textura=c.textura_tela("borde_alfombra", (0.52, 0.40, 0.30), semilla=82))
    c.uv_cubo(c.caja("Alfombra", (0, 0.008, 0), (3.36, 0.016, 2.36), lana, bisel=0.003), escala=0.8)
    for dz in (-1.24, 1.24):
        c.uv_cubo(c.caja("Borde", (0, 0.008, dz), (3.6, 0.017, 0.12), borde, bisel=0.003), escala=0.8)
    for dx in (-1.74, 1.74):
        c.uv_cubo(c.caja("Borde", (dx, 0.008, 0), (0.12, 0.017, 2.36), borde, bisel=0.003), escala=0.8)
