"""La camiseta de la selección de la comuna (escena.casa.camiseta_seleccion): roja, con cuello
blanco y su número en la espalda, colgada de un colgador en el muro.
"""

import comun as c
import muebles
import papeles as p


def numero():
    pix = p.hoja(120, 90, (0.98, 0.98, 0.97), 3, grano=0.0)
    pix[..., :3] = (0.80, 0.15, 0.15)
    p.numero(pix, "9", 0.12, 0.25, 0.75, (0.98, 0.98, 0.97))
    return pix


def armar():
    z, hombro = -0.4, 1.11
    muebles.colgada("camiseta", z, hombro, 0.34, 0.3, (0.75, 0.13, 0.13), semilla=95)
    c.caja("Cuello", (0.062, hombro - 0.006, z), (0.006, 0.014, 0.11), c.material("cuello_camiseta", (0.95, 0.95, 0.94), aspereza=0.8))
    o = c.caja("Numero", (0.0615, hombro - 0.17, z), (0.001, 0.15, 0.11), p.mat_hoja("numero_camiseta", numero(), aspereza=0.7))
    p.uv_plano_lado(o)
    madera = c.material("colgador_madera", (0.55, 0.38, 0.22), aspereza=0.4)
    c.caja("Colgador", (0.07, hombro + 0.005, z), (0.012, 0.012, 0.34), madera, bisel=0.004)
    metal = c.material("gancho_colgador", (0.7, 0.7, 0.72), aspereza=0.3)
    c.cilindro("Gancho del colgador", (0.07, hombro + 0.04, z), 0.0025, 0.06, metal, lados=8)
    c.cilindro("Clavo", (0.04, hombro + 0.07, z), 0.003, 0.08, metal, eje="x", lados=8)
