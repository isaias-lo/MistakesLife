"""La ropa para la entrevista en un perchero de pie junto al ropero (escena.casa.ropa_entrevista):
chaqueta, camisa blanca y corbata.
"""

import comun as c


def armar():
    madera = c.material("perchero", (0.36, 0.25, 0.16), aspereza=0.5)
    azul = c.material("chaqueta", (1, 1, 1), aspereza=0.9, textura=c.textura_tela("tela_chaqueta", (0.20, 0.24, 0.34), semilla=87))
    blanco = c.material("camisa", (0.96, 0.96, 0.95), aspereza=0.8)
    corbata = c.material("corbata", (0.80, 0.62, 0.15), aspereza=0.5)
    x, z = -0.75, -0.35
    c.cilindro("Pie", (x, 0.8, z), 0.02, 1.6, madera, lados=10)
    c.cilindro("Base", (x, 0.02, z), 0.18, 0.03, madera, lados=24)
    c.cilindro("Colgador", (x, 1.56, z - 0.06), 0.008, 0.42, madera, eje="x", lados=8)
    c.uv_cubo(c.caja("Chaqueta", (x, 1.27, z - 0.09), (0.46, 0.58, 0.07), azul, bisel=0.02, segmentos=3), escala=0.5)
    c.caja("Camisa", (x, 1.36, z - 0.128), (0.14, 0.4, 0.01), blanco)
    c.caja("Cuello", (x, 1.54, z - 0.13), (0.16, 0.04, 0.02), blanco, bisel=0.005)
    c.caja("Corbata", (x, 1.33, z - 0.137), (0.05, 0.34, 0.006), corbata, ahusado=1.4)
